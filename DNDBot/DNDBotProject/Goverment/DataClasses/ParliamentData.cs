using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DNDBot.Goverment.Data
{
    public class ParliamentData
    {
        public int TotalSeats { get; set; }
        public int RequiredMajority { get; set; }       
        public bool IsSessionActive { get; set; }

        public int CurrentLawId { get; set; }
        public float CurrentSupport { get; set; }

        public int SpeechesUsed { get; set; }
        public int MaxSpeeches { get; set; }

        public bool VotingInProgress { get; set; }
    }
}
