using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Loyc;
using Loyc.Syntax;
using Loyc.Collections;
using LesGraphingCalc.CalculeteModel;

namespace LesGraphingCalc.CalculeteModel
{
    using number = System.Double;   // Change this line to make a calculator for a different data type 
    class CalcRange
    {
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
        public number PxToValue(int px) => (number)px / PxCount * (Hi - Lo) + Lo;
        public number PxToDelta(int px) => (number)px / PxCount * (Hi - Lo);
        public CalcRange DraggedBy(int dPx) =>
        new CalcRange(Lo - PxToDelta(dPx), Hi - PxToDelta(dPx), PxCount);
        public CalcRange ZoomedBy(number ratio)
        {
            double mid = (Hi + Lo) / 2, halfSpan = (Hi - Lo) * ratio / 2;
            return new CalcRange(mid - halfSpan, mid + halfSpan, PxCount);
        }
    }
}
