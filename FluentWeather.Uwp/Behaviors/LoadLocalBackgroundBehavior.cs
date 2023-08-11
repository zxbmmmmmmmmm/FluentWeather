﻿using System;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.UI.Xaml;
using FluentWeather.Abstraction.Models;
using Microsoft.Toolkit.Uwp.UI.Controls;
using Microsoft.Xaml.Interactivity;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;
using Windows.Storage.Streams;

namespace FluentWeather.Uwp.Behaviors;

public class LoadLocalBackgroundBehavior:Behavior<ImageEx>
{
    public static LoadLocalBackgroundBehavior Instance;
    public LoadLocalBackgroundBehavior()
    {
        Instance = this;
    }
    public WeatherType WeatherType
    {
        get => (WeatherType)GetValue(WeatherTypeProperty);
        set => SetValue(WeatherTypeProperty, value);
    }
    public static readonly DependencyProperty WeatherTypeProperty =
        DependencyProperty.Register(nameof(WeatherType), typeof(WeatherType), 
            typeof(LoadLocalBackgroundBehavior), 
            new PropertyMetadata(WeatherType.Unknown,WeatherTypeUpdated));
    private static void WeatherTypeUpdated(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var b = d as LoadLocalBackgroundBehavior;
        if (b is null) return;
        if (b._weatherType == b.WeatherType) return;//天气类型相同时不更新
        b.LoadImage();
        b._weatherType = b.WeatherType;
    }
    protected override void OnAttached()
    {
        base.OnAttached();
    }
    protected override void OnDetaching()
    {
        base.OnDetaching();
    }
    private WeatherType _weatherType;
    public async void LoadImage()
    {
        var item = await ApplicationData.Current.LocalFolder.TryGetItemAsync("Backgrounds");
        if (item is not StorageFolder folder) return;
        var image = await GetImage(folder, WeatherType.ToString());
        image ??= await GetImage(folder, "All");
        AssociatedObject.Source = image;
    }

    private async Task<BitmapImage> GetImage(StorageFolder folder,string name)
    {
        var item  = await folder.TryGetItemAsync(name +".png");
        if (item is not StorageFile file) return null;
        using var ir = await file.OpenAsync(FileAccessMode.Read);
        var image = new BitmapImage();
        await image.SetSourceAsync(ir);
        return image;
    }
}