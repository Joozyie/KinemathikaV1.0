// Controllers/TeacherController.cs
// WHAT IT DOES: Uses EF to fill sidebar + Classes list from your DB.
using Kinemathika.Data;
using Kinemathika.Models.Analytics;
using Kinemathika.ViewModels.Teacher;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace Kinemathika.Controllers
{
    public class TeacherController : Controller
    {
        private readonly AppDbContext _db;
        public TeacherController(AppDbContext db) => _db = db;

        // --- helper: map classrooms to cards (active or archived) ---
        // WHAT IT DOES: builds ClassCardVm list with StudentCount + avg accuracy/time.
        private async Task<List<ClassCardVm>> GetClassCardsAsync(bool archived)
        {
            var raw = await _db.Classrooms
                .AsNoTracking()
                .Where(c => c.IsArchived == archived)
                .Select(c => new
                {
                    c.Id,
                    c.Name,
                    StudentCount = c.Enrollments.Count(),

                    // Avg accuracy across attempts of students enrolled in this class
                    AvgAcc = (double?)(
                        from e in _db.Enrollments
                        where e.ClassroomId == c.Id
                        join a in _db.AttemptRecords
                            on e.Student.StudentId equals a.student_id
                        select a.level_attempt_accuracy
                    ).Average(),

                    // Avg time-to-correct (ms) for correct attempts for these students
                    // Avg time-to-correct (ms) for correct attempts for these students
                    AvgMs = (double?)(
                        from e in _db.Enrollments
                        where e.ClassroomId == c.Id
                        join a in _db.AttemptRecords on e.Student.StudentId equals a.student_id
                        where a.ended_status == "correct"    // ← was: a.ended_status = AttemptEndedStatus.correct
                        select a.time_to_correct_ms
                    ).Average()
                })
                .OrderBy(x => x.Name)
                .ToListAsync();

            // shape into the VM the views expect
            return raw.Select(x => new ClassCardVm
            {
                Id = x.Id,
                Name = x.Name,
                StudentCount = x.StudentCount,
                AverageAccuracy = x.AvgAcc.HasValue ? (decimal)Math.Round(x.AvgAcc.Value, 2) : 0m,
                AvgTimeSpent = x.AvgMs.HasValue ? $"{Math.Round(x.AvgMs.Value / 60000.0):0} mins" : "0 mins"
            }).ToList();
        }

        // Controllers/TeacherController.cs (helper)
        // what it does: loads up to 4 non-archived classes + student counts for the sidebar
        private async Task<SidebarVm> BuildSidebarAsync()
        {
            var recent = await _db.Classrooms
                .AsNoTracking()
                .Where(c => !c.IsArchived)
                .OrderBy(c => c.Name)
                .Select(c => new ClassCardVm
                {
                    Id = c.Id,
                    Name = c.Name,
                    StudentCount = _db.Enrollments.Count(e => e.ClassroomId == c.Id)
                })
                .Take(4)
                .ToListAsync();

            return new SidebarVm
            {
                TeacherName = "Prof. Jane Doe",
                RecentClasses = recent
            };
        }


        // GET /Teacher  → redirect to dashboard
        [HttpGet]
        public IActionResult Index() => RedirectToAction(nameof(Dashboard));

        // GET /Teacher/Dashboard
        [HttpGet]
        public async Task<IActionResult> Dashboard(int? classId = null)
        {
            ViewBag.Sidebar = await BuildSidebarAsync();

            // --- determine scope (overall or by class) ---
            IQueryable<string> studentIdsQuery;
            string? className = null;

            if (classId.HasValue)
            {
                className = await _db.Classrooms
                    .Where(c => c.Id == classId.Value)
                    .Select(c => c.Name)
                    .FirstOrDefaultAsync();

                if (className == null) classId = null; // fallback to overall if not found
            }

            if (classId.HasValue)
            {
                studentIdsQuery = _db.Enrollments
                    .AsNoTracking()
                    .Where(e => e.ClassroomId == classId.Value)
                    .Select(e => e.Student.StudentId);
            }
            else
            {
                studentIdsQuery = _db.Enrollments
                    .AsNoTracking()
                    .Where(e => !e.Classroom.IsArchived)
                    .Select(e => e.Student.StudentId);
            }

            var studentIds = await studentIdsQuery.Distinct().ToListAsync();

            // --- attempts (filtered) ---
            var attempts = _db.AttemptRecords.AsNoTracking();
            var filteredAttempts = attempts.Where(a => studentIds.Contains(a.student_id));

            // --- aggregates for KPIs from filtered set ---
            var agg = await filteredAttempts.GroupBy(_ => 1)
                .Select(g => new
                {
                    TotalAttempts = g.Count(),
                    AvgAccuracy = (decimal?)g.Average(a => a.level_attempt_accuracy) ?? 0m,
                    AvgCorrectMs = g.Where(a => a.ended_status == "correct")
                                       .Select(a => (double?)a.time_to_correct_ms).Average() ?? 0.0,
                    FirstTryCorrect = g.Count(a => a.first_attempt_correct),
                    Mastery = g.Count(a => a.mastery_valid),
                    DistinctStudents = g.Select(a => a.student_id).Distinct().Count()
                })
                .FirstOrDefaultAsync() ?? new
                {
                    TotalAttempts = 0,
                    AvgAccuracy = 0m,
                    AvgCorrectMs = 0.0,
                    FirstTryCorrect = 0,
                    Mastery = 0,
                    DistinctStudents = 0
                };

            var byConcept = await filteredAttempts
                .GroupBy(a => a.concept_id)
                .Select(g => new
                {
                    concept = g.Key,
                    avgAccPct = (int)Math.Round(g.Average(x => x.level_attempt_accuracy) * 100),
                    avgAttempts = g.Average(x => x.attempts_to_correct)
                })
                .OrderBy(x => x.concept)
                .ToListAsync();

            // --- student directory + per-student stats (filtered) ---
            var studentsInfo = await _db.Students
                .AsNoTracking()
                .Where(s => studentIds.Contains(s.StudentId))
                .Select(s => new { s.StudentId, s.Name, s.Email })
                .ToListAsync();

            var byStudent = await filteredAttempts
                .GroupBy(a => a.student_id)
                .Select(g => new
                {
                    StudentId = g.Key,
                    TotalAttempts = g.Count(),
                    AvgAccuracy = (decimal?)g.Average(x => x.level_attempt_accuracy) ?? 0m,
                    LastActive = (DateTime?)g.Max(x => x.ended_at)
                })
                .ToListAsync();

            // --- build Overall tabs (one per active class) ONLY when overall ---
            var tabs = new List<ClassStudentsVm>();
            if (!classId.HasValue)
            {
                var activeClasses = await _db.Classrooms
                    .AsNoTracking()
                    .Where(c => !c.IsArchived)
                    .OrderBy(c => c.Name)
                    .Select(c => new { c.Id, c.Name })
                    .ToListAsync();

                var directory = studentsInfo; // already loaded for overall set
                var statsById = byStudent.ToDictionary(x => x.StudentId, x => x);

                foreach (var cls in activeClasses)
                {
                    var classStudentIds = await _db.Enrollments
                        .AsNoTracking()
                        .Where(e => e.ClassroomId == cls.Id)
                        .Select(e => e.Student.StudentId)
                        .Distinct()
                        .ToListAsync();

                    var rows = classStudentIds
                        .Join(directory, id => id, s => s.StudentId, (id, s) => s)
                        .Select(s =>
                        {
                            statsById.TryGetValue(s.StudentId, out var st);
                            return new StudentRowVm
                            {
                                StudentId = s.StudentId,
                                Name = string.IsNullOrWhiteSpace(s.Name) ? s.StudentId : s.Name,
                                Email = s.Email ?? "",
                                TotalAttempts = st?.TotalAttempts ?? 0,
                                AvgAccuracy = Math.Round(st?.AvgAccuracy ?? 0m, 2),
                                LastActive = st?.LastActive
                            };
                        })
                        .OrderByDescending(r => r.LastActive ?? DateTime.MinValue)
                        .ThenBy(r => r.Name)
                        .ToList();

                    tabs.Add(new ClassStudentsVm
                    {
                        ClassId = cls.Id,
                        ClassName = cls.Name,
                        Students = rows
                    });
                }
            }

            // --- flat Students list (used for Class Overview table) ---
            var studentRows = studentsInfo
                .GroupJoin(byStudent, s => s.StudentId, a => a.StudentId, (s, g) => new { s, stat = g.FirstOrDefault() })
                .Select(x => new StudentRowVm
                {
                    StudentId = x.s.StudentId,
                    Name = string.IsNullOrWhiteSpace(x.s.Name) ? x.s.StudentId : x.s.Name,
                    Email = x.s.Email ?? "",
                    TotalAttempts = x.stat?.TotalAttempts ?? 0,
                    AvgAccuracy = Math.Round(x.stat?.AvgAccuracy ?? 0m, 2),
                    LastActive = x.stat?.LastActive
                })
                .OrderByDescending(r => r.LastActive ?? DateTime.MinValue)
                .ThenBy(r => r.Name)
                .ToList();

            // --- Overview section; only load RecentAttempts for Overall ---
            var overview = new TeacherOverviewVm
            {
                TotalStudents = agg.DistinctStudents,
                TotalClasses = await _db.Classrooms.CountAsync(c => !c.IsArchived),
                AvgAccuracy = Math.Round(agg.AvgAccuracy, 2),
                AvgTimeToCorrectSec = Math.Round(agg.AvgCorrectMs / 1000.0, 1),
                FirstTryCorrectRate = (agg.TotalAttempts > 0) ? Math.Round((decimal)agg.FirstTryCorrect / agg.TotalAttempts, 2) : 0m,
                MasteryRate = (agg.TotalAttempts > 0) ? Math.Round((decimal)agg.Mastery / agg.TotalAttempts, 2) : 0m,
                Concepts = byConcept.Select(x => x.concept).ToList(),
                ConceptAvgAccuracyPct = byConcept.Select(x => x.avgAccPct).ToList(),
                ConceptAvgAttempts = byConcept.Select(x => Math.Round(x.avgAttempts, 2)).ToList(),
                RecentAttempts = classId.HasValue
                    ? new List<RecentAttemptRow>() // hide on class view
                    : await attempts
                        .OrderByDescending(a => a.ended_at)
                        .Take(10)
                        .Select(a => new RecentAttemptRow
                        {
                            StudentId = a.student_id,
                            ConceptId = a.concept_id,
                            ProblemId = a.problem_id,
                            Status = a.ended_status,
                            FirstTry = a.first_attempt_correct,
                            Attempts = a.attempts_to_correct,
                            TimeSec = Math.Round(a.time_to_correct_ms / 1000.0, 1),
                            EndedAt = a.ended_at
                        })
                        .ToListAsync()
            };

            // --- build VM (note: StudentTabs uses local 'tabs') ---
            var vm = new TeacherDashboardVm
            {
                TeacherName = "Prof. Jane Doe",
                Students = studentRows,
                Report = new ReportVm
                {
                    Bars = byConcept.Select(x => x.avgAccPct).ToList(),
                    Labels = byConcept.Select(x => x.concept).ToList()
                },
                Overview = overview,
                CurrentClassId = classId,
                CurrentClassName = className,
                StudentTabs = classId.HasValue ? new List<ClassStudentsVm>() : tabs
            };

            return View(vm);
        }

        // WHAT IT DOES: Shows a single student's overview (KPIs, concept stats, history).
        [HttpGet]
        public async Task<IActionResult> Student(string studentId)
        {
            if (string.IsNullOrWhiteSpace(studentId))
                return RedirectToAction(nameof(Dashboard));

            ViewBag.Sidebar = await BuildSidebarAsync();

            // Directory info
            var s = await _db.Students
                .AsNoTracking()
                .Where(x => x.StudentId == studentId)
                .Select(x => new { x.StudentId, x.Name, x.Email })
                .FirstOrDefaultAsync();

            if (s is null) { TempData["Toast"] = "Student not found."; return RedirectToAction(nameof(Dashboard)); }

            // Attempts for this student
            var attempts = _db.AttemptRecords.AsNoTracking().Where(a => a.student_id == studentId);

            // KPIs
            var agg = await attempts.GroupBy(_ => 1)
                .Select(g => new
                {
                    TotalAttempts = g.Count(),
                    AvgAccuracy = (decimal?)g.Average(a => a.level_attempt_accuracy) ?? 0m,
                    AvgCorrectMs = g.Where(a => a.ended_status == "correct")
                                    .Select(a => (double?)a.time_to_correct_ms).Average() ?? 0.0,
                    FirstTry = g.Count(a => a.first_attempt_correct),
                    Mastery = g.Count(a => a.mastery_valid)
                })
                .FirstOrDefaultAsync() ?? new { TotalAttempts = 0, AvgAccuracy = 0m, AvgCorrectMs = 0.0, FirstTry = 0, Mastery = 0 };

            // Concept breakdown for this student
            var byConcept = await attempts
                .GroupBy(a => a.concept_id)
                .Select(g => new
                {
                    concept = g.Key,
                    avgAccPct = (int)Math.Round(g.Average(x => x.level_attempt_accuracy) * 100),
                    avgAttempts = g.Average(x => x.attempts_to_correct)
                })
                .OrderBy(x => x.concept)
                .ToListAsync();

            // Recent attempts (this student only)
            var recent = await attempts
                .OrderByDescending(a => a.ended_at)
                .Take(25)
                .Select(a => new RecentAttemptRow
                {
                    EndedAt = a.ended_at,
                    StudentId = a.student_id,
                    ConceptId = a.concept_id,
                    ProblemId = a.problem_id,
                    Status = a.ended_status,
                    FirstTry = a.first_attempt_correct,
                    Attempts = a.attempts_to_correct,
                    TimeSec = Math.Round(a.time_to_correct_ms / 1000.0, 1)
                })
                .ToListAsync();

            var total = Math.Max(1, agg.TotalAttempts); // avoid div0

            var vm = new StudentOverviewVm
            {
                StudentId = s.StudentId,
                Name = string.IsNullOrWhiteSpace(s.Name) ? s.StudentId : s.Name,
                Email = s.Email ?? "",
                TotalAttempts = agg.TotalAttempts,
                AvgAccuracy = Math.Round(agg.AvgAccuracy, 2),
                FirstTryRate = Math.Round((decimal)agg.FirstTry / total, 2),
                MasteryRate = Math.Round((decimal)agg.Mastery / total, 2),
                AvgTimeToCorrectSec = Math.Round(agg.AvgCorrectMs / 1000.0, 1),
                Concepts = byConcept.Select(x => x.concept).ToList(),
                ConceptAvgAccuracyPct = byConcept.Select(x => x.avgAccPct).ToList(),
                ConceptAvgAttempts = byConcept.Select(x => Math.Round(x.avgAttempts, 2)).ToList(),
                RecentAttempts = recent
            };

            return View(vm);
        }

        // GET /Teacher/Classes?archived=true|false
        // WHAT IT DOES: lists active (default) or archived classes with counts/stats.
        [HttpGet]
        public async Task<IActionResult> Classes(bool archived = false)
        {
            ViewBag.Sidebar = await BuildSidebarAsync();

            var classes = await _db.Classrooms
                .AsNoTracking()
                .Where(c => c.IsArchived == archived)
                .OrderBy(c => c.Name)
                .Select(c => new ClassCardVm
                {
                    Id = c.Id,
                    Name = c.Name,
                    StudentCount = _db.Enrollments.Count(e => e.ClassroomId == c.Id),
                    AverageAccuracy = 0m,        // placeholder if you don’t compute it yet
                    AvgTimeSpent = ""            // placeholder
                })
                .ToListAsync();

            var vm = new TeacherClassesVm
            {
                TeacherName = "Prof. Jane Doe",
                Archived = archived,
                Classes = classes
            };
            return View(vm);
        }

        // POST stubs keep your buttons working
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Archive(int id)
        {
            var cls = await _db.Classrooms.FirstOrDefaultAsync(c => c.Id == id);
            if (cls == null)
            {
                TempData["Toast"] = "Class not found.";
                return RedirectToAction(nameof(Classes));
            }

            if (!cls.IsArchived)
            {
                cls.IsArchived = true;
                await _db.SaveChangesAsync();
                TempData["Toast"] = $"Archived: {cls.Name}";
            }
            else
            {
                TempData["Toast"] = "Class is already archived.";
            }

            // After archiving, keep user on All Classes (active list)
            return RedirectToAction(nameof(Classes), new { archived = false });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Unarchive(int id)
        {
            var cls = await _db.Classrooms.FirstOrDefaultAsync(c => c.Id == id);
            if (cls == null)
            {
                TempData["Toast"] = "Class not found.";
                return RedirectToAction(nameof(Classes), new { archived = true });
            }

            if (cls.IsArchived)
            {
                cls.IsArchived = false;
                await _db.SaveChangesAsync();
                TempData["Toast"] = $"Restored: {cls.Name}";
            }
            else
            {
                TempData["Toast"] = "Class is already active.";
            }

            // After restoring, show All Classes so the card appears there
            return RedirectToAction(nameof(Classes), new { archived = false });
        }

        // Wizard + Settings + Help remain the same; we only add the sidebar.
        [HttpGet]
        public async Task<IActionResult> Create() { ViewBag.Sidebar = await BuildSidebarAsync(); return View(new CreateClassStep1Vm()); }

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult Create(CreateClassStep1Vm vm)
        {
            if (!ModelState.IsValid) return View(vm);
            TempData["ClassName"] = vm.ClassName?.Trim();
            return RedirectToAction(nameof(Assign));
        }

        [HttpGet]
        public async Task<IActionResult> Assign()
        {
            ViewBag.Sidebar = await BuildSidebarAsync();
            var className = TempData.Peek("ClassName") as string ?? "New Class";
            return View(new CreateClassStep2Vm { ClassName = className, Students = new() });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Assign(CreateClassStep2Vm vm, string? action)
            => RedirectToAction(nameof(Dashboard));

        [HttpGet]
        public async Task<IActionResult> Settings() { ViewBag.Sidebar = await BuildSidebarAsync(); return View(new SettingsVm { FirstName = "Jane", LastName = "Doe", Email = "jane.doe@school.edu" }); }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Settings(SettingsVm vm) { ViewBag.Sidebar = await BuildSidebarAsync(); if (!ModelState.IsValid) return View(vm); TempData["Toast"] = "Settings saved (stub)."; return RedirectToAction(nameof(Settings)); }

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult ChangePassword(ChangePasswordVm vm) { TempData["Toast"] = (string.IsNullOrWhiteSpace(vm.NewPassword) || vm.NewPassword != vm.ConfirmPassword) ? "Passwords do not match." : "Password changed (stub)."; return RedirectToAction(nameof(Settings)); }

        [HttpGet]
        public async Task<IActionResult> Help() { ViewBag.Sidebar = await BuildSidebarAsync(); ViewData["IsAuthPage"] = true; return View(); }

        [HttpGet]
        public IActionResult DownloadManual() { var bytes = Encoding.UTF8.GetBytes("Kinemathika Teacher Manual (placeholder)"); return File(bytes, "text/plain", "Kinemathika-Teacher-Manual.txt"); }
    }
}
