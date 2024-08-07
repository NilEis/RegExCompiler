using System.Collections.Immutable;
using System.Text;
using RegExCompiler;
using RegExCompiler.Parsing;

const string input = "(a|b)*c[bf]+";
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
Console.Out.WriteLine("NFA:");
using var sw = new StreamWriter("nfa.dot");
sw.Write(Nfa.FromInfix(infix, input));