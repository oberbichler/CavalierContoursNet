using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Interactivity;
using CavalierContours.Polyline;

namespace CavalierContours.Example
{
    public partial class MainWindow : Window
    {
        private bool _isUpdatingUi = false;

        private bool _initialized = false;

        public MainWindow()
        {
            InitializeComponent();
            Canvas.SelectedVertexChanged += OnSelectedVertexChanged;
            Opened += OnWindowOpened;
        }

        private void OnWindowOpened(object? sender, EventArgs e)
        {
            Opened -= OnWindowOpened;

            // Force slider and text to exactly 20 after layout is complete
            SldOffsetDist.Value = 20.0;
            TxtOffsetDist.Text = "20";
            SldRingsCount.Value = 1;
            TxtRingsCount.Text = "1";

            // Now wire events
            SldOffsetDist.ValueChanged += OnOffsetSliderChanged;
            TxtOffsetDist.TextChanged += OnOffsetTextChanged;
            SldRingsCount.ValueChanged += OnRingsCountSliderChanged;
            TxtRingsCount.TextChanged += OnRingsCountTextChanged;
            ChkSelfIntersects.IsCheckedChanged += OnSelfIntersectsChanged;

            _initialized = true;

            UpdateUiFromSelectedVertex();
            Canvas.ComputeOffset(20.0, ChkSelfIntersects.IsChecked ?? true, 1);
        }

        private void OnSelectedVertexChanged(object? sender, EventArgs e)
        {
            UpdateUiFromSelectedVertex();
        }

        private void UpdateUiFromSelectedVertex()
        {
            _isUpdatingUi = true;
            try
            {
                var activePline = Canvas.ActivePolyline;
                int selIdx = Canvas.SelectedVertexIndex;

                if (selIdx >= 0 && selIdx < activePline.VertexCount)
                {
                    var v = activePline.Get(selIdx);
                    TxtX.Text = v.X.ToString("F1");
                    TxtY.Text = v.Y.ToString("F1");
                    TxtBulge.Text = v.Bulge.ToString("F3");
                    SldBulge.Value = Math.Clamp(v.Bulge, -3.0, 3.0);

                    VertexPropsPanel.IsEnabled = true;
                    BtnDeleteVertex.IsEnabled = true;
                }
                else
                {
                    TxtX.Text = string.Empty;
                    TxtY.Text = string.Empty;
                    TxtBulge.Text = "0.0";
                    SldBulge.Value = 0.0;

                    VertexPropsPanel.IsEnabled = false;
                    BtnDeleteVertex.IsEnabled = false;
                }

                // Stats
                LblVertexCount.Text = activePline.VertexCount.ToString();
                try
                {
                    LblArea.Text = activePline.Area().ToString("F2");
                }
                catch
                {
                    LblArea.Text = "N/A";
                }
                try
                {
                    LblLength.Text = activePline.PathLength().ToString("F2");
                }
                catch
                {
                    LblLength.Text = "N/A";
                }

                ChkIsClosed.IsChecked = activePline.IsClosed;
            }
            finally
            {
                _isUpdatingUi = false;
            }
        }

        private void OnActivePolylineChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (_isUpdatingUi) return;
            if (Canvas == null) return;

            Canvas.ActivePolylineIndex = CboActivePolyline.SelectedIndex;
            UpdateUiFromSelectedVertex();
        }

        private void OnIsClosedChanged(object? sender, RoutedEventArgs e)
        {
            if (_isUpdatingUi) return;

            Canvas.ActivePolyline.SetIsClosed(ChkIsClosed.IsChecked ?? false);
            Canvas.ClearResults();
            Canvas.InvalidateVisual();
            UpdateUiFromSelectedVertex();
        }

        private void OnClearActive(object? sender, RoutedEventArgs e)
        {
            Canvas.ClearActive();
        }

        private void OnDeleteVertex(object? sender, RoutedEventArgs e)
        {
            Canvas.DeleteSelectedVertex();
        }

        private void OnVertexCoordsChanged(object? sender, TextChangedEventArgs e)
        {
            if (_isUpdatingUi) return;

            if (double.TryParse(TxtX.Text, out double x) && double.TryParse(TxtY.Text, out double y))
            {
                double bulge = 0.0;
                double.TryParse(TxtBulge.Text, out bulge);
                Canvas.UpdateSelectedVertex(x, y, bulge);
            }
        }

        private void OnBulgeSliderChanged(object? sender, Avalonia.Controls.Primitives.RangeBaseValueChangedEventArgs e)
        {
            if (_isUpdatingUi) return;

            double bulge = SldBulge.Value;
            _isUpdatingUi = true;
            try
            {
                TxtBulge.Text = bulge.ToString("F3");
            }
            finally
            {
                _isUpdatingUi = false;
            }

            if (double.TryParse(TxtX.Text, out double x) && double.TryParse(TxtY.Text, out double y))
            {
                Canvas.UpdateSelectedVertex(x, y, bulge);
            }
        }

        private void OnBulgeTextChanged(object? sender, TextChangedEventArgs e)
        {
            if (_isUpdatingUi) return;

            if (double.TryParse(TxtBulge.Text, out double bulge))
            {
                _isUpdatingUi = true;
                try
                {
                    SldBulge.Value = Math.Clamp(bulge, -3.0, 3.0);
                }
                finally
                {
                    _isUpdatingUi = false;
                }

                if (double.TryParse(TxtX.Text, out double x) && double.TryParse(TxtY.Text, out double y))
                {
                    Canvas.UpdateSelectedVertex(x, y, bulge);
                }
            }
        }

        private void TriggerAutoOffset()
        {
            if (!_initialized) return;
            if (double.TryParse(TxtOffsetDist.Text, out double dist))
            {
                int rings = 1;
                int.TryParse(TxtRingsCount.Text, out rings);
                Canvas.ComputeOffset(dist, ChkSelfIntersects.IsChecked ?? true, Math.Max(1, rings));
            }
        }

        private void OnRingsCountSliderChanged(object? sender, Avalonia.Controls.Primitives.RangeBaseValueChangedEventArgs e)
        {
            if (_isUpdatingUi) return;

            double val = SldRingsCount.Value;
            _isUpdatingUi = true;
            try
            {
                TxtRingsCount.Text = ((int)val).ToString();
            }
            finally
            {
                _isUpdatingUi = false;
            }

            TriggerAutoOffset();
        }

        private void OnRingsCountTextChanged(object? sender, TextChangedEventArgs e)
        {
            if (_isUpdatingUi) return;

            if (double.TryParse(TxtRingsCount.Text, out double val))
            {
                _isUpdatingUi = true;
                try
                {
                    SldRingsCount.Value = Math.Clamp((int)val, 1, 10);
                }
                finally
                {
                    _isUpdatingUi = false;
                }

                TriggerAutoOffset();
            }
        }

        private void OnOffsetSliderChanged(object? sender, Avalonia.Controls.Primitives.RangeBaseValueChangedEventArgs e)
        {
            if (_isUpdatingUi) return;

            double val = SldOffsetDist.Value;
            _isUpdatingUi = true;
            try
            {
                TxtOffsetDist.Text = val.ToString("F1");
            }
            finally
            {
                _isUpdatingUi = false;
            }

            TriggerAutoOffset();
        }

        private void OnOffsetTextChanged(object? sender, TextChangedEventArgs e)
        {
            if (_isUpdatingUi) return;

            if (double.TryParse(TxtOffsetDist.Text, out double val))
            {
                _isUpdatingUi = true;
                try
                {
                    SldOffsetDist.Value = Math.Clamp(val, -120.0, 120.0);
                }
                finally
                {
                    _isUpdatingUi = false;
                }

                TriggerAutoOffset();
            }
        }

        private void OnSelfIntersectsChanged(object? sender, RoutedEventArgs e)
        {
            TriggerAutoOffset();
        }

        private void OnExecuteBoolean(object? sender, RoutedEventArgs e)
        {
            BooleanOp op = BooleanOp.Or;
            switch (CboBooleanOp.SelectedIndex)
            {
                case 0: op = BooleanOp.Or; break;
                case 1: op = BooleanOp.And; break;
                case 2: op = BooleanOp.Not; break;
                case 3: op = BooleanOp.Xor; break;
            }
            Canvas.ComputeBoolean(op);
        }

        private void OnClearResults(object? sender, RoutedEventArgs e)
        {
            Canvas.ClearResults();
        }

        private void OnResetView(object? sender, RoutedEventArgs e)
        {
            Canvas.ResetView();
        }

        private void OnSampleShapeSelected(object? sender, SelectionChangedEventArgs e)
        {
            if (CboSampleShape.SelectedItem is ComboBoxItem item && item.Content is string name)
            {
                Canvas.LoadSampleShape(name);
                UpdateUiFromSelectedVertex();
                CboSampleShape.SelectedIndex = -1; // Reset so same shape can be picked again
            }
        }
    }
}
