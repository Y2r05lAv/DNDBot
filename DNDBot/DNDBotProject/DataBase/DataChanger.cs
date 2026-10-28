using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace DNDBot.DataBase
{
    public class DataChanger : DataCollector
    {
        public static Dictionary<long, string> userStateChangingDataFromDB = new Dictionary<long, string>();
        public static Dictionary<long, int> userStateChangingCharacterId = new Dictionary<long, int>();

        private static readonly HashSet<string> MagicStatsKeys = new()
        {
            "MagicArmour",
            "MaxMana",
            "BaseMana",
            "CurrentMana",
            "Stability",
            "Exhaustion",
            "ManaZoneRadius",
            "ManaZoneControl",
            "RegenTurn",
            "RegenAfterFight"
        };


        public static async Task StartUpdateDataVoid(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken, long userId)
        {
            Console.WriteLine("StartUpdateDataVoid");

            userStateDataChanger.TryAdd(userId, "");
            StateBotMSG.TryAdd(userId, 0);
            OldMessages.TryAdd(userId, new List<int>());

            StateBotMSG[userId] = 1;
            userStateDataChanger[userId] = "UpdateDataValue";
            userStateChangingCharacterId[userId] = UserData.GetActiveCharacterId(userId);

            await PasteUpdateDataVoid(botClient, message: null, cancellationToken, userId);
        }

        public static async Task PasteUpdateDataVoid(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken, long userId)
        {
            Console.WriteLine("PasteUpdateDataVoid DataChanger cycle: " + StateBotMSG[userId]);
            switch (StateBotMSG[userId])
            {
                case 1:
                {
                    if (!userStateChangingDataFromDB.TryGetValue(userId, out string statToChange) || string.IsNullOrWhiteSpace(statToChange))
                    {
                        await botClient.SendTextMessageAsync(userId, "Не вдалося визначити параметр для оновлення. Спробуйте ще раз.", cancellationToken: cancellationToken);
                        userStateDataChanger[userId] = "";
                        StateBotMSG[userId] = 0;
                        userStateChangingCharacterId.Remove(userId);
                        return;
                    }

                    int characterId = userStateChangingCharacterId.TryGetValue(userId, out int selectedCharacterId) && selectedCharacterId > 0
                        ? selectedCharacterId
                        : UserData.GetActiveCharacterId(userId);

                    OldMessages[userId].Add((await botClient.SendTextMessageAsync(
                        userId,
                        $"Напишіть нове значення для: {ReturnInventoryStringData(statToChange)} (анкета #{characterId})",
                        cancellationToken: cancellationToken)).MessageId);

                    StateBotMSG[userId] = 0;
                    return;
                }
                case 0:
                {
                    if (message == null || string.IsNullOrWhiteSpace(message.Text))
                        return;

                    if (!int.TryParse((message.Text ?? string.Empty).Trim(), out int changeValue))
                    {
                        await botClient.SendTextMessageAsync(userId, "Невірне значення! Повинно бути числове значення.", cancellationToken: cancellationToken);
                        return;
                    }

                    if (!userStateChangingDataFromDB.TryGetValue(userId, out string statKey) || string.IsNullOrWhiteSpace(statKey))
                    {
                        await botClient.SendTextMessageAsync(userId, "Не вдалося визначити параметр для оновлення. Спробуйте ще раз.", cancellationToken: cancellationToken);
                        userStateDataChanger[userId] = "";
                        StateBotMSG[userId] = 0;
                        userStateChangingCharacterId.Remove(userId);
                        return;
                    }

                    int characterId = userStateChangingCharacterId.TryGetValue(userId, out int targetCharacterId) && targetCharacterId > 0
                        ? targetCharacterId
                        : UserData.GetActiveCharacterId(userId);

                    if (MagicStatsKeys.Contains(statKey))
                    {
                        ManaData.UpdateMagicStat(userId, characterId, statKey, changeValue);
                    }
                    else
                    {
                        UserData.UpdateDataBaseValue(statKey, userId, characterId, changeValue);
                    }

                    OldMessages[userId].Add(message.MessageId);

                    DeleteAllOldMessages(botClient, cancellationToken, userId);

                    await botClient.SendTextMessageAsync(userId, $"Ви оновили дані для анкети #{characterId}! Ваша інформація: \n\n{UserData.DataBaseReader(userId, characterId)}", cancellationToken: cancellationToken);
                    userStateChangingDataFromDB[userId] = "";
                    userStateDataChanger[userId] = "";
                    userStateChangingCharacterId.Remove(userId);
                    StateBotMSG[userId] = 0;
                    return;
                }
            }
        }

        private static string ReturnInventoryStringData(string InventoryStringData)
        {
            return InventoryStringData switch
            {
                 
                "Strenght" => "Модифікатор сили",
                "Agility" => "Модифікатор спритності",
                "Stamina" => "Модифікатор витривалості",
                "Intelligence" => "Модифікатор інтелекту",
                "Wisdom" => "Модифікатор мудрості",
                "Charisma" => "Модифікатор харизми",
                "MaxHP" => "Максимальне здоров'я",
                "TempHP" => "Тимчасові хіти",
                "TempMaxHPReduction" => "Тимчасове зниження HP",
                "Armor" => "Клас обладунків",

                 
                "MagicArmour" => "Магічна броня",
                "MaxMana" => "Максимальна мана",
                "BaseMana" => "Базова мана",
                "CurrentMana" => "Поточна мана",
                "Stability" => "Стабільність мани",
                "Exhaustion" => "Магічне виснаження",
                "ManaZoneRadius" => "Радіус мана-зони",
                "ManaZoneControl" => "Контроль мана-зони",
                "RegenTurn" => "Регенерація мани за хід",
                "RegenAfterFight" => "Регенерація після бою",

                _ => $"{InventoryStringData}.Exeption: Not right string value"
            };
        }



        public static InlineKeyboardMarkup GetChangingValueForUser()
        {
            InlineKeyboardMarkup inlineKeyboard;

            inlineKeyboard = new InlineKeyboardMarkup(new[]
            {
                new InlineKeyboardButton[]
                {
                    InlineKeyboardButton.WithCallbackData("Сила", "changeValueUserData_Strenght"),
                    InlineKeyboardButton.WithCallbackData("Спритність", "changeValueUserData_Agility"),
                },
                new InlineKeyboardButton[]
                {
                    InlineKeyboardButton.WithCallbackData("Витривалість", "changeValueUserData_Stamina"),
                    InlineKeyboardButton.WithCallbackData("Інтелект", "changeValueUserData_Intelligence"),
                },
                new InlineKeyboardButton[]
                {
                    InlineKeyboardButton.WithCallbackData("Мудрість", "changeValueUserData_Wisdom"),
                    InlineKeyboardButton.WithCallbackData("Харизма", "changeValueUserData_Charisma"),
                },
                new InlineKeyboardButton[]
                {
                    InlineKeyboardButton.WithCallbackData("Максимальне здоров'я", "changeValueUserData_MaxHP"),
                    InlineKeyboardButton.WithCallbackData("Класс обладунків", "changeValueUserData_Armor"),
                },
                new InlineKeyboardButton[]
                {
                    InlineKeyboardButton.WithCallbackData("Тимчасові хіти", "changeValueUserData_TempHP"),
                    InlineKeyboardButton.WithCallbackData("Тимчас. зниження HP", "changeValueUserData_TempMaxHPReduction"),
                },
                new InlineKeyboardButton[]
                {
                    InlineKeyboardButton.WithCallbackData("Магічна броня", "changeValueUserData_MagicArmour"),
                    InlineKeyboardButton.WithCallbackData("Макс. мана", "changeValueUserData_MaxMana"),
                },
                new InlineKeyboardButton[]
                {
                    InlineKeyboardButton.WithCallbackData("Поточна мана", "changeValueUserData_CurrentMana"),
                    InlineKeyboardButton.WithCallbackData("Стабільність", "changeValueUserData_Stability"),
                },
                new InlineKeyboardButton[]
                {
                    InlineKeyboardButton.WithCallbackData("Виснаження", "changeValueUserData_Exhaustion"),
                    InlineKeyboardButton.WithCallbackData("Радіус мана-зони", "changeValueUserData_ManaZoneRadius"),
                },
                new InlineKeyboardButton[]
                {
                    InlineKeyboardButton.WithCallbackData("Контроль мана-зони", "changeValueUserData_ManaZoneControl"),
                    InlineKeyboardButton.WithCallbackData("Базова мана", "changeValueUserData_BaseMana"),

                },
                new InlineKeyboardButton[]
                {
                    InlineKeyboardButton.WithCallbackData("Реген за хід", "changeValueUserData_RegenTurn"),
                    InlineKeyboardButton.WithCallbackData("Реген після бою", "changeValueUserData_RegenAfterFight"),
                }
            });

            return inlineKeyboard;
        }


        public static async Task HandleCallbackQ(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken, Message message, CallbackQuery callbackQuery)
        {
            try
            {
                if (callbackQuery?.Data == null)
                    return;

                 
                if (callbackQuery.Data.StartsWith("changeValueUserData_"))
                {
                    const string callbackPrefix = "changeValueUserData_";
                    string infoAfterSplit = callbackQuery.Data.Substring(callbackPrefix.Length);
                    if (string.IsNullOrWhiteSpace(infoAfterSplit))
                        return;

                    long chatId = callbackQuery.From.Id;

                    if (!userStateChangingDataFromDB.ContainsKey(chatId))
                        userStateChangingDataFromDB.Add(chatId, infoAfterSplit);
                    else
                        userStateChangingDataFromDB[chatId] = infoAfterSplit;

                    await StartUpdateDataVoid(botClient, message, cancellationToken, chatId);


                }


                await botClient.AnswerCallbackQueryAsync(callbackQuery.Id);
            }
            catch (ApiRequestException ex) when (ex.Message.Contains("query is too old"))
            {
                Console.WriteLine("Запрос устарел, игнорируем.");
            }
        }
    }
}
