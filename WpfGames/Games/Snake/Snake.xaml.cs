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
            AddSegment(new Point(5, 10));
            AddSegment(new Point(6, 10));
            AddSegment(new Point(7, 10));

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
            if (elapsed < BaseUpdateInterval) return;

            _lastUpdateTime = DateTime.Now;
            
            var head = _snakeBody.Last.Value;
            // 自动寻路
            if (_isAutoMode)
            {
                if (_snakeBody.Count > 20)
                {
                    if (GetSystematicDirection() is Direction sysDir)
                    {
                        _nextDirection = sysDir;
                    }
                    else
                    {
                        // 如果系统路径规划失败，回退到安全转向
                        _nextDirection = GetSafeTurnDirection() ;
                    }
                }
                else
                {
                    _nextDirection = GetSafeTurnDirection();
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
            if (_snakeBody.First == null)
                return;
            var tail = _snakeBody.First.Value;
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
        // 获取食物方向
        private Direction GetOptimalDirection(Point head, Point food)
        {
            var dx = food.X - head.X;
            var dy = food.Y - head.Y;

            // 优先选择距离差较大的轴向
            if (Math.Abs(dx) > Math.Abs(dy))
            {
                return dx > 0 ? Direction.Right : Direction.Left;
            }
            else
            {
                return dy > 0 ? Direction.Down : Direction.Up;
            }
        }

        // 检查direction方向是否安全
        private bool IsDirectionSafe(Direction direction, Point head)
        {
            var testPos = CalculateNewHead(direction, head);
            return !IsCollision(testPos);
        }

        //获取安全转向方向（优先指向食物，其次选择其他安全方向）
        private Direction GetSafeTurnDirection()
        {
            if (_snakeBody.Last == null)
            {
                return _nextDirection;
            }
            var head = _snakeBody.Last.Value;
            var currentDir = _currentDirection;

            // 1. 优先尝试直接转向食物方向
            if (GetOptimalDirection(head, _food) is Direction targetDir &&
                targetDir != currentDir &&
                IsDirectionSafe(targetDir, head))
            {
                return targetDir;
            }

            // 2. 次优先选择与食物方向夹角≤90度的安全方向
            var candidateDirections = GetCandidateDirections(currentDir);
            foreach (var dir in candidateDirections.OrderBy(d =>
                GetDirectionPriority(d, head, _food)))
            {
                if (IsDirectionSafe(dir, head))
                {
                    return dir;
                }
            }

            // 3. 最后尝试任何安全方向（保底逻辑）
            foreach (var dir in Enum.GetValues(typeof(Direction)).Cast<Direction>())
            {
                if (dir != currentDir && IsDirectionSafe(dir, head))
                {
                    return dir;
                }
            }

            // GG
            return _nextDirection;
        }

        // 获取当前方向的候选转向方向（排除反向）
        private List<Direction> GetCandidateDirections(Direction currentDir)
        {
            return currentDir switch
            {
                Direction.Up => new List<Direction> { Direction.Right, Direction.Left, Direction.Down },
                Direction.Down => new List<Direction> { Direction.Left, Direction.Right, Direction.Up },
                Direction.Left => new List<Direction> { Direction.Up, Direction.Down, Direction.Right },
                Direction.Right => new List<Direction> { Direction.Down, Direction.Up, Direction.Left },
                _ => Enum.GetValues(typeof(Direction)).Cast<Direction>().ToList()
            };
        }

        /// 方向优先级计算（数值越小优先级越高）
        private int GetDirectionPriority(Direction dir, Point head, Point food)
        {
            // 计算与食物方向的吻合度
            var dx = food.X - head.X;
            var dy = food.Y - head.Y;

            int priority = 0;

            // 完全匹配方向
            if ((dir == Direction.Right && dx > 0) ||
                (dir == Direction.Left && dx < 0) ||
                (dir == Direction.Down && dy > 0) ||
                (dir == Direction.Up && dy < 0))
            {
                priority -= 100; // 最高优先级
            }

            // 次优方向（至少轴向正确）
            if ((dir == Direction.Right || dir == Direction.Left) && Math.Abs(dx) > Math.Abs(dy))
            {
                priority -= 50;
            }
            else if ((dir == Direction.Up || dir == Direction.Down) && Math.Abs(dy) > Math.Abs(dx))
            {
                priority -= 50;
            }

            // 添加随机因素避免固定模式
            priority += new Random().Next(10);
            return priority;
        }


        /// <summary>
        /// 地毯式搜索路径规划
        /// </summary>
        private Direction? GetSystematicDirection()
        {
            if(_snakeBody.Last == null || _snakeBody.First == null)
            {
                return _nextDirection;
            }
            var head = _snakeBody.Last.Value;
            var tail = _snakeBody.First.Value;

            // 1. 计算未被占据的空间
            var emptyCells = GetEmptyCells().ToList();

            // 2. 使用BFS找到最近的未探索区域
            var target = FindNearestUnexplored(head, emptyCells);
            if (!target.HasValue) return null;

            // 3. 使用A*算法计算安全路径
            var path = FindPath(head, target.Value, emptyCells);
            if (path == null || path.Count == 0) return null;

            // 4. 转换为移动方向
            return GetDirectionFromPath(head, path[0]);
        }

        /// <summary>
        /// 获取所有空单元格（包括蛇尾）
        /// </summary>
        private IEnumerable<Point> GetEmptyCells()
        {
            for (int x = 0; x < GridSize; x++)
            {
                for (int y = 0; y < GridSize; y++)
                {
                    var point = new Point(x, y);
                    if (!_bodyPositions.Contains(point) || point == _snakeBody.First?.Value)
                        yield return point;
                }
            }
        }

        /// <summary>
        /// 寻找最近的未探索区域（BFS实现）
        /// </summary>
        private Point? FindNearestUnexplored(Point start, List<Point> emptyCells)
        {
            var visited = new HashSet<Point> { start };
            var queue = new Queue<Point>();
            queue.Enqueue(start);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();

                // 如果当前点是蛇尾或未被蛇身覆盖
                if (current == _snakeBody.First?.Value || !_bodyPositions.Contains(current))
                    return current;

                foreach (var neighbor in GetNeighbors(current))
                {
                    if (emptyCells.Contains(neighbor) && !visited.Contains(neighbor))
                    {
                        visited.Add(neighbor);
                        queue.Enqueue(neighbor);
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// A*路径查找算法
        /// </summary>
        private List<Point>? FindPath(Point start, Point end, List<Point> walkableCells)
        {
            var openSet = new PriorityQueue<Point, float>();
            openSet.Enqueue(start, 0);

            var cameFrom = new Dictionary<Point, Point>();
            var gScore = new Dictionary<Point, float> { [start] = 0 };
            var fScore = new Dictionary<Point, float> { [start] = Heuristic(start, end) };

            while (openSet.Count > 0)
            {
                var current = openSet.Dequeue();
                if (current == end)
                    return ReconstructPath(cameFrom, current);

                foreach (var neighbor in GetNeighbors(current))
                {
                    if (!walkableCells.Contains(neighbor)) continue;

                    float tentativeGScore = gScore[current] + 1;
                    if (!gScore.ContainsKey(neighbor) || tentativeGScore < gScore[neighbor])
                    {
                        cameFrom[neighbor] = current;
                        gScore[neighbor] = tentativeGScore;
                        fScore[neighbor] = tentativeGScore + Heuristic(neighbor, end);
                        openSet.Enqueue(neighbor, fScore[neighbor]);
                    }
                }
            }
            return null;
        }

        // 辅助方法
        private float Heuristic(Point a, Point b)
            => Convert.ToInt32(Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y));

        private List<Point>? ReconstructPath(Dictionary<Point, Point> cameFrom, Point current)
        {
            var path = new List<Point> { current };
            while (cameFrom.ContainsKey(current))
            {
                current = cameFrom[current];
                path.Add(current);
            }
            path.Reverse();
            return path.Count > 1 ? path : null;
        }

        private IEnumerable<Point> GetNeighbors(Point p)
        {
            if (p.X > 0) yield return new Point(p.X - 1, p.Y);
            if (p.X < GridSize - 1) yield return new Point(p.X + 1, p.Y);
            if (p.Y > 0) yield return new Point(p.X, p.Y - 1);
            if (p.Y < GridSize - 1) yield return new Point(p.X, p.Y + 1);
        }

        private Direction? GetDirectionFromPath(Point from, Point to)
        {
            if (to.X > from.X) return Direction.Right;
            if (to.X < from.X) return Direction.Left;
            if (to.Y > from.Y) return Direction.Down;
            if (to.Y < from.Y) return Direction.Up;
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
