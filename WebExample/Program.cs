using System;
using System.Runtime.InteropServices.JavaScript;
using RegExCompiler;
using RegExCompiler.Parsing;

Console.WriteLine("Hello, Browser!");

public partial class WebRegExCompiler
{
    [JSExport]
    internal static string[] RegExToNfaAndDfa(string regex, bool minimizeDfa = true)
    {
        var nfa = Nfa.FromInfix(InfixToPostfix.Convert(Tokenizer.Tokenize(regex)), regex);
        var dfa = Dfa.FromNfa(nfa, regex);
        if (minimizeDfa)
        {
            dfa = dfa.Minimize();
        }
        return [nfa.ToString(), dfa.ToString()];
    }
}