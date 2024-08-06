using System.Text;

namespace RegExCompiler.Parsing;

public class Token(int id, string name, char value, bool op = true, IEnumerable<CaptureSubGroup>? values = null)
    : Enumeration<char>(id, value)
{
    private static int idCounter = 0;
    public List<CaptureSubGroup> Values { get; } = values?.ToList() ?? [];
    private int Id { get; } = id;
    public bool IsOperator { get; } = op;
    public static readonly Token BracketOpen = new(idCounter++, "(", '(', false);
    public static readonly Token Element = new(idCounter++, " ELEMENT ", '\0', false);
    public static readonly Token ZeroOrOnce = new(idCounter++, "?", '?');
    public static readonly Token BracketClose = new(idCounter++, ")", ')', false);
    public static readonly Token CaptureGroup = new(idCounter++, " CAPTURE_GROUP ", '\0', false);
    public static readonly Token Or = new(idCounter++, "|", '|');
    public static readonly Token OneOrMore = new(idCounter++, "+", '+');
    public static readonly Token Any = new(idCounter++, ".", '.');
    public static readonly Token Kleene = new(idCounter++, "*", '*');
    public static readonly Token Append = new(idCounter++, " APPEND ", '\0');

    public static Token CreateElement(char value)
    {
        return new Token(Element.Id, $"{value}", value, false);
    }

    public static Token CreateCaptureGroup(IEnumerable<CaptureSubGroup> values)
    {
        var captureName = new StringBuilder("[");
        var v = values.ToList();
        var inverseGroup = false;
        var substring = new StringBuilder("");
        for (var i = 0; i < v.Count; i++)
        {
            var subGroup = v[i];
            if (v.Count >= i + 1 && subGroup.Start == char.MinValue && v[i + 1].End == char.MaxValue)
            {
                inverseGroup = true;
                substring.Append($"{subGroup.End}{v[i + 1].Start}");
                i++;
            }
            else
            {
                substring.Append($"{subGroup.Start}{subGroup.End}");
            }
        }

        captureName.Append(inverseGroup ? "^" : "")
            .Append(substring)
            .Append(']');

        return new Token(CaptureGroup.Id, captureName.ToString(), CaptureGroup.Value, false, v);
    }

    public override string ToString()
    {
        return name;
    }
}