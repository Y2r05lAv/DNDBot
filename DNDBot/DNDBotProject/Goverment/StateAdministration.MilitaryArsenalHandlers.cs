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

namespace DNDBot.Goverment
{
    public partial class StateAdministration
    {
        private const int MilitaryArsenalOfferPageSize = 10;
        private const int MilitaryArsenalUnitPageSize = 10;
        private const decimal MilitaryProvisionPowerPerSoldierAtFullCoverage = 0.01m;
        private const int MilitaryProvisionBuildPlanCap = 96;
        private const int MilitaryProvisionDiversifiedCandidateCount = 8;

        private async Task ShowMilitaryArsenalMenu(
            ITelegramBotClient botClient,
            long userId,
            long chatId,
            int msgId,
            CancellationToken cancellationToken,
            string? status = null)
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
                    cancellationToken);
                return;
            }

            MilitaryArsenalCatalog.EnsureResourcesInEconomyCatalog();

            var country = CountriesData.GetCountryById(selectedCountryId.Value);
            var stock = MilitaryArsenalService.GetStockOverview(selectedCountryId.Value);
            var offers = MilitaryArsenalService.GetCompanyOffers(selectedCountryId.Value);
            var units = MilitaryUnitsData.GetByCountry(selectedCountryId.Value)
                .Where(u => Math.Max(0, u.Soldiers) > 0)
                .ToList();

            decimal totalQty = stock.Sum(s => s.Quantity);
            decimal totalPower = stock.Sum(s => s.PowerPoints);
            int resourceKinds = stock.Count;
            int militaryCompanies = EconomyCompaniesData.GetByCountry(selectedCountryId.Value)
                .Count(IsMilitaryCompanyProfile);
            decimal coveragePercent = GetMilitaryProvisionCoveragePercent(selectedCountryId.Value);

            string countryName = country == null || string.IsNullOrWhiteSpace(country.Name)
                ? $"#{selectedCountryId.Value}"
                : country.Name;

            var sb = new StringBuilder();
            sb.AppendLine($"🗃 Арсенал держави: {countryName}");
            sb.AppendLine($"Номенклатура (каталог): {FmtNumber(MilitaryArsenalCatalog.All.Count)} типів");
            sb.AppendLine($"На складі: {FmtNumber(resourceKinds)} типів / {FmtNumberCompact(totalQty)} од.");
            sb.AppendLine($"Сумарна бойова сила запасу: {FmtNumberCompact(totalPower)}");
            sb.AppendLine($"Оцінка забезпечення армії: {FmtNumberCompact(coveragePercent)}%");
            sb.AppendLine($"Військові компанії: {FmtNumber(militaryCompanies)}");
            sb.AppendLine($"Пропозиції компаній (сьогодні): {FmtNumber(offers.Count)}");
            sb.AppendLine($"Частини для забезпечення: {FmtNumber(units.Count)}");
            sb.AppendLine();
            sb.AppendLine("Тут можна купувати амуніцію у компаній і розподіляти її вручну або автоматично між військами.");

            if (!string.IsNullOrWhiteSpace(status))
            {
                sb.AppendLine();
                sb.AppendLine(status);
            }

            await SafeEditMessageTextAsync(
                botClient,
                chatId,
                msgId,
                sb.ToString(),
                StateKeyboardBuilder.MilitaryArsenalKeyboard(),
                cancellationToken);
        }

        private async Task ShowMilitaryArsenalStock(
            ITelegramBotClient botClient,
            long userId,
            long chatId,
            int msgId,
            CancellationToken cancellationToken,
            string? status = null)
        {
            var selectedCountryId = GetSelectedCountryIdForUser(userId);
            if (!selectedCountryId.HasValue)
                return;

            MilitaryArsenalCatalog.EnsureResourcesInEconomyCatalog();

            var stock = MilitaryArsenalService.GetStockOverview(selectedCountryId.Value);
            decimal totalQty = stock.Sum(s => s.Quantity);
            decimal totalPower = stock.Sum(s => s.PowerPoints);
            int catalogTypes = MilitaryArsenalCatalog.All.Count;

            var categorySummary = stock
                .GroupBy(s => s.Category)
                .Select(g => new
                {
                    Category = g.Key,
                    Quantity = g.Sum(x => x.Quantity),
                    Power = g.Sum(x => x.PowerPoints)
                })
                .OrderByDescending(x => x.Power)
                .ToList();

            var sb = new StringBuilder();
            sb.AppendLine("📦 Склади амуніції та спорядження");
            sb.AppendLine($"Типів на складі: {FmtNumber(stock.Count)}");
            sb.AppendLine($"Типів у повному каталозі: {FmtNumber(catalogTypes)}");
            sb.AppendLine($"Загальна кількість: {FmtNumberCompact(totalQty)}");
            sb.AppendLine($"Сумарна бойова сила запасу: {FmtNumberCompact(totalPower)}");

            if (categorySummary.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("По категоріях:");
                foreach (var item in categorySummary)
                {
                    sb.AppendLine($"• {MilitaryArsenalCatalog.FormatCategoryUk(item.Category)}: {FmtNumberCompact(item.Quantity)} (сила {FmtNumberCompact(item.Power)})");
                }
            }

            if (stock.Count == 0)
            {
                sb.AppendLine();
                sb.AppendLine("Склад порожній. Закупіть амуніцію у підрозділі держзакупівель.");
            }
            else
            {
                sb.AppendLine();
                sb.AppendLine("Позиції:");
                foreach (var item in stock.Take(28))
                {
                    sb.AppendLine($"• {item.DisplayNameUk}: {FmtNumberCompact(item.Quantity)} (сила {FmtNumberCompact(item.PowerPoints)})");
                }

                if (stock.Count > 28)
                    sb.AppendLine($"… ще {FmtNumber(stock.Count - 28)} позицій.");
            }

            sb.AppendLine();
            sb.AppendLine("Повний перелік типів доступний у: Економіка → Ресурси → Військові ресурси.");

            if (!string.IsNullOrWhiteSpace(status))
            {
                sb.AppendLine();
                sb.AppendLine(status);
            }

            await SafeEditMessageTextAsync(
                botClient,
                chatId,
                msgId,
                sb.ToString(),
                StateKeyboardBuilder.MilitaryArsenalStockKeyboard(),
                cancellationToken);
        }

        private async Task ShowMilitaryArsenalProcurement(
            ITelegramBotClient botClient,
            long userId,
            long chatId,
            int msgId,
            int page,
            CancellationToken cancellationToken,
            string? status = null)
        {
            var selectedCountryId = GetSelectedCountryIdForUser(userId);
            if (!selectedCountryId.HasValue)
                return;

            var offers = MilitaryArsenalService.GetCompanyOffers(selectedCountryId.Value);
            int totalPages = Math.Max(1, (int)Math.Ceiling(offers.Count / (double)MilitaryArsenalOfferPageSize));
            int safePage = Math.Max(0, Math.Min(page, totalPages - 1));
            var pageItems = offers
                .Skip(safePage * MilitaryArsenalOfferPageSize)
                .Take(MilitaryArsenalOfferPageSize)
                .ToList();

            var keyboardItems = pageItems
                .Select(o =>
                {
                    string owner = o.IsStateCompany ? "Держ." : "Прив.";
                    string text = $"{owner} {o.CompanyName} • {o.ResourceDisplayUk} ({FmtNumberCompact(o.QuantityPerDay)}/д)";
                    string data = $"state_military_arsenal_offer|{o.CompanyId}|{EncodeResourceKey(o.ResourceType)}|{safePage}";
                    return (text, data);
                })
                .ToList();

            var sb = new StringBuilder();
            sb.AppendLine("🧾 Держзакупівлі амуніції");
            sb.AppendLine($"Пропозицій: {FmtNumber(offers.Count)}");
            sb.AppendLine($"Сторінка: {FmtNumber(safePage + 1)}/{FmtNumber(totalPages)}");
            sb.AppendLine();

            if (pageItems.Count == 0)
            {
                sb.AppendLine("Поки що немає компаній, які випускають військові ресурси.");
                sb.AppendLine("Створіть або активуйте військову компанію та прив'яжіть до неї будівлі.");
            }
            else
            {
                sb.AppendLine("Оберіть пропозицію для купівлі.");
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                sb.AppendLine();
                sb.AppendLine(status);
            }

            await SafeEditMessageTextAsync(
                botClient,
                chatId,
                msgId,
                sb.ToString(),
                StateKeyboardBuilder.MilitaryArsenalProcurementKeyboard(keyboardItems, safePage, totalPages),
                cancellationToken);
        }

        private async Task ShowMilitaryArsenalOffer(
            ITelegramBotClient botClient,
            long userId,
            long chatId,
            int msgId,
            int companyId,
            string resourceKey,
            int returnPage,
            CancellationToken cancellationToken,
            string? status = null)
        {
            var selectedCountryId = GetSelectedCountryIdForUser(userId);
            if (!selectedCountryId.HasValue)
                return;

            string resourceType = DecodeResourceKey(resourceKey);
            if (string.IsNullOrWhiteSpace(resourceType))
            {
                await ShowMilitaryArsenalProcurement(
                    botClient,
                    userId,
                    chatId,
                    msgId,
                    returnPage,
                    cancellationToken,
                    "❌ Пропозиція недоступна: ресурс не знайдено.");
                return;
            }

            var offer = MilitaryArsenalService.GetCompanyOffers(selectedCountryId.Value)
                .FirstOrDefault(o =>
                    o.CompanyId == companyId &&
                    o.ResourceType.Equals(resourceType, StringComparison.OrdinalIgnoreCase));

            if (offer == null)
            {
                await ShowMilitaryArsenalProcurement(
                    botClient,
                    userId,
                    chatId,
                    msgId,
                    returnPage,
                    cancellationToken,
                    "❌ Пропозиція компанії вже недоступна або змінилась.");
                return;
            }

            decimal available = Math.Max(0m, offer.QuantityPerDay);
            decimal maxCost = decimal.Ceiling(available * offer.UnitPrice);
            string owner = offer.IsStateCompany ? "Державна" : "Приватна";

            var actions = new List<(string text, string data)>();
            foreach (int percent in new[] { 10, 25, 50, 100 })
            {
                decimal qty = decimal.Round(available * percent / 100m, 2, MidpointRounding.AwayFromZero);
                if (qty <= 0m)
                    continue;

                decimal pay = decimal.Ceiling(qty * offer.UnitPrice);
                actions.Add((
                    $"{percent}% • {FmtNumberCompact(qty)} од. ({FmtNumberCompact(pay)})",
                    $"state_military_arsenal_buy|{companyId}|{EncodeResourceKey(offer.ResourceType)}|{percent}|{returnPage}"));
            }

            if (actions.Count == 0)
            {
                await ShowMilitaryArsenalProcurement(
                    botClient,
                    userId,
                    chatId,
                    msgId,
                    returnPage,
                    cancellationToken,
                    "❌ Немає доступної кількості для купівлі.");
                return;
            }

            var sb = new StringBuilder();
            sb.AppendLine("🏭 Картка пропозиції");
            sb.AppendLine($"Компанія: {offer.CompanyName} ({owner})");
            sb.AppendLine($"Ресурс: {offer.ResourceDisplayUk}");
            sb.AppendLine($"Категорія: {MilitaryArsenalCatalog.FormatCategoryUk(offer.Category)}");
            sb.AppendLine($"Доступно за день: {FmtNumberCompact(available)}");
            sb.AppendLine($"Ціна за 1: {FmtNumberCompact(offer.UnitPrice)}");
            sb.AppendLine($"Макс. повний викуп: {FmtNumberCompact(maxCost)}");
            sb.AppendLine();
            sb.AppendLine("Оберіть пакет закупівлі:");

            if (!string.IsNullOrWhiteSpace(status))
            {
                sb.AppendLine();
                sb.AppendLine(status);
            }

            await SafeEditMessageTextAsync(
                botClient,
                chatId,
                msgId,
                sb.ToString(),
                StateKeyboardBuilder.MilitaryArsenalOfferKeyboard(actions, returnPage),
                cancellationToken);
        }

        private async Task BuyMilitaryArsenalOffer(
            ITelegramBotClient botClient,
            long userId,
            long chatId,
            int msgId,
            int companyId,
            string resourceKey,
            int percent,
            int returnPage,
            CancellationToken cancellationToken)
        {
            var selectedCountryId = GetSelectedCountryIdForUser(userId);
            if (!selectedCountryId.HasValue)
                return;

            string resourceType = DecodeResourceKey(resourceKey);
            if (string.IsNullOrWhiteSpace(resourceType))
            {
                await ShowMilitaryArsenalProcurement(
                    botClient,
                    userId,
                    chatId,
                    msgId,
                    returnPage,
                    cancellationToken,
                    "❌ Невірний ресурс у запиті закупівлі.");
                return;
            }

            var offer = MilitaryArsenalService.GetCompanyOffers(selectedCountryId.Value)
                .FirstOrDefault(o =>
                    o.CompanyId == companyId &&
                    o.ResourceType.Equals(resourceType, StringComparison.OrdinalIgnoreCase));
            if (offer == null)
            {
                await ShowMilitaryArsenalProcurement(
                    botClient,
                    userId,
                    chatId,
                    msgId,
                    returnPage,
                    cancellationToken,
                    "❌ Пропозиція недоступна.");
                return;
            }

            int safePercent = Math.Max(1, Math.Min(100, percent));
            decimal requestedQty = decimal.Round(offer.QuantityPerDay * safePercent / 100m, 2, MidpointRounding.AwayFromZero);
            if (requestedQty <= 0m)
                requestedQty = Math.Min(offer.QuantityPerDay, 1m);

            var result = MilitaryArsenalService.PurchaseFromCompany(
                selectedCountryId.Value,
                companyId,
                offer.ResourceType,
                requestedQty,
                userId);

            string prefix = result.Success ? "✅ " : "❌ ";
            await ShowMilitaryArsenalOffer(
                botClient,
                userId,
                chatId,
                msgId,
                companyId,
                EncodeResourceKey(offer.ResourceType),
                returnPage,
                cancellationToken,
                prefix + result.Message);
        }

        private async Task ApplyMilitaryArsenalAutoDistribution(
            ITelegramBotClient botClient,
            long userId,
            long chatId,
            int msgId,
            CancellationToken cancellationToken)
        {
            var selectedCountryId = GetSelectedCountryIdForUser(userId);
            if (!selectedCountryId.HasValue)
                return;

            var result = MilitaryArsenalService.AutoDistributeAll(selectedCountryId.Value);
            string prefix = result.Success ? "✅ " : "❌ ";
            await ShowMilitaryArsenalStock(
                botClient,
                userId,
                chatId,
                msgId,
                cancellationToken,
                prefix + result.Message);
        }

        private async Task StartMilitaryArsenalAutoProvisionInput(
            ITelegramBotClient botClient,
            long userId,
            long chatId,
            int msgId,
            CancellationToken cancellationToken)
        {
            var selectedCountryId = GetSelectedCountryIdForUser(userId);
            if (!selectedCountryId.HasValue)
                return;

            int countryId = selectedCountryId.Value;
            decimal armySoldiers = GetArmySoldiersForProvision(countryId);
            if (armySoldiers <= 0m)
            {
                await ShowMilitaryArsenalMenu(
                    botClient,
                    userId,
                    chatId,
                    msgId,
                    cancellationToken,
                    "❌ Немає активної армії для розрахунку забезпечення.");
                return;
            }

            decimal currentCoverage = GetMilitaryProvisionCoveragePercent(countryId);
            decimal currentPower = GetMilitaryProvisionPowerPerDay(countryId);

            EconomyMilitaryProvisionUiContext[userId] = (chatId, msgId);
            EconomyInputState[userId] = EconomyInputMode.MilitaryAutoProvisionPercent;
            EconomyInputStep[userId] = 0;

            await SendHelperMessageAsync(
                botClient,
                userId,
                $@"📈 Автозабезпечення армії
Поточне забезпечення: {FmtNumberCompact(currentCoverage)}%
Поточна добова сила виробництва: {FmtNumberCompact(currentPower)}
Оцінка чисельності війська: {FmtNumberCompact(armySoldiers)}

Введіть цільовий відсоток забезпечення (1..300).",
                cancellationToken);
        }

        private async Task HandleMilitaryArsenalAutoProvisionInput(
            ITelegramBotClient botClient,
            long userId,
            string input,
            CancellationToken cancellationToken)
        {
            var selectedCountryId = GetSelectedCountryIdForUser(userId);
            if (!selectedCountryId.HasValue)
            {
                ClearEconomyInput(userId);
                await botClient.SendTextMessageAsync(userId, "Не знайдено країни.", cancellationToken: cancellationToken);
                return;
            }

            if (!TryParseDecimal(input, out decimal targetPercent) || targetPercent < 1m || targetPercent > 300m)
            {
                await SendHelperMessageAsync(botClient, userId, "❌ Введіть число в діапазоні 1..300.", cancellationToken);
                return;
            }

            string resultMessage = ExecuteMilitaryAutoProvision(selectedCountryId.Value, userId, targetPercent);

            EconomyMilitaryProvisionUiContext.TryGetValue(userId, out var uiContext);
            await CleanupHelperMessagesAsync(botClient, userId, cancellationToken);
            ClearEconomyInput(userId);

            if (uiContext.ChatId != 0 && uiContext.MsgId != 0)
            {
                await ShowMilitaryArsenalMenu(
                    botClient,
                    userId,
                    uiContext.ChatId,
                    uiContext.MsgId,
                    cancellationToken,
                    resultMessage);
                return;
            }

            await botClient.SendTextMessageAsync(userId, resultMessage, cancellationToken: cancellationToken);
        }

        private sealed class MilitaryProvisionTemplateCandidate
        {
            public string Key { get; init; } = string.Empty;
            public string MenuTitle { get; init; } = string.Empty;
            public decimal BuildCost { get; init; }
            public int BuildTurns { get; init; }
            public decimal PowerPerDay { get; init; }
        }

        private string ExecuteMilitaryAutoProvision(int countryId, long userId, decimal targetPercent)
        {
            MilitaryArsenalCatalog.EnsureResourcesInEconomyCatalog();

            decimal soldiers = GetArmySoldiersForProvision(countryId);
            if (soldiers <= 0m)
                return "❌ Немає активної армії для розрахунку забезпечення.";

            decimal normalizedTarget = decimal.Round(Math.Max(1m, Math.Min(300m, targetPercent)), 2, MidpointRounding.AwayFromZero);
            decimal requiredPowerFor100 = Math.Max(1m, soldiers * MilitaryProvisionPowerPerSoldierAtFullCoverage);
            decimal currentPower = GetMilitaryProvisionPowerPerDay(countryId);
            decimal currentCoverage = Math.Max(0m, decimal.Round(currentPower / requiredPowerFor100 * 100m, 2, MidpointRounding.AwayFromZero));
            decimal targetPower = requiredPowerFor100 * normalizedTarget / 100m;

            if (currentPower >= targetPower)
            {
                return $"✅ Забезпечення вже на рівні {FmtNumberCompact(currentCoverage)}% (ціль {FmtNumberCompact(normalizedTarget)}%). Нові будівлі не потрібні.";
            }

            var candidates = BuildMilitaryProvisionCandidates();
            if (candidates.Count == 0)
                return "❌ Не знайдено військових виробничих шаблонів для автозабезпечення.";

            decimal requiredExtraPower = targetPower - currentPower;
            int diversifiedCount = Math.Max(1, Math.Min(MilitaryProvisionDiversifiedCandidateCount, candidates.Count));
            var diversified = candidates.Take(diversifiedCount).ToList();

            var plannedCountByKey = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            decimal plannedExtraPower = 0m;
            int plannedBuildings = 0;
            int cursor = 0;

            while (plannedExtraPower < requiredExtraPower && plannedBuildings < MilitaryProvisionBuildPlanCap)
            {
                var candidate = diversified[cursor % diversified.Count];
                if (!plannedCountByKey.TryGetValue(candidate.Key, out int count))
                    count = 0;
                plannedCountByKey[candidate.Key] = count + 1;
                plannedExtraPower += candidate.PowerPerDay;
                plannedBuildings++;
                cursor++;
            }

            if (plannedBuildings == 0)
                return "❌ Не вдалося згенерувати план автозабезпечення.";

            var candidateMap = candidates.ToDictionary(c => c.Key, c => c, StringComparer.OrdinalIgnoreCase);
            var builtCountByKey = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            decimal spentTotal = 0m;
            decimal addedPower = 0m;
            int addedBuildings = 0;
            string failureReason = string.Empty;
            bool stoppedByFunds = false;

            foreach (var plan in plannedCountByKey.OrderByDescending(p => p.Value))
            {
                if (!candidateMap.TryGetValue(plan.Key, out var candidate))
                    continue;

                for (int i = 0; i < plan.Value; i++)
                {
                    var building = BuildTemplateFromCallback(countryId, candidate.Key);
                    if (building == null)
                        continue;

                    if (!TryAddBuildingWithPayment(
                        userId,
                        building,
                        out _,
                        out decimal chargedCost,
                        out _,
                        out string error))
                    {
                        stoppedByFunds = true;
                        failureReason = string.IsNullOrWhiteSpace(error) ? "Недостатньо коштів або лімітів для продовження будівництва." : error;
                        break;
                    }

                    addedBuildings++;
                    spentTotal += chargedCost;
                    addedPower += candidate.PowerPerDay;
                    if (!builtCountByKey.TryGetValue(candidate.Key, out int builtCount))
                        builtCount = 0;
                    builtCountByKey[candidate.Key] = builtCount + 1;
                }

                if (stoppedByFunds)
                    break;
            }

            if (addedBuildings == 0)
            {
                return $"❌ Автозабезпечення не виконано. {failureReason}";
            }

            decimal projectedPower = currentPower + addedPower;
            decimal projectedCoverage = Math.Max(0m, decimal.Round(projectedPower / requiredPowerFor100 * 100m, 2, MidpointRounding.AwayFromZero));

            var builtLines = builtCountByKey
                .OrderByDescending(x => x.Value)
                .ThenBy(x => x.Key, StringComparer.OrdinalIgnoreCase)
                .Take(10)
                .Select(x =>
                {
                    string name = candidateMap.TryGetValue(x.Key, out var c) ? c.MenuTitle : x.Key;
                    return $"• {name} ×{FmtNumber(x.Value)}";
                })
                .ToList();

            var sb = new StringBuilder();
            sb.AppendLine("✅ Автозабезпечення виконано.");
            sb.AppendLine($"Ціль: {FmtNumberCompact(normalizedTarget)}% | Було: {FmtNumberCompact(currentCoverage)}% | Стане: {FmtNumberCompact(projectedCoverage)}%");
            sb.AppendLine($"Оцінка армії: {FmtNumberCompact(soldiers)} ос.");
            sb.AppendLine($"Додано будівель: {FmtNumber(addedBuildings)}");
            sb.AppendLine($"Планова добова сила +{FmtNumberCompact(addedPower)}");
            sb.AppendLine($"Списано з казни: {FmtNumberCompact(spentTotal)}");

            if (builtLines.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("Побудовано:");
                foreach (var line in builtLines)
                    sb.AppendLine(line);
                if (builtCountByKey.Count > builtLines.Count)
                    sb.AppendLine($"… ще {FmtNumber(builtCountByKey.Count - builtLines.Count)} типів.");
            }

            if (stoppedByFunds)
            {
                sb.AppendLine();
                sb.AppendLine($"⚠ Будівництво зупинено раніше плану: {failureReason}");
            }

            return sb.ToString();
        }

        private static decimal GetArmySoldiersForProvision(int countryId)
        {
            if (countryId <= 0)
                return 0m;

            decimal fromUnits = MilitaryUnitsData.GetByCountry(countryId)
                .Sum(u => Math.Max(0, u.Soldiers));
            if (fromUnits > 0m)
                return fromUnits;

            var country = CountriesData.GetCountryById(countryId);
            if (country == null)
                return 0m;

            return Math.Max(0m, country.ArmySize);
        }

        private static decimal GetMilitaryProvisionPowerPerDay(int countryId)
        {
            if (countryId <= 0)
                return 0m;

            var buildings = EconomyBuildingsData.GetByCountry(countryId);
            decimal totalPower = 0m;

            foreach (var building in buildings)
            {
                if (!AreBuildingEffectsActive(building))
                    continue;
                if (building.BaseProduction <= 0m)
                    continue;

                bool militaryCategory = string.Equals(building.Category, "Military", StringComparison.OrdinalIgnoreCase);
                bool militaryResource = MilitaryArsenalCatalog.IsMilitaryResourceType(building.ResourceType);
                if (!militaryCategory && !militaryResource)
                    continue;

                decimal weight = militaryResource
                    ? Math.Max(0.5m, MilitaryArsenalCatalog.GetCombatPowerWeight(building.ResourceType))
                    : 0.85m;

                totalPower += Math.Max(0m, building.BaseProduction) * weight;
            }

            return decimal.Round(Math.Max(0m, totalPower), 3, MidpointRounding.AwayFromZero);
        }

        private static decimal GetMilitaryProvisionCoveragePercent(int countryId)
        {
            decimal soldiers = GetArmySoldiersForProvision(countryId);
            if (soldiers <= 0m)
                return 0m;

            decimal requiredPowerFor100 = Math.Max(1m, soldiers * MilitaryProvisionPowerPerSoldierAtFullCoverage);
            decimal currentPower = GetMilitaryProvisionPowerPerDay(countryId);
            decimal coverage = currentPower / requiredPowerFor100 * 100m;
            return decimal.Round(Math.Max(0m, coverage), 2, MidpointRounding.AwayFromZero);
        }

        private static List<MilitaryProvisionTemplateCandidate> BuildMilitaryProvisionCandidates()
        {
            var list = MilitaryBuildingTemplateCatalog
                .Where(t => t.BaseProduction > 0m && MilitaryArsenalCatalog.IsMilitaryResourceType(t.ResourceType))
                .Select(t =>
                {
                    decimal weight = Math.Max(0.5m, MilitaryArsenalCatalog.GetCombatPowerWeight(t.ResourceType));
                    decimal power = Math.Max(0.1m, decimal.Round(t.BaseProduction * weight, 3, MidpointRounding.AwayFromZero));
                    return new MilitaryProvisionTemplateCandidate
                    {
                        Key = t.Key,
                        MenuTitle = t.MenuTitle,
                        BuildCost = t.BuildCost,
                        BuildTurns = t.BuildTurns,
                        PowerPerDay = power
                    };
                })
                .OrderByDescending(c => c.PowerPerDay / Math.Max(1m, c.BuildCost))
                .ThenByDescending(c => c.PowerPerDay)
                .ThenBy(c => c.BuildCost)
                .ThenBy(c => c.MenuTitle, StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (list.Count > 0)
                return list;

            return MilitaryBuildingTemplateCatalog
                .Where(t => t.BaseProduction > 0m)
                .Select(t => new MilitaryProvisionTemplateCandidate
                {
                    Key = t.Key,
                    MenuTitle = t.MenuTitle,
                    BuildCost = t.BuildCost,
                    BuildTurns = t.BuildTurns,
                    PowerPerDay = Math.Max(0.1m, decimal.Round(t.BaseProduction * 0.8m, 3, MidpointRounding.AwayFromZero))
                })
                .OrderByDescending(c => c.PowerPerDay / Math.Max(1m, c.BuildCost))
                .ThenByDescending(c => c.PowerPerDay)
                .ThenBy(c => c.BuildCost)
                .ThenBy(c => c.MenuTitle, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private async Task ShowMilitaryArsenalManualDistribution(
            ITelegramBotClient botClient,
            long userId,
            long chatId,
            int msgId,
            int page,
            CancellationToken cancellationToken,
            string? status = null)
        {
            var selectedCountryId = GetSelectedCountryIdForUser(userId);
            if (!selectedCountryId.HasValue)
                return;

            var units = MilitaryUnitsData.GetByCountry(selectedCountryId.Value)
                .Where(u => Math.Max(0, u.Soldiers) > 0)
                .OrderByDescending(u => u.Soldiers)
                .ThenBy(u => u.UnitId)
                .ToList();

            int totalPages = Math.Max(1, (int)Math.Ceiling(units.Count / (double)MilitaryArsenalUnitPageSize));
            int safePage = Math.Max(0, Math.Min(page, totalPages - 1));

            var unitPage = units
                .Skip(safePage * MilitaryArsenalUnitPageSize)
                .Take(MilitaryArsenalUnitPageSize)
                .ToList();

            var keyboardItems = unitPage
                .Select(u =>
                {
                    string text = $"#{u.UnitId} {u.UnitName} ({FmtNumber(u.Soldiers)} ос.)";
                    string data = $"state_military_arsenal_unit|{u.UnitId}|{safePage}";
                    return (text, data);
                })
                .ToList();

            var stock = MilitaryArsenalService.GetStockOverview(selectedCountryId.Value);
            decimal power = stock.Sum(s => s.PowerPoints);

            var sb = new StringBuilder();
            sb.AppendLine("✋ Ручний розподіл амуніції");
            sb.AppendLine($"Частин доступно: {FmtNumber(units.Count)}");
            sb.AppendLine($"Сумарна сила складу: {FmtNumberCompact(power)}");
            sb.AppendLine($"Сторінка: {FmtNumber(safePage + 1)}/{FmtNumber(totalPages)}");
            sb.AppendLine();
            sb.AppendLine("Оберіть частину для цільового забезпечення.");

            if (!string.IsNullOrWhiteSpace(status))
            {
                sb.AppendLine();
                sb.AppendLine(status);
            }

            await SafeEditMessageTextAsync(
                botClient,
                chatId,
                msgId,
                sb.ToString(),
                StateKeyboardBuilder.MilitaryArsenalManualDistributionKeyboard(keyboardItems, safePage, totalPages),
                cancellationToken);
        }

        private async Task ShowMilitaryArsenalUnitDistribution(
            ITelegramBotClient botClient,
            long userId,
            long chatId,
            int msgId,
            int unitId,
            int returnPage,
            CancellationToken cancellationToken,
            string? status = null)
        {
            var selectedCountryId = GetSelectedCountryIdForUser(userId);
            if (!selectedCountryId.HasValue)
                return;

            var unit = MilitaryUnitsData.GetByCountry(selectedCountryId.Value)
                .FirstOrDefault(u => u.UnitId == unitId);
            if (unit == null)
            {
                await ShowMilitaryArsenalManualDistribution(
                    botClient,
                    userId,
                    chatId,
                    msgId,
                    returnPage,
                    cancellationToken,
                    "❌ Частину не знайдено.");
                return;
            }

            var stock = MilitaryArsenalService.GetStockOverview(selectedCountryId.Value);
            decimal totalPower = stock.Sum(s => s.PowerPoints);
            decimal totalQty = stock.Sum(s => s.Quantity);
            bool hasStock = stock.Any();

            var sb = new StringBuilder();
            sb.AppendLine($"🪖 Цільовий розподіл: #{unit.UnitId} {unit.UnitName}");
            sb.AppendLine($"Солдати: {FmtNumber(unit.Soldiers)}");
            sb.AppendLine($"Озброєння: {unit.WeaponReadiness:0.#}%");
            sb.AppendLine($"Забезпечення: {unit.SupplyLevel:0.#}%");
            sb.AppendLine($"Якість спорядження: {unit.EquipmentQuality:0.#}%");
            sb.AppendLine();
            sb.AppendLine($"На складі: {FmtNumberCompact(totalQty)} од. | сила {FmtNumberCompact(totalPower)}");

            if (!hasStock)
            {
                sb.AppendLine();
                sb.AppendLine("Склад порожній. Потрібні закупівлі.");
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                sb.AppendLine();
                sb.AppendLine(status);
            }

            await SafeEditMessageTextAsync(
                botClient,
                chatId,
                msgId,
                sb.ToString(),
                StateKeyboardBuilder.MilitaryArsenalUnitDistributionKeyboard(unitId, returnPage, hasStock),
                cancellationToken);
        }

        private async Task ApplyMilitaryArsenalUnitDistribution(
            ITelegramBotClient botClient,
            long userId,
            long chatId,
            int msgId,
            int unitId,
            int sharePercent,
            int returnPage,
            CancellationToken cancellationToken)
        {
            var selectedCountryId = GetSelectedCountryIdForUser(userId);
            if (!selectedCountryId.HasValue)
                return;

            var result = MilitaryArsenalService.DistributeToUnit(selectedCountryId.Value, unitId, sharePercent);
            string prefix = result.Success ? "✅ " : "❌ ";

            await ShowMilitaryArsenalUnitDistribution(
                botClient,
                userId,
                chatId,
                msgId,
                unitId,
                returnPage,
                cancellationToken,
                prefix + result.Message);
        }

        private async Task CreateMilitaryArsenalCompanyQuick(
            ITelegramBotClient botClient,
            long userId,
            long chatId,
            int msgId,
            CancellationToken cancellationToken)
        {
            var selectedCountryId = GetSelectedCountryIdForUser(userId);
            if (!selectedCountryId.HasValue)
                return;

            MilitaryArsenalCatalog.EnsureResourcesInEconomyCatalog();

            int countryId = selectedCountryId.Value;
            var companies = EconomyCompaniesData.GetByCountry(countryId);
            int ordinal = companies.Count(c => IsMilitaryCompanyProfile(c)) + 1;

            decimal baseSalary = EconomyConfigData.GetDecimalForCountry(countryId, "CompanyBaseSalary", 1m);

            var company = new EconomyCompanyRecord
            {
                CountryId = countryId,
                Name = $"Арсенальна компанія #{ordinal}",
                OwnerName = "Держава",
                ProductionType = "MilitaryIndustry",
                JobsOffered = 140,
                BaseSalary = Math.Max(0.1m, baseSalary * 1.25m),
                TaxRate = 0.18m,
                Budget = 1800m,
                IsStateCompany = true,
                MilitaryProductionShare = 85f,
                InputResourceType = "Materials",
                InputAmount = 24m,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            NormalizeCompanyRecord(company);
            int companyId = EconomyCompaniesData.AddCompany(company);
            if (companyId <= 0)
            {
                await ShowMilitaryArsenalMenu(
                    botClient,
                    userId,
                    chatId,
                    msgId,
                    cancellationToken,
                    "❌ Не вдалося створити військову компанію.");
                return;
            }

            var buildings = EconomyBuildingsData.GetByCountry(countryId)
                .Where(IsAssignableMilitaryBuilding)
                .Take(10)
                .ToList();

            int assigned = 0;
            foreach (var building in buildings)
            {
                if (EconomyBuildingsData.UpdateBuildingField(
                    building.BuildingId,
                    countryId,
                    "CompanyId",
                    companyId,
                    SqlDbType.Int))
                {
                    assigned++;
                }
            }

            string message = assigned > 0
                ? $"✅ Створено компанію #{companyId} і прив'язано {FmtNumber(assigned)} військових будівель."
                : $"✅ Створено компанію #{companyId}. Прив'яжіть військові будівлі вручну у меню компаній.";

            await ShowMilitaryArsenalMenu(
                botClient,
                userId,
                chatId,
                msgId,
                cancellationToken,
                message);
        }

        private static bool IsAssignableMilitaryBuilding(EconomyBuildingRecord building)
        {
            if (building == null)
                return false;
            if (building.CompanyId.HasValue)
                return false;
            if (building.BuildTurns > 0)
                return false;
            if (building.BaseProduction <= 0m)
                return false;

            bool militaryCategory = string.Equals(building.Category, "Military", StringComparison.OrdinalIgnoreCase);
            bool militaryResource = MilitaryArsenalCatalog.IsMilitaryResourceType(building.ResourceType);
            return militaryCategory || militaryResource;
        }

        private static bool IsMilitaryCompanyProfile(EconomyCompanyRecord company)
        {
            if (company == null)
                return false;
            if (company.MilitaryProductionShare >= 20f)
                return true;

            string type = (company.ProductionType ?? string.Empty).Trim().ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(type))
                return false;

            return type.Contains("military", StringComparison.Ordinal)
                || type.Contains("weapon", StringComparison.Ordinal)
                || type.Contains("ammo", StringComparison.Ordinal)
                || type.Contains("armor", StringComparison.Ordinal)
                || type.Contains("siege", StringComparison.Ordinal)
                || type.Contains("arcane", StringComparison.Ordinal)
                || type.Contains("війсь", StringComparison.Ordinal)
                || type.Contains("збро", StringComparison.Ordinal)
                || type.Contains("облог", StringComparison.Ordinal)
                || type.Contains("арк", StringComparison.Ordinal);
        }
    }
}
