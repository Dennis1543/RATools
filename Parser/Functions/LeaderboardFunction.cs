﻿using RATools.Data;
using RATools.Parser.Internal;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace RATools.Parser.Functions
{
    internal class LeaderboardFunction : FunctionDefinitionExpression
    {
        public LeaderboardFunction()
            : base("leaderboard")
        {
            Parameters.Add(new VariableDefinitionExpression("title"));
            Parameters.Add(new VariableDefinitionExpression("description"));
            Parameters.Add(new VariableDefinitionExpression("start"));
            Parameters.Add(new VariableDefinitionExpression("cancel"));
            Parameters.Add(new VariableDefinitionExpression("submit"));
            Parameters.Add(new VariableDefinitionExpression("value"));
            Parameters.Add(new VariableDefinitionExpression("format"));

            DefaultParameters["format"] = new StringConstantExpression("value");
        }

        public override bool Evaluate(InterpreterScope scope, out ExpressionBase result)
        {
            var leaderboard = new Leaderboard();

            var stringExpression = GetStringParameter(scope, "title", out result);
            if (stringExpression == null)
                return false;
            leaderboard.Title = stringExpression.Value;

            stringExpression = GetStringParameter(scope, "description", out result);
            if (stringExpression == null)
                return false;
            leaderboard.Description = stringExpression.Value;

            leaderboard.Start = ProcessTrigger(scope, "start", out result);
            if (leaderboard.Start == null)
                return false;

            leaderboard.Cancel = ProcessTrigger(scope, "cancel", out result);
            if (leaderboard.Cancel == null)
                return false;

            leaderboard.Submit = ProcessTrigger(scope, "submit", out result);
            if (leaderboard.Submit == null)
                return false;

            leaderboard.Value = ProcessValue(scope, "value", out result);
            if (leaderboard.Value == null)
                return false;

            var format = GetStringParameter(scope, "format", out result);
            if (format == null)
                return false;

            leaderboard.Format = Leaderboard.ParseFormat(format.Value);
            if (leaderboard.Format == ValueFormat.None)
            {
                result = new ParseErrorExpression(format.Value + " is not a supported leaderboard format", format);
                return false;
            }

            var functionCall = scope.GetContext<FunctionCallExpression>();
            if (functionCall != null && functionCall.FunctionName.Name == this.Name.Name)
                leaderboard.SourceLine = functionCall.Location.Start.Line;

            var context = scope.GetContext<AchievementScriptContext>();
            Debug.Assert(context != null);
            context.Leaderboards.Add(leaderboard);
            return true;
        }

        private string ProcessTrigger(InterpreterScope scope, string parameter, out ExpressionBase result)
        {
            var expression = GetParameter(scope, parameter, out result);
            if (expression == null)
                return null;

            return TriggerBuilderContext.GetConditionString(expression, scope, out result);
        }

        private string ProcessValue(InterpreterScope scope, string parameter, out ExpressionBase result)
        {
            var expression = GetParameter(scope, parameter, out result);
            if (expression == null)
                return null;

            var functionCallExpression = expression as FunctionCallExpression;
            if (functionCallExpression != null)
            {
                var functionDefinition = scope.GetFunction(functionCallExpression.FunctionName.Name);
                if (functionDefinition is MaxOfFunction)
                {
                    var builder = new StringBuilder();
                    foreach (var value in functionCallExpression.Parameters)
                    {
                        if (builder.Length > 0)
                            builder.Append('$');

                        builder.Append(TriggerBuilderContext.GetValueString(value, scope, out result));
                    }
                    return builder.ToString();
                }
            }

            return TriggerBuilderContext.GetValueString(expression, scope, out result);
        }
    }

    internal class MaxOfFunction : FunctionDefinitionExpression
    {
        public MaxOfFunction()
            : base("max_of")
        {
            Parameters.Add(new VariableDefinitionExpression("..."));
        }

        public override bool ReplaceVariables(InterpreterScope scope, out ExpressionBase result)
        {
            var varargs = GetParameter(scope, "varargs", out result) as ArrayExpression;
            if (varargs == null)
            {
                if (!(result is ParseErrorExpression))
                    result = new ParseErrorExpression("unexpected varargs");
                return false;
            }

            var parameters = new List<ExpressionBase>();
            foreach (var entry in varargs.Entries)
            {
                if (!entry.ReplaceVariables(scope, out result))
                    return false;

                parameters.Add(result);
            }

            result = new FunctionCallExpression(Name.Name, parameters.ToArray());
            CopyLocation(result);
            return true;
        }
    }
}
