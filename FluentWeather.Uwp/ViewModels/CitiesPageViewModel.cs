﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentWeather.Abstraction.Interfaces.GeolocationProvider;
using FluentWeather.Abstraction.Interfaces.Helpers;
using FluentWeather.Abstraction.Interfaces.Setting;
using FluentWeather.Abstraction.Models;
using FluentWeather.DIContainer;
using FluentWeather.Uwp.Controls.Dialogs;
using FluentWeather.Uwp.Helpers;
using Microsoft.AppCenter.Ingestion.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Toolkit.Uwp.UI.Controls.TextToolbarSymbols;
using Windows.UI.Xaml;
using FluentWeather.Uwp.Shared;
using Microsoft.AppCenter.Analytics;

namespace FluentWeather.Uwp.ViewModels;

public partial class CitiesPageViewModel:ObservableObject
{
    [ObservableProperty]
    private ObservableCollection<GeolocationBase> _cities;
    [ObservableProperty]
    private GeolocationBase _currentCity;
    [ObservableProperty]
    private string _query;
    [ObservableProperty]
    private List<GeolocationBase> _suggestedCities;
    public static CitiesPageViewModel Instance { get; private set; }
    public CitiesPageViewModel()
    {
        Cities = Common.Settings.SavedCities;
        Cities.CollectionChanged += (_, _) => Common.Settings.SavedCities = Cities;
        Instance = this;
        GetCurrentCity();
    }

    [RelayCommand]
    public async Task GetCities(string name)
    {
        var service = Locator.ServiceProvider.GetService<IGeolocationProvider>();
        SuggestedCities = await service.GetCitiesGeolocationByName(name);
    }

    [RelayCommand]
    public void SaveCity(GeolocationBase city)
    {
        Cities.Add(city);
        Query = city.Name;
        Analytics.TrackEvent("CitySaved", new Dictionary<string, string> { { "CityName", city.Name } });
    }
    [RelayCommand]
    public void DeleteCity(GeolocationBase item)
    {
        Cities.Remove(item);
    }

    public async void GetCurrentCity()
    {
        if (Common.Settings.QWeatherToken is "" || Common.Settings.QGeolocationToken is "")
            return;
        var location = await LocationHelper.GetGeolocation();
        if (Common.Settings.DefaultGeolocation.Name is null)
            Common.Settings.DefaultGeolocation = location;
        Common.Settings.Latitude = location.Latitude;
        Common.Settings.Longitude = location.Longitude;
        CitiesPageViewModel.Instance.CurrentCity = location;
        MainPageViewModel.Instance.CurrentLocation = location;
    }


}