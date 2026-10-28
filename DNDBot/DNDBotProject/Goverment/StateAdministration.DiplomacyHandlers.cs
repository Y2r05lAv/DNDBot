using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DNDBot.DataBase;
using Telegram.Bot;

namespace DNDBot.Goverment
{
    public partial class StateAdministration
    {
        private async Task ShowDiplomacyMenu(ITelegramBotClient botClient, long userId, long chatId, int msgId, int page, CancellationToken cancellationToken)
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

            var all = CountriesData.GetAllCountries()
                .Where(c => c.CountryId != selectedCountryId.Value)
                .OrderBy(c => c.Name ?? string.Empty)
                .ToList();

            int perPage = 12;
            if (page < 0) page = 0;
            int totalPages = (int)Math.Ceiling(all.Count / (double)perPage);
            if (totalPages == 0) totalPages = 1;
            if (page >= totalPages) page = totalPages - 1;
            var pageItems = all.Skip(page * perPage).Take(perPage).ToList();

            string text = "🤝 Дипломатія: оберіть країну";
            await SafeEditMessageTextAsync(
                botClient,
                chatId,
                msgId,
                text,
                StateKeyboardBuilder.DiplomacyCountriesKeyboard(pageItems, page, totalPages),
                cancellationToken
            );
        }

        private async Task ShowDiplomacyRelation(ITelegramBotClient botClient, long userId, long chatId, int msgId, int targetCountryId, CancellationToken cancellationToken)
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

            var target = CountriesData.GetCountryById(targetCountryId);
            if (target == null)
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "Країну не знайдено.",
                    StateKeyboardBuilder.MainStateKeyboard(userId),
                    cancellationToken
                );
                return;
            }

            var relation = DiplomacyManager.GetRelation(selectedCountryId.Value, targetCountryId);
            bool canEdit = userId == masterID || IsSelectedCountryOwner(userId, selectedCountryId.Value);

            var sb = new StringBuilder();
            sb.AppendLine($"🤝 Дипломатія: {target.Name}");
            sb.AppendLine($"Стан: {relation.State}");
            sb.AppendLine($"Союз: {(relation.IsAlliance ? "Так" : "Ні")}");
            sb.AppendLine($"Війна: {(relation.IsAtWar || relation.State == DiplomaticState.War ? "Так" : "Ні")}");
            sb.AppendLine($"Санкції: {(relation.HasSanctions ? "Так" : "Ні")}");
            sb.AppendLine($"Ембарго: {(relation.HasEmbargo ? "Так" : "Ні")}");
            sb.AppendLine($"Торгова угода: {(relation.HasTradeAgreement ? "Так" : "Ні")}");

            await SafeEditMessageTextAsync(
                botClient,
                chatId,
                msgId,
                sb.ToString(),
                StateKeyboardBuilder.DiplomacyActionsKeyboard(relation, targetCountryId, canEdit, userId == masterID),
                cancellationToken
            );
        }

        private async Task ApplyDiplomacyAction(ITelegramBotClient botClient, long userId, long chatId, int msgId, int targetCountryId, DiplomaticAction action, CancellationToken cancellationToken)
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

            if (userId != masterID && !IsSelectedCountryOwner(userId, selectedCountryId.Value))
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

            var (success, message) = DiplomacyManager.ApplyAction(selectedCountryId.Value, targetCountryId, action, userId, userId == masterID);
            if (success)
                await ShowDiplomacyRelation(botClient, userId, chatId, msgId, targetCountryId, cancellationToken);
            else
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    message,
                    StateKeyboardBuilder.MainStateKeyboard(userId),
                    cancellationToken
                );
        }

        private async Task ShowDiplomacyStateSelect(ITelegramBotClient botClient, long userId, long chatId, int msgId, int targetCountryId, CancellationToken cancellationToken)
        {
            if (userId != masterID)
                return;

            await SafeEditMessageTextAsync(
                botClient,
                chatId,
                msgId,
                "⚙ Встановити рівень відносин:",
                StateKeyboardBuilder.DiplomacyStateSelectKeyboard(targetCountryId),
                cancellationToken
            );
        }

        private async Task ApplyDiplomacyStateSet(ITelegramBotClient botClient, long userId, long chatId, int msgId, int targetCountryId, string stateToken, CancellationToken cancellationToken)
        {
            if (userId != masterID)
                return;

            if (!TryParseDiplomaticState(stateToken, out var state))
                return;

            var selectedCountryId = GetSelectedCountryIdForUser(userId);
            if (!selectedCountryId.HasValue)
                return;

            DiplomacyManager.SetState(selectedCountryId.Value, targetCountryId, state, userId);
            await ShowDiplomacyRelation(botClient, userId, chatId, msgId, targetCountryId, cancellationToken);
        }

        private static bool TryParseDiplomaticState(string token, out DiplomaticState state)
        {
            state = DiplomaticState.Neutral;
            return token switch
            {
                "alliance" => (state = DiplomaticState.Alliance) == DiplomaticState.Alliance,
                "friendly" => (state = DiplomaticState.Friendly) == DiplomaticState.Friendly,
                "neutral" => (state = DiplomaticState.Neutral) == DiplomaticState.Neutral,
                "tense" => (state = DiplomaticState.Tense) == DiplomaticState.Tense,
                "hostile" => (state = DiplomaticState.Hostile) == DiplomaticState.Hostile,
                "war" => (state = DiplomaticState.War) == DiplomaticState.War,
                _ => false
            };
        }

        private static bool TryParseDiplomacyAction(string token, out DiplomaticAction action)
        {
            action = DiplomaticAction.MakePeace;
            return token switch
            {
                "war" => (action = DiplomaticAction.DeclareWar) == DiplomaticAction.DeclareWar,
                "peace" => (action = DiplomaticAction.MakePeace) == DiplomaticAction.MakePeace,
                "alliance" => (action = DiplomaticAction.FormAlliance) == DiplomaticAction.FormAlliance,
                "break_alliance" => (action = DiplomaticAction.BreakAlliance) == DiplomaticAction.BreakAlliance,
                "sanctions" => (action = DiplomaticAction.ApplySanctions) == DiplomaticAction.ApplySanctions,
                "lift_sanctions" => (action = DiplomaticAction.LiftSanctions) == DiplomaticAction.LiftSanctions,
                "trade" => (action = DiplomaticAction.SignTradeAgreement) == DiplomaticAction.SignTradeAgreement,
                "break_trade" => (action = DiplomaticAction.BreakTradeAgreement) == DiplomaticAction.BreakTradeAgreement,
                "embargo" => (action = DiplomaticAction.ImposeEmbargo) == DiplomaticAction.ImposeEmbargo,
                "lift_embargo" => (action = DiplomaticAction.LiftEmbargo) == DiplomaticAction.LiftEmbargo,
                _ => false
            };
        }
    }
}
