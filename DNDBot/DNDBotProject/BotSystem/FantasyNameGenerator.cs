using System;

namespace DNDBot.BotSystem
{
    public enum NameStyle
    {
        Imperial,
        Northern,
        Arcane,
        Southern
    }

    public enum Gender
    {
        Male,
        Female
    }

    public static class FantasyNameGenerator
    {
        private static readonly object _lock = new object();
        private static readonly Random _rng = new Random();

        public static string GenerateFullName(NameStyle style, Gender gender)
        {
            string firstName = GenerateFirstName(style, gender);
            string surname = GenerateSurname(style);
            return $"{firstName} {surname}";
        }

        public static string GenerateFirstName(NameStyle style, Gender gender)
        {
            StyleData data = GetStyleData(style);
            int syllables = NextInt(data.MinFirstSyllables, data.MaxFirstSyllables);
            string end = gender == Gender.Male ? Pick(data.EndMale) : Pick(data.EndFemale);
            return BuildName(Pick(data.Start), data.Middle, end, syllables, data.BridgeConsonants);
        }

        public static string GenerateSurname(NameStyle style)
        {
            StyleData data = GetStyleData(style);
            int syllables = NextInt(data.MinSurnameSyllables, data.MaxSurnameSyllables);
            return BuildName(Pick(data.SurnameStart), data.SurnameMiddle, Pick(data.SurnameEnd), syllables, data.BridgeConsonants);
        }

        private static string BuildName(string start, string[] middlePool, string end, int syllables, char[] bridgeConsonants)
        {
            if (syllables <= 1)
            {
                return Capitalize(start);
            }

            string name = start;

            int middleCount = Math.Max(0, syllables - 2);
            for (int i = 0; i < middleCount; i++)
            {
                string mid = Pick(middlePool);
                name = Combine(name, mid, bridgeConsonants);
            }

            name = Combine(name, end, bridgeConsonants);
            return Capitalize(name);
        }

        private static string Combine(string left, string right, char[] bridgeConsonants)
        {
            if (string.IsNullOrWhiteSpace(left))
                return right ?? string.Empty;
            if (string.IsNullOrWhiteSpace(right))
                return left;

            char last = left[left.Length - 1];
            char first = right[0];

            if (IsVowel(last) && IsVowel(first))
            {
                char bridge = bridgeConsonants[NextInt(0, bridgeConsonants.Length - 1)];
                return left + bridge + right;
            }

            if (!IsVowel(last) && !IsVowel(first) && last == first)
            {
                return left + right.Substring(1);
            }

            return left + right;
        }

        private static bool IsVowel(char c)
        {
            char ch = char.ToLowerInvariant(c);
            return ch == 'a' || ch == 'e' || ch == 'i' || ch == 'o' || ch == 'u' || ch == 'y'
                || ch == 'а' || ch == 'е' || ch == 'и' || ch == 'і' || ch == 'ї' || ch == 'о'
                || ch == 'у' || ch == 'ю' || ch == 'я' || ch == 'є';
        }

        private static string Capitalize(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;
            if (value.Length == 1)
                return value.ToUpperInvariant();
            return char.ToUpperInvariant(value[0]) + value.Substring(1).ToLowerInvariant();
        }

        private static int NextInt(int min, int max)
        {
            if (min >= max)
                return min;
            lock (_lock)
            {
                return _rng.Next(min, max + 1);
            }
        }

        private static string Pick(string[] values)
        {
            if (values == null || values.Length == 0)
                return string.Empty;
            return values[NextInt(0, values.Length - 1)];
        }

        private static StyleData GetStyleData(NameStyle style)
        {
            switch (style)
            {
                case NameStyle.Imperial:
                    return Imperial;
                case NameStyle.Northern:
                    return Northern;
                case NameStyle.Arcane:
                    return Arcane;
                case NameStyle.Southern:
                    return Southern;
                default:
                    return Imperial;
            }
        }

        private sealed class StyleData
        {
            public string[] Start;
            public string[] Middle;
            public string[] EndMale;
            public string[] EndFemale;
            public string[] SurnameStart;
            public string[] SurnameMiddle;
            public string[] SurnameEnd;
            public int MinFirstSyllables;
            public int MaxFirstSyllables;
            public int MinSurnameSyllables;
            public int MaxSurnameSyllables;
            public char[] BridgeConsonants;
        }

        private static readonly StyleData Imperial = new StyleData
        {
            Start = new[] { "ваел", "сер", "кал", "аурел", "корв", "вал", "сар", "тав", "ліан", "марк", "евр", "орим" },
            Middle = new[] { "е", "ія", "ор", "ан", "ел", "рі", "ат", "ем", "ів", "ер", "ол", "ар" },
            EndMale = new[] { "іон", "іус", "ар", "ен", "ор", "ант", "ер", "еон", "аст", "ім" },
            EndFemale = new[] { "ія", "ена", "іра", "ель", "ора", "еса", "іна", "ара", "іса", "елея" },
            SurnameStart = new[] { "вал", "сер", "аурел", "корв", "марк", "тав", "ліан", "орим", "дар", "сіл" },
            SurnameMiddle = new[] { "ен", "ор", "іл", "ав", "ер", "ан", "ем", "ар" },
            SurnameEnd = new[] { "монт", "гард", "вейн", "крес", "делл", "віор", "тан", "мор", "лорн", "рік" },
            MinFirstSyllables = 2,
            MaxFirstSyllables = 3,
            MinSurnameSyllables = 2,
            MaxSurnameSyllables = 3,
            BridgeConsonants = new[] { 'л', 'н', 'р' }
        };

        private static readonly StyleData Northern = new StyleData
        {
            Start = new[] { "ск", "бр", "кр", "тр", "др", "гр", "ст", "хар", "тор", "фенн", "вар", "ул" },
            Middle = new[] { "а", "е", "і", "о", "ур", "арг", "орн", "ак", "ір", "ок" },
            EndMale = new[] { "к", "р", "н", "г", "ск", "д", "вар", "грім", "орн", "хальд" },
            EndFemale = new[] { "а", "іс", "і", "ін", "ет", "ра", "діс", "хільд", "іра", "ен" },
            SurnameStart = new[] { "фрост", "шторм", "стоун", "айрон", "вульф", "скар", "грім", "драк", "скельд", "торн" },
            SurnameMiddle = new[] { "ен", "ар", "ор", "ур", "ір" },
            SurnameEnd = new[] { "сон", "сен", "гард", "марк", "вальд", "хейм", "крофт", "берг", "рун" },
            MinFirstSyllables = 2,
            MaxFirstSyllables = 3,
            MinSurnameSyllables = 2,
            MaxSurnameSyllables = 3,
            BridgeConsonants = new[] { 'р', 'к', 'г' }
        };

        private static readonly StyleData Arcane = new StyleData
        {
            Start = new[] { "ксал", "зае", "ел", "нікс", "ает", "зір", "кір", "сіл", "ва", "іт", "ум", "ет" },
            Middle = new[] { "ае", "іо", "іра", "ул", "еш", "ім", "ор", "іт", "ен", "лір", "аел" },
            EndMale = new[] { "ор", "ет", "іус", "ар", "іон", "ир", "аз", "ем", "от", "ен" },
            EndFemale = new[] { "іра", "аел", "ін", "еса", "іт", "іра", "ена", "ора", "іс", "ієль" },
            SurnameStart = new[] { "руна", "сіділ", "ает", "міст", "вейл", "лор", "мана", "етер", "гліф", "зен" },
            SurnameMiddle = new[] { "ен", "ір", "аел", "ор", "іт", "ул", "еш" },
            SurnameEnd = new[] { "вейв", "спір", "вард", "марк", "крес", "байнд", "сім", "вейл", "річ" },
            MinFirstSyllables = 2,
            MaxFirstSyllables = 4,
            MinSurnameSyllables = 2,
            MaxSurnameSyllables = 3,
            BridgeConsonants = new[] { 'л', 'р', 'с' }
        };

        private static readonly StyleData Southern = new StyleData
        {
            Start = new[] { "лія", "сера", "мае", "сол", "ара", "ена", "лорі", "кала", "міра", "нола", "саві" },
            Middle = new[] { "ла", "рі", "на", "ме", "са", "лі", "ро", "ва", "ле", "мі" },
            EndMale = new[] { "о", "ан", "ел", "ріо", "дар", "ен", "аро", "іан", "іл", "ор" },
            EndFemale = new[] { "а", "ія", "ела", "іна", "ара", "ора", "елле", "іса", "іне", "іта" },
            SurnameStart = new[] { "соле", "роза", "вела", "мір", "лумі", "канта", "аура", "серен", "белла", "маріс" },
            SurnameMiddle = new[] { "лі", "на", "ра", "ве", "ла", "мі", "са" },
            SurnameEnd = new[] { "дора", "луна", "мар", "валь", "канто", "сера", "віста", "сіна", "белла", "ная" },
            MinFirstSyllables = 2,
            MaxFirstSyllables = 3,
            MinSurnameSyllables = 2,
            MaxSurnameSyllables = 3,
            BridgeConsonants = new[] { 'л', 'н', 'р' }
        };
    }
}

