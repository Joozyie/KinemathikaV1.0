// WHAT IT DOES: Teacher dashboard + student pages with full concept names, real "Progress" donut,
// and Recent Attempts showing Problem # and Complete/Incomplete. Also fixes EF error by
// doing dictionary/formatting after materializing to memory.
using Kinemathika.Data;
using Kinemathika.ViewModels.Teacher;
using Kinemathika.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace Kinemathika.Controllers
{
    public class TeacherController : Controller
    {
        private readonly AppDbContext _db;
        private readonly RApiService _rApi;
        public TeacherController(AppDbContext db, RApiService rApi)
        {
            _db = db;
            _rApi = rApi;
        }

        // Map concept codes to full names for all UI
        static readonly Dictionary<string, string> CodeToName = new(StringComparer.OrdinalIgnoreCase)
        {
            ["dd"] = "Distance & Displacement",
            ["sv"] = "Speed & Velocity",
            ["acc"] = "Acceleration"
        };

        // ---------- Sidebar helpers ----------
        private async Task<SidebarVm> BuildSidebarAsync()
        {
            var recent = await _db.Classrooms
                .AsNoTracking()
                .Where(c => !c.IsArchived)
                .OrderBy(c => c.ClassName)
                .Select(c => new ClassCardVm
                {
                    Id = c.ClassroomId,
                    Name = c.ClassName,
                    StudentCount = _db.Enrollments.Count(e => e.ClassroomId == c.ClassroomId)
                })
                .Take(4)
                .ToListAsync();

            return new SidebarVm
            {
                TeacherName = "Prof. Jane Doe",
                RecentClasses = recent
            };
        }
        [HttpGet]
        public async Task<IActionResult> Dashboard(int? classId = null)
        {
            ViewBag.Sidebar = await BuildSidebarAsync();

            IQueryable<string> studentIdsQuery;
            string? className = null;
            List<StudentPerformance>? studentPerformances = null;

            // ---------------------- Fetch student IDs ----------------------
            if (classId.HasValue)
            {
                className = await _db.Classrooms
                    .Where(c => c.ClassroomId == classId.Value)
                    .Select(c => c.ClassName)
                    .FirstOrDefaultAsync();

                studentIdsQuery = _db.Enrollments
                    .AsNoTracking()
                    .Where(e => e.ClassroomId == classId.Value)
                    .Select(e => e.StudentId);

                var studentIdsList = await studentIdsQuery.Distinct().ToListAsync();

                // Fetch students info
                var studentsInfo = await _db.Students
                    .AsNoTracking()
                    .Where(s => studentIdsList.Contains(s.StudentId))
                    .Select(s => new { s.StudentId, s.Name, s.Email })
                    .ToListAsync();

                // Fetch attempts for class students
                var attempts = await _db.AttemptRecords
                    .AsNoTracking()
                    .Where(a => studentIdsList.Contains(a.StudentId))
                    .Select(a => new
                    {
                        a.StudentId,
                        a.ConceptId,
                        a.ProblemNo,
                        a.AttemptsToCorrect,
                        a.TimeToCorrectMs,
                        a.EndedAt
                    })
                    .ToListAsync();

                // ---------------------- Per-student aggregation in C# ----------------------
                studentPerformances = studentsInfo
                    .Select(s =>
                    {
                        var a = attempts.Where(x => x.StudentId == s.StudentId).ToList();
                        var total = Math.Max(1, a.Count);

                        // Progress: distinct completed problems ≤2 tries
                        var completed = a
                            .Where(x => x.AttemptsToCorrect <= 2)
                            .GroupBy(x => new { x.ConceptId, x.ProblemNo })
                            .Count();

                        return new StudentPerformance
                        {
                            StudentId = s.StudentId,
                            Name = string.IsNullOrWhiteSpace(s.Name) ? s.StudentId : s.Name,
                            Email = s.Email ?? "",
                            ProgressPct = Math.Min(1.0, completed / 45.0) * 100,
                            AvgAttempts = a.Any() ? a.Average(x => x.AttemptsToCorrect) : 0.0,
                            AvgTimeSec = a.Any() ? a.Average(x => x.TimeToCorrectMs) / 1000.0 : 0.0,
                        };
                    })
                    .ToList();
            }
            else
            {
                studentIdsQuery = _db.Enrollments
                    .AsNoTracking()
                    .Where(e => !e.Classroom.IsArchived)
                    .Select(e => e.StudentId);
            }

            var studentIds = await studentIdsQuery.Distinct().ToListAsync();

            // ---------------------- Dashboard-level aggregation via R ----------------------
            var attemptsAll = await _db.AttemptRecords.AsNoTracking()
                .Where(a => studentIds.Contains(a.StudentId))
                .Select(a => new
                {
                    a.StudentId,
                    a.ConceptId,
                    a.ProblemNo,
                    a.AttemptsToCorrect,
                    a.TimeToCorrectMs,
                    a.EndedAt
                })
                .ToListAsync();

            var studentsInfoAll = await _db.Students
                .AsNoTracking()
                .Where(s => studentIds.Contains(s.StudentId))
                .Select(s => new { s.StudentId, s.Name, s.Email })
                .ToListAsync();

            var classes = await _db.Classrooms
                .AsNoTracking()
                .Where(c => !c.IsArchived)
                .Select(c => new { c.ClassroomId, c.ClassName })
                .ToListAsync();

            var rInput = new
            {
                attempts = attemptsAll,
                students = studentsInfoAll,
                classes,
                classId,
                className
            };

            var rResult = await _rApi.PostAsync<RDashboardOverviewVm>("overview", rInput);

            foreach (var concept in rResult.Concepts)
            {
                concept.ConceptName = CodeToName.GetValueOrDefault(concept.ConceptId, concept.ConceptName);
            }

            // ---------------------- Wrap into ViewModel ----------------------
            var vm = new TeacherOverviewVm
            {
                ClassName = className,
                Dashboard = rResult,
                StudentPerformances = studentPerformances
            };

            return View(vm);
        }

        // ---------- Student ----------
        [HttpGet]
        public async Task<IActionResult> Student(string studentId)
        {
            if (string.IsNullOrWhiteSpace(studentId))
                return RedirectToAction(nameof(Dashboard));

            ViewBag.Sidebar = await BuildSidebarAsync();

            // Fetch student info
            var student = await _db.Students
                .AsNoTracking()
                .Where(s => s.StudentId == studentId)
                .Select(s => new { s.StudentId, s.Name, s.Email })
                .FirstOrDefaultAsync();

            if (student == null)
            {
                TempData["Toast"] = "Student not found.";
                return RedirectToAction(nameof(Dashboard));
            }

            // Fetch all attempts for this student
            var attempts = await _db.AttemptRecords
                .AsNoTracking()
                .Where(a => a.StudentId == studentId)
                .ToListAsync();

            // Prepare input for R script (same as dashboard but for single student)
            var rInput = new
            {
                attempts,
                students = new[] { student },
                classes = new List<object>(), // not needed here
                classId = (int?)null,
                className = (string?)null
            };

            var rResult = await _rApi.PostAsync<RDashboardOverviewVm>("overview", rInput);

            // Map concept codes to full names
            foreach (var concept in rResult.Concepts)
            {
                concept.ConceptName = CodeToName.GetValueOrDefault(concept.ConceptId, concept.ConceptName);
            }

            // Recent attempts table
            var recent = attempts
                .OrderByDescending(a => a.EndedAt)
                .Take(25)
                .Select(a => new RecentAttemptRow
                {
                    EndedAt = a.EndedAt,
                    StudentId = a.StudentId,
                    ConceptId = CodeToName.GetValueOrDefault(a.ConceptId, a.ConceptId),
                    ProblemId = a.ProblemNo.ToString(),
                    Status = a.AttemptsToCorrect <= 2 ? "Complete" : "Incomplete",
                    FirstTry = a.AttemptsToCorrect == 1,
                    Attempts = a.AttemptsToCorrect,
                    TimeSec = Math.Round(a.TimeToCorrectMs / 1000.0, 1)
                })
                .ToList();

            // Wrap into StudentOverviewVm (same structure as dashboard)
            var vm = new StudentOverviewVm
            {
                StudentId = student.StudentId,
                Name = string.IsNullOrWhiteSpace(student.Name) ? student.StudentId : student.Name,
                Email = student.Email ?? "",
                TotalAttempts = attempts.Count,
                AvgAttemptsToCorrect = attempts.Any() ? Math.Round(attempts.Average(a => a.AttemptsToCorrect), 2) : 0.0,
                AvgAccuracy = 0m, // optional
                FirstTryRate = attempts.Any() ? Math.Round((decimal)attempts.Count(a => a.AttemptsToCorrect == 1) / attempts.Count, 2) : 0m,
                MasteryRate = attempts.Any() ? Math.Round((decimal)attempts.Count(a => a.AttemptsToCorrect <= 2) / attempts.Count, 4) : 0m,
                AvgTimeToCorrectSec = attempts.Any() ? Math.Round(attempts.Average(a => a.TimeToCorrectMs) / 1000.0, 1) : 0.0,
                Concepts = rResult.Concepts.Select(c => c.ConceptName).ToList(),
                ConceptAvgAccuracyPct = rResult.Concepts.Select(c => (int)Math.Round(c.OverallProgress * 100)).ToList(),
                ConceptAvgAttempts = rResult.Concepts.Select(c => Math.Round(c.AvgAttempts, 2)).ToList(),
                RecentAttempts = recent
            };

            return View(vm);
        }


        // ---------- Student CRUD (uses StudentId PK + Enrollment(StudentId)) ----------
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateStudent(int? classId, string name, string email, bool isActive = true)
        {
            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(email))
                return RedirectToAction(nameof(Dashboard), new { classId });

            var student = await _db.Students.FirstOrDefaultAsync(s => s.Email == email);
            if (student == null)
            {
                var newId = "stu_" + Guid.NewGuid().ToString("N")[..6];
                student = new Student { StudentId = newId, Name = name.Trim(), Email = email.Trim() };
                _db.Students.Add(student);
                await _db.SaveChangesAsync();
            }
            else
            {
                student.Name = name.Trim();
                await _db.SaveChangesAsync();
            }

            if (classId.HasValue)
            {
                bool hasEnroll = await _db.Enrollments.AnyAsync(e => e.ClassroomId == classId.Value && e.StudentId == student.StudentId);
                if (!hasEnroll)
                {
                    _db.Enrollments.Add(new Enrollment { ClassroomId = classId.Value, StudentId = student.StudentId });
                    await _db.SaveChangesAsync();
                }
            }

            return RedirectToAction(nameof(Dashboard), new { classId });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> EditStudent(string id, int? classId, string name, string email, bool isActive = true)
        {
            if (string.IsNullOrWhiteSpace(id))
                return RedirectToAction(nameof(Dashboard), new { classId });

            var s = await _db.Students.FirstOrDefaultAsync(x => x.StudentId == id);
            if (s == null) return RedirectToAction(nameof(Dashboard), new { classId });

            s.Name = (name ?? "").Trim();
            s.Email = (email ?? "").Trim();
            await _db.SaveChangesAsync();

            if (classId.HasValue)
            {
                bool hasEnroll = await _db.Enrollments.AnyAsync(e => e.ClassroomId == classId.Value && e.StudentId == s.StudentId);
                if (!hasEnroll)
                {
                    _db.Enrollments.Add(new Enrollment { ClassroomId = classId.Value, StudentId = s.StudentId });
                    await _db.SaveChangesAsync();
                }
            }

            return RedirectToAction(nameof(Dashboard), new { classId });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteStudent(string id, int? classId)
        {
            if (string.IsNullOrWhiteSpace(id))
                return RedirectToAction(nameof(Dashboard), new { classId });

            var s = await _db.Students.FirstOrDefaultAsync(x => x.StudentId == id);
            if (s == null) return RedirectToAction(nameof(Dashboard), new { classId });

            if (classId.HasValue)
            {
                var enroll = await _db.Enrollments.FirstOrDefaultAsync(e => e.ClassroomId == classId.Value && e.StudentId == s.StudentId);
                if (enroll != null)
                {
                    _db.Enrollments.Remove(enroll);
                    await _db.SaveChangesAsync();
                }
            }
            else
            {
                var allEnroll = _db.Enrollments.Where(e => e.StudentId == s.StudentId);
                _db.Enrollments.RemoveRange(allEnroll);
                _db.Students.Remove(s);
                await _db.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Dashboard), new { classId });
        }

        // ---------- Classes list ----------
        [HttpGet]
        public async Task<IActionResult> Classes(bool archived = false)
        {
            ViewBag.Sidebar = await BuildSidebarAsync();

            var classes = await _db.Classrooms
                .AsNoTracking()
                .Where(c => c.IsArchived == archived)
                .OrderBy(c => c.ClassName)
                .Select(c => new ClassCardVm
                {
                    Id = c.ClassroomId,
                    Name = c.ClassName,
                    StudentCount = _db.Enrollments.Count(e => e.ClassroomId == c.ClassroomId),
                    AverageAccuracy = 0m,
                    AvgTimeSpent = ""
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

        // ---------- Class management (rename/archive/unarchive) ----------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RenameClass(int id, string newName, string? returnUrl)
        {
            if (string.IsNullOrWhiteSpace(newName))
            {
                TempData["Toast"] = "Class name is required.";
                return SafeBack(returnUrl, nameof(Classes));
            }

            var cls = await _db.Classrooms.FirstOrDefaultAsync(c => c.ClassroomId == id);
            if (cls == null)
            {
                TempData["Toast"] = "Class not found.";
                return SafeBack(returnUrl, nameof(Classes));
            }

            var exists = await _db.Classrooms
                .AnyAsync(c => c.ClassroomId != id && c.ClassName.ToLower() == newName.Trim().ToLower());
            if (exists)
            {
                TempData["Toast"] = "A class with that name already exists.";
                return SafeBack(returnUrl, nameof(Classes));
            }

            cls.ClassName = newName.Trim();
            await _db.SaveChangesAsync();
            TempData["Toast"] = "Class renamed.";
            return SafeBack(returnUrl, nameof(Classes));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Archive(int id, string? confirmName, string? returnUrl)
        {
            var cls = await _db.Classrooms.FirstOrDefaultAsync(c => c.ClassroomId == id);
            if (cls == null)
            {
                TempData["Toast"] = "Class not found.";
                return SafeBack(returnUrl, nameof(Classes));
            }

            // Require exact match (modal UX)
            if (!string.Equals(cls.ClassName, confirmName ?? string.Empty, StringComparison.Ordinal))
            {
                TempData["Toast"] = "Name confirmation does not match.";
                return SafeBack(returnUrl, nameof(Classes));
            }

            if (!cls.IsArchived)
            {
                cls.IsArchived = true;
                await _db.SaveChangesAsync();
                TempData["Toast"] = $"Archived: {cls.ClassName}";
            }
            return SafeBack(returnUrl, nameof(Classes), new { archived = false });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Unarchive(int id, string? returnUrl)
        {
            var cls = await _db.Classrooms.FirstOrDefaultAsync(c => c.ClassroomId == id);
            if (cls == null)
            {
                TempData["Toast"] = "Class not found.";
                return SafeBack(returnUrl, nameof(Classes), new { archived = true });
            }

            if (cls.IsArchived)
            {
                cls.IsArchived = false;
                await _db.SaveChangesAsync();
                TempData["Toast"] = $"Restored: {cls.ClassName}";
            }
            return SafeBack(returnUrl, nameof(Classes), new { archived = false });
        }

        // Small helper to safely redirect back
        private IActionResult SafeBack(string? returnUrl, string fallbackAction, object? routeValues = null)
            => (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                ? Redirect(returnUrl!)
                : RedirectToAction(fallbackAction, routeValues);

        // ---------- Wizard/Settings/Help ----------
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            ViewBag.Sidebar = await BuildSidebarAsync();
            return View(new CreateClassStep1Vm());
        }

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

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult Assign(CreateClassStep2Vm vm, string? action)
            => RedirectToAction(nameof(Dashboard));

        [HttpGet]
        public async Task<IActionResult> Settings()
        {
            ViewBag.Sidebar = await BuildSidebarAsync();
            return View(new SettingsVm { FirstName = "Jane", LastName = "Doe", Email = "jane.doe@school.edu" });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Settings(SettingsVm vm)
        {
            ViewBag.Sidebar = await BuildSidebarAsync();
            if (!ModelState.IsValid) return View(vm);
            TempData["Toast"] = "Settings saved (stub).";
            return RedirectToAction(nameof(Settings));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult ChangePassword(ChangePasswordVm vm)
        {
            TempData["Toast"] = (string.IsNullOrWhiteSpace(vm.NewPassword) || vm.NewPassword != vm.ConfirmPassword)
                ? "Passwords do not match."
                : "Password changed (stub).";
            return RedirectToAction(nameof(Settings));
        }

        [HttpGet]
        public async Task<IActionResult> Help()
        {
            ViewBag.Sidebar = await BuildSidebarAsync();
            ViewData["IsAuthPage"] = true;
            return View();
        }

        [HttpGet]
        public IActionResult DownloadManual()
        {
            var bytes = Encoding.UTF8.GetBytes("Kinemathika Teacher Manual (placeholder)");
            return File(bytes, "text/plain", "Kinemathika-Teacher-Manual.txt");
        }
    }
}
