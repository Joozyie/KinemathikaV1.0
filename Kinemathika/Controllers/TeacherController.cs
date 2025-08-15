// Controllers/TeacherController.cs
// WHAT IT DOES: Same demo data + pages as yours, but now builds a SidebarVm
// and passes it to every view via ViewBag.Sidebar so the left sidebar renders everywhere.

using Microsoft.AspNetCore.Mvc;
using Kinemathika.ViewModels.Teacher;
using System.Linq;
using System.Text;

namespace Kinemathika.Controllers
{
    public class TeacherController : Controller
    {
        // ---- DEMO DATA (swap to EF later) ----
        private static readonly List<ClassCardVm> ActiveClasses = new()
        {
            new() { Id=1, Name="Physics 1-A", StudentCount=24, AverageAccuracy=0.78m, AvgTimeSpent="50 mins" },
            new() { Id=2, Name="Physics 1-B", StudentCount=21, AverageAccuracy=0.65m, AvgTimeSpent="42 mins" },
            new() { Id=3, Name="Physics 2-A", StudentCount=26, AverageAccuracy=0.83m, AvgTimeSpent="55 mins" },
            new() { Id=4, Name="Algebra 1",   StudentCount=19, AverageAccuracy=0.59m, AvgTimeSpent="38 mins" },
            new() { Id=5, Name="Geometry",    StudentCount=22, AverageAccuracy=0.71m, AvgTimeSpent="46 mins" }
        };

        private static readonly List<ClassCardVm> ArchivedClasses = new(); // start empty

        private bool HasAnyClassForCurrentTeacher() => ActiveClasses.Any();

        // WHAT IT DOES: Builds the sidebar VM (name + up to 4 classes)
        private SidebarVm BuildSidebar() => new SidebarVm
        {
            TeacherName = "Prof. Jane Doe",
            RecentClasses = ActiveClasses.Take(4).ToList()
        };

        [HttpGet]
        public IActionResult Index()
            => HasAnyClassForCurrentTeacher() ? RedirectToAction(nameof(Dashboard))
                                              : RedirectToAction(nameof(Create));

        [HttpGet]
        public IActionResult Dashboard()
        {
            ViewBag.Sidebar = BuildSidebar(); // ← add sidebar

            var vm = new TeacherDashboardVm
            {
                TeacherName = "Prof. Jane Doe",
                Classes = ActiveClasses.ToList(),
                Students = new()
                {
                    new StudentRowVm { Id=1, StudentId="110219252", Name="Alexander McQueen", Email="alex123@example.com" },
                    new StudentRowVm { Id=2, StudentId="130476090", Name="Lewis Hamilton", Email="lewis123@example.com" },
                    new StudentRowVm { Id=3, StudentId="112233445", Name="Ada Lovelace", Email="ada@math.dev" },
                },
                Report = new ReportVm
                {
                    Bars = new() { 90, 72, 68, 40 },
                    Labels = new() { "Distance", "Displacement", "Velocity", "Acceleration" }
                },
                // Keeping this for compatibility if your view still binds Model.Sidebar:
                Sidebar = new SidebarVm { TeacherName = "Prof. Jane Doe", RecentClasses = ActiveClasses.Take(4).ToList() }
            };
            return View(vm);
        }

        // --- All / Archived pages (cards) ---
        [HttpGet]
        public IActionResult Classes(bool archived = false)
        {
            ViewBag.Sidebar = BuildSidebar(); // ← add sidebar

            var list = archived ? ArchivedClasses : ActiveClasses;
            var vm = new TeacherClassesVm
            {
                TeacherName = "Prof. Jane Doe",
                Archived = archived,
                Classes = list.ToList()
            };
            return View(vm);
        }

        // --- Buttons: UI only (no DB yet) ---
        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult Archive(int id)
        {
            TempData["Toast"] = "Archive clicked (no data changes yet).";
            return RedirectToAction(nameof(Classes));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult Unarchive(int id)
        {
            TempData["Toast"] = "Restore clicked (no data changes yet).";
            return RedirectToAction(nameof(Classes), new { archived = true });
        }

        // --- Help page (modal with manual download) ---
        [HttpGet]
        public IActionResult Help()
        {
            ViewBag.Sidebar = BuildSidebar(); // ← add sidebar
            ViewData["IsAuthPage"] = true;
            return View();
        }

        [HttpGet]
        public IActionResult DownloadManual()
        {
            // stub: serve a tiny text file as a placeholder
            var bytes = Encoding.UTF8.GetBytes("Kinemathika Teacher Manual (placeholder)");
            return File(bytes, "text/plain", "Kinemathika-Teacher-Manual.txt");
        }

        // --- Settings (no‑op save & change password) ---
        [HttpGet]
        public IActionResult Settings()
        {
            ViewBag.Sidebar = BuildSidebar(); // ← add sidebar
            return View(new SettingsVm { FirstName = "Jane", LastName = "Doe", Email = "jane.doe@school.edu" });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult Settings(SettingsVm vm)
        {
            ViewBag.Sidebar = BuildSidebar(); // keep sidebar on validation errors too
            if (!ModelState.IsValid) return View(vm);

            TempData["Toast"] = "Settings saved (no database yet).";
            return RedirectToAction(nameof(Settings));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult ChangePassword(ChangePasswordVm vm)
        {
            // After POST-redirect, sidebar will be rebuilt in GET Settings()
            if (string.IsNullOrWhiteSpace(vm.NewPassword) || vm.NewPassword != vm.ConfirmPassword)
            {
                TempData["Toast"] = "Passwords do not match.";
                return RedirectToAction(nameof(Settings));
            }
            TempData["Toast"] = "Password changed (no database yet).";
            return RedirectToAction(nameof(Settings));
        }

        // ---- Wizard (unchanged visuals) ----
        [HttpGet]
        public IActionResult Create()
        {
            ViewBag.Sidebar = BuildSidebar(); // optional: show sidebar even in wizard
            return View(new CreateClassStep1Vm());
        }

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult Create(CreateClassStep1Vm vm)
        {
            // sidebar not needed here; we redirect on success
            if (!ModelState.IsValid)
            {
                ViewBag.Sidebar = BuildSidebar(); // show it again if invalid
                return View(vm);
            }
            TempData["ClassName"] = vm.ClassName?.Trim();
            return RedirectToAction(nameof(Assign));
        }

        [HttpGet]
        public IActionResult Assign()
        {
            ViewBag.Sidebar = BuildSidebar(); // optional: sidebar on step 2 as well

            return View(vm);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult Assign(CreateClassStep2Vm vm, string? action)
    }
}
