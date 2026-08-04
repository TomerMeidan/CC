using EncoderHub.Abstractions;
using EncoderHub.App.Models;
using OperationResultManager;
using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace EncoderHub.App.ViewModels
{
    /// <summary>
    /// The single ViewModel that backs the entire GUI.
    /// <para>
    /// It only knows about <see cref="IEncoderDriver"/> — never about PZ, BiSS,
    /// or any other hardware-specific type. Swapping a driver in the
    /// <see cref="DriverRegistry"/> is the only change needed to support new hardware.
    /// </para>
    /// </summary>
    public class DriverControlViewModel : BaseViewModel
    {
        // ── Private State ─────────────────────────────────────────────────────────

        private IEncoderDriver _selectedDriver;
        private string         _statusMessage = "Select a driver from the list above, then click Connect.";
        private bool           _isBusy;
        private double         _position;
        private CancellationTokenSource _positionPollCts;

        // ── Collections & Selection ───────────────────────────────────────────────

        /// <summary>All drivers that are registered in the platform.</summary>
        public ObservableCollection<IEncoderDriver> AvailableDrivers { get; }

        /// <summary>
        /// The driver currently chosen in the ComboBox.
        /// Changing this property unsubscribes from the old driver and subscribes to the new one.
        /// </summary>
        public IEncoderDriver SelectedDriver
        {
            get => _selectedDriver;
            set
            {
                // Stop any ongoing position poll before switching drivers
                StopPositionPolling();

                // Detach from the old driver's events
                if (_selectedDriver != null)
                    _selectedDriver.ConnectionStateChanged -= OnDriverConnectionStateChanged;

                _selectedDriver = value;

                // Attach to the new driver's events
                if (_selectedDriver != null)
                    _selectedDriver.ConnectionStateChanged += OnDriverConnectionStateChanged;

                OnPropertyChanged();
                OnPropertyChanged(nameof(IsConnected));
                OnPropertyChanged(nameof(SelectedDriverDescription));
                OnPropertyChanged(nameof(HasDriverSelected));
                OnPropertyChanged(nameof(Resolution));
                RefreshCommands();
            }
        }

        /// <summary>
        /// Bit resolution of the selected driver. Bound to a TextBox in the GUI.
        /// Changes take effect on the next Connect — for the PZ driver this means
        /// reconnecting after editing; for MHM the next read tick picks it up live.
        /// </summary>
        public int Resolution
        {
            get => _selectedDriver?.Resolution ?? 18;
            set
            {
                if (_selectedDriver == null) return;
                if (_selectedDriver.Resolution == value) return;
                _selectedDriver.Resolution = value;
                OnPropertyChanged();
            }
        }

        // ── Derived Display Properties ─────────────────────────────────────────────

        public bool   IsConnected               => _selectedDriver?.IsConnected ?? false;
        public bool   HasDriverSelected         => _selectedDriver != null;
        public string SelectedDriverDescription => _selectedDriver?.DriverDescription
                                                   ?? "No driver selected.";

        public string StatusMessage
        {
            get => _statusMessage;
            private set { _statusMessage = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// True while a Connect / Disconnect / Reset / Preset operation is running.
        /// Disables all action buttons to prevent double-clicks.
        /// </summary>
        public bool IsBusy
        {
            get => _isBusy;
            private set { _isBusy = value; OnPropertyChanged(); RefreshCommands(); }
        }

        // ── Position Property ─────────────────────────────────────────────────────

        /// <summary>
        /// The current absolute singleturn position in degrees (0 – 360).
        /// Updated continuously at ~60 Hz while the device is connected.
        /// </summary>
        public double Position
        {
            get => _position;
            private set { _position = value; OnPropertyChanged(); }
        }

        // ── Commands ──────────────────────────────────────────────────────────────

        public RelayCommand ConnectCommand        { get; }
        public RelayCommand DisconnectCommand     { get; }
        public RelayCommand ResetCommand          { get; }
        public RelayCommand PresetCommand         { get; }
        public RelayCommand PresetStoreCommand    { get; }

        // ── Constructor ───────────────────────────────────────────────────────────

        public DriverControlViewModel(DriverRegistry registry)
        {
            AvailableDrivers = new ObservableCollection<IEncoderDriver>(registry.Drivers);

            ConnectCommand = new RelayCommand(
                execute:    () => Run(() => _selectedDriver.Connect(),    "Connect"),
                canExecute: () => HasDriverSelected && !IsConnected && !IsBusy);

            DisconnectCommand = new RelayCommand(
                execute:    () => Run(() => _selectedDriver.Disconnect(), "Disconnect"),
                canExecute: () => HasDriverSelected && IsConnected && !IsBusy);

            ResetCommand = new RelayCommand(
                execute:    () => Run(() => _selectedDriver.Reset(),      "Reset"),
                canExecute: () => HasDriverSelected && IsConnected && !IsBusy);

            PresetCommand = new RelayCommand(
                execute:    () => RunPreset(storeToEeprom: false),
                canExecute: () => HasDriverSelected && IsConnected && !IsBusy);

            PresetStoreCommand = new RelayCommand(
                execute:    () => RunPreset(storeToEeprom: true),
                canExecute: () => HasDriverSelected && IsConnected && !IsBusy);
        }

        // ── Helpers ───────────────────────────────────────────────────────────────

        /// <summary>
        /// Executes a driver action, updates <see cref="StatusMessage"/> with the result,
        /// and guards against re-entry with <see cref="IsBusy"/>.
        /// </summary>
        private void Run(Func<OperationResult<string>> action, string actionName)
        {
            IsBusy = true;
            try
            {
                var result = action();
                StatusMessage = result.IsSuccess
                    ? $"[OK]    {actionName} — {result.Message}"
                    : $"[ERROR] {actionName} — {result.Message}";
            }
            catch (Exception ex)
            {
                StatusMessage = $"[ERROR] {actionName} threw an exception: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }

            // Ensure IsConnected and button states refresh after the operation
            OnPropertyChanged(nameof(IsConnected));
            RefreshCommands();
        }

        /// <summary>
        /// Stops position polling, executes a preset command, then restarts polling.
        /// Mirrors the original <c>PZDeviceService.PresetPosition</c> pattern.
        /// </summary>
        private void RunPreset(bool storeToEeprom)
        {
            StopPositionPolling();
            Run(() => _selectedDriver.PresetPosition(storeToEeprom),
                storeToEeprom ? "Preset MT/ST + Store" : "Preset MT/ST");

            if (IsConnected)
                StartPositionPolling();
        }

        /// <summary>
        /// Handles connection-state change events that may arrive from a non-UI thread.
        /// Marshals to the UI dispatcher so WPF bindings update safely.
        /// </summary>
        private void OnDriverConnectionStateChanged(object sender, bool isConnected)
        {
            Application.Current?.Dispatcher.Invoke(() =>
            {
                OnPropertyChanged(nameof(IsConnected));
                RefreshCommands();

                StatusMessage = isConnected
                    ? $"[OK]    {_selectedDriver?.DriverName} connected."
                    : $"[INFO]  {_selectedDriver?.DriverName} disconnected.";

                if (isConnected)
                    StartPositionPolling();
                else
                    StopPositionPolling();
            });
        }

        private void RefreshCommands()
        {
            ConnectCommand.RaiseCanExecuteChanged();
            DisconnectCommand.RaiseCanExecuteChanged();
            ResetCommand.RaiseCanExecuteChanged();
            PresetCommand.RaiseCanExecuteChanged();
            PresetStoreCommand.RaiseCanExecuteChanged();
        }

        // ── Position Polling ──────────────────────────────────────────────────────

        /// <summary>
        /// Starts a background polling loop that reads position at ~60 Hz.
        /// Any previous loop is cancelled first.
        /// </summary>
        private void StartPositionPolling()
        {
            StopPositionPolling();
            _positionPollCts = new CancellationTokenSource();
            PollPositionAsync(_positionPollCts.Token);
        }

        /// <summary>
        /// Cancels the position polling loop and resets the position readout.
        /// </summary>
        private void StopPositionPolling()
        {
            _positionPollCts?.Cancel();
            _positionPollCts?.Dispose();
            _positionPollCts = null;
            Position = 0.0;
        }

        /// <summary>
        /// Fire-and-forget async loop: reads position every ~16 ms (≈60 Hz)
        /// and updates <see cref="Position"/> on the UI thread.
        /// </summary>
        private async void PollPositionAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested && IsConnected)
            {
                try
                {
                    var result = await _selectedDriver.ReadPositionDegreesAsync(ct);
                    if (result.IsSuccess)
                        Position = result.Data;
                }
                catch (OperationCanceledException) { break; }
                catch { /* swallow transient read errors — retry next tick */ }

                try   { await Task.Delay(16, ct); }
                catch (OperationCanceledException) { break; }
            }
        }
    }
}
