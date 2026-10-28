using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;

namespace DNDBot.MoneySystem
{
    class WalletSubCommandsHandle : BotBodyRealisation
    {

        private readonly ILogger<WalletSubCommandsHandle> _logger;

        public WalletSubCommandsHandle(ILogger<WalletSubCommandsHandle> logger)
        {
            _logger = logger;
        }

        public void ReturnValuesFromWalletClass(long userId, Wallet wallet, ref string CoinTypeForBot, string CoinTypeSwitch, ref int value, ref int ownerValue)
        {
            switch (CoinTypeSwitch)
            {
                case "copper":
                    value = wallet.Copper;
                    ownerValue = wallet.CopperOwner;
                    CoinTypeForBot = "мідних монет";
                    break;
                case "silver":
                    value = wallet.Silver;
                    ownerValue = wallet.SilverOwner;
                    CoinTypeForBot = "срібних монет";
                    break;
                case "gold":
                    value = wallet.Gold;
                    ownerValue = wallet.GoldOwner;
                    CoinTypeForBot = "золотих монет";
                    break;
                case "platinum":
                    value = wallet.Platinum;
                    ownerValue = wallet.PlatinumOwner;
                    CoinTypeForBot = "платинових монет";
                    break;
                default:
                    _logger.LogWarning("Value name not exist: {userStateValueName}", userStateValueName[userId]);
                    break;
            }
        }

        public void ReturnValueToWalletClass(long userId, Wallet wallet, string CoinTypeSwitch, int value, int ownerValue)
        {
            switch (CoinTypeSwitch)
            {
                case "copper":
                    wallet.Copper = value;
                    wallet.CopperOwner = ownerValue;
                    break;
                case "silver":
                    wallet.Silver = value;
                    wallet.SilverOwner = ownerValue;
                    break;
                case "gold":
                    wallet.Gold = value;
                    wallet.GoldOwner = ownerValue;
                    break;
                case "platinum":
                    wallet.Platinum = value;
                    wallet.PlatinumOwner = ownerValue;
                    break;
                default:
                    _logger.LogWarning("Value name not exist: {userStateValueName}", userStateValueName[userId]);
                    break;
            }
        }

        public void AddOrMinUpdateWallet(string functionName, string valueName, long userId, int parsedIntValue, Wallet wallet)
        {
            _logger.LogWarning("Name of coin: {Value}", functionName);

            switch (valueName)
            {
                case "copper":
                    wallet.Copper = functionName == "add" ? wallet.Copper + parsedIntValue : wallet.Copper - parsedIntValue;
                    break;
                case "silver":
                    wallet.Silver = functionName == "add" ? wallet.Silver + parsedIntValue : wallet.Silver - parsedIntValue; 
                    break;
                case "gold":
                    wallet.Gold = functionName == "add" ? wallet.Gold + parsedIntValue : wallet.Gold - parsedIntValue; 
                    break;
                case "platinum":
                    wallet.Platinum = functionName == "add" ? wallet.Platinum + parsedIntValue : wallet.Platinum - parsedIntValue;
                    break;
                default:
                    _logger.LogWarning("Invalid coin type.");
                    break;
            }

            UpdateDataAfterPayment(userId, wallet, false);
        }

        public void PaymentSystem(long userId, ref int value, ref int ownerValue, int changeValue)
        {
            if (userId == masterID)
            {
                value += changeValue;
            }
            if (ownerValue >= changeValue)
            {
                value += changeValue;
                ownerValue -= changeValue;
            }
        }

        public async Task NotEnoughMoney(ITelegramBotClient botClient, CancellationToken cancellationToken, long userId)
        {
            await botClient.SendTextMessageAsync(userId, "Недостатньо коштів!", cancellationToken: cancellationToken);

            StateBotMSG[userId] = 1;
        }

        private static int ResolveCharacterId(long userId, int characterId)
        {
            if (characterId > 0 && UserData.CheckifExist(userId, characterId))
                return characterId;

            return UserData.GetActiveCharacterId(userId);
        }

        public void UpdateDataAfterPayment(long userId, Wallet wallet, bool isItPayment, int receiverCharacterId = 0, int ownerCharacterId = 0)
        {
            if (isItPayment )
            {
                long receiverUserId = userStatePay[userId];
                int resolvedReceiverCharacterId = ResolveCharacterId(receiverUserId, receiverCharacterId);
                int resolvedOwnerCharacterId = ResolveCharacterId(userId, ownerCharacterId);

                UserData.UpdateDataBaseValue("CopperCoin", receiverUserId, resolvedReceiverCharacterId, wallet.Copper);
                UserData.UpdateDataBaseValue("SilverCoin", receiverUserId, resolvedReceiverCharacterId, wallet.Silver);
                UserData.UpdateDataBaseValue("GoldenCoin", receiverUserId, resolvedReceiverCharacterId, wallet.Gold);
                UserData.UpdateDataBaseValue("PlatinumCoin", receiverUserId, resolvedReceiverCharacterId, wallet.Platinum);

                UserData.UpdateDataBaseValue("CopperCoin", userId, resolvedOwnerCharacterId, wallet.CopperOwner);
                UserData.UpdateDataBaseValue("SilverCoin", userId, resolvedOwnerCharacterId, wallet.SilverOwner);
                UserData.UpdateDataBaseValue("GoldenCoin", userId, resolvedOwnerCharacterId, wallet.GoldOwner);
                UserData.UpdateDataBaseValue("PlatinumCoin", userId, resolvedOwnerCharacterId, wallet.PlatinumOwner);

            }
            else
            {
                int resolvedCharacterId = ResolveCharacterId(userId, receiverCharacterId);

                UserData.UpdateDataBaseValue("CopperCoin", userId, resolvedCharacterId, wallet.Copper);
                UserData.UpdateDataBaseValue("SilverCoin", userId, resolvedCharacterId, wallet.Silver);
                UserData.UpdateDataBaseValue("GoldenCoin", userId, resolvedCharacterId, wallet.Gold);
                UserData.UpdateDataBaseValue("PlatinumCoin", userId, resolvedCharacterId, wallet.Platinum);

            }

        }
    }
}
