using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;

namespace StreamMultiviewer;

public sealed class VideoWallWindow : Window
{
    private const string PlayerViewScript = """
        (() => {
          if (window.__windowSnapperInstalled) {
            return window.__windowSnapperApply ? window.__windowSnapperApply() : 'waiting';
          }
          window.__windowSnapperInstalled = true;

          const selector = 'video[data-html5-video], video';
          let playerFrame = null;

          const setImportant = (element, props) => {
            for (const [property, value] of Object.entries(props)) {
              element.style.setProperty(property, value, 'important');
            }
          };

          const applyPlayerView = () => {
            const video = document.querySelector(selector);
            const target = video || playerFrame;
            if (!target || !document.body) return false;
            const playerPoster = video ? document.querySelector('.player-poster.clickable') : null;
            if (video) video.controls = true;

            const html = document.documentElement;
            setImportant(html, { width: '100%', height: '100%', margin: '0', overflow: 'hidden', background: '#000' });
            setImportant(document.body, { width: '100%', height: '100%', margin: '0', overflow: 'hidden', background: '#000' });

            for (const element of document.body.querySelectorAll('*')) {
              const isPlayerContent =
                element === target ||
                element.contains(target) ||
                (playerPoster && (
                  element === playerPoster ||
                  element.contains(playerPoster) ||
                  playerPoster.contains(element)
                ));
              if (!isPlayerContent) {
                element.style.setProperty('display', 'none', 'important');
                continue;
              }

              if (element !== target &&
                  !(playerPoster && (element === playerPoster || playerPoster.contains(element)))) {
                setImportant(element, {
                  background: 'transparent',
                  border: '0',
                  'box-shadow': 'none',
                  margin: '0',
                  padding: '0',
                  overflow: 'visible',
                  transform: 'none',
                  filter: 'none',
                  perspective: 'none',
                  contain: 'none'
                });
              }
            }

            if (playerPoster) {
              setImportant(playerPoster, {
                position: 'fixed',
                inset: '0',
                width: '100vw',
                height: '100vh',
                'z-index': '2147483647',
                'pointer-events': 'auto'
              });
            }

            setImportant(target, {
              position: 'fixed',
              top: '0',
              left: '0',
              right: '0',
              bottom: '0',
              width: '100vw',
              height: '100vh',
              'max-width': 'none',
              'max-height': 'none',
              margin: '0',
              border: '0',
              background: '#000',
              'z-index': '1',
              display: 'block',
              visibility: 'visible',
              opacity: '1',
              ...(video ? { 'object-fit': 'contain' } : {})
            });

            if (window.parent !== window) {
              window.parent.postMessage({ __windowSnapperPlayerFrame: true }, '*');
            }
            return true;
          };

          window.__windowSnapperApply = () => applyPlayerView() ? 'ready' : 'waiting';

          window.addEventListener('message', event => {
            if (!event.data || event.data.__windowSnapperPlayerFrame !== true) return;
            const frame = [...document.querySelectorAll('iframe, frame')]
              .find(candidate => candidate.contentWindow === event.source);
            if (!frame || frame === playerFrame) return;
            playerFrame = frame;
            applyPlayerView();
          });

          new MutationObserver(applyPlayerView).observe(document, {
            childList: true,
            subtree: true
          });
          return window.__windowSnapperApply();
        })();
        """;

    private const string TwitchPlayerViewScript = """
        (() => {
          const applyPlayerView = () => {
            const video = document.querySelector('video');
            if (!video || !document.body) return false;

            let player = video.closest('.video-player, [data-a-target="video-player"]');
            if (!player) {
              for (let ancestor = video.parentElement; ancestor && ancestor !== document.body; ancestor = ancestor.parentElement) {
                if (ancestor.querySelector('button, [role="button"]')) {
                  player = ancestor;
                  break;
                }
              }
            }
            player ||= video.parentElement;
            if (!player) return true;

            const html = document.documentElement;
            html.style.setProperty('width', '100%', 'important');
            html.style.setProperty('height', '100%', 'important');
            html.style.setProperty('margin', '0', 'important');
            html.style.setProperty('overflow', 'hidden', 'important');
            html.style.setProperty('background', '#000', 'important');

            const body = document.body;
            body.style.setProperty('width', '100%', 'important');
            body.style.setProperty('height', '100%', 'important');
            body.style.setProperty('margin', '0', 'important');
            body.style.setProperty('overflow', 'hidden', 'important');
            body.style.setProperty('background', '#000', 'important');

            for (const element of body.querySelectorAll('*')) {
              if (element === player || player.contains(element) || element.contains(player)) {
                continue;
              }
              element.style.setProperty('display', 'none', 'important');
            }

            for (let ancestor = player.parentElement; ancestor && ancestor !== body; ancestor = ancestor.parentElement) {
              const rect = ancestor.getBoundingClientRect();
              const style = getComputedStyle(ancestor);
              if (rect.width <= 1 || rect.height <= 1) {
                for (const [property, value] of Object.entries({
                  position: 'fixed',
                  inset: '0',
                  width: '100vw',
                  height: '100vh',
                  'min-width': '100vw',
                  'min-height': '100vh',
                  transform: 'none'
                })) {
                  ancestor.style.setProperty(property, value, 'important');
                }
              }

              if ([style.overflowX, style.overflowY].some(value => value === 'hidden' || value === 'clip')) {
                ancestor.style.setProperty('overflow', 'visible', 'important');
              }
            }

            for (const [property, value] of Object.entries({
              position: 'fixed',
              inset: '0',
              width: '100vw',
              height: '100vh',
              margin: '0',
              overflow: 'hidden',
              background: '#000'
            })) {
              player.style.setProperty(property, value, 'important');
            }

            return true;
          };

          if (!window.__windowSnapperObserver) {
            window.__windowSnapperObserver = new MutationObserver(applyPlayerView);
            window.__windowSnapperObserver.observe(document.documentElement, {
              childList: true,
              subtree: true
            });
          }
          return applyPlayerView() ? 'ready' : 'waiting';
        })();
        """;

    private readonly IReadOnlyList<Uri> _urls;
    private readonly CoreWebView2Environment? _environment;
    private readonly bool _isTwitchMode;
    private readonly Grid _wall = new() { Background = Brushes.Black };
    private readonly List<WebView2> _webViews = [];
    private readonly List<TextBlock> _statuses = [];
    private readonly CancellationTokenSource _closing = new();

    public VideoWallWindow(
        IReadOnlyList<Uri> urls,
        CoreWebView2Environment? environment = null,
        bool isTwitchMode = false)
    {
        _urls = urls;
        _environment = environment;
        _isTwitchMode = isTwitchMode;
        Title = "StreamMultiviewer";
        Icon = Application.Current.MainWindow?.Icon;
        WindowStyle = WindowStyle.None;
        ResizeMode = ResizeMode.NoResize;
        WindowStartupLocation = WindowStartupLocation.Manual;
        Left = 0;
        Top = 0;
        Width = SystemParameters.PrimaryScreenWidth;
        Height = SystemParameters.PrimaryScreenHeight;
        Background = Brushes.Black;
        Content = _wall;

        BuildTiles();
        Loaded += Viewer_Loaded;
        SizeChanged += (_, _) => UpdateTwoTileSizes();
        KeyDown += (_, args) =>
        {
            if (args.Key == System.Windows.Input.Key.Escape)
            {
                Close();
            }
        };
        Closed += (_, _) => _closing.Cancel();
    }

    private void BuildTiles()
    {
        var columns = _urls.Count switch
        {
            2 or 4 => 2,
            6 => 3,
            _ => throw new ArgumentOutOfRangeException(nameof(_urls), "Only 2, 4, or 6 URLs are supported.")
        };
        var rows = _urls.Count / columns;

        for (var index = 0; index < columns; index++)
        {
            _wall.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        }

        for (var index = 0; index < rows; index++)
        {
            _wall.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        }

        for (var index = 0; index < _urls.Count; index++)
        {
            var tile = new Grid { Background = Brushes.Black, ClipToBounds = true };
            var webView = new WebView2
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            };
            var status = new TextBlock
            {
                Text = "Loading game page…",
                Foreground = Brushes.White,
                Background = new SolidColorBrush(Color.FromArgb(210, 16, 19, 26)),
                FontSize = 16,
                TextWrapping = TextWrapping.Wrap,
                TextAlignment = TextAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                Padding = new Thickness(18),
                IsHitTestVisible = false
            };

            tile.Children.Add(webView);
            tile.Children.Add(status);
            Grid.SetColumn(tile, index % columns);
            Grid.SetRow(tile, index / columns);
            _wall.Children.Add(tile);
            _webViews.Add(webView);
            _statuses.Add(status);
        }

        UpdateTwoTileSizes();
    }

    private void UpdateTwoTileSizes()
    {
        if (_urls.Count != 2 || _wall.ActualWidth <= 0 || _wall.ActualHeight <= 0)
        {
            return;
        }

        var tileWidth = Math.Min(_wall.ActualWidth / 2, _wall.ActualHeight * 16 / 9);
        var tileHeight = tileWidth * 9 / 16;
        foreach (var webView in _webViews)
        {
            webView.Width = tileWidth;
            webView.Height = tileHeight;
            webView.HorizontalAlignment = HorizontalAlignment.Center;
            webView.VerticalAlignment = VerticalAlignment.Center;
        }
    }

    private async void Viewer_Loaded(object sender, RoutedEventArgs e)
    {
        Loaded -= Viewer_Loaded;

        for (var index = 0; index < _webViews.Count; index++)
        {
            await InitializeWebViewAsync(_webViews[index], _statuses[index], _urls[index]);
        }
    }

    private async Task InitializeWebViewAsync(WebView2 webView, TextBlock status, Uri url)
    {
        try
        {
            await webView.EnsureCoreWebView2Async(_environment);
            if (!_isTwitchMode)
            {
                // Runs in every frame so players embedded in iframes are found too.
                await webView.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(PlayerViewScript);
            }

            webView.NavigationCompleted += async (_, args) =>
            {
                if (!args.IsSuccess)
                {
                    status.Text = $"Page load failed: {args.WebErrorStatus}";
                    status.Visibility = Visibility.Visible;
                    return;
                }

                await WaitForVideoAsync(webView, status);
            };
            webView.CoreWebView2.ProcessFailed += (_, _) =>
            {
                status.Text = "The browser process stopped. Press Esc and try again.";
                status.Visibility = Visibility.Visible;
            };
            webView.Source = url;
        }
        catch (WebView2RuntimeNotFoundException)
        {
            status.Text = "Microsoft Edge WebView2 Runtime is required to show games.";
            status.Visibility = Visibility.Visible;
        }
        catch (COMException exception)
        {
            status.Text = $"Could not start the browser view: {exception.Message}";
            status.Visibility = Visibility.Visible;
        }
        catch (InvalidOperationException exception)
        {
            status.Text = $"Could not start the browser view: {exception.Message}";
            status.Visibility = Visibility.Visible;
        }
    }

    private async Task WaitForVideoAsync(WebView2 webView, TextBlock status)
    {
        try
        {
            for (var attempt = 0; attempt < 30; attempt++)
            {
                _closing.Token.ThrowIfCancellationRequested();
                var script = _isTwitchMode ? TwitchPlayerViewScript : PlayerViewScript;
                var result = await webView.ExecuteScriptAsync(script);
                if (result == "\"ready\"")
                {
                    status.Visibility = Visibility.Collapsed;
                    return;
                }

                status.Text = "Waiting for the page's video player…";
                await Task.Delay(TimeSpan.FromSeconds(1), _closing.Token);
            }

            status.Text = "No HTML5 video player was found. Check the URL and provider playback support.";
            status.Visibility = Visibility.Visible;
        }
        catch (OperationCanceledException)
        {
        }
        catch (COMException exception)
        {
            status.Text = $"Could not configure the video view: {exception.Message}";
            status.Visibility = Visibility.Visible;
        }
        catch (InvalidOperationException exception)
        {
            status.Text = $"Could not configure the video view: {exception.Message}";
            status.Visibility = Visibility.Visible;
        }
    }
}
