using System.Collections.Immutable;
using RegExCompiler.Parsing;

namespace RegExCompiler;

public class Nfa
{
    public int initial;
    public int final;
    private readonly Dictionary<int, Dictionary<char, int>> _transitions = [];

    public static Nfa FromInfix(ImmutableList<Token> input)
    {
        
    }

    public bool AddTransition(int from, int to, char c)
    {
        if (!_transitions.TryGetValue(from, out var t))
        {
            t = new Dictionary<char, int>();
            _transitions.Add(from, t);
        }

        return t.TryAdd(c, to);
    }
}