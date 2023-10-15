﻿using FluentWeather.Abstraction.Interfaces.Helpers;
using FluentWeather.DIContainer;
using FluentWeather.Uwp.Pages;
using FluentWeather.Uwp.Shared;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Threading.Tasks;
using Windows.Devices.Geolocation;
using Windows.UI.Xaml;
using FluentWeather.Abstraction.Interfaces.GeolocationProvider;
using FluentWeather.Abstraction.Models;
using FluentWeather.Uwp.Controls.Dialogs;
using FluentWeather.Uwp.Shared;

namespace FluentWeather.Uwp.Helpers;

public class LocationHelper
{
    /// <summary>
    /// g
    /// </summary>
    /// <returns></returns>
    public static async Task<(double lon,double lat)> UpdatePosition()
    {
        var accessStatus = await Geolocator.RequestAccessAsync();
        switch (accessStatus)
        {
            case GeolocationAccessStatus.Allowed:
                try
                {
                    Common.LogManager.GetLogger("Application").Info("启动定位");
                    Geolocator geolocator = new Geolocator { DesiredAccuracyInMeters = 100 };
                    Geoposition pos = await geolocator.GetGeopositionAsync();
                    return (pos.Coordinate.Point.Position.Longitude, pos.Coordinate.Point.Position.Latitude);
                }
                catch(Exception e)
                {
                    Common.LogManager.GetLogger("Application").Info("定位失败:"+ e.Message);
                    return (-1, -1);
                }

            case GeolocationAccessStatus.Unspecified:
            case GeolocationAccessStatus.Denied:
            default:
                return (-1, -1);
        }
    }
    public static async Task<GeolocationBase> GetGeolocation()
    {
        //尝试获取位置
        //获取失败且默认位置未设置:弹出对话框，将默认位置作为当前位置
        //获取失败但默认位置已设置：将默认位置作为当前位置
        //获取成功:设置位置，并将当前位置设置为默认位置

        var service = Locator.ServiceProvider.GetService<IGeolocationProvider>();
        if (Common.Settings.DefaultGeolocation.Name is null)//默认位置未设置
        {
            var (lon, lat) = await LocationHelper.UpdatePosition();
            if (lon is -1 || lat is -1)//获取位置失败
            {
                await new SetLocationDialog().ShowAsync();
                return Common.Settings.DefaultGeolocation;
            }
            var city = await service.GetCitiesGeolocationByLocation(lon, lat);
            if (city.Count is 0)//根据经纬度获取城市失败
            {
                await new SetLocationDialog().ShowAsync();
                return Common.Settings.DefaultGeolocation;
            }
            return city.First();
        }

        if (!Common.Settings.UpdateLocationOnStartup)//不更新位置
            return Common.Settings.DefaultGeolocation;

        //默认位置已设置但需要更新位置
        var (lo, la) = await LocationHelper.UpdatePosition();
        if (lo is -1 || la is -1)//检查失败
        {
            return Common.Settings.DefaultGeolocation;
        }
        var c = await service.GetCitiesGeolocationByLocation(lo, la);
        return c.Count is 0 ? Common.Settings.DefaultGeolocation : c.First();//若定位失败仍然使用默认位置
    }

}