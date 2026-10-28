using DNDBot.DataBase;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;

namespace DNDBot.Goverment
{
    public partial class StateParliamentHandle
    {
        private async Task ShowActiveLawCategories(ITelegramBotClient botClient, long userId, long chatId, int msgId, CancellationToken cancellationToken)
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

            var laws = LawsData.GetByCountry(countryId);
            int accepted = laws.Count(l => string.Equals(l.Status, LawStatusAccepted, StringComparison.OrdinalIgnoreCase));
            int rejected = laws.Count(l => string.Equals(l.Status, LawStatusRejected, StringComparison.OrdinalIgnoreCase));
            int revoked = laws.Count(l => string.Equals(l.Status, LawStatusRevoked, StringComparison.OrdinalIgnoreCase));
            int review1 = laws.Count(l => string.Equals(l.Status, LawStatusReviewFirst, StringComparison.OrdinalIgnoreCase));
            int review2 = laws.Count(l => string.Equals(l.Status, LawStatusReviewSecond, StringComparison.OrdinalIgnoreCase));
            int proposed = laws.Count(l => string.Equals(l.Status, LawStatusProposed, StringComparison.OrdinalIgnoreCase));

            string text =
                $"📜 Закони\n\n" +
                $"Прийняті: {accepted}\n" +
                $"Відхилені: {rejected}\n" +
                $"Відмінені: {revoked}\n" +
                $"На 1 читанні: {review1}\n" +
                $"На 2 читанні: {review2}\n" +
                $"Готові до голосування: {proposed}";
            await SafeEditMessageTextAsync(
                botClient,
                chatId,
                msgId,
                text,
                StateKeyboardBuilder.ActiveLawCategoryKeyboard(),
                cancellationToken
            );
        }

        private async Task ShowActiveLawList(ITelegramBotClient botClient, long userId, long chatId, int msgId, string status, int page, CancellationToken cancellationToken)
        {
            if (!TryGetCountryForDeputy(userId, out int countryId, out string error))
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    error,
                    StateKeyboardBuilder.ActiveLawCategoryKeyboard(),
                    cancellationToken
                );
                return;
            }

            if (string.Equals(status, LawStatusAccepted, StringComparison.OrdinalIgnoreCase))
            {
                await ShowAcceptedLawBranches(botClient, userId, chatId, msgId, page, cancellationToken);
                return;
            }

            var laws = LawsData.GetByCountryAndStatus(countryId, status);

            if (laws.Count == 0)
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    status == LawStatusAccepted
                        ? "Немає прийнятих законів."
                        : status == LawStatusRejected
                            ? "Немає відхилених законів."
                            : "Немає відмінених законів.",
                    StateKeyboardBuilder.ActiveLawCategoryKeyboard(),
                    cancellationToken
                );
                return;
            }

            string header = status == LawStatusRejected
                    ? "❌ Відхилені закони"
                    : "🗑 Відмінені закони";
            await SafeEditMessageTextAsync(
                botClient,
                chatId,
                msgId,
                header,
                StateKeyboardBuilder.ActiveLawSelectKeyboard(laws, status, page),
                cancellationToken
            );
        }

        private async Task ShowActiveLawInfo(ITelegramBotClient botClient, long userId, long chatId, int msgId, int lawId, CancellationToken cancellationToken)
        {
            if (!TryGetCountryForDeputy(userId, out int countryId, out string error))
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    error,
                    StateKeyboardBuilder.ActiveLawCategoryKeyboard(),
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
                    StateKeyboardBuilder.ActiveLawCategoryKeyboard(),
                    cancellationToken
                );
                return;
            }

            var countryLaws = LawsData.GetByCountry(countryId);
            var map = countryLaws.ToDictionary(l => l.LawId, l => l);
            int rootLawId = ResolveRootLawId(law, map);
            string rootTitle = map.TryGetValue(rootLawId, out var rootLaw)
                ? BuildLawTitle(rootLaw)
                : $"#{rootLawId}";
            string parentTitle = law.ParentLawId.HasValue && map.TryGetValue(law.ParentLawId.Value, out var parentLaw)
                ? BuildLawTitle(parentLaw)
                : "—";
            bool canRevoke = string.Equals(law.Status, LawStatusAccepted, StringComparison.OrdinalIgnoreCase);

            string text =
                $"📜 Закон #{law.LawId}\n" +
                $"Назва: {law.Title}\n" +
                $"Опис: {BuildLawDescriptionText(law)}\n" +
                $"Ефект: {BuildLawEffectText(law)}\n" +
                $"Статус: {GetLawStatusDisplayName(law.Status)}\n" +
                $"Батьківський закон: {parentTitle}\n" +
                $"Гілка: {rootTitle}";

            await SafeEditMessageTextAsync(
                botClient,
                chatId,
                msgId,
                text,
                StateKeyboardBuilder.ActiveLawInfoKeyboard(law.LawId, canRevoke, rootLawId),
                cancellationToken
            );
        }

        private async Task ShowAcceptedLawBranches(ITelegramBotClient botClient, long userId, long chatId, int msgId, int page, CancellationToken cancellationToken)
        {
            if (!TryGetCountryForDeputy(userId, out int countryId, out string error))
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    error,
                    StateKeyboardBuilder.ActiveLawCategoryKeyboard(),
                    cancellationToken
                );
                return;
            }

            var countryLaws = LawsData.GetByCountry(countryId);
            var accepted = countryLaws
                .Where(l => string.Equals(l.Status, LawStatusAccepted, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (accepted.Count == 0)
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "Немає прийнятих законів.",
                    StateKeyboardBuilder.ActiveLawCategoryKeyboard(),
                    cancellationToken
                );
                return;
            }

            var map = countryLaws.ToDictionary(l => l.LawId, l => l);
            var grouped = accepted
                .GroupBy(l => ResolveRootLawId(l, map))
                .OrderBy(g => BuildLawTitle(map.TryGetValue(g.Key, out var root) ? root : g.First()), StringComparer.OrdinalIgnoreCase)
                .ToList();

            const int perPage = 12;
            int safePage = Math.Max(0, page);
            int totalPages = (int)Math.Ceiling(grouped.Count / (double)perPage);
            if (totalPages <= 0)
                totalPages = 1;
            if (safePage >= totalPages)
                safePage = totalPages - 1;

            var pageItems = grouped.Skip(safePage * perPage).Take(perPage).ToList();
            var keyboardItems = new List<(string text, string data)>();

            foreach (var branch in pageItems)
            {
                int rootId = branch.Key;
                string rootTitle = map.TryGetValue(rootId, out var rootLaw) ? BuildLawTitle(rootLaw) : $"#{rootId}";
                int acceptedCount = branch.Count();
                int totalBranchCount = countryLaws.Count(l => ResolveRootLawId(l, map) == rootId);
                keyboardItems.Add(($"🌳 {rootTitle} • {acceptedCount}/{totalBranchCount}", $"stateparliament_lawbranch_{rootId}"));
            }

            await SafeEditMessageTextAsync(
                botClient,
                chatId,
                msgId,
                "✅ Прийняті закони (згруповано по гілках)",
                StateKeyboardBuilder.ActiveLawBranchGroupsKeyboard(keyboardItems, safePage, totalPages),
                cancellationToken
            );
        }

        private async Task ShowLawBranch(ITelegramBotClient botClient, long userId, long chatId, int msgId, int rootLawId, int page, CancellationToken cancellationToken)
        {
            if (!TryGetCountryForDeputy(userId, out int countryId, out string error))
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    error,
                    StateKeyboardBuilder.ActiveLawCategoryKeyboard(),
                    cancellationToken
                );
                return;
            }

            var countryLaws = LawsData.GetByCountry(countryId);
            if (countryLaws.Count == 0)
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "Закони не знайдено.",
                    StateKeyboardBuilder.ActiveLawCategoryKeyboard(),
                    cancellationToken
                );
                return;
            }

            var map = countryLaws.ToDictionary(l => l.LawId, l => l);
            if (!map.TryGetValue(rootLawId, out var requestedLaw))
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "Гілку закону не знайдено.",
                    StateKeyboardBuilder.ActiveLawCategoryKeyboard(),
                    cancellationToken
                );
                return;
            }

            int actualRootId = ResolveRootLawId(requestedLaw, map);
            var branchLaws = countryLaws
                .Where(l => ResolveRootLawId(l, map) == actualRootId)
                .OrderBy(l => ResolveLawDepth(l, map))
                .ThenBy(l => l.LawId)
                .ToList();

            if (branchLaws.Count == 0)
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "У цій гілці поки що немає законів.",
                    StateKeyboardBuilder.ActiveLawCategoryKeyboard(),
                    cancellationToken
                );
                return;
            }

            const int perPage = 18;
            int safePage = Math.Max(0, page);
            int totalPages = (int)Math.Ceiling(branchLaws.Count / (double)perPage);
            if (totalPages <= 0)
                totalPages = 1;
            if (safePage >= totalPages)
                safePage = totalPages - 1;

            var pageItems = branchLaws.Skip(safePage * perPage).Take(perPage).ToList();
            var keyboardItems = new List<(string text, string data)>();
            foreach (var law in pageItems)
            {
                int depth = ResolveLawDepth(law, map);
                string indent = new string('·', Math.Min(4, depth));
                string icon = GetLawStatusIcon(law.Status);
                string title = BuildLawTitle(law);
                keyboardItems.Add(($"{icon} {indent}{title}", $"stateparliament_activelawinfo_{law.LawId}"));
            }

            string rootTitle = map.TryGetValue(actualRootId, out var rootLaw) ? BuildLawTitle(rootLaw) : $"#{actualRootId}";
            int acceptedCount = branchLaws.Count(l => string.Equals(l.Status, LawStatusAccepted, StringComparison.OrdinalIgnoreCase));
            int revokedCount = branchLaws.Count(l => string.Equals(l.Status, LawStatusRevoked, StringComparison.OrdinalIgnoreCase));
            int rejectedCount = branchLaws.Count(l => string.Equals(l.Status, LawStatusRejected, StringComparison.OrdinalIgnoreCase));
            int proposedCount = branchLaws.Count(l => string.Equals(l.Status, LawStatusProposed, StringComparison.OrdinalIgnoreCase));
            int reviewCount = branchLaws.Count(l => IsLawOnReview(l.Status));

            string text =
                $"🌿 Гілка закону: {rootTitle}\n" +
                $"Усього: {branchLaws.Count} | ✅ {acceptedCount} | 🗑 {revokedCount} | ❌ {rejectedCount} | 🟡 {proposedCount} | 📖 {reviewCount}";

            await SafeEditMessageTextAsync(
                botClient,
                chatId,
                msgId,
                text,
                StateKeyboardBuilder.ActiveLawBranchKeyboard(keyboardItems, actualRootId, safePage, totalPages),
                cancellationToken
            );
        }

        private static string BuildLawTitle(LawRecord law)
        {
            if (law == null)
                return "—";
            return string.IsNullOrWhiteSpace(law.Title) ? $"#{law.LawId}" : law.Title;
        }

        private static string GetLawStatusIcon(string status)
        {
            if (string.Equals(status, LawStatusAccepted, StringComparison.OrdinalIgnoreCase))
                return "✅";
            if (string.Equals(status, LawStatusRevoked, StringComparison.OrdinalIgnoreCase))
                return "🗑";
            if (string.Equals(status, LawStatusRejected, StringComparison.OrdinalIgnoreCase))
                return "❌";
            if (string.Equals(status, LawStatusProposed, StringComparison.OrdinalIgnoreCase))
                return "🟡";
            if (string.Equals(status, LawStatusReviewSecond, StringComparison.OrdinalIgnoreCase))
                return "📘";
            if (string.Equals(status, LawStatusReviewFirst, StringComparison.OrdinalIgnoreCase))
                return "📗";
            return "📜";
        }

        private static int ResolveRootLawId(LawRecord law, Dictionary<int, LawRecord> lawMap)
        {
            if (law == null)
                return 0;

            int current = law.LawId;
            int guard = 0;
            var visited = new HashSet<int>();
            while (guard < 64 && lawMap.TryGetValue(current, out var node) && node.ParentLawId.HasValue)
            {
                int parent = node.ParentLawId.Value;
                if (!visited.Add(parent))
                    break;
                if (!lawMap.ContainsKey(parent))
                    break;
                current = parent;
                guard++;
            }

            return current;
        }

        private static int ResolveLawDepth(LawRecord law, Dictionary<int, LawRecord> lawMap)
        {
            if (law == null)
                return 0;

            int depth = 0;
            int current = law.LawId;
            var visited = new HashSet<int>();
            while (depth < 64 && lawMap.TryGetValue(current, out var node) && node.ParentLawId.HasValue)
            {
                int parent = node.ParentLawId.Value;
                if (!visited.Add(parent))
                    break;
                if (!lawMap.ContainsKey(parent))
                    break;
                depth++;
                current = parent;
            }

            return depth;
        }

        private static bool TryParseLawBranchData(string data, out int rootLawId, out int page)
        {
            rootLawId = 0;
            page = 0;
            if (string.IsNullOrWhiteSpace(data))
                return false;

            var parts = data.Split('_');
            if (parts.Length < 3)
                return false;

            if (!int.TryParse(parts[2], out rootLawId) || rootLawId <= 0)
                return false;

            if (parts.Length >= 5 && string.Equals(parts[3], "page", StringComparison.OrdinalIgnoreCase))
            {
                if (!int.TryParse(parts[4], out page))
                    page = 0;
            }

            if (page < 0)
                page = 0;
            return true;
        }

        private async Task ShowActiveLawRevokeConfirm(ITelegramBotClient botClient, long userId, long chatId, int msgId, int lawId, CancellationToken cancellationToken)
        {
            _logger.LogInformation("User {UserId} initiated revoke confirmation for law {LawId}", userId, lawId);
            try
            {
                await botClient.SendTextMessageAsync(
                    chatId,
                    $"Оберіть спосіб підрахунку для відміни закону #{lawId}:",
                    replyMarkup: StateKeyboardBuilder.ActiveLawRevokeConfirmKeyboard(lawId),
                    cancellationToken: cancellationToken
                );
            }
            catch (Telegram.Bot.Exceptions.ApiRequestException ex)
            {
                _logger.LogError(ex, "ShowActiveLawRevokeConfirm failed. UserId={UserId} ChatId={ChatId} MsgId={MsgId} LawId={LawId} ErrorCode={ErrorCode} Error={Error}",
                    userId, chatId, msgId, lawId, ex.ErrorCode, ex.Message);
                throw;
            }
        }

        private async Task HandleActiveLawRevokeStart(ITelegramBotClient botClient, long userId, long chatId, int msgId, int lawId, bool manual, CancellationToken cancellationToken)
        {
            if (!TryGetCountryForDeputy(userId, out int countryId, out string error))
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    error,
                    StateKeyboardBuilder.ActiveLawCategoryKeyboard(),
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
                    StateKeyboardBuilder.ActiveLawCategoryKeyboard(),
                    cancellationToken
                );
                return;
            }

            if (!string.Equals(law.Status, LawStatusAccepted, StringComparison.OrdinalIgnoreCase))
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "Цей закон не є прийнятим, його не можна відмінити.",
                    StateKeyboardBuilder.ActiveLawCategoryKeyboard(),
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
                    Action = VotingAction.RevokeLaw
                };
                PendingVotingByUser[masterID] = context;
                AwaitingVotingPercentByUser[masterID] = true;

                await botClient.SendTextMessageAsync(
                    masterID,
                    $"Вкажіть відсоток депутатів 'за' для відміни закону #{law.LawId} (0-100):",
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

            foreach (var d in deputies)
            {
                int chanceYes = Math.Clamp(
                    50 - (int)Math.Round((d.Loyalty - 50) * 0.6) - (int)Math.Round((d.Influence - 50) * 0.2) + (int)Math.Round((d.Corruption - 50) * 0.3),
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
            bool revoked = yes >= required;

            law.Status = revoked ? LawStatusRevoked : LawStatusAccepted;
            LawsData.UpdateLaw(law);

            string result =
                $"🗳 Голосування за відміну закону завершено\n" +
                $"Закон: {law.Title} (#{law.LawId})\n" +
                $"За відміну: {yes}\n" +
                $"Проти: {no}\n" +
                $"Потрібно: {required}\n" +
                $"Результат: {(revoked ? "ВІДМІНЕНО ✅" : "ЗАЛИШЕНО ЧИННИМ ❌")}";

            await SafeEditMessageTextAsync(
                botClient,
                chatId,
                msgId,
                result,
                StateKeyboardBuilder.ParliamentStateKeyboard(),
                cancellationToken
            );
        }

        private static int ParsePage(string data)
        {
            var parts = data.Split('_');
            if (parts.Length > 0 && int.TryParse(parts[^1], out int parsed))
                return parsed;
            return 0;
        }
    }
}

