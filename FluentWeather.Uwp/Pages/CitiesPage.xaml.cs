﻿using CommunityToolkit.Mvvm.ComponentModel;
using FluentWeather.Uwp.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Telerik.Core;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Foundation.Metadata;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;

// https://go.microsoft.com/fwlink/?LinkId=234238 上介绍了“空白页”项模板
namespace FluentWeather.Uwp.Pages;
public sealed partial class CitiesPage : Page
{
    public CitiesPageViewModel ViewModel { get; set; } = new(); 
    public CitiesPage()
    {
        this.InitializeComponent();
        this.DataContext = this;
        this.NavigationCacheMode = NavigationCacheMode.Required;
    }

    private void SettingsButton_Click(object sender, RoutedEventArgs e)
    {
        ((Frame)Parent)?.Navigate(typeof(SettingsPage),null);
    }

    private void CommandBar_Loaded(object sender, RoutedEventArgs e)
    {
        if (ApiInformation.IsPropertyPresent("Windows.UI.Xaml.Controls.CommandBar", "DefaultLabelPosition"))
        {
            BottomCommandBar.DefaultLabelPosition = CommandBarDefaultLabelPosition.Right;
        }
    }
}  