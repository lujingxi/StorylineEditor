using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Shapes;

namespace StorylineEditor
{
    public class DragAdorner : Adorner
    {
        private readonly Rectangle _child;
        private readonly double _offsetLeft;
        private readonly double _offsetTop;
        private double _left;
        private double _top;
        private AdornerLayer? _adornerLayer;

        public DragAdorner(UIElement adornedElement, UIElement dragElement, Point mouseOffset)
            : base(adornedElement)
        {
            // Capture the visual of the element
            var brush = new VisualBrush(dragElement) { Opacity = 0.6 };

            _child = new Rectangle
            {
                Width = dragElement.RenderSize.Width,
                Height = dragElement.RenderSize.Height,
                Fill = brush,
                IsHitTestVisible = false
            };

            // Capture where the user clicked within the item 
            // so the ghost doesn't "jump" to center on the mouse
            _offsetLeft = mouseOffset.X;
            _offsetTop = mouseOffset.Y;
        }

        public void UpdatePosition(Point screenPos)
        {
            // Translate mouse position relative to the AdornedElement
            var localPos = AdornedElement.PointFromScreen(screenPos);

            _left = localPos.X - _offsetLeft;
            _top = localPos.Y - _offsetTop;

            // Force an update to the AdornerLayer
            _adornerLayer ??= AdornerLayer.GetAdornerLayer(AdornedElement);
            _adornerLayer?.Update();
        }

        // Required for Hosting a UIElement in an Adorner
        protected override Visual GetVisualChild(int index) => _child;
        protected override int VisualChildrenCount => 1;

        protected override Size MeasureOverride(Size constraint)
        {
            _child.Measure(constraint);
            return _child.DesiredSize;
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            _child.Arrange(new Rect(_left, _top, _child.DesiredSize.Width, _child.DesiredSize.Height));
            return finalSize;
        }
    }
}
