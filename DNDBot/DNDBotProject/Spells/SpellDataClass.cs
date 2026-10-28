namespace DNDBot.Spells
{
    public class SpellDataClass
    {

        public long UserId { get; set; }
        public int CharacterId { get; set; }
        public int SpellId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string TypeHarm { get; set; }
        public string SpellType { get; set; }
        public string Distance { get; set; }
        public string Target { get; set; }
        public string DamageDice { get; set; }
        public string Modifier { get; set; }
        public int IsActive { get; set; }
        public int ManaCost { get; set; }



    }
}
