﻿using FluentWeather.Abstraction.Models;
using FluentWeather.Uwp.Shared;
using FluentWeather.Uwp.ViewModels;
using Microsoft.Toolkit.Uwp.UI;
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
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// https://go.microsoft.com/fwlink/?LinkId=234238 上介绍了“空白页”项模板

namespace FluentWeather.Uwp.Pages;
public sealed partial class MainPage : Page
{
    public MainPageViewModel ViewModel { get; set; } = new();
    public static MainPage Instance ;
    public MainPage()
    {
        this.InitializeComponent();
        this.DataContext = ViewModel;
        Instance = this;
        Common.LogManager.GetLogger("Application").Info("打开页面:主页");
    }

    private Visibility GetPrecipChartVisibility(PrecipitationBase precip)
    {
        var precipList = precip?.Precipitations;
        if (precipList is null) return Visibility.Collapsed;
        if (precipList.Count is 0) return Visibility.Collapsed;
        return precipList.Sum(p => p.Precipitation) == 0 ? Visibility.Collapsed : Visibility.Visible;
    }
}