using Avalonia.Controls;
using Avalonia.Threading;
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
        private readonly List<double> _valueYs = new List<double>();

        public MainWindow()
        {
            InitializeComponent();

            _stopwatch.Start();

            AvaPlot1.Plot.Grid.IsVisible = false;
            AvaPlot1.Plot.Axes.Bottom.IsVisible = false;
            AvaPlot1.Plot.Axes.Left.IsVisible = false;

            AvaPlot1.Plot.Layout.Frameless();


            AvaPlot1.Plot.Add.Scatter(_time, _valueYs);

            SetupTimer();
        }

        private void SetupTimer()
        {
            _timer.Elapsed += OnTick;
            _timer.Start();
        }

        private void OnTick(object? s, ElapsedEventArgs e)
        {
            double t = _stopwatch.Elapsed.TotalSeconds;
            double y = GetCurrentValue(t);

            lock (AvaPlot1.Plot.Sync)
            {
                _time.Add(t);
                _valueYs.Add(y);
            }

            Dispatcher.UIThread.Post(() => AvaPlot1.Refresh());
        }
        private double GetCurrentValue(double time)
        {
            return 50 + 50 * Math.Sin(time / 5.0) + (time * 0.5);
        }
    }
}