using CheckersBoard;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PerformanceTest
{
    class Program
    {
        static void Main(string[] args)
        {
            /*var sw = new Stopwatch();
            sw.Start();
            for (int i = 0; i < 1000000; i++)
            {
                var board = new CheckerBoard();
                board.Clone();
            }
            sw.Stop();
            Console.WriteLine($"Elapsed: {sw.Elapsed}");*/

            //checkMctsNoMoves();
            dfsTurnDuration();
            Console.ReadLine();
        }

        private static void dfsTurnDuration()
        {
            var board = new CheckerBoard();
            board.InitializeBoard();
            var dfsai = new DfsAI(1, new ListHelpers(new RandomService()));

            var sw = new Stopwatch();
            sw.Start();
            dfsai.GetMove(board, Player.Black);
            sw.Stop();
            Console.WriteLine($"Elapsed: {sw.Elapsed.TotalMilliseconds} ms");
        }

        private static void checkMctsNoMoves()
        {
            var board = new CheckerBoard();
            board.InitializeBoard();
            board.Parse(@"EEEEECEE
REEEEEEE
EBEEEEEE
EEBEEEEE
EEEEEEEE
EEEEEEEE
EBEEEEEE
EEEEEEEE
", Player.Black);

            var simpleMoves = board.GetSimpleMoves(Player.Black);
            var jumpMoves = board.GetJumpMoves(Player.Black);

        }
    }
}
