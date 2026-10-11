namespace I18Next.Net;

/// <summary>
///     Creates configured <see cref="II18Next" /> instances, e.g. from the services and options registered in a
///     dependency injection container.
/// </summary>
public interface II18NextFactory
{
    /// <summary>
    ///     Creates a new <see cref="II18Next" /> instance with the configured backend, translator, language detector and
    ///     options.
    /// </summary>
    /// <returns>The new instance.</returns>
    II18Next CreateInstance();
}
