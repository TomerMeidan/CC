using EncoderHub.Abstractions;
using EncoderHub.Driver.MHM1SL;
using EncoderHub.Driver.PZ1SL;
using System.Collections.Generic;

namespace EncoderHub.App.Models
{
    /// <summary>
    /// The registry of all encoder drivers available in this build of the platform.
    /// <para>
    /// This is the <em>only</em> place in the codebase that references concrete
    /// driver types. To add support for a new encoder, instantiate it here — the
    /// rest of the platform (GUI, ViewModels, commands) requires no changes at all.
    /// </para>
    /// </summary>
    public class DriverRegistry
    {
        public IReadOnlyList<IEncoderDriver> Drivers { get; }

        public DriverRegistry()
        {
            Drivers = new List<IEncoderDriver>
            {
                new PZ1SLDriver(),
                new MHM1SLDriver()
                // ── Add future drivers here ───────────────────────────────────────
                // new SomeOtherVendorDriver(),
            };
        }
    }
}
