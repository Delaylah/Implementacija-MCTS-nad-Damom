using GalaSoft.MvvmLight;
using AutomatedPlay.GUI.Model;
using CheckersBoard;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using GalaSoft.MvvmLight.Command;
using System.Collections.ObjectModel;

namespace AutomatedPlay.GUI.ViewModel
{
    /// <summary>
    /// This class contains properties that the main View can data bind to.
    /// <para>
    /// See http://www.mvvmlight.net
    /// </para>
    /// </summary>
    public class MainViewModel : ViewModelBase
    {
        private IList<IAIAlgorithm> algorithms;

        private int _matchesCount = 100;
        public int MatchesCount
        {
            get { return _matchesCount; }
            set { _matchesCount = value; RaisePropertyChanged(() => this.MatchesCount); }
        }
                
        private int _maxTurnsPerMatch = 200;
        public int MaxTurnsPerMatch
        {
            get { return _maxTurnsPerMatch; }
            set { _maxTurnsPerMatch = value; RaisePropertyChanged(() => this._maxTurnsPerMatch); }
        }

        private int _currentMatch;
        public int CurrentMatch
        {
            get { return _currentMatch; }
            set
            {
                _currentMatch = value;
                RaisePropertyChanged(() => this.CurrentMatch);
            }
        }

        private int _currentTurn;
        public int CurrentTurn
        {
            get { return _currentTurn; }
            set { _currentTurn = value; RaisePropertyChanged(() => this.CurrentTurn); }
        }

        private int _mctsWins;
        public int MctsWins
        {
            get { return _mctsWins; }
            set { _mctsWins = value; RaisePropertyChanged(() => this.MctsWins); }
        }

        private int _dfsWins;
        public int DfsWins
        {
            get { return _dfsWins; }
            set { _dfsWins = value; RaisePropertyChanged(() => this.DfsWins); }
        }

        private int _maxTurnsWins;
        public int MaxTurnsWins
        {
            get { return _maxTurnsWins; }
            set { _maxTurnsWins = value; RaisePropertyChanged(() => this.MaxTurnsWins); }
        }

        private ObservableCollection<MatchResultModel> _matches = new ObservableCollection<MatchResultModel>();
        public ObservableCollection<MatchResultModel> Matches
        {
            get { return _matches; }
            set { _matches = value; RaisePropertyChanged(() => this.Matches); }
        }

        public RelayCommand StartCommand
        {
            get { return new RelayCommand(async () => await start()); }
        }

        public MainViewModel()
        {
            var randomService = new RandomService();
            var listHelper = new ListHelpers(randomService);

            this.algorithms = new List<IAIAlgorithm>
            {
                //new AgresiveRandomAI(),
                new DfsAI(5, listHelper),
                new MctsAI(listHelper, randomService)
            };
        }

        private async Task start()
        {
            this.CurrentMatch = 0;

            for (var i = 0; i < MatchesCount; i++)
            {
                this.CurrentMatch++;
                this.CurrentTurn = 0;

                var shuffeledAlgorithms = algorithms.OrderBy(o => Guid.NewGuid()).ToArray();
                var algorithmsByPlayers = new Dictionary<Player, IAIAlgorithm>
                {
                    { Player.Black, shuffeledAlgorithms[0] },
                    { Player.Red, shuffeledAlgorithms[1] }
                };

                Stopwatch sw = new Stopwatch();
                sw.Start();
                var resultDetails = await Task.Run(() => PlayMatch(algorithmsByPlayers, i + 1, updateTurnsHelper));
                sw.Stop();
                Console.WriteLine($"Match #{i + 1}, {resultDetails.Result}, {resultDetails.Duration}; {resultDetails.WinningAlgorithmType?.Name}");

                string winner = null;
                if (resultDetails.Result == MatchResult.MaxTurnsReached)
                {
                    this.MaxTurnsWins++;
                    winner = "MaxTurnsReached";
                }
                else if (resultDetails.Result == MatchResult.RedWon || resultDetails.Result == MatchResult.BlackWon)
                {
                    winner = resultDetails.WinningAlgorithmType.Name;
                    if (resultDetails.WinningAlgorithmType == typeof(MctsAI))
                        this.MctsWins++;
                    else if (resultDetails.WinningAlgorithmType == typeof(DfsAI))
                        this.DfsWins++;
                    else
                        throw new Exception($"Unknown alogorithm: {resultDetails.WinningAlgorithmType.FullName}");
                }
                else
                    throw new Exception($"Unknown result type: {resultDetails.Result}");

                this.Matches.Insert(0, new MatchResultModel
                {
                    MatchNum = CurrentMatch,
                    Winner = winner,
                    Duration = sw.Elapsed,
                    TotalTurns = resultDetails.TotalTurns
                });
            }
        }

        private void updateTurnsHelper(int turn)
        {
            this.CurrentTurn = turn;
        }

        private ResultDetails PlayMatch(Dictionary<Player, IAIAlgorithm> algorithms, int matchNumber, Action<int> updateTurn)
        {
            var board = new CheckerBoard();
            board.InitializeBoard();

            Stopwatch matchStopwatch = new Stopwatch();
            Stopwatch stopwatch = new Stopwatch();
            int turns = 0;

            matchStopwatch.Start();
            while (board.GetGameStatus() == GameStatuses.Running)
            {
                updateTurn(turns);

                stopwatch.Reset();
                stopwatch.Start();
                var move = algorithms[board.NextPlayer].GetMove(board, board.NextPlayer);
                stopwatch.Stop();

                if (move == null)
                    return new ResultDetails(MatchResult.NoMoreMoves, matchStopwatch.Elapsed, turns);

                Console.WriteLine($"\tTurn #{matchNumber}.{turns}; Player {board.NextPlayer}; ({move.piece1.Row}, {move.piece1.Column}) to ({move.piece2.Row}, {move.piece2.Column}); Elapsed: {stopwatch.ElapsedMilliseconds} ms");

                board.MakeMove(move, board.NextPlayer);

                if (board.GetGameStatus() != GameStatuses.Running)
                {
                    var result = board.GetGameStatus();

                    if (result == GameStatuses.RedWon)
                        return new ResultDetails(MatchResult.RedWon, matchStopwatch.Elapsed, algorithms[board.NextPlayer].GetType(), turns);
                    else if (result == GameStatuses.BlackWon)
                        return new ResultDetails(MatchResult.BlackWon, matchStopwatch.Elapsed, algorithms[board.NextPlayer].GetType(), turns);
                    else
                        throw new Exception($"Unsupported result: {result}");
                }

                if (++turns >= this.MaxTurnsPerMatch)
                    return new ResultDetails(MatchResult.MaxTurnsReached, matchStopwatch.Elapsed, turns);
            }

            return new ResultDetails(MatchResult.NoResult, matchStopwatch.Elapsed, turns);
        }
    }
}