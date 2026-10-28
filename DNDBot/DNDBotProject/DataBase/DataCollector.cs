using DNDBot.BotSystem;
using DNDBot.CommandSystem;
using DNDBot.DataBase;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace DNDBot
{
    ///






    public class DataCollector : BotBodyRealisation
    {
        private static ILogger<DataCollector> _logger = NullLogger<DataCollector>.Instance;

        public static void ConfigureLogger(ILogger<DataCollector> logger)
        {
            _logger = logger ?? NullLogger<DataCollector>.Instance;
        }


         
        private static readonly List<(string FieldName, string Prompt, bool IsString)> DataFields = new()
        {
            ("PlatinumCoin", "Ваша кількість платинових монет:", false),
            ("GoldenCoin", "Ваша кількість золотих монет:", false),
            ("SilverCoin", "Ваша кількість срібних монет:", false),
            ("CopperCoin", "Ваша кількість мідних монет:", false),

            ("Level", "Введіть ваш рівень:", false),
            ("XP", "Введіть ваш досвід:", false),
            ("Name", "Введіть своє ім'я:", true),
            ("Armor", "Введіть ваш клас обладунків:", false),
            ("Health", "Введіть ваше здоров'я:", false),
            ("Charisma", "Введіть ваш модифікатор харизми:", false),
            ("Wisdom", "Введіть ваш модифікатор мудрості:", false),
            ("Intelligence", "Введіть ваш модифікатор інтелекту:", false),
            ("Stamina", "Введіть ваш модифікатор витривалості:", false),
            ("Agility", "Введіть ваш модифікатор спритності:", false),
            ("Strenght", "Введіть ваш модифікатор сили:", false),
            ("StrenghtForWeightMathf", "Введіть вашу силу — НЕ МОДИФІКАТОР, а значення!", false)
        };

         
         
         
        private static readonly List<(string FieldName, string Prompt, bool IsString)> MagicFields = new()
        {
            ("MagicLevel", "Рівень магії:", false),
            ("MagicXP", "Досвід магії:", false),
            ("MagicArmour", "Магічний захист(магічний обладунок):", false),

            ("BaseMana", "Базова кількість мани:", false),
            ("CurrentMana", "Поточна мана:", false),
            ("Stability", "Стабільність:", false),
            ("Exhaustion", "Виснаження:", false),

            ("ManaZoneRadius", "Радіус мана-зони:", false),
            ("ManaZoneControl", "Контроль мана-зони:", false),

            ("RegenTurn", "(Це є прямим процентуальним додаванням до вашої регенерації, якщо не знаєте спитайте майстра) Реген мани за хід:", false),
            ("RegenAfterFight", "(Це є прямим процентуальним додаванням до вашої регенерації, якщо не знаєте спитайте майстра) Реген після бою:", false),
            ("Resonance", "Резонанс (текст):", true)
        };

         
        private const string CB_MENU = "profile_menu";
        private const string CB_NEW = "profile_new";
        private const string CB_SWITCH = "profile_switch";
        private const string CB_SET_PREFIX = "profile_set_";
        private const string CB_DELETE = "profile_delete_";
        private const string CB_CNFDELETE = "profile_cnfdelete_";
         
         
         

        public static async Task CollectData(
            ITelegramBotClient botClient,
            Update update,
            CancellationToken cancellationToken,
            Message message,
            ReplyKeyboardMarkup replyKeyboard)
        {
             
            if (update != null && update.Type == UpdateType.CallbackQuery && update.CallbackQuery != null)
            {
                await HandleProfileCallback(botClient, update.CallbackQuery, cancellationToken);
                return;
            }

            if (message == null || message.From == null)
                return;

            long userId = message.From.Id;
            bool isPrivateChat = message.Chat?.Type == ChatType.Private;
            InitializeUserState(userId);

            ClearDataIfRequested(botClient, message, cancellationToken);
            ClearDatabaseIfRequested(message);

            bool hasCollectionFlow = userStateDataCollector.TryGetValue(userId, out string existingCollectorState) &&
                                     !string.IsNullOrWhiteSpace(existingCollectorState);
            bool hasChangeFlow = userStateDataChanger.TryGetValue(userId, out string existingChangeState) &&
                                 !string.IsNullOrWhiteSpace(existingChangeState);

            if (!isPrivateChat && (hasCollectionFlow || hasChangeFlow))
            {
                await NotifyPrivateOnlyFlow(botClient, message, cancellationToken);
                return;
            }

             
            if (message.Text == "Керувати інформацією бота")
            {
                if (!isPrivateChat)
                {
                    await botClient.SendTextMessageAsync(
                        message.Chat.Id,
                        "Керування особистими даними перенесено у приватний чат з ботом.",
                        cancellationToken: cancellationToken);
                }

                long targetChatId = isPrivateChat ? message.Chat.Id : userId;

                 
                int activeCharId = await EnsureDefaultProfile(botClient, targetChatId, userId, cancellationToken);

                 
                bool hasActiveData = UserData.CheckifExist(userId, activeCharId);

                if (!hasActiveData)
                {
                    await NotifyUserDataInput(botClient, targetChatId, userId, cancellationToken);

                    userStateDataCollector[userId] = "start";

                    string charName = UserData.GetCharacterName(userId, activeCharId);


                     
                    await botClient.SendTextMessageAsync(
                        targetChatId,
                        $"Після заповнення зможеш додати ще анкету або перемкнутись.\nАктивна анкета: *{charName}*",
                        parseMode: ParseMode.Markdown,
                        replyMarkup: BuildQuickProfilesKeyboard(),
                        cancellationToken: cancellationToken);

                    await StartDataCollection(botClient, targetChatId, userId, cancellationToken);
                    return;
                }

                 
                await ShowProfilesMenu(botClient, targetChatId, userId, cancellationToken);
                return;
            }

             
            if (userStateDataCollector.TryGetValue(userId, out string state))
            {
                if (state == "start")
                    await StartDataCollection(botClient, message.Chat.Id, userId, cancellationToken);
                else if (state == "paste")
                    await ContinueDataCollection(botClient, message, cancellationToken);
                else if (state == "magic")
                    await ContinueMagicDataCollection(botClient, message, cancellationToken);
            }

             
            if (userStateDataChanger.TryGetValue(userId, out string changeState))
            {
                if (changeState == "StartUpdateDataValue")
                    await DataChanger.StartUpdateDataVoid(botClient, message, cancellationToken, userId);
                else if (changeState == "UpdateDataValue")
                    await DataChanger.PasteUpdateDataVoid(botClient, message, cancellationToken, userId);
            }
        }

        private static async Task NotifyPrivateOnlyFlow(
            ITelegramBotClient botClient,
            Message message,
            CancellationToken cancellationToken)
        {
            if (message?.From == null || message.Chat == null)
                return;

            await botClient.SendTextMessageAsync(
                message.Chat.Id,
                "Цей процес працює тільки у приватному чаті з ботом.",
                cancellationToken: cancellationToken);

            await botClient.SendTextMessageAsync(
                message.From.Id,
                "Продовжуй, будь ласка, у приватному чаті з ботом.",
                cancellationToken: cancellationToken);
        }

        private static void InitializeUserState(long userId)
        {
            if (!OldMessages.ContainsKey(userId))
                OldMessages[userId] = new List<int>();
        }

        private static async void ClearDataIfRequested(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
        {
            if (message?.From == null)
                return;

            if (message.Text == "Очистити інформацію: ПІДТВЕРДЖУЮ")
                await botClient.SendTextMessageAsync(message.Chat.Id, UserData.ClearUserData(message.From.Id), cancellationToken: cancellationToken);
        }

        private static void ClearDatabaseIfRequested(Message message)
        {
            if (message?.From == null)
                return;

            if (message.Text == "Очистити базу даних" && message.From.Id == ownerID)
            {
                userStateDataCollector[message.From.Id] = "";
                UserData.ClearAllData(true);
            }
        }

         
         
         

        ///







        private static async Task<int> EnsureDefaultProfile(ITelegramBotClient botClient, long chatId, long userId, CancellationToken ct)
        {
            try
            {
                 
                return UserData.GetActiveCharacterId(userId);
            }
            catch
            {
                try
                {
                     
                    return UserData.EnsureDefaultCharacter(userId);
                }
                catch (Exception ex)
                {
                    await botClient.SendTextMessageAsync(
                        chatId,
                        $"Не можу ініціалізувати активну анкету. Помилка: {ex.Message}",
                        cancellationToken: ct);

                     
                    return 0;
                }
            }
        }

        private static InlineKeyboardMarkup BuildQuickProfilesKeyboard()
        {
             
            return InlineKeyboardsClass.CreateInlineKeyboard(
                new[] { ("➕ Додати ще анкету", CB_NEW) },
                new[] { ("🔁 Перемкнути анкету", CB_SWITCH) }
            );
        }

        private static InlineKeyboardMarkup BuildQuickProfilesDELETEConfirmationKeyboard(long userId)
        {
             
            return InlineKeyboardsClass.CreateInlineKeyboard(
                new[] { ("🗑 ВИДАЛИТИ АНКЕТУ", CB_CNFDELETE + + UserData.GetActiveCharacterId(userId)) }
            );
        }

        private static InlineKeyboardMarkup BuildProfilesMenuKeyboard(long userId)
        {
            return InlineKeyboardsClass.CreateInlineKeyboard(
                new[] { ("➕ Додати ще анкету", CB_NEW) },
                new[] { ("🔁 Перемкнути анкету", CB_SWITCH) },
                new[] { ("🗑 Видалити активну анкету?", CB_DELETE + UserData.GetActiveCharacterId(userId)) }

            );
        }

        private static async Task ShowProfilesMenu(ITelegramBotClient botClient, long chatId, long userId, CancellationToken ct)
        {
            int activeCharId = await EnsureDefaultProfile(botClient, chatId, userId, ct);
            bool hasActiveData = UserData.CheckifExist(userId, activeCharId);

            string charName = UserData.GetCharacterName(userId, activeCharId);

            string header = hasActiveData
                ? $"Активна анкета: *{charName}* `#{activeCharId}` (є дані)\nМожеш додати ще або перемкнутися:"
                : $"Активна анкета: *{charName}* `#{activeCharId}` (даних нема)\nНатисни нижче:";


            await botClient.SendTextMessageAsync(
                chatId,
                header,
                parseMode: ParseMode.Markdown,
                replyMarkup: BuildProfilesMenuKeyboard(userId),
                cancellationToken: ct);
        }

        public static async Task HandleProfileCallback(ITelegramBotClient botClient, CallbackQuery cb, CancellationToken ct)
        {
            if (cb.From == null)
                return;

            long userId = cb.From.Id;
            InitializeUserState(userId);

            string data = cb.Data ?? string.Empty;
            long chatId = cb.Message?.Chat?.Id ?? userId;

            try
            {
                await botClient.AnswerCallbackQueryAsync(cb.Id, cancellationToken: ct);
            }
            catch (ApiRequestException ex) when ((ex.Message ?? string.Empty).Contains("query is too old", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogDebug(
                    "Ignoring outdated profile callback answer. CallbackId={CallbackId}, UserId={UserId}, Data={CallbackData}",
                    cb.Id,
                    userId,
                    data);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Failed to answer profile callback. CallbackId={CallbackId}, UserId={UserId}, Data={CallbackData}",
                    cb.Id,
                    userId,
                    data);
            }

            if (data == CB_MENU)
            {
                await ShowProfilesMenu(botClient, chatId, userId, ct);
                return;
            }

            if (data == CB_NEW)
            {
                await EnsureDefaultProfile(botClient, chatId, userId, ct);

                int newCharId;
                try
                {
                     
                     
                    var existing = UserData.GetUserCharacters(userId);
                    string name = $"Анкета {existing.Count + 1}";
                    newCharId = UserData.CreateNewCharacter(userId, name);
                }
                catch (Exception ex)
                {
                    await botClient.SendTextMessageAsync(chatId,
                        $"Не можу створити нову анкету: {ex.Message}",
                        cancellationToken: ct);
                    return;
                }

                await botClient.SendTextMessageAsync(
                    chatId,
                    $"Створено нову анкету: *{newCharId}*. Вона тепер активна. Заповнюємо.",
                    parseMode: ParseMode.Markdown,
                    cancellationToken: ct);

                await NotifyUserDataInput(botClient, chatId, userId, ct);
                userStateDataCollector[userId] = "start";
                await StartDataCollection(botClient, chatId, userId, ct);
                return;
            }

            if (data == CB_SWITCH)
            {
                await EnsureDefaultProfile(botClient, chatId, userId, ct);

                List<(int CharacterId, string Name, bool IsActive)> chars;
                try
                {
                     
                    chars = UserData.GetUserCharacters(userId);
                }
                catch (Exception ex)
                {
                    await botClient.SendTextMessageAsync(chatId,
                        $"Не можу отримати список анкет: {ex.Message}",
                        cancellationToken: ct);
                    return;
                }

                if (chars.Count == 0)
                {
                    await botClient.SendTextMessageAsync(
                        chatId,
                        "У тебе ще нема жодної анкети. Натисни *➕ Додати ще анкету*.",
                        parseMode: ParseMode.Markdown,
                        cancellationToken: ct);
                    return;
                }

                var rows = new List<(string text, string callback)[]>();
                foreach (var c in chars)
                {
                    string title = string.IsNullOrWhiteSpace(c.Name)
                        ? $"Анкета {c.CharacterId}"
                        : $"{c.Name} (#{c.CharacterId})";

                    if (c.IsActive) title = "✅ " + title;

                    rows.Add(new[] { (title, CB_SET_PREFIX + c.CharacterId) });
                }

                rows.Add(new[] { ("⬅️ Назад", CB_MENU) });

                await botClient.SendTextMessageAsync(
                    chatId,
                    "Вибери анкету, яка стане активною:",
                    replyMarkup: InlineKeyboardsClass.CreateInlineKeyboard(rows.ToArray()),
                    cancellationToken: ct);
                return;
            }

            if (data.StartsWith(CB_SET_PREFIX, StringComparison.Ordinal))
            {
                string part = data.Substring(CB_SET_PREFIX.Length);
                if (!int.TryParse(part, out int targetCharId))
                {
                    await botClient.SendTextMessageAsync(chatId, "Невірний CharacterId.", cancellationToken: ct);
                    return;
                }

                try
                {
                    UserData.SetActiveCharacterId(userId, targetCharId);
                }
                catch (Exception ex)
                {
                    await botClient.SendTextMessageAsync(chatId,
                        $"Не можу перемкнути анкету: {ex.Message}",
                        cancellationToken: ct);
                    return;
                }

                await botClient.SendTextMessageAsync(
                    chatId,
                    $"Готово. Активна анкета тепер: *{targetCharId}*.",
                    parseMode: ParseMode.Markdown,
                    cancellationToken: ct);

                await botClient.SendTextMessageAsync(userId, $"Ваші модифікатори та статистика: \n\n" + GetCollectMultiplayer(userId), cancellationToken: ct);

                return;
            }
            if (data.StartsWith(CB_DELETE))
            {
                await botClient.SendTextMessageAsync(
                chatId,
                "Якщо ви впевненні і хочете видалити ЦЮ АНКЕТУ, натисність на кнопку далі:",
                replyMarkup: BuildQuickProfilesDELETEConfirmationKeyboard(userId),
                cancellationToken: ct);
            }
            if (data.StartsWith(CB_CNFDELETE))
            {
                int charId = int.Parse(data.Replace(CB_CNFDELETE, ""));

                try
                {
                    UserData.DeleteCharacter(userId, charId);
                    await botClient.SendTextMessageAsync(
                        chatId,
                        $"Анкету #{charId} видалено.",
                        cancellationToken: ct);

                    await EnsureDefaultProfile(botClient, chatId, userId, ct);

                    List<(int CharacterId, string Name, bool IsActive)> chars;
                    try
                    {
                         
                        chars = UserData.GetUserCharacters(userId);
                    }
                    catch (Exception ex)
                    {
                        await botClient.SendTextMessageAsync(chatId,
                            $"Не можу отримати список анкет: {ex.Message}",
                            cancellationToken: ct);
                        return;
                    }

                    if (chars.Count == 0)
                    {
                        await botClient.SendTextMessageAsync(
                            chatId,
                            "У тебе ще нема жодної анкети. Натисни *➕ Додати ще анкету*.",
                            parseMode: ParseMode.Markdown,
                            cancellationToken: ct);
                        return;
                    }

                    var rows = new List<(string text, string callback)[]>();
                    foreach (var c in chars)
                    {
                        string title = string.IsNullOrWhiteSpace(c.Name)
                            ? $"Анкета {c.CharacterId}"
                            : $"{c.Name} (#{c.CharacterId})";

                        if (c.IsActive) title = "✅ " + title;

                        rows.Add(new[] { (title, CB_SET_PREFIX + c.CharacterId) });
                    }

                    rows.Add(new[] { ("⬅️ Назад", CB_MENU) });

                    await botClient.SendTextMessageAsync(
                        chatId,
                        "Вибери анкету, яка стане активною:",
                        replyMarkup: InlineKeyboardsClass.CreateInlineKeyboard(rows.ToArray()),
                        cancellationToken: ct);
                    return;
                }
                catch (Exception ex)
                {
                    await botClient.SendTextMessageAsync(
                        chatId,
                        $"❌ {ex.Message}",
                        cancellationToken: ct);
                }
                return;
            }

        }

        public static string GetCollectMultiplayer(long userId)
        {
            return UserData.DataBaseReader(userId);
        }

         
         
         

        private static async Task NotifyUserDataInput(ITelegramBotClient botClient, long chatId, long userId, CancellationToken cancellationToken)
        {
            int messageId = (await botClient.SendTextMessageAsync(
                chatId,
                "*УВАГА! Дані будуть збережені. Якщо зробите помилку, використовуйте 'Повернутись'.*",
                parseMode: ParseMode.Markdown,
                cancellationToken: cancellationToken)).MessageId;

            OldMessages[userId].Add(messageId);
        }

        private static async Task StartDataCollection(ITelegramBotClient botClient, long chatId, long userId, CancellationToken cancellationToken)
        {
            int activeCharId = UserData.GetActiveCharacterId(userId);

            UserDataClassArray[userId] = new UserDataClass
            {
                Id = userId,
                CharacterId = activeCharId
            };

            StateBotMSG[userId] = DataFields.Count - 1;
            userStateDataCollector[userId] = "paste";

            int msgId = (await botClient.SendTextMessageAsync(
                chatId,
                DataFields[(int)StateBotMSG[userId]].Prompt,
                cancellationToken: cancellationToken
            )).MessageId;

            OldMessages[userId].Add(msgId);
        }

        private static async Task StartMagicCollection(ITelegramBotClient botClient, long chatId, long userId, CancellationToken cancellationToken)
        {
            int activeCharId = UserData.GetActiveCharacterId(userId);

            MagicDataBuffer[userId] = new ManaDataClass
            {
                UserId = userId,
                CharacterId = activeCharId
            };

            StateBotMSG[userId] = MagicFields.Count - 1;
            userStateDataCollector[userId] = "magic";

            int msgId = (await botClient.SendTextMessageAsync(
                chatId,
                MagicFields[(int)StateBotMSG[userId]].Prompt,
                cancellationToken: cancellationToken
            )).MessageId;

            OldMessages[userId].Add(msgId);
        }
        public static async Task ContinueMagicDataCollection(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
        {
            if (message?.From == null)
                return;

            long userId = message.From.Id;
            string input = (message.Text ?? string.Empty).Trim();
            int resultInt = 0;

            OldMessages[userId].Add(message.MessageId);

            int index = (int)StateBotMSG[userId];

            if (input != "Повернутись" && index < MagicFields.Count)
            {
                var field = MagicFields[index];
                var magic = MagicDataBuffer[userId];

                if (!field.IsString)
                {
                    if (!int.TryParse(input, out resultInt))
                    {
                        await botClient.SendTextMessageAsync(message.Chat.Id, "Помилка: введіть число.", cancellationToken: cancellationToken);
                        return;
                    }
                    AssignMagicData(magic, field.FieldName, resultInt);
                }
                else
                {
                    AssignMagicData(magic, field.FieldName, 0, input);
                }

                StateBotMSG[userId]--;
            }
            else
            {
                StateBotMSG[userId]++;
            }

            if (StateBotMSG[userId] < 0)
            {
                await SaveUserData(botClient, userId, cancellationToken);
                await SaveMagicData(botClient, userId, cancellationToken);
                return;
            }

            int currentIndex = (int)StateBotMSG[userId];
            string prompt = MagicFields[currentIndex].Prompt;

            int msgId1 = (await botClient.SendTextMessageAsync(
                message.Chat.Id,
                prompt,
                cancellationToken: cancellationToken
            )).MessageId;

            OldMessages[userId].Add(msgId1);
        }

        private static async Task ContinueDataCollection(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
        {
            if (message?.From == null)
                return;

            long userId = message.From.Id;
            string input = (message.Text ?? string.Empty).Trim();
            int resultInt = 0;

            OldMessages[userId].Add(message.MessageId);

            int index = (int)StateBotMSG[userId];

            if (input != "Повернутись" && index < DataFields.Count)
            {
                var field = DataFields[index];
                if (!field.IsString)
                {
                    if (!int.TryParse(input, out resultInt))
                    {
                        await botClient.SendTextMessageAsync(message.Chat.Id, "Помилка: введіть число.", cancellationToken: cancellationToken);
                        return;
                    }
                    AssignDataToUser(userId, field.FieldName, resultInt);
                }
                else
                {
                    AssignDataToUser(userId, field.FieldName, resultString: input);
                }

                StateBotMSG[userId]--;
            }
            else
            {
                StateBotMSG[userId]++;
            }

            if (StateBotMSG[userId] < 0)
            {
                 
                await botClient.SendTextMessageAsync(
                    message.Chat.Id,
                    "🔮 Зараз буде збір даних для вашої мани. Усе окрім резонансу є цілим числом.",
                    cancellationToken: cancellationToken
                );

                await StartMagicCollection(botClient, message.Chat.Id, userId, cancellationToken);
                return;
            }

            int currentIndex = (int)StateBotMSG[userId];
            string prompt = DataFields[currentIndex].Prompt;

            int msgId1 = (await botClient.SendTextMessageAsync(
                message.Chat.Id,
                prompt,
                cancellationToken: cancellationToken
            )).MessageId;

            OldMessages[userId].Add(msgId1);
        }

        private static void AssignDataToUser(long userId, string fieldName, int resultInt = 0, string resultString = "")
        {
            var user = UserDataClassArray[userId];
            var property = typeof(UserDataClass).GetProperty(fieldName);
            if (property == null) return;

            object value = property.PropertyType == typeof(int) ? resultInt : resultString;
            property.SetValue(user, value);
        }

        private static async Task SaveUserData(ITelegramBotClient botClient, long userId, CancellationToken cancellationToken)
        {
            StateBotMSG[userId] = -1;
            userStateDataCollector[userId] = "";
            DeleteAllOldMessages(botClient, cancellationToken, userId);

            UserDataClass user = UserDataClassArray[userId];
            user.Level = user.Level <= 0 ? 1 : user.Level;
            user.Level = user.Level >= 20 ? 20 : user.Level;
            user.MaxHP = user.Health;

            await botClient.SendTextMessageAsync(userId, UserData.AddUserToDataBase(user), cancellationToken: cancellationToken);
        }

        private static void AssignMagicData(ManaDataClass magic, string fieldName, int resultInt = 0, string resultString = "")
        {
            var property = typeof(ManaDataClass).GetProperty(fieldName);
            if (property == null) return;

            object value = null;
            if (property.PropertyType == typeof(int))
                value = resultInt;
            else if (property.PropertyType == typeof(double))
            {
                 
                if (double.TryParse(resultString.Replace(',', '.'), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double dbl))
                    value = dbl;
                else
                    value = 0.0;  
            }
            else
                value = resultString;

            property.SetValue(magic, value);
        }


        public static async Task SaveMagicData(ITelegramBotClient botClient, long userId, CancellationToken cancellationToken)
        {
            StateBotMSG[userId] = -1;
            userStateDataCollector[userId] = "";
            DeleteAllOldMessages(botClient, cancellationToken, userId);

            ManaDataClass magic = MagicDataBuffer[userId];

            magic.MaxMana = ManaData.GetEffectiveMaxMana(magic.BaseMana, magic.MagicLevel);

            try
            {
                ManaData.AddMagicStatsToDatabase(magic);
                await botClient.SendTextMessageAsync(userId, "✅ Магічні дані збережено.", cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                await botClient.SendTextMessageAsync(userId, $"❌ Помилка збереження магії: {ex.Message}", cancellationToken: cancellationToken);
            }

            MagicDataBuffer.Remove(userId);


        }
    }
}
