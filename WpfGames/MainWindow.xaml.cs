using System.Windows;

namespace WpfGames
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            MainFrame.Navigate(new Games.HomePage(MainFrame));
            // 保证mainWindow在屏幕中间
            this.Left = (SystemParameters.WorkArea.Width - this.Width) / 2;
            this.Top = (SystemParameters.WorkArea.Height - this.Height) / 2;
        }
    }
}