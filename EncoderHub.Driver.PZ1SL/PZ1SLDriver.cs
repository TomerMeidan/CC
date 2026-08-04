using EncoderHub.Abstractions;
using iC_Haus_Library.Controllers;
using iC_Haus_Library.Controllers.Models;
using OperationResultManager;
using System;
using System.Threading;
using System.Threading.Tasks;
using static iC_Haus_Library.Interop.iC_PZ_Series.NativeEnums;

namespace EncoderHub.Driver.PZ1SL
{
    /// <summary>
    /// Translator: wraps <see cref="PZ1SLController"/> and exposes it as a standard
    /// <see cref="IEncoderDriver"/>.
    /// <para>
    /// All PZ-specific details — BiSS handshake, P/Invoke calls, interface enums — are
    /// isolated inside this class. The GUI never sees any of it.
    /// </para>
    /// </summary>
    public sealed class PZ1SLDriver : IEncoderDriver
    {
        // ── Private State ─────────────────────────────────────────────────────────

        private readonly PZ1SLDriverConfiguration _config;
        private PZ1SLController _controller;
        private bool _disposed;

        // ── IEncoderDriver: Identity ──────────────────────────────────────────────

        public string DriverName        => "iC-Haus PZ1SL";
        public string DriverDescription => "BiSS Position Encoder via USB Adapter (MB3U / MB4U / MB5U)";

        // ── IEncoderDriver: State ─────────────────────────────────────────────────

        public bool IsConnected => _controller?.IsConnected ?? false;

        /// <inheritdoc/>
        public int Resolution
        {
            get => _config.Resolution;
            set => _config.Resolution = value;
        }

        public event EventHandler<bool> ConnectionStateChanged;

        // ── Construction ──────────────────────────────────────────────────────────

        /// <summary>Creates a driver using default PZ1SL settings.</summary>
        public PZ1SLDriver() : this(new PZ1SLDriverConfiguration()) { }

        /// <summary>Creates a driver with explicit configuration.</summary>
        public PZ1SLDriver(PZ1SLDriverConfiguration config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        // ── IEncoderDriver: Commands ──────────────────────────────────────────────

        public OperationResult<string> Connect()
        {
            if (IsConnected)
                return OperationResult.SetSuccessful("Already connected.");

            // Translate from the driver-agnostic call into PZ-specific configuration.
            // This is the core job of a Translator: adapt the standard command.
            var nativeConfig = new PZ1SLConfiguration
            {
                DeviceName    = _config.DeviceName,
                Resolution    = _config.Resolution,
                InterfaceType = _config.InterfaceType,
                InterfaceName = _config.InterfaceName,
                ForceBiSSMode = _config.ForceBiSSMode
            };

            _controller = new PZ1SLController(nativeConfig);
            _controller.ConnectionStateChanged += OnControllerStateChanged;

            return _controller.Connect();
        }

        public OperationResult<string> Disconnect()
        {
            if (!IsConnected)
                return OperationResult.SetSuccessful("Already disconnected.");

            return _controller.Disconnect();
        }

        /// <summary>
        /// Soft-reset: disconnect cleanly, then reconnect.
        /// Override this method in a future subclass if hardware-level reset is needed.
        /// </summary>
        public OperationResult<string> Reset()
        {
            var disconnectResult = Disconnect();
            if (!disconnectResult.IsSuccess)
                return OperationResult.SetError(
                    $"Reset failed during disconnect phase: {disconnectResult.Message}",
                    disconnectResult.ExceptionThrown);

            return Connect();
        }

        // ── IEncoderDriver: Position Reading ─────────────────────────────────────

        /// <summary>
        /// Reads the current singleturn position in degrees (0 – 360) via the PZ1SL controller.
        /// The native DLL call is run on a background thread, keeping the UI responsive.
        /// </summary>
        public async Task<OperationResult<double>> ReadPositionDegreesAsync(CancellationToken ct)
        {
            if (!IsConnected)
                return OperationResult.SetError("Cannot read position: driver is not connected.");

            try
            {
                var result = await _controller.GetPositionAsync(ct);

                if (!result.IsSuccess)
                    return OperationResult.SetError(
                        $"Position read failed: {result.Message}",
                        result.ExceptionThrown);

                return OperationResult.SetSuccessful(result.Data.Degree);
            }
            catch (OperationCanceledException)
            {
                return OperationResult.SetError("Position read was cancelled.");
            }
            catch (Exception ex)
            {
                return OperationResult.SetError("Exception during position read.", ex);
            }
        }

        // ── IEncoderDriver: Calibration ───────────────────────────────────────────

        /// <summary>
        /// Presets the MT/ST position by executing <see cref="PzCommand.MTST_PRESET"/>
        /// (or <see cref="PzCommand.MTST_PRESET_STORE"/> when <paramref name="storeToEeprom"/> is true)
        /// through the native wrapper.
        /// </summary>
        public OperationResult<string> PresetPosition(bool storeToEeprom)
        {
            if (!IsConnected)
                return OperationResult.SetError("Cannot preset: driver is not connected.");

            try
            {
                var cmd = storeToEeprom ? PzCommand.MTST_PRESET_STORE : PzCommand.MTST_PRESET;
                return _controller.NativeWrapper.ExecuteCommand(cmd);
            }
            catch (Exception ex)
            {
                return OperationResult.SetError("Exception during preset.", ex);
            }
        }

        // ── Event Forwarding ──────────────────────────────────────────────────────

        /// <summary>
        /// Forwards the inner controller's connection state event to the platform.
        /// Keeps the inner controller's type hidden from external subscribers.
        /// </summary>
        private void OnControllerStateChanged(object sender, bool isConnected)
            => ConnectionStateChanged?.Invoke(this, isConnected);

        // ── IDisposable ───────────────────────────────────────────────────────────

        public void Dispose()
        {
            if (_disposed) return;

            if (_controller != null)
            {
                _controller.ConnectionStateChanged -= OnControllerStateChanged;
                _controller.Dispose();
                _controller = null;
            }

            _disposed = true;
        }
    }
}
