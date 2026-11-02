using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using F1Tracker.Helpers;

namespace F1Tracker.Views.Controls;

/// <summary>Хлебные крошки. Trail="Главная/Данные/Пилоты" — все, кроме последней, кликабельны, если это раздел.</summary>
public partial class BreadcrumbBar : UserControl
{
    private static readonly Dictionary<string, AppPage> Links = new()
    {
        ["Главная"] = AppPage.Main,
        ["Данные"] = AppPage.Data,
        ["Статистика"] = AppPage.Statistics,
        ["Администрирование"] = AppPage.Admin,
        ["О программе"] = AppPage.About
    };

    public static readonly DependencyProperty TrailProperty =
        DependencyProperty.Register(nameof(Trail), typeof(string), typeof(BreadcrumbBar),
            new PropertyMetadata("", (d, _) => ((BreadcrumbBar)d).Build()));

    public string Trail
    {
        get => (string)GetValue(TrailProperty);
        set => SetValue(TrailProperty, value);
    }

    public BreadcrumbBar() => InitializeComponent();

    private void Build()
    {
        CrumbPanel.Children.Clear();
        var parts = Trail.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        for (int i = 0; i < parts.Length; i++)
        {
            bool last = i == parts.Length - 1;

            if (i > 0)
                CrumbPanel.Children.Add(new TextBlock
                {
                    Text = "›",
                    Margin = new Thickness(10, 0, 10, 0),
                    Foreground = (Brush)FindResource("MutedBrush")
                });

            if (!last && Links.TryGetValue(parts[i], out var page))
            {
                var btn = new Button
                {
                    Content = parts[i],
                    Style = (Style)FindResource("LinkButton")
                };
                btn.Click += (_, _) =>
                {
                    if (Window.GetWindow(this) is { } w) Navigator.GoTo(page, w);
                };
                CrumbPanel.Children.Add(btn);
            }
            else
            {
                CrumbPanel.Children.Add(new TextBlock
                {
                    Text = parts[i],
                    FontWeight = last ? FontWeights.Bold : FontWeights.Normal,
                    Foreground = (Brush)FindResource(last ? "TextBrush" : "MutedBrush")
                });
            }
        }
    }
}