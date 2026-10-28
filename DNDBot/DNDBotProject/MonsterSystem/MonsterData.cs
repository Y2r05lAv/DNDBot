namespace DNDBot
{
    public class MonsterCreator
    {
        public long ID { get; set; }
        public string Name { get; set; }
        public int HP { get; set; }
        public int MaxHP { get; set; }
        public int Armour { get; set; }
        public int Initiative { get; set; }
        public int XP { get; set; }
        public string AttackName { get; set; }
        public string AttackDamageDice { get; set; }

        public int GetEffectiveMaxHP()
            => MaxHP > 0 ? MaxHP : HP;

        public bool HasAttack()
            => !string.IsNullOrWhiteSpace(AttackDamageDice);
    }

    public sealed class MonsterTemplateRecord
    {
        public int TemplateId { get; set; }
        public string Name { get; set; } = "";
        public int HP { get; set; }
        public int Armour { get; set; }
        public int XP { get; set; }
        public string AttackName { get; set; } = "";
        public string AttackDamageDice { get; set; } = "";
        public long CreatedBy { get; set; }
        public System.DateTime CreatedAtUtc { get; set; }
    }
}
