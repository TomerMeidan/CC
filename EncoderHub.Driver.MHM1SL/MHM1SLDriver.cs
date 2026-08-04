using EncoderHub.Abstractions;
using OperationResultManager;
using System;
using System.Threading;
using System.Threading.Tasks;
using static iC_Haus_Library.Interop.iC_MHM_Series.NativeEnums;

namespace EncoderHub.Driver.MHM1SL
{
    /// <summary>
    /// Translator: wraps <see cref="MHM1SLWrapper"/> and exposes it as a standard
    /// <see cref="IEncoderDriver"/>.
    /// <para>
    /// All MHM-specific details — P/Invoke calls, interface enums, native structs —
    /// are isolated inside this class and <see cref="MHM1SLWrapper"/>.
    /// The GUI never sees any of it.
    /// </para>
    /// </summary>
    public sealed class MHM1SLDriver : IEncoderDriver
    {
        // ── Private State ─────────────────────────────────────────────────────────

        private readonly MHM1SLDriverConfiguration _config;
        private MHM1SLWrapper _wrapper;
        private bool _isConnected;
        private bool _disposed;

        // ── IEncoderDriver: Identity ──────────────────────────────────────────────

        public string DriverName        => "iC-Haus MHM1SL";
        public string DriverDescription => "Magnetic Hall-Effect Encoder via USB Adapter (MB3U / MB4U / MB5U / MB8U)";

        // ── IEncoderDriver: State ─────────────────────────────────────────────────

        public bool IsConnected
        {
            get => _isConnected;
            private set
            {
                if (_isConnected == value) return;
                _isConnected = value;
                ConnectionStateChanged?.Invoke(this, value);
            }
        }

        public event EventHandler<bool> ConnectionStateChanged;

        /// <inheritdoc/>
        public int Resolution
        {
            get => _config.Resolution;
            set => _config.Resolution = value;
        }

        // ── Construction ──────────────────────────────────────────────────────────

        /// <summary>Creates a driver using default MHM1SL settings.</summary>
        public MHM1SLDriver() : this(new MHM1SLDriverConfiguration()) { }

        /// <summary>Creates a driver with explicit configuration.</summary>
        public MHM1SLDriver(MHM1SLDriverConfiguration config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        // ── IEncoderDriver: Commands ──────────────────────────────────────────────

        public OperationResult<string> Connect()
        {
            if (IsConnected)
                return OperationResult.SetSuccessful("Already connected.");

            _wrapper = new MHM1SLWrapper(_config.Resolution);

            try
            {
                // 1. Open library / get handle
                var openRes = _wrapper.Open();
                if (!openRes.IsSuccess) return openRes;

                // 2. Select USB adapter
                var setIfRes = _wrapper.SetInterface(_config.InterfaceType, _config.InterfaceOption);
                if (!setIfRes.IsSuccess)
                {
                    _wrapper.Close();
                    return setIfRes;
                }

                // 3. Initialise bus communication
                var initRes = _wrapper.Initialize();
                if (!initRes.IsSuccess)
                {
                    _wrapper.Close();
                    return initRes;
                }

                // 4. (Optional) Force BiSS mode
                if (_config.ForceBiSSMode)
                {
                    var bissRes = _wrapper.SwitchToBiss();
                    // Non-fatal: log but continue — some adapters are already in BiSS mode
                    _ = bissRes;
                }

                // 5. ReadParams — synchronises chip RAM with the DLL.
                //    The MHM_Demo calls this after Initialize and before the first ReadSens.
                //    Skipping it leads to wrong / stale singleturn data.
                var readParamsRes = _wrapper.ReadParams();
                if (!readParamsRes.IsSuccess)
                {
                    _wrapper.Close();
                    return readParamsRes;
                }

                IsConnected = true;
                return OperationResult.SetSuccessful("Connected",
                    $"MHM driver connected via {_config.InterfaceType}.");
            }
            catch (Exception ex)
            {
                _wrapper?.Close();
                IsConnected = false;
                return OperationResult.SetError("Exception during MHM connection flow.", ex);
            }
        }

        public OperationResult<string> Disconnect()
        {
            if (!IsConnected)
                return OperationResult.SetSuccessful("Already disconnected.");

            var closeRes = _wrapper.Close();
            // Logical disconnect regardless of whether the native close succeeded
            IsConnected = false;
            return closeRes;
        }

        /// <summary>
        /// Soft-reset: disconnect cleanly then reconnect.
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
        /// Reads the current singleturn position in degrees (0 – 360) via MHM_ReadSens.
        /// The native DLL call is run on a background thread via <see cref="Task.Run"/>.
        /// </summary>
        public async Task<OperationResult<double>> ReadPositionDegreesAsync(CancellationToken ct)
        {
            if (!IsConnected)
                return OperationResult.SetError("Cannot read position: driver is not connected.");

            try
            {
                var readRes = await Task.Run(() => _wrapper.ReadSens(), ct);

                if (!readRes.IsSuccess)
                    return OperationResult.SetError(
                        $"Position read failed: {readRes.Message}",
                        readRes.ExceptionThrown);

                // Compute degrees — match the original iC-Haus-Biss
                // `MHMSeriesPositionModel` formula EXACTLY:
                //     _degree = (Convert.ToSingle(ReadSens.ulSingleturn) * 360) / Math.Pow(2, Resolution);
                double degrees = (Convert.ToSingle(readRes.Data.ulSingleturn) * 360)
                                 / Math.Pow(2, _config.Resolution);
                return OperationResult.SetSuccessful(degrees);
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
        /// Zeros the encoder's current position via <c>MHM_Preset</c>.
        /// When <paramref name="storeToEeprom"/> is true, also calls <c>MHM_WriteEeprom</c>
        /// (area = MHM-config-no-offsets... actually CFG_MHM which includes the offset registers)
        /// so the preset offset survives a power cycle.
        /// </summary>
        public OperationResult<string> PresetPosition(bool storeToEeprom)
        {
            if (!IsConnected)
                return OperationResult.SetError("Cannot preset: driver is not connected.");

            try
            {
                var presetRes = _wrapper.Preset();
                if (!presetRes.IsSuccess) return presetRes;

                if (storeToEeprom)
                {
                    // CFG_MHM includes the offset registers updated by Preset()
                    var saveRes = _wrapper.WriteEeprom(MHM_E2PAreaEnum.eMHM_E2P_CFG_MHM);
                    if (!saveRes.IsSuccess) return saveRes;
                    return OperationResult.SetSuccessful(
                        $"Preset executed and stored to EEPROM. {saveRes.Message}");
                }

                return presetRes;
            }
            catch (Exception ex)
            {
                return OperationResult.SetError("Exception during MHM preset.", ex);
            }
        }

        // ── IDisposable ───────────────────────────────────────────────────────────

        public void Dispose()
        {
            if (_disposed) return;

            if (_isConnected)
                _wrapper?.Close();

            _disposed = true;
        }
    }
}
