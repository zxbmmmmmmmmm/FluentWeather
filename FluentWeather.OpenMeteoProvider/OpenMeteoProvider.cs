﻿using FluentWeather.Abstraction;
using FluentWeather.Abstraction.Interfaces.Setting;
using FluentWeather.Abstraction.Interfaces.WeatherProvider;
using FluentWeather.Abstraction.Models;
using FluentWeather.DIContainer;
using FluentWeather.OpenMeteoProvider.Mappers;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;
using OpenMeteoApi;
using System.Collections.Generic;
using FluentWeather.OpenMeteoProvider.Models;
using OpenMeteoApi.Variables;

namespace FluentWeather.OpenMeteoProvider;

public class OpenMeteoProvider : ProviderBase, ICurrentWeatherProvider, IAirConditionProvider, IDailyForecastProvider, IHourlyForecastProvider
{
    public override string Name => "OpenMeteo";
    public override string Id => "open-meteo";

    private readonly OpenMeteoClient _client = new();
    public async Task<WeatherNowBase> GetCurrentWeather(double lon, double lat)
    {
        var result = await _client.GetWeatherForecastData(lat, lon, currentVariables: CurrentVariables.All, hourlyVariables:new[]{ HourlyVariables.Visibility, "dew_point_2m" });
        var now = result.CurrentWeather!.MapToOpenMeteoWeatherNow();
        now.DewPointTemperature = (int)result.HourlyForecast?.DewPoint2m?[0]!;
        now.Visibility = (int)(result.HourlyForecast?.Visibility?[0]!/1000);
        return now;
    }
    public async Task<AirConditionBase> GetAirCondition(double lon, double lat)
    {
        var result = await _client.GetCurrentAirQuality(lat, lon);
        return result.MapToOpenMeteoWeatherNow();
    }

    public static void RegisterRequiredServices()
    {
        Locator.ServiceDescriptors.AddSingleton(typeof(ICurrentWeatherProvider), typeof(OpenMeteoProvider));
        Locator.ServiceDescriptors.AddSingleton(typeof(IAirConditionProvider), typeof(OpenMeteoProvider));
        Locator.ServiceDescriptors.AddSingleton(typeof(IDailyForecastProvider), typeof(OpenMeteoProvider));
        Locator.ServiceDescriptors.AddSingleton(typeof(IHourlyForecastProvider), typeof(OpenMeteoProvider));
    }

    public async Task<List<WeatherDailyBase>> GetDailyForecasts(double lon, double lat)
    {
        var result = await _client.GetDailyForecasts(lat, lon);
        return result.ConvertAll<WeatherDailyBase>(p => p.MapToOpenMeteoDailyForecast());
    }

    public async Task<List<WeatherHourlyBase>> GetHourlyForecasts(double lon, double lat)
    {
        var result = await _client.GetHourlyForecasts(lat, lon);
        return result.ConvertAll<WeatherHourlyBase>(p => p.MapToOpenMeteoHourlyForecast());
    }

}