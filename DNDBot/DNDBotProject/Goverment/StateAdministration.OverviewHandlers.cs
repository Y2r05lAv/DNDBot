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
        private async Task ShowMasterPanel(ITelegramBotClient botClient, long chatId, long userId, int msgId, CancellationToken cancellationToken)
        {
            var text = "🔧 Адміністративна панель (для розробника)";
            var keyboard = StateKeyboardBuilder.AddOrApplyCountryKeyboard();

            await SafeEditMessageTextAsync(botClient, chatId, msgId, text, keyboard, cancellationToken);
        }

        private async Task ShowOverviewMessage(ITelegramBotClient botClient, long chatId, long userId, int msgId, CancellationToken cancellationToken)
        {
            _logger.LogInformation("State overview info requested by Telegram user {UserId}", userId);

            var provider = new StateDataProvider();
            var stateData = provider.GetStateData(userId, GetSelectedCountryIdForUser(userId));

            var text = StateMessageBuilder.BuildOverviewStateMessage(stateData);
            var keyboard = StateKeyboardBuilder.PublicOverviewKeyboard(stateData?.CountryName);

            await SafeEditMessageTextAsync(botClient, chatId, msgId, text, keyboard, cancellationToken);
            LastStateViewByUser[userId] = "overview";
        }

        private async Task ShowCountrySelect(ITelegramBotClient botClient, long chatId, long userId, int msgId, CancellationToken cancellationToken)
        {
            var countries = CountriesData.GetAllCountries();
            if (countries.Count == 0)
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "Список країн порожній.",
                    StateKeyboardBuilder.CountrySelectKeyboard(countries),
                    cancellationToken
                );
                return;
            }

                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "🌍 Оберіть країну:",
                    StateKeyboardBuilder.CountrySelectKeyboard(countries),
                    cancellationToken
                );
        }

    }
}
