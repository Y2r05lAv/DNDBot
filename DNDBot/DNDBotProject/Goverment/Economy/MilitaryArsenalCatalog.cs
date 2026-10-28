#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using DNDBot.DataBase;

namespace DNDBot.Goverment.Economy
{
    public sealed class MilitaryResourceDefinition
    {
        public string ResourceType { get; init; } = string.Empty;
        public string DisplayNameUk { get; init; } = string.Empty;
        public string Category { get; init; } = string.Empty;
        public decimal BasePrice { get; init; }
        public decimal CombatPowerWeight { get; init; }
        public float DemandFactor { get; init; } = 0.35f;
        public string? ParentResourceType { get; init; }
        public bool IsTradeable { get; init; } = true;
    }

    public static class MilitaryArsenalCatalog
    {
        private static readonly object Sync = new();

        private static readonly IReadOnlyList<MilitaryResourceDefinition> Definitions = new List<MilitaryResourceDefinition>
        {
            Def("Weapons", "Озброєння (збірне)", "Core", 3.2m, 1.6m),
            Def("Ammunition", "Боєприпаси (збірні)", "Core", 2.4m, 1.4m),
            Def("Armor", "Броня (збірна)", "Core", 3.6m, 1.8m),
            Def("Artillery", "Облогова техніка (збірна)", "Core", 5.1m, 2.6m),
            Def("Support", "Військова підтримка (збірна)", "Core", 2.8m, 1.3m),

            Def("Spear", "Списи", "Melee", 2.7m, 1.2m),
            Def("Pike", "Піки", "Melee", 3.0m, 1.25m),
            Def("Halberd", "Алебарди", "Melee", 3.8m, 1.45m),
            Def("WarHammer", "Бойові молоти", "Melee", 4.2m, 1.55m),
            Def("BattleAxe", "Бойові сокири", "Melee", 3.9m, 1.5m),
            Def("Longsword", "Довгі мечі", "Melee", 4.0m, 1.55m),
            Def("Greatsword", "Дворучні мечі", "Melee", 5.0m, 1.7m),
            Def("Rapier", "Рапіри", "Melee", 3.7m, 1.35m),
            Def("Mace", "Булави", "Melee", 3.4m, 1.3m),
            Def("Dagger", "Кинджали", "Melee", 1.6m, 0.8m),

            Def("Shortbow", "Короткі луки", "Ranged", 3.1m, 1.2m),
            Def("Longbow", "Довгі луки", "Ranged", 4.2m, 1.4m),
            Def("RecurveBow", "Рекурсивні луки", "Ranged", 4.6m, 1.5m),
            Def("Crossbow", "Арбалети", "Ranged", 4.8m, 1.45m),
            Def("HeavyCrossbow", "Важкі арбалети", "Ranged", 5.4m, 1.6m),
            Def("Arbalest", "Арбалести", "Ranged", 5.9m, 1.7m),
            Def("ThrowingJavelin", "Метальні списи", "Ranged", 2.5m, 1.0m),
            Def("WarSling", "Бойові пращі", "Ranged", 1.8m, 0.75m),
            Def("ArcaneRifle", "Арканічні карабіни", "Ranged", 8.2m, 2.1m),
            Def("HandCannon", "Ручні гармати", "Ranged", 9.8m, 2.35m),

            Def("ArrowBundle", "Зв'язки стріл", "Ammo", 1.2m, 0.55m),
            Def("BoltBundle", "Зв'язки болтів", "Ammo", 1.5m, 0.6m),
            Def("StoneShot", "Кам'яні снаряди", "Ammo", 1.0m, 0.45m),
            Def("BlackPowderCharge", "Порохові заряди", "Ammo", 2.8m, 0.95m),
            Def("ManaCartridge", "Мана-картриджі", "Ammo", 4.1m, 1.3m),
            Def("FireFlask", "Вогняні колби", "Ammo", 3.3m, 1.05m),
            Def("FrostFlask", "Крижані колби", "Ammo", 3.2m, 1.0m),

            Def("LeatherArmor", "Шкіряні обладунки", "Armor", 2.9m, 1.1m),
            Def("StuddedArmor", "Клепані обладунки", "Armor", 3.7m, 1.35m),
            Def("Chainmail", "Кольчуга", "Armor", 4.8m, 1.65m),
            Def("ScaleArmor", "Лускаті обладунки", "Armor", 5.2m, 1.75m),
            Def("Brigandine", "Бригантина", "Armor", 5.6m, 1.85m),
            Def("PlateArmor", "Залізні лати", "Armor", 7.4m, 2.25m),
            Def("MithrilArmor", "Мітрилові лати", "Armor", 11.5m, 2.9m),
            Def("TowerShield", "Ростові щити", "Armor", 4.6m, 1.55m),
            Def("KiteShield", "Краплевидні щити", "Armor", 3.8m, 1.3m),
            Def("RunicShield", "Рунічні щити", "Armor", 8.9m, 2.25m),
            Def("WarHelm", "Бойові шоломи", "Armor", 3.1m, 1.05m),
            Def("WarGauntlets", "Бойові рукавиці", "Armor", 2.5m, 0.95m),

            Def("SiegeBallista", "Облогові балісти", "Siege", 10.8m, 3.2m),
            Def("SiegeTrebuchet", "Облогові требушети", "Siege", 12.2m, 3.45m),
            Def("SiegeRam", "Тарани", "Siege", 8.7m, 2.6m),
            Def("SiegeTower", "Облогові вежі", "Siege", 13.6m, 3.6m),
            Def("RunicCannon", "Рунічні гармати", "Siege", 16.5m, 4.1m),

            Def("ManaCrystal", "Мана-кристали", "Arcane", 7.1m, 2.0m),
            Def("SpellScroll", "Бойові сувої", "Arcane", 8.6m, 2.25m),
            Def("RuneWard", "Рунні бар'єри", "Arcane", 9.4m, 2.45m),
            Def("AetherBattery", "Ефірні батареї", "Arcane", 10.2m, 2.6m),
            Def("GolemCore", "Ядра големів", "Arcane", 14.8m, 3.35m),
            Def("DroneFamiliar", "Дрон-фамільяри", "Arcane", 11.9m, 2.95m),

            Def("HealingDraught", "Лікувальні еліксири", "Support", 3.4m, 1.1m),
            Def("StaminaDraught", "Тоніки витривалості", "Support", 3.1m, 1.0m),
            Def("AlchemyBomb", "Алхімічні бомби", "Support", 5.7m, 1.85m),
            Def("SmokeBomb", "Димові бомби", "Support", 2.9m, 0.95m),
            Def("WyvernHarness", "Упряж для виверн", "Support", 9.7m, 2.55m),
            Def("FieldRation", "Польові пайки", "Support", 1.6m, 0.55m),
            Def("WarBandage", "Похідні перев'язки", "Support", 1.4m, 0.45m),
            Def("SignalFlare", "Сигнальні фаєри", "Support", 2.1m, 0.65m),
            Def("MageFocus", "Бойові фокуси мага", "Support", 6.2m, 1.95m),

            Def("Glaive", "Глефи", "Melee", 4.1m, 1.5m),
            Def("Flail", "Цепи", "Melee", 3.6m, 1.35m),
            Def("Morningstar", "Моргенштерни", "Melee", 4.3m, 1.6m),
            Def("WarPick", "Бойові кирки", "Melee", 3.8m, 1.4m),
            Def("Trident", "Тризубці", "Melee", 3.5m, 1.3m),
            Def("Saber", "Шаблі", "Melee", 3.9m, 1.45m),
            Def("Katana", "Катани", "Melee", 5.4m, 1.8m),
            Def("Estoc", "Естоки", "Melee", 4.8m, 1.6m),

            Def("CompositeBow", "Композитні луки", "Ranged", 4.7m, 1.55m),
            Def("HandCrossbow", "Ручні арбалети", "Ranged", 4.1m, 1.3m),
            Def("RepeatingCrossbow", "Повторні арбалети", "Ranged", 6.6m, 1.95m),
            Def("ThrowingAxe", "Метальні сокири", "Ranged", 2.9m, 1.1m),
            Def("Chakram", "Чакрами", "Ranged", 3.3m, 1.2m),
            Def("ArcanePistol", "Арканічні пістолі", "Ranged", 8.9m, 2.25m),
            Def("ThunderMusket", "Грозові мушкети", "Ranged", 10.7m, 2.5m),
            Def("RuneRepeater", "Рунні репітери", "Ranged", 12.1m, 2.85m),

            Def("ExplosiveBolt", "Вибухові болти", "Ammo", 3.7m, 1.15m),
            Def("ArcaneArrow", "Арканічні стріли", "Ammo", 3.4m, 1.05m),
            Def("SilverArrow", "Срібні стріли", "Ammo", 2.6m, 0.85m),
            Def("BlessedBolt", "Освячені болти", "Ammo", 3.0m, 0.95m),
            Def("ShockCharge", "Шок-заряди", "Ammo", 4.6m, 1.45m),
            Def("AcidFlask", "Кислотні колби", "Ammo", 3.9m, 1.2m),
            Def("Grapeshot", "Картеч", "Ammo", 2.8m, 0.9m),
            Def("SunfireCanister", "Каністри сонцежару", "Ammo", 5.2m, 1.65m),

            Def("Gambeson", "Гамбесони", "Armor", 2.2m, 0.85m),
            Def("HalfPlate", "Напівлати", "Armor", 6.4m, 2.05m),
            Def("DragonScaleArmor", "Драколускові лати", "Armor", 13.7m, 3.25m),
            Def("AdamantineArmor", "Адамантитові лати", "Armor", 16.4m, 3.55m),
            Def("SpellguardMantle", "Плащі чарозахисту", "Armor", 9.8m, 2.4m),
            Def("ArcaneRobe", "Бойові аркан-ризи", "Armor", 7.1m, 1.8m),
            Def("BastionShield", "Бастіонні щити", "Armor", 6.1m, 2.0m),
            Def("MithrilHelm", "Мітрилові шоломи", "Armor", 6.8m, 2.05m),

            Def("ArcaneMortar", "Арканні мортири", "Siege", 18.2m, 4.35m),
            Def("SiegeHowitzer", "Облогові гаубиці", "Siege", 19.5m, 4.55m),
            Def("FlameProjector", "Полум'яні проектори", "Siege", 14.4m, 3.75m),
            Def("StormBallista", "Штормові балісти", "Siege", 15.8m, 3.9m),
            Def("SkybreakerHarpoon", "Гарпуни неболома", "Siege", 17.1m, 4.0m),

            Def("LeylineCapacitor", "Лейлінні конденсатори", "Arcane", 12.7m, 3.05m),
            Def("VoidLens", "Порожнинні лінзи", "Arcane", 13.6m, 3.2m),
            Def("RiftBeacon", "Маяки розлому", "Arcane", 15.3m, 3.45m),
            Def("WardingTotem", "Тотеми захисту", "Arcane", 9.7m, 2.35m),
            Def("SpellBattery", "Закляттєві батареї", "Arcane", 11.4m, 2.8m),

            Def("EngineerKit", "Інженерні набори", "Support", 3.5m, 1.05m),
            Def("SiegeToolkit", "Облогові інструменти", "Support", 4.8m, 1.55m),
            Def("Antitoxin", "Антитоксини", "Support", 2.4m, 0.7m),
            Def("ManaMedkit", "Мана-медкити", "Support", 4.4m, 1.35m),
            Def("BattleStandard", "Бойові штандарти", "Support", 5.1m, 1.6m),
            Def("CommandSigil", "Командирські сигіли", "Support", 6.3m, 1.95m),
            Def("WarDrum", "Військові барабани", "Support", 4.9m, 1.5m),
            Def("CamouflageCloak", "Плащі маскування", "Support", 5.7m, 1.7m)
        };

        private static MilitaryResourceDefinition Def(
            string type,
            string displayNameUk,
            string category,
            decimal basePrice,
            decimal combatPower)
        {
            return new MilitaryResourceDefinition
            {
                ResourceType = type,
                DisplayNameUk = displayNameUk,
                Category = category,
                BasePrice = basePrice,
                CombatPowerWeight = combatPower,
                DemandFactor = 0.35f,
                IsTradeable = true
            };
        }

        public static IReadOnlyList<MilitaryResourceDefinition> All => Definitions;

        public static void EnsureResourcesInEconomyCatalog()
        {
            lock (Sync)
            {
                var existing = EconomyResourcesData.GetAll()
                    .Where(r => !string.IsNullOrWhiteSpace(r.ResourceType))
                    .ToDictionary(r => r.ResourceType, StringComparer.OrdinalIgnoreCase);

                foreach (var definition in Definitions)
                {
                    if (existing.ContainsKey(definition.ResourceType))
                        continue;

                    EconomyResourcesData.AddResource(new EconomyResourceRecord
                    {
                        ResourceType = definition.ResourceType,
                        BasePrice = definition.BasePrice,
                        DemandFactor = definition.DemandFactor,
                        IsTradeable = definition.IsTradeable,
                        ParentResourceType = definition.ParentResourceType,
                        IgnoreProductionEfficiency = false
                    });
                }
            }
        }

        public static bool IsMilitaryResourceType(string? resourceType)
        {
            return Find(resourceType) != null;
        }

        public static MilitaryResourceDefinition? Find(string? resourceType)
        {
            if (string.IsNullOrWhiteSpace(resourceType))
                return null;

            string key = resourceType.Trim();
            return Definitions.FirstOrDefault(d => d.ResourceType.Equals(key, StringComparison.OrdinalIgnoreCase));
        }

        public static decimal GetCombatPowerWeight(string? resourceType)
        {
            return Find(resourceType)?.CombatPowerWeight ?? 0m;
        }

        public static string GetCategory(string? resourceType)
        {
            return Find(resourceType)?.Category ?? string.Empty;
        }

        public static bool TryFormatResourceTypeUk(string? resourceType, out string displayName)
        {
            displayName = string.Empty;
            var definition = Find(resourceType);
            if (definition == null)
                return false;

            displayName = definition.DisplayNameUk;
            return true;
        }

        public static string FormatCategoryUk(string? category)
        {
            if (string.IsNullOrWhiteSpace(category))
                return "Інше";

            return category.Trim().ToLowerInvariant() switch
            {
                "core" => "Базове",
                "melee" => "Ближній бій",
                "ranged" => "Дальній бій",
                "ammo" => "Боєприпаси",
                "armor" => "Броня",
                "siege" => "Облога",
                "arcane" => "Аркана",
                "support" => "Підтримка",
                _ => category
            };
        }
    }
}
