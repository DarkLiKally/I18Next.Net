using System;
using System.Threading.Tasks;

using I18Next.Net.Backends;
using I18Next.Net.TranslationTrees;

using NSubstitute;

using Shouldly;

using Xunit;

namespace I18Next.Net.Tests.Backends;

public class CompositeBackendFixture : IDisposable
{
    public CompositeBackendFixture()
    {
        _backendA = Substitute.For<ITranslationBackend>();
        _backendB = Substitute.For<ITranslationBackend>();

        _backend = new CompositeBackend(_backendA, _backendB);

        _backendA.LoadNamespaceAsync("en", "backB").Returns((ITranslationTree)null);
        _backendB.LoadNamespaceAsync("en", "backA").Returns((ITranslationTree)null);
    }

    public void Dispose()
    {
        _backendA.ClearReceivedCalls();
        _backendB.ClearReceivedCalls();

    }

    private readonly ITranslationBackend _backendB;
    private readonly ITranslationBackend _backendA;
    private readonly CompositeBackend _backend;


    [Fact]
    public async Task LoadNamespaceAsync_WithBackendANamespace_ShouldCallBackendA()
    {
        var tree = await _backend.LoadNamespaceAsync("en", "backA");

        tree.ShouldNotBeNull();

        await _backendA.Received(1).LoadNamespaceAsync("en", "backA");
        await _backendA.DidNotReceive().LoadNamespaceAsync("en", "backB");
        await _backendB.DidNotReceive().LoadNamespaceAsync("en", "backA");
        await _backendB.DidNotReceive().LoadNamespaceAsync("en", "backB");
    }

    [Fact]
    public async Task LoadNamespaceAsync_WithBackendBNamespace_ShouldCallBackendB()
    {
        var tree = await _backend.LoadNamespaceAsync("en", "backB");

        tree.ShouldNotBeNull();

        await _backendA.DidNotReceive().LoadNamespaceAsync("en", "backA");
        await _backendA.Received(1).LoadNamespaceAsync("en", "backB");
        await _backendB.DidNotReceive().LoadNamespaceAsync("en", "backA");
        await _backendB.Received(1).LoadNamespaceAsync("en", "backB");
    }
}
