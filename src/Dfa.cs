using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace RegExCompiler;

public class Dfa
{
    private readonly Dictionary<string, Dictionary<char, string>> _transitions = [];
    private readonly HashSet<string> _final = [];
    private string _initial = null!;
    private string _name = null!;
    private HashSet<char> Alphabet { get; init; } = [];

    public static Dfa FromNfa(Nfa nfa, string name = "DFA")
    {
        var res = new Dfa
        {
            _name = name,
            Alphabet = nfa.Alphabet.Where(m => m != Nfa.Epsilon).ToHashSet()
        };
        var tTable =
            new Dictionary<HashSet<int>, Dictionary<char, HashSet<int>>>(HashSet<int>
                .CreateSetComparer()); // states -> char -> dests
        var stack = new Stack<HashSet<int>>();
        var visitedSet = new HashSet<HashSet<int>>(HashSet<int>.CreateSetComparer());
        var initialState = nfa.GetEpsilonReachableStates(nfa.Initial).ToHashSet();
        AddToStack(initialState);
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
                res._final.Add(state);
            }

            foreach (var key in tTable[stateSet].Keys)
            {
                res.AddTransition(state, StateSetToName(tTable[stateSet][key]), key);
            }
        }

        res._initial = res._transitions.Keys.AsEnumerable().ToArray()[1];

        return res;

        string StateSetToName(HashSet<int> stateSet)
        {
            return $"\"{{{string.Join("; ", stateSet)}}}\"";
        }

        void AddToStack(HashSet<int> dest)
        {
            if (visitedSet.Add(dest))
            {
                stack.Push(dest);
            }
        }
    }

    private void AddTransition(string from, string to, char c)
    {
        Alphabet.Add(c);
        if (_transitions.TryGetValue(from, out var t))
        {
            t.Add(c, to);
            return;
        }

        t = new Dictionary<char, string>();
        _transitions.Add(from, t);

        t.Add(c, to);
    }

    public bool IsMatch(string input)
    {
        return Match(input, out var res) && input.Equals(res);
    }

    public bool Match(string input, [NotNullWhen(true)] out string? match)
    {
        var state = _initial;
        var i = 0;
        match = null;
        while (i < input.Length)
        {
            string? dest;
            if (_transitions.TryGetValue(state, out var transition))
            {
                if (!transition.TryGetValue(input[i], out dest))
                {
                    break;
                }
            }
            else
            {
                break;
            }

            state = dest;
            i++;
        }

        if (i == 0)
        {
            return false;
        }

        match = input[..i];
        return true;

    }

    public override string ToString()
    {
        var allStates = _final.Union(_transitions.Keys).ToHashSet();
        Console.Out.WriteLine($"states: {{{string.Join(", ", allStates)}}}");
        Console.Out.WriteLine($"finals: {{{string.Join(", ", _final)}}}");
        var normalStates = string.Join("; ", allStates.Where(state => !_final.Contains(state)));
        var builder = new StringBuilder("digraph {\n").AppendLine("\trankdir=LR;")
            .AppendLine($"\tlabel=\"RegEx: {_name}\"")
            .AppendLine(normalStates.Length != 0 ? $"\tnode [shape = circle]; {normalStates};" : "");


        var finalStates = string.Join("; ", allStates.Where(state => _final.Contains(state)));
        builder.AppendLine(finalStates.Length != 0 ? $"\tnode [shape = doublecircle]; {finalStates};" : "")
            .Append("\t/* Alphabet: ");
        List<string> tmp = [];
        foreach (var c in Alphabet)
        {
            var k = $"{c}";
            if (char.IsControl(c))
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
                if (char.IsControl(key))
                {
                    k = $"Control-{(int)key}";
                }
                else if(key == '"')
                {
                    k = "\\\"";
                }
                else if (key == '\\')
                {
                    k = @"\\";
                }

                builder.AppendLine($"\t{transition} -> {_transitions[transition][key]} [label=\"{k}\"];");
            }
        }

        builder.Append('}');
        return builder.ToString();
    }
}