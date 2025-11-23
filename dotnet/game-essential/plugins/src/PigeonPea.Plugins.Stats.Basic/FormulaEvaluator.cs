using System;
using System.Collections.Generic;

namespace PigeonPea.Plugins.Stats.Basic;

public interface IFormulaEvaluator
{
    float Evaluate(string formula, Dictionary<string, float> context);
}

public sealed class FormulaEvaluator : IFormulaEvaluator
{
    public float Evaluate(string formula, Dictionary<string, float> context)
    {
        if (string.IsNullOrWhiteSpace(formula))
        {
            return 0f;
        }

        try
        {
            var tokens = Tokenize(formula);
            var rpn = ToRpn(tokens);
            return EvaluateRpn(rpn, context);
        }
        catch
        {
            return 0f;
        }
    }

    private static List<Token> Tokenize(string formula)
    {
        var tokens = new List<Token>();
        int i = 0;
        while (i < formula.Length)
        {
            var c = formula[i];
            if (char.IsWhiteSpace(c))
            {
                i++;
                continue;
            }

            if (char.IsLetter(c) || c == '_')
            {
                int start = i;
                while (i < formula.Length && (char.IsLetterOrDigit(formula[i]) || formula[i] == '_'))
                {
                    i++;
                }

                var name = formula.Substring(start, i - start);
                tokens.Add(new Token(TokenKind.Identifier, name));
                continue;
            }

            if (char.IsDigit(c) || c == '.')
            {
                int start = i;
                while (i < formula.Length && (char.IsDigit(formula[i]) || formula[i] == '.'))
                {
                    i++;
                }

                var number = formula.Substring(start, i - start);
                tokens.Add(new Token(TokenKind.Number, number));
                continue;
            }

            if (c == '+' || c == '-' || c == '*' || c == '/')
            {
                tokens.Add(new Token(TokenKind.Operator, c.ToString()));
                i++;
                continue;
            }

            if (c == '(')
            {
                tokens.Add(new Token(TokenKind.LeftParen, "("));
                i++;
                continue;
            }

            if (c == ')')
            {
                tokens.Add(new Token(TokenKind.RightParen, ")"));
                i++;
                continue;
            }

            i++;
        }

        return tokens;
    }

    private static List<Token> ToRpn(List<Token> tokens)
    {
        var output = new List<Token>();
        var ops = new Stack<Token>();

        foreach (var token in tokens)
        {
            if (token.Kind == TokenKind.Number || token.Kind == TokenKind.Identifier)
            {
                output.Add(token);
            }
            else if (token.Kind == TokenKind.Operator)
            {
                while (ops.Count > 0 && ops.Peek().Kind == TokenKind.Operator &&
                       Precedence(ops.Peek().Text) >= Precedence(token.Text))
                {
                    output.Add(ops.Pop());
                }

                ops.Push(token);
            }
            else if (token.Kind == TokenKind.LeftParen)
            {
                ops.Push(token);
            }
            else if (token.Kind == TokenKind.RightParen)
            {
                while (ops.Count > 0 && ops.Peek().Kind != TokenKind.LeftParen)
                {
                    output.Add(ops.Pop());
                }

                if (ops.Count > 0 && ops.Peek().Kind == TokenKind.LeftParen)
                {
                    ops.Pop();
                }
            }
        }

        while (ops.Count > 0)
        {
            output.Add(ops.Pop());
        }

        return output;
    }

    private static int Precedence(string op)
    {
        return op == "*" || op == "/" ? 2 : 1;
    }

    private static float EvaluateRpn(List<Token> rpn, Dictionary<string, float> context)
    {
        var stack = new Stack<float>();

        foreach (var token in rpn)
        {
            if (token.Kind == TokenKind.Number)
            {
                if (!float.TryParse(token.Text, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var value))
                {
                    value = 0f;
                }

                stack.Push(value);
            }
            else if (token.Kind == TokenKind.Identifier)
            {
                if (!context.TryGetValue(token.Text, out var value))
                {
                    value = 0f;
                }

                stack.Push(value);
            }
            else if (token.Kind == TokenKind.Operator)
            {
                if (stack.Count < 2)
                {
                    return 0f;
                }

                var right = stack.Pop();
                var left = stack.Pop();

                float result;
                if (token.Text == "+")
                {
                    result = left + right;
                }
                else if (token.Text == "-")
                {
                    result = left - right;
                }
                else if (token.Text == "*")
                {
                    result = left * right;
                }
                else if (token.Text == "/")
                {
                    result = right != 0f ? left / right : 0f;
                }
                else
                {
                    result = 0f;
                }

                stack.Push(result);
            }
        }

        return stack.Count > 0 ? stack.Pop() : 0f;
    }

    private enum TokenKind
    {
        Identifier,
        Number,
        Operator,
        LeftParen,
        RightParen
    }

    private readonly struct Token
    {
        public TokenKind Kind { get; }

        public string Text { get; }

        public Token(TokenKind kind, string text)
        {
            Kind = kind;
            Text = text;
        }
    }
}
