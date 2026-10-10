using System.Threading.Tasks;

using Bunit;

using I18Next.Net.Backends;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.DependencyInjection;

using Shouldly;

using Xunit;

namespace I18Next.Net.Blazor.Tests;

public class I18NextComponentBaseFixture : BunitContext
{
    private readonly InMemoryBackend _inner = TestServices.CreateBackend();
    private readonly NotifyingBackend _backend;

    public I18NextComponentBaseFixture()
    {
        _backend = new NotifyingBackend(_inner);

        Services.AddTestI18Next(_backend);
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    private IBlazorI18Next I18n => Services.GetRequiredService<IBlazorI18Next>();

    [Fact]
    public void T_ShouldTranslateIntoLanguageOfUser()
    {
        var cut = Render<GreetingComponent>(p => p.Add(c => c.Name, "Jane"));

        cut.Markup.ShouldBe("Hello Jane");
    }

    [Fact]
    public async Task LanguageChanged_ShouldRenderAgain()
    {
        var cut = Render<GreetingComponent>(p => p.Add(c => c.Name, "Jane"));

        await cut.InvokeAsync(() => I18n.ChangeLanguageAsync("de"));

        cut.Markup.ShouldBe("Hallo Jane");
    }

    [Fact]
    public void TranslationsChanged_ShouldRenderAgain()
    {
        var cut = Render<GreetingComponent>(p => p.Add(c => c.Name, "Jane"));

        _inner.RemoveNamespace("en", "translation");
        _inner.AddTranslation("en", "translation", "greeting", "Hi {{name}}");
        _backend.RaiseTranslationsChanged("en", "translation");

        cut.WaitForAssertion(() => cut.Markup.ShouldBe("Hi Jane"));
    }

    [Fact]
    public void SetParameters_ShouldSubscribeOnce()
    {
        var cut = Render<GreetingComponent>(p => p.Add(c => c.Name, "Jane"));
        cut.Render(p => p.Add(c => c.Name, "John"));
        var renderCount = cut.RenderCount;

        _backend.RaiseTranslationsChanged(null, null);

        cut.WaitForAssertion(() => cut.RenderCount.ShouldBe(renderCount + 1));
        cut.Markup.ShouldBe("Hello John");
    }

    [Fact]
    public async Task Dispose_ShouldUnsubscribe()
    {
        var cut = Render<GreetingComponent>(p => p.Add(c => c.Name, "Jane"));
        var component = cut.Instance;

        await DisposeComponentsAsync();
        await I18n.ChangeLanguageAsync("de");
        _backend.RaiseTranslationsChanged(null, null);

        component.Disposed.ShouldBeTrue();
        component.RenderCount.ShouldBe(1);
    }

    [Fact]
    public void Dispose_NotRendered_ShouldNotThrow()
    {
        var component = new GreetingComponent();

        Should.NotThrow(component.Dispose);
        component.Disposed.ShouldBeTrue();
    }

    private sealed class GreetingComponent : I18NextComponentBase
    {
        [Parameter]
        public string Name { get; set; }

        public bool Disposed { get; private set; }

        public int RenderCount { get; private set; }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            RenderCount++;
            builder.AddContent(0, T("greeting", new { name = Name }));
        }

        protected override void Dispose(bool disposing)
        {
            Disposed = true;
            base.Dispose(disposing);
        }
    }
}
