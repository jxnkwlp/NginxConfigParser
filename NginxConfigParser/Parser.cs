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
        var keyEndSymbol = text.IndexOf(' ');
        var endSymbol = text.IndexOf(';');
        var commentSymbol = text.IndexOf('#');
        var groupStartSymbol = text.IndexOf('{');

        if (text.Length == 0)
        {
            return;
        }

        if (text[0] == '#')
        {
            var commentToken = new CommentToken(text.Trim().TrimStart('#').Trim());
            AddToken(commentToken);
            return;
        }
        else if (text[0] == '\'')
        {
            if (_currentToken != null && _currentToken is ValueToken valueToken)
            {
                valueToken.Value += text.Trim();

                if (endSymbol > -1)
                {
                    valueToken.Value = valueToken.Value.TrimEnd(';').Replace("\'\'", null);
                    AddToken(valueToken);
                }

                return;
            }

            throw new Exception($"Unexpected quoted continuation at line {lineIndex}: {text}");
        }
        else if (text[0] == '}')
        {
            _currentGroupToken = _currentGroupToken?.Parent;

            _currentToken = null;

            return;
        }

        string key = string.Empty;
        string value = string.Empty;
        string comment = string.Empty;

        if (keyEndSymbol > -1)
            key = text.Substring(0, keyEndSymbol);

        if (groupStartSymbol > keyEndSymbol)
        {
            value = text.Substring(keyEndSymbol + 1, groupStartSymbol - keyEndSymbol - 1);
        }
        else if (endSymbol > keyEndSymbol)
        {
            value = text.Substring(keyEndSymbol + 1, endSymbol - keyEndSymbol - 1);
        }
        else if (endSymbol == -1)
        {
            value = text.Substring(keyEndSymbol + 1);
        }

        if (commentSymbol > keyEndSymbol)
            comment = text.Substring(commentSymbol + 1).Trim().TrimStart('#').Trim();

        if (groupStartSymbol > -1)
        {
            var groupToken = new GroupToken(_currentGroupToken, key, value, comment);
            AddToken(groupToken);
            _currentGroupToken = groupToken;
            return;
        }

        if (groupStartSymbol == -1 && endSymbol == -1)
        {
            _currentToken = new ValueToken(_currentGroupToken, key, value, comment);
            return;
        }

        if (endSymbol > -1)
        {
            AddToken(new ValueToken(_currentGroupToken, key, value?.Trim(), comment));
            return;
        }

        throw new Exception($"Unable to parse line {lineIndex}: {text}");
    }
}
