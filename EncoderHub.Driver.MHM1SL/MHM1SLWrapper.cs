using OperationResultManager;
using System;
using System.Runtime.InteropServices;
using System.Text;
using static iC_Haus_Library.Interop.iC_MHM_Series.NativeEnums;
using static iC_Haus_Library.Interop.iC_MHM_Series.NativeStructs;
using static OperationResultManager.Utils.Enums;

namespace EncoderHub.Driver.MHM1SL
{
    /// <summary>
    /// Managed wrapper around the native <c>MHM_1SL_interface_64.dll</c>.
    /// <para>
    /// Mirrors the structure of <c>PZ1SLWrapper</c> in iC-Haus-Library:
    /// every native call returns an <see cref="OperationResult{T}"/> so the
    /// caller never needs to inspect raw error codes.
    /// </para>
    /// <para>
    /// The P/Invoke declarations are redeclared here because
    /// <c>iC_Haus_Library.Interop.iC_MHM_Series.NativeMethods</c> is
    /// <c>internal</c> to the library assembly.  The public enums and structs
    /// (<see cref="MHM_ErrorEnum"/>, <see cref="MHM_InterfaceEnum"/>,
    /// <see cref="MHM_ReadSensStruct"/>) are reused directly.
    /// </para>
    /// </summary>
    public class MHM1SLWrapper
    {
        // ── P/Invoke declarations (subset required for Connect/Read/Disconnect) ──

        private const string DllName = "MHM_1SL_interface_64.dll";

        [DllImport(DllName)] private static extern uint MHM_Open(out uint pulMHMHandle);
        [DllImport(DllName)] private static extern uint MHM_Close(uint ulMHMHandle);
        [DllImport(DllName)] private static extern uint MHM_SetInterface(uint ulMHMHandle, uint ulInterface, string pcInterfaceOption);
        [DllImport(DllName)] private static extern uint MHM_Initialize(uint ulMHMHandle);
        [DllImport(DllName)] private static extern uint MHM_SwitchToBiss(uint ulMHMHandle, ref uint pulEnSsi);
        [DllImport(DllName)] private static extern uint MHM_ReadSens(uint ulMHMHandle, out MHM_ReadSensStruct pSensorDataStruct);
        [DllImport(DllName)] private static extern uint MHM_ReadParams(uint ulMHMHandle);
        [DllImport(DllName)] private static extern uint MHM_Preset(uint ulMHMHandle);
        [DllImport(DllName)] private static extern uint MHM_WriteEeprom(uint ulMHMHandle, uint ulEepromArea);
        [DllImport(DllName, CharSet = CharSet.Ansi)]
        private static extern uint MHM_GetLastError(uint ulMHMHandle, out uint pulLastError, out uint pulErrorType, StringBuilder pcErrorText);

        // ── Handle ────────────────────────────────────────────────────────────────

        /// <summary>Session handle returned by <see cref="Open"/> and used in every subsequent call.</summary>
        public uint Handle { get; private set; }

        /// <summary>Singleturn bit resolution used when computing degree values.</summary>
        public int Resolution { get; private set; }

        public MHM1SLWrapper(int resolution)
        {
            Resolution = resolution;
        }

        // ── Lifecycle ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Calls <c>MHM_Open</c>: loads the native library and obtains a session handle.
        /// </summary>
        public OperationResult<string> Open()
        {
            try
            {
                uint error = MHM_Open(out uint handle);
                if (error != (uint)MHM_ErrorEnum.eMHM_OK)
                    return GetLastError();

                Handle = handle;
                return OperationResult.SetSuccessful($"{error}",
                    $"MHM communication open. Handle: {Handle}.", opLogResult: OperationLogResult.ON);
            }
            catch (Exception ex)
            {
                return OperationResult.SetError(
                    $"An {ex.GetType().Name} was thrown during MHM_Open. {ex.InnerException?.Message ?? ex.Message}",
                    ex.InnerException ?? ex, opLogResult: OperationLogResult.ON);
            }
        }

        /// <summary>
        /// Calls <c>MHM_Close</c>: releases the handle and terminates the connection.
        /// </summary>
        public OperationResult<string> Close()
        {
            try
            {
                uint error = MHM_Close(Handle);
                if (error != (uint)MHM_ErrorEnum.eMHM_OK)
                    return GetLastError();

                return OperationResult.SetSuccessful($"{error}",
                    $"Handle: {Handle}, MHM communication closed.", opLogResult: OperationLogResult.ON);
            }
            catch (Exception ex)
            {
                return OperationResult.SetError(
                    $"An {ex.GetType().Name} was thrown during MHM_Close. {ex.InnerException?.Message ?? ex.Message}",
                    ex.InnerException ?? ex, opLogResult: OperationLogResult.ON);
            }
        }

        /// <summary>
        /// Calls <c>MHM_SetInterface</c>: selects and connects to a USB adapter.
        /// </summary>
        /// <param name="interfaceType">Adapter type (MB3U BiSS/SPI, MB4U, MB5U, MB8U).</param>
        /// <param name="interfaceOption">
        /// Optional last-4-chars serial number for MB4U/MB5U disambiguation.
        /// Pass an empty string when only one adapter is connected.
        /// </param>
        public OperationResult<string> SetInterface(MHM_InterfaceEnum interfaceType, string interfaceOption = "")
        {
            try
            {
                uint error = MHM_SetInterface(Handle, (uint)interfaceType, interfaceOption ?? "");
                if (error != (uint)MHM_ErrorEnum.eMHM_OK)
                    return GetLastError();

                return OperationResult.SetSuccessful($"{error}",
                    $"Handle: {Handle}, MHM interface set to {interfaceType}.", opLogResult: OperationLogResult.ON);
            }
            catch (Exception ex)
            {
                return OperationResult.SetError(
                    $"An {ex.GetType().Name} was thrown during MHM_SetInterface.",
                    ex.InnerException ?? ex, opLogResult: OperationLogResult.ON);
            }
        }

        /// <summary>
        /// Calls <c>MHM_Initialize</c>: initialises bus communication.
        /// </summary>
        public OperationResult<string> Initialize()
        {
            try
            {
                uint error = MHM_Initialize(Handle);
                if (error != (uint)MHM_ErrorEnum.eMHM_OK)
                    return GetLastError();

                return OperationResult.SetSuccessful($"{error}",
                    $"Handle: {Handle}, MHM communication initialised.", opLogResult: OperationLogResult.ON);
            }
            catch (Exception ex)
            {
                return OperationResult.SetError(
                    $"An {ex.GetType().Name} was thrown during MHM_Initialize.",
                    ex.InnerException ?? ex, opLogResult: OperationLogResult.ON);
            }
        }

        /// <summary>
        /// Calls <c>MHM_SwitchToBiss</c>: forces the device from SSI to BiSS protocol.
        /// </summary>
        public OperationResult<string> SwitchToBiss()
        {
            try
            {
                uint enSsi  = 0;
                uint error  = MHM_SwitchToBiss(Handle, ref enSsi);
                if (error != (uint)MHM_ErrorEnum.eMHM_OK)
                    return GetLastError();

                return OperationResult.SetSuccessful($"{error}",
                    $"Handle: {Handle}, Switched to BiSS. Previous ENSSI: {enSsi}.", opLogResult: OperationLogResult.ON);
            }
            catch (Exception ex)
            {
                return OperationResult.SetError(
                    $"An {ex.GetType().Name} was thrown during MHM_SwitchToBiss.",
                    ex.InnerException ?? ex, opLogResult: OperationLogResult.ON);
            }
        }

        /// <summary>
        /// Calls <c>MHM_ReadParams</c>: synchronises the chip RAM with the DLL.
        /// MUST be called once after Initialize() and before the first ReadSens() —
        /// otherwise position values will be wrong.
        /// </summary>
        public OperationResult<string> ReadParams()
        {
            try
            {
                uint error = MHM_ReadParams(Handle);
                if (error != (uint)MHM_ErrorEnum.eMHM_OK)
                    return GetLastError();

                return OperationResult.SetSuccessful($"{error}",
                    $"Handle: {Handle}, ReadParams executed.", opLogResult: OperationLogResult.ON);
            }
            catch (Exception ex)
            {
                return OperationResult.SetError(
                    $"An {ex.GetType().Name} was thrown during MHM_ReadParams.",
                    ex.InnerException ?? ex, opLogResult: OperationLogResult.ON);
            }
        }

        /// <summary>
        /// Calls <c>MHM_Preset</c>: zeros the encoder's current position.
        /// Afterwards reads the offset registers of the iC-MHM to the DLL.
        /// </summary>
        public OperationResult<string> Preset()
        {
            try
            {
                uint error = MHM_Preset(Handle);
                if (error != (uint)MHM_ErrorEnum.eMHM_OK)
                    return GetLastError();

                return OperationResult.SetSuccessful($"{error}",
                    $"Handle: {Handle}, Preset executed.", opLogResult: OperationLogResult.ON);
            }
            catch (Exception ex)
            {
                return OperationResult.SetError(
                    $"An {ex.GetType().Name} was thrown during MHM_Preset.",
                    ex.InnerException ?? ex, opLogResult: OperationLogResult.ON);
            }
        }

        /// <summary>
        /// Calls <c>MHM_WriteEeprom</c>: writes parameters from the DLL to the device EEPROM
        /// so the preset offset survives a power cycle.
        /// </summary>
        public OperationResult<string> WriteEeprom(MHM_E2PAreaEnum area)
        {
            try
            {
                uint error = MHM_WriteEeprom(Handle, (uint)area);
                if (error != (uint)MHM_ErrorEnum.eMHM_OK)
                    return GetLastError();

                return OperationResult.SetSuccessful($"{error}",
                    $"Handle: {Handle}, WriteEeprom area {area} executed.", opLogResult: OperationLogResult.ON);
            }
            catch (Exception ex)
            {
                return OperationResult.SetError(
                    $"An {ex.GetType().Name} was thrown during MHM_WriteEeprom.",
                    ex.InnerException ?? ex, opLogResult: OperationLogResult.ON);
            }
        }

        /// <summary>
        /// Calls <c>MHM_ReadSens</c>: reads position, error, warning and sign-of-life from the iC-MHM.
        /// </summary>
        public OperationResult<MHM_ReadSensStruct> ReadSens()
        {
            try
            {
                uint error = MHM_ReadSens(Handle, out MHM_ReadSensStruct readSens);
                if (error != (uint)MHM_ErrorEnum.eMHM_OK)
                    return GetLastError();

                return OperationResult.SetSuccessful(readSens);
            }
            catch (Exception ex)
            {
                return OperationResult.SetError(
                    $"An {ex.GetType().Name} was thrown during MHM_ReadSens.",
                    ex.InnerException ?? ex, opLogResult: OperationLogResult.ON);
            }
        }

        // ── Error helper ──────────────────────────────────────────────────────────

        /// <summary>
        /// Retrieves the last MHM error code and descriptive text via <c>MHM_GetLastError</c>.
        /// </summary>
        public OperationResult GetLastError()
        {
            var errorText = new StringBuilder(1024);
            try
            {
                uint rc = MHM_GetLastError(Handle, out uint lastError, out uint errorType, errorText);
                if (rc != (uint)MHM_ErrorEnum.eMHM_OK)
                {
                    return OperationResult.SetError(
                        $"Handle: {Handle}, Failed to retrieve last MHM error (code {rc}).",
                        opLogResult: OperationLogResult.ON);
                }
                return OperationResult.SetError(
                    $"Handle: {Handle}, MHM error — Type: {(MHM_ErrorTypeEnum)errorType}, " +
                    $"Code: {(MHM_ErrorEnum)lastError}, Text: {errorText}",
                    opLogResult: OperationLogResult.ON);
            }
            catch (Exception ex)
            {
                return OperationResult.SetError(
                    $"An {ex.GetType().Name} was thrown during MHM_GetLastError.",
                    ex.InnerException ?? ex, opLogResult: OperationLogResult.ON);
            }
        }
    }
}
