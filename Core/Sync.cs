using System;
using System.Threading;
using System.Windows;
using System.Windows.Threading;

namespace AvalonLog.Core;

internal static class SyncAvalonLog {

    private static SynchronizationContext? _ctx;

    private static void InstallSynchronizationContext() {
        if (SynchronizationContext.Current == null) {
            SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext(Application.Current.Dispatcher));
        }
        _ctx = SynchronizationContext.Current ?? throw new Exception("AvalonLog: DispatcherSynchronizationContext 获取失败");
    }

    public static SynchronizationContext Context {
        get {
            if (_ctx == null) InstallSynchronizationContext();
            return _ctx!;
        }
    }

    public static void DoSync(Action func) {
        Application.Current.Dispatcher.Invoke(func);
    }
}