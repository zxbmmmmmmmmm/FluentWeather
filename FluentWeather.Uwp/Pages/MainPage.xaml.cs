﻿using FluentWeather.Abstraction.Models;
using FluentWeather.Uwp.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Composition;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Hosting;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Microsoft.Graphics.Canvas.Effects;
using System.Threading.Tasks;
using Microsoft.AppCenter.Analytics;
using FluentWeather.Uwp.Helpers;
using Windows.Storage;
using CommunityToolkit.WinUI;
using Windows.System.RemoteSystems;
using FluentWeather.Uwp.Shared;

// https://go.microsoft.com/fwlink/?LinkId=234238 上介绍了“空白页”项模板

namespace FluentWeather.Uwp.Pages;
public sealed partial class MainPage : Page
{
    public MainPageViewModel ViewModel { get; set; } = new();
    public static MainPage Instance ;
    private XamlRenderService _xamlRenderer = new XamlRenderService();

    private ListViewBase _dailyItemsView;
    private DailyViewPage _dailyViewPage;
    private FrameworkElement _mainContentContainer;

    private RootPage _rootPage = RootPage.Instance;

    public MainPage()
    {
        this.DataContext = ViewModel;
        _xamlRenderer.DataContext = ViewModel;
        Instance = this;
        LoadElements();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _dailyItemsView = FindChildControl<ListViewBase>(this.Content, "DailyItemsView");
        _mainContentContainer = FindChildControl<FrameworkElement>(this.Content, "MainContentContainer");

        _dailyViewPage = this.FindChild<DailyViewPage>();
        if (_dailyItemsView is null) return;
        _dailyItemsView.ItemClick += DailyItemClicked;
        MainPageViewModel.Instance.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName is not "CurrentGeolocation" || _dailyViewPage is null) return;
            _mainContentContainer.Visibility = Visibility.Visible;
        };
    }

    private void DailyItemClicked(object sender, ItemClickEventArgs e)
    {
        _dailyViewPage.SelectedIndex = _dailyItemsView.IndexFromContainer(_dailyItemsView.ContainerFromItem(e.ClickedItem));
        _mainContentContainer.Visibility = Visibility.Collapsed;
        Analytics.TrackEvent("DailyViewEntered");
    }

    private Visibility GetPrecipChartVisibility(PrecipitationBase precip)
    {
        var precipList = precip?.Precipitations;
        if (precipList is null) return Visibility.Collapsed;
        if (precipList.Count is 0) return Visibility.Collapsed;
        return precipList.Sum(p => p.Precipitation) == 0 ? Visibility.Collapsed : Visibility.Visible;
    }

    private async void LoadElements()
    {
        if (Common.Settings.EnableCustomPage)
        {
            var content = await GetCustomContent();
            if (content is null)
            {
                this.InitializeComponent();
            }
            else
            {
                this.Content = content;
            }
        }
        else
        {
            this.InitializeComponent();
        }

        ((FrameworkElement)this.Content).Loaded += OnLoaded;
    }

    

    private T FindChildControl<T>(DependencyObject control, string ctrlName) where T: DependencyObject
    {
        int childNumber = VisualTreeHelper.GetChildrenCount(control);
        for (int i = 0; i < childNumber; i++)
        {
            DependencyObject child = VisualTreeHelper.GetChild(control, i);
            FrameworkElement fe = child as FrameworkElement;
            // Not a framework element or is null
            if (fe == null) return null;

            if (child is T && fe.Name == ctrlName)
            {
                // Found the control so return
                return (T)child;
            }
            else
            {
                // Not found it - search children
                T nextLevel = FindChildControl<T>(child, ctrlName);
                if (nextLevel != null)
                    return nextLevel;
            }
        }
        return null;
    }


    private async Task<UIElement> GetCustomContent()
    {
        StorageFolder folder;
        try
        {
            folder = await ApplicationData.Current.LocalFolder.GetFolderAsync("CustomPages");
        }
        catch
        {
            folder = await ApplicationData.Current.LocalFolder.CreateFolderAsync("Backgrounds");
        }
        try
        {
            var file = await folder.GetFileAsync("MainPage.xaml");
            var text = await FileIO.ReadTextAsync(file);
            return _xamlRenderer.Render(text);
        }
        catch
        {
            return null;
        }
    }
}