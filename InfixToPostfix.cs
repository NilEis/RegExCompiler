using RegExCompiler.Parsing;

namespace RegExCompiler;

public static class InfixToPostfix
{
    public static IEnumerable<Token> Convert(IEnumerable<Token> input)
    {
        Queue<Token> output = [];
        Stack<Token> operators = [];
        foreach (var token in input)
        {
            if (!token.IsOperator && !token.Equals(Token.BracketOpen) && !token.Equals(Token.BracketClose))
            {
                output.Enqueue(token);
            }
            else if (token.Equals(Token.BracketOpen))
            {
                operators.Push(token);
            }
            else if (token.Equals(Token.BracketClose))
            {
                Token curr;
                while (!Equals(curr = operators.Pop(), Token.BracketOpen))
                {
                    output.Enqueue(curr);
                }
            }
            else
            {
                while (operators.Count != 0 && operators.Peek().CompareTo(token) >= 0)
                {
                    output.Enqueue(operators.Pop());
                }

                operators.Push(token);
            }
        }

        while (operators.Count != 0)
        {
            output.Enqueue(operators.Pop());
        }

        return output;
    }
}