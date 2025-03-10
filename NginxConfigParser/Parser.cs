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
        string line = string.Empty;

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
        // comment row
        if (text[0] == '#')
        {
            var commentToken = new CommentToken(text.Trim().TrimStart('#').Trim());
            AddToken(commentToken);
            return;
        }
        // group ending
        if (text[0] == '}')
        {
            _currentGroupToken = _currentGroupToken?.Parent;
    
            _currentToken = null;
    
            return;
        }
    
        var key = string.Empty;
        var value = string.Empty;
        var comment = string.Empty;
        int keyEndSymbol = -1, groupStartSymbol = -1, commendSymbol = -1, endSymbol = -1;
        bool matchKey = false,
             matchValue = false,
             matchEnd = false,
             isBetweenSingleQuote = false,
             isBetweenDoubleQuote = false;
        for (var i = 0; i < text.Length; i++)
        {
            switch (text[i])
            {
                // match single quote
                case '\'':
                    isBetweenSingleQuote = !isBetweenSingleQuote;
                    break;
                // continue if in single quote
                case not '\'' when isBetweenSingleQuote:
                    break;
                // match double quote
                case '"':
                    isBetweenDoubleQuote = !isBetweenDoubleQuote;
                    break;
                // continue if in double quote
                case not '"' when isBetweenDoubleQuote:
                    break;
                // match the token ending
                case ';':
                    endSymbol = i;
                    value = text.Substring(keyEndSymbol + 1, endSymbol - keyEndSymbol - 1).Trim();
                    matchEnd = true;
                    matchValue = true;
                    break;
                // match the group beginning
                case '{':
                    groupStartSymbol = i;
                    value = text.Substring(keyEndSymbol + 1, groupStartSymbol - keyEndSymbol - 1).Trim();
                    matchValue = true;
                    break;
                // won't match group ending here
                // match comment beginning
                case '#':
                    commendSymbol = i;
                    comment = text.Substring(commendSymbol).Trim().TrimStart('#').Trim();
                    goto CommentBreakCheck;
                // match the first space to match token key
                case ' ' when !matchKey:
                    keyEndSymbol = i;
                    matchKey = true;
                    key = text.Substring(0, keyEndSymbol).Trim();
                    break;
            }
        }
    
        CommentBreakCheck:
        // WARNING: Here you need to consider whether to support cross-line quote mark pairs
        if (isBetweenSingleQuote || isBetweenDoubleQuote)
            throw new Exception($"Unpaired quote detected at line {lineIndex+1}!");
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
    
        throw new Exception("Some thing wrong");
    }
}
