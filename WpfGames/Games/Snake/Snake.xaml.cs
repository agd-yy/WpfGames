using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace WpfGames.Games.Snake
{
    /// <summary>
    /// Snake.xaml 的交互逻辑
    /// </summary>
    public partial class Snake : Page
    {
        #region 常量
        private const int GridSize = 20;
        private const int CellSize = 25;
        // 移动速度间隔
        private const double BaseUpdateInterval = 200;
        #endregion

        #region 状态
        private readonly LinkedList<Point> _snakeBody = new();
        private readonly HashSet<Point> _bodyPositions = new();
        private Point _food;
        private enum Direction { Up, Down, Left, Right }
        private Direction _currentDirection = Direction.Right;
        private Direction _nextDirection = Direction.Right;

        private int _score;
        private DateTime _lastUpdateTime;
        private bool _isGameRunning;
        private bool _isAutoMode;
        #endregion

        MainWindow mainWindow = Application.Current.MainWindow as MainWindow ?? new MainWindow();
        private readonly WriteableBitmap _gameBitmap;

        public Snake()
        {
            InitializeComponent();
            InitializePage();
            this.Focusable = true;
            this.Loaded += (s, e) => this.Focus();

            _gameBitmap = new WriteableBitmap(
                GridSize * CellSize,
                GridSize * CellSize,
                96, 96, PixelFormats.Bgra32, null);
            BitmapImage.Source = _gameBitmap;
            InitializeGame();
        }

        #region 初始化
        private void InitializePage()
        {
            mainWindow.Width = 777;
            mainWindow.Height = 650;
            mainWindow.Left = (SystemParameters.WorkArea.Width - mainWindow.Width) / 2;
            mainWindow.Top = (SystemParameters.WorkArea.Height - mainWindow.Height) / 2;
        }
        private void InitializeGame()
        {
            _snakeBody.Clear();
            _bodyPositions.Clear();
            _score = 0;

            // 初始蛇身 (3节)
            AddSegment(new Point(5, 9));
            AddSegment(new Point(6, 9));
            AddSegment(new Point(7, 9));

            _currentDirection = _nextDirection = Direction.Right;
            GenerateFood();
            UpdateScoreDisplay();

            _isGameRunning = true;
            _lastUpdateTime = DateTime.Now;
            CompositionTarget.Rendering += GameLoop;
        }
        
        private void AddSegment(Point position)
        {
            _snakeBody.AddLast(position);
            _bodyPositions.Add(position);
        }
        #endregion

        #region 游戏逻辑
        private void GameLoop(object? sender, EventArgs e)
        {
            if (!_isGameRunning || _snakeBody.Last == null) return;

            double elapsed = (DateTime.Now - _lastUpdateTime).TotalMilliseconds;
            if (elapsed < 20) return;

            _lastUpdateTime = DateTime.Now;
            
            var head = _snakeBody.Last();
            // 自动寻路
            if (_isAutoMode)
            {
                var path = FindSafePathWithTail(_food);

                if (path != null && path.Count > 1)
                {
                    _nextDirection = GetDirectionFromPoints(head, path[1]);
                }
                else
                {
                    _nextDirection = GetHamiltonianDirection(head);
                }

            }

            _currentDirection = _nextDirection;

            var newHead = CalculateNewHead(_currentDirection,head);

            if (IsCollision(newHead))
            {
                GameOver();
                return;
            }

            AddSegment(newHead);

            if (newHead == _food)
            {
                HandleFoodEaten();
            }
            else
            {
                RemoveTail();
            }
            RenderGame();
        }

        private Point CalculateNewHead(Direction currentDirection, Point head)
        {
            return currentDirection switch
            {
                Direction.Up => new Point(head.X, head.Y - 1),
                Direction.Down => new Point(head.X, head.Y + 1),
                Direction.Left => new Point(head.X - 1, head.Y),
                Direction.Right => new Point(head.X + 1, head.Y),
                _ => head
            };
        }

        private bool IsCollision(Point newHead)
        {
            // 检查边界和自身碰撞
            return newHead.X < 0 || newHead.X >= GridSize ||
                   newHead.Y < 0 || newHead.Y >= GridSize ||
                   _bodyPositions.Contains(newHead);
        }

        private void HandleFoodEaten()
        {
            _score += 10;
            UpdateScoreDisplay();
            GenerateFood();
        }

        private void RemoveTail()
        {
            var tail = _snakeBody.First();
            _snakeBody.RemoveFirst();
            _bodyPositions.Remove(tail);
        }

        private bool IsValidTurn(Direction current, Direction next)
        {
            // 不能反向走
            return (current, next) switch
            {
                (Direction.Up, Direction.Down) => false,
                (Direction.Down, Direction.Up) => false,
                (Direction.Left, Direction.Right) => false,
                (Direction.Right, Direction.Left) => false,
                _ => true
            };
        }

        private void GenerateFood()
        {
            // 随机生成食物
            var emptyCells = new List<Point>();
            for (int x = 0; x < GridSize; x++)
            {
                for (int y = 0; y < GridSize; y++)
                {
                    var point = new Point(x, y);
                    if (!_bodyPositions.Contains(point))
                        emptyCells.Add(point);
                }
            }

            if (emptyCells.Count > 0)
            {
                _food = emptyCells[new Random().Next(emptyCells.Count)];
            }
            else
            {
                Victory();
            }
        }

        private void GameOver()
        {
            EndGame("游戏结束! 最终分数: " + _score);
        }
        private void Victory()
        {
            EndGame("恭喜获胜! 最终分数: " + _score);
        }

        private void EndGame(string message)
        {
            _isGameRunning = false;
            CompositionTarget.Rendering -= GameLoop;
            GameOverText.Text = message;
            OverlayPanel.Visibility = Visibility.Visible;
        }
        private void UpdateScoreDisplay()
        {
            ScoreText.Text = $"分数: {_score}";
        }
        #endregion

        #region 自动寻路

        private Direction GetDirectionFromPoints(Point from, Point to)
        {
            if (to.X == from.X)
            {
                if (to.Y == from.Y - 1) return Direction.Up;
                if (to.Y == from.Y + 1) return Direction.Down;
            }
            else if (to.Y == from.Y)
            {
                if (to.X == from.X - 1) return Direction.Left;
                if (to.X == from.X + 1) return Direction.Right;
            }

            return _nextDirection;
        }
        

        // TODO
        private Direction GetHamiltonianDirection(Point current)
        {
            // 以“蛇形遍历”规则（横向扫行）为例
            if (current.Y % 2 == 0) // 偶数行，从左往右
            {
                if (current.X < GridSize - 1)
                    return Direction.Right;
                else
                    return Direction.Down;
            }
            else // 奇数行，从右往左
            {
                if (current.X > 0)
                    return Direction.Left;
                else
                    return Direction.Down;
            }
        }

        private IEnumerable<Point> GetNeighbors(Point p)
        {
            int[] dx = { -1, 1, 0, 0 };
            int[] dy = { 0, 0, -1, 1 };
            for (int i = 0; i < 4; i++)
            {
                var neighbor = new Point(p.X + dx[i], p.Y + dy[i]);
                if (IsInsideMap(neighbor))
                {
                    yield return neighbor;
                }
            }
        }

        private bool IsInsideMap(Point p)
        {
            return p.X >= 0 && p.X < GridSize && p.Y >= 0 && p.Y < GridSize;
        }

        private double Heuristic(Point a, Point b)
        {
            var dx = Math.Abs(a.X - b.X);
            var dy = Math.Abs(a.Y - b.Y);
            return dx + dy;
        }

        private List<Point>? FindSafePathWithTail(Point food)
        {
            // 计算从蛇头到食物的路径
            var path = FindPath(_snakeBody.Last(), food, new HashSet<Point>(_bodyPositions));
            if (path == null)
                return null;

            // 模拟蛇体移动
            var virtualSnake = new LinkedList<Point>(_snakeBody);
            var virtualOccupied = new HashSet<Point>(_bodyPositions);
            foreach (var step in path.Skip(1)) // 跳过当前蛇头
            {
                virtualSnake.AddLast(step);
                virtualOccupied.Add(step);
                if (step != food)
                {
                    Point tail = virtualSnake.First();
                    virtualSnake.RemoveFirst();
                    virtualOccupied.Remove(tail);
                }
            }

            Point newHead = virtualSnake.Last();
            Point newTail = virtualSnake.First();
            // 新状态下，不允许穿越蛇身（此时蛇尾已固定，因为下一步不会移除它）
            var occupiedForTailCheck = new HashSet<Point>(virtualOccupied);
            occupiedForTailCheck.Remove(newTail);

            // 检查是否存在路径从新蛇头到新蛇尾（安全检测）
            var pathToTail = FindPath(newHead, newTail, occupiedForTailCheck);
            if (pathToTail == null)
            {
                var farPoint = FindLongestSafePath(_snakeBody.Last(), food);// Todo 
                return farPoint;
            }
            return path;
        }

        private List<Point>? FindLongestSafePath(Point head, Point food)
        {
            List<Point>? bestPath = null;
            double maxDistance = -1;

            for (int x = 0; x < GridSize; x++)
            {
                for (int y = 0; y < GridSize; y++)
                {
                    Point target = new Point(x, y);
                    if (!_bodyPositions.Contains(target))
                    {
                        var path = FindPath(head, target, _bodyPositions);
                        if (path != null)
                        {
                            var dist = Math.Abs(target.X - food.X) + Math.Abs(target.Y - food.Y);
                            if (dist > maxDistance)
                            {
                                maxDistance = dist;
                                bestPath = path;
                            }
                        }
                    }
                }
            }

            return bestPath;
        }
        private List<Point>? FindPath(Point start, Point target, HashSet<Point> occupied)
        {
            var openSet = new PriorityQueue<Point, double>();
            var cameFrom = new Dictionary<Point, Point>();
            var gScore = new Dictionary<Point, double> { [start] = 0 };
            var fScore = new Dictionary<Point, double> { [start] = Heuristic(start, target) };

            openSet.Enqueue(start, fScore[start]);

            while (openSet.Count > 0)
            {
                Point current = openSet.Dequeue();
                if (current == target)
                {
                    var path = new List<Point> { current };
                    while (cameFrom.ContainsKey(current))
                    {
                        current = cameFrom[current];
                        path.Add(current);
                    }
                    path.Reverse();
                    return path;
                }

                foreach (Point neighbor in GetNeighbors(current))
                {
                    if (occupied.Contains(neighbor)) continue; // 障碍

                    var tentativeG = gScore[current] + 1;
                    if (!gScore.ContainsKey(neighbor) || tentativeG < gScore[neighbor])
                    {
                        cameFrom[neighbor] = current;
                        gScore[neighbor] = tentativeG;
                        fScore[neighbor] = tentativeG + Heuristic(neighbor, target);
                        if (!openSet.UnorderedItems.Any(x => x.Element == neighbor))
                            openSet.Enqueue(neighbor, fScore[neighbor]);
                    }
                }
            }

            return null;
        }
        #endregion

        #region 渲染

        private unsafe void RenderGame()
        {
            try
            {
                _gameBitmap.Lock();
                uint* backBuffer = (uint*)_gameBitmap.BackBuffer;
                int width = _gameBitmap.PixelWidth;

                for (int i = 0; i < width * _gameBitmap.PixelHeight; i++)
                    backBuffer[i] = 0xFF000000;

                DrawCell(_food.X, _food.Y, 0xFFFF0000, backBuffer, width);

                bool isHead = true;
                foreach (var segment in _snakeBody.Reverse())
                {
                    DrawCell(segment.X, segment.Y,
                            isHead ? 0xFF00AA00 : 0xFF00FF00,
                            backBuffer, width);
                    isHead = false;
                }

                _gameBitmap.AddDirtyRect(new Int32Rect(0, 0, width, _gameBitmap.PixelHeight));
            }
            finally
            {
                _gameBitmap.Unlock();
            }
        }

        private unsafe void DrawCell(double x, double y, uint color, uint* buffer, int stride)
        {
            int startX = (int)(x * CellSize);
            int startY = (int)(y * CellSize);

            for (int dy = 0; dy < CellSize; dy++)
            {
                int row = startY + dy;
                for (int dx = 0; dx < CellSize; dx++)
                {
                    buffer[row * stride + startX + dx] = color;
                }
            }
        }
        #endregion

        #region 事件
        protected override void OnKeyDown(KeyEventArgs e)
        {
            Direction? newDir = null;
            switch (e.Key)
            {
                case Key.Space:
                    if (!_isGameRunning)
                        RestartGame();
                    return;
                case Key.Enter:
                    _isAutoMode = !_isAutoMode;
                    AutoMode.Visibility = _isAutoMode ? Visibility.Visible : Visibility.Collapsed;
                    return;
                case Key.Up:
                    newDir = Direction.Up;
                    break;
                 case Key.Down:
                    newDir = Direction.Down;
                    break;
                case Key.Left:
                    newDir = Direction.Left;
                    break;
                case Key.Right:
                    newDir = Direction.Right;
                    break;
            }

            if (newDir.HasValue && IsValidTurn(_currentDirection, newDir.Value))
            {
                _nextDirection = newDir.Value;
            }
        }

        private void RestartButton_Click(object sender, RoutedEventArgs e)
        {
            RestartGame();
        }

        private void RestartGame()
        {
            CompositionTarget.Rendering -= GameLoop;
            ClearRendering();
            InitializeGame();
            OverlayPanel.Visibility = Visibility.Collapsed;
        }

        private void ClearRendering()
        {
            unsafe
            {
                _gameBitmap.Lock();
                int* buffer = (int*)_gameBitmap.BackBuffer;
                for (int i = 0; i < _gameBitmap.PixelWidth * _gameBitmap.PixelHeight; i++)
                    buffer[i] = unchecked((int)0xFF000000);
                _gameBitmap.AddDirtyRect(new Int32Rect(0, 0, _gameBitmap.PixelWidth, _gameBitmap.PixelHeight));
                _gameBitmap.Unlock();
            }
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            CompositionTarget.Rendering -= GameLoop;
            mainWindow?.MainFrame?.Navigate(new HomePage(mainWindow.MainFrame));
        }
        #endregion
    }
}
