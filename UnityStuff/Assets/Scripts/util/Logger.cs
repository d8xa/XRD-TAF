using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;

namespace util
{
    public class Logger
    {
        /**
         * Structure for simple log entries. Contains timestamp and message.
         * Can later be expanded with additional fields.
         */
        private readonly struct LogEntry
        {
            public LogEntry(DateTime timestamp, string message)
            {
                this.timestamp = timestamp;
                this.message = message;
            }

            public DateTime timestamp { get; }
            public string message { get; }

            public override string ToString() => $"{timestamp}:\t{message}";
        }
        
        
        private readonly List<LogEntry> _logEntries;
        private string _defaultFolder = "Logs";
        
        public Logger()
        {
            _logEntries = new List<LogEntry>();
        }

        public void Log(string message)
        {
            _logEntries.Add(new LogEntry(DateTime.Now, message));
        }

        public void WriteToDebug()
        {
            _logEntries.ForEach(entry => Debug.Log(entry.ToString()));
        }

        public void WriteToFile(string filePath)
        {
            if (File.Exists(filePath))
            {
                var fileName = Path.GetFileNameWithoutExtension(filePath);
                var folderPath = Path.GetDirectoryName(filePath) ?? _defaultFolder;
                var fileExtension = Path.GetExtension(filePath);
                var number = 1;

                var regex = Regex.Match(fileName, @"^(.+) \((\d+)\)$");

                if (regex.Success)
                {
                    fileName = regex.Groups[1].Value;
                    number = int.Parse(regex.Groups[2].Value);
                }

                do
                {
                    number++;
                    var newFileName = $"{fileName} ({number}){fileExtension}";
                    filePath = Path.Combine(folderPath, newFileName);
                }
                while (File.Exists(filePath));
            }

            using (var fileStream = File.Create(filePath))
            using (var buffered = new BufferedStream(fileStream))
            using (var writer = new StreamWriter(buffered))
            {
                foreach (var entry in _logEntries)
                {
                    writer.WriteLine(entry);
                }
            }
        }

        public void WriteToFile()
        {
            var fileName = $"{DateTime.Now.ToShortDateString()} Logfile.txt";
            WriteToFile(Path.Combine(_defaultFolder, fileName));
        }

        public void AppendToFile(string filePath)
        {
            if (File.Exists(filePath))
            {
                using (var fileStream = File.Create(filePath))
                using (var buffered = new BufferedStream(fileStream))
                using (var writer = new StreamWriter(buffered))
                {
                    _logEntries.ForEach(entry => writer.WriteLine(entry));
                }
            }
            else WriteToFile(filePath);
        }

        public void AppendToFile()
        {
            var fileName = $"{DateTime.Now.ToShortDateString()} Logfile.txt";
            AppendToFile(Path.Combine(_defaultFolder, fileName));
        }

        public void Flush() => _logEntries.Clear();
    }
}