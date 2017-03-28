using System;
using UtyMap.Unity.Infrastructure.Diagnostic;

namespace Assets.Scripts.Environment
{
    internal sealed class UnityLogTrace : DefaultTrace
    {
        protected override void OnWriteRecord(RecordType type, string category, string message, Exception exception)
        {
            switch (type)
            {
                case RecordType.Error:
                    UnityEngine.Debug.LogError(String.Format("[{0}] {1}:{2}. Exception: {3}", type, category, message, exception));
                    break;
                case RecordType.Warn:
                    UnityEngine.Debug.LogWarning(String.Format("[{0}] {1}:{2}", type, category, message));
                    break;
                default:
                    UnityEngine.Debug.Log(String.Format("[{0}] {1}: {2}", type, category, message));
                    break;
            }
        }
    }
}
