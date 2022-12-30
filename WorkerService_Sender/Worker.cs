using Azure.Storage.Queues;
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

        public Worker(ILogger<Worker> logger, IServerRepository serverRepository)
        {
            _logger = logger;
            _serverRepository = serverRepository;
            _queueClient = new QueueClient(AppSettings.QueueConnection, "cola1");
            _period = TimeSpan.FromMinutes(AppConfiguration.IntervalMinutes);
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
                    await ReadDatabase();
                }
                catch (Exception ex)
                {
                    _logger.LogInformation(
                        $"Failed to execute Periodic Service with exception message {ex.Message}.");
                }
            }
        }
        private async Task ReadDatabase()
        {
            _logger.LogInformation("Reading database: {time}", DateTimeOffset.Now);
            var accounts = await _serverRepository.GetAccounts();
            if (accounts is not null)
            {
                await SendToAzureQueue(accounts);
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