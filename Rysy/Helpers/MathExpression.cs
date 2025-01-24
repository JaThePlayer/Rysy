using Rysy.Gui;

namespace Rysy.Helpers;

public static class MathExpression {


    public static ValidationResult TryEvaluate(ReadOnlySpan<char> expression, out double result) {
        var logger = new ListAbstractExpressionLogger();
                
        if (!AbstractExpression.TryParseUncached(expression, logger, out var expr)) {

            result = 0;
            return ValidationResult.Combine(logger.Errors
                .Select(x => new ValidationMessage { Level = LogLevel.Error, Tooltip = new(x) }).ToArray());
        }
        
        return Evaluate(expr, out result);
    }

    static ValidationResult Evaluate(AbstractExpression expr, out double result) {
        switch (expr) {
            case LiteralExpression<int> il:
                result = il.Value;
                return ValidationResult.Ok;
            case LiteralExpression<float> il:
                result = il.Value;
                return ValidationResult.Ok;
            case BinOpExpression binOp: {
                if (Evaluate(binOp.Left, out var left) is { IsOk: false } leftError) {
                    result = 0;
                    return leftError;
                }
                if (Evaluate(binOp.Right, out var right) is { IsOk: false } rightError) {
                    result = 0;
                    return rightError;
                }

                double? resOrNull = binOp.Operator switch {
                    BinOpExpression.Operators.Add => left + right,
                    BinOpExpression.Operators.Sub => left - right,
                    BinOpExpression.Operators.Div => left / right,
                    BinOpExpression.Operators.DivFloat => left / right,
                    BinOpExpression.Operators.Modulo => left % right,
                    BinOpExpression.Operators.Mul => left * right,
                    _ => null,
                };

                if (resOrNull is null) {
                    result = 0;
                    return ValidationResult.GenericError;
                }
                
                result = resOrNull.Value;
                return ValidationResult.Ok;
            }
            default:
                result = 0;
                return ValidationResult.GenericError;
        }
    }
}