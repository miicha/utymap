using System;

namespace UtyMap.Unity.Infrastructure.Diagnostic
{
    /// <summary> Default trace. Provides method to override. </summary>
    public class DefaultTrace : ITrace
    {
        /// <summary>  Defines trace record types. </summary>
        [Flags]
        public enum RecordType
        {
            None = 0,
            Debug = 1 << 0,
            Info = 1 << 1,
            Warn = 1 << 2,
            Error = 1 << 3
        }

        public DefaultTrace(RecordType level = RecordType.Error)
        {
            Level = level;
        }

        #region ITrace implementation

        public RecordType Level { get; set; }

        /// <inheritdoc />
        public void Debug(string category, string message)
        {
            WriteRecord(RecordType.Debug, category, message, null);
        }

        /// <inheritdoc />
        public void Debug(string category, string format, string arg1)
        {
            WriteRecord(RecordType.Debug, category, format, arg1, null);
        }

        /// <inheritdoc />
        public void Debug(string category, string format, string arg1, string arg2)
        {
            WriteRecord(RecordType.Debug, category, format, arg1, arg2, null);
        }

        /// <inheritdoc />
        public void Info(string category, string message)
        {
            WriteRecord(RecordType.Info, category, message, null);
        }

        /// <inheritdoc />
        public void Info(string category, string format, string arg1)
        {
            WriteRecord(RecordType.Info, category, format, arg1, null);
        }

        /// <inheritdoc />
        public void Info(string category, string format, string arg1, string arg2)
        {
            WriteRecord(RecordType.Info, category, format, arg1, arg2, null);
        }

        /// <inheritdoc />
        public void Warn(string category, string message)
        {
            WriteRecord(RecordType.Warn, category, message, null);
        }

        /// <inheritdoc />
        public void Warn(string category, string format, string arg1)
        {
            WriteRecord(RecordType.Warn, category, format, arg1, null);
        }

        /// <inheritdoc />
        public void Warn(string category, string format, string arg1, string arg2)
        {
            WriteRecord(RecordType.Warn, category, format, arg1, arg2, null);
        }

        /// <inheritdoc />
        public void Error(string category, Exception ex, string message)
        {
            WriteRecord(RecordType.Error, category, message, ex);
        }

        /// <inheritdoc />
        public void Error(string category, Exception ex, string format, string arg1)
        {
            WriteRecord(RecordType.Error, category, format, arg1, ex);
        }

        /// <inheritdoc />
        public void Error(string category, Exception ex, string format, string arg1, string arg2)
        {
            WriteRecord(RecordType.Error, category, format, arg1, arg2, ex);
        }

        #endregion

        private void WriteRecord(RecordType type, string category, string message, Exception exception)
        {
            if ((type & Level) == type)
                OnWriteRecord(type, category, message, exception);
        }

        private void WriteRecord(RecordType type, string category, string format, string arg1, Exception exception)
        {
            if ((type & Level) == type)
                WriteRecord(type, category, String.Format(format, arg1), exception);
        }

        private void WriteRecord(RecordType type, string category, string format, string arg1, string arg2,
            Exception exception)
        {
            if ((type & Level) == type)
                WriteRecord(type, category, String.Format(format, arg1, arg2), exception);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            Dispose(true);
        }

        /// <summary> Writes record to trace. </summary>
        /// <param name="type">Record type.</param>
        /// <param name="category">Category.</param>
        /// <param name="message">Message.</param>
        /// <param name="exception">Exception.</param>
        protected virtual void OnWriteRecord(RecordType type, string category, string message, Exception exception) { }

        /// <inheritdoc />
        protected virtual void Dispose(bool disposing) { }
    }
}
