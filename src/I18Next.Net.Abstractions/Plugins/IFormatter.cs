namespace I18Next.Net.Plugins
{
    public interface IFormatter
    {
        bool CanFormat(object value, string format, string language);

        string Format(object value, string format, string language);
    }
}
