#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DNDBot.DataBase;
using DNDBot.Goverment.Economy;
using Telegram.Bot;

namespace DNDBot.Goverment
{
    public partial class StateAdministration
    {
        private MilitaryIntelPlanContext GetOrCreateMilitaryIntelPlan(long userId)
        {
            if (!MilitaryIntelPlanByUser.TryGetValue(userId, out var plan))
            {
                plan = new MilitaryIntelPlanContext();
                MilitaryIntelPlanByUser[userId] = plan;
            }

            plan.OperationType = MilitaryIntelService.NormalizeOperationType(plan.OperationType);
            plan.BudgetGold = NormalizeMilitaryIntelBudget(plan.BudgetGold);
            return plan;
        }

        private static long NormalizeMilitaryIntelBudget(long budgetGold)
        {
            if (budgetGold < 100)
                return 100;
            if (budgetGold > 100_000)
                return 100_000;
            return budgetGold;
        }

        private async Task ShowMilitaryIntelMenu(ITelegramBotClient botClient, long userId, long chatId, int msgId, CancellationToken cancellationToken)
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

            var plan = GetOrCreateMilitaryIntelPlan(userId);
            plan.BudgetGold = NormalizeMilitaryIntelBudget(plan.BudgetGold);

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

            string ownName = string.IsNullOrWhiteSpace(ownCountry.Name) ? $"#{ownCountry.CountryId}" : ownCountry.Name;
            string targetName = targetCountry == null
                ? "не обрана"
                : (string.IsNullOrWhiteSpace(targetCountry.Name) ? $"#{targetCountry.CountryId}" : targetCountry.Name);

            string previewText = "Оберіть ціль, щоб порахувати шанси операції.";
            if (targetCountry != null)
            {
                var preview = MilitaryIntelService.PreviewOperation(new MilitaryIntelOperationRequest
                {
                    SourceCountryId = ownCountry.CountryId,
                    TargetCountryId = targetCountry.CountryId,
                    OperationType = plan.OperationType,
                    BudgetGold = plan.BudgetGold,
                    UserId = userId
                });

                if (!preview.IsValid)
                {
                    previewText = $"⚠ {preview.Error}";
                }
                else
                {
                    previewText =
$@"Шанс успіху: {preview.SuccessChance * 100f:0.#}%
Ризик викриття: {preview.DetectionChance * 100f:0.#}%
Перевага розвідки: {preview.IntelligenceAdvantage:+0.##;-0.##;0.##}";
                }
            }

string text =
$@"🕵 Операції розвідки
Держава: {ownName}
Розвідка / контррозвідка: {ownCountry.IntelLevel:0.#}% / {ownCountry.CounterIntelLevel:0.#}%
Казна: {FmtTreasury(ownCountry.Treasury, ownCountry.CountryId)}

Ціль: {targetName}
Операція: {MilitaryIntelService.FormatOperationTypeUk(plan.OperationType)}
Бюджет агентури: {TreasuryCoinService.FormatGoldAmount(plan.BudgetGold)}

Прогноз:
{previewText}";

            await SafeEditMessageTextAsync(
                botClient,
                chatId,
                msgId,
                text,
                StateKeyboardBuilder.MilitaryIntelMenuKeyboard(plan.TargetCountryId.HasValue, plan.OperationType, plan.BudgetGold),
                cancellationToken
            );
        }

        private async Task ShowMilitaryIntelTargetSelect(ITelegramBotClient botClient, long userId, long chatId, int msgId, int page, CancellationToken cancellationToken)
        {
            var selectedCountryId = GetSelectedCountryIdForUser(userId);
            if (!selectedCountryId.HasValue)
                return;

            var countries = CountriesData.GetAllCountries()
                .Where(c => c.CountryId != selectedCountryId.Value)
                .OrderBy(c => c.Name)
                .ToList();

            var plan = GetOrCreateMilitaryIntelPlan(userId);
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
$@"🎯 Вибір цілі для розвідоперації
Ціль: {selectedText}
Сторінка: {FmtNumber(page + 1)}/{FmtNumber(totalPages)}
Доступні держави: {FmtNumber(countries.Count)}";

            await SafeEditMessageTextAsync(
                botClient,
                chatId,
                msgId,
                text,
                StateKeyboardBuilder.MilitaryIntelTargetSelectKeyboard(pageCountries, page, totalPages, plan.TargetCountryId),
                cancellationToken
            );
        }

        private async Task SetMilitaryIntelTarget(ITelegramBotClient botClient, long userId, long chatId, int msgId, int targetCountryId, CancellationToken cancellationToken)
        {
            var selectedCountryId = GetSelectedCountryIdForUser(userId);
            if (!selectedCountryId.HasValue)
                return;

            if (targetCountryId <= 0 || targetCountryId == selectedCountryId.Value)
            {
                await ShowMilitaryIntelTargetSelect(botClient, userId, chatId, msgId, 0, cancellationToken);
                return;
            }

            var targetCountry = CountriesData.GetCountryById(targetCountryId);
            if (targetCountry == null)
            {
                await ShowMilitaryIntelTargetSelect(botClient, userId, chatId, msgId, 0, cancellationToken);
                return;
            }

            var plan = GetOrCreateMilitaryIntelPlan(userId);
            plan.TargetCountryId = targetCountryId;
            await ShowMilitaryIntelMenu(botClient, userId, chatId, msgId, cancellationToken);
        }

        private async Task ShowMilitaryIntelOperationMenu(ITelegramBotClient botClient, long userId, long chatId, int msgId, CancellationToken cancellationToken)
        {
            var plan = GetOrCreateMilitaryIntelPlan(userId);
            string text =
$@"🧭 Тип розвідоперації
Поточний тип: {MilitaryIntelService.FormatOperationTypeUk(plan.OperationType)}";

            await SafeEditMessageTextAsync(
                botClient,
                chatId,
                msgId,
                text,
                StateKeyboardBuilder.MilitaryIntelOperationKeyboard(plan.OperationType),
                cancellationToken
            );
        }

        private async Task SetMilitaryIntelOperation(ITelegramBotClient botClient, long userId, long chatId, int msgId, string operationType, CancellationToken cancellationToken)
        {
            var plan = GetOrCreateMilitaryIntelPlan(userId);
            plan.OperationType = MilitaryIntelService.NormalizeOperationType(operationType);
            await ShowMilitaryIntelOperationMenu(botClient, userId, chatId, msgId, cancellationToken);
        }

        private async Task SetMilitaryIntelBudget(ITelegramBotClient botClient, long userId, long chatId, int msgId, int budgetGold, CancellationToken cancellationToken)
        {
            var plan = GetOrCreateMilitaryIntelPlan(userId);
            plan.BudgetGold = NormalizeMilitaryIntelBudget(budgetGold);
            await ShowMilitaryIntelMenu(botClient, userId, chatId, msgId, cancellationToken);
        }

        private async Task ExecuteMilitaryIntelOperation(ITelegramBotClient botClient, long userId, long chatId, int msgId, CancellationToken cancellationToken)
        {
            var selectedCountryId = GetSelectedCountryIdForUser(userId);
            if (!selectedCountryId.HasValue)
                return;

            var plan = GetOrCreateMilitaryIntelPlan(userId);
            if (!plan.TargetCountryId.HasValue || plan.TargetCountryId.Value == selectedCountryId.Value)
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "Спочатку оберіть ціль для розвідоперації.",
                    StateKeyboardBuilder.MilitaryIntelMenuKeyboard(false, plan.OperationType, plan.BudgetGold),
                    cancellationToken
                );
                return;
            }

            var sourceCountry = CountriesData.GetCountryById(selectedCountryId.Value);
            var targetCountry = CountriesData.GetCountryById(plan.TargetCountryId.Value);
            if (sourceCountry == null || targetCountry == null)
            {
                await ShowMilitaryIntelMenu(botClient, userId, chatId, msgId, cancellationToken);
                return;
            }

            long budget = NormalizeMilitaryIntelBudget(plan.BudgetGold);
            if (sourceCountry.Treasury < budget)
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    $"❌ Недостатньо коштів у казні. Потрібно: {FmtNumber(budget)}.",
                    StateKeyboardBuilder.MilitaryIntelMenuKeyboard(true, plan.OperationType, budget),
                    cancellationToken
                );
                return;
            }

            sourceCountry.Treasury = Math.Max(0L, sourceCountry.Treasury - budget);
            sourceCountry.LastUpdate = DateTime.UtcNow;
            CountriesData.UpdateCountry(sourceCountry);
            EconomyMoneyLogData.AddLog(
                sourceCountry.CountryId,
                userId,
                budget,
                "Expense",
                $"Розвідоперація: {MilitaryIntelService.FormatOperationTypeUk(plan.OperationType)}",
                sourceCountry.TurnsSurvived);

            var result = MilitaryIntelService.ExecuteOperation(new MilitaryIntelOperationRequest
            {
                SourceCountryId = sourceCountry.CountryId,
                TargetCountryId = targetCountry.CountryId,
                OperationType = plan.OperationType,
                BudgetGold = budget,
                UserId = userId
            });

            if (!result.IsValid || !string.IsNullOrWhiteSpace(result.Error))
            {
                sourceCountry = CountriesData.GetCountryById(selectedCountryId.Value);
                if (sourceCountry != null)
                {
                    sourceCountry.Treasury += budget;
                    sourceCountry.LastUpdate = DateTime.UtcNow;
                    CountriesData.UpdateCountry(sourceCountry);
                    EconomyMoneyLogData.AddLog(
                        sourceCountry.CountryId,
                        userId,
                        budget,
                        "Income",
                        "Повернення коштів за невдалий запуск розвідоперації",
                        sourceCountry.TurnsSurvived);
                }

                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    $"❌ Операцію не виконано.\n{(string.IsNullOrWhiteSpace(result.Error) ? "Невідома помилка." : result.Error)}",
                    StateKeyboardBuilder.MilitaryIntelMenuKeyboard(true, plan.OperationType, budget),
                    cancellationToken
                );
                return;
            }

            static string Signed(float value) => value >= 0f ? $"+{value:0.##}" : $"{value:0.##}";
            static string SignedLong(long value) => value >= 0L ? $"+{value:N0}" : $"{value:N0}";

            string sourceName = string.IsNullOrWhiteSpace(sourceCountry.Name) ? $"#{sourceCountry.CountryId}" : sourceCountry.Name;
            string targetName = string.IsNullOrWhiteSpace(targetCountry.Name) ? $"#{targetCountry.CountryId}" : targetCountry.Name;

string text =
$@"🕵 Операція завершена
Тип: {MilitaryIntelService.FormatOperationTypeUk(result.OperationType)}
Сторони: {sourceName} → {targetName}
Бюджет агентури: {TreasuryCoinService.FormatGoldAmount(result.BudgetGold)}

Підсумок:
Результат: {(result.Success ? "✅ Успіх" : "❌ Провал")}
Викриття: {(result.Detected ? "🚨 Так" : "🕶 Ні")}
Шанс успіху: {result.SuccessChance * 100f:0.#}%
Ризик викриття: {result.DetectionChance * 100f:0.#}%
Кидок: {result.Roll * 100f:0.#}%

Ефекти:
Казна {sourceName}: {SignedLong(result.SourceTreasuryDelta)}
Казна {targetName}: {SignedLong(result.TargetTreasuryDelta)}
Розвідка {sourceName}: {Signed(result.SourceIntelDelta)}
Контррозвідка {sourceName}: {Signed(result.SourceCounterIntelDelta)}
Розвідка {targetName}: {Signed(result.TargetIntelDelta)}
Контррозвідка {targetName}: {Signed(result.TargetCounterIntelDelta)}
Мораль армії {targetName}: {Signed(result.TargetArmyMoraleDelta)}
Логістика {targetName}: {Signed(result.TargetLogisticsDelta)}
Зачеплено частин: {FmtNumber(result.AffectedUnits)}
Оцінка сили цілі: {(result.ReconPowerEstimate > 0 ? FmtNumber((long)Math.Round(result.ReconPowerEstimate, MidpointRounding.AwayFromZero)) : "-")}
Оцінка військ цілі: {(result.ReconTroopsEstimate > 0 ? FmtNumber(result.ReconTroopsEstimate) : "-")}

Журнал: #{result.IntelLogId}

{(string.IsNullOrWhiteSpace(result.Report) ? "-" : result.Report)}";

            await SafeEditMessageTextAsync(
                botClient,
                chatId,
                msgId,
                text,
                StateKeyboardBuilder.MilitaryIntelMenuKeyboard(plan.TargetCountryId.HasValue, plan.OperationType, plan.BudgetGold),
                cancellationToken
            );
        }

        private async Task ShowMilitaryIntelLogs(ITelegramBotClient botClient, long userId, long chatId, int msgId, int page, CancellationToken cancellationToken)
        {
            var selectedCountryId = GetSelectedCountryIdForUser(userId);
            if (!selectedCountryId.HasValue)
                return;

            var logs = MilitaryIntelOperationsData.GetByCountry(selectedCountryId.Value, 300);
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
                    bool ownIsSource = log.SourceCountryId == selectedCountryId.Value;
                    int secondCountryId = ownIsSource ? log.TargetCountryId : log.SourceCountryId;
                    string secondName = GetCountryName(secondCountryId);
                    string date = log.CreatedAt.ToLocalTime().ToString("dd.MM HH:mm", CultureInfo.InvariantCulture);
                    string op = MilitaryIntelService.FormatOperationTypeUk(log.OperationType);
                    string icon = ownIsSource
                        ? (log.Success ? "✅" : "❌")
                        : (log.Success ? "⚠" : "🛡");
                    if (log.Detected)
                        icon = "🚨";
                    string text = $"{icon} #{log.IntelLogId} {date} • {op} • {secondName}";
                    return (text, $"state_military_intel_log_{log.IntelLogId}");
                })
                .ToList();

            string infoText =
$@"📜 Журнал розвідоперацій
Записів: {FmtNumber(logs.Count)}
Сторінка: {FmtNumber(page + 1)}/{FmtNumber(totalPages)}";

            await SafeEditMessageTextAsync(
                botClient,
                chatId,
                msgId,
                infoText,
                StateKeyboardBuilder.MilitaryIntelLogsKeyboard(pageItems, page, totalPages),
                cancellationToken
            );
        }

        private async Task ShowMilitaryIntelLogInfo(ITelegramBotClient botClient, long userId, long chatId, int msgId, int intelLogId, CancellationToken cancellationToken)
        {
            var selectedCountryId = GetSelectedCountryIdForUser(userId);
            if (!selectedCountryId.HasValue)
                return;

            var log = MilitaryIntelOperationsData.GetByIdForCountry(intelLogId, selectedCountryId.Value);
            if (log == null)
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "Запис журналу не знайдено.",
                    StateKeyboardBuilder.MilitaryIntelLogInfoKeyboard(),
                    cancellationToken
                );
                return;
            }

            var source = CountriesData.GetCountryById(log.SourceCountryId);
            var target = CountriesData.GetCountryById(log.TargetCountryId);
            string sourceName = source == null || string.IsNullOrWhiteSpace(source.Name) ? $"#{log.SourceCountryId}" : source.Name;
            string targetName = target == null || string.IsNullOrWhiteSpace(target.Name) ? $"#{log.TargetCountryId}" : target.Name;
            bool ownIsSource = selectedCountryId.Value == log.SourceCountryId;

            static string Signed(float value) => value >= 0f ? $"+{value:0.##}" : $"{value:0.##}";
            static string SignedLong(long value) => value >= 0L ? $"+{value:N0}" : $"{value:N0}";

            string reportText = ownIsSource
                ? (string.IsNullOrWhiteSpace(log.Report) ? "-" : log.Report)
                : BuildDefenderIntelReport(log);

string text =
$@"📜 Розвідоперація #{log.IntelLogId}
Дата: {log.CreatedAt.ToLocalTime():dd.MM.yyyy HH:mm}
Тип: {MilitaryIntelService.FormatOperationTypeUk(log.OperationType)}
Сторони: {sourceName} → {targetName}
Бюджет: {TreasuryCoinService.FormatGoldAmount(log.BudgetGold)}
Видимість для цілі: {(log.VisibleToTarget ? "так" : "ні")}

Підсумок:
Результат: {(log.Success ? "✅ Успіх" : "❌ Провал")}
Викриття: {(log.Detected ? "🚨 Так" : "🕶 Ні")}
Шанс успіху: {log.SuccessChance * 100f:0.#}%
Ризик викриття: {log.DetectionChance * 100f:0.#}%
Кидок: {log.Roll * 100f:0.#}%

Ефекти:
Казна джерела: {SignedLong(log.SourceTreasuryDelta)}
Казна цілі: {SignedLong(log.TargetTreasuryDelta)}
Розвідка джерела: {Signed(log.SourceIntelDelta)}
Контррозвідка джерела: {Signed(log.SourceCounterIntelDelta)}
Розвідка цілі: {Signed(log.TargetIntelDelta)}
Контррозвідка цілі: {Signed(log.TargetCounterIntelDelta)}
Мораль армії цілі: {Signed(log.TargetArmyMoraleDelta)}
Логістика цілі: {Signed(log.TargetLogisticsDelta)}
Зачеплено частин: {FmtNumber(log.AffectedUnits)}
Оцінка сили цілі: {(log.ReconPowerEstimate > 0 ? FmtNumber((long)Math.Round(log.ReconPowerEstimate, MidpointRounding.AwayFromZero)) : "-")}
Оцінка військ цілі: {(log.ReconTroopsEstimate > 0 ? FmtNumber(log.ReconTroopsEstimate) : "-")}

Звіт:
{reportText}";

            await SafeEditMessageTextAsync(
                botClient,
                chatId,
                msgId,
                text,
                StateKeyboardBuilder.MilitaryIntelLogInfoKeyboard(),
                cancellationToken
            );
        }

        private static string BuildDefenderIntelReport(MilitaryIntelOperationLogRecord log)
        {
            if (!log.Detected)
                return "Деталі операції приховані.";

            if (!log.Success)
                return "Контррозвідка зірвала спробу проникнення.";

            if (log.TargetTreasuryDelta < 0)
                return $"Підтверджено фінансові втрати: {TreasuryCoinService.FormatGoldAmount(Math.Abs(log.TargetTreasuryDelta))}.";

            if (log.TargetArmyMoraleDelta < 0 || log.TargetLogisticsDelta < 0)
                return $"Виявлено диверсію тилу. Зачеплено частин: {log.AffectedUnits:N0}.";

            if (log.TargetIntelDelta < 0 || log.TargetCounterIntelDelta < 0)
                return "Виявлено атаку на агентурну інфраструктуру.";

            return "Операція зафіксована контррозвідкою.";
        }
    }
}
