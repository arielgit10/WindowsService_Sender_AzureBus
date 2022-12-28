using Azure.Messaging.ServiceBus;
using System.Text.Json;
using WorkerService_Sender.Repository;

namespace WorkerService_Sender
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private ServiceBusClient client;
        private ServiceBusSender sender;
        private int numOfMessages = 3;

        public Worker(ILogger<Worker> logger)
        {
            _logger = logger;
            this.numOfMessages = 3;
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await ReadDatabase();
                await Task.Delay(TimeSpan.FromSeconds(9), stoppingToken);
            }
        }
        private async Task ReadDatabase()
        {
            IServerRepository dbServer = new ServerRepository();

            var accounts = await dbServer.GetAccounts();
            if (accounts is not null)
            {
                await SendToAzureQueue(accounts);
            }
            _logger.LogInformation("Windows Service running at: {time}", DateTimeOffset.Now);
        }
        private async Task SendToAzureQueue(List<Account> listAccounts)
        {
            var clientOptions = new ServiceBusClientOptions
            {
                TransportType = ServiceBusTransportType.AmqpWebSockets
            };
            client = new ServiceBusClient(AppSettings.QueueConnection, clientOptions);
            sender = client.CreateSender("cola1");
            using ServiceBusMessageBatch messageBatch = await sender.CreateMessageBatchAsync();

            for (int i = 1; i <= numOfMessages; i++)
            {
                var body = JsonSerializer.Serialize(listAccounts);
                if (!messageBatch.TryAddMessage(new ServiceBusMessage(body)))
                {

                    throw new Exception($"The message {i} could not be sent.");
                }
            }
            try
            {
                await sender.SendMessagesAsync(messageBatch);
                _logger.LogInformation($"A batch of {numOfMessages} messages has been published to the queue.");
            }
            finally
            {
                await sender.DisposeAsync();
                await client.DisposeAsync();
            }
            Console.WriteLine("Press any key to end the application");
            Console.ReadKey();
        }

        private void DisplayAccountInformation(List<Account> accounts)
        {
            accounts?.ForEach(account =>
            {
                _logger.LogInformation($"Account Information:\n {account.AccountId} \t {account.Cbu}" +
                    $"\t {account.Alias} \t {account.Balance}");
            });
        }

    }
}