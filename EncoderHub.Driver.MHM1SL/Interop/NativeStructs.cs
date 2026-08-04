// ── Local redeclaration ───────────────────────────────────────────────────────
// The iC-Haus-Library NuGet package (v1.0.2.6) ships only PZ-series types.
// MHM_ReadSensStruct is not exported from the package, so it is declared here
// verbatim from the original iC-Haus-Biss source (NativeStructs.cs).
// ─────────────────────────────────────────────────────────────────────────────

using System;
using System.Runtime.InteropServices;

namespace iC_Haus_Library.Interop.iC_MHM_Series
{
    public static class NativeStructs
    {
        /// <summary>
        /// Sensor data returned by <c>MHM_ReadSens</c> (Table 4, Page 12).
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct MHM_ReadSensStruct
        {
            /// <summary>Raw data bits 63..32</summary>
            public UInt32 ulHighData;
            /// <summary>Raw data bits 31..0</summary>
            public UInt32 ulLowData;
            /// <summary>Total data length in bits</summary>
            public UInt32 ulDataLength;
            /// <summary>Multiturn position value</summary>
            public UInt32 ulMultiturn;
            /// <summary>Singleturn position value</summary>
            public UInt32 ulSingleturn;
            /// <summary>Error bit (1 = error active)</summary>
            public UInt32 ulError;
            /// <summary>Warning bit (1 = warning active)</summary>
            public UInt32 ulWarning;
            /// <summary>Sign-of-life counter value</summary>
            public UInt32 ulLifeCounter;
            /// <summary>Register content at address 0x70 (status register)</summary>
            public UInt32 ulStatusRegister;
            /// <summary>Register content at address 0x71 (port register)</summary>
            public UInt32 ulPortRegister;
            /// <summary>Register content at address 0x72 (gain register)</summary>
            public UInt32 ulGainRegister;
        }
    }
}
