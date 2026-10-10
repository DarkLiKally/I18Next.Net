using System;

using Microsoft.Maui.Dispatching;

namespace I18Next.Net.Maui.Tests;

internal sealed class TestDispatcher : IDispatcher, IDispatcherProvider
{
    public static readonly TestDispatcher Instance = new();

    public bool IsDispatchRequired => false;

    public static void Use()
    {
        DispatcherProvider.SetCurrent(Instance);
    }

    public IDispatcher GetForCurrentThread()
    {
        return this;
    }

    public bool Dispatch(Action action)
    {
        action();

        return true;
    }

    public bool DispatchDelayed(TimeSpan delay, Action action)
    {
        return Dispatch(action);
    }

    public IDispatcherTimer CreateTimer()
    {
        throw new NotSupportedException();
    }
}
