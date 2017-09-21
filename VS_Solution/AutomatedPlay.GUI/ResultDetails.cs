using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutomatedPlay.GUI
{
    public class ResultDetails
    {
        public MatchResult Result { get; set; }

        public TimeSpan Duration { get; set; }

        public Type WinningAlgorithmType { get; set; }

        public int TotalTurns { get; set; }

        public ResultDetails(MatchResult result, TimeSpan duration, int totalTurns)
        {
            this.Result = result;
            this.Duration = duration;
            this.TotalTurns = totalTurns;
        }

        public ResultDetails(MatchResult result, TimeSpan duration, Type winningAlgorithmType, int totalTurns)
        {
            this.Result = result;
            this.Duration = duration;
            this.WinningAlgorithmType = winningAlgorithmType;
            this.TotalTurns = totalTurns;
        }
    }
}
