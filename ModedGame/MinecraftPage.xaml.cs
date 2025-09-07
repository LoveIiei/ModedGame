using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using ModedGame.ViewModels;
using Microsoft.Win32;

namespace ModedGame
{
    /// <summary>
    /// Interaction logic for MinecraftPage.xaml
    /// </summary>
    public partial class MinecraftPage : Page
    {
        public MinecraftPage()
        {
            InitializeComponent();
            DataContext = new MCFileTypeViewModel();
        }
        private void ColorPicker_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selected = FileTypePicker.SelectedItem?.ToString();
            MessageBox.Show($"Selected: {selected}");
        }

        private void File_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            if (ofd.ShowDialog() == true)
            {
                string selectedFilePath = ofd.FileName;
                MessageBox.Show(selectedFilePath);
            }
        }
    }
}
