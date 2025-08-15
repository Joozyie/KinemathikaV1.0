// Data/AppDbContextFactory.cs
// WHAT IT DOES: Forces EF tools to use local Shared Memory (lpc:.) so no TCP/IP is needed.
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Kinemathika.Data
{
    public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext(string[] args)
        {
            // lpc:. = local shared-memory pipe (no network, no Browser, no TCP/IP)
            var cs = "Server=lpc:.;Database=KinemathikaDb;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True";

            var opts = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlServer(cs)
                .Options;

            return new AppDbContext(opts);
        }
    }
}
