using System.Threading.Tasks;
using FluentAssertions;
using I18Next.Net.Backends;
using I18Next.Net.TranslationTrees;
using NSubstitute;
using NUnit.Framework;

namespace I18Next.Net.Tests.Backends;

[TestFixture]
public class CompositeBackendFixture
{
    [TearDown]
    public void TearDown()
    {
        _backendA.ClearReceivedCalls();
        _backendB.ClearReceivedCalls();
    }

    private ITranslationBackend _backendB;
    private ITranslationBackend _backendA;
    private CompositeBackend _backend;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        _backendA = Substitute.For<ITranslationBackend>();
        _backendB = Substitute.For<ITranslationBackend>();

        _backend = new CompositeBackend(_backendA, _backendB);

        _backendA.LoadNamespaceAsync("en", "backB").Returns((ITranslationTree) null);
        _backendB.LoadNamespaceAsync("en", "backA").Returns((ITranslationTree) null);
    }

    [Test]
    public async Task LoadNamespaceAsync_WithBackendANamespace_ShouldCallBackendA()
    {
        var tree = await _backend.LoadNamespaceAsync("en", "backA");

        tree.Should().NotBeNull();

        await _backendA.Received(1).LoadNamespaceAsync("en", "backA");
        await _backendA.DidNotReceive().LoadNamespaceAsync("en", "backB");
        await _backendB.DidNotReceive().LoadNamespaceAsync("en", "backA");
        await _backendB.DidNotReceive().LoadNamespaceAsync("en", "backB");
    }

    [Test]
    public async Task LoadNamespaceAsync_WithBackendBNamespace_ShouldCallBackendB()
    {
        var tree = await _backend.LoadNamespaceAsync("en", "backB");

        tree.Should().NotBeNull();

        await _backendA.DidNotReceive().LoadNamespaceAsync("en", "backA");
        await _backendA.Received(1).LoadNamespaceAsync("en", "backB");
        await _backendB.DidNotReceive().LoadNamespaceAsync("en", "backA");
        await _backendB.Received(1).LoadNamespaceAsync("en", "backB");
    }
}