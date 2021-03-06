﻿// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

using SiliconStudio.Presentation.Controls;

namespace SiliconStudio.Presentation.Behaviors
{
    public class TilePanelNavigationBehavior : DeferredBehaviorBase<VirtualizingTilePanel>
    {
        private Selector selector;

        protected override void OnAttachedOverride()
        {
            DependencyObject parent = AssociatedObject;
            while (parent != null)
            {
                parent = VisualTreeHelper.GetParent(parent);
                if (parent is Selector)
                    break;
            }

            if (parent == null)
            {
                throw new InvalidOperationException("Unable to find a parent Selector to the associated VirtualizingTilePanel.");
            }

            selector = (Selector)parent;

            selector.IsSynchronizedWithCurrentItem = true;
            KeyboardNavigation.SetDirectionalNavigation(selector, KeyboardNavigationMode.None);

            selector.PreviewKeyDown += OnAssociatedObjectKeyDown;
        }

        protected override void OnDetachingOverride()
        {
            // TODO: Not sure why, but it seems that we get an Unloaded event before the Loaded event if loading a broken package. Need to investigate
            if (selector != null)
                selector.PreviewKeyDown -= OnAssociatedObjectKeyDown;
            selector = null;
        }

        private void OnAssociatedObjectKeyDown(object sender, KeyEventArgs e)
        {
            if (selector.SelectedIndex == -1 || selector.Items.Count == 0)
                return;

            var window = Window.GetWindow(selector);
            if (window == null)
                return;

            // Find the currently focused element (logical focus)
            var focusedElement = FocusManager.GetFocusedElement(window) as DependencyObject;
            if (focusedElement == null)
                return;

            // Because of virtualization, we have to find the container that correspond to the selected item (by its index)
            var element = selector.ItemContainerGenerator.ContainerFromIndex(selector.SelectedIndex);
            // focusedElement can be either the current selector or one of its item container
            if (!ReferenceEquals(focusedElement, selector) && !ReferenceEquals(focusedElement, element))
            {
                // In this case, it means another control is focused, either a control out of scope, or an editing text box in the scope of the item container
                return;
            }

            bool moved;

            switch (e.Key)
            {
                case Key.Right:
                    moved = AssociatedObject.Orientation == Orientation.Vertical ? MoveToNextItem() : MoveToNextLineItem(1);
                    break;

                case Key.Left:
                    moved = AssociatedObject.Orientation == Orientation.Vertical ? MoveToPreviousItem() : MoveToPreviousLineItem(1);
                    break;

                case Key.Up:
                    moved = AssociatedObject.Orientation == Orientation.Vertical ? MoveToPreviousLineItem(1) : MoveToPreviousItem();
                    break;

                case Key.Down:
                    moved = AssociatedObject.Orientation == Orientation.Vertical ? MoveToNextLineItem(1) : MoveToNextItem();
                    break;

                case Key.PageUp:
                    if (AssociatedObject.Orientation == Orientation.Vertical)
                    {
                        var itemHeight = AssociatedObject.ItemSlotSize.Height + AssociatedObject.MinimumItemSpacing * 2.0f;
                        moved = MoveToPreviousLineItem((int)(AssociatedObject.ViewportHeight / itemHeight));
                    }
                    else
                    {
                        var itemWidth = AssociatedObject.ItemSlotSize.Width + AssociatedObject.MinimumItemSpacing * 2.0f;
                        moved = MoveToPreviousLineItem((int)(AssociatedObject.ViewportWidth / itemWidth));
                    }
                    break;

                case Key.PageDown:
                    if (AssociatedObject.Orientation == Orientation.Vertical)
                    {
                        var itemHeight = AssociatedObject.ItemSlotSize.Height + AssociatedObject.MinimumItemSpacing * 2.0f;
                        moved = MoveToNextLineItem((int)(AssociatedObject.ViewportHeight / itemHeight));
                    }
                    else
                    {
                        var itemWidth = AssociatedObject.ItemSlotSize.Width + AssociatedObject.MinimumItemSpacing * 2.0f;
                        moved = MoveToNextLineItem((int)(AssociatedObject.ViewportWidth / itemWidth));
                    }
                    break;

                case Key.Home:
                    moved = selector.Items.MoveCurrentToFirst();
                    break;

                case Key.End:
                    moved = selector.Items.MoveCurrentToLast();
                    break;

                default:
                    return;
            }

            e.Handled = true;

            if (moved)
            {
                if (selector.SelectedIndex > -1)
                {
                    AssociatedObject.ScrollToIndexedItem(selector.SelectedIndex);

                    if (selector.ItemContainerGenerator != null && selector.SelectedItem != null)
                    {
                        var lbi = selector.ItemContainerGenerator.ContainerFromItem(selector.SelectedItem) as UIElement;
                        if (lbi != null)
                            lbi.Focus();
                    }
                }
            }
        }

        private bool MoveToPreviousLineItem(int lineCount)
        {
            var moved = false;

            int newPos = selector.SelectedIndex - (AssociatedObject.ItemsPerLine * lineCount);

            if (newPos >= 0)
                moved = selector.Items.MoveCurrentToPosition(newPos);

            return moved;
        }

        private bool MoveToNextLineItem(int lineCount)
        {
            var moved = false;

            if (AssociatedObject.ItemCount > -1)
            {
                int newPos = selector.SelectedIndex + (AssociatedObject.ItemsPerLine * lineCount);

                if (newPos < AssociatedObject.ItemCount)
                    moved = selector.Items.MoveCurrentToPosition(newPos);
            }
            return moved;
        }

        private bool MoveToPreviousItem()
        {
            bool moved = selector.Items.MoveCurrentToPrevious();
            if (moved == false)
            {
                if (selector.SelectedItem == null)
                    selector.Items.MoveCurrentToFirst();
            }
            return moved;
        }

        private bool MoveToNextItem()
        {
            bool moved = selector.Items.MoveCurrentToNext();
            if (moved == false)
            {
                if (selector.SelectedItem == null)
                    selector.Items.MoveCurrentToLast();
            }
            return moved;
        }
    }
}
