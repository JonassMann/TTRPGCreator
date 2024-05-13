using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TTRPGCreator.Database;
using TTRPGCreator.System;

namespace TTRPGCreator.Functionality
{
    public class Calculator
    {
        private static string[] _operators = { "-", "+", "/", "*", "^", "d" };
        private static Func<double, double, double>[] _operations = {
            (a1, a2) => a1 - a2,
            (a1, a2) => a1 + a2,
            (a1, a2) => a1 / a2,
            (a1, a2) => a1 * a2,
            (a1, a2) => Math.Pow(a1, a2),
            (a1, a2) => DiceRoller.Roll($"{a1}d{a2}") ?? 0
        };

        private static Dictionary<string, int> _operatorPrecedence = new Dictionary<string, int>
        {
            { "^", 3 },
            { "*", 2 },
            { "/", 2 },
            { "+", 1 },
            { "-", 1 }
        };

        public static async Task<double> Eval(string expression, ulong gameID, long characterID)
        {
            // Splits string into parts
            List<string> tokens = await getTokens(expression, gameID, characterID);
            Stack<double> operandStack = new Stack<double>();
            Stack<string> operatorStack = new Stack<string>();
            int tokenIndex = 0;

            string tokenString = "";
            foreach (string token in tokens)
            {
                tokenString += token + " ";
            }
            Console.WriteLine(tokenString);

            while (tokenIndex < tokens.Count)
            {
                string token = tokens[tokenIndex];

                switch (token)
                {
                    // Finds and evaluates sub-expressions
                    case "(":
                        string subExpr = getSubExpression(tokens, ref tokenIndex);
                        operandStack.Push(await Eval(subExpr, gameID, characterID));
                        continue;

                    case ")":
                        throw new ArgumentException("Mis-matched parentheses in expression");

                    // Finds and evaluates roll-expressions
                    case "[":
                        string rollExpr = getSubExpression(tokens, ref tokenIndex);
                        operandStack.Push(await Eval(rollExpr, gameID, characterID));
                        continue;

                    case "]":
                        throw new ArgumentException("Mis-matched parentheses in expression");

                    default:
                        break;
                }

                // If token is an operator
                if (Array.IndexOf(_operators, token) >= 0)
                {
                    while (operatorStack.Count > 0 && _operatorPrecedence[token] <= _operatorPrecedence[operatorStack.Peek()])
                    {
                        string op = operatorStack.Pop();
                        double arg2 = operandStack.Pop();
                        double arg1 = operandStack.Pop();
                        operandStack.Push(_operations[Array.IndexOf(_operators, op)](arg1, arg2));
                    }
                    operatorStack.Push(token);
                }
                // If token is a number (double)
                else if (double.TryParse(token, out double output))
                {
                    operandStack.Push(output);
                }
                tokenIndex += 1;
            }

            while (operatorStack.Count > 0)
            {
                string op = operatorStack.Pop();
                double arg2 = operandStack.Pop();
                double arg1 = operandStack.Pop();
                operandStack.Push(_operations[Array.IndexOf(_operators, op)](arg1, arg2));
            }

            return operandStack.Pop();
        }

        private static string getSubExpression(List<string> tokens, ref int index)
        {
            StringBuilder subExpr = new StringBuilder();
            string openParen = tokens[index];
            string closeParen = openParen == "(" ? ")" : "]";
            int parenlevels = 1;
            index += 1;
            while (index < tokens.Count && parenlevels > 0)
            {
                string token = tokens[index];
                if (tokens[index] == openParen)
                {
                    parenlevels += 1;
                }

                if (tokens[index] == closeParen)
                {
                    parenlevels -= 1;
                }

                if (parenlevels > 0)
                {
                    subExpr.Append(token);
                }

                index += 1;
            }

            if (parenlevels > 0)
            {
                throw new ArgumentException("Mis-matched parentheses in expression");
            }
            return subExpr.ToString();
        }

        private static async Task<List<string>> getTokens(string expression, ulong gameID, long characterID)
        {
            string operators = "()^*/+-[]d";
            List<string> tokens = new List<string>();
            StringBuilder sb = new StringBuilder();
            bool isReference = false;

            string[] diceParts = DataCache.gameRules[DataCache.gameList[gameID]].diceRoll.Split(new char[] { 'd', '+' }, StringSplitOptions.RemoveEmptyEntries);

            if (expression.StartsWith("-"))
                expression = "0" + expression;

            foreach (char c in expression.Replace(" ", string.Empty))
            {
                //If character is an operator
                if (!isReference && operators.IndexOf(c) >= 0)
                {
                    if (sb.Length > 0)
                    {
                        tokens.Add(sb.ToString());
                        sb.Length = 0;
                    }
                    tokens.Add(c.ToString());
                }
                else
                    switch (c)
                    {
                        case '$':
                            sb.Append(diceParts[0]);
                            break;

                        case '¤':
                            sb.Append(diceParts[1]);
                            break;

                        case '£':
                            sb.Append(diceParts.Length > 2 ? diceParts[2] : "0");
                            break;

                        case '@':
                            if (isReference)
                            {
                                DBEngine DBEngine = new DBEngine();

                                List<string> tags = new List<string>(sb.ToString().Split(','));
                                List<string> effects = await DBEngine.GetAllEffects(gameID, characterID, tags);

                                if (effects != null)
                                {
                                    foreach (string effect in effects)
                                    {
                                        tokens.Add($"(");
                                        tokens.Add($"{await Eval(effect, gameID, characterID)}");
                                        tokens.Add($")");
                                        tokens.Add("+");
                                    }
                                    tokens.RemoveAt(tokens.Count - 1);
                                }
                                sb.Length = 0;
                                isReference = false;
                            }
                            else
                            {
                                if (sb.Length > 0)
                                {
                                    tokens.Add(sb.ToString());
                                    sb.Length = 0;
                                }
                                isReference = true;
                            }
                            break;

                        default:
                            sb.Append(c);
                            break;
                    }
            }

            if (sb.Length > 0)
            {
                tokens.Add(sb.ToString());
            }

            return tokens;
        }
    }
}

//using System;
//using System.Collections.Generic;
//using System.Diagnostics;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace TTRPGCreator.Functionality
//{
//    public class Calculator
//    {
//        private static string[] _operators = { "-", "+", "/", "*", "^" };
//        private static Func<double, double, double>[] _operations = {
//            (a1, a2) => a1 - a2,
//            (a1, a2) => a1 + a2,
//            (a1, a2) => a1 / a2,
//            (a1, a2) => a1 * a2,
//            (a1, a2) => Math.Pow(a1, a2)
//        };

//        public static double Eval(string expression, string gameID = null)
//        {
//            // Splits string into parts
//            List<string> tokens = getTokens(expression);
//            Stack<double> operandStack = new Stack<double>();
//            Stack<string> operatorStack = new Stack<string>();
//            int tokenIndex = 0;

//            while (tokenIndex < tokens.Count)
//            {
//                string token = tokens[tokenIndex];

//                // Finds and evaluates sub-expressions
//                if (token == "(")
//                {
//                    string subExpr = getSubExpression(tokens, ref tokenIndex);
//                    operandStack.Push(Eval(subExpr));
//                    continue;
//                }
//                if (token == ")")
//                {
//                    throw new ArgumentException("Mis-matched parentheses in expression");
//                }
//                // If token is an operator
//                if (Array.IndexOf(_operators, token) >= 0)
//                {
//                    while (operatorStack.Count > 0 && Array.IndexOf(_operators, token) < Array.IndexOf(_operators, operatorStack.Peek()))
//                    {
//                        string op = operatorStack.Pop();
//                        double arg2 = operandStack.Pop();
//                        double arg1 = operandStack.Pop();
//                        operandStack.Push(_operations[Array.IndexOf(_operators, op)](arg1, arg2));
//                    }
//                    operatorStack.Push(token);
//                }
//                // If token is a number (double)
//                else if (double.TryParse(token, out double output))
//                {
//                    operandStack.Push(output);
//                }
//                tokenIndex += 1;
//            }

//            while (operatorStack.Count > 0)
//            {
//                string op = operatorStack.Pop();
//                double arg2 = operandStack.Pop();
//                double arg1 = operandStack.Pop();
//                operandStack.Push(_operations[Array.IndexOf(_operators, op)](arg1, arg2));
//            }

//            return operandStack.Pop();
//        }

//        private static string getSubExpression(List<string> tokens, ref int index)
//        {
//            StringBuilder subExpr = new StringBuilder();
//            int parenlevels = 1;
//            index += 1;
//            while (index < tokens.Count && parenlevels > 0)
//            {
//                string token = tokens[index];
//                if (tokens[index] == "(")
//                {
//                    parenlevels += 1;
//                }

//                if (tokens[index] == ")")
//                {
//                    parenlevels -= 1;
//                }

//                if (parenlevels > 0)
//                {
//                    subExpr.Append(token);
//                }

//                index += 1;
//            }

//            if (parenlevels > 0)
//            {
//                throw new ArgumentException("Mis-matched parentheses in expression");
//            }
//            return subExpr.ToString();
//        }

//        private static List<string> getTokens(string expression)
//        {
//            string operators = "()^*/+-";
//            List<string> tokens = new List<string>();
//            StringBuilder sb = new StringBuilder();

//            foreach (char c in expression.Replace(" ", string.Empty))
//            {
//                // If character is an operator
//                if (operators.IndexOf(c) >= 0)
//                {
//                    if (sb.Length > 0)
//                    {
//                        tokens.Add(sb.ToString());
//                        sb.Length = 0;
//                    }
//                    tokens.Add(c.ToString());
//                }
//                else
//                {
//                    sb.Append(c);
//                }
//            }

//            if (sb.Length > 0)
//            {
//                tokens.Add(sb.ToString());
//            }

//            return tokens;
//        }
//    }
//}
