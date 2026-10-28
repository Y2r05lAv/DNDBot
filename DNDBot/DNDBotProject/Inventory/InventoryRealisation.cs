using DNDBot.BotSystem;
using DNDBot.Inventory;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace DNDBot
{
    class InventoryRealisation : BotBodyRealisation
    {
        private readonly ILogger<InventoryRealisation> _logger;
        private readonly InventoryCollector _inventoryCollector;
        private readonly InventoryMasterCommand _inventoryMasterCommand;
        private readonly InventorySetMasterValue _inventorySetMasterValue;
        private readonly InventorySetValue _inventorySetValue;

        public InventoryRealisation(ILogger<InventoryRealisation> logger, InventoryCollector inventoryCollector, InventoryMasterCommand inventoryMasterCommand, 
            InventorySetValue inventorySetValue, InventorySetMasterValue inventorySetMasterValue)
        {
            _logger = logger;
            _inventoryCollector = inventoryCollector;
            _inventoryMasterCommand = inventoryMasterCommand;
            _inventorySetValue = inventorySetValue;
            _inventorySetMasterValue = inventorySetMasterValue;
        }

        public async Task InventoryTaskHandle(ITelegramBotClient botClient, CancellationToken cancellationToken, Message message)
        {
            if (message?.From == null)
                return;

            long userId = message.From.Id;
             

            try
            {
                if (message.Text == "Інвентар")
                {
                    _logger.LogInformation("User {UserId} requested inventory menu", userId);
                    await InlineKeyboardsClass.ButtonInventoryCreation(botClient, cancellationToken, userId, "inv_", "Команди інвентаря");
                }

                if (userStateInventory.ContainsKey(userId))
                {
                    _logger.LogInformation("Processing inventory state {State} for user {UserId}", userStateInventory[userId], userId);
                    switch (userStateInventory[userId])
                    {
                        case "set":
                            await _inventorySetValue.StartSetVoid(botClient, message, cancellationToken, userId);
                            break;
                        case "setValue":
                            await _inventorySetValue.PasteSetVoid(botClient, message, cancellationToken, userId);
                            break;
                        case "masterStart":
                            await _inventoryMasterCommand.MasterStartVoid(botClient, message, cancellationToken,0);
                            break;
                        case "changeMasterValue":
                            await _inventoryMasterCommand.MasterPasteVoidAddObjToInventory(botClient, message, cancellationToken);
                            break;
                        case "setMaster":
                            await _inventorySetMasterValue.StartSetMasterVoid(botClient, message, cancellationToken);
                            break;
                        case "setMasterValue":
                            await _inventorySetMasterValue.PasteSetMasterVoid(botClient, message, cancellationToken);
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing inventory task for user {UserId}", userId);
                await botClient.SendTextMessageAsync(userId, "Сталася помилка під час обробки інвентаря. Спробуйте ще раз.", cancellationToken: cancellationToken);
            }
        }

        private async Task StartSetObjectValue(ITelegramBotClient botClient, CancellationToken cancellationToken, long userId, bool IsUserMaster)
        {
            _logger.LogInformation("Starting set object value for user {UserId}", userId);
            await botClient.SendTextMessageAsync(IsUserMaster ? masterID : userId, "Який айді у предмета, котрий ви хочете змінити?\n" + UserInventory.InventoryDataBaseReader(userId), cancellationToken: cancellationToken);

            if(!IsUserMaster)
                await _inventorySetValue.StartSetVoid(botClient, null, cancellationToken, userId);
            else
                await _inventorySetMasterValue.StartSetMasterVoid(botClient, null, cancellationToken, userId);
        }

        public async Task HandleInventoryCallback(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken, Message message, CallbackQuery callbackQuery)
        {
            if (callbackQuery?.Data == null || callbackQuery.Message?.Chat == null)
                return;

            long chatId = callbackQuery.Message.Chat.Id;
            string callbackData = callbackQuery.Data;
             

            try
            {
                var parts = callbackData.Split('_');
                if (parts.Length < 2)
                    return;
                string recipientId = parts[1];

                if (callbackQuery.Data.StartsWith("inv_"))
                {
                    _logger.LogInformation("Processing inventory callback command: {Command}", recipientId);
                    switch (recipientId)
                    {
                        case "check":
                            _logger.LogInformation("Checking inventory for user {UserId}", chatId);
                            if (UserInventory.CheckifExist(chatId))
                                await SendInventoryData(botClient, cancellationToken, chatId, "Ваш інвентар: \nЗагальна вага: " + UserInventory.GetAllInventoryWeight(chatId) + " кг");
                            else
                                await SendInventoryData(botClient, cancellationToken, chatId, "Не існує даних про користувача " + chatId);
                            break;
                        case "add":
                            _logger.LogInformation("Adding item to inventory for user {UserId}", chatId);
                            await _inventoryCollector.StartVoid(botClient, message, cancellationToken, chatId);
                            break;
                        case "set":
                            if (chatId == masterID)
                            {
                                _logger.LogInformation("Master {MasterId} is setting item for another user", chatId);
                                await GetUserInCallBackDataMasterCommand(botClient, cancellationToken, "setMaster_", "У якого гравця ви хочете змінити предмет?");
                            }
                            else
                            {
                                _logger.LogInformation("User {UserId} is setting item", chatId);
                                await StartSetObjectValue(botClient, cancellationToken, chatId, false);
                            }
                            break;
                        case "masterAdd":
                            _logger.LogInformation("Master {MasterId} is adding item to another user's inventory", chatId);
                            await GetUserInCallBackDataMasterCommand(botClient, cancellationToken, "itemMaster_", "До якого гравця ви хочете додати предмет?");
                            break;
                        case "masterCheck":
                            _logger.LogInformation("Master {MasterId} is checking another user's inventory", chatId);
                            await GetUserInCallBackDataMasterCommand(botClient, cancellationToken, "checkMaster_", "Чий інвентар ви хочете подивитись?");
                            break;
                    }
                }

                if (callbackQuery.Data.StartsWith("itemMaster_"))
                {
                    _logger.LogInformation("Processing itemMaster command for user {UserId}", chatId);
                    StateBotMSG.TryAdd(chatId, 3);
                    userStateInventory.TryAdd(chatId, "masterStart");

                    long.TryParse(recipientId, out long choosenId);
                    choosedUser = choosenId;


                    await _inventoryMasterCommand.MasterStartVoid(botClient, message, cancellationToken, choosenId);
                }

                if (callbackQuery.Data.StartsWith("checkMaster_"))
                {
                    _logger.LogInformation("Processing checkMaster command for user {UserId}", chatId);
                    long.TryParse(recipientId, out long choosenUserId);

                    _logger.LogInformation("Processing checkMaster command for choosed user: {user}", choosenUserId);

                    if (choosenUserId != 0)
                    {
                        if (!UserInventory.CheckifExist(choosenUserId))
                        {
                            await SendInventoryDataToMaster(botClient, cancellationToken, choosenUserId, "Помилка інвентаря: ");
                            return;
                        }

                        string name = UserData.DataBaseValueReader<string>(choosenUserId, "Name");
                        string weight = UserInventory.GetAllInventoryWeight(choosenUserId).ToString();


                        string msgText = $"{name}\n Загальна вага: {weight}\n\n";

                        await SendInventoryDataToMaster(botClient, cancellationToken, choosenUserId, msgText);
                    }
                    else
                    {
                        _logger.LogWarning("Invalid user ID in checkMaster command");
                    }
                }

                if (callbackQuery.Data.StartsWith("setMaster_"))
                {
                    _logger.LogInformation("Processing setMaster command for user {UserId}", chatId);
                    long.TryParse(recipientId, out long choosenUserId);
                    await StartSetObjectValue(botClient, cancellationToken, choosenUserId, true);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing inventory callback for user {UserId}", chatId);
                await botClient.SendTextMessageAsync(chatId, "Сталася помилка під час обробки команди інвентаря. Спробуйте ще раз.", cancellationToken: cancellationToken);
            }
        }

        private async Task SendInventoryData(ITelegramBotClient botClient, CancellationToken cancellationToken, long chatId, string MsgText)
        {
            _logger.LogInformation("Sending inventory data to user {UserId}", chatId);
            await botClient.SendTextMessageAsync(chatId, $"{MsgText} {UserInventory.InventoryDataBaseReader(chatId)}", cancellationToken: cancellationToken);
        }

        private async Task SendInventoryDataToMaster(ITelegramBotClient botClient, CancellationToken cancellationToken, long chatId, string MsgText)
        {
            _logger.LogInformation("Sending inventory data to master for user {UserId}", chatId);
            await botClient.SendTextMessageAsync(masterID, $"{MsgText} {UserInventory.GetUsersFromDatabaseMaster(chatId)}", cancellationToken: cancellationToken);
        }
    }
}
