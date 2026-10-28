#nullable enable
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DNDBot.DataBase;
using DNDBot.Goverment.Economy;
using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;

namespace DNDBot.Goverment
{
    public partial class StateAdministration : BotBodyRealisation
    {
        private sealed class ArmyTemplateDefinition
        {
            public string Key { get; init; } = string.Empty;
            public string DisplayName { get; init; } = string.Empty;
            public string UnitName { get; init; } = string.Empty;
            public string UnitLevel { get; init; } = "Company";
            public string UnitRole { get; init; } = "Field";
            public string UnitType { get; init; } = "Infantry";
            public string Race { get; init; } = "Human";
            public int Soldiers { get; init; }
            public string EquipmentTier { get; init; } = "Standard";
            public float EquipmentQuality { get; init; } = 50f;
            public float BattleExperience { get; init; } = 10f;
            public float Fatigue { get; init; }
            public float TrainingLevel { get; init; } = 40f;
            public float Morale { get; init; } = 55f;
            public float WeaponReadiness { get; init; } = 55f;
            public float SupplyLevel { get; init; } = 60f;
            public float CommanderSkill { get; init; } = 50f;
            public float CommanderLoyalty { get; init; } = 55f;
            public float CommanderCorruption { get; init; } = 5f;
            public float CommanderExperience { get; init; } = 20f;
            public string Status { get; init; } = "Forming";
            public int FormationDays { get; init; } = 10;
            public long CostGold { get; init; }
        }

        private static readonly List<ArmyTemplateDefinition> ArmyTemplateCatalog = new()
        {
            new ArmyTemplateDefinition
            {
                Key = "city_garrison",
                DisplayName = "🏰 Міський гарнізон",
                UnitName = "Міський гарнізон",
                UnitLevel = "Garrison",
                UnitRole = "Garrison",
                UnitType = "Spearmen",
                Race = "Human",
                Soldiers = 420,
                EquipmentTier = "Standard",
                EquipmentQuality = 48f,
                BattleExperience = 18f,
                TrainingLevel = 42f,
                Morale = 62f,
                WeaponReadiness = 52f,
                SupplyLevel = 58f,
                CommanderSkill = 48f,
                CommanderLoyalty = 65f,
                CommanderCorruption = 8f,
                CommanderExperience = 22f,
                FormationDays = 6,
                CostGold = 1800
            },
            new ArmyTemplateDefinition
            {
                Key = "ranger_archers",
                DisplayName = "🏹 Рота слідопитів",
                UnitName = "Рота слідопитів",
                UnitLevel = "Company",
                UnitRole = "Vanguard",
                UnitType = "Archers",
                Race = "Elf",
                Soldiers = 260,
                EquipmentTier = "Standard",
                EquipmentQuality = 58f,
                BattleExperience = 24f,
                TrainingLevel = 56f,
                Morale = 66f,
                WeaponReadiness = 60f,
                SupplyLevel = 61f,
                CommanderSkill = 57f,
                CommanderLoyalty = 63f,
                CommanderCorruption = 4f,
                CommanderExperience = 28f,
                FormationDays = 8,
                CostGold = 2100
            },
            new ArmyTemplateDefinition
            {
                Key = "field_battalion",
                DisplayName = "🛡 Польовий батальйон",
                UnitName = "Польовий батальйон",
                UnitLevel = "Battalion",
                UnitRole = "Field",
                UnitType = "HeavyInfantry",
                Race = "Human",
                Soldiers = 850,
                EquipmentTier = "Heavy",
                EquipmentQuality = 62f,
                BattleExperience = 26f,
                TrainingLevel = 58f,
                Morale = 63f,
                WeaponReadiness = 61f,
                SupplyLevel = 63f,
                CommanderSkill = 60f,
                CommanderLoyalty = 58f,
                CommanderCorruption = 7f,
                CommanderExperience = 34f,
                FormationDays = 12,
                CostGold = 4200
            },
            new ArmyTemplateDefinition
            {
                Key = "cavalry_wing",
                DisplayName = "🐎 Кавалерійський полк",
                UnitName = "Кавалерійський полк",
                UnitLevel = "Regiment",
                UnitRole = "Vanguard",
                UnitType = "Cavalry",
                Race = "Human",
                Soldiers = 520,
                EquipmentTier = "Heavy",
                EquipmentQuality = 68f,
                BattleExperience = 30f,
                TrainingLevel = 62f,
                Morale = 68f,
                WeaponReadiness = 66f,
                SupplyLevel = 60f,
                CommanderSkill = 64f,
                CommanderLoyalty = 57f,
                CommanderCorruption = 8f,
                CommanderExperience = 37f,
                FormationDays = 14,
                CostGold = 5800
            },
            new ArmyTemplateDefinition
            {
                Key = "arcane_corps",
                DisplayName = "✨ Арканний корпус",
                UnitName = "Арканний корпус",
                UnitLevel = "Battalion",
                UnitRole = "Field",
                UnitType = "MageCorps",
                Race = "Tiefling",
                Soldiers = 180,
                EquipmentTier = "Arcane",
                EquipmentQuality = 78f,
                BattleExperience = 38f,
                TrainingLevel = 68f,
                Morale = 64f,
                WeaponReadiness = 72f,
                SupplyLevel = 56f,
                CommanderSkill = 74f,
                CommanderLoyalty = 54f,
                CommanderCorruption = 9f,
                CommanderExperience = 45f,
                FormationDays = 16,
                CostGold = 7600
            },
            new ArmyTemplateDefinition
            {
                Key = "siege_train",
                DisplayName = "🏰 Облоговий дивізіон",
                UnitName = "Облоговий дивізіон",
                UnitLevel = "Battalion",
                UnitRole = "SiegeTrain",
                UnitType = "Siege",
                Race = "Dwarf",
                Soldiers = 300,
                EquipmentTier = "Heavy",
                EquipmentQuality = 70f,
                BattleExperience = 34f,
                TrainingLevel = 55f,
                Morale = 58f,
                WeaponReadiness = 74f,
                SupplyLevel = 62f,
                CommanderSkill = 62f,
                CommanderLoyalty = 56f,
                CommanderCorruption = 10f,
                CommanderExperience = 39f,
                FormationDays = 18,
                CostGold = 6800
            },
            new ArmyTemplateDefinition
            {
                Key = "imperial_legion",
                DisplayName = "🦁 Імперський легіон",
                UnitName = "Імперський легіон",
                UnitLevel = "Legion",
                UnitRole = "Field",
                UnitType = "HeavyInfantry",
                Race = "Human",
                Soldiers = 3200,
                EquipmentTier = "Elite",
                EquipmentQuality = 80f,
                BattleExperience = 52f,
                TrainingLevel = 74f,
                Morale = 72f,
                WeaponReadiness = 78f,
                SupplyLevel = 76f,
                CommanderSkill = 78f,
                CommanderLoyalty = 60f,
                CommanderCorruption = 6f,
                CommanderExperience = 61f,
                FormationDays = 35,
                CostGold = 26000
            },
            new ArmyTemplateDefinition
            {
                Key = "coastal_fleet",
                DisplayName = "🌊 Берегова ескадра",
                UnitName = "Берегова ескадра",
                UnitLevel = "Fleet",
                UnitRole = "Fleet",
                UnitType = "NavalFleet",
                Race = "Human",
                Soldiers = 900,
                EquipmentTier = "Naval",
                EquipmentQuality = 74f,
                BattleExperience = 36f,
                TrainingLevel = 61f,
                Morale = 67f,
                WeaponReadiness = 70f,
                SupplyLevel = 73f,
                CommanderSkill = 66f,
                CommanderLoyalty = 62f,
                CommanderCorruption = 7f,
                CommanderExperience = 48f,
                FormationDays = 42,
                CostGold = 34000
            }
        };

        private const int ArmyTemplatePageSize = 6;

        private async Task ShowEconomyCompaniesList(ITelegramBotClient botClient, long userId, long chatId, int msgId, int page, CancellationToken cancellationToken)
        {
            var selectedCountryId = GetSelectedCountryIdForUser(userId);
            if (!selectedCountryId.HasValue)
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "Не знайдено країни. Оберіть країну або призначте CountryId персонажу.",
                    StateKeyboardBuilder.EconomyCompaniesKeyboard(),
                    cancellationToken
                );
                return;
            }

            var companies = EconomyCompaniesData.GetByCountry(selectedCountryId.Value);
            const int pageSize = 12;
            int totalPages = Math.Max(1, (int)Math.Ceiling(companies.Count / (double)pageSize));
            page = Math.Max(0, Math.Min(page, totalPages - 1));

            var pageItems = companies
                .Skip(page * pageSize)
                .Take(pageSize)
                .Select(c =>
                {
                    string owner = c.IsStateCompany ? "Держ." : "Приват.";
                    string status = c.IsActive ? "🟢" : "⚫";
                    return ($"{status} {c.Name} • {owner} • {FmtNumber(c.JobsOffered)} місць", $"state_economy_companyinfo_{c.CompanyId}");
                })
                .ToList();

            var sb = new StringBuilder();
            sb.AppendLine("📄 Реєстр компаній");
            sb.AppendLine($"Усього: {FmtNumber(companies.Count)}");
            sb.AppendLine($"Сторінка: {FmtNumber(page + 1)}/{FmtNumber(totalPages)}");
            sb.AppendLine();
            sb.AppendLine("Оберіть компанію для перегляду.");

            await SafeEditMessageTextAsync(
                botClient,
                chatId,
                msgId,
                sb.ToString(),
                StateKeyboardBuilder.EconomyCompaniesListKeyboard(pageItems, page, totalPages),
                cancellationToken
            );
        }

        private async Task ShowEconomyCompanyInfo(ITelegramBotClient botClient, long userId, long chatId, int msgId, int companyId, CancellationToken cancellationToken)
        {
            var selectedCountryId = GetSelectedCountryIdForUser(userId);
            if (!selectedCountryId.HasValue)
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "Не знайдено країни.",
                    StateKeyboardBuilder.EconomyCompaniesKeyboard(),
                    cancellationToken
                );
                return;
            }

            var company = EconomyCompaniesData.GetById(companyId, selectedCountryId.Value);
            if (company == null)
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "Компанію не знайдено.",
                    StateKeyboardBuilder.EconomyCompaniesKeyboard(),
                    cancellationToken
                );
                return;
            }

            var buildings = EconomyBuildingsData.GetByCountry(selectedCountryId.Value)
                .Where(b => b.CompanyId == company.CompanyId)
                .ToList();

            int buildingsCount = buildings.Count;
            long buildingJobs = buildings.Sum(b => Math.Max(0, b.JobsProvided));
            decimal payroll = Math.Max(0m, company.BaseSalary) * Math.Max(0, company.JobsOffered);
            string owner = company.IsStateCompany ? "Державна" : "Приватна";
            string status = company.IsActive ? "Активна" : "Призупинена";
            string ownerName = string.IsNullOrWhiteSpace(company.OwnerName) ? "Немає" : company.OwnerName!;

            string text =
$@"🏭 Компанія #{company.CompanyId}
Назва: {company.Name}
Тип: {FormatProductionTypeUk(company.ProductionType)}
Власність: {owner}
Власник: {ownerName}
Статус: {status}

Робочі місця: {FmtNumber(company.JobsOffered)}
Базова зарплата: {FmtNumberCompact(company.BaseSalary)}
Оцінка фонду зарплат: {FmtNumberCompact(payroll)}
Податок компанії: {(company.TaxRate * 100m):0.##}%
Бюджет компанії: {FmtNumberCompact(company.Budget)}
Ресурс споживання: {(string.IsNullOrWhiteSpace(company.InputResourceType) ? "-" : FormatResourceTypeUk(company.InputResourceType))}
Споживання ресурсу/день: {FmtNumberCompact(company.InputAmount)}
Військовий профіль: {EconomyCalculator.Clamp(company.MilitaryProductionShare, 0f, 100f):0.#}%

Будівлі компанії: {FmtNumber(buildingsCount)} (роб. місць у будівлях: {FmtNumber(buildingJobs)})";

            await SafeEditMessageTextAsync(
                botClient,
                chatId,
                msgId,
                text,
                StateKeyboardBuilder.EconomyCompanyInfoKeyboard(company.CompanyId),
                cancellationToken
            );
        }

        private async Task ShowEconomyCompanyEditMenu(ITelegramBotClient botClient, long userId, long chatId, int msgId, int companyId, CancellationToken cancellationToken)
        {
            await SafeEditMessageTextAsync(
                botClient,
                chatId,
                msgId,
                $"✏ Редагування компанії #{companyId}",
                StateKeyboardBuilder.EconomyCompanyEditKeyboard(companyId),
                cancellationToken
            );
        }

        private async Task DeleteEconomyCompany(ITelegramBotClient botClient, long userId, long chatId, int msgId, int companyId, CancellationToken cancellationToken)
        {
            var selectedCountryId = GetSelectedCountryIdForUser(userId);
            if (!selectedCountryId.HasValue)
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "Не знайдено країни.",
                    StateKeyboardBuilder.EconomyCompaniesKeyboard(),
                    cancellationToken
                );
                return;
            }

            var buildings = EconomyBuildingsData.GetByCountry(selectedCountryId.Value)
                .Where(b => b.CompanyId == companyId)
                .ToList();

            foreach (var building in buildings)
                EconomyBuildingsData.UpdateBuildingField(building.BuildingId, selectedCountryId.Value, "CompanyId", DBNull.Value, SqlDbType.Int);

            bool deleted = EconomyCompaniesData.DeleteCompany(companyId, selectedCountryId.Value);
            string message = deleted
                ? $"✅ Компанію #{companyId} видалено. Відв'язано будівель: {FmtNumber(buildings.Count)}."
                : "❌ Не вдалося видалити компанію.";

            await SafeEditMessageTextAsync(
                botClient,
                chatId,
                msgId,
                message,
                StateKeyboardBuilder.EconomyCompaniesKeyboard(),
                cancellationToken
            );
        }

        private async Task StartCompanyCreation(ITelegramBotClient botClient, long userId, CancellationToken cancellationToken)
        {
            var selectedCountryId = GetSelectedCountryIdForUser(userId);
            if (!selectedCountryId.HasValue)
            {
                await botClient.SendTextMessageAsync(userId, "Не знайдено країни.", cancellationToken: cancellationToken);
                return;
            }

            ClearEconomyInput(userId);
            EconomyInputState[userId] = EconomyInputMode.AddCompany;
            EconomyInputStep[userId] = 0;
            EconomyCompanyBuffer[userId] = new EconomyCompanyRecord
            {
                CountryId = selectedCountryId.Value,
                ProductionType = "General",
                BaseSalary = EconomyConfigData.GetDecimalForCountry(selectedCountryId.Value, "CompanyBaseSalary", 1m),
                TaxRate = 0.2m,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await SendModeFieldPromptAsync(
                botClient,
                userId,
                EconomyInputMode.AddCompany,
                CompanyFields[0].FieldName,
                CompanyFields[0].Prompt,
                cancellationToken);
        }

        private async Task HandleCompanyInput(ITelegramBotClient botClient, long userId, string input, CancellationToken cancellationToken)
        {
            int step = EconomyInputStep.TryGetValue(userId, out int currentStep) ? currentStep : 0;
            if (step < 0 || step >= CompanyFields.Count)
                step = 0;

            if (!EconomyCompanyBuffer.TryGetValue(userId, out var company))
            {
                var selectedCountryId = GetSelectedCountryIdForUser(userId);
                if (!selectedCountryId.HasValue)
                {
                    await SendHelperMessageAsync(botClient, userId, "Не знайдено країни.", cancellationToken);
                    return;
                }

                company = new EconomyCompanyRecord
                {
                    CountryId = selectedCountryId.Value,
                    ProductionType = "General",
                    BaseSalary = 1m,
                    TaxRate = 0.2m,
                    IsActive = true
                };
                EconomyCompanyBuffer[userId] = company;
            }

            var field = CompanyFields[step];
            if (field.FieldName == "OwnerName" && (input == "-" || string.IsNullOrWhiteSpace(input)))
            {
                company.OwnerName = null;
            }
            else if (!TryAssignByKind(company, field.FieldName, field.Kind, input, out string error))
            {
                await SendHelperMessageAsync(botClient, userId, error, cancellationToken);
                return;
            }

            EconomyInputStep[userId] = step + 1;
            if (EconomyInputStep[userId] >= CompanyFields.Count)
            {
                NormalizeCompanyRecord(company);
                int id = EconomyCompaniesData.AddCompany(company);
                await CleanupHelperMessagesAsync(botClient, userId, cancellationToken);
                ClearEconomyInput(userId);
                await botClient.SendTextMessageAsync(
                    userId,
                    id > 0 ? $"✅ Компанію створено: #{id} {company.Name}" : "❌ Не вдалося створити компанію.",
                    cancellationToken: cancellationToken
                );
                return;
            }

            var nextField = CompanyFields[EconomyInputStep[userId]];
            await SendModeFieldPromptAsync(
                botClient,
                userId,
                EconomyInputMode.AddCompany,
                nextField.FieldName,
                nextField.Prompt,
                cancellationToken);
        }

        private async Task ShowEconomyCompaniesBudget(ITelegramBotClient botClient, long userId, long chatId, int msgId, CancellationToken cancellationToken)
        {
            var selectedCountryId = GetSelectedCountryIdForUser(userId);
            if (!selectedCountryId.HasValue)
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "Не знайдено країни.",
                    StateKeyboardBuilder.EconomyCompaniesKeyboard(),
                    cancellationToken
                );
                return;
            }

            var companies = EconomyCompaniesData.GetByCountry(selectedCountryId.Value);
            var active = companies.Where(c => c.IsActive).ToList();
            decimal total = active.Sum(c => Math.Max(0m, c.Budget));
            decimal stateOwned = active.Where(c => c.IsStateCompany).Sum(c => Math.Max(0m, c.Budget));
            decimal privateOwned = active.Where(c => !c.IsStateCompany).Sum(c => Math.Max(0m, c.Budget));
            decimal payroll = active.Sum(c => Math.Max(0m, c.BaseSalary) * Math.Max(0, c.JobsOffered));
            long jobs = active.Sum(c => Math.Max(0, c.JobsOffered));

            var sb = new StringBuilder();
            sb.AppendLine("💼 Бюджети компаній");
            sb.AppendLine($"Активних компаній: {FmtNumber(active.Count)}");
            sb.AppendLine($"Загальний бюджет: {FmtNumberCompact(total)}");
            sb.AppendLine($"Держ. компанії: {FmtNumberCompact(stateOwned)}");
            sb.AppendLine($"Приватні компанії: {FmtNumberCompact(privateOwned)}");
            sb.AppendLine($"Оцінка фонду зарплат: {FmtNumberCompact(payroll)}");
            sb.AppendLine($"Робочі місця: {FmtNumber(jobs)}");

            await SafeEditMessageTextAsync(
                botClient,
                chatId,
                msgId,
                sb.ToString(),
                StateKeyboardBuilder.EconomyCompaniesKeyboard(),
                cancellationToken
            );
        }

        private async Task ShowEconomyCompanyBuildingsOverview(ITelegramBotClient botClient, long userId, long chatId, int msgId, CancellationToken cancellationToken)
        {
            var selectedCountryId = GetSelectedCountryIdForUser(userId);
            if (!selectedCountryId.HasValue)
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "Не знайдено країни.",
                    StateKeyboardBuilder.EconomyCompaniesKeyboard(),
                    cancellationToken
                );
                return;
            }

            var companies = EconomyCompaniesData.GetByCountry(selectedCountryId.Value);
            var buildings = EconomyBuildingsData.GetByCountry(selectedCountryId.Value);
            int assigned = buildings.Count(b => b.CompanyId.HasValue);
            int unassigned = Math.Max(0, buildings.Count - assigned);

            var rows = new List<InlineKeyboardButton[]>();
            foreach (var company in companies.Take(18))
            {
                int count = buildings.Count(b => b.CompanyId == company.CompanyId);
                string marker = company.IsActive ? "🟢" : "⚫";
                rows.Add(new[]
                {
                    InlineKeyboardButton.WithCallbackData(
                        $"{marker} {company.Name} ({FmtNumber(count)})",
                        $"state_economy_company_buildings_{company.CompanyId}")
                });
            }

            rows.Add(new[] { InlineKeyboardButton.WithCallbackData("🔙 Назад", "state_economy_companies") });

            string text =
$@"🏗 Будівлі компаній
Загалом будівель: {FmtNumber(buildings.Count)}
Прив'язані до компаній: {FmtNumber(assigned)}
Без компанії: {FmtNumber(unassigned)}

Оберіть компанію, щоб призначати/знімати будівлі.";

            await SafeEditMessageTextAsync(
                botClient,
                chatId,
                msgId,
                text,
                new InlineKeyboardMarkup(rows),
                cancellationToken
            );
        }

        private async Task ShowEconomyCompanyBuildingsAssignment(ITelegramBotClient botClient, long userId, long chatId, int msgId, int companyId, int page, CancellationToken cancellationToken)
        {
            var selectedCountryId = GetSelectedCountryIdForUser(userId);
            if (!selectedCountryId.HasValue)
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "Не знайдено країни.",
                    StateKeyboardBuilder.EconomyCompaniesKeyboard(),
                    cancellationToken
                );
                return;
            }

            var company = EconomyCompaniesData.GetById(companyId, selectedCountryId.Value);
            if (company == null)
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "Компанію не знайдено.",
                    StateKeyboardBuilder.EconomyCompaniesKeyboard(),
                    cancellationToken
                );
                return;
            }

            var companies = EconomyCompaniesData.GetByCountry(selectedCountryId.Value)
                .ToDictionary(c => c.CompanyId, c => c.Name);
            var buildings = EconomyBuildingsData.GetByCountry(selectedCountryId.Value);
            const int pageSize = 10;
            int totalPages = Math.Max(1, (int)Math.Ceiling(buildings.Count / (double)pageSize));
            page = Math.Max(0, Math.Min(page, totalPages - 1));

            var items = buildings
                .OrderByDescending(b => b.CompanyId == companyId)
                .ThenBy(b => b.BuildingId)
                .Skip(page * pageSize)
                .Take(pageSize)
                .Select(b =>
                {
                    bool mine = b.CompanyId == companyId;
                    string marker = mine ? "✅" : "⬜";
                    string owner = "без компанії";
                    if (b.CompanyId.HasValue && companies.TryGetValue(b.CompanyId.Value, out var ownerName))
                        owner = ownerName;
                    string text = $"{marker} #{b.BuildingId} {b.Type} (L{b.Level}) • {owner}";
                    string data = $"state_economy_company_building_toggle|{companyId}|{b.BuildingId}|{page}";
                    return (text, data);
                })
                .ToList();

            int assignedCount = buildings.Count(b => b.CompanyId == companyId);
            string text =
$@"🏗 Прив'язка будівель до компанії
Компанія: {company.Name}
Прив'язано будівель: {FmtNumber(assignedCount)}
Сторінка: {FmtNumber(page + 1)}/{FmtNumber(totalPages)}

Натисніть на будівлю, щоб призначити або зняти.";

            await SafeEditMessageTextAsync(
                botClient,
                chatId,
                msgId,
                text,
                StateKeyboardBuilder.EconomyCompanyBuildingsKeyboard(companyId, items, page, totalPages),
                cancellationToken
            );
        }

        private async Task ToggleEconomyCompanyBuildingAssignment(ITelegramBotClient botClient, long userId, long chatId, int msgId, int companyId, int buildingId, int page, CancellationToken cancellationToken)
        {
            var selectedCountryId = GetSelectedCountryIdForUser(userId);
            if (!selectedCountryId.HasValue)
                return;

            var building = EconomyBuildingsData.GetByCountry(selectedCountryId.Value).FirstOrDefault(b => b.BuildingId == buildingId);
            if (building == null)
                return;

            object value = building.CompanyId == companyId ? DBNull.Value : companyId;
            EconomyBuildingsData.UpdateBuildingField(buildingId, selectedCountryId.Value, "CompanyId", value, SqlDbType.Int);
            await ShowEconomyCompanyBuildingsAssignment(botClient, userId, chatId, msgId, companyId, page, cancellationToken);
        }

        private async Task ShowMilitaryMenu(ITelegramBotClient botClient, long userId, long chatId, int msgId, CancellationToken cancellationToken)
        {
            if (!HasAdminAccess(userId))
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "Нема доступу.",
                    StateKeyboardBuilder.MainStateKeyboard(userId),
                    cancellationToken
                );
                return;
            }

            var selectedCountryId = GetSelectedCountryIdForUser(userId);
            if (!selectedCountryId.HasValue)
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "Не знайдено країни.",
                    StateKeyboardBuilder.MainStateKeyboard(userId),
                    cancellationToken
                );
                return;
            }

            MilitaryArsenalCatalog.EnsureResourcesInEconomyCatalog();
            var state = new StateDataProvider().GetStateData(userId, selectedCountryId.Value);
            var units = MilitaryUnitsData.GetByCountry(selectedCountryId.Value);
            var obligations = VassalMilitaryObligationsData.GetByCountry(selectedCountryId.Value);
            var stock = MilitaryArsenalService.GetStockOverview(selectedCountryId.Value);
            decimal stateOrderShare = EconomyConfigData.GetDecimalForCountry(selectedCountryId.Value, "StateOrderTreasuryShare", 0.02m);
            decimal manualWeaponQualityBonus = EconomyConfigData.GetDecimalForCountry(selectedCountryId.Value, "ManualWeaponQualityBonus", 0m);
            decimal stockQty = stock.Sum(s => s.Quantity);
            decimal stockPower = stock.Sum(s => s.PowerPoints);

            string text =
$@"🛡 Військо: {state.CountryName}
Армія: {FmtNumber(state.ArmySize)} | Боєздатність: {state.CombatEffectiveness:0.##}%
Мораль: {state.ArmyMorale:0.##}% | Якість зброї: {state.WeaponQuality:0.##}% ({manualWeaponQualityBonus:+0.##;-0.##;0}% бонус)
Забезпечення: {state.SupplyCoverage:0.##}% | Амуніція: {state.AmmunitionIssued:0.##}%
Арсенал: {FmtNumber(stock.Count)} типів / {FmtNumberCompact(stockQty)} од. (сила {FmtNumberCompact(stockPower)})
Розвідка / контррозвідка: {state.IntelLevel:0.##}% / {state.CounterIntelLevel:0.##}%
Частини: {FmtNumber(units.Count)} | Васали: {FmtNumber(obligations.Count(o => o.IsActive))}
Васальні полки: {FmtNumber(state.VassalRegiments)} | Данина: {FmtNumberCompact(state.VassalFunding)}
Держзамовлення: {FmtNumberCompact(state.StateOrderBudget)} ({stateOrderShare * 100m:0.##}% від казни)";

            await SafeEditMessageTextAsync(
                botClient,
                chatId,
                msgId,
                text,
                StateKeyboardBuilder.MilitaryMenuKeyboard(),
                cancellationToken
            );
            LastStateViewByUser[userId] = "military";
        }

        private async Task ShowMilitaryStateOrders(ITelegramBotClient botClient, long userId, long chatId, int msgId, CancellationToken cancellationToken)
        {
            var selectedCountryId = GetSelectedCountryIdForUser(userId);
            if (!selectedCountryId.HasValue)
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "Не знайдено країни.",
                    StateKeyboardBuilder.MilitaryMenuKeyboard(),
                    cancellationToken
                );
                return;
            }

            var country = CountriesData.GetCountryById(selectedCountryId.Value);
            if (country == null)
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "Країну не знайдено.",
                    StateKeyboardBuilder.MilitaryMenuKeyboard(),
                    cancellationToken
                );
                return;
            }

            decimal share = EconomyConfigData.GetDecimalForCountry(country.CountryId, "StateOrderTreasuryShare", 0.02m);
            decimal max = EconomyConfigData.GetDecimalForCountry(country.CountryId, "StateOrderBudgetMax", 10000m);
            decimal warMult = EconomyConfigData.GetDecimalForCountry(country.CountryId, "StateOrderWarMultiplier", 1.35m);
            decimal budget = SocioMilitaryMechanics.ComputeStateOrderBudget(country);

            var rows = new List<InlineKeyboardButton[]>
            {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("1%", "state_military_stateorder_set_1"),
                    InlineKeyboardButton.WithCallbackData("2%", "state_military_stateorder_set_2"),
                    InlineKeyboardButton.WithCallbackData("3%", "state_military_stateorder_set_3")
                },
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("5%", "state_military_stateorder_set_5"),
                    InlineKeyboardButton.WithCallbackData("8%", "state_military_stateorder_set_8"),
                    InlineKeyboardButton.WithCallbackData("10%", "state_military_stateorder_set_10")
                },
                new[] { InlineKeyboardButton.WithCallbackData("⬅ Назад", "state_military") }
            };

            string text =
$@"📦 Налаштування держзамовлення
Частка від казни: {share * 100m:0.##}%
Поточний бюджет: {FmtNumberCompact(budget)}
Макс. бюджет: {FmtNumberCompact(max)}
Множник у війну: x{warMult:0.##}

Оберіть частку від казни для держзамовлення.";

            await SafeEditMessageTextAsync(botClient, chatId, msgId, text, new InlineKeyboardMarkup(rows), cancellationToken);
        }

        private async Task ApplyMilitaryStateOrderShare(ITelegramBotClient botClient, long userId, long chatId, int msgId, int percent, CancellationToken cancellationToken)
        {
            if (!HasAdminAccess(userId))
                return;

            var selectedCountryId = GetSelectedCountryIdForUser(userId);
            if (!selectedCountryId.HasValue)
                return;

            int clamped = Math.Max(0, Math.Min(50, percent));
            decimal oldShare = EconomyConfigData.GetDecimalForCountry(selectedCountryId.Value, "StateOrderTreasuryShare", 0.02m);
            decimal newShare = clamped / 100m;
            EconomyConfigData.UpsertCountryDecimal(selectedCountryId.Value, "StateOrderTreasuryShare", newShare);
            LogAdminAction(selectedCountryId.Value, userId, "StateOrderShare", oldShare.ToString(CultureInfo.InvariantCulture), newShare.ToString(CultureInfo.InvariantCulture), "MilitaryStateOrder");

            await ShowMilitaryStateOrders(botClient, userId, chatId, msgId, cancellationToken);
        }

        private MilitaryBattlePlanContext GetOrCreateMilitaryBattlePlan(long userId)
        {
            if (!MilitaryBattlePlanByUser.TryGetValue(userId, out var plan))
            {
                plan = new MilitaryBattlePlanContext();
                MilitaryBattlePlanByUser[userId] = plan;
            }

            plan.Terrain = MilitaryBattleService.NormalizeTerrain(plan.Terrain);
            plan.Tactic = MilitaryBattleService.NormalizeTactic(plan.Tactic);
            return plan;
        }

        private async Task ShowMilitaryBattleMenu(ITelegramBotClient botClient, long userId, long chatId, int msgId, CancellationToken cancellationToken)
        {
            var selectedCountryId = GetSelectedCountryIdForUser(userId);
            if (!selectedCountryId.HasValue)
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "Не знайдено країни.",
                    StateKeyboardBuilder.MilitaryMenuKeyboard(),
                    cancellationToken
                );
                return;
            }

            var ownCountry = CountriesData.GetCountryById(selectedCountryId.Value);
            if (ownCountry == null)
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "Країну не знайдено.",
                    StateKeyboardBuilder.MilitaryMenuKeyboard(),
                    cancellationToken
                );
                return;
            }

            var plan = GetOrCreateMilitaryBattlePlan(userId);
            CountryRecord? targetCountry = null;
            if (plan.TargetCountryId.HasValue)
            {
                targetCountry = CountriesData.GetCountryById(plan.TargetCountryId.Value);
                if (targetCountry == null || targetCountry.CountryId == ownCountry.CountryId)
                {
                    plan.TargetCountryId = null;
                    targetCountry = null;
                }
            }

            if (plan.TargetCountryId.HasValue && targetCountry == null)
                targetCountry = CountriesData.GetCountryById(plan.TargetCountryId.Value);

            var ownEstimate = MilitaryBattleService.EstimateCountryPower(
                ownCountry.CountryId,
                plan.Terrain,
                plan.Tactic,
                attacking: true,
                includeVassals: plan.IncludeVassals);

            MilitaryForceEstimate? targetEstimate = null;
            if (plan.TargetCountryId.HasValue)
            {
                targetEstimate = MilitaryBattleService.EstimateCountryPower(
                    plan.TargetCountryId.Value,
                    plan.Terrain,
                    "Balanced",
                    attacking: false,
                    includeVassals: true);
            }

            string ownName = string.IsNullOrWhiteSpace(ownCountry.Name) ? $"#{ownCountry.CountryId}" : ownCountry.Name;
            string targetName = targetCountry == null
                ? "не обрана"
                : (string.IsNullOrWhiteSpace(targetCountry.Name) ? $"#{targetCountry.CountryId}" : targetCountry.Name);
            float ratio = (targetEstimate != null && targetEstimate.Exists)
                ? ownEstimate.Power / Math.Max(1f, targetEstimate.Power)
                : 0f;

            string text =
$@"⚔ Бойові дії
Атакуюча держава: {ownName}
Ціль: {targetName}
Місцевість: {MilitaryBattleService.FormatTerrainUk(plan.Terrain)}
Тактика: {MilitaryBattleService.FormatTacticUk(plan.Tactic)}
Підключати васалів: {(plan.IncludeVassals ? "так" : "ні")}

Ваша оцінка:
Сила: {FmtNumber((int)Math.Round(ownEstimate.Power, MidpointRounding.AwayFromZero))}
Воїни: {FmtNumber(ownEstimate.TotalTroops)} (власні {FmtNumber(ownEstimate.OwnTroops)}, васали {FmtNumber(ownEstimate.VassalTroops)})
Середній бойовий досвід: {ownEstimate.AverageExperience:0.#}

Оцінка цілі (захист, базова тактика): {(targetEstimate == null || !targetEstimate.Exists ? "немає" : FmtNumber((int)Math.Round(targetEstimate.Power, MidpointRounding.AwayFromZero)))}
Порівняння сили (ви/ціль): {(targetEstimate == null || !targetEstimate.Exists ? "-" : ratio.ToString("0.00", CultureInfo.InvariantCulture))}";

            await SafeEditMessageTextAsync(
                botClient,
                chatId,
                msgId,
                text,
                StateKeyboardBuilder.MilitaryBattleMenuKeyboard(plan.TargetCountryId.HasValue, plan.IncludeVassals),
                cancellationToken
            );
        }

        private async Task ShowMilitaryBattleTargetSelect(ITelegramBotClient botClient, long userId, long chatId, int msgId, int page, CancellationToken cancellationToken)
        {
            var selectedCountryId = GetSelectedCountryIdForUser(userId);
            if (!selectedCountryId.HasValue)
                return;

            var countries = CountriesData.GetAllCountries()
                .Where(c => c.CountryId != selectedCountryId.Value)
                .OrderBy(c => c.Name)
                .ToList();

            var plan = GetOrCreateMilitaryBattlePlan(userId);
            const int pageSize = 12;
            int totalPages = Math.Max(1, (int)Math.Ceiling(countries.Count / (double)pageSize));
            page = Math.Max(0, Math.Min(page, totalPages - 1));
            var pageCountries = countries.Skip(page * pageSize).Take(pageSize).ToList();

            string selectedText = "не обрана";
            if (plan.TargetCountryId.HasValue)
            {
                var selectedTarget = countries.FirstOrDefault(c => c.CountryId == plan.TargetCountryId.Value);
                if (selectedTarget != null)
                    selectedText = string.IsNullOrWhiteSpace(selectedTarget.Name) ? $"#{selectedTarget.CountryId}" : selectedTarget.Name;
            }

            string text =
$@"🎯 Вибір цілі
Ціль: {selectedText}
Сторінка: {FmtNumber(page + 1)}/{FmtNumber(totalPages)}
Доступні держави: {FmtNumber(countries.Count)}";

            await SafeEditMessageTextAsync(
                botClient,
                chatId,
                msgId,
                text,
                StateKeyboardBuilder.MilitaryBattleTargetSelectKeyboard(pageCountries, page, totalPages, plan.TargetCountryId),
                cancellationToken
            );
        }

        private async Task SetMilitaryBattleTarget(ITelegramBotClient botClient, long userId, long chatId, int msgId, int targetCountryId, CancellationToken cancellationToken)
        {
            var selectedCountryId = GetSelectedCountryIdForUser(userId);
            if (!selectedCountryId.HasValue)
                return;

            if (targetCountryId <= 0 || targetCountryId == selectedCountryId.Value)
            {
                await ShowMilitaryBattleTargetSelect(botClient, userId, chatId, msgId, 0, cancellationToken);
                return;
            }

            var targetCountry = CountriesData.GetCountryById(targetCountryId);
            if (targetCountry == null)
            {
                await ShowMilitaryBattleTargetSelect(botClient, userId, chatId, msgId, 0, cancellationToken);
                return;
            }

            var plan = GetOrCreateMilitaryBattlePlan(userId);
            plan.TargetCountryId = targetCountryId;
            await ShowMilitaryBattleMenu(botClient, userId, chatId, msgId, cancellationToken);
        }

        private async Task ShowMilitaryBattleTerrainMenu(ITelegramBotClient botClient, long userId, long chatId, int msgId, CancellationToken cancellationToken)
        {
            var plan = GetOrCreateMilitaryBattlePlan(userId);
            string text =
$@"🗺 Вибір місцевості
Поточне значення: {MilitaryBattleService.FormatTerrainUk(plan.Terrain)}";

            await SafeEditMessageTextAsync(
                botClient,
                chatId,
                msgId,
                text,
                StateKeyboardBuilder.MilitaryBattleTerrainKeyboard(plan.Terrain),
                cancellationToken
            );
        }

        private async Task SetMilitaryBattleTerrain(ITelegramBotClient botClient, long userId, long chatId, int msgId, string terrain, CancellationToken cancellationToken)
        {
            var plan = GetOrCreateMilitaryBattlePlan(userId);
            plan.Terrain = MilitaryBattleService.NormalizeTerrain(terrain);
            await ShowMilitaryBattleTerrainMenu(botClient, userId, chatId, msgId, cancellationToken);
        }

        private async Task ShowMilitaryBattleTacticMenu(ITelegramBotClient botClient, long userId, long chatId, int msgId, CancellationToken cancellationToken)
        {
            var plan = GetOrCreateMilitaryBattlePlan(userId);
            string text =
$@"🧭 Вибір тактики
Поточне значення: {MilitaryBattleService.FormatTacticUk(plan.Tactic)}";

            await SafeEditMessageTextAsync(
                botClient,
                chatId,
                msgId,
                text,
                StateKeyboardBuilder.MilitaryBattleTacticKeyboard(plan.Tactic),
                cancellationToken
            );
        }

        private async Task SetMilitaryBattleTactic(ITelegramBotClient botClient, long userId, long chatId, int msgId, string tactic, CancellationToken cancellationToken)
        {
            var plan = GetOrCreateMilitaryBattlePlan(userId);
            plan.Tactic = MilitaryBattleService.NormalizeTactic(tactic);
            await ShowMilitaryBattleTacticMenu(botClient, userId, chatId, msgId, cancellationToken);
        }

        private async Task ToggleMilitaryBattleIncludeVassals(ITelegramBotClient botClient, long userId, long chatId, int msgId, CancellationToken cancellationToken)
        {
            var plan = GetOrCreateMilitaryBattlePlan(userId);
            plan.IncludeVassals = !plan.IncludeVassals;
            await ShowMilitaryBattleMenu(botClient, userId, chatId, msgId, cancellationToken);
        }

        private async Task ExecuteMilitaryBattle(ITelegramBotClient botClient, long userId, long chatId, int msgId, bool retreat, CancellationToken cancellationToken)
        {
            var selectedCountryId = GetSelectedCountryIdForUser(userId);
            if (!selectedCountryId.HasValue)
                return;

            var plan = GetOrCreateMilitaryBattlePlan(userId);
            if (!plan.TargetCountryId.HasValue || plan.TargetCountryId.Value == selectedCountryId.Value)
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "Спочатку оберіть ціль для бойової операції.",
                    StateKeyboardBuilder.MilitaryBattleMenuKeyboard(false, plan.IncludeVassals),
                    cancellationToken
                );
                return;
            }

            var targetCountry = CountriesData.GetCountryById(plan.TargetCountryId.Value);
            if (targetCountry == null)
            {
                plan.TargetCountryId = null;
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "Ціль не знайдена. Оберіть іншу державу.",
                    StateKeyboardBuilder.MilitaryBattleMenuKeyboard(false, plan.IncludeVassals),
                    cancellationToken
                );
                return;
            }

            var request = new MilitaryBattleRequest
            {
                AttackerCountryId = selectedCountryId.Value,
                DefenderCountryId = targetCountry.CountryId,
                Terrain = plan.Terrain,
                AttackerTactic = plan.Tactic,
                IncludeAttackerVassals = plan.IncludeVassals,
                IncludeDefenderVassals = true
            };

            var result = retreat
                ? MilitaryBattleService.ResolveRetreat(request)
                : MilitaryBattleService.ResolveBattle(request);

            if (!result.Success)
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    $"❌ Бойову операцію не виконано.\n{result.Error}",
                    StateKeyboardBuilder.MilitaryBattleMenuKeyboard(plan.TargetCountryId.HasValue, plan.IncludeVassals),
                    cancellationToken
                );
                return;
            }

            var attackerCountry = CountriesData.GetCountryById(result.AttackerCountryId);
            var defenderCountry = CountriesData.GetCountryById(result.DefenderCountryId);
            string attackerName = attackerCountry == null || string.IsNullOrWhiteSpace(attackerCountry.Name) ? $"#{result.AttackerCountryId}" : attackerCountry.Name;
            string defenderName = defenderCountry == null || string.IsNullOrWhiteSpace(defenderCountry.Name) ? $"#{result.DefenderCountryId}" : defenderCountry.Name;
            string winnerText = result.WinnerCountryId.HasValue
                ? (result.WinnerCountryId.Value == result.AttackerCountryId ? attackerName : defenderName)
                : "Нічия";
            static string Signed(float value) => value >= 0f ? $"+{value:0.#}" : $"{value:0.#}";

            string text =
$@"{(retreat ? "🪽 Відступ виконано" : "⚔ Бій завершено")}
Формат: {MilitaryBattleService.FormatResultTypeUk(result.ResultType)}
Місцевість: {MilitaryBattleService.FormatTerrainUk(result.Terrain)}
Тактика атаки: {MilitaryBattleService.FormatTacticUk(result.AttackerTactic)}
Тактика захисту: {MilitaryBattleService.FormatTacticUk(result.DefenderTactic)}
Переможець: {winnerText}

Сили сторін:
{attackerName}: {FmtNumber((int)Math.Round(result.AttackerPower, MidpointRounding.AwayFromZero))}
{defenderName}: {FmtNumber((int)Math.Round(result.DefenderPower, MidpointRounding.AwayFromZero))}

Втрати:
{attackerName}: {FmtNumber(result.AttackerLosses)} (васали {FmtNumber(result.AttackerVassalLosses)})
{defenderName}: {FmtNumber(result.DefenderLosses)} (васали {FmtNumber(result.DefenderVassalLosses)})

Мораль:
{attackerName}: {Signed(result.AttackerMoraleDelta)}
{defenderName}: {Signed(result.DefenderMoraleDelta)}

Бойовий досвід:
{attackerName}: {Signed(result.AttackerExperienceGain)}
{defenderName}: {Signed(result.DefenderExperienceGain)}

Журнал: #{result.BattleLogId}

{result.Summary}";

            await SafeEditMessageTextAsync(
                botClient,
                chatId,
                msgId,
                text,
                StateKeyboardBuilder.MilitaryBattleMenuKeyboard(plan.TargetCountryId.HasValue, plan.IncludeVassals),
                cancellationToken
            );
        }

        private async Task ShowMilitaryBattleLogs(ITelegramBotClient botClient, long userId, long chatId, int msgId, int page, CancellationToken cancellationToken)
        {
            var selectedCountryId = GetSelectedCountryIdForUser(userId);
            if (!selectedCountryId.HasValue)
                return;

            var logs = MilitaryBattleLogsData.GetByCountry(selectedCountryId.Value, 200);
            const int pageSize = 10;
            int totalPages = Math.Max(1, (int)Math.Ceiling(logs.Count / (double)pageSize));
            page = Math.Max(0, Math.Min(page, totalPages - 1));

            var countryMap = CountriesData.GetAllCountries().ToDictionary(c => c.CountryId, c => c);
            string GetCountryName(int countryId)
            {
                if (!countryMap.TryGetValue(countryId, out var country))
                    return $"#{countryId}";
                return string.IsNullOrWhiteSpace(country.Name) ? $"#{countryId}" : country.Name;
            }

            var pageItems = logs
                .Skip(page * pageSize)
                .Take(pageSize)
                .Select(log =>
                {
                    bool ownIsAttacker = log.AttackerCountryId == selectedCountryId.Value;
                    int enemyCountryId = ownIsAttacker ? log.DefenderCountryId : log.AttackerCountryId;
                    bool ownWon = log.WinnerCountryId.HasValue && log.WinnerCountryId.Value == selectedCountryId.Value;
                    string icon = log.WinnerCountryId.HasValue ? (ownWon ? "🏆" : "⚠") : "⚖";
                    string enemyName = GetCountryName(enemyCountryId);
                    string date = log.CreatedAt.ToLocalTime().ToString("dd.MM HH:mm", CultureInfo.InvariantCulture);
                    string text = $"{icon} #{log.BattleLogId} {date} • {enemyName}";
                    return (text, $"state_military_battle_log_{log.BattleLogId}");
                })
                .ToList();

            string infoText =
$@"📜 Журнал боїв
Записів: {FmtNumber(logs.Count)}
Сторінка: {FmtNumber(page + 1)}/{FmtNumber(totalPages)}";

            await SafeEditMessageTextAsync(
                botClient,
                chatId,
                msgId,
                infoText,
                StateKeyboardBuilder.MilitaryBattleLogsKeyboard(pageItems, page, totalPages),
                cancellationToken
            );
        }

        private async Task ShowMilitaryBattleLogInfo(ITelegramBotClient botClient, long userId, long chatId, int msgId, int battleLogId, CancellationToken cancellationToken)
        {
            var selectedCountryId = GetSelectedCountryIdForUser(userId);
            if (!selectedCountryId.HasValue)
                return;

            var log = MilitaryBattleLogsData.GetByIdForCountry(battleLogId, selectedCountryId.Value);
            if (log == null)
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "Запис журналу не знайдено.",
                    StateKeyboardBuilder.MilitaryBattleLogInfoKeyboard(),
                    cancellationToken
                );
                return;
            }

            var attacker = CountriesData.GetCountryById(log.AttackerCountryId);
            var defender = CountriesData.GetCountryById(log.DefenderCountryId);
            string attackerName = attacker == null || string.IsNullOrWhiteSpace(attacker.Name) ? $"#{log.AttackerCountryId}" : attacker.Name;
            string defenderName = defender == null || string.IsNullOrWhiteSpace(defender.Name) ? $"#{log.DefenderCountryId}" : defender.Name;
            string winnerText = !log.WinnerCountryId.HasValue
                ? "Нічия"
                : (log.WinnerCountryId.Value == log.AttackerCountryId ? attackerName : defenderName);
            static string Signed(float value) => value >= 0f ? $"+{value:0.#}" : $"{value:0.#}";

            string text =
$@"📜 Запис бою #{log.BattleLogId}
Дата: {log.CreatedAt.ToLocalTime():dd.MM.yyyy HH:mm}
Тип: {MilitaryBattleService.FormatResultTypeUk(log.ResultType)}
Місцевість: {MilitaryBattleService.FormatTerrainUk(log.Terrain)}
Тактики: {MilitaryBattleService.FormatTacticUk(log.AttackerTactic)} / {MilitaryBattleService.FormatTacticUk(log.DefenderTactic)}
Сторони: {attackerName} vs {defenderName}
Переможець: {winnerText}

Сила сторін: {FmtNumber((int)Math.Round(log.AttackerPower, MidpointRounding.AwayFromZero))} / {FmtNumber((int)Math.Round(log.DefenderPower, MidpointRounding.AwayFromZero))}
Війська на старті: {FmtNumber(log.AttackerTroopsStart)} / {FmtNumber(log.DefenderTroopsStart)}
Втрати: {FmtNumber(log.AttackerLosses)} / {FmtNumber(log.DefenderLosses)}
Втрати васалів: {FmtNumber(log.AttackerVassalLosses)} / {FmtNumber(log.DefenderVassalLosses)}
Мораль: {Signed(log.AttackerMoraleDelta)} / {Signed(log.DefenderMoraleDelta)}
Досвід: {Signed(log.AttackerExperienceGain)} / {Signed(log.DefenderExperienceGain)}

Підсумок:
{(string.IsNullOrWhiteSpace(log.Summary) ? "-" : log.Summary)}";

            await SafeEditMessageTextAsync(
                botClient,
                chatId,
                msgId,
                text,
                StateKeyboardBuilder.MilitaryBattleLogInfoKeyboard(),
                cancellationToken
            );
        }

        private static int NormalizeArmyTemplatePage(int page)
        {
            int totalPages = GetArmyTemplatePageCount();
            if (page < 0)
                return 0;
            if (page >= totalPages)
                return totalPages - 1;
            return page;
        }

        private static int GetArmyTemplatePageCount()
        {
            if (ArmyTemplateCatalog.Count == 0)
                return 1;
            return Math.Max(1, (int)Math.Ceiling(ArmyTemplateCatalog.Count / (double)ArmyTemplatePageSize));
        }

        private static List<ArmyTemplateDefinition> GetArmyTemplatePageItems(int page)
        {
            int safePage = NormalizeArmyTemplatePage(page);
            return ArmyTemplateCatalog
                .Skip(safePage * ArmyTemplatePageSize)
                .Take(ArmyTemplatePageSize)
                .ToList();
        }

        private static bool TryGetArmyTemplateDefinition(string key, out ArmyTemplateDefinition template)
        {
            template = ArmyTemplateCatalog.FirstOrDefault(t => string.Equals(t.Key, key, StringComparison.OrdinalIgnoreCase))
                ?? new ArmyTemplateDefinition();
            return !string.IsNullOrWhiteSpace(template.Key);
        }

        private static string ResolvePreferredMilitaryRace(CountryRecord? country)
        {
            if (country == null || country.CountryId <= 0)
                return "Human";

            var races = GetPopulationRaces(country);
            if (races.Count == 0)
                return "Human";

            string race = races
                .OrderByDescending(x => x.Value)
                .ThenBy(x => x.Key, StringComparer.OrdinalIgnoreCase)
                .Select(x => NormalizeUnitRace(x.Key))
                .FirstOrDefault(x => !string.IsNullOrWhiteSpace(x)) ?? "Human";

            race = NormalizeUnitRace(race);
            if (race.Length > 40)
                race = race.Substring(0, 40).Trim();

            return string.IsNullOrWhiteSpace(race) ? "Human" : race;
        }

        private static MilitaryUnitRecord BuildUnitFromArmyTemplate(int countryId, ArmyTemplateDefinition template)
        {
            return new MilitaryUnitRecord
            {
                CountryId = countryId,
                UnitName = template.UnitName,
                UnitLevel = template.UnitLevel,
                UnitRole = template.UnitRole,
                UnitType = template.UnitType,
                Race = template.Race,
                Soldiers = template.Soldiers,
                EquipmentTier = template.EquipmentTier,
                EquipmentQuality = template.EquipmentQuality,
                BattleExperience = template.BattleExperience,
                Fatigue = template.Fatigue,
                TrainingLevel = template.TrainingLevel,
                Morale = template.Morale,
                WeaponReadiness = template.WeaponReadiness,
                SupplyLevel = template.SupplyLevel,
                CommanderName = null,
                CommanderSkill = template.CommanderSkill,
                CommanderLoyalty = template.CommanderLoyalty,
                CommanderCorruption = template.CommanderCorruption,
                CommanderExperience = template.CommanderExperience,
                Status = template.Status,
                IsMobilized = false,
                FormationProgress = 0f,
                FormationBaseDays = template.FormationDays,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
        }

        private async Task ShowMilitaryUnitTemplates(ITelegramBotClient botClient, long userId, long chatId, int msgId, int page, CancellationToken cancellationToken)
        {
            var selectedCountryId = GetSelectedCountryIdForUser(userId);
            if (!selectedCountryId.HasValue)
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "Не знайдено країни.",
                    StateKeyboardBuilder.MilitaryUnitsKeyboard(),
                    cancellationToken
                );
                return;
            }

            var country = CountriesData.GetCountryById(selectedCountryId.Value);
            if (country == null)
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "Країну не знайдено.",
                    StateKeyboardBuilder.MilitaryUnitsKeyboard(),
                    cancellationToken
                );
                return;
            }

            int safePage = NormalizeArmyTemplatePage(page);
            int totalPages = GetArmyTemplatePageCount();
            var pageItems = GetArmyTemplatePageItems(safePage)
                .Select(t =>
                {
                    string text = $"{t.DisplayName} • {FmtNumber(t.Soldiers)} • {FmtNumber(t.CostGold)} зол. • {FmtNumber(t.FormationDays)} дн.";
                    return (text, $"state_military_unit_template_{t.Key}");
                })
                .ToList();

string info =
$@"📦 Готові шаблони армії
Казна: {FmtTreasury(country.Treasury, country.CountryId)}
Шаблонів: {FmtNumber(ArmyTemplateCatalog.Count)} | Сторінка: {FmtNumber(safePage + 1)}/{FmtNumber(totalPages)}

Оберіть шаблон, щоб подивитися склад, вартість і час формування.";

            await SafeEditMessageTextAsync(
                botClient,
                chatId,
                msgId,
                info,
                StateKeyboardBuilder.MilitaryUnitTemplatesKeyboard(pageItems, safePage, totalPages),
                cancellationToken
            );
        }

        private async Task ShowMilitaryUnitTemplateInfo(ITelegramBotClient botClient, long userId, long chatId, int msgId, string key, CancellationToken cancellationToken)
        {
            var selectedCountryId = GetSelectedCountryIdForUser(userId);
            if (!selectedCountryId.HasValue)
                return;

            if (!TryGetArmyTemplateDefinition(key, out var template))
            {
                await ShowMilitaryUnitTemplates(botClient, userId, chatId, msgId, 0, cancellationToken);
                return;
            }

            var country = CountriesData.GetCountryById(selectedCountryId.Value);
            if (country == null)
            {
                await ShowMilitaryUnitTemplates(botClient, userId, chatId, msgId, 0, cancellationToken);
                return;
            }

            var preview = BuildUnitFromArmyTemplate(selectedCountryId.Value, template);
            NormalizeMilitaryUnitRecord(preview);
            float previewPower = MilitaryBattleService.EstimateUnitPower(preview, "Plains", defending: false);
            bool canAfford = country.Treasury >= template.CostGold;
            var population = PopulationCalculator.GetPopulationSnapshot(country);
            long availableWorkforce = PopulationCalculator.ComputeCivilianWorkforce(population.WorkingAge, country.ArmySize, country.WarState);
            bool hasManpower = availableWorkforce >= template.Soldiers;

            string text =
$@"📦 Шаблон: {template.DisplayName}
Назва частини: {template.UnitName}
Рівень: {FormatUnitLevelUk(template.UnitLevel)} | Роль: {FormatUnitRoleUk(template.UnitRole)}
Тип: {FormatUnitTypeUk(template.UnitType)} | Раса: {FormatUnitRaceUk(template.Race)}
Кількість воїнів: {FmtNumber(template.Soldiers)}

Екіпірування: {FormatEquipmentTierUk(template.EquipmentTier)}
Якість спорядження: {template.EquipmentQuality:0.#}%
Бойовий досвід: {template.BattleExperience:0.#}%
Підготовка/мораль: {template.TrainingLevel:0.#}% / {template.Morale:0.#}%
Оціночна сила (рівнина, атака): {FmtNumber((int)Math.Round(previewPower, MidpointRounding.AwayFromZero))}

Вартість: {FmtNumber(template.CostGold)} золота
Час формування: {FmtNumber(template.FormationDays)} дн.
Казна: {FmtTreasury(country.Treasury, country.CountryId)} {(canAfford ? "✅" : "❌")}
Вільна робоча сила: {FmtNumber(availableWorkforce)} {(hasManpower ? "✅" : "❌")}";

            await SafeEditMessageTextAsync(
                botClient,
                chatId,
                msgId,
                text,
                StateKeyboardBuilder.MilitaryUnitTemplateInfoKeyboard(template.Key),
                cancellationToken
            );
        }

        private static bool TryChargeStateTreasuryForArmyTemplate(long userId, CountryRecord country, long costGold, string templateName, out string error)
        {
            error = string.Empty;
            if (country == null || costGold <= 0)
                return true;

            if (country.Treasury < costGold)
            {
                error = $"❌ Недостатньо коштів у казні. Потрібно: {FmtNumber(costGold)}.";
                return false;
            }

            country.Treasury -= costGold;
            CountriesData.UpdateCountry(country);
            EconomyMoneyLogData.AddLog(
                country.CountryId,
                userId,
                costGold,
                "Expense",
                $"Формування військ за шаблоном: {templateName}",
                country.TurnsSurvived);
            return true;
        }

        private static bool TryReserveCountryManpowerForUnit(CountryRecord country, int soldiers, out string error)
        {
            error = string.Empty;
            if (country == null)
            {
                error = "❌ Країну не знайдено.";
                return false;
            }

            int needed = Math.Max(0, soldiers);
            if (needed <= 0)
                return true;

            var population = PopulationCalculator.GetPopulationSnapshot(country);
            long availableWorkforce = PopulationCalculator.ComputeCivilianWorkforce(population.WorkingAge, country.ArmySize, country.WarState);
            if (availableWorkforce < needed)
            {
                error = $"❌ Недостатньо вільної робочої сили для набору. Потрібно: {FmtNumber(needed)}, доступно: {FmtNumber(availableWorkforce)}.";
                return false;
            }

            country.ArmySize = Math.Max(0, country.ArmySize + needed);
            country.AvailableWorkers = PopulationCalculator.ComputeCivilianWorkforce(population.WorkingAge, country.ArmySize, country.WarState);
            country.WorkRate = PopulationCalculator.ComputeWorkRate(country.Population, population.WorkingAge);
            country.LastUpdate = DateTime.UtcNow;
            CountriesData.UpdateCountry(country);
            return true;
        }

        private static void ReleaseCountryManpowerForUnit(CountryRecord country, int soldiers)
        {
            if (country == null)
                return;

            int released = Math.Max(0, soldiers);
            if (released <= 0)
                return;

            var population = PopulationCalculator.GetPopulationSnapshot(country);
            country.ArmySize = Math.Max(0, country.ArmySize - released);
            country.AvailableWorkers = PopulationCalculator.ComputeCivilianWorkforce(population.WorkingAge, country.ArmySize, country.WarState);
            country.WorkRate = PopulationCalculator.ComputeWorkRate(country.Population, population.WorkingAge);
            country.LastUpdate = DateTime.UtcNow;
            CountriesData.UpdateCountry(country);
        }

        private static void RefundStateTreasuryForArmyTemplate(long userId, CountryRecord country, long costGold, string templateName)
        {
            if (country == null || costGold <= 0)
                return;

            country.Treasury += costGold;
            CountriesData.UpdateCountry(country);
            EconomyMoneyLogData.AddLog(
                country.CountryId,
                userId,
                costGold,
                "Income",
                $"Повернення коштів за шаблон: {templateName}",
                country.TurnsSurvived);
        }

        private async Task CreateMilitaryUnitFromTemplate(ITelegramBotClient botClient, long userId, long chatId, int msgId, string key, CancellationToken cancellationToken)
        {
            var selectedCountryId = GetSelectedCountryIdForUser(userId);
            if (!selectedCountryId.HasValue)
                return;

            if (!TryGetArmyTemplateDefinition(key, out var template))
            {
                await ShowMilitaryUnitTemplates(botClient, userId, chatId, msgId, 0, cancellationToken);
                return;
            }

            var country = CountriesData.GetCountryById(selectedCountryId.Value);
            if (country == null)
            {
                await ShowMilitaryUnitTemplates(botClient, userId, chatId, msgId, 0, cancellationToken);
                return;
            }

            if (!TryReserveCountryManpowerForUnit(country, template.Soldiers, out string manpowerError))
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    manpowerError,
                    StateKeyboardBuilder.MilitaryUnitTemplateInfoKeyboard(template.Key),
                    cancellationToken
                );
                return;
            }

            if (!TryChargeStateTreasuryForArmyTemplate(userId, country, template.CostGold, template.DisplayName, out string chargeError))
            {
                ReleaseCountryManpowerForUnit(country, template.Soldiers);
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    chargeError,
                    StateKeyboardBuilder.MilitaryUnitTemplateInfoKeyboard(template.Key),
                    cancellationToken
                );
                return;
            }

            var unit = BuildUnitFromArmyTemplate(selectedCountryId.Value, template);
            NormalizeMilitaryUnitRecord(unit);
            int unitId = MilitaryUnitsData.AddUnit(unit);
            if (unitId <= 0)
            {
                RefundStateTreasuryForArmyTemplate(userId, country, template.CostGold, template.DisplayName);
                ReleaseCountryManpowerForUnit(country, template.Soldiers);
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "❌ Не вдалося створити частину за шаблоном.",
                    StateKeyboardBuilder.MilitaryUnitTemplateInfoKeyboard(template.Key),
                    cancellationToken
                );
                return;
            }

            string text =
$@"✅ Частину створено за шаблоном
Шаблон: {template.DisplayName}
Списано з казни: {FmtNumber(template.CostGold)} золота
Залучено рекрутів: {FmtNumber(template.Soldiers)}
Час формування: {FmtNumber(template.FormationDays)} дн.
Створена частина: #{unitId}";

            var keyboard = new InlineKeyboardMarkup(new[]
            {
                new[] { InlineKeyboardButton.WithCallbackData("👁 Відкрити частину", $"state_military_unitinfo_{unitId}") },
                new[] { InlineKeyboardButton.WithCallbackData("📦 Ще шаблони", "state_military_unit_templates"), InlineKeyboardButton.WithCallbackData("📄 Реєстр", "state_military_units_list") },
                new[] { InlineKeyboardButton.WithCallbackData("🔙 До частин", "state_military_units") }
            });

            await SafeEditMessageTextAsync(
                botClient,
                chatId,
                msgId,
                text,
                keyboard,
                cancellationToken
            );
        }

        private async Task ShowMilitaryUnitsMenu(ITelegramBotClient botClient, long userId, long chatId, int msgId, CancellationToken cancellationToken)
        {
            await SafeEditMessageTextAsync(
                botClient,
                chatId,
                msgId,
                "🪖 Управління військовими частинами",
                StateKeyboardBuilder.MilitaryUnitsKeyboard(),
                cancellationToken
            );
        }

        private async Task ShowMilitaryUnitsList(ITelegramBotClient botClient, long userId, long chatId, int msgId, int page, CancellationToken cancellationToken)
        {
            var selectedCountryId = GetSelectedCountryIdForUser(userId);
            if (!selectedCountryId.HasValue)
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "Не знайдено країни.",
                    StateKeyboardBuilder.MilitaryUnitsKeyboard(),
                    cancellationToken
                );
                return;
            }

            var units = MilitaryUnitsData.GetByCountry(selectedCountryId.Value);
            const int pageSize = 12;
            int totalPages = Math.Max(1, (int)Math.Ceiling(units.Count / (double)pageSize));
            page = Math.Max(0, Math.Min(page, totalPages - 1));

            var pageItems = units
                .OrderByDescending(u => Math.Max(0, u.Soldiers))
                .ThenBy(u => u.UnitId)
                .Skip(page * pageSize)
                .Take(pageSize)
                .Select(u =>
                {
                    string status = FormatUnitStatusUk(u.Status);
                    string name = string.IsNullOrWhiteSpace(u.UnitName) ? "Без назви" : u.UnitName.Trim();
                    string text = $"#{u.UnitId} {name} • {FormatUnitLevelUk(u.UnitLevel)} • {FormatUnitTypeUk(u.UnitType)} • {FmtNumber(u.Soldiers)} ({status})";
                    return (text, $"state_military_unitinfo_{u.UnitId}");
                })
                .ToList();

            string text =
$@"🪖 Реєстр військових частин
Усього частин: {FmtNumber(units.Count)}
Сторінка: {FmtNumber(page + 1)}/{FmtNumber(totalPages)}";

            await SafeEditMessageTextAsync(
                botClient,
                chatId,
                msgId,
                text,
                StateKeyboardBuilder.MilitaryUnitsListKeyboard(pageItems, page, totalPages),
                cancellationToken
            );
        }

        private async Task ShowMilitaryUnitInfo(ITelegramBotClient botClient, long userId, long chatId, int msgId, int unitId, CancellationToken cancellationToken)
        {
            var selectedCountryId = GetSelectedCountryIdForUser(userId);
            if (!selectedCountryId.HasValue)
                return;

            var unit = MilitaryUnitsData.GetByCountry(selectedCountryId.Value).FirstOrDefault(u => u.UnitId == unitId);
            if (unit == null)
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "Частину не знайдено.",
                    StateKeyboardBuilder.MilitaryUnitsKeyboard(),
                    cancellationToken
                );
                return;
            }

            string commanderName = string.IsNullOrWhiteSpace(unit.CommanderName) ? "Немає" : unit.CommanderName!;
            float estimatedPower = MilitaryBattleService.EstimateUnitPower(unit, "Plains", defending: false);
            string text =
$@"🪖 Частина #{unit.UnitId}
Назва: {(string.IsNullOrWhiteSpace(unit.UnitName) ? "Частина" : unit.UnitName.Trim())}
Рівень: {FormatUnitLevelUk(unit.UnitLevel)}
Роль: {FormatUnitRoleUk(unit.UnitRole)}
Тип: {FormatUnitTypeUk(unit.UnitType)}
Раса: {FormatUnitRaceUk(unit.Race)}
Солдати: {FmtNumber(unit.Soldiers)}
Статус: {FormatUnitStatusUk(unit.Status)}
Мобілізована: {(unit.IsMobilized ? "так" : "ні")}

Клас спорядження: {FormatEquipmentTierUk(unit.EquipmentTier)}
Якість спорядження: {EconomyCalculator.Clamp(unit.EquipmentQuality, 0f, 100f):0.#}%
Бойовий досвід: {EconomyCalculator.Clamp(unit.BattleExperience, 0f, 100f):0.#}%
Втома: {EconomyCalculator.Clamp(unit.Fatigue, 0f, 100f):0.#}%
Оціночна сила (рівнина, атака): {FmtNumber((int)Math.Round(estimatedPower, MidpointRounding.AwayFromZero))}

Підготовка: {EconomyCalculator.Clamp(unit.TrainingLevel, 0f, 100f):0.#}%
Мораль: {EconomyCalculator.Clamp(unit.Morale, 0f, 100f):0.#}%
Озброєння: {EconomyCalculator.Clamp(unit.WeaponReadiness, 0f, 100f):0.#}%
Забезпечення: {EconomyCalculator.Clamp(unit.SupplyLevel, 0f, 100f):0.#}%
Прогрес формування: {(EconomyCalculator.Clamp(unit.FormationProgress, 0f, 1f) * 100f):0.#}%
Базовий час формування: {FmtNumber(unit.FormationBaseDays)} дн.

Командир: {commanderName}
Навички/лояльність/корупція/досвід:
{EconomyCalculator.Clamp(unit.CommanderSkill, 0f, 100f):0.#}% / {EconomyCalculator.Clamp(unit.CommanderLoyalty, 0f, 100f):0.#}% / {EconomyCalculator.Clamp(unit.CommanderCorruption, 0f, 100f):0.#}% / {EconomyCalculator.Clamp(unit.CommanderExperience, 0f, 100f):0.#}%";

            await SafeEditMessageTextAsync(
                botClient,
                chatId,
                msgId,
                text,
                StateKeyboardBuilder.MilitaryUnitInfoKeyboard(unit.UnitId),
                cancellationToken
            );
        }

        private async Task ShowMilitaryUnitEditMenu(ITelegramBotClient botClient, long userId, long chatId, int msgId, int unitId, CancellationToken cancellationToken)
        {
            await SafeEditMessageTextAsync(
                botClient,
                chatId,
                msgId,
                $"✏ Редагування частини #{unitId}",
                StateKeyboardBuilder.MilitaryUnitEditKeyboard(unitId),
                cancellationToken
            );
        }

        private async Task DeleteMilitaryUnit(ITelegramBotClient botClient, long userId, long chatId, int msgId, int unitId, CancellationToken cancellationToken)
        {
            var selectedCountryId = GetSelectedCountryIdForUser(userId);
            if (!selectedCountryId.HasValue)
                return;

            var unit = MilitaryUnitsData.GetByCountry(selectedCountryId.Value).FirstOrDefault(u => u.UnitId == unitId);
            bool deleted = MilitaryUnitsData.DeleteUnit(unitId, selectedCountryId.Value);
            if (deleted && unit != null && unit.Soldiers > 0)
            {
                var country = CountriesData.GetCountryById(selectedCountryId.Value);
                if (country != null)
                    ReleaseCountryManpowerForUnit(country, unit.Soldiers);
            }
            string text = deleted ? $"✅ Частину #{unitId} видалено." : "❌ Не вдалося видалити частину.";
            await SafeEditMessageTextAsync(
                botClient,
                chatId,
                msgId,
                text,
                StateKeyboardBuilder.MilitaryUnitsKeyboard(),
                cancellationToken
            );
        }

        private async Task StartMilitaryUnitCreation(ITelegramBotClient botClient, long userId, CancellationToken cancellationToken)
        {
            var selectedCountryId = GetSelectedCountryIdForUser(userId);
            if (!selectedCountryId.HasValue)
            {
                await botClient.SendTextMessageAsync(userId, "Не знайдено країни.", cancellationToken: cancellationToken);
                return;
            }

            var country = CountriesData.GetCountryById(selectedCountryId.Value);
            string preferredRace = ResolvePreferredMilitaryRace(country);

            ClearEconomyInput(userId);
            EconomyInputState[userId] = EconomyInputMode.AddMilitaryUnit;
            EconomyInputStep[userId] = 0;
            EconomyMilitaryUnitBuffer[userId] = new MilitaryUnitRecord
            {
                CountryId = selectedCountryId.Value,
                UnitName = "Нова частина",
                UnitLevel = "Squad",
                UnitRole = "Field",
                UnitType = "Infantry",
                Race = preferredRace,
                Soldiers = 10,
                EquipmentTier = "Standard",
                EquipmentQuality = 50f,
                BattleExperience = 15f,
                Fatigue = 0f,
                TrainingLevel = 35f,
                Morale = 55f,
                WeaponReadiness = 55f,
                SupplyLevel = 60f,
                CommanderSkill = 50f,
                CommanderLoyalty = 50f,
                CommanderCorruption = 0f,
                CommanderExperience = 15f,
                Status = "Forming",
                IsMobilized = false,
                FormationProgress = 0f,
                FormationBaseDays = 7,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await SendModeFieldPromptAsync(
                botClient,
                userId,
                EconomyInputMode.AddMilitaryUnit,
                MilitaryUnitCreationFields[0].FieldName,
                MilitaryUnitCreationFields[0].Prompt,
                cancellationToken);
        }

        private async Task CreateQuickCompanyUnit(ITelegramBotClient botClient, long userId, long chatId, int msgId, CancellationToken cancellationToken)
        {
            var selectedCountryId = GetSelectedCountryIdForUser(userId);
            if (!selectedCountryId.HasValue)
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "Не знайдено країни.",
                    StateKeyboardBuilder.MilitaryUnitsKeyboard(),
                    cancellationToken
                );
                return;
            }

            var country = CountriesData.GetCountryById(selectedCountryId.Value);
            if (country == null)
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "Країну не знайдено.",
                    StateKeyboardBuilder.MilitaryUnitsKeyboard(),
                    cancellationToken
                );
                return;
            }

            string preferredRace = ResolvePreferredMilitaryRace(country);

            var unit = new MilitaryUnitRecord
            {
                CountryId = selectedCountryId.Value,
                UnitName = "Рота швидкого набору",
                UnitLevel = "Company",
                UnitRole = "Field",
                UnitType = "Infantry",
                Race = preferredRace,
                Soldiers = 120,
                EquipmentTier = "Standard",
                EquipmentQuality = 52f,
                BattleExperience = 12f,
                Fatigue = 4f,
                TrainingLevel = 35f,
                Morale = 55f,
                WeaponReadiness = 55f,
                SupplyLevel = 60f,
                CommanderName = null,
                CommanderSkill = 50f,
                CommanderLoyalty = 50f,
                CommanderCorruption = 5f,
                CommanderExperience = 15f,
                Status = "Forming",
                IsMobilized = false,
                FormationProgress = 0f,
                FormationBaseDays = 7,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            NormalizeMilitaryUnitRecord(unit);

            if (!TryReserveCountryManpowerForUnit(country, unit.Soldiers, out string manpowerError))
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    manpowerError,
                    StateKeyboardBuilder.MilitaryUnitsKeyboard(),
                    cancellationToken
                );
                return;
            }

            int id = MilitaryUnitsData.AddUnit(unit);
            if (id > 0)
            {
                await ShowMilitaryUnitInfo(botClient, userId, chatId, msgId, id, cancellationToken);
                return;
            }

            ReleaseCountryManpowerForUnit(country, unit.Soldiers);

            await SafeEditMessageTextAsync(
                botClient,
                chatId,
                msgId,
                "❌ Не вдалося створити роту.",
                StateKeyboardBuilder.MilitaryUnitsKeyboard(),
                cancellationToken
            );
        }

        private async Task HandleMilitaryUnitInput(ITelegramBotClient botClient, long userId, string input, CancellationToken cancellationToken)
        {
            int step = EconomyInputStep.TryGetValue(userId, out int currentStep) ? currentStep : 0;
            if (step < 0 || step >= MilitaryUnitCreationFields.Count)
                step = 0;

            if (!EconomyMilitaryUnitBuffer.TryGetValue(userId, out var unit))
            {
                var selectedCountryId = GetSelectedCountryIdForUser(userId);
                if (!selectedCountryId.HasValue)
                {
                    await SendHelperMessageAsync(botClient, userId, "Не знайдено країни.", cancellationToken);
                    return;
                }

                unit = new MilitaryUnitRecord { CountryId = selectedCountryId.Value };
                EconomyMilitaryUnitBuffer[userId] = unit;
            }

            var field = MilitaryUnitCreationFields[step];
            if (field.FieldName == "CommanderName" && (input == "-" || string.IsNullOrWhiteSpace(input)))
            {
                unit.CommanderName = null;
            }
            else if (!TryAssignByKind(unit, field.FieldName, field.Kind, input, out string error))
            {
                await SendHelperMessageAsync(botClient, userId, error, cancellationToken);
                return;
            }

            EconomyInputStep[userId] = step + 1;
            if (EconomyInputStep[userId] >= MilitaryUnitCreationFields.Count)
            {
                NormalizeMilitaryUnitRecord(unit);
                var country = CountriesData.GetCountryById(unit.CountryId);
                if (country == null)
                {
                    await SendHelperMessageAsync(botClient, userId, "❌ Країну не знайдено.", cancellationToken);
                    return;
                }

                if (!TryReserveCountryManpowerForUnit(country, unit.Soldiers, out string manpowerError))
                {
                    await SendHelperMessageAsync(botClient, userId, manpowerError, cancellationToken);
                    return;
                }

                int id = MilitaryUnitsData.AddUnit(unit);
                if (id <= 0)
                    ReleaseCountryManpowerForUnit(country, unit.Soldiers);
                await CleanupHelperMessagesAsync(botClient, userId, cancellationToken);
                ClearEconomyInput(userId);
                await botClient.SendTextMessageAsync(
                    userId,
                    id > 0 ? $"✅ Частину створено: #{id} ({FormatUnitLevelUk(unit.UnitLevel)} / {FormatUnitTypeUk(unit.UnitType)})" : "❌ Не вдалося створити частину.",
                    cancellationToken: cancellationToken
                );
                return;
            }

            var nextUnitField = MilitaryUnitCreationFields[EconomyInputStep[userId]];
            await SendModeFieldPromptAsync(
                botClient,
                userId,
                EconomyInputMode.AddMilitaryUnit,
                nextUnitField.FieldName,
                nextUnitField.Prompt,
                cancellationToken);
        }

        private async Task ShowMilitaryVassalsMenu(ITelegramBotClient botClient, long userId, long chatId, int msgId, CancellationToken cancellationToken)
        {
            await SafeEditMessageTextAsync(
                botClient,
                chatId,
                msgId,
                "🏰 Управління васалами та даниною",
                StateKeyboardBuilder.MilitaryVassalsKeyboard(),
                cancellationToken
            );
        }

        private async Task ShowMilitaryVassalsList(ITelegramBotClient botClient, long userId, long chatId, int msgId, int page, CancellationToken cancellationToken)
        {
            var selectedCountryId = GetSelectedCountryIdForUser(userId);
            if (!selectedCountryId.HasValue)
                return;

            var obligations = VassalMilitaryObligationsData.GetByCountry(selectedCountryId.Value);
            const int pageSize = 12;
            int totalPages = Math.Max(1, (int)Math.Ceiling(obligations.Count / (double)pageSize));
            page = Math.Max(0, Math.Min(page, totalPages - 1));

            var pageItems = obligations
                .OrderByDescending(o => o.IsActive)
                .ThenByDescending(o => o.RegimentsProvided)
                .ThenBy(o => o.SubjectName)
                .Skip(page * pageSize)
                .Take(pageSize)
                .Select(o =>
                {
                    string marker = o.IsActive ? "✅" : "⏸";
                    string subject = string.IsNullOrWhiteSpace(o.SubjectName) ? $"#{o.ObligationId}" : o.SubjectName;
                    string text = $"{marker} {subject} • полки {FmtNumber(o.RegimentsProvided)} • данина {FmtNumberCompact(o.FundingPerTurn)}";
                    return (text, $"state_military_vassalinfo_{o.ObligationId}");
                })
                .ToList();

            string text =
$@"🏰 Реєстр васалів
Усього зобов'язань: {FmtNumber(obligations.Count)}
Сторінка: {FmtNumber(page + 1)}/{FmtNumber(totalPages)}";

            await SafeEditMessageTextAsync(
                botClient,
                chatId,
                msgId,
                text,
                StateKeyboardBuilder.MilitaryVassalsListKeyboard(pageItems, page, totalPages),
                cancellationToken
            );
        }

        private async Task ShowMilitaryVassalInfo(ITelegramBotClient botClient, long userId, long chatId, int msgId, int obligationId, CancellationToken cancellationToken)
        {
            var selectedCountryId = GetSelectedCountryIdForUser(userId);
            if (!selectedCountryId.HasValue)
                return;

            var obligation = VassalMilitaryObligationsData.GetByCountry(selectedCountryId.Value)
                .FirstOrDefault(o => o.ObligationId == obligationId);
            if (obligation == null)
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "Зобов'язання не знайдено.",
                    StateKeyboardBuilder.MilitaryVassalsKeyboard(),
                    cancellationToken
                );
                return;
            }

            string subject = string.IsNullOrWhiteSpace(obligation.SubjectName) ? $"#{obligation.ObligationId}" : obligation.SubjectName;
            string text =
$@"🏰 Васал / колонія #{obligation.ObligationId}
Назва: {subject}
Статус: {(obligation.IsActive ? "активне" : "призупинене")}

Полки: {FmtNumber(obligation.RegimentsProvided)}
Данина: {FmtNumberCompact(obligation.FundingPerTurn)}
Лояльність: {EconomyCalculator.Clamp(obligation.Loyalty, 0f, 100f):0.#}%
Ризик повстання: {EconomyCalculator.Clamp(obligation.RebellionRisk, 0f, 100f):0.#}%";

            await SafeEditMessageTextAsync(
                botClient,
                chatId,
                msgId,
                text,
                StateKeyboardBuilder.MilitaryVassalInfoKeyboard(obligation.ObligationId, obligation.IsActive),
                cancellationToken
            );
        }

        private async Task ShowMilitaryVassalEditMenu(ITelegramBotClient botClient, long userId, long chatId, int msgId, int obligationId, CancellationToken cancellationToken)
        {
            await SafeEditMessageTextAsync(
                botClient,
                chatId,
                msgId,
                $"✏ Редагування васала #{obligationId}",
                StateKeyboardBuilder.MilitaryVassalEditKeyboard(obligationId),
                cancellationToken
            );
        }

        private async Task DeleteMilitaryVassal(ITelegramBotClient botClient, long userId, long chatId, int msgId, int obligationId, CancellationToken cancellationToken)
        {
            var selectedCountryId = GetSelectedCountryIdForUser(userId);
            if (!selectedCountryId.HasValue)
                return;

            bool deleted = VassalMilitaryObligationsData.Delete(obligationId, selectedCountryId.Value);
            string text = deleted ? $"✅ Васала #{obligationId} видалено." : "❌ Не вдалося видалити запис.";

            await SafeEditMessageTextAsync(
                botClient,
                chatId,
                msgId,
                text,
                StateKeyboardBuilder.MilitaryVassalsKeyboard(),
                cancellationToken
            );
        }

        private async Task ToggleMilitaryVassalActive(ITelegramBotClient botClient, long userId, long chatId, int msgId, int obligationId, CancellationToken cancellationToken)
        {
            var selectedCountryId = GetSelectedCountryIdForUser(userId);
            if (!selectedCountryId.HasValue)
                return;

            var obligation = VassalMilitaryObligationsData.GetByCountry(selectedCountryId.Value)
                .FirstOrDefault(o => o.ObligationId == obligationId);
            if (obligation == null)
                return;

            obligation.IsActive = !obligation.IsActive;
            obligation.LastUpdated = DateTime.UtcNow;
            VassalMilitaryObligationsData.Update(obligation);

            await ShowMilitaryVassalInfo(botClient, userId, chatId, msgId, obligationId, cancellationToken);
        }

        private async Task StartMilitaryVassalCreation(ITelegramBotClient botClient, long userId, CancellationToken cancellationToken)
        {
            var selectedCountryId = GetSelectedCountryIdForUser(userId);
            if (!selectedCountryId.HasValue)
            {
                await botClient.SendTextMessageAsync(userId, "Не знайдено країни.", cancellationToken: cancellationToken);
                return;
            }

            ClearEconomyInput(userId);
            EconomyInputState[userId] = EconomyInputMode.AddVassalObligation;
            EconomyInputStep[userId] = 0;
            EconomyVassalBuffer[userId] = new VassalMilitaryObligationRecord
            {
                CountryId = selectedCountryId.Value,
                SubjectName = string.Empty,
                RegimentsProvided = 0,
                FundingPerTurn = 0m,
                Loyalty = 60f,
                RebellionRisk = 5f,
                IsActive = true,
                LastUpdated = DateTime.UtcNow
            };

            await SendModeFieldPromptAsync(
                botClient,
                userId,
                EconomyInputMode.AddVassalObligation,
                VassalFields[0].FieldName,
                VassalFields[0].Prompt,
                cancellationToken);
        }

        private async Task HandleVassalInput(ITelegramBotClient botClient, long userId, string input, CancellationToken cancellationToken)
        {
            int step = EconomyInputStep.TryGetValue(userId, out int currentStep) ? currentStep : 0;
            if (step < 0 || step >= VassalFields.Count)
                step = 0;

            if (!EconomyVassalBuffer.TryGetValue(userId, out var obligation))
            {
                var selectedCountryId = GetSelectedCountryIdForUser(userId);
                if (!selectedCountryId.HasValue)
                {
                    await SendHelperMessageAsync(botClient, userId, "Не знайдено країни.", cancellationToken);
                    return;
                }

                obligation = new VassalMilitaryObligationRecord
                {
                    CountryId = selectedCountryId.Value,
                    IsActive = true
                };
                EconomyVassalBuffer[userId] = obligation;
            }

            var field = VassalFields[step];
            if (!TryAssignByKind(obligation, field.FieldName, field.Kind, input, out string error))
            {
                await SendHelperMessageAsync(botClient, userId, error, cancellationToken);
                return;
            }

            EconomyInputStep[userId] = step + 1;
            if (EconomyInputStep[userId] >= VassalFields.Count)
            {
                NormalizeVassalRecord(obligation);
                int id = VassalMilitaryObligationsData.Add(obligation);
                await CleanupHelperMessagesAsync(botClient, userId, cancellationToken);
                ClearEconomyInput(userId);
                await botClient.SendTextMessageAsync(
                    userId,
                    id > 0 ? $"✅ Васальне зобов'язання створено: #{id}" : "❌ Не вдалося створити зобов'язання.",
                    cancellationToken: cancellationToken
                );
                return;
            }

            var nextVassalField = VassalFields[EconomyInputStep[userId]];
            await SendModeFieldPromptAsync(
                botClient,
                userId,
                EconomyInputMode.AddVassalObligation,
                nextVassalField.FieldName,
                nextVassalField.Prompt,
                cancellationToken);
        }

        private async Task ShowMilitaryVassalCountrySelect(ITelegramBotClient botClient, long userId, long chatId, int msgId, int page, CancellationToken cancellationToken)
        {
            var selectedCountryId = GetSelectedCountryIdForUser(userId);
            if (!selectedCountryId.HasValue)
                return;

            var countries = CountriesData.GetAllCountries()
                .Where(c => c.CountryId != selectedCountryId.Value)
                .OrderBy(c => c.Name)
                .ToList();

            const int pageSize = 12;
            int totalPages = Math.Max(1, (int)Math.Ceiling(countries.Count / (double)pageSize));
            page = Math.Max(0, Math.Min(page, totalPages - 1));
            var pageCountries = countries.Skip(page * pageSize).Take(pageSize).ToList();

            await SafeEditMessageTextAsync(
                botClient,
                chatId,
                msgId,
                "🌍 Призначення васала з переліку країн",
                StateKeyboardBuilder.MilitaryVassalCountrySelectKeyboard(pageCountries, page, totalPages),
                cancellationToken
            );
        }

        private async Task AddMilitaryVassalFromCountry(ITelegramBotClient botClient, long userId, long chatId, int msgId, int targetCountryId, CancellationToken cancellationToken)
        {
            var selectedCountryId = GetSelectedCountryIdForUser(userId);
            if (!selectedCountryId.HasValue)
                return;

            var target = CountriesData.GetCountryById(targetCountryId);
            if (target == null)
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "Країну не знайдено.",
                    StateKeyboardBuilder.MilitaryVassalsKeyboard(),
                    cancellationToken
                );
                return;
            }

            var existing = VassalMilitaryObligationsData.GetByCountry(selectedCountryId.Value)
                .FirstOrDefault(v => string.Equals(v.SubjectName, target.Name, StringComparison.OrdinalIgnoreCase));
            if (existing != null)
            {
                await ShowMilitaryVassalInfo(botClient, userId, chatId, msgId, existing.ObligationId, cancellationToken);
                return;
            }

            var record = new VassalMilitaryObligationRecord
            {
                CountryId = selectedCountryId.Value,
                SubjectName = target.Name,
                RegimentsProvided = 1,
                FundingPerTurn = 100m,
                Loyalty = 60f,
                RebellionRisk = 8f,
                IsActive = true,
                LastUpdated = DateTime.UtcNow
            };

            int id = VassalMilitaryObligationsData.Add(record);
            if (id > 0)
            {
                await ShowMilitaryVassalInfo(botClient, userId, chatId, msgId, id, cancellationToken);
                return;
            }

            await SafeEditMessageTextAsync(
                botClient,
                chatId,
                msgId,
                "❌ Не вдалося додати васала.",
                StateKeyboardBuilder.MilitaryVassalsKeyboard(),
                cancellationToken
            );
        }

        private static void NormalizeCompanyRecord(EconomyCompanyRecord company)
        {
            company.Name = string.IsNullOrWhiteSpace(company.Name) ? "Компанія" : company.Name.Trim();
            company.OwnerName = string.IsNullOrWhiteSpace(company.OwnerName) || company.OwnerName == "-" ? null : company.OwnerName.Trim();
            company.ProductionType = NormalizeProductionType(company.ProductionType);
            company.JobsOffered = Math.Max(0, company.JobsOffered);
            company.BaseSalary = Math.Max(0m, decimal.Round(company.BaseSalary, 4, MidpointRounding.AwayFromZero));
            company.TaxRate = ClampDecimal(decimal.Round(company.TaxRate, 4, MidpointRounding.AwayFromZero), 0m, 0.95m);
            company.Budget = Math.Max(0m, decimal.Round(company.Budget, 2, MidpointRounding.AwayFromZero));
            company.MilitaryProductionShare = EconomyCalculator.Clamp(company.MilitaryProductionShare, 0f, 100f);
            company.InputResourceType = string.IsNullOrWhiteSpace(company.InputResourceType) || company.InputResourceType == "-"
                ? null
                : company.InputResourceType.Trim();
            company.InputAmount = Math.Max(0m, decimal.Round(company.InputAmount, 4, MidpointRounding.AwayFromZero));
            company.UpdatedAt = DateTime.UtcNow;
            if (company.CreatedAt == default)
                company.CreatedAt = DateTime.UtcNow;
        }

        private static void NormalizeMilitaryUnitRecord(MilitaryUnitRecord unit)
        {
            unit.UnitName = string.IsNullOrWhiteSpace(unit.UnitName) ? "Частина" : unit.UnitName.Trim();
            unit.UnitLevel = NormalizeUnitLevelKey(unit.UnitLevel);
            unit.UnitRole = NormalizeUnitRole(unit.UnitRole);
            unit.UnitType = NormalizeUnitType(unit.UnitType);
            unit.Race = NormalizeUnitRace(unit.Race);
            if (unit.Race.Length > 40)
                unit.Race = unit.Race.Substring(0, 40).Trim();
            if (string.IsNullOrWhiteSpace(unit.Race))
                unit.Race = "Human";
            unit.EquipmentTier = NormalizeEquipmentTier(unit.EquipmentTier);
            unit.Soldiers = Math.Max(1, unit.Soldiers);
            unit.EquipmentQuality = EconomyCalculator.Clamp(unit.EquipmentQuality, 0f, 100f);
            unit.BattleExperience = EconomyCalculator.Clamp(unit.BattleExperience, 0f, 100f);
            unit.Fatigue = EconomyCalculator.Clamp(unit.Fatigue, 0f, 100f);
            unit.TrainingLevel = EconomyCalculator.Clamp(unit.TrainingLevel, 0f, 100f);
            unit.Morale = EconomyCalculator.Clamp(unit.Morale, 0f, 100f);
            unit.WeaponReadiness = EconomyCalculator.Clamp(unit.WeaponReadiness, 0f, 100f);
            unit.SupplyLevel = EconomyCalculator.Clamp(unit.SupplyLevel, 0f, 100f);
            unit.CommanderName = string.IsNullOrWhiteSpace(unit.CommanderName) || unit.CommanderName == "-" ? null : unit.CommanderName.Trim();
            unit.CommanderSkill = EconomyCalculator.Clamp(unit.CommanderSkill, 0f, 100f);
            unit.CommanderLoyalty = EconomyCalculator.Clamp(unit.CommanderLoyalty, 0f, 100f);
            unit.CommanderCorruption = EconomyCalculator.Clamp(unit.CommanderCorruption, 0f, 100f);
            unit.CommanderExperience = EconomyCalculator.Clamp(unit.CommanderExperience, 0f, 100f);
            unit.Status = NormalizeUnitStatus(unit.Status);
            unit.FormationProgress = EconomyCalculator.Clamp(unit.FormationProgress, 0f, 1f);
            unit.FormationBaseDays = Math.Max(1, unit.FormationBaseDays);
            unit.UpdatedAt = DateTime.UtcNow;
            if (unit.CreatedAt == default)
                unit.CreatedAt = DateTime.UtcNow;
        }

        private static void NormalizeVassalRecord(VassalMilitaryObligationRecord obligation)
        {
            obligation.SubjectName = string.IsNullOrWhiteSpace(obligation.SubjectName) ? "Васал" : obligation.SubjectName.Trim();
            obligation.RegimentsProvided = Math.Max(0, obligation.RegimentsProvided);
            obligation.FundingPerTurn = Math.Max(0m, decimal.Round(obligation.FundingPerTurn, 2, MidpointRounding.AwayFromZero));
            obligation.Loyalty = EconomyCalculator.Clamp(obligation.Loyalty, 0f, 100f);
            obligation.RebellionRisk = EconomyCalculator.Clamp(obligation.RebellionRisk, 0f, 100f);
            obligation.LastUpdated = DateTime.UtcNow;
        }

        private static string NormalizeProductionType(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return "General";

            string value = raw.Trim().ToLowerInvariant();
            return value switch
            {
                "general" or "загальне" => "General",
                "food" or "харчі" => "Food",
                "industry" or "промисловість" => "Industry",
                "services" or "послуги" => "Services",
                "logistics" or "логістика" => "Logistics",
                "weapons" or "arms" or "зброя" or "озброєння" => "Weapons",
                "militaryindustry" or "military" or "військове" or "військпром" or "оборонпром" => "MilitaryIndustry",
                "siegeworks" or "siege" or "облогова" or "облогове" => "SiegeWorks",
                "arcanearms" or "аркан-зброя" or "арканзброя" or "магічназброя" => "ArcaneArms",
                _ => raw.Trim()
            };
        }

        private static string FormatProductionTypeUk(string? productionType)
        {
            if (string.IsNullOrWhiteSpace(productionType))
                return "Загальне";

            string value = productionType.Trim().ToLowerInvariant();
            return value switch
            {
                "general" => "Загальне",
                "weapons" or "arms" => "Озброєння",
                "militaryindustry" or "military" => "Військпром",
                "siegeworks" => "Облогове виробництво",
                "arcanearms" => "Аркан-зброя",
                "food" => "Харчі",
                "industry" => "Промисловість",
                "logistics" => "Логістика",
                "services" => "Послуги",
                _ => productionType.Trim()
            };
        }

        private static string NormalizeUnitType(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return "Infantry";

            string value = raw.Trim().ToLowerInvariant();
            return value switch
            {
                "infantry" or "піхота" => "Infantry",
                "spearmen" or "копійники" => "Spearmen",
                "archers" or "лучники" => "Archers",
                "crossbowmen" or "арбалетники" => "Crossbowmen",
                "cavalry" or "кавалерія" => "Cavalry",
                "heavyinfantry" or "heavy" or "важка піхота" or "важка" => "HeavyInfantry",
                "magecorps" or "mages" or "маги" => "MageCorps",
                "siege" or "облогові" or "артилерія" => "Siege",
                "scouts" or "розвідники" => "Scouts",
                "navalfleet" or "fleet" or "флот" => "NavalFleet",
                "support" or "підтримка" => "Support",
                "механізована" => "HeavyInfantry",
                "гвардія" => "HeavyInfantry",
                "броня" or "бронетехніка" => "HeavyInfantry",
                _ => raw.Trim()
            };
        }

        private static string NormalizeUnitStatus(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return "Forming";

            string value = raw.Trim().ToLowerInvariant();
            return value switch
            {
                "forming" or "формується" => "Forming",
                "active" or "активна" => "Active",
                "reserve" or "резерв" => "Reserve",
                "routed" or "розбита" or "відступає" => "Routed",
                "destroyed" or "знищена" => "Destroyed",
                _ => raw.Trim()
            };
        }
    }
}
