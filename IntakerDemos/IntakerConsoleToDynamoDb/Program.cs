using Amazon.DynamoDBv2;
using Intaker.Repository.DynamoDb;
using IntakerConsole.Shared.Logger;
using IntakerConsoleToDynamoDb.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Diagnostics;

namespace IntakerConsoleToDynamoDb
{
    class Program
    {
        static void Main(string[] args)
        {
            var host = Host.CreateDefaultBuilder(args)
                .ConfigureServices(ConfigureServices)
                .Build();

            var consoleWorker = ActivatorUtilities.CreateInstance<ConsoleWorker>(host.Services);

            var sw = new Stopwatch();
            sw.Start();
            consoleWorker.Run();
            sw.Stop();

            Console.WriteLine($"Elapsed: {sw.Elapsed}");
        }

        private static AmazonDynamoDBClient BuildAmazonDynamoDBClient(IConfiguration configuration)
        {
            var awsDymanoDbClientConfig = configuration.GetSection(ApplicationConstants.CONFIG_SECTION_AWS_DYNAMODB_CLIENT).Get<AwsDymanoDbClient>();

            if (awsDymanoDbClientConfig.UseDynamoDbLocal)
            {
                var clientConfig = new AmazonDynamoDBConfig { ServiceURL = awsDymanoDbClientConfig.ServiceURL };
                return new AmazonDynamoDBClient(clientConfig);
            }
            else
            {
                return new AmazonDynamoDBClient();
            }
        }

        private static void ConfigureServices(HostBuilderContext hostContext, IServiceCollection services)
        {
            var logger = new ApplicationLogger();
            services.AddSingleton<IApplicationLogger>(logger);

            var dbClient = BuildAmazonDynamoDBClient(hostContext.Configuration);

            var intakerConsoleAppConfig = hostContext.Configuration.GetSection(ApplicationConstants.CONFIG_SECTION_INTAKER_CONSOLE_APP).Get<IntakerConsoleAppConfig>();
            var dynamoDbIntakerRepositoryConfig = new DynamoDbIntakerRepositoryConfig { TableName = intakerConsoleAppConfig.DynamoDbTableName };
            var dynamoDbIntakerRepository = new DynamoDbIntakerRepository(dbClient, dynamoDbIntakerRepositoryConfig);

            services.AddSingleton<IIntakerRepository>(dynamoDbIntakerRepository);
        }
    }
}
