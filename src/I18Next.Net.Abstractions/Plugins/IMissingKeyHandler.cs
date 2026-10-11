using System.Threading.Tasks;

namespace I18Next.Net.Plugins;

/// <summary>
///     A handler for keys missing in the translation backend, e.g. to log, count or save them like the i18next
///     <c>saveMissing</c> option.
/// </summary>
public interface IMissingKeyHandler
{
    /// <summary>
    ///     Called by the translator when a key is missing in a namespace of a language it looked into. The translator
    ///     awaits the handlers one after another before it continues with the fallbacks, so exceptions fail the
    ///     translation and slow handlers delay it.
    /// </summary>
    /// <param name="sender">The calling translator.</param>
    /// <param name="args">The language, namespace and key of the missing translation.</param>
    /// <returns>A task completing when the missing key has been handled.</returns>
    Task HandleMissingKeyAsync(object sender, MissingKeyEventArgs args);
}
