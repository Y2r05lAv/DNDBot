 ﻿using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace DNDBot.Inventory
{
    public class InventorySetMasterValue : BotBodyRealisation
    {
        public Dictionary<long, long> choosenUser = new Dictionary<long, long>();
        private Dictionary<long, InventoryDataClass> inventoryDataClass = new Dictionary<long, InventoryDataClass>();

        private ILogger<InventorySetMasterValue> _logger;

        public InventorySetMasterValue(ILogger<InventorySetMasterValue> logger)
        {
            _logger = logger;
        }

        public async Task StartSetMasterVoid(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken, long choosenUserId = 0)
        {
            _logger.LogInformation("Starting to set master value for user {ChoosenUserId}'s inventory", choosenUserId);

            if (!inventoryDataClass.TryAdd(masterID, new InventoryDataClass()))
                inventoryDataClass[masterID] = new InventoryDataClass();

            choosenUser.TryAdd(masterID, choosenUserId);

            StateBotMSG[masterID] = 2;

            userStateInventory[masterID] = "setMasterValue";

            await PasteSetMasterVoid(botClient, message, cancellationToken, choosenUser[masterID]);
        }

        public async Task PasteSetMasterVoid(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken, long choosenUserId = 0)
        {
            _logger.LogInformation("Entering PasteSetMasterVoid for user {MasterID}, step {Step}", masterID, StateBotMSG[masterID]);

            choosenUserId = choosenUser[masterID];
            int itemId;
            int itemAmount;

            if (message == null)
                return;

            if(message.Text == "Закінчити")
            {
                userStateInventory[masterID] = "";
                await botClient.SendTextMessageAsync(masterID, "Праця була прервана.", cancellationToken: cancellationToken);
                StateBotMSG[masterID] = 2;
            }

            switch (StateBotMSG[masterID])
            {
                case 2:
                    _logger.LogInformation("Requesting item ID from master {MasterID} for user {ChoosenUserId}", masterID, choosenUserId);
                    await botClient.SendTextMessageAsync(masterID, $"Напишіть числове айді предмета", cancellationToken: cancellationToken);
                    break;
                case 1:
                    int.TryParse((message.Text ?? string.Empty).Trim(), out itemId);
                    _logger.LogInformation("Received item ID {ItemId} from master {MasterID}", itemId, masterID);
                    await ExceptionNotRightFormatHandler(botClient, cancellationToken, itemId, choosenUserId);

                    inventoryDataClass[masterID].ItemId = itemId;

                    _logger.LogInformation("Requesting item amount for item ID {ItemId} from master {MasterID}", itemId, masterID);
                    await botClient.SendTextMessageAsync(masterID, $"У вас наразі \n {UserInventory.InventoryDataGetSpecialObjectInfo(choosenUserId, itemId)}\n\n Скільки предметів ви хочете мати?", cancellationToken: cancellationToken);
                    break;
                case 0:
                    int.TryParse((message.Text ?? string.Empty).Trim(), out itemAmount);
                    _logger.LogInformation("Received item amount {ItemAmount} from master {MasterID}", itemAmount, masterID);
                    await ExceptionNotRightFormatHandler(botClient, cancellationToken, itemAmount, choosenUserId, true);
                    inventoryDataClass[masterID].ItemAmount = itemAmount;
                    inventoryDataClass[masterID].UserId = choosenUser[masterID];
                    break;
            }

            StateBotMSG[masterID]--;

            if (StateBotMSG[masterID] < 0)
            {
                inventoryDataClass[masterID].ItemWeight = UserInventory.DataBaseValueReader<double>(choosenUserId, inventoryDataClass[masterID].ItemId, "ItemWeight");
                inventoryDataClass[masterID].ItemName = UserInventory.DataBaseValueReader<string>(choosenUserId, inventoryDataClass[masterID].ItemId, "ItemName");

                if (inventoryDataClass[masterID].ItemAmount > 0)
                {
                    _logger.LogInformation("Adding item to inventory - User: {UserId}, Item: {ItemId}, Amount: {ItemAmount}", 
                        inventoryDataClass[masterID].UserId, inventoryDataClass[masterID].ItemId, inventoryDataClass[masterID].ItemAmount);
                    UserInventory.AddOrUpdateUserInventory(inventoryDataClass[masterID]);
                    await botClient.SendTextMessageAsync(inventoryDataClass[masterID].UserId, "Щось ваша сумка стало тяжщою...", cancellationToken: cancellationToken);

                    await botClient.SendTextMessageAsync(masterID, $"{UserInventory.InventoryDataGetSpecialObjectInfo(inventoryDataClass[masterID].UserId, inventoryDataClass[masterID].ItemId)}\n\n Дані було оновлено!", cancellationToken: cancellationToken);
                }
                else
                {
                    _logger.LogInformation("Removing item from inventory - User: {UserId}, Item: {ItemId}", 
                        inventoryDataClass[masterID].UserId, inventoryDataClass[masterID].ItemId);
                    UserInventory.AddOrUpdateUserInventory(inventoryDataClass[masterID], 0);
                    await botClient.SendTextMessageAsync(inventoryDataClass[masterID].UserId, "Вам здається що стало якось легше...", cancellationToken: cancellationToken);

                    await botClient.SendTextMessageAsync(masterID, $"Предмет був видалений!", cancellationToken: cancellationToken);
                }

                userStatePay[masterID] = 0;
                userStateInventory[masterID] = "";
                StateBotMSG[masterID] = 3;

                _logger.LogInformation("Completed inventory update for user {UserId}", inventoryDataClass[masterID].UserId);
                return;
            }
        }

        private async Task ExceptionNotRightFormatHandler(ITelegramBotClient botClient, CancellationToken cancellationToken, int itemInfo, long userId, bool itemDeletingExeption = false)
        {
            if (itemInfo == 0 && !itemDeletingExeption)
            {
                _logger.LogWarning("Invalid value received from user {UserId}", userId);
                await botClient.SendTextMessageAsync(userId, "Невірне значення! Повинно бути числове значення.", cancellationToken: cancellationToken);
                StateBotMSG[userId] = 2;
            }
        }
    }
}
