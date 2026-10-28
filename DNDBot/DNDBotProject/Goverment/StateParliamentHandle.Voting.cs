using DNDBot.DataBase;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace DNDBot.Goverment
{
    public partial class StateParliamentHandle
    {
        private enum VotingAction
        {
            AcceptLaw,
            RevokeLaw
        }

        private sealed class VotingPercentContext
        {
            public int LawId { get; set; }
            public int CountryId { get; set; }
            public int SessionId { get; set; }
            public List<DeputyRecord> PresentDeputies { get; set; } = new();
            public VotingAction Action { get; set; } = VotingAction.AcceptLaw;
            public int PercentYes { get; set; }
            public int Yes { get; set; }
            public int No { get; set; }
            public int Total { get; set; }
        }

        private async Task ShowVotingLawList(ITelegramBotClient botClient, long userId, long chatId, int msgId, int page, CancellationToken cancellationToken)
        {
            if (!TryGetCountryForDeputy(userId, out int countryId, out string error))
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    error,
                    StateKeyboardBuilder.ParliamentStateKeyboard(),
                    cancellationToken
                );
                return;
            }

            var laws = LawsData.GetAllLaws()
                .Where(l => l.CountryId == countryId && string.Equals(l.Status, LawStatusProposed, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (laws.Count == 0)
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "Немає законів на голосування.\nСпочатку переведіть закон із 2 читання до етапу голосування в меню «Розглядання».",
                    StateKeyboardBuilder.ParliamentStateKeyboard(),
                    cancellationToken
                );
                return;
            }

            await SafeEditMessageTextAsync(
                botClient,
                chatId,
                msgId,
                "🗳 Оберіть закон для голосування:",
                StateKeyboardBuilder.VotingLawSelectKeyboard(laws, page),
                cancellationToken
            );
        }

        private async Task ShowVotingLawInfo(ITelegramBotClient botClient, long userId, long chatId, int msgId, int lawId, CancellationToken cancellationToken)
        {
            if (!TryGetCountryForDeputy(userId, out int countryId, out string error))
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    error,
                    StateKeyboardBuilder.ParliamentStateKeyboard(),
                    cancellationToken
                );
                return;
            }

            var law = LawsData.GetLawById(lawId);
            if (law == null || law.CountryId != countryId)
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "Закон не знайдено.",
                    StateKeyboardBuilder.ParliamentStateKeyboard(),
                    cancellationToken
                );
                return;
            }

            if (!string.Equals(law.Status, LawStatusProposed, StringComparison.OrdinalIgnoreCase))
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "Цей закон вже не доступний для голосування.",
                    StateKeyboardBuilder.ParliamentStateKeyboard(),
                    cancellationToken
                );
                return;
            }

            string text =
                $"📜 Закон #{law.LawId}\n" +
                $"Назва: {law.Title}\n" +
                $"Опис: {BuildLawDescriptionText(law)}\n" +
                $"Ефект: {BuildLawEffectText(law)}\n" +
                $"Статус: {GetLawStatusDisplayName(law.Status)}";

            await SafeEditMessageTextAsync(
                botClient,
                chatId,
                msgId,
                text,
                StateKeyboardBuilder.VotingLawActionKeyboard(lawId),
                cancellationToken
            );
        }

        private async Task HandleVotingStart(ITelegramBotClient botClient, long userId, long chatId, int msgId, int lawId, bool manual, CancellationToken cancellationToken)
        {
            if (!TryGetCountryForDeputy(userId, out int countryId, out string error))
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    error,
                    StateKeyboardBuilder.ParliamentStateKeyboard(),
                    cancellationToken
                );
                return;
            }

            var law = LawsData.GetLawById(lawId);
            if (law == null || law.CountryId != countryId)
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "Закон не знайдено.",
                    StateKeyboardBuilder.ParliamentStateKeyboard(),
                    cancellationToken
                );
                return;
            }

            if (!string.Equals(law.Status, LawStatusProposed, StringComparison.OrdinalIgnoreCase))
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "Цей закон вже не доступний для голосування.",
                    StateKeyboardBuilder.ParliamentStateKeyboard(),
                    cancellationToken
                );
                return;
            }

            if (!ActiveParliamentSessionByCountry.TryGetValue(countryId, out int sessionId))
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "Немає активної сесії парламенту для цієї країни.",
                    StateKeyboardBuilder.ParliamentStateKeyboard(),
                    cancellationToken
                );
                return;
            }

            var session = ParliamentSessionsData.GetSessionById(sessionId);
            if (session == null || !string.Equals(session.Status, "Active", StringComparison.OrdinalIgnoreCase))
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "Сесія парламенту неактивна.",
                    StateKeyboardBuilder.ParliamentStateKeyboard(),
                    cancellationToken
                );
                return;
            }

            var attendance = SessionAttendanceData.GetAllAttendance()
                .Where(a => a.SessionId == sessionId && a.IsPresent)
                .Select(a => a.DeputyId)
                .ToHashSet();

            var deputies = DeputiesData.GetAllDeputies()
                .Where(d => d.CountryId == countryId && d.IsAlive && attendance.Contains(d.DeputyId))
                .ToList();

            if (deputies.Count == 0)
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "Немає присутніх депутатів для голосування.",
                    StateKeyboardBuilder.ParliamentStateKeyboard(),
                    cancellationToken
                );
                return;
            }

            if (PendingVotingByUser.TryGetValue(masterID, out _))
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "Вже є активне голосування. Завершіть його перед стартом нового.",
                    StateKeyboardBuilder.ParliamentStateKeyboard(),
                    cancellationToken
                );
                return;
            }

            if (manual)
            {
                var context = new VotingPercentContext
                {
                    LawId = law.LawId,
                    CountryId = countryId,
                    SessionId = sessionId,
                    PresentDeputies = deputies,
                    Action = VotingAction.AcceptLaw
                };
                PendingVotingByUser[masterID] = context;
                AwaitingVotingPercentByUser[masterID] = true;

                await botClient.SendTextMessageAsync(
                    masterID,
                    $"Вкажіть відсоток депутатів 'за' для закону #{law.LawId} (0-100):",
                    cancellationToken: cancellationToken
                );

                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "🗳 Очікуємо рішення майстра щодо відсотку голосів.",
                    StateKeyboardBuilder.ParliamentStateKeyboard(),
                    cancellationToken
                );
                return;
            }

             
            int yes = 0;
            int no = 0;

            int speechSuccess = SpeechInfluenceByCountry.TryGetValue(countryId, out int s) ? s : 50;
            int speechBonus = Math.Clamp((speechSuccess - 50) / 2, -25, 25);

            foreach (var d in deputies)
            {
                int chanceYes = Math.Clamp(
                    50 + (int)Math.Round((d.Loyalty - 50) * 0.6) + (int)Math.Round((d.Influence - 50) * 0.2) - (int)Math.Round((d.Corruption - 50) * 0.3) + speechBonus,
                    5,
                    95
                );
                int roll = Rng.Next(1, 101);
                bool voteYes = roll <= chanceYes;
                if (voteYes) yes++; else no++;

                var existing = VotesData.GetVote(law.LawId, d.DeputyId);
                var vote = new VoteRecord
                {
                    LawId = law.LawId,
                    DeputyId = d.DeputyId,
                    VoteType = voteYes ? "Yes" : "No"
                };
                if (existing == null)
                    VotesData.AddVote(vote);
                else
                    VotesData.UpdateVote(vote);
            }

            int total = deputies.Count;
            int required = (total / 2) + 1;
            bool passed = yes >= required;

            law.Status = passed ? LawStatusAccepted : LawStatusRejected;
            LawsData.UpdateLaw(law);
            if (passed)
                ApplyAcceptedLawEffects(law, userId);
            SpeechInfluenceByCountry.Remove(countryId);

            string result =
                $"🗳 Голосування завершено\n" +
                $"Закон: {law.Title} (#{law.LawId})\n" +
                $"За: {yes}\n" +
                $"Проти: {no}\n" +
                $"Потрібно: {required}\n" +
                $"Результат: {(passed ? "ПРИЙНЯТО ✅" : "ВІДХИЛЕНО ❌")}\n" +
                $"Бонус від виступу: {(speechBonus >= 0 ? "+" : "")}{speechBonus}%";

            await SafeEditMessageTextAsync(
                botClient,
                chatId,
                msgId,
                result,
                StateKeyboardBuilder.ParliamentStateKeyboard(),
                cancellationToken
            );
        }

        private async Task HandleVotingPercentInput(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
        {
            if (message?.From == null)
                return;

            long userId = message.From.Id;
            string input = (message.Text ?? string.Empty).Trim();

            if (!int.TryParse(input, out int percent) || percent < 0 || percent > 100)
            {
                await botClient.SendTextMessageAsync(
                    userId,
                    "Невірне значення. Вкажіть число від 0 до 100:",
                    cancellationToken: cancellationToken
                );
                return;
            }

            if (!PendingVotingByUser.TryGetValue(userId, out var ctx))
            {
                AwaitingVotingPercentByUser[userId] = false;
                await botClient.SendTextMessageAsync(userId, "Немає активного голосування.", cancellationToken: cancellationToken);
                return;
            }

            AwaitingVotingPercentByUser[userId] = false;
            ctx.PercentYes = percent;
            ctx.Total = ctx.PresentDeputies.Count;
            ctx.Yes = (int)Math.Round(ctx.Total * (percent / 100.0));
            ctx.No = ctx.Total - ctx.Yes;
            PendingVotingByUser[userId] = ctx;

            var actionText = ctx.Action == VotingAction.RevokeLaw ? "відміну закону" : "прийняття закону";
            var confirmKeyboard = ctx.Action == VotingAction.RevokeLaw
                ? StateKeyboardBuilder.VotingRevokeConfirmKeyboard(ctx.LawId)
                : StateKeyboardBuilder.VotingConfirmKeyboard(ctx.LawId);

            await botClient.SendTextMessageAsync(
                userId,
                $"Підтвердити результат на {actionText}? За: {ctx.Yes}/{ctx.Total} ({percent}%).",
                replyMarkup: confirmKeyboard,
                cancellationToken: cancellationToken
            );
        }

        private async Task HandleVotingConfirm(ITelegramBotClient botClient, CallbackQuery callbackQuery, int lawId, bool accept, VotingAction action, CancellationToken cancellationToken)
        {
            long userId = callbackQuery.From.Id;
            if (userId != masterID)
                return;

            if (!PendingVotingByUser.TryGetValue(userId, out var ctx) || ctx.LawId != lawId)
            {
                await botClient.SendTextMessageAsync(userId, "Немає активного голосування для цього закону.", cancellationToken: cancellationToken);
                return;
            }

            var law = LawsData.GetLawById(lawId);
            if (law == null)
            {
                await botClient.SendTextMessageAsync(userId, "Закон не знайдено.", cancellationToken: cancellationToken);
                return;
            }

            var deputies = ctx.PresentDeputies;
            var shuffled = deputies.OrderBy(_ => Rng.Next()).ToList();
            var yesSet = new HashSet<int>(shuffled.Take(ctx.Yes).Select(d => d.DeputyId));

            foreach (var d in deputies)
            {
                bool voteYes = yesSet.Contains(d.DeputyId);
                var existing = VotesData.GetVote(law.LawId, d.DeputyId);
                var vote = new VoteRecord
                {
                    LawId = law.LawId,
                    DeputyId = d.DeputyId,
                    VoteType = voteYes ? "Yes" : "No"
                };
                if (existing == null)
                    VotesData.AddVote(vote);
                else
                    VotesData.UpdateVote(vote);
            }

            if (action == VotingAction.RevokeLaw)
            {
                law.Status = accept ? LawStatusRevoked : LawStatusAccepted;
            }
            else
            {
                law.Status = accept ? LawStatusAccepted : LawStatusRejected;
            }
            LawsData.UpdateLaw(law);
            if (action == VotingAction.AcceptLaw && accept)
                ApplyAcceptedLawEffects(law, userId);
            PendingVotingByUser.Remove(userId);

            string result =
                $"🗳 Голосування завершено\n" +
                $"Закон: {law.Title} (#{law.LawId})\n" +
                $"За: {ctx.Yes}\n" +
                $"Проти: {ctx.No}\n" +
                $"Результат: {(action == VotingAction.RevokeLaw ? (accept ? "ВІДМІНЕНО ✅" : "ЗАЛИШЕНО ЧИННИМ ❌") : (accept ? "ПРИЙНЯТО ✅" : "ВІДХИЛЕНО ❌"))}";

            await botClient.SendTextMessageAsync(userId, result, cancellationToken: cancellationToken);
        }
    }
}
