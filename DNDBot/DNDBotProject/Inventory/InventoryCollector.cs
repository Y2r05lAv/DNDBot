#nullable enable

using Microsoft.Extensions.Logging; 
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace DNDBot
{
    public class InventoryCollector : BotBodyRealisation
    {

         
        private string[] DialogNumber =
        [
            "Введіть ім'я предмета:",
            "Введіть кількість предметів:",
            "Введіть вагу предмета:"
        ];

        private readonly ILogger<InventoryCollector> _logger;

        public InventoryCollector(ILogger<InventoryCollector> logger)
        {
            _logger = logger;
        }

        public async Task InventoryCreationTaskHandle(ITelegramBotClient botClient, CancellationToken cancellationToken, Message message)
        {
            if (message?.From == null)
                return;

            long userId = message.From.Id;

            if (message.Text == "Додати до інвентаря")
            {
                StateBotMSG[userId] = 0;
                userStateInventory[userId] = "start";
            }

            if (userStateInventory.TryGetValue(userId, out string? value))
            {
                switch (value)
                {
                    case "start":
                        await StartVoid(botClient, message, cancellationToken, userId);
                        break;
                    case "changeValue":
                        await PasteVoidAddObjToInventory(botClient, message, cancellationToken, userId);
                        break;
                }
            }
        }

        public async Task StartVoid(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken, long userId)
        {
            if (message?.From != null && !OldMessages.ContainsKey(message.From.Id))
                OldMessages.Add(message.From.Id, []);

            InventoryDataClass[userId] = new InventoryDataClass
            {
                UserId = userId
            };

            StateBotMSG[userId] = 0;  
            userStateInventory[userId] = "changeValue";

            _logger.LogInformation("Started cycle: Add item in inventory.");
            await SendMessage(botClient, userId, DialogNumber[0], cancellationToken);
        }

        private async Task PasteVoidAddObjToInventory(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken, long userId)
        {
            if (!StateBotMSG.ContainsKey(userId) || !InventoryDataClass.ContainsKey(userId))
                return;

            string inputText = message?.Text?.Trim() ?? "";
            if (message != null)
                OldMessages[userId].Add(message.MessageId);

            int step = (int)StateBotMSG[userId];

            switch (step)
            {
                case 0:  
                    if (string.IsNullOrWhiteSpace(inputText))
                    {
                        await SendMessage(botClient, userId, "❗ Неправильний формат імені!", cancellationToken);
                        return;
                    }
                    InventoryDataClass[userId].ItemName = inputText;
                    break;

                case 1:  
                    if (!int.TryParse(inputText, out int amount) || amount <= 0)
                    {
                        await SendMessage(botClient, userId, "❗ Введіть правильне число!", cancellationToken);
                        return;
                    }
                    InventoryDataClass[userId].ItemAmount = amount;
                    break;

                case 2:  
                    if (!double.TryParse(inputText, NumberStyles.Float, CultureInfo.InvariantCulture, out double weight) || weight <= 0)
                    {
                        await SendMessage(botClient, userId, "❗ Введіть правильну вагу!", cancellationToken);
                        return;
                    }
                    InventoryDataClass[userId].ItemWeight = Math.Round(weight, 2);
                    break;
            }

             
            if (step >= DialogNumber.Length - 1)
            {
                await AddInfoToData(botClient, userId, cancellationToken);
                DeleteAllOldMessages(botClient, cancellationToken, userId);
                userStateInventory[userId] = "";
                StateBotMSG[userId] = 0;
                return;
            }

             
            StateBotMSG[userId]++;
            await SendMessage(botClient, userId, DialogNumber[StateBotMSG[userId]], cancellationToken);

            _logger.LogInformation($"User completed {StateBotMSG[userId]} of {DialogNumber.Length} steps.");
        }

        private async Task SendMessage(ITelegramBotClient botClient, long userId, string message, CancellationToken cancellationToken, Message? originalMessage = null)
        {
            var sentMessage = await botClient.SendTextMessageAsync(userId, message, cancellationToken: cancellationToken);
            if (!OldMessages.ContainsKey(userId))
                OldMessages[userId] = new List<int>();
            OldMessages[userId].Add(sentMessage.MessageId);

            if (originalMessage != null)
                OldMessages[userId].Add(originalMessage.MessageId);
        }

        private async Task AddInfoToData(ITelegramBotClient botClient, long userId, CancellationToken cancellationToken)
        {
            var result = UserInventory.AddOrUpdateUserInventory(InventoryDataClass[userId]);
            await botClient.SendTextMessageAsync(userId, result, cancellationToken: cancellationToken);
        }
    }
}
