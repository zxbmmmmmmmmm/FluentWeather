﻿using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentWeather.DIContainer;
using FluentWeather.Uwp.Helpers.Analytics;
using FluentWeather.Uwp.Shared;
using Microsoft.AppCenter;
using Microsoft.AppCenter.Analytics;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// https://go.microsoft.com/fwlink/?LinkId=234238 上介绍了“内容对话框”项模板

namespace FluentWeather.Uwp.Controls.Dialogs;

[ObservableObject]
public sealed partial class AboutDialog : ContentDialog
{
    public AboutDialog()
    {
        this.InitializeComponent();
        Locator.ServiceProvider.GetService<AppAnalyticsService>()?.TrackAboutOpened();
    }
    [RelayCommand]
    public void Close()
    {
        Hide();
    }
    [RelayCommand]
    public void EnableDeveloperMode()
    {
        Locator.ServiceProvider.GetService<AppAnalyticsService>()?.TrackDeveloperModeEnabled();
        Common.Settings.DeveloperMode = true;
    }
}