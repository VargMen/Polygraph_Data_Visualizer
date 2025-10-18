using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using SkiaTestMacOS_MVVM.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Timers;

namespace SkiaTestMacOS_MVVM.Controls
{
    public class MyCanvas : Control
    {
        private Point _panOffset = new Point(0, 0);   
        private Point _lastMouse;                     
        private bool _isPanning;

        private const double _waveformsAmount = 2;
        private readonly List<Waveform> _waveforms = new List<Waveform>();

        private const double _amplitudeStep = 0.1;
        private const double _yPositionStep = 10;

        private readonly Timer _timer = new(50) { AutoReset = true, Enabled = false };
        private readonly Stopwatch _stopwatch = new Stopwatch();

        private bool _draw = false;

        public MyCanvas()
        {
            PointerPressed += OnPointerPressed;
            PointerReleased += OnPointerReleased;
            PointerMoved += OnPointerMoved;
            Focusable = true;
            KeyDown += OnKeyDown;

            _timer.Elapsed += OnTick;
            _timer.Interval = 50;

            for (int i = 0; i < _waveformsAmount; ++i)
            {
                _waveforms.Add(new Waveform());
            }
        }

        private void OnTick(object? s, ElapsedEventArgs e)
        {
             double t = _stopwatch.Elapsed.TotalSeconds;

            System.Diagnostics.Debug.WriteLine(t);
            _waveforms[0].nominalPoints.Add(SinusWavePoint(t, 0, 200, 15, 10, 100));
            _waveforms[1].nominalPoints.Add(SinusWavePoint(t, 0, 100, 5, 10, 100));

            Dispatcher.UIThread.Post(() => InvalidateVisual());
        }
        public static string RandomHexRgb()
        {
            var rng = Random.Shared;
            int r = rng.Next(256), g = rng.Next(256), b = rng.Next(256);
            return $"#{r:X2}{g:X2}{b:X2}";
        }

        private void OnKeyDown(object? sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.W:
                    _waveforms[0].scale += _amplitudeStep;
                    break;
                case Key.S:
                    _waveforms[0].scale -= _amplitudeStep;
                    break;

                case Key.E:
                    _waveforms[0].verticalOffset += _yPositionStep;
                    break;
                case Key.D:
                    _waveforms[0].verticalOffset -= _yPositionStep;
                    break;

                case Key.T:
                    _waveforms[1].scale += _amplitudeStep;
                    break;
                case Key.G:
                    _waveforms[1].scale -= _amplitudeStep;
                    break;

                case Key.Y:
                    _waveforms[1].verticalOffset += _yPositionStep;
                    break;
                case Key.H:
                    _waveforms[1].verticalOffset -= _yPositionStep;
                    break;

                case Key.Space:
                    if (!_draw)
                    {
                        _timer.Start();
                        _stopwatch.Start();
                    }
                    else
                    {
                        _timer.Stop();
                        _stopwatch.Stop();
                    }

                    _draw = !_draw;
                    break;
            }

            InvalidateVisual();
        }

        private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            _isPanning = true;
            _lastMouse = e.GetPosition(this);
            e.Pointer.Capture(this);
        }

        private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
        {
            _isPanning = false;
            e.Pointer.Capture(null);
        }

        private void OnPointerMoved(object? sender, PointerEventArgs e)
        {
            if (!_isPanning) return;

            var pos = e.GetPosition(this);
            var dx = pos.X - _lastMouse.X;
            var dy = pos.Y - _lastMouse.Y;

            _panOffset = new Point(_panOffset.X + dx, _panOffset.Y + dy);
            _lastMouse = pos;

            InvalidateVisual();
        }

        public override void Render(DrawingContext context)
        {
            base.Render(context);

            context.FillRectangle(Brushes.Black, Bounds);

            context.PushTransform(Matrix.CreateTranslation(_panOffset));

            var pen = new Pen(Brushes.White, 2);

            double minVisibleX = -_panOffset.X;
            double maxVisibleX = -_panOffset.X + Bounds.Width;

            context.DrawLine(pen, new Point(0, 0), new Point(100, 100));

            var geo = new StreamGeometry();
            using (var g = geo.Open())
            {
                foreach (Waveform waveform in _waveforms)
                {
                    int start = LowerBound(waveform.nominalPoints, minVisibleX);
                    int end = UpperBound(waveform.nominalPoints, maxVisibleX);

                    if (start > end || start >= waveform.nominalPoints.Count || end < 0)
                        continue;

                    start = Math.Max(0, start - 1);
                    end = Math.Min(waveform.nominalPoints.Count - 1, end + 1);

                    g.BeginFigure(waveform.GetTransformedPoint(start), false);
                    for (int j = start + 1; j <= end; j++)
                    {
                        g.LineTo(waveform.GetTransformedPoint(j));
                    }
                    g.EndFigure(false);
                }
            }

            context.DrawGeometry(null, pen, geo);
        }

        private static int LowerBound(List<Avalonia.Point> pts, double x)
        {
            int lo = 0, hi = pts.Count;
            while (lo < hi)
            {
                int mid = (lo + hi) >> 1;
                if (pts[mid].X < x) lo = mid + 1;
                else hi = mid;
            }
            return lo;
        }

        // Binary search: last index with points[i].X <= x
        private static int UpperBound(List<Avalonia.Point> pts, double x)
        {
            int lo = 0, hi = pts.Count; // [lo, hi)
            while (lo < hi)
            {
                int mid = (lo + hi) >> 1;
                if (pts[mid].X <= x) lo = mid + 1;
                else hi = mid;
            }
            return lo - 1;
        }

        private Avalonia.Point SinusWavePoint(double t, double y0 = 0, double a = 1, double w = 1, double p = 0, double tStep = 1)
        {
            double x = t * tStep;
            double y = y0 + a * Math.Sin(w * x + p);

            return new Point(x, y);
        }
        private Avalonia.Points GenerateSine(double y0 = 0, double a = 1, double w = 1, double p = 0, double tStep = 1)
        {
            Avalonia.Points points = new Avalonia.Points();
            for (int i = 0; i < 10000; i++)
            {
                double x = i * tStep;                 
                double y = y0 + a * Math.Sin(w * x + p);
                points.Add(new Point(x, y));
            }
            return points;
        }
    }
}
