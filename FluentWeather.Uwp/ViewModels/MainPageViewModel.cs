﻿using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentWeather.Abstraction.Interfaces.Weather;
using FluentWeather.Abstraction.Interfaces.WeatherProvider;
using FluentWeather.Abstraction.Models;
using FluentWeather.DIContainer;
using FluentWeather.Tasks;
using FluentWeather.Uwp.Helpers;
using FluentWeather.Uwp.Shared;
using Microsoft.AppCenter.Analytics;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Telerik.Geospatial;
using Windows.ApplicationModel.Resources;

namespace FluentWeather.Uwp.ViewModels;

public sealed partial class MainPageViewModel : ObservableObject
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DailyForecasts7D))]
    private List<WeatherDailyBase> _dailyForecasts =new();
    public List<WeatherDailyBase> DailyForecasts7D =>(DailyForecasts.Count <7)? DailyForecasts.GetRange(0,DailyForecasts.Count) : DailyForecasts.GetRange(0, 7);

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HourlyForecasts24H))]
    private List<WeatherHourlyBase> _hourlyForecasts = new();
    public List<WeatherHourlyBase> HourlyForecasts24H => (HourlyForecasts.Count < 24) ? HourlyForecasts.GetRange(0, HourlyForecasts.Count) : HourlyForecasts.GetRange(0, 24);

    [ObservableProperty]
    private List<WeatherWarningBase> _warnings ;

    [ObservableProperty]
    private WeatherNowBase _weatherNow;

    [ObservableProperty]
    private string _weatherDescription;

    [ObservableProperty]
    private DateTime _sunRise;

    [ObservableProperty]
    private DateTime _sunSet;

    [ObservableProperty]
    private GeolocationBase _currentLocation;

    [ObservableProperty]
    private List<IndicesBase> _indices;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TotalPrecipitation))]
    [NotifyPropertyChangedFor(nameof(HasPrecipitation))]
    private PrecipitationBase _precipitation;

    [ObservableProperty]
    private AirConditionBase _airCondition;
    public double? TotalPrecipitation => Precipitation.Precipitations?.Sum(p => p.Precipitation);
    public bool HasPrecipitation => TotalPrecipitation > 0;
    public static MainPageViewModel Instance{ get; private set; }
    public MainPageViewModel()
    {
        Instance = this;
    }

    partial void OnCurrentLocationChanged(GeolocationBase oldValue, GeolocationBase newValue)
    {
        GetWeather(CurrentLocation);
    }
    public async Task GetDailyForecast(double lon, double lat)
    {
        var dailyProvider = Locator.ServiceProvider.GetService<IDailyForecastProvider>();
        DailyForecasts = await dailyProvider.GetDailyForecasts(lon, lat);
        if (DailyForecasts[0] is IAstronomic astronomic)
        {
            SunRise = astronomic.SunRise;
            SunSet = astronomic.SunSet;
        }
    }
    public async Task GetHourlyForecast(double lon,double lat)
    {
        var hourlyProvider = Locator.ServiceProvider.GetService<IHourlyForecastProvider>();
        HourlyForecasts = await hourlyProvider.GetHourlyForecasts(lon, lat);
    }
    public async Task GetWeatherNow(double lon, double lat)
    {
        IsLoading = true;
        var nowProvider = Locator.ServiceProvider.GetService<ICurrentWeatherProvider>();
        WeatherNow = await nowProvider.GetCurrentWeather(lon, lat);
        IsLoading = false;
    }
    public async Task GetWeatherWarnings(double lon, double lat)
    {
        var warningProvider = Locator.ServiceProvider.GetService<IWeatherWarningProvider>();
        Warnings = await warningProvider.GetWeatherWarnings(lon, lat);
    }
    public async Task GetIndices(double lon, double lat)
    {
        var indicesProvider = Locator.ServiceProvider.GetService<IIndicesProvider>();
        var indices = await indicesProvider.GetIndices(lon, lat);
        indices?.ForEach(p => p.Name = p.Name.Replace("指数", ""));
        Indices = indices;
    }
    public async Task GetWeatherPrecipitations(double lon, double lat)
    {
        var precipProvider = Locator.ServiceProvider.GetService<IPrecipitationProvider>();
        Precipitation = await precipProvider.GetPrecipitations(lon, lat);
    }
    public async Task GetAirCondition(double lon, double lat)
    {
        var airConditionProvider = Locator.ServiceProvider.GetService<IAirConditionProvider>();
        AirCondition = await airConditionProvider.GetAirCondition(lon, lat);
    }
    [RelayCommand]
    public async Task Refresh()
    {
        var lon = CurrentLocation.Longitude;
        var lat = CurrentLocation.Latitude;
        List<Task> tasks = new()
        {
            GetWeatherNow(lon, lat),
            GetWeatherWarnings(lon, lat),
            GetDailyForecast(lon, lat),
            GetHourlyForecast(lon, lat),
            GetWeatherPrecipitations(lon, lat),
            GetAirCondition(lon, lat),
            GetIndices(lon, lat),
        };
        await Task.WhenAll(tasks.ToArray());
        if (DailyForecasts[0] is ITemperatureRange currentTemperatureRange)
        {
            WeatherDescription = $"{WeatherNow.Description} {currentTemperatureRange.MinTemperature}° / {currentTemperatureRange.MaxTemperature}°";
        }
        if (CurrentLocation.Name == Common.Settings.DefaultGeolocation?.Name)
        {
            TileHelper.UpdateTiles(DailyForecasts);
        }
        foreach (var hourly in HourlyForecasts)
        {
            if (CurrentLocation.UtcOffset is not null)
            {
                hourly.Time += (TimeSpan)CurrentLocation.UtcOffset;
            }
            var daily = DailyForecasts.Find(p => p.Time.Date == hourly.Time.Date);
            if(daily is null) continue;
            daily.HourlyForecasts ??= new List<WeatherHourlyBase>();
            daily.HourlyForecasts?.Add(hourly);
        }
        CacheHelper.Cache(this);
    }
    public async void GetWeather(GeolocationBase geo)
    {
        if (Common.Settings.QWeatherToken is "" || Common.Settings.QGeolocationToken is "")
        {
            IsLoading = false;
            return;
        }
        var lon = geo.Longitude;
        var lat = geo.Latitude;
        var cacheData = await CacheHelper.GetWeatherCache(CurrentLocation);
        if (cacheData is not null)
        {
            DailyForecasts = cacheData.DailyForecasts;
            SunRise = cacheData.SunRise;
            SunSet = cacheData.SunSet;
            AirCondition = cacheData.AirCondition!;
            HourlyForecasts = cacheData.HourlyForecasts;
            Indices = cacheData.Indices!;
            Precipitation = cacheData.Precipitation!;
            Warnings = cacheData.Warnings!;
            WeatherNow = cacheData.WeatherNow;
            IsLoading = false;
        }
        else
        {
            await Refresh();
        }
        Analytics.TrackEvent("WeatherDataObtained", new Dictionary<string, string> { { "CityName", geo.Name } });
    }
    [RelayCommand]
    public void SpeechWeather()
    {
        var loader = ResourceLoader.GetForCurrentView();
        var text = $"{CurrentLocation.Name},{DailyForecasts[0].Description},{loader.GetString("HighestTemperature")}:{DailyForecasts[0].MaxTemperature}°,{loader.GetString("LowestTemperature")}:{DailyForecasts[0].MinTemperature}°";
        text += $",{loader.GetString("AirQuality")}:{AirCondition.AqiCategory}";
        if(!TTSHelper.IsPlaying)
        {
            InfoBarHelper.Info(loader.GetString("SpeechWeather"), text, 9000 , false);
        }
        TTSHelper.Speech(text);
    }
    [ObservableProperty]
    private bool _isLoading = true;
}
