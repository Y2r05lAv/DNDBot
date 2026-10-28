using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace DNDBot.Spells
{
    public class SpellCollector : BotBodyRealisation
    {
         
        private static readonly string[] DialogNumber = new string[]
        {
            "Введіть опис:",                            
            "Введіть модифікатор:",                     
            "Скільки кубіків урону?",                   
            "Виберіть кубік:",                          
            "Введіть тривалість:",                      
            "Введіть відстань:",                        
            "Введіть тип шкоди (коляча, магічна…):",    
            "Введіть тип (магія, предмет…):",           
            "Скільки мани потребує ефект?",             
            "Введіть ім'я:"                             
        };

         
        public static Dictionary<long, string> userStateSpells = new Dictionary<long, string> { { 0, "" } };

         
         
        private static readonly Dictionary<long, List<string>> DraftParts = new() { { 0, new List<string>() } };

         
        private static readonly Dictionary<long, string> SelectedDie = new() { { 0, "" } };

        public static bool TryCancelSpellInput(long userId)
        {
            bool hasState = userStateSpells.TryGetValue(userId, out string state) && !string.IsNullOrWhiteSpace(state);
            bool hasDraft = DraftParts.ContainsKey(userId) || SelectedDie.ContainsKey(userId);

            if (!hasState && !hasDraft)
                return false;

            userStateSpells[userId] = "";
            DraftParts.Remove(userId);
            SelectedDie.Remove(userId);
            SpellDataClass.Remove(userId);
            StateBotMSG[userId] = 0;
            isProcessingAnotherProcess[userId] = false;
            return true;
        }

         

        private static InlineKeyboardMarkup YesNoKeyboard() => new(new[]
        {
            new []
            {
                InlineKeyboardButton.WithCallbackData("Так", "SC_HasDmg_Yes"),
                InlineKeyboardButton.WithCallbackData("Ні",  "SC_HasDmg_No"),
            }
        });

        public static InlineKeyboardMarkup DiceInlineKeyboard() => new(new[]
        {
            new []
            {
                InlineKeyboardButton.WithCallbackData("d4", "Dice_d4"),
                InlineKeyboardButton.WithCallbackData("d6", "Dice_d6"),
                InlineKeyboardButton.WithCallbackData("d8", "Dice_d8"),
            },
            new []
            {
                InlineKeyboardButton.WithCallbackData("d10", "Dice_d10"),
                InlineKeyboardButton.WithCallbackData("d12", "Dice_d12"),
                InlineKeyboardButton.WithCallbackData("d20", "Dice_d20"),
            },
        });

        public static InlineKeyboardMarkup DamageMoreOrDoneKeyboard() => new(new[]
        {
            new [] { InlineKeyboardButton.WithCallbackData("➕ Додати ще урон", "Damage_AddMore") },
            new [] { InlineKeyboardButton.WithCallbackData("✅ Готово", "Damage_Done") }
        });

        public static InlineKeyboardMarkup ModifierInlineKeyboard() => new(new[]
        {
            new InlineKeyboardButton[]
            {
                InlineKeyboardButton.WithCallbackData("Сила",         "Modifier_Strenght"),
                InlineKeyboardButton.WithCallbackData("Спритність",   "Modifier_Agility"),
            },
            new InlineKeyboardButton[]
            {
                InlineKeyboardButton.WithCallbackData("Витривалість", "Modifier_Stamina"),
                InlineKeyboardButton.WithCallbackData("Інтелект",     "Modifier_Intelligence"),
            },
            new InlineKeyboardButton[]
            {
                InlineKeyboardButton.WithCallbackData("Мудрість",     "Modifier_Wisdom"),
                InlineKeyboardButton.WithCallbackData("Харизма",      "Modifier_Charisma"),
            },
        });

         
        public static async Task SpellCreationTaskHandle(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken, Message message)
        {
            if (message?.From == null)
            {
                if (message?.Chat != null)
                    await botClient.SendTextMessageAsync(message.Chat.Id, "Помилка: не вдалося знайти користувача.", cancellationToken: cancellationToken);
                return;
            }
            long userId = message.From.Id;

            if (userStateSpells.ContainsKey(userId) && message != null)
                switch (userStateSpells[userId])
                {
                    case "start":
                        await StartVoidSpell(botClient, message, cancellationToken, userId);
                        break;
                    case "changeValue":
                        await PasteVoidAddSpell(botClient, message, cancellationToken, userId);
                        break;
                    case "startDeleting":
                        await DeleteSpell.StartVoid(botClient, message, cancellationToken, message.From.Id);
                        break;
                    case "pasteDeleting":
                        await DeleteSpell.PasteVoid(botClient, message, cancellationToken, message.From.Id);
                        break;
                    case "startActive":
                        await ActivateSpell.StartVoid(botClient, message, cancellationToken, message.From.Id);
                        break;
                    case "pasteActive":
                        break;
                    case "pastechangeSpell":
                        await EditSpellDamage.PasteVoidEditSpell(botClient, message, cancellationToken, userId);
                        break;
                }
        }

         
        public static async Task StartVoidSpell(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken, long userId)
        {
            isProcessingAnotherProcess[userId] = true;

            OldMessages.TryAdd(userId, new List<int>());
            userStateSpells.TryAdd(userId, "");

            SpellDataClass[userId] = new SpellDataClass { UserId = userId };
            DraftParts[userId] = new List<string>();
            SelectedDie[userId] = "";

             
            StateBotMSG[userId] = DialogNumber.Length;  

            userStateSpells[userId] = "changeValue";

             
            await PasteVoidAddSpell(botClient, message, cancellationToken, userId);
        }

         
        private static async Task PasteVoidAddSpell(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken, long userId)
        {
             
            if (!StateBotMSG.ContainsKey(userId)) StateBotMSG[userId] = DialogNumber.Length;
            if (!SpellDataClass.ContainsKey(userId)) SpellDataClass[userId] = new SpellDataClass { UserId = userId };

            string text = message?.Text?.Trim() ?? "";

             
             
             
            switch (StateBotMSG[userId])
            {
                case 10:  
                    OldMessages[userId].Add((await botClient.SendTextMessageAsync(userId, DialogNumber[9], cancellationToken: cancellationToken)).MessageId);
                    StateBotMSG[userId]--;
                    return;
                case 9:  
                    if (string.IsNullOrWhiteSpace(text))
                    {
                        OldMessages[userId].Add((await botClient.SendTextMessageAsync(userId, "Неправильний формат ім'я! Спробуйте ще раз!", cancellationToken: cancellationToken)).MessageId);
                        return;
                    }
                    SpellDataClass[userId].Name = text;
                    OldMessages[userId].Add((await botClient.SendTextMessageAsync(userId, DialogNumber[8], cancellationToken: cancellationToken)).MessageId);
                    StateBotMSG[userId]--;
                    return;

                case 8:  
                    if (!int.TryParse(text, out int m))
                    {
                        OldMessages[userId].Add((await botClient.SendTextMessageAsync(userId, "Вкажіть кількість мани.", cancellationToken: cancellationToken)).MessageId);
                        return;
                    }
                    SpellDataClass[userId].ManaCost = m;
                    OldMessages[userId].Add((await botClient.SendTextMessageAsync(userId, DialogNumber[7], cancellationToken: cancellationToken)).MessageId);
                    StateBotMSG[userId]--;
                    return;

                case 7:  
                    if (string.IsNullOrWhiteSpace(text))
                    {
                        OldMessages[userId].Add((await botClient.SendTextMessageAsync(userId, "Вкажіть тип (магія/предмет...)", cancellationToken: cancellationToken)).MessageId);
                        return;
                    }
                    SpellDataClass[userId].SpellType = text;
                    OldMessages[userId].Add((await botClient.SendTextMessageAsync(userId, DialogNumber[6], cancellationToken: cancellationToken)).MessageId);
                    StateBotMSG[userId]--;
                    return;

                case 6:  
                    if (string.IsNullOrWhiteSpace(text))
                    {
                        OldMessages[userId].Add((await botClient.SendTextMessageAsync(userId, "Вкажіть тип шкоди.", cancellationToken: cancellationToken)).MessageId);
                        return;
                    }
                    SpellDataClass[userId].TypeHarm = text;
                    OldMessages[userId].Add((await botClient.SendTextMessageAsync(userId, DialogNumber[5], cancellationToken: cancellationToken)).MessageId);
                    StateBotMSG[userId]--;
                    return;

                case 5:  
                    if (string.IsNullOrWhiteSpace(text))
                    {
                        OldMessages[userId].Add((await botClient.SendTextMessageAsync(userId, "Вкажіть відстань.", cancellationToken: cancellationToken)).MessageId);
                        return;
                    }
                    SpellDataClass[userId].Distance = text;
                    OldMessages[userId].Add((await botClient.SendTextMessageAsync(userId, DialogNumber[4], cancellationToken: cancellationToken)).MessageId);
                    StateBotMSG[userId]--;
                    return;

                case 4:  
                    if (string.IsNullOrWhiteSpace(text))
                    {
                        OldMessages[userId].Add((await botClient.SendTextMessageAsync(userId, "Вкажіть тривалість.", cancellationToken: cancellationToken)).MessageId);
                        return;
                    }
                    SpellDataClass[userId].Target = text;

                     
                    OldMessages[userId].Add((await botClient.SendTextMessageAsync(
                        userId,
                        "Чи є урон у цьому ефекті?",
                        replyMarkup: YesNoKeyboard(),
                        cancellationToken: cancellationToken)).MessageId);

                     
                    StateBotMSG[userId] = 3;
                    return;

                case 3:
                     
                     
                    return;

                case 2:
                     
                     
                     

                    await TryConsumeDiceCountAsync(botClient,message,cancellationToken);
                    return;

                case 1:
                     
                    if (string.IsNullOrWhiteSpace(text))
                    {
                        OldMessages[userId].Add((await botClient.SendTextMessageAsync(userId, DialogNumber[0], cancellationToken: cancellationToken)).MessageId);
                        return;
                    }
                    SpellDataClass[userId].Description = text;

                     
                    await botClient.SendTextMessageAsync(userId, SpellData.AddOrUpdateSpellBook(SpellDataClass[userId]), cancellationToken: cancellationToken);

                    DeleteAllOldMessages(botClient, cancellationToken, userId);
                    isProcessingAnotherProcess[userId] = false;
                    userStateSpells[userId] = "";
                    StateBotMSG[userId] = 0;
                    return;

                case 0:
                     
                    return;
            }
        }

         
        public static async Task HandleCallbackQ(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken, Message message, CallbackQuery callbackQuery)
        {
            try
            {
                var data = callbackQuery.Data ?? "";
                if (callbackQuery.Message?.Chat == null)
                    return;
                long chatId = callbackQuery.Message.Chat.Id;

                 
                if (data.StartsWith("SC_HasDmg_"))
                {
                    if (data.EndsWith("_Yes"))
                    {
                         
                        DraftParts[chatId] = new List<string>();
                        SelectedDie[chatId] = "";
                        OldMessages[chatId].Add((await botClient.SendTextMessageAsync(chatId, "Виберіть куб:", replyMarkup: DiceInlineKeyboard(), cancellationToken: cancellationToken)).MessageId);
                        StateBotMSG[chatId] = 3;  
                    }
                    else
                    {
                         
                        SpellDataClass[chatId].DamageDice = "";
                        OldMessages[chatId].Add((await botClient.SendTextMessageAsync(
                            chatId,
                            "Виберіть модифікатор для ефекту або предмета:",
                            replyMarkup: ModifierInlineKeyboard(),
                            cancellationToken: cancellationToken)).MessageId);

                         
                    }

                    await botClient.AnswerCallbackQueryAsync(callbackQuery.Id);
                    return;
                }

                if (data.StartsWith("Dice_"))
                {
                     
                    var die = data.Split('_')[1];  
                    SelectedDie[chatId] = die;

                    OldMessages[chatId].Add((await botClient.SendTextMessageAsync(
                        chatId,
                        $"Обрано {die}. Скільки кубиків кинути? Введи ціле число > 0.",
                        cancellationToken: cancellationToken)).MessageId);

                     
                    StateBotMSG[chatId] = 2;
                    await botClient.AnswerCallbackQueryAsync(callbackQuery.Id);
                    return;
                }

                if (data == "Damage_AddMore")
                {
                     
                    OldMessages[chatId].Add((await botClient.SendTextMessageAsync(
                        chatId, "Вибери наступний куб:", replyMarkup: DiceInlineKeyboard(), cancellationToken: cancellationToken)).MessageId);

                    StateBotMSG[chatId] = 3;
                    await botClient.AnswerCallbackQueryAsync(callbackQuery.Id);
                    return;
                }

                if (data == "Damage_Done")
                {
                     
                    var parts = DraftParts.TryGetValue(chatId, out var list) ? list : new List<string>();
                    SpellDataClass[chatId].DamageDice = parts.Count == 0 ? "" : string.Join(" + ", parts);

                    OldMessages[chatId].Add((await botClient.SendTextMessageAsync(
                        chatId,
                        "Виберіть модифікатор для ефекту або предмета:",
                        replyMarkup: ModifierInlineKeyboard(),
                        cancellationToken: cancellationToken)).MessageId);

                    await botClient.AnswerCallbackQueryAsync(callbackQuery.Id);
                    return;
                }

                 
                if (data.StartsWith("Modifier_"))
                {
                    var infoAfterSplit = data.Split('_')[1];

                    if (SpellDataClass.ContainsKey(chatId))
                    {
                         
                        SpellDataClass[chatId].Modifier = infoAfterSplit;
                        StateBotMSG[chatId] = 1;
                        OldMessages[chatId].Add((await botClient.SendTextMessageAsync(chatId, DialogNumber[0], cancellationToken: cancellationToken)).MessageId);
                    }

                    await botClient.AnswerCallbackQueryAsync(callbackQuery.Id);
                    return;
                }
                if (data.StartsWith("DelSpell_"))
                {
                    var infoAfterSplit = data.Split('_')[1];
                    if (int.TryParse(infoAfterSplit, out int spellId) && spellId > 0)
                    {
                        await DeleteSpell.SendDeleteConfirm(botClient, cancellationToken, chatId, spellId);
                    }
                    await botClient.AnswerCallbackQueryAsync(callbackQuery.Id);
                    return;
                }

                if (data.StartsWith("DelConfirm_") || data == "DelCancel")
                {
                    if (data.StartsWith("DelConfirm_"))
                    {
                        var infoAfterSplit = data.Split('_')[1];
                        if (int.TryParse(infoAfterSplit, out int spellId) && spellId > 0)
                        {
                             
                            var result = DeleteSpell.DeleteSpellById(chatId, spellId);
                            await botClient.SendTextMessageAsync(chatId, result, cancellationToken: cancellationToken);
                        }
                    }

                     
                    await botClient.SendTextMessageAsync(chatId, "Поточні ефекти:", cancellationToken: cancellationToken);
                    var spells = SpellData.SpellDataBaseReaderList(chatId);
                    foreach (var s in spells)
                        await botClient.SendTextMessageAsync(chatId, SpellData.FormatSpellFull(s), cancellationToken: cancellationToken);

                     
                    var method = typeof(DeleteSpell)
                        .GetMethod("BuildDeleteKeyboard", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
                    var kb = method?.Invoke(null, new object[] { (long)chatId }) as InlineKeyboardMarkup;

                    OldMessages[chatId].Add((await botClient.SendTextMessageAsync(
                        chatId,
                        "Оберіть наступний ефект для видалення або натисніть «⬅️ Скасувати».",
                        replyMarkup: kb,
                        cancellationToken: cancellationToken)).MessageId);

                    await botClient.AnswerCallbackQueryAsync(callbackQuery.Id);
                    return;
                }
                if (data.StartsWith("DelCancel_"))
                {
                    await botClient.SendTextMessageAsync(chatId, "Скасовано.", cancellationToken: cancellationToken);
                    DeleteAllOldMessages(botClient, cancellationToken, chatId);
                    isProcessingAnotherProcess[chatId] = false;
                    userStateSpells[chatId] = "";          
                    StateBotMSG[chatId] = 0;
                    await botClient.AnswerCallbackQueryAsync(callbackQuery.Id);
                }
                 
                if (data.StartsWith("Spell_"))
                {
                    var infoAfterSplit = data.Split('_')[1];

                    switch (infoAfterSplit)
                    {
                        case "check":
                            await SendAllSpellsToUser(botClient, chatId, cancellationToken);
                            break;

                        case "add":
                            long userId = chatId;

                            StateBotMSG.TryAdd(userId, 3);
                            userStateSpells.TryAdd(userId, "start");

                            await botClient.SendTextMessageAsync(chatId, "Якщо ви ввели неправильне значення ви завжди можете повернутись за допомогою команди 'Повернутись'", cancellationToken: cancellationToken);

                            await StartVoidSpell(botClient, message, cancellationToken, userId);
                            break;

                        case "delete":
                            await botClient.SendTextMessageAsync(chatId, "Який ID ефекту ви хочете видалити?", cancellationToken: cancellationToken);
                            ///
                            await DeleteSpell.StartVoid(botClient, message, cancellationToken, chatId);
                            break;

                        case "use":
                            await ActivateSpell.StartVoid(botClient, message, cancellationToken, chatId);
                            break;

                        case "edit":
                            await EditSpellDamage.StartVoidEditSpell(botClient, message, cancellationToken, chatId);
                            break;
                    }

                    await botClient.AnswerCallbackQueryAsync(callbackQuery.Id);
                    return;
                }

                if (data.StartsWith("UseSpell_"))
                {
                    var infoAfterSplit = data.Split('_')[1];
                    if (int.TryParse(infoAfterSplit, out int spellId))
                    {
                        StateBotMSG[chatId] = 0;
                        if (spellId > 0)
                            await ActivateSpell.PasteVoid(botClient, message, cancellationToken, chatId, spellId);
                    }

                    await botClient.AnswerCallbackQueryAsync(callbackQuery.Id);
                    return;
                }
            }
            catch (ApiRequestException ex) when (ex.Message.Contains("query is too old"))
            {
                Console.WriteLine($"Callback query is too old for user {callbackQuery.From.Id}");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error handling callback for user {0}: {1}", callbackQuery.From.Id, ex);
            }
        }

         
         
         
        public static async Task<bool> TryConsumeDiceCountAsync(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
        {
            var chatId = message.Chat.Id;
            if (!StateBotMSG.TryGetValue(chatId, out var state)) return false;
            if (state != 2) return false;  

            var input = message.Text?.Trim() ?? "";
            if (!int.TryParse(input, out var count) || count <= 0)
            {
                OldMessages[chatId].Add((await botClient.SendTextMessageAsync(chatId, "Невірний формат. Введи ціле число > 0.", cancellationToken: cancellationToken)).MessageId);
                return true;  
            }

            var die = SelectedDie.TryGetValue(chatId, out var d) ? d : "";
            if (string.IsNullOrWhiteSpace(die))
            {
                OldMessages[chatId].Add((await botClient.SendTextMessageAsync(chatId, "Куб не обрано. Почни ще раз (тисни \"Вибери куб\").", cancellationToken: cancellationToken)).MessageId);
                return true;
            }

            if (!DraftParts.ContainsKey(chatId)) DraftParts[chatId] = new List<string>();
            DraftParts[chatId].Add($"{count}{die}");

            var preview = (DraftParts[chatId].Count > 0) ? string.Join(" + ", DraftParts[chatId]) : "(порожньо)";

             
            OldMessages[chatId].Add((await botClient.SendTextMessageAsync(
                chatId,
                $"Додано: {count}{die}\nПоточний урон: {preview}",
                replyMarkup: DamageMoreOrDoneKeyboard(),
                cancellationToken: cancellationToken)).MessageId);

             
            StateBotMSG[chatId] = 3;
            return true;
        }

         

        public static async Task UseSpelInlineKeyboard(ITelegramBotClient botClient, CancellationToken cancellationToken, long userId)
        {
            var inlineKeyboard = new List<List<InlineKeyboardButton>>();
            var spells = SpellData.GetSpellsFromDatabase(userId);

            foreach (var spell in spells)
            {
                inlineKeyboard.Add(new List<InlineKeyboardButton>
                {
                    InlineKeyboardButton.WithCallbackData($"{spell.Name}", $"UseSpell_{spell.SpellId}")
                });
            }

            OldMessages[userId].Add((await botClient.SendTextMessageAsync(userId, "Який ефект ви хочете застосувати?", replyMarkup: new InlineKeyboardMarkup(inlineKeyboard), cancellationToken: cancellationToken)).MessageId);
        }

        private static async Task SendAllSpellsToUser(ITelegramBotClient botClient, long chatId, CancellationToken cancellationToken)
        {
            var spellList = SpellData.SpellDataBaseReaderList(chatId);

            foreach (var spell in spellList)
                await botClient.SendTextMessageAsync(chatId, SpellData.FormatSpellFull(spell), cancellationToken: cancellationToken);
        }
    }
}
