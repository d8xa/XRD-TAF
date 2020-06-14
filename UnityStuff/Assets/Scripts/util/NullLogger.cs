namespace util
{
    public class NullLogger : Logger
    {
        public NullLogger() {}

        public new void Log(EventType eventType, string message, bool printDebug = false) {}

        public new void PrintToDebug(LogLevel logLevel) {}
        
        public new void WriteToFile(string filePath) {}
        
        public new void AppendToFile(string filePath) {}
    }
}