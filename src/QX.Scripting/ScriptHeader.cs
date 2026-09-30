using System.Text.RegularExpressions;

namespace Qx.Scripting;

/// <summary>
/// The directives in a script's leading comment block: <c>/// @name</c>, the name the script goes
/// by, and <c>/// @group</c>, the library group it belongs to.
/// </summary>
/// <remarks>
/// Only the comment lines before the first line of code count, so a directive quoted further down
/// in a string or a comment does not rename or regroup the script.
/// </remarks>
/// <param name="Name">The declared name, or <see langword="null"/> when there is none.</param>
/// <param name="Group">The declared library group, or <see langword="null"/> when there is none.</param>
public sealed partial record ScriptHeader(string? Name, string? Group)
{
    const string NameKey = "name";
    const string GroupKey = "group";

    public static ScriptHeader Parse(string code)
    {
        ArgumentNullException.ThrowIfNull(code);
        string? name = null;
        string? group = null;
        foreach (Directive directive in Directives(code))
        {
            if (directive.Key == NameKey)
                name ??= directive.Value;
            else if (directive.Key == GroupKey)
                group ??= directive.Value;
        }
        return new ScriptHeader(name, group);
    }

    /// <summary>The script with its <c>/// @name</c> set to a name, added at the top when missing.</summary>
    public static string WithName(string code, string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return With(code, NameKey, name.Trim());
    }

    /// <summary>
    /// The script with its <c>/// @group</c> set to a group, added at the top when missing, or
    /// removed when the group is empty.
    /// </summary>
    public static string WithGroup(string code, string? group) =>
        With(code, GroupKey, string.IsNullOrWhiteSpace(group) ? null : group.Trim());

    static string With(string code, string key, string? value)
    {
        ArgumentNullException.ThrowIfNull(code);
        string newline = code.Contains("\r\n", StringComparison.Ordinal) ? "\r\n" : "\n";
        if (Directives(code).FirstOrDefault(directive => directive.Key == key) is { } existing)
        {
            string replacement = value is null ? "" : $"/// @{key} {value}";
            int end = existing.Start + existing.Length;
            if (value is null && end < code.Length)
                end += code.AsSpan(end).StartsWith("\r\n") ? 2 : 1;
            return string.Concat(code.AsSpan(0, existing.Start), replacement, code.AsSpan(end));
        }
        if (value is null)
            return code;
        int insert = key == GroupKey && Directives(code).FirstOrDefault(directive => directive.Key == NameKey) is { } name
            ? LineEnd(code, name.Start + name.Length, newline)
            : 0;
        return string.Concat(code.AsSpan(0, insert), $"/// @{key} {value}{newline}", code.AsSpan(insert));
    }

    static int LineEnd(string code, int position, string newline) =>
        position >= code.Length ? code.Length : position + (code.AsSpan(position).StartsWith(newline) ? newline.Length : 1);

    static IEnumerable<Directive> Directives(string code)
    {
        int start = 0;
        while (start < code.Length)
        {
            int end = code.IndexOf('\n', start);
            int length = (end < 0 ? code.Length : end) - start;
            if (length > 0 && code[start + length - 1] == '\r')
                length--;
            string line = code.Substring(start, length);
            string trimmed = line.Trim();
            if (trimmed.Length > 0 && !trimmed.StartsWith("//", StringComparison.Ordinal))
                yield break;
            Match match = DirectiveLine().Match(line);
            if (match.Success)
                yield return new Directive(match.Groups["key"].Value.ToLowerInvariant(), match.Groups["value"].Value.Trim(), start, length);
            if (end < 0)
                yield break;
            start = end + 1;
        }
    }

    sealed record Directive(string Key, string Value, int Start, int Length);

    [GeneratedRegex(@"^\s*///\s*@(?<key>name|group)[^\S\r\n]+(?<value>\S.*?)\s*$", RegexOptions.IgnoreCase)]
    private static partial Regex DirectiveLine();
}
