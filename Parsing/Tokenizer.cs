namespace RegExCompiler.Parsing;

public static class Tokenizer
{
    public static IEnumerable<Token> Tokenize(string regex)
    {
        var tokenized = new List<Token>();
        for (var i = 0; i < regex.Length; i++)
        {
            if (Token.FromValue<Token>(regex[i], out var foundToken))
            {
                tokenized.Add(foundToken);
            }
            else
            {
                switch (regex[i])
                {
                    case '[':
                        i++;
                        var capturegroup = new List<CaptureSubGroup>();
                        while (regex[i] != ']')
                        {
                            if (regex[i] == '^')
                            {
                                i++;
                                capturegroup.Add(new CaptureSubGroup(char.MinValue, regex[i++]));
                                capturegroup.Add(new CaptureSubGroup(regex[i++], char.MaxValue));
                            }
                            else
                            {
                                var start = regex[i++];
                                var end = regex[i++];
                                capturegroup.Add(new CaptureSubGroup(start, end));
                            }
                        }

                        tokenized.Add(Token.CreateCaptureGroup(capturegroup));
                        break;
                    default:
                        tokenized.Add(Token.CreateElement(regex[i]));
                        break;
                }
            }
        }

        List<Token> res =
        [
            tokenized[0]
        ];
        for (var i = 1; i < tokenized.Count; i++)
        {
            if (!tokenized[i].IsOperator && !tokenized[i-1].IsOperator && !tokenized[i].Equals(Token.BracketClose) &&
                !tokenized[i - 1].Equals(Token.BracketOpen))
            {
                res.Add(Token.Append);
            }

            res.Add(tokenized[i]);
        }


        return res;
    }
}