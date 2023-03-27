using System;
using System.Collections.Generic;
using System.Linq;
using static PendleCodeMonkey.MazeGenerator.Wilsons;

namespace PendleCodeMonkey.MazeGenerator
{
	/// <summary>
	/// Class that implements an A* pathfinding algorithm that is used to find a path through (i.e. solve) a supplied maze.
	/// See https://en.wikipedia.org/wiki/A*_search_algorithm
	/// </summary>
	internal class Pathfinder
	{
		private readonly List<List<CellState>> _maze;
		private readonly int _mazeHeight;
		private readonly int _mazeWidth;
		private readonly List<Node>? _nodeList = null;

		/// <summary>
		/// Constructor for the Pathfinder class.
		/// </summary>
		/// <param name="maze">The maze for which a [solution] path is to be calculated.</param>
		internal Pathfinder(List<List<CellState>> maze)
		{
			_maze = maze;
			_mazeHeight = _maze.Count;
			_mazeWidth = _maze.First().Count;
			_nodeList = new List<Node>();

			Init();
		}

		/// <summary>
		/// Run the A* pathfinder and return the calculated path.
		/// </summary>
		/// <returns>A list containing tuples of the X and Y coordinates of each point along the calculated path.</returns>
		internal List<(int x, int y)>? FindPath()
		{
			if (_nodeList == null)
			{
				return null;
			}

			List<Node>? closedSet = new();
			List<Node>? openSet = new();

			// We start at the entrance to the maze (which is located in top-left corner).
			Node startNode = _nodeList[1];

			// And end at the exit from the maze (in bottom-right corner).
			Node endNode = _nodeList[(_mazeHeight * _mazeWidth) - 2];

			Node currentNode = startNode;
			closedSet.Add(currentNode);

			do
			{
				// Handle each node that is adjacent to the current node (i.e. each node that we could
				// move to from the current node)
				foreach (Node node in currentNode.AdjacentNodeList!)
				{
					// Avoid backtracking into a node that has already been encountered.
					if (closedSet.Contains(node))
					{
						continue;
					}

					// Set this node's parent to the current node.
					node.ParentNode = currentNode;
					// Calculate the heuristic estimated cost for this node (the Manhattan Distance between this node
					// and the end node)
					node.H = CalcDistance(node, endNode);
					// Calculate the actual cost of the path from the start node to this node (which is equal to the
					// cost to the parent node plus the distance between this node and the parent node).
					node.G = node.ParentNode.G + CalcDistance(node, node.ParentNode /*currentNode*/);
					// F is the sum of the actual cost of the path from the start node to this node and the
					// heuristic estimated cost for this node (i.e. the sum of G and H).
					node.F = node.G + node.H;
					// Add this node to the openSet list.
					openSet.Add(node);
				}

				// Find the node in the openSet list with the lowest F value and set it to be the current node.
				currentNode = openSet.MinBy(x => x.F)!;

				// Remove this new current node from the openSet list
				openSet.Remove(currentNode);
				// and add it to the closedSet list.
				closedSet.Add(currentNode);

				// Keep going until we reach the end node.
			} while (currentNode != endNode);

			// Generate the path (as a Stack containing tuples of X and Y coordinates)
			// We start at the end node and work back through each node's parent until we get to a node that has no parent (which
			// is the node at the start of the calculated path)
			// As we're using a Stack and handling the nodes in reverse order (i.e. end -> start) then the stack will end
			// up containing the nodes on the path in the correct order (start -> end)
			Stack<(int x, int y)> path = new();
			Node? n = endNode;
			while (n != null)
			{
				// Push this node's X and Y coordinates onto the stack (as a tuple)
				path.Push((n.X, n.Y));
				// Move onto this node's parent (if any)
				n = n.ParentNode;
			}
			// Return the calculated path as a list of tuples of X and Y coordinates.
			return path.ToList();
		}

		/// <summary>
		/// Perform initialization required for the A* pathfinder functionality.
		/// </summary>
		private void Init()
		{
			// Create a Node entry for each cell in the maze, adding them to _nodeList.
			for (int row = 0; row < _mazeHeight; ++row)
			{
				for (int col = 0; col < _mazeWidth; ++col)
				{
					_nodeList!.Add(new() { X = col, Y = row });
				}
			}

			// Set up the adjacent node data for each cell in the maze.
			for (int row = 0; row < _mazeHeight; ++row)
			{
				for (int col = 0; col < _mazeWidth; ++col)
				{
					// Get the Node corresponding to this row and column.
					Node node = _nodeList![(row * _mazeWidth) + col];
					// Create an empty list to hold the nodes that are found to be adjacent to this node.
					node.AdjacentNodeList = new List<Node>();

					// Add node that is adjacent to the left
					if (col > 0 && _maze[row][col - 1] == CellState.Path)
					{
						node.AdjacentNodeList.Add(_nodeList[(row * _mazeWidth) + col - 1]);
					}

					// Add node that is adjacent to the right
					if (col < _mazeWidth - 1 && _maze[row][col + 1] == CellState.Path)
					{
						node.AdjacentNodeList.Add(_nodeList[(row * _mazeWidth) + col + 1]);
					}

					// Add node that is adjacent above
					if (row > 0 && _maze[row - 1][col] == CellState.Path)
					{
						node.AdjacentNodeList.Add(_nodeList[((row - 1) * _mazeWidth) + col]);
					}

					// Add node that is adjacent below
					if (row < _mazeHeight - 1 && _maze[row + 1][col] == CellState.Path)
					{
						node.AdjacentNodeList.Add(_nodeList[((row + 1) * _mazeWidth) + col]);
					}
				}
			}
		}

		/// <summary>
		/// Calculate the distance between two nodes.
		/// </summary>
		/// <remarks>
		/// Uses the Manhattan Distance method of calculating the distance between two cells.
		/// </remarks>
		/// <param name="start">Start node.</param>
		/// <param name="end">End node.</param>
		/// <returns>The distance between the two nodes.</returns>
		private static int CalcDistance(Node start, Node end) => CalcDistance(start.X, start.Y, end.X, end.Y);

		/// <summary>
		/// Calculate the distance between two locations.
		/// </summary>
		/// <remarks>
		/// Uses the Manhattan Distance method of calculating the distance between two cells.
		/// </remarks>
		/// <param name="startX">Start X position.</param>
		/// <param name="startY">Start Y position.</param>
		/// <param name="endX">End X position.</param>
		/// <param name="endY">End Y position.</param>
		/// <returns>The distance between two locations.</returns>
		private static int CalcDistance(int startX, int startY, int endX, int endY) => Math.Abs(startX - endX) + Math.Abs(startY - endY);

		/// <summary>
		/// Class that stores the data for an individual node.
		/// </summary>
		private class Node
		{
			#region constructor

			/// <summary>
			/// Constructor for the Node class.
			/// </summary>
			internal Node()
			{
				ParentNode = null;
				X = 0;
				Y = 0;
				AdjacentNodeList = null;
				F = 0;
				G = 0;
				H = 0;
			}

			#endregion

			#region properties

			/// <summary>
			/// Gets or sets this node's parent node.
			/// </summary>
			internal Node? ParentNode { get; set; }

			/// <summary>
			/// Gets or sets the X coordinate of this node.
			/// </summary>
			internal int X { get; set; }

			/// <summary>
			/// Gets or sets the Y coordinate of this node.
			/// </summary>
			internal int Y { get; set; }

			/// <summary>
			/// Gets or sets a list of the nodes that are adjacent to this node.
			/// </summary>
			internal List<Node>? AdjacentNodeList { get; set; }

			/// <summary>
			/// Gets or sets a value that is sum of the cost of the path from the start node to this node and the
			/// estimated cost of the cheapest path from this node to the end node (i.e. the sum of the G and H properties).
			/// </summary>
			internal int F { get; set; }

			/// <summary>
			/// Gets or sets the actual (calculated) cost of the path from the start node to this node.
			/// </summary>
			internal int G { get; set; }

			/// <summary>
			/// Gets or sets the heuristic estimated cost of the cheapest path from this
			/// node to the end node (which is calculated using the Manhattan Distance method).
			/// </summary>
			internal int H { get; set; }

			#endregion
		}
	}
}
