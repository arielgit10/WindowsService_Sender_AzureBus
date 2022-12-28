

namespace WorkerService_Sender.Repository
{
    public interface IServerRepository
    {
        Task<List<Account>> GetAccounts();
    }
}
