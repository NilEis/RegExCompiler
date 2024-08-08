using System.Text;

namespace RegExCompiler.Parsing;

public class Token(int id, string name, char value, bool op = true, IEnumerable<CaptureSubGroup>? values = null)
    : Enumeration<char>(id, value)
{
    private static readonly int IdCounter;
    public static readonly Token BracketOpen = new(IdCounter++, "(", '(', false); //
    public static readonly Token Element = new(IdCounter++, " ELEMENT ", '\0', false); //
    public static readonly Token BracketClose = new(IdCounter++, ")", ')', false); //
    public static readonly Token CharacterClass = new(IdCounter++, " CharacterClass ", '\0', false); //
    public static readonly Token Or = new(IdCounter++, "|", '|'); //
    public static readonly Token Any = new(IdCounter++, ".", '.', false); //
    public static readonly Token Append = new(IdCounter++, " APPEND ", '\0'); //
    public static readonly Token ZeroOrOnce = new(IdCounter++, "?", '?'); //
    public static readonly Token OneOrMore = new(IdCounter++, "+", '+'); //
    public static readonly Token Kleene = new(IdCounter++, "*", '*'); //
    public List<CaptureSubGroup> Values { get; } = values?.ToList() ?? [];
    private int Id { get; } = id;
    public bool IsOperator { get; } = op;

    public static Token CreateElement(char value)
    {
        return new Token(Element.Id, $"{value}", value, false);
    }

    public static Token CreateCharacterClass(IEnumerable<CaptureSubGroup> values)
    {
        var captureName = new StringBuilder("[");
        var v = values.ToList();
        var inverseGroup = false;
        var substring = new StringBuilder("");
        for (var i = 0; i < v.Count; i++)
        {
            var subGroup = v[i];
            if (v.Count >= i + 1 && subGroup.Start == (char)(1 + char.MinValue) && v[i + 1].End == char.MaxValue)
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

        return new Token(CharacterClass.Id, captureName.ToString(), CharacterClass.Value, false, v);
    }

    public override string ToString()
    {
        return name;
    }
}