using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

namespace util
{
    public class Logger
    {
        
        public enum LogLevel : uint
        {
            All = 70,              // 1 + any event.
            Verbose = 60,          // 2 + statements.
            Info = 50,             // 4 + initializer method start- and end.
            MethodLevel = 40,      // 5 + method start- and end.
            Debug = 30,            // 3 + object creation- and destruction.
            Warning = 20,          // 6 + warnings.
            Error = 10,            // only errors.
            None = 00
        }

        public enum EventType : uint
        {
            Error = 9, 
            Warning = 19, 
            Method = 29,                 // method start- or end. 
            Class = 39,                  // constructor.
            InitializerMethod = 49,      // every event inside initializer methods of class, unless events are declared as lower.
            ShaderInteraction = 48,
            Data = 59,                   // data retrieval, assignment, allocation, etc.
            Step = 69,                    // a logical step or group of the code. 
            Performance = 68
        }
        
        /**
         * Structure for simple log entries. Contains timestamp and message.
         * Can later be expanded with additional fields.
         */
        private readonly struct LogEntry
        {
            public LogEntry(EventType eventType, DateTime timestamp, string message)
            {
                Timestamp = timestamp;
                Message = message;
                EventType = eventType;
            }

            private DateTime Timestamp { get; }
            private string Message { get; }
            public EventType EventType { get; }

            public override string ToString()
            {
                return $"{Timestamp:HH:mm:ss.ffffff}:\t{Message}";
            }
        }
        
        
        private readonly List<LogEntry> _logEntries;
        private string _defaultFolder = "Logs";
        private LogLevel PrintLevel { get; set; } = LogLevel.All;
        private LogLevel WriteLevel { get; set; } = LogLevel.All;

        public Logger SetPrintLevel(LogLevel logLevel)
        {
            PrintLevel = logLevel;
            return this;
        }

        public Logger SetWriteLevel(LogLevel logLevel)
        {
            WriteLevel = logLevel;
            return this;
        }

        public Logger()
        {
            _logEntries = new List<LogEntry>();
        }

        public void Log(EventType eventType, string message, bool printDebug = false)
        {
            var entry = new LogEntry(eventType, DateTime.Now, message);
            _logEntries.Add(entry);
            if (printDebug || (int) eventType <= (int) PrintLevel) 
                Debug.Log(entry.ToString());
        }

        private List<LogEntry> Restrict(LogLevel logLevel)
        {
            return _logEntries
                .Where(e => (int) e.EventType <= (int) logLevel)
                .ToList();
        }
        
        private List<LogEntry> Filter(List<EventType> eventTypes)
        {
            return _logEntries
                .Where(e => eventTypes.Contains(e.EventType))
                .ToList();
        }

        public void PrintToDebug(LogLevel logLevel)
        {
            Restrict(logLevel).ForEach(entry => Debug.Log(entry.ToString()));
        }

        public void PrintToDebug()
        {
            PrintToDebug(PrintLevel);
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