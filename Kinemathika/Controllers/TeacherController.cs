// WHAT IT DOES: Teacher dashboard + classes + student details + student CRUD,
// PLUS class RenameClass, Archive (with confirmName), Unarchive, and returnUrl redirects.
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
        public TeacherController(AppDbContext db) => _db = db;

        // ---------- Sidebar helpers ----------
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
                    AvgAcc = (double?)(
                        from e in _db.Enrollments
                        where e.ClassroomId == c.Id
                        join a in _db.AttemptRecords on e.Student.StudentId equals a.student_id
                        select a.level_attempt_accuracy
                    ).Average(),
                    AvgMs = (double?)(
                        from e in _db.Enrollments
                        where e.ClassroomId == c.Id
                        join a in _db.AttemptRecords on e.Student.StudentId equals a.student_id
                        where a.ended_status == "correct"
                        select a.time_to_correct_ms
                    ).Average()
                })
                .OrderBy(x => x.Name)
                .ToListAsync();

            return raw.Select(x => new ClassCardVm
            {
                Id = x.Id,
                Name = x.Name,
                StudentCount = x.StudentCount,
                AverageAccuracy = x.AvgAcc.HasValue ? (decimal)Math.Round(x.AvgAcc.Value, 2) : 0m,
                AvgTimeSpent = x.AvgMs.HasValue ? $"{Math.Round(x.AvgMs.Value / 60000.0):0} mins" : "0 mins"
            }).ToList();
        }

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

        [HttpGet]
        public IActionResult Index() => RedirectToAction(nameof(Dashboard));

        // ---------- Dashboard ----------
        [HttpGet]
        public async Task<IActionResult> Dashboard(int? classId = null)
        {
            ViewBag.Sidebar = await BuildSidebarAsync();

            IQueryable<string> studentIdsQuery;
            string? className = null;

            if (classId.HasValue)
            {
                className = await _db.Classrooms
                    .Where(c => c.Id == classId.Value)
                    .Select(c => c.Name)
                    .FirstOrDefaultAsync();

                if (className == null) classId = null;
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
            var attempts = _db.AttemptRecords.AsNoTracking();
            var filteredAttempts = attempts.Where(a => studentIds.Contains(a.student_id));

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

            var studentsInfo = await _db.Students
                .AsNoTracking()
                .Where(s => studentIds.Contains(s.StudentId))
                .Select(s => new { s.StudentId, s.Name, s.Email, s.Id })
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

            var tabs = new List<ClassStudentsVm>();
            if (!classId.HasValue)
            {
                var activeClasses = await _db.Classrooms
                    .AsNoTracking()
                    .Where(c => !c.IsArchived)
                    .OrderBy(c => c.Name)
                    .Select(c => new { c.Id, c.Name })
                    .ToListAsync();

                var directory = studentsInfo;
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

            // recent attempts for BOTH overall and class view
            var recentAttempts = await filteredAttempts
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
                .ToListAsync();

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
                RecentAttempts = recentAttempts
            };

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

        // ---------- Student Overview ----------
        [HttpGet]
        public async Task<IActionResult> Student(string studentId)
        {
            if (string.IsNullOrWhiteSpace(studentId))
                return RedirectToAction(nameof(Dashboard));

            ViewBag.Sidebar = await BuildSidebarAsync();

            var s = await _db.Students
                .AsNoTracking()
                .Where(x => x.StudentId == studentId)
                .Select(x => new { x.StudentId, x.Name, x.Email })
                .FirstOrDefaultAsync();

            if (s is null) { TempData["Toast"] = "Student not found."; return RedirectToAction(nameof(Dashboard)); }

            var attempts = _db.AttemptRecords.AsNoTracking().Where(a => a.student_id == studentId);

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

            var total = Math.Max(1, agg.TotalAttempts);

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

        // ---------- Student CRUD (unchanged from your working version) ----------
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
                bool hasEnroll = await _db.Enrollments.AnyAsync(e => e.ClassroomId == classId.Value && e.StudentDbId == student.Id);
                if (!hasEnroll)
                {
                    _db.Enrollments.Add(new Enrollment { ClassroomId = classId.Value, StudentDbId = student.Id });
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
                bool hasEnroll = await _db.Enrollments.AnyAsync(e => e.ClassroomId == classId.Value && e.StudentDbId == s.Id);
                if (!hasEnroll)
                {
                    _db.Enrollments.Add(new Enrollment { ClassroomId = classId.Value, StudentDbId = s.Id });
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
                var enroll = await _db.Enrollments.FirstOrDefaultAsync(e => e.ClassroomId == classId.Value && e.StudentDbId == s.Id);
                if (enroll != null)
                {
                    _db.Enrollments.Remove(enroll);
                    await _db.SaveChangesAsync();
                }
            }
            else
            {
                var allEnroll = _db.Enrollments.Where(e => e.StudentDbId == s.Id);
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
                .OrderBy(c => c.Name)
                .Select(c => new ClassCardVm
                {
                    Id = c.Id,
                    Name = c.Name,
                    StudentCount = _db.Enrollments.Count(e => e.ClassroomId == c.Id),
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

            var cls = await _db.Classrooms.FirstOrDefaultAsync(c => c.Id == id);
            if (cls == null)
            {
                TempData["Toast"] = "Class not found.";
                return SafeBack(returnUrl, nameof(Classes));
            }

            // Optional: avoid duplicates under the same teacher scope if you track that
            var exists = await _db.Classrooms
                .AnyAsync(c => c.Id != id && c.Name.ToLower() == newName.Trim().ToLower());
            if (exists)
            {
                TempData["Toast"] = "A class with that name already exists.";
                return SafeBack(returnUrl, nameof(Classes));
            }

            cls.Name = newName.Trim();
            await _db.SaveChangesAsync();
            TempData["Toast"] = "Class renamed.";
            return SafeBack(returnUrl, nameof(Classes));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Archive(int id, string? confirmName, string? returnUrl)
        {
            var cls = await _db.Classrooms.FirstOrDefaultAsync(c => c.Id == id);
            if (cls == null)
            {
                TempData["Toast"] = "Class not found.";
                return SafeBack(returnUrl, nameof(Classes));
            }

            // Require correct confirm name (case sensitive to match modal UX)
            if (!string.Equals(cls.Name, confirmName ?? string.Empty, StringComparison.Ordinal))
            {
                TempData["Toast"] = "Name confirmation does not match.";
                return SafeBack(returnUrl, nameof(Classes));
            }

            if (!cls.IsArchived)
            {
                cls.IsArchived = true;
                await _db.SaveChangesAsync();
                TempData["Toast"] = $"Archived: {cls.Name}";
            }
            return SafeBack(returnUrl, nameof(Classes), new { archived = false });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Unarchive(int id, string? returnUrl)
        {
            var cls = await _db.Classrooms.FirstOrDefaultAsync(c => c.Id == id);
            if (cls == null)
            {
                TempData["Toast"] = "Class not found.";
                return SafeBack(returnUrl, nameof(Classes), new { archived = true });
            }

            if (cls.IsArchived)
            {
                cls.IsArchived = false;
                await _db.SaveChangesAsync();
                TempData["Toast"] = $"Restored: {cls.Name}";
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

        [HttpPost, ValidateAntiForgeryToken]
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
