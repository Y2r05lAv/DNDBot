using DNDBot.DataBase;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace DNDBot.Goverment
{
    public partial class StateParliamentHandle
    {
        private async Task StartSpeechEvaluation(ITelegramBotClient botClient, CallbackQuery callbackQuery, CancellationToken cancellationToken)
        {
            long userId = callbackQuery.From.Id;

            if (!TryGetCountryForDeputy(userId, out int countryId, out string error))
            {
                await botClient.SendTextMessageAsync(userId, error, cancellationToken: cancellationToken);
                return;
            }

            AwaitingSpeechResult[masterID] = true;
            PendingSpeechCountryByUser[masterID] = countryId;
            PendingSpeechRequesterByUser[masterID] = userId;

            string countryName = CountriesData.GetCountryNameById(countryId);
            string requester = userId == masterID ? "майстер" : $"гравець #{userId}";

            await botClient.SendTextMessageAsync(
                masterID,
                $"Оцініть успішність виступу (0-100) для країни \"{countryName}\". Ініціатор: {requester}.",
                cancellationToken: cancellationToken
            );

            if (callbackQuery.Message == null)
                return;

            await SafeEditMessageTextAsync(
                botClient,
                callbackQuery.Message.Chat.Id,
                callbackQuery.Message.MessageId,
                "🎤 Виступ: очікуємо оцінку майстра.",
                StateKeyboardBuilder.ParliamentStateKeyboard(),
                cancellationToken
            );
        }

        private async Task HandleSpeechResultInput(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
        {
            if (message?.From == null)
                return;

            long userId = message.From.Id;
            string input = (message.Text ?? string.Empty).Trim();

            if (!int.TryParse(input, out int success) || success < 0 || success > 100)
            {
                await botClient.SendTextMessageAsync(
                    userId,
                    "Невірне значення. Вкажіть число від 0 до 100:",
                    cancellationToken: cancellationToken
                );
                return;
            }

            AwaitingSpeechResult[userId] = false;

            if (!PendingSpeechCountryByUser.TryGetValue(userId, out int countryId))
            {
                await botClient.SendTextMessageAsync(userId, "Немає активного виступу.", cancellationToken: cancellationToken);
                return;
            }

            var deputies = DeputiesData.GetAllDeputies()
                .Where(d => d.CountryId == countryId && d.IsAlive)
                .ToList();

            if (deputies.Count == 0)
            {
                await botClient.SendTextMessageAsync(userId, "У країні немає депутатів.", cancellationToken: cancellationToken);
                return;
            }

            int persuaded = 0;
            foreach (var d in deputies)
            {
                int chance = Math.Clamp(success + (d.Loyalty - 50) / 2, 5, 95);
                int roll = Rng.Next(1, 101);
                if (roll <= chance)
                    persuaded++;
            }

            SpeechInfluenceByCountry[countryId] = success;

            string countryName = CountriesData.GetCountryNameById(countryId);
            int speechBonus = Math.Clamp((success - 50) / 2, -25, 25);
            string result =
                $"🎤 Виступ завершено.\n" +
                $"Країна: {countryName}\n" +
                $"Успішність: {success}%\n" +
                $"Переконано депутатів: {persuaded}/{deputies.Count}\n" +
                $"Вплив на голосування: {(speechBonus >= 0 ? "+" : "")}{speechBonus}% до шансу \"за\".";

            await botClient.SendTextMessageAsync(userId, result, cancellationToken: cancellationToken);

            if (PendingSpeechRequesterByUser.TryGetValue(userId, out long requesterId) && requesterId != userId)
            {
                await botClient.SendTextMessageAsync(requesterId, result, cancellationToken: cancellationToken);
            }

            PendingSpeechCountryByUser.Remove(userId);
            PendingSpeechRequesterByUser.Remove(userId);
        }
    }
}
