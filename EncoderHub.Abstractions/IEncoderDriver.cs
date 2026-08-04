using OperationResultManager;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace EncoderHub.Abstractions
{
    /// <summary>
    /// The Universal Remote — the single standard contract that every encoder driver must fulfill.
    /// <para>
    /// The GUI and any consumer layer only ever reference this interface.
    /// They never import vendor-specific namespaces, PInvoke code, or hardware enums.
    /// Adding support for a new encoder brand means creating a new class that implements
    /// this interface; nothing else in the platform changes.
    /// </para>
    /// </summary>
    public interface IEncoderDriver : IDisposable
    {
        // ── Identity ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Human-readable display name shown in the driver selection list.
        /// Example: "iC-Haus PZ1SL"
        /// </summary>
        string DriverName { get; }

        /// <summary>
        /// Short description of the hardware this driver controls.
        /// Example: "BiSS Position Encoder via USB Adapter (MB3U / MB4U / MB5U)"
        /// </summary>
        string DriverDescription { get; }

        // ── State ─────────────────────────────────────────────────────────────────

        /// <summary>
        /// <c>true</c> when a communication session is open and the device is operational.
        /// </summary>
        bool IsConnected { get; }

        /// <summary>
        /// Fired whenever the connection state changes.
        /// <c>true</c> = device just connected, <c>false</c> = device just disconnected.
        /// </summary>
        event EventHandler<bool> ConnectionStateChanged;

        // ── Commands ──────────────────────────────────────────────────────────────

        /// <summary>
        /// Open a communication session with the device.
        /// Performs all vendor-specific initialisation steps internally.
        /// </summary>
        OperationResult<string> Connect();

        /// <summary>
        /// Close the communication session and release all hardware resources.
        /// </summary>
        OperationResult<string> Disconnect();

        /// <summary>
        /// Perform a soft-reset: disconnect cleanly, then reconnect.
        /// A driver may override this with a hardware-level reset command if the
        /// underlying device supports one.
        /// </summary>
        OperationResult<string> Reset();

        // ── Configuration ─────────────────────────────────────────────────────────

        /// <summary>
        /// The encoder's singleturn bit resolution (e.g. 12, 14, 18, 22).
        /// Used to convert the raw singleturn count into degrees:
        /// <c>degrees = (ulSingleturn × 360) / 2^Resolution</c>.
        /// <para>
        /// Must be set to match the actual hardware — different iC-Haus encoder
        /// variants use different bit widths. Changes take effect on the next
        /// <see cref="Connect"/> call.
        /// </para>
        /// </summary>
        int Resolution { get; set; }

        // ── Sensor Reading ────────────────────────────────────────────────────────

        /// <summary>
        /// Reads the current absolute singleturn position in degrees (0 – 360).
        /// <para>
        /// Implementations offload the native DLL call to a background thread.
        /// The returned value is the fractional angle for the current revolution.
        /// </para>
        /// </summary>
        /// <param name="ct">Cancellation token — honour it to allow the caller to abort polling.</param>
        Task<OperationResult<double>> ReadPositionDegreesAsync(CancellationToken ct);

        // ── Calibration ───────────────────────────────────────────────────────────

        /// <summary>
        /// Presets the current multiturn + singleturn position to zero
        /// (equivalent to commanding the encoder to treat its current position as the origin).
        /// </summary>
        /// <param name="storeToEeprom">
        /// When <c>true</c>, the resulting offset is also written to the device's EEPROM
        /// so it survives a power cycle.
        /// </param>
        OperationResult<string> PresetPosition(bool storeToEeprom);
    }
}
