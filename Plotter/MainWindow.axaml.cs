using Avalonia.Controls;
using Avalonia.Threading;
using ScottPlot;
using ScottPlot.Avalonia;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Timers;

namespace Plotter
{
    public partial class MainWindow : Window
    {
        private readonly Stopwatch _stopwatch = new Stopwatch();

        private readonly Timer _timer = new(50) { AutoReset = true, Enabled = false };

        private readonly List<double> _time = new List<double>();
        private static readonly uint _plotsAmount = 20;
        private readonly List<double>[] _ys = new List<double>[_plotsAmount];
        private readonly double _yOffsetScale = 5.0;
        private double[] _yOffsets = new double[_plotsAmount];

        public MainWindow()
        {
            InitializeComponent();

            _stopwatch.Start();

            AvaPlot1.Plot.Grid.IsVisible = false;
            AvaPlot1.Plot.Axes.Bottom.IsVisible = false;
            AvaPlot1.Plot.Axes.Left.IsVisible = false;

            AvaPlot1.Plot.Layout.Frameless();

            for (int i = 0; i < _plotsAmount; i++)
            {
                _ys[i] = new List<double>();
                _yOffsets[i] = i * _yOffsetScale;
                var s = AvaPlot1.Plot.Add.Scatter(_time, _ys[i], color: ScottPlot.Color.FromHex(RandomHexRgb()));
                s.MarkerSize = 0;
                s.LineWidth = 2;
                s.LinePattern = ScottPlot.LinePattern.Solid;
            }

            SetupTimer();
        }

        private void SetupTimer()
        {
            _timer.Elapsed += OnTick;
            _timer.Interval = 50;
            _timer.Start();
        }

        private void OnTick(object? s, ElapsedEventArgs e)
        {
            double t = _stopwatch.Elapsed.TotalSeconds;

            // generate 10 different signals
            double[] vals = new double[_plotsAmount];
            for (int i = 0; i < vals.Length; i++)
            {
                // vary frequency/phase/amplitude a bit per series
                double freq = 0.6 + i * 0.12;
                double amp = 20 + i * 3;
                double ph = i * 0.4;
                vals[i] = 50 + amp * Math.Sin(t * freq + ph);
            }

            lock (AvaPlot1.Plot.Sync)
            {
                _time.Add(t);
                for (int i = 0; i < _plotsAmount; i++)
                    _ys[i].Add(vals[i] + _yOffsets[i] * 15);
            }

            Dispatcher.UIThread.Post(() => AvaPlot1.Refresh());
        }
        public static string RandomHexRgb()
        {
            var rng = Random.Shared;
            int r = rng.Next(256), g = rng.Next(256), b = rng.Next(256);
            return $"#{r:X2}{g:X2}{b:X2}";
        }
    }
}