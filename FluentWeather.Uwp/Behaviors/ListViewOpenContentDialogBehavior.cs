﻿using FluentWeather.Uwp.Controls.Popups;
using Microsoft.Xaml.Interactivity;
using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;

namespace FluentWeather.Uwp.Behaviors;

public class ListViewOpenContentDialogBehavior:Behavior<ListViewBase>
{
    protected override void OnAttached()
    {
        base.OnAttached();
        AssociatedObject.ItemClick += ListItemClicked;
    }

    public Type DialogType
    {
        get => (Type)GetValue(DialogTypeProperty);
        set => SetValue(DialogTypeProperty, value);
    }

    public static readonly DependencyProperty DialogTypeProperty =
        DependencyProperty.Register(nameof(DialogType), typeof(Type), typeof(ListViewOpenContentDialogBehavior), new PropertyMetadata(typeof(Page)));

    public bool UseArguments
    {
        get => (bool)GetValue(UseArgumentsProperty);
        set => SetValue(UseArgumentsProperty, value);
    }

    public static readonly DependencyProperty UseArgumentsProperty =
        DependencyProperty.Register(nameof(UseArguments), typeof(bool), typeof(ListViewOpenContentDialogBehavior), new PropertyMetadata(false));



    protected override void OnDetaching()
    {
        base.OnDetaching();
        AssociatedObject.ItemClick -= ListItemClicked;
    }
    private async void ListItemClicked(object sender, ItemClickEventArgs e)
    {

        if(UseArguments)
        {
            var dialog = Activator.CreateInstance(DialogType,AssociatedObject,e.ClickedItem) as ContentDialog;
            await dialog?.ShowAsync();
        }
        else
        {
            var dialog = Activator.CreateInstance(DialogType) as ContentDialog;
            await dialog?.ShowAsync();
        }
    }

}