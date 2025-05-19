using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace WpfGames.Games.Minesweeper
{
    /// <summary>
    /// Minesweeper.xaml 的交互逻辑
    /// </summary>
    public partial class Minesweeper : Page
    {
        MainWindow? mainWindow = Application.Current.MainWindow as MainWindow;
        private const int CellSize = 30;
        private int _mapSize = 10;
        private int _bombCount = 10;
        private List<Cell> _cells = new List<Cell>();
        private Random _random = new Random();
        private readonly Dictionary<string, (int size, int bombs)> _difficultySettings = new()
        {
            ["初级"] = (6, 5),
            ["中级"] = (10, 10),
            ["高级"] = (16, 60),
            ["地狱"] = (20, 120)
        };
        public class Cell
        {
            public int X { get; set; }
            public int Y { get; set; }
            public int AdjacentBombs { get; set; }
            public bool IsOpened { get; set; }
            public bool IsFlagged { get; set; }
            public bool IsBomb { get; set; }
        }

        public Minesweeper()
        {
            InitializeComponent();
            InitializeDifficultyButtons();
            InitializeGame();
            DrawGameBoard();
        }
        private void InitializeDifficultyButtons()
        {
            foreach (var setting in _difficultySettings)
            {
                var button = new Button
                {
                    Content = setting.Key,
                    Margin = new Thickness(5),
                    Padding = new Thickness(10, 5, 10, 5),
                    Background = Brushes.LightGray,
                    BorderBrush = Brushes.Gray
                };
                button.Click += (s, e) =>
                {
                    _mapSize = setting.Value.size;
                    _bombCount = setting.Value.bombs;
                    InitializeGame();
                    DrawGameBoard();
                };
                DifficultyPanel.Children.Add(button);
            }
        }
        private void InitializeGame()
        {
            _cells.Clear();
            MainCanvas.Children.Clear();
            // 调整画布大小
            MainCanvas.Width = _mapSize * CellSize;
            MainCanvas.Height = _mapSize * CellSize;
            GameBorder.Width = MainCanvas.Width + 10;
            GameBorder.Height = MainCanvas.Height + 10;
            mainWindow.Width = GameBorder.Width + 80;
            mainWindow.Height = GameBorder.Height + 233;
            // 保证mainWindow在屏幕中间
            mainWindow.Left = (SystemParameters.WorkArea.Width - mainWindow.Width) / 2;
            mainWindow.Top = (SystemParameters.WorkArea.Height - mainWindow.Height) / 2;
            for (int y = 0; y < _mapSize; y++)
            {
                for (int x = 0; x < _mapSize; x++)
                {
                    _cells.Add(new Cell { X = x, Y = y });
                }
            }

            for (int i = 0; i < _bombCount; i++)
            {
                int index;
                do
                {
                    index = _random.Next(_cells.Count);
                } while (_cells[index].IsBomb);

                _cells[index].IsBomb = true;
            }

            foreach (var cell in _cells)
            {
                if (cell.IsBomb) continue;

                cell.AdjacentBombs = GetAdjacentCells(cell.X, cell.Y).Count(c => c.IsBomb);
            }
        }

        private IEnumerable<Cell> GetAdjacentCells(int x, int y)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                for (int dx = -1; dx <= 1; dx++)
                {
                    if (dx == 0 && dy == 0) continue;

                    int nx = x + dx;
                    int ny = y + dy;

                    if (nx >= 0 && nx < _mapSize && ny >= 0 && ny < _mapSize)
                    {
                        yield return _cells.First(c => c.X == nx && c.Y == ny);
                    }
                }
            }
        }

        private void DrawGameBoard(bool isSuccess = false)
        {
            MainCanvas.Width = _mapSize * CellSize;
            MainCanvas.Height = _mapSize * CellSize;

            foreach (var cell in _cells)
            {
                var border = new Border
                {
                    Width = CellSize,
                    Height = CellSize,
                    BorderBrush = Brushes.Gray,
                    BorderThickness = new Thickness(1),
                    Background = new SolidColorBrush(Color.FromRgb(0xC0, 0xC0, 0xC0)),
                    Tag = cell
                };

                if (!cell.IsOpened)
                {
                    border.BorderThickness = new Thickness(2);
                    border.BorderBrush = new LinearGradientBrush(
                        Colors.White, Colors.Gray, new Point(0, 0), new Point(1, 1));
                }

                Canvas.SetLeft(border, cell.X * CellSize);
                Canvas.SetTop(border, cell.Y * CellSize);

                var textBlock = new TextBlock
                {
                    Text = cell.IsOpened && cell.AdjacentBombs > 0 ? cell.AdjacentBombs.ToString() : "",
                    FontWeight = FontWeights.Bold,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Foreground = GetNumberColor(cell.AdjacentBombs)
                };

                border.Child = textBlock;

                if (cell.IsFlagged && !isSuccess)
                {
                    var flag = new TextBlock
                    {
                        Text = "🚩",
                        FontSize = 16,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center
                    };
                    border.Child = flag;
                }
                else if (cell.IsOpened && cell.IsBomb)
                {
                    border.Background = isSuccess ? Brushes.Green : Brushes.Red;
                    var bomb = new TextBlock
                    {
                        Text = "💣",
                        FontSize = 16,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center
                    };
                    border.Child = bomb;
                }

                border.MouseLeftButtonDown += Cell_LeftClick;
                border.MouseRightButtonDown += Cell_RightClick;

                MainCanvas.Children.Add(border);
            }
        }

        private Brush GetNumberColor(int number)
        {
            return number switch
            {
                1 => Brushes.Blue,
                2 => Brushes.Green,
                3 => Brushes.Red,
                4 => Brushes.DarkBlue,
                5 => Brushes.DarkRed,
                6 => Brushes.Teal,
                7 => Brushes.Black,
                8 => Brushes.Gray,
                _ => Brushes.Black
            };
        }
        
        #region private method

        private bool CheckWinCondition()
        {
            // 胜利条件：所有非炸弹格子都已打开
            return _cells.Where(c => !c.IsBomb).All(c => c.IsOpened);
        }

        private void OpenCell(Cell cell)
        {
            if (cell.IsOpened || cell.IsFlagged) return;

            cell.IsOpened = true;

            if (cell.AdjacentBombs == 0)
            {
                foreach (var adjacent in GetAdjacentCells(cell.X, cell.Y))
                {
                    OpenCell(adjacent);
                }
            }
        }

        private void RevealAllBombs(bool isSuccess)
        {
            foreach (var cell in _cells.Where(c => c.IsBomb))
            {
                cell.IsOpened = true;
            }
            DrawGameBoard(isSuccess);
        }

        #endregion

        #region click event
        private void Cell_LeftClick(object sender, MouseButtonEventArgs e)
        {
            var border = (Border)sender;
            var cell = (Cell)border.Tag;

            if (cell.IsFlagged || cell.IsOpened) return;

            if (cell.IsBomb)
            {
                // Game over - 显示所有炸弹
                RevealAllBombs(false);
                MessageBox.Show("游戏结束！你踩到地雷了！", "游戏结束",
                              MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }

            OpenCell(cell);
            DrawGameBoard();

            // 检查是否胜利
            if (CheckWinCondition())
            {
                RevealAllBombs(true);
                MessageBox.Show("恭喜你赢了！", "胜利",
                               MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void Cell_RightClick(object sender, MouseButtonEventArgs e)
        {
            var border = (Border)sender;
            var cell = (Cell)border.Tag;

            if (cell.IsOpened) return;

            cell.IsFlagged = !cell.IsFlagged;
            DrawGameBoard();
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            var frame = Parent as Frame;
          
            mainWindow?.MainFrame?.Navigate(new HomePage(mainWindow.MainFrame));
        }


        private void RestartButton_Click(object sender, RoutedEventArgs e)
        {
            InitializeGame();
            DrawGameBoard();
        }
        #endregion
    }
}
