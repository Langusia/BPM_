using System.Text;
using System.Text.RegularExpressions;

namespace BPM.UI.Configuration;

public class BpmConventionBuilder
{
    private readonly List<Func<string, string>> _transforms = [];

    public BpmConventionBuilder RemoveCommandSuffix()
    {
        _transforms.Add(name => Regex.Replace(name, "Command$", ""));
        return this;
    }

    public BpmConventionBuilder RemoveQuerySuffix()
    {
        _transforms.Add(name => Regex.Replace(name, "Query$", ""));
        return this;
    }

    public BpmConventionBuilder ToKebabCase()
    {
        _transforms.Add(name =>
        {
            var sb = new StringBuilder();
            for (int i = 0; i < name.Length; i++)
            {
                var c = name[i];
                if (i > 0 && char.IsUpper(c) && !char.IsUpper(name[i - 1]))
                {
                    sb.Append('-');
                }
                sb.Append(char.ToLower(c));
            }
            return sb.ToString();
        });
        return this;
    }

    public BpmConventionBuilder ToSnakeCase()
    {
        _transforms.Add(name =>
        {
            var sb = new StringBuilder();
            for (int i = 0; i < name.Length; i++)
            {
                var c = name[i];
                if (i > 0 && char.IsUpper(c) && !char.IsUpper(name[i - 1]))
                {
                    sb.Append('_');
                }
                sb.Append(char.ToLower(c));
            }
            return sb.ToString();
        });
        return this;
    }

    public BpmConventionBuilder ToLowerCase()
    {
        _transforms.Add(name => name.ToLowerInvariant());
        return this;
    }

    public BpmConventionBuilder WithPrefix(string prefix)
    {
        _transforms.Add(name =>
        {
            var p = prefix.TrimEnd('/');
            var n = name.StartsWith('/') ? name : "/" + name;
            return p + n;
        });
        return this;
    }

    public BpmConventionBuilder WithSuffix(string suffix)
    {
        _transforms.Add(name =>
        {
            var s = suffix.StartsWith('/') ? suffix : "/" + suffix;
            var n = name.TrimEnd('/');
            return n + s;
        });
        return this;
    }

    public BpmConventionBuilder FromNamespaceSegment(int index)
    {
        _transforms.Add(name => name);
        return this;
    }

    public BpmConventionBuilder GroupByNamespace(int depth)
    {
        _transforms.Add(name => name);
        return this;
    }

    public BpmConventionBuilder Transform(Func<string, string> transform)
    {
        _transforms.Add(transform);
        return this;
    }

    internal string Apply(string commandName)
    {
        var result = commandName;
        foreach (var transform in _transforms)
        {
            result = transform(result);
        }
        if (!result.StartsWith('/'))
            result = "/" + result;
        return result;
    }
}
