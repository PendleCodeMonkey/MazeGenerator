using System;
using System.Collections.Generic;
using System.Linq;

namespace PendleCodeMonkey.MazeGenerator
{
	// Class that implements Wilson's algorithm for maze generation (using loop-erased random walks)
	// See https://en.wikipedia.org/wiki/Maze_generation_algorithm#Wilson's_algorithm
	internal class Wilsons
	{
		#region data

		private readonly Random _rand = new();

		#endregion

		#region enumerations

		/// <summary>
		/// Enumeration of the possible states of each cell in the maze's grid.
		/// </summary>
		/// <remarks>
		/// The WorkingPath value is used to indicate cells that are part of the current random walk.
		/// </remarks>
		internal enum CellState
		{
			Path,
			Wall,
			WorkingPath
		}

		/// <summary>
		/// Enumeration of the four possible directions of movement from a cell to one of its neighbouring cells.
		/// </summary>
		private enum Directions
		{
			North,
			South,
			East,
			West
		}

		#endregion

		#region constructors

		/// <summary>
		/// Constructs a maze of the default dimensions (63 x 63)
		/// </summary>
		public Wilsons() : this(63, 63)
		{
		}

		/// <summary>
		/// Constructs a maze of the specified height and width
		/// </summary>
		public Wilsons(int height, int width)
		{
			Maze = new();

			// Set the height and width (ensuring that they are odd because the maze generation only works
			// correctly with odd dimensions.)
			Height = height + (height % 2 == 0 ? 1 : 0);
			Width = width + (width % 2 == 0 ? 1 : 0);
		}

		#endregion

		#region properties

		/// <summary>
		/// Gets the data for the maze (a list [for the rows] of lists [for the columns] of CellState objects)
		/// </summary>
		internal List<List<CellState>> Maze { get; private set; }

		/// <summary>
		/// Gets the height of the maze
		/// </summary>
		internal int Height { get; private set; }

		/// <summary>
		/// Gets the width of the maze.
		/// </summary>
		internal int Width { get; private set; }

		#endregion

		#region methods

		/// <summary>
		/// Generates a maze using Wilson's algorithm.
		/// </summary>
		internal void GenerateMaze()
		{
			// Initialize the maze (so initially every cell is a wall, ready for paths to be cut out)
			InitMaze();

			// Randomly select an initial target cell in the maze and mark it as part of a path.
			var (x, y) = RandomCoord();
			Maze[y][x] = CellState.Path;

			// Keep working until the maze has been completely generated.
			while (!IsComplete())
			{
				// Randonly select a cell (that is a wall) from which to start the path generation.
				do
				{
					(x, y) = RandomCoord();
				} while (Maze[y][x] != CellState.Wall);

				// Mark this start cell as being part of the working path generation.
				Maze[y][x] = CellState.WorkingPath;

				// Create a list to hold the coordinates of cells that make up the current working path (with
				// the coordinates of the start cell as the first entry of the list)
				List<(int x, int y)> path = new()
				{
					(x, y)
				};

				// Keep working until we encounter a cell that is already part of a path (i.e. we keep randomly walking
				// between cells until we reach a cell on an existing path, at which point the current working path becomes part
				// of the overall path through the maze).
				while (Maze[path.Last().y][path.Last().x] != CellState.Path)
				{
					// Get the coordinates of the last entry in the path.
					var last = path.Last();
					// Get a list of directions that it is valid to move in from this last cell.
					var dirs = ValidMoveDirections(last.x, last.y).ToList();
					// Randomly select one of the directions.
					var dir = dirs[_rand.Next(dirs.Count)];
					// And determine the coordinates of the neighbouring cell in that direction.
					var neighbour = GetCoordInDirection(dir, last);

					// Add this neighbour cell to the working path.
					path.Add(neighbour);

					// And mark the square between these two neighbouring cells as being part of the working path.
					Maze[(neighbour.y + last.y) / 2][(neighbour.x + last.x) / 2] = CellState.WorkingPath;

					// If the neighbouring cell that we have moved to is already part of a path then the entire current working path
					// becomes part of the overall path through the maze.
					if (Maze[neighbour.y][neighbour.x] == CellState.Path)
					{
						// Scan every cell in the maze (across the entire height and width)
						for (var i = 0; i < Height; i++)
						{
							for (var j = 0; j < Width; j++)
							{
								// If the cell is part of the current working path
								if (Maze[i][j] == CellState.WorkingPath)
								{
									// then change it to be part of the final overall path through the maze.
									Maze[i][j] = CellState.Path;
								}
							}
						}
					}
					else
					{
						// Mark the neighbouring cell as being part of the current working path.
						Maze[neighbour.y][neighbour.x] = CellState.WorkingPath;
						// Locate the neighbour cell in the path list
						var loc = path.IndexOf(neighbour);
						// If it is not the last entry in the path list then the working path has backtracked (i.e.
						// as part of its random walk it has looped back onto a cell that is already in the working path)
						// When this happens, we remove the loop from the working path.
						if (loc != path.Count - 1)
						{
							// Get a list of the cells that form the loop that we need to remove.
							var toBeRemoved = path.Skip(loc + 1).Take(path.Count - loc - 1).ToList();
							// Remove the loop of cells from the path (i.e. only keep the cells in the working path up to the
							// point where the loop starts)
							path = path.Take(loc + 1).ToList();
							// Mark the square between the two neighbouring cells as being a wall.
							Maze[(neighbour.y + last.y) / 2][(neighbour.x + last.x) / 2] = CellState.Wall;
							// Retrieve the coordinates of the last entry in the shortened path.
							last = path.Last();

							// Handle each cell that forms part of the loop that we are removing (in reverse order)
							for (var i = toBeRemoved.Count - 1; i >= 0; i--)
							{
								// Get the coordinates of the cell to be removed from the path
								var removeCell = toBeRemoved[i];
								// and the coordinates of the cell neighbouring it in the loop being removed.
								var removeCellNeighbour = i > 0 ? toBeRemoved[i - 1] : last;

								if (i != toBeRemoved.Count - 1)
								{
									// Set the state of the cell being removed from the path to be a wall.
									Maze[removeCell.y][removeCell.x] = CellState.Wall;
								}

								// Mark the square between the two neighbouring cells as being a wall.
								Maze[(removeCell.y + removeCellNeighbour.y) / 2][(removeCell.x + removeCellNeighbour.x) / 2] = CellState.Wall;
							}
						}
					}
				}
			}

			// Cut the entrance to the maze.
			Maze[0][1] = CellState.Path;
			// And finally, cut the exit from the maze.
			Maze[Height - 1][Width - 2] = CellState.Path;
		}

		/// <summary>
		/// Initialize the maze data (in preparation for maze generation)
		/// </summary>
		/// <remarks>
		/// This sets the state of each cell in the maze to be a wall, ready for the maze generation
		/// algorithm to cut paths through.
		/// </remarks>
		private void InitMaze()
		{
			for (int i = 0; i < Height; i++)
			{
				Maze.Add(Enumerable.Repeat(CellState.Wall, Width).ToList());
			}
		}

		/// <summary>
		/// Determine if the maze has been completely generated.
		/// </summary>
		/// <returns><c>true</c> if the maze generation is complete, otherwise <c>false</c>.</returns>
		private bool IsComplete()
		{
			for (var i = 1; i < Height; i += 2)
			{
				for (var j = 1; j < Width; j += 2)
				{
					if (Maze[i][j] != CellState.Path)
					{
						return false;
					}
				}
			}
			return true;
		}

		/// <summary>
		/// Get the coordinates of a random cell within the maze.
		/// </summary>
		/// <returns>A tuple containing the X and Y coordinates of the random cell.</returns>
		private (int x, int y) RandomCoord() => ((_rand.Next(Width / 2) * 2) + 1, (_rand.Next(Height / 2) * 2) + 1);

		/// <summary>
		/// Get the coordinates of the cell that are reached when moving in the specified direction from a specified cell's coordinates.
		/// </summary>
		/// <param name="direction">The direction of travel.</param>
		/// <param name="coord">The coordinates of the cell from which we are moving.</param>
		/// <returns>A tuple containing the X and Y coordinates of the neighbouring cell in the specified direction.</returns>
		private static (int x, int y) GetCoordInDirection(Directions direction, (int x, int y) coord)
		{
			return direction switch
			{
				Directions.North => (coord.x, coord.y - 2),
				Directions.South => (coord.x, coord.y + 2),
				Directions.East => (coord.x + 2, coord.y),
				_ => (coord.x - 2, coord.y),
			};
		}

		/// <summary>
		/// Determine the valid directions in which a neighbouring cell can be reached from the cell at
		/// the specified coordinates.
		/// </summary>
		/// <param name="x">X coordinate of the cell whose neighbours we are trying to find.</param>
		/// <param name="y">X coordinate of the cell whose neighbours we are trying to find.</param>
		/// <returns>A sequence containing the valid directions in which a neighbouring cell can be reached.</returns>
		private IEnumerable<Directions> ValidMoveDirections(int x, int y)
		{
			if (x + 2 < Width)
			{
				yield return Directions.East;
			}
			if (x - 2 > 0)
			{
				yield return Directions.West;
			}
			if (y - 2 > 0)
			{
				yield return Directions.North;
			}
			if (y + 2 < Height)
			{
				yield return Directions.South;
			}
		}

		#endregion
	}
}
