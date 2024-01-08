﻿using FluentWeather.Abstraction.Interfaces.Weather;
using FluentWeather.Abstraction.Models;

namespace FluentWeather.OpenMeteoProvider.Models;

public class OpenMeteoWeatherNow : WeatherNowBase, IDew
{
    public int? DewPointTemperature { get; set; }
}