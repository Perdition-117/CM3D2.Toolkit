// --------------------------------------------------
// CM3D2.Toolkit - FileEntry.cs
// --------------------------------------------------

using System.Runtime.InteropServices;

namespace CM3D2.Toolkit.Guest4168Branch.Arc.Native
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct FileEntry
    {
        public ulong Hash;
        public long Offset;
    }
}
