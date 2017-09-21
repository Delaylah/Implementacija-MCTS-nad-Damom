using GalaSoft.MvvmLight;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutomatedPlay.GUI.Model
{
    public class MatchResultModel : ObservableObject
    {
        private int _matchNum;
        public int MatchNum
        {
            get { return _matchNum; }
            set { _matchNum = value; RaisePropertyChanged(() => this.MatchNum); }
        }

        private string _winner;
        public string Winner
        {
            get { return _winner; }
            set { _winner = value; RaisePropertyChanged(() => this.Winner); }
        }        

        private int _totalTurns;
        public int TotalTurns
        {
            get { return _totalTurns; }
            set { _totalTurns = value; RaisePropertyChanged(() => this.TotalTurns); }
        }
        

        private TimeSpan _duration;
        public TimeSpan Duration
        {
            get { return _duration; }
            set { _duration = value; RaisePropertyChanged(() => this.Duration); }
        }


    }
}
