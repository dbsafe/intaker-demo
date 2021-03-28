using Amazon.Lambda.Core;

namespace IntakerAWSLambda
{
    public interface IFileProcessorLogger
    {
        void Log(string message);
    }

    public class FileProcessorLogger : IFileProcessorLogger
    {
        public void Log(string message)
        {
            LambdaLogger.Log(message);
        }
    }
}
