 
 
 

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace DNDBot.Spells
{
    public class EditSpellDamage : SpellCollector
    {
         
         
         
         
        private const int STATE_SELECT_SPELL = 2;
        private const int STATE_SELECT_DIE = 1;
        private const int STATE_ENTER_COUNT = 0;
        private const int STATE_CONFIRM_HASDMG = 3;  


         
        private static readonly Dictionary<long, int> SelectedSpellId = new Dictionary<long, int>();
        private static readonly Dictionary<long, string> SelectedDie = new Dictionary<long, string>();  

         

         
        private static readonly Dictionary<long, List<string>> DraftParts = new();   


        public static async Task StartVoidEditSpell(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken, long userId)
        {
            isProcessingAnotherProcess[userId] = true;

            if (!OldMessages.ContainsKey(userId)) OldMessages.Add(userId, new List<int>());
            if (!userStateSpells.ContainsKey(userId)) userStateSpells.Add(userId, "");
            if (message != null) OldMessages[userId].Add(message.MessageId);

             
            SelectedSpellId.Remove(userId);
            SelectedDie.Remove(userId);
            DraftParts[userId] = new List<string>();

            StateBotMSG[userId] = STATE_SELECT_SPELL;
            userStateSpells[userId] = "pastechangeSpell";

            await PasteVoidEditSpell(botClient, message, cancellationToken, userId);
        }

        public static async Task PasteVoidEditSpell(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken, long userId)
        {
            if (!StateBotMSG.ContainsKey(userId)) return;

            switch ((int)StateBotMSG[userId])
            {
                case STATE_SELECT_SPELL:
                    {
                        var spells = SpellData.GetSpellsFromDatabase(userId);
                        if (spells == null || spells.Count == 0)
                        {
                            await botClient.SendTextMessageAsync(userId, "У тебе ще немає активних ефектів.", cancellationToken: cancellationToken);
                            StopMethods(userId);
                            return;
                        }

                        var kb = new List<List<InlineKeyboardButton>>();
                        foreach (var s in spells)
                            kb.Add(new List<InlineKeyboardButton> {
                    InlineKeyboardButton.WithCallbackData($"{s.Name}", $"EditDmg_Select_{s.SpellId}")
                });

                        OldMessages[userId].Add((await botClient.SendTextMessageAsync(
                            userId,
                            "Оберіть ефект для редагування урону:",
                            replyMarkup: new InlineKeyboardMarkup(kb),
                            cancellationToken: cancellationToken)).MessageId);
                        return;
                    }

                case STATE_CONFIRM_HASDMG:
                    {
                         
                        OldMessages[userId].Add((await botClient.SendTextMessageAsync(
                            userId,
                            "Чи є урон у цьому ефекті?",
                            replyMarkup: YesNoKeyboard(),   
                            cancellationToken: cancellationToken)).MessageId);
                        return;
                    }

                case STATE_SELECT_DIE:
                    {
                        OldMessages[userId].Add((await botClient.SendTextMessageAsync(
                            userId,
                            DraftParts.TryGetValue(userId, out var parts) && parts.Count > 0
                                ? $"Поточний урон: {JoinParts(parts)}\nВиберіть тип кубика для наступної частини:"
                                : "Виберіть тип кубика для урону:",
                            replyMarkup: DiceKeyboard(),
                            cancellationToken: cancellationToken)).MessageId);
                        return;
                    }

                case STATE_ENTER_COUNT:
                    {
                        var input = message?.Text?.Trim() ?? "";
                        if (string.IsNullOrEmpty(input))
                        {
                            OldMessages[userId].Add((await botClient.SendTextMessageAsync(
                                userId, "Введи ціле число > 0 для кількості кубиків.", cancellationToken: cancellationToken)).MessageId);
                            return;
                        }

                        if (!int.TryParse(input, out var count) || count <= 0)
                        {
                            OldMessages[userId].Add((await botClient.SendTextMessageAsync(
                                userId, "Невірний формат. Введи ціле число > 0.", cancellationToken: cancellationToken)).MessageId);
                            return;
                        }

                        if (!SelectedSpellId.TryGetValue(userId, out var spellId) || spellId <= 0)
                        {
                            OldMessages[userId].Add((await botClient.SendTextMessageAsync(
                                userId, "Ефект не обрано. Почни ще раз.", cancellationToken: cancellationToken)).MessageId);
                            StopMethods(userId);
                            return;
                        }

                        if (!SelectedDie.TryGetValue(userId, out var die) || string.IsNullOrWhiteSpace(die))
                        {
                            OldMessages[userId].Add((await botClient.SendTextMessageAsync(
                                userId, "Куб не обрано. Почни ще раз.", cancellationToken: cancellationToken)).MessageId);
                            StopMethods(userId);
                            return;
                        }

                         
                        if (!DraftParts.ContainsKey(userId)) DraftParts[userId] = new List<string>();
                        DraftParts[userId].Add($"{count}{die}");

                         
                        var partsNow = JoinParts(DraftParts[userId]);

                         
                        StateBotMSG[userId] = STATE_SELECT_DIE;
                        OldMessages[userId].Add((await botClient.SendTextMessageAsync(
                            userId,
                            $"Додано: {count}{die}\nПоточний урон: {partsNow}\nВиберіть чи йти далі чи призупиніть:",
                            replyMarkup: StopKeyboard(),   
                            cancellationToken: cancellationToken)).MessageId);

                        return;
                    }
            }
        }

         
        public static new async Task HandleCallbackQ(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken, Message message, CallbackQuery callbackQuery)
        {
            try
            {
                var data = callbackQuery.Data ?? "";
                if (!data.StartsWith("EditDmg_"))
                    return;

                await TryAnswerCallbackQuerySafe(botClient, callbackQuery.Id, cancellationToken);

                if (callbackQuery.Message?.Chat == null)
                    return;

                var chatId = callbackQuery.Message.Chat.Id;

                 
                if (data.StartsWith("EditDmg_Select_"))
                {
                    var tail = data.Split('_').Last();
                    if (int.TryParse(tail, out var spellId))
                    {
                        SelectedSpellId[chatId] = spellId;
                        DraftParts[chatId] = new List<string>();
                        StateBotMSG[chatId] = STATE_CONFIRM_HASDMG;

                        OldMessages[chatId].Add((await botClient.SendTextMessageAsync(
                            chatId,
                            "Чи є урон у цьому ефекті?",
                            replyMarkup: YesNoKeyboard(),
                            cancellationToken: cancellationToken)).MessageId);
                    }
                    return;
                }
                if (data.StartsWith("EditDmg_AddMoreDamage"))
                {

                    OldMessages[chatId].Add((await botClient.SendTextMessageAsync(
                    chatId,
                    DraftParts.TryGetValue(chatId, out var parts) && parts.Count > 0
                        ? $"Поточний урон: {JoinParts(parts)}\nВиберіть тип кубика для наступної частини:"
                        : "Виберіть тип кубика для урону:",
                    replyMarkup: DiceKeyboard(),
                    cancellationToken: cancellationToken)).MessageId);
                    return;
                }

                 
                if (data == "EditDmg_HasDmg_Yes")
                {
                    StateBotMSG[chatId] = STATE_SELECT_DIE;

                    OldMessages[chatId].Add((await botClient.SendTextMessageAsync(
                        chatId,
                        "Виберіть тип кубика для урону:",
                        replyMarkup: DiceKeyboard(),
                        cancellationToken: cancellationToken)).MessageId);

                    return;
                }

                if (data == "EditDmg_HasDmg_No")
                {
                     
                    if (SelectedSpellId.TryGetValue(chatId, out var spellId) && spellId > 0)
                    {
                        var result = SpellData.UpdateDamage(chatId, spellId, "");  
                        await botClient.SendTextMessageAsync(chatId, $"{result}\nНемає урону.", cancellationToken: cancellationToken);
                    }
                    DeleteAllOldMessages(botClient, cancellationToken, chatId);
                    StopMethods(chatId);

                    return;
                }

                 
                if (data.StartsWith("EditDmg_Dice_"))
                {
                    var die = data.Split('_').Last();  
                    SelectedDie[chatId] = die;
                    StateBotMSG[chatId] = STATE_ENTER_COUNT;

                    OldMessages[chatId].Add((await botClient.SendTextMessageAsync(
                        chatId,
                        $"Обрано {die}. Скільки кубиків кинути? Введи ціле число > 0.",
                        cancellationToken: cancellationToken)).MessageId);

                    return;
                }

                 
                if (data == "EditDmg_Stop")
                {
                    if (!SelectedSpellId.TryGetValue(chatId, out var spellId) || spellId <= 0)
                    {
                        return;
                    }

                    var parts = DraftParts.TryGetValue(chatId, out var list) ? list : new List<string>();
                    var damage = parts.Count == 0 ? "" : JoinParts(parts);

                    var result = SpellData.UpdateDamage(chatId, spellId, damage);
                    await botClient.SendTextMessageAsync(chatId,
                        parts.Count == 0 ? $"{result}\nНемає урону." : $"{result}\nОновлено урон: {damage}",
                        cancellationToken: cancellationToken);

                    DeleteAllOldMessages(botClient, cancellationToken, chatId);
                    StopMethods(chatId);

                    return;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("EditSpellDamage.HandleCallbackQ error: {0}", ex);
            }
        }

        private static async Task TryAnswerCallbackQuerySafe(
            ITelegramBotClient botClient,
            string callbackQueryId,
            CancellationToken cancellationToken)
        {
            try
            {
                await botClient.AnswerCallbackQueryAsync(callbackQueryId, cancellationToken: cancellationToken);
            }
            catch (ApiRequestException ex) when (IsExpiredOrInvalidCallback(ex))
            {
                Console.WriteLine("EditSpellDamage: callback query already expired or invalid.");
            }
        }

        private static bool IsExpiredOrInvalidCallback(ApiRequestException ex)
        {
            var text = ex.Message ?? string.Empty;
            return text.Contains("query is too old", StringComparison.OrdinalIgnoreCase)
                || text.Contains("response timeout expired", StringComparison.OrdinalIgnoreCase)
                || text.Contains("query ID is invalid", StringComparison.OrdinalIgnoreCase);
        }

        private static InlineKeyboardMarkup YesNoKeyboard()
        {
            return new InlineKeyboardMarkup(new[]
            {
        new []
        {
            InlineKeyboardButton.WithCallbackData("Так", "EditDmg_HasDmg_Yes"),
            InlineKeyboardButton.WithCallbackData("Ні",  "EditDmg_HasDmg_No")
        }
    });
        }

        private static InlineKeyboardMarkup DiceKeyboard()
        {
            return new InlineKeyboardMarkup(new[]
            {
        new []
        {
            InlineKeyboardButton.WithCallbackData("d4",  "EditDmg_Dice_d4"),
            InlineKeyboardButton.WithCallbackData("d6",  "EditDmg_Dice_d6"),
            InlineKeyboardButton.WithCallbackData("d8",  "EditDmg_Dice_d8"),
        },
        new []
        {
            InlineKeyboardButton.WithCallbackData("d10", "EditDmg_Dice_d10"),
            InlineKeyboardButton.WithCallbackData("d12", "EditDmg_Dice_d12"),
            InlineKeyboardButton.WithCallbackData("d20", "EditDmg_Dice_d20"),
        }
    });
        }

        private static InlineKeyboardMarkup StopKeyboard()
        {
            return new InlineKeyboardMarkup(new[]
            {
        new []
        {
            InlineKeyboardButton.WithCallbackData("✖ Призупинити", "EditDmg_Stop"),
            InlineKeyboardButton.WithCallbackData("➕ Додати ще урон", "EditDmg_AddMoreDamage")
        }
    });
        }

        private static string JoinParts(List<string> parts)
        {
            return string.Join(" + ", parts);
        }


        private static void StopMethods(long userId)
        {
            isProcessingAnotherProcess[userId] = false;
            userStateSpells[userId] = "";
            StateBotMSG[userId] = STATE_SELECT_SPELL;  

            SelectedSpellId.Remove(userId);
            SelectedDie.Remove(userId);
        }
    }
}
