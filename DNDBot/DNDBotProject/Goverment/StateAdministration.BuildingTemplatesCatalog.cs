#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;

namespace DNDBot.Goverment
{
    public partial class StateAdministration
    {
        private const int BuildingTemplatePageSize = 12;

        private sealed class BuildingTemplateDefinition
        {
            public string Key { get; }
            public string MenuTitle { get; }
            public string Type { get; }
            public string Category { get; }
            public int JobsProvided { get; }
            public decimal UpkeepCost { get; }
            public decimal BaseProduction { get; }
            public string? ResourceType { get; }
            public bool StateOwned { get; }
            public decimal BuildCost { get; }
            public int BuildTurns { get; }
            public float StabilityBonus { get; }
            public float CorruptionReduction { get; }
            public float ResearchBonus { get; }
            public int ArmyBonus { get; }
            public string FantasyNote { get; }

            public BuildingTemplateDefinition(
                string key,
                string menuTitle,
                string type,
                string category,
                int jobsProvided,
                decimal upkeepCost,
                decimal baseProduction,
                string? resourceType,
                bool stateOwned,
                decimal buildCost,
                int buildTurns,
                string fantasyNote,
                float stabilityBonus = 0f,
                float corruptionReduction = 0f,
                float researchBonus = 0f,
                int armyBonus = 0)
            {
                Key = key;
                MenuTitle = menuTitle;
                Type = type;
                Category = category;
                JobsProvided = jobsProvided;
                UpkeepCost = upkeepCost;
                BaseProduction = baseProduction;
                ResourceType = resourceType;
                StateOwned = stateOwned;
                BuildCost = buildCost;
                BuildTurns = buildTurns;
                StabilityBonus = stabilityBonus;
                CorruptionReduction = corruptionReduction;
                ResearchBonus = researchBonus;
                ArmyBonus = armyBonus;
                FantasyNote = fantasyNote;
            }
        }

        private static readonly IReadOnlyList<BuildingTemplateDefinition> BuildingTemplateCatalog = new List<BuildingTemplateDefinition>
        {
            Template("housing", "🏠 Житло", "Будинки", "Housing", 0, 2m, 24m, "Housing", true, 75m, 2, "Квартали для швидкого зростання населення."),
            Template("farm", "🌾 Ферма", "Ферма", "Production", 20, 3m, 30m, "Food", false, 95m, 3, "Надійна основа продовольчої безпеки."),
            Template("mill", "🌬 Млин", "Млин", "Production", 12, 3.5m, 18m, "Food", false, 110m, 3, "Переробляє зерно на стабільний харчовий запас."),
            Template("smithy", "⚒ Кузня", "Кузня", "Production", 16, 5m, 18m, "Tools", false, 145m, 4, "Кує інструменти і спорядження для армії.", armyBonus: 2),
            Template("weaver", "🧵 Ткацька", "Ткацька майстерня", "Production", 14, 4m, 20m, "Clothing", false, 120m, 3, "Виготовляє одяг для міста й караванів."),
            Template("lumber", "🪚 Лісопилка", "Лісопилка", "Production", 15, 4m, 20m, "Housing", false, 130m, 4, "Постачає деревину для будівництва."),
            Template("quarry", "⛏ Каменоломня", "Каменоломня", "Production", 16, 5m, 24m, "Materials", false, 150m, 4, "Добуває камінь і мінерали для міста."),
            Template("workshop", "🛠 Майстерня", "Реміснича майстерня", "Production", 18, 4.5m, 22m, "Goods", false, 135m, 4, "Універсальна реміснича база для товарів."),
            Template("market", "🛒 Ринок", "Ринок", "Other", 22, 4m, 0m, null, false, 170m, 4, "Торгівельне серце міста.", stabilityBonus: 1.2f),
            Template("guildhall", "🏛 Гільдія", "Гільдія ремісників", "Administration", 18, 5m, 0m, null, false, 210m, 5, "Підвищує контроль якості та податки.", stabilityBonus: 0.7f, corruptionReduction: 2.2f),
            Template("admin", "🏢 Адміністрація", "Адміністрація", "Administration", 10, 5m, 0m, null, true, 195m, 5, "Координує державні служби.", corruptionReduction: 2.5f),
            Template("church", "⛪ Церква", "Церква", "Culture", 10, 3m, 0m, null, true, 115m, 3, "Підтримує мораль і єдність громад.", stabilityBonus: 1.6f, corruptionReduction: 0.6f),
            Template("monastery", "🕍 Монастир", "Монастир", "Science", 12, 4m, 0m, null, true, 220m, 5, "Осередок знань і перепису книг.", stabilityBonus: 0.8f, corruptionReduction: 0.8f, researchBonus: 2.2f),
            Template("university", "🎓 Університет", "Університет", "Science", 8, 7m, 0m, null, true, 280m, 6, "Готує інженерів, вчених і радників.", corruptionReduction: 1f, researchBonus: 3f),
            Template("barracks", "🪖 Казарма", "Казарма", "Military", 0, 8m, 0m, null, true, 205m, 5, "Базова підготовка війська.", armyBonus: 10),
            Template("archery", "🏹 Стрільбище", "Стрільбище", "Military", 0, 6m, 0m, null, true, 165m, 4, "Підсилює дальній бій.", armyBonus: 8),
            Template("stable", "🐎 Стайня", "Стайня", "Military", 6, 5m, 0m, null, true, 160m, 4, "Кавалерійські загони та логістика.", armyBonus: 6),
            Template("tower", "🗼 Вежа", "Сторожова вежа", "Military", 4, 4m, 0m, null, true, 180m, 4, "Контроль околиць і раннє попередження.", stabilityBonus: 0.3f, armyBonus: 5),
            Template("walls", "🧱 Мури", "Міські мури", "Military", 0, 5m, 0m, null, true, 260m, 6, "Оборона столиці та ключових міст.", stabilityBonus: 1f, armyBonus: 12),
            Template("fortress", "🏰 Фортеця", "Фортеця", "Military", 0, 10m, 0m, null, true, 300m, 7, "Центр оборони та резервів.", stabilityBonus: 1.5f, armyBonus: 22),
            Template("granary", "🧺 Зерносховище", "Зерносховище", "Production", 10, 3m, 16m, "Food", true, 125m, 3, "Стратегічний запас зерна.", stabilityBonus: 0.8f),
            Template("theater", "🎭 Театр", "Театр", "Culture", 5, 4m, 0m, null, true, 175m, 4, "Підтримує культуру і настрій міста.", stabilityBonus: 2f),
            Template("factory", "🏭 Фабрика", "Фабрика", "Production", 25, 6m, 40m, "Goods", false, 290m, 6, "Промисловий центр масового виробництва."),
            Template("mint", "🪙 Монетний двір", "Монетний двір", "Administration", 12, 6m, 0m, null, true, 270m, 6, "Зміцнює фінансову дисципліну.", stabilityBonus: 0.5f, corruptionReduction: 1.5f),
            Template("port", "⚓ Порт", "Порт", "Production", 20, 6m, 18m, "Luxury", true, 285m, 6, "Вузол морської торгівлі.", stabilityBonus: 0.7f),
            Template("bakery", "🍞 Пекарня", "Пекарня", "Production", 9, 2.5m, 12m, "Food", false, 105m, 3, "Щоденний харчовий комфорт для міста.", stabilityBonus: 0.2f),
            Template("brewery", "🍺 Броварня", "Броварня", "Production", 11, 3m, 12m, "Luxury", false, 115m, 3, "Товар для ярмарків та свят.", stabilityBonus: 0.3f),
            Template("tannery", "🧴 Дубильня", "Дубильня", "Production", 12, 3.5m, 16m, "Clothing", false, 130m, 4, "Шкіра для ремесел і спорядження."),
            Template("brickworks", "🧱 Цегельня", "Цегельня", "Production", 14, 4m, 20m, "Materials", false, 140m, 4, "Будівельні матеріали для містобудування."),
            Template("bathhouse", "🛁 Лазня", "Лазня", "Culture", 8, 3m, 0m, null, true, 145m, 3, "Покращує гігієну і настрій.", stabilityBonus: 1.1f),
            Template("infirmary", "🏥 Лазарет", "Лазарет", "Science", 10, 4m, 0m, null, true, 230m, 5, "Підтримує здоров'я громадян і гарнізону.", stabilityBonus: 0.7f, corruptionReduction: 0.5f, researchBonus: 0.8f),
            Template("chancery", "📜 Канцелярія", "Канцелярія", "Administration", 9, 4m, 0m, null, true, 225m, 5, "Оформлення рішень і державних указів.", corruptionReduction: 1.8f),
            Template("caravanserai", "🐫 Караван-сарай", "Караван-сарай", "Other", 16, 4m, 8m, "Luxury", false, 185m, 4, "Точка безпеки для торгових караванів.", stabilityBonus: 0.6f),

            Template("inn", "🛏 Заїжджий двір", "Заїжджий двір", "Other", 12, 3.5m, 6m, "Luxury", false, 135m, 3, "Приймає мандрівників і найманців.", stabilityBonus: 0.5f),
            Template("tavern", "🍻 Таверна", "Таверна", "Culture", 10, 3m, 5m, "Luxury", false, 120m, 3, "Місце чуток, контрактів і пісень.", stabilityBonus: 0.7f),
            Template("fishery", "🎣 Рибальська пристань", "Рибальська пристань", "Production", 14, 4m, 26m, "Food", false, 145m, 4, "Стабільне постачання риби та солі."),
            Template("smokehouse", "🥩 Коптильня", "Коптильня", "Production", 11, 3.5m, 16m, "Food", false, 130m, 3, "Збільшує термін зберігання харчів.", stabilityBonus: 0.2f),
            Template("alchemist", "⚗ Алхімічна", "Алхімічна лабораторія", "Science", 9, 5m, 10m, "Goods", false, 210m, 5, "Створює еліксири і реактиви.", researchBonus: 1.6f),
            Template("apothecary", "🧪 Аптекарня", "Аптекарня", "Science", 8, 4m, 8m, "Goods", false, 180m, 4, "Лікувальні суміші для міста.", stabilityBonus: 0.3f, researchBonus: 0.9f),
            Template("glassworks", "🫙 Склярня", "Склярня", "Production", 15, 4.5m, 22m, "Goods", false, 165m, 4, "Виробляє скло і лабораторний посуд."),
            Template("pottery", "🏺 Гончарня", "Гончарня", "Production", 12, 3.8m, 18m, "Goods", false, 135m, 3, "Побутова кераміка і тара."),
            Template("jeweler", "💎 Ювелірня", "Ювелірна майстерня", "Production", 10, 5m, 12m, "Luxury", false, 220m, 5, "Преміум-товари для торгівлі.", stabilityBonus: 0.4f),
            Template("ropewalk", "🪢 Канатня", "Канатна мануфактура", "Production", 11, 3.8m, 17m, "Tools", false, 125m, 3, "Такелаж і канати для флоту."),
            Template("shipyard", "🛶 Верф", "Верф", "Production", 18, 5.8m, 24m, "Goods", true, 260m, 6, "Будує торгові і військові судна.", stabilityBonus: 0.3f, armyBonus: 3),
            Template("warehouse", "📦 Склади", "Торгові склади", "Production", 10, 3.5m, 0m, null, false, 150m, 4, "Знижує втрати ресурсів у логістиці.", stabilityBonus: 0.5f),
            Template("bank", "🏦 Банк", "Міський банк", "Administration", 14, 5m, 0m, null, false, 240m, 5, "Фінансування будівництв та угод.", stabilityBonus: 0.5f, corruptionReduction: 1.6f),
            Template("exchange", "💱 Біржа", "Торгова біржа", "Administration", 12, 4.5m, 0m, null, false, 235m, 5, "Покращує курс і оборот товарів.", stabilityBonus: 0.5f, corruptionReduction: 1.4f),
            Template("embassy", "🕊 Посольство", "Посольство", "Administration", 8, 4.8m, 0m, null, true, 215m, 5, "Впливає на міжнародну довіру.", stabilityBonus: 0.9f, corruptionReduction: 0.7f),
            Template("courthouse", "⚖ Суд", "Суд палати", "Administration", 10, 4.5m, 0m, null, true, 205m, 5, "Підсилює закон і правопорядок.", stabilityBonus: 1.1f, corruptionReduction: 1.6f),
            Template("taxoffice", "🧾 Податкова", "Податкова палата", "Administration", 11, 4m, 0m, null, true, 190m, 4, "Покращує збір податків.", corruptionReduction: 1.9f),
            Template("watchpost", "🛡 Сторожовий пост", "Сторожовий пост", "Military", 4, 3m, 0m, null, true, 120m, 3, "Швидка реакція на загрози.", stabilityBonus: 0.2f, armyBonus: 4),
            Template("thievesguild", "🗝 Тіньова гільдія", "Тіньова гільдія", "Other", 12, 4m, 6m, "Luxury", false, 200m, 4, "Контроль підпільних потоків.", stabilityBonus: 0.2f, corruptionReduction: 0.3f),
            Template("rangerlodge", "🌲 Ложа рейнджерів", "Ложа рейнджерів", "Military", 8, 4.2m, 0m, null, true, 175m, 4, "Патрулі лісів і кордонів.", stabilityBonus: 0.3f, armyBonus: 7),
            Template("scoutcamp", "🧭 Табір розвідки", "Табір розвідки", "Military", 7, 3.8m, 0m, null, true, 150m, 4, "Розвіддані перед битвами.", armyBonus: 6),
            Template("siegeworks", "🧱 Облоговий двір", "Облоговий двір", "Military", 14, 6.5m, 12m, "Tools", true, 275m, 6, "Готує облогову техніку.", armyBonus: 12),
            Template("armory", "🗡 Зброярня", "Зброярня", "Military", 13, 5.5m, 14m, "Tools", true, 225m, 5, "Оснащує гарнізон та дружини.", armyBonus: 9),
            Template("spearforge", "🪓 Копійна кузня", "Копійна кузня", "Military", 12, 5.2m, 18m, "Spear", false, 240m, 5, "Серійне кування списів і дротиків.", armyBonus: 7),
            Template("pikeyard", "🛡 Пікінерний двір", "Пікінерний двір", "Military", 11, 5.4m, 17m, "Pike", false, 245m, 5, "Виробляє піки для стройової піхоти.", armyBonus: 7),
            Template("halberdhall", "⚔ Алебардна майстерня", "Алебардна майстерня", "Military", 12, 5.9m, 16m, "Halberd", false, 255m, 5, "Озброює вартових алебардами.", armyBonus: 8),
            Template("hammerhold", "🔨 Молотобійня", "Молотобійня", "Military", 10, 5.8m, 14m, "WarHammer", false, 250m, 5, "Кує важкі бойові молоти.", armyBonus: 8),
            Template("bladesmith", "🗡 Мечова кузня", "Мечова кузня", "Military", 13, 6.1m, 18m, "Longsword", false, 265m, 5, "Балансує масу і міць клинків.", armyBonus: 8),
            Template("greatforge", "🗡⚡ Велика клинкова", "Велика клинкова", "Military", 11, 6.4m, 15m, "Greatsword", false, 275m, 6, "Дворучна зброя для ударних ліній.", armyBonus: 9),
            Template("bowyerhall", "🏹 Лукарня", "Лукарня", "Military", 12, 5.5m, 18m, "Longbow", false, 240m, 5, "Довгі луки для рейнджерів.", armyBonus: 8),
            Template("crossbowhall", "🎯 Арбалетна", "Арбалетна майстерня", "Military", 12, 5.7m, 16m, "HeavyCrossbow", false, 255m, 5, "Точна зброя гарнізонів і бастіонів.", armyBonus: 8),
            Template("fletchery", "🪶 Флетчерська", "Флетчерська", "Military", 10, 4.6m, 24m, "ArrowBundle", false, 215m, 4, "Масове виготовлення стріл.", armyBonus: 5),
            Template("boltpress", "🧷 Болт-прес", "Болт-прес", "Military", 9, 4.8m, 22m, "BoltBundle", false, 220m, 4, "Пакує болти для арбалетників.", armyBonus: 5),
            Template("powdermill", "🧨 Пороховий млин", "Пороховий млин", "Military", 11, 6.8m, 14m, "BlackPowderCharge", true, 295m, 6, "Стабільні порохові заряди для гармат.", armyBonus: 9),
            Template("alchemyarsenal", "💣 Алхім-арсенал", "Алхім-арсенал", "Military", 10, 6.5m, 13m, "AlchemyBomb", false, 285m, 6, "Готує вибухові суміші і бомби.", researchBonus: 0.8f, armyBonus: 8),
            Template("chainatelier", "⛓ Кольчужна", "Кольчужна майстерня", "Military", 12, 5.9m, 16m, "Chainmail", false, 260m, 5, "Класична кольчуга для лінійної піхоти.", armyBonus: 8),
            Template("platefoundry", "🛡 Латна ливарня", "Латна ливарня", "Military", 11, 6.8m, 13m, "PlateArmor", true, 305m, 6, "Залізні лати для важкої гвардії.", armyBonus: 11),
            Template("shieldworks", "🛡 Щитова", "Щитова мануфактура", "Military", 10, 5.1m, 18m, "TowerShield", false, 245m, 5, "Великі щити для штурмових загонів.", armyBonus: 7),
            Template("runeshieldworks", "🛡✨ Рунна щитова", "Рунна щитова", "Military", 9, 6.9m, 12m, "RunicShield", true, 315m, 6, "Щити з вбудованими захисними рунами.", researchBonus: 1.2f, armyBonus: 10),
            Template("siegeballistaworks", "🏹 Балістний двір", "Балістний двір", "Military", 13, 7.1m, 11m, "SiegeBallista", true, 320m, 6, "Виготовляє польові та бастіонні балісти.", armyBonus: 11),
            Template("trebuchetyard", "🏰 Требушетний майдан", "Требушетний майдан", "Military", 12, 7.3m, 10m, "SiegeTrebuchet", true, 330m, 7, "Далекобійна облогова машина.", armyBonus: 12),
            Template("ramforge", "🪵 Таранний цех", "Таранний цех", "Military", 10, 6.2m, 12m, "SiegeRam", true, 285m, 6, "Штурмові тарани з підсиленою рамою.", armyBonus: 9),
            Template("runecannonfoundry", "🧨 Рунічна гарматня", "Рунічна гарматня", "Military", 12, 8.2m, 9m, "RunicCannon", true, 350m, 7, "Магтех-артилерія для прориву оборони.", researchBonus: 1.4f, armyBonus: 14),
            Template("crystalarmory", "💎 Кристальна арсенальна", "Кристальна арсенальна", "Military", 9, 7.2m, 11m, "ManaCrystal", true, 310m, 6, "Очищує кристали для бойових фокусів.", researchBonus: 1.8f, armyBonus: 8),
            Template("scrollarmory", "📜 Сувійна зброярня", "Сувійна зброярня", "Military", 9, 7.4m, 10m, "SpellScroll", true, 320m, 6, "Бойові сувої для магічних загонів.", researchBonus: 1.9f, armyBonus: 9),
            Template("golemcorelab", "🗿 Лаб. ядер големів", "Лабораторія ядер големів", "Military", 8, 8.8m, 8m, "GolemCore", true, 360m, 7, "Ядра для автономних бойових платформ.", researchBonus: 2f, armyBonus: 12),
            Template("aetherbatteryworks", "🔋 Ефірний акумуляторний цех", "Ефірний акумуляторний цех", "Military", 8, 8.4m, 8m, "AetherBattery", true, 355m, 7, "Живлення для арканних приладів війська.", researchBonus: 1.7f, armyBonus: 11),
            Template("wyvernharnessery", "🐉 Майстерня виверн", "Майстерня виверн", "Military", 9, 7.5m, 9m, "WyvernHarness", true, 340m, 6, "Спорядження для повітряної кавалерії.", armyBonus: 12),
            Template("magefocusforge", "🪄 Кузня фокусів", "Кузня фокусів", "Military", 10, 7m, 11m, "MageFocus", false, 295m, 6, "Фокуси, жезли й бойові канали для магів.", researchBonus: 1.3f, armyBonus: 8),
            Template("warmediclab", "🧪 Військовий медлабораторій", "Військовий медлабораторій", "Military", 10, 6.4m, 12m, "HealingDraught", true, 275m, 5, "Лікувальні еліксири для фронту.", stabilityBonus: 0.4f, armyBonus: 7),
            Template("signalflareforge", "🎇 Фаєрна майстерня", "Фаєрна майстерня", "Military", 7, 5.1m, 14m, "SignalFlare", false, 225m, 4, "Сигнальні засоби координації підрозділів.", armyBonus: 5),
            Template("dronefamiliarforge", "🤖 Фамільярний цех", "Фамільярний цех", "Military", 8, 8m, 8m, "DroneFamiliar", true, 345m, 7, "Магічні дрони-розвідники для штабу.", researchBonus: 1.6f, armyBonus: 10),
            Template("rationfoundry", "🥫 Пайкова мануфактура", "Пайкова мануфактура", "Military", 10, 4.9m, 18m, "FieldRation", true, 230m, 4, "Польові пайки для затяжних кампаній.", stabilityBonus: 0.2f, armyBonus: 6),
            Template("glaiveforge", "🪓 Глефова кузня", "Глефова кузня", "Military", 11, 5.8m, 17m, "Glaive", false, 255m, 5, "Серійні древкові клинки для строїв.", armyBonus: 8),
            Template("flailworks", "⛓ Цепова майстерня", "Цепова майстерня", "Military", 10, 5.5m, 16m, "Flail", false, 245m, 5, "Зміцнені бойові цепи для штурмовиків.", armyBonus: 7),
            Template("katanadojo", "🗡 Додзьо катан", "Додзьо катан", "Military", 9, 6.7m, 12m, "Katana", false, 285m, 6, "Точна клинкова зброя елітних загонів.", armyBonus: 9),
            Template("repeaterhall", "🎯 Репітерна арбалетна", "Репітерна арбалетна", "Military", 11, 6.5m, 13m, "RepeatingCrossbow", false, 295m, 6, "Швидкострільні арбалети для гарнізонів.", armyBonus: 10),
            Template("arcanepistolry", "🔫 Аркан-пістольня", "Аркан-пістольня", "Military", 10, 7.2m, 11m, "ArcanePistol", true, 320m, 6, "Магтех пістолі для рейдових груп.", researchBonus: 1.2f, armyBonus: 9),
            Template("thundermusketworks", "⚡ Мушкетний грімцех", "Мушкетний грімцех", "Military", 10, 7.8m, 10m, "ThunderMusket", true, 335m, 7, "Грозові мушкети з руно-каналом.", researchBonus: 1.4f, armyBonus: 11),
            Template("runerepeaterforge", "🧿 Рунно-репітерний цех", "Рунно-репітерний цех", "Military", 9, 8.3m, 9m, "RuneRepeater", true, 355m, 7, "Репітери з арканним стабілізатором.", researchBonus: 1.8f, armyBonus: 12),
            Template("arcanefletchery", "✨ Аркан-флетчерська", "Аркан-флетчерська", "Military", 10, 5.8m, 17m, "ArcaneArrow", false, 255m, 5, "Стріли з рунами пробиття щитів.", armyBonus: 8),
            Template("shockchargeplant", "⚡ Шок-зарядний цех", "Шок-зарядний цех", "Military", 9, 6.9m, 13m, "ShockCharge", true, 300m, 6, "Електро-манові заряди для спецзброї.", armyBonus: 9),
            Template("sunfirelab", "☀ Лабораторія сонцежару", "Лабораторія сонцежару", "Military", 8, 7.5m, 10m, "SunfireCanister", true, 325m, 6, "Контейнери випалюючого світловогню.", researchBonus: 1.1f, armyBonus: 10),
            Template("halfplateforge", "🛡 Кузня напівлат", "Кузня напівлат", "Military", 11, 6.4m, 14m, "HalfPlate", false, 290m, 6, "Броня балансу рухливості та захисту.", armyBonus: 10),
            Template("dragonscalefoundry", "🐉 Ливарня драколуски", "Ливарня драколуски", "Military", 8, 8.9m, 8m, "DragonScaleArmor", true, 365m, 7, "Драколускові панцирі для ударних рот.", armyBonus: 13),
            Template("adamantinebastion", "⛰ Адамантит-бастіон", "Адамантит-бастіон", "Military", 7, 9.5m, 7m, "AdamantineArmor", true, 390m, 7, "Надважкі лати для гвардії прориву.", armyBonus: 15),
            Template("spellguardatelier", "🧥 Ательє чарозахисту", "Ательє чарозахисту", "Military", 9, 7.1m, 11m, "SpellguardMantle", true, 320m, 6, "Мантії для протимагічного захисту.", researchBonus: 1.3f, armyBonus: 10),
            Template("bastionshieldyard", "🛡 Бастіонна щитова", "Бастіонна щитова", "Military", 10, 6.8m, 13m, "BastionShield", false, 300m, 6, "Щити штурмових і саперних команд.", armyBonus: 10),
            Template("mithrilhelmery", "⛑ Мітрилова шоломарня", "Мітрилова шоломарня", "Military", 9, 6.6m, 13m, "MithrilHelm", false, 295m, 6, "Легкі шоломи з високим захистом.", armyBonus: 9),
            Template("arcanemortaryard", "🧨 Аркан-мортирний двір", "Аркан-мортирний двір", "Military", 9, 9.1m, 8m, "ArcaneMortar", true, 380m, 7, "Облогові мортири з магічним наведенням.", armyBonus: 14),
            Template("flameprojectoryard", "🔥 Полум'яний проектор-цех", "Полум'яний проектор-цех", "Military", 10, 8.4m, 9m, "FlameProjector", true, 350m, 7, "Системи прориву укріплень полум'ям.", armyBonus: 12),
            Template("skyharpoonworks", "🪝 Неболомна гарпунарня", "Неболомна гарпунарня", "Military", 9, 8.8m, 8m, "SkybreakerHarpoon", true, 370m, 7, "Гарпуни проти повітряних цілей.", armyBonus: 13),
            Template("leylinelattice", "💠 Лейлінний вузол", "Лейлінний вузол", "Military", 8, 8.2m, 9m, "LeylineCapacitor", true, 355m, 7, "Акумулює енергію лейліній для війська.", researchBonus: 1.8f, armyBonus: 11),
            Template("voidlenslab", "🕳 Лабораторія порожнинних лінз", "Лабораторія порожнинних лінз", "Military", 8, 8.6m, 8m, "VoidLens", true, 360m, 7, "Оптика для руйнування магічних бар'єрів.", researchBonus: 1.9f, armyBonus: 11),
            Template("riftbeaconworks", "🌀 Маякова розломна", "Маякова розломна", "Military", 7, 9.2m, 7m, "RiftBeacon", true, 385m, 7, "Керування тактичними розломами на фронті.", researchBonus: 2.1f, armyBonus: 13),
            Template("engineerdepot", "🧰 Інженерний депо-цех", "Інженерний депо-цех", "Military", 11, 5.6m, 16m, "EngineerKit", true, 250m, 5, "Набори для польових фортифікацій.", armyBonus: 8),
            Template("siegetoolforge", "🧱 Облоговий інструментарій", "Облоговий інструментарій", "Military", 10, 6.3m, 14m, "SiegeToolkit", true, 285m, 6, "Оснащення саперів та облогових команд.", armyBonus: 10),
            Template("manamedkitlab", "🩺 Мана-медичний цех", "Мана-медичний цех", "Military", 10, 6.2m, 14m, "ManaMedkit", true, 280m, 5, "Відновлення бійців у магічних підрозділах.", stabilityBonus: 0.3f, armyBonus: 8),
            Template("commandatelier", "🎖 Командне сигіл-ательє", "Командне сигіл-ательє", "Military", 8, 7.3m, 11m, "CommandSigil", true, 325m, 6, "Командні знаки для координації армії.", armyBonus: 11),
            Template("wardrumfoundry", "🥁 Барабанна ливарня", "Барабанна ливарня", "Military", 9, 6.1m, 13m, "WarDrum", false, 270m, 5, "Ритм-марш і сигнали бойового духу.", stabilityBonus: 0.2f, armyBonus: 9),
            Template("camouflageatelier", "🫥 Ательє маскування", "Ательє маскування", "Military", 8, 6.8m, 12m, "CamouflageCloak", false, 300m, 6, "Плащі для засідок і рейнджерів.", armyBonus: 10),
            Template("warcollege", "📘 Військова академія", "Військова академія", "Military", 6, 6.5m, 0m, null, true, 280m, 6, "Готує офіцерський корпус.", researchBonus: 1f, armyBonus: 15),
            Template("gryphonaerie", "🦅 Аерій грифонів", "Аерій грифонів", "Military", 9, 7m, 0m, null, true, 295m, 6, "Елітні повітряні патрулі.", stabilityBonus: 0.5f, armyBonus: 16),
            Template("dragonroost", "🐉 Драконячий бастіон", "Драконячий бастіон", "Military", 5, 9m, 0m, null, true, 300m, 7, "Найдорожча бойова платформа.", stabilityBonus: 1f, armyBonus: 20),
            Template("arcanetower", "🔮 Арканічна вежа", "Арканічна вежа", "Science", 7, 7m, 0m, null, true, 290m, 6, "Фокус магічних досліджень.", stabilityBonus: 0.5f, researchBonus: 3f),
            Template("mageacademy", "🧙 Академія магів", "Академія магів", "Science", 9, 6.8m, 0m, null, true, 300m, 7, "Підготовка магістрів і архіваріусів.", corruptionReduction: 0.8f, researchBonus: 3.8f),
            Template("enchantery", "✨ Зачарувальня", "Зачарувальна майстерня", "Science", 10, 6m, 14m, "Tools", false, 255m, 6, "Покращує обладнання магією.", researchBonus: 2.4f),
            Template("runeforge", "🪄 Рунічна кузня", "Рунічна кузня", "Science", 11, 6.5m, 16m, "Tools", true, 270m, 6, "Поєднує ремесло та аркани.", researchBonus: 1.9f, armyBonus: 6),
            Template("observatory", "🔭 Обсерваторія", "Обсерваторія", "Science", 6, 5.5m, 0m, null, true, 230m, 5, "Точні прогнози і наука.", stabilityBonus: 0.4f, researchBonus: 2.8f),
            Template("library", "📚 Велика бібліотека", "Велика бібліотека", "Science", 8, 4.8m, 0m, null, true, 200m, 5, "Сховище трактатів і карт.", stabilityBonus: 0.3f, researchBonus: 2.1f),
            Template("scriptorium", "🖋 Скрипторій", "Скрипторій", "Science", 10, 4.5m, 10m, "Goods", true, 185m, 4, "Швидке копіювання і обіг знань.", researchBonus: 1.5f),
            Template("druidgrove", "🌿 Друїдичний гай", "Друїдичний гай", "Culture", 7, 4m, 8m, "Food", true, 190m, 5, "Гармонізує природу і громаду.", stabilityBonus: 1.2f, researchBonus: 1.1f),
            Template("bardcollege", "🎶 Колегія бардів", "Колегія бардів", "Culture", 9, 4.5m, 0m, null, true, 210m, 5, "Підтримує престиж і дух народу.", stabilityBonus: 1.8f, researchBonus: 0.6f),
            Template("arena", "🏟 Арена", "Арена", "Culture", 12, 4.8m, 0m, null, true, 220m, 5, "Турніри та видовища для містян.", stabilityBonus: 1f, armyBonus: 5),

            Template("spellforge", "🔥 Кузня заклять", "Кузня заклять", "Science", 12, 6m, 18m, "Tools", true, 285m, 6, "Створення магічних фокусів та рунних деталей.", researchBonus: 2.7f, armyBonus: 4),
            Template("manaforge", "💠 Мана-горн", "Мана-горн", "Science", 9, 6.5m, 15m, "Goods", true, 295m, 6, "Акумулює ману для державних ритуалів.", researchBonus: 3.2f),
            Template("crystalmine", "🪨 Кристальна шахта", "Кристальна шахта", "Production", 17, 5.5m, 24m, "Materials", false, 250m, 5, "Добуває чарівні кристали для ремесла."),
            Template("moonwell", "🌙 Місячне джерело", "Місячне джерело", "Culture", 6, 4.2m, 9m, "Food", true, 200m, 5, "Живить місто чистою водою та зменшує смуту.", stabilityBonus: 1.5f, researchBonus: 0.8f),
            Template("starshrine", "⭐ Зоряне святилище", "Зоряне святилище", "Culture", 7, 4.7m, 0m, null, true, 225m, 5, "Місце паломництва і пророцтв.", stabilityBonus: 1.4f, corruptionReduction: 0.9f),
            Template("suntemple", "☀ Храм сонця", "Храм сонця", "Culture", 10, 5m, 0m, null, true, 245m, 5, "Піднімає мораль і громадську дисципліну.", stabilityBonus: 1.9f, corruptionReduction: 1.1f),
            Template("necrolab", "🕯 Некролабораторія", "Некролабораторія", "Science", 8, 6.2m, 12m, "Goods", false, 265m, 6, "Суперечливі дослідження темних мистецтв.", researchBonus: 2.6f),
            Template("boneyard", "⚰ Кістяний двір", "Кістяний двір", "Military", 9, 5.6m, 10m, "Materials", true, 240m, 5, "Резерв логістики в суворі часи.", armyBonus: 8),
            Template("beastpits", "🐗 Ями звірів", "Ями звірів", "Military", 11, 5.4m, 0m, null, true, 210m, 5, "Тренування бойових звірів і приборкувачів.", armyBonus: 10),
            Template("wolfkennel", "🐺 Вовчі псарні", "Вовчі псарні", "Military", 8, 4.3m, 0m, null, true, 170m, 4, "Розвідка та патрулі з бойовими псами.", armyBonus: 7),
            Template("falconry", "🦉 Соколярня", "Соколярня", "Military", 6, 3.9m, 0m, null, true, 160m, 4, "Швидкі донесення і повітряна розвідка.", stabilityBonus: 0.2f, armyBonus: 5),
            Template("hippodrome", "🏇 Іподром", "Іподром", "Culture", 10, 4.8m, 6m, "Luxury", true, 205m, 5, "Перегони для дозвілля та престижу.", stabilityBonus: 1.2f),
            Template("merchanthub", "🛍 Торгівельний двір", "Торгівельний двір", "Other", 16, 4.9m, 12m, "Luxury", false, 220m, 5, "Розподільчий центр караванної торгівлі.", stabilityBonus: 0.7f),
            Template("countinghouse", "📒 Лічильна палата", "Лічильна палата", "Administration", 11, 4.4m, 0m, null, true, 195m, 4, "Фінансовий облік та контроль мит.", corruptionReduction: 1.7f),
            Template("customsdock", "🧾 Митний причал", "Митний причал", "Administration", 12, 4.8m, 0m, null, true, 230m, 5, "Оформлення імпорту та перевірка вантажів.", corruptionReduction: 1.5f, stabilityBonus: 0.4f),
            Template("dryadgrove", "🌳 Гай дріад", "Гай дріад", "Culture", 7, 4m, 10m, "Food", true, 185m, 4, "Природний заповідник і лікувальні трави.", stabilityBonus: 1.3f, researchBonus: 0.7f),
            Template("herbalgarden", "🌺 Травницький сад", "Травницький сад", "Production", 9, 3.7m, 14m, "Goods", false, 140m, 3, "Лікарські трави для аптек і алхімії.", stabilityBonus: 0.3f),
            Template("irrigation", "🚰 Іригаційний вузол", "Іригаційний вузол", "Production", 10, 3.8m, 20m, "Food", true, 175m, 4, "Стабілізує врожаї в посушливі сезони.", stabilityBonus: 0.5f),
            Template("windtower", "🌪 Вітрова вежа", "Вітрова вежа", "Production", 8, 4m, 16m, "Goods", true, 190m, 4, "Механічна енергія для майстерень."),
            Template("froststore", "❄ Крижаний льох", "Крижаний льох", "Production", 8, 3.6m, 15m, "Food", true, 150m, 3, "Довге зберігання продовольства.", stabilityBonus: 0.4f),
            Template("firewatch", "🔥 Вогняний пост", "Вогняний пост", "Military", 5, 3.4m, 0m, null, true, 145m, 3, "Сигнальна система вздовж кордонів.", armyBonus: 5),
            Template("shadowspire", "🌑 Тіньовий шпиль", "Тіньовий шпиль", "Other", 9, 5.2m, 7m, "Luxury", false, 235m, 5, "Контроль таємних каналів впливу.", corruptionReduction: 0.9f),
            Template("skyharbor", "☁ Небесна гавань", "Небесна гавань", "Production", 15, 6m, 20m, "Luxury", true, 300m, 7, "Порт для дирижаблів і повітряних караванів.", stabilityBonus: 0.8f),
            Template("portalgate", "🌀 Портальна брама", "Портальна брама", "Science", 6, 7m, 0m, null, true, 300m, 7, "Дорогий вузол миттєвої логістики.", researchBonus: 3.5f, stabilityBonus: 0.6f),
            Template("sentrykeep", "🛡 Опорний замок", "Опорний замок", "Military", 7, 5.8m, 0m, null, true, 280m, 6, "Посилена прикордонна фортифікація.", stabilityBonus: 0.6f, armyBonus: 13),
            Template("coinvault", "💰 Скарбниця", "Скарбниця", "Administration", 8, 4.5m, 0m, null, true, 260m, 5, "Зберігання резервів і державних фондів.", corruptionReduction: 1.8f, stabilityBonus: 0.5f)
        };

        private static readonly IReadOnlyDictionary<string, BuildingTemplateDefinition> BuildingTemplateByKey =
            BuildingTemplateCatalog.ToDictionary(t => t.Key, t => t, StringComparer.OrdinalIgnoreCase);
        private static readonly IReadOnlyList<BuildingTemplateDefinition> MilitaryBuildingTemplateCatalog =
            BuildingTemplateCatalog
                .Where(IsMilitaryBuildingTemplate)
                .ToList();
        private static readonly IReadOnlyList<BuildingTemplateDefinition> CivilBuildingTemplateCatalog =
            BuildingTemplateCatalog
                .Where(t => !IsMilitaryBuildingTemplate(t))
                .ToList();

        private static BuildingTemplateDefinition Template(
            string key,
            string menuTitle,
            string type,
            string category,
            int jobsProvided,
            decimal upkeepCost,
            decimal baseProduction,
            string? resourceType,
            bool stateOwned,
            decimal buildCost,
            int buildTurns,
            string fantasyNote,
            float stabilityBonus = 0f,
            float corruptionReduction = 0f,
            float researchBonus = 0f,
            int armyBonus = 0)
        {
            return new BuildingTemplateDefinition(
                key,
                menuTitle,
                type,
                category,
                jobsProvided,
                upkeepCost,
                baseProduction,
                resourceType,
                stateOwned,
                buildCost,
                buildTurns,
                fantasyNote,
                stabilityBonus,
                corruptionReduction,
                researchBonus,
                armyBonus);
        }

        private static bool TryGetBuildingTemplateDefinition(string? key, out BuildingTemplateDefinition definition)
        {
            definition = null!;
            if (string.IsNullOrWhiteSpace(key))
                return false;
            return BuildingTemplateByKey.TryGetValue(key.Trim(), out definition!);
        }

        private static int GetBuildingTemplateCount(bool militaryOnly = false)
        {
            return GetBuildingTemplateCatalogView(militaryOnly).Count;
        }

        private static int GetBuildingTemplatePageCount(bool militaryOnly = false)
        {
            return Math.Max(1, (int)Math.Ceiling(GetBuildingTemplateCount(militaryOnly) / (double)BuildingTemplatePageSize));
        }

        private static int NormalizeBuildingTemplatePage(int page, bool militaryOnly = false)
        {
            int totalPages = GetBuildingTemplatePageCount(militaryOnly);
            if (page < 0)
                return 0;
            if (page >= totalPages)
                return totalPages - 1;
            return page;
        }

        private static List<(string text, string data)> GetBuildingTemplatePageItems(int page, bool militaryOnly = false)
        {
            int safePage = NormalizeBuildingTemplatePage(page, militaryOnly);
            return GetBuildingTemplateCatalogView(militaryOnly)
                .Skip(safePage * BuildingTemplatePageSize)
                .Take(BuildingTemplatePageSize)
                .Select(t => (t.MenuTitle, $"{BuildingTemplatePrefix}{t.Key}"))
                .ToList();
        }

        private static int GetBuildingTemplatePageByKey(string key, bool militaryOnly = false)
        {
            int index = GetBuildingTemplateIndex(key, militaryOnly);
            if (index < 0)
                return 0;
            return index / BuildingTemplatePageSize;
        }

        private static int GetBuildingTemplateIndex(string key, bool militaryOnly = false)
        {
            if (string.IsNullOrWhiteSpace(key))
                return -1;

            string safeKey = key.Trim();
            var view = GetBuildingTemplateCatalogView(militaryOnly);
            for (int i = 0; i < view.Count; i++)
            {
                if (string.Equals(view[i].Key, safeKey, StringComparison.OrdinalIgnoreCase))
                    return i;
            }

            return -1;
        }

        private static string? GetAdjacentBuildingTemplateKey(string key, int offset, bool militaryOnly = false)
        {
            int index = GetBuildingTemplateIndex(key, militaryOnly);
            if (index < 0)
                return null;

            int targetIndex = index + offset;
            var view = GetBuildingTemplateCatalogView(militaryOnly);
            if (targetIndex < 0 || targetIndex >= view.Count)
                return null;

            return view[targetIndex].Key;
        }

        private static IReadOnlyList<BuildingTemplateDefinition> GetBuildingTemplateCatalogView(bool militaryOnly)
        {
            return militaryOnly
                ? MilitaryBuildingTemplateCatalog
                : CivilBuildingTemplateCatalog;
        }

        private static bool IsMilitaryBuildingTemplate(BuildingTemplateDefinition? definition)
        {
            return definition != null &&
                string.Equals(definition.Category, "Military", StringComparison.OrdinalIgnoreCase);
        }
    }
}
