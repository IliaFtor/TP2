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
    class Calculator2D : CalculatorCore
    {
        static readonly Symbol sy_x = (Symbol)"x";
        public Calculator2D(LNode Expr, Dictionary<Symbol, LNode> Vars, CalcRange XRange)
             : base(Expr, Vars, XRange) { }
        public override CalculatorCore WithExpr(LNode newValue)
        {
            return new Calculator2D(newValue, Vars, XRange);
        }
        public override CalculatorCore WithVars(Dictionary<Symbol, LNode> newValue)
        {
            return new Calculator2D(Expr, newValue, XRange);
        }
        public override CalculatorCore WithXRange(CalcRange newValue)
        {
            return new Calculator2D(Expr, Vars, newValue);
        }
        public override object Run()
        {
            var results = new number[XRange.PxCount];
            number x = XRange.Lo;

            Func<Symbol, number> lookup = null;
            lookup = name => (name == sy_x ? x : Eval(Vars[name], lookup));

            for (int i = 0; i < results.Length; i++)
            {
                results[i] = Eval(Expr, lookup);
                x += XRange.StepSize;
            }
            return Results = results;
        }
        public override number? GetValueAt(int x, int _)
        {
            var tmp_14 = (uint)x;
            var r = ((number[])Results);
            return
            tmp_14 < (uint)r.Length ? r[x] : (number?)null;
        }
    }
}
