using System.Threading.Tasks;

namespace Localizer.Plugins;

/// <summary>
///     Abstraction for a handler which can handle the occurence of a missing key in the used translation backend.
/// </summary>
public interface IMissingKeyHandler
{
    /// <summary>
    ///     Called by a translator plugin when it was unable to find any value for a given key in the underlying translation
    ///     backend.
    /// </summary>
    /// <param name="sender">Reference to the calling translator plugin.</param>
    /// <param name="missingKey">Object containing various information about the missing key.</param>
    /// <returns>Awaitable Task.</returns>
    Task HandleMissingKeyAsync(object sender, MissingKeyEventArgs args);
}
