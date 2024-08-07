using System.Text;

namespace RegExCompiler;

public class Dfa
{
    public string Initial = null!;
    public readonly HashSet<string> Final = [];
    public string Name = null!;
    public HashSet<char> Alphabet { get; private set; } = [];
    private readonly Dictionary<string, Dictionary<char, string>> _transitions = [];

    public static Dfa FromNfa(Nfa nfa, string name = "DFA")
    {
        var res = new Dfa
        {
            Name = name,
            Alphabet = nfa.Alphabet.Where(m => m != Nfa.Epsilon).ToHashSet()
        };
        var tTable =
            new Dictionary<HashSet<int>, Dictionary<char, HashSet<int>>>(HashSet<int>
                .CreateSetComparer()); // states -> char -> dests
        var stack = new Stack<HashSet<int>>();
        var visitedSet = new HashSet<HashSet<int>>(HashSet<int>.CreateSetComparer());
        AddToStack(nfa.GetEpsilonReachableStates(nfa.Initial).ToHashSet());
        while (stack.TryPop(out var states))
        {
            if (!tTable.TryGetValue(states, out var d))
            {
                d = new Dictionary<char, HashSet<int>>();
                tTable.Add(states, d);
            }

            foreach (var c in res.Alphabet)
            {
                var dest = new HashSet<int>();
                foreach (var state in states)
                {
                    if (!nfa.GetTransitions(state, c, out var r))
                    {
                        continue;
                    }

                    foreach (var reachableState in r.SelectMany(nfa.GetEpsilonReachableStates))
                    {
                        dest.Add(reachableState);
                    }
                }

                if (dest.Count == 0)
                {
                    continue;
                }

                AddToStack(dest);
                if (d.TryGetValue(c, out var s))
                {
                    s.UnionWith(dest);
                }
                else
                {
                    d.Add(c, dest);
                }
            }
        }

        foreach (var stateSet in tTable.Keys)
        {
            var state = StateSetToName(stateSet);
            if (stateSet.Any(s => nfa.Final == s))
            {
                res.Final.Add(state);
            }

            foreach (var key in tTable[stateSet].Keys)
            {
                res.AddTransition(state, StateSetToName(tTable[stateSet][key]), key);
            }
        }

        return res;

        string StateSetToName(HashSet<int> stateSet)
        {
            return $"\"{{{string.Join("; ", stateSet)}}}\"";
        }

        void AddToStack(HashSet<int> dest)
        {
            if (!visitedSet.Contains(dest))
            {
                visitedSet.Add(dest);
                stack.Push(dest);
            }
        }
    }

    public bool AddTransition(string from, string to, char c)
    {
        Alphabet.Add(c);
        if (_transitions.TryGetValue(from, out var t))
        {
            return t.TryAdd(c, to);
        }

        t = new Dictionary<char, string>();
        _transitions.Add(from, t);

        return t.TryAdd(c, to);
    }

    public override string ToString()
    {
        var allStates = Final.Union(_transitions.Keys);
        Console.Out.WriteLine($"states: {{{string.Join(", ", allStates)}}}");
        Console.Out.WriteLine($"finals: {{{string.Join(", ", Final)}}}");
        var normalStates = string.Join("; ", allStates.Where(state => !Final.Contains(state)));
        var builder = new StringBuilder("digraph {\n").AppendLine("\trankdir=LR;")
            .AppendLine($"\tlabel=\"RegEx: {Name}\"")
            .AppendLine(normalStates.Length != 0 ? $"\tnode [shape = circle]; {normalStates};" : "");


        var finalStates = string.Join("; ", allStates.Where(state => Final.Contains(state)));
        builder.AppendLine(finalStates.Length != 0 ? $"\tnode [shape = doublecircle]; {finalStates};" : "")
            .Append("\t/* Alphabet: ");
        List<string> tmp = [];
        foreach (var c in Alphabet)
        {
            var k = $"{c}";
            if (!char.IsDigit(c) && !char.IsLetter(c))
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
                if (!char.IsDigit(key) && !char.IsLetter(key))
                {
                    k = $"Control-{(int)key}";
                }

                builder.AppendLine($"\t{transition} -> {_transitions[transition][key]} [label=\"{k}\"];");
            }
        }

        builder.Append('}');
        return builder.ToString();
    }
}