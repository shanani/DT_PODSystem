// ✅ UPDATED: FormulaCalculationService - Modified for Query structure (keeping core functionality)
// OLD: Used TemplateVariable and CalculatedField entities  
// NEW: Uses QueryConstant and QueryOutput entities

using DT_PODSystem.Data;
using DT_PODSystem.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Data;
using System.Text;
using System.Text.RegularExpressions;

namespace DT_PODSystemWorker.Services
{
    public interface IFormulaCalculationService
    {
        /// <summary>
        /// ✅ UPDATED: Calculate outputs for a specific Query (not Template)
        /// </summary>
        Task<CalculationResult> CalculateQueryOutputsAsync(int queryId, Dictionary<string, object> extractedFields);
    }

    public class FormulaCalculationService : IFormulaCalculationService
    {
        private readonly ILogger<FormulaCalculationService> _logger;
        private readonly ApplicationDbContext _context;

        public FormulaCalculationService(
            ILogger<FormulaCalculationService> logger,
            ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        /// <summary>
        /// ✅ UPDATED: Calculate outputs for Query (replaced templateId with queryId)
        /// </summary>
        public async Task<CalculationResult> CalculateQueryOutputsAsync(int queryId, Dictionary<string, object> extractedFields)
        {
            var result = new CalculationResult();

            try
            {
                // ✅ UPDATED: Get QueryOutputs ordered by execution order (was CalculatedFields)
                var queryOutputs = await _context.QueryOutputs
                    .Where(qo => qo.QueryId == queryId && qo.IsActive && qo.IncludeInOutput)
                    .OrderBy(qo => qo.ExecutionOrder)
                    .ToListAsync();

                if (!queryOutputs.Any())
                {
                    result.Success = true; // No calculations needed
                    return result;
                }

                // ✅ UPDATED: Get QueryConstants (was TemplateVariables)
                // Include both query-specific constants and global constants
                var queryConstants = await _context.QueryConstants
                    .Where(qc => (qc.QueryId == queryId || qc.QueryId == null) && qc.IsActive)
                    .ToListAsync();

                // ✅ SEPARATE CONTEXTS: Never mix inputs with outputs (keeping core logic)
                var inputContext = new Dictionary<string, object>();     // Only for [Input:...] patterns
                var calculatedContext = new Dictionary<string, object>(); // Only for [Calculated:...] patterns 

                // ✅ INPUT CONTEXT: Add extracted fields (PDF inputs only)
                foreach (var field in extractedFields)
                {
                    inputContext[field.Key] = field.Value;
                    _logger.LogDebug($"📥 Input: {field.Key} = {field.Value}");
                }

                // ✅ UPDATED: INPUT CONTEXT: Add query constants (was template constants)
                foreach (var constant in queryConstants.Where(qc => qc.IsConstant))
                {
                    var constantValue = ConvertValue(constant.DefaultValue ?? "0", constant.DataType);
                    inputContext[constant.Name] = constantValue;
                    _logger.LogDebug($"🔧 Constant: {constant.Name} = {constantValue} (Global: {constant.IsGlobal})");
                }

                _logger.LogInformation($"🧮 [CONTEXTS] Input fields: {inputContext.Count}, Query outputs to process: {queryOutputs.Count}");

                // ✅ Initialize calculation details dictionary
                result.CalculationDetails = new Dictionary<string, string>();

                // ✅ UPDATED: Calculate each QueryOutput in execution order (was CalculatedField)
                foreach (var output in queryOutputs)
                {
                    try
                    {
                        _logger.LogDebug($"🔄 Calculating output: {output.Name} with formula: {output.FormulaExpression}");

                        // ✅ KEEPING CORE LOGIC: Pass separate contexts to prevent pollution
                        var calculationDetails = CalculateFormulaWithAudit(output.FormulaExpression, inputContext, calculatedContext);

                        if (calculationDetails.Result != null)
                        {
                            result.CalculatedOutputs[output.Name] = calculationDetails.Result;

                            // ✅ Store processed formula for auditing
                            result.CalculationDetails[output.Name] = calculationDetails.ProcessedFormula;

                            // ✅ KEEPING CORE LOGIC: Add to CALCULATED context only (for dependent calculations between outputs)
                            calculatedContext[output.Name] = calculationDetails.Result;

                            _logger.LogInformation($"✅ Calculated {output.Name}: {calculationDetails.Result} (Processed: {calculationDetails.ProcessedFormula})");
                        }
                        else
                        {
                            result.CalculatedOutputs[output.Name] = null;
                            result.CalculationDetails[output.Name] = calculationDetails.ProcessedFormula;
                            _logger.LogWarning($"❌ Could not calculate output: {output.Name}");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"💥 Error calculating output {output.Name}");
                        result.CalculatedOutputs[output.Name] = null;
                        result.CalculationDetails[output.Name] = $"Error: {ex.Message}";
                    }
                }

                result.Success = true;
                _logger.LogInformation($"🎯 Calculated {result.CalculatedOutputs.Count} query outputs");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 Error in query calculations");
                result.ErrorMessage = ex.Message;
            }

            return result;
        }

        // ✅ KEEPING ALL CORE CALCULATION LOGIC UNCHANGED
        // ✅ ENHANCED: Calculate formula with separate input/calculated contexts
        private FormulaCalculationDetails CalculateFormulaWithAudit(string formulaExpression,
            Dictionary<string, object> inputContext,
            Dictionary<string, object> calculatedContext)
        {
            var details = new FormulaCalculationDetails
            {
                OriginalFormula = formulaExpression
            };

            try
            {
                if (string.IsNullOrEmpty(formulaExpression))
                {
                    details.ProcessedFormula = "Empty formula";
                    return details;
                }

                // ✅ KEEPING CORE LOGIC: Process formula with separate contexts
                details.ProcessedFormula = ProcessFormulaExpression(formulaExpression, inputContext, calculatedContext);

                if (string.IsNullOrEmpty(details.ProcessedFormula))
                {
                    details.ProcessedFormula = "Formula processing failed";
                    return details;
                }

                // Evaluate mathematical expression with precision fix
                var rawResult = EvaluateExpression(details.ProcessedFormula);

                if (rawResult.HasValue)
                {
                    // ✅ FIX FLOATING-POINT PRECISION: Round to 10 decimal places
                    details.Result = Math.Round(rawResult.Value, 10);
                    _logger.LogDebug($"🔧 Precision fix: {rawResult.Value} → {details.Result}");
                }

                return details;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"💥 Error calculating formula: {formulaExpression}");
                details.ProcessedFormula = $"Error: {ex.Message}";
                return details;
            }
        }

        // ✅ KEEPING ALL PATTERN MATCHING LOGIC UNCHANGED
        // ✅ FIXED: Process formula with separate input/calculated contexts
        private string ProcessFormulaExpression(string formula,
            Dictionary<string, object> inputContext,
            Dictionary<string, object> calculatedContext)
        {
            var processedFormula = formula;

            try
            {
                _logger.LogDebug($"🔄 Processing formula: {formula}");

                // ✅ KEEPING: Replace [Input:FieldName#ID] patterns with INPUT CONTEXT ONLY
                var inputPattern = @"\[Input:([^#\]]+)#(\d+)\]";
                processedFormula = Regex.Replace(processedFormula, inputPattern, match =>
                {
                    var fieldName = match.Groups[1].Value.Trim();
                    _logger.LogDebug($"🔍 Replacing [Input:{fieldName}#...] with INPUT CONTEXT value");

                    if (inputContext.ContainsKey(fieldName))
                    {
                        var value = inputContext[fieldName];
                        var numericValue = ConvertToNumber(value)?.ToString() ?? "0";
                        _logger.LogDebug($"✅ Input field '{fieldName}': {value} → {numericValue}");
                        return numericValue;
                    }

                    _logger.LogWarning($"⚠️ Input field '{fieldName}' not found in input context, using 0");
                    return "0";
                });

                // ✅ KEEPING: Replace [Calculated:FieldName#ID] patterns with CALCULATED CONTEXT ONLY
                var calculatedPattern = @"\[Calculated:([^#\]]+)#(\d+)\]";
                processedFormula = Regex.Replace(processedFormula, calculatedPattern, match =>
                {
                    var fieldName = match.Groups[1].Value.Trim();
                    _logger.LogDebug($"🔍 Replacing [Calculated:{fieldName}#...] with CALCULATED CONTEXT value");

                    if (calculatedContext.ContainsKey(fieldName))
                    {
                        var value = calculatedContext[fieldName];
                        var numericValue = ConvertToNumber(value)?.ToString() ?? "0";
                        _logger.LogDebug($"✅ Calculated field '{fieldName}': {value} → {numericValue}");
                        return numericValue;
                    }

                    _logger.LogWarning($"⚠️ Calculated field '{fieldName}' not found in calculated context, using 0");
                    return "0";
                });

                // ✅ KEEPING: Replace [Variable:VariableName] patterns (constants)
                var variablePattern = @"\[Variable:([^\]]+)\]";
                processedFormula = Regex.Replace(processedFormula, variablePattern, match =>
                {
                    var variableName = match.Groups[1].Value.Trim();
                    _logger.LogDebug($"🔍 Replacing [Variable:{variableName}] with constant value");

                    if (inputContext.ContainsKey(variableName))
                    {
                        var value = inputContext[variableName];
                        var numericValue = ConvertToNumber(value)?.ToString() ?? "0";
                        _logger.LogDebug($"✅ Variable '{variableName}': {value} → {numericValue}");
                        return numericValue;
                    }

                    _logger.LogWarning($"⚠️ Variable '{variableName}' not found in context, using 0");
                    return "0";
                });

                // ✅ KEEPING: Replace [Constant:ConstantName] patterns 
                var constantPattern = @"\[Constant:([^\]]+)\]";
                processedFormula = Regex.Replace(processedFormula, constantPattern, match =>
                {
                    var constantName = match.Groups[1].Value.Trim();
                    _logger.LogDebug($"🔍 Replacing [Constant:{constantName}] with constant value");

                    if (inputContext.ContainsKey(constantName))
                    {
                        var value = inputContext[constantName];
                        var numericValue = ConvertToNumber(value)?.ToString() ?? "0";
                        _logger.LogDebug($"✅ Constant '{constantName}': {value} → {numericValue}");
                        return numericValue;
                    }

                    _logger.LogWarning($"⚠️ Constant '{constantName}' not found in context, using 0");
                    return "0";
                });

                _logger.LogDebug($"✅ Processed formula: {processedFormula}");
                return processedFormula;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"💥 Error processing formula: {formula}");
                return string.Empty;
            }
        }

        // ✅ KEEPING ALL EVALUATION METHODS UNCHANGED - FULL IMPLEMENTATION FROM YOUR CODE


        // ✅ KEEPING ALL YOUR CORE EVALUATION LOGIC UNCHANGED
        private decimal? EvaluateExpression(string expression)
        {
            try
            {
                _logger.LogDebug($"🧮 Evaluating: {expression}");

                // ✅ ENHANCED: Handle IF statements and other functions
                var result = EvaluateAdvancedExpression(expression);

                if (result.HasValue)
                {
                    _logger.LogDebug($"✅ Evaluation result: {result.Value}");
                    return result.Value;
                }

                _logger.LogWarning($"⚠️ Evaluation returned null for: {expression}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"💥 Error evaluating expression: {expression}");
            }

            return null;
        }

        private decimal? EvaluateAdvancedExpression(string expression)
        {
            try
            {
                // ✅ Handle IF function: IF(condition, trueValue, falseValue)
                var ifPattern = @"IF\s*\(\s*(.+?)\s*,\s*(.+?)\s*,\s*(.+?)\s*\)";
                var ifMatch = Regex.Match(expression, ifPattern, RegexOptions.IgnoreCase);

                if (ifMatch.Success)
                {
                    var condition = ifMatch.Groups[1].Value.Trim();
                    var trueValue = ifMatch.Groups[2].Value.Trim();
                    var falseValue = ifMatch.Groups[3].Value.Trim();

                    _logger.LogDebug($"🔀 IF statement: Condition='{condition}', True='{trueValue}', False='{falseValue}'");

                    // Evaluate condition using DataTable.Compute (supports comparison operators)
                    var conditionResult = EvaluateSimpleExpression(condition);
                    if (conditionResult.HasValue)
                    {
                        bool isTrue = conditionResult.Value != 0; // Non-zero = true
                        _logger.LogDebug($"🔀 Condition result: {conditionResult.Value} → {isTrue}");

                        if (isTrue)
                        {
                            return EvaluateSimpleExpression(trueValue);
                        }
                        else
                        {
                            return EvaluateSimpleExpression(falseValue);
                        }
                    }
                }

                // ✅ Handle POWER function: POWER(base, exponent)
                var powerPattern = @"POWER\s*\(\s*(.+?)\s*,\s*(.+?)\s*\)";
                var powerMatch = Regex.Match(expression, powerPattern, RegexOptions.IgnoreCase);

                if (powerMatch.Success)
                {
                    var baseValue = EvaluateSimpleExpression(powerMatch.Groups[1].Value.Trim());
                    var exponentValue = EvaluateSimpleExpression(powerMatch.Groups[2].Value.Trim());

                    if (baseValue.HasValue && exponentValue.HasValue)
                    {
                        var result = (decimal)Math.Pow((double)baseValue.Value, (double)exponentValue.Value);
                        _logger.LogDebug($"🔢 POWER: {baseValue}^{exponentValue} = {result}");
                        return result;
                    }
                }

                // ✅ Handle SQRT function: SQRT(value)
                var sqrtPattern = @"SQRT\s*\(\s*(.+?)\s*\)";
                var sqrtMatch = Regex.Match(expression, sqrtPattern, RegexOptions.IgnoreCase);

                if (sqrtMatch.Success)
                {
                    var value = EvaluateSimpleExpression(sqrtMatch.Groups[1].Value.Trim());
                    if (value.HasValue && value.Value >= 0)
                    {
                        var result = (decimal)Math.Sqrt((double)value.Value);
                        _logger.LogDebug($"🔢 SQRT: √{value} = {result}");
                        return result;
                    }
                }

                // ✅ Handle ABS function: ABS(value)
                var absPattern = @"ABS\s*\(\s*(.+?)\s*\)";
                var absMatch = Regex.Match(expression, absPattern, RegexOptions.IgnoreCase);

                if (absMatch.Success)
                {
                    var value = EvaluateSimpleExpression(absMatch.Groups[1].Value.Trim());
                    if (value.HasValue)
                    {
                        var result = Math.Abs(value.Value);
                        _logger.LogDebug($"🔢 ABS: |{value}| = {result}");
                        return result;
                    }
                }

                // ✅ Handle ROUND function: ROUND(value) or ROUND(value, digits)
                var roundPattern = @"ROUND\s*\(\s*(.+?)(?:\s*,\s*(.+?))?\s*\)";
                var roundMatch = Regex.Match(expression, roundPattern, RegexOptions.IgnoreCase);

                if (roundMatch.Success)
                {
                    var value = EvaluateSimpleExpression(roundMatch.Groups[1].Value.Trim());
                    if (value.HasValue)
                    {
                        int digits = 0;
                        if (roundMatch.Groups[2].Success)
                        {
                            var digitsValue = EvaluateSimpleExpression(roundMatch.Groups[2].Value.Trim());
                            if (digitsValue.HasValue)
                            {
                                digits = (int)digitsValue.Value;
                            }
                        }

                        var result = Math.Round(value.Value, digits);
                        _logger.LogDebug($"🔢 ROUND: Round({value}, {digits}) = {result}");
                        return result;
                    }
                }

                // ✅ Handle FLOOR function: FLOOR(value)
                var floorPattern = @"FLOOR\s*\(\s*(.+?)\s*\)";
                var floorMatch = Regex.Match(expression, floorPattern, RegexOptions.IgnoreCase);

                if (floorMatch.Success)
                {
                    var value = EvaluateSimpleExpression(floorMatch.Groups[1].Value.Trim());
                    if (value.HasValue)
                    {
                        var result = Math.Floor(value.Value);
                        _logger.LogDebug($"🔢 FLOOR: Floor({value}) = {result}");
                        return result;
                    }
                }

                // ✅ Handle CEIL/CEILING function: CEIL(value)
                var ceilPattern = @"CEIL(?:ING)?\s*\(\s*(.+?)\s*\)";
                var ceilMatch = Regex.Match(expression, ceilPattern, RegexOptions.IgnoreCase);

                if (ceilMatch.Success)
                {
                    var value = EvaluateSimpleExpression(ceilMatch.Groups[1].Value.Trim());
                    if (value.HasValue)
                    {
                        var result = Math.Ceiling(value.Value);
                        _logger.LogDebug($"🔢 CEIL: Ceiling({value}) = {result}");
                        return result;
                    }
                }

                // ✅ Handle MAX function: MAX(value1, value2, ...)
                var maxPattern = @"MAX\s*\(\s*(.+)\s*\)";
                var maxMatch = Regex.Match(expression, maxPattern, RegexOptions.IgnoreCase);

                if (maxMatch.Success)
                {
                    var values = SplitFunctionArguments(maxMatch.Groups[1].Value);
                    var numericValues = new List<decimal>();

                    foreach (var value in values)
                    {
                        var numValue = EvaluateSimpleExpression(value.Trim());
                        if (numValue.HasValue)
                        {
                            numericValues.Add(numValue.Value);
                        }
                    }

                    if (numericValues.Any())
                    {
                        var result = numericValues.Max();
                        _logger.LogDebug($"🔢 MAX: Max({string.Join(", ", numericValues)}) = {result}");
                        return result;
                    }
                }

                // ✅ Handle MIN function: MIN(value1, value2, ...)
                var minPattern = @"MIN\s*\(\s*(.+)\s*\)";
                var minMatch = Regex.Match(expression, minPattern, RegexOptions.IgnoreCase);

                if (minMatch.Success)
                {
                    var values = SplitFunctionArguments(minMatch.Groups[1].Value);
                    var numericValues = new List<decimal>();

                    foreach (var value in values)
                    {
                        var numValue = EvaluateSimpleExpression(value.Trim());
                        if (numValue.HasValue)
                        {
                            numericValues.Add(numValue.Value);
                        }
                    }

                    if (numericValues.Any())
                    {
                        var result = numericValues.Min();
                        _logger.LogDebug($"🔢 MIN: Min({string.Join(", ", numericValues)}) = {result}");
                        return result;
                    }
                }

                // ✅ Handle SUM function: SUM(value1, value2, ...)
                var sumPattern = @"SUM\s*\(\s*(.+)\s*\)";
                var sumMatch = Regex.Match(expression, sumPattern, RegexOptions.IgnoreCase);

                if (sumMatch.Success)
                {
                    var values = SplitFunctionArguments(sumMatch.Groups[1].Value);
                    var numericValues = new List<decimal>();

                    foreach (var value in values)
                    {
                        var numValue = EvaluateSimpleExpression(value.Trim());
                        if (numValue.HasValue)
                        {
                            numericValues.Add(numValue.Value);
                        }
                    }

                    if (numericValues.Any())
                    {
                        var result = numericValues.Sum();
                        _logger.LogDebug($"🔢 SUM: Sum({string.Join(", ", numericValues)}) = {result}");
                        return result;
                    }
                }

                // ✅ Handle AVG/AVERAGE function: AVG(value1, value2, ...)
                var avgPattern = @"AVG(?:ERAGE)?\s*\(\s*(.+)\s*\)";
                var avgMatch = Regex.Match(expression, avgPattern, RegexOptions.IgnoreCase);

                if (avgMatch.Success)
                {
                    var values = SplitFunctionArguments(avgMatch.Groups[1].Value);
                    var numericValues = new List<decimal>();

                    foreach (var value in values)
                    {
                        var numValue = EvaluateSimpleExpression(value.Trim());
                        if (numValue.HasValue)
                        {
                            numericValues.Add(numValue.Value);
                        }
                    }

                    if (numericValues.Any())
                    {
                        var result = numericValues.Average();
                        _logger.LogDebug($"🔢 AVG: Average({string.Join(", ", numericValues)}) = {result}");
                        return result;
                    }
                }

                // ✅ Handle AND function: AND(condition1, condition2, ...)
                var andPattern = @"AND\s*\(\s*(.+)\s*\)";
                var andMatch = Regex.Match(expression, andPattern, RegexOptions.IgnoreCase);

                if (andMatch.Success)
                {
                    var conditions = SplitFunctionArguments(andMatch.Groups[1].Value);

                    foreach (var condition in conditions)
                    {
                        var conditionResult = EvaluateSimpleExpression(condition.Trim());
                        if (!conditionResult.HasValue || conditionResult.Value == 0)
                        {
                            _logger.LogDebug($"🔢 AND: One condition failed, returning false");
                            return 0; // False
                        }
                    }

                    _logger.LogDebug($"🔢 AND: All conditions passed, returning true");
                    return 1; // True
                }

                // ✅ Handle OR function: OR(condition1, condition2, ...)
                var orPattern = @"OR\s*\(\s*(.+)\s*\)";
                var orMatch = Regex.Match(expression, orPattern, RegexOptions.IgnoreCase);

                if (orMatch.Success)
                {
                    var conditions = SplitFunctionArguments(orMatch.Groups[1].Value);

                    foreach (var condition in conditions)
                    {
                        var conditionResult = EvaluateSimpleExpression(condition.Trim());
                        if (conditionResult.HasValue && conditionResult.Value != 0)
                        {
                            _logger.LogDebug($"🔢 OR: One condition passed, returning true");
                            return 1; // True
                        }
                    }

                    _logger.LogDebug($"🔢 OR: All conditions failed, returning false");
                    return 0; // False
                }

                // ✅ Handle NOT function: NOT(condition)
                var notPattern = @"NOT\s*\(\s*(.+?)\s*\)";
                var notMatch = Regex.Match(expression, notPattern, RegexOptions.IgnoreCase);

                if (notMatch.Success)
                {
                    var condition = EvaluateSimpleExpression(notMatch.Groups[1].Value.Trim());
                    if (condition.HasValue)
                    {
                        var result = condition.Value == 0 ? 1 : 0; // Invert boolean
                        _logger.LogDebug($"🔢 NOT: Not({condition}) = {result}");
                        return result;
                    }
                }

                // ✅ Fallback: Use simple expression evaluation
                return EvaluateSimpleExpression(expression);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"💥 Error in advanced expression evaluation: {expression}");
                return null;
            }
        }

        // ✅ Helper method to split function arguments properly (handles nested parentheses)
        private List<string> SplitFunctionArguments(string arguments)
        {
            var result = new List<string>();
            var current = new StringBuilder();
            int parenthesesLevel = 0;

            foreach (char c in arguments)
            {
                if (c == ',' && parenthesesLevel == 0)
                {
                    result.Add(current.ToString().Trim());
                    current.Clear();
                }
                else
                {
                    if (c == '(') parenthesesLevel++;
                    else if (c == ')') parenthesesLevel--;
                    current.Append(c);
                }
            }

            if (current.Length > 0)
            {
                result.Add(current.ToString().Trim());
            }

            return result;
        }

        private decimal? EvaluateSimpleExpression(string expression)
        {
            try
            {
                // Use DataTable.Compute for basic math and comparisons
                var table = new DataTable();
                var result = table.Compute(expression, null);

                if (result != null && result != DBNull.Value)
                {
                    return Convert.ToDecimal(result);
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug($"⚠️ Simple evaluation failed for '{expression}': {ex.Message}");
            }

            return null;
        }

        private decimal? ConvertToNumber(object? value)
        {
            if (value == null) return null;

            try
            {
                return Convert.ToDecimal(value);
            }
            catch
            {
                if (decimal.TryParse(value.ToString(), out var result))
                    return result;
            }

            return null;
        }

        private object ConvertValue(string value, DataTypeEnum dataType)
        {
            try
            {
                return dataType switch
                {
                    DataTypeEnum.Number => decimal.Parse(value),
                    DataTypeEnum.Currency => decimal.Parse(value.Replace("$", "").Replace(",", "")),
                    DataTypeEnum.Date => DateTime.Parse(value),
                    DataTypeEnum.Boolean => bool.Parse(value),
                    _ => value
                };
            }
            catch
            {
                return value; // Return as string if conversion fails
            }
        }
    }

    /// <summary>
    /// ✅ KEEPING: Supporting classes for audit (unchanged)
    /// </summary>
    internal class FormulaCalculationDetails
    {
        public string OriginalFormula { get; set; } = string.Empty;
        public string ProcessedFormula { get; set; } = string.Empty;
        public decimal? Result { get; set; }
    }

    /// <summary>
    /// ✅ KEEPING: Calculation result structure (unchanged)
    /// </summary>
    public class CalculationResult
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public Dictionary<string, object?> CalculatedOutputs { get; set; } = new();
        public Dictionary<string, string>? CalculationDetails { get; set; }
    }
}