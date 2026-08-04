using static iC_Haus_Library.Interop.iC_PZ_Series.NativeEnums;

namespace EncoderHub.Driver.PZ1SL
{
    /// <summary>
    /// All settings required to open a PZ1SL encoder session.
    /// <para>
    /// Deliberately kept inside the driver project so the GUI and the Abstractions
    /// layer never need to import PZ-specific enums such as
    /// <see cref="PZ_InterfaceEnum"/>.
    /// </para>
    /// </summary>
    public class PZ1SLDriverConfiguration
    {
        /// <summary>Friendly label for this device instance.</summary>
        public string DeviceName { get; set; } = "PZ-Default";

        /// <summary>
        /// Encoder singleturn bit resolution. iC-PZ devices typically report 24-bit
        /// singleturn data via BiSS, so 24 is the correct default.
        /// </summary>
        public int Resolution { get; set; } = 24;

        /// <summary>USB adapter type — MB3U, MB4U, MB5U, etc.</summary>
        public PZ_InterfaceEnum InterfaceType { get; set; } = PZ_InterfaceEnum.ePZ_MB5U;

        /// <summary>
        /// Optional serial-number suffix used to disambiguate multiple adapters of the
        /// same type. Leave empty when only one adapter is present.
        /// </summary>
        public string InterfaceName { get; set; } = "";

        /// <summary>
        /// When <c>true</c> the driver will attempt to switch the device from SSI to
        /// BiSS mode during connection.
        /// </summary>
        public bool ForceBiSSMode { get; set; } = true;
    }
}
