using System.Collections.Immutable;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using RegExCompiler.Parsing;

namespace RegExCompiler;

public class Nfa
{
    public const char Epsilon = 'ε';
    private readonly Dictionary<int, Dictionary<char, HashSet<int>>> _transitions = [];
    public int Final;
    public int Initial;
    private string _name = null!;

    public HashSet<char> Alphabet { get; } = [];

    public static Nfa FromInfix(ImmutableList<Token> input, string title = "NFA")
    {
        var stack = new Stack<Nfa>();
        foreach (var token in input)
        {
            if (token.Equals(Token.Element))
            {
                var curr = new Nfa
                {
                    _name = title,
                    Initial = 0,
                    Final = 1
                };
                curr.AddTransition(0, 1, token.Value);
                stack.Push(curr);
            }
            else if (token.Equals(Token.CaptureGroup))
            {
                var curr = FromCaptureGroup(token);
                stack.Push(curr);
            }
            else if (token.Equals(Token.Append))
            {
                // ab
                var b = stack.Pop();
                var a = stack.Pop();
                var curr = AppendNfa(a, b);
                stack.Push(curr);
            }
            else if (token.Equals(Token.Or))
            {
                var a = stack.Pop();
                var b = stack.Pop();
                var curr = OrNfa(a, b);
                stack.Push(curr);
            }
            else if (token.Equals(Token.OneOrMore))
            {
                var a = stack.Pop();
                var curr = new Nfa
                {
                    _name = title,
                    Initial = a.Initial,
                    Final = a.Final
                };
                curr.AddTransition(curr.Initial, curr.Final, Epsilon);
                curr.AddTransition(curr.Final, curr.Initial, Epsilon);
                foreach (var transition in a._transitions.Keys)
                {
                    foreach (var key in a._transitions[transition].Keys)
                    {
                        foreach (var dest in a._transitions[transition][key])
                        {
                            curr.AddTransition(transition, dest, key);
                        }
                    }
                }

                curr = AppendNfa(a, curr);
                stack.Push(curr);
            }
            else if (token.Equals(Token.ZeroOrOnce))
            {
                var a = stack.Pop();
                var curr = new Nfa
                {
                    _name = title,
                    Initial = a.Initial,
                    Final = a.Final
                };
                foreach (var transition in a._transitions.Keys)
                {
                    foreach (var key in a._transitions[transition].Keys)
                    {
                        foreach (var dest in a._transitions[transition][key])
                        {
                            curr.AddTransition(transition, dest, key);
                        }
                    }
                }

                curr.AddTransition(curr.Initial, curr.Final, Epsilon);
                stack.Push(curr);
            }
            else if (token.Equals(Token.Any))
            {
                var curr = new Nfa
                {
                    _name = title,
                    Initial = 0,
                    Final = 1
                };
                curr.AddTransition(0, 1, Epsilon);
                stack.Push(curr);
            }
            else if (token.Equals(Token.Kleene))
            {
                var a = stack.Pop();
                var curr = KleeneNfa(a);
                stack.Push(curr);
            }
        }

        if (stack.Count == 1)
        {
            return stack.Pop();
        }

        Console.Out.WriteLine($"Something went wrong: stack.Count = {stack.Count}");
        throw new ConstraintException($"stack.Count = {stack.Count}");
    }

    private static Nfa FromCaptureGroup(Token token)
    {
        var curr = new Nfa
        {
            Initial = 0,
            Final = 1
        };
        foreach (var subGroup in token.Values)
        {
            for (var c = subGroup.Start; c < subGroup.End; c++)
            {
                curr.AddTransition(0, 1, c);
            }
        }

        return curr;
    }

    private static Nfa KleeneNfa(Nfa a)
    {
        var curr = new Nfa
        {
            _name = a._name,
            Initial = 0
        };
        curr.AddTransition(0, a.Initial + 1, Epsilon);
        foreach (var transition in a._transitions.Keys)
        {
            foreach (var key in a._transitions[transition].Keys)
            {
                foreach (var dest in a._transitions[transition][key])
                {
                    curr.AddTransition(transition + 1, dest + 1, key);
                }
            }
        }

        curr.Final = a.Final + 2;
        curr.AddTransition(a.Final + 1, a.Final + 2, Epsilon);
        curr.AddTransition(curr.Final, curr.Initial, Epsilon);
        curr.AddTransition(curr.Initial, curr.Final, Epsilon);
        return curr;
    }

    private static Nfa AppendNfa(Nfa a, Nfa b)
    {
        var curr = new Nfa
        {
            _name = a._name,
            Initial = 0
        };
        curr.AddTransition(0, a.Initial + 1, Epsilon);
        foreach (var transition in a._transitions.Keys)
        {
            foreach (var key in a._transitions[transition].Keys)
            {
                foreach (var dest in a._transitions[transition][key])
                {
                    curr.AddTransition(transition + 1, dest + 1, key);
                }
            }
        }

        foreach (var transition in b._transitions.Keys)
        {
            foreach (var key in b._transitions[transition].Keys)
            {
                foreach (var dest in b._transitions[transition][key])
                {
                    curr.AddTransition(transition + a.Final + 2, dest + a.Final + 2,
                        key);
                }
            }
        }

        curr.AddTransition(a.Final + 1, b.Initial + a.Final + 2, Epsilon);
        curr.AddTransition(b.Final + a.Final + 2, b.Final + a.Final + 3, Epsilon);
        curr.Final = b.Final + a.Final + 3;
        return curr;
    }

    private static Nfa OrNfa(Nfa a, Nfa b)
    {
        var curr = new Nfa
        {
            _name = a._name,
            Initial = 0
        };
        var i = 1;
        var aFinal = i + a.Final;
        curr.AddTransition(0, a.Initial + i, Epsilon);
        foreach (var transition in a._transitions.Keys)
        {
            foreach (var key in a._transitions[transition].Keys)
            {
                foreach (var dest in a._transitions[transition][key])
                {
                    curr.AddTransition(transition + i, dest + i, key);
                }
            }
        }

        i = a.Final + 2;
        var bFinal = i + b.Final;
        curr.AddTransition(0, b.Initial + i, Epsilon);
        foreach (var transition in b._transitions.Keys)
        {
            foreach (var key in b._transitions[transition].Keys)
            {
                foreach (var dest in b._transitions[transition][key])
                {
                    curr.AddTransition(transition + i, dest + i, key);
                }
            }
        }

        curr.AddTransition(aFinal, bFinal + 1, Epsilon);
        curr.AddTransition(bFinal, bFinal + 1, Epsilon);
        curr.Final = bFinal + 1;
        return curr;
    }

    private void AddTransition(int from, int to, char c)
    {
        Alphabet.Add(c);
        if (_transitions.TryGetValue(from, out var t))
        {
            if (t.TryGetValue(c, out var l))
            {
                l.Add(to);
                return;
            }

            t.Add(c, [to]);
            return;
        }

        t = new Dictionary<char, HashSet<int>>();
        _transitions.Add(from, t);

        t.Add(c, [to]);
    }

    public bool GetTransitions(int start, char c, [NotNullWhen(true)] out ImmutableHashSet<int>? res)
    {
        res = null;
        if (!_transitions.TryGetValue(start, out var d))
        {
            return false;
        }

        if (!d.TryGetValue(c, out var set))
        {
            return false;
        }

        res = set.ToImmutableHashSet();
        return true;
    }

    public IEnumerable<int> GetEpsilonReachableStates(int start)
    {
        var res = new HashSet<int> { start };
        if (!GetTransitions(start, Epsilon, out var t))
        {
            return res;
        }

        foreach (var dest in t)
        {
            res.Add(dest);
            GetEpsilonReachableStates(dest, ref res);
        }

        return res;
    }

    private void GetEpsilonReachableStates(int start, ref HashSet<int> res)
    {
        if (!GetTransitions(start, Epsilon, out var t))
        {
            return;
        }

        foreach (var dest in t)
        {
            if (!res.Add(dest))
            {
                continue;
            }

            GetEpsilonReachableStates(dest, ref res);
        }
    }

    public override string ToString()
    {
        var builder = new StringBuilder("digraph {\n").AppendLine("\trankdir=LR;")
            .AppendLine($"\tlabel=\"RegEx: {_name}\"")
            .Append($"\tnode [shape = circle]; {string.Join("; ", _transitions.Keys.Where(start => start != Final))};");

        builder.Append('\n');

        builder.AppendLine($"\tnode [shape = doublecircle]; {Final};")
            .Append("\t/* Alphabet: ");
        List<string> tmp = [];
        foreach (var c in Alphabet)
        {
            var k = $"{c}";
            if (c == Epsilon)
            {
                k = "ε";
            }
            else if (!char.IsDigit(c) && !char.IsLetter(c))
            {
                k = $"Control-{(int)c}";
            }

            tmp.Add($"{k}");
        }

        builder.AppendLine($"{{{string.Join(", ", tmp)}}} */");
        foreach (var transition in _transitions.Keys)
        {
            foreach (var key in _transitions[transition].Keys)
            {
                var k = $"{key}";
                if (key == Epsilon)
                {
                    k = "ε";
                }
                else if (!char.IsDigit(key) && !char.IsLetter(key))
                {
                    k = $"Control-{(int)key}";
                }

                foreach (var dest in _transitions[transition][key])
                {
                    builder.AppendLine($"\t{transition} -> {dest} [label=\"{k}\"];");
                }
            }
        }

        builder.Append('}');
        return builder.ToString();
    }
}