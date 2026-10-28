#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;

namespace DNDBot.Goverment.Research
{
    public static class ResearchDefinitions
    {
        private const decimal ScienceCostMultiplier = 1.12m;
        private const decimal MoneyCostMultiplier = 1.12m;
        private const decimal DurationDaysMultiplier = 1.18m;
        private const int MinResearchDurationDays = 7;
        private const int MaxResearchDurationDays = 150;

        private static readonly List<ResearchDefinition> FallbackDefinitions = new()
        {
            new ResearchDefinition
            {
                Id = 1,
                Name = "Писемність",
                Branch = "Адміністрація",
                Description = "Базові навички письма та обліку. Відкриває наступні адміністративні практики.",
                ScienceCost = 50,
                MoneyCost = 2000,
                DurationDays = 3,
                SortOrder = 10,
                Effects = new List<ResearchEffect>
                {
                    new ResearchEffect { Type = ResearchEffectType.Bonus, Value = "AdminEfficiency", Amount = 2 }
                }
            },
            new ResearchDefinition
            {
                Id = 2,
                Name = "Канцелярські статути",
                Branch = "Адміністрація",
                Description = "Упорядкування наказів, посад і обліку. Відкриває станові збори.",
                ScienceCost = 120,
                MoneyCost = 8000,
                DurationDays = 5,
                SortOrder = 20,
                Requirements = new List<int> { 1 },
                Effects = new List<ResearchEffect>
                {
                    new ResearchEffect { Type = ResearchEffectType.EnableParliament, Amount = 1 }
                }
            },
            new ResearchDefinition
            {
                Id = 3,
                Name = "Архіви",
                Branch = "Наука",
                Description = "Систематизація знань та їх збереження.",
                ScienceCost = 90,
                MoneyCost = 5000,
                DurationDays = 4,
                SortOrder = 10,
                Requirements = new List<int> { 1 },
                Effects = new List<ResearchEffect>
                {
                    new ResearchEffect { Type = ResearchEffectType.IncreaseResearchSpeed, Amount = 10 }
                }
            },
            new ResearchDefinition
            {
                Id = 4,
                Name = "Становий сейм",
                Branch = "Адміністрація",
                Description = "Скликання станів та узгодження податків і законів.",
                ScienceCost = 180,
                MoneyCost = 12000,
                DurationDays = 6,
                SortOrder = 30,
                Requirements = new List<int> { 2 },
                Effects = new List<ResearchEffect>
                {
                    new ResearchEffect { Type = ResearchEffectType.EnableParliament, Amount = 1 },
                    new ResearchEffect { Type = ResearchEffectType.IncreaseParliamentSeats, Amount = 10 }
                }
            },
            new ResearchDefinition
            {
                Id = 5,
                Name = "Контроль скарбниці",
                Branch = "Право",
                Description = "Перевірка зборів і витрат через ревізорів.",
                ScienceCost = 140,
                MoneyCost = 10000,
                DurationDays = 5,
                SortOrder = 10,
                Requirements = new List<int> { 2 },
                Effects = new List<ResearchEffect>
                {
                    new ResearchEffect { Type = ResearchEffectType.ReduceCorruption, Amount = 8 }
                }
            },
            new ResearchDefinition
            {
                Id = 6,
                Name = "Розширення станів",
                Branch = "Адміністрація",
                Description = "Залучення міст і гільдій до ради.",
                ScienceCost = 200,
                MoneyCost = 15000,
                DurationDays = 7,
                SortOrder = 40,
                Requirements = new List<int> { 4 },
                Effects = new List<ResearchEffect>
                {
                    new ResearchEffect { Type = ResearchEffectType.IncreaseParliamentSeats, Amount = 20 }
                }
            },
            new ResearchDefinition
            {
                Id = 7,
                Name = "Канцелярія",
                Branch = "Адміністрація",
                Description = "Організація діловодства, печаток та обліку наказів.",
                ScienceCost = 70,
                MoneyCost = 3000,
                DurationDays = 3,
                SortOrder = 12,
                Requirements = new List<int> { 1 },
                Effects = new List<ResearchEffect>
                {
                    new ResearchEffect { Type = ResearchEffectType.ReduceCorruption, Amount = 2 }
                }
            },
            new ResearchDefinition
            {
                Id = 8,
                Name = "Перепис населення",
                Branch = "Адміністрація",
                Description = "Оцінка підданих, земель та податкової бази.",
                ScienceCost = 90,
                MoneyCost = 6000,
                DurationDays = 4,
                SortOrder = 15,
                Requirements = new List<int> { 1 },
                Effects = new List<ResearchEffect>
                {
                    new ResearchEffect { Type = ResearchEffectType.ReduceCorruption, Amount = 3 }
                }
            },
            new ResearchDefinition
            {
                Id = 9,
                Name = "Податкові реєстри",
                Branch = "Адміністрація",
                Description = "Системний облік податків і повинностей.",
                ScienceCost = 110,
                MoneyCost = 8000,
                DurationDays = 5,
                SortOrder = 18,
                Requirements = new List<int> { 1 },
                Effects = new List<ResearchEffect>
                {
                    new ResearchEffect { Type = ResearchEffectType.ReduceCorruption, Amount = 4 }
                }
            },
            new ResearchDefinition
            {
                Id = 10,
                Name = "Стандарти мір і ваг",
                Branch = "Адміністрація",
                Description = "Єдині міри для торгівлі та ремесел.",
                ScienceCost = 130,
                MoneyCost = 7000,
                DurationDays = 4,
                SortOrder = 22,
                Requirements = new List<int> { 1 },
                Effects = new List<ResearchEffect>
                {
                    new ResearchEffect { Type = ResearchEffectType.ReduceCorruption, Amount = 2 }
                }
            },
            new ResearchDefinition
            {
                Id = 11,
                Name = "Провінційне управління",
                Branch = "Адміністрація",
                Description = "Намісники та локальні канцелярії для швидких рішень.",
                ScienceCost = 150,
                MoneyCost = 10000,
                DurationDays = 6,
                SortOrder = 25,
                Requirements = new List<int> { 2 },
                Effects = new List<ResearchEffect>
                {
                    new ResearchEffect { Type = ResearchEffectType.ReduceCorruption, Amount = 4 }
                }
            },
            new ResearchDefinition
            {
                Id = 12,
                Name = "Писарі та урядники",
                Branch = "Адміністрація",
                Description = "Навчені служби для справ, судів і обліку.",
                ScienceCost = 180,
                MoneyCost = 14000,
                DurationDays = 6,
                SortOrder = 28,
                Requirements = new List<int> { 2 },
                Effects = new List<ResearchEffect>
                {
                    new ResearchEffect { Type = ResearchEffectType.ReduceCorruption, Amount = 5 }
                }
            },
            new ResearchDefinition
            {
                Id = 13,
                Name = "Централізація влади",
                Branch = "Адміністрація",
                Description = "Зміцнення столиці та підпорядкування місцевих влад.",
                ScienceCost = 220,
                MoneyCost = 20000,
                DurationDays = 7,
                SortOrder = 35,
                Requirements = new List<int> { 2 },
                Effects = new List<ResearchEffect>
                {
                    new ResearchEffect { Type = ResearchEffectType.ReduceCorruption, Amount = 6 }
                }
            },
            new ResearchDefinition
            {
                Id = 14,
                Name = "Коронні палати",
                Branch = "Адміністрація",
                Description = "Поділ управління на палати: скарбниця, військо, двір.",
                ScienceCost = 260,
                MoneyCost = 25000,
                DurationDays = 8,
                SortOrder = 45,
                Requirements = new List<int> { 4 },
                Effects = new List<ResearchEffect>
                {
                    new ResearchEffect { Type = ResearchEffectType.ReduceCorruption, Amount = 3 }
                }
            },
            new ResearchDefinition
            {
                Id = 15,
                Name = "Гінці та кур'єри",
                Branch = "Адміністрація",
                Description = "Регулярна служба гінців і шляхових постів.",
                ScienceCost = 280,
                MoneyCost = 22000,
                DurationDays = 8,
                SortOrder = 50,
                Requirements = new List<int> { 11 },
                Effects = new List<ResearchEffect>
                {
                    new ResearchEffect { Type = ResearchEffectType.IncreaseResearchSpeed, Amount = 3 }
                }
            },
            new ResearchDefinition
            {
                Id = 16,
                Name = "Літопис і облікові книги",
                Branch = "Адміністрація",
                Description = "Збір даних про землі, податки та людність.",
                ScienceCost = 320,
                MoneyCost = 30000,
                DurationDays = 9,
                SortOrder = 55,
                Requirements = new List<int> { 8 },
                Effects = new List<ResearchEffect>
                {
                    new ResearchEffect { Type = ResearchEffectType.IncreaseResearchSpeed, Amount = 4 }
                }
            },
            new ResearchDefinition
            {
                Id = 17,
                Name = "Скарбничий розпис",
                Branch = "Адміністрація",
                Description = "Планування зборів і видатків казни.",
                ScienceCost = 360,
                MoneyCost = 35000,
                DurationDays = 10,
                SortOrder = 60,
                Requirements = new List<int> { 9, 12 },
                Effects = new List<ResearchEffect>
                {
                    new ResearchEffect { Type = ResearchEffectType.ReduceCorruption, Amount = 6 }
                }
            },
            new ResearchDefinition
            {
                Id = 18,
                Name = "Статутні грамоти",
                Branch = "Адміністрація",
                Description = "Закріплення прав станів і обов'язків корони.",
                ScienceCost = 420,
                MoneyCost = 50000,
                DurationDays = 12,
                SortOrder = 70,
                Requirements = new List<int> { 4, 19 },
                Effects = new List<ResearchEffect>
                {
                    new ResearchEffect { Type = ResearchEffectType.IncreaseParliamentSeats, Amount = 15 }
                }
            },
            new ResearchDefinition
            {
                Id = 19,
                Name = "Звичаєве право",
                Branch = "Право",
                Description = "Упорядковані традиції вирішення спорів.",
                ScienceCost = 60,
                MoneyCost = 2000,
                DurationDays = 3,
                SortOrder = 5,
                Effects = new List<ResearchEffect>
                {
                    new ResearchEffect { Type = ResearchEffectType.ReduceCorruption, Amount = 1 }
                }
            },
            new ResearchDefinition
            {
                Id = 20,
                Name = "Звід законів",
                Branch = "Право",
                Description = "Письмовий збірник правил і покарань.",
                ScienceCost = 140,
                MoneyCost = 9000,
                DurationDays = 6,
                SortOrder = 15,
                Requirements = new List<int> { 1, 19 },
                Effects = new List<ResearchEffect>
                {
                    new ResearchEffect { Type = ResearchEffectType.ReduceCorruption, Amount = 3 }
                }
            },
            new ResearchDefinition
            {
                Id = 21,
                Name = "Судова система",
                Branch = "Право",
                Description = "Створення судів та процедур розгляду справ.",
                ScienceCost = 170,
                MoneyCost = 12000,
                DurationDays = 7,
                SortOrder = 20,
                Requirements = new List<int> { 20 },
                Effects = new List<ResearchEffect>
                {
                    new ResearchEffect { Type = ResearchEffectType.ReduceCorruption, Amount = 4 }
                }
            },
            new ResearchDefinition
            {
                Id = 22,
                Name = "Судові книги і печаті",
                Branch = "Право",
                Description = "Офіційні записи угод, власності й спадку.",
                ScienceCost = 190,
                MoneyCost = 15000,
                DurationDays = 7,
                SortOrder = 25,
                Requirements = new List<int> { 20 },
                Effects = new List<ResearchEffect>
                {
                    new ResearchEffect { Type = ResearchEffectType.ReduceCorruption, Amount = 4 }
                }
            },
            new ResearchDefinition
            {
                Id = 23,
                Name = "Міське право",
                Branch = "Право",
                Description = "Правила самоврядування для міст і гільдій.",
                ScienceCost = 210,
                MoneyCost = 16000,
                DurationDays = 8,
                SortOrder = 30,
                Requirements = new List<int> { 20 },
                Effects = new List<ResearchEffect>
                {
                    new ResearchEffect { Type = ResearchEffectType.ReduceCorruption, Amount = 3 }
                }
            },
            new ResearchDefinition
            {
                Id = 24,
                Name = "Купецьке право",
                Branch = "Право",
                Description = "Норми для торгівлі, боргів та партнерств.",
                ScienceCost = 240,
                MoneyCost = 20000,
                DurationDays = 8,
                SortOrder = 35,
                Requirements = new List<int> { 20 },
                Effects = new List<ResearchEffect>
                {
                    new ResearchEffect { Type = ResearchEffectType.ReduceCorruption, Amount = 3 }
                }
            },
            new ResearchDefinition
            {
                Id = 25,
                Name = "Відкритий суд",
                Branch = "Право",
                Description = "Публічні слухання та оголошення вироків.",
                ScienceCost = 260,
                MoneyCost = 22000,
                DurationDays = 9,
                SortOrder = 40,
                Requirements = new List<int> { 21 },
                Effects = new List<ResearchEffect>
                {
                    new ResearchEffect { Type = ResearchEffectType.ReduceCorruption, Amount = 5 }
                }
            },
            new ResearchDefinition
            {
                Id = 26,
                Name = "Права підданих",
                Branch = "Право",
                Description = "Визначення базових прав та обов'язків підданих.",
                ScienceCost = 320,
                MoneyCost = 30000,
                DurationDays = 10,
                SortOrder = 50,
                Requirements = new List<int> { 20, 4 },
                Effects = new List<ResearchEffect>
                {
                    new ResearchEffect { Type = ResearchEffectType.ReduceCorruption, Amount = 6 }
                }
            },
            new ResearchDefinition
            {
                Id = 27,
                Name = "Станові вольності",
                Branch = "Право",
                Description = "Гарантії прав станів і рівновага влади.",
                ScienceCost = 400,
                MoneyCost = 45000,
                DurationDays = 12,
                SortOrder = 60,
                Requirements = new List<int> { 18, 26 },
                Effects = new List<ResearchEffect>
                {
                    new ResearchEffect { Type = ResearchEffectType.ReduceCorruption, Amount = 7 }
                }
            },
            new ResearchDefinition
            {
                Id = 28,
                Name = "Натурфілософія",
                Branch = "Наука",
                Description = "Спроби пояснити явища природи системно.",
                ScienceCost = 80,
                MoneyCost = 4000,
                DurationDays = 4,
                SortOrder = 12,
                Requirements = new List<int> { 3 },
                Effects = new List<ResearchEffect>
                {
                    new ResearchEffect { Type = ResearchEffectType.IncreaseResearchSpeed, Amount = 2 }
                }
            },
            new ResearchDefinition
            {
                Id = 29,
                Name = "Травництво і лікування",
                Branch = "Наука",
                Description = "Збір лікувальних трав і базова медицина.",
                ScienceCost = 100,
                MoneyCost = 5000,
                DurationDays = 4,
                SortOrder = 15,
                Requirements = new List<int> { 1 },
                Effects = new List<ResearchEffect>
                {
                    new ResearchEffect { Type = ResearchEffectType.IncreaseResearchSpeed, Amount = 1 }
                }
            },
            new ResearchDefinition
            {
                Id = 30,
                Name = "Астрономія",
                Branch = "Наука",
                Description = "Спостереження небесних тіл для календаря та навігації.",
                ScienceCost = 140,
                MoneyCost = 9000,
                DurationDays = 6,
                SortOrder = 20,
                Requirements = new List<int> { 28 },
                Effects = new List<ResearchEffect>
                {
                    new ResearchEffect { Type = ResearchEffectType.IncreaseResearchSpeed, Amount = 2 }
                }
            },
            new ResearchDefinition
            {
                Id = 31,
                Name = "Картографія",
                Branch = "Наука",
                Description = "Створення точних карт земель і морів.",
                ScienceCost = 160,
                MoneyCost = 10000,
                DurationDays = 6,
                SortOrder = 25,
                Requirements = new List<int> { 30 },
                Effects = new List<ResearchEffect>
                {
                    new ResearchEffect { Type = ResearchEffectType.IncreaseResearchSpeed, Amount = 2 }
                }
            },
            new ResearchDefinition
            {
                Id = 32,
                Name = "Навігація",
                Branch = "Наука",
                Description = "Прилади й методи мореплавства.",
                ScienceCost = 180,
                MoneyCost = 12000,
                DurationDays = 7,
                SortOrder = 30,
                Requirements = new List<int> { 31 },
                Effects = new List<ResearchEffect>
                {
                    new ResearchEffect { Type = ResearchEffectType.IncreaseResearchSpeed, Amount = 2 }
                }
            },
            new ResearchDefinition
            {
                Id = 33,
                Name = "Алхімія",
                Branch = "Наука",
                Description = "Досліди з речовинами, еліксирами та реагентами.",
                ScienceCost = 200,
                MoneyCost = 14000,
                DurationDays = 7,
                SortOrder = 35,
                Requirements = new List<int> { 28 },
                Effects = new List<ResearchEffect>
                {
                    new ResearchEffect { Type = ResearchEffectType.IncreaseResearchSpeed, Amount = 3 }
                }
            },
            new ResearchDefinition
            {
                Id = 34,
                Name = "Університети",
                Branch = "Наука",
                Description = "Постійні осередки навчання та досліджень.",
                ScienceCost = 260,
                MoneyCost = 20000,
                DurationDays = 8,
                SortOrder = 40,
                Requirements = new List<int> { 3, 28 },
                Effects = new List<ResearchEffect>
                {
                    new ResearchEffect { Type = ResearchEffectType.IncreaseResearchSpeed, Amount = 6 }
                }
            },
            new ResearchDefinition
            {
                Id = 35,
                Name = "Скрипторії та переписувачі",
                Branch = "Наука",
                Description = "Організоване переписування книг і знань.",
                ScienceCost = 280,
                MoneyCost = 25000,
                DurationDays = 8,
                SortOrder = 45,
                Requirements = new List<int> { 1, 34 },
                Effects = new List<ResearchEffect>
                {
                    new ResearchEffect { Type = ResearchEffectType.IncreaseResearchSpeed, Amount = 5 }
                }
            },
            new ResearchDefinition
            {
                Id = 36,
                Name = "Анатомія",
                Branch = "Наука",
                Description = "Поглиблене вивчення тіла та лікування.",
                ScienceCost = 300,
                MoneyCost = 22000,
                DurationDays = 8,
                SortOrder = 50,
                Requirements = new List<int> { 29, 34 },
                Effects = new List<ResearchEffect>
                {
                    new ResearchEffect { Type = ResearchEffectType.IncreaseResearchSpeed, Amount = 3 }
                }
            },
            new ResearchDefinition
            {
                Id = 37,
                Name = "Схоластика і диспути",
                Branch = "Наука",
                Description = "Упорядкування знань через трактати та публічні суперечки.",
                ScienceCost = 400,
                MoneyCost = 35000,
                DurationDays = 10,
                SortOrder = 60,
                Requirements = new List<int> { 34 },
                Effects = new List<ResearchEffect>
                {
                    new ResearchEffect { Type = ResearchEffectType.IncreaseResearchSpeed, Amount = 8 }
                }
            },
            new ResearchDefinition
            {
                Id = 38,
                Name = "Металургія",
                Branch = "Наука",
                Description = "Плавка руд і створення міцних сплавів.",
                ScienceCost = 420,
                MoneyCost = 30000,
                DurationDays = 10,
                SortOrder = 65,
                Requirements = new List<int> { 33 },
                Effects = new List<ResearchEffect>
                {
                    new ResearchEffect { Type = ResearchEffectType.IncreaseResearchSpeed, Amount = 4 }
                }
            },
            new ResearchDefinition
            {
                Id = 39,
                Name = "Оптика і лінзи",
                Branch = "Наука",
                Description = "Лінзи для точних вимірів і спостережень.",
                ScienceCost = 380,
                MoneyCost = 28000,
                DurationDays = 9,
                SortOrder = 55,
                Requirements = new List<int> { 34 },
                Effects = new List<ResearchEffect>
                {
                    new ResearchEffect { Type = ResearchEffectType.IncreaseResearchSpeed, Amount = 4 }
                }
            },
            new ResearchDefinition
            {
                Id = 40,
                Name = "Гідравліка",
                Branch = "Наука",
                Description = "Використання сили води для механічної роботи.",
                ScienceCost = 550,
                MoneyCost = 60000,
                DurationDays = 12,
                SortOrder = 70,
                Requirements = new List<int> { 37 },
                Effects = new List<ResearchEffect>
                {
                    new ResearchEffect { Type = ResearchEffectType.IncreaseResearchSpeed, Amount = 6 }
                }
            },
            new ResearchDefinition
            {
                Id = 41,
                Name = "Механічні годинники",
                Branch = "Наука",
                Description = "Точні механізми, шестерні та хід часу.",
                ScienceCost = 750,
                MoneyCost = 90000,
                DurationDays = 14,
                SortOrder = 80,
                Requirements = new List<int> { 40 },
                Effects = new List<ResearchEffect>
                {
                    new ResearchEffect { Type = ResearchEffectType.IncreaseResearchSpeed, Amount = 8 }
                }
            },
            new ResearchDefinition
            {
                Id = 42,
                Name = "Сигнальні вежі",
                Branch = "Наука",
                Description = "Передача повідомлень через дим, вогні та прапори.",
                ScienceCost = 900,
                MoneyCost = 120000,
                DurationDays = 15,
                SortOrder = 90,
                Requirements = new List<int> { 41 },
                Effects = new List<ResearchEffect>
                {
                    new ResearchEffect { Type = ResearchEffectType.IncreaseResearchSpeed, Amount = 6 }
                }
            },
            new ResearchDefinition
            {
                Id = 43,
                Name = "Лазні та гігієна",
                Branch = "Наука",
                Description = "Чистота, лазні та правила догляду.",
                ScienceCost = 500,
                MoneyCost = 40000,
                DurationDays = 11,
                SortOrder = 75,
                Requirements = new List<int> { 36 },
                Effects = new List<ResearchEffect>
                {
                    new ResearchEffect { Type = ResearchEffectType.IncreaseResearchSpeed, Amount = 4 }
                }
            },
            new ResearchDefinition
            {
                Id = 44,
                Name = "Прості механізми",
                Branch = "Наука",
                Description = "Важелі, блоки та підйомні машини.",
                ScienceCost = 320,
                MoneyCost = 24000,
                DurationDays = 8,
                SortOrder = 52,
                Requirements = new List<int> { 34 },
                Effects = new List<ResearchEffect>
                {
                    new ResearchEffect { Type = ResearchEffectType.IncreaseResearchSpeed, Amount = 3 }
                }
            },
            new ResearchDefinition
            {
                Id = 45,
                Name = "Сівозміна",
                Branch = "Економіка",
                Description = "Покращення врожаїв через чергування культур.",
                ScienceCost = 80,
                MoneyCost = 4000,
                DurationDays = 4,
                SortOrder = 10,
                Effects = new List<ResearchEffect>
                {
                    new ResearchEffect { Type = ResearchEffectType.Economy, Value = "LogisticsEfficiency", Amount = 1 }
                }
            },
            new ResearchDefinition
            {
                Id = 46,
                Name = "Зрошення та канали",
                Branch = "Економіка",
                Description = "Керування водою для полів і міст.",
                ScienceCost = 120,
                MoneyCost = 9000,
                DurationDays = 6,
                SortOrder = 20,
                Requirements = new List<int> { 45 },
                Effects = new List<ResearchEffect>
                {
                    new ResearchEffect { Type = ResearchEffectType.Economy, Value = "LogisticsEfficiency", Amount = 1 }
                }
            },
            new ResearchDefinition
            {
                Id = 47,
                Name = "Млини",
                Branch = "Економіка",
                Description = "Механізація помолу та ремесел.",
                ScienceCost = 130,
                MoneyCost = 8000,
                DurationDays = 5,
                SortOrder = 30,
                Requirements = new List<int> { 45 },
                Effects = new List<ResearchEffect>
                {
                    new ResearchEffect { Type = ResearchEffectType.Economy, Value = "LogisticsEfficiency", Amount = 2 }
                }
            },
            new ResearchDefinition
            {
                Id = 48,
                Name = "Гільдії ремісників",
                Branch = "Економіка",
                Description = "Об'єднання майстрів для якості та стандартів.",
                ScienceCost = 160,
                MoneyCost = 12000,
                DurationDays = 6,
                SortOrder = 40,
                Requirements = new List<int> { 45 },
                Effects = new List<ResearchEffect>
                {
                    new ResearchEffect { Type = ResearchEffectType.Economy, Value = "LogisticsEfficiency", Amount = 2 }
                }
            },
            new ResearchDefinition
            {
                Id = 49,
                Name = "Ринки та ярмарки",
                Branch = "Економіка",
                Description = "Регулярні торгові події та міські ринки.",
                ScienceCost = 170,
                MoneyCost = 14000,
                DurationDays = 6,
                SortOrder = 45,
                Requirements = new List<int> { 48 },
                Effects = new List<ResearchEffect>
                {
                    new ResearchEffect { Type = ResearchEffectType.Economy, Value = "LogisticsEfficiency", Amount = 2 }
                }
            },
            new ResearchDefinition
            {
                Id = 50,
                Name = "Караванна торгівля",
                Branch = "Економіка",
                Description = "Довгі торгові маршрути й охоронні каравани.",
                ScienceCost = 200,
                MoneyCost = 18000,
                DurationDays = 7,
                SortOrder = 50,
                Requirements = new List<int> { 49 },
                Effects = new List<ResearchEffect>
                {
                    new ResearchEffect { Type = ResearchEffectType.Economy, Value = "LogisticsEfficiency", Amount = 2 }
                }
            },
            new ResearchDefinition
            {
                Id = 51,
                Name = "Міняйли та позики",
                Branch = "Економіка",
                Description = "Обмін монет, зберігання коштів і позики.",
                ScienceCost = 240,
                MoneyCost = 22000,
                DurationDays = 8,
                SortOrder = 60,
                Requirements = new List<int> { 49 },
                Effects = new List<ResearchEffect>
                {
                    new ResearchEffect { Type = ResearchEffectType.Economy, Value = "LogisticsEfficiency", Amount = 3 }
                }
            },
            new ResearchDefinition
            {
                Id = 52,
                Name = "Купецькі каси взаємодопомоги",
                Branch = "Економіка",
                Description = "Спільні фонди для покриття втрат караванів.",
                ScienceCost = 260,
                MoneyCost = 24000,
                DurationDays = 8,
                SortOrder = 65,
                Requirements = new List<int> { 50, 51 },
                Effects = new List<ResearchEffect>
                {
                    new ResearchEffect { Type = ResearchEffectType.Economy, Value = "LogisticsEfficiency", Amount = 3 }
                }
            },
            new ResearchDefinition
            {
                Id = 53,
                Name = "Великі майстерні",
                Branch = "Економіка",
                Description = "Зосереджене виробництво у майстернях і цехах.",
                ScienceCost = 320,
                MoneyCost = 35000,
                DurationDays = 9,
                SortOrder = 70,
                Requirements = new List<int> { 48 },
                Effects = new List<ResearchEffect>
                {
                    new ResearchEffect { Type = ResearchEffectType.Economy, Value = "LogisticsEfficiency", Amount = 3 }
                }
            },
            new ResearchDefinition
            {
                Id = 54,
                Name = "Купецькі двори",
                Branch = "Економіка",
                Description = "Склади й торгові двори для великих угод.",
                ScienceCost = 360,
                MoneyCost = 40000,
                DurationDays = 10,
                SortOrder = 75,
                Requirements = new List<int> { 51 },
                Effects = new List<ResearchEffect>
                {
                    new ResearchEffect { Type = ResearchEffectType.Economy, Value = "LogisticsEfficiency", Amount = 4 }
                }
            },
            new ResearchDefinition
            {
                Id = 55,
                Name = "Водяні колеса та механізми",
                Branch = "Економіка",
                Description = "Сила води для шахт, млинів і майстерень.",
                ScienceCost = 520,
                MoneyCost = 70000,
                DurationDays = 12,
                SortOrder = 80,
                Requirements = new List<int> { 53, 40 },
                Effects = new List<ResearchEffect>
                {
                    new ResearchEffect { Type = ResearchEffectType.Economy, Value = "LogisticsEfficiency", Amount = 5 }
                }
            },
            new ResearchDefinition
            {
                Id = 56,
                Name = "Бруковані дороги",
                Branch = "Економіка",
                Description = "Поліпшені шляхи для швидших перевезень.",
                ScienceCost = 700,
                MoneyCost = 100000,
                DurationDays = 14,
                SortOrder = 90,
                Requirements = new List<int> { 55 },
                Effects = new List<ResearchEffect>
                {
                    new ResearchEffect { Type = ResearchEffectType.Economy, Value = "LogisticsEfficiency", Amount = 5 }
                }
            },
            new ResearchDefinition
            {
                Id = 57,
                Name = "Механізовані цехи",
                Branch = "Економіка",
                Description = "Використання водяних приводів і шестерень у виробництві.",
                ScienceCost = 900,
                MoneyCost = 140000,
                DurationDays = 16,
                SortOrder = 100,
                Requirements = new List<int> { 56, 41 },
                Effects = new List<ResearchEffect>
                {
                    new ResearchEffect { Type = ResearchEffectType.Economy, Value = "LogisticsEfficiency", Amount = 6 }
                }
            },
            new ResearchDefinition
            {
                Id = 58,
                Name = "Цехові статути",
                Branch = "Економіка",
                Description = "Уніфікація виробів і контроль якості.",
                ScienceCost = 600,
                MoneyCost = 80000,
                DurationDays = 13,
                SortOrder = 85,
                Requirements = new List<int> { 53, 37 },
                Effects = new List<ResearchEffect>
                {
                    new ResearchEffect { Type = ResearchEffectType.Economy, Value = "LogisticsEfficiency", Amount = 5 }
                }
            },
            new ResearchDefinition
            {
                Id = 59,
                Name = "Ополчення",
                Branch = "Військова",
                Description = "Місцеві загони для оборони.",
                ScienceCost = 60,
                MoneyCost = 3000,
                DurationDays = 3,
                SortOrder = 10,
                Effects = new List<ResearchEffect>
                {
                    new ResearchEffect { Type = ResearchEffectType.Army, Value = "ArmyMorale", Amount = 1 }
                }
            },
            new ResearchDefinition
            {
                Id = 60,
                Name = "Бойова дисципліна",
                Branch = "Військова",
                Description = "Єдині правила тренувань і підпорядкування.",
                ScienceCost = 120,
                MoneyCost = 8000,
                DurationDays = 5,
                SortOrder = 20,
                Requirements = new List<int> { 59 },
                Effects = new List<ResearchEffect>
                {
                    new ResearchEffect { Type = ResearchEffectType.Army, Value = "ArmyMorale", Amount = 1 }
                }
            },
            new ResearchDefinition
            {
                Id = 61,
                Name = "Фортифікації",
                Branch = "Військова",
                Description = "Мури, вежі та укріплені лінії.",
                ScienceCost = 140,
                MoneyCost = 12000,
                DurationDays = 6,
                SortOrder = 30,
                Requirements = new List<int> { 60 },
                Effects = new List<ResearchEffect>
                {
                    new ResearchEffect { Type = ResearchEffectType.Army, Value = "ArmyMorale", Amount = 2 }
                }
            },
            new ResearchDefinition
            {
                Id = 62,
                Name = "Лучники",
                Branch = "Військова",
                Description = "Підготовка стрільців для масованого вогню.",
                ScienceCost = 120,
                MoneyCost = 9000,
                DurationDays = 5,
                SortOrder = 25,
                Requirements = new List<int> { 59 },
                Effects = new List<ResearchEffect>
                {
                    new ResearchEffect { Type = ResearchEffectType.Army, Value = "ArmyMorale", Amount = 1 }
                }
            },
            new ResearchDefinition
            {
                Id = 63,
                Name = "Арбалети",
                Branch = "Військова",
                Description = "Механічна стрілецька зброя з високою пробивністю.",
                ScienceCost = 160,
                MoneyCost = 14000,
                DurationDays = 6,
                SortOrder = 35,
                Requirements = new List<int> { 62 },
                Effects = new List<ResearchEffect>
                {
                    new ResearchEffect { Type = ResearchEffectType.Army, Value = "ArmyMorale", Amount = 2 }
                }
            },
            new ResearchDefinition
            {
                Id = 64,
                Name = "Кіннота",
                Branch = "Військова",
                Description = "Важка і легка кіннота для швидких ударів.",
                ScienceCost = 180,
                MoneyCost = 16000,
                DurationDays = 6,
                SortOrder = 40,
                Requirements = new List<int> { 60 },
                Effects = new List<ResearchEffect>
                {
                    new ResearchEffect { Type = ResearchEffectType.Army, Value = "ArmyMorale", Amount = 2 }
                }
            },
            new ResearchDefinition
            {
                Id = 65,
                Name = "Облогові машини",
                Branch = "Військова",
                Description = "Таран, катапульти та інші знаряддя облоги.",
                ScienceCost = 200,
                MoneyCost = 18000,
                DurationDays = 7,
                SortOrder = 45,
                Requirements = new List<int> { 61 },
                Effects = new List<ResearchEffect>
                {
                    new ResearchEffect { Type = ResearchEffectType.Army, Value = "ArmyMorale", Amount = 2 }
                }
            },
            new ResearchDefinition
            {
                Id = 66,
                Name = "Зброярські майстерні",
                Branch = "Військова",
                Description = "Виробництво якісної зброї та обладунків.",
                ScienceCost = 300,
                MoneyCost = 26000,
                DurationDays = 8,
                SortOrder = 55,
                Requirements = new List<int> { 38 },
                Effects = new List<ResearchEffect>
                {
                    new ResearchEffect { Type = ResearchEffectType.Army, Value = "ArmyMorale", Amount = 3 }
                }
            },
            new ResearchDefinition
            {
                Id = 67,
                Name = "Тяжка піхота",
                Branch = "Військова",
                Description = "Добре озброєна піхота в міцних обладунках.",
                ScienceCost = 380,
                MoneyCost = 35000,
                DurationDays = 9,
                SortOrder = 60,
                Requirements = new List<int> { 66 },
                Effects = new List<ResearchEffect>
                {
                    new ResearchEffect { Type = ResearchEffectType.Army, Value = "ArmyMorale", Amount = 4 }
                }
            },
            new ResearchDefinition
            {
                Id = 68,
                Name = "Облогові інженери",
                Branch = "Військова",
                Description = "Будівництво мостів, укріплень і підкопів.",
                ScienceCost = 320,
                MoneyCost = 30000,
                DurationDays = 9,
                SortOrder = 65,
                Requirements = new List<int> { 65 },
                Effects = new List<ResearchEffect>
                {
                    new ResearchEffect { Type = ResearchEffectType.Army, Value = "ArmyMorale", Amount = 3 }
                }
            },
            new ResearchDefinition
            {
                Id = 69,
                Name = "Школа тактики",
                Branch = "Військова",
                Description = "Навчання командирів і вивчення військових трактатів.",
                ScienceCost = 420,
                MoneyCost = 45000,
                DurationDays = 10,
                SortOrder = 70,
                Requirements = new List<int> { 34, 60 },
                Effects = new List<ResearchEffect>
                {
                    new ResearchEffect { Type = ResearchEffectType.Army, Value = "ArmyMorale", Amount = 4 }
                }
            },
            new ResearchDefinition
            {
                Id = 70,
                Name = "Постійна дружина",
                Branch = "Військова",
                Description = "Постійні загони з утриманням і постачанням.",
                ScienceCost = 480,
                MoneyCost = 60000,
                DurationDays = 11,
                SortOrder = 75,
                Requirements = new List<int> { 69 },
                Effects = new List<ResearchEffect>
                {
                    new ResearchEffect { Type = ResearchEffectType.Army, Value = "ArmyMorale", Amount = 4 }
                }
            },
            new ResearchDefinition
            {
                Id = 71,
                Name = "Піхотні строї",
                Branch = "Військова",
                Description = "Стройова тактика і уніфіковане спорядження.",
                ScienceCost = 650,
                MoneyCost = 80000,
                DurationDays = 12,
                SortOrder = 80,
                Requirements = new List<int> { 66, 58 },
                Effects = new List<ResearchEffect>
                {
                    new ResearchEffect { Type = ResearchEffectType.Army, Value = "ArmyMorale", Amount = 5 }
                }
            },
            new ResearchDefinition
            {
                Id = 72,
                Name = "Лікарі при війську",
                Branch = "Військова",
                Description = "Допомога пораненим у походах та лазарети.",
                ScienceCost = 360,
                MoneyCost = 28000,
                DurationDays = 8,
                SortOrder = 50,
                Requirements = new List<int> { 36 },
                Effects = new List<ResearchEffect>
                {
                    new ResearchEffect { Type = ResearchEffectType.Army, Value = "ArmyMorale", Amount = 4 }
                }
            },
            new ResearchDefinition
            {
                Id = 73,
                Name = "Посольства",
                Branch = "Дипломатія",
                Description = "Постійні дипломатичні місії.",
                ScienceCost = 80,
                MoneyCost = 5000,
                DurationDays = 4,
                SortOrder = 10,
                Requirements = new List<int> { 1 },
                Effects = new List<ResearchEffect>
                {
                    new ResearchEffect { Type = ResearchEffectType.Economy, Value = "InternationalReputation", Amount = 1 }
                }
            },
            new ResearchDefinition
            {
                Id = 74,
                Name = "Династичні шлюби",
                Branch = "Дипломатія",
                Description = "Союзи через родинні зв'язки.",
                ScienceCost = 120,
                MoneyCost = 8000,
                DurationDays = 5,
                SortOrder = 20,
                Requirements = new List<int> { 73 },
                Effects = new List<ResearchEffect>
                {
                    new ResearchEffect { Type = ResearchEffectType.Economy, Value = "InternationalReputation", Amount = 1 }
                }
            },
            new ResearchDefinition
            {
                Id = 75,
                Name = "Дворовий етикет",
                Branch = "Дипломатія",
                Description = "Етикет двору і процедури переговорів.",
                ScienceCost = 150,
                MoneyCost = 11000,
                DurationDays = 5,
                SortOrder = 30,
                Requirements = new List<int> { 73 },
                Effects = new List<ResearchEffect>
                {
                    new ResearchEffect { Type = ResearchEffectType.Economy, Value = "InternationalReputation", Amount = 2 }
                }
            },
            new ResearchDefinition
            {
                Id = 76,
                Name = "Торгівельні угоди",
                Branch = "Дипломатія",
                Description = "Договори про мита, квоти та обмін.",
                ScienceCost = 180,
                MoneyCost = 14000,
                DurationDays = 6,
                SortOrder = 40,
                Requirements = new List<int> { 75 },
                Effects = new List<ResearchEffect>
                {
                    new ResearchEffect { Type = ResearchEffectType.Economy, Value = "InternationalReputation", Amount = 2 }
                }
            },
            new ResearchDefinition
            {
                Id = 77,
                Name = "Розвідка і шпигунство",
                Branch = "Дипломатія",
                Description = "Мережі агентів для інформації.",
                ScienceCost = 220,
                MoneyCost = 18000,
                DurationDays = 7,
                SortOrder = 50,
                Requirements = new List<int> { 75 },
                Effects = new List<ResearchEffect>
                {
                    new ResearchEffect { Type = ResearchEffectType.Economy, Value = "InternationalReputation", Amount = 3 }
                }
            },
            new ResearchDefinition
            {
                Id = 78,
                Name = "Морські союзи",
                Branch = "Дипломатія",
                Description = "Координація флотів і портів.",
                ScienceCost = 240,
                MoneyCost = 20000,
                DurationDays = 7,
                SortOrder = 55,
                Requirements = new List<int> { 76 },
                Effects = new List<ResearchEffect>
                {
                    new ResearchEffect { Type = ResearchEffectType.Economy, Value = "InternationalReputation", Amount = 3 }
                }
            },
            new ResearchDefinition
            {
                Id = 79,
                Name = "Міжкнязівські угоди",
                Branch = "Дипломатія",
                Description = "Система взаємних зобов'язань і арбітражу.",
                ScienceCost = 280,
                MoneyCost = 25000,
                DurationDays = 8,
                SortOrder = 60,
                Requirements = new List<int> { 75 },
                Effects = new List<ResearchEffect>
                {
                    new ResearchEffect { Type = ResearchEffectType.Economy, Value = "InternationalReputation", Amount = 3 }
                }
            },
            new ResearchDefinition
            {
                Id = 80,
                Name = "Союз корон",
                Branch = "Дипломатія",
                Description = "Постійна рада для вирішення конфліктів.",
                ScienceCost = 450,
                MoneyCost = 60000,
                DurationDays = 12,
                SortOrder = 80,
                Requirements = new List<int> { 79, 18 },
                Effects = new List<ResearchEffect>
                {
                    new ResearchEffect { Type = ResearchEffectType.Economy, Value = "InternationalReputation", Amount = 4 }
                }
            },
            new ResearchDefinition
            {
                Id = 81,
                Name = "Базові руни",
                Branch = "Магія",
                Description = "Прості знаки для збереження мани.",
                ScienceCost = 90,
                MoneyCost = 6000,
                DurationDays = 4,
                SortOrder = 10,
                Effects = new List<ResearchEffect>
                {
                    new ResearchEffect { Type = ResearchEffectType.Magic, Value = "ManaStability", Amount = 1 }
                }
            },
            new ResearchDefinition
            {
                Id = 82,
                Name = "Школи магії",
                Branch = "Магія",
                Description = "Систематизація заклять за напрямами.",
                ScienceCost = 160,
                MoneyCost = 12000,
                DurationDays = 6,
                SortOrder = 20,
                Requirements = new List<int> { 81 },
                Effects = new List<ResearchEffect>
                {
                    new ResearchEffect { Type = ResearchEffectType.IncreaseResearchSpeed, Amount = 3 }
                }
            },
            new ResearchDefinition
            {
                Id = 83,
                Name = "Ритуальна магія",
                Branch = "Магія",
                Description = "Потужні обряди з тривалими підготовками.",
                ScienceCost = 200,
                MoneyCost = 15000,
                DurationDays = 7,
                SortOrder = 30,
                Requirements = new List<int> { 82 },
                Effects = new List<ResearchEffect>
                {
                    new ResearchEffect { Type = ResearchEffectType.Magic, Value = "ManaStability", Amount = 2 }
                }
            },
            new ResearchDefinition
            {
                Id = 84,
                Name = "Зачарування зброї",
                Branch = "Магія",
                Description = "Накладання магії на зброю та обладунки.",
                ScienceCost = 220,
                MoneyCost = 18000,
                DurationDays = 7,
                SortOrder = 35,
                Requirements = new List<int> { 82 },
                Effects = new List<ResearchEffect>
                {
                    new ResearchEffect { Type = ResearchEffectType.Magic, Value = "ManaStability", Amount = 3 }
                }
            },
            new ResearchDefinition
            {
                Id = 85,
                Name = "Магічні бар'єри",
                Branch = "Магія",
                Description = "Стійкі щити та захисні поля.",
                ScienceCost = 260,
                MoneyCost = 22000,
                DurationDays = 8,
                SortOrder = 40,
                Requirements = new List<int> { 83 },
                Effects = new List<ResearchEffect>
                {
                    new ResearchEffect { Type = ResearchEffectType.Magic, Value = "ManaStability", Amount = 3 }
                }
            },
            new ResearchDefinition
            {
                Id = 86,
                Name = "Кристали мани",
                Branch = "Магія",
                Description = "Накопичення та передача магічної енергії.",
                ScienceCost = 300,
                MoneyCost = 26000,
                DurationDays = 9,
                SortOrder = 45,
                Requirements = new List<int> { 82 },
                Effects = new List<ResearchEffect>
                {
                    new ResearchEffect { Type = ResearchEffectType.Magic, Value = "ManaStability", Amount = 3 }
                }
            },
            new ResearchDefinition
            {
                Id = 87,
                Name = "Антимагія",
                Branch = "Магія",
                Description = "Методи приглушення та нейтралізації чар.",
                ScienceCost = 360,
                MoneyCost = 32000,
                DurationDays = 10,
                SortOrder = 55,
                Requirements = new List<int> { 85 },
                Effects = new List<ResearchEffect>
                {
                    new ResearchEffect { Type = ResearchEffectType.Magic, Value = "ManaStability", Amount = 4 }
                }
            },
            new ResearchDefinition
            {
                Id = 88,
                Name = "Телепортація",
                Branch = "Магія",
                Description = "Переміщення предметів та людей на відстані.",
                ScienceCost = 420,
                MoneyCost = 40000,
                DurationDays = 11,
                SortOrder = 60,
                Requirements = new List<int> { 86 },
                Effects = new List<ResearchEffect>
                {
                    new ResearchEffect { Type = ResearchEffectType.Magic, Value = "ManaStability", Amount = 4 }
                }
            },
            new ResearchDefinition
            {
                Id = 89,
                Name = "Арканна інженерія",
                Branch = "Магія",
                Description = "Поєднання магії з механікою та технікою.",
                ScienceCost = 600,
                MoneyCost = 70000,
                DurationDays = 14,
                SortOrder = 70,
                Requirements = new List<int> { 88, 41 },
                Effects = new List<ResearchEffect>
                {
                    new ResearchEffect { Type = ResearchEffectType.Magic, Value = "ManaStability", Amount = 5 }
                }
            },
            new ResearchDefinition
            {
                Id = 90,
                Name = "Магічні мережі",
                Branch = "Магія",
                Description = "Стабільні канали передачі мани і інформації.",
                ScienceCost = 800,
                MoneyCost = 100000,
                DurationDays = 16,
                SortOrder = 80,
                Requirements = new List<int> { 89, 42 },
                Effects = new List<ResearchEffect>
                {
                    new ResearchEffect { Type = ResearchEffectType.Magic, Value = "ManaStability", Amount = 6 }
                }
            },
            new ResearchDefinition
            {
                Id = 91,
                Name = "Феодальні хартії",
                Branch = "Адміністрація",
                Description = "Письмові грамоти про права та повинності васалів.",
                ScienceCost = 60,
                MoneyCost = 3000,
                DurationDays = 3,
                SortOrder = 8,
                Requirements = new List<int> { 1 },
                Effects = new List<ResearchEffect>
                {
                    new ResearchEffect { Type = ResearchEffectType.ReduceCorruption, Amount = 1 }
                }
            },
            new ResearchDefinition
            {
                Id = 92,
                Name = "Домени та маєтки",
                Branch = "Адміністрація",
                Description = "Облік земель і маєтностей, межові книги.",
                ScienceCost = 80,
                MoneyCost = 4000,
                DurationDays = 4,
                SortOrder = 14,
                Requirements = new List<int> { 1 },
                Effects = new List<ResearchEffect>
                {
                    new ResearchEffect { Type = ResearchEffectType.ReduceCorruption, Amount = 2 }
                }
            },
            new ResearchDefinition
            {
                Id = 93,
                Name = "Служба десятників",
                Branch = "Адміністрація",
                Description = "Низова адміністрація для збору податків і підтримки порядку.",
                ScienceCost = 100,
                MoneyCost = 6000,
                DurationDays = 4,
                SortOrder = 16,
                Requirements = new List<int> { 8 },
                Effects = new List<ResearchEffect>
                {
                    new ResearchEffect { Type = ResearchEffectType.ReduceCorruption, Amount = 2 }
                }
            },
            new ResearchDefinition
            {
                Id = 94,
                Name = "Посадові печаті",
                Branch = "Адміністрація",
                Description = "Єдині печаті для наказів, листів і договорів.",
                ScienceCost = 120,
                MoneyCost = 7000,
                DurationDays = 4,
                SortOrder = 24,
                Requirements = new List<int> { 7 },
                Effects = new List<ResearchEffect>
                {
                    new ResearchEffect { Type = ResearchEffectType.ReduceCorruption, Amount = 2 }
                }
            },
            new ResearchDefinition
            {
                Id = 95,
                Name = "Скарбнича палата",
                Branch = "Адміністрація",
                Description = "Окрема палата для контролю казни та зборів.",
                ScienceCost = 200,
                MoneyCost = 14000,
                DurationDays = 6,
                SortOrder = 32,
                Requirements = new List<int> { 9, 12 },
                Effects = new List<ResearchEffect>
                {
                    new ResearchEffect { Type = ResearchEffectType.ReduceCorruption, Amount = 4 }
                }
            },
            new ResearchDefinition
            {
                Id = 96,
                Name = "Дорожні застави",
                Branch = "Адміністрація",
                Description = "Збори мита й охорона ключових шляхів.",
                ScienceCost = 230,
                MoneyCost = 18000,
                DurationDays = 6,
                SortOrder = 48,
                Requirements = new List<int> { 15 },
                Effects = new List<ResearchEffect>
                {
                    new ResearchEffect { Type = ResearchEffectType.ReduceCorruption, Amount = 3 }
                }
            },
            new ResearchDefinition
            {
                Id = 97,
                Name = "Королівські інтенданти",
                Branch = "Адміністрація",
                Description = "Представники корони для нагляду за провінціями.",
                ScienceCost = 260,
                MoneyCost = 22000,
                DurationDays = 7,
                SortOrder = 58,
                Requirements = new List<int> { 11, 13 },
                Effects = new List<ResearchEffect>
                {
                    new ResearchEffect { Type = ResearchEffectType.ReduceCorruption, Amount = 4 }
                }
            },
            new ResearchDefinition
            {
                Id = 101,
                Name = "Феодальне право",
                Branch = "Право",
                Description = "Правила володіння землею, васальних зв'язків і судів.",
                ScienceCost = 80,
                MoneyCost = 4000,
                DurationDays = 4,
                SortOrder = 8,
                Requirements = new List<int> { 19 },
                Effects = new List<ResearchEffect>
                {
                    new ResearchEffect { Type = ResearchEffectType.ReduceCorruption, Amount = 2 }
                }
            },
            new ResearchDefinition
            {
                Id = 102,
                Name = "Клятви та васальні договори",
                Branch = "Право",
                Description = "Форми присяг, договорів служби та відповідальності.",
                ScienceCost = 90,
                MoneyCost = 5000,
                DurationDays = 4,
                SortOrder = 12,
                Requirements = new List<int> { 19 },
                Effects = new List<ResearchEffect>
                {
                    new ResearchEffect { Type = ResearchEffectType.ReduceCorruption, Amount = 1 }
                }
            },
            new ResearchDefinition
            {
                Id = 103,
                Name = "Спадкові статути",
                Branch = "Право",
                Description = "Закріплення порядку спадкування й опіки.",
                ScienceCost = 130,
                MoneyCost = 8000,
                DurationDays = 5,
                SortOrder = 18,
                Requirements = new List<int> { 20 },
                Effects = new List<ResearchEffect>
                {
                    new ResearchEffect { Type = ResearchEffectType.ReduceCorruption, Amount = 2 }
                }
            },
            new ResearchDefinition
            {
                Id = 104,
                Name = "Цехове право",
                Branch = "Право",
                Description = "Правила для ремісничих цехів і майстрів.",
                ScienceCost = 190,
                MoneyCost = 13000,
                DurationDays = 6,
                SortOrder = 28,
                Requirements = new List<int> { 23 },
                Effects = new List<ResearchEffect>
                {
                    new ResearchEffect { Type = ResearchEffectType.ReduceCorruption, Amount = 2 }
                }
            },
            new ResearchDefinition
            {
                Id = 105,
                Name = "Митні правила",
                Branch = "Право",
                Description = "Єдині норми збору мита і проїзних платежів.",
                ScienceCost = 210,
                MoneyCost = 15000,
                DurationDays = 6,
                SortOrder = 33,
                Requirements = new List<int> { 24 },
                Effects = new List<ResearchEffect>
                {
                    new ResearchEffect { Type = ResearchEffectType.ReduceCorruption, Amount = 2 }
                }
            },
            new ResearchDefinition
            {
                Id = 106,
                Name = "Морське право",
                Branch = "Право",
                Description = "Норми для морської торгівлі, фрахту та аварій.",
                ScienceCost = 230,
                MoneyCost = 17000,
                DurationDays = 7,
                SortOrder = 38,
                Requirements = new List<int> { 24 },
                Effects = new List<ResearchEffect>
                {
                    new ResearchEffect { Type = ResearchEffectType.ReduceCorruption, Amount = 2 }
                }
            },
            new ResearchDefinition
            {
                Id = 107,
                Name = "Арбітражні грамоти",
                Branch = "Право",
                Description = "Порядок мирного розв'язання спорів між містами.",
                ScienceCost = 300,
                MoneyCost = 24000,
                DurationDays = 8,
                SortOrder = 55,
                Requirements = new List<int> { 25 },
                Effects = new List<ResearchEffect>
                {
                    new ResearchEffect { Type = ResearchEffectType.ReduceCorruption, Amount = 3 }
                }
            },
            new ResearchDefinition
            {
                Id = 111,
                Name = "Кам'яне мурування",
                Branch = "Наука",
                Description = "Техніки кладки для міцних стін і башт.",
                ScienceCost = 100,
                MoneyCost = 6000,
                DurationDays = 4,
                SortOrder = 18,
                Requirements = new List<int> { 28 },
                Effects = new List<ResearchEffect>
                {
                    new ResearchEffect { Type = ResearchEffectType.IncreaseResearchSpeed, Amount = 1 }
                }
            },
            new ResearchDefinition
            {
                Id = 112,
                Name = "Астролябія",
                Branch = "Наука",
                Description = "Прилад для вимірів неба та навігації.",
                ScienceCost = 150,
                MoneyCost = 9000,
                DurationDays = 5,
                SortOrder = 22,
                Requirements = new List<int> { 30 },
                Effects = new List<ResearchEffect>
                {
                    new ResearchEffect { Type = ResearchEffectType.IncreaseResearchSpeed, Amount = 2 }
                }
            },
            new ResearchDefinition
            {
                Id = 113,
                Name = "Папір та чорнила",
                Branch = "Наука",
                Description = "Дешевші носії знань і стабільні чорнила.",
                ScienceCost = 120,
                MoneyCost = 7000,
                DurationDays = 4,
                SortOrder = 32,
                Requirements = new List<int> { 1 },
                Effects = new List<ResearchEffect>
                {
                    new ResearchEffect { Type = ResearchEffectType.IncreaseResearchSpeed, Amount = 2 }
                }
            },
            new ResearchDefinition
            {
                Id = 114,
                Name = "Скляні майстерні",
                Branch = "Наука",
                Description = "Виробництво скла для посуду й оптики.",
                ScienceCost = 140,
                MoneyCost = 8000,
                DurationDays = 5,
                SortOrder = 34,
                Requirements = new List<int> { 28 },
                Effects = new List<ResearchEffect>
                {
                    new ResearchEffect { Type = ResearchEffectType.IncreaseResearchSpeed, Amount = 1 }
                }
            },
            new ResearchDefinition
            {
                Id = 115,
                Name = "Гірнича справа",
                Branch = "Наука",
                Description = "Підземні розробки, вентиляція та кріплення.",
                ScienceCost = 180,
                MoneyCost = 11000,
                DurationDays = 6,
                SortOrder = 40,
                Requirements = new List<int> { 33 },
                Effects = new List<ResearchEffect>
                {
                    new ResearchEffect { Type = ResearchEffectType.IncreaseResearchSpeed, Amount = 2 }
                }
            },
            new ResearchDefinition
            {
                Id = 116,
                Name = "Архітектура мостів",
                Branch = "Наука",
                Description = "Арки, ферми та техніки будівництва мостів.",
                ScienceCost = 220,
                MoneyCost = 14000,
                DurationDays = 6,
                SortOrder = 48,
                Requirements = new List<int> { 111 },
                Effects = new List<ResearchEffect>
                {
                    new ResearchEffect { Type = ResearchEffectType.IncreaseResearchSpeed, Amount = 2 }
                }
            },
            new ResearchDefinition
            {
                Id = 117,
                Name = "Сторожові маяки",
                Branch = "Наука",
                Description = "Вогневі маяки для берегів та гірських перевалів.",
                ScienceCost = 260,
                MoneyCost = 16000,
                DurationDays = 7,
                SortOrder = 58,
                Requirements = new List<int> { 39 },
                Effects = new List<ResearchEffect>
                {
                    new ResearchEffect { Type = ResearchEffectType.IncreaseResearchSpeed, Amount = 2 }
                }
            },
            new ResearchDefinition
            {
                Id = 121,
                Name = "Соляні промисли",
                Branch = "Економіка",
                Description = "Видобуток і зберігання солі для торгівлі.",
                ScienceCost = 90,
                MoneyCost = 5000,
                DurationDays = 4,
                SortOrder = 12,
                Requirements = new List<int> { 45 },
                Effects = new List<ResearchEffect>
                {
                    new ResearchEffect { Type = ResearchEffectType.Economy, Value = "LogisticsEfficiency", Amount = 1 }
                }
            },
            new ResearchDefinition
            {
                Id = 122,
                Name = "Склади зерна",
                Branch = "Економіка",
                Description = "Запаси зерна для стабільності цін і постачання.",
                ScienceCost = 110,
                MoneyCost = 7000,
                DurationDays = 4,
                SortOrder = 25,
                Requirements = new List<int> { 45 },
                Effects = new List<ResearchEffect>
                {
                    new ResearchEffect { Type = ResearchEffectType.Economy, Value = "LogisticsEfficiency", Amount = 1 }
                }
            },
            new ResearchDefinition
            {
                Id = 123,
                Name = "Вітряні млини",
                Branch = "Економіка",
                Description = "Використання вітру для помолу та ремесел.",
                ScienceCost = 130,
                MoneyCost = 8000,
                DurationDays = 5,
                SortOrder = 28,
                Requirements = new List<int> { 45 },
                Effects = new List<ResearchEffect>
                {
                    new ResearchEffect { Type = ResearchEffectType.Economy, Value = "LogisticsEfficiency", Amount = 2 }
                }
            },
            new ResearchDefinition
            {
                Id = 124,
                Name = "Купецькі гільдії",
                Branch = "Економіка",
                Description = "Об'єднання купців для захисту угод і маршрутів.",
                ScienceCost = 190,
                MoneyCost = 14000,
                DurationDays = 6,
                SortOrder = 52,
                Requirements = new List<int> { 49 },
                Effects = new List<ResearchEffect>
                {
                    new ResearchEffect { Type = ResearchEffectType.Economy, Value = "LogisticsEfficiency", Amount = 2 }
                }
            },
            new ResearchDefinition
            {
                Id = 125,
                Name = "Караван-сараї",
                Branch = "Економіка",
                Description = "Притулки та склади на далеких торгових шляхах.",
                ScienceCost = 210,
                MoneyCost = 16000,
                DurationDays = 6,
                SortOrder = 55,
                Requirements = new List<int> { 50 },
                Effects = new List<ResearchEffect>
                {
                    new ResearchEffect { Type = ResearchEffectType.Economy, Value = "LogisticsEfficiency", Amount = 3 }
                }
            },
            new ResearchDefinition
            {
                Id = 126,
                Name = "Монетний двір",
                Branch = "Економіка",
                Description = "Стабільна чеканка монет і контроль проби.",
                ScienceCost = 230,
                MoneyCost = 18000,
                DurationDays = 7,
                SortOrder = 58,
                Requirements = new List<int> { 51 },
                Effects = new List<ResearchEffect>
                {
                    new ResearchEffect { Type = ResearchEffectType.Economy, Value = "LogisticsEfficiency", Amount = 2 }
                }
            },
            new ResearchDefinition
            {
                Id = 127,
                Name = "Портові доки",
                Branch = "Економіка",
                Description = "Ремонт і спорядження суден у великих портах.",
                ScienceCost = 240,
                MoneyCost = 20000,
                DurationDays = 7,
                SortOrder = 62,
                Requirements = new List<int> { 50 },
                Effects = new List<ResearchEffect>
                {
                    new ResearchEffect { Type = ResearchEffectType.Economy, Value = "LogisticsEfficiency", Amount = 3 }
                }
            },
            new ResearchDefinition
            {
                Id = 131,
                Name = "Кольчужні майстерні",
                Branch = "Військова",
                Description = "Масове виробництво кольчуг і обладунків.",
                ScienceCost = 90,
                MoneyCost = 6000,
                DurationDays = 4,
                SortOrder = 15,
                Requirements = new List<int> { 59 },
                Effects = new List<ResearchEffect>
                {
                    new ResearchEffect { Type = ResearchEffectType.Army, Value = "ArmyMorale", Amount = 1 }
                }
            },
            new ResearchDefinition
            {
                Id = 132,
                Name = "Польові розвідники",
                Branch = "Військова",
                Description = "Легкі загони для розвідки й засідок.",
                ScienceCost = 110,
                MoneyCost = 7000,
                DurationDays = 4,
                SortOrder = 27,
                Requirements = new List<int> { 59 },
                Effects = new List<ResearchEffect>
                {
                    new ResearchEffect { Type = ResearchEffectType.Army, Value = "ArmyMorale", Amount = 1 }
                }
            },
            new ResearchDefinition
            {
                Id = 133,
                Name = "Щитовий лад",
                Branch = "Військова",
                Description = "Стройові формації для утримання лінії.",
                ScienceCost = 130,
                MoneyCost = 8000,
                DurationDays = 5,
                SortOrder = 22,
                Requirements = new List<int> { 60 },
                Effects = new List<ResearchEffect>
                {
                    new ResearchEffect { Type = ResearchEffectType.Army, Value = "ArmyMorale", Amount = 1 }
                }
            },
            new ResearchDefinition
            {
                Id = 134,
                Name = "Замкові донжони",
                Branch = "Військова",
                Description = "Укріплені башти для оборони міст і фортів.",
                ScienceCost = 150,
                MoneyCost = 10000,
                DurationDays = 5,
                SortOrder = 32,
                Requirements = new List<int> { 61 },
                Effects = new List<ResearchEffect>
                {
                    new ResearchEffect { Type = ResearchEffectType.Army, Value = "ArmyMorale", Amount = 2 }
                }
            },
            new ResearchDefinition
            {
                Id = 135,
                Name = "Пікінири",
                Branch = "Військова",
                Description = "Довгі списи для протидії кінноті.",
                ScienceCost = 170,
                MoneyCost = 12000,
                DurationDays = 6,
                SortOrder = 38,
                Requirements = new List<int> { 60 },
                Effects = new List<ResearchEffect>
                {
                    new ResearchEffect { Type = ResearchEffectType.Army, Value = "ArmyMorale", Amount = 2 }
                }
            },
            new ResearchDefinition
            {
                Id = 136,
                Name = "Обоз та постачання",
                Branch = "Військова",
                Description = "Організація обозів, фуражу та складів.",
                ScienceCost = 190,
                MoneyCost = 14000,
                DurationDays = 6,
                SortOrder = 48,
                Requirements = new List<int> { 60 },
                Effects = new List<ResearchEffect>
                {
                    new ResearchEffect { Type = ResearchEffectType.Army, Value = "ArmyMorale", Amount = 1 }
                }
            },
            new ResearchDefinition
            {
                Id = 137,
                Name = "Лицарські ордени",
                Branch = "Військова",
                Description = "Елітні загони з присягою й дисципліною.",
                ScienceCost = 220,
                MoneyCost = 16000,
                DurationDays = 6,
                SortOrder = 52,
                Requirements = new List<int> { 64 },
                Effects = new List<ResearchEffect>
                {
                    new ResearchEffect { Type = ResearchEffectType.Army, Value = "ArmyMorale", Amount = 2 }
                }
            },
            new ResearchDefinition
            {
                Id = 141,
                Name = "Гінці між дворами",
                Branch = "Дипломатія",
                Description = "Швидка передача послань між правителями.",
                ScienceCost = 90,
                MoneyCost = 5000,
                DurationDays = 4,
                SortOrder = 15,
                Requirements = new List<int> { 73 },
                Effects = new List<ResearchEffect>
                {
                    new ResearchEffect { Type = ResearchEffectType.Economy, Value = "InternationalReputation", Amount = 1 }
                }
            },
            new ResearchDefinition
            {
                Id = 142,
                Name = "Посольські дари",
                Branch = "Дипломатія",
                Description = "Традиції дарунків та прийому послів.",
                ScienceCost = 110,
                MoneyCost = 7000,
                DurationDays = 4,
                SortOrder = 25,
                Requirements = new List<int> { 73 },
                Effects = new List<ResearchEffect>
                {
                    new ResearchEffect { Type = ResearchEffectType.Economy, Value = "InternationalReputation", Amount = 1 }
                }
            },
            new ResearchDefinition
            {
                Id = 143,
                Name = "Торгівельні місії",
                Branch = "Дипломатія",
                Description = "Системні поїздки купців і дипломатів.",
                ScienceCost = 160,
                MoneyCost = 11000,
                DurationDays = 5,
                SortOrder = 35,
                Requirements = new List<int> { 76 },
                Effects = new List<ResearchEffect>
                {
                    new ResearchEffect { Type = ResearchEffectType.Economy, Value = "InternationalReputation", Amount = 2 }
                }
            },
            new ResearchDefinition
            {
                Id = 144,
                Name = "Поручительства і заручники",
                Branch = "Дипломатія",
                Description = "Гарантії договорів через заручників і поручителів.",
                ScienceCost = 180,
                MoneyCost = 13000,
                DurationDays = 5,
                SortOrder = 45,
                Requirements = new List<int> { 74 },
                Effects = new List<ResearchEffect>
                {
                    new ResearchEffect { Type = ResearchEffectType.Economy, Value = "InternationalReputation", Amount = 2 }
                }
            },
            new ResearchDefinition
            {
                Id = 145,
                Name = "Морські конвої",
                Branch = "Дипломатія",
                Description = "Охорона торгових шляхів спільними флотами.",
                ScienceCost = 220,
                MoneyCost = 17000,
                DurationDays = 6,
                SortOrder = 58,
                Requirements = new List<int> { 78 },
                Effects = new List<ResearchEffect>
                {
                    new ResearchEffect { Type = ResearchEffectType.Economy, Value = "InternationalReputation", Amount = 2 }
                }
            },
            new ResearchDefinition
            {
                Id = 146,
                Name = "Арбітражні ради",
                Branch = "Дипломатія",
                Description = "Постійні ради для мирного врегулювання спорів.",
                ScienceCost = 260,
                MoneyCost = 20000,
                DurationDays = 7,
                SortOrder = 65,
                Requirements = new List<int> { 79 },
                Effects = new List<ResearchEffect>
                {
                    new ResearchEffect { Type = ResearchEffectType.Economy, Value = "InternationalReputation", Amount = 3 }
                }
            },
            new ResearchDefinition
            {
                Id = 151,
                Name = "Відчуття мани",
                Branch = "Магія",
                Description = "Перші практики відчуття потоків мани.",
                ScienceCost = 70,
                MoneyCost = 4000,
                DurationDays = 3,
                SortOrder = 5,
                Effects = new List<ResearchEffect>
                {
                    new ResearchEffect { Type = ResearchEffectType.Magic, Value = "ManaStability", Amount = 1 }
                }
            },
            new ResearchDefinition
            {
                Id = 152,
                Name = "Фокусні кристали",
                Branch = "Магія",
                Description = "Кристали для концентрації й посилення чар.",
                ScienceCost = 110,
                MoneyCost = 7000,
                DurationDays = 4,
                SortOrder = 15,
                Requirements = new List<int> { 81 },
                Effects = new List<ResearchEffect>
                {
                    new ResearchEffect { Type = ResearchEffectType.Magic, Value = "ManaStability", Amount = 1 }
                }
            },
            new ResearchDefinition
            {
                Id = 153,
                Name = "Захисні обереги",
                Branch = "Магія",
                Description = "Прості обереги для захисту людей і осель.",
                ScienceCost = 130,
                MoneyCost = 8000,
                DurationDays = 4,
                SortOrder = 18,
                Requirements = new List<int> { 81 },
                Effects = new List<ResearchEffect>
                {
                    new ResearchEffect { Type = ResearchEffectType.Magic, Value = "ManaStability", Amount = 1 }
                }
            },
            new ResearchDefinition
            {
                Id = 154,
                Name = "Магічне світло",
                Branch = "Магія",
                Description = "Стабільні світляні закляття для освітлення.",
                ScienceCost = 170,
                MoneyCost = 12000,
                DurationDays = 5,
                SortOrder = 25,
                Requirements = new List<int> { 82 },
                Effects = new List<ResearchEffect>
                {
                    new ResearchEffect { Type = ResearchEffectType.Magic, Value = "ManaStability", Amount = 2 }
                }
            },
            new ResearchDefinition
            {
                Id = 155,
                Name = "Чарівні ліхтарі",
                Branch = "Магія",
                Description = "Портативні світильники на кристалах мани.",
                ScienceCost = 190,
                MoneyCost = 14000,
                DurationDays = 5,
                SortOrder = 28,
                Requirements = new List<int> { 154 },
                Effects = new List<ResearchEffect>
                {
                    new ResearchEffect { Type = ResearchEffectType.Magic, Value = "ManaStability", Amount = 1 }
                }
            },
            new ResearchDefinition
            {
                Id = 156,
                Name = "Рунні маяки",
                Branch = "Магія",
                Description = "Сигнальні вежі з рунним підсиленням.",
                ScienceCost = 210,
                MoneyCost = 16000,
                DurationDays = 6,
                SortOrder = 33,
                Requirements = new List<int> { 154 },
                Effects = new List<ResearchEffect>
                {
                    new ResearchEffect { Type = ResearchEffectType.Magic, Value = "ManaStability", Amount = 1 }
                }
            },
            new ResearchDefinition
            {
                Id = 157,
                Name = "Арканні приводи",
                Branch = "Магія",
                Description = "Магічні механізми для руху пристроїв і возів.",
                ScienceCost = 260,
                MoneyCost = 22000,
                DurationDays = 7,
                SortOrder = 48,
                Requirements = new List<int> { 86 },
                Effects = new List<ResearchEffect>
                {
                    new ResearchEffect { Type = ResearchEffectType.Economy, Value = "LogisticsEfficiency", Amount = 2 }
                }
            },
            new ResearchDefinition
            {
                Id = 158,
                Name = "Лей-лінійні колії",
                Branch = "Магія",
                Description = "Прокладання маршрутів уздовж лей-ліній.",
                ScienceCost = 300,
                MoneyCost = 26000,
                DurationDays = 8,
                SortOrder = 52,
                Requirements = new List<int> { 157 },
                Effects = new List<ResearchEffect>
                {
                    new ResearchEffect { Type = ResearchEffectType.Economy, Value = "LogisticsEfficiency", Amount = 3 }
                }
            },
            new ResearchDefinition
            {
                Id = 159,
                Name = "Маг-поїзд",
                Branch = "Магія",
                Description = "Рухомі вагони на арканних приводах.",
                ScienceCost = 340,
                MoneyCost = 30000,
                DurationDays = 8,
                SortOrder = 55,
                Requirements = new List<int> { 158 },
                Effects = new List<ResearchEffect>
                {
                    new ResearchEffect { Type = ResearchEffectType.Economy, Value = "LogisticsEfficiency", Amount = 4 }
                }
            },
            new ResearchDefinition
            {
                Id = 160,
                Name = "Манопроводи",
                Branch = "Магія",
                Description = "Канали передачі мани між вузлами.",
                ScienceCost = 360,
                MoneyCost = 32000,
                DurationDays = 9,
                SortOrder = 62,
                Requirements = new List<int> { 157 },
                Effects = new List<ResearchEffect>
                {
                    new ResearchEffect { Type = ResearchEffectType.Magic, Value = "ManaStability", Amount = 2 }
                }
            },
            new ResearchDefinition
            {
                Id = 161,
                Name = "Арканні акумулятори",
                Branch = "Магія",
                Description = "Накопичення енергії для великих заклять.",
                ScienceCost = 420,
                MoneyCost = 38000,
                DurationDays = 10,
                SortOrder = 68,
                Requirements = new List<int> { 86 },
                Effects = new List<ResearchEffect>
                {
                    new ResearchEffect { Type = ResearchEffectType.Magic, Value = "ManaStability", Amount = 3 }
                }
            },
            new ResearchDefinition
            {
                Id = 162,
                Name = "Магічні енергомережі",
                Branch = "Магія",
                Description = "Розподіл мани між містами через стабільні вузли.",
                ScienceCost = 950,
                MoneyCost = 110000,
                DurationDays = 16,
                SortOrder = 90,
                Requirements = new List<int> { 90, 161 },
                Effects = new List<ResearchEffect>
                {
                    new ResearchEffect { Type = ResearchEffectType.Magic, Value = "ManaStability", Amount = 5 }
                }
            }
        };

        private static string NormalizeBranchName(string? branch)
        {
            if (string.IsNullOrWhiteSpace(branch))
                return "Загальне";

            var trimmed = branch.Trim();
            if (string.Equals(trimmed, "Воєнна", StringComparison.OrdinalIgnoreCase))
                return "Військова";

            return trimmed;
        }

        private static int ScaleIntCost(int value, decimal multiplier)
        {
            int baseValue = Math.Max(1, value);
            var scaled = (int)Math.Ceiling(baseValue * (double)multiplier);
            return Math.Max(1, scaled);
        }

        private static long ScaleLongCost(long value, decimal multiplier)
        {
            long baseValue = Math.Max(1L, value);
            decimal scaled = Math.Ceiling(baseValue * multiplier);
            if (scaled > long.MaxValue)
                return long.MaxValue;
            return Math.Max(1L, (long)scaled);
        }

        private static int ScaleDurationDays(int days)
        {
            int baseDays = Math.Max(1, days);
            int scaled = (int)Math.Ceiling(baseDays * (double)DurationDaysMultiplier);
            return Math.Clamp(scaled, MinResearchDurationDays, MaxResearchDurationDays);
        }

        private static ResearchDefinition BuildAdjustedDefinition(ResearchDefinition source)
        {
            return new ResearchDefinition
            {
                Id = source.Id,
                Name = source.Name,
                Branch = NormalizeBranchName(source.Branch),
                Description = source.Description,
                ScienceCost = ScaleIntCost(source.ScienceCost, ScienceCostMultiplier),
                MoneyCost = ScaleLongCost(source.MoneyCost, MoneyCostMultiplier),
                DurationDays = ScaleDurationDays(source.DurationDays),
                SortOrder = source.SortOrder,
                IsActive = source.IsActive,
                Requirements = source.Requirements?.ToList() ?? new List<int>(),
                Effects = source.Effects?
                    .Select(e => new ResearchEffect { Type = e.Type, Value = e.Value, Amount = e.Amount })
                    .ToList() ?? new List<ResearchEffect>()
            };
        }

        public static IReadOnlyList<ResearchDefinition> GetAll()
        {
            var db = DataBase.ResearchDefinitionsData.GetAllActive();
            if (db.Count == 0)
            {
                return FallbackDefinitions
                    .Select(BuildAdjustedDefinition)
                    .OrderBy(d => d.SortOrder)
                    .ThenBy(d => d.Id)
                    .ToList();
            }

            var allIds = DataBase.ResearchDefinitionsData.GetAllIds();
            var merged = new List<ResearchDefinition>(db);
            foreach (var def in FallbackDefinitions)
            {
                if (!allIds.Contains(def.Id))
                    merged.Add(def);
            }

            return merged
                .Select(BuildAdjustedDefinition)
                .OrderBy(d => d.SortOrder)
                .ThenBy(d => d.Id)
                .ToList();
        }

        public static ResearchDefinition? GetById(int id)
        {
            var db = DataBase.ResearchDefinitionsData.GetById(id);
            if (db != null)
                return BuildAdjustedDefinition(db);

            var fallback = FallbackDefinitions.Find(r => r.Id == id);
            return fallback == null ? null : BuildAdjustedDefinition(fallback);
        }
    }
}

