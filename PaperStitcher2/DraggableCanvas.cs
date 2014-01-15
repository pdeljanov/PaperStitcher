//
// PaperStitcher -- A multi-monitor wallpaper utility
// Copyright (c) 2010 Philip Deljanov <philip.deljanov@gmail.com>
//
// This program is free software; you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation; either version 2 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License along
// with this program; if not, write to the Free Software Foundation, Inc.,
// 51 Franklin Street, Fifth Floor, Boston, MA 02110-1301 USA.
//

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace PaperStitcher2
{
    public class DraggableCanvas : Canvas
    {
        private const double SizingHandleSize = 40;

        private enum SizingHandles
        {
            TopLeft = 0,
            TopRight = 1,
            BottomLeft = 2,
            BottomRight = 3,
            TopCentre = 4,
            RightCentre = 5,
            BottomCentre = 6,
            LeftCentre = 7
        };

        public enum MeasureType
        {
            Absolute,
            Relative
        };

        private UIElement m_selectedElement;
        private UIElement m_dragElement;
        private Point m_origLocation;

        private bool m_isDraggingHandle;
        private SizingHandles m_isDragHandleSelected;

        private Rect m_newSelectedRect;
        private Rect m_origSelectedRect;
        private double m_origX;
        private double m_origY;
        private bool m_isDragging;

        private Ellipse[] m_handles;

        #region Attached Properties

        public static readonly DependencyProperty CanBeDraggedProperty;

        public static bool GetCanBeDragged(UIElement uiElement)
        {
            if (uiElement == null)
                return false;

            return (bool)uiElement.GetValue(CanBeDraggedProperty);
        }

        public static void SetCanBeDragged(UIElement uiElement, bool value)
        {
            if (uiElement != null)
                uiElement.SetValue(CanBeDraggedProperty, value);
        }

        #endregion

        #region Static Constructor

        static DraggableCanvas()
        {

            CanBeDraggedProperty = DependencyProperty.RegisterAttached(
                "CanBeDragged",
                typeof(bool),
                typeof(DraggableCanvas),
                new UIPropertyMetadata(true));
        }

        #endregion

        #region Constructor

        public DraggableCanvas()
        {
            m_handles = new Ellipse[4];

            for (int i = 0; i < m_handles.Length; i++)
            {
                m_handles[i] = new Ellipse();

                this.Children.Add(m_handles[i]);

                m_handles[i].Height = SizingHandleSize;
                m_handles[i].Width = SizingHandleSize;
                m_handles[i].StrokeThickness = 2;
                m_handles[i].Fill = new SolidColorBrush(Color.FromArgb(32, 255, 255, 255));
                m_handles[i].Stroke = Brushes.Red;
                m_handles[i].Opacity = 0;
                m_handles[i].IsEnabled = false;

                Canvas.SetZIndex(m_handles[i], 100);
            }

            m_handles[(int)SizingHandles.TopLeft].Cursor = Cursors.SizeNWSE;
            m_handles[(int)SizingHandles.TopRight].Cursor = Cursors.SizeNESW;
            m_handles[(int)SizingHandles.BottomLeft].Cursor = Cursors.SizeNESW;
            m_handles[(int)SizingHandles.BottomRight].Cursor = Cursors.SizeNWSE;
        }

        #endregion

        #region Interface

        public UIElement ElementBeingDragged
        {
            get
            {
                return this.m_dragElement;
            }
            protected set
            {
                if (this.m_dragElement != null)
                    this.m_dragElement.ReleaseMouseCapture();

                if (DraggableCanvas.GetCanBeDragged(value))
                {
                    this.m_dragElement = value;
                    this.m_dragElement.CaptureMouse();
                }
                else
                    this.m_dragElement = null;
            }
        }

        public UIElement SelectedElement
        {
            get
            {
                return this.m_selectedElement;
            }
            protected set
            {
                this.m_selectedElement = value;
            }
        }

        public void MoveUIElement(UIElement elem, Point newLocation, MeasureType type)
        {
            if (elem == null) return;

            Canvas.SetRight(elem, Double.NaN);
            Canvas.SetBottom(elem, Double.NaN);

            if (type == MeasureType.Relative)
            {
                newLocation.X += Canvas.GetLeft(elem);
                newLocation.Y += Canvas.GetTop(elem);
            }

            Canvas.SetLeft(elem, newLocation.X);
            Canvas.SetTop(elem, newLocation.Y);

            if (this.SelectedElement == elem)
            {
                this.m_origSelectedRect.X = newLocation.X;
                this.m_origSelectedRect.Y = newLocation.Y;
                PositionHandles(this.m_origSelectedRect, SizingHandles.BottomCentre);
            }

            if (OnChildElementMoved != null)
                OnChildElementMoved(elem, newLocation);
        }

        public void ResizeUIElement(UIElement elem, Size newSize, MeasureType type)
        {
            if (elem == null) return;

            FrameworkElement fe = (FrameworkElement)elem;

            if (type == MeasureType.Relative)
            {
                newSize.Width += fe.Width;
                newSize.Height += fe.Height;
            }

            fe.Width = newSize.Width;
            fe.Height = newSize.Height;

            if (fe.RenderTransform is ScaleTransform)
            {
                ScaleTransform st = (ScaleTransform)fe.RenderTransform;
                st.CenterX = newSize.Width / 2;
                st.CenterY = newSize.Height / 2;
            }

            if (this.SelectedElement == elem)
            {
                this.m_origSelectedRect.Width = newSize.Width;
                this.m_origSelectedRect.Height = newSize.Height;
                PositionHandles(this.m_origSelectedRect, SizingHandles.BottomCentre);
            }

            double x = Canvas.GetLeft(elem);
            double y = Canvas.GetTop(elem);

            if (OnChildElementResized != null)
                OnChildElementResized(elem, new Rect(x, y, newSize.Width, newSize.Height));
        }

        public void FlipUIElement(UIElement elem, bool flipX, bool flipY)
        {
            if (elem == null) return;

            FrameworkElement fe = (FrameworkElement)elem;

            double scaleX = (flipX == true) ? -1 : 1;
            double scaleY = (flipY == true) ? -1 : 1;

            ScaleTransform st = new ScaleTransform(scaleX, scaleY, fe.Width/2, fe.Height/2);

            fe.RenderTransform = st;

            if (OnChildElementFlipped != null)
                OnChildElementFlipped(elem, flipX, flipY);
        }

        public void SetUIElementZIndex(UIElement elem, int index)
        {
            Canvas.SetZIndex(elem, index);

            if (OnChildElementZIndexChanged != null)
                OnChildElementZIndexChanged(elem, index);
        }

        public void SendUIElementBackwards(UIElement elem)
        {
            int pivot = Canvas.GetZIndex(elem);
            int index = pivot - 1;

            if ( index < 0 ){ return; }

            UIElement swapWith = FindByZIndex(index);

            Canvas.SetZIndex(swapWith, pivot);
            Canvas.SetZIndex(elem, index);

            if (OnChildElementZIndexChanged != null)
            {
                OnChildElementZIndexChanged(elem, index);
                OnChildElementZIndexChanged(swapWith, pivot);
            }
        }

        public void SendUIElementForwards(UIElement elem)
        {
            int pivot = Canvas.GetZIndex(elem);
            int index = pivot + 1;
            int frontIndex = this.Children.Count - m_handles.Length - 1;

            //  Check if we can even go any more ahead
            if ( index > frontIndex ){   return;   }

            UIElement swapWith = FindByZIndex(index);

            Canvas.SetZIndex(swapWith, pivot);
            Canvas.SetZIndex(elem, index);

            if (OnChildElementZIndexChanged != null)
            {
                OnChildElementZIndexChanged(elem, index);
                OnChildElementZIndexChanged(swapWith, pivot);
            }
        }

        public void SendUIElementToBack(UIElement elem)
        {
            int pivot = Canvas.GetZIndex(elem);

            Canvas.SetZIndex(elem, 0);

            ShiftZIndicies(ShiftDirection.Forwards, pivot, elem);

            if (OnChildElementZIndexChanged != null)
                OnChildElementZIndexChanged(elem, 0);
        }

        public void SendUIElementToFront(UIElement elem)
        {
            int pivot = Canvas.GetZIndex(elem);

            int frontIndex = this.Children.Count - m_handles.Length - 1;
            Canvas.SetZIndex(elem, frontIndex);

            ShiftZIndicies(ShiftDirection.Backwards, pivot, elem);

            if (OnChildElementZIndexChanged != null)
                OnChildElementZIndexChanged(elem, frontIndex);
        }

        public void ClearChildren()
        {
            int count = this.Children.Count - m_handles.Length;
            this.Children.RemoveRange(m_handles.Length, count);
        }

        public void AddChild(UIElement child)
        {
            this.Children.Add(child);

            int frontIndex = this.Children.Count - m_handles.Length - 1;

            Canvas.SetZIndex(child, frontIndex);

            if (OnChildElementZIndexChanged != null)
                OnChildElementZIndexChanged(child, frontIndex);
        }

        public void DeleteChild(UIElement child)
        {
            if (m_selectedElement == child)
            {
                m_selectedElement = null;
                SetHandleVisibility(false);
            }

            int pivot = Canvas.GetZIndex(child);
            int maxZ = this.Children.Count - m_handles.Length - 1;

            this.Children.Remove(child);

            if (pivot == 0)
            {
                ShiftZIndicies(ShiftDirection.Backwards, 0, null);
            }
            else if( pivot != maxZ )
            {
                ShiftZIndicies(ShiftDirection.Backwards, pivot, null);
            }
        }

        public UIElement First()
        {
            return this.Children[m_handles.Length];
        }

        #endregion

        #region Overrides
        
        protected override void OnPreviewMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnPreviewMouseLeftButtonDown(e);

            bool hadSelection = false;

            this.m_isDragging = false;
            this.m_isDraggingHandle = false;

            this.m_origLocation = e.GetPosition(this);

            //  Mark that we had a previously selected element
            if (this.m_selectedElement != null)
                hadSelection = true;

            //  Get the selected element (also the one being dragged)
            this.m_dragElement = this.FindCanvasChild(e.Source as DependencyObject);

            //  If the selection is null, do nothing
            if (this.m_dragElement == null)
            {
                this.m_selectedElement = null;

                //  If we had a previous selection, hide the handles
                if (hadSelection)
                    SetHandleVisibility(false);

                if (OnSelectedElementChanged != null && hadSelection)
                    OnSelectedElementChanged(this, new EventArgs());

                return;
            }

            //  Check if we are dragging a handle
            for (int i = 0; i < m_handles.Length; i++)
            {
                if (this.m_dragElement == m_handles[i])
                {
                    this.m_isDraggingHandle = true;
                    this.m_isDragHandleSelected = (SizingHandles)i;

                    break;
                }
            }

            //  Get the position of the element (NaN's mean the object isn't bound to the side)
            double left = Canvas.GetLeft(this.m_dragElement);
            double right = Canvas.GetRight(this.m_dragElement);
            double top = Canvas.GetTop(this.m_dragElement);
            double bottom = Canvas.GetBottom(this.m_dragElement);

            //  FE of element being dragged
            FrameworkElement fse = (FrameworkElement)this.m_dragElement;

            //  Normalize coordinates of the element (if they're whacko)
            if (Double.IsNaN(left))
            {
                double nl = right - fse.Width;
                Canvas.SetLeft(this.m_dragElement, nl);
                Canvas.SetRight(this.m_dragElement, Double.NaN);
            }
            if (Double.IsNaN(top))
            {
                double nt = bottom - fse.Height;
                Canvas.SetTop(this.m_dragElement, nt);
                Canvas.SetBottom(this.m_dragElement, Double.NaN);
            }

            m_origX = Canvas.GetLeft(this.m_dragElement);
            m_origY = Canvas.GetTop(this.m_dragElement);

            //  If we aren't dragging a handle, cache the rectangle of the selected element
            if (!m_isDraggingHandle)
            {
                if (this.m_dragElement != this.m_selectedElement)
                {
                    this.m_selectedElement = this.m_dragElement;

                    if (OnSelectedElementChanged != null)
                        OnSelectedElementChanged(this, new EventArgs());
                }

                double x = Canvas.GetLeft(this.m_selectedElement);
                double y = Canvas.GetTop(this.m_selectedElement);

                this.m_origSelectedRect = new Rect(x, y, fse.Width, fse.Height);
                

                //  Position the sizing handles around the object
                PositionHandles(this.m_origSelectedRect, SizingHandles.BottomCentre);

                //  If we didn't have a previous selections, show the handles
                if (!hadSelection)
                    SetHandleVisibility(true);
            }

            this.m_newSelectedRect = this.m_origSelectedRect;

            //  Mark that we will drag
            this.m_isDragging = true;

            //  Mark that we handles the mouse down
            e.Handled = true;
        }

        protected override void OnPreviewMouseMove(MouseEventArgs e)
        {
            base.OnPreviewMouseMove(e);

            //  If we aren't dragging, or have no drag element, do nothing
            if (this.m_dragElement == null || !this.m_isDragging)
                return;

            //  Get the position of the cursor
            Point cursorLocation = e.GetPosition(this);

            double dx = cursorLocation.X - this.m_origLocation.X;
            double dy = cursorLocation.Y - this.m_origLocation.Y;

            double x = m_origX + dx;
            double y = m_origY + dy;

            if (!m_isDraggingHandle)
            {
                //  Set the new position
                Canvas.SetLeft(this.m_dragElement, x);
                Canvas.SetTop(this.m_dragElement, y);

                //  Find the bounding rectangle of the element
                m_origSelectedRect.X = x;
                m_origSelectedRect.Y = y;

                //  Set the position of the handles
                PositionHandles(this.m_origSelectedRect, SizingHandles.BottomCentre);
            }
            else
            {
                double nw = 0, nh = 0;

                switch (this.m_isDragHandleSelected)
                {
                    case SizingHandles.BottomRight:
                        {
                            nw = this.m_origSelectedRect.Width + dx;
                            nh = this.m_origSelectedRect.Height + dy;
                            if (nw < 1 || nh < 1) return;

                            break;
                        }
                    case SizingHandles.BottomLeft:
                        {
                            nw = this.m_origSelectedRect.Width - dx;
                            nh = this.m_origSelectedRect.Height + dy;
                            if (nw < 1 || nh < 1) return;

                            this.m_newSelectedRect.X = this.m_origSelectedRect.X + dx;

                            break;
                        }
                    case SizingHandles.TopLeft:
                        {
                            nw = this.m_origSelectedRect.Width - dx;
                            nh = this.m_origSelectedRect.Height - dy;
                            if (nw < 1 || nh < 1) return;

                            this.m_newSelectedRect.X = this.m_origSelectedRect.X + dx;
                            this.m_newSelectedRect.Y = this.m_origSelectedRect.Y + dy;

                            break;
                        }
                    case SizingHandles.TopRight:
                        {
                            nw = this.m_origSelectedRect.Width + dx;
                            nh = this.m_origSelectedRect.Height - dy;
                            if (nw < 1 || nh < 1) return;

                            this.m_newSelectedRect.Y = this.m_origSelectedRect.Y + dy;

                            break;
                        }
                }

                this.m_newSelectedRect.Width = nw;
                this.m_newSelectedRect.Height = nh;

                FrameworkElement fe = (FrameworkElement)this.m_selectedElement;

                fe.Width = nw;
                fe.Height = nh;

                if (fe.RenderTransform is ScaleTransform)
                {
                    ScaleTransform st = (ScaleTransform)fe.RenderTransform;
                    st.CenterX = nw / 2;
                    st.CenterY = nh / 2;
                }

                Canvas.SetLeft(this.m_selectedElement, this.m_newSelectedRect.X);
                Canvas.SetTop(this.m_selectedElement, this.m_newSelectedRect.Y);

                //  Set the new position
                Canvas.SetLeft(this.m_dragElement, x);
                Canvas.SetTop(this.m_dragElement, y);

                PositionHandles(this.m_newSelectedRect, this.m_isDragHandleSelected);
            }
        }

        protected override void OnMouseLeave(MouseEventArgs e)
        {
            base.OnMouseLeave(e);
            MouseUpHandler();            
        }

        protected override void OnMouseEnter(MouseEventArgs e)
        {
            base.OnMouseEnter(e);
        }

        protected override void OnPreviewMouseUp(MouseButtonEventArgs e)
        {
            base.OnPreviewMouseUp(e);
            MouseUpHandler();
        }

        private void MouseUpHandler()
        {
            if (this.m_dragElement != null)
            {
                if (m_isDraggingHandle)
                {
                    this.m_origSelectedRect = this.m_newSelectedRect;

                    this.m_isDraggingHandle = false;

                    if (OnChildElementResized != null)
                        OnChildElementResized(this.m_selectedElement, this.m_origSelectedRect);
                }
                else
                {
                    if (OnChildElementMoved != null)
                        OnChildElementMoved(this.ElementBeingDragged, this.m_origSelectedRect.Location);
                }
            }

            this.m_isDragging = false;
            this.m_dragElement = null;
        }

        #endregion

        #region Events

        public delegate void ChildElementMoveHandler(UIElement elem, Point location);
        public delegate void ChildElementResizeHandler(UIElement elem, Rect bounds);
        public delegate void ChildElementFlippedHandler(UIElement elem, bool isFlippedX, bool isFlippedY);
        public delegate void ChildElementZIndexHandler(UIElement elem, int index);

        public event ChildElementMoveHandler OnChildElementMoved;
        public event ChildElementResizeHandler OnChildElementResized;
        public event ChildElementFlippedHandler OnChildElementFlipped;
        public event ChildElementZIndexHandler OnChildElementZIndexChanged;
        public event EventHandler OnSelectedElementChanged;

        #endregion

        private void PositionHandles(Rect elemRect, SizingHandles ignore)
        {
            double halfSize = (SizingHandleSize / 2);

            if (ignore != SizingHandles.TopLeft)
            {
                Canvas.SetLeft(m_handles[(int)SizingHandles.TopLeft], elemRect.Left - halfSize);
                Canvas.SetTop(m_handles[(int)SizingHandles.TopLeft], elemRect.Top - halfSize);
            }

            if (ignore != SizingHandles.TopRight)
            {
                Canvas.SetLeft(m_handles[(int)SizingHandles.TopRight], elemRect.Right - halfSize);
                Canvas.SetTop(m_handles[(int)SizingHandles.TopRight], elemRect.Top - halfSize);
            }

            if (ignore != SizingHandles.BottomLeft)
            {
                Canvas.SetLeft(m_handles[(int)SizingHandles.BottomLeft], elemRect.Left - halfSize);
                Canvas.SetTop(m_handles[(int)SizingHandles.BottomLeft], elemRect.Bottom - halfSize);
            }

            if (ignore != SizingHandles.BottomRight)
            {
                Canvas.SetLeft(m_handles[(int)SizingHandles.BottomRight], elemRect.Right - halfSize);
                Canvas.SetTop(m_handles[(int)SizingHandles.BottomRight], elemRect.Bottom - halfSize);
            }
        }

        private void SetHandleVisibility(bool show)
        {
            DoubleAnimation anim = new DoubleAnimation();

            if (show)
            {
                anim.From = 0;
                anim.To = 1;
            }
            else
            {
                anim.From = 1;
                anim.To = 0;
            }

            anim.Duration = new Duration(TimeSpan.FromSeconds(.125));

            for (int i = 0; i < m_handles.Length; i++)
            {
                m_handles[i].IsEnabled = show;
                m_handles[i].BeginAnimation(Ellipse.OpacityProperty, anim);
            }
        }

        #region Private Helpers

        private UIElement FindCanvasChild(DependencyObject depObj)
        {
            while (depObj != null)
            {
                // If the current object is a UIElement which is a child of the
                // Canvas, exit the loop and return it.
                UIElement elem = depObj as UIElement;
                if (elem != null && base.Children.Contains(elem))
                    break;

                // VisualTreeHelper works with objects of type Visual or Visual3D.
                // If the current object is not derived from Visual or Visual3D,
                // then use the LogicalTreeHelper to find the parent element.
                if (depObj is Visual || depObj is Visual3D)
                    depObj = VisualTreeHelper.GetParent(depObj);
                else
                    depObj = LogicalTreeHelper.GetParent(depObj);
            }
            return depObj as UIElement;
        }


        private enum ShiftDirection
        {
            Backwards,
            Forwards
        }

        private void ShiftZIndicies(ShiftDirection direction, int pivot, UIElement exclusion)
        {
            for(int i = m_handles.Length; i < this.Children.Count; ++i)
            {
                if (this.Children[i] != exclusion)
                {
                    int z = Canvas.GetZIndex(this.Children[i]);

                    if (direction == ShiftDirection.Backwards && z > pivot)
                    {
                        Canvas.SetZIndex(this.Children[i], --z);

                        if (OnChildElementZIndexChanged != null)
                            OnChildElementZIndexChanged(this.Children[i], z);
                    }
                    else if( direction == ShiftDirection.Forwards && z < pivot)
                    {
                        Canvas.SetZIndex(this.Children[i], ++z);

                        if (OnChildElementZIndexChanged != null)
                            OnChildElementZIndexChanged(this.Children[i], z);
                    }
                }
            }
        }

        private UIElement FindByZIndex(int index)
        {
            UIElement elem = null;

            for (int i = m_handles.Length; i < this.Children.Count; ++i)
            {
                if (Canvas.GetZIndex(this.Children[i]) == index)
                {
                    elem = this.Children[i];
                    break;
                }
            }

            return elem as UIElement;
        }

        #endregion

    }
}
