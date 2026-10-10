using System;
using System.Threading;
using System.Windows.Threading;

namespace I18Next.Net.Wpf.Tests;

public sealed class StaDispatcher : IDisposable
{
    private readonly Dispatcher _dispatcher;

    public StaDispatcher()
    {
        using var started = new ManualResetEventSlim();
        Dispatcher dispatcher = null;

        var thread = new Thread(() =>
        {
            dispatcher = Dispatcher.CurrentDispatcher;
            started.Set();
            Dispatcher.Run();
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.IsBackground = true;
        thread.Start();
        started.Wait();

        _dispatcher = dispatcher;
    }

    public void Dispose()
    {
        _dispatcher.InvokeShutdown();
    }

    public static void Flush()
    {
        var frame = new DispatcherFrame();
        Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background, () => frame.Continue = false);
        Dispatcher.PushFrame(frame);
    }

    public void Invoke(Action action)
    {
        _dispatcher.Invoke(action);
    }
}
