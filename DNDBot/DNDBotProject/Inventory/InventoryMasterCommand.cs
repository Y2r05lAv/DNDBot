using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace DNDBot.Inventory
{
    class InventoryMasterCommand : BotBodyRealisation
    {
        private readonly ILogger<InventoryMasterCommand> _logger;
        private readonly string[] DialogNumber =
        {
            "Введіть вагу предмета:", "Введіть кількість предметів:", "Введіть ім'я предмета"
        };

        public InventoryMasterCommand(ILogger<InventoryMasterCommand> logger)
        {
            _logger = logger;
        }

        public async Task MasterStartVoid(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken, long userId)
        {
            _logger.LogInformation("MasterStartVoid initiated for user {UserId}", userId);

            if (!OldMessages.ContainsKey(masterID))
                OldMessages.Add(masterID, []);

            if (!userStateInventory.TryAdd(masterID, "masterStart"))
                userStateInventory[masterID] = "masterStart";

            userStateInventory[masterID] = "changeMasterValue";

            InventoryDataClass[masterID] = new InventoryDataClass();

            if (choosedUser > 0)
                InventoryDataClass[masterID].UserId = choosedUser;


            UpdateUserStatePay(ref StateBotMSG, masterID, false);

            StateBotMSG[masterID] = 3;

            await MasterPasteVoidAddObjToInventory(botClient, message, cancellationToken, choosedUser);
        }

        public async Task MasterPasteVoidAddObjToInventory(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken, long userId = 0)
        {
            _logger.LogInformation($"Processing inventory input for user {userId} at state {StateBotMSG[masterID]}", masterID);

            string inputText = message?.Text?.Trim() ?? "";

            if (message?.Text?.Trim() == "Повернутись")
            {
                StateBotMSG[masterID]++;
                if (StateBotMSG[masterID] >= DialogNumber.Length)
                {
                    await AddInfoToData(botClient, cancellationToken);
                    userStateDataCollector[masterID] = "";
                    StateBotMSG[masterID] = 0;
                    return;
                }
                OldMessages[masterID].Add((await botClient.SendTextMessageAsync(masterID, DialogNumber[StateBotMSG[masterID]], cancellationToken: cancellationToken)).MessageId);
                return;
            }

            switch (StateBotMSG[masterID])
            {
                case 2:
                    if (string.IsNullOrEmpty(inputText))
                    {
                        await SendErrorMessage(botClient, masterID, "Неправильний формат ім'я!", cancellationToken);
                        return;
                    }
                    InventoryDataClass[masterID].ItemName = inputText;
                    break;
                case 1:
                    if (!int.TryParse(inputText, out int resultInt) || resultInt <= 0 || resultInt >= int.MaxValue)
                    {
                        await SendErrorMessage(botClient, masterID, "Неправильний формат цифри!", cancellationToken);
                        return;
                    }
                    InventoryDataClass[masterID].ItemAmount = resultInt;
                    break;
                case 0:
                    if (!double.TryParse(inputText, NumberStyles.Float, CultureInfo.InvariantCulture, out double resultFloat) || resultFloat >= int.MaxValue)
                    {
                        await SendErrorMessage(botClient, masterID, "Неправильний формат цифри!", cancellationToken);
                        return;
                    }
                    InventoryDataClass[masterID].ItemWeight = resultFloat;
                    break;
            }

            StateBotMSG[masterID]--;

            if (message != null)
                OldMessages[masterID].Add(message.MessageId);

            

            if (StateBotMSG[masterID] < 0)
            {
                await AddInfoToData(botClient, cancellationToken);
                DeleteAllOldMessages(botClient, cancellationToken, masterID);
                await botClient.SendTextMessageAsync(InventoryDataClass[masterID].UserId, "Щось ваша сумка стала важчою...", cancellationToken: cancellationToken);
                userStateInventory[masterID] = "";
                StateBotMSG[masterID] = 0;
                return;
            }

            await botClient.SendTextMessageAsync(masterID, DialogNumber[StateBotMSG[masterID]], cancellationToken: cancellationToken);
        }

        private async Task SendErrorMessage(ITelegramBotClient botClient, long userId, string errorMessage, CancellationToken cancellationToken)
        {
            OldMessages[userId].Add((await botClient.SendTextMessageAsync(userId, errorMessage, cancellationToken: cancellationToken)).MessageId);
            _logger.LogWarning($"Error for user {userId}: {errorMessage}", userId, errorMessage);
        }

        private async Task AddInfoToData(ITelegramBotClient botClient, CancellationToken cancellationToken)
        {
            string response = UserInventory.AddOrUpdateUserInventory(InventoryDataClass[masterID]);
            await botClient.SendTextMessageAsync(masterID, response, cancellationToken: cancellationToken);
            _logger.LogInformation($"Inventory updated for user {masterID}");
        }

        private new void UpdateUserStatePay(ref Dictionary<long, long> userState, long userId, bool fromCallBack = false, long chatId = 0)
        {
            if (fromCallBack)
            {
                if (userState.ContainsKey(chatId))
                    userState[chatId] = userId;
                else
                    userState.Add(chatId, masterID);
            }
            else
            {
                if (userState.ContainsKey(masterID))
                    userState[masterID] = 0;
                else
                    userState.Add(masterID, 0);
            }
        }
    }

}

