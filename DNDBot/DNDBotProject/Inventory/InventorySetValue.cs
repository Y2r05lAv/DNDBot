using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace DNDBot.Inventory
{
    public class InventorySetValue : BotBodyRealisation
    {
        private readonly Dictionary<long, InventoryDataClass> inventoryDataClass = new();
        private readonly ILogger<InventorySetValue> _logger;

        public InventorySetValue(ILogger<InventorySetValue> logger)
        {
            _logger = logger;
        }

        ///


        public async Task StartSetVoid(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken, long userId)
        {
            _logger.LogInformation("Started object redaction initiated for user {UserId}", userId);

            if (!OldMessages.ContainsKey(userId))
                OldMessages.Add(userId, []);

            inventoryDataClass[userId] = new InventoryDataClass();
            StateBotMSG[userId] = 2;
            userStateInventory[userId] = "setValue";

            await PasteSetVoid(botClient, message, cancellationToken, userId);
        }

        ///


        public async Task PasteSetVoid(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken, long userId)
        {
            _logger.LogInformation("Started to redact info about object in user inventory {UserId} at step {Step}", userId, StateBotMSG[userId]);

            int itemId = 0;
            int itemAmount = 0;

            switch (StateBotMSG[userId])
            {
                case 2:
                    await botClient.SendTextMessageAsync(userId, "Напишіть числове айді предмета", cancellationToken: cancellationToken);
                    break;
                case 1:
                    if (!int.TryParse(message.Text?.Trim(), out itemId))
                    {
                        await HandleInvalidInput(botClient, cancellationToken, userId);
                        return;
                    }
                    inventoryDataClass[userId].ItemId = itemId;
                    await botClient.SendTextMessageAsync(userId, $"У вас наразі \n {UserInventory.InventoryDataGetSpecialObjectInfo(userId, itemId)}\n\n Скільки предметів ви хочете мати?", cancellationToken: cancellationToken);
                    break;
                case 0:
                    if (!int.TryParse(message.Text?.Trim(), out itemAmount))
                    {
                        await HandleInvalidInput(botClient, cancellationToken, userId, true);
                        return;
                    }
                    inventoryDataClass[userId].ItemAmount = itemAmount;
                    inventoryDataClass[userId].UserId = userId;
                    break;
            }

            StateBotMSG[userId]--;

            if (StateBotMSG[userId] < 0)
            {
                await FinalizeInventoryUpdate(botClient, cancellationToken, userId);
            }
        }

        ///


        private async Task HandleInvalidInput(ITelegramBotClient botClient, CancellationToken cancellationToken, long userId, bool itemDeletingException = false)
        {
            if (!itemDeletingException)
            {
                _logger.LogWarning("User {UserId} entered invalid input", userId);
                await botClient.SendTextMessageAsync(userId, "Невірне значення! Повинно бути числове значення.", cancellationToken: cancellationToken);
                StateBotMSG[userId] = 2;
            }
        }

        ///


        private async Task FinalizeInventoryUpdate(ITelegramBotClient botClient, CancellationToken cancellationToken, long userId)
        {
            _logger.LogInformation("Finalizing inventory update for user {UserId}", userId);

            userStatePay[userId] = 0;
            userStateInventory[userId] = "";
            StateBotMSG[userId] = 3;

            var inventory = inventoryDataClass[userId];
            inventory.ItemWeight = UserInventory.DataBaseValueReader<double>(userId, inventory.ItemId, "ItemWeight");
            inventory.ItemName = UserInventory.DataBaseValueReader<string>(userId, inventory.ItemId, "ItemName");

            _logger.LogInformation("Updated item: {ItemName}, ID: {ItemId}, Weight: {ItemWeight}", inventory.ItemName, inventory.ItemId, inventory.ItemWeight);

            if (inventory.ItemAmount > 0)
            {
                UserInventory.AddOrUpdateUserInventory(inventory);
                await botClient.SendTextMessageAsync(userId, $"{UserInventory.InventoryDataGetSpecialObjectInfo(userId, inventory.ItemId)}\n\n Дані було оновлено!", cancellationToken: cancellationToken);
            }
            else
            {
                UserInventory.AddOrUpdateUserInventory(inventory, 0);
                await botClient.SendTextMessageAsync(userId, "Предмет був видалений!", cancellationToken: cancellationToken);
            }
        }
    }


}

