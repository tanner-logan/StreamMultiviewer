using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Web.WebView2.Core;

namespace StreamMultiviewer;

public partial class MainWindow : Window
{
    private readonly List<TextBox> _urlInputs = [];
    private readonly List<StackPanel> _urlRows = [];
    private CoreWebView2Environment? _twitchEnvironment;
    private bool _isTwitchMode;

    public MainWindow()
    {
        InitializeComponent();
        CreateUrlInputs();
        LayoutSelector.SelectionChanged += LayoutSelector_SelectionChanged;
        UpdateVisibleInputs();
    }

    private void CreateUrlInputs()
    {
        for (var index = 0; index < 6; index++)
        {
            var row = new StackPanel
            {
                Margin = new Thickness(0, 0, 0, 16)
            };

            var label = new TextBlock
            {
                Text = $"Game {index + 1}",
                Foreground = new SolidColorBrush(Color.FromRgb(196, 205, 219)),
                FontSize = 13,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 0, 7)
            };

            var input = new TextBox
            {
                Height = 42,
                Padding = new Thickness(12, 9, 12, 8),
                Background = new SolidColorBrush(Color.FromRgb(23, 27, 36)),
                Foreground = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(52, 60, 75)),
                FontSize = 14,
                ToolTip = "Enter the game's official page URL"
            };

            row.Children.Add(label);
            row.Children.Add(input);
            UrlFieldsPanel.Children.Add(row);
            _urlRows.Add(row);
            _urlInputs.Add(input);
        }
    }

    private void LayoutSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        UpdateVisibleInputs();
    }

    private void UpdateVisibleInputs()
    {
        var count = GetSelectedCount();
        for (var index = 0; index < _urlRows.Count; index++)
        {
            _urlRows[index].Visibility = index < count ? Visibility.Visible : Visibility.Collapsed;
        }
    }

    private int GetSelectedCount()
    {
        return LayoutSelector.SelectedItem is ComboBoxItem item &&
               int.TryParse(item.Tag?.ToString(), out var count)
            ? count
            : 2;
    }

    private void GeneralModeButton_Click(object sender, RoutedEventArgs e)
    {
        _isTwitchMode = false;
        SetupTitle.Text = "Stream Multiviewer";
        SetupDescription.Text = "Choose a layout and enter the game page URLs.";
        ShowSetup();
    }

    private async void TwitchModeButton_Click(object sender, RoutedEventArgs e)
    {
        ModeError.Text = string.Empty;
        try
        {
            _twitchEnvironment ??= await CreateTwitchEnvironmentAsync();
            var loginWindow = new TwitchLoginWindow(_twitchEnvironment) { Owner = this };
            loginWindow.ShowDialog();

            _isTwitchMode = true;
            SetupTitle.Text = "Twitch Multi-View";
            SetupDescription.Text = "Enter Twitch game page URLs for your signed-in session.";
            ShowSetup();
        }
        catch (WebView2RuntimeNotFoundException)
        {
            ModeError.Text = "Microsoft Edge WebView2 Runtime is required for Twitch mode.";
        }
        catch (COMException exception)
        {
            ModeError.Text = $"Could not start Twitch sign-in: {exception.Message}";
        }
        catch (IOException exception)
        {
            ModeError.Text = $"Could not create the Twitch browser profile: {exception.Message}";
        }
        catch (UnauthorizedAccessException exception)
        {
            ModeError.Text = $"Could not access the Twitch browser profile folder: {exception.Message}";
        }
        catch (InvalidOperationException exception)
        {
            ModeError.Text = $"Could not start Twitch sign-in: {exception.Message}";
        }
    }

    private static async Task<CoreWebView2Environment> CreateTwitchEnvironmentAsync()
    {
        var userDataFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "StreamMultiviewer",
            "TwitchWebView2");
        Directory.CreateDirectory(userDataFolder);
        return await CoreWebView2Environment.CreateAsync(userDataFolder: userDataFolder);
    }

    private void ShowSetup()
    {
        ModePanel.Visibility = Visibility.Collapsed;
        SetupPanel.Visibility = Visibility.Visible;
        ValidationMessage.Text = string.Empty;
    }

    private void BackButton_Click(object sender, RoutedEventArgs e)
    {
        SetupPanel.Visibility = Visibility.Collapsed;
        ModePanel.Visibility = Visibility.Visible;
    }

    private async void StartButton_Click(object sender, RoutedEventArgs e)
    {
        var count = GetSelectedCount();
        var urls = new List<Uri>(count);

        for (var index = 0; index < count; index++)
        {
            var value = _urlInputs[index].Text.Trim();
            if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) ||
                (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp))
            {
                ValidationMessage.Text = $"Enter a valid http or https URL for Game {index + 1}.";
                _urlInputs[index].Focus();
                return;
            }

            urls.Add(uri);
        }

        ValidationMessage.Text = string.Empty;
        CoreWebView2Environment? environment = _isTwitchMode
            ? _twitchEnvironment
            : null;
        if (_isTwitchMode && environment is null)
        {
            try
            {
                environment = _twitchEnvironment = await CreateTwitchEnvironmentAsync();
            }
            catch (WebView2RuntimeNotFoundException)
            {
                ValidationMessage.Text = "Microsoft Edge WebView2 Runtime is required for Twitch mode.";
                return;
            }
            catch (COMException exception)
            {
                ValidationMessage.Text = $"Could not start Twitch browser profile: {exception.Message}";
                return;
            }
            catch (IOException exception)
            {
                ValidationMessage.Text = $"Could not create the Twitch browser profile: {exception.Message}";
                return;
            }
            catch (UnauthorizedAccessException exception)
            {
                ValidationMessage.Text = $"Could not access the Twitch browser profile folder: {exception.Message}";
                return;
            }
            catch (InvalidOperationException exception)
            {
                ValidationMessage.Text = $"Could not start Twitch browser profile: {exception.Message}";
                return;
            }
        }

        var viewer = new VideoWallWindow(urls, environment, _isTwitchMode);
        viewer.Closed += (_, _) =>
        {
            Show();
            Activate();
        };

        Hide();
        viewer.Show();
        viewer.Activate();
    }
}
