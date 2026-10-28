using DNDBot.BotSystem;
using DNDBot.MonsterSystem;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace DNDBot
{
    public class MonsterRealisation : BotBodyRealisation
    {
        private const int TemplatePageSize = 8;
        private const string TemplateListPagePrefix = "monster_template_list_page_";
        private const string TemplateSpawnPagePrefix = "monster_template_spawn_page_";
        private const string TemplateDetailsPrefix = "monster_template_details_";
        private readonly ILogger<MonsterRealisation> _logger;

        public MonsterRealisation()
            : this(NullLogger<MonsterRealisation>.Instance)
        {
        }

        public MonsterRealisation(ILogger<MonsterRealisation> logger)
        {
            _logger = logger ?? NullLogger<MonsterRealisation>.Instance;
        }

        public async Task Monster(
            ITelegramBotClient botClient,
            Update update,
            CancellationToken cancellationToken,
            Message message,
            ReplyKeyboardMarkup replyKeyboard,
            BotBodyRealisation botBodyRealisation)
        {
            if (message?.From == null)
                return;

            long userId = message.From.Id;

            if (userId != masterID)
                return;

            if (MonsterTemplateState.TryGetValue(userId, out string templateState) &&
                templateState == "count")
            {
                await HandleTemplateSpawnCountInput(botClient, message, cancellationToken);
                return;
            }

            switch (message.Text)
            {
                case "Очистити":
                    DeleteAllOldMessages(botClient, cancellationToken, userId);
                    Monsters.Clear();
                    MonsterID = 1;
                    MonsterCount = 1;
                    userState.Clear();
                    InitiativeUsers.Clear();
                    Initiative.Clear();
                    FightCharacter.Clear();
                    MonsterTemplateState.Remove(userId);
                    SelectedMonsterTemplate.Remove(userId);

                    await botClient.SendTextMessageAsync(
                        message.Chat.Id,
                        "Всі монстри видалені!",
                        cancellationToken: cancellationToken);
                    return;

                case "Очистити ініціативу гравців":
                    InitiativeUsers.Clear();
                    Initiative.Clear();
                    FightCharacter.Clear();
                    await botClient.SendTextMessageAsync(
                        message.Chat.Id,
                        "Ініціатива гравців видалена!",
                        cancellationToken: cancellationToken);
                    return;

                case "Створити монстра":
                    userState.TryAdd(MonsterCount, "start");

                    await botClient.SendTextMessageAsync(
                        message.Chat.Id,
                        "*УВАГА - Якщо при написанні значення для монстрів буде якийсь інший символ, окрім цифри (або значення більше 2 мільярдів) - Бот оброблювати НЕ БУДЕ!*",
                        parseMode: ParseMode.Markdown,
                        cancellationToken: cancellationToken);

                    await StartMonsterCreation.StartVoid(botClient, message, cancellationToken);
                    return;

                case "Шаблони монстрів":
                    await SendMonsterTemplateMenu(botClient, message.Chat.Id, cancellationToken);
                    return;
            }

            if (!userState.TryGetValue(MonsterCount, out string state))
                return;

            OldMessages.TryAdd(masterID, [message.MessageId]);

            switch (state)
            {
                case "start":
                    await StartMonsterCreation.StartVoid(botClient, message, cancellationToken);
                    break;
                case "name":
                    await NameMonsterCreation.NameVoid(botClient, message, botBodyRealisation, cancellationToken);
                    break;
                case "health":
                    await HealthMonsterCreation.HealthVoid(botClient, message, botBodyRealisation, cancellationToken);
                    break;
                case "Expirience":
                    await ExpirienceMonsterCreation.ExpirienceVoid(botClient, message, botBodyRealisation, cancellationToken);
                    break;
                case "Armor":
                    await ArmorMonsterCreation.ArmorVoid(botClient, message, replyKeyboard, botBodyRealisation, cancellationToken);
                    break;
                case "AttackPrompt":
                    await botClient.SendTextMessageAsync(
                        message.Chat.Id,
                        "Оберіть кнопку 'Так' або 'Ні' для атаки монстра.",
                        cancellationToken: cancellationToken);
                    break;
                case "AttackName":
                    await MonsterAttackCreation.HandleAttackNameInput(botClient, message, cancellationToken);
                    break;
                case "AttackDiceSelect":
                    await botClient.SendTextMessageAsync(
                        message.Chat.Id,
                        "Оберіть кубик через кнопку під повідомленням.",
                        cancellationToken: cancellationToken);
                    break;
                case "AttackCount":
                    await MonsterAttackCreation.HandleAttackCountInput(botClient, message, cancellationToken);
                    break;
                case "Delete":
                    await StopMonsterCreation.DeleteVoid(botClient, message, replyKeyboard, botBodyRealisation, cancellationToken);
                    break;
                case "Count":
                    await MakeNewMonsterCreation.MakeNewMonster(botClient, message, replyKeyboard, botBodyRealisation, cancellationToken);
                    break;
            }
        }

        public async Task HandleMonsterCallback(
            ITelegramBotClient botClient,
            CallbackQuery callbackQuery,
            CancellationToken cancellationToken,
            ReplyKeyboardMarkup replyKeyboard,
            BotBodyRealisation botBodyRealisation)
        {
            if (callbackQuery == null)
                return;

            string data = callbackQuery.Data ?? "";
            long chatId = callbackQuery.Message?.Chat?.Id ?? callbackQuery.From.Id;
            long stateOwnerId = callbackQuery.From.Id;

            try
            {
                if (callbackQuery.From.Id != masterID && callbackQuery.From.Id != ownerID)
                {
                    await botClient.AnswerCallbackQueryAsync(
                        callbackQuery.Id,
                        "Тільки майстер керує монстрами.",
                        cancellationToken: cancellationToken);
                    return;
                }

                if (await MonsterAttackCreation.HandleAttackCallback(
                    botClient,
                    stateOwnerId,
                    chatId,
                    data,
                    cancellationToken))
                {
                    await botClient.AnswerCallbackQueryAsync(callbackQuery.Id, cancellationToken: cancellationToken);
                    return;
                }

                if (data == "monster_keep" || data == "monster_delete" || data == "monster_keep_template")
                {
                    await StopMonsterCreation.HandleMonsterDeleteCallback(
                        botClient,
                        chatId,
                        data,
                        replyKeyboard,
                        botBodyRealisation,
                        cancellationToken);

                    await botClient.AnswerCallbackQueryAsync(callbackQuery.Id, cancellationToken: cancellationToken);
                    return;
                }

                if (data == "monster_template_menu")
                {
                    await SendMonsterTemplateMenu(botClient, chatId, cancellationToken);
                    await botClient.AnswerCallbackQueryAsync(callbackQuery.Id, cancellationToken: cancellationToken);
                    return;
                }

                if (data == "monster_template_list")
                {
                    await SendTemplateListPage(botClient, callbackQuery, chatId, 0, cancellationToken);
                    await botClient.AnswerCallbackQueryAsync(callbackQuery.Id, cancellationToken: cancellationToken);
                    return;
                }

                if (data.StartsWith(TemplateListPagePrefix, StringComparison.Ordinal))
                {
                    if (!TryParsePageCallback(data, TemplateListPagePrefix, out int page))
                    {
                        await botClient.AnswerCallbackQueryAsync(callbackQuery.Id, "Невірна сторінка.", cancellationToken: cancellationToken);
                        return;
                    }

                    await SendTemplateListPage(botClient, callbackQuery, chatId, page, cancellationToken);
                    await botClient.AnswerCallbackQueryAsync(callbackQuery.Id, cancellationToken: cancellationToken);
                    return;
                }

                if (data.StartsWith(TemplateDetailsPrefix, StringComparison.Ordinal))
                {
                    if (!TryParseTemplateDetailsCallback(data, out int templateId, out char sourceMode, out int sourcePage))
                    {
                        await botClient.AnswerCallbackQueryAsync(callbackQuery.Id, "Невірні дані шаблону.", cancellationToken: cancellationToken);
                        return;
                    }

                    var template = MonsterTemplateData.GetTemplateById(templateId);
                    if (template == null)
                    {
                        await botClient.AnswerCallbackQueryAsync(callbackQuery.Id, "Шаблон не знайдено.", cancellationToken: cancellationToken);
                        return;
                    }

                    await SendTemplateDetails(botClient, callbackQuery, chatId, template, sourceMode, sourcePage, cancellationToken);
                    await botClient.AnswerCallbackQueryAsync(callbackQuery.Id, cancellationToken: cancellationToken);
                    return;
                }

                if (data == "monster_template_spawn_menu")
                {
                    await SendTemplateSpawnPage(botClient, callbackQuery, chatId, 0, cancellationToken);
                    await botClient.AnswerCallbackQueryAsync(callbackQuery.Id, cancellationToken: cancellationToken);
                    return;
                }

                if (data.StartsWith(TemplateSpawnPagePrefix, StringComparison.Ordinal))
                {
                    if (!TryParsePageCallback(data, TemplateSpawnPagePrefix, out int page))
                    {
                        await botClient.AnswerCallbackQueryAsync(callbackQuery.Id, "Невірна сторінка.", cancellationToken: cancellationToken);
                        return;
                    }

                    await SendTemplateSpawnPage(botClient, callbackQuery, chatId, page, cancellationToken);
                    await botClient.AnswerCallbackQueryAsync(callbackQuery.Id, cancellationToken: cancellationToken);
                    return;
                }

                const string spawnPrefix = "monster_template_spawn_";
                if (data.StartsWith(spawnPrefix, StringComparison.Ordinal))
                {
                    string idPart = data[spawnPrefix.Length..];
                    if (!int.TryParse(idPart, out int templateId))
                    {
                        await botClient.AnswerCallbackQueryAsync(
                            callbackQuery.Id,
                            "Невірний шаблон.",
                            cancellationToken: cancellationToken);
                        return;
                    }

                    var template = MonsterTemplateData.GetTemplateById(templateId);
                    if (template == null)
                    {
                        await botClient.AnswerCallbackQueryAsync(
                            callbackQuery.Id,
                            "Шаблон не знайдено.",
                            cancellationToken: cancellationToken);
                        return;
                    }

                    SelectedMonsterTemplate[stateOwnerId] = template.TemplateId;
                    MonsterTemplateState[stateOwnerId] = "count";

                    await botClient.SendTextMessageAsync(
                        chatId,
                        $"Шаблон '{template.Name}' обрано. Скільки монстрів створити?",
                        cancellationToken: cancellationToken);

                    await botClient.AnswerCallbackQueryAsync(callbackQuery.Id, cancellationToken: cancellationToken);
                }
            }
            catch (Telegram.Bot.Exceptions.ApiRequestException apiEx)
            {
                _logger.LogError(
                    apiEx,
                    "Telegram API error in monster callback. UserId={UserId}, ChatId={ChatId}, CallbackData={CallbackData}, ErrorCode={ErrorCode}, Description={Description}",
                    callbackQuery.From?.Id,
                    chatId,
                    data,
                    apiEx.ErrorCode,
                    apiEx.Message);

                await SafeAnswerMonsterCallback(botClient, callbackQuery, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Unhandled error in monster callback. UserId={UserId}, ChatId={ChatId}, CallbackData={CallbackData}",
                    callbackQuery.From?.Id,
                    chatId,
                    data);

                await SafeAnswerMonsterCallback(botClient, callbackQuery, cancellationToken);
            }
        }

        public static int GetNextMonsterID()
        {
            var existingIDs = Monsters
                .Where(m => m != null)
                .Select(m => m.ID)
                .OrderBy(id => id)
                .ToList();

            int newID = 1;
            foreach (int id in existingIDs)
            {
                if (id == newID)
                    newID++;
                else
                    break;
            }

            return newID;
        }

        private static async Task HandleTemplateSpawnCountInput(
            ITelegramBotClient botClient,
            Message message,
            CancellationToken cancellationToken)
        {
            long userId = message.From?.Id ?? 0;
            if (userId == 0)
                return;

            if (!SelectedMonsterTemplate.TryGetValue(userId, out int templateId))
            {
                MonsterTemplateState.Remove(userId);
                await botClient.SendTextMessageAsync(message.Chat.Id, "Шаблон не обрано.", cancellationToken: cancellationToken);
                return;
            }

            if (!int.TryParse(message.Text, out int count) || count <= 0)
            {
                await botClient.SendTextMessageAsync(
                    message.Chat.Id,
                    "Введіть коректне число більше 0.",
                    cancellationToken: cancellationToken);
                return;
            }

            if (count > 200)
            {
                await botClient.SendTextMessageAsync(
                    message.Chat.Id,
                    "Забагато монстрів за раз (максимум 200).",
                    cancellationToken: cancellationToken);
                return;
            }

            var template = MonsterTemplateData.GetTemplateById(templateId);
            if (template == null)
            {
                MonsterTemplateState.Remove(userId);
                SelectedMonsterTemplate.Remove(userId);
                await botClient.SendTextMessageAsync(message.Chat.Id, "Шаблон не знайдено.", cancellationToken: cancellationToken);
                return;
            }

            for (int i = 0; i < count; i++)
            {
                var monster = new MonsterCreator
                {
                    ID = GetNextMonsterID(),
                    Name = template.Name,
                    HP = template.HP,
                    MaxHP = template.HP,
                    Armour = template.Armour,
                    XP = template.XP,
                    Initiative = BotMath.MathfDND.DiceReturn(20),
                    AttackName = template.AttackName,
                    AttackDamageDice = template.AttackDamageDice
                };

                Monsters.Add(monster);
                MonsterCount++;
            }

            MonsterTemplateState.Remove(userId);
            SelectedMonsterTemplate.Remove(userId);

            await botClient.SendTextMessageAsync(
                message.Chat.Id,
                $"Створено {count} монстр(ів) із шаблону '{template.Name}'.",
                cancellationToken: cancellationToken);
        }

        private static async Task SendMonsterTemplateMenu(
            ITelegramBotClient botClient,
            long chatId,
            CancellationToken cancellationToken)
        {
            await botClient.SendTextMessageAsync(
                chatId,
                "Керування шаблонами монстрів:",
                replyMarkup: new InlineKeyboardMarkup(new[]
                {
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData("📋 Показати шаблони", "monster_template_list")
                    },
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData("➕ Створити з шаблону", "monster_template_spawn_menu")
                    }
                }),
                cancellationToken: cancellationToken);
        }

        private async Task SendTemplateListPage(
            ITelegramBotClient botClient,
            CallbackQuery callbackQuery,
            long chatId,
            int requestedPage,
            CancellationToken cancellationToken)
        {
            var templates = MonsterTemplateData.GetTemplates();
            if (templates.Count == 0)
            {
                await SendOrEditMessage(
                    botClient,
                    callbackQuery,
                    chatId,
                    "Шаблонів поки немає. Створи монстра і натисни 'Залишити + шаблон'.",
                    new InlineKeyboardMarkup(new[]
                    {
                        new[] { InlineKeyboardButton.WithCallbackData("↩️ До меню", "monster_template_menu") }
                    }),
                    cancellationToken);
                return;
            }

            IReadOnlyList<MonsterTemplateRecord> pageItems = GetTemplatePage(templates, requestedPage, out int page, out int totalPages);
            string text =
                $"📋 Шаблони монстрів ({page + 1}/{totalPages})\n" +
                "Натисни на шаблон, щоб подивитися деталі.";

            await SendOrEditMessage(
                botClient,
                callbackQuery,
                chatId,
                text,
                BuildTemplateListKeyboard(pageItems, page, totalPages),
                cancellationToken);
        }

        private async Task SendTemplateSpawnPage(
            ITelegramBotClient botClient,
            CallbackQuery callbackQuery,
            long chatId,
            int requestedPage,
            CancellationToken cancellationToken)
        {
            var templates = MonsterTemplateData.GetTemplates();
            if (templates.Count == 0)
            {
                await SendOrEditMessage(
                    botClient,
                    callbackQuery,
                    chatId,
                    "Шаблонів поки немає.",
                    new InlineKeyboardMarkup(new[]
                    {
                        new[] { InlineKeyboardButton.WithCallbackData("↩️ До меню", "monster_template_menu") }
                    }),
                    cancellationToken);
                return;
            }

            IReadOnlyList<MonsterTemplateRecord> pageItems = GetTemplatePage(templates, requestedPage, out int page, out int totalPages);
            string text =
                $"➕ Вибір шаблону ({page + 1}/{totalPages})\n" +
                "Натисни назву, щоб обрати шаблон, або ℹ️ щоб подивитися деталі.";

            await SendOrEditMessage(
                botClient,
                callbackQuery,
                chatId,
                text,
                BuildTemplateSpawnKeyboard(pageItems, page, totalPages),
                cancellationToken);
        }

        private async Task SendTemplateDetails(
            ITelegramBotClient botClient,
            CallbackQuery callbackQuery,
            long chatId,
            MonsterTemplateRecord template,
            char sourceMode,
            int sourcePage,
            CancellationToken cancellationToken)
        {
            string attack = string.IsNullOrWhiteSpace(template.AttackDamageDice)
                ? "немає"
                : $"{(string.IsNullOrWhiteSpace(template.AttackName) ? "Атака" : template.AttackName)} ({template.AttackDamageDice})";

            string text =
                $"📄 Шаблон монстра\n\n" +
                $"ID шаблону: {template.TemplateId}\n" +
                $"Ім'я: {template.Name}\n" +
                $"HP: {template.HP}\n" +
                $"AC: {template.Armour}\n" +
                $"XP: {template.XP}\n" +
                $"Атака: {attack}";

            await SendOrEditMessage(
                botClient,
                callbackQuery,
                chatId,
                text,
                BuildTemplateDetailsKeyboard(template.TemplateId, sourceMode, sourcePage),
                cancellationToken);
        }

        private static IReadOnlyList<MonsterTemplateRecord> GetTemplatePage(
            IReadOnlyList<MonsterTemplateRecord> templates,
            int requestedPage,
            out int normalizedPage,
            out int totalPages)
        {
            totalPages = Math.Max(1, (int)Math.Ceiling(templates.Count / (double)TemplatePageSize));
            normalizedPage = Math.Clamp(requestedPage, 0, totalPages - 1);

            int skip = normalizedPage * TemplatePageSize;
            return templates.Skip(skip).Take(TemplatePageSize).ToList();
        }

        private static InlineKeyboardMarkup BuildTemplateListKeyboard(
            IReadOnlyList<MonsterTemplateRecord> pageItems,
            int page,
            int totalPages)
        {
            var rows = new List<InlineKeyboardButton[]>();

            foreach (var template in pageItems)
            {
                rows.Add(new[]
                {
                    InlineKeyboardButton.WithCallbackData(
                        $"📄 {ShortLabel(template.Name, 30)}",
                        $"{TemplateDetailsPrefix}{template.TemplateId}_l_{page}")
                });
            }

            AddPaginationRow(rows, page, totalPages, TemplateListPagePrefix);

            rows.Add(new[] { InlineKeyboardButton.WithCallbackData("➕ Створити з шаблону", "monster_template_spawn_menu") });
            rows.Add(new[] { InlineKeyboardButton.WithCallbackData("↩️ До меню", "monster_template_menu") });

            return new InlineKeyboardMarkup(rows);
        }

        private static InlineKeyboardMarkup BuildTemplateSpawnKeyboard(
            IReadOnlyList<MonsterTemplateRecord> pageItems,
            int page,
            int totalPages)
        {
            var rows = new List<InlineKeyboardButton[]>();

            foreach (var template in pageItems)
            {
                rows.Add(new[]
                {
                    InlineKeyboardButton.WithCallbackData(
                        $"⚔️ {ShortLabel(template.Name, 24)}",
                        $"monster_template_spawn_{template.TemplateId}"),
                    InlineKeyboardButton.WithCallbackData(
                        "ℹ️",
                        $"{TemplateDetailsPrefix}{template.TemplateId}_s_{page}")
                });
            }

            AddPaginationRow(rows, page, totalPages, TemplateSpawnPagePrefix);

            rows.Add(new[] { InlineKeyboardButton.WithCallbackData("📋 Перегляд шаблонів", "monster_template_list") });
            rows.Add(new[] { InlineKeyboardButton.WithCallbackData("↩️ До меню", "monster_template_menu") });

            return new InlineKeyboardMarkup(rows);
        }

        private static InlineKeyboardMarkup BuildTemplateDetailsKeyboard(int templateId, char sourceMode, int sourcePage)
        {
            string backCallback = sourceMode == 's'
                ? $"{TemplateSpawnPagePrefix}{sourcePage}"
                : $"{TemplateListPagePrefix}{sourcePage}";

            string selectText = sourceMode == 's'
                ? "✅ Обрати цей шаблон"
                : "➕ Створити з шаблону";

            return new InlineKeyboardMarkup(new[]
            {
                new[] { InlineKeyboardButton.WithCallbackData(selectText, $"monster_template_spawn_{templateId}") },
                new[] { InlineKeyboardButton.WithCallbackData("⬅️ Назад", backCallback) },
                new[] { InlineKeyboardButton.WithCallbackData("↩️ До меню", "monster_template_menu") }
            });
        }

        private static void AddPaginationRow(List<InlineKeyboardButton[]> rows, int page, int totalPages, string prefix)
        {
            var navButtons = new List<InlineKeyboardButton>();
            if (page > 0)
            {
                navButtons.Add(InlineKeyboardButton.WithCallbackData("⬅️ Назад", $"{prefix}{page - 1}"));
            }

            if (page < totalPages - 1)
            {
                navButtons.Add(InlineKeyboardButton.WithCallbackData("Далі ➡️", $"{prefix}{page + 1}"));
            }

            if (navButtons.Count > 0)
            {
                rows.Add(navButtons.ToArray());
            }
        }

        private static bool TryParsePageCallback(string data, string prefix, out int page)
        {
            page = 0;
            if (!data.StartsWith(prefix, StringComparison.Ordinal))
                return false;

            return int.TryParse(data[prefix.Length..], out page) && page >= 0;
        }

        private static bool TryParseTemplateDetailsCallback(string data, out int templateId, out char sourceMode, out int sourcePage)
        {
            templateId = 0;
            sourceMode = 'l';
            sourcePage = 0;

            if (!data.StartsWith(TemplateDetailsPrefix, StringComparison.Ordinal))
                return false;

            string tail = data[TemplateDetailsPrefix.Length..];
            string[] parts = tail.Split('_');
            if (parts.Length != 3)
                return false;

            if (!int.TryParse(parts[0], out templateId) || templateId <= 0)
                return false;

            if (parts[1].Length != 1)
                return false;

            sourceMode = parts[1][0];
            if (sourceMode != 'l' && sourceMode != 's')
                return false;

            if (!int.TryParse(parts[2], out sourcePage) || sourcePage < 0)
                return false;

            return true;
        }

        private static string ShortLabel(string value, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "Без назви";

            string clean = value.Trim();
            if (clean.Length <= maxLength)
                return clean;

            return clean[..Math.Max(1, maxLength - 1)] + "…";
        }

        private async Task SendOrEditMessage(
            ITelegramBotClient botClient,
            CallbackQuery callbackQuery,
            long chatId,
            string text,
            InlineKeyboardMarkup replyMarkup,
            CancellationToken cancellationToken)
        {
            if (callbackQuery?.Message != null)
            {
                try
                {
                    await botClient.EditMessageTextAsync(
                        chatId: chatId,
                        messageId: callbackQuery.Message.MessageId,
                        text: text,
                        replyMarkup: replyMarkup,
                        cancellationToken: cancellationToken);
                    return;
                }
                catch (ApiRequestException ex)
                {
                    string msg = ex.Message ?? string.Empty;
                    if (msg.Contains("message is not modified", StringComparison.OrdinalIgnoreCase))
                        return;

                    if (!msg.Contains("message to edit not found", StringComparison.OrdinalIgnoreCase) &&
                        !msg.Contains("message can't be edited", StringComparison.OrdinalIgnoreCase))
                    {
                        _logger.LogWarning(
                            ex,
                            "Failed to edit monster template message. ChatId={ChatId}, MessageId={MessageId}, Data={CallbackData}",
                            chatId,
                            callbackQuery.Message.MessageId,
                            callbackQuery.Data);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(
                        ex,
                        "Unexpected edit failure in monster template message. ChatId={ChatId}, MessageId={MessageId}, Data={CallbackData}",
                        chatId,
                        callbackQuery.Message.MessageId,
                        callbackQuery.Data);
                }
            }

            await botClient.SendTextMessageAsync(
                chatId,
                text,
                replyMarkup: replyMarkup,
                cancellationToken: cancellationToken);
        }

        private async Task SafeAnswerMonsterCallback(
            ITelegramBotClient botClient,
            CallbackQuery callbackQuery,
            CancellationToken cancellationToken)
        {
            try
            {
                await botClient.AnswerCallbackQueryAsync(callbackQuery.Id, cancellationToken: cancellationToken);
            }
            catch (Telegram.Bot.Exceptions.ApiRequestException apiEx) when ((apiEx.Message ?? string.Empty).Contains("query is too old", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogDebug("Callback query is too old while answering monster callback. CallbackId={CallbackId}", callbackQuery.Id);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Failed to answer monster callback. CallbackId={CallbackId}, UserId={UserId}, Data={CallbackData}",
                    callbackQuery.Id,
                    callbackQuery.From?.Id,
                    callbackQuery.Data);
            }
        }
    }
}
