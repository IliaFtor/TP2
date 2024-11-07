using Loyc.Syntax;
using Loyc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LesGraphingCalc.CalculeteModel
{
    using number = System.Double;
    abstract class CalculatorCore
    {
        static readonly Symbol sy_x = (Symbol)"x", sy_y = (Symbol)"y";
        // Base class constructor and fields
        public CalculatorCore(LNode Expr, Dictionary<Symbol, LNode> Vars, CalcRange XRange)
        {
            this.Expr = Expr;
            this.Vars = Vars;
            this.XRange = XRange;
        }

        public LNode Expr { get; private set; }
        public Dictionary<Symbol, LNode> Vars { get; private set; }
        public CalcRange XRange { get; private set; }
        public abstract CalculatorCore WithExpr(LNode newValue);
        public abstract CalculatorCore WithVars(Dictionary<Symbol, LNode> newValue);
        public abstract CalculatorCore WithXRange(CalcRange newValue);
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public LNode Item1
        {
            get
            {
                return Expr;
            }
        }
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public Dictionary<Symbol, LNode> Item2
        {
            get
            {
                return Vars;
            }
        }
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public CalcRange Item3
        {
            get
            {
                return XRange;
            }
        }
        public object Results { get; protected set; }

        public abstract object Run();
        public abstract number? GetValueAt(int x, int y);

        public static CalculatorCore New(LNode expr, Dictionary<Symbol, LNode> vars, CalcRange xRange, CalcRange yRange)
        {

            bool isEquation = expr.Calls(CodeSymbols.Assign, 2) || expr.Calls(CodeSymbols.Eq, 2), usesY = false;
            if (!isEquation)
            {
                LNode zero = LNode.Literal((double)0);
                Func<Symbol, double> lookup = null;
                lookup = name => name == sy_x || (usesY |= name == sy_y) ? 0 : Eval(vars[name], lookup);
                Eval(expr, lookup);
            }
            if (isEquation || usesY)
                return new Calculator3D(expr, vars, xRange, yRange);
            else
                return new Calculator2D(expr, vars, xRange);
        }

        // GUI
        public static Dictionary<Symbol, LNode> ParseVarList(IEnumerable<LNode> varList)
        {
            var vars = new Dictionary<Symbol, LNode>();
            foreach (LNode assignment in varList)
            {
                {
                    LNode expr, @var;
                    if (assignment.Calls(CodeSymbols.Assign, 2) && (@var = assignment.Args[0]) != null && (expr = assignment.Args[1]) != null)
                    {
                        if (!@var.IsId)
                            throw new ArgumentException("Left-hand side of '=' must be a variable name: {0}".Localized(@var));

                        try { expr = LNode.Literal(Eval(expr, vars)); } catch { }
                        vars.Add(@var.Name, expr);
                    }
                    else
                        throw new ArgumentException("Expected assignment expression: {0}".Localized(assignment));
                };
            }
            return vars;
        }

        public static number Eval(LNode expr, Dictionary<Symbol, LNode> vars)
        {
            Func<Symbol, number> lookup = null;
            lookup = name => Eval(vars[name], lookup);
            return Eval(expr, lookup);
        }

        // Evaluates an expression
        public static number Eval(LNode expr, Func<Symbol, number> lookup)
        {
            // Проверка на литерал и идентификатор
            if (expr.IsLiteral) return expr.Value is number ? (number)expr.Value : (number)Convert.ToDouble(expr.Value);
            if (expr.IsId) return lookup(expr.Name);

            // Бинарные операции
            if (expr.ArgCount == 2)
            {
                var a = expr.Args[0];
                var b = expr.Args[1];
                return expr.Calls(CodeSymbols.Add) ? Eval(a, lookup) + Eval(b, lookup) :
                       expr.Calls(CodeSymbols.Mul) ? Eval(a, lookup) * Eval(b, lookup) :
                       expr.Calls(CodeSymbols.Sub) ? Eval(a, lookup) - Eval(b, lookup) :
                       expr.Calls(CodeSymbols.Div) ? Eval(a, lookup) / Eval(b, lookup) :
                       expr.Calls(CodeSymbols.Mod) ? Eval(a, lookup) % Eval(b, lookup) :
                       expr.Calls(CodeSymbols.Exp) ? (number)Math.Pow(Eval(a, lookup), Eval(b, lookup)) :
                       expr.Calls(CodeSymbols.GT) ? Eval(a, lookup) > Eval(b, lookup) ? 1 : 0 :
                       expr.Calls(CodeSymbols.LT) ? Eval(a, lookup) < Eval(b, lookup) ? 1 : 0 :
                       expr.Calls(CodeSymbols.Eq) ? Eval(a, lookup) == Eval(b, lookup) ? 1 : 0 :
                       expr.Calls(CodeSymbols.Neq) ? Eval(a, lookup) != Eval(b, lookup) ? 1 : 0 :
                       expr.Calls(CodeSymbols.And) ? Eval(a, lookup) != 0 && Eval(b, lookup) != 0 ? 1 : 0 :
                       expr.Calls(CodeSymbols.Or) ? Eval(a, lookup) != 0 || Eval(b, lookup) != 0 ? 1 : 0 :
                       throw new ArgumentException($"Unsupported binary expression: {expr}");
            }

            // Унарные операции
            if (expr.ArgCount == 1)
            {
                var a = expr.Args[0];
                return expr.Calls(CodeSymbols.Sub) ? -Eval(a, lookup) :
                       expr.Calls(CodeSymbols.Not) ? Eval(a, lookup) == 0 ? 1 : 0 :
                       expr.Calls((Symbol)"square") ? Math.Pow(Eval(a, lookup), 2) :
                       expr.Calls((Symbol)"sqrt") ? Math.Sqrt(Eval(a, lookup)) :
                       expr.Calls((Symbol)"sin") ? Math.Sin(Eval(a, lookup)) :
                       expr.Calls((Symbol)"cos") ? Math.Cos(Eval(a, lookup)) :
                       expr.Calls((Symbol)"tan") ? Math.Tan(Eval(a, lookup)) :
                       expr.Calls((Symbol)"acos") ? Math.Acos(Eval(a, lookup)) :
                       expr.Calls((Symbol)"atan") ? Math.Atan(Eval(a, lookup)) :
                       expr.Calls((Symbol)"exp") ? Math.Exp(Eval(a, lookup)) :
                       expr.Calls((Symbol)"ln") ? Math.Log(Eval(a, lookup)) :
                       expr.Calls((Symbol)"log") ? Math.Log10(Eval(a, lookup)) :
                       expr.Calls((Symbol)"ceil") ? Math.Ceiling(Eval(a, lookup)) :
                       expr.Calls((Symbol)"floor") ? Math.Floor(Eval(a, lookup)) :
                       expr.Calls((Symbol)"sign") ? Math.Sign(Eval(a, lookup)) :
                       expr.Calls((Symbol)"abs") ? Math.Abs(Eval(a, lookup)) :
                       throw new ArgumentException($"Unsupported unary expression: {expr}");
            }
            // Интегрирование методом трапеций
            if (expr.Calls((Symbol)"integrate", 4))
            {
                var funcExpr = expr.Args[0];      // Функция для интегрирования
                var a = Eval(expr.Args[1], lookup); // Нижний предел интегрирования
                var b = Eval(expr.Args[2], lookup); // Верхний предел интегрирования
                var n = (int)Eval(expr.Args[3], lookup); // Число интервалов

                number h = (b - a) / n;
                number sum = 0.0;

                for (int i = 0; i <= n; i++)
                {
                    number x = a + i * h;

                    Func<Symbol, number> localLookup = symbol => symbol.Name == "x" ? x : lookup(symbol);

                    var factor = (i == 0 || i == n) ? 0.5 : 1.0;
                    sum += factor * Eval(funcExpr, localLookup);
                }

                return sum * h;
            }

            // Тернарный оператор
            if (expr.Calls(CodeSymbols.QuestionMark) && expr.ArgCount == 2 && expr.Args[1].Calls(CodeSymbols.Colon, 2))
            {
                var cond = Eval(expr.Args[0], lookup);
                return cond != 0 ? Eval(expr.Args[1].Args[0], lookup) : Eval(expr.Args[1].Args[1], lookup);
            }

            throw new ArgumentException($"Expression not understood: {expr}");
        }


        static double Mod(double x, double y)
        {
            double m = x % y;
            return m + (m < 0 ? y : 0);
        }
        static double Factorial(double n) =>
        n <= 1 ? 1 : n * Factorial(n - 1);
        static double P(int n, int k) =>
        k <= 0 ? 1 : k > n ? 0 : n * P(n - 1, k - 1);
        static double C(ulong n, ulong k)
        {
            if (k > n)
                return 0;
            k = Math.Min(k, n - k);
            double result = 1;
            for (ulong d = 1; d <= k; ++d)
            {
                result *= n--;
                result /= d;
            }
            return result;
        }
        static Random _r = new Random();
    }

}
