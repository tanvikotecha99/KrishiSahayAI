using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace KrishiSahayAI.Data
{
    public class ApplicationDbContext
        : IdentityDbContext<IdentityUser>, IDataProtectionKeyContext
    {
        public ApplicationDbContext(
            DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // Stores ASP.NET Core Data Protection keys
        // so login cookies remain valid across Cloud Run restarts.
        public DbSet<DataProtectionKey> DataProtectionKeys { get; set; }
            = null!;
    }
}