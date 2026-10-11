using System;

using Avalonia;

namespace I18Next.Net.Avalonia;

public static class AppBuilderExtensions
{
    /// <summary>
    ///     Sets the instance translating the XAML with <see cref="TExtension" /> when the application is set up.
    /// </summary>
    /// <param name="builder">The application builder.</param>
    /// <param name="i18Next">The I18Next instance.</param>
    /// <returns>The application builder.</returns>
    public static AppBuilder UseI18Next(this AppBuilder builder, II18Next i18Next)
    {
        if (i18Next == null)
            throw new ArgumentNullException(nameof(i18Next));

        return builder.AfterSetup(_ => I18NextXaml.Instance = i18Next);
    }
}
