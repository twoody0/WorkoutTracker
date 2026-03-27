using System.Runtime.CompilerServices;
using MauiView = Microsoft.Maui.Controls.View;

#if ANDROID
using Android.Views;
using PlatformView = Android.Views.View;
#endif

namespace WorkoutTracker.Helpers;

public static class TabSwipeNavigationHelper
{
    private static readonly ConditionalWeakTable<ContentPage, SwipeAttachment> Attachments = new();

    public static void Attach(ContentPage page, string currentRoute, params MauiView[] additionalViews)
    {
        var attachment = Attachments.GetValue(page, _ => new SwipeAttachment(page));
        attachment.UpdateRoute(currentRoute, additionalViews);
    }

    public static void Refresh(ContentPage page)
    {
        if (Attachments.TryGetValue(page, out var attachment))
        {
            attachment.Refresh();
        }
    }

    private sealed class SwipeAttachment
    {
        private readonly ContentPage _page;
        private string _currentRoute = string.Empty;

#if ANDROID
        private readonly GestureDetector _gestureDetector;
        private readonly List<MauiView> _observedViews = new();
        private readonly List<PlatformView> _platformViews = new();
        private readonly Dictionary<PlatformView, EventHandler<PlatformView.LayoutChangeEventArgs>> _layoutHandlers = new();
#endif

        public SwipeAttachment(ContentPage page)
        {
            _page = page;
            _page.HandlerChanged += OnHandlerChanged;
            _page.Loaded += OnLoaded;

#if ANDROID
            _gestureDetector = new GestureDetector(Android.App.Application.Context, new HorizontalFlingListener(this));
#endif
        }

        public void UpdateRoute(string currentRoute, params MauiView[] additionalViews)
        {
            _currentRoute = currentRoute;
            RefreshObservedViews(additionalViews);
            TryAttach();
        }

        public void Refresh()
        {
#if ANDROID
            DetachPlatformViews();
#endif
            TryAttach();
        }

        private void OnHandlerChanged(object? sender, EventArgs e)
        {
#if ANDROID
            DetachPlatformViews();
#endif

            TryAttach();
        }

        private void OnLoaded(object? sender, EventArgs e)
        {
            TryAttach();
        }

        private void TryAttach()
        {
#if ANDROID
            if (string.IsNullOrWhiteSpace(_currentRoute))
            {
                return;
            }

            foreach (var view in _observedViews.ToList())
            {
                TryAttachView(view);
            }
#endif
        }

#if ANDROID
        private void RefreshObservedViews(params MauiView[] additionalViews)
        {
            foreach (var view in _observedViews)
            {
                view.HandlerChanged -= OnObservedViewHandlerChanged;
            }

            _observedViews.Clear();
            DetachPlatformViews();

            if (_page.Content is MauiView contentView)
            {
                AddObservedView(contentView);
            }

            foreach (var additionalView in additionalViews.Where(view => view != null))
            {
                AddObservedView(additionalView);
            }
        }

        private void AddObservedView(MauiView view)
        {
            if (_observedViews.Contains(view))
            {
                return;
            }

            _observedViews.Add(view);
            view.HandlerChanged += OnObservedViewHandlerChanged;
        }

        private void OnObservedViewHandlerChanged(object? sender, EventArgs e)
        {
            if (sender is MauiView view)
            {
                TryAttachView(view);
            }
        }

        private void TryAttachView(MauiView view)
        {
            if (view.Handler?.PlatformView is not PlatformView platformView)
            {
                return;
            }

            AttachPlatformViewTree(platformView);

            if (_layoutHandlers.ContainsKey(platformView))
            {
                return;
            }

            EventHandler<PlatformView.LayoutChangeEventArgs> layoutHandler = (_, _) => AttachPlatformViewTree(platformView);
            platformView.LayoutChange += layoutHandler;
            _layoutHandlers[platformView] = layoutHandler;
        }

        private void DetachPlatformViews()
        {
            foreach (var platformView in _platformViews)
            {
                platformView.Touch -= OnPlatformViewTouch;
            }

            _platformViews.Clear();

            foreach (var entry in _layoutHandlers)
            {
                entry.Key.LayoutChange -= entry.Value;
            }

            _layoutHandlers.Clear();
        }

        private void AttachPlatformViewTree(PlatformView platformView)
        {
            AttachPlatformView(platformView);

            if (platformView is not ViewGroup viewGroup)
            {
                return;
            }

            for (var index = 0; index < viewGroup.ChildCount; index++)
            {
                var child = viewGroup.GetChildAt(index);
                if (child != null)
                {
                    AttachPlatformViewTree(child);
                }
            }
        }

        private void AttachPlatformView(PlatformView platformView)
        {
            if (_platformViews.Contains(platformView))
            {
                return;
            }

            platformView.Touch += OnPlatformViewTouch;
            _platformViews.Add(platformView);
        }

        private void OnPlatformViewTouch(object? sender, Android.Views.View.TouchEventArgs e)
        {
            _gestureDetector.OnTouchEvent(e.Event);
            e.Handled = false;
        }

        private async Task NavigateAsync(int step)
        {
            if (Shell.Current is not AppShell appShell)
            {
                return;
            }

            await appShell.NavigateToAdjacentPrimaryTabAsync(_currentRoute, step);
        }

        private sealed class HorizontalFlingListener : GestureDetector.SimpleOnGestureListener
        {
            private const double SwipeMinDistance = 120;
            private const double SwipeMinVelocity = 250;
            private const double HorizontalDominanceRatio = 1.5;
            private readonly SwipeAttachment _attachment;

            public HorizontalFlingListener(SwipeAttachment attachment)
            {
                _attachment = attachment;
            }

            public override bool OnDown(MotionEvent? e) => true;

            public override bool OnFling(MotionEvent? e1, MotionEvent? e2, float velocityX, float velocityY)
            {
                if (e1 == null || e2 == null)
                {
                    return false;
                }

                var deltaX = e2.GetX() - e1.GetX();
                var deltaY = e2.GetY() - e1.GetY();

                if (Math.Abs(deltaX) < SwipeMinDistance)
                {
                    return false;
                }

                if (Math.Abs(velocityX) < SwipeMinVelocity)
                {
                    return false;
                }

                if (Math.Abs(deltaX) < Math.Abs(deltaY) * HorizontalDominanceRatio)
                {
                    return false;
                }

                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    await _attachment.NavigateAsync(deltaX < 0 ? 1 : -1);
                });

                return false;
            }
        }
#endif
    }
}
