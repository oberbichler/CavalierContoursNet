using System;
using System.Collections.Generic;
using System.Numerics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using CavalierContours.Core;
using CavalierContours.Polyline;

namespace CavalierContours.Example
{
    public class PolylineCanvas : Control
    {
        public Polyline<double> PolylineA { get; } = new Polyline<double>(true);
        public Polyline<double> PolylineB { get; } = new Polyline<double>(true);

        private int _activePolylineIndex = 0;
        public int ActivePolylineIndex
        {
            get => _activePolylineIndex;
            set
            {
                if (_activePolylineIndex != value)
                {
                    _activePolylineIndex = value;
                    SelectedVertexIndex = -1;
                    InvalidateVisual();
                    SelectedVertexChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        public Polyline<double> ActivePolyline => ActivePolylineIndex == 0 ? PolylineA : PolylineB;

        public int SelectedVertexIndex { get; set; } = -1;
        private int _draggedVertexIndex = -1;

        public double Zoom { get; set; } = 1.0;
        public Point Pan { get; set; } = new Point(0, 0);

        private bool _isPanning = false;
        private Point _lastPanPointerPos = new Point(0, 0);

        public Point WorldToScreen(double x, double y)
        {
            return new Point(x * Zoom + Pan.X, y * Zoom + Pan.Y);
        }

        public Point ScreenToWorld(double x, double y)
        {
            return new Point((x - Pan.X) / Zoom, (y - Pan.Y) / Zoom);
        }

        public void ResetView()
        {
            Zoom = 1.0;
            Pan = new Point(0, 0);
            InvalidateVisual();
        }

        public double LastOffsetDistance { get; set; } = 20.0;
        public bool LastHandleSelfIntersects { get; set; } = true;
        public int OffsetRingsCount { get; set; } = 1;

        public List<Polyline<double>>? OffsetResult { get; set; }
        public BooleanResult<Polyline<double>, double>? BooleanResult { get; set; }

        public event EventHandler? SelectedVertexChanged;

        public PolylineCanvas()
        {
            ClipToBounds = true;

            // Initialize Polyline A as a star/nice shape with a curved top/sides
            PolylineA.Add(150, 150, 0.41421356); // Curve 1
            PolylineA.Add(350, 150, 0.0);
            PolylineA.Add(350, 350, 0.41421356); // Curve 2
            PolylineA.Add(150, 350, 0.0);
            PolylineA.SetIsClosed(true);

            // Initialize Polyline B as an overlapping shape
            PolylineB.Add(250, 250, 0.0);
            PolylineB.Add(450, 250, -0.41421356); // Inverse curve
            PolylineB.Add(450, 450, 0.0);
            PolylineB.Add(250, 450, 0.0);
            PolylineB.SetIsClosed(true);
        }

        public void ClearActive()
        {
            ActivePolyline.Clear();
            SelectedVertexIndex = -1;
            _draggedVertexIndex = -1;
            OffsetResult = null;
            BooleanResult = null;
            InvalidateVisual();
            SelectedVertexChanged?.Invoke(this, EventArgs.Empty);
        }

        public void LoadSampleShape(string shapeName)
        {
            var pline = ActivePolyline;
            pline.Clear();
            SelectedVertexIndex = -1;
            _draggedVertexIndex = -1;

            // Center around (300, 300), size ~200
            switch (shapeName)
            {
                case "Rectangle":
                    pline.Add(200, 200, 0.0);
                    pline.Add(400, 200, 0.0);
                    pline.Add(400, 400, 0.0);
                    pline.Add(200, 400, 0.0);
                    break;

                case "Rounded Rectangle":
                    pline.Add(200, 200, 0.414);
                    pline.Add(400, 200, 0.414);
                    pline.Add(400, 400, 0.414);
                    pline.Add(200, 400, 0.414);
                    break;

                case "Circle":
                    pline.Add(200, 300, 1.0);
                    pline.Add(400, 300, 1.0);
                    break;

                case "Star":
                    double cx = 300, cy = 300, outerR = 150, innerR = 60;
                    for (int i = 0; i < 5; i++)
                    {
                        double outerAngle = -Math.PI / 2 + i * 2 * Math.PI / 5;
                        double innerAngle = outerAngle + Math.PI / 5;
                        pline.Add(cx + outerR * Math.Cos(outerAngle), cy + outerR * Math.Sin(outerAngle), 0.0);
                        pline.Add(cx + innerR * Math.Cos(innerAngle), cy + innerR * Math.Sin(innerAngle), 0.0);
                    }
                    break;

                case "Hourglass":
                    pline.Add(150, 150, 0.0);
                    pline.Add(300, 260, 0.0);
                    pline.Add(450, 150, 0.0);
                    pline.Add(450, 450, 0.0);
                    pline.Add(300, 340, 0.0);
                    pline.Add(150, 450, 0.0);
                    break;

                case "L-Shape":
                    pline.Add(200, 150, 0.0);
                    pline.Add(350, 150, 0.0);
                    pline.Add(350, 300, 0.0);
                    pline.Add(450, 300, 0.0);
                    pline.Add(450, 450, 0.0);
                    pline.Add(200, 450, 0.0);
                    break;

                case "Arrow":
                    pline.Add(200, 250, 0.0);
                    pline.Add(350, 250, 0.0);
                    pline.Add(350, 180, 0.0);
                    pline.Add(450, 300, 0.0);
                    pline.Add(350, 420, 0.0);
                    pline.Add(350, 350, 0.0);
                    pline.Add(200, 350, 0.0);
                    break;

                case "Gear (8 teeth)":
                    double gcx = 300, gcy = 300, gr1 = 100, gr2 = 140;
                    int teeth = 8;
                    for (int i = 0; i < teeth; i++)
                    {
                        double a0 = i * 2 * Math.PI / teeth;
                        double a1 = a0 + 0.3 * 2 * Math.PI / teeth;
                        double a2 = a0 + 0.5 * 2 * Math.PI / teeth;
                        double a3 = a0 + 0.8 * 2 * Math.PI / teeth;
                        pline.Add(gcx + gr1 * Math.Cos(a0), gcy + gr1 * Math.Sin(a0), 0.0);
                        pline.Add(gcx + gr2 * Math.Cos(a1), gcy + gr2 * Math.Sin(a1), 0.0);
                        pline.Add(gcx + gr2 * Math.Cos(a2), gcy + gr2 * Math.Sin(a2), 0.0);
                        pline.Add(gcx + gr1 * Math.Cos(a3), gcy + gr1 * Math.Sin(a3), 0.0);
                    }
                    break;

                default:
                    return;
            }

            pline.SetIsClosed(true);
            OnGeometryChanged();
            InvalidateVisual();
            SelectedVertexChanged?.Invoke(this, EventArgs.Empty);
        }

        public void ClearResults()
        {
            OffsetResult = null;
            BooleanResult = null;
            InvalidateVisual();
        }

        public void DeleteSelectedVertex()
        {
            if (SelectedVertexIndex >= 0 && SelectedVertexIndex < ActivePolyline.VertexCount)
            {
                ActivePolyline.RemoveAt(SelectedVertexIndex);
                SelectedVertexIndex = -1;
                _draggedVertexIndex = -1;
                OnGeometryChanged();
                InvalidateVisual();
                SelectedVertexChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public void UpdateSelectedVertex(double x, double y, double bulge)
        {
            if (SelectedVertexIndex >= 0 && SelectedVertexIndex < ActivePolyline.VertexCount)
            {
                ActivePolyline.SetVertex(SelectedVertexIndex, new PlineVertex<double>(x, y, bulge));
                OnGeometryChanged();
                InvalidateVisual();
                SelectedVertexChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public void ComputeOffset(double distance, bool handleSelfIntersects, int ringsCount = 1)
        {
            LastOffsetDistance = distance;
            LastHandleSelfIntersects = handleSelfIntersects;
            OffsetRingsCount = ringsCount;

            var options = new PlineOffsetOptions<double>
            {
                HandleSelfIntersects = handleSelfIntersects,
                PosEqualEps = 1e-5,
                SliceJoinEps = 1e-4,
                OffsetDistEps = 1e-4
            };

            var currentPline = ActivePolyline;
            try
            {
                var allOffsets = new List<Polyline<double>>();
                for (int i = 1; i <= ringsCount; i++)
                {
                    double currentDistance = distance * i;
                    var result = PlineOffset.ParallelOffset<Polyline<double>, double>(currentPline, currentDistance, options);
                    allOffsets.AddRange(result);
                }
                OffsetResult = allOffsets;
                BooleanResult = null; // Clear boolean results when running offset
                InvalidateVisual();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Offset calculation failed: {ex.Message}");
            }
        }

        public void ComputeBoolean(BooleanOp op)
        {
            var options = new PlineBooleanOptions<double>
            {
                PosEqualEps = 1e-5
            };

            try
            {
                BooleanResult = PlineBoolean.PolylineBoolean<Polyline<double>, double>(PolylineA, PolylineB, op, options);
                OffsetResult = null; // Clear offset results when running boolean
                InvalidateVisual();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Boolean calculation failed: {ex.Message}");
            }
        }

        private void OnGeometryChanged()
        {
            if (OffsetResult != null)
            {
                ComputeOffset(LastOffsetDistance, LastHandleSelfIntersects, OffsetRingsCount);
            }
            else
            {
                OffsetResult = null;
            }
            BooleanResult = null;
        }

        protected override void OnPointerPressed(PointerPressedEventArgs e)
        {
            base.OnPointerPressed(e);

            var point = e.GetCurrentPoint(this);
            var pos = point.Position;

            if (point.Properties.IsLeftButtonPressed)
            {
                // Check if we clicked on an existing vertex of the active polyline
                int hitIndex = -1;
                double minDistance = 12.0; // Click tolerance in pixels

                for (int i = 0; i < ActivePolyline.VertexCount; i++)
                {
                    var v = ActivePolyline.Get(i);
                    var screenV = WorldToScreen(v.X, v.Y);
                    double dx = screenV.X - pos.X;
                    double dy = screenV.Y - pos.Y;
                    double dist = Math.Sqrt(dx * dx + dy * dy);
                    if (dist < minDistance)
                    {
                        hitIndex = i;
                        minDistance = dist;
                    }
                }

                if (hitIndex >= 0)
                {
                    SelectedVertexIndex = hitIndex;
                    _draggedVertexIndex = hitIndex;
                    SelectedVertexChanged?.Invoke(this, EventArgs.Empty);
                    e.Pointer.Capture(this);
                    InvalidateVisual();
                }
                else
                {
                    // Clicked on empty space: Add a vertex in world coordinates
                    var worldPos = ScreenToWorld(pos.X, pos.Y);
                    ActivePolyline.Add(worldPos.X, worldPos.Y, 0.0);
                    SelectedVertexIndex = ActivePolyline.VertexCount - 1;
                    _draggedVertexIndex = SelectedVertexIndex;
                    OnGeometryChanged();
                    SelectedVertexChanged?.Invoke(this, EventArgs.Empty);
                    e.Pointer.Capture(this);
                    InvalidateVisual();
                }
            }
            else if (point.Properties.IsRightButtonPressed || point.Properties.IsMiddleButtonPressed)
            {
                // Start panning
                _isPanning = true;
                _lastPanPointerPos = pos;
                e.Pointer.Capture(this);
            }
        }

        protected override void OnPointerMoved(PointerEventArgs e)
        {
            base.OnPointerMoved(e);
            var pos = e.GetPosition(this);

            if (_draggedVertexIndex >= 0 && _draggedVertexIndex < ActivePolyline.VertexCount)
            {
                var worldPos = ScreenToWorld(pos.X, pos.Y);
                var currentVertex = ActivePolyline.Get(_draggedVertexIndex);
                ActivePolyline.SetVertex(_draggedVertexIndex, new PlineVertex<double>(worldPos.X, worldPos.Y, currentVertex.Bulge));
                OnGeometryChanged();
                InvalidateVisual();
                SelectedVertexChanged?.Invoke(this, EventArgs.Empty);
            }
            else if (_isPanning)
            {
                double dx = pos.X - _lastPanPointerPos.X;
                double dy = pos.Y - _lastPanPointerPos.Y;
                Pan = new Point(Pan.X + dx, Pan.Y + dy);
                _lastPanPointerPos = pos;
                InvalidateVisual();
            }
        }

        protected override void OnPointerReleased(PointerReleasedEventArgs e)
        {
            base.OnPointerReleased(e);
            if (_draggedVertexIndex >= 0)
            {
                _draggedVertexIndex = -1;
                e.Pointer.Capture(null);
                InvalidateVisual();
            }
            else if (_isPanning)
            {
                _isPanning = false;
                e.Pointer.Capture(null);
                InvalidateVisual();
            }
        }

        protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
        {
            base.OnPointerWheelChanged(e);

            var mousePos = e.GetPosition(this);
            double oldZoom = Zoom;
            double factor = e.Delta.Y > 0 ? 1.15 : 1.0 / 1.15;
            Zoom = Math.Clamp(Zoom * factor, 0.05, 100.0);

            // Zoom towards mouse position
            Pan = new Point(
                mousePos.X - (mousePos.X - Pan.X) * (Zoom / oldZoom),
                mousePos.Y - (mousePos.Y - Pan.Y) * (Zoom / oldZoom)
            );

            InvalidateVisual();
        }

        public override void Render(DrawingContext context)
        {
            // Draw background
            context.FillRectangle(
                new SolidColorBrush(Color.Parse("#F9FAF9")),
                new Rect(0, 0, Bounds.Width, Bounds.Height)
            );

            // Determine dynamic grid spacing
            double gridSpacing = 50.0;
            while (gridSpacing * Zoom < 20.0) gridSpacing *= 2.0;
            while (gridSpacing * Zoom > 100.0) gridSpacing /= 2.0;

            double scaledSpacing = gridSpacing * Zoom;
            double startX = Pan.X % scaledSpacing;
            if (startX < 0) startX += scaledSpacing;
            double startY = Pan.Y % scaledSpacing;
            if (startY < 0) startY += scaledSpacing;

            var gridPen = new Pen(new SolidColorBrush(Color.Parse("#EAEAEA")), 1.0);
            var axisPen = new Pen(new SolidColorBrush(Color.Parse("#CFD8DC")), 1.5);

            for (double x = startX; x < Bounds.Width; x += scaledSpacing)
            {
                context.DrawLine(gridPen, new Point(x, 0), new Point(x, Bounds.Height));
            }
            for (double y = startY; y < Bounds.Height; y += scaledSpacing)
            {
                context.DrawLine(gridPen, new Point(0, y), new Point(Bounds.Width, y));
            }

            // Draw X and Y main axes (World coordinate X=0 and Y=0) if visible
            double axisX = WorldToScreen(0, 0).X;
            double axisY = WorldToScreen(0, 0).Y;

            if (axisX >= 0 && axisX <= Bounds.Width)
            {
                context.DrawLine(axisPen, new Point(axisX, 0), new Point(axisX, Bounds.Height));
            }
            if (axisY >= 0 && axisY <= Bounds.Height)
            {
                context.DrawLine(axisPen, new Point(0, axisY), new Point(Bounds.Width, axisY));
            }

            // Draw Polyline A
            var geomA = CreateGeometryFromPolyline(PolylineA);
            if (geomA != null)
            {
                var fillBrush = new SolidColorBrush(Color.Parse("#1A2196F3")); // 10% opacity blue
                var borderPen = new Pen(new SolidColorBrush(Color.Parse("#2196F3")), ActivePolylineIndex == 0 ? 3.0 : 1.5);
                context.DrawGeometry(PolylineA.IsClosed ? fillBrush : null, borderPen, geomA);
            }

            // Draw Polyline B
            var geomB = CreateGeometryFromPolyline(PolylineB);
            if (geomB != null)
            {
                var fillBrush = new SolidColorBrush(Color.Parse("#1AFFE0B2")); // 10% opacity orange-gold
                var borderPen = new Pen(new SolidColorBrush(Color.Parse("#FF9800")), ActivePolylineIndex == 1 ? 3.0 : 1.5);
                context.DrawGeometry(PolylineB.IsClosed ? fillBrush : null, borderPen, geomB);
            }

            // Draw Parallel Offset Polylines (if any)
            if (OffsetResult != null)
            {
                var offsetPen = new Pen(new SolidColorBrush(Color.Parse("#4CAF50")), 2.0, new DashStyle(new double[] { 4, 4 }, 0));
                var offsetFillBrush = new SolidColorBrush(Color.Parse("#124CAF50"));
                foreach (var pline in OffsetResult)
                {
                    var geom = CreateGeometryFromPolyline(pline);
                    if (geom != null)
                    {
                        context.DrawGeometry(pline.IsClosed ? offsetFillBrush : null, offsetPen, geom);
                    }
                }
            }

            // Draw Boolean Result Polylines (if any)
            if (BooleanResult != null)
            {
                var boolFillBrush = new SolidColorBrush(Color.Parse("#339C27B0")); // purple transparent fill
                var boolPosPen = new Pen(new SolidColorBrush(Color.Parse("#9C27B0")), 3.0);
                var boolNegPen = new Pen(new SolidColorBrush(Color.Parse("#E91E63")), 2.0, new DashStyle(new double[] { 2, 2 }, 0));

                foreach (var p in BooleanResult.PosPlines)
                {
                    var geom = CreateGeometryFromPolyline(p.Pline);
                    if (geom != null)
                    {
                        context.DrawGeometry(boolFillBrush, boolPosPen, geom);
                    }
                }

                foreach (var p in BooleanResult.NegPlines)
                {
                    var geom = CreateGeometryFromPolyline(p.Pline);
                    if (geom != null)
                    {
                        context.DrawGeometry(null, boolNegPen, geom);
                    }
                }
            }

            // Draw Vertices of Active Polyline
            var activePline = ActivePolyline;
            var vertexBrush = new SolidColorBrush(Color.Parse("#2196F3"));
            if (ActivePolylineIndex == 1)
            {
                vertexBrush = new SolidColorBrush(Color.Parse("#FF9800"));
            }

            for (int i = 0; i < activePline.VertexCount; i++)
            {
                var v = activePline.Get(i);
                var center = WorldToScreen(v.X, v.Y);

                if (i == SelectedVertexIndex)
                {
                    // Draw highlight ring for selected vertex
                    var ringPen = new Pen(new SolidColorBrush(Color.Parse("#F44336")), 2.0);
                    context.DrawEllipse(null, ringPen, center, 8, 8);
                }

                context.DrawEllipse(vertexBrush, null, center, 5, 5);

                // Draw arc indicator text if bulge is non-zero
                if (!v.BulgeIsZero())
                {
                    var labelBrush = new SolidColorBrush(Color.Parse("#F44336"));
                    var ft = new FormattedText(
                        $"b={v.Bulge:F2}",
                        System.Globalization.CultureInfo.CurrentCulture,
                        FlowDirection.LeftToRight,
                        Typeface.Default,
                        10,
                        labelBrush
                    );
                    context.DrawText(ft, new Point(center.X + 8, center.Y - 14));
                }
            }
        }

        private Geometry? CreateGeometryFromPolyline(IPlineSource<double> pline)
        {
            if (pline.VertexCount < 2) return null;

            var geometry = new StreamGeometry();
            using (var context = geometry.Open())
            {
                var first = pline.Get(0);
                var firstScreen = WorldToScreen(first.X, first.Y);
                context.BeginFigure(firstScreen, pline.IsClosed);

                for (int i = 0; i < pline.VertexCount; i++)
                {
                    var v1 = pline.Get(i);
                    var nextIndex = (i + 1) % pline.VertexCount;
                    if (nextIndex == 0 && !pline.IsClosed)
                        break;

                    var v2 = pline.Get(nextIndex);

                    if (v1.BulgeIsZero())
                    {
                        var screenV2 = WorldToScreen(v2.X, v2.Y);
                        context.LineTo(screenV2);
                    }
                    else
                    {
                        try
                        {
                            (double radius, Vector2<double> center) = PlineSeg.SegArcRadiusAndCenter(v1, v2);
                            double startAngle = BaseMath.Angle(center, v1.Pos());
                            double sweepAngle = BaseMath.AngleFromBulge(v1.Bulge);
                            int segments = Math.Max(12, (int)(Math.Abs(sweepAngle) * 8.0));
                            for (int j = 1; j <= segments; j++)
                            {
                                double t = (double)j / segments;
                                double targetAngle = startAngle + sweepAngle * t;
                                Vector2<double> pt = BaseMath.PointOnCircle(radius, center, targetAngle);
                                var screenPt = WorldToScreen(pt.X, pt.Y);
                                context.LineTo(screenPt);
                            }
                        }
                        catch
                        {
                            var screenV2 = WorldToScreen(v2.X, v2.Y);
                            context.LineTo(screenV2);
                        }
                    }
                }

                context.EndFigure(pline.IsClosed);
            }

            return geometry;
        }
    }
}
