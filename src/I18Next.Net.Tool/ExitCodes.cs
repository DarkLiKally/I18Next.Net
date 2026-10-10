namespace I18Next.Net.Tool;

internal static class ExitCodes
{
    public const int Success = 0;

    /// <summary>
    ///     Problems were found or files would change in check mode.
    /// </summary>
    public const int ProblemsFound = 1;

    /// <summary>
    ///     Invalid arguments, unreadable files or failed requests.
    /// </summary>
    public const int Error = 2;
}
