using System;
using System.Runtime.InteropServices.JavaScript;
using RegExCompiler;
using RegExCompiler.Parsing;

Console.WriteLine("Hello, Browser!");

public partial class WebRegExCompiler
{
    [JSExport]
    internal static string[] RegExToNfaAndDfa(string regex)
    {
        var nfa = Nfa.FromInfix(InfixToPostfix.Convert(Tokenizer.Tokenize(regex)), regex);
        return [nfa.ToString(), Dfa.FromNfa(nfa, regex).ToString()];
    }
}