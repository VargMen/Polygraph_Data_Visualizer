using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkiaTestMacOS_MVVM.Models
{
    internal class Waveform
    {
        public List<Avalonia.Point> nominalPoints { get; set; } = new List<Avalonia.Point>(60000);
        public double scale { get; set; } = 1.0;
        public double verticalOffset { get; set; } = 0.0;

        public Avalonia.Point GetTransformedPoint(int index)
        {
            return new Avalonia.Point(nominalPoints[index].X, verticalOffset + scale * nominalPoints[index].Y);
        }
        struct Interval
        {
            public uint startIndex;
            public uint endIndex;
        }


    }
}
