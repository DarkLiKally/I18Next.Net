using System;
using System.CommandLine;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

using I18Next.Net.MachineTranslation;
using I18Next.Net.Tool.Commands;

namespace I18Next.Net.Tool;

internal static class ToolCommand
{
    public static RootCommand Create(ToolServices services)
    {
        return new RootCommand("Extracts, checks, sorts, converts, migrates and machine translates i18next translation files.")
        {
            ExtractCommand.Create(),
            CheckCommand.Create(),
            SortCommand.Create(),
            ConvertCommand.Create(),
            MigrateCommand.Create(),
            TranslateCommand.Create(services)
        };
    }

    public static async Task<int> InvokeAsync(string[] args, ToolServices services, TextWriter output = null, TextWriter error = null,
        CancellationToken cancellationToken = default)
    {
        var configuration = new InvocationConfiguration();

        if (output != null)
            configuration.Output = output;

        if (error != null)
            configuration.Error = error;

        var parseResult = Create(services).Parse(args);

        if (parseResult.Errors.Count == 0)
            return await parseResult.InvokeAsync(configuration, cancellationToken).ConfigureAwait(false);

        foreach (var parseError in parseResult.Errors)
            await configuration.Error.WriteLineAsync(parseError.Message).ConfigureAwait(false);

        await configuration.Error.WriteLineAsync("Run 'dotnet i18next --help' for usage information.").ConfigureAwait(false);

        return ExitCodes.Error;
    }

    public static void SetToolAction(this Command command, Func<ParseResult, TextWriter, CancellationToken, Task<int>> action)
    {
        command.SetAction(async (parseResult, cancellationToken) =>
        {
            try
            {
                return await action(parseResult, parseResult.InvocationConfiguration.Output, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception e) when (e is ToolException or MachineTranslationException or HttpRequestException or IOException or UnauthorizedAccessException)
            {
                await parseResult.InvocationConfiguration.Error.WriteLineAsync("error: " + e.Message).ConfigureAwait(false);

                return ExitCodes.Error;
            }
        });
    }
}
