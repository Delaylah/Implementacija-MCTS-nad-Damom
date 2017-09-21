using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CheckersBoard
{
    public class DfsAI : IAIAlgorithm
    {
        #region Dependencies

        private readonly IListHelper listHelper;

        #endregion

        public int MAX_DEPTH; // = 5;

        public DfsAI(int depth, IListHelper listHelper)
        {
            this.listHelper = listHelper;
            MAX_DEPTH = depth;
        }

        private IList<Move> GetAvaliableMoves(CheckerBoard currentBoard, Player player)
        {
            // If there exists jump move, return that because by rules nothing else can be played
            var jumpMoves = currentBoard.GetJumpMoves(player);
            if (jumpMoves.Count > 0)
                return jumpMoves;

            var simpleMoves = currentBoard.GetSimpleMoves(player);
            return simpleMoves;
        }



        public void AddToTree(DfsNode parent, CheckerBoard currentBoard, int currentDepth)
        {
            if (currentDepth >= MAX_DEPTH)
                return;

            if (currentBoard.GetGameStatus() != GameStatuses.Running)
                return;

            IList<Move> allPossibleMoves = GetAvaliableMoves(currentBoard, currentBoard.NextPlayer);

            foreach (var move in allPossibleMoves)
            {
                // Kloniras board i odigraš jedan potez iz liste
                var newBoard = (CheckerBoard)currentBoard.Clone();
                newBoard.MakeMove(move, currentBoard.NextPlayer);

                // Kreiraš node i dodaš ga kao child u parent
                var node = new DfsNode(move, newBoard);
                node.Name = $"{parent.Name}-{currentDepth + 1}";
                parent.AddChild(node);

                // Rekurzivno pozivaš AddToTree da doda i za slijedece poteze
                AddToTree(node, newBoard, currentDepth + 1);
            }
        }

        public Move GetMove(CheckerBoard currentBoard, Player player, int turn = 0)
        {
            var root = new DfsNode(currentBoard);
            root.Name = "0";
            AddToTree(root, currentBoard, 0);

            if (root.Children == null || root.Children.Count() == 0)
                return null;

            var bestScore = new BestInTreeScore { Score = Int32.MinValue, Moves = new List<IList<Move>>() };
            findBestMoveInTree(root, new List<Move>(), player, bestScore);

            // Save tree
            //TreeJsonSaver.Save("dfs", root);

            if (bestScore.Moves == null || bestScore.Moves.Count == 0)
                return null;
            else
            {
                //return bestScore.Moves.OrderBy(o => Guid.NewGuid()).First().First();
                var bestPath = listHelper.Random(bestScore.Moves);
                return bestPath.First();
            }
        }

        private double score(CheckerBoard board, Player player)
        {
            var counts = new Dictionary<FieldState, int>();
            var allowedStats = new[] { FieldState.Red, FieldState.RedKing, FieldState.Black, FieldState.BlackKing };
            foreach (var s in allowedStats)
                counts[s] = 0;

            for (var r = 0; r < 8; r++)
                for (var c = 0; c < 8; c++)
                {
                    var state = board.GetState(r, c);
                    if (allowedStats.Contains(state))
                    {
                        counts[state]++;
                    }
                }

            var possibleJumps = board.GetJumpMoves(player);
            //var possibleMoves = board.GetSimpleMoves(player);

            int kings = 0, pieces = 0;
            if (player == Player.Black)
            {
                kings = counts[FieldState.BlackKing];
                pieces = counts[FieldState.Black];
            }
            else if (player == Player.Red)
            {
                kings = counts[FieldState.RedKing];
                pieces = counts[FieldState.Red];
            }

            var totalPieces = kings + pieces;

            return (kings / 12d) * 0.5
                   + (totalPieces / 12d) * 0.2
                   + Math.Min(possibleJumps.Count() / 6d, 1d) * 0.3;
        }

        private double scoreForceAdventage(CheckerBoard board, Player player)
        {
            var counts = new Dictionary<FieldState, int>();
            var allowedStats = new[] { FieldState.Red, FieldState.RedKing, FieldState.Black, FieldState.BlackKing };
            var redPieces = new List<Piece>();
            var blackPieces = new List<Piece>();

            foreach (var s in allowedStats)
                counts[s] = 0;

            for (var r = 0; r < 8; r++)
                for (var c = 0; c < 8; c++)
                {
                    var state = board.GetState(r, c);
                    if (allowedStats.Contains(state))
                    {
                        counts[state]++;
                    }

                    if (state == FieldState.Red || state == FieldState.RedKing)
                        redPieces.Add(new Piece(r, c));
                    else if (state == FieldState.Black || state == FieldState.BlackKing)
                        blackPieces.Add(new Piece(r, c));
                }

            var possibleJumps = board.GetJumpMoves(player);
            //var possibleMoves = board.GetSimpleMoves(player);

            int playerKings = 0, playerPieces = 0, opponentKings = 0, opponentPieces = 0;
            if (player == Player.Black)
            {
                playerKings = counts[FieldState.BlackKing];
                playerPieces = counts[FieldState.Black];
                opponentKings = counts[FieldState.RedKing];
                opponentPieces = counts[FieldState.Red];
            }
            else if (player == Player.Red)
            {
                playerKings = counts[FieldState.RedKing];
                playerPieces = counts[FieldState.Red];
                opponentKings = counts[FieldState.BlackKing];
                opponentPieces = counts[FieldState.Black];
            }

            var playerTotal = playerKings + playerPieces;
            var opponentTotal = opponentKings + opponentPieces;

            var avgDistance = getAverageDistance(redPieces, blackPieces);

            return ((playerTotal - opponentTotal) / Math.Max(playerTotal, opponentTotal)) * 0.4
                   + (playerKings / 12d) * 0.5
                   + (avgDistance/8d) * 0.1;

            //return (kings / 12d) * 0.5
            //       + (totalPieces / 12d) * 0.2
            //       + Math.Min(possibleJumps.Count() / 6d, 1d) * 0.3;
        }

        private double getAverageDistance(IList<Piece> redPieces, IList<Piece> blackPieces)
        {
            double sum = 0;
            int i = 0;

            foreach (var r in redPieces)
            {
                foreach (var b in blackPieces)
                {
                    sum += Math.Abs(b.Row - r.Row) + Math.Abs(b.Column - r.Column);
                    i += 2;
                }
            }

            if (i == 0)
                return 0;

            var average = sum / i;
            return average;
        }

        private void findBestMoveInTree(DfsNode node, IList<Move> previousMoves, Player player, BestInTreeScore bestScore)
        {
            // Dosao je do lista, evaluate
            if (node.Children == null || node.Children.Count() == 0)
            {
                var s = scoreForceAdventage(node.Board, player);

                if (s > bestScore.Score)
                {
                    bestScore.Score = s;
                    bestScore.Moves.Clear();
                    bestScore.Moves.Add(previousMoves);
                }
                else if (s == bestScore.Score)
                {
                    bestScore.Moves.Add(previousMoves);
                }
            }
            else
            {
                foreach (var child in node.Children)
                {
                    var pm = previousMoves.ToList();
                    pm.Add(child.Move);
                    findBestMoveInTree(child, pm, player, bestScore);
                }
            }
        }

        class BestInTreeScore
        {
            public IList<IList<Move>> Moves { get; set; }

            public double Score { get; set; }
        }
    }

    // 1. Kreiraj root
    // 2. Nađi listu svih mogućih poteza
    // 3. Dodaoj moguće poteze kao childs root (za svaki potez kreiraš novi board, tj kopiraš njegovo stanje u novi objekat i igraš potez na tom bordu)

    // 4. Za svaki child nađi listu mogućih poteza i dodaj kao chils tog noda
    // 5. Pomovi 3 i 4 rekurivno sve dok ne dodjes u situaciju da nema više mogućih poteza

    public class DfsNode : Node<DfsNode>
    {
        public DfsNode(Move move, CheckerBoard board) : base(move, board)
        { }

        public DfsNode(CheckerBoard board) : base(board)
        { }
    }
}
