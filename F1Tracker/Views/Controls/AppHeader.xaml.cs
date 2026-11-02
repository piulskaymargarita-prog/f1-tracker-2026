using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using F1Tracker.Data;
using F1Tracker.Helpers;
using F1Tracker.Models;

namespace F1Tracker.Views.Controls;

public partial class AppHeader : UserControl
{
    public static readonly DependencyProperty ActivePageProperty =
        DependencyProperty.Register(nameof(ActivePage), typeof(AppPage), typeof(AppHeader),
            new PropertyMetadata(AppPage.Main, (d, _) => ((AppHeader)d).UpdateActive()));

    /// <summary>Какой раздел подсвечивать в меню.</summary>
    public AppPage ActivePage
    {
        get => (AppPage)GetValue(ActivePageProperty);
        set => SetValue(ActivePageProperty, value);
    }

    public AppHeader()
    {
        InitializeComponent();

        BtnAdmin.Visibility = Session.IsAdmin ? Visibility.Visible : Visibility.Collapsed;
        UserText.Text = Session.Username;
        RoleText.Text = Session.Role.ToUpperInvariant();
        RoleBadge.Background = (Brush)FindResource(Session.IsAdmin ? "AccentBrush" : "LineBrush");
        UpdateActive();
    }

    private void UpdateActive()
    {
        foreach (var child in NavPanel.Children)
            if (child is Button b && b.CommandParameter is AppPage p)
                b.Tag = p == ActivePage ? "active" : null;
    }

    private Window? Host => Window.GetWindow(this);

    private void Nav_Click(object sender, RoutedEventArgs e)
    {
        if (Host is { } w && sender is Button { CommandParameter: AppPage page })
            Navigator.GoTo(page, w);
    }

    private void Logo_Click(object sender, MouseButtonEventArgs e)
    {
        if (Host is { } w) Navigator.GoTo(AppPage.Main, w);
    }

    private void Logout_Click(object sender, RoutedEventArgs e)
    {
        if (Host is not { } w) return;
        if (MessageBox.Show("Выйти из учётной записи?", "Выход",
                MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
        Navigator.Logout(w);
    }

    // ---------------- Сквозной поиск ----------------
    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        string q = SearchBox.Text.Trim();
        if (q.Length < 2)
        {
            ResultsPopup.IsOpen = false;
            return;
        }

        var hits = SearchService.Search(q);
        HitsList.ItemsSource = hits;
        HitsList.Visibility = hits.Count == 0 ? Visibility.Collapsed : Visibility.Visible;
        EmptyText.Visibility = hits.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        ResultsPopup.IsOpen = true;
    }

    private void SearchBox_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Escape:
                ResultsPopup.IsOpen = false;
                SearchBox.Clear();
                e.Handled = true;
                break;

            case Key.Enter:
                if (HitsList.Items.Count > 0 && HitsList.Items[0] is SearchHit first) OpenHit(first);
                e.Handled = true;
                break;

            case Key.Down when HitsList.Items.Count > 0:
                HitsList.SelectedIndex = 0;
                HitsList.UpdateLayout();
                (HitsList.ItemContainerGenerator.ContainerFromIndex(0) as ListBoxItem)?.Focus();
                e.Handled = true;
                break;
        }
    }

    private void HitsList_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && HitsList.SelectedItem is SearchHit hit)
        {
            OpenHit(hit);
            e.Handled = true;
        }
        else if (e.Key == Key.Escape)
        {
            ResultsPopup.IsOpen = false;
            SearchBox.Focus();
            e.Handled = true;
        }
    }

    private void Hit_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is ListBoxItem { DataContext: SearchHit hit }) OpenHit(hit);
    }

    private void OpenHit(SearchHit hit)
    {
        ResultsPopup.IsOpen = false;
        if (Host is not { } w) return;

        ActionLogger.Log("SEARCH", $"'{SearchBox.Text.Trim()}' → {hit.Kind}: {hit.Title}");
        Navigator.GoToData(hit.Tab, hit.Title, w);
    }
}