using System.Windows;
using System.Windows.Controls;

namespace WpfGames.Games
{
    /// <summary>
    /// HomePage.xaml 的交互逻辑
    /// </summary>
    public partial class HomePage : Page
    {
        private Frame _mainFrame;
        MainWindow mainWindow = Application.Current.MainWindow as MainWindow ?? new MainWindow();
        public HomePage(Frame mainFrame)
        {
            InitializeComponent();
            _mainFrame = mainFrame;
            InitPage();
        }
        private void InitPage()
        {
            mainWindow.Width = 500;
            mainWindow.Height = 400;
            // 保证mainWindow在屏幕中间
            mainWindow.Left = (SystemParameters.WorkArea.Width - mainWindow.Width) / 2;
            mainWindow.Top = (SystemParameters.WorkArea.Height - mainWindow.Height) / 2;
        }
        private void Minesweeper_Click(object sender, RoutedEventArgs e)
        {
            _mainFrame.Navigate(new Minesweeper.Minesweeper());
        }

        private void Snake_Click(object sender, RoutedEventArgs e)
        {
            _mainFrame.Navigate(new Snake.Snake());
        }

    }
}

