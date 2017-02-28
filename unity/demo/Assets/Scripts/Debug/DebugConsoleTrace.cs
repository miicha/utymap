using System;
using System.Text;
using Assets.Scripts.Debug.Utils;
using UnityEngine;
using UnityEngine.UI;
using UtyMap.Unity.Infrastructure.Diagnostic;
using UtyRx;
using Object = UnityEngine.Object;

namespace Assets.Scripts.Debug
{
    public class DebugConsoleTrace : DefaultTrace
    {
        private DebugConsole _console;
        private Text _uiText;

        public void SetUIText(Text uiText)
        {
            _uiText = uiText;
        }

        public void SetConsole(DebugConsole console)
        {
            if (_console != null)
                Object.Destroy(_console);

            _console = console;
        }

        protected override void WriteRecord(RecordType type, string category, string message, Exception exception)
        {
            var logMessage = ToLogMessage(type, category, message, exception);

            if (_console != null)
                _console.LogMessage(logMessage);

            if (_uiText != null)
                Scheduler.MainThread.Schedule(() => LogUiTextMessage(type, category, message, exception));

            switch (type)
            {
                case RecordType.Error:
                    UnityEngine.Debug.LogError(logMessage.Text);
                    break;
                case RecordType.Warn:
                    UnityEngine.Debug.LogWarning(logMessage.Text);
                    break;
                default:
                    UnityEngine.Debug.Log(logMessage.Text);
                    break;
            }
        }

        private ConsoleMessage ToLogMessage(RecordType type, string category, string text, Exception exception)
        {
            switch (type)
            {
                case RecordType.Error:
                    return ConsoleMessage.Error(String.Format("[{0}] {1}:{2}. Exception: {3}", type, category, text, exception));
                case RecordType.Warn:
                    return ConsoleMessage.Warning(String.Format("[{0}] {1}:{2}", type, category, text));
                case RecordType.Info:
                    var lines = text.Trim('\n').Split('\n');
                    var output = new StringBuilder();
                    output.Append(String.Format("[{0}] {1}:", type, category));
                    for (var i = 0; i < lines.Length; i++)
                        output.AppendFormat("{0}{1}", lines[i], i != lines.Length - 1 ? "\n" : "");
                    return ConsoleMessage.Info(output.ToString());
                default:
                    return ConsoleMessage.Debug(String.Format("[{0}] {1}: {2}", type, category, text));
            }
        }

        private void LogUiTextMessage(RecordType type, string category, string text, Exception exception)
        {
            switch (type)
            {
                case RecordType.Error:
                    _uiText.color = Color.red;
                    _uiText.text = String.Format("[{0}] {1}:{2}. Exception: {3}", type, category, text, exception);
                    return;
                case RecordType.Warn:
                    _uiText.color = Color.yellow;
                    break;
                case RecordType.Info:
                    _uiText.color = Color.blue;
                    break;
                default:
                    _uiText.color = Color.white;
                    break;
            }
            _uiText.text = String.Format("[{0}] {1}:{2}", type, category, text);
        }
    }
}