using System;
using System.Threading;
using System.Windows;
using System.Windows.Threading;

namespace AvalonLog.Core;

internal static class SyncAvalonLog {

    private static SynchronizationContext? _ctx;
    private static bool _errorFileWrittenOnce = false;

    private static void InstallSynchronizationContext(bool logErrorsOnDesktop) {
        if (SynchronizationContext.Current == null) {
            SynchronizationContext.SetSynchronizationContext(
                new DispatcherSynchronizationContext(Application.Current.Dispatcher));
        }
        _ctx = SynchronizationContext.Current;

        if (_ctx == null && logErrorsOnDesktop && !_errorFileWrittenOnce) {
            string time = DateTime.UtcNow.ToString("yyyy-MM-dd_HH-mm-ss-fff");
            string filename = $"AvalonLog-SynchronizationContext setup failed-{time}.txt";
            string desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            string file = System.IO.Path.Combine(desktop, filename);
            try { System.IO.File.WriteAllText(file, "Failed to get DispatcherSynchronizationContext"); } catch { }
            _errorFileWrittenOnce = true;
            throw new Exception("See " + file);
        }
    }

    public static SynchronizationContext Context {
        get {
            if (_ctx == null) InstallSynchronizationContext(true);
            return _ctx!;
        }
    }

    public static void DoSync(Action func) {
        Application.Current.Dispatcher.Invoke(func);
    }
}