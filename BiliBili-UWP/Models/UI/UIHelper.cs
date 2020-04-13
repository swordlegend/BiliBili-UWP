﻿using BiliBili_Lib.Enums;
using BiliBili_Lib.Tools;
using BiliBili_UWP.Models.Enums;
using BiliBili_UWP.Models.UI.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;

namespace BiliBili_UWP.Models.UI
{
    public class UIHelper
    {
        /// <summary>
        /// 初始化标题栏
        /// </summary>
        public static void SetTitleBarColor()
        {
            var view = ApplicationView.GetForCurrentView();
            CoreApplication.GetCurrentView().TitleBar.ExtendViewIntoTitleBar = true;
            var Theme = AppTool.GetLocalSetting(Settings.Theme, "Light");
            if (Theme == "Dark")
            {
                // active
                view.TitleBar.BackgroundColor = Colors.Transparent;
                view.TitleBar.ForegroundColor = Colors.White;

                // inactive
                view.TitleBar.InactiveBackgroundColor = Colors.Transparent;
                view.TitleBar.InactiveForegroundColor = Colors.Gray;
                // button
                view.TitleBar.ButtonBackgroundColor = Colors.Transparent;
                view.TitleBar.ButtonForegroundColor = Colors.White;

                view.TitleBar.ButtonHoverBackgroundColor = Color.FromArgb(255, 33, 42, 67);
                view.TitleBar.ButtonHoverForegroundColor = Colors.White;

                view.TitleBar.ButtonPressedBackgroundColor = Color.FromArgb(255, 255, 86, 86);
                view.TitleBar.ButtonPressedForegroundColor = Colors.White;

                view.TitleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
                view.TitleBar.ButtonInactiveForegroundColor = Colors.Gray;
            }
            else
            {
                // active
                view.TitleBar.BackgroundColor = Colors.Transparent;
                view.TitleBar.ForegroundColor = Colors.Black;

                // inactive
                view.TitleBar.InactiveBackgroundColor = Colors.Transparent;
                view.TitleBar.InactiveForegroundColor = Colors.Gray;
                // button
                view.TitleBar.ButtonBackgroundColor = Colors.Transparent;
                view.TitleBar.ButtonForegroundColor = Colors.DarkGray;

                view.TitleBar.ButtonHoverBackgroundColor = Color.FromArgb(255, 63, 63, 63);
                view.TitleBar.ButtonHoverForegroundColor = Colors.DarkGray;

                view.TitleBar.ButtonPressedBackgroundColor = Color.FromArgb(255, 63, 63, 63);
                view.TitleBar.ButtonPressedForegroundColor = Colors.DarkGray;

                view.TitleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
                view.TitleBar.ButtonInactiveForegroundColor = Colors.Gray;
            }
        }
        /// <summary>
        /// 获取预先定义的线性画笔资源
        /// </summary>
        /// <param name="key">键</param>
        /// <returns></returns>
        public static Brush GetThemeBrush(ColorType key)
        {
            return (Brush)Application.Current.Resources[key.ToString()];
        }
        /// <summary>
        /// 获取预先定义的字体资源
        /// </summary>
        /// <param name="key">键</param>
        /// <returns></returns>
        public static FontFamily GetFontFamily(string key)
        {
            return (FontFamily)Application.Current.Resources[key];
        }
        /// <summary>
        /// 设置预定义的字体资源
        /// </summary>
        /// <param name="key">键</param>
        /// <returns></returns>
        public static void SetFontFamily(string key,FontFamily font)
        {
            Application.Current.Resources.Add(key, font);
        }
        public static Storyboard GetPopupStoryboard(bool isPopin = true)
        {
            var board = new Storyboard();
            var containerOpacityAni = new DoubleAnimation();
            var backgroundOpacityAni = new DoubleAnimation();
            var containerTransformAni = new DoubleAnimation();

            containerOpacityAni.From = isPopin ? 0f : 1f;
            containerOpacityAni.To = isPopin ? 1f : 0f;
            containerOpacityAni.Duration = new Duration(TimeSpan.FromSeconds(0.5));
            Storyboard.SetTargetName(containerOpacityAni, "PopupContainer");
            Storyboard.SetTargetProperty(containerOpacityAni, "Opacity");

            backgroundOpacityAni.From = isPopin ? 0f : 1f;
            backgroundOpacityAni.To = isPopin ? 1f : 0f;
            backgroundOpacityAni.Duration = new Duration(TimeSpan.FromSeconds(0.5));
            Storyboard.SetTargetName(backgroundOpacityAni, "PopupBackground");
            Storyboard.SetTargetProperty(backgroundOpacityAni, "Opacity");

            containerTransformAni.From = isPopin ? -20f : 0f;
            containerTransformAni.To = isPopin ? 0f : -20f;
            containerTransformAni.Duration = new Duration(TimeSpan.FromSeconds(0.5));
            Storyboard.SetTargetName(containerTransformAni, "PopupContainer");
            Storyboard.SetTargetProperty(containerTransformAni, "(UIElement.RenderTransform).(TranslateTransform.Y)");

            board.Children.Add(containerOpacityAni);
            board.Children.Add(backgroundOpacityAni);
            board.Children.Add(containerTransformAni);
            return board;
        }
        public static void PopupInit(IAppPopup popup)
        {
            popup._popup = new Popup();
            popup._popupId = Guid.NewGuid();
            popup._popup.Child = popup as UIElement;
        }
        public static void PopupShow(IAppPopup popup)
        {
            App.AppViewModel.WindowsSizeChangedNotify.Add(new Tuple<Guid, Action<Size>>(popup._popupId, (rect) =>
            {
                popup.Width = rect.Width;
                popup.Height = rect.Height;
            }));
            popup.Width = Window.Current.Bounds.Width;
            popup.Height = Window.Current.Bounds.Height;
            popup._popup.IsOpen = true;
        }
    }
}
