using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using static PendleCodeMonkey.MazeGenerator.Wilsons;

namespace PendleCodeMonkey.MazeGenerator
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{

		private List<List<CellState>>? _maze;

		public MainWindow()
		{
			_maze = null;
			InitializeComponent();
		}

		private void Window_Loaded(object sender, RoutedEventArgs e)
		{
			SolveButton.IsEnabled = false;
		}

		private void GenerateButton_Click(object sender, RoutedEventArgs e)
		{
			int mazeHeight = 63;
			int mazeWidth = 63;

			Wilsons wilsons = new(mazeHeight, mazeWidth);
			Mouse.OverrideCursor = Cursors.Wait;
			wilsons.GenerateMaze();
			Mouse.OverrideCursor = null;

			_maze = wilsons.Maze;

			mazeGrid.Rows = wilsons.Height;
			mazeGrid.Columns = wilsons.Width;

			mazeGrid.Children.Clear();

			for (var row = 0; row < mazeGrid.Rows; row++)
			{
				for (int col = 0; col < mazeGrid.Columns; col++)
				{
					var isBlack = _maze[row][col] == CellState.Wall;
					var square = new Rectangle { Fill = isBlack ? Brushes.Black : Brushes.White };
					mazeGrid.Children.Add(square);
				}
			}

			SolveButton.IsEnabled = true;
		}

		private void SolveButton_Click(object sender, RoutedEventArgs e)
		{
			if (_maze == null)
			{
				return;
			}

			Pathfinder pf = new(_maze);
			Mouse.OverrideCursor = Cursors.Wait;
			var solution = pf.FindPath();
			Mouse.OverrideCursor = null;
			if (solution != null)
			{
				foreach (var (x, y) in solution)
				{
					Rectangle child = (Rectangle)mazeGrid.Children[y * mazeGrid.Columns + x];
					child.Fill = Brushes.OrangeRed;
				}
			}
		}
	}
}
