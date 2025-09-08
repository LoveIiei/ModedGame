using System.Windows;

namespace ModedGame
{
    public partial class ProgressWindow : Window
    {
        public ProgressWindow(string title)
        {
            InitializeComponent();
            Title = title;
            ProgressText.Text = title;
        }

        public void UpdateProgress(int percentage)
        {
            Dispatcher.Invoke(() =>
            {
                ProgressBar.Value = percentage;
                PercentageText.Text = $"{percentage}%";
            });
        }
    }
}