using DNDBot.BotSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot.Types;
using Telegram.Bot;
using Microsoft.Extensions.Logging;
using static DNDBot.BotBodyRealisation;

namespace DNDBot.MoneySystem
{
    class WalletCallbackDataHandle : BotBodyRealisation
    {
        private readonly ILogger<WalletCallbackDataHandle> _logger;

        public WalletCallbackDataHandle(ILogger<WalletCallbackDataHandle> logger)
        {
            _logger = logger;
        }

        public async Task MasterCommnandsCallBackHandle(ITelegramBotClient botClient, Message message, long chatId, string callbackData, CancellationToken cancellationToken)
        {
            string currencyType = callbackData.Split('_')[1];
            string coinTypeName = GetCoinTypeName(currencyType);

            string actionText = callbackData.StartsWith("transfer_")
                ? $"Скільки {coinTypeName} ви хочете отримати?"
                : callbackData.StartsWith("payToAll_")
                    ? $"Скільки {coinTypeName} ви хочете додати до гравців?"
                    : $"Скільки {coinTypeName} ви хочете відняти від гравців?";

            UpdateUserStatePay(ref StateBotMSG, chatId, false);
            UserStatePayInitialization(currencyType, chatId);
            StateBotMSG[chatId] = 1;
            await botClient.SendTextMessageAsync(chatId, actionText, cancellationToken: cancellationToken);

             
        }

        public async Task UserPayCallBackHandle(ITelegramBotClient botClient, CancellationToken cancellationToken, Message message, string callbackData, long userId)
        {
            if (!userStateWallet.ContainsKey(userId))
                userStateWallet.Add(userId, "start");

            string action = callbackData == "add" ? "add" : "min";
            await botClient.SendTextMessageAsync(userId, $"Скільки монет ви хочете {(action == "add" ? "додати" : "відняти")}?", cancellationToken: cancellationToken);
        }

        public async Task UserPayChosenCallBackHandle(ITelegramBotClient botClient, CancellationToken cancellationToken, Message message, string callbackData, long chatId, long userId)
        {

            string coinType = callbackData.Split('_')[1];
            string coinTypeName = GetCoinTypeName(coinType);

            _logger.LogWarning($"Coin state {callbackData}");
            _logger.LogWarning($"Sellected user: {userStatePay[userId]}");
            _logger.LogWarning($"Sellected chat: {chatId}");

            long selectedUserId = userStatePay[userId];
            int selectedCharacterId = userStatePayCharacter.TryGetValue(userId, out int characterId) && characterId > 0
                ? characterId
                : UserData.GetActiveCharacterId(selectedUserId);
            string name = UserData.DataBaseValueReader<string>(selectedUserId, selectedCharacterId, "Name");

            string actionText = callbackData.StartsWith("fineUser_")
                ? $"На скільки {coinTypeName} ви хочете оштрафувати гравця {name} (C{selectedCharacterId})?"
                : $"Скільки {coinTypeName} ви хочете переслати гравцю {name} (C{selectedCharacterId})?";

            UpdateUserStatePay(ref StateBotMSG, userId, false);

            StateBotMSG[userId] = 1;

            UserStatePayInitialization(coinType, userId);


            await botClient.SendTextMessageAsync(userId, actionText, cancellationToken: cancellationToken);

            string action = callbackData.StartsWith("fineUser_") ? "fineUser" : "pay";

        }

        public void UserStatePayInitialization(string coinType, long chatId)
        {
            if (userStateValueName.ContainsKey(chatId))
                userStateValueName[chatId] = coinType;
            else
                userStateValueName.Add(chatId, coinType);

            if (userStateWallet.ContainsKey(chatId))
                userStateWallet[chatId] = "start";
            else
                userStateWallet.Add(chatId, "start");
        }

        public new void UpdateUserStatePay(ref Dictionary<long, long> userState, long userId, bool fromCallBack = false, long chatId = 0)
        {
            if (fromCallBack)
            {
                if (userState.ContainsKey(chatId))
                    userState[chatId] = userId;
                else
                    userState.Add(chatId, userId);
            }
            else
            {
                if (userState.ContainsKey(userId))
                    userState[userId] = 0;
                else
                    userState.Add(userId, 0);
            }
        }

        public async Task PayCallBackData(ITelegramBotClient botClient, CancellationToken cancellationToken, string callbackData, long chatId, long choosedUser, int choosedCharacterId)
        {
             

            string commandPrefix = callbackData.StartsWith("pay_") ? "payChosen_" : "fineUser_";
            int resolvedCharacterId = choosedCharacterId > 0
                ? choosedCharacterId
                : UserData.GetActiveCharacterId(choosedUser);
            string characterName = UserData.DataBaseValueReader<string>(choosedUser, resolvedCharacterId, "Name");
            string actionText = callbackData.StartsWith("pay_")
                ? $"Яку монету ви хочете заплатити гравцю {characterName} (C{resolvedCharacterId})?"
                : $"На яку монету ви хочете оштрафувати {characterName} (C{resolvedCharacterId})?";

             

            if (!userStatePay.TryAdd(chatId, choosedUser))
                userStatePay[chatId] = choosedUser;

            if (!userStatePayCharacter.TryAdd(chatId, resolvedCharacterId))
                userStatePayCharacter[chatId] = resolvedCharacterId;

            await InlineKeyboardsClass.InlineButtonsCreation(botClient, cancellationToken, chatId, commandPrefix, actionText);
        }


        public static string GetCoinTypeName(string coinType)
        {
            return coinType switch
            {
                "copper" => "мідних монет",
                "silver" => "срібних монет",
                "gold" => "золотих монет",
                "platinum" => "платинових монет",
                _ => throw new ArgumentException($"Invalid coin type: {coinType}")
            };
        }
    }
}
