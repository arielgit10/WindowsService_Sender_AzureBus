using Azure.Storage.Queues;
using Microsoft.Extensions.Caching.Memory;
using System.Text.Json;
using WorkerService_Sender.Models;
using WorkerService_Sender.Repository;

namespace WorkerService_Sender
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly TimeSpan _period;
        private readonly IServerRepository _serverRepository;
        private readonly QueueClient _queueClient;
        private readonly IMemoryCache _memoryCache;
        private const string accountsListCacheKey = "accountList";
        private List<Account> _newAccounts;
        private List<Guid> _accountsId;

        public Worker(ILogger<Worker> logger, IServerRepository serverRepository, IMemoryCache memoryCache)
        {
            _logger = logger;
            _serverRepository = serverRepository;
            _queueClient = new QueueClient(AppSettings.QueueConnection, "cola1");
            _period = TimeSpan.FromMinutes(AppConfiguration.IntervalMinutes);
            _memoryCache = memoryCache;
            _newAccounts = new List<Account>();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using PeriodicTimer timer = new PeriodicTimer(_period);
            while (!stoppingToken.IsCancellationRequested &&
                  await timer.WaitForNextTickAsync(stoppingToken))
            {
                try
                {
                    _logger.LogInformation("Windows Service running at: {time}", DateTimeOffset.Now);
                    await ProcessingData();
                }
                catch (Exception ex)
                {
                    _logger.LogInformation(
                        $"Failed to execute Periodic Service with exception message {ex.Message}.");
                }
            }
        }
        private async Task ProcessingData()
        {
            var accounts = await ReadDataBase();
            if (accounts is not null)
            {
                SetAccountsIdFromCache(accounts);
            }
            if (_newAccounts.Any())
            {
                await SendToAzureQueue(_newAccounts);
                _newAccounts.Clear();
            }           
        }

        private async Task<List<Account>> ReadDataBase()
        {
            _logger.LogInformation("Reading database: {time}", DateTimeOffset.Now);
            var accounts = await _serverRepository.GetAccounts();
            return accounts;
        }
        public void SetAccountsIdFromCache(List<Account> listAccounts)
        {
            if (!_memoryCache.TryGetValue(accountsListCacheKey, out _accountsId))
            {
                _accountsId = new List<Guid>();
                _memoryCache.Set(accountsListCacheKey, _accountsId, TimeSpan.FromMinutes(15));
            }

            _newAccounts = listAccounts.Where(account => !_accountsId.Contains(account.AccountId)).ToList();
            
            foreach (var account in _newAccounts)
            {
                if (!_accountsId.Contains(account.AccountId))
                {
                    _accountsId.Add(account.AccountId);
                }
            }
        }

        private async Task SendToAzureQueue(List<Account> listAccounts)
        {
            var body = JsonSerializer.Serialize(listAccounts);
            if (_queueClient.Exists())
            {
                _logger.LogInformation("Sending to Azure: {time}", DateTimeOffset.Now);
                _queueClient.SendMessage(body);
                _logger.LogInformation("A message has been published to the queue at time: {time}", DateTimeOffset.Now);
            }
        }
    } 
}