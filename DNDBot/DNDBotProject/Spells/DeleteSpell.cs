using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace DNDBot.Spells
{
    public class DeleteSpell : SpellCollector
    {
         
        public static async Task StartVoid(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken, long userId)
        {
            isProcessingAnotherProcess[userId] = true;
            userStateSpells[userId] = "pasteDeleting";          
            StateBotMSG[userId] = 1;                            

            var kb = BuildDeleteKeyboard(userId);
            OldMessages[userId].Add((await botClient.SendTextMessageAsync(
                userId,
                "Оберіть ефект для видалення:",
                replyMarkup: kb,
                cancellationToken: cancellationToken)).MessageId);
        }

         
        public static async Task PasteVoid(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken, long userId)
        {
            if (message?.Text == null) return;

            var text = message.Text.Trim();
            if (!int.TryParse(text, out var spellId) || spellId <= 0)
            {
                OldMessages[userId].Add((await botClient.SendTextMessageAsync(
                    userId,
                    "Введіть коректний ID або натисніть кнопку зі списку.",
                    cancellationToken: cancellationToken)).MessageId);
                return;
            }

             
            await SendDeleteConfirm(botClient, cancellationToken, userId, spellId);
        }

         

        private static InlineKeyboardMarkup BuildDeleteKeyboard(long userId)
        {
            var spells = SpellData.GetSpellsFromDatabase(userId);
            var rows = new List<List<InlineKeyboardButton>>();

            foreach (var s in spells)
            {
                rows.Add(new List<InlineKeyboardButton> {
                    InlineKeyboardButton.WithCallbackData($"{s.Name} (Id={s.SpellId})", $"DelSpell_{s.SpellId}")
                });
            }

             
            if (rows.Count == 0)
            {
                rows.Add(new List<InlineKeyboardButton> {
                    InlineKeyboardButton.WithCallbackData("Ефектів нема — повернутись", "DelCancel_cancel")
                });
            }
            else
            {
                rows.Add(new List<InlineKeyboardButton> {
                    InlineKeyboardButton.WithCallbackData("⬅️ Скасувати", "DelCancel_cancel")
                });
            }

            return new InlineKeyboardMarkup(rows);
        }

        private static InlineKeyboardMarkup BuildConfirmKeyboard(int spellId)
        {
            return new InlineKeyboardMarkup(new[]
            {
                new []
                {
                    InlineKeyboardButton.WithCallbackData("✅ Так, видалити", $"DelConfirm_{spellId}"),
                    InlineKeyboardButton.WithCallbackData("❌ Ні", "DelCancel_cancel")
                }
            });
        }

        public static async Task SendDeleteConfirm(ITelegramBotClient botClient, CancellationToken cancellationToken, long userId, int spellId)
        {
            var s = SpellData.GetSpellToClass(userId, spellId);
            string name = string.IsNullOrWhiteSpace(s?.Name) ? $"Id={spellId}" : $"{s.Name} (Id={spellId})";

            OldMessages[userId].Add((await botClient.SendTextMessageAsync(
                userId,
                $"Точно видалити: {name} ?",
                replyMarkup: BuildConfirmKeyboard(spellId),
                cancellationToken: cancellationToken)).MessageId);
        }

         
        public static string DeleteSpellById(long userId, int spellId)
        {
             
             
             
            return SpellData.DeactivateSpell(userId, spellId);   
        }
    }
}
