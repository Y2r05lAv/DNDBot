using System;

namespace DNDBot.Goverment.Research
{
    public sealed class ResearchProgressRecord
    {
        public int CountryId { get; set; }
        public int ResearchId { get; set; }
        public float Progress { get; set; }
        public DateTime StartedAt { get; set; }
        public bool IsCompleted { get; set; }
    }
}
