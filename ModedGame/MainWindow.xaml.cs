using ModedGame.Pages;
using System.Windows;

namespace ModedGame
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            MainFrame.Navigate(new MinecraftPage()); // Default page
        }

        private void NevigateToMinecraft(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new MinecraftPage());
        }

        private void NevigateToMHW(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new MHWPage());
        }
    }
}