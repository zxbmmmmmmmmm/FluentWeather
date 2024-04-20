﻿using CommunityToolkit.Common;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentWeather.Abstraction.Interfaces.GeolocationProvider;
using FluentWeather.Abstraction.Models;
using FluentWeather.DIContainer;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
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
public sealed partial class LocationDialog : ContentDialog
{
    public GeolocationBase Result { get; set; }
    public LocationDialogOptions? Options { get; set; }

    public LocationDialog(LocationDialogOptions? options = null)
    {
        this.InitializeComponent();
        _timeZones = TimeZoneInfo.GetSystemTimeZones();
        Options = options;
        if ((options & LocationDialogOptions.HideSearchLocation) is LocationDialogOptions.HideSearchLocation)
        {
            SearchLocationBox.Visibility = Visibility.Collapsed;
            ShowCustomLocationButton.Visibility = Visibility.Collapsed;
            CustomLocationPanel.Visibility = Visibility.Visible;
        }
        if ((options & LocationDialogOptions.HideCustomLocation) is LocationDialogOptions.HideCustomLocation)
        {
            CustomLocationPanel.Visibility = Visibility.Collapsed;
            ShowCustomLocationButton.Visibility = Visibility.Collapsed;
        }
        if ((options & LocationDialogOptions.HideCancelButton) is not LocationDialogOptions.HideCancelButton)
        {
            SecondaryButtonText = "关闭";
        }
    }

    [ObservableProperty]
    private string _query;

    [ObservableProperty]
    private ObservableCollection<GeolocationBase> _suggestedCities = new();

    [RelayCommand]
    public async Task FindCities()
    {
        SuggestedCities.Clear();
        var service = Locator.ServiceProvider.GetService<IGeolocationProvider>();
        var result = await service.GetCitiesGeolocationByName(Query);
        result?.ForEach(SuggestedCities.Add);
    }

    [RelayCommand]
    private void SelectSuggestedCities(GeolocationBase location)
    {
        Query = location.Name;
        Name = location.Name;
        Latitude = location.Location.Latitude.ToString(CultureInfo.InvariantCulture);
        Longitude = location.Location.Longitude.ToString(CultureInfo.InvariantCulture);
        AdmDistrict = location.AdmDistrict;
        AdmDistrict2 = location.AdmDistrict2;
        Country = location.Country;
        if (location.TimeZone is not null)
        {
            TimeZone = TimeZoneInfo.FindSystemTimeZoneById(location.TimeZone);
        }
        else if(location.UtcOffset is not null)
        {
            TimeZone = TimeZones.First(p => p.BaseUtcOffset == location.UtcOffset);
        }
        else
        {
            TimeZone = GetTimeZoneFromLocation(location.Location.Longitude);
        }
        IsDaylightSavingTime = location.IsDaylightSavingTime;
    }

    private bool CanContinue
    {
        get
        {
            if (Name is null or "") return false;
            if (Latitude is null or ""||!Latitude.IsDecimal()) return false;
            if (Longitude is null or "" || !Longitude.IsDecimal()) return false;
            if (_timeZone is null) return false;
            return true;
        }
    }
    [RelayCommand]
    public void Continue()
    {
        if (double.Parse(Latitude) > 90 || double.Parse(Latitude)<-90)
        {
            Latitude = "";
            return;
        }
        Result = new GeolocationBase()
        {
            Location = new(double.Parse(Latitude), double.Parse(Longitude)),
            Country = Country,
            AdmDistrict = AdmDistrict,
            AdmDistrict2 = AdmDistrict2,
            Name = Name,
            UtcOffset = TimeZone.BaseUtcOffset,
            TimeZone = TimeZone.Id
        };
        Hide();
    }
    private TimeZoneInfo GetTimeZoneFromLocation(double longitude)
    {
        var timeZone = 0;

        var shangValue = (int)(longitude / 15);
        var yushuValue = Math.Abs(longitude % 15);
        if (yushuValue <= 7.5)
        {
            timeZone = shangValue;
        }
        else
        {
            timeZone = shangValue + (longitude > 0 ? 1 : -1);
        }

        return TimeZones.First(p => p.BaseUtcOffset == TimeSpan.FromHours(1)* timeZone);
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanContinue))]
    private string _name;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanContinue))]
    private string _latitude;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanContinue))]
    private string _longitude;

    [ObservableProperty]
    private string _admDistrict;

    [ObservableProperty]
    private string _admDistrict2;

    [ObservableProperty]
    private string _country;
    
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanContinue))]
    private TimeZoneInfo _timeZone;

    [ObservableProperty]
    private IList<TimeZoneInfo> _timeZones;

    [ObservableProperty]
    private bool? _isDaylightSavingTime;

    private void ShowCustomLocationButton_Click(object sender, RoutedEventArgs e)
    {
        CustomLocationPanel.Visibility = Visibility.Visible;
        ShowCustomLocationButton.Visibility = Visibility.Collapsed;
    }

    private void ContentDialog_Closing(ContentDialog sender, ContentDialogClosingEventArgs args)
    {
        var isCancelButtonHidden = (Options & LocationDialogOptions.HideCancelButton) is LocationDialogOptions.HideCancelButton;
        if (!CanContinue && isCancelButtonHidden)
        {
            args.Cancel = true;
        }
    }
}

[Flags]
public enum LocationDialogOptions
{
    /// <summary>
    /// 隐藏搜索位置
    /// </summary>
    HideSearchLocation = 1,

    /// <summary>
    /// 隐藏自定义位置
    /// </summary>
    HideCustomLocation = 2,

    /// <summary>
    /// 隐藏关闭按钮
    /// </summary>
    HideCancelButton = 4,
}