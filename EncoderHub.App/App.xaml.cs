using EncoderHub.App.Models;
using EncoderHub.App.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Windows;

namespace EncoderHub.App
{
    public partial class App : Application
    {
        public static IServiceProvider Services { get; private set; }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var services = new ServiceCollection();

            // ── Platform core ────────────────────────────────────────────────────
            // DriverRegistry is the single place to register new encoder drivers.
            // The GUI never references driver types directly.
            services.AddSingleton<DriverRegistry>();

            // ── ViewModels ───────────────────────────────────────────────────────
            services.AddSingleton<DriverControlViewModel>();
            services.AddSingleton<MainViewModel>();

            // ── Main window ──────────────────────────────────────────────────────
            services.AddSingleton<MainWindow>();

            Services = services.BuildServiceProvider();

            Services.GetRequiredService<MainWindow>().Show();
        }
    }
}
