using DNDBot.AttackSystem;
using DNDBot.BotMath;
using DNDBot.BotSystem;
using DNDBot.CommandSystem;
using DNDBot.DataBase;
using DNDBot.MonsterSystem;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.AccessControl;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using System.Text.RegularExpressions;
using System.Globalization;
using DNDBot.Goverment;
using DNDBot.Goverment.Economy;

namespace DNDBot
{
    class ComandsRealisation : BotBodyRealisation
    {

        public readonly ILogger<ComandsRealisation> _logger;
        public readonly SubCommandsPlayerBattle _subCommandsPlayerBattle;
        public readonly SubUserComandsRealisation _subUserComandsRealisation;  
        public readonly CallBackWalletCommands _callBackWalletCommands;  
        public readonly PercentModifierHandler _percentModifierHandler;  
        public readonly StateAdministration _stateAdministration;  
        public readonly FightTask _fightTask;  
        public readonly LevelSystem _levelSystem;
        public readonly MonsterRealisation _monsterRealisation;
        private static readonly Dictionary<long, CombatSessionState> CombatSessions = new();

        private sealed class CombatTurnEntry
        {
            public long Id { get; init; }
            public int CharacterId { get; init; }
            public string Name { get; init; } = "";
            public int Initiative { get; init; }
        }

        private sealed class CombatSessionState
        {
            public bool IsActive { get; set; }
            public int Round { get; set; } = 1;
            public int TurnIndex { get; set; }
            public List<CombatTurnEntry> Order { get; set; } = new();
        }

        public static bool IsCombatSessionActive(long chatId)
            => CombatSessions.TryGetValue(chatId, out var session) && session.IsActive;

        public static InlineKeyboardMarkup GetCombatPanelMarkup(long chatId)
            => InlineKeyboardsClass.CombatControlKeyboard(IsCombatSessionActive(chatId));


        public ComandsRealisation(ILogger<ComandsRealisation> logger, SubCommandsPlayerBattle subCommandsPlayerBattle, SubUserComandsRealisation subUserComandsRealisation, 
            CallBackWalletCommands callBackWalletCommands, FightTask fightTask, LevelSystem levelSystem, PercentModifierHandler percentModifierHandler, StateAdministration stateAdministration, MonsterRealisation monsterRealisation)
        {
            _logger = logger;
            _subCommandsPlayerBattle = subCommandsPlayerBattle;
            _subUserComandsRealisation = subUserComandsRealisation;
            _callBackWalletCommands = callBackWalletCommands;
            _percentModifierHandler = percentModifierHandler;
            _stateAdministration = stateAdministration; 
            _fightTask = fightTask;
            _levelSystem = levelSystem;
            _monsterRealisation = monsterRealisation;
        }

        public async Task Commands(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken, Message message, ReplyKeyboardMarkup replyKeyboard)
        {
            if (message?.From == null)
                return;

            string msgText = message.Text ?? "";
            long userId = message.From.Id;
            bool isPrivateChat = message.Chat?.Type == ChatType.Private;
            BotBodyRealisation botBodyRealisation = new BotBodyRealisation();

            OldMessages.TryAdd(userId, new List<int>());

            if (!isProcessingAnotherProcess.ContainsKey(userId))
                isProcessingAnotherProcess.Add(userId, false);

            if (isProcessingAnotherProcess[userId])
                return;

            if (userId == masterID || userId == ownerID)
                await SendMessageToAll(botClient, message);

            if (userId == ownerID && msgText == "/RemoveInlineKeyboardButtons")
            {
                await botClient.SendTextMessageAsync(message.Chat.Id, $"Кнопки в клавіатурі були успішно видалені!", cancellationToken: cancellationToken, replyMarkup: new ReplyKeyboardRemove());
            }

            if (userId == masterID)
                if (message.Text == "Бій" || message.Text == "Заскочені гравці" || message.Text == "Заскочені монстри")
                    await _fightTask.Fight(botClient, cancellationToken, message, message.Text);

            if (!isPrivateChat && IsPrivateOnlyCommand(msgText))
            {
                await botClient.SendTextMessageAsync(
                    message.Chat.Id,
                    "Ця команда працює з особистими даними. Відповідь надіслано у приватний чат з ботом.",
                    cancellationToken: cancellationToken);
            }

            switch (msgText)  
            {
                case "/start":  
                    await StartBotCommands(botClient, message, replyKeyboard, cancellationToken);
                    break;
                case "Допомога":
                    await botClient.SendTextMessageAsync(message.Chat.Id,
        "Доступні команди бота (викликаються текстом у чаті):\n\n" +
         
        "- /start — почати роботу з ботом\n" +
        "- Допомога — показати цей список команд\n" +
        "- Інформація — інформація про вашого персонажа\n" +
        "- Перевірки — список можливих перевірок\n" +
         
        "- Ініціатива — кинути ініціативу\n" +
        "- Попадання — кинути попадання\n" +
        "- Рятунок від заклинань — кидок рятування від заклять\n" +
        "- Атака — використовується для атаки\n" +
        "- Вилікувати — використовувати лікування\n" +
        "- Бойова панель — кнопки керування боєм (старт, наступний хід, стан, завершення)\n" +
        "- Тимчасові хіти — додати/скинути тимчасові хіти через inline кнопки\n" +
        "- Тимчасове зниження ХП — тимчасово знизити або скинути max/current HP\n" +
        "- Бій — запустити бій (мастер)\n" +
        "- Заскочені гравці / Заскочені монстри — (мастер)\n" +
         
        "- Кубіки — меню кидка кубиків\n" +
        "- Зброя — список зброї\n" +
        "- Сутності — список бойових сутностей\n" +
        "- Шаблони монстрів — список/створення монстрів із збережених шаблонів (мастер)\n" +
        "- Інвентар — перевірити інвентар\n" +
        "- Ефекти — робота з ефектами/предметами (меню)\n" +
        "- Таблиці — корисні таблиці\n" +
         
        "- Мана — показати ваші магічні характеристики (має підменю для мастера: показ усіх гравців)\n" +
        "- Відновити ману <кількість> — додати вказану кількість до поточної мани активного персонажа\n" +
        "- Витратити ману <кількість> — списати вказану кількість мани з активного персонажа\n" +
        "- Відновити ману за хід — відновлення згідно RegenTurn (кнопка)\n" +
        "- Відновити ману після бою — відновлення згідно RegenAfterFight (кнопка)\n" +
        "- Зілля мани — швидкі кнопки відновлення у відсотках\n" +
        "- Зілля зцілення — швидкі кнопки для відновлення HP\n" +
         
        "- Гаманець — показати гаманець (вашу активну персону)\n" +
        "- Транзакції — меню операцій з монетами (обмін/переслати/додати/відняти)\n" +
        "- /exchange_rate — курс валюти країни\n" +
        "- /convert <amount> <currency1> <currency2> — конвертація валют\n" +
        "- /central_bank <country> — дані центробанку\n" +
        "- /adjust_rate <country> <new_rate> — (мастер) змінити курс\n" +
        "- /transfer <country1> <country2> <amount> — (мастер) трансфер між країнами\n" +
        "- /inflation <country> — інфляція країни\n" +
        "- /loan <country> <amount> — (мастер) позика\n" +
        "- /repay <country> <amount> — (мастер) погашення позики\n" +
        "- /black_market_rate <country> — чорний курс\n" +
         
        "- Змінити дані — меню для зміни значень персонажа\n" +
        "- Змінити процентні значення — налаштування процентних модифікаторів для мани/HP\n" +
        "- Додати досвід — додати XP для вашого персонажа\n" +
        "- Додати досвід до всіх — (мастер) додати XP усім гравцям\n" +
        "- Додати магічний досвід — додати MagicXP для вашого персонажа\n" +
        "- Додати магічний досвід до всіх — (мастер) додати MagicXP усім гравцям\n" +
        "- Додати інформацію до бота — (меню для додавання різних даних)\n" +
        "- Очистити свої дані — видалити ваші дані з БД\n" +
         
        "- Державне управління — інформація про стан (додаткове меню)\n" +
        "- Створити покірних і лояльних депутатів <кількість> — швидко створити лояльних депутатів (1-200, за замовчуванням 10)\n" +
        "- Надіслати: --Текст-- — відправити повідомлення всім (майстер/власник)\n" +
        "- /RemoveInlineKeyboardButtons — (власник) видалити клавіатуру\n" +
         
        "- Керувати інформацією бота — кнопка в клавіатурі\n" +
        "\nПримітки:\n" +
        "- Багато операцій відкривають підменю (inline клавіатури) — після вибору опції бот попросить додаткові дані.\n" +
        "- Команди для мастера помічені (мастер) — доступні тільки майстру гри.\n" +
        "- Для пересилання/обміну монет використовуйте 'Транзакції' і далі слідуйте підказкам.\n",
        cancellationToken: cancellationToken);
                    break;
                case "Меню":
                    {

                         
                        var replKeyboard = ButtonsCreate(userId, false);
                        var inlinKeyboard = BotKeyboardHelper.ConvertReplyKeyboardToInline(replKeyboard);
                        MiniButtonsCreate();

                            OldMessages[userId].Add((await botClient.SendTextMessageAsync(message.Chat.Id, "Невеличкі кнопочки знайшли місце у вас під клавіатурою :3", replyMarkup: MiniButtonsCreate(), cancellationToken: cancellationToken)).MessageId);
                            await botClient.SendTextMessageAsync(message.Chat.Id, "Меню всіх кнопочок:", replyMarkup: inlinKeyboard, cancellationToken: cancellationToken);
                    }
                    break;
                case "Інформація":
                    _logger.LogInformation("Command 'Інформація' invoked by Telegram user {UserId}", userId);
                    await _subUserComandsRealisation.GetUserInfo(botClient, message, userId, botBodyRealisation, cancellationToken);
                    break;
                case "Попадання":
                    await _subCommandsPlayerBattle.HitCheck(botClient, cancellationToken, message);
                    break;
                case "Ініціатива":
                    await _subCommandsPlayerBattle.PlayerInitiative(botClient, cancellationToken, message);
                    break;
                case "Рятунок від заклинань":
                    await _subCommandsPlayerBattle.RescueFromSpells(botClient, cancellationToken, message);
                    break;
                case "Зброя":
                    await _subCommandsPlayerBattle.WeaponList(botClient, cancellationToken, message.Chat.Id);
                    break;
                case "Монстри":
                case "Сутності":
                    await _subCommandsPlayerBattle.MonstersList(botClient, cancellationToken, message.Chat.Id);
                    break;
                case "Перевірки":
                    await botClient.SendTextMessageAsync(message.From.Id, $"Всі можливі перевірки: \n\n" + _subUserComandsRealisation.ReturnPossibleChecks(), cancellationToken: cancellationToken);
                    break;
                case "Очистити свої дані":
                    await botClient.SendTextMessageAsync(message.From.Id, $"{UserData.ClearUserData(message.From.Id)}", cancellationToken: cancellationToken);
                    break;
                case "Додати досвід":
                    await _subUserComandsRealisation.XpAdd(botClient, cancellationToken, message.From.Id);
                    break;
                case "Додати досвід до всіх":
                    if (userId == masterID)
                        await AddXpToAllPlayers(botClient, userId, cancellationToken);
                    break;
                case "Додати магічний досвід":
                    await _subUserComandsRealisation.MagicXpAdd(botClient, cancellationToken, message.From.Id);
                    break;
                case "Додати магічний досвід до всіх":
                    if (userId == masterID)
                        await AddXpToAllPlayers(botClient, userId, cancellationToken, true);
                    break;
                case "Транзакції":
                    InlineKeyboardMarkup inlineKeyboard = InlineKeyboardsClass.Transictions(message);
                    OldMessages[userId].Add((await botClient.SendTextMessageAsync(message.From.Id, $"Функції гаманеця:", replyMarkup: inlineKeyboard, cancellationToken: cancellationToken)).MessageId);
                    break;
                case "Гаманець":
                    await _subUserComandsRealisation.Wallet(botClient, cancellationToken, userId);
                    break;
                case "Змінити дані":
                    await botClient.SendTextMessageAsync(message.From.Id, $"Який модифікатор чи значення ви хочете змінити у ваших данних?", replyMarkup: DataChanger.GetChangingValueForUser(), cancellationToken: cancellationToken);
                    break;
                case "Закляття":
                case "Ефекти":
                    await botClient.SendTextMessageAsync(message.From.Id, $"Дії з ефектами (уміння/предмети):", replyMarkup: InlineKeyboardsClass.MagicInlineKeyboardMarkup(), cancellationToken: cancellationToken);
                    break;
                case "Таблиці":
                    await botClient.SendTextMessageAsync(message.From.Id, $"Всі можливі таблиці:", replyMarkup: InlineKeyboardsClass.TableInlineKeyboardMarkup(), cancellationToken: cancellationToken);
                    break;
                case "Атака":
                    await _subCommandsPlayerBattle.HealOrAttackTask(botClient, cancellationToken, message, true);
                    break;
                case "Вилікувати":
                    await _subCommandsPlayerBattle.HealOrAttackTask(botClient, cancellationToken, message, false);
                    break;
                case "Бойова панель":
                    await SendCombatPanel(botClient, message.Chat.Id, cancellationToken);
                    break;
                case "Тимчасові хіти":
                    await _subCommandsPlayerBattle.TemporaryHpTask(botClient, cancellationToken, message);
                    break;
                case "Тимчасове зниження ХП":
                    await _subCommandsPlayerBattle.TemporaryHpReductionTask(botClient, cancellationToken, message);
                    break;
                case "Кубіки":
                    OldMessages[userId].Add(message.MessageId);

                    await botClient.SendTextMessageAsync(message.Chat.Id, $"Який кубік ви хочете кинути?", replyMarkup: InlineKeyboardsClass.DiceInlineKeyboard(), cancellationToken: cancellationToken);
                    break;
                case "Мана":
                    if(userId != masterID) await botClient.SendTextMessageAsync(message.From.Id, BuildPlayerView(ManaData.GetMagicStats(userId,UserData.GetActiveCharacterId(userId))), cancellationToken: cancellationToken);
                    else if(UserData.GetDistinctUserCount(true) > 0) await botClient.SendTextMessageAsync(message.From.Id, BuildPlayerView(ManaData.GetAllPlayersMagicStats()), cancellationToken: cancellationToken); 
                        else await botClient.SendTextMessageAsync(message.From.Id, "Наразі не достатньо користувачів.", cancellationToken: cancellationToken);
                    break;
                case "Змінити процентні значення":
                    await _percentModifierHandler.StartModifyFlow(botClient, message, cancellationToken);
                    break;
                case "Зілля мани":
                    await _percentModifierHandler.ShowPotionKeyboard(botClient, message, true, cancellationToken);
                    break;
                case "Зілля зцілення":
                    await _percentModifierHandler.ShowPotionKeyboard(botClient, message, false, cancellationToken);
                    break;

                 
                case "Відновити ману за хід":
                    await ApplyRegenAsync(false);
                    break;
                case "Відновити ману після бою":
                    await ApplyRegenAsync(true);
                    break;
                case "Державне управління":
                    await _stateAdministration.ShowStateAdministration(botClient, userId, cancellationToken, message.Chat.Id);
                    break;
            }

             
            if (!string.IsNullOrWhiteSpace(msgText) && msgText.StartsWith("/", StringComparison.OrdinalIgnoreCase))
            {
                var parts = msgText.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                string cmd = parts[0].ToLowerInvariant();
                var currencyService = new CurrencyExchangeService();

                if (cmd == "/exchange_rate")
                {
                    int? countryId = parts.Length > 1
                        ? currencyService.ResolveCountryId(parts[1])
                        : StateAdministration.GetSelectedCountryIdForUser(userId);
                    if (!countryId.HasValue)
                    {
                        await botClient.SendTextMessageAsync(message.Chat.Id, "Країну не знайдено.", cancellationToken: cancellationToken);
                        return;
                    }
                    string text = currencyService.BuildRateSummary(countryId.Value);
                    text += "\n\n" + currencyService.BuildExampleTurnCalculation(countryId.Value);
                    await botClient.SendTextMessageAsync(message.Chat.Id, text, cancellationToken: cancellationToken);
                    return;
                }

                if (cmd == "/convert")
                {
                    if (parts.Length < 4)
                    {
                        await botClient.SendTextMessageAsync(message.Chat.Id, "Формат: /convert <amount> <currency1> <currency2>", cancellationToken: cancellationToken);
                        return;
                    }
                    if (!TryParseAmount(parts[1], out decimal amount))
                    {
                        await botClient.SendTextMessageAsync(message.Chat.Id, "Невірна сума.", cancellationToken: cancellationToken);
                        return;
                    }
                    string fromCode = currencyService.ResolveCurrencyCode(parts[2]);
                    string toCode = currencyService.ResolveCurrencyCode(parts[3]);
                    try
                    {
                        decimal result = currencyService.ConvertCurrency(amount, fromCode, toCode, useBlackMarket: false, out string details);
                        await botClient.SendTextMessageAsync(message.Chat.Id, $"Конвертація: {amount} {fromCode} = {result:0.####} {toCode}\n{details}", cancellationToken: cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        await botClient.SendTextMessageAsync(message.Chat.Id, $"Помилка: {ex.Message}", cancellationToken: cancellationToken);
                    }
                    return;
                }

                if (cmd == "/central_bank")
                {
                    int? countryId = parts.Length > 1
                        ? currencyService.ResolveCountryId(parts[1])
                        : StateAdministration.GetSelectedCountryIdForUser(userId);
                    if (!countryId.HasValue)
                    {
                        await botClient.SendTextMessageAsync(message.Chat.Id, "Країну не знайдено.", cancellationToken: cancellationToken);
                        return;
                    }
                    string text = currencyService.BuildCentralBankView(countryId.Value);
                    await botClient.SendTextMessageAsync(message.Chat.Id, text, cancellationToken: cancellationToken);
                    return;
                }

                if (cmd == "/adjust_rate")
                {
                    if (userId != masterID)
                    {
                        await botClient.SendTextMessageAsync(message.Chat.Id, "Нема доступу.", cancellationToken: cancellationToken);
                        return;
                    }
                    if (parts.Length < 3 || !TryParseAmount(parts[2], out decimal newRate))
                    {
                        await botClient.SendTextMessageAsync(message.Chat.Id, "Формат: /adjust_rate <country> <new_rate>", cancellationToken: cancellationToken);
                        return;
                    }
                    int? countryId = currencyService.ResolveCountryId(parts[1]);
                    if (!countryId.HasValue)
                    {
                        await botClient.SendTextMessageAsync(message.Chat.Id, "Країну не знайдено.", cancellationToken: cancellationToken);
                        return;
                    }
                    string result = currencyService.AdjustRate(countryId.Value, newRate);
                    await botClient.SendTextMessageAsync(message.Chat.Id, result, cancellationToken: cancellationToken);
                    return;
                }

                if (cmd == "/transfer")
                {
                    if (userId != masterID)
                    {
                        await botClient.SendTextMessageAsync(message.Chat.Id, "Нема доступу.", cancellationToken: cancellationToken);
                        return;
                    }
                    if (parts.Length < 4 || !TryParseAmount(parts[3], out decimal amount))
                    {
                        await botClient.SendTextMessageAsync(message.Chat.Id, "Формат: /transfer <country1> <country2> <amount>", cancellationToken: cancellationToken);
                        return;
                    }
                    int? fromId = currencyService.ResolveCountryId(parts[1]);
                    int? toId = currencyService.ResolveCountryId(parts[2]);
                    if (!fromId.HasValue || !toId.HasValue)
                    {
                        await botClient.SendTextMessageAsync(message.Chat.Id, "Країну не знайдено.", cancellationToken: cancellationToken);
                        return;
                    }
                    string result = currencyService.Transfer(fromId.Value, toId.Value, amount, userId);
                    await botClient.SendTextMessageAsync(message.Chat.Id, result, cancellationToken: cancellationToken);
                    return;
                }

                if (cmd == "/inflation")
                {
                    int? countryId = parts.Length > 1
                        ? currencyService.ResolveCountryId(parts[1])
                        : StateAdministration.GetSelectedCountryIdForUser(userId);
                    if (!countryId.HasValue)
                    {
                        await botClient.SendTextMessageAsync(message.Chat.Id, "Країну не знайдено.", cancellationToken: cancellationToken);
                        return;
                    }
                    string text = currencyService.BuildInflationView(countryId.Value);
                    await botClient.SendTextMessageAsync(message.Chat.Id, text, cancellationToken: cancellationToken);
                    return;
                }

                if (cmd == "/loan")
                {
                    if (userId != masterID)
                    {
                        await botClient.SendTextMessageAsync(message.Chat.Id, "Нема доступу.", cancellationToken: cancellationToken);
                        return;
                    }
                    if (parts.Length < 3 || !TryParseAmount(parts[2], out decimal amount))
                    {
                        await botClient.SendTextMessageAsync(message.Chat.Id, "Формат: /loan <country> <amount>", cancellationToken: cancellationToken);
                        return;
                    }
                    int? countryId = currencyService.ResolveCountryId(parts[1]);
                    if (!countryId.HasValue)
                    {
                        await botClient.SendTextMessageAsync(message.Chat.Id, "Країну не знайдено.", cancellationToken: cancellationToken);
                        return;
                    }
                    string result = currencyService.Loan(countryId.Value, amount, userId);
                    await botClient.SendTextMessageAsync(message.Chat.Id, result, cancellationToken: cancellationToken);
                    return;
                }

                if (cmd == "/repay")
                {
                    if (userId != masterID)
                    {
                        await botClient.SendTextMessageAsync(message.Chat.Id, "Нема доступу.", cancellationToken: cancellationToken);
                        return;
                    }
                    if (parts.Length < 3 || !TryParseAmount(parts[2], out decimal amount))
                    {
                        await botClient.SendTextMessageAsync(message.Chat.Id, "Формат: /repay <country> <amount>", cancellationToken: cancellationToken);
                        return;
                    }
                    int? countryId = currencyService.ResolveCountryId(parts[1]);
                    if (!countryId.HasValue)
                    {
                        await botClient.SendTextMessageAsync(message.Chat.Id, "Країну не знайдено.", cancellationToken: cancellationToken);
                        return;
                    }
                    string result = currencyService.Repay(countryId.Value, amount, userId);
                    await botClient.SendTextMessageAsync(message.Chat.Id, result, cancellationToken: cancellationToken);
                    return;
                }

                if (cmd == "/black_market_rate")
                {
                    int? countryId = parts.Length > 1
                        ? currencyService.ResolveCountryId(parts[1])
                        : StateAdministration.GetSelectedCountryIdForUser(userId);
                    if (!countryId.HasValue)
                    {
                        await botClient.SendTextMessageAsync(message.Chat.Id, "Країну не знайдено.", cancellationToken: cancellationToken);
                        return;
                    }
                    string text = currencyService.BuildBlackMarketRateView(countryId.Value);
                    await botClient.SendTextMessageAsync(message.Chat.Id, text, cancellationToken: cancellationToken);
                    return;
                }
            }

             
            if (msgText == "Відновити ману за хід" || msgText == "Відновити ману після бою")
            {
                 
                return;
            }

             
            if (!string.IsNullOrWhiteSpace(msgText))
            {
                 
                if (msgText.StartsWith("Відновити ману", StringComparison.OrdinalIgnoreCase))
                {
                    var m = Regex.Match(msgText, @"(-?\d+)");
                    if (!m.Success)
                    {
                         
                        await botClient.SendTextMessageAsync(userId, "Вкажіть кількість. Формат: 'Відновити ману 10'", cancellationToken: cancellationToken);
                    }
                    else
                    {
                        if (!int.TryParse(m.Value, out int amount) || amount <= 0)
                        {
                            await botClient.SendTextMessageAsync(userId, "Невірна кількість. Вкажіть додатнє число.", cancellationToken: cancellationToken);
                        }
                        else
                        {
                             
                            int charId = UserData.GetActiveCharacterId(userId);
                            var stats = ManaData.GetMagicStats(userId, charId);
                            int newCur = Math.Min(stats.MaxMana, stats.CurrentMana + amount);
                            ManaData.UpdateMagicStat(userId, charId, "CurrentMana", newCur);
                            await botClient.SendTextMessageAsync(userId, $"Відновлено {amount} мани. Поточна мана: {newCur}/{stats.MaxMana}", cancellationToken: cancellationToken);
                        }
                    }
                }

                 
                else if (msgText.StartsWith("Витратити ману", StringComparison.OrdinalIgnoreCase))
                {
                    var m = Regex.Match(msgText, @"(-?\d+)");
                    if (!m.Success)
                    {
                        await botClient.SendTextMessageAsync(userId, "Вкажіть кількість. Формат: 'Витратити ману 10'", cancellationToken: cancellationToken);
                    }
                    else
                    {
                        if (!int.TryParse(m.Value, out int amount) || amount <= 0)
                        {
                            await botClient.SendTextMessageAsync(userId, "Невірна кількість. Вкажіть додатнє число.", cancellationToken: cancellationToken);
                        }
                        else
                        {
                            int charId = UserData.GetActiveCharacterId(userId);
                            var stats = ManaData.GetMagicStats(userId, charId);
                            int newCur = Math.Max(0, stats.CurrentMana - amount);
                            ManaData.UpdateMagicStat(userId, charId, "CurrentMana", newCur);
                            await botClient.SendTextMessageAsync(userId, $"Витрачено {amount} мани. Поточна мана: {newCur}/{stats.MaxMana}", cancellationToken: cancellationToken);
                        }
                    }
                }
            }

            if (!UserData.CheckifExist(message.From.Id))
                return;

            if (Strenght.Contains(msgText))
                await _subUserComandsRealisation.CharacterizationCheck(botClient, cancellationToken, message, Strenght, ReturnCharacterModificatorNameEnglishToUkrainian(Strenght[0]));

            if (Speed.Contains(msgText))
                await _subUserComandsRealisation.CharacterizationCheck(botClient, cancellationToken, message, Speed, ReturnCharacterModificatorNameEnglishToUkrainian(Speed[0]));

            if (Durablity.Contains(msgText))
                await _subUserComandsRealisation.CharacterizationCheck(botClient, cancellationToken, message, Durablity, ReturnCharacterModificatorNameEnglishToUkrainian(Durablity[0]));

            if (Intellect.Contains(msgText))
                await _subUserComandsRealisation.CharacterizationCheck(botClient, cancellationToken, message, Intellect, ReturnCharacterModificatorNameEnglishToUkrainian(Intellect[0]));

            if (Wisdom.Contains(msgText))
                await _subUserComandsRealisation.CharacterizationCheck(botClient, cancellationToken, message, Wisdom, ReturnCharacterModificatorNameEnglishToUkrainian(Wisdom[0]));

            if (Harism.Contains(msgText))
                await _subUserComandsRealisation.CharacterizationCheck(botClient, cancellationToken, message, Harism, ReturnCharacterModificatorNameEnglishToUkrainian(Harism[0]));


             
            async Task ApplyRegenAsync(bool isAfterFight)
            {
                 
                if (userId == masterID)
                {
                    var userIds = UserData.GetUsersFromDatabase()
                        .Select(u => u.Id)
                        .Distinct()
                        .ToList();
                    int appliedPlayers = 0;

                    foreach (var targetUserId in userIds)
                    {
                        try
                        {
                            var charIds = UserData.GetUserCharacterIds(targetUserId);
                            if (charIds == null || charIds.Count == 0)
                                continue;

                            var perPlayerLines = new List<string>();
                            bool anyUpdated = false;

                            foreach (var charId in charIds)
                            {
                                try
                                {
                                    var s = ManaData.GetMagicStats(targetUserId, charId);
                                    if (s == null) continue;

                                    int restore = isAfterFight
                                        ? (int)Math.Ceiling(s.MaxMana / 10.0 + s.RegenAfterFight)
                                        : (int)Math.Ceiling(s.MaxMana / 100.0 + s.RegenTurn);

                                    int newCur = Math.Min(s.MaxMana, s.CurrentMana + restore);
                                    ManaData.UpdateMagicStat(targetUserId, charId, "CurrentMana", newCur);

                                    perPlayerLines.Add($"Персонаж {charId}: +{restore} → {newCur}/{s.MaxMana}");
                                    anyUpdated = true;
                                }
                                catch
                                {
                                     
                                }
                            }

                            if (anyUpdated)
                            {
                                 
                                try
                                {
                                    var header = isAfterFight ? "Відновлення після бою" : "Відновлення за хід";
                                    await botClient.SendTextMessageAsync(targetUserId, $"Майстер застосував {header}:\n" + string.Join("\n", perPlayerLines), cancellationToken: cancellationToken);
                                }
                                catch
                                {
                                     
                                }

                                appliedPlayers++;
                            }
                        }
                        catch
                        {
                             
                        }
                    }

                    await botClient.SendTextMessageAsync(userId, $"Реген застосовано до {appliedPlayers} гравців.", cancellationToken: cancellationToken);
                }
                else
                {
                    int charId = UserData.GetActiveCharacterId(userId);
                    var s = ManaData.GetMagicStats(userId, charId);
                    if (s == null)
                    {
                        await botClient.SendTextMessageAsync(userId, "Не знайдено даних мани для вашого персонажа.", cancellationToken: cancellationToken);
                        return;
                    }

                    int restore = isAfterFight
                        ? (int)Math.Ceiling(s.MaxMana / 10.0 + s.RegenAfterFight)
                        : (int)Math.Ceiling(s.MaxMana / 100.0 + s.RegenTurn);

                    int newCur = Math.Min(s.MaxMana, s.CurrentMana + restore);
                    ManaData.UpdateMagicStat(userId, charId, "CurrentMana", newCur);

                    await botClient.SendTextMessageAsync(userId, $"Відновлено {(isAfterFight ? "після бою" : "за хід")}: +{restore} мани. Поточна: {newCur}/{s.MaxMana}", cancellationToken: cancellationToken);
                }
            }
        }

        private static bool TryParseAmount(string raw, out decimal value)
        {
            value = 0m;
            if (string.IsNullOrWhiteSpace(raw))
                return false;
            string normalized = raw.Replace(',', '.');
            if (decimal.TryParse(normalized, NumberStyles.Any, CultureInfo.InvariantCulture, out value))
                return true;
            return decimal.TryParse(raw, NumberStyles.Any, CultureInfo.CurrentCulture, out value);
        }

        public string BuildPlayerView(ManaDataClass s)
        {
             
            string baseManaSuffix = "";
            string maxManaSuffix = "";
            string currentManaSuffix = "";

            try
            {
                var mods = ResourceModifiers.Get(s.UserId, s.CharacterId);
                if (mods != null)
                {
                    if (mods.BaseManaPercentMod != 0) baseManaSuffix = $" ({(mods.BaseManaPercentMod > 0 ? "+" : "")}{mods.BaseManaPercentMod}%)";
                    if (mods.MaxManaPercentMod != 0) maxManaSuffix = $" ({(mods.MaxManaPercentMod > 0 ? "+" : "")}{mods.MaxManaPercentMod}%)";
                    if (mods.CurrentManaPercentMod != 0) currentManaSuffix = $" ({(mods.CurrentManaPercentMod > 0 ? "+" : "")}{mods.CurrentManaPercentMod}%)";
                }
            }
            catch
            {
                 
            }

            return
            $@"🔮 Магічні характеристики

            Магічний рівень: {s.MagicLevel}
            Магічний досвід: {s.MagicXP}/{LevelSystem.ManaConsumption[s.MagicLevel + 1]}

            Мана: {s.CurrentMana}{currentManaSuffix} / {s.MaxMana}{maxManaSuffix}
            Базова мана: {s.BaseMana}{baseManaSuffix}
            Множник: {Math.Round(Math.Pow(1.12, s.MagicLevel - 1),2)}

            Магічний обладунок: {s.MagicArmour}

            Стабільність: {s.Stability}%
            Виснаження: {s.Exhaustion}%

            Зона мани:
            • Радіус: {s.ManaZoneRadius} м
            • Контроль: {s.ManaZoneControl}%

            Регенерація:
            • За хід: +{Math.Round(s.MaxMana / 100.0 + s.RegenTurn, 2)}
            • Після бою: +{Math.Round(s.MaxMana / 10.0 + s.RegenAfterFight, 2)}";
        }



        public string BuildPlayerView(List<ManaDataClass> s)
        {
            if (s == null || s.Count == 0)
                return "Немає даних.";

             
            var filtered = s.Where(m => m.UserId != masterID).ToList();

             
            if (filtered.Count == 0)
                filtered = s.ToList();

             
            if (filtered.Count == 1)
            {
                var m = filtered[0];
                string name = "";
                try
                {
                    name = UserData.DataBaseValueReader<string>(m.UserId, "Name") ?? "";
                }
                catch
                {
                    name = "";
                }

                var header = !string.IsNullOrWhiteSpace(name) ? $"Гравець: {name} (C{m.CharacterId})\n" : $"Гравець: ID {m.UserId} (C{m.CharacterId})\n";
                return header + BuildPlayerView(m);
            }

             
            string text = "";
            foreach (var m in filtered)
            {
                string name = "";
                try
                {
                    name = UserData.DataBaseValueReader<string>(m.UserId, "Name") ?? "";
                }
                catch
                {
                    name = "";
                }

                var header = !string.IsNullOrWhiteSpace(name) ? $"Гравець: {name} (C{m.CharacterId})" : $"Гравець: ID {m.UserId} (C{m.CharacterId})";
                text += header + "\n";
                text += BuildPlayerView(m);
                text += "\n-------------------\n\n";
            }

            return text.TrimEnd();
        }


        private async Task SendMessageToAll(ITelegramBotClient botClient,Message message)
        {
            if (message?.Text != null && message.Text.StartsWith("Надіслати:"))
            {
                string broadcastMessage = message.Text.Substring(10);  
                await SendToAllUsers(botClient,broadcastMessage);
                _logger.LogInformation($"Розіслано повідомлення всім користувачам: {broadcastMessage}");
            }
        }


        private async Task SendToAllUsers(ITelegramBotClient botClient, string message)
        {
            var userIds = UserListDataBase.GetAllUsers();  

            foreach (var userId in userIds)
            {
                try
                {
                    await botClient.SendTextMessageAsync(userId, message);
                    _logger.LogInformation($"Повідомлення надіслано користувачу {userId}");
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Не вдалося надіслати повідомлення користувачу {userId}: {ex.Message}");
                }
            }
        }

        private async Task AddXpToAllPlayers(ITelegramBotClient botClient, long userId, CancellationToken cancellationToken, bool isMagicXp = false)
        {
            await botClient.SendTextMessageAsync(masterID, !isMagicXp ? $"Скільки досвіду ви хочете додати?" : $"Скільки магічнного досвіду ви хочете додати?", cancellationToken: cancellationToken);
            if (!isMagicXp)
            {
                if (!userStateXP.TryAdd(masterID, "start"))
                    userStateXP[masterID] = "start";

                userStateXP[masterID] = "start";
            }
            else
            {
                if (!userStateMagicXP.TryAdd(masterID, "start"))
                    userStateMagicXP[masterID] = "start";

                userStateMagicXP[masterID] = "start";
            }

                LevelSystem.AddToAllUsers = true;
        }

        private static bool IsPrivateOnlyCommand(string text)
        {
            return text == "Інформація" ||
                   text == "Гаманець" ||
                   text == "Транзакції" ||
                   text == "Змінити дані";
        }

        public async Task StartBotCommands(ITelegramBotClient botClient, Message message, ReplyKeyboardMarkup replyKeyboard, CancellationToken cancellationToken)
        {
            if (message?.From == null)
                return;

            long userId = message.From.Id;

             
            if (BotKeyboardHelper.UseInlineButtonsUnderMessages)
            {
                var inline = BotKeyboardHelper.ConvertReplyKeyboardToInline(replyKeyboard);
                await botClient.SendTextMessageAsync(message.Chat.Id, "*Вітаю вас серед магічних пригод!* \nСтворений за ідеєю @Raika9112, розроблений @loWreUns \n\nДля списку команд напишіть 'Допомога' або 'Меню'", replyMarkup: inline, parseMode: ParseMode.Markdown, cancellationToken: cancellationToken);            }
            else
            {
                await botClient.SendTextMessageAsync(message.Chat.Id, "*Вітаю вас серед магічних пригод!* \nСтворений за ідеєю @Raika9112, розроблений @loWreUns \n\nДля списку команд напишіть 'Допомога' або 'Меню'", parseMode: ParseMode.Markdown, cancellationToken: cancellationToken);
            }

            userState.Clear();

            if (!OldMessages.TryGetValue(userId, out List<int> value))
                OldMessages[userId] = [];
        }

        public async Task HandleCallbackQ(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken, Message message, CallbackQuery callbackQuery)
        {
            try
            {
                var data = callbackQuery.Data ?? "";
                long chatId = callbackQuery.From.Id;
                long userId = callbackQuery.From.Id;

                 
                if (data.StartsWith("kb_"))
                {
                     
                    string text = BotSystem.BotKeyboardHelper.UnescapeCallback(data);

                     
                    await botClient.AnswerCallbackQueryAsync(callbackQuery.Id, cancellationToken: cancellationToken);

                     
                    var syntheticMessage = new Message
                    {
                        MessageId = 0,
                        From = callbackQuery.From,
                        Chat = callbackQuery.Message?.Chat ?? new Chat { Id = chatId },
                        Text = text
                    };

                     
                    var replyKeyboard = ButtonsCreate(chatId, false);

                     
                    var botBodyRealisation = new BotBodyRealisation();
                    await _monsterRealisation.Monster(botClient, null, cancellationToken, syntheticMessage, replyKeyboard, botBodyRealisation);

                     
                    await Commands(botClient, null, cancellationToken, syntheticMessage, replyKeyboard);

                     
                     
                    await DataCollector.CollectData(botClient, null, cancellationToken, syntheticMessage, replyKeyboard);
                    return;
                }

                 
                if (data.StartsWith("monster_"))
                {
                    var replyKeyboard = ButtonsCreate(chatId, false);
                    var botBodyRealisation = new BotBodyRealisation();

                    await _monsterRealisation.HandleMonsterCallback(
                        botClient, 
                        callbackQuery,
                        cancellationToken,
                        replyKeyboard, 
                        botBodyRealisation);
                    return;
                }

                if (data.StartsWith("combat_"))
                {
                    await HandleCombatCallback(botClient, callbackQuery, cancellationToken);
                    return;
                }

                 
                var parts = data.Split('_');
                if (parts.Length < 2)
                    return;
                var infoButton = parts[1];

                if (data.StartsWith("trf_"))
                {
                    if(chatId != masterID)
                    if (!UserData.CheckifExist(chatId))
                    {
                        await botClient.SendTextMessageAsync(chatId, $"Немає даних.", cancellationToken: cancellationToken);
                        return;
                    }

                    await _callBackWalletCommands.WalletCallBackCommands(botClient,cancellationToken,chatId,infoButton);
                }

                if (data.StartsWith("table_"))
                {
                    await SubTableCallBackCommands.TableCallBackCommands(botClient, cancellationToken, chatId, infoButton);
                }
                if (data.StartsWith("ThDice_"))
                {
                    if (OldMessages.ContainsKey(chatId) && OldMessages.Count > 0)
                        DeleteAllOldMessages(botClient, cancellationToken, chatId);

                    if (int.TryParse(infoButton, out int diceType))
                        await botClient.SendTextMessageAsync(chatId, $"На кубіку з {diceType} гранями випало: {MathfDND.DiceReturn(diceType)} ", cancellationToken: cancellationToken);
                }
                if (data.StartsWith("xpAdd_"))
                {
                    if (!long.TryParse(infoButton, out long _choosenUser))
                        return;

                    int chosenCharacterId = 0;
                    if (parts.Length >= 3)
                        int.TryParse(parts[2], out chosenCharacterId);

                    _logger.Log(0, $"User:{chatId} trying to add XP to UserData to this user: {_choosenUser}");

                    await XpAddCallBack(botClient, message, chatId, _choosenUser, chosenCharacterId, cancellationToken);
                }
                if (data.StartsWith("magicXpAdd_"))
                {
                    if (!long.TryParse(infoButton, out long _choosenUser))
                        return;

                    int chosenCharacterId = 0;
                    if (parts.Length >= 3)
                        int.TryParse(parts[2], out chosenCharacterId);

                    _logger.Log(0, $"User:{chatId} trying to add MagicXP to this user: {_choosenUser}");

                    await MagicXpAddCallBack(botClient, message, chatId, _choosenUser, chosenCharacterId, cancellationToken);
                }



                await botClient.AnswerCallbackQueryAsync(callbackQuery.Id);
            }
            catch (ApiRequestException ex) when (ex.Message.Contains("query is too old"))
            {
                _logger.LogInformation("Exception `Ignore old query: Query is too old`");
            }
        }

        private async Task XpAddCallBack(ITelegramBotClient botClient, Message message, long chatId, long _choosenUser, int chosenCharacterId, CancellationToken cancellationToken)
        {
            int targetCharacterId = chosenCharacterId > 0
                ? chosenCharacterId
                : UserData.GetActiveCharacterId(_choosenUser);

            if (!UserData.CheckifExist(_choosenUser, targetCharacterId))
            {
                await botClient.SendTextMessageAsync(chatId, "Не вдалося знайти вказану анкету гравця.", cancellationToken: cancellationToken);
                return;
            }

            string characterName = UserData.GetCharacterName(_choosenUser, targetCharacterId);
            await botClient.SendTextMessageAsync(chatId, $"Скільки досвіду ви хочете додати персонажу: {characterName} (C{targetCharacterId})", cancellationToken: cancellationToken);

            if (!LevelSystem.choosenUser.ContainsKey(masterID))
                LevelSystem.choosenUser.TryAdd(masterID, _choosenUser);

            if (!LevelSystem.choosenCharacter.ContainsKey(masterID))
                LevelSystem.choosenCharacter.TryAdd(masterID, targetCharacterId);

            if (!userStateXP.ContainsKey(masterID))
                userStateXP.TryAdd(masterID, "paste");

            LevelSystem.choosenUser[masterID] = _choosenUser;
            LevelSystem.choosenCharacter[masterID] = targetCharacterId;
            userStateXP[masterID] = "paste";
            userStateMagicXP[masterID] = "";

            _logger.LogInformation("Master start method to add expirience to user");
        }

        private async Task MagicXpAddCallBack(ITelegramBotClient botClient, Message message, long chatId, long _choosenUser, int chosenCharacterId, CancellationToken cancellationToken)
        {
            int targetCharacterId = chosenCharacterId > 0
                ? chosenCharacterId
                : UserData.GetActiveCharacterId(_choosenUser);

            if (!UserData.CheckifExist(_choosenUser, targetCharacterId))
            {
                await botClient.SendTextMessageAsync(chatId, "Не вдалося знайти вказану анкету гравця.", cancellationToken: cancellationToken);
                return;
            }

            string characterName = UserData.GetCharacterName(_choosenUser, targetCharacterId);
            await botClient.SendTextMessageAsync(chatId, $"Скільки магічного досвіду ви хочете додати персонажу: {characterName} (C{targetCharacterId})", cancellationToken: cancellationToken);

            if (!LevelSystem.choosenUser.ContainsKey(masterID))
                LevelSystem.choosenUser.TryAdd(masterID, _choosenUser);

            if (!LevelSystem.choosenCharacter.ContainsKey(masterID))
                LevelSystem.choosenCharacter.TryAdd(masterID, targetCharacterId);

            if (!userStateMagicXP.ContainsKey(masterID))
                userStateMagicXP.TryAdd(masterID, "paste");

            LevelSystem.choosenUser[masterID] = _choosenUser;
            LevelSystem.choosenCharacter[masterID] = targetCharacterId;
            userStateMagicXP[masterID] = "paste";
            userStateXP[masterID] = "";

            _logger.LogInformation("Master start method to add magic expirience to user");
        }

        private async Task SendCombatPanel(ITelegramBotClient botClient, long chatId, CancellationToken cancellationToken)
        {
            var session = GetOrCreateCombatSession(chatId);
            await botClient.SendTextMessageAsync(
                chatId,
                BuildCombatStatusText(session),
                replyMarkup: InlineKeyboardsClass.CombatControlKeyboard(session.IsActive),
                cancellationToken: cancellationToken);
        }

        private async Task HandleCombatCallback(ITelegramBotClient botClient, CallbackQuery callbackQuery, CancellationToken cancellationToken)
        {
            string data = callbackQuery.Data ?? string.Empty;
            long chatId = callbackQuery.Message?.Chat?.Id ?? callbackQuery.From.Id;
            var session = GetOrCreateCombatSession(chatId);

            if (data.StartsWith("combat_monster_auto_target_", StringComparison.Ordinal))
            {
                await HandleMonsterAutoAttackTargetSelection(botClient, chatId, callbackQuery, session, data, cancellationToken);
                return;
            }

            if (data.StartsWith("combat_monster_auto_", StringComparison.Ordinal))
            {
                await HandleMonsterAutoAttackSelection(botClient, chatId, callbackQuery, session, data, cancellationToken);
                return;
            }

            if (data.StartsWith("combat_monster_manual_", StringComparison.Ordinal))
            {
                await HandleMonsterManualAttackSelection(botClient, chatId, callbackQuery, session, data, cancellationToken);
                return;
            }

            switch (data)
            {
                case "combat_start":
                    if (!await EnsureCombatMasterAccess(botClient, callbackQuery, cancellationToken))
                        return;

                    if (callbackQuery.Message == null)
                    {
                        await botClient.SendTextMessageAsync(chatId, "Не можу запустити бій без контексту чату.", cancellationToken: cancellationToken);
                        break;
                    }

                    await _fightTask.Fight(botClient, cancellationToken, callbackQuery.Message, "Бій");
                    InitializeCombatSessionFromSnapshot(session);

                    if (!session.IsActive)
                    {
                        await botClient.SendTextMessageAsync(
                            chatId,
                            "Не вдалося розпочати бій: немає учасників (попроси гравців кинути ініціативу).",
                            replyMarkup: InlineKeyboardsClass.CombatControlKeyboard(false),
                            cancellationToken: cancellationToken);
                        break;
                    }

                    await botClient.SendTextMessageAsync(
                        chatId,
                        "⚔ Бій розпочато!\n" + BuildCurrentTurnText(session),
                        replyMarkup: InlineKeyboardsClass.CombatControlKeyboard(true),
                        cancellationToken: cancellationToken);
                    break;

                case "combat_next":
                    if (!await EnsureCombatMasterAccess(botClient, callbackQuery, cancellationToken))
                        return;

                    if (!session.IsActive)
                    {
                        await botClient.SendTextMessageAsync(chatId, "Бій ще не розпочато. Натисни 'Розпочати бій'.", cancellationToken: cancellationToken);
                        break;
                    }

                    SyncCombatSessionWithSnapshot(session);
                    if (!session.IsActive)
                    {
                        await botClient.SendTextMessageAsync(chatId, "Бій завершено автоматично: учасників більше немає.", replyMarkup: InlineKeyboardsClass.CombatControlKeyboard(false), cancellationToken: cancellationToken);
                        break;
                    }

                    AdvanceTurn(session);
                    await botClient.SendTextMessageAsync(
                        chatId,
                        "➡ Наступний хід\n" + BuildCurrentTurnText(session),
                        replyMarkup: InlineKeyboardsClass.CombatControlKeyboard(true),
                        cancellationToken: cancellationToken);
                    break;

                case "combat_status":
                    SyncCombatSessionWithSnapshot(session);
                    await botClient.SendTextMessageAsync(
                        chatId,
                        BuildCombatStatusText(session),
                        replyMarkup: InlineKeyboardsClass.CombatControlKeyboard(session.IsActive),
                        cancellationToken: cancellationToken);
                    break;

                case "combat_add_participants":
                    if (!await EnsureCombatMasterAccess(botClient, callbackQuery, cancellationToken))
                        return;

                    if (callbackQuery.Message == null)
                    {
                        await botClient.SendTextMessageAsync(chatId, "Не можу оновити список учасників без контексту чату.", cancellationToken: cancellationToken);
                        break;
                    }

                    int beforeCount = Initiative.Count;
                    await _fightTask.Fight(botClient, cancellationToken, callbackQuery.Message, "Бій");
                    int addedCount = Math.Max(0, Initiative.Count - beforeCount);

                    SyncCombatSessionWithSnapshot(session);

                    await botClient.SendTextMessageAsync(
                        chatId,
                        addedCount > 0
                            ? $"➕ Додано нових учасників: {addedCount}.\n\n{BuildCombatStatusText(session)}"
                            : "Нових учасників для додавання не знайдено.\n" +
                              "Щоб додати гравця, він має кинути ініціативу.",
                        replyMarkup: InlineKeyboardsClass.CombatControlKeyboard(session.IsActive),
                        cancellationToken: cancellationToken);
                    break;

                case "combat_end":
                    if (!await EnsureCombatMasterAccess(botClient, callbackQuery, cancellationToken))
                        return;

                    EndCombatSession(chatId, clearFightCollections: true);
                    await botClient.SendTextMessageAsync(
                        chatId,
                        "🛑 Бій завершено. Черга ходів очищена.",
                        replyMarkup: InlineKeyboardsClass.CombatControlKeyboard(false),
                        cancellationToken: cancellationToken);
                    break;

                case "combat_attack":
                case "combat_heal":
                    bool isAttack = data == "combat_attack";

                    if (isAttack &&
                        session.IsActive &&
                        TryGetCurrentTurnEntry(session, out var currentTurnEntry) &&
                        currentTurnEntry.CharacterId == 0)
                    {
                        var currentMonster = Monsters.FirstOrDefault(m => m != null && m.ID == currentTurnEntry.Id);
                        bool hasAutoAttack = currentMonster?.HasAttack() == true;

                        await botClient.SendTextMessageAsync(
                            chatId,
                            $"Хід монстра: {currentTurnEntry.Name}\nОберіть режим атаки:",
                            replyMarkup: InlineKeyboardsClass.CombatMonsterAttackModeKeyboard(currentTurnEntry.Id, hasAutoAttack),
                            cancellationToken: cancellationToken);
                        break;
                    }

                    string actionText = isAttack ? "атакувати" : "вилікувати";
                    var keyboard = InlineKeyboardsClass.InlineAttackButtons(isAttack, out int targetsCount);

                    if (targetsCount == 0)
                    {
                        await botClient.SendTextMessageAsync(chatId, "Немає доступних цілей для цієї дії.", cancellationToken: cancellationToken);
                        break;
                    }

                    await botClient.SendTextMessageAsync(
                        chatId,
                        $"Кого ви хочете {actionText}?",
                        replyMarkup: keyboard,
                        cancellationToken: cancellationToken);
                    break;
            }

            await SafeAnswerCallback(
                botClient,
                callbackQuery.Id,
                cancellationToken: cancellationToken,
                callbackData: callbackQuery.Data,
                userId: callbackQuery.From?.Id);
        }

        private async Task HandleMonsterManualAttackSelection(
            ITelegramBotClient botClient,
            long chatId,
            CallbackQuery callbackQuery,
            CombatSessionState session,
            string data,
            CancellationToken cancellationToken)
        {
            if (!await EnsureCombatMasterAccess(botClient, callbackQuery, cancellationToken))
                return;

            if (!TryParseLongSuffix(data, "combat_monster_manual_", out long monsterId))
            {
                await SafeAnswerCallback(botClient, callbackQuery.Id, "Невірний монстр.", cancellationToken);
                return;
            }

            if (!session.IsActive || !TryGetCurrentTurnEntry(session, out var current) || current.CharacterId != 0 || current.Id != monsterId)
            {
                await SafeAnswerCallback(botClient, callbackQuery.Id, "Зараз не хід цього монстра.", cancellationToken);
                return;
            }

            var keyboard = InlineKeyboardsClass.InlineAttackButtons(true, out int targetsCount);
            if (targetsCount == 0)
            {
                await botClient.SendTextMessageAsync(chatId, "Немає доступних цілей.", cancellationToken: cancellationToken);
                await SafeAnswerCallback(botClient, callbackQuery.Id, cancellationToken: cancellationToken);
                return;
            }

            await botClient.SendTextMessageAsync(
                chatId,
                "Ручний розрахунок: оберіть ціль, потім введіть урон.",
                replyMarkup: keyboard,
                cancellationToken: cancellationToken);

            await SafeAnswerCallback(botClient, callbackQuery.Id, cancellationToken: cancellationToken);
        }

        private async Task HandleMonsterAutoAttackSelection(
            ITelegramBotClient botClient,
            long chatId,
            CallbackQuery callbackQuery,
            CombatSessionState session,
            string data,
            CancellationToken cancellationToken)
        {
            if (!await EnsureCombatMasterAccess(botClient, callbackQuery, cancellationToken))
                return;

            if (!TryParseLongSuffix(data, "combat_monster_auto_", out long monsterId))
            {
                await SafeAnswerCallback(botClient, callbackQuery.Id, "Невірний монстр.", cancellationToken);
                return;
            }

            if (!session.IsActive || !TryGetCurrentTurnEntry(session, out var current) || current.CharacterId != 0 || current.Id != monsterId)
            {
                await SafeAnswerCallback(botClient, callbackQuery.Id, "Зараз не хід цього монстра.", cancellationToken);
                return;
            }

            var monster = Monsters.FirstOrDefault(m => m != null && m.ID == monsterId);
            if (monster == null)
            {
                await SafeAnswerCallback(botClient, callbackQuery.Id, "Монстра не знайдено.", cancellationToken);
                return;
            }

            if (!monster.HasAttack())
            {
                await SafeAnswerCallback(botClient, callbackQuery.Id, "У монстра не налаштована автоатака.", cancellationToken);
                return;
            }

            var keyboard = InlineKeyboardsClass.InlineCombatPlayerTargetButtons($"combat_monster_auto_target_{monsterId}", out int targetsCount);
            if (targetsCount == 0)
            {
                await botClient.SendTextMessageAsync(chatId, "Немає доступних гравців для автоатаки.", cancellationToken: cancellationToken);
                await SafeAnswerCallback(botClient, callbackQuery.Id, cancellationToken: cancellationToken);
                return;
            }

            await botClient.SendTextMessageAsync(
                chatId,
                $"Автоатака '{monster.Name}'. Оберіть ціль:",
                replyMarkup: keyboard,
                cancellationToken: cancellationToken);

            await SafeAnswerCallback(botClient, callbackQuery.Id, cancellationToken: cancellationToken);
        }

        private async Task HandleMonsterAutoAttackTargetSelection(
            ITelegramBotClient botClient,
            long chatId,
            CallbackQuery callbackQuery,
            CombatSessionState session,
            string data,
            CancellationToken cancellationToken)
        {
            if (!await EnsureCombatMasterAccess(botClient, callbackQuery, cancellationToken))
                return;

            if (!TryParseAutoTargetCallback(data, out long monsterId, out long playerId, out int playerCharacterId))
            {
                await SafeAnswerCallback(botClient, callbackQuery.Id, "Невірна ціль.", cancellationToken);
                return;
            }

            if (!session.IsActive || !TryGetCurrentTurnEntry(session, out var current) || current.CharacterId != 0 || current.Id != monsterId)
            {
                await SafeAnswerCallback(botClient, callbackQuery.Id, "Зараз не хід цього монстра.", cancellationToken);
                return;
            }

            if (!InlineKeyboardsClass.IsAllowedCombatTarget(playerId, playerCharacterId))
            {
                await SafeAnswerCallback(botClient, callbackQuery.Id, "Ціль не є учасником бою.", cancellationToken);
                return;
            }

            if (!UserData.CheckifExist(playerId, playerCharacterId))
            {
                await SafeAnswerCallback(botClient, callbackQuery.Id, "Гравця не знайдено.", cancellationToken);
                return;
            }

            var monster = Monsters.FirstOrDefault(m => m != null && m.ID == monsterId);
            if (monster == null || !monster.HasAttack())
            {
                await SafeAnswerCallback(botClient, callbackQuery.Id, "Автоатака для монстра недоступна.", cancellationToken);
                return;
            }

            if (!MonsterAttackRoller.TryRollDamage(monster.AttackDamageDice, out int damage, out string breakdown, out string rollError))
            {
                await botClient.SendTextMessageAsync(
                    chatId,
                    $"Не вдалося порахувати автоатаку монстра: {rollError}",
                    cancellationToken: cancellationToken);
                await SafeAnswerCallback(botClient, callbackQuery.Id, cancellationToken: cancellationToken);
                return;
            }

            string playerName = UserData.DataBaseValueReader<string>(playerId, playerCharacterId, "Name");
            string attackName = string.IsNullOrWhiteSpace(monster.AttackName) ? "Атака" : monster.AttackName;

            var damageResult = UserData.ApplyDamageWithTemporaryHp(playerId, playerCharacterId, damage);
            string healthDisplay = UserData.BuildHealthDisplay(
                damageResult.CurrentHp,
                damageResult.MaxHp,
                damageResult.TempHp);
            string absorbedText = damageResult.DamageToTempHp > 0
                ? $" ({damageResult.DamageToTempHp} поглинуто тимчасовими хітами)"
                : string.Empty;

            await botClient.SendTextMessageAsync(
                chatId,
                $"🤖 {monster.Name} використовує '{attackName}' ({monster.AttackDamageDice})\n" +
                $"Кидок: {breakdown}\n" +
                $"Урон: {damage}",
                cancellationToken: cancellationToken);

            await botClient.SendTextMessageAsync(
                chatId,
                $"Було нанесено гравцю {playerName} на {damage}{absorbedText}, у гравця зараз {healthDisplay} здоров'я",
                cancellationToken: cancellationToken);

            await botClient.SendTextMessageAsync(
                chatId,
                "Повернення до бойової панелі:",
                replyMarkup: GetCombatPanelMarkup(chatId),
                cancellationToken: cancellationToken);

            await SafeAnswerCallback(botClient, callbackQuery.Id, cancellationToken: cancellationToken);
        }

        private static bool TryGetCurrentTurnEntry(CombatSessionState session, out CombatTurnEntry current)
        {
            current = default;
            if (session == null || !session.IsActive || session.Order.Count == 0)
                return false;

            if (session.TurnIndex < 0 || session.TurnIndex >= session.Order.Count)
                return false;

            current = session.Order[session.TurnIndex];
            return current != null;
        }

        private static bool TryParseLongSuffix(string data, string prefix, out long value)
        {
            value = 0;
            if (!data.StartsWith(prefix, StringComparison.Ordinal))
                return false;

            return long.TryParse(data[prefix.Length..], out value);
        }

        private static bool TryParseAutoTargetCallback(string data, out long monsterId, out long playerId, out int characterId)
        {
            monsterId = 0;
            playerId = 0;
            characterId = 0;

            const string prefix = "combat_monster_auto_target_";
            if (!data.StartsWith(prefix, StringComparison.Ordinal))
                return false;

            string tail = data[prefix.Length..];
            string[] parts = tail.Split('_');
            if (parts.Length != 3)
                return false;

            return long.TryParse(parts[0], out monsterId) &&
                   long.TryParse(parts[1], out playerId) &&
                   int.TryParse(parts[2], out characterId);
        }

        private async Task SafeAnswerCallback(
            ITelegramBotClient botClient,
            string callbackId,
            string text = null,
            CancellationToken cancellationToken = default,
            string callbackData = null,
            long? userId = null)
        {
            try
            {
                await botClient.AnswerCallbackQueryAsync(
                    callbackId,
                    string.IsNullOrWhiteSpace(text) ? null : text,
                    cancellationToken: cancellationToken);
            }
            catch (ApiRequestException ex) when ((ex.Message ?? string.Empty).Contains("query is too old", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogDebug(
                    "Skipping old callback answer. CallbackId={CallbackId}, UserId={UserId}, Data={CallbackData}",
                    callbackId,
                    userId,
                    callbackData);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Failed to answer callback query. CallbackId={CallbackId}, UserId={UserId}, Data={CallbackData}, Text={CallbackText}",
                    callbackId,
                    userId,
                    callbackData,
                    text);
            }
        }

        private async Task<bool> EnsureCombatMasterAccess(ITelegramBotClient botClient, CallbackQuery callbackQuery, CancellationToken cancellationToken)
        {
            long userId = callbackQuery.From.Id;
            if (userId == masterID || userId == ownerID)
                return true;

            await SafeAnswerCallback(
                botClient,
                callbackQuery.Id,
                "Лише майстер керує ходами бою.",
                cancellationToken,
                callbackQuery.Data,
                callbackQuery.From?.Id);

            return false;
        }

        private CombatSessionState GetOrCreateCombatSession(long chatId)
        {
            if (!CombatSessions.TryGetValue(chatId, out var session))
            {
                session = new CombatSessionState();
                CombatSessions[chatId] = session;
            }

            return session;
        }

        private void InitializeCombatSessionFromSnapshot(CombatSessionState session)
        {
            var order = _fightTask.GetOrderedParticipants(true)
                .Select(p => new CombatTurnEntry
                {
                    Id = p.Id,
                    CharacterId = p.CharacterId,
                    Name = p.Name,
                    Initiative = p.Initiative
                })
                .ToList();

            session.Order = order;
            session.Round = 1;
            session.TurnIndex = 0;
            session.IsActive = order.Count > 0;
        }

        private void SyncCombatSessionWithSnapshot(CombatSessionState session)
        {
            var snapshot = _fightTask.GetOrderedParticipants(true)
                .ToDictionary(p => (p.Id, p.CharacterId), p => p);

            if (snapshot.Count == 0)
            {
                session.IsActive = false;
                session.Order.Clear();
                session.Round = 1;
                session.TurnIndex = 0;
                return;
            }

            var syncedOrder = new List<CombatTurnEntry>();
            foreach (var entry in session.Order)
            {
                if (!snapshot.TryGetValue((entry.Id, entry.CharacterId), out var live))
                    continue;

                syncedOrder.Add(new CombatTurnEntry
                {
                    Id = live.Id,
                    CharacterId = live.CharacterId,
                    Name = live.Name,
                    Initiative = live.Initiative
                });
            }

             
            var existingKeys = syncedOrder
                .Select(e => (e.Id, e.CharacterId))
                .ToHashSet();

            foreach (var live in snapshot.Values.OrderByDescending(v => v.Initiative))
            {
                if (existingKeys.Contains((live.Id, live.CharacterId)))
                    continue;

                syncedOrder.Add(new CombatTurnEntry
                {
                    Id = live.Id,
                    CharacterId = live.CharacterId,
                    Name = live.Name,
                    Initiative = live.Initiative
                });
            }

            session.Order = syncedOrder;
            if (session.Order.Count == 0)
            {
                session.IsActive = false;
                session.Round = 1;
                session.TurnIndex = 0;
                return;
            }

            if (session.TurnIndex >= session.Order.Count)
                session.TurnIndex = 0;
        }

        private static void AdvanceTurn(CombatSessionState session)
        {
            if (session.Order.Count == 0)
                return;

            session.TurnIndex++;
            if (session.TurnIndex >= session.Order.Count)
            {
                session.TurnIndex = 0;
                session.Round++;
            }
        }

        private void EndCombatSession(long chatId, bool clearFightCollections)
        {
            var session = GetOrCreateCombatSession(chatId);

            session.IsActive = false;
            session.Round = 1;
            session.TurnIndex = 0;
            session.Order.Clear();

            if (!clearFightCollections)
                return;

            InitiativeUsers.Clear();
            Initiative.Clear();
            FightCharacter.Clear();
        }

        private static string BuildCurrentTurnText(CombatSessionState session)
        {
            if (session.Order.Count == 0)
                return "Немає учасників у черзі ходів.";

            if (session.TurnIndex < 0 || session.TurnIndex >= session.Order.Count)
                session.TurnIndex = 0;

            var current = session.Order[session.TurnIndex];
            return $"Раунд: {session.Round}\n" +
                   $"Хід: {session.TurnIndex + 1}/{session.Order.Count}\n" +
                   $"Зараз ходить: {FormatCombatParticipant(current)} (ініціатива {current.Initiative})";
        }

        private static string BuildCombatStatusText(CombatSessionState session)
        {
            if (session.Order.Count == 0)
                return "Бій не активний. Натисни 'Розпочати бій', коли всі готові.";

            var lines = session.Order
                .Select((entry, index) =>
                {
                    string marker = session.IsActive && index == session.TurnIndex ? "➡" : "•";
                    return $"{marker} {index + 1}. {FormatCombatParticipant(entry)} [{entry.Initiative}]";
                });

            string state = session.IsActive ? "активний" : "неактивний";
            string currentTurn = session.IsActive ? "\n\n" + BuildCurrentTurnText(session) : "";

            return $"Стан бою: {state}\n" +
                   $"Раунд: {session.Round}\n" +
                   $"Учасників: {session.Order.Count}\n\n" +
                   $"Черга ходів:\n{string.Join("\n", lines)}" +
                   currentTurn;
        }

        private static string FormatCombatParticipant(CombatTurnEntry entry)
        {
            string suffix = entry.CharacterId > 0 ? $" (C{entry.CharacterId})" : " (монстр)";
            return $"{entry.Name}{suffix}";
        }


         
        private string BuildMenuText(long userId)
        {
             
            var lines = new List<string>();

             
            lines.Add("Меню — повний перелік доступних дій:");
            lines.Add("");
            lines.Add("Основні:");
            lines.Add("- Інформація");
            lines.Add("- Допомога");
            lines.Add("- Транзакції");
            lines.Add("");
            lines.Add("Бойові:");
            lines.Add("- Перевірки");
            lines.Add("- Попадання");
            lines.Add("- Ініціатива");
            lines.Add("- Атака");
            lines.Add("- Вилікувати");
            lines.Add("- Бойова панель");
            lines.Add("- Тимчасові хіти");
            lines.Add("- Тимчасове зниження ХП");
            lines.Add("- Бій (мастер)");
            lines.Add("- Заскочені гравці / Заскочені монстри (мастер)");
            lines.Add("");
            lines.Add("Кубіки / Інвентар / Сутності:");
            lines.Add("- Кубіки");
            lines.Add("- Зброя");
            lines.Add("- Сутності");
            lines.Add("- Шаблони монстрів (мастер)");
            lines.Add("- Інвентар");
            lines.Add("");
            lines.Add("Магія:");
            lines.Add("- Ефекти");
            lines.Add("- Мана");
            lines.Add("- Відновити ману <кількість>");
            lines.Add("- Витратити ману <кількість>");
            lines.Add("- Відновити ману за хід");
            lines.Add("- Відновити ману після бою");
            lines.Add("- Зілля мани");
            lines.Add("- Зілля зцілення");
            lines.Add("");
            lines.Add("Гроші:");
            lines.Add("- Гаманець");
            lines.Add("- Транзакції (обмін/переслати/додати/відняти)");
            lines.Add("");
            lines.Add("Дані / Прокачка:");
            lines.Add("- Змінити дані");
            lines.Add("- Змінити процентні значення");
            lines.Add("- Додати досвід");
            lines.Add("- Додати досвід до всіх (мастер)");
            lines.Add("- Додати магічний досвід");
            lines.Add("- Додати магічний досвід до всіх (мастер)");
            lines.Add("- Додати інформацію до бота");
            lines.Add("- Очистити свої дані");
            lines.Add("");
            lines.Add("Інше:");
            lines.Add("- Державне управління");
            lines.Add("- Створити покірних і лояльних депутатів <кількість>");
            lines.Add("- Керувати інформацією бота");

            return string.Join("\n", lines);
        }

        private InlineKeyboardMarkup MergeInlineKeyboards(InlineKeyboardMarkup first, InlineKeyboardMarkup second)
{
    if (first == null && second == null) return null;
    if (first == null) return second;
    if (second == null) return first;

    var firstRows = first.InlineKeyboard ?? Array.Empty<IEnumerable<InlineKeyboardButton>>();
    var secondRows = second.InlineKeyboard ?? Array.Empty<IEnumerable<InlineKeyboardButton>>();

    var mergedRows = firstRows.Concat(secondRows).ToArray();
    return new InlineKeyboardMarkup(mergedRows);
}
    }
}
