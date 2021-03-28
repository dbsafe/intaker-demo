using Amazon.Lambda.Core;
using Amazon.Lambda.S3Events;
using Amazon.S3;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using System;
using System.Threading.Tasks;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace IntakerAWSLambda
{
    public class Function : BaseLambdaFunction
    {
        protected override void ConfigureServices(HostBuilderContext hostContext, IServiceCollection services)
        {
            services.AddSingleton<IFileProcessorLogger>(new FileProcessorLogger());
            services.AddSingleton<FileProcessor>();
        }

        public async Task FunctionHandler(S3Event input, ILambdaContext context)
        {
            Log($"CONTEXT: {JsonConvert.SerializeObject(context)}");
            Log($"INPUT: {JsonConvert.SerializeObject(input)}");

            var fileProcessor = CreateInstance<FileProcessor>();

            foreach(var record in input.Records)
            {
                ValidateEvent(record.EventName.Value);
                await fileProcessor.ProcessFileAsync(record);
            }
        }

        private void ValidateEvent(EventType eventType)
        {
            if (eventType.Value != "ObjectCreated:Put")
            {
                throw new Exception($"Unexpected event type '{eventType.Value}'");
            }
        }

        private void Log(string message)
        {
            LambdaLogger.Log(message);
        }
    }
}
