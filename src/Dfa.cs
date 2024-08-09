using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace RegExCompiler;

public class Dfa
{
    private Dictionary<string, Dictionary<char, string>> _transitions = [];
    private HashSet<string> _final = [];
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

        res._initial = res._transitions.Keys.AsEnumerable().ToArray()[0];

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

    public Dfa Minimize()
    {
        // Step 1: Remove unreachable states
        var reachableStates = new HashSet<string> { _initial };
        var queue = new Queue<string>();
        queue.Enqueue(_initial);

        while (queue.Count > 0)
        {
            var state = queue.Dequeue();
            if (!_transitions.TryGetValue(state, out var transitions))
            {
                continue;
            }

            foreach (var nextState in transitions.Values.Where(nextState => reachableStates.Add(nextState)))
            {
                queue.Enqueue(nextState);
            }
        }

        // Remove unreachable states
        var allStates = new HashSet<string>(_transitions.Keys);
        var unreachableStates = allStates.Except(reachableStates).ToList();
        foreach (var state in unreachableStates)
        {
            _transitions.Remove(state);
        }

        _final.IntersectWith(reachableStates);
        if (!_final.Contains(_initial))
        {
            _initial = reachableStates.First();
        }

        foreach (var state in _transitions.Keys.Where(e =>
                     _final.Contains(e) && (_transitions[e].Keys.Count == 1 || (_transitions[e].Keys.Count != 0 &&
                         _transitions[e].Keys.All(v =>
                             _transitions[e][v].Equals(_transitions[e][_transitions[e].Keys.First()]))))))
        {
            var transition = _transitions[state].Keys.First();
            var dest = _transitions[state][transition];
            if (!_final.Contains(dest) || !_transitions[dest].Keys.All(v => _transitions[dest].ContainsKey(v) &&
                                                                           _transitions[dest][v].Equals(dest)) ||
                dest.Equals(state))
            {
                continue;
            }

            foreach (var st in _transitions.Keys)
            {
                foreach (var key in _transitions[st].Keys.Where(key => _transitions[st][key] == state))
                {
                    _transitions[st][key] = dest;
                }
            }

            Console.Out.WriteLine($"Merge {state} and {dest}");
            if (_initial.Equals(state))
            {
                _initial = dest;
            }
            _transitions.Remove(state);
            _final.Remove(state);
        }

        return this;
    }

    public Dfa MinimizeNames()
    {
        var i = 0;
        var dict = _transitions.Keys.ToDictionary(state => state, state => IndexToName(i++));

        foreach (var fin in _final)
        {
            dict.TryAdd(fin, IndexToName(i++));
        }

        foreach (var renamedState in dict.Keys)
        {
            foreach (var state in _transitions.Keys)
            {
                foreach (var transition in _transitions[state].Keys
                             .Where(transition => _transitions[state][transition].Equals(renamedState)))
                {
                    _transitions[state][transition] = dict[renamedState];
                }
            }

            if (_transitions.Remove(renamedState, out var tmp))
            {
                _transitions.Add(dict[renamedState], tmp);
            }
        }

        _initial = dict[_initial];
        foreach (var fin in _final.ToList())
        {
            _final.Remove(fin);
            _final.Add(dict[fin]);
        }

        return this;

        string IndexToName(int i1)
        {
            return $"{{{i1}}}";
        }
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
                else if (key == '"')
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