
using DNDBot.AttackSystem;
using DNDBot.BotSystem;
using DNDBot.CommandSystem;
using DNDBot.DataBase;
using DNDBot.Goverment;
using DNDBot.Inventory;
using DNDBot.MoneySystem;
using DNDBot.Spells;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace DNDBot.BotSystem
{
    class BotStart : BotBodyRealisation
    {
        private readonly ITelegramBotClient _botClient;
        private readonly ILogger<BotStart> _logger;

        private readonly BotBodyRealisation _botBodyMethods;

        public readonly MonsterRealisation _monsterRealisation;

        public readonly ComandsRealisation _comandsRealisation; 
        public readonly SubUserComandsRealisation _subUserComandsRealisation; 
        public readonly SubCommandsPlayerBattle _subCommandsPlayerBattle; 
        public readonly CallBackWalletCommands _callBackWalletCommands;

        private readonly AttackRealisation _attackRealisation;
        private readonly HealCommands _healCommands;
        private readonly AttackTask _attackTask;
        private readonly FightTask _fightTask;
        private readonly DamageTask _damageTask;

        private readonly LevelSystem _levelSystem;

        private readonly InventoryCollector _inventoryCollector;
        private readonly InventoryMasterCommand _inventoryMasterCommand;
        private readonly InventoryRealisation _inventoryRealisation;
        private readonly InventorySetMasterValue _inventorySetMasterValue;
        private readonly InventorySetValue _inventorySetValue;
        private readonly PercentModifierHandler _percentModifierHandler;

        private readonly WalletRealisation _walletRealisation;
        private readonly StateAdministration _stateAdministration;
        private readonly StateCreateOrHandleCountryCreation _stateCreateOrHandleCountryCreation;
        private readonly StateParliamentHandle _stateParliamentHandle;


        public BotStart(ITelegramBotClient botClient, ILogger<BotStart> logger, ILogger<DataCollector> dataCollectorLogger, BotBodyRealisation botBodyMethods, MonsterRealisation monsterRealisation, ComandsRealisation comandsRealisation,
            SubUserComandsRealisation subUserComandsRealisation, SubCommandsPlayerBattle subCommandsPlayerBattle, CallBackWalletCommands callBackWalletCommands, AttackRealisation attackRealisation,
            HealCommands healCommands, AttackTask attackTask, FightTask fightTask, DamageTask damageTask, LevelSystem levelSystem, InventoryCollector inventoryCollector, InventoryMasterCommand inventoryMasterCommand,
            InventoryRealisation inventoryRealisation, InventorySetMasterValue inventorySetMasterValue, InventorySetValue inventorySetValue, MoneyTransfer moneyTransfer, WalletRealisation walletRealisation, PercentModifierHandler percentModifierHandler, StateAdministration stateAdministration, StateCreateOrHandleCountryCreation stateCreateOrHandleCountryCreation, StateParliamentHandle stateParliamentHandle)
        {
            _botClient = botClient;
            _logger = logger;
            DataCollector.ConfigureLogger(dataCollectorLogger);

            _botBodyMethods = botBodyMethods;

            _monsterRealisation = monsterRealisation;

            _comandsRealisation = comandsRealisation;
            _subUserComandsRealisation = subUserComandsRealisation;
            _subCommandsPlayerBattle = subCommandsPlayerBattle;
            _callBackWalletCommands = callBackWalletCommands;

            _attackRealisation = attackRealisation;
            _healCommands = healCommands;
            _attackTask = attackTask;
            _fightTask = fightTask;
            _damageTask = damageTask;
            _levelSystem = levelSystem;

            _inventoryCollector = inventoryCollector;
            _inventoryMasterCommand = inventoryMasterCommand;
            _inventoryRealisation = inventoryRealisation;
            _inventorySetMasterValue = inventorySetMasterValue;
            _inventorySetValue = inventorySetValue;

            _percentModifierHandler = percentModifierHandler;

            _walletRealisation = walletRealisation;
            _stateAdministration = stateAdministration;
            _stateCreateOrHandleCountryCreation = stateCreateOrHandleCountryCreation;
            _stateParliamentHandle = stateParliamentHandle;
        }

        public static async Task Main(string[] args)
        {
            masterID = DndMaster;

            var serviceProvider = new ServiceCollection()
                .AddLogging(configure =>
                {
                    configure.ClearProviders();
                    configure.AddSimpleConsole(options =>
                    {
                        options.SingleLine = true;
                        options.TimestampFormat = "HH:mm:ss ";
                        options.IncludeScopes = false;
                    });
                })
                .AddSingleton<ITelegramBotClient>(new TelegramBotClient(ConfigurationManager.ConnectionStrings["BotKey"].ConnectionString))
                .AddSingleton<StateParliamentHandle>()
                .AddSingleton<StateAdministration>()
                .AddSingleton<StateCreateOrHandleCountryCreation>()
                .AddSingleton<PercentModifierHandler>()
                .AddSingleton<WalletRealisation>()
                .AddSingleton<WalletCallbackDataHandle>()
                .AddSingleton<WalletSubCommandsHandle>()
                .AddSingleton<WalletSwitchHandle>()
                .AddSingleton<MoneyTransfer>()

                .AddSingleton<InventoryCollector>() 
                .AddSingleton<InventoryMasterCommand>() 
                .AddSingleton<InventoryRealisation>() 
                .AddSingleton<InventorySetMasterValue>() 
                .AddSingleton<InventorySetValue>() 

                .AddSingleton<AttackRealisation>() 
                .AddSingleton<InventoryCollector>() 
                .AddSingleton<LevelSystem>() 
                .AddSingleton<HealCommands>() 
                .AddSingleton<AttackTask>() 
                .AddSingleton<FightTask>() 
                .AddSingleton<DamageTask>() 

                .AddSingleton<SubUserComandsRealisation>() 
                .AddSingleton<SubCommandsPlayerBattle>() 
                .AddSingleton<CallBackWalletCommands>() 
                .AddSingleton<ComandsRealisation>() 
                .AddSingleton<MonsterRealisation>() 
                .AddSingleton<BotBodyRealisation>()
                .AddSingleton<BotStart>()
                .BuildServiceProvider();

            var botStart = serviceProvider.GetRequiredService<BotStart>();
            await botStart.RunAsync();
        }

        public async Task RunAsync()
        {
            Console.OutputEncoding = Encoding.UTF8;
            Console.InputEncoding = Encoding.UTF8;

            _logger.LogInformation(LogTags.Tag("Bot is running...", LogTags.System));

            _botBodyMethods.DataInitialization();
            _botBodyMethods.ClassInitialization();

            var cancellationTokenSource = new CancellationTokenSource();
            var receiverOptions = new ReceiverOptions
            {
                AllowedUpdates = Array.Empty<UpdateType>()
            };

            _botClient.StartReceiving(
HandleUpdateAsync,
HandleErrorAsync,
receiverOptions,
cancellationTokenSource.Token);


        var me = await _botClient.GetMeAsync();
            _logger.LogInformation(LogTags.Tag($"Bot is working! {me.Username}, to see commands use /CMDHELP.", LogTags.System));

            await ReadConsoleCommands(cancellationTokenSource);
        }

        private async Task ReadConsoleCommands(CancellationTokenSource cts)
        {
            while (!cts.Token.IsCancellationRequested)
            {

                string messageText = Console.ReadLine();

                if (string.IsNullOrEmpty(messageText))
                    continue;

                var trimmedMessage = messageText.Trim();

                if (trimmedMessage.StartsWith("/sendToAll "))
                {
                    string broadcastMessage = trimmedMessage.Substring(11); 
                    await SendToAllUsers(broadcastMessage);
                    _logger.LogInformation(LogTags.Tag($"Message sent to all users. Text: {broadcastMessage}", LogTags.Cmd, LogTags.Broadcast));
                    continue;
                }
                else if(trimmedMessage.StartsWith("/stop"))
                {
                    _logger.LogWarning(LogTags.Tag("Bot stopping by console command...", LogTags.System, LogTags.Cmd));
                    await SendToAllUsers("Бот був вимкнений.");
                    cts.Cancel();
                    continue;
                }
                else if(trimmedMessage.StartsWith("CLEAR ALL DATA FROM UserInventory", StringComparison.OrdinalIgnoreCase))
                {
                    _botBodyMethods.ClearALlInventoryDataBase();
                    _logger.LogInformation(LogTags.Tag("All data was erased from UserInventory.", LogTags.Data, LogTags.DbClear));
                    continue;
                }                
                else if (trimmedMessage.StartsWith("CLEAR ALL DATA FROM SpellBook", StringComparison.OrdinalIgnoreCase))
                {
                    _botBodyMethods.ClearAllSpellDataBase();
                    _logger.LogInformation(LogTags.Tag("All data was erased from SpellBook.", LogTags.Data, LogTags.DbClear));
                    continue;
                }
                else if (trimmedMessage.StartsWith("CLEAR ALL DATA FROM MagicStats", StringComparison.OrdinalIgnoreCase))
                {
                    _botBodyMethods.ClearAllMagicStatsDataBase();
                    _logger.LogInformation(LogTags.Tag("All data was erased from MagicStats.", LogTags.Data, LogTags.DbClear));
                    continue;
                }
                else if (trimmedMessage.StartsWith("CLEAR ALL DATA FROM Research", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        var result = ResearchAdminData.ClearAllResearchData();
                        _logger.LogInformation(LogTags.Tag(result, LogTags.Data, LogTags.DbClear));
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, LogTags.Tag("Failed to clear research data.", LogTags.Data, LogTags.DbClear, LogTags.Fail));
                    }
                    continue;
                }
                else if (trimmedMessage.StartsWith("CLEAR ALL DATA FROM UserData COUNTRY", StringComparison.OrdinalIgnoreCase))
                {
                    var parts = trimmedMessage.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length < 7 || !int.TryParse(parts[^1], out int countryId))
                    {
                        _logger.LogWarning(LogTags.Tag("Usage: CLEAR ALL DATA FROM UserData COUNTRY <CountryId>", LogTags.Cmd, LogTags.Warn));
                        continue;
                    }

                    var result = UserData.ClearAllDataForCountry(countryId);
                    _logger.LogInformation(LogTags.Tag(result, LogTags.Data, LogTags.DbClear));
                    continue;
                }
                else if (trimmedMessage.StartsWith("CLEAR ALL DATA FROM Countries", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        var result = CountriesData.DeleteAllCountriesAndData();
                        _logger.LogInformation(LogTags.Tag(result, LogTags.Data, LogTags.DbClear));
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, LogTags.Tag("Failed to delete all countries and related data.", LogTags.Data, LogTags.DbClear, LogTags.Fail));
                    }
                    continue;
                }
                else if (trimmedMessage.StartsWith("SET MASTER OWNER"))
                {
                    masterID = ownerID;
                    _logger.LogInformation(LogTags.Tag("Owner now is a master.", LogTags.Data, LogTags.Cmd));
                    continue;
                }
                else if (trimmedMessage.StartsWith("SET MASTER MASTER"))
                {
                    masterID = DndMaster;
                    _logger.LogInformation(LogTags.Tag("DND master now is a master.", LogTags.Data, LogTags.Cmd));
                    continue;
                }
                else if (trimmedMessage.StartsWith("/CMDHELP"))
                {
                    _logger.LogInformation(LogTags.Tag("YOU CAN USE FOLLOW COMMANDS:\n" +
                                     "SET MASTER MASTER or SET MASTER OWNER\n" +
                                     "/sendToAll + msg\n" +
                                     "CLEAR ALL DATA FROM SpellBook\n" +
                                     "CLEAR ALL DATA FROM UserInventory\n" +
                                     "CLEAR ALL DATA ---- IT WILL DELETE ALL POSSIBLE DATA THAT EXISTS!\n" +
                                     "CLEAR ALL DATA FROM MagicStats\n" +
                                     "CLEAR ALL DATA FROM Research\n" +
                                     "CLEAR ALL DATA FROM UserData COUNTRY <CountryId>\n" +
                                     "CLEAR ALL DATA FROM Countries\n", LogTags.Data, LogTags.Cmd));
                    continue;
                }
                else if (trimmedMessage.StartsWith("/DEBUGMANA"))
                {
                    for (int i = 1; i <= 100; i++)
                        Console.WriteLine(Math.Round(100 * Math.Pow(1.15, i)));
                }
                else if (trimmedMessage.Equals("CLEAR ALL DATA", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        _botBodyMethods.ClearALlInventoryDataBase();
                        _botBodyMethods.ClearAllSpellDataBase();
                        _botBodyMethods.ClearALlDataBase();
                        _logger.LogInformation(LogTags.Tag("All data was erased.", LogTags.Data, LogTags.DbClear));
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, LogTags.Tag("Failed to erase all data.", LogTags.Data, LogTags.DbClear, LogTags.Fail));
                    }
                    continue;
                }

                _logger.LogWarning(LogTags.Tag($"Unknown command: {trimmedMessage}", LogTags.Cmd, LogTags.Warn));
            }
        }

        private async Task SendToAllUsers(string message)
        {
            var userIds = UserListDataBase.GetAllUsers();

            foreach (var userId in userIds)
            {
                try
                {
                    await _botClient.SendTextMessageAsync(userId, message);
                    _logger.LogInformation(LogTags.Tag($"Message sent to user {userId}.", LogTags.Broadcast));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, LogTags.Tag($"Failed to send message to user {userId}.", LogTags.Broadcast, LogTags.Fail));
                }
            }
        }


        public async Task SendMSG(string messageText)
        {
            await _botClient.SendTextMessageAsync(ownerID, messageText);
        }

        public async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            if (update.Message is Message message && message.Text != null)
            {
                if (message.From == null)
                    return;

                if (message.Chat != null && (message.Chat.Type == ChatType.Group || message.Chat.Type == ChatType.Supergroup))
                {
                    UpdateParliamentGroupChatId(message.Chat.Id);
                }

                var replyKeyboard = _botBodyMethods.ButtonsCreate(message.From.Id, false);

                UserListDataBase.AddUserToDataBase(message.From.Id);

                if (await TryHandleGlobalInputCancel(botClient, message, cancellationToken))
                    return;

                await _monsterRealisation.Monster(botClient, update, cancellationToken, message, replyKeyboard, _botBodyMethods);
                await _comandsRealisation.Commands(botClient, update, cancellationToken, message, replyKeyboard);
                await _botBodyMethods.WeaponOperations(botClient, update, cancellationToken, message, replyKeyboard);
                await _attackRealisation.AttackRealisationHandle(botClient, update, cancellationToken, message, replyKeyboard);
                await _botBodyMethods.CollectDataOperations(botClient, update, cancellationToken, message, replyKeyboard);
                await _inventoryCollector.InventoryCreationTaskHandle(botClient, cancellationToken, message);
                await _inventoryRealisation.InventoryTaskHandle(botClient, cancellationToken, message);
                await _botBodyMethods.SpellCreationTaskHandle(botClient, update, cancellationToken, message, replyKeyboard);
                await _walletRealisation.WalletTaskHandle(botClient, update, cancellationToken, message);
                await _levelSystem.AddExpirienceToUser(botClient, update, cancellationToken, message);
                await _percentModifierHandler.HandleTextInput(botClient, message, cancellationToken);
                await _stateParliamentHandle.HandleTextInput(botClient, message, cancellationToken);
                await _stateAdministration.HandleTextInput(botClient, message, cancellationToken);
                await _stateCreateOrHandleCountryCreation.HandleTextInput(botClient, message, cancellationToken);
            }
            else if (update.Type == UpdateType.CallbackQuery && update.CallbackQuery != null)
            {
                if (update.CallbackQuery.Message?.Chat != null &&
                    (update.CallbackQuery.Message.Chat.Type == ChatType.Group || update.CallbackQuery.Message.Chat.Type == ChatType.Supergroup))
                {
                    UpdateParliamentGroupChatId(update.CallbackQuery.Message.Chat.Id);
                }

                string callbackData = update.CallbackQuery.Data ?? string.Empty;
                if (callbackData.StartsWith("kb_", StringComparison.Ordinal))
                {
                    await HandleInlineKeyboardMenuCallbackAsText(botClient, update, cancellationToken);
                    return;
                }

                await _botBodyMethods.HandleCallbackQuery_6(botClient, update, cancellationToken, update.Message, update.CallbackQuery);
                await _attackRealisation.HandleAttackCallback(botClient, update.CallbackQuery, cancellationToken);
                await _walletRealisation.HandleCallbackWallet(botClient, update, cancellationToken, update.Message, update.CallbackQuery);
                await _comandsRealisation.HandleCallbackQ(botClient, update, cancellationToken, update.Message, update.CallbackQuery);
                await _inventoryRealisation.HandleInventoryCallback(botClient, update, cancellationToken, update.Message, update.CallbackQuery);
                await _botBodyMethods.HandleCallbackQuery_5(botClient, update, cancellationToken, update.Message, update.CallbackQuery);
                await EditSpellDamage.HandleCallbackQ(botClient, update, cancellationToken, update.Message, update.CallbackQuery);
                await DataCollector.HandleProfileCallback(botClient, update.CallbackQuery, cancellationToken);
                await _percentModifierHandler.HandleCallback(botClient, update.CallbackQuery, cancellationToken);
                await _percentModifierHandler.HandlePotionCallback(botClient, update.CallbackQuery, cancellationToken);
                await _stateAdministration.HandleCallbackQ(botClient, update, cancellationToken, update.Message, update.CallbackQuery);
                await _stateParliamentHandle.HandleCallbackQ(botClient, update, cancellationToken, update.Message, update.CallbackQuery);
                await _stateCreateOrHandleCountryCreation.HandleCallbackQ(botClient, update, cancellationToken, update.Message, update.CallbackQuery);
            }
        }

        private static bool IsCancelInput(string input)
        {
            string text = (input ?? string.Empty).Trim();
            return string.Equals(text, "/cancel", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(text, "скасувати", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(text, "відміна", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(text, "отмена", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(text, "cancel", StringComparison.OrdinalIgnoreCase);
        }

        private async Task<bool> TryHandleGlobalInputCancel(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
        {
            if (!IsCancelInput(message.Text) || message.From == null)
                return false;

            long userId = message.From.Id;
            bool cancelled = false;

            cancelled |= await _stateAdministration.TryCancelUserInputAsync(botClient, userId, cancellationToken);
            cancelled |= await _stateParliamentHandle.TryCancelUserInputAsync(botClient, userId, cancellationToken);
            cancelled |= await _stateCreateOrHandleCountryCreation.TryCancelUserInputAsync(botClient, userId, cancellationToken);
            cancelled |= TryCancelLegacyInputState(botClient, userId, cancellationToken);

            if (!cancelled)
                return false;

            await botClient.SendTextMessageAsync(message.Chat.Id, "Дію скасовано.", cancellationToken: cancellationToken);
            return true;
        }

        private static bool TryCancelLegacyInputState(ITelegramBotClient botClient, long userId, CancellationToken cancellationToken)
        {
            bool cancelled = false;

            if (userStateDataCollector.TryGetValue(userId, out var collectorState) && !string.IsNullOrWhiteSpace(collectorState))
            {
                userStateDataCollector[userId] = "";
                cancelled = true;
            }

            if (userStateDataChanger.TryGetValue(userId, out var changerState) && !string.IsNullOrWhiteSpace(changerState))
            {
                userStateDataChanger[userId] = "";
                cancelled = true;
            }

            if (DataChanger.userStateChangingDataFromDB.ContainsKey(userId))
            {
                DataChanger.userStateChangingDataFromDB.Remove(userId);
                cancelled = true;
            }

            if (DataChanger.userStateChangingCharacterId.ContainsKey(userId))
            {
                DataChanger.userStateChangingCharacterId.Remove(userId);
                cancelled = true;
            }

            if (userStateXP.TryGetValue(userId, out var xpState) && !string.IsNullOrWhiteSpace(xpState))
            {
                userStateXP[userId] = "";
                cancelled = true;
            }

            if (userStateMagicXP.TryGetValue(userId, out var magicXpState) && !string.IsNullOrWhiteSpace(magicXpState))
            {
                userStateMagicXP[userId] = "";
                cancelled = true;
            }

            if (userStateInventory.TryGetValue(userId, out var inventoryState) && !string.IsNullOrWhiteSpace(inventoryState))
            {
                userStateInventory[userId] = "";
                cancelled = true;
            }

            if (SpellCollector.TryCancelSpellInput(userId))
                cancelled = true;

            if (userStateWallet.TryGetValue(userId, out var walletState) && !string.IsNullOrWhiteSpace(walletState))
            {
                userStateWallet[userId] = "";
                cancelled = true;
            }

            if (userStateMethod.TryGetValue(userId, out var methodState) && !string.IsNullOrWhiteSpace(methodState))
            {
                userStateMethod[userId] = "";
                cancelled = true;
            }

            if (userStateValueName.ContainsKey(userId))
            {
                userStateValueName.Remove(userId);
                cancelled = true;
            }

            if (userStatePay.TryGetValue(userId, out var payState) && payState != 0)
            {
                userStatePay[userId] = 0;
                cancelled = true;
            }

            if (StateBotMSG.ContainsKey(userId) && StateBotMSG[userId] != 0)
            {
                StateBotMSG[userId] = 0;
                cancelled = true;
            }

            if (userId == masterID)
            {
                if (MonsterTemplateState.Remove(userId))
                    cancelled = true;
                if (SelectedMonsterTemplate.Remove(userId))
                    cancelled = true;

                if (userState.TryGetValue(MonsterCount, out var monsterState) && !string.IsNullOrWhiteSpace(monsterState))
                {
                    userState[MonsterCount] = "";
                    State = 0;
                    cancelled = true;
                }
            }

            if (cancelled && OldMessages.TryGetValue(userId, out var oldMessages) && oldMessages.Count > 0)
            {
                DeleteAllOldMessages(botClient, cancellationToken, userId);
                oldMessages.Clear();
            }

            return cancelled;
        }

        private async Task HandleInlineKeyboardMenuCallbackAsText(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            var callbackQuery = update.CallbackQuery;
            if (callbackQuery?.From == null)
                return;

            try
            {
                await botClient.AnswerCallbackQueryAsync(callbackQuery.Id, cancellationToken: cancellationToken);
            }
            catch (ApiRequestException ex) when ((ex.Message ?? string.Empty).Contains("query is too old", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogDebug("Skipping old kb callback answer. CallbackId={CallbackId}", callbackQuery.Id);
            }

            string text = BotKeyboardHelper.UnescapeCallback(callbackQuery.Data ?? string.Empty);
            var syntheticMessage = new Message
            {
                MessageId = 0,
                Date = DateTime.UtcNow,
                From = callbackQuery.From,
                Chat = callbackQuery.Message?.Chat ?? new Chat
                {
                    Id = callbackQuery.From.Id,
                    Type = ChatType.Private
                },
                Text = text
            };

            var syntheticUpdate = new Update
            {
                Message = syntheticMessage
            };

            await HandleUpdateAsync(botClient, syntheticUpdate, cancellationToken);
        }

        public Task HandleErrorAsync(
            ITelegramBotClient botClient,
            Exception exception,
            CancellationToken cancellationToken)
        {
            if (exception is ApiRequestException apiEx)
            {
                _logger.LogError(
                    apiEx,
                    LogTags.Tag("Telegram API error. Code: {Code}, Description: {Description}", LogTags.System, LogTags.Fail),
                    apiEx.ErrorCode,
                    apiEx.Message
                );
            }
            else
            {
                _logger.LogError(exception, LogTags.Tag("Unhandled exception.", LogTags.System, LogTags.Fail));
            }

            return Task.CompletedTask;
        }

    }
}
