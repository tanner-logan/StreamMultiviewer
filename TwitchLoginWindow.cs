using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;

namespace StreamMultiviewer;

public sealed class TwitchLoginWindow : Window
{
    private readonly CoreWebView2Environment _environment;
    private readonly WebView2 _webView = new();
    private readonly TextBlock _status = new()
    {
        VerticalAlignment = VerticalAlignment.Center,
        Foreground = new SolidColorBrush(Color.FromRgb(196, 205, 219)),
        Text = "Sign in here. This app session is saved for future Twitch mode launches."
    };

    public TwitchLoginWindow(CoreWebView2Environment environment)
    {
        _environment = environment;
        Title = "Sign in to Twitch - Stream Multiviewer";
        Icon = Application.Current.MainWindow?.Icon;
        Width = 1000;
        Height = 760;
        MinWidth = 700;
        MinHeight = 500;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Background = new SolidColorBrush(Color.FromRgb(16, 19, 26));

        var root = new Grid();
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        var header = new DockPanel
        {
            LastChildFill = true,
            Margin = new Thickness(18, 12, 18, 12)
        };
        var continueButton = new Button
        {
            Content = "Continue to Twitch setup",
            Padding = new Thickness(14, 8, 14, 8),
            Margin = new Thickness(16, 0, 0, 0),
            Background = new SolidColorBrush(Color.FromRgb(71, 120, 232)),
            Foreground = Brushes.White,
            BorderThickness = new Thickness(0),
            VerticalAlignment = VerticalAlignment.Center
        };
        continueButton.Click += (_, _) => Close();
        DockPanel.SetDock(continueButton, Dock.Right);
        header.Children.Add(continueButton);

        var instructions = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        instructions.Children.Add(new TextBlock
        {
            Text = "Twitch sign-in",
            Foreground = Brushes.White,
            FontSize = 17,
            FontWeight = FontWeights.SemiBold
        });
        instructions.Children.Add(_status);
        header.Children.Add(instructions);

        Grid.SetRow(header, 0);
        Grid.SetRow(_webView, 1);
        root.Children.Add(header);
        root.Children.Add(_webView);
        Content = root;

        Loaded += LoginWindow_Loaded;
    }

    private async void LoginWindow_Loaded(object sender, RoutedEventArgs e)
    {
        Loaded -= LoginWindow_Loaded;
        try
        {
            await _webView.EnsureCoreWebView2Async(_environment);
            _webView.NavigationCompleted += (_, args) =>
            {
                if (!args.IsSuccess)
                {
                    _status.Text = $"Twitch sign-in page failed to load: {args.WebErrorStatus}";
                }
            };
            _webView.Source = new Uri("https://www.twitch.tv/login");
        }
        catch (WebView2RuntimeNotFoundException)
        {
            _status.Text = "Microsoft Edge WebView2 Runtime is required for Twitch sign-in.";
        }
        catch (COMException exception)
        {
            _status.Text = $"Could not open Twitch sign-in: {exception.Message}";
        }
        catch (InvalidOperationException exception)
        {
            _status.Text = $"Could not open Twitch sign-in: {exception.Message}";
        }
    }
}
