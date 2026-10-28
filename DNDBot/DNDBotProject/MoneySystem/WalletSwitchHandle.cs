﻿using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;

namespace DNDBot.MoneySystem
{
    class WalletSwitchHandle : BotBodyRealisation
    {

        private readonly ILogger<WalletSwitchHandle> _logger;
        private readonly WalletSubCommandsHandle _walletSubCommandsHandle;

        public WalletSwitchHandle(ILogger<WalletSwitchHandle> logger, WalletSubCommandsHandle walletSubCommandsHandle)
        {
            _logger = logger;
            _walletSubCommandsHandle = walletSubCommandsHandle;
        }

        private static int ResolveCharacterId(long userId, int characterId)
        {
            if (characterId > 0 && UserData.CheckifExist(userId, characterId))
                return characterId;

            return UserData.GetActiveCharacterId(userId);
        }


        public async Task FineAllPlayersSwitchHandle(ITelegramBotClient botClient, long userId, int parsedIntValue, CancellationToken cancellationToken)
        {
            string coinType2 = "";
            MasterComand_1(parsedIntValue, true, userId, ref coinType2);

            await botClient.SendTextMessageAsync(masterID, $"Дані було оновлено! Ви оштрафували всіх гравців на {parsedIntValue} {coinType2}!", cancellationToken: cancellationToken);

            var users = UserData.GetUsersFromDatabase()
                .Where(u => u.Id != masterID)
                .Select(u => u.Id)
                .Distinct();

            foreach (long targetUserId in users)
            {
                await botClient.SendTextMessageAsync(targetUserId, $"Дані було оновлено! Ви були оштрафовані від майстра на {parsedIntValue} {coinType2}!", cancellationToken: cancellationToken);
            }
        }

        public async Task PayToAllUsersSwitchHandle(ITelegramBotClient botClient, long userId, int parsedIntValue, CancellationToken cancellationToken)
        {
            string coinType = "";
            MasterComand_1(parsedIntValue, false, userId, ref coinType);

            await botClient.SendTextMessageAsync(masterID, $"Дані було оновлено! Ви додали всі гравцям {parsedIntValue} {coinType}!", cancellationToken: cancellationToken);

            var users = UserData.GetUsersFromDatabase()
                .Where(u => u.Id != masterID)
                .Select(u => u.Id)
                .Distinct();

            foreach (long targetUserId in users)
            {
                await botClient.SendTextMessageAsync(targetUserId, $"Дані було оновлено! Ви отримали від майстра {parsedIntValue} {coinType}!", cancellationToken: cancellationToken);
            }
        }

        public async Task FineUserSwitchHandle(ITelegramBotClient botClient, string pasteState, long userId, int parsedIntValue, CancellationToken cancellationToken, long choosedUser, int choosedUserCharacterId)
        {
            _logger.LogInformation("Starting fine cycle for user {UserId} with amount {Amount}", choosedUser, parsedIntValue);

            int resolvedTargetCharacterId = ResolveCharacterId(choosedUser, choosedUserCharacterId);
            Wallet wallet = LoadUserWallet(choosedUser, choosedUser, "fine", resolvedTargetCharacterId);

            string coinTypefine = "";
            string name = UserData.DataBaseValueReader<string>(choosedUser, resolvedTargetCharacterId, "Name");

            switch (userStateValueName[userId])
            {
                case "copper":
                    wallet.Copper -= parsedIntValue;
                    coinTypefine = "мідних монет";
                    break;
                case "silver":
                    wallet.Silver -= parsedIntValue;
                    coinTypefine = "срібних монет";
                    break;
                case "gold":
                    wallet.Gold -= parsedIntValue;
                    coinTypefine = "золотих монет";
                    break;
                case "platinum":
                    wallet.Platinum -= parsedIntValue;
                    coinTypefine = "платинових монет";
                    break;
                default:
                    _logger.LogWarning("Invalid coin type.");
                    break;
            }

            _walletSubCommandsHandle.UpdateDataAfterPayment(choosedUser, wallet, false, resolvedTargetCharacterId);

            await botClient.SendTextMessageAsync(masterID, $"Дані було оновлено! Ви оштрафували {name} (C{resolvedTargetCharacterId}) на {parsedIntValue} {coinTypefine}!", cancellationToken: cancellationToken);
            await botClient.SendTextMessageAsync(choosedUser, $"Дані було оновлено! Вас було оштрафовано на {parsedIntValue} {coinTypefine}!", cancellationToken: cancellationToken);


            userStatePay[userId] = 0;
            userStatePayCharacter[userId] = 0;
            userStateValueName[userId] = "";

            _logger.LogInformation("User {UserId} was successfully fined {Amount} {CoinType}", choosedUser, parsedIntValue, coinTypefine);


        }



        public async Task<bool> PaySwitchHandle(ITelegramBotClient botClient, long userId, Wallet wallet, int parsedIntValue, CancellationToken cancellationToken)
        {
            long receiverUserId = userStatePay[userId];
            int receiverCharacterId = ResolveCharacterId(
                receiverUserId,
                userStatePayCharacter.TryGetValue(userId, out int selectedCharacterId) ? selectedCharacterId : 0
            );
            int ownerCharacterId = UserData.GetActiveCharacterId(userId);

            _logger.LogInformation("Starting payment process. Sender: {SenderId}, Receiver: {ReceiverId}, CoinType: {CoinType}, Amount: {Amount}", 
                userId, receiverUserId, userStateValueName[userId], parsedIntValue);

            string CoinTypeForBot = "";
            string CoinTypeSwitch = "";

            if(userId != masterID)
            if (wallet.CopperOwner < parsedIntValue && wallet.SilverOwner < parsedIntValue && wallet.GoldOwner < parsedIntValue && wallet.PlatinumOwner < parsedIntValue)
            {
                await _walletSubCommandsHandle.NotEnoughMoney(botClient, cancellationToken, userId);
                return false;
            }

            _logger.LogInformation("Sufficient funds verified for payment. Sender: {SenderId}, CoinType: {CoinType}, Amount: {Amount}", 
                userId, userStateValueName[userId], parsedIntValue);



            int value = 0;
            int ownerValue = 0;
            string userName = UserData.DataBaseValueReader<string>(receiverUserId, receiverCharacterId, "Name");

            CoinTypeSwitch = userStateValueName[userId];

            _walletSubCommandsHandle.ReturnValuesFromWalletClass(userId, wallet, ref CoinTypeForBot, CoinTypeSwitch, ref value, ref ownerValue);

            _walletSubCommandsHandle.PaymentSystem(userId, ref value, ref ownerValue, parsedIntValue);

            _walletSubCommandsHandle.ReturnValueToWalletClass(userId, wallet, CoinTypeSwitch, value, ownerValue);

            _walletSubCommandsHandle.UpdateDataAfterPayment(userId, wallet, true, receiverCharacterId, ownerCharacterId);


            await botClient.SendTextMessageAsync(receiverUserId, $"Вам надішли кошти у розмірі {parsedIntValue} {CoinTypeForBot}!", cancellationToken: cancellationToken);
            await botClient.SendTextMessageAsync(userId, $"Ви переслали гравцю {userName} (C{receiverCharacterId}) {parsedIntValue} {CoinTypeForBot}!", cancellationToken: cancellationToken);

            _logger.LogInformation("Payment completed successfully. Sender: {SenderId}, Receiver: {ReceiverId}, Amount: {Amount} {CoinType}", 
                userId, receiverUserId, parsedIntValue, CoinTypeForBot);

            return true;
        }

        public void MasterComand_1(int resultInt, bool isItfine, long userId, ref string coinType)
        {
            var users = UserData.GetUsersFromDatabase()
                .Where(u => u.Id != masterID)
                .Select(u => u.Id)
                .Distinct();

            Wallet wallet;

             
            foreach (long targetUserId in users)
            {
                int targetCharacterId = UserData.GetActiveCharacterId(targetUserId);
                wallet = LoadUserWallet(targetUserId, targetUserId, "re", targetCharacterId);

                switch (userStateValueName[userId])
                {
                    case "copper":
                        wallet.Copper = isItfine == true ? wallet.Copper - resultInt : wallet.Copper + resultInt;
                        coinType = "мідних монет";
                        break;
                    case "silver":
                        wallet.Silver = isItfine == true ? wallet.Silver - resultInt : wallet.Silver + resultInt;
                        coinType = "срібних монет";
                        break;
                    case "gold":
                        wallet.Gold = isItfine == true ? wallet.Gold - resultInt : wallet.Gold + resultInt;
                        coinType = "золотих монет";
                        break;
                    case "platinum":
                        wallet.Platinum = isItfine == true ? wallet.Platinum - resultInt : wallet.Platinum + resultInt;
                        coinType = "платинових монет";
                        break;
                }

                _logger.LogInformation("User with {id} and wallet have {wallet}", targetUserId, wallet.Gold);
                _walletSubCommandsHandle.UpdateDataAfterPayment(targetUserId, wallet, false, targetCharacterId);
            }

            _logger.LogInformation("Completed {Operation} operation for all users. Value: {Amount} {CoinType}", 
                isItfine ? "fine" : "payment", resultInt, coinType);

        }

        public Wallet LoadUserWallet(long userId, long choosedUser, string pasteState, int choosedUserCharacterId = 0, int ownerCharacterId = 0)
        {
            if (pasteState != "payChosen")
            {
                int userCharacterId = ResolveCharacterId(userId, choosedUserCharacterId);
                return new Wallet(
                    UserData.DataBaseValueReader<int>(userId, userCharacterId, "CopperCoin"),
                    UserData.DataBaseValueReader<int>(userId, userCharacterId, "SilverCoin"),
                    UserData.DataBaseValueReader<int>(userId, userCharacterId, "GoldenCoin"),
                    UserData.DataBaseValueReader<int>(userId, userCharacterId, "PlatinumCoin"),

                    -1, -1, -1, -1
                );
            }
            else
            {
                int receiverCharacterId = ResolveCharacterId(choosedUser, choosedUserCharacterId);
                int resolvedOwnerCharacterId = ResolveCharacterId(userId, ownerCharacterId);

                return new Wallet(
                    UserData.DataBaseValueReader<int>(choosedUser, receiverCharacterId, "CopperCoin"),
                    UserData.DataBaseValueReader<int>(choosedUser, receiverCharacterId, "SilverCoin"),
                    UserData.DataBaseValueReader<int>(choosedUser, receiverCharacterId, "GoldenCoin"),
                    UserData.DataBaseValueReader<int>(choosedUser, receiverCharacterId, "PlatinumCoin"),

                     
                    UserData.DataBaseValueReader<int>(userId, resolvedOwnerCharacterId, "CopperCoin"),
                    UserData.DataBaseValueReader<int>(userId, resolvedOwnerCharacterId, "SilverCoin"),
                    UserData.DataBaseValueReader<int>(userId, resolvedOwnerCharacterId, "GoldenCoin"),
                    UserData.DataBaseValueReader<int>(userId, resolvedOwnerCharacterId, "PlatinumCoin")

                );
            }
        }
    }
}
