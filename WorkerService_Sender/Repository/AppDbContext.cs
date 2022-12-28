
using Microsoft.EntityFrameworkCore;

namespace WorkerService_Sender.Repository
{
    public class AppDbContext : DbContext
    {
        public AppDbContext()
        {

        }
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }
        public virtual DbSet<Account> Accounts { get; set; } = null!;
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Account>(account =>
            {
                account.HasKey(a => a.AccountId);
                account.Property(a => a.AccountId).ValueGeneratedOnAdd();
                account.Property(a => a.Alias).HasMaxLength(27);
                account.Property(a => a.Cbu).HasMaxLength(22);
                account.Property(a => a.Balance);

            });
        }
    }
}