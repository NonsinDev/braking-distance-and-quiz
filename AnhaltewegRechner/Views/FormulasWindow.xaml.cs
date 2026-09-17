using System.Windows;

namespace AnhaltewegRechner.Views;

public partial class FormulasWindow : Window
{
    public FormulasWindow()
    {
        InitializeComponent();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
