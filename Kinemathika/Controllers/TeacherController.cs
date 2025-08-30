// WHAT IT DOES: Teacher dashboard + student pages with full concept names, real "Progress" donut,
// and Recent Attempts showing Problem # and Complete/Incomplete. Also fixes EF error by
// doing dictionary/formatting after materializing to memory.
using Kinemathika.Data;
using Kinemathika.ViewModels.Teacher;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace Kinemathika.Controllers
{
    public class TeacherController : Controller
    {
        private readonly SbDbContext _db;
        private readonly RApiService _rApi;
        private readonly IHttpContextAccessor _httpContextAccessor;
        public TeacherController(SbDbContext db, RApiService rApi, IHttpContextAccessor httpContextAccessor)
        {
            _db = db;
            _rApi = rApi;
            _httpContextAccessor = httpContextAccessor;
        }

        //Sidebar VM Builder
        private async Task<SidebarVm> BuildSidebarAsync()
        {
            // Get teacher's ID + email from claims
            var user = _httpContextAccessor.HttpContext?.User;
            var userIdStr = user?.FindFirstValue(ClaimTypes.NameIdentifier);
            var teacherName = user?.FindFirstValue(ClaimTypes.Email) ?? "Teacher";

            // Validate user ID
            if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var teacherId))
                throw new InvalidOperationException("User ID is missing or invalid.");
            try
            {
                // 1. Fetch teacher's active classes (ordered by class name for now)
                var classes = await _db.Classes
                    .Where(c => c.TeacherInChargeId == teacherId && !c.IsArchived)
                    .OrderBy(c => c.ClassName)
                    .Select(c => new { c.ClassId, c.ClassName })
                    .ToListAsync();
                // If teacher has no classes
                if (classes.Count == 0)
                {
                    return new SidebarVm
                    {
                        TeacherName = teacherName,
                        RecentClasses = new List<ClassCardVm>()
                    };
                }
                // 2. Get student counts
                var classIds = classes.Select(c => c.ClassId).ToList();
                var students = await _db.Students
                    .Where(s => classIds.Contains(s.ClassId))
                    .Select(s => s.ClassId)
                    .ToListAsync();
                var countsByClass = students
                    .GroupBy(classId => classId)
                    .ToDictionary(g => g.Key, g => g.Count());
                // Build RecentClasses VM
                var recentClasses = classes.Select(c => new ClassCardVm
                {
                    Id = c.ClassId.ToString(),
                    Name = c.ClassName,
                    StudentCount = countsByClass.TryGetValue(c.ClassId, out var cnt) ? cnt : 0
                }).ToList();

                return new SidebarVm
                {
                    TeacherName = teacherName,
                    RecentClasses = recentClasses
                };
            }
            catch (Exception ex)
            {
                // Log the error and return empty sidebar rather than crashing
                // MATTTTTTTTTTTTTT LET'S REPLACE THIS SHIT WITH A PROPER LOGGING/MODAL JS LIBRARY
                Console.WriteLine($"Error building sidebar: {ex.Message}");
                return new SidebarVm
                {
                    TeacherName = teacherName,
                    RecentClasses = new List<ClassCardVm>()
                };
            }
        }

        // --------- Dashboard ----------
        [HttpGet]
        [Authorize(Roles = "teacher")] // Example of attribute for page authorization (check if user has the proper role to view page)
        public async Task<IActionResult> Dashboard(string? classId = null)
        {
            // Construct sidebar
            var sidebar = await BuildSidebarAsync();
            ViewBag.Sidebar = sidebar;

            // Initialize variables
            string? className = null;
            List<StudentPerformance>? studentPerformances = null;
            // Lists for R input
            List<RStudentDto> studentsForR = new();
            List<RAttemptDto> attemptsForR = new();

            // Collect classIds depending on single class / all classes
            List<Guid> targetClassIds = string.IsNullOrEmpty(classId)
                ? sidebar.RecentClasses.Select(c => Guid.Parse(c.Id)).ToList() // All Classes
                : new List<Guid> { Guid.Parse(classId) }; // Specific Class

            // ---------- Fetch students + attempts ----------
            if (targetClassIds.Any())
            {
                // ---------- Students ----------
                var students = await _db.Students
                    .Where(s => targetClassIds.Contains(s.ClassId)) // Get all students for the target classes (all or one)
                    .ToListAsync();

                var studentIds = students.Select(s => s.UserId).ToList(); // convert to student ids

                studentsForR = students.Select(s => new RStudentDto
                {
                    StudentId = s.UserId.ToString(),
                    Name = s.UserId.ToString(),
                    Email = "" // map if available
                }).ToList();

                // ---------- Attempts ----------
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

                // ---------- Single class aggregations ----------
                if (!string.IsNullOrEmpty(classId))
                {
                    var selectedClass = sidebar.RecentClasses.FirstOrDefault(c => c.Id == classId);
                    className = selectedClass?.Name;

                    studentPerformances = studentsForR.Select(s =>
                    {
                        var studentAttempts = attemptsForR.Where(a => a.StudentId == s.StudentId).ToList();
                        var completed = studentAttempts
                            .Where(a => a.AttemptsToCorrect <= 2)
                            .GroupBy(a => new { a.ConceptId, a.ProblemNo })
                            .Count();

                        return new StudentPerformance
                        {
                            StudentId = s.StudentId,
                            Name = s.Name,
                            Email = s.Email,
                            ProgressPct = Math.Min(1.0, completed / 45.0) * 100,
                            AvgAttempts = studentAttempts.Any() ? studentAttempts.Average(a => a.AttemptsToCorrect) : 0.0,
                            AvgTimeSec = studentAttempts.Any() ? studentAttempts.Average(a => a.TimeToCorrectMs) / 1000.0 : 0.0
                        };
                    }).ToList();
                }
            }

            // ---------- R Request preparation ----------
            var rInput = new
            {
                attempts = attemptsForR,
                students = studentsForR,
                classes = sidebar.RecentClasses,
                classId,
                className
            };
            var rResult = await _rApi.PostAsync<RDashboardOverviewVm>("overview", rInput);
            var vm = new TeacherOverviewVm
            {
                ClassName = className,
                Dashboard = rResult,
                StudentPerformances = studentPerformances
            };

            ViewBag.ClassId = classId ?? ""; // simple assignment for class-specific or all-class 
            return View(vm);
        }

        // ---------- Student ----------
        [HttpGet]
        [Authorize(Roles = "teacher")]
        public async Task<IActionResult> Student(string studentId)
        {
            if (string.IsNullOrWhiteSpace(studentId))
                return RedirectToAction(nameof(Dashboard));

            ViewBag.Sidebar = await BuildSidebarAsync();

            // ---------- Fetch student ----------
            var student = await _db.Students
                .AsNoTracking()
                .Where(s => s.UserId.ToString() == studentId || s.StudentNumber == studentId)
                .Select(s => new RStudentDto
                {
                    StudentId = s.UserId.ToString(),
                    Name = string.IsNullOrWhiteSpace(s.StudentNumber) ? s.UserId.ToString() : s.StudentNumber,
                    Email = "" // accounts.student doesn’t have email; fill in if available elsewhere
                })
                .FirstOrDefaultAsync();

            if (student == null)
            {
                TempData["Toast"] = "Student not found.";
                return RedirectToAction(nameof(Dashboard));
            }

            // ---------- Fetch attempts ----------
            var attempts = await _db.ProblemAttempts
                .AsNoTracking()
                .Where(a => a.UserId.ToString() == student.StudentId)
                .Select(a => new RAttemptDto
                {
                    StudentId = a.UserId.ToString(),
                    ConceptId = a.ConceptId,
                    ProblemNo = a.ProblemId,
                    AttemptsToCorrect = a.AttemptsToCorrect,
                    TimeToCorrectMs = a.TimeToCorrectMs,
                    EndedAt = a.EndedAt
                })
                .ToListAsync();

            // ---------- Send to R ----------
            var rInput = new
            {
                attempts,
                students = new[] { student },
                classes = new List<object>(), // not needed here
                classId = (string?)null,
                className = (string?)null
            };

            var rResult = await _rApi.PostAsync<RDashboardOverviewVm>("overview", rInput);

            // ---------- Recent attempts ----------
            var recent = attempts
                .OrderByDescending(a => a.EndedAt)
                .Take(25)
                .Select(a => new RecentAttemptRow
                {
                    EndedAt = a.EndedAt ?? DateTime.MinValue,
                    StudentId = a.StudentId,
                    ConceptId = a.ConceptId,
                    // ConceptId = CodeToName.GetValueOrDefault(a.ConceptId, a.ConceptId),
                    ProblemId = a.ProblemNo,
                    Status = a.AttemptsToCorrect <= 2 ? "Complete" : "Incomplete",
                    FirstTry = a.AttemptsToCorrect == 1,
                    Attempts = a.AttemptsToCorrect,
                    TimeSec = Math.Round(a.TimeToCorrectMs / 1000.0, 1)
                })
                .ToList();

            // ---------- Build VM ----------
            var vm = new StudentOverviewVm
            {
                StudentId = student.StudentId,
                Name = student.Name,
                Email = student.Email,
                TotalAttempts = attempts.Count,
                AvgAttemptsToCorrect = attempts.Any() ? Math.Round(attempts.Average(a => a.AttemptsToCorrect), 2) : 0.0,
                AvgAccuracy = 0m, // legacy, remove? or maybe not? idk
                FirstTryRate = attempts.Any() ? Math.Round((decimal)attempts.Count(a => a.AttemptsToCorrect == 1) / attempts.Count, 2) : 0m,
                MasteryRate = attempts.Any() ? Math.Round((decimal)attempts.Count(a => a.AttemptsToCorrect <= 2) / attempts.Count, 4) : 0m,
                AvgTimeToCorrectSec = attempts.Any() ? Math.Round(attempts.Average(a => a.TimeToCorrectMs) / 1000.0, 1) : 0.0,
                Concepts = rResult.Concepts.Select(c => new ConceptProgressVm
                {
                    ConceptId = c.ConceptId,
                    ConceptName = c.ConceptId,
                    OverallProgress = c.OverallProgress,
                    AvgAttempts = Math.Round(c.AvgAttempts, 2),
                    AvgTime = Math.Round(c.AvgTime, 2),
                    AttemptsTrend = c.AttemptsTrend,
                    TimeTrend = c.TimeTrend
                }).ToList(),
                RecentAttempts = recent
            };

            return View(vm);
        }

        // Temp Cutoff Point
        // Ples fix mattttttttttttttttttttttt


        // // ---------- Student CRUD (uses StudentId PK + Enrollment(StudentId)) ----------
        // [HttpPost, ValidateAntiForgeryToken]
        // public async Task<IActionResult> CreateStudent(int? classId, string name, string email, bool isActive = true)
        // {
        //     if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(email))
        //         return RedirectToAction(nameof(Dashboard), new { classId });

        //     var student = await _db.Students.FirstOrDefaultAsync(s => s.Email == email);
        //     if (student == null)
        //     {
        //         var newId = "stu_" + Guid.NewGuid().ToString("N")[..6];
        //         student = new Student { StudentId = newId, Name = name.Trim(), Email = email.Trim() };
        //         _db.Students.Add(student);
        //         await _db.SaveChangesAsync();
        //     }
        //     else
        //     {
        //         student.Name = name.Trim();
        //         await _db.SaveChangesAsync();
        //     }

        //     if (classId.HasValue)
        //     {
        //         bool hasEnroll = await _db.Enrollments.AnyAsync(e => e.ClassroomId == classId.Value && e.StudentId == student.StudentId);
        //         if (!hasEnroll)
        //         {
        //             _db.Enrollments.Add(new Enrollment { ClassroomId = classId.Value, StudentId = student.StudentId });
        //             await _db.SaveChangesAsync();
        //         }
        //     }

        //     return RedirectToAction(nameof(Dashboard), new { classId });
        // }

        // [HttpPost, ValidateAntiForgeryToken]
        // public async Task<IActionResult> EditStudent(string id, int? classId, string name, string email, bool isActive = true)
        // {
        //     if (string.IsNullOrWhiteSpace(id))
        //         return RedirectToAction(nameof(Dashboard), new { classId });

        //     var s = await _db.Students.FirstOrDefaultAsync(x => x.StudentId == id);
        //     if (s == null) return RedirectToAction(nameof(Dashboard), new { classId });

        //     s.Name = (name ?? "").Trim();
        //     s.Email = (email ?? "").Trim();
        //     await _db.SaveChangesAsync();

        //     if (classId.HasValue)
        //     {
        //         bool hasEnroll = await _db.Enrollments.AnyAsync(e => e.ClassroomId == classId.Value && e.StudentId == s.StudentId);
        //         if (!hasEnroll)
        //         {
        //             _db.Enrollments.Add(new Enrollment { ClassroomId = classId.Value, StudentId = s.StudentId });
        //             await _db.SaveChangesAsync();
        //         }
        //     }

        //     return RedirectToAction(nameof(Dashboard), new { classId });
        // }

        // [HttpPost, ValidateAntiForgeryToken]
        // public async Task<IActionResult> DeleteStudent(string id, int? classId)
        // {
        //     if (string.IsNullOrWhiteSpace(id))
        //         return RedirectToAction(nameof(Dashboard), new { classId });

        //     var s = await _db.Students.FirstOrDefaultAsync(x => x.StudentId == id);
        //     if (s == null) return RedirectToAction(nameof(Dashboard), new { classId });

        //     if (classId.HasValue)
        //     {
        //         var enroll = await _db.Enrollments.FirstOrDefaultAsync(e => e.ClassroomId == classId.Value && e.StudentId == s.StudentId);
        //         if (enroll != null)
        //         {
        //             _db.Enrollments.Remove(enroll);
        //             await _db.SaveChangesAsync();
        //         }
        //     }
        //     else
        //     {
        //         var allEnroll = _db.Enrollments.Where(e => e.StudentId == s.StudentId);
        //         _db.Enrollments.RemoveRange(allEnroll);
        //         _db.Students.Remove(s);
        //         await _db.SaveChangesAsync();
        //     }

        //     return RedirectToAction(nameof(Dashboard), new { classId });
        // }

        // // ---------- Classes list ----------
        // [HttpGet]
        // public async Task<IActionResult> Classes(bool archived = false)
        // {
        //     ViewBag.Sidebar = await BuildSidebarAsync();

        //     var classes = await _db.Classrooms
        //         .AsNoTracking()
        //         .Where(c => c.IsArchived == archived)
        //         .OrderBy(c => c.ClassName)
        //         .Select(c => new ClassCardVm
        //         {
        //             Id = c.ClassroomId,
        //             Name = c.ClassName,
        //             StudentCount = _db.Enrollments.Count(e => e.ClassroomId == c.ClassroomId),
        //             AverageAccuracy = 0m,
        //             AvgTimeSpent = ""
        //         })
        //         .ToListAsync();

        //     var vm = new TeacherClassesVm
        //     {
        //         TeacherName = "Prof. Jane Doe",
        //         Archived = archived,
        //         Classes = classes
        //     };
        //     return View(vm);
        // }

        // // ---------- Class management (rename/archive/unarchive) ----------
        // [HttpPost]
        // [ValidateAntiForgeryToken]
        // public async Task<IActionResult> RenameClass(int id, string newName, string? returnUrl)
        // {
        //     if (string.IsNullOrWhiteSpace(newName))
        //     {
        //         TempData["Toast"] = "Class name is required.";
        //         return SafeBack(returnUrl, nameof(Classes));
        //     }

        //     var cls = await _db.Classrooms.FirstOrDefaultAsync(c => c.ClassroomId == id);
        //     if (cls == null)
        //     {
        //         TempData["Toast"] = "Class not found.";
        //         return SafeBack(returnUrl, nameof(Classes));
        //     }

        //     var exists = await _db.Classrooms
        //         .AnyAsync(c => c.ClassroomId != id && c.ClassName.ToLower() == newName.Trim().ToLower());
        //     if (exists)
        //     {
        //         TempData["Toast"] = "A class with that name already exists.";
        //         return SafeBack(returnUrl, nameof(Classes));
        //     }

        //     cls.ClassName = newName.Trim();
        //     await _db.SaveChangesAsync();
        //     TempData["Toast"] = "Class renamed.";
        //     return SafeBack(returnUrl, nameof(Classes));
        // }

        // [HttpPost]
        // [ValidateAntiForgeryToken]
        // public async Task<IActionResult> Archive(int id, string? confirmName, string? returnUrl)
        // {
        //     var cls = await _db.Classrooms.FirstOrDefaultAsync(c => c.ClassroomId == id);
        //     if (cls == null)
        //     {
        //         TempData["Toast"] = "Class not found.";
        //         return SafeBack(returnUrl, nameof(Classes));
        //     }

        //     // Require exact match (modal UX)
        //     if (!string.Equals(cls.ClassName, confirmName ?? string.Empty, StringComparison.Ordinal))
        //     {
        //         TempData["Toast"] = "Name confirmation does not match.";
        //         return SafeBack(returnUrl, nameof(Classes));
        //     }

        //     if (!cls.IsArchived)
        //     {
        //         cls.IsArchived = true;
        //         await _db.SaveChangesAsync();
        //         TempData["Toast"] = $"Archived: {cls.ClassName}";
        //     }
        //     return SafeBack(returnUrl, nameof(Classes), new { archived = false });
        // }

        // [HttpPost]
        // [ValidateAntiForgeryToken]
        // public async Task<IActionResult> Unarchive(int id, string? returnUrl)
        // {
        //     var cls = await _db.Classrooms.FirstOrDefaultAsync(c => c.ClassroomId == id);
        //     if (cls == null)
        //     {
        //         TempData["Toast"] = "Class not found.";
        //         return SafeBack(returnUrl, nameof(Classes), new { archived = true });
        //     }

        //     if (cls.IsArchived)
        //     {
        //         cls.IsArchived = false;
        //         await _db.SaveChangesAsync();
        //         TempData["Toast"] = $"Restored: {cls.ClassName}";
        //     }
        //     return SafeBack(returnUrl, nameof(Classes), new { archived = false });
        // }

        // // Small helper to safely redirect back
        // private IActionResult SafeBack(string? returnUrl, string fallbackAction, object? routeValues = null)
        //     => (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        //         ? Redirect(returnUrl!)
        //         : RedirectToAction(fallbackAction, routeValues);

        // // ---------- Wizard/Settings/Help ----------
        // [HttpGet]
        // public async Task<IActionResult> Create()
        // {
        //     ViewBag.Sidebar = await BuildSidebarAsync();
        //     return View(new CreateClassStep1Vm());
        // }

        // [HttpPost, ValidateAntiForgeryToken]
        // public IActionResult Create(CreateClassStep1Vm vm)
        // {
        //     if (!ModelState.IsValid) return View(vm);
        //     TempData["ClassName"] = vm.ClassName?.Trim();
        //     return RedirectToAction(nameof(Assign));
        // }

        // [HttpGet]
        // public async Task<IActionResult> Assign()
        // {
        //     ViewBag.Sidebar = await BuildSidebarAsync();
        //     var className = TempData.Peek("ClassName") as string ?? "New Class";
        //     return View(new CreateClassStep2Vm { ClassName = className, Students = new() });
        // }

        // [HttpPost, ValidateAntiForgeryToken]
        // public IActionResult Assign(CreateClassStep2Vm vm, string? action)
        //     => RedirectToAction(nameof(Dashboard));

        // [HttpGet]
        // public async Task<IActionResult> Settings()
        // {
        //     ViewBag.Sidebar = await BuildSidebarAsync();
        //     return View(new SettingsVm { FirstName = "Jane", LastName = "Doe", Email = "jane.doe@school.edu" });
        // }

        // [HttpPost, ValidateAntiForgeryToken]
        // public async Task<IActionResult> Settings(SettingsVm vm)
        // {
        //     ViewBag.Sidebar = await BuildSidebarAsync();
        //     if (!ModelState.IsValid) return View(vm);
        //     TempData["Toast"] = "Settings saved (stub).";
        //     return RedirectToAction(nameof(Settings));
        // }

        // [HttpPost, ValidateAntiForgeryToken]
        // public IActionResult ChangePassword(ChangePasswordVm vm)
        // {
        //     TempData["Toast"] = (string.IsNullOrWhiteSpace(vm.NewPassword) || vm.NewPassword != vm.ConfirmPassword)
        //         ? "Passwords do not match."
        //         : "Password changed (stub).";
        //     return RedirectToAction(nameof(Settings));
        // }

        // [HttpGet]
        // public async Task<IActionResult> Help()
        // {
        //     ViewBag.Sidebar = await BuildSidebarAsync();
        //     ViewData["IsAuthPage"] = true;
        //     return View();
        // }

        // [HttpGet]
        // public IActionResult DownloadManual()
        // {
        //     var bytes = Encoding.UTF8.GetBytes("Kinemathika Teacher Manual (placeholder)");
        //     return File(bytes, "text/plain", "Kinemathika-Teacher-Manual.txt");
        // }

    }
}
