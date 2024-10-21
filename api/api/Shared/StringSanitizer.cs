using System.Text.RegularExpressions;

namespace api.Shared;
public static class StringSanitizer
{
    public static string Sanitize(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        input = Regex.Replace(input, "<script.*?>.*?</script>", string.Empty, RegexOptions.IgnoreCase);

        input = Regex.Replace(input, "<.*?>", string.Empty);

        input = System.Net.WebUtility.HtmlEncode(input);

        return input;
    }
}