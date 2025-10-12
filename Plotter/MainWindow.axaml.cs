using Avalonia.Controls;
using Avalonia.Threading;
using ScottPlot;
using ScottPlot.Avalonia;
using ScottPlot.Plottables; // for Signal
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Timers;

namespace Plotter
{
    public partial class MainWindow : Window
    {
        private readonly Stopwatch _stopwatch = new();
        private readonly Timer _timer = new(50) { AutoReset = true, Enabled = false };

        private static readonly int _pointsSize = 60000;
        private readonly List<double> _time = new();
        private static readonly uint _plotsAmount = 20;
        private readonly List<double>[] _ys = new List<double>[_plotsAmount];
        private readonly double _yOffsetScale = 5.0;
        private readonly double[] _yOffsets = new double[_plotsAmount];

        // NEW: hold references to the signals we add
        private readonly List<Signal> _signals = new();

        // NEW: toggle flag
        private bool _indexesEnabled = false;

        double[] vals = new double[_plotsAmount];
        public MainWindow()
        {
            InitializeComponent();

            _time.Capacity = _pointsSize;
            for (uint i = 0; i < _plotsAmount; ++i)
                _ys[i] = new List<double>(_pointsSize);

            _stopwatch.Start();

            AvaPlot1.Plot.Grid.IsVisible = false;
            AvaPlot1.Plot.Axes.Bottom.IsVisible = false;
            AvaPlot1.Plot.Axes.Left.IsVisible = false;
            AvaPlot1.Plot.Layout.Frameless();

            for (int i = 0; i < _plotsAmount; i++)
            {
                _yOffsets[i] = i * _yOffsetScale;

                // keep the returned Signal
                var sig = AvaPlot1.Plot.Add.Signal(_ys[i], color: ScottPlot.Color.FromHex(RandomHexRgb()));
                sig.MarkerSize = 0;
                sig.LineWidth = 2;
                sig.LinePattern = ScottPlot.LinePattern.Solid;

                _signals.Add(sig);
            }

            SetupTimer();
        }

        private void SetupTimer()
        {
            _timer.Elapsed += OnTick;
            _timer.Interval = 15;
            _timer.Start();
        }

        private void OnTick(object? s, ElapsedEventArgs e)
        {
            double t = _stopwatch.Elapsed.TotalSeconds;

            for (int i = 0; i < vals.Length; i++)
            {
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

        // NEW: button handler
        private void BtnToggleIndexes_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            _indexesEnabled = !_indexesEnabled;

            // choose a visible window size in points (e.g., last 2000 samples)
            const int windowPts = 2000;

            lock (AvaPlot1.Plot.Sync)
            {
                foreach (var (sig, i) in _signals.WithIndex())
                {
                    int count = _ys[i].Count;
                    if (count == 0)
                        continue;

                    if (_indexesEnabled)
                    {
                        // show only the newest "windowPts" samples
                        int min = Math.Max(0, count - windowPts);
                        int max = count - 1;

                        // Either of these patterns is fine in v5:
                        sig.MinRenderIndex = min;
                        sig.MaxRenderIndex = max;

                        // or: sig.Data.MinimumIndex = min; sig.Data.MaximumIndex = max;
                    }
                    else
                    {
                        // clear the restriction to show all samples
                        sig.MinRenderIndex = 0;
                        sig.MaxRenderIndex = int.MaxValue;

                        // If your ScottPlot build doesn't support nullables on these props,
                        // fallback to full range explicitly:
                        // sig.MinRenderIndex = 0;
                        // sig.MaxRenderIndex = count - 1;
                        // and keep these synchronized as data grows.
                    }
                }
            }

            AvaPlot1.Refresh();
            if (sender is Button b)
                b.Content = _indexesEnabled ? "Unset Render Indexes" : "Set Render Indexes";
        }
    }

    // small helper to iterate with index
    internal static class EnumerableExtensions
    {
        public static IEnumerable<(T item, int index)> WithIndex<T>(this IEnumerable<T> self)
        {
            int i = 0;
            foreach (var item in self) yield return (item, i++);
        }
    }
}
