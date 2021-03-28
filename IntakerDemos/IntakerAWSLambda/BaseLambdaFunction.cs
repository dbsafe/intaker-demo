using Amazon.Lambda.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;

namespace IntakerAWSLambda
{
    public abstract class BaseLambdaFunction
    {
        protected IServiceProvider ServiceProvider { get; }

        protected virtual void ConfigureServices(HostBuilderContext hostContext, IServiceCollection services) { }

        public BaseLambdaFunction()
        {
            var host = Host.CreateDefaultBuilder()
                .ConfigureServices(ConfigureServices)
                .Build();

            ServiceProvider = host.Services;
        }

        protected T CreateInstance<T>()
        {
            return ActivatorUtilities.CreateInstance<T>(ServiceProvider);
        }
    }
}
