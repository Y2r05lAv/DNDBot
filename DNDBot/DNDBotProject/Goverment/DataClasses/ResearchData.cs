using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DNDBot.Goverment.Data
{
    public class ResearchData
    {
        public int ResearchId { get; set; }
        public string Name { get; set; }
        public string Branch { get; set; }

        public int Cost { get; set; }
        public float Progress { get; set; }  
        public bool IsCompleted { get; set; }

        public string Unlocks { get; set; }  
    }
}
