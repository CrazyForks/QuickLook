﻿// Copyright © 2017-2025 QL-Win Contributors
//
// This file is part of QuickLook program.
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

using QuickLook.Common.ExtensionMethods;
using QuickLook.Common.Plugin;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace QuickLook.Plugin.ImageViewer.AnimatedImage;

public class AnimatedImage : Image, IDisposable
{
    // List<Pair<formats, type>>
    public static List<KeyValuePair<string[], Type>> Providers = [];

    private AnimationProvider _animation;
    private bool _disposing;

    public void Dispose()
    {
        _disposing = true;

        BeginAnimation(AnimationFrameIndexProperty, null);
        Source = null;

        _animation?.Dispose();
        _animation = null;
    }

    public event EventHandler ImageLoaded;

    public event EventHandler DoZoomToFit;

    private static AnimationProvider InitAnimationProvider(Uri path, MetaProvider meta, ContextObject contextObject)
    {
        var ext = Path.GetExtension(path.LocalPath).ToLower();
        var type = Providers.First(p => p.Key.Contains(ext) || p.Key.Contains("*")).Value;

        var provider = type.CreateInstance<AnimationProvider>(path, meta, contextObject);

        return provider;
    }

    public static readonly DependencyProperty AnimationFrameIndexProperty =
        DependencyProperty.Register(nameof(AnimationFrameIndex), typeof(int), typeof(AnimatedImage),
            new UIPropertyMetadata(-1, AnimationFrameIndexChanged));

    public static readonly DependencyProperty AnimationUriProperty =
        DependencyProperty.Register(nameof(AnimationUri), typeof(Uri), typeof(AnimatedImage),
            new UIPropertyMetadata(null, AnimationUriChanged));

    public static readonly DependencyProperty MetaProperty =
        DependencyProperty.Register(nameof(Meta), typeof(MetaProvider), typeof(AnimatedImage));

    public static readonly DependencyProperty ContextObjectProperty =
        DependencyProperty.Register(nameof(ContextObject), typeof(ContextObject), typeof(AnimatedImage));

    public int AnimationFrameIndex
    {
        get => (int)GetValue(AnimationFrameIndexProperty);
        set => SetValue(AnimationFrameIndexProperty, value);
    }

    public Uri AnimationUri
    {
        get => (Uri)GetValue(AnimationUriProperty);
        set => SetValue(AnimationUriProperty, value);
    }

    public MetaProvider Meta
    {
        private get => (MetaProvider)GetValue(MetaProperty);
        set => SetValue(MetaProperty, value);
    }

    public ContextObject ContextObject
    {
        private get => (ContextObject)GetValue(ContextObjectProperty);
        set => SetValue(ContextObjectProperty, value);
    }

    private static void AnimationUriChanged(DependencyObject obj, DependencyPropertyChangedEventArgs ev)
    {
        if (obj is not AnimatedImage instance)
            return;

        instance._animation = InitAnimationProvider((Uri)ev.NewValue, instance.Meta, instance.ContextObject);
        ShowThumbnailAndStartAnimation(instance);
    }

    private static void ShowThumbnailAndStartAnimation(AnimatedImage instance)
    {
        var task = instance._animation.GetThumbnail(instance.ContextObject.PreferredSize);
        if (task == null) return;

        task.ContinueWith(_ => instance.Dispatcher.Invoke(() =>
        {
            if (instance._disposing)
                return;

            instance.Source = _.Result;

            if (_.Result != null)
            {
                instance.DoZoomToFit?.Invoke(instance, EventArgs.Empty);
                instance.ImageLoaded?.Invoke(instance, EventArgs.Empty);
            }

            instance.BeginAnimation(AnimationFrameIndexProperty, instance._animation?.Animator);
        }));
        task.Start();
    }

    private static void AnimationFrameIndexChanged(DependencyObject obj, DependencyPropertyChangedEventArgs ev)
    {
        if (obj is not AnimatedImage instance)
            return;

        if (instance._disposing)
            return;

        var task = instance._animation.GetRenderedFrame((int)ev.NewValue);

        task.ContinueWith(_ => instance.Dispatcher.Invoke(() =>
        {
            if (instance._disposing)
                return;

            var firstLoad = instance.Source == null;

            instance.Source = _.Result;

            if (firstLoad)
            {
                instance.DoZoomToFit?.Invoke(instance, EventArgs.Empty);
                instance.ImageLoaded?.Invoke(instance, EventArgs.Empty);
            }
        }));
        task.Start();
    }
}
