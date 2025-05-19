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

        public HomePage(Frame mainFrame)
        {
            InitializeComponent();
            _mainFrame = mainFrame;
        }

        private void Minesweeper_Click(object sender, RoutedEventArgs e)
        {
            _mainFrame.Navigate(new Minesweeper.Minesweeper());
        }
    }
}

