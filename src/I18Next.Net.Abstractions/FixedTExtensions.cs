namespace I18Next.Net;

public static class FixedTExtensions
{
    /// <summary>
    ///     Returns a translator with a fixed language, namespace and key prefix, the equivalent of the i18next
    ///     <c>getFixedT</c>.
    /// </summary>
    public static FixedT GetFixedT(this II18Next i18Next, string language = null, string @namespace = null, string keyPrefix = null)
    {
        return new FixedT(i18Next, language, @namespace, keyPrefix);
    }
}
