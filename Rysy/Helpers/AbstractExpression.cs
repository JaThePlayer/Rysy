﻿using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Rysy.Helpers;

internal sealed class InterpolatedStringExpression(IList<AbstractExpression> args) : AbstractExpression {
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public IList<AbstractExpression> Arguments => args;
}

internal sealed class SimpleCommandExpression(string name) : AbstractExpression {
    public string Name => name;
}

internal sealed class FunctionCommandExpression(string name, IList<AbstractExpression> args) : AbstractExpression {
    public string Name => name;
    
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public IList<AbstractExpression> Arguments => args;
}

internal sealed class BinOpExpression(AbstractExpression left, AbstractExpression right, BinOpExpression.Operators op) : AbstractExpression {
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public AbstractExpression Left => left;

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public AbstractExpression Right => right;

    public Operators Operator => op;
    
    public enum Operators {
        Add, Sub, Mul, Div, DivFloat, Modulo,
        And, Or,
        BitwiseAnd, BitwiseOr,
        Eq, Ne, Lt, Le, Gt, Ge
    }
}

internal sealed class GetSessionVariableExpression(AbstractExpression name, GetSessionVariableExpression.Types type)  : AbstractExpression {
    public AbstractExpression Name { get; set; } = name;
    
    public Types VariableType => type;
    
    public enum Types {
        Flag, Counter, Slider,
    }
}

internal sealed class InvertExpression(AbstractExpression expr) : AbstractExpression {
    public AbstractExpression Expression => expr;
}

internal sealed class LiteralExpression<T>(T value) : AbstractExpression {
    public T Value => value;
}

public interface IAbstractExpressionErrorLogger {
    void Error(string message);
}

public sealed class ListAbstractExpressionLogger : IAbstractExpressionErrorLogger {
    public List<string> Errors { get; } = new List<string>();
    
    public void Error(string message) {
        Errors.Add(message);
    }
}

internal partial class AbstractExpression {
    protected AbstractExpression()
    {
        
    }

    public string TypeName => GetType().Name;
    
    // ':' is not banned due to some vanilla flags using it - ternary operations will need something else
    [GeneratedRegex(@"[\~\^\[\]\{\};\?]", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex ReservedCharsRegex();
    
    internal static readonly JsonSerializerOptions JsonOptions = new() {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    private static readonly Dictionary<string, AbstractExpression> Cache = [];

    public override string ToString() {
        return JsonSerializer.Serialize<object>(this, JsonOptions);
    }

    public static bool TryParseUncached(ReadOnlySpan<char> str, IAbstractExpressionErrorLogger logger, [NotNullWhen(true)] out AbstractExpression? expression) {
        if (ExpressionToken.Tokenize(str, logger, out var tokens) is not ExpressionToken.TokenizerState.End) {
            logger.Error($"Failed to tokenize Session Expression:\n{str}");
            expression = new LiteralExpression<int>(0);
            return false;
        }
        
        return Parse(CollectionsMarshal.AsSpan(tokens), logger, out expression);
    }
    
    public static bool TryParseCached(string str, IAbstractExpressionErrorLogger logger, [NotNullWhen(true)] out AbstractExpression? expression) {
        ref var cacheRef = ref CollectionsMarshal.GetValueRefOrAddDefault(Cache, str, out _);

        if (cacheRef is null) {
            var ret = TryParseUncached(str, logger, out expression);
            cacheRef = expression;
            return ret;
        }
        
        expression = cacheRef;
        return true;
    }

    internal static bool Parse(ReadOnlySpan<ExpressionToken> tokens, IAbstractExpressionErrorLogger logger, [NotNullWhen(true)] out AbstractExpression? expression)
    {
        expression = null;

        if (tokens.Length == 0)
            return false;

        if (tokens is [var only])
        {
            if (only is { Kind: ExpressionToken.Kinds.Bracket, Operand: List<ExpressionToken> bracketTokens })
            {
                return Parse(CollectionsMarshal.AsSpan(bracketTokens), logger, out expression);
            }
            
            if (only is { Kind: ExpressionToken.Kinds.InterpolatedString, Operand: List<List<ExpressionToken>> holes })
            {
                var args = new List<AbstractExpression>(holes.Count);
                foreach (var innerTokens in holes)
                {
                    if (!Parse(CollectionsMarshal.AsSpan(innerTokens), logger, out var innerExpr))
                        return false;
                    args.Add(innerExpr);
                }

                expression = new InterpolatedStringExpression(args);
                return true;
            }
            
            if (only is { Kind: ExpressionToken.Kinds.Command, Operand: CommandTokenOperand commandOperand })
            {
                if (commandOperand.Arguments is null)
                {
                    expression = new SimpleCommandExpression(commandOperand.Name);
                    return true;
                }
                
                var args = new List<AbstractExpression>(commandOperand.Arguments.Count);
                foreach (var innerTokens in commandOperand.Arguments)
                {
                    if (!Parse(CollectionsMarshal.AsSpan(innerTokens), logger, out var innerExpr))
                        return false;
                    args.Add(innerExpr);
                }

                expression = new FunctionCommandExpression(commandOperand.Name, args);
                return true;
            }
            
            expression = CreateExpressionFromSimpleToken(only);

            if (expression is null)
            {
                logger.Error($"Invalid expression: {string.Join(" ", tokens.ToArray().ToList() )}");
                return false;
            }

            return true;
        }
        
        if (tokens is [{ IsUnaryOnStrings: true } stringUnary, var stringUnaryArg])
        {
            // simplify expressions like `#"strLit"` into just `#stringLit`
            if (stringUnaryArg is { Kind: ExpressionToken.Kinds.LitString, Operand: string op })
                return Parse([new ExpressionToken(stringUnary.Kind) { Operand = op }], logger, out expression);

            if (stringUnaryArg is { Kind: ExpressionToken.Kinds.InterpolatedString })
            {
                if (!Parse([stringUnaryArg], logger, out var unaryStringInterpol))
                    return false;

                expression = CreateExpressionFromSimpleToken(stringUnary)!;
                switch (expression) {
                    case GetSessionVariableExpression sessionVariableExpression:
                        sessionVariableExpression.Name = unaryStringInterpol;
                        break;
                }
                return true;
            }
        }
        
        AbstractExpression? left = null;
        AbstractExpression? right = null;

        // Search for && and ||
        if (FindAny(tokens, out var preBin, out var postBin, out var kind,
                (int)ExpressionToken.Kinds.And, (int)ExpressionToken.Kinds.Or))
        {
            return HandleBinOp(preBin, postBin, out expression);
        }
        //< > != == <= >=
        if (FindAny(tokens, out preBin, out postBin, out kind,
                (int)ExpressionToken.Kinds.Eq, (int)ExpressionToken.Kinds.Ne,
                (int)ExpressionToken.Kinds.Le, (int)ExpressionToken.Kinds.Lt,
                (int)ExpressionToken.Kinds.Ge, (int)ExpressionToken.Kinds.Gt))
        {
            return HandleBinOp(preBin, postBin, out expression);
        }
        
        // | &
        if (FindAny(tokens, out preBin, out postBin, out kind,
                (int)ExpressionToken.Kinds.BitwiseAnd, (int)ExpressionToken.Kinds.BitwiseOr))
        {
            return HandleBinOp(preBin, postBin, out expression);
        }
        
        // unary +-
        // +-
        if (FindAny(tokens, out preBin, out postBin, out kind,
                (int)ExpressionToken.Kinds.Add, (int)ExpressionToken.Kinds.Sub))
        {
            if (preBin.IsEmpty)
            {
                // unary ops
                if (!Parse(postBin, logger, out right))
                    return false;
                
                expression = new BinOpExpression(new LiteralExpression<int>(0), right, kind switch {
                    ExpressionToken.Kinds.Add => BinOpExpression.Operators.Add,
                    ExpressionToken.Kinds.Sub => BinOpExpression.Operators.Sub,
                });
                
                return true;
            }
            
            return HandleBinOp(preBin, postBin, out expression);
        }
        
        // * / % //
        if (FindAny(tokens, out preBin, out postBin, out kind,
                (int)ExpressionToken.Kinds.Mul, (int)ExpressionToken.Kinds.Div, (int)ExpressionToken.Kinds.DivFloat, (int)ExpressionToken.Kinds.Modulo))
        {
            return HandleBinOp(preBin, postBin, out expression);
        }
        
        if (tokens is [{ Kind: ExpressionToken.Kinds.Invert }, .. var toInvert])
        {
            if (!Parse(toInvert, logger, out var invertedExpr))
                return false;

            expression = new InvertExpression(invertedExpr);
            return true;
        }

        return false;

        bool HandleBinOp(ReadOnlySpan<ExpressionToken> preBin, ReadOnlySpan<ExpressionToken> postBin, 
            [NotNullWhen(true)] out AbstractExpression? expression)
        {
            expression = null;

            if (preBin.IsEmpty)
            {
                logger.Error("Binary operator without left-hand side.");
                return false;
            }
            
            if (postBin.IsEmpty)
            {
                logger.Error("Binary operator without right-hand side.");
                return false;
            }
            
            if (!Parse(preBin, logger, out left))
                return false;
            if (!Parse(postBin, logger, out right))
                return false;

            expression = new BinOpExpression(left, right, kind switch {
                ExpressionToken.Kinds.And => BinOpExpression.Operators.And,
                ExpressionToken.Kinds.Or => BinOpExpression.Operators.Or,
                ExpressionToken.Kinds.Add => BinOpExpression.Operators.Add,
                ExpressionToken.Kinds.Sub => BinOpExpression.Operators.Sub,
                ExpressionToken.Kinds.Mul => BinOpExpression.Operators.Mul,
                ExpressionToken.Kinds.Div => BinOpExpression.Operators.Div,
                ExpressionToken.Kinds.DivFloat => BinOpExpression.Operators.DivFloat,
                ExpressionToken.Kinds.Modulo => BinOpExpression.Operators.Modulo,
                ExpressionToken.Kinds.Eq => BinOpExpression.Operators.Eq,
                ExpressionToken.Kinds.Ne => BinOpExpression.Operators.Ne,
                ExpressionToken.Kinds.Gt => BinOpExpression.Operators.Gt,
                ExpressionToken.Kinds.Ge => BinOpExpression.Operators.Ge,
                ExpressionToken.Kinds.Lt => BinOpExpression.Operators.Lt,
                ExpressionToken.Kinds.Le => BinOpExpression.Operators.Le,
                ExpressionToken.Kinds.BitwiseAnd => BinOpExpression.Operators.BitwiseAnd,
                ExpressionToken.Kinds.BitwiseOr => BinOpExpression.Operators.BitwiseOr,
            });
            return true;
        }

        bool FindAny(ReadOnlySpan<ExpressionToken> tokens, out ReadOnlySpan<ExpressionToken> left, out ReadOnlySpan<ExpressionToken> right, out ExpressionToken.Kinds kind, 
            params ReadOnlySpan<int> kinds)
        {
            for (int i = 0; i < tokens.Length; i++)
            {
                var token = tokens[i];
                if (kinds.Contains((int)token.Kind))
                {
                    left = tokens[..i];
                    right = tokens[(i + 1)..];
                    kind = token.Kind;
                    return true;
                }
            }

            left = default;
            right = default;
            kind = default;
            return false;
        }
    }

    private static AbstractExpression? CreateExpressionFromSimpleToken(ExpressionToken only)
    {
        return only.Kind switch
        {
            ExpressionToken.Kinds.Add => null,
            ExpressionToken.Kinds.Sub => null,
            ExpressionToken.Kinds.Mul => null,
            ExpressionToken.Kinds.Div => null,
            ExpressionToken.Kinds.DivFloat => null,
            ExpressionToken.Kinds.Flag =>
                new GetSessionVariableExpression(new LiteralExpression<string>(only.Operand?.ToString() ?? ""), GetSessionVariableExpression.Types.Flag),
            ExpressionToken.Kinds.Counter =>
                new GetSessionVariableExpression(new LiteralExpression<string>(only.Operand?.ToString() ?? ""), GetSessionVariableExpression.Types.Counter),
            ExpressionToken.Kinds.Slider =>
                new GetSessionVariableExpression(new LiteralExpression<string>(only.Operand?.ToString() ?? ""), GetSessionVariableExpression.Types.Slider),
            ExpressionToken.Kinds.LitString => new LiteralExpression<string>(only.Operand?.ToString() ?? ""),
            ExpressionToken.Kinds.LitInt => new LiteralExpression<int>((int)only.Operand!),
            ExpressionToken.Kinds.LitFloat => new LiteralExpression<float>((float)only.Operand!),
            ExpressionToken.Kinds.InterpolatedString => null,
            ExpressionToken.Kinds.And => null,
            ExpressionToken.Kinds.Or => null,
            ExpressionToken.Kinds.Bracket => null,
            _ => throw new ArgumentOutOfRangeException(nameof(only))
        };
    }
}