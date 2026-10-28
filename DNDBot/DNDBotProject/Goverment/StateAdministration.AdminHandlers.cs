#nullable enable
using DNDBot.DataBase;
using DNDBot.Goverment.Economy;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;

namespace DNDBot.Goverment
{
    public partial class StateAdministration : BotBodyRealisation
    {
        private const string PopulationRacesConfigKey = "PopulationRaces";
        private const string DefaultPopulationRaceName = "Люди";

        private bool HasAdminAccess(long userId)
        {
            var selectedCountryId = GetSelectedCountryIdForUser(userId);
            return userId == masterID || IsSelectedCountryOwner(userId, selectedCountryId);
        }

        private async Task ShowAdminMenu(ITelegramBotClient botClient, long userId, long chatId, int msgId, CancellationToken cancellationToken)
        {
            if (!HasAdminAccess(userId))
            {
                await SafeEditMessageTextAsync(botClient, chatId, msgId, "Нема доступу.", StateKeyboardBuilder.MainStateKeyboard(userId), cancellationToken);
                return;
            }

            var selectedCountryId = GetSelectedCountryIdForUser(userId);
            if (!selectedCountryId.HasValue)
            {
                await SafeEditMessageTextAsync(botClient, chatId, msgId, "Не знайдено країни. Оберіть країну або призначте CountryId персонажу.", StateKeyboardBuilder.PublicOverviewKeyboard(), cancellationToken);
                return;
            }

            var provider = new StateDataProvider();
            var state = provider.GetStateData(userId, selectedCountryId);
            string text =
$@"⚙ Державне управління
Країна: {state.CountryName}

Оберіть блок управління.";

            await SafeEditMessageTextAsync(botClient, chatId, msgId, text, StateKeyboardBuilder.AdminMenuKeyboard(userId == masterID), cancellationToken);
            LastStateViewByUser[userId] = "admin";
        }

        private async Task ShowAdminEconomyMenu(ITelegramBotClient botClient, long userId, long chatId, int msgId, CancellationToken cancellationToken)
        {
            var state = GetStateOrFallback(userId);
            int countryId = GetSelectedCountryIdForUser(userId) ?? 0;
            var budget = EconomyBudgetData.GetByCountry(countryId);
            decimal civilianSalary = EconomyConfigData.GetDecimalForCountry(countryId, "CompanyBaseSalary", EconomyConfigData.GetDecimalForCountry(countryId, "BaseWage", 1m));
            decimal deputySalary = EconomyConfigData.GetDecimalForCountry(countryId, "DeputySalaryBase", 5m);
            string text =
$@"💰 Економіка та фінанси
Казна: {FmtTreasury(state.Treasury, countryId)}
Податок: {state.TaxLevel}%
Інфляція: {state.Inflation:0.##}%
Доходи / витрати: {FmtNumberCompact(state.IncomePerTurn)} / {FmtNumberCompact(state.ExpensesPerTurn)}
ЗП людям (база): {FmtNumberCompact(civilianSalary)}
ЗП депутатам (за 1): {FmtNumberCompact(deputySalary)}

Бюджет (%): інфраструктура {budget.InfrastructureShare * 100:0.#}, армія {budget.MilitaryShare * 100:0.#}, наука {budget.ScienceShare * 100:0.#}, соціалка {budget.SocialShare * 100:0.#}

Для гравців зміна зарплат робиться через парламентські закони.";

            await SafeEditMessageTextAsync(botClient, chatId, msgId, text, StateKeyboardBuilder.AdminEconomyKeyboard(), cancellationToken);
            LastStateViewByUser[userId] = "admin_economy";
        }

        private async Task ShowAdminSocialMenu(ITelegramBotClient botClient, long userId, long chatId, int msgId, CancellationToken cancellationToken)
        {
            var state = GetStateOrFallback(userId);
            string text =
$@"👥 Соціальна політика
Стабільність: {StatusEmoji(state.Stability)} {state.Stability:0.##}%
Щастя: {StatusEmoji(state.Happiness)} {state.Happiness:0.##}%
Корупція: {StatusEmoji(100 - state.Corruption)} {state.Corruption:0.##}%
Соц. напруга: {StatusEmoji(100 - state.Unrest)} {state.Unrest:0.##}%
Ріст населення: {state.PopulationGrowth:0.##}% / тиждень
Закони: {state.ActiveLawsCount}/{state.MaxLawsAllowed}
Наука: рівень {state.ResearchLevel}, швидкість {state.ResearchSpeed:0.##}";

            await SafeEditMessageTextAsync(botClient, chatId, msgId, text, StateKeyboardBuilder.AdminSocialKeyboard(), cancellationToken);
            LastStateViewByUser[userId] = "admin_social";
        }

        private async Task ShowAdminMilitaryMenu(ITelegramBotClient botClient, long userId, long chatId, int msgId, CancellationToken cancellationToken)
        {
            var state = GetStateOrFallback(userId);
            int countryId = GetSelectedCountryIdForUser(userId) ?? 0;
            decimal soldierSalary = EconomyConfigData.GetDecimalForCountry(countryId, "SoldierSalaryPerTurn", 0.6m);
            decimal manualWeaponQualityBonus = EconomyConfigData.GetDecimalForCountry(countryId, "ManualWeaponQualityBonus", 0m);
            string text =
$@"🛡 Військо
Армія: {FmtNumber(state.ArmySize)}
Лояльність: {state.ArmyLoyalty:0.##}% | Мораль: {state.ArmyMorale:0.##}%
Якість зброї: {state.WeaponQuality:0.##}% | Ручний бонус: {manualWeaponQualityBonus:+0.##;-0.##;0}%
Витрати на армію: {FmtNumberCompact(state.MilitaryUpkeep)}
ЗП військовим (за 1): {FmtNumberCompact(soldierSalary)}
Воєнний стан: {(state.MartialLaw ? "так" : "ні")}
Розвідка: {state.IntelLevel:0.##} | Контррозвідка: {state.CounterIntelLevel:0.##}

Для гравців зміна зарплат робиться через парламентські закони.";

            await SafeEditMessageTextAsync(botClient, chatId, msgId, text, StateKeyboardBuilder.AdminMilitaryKeyboard(), cancellationToken);
            LastStateViewByUser[userId] = "admin_military";
        }

        private async Task ShowAdminResourcesMenu(ITelegramBotClient botClient, long userId, long chatId, int msgId, CancellationToken cancellationToken)
        {
            var state = GetStateOrFallback(userId);
            string text =
$@"⛏ Контроль ресурсів
Населення: {FmtNumber(state.Population)}
Безробіття: {state.UnemploymentRate:0.##}%

Для детальних даних використовуйте меню ресурсів та торгівлі.";

            await SafeEditMessageTextAsync(botClient, chatId, msgId, text, StateKeyboardBuilder.AdminResourcesKeyboard(), cancellationToken);
            LastStateViewByUser[userId] = "admin_resources";
        }

        private async Task ShowAdminAnalytics(ITelegramBotClient botClient, long userId, long chatId, int msgId, CancellationToken cancellationToken)
        {
            var state = GetStateOrFallback(userId);
            string text =
$@"📈 Аналітика та звіти
Стабільність: {StatusEmoji(state.Stability)} {state.Stability:0.##}%
Щастя: {StatusEmoji(state.Happiness)} {state.Happiness:0.##}%
Продуктивність: {state.WorkRate:0.##}%
Корупція: {StatusEmoji(100 - state.Corruption)} {state.Corruption:0.##}%
Санкції: {FormatSanctionsLevel(state.SanctionsLevel)}
Війна: {(state.WarState ? "так" : "ні")}
Міжнародна репутація: {state.InternationalReputation:0.##}%";

            await SafeEditMessageTextAsync(botClient, chatId, msgId, text, StateKeyboardBuilder.AdminAnalyticsKeyboard(), cancellationToken);
            LastStateViewByUser[userId] = "admin_analytics";
        }

        private async Task ShowAdminDecisionLog(ITelegramBotClient botClient, long userId, long chatId, int msgId, int page, CancellationToken cancellationToken)
        {
            var selectedCountryId = GetSelectedCountryIdForUser(userId);
            if (!selectedCountryId.HasValue)
            {
                await SafeEditMessageTextAsync(botClient, chatId, msgId, "Не знайдено країни.", StateKeyboardBuilder.AdminAnalyticsKeyboard(), cancellationToken);
                return;
            }

            int pageSize = 15;
            var list = AdminActionsLogData.GetByCountry(selectedCountryId.Value, page, pageSize, out int total);
            int totalPages = (int)Math.Ceiling(total / (double)pageSize);
            if (totalPages < 1) totalPages = 1;
            if (page >= totalPages) page = totalPages - 1;

            var sb = new StringBuilder();
            sb.AppendLine("🧾 Лог рішень");
            if (list.Count == 0)
            {
                sb.AppendLine("Немає записів.");
            }
            else
            {
                foreach (var item in list)
                {
                    string when = item.CreatedAt.ToString("yyyy-MM-dd HH:mm");
                    string line = $"{when} — {item.ActionType}";
                    if (!string.IsNullOrWhiteSpace(item.OldValue) || !string.IsNullOrWhiteSpace(item.NewValue))
                        line += $" ({item.OldValue} → {item.NewValue})";
                    if (!string.IsNullOrWhiteSpace(item.Details))
                        line += $" [{item.Details}]";
                    sb.AppendLine(line);
                }
            }

            var navRows = new List<InlineKeyboardButton[]>();
            if (totalPages > 1)
            {
                var nav = new List<(string text, string data)>();
                if (page > 0)
                    nav.Add(("◀ Попередня", $"state_admin_log_page_{page - 1}"));
                if (page < totalPages - 1)
                    nav.Add(("Далі ▶", $"state_admin_log_page_{page + 1}"));
                if (nav.Count > 0)
                    navRows.Add(nav.Select(n => InlineKeyboardButton.WithCallbackData(n.text, n.data)).ToArray());
            }
            navRows.Add(new[] { InlineKeyboardButton.WithCallbackData("⬅ Назад", "state_admin_analytics") });
            var keyboard = new InlineKeyboardMarkup(navRows);

            await SafeEditMessageTextAsync(botClient, chatId, msgId, sb.ToString(), keyboard, cancellationToken);
        }

        private StateDataClass GetStateOrFallback(long userId)
        {
            var provider = new StateDataProvider();
            var selectedCountryId = GetSelectedCountryIdForUser(userId);
            if (!selectedCountryId.HasValue)
                return provider.GetStateData(userId);
            return provider.GetStateData(userId, selectedCountryId);
        }

        private async Task StartAdminTaxInput(ITelegramBotClient botClient, long userId, CancellationToken cancellationToken)
        {
            ClearEconomyInput(userId);
            AdminInputState[userId] = AdminInputMode.SetTax;
            await SendHelperMessageAsync(botClient, userId, "Вкажіть рівень податків (0-100):", cancellationToken);
        }

        private async Task StartAdminCountryNameInput(ITelegramBotClient botClient, long userId, CancellationToken cancellationToken)
        {
            if (userId != masterID)
            {
                await botClient.SendTextMessageAsync(userId, "Нема доступу.", cancellationToken: cancellationToken);
                return;
            }

            ClearEconomyInput(userId);
            AdminInputState[userId] = AdminInputMode.SetCountryName;
            await SendHelperMessageAsync(botClient, userId, "Вкажіть нову назву країни:", cancellationToken);
        }

        private async Task StartAdminInflationInput(ITelegramBotClient botClient, long userId, CancellationToken cancellationToken)
        {
            ClearEconomyInput(userId);
            AdminInputState[userId] = AdminInputMode.SetInflation;
            await SendHelperMessageAsync(botClient, userId, "Вкажіть рівень інфляції (0-200):", cancellationToken);
        }

        private async Task StartAdminBudgetInput(ITelegramBotClient botClient, long userId, CancellationToken cancellationToken)
        {
            ClearEconomyInput(userId);
            AdminInputState[userId] = AdminInputMode.SetBudget;
            await SendHelperMessageAsync(botClient, userId, "Вкажіть бюджетні частки у % (інфра/армія/наука/соц), напр: 25 25 25 25", cancellationToken);
        }

        private async Task StartAdminMilitaryUpkeepInput(ITelegramBotClient botClient, long userId, CancellationToken cancellationToken)
        {
            ClearEconomyInput(userId);
            AdminInputState[userId] = AdminInputMode.SetMilitaryUpkeep;
            await SendHelperMessageAsync(botClient, userId, "Вкажіть витрати на армію (decimal >= 0):", cancellationToken);
        }

        private async Task StartAdminArmyMoraleInput(ITelegramBotClient botClient, long userId, CancellationToken cancellationToken)
        {
            ClearEconomyInput(userId);
            AdminInputState[userId] = AdminInputMode.AdjustArmyMorale;
            await SendHelperMessageAsync(botClient, userId, "Вкажіть, на скільки змінити мораль армії (напр. 5 або -3):", cancellationToken);
        }

        private async Task StartAdminWeaponQualityInput(ITelegramBotClient botClient, long userId, CancellationToken cancellationToken)
        {
            ClearEconomyInput(userId);
            AdminInputState[userId] = AdminInputMode.AdjustWeaponQualityBonus;
            await SendHelperMessageAsync(botClient, userId, "Вкажіть, на скільки змінити ручний бонус якості зброї (напр. 4 або -2):", cancellationToken);
        }

        private async Task StartAdminSoldierSalaryInput(ITelegramBotClient botClient, long userId, CancellationToken cancellationToken)
        {
            if (userId != masterID)
            {
                await SendHelperMessageAsync(botClient, userId, "Для гравців зарплата військовим змінюється через парламент (закон типу SetSoldierSalary).", cancellationToken);
                return;
            }

            ClearEconomyInput(userId);
            AdminInputState[userId] = AdminInputMode.SetSoldierSalary;
            await SendHelperMessageAsync(botClient, userId, "Вкажіть зарплату військовим за 1 солдата/хід (decimal >= 0):", cancellationToken);
        }

        private async Task StartAdminDeputySalaryInput(ITelegramBotClient botClient, long userId, CancellationToken cancellationToken)
        {
            if (userId != masterID)
            {
                await SendHelperMessageAsync(botClient, userId, "Для гравців зарплата депутатам змінюється через парламент (закон типу SetDeputySalary).", cancellationToken);
                return;
            }

            ClearEconomyInput(userId);
            AdminInputState[userId] = AdminInputMode.SetDeputySalary;
            await SendHelperMessageAsync(botClient, userId, "Вкажіть зарплату депутата за хід (decimal >= 0):", cancellationToken);
        }

        private async Task StartAdminCivilianSalaryInput(ITelegramBotClient botClient, long userId, CancellationToken cancellationToken)
        {
            if (userId != masterID)
            {
                await SendHelperMessageAsync(botClient, userId, "Для гравців зарплата людям змінюється через парламент (закон типу SetCivilianSalary).", cancellationToken);
                return;
            }

            ClearEconomyInput(userId);
            AdminInputState[userId] = AdminInputMode.SetCivilianSalary;
            await SendHelperMessageAsync(botClient, userId, "Вкажіть базову зарплату людям за хід (decimal >= 0):", cancellationToken);
        }

        private async Task StartAdminStabilityInput(ITelegramBotClient botClient, long userId, CancellationToken cancellationToken)
        {
            ClearEconomyInput(userId);
            AdminInputState[userId] = AdminInputMode.SetStability;
            await SendHelperMessageAsync(botClient, userId, "Вкажіть рівень стабільності (0-100):", cancellationToken);
        }

        private async Task StartAdminCorruptionInput(ITelegramBotClient botClient, long userId, CancellationToken cancellationToken)
        {
            ClearEconomyInput(userId);
            AdminInputState[userId] = AdminInputMode.SetCorruption;
            await SendHelperMessageAsync(botClient, userId, "Вкажіть рівень корупції (0-100):", cancellationToken);
        }

        private async Task StartAdminHappinessInput(ITelegramBotClient botClient, long userId, CancellationToken cancellationToken)
        {
            ClearEconomyInput(userId);
            AdminInputState[userId] = AdminInputMode.SetHappiness;
            await SendHelperMessageAsync(botClient, userId, "Вкажіть рівень щастя (0-100):", cancellationToken);
        }

        private async Task StartAdminUnrestInput(ITelegramBotClient botClient, long userId, CancellationToken cancellationToken)
        {
            ClearEconomyInput(userId);
            AdminInputState[userId] = AdminInputMode.SetUnrest;
            await SendHelperMessageAsync(botClient, userId, "Вкажіть рівень соц. напруги (0-100):", cancellationToken);
        }

        private async Task StartAdminPopulationGrowthInput(ITelegramBotClient botClient, long userId, CancellationToken cancellationToken)
        {
            ClearEconomyInput(userId);
            AdminInputState[userId] = AdminInputMode.SetPopulationGrowth;
            await SendHelperMessageAsync(botClient, userId, "Вкажіть ріст населення у % (наприклад 1.5 або -0.5):", cancellationToken);
        }

        private async Task StartAdminAddPopulationInput(ITelegramBotClient botClient, long userId, CancellationToken cancellationToken)
        {
            ClearEconomyInput(userId);
            AdminInputState[userId] = AdminInputMode.AddPopulation;
            await SendHelperMessageAsync(botClient, userId, "Вкажіть, скільки додати до населення (ціле число > 0):", cancellationToken);
        }

        private async Task StartAdminRemovePopulationInput(ITelegramBotClient botClient, long userId, CancellationToken cancellationToken)
        {
            ClearEconomyInput(userId);
            AdminInputState[userId] = AdminInputMode.RemovePopulation;
            await SendHelperMessageAsync(botClient, userId, "Вкажіть, скільки відняти від населення (ціле число > 0):", cancellationToken);
        }

        private async Task ShowAdminPopulationRacesMenu(ITelegramBotClient botClient, long userId, long chatId, int msgId, int page, CancellationToken cancellationToken)
        {
            var selectedCountryId = GetSelectedCountryIdForUser(userId);
            if (!selectedCountryId.HasValue)
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "Не знайдено країни.",
                    StateKeyboardBuilder.AdminSocialKeyboard(),
                    cancellationToken);
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
                    StateKeyboardBuilder.AdminSocialKeyboard(),
                    cancellationToken);
                return;
            }

            var races = GetPopulationRaces(country);
            var ordered = races
                .OrderByDescending(x => x.Value)
                .ThenBy(x => x.Key, StringComparer.OrdinalIgnoreCase)
                .ToList();

            const int pageSize = 10;
            int safePage = Math.Max(0, page);
            int totalPages = Math.Max(1, (int)Math.Ceiling(ordered.Count / (double)pageSize));
            if (safePage >= totalPages)
                safePage = totalPages - 1;

            var pageItems = ordered
                .Skip(safePage * pageSize)
                .Take(pageSize)
                .Select(x =>
                {
                    decimal share = country.Population > 0
                        ? (x.Value / (decimal)country.Population) * 100m
                        : 0m;
                    string encoded = EncodeKey(x.Key);
                    return ($"{x.Key}: {FmtNumber(x.Value)} ({share:0.##}%)", $"state_admin_population_raceinfo|{encoded}|{safePage}");
                })
                .ToList();

            long totalByRaces = ordered.Sum(x => x.Value);
            string text =
$@"🧬 Раси населення
Населення країни: {FmtNumber(country.Population)}
Сума по расах: {FmtNumber(totalByRaces)}
К-сть рас: {FmtNumber(ordered.Count)}

Оберіть расу для редагування або додайте нову.";

            await SafeEditMessageTextAsync(
                botClient,
                chatId,
                msgId,
                text,
                StateKeyboardBuilder.AdminPopulationRacesKeyboard(pageItems, safePage, totalPages),
                cancellationToken);
            LastStateViewByUser[userId] = "admin_population_races";
        }

        private async Task ShowAdminPopulationRaceInfo(
            ITelegramBotClient botClient,
            long userId,
            long chatId,
            int msgId,
            string raceName,
            int returnPage,
            CancellationToken cancellationToken)
        {
            var selectedCountryId = GetSelectedCountryIdForUser(userId);
            if (!selectedCountryId.HasValue)
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "Не знайдено країни.",
                    StateKeyboardBuilder.AdminSocialKeyboard(),
                    cancellationToken);
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
                    StateKeyboardBuilder.AdminSocialKeyboard(),
                    cancellationToken);
                return;
            }

            string normalizedRace = NormalizePopulationRaceName(raceName);
            var races = GetPopulationRaces(country);
            races.TryGetValue(normalizedRace, out long count);
            decimal share = country.Population > 0 ? (count / (decimal)country.Population) * 100m : 0m;

            string text =
$@"🧬 Раса: {normalizedRace}
Кількість: {FmtNumber(count)}
Частка: {share:0.##}%
Населення країни: {FmtNumber(country.Population)}

Оберіть дію:";

            await SafeEditMessageTextAsync(
                botClient,
                chatId,
                msgId,
                text,
                StateKeyboardBuilder.AdminPopulationRaceInfoKeyboard(normalizedRace, returnPage),
                cancellationToken);
        }

        private async Task StartAdminCreatePopulationRaceInput(ITelegramBotClient botClient, long userId, CancellationToken cancellationToken)
        {
            ClearEconomyInput(userId);
            AdminPopulationRaceBuffer.Remove(userId);
            AdminPopulationRacePageBuffer.Remove(userId);
            AdminInputState[userId] = AdminInputMode.CreatePopulationRace;
            await SendHelperMessageAsync(botClient, userId, "Вкажіть нову расу у форматі: Назва = Кількість\nПриклад: Ельфи = 12000", cancellationToken);
        }

        private async Task StartAdminPopulationRaceAmountInput(
            ITelegramBotClient botClient,
            long userId,
            AdminInputMode mode,
            string raceName,
            int returnPage,
            CancellationToken cancellationToken)
        {
            ClearEconomyInput(userId);
            AdminPopulationRaceBuffer[userId] = NormalizePopulationRaceName(raceName);
            AdminPopulationRacePageBuffer[userId] = Math.Max(0, returnPage);
            AdminInputState[userId] = mode;

            string prompt = mode switch
            {
                AdminInputMode.SetPopulationRaceAmount => $"Вкажіть нову кількість для раси \"{AdminPopulationRaceBuffer[userId]}\" (ціле число >= 0):",
                AdminInputMode.AddPopulationRaceAmount => $"Вкажіть, скільки додати до раси \"{AdminPopulationRaceBuffer[userId]}\" (ціле число > 0):",
                AdminInputMode.RemovePopulationRaceAmount => $"Вкажіть, скільки відняти від раси \"{AdminPopulationRaceBuffer[userId]}\" (ціле число > 0):",
                _ => "Вкажіть кількість:"
            };

            await SendHelperMessageAsync(botClient, userId, prompt, cancellationToken);
        }

        private async Task DeleteAdminPopulationRace(
            ITelegramBotClient botClient,
            long userId,
            long chatId,
            int msgId,
            string raceName,
            int returnPage,
            CancellationToken cancellationToken)
        {
            var selectedCountryId = GetSelectedCountryIdForUser(userId);
            if (!selectedCountryId.HasValue)
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "Не знайдено країни.",
                    StateKeyboardBuilder.AdminSocialKeyboard(),
                    cancellationToken);
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
                    StateKeyboardBuilder.AdminSocialKeyboard(),
                    cancellationToken);
                return;
            }

            string normalizedRace = NormalizePopulationRaceName(raceName);
            var races = GetPopulationRaces(country);
            if (!races.TryGetValue(normalizedRace, out long oldAmount))
            {
                await ShowAdminPopulationRacesMenu(botClient, userId, chatId, msgId, returnPage, cancellationToken);
                return;
            }

            races.Remove(normalizedRace);
            long oldPopulation = Math.Max(0L, country.Population);
            country.Population = SumPopulationRaces(races);
            SavePopulationRaces(country.CountryId, races);
            country.LastUpdate = DateTime.UtcNow;
            CountriesData.UpdateCountry(country);
            LogAdminAction(
                country.CountryId,
                userId,
                "PopulationRaceDelete",
                $"{normalizedRace}:{oldAmount.ToString(CultureInfo.InvariantCulture)}",
                "-",
                $"Population {oldPopulation} -> {country.Population}");

            await ShowAdminPopulationRacesMenu(botClient, userId, chatId, msgId, returnPage, cancellationToken);
        }

        private static bool TryParseAdminPopulationRacePayload(string data, string prefix, out string raceName, out int returnPage)
        {
            raceName = string.Empty;
            returnPage = 0;

            if (string.IsNullOrWhiteSpace(data) || string.IsNullOrWhiteSpace(prefix))
                return false;
            if (!data.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                return false;

            string payload = data.Substring(prefix.Length);
            string[] parts = payload.Split('|', StringSplitOptions.None);
            if (parts.Length == 0 || string.IsNullOrWhiteSpace(parts[0]))
                return false;

            raceName = DecodeKey(parts[0]).Trim();
            if (string.IsNullOrWhiteSpace(raceName))
                return false;

            if (parts.Length > 1 && int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsedPage))
                returnPage = Math.Max(0, parsedPage);

            return true;
        }

        private async Task StartAdminMobilizeInput(ITelegramBotClient botClient, long userId, CancellationToken cancellationToken)
        {
            ClearEconomyInput(userId);
            AdminInputState[userId] = AdminInputMode.MobilizePercent;
            await SendHelperMessageAsync(botClient, userId, "Вкажіть % населення для армії (0-100):", cancellationToken);
        }

        private async Task StartAdminIntelInput(ITelegramBotClient botClient, long userId, CancellationToken cancellationToken)
        {
            ClearEconomyInput(userId);
            AdminInputState[userId] = AdminInputMode.SetIntel;
            await SendHelperMessageAsync(botClient, userId, "Вкажіть рівень розвідки (0-100):", cancellationToken);
        }

        private async Task StartAdminCounterIntelInput(ITelegramBotClient botClient, long userId, CancellationToken cancellationToken)
        {
            ClearEconomyInput(userId);
            AdminInputState[userId] = AdminInputMode.SetCounterIntel;
            await SendHelperMessageAsync(botClient, userId, "Вкажіть рівень контррозвідки (0-100):", cancellationToken);
        }

        private async Task HandleAdminInput(ITelegramBotClient botClient, long userId, AdminInputMode mode, string input, CancellationToken cancellationToken)
        {
            if (!HasAdminAccess(userId))
            {
                await CleanupHelperMessagesAsync(botClient, userId, cancellationToken);
                ClearAdminInput(userId);
                await botClient.SendTextMessageAsync(userId, "Нема доступу.", cancellationToken: cancellationToken);
                return;
            }

            var selectedCountryId = GetSelectedCountryIdForUser(userId);
            if (!selectedCountryId.HasValue)
            {
                await CleanupHelperMessagesAsync(botClient, userId, cancellationToken);
                ClearAdminInput(userId);
                await botClient.SendTextMessageAsync(userId, "Не знайдено країни. Оберіть країну або призначте CountryId персонажу.", cancellationToken: cancellationToken);
                return;
            }

            var country = CountriesData.GetCountryById(selectedCountryId.Value);
            if (country == null)
            {
                await CleanupHelperMessagesAsync(botClient, userId, cancellationToken);
                ClearAdminInput(userId);
                await botClient.SendTextMessageAsync(userId, "Країну не знайдено.", cancellationToken: cancellationToken);
                return;
            }

            switch (mode)
            {
                case AdminInputMode.SetCountryName:
                    if (userId != masterID)
                    {
                        await botClient.SendTextMessageAsync(userId, "Нема доступу.", cancellationToken: cancellationToken);
                        return;
                    }
                    if (!CountryNameUtils.TryNormalize(input, out var countryName, out var errName))
                    {
                        await SendHelperMessageAsync(botClient, userId, $"❌ {errName}", cancellationToken);
                        return;
                    }
                    LogAdminAction(country.CountryId, userId, "CountryName", country.Name, countryName, null);
                    country.Name = countryName;
                    break;
                case AdminInputMode.SetTax:
                    if (!int.TryParse(input, NumberStyles.Integer, CultureInfo.InvariantCulture, out int tax) || tax < 0 || tax > 100)
                    {
                        await SendHelperMessageAsync(botClient, userId, "❌ Очікується ціле число 0-100.", cancellationToken);
                        return;
                    }
                    LogAdminAction(country.CountryId, userId, "TaxLevel", country.TaxLevel.ToString(CultureInfo.InvariantCulture), tax.ToString(CultureInfo.InvariantCulture), null);
                    country.TaxLevel = tax;
                    break;
                case AdminInputMode.SetInflation:
                    if (!float.TryParse(input.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out var infl) || infl < 0 || infl > 200)
                    {
                        await SendHelperMessageAsync(botClient, userId, "❌ Очікується число 0-200.", cancellationToken);
                        return;
                    }
                    LogAdminAction(country.CountryId, userId, "Inflation", country.Inflation.ToString(CultureInfo.InvariantCulture), infl.ToString(CultureInfo.InvariantCulture), null);
                    country.Inflation = infl;
                    break;
                case AdminInputMode.SetBudget:
                    var parts = (input ?? string.Empty).Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length < 4 ||
                        !TryParsePercent(parts[0], out var infraPct) ||
                        !TryParsePercent(parts[1], out var milPct) ||
                        !TryParsePercent(parts[2], out var sciPct) ||
                        !TryParsePercent(parts[3], out var socPct))
                    {
                        await SendHelperMessageAsync(botClient, userId, "❌ Вкажіть 4 числа у % (0-100).", cancellationToken);
                        return;
                    }
                    decimal sum = infraPct + milPct + sciPct + socPct;
                    if (Math.Abs(sum - 100m) > 0.5m)
                    {
                        await SendHelperMessageAsync(botClient, userId, "❌ Сума має бути 100% (допуск 0.5%).", cancellationToken);
                        return;
                    }
                    var oldBudget = EconomyBudgetData.GetByCountry(country.CountryId);
                    var newBudget = new EconomyBudgetData.EconomyBudgetRecord
                    {
                        CountryId = country.CountryId,
                        InfrastructureShare = infraPct / 100m,
                        MilitaryShare = milPct / 100m,
                        ScienceShare = sciPct / 100m,
                        SocialShare = socPct / 100m,
                        UpdatedAt = DateTime.UtcNow
                    };
                    EconomyBudgetData.Upsert(newBudget);
                    LogAdminAction(country.CountryId, userId, "Budget",
                        $"{oldBudget.InfrastructureShare:0.####}/{oldBudget.MilitaryShare:0.####}/{oldBudget.ScienceShare:0.####}/{oldBudget.SocialShare:0.####}",
                        $"{newBudget.InfrastructureShare:0.####}/{newBudget.MilitaryShare:0.####}/{newBudget.ScienceShare:0.####}/{newBudget.SocialShare:0.####}",
                        "infra/mil/sci/soc");
                    break;
                case AdminInputMode.SetMilitaryUpkeep:
                    if (!decimal.TryParse(input.Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture, out var upkeep) || upkeep < 0)
                    {
                        await SendHelperMessageAsync(botClient, userId, "❌ Очікується decimal >= 0.", cancellationToken);
                        return;
                    }
                    LogAdminAction(country.CountryId, userId, "MilitaryUpkeep", country.MilitaryUpkeep.ToString(CultureInfo.InvariantCulture), upkeep.ToString(CultureInfo.InvariantCulture), null);
                    country.MilitaryUpkeep = upkeep;
                    break;
                case AdminInputMode.AdjustArmyMorale:
                    if (!float.TryParse(input.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out var moraleDelta) || moraleDelta < -100 || moraleDelta > 100)
                    {
                        await SendHelperMessageAsync(botClient, userId, "❌ Очікується число від -100 до 100.", cancellationToken);
                        return;
                    }

                    float oldMorale = country.ArmyMorale;
                    float newMorale = EconomyCalculator.Clamp(oldMorale + moraleDelta, 0f, 100f);
                    LogAdminAction(
                        country.CountryId,
                        userId,
                        "ArmyMoraleDelta",
                        oldMorale.ToString(CultureInfo.InvariantCulture),
                        newMorale.ToString(CultureInfo.InvariantCulture),
                        $"{moraleDelta:+0.##;-0.##;0}");
                    country.ArmyMorale = newMorale;
                    break;
                case AdminInputMode.AdjustWeaponQualityBonus:
                    if (!decimal.TryParse(input.Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture, out var weaponQualityDelta) || weaponQualityDelta < -100m || weaponQualityDelta > 100m)
                    {
                        await SendHelperMessageAsync(botClient, userId, "❌ Очікується число від -100 до 100.", cancellationToken);
                        return;
                    }

                    decimal oldWeaponBonus = EconomyConfigData.GetDecimalForCountry(country.CountryId, "ManualWeaponQualityBonus", 0m);
                    decimal newWeaponBonus = Math.Clamp(oldWeaponBonus + weaponQualityDelta, -100m, 100m);
                    if (!EconomyConfigData.UpsertCountryDecimal(country.CountryId, "ManualWeaponQualityBonus", newWeaponBonus))
                    {
                        await SendHelperMessageAsync(botClient, userId, "❌ Не вдалося оновити бонус якості зброї.", cancellationToken);
                        return;
                    }

                    LogAdminAction(
                        country.CountryId,
                        userId,
                        "WeaponQualityBonusDelta",
                        oldWeaponBonus.ToString(CultureInfo.InvariantCulture),
                        newWeaponBonus.ToString(CultureInfo.InvariantCulture),
                        $"{weaponQualityDelta:+0.##;-0.##;0}");
                    break;
                case AdminInputMode.SetSoldierSalary:
                    if (userId != masterID)
                    {
                        await SendHelperMessageAsync(botClient, userId, "Для гравців зарплати змінюються через парламент.", cancellationToken);
                        return;
                    }
                    if (!TryParseSalaryValue(input, out var soldierSalary))
                    {
                        await SendHelperMessageAsync(botClient, userId, "❌ Очікується decimal >= 0.", cancellationToken);
                        return;
                    }
                    if (!TryUpdateSalaryConfig(country.CountryId, userId, "SoldierSalaryPerTurn", soldierSalary, "SoldierSalary"))
                    {
                        await SendHelperMessageAsync(botClient, userId, "❌ Не вдалося оновити зарплату військовим.", cancellationToken);
                        return;
                    }
                    break;
                case AdminInputMode.SetDeputySalary:
                    if (userId != masterID)
                    {
                        await SendHelperMessageAsync(botClient, userId, "Для гравців зарплати змінюються через парламент.", cancellationToken);
                        return;
                    }
                    if (!TryParseSalaryValue(input, out var deputySalary))
                    {
                        await SendHelperMessageAsync(botClient, userId, "❌ Очікується decimal >= 0.", cancellationToken);
                        return;
                    }
                    if (!TryUpdateSalaryConfig(country.CountryId, userId, "DeputySalaryBase", deputySalary, "DeputySalary"))
                    {
                        await SendHelperMessageAsync(botClient, userId, "❌ Не вдалося оновити зарплату депутатам.", cancellationToken);
                        return;
                    }
                    break;
                case AdminInputMode.SetCivilianSalary:
                    if (userId != masterID)
                    {
                        await SendHelperMessageAsync(botClient, userId, "Для гравців зарплати змінюються через парламент.", cancellationToken);
                        return;
                    }
                    if (!TryParseSalaryValue(input, out var civilianSalary))
                    {
                        await SendHelperMessageAsync(botClient, userId, "❌ Очікується decimal >= 0.", cancellationToken);
                        return;
                    }
                    if (!TryUpdateSalaryConfig(country.CountryId, userId, "CompanyBaseSalary", civilianSalary, "CivilianSalary") ||
                        !TryUpdateSalaryConfig(country.CountryId, userId, "BaseWage", civilianSalary, "CivilianSalaryFallback"))
                    {
                        await SendHelperMessageAsync(botClient, userId, "❌ Не вдалося оновити зарплату людям.", cancellationToken);
                        return;
                    }
                    break;
                case AdminInputMode.SetStability:
                    if (!float.TryParse(input.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out var stability) || stability < 0 || stability > 100)
                    {
                        await SendHelperMessageAsync(botClient, userId, "❌ Очікується число 0-100.", cancellationToken);
                        return;
                    }
                    LogAdminAction(country.CountryId, userId, "Stability", country.Stability.ToString(CultureInfo.InvariantCulture), stability.ToString(CultureInfo.InvariantCulture), null);
                    country.Stability = stability;
                    break;
                case AdminInputMode.SetCorruption:
                    if (!float.TryParse(input.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out var corruption) || corruption < 0 || corruption > 100)
                    {
                        await SendHelperMessageAsync(botClient, userId, "❌ Очікується число 0-100.", cancellationToken);
                        return;
                    }
                    LogAdminAction(country.CountryId, userId, "Corruption", country.Corruption.ToString(CultureInfo.InvariantCulture), corruption.ToString(CultureInfo.InvariantCulture), null);
                    country.Corruption = corruption;
                    break;
                case AdminInputMode.SetHappiness:
                    if (!float.TryParse(input.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out var happiness) || happiness < 0 || happiness > 100)
                    {
                        await SendHelperMessageAsync(botClient, userId, "❌ Очікується число 0-100.", cancellationToken);
                        return;
                    }
                    LogAdminAction(country.CountryId, userId, "Happiness", country.Happiness.ToString(CultureInfo.InvariantCulture), happiness.ToString(CultureInfo.InvariantCulture), null);
                    country.Happiness = happiness;
                    break;
                case AdminInputMode.SetUnrest:
                    if (!float.TryParse(input.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out var unrest) || unrest < 0 || unrest > 100)
                    {
                        await SendHelperMessageAsync(botClient, userId, "❌ Очікується число 0-100.", cancellationToken);
                        return;
                    }
                    LogAdminAction(country.CountryId, userId, "Unrest", country.Unrest.ToString(CultureInfo.InvariantCulture), unrest.ToString(CultureInfo.InvariantCulture), null);
                    country.Unrest = unrest;
                    break;
                case AdminInputMode.SetPopulationGrowth:
                    if (!float.TryParse(input.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out var growth) || growth < -20 || growth > 20)
                    {
                        await SendHelperMessageAsync(botClient, userId, "❌ Очікується число від -20 до 20.", cancellationToken);
                        return;
                    }
                    LogAdminAction(country.CountryId, userId, "PopulationGrowth", country.PopulationGrowth.ToString(CultureInfo.InvariantCulture), growth.ToString(CultureInfo.InvariantCulture), null);
                    country.PopulationGrowth = growth;
                    break;
                case AdminInputMode.AddPopulation:
                    if (!TryParsePositiveLong(input, out long addPopulation))
                    {
                        await SendHelperMessageAsync(botClient, userId, "❌ Очікується ціле число > 0.", cancellationToken);
                        return;
                    }
                    long oldPopulation = Math.Max(0L, country.Population);
                    long increasedPopulation;
                    try
                    {
                        increasedPopulation = checked(oldPopulation + addPopulation);
                    }
                    catch (OverflowException)
                    {
                        increasedPopulation = long.MaxValue;
                    }
                    LogAdminAction(
                        country.CountryId,
                        userId,
                        "Population",
                        oldPopulation.ToString(CultureInfo.InvariantCulture),
                        increasedPopulation.ToString(CultureInfo.InvariantCulture),
                        $"+{addPopulation.ToString(CultureInfo.InvariantCulture)}");
                    country.Population = increasedPopulation;
                    UpdatePopulationRacesAfterPopulationChange(country, oldPopulation);
                    break;
                case AdminInputMode.RemovePopulation:
                    if (!TryParsePositiveLong(input, out long removePopulation))
                    {
                        await SendHelperMessageAsync(botClient, userId, "❌ Очікується ціле число > 0.", cancellationToken);
                        return;
                    }
                    oldPopulation = Math.Max(0L, country.Population);
                    long appliedReduction = Math.Min(removePopulation, oldPopulation);
                    long reducedPopulation = oldPopulation - appliedReduction;
                    LogAdminAction(
                        country.CountryId,
                        userId,
                        "Population",
                        oldPopulation.ToString(CultureInfo.InvariantCulture),
                        reducedPopulation.ToString(CultureInfo.InvariantCulture),
                        $"-{appliedReduction.ToString(CultureInfo.InvariantCulture)}");
                    country.Population = reducedPopulation;
                    UpdatePopulationRacesAfterPopulationChange(country, oldPopulation);
                    break;
                case AdminInputMode.CreatePopulationRace:
                    if (!TryParseRaceCreateInput(input, out string createdRaceName, out long createdRaceAmount, out string createError))
                    {
                        await SendHelperMessageAsync(botClient, userId, createError, cancellationToken);
                        return;
                    }

                    var createRaces = GetPopulationRaces(country);
                    if (createRaces.ContainsKey(createdRaceName))
                    {
                        await SendHelperMessageAsync(botClient, userId, "❌ Така раса вже існує. Оберіть її зі списку для редагування.", cancellationToken);
                        return;
                    }

                    createRaces[createdRaceName] = createdRaceAmount;
                    long beforeCreatePopulation = Math.Max(0L, country.Population);
                    country.Population = SumPopulationRaces(createRaces);
                    SavePopulationRaces(country.CountryId, createRaces);
                    LogAdminAction(
                        country.CountryId,
                        userId,
                        "PopulationRaceCreate",
                        "-",
                        $"{createdRaceName}:{createdRaceAmount.ToString(CultureInfo.InvariantCulture)}",
                        $"Population {beforeCreatePopulation} -> {country.Population}");
                    break;
                case AdminInputMode.SetPopulationRaceAmount:
                case AdminInputMode.AddPopulationRaceAmount:
                case AdminInputMode.RemovePopulationRaceAmount:
                    if (!AdminPopulationRaceBuffer.TryGetValue(userId, out string? raceName) || string.IsNullOrWhiteSpace(raceName))
                    {
                        await SendHelperMessageAsync(botClient, userId, "❌ Спочатку оберіть расу у меню.", cancellationToken);
                        return;
                    }

                    var raceMap = GetPopulationRaces(country);
                    raceMap.TryGetValue(raceName, out long oldRaceAmount);
                    long newRaceAmount;

                    if (mode == AdminInputMode.SetPopulationRaceAmount)
                    {
                        if (!TryParseNonNegativeLong(input, out long parsedSet))
                        {
                            await SendHelperMessageAsync(botClient, userId, "❌ Очікується ціле число >= 0.", cancellationToken);
                            return;
                        }
                        newRaceAmount = parsedSet;
                    }
                    else if (mode == AdminInputMode.AddPopulationRaceAmount)
                    {
                        if (!TryParsePositiveLong(input, out long parsedAdd))
                        {
                            await SendHelperMessageAsync(botClient, userId, "❌ Очікується ціле число > 0.", cancellationToken);
                            return;
                        }
                        try
                        {
                            newRaceAmount = checked(oldRaceAmount + parsedAdd);
                        }
                        catch (OverflowException)
                        {
                            newRaceAmount = long.MaxValue;
                        }
                    }
                    else
                    {
                        if (!TryParsePositiveLong(input, out long parsedRemove))
                        {
                            await SendHelperMessageAsync(botClient, userId, "❌ Очікується ціле число > 0.", cancellationToken);
                            return;
                        }
                        long applied = Math.Min(parsedRemove, oldRaceAmount);
                        newRaceAmount = oldRaceAmount - applied;
                    }

                    if (newRaceAmount <= 0L)
                        raceMap.Remove(raceName);
                    else
                        raceMap[raceName] = newRaceAmount;

                    long beforeRacePopulation = Math.Max(0L, country.Population);
                    long afterRacePopulation = SumPopulationRaces(raceMap);
                    country.Population = afterRacePopulation;
                    SavePopulationRaces(country.CountryId, raceMap);

                    LogAdminAction(
                        country.CountryId,
                        userId,
                        "PopulationRaceAmount",
                        $"{raceName}:{oldRaceAmount.ToString(CultureInfo.InvariantCulture)}",
                        $"{raceName}:{Math.Max(0L, newRaceAmount).ToString(CultureInfo.InvariantCulture)}",
                        $"Population {beforeRacePopulation} -> {afterRacePopulation}");
                    break;
                case AdminInputMode.MobilizePercent:
                    if (!float.TryParse(input.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out var pct) || pct < 0 || pct > 100)
                    {
                        await SendHelperMessageAsync(botClient, userId, "❌ Очікується число 0-100.", cancellationToken);
                        return;
                    }
                    var population = PopulationCalculator.GetPopulationSnapshot(country);
                    int targetArmy = (int)Math.Min(int.MaxValue, Math.Round(population.WorkingAge * (pct / 100f), MidpointRounding.AwayFromZero));
                    int delta = targetArmy - country.ArmySize;
                    LogAdminAction(country.CountryId, userId, "Mobilization", country.ArmySize.ToString(CultureInfo.InvariantCulture), targetArmy.ToString(CultureInfo.InvariantCulture), $"{pct:0.##}%");
                    country.ArmySize = targetArmy;
                    if (delta != 0)
                    {
                        country.AvailableWorkers = Math.Max(0L, country.AvailableWorkers - delta);
                    }
                    break;
                case AdminInputMode.SetIntel:
                    if (!float.TryParse(input.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out var intel) || intel < 0 || intel > 100)
                    {
                        await SendHelperMessageAsync(botClient, userId, "❌ Очікується число 0-100.", cancellationToken);
                        return;
                    }
                    LogAdminAction(country.CountryId, userId, "IntelLevel", country.IntelLevel.ToString(CultureInfo.InvariantCulture), intel.ToString(CultureInfo.InvariantCulture), null);
                    country.IntelLevel = intel;
                    break;
                case AdminInputMode.SetCounterIntel:
                    if (!float.TryParse(input.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out var counter) || counter < 0 || counter > 100)
                    {
                        await SendHelperMessageAsync(botClient, userId, "❌ Очікується число 0-100.", cancellationToken);
                        return;
                    }
                    LogAdminAction(country.CountryId, userId, "CounterIntelLevel", country.CounterIntelLevel.ToString(CultureInfo.InvariantCulture), counter.ToString(CultureInfo.InvariantCulture), null);
                    country.CounterIntelLevel = counter;
                    break;
            }

            country.LastUpdate = DateTime.UtcNow;
            CountriesData.UpdateCountry(country);

            await CleanupHelperMessagesAsync(botClient, userId, cancellationToken);
            ClearAdminInput(userId);
            await botClient.SendTextMessageAsync(userId, "✅ Зміни збережено.", cancellationToken: cancellationToken);
        }

        private static Dictionary<string, long> GetPopulationRaces(CountryRecord country)
        {
            var races = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
            if (country == null || country.CountryId <= 0)
                return races;

            string raw = EconomyConfigData.GetTextForCountry(country.CountryId, PopulationRacesConfigKey, string.Empty);
            races = NormalizePopulationRacesMap(DeserializePopulationRaces(raw));
            AlignPopulationRacesTotal(races, Math.Max(0L, country.Population));
            return races;
        }

        private static Dictionary<string, long> DeserializePopulationRaces(string raw)
        {
            var result = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrWhiteSpace(raw))
                return result;

            try
            {
                var fromJson = JsonSerializer.Deserialize<Dictionary<string, long>>(raw);
                if (fromJson != null)
                {
                    foreach (var pair in fromJson)
                    {
                        string name = NormalizePopulationRaceName(pair.Key);
                        if (pair.Value <= 0L || string.IsNullOrWhiteSpace(name))
                            continue;
                        result[name] = pair.Value;
                    }
                    return result;
                }
            }
            catch
            {
                 
            }

            string[] parts = raw.Split(';', StringSplitOptions.RemoveEmptyEntries);
            foreach (string part in parts)
            {
                string[] tokens = part.Split('=', 2, StringSplitOptions.TrimEntries);
                if (tokens.Length != 2)
                    continue;
                if (!long.TryParse(tokens[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out long value) || value <= 0L)
                    continue;
                string name = NormalizePopulationRaceName(tokens[0]);
                if (string.IsNullOrWhiteSpace(name))
                    continue;
                result[name] = value;
            }

            return result;
        }

        private static Dictionary<string, long> NormalizePopulationRacesMap(Dictionary<string, long>? source)
        {
            var result = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
            if (source == null || source.Count == 0)
                return result;

            foreach (var pair in source)
            {
                string name = NormalizePopulationRaceName(pair.Key);
                if (string.IsNullOrWhiteSpace(name) || pair.Value <= 0L)
                    continue;
                result[name] = pair.Value;
            }

            return result;
        }

        private static void AlignPopulationRacesTotal(Dictionary<string, long> races, long targetPopulation)
        {
            if (races == null)
                return;

            foreach (var key in races.Where(x => x.Value <= 0L).Select(x => x.Key).ToList())
                races.Remove(key);

            long safeTarget = Math.Max(0L, targetPopulation);
            if (safeTarget <= 0L)
            {
                races.Clear();
                return;
            }

            if (races.Count == 0)
            {
                races[DefaultPopulationRaceName] = safeTarget;
                return;
            }

            long current = SumPopulationRaces(races);
            if (current == safeTarget)
                return;

            if (current < safeTarget)
            {
                string dominant = GetDominantPopulationRace(races);
                races[dominant] += safeTarget - current;
                return;
            }

            long toReduce = current - safeTarget;
            foreach (var key in races.OrderByDescending(x => x.Value).ThenBy(x => x.Key, StringComparer.OrdinalIgnoreCase).Select(x => x.Key).ToList())
            {
                if (toReduce <= 0L)
                    break;

                long currentValue = races[key];
                long reduce = Math.Min(currentValue, toReduce);
                long left = currentValue - reduce;
                if (left <= 0L)
                    races.Remove(key);
                else
                    races[key] = left;
                toReduce -= reduce;
            }

            if (races.Count == 0 && safeTarget > 0L)
                races[DefaultPopulationRaceName] = safeTarget;
        }

        private static string GetDominantPopulationRace(Dictionary<string, long> races)
        {
            if (races == null || races.Count == 0)
                return DefaultPopulationRaceName;

            if (races.TryGetValue(DefaultPopulationRaceName, out long defaultRaceCount) && defaultRaceCount > 0L)
                return DefaultPopulationRaceName;

            return races
                .OrderByDescending(x => x.Value)
                .ThenBy(x => x.Key, StringComparer.OrdinalIgnoreCase)
                .Select(x => x.Key)
                .FirstOrDefault() ?? DefaultPopulationRaceName;
        }

        private static long SumPopulationRaces(Dictionary<string, long> races)
        {
            if (races == null || races.Count == 0)
                return 0L;
            return races.Values.Where(v => v > 0L).Aggregate(0L, (acc, value) =>
            {
                try
                {
                    return checked(acc + value);
                }
                catch (OverflowException)
                {
                    return long.MaxValue;
                }
            });
        }

        private static void SavePopulationRaces(int countryId, Dictionary<string, long> races)
        {
            var normalized = NormalizePopulationRacesMap(races);
            string json = JsonSerializer.Serialize(normalized);
            EconomyConfigData.UpsertCountryText(countryId, PopulationRacesConfigKey, json);
        }

        private static void UpdatePopulationRacesAfterPopulationChange(CountryRecord country, long previousPopulation)
        {
            if (country == null || country.CountryId <= 0)
                return;

            string raw = EconomyConfigData.GetTextForCountry(country.CountryId, PopulationRacesConfigKey, string.Empty);
            var races = NormalizePopulationRacesMap(DeserializePopulationRaces(raw));
            AlignPopulationRacesTotal(races, Math.Max(0L, previousPopulation));
            AlignPopulationRacesTotal(races, Math.Max(0L, country.Population));
            SavePopulationRaces(country.CountryId, races);
        }

        private static string NormalizePopulationRaceName(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return DefaultPopulationRaceName;

            string normalized = raw.Trim();
            if (normalized.Length > 40)
                normalized = normalized.Substring(0, 40).Trim();
            return string.IsNullOrWhiteSpace(normalized) ? DefaultPopulationRaceName : normalized;
        }

        private static bool TryParseRaceCreateInput(string input, out string raceName, out long amount, out string error)
        {
            raceName = string.Empty;
            amount = 0L;
            error = string.Empty;

            if (string.IsNullOrWhiteSpace(input))
            {
                error = "❌ Вкажіть назву раси.";
                return false;
            }

            string raw = input.Trim();
            string[] separators = { "=", ":" };
            string? usedSeparator = separators.FirstOrDefault(raw.Contains);
            if (usedSeparator == null)
            {
                raceName = NormalizePopulationRaceName(raw);
                amount = 0L;
                return true;
            }

            string[] parts = raw.Split(new[] { usedSeparator }, 2, StringSplitOptions.TrimEntries);
            if (parts.Length != 2 || string.IsNullOrWhiteSpace(parts[0]))
            {
                error = "❌ Формат: Назва = Кількість";
                return false;
            }

            if (!TryParseNonNegativeLong(parts[1], out long parsedAmount))
            {
                error = "❌ Кількість має бути цілим числом >= 0.";
                return false;
            }

            raceName = NormalizePopulationRaceName(parts[0]);
            amount = parsedAmount;
            return true;
        }

        private static void ClearAdminInput(long userId)
        {
            AdminInputState[userId] = AdminInputMode.None;
            AdminPopulationRaceBuffer.Remove(userId);
            AdminPopulationRacePageBuffer.Remove(userId);
        }

        private static void LogAdminAction(int countryId, long userId, string action, string? oldValue, string? newValue, string? details)
        {
            AdminActionsLogData.AddLog(countryId, userId, action, oldValue, newValue, details);
        }

        private static bool TryParsePercent(string raw, out decimal value)
        {
            value = 0m;
            if (!decimal.TryParse(raw.Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture, out var v))
                return false;
            if (v < 0 || v > 100)
                return false;
            value = v;
            return true;
        }

        private static bool TryParseSalaryValue(string raw, out decimal value)
        {
            value = 0m;
            if (!decimal.TryParse(raw.Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed))
                return false;
            if (parsed < 0m)
                return false;
            value = decimal.Round(parsed, 4, MidpointRounding.AwayFromZero);
            return true;
        }

        private static bool TryParsePositiveLong(string raw, out long value)
        {
            value = 0L;
            if (!long.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
                return false;
            if (parsed <= 0L)
                return false;
            value = parsed;
            return true;
        }

        private static bool TryParseNonNegativeLong(string raw, out long value)
        {
            value = 0L;
            if (!long.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
                return false;
            if (parsed < 0L)
                return false;
            value = parsed;
            return true;
        }

        private static bool TryUpdateSalaryConfig(int countryId, long userId, string configKey, decimal newValue, string actionName)
        {
            decimal oldValue = EconomyConfigData.GetDecimalForCountry(countryId, configKey, EconomyConfigData.GetDecimal(configKey, 0m));
            if (!EconomyConfigData.UpsertCountryDecimal(countryId, configKey, newValue))
                return false;

            LogAdminAction(
                countryId,
                userId,
                actionName,
                oldValue.ToString(CultureInfo.InvariantCulture),
                newValue.ToString(CultureInfo.InvariantCulture),
                $"Country:{countryId}:{configKey}"
            );

            return true;
        }

        private static string StatusEmoji(float value)
        {
            if (value >= 70) return "🟢";
            if (value >= 40) return "🟡";
            if (value >= 20) return "🟠";
            return "🔴";
        }

    }
}

