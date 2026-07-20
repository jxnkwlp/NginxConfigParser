using System;
using System.Collections.Generic;
using System.IO;

namespace NginxConfigParser;

public class Parser
{
    private readonly string _content = string.Empty;
    private GroupToken _currentGroupToken;
    private ValueToken _currentToken;

    private readonly List<IToken> _tokens = new List<IToken>();

    public Parser(string content)
    {
        _content = content ?? throw new ArgumentNullException(nameof(content));
    }

    public IReadOnlyList<IToken> GetTokens() => _tokens;

    public void Parse()
    {
        var lineIndex = 0;

        StringReader sr = new StringReader(_content);
        string line;

        while ((line = sr.ReadLine()) != null)
        {
            lineIndex++;

            if (string.IsNullOrEmpty(line))
                continue;

            var text = line.Trim();

            ParseLine(text, lineIndex);
        }
    }

    private void AddToken(IToken token)
    {
        if (_currentGroupToken != null)
            _currentGroupToken.Tokens.Add(token);
        else _tokens.Add(token);
    }

    private void ParseLine(string text, int lineIndex)
    {
        if (text.Length == 0)
        {
            return;
        }

        // Full-line comment
        if (text[0] == '#')
        {
            var commentToken = new CommentToken(text.Trim().TrimStart('#').Trim());
            AddToken(commentToken);
            return;
        }

        // Group ending
        if (text[0] == '}')
        {
            _currentGroupToken = _currentGroupToken?.Parent;
            _currentToken = null;
            return;
        }

        // Multi-line value continuation (historical support for lines starting with ')
        if (text[0] == '\'')
        {
            if (_currentToken != null)
            {
                _currentToken.Value += text.Trim();

                if (HasUnquotedStatementEnd(text))
                {
                    _currentToken.Value = _currentToken.Value.TrimEnd(';').Replace("\'\'", null);
                    AddToken(_currentToken);
                    _currentToken = null;
                }

                return;
            }

            throw new Exception($"Unexpected quoted continuation at line {lineIndex}: {text}");
        }

        var key = string.Empty;
        var value = string.Empty;
        var comment = string.Empty;
        var keyEndSymbol = -1;
        var groupStartSymbol = -1;
        var endSymbol = -1;
        var matchKey = false;
        var isBetweenSingleQuote = false;
        var isBetweenDoubleQuote = false;

        for (var i = 0; i < text.Length; i++)
        {
            var ch = text[i];

            // Toggle / stay inside single quotes (unless currently inside double quotes)
            if (ch == '\'' && !isBetweenDoubleQuote)
            {
                isBetweenSingleQuote = !isBetweenSingleQuote;
                continue;
            }

            if (isBetweenSingleQuote)
                continue;

            // Toggle / stay inside double quotes
            if (ch == '"')
            {
                isBetweenDoubleQuote = !isBetweenDoubleQuote;
                continue;
            }

            if (isBetweenDoubleQuote)
                continue;

            // Outside quotes: structural characters
            if (ch == ';')
            {
                endSymbol = i;
                if (keyEndSymbol >= 0)
                    value = text.Substring(keyEndSymbol + 1, endSymbol - keyEndSymbol - 1).Trim();
                break;
            }

            if (ch == '{')
            {
                groupStartSymbol = i;
                if (keyEndSymbol >= 0)
                    value = text.Substring(keyEndSymbol + 1, groupStartSymbol - keyEndSymbol - 1).Trim();
                break;
            }

            if (ch == '#')
            {
                if (keyEndSymbol >= 0)
                    value = text.Substring(keyEndSymbol + 1, i - keyEndSymbol - 1).Trim();
                comment = text.Substring(i + 1).Trim().TrimStart('#').Trim();
                break;
            }

            if (ch == ' ' && !matchKey)
            {
                keyEndSymbol = i;
                matchKey = true;
                key = text.Substring(0, keyEndSymbol).Trim();
            }
        }

        if (isBetweenSingleQuote || isBetweenDoubleQuote)
            throw new Exception($"Unpaired quote detected at line {lineIndex}: {text}");

        if (groupStartSymbol > -1)
        {
            // Inline comment after {: "server { # comment"
            if (string.IsNullOrEmpty(comment))
            {
                var afterGroup = groupStartSymbol + 1;
                if (afterGroup < text.Length)
                {
                    var inline = text.Substring(afterGroup).Trim();
                    if (inline.StartsWith("#", StringComparison.Ordinal))
                        comment = inline.TrimStart('#').Trim();
                }
            }

            var groupToken = new GroupToken(_currentGroupToken, key, value, comment);
            AddToken(groupToken);
            _currentGroupToken = groupToken;
            return;
        }

        if (endSymbol == -1 && groupStartSymbol == -1)
        {
            if (keyEndSymbol >= 0 && string.IsNullOrEmpty(value))
                value = text.Substring(keyEndSymbol + 1).Trim();

            _currentToken = new ValueToken(_currentGroupToken, key, value, comment);
            return;
        }

        if (endSymbol > -1)
        {
            // Inline comment after semicolon: key value; # comment
            if (string.IsNullOrEmpty(comment) && endSymbol + 1 < text.Length)
            {
                var afterEnd = text.Substring(endSymbol + 1).Trim();
                if (afterEnd.StartsWith("#", StringComparison.Ordinal))
                    comment = afterEnd.TrimStart('#').Trim();
            }

            AddToken(new ValueToken(_currentGroupToken, key, value?.Trim(), comment));
            return;
        }

        throw new Exception($"Unable to parse line {lineIndex}: {text}");
    }

    /// <summary>
    /// Returns true when the line contains a statement-ending ';' outside of quotes.
    /// </summary>
    private static bool HasUnquotedStatementEnd(string text)
    {
        return IndexOfUnquoted(text, ';') >= 0;
    }

    private static int IndexOfUnquoted(string text, char target)
    {
        var inSingle = false;
        var inDouble = false;

        for (var i = 0; i < text.Length; i++)
        {
            var ch = text[i];

            if (ch == '\'' && !inDouble)
            {
                inSingle = !inSingle;
                continue;
            }

            if (inSingle)
                continue;

            if (ch == '"')
            {
                inDouble = !inDouble;
                continue;
            }

            if (inDouble)
                continue;

            if (ch == target)
                return i;
        }

        return -1;
    }
}
