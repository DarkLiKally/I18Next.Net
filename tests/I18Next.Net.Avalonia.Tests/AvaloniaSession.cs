using System;
using System.Threading;

using Avalonia;
using Avalonia.Headless;
using Avalonia.Threading;

namespace I18Next.Net.Avalonia.Tests;

/// <summary>
///     Runs the tests on the UI thread of a headless Avalonia application.
/// </summary>
public sealed class AvaloniaSession : IDisposable
{
    private readonly HeadlessUnitTestSession _session = HeadlessUnitTestSession.StartNew(typeof(TestApplication));

    public void Dispose()
    {
        _session.Dispose();
    }

    public void Invoke(Action action)
    {
        _session.Dispatch(action, CancellationToken.None).GetAwaiter().GetResult();
    }

    public static void Flush()
    {
        Dispatcher.UIThread.RunJobs();
    }
}

public class TestApplication : Application
{
    public static AppBuilder BuildAvaloniaApp()
    {
        return AppBuilder.Configure<TestApplication>().UseHeadless(new AvaloniaHeadlessPlatformOptions());
    }
}
