using System;
using System.Runtime.InteropServices.JavaScript;
using RegExCompiler;
using RegExCompiler.Parsing;

Console.WriteLine("Hello, Browser!");

public partial class WebRegExCompiler
{
    [JSExport]
    internal static string[] RegExToNfaAndDfa(string regex, bool minimizeDfa = true, bool minimizeDfaNames = true)
    {
        var nfa = Nfa.FromInfix(InfixToPostfix.Convert(Tokenizer.Tokenize(regex)), regex);
        var dfa = Dfa.FromNfa(nfa, regex);
        if (minimizeDfa)
        {
            dfa = dfa.Minimize();
        }

        if (minimizeDfaNames)
        {
            dfa = dfa.MinimizeNames();
        }
        return [nfa.ToString(), dfa.ToString()];
    }
}