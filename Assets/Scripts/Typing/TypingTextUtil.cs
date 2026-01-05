using System.Text;

public static class TypingTextUtil
{
    // 规则：只保留 A-Z / a-z；转小写；去掉空格、标点、数字、其他字符
    public static string NormalizeLettersOnly(string s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        var sb = new StringBuilder(s.Length);

        for (int i = 0; i < s.Length; i++)
        {
            char c = s[i];
            if (c >= 'A' && c <= 'Z')
            {
                sb.Append((char)(c + 32)); // to lower
            }
            else if (c >= 'a' && c <= 'z')
            {
                sb.Append(c);
            }
        }

        return sb.ToString();
    }
}
