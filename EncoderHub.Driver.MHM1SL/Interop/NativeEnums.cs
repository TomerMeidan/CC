// ── Local redeclaration ───────────────────────────────────────────────────────
// The iC-Haus-Library NuGet package (v1.0.2.6) ships only PZ-series types.
// The MHM-series enumerations are not exported from the package, so they are
// declared here verbatim from the original iC-Haus-Biss source so that
// MHM1SLWrapper and MHM1SLDriverConfiguration can compile without changes.
// ─────────────────────────────────────────────────────────────────────────────

namespace iC_Haus_Library.Interop.iC_MHM_Series
{
    public static class NativeEnums
    {
        /// <summary>Error codes returned by the MHM DLL (Table 15, Page 39-40).</summary>
        public enum MHM_ErrorEnum : uint
        {
            eMHM_OK = 0,
            eMHM_INVALID_HANDLE,
            eMHM_INTERFACEDRIVER_NOT_FOUND,
            eMHM_INTERFACE_NOT_FOUND,
            eMHM_INVALID_INTERFACE,
            eMHM_INVALID_PARAMETER,
            eMHM_INVALID_ADDRESS,
            eMHM_INVALID_VALUE,
            eMHM_USB_ERROR,
            eMHM_FILE_NOT_FOUND,
            eMHM_INVALID_FILE,
            eMHM_VERIFY_FAILED,
            eMHM_MASTERCOMM_FAILED,
            eMHM_BISSCOMM_FAILED,
            eMHM_SPICOMM_FAILED,
            eMHM_USB_HIGHSPEED_WARNING,
            eMHM_INVALID_BISSMASTER,
            eMHM_NO_INTERFACE_SELECTED,
            eMHM_READPARAM_SSI,
            eMHM_SPI_ERROR,
            eMHM_SPI_DISMISS,
            eMHM_SPI_FAIL,
            eMHM_SPI_BUSY_TIMEOUT,
            eMHM_FILE_ACCESS_DENIED,
            eMHM_INVALID_CONFIGURATION,
            eMHM_INVALID_EDS,
            eMHM_INVALID_EDS_CHKSUM,
            eMHM_EDS_CORRECTED,
            eMHM_CORRECTED_INVALID_VALUE,
            eMHM_BISS_REGERROR,
            eMHM_EDS_UNDEFINED,
            eMHM_FILESIZE_WARNING,
            eMHM_SLOW_ROTATION,
            eMHM_FAST_ROTATION,
            eMHM_GAIN_LIMIT,
            eMHM_ACQUISITION_FAILED,
            eMHM_USB_DATA_LOSS,
            eMHM_INTERNAL_CALIB_ERROR,
            eMHM_OFFSET_LIMIT,
            eMHM_CALIBRATION_FAILED,
            eMHM_I2C_COMM_FAILED,
            eMHM_GAINCOSCORR_LIMIT,
            eMHM_HARMCAL_LIMIT,
            eMHM_BAD_CAL_DATA,
            eMHM_TIMEOUT,
            eMHM_ADAPTER_FIRMWARE_INCOMPATIBLE
        }

        /// <summary>USB adapter types supported by the MHM DLL (Table 1, Page 6).</summary>
        public enum MHM_InterfaceEnum : uint
        {
            eMHM_NO_INTERFACE = 0,
            eMHM_MB3U_SPI,
            eMHM_MB3U_BISS,
            eMHM_MB4U,
            eMHM_MB5U,
            eMHM_MB8U_SPI,
            eMHM_MB8U_BISS
        }

        /// <summary>Error type categories (Table 5, Page 16).</summary>
        public enum MHM_ErrorTypeEnum : uint
        {
            eMHM_NONE = 0,
            eMHM_HINT,
            eMHM_WARNING,
            eMHM_PROGRAMMING_ERROR,
            eMHM_OPERATING_ERROR,
            eMHM_COMMUNICATION_ERROR
        }

        /// <summary>EEPROM areas selectable when calling MHM_WriteEeprom (Table 11, Page 33).</summary>
        public enum MHM_E2PAreaEnum : uint
        {
            /// <summary>iC-MHM only (Bank1; Adr. 0x10..0x23)</summary>
            eMHM_E2P_CFG_MHM = 0,
            /// <summary>Multiturn only (Bank1; Adr. 0x00..0x0F)</summary>
            eMHM_E2P_CFG_MT,
            /// <summary>EDS only</summary>
            eMHM_E2P_EDS,
            /// <summary>All</summary>
            eMHM_E2P_ALL,
            /// <summary>iC-MHM configuration without offsets (Bank1; Adr. 0x10..0x1C)</summary>
            eMHM_E2P_CFG_MHM_NO_OFFS
        }
    }
}
