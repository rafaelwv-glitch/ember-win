using Ember;

namespace Ember.App.Pages;

public sealed partial class ChecksPage : Page
{
    public ChecksPage()
    {
        InitializeComponent();
        Loaded += (_, _) => Render();
    }

    void Render()
    {
        Problems.Items.Clear();
        var compiled = Fire.Compile(Session.Active, Session.Catalog);
        if (compiled.Problems.Count == 0)
        {
            Problems.Items.Add("No problems on this card.");
            return;
        }
        foreach (var problem in compiled.Problems)
            Problems.Items.Add($"{problem.Severity.ToUpperInvariant()}  {problem.Rule}  {problem.Message}");
    }
}
