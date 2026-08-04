using static iC_Haus_Library.Interop.iC_MHM_Series.NativeEnums;

namespace EncoderHub.Driver.MHM1SL
{
    /// <summary>
    /// All settings required to open an MHM1SL encoder session.
    /// <para>
    /// Kept inside the driver project so the GUI and Abstractions layer
    /// never need to import MHM-specific enums such as
    /// <see cref="MHM_InterfaceEnum"/>.
    /// </para>
    /// </summary>
    public class MHM1SLDriverConfiguration
    {
        /// <summary>Friendly label for this device instance.</summary>
        public string DeviceName { get; set; } = "MHM-Default";

        /// <summary>
        /// Encoder singleturn bit resolution. The MHM_Demo project that ships with
        /// the iC-Haus library uses <c>14</c> bits, which matches the standard
        /// iC-MHM-1SL hardware singleturn count (16384 counts/turn).
        /// </summary>
        public int Resolution { get; set; } = 14;

        /// <summary>USB adapter type — MB3U (SPI/BiSS), MB4U, MB5U, MB8U.</summary>
        public MHM_InterfaceEnum InterfaceType { get; set; } = MHM_InterfaceEnum.eMHM_MB5U;

        /// <summary>
        /// Optional serial-number suffix to disambiguate multiple adapters.
        /// Leave empty when only one adapter is present.
        /// </summary>
        public string InterfaceOption { get; set; } = "";

        /// <summary>
        /// When <c>true</c>, the driver will attempt to switch the device from SSI
        /// to BiSS mode during connection.
        /// </summary>
        public bool ForceBiSSMode { get; set; } = true;
    }
}
