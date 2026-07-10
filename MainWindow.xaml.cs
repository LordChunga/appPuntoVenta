using System.Windows;
using MiniPosWpf.Data;
using MiniPosWpf.ViewModels;

namespace MiniPosWpf;

public partial class MainWindow : Window
{
    private readonly Database database;
    private readonly MainViewModel viewModel;

    public MainWindow()
    {
        InitializeComponent();

        database = new Database();
        viewModel = new MainViewModel(new StoreRepository(database));
        DataContext = viewModel;

        Loaded += OnLoaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        Loaded -= OnLoaded;
        await database.InitializeAsync();
        await viewModel.LoadAsync();
    }

    private void MetricasAcciones_Click(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.Button button && button.ContextMenu != null)
        {
            button.ContextMenu.PlacementTarget = button;
            button.ContextMenu.IsOpen = true;
        }
    }
}
