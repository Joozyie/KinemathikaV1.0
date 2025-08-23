// JSON API for D3 charts and concept summaries
using Kinemathika.Data;
using Kinemathika.Models.Analytics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kinemathika.Controllers
{
    [ApiController]
    [Route("api/analytics")]
    public class ApiAnalyticsController : ControllerBase
    {
        private readonly AppDbContext _db;
        public ApiAnalyticsController(AppDbContext db) => _db = db;

        // Helpers ----------------------------------------------------
        private static IQueryable<AttemptRecord> FilterConcept(IQueryable<AttemptRecord> q, string? conceptId)
        {
            if (string.IsNullOrWhiteSpace(conceptId)) return q;
            return q.Where(a => a.concept_id == conceptId);
        }

        private static double AvgTimeSec(IQueryable<AttemptRecord> q)
            => q.Any() ? q.Average(a => (double)a.time_to_correct_ms) / 1000.0 : 0.0;

        private static double AvgAttempts(IQueryable<AttemptRecord> q)
            => q.Any() ? q.Average(a => (double)a.attempts_to_correct) : 0.0;

        private static double AvgAccuracyPct(IQueryable<AttemptRecord> q)
            => q.Any() ? (q.Average(a => (double)(a.level_attempt_accuracy)) * 100.0) : 0.0;

        private async Task<List<string>> ClassStudentIdsAsync(int classId)
        {
            return await _db.Enrollments.AsNoTracking()
                .Where(e => e.ClassroomId == classId)
                .Select(e => e.Student.StudentId)
                .Distinct()
                .ToListAsync();
        }

        private static string[] AllConcepts = new[] { "dd", "sv", "acc" };
        private static Dictionary<string, string> ConceptName = new()
        {
            ["dd"] = "Distance & Displacement",
            ["sv"] = "Speed & Velocity",
            ["acc"] = "Acceleration"
        };

        // ---- OVERALL (All classes) trend (Attempts | TimeMs) ----
        [HttpGet("overview/trend")]
        public async Task<IActionResult> OverviewTrend(string metric = "Attempts", string? conceptId = null)
        {
            var since = DateTime.UtcNow.Date.AddMonths(-6); // was: AddDays(-30)
            var q = _db.AttemptRecords.AsNoTracking()
                    .Where(a => a.ended_at >= since);

            if (!string.IsNullOrWhiteSpace(conceptId))
                q = q.Where(a => a.concept_id == conceptId);

            var points = await q.GroupBy(a => a.ended_at.Date)
                .Select(g => new {
                    x = g.Key,
                    y = metric == "Attempts"
                        ? g.Average(a => a.attempts_to_correct)
                        : g.Average(a => a.time_to_correct_ms)
                })
                .OrderBy(x => x.x)
                .ToListAsync();

            return Ok(new { name = metric, points });
        }

        // ---- PER-CLASS trend (Attempts | TimeMs), optional conceptId ----
        [HttpGet("class/{classId:int}/trend")]
        public async Task<IActionResult> ClassTrend(int classId, string metric = "Attempts", string? conceptId = null)
        {
            var since = DateTime.UtcNow.Date.AddMonths(-6); // was: AddDays(-30)

            var classStudentIds = await _db.Enrollments.AsNoTracking()
                .Where(e => e.ClassroomId == classId)
                .Select(e => e.Student.StudentId)
                .Distinct()
                .ToListAsync();

            var q = _db.AttemptRecords.AsNoTracking()
                    .Where(a => a.ended_at >= since && classStudentIds.Contains(a.student_id));

            if (!string.IsNullOrWhiteSpace(conceptId))
                q = q.Where(a => a.concept_id == conceptId);

            var points = await q.GroupBy(a => a.ended_at.Date)
                .Select(g => new {
                    x = g.Key,
                    y = metric == "Attempts"
                        ? g.Average(a => a.attempts_to_correct)
                        : g.Average(a => a.time_to_correct_ms)
                })
                .OrderBy(x => x.x)
                .ToListAsync();

            return Ok(new { name = metric, points });
        }

        // ---- PER-STUDENT trend (Attempts | TimeMs) ----
        [HttpGet("student/{studentId}/trend")]
        public async Task<IActionResult> StudentTrend(string studentId, string metric = "Attempts", string? conceptId = null)
        {
            // show more history (last 6 months)
            var since = DateTime.UtcNow.Date.AddMonths(-6);

            var q = _db.AttemptRecords.AsNoTracking()
                .Where(a => a.student_id == studentId && a.ended_at >= since);

            if (!string.IsNullOrWhiteSpace(conceptId))
                q = q.Where(a => a.concept_id == conceptId);

            var points = await q.GroupBy(a => a.ended_at.Date)
                .Select(g => new {
                    x = g.Key,
                    y = metric == "Attempts"
                        ? g.Average(a => a.attempts_to_correct)
                        : g.Average(a => a.time_to_correct_ms)
                })
                .OrderBy(x => x.x)
                .ToListAsync();

            return Ok(new { name = metric, points });
        }

        // CONCEPT SUMMARY tiles (avg attempts/time) ------------------
        // ---- OVERVIEW concept summary (avg attempts / avg time sec) ----
        [HttpGet("overview/concept-summary")]
        public async Task<IActionResult> OverviewConceptSummary(string conceptId)
        {
            var q = FilterConcept(_db.AttemptRecords.AsNoTracking(), conceptId);

            var avgAttempts = await q
                .Select(a => (double?)a.attempts_to_correct)
                .AverageAsync() ?? 0.0;

            var avgTimeMs = await q
                .Select(a => (double?)a.time_to_correct_ms)
                .AverageAsync() ?? 0.0;

            var avgTimeSec = Math.Round(avgTimeMs / 1000.0, 0);

            return Ok(new { avgAttempts = Math.Round(avgAttempts, 2), avgTimeSec });
        }

        // ---- CLASS concept summary (avg attempts / avg time sec) ----
        [HttpGet("class/{classId:int}/concept-summary")]
        public async Task<IActionResult> ClassConceptSummary(int classId, string conceptId)
        {
            var ids = await ClassStudentIdsAsync(classId);

            var q = FilterConcept(
                _db.AttemptRecords.AsNoTracking()
                    .Where(a => ids.Contains(a.student_id)),
                conceptId);

            var avgAttempts = await q
                .Select(a => (double?)a.attempts_to_correct)
                .AverageAsync() ?? 0.0;

            var avgTimeMs = await q
                .Select(a => (double?)a.time_to_correct_ms)
                .AverageAsync() ?? 0.0;

            var avgTimeSec = Math.Round(avgTimeMs / 1000.0, 0);

            return Ok(new { avgAttempts = Math.Round(avgAttempts, 2), avgTimeSec });
        }


        // CONCEPT BARS (combined 3 bars OR single bar)  -----------------------
        [HttpGet("overview/concept-bars")]
        public async Task<IActionResult> OverviewConceptBars(string mode = "all")
        {
            // Which concepts should we include?
            string[] wanted = string.Equals(mode, "all", StringComparison.OrdinalIgnoreCase)
                ? AllConcepts
                : new[] { mode };

            // Query once, async, and let SQL do the grouping/averaging.
            // "Accuracy %" ~ average(1 / attempts_to_correct) * 100  (round to 1 decimal)
            var rows = await _db.AttemptRecords
                .AsNoTracking()
                .Where(a => wanted.Contains(a.concept_id))
                .GroupBy(a => a.concept_id)
                .Select(g => new
                {
                    concept = g.Key,
                    pct = 100.0 * g.Average(a => 1.0 / (double)a.attempts_to_correct)
                })
                .ToListAsync();

            // Order to match AllConcepts and map to { x, y } for your bar chart
            var data = rows
                .OrderBy(r => Array.IndexOf(AllConcepts, r.concept))
                .Select(r => new
                {
                    x = ConceptName.TryGetValue(r.concept, out var name) ? name : r.concept,
                    y = Math.Round(r.pct, 1)
                })
                .ToList();

            return Ok(data);
        }


        [HttpGet("class/{classId:int}/concept-bars")]
        public async Task<IActionResult> ClassConceptBars(int classId, string mode = "all")
        {
            var ids = await ClassStudentIdsAsync(classId);

            if (mode == "all")
            {
                var list = new List<object>();
                foreach (var c in AllConcepts)
                {
                    var q = _db.AttemptRecords.AsNoTracking()
                        .Where(a => a.concept_id == c && ids.Contains(a.student_id));
                    list.Add(new { x = ConceptName[c], y = Math.Round(AvgAccuracyPct(q), 1) });
                }
                return Ok(list);
            }
            else
            {
                var q = _db.AttemptRecords.AsNoTracking()
                    .Where(a => a.concept_id == mode && ids.Contains(a.student_id));
                var one = new[] { new { x = ConceptName.GetValueOrDefault(mode, mode), y = Math.Round(AvgAccuracyPct(q), 1) } };
                return Ok(one);
            }
        }

        // OPTIONAL: Per-student trend kept from your previous version.
    }
}
