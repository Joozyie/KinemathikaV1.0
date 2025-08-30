using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Rotativa.AspNetCore;
using Kinemathika.Data;
using Kinemathika.ViewModels.Teacher;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace Kinemathika.Controllers
{
    [Authorize(Roles = "teacher")]
    public class ReportController : Controller
    {
        private readonly SbDbContext _db;
        private readonly RApiService _rApi;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ReportController(
            SbDbContext db,
            RApiService rApi,
            IHttpContextAccessor httpContextAccessor)
        {
            _db = db;
            _rApi = rApi;
            _httpContextAccessor = httpContextAccessor;
        }

        // Concept name mapping (for UI)
        static readonly Dictionary<string, string> CodeToName = new(StringComparer.OrdinalIgnoreCase)
        {
            ["all"] = "All Concepts",
            ["C101"] = "Distance & Displacement",
            ["C102"] = "Speed & Velocity",
            ["C103"] = "Acceleration"
        };

        // --------- Simple HTML View ----------
        [HttpGet]
        public async Task<IActionResult> Dashboard(string classId)
        {
            if (string.IsNullOrEmpty(classId))
                return BadRequest("ClassId is required for reports.");

            var vm = await BuildOverviewAsync(classId);
            return View("Dashboard", vm); // reuse Teacher Dashboard view
        }

        // --------- Report Generation Endpoint ----------
        [HttpGet]
        public async Task<IActionResult> ClassReport(string classId)
        {
            if (string.IsNullOrEmpty(classId))
                return BadRequest("ClassId is required for reports.");

            var vm = await BuildOverviewAsync(classId);

            return new ViewAsPdf("ClassReport", vm)
            {
                FileName = $"Class_Report_{vm.ClassName}_{DateTime.Now:yyyyMMdd}.pdf",
                PageOrientation = Rotativa.AspNetCore.Options.Orientation.Landscape,
                PageSize = Rotativa.AspNetCore.Options.Size.A4,
                ContentDisposition = Rotativa.AspNetCore.Options.ContentDisposition.Attachment
            };
        }

        // --------- Report Builder ----------
        private async Task<TeacherOverviewVm> BuildOverviewAsync(string classId)
        {
            // Teacher identity check
            var user = _httpContextAccessor.HttpContext?.User;
            var userIdStr = user?.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var teacherId))
                throw new InvalidOperationException("User ID is missing or invalid.");

            // Make sure the teacher owns this class
            var selectedClass = await _db.Classes
                .Where(c => c.ClassId.ToString() == classId && c.TeacherInChargeId == teacherId && !c.IsArchived)
                .Select(c => new { c.ClassId, c.ClassName })
                .FirstOrDefaultAsync();

            if (selectedClass == null)
                throw new UnauthorizedAccessException("Class not found or access denied.");

            // ---------- Students ----------
            var students = await _db.Students
                .Where(s => s.ClassId == selectedClass.ClassId)
                .ToListAsync();

            var studentIds = students.Select(s => s.UserId).ToList();

            var studentsForR = students.Select(s => new RStudentDto
            {
                StudentId = s.UserId.ToString(),
                Name = s.UserId.ToString(),
                Email = ""                  // maybe? maybe not?
            }).ToList();

            // ---------- Attempts ----------
            var attemptsForR = new List<RAttemptDto>();
            if (studentIds.Any())
            {
                var attempts = await _db.ProblemAttempts
                    .Where(a => studentIds.Contains(a.UserId))
                    .ToListAsync();

                attemptsForR = attempts.Select(a => new RAttemptDto
                {
                    StudentId = a.UserId.ToString(),
                    ConceptId = a.ConceptId,
                    ProblemNo = a.ProblemId,
                    AttemptsToCorrect = a.AttemptsToCorrect,
                    TimeToCorrectMs = a.TimeToCorrectMs,
                    EndedAt = a.EndedAt
                }).ToList();
            }

            // ---------- Per-student aggregation ----------
            var studentPerformances = studentsForR.Select(s =>
            {
                var sa = attemptsForR.Where(a => a.StudentId == s.StudentId).ToList();
                var completed = sa
                    .Where(a => a.AttemptsToCorrect <= 2)
                    .GroupBy(a => new { a.ConceptId, a.ProblemNo })
                    .Count();

                return new StudentPerformance
                {
                    StudentId = s.StudentId,
                    Name = s.Name,
                    Email = s.Email,
                    ProgressPct = Math.Min(1.0, completed / 45.0) * 100,
                    AvgAttempts = sa.Any() ? sa.Average(a => a.AttemptsToCorrect) : 0.0,
                    AvgTimeSec = sa.Any() ? sa.Average(a => a.TimeToCorrectMs) / 1000.0 : 0.0
                };
            }).ToList();

            // ---------- Call R API ----------
            var rInput = new
            {
                attempts = attemptsForR,
                students = studentsForR,
                classes = new[] { new { selectedClass.ClassId, selectedClass.ClassName } },
                classId,
                className = selectedClass.ClassName
            };

            var rResult = await _rApi.PostAsync<RDashboardOverviewVm>("overview", rInput);

            foreach (var concept in rResult.Concepts)
                concept.ConceptName = CodeToName.GetValueOrDefault(concept.ConceptId, concept.ConceptName);

            return new TeacherOverviewVm
            {
                ClassName = selectedClass.ClassName,
                Dashboard = rResult,
                StudentPerformances = studentPerformances
            };
        }
    }
}
