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
    class Calculator3D : CalculatorCore
    {
        static readonly Symbol sy_x = (Symbol)"x", sy_y = (Symbol)"y";
        public Calculator3D(LNode Expr, Dictionary<Symbol, LNode> Vars, CalcRange XRange, CalcRange YRange)
             : base(Expr, Vars, XRange)
        {
            this.YRange = YRange;
        }
        public CalcRange YRange { get; private set; }
        public override CalculatorCore WithExpr(LNode newValue)
        {
            return new Calculator3D(newValue, Vars, XRange, YRange);
        }
        public override CalculatorCore WithVars(Dictionary<Symbol, LNode> newValue)
        {
            return new Calculator3D(Expr, newValue, XRange, YRange);
        }
        public override CalculatorCore WithXRange(CalcRange newValue)
        {
            return new Calculator3D(Expr, Vars, newValue, YRange);
        }
        public Calculator3D WithYRange(CalcRange newValue)
        {
            return new Calculator3D(Expr, Vars, XRange, newValue);
        }
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public CalcRange Item4
        {
            get
            {
                return YRange;
            }
        }
        public bool EquationMode { get; private set; }

        public override object Run()
        {
            {
                var Expr_13 = Expr;
                LNode L, R;
                if (Expr_13.Calls(CodeSymbols.Assign, 2) && (L = Expr_13.Args[0]) != null && (R = Expr_13.Args[1]) != null || Expr_13.Calls(CodeSymbols.Eq, 2) && (L = Expr_13.Args[0]) != null && (R = Expr_13.Args[1]) != null)
                {
                    EquationMode = true;
                    number[,] results = RunCore(LNode.Call(CodeSymbols.Sub, LNode.List(L, R)).SetStyle(NodeStyle.Operator), true);
                    number[,] results2 = new number[results.GetLength(0) - 1, results.GetLength(1) - 1];
                    for (int i = 0; i < results.GetLength(0) - 1; i++)
                    {
                        for (int j = 0; j < results.GetLength(1) - 1; j++)
                        {
                            int sign = Math.Sign(results[i, j]);
                            if (sign == 0 || sign != Math.Sign(results[i + 1, j]) ||
                            sign != Math.Sign(results[i, j + 1]) ||
                            sign != Math.Sign(results[i + 1, j + 1]))
                                results2[i, j] = (number)1;
                            else
                                results2[i, j] = (number)0;
                        }
                    }
                    return Results = results2;
                }
                else
                {
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
            if (difMode)
            {
                x -= XRange.StepSize / 2;
                y -= YRange.StepSize / 2;
            }

            Func<Symbol, number> lookup = null;
            try
            {
                lookup = name => (name == sy_x ? x : name == sy_y ? y : Eval(Vars[name], lookup));
            }
            catch
            {
                throw new FormatException("The function must have sensitive case variables{ }");
            }

            for (int yi = 0; yi < results.GetLength(0); yi++, x = startx)
            {
                for (int xi = 0; xi < results.GetLength(1); xi++)
                {
                    results[yi, xi] = Eval(expr, lookup);
                    x += XRange.StepSize;
                }
                y += YRange.StepSize;
            }
            return results;
        }
        public override number? GetValueAt(int x, int y)
        {
            var tmp_15 = (uint)x;
            var r = ((number[,])Results);
            return
            tmp_15 < (uint)r.GetLength(1) &&
            (uint)y < (uint)r.GetLength(0) ? r[y, x] : (number?)null;
        }
    }
}
