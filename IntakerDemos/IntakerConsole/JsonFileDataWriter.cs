using System;
using System.IO;

namespace IntakerConsole
{
    public class JsonFileDataWriter : IDisposable
    {
        private readonly StreamWriter _streamWriter;
        private int _addedLines;

        public JsonFileDataWriter(string path)
        {
            _streamWriter = new StreamWriter(path);
            _streamWriter.WriteLine($"[");
        }

        public void Dispose()
        {
            _streamWriter.WriteLine("");
            _streamWriter.WriteLine($"]");
            _streamWriter.Dispose();
            GC.SuppressFinalize(this);
        }

        public void Write(string jsonLine)
        {
            if (string.IsNullOrEmpty(jsonLine))
            {
                return;
            }

            if (_addedLines > 0)
            {
                _streamWriter.WriteLine(",");
            }

            _streamWriter.Write($"{jsonLine}");
            _addedLines++;
        }
    }
}
