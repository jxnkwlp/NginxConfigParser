using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace NginxConfigParser;

/// <summary>
///  Represents an nginx configuration file operation object.
/// </summary>
public class NginxConfig
{
    private static readonly Regex KeyRegex = new(@"^[\w]+(\[\d+\])?$");
    private static readonly Encoding Utf8NoBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

    private readonly Parser _parser;

    private IList<IToken> _tokens = new List<IToken>();

    protected NginxConfig(Parser parser)
    {
        _parser = parser;

        Initial();
    }

    /// <summary>
    ///  Create an new
    /// </summary>
    public static NginxConfig Create()
    {
        var parser = new Parser(string.Empty);

        return new NginxConfig(parser);
    }

    /// <summary>
    ///  Load from specific file
    /// </summary>
    /// <param name="fileName">The file path</param>
    /// <returns><see cref="NginxConfig"/></returns>
    /// <exception cref="ArgumentException"></exception>
    /// <exception cref="FileNotFoundException"></exception>
    public static NginxConfig LoadFrom(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new ArgumentException($"'{nameof(fileName)}' cannot be null or whitespace.", nameof(fileName));
        }

        if (!File.Exists(fileName))
        {
            throw new FileNotFoundException(fileName);
        }

        var content = File.ReadAllText(fileName);

        return Load(content);
    }

    /// <summary>
    ///  Load from file content
    /// </summary>
    /// <param name="content">The string of file content</param>
    /// <returns><see cref="NginxConfig"/></returns>
    /// <exception cref="ArgumentNullException"></exception>
    public static NginxConfig Load(string content)
    {
        if (content is null)
        {
            throw new ArgumentNullException(nameof(content));
        }

        var parser = new Parser(content);

        return new NginxConfig(parser);
    }

    private void Initial()
    {
        _parser.Parse();
        _tokens = _parser.GetTokens().ToList();
    }

    /// <summary>
    ///  Get all values
    /// </summary>
    public IEnumerable<IToken> GetTokens() => _tokens;

    /// <summary>
    ///  Read value from given the key path.
    ///  if the key not exist, will return null.
    /// </summary>
    /// <exception cref="ArgumentException"></exception>
    public IValueToken GetToken(string keyPath)
    {
        if (string.IsNullOrWhiteSpace(keyPath))
        {
            throw new ArgumentException($"'{nameof(keyPath)}' cannot be null or whitespace.", nameof(keyPath));
        }

        return GetTokenFromPath(keyPath);
    }

    /// <summary>
    ///  Read value list from given the key path.
    /// </summary>
    /// <exception cref="ArgumentException"></exception>
    public IList<IValueToken> GetTokens(string keyPath)
    {
        if (string.IsNullOrWhiteSpace(keyPath))
        {
            throw new ArgumentException($"'{nameof(keyPath)}' cannot be null or whitespace.", nameof(keyPath));
        }

        return GetTokensFromPath(keyPath);
    }

    /// <summary>
    ///  Read all values from specific group key
    /// </summary>
    /// <exception cref="ArgumentException"></exception>
    public GroupToken GetGroup(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException($"'{nameof(key)}' cannot be null or whitespace.", nameof(key));
        }

        var find = _tokens.Where(x => x is GroupToken).FirstOrDefault(x => ((IValueToken)x).Key == key);

        return find as GroupToken;
    }

    /// <summary>
    ///  Read value from given the key path.
    ///  if the key not exist, will return null.
    /// </summary>
    /// <exception cref="ArgumentException"></exception>
    public IValueToken this[string keyPath]
    {
        get
        {
            if (string.IsNullOrWhiteSpace(keyPath))
            {
                throw new ArgumentException($"'{nameof(keyPath)}' cannot be null or whitespace.", nameof(keyPath));
            }

            return GetTokenFromPath(keyPath);
        }
    }

    /// <summary>
    ///  Add or update value by the key path
    /// </summary>
    /// <param name="keyPath">The key path</param>
    /// <param name="value">The string value</param>
    /// <param name="addAsGroup">Add as group when key path not found</param>
    /// <param name="comment">The comment</param>
    /// <exception cref="ArgumentException"></exception>
    /// <exception cref="IndexOutOfRangeException"></exception>
    public NginxConfig AddOrUpdate(string keyPath, string value, bool addAsGroup = false, string comment = null)
    {
        if (string.IsNullOrWhiteSpace(keyPath))
        {
            throw new ArgumentException($"'{nameof(keyPath)}' cannot be null or whitespace.", nameof(keyPath));
        }

        var tokens = _tokens;

        var paths = keyPath.Split(':');

        GroupToken groupToken = null;

        int length = paths.Length;

        for (int i = 0; i < length; i++)
        {
            var (key, index, _) = ResolveKey(paths[i]);

            IToken find = null;
            IEnumerable<IToken> findTokens;
            if (groupToken == null)
                findTokens = FindTokens(tokens, key);
            else
                findTokens = FindTokens(groupToken.Tokens, key);

            if (index > findTokens.Count())
                throw new IndexOutOfRangeException($"The key '{key}' index must be <= {findTokens.Count()}");

            if (index <= findTokens.Count() - 1)
                find = findTokens.ElementAt(index);

            if (find != null)
            {
                if (find is ValueToken valueToken)
                {
                    if (i == length - 1)
                    {
                        valueToken.Value = value;
                        valueToken.Comment = comment;
                        break;
                    }

                    throw new Exception($"The token '{find}' already exists.");
                }

                groupToken = (GroupToken)find;
            }
            else if (i == length - 1)
            {
                IValueToken newToken = new ValueToken(groupToken, key, value, comment);

                if (addAsGroup)
                {
                    newToken = new GroupToken(groupToken, key, value, comment);
                }

                if (groupToken == null)
                {
                    tokens.Add(newToken);
                }
                else
                {
                    groupToken.Add(newToken);
                }
            }
            else
            {
                var newGroupToken = new GroupToken(groupToken, key);
                if (groupToken == null)
                {
                    tokens.Add(newGroupToken);
                }
                else
                {
                    groupToken.Add(newGroupToken);
                }
                groupToken = newGroupToken;
            }
        }

        return this;
    }

    /// <summary>
    ///  Remove the value by key path
    /// </summary>
    /// <exception cref="ArgumentException"></exception>
    /// <exception cref="IndexOutOfRangeException"></exception>
    public NginxConfig Remove(string keyPath)
    {
        if (string.IsNullOrWhiteSpace(keyPath))
        {
            throw new ArgumentException($"'{nameof(keyPath)}' cannot be null or whitespace.", nameof(keyPath));
        }

        var tokens = _tokens;

        var paths = keyPath.Split(':');

        GroupToken groupToken = null;

        int length = paths.Length;

        for (int i = 0; i < length; i++)
        {
            var (key, index, hasIndex) = ResolveKey(paths[i]);

            IToken find = null;
            IEnumerable<IToken> findTokens;

            if (groupToken == null)
                findTokens = FindTokens(tokens, key);
            else
                findTokens = FindTokens(groupToken.Tokens, key);

            if (i == length - 1)
            {
                IList<IToken> targetList = groupToken == null ? tokens : groupToken.Tokens;

                if (hasIndex)
                {
                    var findTokensCount = findTokens.Count();
                    if (index < 0 || index >= findTokensCount)
                        throw new IndexOutOfRangeException($"The key '{key}' index must be >= 0 and < {findTokensCount}");

                    targetList.Remove(findTokens.ElementAt(index));
                }
                else
                {
                    foreach (var item in findTokens)
                    {
                        targetList.Remove(item);
                    }
                }
            }
            else
            {
                var findTokensCount = findTokens.Count();
                if (index > findTokensCount)
                    throw new IndexOutOfRangeException($"The key '{key}' index must be <= {findTokensCount}");

                if (index <= findTokensCount - 1)
                    find = findTokens.ElementAt(index);

                if (find != null && find is GroupToken groupToken1)
                {
                    groupToken = groupToken1;
                }
                else
                {
                    // not found , break.
                    break;
                }
            }
        }

        return this;
    }

    /// <summary>
    ///  Save the configuration content to specific file
    /// </summary>
    /// <param name="fileName">The file path</param>
    /// <exception cref="ArgumentException"></exception>
    public void Save(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new ArgumentException($"'{nameof(fileName)}' cannot be null or whitespace.", nameof(fileName));
        }

        Save(fileName, Utf8NoBom);
    }

    /// <summary>
    ///  Save the configuration content to specific file
    /// </summary>
    /// <param name="fileName">The file path</param>
    /// <param name="encoding">The file encoding</param>
    /// <exception cref="ArgumentException"></exception>
    /// <exception cref="ArgumentNullException"></exception>
    public void Save(string fileName, Encoding encoding)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new ArgumentException($"'{nameof(fileName)}' cannot be null or whitespace.", nameof(fileName));
        }

        if (encoding is null)
        {
            throw new ArgumentNullException(nameof(encoding));
        }

        using StringWriter sw = new StringWriter(new StringBuilder());

        WriteTokenString(_tokens, sw, 0);
        using (StreamWriter fsWriter = new StreamWriter(fileName, false, encoding))
        {
            fsWriter.NewLine = Environment.NewLine;
            fsWriter.Write(sw);
        }
    }

    /// <summary>
    ///  Return configuration file content
    /// </summary>
    public override string ToString()
    {
        using StringWriter sw = new StringWriter(new StringBuilder());

        WriteTokenString(_tokens, sw, 0);

        return sw.GetStringBuilder().ToString();
    }

    private void WriteTokenString(IEnumerable<IToken> tokens, TextWriter textWriter, int level = 0)
    {
        textWriter.NewLine = Environment.NewLine;

        var tokenList = tokens as IList<IToken> ?? tokens.ToList();
        var wroteAny = false;

        foreach (var token in tokenList)
        {
            if (token is CommentToken comment)
            {
                textWriter.WriteLine(PadLeftSpace(comment.ToString(), level));
                wroteAny = true;
            }
            else if (token is ValueToken value)
            {
                textWriter.WriteLine(PadLeftSpace(value.ToString(), level));
                wroteAny = true;
            }
            else if (token is GroupToken group)
            {
                if (wroteAny)
                    textWriter.WriteLine();

                if (!string.IsNullOrWhiteSpace(group.Comment))
                    textWriter.WriteLine(PadLeftSpace($"{group.Key}  {group.Value} {{ # {group.Comment}", level));
                else
                    textWriter.WriteLine(PadLeftSpace($"{group.Key}  {group.Value} {{ ", level));

                WriteTokenString(group.Tokens, textWriter, level + 1);

                textWriter.WriteLine(PadLeftSpace("}", level));
                wroteAny = true;
            }
        }
    }

    private string PadLeftSpace(string text, int level = 0)
    {
        return text.PadLeft(text.Length + (level * 2), ' ');
    }

    private IValueToken GetTokenFromPath(string keyPath)
    {
        var tokens = _tokens;

        var paths = keyPath.Split(':');

        IValueToken result = null;

        foreach (var key in paths)
        {
            var (keyName, index, _) = ResolveKey(key);

            result = FindToken(tokens, keyName, index);
            if (result != null)
            {
                if (result is GroupToken groupToken)
                    tokens = groupToken.Tokens.ToList();
            }
            else
            {
                break;
            }
        }

        return result;
    }

    private IList<IValueToken> GetTokensFromPath(string keyPath)
    {
        var tokens = _tokens;

        var paths = keyPath.Split(':');

        IEnumerable<IValueToken> result = Array.Empty<IValueToken>();

        IValueToken current = null;

        for (int i = 0; i < paths.Length; i++)
        {
            var (keyName, index, _) = ResolveKey(paths[i]);

            if (i == paths.Length - 1)
            {
                result = FindTokens(tokens, keyName);
            }
            else
            {
                current = FindToken(tokens, keyName, index);

                if (current != null && current is GroupToken groupToken)
                {
                    tokens = groupToken.Tokens.ToList();
                }
                else
                {
                    return new List<IValueToken>();
                }
            }
        }

        return result.ToList();
    }

    private static IValueToken FindToken(IEnumerable<IToken> tokens, string key, int index = 0)
    {
        return tokens.Where(x => x is IValueToken valueToken && valueToken.Key == key).ElementAtOrDefault(index) as IValueToken;
    }

    private static IEnumerable<IValueToken> FindTokens(IEnumerable<IToken> tokens, string key)
    {
        return tokens.Where(x => x is IValueToken valueToken && valueToken.Key == key).Cast<IValueToken>().ToArray();
    }

    private (string key, int index, bool hasIndex) ResolveKey(string key)
    {
        if (!KeyRegex.IsMatch(key))
        {
            throw new Exception($"The key '{key}' format is incorrect");
        }

        var numberStartSymbol = key.IndexOf('[');

        var index = 0;
        string keyName = key;
        var hasIndex = false;

        if (numberStartSymbol > 0)
        {
            hasIndex = true;
            var numberStartIndex = numberStartSymbol + 1;

            if (!int.TryParse(key.Substring(numberStartIndex, key.Length - 1 - numberStartIndex), out index))
            {
                throw new Exception($"The key '{key}' index format is incorrect");
            }

            keyName = key.Substring(0, numberStartSymbol);
        }

        return (keyName, index, hasIndex);
    }
}
