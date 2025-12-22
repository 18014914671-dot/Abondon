using System.Text;
using System.Text.RegularExpressions;

public static class TextNormalizer
{
    /// <summary>
    /// 用于匹配用的“统一文本”：
    /// - Trim
    /// - ToLower
    /// - 把 '_' '-' 视作空格（关键：避免 3_star -> 3star）
    /// - 去掉其它标点，只保留字母/数字/空白
    /// - 多空格压成单空格
    /// </summary>
    public static string Normalize(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return "";

        // 关键：先把常见分隔符转成空格
        string s = input.Trim()
                        .ToLowerInvariant()
                        .Replace('_', ' ')
                        .Replace('-', ' ');

        var sb = new StringBuilder(s.Length);
        foreach (char c in s)
        {
            if (char.IsLetterOrDigit(c) || char.IsWhiteSpace(c))
                sb.Append(c);
        }

        return Regex.Replace(sb.ToString(), @"\s+", " ").Trim();
    }

    public static string[] SplitTokens(string normalized)
    {
        if (string.IsNullOrWhiteSpace(normalized)) return System.Array.Empty<string>();
        return normalized.Split(new[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);
    }
}
