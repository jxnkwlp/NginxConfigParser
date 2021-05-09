namespace NginxConfigParser
{
    public interface IToken
    {
    }

    public interface IValueToken : IToken
    {
        string Key { get; }
        string Value { get; set; }
        string Comment { get; set; }
    }

}
