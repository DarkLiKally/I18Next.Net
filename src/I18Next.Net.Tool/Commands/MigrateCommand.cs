using System.CommandLine;
using System.Threading.Tasks;

using I18Next.Net.Tool.Resources;

namespace I18Next.Net.Tool.Commands;

internal static class MigrateCommand
{
    public static Command Create()
    {
        var locales = new LocalesOptions();
        var check = new Option<bool>("--check", "--dry-run") { Description = "Does not write the files and fails when they contain v3 plurals." };

        var command = new Command("migrate",
            "Converts i18next JSON v3 plurals (key, key_plural, key_0 to key_n) to v4 plurals (key_one, key_other) of the CLDR plural categories.");
        locales.AddTo(command);
        command.Options.Add(check);

        command.SetToolAction((parseResult, output, _) =>
        {
            var files = locales.GetFiles(parseResult);
            files.EnsureLanguages();

            var dryRun = parseResult.GetValue(check);
            var changed = 0;
            var conflicts = 0;

            foreach (var file in files.LoadExisting())
            {
                var migration = new PluralMigration(file.Language);
                var renamed = migration.Migrate(file.Document.Root);

                foreach (var conflict in migration.Conflicts)
                    output.WriteLine($"{file.Path}: {conflict}");

                conflicts += migration.Conflicts.Count;

                if (renamed == 0 || !file.Save(dryRun))
                    continue;

                changed++;
                output.WriteLine(dryRun ? $"{file.Path}: would rename {renamed} keys." : $"{file.Path}: renamed {renamed} keys.");
            }

            if (changed == 0 && conflicts == 0)
                output.WriteLine("No v3 plurals found.");

            return Task.FromResult((dryRun && changed > 0) || conflicts > 0 ? ExitCodes.ProblemsFound : ExitCodes.Success);
        });

        return command;
    }
}
