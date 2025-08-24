// WHAT IT DOES: JSON APIs for charts/tiles + progress. Accepts concept code or full name.
// Progress = per-student distinct completed problems (≤2 tries) / 45, averaged as needed.
using Kinemathika.Data;
using Kinemathika.Models.Analytics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace Kinemathika.Controllers
{
    [ApiController]
    [Route("api/analytics")]
    public class ApiAnalyticsController : ControllerBase
    {
        private readonly AppDbContext _db;
        public ApiAnalyticsController(AppDbContext db) => _db = db;

        // ---- Concept label helpers (code <-> full name) ----
        private static readonly Dictionary<string, string> CodeToName = new(StringComparer.OrdinalIgnoreCase)
        {
            ["dd"] = "Distance & Displacement",
            ["sv"] = "Speed & Velocity",
            ["acc"] = "Acceleration"
        };
        private static readonly Dictionary<string, string> NameToCode =
            CodeToName.ToDictionary(kv => kv.Value, kv => kv.Key, StringComparer.OrdinalIgnoreCase);

        private static string? NormalizeConcept(string? idOrName)
        {
            if (string.IsNullOrWhiteSpace(idOrName) || string.Equals(idOrName, "all", StringComparison.OrdinalIgnoreCase))
                return null;                  // treat null/'all' as no filter
            if (CodeToName.ContainsKey(idOrName))   // short code already
                return idOrName;
            return NameToCode.TryGetValue(idOrName, out var code) ? code : idOrName;
        }

        private static IQueryable<AttemptRecord> FilterConcept(IQueryable<AttemptRecord> q, string? concept)
        {
            var code = NormalizeConcept(concept);
            return code is null ? q : q.Where(a => a.ConceptId == code);
        }

        private async Task<List<string>> ClassStudentIdsAsync(int classId)
            => await _db.Enrollments.AsNoTracking()
                   .Where(e => e.ClassroomId == classId)
                   .Select(e => e.StudentId)
                   .Distinct()
                   .ToListAsync();

        private static double AvgAccuracyPct(IQueryable<AttemptRecord> q)
            => q.Any() ? 100.0 * q.Average(a => 1.0 / Math.Max(1, a.AttemptsToCorrect)) : 0.0;

        // ========================= TRENDS =========================

        // WHAT IT DOES: Overall trend (Attempts or Time) – last 6 months.
        [HttpGet("overview/trend")]
        public async Task<IActionResult> OverviewTrend(string metric = "Attempts", string? conceptId = null)
        {
            var since = DateTime.UtcNow.Date.AddMonths(-6);
            var q = FilterConcept(_db.AttemptRecords.AsNoTracking().Where(a => a.EndedAt >= since), conceptId);

            var points = await q.GroupBy(a => a.EndedAt.Date)
                .Select(g => new
                {
                    x = g.Key,
                    y = metric == "Attempts"
                        ? g.Average(a => (double)a.AttemptsToCorrect)
                        : g.Average(a => (double)a.TimeToCorrectMs)
                })
                .OrderBy(x => x.x)
                .ToListAsync();

            return Ok(new { name = metric, points });
        }

        // WHAT IT DOES: Per-class trend (Attempts or Time).
        [HttpGet("class/{classId:int}/trend")]
        public async Task<IActionResult> ClassTrend(int classId, string metric = "Attempts", string? conceptId = null)
        {
            var since = DateTime.UtcNow.Date.AddMonths(-6);
            var q = FilterConcept(
                _db.AttemptRecords.AsNoTracking().Where(a => a.ClassId == classId && a.EndedAt >= since),
                conceptId);

            var points = await q.GroupBy(a => a.EndedAt.Date)
                .Select(g => new
                {
                    x = g.Key,
                    y = metric == "Attempts"
                        ? g.Average(a => (double)a.AttemptsToCorrect)
                        : g.Average(a => (double)a.TimeToCorrectMs)
                })
                .OrderBy(x => x.x)
                .ToListAsync();

            return Ok(new { name = metric, points });
        }

        // WHAT IT DOES: Per-student trend (Attempts or Time).
        [HttpGet("student/{studentId}/trend")]
        public async Task<IActionResult> StudentTrend(string studentId, string metric = "Attempts", string? conceptId = null)
        {
            var since = DateTime.UtcNow.Date.AddMonths(-6);
            var q = FilterConcept(
                _db.AttemptRecords.AsNoTracking().Where(a => a.StudentId == studentId && a.EndedAt >= since),
                conceptId);

            var points = await q.GroupBy(a => a.EndedAt.Date)
                .Select(g => new
                {
                    x = g.Key,
                    y = metric == "Attempts"
                        ? g.Average(a => (double)a.AttemptsToCorrect)
                        : g.Average(a => (double)a.TimeToCorrectMs)
                })
                .OrderBy(x => x.x)
                .ToListAsync();

            return Ok(new { name = metric, points });
        }

        // ===================== CONCEPT SUMMARY =====================

        // WHAT IT DOES: Overall avg attempts / time (optional concept filter).
        [HttpGet("overview/concept-summary")]
        public async Task<IActionResult> OverviewConceptSummary(string conceptId)
        {
            var q = FilterConcept(_db.AttemptRecords.AsNoTracking(), conceptId);
            var avgAttempts = await q.Select(a => (double?)a.AttemptsToCorrect).AverageAsync() ?? 0.0;
            var avgTimeMs = await q.Select(a => (double?)a.TimeToCorrectMs).AverageAsync() ?? 0.0;
            return Ok(new { avgAttempts = Math.Round(avgAttempts, 2), avgTimeSec = Math.Round(avgTimeMs / 1000.0, 0) });
        }

        // WHAT IT DOES: Class avg attempts / time (optional concept filter).
        [HttpGet("class/{classId:int}/concept-summary")]
        public async Task<IActionResult> ClassConceptSummary(int classId, string conceptId)
        {
            var q = FilterConcept(_db.AttemptRecords.AsNoTracking().Where(a => a.ClassId == classId), conceptId);
            var avgAttempts = await q.Select(a => (double?)a.AttemptsToCorrect).AverageAsync() ?? 0.0;
            var avgTimeMs = await q.Select(a => (double?)a.TimeToCorrectMs).AverageAsync() ?? 0.0;
            return Ok(new { avgAttempts = Math.Round(avgAttempts, 2), avgTimeSec = Math.Round(avgTimeMs / 1000.0, 0) });
        }

        // WHAT IT DOES: Student avg attempts / time (optional concept filter).
        [HttpGet("student/{studentId}/concept-summary")]
        public async Task<IActionResult> StudentConceptSummary(string studentId, string? conceptId = null)
        {
            var q = FilterConcept(_db.AttemptRecords.AsNoTracking().Where(a => a.StudentId == studentId), conceptId);
            var avgAttempts = await q.Select(a => (double?)a.AttemptsToCorrect).AverageAsync() ?? 0.0;
            var avgTimeMs = await q.Select(a => (double?)a.TimeToCorrectMs).AverageAsync() ?? 0.0;
            return Ok(new { avgAttempts = Math.Round(avgAttempts, 2), avgTimeSec = Math.Round(avgTimeMs / 1000.0, 0) });
        }

        // ====================== CONCEPT BARS =======================

        // WHAT IT DOES: Overall concept bars (accuracy proxy) – supports "all" or a single concept.
        [HttpGet("overview/concept-bars")]
        public async Task<IActionResult> OverviewConceptBars(string mode = "all")
        {
            string? code = NormalizeConcept(mode);
            var q = _db.AttemptRecords.AsNoTracking();
            if (code is not null) q = q.Where(a => a.ConceptId == code);

            var rows = await q.GroupBy(a => a.ConceptId)
                .Select(g => new
                {
                    concept = g.Key,
                    pct = 100.0 * g.Average(a => 1.0 / Math.Max(1, a.AttemptsToCorrect))
                })
                .ToListAsync();

            var order = new[] { "Distance & Displacement", "Speed & Velocity", "Acceleration" };
            var data = rows
                .OrderBy(r => CodeToName.ContainsKey(r.concept) ? Array.IndexOf(order, CodeToName[r.concept]) : int.MaxValue)
                .Select(r => new { x = CodeToName.GetValueOrDefault(r.concept, r.concept), y = Math.Round(r.pct, 1) })
                .ToList();

            return Ok(data);
        }

        // WHAT IT DOES: Class concept bars (accuracy proxy) – supports "all" or a single concept.
        [HttpGet("class/{classId:int}/concept-bars")]
        public async Task<IActionResult> ClassConceptBars(int classId, string mode = "all")
        {
            if (string.Equals(mode, "all", StringComparison.OrdinalIgnoreCase))
            {
                var rows = await _db.AttemptRecords.AsNoTracking()
                    .Where(a => a.ClassId == classId)
                    .GroupBy(a => a.ConceptId)
                    .Select(g => new
                    {
                        concept = g.Key,
                        pct = 100.0 * g.Average(a => 1.0 / Math.Max(1, a.AttemptsToCorrect))
                    })
                    .ToListAsync();

                var list = rows
                    .OrderBy(r => CodeToName.ContainsKey(r.concept) ? Array.IndexOf(new[] { "dd", "sv", "acc" }, r.concept) : int.MaxValue)
                    .Select(r => new { x = CodeToName.GetValueOrDefault(r.concept, r.concept), y = Math.Round(r.pct, 1) })
                    .ToList();

                return Ok(list);
            }
            else
            {
                string? code = NormalizeConcept(mode);
                var avg = await _db.AttemptRecords.AsNoTracking()
                    .Where(a => a.ClassId == classId && (code == null || a.ConceptId == code))
                    .Select(a => 1.0 / Math.Max(1, a.AttemptsToCorrect))
                    .AverageAsync();

                return Ok(new[]
                {
                    new { x = CodeToName.GetValueOrDefault(code ?? "", mode), y = Math.Round(100.0 * avg, 1) }
                });
            }
        }

        // ========================= PROGRESS ========================

        // WHAT IT DOES: Per-student progress (optionally per concept).
        [HttpGet("student/{studentId}/progress")]
        public async Task<IActionResult> StudentProgress(string studentId, string conceptId = "all")
        {
            var code = NormalizeConcept(conceptId);
            var q = _db.AttemptRecords.AsNoTracking().Where(a => a.StudentId == studentId);
            if (code is not null) q = q.Where(a => a.ConceptId == code);

            var completed = await q
                .Where(a => a.AttemptsToCorrect <= 2)
                .GroupBy(a => new { a.ConceptId, a.ProblemNo })
                .Select(g => 1)
                .CountAsync();

            int total = code is null ? 45 : 15;
            double pct = total > 0 ? (double)completed / total : 0;
            return Ok(new { completed, total, pct = Math.Round(pct, 4) });
        }

        // WHAT IT DOES: Class progress = average of each enrolled student's progress.
        [HttpGet("class/{classId:int}/progress")]
        public async Task<IActionResult> ClassProgress(int classId)
        {
            var studentIds = await ClassStudentIdsAsync(classId);
            if (studentIds.Count == 0) return Ok(new { avgPct = 0.0, population = 0 });

            var perStudentCompleted = await _db.AttemptRecords.AsNoTracking()
                .Where(a => studentIds.Contains(a.StudentId) && a.AttemptsToCorrect <= 2)
                .GroupBy(a => new { a.StudentId, a.ConceptId, a.ProblemNo })
                .Select(g => g.Key.StudentId)
                .GroupBy(s => s)
                .Select(g => new { StudentId = g.Key, Completed = g.Count() })
                .ToListAsync();

            var map = perStudentCompleted.ToDictionary(x => x.StudentId, x => x.Completed);
            double avgPct = studentIds.Average(id => Math.Min(1.0, (map.TryGetValue(id, out var c) ? c : 0) / 45.0));

            return Ok(new { avgPct = Math.Round(avgPct, 4), population = studentIds.Count });
        }

        // WHAT IT DOES: Overall progress = average of all students (non-archived classes).
        [HttpGet("overview/progress")]
        public async Task<IActionResult> OverviewProgress()
        {
            var studentIds = await _db.Enrollments.AsNoTracking()
                .Where(e => !e.Classroom.IsArchived)
                .Select(e => e.StudentId)
                .Distinct()
                .ToListAsync();

            if (studentIds.Count == 0) return Ok(new { avgPct = 0.0, population = 0 });

            var perStudentCompleted = await _db.AttemptRecords.AsNoTracking()
                .Where(a => studentIds.Contains(a.StudentId) && a.AttemptsToCorrect <= 2)
                .GroupBy(a => new { a.StudentId, a.ConceptId, a.ProblemNo })
                .Select(g => g.Key.StudentId)
                .GroupBy(s => s)
                .Select(g => new { StudentId = g.Key, Completed = g.Count() })
                .ToListAsync();

            var map = perStudentCompleted.ToDictionary(x => x.StudentId, x => x.Completed);
            double avgPct = studentIds.Average(id => Math.Min(1.0, (map.TryGetValue(id, out var c) ? c : 0) / 45.0));

            return Ok(new { avgPct = Math.Round(avgPct, 4), population = studentIds.Count });
        }
    }
}
