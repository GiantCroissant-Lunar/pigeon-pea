using System;
using System.Threading.Tasks;
using Avalonia.Controls;

namespace PigeonPea.Windows;

/// <summary>
/// Simple message box utility for displaying errors and information.
/// </summary>
public static class MessageBox
{
    public static async Task Show(Window? owner, string message, string title = "PigeonPea")
    {
        var dialog = new Window
        {
            Title = title,
            Width = 400,
            Height = 150,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false
        };

        var panel = new StackPanel
        {
            Margin = new Avalonia.Thickness(10)
        };

        panel.Children.Add(new TextBlock
        {
            Text = message,
            TextWrapping = Avalonia.Media.TextWrapping.Wrap,
            Margin = new Avalonia.Thickness(0, 0, 0, 10)
        });

        var button = new Button
        {
            Content = "OK",
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            MinWidth = 75
        };

        button.Click += (s, e) => dialog.Close();
        panel.Children.Add(button);

        dialog.Content = panel;

        if (owner != null)
        {
            await dialog.ShowDialog(owner);
        }
        else
        {
            dialog.Show();
        }
    }
}
