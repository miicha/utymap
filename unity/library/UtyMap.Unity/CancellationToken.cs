using System.Runtime.InteropServices;

namespace UtyMap.Unity
{
    /// <summary> Cancellation token is used to cancel processing in native code. </summary>
    [StructLayout(LayoutKind.Sequential)]
    public class CancellationToken
    {
        internal int IsCancelled;

        internal void SetCancelled(bool isCancelled)
        {
            IsCancelled = (byte) (isCancelled ? 1 : 0);
        }
    }
}
