using System.Collections.Generic;
using System.Linq;

namespace NginxConfigParser
{
    public class CommentToken : IToken
    {
        public string Content { get; set; }

        public CommentToken(string text)
        {
            Content = text;
        }

        public override string ToString() => $"# {Content}";
    }

    public class ValueToken : IValueToken
    {
        public ValueToken(GroupToken parent, string key, string value = null, string comment = null)
        {
            Key = key;
            Value = value;
            Comment = comment;
            Parent = parent;
        }

        public GroupToken Parent { get; }

        public string Key { get; }
        public string Value { get; set; }
        public string Comment { get; set; }

        public override string ToString()
        {
            if (!string.IsNullOrWhiteSpace(Comment))
                return $"{Key}  {Value};  # {Comment}";
            else
                return $"{Key}  {Value};";
        }
    }

    public class GroupToken : IValueToken
    {
        public GroupToken(GroupToken parent, string key, string value = null, string comment = null)
        {
            Key = key;
            Value = value == null ? value : value.Trim();
            Comment = comment;
            Tokens = new List<IToken>();
            Parent = parent;
        }

        public string Key { get; }
        public string Value { get; set; }
        public string Comment { get; set; }

        public IList<IToken> Tokens { get; }

        public GroupToken Parent { get; }

        public void Add(IToken token)
        {
            Tokens.Add(token);
        }

        public void Add(string key, string value, string comment = null)
        {
            Add(new ValueToken(this, key, value, comment));
        }

        public override string ToString()
        {
            if (!string.IsNullOrWhiteSpace(Comment))
                return $"{Key} {Value}; # {Comment} => [{Tokens.Count()}]";
            else
                return $"{Key} {Value} => [{Tokens.Count()}]";
        }

    }

    public class RootToken
    {
    }
}
