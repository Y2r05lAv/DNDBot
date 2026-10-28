using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot.Types;
using Telegram.Bot;
using DNDBot.MonsterSystem;

namespace DNDBot.MonsterSystem
{
    class StopMonsterCreation : MonsterRealisation
    {
        public static async Task HandleMonsterDeleteCallback(
            ITelegramBotClient botClient,
            long userId,
            string action,
            ReplyKeyboardMarkup replyKeyboard,
            BotBodyRealisation botBodyRealisation,
            CancellationToken cancellationToken)
        {
            DeleteAllOldMessages(botClient, cancellationToken, userId);

            if (action == "monster_delete")
            {
                if (Monsters.Any(m => m.ID == damagedCreature))
                    Monsters.RemoveAll(m => m.ID == damagedCreature);

                 
                MonsterCount = Math.Max(1, MonsterCount - 1);
                State = 0;

                replyKeyboard = botBodyRealisation.ButtonsCreate(userId, false);

                var deleteMsg = await botClient.SendTextMessageAsync(
                    userId,
                    "Монстр був видалений.",
                    cancellationToken: cancellationToken,
                    replyMarkup: replyKeyboard);

                OldMessages[userId].Add(deleteMsg.MessageId);
                userState[MonsterCount] = "";
            }
            else if (action == "monster_keep" || action == "monster_keep_template")
            {
                if (action == "monster_keep_template")
                {
                    if (newMonster == null)
                    {
                        var noMonsterMsg = await botClient.SendTextMessageAsync(
                            userId,
                            "Немає монстра для збереження у шаблон.",
                            cancellationToken: cancellationToken);

                        OldMessages[userId].Add(noMonsterMsg.MessageId);
                    }
                    else
                    {
                        int templateId = MonsterTemplateData.AddTemplate(userId, newMonster);
                        var templateSavedMsg = await botClient.SendTextMessageAsync(
                            userId,
                            $"Шаблон збережено (ID: {templateId}).",
                            cancellationToken: cancellationToken);

                        OldMessages[userId].Add(templateSavedMsg.MessageId);
                    }
                }

                replyKeyboard = botBodyRealisation.ButtonsCreate(userId, false);

                var countMsg = await botClient.SendTextMessageAsync(
                    userId,
                    "Скільки монстрів ви хочете створити?",
                    cancellationToken: cancellationToken);

                OldMessages[userId].Add(countMsg.MessageId);
                userState[MonsterCount] = "Count";
            }
        }

        public static async Task<ReplyKeyboardMarkup> DeleteVoid(
            ITelegramBotClient botClient,
            Message message,
            ReplyKeyboardMarkup replyKeyboard,
            BotBodyRealisation botBodyRealisation,
            CancellationToken cancellationToken)
        {
            long userId = masterID;

            OldMessages.TryAdd(masterID, [message.MessageId]);
            DeleteAllOldMessages(botClient, cancellationToken, userId);

            if (message.Text == "ВИДАЛИТИ")
            {
                if (Monsters.Any(m => m.ID == damagedCreature))
                    Monsters.RemoveAll(m => m.ID == damagedCreature);

                 
                MonsterCount = Math.Max(1, MonsterCount - 1);
                State = 0;

                replyKeyboard = botBodyRealisation.ButtonsCreate(userId, false);

                var deleteMsg = await botClient.SendTextMessageAsync(
                    message.Chat.Id,
                    "Монстр був видалений.",
                    cancellationToken: cancellationToken,
                    replyMarkup: replyKeyboard);

                OldMessages[userId].Add(deleteMsg.MessageId);
                userState[MonsterCount] = "";
            }
            else if (message.Text == "ЗАЛИШИТИ")
            {
                replyKeyboard = botBodyRealisation.ButtonsCreate(userId, false);

                var countMsg = await botClient.SendTextMessageAsync(
                    message.Chat.Id,
                    "Скільки монстрів ви хочете створити?",
                    cancellationToken: cancellationToken);

                OldMessages[userId].Add(countMsg.MessageId);
                userState[MonsterCount] = "Count";
            }
            else
            {
                var errorMsg = await botClient.SendTextMessageAsync(
                    message.Chat.Id,
                    "Чекаю на точну відповідь!",
                    cancellationToken: cancellationToken);

                OldMessages[userId].Add(errorMsg.MessageId);
            }

            return replyKeyboard;
        }

    }
}
