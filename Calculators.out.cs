using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Loyc;
using Loyc.Syntax;
using Loyc.Collections;
namespace LesGraphingCalc
{
	using number = System.Double;	// Change this line to make a calculator for a different data type 
	class CalcRange {
		public number Lo;
		public number Hi;
		public int PxCount;
		// Generate a constructor and three public fields
		public CalcRange(number lo, number hi, int pxCount)
		{
			Lo = lo;
			Hi = hi;
			PxCount = pxCount;
			StepSize = (Hi - Lo) / Math.Max(PxCount - 1, 1);
		}
		public number StepSize;
		public number ValueToPx(number value) => (value - Lo) / (Hi - Lo) * PxCount;
		public number PxToValue(int px) => (number) px / PxCount * (Hi - Lo) + Lo;
		public number PxToDelta(int px) => (number) px / PxCount * (Hi - Lo);
		public CalcRange DraggedBy(int dPx) => 
		new CalcRange(Lo - PxToDelta(dPx), Hi - PxToDelta(dPx), PxCount);
		public CalcRange ZoomedBy(number ratio)
		{
			double mid = (Hi + Lo) / 2, halfSpan = (Hi - Lo) * ratio / 2;
			return new CalcRange(mid - halfSpan, mid + halfSpan, PxCount);
		}
	}
	abstract class CalculatorCore {
		static readonly Symbol sy_x = (Symbol) "x", sy_y = (Symbol) "y";
		// Base class constructor and fields
		public CalculatorCore(LNode Expr, Dictionary<Symbol, LNode> Vars, CalcRange XRange) {
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
		[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)] public LNode Item1 {
			get {
				return Expr;
			}
		}
		[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)] public Dictionary<Symbol, LNode> Item2 {
			get {
				return Vars;
			}
		}
		[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)] public CalcRange Item3 {
			get {
				return XRange;
			}
		}
		public object Results { get; protected set; }
	
		public abstract object Run();
		public abstract number? GetValueAt(int x, int y);
	
		public static CalculatorCore New(LNode expr, Dictionary<Symbol, LNode> vars, CalcRange xRange, CalcRange yRange)
		{

			bool isEquation = expr.Calls(CodeSymbols.Assign, 2) || expr.Calls(CodeSymbols.Eq, 2), usesY = false;
			if (!isEquation) {
				LNode zero = LNode.Literal((double) 0);
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
			foreach (LNode assignment in varList) {
				{
					LNode expr, @var;
					if (assignment.Calls(CodeSymbols.Assign, 2) && (@var = assignment.Args[0]) != null && (expr = assignment.Args[1]) != null) {
						if (!@var.IsId)
							throw new ArgumentException("Left-hand side of '=' must be a variable name: {0}".Localized(@var));
					
						try { expr = LNode.Literal(Eval(expr, vars)); } catch { }	
						vars.Add(@var.Name, expr);
					} else
						throw new ArgumentException("Expected assignment expression: {0}".Localized(assignment));
				} ;
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
		static double C(ulong n, ulong k) {
			if (k > n)
				return 0;
			k = Math.Min(k, n - k);
			double result = 1;
			for (ulong d = 1; d <= k; ++d) {
				result *= n--;
				result /= d;
			}
			return result;
		}
		static Random _r = new Random();
	}
	class Calculator2D : CalculatorCore {
		static readonly Symbol sy_x = (Symbol) "x";
		public Calculator2D(LNode Expr, Dictionary<Symbol, LNode> Vars, CalcRange XRange)
			 : base(Expr, Vars, XRange) { }
		public override CalculatorCore WithExpr(LNode newValue) {
			return new Calculator2D(newValue, Vars, XRange);
		}
		public override CalculatorCore WithVars(Dictionary<Symbol, LNode> newValue) {
			return new Calculator2D(Expr, newValue, XRange);
		}
		public override CalculatorCore WithXRange(CalcRange newValue) {
			return new Calculator2D(Expr, Vars, newValue);
		}
		public override object Run()
		{
			var results = new number[XRange.PxCount];
			number x = XRange.Lo;
		
			Func<Symbol, number> lookup = null;
			lookup = name => (name == sy_x ? x : Eval(Vars[name], lookup));
		
			for (int i = 0; i < results.Length; i++) {
				results[i] = Eval(Expr, lookup);
				x += XRange.StepSize;
			}
			return Results = results;
		}
		public override number? GetValueAt(int x, int _) {
			var tmp_14 = (uint) x;
			var r = ((number[]) Results);
			return 
			tmp_14 < (uint) r.Length ? r[x] : (number?) null;
		}
	}

	class Calculator3D : CalculatorCore {
		static readonly Symbol sy_x = (Symbol) "x", sy_y = (Symbol) "y";
		public Calculator3D(LNode Expr, Dictionary<Symbol, LNode> Vars, CalcRange XRange, CalcRange YRange)
			 : base(Expr, Vars, XRange) {
			this.YRange = YRange;
		}
		public CalcRange YRange { get; private set; }
		public override CalculatorCore WithExpr(LNode newValue) {
			return new Calculator3D(newValue, Vars, XRange, YRange);
		}
		public override CalculatorCore WithVars(Dictionary<Symbol, LNode> newValue) {
			return new Calculator3D(Expr, newValue, XRange, YRange);
		}
		public override CalculatorCore WithXRange(CalcRange newValue) {
			return new Calculator3D(Expr, Vars, newValue, YRange);
		}
		public Calculator3D WithYRange(CalcRange newValue) {
			return new Calculator3D(Expr, Vars, XRange, newValue);
		}
		[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)] public CalcRange Item4 {
			get {
				return YRange;
			}
		}
		public bool EquationMode { get; private set; }
	
		public override object Run()
		{
			{
				var Expr_13 = Expr;
				LNode L, R;
				if (Expr_13.Calls(CodeSymbols.Assign, 2) && (L = Expr_13.Args[0]) != null && (R = Expr_13.Args[1]) != null || Expr_13.Calls(CodeSymbols.Eq, 2) && (L = Expr_13.Args[0]) != null && (R = Expr_13.Args[1]) != null) {
					EquationMode = true;
					number[,] results = RunCore(LNode.Call(CodeSymbols.Sub, LNode.List(L, R)).SetStyle(NodeStyle.Operator), true);
					number[,] results2 = new number[results.GetLength(0) - 1, results.GetLength(1) - 1];
					for (int i = 0; i < results.GetLength(0) - 1; i++) {
						for (int j = 0; j < results.GetLength(1) - 1; j++) {
							int sign = Math.Sign(results[i, j]);
							if (sign == 0 || sign != Math.Sign(results[i + 1, j]) || 
							sign != Math.Sign(results[i, j + 1]) || 
							sign != Math.Sign(results[i + 1, j + 1]))
								results2[i, j] = (number) 1;
							else
								results2[i, j] = (number) 0;
						}
					}
					return Results = results2;
				} else {
					EquationMode = Expr.ArgCount == 2 && Expr.Name.IsOneOf(
					CodeSymbols.GT, CodeSymbols.LT, CodeSymbols.GE, CodeSymbols.LE, CodeSymbols.Neq, CodeSymbols.And, CodeSymbols.Or);
					return Results = RunCore(Expr, false);
				}
			}
		}
		public number[,] RunCore(LNode expr, bool difMode)
		{
			var results = new number
			[YRange.PxCount + (difMode ? 1 : 0), XRange.PxCount + (difMode ? 1 : 0)];
			number x = XRange.Lo, startx = x;
			number y = YRange.Lo;
			if (difMode) {
				x -= XRange.StepSize / 2;
				y -= YRange.StepSize / 2;
			}
		
			Func<Symbol, number> lookup = null;
			try
			{
				lookup = name => (name == sy_x ? x : name == sy_y ? y : Eval(Vars[name], lookup));
			}
			catch {
                throw new FormatException("The function must have sensitive case variables{ }");
            }

            for (int yi = 0; yi < results.GetLength(0); yi++, x = startx) {
				for (int xi = 0; xi < results.GetLength(1); xi++) {
					results[yi, xi] = Eval(expr, lookup);
					x += XRange.StepSize;
				}
				y += YRange.StepSize;
			}
			return results;
		}
		public override number? GetValueAt(int x, int y) {
			var tmp_15 = (uint) x;
			var r = ((number[,]) Results);
			return 
			tmp_15 < (uint) r.GetLength(1) && 
			(uint) y < (uint) r.GetLength(0) ? r[y, x] : (number?) null;
		}
	}

}