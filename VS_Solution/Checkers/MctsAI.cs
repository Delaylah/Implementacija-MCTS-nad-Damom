using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CheckersBoard
{
    public class MctsAI : IAIAlgorithm
    {
        #region Dependencies

        private readonly IListHelper listHelper;
        private readonly IRandomService randomService;

        #endregion

        public static TimeSpan MAX_DURATION = TimeSpan.FromMilliseconds(500);
        public static int MAX_TURNS = 200;
        //public static int MAX_FINISHED_GAMES = 10;

        public MctsAI(IListHelper listHelper, IRandomService randomService)
        {
            this.listHelper = listHelper;
            this.randomService = randomService;
        }

        public Move GetMove(CheckerBoard currentBoard, Player invokingPlayer, int turn = 0)
        {
            var root = new MctsNode(null, currentBoard);
            root.Name = "0";
            MctsNode currentNode = root;
            var visitedNodes = new List<MctsNode>();

            var executionStopwatch = new Stopwatch();
            executionStopwatch.Start();

            var iterations = 0;
            while (executionStopwatch.Elapsed <= MAX_DURATION)//(++iterations <= 1000) //
            {
                iterations++;

                visitedNodes.Add(currentNode);
                currentNode.Visits++;

                // If current node is expanded, it will find best child, and go one leven down
                if (currentNode.IsExpanded)
                {
                    // Find best child
                    var bestChild = getBestChild(currentNode);
                    currentNode = bestChild;
                }
                else
                {
                    // If node is not expanded, it expands it, and runs simulation from random child
                    expand(currentNode);

                    var reward = -1d;
                    if (currentNode.Children.Count() > 0)
                    {
                        var bestChild = getBestChild(currentNode);
                        visitedNodes.Add(bestChild);
                        bestChild.Visits++;

                        // Run random simulation from this point
                        reward = runRandomSimulation(bestChild, invokingPlayer);
                    }

                    // Backpropagate results
                    backpropageate(visitedNodes, reward);

                    // Reset path to start from root
                    currentNode = root;
                    visitedNodes.Clear();
                }
            }

            // Save tree
            //TreeJsonSaver.Save($"mcts_{turn}", root);

            Console.WriteLine($"MCTS iteratiosn: {iterations}");

            // Find best
            var best = getFinalBestChild(root);
            return best.Move;
        }

        private void expand(MctsNode node)
        {
            // If there are no more moves, stop it
            var availableMoves = getAvailableMoves(node.Board, node.Board.NextPlayer);
            if (availableMoves == null || availableMoves.Count() == 0)
                return;

            int i = 0;
            foreach (var move in availableMoves)
            {
                var newBoard = (CheckerBoard)node.Board.Clone();
                newBoard.MakeMove(move, node.Board.NextPlayer); 

                var childNode = new MctsNode(move, newBoard);
                childNode.Visits++;
                childNode.Name += node.Name + $"-{i++}";
                node.Children.Add(childNode);
            }

            node.IsExpanded = true;
        }

        private double balanceConstant = 0.7d;

        private MctsNode getBestChild(MctsNode node)
        {
            if (node == null)
                throw new ArgumentNullException("node");
            if (node.Children == null)
                throw new ArgumentNullException("node.Children");
            if (node.Children.Count() == 0)
                throw new ArgumentException("Node must have at least one child.", "node.Children");

            //Debug.WriteLine($"Get best child of {node.Name}");

          var orderedNodes = node.Children.OrderByDescending(c => ucb(c, node)).ThenBy(o => randomService.NextDouble());
           var bestNode = orderedNodes.First();
           return bestNode;

            //var randChild = listHelper.Random(node.Children);
           // return randChild;
        }

        private MctsNode getFinalBestChild(MctsNode node)
        {
            if (node == null)
                throw new ArgumentNullException("node");
            if (node.Children == null)
                throw new ArgumentNullException("node.Children");
            if (node.Children.Count() == 0)
                throw new ArgumentException("Node must have at least one child.", "node.Children");

            //Debug.WriteLine($"Get best child of {node.Name}");

            /*var orderedNodes = node.Children.OrderByDescending(c => ucb(c, node)).ThenBy(o => randomService.NextDouble());
            var bestNode = orderedNodes.First();
            return bestNode;*/

            var orderedNodes = node.Children.OrderByDescending(c => c.Reward/c.Visits).ThenBy(o => randomService.NextDouble());
            var bestNode = orderedNodes.First();
            return bestNode;
        }

        private double ucb(MctsNode child, MctsNode parent)
        {
            var result = child.Reward + balanceConstant * Math.Sqrt(2 * Math.Log(parent.Visits) / child.Visits);
            //var result = child.Wins / (double)child.Visits + balanceConstant * Math.Sqrt(2 * Math.Log(parent.Visits) / child.Visits);
            //Debug.WriteLine($"UCT for {child.Name}: CV={child.Visits}; CW={child.Wins}; PV={parent.Visits}; R={result}");
            return result;
        }

        private double runRandomSimulation(MctsNode node, Player invokingPlayer)
        {
            var board = (CheckerBoard)node.Board.Clone();
            int turns = 0;

            while (board.GetGameStatus() == GameStatuses.Running && turns < MAX_TURNS)
            {
                var availableMoves = getAvailableMoves(board, board.NextPlayer);
                if (availableMoves != null && availableMoves.Count() > 0)
                {
                    var randomMove = listHelper.Random(availableMoves);
                    board.MakeMove(randomMove, board.NextPlayer);
                }

                turns++;
            }

            if (board.GetGameStatus() != GameStatuses.Running && board.NextPlayer == invokingPlayer)
            {
                //Debug.WriteLine($"Finished in turns {turns}");
                var res = (MAX_TURNS - turns) / ((double) MAX_TURNS/2d);
                return res;
            }
            else
                return -1d;
        }

        private bool runSimulationWithDfs(MctsNode node, Player invokingPlayer)
        {
            var board = (CheckerBoard)node.Board.Clone();
            int turns = 0;

            while (board.GetGameStatus() == GameStatuses.Running && turns < MAX_TURNS)
            {
                var dfs = new DfsAI(3, listHelper);
                var move = dfs.GetMove(board, board.NextPlayer);
                board.MakeMove(move, board.NextPlayer);

                /*var availableMoves = getAvailableMoves(board, board.NextPlayer);
                if (availableMoves != null && availableMoves.Count() > 0)
                {
                    var randomMove = listHelper.Random(availableMoves);
                    board.MakeMove(randomMove, board.NextPlayer);
                }*/

                turns++;
            }

            if (board.GetGameStatus() != GameStatuses.Running && board.NextPlayer == invokingPlayer)
                return true;
            else
                return false;
        }

        public void backpropageate(IList<MctsNode> visitedNodes, double reward)
        {
            //Debug.WriteLine($"Backpropagating revard {reward}");

            foreach (var node in visitedNodes)
            {
                node.Reward += reward;

                /*if (isWin)
                    node.Wins++;
                else
                    node.Wins--;*/
            }
        }
        

        private IList<Move> getAvailableMoves(CheckerBoard currentBoard, Player player)
        {
            var jumpMoves = currentBoard.GetJumpMoves(player);
            if (jumpMoves != null && jumpMoves.Count() > 0)
                return jumpMoves;

            var simpleMoves = currentBoard.GetSimpleMoves(player);
            if (simpleMoves != null && simpleMoves.Count() > 0)
                return simpleMoves;

            return null;
        }
    }

    public class MctsNode : Node<MctsNode>
    {
        public int Visits { get; set; }

        //public int Wins { get; set; }

        public double Reward { get; set; }

        public bool IsExpanded { get; set; }
        
        public MctsNode(Move move, CheckerBoard board):base(move, board)
        {}

        public MctsNode(CheckerBoard board):base(board)
        {}

        public override string RenderName()
        {
            return $"{Name} ({Reward}/{Visits})";
        }
    }

    public class MctsSimulationResult
    {
        public bool IsWon { get; set; }

        public IList<Move> Moves { get; set; }
    }
}