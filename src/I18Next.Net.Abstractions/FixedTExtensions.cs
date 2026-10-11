namespace I18Next.Net;

/// <summary>
///     Extension methods creating a <see cref="FixedT" /> for an <see cref="II18Next" /> instance.
/// </summary>
public static class FixedTExtensions
{
    /// <summary>
    ///     Returns a translator with a fixed language, namespace and key prefix, the equivalent of the i18next
    ///     <c>getFixedT</c>.
    /// </summary>
    /// <param name="i18Next">The instance used to translate.</param>
    /// <param name="language">The language to translate into or <c>null</c> to use the current language.</param>
    /// <param name="namespace">
    ///     The namespace used for keys without namespace prefix or <c>null</c> to use the default namespace.
    /// </param>
    /// <param name="keyPrefix">The prefix put in front of each key, separated by a dot, or <c>null</c>.</param>
    /// <returns>The translator with the fixed values.</returns>
    /// <exception cref="System.ArgumentNullException"><paramref name="i18Next" /> is <c>null</c>.</exception>
    public static FixedT GetFixedT(this II18Next i18Next, string language = null, string @namespace = null, string keyPrefix = null)
    {
        return new FixedT(i18Next, language, @namespace, keyPrefix);
    }
}
