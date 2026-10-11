using System;

using Avalonia;

namespace I18Next.Net.Avalonia;

/// <summary>
///     Extension methods for <see cref="AppBuilder" /> to translate Avalonia applications with I18Next.
/// </summary>
public static class AppBuilderExtensions
{
    /// <summary>
    ///     Sets the instance translating the XAML with <see cref="TExtension" /> (<see cref="I18NextXaml.Instance" />) when
    ///     the application is set up.
    /// </summary>
    /// <param name="builder">The application builder.</param>
    /// <param name="i18Next">The I18Next instance.</param>
    /// <returns>The application builder.</returns>
    /// <exception cref="ArgumentNullException">If <paramref name="i18Next" /> is <c>null</c>.</exception>
    public static AppBuilder UseI18Next(this AppBuilder builder, II18Next i18Next)
    {
        if (i18Next == null)
            throw new ArgumentNullException(nameof(i18Next));

        return builder.AfterSetup(_ => I18NextXaml.Instance = i18Next);
    }
}
