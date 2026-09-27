using Ember;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace Ember.App;

public sealed partial class MainWindow : Window
{
    bool _loadingBots;

    public MainWindow()
    {
        InitializeComponent();
        AppWindow.Resize(new global::Windows.Graphics.SizeInt32(1280, 840));
        NavEdit.IsSelected = true;
        Session.Changed += RefreshChrome;
        FillBots();
        RootFrame.Navigate(typeof(Pages.EditorPage));
        RefreshChrome();
        Root.KeyboardAccelerators.Add(new KeyboardAccelerator
        {
            Key = global::Windows.System.VirtualKey.F5,
            Invoked = (_, args) =>
            {
                args.Handled = true;
                ShowFire();
            }
        });
    }

    void FillBots()
    {
        _loadingBots = true;
        BotBox.Items.Clear();
        foreach (var bot in Session.Bots)
            BotBox.Items.Add(new ComboBoxItem { Content = bot.Title, Tag = bot.Slug });
        for (var i = 0; i < BotBox.Items.Count; i++)
        {
            if ((BotBox.Items[i] as ComboBoxItem)?.Tag as string == Session.ActiveSlug)
                BotBox.SelectedIndex = i;
        }
        _loadingBots = false;
    }

    void RefreshChrome()
    {
        FillBots();
        var compiled = Fire.Compile(Session.Active, Session.Catalog);
        var errors = compiled.Problems.Count(problem => problem.Severity == "error");
        StatusText.Text = errors == 0
            ? $"{Session.Active.Title}  ·  checks clear  ·  nothing is posted"
            : $"{Session.Active.Title}  ·  {errors} error{(errors == 1 ? "" : "s")}";
        NavChecks.Content = errors == 0 ? "Checks" : $"Checks ({errors})";
    }

    void Nav_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.SelectedItem is not NavigationViewItem item) return;
        var tag = item.Tag as string;
        var page = tag switch
        {
            "library" => typeof(Pages.LibraryPage),
            "fire" => typeof(Pages.FirePage),
            "checks" => typeof(Pages.ChecksPage),
            _ => typeof(Pages.EditorPage)
        };
        if (RootFrame.CurrentSourcePageType != page)
            RootFrame.Navigate(page);
    }

    void BotBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_loadingBots) return;
        if (BotBox.SelectedItem is ComboBoxItem item && item.Tag is string slug && slug != Session.ActiveSlug)
            Session.Select(slug);
    }

    void Fire_Click(object sender, RoutedEventArgs e) => ShowFire();

    void ShowFire()
    {
        Nav.SelectedItem = NavFire;
        RootFrame.Navigate(typeof(Pages.FirePage));
    }

    void Reset_Click(object sender, RoutedEventArgs e)
    {
        Session.Reset();
        RootFrame.Navigate(typeof(Pages.EditorPage));
        Nav.SelectedItem = NavEdit;
    }

    async void New_Click(object sender, RoutedEventArgs e)
    {
        var title = new TextBox { Header = "Name", Text = "Sunday Landing" };
        var engines = new ComboBox { Header = "Engine", HorizontalAlignment = HorizontalAlignment.Stretch };
        foreach (var engine in Session.Catalog.Engines)
            engines.Items.Add(new ComboBoxItem { Content = engine.Title, Tag = engine.Id });
        engines.SelectedIndex = Session.Catalog.Engines.ToList().FindIndex(engine => engine.Id == "shared-landing");
        var places = new ComboBox { Header = "Place", HorizontalAlignment = HorizontalAlignment.Stretch };
        foreach (var place in Session.Catalog.Locations)
            places.Items.Add(new ComboBoxItem { Content = place.Title, Tag = place.Id });
        places.SelectedIndex = 0;
        var castPanel = new StackPanel { Spacing = 4 };
        var boxes = new List<(CheckBox Box, string Id)>();
        foreach (var npc in Session.Catalog.Npcs)
        {
            var box = new CheckBox { Content = $"{npc.Name}  ·  {npc.Job}", IsChecked = npc.Id == "mara" };
            boxes.Add((box, npc.Id));
            castPanel.Children.Add(box);
        }
        var panel = new StackPanel { Spacing = 10, Width = 420 };
        panel.Children.Add(title);
        panel.Children.Add(engines);
        panel.Children.Add(places);
        panel.Children.Add(new TextBlock { Text = "People", Margin = new Thickness(0, 6, 0, 0) });
        panel.Children.Add(castPanel);
        var dialog = new ContentDialog
        {
            Title = "New card",
            Content = panel,
            PrimaryButtonText = "Create",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = Root.XamlRoot
        };
        if (await dialog.ShowAsync() != ContentDialogResult.Primary) return;
        var cast = boxes.Where(item => item.Box.IsChecked == true).Select(item => item.Id).ToList();
        if (cast.Count == 0 || string.IsNullOrWhiteSpace(title.Text)) return;
        var engineId = (engines.SelectedItem as ComboBoxItem)?.Tag as string ?? "shared-landing";
        var placeId = (places.SelectedItem as ComboBoxItem)?.Tag as string ?? "harbor-flat";
        Session.Add(Skeletons.Create(Session.Catalog, title.Text, engineId, cast, placeId));
        Nav.SelectedItem = NavEdit;
        RootFrame.Navigate(typeof(Pages.EditorPage));
    }
}
