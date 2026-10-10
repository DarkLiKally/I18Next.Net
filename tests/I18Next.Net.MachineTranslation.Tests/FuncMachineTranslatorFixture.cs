using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using NSubstitute;

using Shouldly;

using Xunit;

namespace I18Next.Net.MachineTranslation.Tests;

public class FuncMachineTranslatorFixture
{
    [Fact]
    public async Task TranslateAsync_ShouldPassTextsAndLanguagesToTheDelegate()
    {
        using var cancellation = new CancellationTokenSource();
        var translator = new FuncMachineTranslator((texts, source, target, cancellationToken) =>
        {
            cancellationToken.ShouldBe(cancellation.Token);

            return Task.FromResult<IReadOnlyList<string>>(texts.Select(t => $"{t} ({source}->{target})").ToList());
        });

        var result = await translator.TranslateAsync(["Hello {{name}}", "Bye"], "en", "de", cancellation.Token);

        result.ShouldBe(["Hello {{name}} (en->de)", "Bye (en->de)"]);
    }

    [Fact]
    public async Task TranslateAsync_NoTexts_ShouldNotCallTheDelegate()
    {
        var translator = new FuncMachineTranslator((_, _, _, _) => throw new InvalidOperationException());

        (await translator.TranslateAsync([], "en", "de")).ShouldBeEmpty();
    }

    [Fact]
    public async Task TranslateAsync_WrongNumberOfResults_ShouldThrow()
    {
        var translator = new FuncMachineTranslator((_, _, _, _) => Task.FromResult<IReadOnlyList<string>>(["a"]));
        var nullTranslator = new FuncMachineTranslator((_, _, _, _) => Task.FromResult<IReadOnlyList<string>>(null));

        (await Should.ThrowAsync<MachineTranslationException>(() => translator.TranslateAsync(["a", "b"], "en", "de")))
            .Message.ShouldBe("The translation delegate returned 1 texts for 2 texts.");
        await Should.ThrowAsync<MachineTranslationException>(() => nullTranslator.TranslateAsync(["a"], "en", "de"));
    }

    [Fact]
    public async Task InvalidArguments_ShouldThrow()
    {
        Should.Throw<ArgumentNullException>(() => new FuncMachineTranslator(null));
        await Should.ThrowAsync<ArgumentNullException>(() => new FuncMachineTranslator((_, _, _, _) => null).TranslateAsync(null, "en", "de"));
        await Should.ThrowAsync<ArgumentNullException>(() => ((IMachineTranslator)null).TranslateAsync("a", "en", "de"));
        await Should.ThrowAsync<ArgumentNullException>(() => Substitute.For<IMachineTranslator>().TranslateAsync((string)null, "en", "de"));
    }

    [Fact]
    public async Task TranslateAsync_SingleText_ShouldUseTheBatch()
    {
        var translator = Substitute.For<IMachineTranslator>();
        translator.TranslateAsync(Arg.Is<IReadOnlyList<string>>(t => t.Count == 1 && t[0] == "Hello"), "en", "de", Arg.Any<CancellationToken>())
            .Returns(["Hallo"]);

        (await translator.TranslateAsync("Hello", "en", "de")).ShouldBe("Hallo");
    }
}
