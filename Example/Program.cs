using System.Collections.Immutable;
using System.Text;
using RegExCompiler;
using RegExCompiler.Parsing;

const string input = "abc*";
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
Console.Out.WriteLine("writing nfa.dot");
swNfa.Write(nfa);
swNfa.Flush();
var dfa = Dfa.FromNfa(nfa, input).Minimize();
using var swDfa = new StreamWriter("dfa.dot");
Console.Out.WriteLine("writing dfa.dot");
swDfa.Write(dfa);
swDfa.Flush();

dfa.Match("amogus", out var res);
dfa.Match("amogs", out res);
dfa.Match("amogusa", out res);
var x = dfa.IsMatch("abacusa");