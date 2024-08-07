using System.Collections.Immutable;
using System.Data;
using System.Text;
using RegExCompiler.Parsing;

namespace RegExCompiler;

public class Nfa
{
    public int initial;
    public int final;
    public string name;
    private readonly Dictionary<int, Dictionary<char, HashSet<int>>> _transitions = [];
    private const char Epsilon = 'ε';

    public static Nfa FromInfix(ImmutableList<Token> input, string title = "NFA")
    {
        var stack = new Stack<Nfa>();
        foreach (var token in input)
        {
            if (token.Equals(Token.Element))
            {
                var curr = new Nfa
                {
                    name = title,
                    initial = 0,
                    final = 1
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
                    name = title,
                    initial = a.initial,
                    final = a.final
                };
                curr.AddTransition(curr.initial, curr.final, Epsilon);
                curr.AddTransition(curr.final, curr.initial, Epsilon);
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
                    name = title,
                    initial = a.initial,
                    final = a.final
                };
                curr.AddTransition(curr.initial, curr.final, Epsilon);
                stack.Push(curr);
            }
            else if (token.Equals(Token.Any))
            {
                var curr = new Nfa
                {
                    name = title,
                    initial = 0,
                    final = 1
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
            initial = 0,
            final = 1
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
            name = a.name,
            initial = 0
        };
        curr.AddTransition(0, a.initial + 1, Epsilon);
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

        curr.final = a.final + 2;
        curr.AddTransition(a.final + 1, a.final + 2, Epsilon);
        curr.AddTransition(curr.final, curr.initial, Epsilon);
        curr.AddTransition(curr.initial, curr.final, Epsilon);
        return curr;
    }

    private static Nfa AppendNfa(Nfa a, Nfa b)
    {
        var curr = new Nfa
        {
            name = a.name,
            initial = 0
        };
        curr.AddTransition(0, a.initial + 1, Epsilon);
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
                    curr.AddTransition(transition + a.final + 2, dest + a.final + 2,
                        key);
                }
            }
        }

        curr.AddTransition(a.final + 1, b.initial + a.final + 2, Epsilon);
        curr.AddTransition(b.final + a.final + 2, b.final + a.final + 3, Epsilon);
        curr.final = b.final + a.final + 3;
        return curr;
    }

    private static Nfa OrNfa(Nfa a, Nfa b)
    {
        var curr = new Nfa
        {
            name = a.name,
            initial = 0
        };
        var i = 1;
        var aFinal = i + a.final;
        curr.AddTransition(0, a.initial + i, Epsilon);
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

        i = a.final + 2;
        var bFinal = i + b.final;
        curr.AddTransition(0, b.initial + i, Epsilon);
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
        curr.final = bFinal + 1;
        return curr;
    }

    public bool AddTransition(int from, int to, char c)
    {
        if (_transitions.TryGetValue(from, out var t))
        {
            if (t.TryGetValue(c, out var l))
            {
                l.Add(to);
            }

            return t.TryAdd(c, [to]);
        }

        t = new Dictionary<char, HashSet<int>>();
        _transitions.Add(from, t);

        return t.TryAdd(c, [to]);
    }

    public override string ToString()
    {
        var builder = new StringBuilder("digraph {\n").AppendLine("\trankdir=LR;")
            .AppendLine($"\tlabel=\"RegEx: {name}\"")
            .Append("\tnode [shape = circle];");
        foreach (var start in _transitions.Keys.Where(start => start != final))
        {
            builder.Append($"{start}; ");
        }

        builder.Append('\n');

        builder.AppendLine($"\tnode [shape = doublecircle]; {final};");
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