using System;
using System.Collections.Generic;

namespace DNDBot.Goverment.Research
{
    public enum ResearchEffectType
    {
        UnlockLaw,
        UnlockBuilding,
        Bonus,
        UnlockFeature,
        EnableParliament,
        IncreaseParliamentSeats,
        ReduceCorruption,
        IncreaseResearchSpeed,
        Army,
        Economy,
        Magic
    }

    public sealed class ResearchEffect
    {
        public ResearchEffectType Type { get; set; }
        public string Value { get; set; } = string.Empty;
        public float Amount { get; set; }
    }

    public sealed class ResearchDefinition
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Branch { get; set; } = "Загальне";
        public string Description { get; set; } = string.Empty;
        public int ScienceCost { get; set; }
        public long MoneyCost { get; set; }
        public int DurationDays { get; set; }
        public int SortOrder { get; set; }
        public bool IsActive { get; set; } = true;
        public List<int> Requirements { get; set; } = new();
        public List<ResearchEffect> Effects { get; set; } = new();
    }
}
