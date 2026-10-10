using System;
using System.Linq;
using System.Threading.Tasks;

using Shouldly;

using Xunit;

namespace I18Next.Net.Tool.Tests.Commands;

public class ToolCommandFixture : IDisposable
{
    private readonly ToolTestContext _context = new();

    public void Dispose()
    {
        _context.Dispose();
    }

    [Fact]
    public void Create_ShouldContainAllCommands()
    {
        ToolCommand.Create(new ToolServices()).Subcommands.Select(c => c.Name).ShouldBe(["extract", "check", "sort", "convert", "migrate", "translate"]);
    }

    [Theory]
    [InlineData("--help")]
    [InlineData("extract", "--help")]
    [InlineData("check", "-h")]
    [InlineData("sort", "--help")]
    [InlineData("convert", "--help")]
    [InlineData("migrate", "--help")]
    [InlineData("translate", "--help")]
    public async Task Help_ShouldBePrinted(params string[] args)
    {
        (await _context.RunAsync(args)).ShouldBe(ExitCodes.Success);

        _context.Output.ShouldContain("Usage:");
        _context.Output.ShouldContain("Options:");
    }

    [Theory]
    [InlineData]
    [InlineData("unknown")]
    [InlineData("sort", "--unknown")]
    [InlineData("extract", "--default-value", "other")]
    [InlineData("translate", "--provider", "google")]
    [InlineData("convert", "only-input")]
    public async Task InvalidArguments_ShouldFailWithExitCode2(params string[] args)
    {
        (await _context.RunAsync(args)).ShouldBe(ExitCodes.Error);

        _context.Error.ShouldContain("Run 'dotnet i18next --help' for usage information.");
    }

    [Fact]
    public void ToolServices_ShouldReadTheEnvironment()
    {
        var services = new ToolServices();

        services.HttpClient.ShouldNotBeNull();
        services.GetEnvironmentVariable("PATH").ShouldBe(Environment.GetEnvironmentVariable("PATH"));
    }
}
