
using Microsoft.EntityFrameworkCore;

namespace WorkerService_Sender.Repository
{
    public class ServerRepository : IServerRepository
    {
        private AppDbContext _context;

        private DbContextOptions<AppDbContext> GetAllOptions()
        {
            DbContextOptionsBuilder<AppDbContext> optionsBuilder = 
                            new DbContextOptionsBuilder<AppDbContext>();

            optionsBuilder.UseSqlServer(AppSettings.ConnectionString);
            return optionsBuilder.Options;
        }

        public async Task<List<Account>> GetAccounts()
        {
            using (_context = new AppDbContext(GetAllOptions()))
            {
                try
                {
                    var accounts = await _context.Accounts.ToListAsync();
                    return accounts;
                }
                catch (Exception)
                {
                    throw;
                }
            }
        }
    }
}
