using Ember;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.ApplicationModel.DataTransfer;

namespace Ember.App.Pages;

public sealed partial class FirePage : Page
{
    string _mode = "card";
    Compiled? _compiled;

    public FirePage()
    {
        InitializeComponent();
        Loaded += (_, _) => Render();
    }

    void Mode_Click(object sender, RoutedEventArgs e)
    {
        _mode = (sender as Button)?.Tag as string ?? "card";
        Render();
    }

    void Render()
    {
        _compiled = Fire.Compile(Session.Active, Session.Catalog);
        var errors = _compiled.Problems.Count(problem => problem.Severity == "error");
        Summary.Text = errors == 0 ? "Clean. Nothing is sent." : $"{errors} error(s). Nothing is sent.";
        Output.Text = _mode switch
        {
            "json" => _compiled.Json,
            "md" => _compiled.Markdown,
            _ => string.Join("\n\n", new[]
            {
                "Name · characterName\n" + _compiled.Title,
                "Introduction · introduction\n" + _compiled.Bio,
                "Situation · setting\n" + _compiled.Situation,
                "World · scenario\n" + _compiled.World,
                "Greeting · greeting\n" + _compiled.Greeting,
                "Reply style · archive only\n" + _compiled.Reply
            })
        };
    }

    void Copy_Click(object sender, RoutedEventArgs e)
    {
        var data = new DataPackage();
        data.SetText(Output.Text);
        Clipboard.SetContent(data);
    }
}
