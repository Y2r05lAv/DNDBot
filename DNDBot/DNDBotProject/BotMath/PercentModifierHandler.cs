using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using DNDBot.DataBase;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace DNDBot.BotSystem
{
    public class PercentModifierHandler
    {
        private readonly ILogger<PercentModifierHandler> _logger;

        public PercentModifierHandler(ILogger<PercentModifierHandler> logger)
        {
            _logger = logger;
        }

        public async Task StartModifyFlow(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
        {
            if (message?.From == null)
                return;

            long userId = message.From.Id;
            var inline = new InlineKeyboardMarkup(new[]
            {
                new [] { InlineKeyboardButton.WithCallbackData("MaxMana %","pm_MaxMana"), InlineKeyboardButton.WithCallbackData("CurrentMana %","pm_CurrentMana") },
                new [] { InlineKeyboardButton.WithCallbackData("MaxHP %","pm_MaxHP"), InlineKeyboardButton.WithCallbackData("CurrentHP %","pm_CurrentHP"),  },
            });

            var sent = await botClient.SendTextMessageAsync(userId, "Оберіть, який відсоток змінити:", replyMarkup: inline, cancellationToken: cancellationToken);
            if (!BotBodyRealisation.OldMessages.ContainsKey(userId)) BotBodyRealisation.OldMessages[userId] = new System.Collections.Generic.List<int>();
            BotBodyRealisation.OldMessages[userId].Add(sent.MessageId);
            _logger.LogDebug("User {UserId} started percent modify flow", userId);
        }

        public async Task HandleCallback(ITelegramBotClient botClient, CallbackQuery callbackQuery, CancellationToken cancellationToken)
        {
            try
            {
                var data = callbackQuery.Data ?? "";
                if (callbackQuery.Message?.Chat == null)
                    return;
                long chatId = callbackQuery.Message.Chat.Id;

                if (!data.StartsWith("pm_"))
                {
                    return;
                }

                string field = data.Substring(3); 
                BotBodyRealisation.userStateDataChanger[chatId] = $"percent:{field}";

                string hint =
                    "Введіть значення у відсотках:\n" +
                    "+20 — додати 20%\n" +
                    "-15 — зняти 15%\n" +
                    "20  — додати 20%\n" +
                    "Можете писати знак `%`, він необов'язковий.";

                var msg = await botClient.SendTextMessageAsync(chatId, hint, cancellationToken: cancellationToken);
                if (!BotBodyRealisation.OldMessages.ContainsKey(chatId)) BotBodyRealisation.OldMessages[chatId] = new System.Collections.Generic.List<int>();
                BotBodyRealisation.OldMessages[chatId].Add(msg.MessageId);

                await botClient.AnswerCallbackQueryAsync(callbackQuery.Id, "Введіть відсоток у чаті", cancellationToken: cancellationToken);
                _logger.LogInformation("User {UserId} selected percent field {Field}", chatId, field);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "HandleCallback error");
                try
                {
                    await botClient.AnswerCallbackQueryAsync(callbackQuery.Id, "Помилка обробки", cancellationToken: cancellationToken);
                }
                catch (Exception answerEx)
                {
                    _logger.LogWarning(answerEx, "Failed to answer callback after percent modifier error. CallbackId={CallbackId}", callbackQuery.Id);
                }
            }
        }

        public async Task HandleTextInput(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
        {
            if (message?.From == null)
                return;

            long userId = message.From.Id;
            if (!BotBodyRealisation.userStateDataChanger.TryGetValue(userId, out var state) || string.IsNullOrWhiteSpace(state))
                return;

            if (!state.StartsWith("percent:"))
                return;

            string field = state.Substring("percent:".Length);
            string text = (message.Text ?? "").Trim();
            if (string.IsNullOrEmpty(text))
                return;

            text = text.Replace("%", "").Trim();
            if (!int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int deltaPercent))
            {
                await botClient.SendTextMessageAsync(userId, "Не вдалося розпізнати число. Введіть ціле число, наприклад +20 або -10.", cancellationToken: cancellationToken);
                return;
            }

            try
            {
                int charId = UserData.GetActiveCharacterId(userId);
                var mods = ResourceModifiers.Get(userId, charId);

                switch (field)
                {
                    case "MaxMana":
                        mods.MaxManaPercentMod += deltaPercent;
                        break;
                    case "CurrentMana":
                        mods.CurrentManaPercentMod += deltaPercent;
                        break;
                    case "BaseMana":
                        mods.BaseManaPercentMod += deltaPercent;
                        break;
                    case "MaxHP":
                        mods.MaxHPPercentMod += deltaPercent;
                        break;
                    case "CurrentHP":
                        mods.CurrentHPPercentMod += deltaPercent;
                        break;
                    case "BaseHP":
                        mods.BaseHPPercentMod += deltaPercent;
                        break;
                    default:
                        await botClient.SendTextMessageAsync(userId, "Невідоме поле для зміни.", cancellationToken: cancellationToken);
                        BotBodyRealisation.userStateDataChanger[userId] = "";
                        return;
                }

                ResourceModifiers.Upsert(mods);

                if (field.Contains("Mana"))
                {
                    ResourceModifiers.ApplyManaModifiersAfterRecalc(userId, charId, keepCurrentProportion: true);
                }
                else
                {
                    var r = mods;
                    int rawMaxHP = UserData.DataBaseValueReader<int>(userId, charId, "MaxHP");
                    int currentHP = UserData.DataBaseValueReader<int>(userId, charId, "Health");
                    double maxMultiplier = 1.0 + r.MaxHPPercentMod / 100.0;
                    int effectiveMaxHP = (int)Math.Round(rawMaxHP * maxMultiplier);

                    int newCurrentHP;
                    int delta = r.CurrentHPPercentMod; 
                    int percentToApply = deltaPercent;

                    int deficit = effectiveMaxHP - currentHP;
                    if (deficit <= 0)
                    {
                        newCurrentHP = Math.Min(currentHP, effectiveMaxHP);
                    }
                    else
                    {
                        int restore = (int)Math.Ceiling(deficit * (percentToApply / 100.0));
                        newCurrentHP = Math.Min(effectiveMaxHP, currentHP + restore);
                    }

                    newCurrentHP = Math.Clamp(newCurrentHP, int.MinValue / 2, Math.Max(newCurrentHP, effectiveMaxHP));

                    UserData.UpdateDataBaseValue("MaxHP", userId, charId, effectiveMaxHP);
                    UserData.UpdateDataBaseValue("Health", userId, charId, newCurrentHP);
                }

                await botClient.SendTextMessageAsync(userId, $"Готово. Поле {field} змінено на {deltaPercent}% (всього {GetModValueByField(mods, field)}%).", cancellationToken: cancellationToken);
                _logger.LogInformation("User {UserId} applied {Delta}% to {Field}", userId, deltaPercent, field);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error applying percent change for user {UserId}", userId);
                await botClient.SendTextMessageAsync(userId, $"Помилка: {ex.Message}", cancellationToken: cancellationToken);
            }
            finally
            {
                BotBodyRealisation.userStateDataChanger[userId] = "";
            }
        }

        private static int GetModValueByField(ResourceModifiersRecord r, string field)
        {
            return field switch
            {
                "MaxMana" => r.MaxManaPercentMod,
                "CurrentMana" => r.CurrentManaPercentMod,
                "BaseMana" => r.BaseManaPercentMod,
                "MaxHP" => r.MaxHPPercentMod,
                "CurrentHP" => r.CurrentHPPercentMod,
                "BaseHP" => r.BaseHPPercentMod,
                _ => 0
            };
        }

        public async Task ShowPotionKeyboard(ITelegramBotClient botClient, Message message, bool isMana, CancellationToken cancellationToken)
        {
            if (message?.From == null)
                return;

            long userId = message.From.Id;
            var prefix = isMana ? "potion_mana_" : "potion_hp_";

            var inline = new InlineKeyboardMarkup(new[]
            {
                new[] { InlineKeyboardButton.WithCallbackData("10%", $"{prefix}10"), InlineKeyboardButton.WithCallbackData("25%", $"{prefix}25") },
                new[] { InlineKeyboardButton.WithCallbackData("50%", $"{prefix}50"), InlineKeyboardButton.WithCallbackData("75%", $"{prefix}75") },
                new[] { InlineKeyboardButton.WithCallbackData("100%", $"{prefix}100"), InlineKeyboardButton.WithCallbackData("❌ Відмінити", "potion_cancel") }
            });

            var text = isMana ? "Оберіть зілля для відновлення мани:" : "Оберіть зілля для відновлення здоров'я:";
            var sent = await botClient.SendTextMessageAsync(userId, text, replyMarkup: inline, cancellationToken: cancellationToken);
            if (!BotBodyRealisation.OldMessages.ContainsKey(userId)) BotBodyRealisation.OldMessages[userId] = new System.Collections.Generic.List<int>();
            BotBodyRealisation.OldMessages[userId].Add(sent.MessageId);
        }

        public async Task HandlePotionCallback(ITelegramBotClient botClient, CallbackQuery callbackQuery, CancellationToken cancellationToken)
        {
            var data = callbackQuery.Data ?? "";
            long chatId = callbackQuery.From.Id;

            if (!data.StartsWith("potion_"))
                return;

            try
            {
                if (data == "potion_cancel")
                {
                    await botClient.AnswerCallbackQueryAsync(callbackQuery.Id, "Скасовано", cancellationToken: cancellationToken);
                    return;
                }

                var parts = data.Split('_', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length != 3)
                {
                    await botClient.AnswerCallbackQueryAsync(callbackQuery.Id, "Невірні дані", cancellationToken: cancellationToken);
                    return;
                }

                bool isMana = parts[1] == "mana";
                if (!int.TryParse(parts[2], out int percent) || percent <= 0)
                {
                    await botClient.AnswerCallbackQueryAsync(callbackQuery.Id, "Невірний відсоток", cancellationToken: cancellationToken);
                    return;
                }

                int charId = UserData.GetActiveCharacterId(chatId);

                if (isMana)
                {
                    int current = ManaData.DataBaseValueReader<int>(chatId, charId, "CurrentMana");
                    int max = ManaData.DataBaseValueReader<int>(chatId, charId, "MaxMana");

                    int restore = (int)Math.Ceiling(max * percent / 100.0);
                    int newCurrent = Math.Min(max, current + restore);

                    ManaData.UpdateMagicStat(chatId, charId, "CurrentMana", newCurrent);

                    await botClient.AnswerCallbackQueryAsync(callbackQuery.Id, $"Відновлено {percent}% мани ({restore}).", cancellationToken: cancellationToken);
                    await botClient.SendTextMessageAsync(chatId, $"Застосовано зілля: відновлено {percent}% мани ({restore}). Поточна мана: {newCurrent}/{max}.");
                    _logger.LogInformation("User {UserId} used mana potion {Percent}%", chatId, percent);
                }
                else  
                {
                    int current = UserData.DataBaseValueReader<int>(chatId, charId, "Health");
                    int max = UserData.DataBaseValueReader<int>(chatId, charId, "MaxHP");
                    int tempHp = UserData.DataBaseValueReader<int>(chatId, charId, "TempHP");

                    int deficit = max - current; 
                    if (deficit <= 0)
                    {
                        await botClient.AnswerCallbackQueryAsync(callbackQuery.Id, "Нічого відновлювати.", cancellationToken: cancellationToken);
                        return;
                    }

                    int restore = (int)Math.Ceiling(deficit * (percent / 100.0));
                    int newCurrent = Math.Min(max, current + restore);

                    UserData.UpdateDataBaseValue("Health", chatId, charId, newCurrent);

                    await botClient.AnswerCallbackQueryAsync(callbackQuery.Id, $"Відновлено {percent}% здоров'я ({restore}).", cancellationToken: cancellationToken);
                    string healthDisplay = UserData.BuildHealthDisplay(newCurrent, max, tempHp);
                    await botClient.SendTextMessageAsync(chatId, $"Застосовано зілля: відновлено {percent}% здоров'я ({restore}). Поточне здоров'я: {healthDisplay}.");
                    _logger.LogInformation("User {UserId} used hp potion {Percent}%", chatId, percent);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling potion callback for user {UserId}", chatId);
                try
                {
                    await botClient.AnswerCallbackQueryAsync(callbackQuery.Id, "Помилка при застосуванні зілля", cancellationToken: cancellationToken);
                }
                catch (Exception answerEx)
                {
                    _logger.LogWarning(answerEx, "Failed to answer potion callback with error message. CallbackId={CallbackId}, UserId={UserId}", callbackQuery.Id, chatId);
                }
            }
        }
    }
}
