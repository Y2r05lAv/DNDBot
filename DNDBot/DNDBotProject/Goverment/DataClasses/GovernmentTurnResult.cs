using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DNDBot.Goverment.Data
{
    public class GovernmentTurnResult
    {
        public bool RebellionStarted { get; set; }
        public bool CoupAttempt { get; set; }
        public bool LawPassed { get; set; }

        public string Summary { get; set; }
    }
}
