﻿using FluentWeather.Abstraction.Interfaces.Weather;
using FluentWeather.Abstraction.Models;
using Microsoft.Toolkit.Uwp.Notifications;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;
using Windows.Storage;
using Windows.UI.Notifications;
using static FluentWeather.Uwp.Shared.TileHelper;
using static FluentWeather.Uwp.Shared.Common;
using System.Text.RegularExpressions;
using FluentWeather.Uwp.QWeatherProvider;
using FluentWeather.Abstraction;
using FluentWeather.OpenMeteoProvider;
using FluentWeather.Abstraction.Interfaces.WeatherProvider;
using Windows.ApplicationModel.Resources;

namespace FluentWeather.Tasks
{
    public sealed class NotifyTask :IBackgroundTask
    {
        private IWeatherWarningProvider _warningProvider;
        private IDailyForecastProvider _dailyForecastProvider;

        public async void Run(IBackgroundTaskInstance taskInstance)
        {
            BackgroundTaskDeferral deferral = taskInstance.GetDeferral();

            LogManager.GetLogger(nameof(NotifyTask)).Info("NotifyTask Started");

            if(Settings.ProviderConfig is Uwp.Shared.ProviderConfig.QWeather)
            {
                var provider = new QWeatherProvider(Settings.QWeatherToken, Settings.QWeatherDomain, null, Settings.QWeatherPublicId);
                _warningProvider = provider;
                _dailyForecastProvider = provider;
            }
            else
            {
                var provider = new OpenMeteoProvider.OpenMeteoProvider();
                _dailyForecastProvider = provider;
            }
            var lat = Settings.Latitude;
            var lon = Settings.Longitude;
            if(lat is -1 || lon is -1)
            {
                deferral.Complete();
                return;
            }

            await PushDaily(lon,lat);
            await PushWarnings(lon, lat);


            deferral.Complete();
        }
        private async Task PushWarnings(double lon, double lat)
        {
            if (_warningProvider is null) return;
            var settingContainer = ApplicationData.Current.LocalSettings;
            var isWarningNotificationEnabled = Settings.IsWarningNotificationEnabled;
            if (!isWarningNotificationEnabled) return;
            var warnings = await _warningProvider.GetWeatherWarnings(lon, lat);
            settingContainer.Values["PushedWarnings"] ??= JsonSerializer.Serialize(new Dictionary<string,DateTime>());
            var pushed = JsonSerializer.Deserialize<Dictionary<string, DateTime>>((string)settingContainer.Values["PushedWarnings"]);
            foreach (var warning in warnings)
            {
                if (pushed.ContainsKey(warning.Id)) continue; //未被推送过
                if (Settings.IgnoreWarningWords != "" && Regex.IsMatch(warning.Title,Settings.IgnoreWarningWords)) continue;//匹配屏蔽词
                var toast = new ToastContentBuilder()
                    .AddText(warning.Title)
                    .AddText(warning.Description)
                    .AddAttributionText(warning.Sender);
                toast.Show();
                pushed.Add(warning.Id,warning.PublishTime);
            }
            settingContainer.Values["PushedWarnings"] = JsonSerializer.Serialize(pushed);
        }
        private async Task PushDaily(double lon, double lat)
        {
            if (_dailyForecastProvider is null) return;
            var isPushTodayAvailable = Settings.IsDailyNotificationEnabled && Settings.LastPushedTime != DateTime.Now.Date.DayOfYear;
            var isPushTomorrowAvailable = Settings.IsTomorrowNotificationEnabled && Settings.LastPushedTimeTomorrow != DateTime.Now.Date.DayOfYear;
            var isTileAvailable = Settings.IsDailyNotificationTileEnabled && Settings.LastPushedTime != DateTime.Now.Date.DayOfYear;
            if (!isPushTodayAvailable && !isPushTomorrowAvailable && !isTileAvailable) return;
            var data = await _dailyForecastProvider.GetDailyForecasts(lon, lat);
            if (isTileAvailable)
            {
                UpdateTiles(data);
                LogManager.GetLogger(nameof(NotifyTask)).Info("Tile Updated");
            }
            if (DateTime.Now.Hour < 18)
            {
                if (!isPushTodayAvailable) return;
                PushToday(data);
                Settings.LastPushedTime = DateTime.Now.Date.DayOfYear;
            }
            else
            {
                if (!isPushTomorrowAvailable) return;
                PushTomorrow(data);
                Settings.LastPushedTimeTomorrow = DateTime.Now.Date.DayOfYear;
            }
        }
        private void PushToday(List<WeatherDailyBase> data)
        {
            var trimmed = (data.Count >= 7) ? data.GetRange(0, 7) : data;
            var group = new AdaptiveGroup();
            GetGroupChildren(group, trimmed);
            var builder = new ToastContentBuilder()
                .AddHeroImage(new Uri("ms-appx:///Assets/Backgrounds/" + trimmed[0].WeatherType +".png"))
                .AddAttributionText(ResourceLoader.GetForCurrentView().GetString("ToadyWeather"))
                .AddText($"{trimmed[0].Description}  {ResourceLoader.GetForCurrentView().GetString("HighestTemperature")}{((ITemperatureRange)trimmed[0]).MaxTemperature}°,{ResourceLoader.GetForCurrentView().GetString("LowestTemperature")}{((ITemperatureRange)trimmed[0]).MinTemperature}°")
                .AddVisualChild(group);
            builder.Show();
            LogManager.GetLogger(nameof(NotifyTask)).Info("Notification Pushed(Today)");
        }
        private void PushTomorrow(List<WeatherDailyBase> data)
        {
            var trimmed = (data.Count >= 7) ? data.GetRange(1, 6) : data;
            var group = new AdaptiveGroup();
            GetGroupChildren(group, trimmed);
            var builder = new ToastContentBuilder()
                .AddHeroImage(new Uri("ms-appx:///Assets/Backgrounds/" + trimmed[0].WeatherType + ".png"))
                .AddAttributionText(ResourceLoader.GetForCurrentView().GetString("TomorrowWeather"))
                .AddText($"{trimmed[0].Description}  {ResourceLoader.GetForCurrentView().GetString("HighestTemperature")}{((ITemperatureRange)trimmed[0]).MaxTemperature}°,{ResourceLoader.GetForCurrentView().GetString("LowestTemperature")}{((ITemperatureRange)trimmed[0]).MinTemperature}°")
                .AddVisualChild(group);
            builder.Show();
            LogManager.GetLogger(nameof(NotifyTask)).Info("Notification Pushed(Tomorrow)");

        }

    }
}