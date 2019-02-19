﻿using ExClient.Forums;
using ExClient.Status;
using Opportunity.MvvmUniverse.Services.Navigation;
using Opportunity.MvvmUniverse.Views;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Windows.Foundation;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

//“空白页”项模板在 http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409 上有介绍

namespace ExViewer.Views
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class RootControl : MvvmPage
    {
        public RootControl()
        {
            InitializeComponent();

            manager.Handlers.Add(fm_inner.AsNavigationHandler());

            tabs = new Dictionary<Controls.SplitViewTab, Type>()
            {
                [svt_Saved] = typeof(SavedPage),
                [svt_Cached] = typeof(CachedPage),
                [svt_Search] = typeof(SearchPage),
                [svt_Favorites] = typeof(FavoritesPage),
                [svt_Popular] = typeof(PopularPage),
                [svt_Toplist] = typeof(ToplistPage),
                [svt_Settings] = typeof(SettingsPage)
            };

            pages = new Dictionary<Type, Controls.SplitViewTab>()
            {
                [typeof(CachedPage)] = svt_Cached,
                [typeof(SavedPage)] = svt_Saved,
                [typeof(SearchPage)] = svt_Search,
                [typeof(FavoritesPage)] = svt_Favorites,
                [typeof(PopularPage)] = svt_Popular,
                [typeof(ToplistPage)] = svt_Toplist,
                [typeof(SettingsPage)] = svt_Settings
            };

            vistualizeFocus();
        }

        [Conditional("DEBUG")]
        private void vistualizeFocus()
        {
            GotFocus += (s, e) =>
            {
                var focus = FocusManager.GetFocusedElement();
                if (focus is null)
                {
                    return;
                }

                var fe = focus as FrameworkElement;
                var con = fe as Control;
                Debug.WriteLine($"{(fe?.Name ?? focus.ToString())}({focus.GetType()}) {con?.FocusState}", "Focus state");
            };
        }

        private readonly Dictionary<Controls.SplitViewTab, Type> tabs;
        private readonly Dictionary<Type, Controls.SplitViewTab> pages;

        private bool layoutLoaded;

        public UserInfo UserInfo
        {
            get => (UserInfo)GetValue(UserInfoProperty);
            set => SetValue(UserInfoProperty, value);
        }

        // Using a DependencyProperty as the backing store for UserInfo.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty UserInfoProperty =
            DependencyProperty.Register("UserInfo", typeof(UserInfo), typeof(RootControl), new PropertyMetadata(null));

        private readonly Navigator manager = Navigator.GetOrCreateForCurrentView();

        private async void Control_Loading(FrameworkElement sender, object args)
        {
            if (!layoutLoaded)
            {
                RootController.SetRoot(this);
            }
            else
            {
                await manager.NavigateAsync(typeof(SearchPage));
            }
        }

        private async void Control_Loaded(object sender, RoutedEventArgs e)
        {
            var temp = layoutLoaded;
            layoutLoaded = true;
            if (!temp)
            {
                UserInfo = await UserInfo.LoadFromCache();
                RootController.UpdateUserInfo(false);
            }
            else
            {
                RootController.HandleUriLaunch();
                tbtPane.Focus(FocusState.Pointer);
            }
        }

        private void Control_Unloaded(object sender, RoutedEventArgs e)
        {
        }

        private void fm_inner_Navigated(object sender, NavigationEventArgs e)
        {
            var pageType = fm_inner.Content.GetType();
            if (pages.TryGetValue(pageType, out var tab))
            {
                tab.IsChecked = true;
            }
        }

        private void fm_inner_Navigating(object sender, NavigatingCancelEventArgs e)
        {
            var content = fm_inner.Content;
            if (content is null)
            {
                return;
            }

            var pageType = content.GetType();
            if (pages.TryGetValue(pageType, out var tab))
            {
                tab.IsChecked = false;
            }
        }

        private async void svt_Click(object sender, RoutedEventArgs e)
        {
            var s = (Controls.SplitViewTab)sender;
            if (s.IsChecked)
                return;
            RootController.SwitchSplitView(false);
            await manager.NavigateAsync(tabs[s]);
        }

        private async void btn_UserInfo_Click(object sender, RoutedEventArgs e)
        {
            if (!(fm_inner.Content is InfoPage))
            {
                RootController.SwitchSplitView(false);
                await manager.NavigateAsync(typeof(InfoPage));
            }
        }

        private void tbtPaneBindBack(bool? value)
        {
            RootController.SwitchSplitView((bool)value);
        }

        private bool? tbtPaneBind(bool value)
        {
            return value;
        }

        private AcrylicBackgroundSource abPaneBackgroundBind(bool value, SplitViewDisplayMode mode)
        {
            if (mode == SplitViewDisplayMode.Overlay)
                return AcrylicBackgroundSource.Backdrop;
            return value ? AcrylicBackgroundSource.Backdrop : AcrylicBackgroundSource.HostBackdrop;
        }

        protected override void OnKeyUp(KeyRoutedEventArgs e)
        {
            base.OnKeyUp(e);
            e.Handled = true;
            switch (e.OriginalKey)
            {
            case Windows.System.VirtualKey.GamepadView:
                RootController.SwitchSplitView(null);
                break;
            default:
                e.Handled = false;
                break;
            }
        }
#if DEBUG_BOUNDS
        private const double NARROW_WIDE_WIDTH = 620;
#else
        private const double NARROW_WIDE_WIDTH = 720;
#endif

        protected override Size MeasureOverride(Size availableSize)
        {
            if (RootController.IsFullScreen || availableSize.Width < NARROW_WIDE_WIDTH)
            {
                if (sv_root.DisplayMode != SplitViewDisplayMode.Overlay)
                {
                    sv_root.DisplayMode = SplitViewDisplayMode.Overlay;
                    RootController.SetSplitViewButtonPlaceholderVisibility(true);
                }
            }
            else
            {
                if (sv_root.DisplayMode != SplitViewDisplayMode.CompactOverlay)
                {
                    sv_root.DisplayMode = SplitViewDisplayMode.CompactOverlay;
                    RootController.SetSplitViewButtonPlaceholderVisibility(false);
                }
            }
            return base.MeasureOverride(availableSize);
        }

        private double getPaneLength(Thickness visibleBounds, double offset)
        {
            return visibleBounds.Left + offset;
        }
    }
}
