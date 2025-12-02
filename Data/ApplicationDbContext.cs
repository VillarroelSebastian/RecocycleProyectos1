using Microsoft.EntityFrameworkCore;
using Web_Recocycle.Models;

namespace Web_Recocycle.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        public DbSet<Empresa> Empresa { get; set; }
        public DbSet<Premio> Premio { get; set; }
    }
}
