using DNDBot.BotSystem;
using Microsoft.Extensions.Logging;
using DNDBot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Globalization;
using System.Data;
using DNDBot.DataBase;
using DNDBot.Goverment.Research;
using DNDBot.Goverment.Economy;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace DNDBot.Goverment
{
    public partial class StateAdministration : BotBodyRealisation
    {
        private async Task ShowResearchMenu(ITelegramBotClient botClient, long chatId, long userId, int msgId, CancellationToken cancellationToken)
        {
            var selectedCountryId = GetSelectedCountryIdForUser(userId);
            if (!selectedCountryId.HasValue)
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "Не знайдено країни. Оберіть країну або призначте CountryId персонажу.",
                    StateKeyboardBuilder.PublicOverviewKeyboard(),
                    cancellationToken
                );
                return;
            }

            await SafeEditMessageTextAsync(
                botClient,
                chatId,
                msgId,
                "🔬 Дослідження",
                StateKeyboardBuilder.ResearchMenuKeyboard(userId == masterID),
                cancellationToken
            );
        }

        private async Task ShowResearchList(ITelegramBotClient botClient, long chatId, long userId, int msgId, int page, CancellationToken cancellationToken)
        {
            var selectedCountryId = GetSelectedCountryIdForUser(userId);
            if (!selectedCountryId.HasValue)
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "Не знайдено країни. Оберіть країну або призначте CountryId персонажу.",
                    StateKeyboardBuilder.PublicOverviewKeyboard(),
                    cancellationToken
                );
                return;
            }

            var branches = BuildResearchBranchList();

            await SafeEditMessageTextAsync(
                botClient,
                chatId,
                msgId,
                "🔬 Оберіть галузь досліджень:",
                StateKeyboardBuilder.ResearchBranchesKeyboard(branches),
                cancellationToken
            );
        }

        private async Task ShowResearchBranchList(ITelegramBotClient botClient, long chatId, long userId, int msgId, string branch, int page, CancellationToken cancellationToken)
        {
            var selectedCountryId = GetSelectedCountryIdForUser(userId);
            if (!selectedCountryId.HasValue)
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "Не знайдено країни. Оберіть країну або призначте CountryId персонажу.",
                    StateKeyboardBuilder.PublicOverviewKeyboard(),
                    cancellationToken
                );
                return;
            }

            var service = new ResearchService();
            var definitions = ResearchDefinitions.GetAll()
                .Where(d => d.IsActive && string.Equals(d.Branch, branch, StringComparison.OrdinalIgnoreCase))
                .OrderBy(d => d.SortOrder)
                .ThenBy(d => d.Id)
                .ToList();

            var items = new List<(string text, string data)>();
            foreach (var def in definitions)
            {
                bool canStart = service.CanStartResearch(selectedCountryId.Value, def.Id, out _);
                string status = canStart ? "✅" : "🔒";
                items.Add(($"{status} {def.Name}", $"state_research_info_{def.Id}"));
            }

            int perPage = 10;
            if (page < 0) page = 0;
            int totalPages = (int)Math.Ceiling(items.Count / (double)perPage);
            if (totalPages == 0) totalPages = 1;
            if (page >= totalPages) page = totalPages - 1;

            var pageItems = items.Skip(page * perPage).Take(perPage).ToList();

            await SafeEditMessageTextAsync(
                botClient,
                chatId,
                msgId,
                $"🔬 Галузь: {branch}",
                StateKeyboardBuilder.ResearchBranchListKeyboard(pageItems, page, totalPages),
                cancellationToken
            );
            LastResearchBranchByUser[userId] = branch;
        }

        private async Task ShowResearchInfo(ITelegramBotClient botClient, long chatId, long userId, int msgId, int researchId, CancellationToken cancellationToken)
        {
            var selectedCountryId = GetSelectedCountryIdForUser(userId);
            if (!selectedCountryId.HasValue)
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "Не знайдено країни. Оберіть країну або призначте CountryId персонажу.",
                    StateKeyboardBuilder.PublicOverviewKeyboard(),
                    cancellationToken
                );
                return;
            }

            var def = ResearchDefinitions.GetById(researchId);
            if (def == null)
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "Дослідження не знайдено.",
                    StateKeyboardBuilder.ResearchMenuKeyboard(userId == masterID),
                    cancellationToken
                );
                return;
            }

            var service = new ResearchService();
            bool canStart = service.CanStartResearch(selectedCountryId.Value, researchId, out string reason);

            string effectsText;
            if (def.Effects.Count == 0)
            {
                effectsText = "Ефекти: —";
            }
            else
            {
                var effects = def.Effects.Select(e =>
                {
                    var parts = new List<string> { e.Type.ToString() };
                    if (!string.IsNullOrWhiteSpace(e.Value))
                        parts.Add(e.Value);
                    if (e.Amount != 0)
                        parts.Add(e.Amount.ToString("0.##"));
                    return string.Join(" ", parts);
                });
                effectsText = $"Ефекти: {string.Join(", ", effects)}";
            }

            string reqText;
            if (def.Requirements.Count == 0)
            {
                reqText = "Немає вимог";
            }
            else
            {
                var reqNames = def.Requirements
                    .Select(id => ResearchDefinitions.GetById(id)?.Name ?? $"#{id}")
                    .ToList();
                reqText = $"Потрібні: {string.Join(", ", reqNames)}";
            }

            string text =
                $"📘 {def.Name}\n" +
                $"Напрям: {def.Branch}\n" +
                $"{def.Description}\n\n" +
                $"Наука: {def.ScienceCost}\n" +
                $"Вартість: {def.MoneyCost:N0} золота\n" +
                $"Тривалість: {def.DurationDays} дн.\n" +
                $"{reqText}\n" +
                $"{effectsText}\n" +
                (canStart ? "✅ Доступне для старту" : $"🔒 {reason}");

            await SafeEditMessageTextAsync(
                botClient,
                chatId,
                msgId,
                text,
                StateKeyboardBuilder.ResearchInfoKeyboard(researchId, canStart),
                cancellationToken
            );
        }

        private async Task StartResearch(ITelegramBotClient botClient, long chatId, long userId, int msgId, int researchId, CancellationToken cancellationToken)
        {
            var selectedCountryId = GetSelectedCountryIdForUser(userId);
            if (!selectedCountryId.HasValue)
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "Не знайдено країни. Оберіть країну або призначте CountryId персонажу.",
                    StateKeyboardBuilder.PublicOverviewKeyboard(),
                    cancellationToken
                );
                return;
            }

            var service = new ResearchService();
            string result = service.StartResearch(selectedCountryId.Value, researchId);

            await SafeEditMessageTextAsync(
                botClient,
                chatId,
                msgId,
                result,
                StateKeyboardBuilder.ResearchMenuKeyboard(userId == masterID),
                cancellationToken
            );
        }

        private async Task ShowActiveResearch(ITelegramBotClient botClient, long chatId, long userId, int msgId, CancellationToken cancellationToken)
        {
            var selectedCountryId = GetSelectedCountryIdForUser(userId);
            if (!selectedCountryId.HasValue)
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "Не знайдено країни. Оберіть країну або призначте CountryId персонажу.",
                    StateKeyboardBuilder.PublicOverviewKeyboard(),
                    cancellationToken
                );
                return;
            }

            var service = new ResearchService();
            var active = ResearchProgressData.GetActiveResearch(selectedCountryId.Value);
            var def = active == null ? null : ResearchDefinitions.GetById(active.ResearchId);
            string details = def == null ? string.Empty : $"\n\nОпис: {def.Description}\nНапрям: {def.Branch}";
            string text = service.GetActiveResearchStatus(selectedCountryId.Value) + details;
            bool canManualComplete = active != null && !active.IsCompleted;

            await SafeEditMessageTextAsync(
                botClient,
                chatId,
                msgId,
                text,
                StateKeyboardBuilder.ResearchActiveKeyboard(canManualComplete),
                cancellationToken
            );
        }

        private async Task CompleteActiveResearchManually(ITelegramBotClient botClient, long chatId, long userId, int msgId, CancellationToken cancellationToken)
        {
            var selectedCountryId = GetSelectedCountryIdForUser(userId);
            if (!selectedCountryId.HasValue)
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "Не знайдено країни. Оберіть країну або призначте CountryId персонажу.",
                    StateKeyboardBuilder.PublicOverviewKeyboard(),
                    cancellationToken
                );
                return;
            }

            var service = new ResearchService();
            string result = service.CompleteActiveResearchManually(selectedCountryId.Value);

            await SafeEditMessageTextAsync(
                botClient,
                chatId,
                msgId,
                result,
                StateKeyboardBuilder.ResearchMenuKeyboard(userId == masterID),
                cancellationToken
            );
        }

        private async Task StartResearchCreation(ITelegramBotClient botClient, long userId, CancellationToken cancellationToken)
        {
            if (userId != masterID)
            {
                await botClient.SendTextMessageAsync(userId, "Нема доступу.", cancellationToken: cancellationToken);
                return;
            }

            var ctx = new ResearchCreationContext
            {
                Step = 0,
                Def = new ResearchDefinition(),
                Requirements = new List<int>(),
                Effects = new List<ResearchEffect>()
            };
            ResearchInputContext[userId] = ctx;
            ResearchInputState[userId] = ResearchInputMode.Create;

            await SendHelperMessageAsync(botClient, userId, "Назва дослідження:", cancellationToken);
        }

        private async Task HandleResearchInput(ITelegramBotClient botClient, long userId, string input, CancellationToken cancellationToken)
        {
            if (!ResearchInputState.TryGetValue(userId, out var mode) || mode != ResearchInputMode.Create)
                return;

            if (!ResearchInputContext.TryGetValue(userId, out var ctx))
            {
                ctx = new ResearchCreationContext();
                ResearchInputContext[userId] = ctx;
            }

            if (ctx.AwaitingEffectType)
            {
                if (!TryParseEffectType(input, out var effectType))
                {
                    await SendHelperMessageAsync(botClient, userId, $"❌ Невірний тип. Доступно: {GetEffectTypeHelp()}", cancellationToken);
                    return;
                }
                ctx.PendingEffect = new ResearchEffect { Type = effectType };
                ctx.AwaitingEffectType = false;
                ctx.AwaitingEffectValue = true;
                await SendHelperMessageAsync(botClient, userId, "Значення ефекту (рядок або '-' якщо не потрібно):", cancellationToken);
                return;
            }

            if (ctx.AwaitingEffectValue)
            {
                ctx.PendingEffect.Value = input == "-" ? string.Empty : input;
                ctx.AwaitingEffectValue = false;
                ctx.AwaitingEffectAmount = true;
                await SendHelperMessageAsync(botClient, userId, "Кількість/сила ефекту (float, 0 якщо не треба):", cancellationToken);
                return;
            }

            if (ctx.AwaitingEffectAmount)
            {
                if (!float.TryParse(input.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out var amount))
                {
                    await SendHelperMessageAsync(botClient, userId, "❌ Очікується число.", cancellationToken);
                    return;
                }
                ctx.PendingEffect.Amount = amount;
                ctx.Effects.Add(ctx.PendingEffect);
                ctx.PendingEffect = new ResearchEffect();
                ctx.AwaitingEffectAmount = false;
                await SendHelperMessageAsync(botClient, userId, "Додати ще ефект? (так/ні)", cancellationToken);
                return;
            }

            if (ctx.Step == 0)
            {
                if (string.IsNullOrWhiteSpace(input))
                {
                    await SendHelperMessageAsync(botClient, userId, "❌ Назва не може бути порожньою.", cancellationToken);
                    return;
                }
                ctx.Def.Name = input.Trim();
                ctx.Step++;
                var branches = BuildResearchBranchList();
                await SendHelperMessageAsync(
                    botClient,
                    userId,
                    "Галузь (оберіть зі списку або введіть вручну):",
                    StateKeyboardBuilder.ResearchBranchSelectKeyboard(branches),
                    cancellationToken
                );
                return;
            }
            if (ctx.Step == 1)
            {
                ctx.Def.Branch = string.IsNullOrWhiteSpace(input) ? "Загальне" : input.Trim();
                ResearchBranchesData.EnsureBranch(ctx.Def.Branch);
                ctx.Step++;
                await SendHelperMessageAsync(botClient, userId, "Опис (можна '-' або порожньо):", cancellationToken);
                return;
            }
            if (ctx.Step == 2)
            {
                ctx.Def.Description = input == "-" ? string.Empty : input.Trim();
                ctx.Step++;
                await SendHelperMessageAsync(botClient, userId, "Вартість науки (int):", cancellationToken);
                return;
            }
            if (ctx.Step == 3)
            {
                if (!int.TryParse(input, NumberStyles.Integer, CultureInfo.InvariantCulture, out var sci) || sci < 0)
                {
                    await SendHelperMessageAsync(botClient, userId, "❌ Очікується int >= 0.", cancellationToken);
                    return;
                }
                ctx.Def.ScienceCost = sci;
                ctx.Step++;
                await SendHelperMessageAsync(botClient, userId, "Вартість золота (long):", cancellationToken);
                return;
            }
            if (ctx.Step == 4)
            {
                if (!long.TryParse(input, NumberStyles.Integer, CultureInfo.InvariantCulture, out var money) || money < 0)
                {
                    await SendHelperMessageAsync(botClient, userId, "❌ Очікується long >= 0.", cancellationToken);
                    return;
                }
                ctx.Def.MoneyCost = money;
                ctx.Step++;
                await SendHelperMessageAsync(botClient, userId, "Тривалість (дні, int):", cancellationToken);
                return;
            }
            if (ctx.Step == 5)
            {
                if (!int.TryParse(input, NumberStyles.Integer, CultureInfo.InvariantCulture, out var days) || days < 1)
                {
                    await SendHelperMessageAsync(botClient, userId, "❌ Очікується int >= 1.", cancellationToken);
                    return;
                }
                ctx.Def.DurationDays = days;
                ctx.Step++;
                await SendHelperMessageAsync(botClient, userId, "Порядок у гілці (int, менше = раніше):", cancellationToken);
                return;
            }
            if (ctx.Step == 6)
            {
                if (!int.TryParse(input, NumberStyles.Integer, CultureInfo.InvariantCulture, out var order))
                {
                    await SendHelperMessageAsync(botClient, userId, "❌ Очікується int.", cancellationToken);
                    return;
                }
                ctx.Def.SortOrder = order;
                ctx.Step++;
                await ShowResearchRequirementsBranches(botClient, userId, cancellationToken);
                return;
            }
            if (ctx.Step == 7)
            {
                if (!string.IsNullOrWhiteSpace(input))
                    ctx.Requirements = ParseIdList(input);
                ctx.Step++;
                await SendHelperMessageAsync(
                    botClient,
                    userId,
                    $"Додати ефект? (так/ні). Типи: {GetEffectTypeHelp()}",
                    StateKeyboardBuilder.ResearchYesNoKeyboard("state_research_create_yes", "state_research_create_no"),
                    cancellationToken
                );
                return;
            }

             
            if (ctx.Step == 8)
            {
                if (IsYes(input))
                {
                    ctx.AwaitingEffectType = true;
                    await SendHelperMessageAsync(
                        botClient,
                        userId,
                        $"Вкажіть тип ефекту: {GetEffectTypeHelp()}",
                        StateKeyboardBuilder.ResearchEffectTypeKeyboard(),
                        cancellationToken
                    );
                    return;
                }

                if (IsNo(input))
                {
                    await SaveResearchDefinition(botClient, userId, ctx, cancellationToken);
                    return;
                }

                await SendHelperMessageAsync(botClient, userId, "❌ Введіть 'так' або 'ні'.", cancellationToken);
                return;
            }

             
            if (IsYes(input))
            {
                ctx.AwaitingEffectType = true;
                await SendHelperMessageAsync(
                    botClient,
                    userId,
                    $"Вкажіть тип ефекту: {GetEffectTypeHelp()}",
                    StateKeyboardBuilder.ResearchEffectTypeKeyboard(),
                    cancellationToken
                );
                return;
            }
            if (IsNo(input))
            {
                await SaveResearchDefinition(botClient, userId, ctx, cancellationToken);
                return;
            }

            await SendHelperMessageAsync(botClient, userId, "❌ Введіть 'так' або 'ні'.", cancellationToken);
        }

        private static List<int> ParseIdList(string input)
        {
            var list = new List<int>();
            var parts = (input ?? string.Empty)
                .Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var p in parts)
            {
                if (p == "0")
                    continue;
                if (int.TryParse(p, NumberStyles.Integer, CultureInfo.InvariantCulture, out var id) && id > 0)
                    list.Add(id);
            }
            return list;
        }

        private static bool TryParseEffectType(string input, out ResearchEffectType effectType)
        {
            effectType = ResearchEffectType.Bonus;
            if (int.TryParse(input, out var i))
            {
                if (Enum.IsDefined(typeof(ResearchEffectType), i))
                {
                    effectType = (ResearchEffectType)i;
                    return true;
                }
            }
            return Enum.TryParse(input, true, out effectType);
        }

        private static string GetEffectTypeHelp()
        {
            return string.Join(", ", Enum.GetNames(typeof(ResearchEffectType)));
        }

        private static bool IsYes(string input)
        {
            var s = (input ?? string.Empty).Trim().ToLowerInvariant();
            return s == "так" || s == "yes" || s == "y";
        }

        private static bool IsNo(string input)
        {
            var s = (input ?? string.Empty).Trim().ToLowerInvariant();
            return s == "ні" || s == "no" || s == "n";
        }

        private static string NormalizeResearchBranchName(string branch)
        {
            if (string.IsNullOrWhiteSpace(branch))
                return "Загальне";

            var trimmed = branch.Trim();
            return string.Equals(trimmed, "Воєнна", StringComparison.OrdinalIgnoreCase)
                ? "Військова"
                : trimmed;
        }

        private static List<string> BuildResearchBranchList()
        {
            var ordered = ResearchBranchesData.GetAll()
                .OrderBy(b => b.SortOrder)
                .Select(b => NormalizeResearchBranchName(b.BranchName))
                .Where(b => !string.IsNullOrWhiteSpace(b))
                .ToList();

            var result = new List<string>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var b in ordered)
            {
                if (seen.Add(b))
                    result.Add(b);
            }

            var fromDefs = ResearchDefinitions.GetAll()
                .Select(d => NormalizeResearchBranchName(d.Branch))
                .Where(b => !string.IsNullOrWhiteSpace(b))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(b => b, StringComparer.OrdinalIgnoreCase)
                .ToList();

            foreach (var b in fromDefs)
            {
                if (seen.Add(b))
                    result.Add(b);
            }

            if (result.Count == 0)
            {
                result = new List<string>
                {
                    "Військова",
                    "Адміністрація",
                    "Економіка",
                    "Наука",
                    "Право",
                    "Дипломатія",
                    "Магія",
                    "Загальне"
                };
            }

            return result;
        }

        private async Task SaveResearchDefinition(ITelegramBotClient botClient, long userId, ResearchCreationContext ctx, CancellationToken cancellationToken)
        {
            ctx.Def.Requirements = ctx.Requirements ?? new List<int>();
            ctx.Def.Effects = ctx.Effects ?? new List<ResearchEffect>();
            ctx.Def.IsActive = true;

            int id = ResearchDefinitionsData.Add(ctx.Def);
            if (id > 0)
            {
                ResearchRequirementsData.ReplaceRequirements(id, ctx.Def.Requirements);
                ResearchEffectsData.ReplaceEffects(id, ctx.Def.Effects);
            }

            await CleanupHelperMessagesAsync(botClient, userId, cancellationToken);
            ClearResearchInput(userId);
            await botClient.SendTextMessageAsync(userId, id > 0 ? $"✅ Дослідження створено (Id={id})." : "❌ Не вдалося створити дослідження.", cancellationToken: cancellationToken);
        }

        private static void ClearResearchInput(long userId)
        {
            ResearchInputState[userId] = ResearchInputMode.None;
            ResearchInputContext.Remove(userId);
        }

        private async Task ShowResearchRequirementsBranches(ITelegramBotClient botClient, long userId, CancellationToken cancellationToken)
        {
            var branches = BuildResearchBranchList();

            await SendHelperMessageAsync(
                botClient,
                userId,
                "Вимоги: оберіть галузь для вибору досліджень (або введіть ID через кому):",
                StateKeyboardBuilder.ResearchRequirementsBranchKeyboard(branches),
                cancellationToken
            );
        }

        private async Task ShowResearchRequirementsList(ITelegramBotClient botClient, long userId, string branchKey, int page, CancellationToken cancellationToken)
        {
            if (!ResearchInputContext.TryGetValue(userId, out var ctx))
                return;

            string branch = DecodeKey(branchKey);
            var defs = ResearchDefinitions.GetAll()
                .Where(d => d.IsActive && string.Equals(d.Branch, branch, StringComparison.OrdinalIgnoreCase))
                .OrderBy(d => d.SortOrder)
                .ThenBy(d => d.Id)
                .ToList();

            int perPage = 10;
            if (page < 0) page = 0;
            int totalPages = (int)Math.Ceiling(defs.Count / (double)perPage);
            if (totalPages == 0) totalPages = 1;
            if (page >= totalPages) page = totalPages - 1;

            var pageItems = defs.Skip(page * perPage).Take(perPage).ToList();
            var items = new List<(string text, string data)>();
            var selected = new HashSet<int>(ctx.Requirements);
            foreach (var d in pageItems)
            {
                string mark = selected.Contains(d.Id) ? "✅" : "➕";
                items.Add(($"{mark} {d.Name}", $"state_research_req_pick_{d.Id}_{branchKey}_{page}"));
            }

            await SendHelperMessageAsync(
                botClient,
                userId,
                $"Вимоги: {branch}",
                StateKeyboardBuilder.ResearchRequirementsListKeyboard(items, page, totalPages, branchKey),
                cancellationToken
            );
        }

        private void ToggleResearchRequirement(long userId, int researchId)
        {
            if (!ResearchInputContext.TryGetValue(userId, out var ctx))
                return;
            if (ctx.Requirements.Contains(researchId))
                ctx.Requirements.Remove(researchId);
            else
                ctx.Requirements.Add(researchId);
        }

        private async Task FinishResearchRequirements(ITelegramBotClient botClient, long userId, CancellationToken cancellationToken)
        {
            if (!ResearchInputContext.TryGetValue(userId, out var ctx))
                return;
            ctx.Step = 8;
            await SendHelperMessageAsync(
                botClient,
                userId,
                $"Додати ефект? (так/ні). Типи: {GetEffectTypeHelp()}",
                StateKeyboardBuilder.ResearchYesNoKeyboard("state_research_create_yes", "state_research_create_no"),
                cancellationToken
            );
        }


    }
}
