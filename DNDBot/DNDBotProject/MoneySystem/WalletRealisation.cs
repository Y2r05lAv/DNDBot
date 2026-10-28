﻿using DNDBot.BotSystem;
using DNDBot.MoneySystem;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;

namespace DNDBot
{
    class WalletRealisation : BotBodyRealisation
    {
        private long choosedUserToStraf;

        private readonly ILogger<WalletRealisation> _logger;
        private readonly WalletCallbackDataHandle _walletCallbackDataHandle;
        private readonly WalletSubCommandsHandle _walletSubCommandsHandle;
        private readonly WalletSwitchHandle _walletSwitchHandle;
        private readonly MoneyTransfer _moneyTransfer;

        public WalletRealisation(ILogger<WalletRealisation> logger,
            WalletCallbackDataHandle walletCallbackDataHandle, WalletSwitchHandle walletSwitchHandle, MoneyTransfer moneyTransfer,
            WalletSubCommandsHandle walletSubCommandsHandle)
        {
            _logger = logger;
            _walletCallbackDataHandle = walletCallbackDataHandle;
            _walletSwitchHandle = walletSwitchHandle;
            _moneyTransfer = moneyTransfer;
            _walletSubCommandsHandle = walletSubCommandsHandle;
        }

        public async Task WalletTaskHandle(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken, Message message)
        {
             
             

            if (message?.From == null)
                return;

            long userId = message.From.Id;
            if (!UserData.CheckifExist(message.From.Id))
            {
                 
                return;
            }

            if (message?.Text == "Додати гроші")
            {
                _logger.LogInformation("User {UserId} selected 'Add money' option", userId);
                await InlineKeyboardsClass.InlineButtonsCreation(botClient, cancellationToken, userId, "add_", "Яку монету ви хочете додати?");
            }

            if (message?.Text == "Відняти гроші")
            {
                _logger.LogInformation("User {UserId} selected 'Subtract money' option", userId);
                await InlineKeyboardsClass.InlineButtonsCreation(botClient, cancellationToken, userId, "min_", "Яку монету ви хочете відняти?");
            }

            if (message?.Text == "Заплатити")
            {
                _logger.LogInformation("User {UserId} selected 'Pay' option", userId);
                await PayMoney(botClient, cancellationToken, userId);
            }

            if (message?.Text == "Перевести грошову одиницю")
            {
                _logger.LogInformation("User {UserId} selected 'Transfer money' option", userId);
                await InlineKeyboardsClass.InlineButtonsCreation(botClient, cancellationToken, userId, "transfer_", "Виберіть валюту, котру хочете отримати з інших ваших монет:");
            }

            if (message?.Text == "Поощирити" && userId == masterID)
            {
                _logger.LogInformation("Master user {UserId} selected 'Reward all' option", userId);
                await InlineKeyboardsClass.InlineButtonsCreation(botClient, cancellationToken, userId, "payToAll_", "Виберіть монету, котру хочете додати до всіх гравців:");
            }

            if (message?.Text == "Оштрафувати" && userId == masterID)
            {
                _logger.LogInformation("Master user {UserId} selected 'Fine all' option", userId);
                await InlineKeyboardsClass.InlineButtonsCreation(botClient, cancellationToken, userId, "fine_", "Виберіть монету, котру хочете відняти від всіх гравців:");
            }

            if (!userStateWallet.ContainsKey(userId))
            {
                
                return;
            }

            switch (userStateWallet[userId])
            {
                case "start":
                    _logger.LogInformation("Starting wallet cycle for user {UserId}", userId);
                    await StartVoid(botClient, message, cancellationToken, userStateMethod[userId], userId);
                    break;
                case "changeValue":
                    _logger.LogInformation("Changing value for user {UserId}", userId);
                    await PasteVoid(botClient, message, cancellationToken, userStateMethod[userId], userId);
                    break;
            }
        }

        public async Task HandleCallbackWallet(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken, Message message, CallbackQuery callbackQuery)
        {
            try
            {
                if (callbackQuery?.Data == null || callbackQuery.Message?.Chat == null)
                    return;

                long chatId = callbackQuery.Message.Chat.Id;
                string callbackData = callbackQuery.Data;

                 

                var parts = callbackData.Split('_');
                if (parts.Length < 2)
                    return;
                string recipientId = parts[1];
                string recipienData = parts[0];
                int recipientCharacterId = 0;
                if (parts.Length >= 3)
                    int.TryParse(parts[2], out recipientCharacterId);

                if (!userStateValueName.ContainsKey(chatId))
                    userStateValueName.Add(chatId, recipientId);

                userStateValueName[chatId] = recipientId;

                if (!userStateMethod.TryAdd(chatId, recipienData))
                    userStateMethod[chatId] = recipienData;

                switch (recipienData)
                {
                    case "pay":
                        long.TryParse(recipientId, out long choosedPlayer);
                        recipientCharacterId = recipientCharacterId > 0
                            ? recipientCharacterId
                            : UserData.GetActiveCharacterId(choosedPlayer);
                        _logger.LogInformation("Selected user to pay: {ChoosedUser}", choosedPlayer);

                        if (!userStatePay.TryAdd(chatId, choosedPlayer))
                            userStatePay[chatId] = choosedPlayer;

                        userStatePay[chatId] = choosedPlayer;
                        userStatePayCharacter[chatId] = recipientCharacterId;

                        await _walletCallbackDataHandle.PayCallBackData(botClient, cancellationToken, callbackData, chatId, choosedPlayer, recipientCharacterId);
                        break;

                    case "uitgeven":
                        long.TryParse(recipientId, out long choosedPlayer1);
                        recipientCharacterId = recipientCharacterId > 0
                            ? recipientCharacterId
                            : UserData.GetActiveCharacterId(choosedPlayer1);
                        _logger.LogInformation("Selected user for uitgeven: {ChoosedUser}", choosedPlayer1);

                        if (!userStatePay.TryAdd(masterID, choosedPlayer1))
                            userStatePay[masterID] = choosedPlayer1;

                        userStatePay[masterID] = choosedPlayer1;
                        userStatePayCharacter[masterID] = recipientCharacterId;
                        choosedUserToStraf = choosedPlayer1;

                        await _walletCallbackDataHandle.PayCallBackData(botClient, cancellationToken, callbackData, chatId, choosedPlayer1, recipientCharacterId);
                        await StartVoid(botClient, null, cancellationToken, recipientId, chatId);
                        break;

                    case "payChosen":
                        await _walletCallbackDataHandle.UserPayChosenCallBackHandle(botClient, cancellationToken, message, callbackData,chatId, chatId);
                        await StartVoid(botClient, null, cancellationToken, recipientId, chatId);
                        break;

                    case "fineUser":
                        _logger.LogInformation("Selected user to fine: {ChoosedUser}", userStatePay[masterID]);


                        await _walletCallbackDataHandle.UserPayChosenCallBackHandle(botClient, cancellationToken, message, callbackData, chatId, masterID);
                        await StartVoid(botClient, null, cancellationToken, recipientId, chatId);
                        break;

                    case "add":
                        if (!userStateValueName.ContainsKey(chatId))
                            userStateValueName.Add(chatId, recipientId);

                        userStateValueName[chatId] = recipientId;
                        userStateMethod[chatId] = recipienData;

                        await _walletCallbackDataHandle.UserPayCallBackHandle(botClient, cancellationToken, message, recipienData, chatId);
                        await StartVoid(botClient, null, cancellationToken, recipientId, chatId);
                        break;

                    case "min":
                        userStateValueName[chatId] = recipientId;
                        await _walletCallbackDataHandle.UserPayCallBackHandle(botClient, cancellationToken, message, recipienData, chatId);
                        await StartVoid(botClient, null, cancellationToken, recipientId, chatId);
                        break;
                }

                if (callbackData.StartsWith("transfer_") || callbackData.StartsWith("payToAll_") || callbackData.StartsWith("fine_"))
                {
                    long.TryParse(recipientId, out long choosedPlayer2);
                    _logger.LogInformation("Selected operation: {Operation} for user {UserId}", callbackData.Split('_')[0], choosedPlayer2);

                    if (!userStatePay.TryAdd(masterID, choosedPlayer2))
                        userStatePay[masterID] = choosedPlayer2;

                    userStatePay[masterID] = choosedPlayer2;

                    if(callbackData.StartsWith("fine_"))
                        userStateValueName[chatId] = recipientId;

                    await _walletCallbackDataHandle.MasterCommnandsCallBackHandle(botClient, message, chatId, callbackData, cancellationToken);
                    await StartVoid(botClient, null, cancellationToken, recipientId, chatId);
                }

                await botClient.AnswerCallbackQueryAsync(callbackQuery.Id);
            }
            catch (ApiRequestException ex) when (ex.Message.Contains("query is too old"))
            {
                _logger.LogWarning("Callback query is too old for user {UserId}", callbackQuery.From.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling callback for user {UserId}", callbackQuery.From.Id);
            }
        }

        public async Task StartVoid(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken, string pasteState, long userId)
        {
            _logger.LogInformation("Starting wallet void for user {UserId}", userId);

            UpdateUserStatePay(ref StateBotMSG, userId, false);
            StateBotMSG[userId] = 1;

            if (!userStateWallet.ContainsKey(userId))
                userStateWallet.Add(userId, "changeValue");

            userStateWallet[userId] = "changeValue";
            await PasteVoid(botClient, message, cancellationToken, pasteState, userId);
        }

        public async Task PasteVoid(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken, string pasteState, long userId)
        {
            _logger.LogInformation("Starting wallet cycle for user {UserId}. Method: {Method}", 
                userId, userStateMethod[userId]);

            if (!string.IsNullOrEmpty(pasteState))
            {
                if (!userStateValueName.ContainsKey(userId))
                    userStateValueName.Add(userId, pasteState);
                _logger.LogInformation("Set paste state for user {UserId}: {PasteState}", userId, pasteState);
            }

            Wallet wallet;
            int selectedPayCharacterId = userStatePayCharacter.TryGetValue(userId, out int selectedCharacterId)
                ? selectedCharacterId
                : 0;
            int ownerCharacterId = UserData.GetActiveCharacterId(userId);
            if (userStatePay.ContainsKey(userId))
               wallet = _walletSwitchHandle.LoadUserWallet(userId, userStatePay[userId], pasteState, selectedPayCharacterId, ownerCharacterId);
            else
                wallet = _walletSwitchHandle.LoadUserWallet(userId, 0, pasteState, 0, ownerCharacterId);

            bool succefullPayment = false;

            _logger.LogInformation("Wallet information for user {UserId}. Copper: {Copper}, Silver: {Silver}, Gold: {Gold}, Owner Copper: {CopperOwner}, Owner Silver: {SilverOwner}, Owner Gold: {GoldOwner}, Coin Type: {CoinType}",
                userId, wallet.Copper, wallet.Silver, wallet.Gold, wallet.CopperOwner, wallet.SilverOwner, wallet.GoldOwner, userStateValueName[userId]);

            if (StateBotMSG[userId] >= 1)
            {
                _logger.LogInformation("Skipping first cycle for user {UserId}", userId);
                StateBotMSG[userId] = 0;
                return;
            }

            string inputText = (message?.Text ?? string.Empty).Trim();

            if (!int.TryParse(inputText, out int parsedIntValue))
            {
                _logger.LogWarning("Invalid number format from user {UserId}. Input: {Input}", userId, inputText);
                await botClient.SendTextMessageAsync(userId, "Виникла помилка: неправильний тип числа.", cancellationToken: cancellationToken);
                return;
            }

            _logger.LogInformation("Parsed wallet value: {Value} for user {UserId}", 
                parsedIntValue, userId);

            switch (userStateMethod[userId])
            {
                case "add":
                case "min":
                    _logger.LogInformation("Performing {Operation} operation for user {UserId}. Coin type: {CoinType}, Value: {Value}", 
                        userStateMethod[userId], userId, userStateValueName[userId], parsedIntValue);
                    _walletSubCommandsHandle.AddOrMinUpdateWallet(userStateMethod[userId], userStateValueName[userId], userId, parsedIntValue, wallet);
                    break;

                case "transfer":
                    _logger.LogInformation("Transferring {Value} {CoinType} for user {UserId}", 
                        parsedIntValue, userStateValueName[userId], userId);
                    bool succesfulTransaction = _moneyTransfer.TransferMoney(parsedIntValue, userStateValueName[userId], userId);

                    if(succesfulTransaction)
                        await botClient.SendTextMessageAsync(userId, "Вдалося обміняти монети!", cancellationToken: cancellationToken);
                    else
                        await botClient.SendTextMessageAsync(userId, "Ви занадто бідні лохи для цього!", cancellationToken: cancellationToken);
                    break;

                case "payChosen":
                    _logger.LogInformation("Processing payment for user {UserId}. Amount: {Amount}", userId, parsedIntValue);
                    succefullPayment = await _walletSwitchHandle.PaySwitchHandle(botClient, userId, wallet, parsedIntValue, cancellationToken);

                    if(succefullPayment)
                    {
                        _logger.LogInformation("Payment successful for user {UserId}", userId);
                        if(userId != masterID)
                            await botClient.SendTextMessageAsync(userId, "Дані було оновлено! У вас зараз:\n\n" + UserData.DataBaseWalletReader(userId), cancellationToken: cancellationToken);
                    }
                    break;

                case "fineUser":
                    _logger.LogInformation("Fining user {UserId}. Amount: {Amount}", choosedUserToStraf, parsedIntValue);
                    int choosedUserCharacterId = userStatePayCharacter.TryGetValue(masterID, out int finedCharacterId)
                        ? finedCharacterId
                        : 0;
                    await _walletSwitchHandle.FineUserSwitchHandle(botClient, pasteState, userId, parsedIntValue, cancellationToken, choosedUserToStraf, choosedUserCharacterId);
                    break;

                case "payToAll":
                    _logger.LogInformation("Paying to all users. Amount: {Amount} {CoinType}", parsedIntValue, userStateValueName[userId]);
                    await _walletSwitchHandle.PayToAllUsersSwitchHandle(botClient, userId, parsedIntValue, cancellationToken);
                    break;

                case "fine":
                    _logger.LogInformation("Fining all users. Amount: {Amount} {CoinType}", parsedIntValue, userStateValueName[userId]);
                    await _walletSwitchHandle.FineAllPlayersSwitchHandle(botClient, userId, parsedIntValue, cancellationToken);
                    break;
            }

            if (userStateMethod[userId] != "fine" && userStateMethod[userId] != "payToAll" && userStateMethod[userId] != "fineUser" && UserData.CheckifExist(userId) && userStateMethod[userId] != "payChosen")
            {
                _logger.LogInformation("Updating wallet display for user {UserId}", userId);
                await botClient.SendTextMessageAsync(userId, "Дані було оновлено! У вас зараз:\n\n" + UserData.DataBaseWalletReader(userId), cancellationToken: cancellationToken);
            }

            if (userStateMethod[userId] != "add" && userStateMethod[userId] != "min" && userStateMethod[userId] != "transfer" && userStateMethod[userId] != "fine" && userStateMethod[userId] != "payToAll" && userStateMethod[userId] != "fineUser")
            {
                if (userId == masterID)
                {
                    long targetUserId = userStatePay[userId];
                    int targetCharacterId = userStatePayCharacter.TryGetValue(userId, out int selectedTargetCharacterId) && selectedTargetCharacterId > 0
                        ? selectedTargetCharacterId
                        : UserData.GetActiveCharacterId(targetUserId);
                    string targetName = UserData.DataBaseValueReader<string>(targetUserId, targetCharacterId, "Name");
                    _logger.LogInformation("Updating wallet display for master user {UserId}", userId);
                    await botClient.SendTextMessageAsync(masterID, $"Дані було оновлено! У {targetName} (C{targetCharacterId}) наразі:\n\n" + UserData.DataBaseWalletReader(targetUserId, targetCharacterId), cancellationToken: cancellationToken);
                }
            }

            userStatePay[userId] = 0;
            userStatePayCharacter[userId] = 0;
            userStateWallet[userId] = "";
            userStateMethod[userId] = "";
            StateBotMSG[userId] = 1;
            DeleteAllOldMessages(botClient, cancellationToken, userId);

            _logger.LogInformation("Completed wallet operation for user {UserId}", userId);
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
    }
}
