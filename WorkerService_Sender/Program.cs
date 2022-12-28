using WorkerService_Sender;
using WorkerService_Sender.Repository;
using Microsoft.EntityFrameworkCore;



IHost host = Host.CreateDefaultBuilder(args)
     .UseWindowsService(options =>
     {
         options.ServiceName = "Sender Service";
         })

    .ConfigureServices((hostContext, services) =>
                   {
       
        IConfiguration configuration = hostContext.Configuration;

        AppSettings.ConnectionString = configuration.GetConnectionString("DefaultConnection");

        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();

        optionsBuilder.UseSqlServer(AppSettings.ConnectionString);

        services.AddScoped<AppDbContext>(db => new AppDbContext(optionsBuilder.Options));

        services.AddHostedService<Worker>();


    })
    .Build();



await host.RunAsync();
