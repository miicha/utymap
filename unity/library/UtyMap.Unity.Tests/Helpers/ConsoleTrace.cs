using System;
using UtyMap.Unity.Infrastructure.Diagnostic;

namespace UtyMap.Unity.Tests.Helpers
{
    public class ConsoleTrace: DefaultTrace
    {
        public ConsoleTrace()
            : base(RecordType.Debug | RecordType.Info | RecordType.Warn | RecordType.Error)
        {
        }

        protected override void OnWriteRecord(RecordType type, string category, string message, Exception exception)
        {
            Console.WriteLine("[{0}] {1}: {2}{3}", type, category, message,
                (exception == null? "": " .Exception:" + exception));
        }
    }
}
