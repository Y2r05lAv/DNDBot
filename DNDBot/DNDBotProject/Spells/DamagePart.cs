using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DNDBot.Spells
{
     
    public class DamagePart
    {
        public int Count { get; set; }       
        public int Die { get; set; }         
        public string Type { get; set; }     
        public string Modifier { get; set; }  
    }

}
