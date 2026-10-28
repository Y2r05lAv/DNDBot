using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Cryptography;
using System.Threading.Tasks;
using DNDBot.DataBase;
using DNDBot.Goverment.Economy;
using Telegram.Bot.Types.ReplyMarkups;
using System.Globalization;

namespace DNDBot.Goverment
{
    public class StateKeyboardBuilder : BotBodyRealisation
    {
        private static string FormatResourceTypeUk(string resourceType)
        {
            if (string.IsNullOrWhiteSpace(resourceType))
                return "-";

            if (MilitaryArsenalCatalog.TryFormatResourceTypeUk(resourceType, out string militaryName))
                return militaryName;

            return resourceType.Trim().ToLowerInvariant() switch
            {
                "food" => "Їжа",
                "clothing" => "Одяг",
                "housing" => "Житло",
                "tools" => "Інструменти",
                "luxury" => "Розкіш",
                "goods" => "Товари",
                "materials" => "Матеріали",
                "material" => "Матеріали",
                _ => resourceType
            };
        }

        private static InlineKeyboardButton[] CreateRow(params (string text, string data)[] buttons)
        {
            return buttons.Select(b => InlineKeyboardButton.WithCallbackData(b.text, b.data)).ToArray();
        }

        private static InlineKeyboardMarkup BuildChoiceKeyboard(
            IReadOnlyList<(string text, string value)> options,
            string callbackPrefix,
            int columns = 2,
            bool includeCancel = true)
        {
            var rows = new List<InlineKeyboardButton[]>();
            if (options != null && options.Count > 0)
            {
                columns = Math.Max(1, columns);
                for (int i = 0; i < options.Count; i += columns)
                {
                    var slice = options.Skip(i).Take(columns)
                        .Select(o => InlineKeyboardButton.WithCallbackData(o.text, $"{callbackPrefix}{o.value}"))
                        .ToArray();
                    rows.Add(slice);
                }
            }

            if (includeCancel)
                rows.Add(CreateRow(("❌ Скасувати", "state_inlinepick_cancel")));

            return new InlineKeyboardMarkup(rows);
        }

        public static InlineKeyboardMarkup InlineBoolChoiceKeyboard(string callbackPrefix)
        {
            var options = new List<(string text, string value)>
            {
                ("✅ Так", "true"),
                ("❌ Ні", "false")
            };
            return BuildChoiceKeyboard(options, callbackPrefix, columns: 2);
        }

        public static InlineKeyboardMarkup InlineCompanyProductionTypeKeyboard(string callbackPrefix)
        {
            var options = new List<(string text, string value)>
            {
                ("🏛 Загальне", "General"),
                ("🛡 Військпром", "MilitaryIndustry"),
                ("✨ Аркан-зброя", "ArcaneArms"),
                ("🏰 Облогова", "SiegeWorks"),
                ("🔫 Озброєння", "Weapons"),
                ("🌾 Харчі", "Food"),
                ("🏭 Промисловість", "Industry"),
                ("🚚 Логістика", "Logistics"),
                ("🛎 Послуги", "Services")
            };
            return BuildChoiceKeyboard(options, callbackPrefix, columns: 2);
        }

        public static InlineKeyboardMarkup InlineMilitaryUnitLevelKeyboard(string callbackPrefix)
        {
            var options = new List<(string text, string value)>
            {
                ("🏰 Гарнізон", "Garrison"),
                ("🧍 Відділення", "Squad"),
                ("🪖 Взвод", "Platoon"),
                ("🏴 Рота", "Company"),
                ("🧱 Батальйон", "Battalion"),
                ("⚔ Полк", "Regiment"),
                ("🛡 Бригада", "Brigade"),
                ("🏯 Дивізія", "Division"),
                ("🦅 Легіон", "Legion"),
                ("⛵ Флот", "Fleet")
            };
            return BuildChoiceKeyboard(options, callbackPrefix, columns: 2);
        }

        public static InlineKeyboardMarkup InlineMilitaryUnitTypeKeyboard(string callbackPrefix)
        {
            var options = new List<(string text, string value)>
            {
                ("🪖 Піхота", "Infantry"),
                ("🛡 Копійники", "Spearmen"),
                ("🏹 Лучники", "Archers"),
                ("🎯 Арбалетники", "Crossbowmen"),
                ("🐎 Кавалерія", "Cavalry"),
                ("🧱 Важка піхота", "HeavyInfantry"),
                ("✨ Маги", "MageCorps"),
                ("🏰 Облогові", "Siege"),
                ("👁 Розвідники", "Scouts"),
                ("⛵ Флот", "NavalFleet"),
                ("🧰 Підтримка", "Support")
            };
            return BuildChoiceKeyboard(options, callbackPrefix, columns: 2);
        }

        public static InlineKeyboardMarkup InlineMilitaryUnitRoleKeyboard(string callbackPrefix)
        {
            var options = new List<(string text, string value)>
            {
                ("⚔ Польова", "Field"),
                ("🏰 Гарнізон", "Garrison"),
                ("🦅 Авангард", "Vanguard"),
                ("🗄 Резерв", "Reserve"),
                ("🏹 Облогова", "SiegeTrain"),
                ("⛵ Флотська", "Fleet")
            };
            return BuildChoiceKeyboard(options, callbackPrefix, columns: 2);
        }

        public static InlineKeyboardMarkup InlineMilitaryEquipmentTierKeyboard(string callbackPrefix)
        {
            var options = new List<(string text, string value)>
            {
                ("🪓 Ополчення", "Militia"),
                ("🛡 Стандарт", "Standard"),
                ("⚙ Важка", "Heavy"),
                ("👑 Елітна", "Elite"),
                ("✨ Арканна", "Arcane"),
                ("⛵ Морська", "Naval")
            };
            return BuildChoiceKeyboard(options, callbackPrefix, columns: 2);
        }

        public static InlineKeyboardMarkup InlineMilitaryUnitRaceKeyboard(string callbackPrefix)
        {
            var options = new List<(string text, string value)>
            {
                ("🧑 Люди", "Human"),
                ("🧝 Ельфи", "Elf"),
                ("😈 Тіфлінги", "Tiefling"),
                ("🍄 Гриби", "Mushroomfolk"),
                ("⛰ Дворфи", "Dwarf"),
                ("🪓 Орки", "Orc"),
                ("🧙 Гноми", "Gnome"),
                ("🦶 Халфлінги", "Halfling")
            };
            return BuildChoiceKeyboard(options, callbackPrefix, columns: 2);
        }

        public static InlineKeyboardMarkup InlineMilitaryUnitStatusKeyboard(string callbackPrefix)
        {
            var options = new List<(string text, string value)>
            {
                ("⏳ Формується", "Forming"),
                ("✅ Активна", "Active"),
                ("🗄 Резерв", "Reserve")
            };
            return BuildChoiceKeyboard(options, callbackPrefix, columns: 2);
        }

        public static InlineKeyboardMarkup InlineBuildingCategoryKeyboard(string callbackPrefix)
        {
            var options = new List<(string text, string value)>
            {
                ("🏠 Житло", "Housing"),
                ("🏭 Виробництво", "Production"),
                ("🏛 Адміністрація", "Administration"),
                ("🧠 Наука", "Science"),
                ("🛡 Військова", "Military"),
                ("🎭 Культура", "Culture"),
                ("📦 Інше", "Other")
            };
            return BuildChoiceKeyboard(options, callbackPrefix, columns: 2);
        }

        public static InlineKeyboardMarkup MainStateKeyboard(long userId, string countryName = null, bool? hasParliamentOverride = null, bool? isMasterOverride = null, bool? isCountryOwnerOverride = null)
        {
            var state = new StateDataProvider().GetStateData(userId);
            bool hasParliament = hasParliamentOverride ?? (state?.HasParliament ?? false);
            bool isMaster = isMasterOverride ?? (userId == masterID);
            bool isCountryOwner = isCountryOwnerOverride ?? false;
            bool canAccessFull = isMaster || isCountryOwner;

            var rows = new List<InlineKeyboardButton[]>
            {
                CreateRow((BuildCountryButtonText(countryName), "state_selectcountry"), ("ℹ Загальна інфа", "state_overview")),
            };

            if (canAccessFull)
            {
                rows.Add(
                    hasParliament
                        ? CreateRow(("🏛 Парламент", "state_parliament"), ("🤝 Дипломатія", "state_diplomacy"))
                        : CreateRow(("🤝 Дипломатія", "state_diplomacy"))
                );
                rows.Add(CreateRow(("🧠 Дослідження", "state_research"), ("💰 Економіка", "state_economy")));
                rows.Add(CreateRow(("🛡 Військо", "state_military"), ("⏳ Час", "state_time")));
                rows.Add(CreateRow(("🏥 МОЗ", "state_admin_health"), ("🎓 МОН", "state_admin_education")));
                rows.Add(CreateRow(("🌪 Ефекти країни", "state_country_effects")));
                rows.Add(
                    isMaster
                        ? CreateRow(("⚙ Державне управління", "state_admin"), ("🔧 Адмін. панель", "state_masterpanel"))
                        : CreateRow(("⚙ Державне управління", "state_admin"))
                );
            }


            return new InlineKeyboardMarkup(rows);
        }

        public static InlineKeyboardMarkup TimeMenuKeyboard(bool isMaster = false)
        {
            var rows = new List<InlineKeyboardButton[]>
            {
                CreateRow(("1 день", "state_time_days_1"), ("3 дні", "state_time_days_3")),
                CreateRow(("Неділя (7 днів)", "state_time_days_7"), ("Місяць (31 день)", "state_time_days_31")),
                CreateRow(("Рік (365 днів)", "state_time_days_365")),
                CreateRow(("👶 Цикл населення", "state_time_demography")),
            };

            if (isMaster)
                rows.Add(CreateRow(("🤖 ШІ-країни", "state_time_ai")));

            rows.Add(CreateRow(("⬅ Назад", "state_main")));

            return new InlineKeyboardMarkup(rows);
        }

        public static InlineKeyboardMarkup TimeAutomationCountriesKeyboard(
            IReadOnlyList<(int CountryId, string Name, bool Enabled)> countries,
            int page,
            int totalPages)
        {
            var rows = new List<InlineKeyboardButton[]>();
            if (countries != null)
            {
                foreach (var country in countries)
                {
                    string mark = country.Enabled ? "✅" : "⬜";
                    string label = string.IsNullOrWhiteSpace(country.Name)
                        ? $"{mark} #{country.CountryId}"
                        : $"{mark} {country.Name}";
                    rows.Add(CreateRow((label, $"state_time_ai_toggle_{country.CountryId}")));
                }
            }

            var nav = new List<(string text, string data)>();
            if (page > 0)
                nav.Add(("⬅", $"state_time_ai_page_{page - 1}"));
            if (page < totalPages - 1)
                nav.Add(("➡", $"state_time_ai_page_{page + 1}"));
            if (nav.Count > 0)
                rows.Add(CreateRow(nav.ToArray()));

            rows.Add(CreateRow(("🔄 Оновити", $"state_time_ai_page_{page}")));
            rows.Add(CreateRow(("⬅ Назад", "state_time")));

            return new InlineKeyboardMarkup(rows);
        }

        public static InlineKeyboardMarkup PublicOverviewKeyboard(string countryName = null)
        {
            var rows = new List<InlineKeyboardButton[]>
            {
                CreateRow((BuildCountryButtonText(countryName), "state_selectcountry")),
                CreateRow(("⬅ Назад", "state_main"))
            };

            return new InlineKeyboardMarkup(rows);
        }

        public static InlineKeyboardMarkup CountrySelectKeyboard(List<CountryRecord> countries)
        {
            var rows = new List<InlineKeyboardButton[]>();

            if (countries != null)
            {
                foreach (var c in countries)
                {
                    string flag = string.IsNullOrWhiteSpace(c.FlagEmoji) ? "" : $"{c.FlagEmoji} ";
                    string name = string.IsNullOrWhiteSpace(c.Name) ? $"#{c.CountryId}" : c.Name;
                    rows.Add(CreateRow(($"{flag}{name}", $"state_selectcountry_{c.CountryId}")));
                }
            }

            rows.Add(CreateRow(("❌ Скасувати", "state_cancel")));

            return new InlineKeyboardMarkup(rows);
        }

        public static InlineKeyboardMarkup CollectPairlamentQuorumKeyboard()
        {
            return new InlineKeyboardMarkup(new[]
            {
                CreateRow(("✅ Призначити кворум", "stateparliament_joinquorum")),
                CreateRow(("❌ Скасувати засідання", "stateparliament_leavequorum"))
            });
        }

        private static string BuildCountryButtonText(string countryName)
        {
            if (string.IsNullOrWhiteSpace(countryName))
                return "🌍 Країна";

            return $"🌍 Країна: {countryName}";
        }

        private static string EncodeResourceKey(string resourceType)
        {
            if (string.IsNullOrWhiteSpace(resourceType))
                return string.Empty;

            string normalized = resourceType.Trim().ToLowerInvariant();
            byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(normalized));
            return Convert.ToHexString(hash.AsSpan(0, 5)).ToLowerInvariant();
        }

        public static InlineKeyboardMarkup AddOrApplyCountryKeyboard()
        {
            var rows = new List<InlineKeyboardButton[]>
            {
                CreateRow(("➕ Додати країну", "adminstate_addcountry"), ("⚡ Швидко створити", "adminstate_quickcountry")),
                CreateRow(("🗑 Видалити країну", "adminstate_deleteone"), ("🧹 Видалити всі", "adminstate_deleteall")),
                CreateRow(("🔀 Об'єднати країни", "adminstate_merge"), ("👤 Призначити країну", "adminstate_assign")),
                CreateRow(("📄 Список країн", "adminstate_listcountries"), ("❌ Скасувати", "stateparliament_cancel"))
            };


            return new InlineKeyboardMarkup(rows);
        }

        public static InlineKeyboardMarkup QuickCountryPresetKeyboard()
        {
            var rows = new List<InlineKeyboardButton[]>
            {
                CreateRow(("🪙 Скромний старт", "adminstate_quickcountry_profile_poor")),
                CreateRow(("⚖ Збалансований старт", "adminstate_quickcountry_profile_standard")),
                CreateRow(("💹 Економічний старт", "adminstate_quickcountry_profile_economy")),
                CreateRow(("👑 Заможний старт", "adminstate_quickcountry_profile_prosperous")),
                CreateRow(("🔙 Назад", "adminstate_quickcountry_back"))
            };

            return new InlineKeyboardMarkup(rows);
        }

        public static InlineKeyboardMarkup AdminDeleteCountryKeyboard(List<CountryRecord> countries)
        {
            var rows = new List<InlineKeyboardButton[]>();

            if (countries != null)
            {
                foreach (var c in countries)
                {
                    string flag = string.IsNullOrWhiteSpace(c.FlagEmoji) ? "" : $"{c.FlagEmoji} ";
                    string name = string.IsNullOrWhiteSpace(c.Name) ? $"#{c.CountryId}" : c.Name;
                    rows.Add(CreateRow(($"🗑 {flag}{name}", $"adminstate_deletecountry_{c.CountryId}")));
                }
            }

            rows.Add(CreateRow(("⬅ Назад", "adminstate_cancel")));

            return new InlineKeyboardMarkup(rows);
        }

        public static InlineKeyboardMarkup AdminMergeTargetKeyboard(List<CountryRecord> countries)
        {
            var rows = new List<InlineKeyboardButton[]>();

            if (countries != null)
            {
                foreach (var c in countries)
                {
                    string flag = string.IsNullOrWhiteSpace(c.FlagEmoji) ? "" : $"{c.FlagEmoji} ";
                    string name = string.IsNullOrWhiteSpace(c.Name) ? $"#{c.CountryId}" : c.Name;
                    rows.Add(CreateRow(($"🎯 {flag}{name}", $"adminstate_merge_target_{c.CountryId}")));
                }
            }

            rows.Add(CreateRow(("❌ Скасувати", "adminstate_cancel")));
            return new InlineKeyboardMarkup(rows);
        }

        public static InlineKeyboardMarkup AdminMergeSourceKeyboard(List<CountryRecord> countries, int targetCountryId)
        {
            var rows = new List<InlineKeyboardButton[]>();

            if (countries != null)
            {
                foreach (var c in countries)
                {
                    if (c.CountryId == targetCountryId)
                        continue;

                    string flag = string.IsNullOrWhiteSpace(c.FlagEmoji) ? "" : $"{c.FlagEmoji} ";
                    string name = string.IsNullOrWhiteSpace(c.Name) ? $"#{c.CountryId}" : c.Name;
                    rows.Add(CreateRow(($"➡ {flag}{name}", $"adminstate_merge_source_{c.CountryId}")));
                }
            }

            rows.Add(CreateRow(("⬅ Назад", "adminstate_merge_back_target"), ("❌ Скасувати", "adminstate_cancel")));
            return new InlineKeyboardMarkup(rows);
        }

        public static InlineKeyboardMarkup AdminMergeConfirmKeyboard(int sourceCountryId)
        {
            return new InlineKeyboardMarkup(new[]
            {
                CreateRow(("✅ Об'єднати", $"adminstate_merge_confirm_{sourceCountryId}"), ("⬅ Назад", "adminstate_merge_back_source")),
                CreateRow(("❌ Скасувати", "adminstate_cancel"))
            });
        }

        public static InlineKeyboardMarkup AdminBackKeyboard()
        {
            return new InlineKeyboardMarkup(new[]
            {
                CreateRow(("⬅ Назад", "adminstate_cancel"))
            });
        }

        public static InlineKeyboardMarkup EmptyInlineKeyboard()
        {
            return new InlineKeyboardMarkup(Array.Empty<InlineKeyboardButton[]>());
        }

        public static InlineKeyboardMarkup AdminDeleteAllConfirmKeyboard(int step = 1)
        {
            string confirmText = step switch
            {
                2 => "✅ Підтвердити ще раз",
                3 => "🔥 Остаточно видалити",
                _ => "✅ Підтвердити"
            };
            string confirmData = $"adminstate_deleteall_confirm_{step}";
            string backData = $"adminstate_deleteall_back_{step}";
            return new InlineKeyboardMarkup(new[]
            {
                CreateRow((confirmText, confirmData), ("⬅ Назад", backData)),
                CreateRow(("❌ Скасувати", "adminstate_cancel"))
            });
        }

        public static InlineKeyboardMarkup AdminDeleteCountryConfirmKeyboard(int countryId, int step = 1)
        {
            string confirmText = step switch
            {
                2 => "✅ Підтвердити ще раз",
                3 => "🔥 Остаточно видалити",
                _ => "✅ Підтвердити"
            };
            string confirmData = $"adminstate_deletecountry_confirm_{countryId}_{step}";
            string backData = $"adminstate_deletecountry_back_{countryId}_{step}";
            return new InlineKeyboardMarkup(new[]
            {
                CreateRow((confirmText, confirmData), ("⬅ Назад", backData)),
                CreateRow(("❌ Скасувати", "adminstate_cancel"))
            });
        }

        public static InlineKeyboardMarkup AdminAssignCharacterKeyboard(List<(int CharacterId, string Title)> characters)
        {
            var rows = new List<InlineKeyboardButton[]>();

            if (characters != null)
            {
                foreach (var c in characters)
                {
                    rows.Add(CreateRow(($"👤 {c.Title}", $"adminstate_assignchar_{c.CharacterId}")));
                }
            }

            rows.Add(CreateRow(("❌ Скасувати", "adminstate_cancel")));

            return new InlineKeyboardMarkup(rows);
        }

        public static InlineKeyboardMarkup AdminAssignCountryKeyboard(List<CountryRecord> countries)
        {
            var rows = new List<InlineKeyboardButton[]>();

            if (countries != null)
            {
                foreach (var c in countries)
                {
                    string flag = string.IsNullOrWhiteSpace(c.FlagEmoji) ? "" : $"{c.FlagEmoji} ";
                    string name = string.IsNullOrWhiteSpace(c.Name) ? $"#{c.CountryId}" : c.Name;
                    rows.Add(CreateRow(($"{flag}{name}", $"adminstate_assigncountry_{c.CountryId}")));
                }
            }

            rows.Add(CreateRow(("❌ Скасувати", "adminstate_cancel")));

            return new InlineKeyboardMarkup(rows);
        }

        public static InlineKeyboardMarkup ResearchMenuKeyboard(bool isMaster)
        {
            var rows = new List<InlineKeyboardButton[]>
            {
                CreateRow(("📚 Доступні дослідження", "state_research_list"), ("✅ Активні дослідження", "state_research_active"))
            };
            if (isMaster)
                rows.Add(CreateRow(("➕ Створити дослідження", "state_research_create")));
            rows.Add(CreateRow(("⬅ Назад", "state_main")));
            return new InlineKeyboardMarkup(rows);
        }

        public static InlineKeyboardMarkup ResearchListKeyboard(List<(string text, string data)> items, int page, int totalPages)
        {
            var rows = new List<InlineKeyboardButton[]>();

            foreach (var item in items)
            {
                rows.Add(CreateRow((item.text, item.data)));
            }

            if (totalPages > 1)
            {
                var nav = new List<(string text, string data)>();
                if (page > 0)
                    nav.Add(("⬅ Попередні", $"state_research_list_page_{page - 1}"));
                if (page < totalPages - 1)
                    nav.Add(("➡ Наступні", $"state_research_list_page_{page + 1}"));
                if (nav.Count > 0)
                    rows.Add(CreateRow(nav.ToArray()));
            }

            rows.Add(CreateRow(("🧠 Дослідження", "state_research")));
            return new InlineKeyboardMarkup(rows);
        }

        public static InlineKeyboardMarkup ResearchBranchesKeyboard(List<string> branches)
        {
            var rows = new List<InlineKeyboardButton[]>();
            var icons = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "Адміністрація", "🏛" },
                { "Військова", "🛡" },
                { "Наука", "🧪" },
                { "Економіка", "💰" },
                { "Право", "⚖️" },
                { "Дипломатія", "🤝" },
                { "Магія", "✨" },
                { "Загальне", "📚" }
            };
            foreach (var b in branches)
            {
                string icon = icons.TryGetValue(b, out var ic) ? ic : "📁";
                rows.Add(CreateRow(($"{icon} {b}", $"state_research_branchpick_{EncodeKey(b)}")));
            }
            rows.Add(CreateRow(("🧠 Дослідження", "state_research")));
            return new InlineKeyboardMarkup(rows);
        }

        public static InlineKeyboardMarkup ResearchBranchListKeyboard(List<(string text, string data)> items, int page, int totalPages)
        {
            var rows = new List<InlineKeyboardButton[]>();
            foreach (var item in items)
                rows.Add(CreateRow((item.text, item.data)));

            if (totalPages > 1)
            {
                var nav = new List<(string text, string data)>();
                if (page > 0)
                    nav.Add(("⬅ Попередні", $"state_research_branchlist_page_{page - 1}"));
                if (page < totalPages - 1)
                    nav.Add(("➡ Наступні", $"state_research_branchlist_page_{page + 1}"));
                if (nav.Count > 0)
                    rows.Add(CreateRow(nav.ToArray()));
            }

            rows.Add(CreateRow(("🔙 Назад", "state_research_list")));
            return new InlineKeyboardMarkup(rows);
        }

        public static InlineKeyboardMarkup ResearchInfoKeyboard(int researchId, bool canStart)
        {
            var rows = new List<InlineKeyboardButton[]>();
            if (canStart)
                rows.Add(CreateRow(("▶ Почати дослідження", $"state_research_start_{researchId}")));
            rows.Add(CreateRow(("🔙 Назад", "state_research_list")));
            return new InlineKeyboardMarkup(rows);
        }

        public static InlineKeyboardMarkup ResearchActiveKeyboard(bool canManualComplete)
        {
            var rows = new List<InlineKeyboardButton[]>();
            if (canManualComplete)
                rows.Add(CreateRow(("✅ Завершити вручну", "state_research_complete_now")));
            rows.Add(CreateRow(("🧠 Дослідження", "state_research")));
            return new InlineKeyboardMarkup(rows);
        }

        public static InlineKeyboardMarkup ResearchYesNoKeyboard(string yesData, string noData)
        {
            return new InlineKeyboardMarkup(new[]
            {
                CreateRow(("✅ Так", yesData), ("❌ Ні", noData))
            });
        }

        public static InlineKeyboardMarkup ResearchBranchSelectKeyboard(List<string> branches)
        {
            var rows = new List<InlineKeyboardButton[]>();
            foreach (var b in branches)
                rows.Add(CreateRow((b, $"state_research_branch_pick_{EncodeKey(b)}")));
            rows.Add(CreateRow(("🔙 Назад", "state_research_create_back")));
            return new InlineKeyboardMarkup(rows);
        }

        public static InlineKeyboardMarkup ResearchEffectTypeKeyboard()
        {
            return new InlineKeyboardMarkup(new[]
            {
                CreateRow(("Відкрити закон", "state_research_effecttype_UnlockLaw"), ("Відкрити будівлю", "state_research_effecttype_UnlockBuilding")),
                CreateRow(("Бонус", "state_research_effecttype_Bonus"), ("Відкрити можливість", "state_research_effecttype_UnlockFeature")),
                CreateRow(("Увімкнути парламент", "state_research_effecttype_EnableParliament"), ("Збільшити місця парламенту", "state_research_effecttype_IncreaseParliamentSeats")),
                CreateRow(("Зменшити корупцію", "state_research_effecttype_ReduceCorruption"), ("Збільшити швидкість досліджень", "state_research_effecttype_IncreaseResearchSpeed")),
                CreateRow(("Армія", "state_research_effecttype_Army"), ("Економіка", "state_research_effecttype_Economy")),
                CreateRow(("Магія", "state_research_effecttype_Magic")),
                CreateRow(("🔙 Назад", "state_research_create_back"))
            });
        }

        public static InlineKeyboardMarkup ResearchRequirementsBranchKeyboard(List<string> branches)
        {
            var rows = new List<InlineKeyboardButton[]>();
            foreach (var b in branches)
                rows.Add(CreateRow((b, $"state_research_req_branch_{EncodeKey(b)}")));
            rows.Add(CreateRow(("✅ Готово", "state_research_req_done"), ("🧹 Очистити", "state_research_req_clear")));
            rows.Add(CreateRow(("🔙 Назад", "state_research_create_back")));
            return new InlineKeyboardMarkup(rows);
        }

        public static InlineKeyboardMarkup ResearchRequirementsListKeyboard(List<(string text, string data)> items, int page, int totalPages, string branchKey)
        {
            var rows = new List<InlineKeyboardButton[]>();
            foreach (var it in items)
                rows.Add(CreateRow((it.text, it.data)));

            if (totalPages > 1)
            {
                var nav = new List<(string text, string data)>();
                if (page > 0)
                    nav.Add(("⬅ Попередні", $"state_research_req_page_{branchKey}_{page - 1}"));
                if (page < totalPages - 1)
                    nav.Add(("➡ Наступні", $"state_research_req_page_{branchKey}_{page + 1}"));
                if (nav.Count > 0)
                    rows.Add(CreateRow(nav.ToArray()));
            }

            rows.Add(CreateRow(("🔙 Назад", "state_research_req_back")));
            return new InlineKeyboardMarkup(rows);
        }

        private static string EncodeKey(string value)
        {
            var bytes = Encoding.UTF8.GetBytes(value ?? string.Empty);
            return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        }

        public static InlineKeyboardMarkup EconomyMenuKeyboard(bool isMaster)
        {
            var rows = new List<InlineKeyboardButton[]>();
            rows.Add(CreateRow(("⚠ Попередження", "state_economy_warnings"), ("🔎 Прев'ю", "state_economy_preview")));

            rows.Add(CreateRow(("📊 Звіт", "state_economy_report"), ("📦 Звіт ресурсів", "state_economy_resources_report")));
            rows.Add(CreateRow(("🏗 Будівлі", "state_economy_buildings"), ("🎭 Фестивалі", "state_economy_festivals")));
            rows.Add(CreateRow(("🤝 Торгівля", "state_economy_trade"), ("📦 Ресурси", "state_economy_resources")));
            rows.Add(CreateRow(("🏭 Компанії", "state_economy_companies"), ("👷 Ринок праці", "state_economy_labor")));
            rows.Add(CreateRow(("🧯 Антикризові дії", "state_economy_crisis_actions"), ("🎯 Ліміт доходу", "state_economy_income_limit")));
            rows.Add(CreateRow(("➕ Внести в казну", "state_economy_treasury_add"), ("➖ Зняти з казни", "state_economy_treasury_remove")));
            rows.Add(CreateRow(("🧾 Журнал грошей", "state_economy_moneylog"), ("🛡 Протекціонізм", "state_economy_sanctions")));
            rows.Add(CreateRow(("💱 Валюти", "state_economy_currency")));
            if (isMaster)
                rows.Add(CreateRow(("🧠 Майстер: Баланс", "state_economy_master_balance")));

            rows.Add(CreateRow(("⬅ Назад", "state_main")));
            return new InlineKeyboardMarkup(rows);
        }

        public static InlineKeyboardMarkup EconomyMasterBalancingKeyboard()
        {
            return new InlineKeyboardMarkup(new[]
            {
                CreateRow(("⚖ Балансування виробн.", "state_economy_rebalance"), ("💎 Розкішне балансування", "state_economy_rebalance_luxury")),
                CreateRow(("🏁 Провідний ресурс", "state_economy_leader")),
                CreateRow(("🔙 До економіки", "state_economy"))
            });
        }

        public static InlineKeyboardMarkup PublicSystemsMenuKeyboard(bool canControl)
        {
            var rows = new List<InlineKeyboardButton[]>
            {
                CreateRow(("📊 Показники", "state_public_report"), ("💸 Бюджети", "state_public_budget")),
                CreateRow(("🦠 Хвороби", "state_public_diseases"), ("🌍 Міжнародні програми", "state_public_intl"))
            };

            if (canControl)
                rows.Add(CreateRow(("🛠 Дії під час хвороб", "state_public_actions")));

            rows.Add(CreateRow(("⬅ До економіки", "state_economy")));
            return new InlineKeyboardMarkup(rows);
        }

        public static InlineKeyboardMarkup PublicSystemsBudgetKeyboard(bool intlAuto, bool canControl)
        {
            string intlModeLabel = intlAuto ? "🤖 Міжнар.: авто" : "✋ Міжнар.: вручну";
            var rows = new List<InlineKeyboardButton[]>
            {
                CreateRow(("🏥 Бюджет МОЗ", "state_public_budget_health"), ("🎓 Бюджет МОН", "state_public_budget_education")),
                CreateRow(("🌍 Бюджет міжнародних програм", "state_public_budget_intl"), (intlModeLabel, "state_public_budget_intl_mode"))
            };

            rows.Add(CreateRow(("🔙 До модуля", "state_public")));

            if (!canControl)
                rows.Insert(0, CreateRow(("ℹ Лише перегляд", "state_noop")));

            return new InlineKeyboardMarkup(rows);
        }

        public static InlineKeyboardMarkup PublicSystemsActionsKeyboard()
        {
            return new InlineKeyboardMarkup(new[]
            {
                CreateRow(("🚧 Карантин увімк./вимк.", "state_public_action_quarantine_toggle"), ("🩺 Мобілізація медиків", "state_public_action_medic_mobilize")),
                CreateRow(("📣 Кампанія довіри", "state_public_action_trust_campaign"), ("💉 Вакцинація", "state_public_action_vaccination")),
                CreateRow(("🧪 Епідмоніторинг стоків", "state_public_action_wastewater"), ("📱 Телемедицина й тріаж", "state_public_action_telemed")),
                CreateRow(("🧬 R&D ліків", "state_public_action_drug_research"), ("👩‍⚕️ Резиденції лікарів", "state_public_action_doctor_training")),
                CreateRow(("🧪 Клінічні випробування", "state_public_action_clinical_trials")),
                CreateRow(("🔙 До модуля", "state_public"))
            });
        }

        public static InlineKeyboardMarkup PublicSystemsDiseasesKeyboard(List<(string text, string data)> diseaseItems, bool canControl)
        {
            var rows = new List<InlineKeyboardButton[]>();
            if (diseaseItems != null)
            {
                foreach (var item in diseaseItems)
                    rows.Add(CreateRow((item.text, item.data)));
            }

            if (rows.Count == 0)
                rows.Add(CreateRow(("Немає активних хвороб", "state_noop")));

            if (canControl)
                rows.Add(CreateRow(("🎲 Створити (ГМ)", "state_public_gm_spawn_help"), ("🛑 Завершити всі (ГМ)", "state_public_gm_end_all")));

            rows.Add(CreateRow(("🔙 До модуля", "state_public")));
            return new InlineKeyboardMarkup(rows);
        }

        public static InlineKeyboardMarkup PublicSystemsIntlKeyboard(List<(string text, string data)> programItems, bool canControl)
        {
            var rows = new List<InlineKeyboardButton[]>();
            if (programItems != null)
            {
                foreach (var item in programItems)
                    rows.Add(CreateRow((item.text, item.data)));
            }

            if (rows.Count == 0)
                rows.Add(CreateRow(("Немає програм", "state_noop")));

            if (!canControl)
                rows.Insert(0, CreateRow(("ℹ Лише перегляд", "state_noop")));

            rows.Add(CreateRow(("🔙 До модуля", "state_public")));
            return new InlineKeyboardMarkup(rows);
        }

        public static InlineKeyboardMarkup EconomyProductionBalanceConfirmKeyboard()
        {
            return new InlineKeyboardMarkup(new[]
            {
                CreateRow(("✅ Підтвердити", "state_economy_rebalance_confirm_yes"), ("❌ Скасувати", "state_economy_rebalance_confirm_no")),
                CreateRow(("🔙 До економіки", "state_economy"))
            });
        }

        public static InlineKeyboardMarkup EconomyLuxuryProductionBalanceConfirmKeyboard()
        {
            return new InlineKeyboardMarkup(new[]
            {
                CreateRow(("✅ Підтвердити", "state_economy_rebalance_luxury_confirm_yes"), ("❌ Скасувати", "state_economy_rebalance_luxury_confirm_no")),
                CreateRow(("🔙 До економіки", "state_economy"))
            });
        }

        public static InlineKeyboardMarkup EconomyLeaderResourceKeyboard(string currentResource)
        {
            string current = string.IsNullOrWhiteSpace(currentResource) ? string.Empty : currentResource.Trim();

            string Mark(string resource)
            {
                return string.Equals(current, resource, StringComparison.OrdinalIgnoreCase)
                    ? $"⭐ {FormatResourceTypeUk(resource)}"
                    : FormatResourceTypeUk(resource);
            }

            return new InlineKeyboardMarkup(new[]
            {
                CreateRow((Mark("Food"), "state_economy_leader_set_food"), (Mark("Clothing"), "state_economy_leader_set_clothing")),
                CreateRow((Mark("Housing"), "state_economy_leader_set_housing"), (Mark("Tools"), "state_economy_leader_set_tools")),
                CreateRow((Mark("Luxury"), "state_economy_leader_set_luxury"), ("❎ Без лідера", "state_economy_leader_set_none")),
                CreateRow(("⚖ Балансування виробн.", "state_economy_rebalance"), ("🔙 До майстер-групи", "state_economy_master_balance"))
            });
        }

        public static InlineKeyboardMarkup EconomyIncomeLimitKeyboard(bool enabled, decimal currentTargetDaily)
        {
            string Mark(decimal value)
            {
                if (!enabled)
                    return $"{value:N0} / день";

                decimal diff = Math.Abs(currentTargetDaily - value);
                return diff <= 0.5m ? $"✅ {value:N0} / день" : $"{value:N0} / день";
            }

            return new InlineKeyboardMarkup(new[]
            {
                CreateRow((Mark(1000m), "state_economy_income_limit_set_1000"), (Mark(5000m), "state_economy_income_limit_set_5000")),
                CreateRow((Mark(10000m), "state_economy_income_limit_set_10000"), (Mark(25000m), "state_economy_income_limit_set_25000")),
                CreateRow(("✍ Свій ліміт", "state_economy_income_limit_custom"), ("❎ Вимкнути", "state_economy_income_limit_disable")),
                CreateRow(("🔙 До економіки", "state_economy"))
            });
        }

        public static InlineKeyboardMarkup AdminMenuKeyboard(bool isMaster)
        {
            var rows = new List<InlineKeyboardButton[]>();
            if (isMaster)
            {
                rows.Add(CreateRow(("✏ Назва країни", "state_admin_countryname")));
            }

            rows.Add(CreateRow(("💰 Економіка", "state_admin_economy")));
            rows.Add(CreateRow(("👥 Соціальна політика", "state_admin_social")));
            rows.Add(CreateRow(("📦 Ресурси", "state_admin_resources")));
            rows.Add(CreateRow(("📊 Аналітика", "state_admin_analytics")));
            rows.Add(CreateRow(("⬅ Назад", "state_main")));

            return new InlineKeyboardMarkup(rows);
        }

        public static InlineKeyboardMarkup AdminHealthMenuKeyboard(bool canControl)
        {
            var rows = new List<InlineKeyboardButton[]>
            {
                CreateRow(("📊 Показники МОЗ", "state_admin_health_report"), ("💸 Бюджет МОЗ", "state_admin_health_budget")),
                CreateRow(("🦠 Хвороби", "state_admin_health_diseases"), ("🌍 Міжнар. (МОЗ)", "state_admin_health_intl"))
            };

            if (canControl)
                rows.Add(CreateRow(("🛠 Дії МОЗ", "state_admin_health_actions")));

            rows.Add(CreateRow(("⬅ До головного меню", "state_main")));
            return new InlineKeyboardMarkup(rows);
        }

        public static InlineKeyboardMarkup AdminHealthBudgetKeyboard(bool intlAuto, bool canControl)
        {
            string intlModeLabel = intlAuto ? "🤖 Міжнар.: авто" : "✋ Міжнар.: вручну";
            var rows = new List<InlineKeyboardButton[]>
            {
                CreateRow(("🏥 Бюджет МОЗ", "state_admin_health_budget_health")),
                CreateRow(("🌍 Бюджет міжнародних програм", "state_admin_health_budget_intl"), (intlModeLabel, "state_admin_health_budget_intl_mode")),
                CreateRow(("🔙 До МОЗ", "state_admin_health"))
            };

            if (!canControl)
                rows.Insert(0, CreateRow(("ℹ Лише перегляд", "state_noop")));

            return new InlineKeyboardMarkup(rows);
        }

        public static InlineKeyboardMarkup AdminHealthActionsKeyboard()
        {
            return new InlineKeyboardMarkup(new[]
            {
                CreateRow(("🚧 Карантин увімк./вимк.", "state_admin_health_action_quarantine_toggle"), ("🩺 Мобілізація медиків", "state_admin_health_action_medic_mobilize")),
                CreateRow(("📣 Кампанія довіри", "state_admin_health_action_trust_campaign"), ("💉 Вакцинація", "state_admin_health_action_vaccination")),
                CreateRow(("🧪 Епідмоніторинг стоків", "state_admin_health_action_wastewater"), ("📱 Телемедицина й тріаж", "state_admin_health_action_telemed")),
                CreateRow(("🧬 R&D ліків", "state_admin_health_action_drug_research"), ("👩‍⚕️ Резиденції лікарів", "state_admin_health_action_doctor_training")),
                CreateRow(("🧪 Клінічні випробування", "state_admin_health_action_clinical_trials"), ("🧼 Санітарні рейди", "state_admin_health_action_sanitation_raids")),
                CreateRow(("🔙 До МОЗ", "state_admin_health"))
            });
        }

        public static InlineKeyboardMarkup AdminHealthDiseasesKeyboard(List<(string text, string data)> diseaseItems, bool canControl)
        {
            var rows = new List<InlineKeyboardButton[]>();
            if (diseaseItems != null)
            {
                foreach (var item in diseaseItems)
                    rows.Add(CreateRow((item.text, item.data)));
            }

            if (rows.Count == 0)
                rows.Add(CreateRow(("Немає активних хвороб", "state_noop")));

            if (canControl)
                rows.Add(CreateRow(("🎲 Створити (ГМ)", "state_admin_health_gm_spawn_help"), ("🛑 Завершити всі (ГМ)", "state_admin_health_gm_end_all")));

            rows.Add(CreateRow(("🔙 До МОЗ", "state_admin_health")));
            return new InlineKeyboardMarkup(rows);
        }

        public static InlineKeyboardMarkup AdminHealthIntlKeyboard(List<(string text, string data)> programItems, bool canControl)
        {
            var rows = new List<InlineKeyboardButton[]>();
            if (programItems != null)
            {
                foreach (var item in programItems)
                    rows.Add(CreateRow((item.text, item.data)));
            }

            if (rows.Count == 0)
                rows.Add(CreateRow(("Немає програм", "state_noop")));

            if (!canControl)
                rows.Insert(0, CreateRow(("ℹ Лише перегляд", "state_noop")));

            rows.Add(CreateRow(("🔙 До МОЗ", "state_admin_health")));
            return new InlineKeyboardMarkup(rows);
        }

        public static InlineKeyboardMarkup AdminEducationMenuKeyboard(bool canControl)
        {
            var rows = new List<InlineKeyboardButton[]>
            {
                CreateRow(("📊 Показники МОН", "state_admin_education_report"), ("💸 Бюджет МОН", "state_admin_education_budget")),
                CreateRow(("🛠 Реформи МОН", "state_admin_education_actions"), ("🌍 Міжнар. (МОН)", "state_admin_education_intl"))
            };

            if (!canControl)
                rows.Insert(0, CreateRow(("ℹ Лише перегляд", "state_noop")));

            rows.Add(CreateRow(("⬅ До головного меню", "state_main")));
            return new InlineKeyboardMarkup(rows);
        }

        public static InlineKeyboardMarkup AdminEducationBudgetKeyboard(bool intlAuto, bool canControl)
        {
            string intlModeLabel = intlAuto ? "🤖 Міжнар.: авто" : "✋ Міжнар.: вручну";
            var rows = new List<InlineKeyboardButton[]>
            {
                CreateRow(("🎓 Бюджет МОН", "state_admin_education_budget_education")),
                CreateRow(("🌍 Бюджет міжнародних програм", "state_admin_education_budget_intl"), (intlModeLabel, "state_admin_education_budget_intl_mode")),
                CreateRow(("🔙 До МОН", "state_admin_education"))
            };

            if (!canControl)
                rows.Insert(0, CreateRow(("ℹ Лише перегляд", "state_noop")));

            return new InlineKeyboardMarkup(rows);
        }

        public static InlineKeyboardMarkup AdminEducationActionsKeyboard(bool canControl)
        {
            var rows = new List<InlineKeyboardButton[]>
            {
                CreateRow(("📚 Кампанія грамотності", "state_admin_education_action_literacy"), ("👩‍🏫 Підготовка викладачів", "state_admin_education_action_teachers")),
                CreateRow(("🎓 Стипендійний поштовх", "state_admin_education_action_scholarship"), ("🫱 Громадянська програма", "state_admin_education_action_civics")),
                CreateRow(("🔬 Освітні гранти STEM", "state_admin_education_action_teacher_cert")),
                CreateRow(("🧠 Інтенсивне тьюторство", "state_admin_education_action_tutoring"), ("🍲 Шкільне харчування", "state_admin_education_action_school_meals")),
                CreateRow(("🧑‍🏫 Наставництво вчителів", "state_admin_education_action_mentor")),
                CreateRow(("🔙 До МОН", "state_admin_education"))
            };

            if (!canControl)
                rows.Insert(0, CreateRow(("ℹ Лише перегляд", "state_noop")));

            return new InlineKeyboardMarkup(rows);
        }

        public static InlineKeyboardMarkup AdminEducationIntlKeyboard(List<(string text, string data)> programItems, bool canControl)
        {
            var rows = new List<InlineKeyboardButton[]>();
            if (programItems != null)
            {
                foreach (var item in programItems)
                    rows.Add(CreateRow((item.text, item.data)));
            }

            if (rows.Count == 0)
                rows.Add(CreateRow(("Немає програм", "state_noop")));

            if (!canControl)
                rows.Insert(0, CreateRow(("ℹ Лише перегляд", "state_noop")));

            rows.Add(CreateRow(("🔙 До МОН", "state_admin_education")));
            return new InlineKeyboardMarkup(rows);
        }

        public static InlineKeyboardMarkup AdminEconomyKeyboard()
        {
            var rows = new List<InlineKeyboardButton[]>
            {
                CreateRow(("Податки", "state_admin_tax"), ("Бюджет", "state_admin_budget")),
                CreateRow(("👥 ЗП людям", "state_admin_salary_civilian"), ("🏛 ЗП депутатам", "state_admin_salary_deputy")),
                CreateRow(("Звіт", "state_economy_report")),
                CreateRow(("Контроль інфляції", "state_admin_inflation"), ("Валюта / курси", "state_economy_currency")),
                CreateRow(("⚙ Державне управління", "state_admin"))
            };

            return new InlineKeyboardMarkup(rows);
        }

        public static InlineKeyboardMarkup AdminSocialKeyboard()
        {
            var rows = new List<InlineKeyboardButton[]>
            {
                CreateRow(("🏛 Парламент", "state_parliament"), ("🧠 Дослідження", "state_research")),
                CreateRow(("Стабільність", "state_admin_stability"), ("Щастя населення", "state_admin_happiness")),
                CreateRow(("Корупція", "state_admin_corruption")),
                CreateRow(("Соц. напруга", "state_admin_unrest")),
                CreateRow(("Приріст / смертність", "state_admin_population_growth")),
                CreateRow(("➕ Додати населення", "state_admin_population_add"), ("➖ Відняти населення", "state_admin_population_remove")),
                CreateRow(("🧬 Раси населення", "state_admin_population_races")),
                CreateRow(("⚙ Державне управління", "state_admin"))
            };

            return new InlineKeyboardMarkup(rows);
        }

        public static InlineKeyboardMarkup InlineChoiceKeyboard(
            IReadOnlyList<(string text, string value)> options,
            string callbackPrefix,
            int columns = 2,
            bool includeCancel = true)
        {
            return BuildChoiceKeyboard(options, callbackPrefix, columns, includeCancel);
        }

        public static InlineKeyboardMarkup AdminPopulationRacesKeyboard(List<(string text, string data)> items, int page, int totalPages)
        {
            var rows = new List<InlineKeyboardButton[]>();

            if (items != null)
            {
                foreach (var item in items)
                    rows.Add(CreateRow((item.text, item.data)));
            }

            if (totalPages > 1)
            {
                var nav = new List<(string text, string data)>();
                if (page > 0)
                    nav.Add(("⬅ Попередні", $"state_admin_population_races_page_{page - 1}"));
                if (page < totalPages - 1)
                    nav.Add(("➡ Наступні", $"state_admin_population_races_page_{page + 1}"));
                if (nav.Count > 0)
                    rows.Add(CreateRow(nav.ToArray()));
            }

            rows.Add(CreateRow(("➕ Нова раса", "state_admin_population_race_new")));
            rows.Add(CreateRow(("🔙 До соцполітики", "state_admin_social")));
            return new InlineKeyboardMarkup(rows);
        }

        public static InlineKeyboardMarkup AdminPopulationRaceInfoKeyboard(string raceName, int returnPage)
        {
            string safeRace = string.IsNullOrWhiteSpace(raceName) ? "Люди" : raceName.Trim();
            int safePage = Math.Max(0, returnPage);
            string encoded = EncodeKey(safeRace);

            return new InlineKeyboardMarkup(new[]
            {
                CreateRow(("✏ Встановити", $"state_admin_population_race_set|{encoded}|{safePage}"),
                          ("➕ Додати", $"state_admin_population_race_add|{encoded}|{safePage}")),
                CreateRow(("➖ Відняти", $"state_admin_population_race_remove|{encoded}|{safePage}"),
                          ("🗑 Видалити", $"state_admin_population_race_delete|{encoded}|{safePage}")),
                CreateRow(("🔙 До рас", $"state_admin_population_races_page_{safePage}"))
            });
        }

        public static InlineKeyboardMarkup AdminMilitaryKeyboard()
        {
            var rows = new List<InlineKeyboardButton[]>
            {
                CreateRow(("Мобілізація", "state_admin_mobilize"), ("Утримання армії", "state_admin_military_upkeep")),
                CreateRow(("🧠 Мораль армії ±", "state_admin_army_morale"), ("🗡 Якість зброї ±", "state_admin_weapon_quality")),
                CreateRow(("🪖 ЗП військовим", "state_admin_salary_soldier")),
                CreateRow(("Воєнний стан", "state_admin_martiallaw"), ("Розвідка", "state_admin_intel")),
                CreateRow(("Контррозвідка", "state_admin_counterintel")),
                CreateRow(("🚚 Логістика", "state_economy_military_logistics"), ("🧱 Частини", "state_economy_army_structure")),
                CreateRow(("🏰 Васали", "state_economy_vassals")),
                CreateRow(("⚙ Державне управління", "state_admin"))
            };

            return new InlineKeyboardMarkup(rows);
        }

        public static InlineKeyboardMarkup AdminResourcesKeyboard()
        {
            var rows = new List<InlineKeyboardButton[]>
            {
                CreateRow(("Звіт ресурсів", "state_economy_resources_report"), ("Ресурси", "state_economy_resources")),
                CreateRow(("Торгівля", "state_economy_trade"), ("Регіони ресурсів", "state_admin_resource_regions")),
                CreateRow(("⚙ Державне управління", "state_admin"))
            };

            return new InlineKeyboardMarkup(rows);
        }

        public static InlineKeyboardMarkup AdminAnalyticsKeyboard()
        {
            var rows = new List<InlineKeyboardButton[]>
            {
                CreateRow(("Попередження економіки", "state_economy_warnings"), ("ℹ Загальна інфа", "state_overview")),
                CreateRow(("Логи дій", "state_admin_log")),
                CreateRow(("⚙ Державне управління", "state_admin"))
            };

            return new InlineKeyboardMarkup(rows);
        }

        public static InlineKeyboardMarkup EconomyCurrencyMenuKeyboard(bool isMaster)
        {
            var rows = new List<InlineKeyboardButton[]>
            {
                CreateRow(("📈 Курс", "state_economy_currency_rate"), ("🕶 Чорний ринок", "state_economy_currency_black")),
                CreateRow(("🏦 Центробанк", "state_economy_currency_central"), ("📉 Інфляція", "state_economy_currency_inflation")),
                CreateRow(("💱 Конвертація", "state_economy_currency_convert"))
            };

            if (isMaster)
            {
                rows.Add(CreateRow(("➡ Переказ", "state_economy_currency_transfer"), ("✏ Змінити курс", "state_economy_currency_adjust")));
                rows.Add(CreateRow(("💵 Позика", "state_economy_currency_loan"), ("✅ Погашення", "state_economy_currency_repay")));
            }
            else
            {
                rows.Add(CreateRow(("➡ Переказ", "state_economy_currency_transfer")));
                rows.Add(CreateRow(("💵 Позика", "state_economy_currency_loan")));
            }

            rows.Add(CreateRow(("💰 Економіка", "state_economy")));
            return new InlineKeyboardMarkup(rows);
        }

        public static InlineKeyboardMarkup EconomyCurrencyInflationKeyboard(bool canManageInflation)
        {
            var rows = new List<InlineKeyboardButton[]>
            {
                CreateRow(("📈 Курс", "state_economy_currency_rate"), ("🕶 Чорний ринок", "state_economy_currency_black")),
                CreateRow(("🏦 Центробанк", "state_economy_currency_central"), ("📉 Оновити", "state_economy_currency_inflation"))
            };

            if (canManageInflation)
            {
                rows.Add(CreateRow(("➖10", "state_economy_currency_inflation_shift_-10"), ("➖5", "state_economy_currency_inflation_shift_-5"), ("➖1", "state_economy_currency_inflation_shift_-1")));
                rows.Add(CreateRow(("➕1", "state_economy_currency_inflation_shift_1"), ("➕5", "state_economy_currency_inflation_shift_5"), ("➕10", "state_economy_currency_inflation_shift_10")));
                rows.Add(CreateRow(("🎯 Норма 2%", "state_economy_currency_inflation_set_2")));
            }

            if (canManageInflation)
                rows.Add(CreateRow(("✏ Точне значення", "state_admin_inflation")));

            rows.Add(CreateRow(("⬅ До валют", "state_economy_currency"), ("💰 Економіка", "state_economy")));
            return new InlineKeyboardMarkup(rows);
        }

        public static InlineKeyboardMarkup EconomyCurrencyDisabledKeyboard(bool isMaster)
        {
            var rows = new List<InlineKeyboardButton[]>();
            if (isMaster)
            {
                rows.Add(CreateRow(("➡ Переказ", "state_economy_currency_transfer"), ("💵 Позика", "state_economy_currency_loan")));
                rows.Add(CreateRow(("✅ Погашення", "state_economy_currency_repay")));
            }
            else
                rows.Add(CreateRow(("➡ Переказ", "state_economy_currency_transfer"), ("💵 Позика", "state_economy_currency_loan")));

            rows.Add(CreateRow(("💰 Економіка", "state_economy")));
            return new InlineKeyboardMarkup(rows);
        }

        public static InlineKeyboardMarkup EconomyCurrencyDisabledKeyboard()
        {
            return EconomyCurrencyDisabledKeyboard(false);
        }

        public static InlineKeyboardMarkup EconomyCurrencySelectKeyboard(List<(string code, string name)> items, int page, int totalPages, string target)
        {
            var rows = new List<InlineKeyboardButton[]>();
            foreach (var it in items)
            {
                rows.Add(CreateRow(($"{it.code} • {it.name}", $"state_economy_currency_{target}_{it.code}")));
            }

            if (totalPages > 1)
            {
                var nav = new List<(string text, string data)>();
                if (page > 0)
                    nav.Add(("⬅ Попередні", $"state_economy_currency_{target}_page_{page - 1}"));
                if (page < totalPages - 1)
                    nav.Add(("➡ Наступні", $"state_economy_currency_{target}_page_{page + 1}"));
                if (nav.Count > 0)
                    rows.Add(CreateRow(nav.ToArray()));
            }

            rows.Add(CreateRow(("🔙 Назад", "state_economy_currency")));
            return new InlineKeyboardMarkup(rows);
        }

        public static InlineKeyboardMarkup EconomyCurrencyCountrySelectKeyboard(List<CountryRecord> countries, int page, int totalPages, string action, string target)
        {
            var rows = new List<InlineKeyboardButton[]>();
            foreach (var c in countries)
            {
                string flag = string.IsNullOrWhiteSpace(c.FlagEmoji) ? "" : $"{c.FlagEmoji} ";
                string name = string.IsNullOrWhiteSpace(c.Name) ? $"#{c.CountryId}" : c.Name;
                rows.Add(CreateRow(($"{flag}{name}", $"state_economy_currency_action_{action}_{target}_{c.CountryId}")));
            }

            if (totalPages > 1)
            {
                var nav = new List<(string text, string data)>();
                if (page > 0)
                    nav.Add(("⬅ Попередні", $"state_economy_currency_action_{action}_{target}_page_{page - 1}"));
                if (page < totalPages - 1)
                    nav.Add(("➡ Наступні", $"state_economy_currency_action_{action}_{target}_page_{page + 1}"));
                if (nav.Count > 0)
                    rows.Add(CreateRow(nav.ToArray()));
            }

            rows.Add(CreateRow(("🔙 Назад", "state_economy_currency")));
            return new InlineKeyboardMarkup(rows);
        }

        public static InlineKeyboardMarkup EconomyCurrencyAmountKeyboard(List<decimal> amounts, string action)
        {
            var rows = new List<InlineKeyboardButton[]>();
            foreach (var a in amounts)
            {
                rows.Add(CreateRow(($"{a:0.##}", $"state_economy_currency_action_{action}_amount_{a.ToString(CultureInfo.InvariantCulture)}")));
            }
            rows.Add(CreateRow(("🔙 Назад", "state_economy_currency")));
            return new InlineKeyboardMarkup(rows);
        }

        public static InlineKeyboardMarkup EconomyCurrencyTransferDaysKeyboard(string action)
        {
            var rows = new List<InlineKeyboardButton[]>
            {
                CreateRow(("1 день", $"state_economy_currency_action_{action}_days_1"), ("3 дні", $"state_economy_currency_action_{action}_days_3")),
                CreateRow(("7 днів", $"state_economy_currency_action_{action}_days_7"), ("14 днів", $"state_economy_currency_action_{action}_days_14")),
                CreateRow(("30 днів", $"state_economy_currency_action_{action}_days_30"), ("✍ Свій термін", $"state_economy_currency_action_{action}_days_custom")),
                CreateRow(("🔙 Назад", "state_economy_currency"))
            };

            return new InlineKeyboardMarkup(rows);
        }

        public static InlineKeyboardMarkup EconomyCurrencyRatePresetKeyboard(List<decimal> rates, string action)
        {
            var rows = new List<InlineKeyboardButton[]>();
            foreach (var r in rates)
            {
                rows.Add(CreateRow(($"{r:0.###}", $"state_economy_currency_action_{action}_rate_{r.ToString(CultureInfo.InvariantCulture)}")));
            }
            rows.Add(CreateRow(("🔙 Назад", "state_economy_currency")));
            return new InlineKeyboardMarkup(rows);
        }

        public static InlineKeyboardMarkup EconomyReportKeyboard(bool isMaster)
        {
            var rows = new List<InlineKeyboardButton[]>();
            if (isMaster)
            {
                rows.Add(CreateRow(("📥 Доходи (формула)", "state_economy_breakdown_income"), ("📤 Витрати (формула)", "state_economy_breakdown_expenses")));
                rows.Add(CreateRow(("📊 Стабільність (формула)", "state_economy_breakdown_stability"), ("📉 Безробіття", "state_economy_breakdown_unemployment")));
            }
            rows.Add(CreateRow(("🏭 Сектори", "state_economy_report_sectors")));
            rows.Add(CreateRow(("💰 Економіка", "state_economy")));
            return new InlineKeyboardMarkup(rows);
        }

        public static InlineKeyboardMarkup EconomyReportSectorsKeyboard(
            List<(string text, string data)> sectorItems,
            bool canEdit,
            bool hasSelection)
        {
            var rows = new List<InlineKeyboardButton[]>();

            if (sectorItems != null && sectorItems.Count > 0)
            {
                for (int i = 0; i < sectorItems.Count; i += 2)
                {
                    var left = sectorItems[i];
                    if (i + 1 < sectorItems.Count)
                    {
                        var right = sectorItems[i + 1];
                        rows.Add(CreateRow((left.text, left.data), (right.text, right.data)));
                    }
                    else
                    {
                        rows.Add(CreateRow((left.text, left.data)));
                    }
                }
            }

            if (canEdit && hasSelection)
            {
                rows.Add(CreateRow(("✏ Множник доходу", "state_economy_report_sector_income_edit"), ("💸 Податок сектора", "state_economy_report_sector_tax_edit")));
                rows.Add(CreateRow(("♻ Скинути налаштування", "state_economy_report_sector_reset")));
            }

            rows.Add(CreateRow(("🔙 До звіту", "state_economy_report"), ("💰 Економіка", "state_economy")));
            return new InlineKeyboardMarkup(rows);
        }

        public static InlineKeyboardMarkup CountryEffectsKeyboard(bool isInCrisis, bool isCollapsed, bool isInGoldenAge)
        {
            string crisis = isInCrisis ? "✅ Криза" : "❌ Криза";
            string collapse = isCollapsed ? "✅ Колапс" : "❌ Колапс";
            string golden = isInGoldenAge ? "✅ Золота доба" : "❌ Золота доба";

            return new InlineKeyboardMarkup(new[]
            {
                CreateRow((crisis, "state_country_effects_toggle_crisis"), (collapse, "state_country_effects_toggle_collapse")),
                CreateRow((golden, "state_country_effects_toggle_golden"), ("🧹 Скинути ефекти", "state_country_effects_clear")),
                CreateRow(("⬅ Назад", "state_main"))
            });
        }

        public static InlineKeyboardMarkup CountryEffectsConfirmCollapseKeyboard()
        {
            return new InlineKeyboardMarkup(new[]
            {
                CreateRow(("✅ Так, підтвердити", "state_country_effects_confirm_collapse_yes"),
                          ("❌ Скасувати", "state_country_effects_confirm_collapse_no")),
                CreateRow(("🌪 Ефекти країни", "state_country_effects"))
            });
        }

        public static InlineKeyboardMarkup DiplomacyCountriesKeyboard(List<CountryRecord> countries, int page, int totalPages)
        {
            var rows = new List<InlineKeyboardButton[]>();
            for (int i = 0; i < countries.Count; i += 2)
            {
                var left = countries[i];
                InlineKeyboardButton leftBtn = InlineKeyboardButton.WithCallbackData(
                    $"{FormatCountryLabel(left)}",
                    $"state_diplomacy_select_{left.CountryId}"
                );
                if (i + 1 < countries.Count)
                {
                    var right = countries[i + 1];
                    InlineKeyboardButton rightBtn = InlineKeyboardButton.WithCallbackData(
                        $"{FormatCountryLabel(right)}",
                        $"state_diplomacy_select_{right.CountryId}"
                    );
                    rows.Add(new[] { leftBtn, rightBtn });
                }
                else
                {
                    rows.Add(new[] { leftBtn });
                }
            }

            if (totalPages > 1)
            {
                var nav = new List<(string text, string data)>();
                if (page > 0)
                    nav.Add(("⬅ Попередні", $"state_diplomacy_page_{page - 1}"));
                if (page < totalPages - 1)
                    nav.Add(("➡ Наступні", $"state_diplomacy_page_{page + 1}"));
                if (nav.Count > 0)
                    rows.Add(CreateRow(nav.ToArray()));
            }

            rows.Add(CreateRow(("⬅ Назад", "state_main")));
            return new InlineKeyboardMarkup(rows);
        }

        public static InlineKeyboardMarkup DiplomacyActionsKeyboard(DiplomaticRelation relation, int targetCountryId, bool canEdit, bool isMaster)
        {
            string alliance = relation.IsAlliance ? "✅ Союз" : "➕ Союз";
            string sanctions = relation.HasSanctions ? "✅ Санкції" : "➕ Санкції";
            string embargo = relation.HasEmbargo ? "✅ Ембарго" : "➕ Ембарго";
            string trade = relation.HasTradeAgreement ? "✅ Торг. угода" : "➕ Торг. угода";
            string war = relation.IsAtWar || relation.State == DiplomaticState.War ? "🛡 Війна" : "☮ Мир";

            string MakeData(string action) => canEdit ? $"state_diplomacy_action_{action}_{targetCountryId}" : "state_noop";

            var rows = new List<InlineKeyboardButton[]>
            {
                CreateRow((war, MakeData("war")), ("☮ Мир", MakeData("peace"))),
                CreateRow((alliance, MakeData(relation.IsAlliance ? "break_alliance" : "alliance")),
                          (trade, MakeData(relation.HasTradeAgreement ? "break_trade" : "trade"))),
                CreateRow((sanctions, MakeData(relation.HasSanctions ? "lift_sanctions" : "sanctions")),
                          (embargo, MakeData(relation.HasEmbargo ? "lift_embargo" : "embargo")))
            };

            if (isMaster)
                rows.Add(CreateRow(("⚙ Статус відносин", $"state_diplomacy_state_{targetCountryId}")));

            rows.Add(CreateRow(("🤝 Дипломатія", "state_diplomacy")));
            return new InlineKeyboardMarkup(rows);
        }

        public static InlineKeyboardMarkup DiplomacyStateSelectKeyboard(int targetCountryId)
        {
            var rows = new List<InlineKeyboardButton[]>
            {
                CreateRow(("Союз", $"state_diplomacy_setstate_{targetCountryId}_alliance"),
                          ("Дружні", $"state_diplomacy_setstate_{targetCountryId}_friendly")),
                CreateRow(("Нейтральні", $"state_diplomacy_setstate_{targetCountryId}_neutral"),
                          ("Напружені", $"state_diplomacy_setstate_{targetCountryId}_tense")),
                CreateRow(("Ворожі", $"state_diplomacy_setstate_{targetCountryId}_hostile"),
                          ("Війна", $"state_diplomacy_setstate_{targetCountryId}_war")),
                CreateRow(("🔙 Назад", $"state_diplomacy_select_{targetCountryId}"))
            };
            return new InlineKeyboardMarkup(rows);
        }

        private static string FormatCountryLabel(CountryRecord c)
        {
            string flag = string.IsNullOrWhiteSpace(c.FlagEmoji) ? "" : $"{c.FlagEmoji} ";
            string name = string.IsNullOrWhiteSpace(c.Name) ? $"#{c.CountryId}" : c.Name;
            return $"{flag}{name}";
        }

        public static InlineKeyboardMarkup LoanApprovalKeyboard(long requestId)
        {
            return new InlineKeyboardMarkup(new[]
            {
                CreateRow(("✅ Схвалити", $"state_economy_loanapprove_{requestId}_yes"),
                          ("❌ Відхилити", $"state_economy_loanapprove_{requestId}_no"))
            });
        }

        public static InlineKeyboardMarkup TradeApprovalKeyboard(long requestId)
        {
            return new InlineKeyboardMarkup(new[]
            {
                CreateRow(("✅ Схвалити", $"state_economy_tradeapprove_{requestId}_yes"),
                          ("❌ Відхилити", $"state_economy_tradeapprove_{requestId}_no"))
            });
        }

        public static InlineKeyboardMarkup EconomyWarningsKeyboard()
        {
            return new InlineKeyboardMarkup(new[]
            {
                CreateRow(("💰 Економіка", "state_economy"))
            });
        }

        public static InlineKeyboardMarkup EconomyPreviewKeyboard()
        {
            return new InlineKeyboardMarkup(new[]
            {
                CreateRow(("⏩ Обрати дні", "state_economy_preview_days")),
                CreateRow(("💰 Економіка", "state_economy"))
            });
        }

        public static InlineKeyboardMarkup EconomyLaborKeyboard()
        {
            return new InlineKeyboardMarkup(new[]
            {
                CreateRow(("💰 Економіка", "state_economy"))
            });
        }

        public static InlineKeyboardMarkup EconomyCompaniesKeyboard()
        {
            return new InlineKeyboardMarkup(new[]
            {
                CreateRow(("📄 Реєстр компаній", "state_economy_companies_list"), ("➕ Створити компанію", "state_economy_company_add")),
                CreateRow(("🏗 Будівлі компаній", "state_economy_company_buildings"), ("💼 Бюджети компаній", "state_economy_companies_budget")),
                CreateRow(("👷 Ринок праці", "state_economy_labor"), ("📦 Звіт ресурсів", "state_economy_resources_report")),
                CreateRow(("💰 Економіка", "state_economy"))
            });
        }

        public static InlineKeyboardMarkup EconomyCompaniesListKeyboard(List<(string text, string data)> items, int page, int totalPages)
        {
            var rows = new List<InlineKeyboardButton[]>();
            foreach (var item in items)
                rows.Add(CreateRow((item.text, item.data)));

            if (totalPages > 1)
            {
                var nav = new List<(string text, string data)>();
                if (page > 0)
                    nav.Add(("⬅ Попередні", $"state_economy_companies_page_{page - 1}"));
                if (page < totalPages - 1)
                    nav.Add(("➡ Наступні", $"state_economy_companies_page_{page + 1}"));
                if (nav.Count > 0)
                    rows.Add(CreateRow(nav.ToArray()));
            }

            rows.Add(CreateRow(("➕ Створити компанію", "state_economy_company_add")));
            rows.Add(CreateRow(("🔙 Назад", "state_economy_companies")));
            return new InlineKeyboardMarkup(rows);
        }

        public static InlineKeyboardMarkup EconomyCompanyInfoKeyboard(int companyId)
        {
            return new InlineKeyboardMarkup(new[]
            {
                CreateRow(("✏ Редагувати", $"state_economy_company_edit_{companyId}"), ("🗑 Видалити", $"state_economy_company_delete_{companyId}")),
                CreateRow(("🏗 Будівлі компанії", $"state_economy_company_buildings_{companyId}")),
                CreateRow(("🔙 Назад", "state_economy_companies_list"))
            });
        }

        public static InlineKeyboardMarkup EconomyCompanyEditKeyboard(int companyId)
        {
            return new InlineKeyboardMarkup(new[]
            {
                CreateRow(("Назва", $"state_economy_company_editfield_{companyId}_Name"), ("Власник", $"state_economy_company_editfield_{companyId}_OwnerName")),
                CreateRow(("Тип виробн.", $"state_economy_company_editfield_{companyId}_ProductionType"), ("Робочі місця", $"state_economy_company_editfield_{companyId}_JobsOffered")),
                CreateRow(("Ресурс спожив.", $"state_economy_company_editfield_{companyId}_InputResourceType"), ("К-сть/день", $"state_economy_company_editfield_{companyId}_InputAmount")),
                CreateRow(("Зарплата", $"state_economy_company_editfield_{companyId}_BaseSalary"), ("Податок", $"state_economy_company_editfield_{companyId}_TaxRate")),
                CreateRow(("Бюджет", $"state_economy_company_editfield_{companyId}_Budget"), ("Держкомпанія", $"state_economy_company_editfield_{companyId}_IsStateCompany")),
                CreateRow(("Військ. частка", $"state_economy_company_editfield_{companyId}_MilitaryProductionShare"), ("Активна", $"state_economy_company_editfield_{companyId}_IsActive")),
                CreateRow(("🔙 Назад", $"state_economy_companyinfo_{companyId}"))
            });
        }

        public static InlineKeyboardMarkup EconomyCompanyBuildingsKeyboard(int companyId, List<(string text, string data)> items, int page, int totalPages)
        {
            var rows = new List<InlineKeyboardButton[]>();
            foreach (var item in items)
                rows.Add(CreateRow((item.text, item.data)));

            if (totalPages > 1)
            {
                var nav = new List<(string text, string data)>();
                if (page > 0)
                    nav.Add(("⬅ Попередні", $"state_economy_company_buildings_page|{companyId}|{page - 1}"));
                if (page < totalPages - 1)
                    nav.Add(("➡ Наступні", $"state_economy_company_buildings_page|{companyId}|{page + 1}"));
                if (nav.Count > 0)
                    rows.Add(CreateRow(nav.ToArray()));
            }

            rows.Add(CreateRow(("🔙 Назад", $"state_economy_companyinfo_{companyId}")));
            return new InlineKeyboardMarkup(rows);
        }

        public static InlineKeyboardMarkup MilitaryMenuKeyboard()
        {
            return new InlineKeyboardMarkup(new[]
            {
                CreateRow(("🗃 Арсенал", "state_military_arsenal"), ("🚚 Логістика війська", "state_economy_military_logistics")),
                CreateRow(("🧱 Структура армії", "state_economy_army_structure"), ("⚔ Бойові дії", "state_military_battle")),
                CreateRow(("🕵 Операції розвідки", "state_military_intel"), ("🪖 Частини", "state_military_units")),
                CreateRow(("🏰 Васали / данина", "state_military_vassals"), ("📦 Держзамовлення", "state_military_state_orders")),
                CreateRow(("🪖 ЗП військовим", "state_admin_salary_soldier")),
                CreateRow(("📣 Мобілізація", "state_admin_mobilize"), ("💸 Утримання", "state_admin_military_upkeep")),
                CreateRow(("🧠 Мораль ±", "state_admin_army_morale"), ("🗡 Якість зброї ±", "state_admin_weapon_quality")),
                CreateRow(("⚖ Воєнний стан", "state_admin_martiallaw"), ("🕵 Розвідка ±", "state_admin_intel")),
                CreateRow(("🛡 Контррозвідка ±", "state_admin_counterintel")),
                CreateRow(("⬅ Назад", "state_main"))
            });
        }

        public static InlineKeyboardMarkup MilitaryUnitsKeyboard()
        {
            return new InlineKeyboardMarkup(new[]
            {
                CreateRow(("📄 Реєстр частин", "state_military_units_list"), ("➕ Створити частину", "state_military_unit_add")),
                CreateRow(("⚡ Швидко: рота", "state_military_unit_add_company"), ("📦 Шаблони армії", "state_military_unit_templates")),
                CreateRow(("🛡 Військо", "state_military"))
            });
        }

        public static InlineKeyboardMarkup MilitaryUnitsListKeyboard(List<(string text, string data)> items, int page, int totalPages)
        {
            var rows = new List<InlineKeyboardButton[]>();
            foreach (var item in items)
                rows.Add(CreateRow((item.text, item.data)));

            if (totalPages > 1)
            {
                var nav = new List<(string text, string data)>();
                if (page > 0)
                    nav.Add(("⬅ Попередні", $"state_military_units_page_{page - 1}"));
                if (page < totalPages - 1)
                    nav.Add(("➡ Наступні", $"state_military_units_page_{page + 1}"));
                if (nav.Count > 0)
                    rows.Add(CreateRow(nav.ToArray()));
            }

            rows.Add(CreateRow(("➕ Створити частину", "state_military_unit_add")));
            rows.Add(CreateRow(("⚡ Швидко: рота", "state_military_unit_add_company")));
            rows.Add(CreateRow(("📦 Шаблони армії", "state_military_unit_templates")));
            rows.Add(CreateRow(("🔙 Назад", "state_military_units")));
            return new InlineKeyboardMarkup(rows);
        }

        public static InlineKeyboardMarkup MilitaryUnitTemplatesKeyboard(List<(string text, string data)> items, int page, int totalPages)
        {
            var rows = new List<InlineKeyboardButton[]>();
            foreach (var item in items)
                rows.Add(CreateRow((item.text, item.data)));

            if (totalPages > 1)
            {
                var nav = new List<(string text, string data)>();
                if (page > 0)
                    nav.Add(("⬅ Попередні", $"state_military_unit_templates_page_{page - 1}"));
                if (page < totalPages - 1)
                    nav.Add(("➡ Наступні", $"state_military_unit_templates_page_{page + 1}"));
                if (nav.Count > 0)
                    rows.Add(CreateRow(nav.ToArray()));
            }

            rows.Add(CreateRow(("🔙 До частин", "state_military_units")));
            return new InlineKeyboardMarkup(rows);
        }

        public static InlineKeyboardMarkup MilitaryUnitTemplateInfoKeyboard(string templateKey)
        {
            return new InlineKeyboardMarkup(new[]
            {
                CreateRow(("✅ Сформувати за шаблоном", $"state_military_unit_template_create_{templateKey}")),
                CreateRow(("🔙 До шаблонів", "state_military_unit_templates"))
            });
        }

        public static InlineKeyboardMarkup MilitaryUnitInfoKeyboard(int unitId)
        {
            return new InlineKeyboardMarkup(new[]
            {
                CreateRow(("✏ Редагувати", $"state_military_unit_edit_{unitId}"), ("🗑 Видалити", $"state_military_unit_delete_{unitId}")),
                CreateRow(("🔙 Назад", "state_military_units_list"))
            });
        }

        public static InlineKeyboardMarkup MilitaryUnitEditKeyboard(int unitId)
        {
            return new InlineKeyboardMarkup(new[]
            {
                CreateRow(("Назва", $"state_military_unit_editfield_{unitId}_UnitName"), ("Рівень", $"state_military_unit_editfield_{unitId}_UnitLevel")),
                CreateRow(("Роль", $"state_military_unit_editfield_{unitId}_UnitRole"), ("Тип", $"state_military_unit_editfield_{unitId}_UnitType")),
                CreateRow(("Раса", $"state_military_unit_editfield_{unitId}_Race"), ("Солдати", $"state_military_unit_editfield_{unitId}_Soldiers")),
                CreateRow(("Екіп. клас", $"state_military_unit_editfield_{unitId}_EquipmentTier"), ("Екіп. якість", $"state_military_unit_editfield_{unitId}_EquipmentQuality")),
                CreateRow(("Бойовий досвід", $"state_military_unit_editfield_{unitId}_BattleExperience"), ("Втома", $"state_military_unit_editfield_{unitId}_Fatigue")),
                CreateRow(("Статус", $"state_military_unit_editfield_{unitId}_Status")),
                CreateRow(("Підготовка", $"state_military_unit_editfield_{unitId}_TrainingLevel"), ("Мораль", $"state_military_unit_editfield_{unitId}_Morale")),
                CreateRow(("Озброєння", $"state_military_unit_editfield_{unitId}_WeaponReadiness"), ("Забезпечення", $"state_military_unit_editfield_{unitId}_SupplyLevel")),
                CreateRow(("Командир", $"state_military_unit_editfield_{unitId}_CommanderName"), ("Навички", $"state_military_unit_editfield_{unitId}_CommanderSkill")),
                CreateRow(("Лояльність", $"state_military_unit_editfield_{unitId}_CommanderLoyalty"), ("Корупція", $"state_military_unit_editfield_{unitId}_CommanderCorruption")),
                CreateRow(("Досвід", $"state_military_unit_editfield_{unitId}_CommanderExperience"), ("Мобілізована", $"state_military_unit_editfield_{unitId}_IsMobilized")),
                CreateRow(("Прогрес форм.", $"state_military_unit_editfield_{unitId}_FormationProgress"), ("Баз. днів", $"state_military_unit_editfield_{unitId}_FormationBaseDays")),
                CreateRow(("🔙 Назад", $"state_military_unitinfo_{unitId}"))
            });
        }

        public static InlineKeyboardMarkup MilitaryBattleMenuKeyboard(bool hasTarget, bool includeVassals)
        {
            return new InlineKeyboardMarkup(new[]
            {
                CreateRow(("🎯 Обрати ціль", "state_military_battle_target"), ("🗺 Місцевість", "state_military_battle_terrain")),
                CreateRow(("🧭 Тактика", "state_military_battle_tactic"), (includeVassals ? "🏰 Васали: увімк." : "🏰 Васали: вимк.", "state_military_battle_toggle_vassals")),
                CreateRow(("⚔ Вступити в бій", "state_military_battle_engage"), ("🪽 Відступити", "state_military_battle_retreat")),
                CreateRow(("📜 Журнал боїв", "state_military_battle_logs")),
                CreateRow((hasTarget ? "✅ Ціль обрана" : "ℹ Обери ціль перед боєм", "state_noop")),
                CreateRow(("🔙 Назад", "state_military"))
            });
        }

        public static InlineKeyboardMarkup MilitaryBattleTerrainKeyboard(string selectedTerrain)
        {
            static string Mark(string key, string selected, string label)
            {
                return string.Equals(key, selected, StringComparison.OrdinalIgnoreCase) ? $"✅ {label}" : label;
            }

            return new InlineKeyboardMarkup(new[]
            {
                CreateRow((Mark("Plains", selectedTerrain, "🌾 Рівнина"), "state_military_battle_terrain_set_Plains"),
                          (Mark("Forest", selectedTerrain, "🌲 Ліс"), "state_military_battle_terrain_set_Forest")),
                CreateRow((Mark("Hills", selectedTerrain, "⛰ Пагорби"), "state_military_battle_terrain_set_Hills"),
                          (Mark("Mountains", selectedTerrain, "🏔 Гори"), "state_military_battle_terrain_set_Mountains")),
                CreateRow((Mark("Swamp", selectedTerrain, "🪵 Болото"), "state_military_battle_terrain_set_Swamp"),
                          (Mark("Urban", selectedTerrain, "🏙 Місто"), "state_military_battle_terrain_set_Urban")),
                CreateRow((Mark("Coastal", selectedTerrain, "🏖 Узбережжя"), "state_military_battle_terrain_set_Coastal"),
                          (Mark("Sea", selectedTerrain, "🌊 Море"), "state_military_battle_terrain_set_Sea")),
                CreateRow(("🔙 До бою", "state_military_battle"))
            });
        }

        public static InlineKeyboardMarkup MilitaryBattleTacticKeyboard(string selectedTactic)
        {
            static string Mark(string key, string selected, string label)
            {
                return string.Equals(key, selected, StringComparison.OrdinalIgnoreCase) ? $"✅ {label}" : label;
            }

            return new InlineKeyboardMarkup(new[]
            {
                CreateRow((Mark("Balanced", selectedTactic, "⚖ Збалансована"), "state_military_battle_tactic_set_Balanced"),
                          (Mark("Aggressive", selectedTactic, "⚔ Агресивна"), "state_military_battle_tactic_set_Aggressive")),
                CreateRow((Mark("Defensive", selectedTactic, "🛡 Оборонна"), "state_military_battle_tactic_set_Defensive"),
                          (Mark("Skirmish", selectedTactic, "🏹 Маневрена"), "state_military_battle_tactic_set_Skirmish")),
                CreateRow((Mark("Siege", selectedTactic, "🏰 Облога"), "state_military_battle_tactic_set_Siege")),
                CreateRow(("🔙 До бою", "state_military_battle"))
            });
        }

        public static InlineKeyboardMarkup MilitaryBattleTargetSelectKeyboard(List<CountryRecord> countries, int page, int totalPages, int? selectedTargetCountryId)
        {
            var rows = new List<InlineKeyboardButton[]>();
            foreach (var c in countries)
            {
                string flag = string.IsNullOrWhiteSpace(c.FlagEmoji) ? string.Empty : $"{c.FlagEmoji} ";
                string name = string.IsNullOrWhiteSpace(c.Name) ? $"#{c.CountryId}" : c.Name;
                bool isSelected = selectedTargetCountryId.HasValue && selectedTargetCountryId.Value == c.CountryId;
                rows.Add(CreateRow(($"{(isSelected ? "✅ " : string.Empty)}{flag}{name}", $"state_military_battle_target_pick_{c.CountryId}")));
            }

            if (totalPages > 1)
            {
                var nav = new List<(string text, string data)>();
                if (page > 0)
                    nav.Add(("⬅ Попередні", $"state_military_battle_target_page_{page - 1}"));
                if (page < totalPages - 1)
                    nav.Add(("➡ Наступні", $"state_military_battle_target_page_{page + 1}"));
                if (nav.Count > 0)
                    rows.Add(CreateRow(nav.ToArray()));
            }

            rows.Add(CreateRow(("🔙 До бою", "state_military_battle")));
            return new InlineKeyboardMarkup(rows);
        }

        public static InlineKeyboardMarkup MilitaryBattleLogsKeyboard(List<(string text, string data)> items, int page, int totalPages)
        {
            var rows = new List<InlineKeyboardButton[]>();
            foreach (var item in items)
                rows.Add(CreateRow((item.text, item.data)));

            if (totalPages > 1)
            {
                var nav = new List<(string text, string data)>();
                if (page > 0)
                    nav.Add(("⬅ Попередні", $"state_military_battle_logs_page_{page - 1}"));
                if (page < totalPages - 1)
                    nav.Add(("➡ Наступні", $"state_military_battle_logs_page_{page + 1}"));
                if (nav.Count > 0)
                    rows.Add(CreateRow(nav.ToArray()));
            }

            rows.Add(CreateRow(("🔙 До бою", "state_military_battle")));
            return new InlineKeyboardMarkup(rows);
        }

        public static InlineKeyboardMarkup MilitaryBattleLogInfoKeyboard()
        {
            return new InlineKeyboardMarkup(new[]
            {
                CreateRow(("🔙 До журналу", "state_military_battle_logs"))
            });
        }

        public static InlineKeyboardMarkup MilitaryIntelMenuKeyboard(bool hasTarget, string operationType, long budgetGold)
        {
            static string Mark(bool selected, string label) => selected ? $"✅ {label}" : label;

            string op = MilitaryIntelService.NormalizeOperationType(operationType);
            long budget = Math.Max(100, Math.Min(100000, budgetGold));

            return new InlineKeyboardMarkup(new[]
            {
                CreateRow(("🎯 Обрати ціль", "state_military_intel_target"), ("🧭 Тип операції", "state_military_intel_operation")),
                CreateRow((Mark(budget == 500, "💰 500"), "state_military_intel_budget_set_500"),
                          (Mark(budget == 1500, "💰 1 500"), "state_military_intel_budget_set_1500"),
                          (Mark(budget == 5000, "💰 5 000"), "state_military_intel_budget_set_5000")),
                CreateRow((Mark(budget == 12000, "💰 12 000"), "state_military_intel_budget_set_12000"),
                          (Mark(budget == 25000, "💰 25 000"), "state_military_intel_budget_set_25000")),
                CreateRow(("🚀 Провести операцію", "state_military_intel_execute"), ("📜 Журнал", "state_military_intel_logs")),
                CreateRow((hasTarget ? $"✅ Обрано: {MilitaryIntelService.FormatOperationTypeUk(op)}" : "ℹ Обери ціль перед запуском", "state_noop")),
                CreateRow(("🔙 Назад", "state_military"))
            });
        }

        public static InlineKeyboardMarkup MilitaryIntelTargetSelectKeyboard(List<CountryRecord> countries, int page, int totalPages, int? selectedTargetCountryId)
        {
            var rows = new List<InlineKeyboardButton[]>();
            foreach (var c in countries)
            {
                string flag = string.IsNullOrWhiteSpace(c.FlagEmoji) ? string.Empty : $"{c.FlagEmoji} ";
                string name = string.IsNullOrWhiteSpace(c.Name) ? $"#{c.CountryId}" : c.Name;
                bool isSelected = selectedTargetCountryId.HasValue && selectedTargetCountryId.Value == c.CountryId;
                rows.Add(CreateRow(($"{(isSelected ? "✅ " : string.Empty)}{flag}{name}", $"state_military_intel_target_pick_{c.CountryId}")));
            }

            if (totalPages > 1)
            {
                var nav = new List<(string text, string data)>();
                if (page > 0)
                    nav.Add(("⬅ Попередні", $"state_military_intel_target_page_{page - 1}"));
                if (page < totalPages - 1)
                    nav.Add(("➡ Наступні", $"state_military_intel_target_page_{page + 1}"));
                if (nav.Count > 0)
                    rows.Add(CreateRow(nav.ToArray()));
            }

            rows.Add(CreateRow(("🔙 До розвідки", "state_military_intel")));
            return new InlineKeyboardMarkup(rows);
        }

        public static InlineKeyboardMarkup MilitaryIntelOperationKeyboard(string selectedOperationType)
        {
            static string Mark(string key, string selected, string label)
            {
                return string.Equals(key, selected, StringComparison.OrdinalIgnoreCase) ? $"✅ {label}" : label;
            }

            string op = MilitaryIntelService.NormalizeOperationType(selectedOperationType);
            return new InlineKeyboardMarkup(new[]
            {
                CreateRow((Mark("ScoutForces", op, "👁 Розвідка військ"), "state_military_intel_operation_set_ScoutForces")),
                CreateRow((Mark("StealTreasury", op, "💰 Екон. шпигунство"), "state_military_intel_operation_set_StealTreasury")),
                CreateRow((Mark("SabotageLogistics", op, "🧨 Диверсія логістики"), "state_military_intel_operation_set_SabotageLogistics")),
                CreateRow((Mark("CounterintelRaid", op, "🛡 Контррозвід. рейд"), "state_military_intel_operation_set_CounterintelRaid")),
                CreateRow(("🔙 До розвідки", "state_military_intel"))
            });
        }

        public static InlineKeyboardMarkup MilitaryIntelLogsKeyboard(List<(string text, string data)> items, int page, int totalPages)
        {
            var rows = new List<InlineKeyboardButton[]>();
            foreach (var item in items)
                rows.Add(CreateRow((item.text, item.data)));

            if (totalPages > 1)
            {
                var nav = new List<(string text, string data)>();
                if (page > 0)
                    nav.Add(("⬅ Попередні", $"state_military_intel_logs_page_{page - 1}"));
                if (page < totalPages - 1)
                    nav.Add(("➡ Наступні", $"state_military_intel_logs_page_{page + 1}"));
                if (nav.Count > 0)
                    rows.Add(CreateRow(nav.ToArray()));
            }

            rows.Add(CreateRow(("🔙 До розвідки", "state_military_intel")));
            return new InlineKeyboardMarkup(rows);
        }

        public static InlineKeyboardMarkup MilitaryIntelLogInfoKeyboard()
        {
            return new InlineKeyboardMarkup(new[]
            {
                CreateRow(("🔙 До журналу", "state_military_intel_logs"))
            });
        }

        public static InlineKeyboardMarkup MilitaryVassalsKeyboard()
        {
            return new InlineKeyboardMarkup(new[]
            {
                CreateRow(("📄 Реєстр васалів", "state_military_vassals_list"), ("➕ Додати васала", "state_military_vassal_add")),
                CreateRow(("🌍 Призначити з країн", "state_military_vassal_add_country")),
                CreateRow(("🛡 Військо", "state_military"))
            });
        }

        public static InlineKeyboardMarkup MilitaryVassalsListKeyboard(List<(string text, string data)> items, int page, int totalPages)
        {
            var rows = new List<InlineKeyboardButton[]>();
            foreach (var item in items)
                rows.Add(CreateRow((item.text, item.data)));

            if (totalPages > 1)
            {
                var nav = new List<(string text, string data)>();
                if (page > 0)
                    nav.Add(("⬅ Попередні", $"state_military_vassals_page_{page - 1}"));
                if (page < totalPages - 1)
                    nav.Add(("➡ Наступні", $"state_military_vassals_page_{page + 1}"));
                if (nav.Count > 0)
                    rows.Add(CreateRow(nav.ToArray()));
            }

            rows.Add(CreateRow(("➕ Додати васала", "state_military_vassal_add")));
            rows.Add(CreateRow(("🔙 Назад", "state_military_vassals")));
            return new InlineKeyboardMarkup(rows);
        }

        public static InlineKeyboardMarkup MilitaryVassalInfoKeyboard(int obligationId, bool isActive)
        {
            return new InlineKeyboardMarkup(new[]
            {
                CreateRow(("✏ Редагувати", $"state_military_vassal_edit_{obligationId}"), ("🗑 Видалити", $"state_military_vassal_delete_{obligationId}")),
                CreateRow((isActive ? "⏸ Призупинити" : "▶ Активувати", $"state_military_vassal_toggle_{obligationId}")),
                CreateRow(("🔙 Назад", "state_military_vassals_list"))
            });
        }

        public static InlineKeyboardMarkup MilitaryVassalEditKeyboard(int obligationId)
        {
            return new InlineKeyboardMarkup(new[]
            {
                CreateRow(("Назва васала", $"state_military_vassal_editfield_{obligationId}_SubjectName")),
                CreateRow(("Полки", $"state_military_vassal_editfield_{obligationId}_RegimentsProvided"), ("Данина", $"state_military_vassal_editfield_{obligationId}_FundingPerTurn")),
                CreateRow(("Лояльність", $"state_military_vassal_editfield_{obligationId}_Loyalty"), ("Ризик повстання", $"state_military_vassal_editfield_{obligationId}_RebellionRisk")),
                CreateRow(("Активність", $"state_military_vassal_editfield_{obligationId}_IsActive")),
                CreateRow(("🔙 Назад", $"state_military_vassalinfo_{obligationId}"))
            });
        }

        public static InlineKeyboardMarkup MilitaryVassalCountrySelectKeyboard(List<CountryRecord> countries, int page, int totalPages)
        {
            var rows = new List<InlineKeyboardButton[]>();
            foreach (var c in countries)
            {
                string flag = string.IsNullOrWhiteSpace(c.FlagEmoji) ? "" : $"{c.FlagEmoji} ";
                string name = string.IsNullOrWhiteSpace(c.Name) ? $"#{c.CountryId}" : c.Name;
                rows.Add(CreateRow(($"{flag}{name}", $"state_military_vassal_pick_{c.CountryId}")));
            }

            if (totalPages > 1)
            {
                var nav = new List<(string text, string data)>();
                if (page > 0)
                    nav.Add(("⬅ Попередні", $"state_military_vassal_country_page_{page - 1}"));
                if (page < totalPages - 1)
                    nav.Add(("➡ Наступні", $"state_military_vassal_country_page_{page + 1}"));
                if (nav.Count > 0)
                    rows.Add(CreateRow(nav.ToArray()));
            }

            rows.Add(CreateRow(("🔙 Назад", "state_military_vassals")));
            return new InlineKeyboardMarkup(rows);
        }

        public static InlineKeyboardMarkup EconomyMilitaryLogisticsKeyboard()
        {
            return new InlineKeyboardMarkup(new[]
            {
                CreateRow(("🗃 Арсенал", "state_military_arsenal"), ("🧱 Структура армії", "state_economy_army_structure")),
                CreateRow(("🏰 Васали / колонії", "state_economy_vassals")),
                CreateRow(("🛡 Військо", "state_military"))
            });
        }

        public static InlineKeyboardMarkup EconomyArmyStructureKeyboard()
        {
            return new InlineKeyboardMarkup(new[]
            {
                CreateRow(("🗃 Арсенал", "state_military_arsenal"), ("🚚 Логістика війська", "state_economy_military_logistics")),
                CreateRow(("🏰 Васали / колонії", "state_economy_vassals")),
                CreateRow(("🛡 Військо", "state_military"))
            });
        }

        public static InlineKeyboardMarkup EconomyVassalsKeyboard()
        {
            return new InlineKeyboardMarkup(new[]
            {
                CreateRow(("🗃 Арсенал", "state_military_arsenal"), ("🧱 Структура армії", "state_economy_army_structure")),
                CreateRow(("🚚 Логістика війська", "state_economy_military_logistics")),
                CreateRow(("🛡 Військо", "state_military"))
            });
        }

        public static InlineKeyboardMarkup MilitaryArsenalKeyboard()
        {
            return new InlineKeyboardMarkup(new[]
            {
                CreateRow(("📦 Склади", "state_military_arsenal_stock"), ("🧾 Держзакупівлі", "state_military_arsenal_procurement")),
                CreateRow(("✋ Ручний розподіл", "state_military_arsenal_distribute_manual"), ("⚙ Авто-розподіл", "state_military_arsenal_distribute_auto")),
                CreateRow(("📈 Автозабезпечення", "state_military_arsenal_autobuild")),
                CreateRow(("➕ Швидка військкомпанія", "state_military_arsenal_company_quick"), ("🏭 Компанії", "state_economy_companies")),
                CreateRow(("🛡 Військові шаблони", "state_economy_building_templates_military"), ("📚 Військ. ресурси", "state_economy_resources_list_military")),
                CreateRow(("🛡 Військо", "state_military"))
            });
        }

        public static InlineKeyboardMarkup MilitaryArsenalStockKeyboard()
        {
            return new InlineKeyboardMarkup(new[]
            {
                CreateRow(("🧾 До закупівель", "state_military_arsenal_procurement"), ("✋ Ручний розподіл", "state_military_arsenal_distribute_manual")),
                CreateRow(("⚙ Авто-розподіл", "state_military_arsenal_distribute_auto")),
                CreateRow(("🔙 Арсенал", "state_military_arsenal"))
            });
        }

        public static InlineKeyboardMarkup MilitaryArsenalProcurementKeyboard(List<(string text, string data)> items, int page, int totalPages)
        {
            var rows = new List<InlineKeyboardButton[]>();
            foreach (var item in items)
                rows.Add(CreateRow((item.text, item.data)));

            if (totalPages > 1)
            {
                var nav = new List<(string text, string data)>();
                if (page > 0)
                    nav.Add(("⬅ Попередні", $"state_military_arsenal_procurement_page_{page - 1}"));
                if (page < totalPages - 1)
                    nav.Add(("➡ Наступні", $"state_military_arsenal_procurement_page_{page + 1}"));
                if (nav.Count > 0)
                    rows.Add(CreateRow(nav.ToArray()));
            }

            rows.Add(CreateRow(("📦 Склади", "state_military_arsenal_stock"), ("➕ Швидка військкомпанія", "state_military_arsenal_company_quick")));
            rows.Add(CreateRow(("🔙 Арсенал", "state_military_arsenal")));
            return new InlineKeyboardMarkup(rows);
        }

        public static InlineKeyboardMarkup MilitaryArsenalOfferKeyboard(List<(string text, string data)> actions, int returnPage)
        {
            var rows = new List<InlineKeyboardButton[]>();
            foreach (var action in actions)
                rows.Add(CreateRow((action.text, action.data)));

            rows.Add(CreateRow(("🔙 До закупівель", $"state_military_arsenal_procurement_page_{Math.Max(0, returnPage)}")));
            rows.Add(CreateRow(("🔙 Арсенал", "state_military_arsenal")));
            return new InlineKeyboardMarkup(rows);
        }

        public static InlineKeyboardMarkup MilitaryArsenalManualDistributionKeyboard(List<(string text, string data)> items, int page, int totalPages)
        {
            var rows = new List<InlineKeyboardButton[]>();
            foreach (var item in items)
                rows.Add(CreateRow((item.text, item.data)));

            if (totalPages > 1)
            {
                var nav = new List<(string text, string data)>();
                if (page > 0)
                    nav.Add(("⬅ Попередні", $"state_military_arsenal_distribute_manual_page_{page - 1}"));
                if (page < totalPages - 1)
                    nav.Add(("➡ Наступні", $"state_military_arsenal_distribute_manual_page_{page + 1}"));
                if (nav.Count > 0)
                    rows.Add(CreateRow(nav.ToArray()));
            }

            rows.Add(CreateRow(("⚙ Авто-розподіл", "state_military_arsenal_distribute_auto"), ("📦 Склади", "state_military_arsenal_stock")));
            rows.Add(CreateRow(("🔙 Арсенал", "state_military_arsenal")));
            return new InlineKeyboardMarkup(rows);
        }

        public static InlineKeyboardMarkup MilitaryArsenalUnitDistributionKeyboard(int unitId, int returnPage, bool hasStock)
        {
            var rows = new List<InlineKeyboardButton[]>();
            if (hasStock)
            {
                rows.Add(CreateRow(
                    ("10% складу", $"state_military_arsenal_unit_apply|{unitId}|10|{returnPage}"),
                    ("25% складу", $"state_military_arsenal_unit_apply|{unitId}|25|{returnPage}")));
                rows.Add(CreateRow(
                    ("50% складу", $"state_military_arsenal_unit_apply|{unitId}|50|{returnPage}"),
                    ("100% складу", $"state_military_arsenal_unit_apply|{unitId}|100|{returnPage}")));
            }

            rows.Add(CreateRow(("📦 Склади", "state_military_arsenal_stock"), ("⚙ Авто-розподіл", "state_military_arsenal_distribute_auto")));
            rows.Add(CreateRow(("🔙 До списку частин", $"state_military_arsenal_distribute_manual_page_{Math.Max(0, returnPage)}")));
            rows.Add(CreateRow(("🔙 Арсенал", "state_military_arsenal")));
            return new InlineKeyboardMarkup(rows);
        }

        public static InlineKeyboardMarkup EconomySanctionsKeyboard(int currentLevel)
        {
            return new InlineKeyboardMarkup(new[]
            {
                CreateRow(("🌍 Ліміти по країнах", "state_economy_sanctions_targets")),
                CreateRow(("📦 Ліміти по товарах", "state_economy_sanctions_products")),
                CreateRow(("💰 Економіка", "state_economy"))
            });
        }

        public static InlineKeyboardMarkup EconomyBuildingCategoryKeyboard()
        {
            return new InlineKeyboardMarkup(new[]
            {
                CreateRow(("Житло", "state_economy_building_cat_Housing"), ("Виробництво", "state_economy_building_cat_Production")),
                CreateRow(("Адміністрація", "state_economy_building_cat_Administration"), ("Наука", "state_economy_building_cat_Science")),
                CreateRow(("Військова", "state_economy_building_cat_Military"), ("Культура", "state_economy_building_cat_Culture")),
                CreateRow(("Інше", "state_economy_building_cat_Other")),
                CreateRow(("🔙 Назад", "state_economy_buildings"))
            });
        }

        public static InlineKeyboardMarkup EconomySanctionsTargetsKeyboard(List<CountryRecord> countries, Dictionary<int, EconomyCountrySanctionRecord> sanctions, int page, int totalPages)
        {
            var rows = new List<InlineKeyboardButton[]>();

            foreach (var c in countries)
            {
                string flag = string.IsNullOrWhiteSpace(c.FlagEmoji) ? "" : $"{c.FlagEmoji} ";
                string name = string.IsNullOrWhiteSpace(c.Name) ? $"#{c.CountryId}" : c.Name;
                string marker = string.Empty;
                if (sanctions != null && sanctions.TryGetValue(c.CountryId, out var s) &&
                    (s.ExportLevel == (int)SanctionsLevel.Embargo ||
                     s.ImportLevel == (int)SanctionsLevel.Embargo ||
                     s.ExportEffectiveness > 0f ||
                     s.ImportEffectiveness > 0f))
                {
                    float exportImpact = s.ExportLevel == (int)SanctionsLevel.Embargo
                        ? 100f
                        : EconomyCalculator.Clamp(s.ExportEffectiveness, 0f, 1f) * 100f;
                    float importImpact = s.ImportLevel == (int)SanctionsLevel.Embargo
                        ? 100f
                        : EconomyCalculator.Clamp(s.ImportEffectiveness, 0f, 1f) * 100f;

                    string export = s.ExportLevel == (int)SanctionsLevel.Embargo
                        ? "E: Ембарго"
                        : $"E: -{exportImpact:0.#}%";
                    string import = s.ImportLevel == (int)SanctionsLevel.Embargo
                        ? "I: Ембарго"
                        : $"I: -{importImpact:0.#}%";
                    var parts = new List<string>();
                    if (!string.IsNullOrWhiteSpace(export)) parts.Add(export);
                    if (!string.IsNullOrWhiteSpace(import)) parts.Add(import);
                    if (parts.Count > 0)
                        marker = " • " + string.Join(" / ", parts);
                }

                rows.Add(CreateRow(($"{flag}{name}{marker}", $"state_economy_sanctions_target_{c.CountryId}")));
            }

            if (totalPages > 1)
            {
                var nav = new List<(string text, string data)>();
                if (page > 0)
                    nav.Add(("⬅ Попередні", $"state_economy_sanctions_targets_page_{page - 1}"));
                if (page < totalPages - 1)
                    nav.Add(("➡ Наступні", $"state_economy_sanctions_targets_page_{page + 1}"));
                if (nav.Count > 0)
                    rows.Add(CreateRow(nav.ToArray()));
            }

            rows.Add(CreateRow(("🔙 Назад", "state_economy_sanctions")));
            return new InlineKeyboardMarkup(rows);
        }

        public static InlineKeyboardMarkup EconomySanctionsTargetKeyboard(int targetCountryId, int exportLevel, int importLevel, float exportEffectiveness, float importEffectiveness)
        {
            bool exportEmbargoActive = exportLevel == 3;
            bool importEmbargoActive = importLevel == 3;
            string exportEmbargo = exportEmbargoActive ? "✅ Ембарго експорту" : "⛔ Ембарго експорту";
            string importEmbargo = importEmbargoActive ? "✅ Ембарго імпорту" : "⛔ Ембарго імпорту";
            string exportReset = "Скинути експорт";
            string importReset = "Скинути імпорт";

            string exp25 = Math.Abs(exportEffectiveness - 0.25f) < 0.001f ? "? 25%" : "25%";
            string exp50 = Math.Abs(exportEffectiveness - 0.5f) < 0.001f ? "? 50%" : "50%";
            string exp75 = Math.Abs(exportEffectiveness - 0.75f) < 0.001f ? "? 75%" : "75%";
            string exp99 = Math.Abs(exportEffectiveness - 0.99f) < 0.001f ? "? 99%" : "99%";

            string imp25 = Math.Abs(importEffectiveness - 0.25f) < 0.001f ? "? 25%" : "25%";
            string imp50 = Math.Abs(importEffectiveness - 0.5f) < 0.001f ? "? 50%" : "50%";
            string imp75 = Math.Abs(importEffectiveness - 0.75f) < 0.001f ? "? 75%" : "75%";
            string imp99 = Math.Abs(importEffectiveness - 0.99f) < 0.001f ? "? 99%" : "99%";

            return new InlineKeyboardMarkup(new[]
            {
                CreateRow((exportEmbargo, $"state_economy_sanctions_targetset|{targetCountryId}|export|{(exportEmbargoActive ? 0 : 3)}"), (importEmbargo, $"state_economy_sanctions_targetset|{targetCountryId}|import|{(importEmbargoActive ? 0 : 3)}")),
                CreateRow((exp25, $"state_economy_sanctions_targeteff|{targetCountryId}|export|25"), (exp50, $"state_economy_sanctions_targeteff|{targetCountryId}|export|50"), (exp75, $"state_economy_sanctions_targeteff|{targetCountryId}|export|75")),
                CreateRow((exp99, $"state_economy_sanctions_targeteff|{targetCountryId}|export|99"), ("✏ Ввести %", $"state_economy_sanctions_targeteff_input|{targetCountryId}|export"), (exportReset, $"state_economy_sanctions_targetreset|{targetCountryId}|export")),
                CreateRow((imp25, $"state_economy_sanctions_targeteff|{targetCountryId}|import|25"), (imp50, $"state_economy_sanctions_targeteff|{targetCountryId}|import|50"), (imp75, $"state_economy_sanctions_targeteff|{targetCountryId}|import|75")),
                CreateRow((imp99, $"state_economy_sanctions_targeteff|{targetCountryId}|import|99"), ("✏ Ввести %", $"state_economy_sanctions_targeteff_input|{targetCountryId}|import"), (importReset, $"state_economy_sanctions_targetreset|{targetCountryId}|import")),
                CreateRow(("🔙 Назад", "state_economy_sanctions_targets"))
            });
        }

        public static InlineKeyboardMarkup EconomySanctionsProductCountryKeyboard(List<CountryRecord> countries, HashSet<int> targetsWithSanctions, int page, int totalPages)
        {
            var rows = new List<InlineKeyboardButton[]>();
            foreach (var c in countries)
            {
                string flag = string.IsNullOrWhiteSpace(c.FlagEmoji) ? "" : $"{c.FlagEmoji} ";
                string name = string.IsNullOrWhiteSpace(c.Name) ? $"#{c.CountryId}" : c.Name;
                string marker = targetsWithSanctions != null && targetsWithSanctions.Contains(c.CountryId) ? " • ⛔" : "";
                rows.Add(CreateRow(($"{flag}{name}{marker}", $"state_economy_sanctions_product_country_{c.CountryId}")));
            }

            if (totalPages > 1)
            {
                var nav = new List<(string text, string data)>();
                if (page > 0)
                    nav.Add(("⬅ Попередні", $"state_economy_sanctions_products_page_{page - 1}"));
                if (page < totalPages - 1)
                    nav.Add(("➡ Наступні", $"state_economy_sanctions_products_page_{page + 1}"));
                if (nav.Count > 0)
                    rows.Add(CreateRow(nav.ToArray()));
            }

            rows.Add(CreateRow(("🔙 Назад", "state_economy_sanctions")));
            return new InlineKeyboardMarkup(rows);
        }

        public static InlineKeyboardMarkup EconomySanctionsProductResourceKeyboard(int targetCountryId, List<EconomyResourceRecord> resources, List<EconomyResourceRecord> topResources, int page, int totalPages, bool showTop)
        {
            var rows = new List<InlineKeyboardButton[]>();
            rows.Add(CreateRow(("🌐 Весь імпорт/експорт країни", $"state_economy_sanctions_product_all|{targetCountryId}")));

            if (showTop && topResources != null && topResources.Count > 0)
            {
                foreach (var r in topResources)
                {
                    rows.Add(CreateRow(($"⭐ {FormatResourceTypeUk(r.ResourceType)}", $"state_economy_sanctions_product_resource|{targetCountryId}|{EncodeResourceKey(r.ResourceType)}")));
                }
            }
            foreach (var r in resources)
            {
                rows.Add(CreateRow(($"{FormatResourceTypeUk(r.ResourceType)}", $"state_economy_sanctions_product_resource|{targetCountryId}|{EncodeResourceKey(r.ResourceType)}")));
            }

            if (totalPages > 1)
            {
                var nav = new List<(string text, string data)>();
                if (page > 0)
                    nav.Add(("⬅ Попередні", $"state_economy_sanctions_product_resource_page|{targetCountryId}|{page - 1}"));
                if (page < totalPages - 1)
                    nav.Add(("➡ Наступні", $"state_economy_sanctions_product_resource_page|{targetCountryId}|{page + 1}"));
                if (nav.Count > 0)
                    rows.Add(CreateRow(nav.ToArray()));
            }

            rows.Add(CreateRow(("🔙 Назад", "state_economy_sanctions_products")));
            return new InlineKeyboardMarkup(rows);
        }

        public static InlineKeyboardMarkup EconomySanctionsProductKeyboard(int targetCountryId, string resourceKey, int exportLevel, int importLevel, float exportEffectiveness, float importEffectiveness)
        {
            bool exportEmbargoActive = exportLevel == 3;
            bool importEmbargoActive = importLevel == 3;
            string exportEmbargo = exportEmbargoActive ? "✅ Ембарго експорту" : "⛔ Ембарго експорту";
            string importEmbargo = importEmbargoActive ? "✅ Ембарго імпорту" : "⛔ Ембарго імпорту";
            string exportReset = "Скинути експорт";
            string importReset = "Скинути імпорт";

            string exp25 = Math.Abs(exportEffectiveness - 0.25f) < 0.001f ? "? 25%" : "25%";
            string exp50 = Math.Abs(exportEffectiveness - 0.5f) < 0.001f ? "? 50%" : "50%";
            string exp75 = Math.Abs(exportEffectiveness - 0.75f) < 0.001f ? "? 75%" : "75%";
            string exp99 = Math.Abs(exportEffectiveness - 0.99f) < 0.001f ? "? 99%" : "99%";

            string imp25 = Math.Abs(importEffectiveness - 0.25f) < 0.001f ? "? 25%" : "25%";
            string imp50 = Math.Abs(importEffectiveness - 0.5f) < 0.001f ? "? 50%" : "50%";
            string imp75 = Math.Abs(importEffectiveness - 0.75f) < 0.001f ? "? 75%" : "75%";
            string imp99 = Math.Abs(importEffectiveness - 0.99f) < 0.001f ? "? 99%" : "99%";

            return new InlineKeyboardMarkup(new[]
            {
                CreateRow((exportEmbargo, $"state_economy_sanctions_productset|{targetCountryId}|{resourceKey}|export|{(exportEmbargoActive ? 0 : 3)}"), (importEmbargo, $"state_economy_sanctions_productset|{targetCountryId}|{resourceKey}|import|{(importEmbargoActive ? 0 : 3)}")),
                CreateRow((exp25, $"state_economy_sanctions_producteff|{targetCountryId}|{resourceKey}|export|25"), (exp50, $"state_economy_sanctions_producteff|{targetCountryId}|{resourceKey}|export|50"), (exp75, $"state_economy_sanctions_producteff|{targetCountryId}|{resourceKey}|export|75")),
                CreateRow((exp99, $"state_economy_sanctions_producteff|{targetCountryId}|{resourceKey}|export|99"), ("✏ Ввести %", $"state_economy_sanctions_producteff_input|{targetCountryId}|{resourceKey}|export"), (exportReset, $"state_economy_sanctions_productreset|{targetCountryId}|{resourceKey}|export")),
                CreateRow((imp25, $"state_economy_sanctions_producteff|{targetCountryId}|{resourceKey}|import|25"), (imp50, $"state_economy_sanctions_producteff|{targetCountryId}|{resourceKey}|import|50"), (imp75, $"state_economy_sanctions_producteff|{targetCountryId}|{resourceKey}|import|75")),
                CreateRow((imp99, $"state_economy_sanctions_producteff|{targetCountryId}|{resourceKey}|import|99"), ("✏ Ввести %", $"state_economy_sanctions_producteff_input|{targetCountryId}|{resourceKey}|import"), (importReset, $"state_economy_sanctions_productreset|{targetCountryId}|{resourceKey}|import")),
                CreateRow(("🔙 Назад", "state_economy_sanctions_products"))
            });
        }

        public static InlineKeyboardMarkup EconomyMoneyLogKeyboard(int page, int totalPages)
        {
            var rows = new List<InlineKeyboardButton[]>();
            if (totalPages > 1)
            {
                var nav = new List<(string text, string data)>();
                if (page > 0)
                    nav.Add(("⬅ Попередні", $"state_economy_moneylog_page_{page - 1}"));
                if (page < totalPages - 1)
                    nav.Add(("➡ Наступні", $"state_economy_moneylog_page_{page + 1}"));
                if (nav.Count > 0)
                    rows.Add(CreateRow(nav.ToArray()));
            }
            rows.Add(CreateRow(("💰 Економіка", "state_economy")));
            return new InlineKeyboardMarkup(rows);
        }

        public static InlineKeyboardMarkup EconomyBuildingsKeyboard(bool isMaster)
        {
            var rows = new List<InlineKeyboardButton[]>
            {
                CreateRow(("📄 Список будівель", "state_economy_buildings_list"), ("🛡 Військові будівлі", "state_economy_buildings_list_military")),
                CreateRow(("⏳ У будівництві", "state_economy_buildings_under_construction")),
                CreateRow(("🏛 Цивільні шаблони", "state_economy_building_templates"), ("🛡 Військові шаблони", "state_economy_building_templates_military")),
                CreateRow(("🏙 Міста", "state_economy_cities"))
            };
            rows.Add(CreateRow(("➕ Додати будівлю", "state_economy_building_add")));
            rows.Add(CreateRow(("💰 Економіка", "state_economy")));
            return new InlineKeyboardMarkup(rows);
        }

        public static InlineKeyboardMarkup EconomyFestivalsKeyboard(bool isMaster)
        {
            var rows = new List<InlineKeyboardButton[]>
            {
                CreateRow(("📄 Список фестивалів", "state_economy_festivals_list"))
            };
            rows.Add(CreateRow(("➕ Додати фестиваль", "state_economy_festival_add")));
            rows.Add(CreateRow(("💰 Економіка", "state_economy")));
            return new InlineKeyboardMarkup(rows);
        }

        public static InlineKeyboardMarkup EconomyBuildingsListKeyboard(List<(string text, string data)> items, int page, int totalPages, bool isMaster)
        {
            var rows = new List<InlineKeyboardButton[]>();

            foreach (var item in items)
                rows.Add(CreateRow((item.text, item.data)));

            if (totalPages > 1)
            {
                var nav = new List<(string text, string data)>();
                if (page > 0)
                    nav.Add(("⬅ Попередні", $"state_economy_buildings_page_{page - 1}"));
                if (page < totalPages - 1)
                    nav.Add(("➡ Наступні", $"state_economy_buildings_page_{page + 1}"));
                if (nav.Count > 0)
                    rows.Add(CreateRow(nav.ToArray()));
            }

            rows.Add(CreateRow(("➕ Додати будівлю", "state_economy_building_add")));
            rows.Add(CreateRow(("🔙 Назад", "state_economy_buildings")));
            return new InlineKeyboardMarkup(rows);
        }

        public static InlineKeyboardMarkup EconomyBuildingsMilitaryListKeyboard(List<(string text, string data)> items, int page, int totalPages, bool isMaster)
        {
            var rows = new List<InlineKeyboardButton[]>();

            foreach (var item in items)
                rows.Add(CreateRow((item.text, item.data)));

            if (totalPages > 1)
            {
                var nav = new List<(string text, string data)>();
                if (page > 0)
                    nav.Add(("⬅ Попередні", $"state_economy_buildings_military_page_{page - 1}"));
                if (page < totalPages - 1)
                    nav.Add(("➡ Наступні", $"state_economy_buildings_military_page_{page + 1}"));
                if (nav.Count > 0)
                    rows.Add(CreateRow(nav.ToArray()));
            }

            rows.Add(CreateRow(("➕ Додати будівлю", "state_economy_building_add"), ("📄 Усі будівлі", "state_economy_buildings_list")));
            rows.Add(CreateRow(("🔙 Назад", "state_economy_buildings")));
            return new InlineKeyboardMarkup(rows);
        }

        public static InlineKeyboardMarkup EconomyBuildingsUnderConstructionKeyboard(List<(string text, string data)> items, int page, int totalPages)
        {
            var rows = new List<InlineKeyboardButton[]>();

            foreach (var item in items)
                rows.Add(CreateRow((item.text, item.data)));

            if (totalPages > 1)
            {
                var nav = new List<(string text, string data)>();
                if (page > 0)
                    nav.Add(("⬅ Попередні", $"state_economy_buildings_under_construction_page_{page - 1}"));
                if (page < totalPages - 1)
                    nav.Add(("➡ Наступні", $"state_economy_buildings_under_construction_page_{page + 1}"));
                if (nav.Count > 0)
                    rows.Add(CreateRow(nav.ToArray()));
            }

            rows.Add(CreateRow(("📄 Усі будівлі", "state_economy_buildings_list")));
            rows.Add(CreateRow(("🔙 Назад", "state_economy_buildings")));
            return new InlineKeyboardMarkup(rows);
        }

        public static InlineKeyboardMarkup EconomyBuildingInfoKeyboard(int buildingId, bool isMaster)
        {
            var rows = new List<InlineKeyboardButton[]>();
            rows.Add(CreateRow(
                ("👁 Прев'ю рівнів", $"state_economy_building_upgrade_preview_{buildingId}"),
                ("⬆ Покращити", $"state_economy_building_upgrade_{buildingId}")));
            rows.Add(CreateRow(("✏ Редагувати", $"state_economy_building_edit_{buildingId}"), ("🗑 Видалити", $"state_economy_building_delete_{buildingId}")));
            rows.Add(CreateRow(("⚙ Ефективність: перемкнути", $"state_economy_building_tgeff_{buildingId}")));
            rows.Add(CreateRow(("🔙 Назад", "state_economy_buildings_list")));
            return new InlineKeyboardMarkup(rows);
        }

        public static InlineKeyboardMarkup EconomyFestivalsListKeyboard(List<(string text, string data)> items, int page, int totalPages, bool isMaster)
        {
            var rows = new List<InlineKeyboardButton[]>();
            foreach (var item in items)
                rows.Add(CreateRow((item.text, item.data)));

            if (totalPages > 1)
            {
                var nav = new List<(string text, string data)>();
                if (page > 0)
                    nav.Add(("⬅ Попередні", $"state_economy_festivals_page_{page - 1}"));
                if (page < totalPages - 1)
                    nav.Add(("➡ Наступні", $"state_economy_festivals_page_{page + 1}"));
                if (nav.Count > 0)
                    rows.Add(CreateRow(nav.ToArray()));
            }

            rows.Add(CreateRow(("➕ Додати фестиваль", "state_economy_festival_add")));
            rows.Add(CreateRow(("🔙 Назад", "state_economy_festivals")));
            return new InlineKeyboardMarkup(rows);
        }

        public static InlineKeyboardMarkup EconomyFestivalInfoKeyboard(int festivalId, bool isMaster)
        {
            var rows = new List<InlineKeyboardButton[]>();
            rows.Add(CreateRow(("✏ Редагувати", $"state_economy_festival_edit_{festivalId}"), ("⛔ Завершити", $"state_economy_festival_end_{festivalId}")));
            rows.Add(CreateRow(("🔙 Назад", "state_economy_festivals_list")));
            return new InlineKeyboardMarkup(rows);
        }

        public static InlineKeyboardMarkup EconomyBuildingTemplatesKeyboard(List<(string text, string data)> items, int page, int totalPages, bool militaryOnly = false)
        {
            var rows = new List<InlineKeyboardButton[]>();

            if (items != null && items.Count > 0)
            {
                for (int i = 0; i < items.Count; i += 2)
                {
                    var slice = items.Skip(i).Take(2).ToArray();
                    rows.Add(CreateRow(slice));
                }
            }

            if (totalPages > 1)
            {
                var nav = new List<(string text, string data)>();
                if (page > 0)
                    nav.Add(("⬅ Назад", militaryOnly
                        ? $"state_economy_building_templates_military_page_{page - 1}"
                        : $"state_economy_building_templates_page_{page - 1}"));
                nav.Add(($"📖 {page + 1}/{totalPages}", militaryOnly
                    ? $"state_economy_building_templates_military_page_{page}"
                    : $"state_economy_building_templates_page_{page}"));
                if (page < totalPages - 1)
                    nav.Add(("➡ Далі", militaryOnly
                        ? $"state_economy_building_templates_military_page_{page + 1}"
                        : $"state_economy_building_templates_page_{page + 1}"));
                rows.Add(CreateRow(nav.ToArray()));
            }

            rows.Add(CreateRow(militaryOnly
                ? ("🏛 Цивільні шаблони", "state_economy_building_templates")
                : ("🛡 Військові шаблони", "state_economy_building_templates_military")));
            rows.Add(CreateRow(("🔙 Назад", "state_economy_buildings")));
            return new InlineKeyboardMarkup(rows);
        }

        public static InlineKeyboardMarkup EconomyBuildingTemplatePreviewKeyboard(
            string templateKey,
            string prevTemplateKey,
            string nextTemplateKey,
            int returnPage,
            bool militaryOnly = false)
        {
            string safeKey = string.IsNullOrWhiteSpace(templateKey)
                ? "housing"
                : templateKey.Trim().ToLowerInvariant();
            int safePage = Math.Max(0, returnPage);

            var rows = new List<InlineKeyboardButton[]>
            {
                CreateRow(
                    ("👁 Предосмотр", $"state_economy_building_template_preview|{safeKey}|1"),
                    ("✅ Використати шаблон", $"state_economy_building_template_use_{safeKey}"))
            };

            var nav = new List<(string text, string data)>();
            if (!string.IsNullOrWhiteSpace(prevTemplateKey))
                nav.Add(("⬅ Попередній", $"state_economy_building_template_{prevTemplateKey.Trim().ToLowerInvariant()}"));
            if (!string.IsNullOrWhiteSpace(nextTemplateKey))
                nav.Add(("➡ Наступний", $"state_economy_building_template_{nextTemplateKey.Trim().ToLowerInvariant()}"));
            if (nav.Count > 0)
                rows.Add(CreateRow(nav.ToArray()));

            rows.Add(CreateRow(
                ("📚 До каталогу", militaryOnly
                    ? $"state_economy_building_templates_military_page_{safePage}"
                    : $"state_economy_building_templates_page_{safePage}"),
                ("🏗 Будівлі", "state_economy_buildings")));
            return new InlineKeyboardMarkup(rows);
        }

        public static InlineKeyboardMarkup EconomyBuildingTemplateLevelPreviewKeyboard(string templateKey, int level, int maxLevel)
        {
            string safeKey = string.IsNullOrWhiteSpace(templateKey)
                ? "housing"
                : templateKey.Trim().ToLowerInvariant();
            int safeMax = Math.Max(1, maxLevel);
            int safeLevel = Math.Clamp(level, 1, safeMax);

            var rows = new List<InlineKeyboardButton[]>();
            var nav = new List<(string text, string data)>();
            if (safeLevel > 1)
                nav.Add(("⬅ Назад", $"state_economy_building_template_preview|{safeKey}|{safeLevel - 1}"));
            nav.Add(($"📖 L{safeLevel}/{safeMax}", $"state_economy_building_template_preview|{safeKey}|{safeLevel}"));
            if (safeLevel < safeMax)
                nav.Add(("➡ Далі", $"state_economy_building_template_preview|{safeKey}|{safeLevel + 1}"));
            rows.Add(CreateRow(nav.ToArray()));

            rows.Add(CreateRow(("✅ Використати шаблон", $"state_economy_building_template_use_{safeKey}")));
            rows.Add(CreateRow(("↩ До шаблону", $"state_economy_building_template_{safeKey}")));
            return new InlineKeyboardMarkup(rows);
        }

        public static InlineKeyboardMarkup EconomyBuildingEditKeyboard(int buildingId)
        {
            return new InlineKeyboardMarkup(new[]
            {
                CreateRow(("Назва", $"state_economy_building_editfield_{buildingId}_Type"), ("Категорія", $"state_economy_building_editfield_{buildingId}_Category")),
                CreateRow(("Рівень", $"state_economy_building_editfield_{buildingId}_Level"), ("Робочі місця", $"state_economy_building_editfield_{buildingId}_JobsProvided")),
                CreateRow(("Утримання", $"state_economy_building_editfield_{buildingId}_UpkeepCost"), ("Базове виробн.", $"state_economy_building_editfield_{buildingId}_BaseProduction")),
                CreateRow(("Вартість буд.", $"state_economy_building_editfield_{buildingId}_BuildCost"), ("Тривалість", $"state_economy_building_editfield_{buildingId}_BuildTurns")),
                CreateRow(("Вихід ресурс", $"state_economy_building_editfield_{buildingId}_ResourceType"), ("Вхід ресурс", $"state_economy_building_editfield_{buildingId}_InputResourceType")),
                CreateRow(("Споживання/день", $"state_economy_building_editfield_{buildingId}_InputAmount"), ("Держ. власн.", $"state_economy_building_editfield_{buildingId}_StateOwned")),
                CreateRow(("Стабільність", $"state_economy_building_editfield_{buildingId}_StabilityBonus"), ("Антикорупц.", $"state_economy_building_editfield_{buildingId}_CorruptionReduction")),
                CreateRow(("Наука", $"state_economy_building_editfield_{buildingId}_ResearchBonus"), ("Армія", $"state_economy_building_editfield_{buildingId}_ArmyBonus")),
                CreateRow(("🔙 Назад", $"state_economy_buildinginfo_{buildingId}"))
            });
        }

        public static InlineKeyboardMarkup EconomyFestivalEditKeyboard(int festivalId)
        {
            return new InlineKeyboardMarkup(new[]
            {
                CreateRow(("Назва", $"state_economy_festival_editfield_{festivalId}_Name"), ("Тривалість", $"state_economy_festival_editfield_{festivalId}_DurationTurns")),
                CreateRow(("Залишилось", $"state_economy_festival_editfield_{festivalId}_RemainingTurns"), ("Вартість", $"state_economy_festival_editfield_{festivalId}_Cost")),
                CreateRow(("Стабільність", $"state_economy_festival_editfield_{festivalId}_StabilityBonus")),
                CreateRow(("Активний", $"state_economy_festival_editfield_{festivalId}_IsActive")),
                CreateRow(("🔙 Назад", $"state_economy_festivalinfo_{festivalId}"))
            });
        }

        public static InlineKeyboardMarkup EconomyTradeMenuKeyboard(bool isMaster)
        {
            var rows = new List<InlineKeyboardButton[]>
            {
                CreateRow(("📄 Список угод", "state_economy_trade_list"))
            };
            rows.Add(CreateRow(("➕ Додати угоду", "state_economy_trade_add")));
            if (isMaster)
            {
                rows.Add(CreateRow(("⏳ Запити на схвалення", "state_economy_trade_pending")));
                rows.Add(CreateRow(("🔁 Синхронізувати пари", "state_economy_trade_sync_pairs")));
            }
            rows.Add(CreateRow(("💰 Економіка", "state_economy")));
            return new InlineKeyboardMarkup(rows);
        }

        public static InlineKeyboardMarkup EconomyTradePartnerKeyboard(List<CountryRecord> countries, int page, int totalPages)
        {
            var rows = new List<InlineKeyboardButton[]>();
            foreach (var c in countries)
            {
                string flag = string.IsNullOrWhiteSpace(c.FlagEmoji) ? "" : $"{c.FlagEmoji} ";
                string name = string.IsNullOrWhiteSpace(c.Name) ? $"#{c.CountryId}" : c.Name;
                rows.Add(CreateRow(($"{flag}{name}", $"state_economy_trade_partner_{c.CountryId}")));
            }

            if (totalPages > 1)
            {
                var nav = new List<(string text, string data)>();
                if (page > 0)
                    nav.Add(("⬅ Попередні", $"state_economy_trade_partner_page_{page - 1}"));
                if (page < totalPages - 1)
                    nav.Add(("➡ Наступні", $"state_economy_trade_partner_page_{page + 1}"));
                if (nav.Count > 0)
                    rows.Add(CreateRow(nav.ToArray()));
            }
            rows.Add(CreateRow(("⏭ Пропустити", "state_economy_trade_partner_skip")));
            rows.Add(CreateRow(("🔙 Назад", "state_economy_trade")));
            return new InlineKeyboardMarkup(rows);
        }

        public static InlineKeyboardMarkup EconomyTradeFlowModeKeyboard()
        {
            return new InlineKeyboardMarkup(new[]
            {
                CreateRow(("📤 Лише експорт", "state_economy_trade_mode_export")),
                CreateRow(("📥 Лише імпорт", "state_economy_trade_mode_import")),
                CreateRow(("🔄 Експорт і імпорт", "state_economy_trade_mode_both")),
                CreateRow(("🔙 Назад", "state_economy_trade"))
            });
        }

        public static InlineKeyboardMarkup EconomyTradeResourceKeyboard(List<EconomyResourceRecord> resources, int page, int totalPages, string kind)
        {
            var rows = new List<InlineKeyboardButton[]>();
            foreach (var r in resources)
                rows.Add(CreateRow(($"{FormatResourceTypeUk(r.ResourceType)} ({r.BasePrice:0.##})", $"state_economy_trade_res_{kind}_{r.ResourceType}")));

            if (totalPages > 1)
            {
                var nav = new List<(string text, string data)>();
                if (page > 0)
                    nav.Add(("⬅ Попередні", $"state_economy_trade_res_{kind}_page_{page - 1}"));
                if (page < totalPages - 1)
                    nav.Add(("➡ Наступні", $"state_economy_trade_res_{kind}_page_{page + 1}"));
                if (nav.Count > 0)
                    rows.Add(CreateRow(nav.ToArray()));
            }
            rows.Add(CreateRow(("⏭ Пропустити", $"state_economy_trade_res_{kind}_skip")));
            rows.Add(CreateRow(("🔙 Назад", "state_economy_trade")));
            return new InlineKeyboardMarkup(rows);
        }

        public static InlineKeyboardMarkup EconomyBuildingResourceKeyboard(List<EconomyResourceRecord> resources, int page, int totalPages)
        {
            var rows = new List<InlineKeyboardButton[]>();
            foreach (var r in resources)
                rows.Add(CreateRow(($"{FormatResourceTypeUk(r.ResourceType)} ({r.BasePrice:0.##})", $"state_economy_building_res_{r.ResourceType}")));

            if (totalPages > 1)
            {
                var nav = new List<(string text, string data)>();
                if (page > 0)
                    nav.Add(("⬅ Попередні", $"state_economy_building_res_page_{page - 1}"));
                if (page < totalPages - 1)
                    nav.Add(("➡ Наступні", $"state_economy_building_res_page_{page + 1}"));
                if (nav.Count > 0)
                    rows.Add(CreateRow(nav.ToArray()));
            }
            rows.Add(CreateRow(("⏭ Пропустити", "state_economy_building_res_skip")));
            rows.Add(CreateRow(("🔙 Назад", "state_economy_buildings")));
            return new InlineKeyboardMarkup(rows);
        }

        public static InlineKeyboardMarkup EconomyBuildingCityKeyboard(List<(string text, string data)> items, int page, int totalPages)
        {
            var rows = new List<InlineKeyboardButton[]>();
            foreach (var item in items)
                rows.Add(CreateRow((item.text, item.data)));

            if (totalPages > 1)
            {
                var nav = new List<(string text, string data)>();
                if (page > 0)
                    nav.Add(("⬅ Попередні", $"state_economy_building_city_page_{page - 1}"));
                if (page < totalPages - 1)
                    nav.Add(("➡ Наступні", $"state_economy_building_city_page_{page + 1}"));
                if (nav.Count > 0)
                    rows.Add(CreateRow(nav.ToArray()));
            }

            rows.Add(CreateRow(("➕ Створити нове місто", "state_economy_building_city_new")));
            rows.Add(CreateRow(("❌ Скасувати", "state_economy_building_city_cancel")));
            return new InlineKeyboardMarkup(rows);
        }

        public static InlineKeyboardMarkup EconomyCitiesListKeyboard(List<(string text, string data)> items, int page, int totalPages)
        {
            var rows = new List<InlineKeyboardButton[]>();
            foreach (var item in items)
                rows.Add(CreateRow((item.text, item.data)));

            if (totalPages > 1)
            {
                var nav = new List<(string text, string data)>();
                if (page > 0)
                    nav.Add(("⬅ Попередні", $"state_economy_cities_page_{page - 1}"));
                if (page < totalPages - 1)
                    nav.Add(("➡ Наступні", $"state_economy_cities_page_{page + 1}"));
                if (nav.Count > 0)
                    rows.Add(CreateRow(nav.ToArray()));
            }

            rows.Add(CreateRow(("➕ Створити місто", "state_economy_city_create")));
            rows.Add(CreateRow(("🧨 Зруйнувати місто", "state_economy_city_destroy_select")));
            rows.Add(CreateRow(("🔙 Назад", "state_economy_buildings")));
            return new InlineKeyboardMarkup(rows);
        }

        public static InlineKeyboardMarkup EconomyCitiesDestroyKeyboard(List<(string text, string data)> items, int page, int totalPages)
        {
            var rows = new List<InlineKeyboardButton[]>();
            foreach (var item in items)
                rows.Add(CreateRow((item.text, item.data)));

            if (totalPages > 1)
            {
                var nav = new List<(string text, string data)>();
                if (page > 0)
                    nav.Add(("⬅ Попередні", $"state_economy_city_destroy_page_{page - 1}"));
                if (page < totalPages - 1)
                    nav.Add(("➡ Наступні", $"state_economy_city_destroy_page_{page + 1}"));
                if (nav.Count > 0)
                    rows.Add(CreateRow(nav.ToArray()));
            }

            rows.Add(CreateRow(("🔙 До міст", "state_economy_cities")));
            return new InlineKeyboardMarkup(rows);
        }

        public static InlineKeyboardMarkup EconomyCityInfoKeyboard(int cityId, bool canDelete)
        {
            var rows = new List<InlineKeyboardButton[]>();
            if (canDelete)
                rows.Add(CreateRow(("🧨 Зруйнувати місто", $"state_economy_city_delete_{cityId}")));
            rows.Add(CreateRow(("🔙 До міст", "state_economy_cities")));
            return new InlineKeyboardMarkup(rows);
        }

        public static InlineKeyboardMarkup EconomyCityDeleteConfirmKeyboard(int cityId)
        {
            return new InlineKeyboardMarkup(new[]
            {
                CreateRow(("✅ Підтвердити", $"state_economy_city_deleteconfirm_{cityId}"), ("❌ Скасувати", "state_economy_cities"))
            });
        }

        public static InlineKeyboardMarkup EconomyResourceParentKeyboard(List<EconomyResourceRecord> resources, int page, int totalPages)
        {
            var rows = new List<InlineKeyboardButton[]>();
            foreach (var r in resources)
                rows.Add(CreateRow(($"{FormatResourceTypeUk(r.ResourceType)} ({r.BasePrice:0.##})", $"state_economy_resource_parent_pick|{EncodeResourceKey(r.ResourceType)}")));

            if (totalPages > 1)
            {
                var nav = new List<(string text, string data)>();
                if (page > 0)
                    nav.Add(("⬅ Попередні", $"state_economy_resource_parent_page_{page - 1}"));
                if (page < totalPages - 1)
                    nav.Add(("➡ Наступні", $"state_economy_resource_parent_page_{page + 1}"));
                if (nav.Count > 0)
                    rows.Add(CreateRow(nav.ToArray()));
            }

            rows.Add(CreateRow(("⏭ Без батьк. типу", "state_economy_resource_parent_none")));
            rows.Add(CreateRow(("✏ Ввести вручну", "state_economy_resource_parent_manual")));
            rows.Add(CreateRow(("❌ Скасувати", "state_inlinepick_cancel")));
            return new InlineKeyboardMarkup(rows);
        }

        public static InlineKeyboardMarkup EconomyResourcesKeyboard()
        {
            return new InlineKeyboardMarkup(new[]
            {
                CreateRow(("📄 Список ресурсів", "state_economy_resources_list"), ("🛡 Військові ресурси", "state_economy_resources_list_military")),
                CreateRow(("➕ Додати ресурс", "state_economy_resource_add")),
                CreateRow(("💰 Економіка", "state_economy"))
            });
        }

        public static InlineKeyboardMarkup EconomyResourcesListKeyboard(List<(string text, string data)> items, int page, int totalPages)
        {
            var rows = new List<InlineKeyboardButton[]>();
            foreach (var item in items)
                rows.Add(CreateRow((item.text, item.data)));

            if (totalPages > 1)
            {
                var nav = new List<(string text, string data)>();
                if (page > 0)
                    nav.Add(("⬅ Попередні", $"state_economy_resources_page_{page - 1}"));
                if (page < totalPages - 1)
                    nav.Add(("➡ Наступні", $"state_economy_resources_page_{page + 1}"));
                if (nav.Count > 0)
                    rows.Add(CreateRow(nav.ToArray()));
            }

            rows.Add(CreateRow(("🔙 Назад", "state_economy_resources")));
            return new InlineKeyboardMarkup(rows);
        }

        public static InlineKeyboardMarkup EconomyResourcesMilitaryListKeyboard(List<(string text, string data)> items, int page, int totalPages)
        {
            var rows = new List<InlineKeyboardButton[]>();
            foreach (var item in items)
                rows.Add(CreateRow((item.text, item.data)));

            if (totalPages > 1)
            {
                var nav = new List<(string text, string data)>();
                if (page > 0)
                    nav.Add(("⬅ Попередні", $"state_economy_resources_military_page_{page - 1}"));
                if (page < totalPages - 1)
                    nav.Add(("➡ Наступні", $"state_economy_resources_military_page_{page + 1}"));
                if (nav.Count > 0)
                    rows.Add(CreateRow(nav.ToArray()));
            }

            rows.Add(CreateRow(("📄 Усі ресурси", "state_economy_resources_list")));
            rows.Add(CreateRow(("🔙 Назад", "state_economy_resources")));
            return new InlineKeyboardMarkup(rows);
        }

        public static InlineKeyboardMarkup EconomyResourceInfoKeyboard(string resourceKey)
        {
            return new InlineKeyboardMarkup(new[]
            {
                CreateRow(("✏ Редагувати", $"state_economy_resource_edit|{resourceKey}"), ("🗑 Видалити", $"state_economy_resource_delete|{resourceKey}")),
                CreateRow(("⚖ Змінити попит", $"state_economy_resource_editfield|{resourceKey}|DemandFactor")),
                CreateRow(("⚙ Ефективність: перемкнути", $"state_economy_resource_tgeff|{resourceKey}")),
                CreateRow(("🔙 Назад", "state_economy_resources_list"))
            });
        }

        public static InlineKeyboardMarkup EconomyResourceEditKeyboard(string resourceKey)
        {
            return new InlineKeyboardMarkup(new[]
            {
                CreateRow(("Тип", $"state_economy_resource_editfield|{resourceKey}|ResourceType"), ("Батьк. тип", $"state_economy_resource_editfield|{resourceKey}|ParentResourceType")),
                CreateRow(("Баз. ціна", $"state_economy_resource_editfield|{resourceKey}|BasePrice"), ("Торгівля", $"state_economy_resource_editfield|{resourceKey}|IsTradeable")),
                CreateRow(("Попит", $"state_economy_resource_editfield|{resourceKey}|DemandFactor")),
                CreateRow(("🔙 Назад", $"state_economy_resourceinfo|{resourceKey}"))
            });
        }

        public static InlineKeyboardMarkup EconomyResourceDeleteConfirmKeyboard(string resourceKey)
        {
            return new InlineKeyboardMarkup(new[]
            {
                CreateRow(("✅ Підтвердити", $"state_economy_resource_deleteconfirm|{resourceKey}"), ("❌ Скасувати", $"state_economy_resourceinfo|{resourceKey}"))
            });
        }

        public static InlineKeyboardMarkup EconomyTradeListKeyboard(List<(string text, string data)> items, int page, int totalPages, bool isMaster)
        {
            var rows = new List<InlineKeyboardButton[]>();
            foreach (var item in items)
                rows.Add(CreateRow((item.text, item.data)));

            if (totalPages > 1)
            {
                var nav = new List<(string text, string data)>();
                if (page > 0)
                    nav.Add(("⬅ Попередні", $"state_economy_trade_page_{page - 1}"));
                if (page < totalPages - 1)
                    nav.Add(("➡ Наступні", $"state_economy_trade_page_{page + 1}"));
                if (nav.Count > 0)
                    rows.Add(CreateRow(nav.ToArray()));
            }

            rows.Add(CreateRow(("➕ Додати угоду", "state_economy_trade_add")));
            rows.Add(CreateRow(("🔙 Назад", "state_economy_trade")));
            return new InlineKeyboardMarkup(rows);
        }

        public static InlineKeyboardMarkup EconomyTradeInfoKeyboard(int agreementId, bool isMaster)
        {
            var rows = new List<InlineKeyboardButton[]>();
            rows.Add(CreateRow(("✏ Редагувати", $"state_economy_trade_edit_{agreementId}"), ("⛔ Завершити", $"state_economy_trade_end_{agreementId}")));
            rows.Add(CreateRow(("🔙 Назад", "state_economy_trade_list")));
            return new InlineKeyboardMarkup(rows);
        }

        public static InlineKeyboardMarkup EconomyTradePendingListKeyboard(List<(string text, string data)> items, int page, int totalPages)
        {
            var rows = new List<InlineKeyboardButton[]>();
            foreach (var item in items)
                rows.Add(CreateRow((item.text, item.data)));

            if (totalPages > 1)
            {
                var nav = new List<(string text, string data)>();
                if (page > 0)
                    nav.Add(("⬅ Попередні", $"state_economy_trade_pending_page_{page - 1}"));
                if (page < totalPages - 1)
                    nav.Add(("➡ Наступні", $"state_economy_trade_pending_page_{page + 1}"));
                if (nav.Count > 0)
                    rows.Add(CreateRow(nav.ToArray()));
            }

            rows.Add(CreateRow(("🔙 Назад", "state_economy_trade")));
            return new InlineKeyboardMarkup(rows);
        }

        public static InlineKeyboardMarkup EconomyTradePendingInfoKeyboard(long requestId)
        {
            return new InlineKeyboardMarkup(new[]
            {
                CreateRow(("✅ Схвалити", $"state_economy_tradeapprove_{requestId}_yes"),
                          ("❌ Відхилити", $"state_economy_tradeapprove_{requestId}_no")),
                CreateRow(("🔙 Назад", "state_economy_trade_pending"))
            });
        }

        public static InlineKeyboardMarkup EconomyTradeEditKeyboard(int agreementId)
        {
            return new InlineKeyboardMarkup(new[]
            {
                CreateRow(("Партнер", $"state_economy_trade_editfield_{agreementId}_PartnerName"), ("Активна", $"state_economy_trade_editfield_{agreementId}_IsActive")),
                CreateRow(("Експорт ресурс", $"state_economy_trade_editfield_{agreementId}_ExportResourceType"), ("Імпорт ресурс", $"state_economy_trade_editfield_{agreementId}_ImportResourceType")),
                CreateRow(("Формат к-ті", $"state_economy_trade_editfield_{agreementId}_QuantityIsPerDay"), ("Час доставки (дн)", $"state_economy_trade_editfield_{agreementId}_PeriodDays")),
                CreateRow(("Тип доставки", $"state_economy_trade_editfield_{agreementId}_DeliveryMode")),
                CreateRow(("Експорт к-ть", $"state_economy_trade_editfield_{agreementId}_ExportQty"), ("Експорт ціна", $"state_economy_trade_editfield_{agreementId}_ExportUnitPrice")),
                CreateRow(("Імпорт к-ть", $"state_economy_trade_editfield_{agreementId}_ImportQty"), ("Імпорт ціна", $"state_economy_trade_editfield_{agreementId}_ImportUnitPrice")),
                CreateRow(("🔙 Назад", $"state_economy_tradeinfo_{agreementId}"))
            });
        }


        public static InlineKeyboardMarkup ParliamentStateKeyboard()
        {
            return new InlineKeyboardMarkup(new[]
            {
                CreateRow(("📜 Активні закони", "stateparliament_activelaw"), ("📖 Розглядання", "stateparliament_review")),
                CreateRow(("🗳 Голосування", "stateparliament_voting"), ("➕ Запропонувати закон", "stateparliament_proposelaw")),
                CreateRow(("📦 Готові закони", "stateparliament_lawtemplate_tiers"), ("🎙 Промова", "stateparliament_speech")),
                CreateRow(("🏛 Фракції / партії", "stateparliament_faction"), ("👥 Депутати", "stateparliament_deputies")),
                CreateRow(("📣 Скликати парламент", "stateparliament_conveneparl"), ("🚪 Закрити сесію", "stateparliament_closesession")),
                CreateRow(("🔙 Назад", "stateparliament_cancel"))
            });
        }

        public static InlineKeyboardMarkup ParliamentLockedKeyboard()
        {
            return new InlineKeyboardMarkup(new[]
            {
                CreateRow(("🔙 Назад", "stateparliament_cancel"))
            });
        }

        public static InlineKeyboardMarkup ParliamentLawTypeKeyboard()
        {
            return new InlineKeyboardMarkup(new[]
            {
                CreateRow(("💰 Економіка", "stateparliament_lawtype_Economy"), ("🛡 Армія", "stateparliament_lawtype_Army")),
                CreateRow(("✨ Магія", "stateparliament_lawtype_Magic"), ("🤝 Дипломатія", "stateparliament_lawtype_Diplomacy")),
                CreateRow(("⚖️ Право", "stateparliament_lawtype_Law"), ("📈 Стабільність", "stateparliament_lawtype_Stability")),
                CreateRow(("💸 Податок на брутто %", "stateparliament_lawtype_GrossIncomeTaxPercent"), ("💳 Податок на нетто %", "stateparliament_lawtype_NetIncomeTaxPercent")),
                CreateRow(("🏷 Соцвнесок %", "stateparliament_lawtype_SocialContributionPercent")),
                CreateRow(("🪖 ЗП військовим", "stateparliament_lawtype_SetSoldierSalary"), ("🏛 ЗП депутатам", "stateparliament_lawtype_SetDeputySalary")),
                CreateRow(("👥 ЗП людям", "stateparliament_lawtype_SetCivilianSalary"), ("📝 Перейменування", "stateparliament_lawtype_RenameCountry")),
                CreateRow(("👴 Пенсія / особа", "stateparliament_lawtype_SetPensionPayment"), ("♿ Соцвиплата / особа", "stateparliament_lawtype_SetSocialSupportPayment")),
                CreateRow(("💰 Додати в казну", "stateparliament_lawtype_AddTreasury"), ("💸 Списати з казни", "stateparliament_lawtype_RemoveTreasury")),
                CreateRow(("💰 Казна +/день", "stateparliament_lawtype_AddTreasuryDaily"), ("💸 Казна -/день", "stateparliament_lawtype_RemoveTreasuryDaily")),
                CreateRow(("📥 Ліміт імпорту %", "stateparliament_lawtype_LimitImportPercent"), ("📤 Ліміт експорту %", "stateparliament_lawtype_LimitExportPercent")),
                CreateRow(("🏭 Ліміт спожив. комп. %", "stateparliament_lawtype_LimitCompanyResourceUsePercent")),
                CreateRow(("🏥 МОЗ (закон)", "stateparliament_lawtype_LAW_HEALTH_MINISTRY"), ("🧼 Санітарія (закон)", "stateparliament_lawtype_LAW_PUBLIC_SANITATION")),
                CreateRow(("🚧 Карантин (закон)", "stateparliament_lawtype_LAW_QUARANTINE_PROTOCOLS"), ("💉 Імунізація (закон)", "stateparliament_lawtype_LAW_NATIONAL_IMMUNIZATION")),
                CreateRow(("🩺 Ліцензування лікарів", "stateparliament_lawtype_LAW_MEDICAL_LICENSING"), ("🏥 Резиденції лікарів", "stateparliament_lawtype_LAW_HOSPITAL_RESIDENCY_PROGRAM")),
                CreateRow(("🧬 Агенція розробки ліків", "stateparliament_lawtype_LAW_PHARMA_RESEARCH_AGENCY"), ("🧪 Клінічні протоколи", "stateparliament_lawtype_LAW_CLINICAL_TRIAL_PROTOCOLS")),
                CreateRow(("📦 Резерв ліків", "stateparliament_lawtype_LAW_EMERGENCY_MEDICINE_RESERVE")),
                CreateRow(("🎓 МОН (закон)", "stateparliament_lawtype_LAW_EDU_MINISTRY"), ("🏫 Базова освіта", "stateparliament_lawtype_LAW_BASIC_SCHOOLING")),
                CreateRow(("🏛 Вища освіта", "stateparliament_lawtype_LAW_HIGHER_EDU_CHARTER"), ("🎒 Стипендії", "stateparliament_lawtype_LAW_SCHOLARSHIP_PROGRAM")),
                CreateRow(("🌍 Міжнародна співпраця", "stateparliament_lawtype_LAW_INTL_COOPERATION"), ("🧾 Міжнар. епіддані", "stateparliament_lawtype_LAW_INTL_DATA_SHARING_HEALTH")),
                CreateRow(("🎓 Міжнар. академобмін", "stateparliament_lawtype_LAW_INTL_ACADEMIC_EXCHANGE"), ("🫶 Громадське здоров'я", "stateparliament_lawtype_LAW_COMMUNITY_HEALTH_OUTREACH")),
                CreateRow(("👩‍🏫 Сертифікація педагогів", "stateparliament_lawtype_LAW_TEACHER_CERTIFICATION")),
                CreateRow(("⏭ Пропустити ефект", "stateparliament_lawtype_None"), ("✏ Ввести вручну", "stateparliament_lawtype_custom"))
            });
        }

        public static InlineKeyboardMarkup ParliamentLawParentKeyboard(List<LawRecord> laws, int page = 0, int perPage = 12)
        {
            var rows = new List<InlineKeyboardButton[]>
            {
                CreateRow(("🆕 Нова гілка (без батьківського)", "stateparliament_lawparent_none"))
            };

            if (laws != null && laws.Count > 0)
            {
                if (page < 0)
                    page = 0;
                if (perPage < 1)
                    perPage = 12;

                int total = laws.Count;
                int totalPages = (int)Math.Ceiling(total / (double)perPage);
                if (page >= totalPages)
                    page = Math.Max(0, totalPages - 1);

                var pageItems = laws.Skip(page * perPage).Take(perPage).ToList();
                foreach (var law in pageItems)
                {
                    string title = string.IsNullOrWhiteSpace(law.Title) ? $"#{law.LawId}" : law.Title;
                    rows.Add(CreateRow(($"📜 #{law.LawId} {title}", $"stateparliament_lawparent_pick_{law.LawId}")));
                }

                if (totalPages > 1)
                {
                    var navButtons = new List<(string text, string data)>();
                    if (page > 0)
                        navButtons.Add(("⬅ Попередні", $"stateparliament_lawparent_page_{page - 1}"));
                    if (page < totalPages - 1)
                        navButtons.Add(("➡ Наступні", $"stateparliament_lawparent_page_{page + 1}"));
                    if (navButtons.Count > 0)
                        rows.Add(CreateRow(navButtons.ToArray()));
                }
            }

            return new InlineKeyboardMarkup(rows);
        }

        public static InlineKeyboardMarkup ParliamentLawDateSkipKeyboard()
        {
            return new InlineKeyboardMarkup(new[]
            {
                CreateRow(("⏭ Пропустити дату", "stateparliament_lawdate_skip"))
            });
        }

        public static InlineKeyboardMarkup ParliamentReadyLawTemplateTierKeyboard()
        {
            return new InlineKeyboardMarkup(new[]
            {
                CreateRow(("🟢 Рівень I", "stateparliament_lawtemplate_tier_1_page_0"), ("🟡 Рівень II", "stateparliament_lawtemplate_tier_2_page_0")),
                CreateRow(("🟠 Рівень III", "stateparliament_lawtemplate_tier_3_page_0"), ("🔴 Рівень IV", "stateparliament_lawtemplate_tier_4_page_0")),
                CreateRow(("📚 Усі рівні", "stateparliament_lawtemplate_page_0")),
                CreateRow(("🔙 До парламенту", "stateparliament_back"))
            });
        }

        public static InlineKeyboardMarkup ParliamentReadyLawTemplateKeyboard(List<(string text, string data)> items, int? tier, int page, int totalPages)
        {
            var rows = new List<InlineKeyboardButton[]>();

            if (items != null)
            {
                foreach (var item in items)
                    rows.Add(CreateRow((item.text, item.data)));
            }

            if (totalPages > 1)
            {
                var nav = new List<(string text, string data)>();
                string prefix = tier.HasValue
                    ? $"stateparliament_lawtemplate_tier_{tier.Value}_page_"
                    : "stateparliament_lawtemplate_page_";
                if (page > 0)
                    nav.Add(("⬅ Попередні", $"{prefix}{page - 1}"));
                if (page < totalPages - 1)
                    nav.Add(("➡ Наступні", $"{prefix}{page + 1}"));
                if (nav.Count > 0)
                    rows.Add(CreateRow(nav.ToArray()));
            }

            rows.Add(CreateRow(("📚 Рівні", "stateparliament_lawtemplate_tiers")));
            rows.Add(CreateRow(("✍ Запропонувати вручну", "stateparliament_proposelaw")));
            rows.Add(CreateRow(("🔙 До парламенту", "stateparliament_back")));

            return new InlineKeyboardMarkup(rows);
        }

        public static InlineKeyboardMarkup ParliamentReadyLawTemplateKeyboard(List<(string text, string data)> items, int page, int totalPages)
        {
            return ParliamentReadyLawTemplateKeyboard(items, tier: null, page, totalPages);
        }

        public static InlineKeyboardMarkup LawReviewListKeyboard(List<LawRecord> laws, int page = 0, int perPage = 12)
        {
            var rows = new List<InlineKeyboardButton[]>();

            if (laws != null && laws.Count > 0)
            {
                if (page < 0)
                    page = 0;
                if (perPage < 1)
                    perPage = 12;

                int total = laws.Count;
                int totalPages = (int)Math.Ceiling(total / (double)perPage);
                if (page >= totalPages)
                    page = Math.Max(0, totalPages - 1);

                var pageItems = laws.Skip(page * perPage).Take(perPage).ToList();
                foreach (var law in pageItems)
                {
                    string title = string.IsNullOrWhiteSpace(law.Title) ? $"#{law.LawId}" : law.Title;
                    string stage = string.Equals(law.Status, "Review2", StringComparison.OrdinalIgnoreCase)
                        ? "2 чит."
                        : "1 чит.";
                    rows.Add(CreateRow(($"📖 {stage} {title}", $"stateparliament_review_show_{law.LawId}")));
                }

                if (totalPages > 1)
                {
                    var navButtons = new List<(string text, string data)>();
                    if (page > 0)
                        navButtons.Add(("⬅ Попередні", $"stateparliament_review_page_{page - 1}"));
                    if (page < totalPages - 1)
                        navButtons.Add(("➡ Наступні", $"stateparliament_review_page_{page + 1}"));
                    if (navButtons.Count > 0)
                        rows.Add(CreateRow(navButtons.ToArray()));
                }
            }
            else
            {
                rows.Add(CreateRow(("Немає законів", "stateparliament_review_empty")));
            }

            rows.Add(CreateRow(("🔙 Назад", "stateparliament_back")));
            return new InlineKeyboardMarkup(rows);
        }

        public static InlineKeyboardMarkup LawReviewInfoKeyboard(int lawId, int stage)
        {
            var rows = new List<InlineKeyboardButton[]>();
            string advanceText = stage <= 1 ? "📘 Провести 1 читання" : "📗 Завершити 2 читання";
            rows.Add(CreateRow((advanceText, $"stateparliament_review_advance_{lawId}")));
            rows.Add(CreateRow(("✏ Змінити назву", $"stateparliament_review_edit_title_{lawId}"), ("📝 Змінити опис", $"stateparliament_review_edit_desc_{lawId}")));
            rows.Add(CreateRow(("🎯 Змінити ефекти", $"stateparliament_review_edit_effect_{lawId}")));
            rows.Add(CreateRow(("❌ Відхилити на розгляді", $"stateparliament_review_reject_{lawId}")));
            rows.Add(CreateRow(("🔙 До розглядання", "stateparliament_review")));
            return new InlineKeyboardMarkup(rows);
        }

        public static InlineKeyboardMarkup LawReviewEffectEditKeyboard(
            List<(string text, string data)> items,
            List<(string text, string data)> valueItems,
            List<(string text, string data)> scopeItems,
            int lawId)
        {
            var rows = new List<InlineKeyboardButton[]>();

            if (items != null && items.Count > 0)
            {
                for (int i = 0; i < items.Count; i += 2)
                {
                    rows.Add(CreateRow(items.Skip(i).Take(2).ToArray()));
                }
            }

            if (valueItems != null && valueItems.Count > 0)
            {
                foreach (var valueItem in valueItems)
                    rows.Add(CreateRow(valueItem));
            }

            if (scopeItems != null && scopeItems.Count > 0)
            {
                foreach (var scopeItem in scopeItems)
                    rows.Add(CreateRow(scopeItem));
            }

            rows.Add(CreateRow(("🧹 Очистити", $"stateparliament_review_effect_clear_{lawId}"), ("✅ Застосувати", $"stateparliament_review_effect_apply_{lawId}")));
            rows.Add(CreateRow(("🔙 Скасувати", $"stateparliament_review_effect_cancel_{lawId}")));
            return new InlineKeyboardMarkup(rows);
        }

        public static InlineKeyboardMarkup ReviewToVotingKeyboard(int lawId)
        {
            return new InlineKeyboardMarkup(new[]
            {
                CreateRow(("🗳 Відкрити в голосуванні", $"stateparliament_votinglaw_{lawId}")),
                CreateRow(("📖 До розглядання", "stateparliament_review"), ("🔙 До парламенту", "stateparliament_back"))
            });
        }

        public static InlineKeyboardMarkup ParliamentSessionStartKeyboard()
        {
            return new InlineKeyboardMarkup(new[]
            {
                CreateRow(("✅ Всі депутати прийшли", "stateparliament_startsession_all"), ("🧮 Розрахувати кількість депутатів", "stateparliament_startsession_calc")),
                CreateRow(("❌ Скасувати", "stateparliament_startsession_cancel"))
            });
        }

        public static InlineKeyboardMarkup ParliamentSessionAttendanceActionsKeyboard(int sessionId)
        {
            return new InlineKeyboardMarkup(new[]
            {
                CreateRow(
                    ("✅ Подивитись хто прийшов", $"stateparliament_sessionattendance_present_{sessionId}_0"),
                    ("❌ Хто не прийшов", $"stateparliament_sessionattendance_absent_{sessionId}_0")
                ),
                CreateRow(("🔙 До парламенту", "stateparliament_back"))
            });
        }

        public static InlineKeyboardMarkup ParliamentSessionAttendanceListKeyboard(int sessionId, bool showPresent, int page, int totalPages)
        {
            var rows = new List<InlineKeyboardButton[]>();

            if (totalPages > 1)
            {
                var navButtons = new List<(string text, string data)>();

                if (page > 0)
                {
                    navButtons.Add(
                        showPresent
                            ? ("⬅ Попередні", $"stateparliament_sessionattendance_present_{sessionId}_{page - 1}")
                            : ("⬅ Попередні", $"stateparliament_sessionattendance_absent_{sessionId}_{page - 1}")
                    );
                }

                if (page < totalPages - 1)
                {
                    navButtons.Add(
                        showPresent
                            ? ("➡ Далі", $"stateparliament_sessionattendance_present_{sessionId}_{page + 1}")
                            : ("➡ Далі", $"stateparliament_sessionattendance_absent_{sessionId}_{page + 1}")
                    );
                }

                if (navButtons.Count > 0)
                    rows.Add(CreateRow(navButtons.ToArray()));
            }

            if (showPresent)
                rows.Add(CreateRow(("❌ Хто не прийшов", $"stateparliament_sessionattendance_absent_{sessionId}_0")));
            else
                rows.Add(CreateRow(("✅ Подивитись хто прийшов", $"stateparliament_sessionattendance_present_{sessionId}_0")));

            rows.Add(CreateRow(("🔙 До підсумку", $"stateparliament_sessionattendance_summary_{sessionId}")));

            return new InlineKeyboardMarkup(rows);
        }

        public static InlineKeyboardMarkup ActiveLawCategoryKeyboard()
        {
            return new InlineKeyboardMarkup(new[]
            {
                CreateRow(("✅ Прийняті (гілки)", "stateparliament_activelawcategory_accepted"), ("❌ Відхилені", "stateparliament_activelawcategory_rejected")),
                CreateRow(("🗑 Скасовані", "stateparliament_activelawcategory_revoked")),
                CreateRow(("🔙 Назад", "stateparliament_back"))
            });
        }

        public static InlineKeyboardMarkup ActiveLawBranchGroupsKeyboard(List<(string text, string data)> items, int page, int totalPages)
        {
            var rows = new List<InlineKeyboardButton[]>();

            if (items != null)
            {
                foreach (var item in items)
                    rows.Add(CreateRow((item.text, item.data)));
            }

            if (totalPages > 1)
            {
                var nav = new List<(string text, string data)>();
                if (page > 0)
                    nav.Add(("⬅ Попередні", $"stateparliament_activelawbranches_page_{page - 1}"));
                if (page < totalPages - 1)
                    nav.Add(("➡ Наступні", $"stateparliament_activelawbranches_page_{page + 1}"));
                if (nav.Count > 0)
                    rows.Add(CreateRow(nav.ToArray()));
            }

            rows.Add(CreateRow(("🔙 Назад", "stateparliament_activelaw")));
            return new InlineKeyboardMarkup(rows);
        }

        public static InlineKeyboardMarkup ActiveLawBranchKeyboard(List<(string text, string data)> items, int rootLawId, int page, int totalPages)
        {
            var rows = new List<InlineKeyboardButton[]>();

            if (items != null)
            {
                foreach (var item in items)
                    rows.Add(CreateRow((item.text, item.data)));
            }

            if (totalPages > 1)
            {
                var nav = new List<(string text, string data)>();
                if (page > 0)
                    nav.Add(("⬅ Попередні", $"stateparliament_lawbranch_{rootLawId}_page_{page - 1}"));
                if (page < totalPages - 1)
                    nav.Add(("➡ Наступні", $"stateparliament_lawbranch_{rootLawId}_page_{page + 1}"));
                if (nav.Count > 0)
                    rows.Add(CreateRow(nav.ToArray()));
            }

            rows.Add(CreateRow(("🔙 До гілок", "stateparliament_activelawcategory_accepted")));
            return new InlineKeyboardMarkup(rows);
        }

        public static InlineKeyboardMarkup ActiveLawSelectKeyboard(List<LawRecord> laws, string status, int page = 0, int perPage = 30)
        {
            var rows = new List<InlineKeyboardButton[]>();

            if (laws != null && laws.Count > 0)
            {
                if (page < 0)
                    page = 0;
                if (perPage < 1)
                    perPage = 30;

                int total = laws.Count;
                int totalPages = (int)Math.Ceiling(total / (double)perPage);
                if (page >= totalPages)
                    page = Math.Max(0, totalPages - 1);

                var pageItems = laws.Skip(page * perPage).Take(perPage).ToList();
                var buffer = new List<(string text, string data)>(3);

                foreach (var law in pageItems)
                {
                    string title = string.IsNullOrWhiteSpace(law.Title) ? $"#{law.LawId}" : law.Title;
                    buffer.Add(($"📜 {title}", $"stateparliament_activelawinfo_{law.LawId}"));
                    if (buffer.Count == 3)
                    {
                        rows.Add(CreateRow(buffer.ToArray()));
                        buffer.Clear();
                    }
                }

                if (buffer.Count > 0)
                    rows.Add(CreateRow(buffer.ToArray()));

                if (totalPages > 1)
                {
                    var navButtons = new List<(string text, string data)>();
                    if (page > 0)
                        navButtons.Add(("⬅ Попередні", $"stateparliament_activelawlist_{status.ToLowerInvariant()}_page_{page - 1}"));
                    if (page < totalPages - 1)
                        navButtons.Add(("➡ Наступні", $"stateparliament_activelawlist_{status.ToLowerInvariant()}_page_{page + 1}"));

                    if (navButtons.Count > 0)
                        rows.Add(CreateRow(navButtons.ToArray()));
                }
            }
            else
            {
                rows.Add(CreateRow(("Немає законів", "stateparliament_activelaw_empty")));
            }

            rows.Add(CreateRow(("🔙 Назад", "stateparliament_activelaw")));

            return new InlineKeyboardMarkup(rows);
        }

        public static InlineKeyboardMarkup ActiveLawInfoKeyboard(int lawId, bool canRevoke, int rootLawId)
        {
            var rows = new List<InlineKeyboardButton[]>();
            if (rootLawId > 0)
                rows.Add(CreateRow(("🌿 Показати гілку", $"stateparliament_lawbranch_{rootLawId}")));
            if (canRevoke)
                rows.Add(CreateRow(("🗑 Скасувати закон", $"stateparliament_activelawrepeal_{lawId}_confirm")));
            rows.Add(CreateRow(("🔙 Назад", "stateparliament_activelaw")));
            return new InlineKeyboardMarkup(rows);
        }

        public static InlineKeyboardMarkup ActiveLawRevokeConfirmKeyboard(int lawId)
        {
            return new InlineKeyboardMarkup(new[]
            {
                CreateRow(("✍ Ручне скасування", $"stateparliament_activelawrepeal_{lawId}_manual"), ("⚙ Автоскасування", $"stateparliament_activelawrepeal_{lawId}_auto")),
                CreateRow(("🔙 Назад", $"stateparliament_activelawinfo_{lawId}"))
            });
        }

        public static InlineKeyboardMarkup VotingLawSelectKeyboard(List<LawRecord> laws, int page = 0, int perPage = 20)
        {
            var rows = new List<InlineKeyboardButton[]>();

            if (laws != null && laws.Count > 0)
            {
                if (page < 0)
                    page = 0;
                if (perPage < 1)
                    perPage = 20;

                int total = laws.Count;
                int totalPages = (int)Math.Ceiling(total / (double)perPage);
                if (page >= totalPages)
                    page = Math.Max(0, totalPages - 1);

                var pageItems = laws.Skip(page * perPage).Take(perPage);
                foreach (var law in pageItems)
                {
                    string title = string.IsNullOrWhiteSpace(law.Title) ? $"#{law.LawId}" : law.Title;
                    rows.Add(CreateRow(($"🗳 {title}", $"stateparliament_votinglaw_{law.LawId}")));
                }

                if (totalPages > 1)
                {
                    var navButtons = new List<(string text, string data)>();
                    if (page > 0)
                        navButtons.Add(("⬅ Попередні", $"stateparliament_voting_page_{page - 1}"));
                    if (page < totalPages - 1)
                        navButtons.Add(("➡ Наступні", $"stateparliament_voting_page_{page + 1}"));

                    if (navButtons.Count > 0)
                        rows.Add(CreateRow(navButtons.ToArray()));
                }
            }
            else
            {
                rows.Add(CreateRow(("Немає законів", "stateparliament_voting_empty")));
            }

            rows.Add(CreateRow(("🔙 Назад", "stateparliament_back")));

            return new InlineKeyboardMarkup(rows);
        }

        public static InlineKeyboardMarkup VotingLawActionKeyboard(int lawId)
        {
            return new InlineKeyboardMarkup(new[]
            {
                CreateRow(("✍ Ручне голосування", $"stateparliament_votingstart_manual_{lawId}"), ("⚙ Автоголосування", $"stateparliament_votingstart_auto_{lawId}")),
                CreateRow(("🔙 Назад", "stateparliament_voting"))
            });
        }

        public static InlineKeyboardMarkup VotingConfirmKeyboard(int lawId)
        {
            return new InlineKeyboardMarkup(new[]
            {
                CreateRow(("✅ Підтвердити", $"stateparliament_votingconfirm_yes_{lawId}"), ("❌ Відхилити", $"stateparliament_votingconfirm_no_{lawId}"))
            });
        }

        public static InlineKeyboardMarkup VotingRevokeConfirmKeyboard(int lawId)
        {
            return new InlineKeyboardMarkup(new[]
            {
                CreateRow(("✅ Скасувати", $"stateparliament_votingconfirmrevoke_yes_{lawId}"), ("❌ Не скасовувати", $"stateparliament_votingconfirmrevoke_no_{lawId}"))
            });
        }

        public static InlineKeyboardMarkup FactionCommandsKeyboard(bool hasParties)
        {
            var rows = new List<InlineKeyboardButton[]>
            {
                CreateRow(("➕ Створити фракцію", "stateparliament_factioncreate"))
            };

            if (hasParties)
            {
                rows.Add(CreateRow(("✏ Редагувати фракцію", "stateparliament_factionedit"), ("🗑 Видалити фракцію", "stateparliament_factiondelete")));
            }

            rows.Add(CreateRow(("📄 Список фракцій", "stateparliament_factionlist"), ("👥 Члени фракцій", "stateparliament_factionmembers")));
            rows.Add(CreateRow(("🔙 Назад", "stateparliament_back")));

            return new InlineKeyboardMarkup(rows);
        }

        public static InlineKeyboardMarkup FactionSelectKeyboard(List<PartyRecord> parties, string callbackPrefix, string backData = "stateparliament_faction")
        {
            var rows = new List<InlineKeyboardButton[]>();

            if (parties != null && parties.Count > 0)
            {
                foreach (var party in parties.OrderBy(p => p.PartyId))
                {
                    string name = string.IsNullOrWhiteSpace(party.Name) ? $"#{party.PartyId}" : party.Name;
                    rows.Add(CreateRow(($"🏛 {name}", $"{callbackPrefix}_{party.PartyId}")));
                }
            }

            rows.Add(CreateRow(("🔙 Назад", backData)));
            return new InlineKeyboardMarkup(rows);
        }

        public static InlineKeyboardMarkup FactionDeleteConfirmKeyboard(int partyId, string backData = "stateparliament_factiondelete")
        {
            return new InlineKeyboardMarkup(new[]
            {
                CreateRow(("✅ Видалити", $"stateparliament_factiondelete_yes_{partyId}"), ("❌ Скасувати", backData))
            });
        }

        public static InlineKeyboardMarkup DeputiesListKeyboard(List<DeputyRecord> deputies, int page = 0, int perPage = 30)
        {
            var rows = new List<InlineKeyboardButton[]>();

            if (deputies != null && deputies.Count > 0)
            {
                if (page < 0)
                    page = 0;
                if (perPage < 1)
                    perPage = 30;

                int total = deputies.Count;
                int totalPages = (int)Math.Ceiling(total / (double)perPage);
                if (page >= totalPages)
                    page = Math.Max(0, totalPages - 1);

                var pageItems = deputies.Skip(page * perPage).Take(perPage);
                var buffer = new List<(string text, string data)>(3);

                foreach (var d in pageItems)
                {
                    string status = d.IsAlive ? "🟢" : "⚫";
                    buffer.Add(($"{status} {d.Name}", $"stateparliament_deputy_{d.DeputyId}"));
                    if (buffer.Count == 3)
                    {
                        rows.Add(CreateRow(buffer.ToArray()));
                        buffer.Clear();
                    }
                }

                if (buffer.Count > 0)
                    rows.Add(CreateRow(buffer.ToArray()));

                if (totalPages > 1)
                {
                    var navButtons = new List<(string text, string data)>();
                    if (page > 0)
                        navButtons.Add(("⬅ Попередні", $"stateparliament_alldeputies_page_{page - 1}"));
                    if (page < totalPages - 1)
                        navButtons.Add(("➡ Наступні", $"stateparliament_alldeputies_page_{page + 1}"));

                    if (navButtons.Count > 0)
                        rows.Add(CreateRow(navButtons.ToArray()));
                }
            }
            else
            {
                rows.Add(CreateRow(("Немає депутатів", "stateparliament_deputies_empty")));
            }

            rows.Add(CreateRow(("🔙 Назад", "stateparliament_deputies")));

            return new InlineKeyboardMarkup(rows);
        }

        public static InlineKeyboardMarkup DeputiesSelectKeyboard(List<DeputyRecord> deputies, string selectPrefix, string pagePrefix, int page = 0, int perPage = 30, string backData = "stateparliament_deputies")
        {
            var rows = new List<InlineKeyboardButton[]>();

            if (deputies != null && deputies.Count > 0)
            {
                if (page < 0)
                    page = 0;
                if (perPage < 1)
                    perPage = 30;

                int total = deputies.Count;
                int totalPages = (int)Math.Ceiling(total / (double)perPage);
                if (page >= totalPages)
                    page = Math.Max(0, totalPages - 1);

                var pageItems = deputies.Skip(page * perPage).Take(perPage);
                var buffer = new List<(string text, string data)>(3);

                foreach (var d in pageItems)
                {
                    string status = d.IsAlive ? "🟢" : "⚫";
                    buffer.Add(($"{status} {d.Name}", $"{selectPrefix}_{d.DeputyId}"));
                    if (buffer.Count == 3)
                    {
                        rows.Add(CreateRow(buffer.ToArray()));
                        buffer.Clear();
                    }
                }

                if (buffer.Count > 0)
                    rows.Add(CreateRow(buffer.ToArray()));

                if (totalPages > 1)
                {
                    var navButtons = new List<(string text, string data)>();
                    if (page > 0)
                        navButtons.Add(("⬅ Попередні", $"{pagePrefix}_{page - 1}"));
                    if (page < totalPages - 1)
                        navButtons.Add(("➡ Наступні", $"{pagePrefix}_{page + 1}"));

                    if (navButtons.Count > 0)
                        rows.Add(CreateRow(navButtons.ToArray()));
                }
            }
            else
            {
                rows.Add(CreateRow(("Немає депутатів", "stateparliament_deputies_empty")));
            }

            rows.Add(CreateRow(("🔙 Назад", backData)));

            return new InlineKeyboardMarkup(rows);
        }

        public static InlineKeyboardMarkup DeputyCommandsKeyboard(bool hasenoughdeputies)
        {
            var rows = new List<InlineKeyboardButton[]>
            {
                CreateRow(("➕ Створити депутата", $"stateparliament_deputycreate"), ("⚡ Швидко створити", $"stateparliament_deputyfastcreate"))
            };

            if (hasenoughdeputies)
            {
                rows.Add(CreateRow(("📄 Всі депутати", "stateparliament_alldeputies"), ("🗑 Звільнити депутатів", "stateparliament_deputydismisslist")));
                rows.Add(CreateRow(("🧹 Звільнити всіх", "stateparliament_deputydismissall_confirm"), ("🔙 Назад", "stateparliament_back")));
            }
            else
            {
                rows.Add(CreateRow(("🔙 Назад", "stateparliament_back")));
            }

            return new InlineKeyboardMarkup(rows);
        }

        public static InlineKeyboardMarkup DeputyDismissAllConfirmKeyboard()
        {
            return new InlineKeyboardMarkup(new[]
            {
                CreateRow(("✅ Підтвердити", "stateparliament_deputydismissall_yes"), ("❌ Скасувати", "stateparliament_deputydismissall_no"))
            });
        }

        public static InlineKeyboardMarkup DeputyDismissKeyboard(int deputyId, string backData = "stateparliament_deputies")
        {
            return new InlineKeyboardMarkup(new[]
            {
                CreateRow(("🗑 Звільнити", $"stateparliament_deputydismissdo_{deputyId}"), ("☠ Вбити", $"stateparliament_deputykill_{deputyId}")),
                CreateRow(("🔙 Назад", backData))
            });
        }


        public static InlineKeyboardMarkup DeputiesChangeCommandsKeyboard(int deputyId)
        {
            return new InlineKeyboardMarkup(new[]
            {
                CreateRow(("☠ Вбити", $"stateparliament_deputykill_{deputyId}"), ("💊 Лікувати", $"stateparliament_deputyheal_{deputyId}")),
                CreateRow(("⬆ Лояльність", $"stateparliament_deputyincrease_loyalty_{deputyId}"), ("⬇ Лояльність", $"stateparliament_deputydecrease_loyalty_{deputyId}")),
                CreateRow(("⬆ Корупція", $"stateparliament_deputyincrease_corruption_{deputyId}"), ("⬇ Корупція", $"stateparliament_deputydecrease_corruption_{deputyId}")),
                CreateRow(("⬆ Відвідуваність", $"stateparliament_deputyincrease_attendance_{deputyId}"), ("⬇ Відвідуваність", $"stateparliament_deputydecrease_attendance_{deputyId}")),
                CreateRow(("🔙 Назад", "stateparliament_back"))
            });
        }

        public static InlineKeyboardMarkup DeputyPartySelectKeyboard(List<PartyRecord> parties, string callbackPrefix, string backData = "stateparliament_deputies")
        {
            var rows = new List<InlineKeyboardButton[]>();

            rows.Add(CreateRow(("Без партії", $"{callbackPrefix}_0")));

            if (parties != null && parties.Count > 0)
            {
                foreach (var p in parties)
                {
                    string name = string.IsNullOrWhiteSpace(p.Name) ? $"#{p.PartyId}" : p.Name;
                    rows.Add(CreateRow(($"🏛 {name}", $"{callbackPrefix}_{p.PartyId}")));
                }
            }

            rows.Add(CreateRow(("🔙 Назад", backData)));

            return new InlineKeyboardMarkup(rows);
        }
    }
}
