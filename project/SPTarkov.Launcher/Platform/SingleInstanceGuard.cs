using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;

namespace SPTarkov.Launcher.Platform;

/// <summary>
/// Enforces one running launcher per install location. Identity is derived from the launcher executable path, so two different SPT installs
/// run side by side, while a second launch of the same install is turned away.
/// </summary>
/// <remarks>
/// The gate is cross-platform. On Windows a turned-away launch additionally wakes the running instance via a named EventWaitHandle so it
/// can surface its window; named events are unsupported on Linux, so there the second instance simply prints a notice and exits.
/// </remarks>
public sealed class SingleInstanceGuard : IDisposable
{
    // Distinct kernel-object name prefixes; the per-install hash is appended to each. No "Local\" prefix so the names stay valid on Unix
    // (where they back a file) as well as Windows (session namespace).
    private const string MutexPrefix = "SPTarkov.Launcher.SingleInstance.";
    private const string EventPrefix = "SPTarkov.Launcher.Activate.";

    private readonly string _mutexName;
    private readonly string _eventName;

    private Mutex? _mutex;
    private bool _ownsMutex;
    private EventWaitHandle? _activateEvent;

    public SingleInstanceGuard()
    {
        var hash = InstallHash();
        _mutexName = MutexPrefix + hash;
        _eventName = EventPrefix + hash;
    }

    /// <summary>
    /// Attempts to become the sole instance for this install. Returns <c>true</c> if this process owns the slot and <c>false</c> if another
    /// instance already holds it. When <c>false</c> on Windows, the running instance is signalled to show its window.
    /// </summary>
    public bool TryClaimPrimary()
    {
        _mutex = new Mutex(false, _mutexName);

        try
        {
            _ownsMutex = _mutex.WaitOne(0, false);
        }
        catch (AbandonedMutexException)
        {
            // Previous owner died. WaitOne still transfers ownership to us.
            _ownsMutex = true;
        }

        if (_ownsMutex)
        {
            if (OperatingSystem.IsWindows())
            {
                _activateEvent = new EventWaitHandle(false, EventResetMode.AutoReset, _eventName);
            }

            return true;
        }

        // A sibling already owns this instances slot.
        if (OperatingSystem.IsWindows())
        {
            SignalExistingInstance();
        }
        else
        {
            Console.Error.WriteLine("Another instance of this SPT launcher is already running.");
        }

        return false;
    }

    /// <summary>
    /// Spins a background thread that waits for a second launch to signal, then runs onActivate to surface our window. Does nothing on Unix
    /// (no activation event) or when this process is not the primary.
    /// </summary>
    public void StartActivationListener(Action onActivate, ILogger logger)
    {
        if (_activateEvent is null)
        {
            return;
        }

        var listener = new Thread(() =>
        {
            while (true)
            {
                try
                {
                    _activateEvent.WaitOne();
                    onActivate();
                }
                catch (Exception ex)
                {
                    logger.LogError("Single-instance activation listener stopped: {ex}", ex);
                    return;
                }
            }
        })
        {
            IsBackground = true,
            Name = "SptSingleInstance",
        };
        listener.Start();
    }

    // Runs in a second instance before the app starts. Wakes the primary so it can surface itself, granting it permission to take the
    // foreground first. Windows only.
    private void SignalExistingInstance()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        try
        {
            WindowNative.AllowSetForegroundWindow(WindowNative.ASFW_ANY);

            if (EventWaitHandle.TryOpenExisting(_eventName, out var activate))
            {
                activate.Set();
                activate.Dispose();
            }
        }
        catch
        {
            // Best-effort. If signalling fails the primary simply stays as it is.
        }
    }

    public void Dispose()
    {
        if (_mutex is not null)
        {
            if (_ownsMutex)
            {
                try
                {
                    _mutex.ReleaseMutex();
                }
                catch
                {
                    // Already released, or released on a different thread... the OS reclaims it on exit anyway.
                }
            }

            _mutex.Dispose();
            _mutex = null;
        }

        _activateEvent?.Dispose();
        _activateEvent = null;
    }

    // Identity for this install.
    private static string InstallHash()
    {
        var identity = Environment.ProcessPath;
        if (string.IsNullOrEmpty(identity))
        {
            identity = Directory.GetCurrentDirectory();
        }

        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(identity.ToLowerInvariant()));

        return Convert.ToHexString(bytes, 0, 16); // 16 bytes to 32 chars... unique enough
    }
}
