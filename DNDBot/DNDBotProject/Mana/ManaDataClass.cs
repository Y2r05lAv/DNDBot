using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DNDBot
{
    public class ManaDataClass
    {
        public long UserId { get; set; }
        public int CharacterId { get; set; }
        public string CharacterName { get; set; }
        

        public int MagicArmour { get; set; }
        public int MagicLevel { get; set; }
        public int MagicXP { get; set; }

        public int MaxMana { get; set; }
        public int BaseMana { get; set; }
        public int CurrentMana { get; set; }

        public int Stability { get; set; }
        public int Exhaustion { get; set; }
        public double ManaZoneRadius { get; set; }
        public int ManaZoneControl { get; set; }

        public int RegenTurn { get; set; }
        public int RegenAfterFight { get; set; }

        public string Resonance { get; set; }   
    }

}
