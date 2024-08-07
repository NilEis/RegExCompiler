using System.Collections.Immutable;
using System.Text;
using RegExCompiler;
using RegExCompiler.Parsing;

const string input = "a+bc(de)*";
var tokenized = Tokenizer.Tokenize(input).ToImmutableList();
var tokenizedString = new StringBuilder("");
foreach (var token in tokenized)
{
    tokenizedString.Append(token);
}

Console.Out.WriteLine($"tokenized: {tokenizedString}");
var infix = InfixToPostfix.Convert(tokenized);
var infixString = new StringBuilder("");
foreach (var token in infix)
{
    infixString.Append(token);
}

Console.Out.WriteLine($"infix: {infixString}");
var nfa = Nfa.FromInfix(infix, input);
using var swNfa = new StreamWriter("nfa.dot");
swNfa.Write(nfa);
var dfa = Dfa.FromNfa(nfa);
using var swDfa = new StreamWriter("dfa.dot");
swDfa.Write(dfa);