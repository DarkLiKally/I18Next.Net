using System.CommandLine;
using System.Threading.Tasks;

namespace I18Next.Net.Tool.Commands;

internal static class SortCommand
{
    public static Command Create()
    {
        var locales = new LocalesOptions();
        var check = new Option<bool>("--check", "--dry-run") { Description = "Does not write the files and fails when they are not sorted and formatted." };

        var command = new Command("sort", "Sorts the keys of the translation files alphabetically and formats them with two spaces indentation.");
        locales.AddTo(command);
        command.Options.Add(check);

        command.SetToolAction((parseResult, output, _) =>
        {
            var files = locales.GetFiles(parseResult);
            files.EnsureLanguages();

            var dryRun = parseResult.GetValue(check);
            var changed = 0;

            foreach (var file in files.LoadExisting())
            {
                file.Document.Sort();

                if (!file.Save(dryRun))
                    continue;

                changed++;
                output.WriteLine(dryRun ? $"{file.Path}: not sorted or formatted." : $"{file.Path}: sorted.");
            }

            if (changed == 0)
                output.WriteLine("All translation files are sorted.");

            return Task.FromResult(dryRun && changed > 0 ? ExitCodes.ProblemsFound : ExitCodes.Success);
        });

        return command;
    }
}
