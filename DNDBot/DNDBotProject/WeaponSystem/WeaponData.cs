using System.Collections.Generic;

namespace DNDBot
{
    public class WeaponInfo
    {
        Dictionary<int, Dictionary<string, int>> Weapons = new Dictionary<int, Dictionary<string, int>>
        {
            { 1, new Dictionary<string, int>
                {
                    {"Духова трубка", 0},
                }
            },
            { 4, new Dictionary<string, int>
                {
                    {"Кийок", 1},
                    {"Кинджал", 1},
                    {"Легкий молот", 1},
                    {"Серп", 1},
                    {"Дротик", 1},
                    {"Праща", 1},
                    {"Батіг", 1},
                }
            },
            { 6, new Dictionary<string, int>
                {
                    {"Посох", 1},
                    {"Булава", 1},
                    {"Метальний спис", 1},
                    {"Ручна сокира", 1},
                    {"Спис", 1},
                    {"Короткий лук", 1},
                    {"Дворучний меч", 2},
                    {"Короткий меч", 1},
                    {"Кувалда", 2},
                    {"Тризуб", 1},
                    {"Шабля", 1},
                    {"Ручний арбалет", 1},
                }
            },

            { 8, new Dictionary<string, int>
                {
                    {"Дубина", 1},
                    {"Легкий арбалет", 1},
                    {"Бойова кирка", 1},
                    {"Бойова сокира", 1},
                    {"Бойовий молот", 1},
                    {"Бойовий ціп", 1},
                    {"Довгий меч", 2},
                    {"Моргенштерн", 1},
                    {"Рапіра", 2},
                    {"Довгий лук", 1},
                }
            },

            { 10, new Dictionary<string, int>
                {
                    {"Алебарда", 1},
                    {"Глефа", 1},
                    {"Піка", 1},
                    {"Важкий арбалет", 1},
                }
            },

            { 12, new Dictionary<string, int>
                {
                    {"Дворучна сокира", 1},
                    {"Довгий спис", 1},
                }
            },

        };
        public Dictionary<int, Dictionary<string, int>> WeaponInfoDice
        {
            get { return Weapons; }
        }

    }
}
