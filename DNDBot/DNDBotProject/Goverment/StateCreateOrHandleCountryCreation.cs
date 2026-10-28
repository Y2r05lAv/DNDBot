using DNDBot.DataBase;
using DNDBot.Goverment.Research;
using DNDBot.Goverment.Economy;
using DNDBot;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace DNDBot.Goverment
{
    public class StateCreateOrHandleCountryCreation : BotBodyRealisation
    {
        private readonly ILogger<StateCreateOrHandleCountryCreation> _logger;

        private enum FieldKind
        {
            String,
            Int,
            Long,
            Float,
            Decimal,
            Bool,
            DateTime
        }

        private static readonly List<(string FieldName, string Prompt, FieldKind Kind)> CountryFields = new()
        {
            ("Name", "Введіть назву країни:", FieldKind.String),
            ("FlagEmoji", "Введіть прапор (emoji) країни:", FieldKind.String),
            ("FormOfGovernment", "Введіть форму правління:", FieldKind.String),

            ("Stability", "Стабільність (float 0-100):", FieldKind.Float),
            ("PopularLoyalty", "Лояльність населення (float 0-100):", FieldKind.Float),
            ("EliteLoyalty", "Лояльність еліт (float 0-100):", FieldKind.Float),
            ("Corruption", "Корупція (float 0-100):", FieldKind.Float),
            ("Legitimacy", "Легітимність (float 0-100):", FieldKind.Float),
            ("Control", "Контроль (float 0-100):", FieldKind.Float),
            ("Inflation", "Інфляція (float):", FieldKind.Float),

            ("Treasury", "Казна (long):", FieldKind.Long),
            ("TaxLevel", "Рівень податків (int 0-100):", FieldKind.Int),
            ("IncomePerTurn", "Доходи за день (decimal):", FieldKind.Decimal),
            ("ExpensesPerTurn", "Витрати за день (decimal):", FieldKind.Decimal),
            ("CorruptionLosses", "Втрати від корупції (decimal):", FieldKind.Decimal),
            ("WorkRate", "Work rate % (float 0-100):", FieldKind.Float),
            ("AvailableWorkers", "Available workers (long):", FieldKind.Long),
            ("UnemploymentRate", "Unemployment % (float 0-100):", FieldKind.Float),
            ("WarState", "Стан війни? (так/ні або 1/0):", FieldKind.Bool),
            ("SanctionsLevel", "Sanctions level (int 0=None 1=Partial 2=Full 3=Embargo):", FieldKind.Int),

            ("Population", "Населення (long):", FieldKind.Long),
            ("PopulationGrowth", "Приріст населення % за день (float):", FieldKind.Float),
            ("Happiness", "Щастя (float 0-100):", FieldKind.Float),
            ("Unrest", "Ризик бунтів (float 0-100):", FieldKind.Float),
            ("Refugees", "Біженці (long):", FieldKind.Long),

            ("NobilityInfluence", "Вплив знаті (float 0-100):", FieldKind.Float),
            ("MerchantsInfluence", "Вплив купців (float 0-100):", FieldKind.Float),
            ("ClergyInfluence", "Вплив духовенства (float 0-100):", FieldKind.Float),
            ("MilitaryInfluence", "Вплив військових (float 0-100):", FieldKind.Float),
            ("MagesInfluence", "Вплив магів (float 0-100):", FieldKind.Float),
            ("PeasantsInfluence", "Вплив селян (float 0-100):", FieldKind.Float),
            ("ExternalInfluence", "Зовнішній вплив (float 0-100):", FieldKind.Float),

            ("HasParliament", "Є парламент? (так/ні або 1/0):", FieldKind.Bool),
            ("ParliamentSeats", "Кількість місць у парламенті (int):", FieldKind.Int),
            ("ParliamentFractions", "Кількість фракцій (int):", FieldKind.Int),
            ("ParliamentSupport", "Підтримка уряду % (float 0-100):", FieldKind.Float),
            ("OppositionStrength", "Сила опозиції % (float 0-100):", FieldKind.Float),

            ("ActiveLawsCount", "Кількість активних законів (int):", FieldKind.Int),
            ("MaxLawsAllowed", "Максимум законів (int):", FieldKind.Int),
            ("EmergencyLawActive", "Надзвичайний стан активний? (так/ні або 1/0):", FieldKind.Bool),

            ("ArmySize", "Розмір армії (int):", FieldKind.Int),
            ("ArmyLoyalty", "Лояльність армії % (float 0-100):", FieldKind.Float),
            ("ArmyMorale", "Мораль армії % (float 0-100):", FieldKind.Float),
            ("MilitaryUpkeep", "Утримання армії (decimal):", FieldKind.Decimal),
            ("MartialLaw", "Воєнний стан? (так/ні або 1/0):", FieldKind.Bool),

            ("MagicInfluence", "Магічний вплив % (float 0-100):", FieldKind.Float),
            ("ManaStability", "Стабільність мани % (float 0-100):", FieldKind.Float),
            ("ActiveManaZones", "Активні мана-зони (int):", FieldKind.Int),
            ("ArcaneRisk", "Арканічний ризик % (float 0-100):", FieldKind.Float),

            ("Cities", "Кількість міст (int):", FieldKind.Int),
            ("Villages", "Кількість сіл (int):", FieldKind.Int),
            ("InfrastructureLevel", "Рівень інфраструктури % (float 0-100):", FieldKind.Float),
            ("LogisticsEfficiency", "Ефективність логістики % (float 0-100):", FieldKind.Float),

            ("ResearchLevel", "Рівень досліджень (int):", FieldKind.Int),
            ("ResearchSpeed", "Швидкість досліджень % (float 0-100):", FieldKind.Float),
            ("CompletedResearches", "Завершені дослідження (int):", FieldKind.Int),

            ("CoupRisk", "Ризик перевороту % (float 0-100):", FieldKind.Float),
            ("RebellionRisk", "Ризик повстання % (float 0-100):", FieldKind.Float),
            ("AssassinationRisk", "Ризик замаху % (float 0-100):", FieldKind.Float),

            ("Allies", "Союзники (int):", FieldKind.Int),
            ("Enemies", "Вороги (int):", FieldKind.Int),
            ("InternationalReputation", "Міжнародна репутація % (float 0-100):", FieldKind.Float),
            ("IntelLevel", "Рівень розвідки % (float 0-100):", FieldKind.Float),
            ("CounterIntelLevel", "Рівень контррозвідки % (float 0-100):", FieldKind.Float),

            ("LastUpdate", "Останнє оновлення (yyyy-MM-dd HH:mm) або порожньо:", FieldKind.DateTime),
            ("TurnsSurvived", "Кількість днів (int):", FieldKind.Int),

            ("IsInCrisis", "Криза? (так/ні або 1/0):", FieldKind.Bool),
            ("IsInGoldenAge", "Золотий вік? (так/ні або 1/0):", FieldKind.Bool),
            ("IsCollapsed", "Колапс? (так/ні або 1/0):", FieldKind.Bool)
        };

        private static readonly List<(string FieldName, string Prompt, FieldKind Kind)> QuickCountryFields = new()
        {
            ("Name", "Введіть назву країни:", FieldKind.String),
            ("FlagEmoji", "Введіть прапор (emoji) країни:", FieldKind.String),
            ("FormOfGovernment", "Введіть форму правління:", FieldKind.String),
            ("Treasury", "Казна (long):", FieldKind.Long),
            ("Population", "Населення (long):", FieldKind.Long),
            ("PopulationGrowth", "Приріст населення % за день (float):", FieldKind.Float),
            ("Stability", "Стабільність (float 0-100):", FieldKind.Float),
            ("PopularLoyalty", "Лояльність населення (float 0-100):", FieldKind.Float),
            ("Corruption", "Корупція (float 0-100):", FieldKind.Float)
        };

        private enum CreationMode
        {
            Full,
            Quick
        }

        private enum QuickCountryPreset
        {
            Poor,
            Standard,
            Economy,
            Prosperous
        }

        private static readonly Dictionary<long, int> CountryStep = new();
        private static readonly Dictionary<long, CountryRecord> CountryBuffer = new();
        private static readonly Dictionary<long, string> CountryState = new();
        private static readonly Dictionary<long, CreationMode> CountryMode = new();
        private static readonly Dictionary<long, QuickCountryPreset> QuickCountryPresetByUser = new();
        private static readonly Dictionary<long, int> AdminSelectedCharacter = new();
        private static readonly Dictionary<long, int> AdminMergeTargetByUser = new();

        public StateCreateOrHandleCountryCreation(ILogger<StateCreateOrHandleCountryCreation> logger)
        {
            _logger = logger;
        }

        public async Task HandleCallbackQ(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken, Message message, CallbackQuery callbackQuery)
        {
            string data = callbackQuery.Data ?? "";
            long userId = callbackQuery.From.Id;

            if (!data.StartsWith("adminstate_"))
                return;

            if (callbackQuery.Message == null)
                return;
            if (callbackQuery.Message.Chat.Type != Telegram.Bot.Types.Enums.ChatType.Private)
            {
                await SafeAnswerCallback(botClient, callbackQuery, "Держуправління працює в особистому чаті.", cancellationToken);
                return;
            }

            if (userId != masterID)
            {
                await SafeAnswerCallback(botClient, callbackQuery, "Недостатньо прав.", cancellationToken);
                return;
            }

            if (data.StartsWith("adminstate_deletecountry_confirm_", StringComparison.OrdinalIgnoreCase))
            {
                var partsConfirm = data.Split('_');
                if (partsConfirm.Length >= 5 &&
                    int.TryParse(partsConfirm[3], out int countryId) &&
                    int.TryParse(partsConfirm[4], out int step))
                {
                    if (step <= 1)
                    {
                        await ShowDeleteCountryConfirm(botClient, userId, callbackQuery.Message.MessageId, countryId, cancellationToken, 2);
                    }
                    else if (step == 2)
                    {
                        await ShowDeleteCountryConfirm(botClient, userId, callbackQuery.Message.MessageId, countryId, cancellationToken, 3);
                    }
                    else
                    {
                        await DeleteCountryById(botClient, userId, callbackQuery.Message.MessageId, countryId, cancellationToken);
                    }
                }
                await SafeAnswerCallback(botClient, callbackQuery, "Готово.", cancellationToken);
                return;
            }

            if (data.StartsWith("adminstate_deletecountry_back_", StringComparison.OrdinalIgnoreCase))
            {
                var partsBack = data.Split('_');
                if (partsBack.Length >= 5 &&
                    int.TryParse(partsBack[3], out int countryId) &&
                    int.TryParse(partsBack[4], out int step))
                {
                    if (step <= 1)
                    {
                        await ShowDeleteCountryList(botClient, userId, callbackQuery.Message.MessageId, cancellationToken);
                    }
                    else if (step == 2)
                    {
                        await ShowDeleteCountryConfirm(botClient, userId, callbackQuery.Message.MessageId, countryId, cancellationToken, 1);
                    }
                    else
                    {
                        await ShowDeleteCountryConfirm(botClient, userId, callbackQuery.Message.MessageId, countryId, cancellationToken, 2);
                    }
                }
                await SafeAnswerCallback(botClient, callbackQuery, "Готово.", cancellationToken);
                return;
            }

            if (data.StartsWith("adminstate_deletecountry_"))
            {
                if (TryParseId(data, 2, out int countryId))
                {
                    await ShowDeleteCountryConfirm(botClient, userId, callbackQuery.Message.MessageId, countryId, cancellationToken, 1);
                    await SafeAnswerCallback(botClient, callbackQuery, "Готово.", cancellationToken);
                    return;
                }
            }

            if (data == "adminstate_merge")
            {
                AdminMergeTargetByUser.Remove(userId);
                await ShowMergeTargetList(botClient, userId, callbackQuery.Message.MessageId, cancellationToken);
                await SafeAnswerCallback(botClient, callbackQuery, "Готово.", cancellationToken);
                return;
            }

            if (data == "adminstate_merge_back_target")
            {
                AdminMergeTargetByUser.Remove(userId);
                await ShowMergeTargetList(botClient, userId, callbackQuery.Message.MessageId, cancellationToken);
                await SafeAnswerCallback(botClient, callbackQuery, "Готово.", cancellationToken);
                return;
            }

            if (data == "adminstate_merge_back_source")
            {
                if (AdminMergeTargetByUser.TryGetValue(userId, out int targetId))
                {
                    await ShowMergeSourceList(botClient, userId, callbackQuery.Message.MessageId, cancellationToken, targetId);
                }
                else
                {
                    await ShowMergeTargetList(botClient, userId, callbackQuery.Message.MessageId, cancellationToken);
                }
                await SafeAnswerCallback(botClient, callbackQuery, "Готово.", cancellationToken);
                return;
            }

            if (data.StartsWith("adminstate_merge_target_", StringComparison.OrdinalIgnoreCase))
            {
                if (TryParseId(data, 3, out int targetId))
                {
                    AdminMergeTargetByUser[userId] = targetId;
                    await ShowMergeSourceList(botClient, userId, callbackQuery.Message.MessageId, cancellationToken, targetId);
                }
                await SafeAnswerCallback(botClient, callbackQuery, "Готово.", cancellationToken);
                return;
            }

            if (data.StartsWith("adminstate_merge_source_", StringComparison.OrdinalIgnoreCase))
            {
                if (TryParseId(data, 3, out int sourceId) && AdminMergeTargetByUser.TryGetValue(userId, out int targetId))
                {
                    await ShowMergeConfirm(botClient, userId, callbackQuery.Message.MessageId, cancellationToken, targetId, sourceId);
                }
                else
                {
                    await ShowMergeTargetList(botClient, userId, callbackQuery.Message.MessageId, cancellationToken);
                }
                await SafeAnswerCallback(botClient, callbackQuery, "Готово.", cancellationToken);
                return;
            }

            if (data.StartsWith("adminstate_merge_confirm_", StringComparison.OrdinalIgnoreCase))
            {
                if (TryParseId(data, 3, out int sourceId) && AdminMergeTargetByUser.TryGetValue(userId, out int targetId))
                {
                    string result = CountriesData.MergeCountries(targetId, sourceId);
                    AdminMergeTargetByUser.Remove(userId);
                    await SafeEditMessageTextAsync(
                botClient: botClient,
                chatId: userId,
                        messageId: callbackQuery.Message.MessageId,
                        text: result,
                        replyMarkup: StateKeyboardBuilder.AddOrApplyCountryKeyboard(),
                        cancellationToken: cancellationToken
                    );
                }
                else
                {
                    await ShowMergeTargetList(botClient, userId, callbackQuery.Message.MessageId, cancellationToken);
                }
                await SafeAnswerCallback(botClient, callbackQuery, "Готово.", cancellationToken);
                return;
            }

            if (data.StartsWith("adminstate_assignchar_"))
            {
                if (TryParseId(data, 2, out int characterId))
                {
                    AdminSelectedCharacter[userId] = characterId;
                    await ShowAssignCountryList(botClient, userId, callbackQuery.Message.MessageId, cancellationToken);
                }

                await SafeAnswerCallback(botClient, callbackQuery, "Готово.", cancellationToken);
                return;
            }

            if (data.StartsWith("adminstate_assigncountry_"))
            {
                if (TryParseId(data, 2, out int countryId) && AdminSelectedCharacter.TryGetValue(userId, out int characterId))
                {
                    var result = UserData.SetCharacterCountry(characterId, countryId);
                    await SafeEditMessageTextAsync(
                botClient: botClient,
                chatId: userId,
                        messageId: callbackQuery.Message.MessageId,
                        text: result,
                        replyMarkup: StateKeyboardBuilder.AddOrApplyCountryKeyboard(),
                        cancellationToken: cancellationToken
                    );
                }

                await SafeAnswerCallback(botClient, callbackQuery, "Готово.", cancellationToken);
                return;
            }

            if (data == "adminstate_deleteall_confirm")
            {
                await ShowDeleteAllConfirm(botClient, userId, callbackQuery.Message.MessageId, cancellationToken, 2);
                await SafeAnswerCallback(botClient, callbackQuery, "Готово.", cancellationToken);
                return;
            }

            if (data.StartsWith("adminstate_deleteall_confirm_", StringComparison.OrdinalIgnoreCase))
            {
                var partsConfirm = data.Split('_');
                if (partsConfirm.Length >= 4 && int.TryParse(partsConfirm[3], out int step))
                {
                    if (step <= 1)
                    {
                        await ShowDeleteAllConfirm(botClient, userId, callbackQuery.Message.MessageId, cancellationToken, 2);
                    }
                    else if (step == 2)
                    {
                        await ShowDeleteAllConfirm(botClient, userId, callbackQuery.Message.MessageId, cancellationToken, 3);
                    }
                    else
                    {
                        await DeleteAllCountries(botClient, userId, callbackQuery.Message.MessageId, cancellationToken);
                    }
                }
                await SafeAnswerCallback(botClient, callbackQuery, "Готово.", cancellationToken);
                return;
            }

            if (data.StartsWith("adminstate_deleteall_back_", StringComparison.OrdinalIgnoreCase))
            {
                var partsBack = data.Split('_');
                if (partsBack.Length >= 4 && int.TryParse(partsBack[3], out int step))
                {
                    if (step <= 1)
                    {
                        await ShowAdminPanel(botClient, userId, callbackQuery.Message.MessageId, cancellationToken);
                    }
                    else if (step == 2)
                    {
                        await ShowDeleteAllConfirm(botClient, userId, callbackQuery.Message.MessageId, cancellationToken, 1);
                    }
                    else
                    {
                        await ShowDeleteAllConfirm(botClient, userId, callbackQuery.Message.MessageId, cancellationToken, 2);
                    }
                }
                await SafeAnswerCallback(botClient, callbackQuery, "Готово.", cancellationToken);
                return;
            }

            if (string.Equals(data, "adminstate_quickcountry", StringComparison.OrdinalIgnoreCase))
            {
                await ShowQuickCountryPresetMenu(botClient, userId, callbackQuery.Message.MessageId, cancellationToken);
                await SafeAnswerCallback(botClient, callbackQuery, "Оберіть старт.", cancellationToken);
                return;
            }

            if (string.Equals(data, "adminstate_quickcountry_back", StringComparison.OrdinalIgnoreCase))
            {
                await ShowAdminPanel(botClient, userId, callbackQuery.Message.MessageId, cancellationToken);
                await SafeAnswerCallback(botClient, callbackQuery, "Повернулись.", cancellationToken);
                return;
            }

            if (data.StartsWith("adminstate_quickcountry_profile_", StringComparison.OrdinalIgnoreCase))
            {
                string rawPreset = data.Substring("adminstate_quickcountry_profile_".Length);
                QuickCountryPreset preset = ParseQuickCountryPreset(rawPreset);
                QuickCountryPresetByUser[userId] = preset;

                await StartCountryCreation(botClient, userId, cancellationToken, CreationMode.Quick);
                await SafeAnswerCallback(botClient, callbackQuery, $"Старт: {FormatQuickCountryPresetName(preset)}", cancellationToken);
                return;
            }

            var parts = data.Split('_');
            if (parts.Length < 2)
                return;
            string info = parts[1];

            switch (info)
            {
                case "addcountry":
                    await StartCountryCreation(botClient, userId, cancellationToken, CreationMode.Full);
                    break;
                case "quickcountry":
                    await StartCountryCreation(botClient, userId, cancellationToken, CreationMode.Quick);
                    break;
                case "listcountries":
                    await ShowCountriesList(botClient, userId, cancellationToken);
                    break;
                case "deleteone":
                    await ShowDeleteCountryList(botClient, userId, callbackQuery.Message.MessageId, cancellationToken);
                    break;
                case "deleteall":
                    await ShowDeleteAllConfirm(botClient, userId, callbackQuery.Message.MessageId, cancellationToken, 1);
                    break;
                case "assign":
                    await ShowAssignCharacterList(botClient, userId, callbackQuery.Message.MessageId, cancellationToken);
                    break;
                case "cancel":
                    await ShowAdminPanel(botClient, userId, callbackQuery.Message.MessageId, cancellationToken);
                    break;
                default:
                    break;
            }

            await SafeAnswerCallback(botClient, callbackQuery, "Готово.", cancellationToken);
        }

        public async Task HandleTextInput(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
        {
            if (message == null || message.From == null)
                return;
            if (message.Chat == null || message.Chat.Type != Telegram.Bot.Types.Enums.ChatType.Private)
                return;

            long userId = message.From.Id;

            if (!CountryState.TryGetValue(userId, out string state) || state != "create")
                return;

            if (!CountryStep.ContainsKey(userId) || !CountryBuffer.ContainsKey(userId))
                return;

            string input = (message.Text ?? string.Empty).Trim();
            if (!OldMessages.ContainsKey(userId))
                OldMessages[userId] = new List<int>();
            OldMessages[userId].Add(message.MessageId);

            int index = CountryStep[userId];
            var mode = CountryMode.TryGetValue(userId, out var m) ? m : CreationMode.Full;
            var fields = mode == CreationMode.Quick ? QuickCountryFields : CountryFields;

            if (input != "Повернутись" && index < fields.Count)
            {
                var field = fields[index];
                if (string.IsNullOrWhiteSpace(input) && field.Kind != FieldKind.DateTime)
                {
                    await botClient.SendTextMessageAsync(userId, "❗ Введіть непорожнє значення.", cancellationToken: cancellationToken);
                    return;
                }

                if (!TryAssignField(CountryBuffer[userId], field.FieldName, field.Kind, input, out string error))
                {
                    await botClient.SendTextMessageAsync(userId, error, cancellationToken: cancellationToken);
                    return;
                }
                CountryStep[userId]--;
            }
            else
            {
                if (index < fields.Count - 1)
                    CountryStep[userId]++;
            }

            if (CountryStep[userId] < 0)
            {
                await SaveCountry(botClient, userId, cancellationToken);
                return;
            }

            int currentIndex = CountryStep[userId];
            if (currentIndex >= fields.Count)
                currentIndex = fields.Count - 1;
            string prompt = fields[currentIndex].Prompt;

            int msgId = (await botClient.SendTextMessageAsync(
                userId,
                prompt,
                cancellationToken: cancellationToken
            )).MessageId;

            OldMessages[userId].Add(msgId);
        }

        public async Task<bool> TryCancelUserInputAsync(ITelegramBotClient botClient, long userId, CancellationToken cancellationToken)
        {
            bool hasCreateState = CountryState.TryGetValue(userId, out string state) &&
                                  string.Equals(state, "create", StringComparison.OrdinalIgnoreCase);
            bool hasPendingData = CountryStep.ContainsKey(userId) ||
                                  CountryBuffer.ContainsKey(userId) ||
                                  CountryMode.ContainsKey(userId) ||
                                  QuickCountryPresetByUser.ContainsKey(userId);

            if (!hasCreateState && !hasPendingData)
                return false;

            CountryState[userId] = "";
            CountryStep.Remove(userId);
            CountryBuffer.Remove(userId);
            CountryMode.Remove(userId);
            QuickCountryPresetByUser.Remove(userId);
            AdminSelectedCharacter.Remove(userId);
            AdminMergeTargetByUser.Remove(userId);

            if (OldMessages.TryGetValue(userId, out var oldMessages) && oldMessages.Count > 0)
            {
                DeleteAllOldMessages(botClient, cancellationToken, userId);
                oldMessages.Clear();
            }

            await Task.CompletedTask;
            return true;
        }

        private async Task StartCountryCreation(ITelegramBotClient botClient, long userId, CancellationToken cancellationToken, CreationMode mode)
        {
            if (!OldMessages.ContainsKey(userId))
                OldMessages[userId] = new List<int>();

            CountryBuffer[userId] = CreateDefaultCountry();
            CountryMode[userId] = mode;
            if (mode == CreationMode.Quick && !QuickCountryPresetByUser.ContainsKey(userId))
                QuickCountryPresetByUser[userId] = QuickCountryPreset.Standard;

            var fields = mode == CreationMode.Quick ? QuickCountryFields : CountryFields;
            CountryStep[userId] = fields.Count - 1;
            CountryState[userId] = "create";

            int msgId = (await botClient.SendTextMessageAsync(
                userId,
                fields[CountryStep[userId]].Prompt,
                cancellationToken: cancellationToken
            )).MessageId;

            OldMessages[userId].Add(msgId);
            _logger.LogInformation("Country creation started for user {UserId}", userId);
        }

        private async Task SaveCountry(ITelegramBotClient botClient, long userId, CancellationToken cancellationToken)
        {
            CountryState[userId] = "";
            CountryStep[userId] = -1;

            var country = CountryBuffer[userId];
            var mode = CountryMode.TryGetValue(userId, out var creationMode) ? creationMode : CreationMode.Full;
            var quickPreset = QuickCountryPresetByUser.TryGetValue(userId, out var storedPreset)
                ? storedPreset
                : QuickCountryPreset.Standard;
            country.CreatedAt = DateTime.Now;

            string result;
            try
            {
                result = CountriesData.AddCountry(country);
                if (TryExtractAddedCountryId(result, out int countryId) && countryId > 0)
                {
                    PopulationCalculator.SavePopulationSnapshot(countryId, PopulationCalculator.ComputePopulation(Math.Max(0L, country.Population)));

                    if (mode == CreationMode.Quick)
                    {
                        var bootstrapSummary = BootstrapQuickCountry(countryId, country, quickPreset);
                        if (!string.IsNullOrWhiteSpace(bootstrapSummary))
                            result = $"{result}\n\n{bootstrapSummary}";
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Save country failed. UserId={UserId}", userId);
                result = $"❌ Помилка збереження країни: {ex.Message}";
            }

            await botClient.SendTextMessageAsync(userId, result, cancellationToken: cancellationToken);

            CountryBuffer.Remove(userId);
            CountryMode.Remove(userId);
            QuickCountryPresetByUser.Remove(userId);
            DeleteAllOldMessages(botClient, cancellationToken, userId);
        }

        private string BootstrapQuickCountry(int countryId, CountryRecord country, QuickCountryPreset preset)
        {
            int buildingsAdded = 0;
            int companiesAdded = 0;
            int completedResearch = 0;
            long generatedJobs = 0L;
            long treasuryReserveAdded = 0L;
            string activeResearchName = string.Empty;
            ProductionBalanceResult productionBalance = null;

            try
            {
                var persistedCountry = CountriesData.GetCountryById(countryId) ?? country;
                StabilizeQuickCountryProfile(countryId, persistedCountry, preset, out treasuryReserveAdded);
                EnsureQuickCountryResources();

                var seededBuildings = GenerateQuickCountryBuildings(countryId, persistedCountry, preset);
                generatedJobs = seededBuildings.Sum(b => (long)Math.Max(0, b.JobsProvided));
                var insertedBuildings = new List<(int BuildingId, EconomyBuildingRecord Building)>();
                foreach (var building in seededBuildings)
                {
                    int buildingId = EconomyBuildingsData.AddBuilding(building);
                    if (buildingId <= 0)
                        continue;

                    buildingsAdded++;
                    insertedBuildings.Add((buildingId, building));
                }

                var seededCompanies = GenerateQuickCountryCompanies(countryId, persistedCountry, seededBuildings, preset);
                var companyByType = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                foreach (var company in seededCompanies)
                {
                    int companyId = EconomyCompaniesData.AddCompany(company);
                    if (companyId <= 0)
                        continue;

                    companiesAdded++;
                    if (!companyByType.ContainsKey(company.ProductionType))
                        companyByType[company.ProductionType] = companyId;
                }

                foreach (var pair in insertedBuildings)
                {
                    string targetType = ResolveCompanyTypeForBuilding(pair.Building);
                    if (!companyByType.TryGetValue(targetType, out int companyId) || companyId <= 0)
                        continue;

                    EconomyBuildingsData.UpdateBuildingField(
                        pair.BuildingId,
                        countryId,
                        "CompanyId",
                        companyId,
                        SqlDbType.Int
                    );
                }

                decimal socialShare = persistedCountry.PopularLoyalty < 50f ? 0.24m : 0.20m;
                decimal militaryShare = persistedCountry.WarState ? 0.16m : 0.10m;
                EconomyBudgetData.Upsert(new EconomyBudgetData.EconomyBudgetRecord
                {
                    CountryId = countryId,
                    InfrastructureShare = 0.14m,
                    MilitaryShare = militaryShare,
                    ScienceShare = 0.08m,
                    SocialShare = socialShare,
                    UpdatedAt = DateTime.UtcNow
                });

                var researchSeed = SeedQuickCountryResearch(countryId, persistedCountry);
                completedResearch = researchSeed.Completed;
                activeResearchName = researchSeed.ActiveResearchName;

                long sustainabilityReserveAdded = ApplyQuickCountrySustainabilityPass(countryId, preset, out productionBalance);
                treasuryReserveAdded = SafeAddLong(treasuryReserveAdded, sustainabilityReserveAdded);
                if (productionBalance != null && productionBalance.AddedBuildings > 0)
                {
                    buildingsAdded += productionBalance.AddedBuildings;
                    generatedJobs = SafeAddLong(generatedJobs, productionBalance.AddedJobs);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Quick country bootstrap failed. CountryId={CountryId}", countryId);
                return "⚠ Країну створено, але стартовий пакет економіки застосовано частково.";
            }

            var sb = new StringBuilder();
            sb.AppendLine("⚡ Швидкий старт згенеровано");
            sb.AppendLine($"🧭 Профіль: {FormatQuickCountryPresetName(preset)}");
            sb.AppendLine($"🏗 Будівель: {buildingsAdded}");
            sb.AppendLine($"🏭 Компаній: {companiesAdded}");
            sb.AppendLine($"👷 Робочих місць: {generatedJobs:N0}");
            if (treasuryReserveAdded > 0)
                sb.AppendLine($"💼 Резерв казни додано: +{treasuryReserveAdded:N0}");
            if (productionBalance != null && productionBalance.Success)
            {
                sb.AppendLine($"⚖ Баланс виробництва: профіцит {productionBalance.SurplusAfter}/5, дефіцит {productionBalance.DeficitAfter}/5");
                if (productionBalance.AddedBuildings > 0)
                    sb.AppendLine($"🏭 L5 для балансу: +{productionBalance.AddedBuildings} (робочі місця +{productionBalance.AddedJobs:N0})");
            }
            sb.AppendLine($"🧠 Завершених досліджень: {completedResearch}");
            if (!string.IsNullOrWhiteSpace(activeResearchName))
                sb.AppendLine($"🔬 Активне дослідження: {activeResearchName}");
            return sb.ToString().TrimEnd();
        }

        private static void StabilizeQuickCountryProfile(int countryId, CountryRecord country, QuickCountryPreset preset, out long treasuryReserveAdded)
        {
            treasuryReserveAdded = 0L;
            if (country == null)
                return;

            long population = country.Population > 0 ? country.Population : 10_000L;
            var populationSnapshot = PopulationCalculator.ComputePopulation(population);
            long workingAge = Math.Max(1L, populationSnapshot.WorkingAge);

            country.Population = population;
            int defaultTax = preset switch
            {
                QuickCountryPreset.Poor => 19,
                QuickCountryPreset.Economy => 15,
                QuickCountryPreset.Prosperous => 14,
                _ => 16
            };

            int taxLevel = country.TaxLevel <= 0 ? defaultTax : country.TaxLevel;
            if (preset == QuickCountryPreset.Economy && taxLevel < defaultTax)
                taxLevel = defaultTax;
            country.TaxLevel = Math.Clamp(taxLevel, 10, 30);

            double citizensPerCity = preset switch
            {
                QuickCountryPreset.Prosperous => 125_000d,
                QuickCountryPreset.Economy => 132_000d,
                _ => 145_000d
            };
            double citizensPerVillage = preset switch
            {
                QuickCountryPreset.Poor => 7_000d,
                QuickCountryPreset.Economy => 8_200d,
                _ => 8_500d
            };

            country.Cities = Math.Max(country.Cities, Math.Clamp((int)Math.Ceiling(population / citizensPerCity), 1, 120));
            country.Villages = Math.Max(country.Villages, Math.Clamp((int)Math.Ceiling(population / citizensPerVillage), 6, 3_200));

            float infraMin = preset switch
            {
                QuickCountryPreset.Poor => 50f,
                QuickCountryPreset.Economy => 63f,
                QuickCountryPreset.Prosperous => 68f,
                _ => 57f
            };
            float logisticsMin = preset switch
            {
                QuickCountryPreset.Poor => 52f,
                QuickCountryPreset.Economy => 66f,
                QuickCountryPreset.Prosperous => 70f,
                _ => 60f
            };
            float researchSpeedMin = preset switch
            {
                QuickCountryPreset.Poor => 48f,
                QuickCountryPreset.Economy => 60f,
                QuickCountryPreset.Prosperous => 66f,
                _ => 56f
            };
            int researchLevelMin = preset switch
            {
                QuickCountryPreset.Poor => 1,
                QuickCountryPreset.Economy => 2,
                QuickCountryPreset.Prosperous => 2,
                _ => 1
            };
            float happinessMin = preset switch
            {
                QuickCountryPreset.Poor => 46f,
                QuickCountryPreset.Economy => 58f,
                QuickCountryPreset.Prosperous => 61f,
                _ => 53f
            };
            float inflationDefault = preset switch
            {
                QuickCountryPreset.Poor => 11f,
                QuickCountryPreset.Economy => 6f,
                QuickCountryPreset.Prosperous => 6.5f,
                _ => 8f
            };
            float unrestDefault = preset switch
            {
                QuickCountryPreset.Poor => 24f,
                QuickCountryPreset.Economy => 11f,
                QuickCountryPreset.Prosperous => 12f,
                _ => 18f
            };

            country.InfrastructureLevel = Math.Max(country.InfrastructureLevel, infraMin);
            country.LogisticsEfficiency = Math.Max(country.LogisticsEfficiency, logisticsMin);
            country.ResearchSpeed = Math.Max(country.ResearchSpeed, researchSpeedMin);
            country.ResearchLevel = Math.Max(country.ResearchLevel, researchLevelMin);
            float controlMin = preset switch
            {
                QuickCountryPreset.Poor => 52f,
                QuickCountryPreset.Economy => 60f,
                _ => 57f
            };
            country.Control = Math.Max(country.Control, controlMin);
            country.Happiness = Math.Max(country.Happiness, happinessMin);
            country.Inflation = EconomyCalculator.Clamp(country.Inflation <= 0f ? inflationDefault : country.Inflation, 2f, 20f);
            country.Unrest = EconomyCalculator.Clamp(country.Unrest <= 0f ? unrestDefault : country.Unrest, 4f, 45f);

            country.WarState = false;
            country.MartialLaw = false;
            if (country.ArmySize <= 0)
            {
                double armyShare = preset switch
                {
                    QuickCountryPreset.Poor => 0.015d,
                    QuickCountryPreset.Economy => 0.0175d,
                    QuickCountryPreset.Prosperous => 0.018d,
                    _ => 0.017d
                };
                country.ArmySize = Math.Clamp((int)Math.Round(workingAge * armyShare), 120, 25_000);
            }

            country.AvailableWorkers = PopulationCalculator.ComputeCivilianWorkforce(populationSnapshot.WorkingAge, country.ArmySize, country.WarState);
            country.WorkRate = PopulationCalculator.ComputeWorkRate(country.Population, populationSnapshot.WorkingAge);
            country.LastUpdate = DateTime.UtcNow;

            CountriesData.UpdateCountry(country);
            PopulationCalculator.SavePopulationSnapshot(countryId, populationSnapshot);
            ApplyQuickCountryConfigOverrides(countryId, country, preset);
        }

        private static void ApplyQuickCountryConfigOverrides(int countryId, CountryRecord country, QuickCountryPreset preset)
        {
            if (countryId <= 0)
                return;

            decimal population = Math.Max(1m, country.Population);
            decimal presetSalaryMult = preset switch
            {
                QuickCountryPreset.Poor => 0.90m,
                QuickCountryPreset.Economy => 1.06m,
                QuickCountryPreset.Prosperous => 1.16m,
                _ => 1.00m
            };

            decimal salaryBase = ClampDecimalValue(0.80m + (decimal)Math.Log10((double)Math.Max(10m, population)) * 0.06m, 0.85m, 1.35m);
            decimal companySalary = decimal.Round(salaryBase * 1.04m * presetSalaryMult, 4, MidpointRounding.AwayFromZero);
            decimal baseWage = decimal.Round(Math.Max(0.78m, companySalary * 0.92m), 4, MidpointRounding.AwayFromZero);
            decimal deputySalary = ClampDecimalValue(companySalary * 2.4m, 2m, 6m);
            decimal soldierSalary = ClampDecimalValue(companySalary * 0.55m, 0.40m, 1.10m);

            decimal stateOrderShare = preset switch
            {
                QuickCountryPreset.Poor => 0.006m,
                QuickCountryPreset.Economy => 0.007m,
                QuickCountryPreset.Prosperous => 0.010m,
                _ => 0.008m
            };
            if (country.WarState)
                stateOrderShare = ClampDecimalValue(stateOrderShare * 1.35m, 0.006m, 0.016m);

            decimal stateOrderMaxBase = preset switch
            {
                QuickCountryPreset.Poor => population * 0.024m,
                QuickCountryPreset.Economy => population * 0.034m,
                QuickCountryPreset.Prosperous => population * 0.040m,
                _ => population * 0.030m
            };
            decimal stateOrderMax = ClampDecimalValue(stateOrderMaxBase, 4_000m, 260_000m);
            decimal stateOrderWarMult = preset switch
            {
                QuickCountryPreset.Prosperous => 1.26m,
                QuickCountryPreset.Economy => 1.24m,
                _ => 1.22m
            };

            EconomyConfigData.UpsertCountryDecimal(countryId, "CompanyBaseSalary", companySalary);
            EconomyConfigData.UpsertCountryDecimal(countryId, "BaseWage", baseWage);
            EconomyConfigData.UpsertCountryDecimal(countryId, "DeputySalaryBase", deputySalary);
            EconomyConfigData.UpsertCountryDecimal(countryId, "SoldierSalaryPerTurn", soldierSalary);
            EconomyConfigData.UpsertCountryDecimal(countryId, "StateOrderTreasuryShare", stateOrderShare);
            EconomyConfigData.UpsertCountryDecimal(countryId, "StateOrderBudgetMax", stateOrderMax);
            EconomyConfigData.UpsertCountryDecimal(countryId, "StateOrderWarMultiplier", stateOrderWarMult);
        }

        private static long ApplyQuickCountrySustainabilityPass(int countryId, QuickCountryPreset preset, out ProductionBalanceResult productionBalance)
        {
            productionBalance = null;
            if (countryId <= 0)
                return 0L;

            var country = CountriesData.GetCountryById(countryId);
            if (country == null)
                return 0L;

            var economyService = new EconomyService();
            int previewDays = preset switch
            {
                QuickCountryPreset.Poor => 90,
                QuickCountryPreset.Economy => 210,
                QuickCountryPreset.Prosperous => 180,
                _ => 120
            };
            var preview = economyService.PreviewTurn(countryId, previewDays);
            long currentTreasury = country.Treasury;

            if (preview.TreasuryAfter <= (long)Math.Round(currentTreasury * 0.25d))
            {
                country.TaxLevel = Math.Clamp(country.TaxLevel + 2, 12, 24);
                CountriesData.UpdateCountry(country);

                decimal oldStateOrderShare = EconomyConfigData.GetDecimalForCountry(countryId, "StateOrderTreasuryShare", 0.02m);
                decimal tunedStateOrderShare = ClampDecimalValue(oldStateOrderShare * 0.85m, 0.004m, 0.015m);
                EconomyConfigData.UpsertCountryDecimal(countryId, "StateOrderTreasuryShare", tunedStateOrderShare);

                decimal oldCompanySalary = EconomyConfigData.GetDecimalForCountry(countryId, "CompanyBaseSalary", 1m);
                decimal tunedCompanySalary = ClampDecimalValue(oldCompanySalary * 0.92m, 0.78m, 1.40m);
                EconomyConfigData.UpsertCountryDecimal(countryId, "CompanyBaseSalary", tunedCompanySalary);
                EconomyConfigData.UpsertCountryDecimal(countryId, "BaseWage", ClampDecimalValue(tunedCompanySalary * 0.92m, 0.72m, 1.20m));

                preview = economyService.PreviewTurn(countryId, previewDays);
                country = CountriesData.GetCountryById(countryId) ?? country;
            }

            if (preset == QuickCountryPreset.Economy)
            {
                productionBalance = ProductionBalancingService.BalanceCountryProduction(
                    countryId,
                    new ProductionBalanceOptions
                    {
                        StrictEconomyMode = false,
                        SurplusTargetCount = 1,
                        SurplusCoverageRatio = 1.00m,
                        NearBalanceCoverageRatio = 0.88m,
                        LeaderCoverageRatio = 0.96m,
                        EnableOversupplyCorrection = true,
                        OversupplyCeilingRatio = 0.92m,
                        LeaderOversupplyCeilingRatio = 1.04m,
                        MaxBuildingsToDisable = 300,
                        MaxIterations = 8,
                        MaxBuildingsToAdd = 120
                    });
                BalanceEconomyPresetDailyNet(countryId, economyService, previewDays, targetAbsDailyNet: 1_000m);
            }

            return 0L;
        }

        private static void BalanceEconomyPresetDailyNet(int countryId, EconomyService economyService, int previewDays, decimal targetAbsDailyNet)
        {
            if (countryId <= 0 || economyService == null || previewDays <= 0)
                return;

            const int maxIterations = 14;

            for (int i = 0; i < maxIterations; i++)
            {
                var preview = economyService.PreviewTurn(countryId, previewDays);
                 
                 
                decimal rawNetTotal = (decimal)preview.Income - (decimal)preview.Expenses;
                decimal dailyNet = rawNetTotal / Math.Max(1m, previewDays);
                decimal dailyAbs = Math.Abs(dailyNet);
                if (dailyAbs <= targetAbsDailyNet)
                    return;

                var country = CountriesData.GetCountryById(countryId);
                if (country == null)
                    return;

                decimal imbalance = dailyAbs - targetAbsDailyNet;
                decimal pressure = ClampDecimalValue(imbalance / 18_000m, 0.03m, 0.30m);
                bool changed = false;

                decimal stateOrderShare = EconomyConfigData.GetDecimalForCountry(countryId, "StateOrderTreasuryShare", 0.008m);
                decimal stateOrderMax = EconomyConfigData.GetDecimalForCountry(countryId, "StateOrderBudgetMax", 65_000m);
                decimal companySalary = EconomyConfigData.GetDecimalForCountry(countryId, "CompanyBaseSalary", 1.0m);
                decimal soldierSalary = EconomyConfigData.GetDecimalForCountry(countryId, "SoldierSalaryPerTurn", 0.65m);

                if (dailyNet < -targetAbsDailyNet)
                {
                    int taxStep = Math.Clamp((int)Math.Ceiling((double)(imbalance / 2_500m)), 1, 3);
                    int tunedTax = Math.Clamp(country.TaxLevel + taxStep, 12, 28);
                    if (tunedTax != country.TaxLevel)
                    {
                        country.TaxLevel = tunedTax;
                        changed = true;
                    }

                    decimal tunedStateOrderShare = ClampDecimalValue(stateOrderShare * (1m - pressure), 0.003m, 0.014m);
                    if (Math.Abs(tunedStateOrderShare - stateOrderShare) >= 0.0001m)
                    {
                        EconomyConfigData.UpsertCountryDecimal(countryId, "StateOrderTreasuryShare", tunedStateOrderShare);
                        changed = true;
                    }

                    decimal tunedStateOrderMax = ClampDecimalValue(stateOrderMax * (1m - pressure * 0.65m), 3_500m, 300_000m);
                    if (Math.Abs(tunedStateOrderMax - stateOrderMax) >= 0.0001m)
                    {
                        EconomyConfigData.UpsertCountryDecimal(countryId, "StateOrderBudgetMax", tunedStateOrderMax);
                        changed = true;
                    }

                    decimal tunedCompanySalary = ClampDecimalValue(companySalary * (1m - pressure * 0.40m), 0.70m, 1.60m);
                    if (Math.Abs(tunedCompanySalary - companySalary) >= 0.0001m)
                    {
                        EconomyConfigData.UpsertCountryDecimal(countryId, "CompanyBaseSalary", tunedCompanySalary);
                        EconomyConfigData.UpsertCountryDecimal(countryId, "BaseWage", ClampDecimalValue(tunedCompanySalary * 0.92m, 0.65m, 1.30m));
                        changed = true;
                    }

                    decimal tunedSoldierSalary = ClampDecimalValue(soldierSalary * (1m - pressure * 0.25m), 0.35m, 1.20m);
                    if (Math.Abs(tunedSoldierSalary - soldierSalary) >= 0.0001m)
                    {
                        EconomyConfigData.UpsertCountryDecimal(countryId, "SoldierSalaryPerTurn", tunedSoldierSalary);
                        changed = true;
                    }
                }
                else
                {
                    int taxStep = Math.Clamp((int)Math.Ceiling((double)(imbalance / 3_500m)), 1, 2);
                    int tunedTax = Math.Clamp(country.TaxLevel - taxStep, 10, 22);
                    if (tunedTax != country.TaxLevel)
                    {
                        country.TaxLevel = tunedTax;
                        changed = true;
                    }

                    decimal tunedStateOrderShare = ClampDecimalValue(stateOrderShare * (1m + pressure * 0.90m), 0.004m, 0.016m);
                    if (Math.Abs(tunedStateOrderShare - stateOrderShare) >= 0.0001m)
                    {
                        EconomyConfigData.UpsertCountryDecimal(countryId, "StateOrderTreasuryShare", tunedStateOrderShare);
                        changed = true;
                    }

                    decimal tunedStateOrderMax = ClampDecimalValue(stateOrderMax * (1m + pressure * 0.80m), 4_000m, 360_000m);
                    if (Math.Abs(tunedStateOrderMax - stateOrderMax) >= 0.0001m)
                    {
                        EconomyConfigData.UpsertCountryDecimal(countryId, "StateOrderBudgetMax", tunedStateOrderMax);
                        changed = true;
                    }

                    decimal tunedCompanySalary = ClampDecimalValue(companySalary * (1m + pressure * 0.18m), 0.70m, 1.70m);
                    if (Math.Abs(tunedCompanySalary - companySalary) >= 0.0001m)
                    {
                        EconomyConfigData.UpsertCountryDecimal(countryId, "CompanyBaseSalary", tunedCompanySalary);
                        EconomyConfigData.UpsertCountryDecimal(countryId, "BaseWage", ClampDecimalValue(tunedCompanySalary * 0.92m, 0.65m, 1.35m));
                        changed = true;
                    }

                    decimal tunedSoldierSalary = ClampDecimalValue(soldierSalary * (1m + pressure * 0.15m), 0.35m, 1.30m);
                    if (Math.Abs(tunedSoldierSalary - soldierSalary) >= 0.0001m)
                    {
                        EconomyConfigData.UpsertCountryDecimal(countryId, "SoldierSalaryPerTurn", tunedSoldierSalary);
                        changed = true;
                    }
                }

                if (!changed)
                    return;

                CountriesData.UpdateCountry(country);
            }
        }

        private static long SafeAddLong(long left, long right)
        {
            if (right > 0 && left > long.MaxValue - right)
                return long.MaxValue;
            if (right < 0 && left < long.MinValue - right)
                return long.MinValue;
            return left + right;
        }

        private static void EnsureQuickCountryResources()
        {
            var defaults = new[]
            {
                new EconomyResourceRecord { ResourceType = "Food", BasePrice = 1.00m, IsTradeable = true, DemandFactor = 1.00f },
                new EconomyResourceRecord { ResourceType = "Clothing", BasePrice = 1.30m, IsTradeable = true, DemandFactor = 0.90f },
                new EconomyResourceRecord { ResourceType = "Housing", BasePrice = 1.60m, IsTradeable = true, DemandFactor = 0.85f },
                new EconomyResourceRecord { ResourceType = "Tools", BasePrice = 1.80m, IsTradeable = true, DemandFactor = 0.80f },
                new EconomyResourceRecord { ResourceType = "Luxury", BasePrice = 2.40m, IsTradeable = true, DemandFactor = 0.55f },
                new EconomyResourceRecord { ResourceType = "Goods", BasePrice = 1.20m, IsTradeable = true, DemandFactor = 0.75f }
            };

            foreach (var resource in defaults)
            {
                try
                {
                    if (EconomyResourcesData.GetByType(resource.ResourceType) == null)
                        EconomyResourcesData.AddResource(resource);
                }
                catch
                {
                     
                }
            }
        }

        private static List<EconomyBuildingRecord> GenerateQuickCountryBuildings(int countryId, CountryRecord country, QuickCountryPreset preset)
        {
            long population = Math.Max(2_000L, country.Population);
            var snapshot = PopulationCalculator.ComputePopulation(population);
            long workforce = Math.Max(1L, snapshot.WorkingAge);

            double jobCoverage = preset switch
            {
                QuickCountryPreset.Poor => 0.68d,
                QuickCountryPreset.Economy => 0.84d,
                QuickCountryPreset.Prosperous => 0.90d,
                _ => 0.78d
            };
            decimal productionScale = preset switch
            {
                QuickCountryPreset.Poor => 0.86m,
                QuickCountryPreset.Economy => 1.08m,
                QuickCountryPreset.Prosperous => 1.18m,
                _ => 1.00m
            };
            decimal upkeepScale = preset switch
            {
                QuickCountryPreset.Poor => 0.92m,
                QuickCountryPreset.Economy => 1.00m,
                QuickCountryPreset.Prosperous => 1.10m,
                _ => 1.00m
            };

            int targetJobs = (int)Math.Clamp((long)Math.Round(workforce * jobCoverage), 180L, 2_000_000L);
            int productionJobs = (int)Math.Round(targetJobs * 0.60d, MidpointRounding.AwayFromZero);
            int servicesJobs = (int)Math.Round(targetJobs * 0.20d, MidpointRounding.AwayFromZero);
            int governanceJobs = (int)Math.Round(targetJobs * 0.10d, MidpointRounding.AwayFromZero);
            int scienceCultureJobs = (int)Math.Round(targetJobs * 0.06d, MidpointRounding.AwayFromZero);
            int militaryJobs = Math.Max(0, targetJobs - productionJobs - servicesJobs - governanceJobs - scienceCultureJobs);

            int agricultureJobs = (int)Math.Round(productionJobs * 0.40d, MidpointRounding.AwayFromZero);
            int industryJobs = (int)Math.Round(productionJobs * 0.45d, MidpointRounding.AwayFromZero);
            int logisticsJobs = Math.Max(0, productionJobs - agricultureJobs - industryJobs);

            int[] agri = SplitJobs(agricultureJobs, 0.45m, 0.35m, 0.20m);
            int[] industry = SplitJobs(industryJobs, 0.32m, 0.25m, 0.22m, 0.21m);
            int[] logistics = SplitJobs(logisticsJobs, 0.55m, 0.45m);
            int[] services = SplitJobs(servicesJobs, 0.42m, 0.30m, 0.28m);
            int[] gov = SplitJobs(governanceJobs, 0.62m, 0.38m);
            int[] sci = SplitJobs(scienceCultureJobs, 0.52m, 0.28m, 0.20m);
            int[] mil = SplitJobs(militaryJobs, 0.45m, 0.35m, 0.20m);

            var buildings = new List<EconomyBuildingRecord>
            {
                CreateQuickScaledBuilding(countryId, "Аграрний пояс", "Production", agri[0], "Food", false, 12.5m * productionScale, 0.011m * upkeepScale, stabilityBonus: 0.4f),
                CreateQuickScaledBuilding(countryId, "Фермерські господарства", "Production", agri[1], "Food", false, 11.5m * productionScale, 0.0115m * upkeepScale),
                CreateQuickScaledBuilding(countryId, "Млини й зерносховища", "Production", agri[2], "Food", true, 9.5m * productionScale, 0.0105m * upkeepScale, stabilityBonus: 0.8f),

                CreateQuickScaledBuilding(countryId, "Кузні та ливарні", "Production", industry[0], "Tools", false, 12.8m * productionScale, 0.015m * upkeepScale, armyBonus: 6),
                CreateQuickScaledBuilding(countryId, "Ткацькі цехи", "Production", industry[1], "Clothing", false, 11.4m * productionScale, 0.014m * upkeepScale),
                CreateQuickScaledBuilding(countryId, "Каменоломні й цегельні", "Production", industry[2], "Housing", false, 10.8m * productionScale, 0.013m * upkeepScale),
                CreateQuickScaledBuilding(countryId, "Міські мануфактури", "Production", industry[3], "Goods", false, 11.8m * productionScale, 0.0145m * upkeepScale),

                CreateQuickScaledBuilding(countryId, "Порт і склади", "Production", logistics[0], "Luxury", true, 9.8m * productionScale, 0.013m * upkeepScale, stabilityBonus: 0.5f),
                CreateQuickScaledBuilding(countryId, "Караванні двори", "Production", logistics[1], "Goods", false, 8.7m * productionScale, 0.012m * upkeepScale),

                CreateQuickScaledBuilding(countryId, "Торгові ринки", "Other", services[0], null, false, 0m, 0.009m * upkeepScale, stabilityBonus: 1.2f),
                CreateQuickScaledBuilding(countryId, "Крамниці й трактири", "Other", services[1], null, false, 0m, 0.008m * upkeepScale, stabilityBonus: 0.7f),
                CreateQuickScaledBuilding(countryId, "Житлові квартали", "Housing", services[2], null, true, 0m, 0.007m * upkeepScale, stabilityBonus: 0.9f),

                CreateQuickScaledBuilding(countryId, "Канцелярії та суди", "Administration", gov[0], null, true, 0m, 0.010m * upkeepScale, corruptionReduction: 2.3f),
                CreateQuickScaledBuilding(countryId, "Податкова служба", "Administration", gov[1], null, true, 0m, 0.009m * upkeepScale, corruptionReduction: 1.6f),

                CreateQuickScaledBuilding(countryId, "Школи та скрипторії", "Science", sci[0], null, true, 0m, 0.009m * upkeepScale, researchBonus: 1.4f),
                CreateQuickScaledBuilding(countryId, "Лазарети й лазні", "Science", sci[1], null, true, 0m, 0.009m * upkeepScale, stabilityBonus: 0.8f, researchBonus: 0.6f),
                CreateQuickScaledBuilding(countryId, "Храми й монастирі", "Culture", sci[2], null, true, 0m, 0.008m * upkeepScale, stabilityBonus: 1.5f, corruptionReduction: 0.4f),

                CreateQuickScaledBuilding(countryId, "Казармені округи", "Military", mil[0], null, true, 0m, 0.012m * upkeepScale, armyBonus: 14),
                CreateQuickScaledBuilding(countryId, "Арсенали й стрільбища", "Military", mil[1], null, true, 0m, 0.011m * upkeepScale, armyBonus: 12),
                CreateQuickScaledBuilding(countryId, "Міські укріплення", "Military", mil[2], null, true, 0m, 0.010m * upkeepScale, stabilityBonus: 0.6f, armyBonus: 10)
            };

            return buildings
                .Where(b => b.JobsProvided > 0 || b.BaseProduction > 0m || b.ArmyBonus > 0)
                .ToList();
        }

        private static EconomyBuildingRecord CreateQuickCountryBuilding(
            int countryId,
            string type,
            string category,
            int jobsProvided,
            decimal upkeepCost,
            decimal baseProduction,
            string resourceType,
            bool stateOwned,
            float stabilityBonus = 0f,
            float corruptionReduction = 0f,
            float researchBonus = 0f,
            int armyBonus = 0)
        {
            return new EconomyBuildingRecord
            {
                CountryId = countryId,
                Type = type,
                Category = category,
                Level = 1,
                JobsProvided = jobsProvided,
                UpkeepCost = upkeepCost,
                BaseProduction = baseProduction,
                BuildCost = Math.Max(1m, baseProduction * 1.4m + jobsProvided * 0.7m),
                BuildTurns = 0,
                ResourceType = resourceType,
                StateOwned = stateOwned,
                StabilityBonus = stabilityBonus,
                CorruptionReduction = corruptionReduction,
                ResearchBonus = researchBonus,
                ArmyBonus = armyBonus,
                BuildingStatus = "Active",
                InactiveTurns = 0,
                DegradationLevel = 0
            };
        }

        private static EconomyBuildingRecord CreateQuickScaledBuilding(
            int countryId,
            string type,
            string category,
            int jobsProvided,
            string? resourceType,
            bool stateOwned,
            decimal productionPerJob,
            decimal upkeepPerJob,
            float stabilityBonus = 0f,
            float corruptionReduction = 0f,
            float researchBonus = 0f,
            int armyBonus = 0)
        {
            int jobs = Math.Max(0, jobsProvided);
            decimal baseProduction = (string.Equals(category, "Production", StringComparison.OrdinalIgnoreCase) && jobs > 0)
                ? decimal.Round(jobs * Math.Max(0m, productionPerJob), 2, MidpointRounding.AwayFromZero)
                : 0m;

            decimal upkeep = decimal.Round(Math.Max(1m, jobs * Math.Max(0.001m, upkeepPerJob)), 2, MidpointRounding.AwayFromZero);

            return CreateQuickCountryBuilding(
                countryId,
                type,
                category,
                jobs,
                upkeep,
                baseProduction,
                resourceType ?? string.Empty,
                stateOwned,
                stabilityBonus,
                corruptionReduction,
                researchBonus,
                armyBonus
            );
        }

        private static List<EconomyCompanyRecord> GenerateQuickCountryCompanies(int countryId, CountryRecord country, IReadOnlyCollection<EconomyBuildingRecord> seededBuildings, QuickCountryPreset preset)
        {
            long buildingJobs = seededBuildings?.Sum(b => (long)Math.Max(0, b.JobsProvided)) ?? 0L;
            decimal companyJobsRatio = preset switch
            {
                QuickCountryPreset.Poor => 0.85m,
                QuickCountryPreset.Economy => 0.97m,
                QuickCountryPreset.Prosperous => 1.02m,
                _ => 0.92m
            };
            int companyJobsTarget = (int)Math.Clamp((long)Math.Round((double)Math.Max(120L, buildingJobs) * (double)companyJobsRatio), 120L, 2_000_000L);

            decimal presetSalaryMult = preset switch
            {
                QuickCountryPreset.Poor => 0.92m,
                QuickCountryPreset.Economy => 1.05m,
                QuickCountryPreset.Prosperous => 1.12m,
                _ => 1.00m
            };
            decimal budgetScale = preset switch
            {
                QuickCountryPreset.Poor => 0.72m,
                QuickCountryPreset.Economy => 1.20m,
                QuickCountryPreset.Prosperous => 1.45m,
                _ => 1.00m
            };
            int reserveDaysDelta = preset switch
            {
                QuickCountryPreset.Poor => -18,
                QuickCountryPreset.Economy => 16,
                QuickCountryPreset.Prosperous => 28,
                _ => 0
            };

            int TuneReserveDays(int baseDays) => Math.Max(45, baseDays + reserveDaysDelta);
            decimal TuneBudgetMultiplier(decimal baseMult) => ClampDecimalValue(baseMult * budgetScale, 0.65m, 2.80m);

            decimal salaryBase = ClampDecimalValue(0.82m + (decimal)Math.Log10(Math.Max(10d, country.Population)) * 0.07m, 0.85m, 1.45m) * presetSalaryMult;
            string countryTag = string.IsNullOrWhiteSpace(country.Name) ? $"#{countryId}" : country.Name;
            int[] jobs = SplitJobs(companyJobsTarget, 0.20m, 0.14m, 0.18m, 0.12m, 0.11m, 0.10m, 0.09m, 0.06m);

            var companies = new List<EconomyCompanyRecord>
            {
                new EconomyCompanyRecord
                {
                    CountryId = countryId,
                    Name = $"Гільдія рільників {countryTag}",
                    ProductionType = "Food",
                    JobsOffered = jobs[0],
                    BaseSalary = decimal.Round(salaryBase * 0.96m, 4, MidpointRounding.AwayFromZero),
                    TaxRate = 0.11m,
                    Budget = BuildCompanySeedBudget(jobs[0], salaryBase * 0.96m, TuneReserveDays(85), TuneBudgetMultiplier(1.05m)),
                    IsStateCompany = false,
                    MilitaryProductionShare = 0f,
                    IsActive = true
                },
                new EconomyCompanyRecord
                {
                    CountryId = countryId,
                    Name = $"Зерновий торговий союз {countryTag}",
                    ProductionType = "Food",
                    JobsOffered = jobs[1],
                    BaseSalary = decimal.Round(salaryBase * 0.98m, 4, MidpointRounding.AwayFromZero),
                    TaxRate = 0.10m,
                    Budget = BuildCompanySeedBudget(jobs[1], salaryBase * 0.98m, TuneReserveDays(78), TuneBudgetMultiplier(1.00m)),
                    IsStateCompany = false,
                    MilitaryProductionShare = 0f,
                    IsActive = true
                },
                new EconomyCompanyRecord
                {
                    CountryId = countryId,
                    Name = $"Ремісничий консорціум {countryTag}",
                    ProductionType = "Industry",
                    JobsOffered = jobs[2],
                    BaseSalary = decimal.Round(salaryBase * 1.08m, 4, MidpointRounding.AwayFromZero),
                    TaxRate = 0.14m,
                    Budget = BuildCompanySeedBudget(jobs[2], salaryBase * 1.08m, TuneReserveDays(92), TuneBudgetMultiplier(1.18m)),
                    IsStateCompany = false,
                    MilitaryProductionShare = 12f,
                    IsActive = true
                },
                new EconomyCompanyRecord
                {
                    CountryId = countryId,
                    Name = $"Будівнича гільдія {countryTag}",
                    ProductionType = "Industry",
                    JobsOffered = jobs[3],
                    BaseSalary = decimal.Round(salaryBase * 1.04m, 4, MidpointRounding.AwayFromZero),
                    TaxRate = 0.13m,
                    Budget = BuildCompanySeedBudget(jobs[3], salaryBase * 1.04m, TuneReserveDays(82), TuneBudgetMultiplier(1.10m)),
                    IsStateCompany = false,
                    MilitaryProductionShare = 8f,
                    IsActive = true
                },
                new EconomyCompanyRecord
                {
                    CountryId = countryId,
                    Name = $"Караванний союз {countryTag}",
                    ProductionType = "Logistics",
                    JobsOffered = jobs[4],
                    BaseSalary = decimal.Round(salaryBase * 1.02m, 4, MidpointRounding.AwayFromZero),
                    TaxRate = 0.12m,
                    Budget = BuildCompanySeedBudget(jobs[4], salaryBase * 1.02m, TuneReserveDays(86), TuneBudgetMultiplier(1.05m)),
                    IsStateCompany = false,
                    MilitaryProductionShare = 15f,
                    IsActive = true
                },
                new EconomyCompanyRecord
                {
                    CountryId = countryId,
                    Name = $"Міські служби {countryTag}",
                    ProductionType = "Services",
                    JobsOffered = jobs[5],
                    BaseSalary = decimal.Round(salaryBase * 0.94m, 4, MidpointRounding.AwayFromZero),
                    TaxRate = 0.09m,
                    Budget = BuildCompanySeedBudget(jobs[5], salaryBase * 0.94m, TuneReserveDays(72), TuneBudgetMultiplier(0.95m)),
                    IsStateCompany = false,
                    MilitaryProductionShare = 0f,
                    IsActive = true
                },
                new EconomyCompanyRecord
                {
                    CountryId = countryId,
                    Name = $"Королівський арсенал {countryTag}",
                    ProductionType = "Weapons",
                    JobsOffered = jobs[6],
                    BaseSalary = decimal.Round(salaryBase * 1.12m, 4, MidpointRounding.AwayFromZero),
                    TaxRate = 0.08m,
                    Budget = BuildCompanySeedBudget(jobs[6], salaryBase * 1.12m, TuneReserveDays(110), TuneBudgetMultiplier(1.30m)),
                    IsStateCompany = true,
                    MilitaryProductionShare = 82f,
                    IsActive = true
                },
                new EconomyCompanyRecord
                {
                    CountryId = countryId,
                    Name = $"Державна портова служба {countryTag}",
                    ProductionType = "Logistics",
                    JobsOffered = jobs[7],
                    BaseSalary = decimal.Round(salaryBase * 1.00m, 4, MidpointRounding.AwayFromZero),
                    TaxRate = 0.07m,
                    Budget = BuildCompanySeedBudget(jobs[7], salaryBase * 1.00m, TuneReserveDays(95), TuneBudgetMultiplier(1.20m)),
                    IsStateCompany = true,
                    MilitaryProductionShare = 32f,
                    IsActive = true
                }
            };

            return companies;
        }

        private static decimal BuildCompanySeedBudget(int jobs, decimal salary, int reserveDays, decimal multiplier)
        {
            decimal headcount = Math.Max(1m, jobs);
            decimal pay = Math.Max(0.01m, salary);
            decimal days = Math.Max(30m, reserveDays);
            decimal mult = Math.Max(0.5m, multiplier);
            decimal budget = headcount * pay * days * mult;
            return decimal.Round(ClampDecimalValue(budget, 2_500m, 900_000_000m), 2, MidpointRounding.AwayFromZero);
        }

        private static int[] SplitJobs(int total, params decimal[] weights)
        {
            if (weights == null || weights.Length == 0)
                return Array.Empty<int>();

            var result = new int[weights.Length];
            if (total <= 0)
                return result;

            decimal normalizedSum = weights.Where(w => w > 0m).Sum();
            if (normalizedSum <= 0m)
            {
                result[^1] = total;
                return result;
            }

            int assigned = 0;
            for (int i = 0; i < weights.Length; i++)
            {
                if (i == weights.Length - 1)
                {
                    result[i] = Math.Max(0, total - assigned);
                    break;
                }

                decimal ratio = Math.Max(0m, weights[i]) / normalizedSum;
                int slice = (int)Math.Round(total * (double)ratio, MidpointRounding.AwayFromZero);
                int allowed = Math.Max(0, total - assigned);
                if (slice > allowed)
                    slice = allowed;

                result[i] = Math.Max(0, slice);
                assigned += result[i];
            }

            return result;
        }

        private static decimal ClampDecimalValue(decimal value, decimal min, decimal max)
        {
            if (value < min)
                return min;
            if (value > max)
                return max;
            return value;
        }

        private static string ResolveCompanyTypeForBuilding(EconomyBuildingRecord building)
        {
            if (string.Equals(building.Category, "Military", StringComparison.OrdinalIgnoreCase))
                return "Weapons";

            if (string.Equals(building.Category, "Production", StringComparison.OrdinalIgnoreCase))
            {
                if (string.Equals(building.ResourceType, "Food", StringComparison.OrdinalIgnoreCase))
                    return "Food";
                if (string.Equals(building.ResourceType, "Luxury", StringComparison.OrdinalIgnoreCase))
                    return "Logistics";
                return "Industry";
            }

            return "Services";
        }

        private (int Completed, string ActiveResearchName) SeedQuickCountryResearch(int countryId, CountryRecord seedCountry)
        {
            var service = new ResearchService();
            var available = ResearchDefinitions.GetAll()
                .Where(d => d.IsActive && d.Id > 0 && (d.Requirements == null || d.Requirements.Count == 0))
                .OrderBy(d => d.MoneyCost)
                .ThenBy(d => d.ScienceCost)
                .ThenBy(d => d.Id)
                .ToList();

            if (available.Count == 0)
                return (0, string.Empty);

            int completed = 0;
            int targetCompleted = Math.Clamp((int)(Math.Max(1L, seedCountry.Population) / 300_000L) + 1, 1, 3);
            long initialTreasury = CountriesData.GetCountryById(countryId)?.Treasury ?? seedCountry.Treasury;
            long maxSpend = Math.Max(0L, (long)Math.Ceiling(initialTreasury * 0.25d));
            long spent = 0L;

            foreach (var def in available)
            {
                if (completed >= targetCompleted)
                    break;

                var before = CountriesData.GetCountryById(countryId);
                if (before == null)
                    break;

                if (maxSpend > 0 && spent >= maxSpend)
                    break;

                if (!service.CanStartResearch(countryId, def.Id, out _))
                    continue;

                string startMsg = service.StartResearch(countryId, def.Id);
                if (!startMsg.StartsWith("📘", StringComparison.OrdinalIgnoreCase))
                    continue;

                bool isCompleted = false;
                for (int guard = 0; guard < 40; guard++)
                {
                    string tick = service.UpdateResearch(countryId);
                    if (tick.StartsWith("✅", StringComparison.OrdinalIgnoreCase))
                    {
                        isCompleted = true;
                        break;
                    }

                    if (tick.StartsWith("Немає активного", StringComparison.OrdinalIgnoreCase))
                        break;
                }

                if (!isCompleted)
                    continue;

                completed++;
                var after = CountriesData.GetCountryById(countryId);
                if (after != null && before.Treasury > after.Treasury)
                    spent += (before.Treasury - after.Treasury);
            }

            string activeResearchName = string.Empty;
            foreach (var def in available)
            {
                if (!service.CanStartResearch(countryId, def.Id, out _))
                    continue;

                string startMsg = service.StartResearch(countryId, def.Id);
                if (startMsg.StartsWith("📘", StringComparison.OrdinalIgnoreCase))
                {
                    activeResearchName = def.Name;
                    break;
                }
            }

            return (completed, activeResearchName);
        }

        private static bool TryExtractAddedCountryId(string result, out int countryId)
        {
            countryId = 0;
            if (string.IsNullOrWhiteSpace(result))
                return false;

            var parts = result.Split(new[] { ' ', '=', ':', '#' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = parts.Length - 1; i >= 0; i--)
            {
                if (int.TryParse(parts[i], NumberStyles.Integer, CultureInfo.InvariantCulture, out countryId))
                    return true;
            }

            countryId = 0;
            return false;
        }

        private async Task ShowCountriesList(ITelegramBotClient botClient, long userId, CancellationToken cancellationToken)
        {
            var countries = CountriesData.GetAllCountries();
            if (countries.Count == 0)
            {
                await botClient.SendTextMessageAsync(
                    userId,
                    "Список країн порожній.",
                    replyMarkup: StateKeyboardBuilder.AdminBackKeyboard(),
                    cancellationToken: cancellationToken
                );
                return;
            }

            var lines = new List<string>
            {
                "📜 Список країн:"
            };

            foreach (var c in countries)
            {
                string flag = string.IsNullOrWhiteSpace(c.FlagEmoji) ? "" : $"{c.FlagEmoji} ";
                lines.Add($"#{c.CountryId} {flag}{c.Name} — {c.FormOfGovernment} (створено {c.CreatedAt:dd.MM.yyyy})");
            }

            await botClient.SendTextMessageAsync(
                userId,
                string.Join("\n", lines),
                replyMarkup: StateKeyboardBuilder.AdminBackKeyboard(),
                cancellationToken: cancellationToken
            );
        }

        private async Task ShowDeleteCountryList(ITelegramBotClient botClient, long userId, int msgId, CancellationToken cancellationToken)
        {
            var countries = CountriesData.GetAllCountries();
            if (countries.Count == 0)
            {
                await SafeEditMessageTextAsync(
                botClient: botClient,
                chatId: userId,
                    messageId: msgId,
                    text: "Список країн порожній.",
                    replyMarkup: StateKeyboardBuilder.AdminDeleteCountryKeyboard(countries),
                    cancellationToken: cancellationToken
                );
                return;
            }

            await SafeEditMessageTextAsync(
                botClient: botClient,
                chatId: userId,
                messageId: msgId,
                text: "🗑 Оберіть країну для видалення:",
                replyMarkup: StateKeyboardBuilder.AdminDeleteCountryKeyboard(countries),
                cancellationToken: cancellationToken
            );
        }

        private async Task ShowMergeTargetList(ITelegramBotClient botClient, long userId, int msgId, CancellationToken cancellationToken)
        {
            var countries = CountriesData.GetAllCountries();
            if (countries.Count < 2)
            {
                await SafeEditMessageTextAsync(
                botClient: botClient,
                chatId: userId,
                    messageId: msgId,
                    text: "Недостатньо країн для об'єднання.",
                    replyMarkup: StateKeyboardBuilder.AddOrApplyCountryKeyboard(),
                    cancellationToken: cancellationToken
                );
                return;
            }

            await SafeEditMessageTextAsync(
                botClient: botClient,
                chatId: userId,
                messageId: msgId,
                text: "🎯 Оберіть країну, яка залишиться після об'єднання:",
                replyMarkup: StateKeyboardBuilder.AdminMergeTargetKeyboard(countries),
                cancellationToken: cancellationToken
            );
        }

        private async Task ShowMergeSourceList(ITelegramBotClient botClient, long userId, int msgId, CancellationToken cancellationToken, int targetId)
        {
            var countries = CountriesData.GetAllCountries();
            if (countries.Count < 2)
            {
                await SafeEditMessageTextAsync(
                botClient: botClient,
                chatId: userId,
                    messageId: msgId,
                    text: "Недостатньо країн для об'єднання.",
                    replyMarkup: StateKeyboardBuilder.AddOrApplyCountryKeyboard(),
                    cancellationToken: cancellationToken
                );
                return;
            }

            var target = CountriesData.GetCountryById(targetId);
            string targetName = target == null ? $"#{targetId}" : (string.IsNullOrWhiteSpace(target.Name) ? $"#{targetId}" : target.Name);

            await SafeEditMessageTextAsync(
                botClient: botClient,
                chatId: userId,
                messageId: msgId,
                text: $"➡ Оберіть країну, яку приєднати до {targetName}:",
                replyMarkup: StateKeyboardBuilder.AdminMergeSourceKeyboard(countries, targetId),
                cancellationToken: cancellationToken
            );
        }

        private async Task ShowMergeConfirm(ITelegramBotClient botClient, long userId, int msgId, CancellationToken cancellationToken, int targetId, int sourceId)
        {
            var target = CountriesData.GetCountryById(targetId);
            var source = CountriesData.GetCountryById(sourceId);
            string targetName = target == null ? $"#{targetId}" : (string.IsNullOrWhiteSpace(target.Name) ? $"#{targetId}" : target.Name);
            string sourceName = source == null ? $"#{sourceId}" : (string.IsNullOrWhiteSpace(source.Name) ? $"#{sourceId}" : source.Name);

            await SafeEditMessageTextAsync(
                botClient: botClient,
                chatId: userId,
                messageId: msgId,
                text: $"⚠ Об'єднати {sourceName} ➜ {targetName}?\nЦе видалить країну {sourceName}.",
                replyMarkup: StateKeyboardBuilder.AdminMergeConfirmKeyboard(sourceId),
                cancellationToken: cancellationToken
            );
        }

        private async Task ShowDeleteAllConfirm(ITelegramBotClient botClient, long userId, int msgId, CancellationToken cancellationToken, int step = 1)
        {
            string text = step switch
            {
                2 => "⚠ Повторне підтвердження. Справді видалити всі країни?",
                3 => "🔥 Останнє підтвердження. Це видалить ВСІ країни та їх дані.",
                _ => "⚠ Ви впевнені, що хочете видалити всі країни?"
            };
            await SafeEditMessageTextAsync(
                botClient: botClient,
                chatId: userId,
                messageId: msgId,
                text: text,
                replyMarkup: StateKeyboardBuilder.AdminDeleteAllConfirmKeyboard(step),
                cancellationToken: cancellationToken
            );
        }

        private async Task ShowDeleteCountryConfirm(ITelegramBotClient botClient, long userId, int msgId, int countryId, CancellationToken cancellationToken, int step = 1)
        {
            var country = CountriesData.GetCountryById(countryId);
            string name = country == null ? $"#{countryId}" : (string.IsNullOrWhiteSpace(country.Name) ? $"#{countryId}" : country.Name);
            string text = step switch
            {
                2 => $"⚠ Повторне підтвердження. Видалити країну {name}?",
                3 => $"🔥 Останнє підтвердження. Це видалить країну {name} та її дані.",
                _ => $"⚠ Видалити країну {name}?"
            };

            await SafeEditMessageTextAsync(
                botClient: botClient,
                chatId: userId,
                messageId: msgId,
                text: text,
                replyMarkup: StateKeyboardBuilder.AdminDeleteCountryConfirmKeyboard(countryId, step),
                cancellationToken: cancellationToken
            );
        }

        private async Task ShowAssignCharacterList(ITelegramBotClient botClient, long userId, int msgId, CancellationToken cancellationToken)
        {
            var characters = UserData.GetAllCharactersForAdmin();
            if (characters.Count == 0)
            {
                await SafeEditMessageTextAsync(
                botClient: botClient,
                chatId: userId,
                    messageId: msgId,
                    text: "Список персонажів порожній.",
                    replyMarkup: StateKeyboardBuilder.AdminAssignCharacterKeyboard(null),
                    cancellationToken: cancellationToken
                );
                return;
            }

            var items = new List<(int CharacterId, string Title)>();
            foreach (var c in characters)
            {
                string activeMark = c.IsActive ? " (актив.)" : "";
                string namePart = string.IsNullOrWhiteSpace(c.Name) ? "" : c.Name;
                string title = string.IsNullOrWhiteSpace(namePart)
                    ? $"Персонаж{activeMark}"
                    : $"{namePart}{activeMark}";
                items.Add((c.CharacterId, title));
            }

            await SafeEditMessageTextAsync(
                botClient: botClient,
                chatId: userId,
                messageId: msgId,
                text: "👤 Оберіть персонажа:",
                replyMarkup: StateKeyboardBuilder.AdminAssignCharacterKeyboard(items),
                cancellationToken: cancellationToken
            );
        }

        private async Task ShowAssignCountryList(ITelegramBotClient botClient, long userId, int msgId, CancellationToken cancellationToken)
        {
            var countries = CountriesData.GetAllCountries();
            if (countries.Count == 0)
            {
                await SafeEditMessageTextAsync(
                botClient: botClient,
                chatId: userId,
                    messageId: msgId,
                    text: "Список країн порожній.",
                    replyMarkup: StateKeyboardBuilder.AdminAssignCountryKeyboard(countries),
                    cancellationToken: cancellationToken
                );
                return;
            }

            await SafeEditMessageTextAsync(
                botClient: botClient,
                chatId: userId,
                messageId: msgId,
                text: "🌍 Оберіть країну для призначення:",
                replyMarkup: StateKeyboardBuilder.AdminAssignCountryKeyboard(countries),
                cancellationToken: cancellationToken
            );
        }

        private async Task DeleteCountryByCallback(ITelegramBotClient botClient, CallbackQuery callbackQuery, CancellationToken cancellationToken)
        {
            var parts = (callbackQuery.Data ?? string.Empty).Split('_');
            if (parts.Length < 3 || !int.TryParse(parts[2], out int countryId))
                return;

            if (callbackQuery.Message == null)
                return;

            await DeleteCountryById(botClient, callbackQuery.From.Id, callbackQuery.Message.MessageId, countryId, cancellationToken);
        }

        private async Task DeleteCountryById(ITelegramBotClient botClient, long userId, int msgId, int countryId, CancellationToken cancellationToken)
        {
            var result = CountriesData.DeleteCountry(countryId);

            await SafeEditMessageTextAsync(
                botClient: botClient,
                chatId: userId,
                messageId: msgId,
                text: result,
                replyMarkup: StateKeyboardBuilder.AddOrApplyCountryKeyboard(),
                cancellationToken: cancellationToken
            );
        }

        private async Task DeleteAllCountries(ITelegramBotClient botClient, long userId, int msgId, CancellationToken cancellationToken)
        {
            var countries = CountriesData.GetAllCountries();
            int deleted = 0;
            foreach (var c in countries)
            {
                var result = CountriesData.DeleteCountry(c.CountryId);
                if (result.StartsWith("Країна видалена", StringComparison.OrdinalIgnoreCase))
                    deleted++;
            }

            await SafeEditMessageTextAsync(
                botClient: botClient,
                chatId: userId,
                messageId: msgId,
                text: $"Видалено країн: {deleted}.",
                replyMarkup: StateKeyboardBuilder.AddOrApplyCountryKeyboard(),
                cancellationToken: cancellationToken
            );
        }

        private async Task ShowAdminPanel(ITelegramBotClient botClient, long userId, int msgId, CancellationToken cancellationToken)
        {
            var text = "🔧 Адміністративна панель (для розробника)";
            var keyboard = StateKeyboardBuilder.AddOrApplyCountryKeyboard();

            await SafeEditMessageTextAsync(
                botClient: botClient,
                chatId: userId,
                messageId: msgId,
                text: text,
                replyMarkup: keyboard,
                cancellationToken: cancellationToken);
        }

        private async Task ShowQuickCountryPresetMenu(ITelegramBotClient botClient, long userId, int msgId, CancellationToken cancellationToken)
        {
            string text =
@"⚡ Швидке створення країни
Оберіть пресет старту:";

            await SafeEditMessageTextAsync(
                botClient: botClient,
                chatId: userId,
                messageId: msgId,
                text: text,
                replyMarkup: StateKeyboardBuilder.QuickCountryPresetKeyboard(),
                cancellationToken: cancellationToken);
        }

        private static QuickCountryPreset ParseQuickCountryPreset(string? raw)
        {
            string key = (raw ?? string.Empty).Trim().ToLowerInvariant();
            return key switch
            {
                "poor" => QuickCountryPreset.Poor,
                "economy" => QuickCountryPreset.Economy,
                "prosperous" => QuickCountryPreset.Prosperous,
                _ => QuickCountryPreset.Standard
            };
        }

        private static string FormatQuickCountryPresetName(QuickCountryPreset preset)
        {
            return preset switch
            {
                QuickCountryPreset.Poor => "Скромний",
                QuickCountryPreset.Economy => "Економічний",
                QuickCountryPreset.Prosperous => "Заможний",
                _ => "Збалансований"
            };
        }

        private static bool TryParseId(string data, int index, out int value)
        {
            value = 0;
            var parts = (data ?? string.Empty).Split('_');
            if (parts.Length <= index)
                return false;

            return int.TryParse(parts[index], out value);
        }

        private static bool TryAssignField(CountryRecord country, string fieldName, FieldKind kind, string input, out string error)
        {
            error = "";
            var prop = typeof(CountryRecord).GetProperty(fieldName);
            if (prop == null)
            {
                error = "❗ Невідоме поле.";
                return false;
            }

            switch (kind)
            {
                case FieldKind.String:
                    prop.SetValue(country, input.Trim());
                    if (fieldName == "FormOfGovernment")
                    {
                        country.HasParliament = ShouldHaveParliament(input);
                        if (!country.HasParliament)
                        {
                            country.ParliamentSeats = 0;
                            country.ParliamentFractions = 0;
                            country.ParliamentSupport = 0;
                            country.OppositionStrength = 0;
                        }
                    }
                    return true;
                case FieldKind.Int:
                    string normalizedInt = NormalizeIntegerInput(input);
                    if (!int.TryParse(normalizedInt, NumberStyles.Integer, CultureInfo.InvariantCulture, out var i))
                    {
                        error = "❗ Очікується ціле число.";
                        return false;
                    }
                    prop.SetValue(country, i);
                    return true;
                case FieldKind.Long:
                    string normalizedLong = NormalizeIntegerInput(input);
                    if (!long.TryParse(normalizedLong, NumberStyles.Integer, CultureInfo.InvariantCulture, out var l))
                    {
                        error = "❗ Очікується ціле число (long).";
                        return false;
                    }
                    prop.SetValue(country, l);
                    return true;
                case FieldKind.Float:
                    if (!float.TryParse(input.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out var f))
                    {
                        error = "❗ Очікується число з десятковою крапкою.";
                        return false;
                    }
                    prop.SetValue(country, f);
                    return true;
                case FieldKind.Decimal:
                    if (!decimal.TryParse(input.Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture, out var d))
                    {
                        error = "❗ Очікується число з десятковою крапкою.";
                        return false;
                    }
                    prop.SetValue(country, d);
                    return true;
                case FieldKind.Bool:
                    if (TryParseBool(input, out var b))
                    {
                        prop.SetValue(country, b);
                        return true;
                    }
                    error = "❗ Очікується так/ні або 1/0.";
                    return false;
                case FieldKind.DateTime:
                    if (string.IsNullOrWhiteSpace(input))
                    {
                        prop.SetValue(country, null);
                        return true;
                    }
                    if (DateTime.TryParse(input, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var dt))
                    {
                        prop.SetValue(country, dt);
                        return true;
                    }
                    error = "❗ Невірний формат дати. Спробуй: 2026-02-07 18:30";
                    return false;
                default:
                    error = "❗ Невідомий тип поля.";
                    return false;
            }
        }

        private static bool TryParseBool(string input, out bool value)
        {
            var s = (input ?? string.Empty).Trim().ToLowerInvariant();
            if (s == "1" || s == "true" || s == "так" || s == "yes")
            {
                value = true;
                return true;
            }
            if (s == "0" || s == "false" || s == "ні" || s == "no")
            {
                value = false;
                return true;
            }
            value = false;
            return false;
        }

        private static string NormalizeIntegerInput(string input)
        {
            return (input ?? string.Empty)
                .Trim()
                .Replace(" ", string.Empty)
                .Replace("\u00A0", string.Empty)
                .Replace("_", string.Empty)
                .Replace("'", string.Empty);
        }

        private static CountryRecord CreateDefaultCountry()
        {
            return new CountryRecord
            {
                Name = string.Empty,
                FlagEmoji = null,
                FormOfGovernment = string.Empty,
                GovernmentLevel = 0,
                GovernmentXP = 0,
                Stability = 50,
                PopularLoyalty = 50,
                EliteLoyalty = 50,
                Corruption = 15,
                Legitimacy = 50,
                Control = 50,
                Inflation = 15,
                Treasury = 0,
                TaxLevel = 10,
                IncomePerTurn = 0,
                ExpensesPerTurn = 0,
                CorruptionLosses = 0,
                WorkRate = 60,
                AvailableWorkers = 0L,
                UnemploymentRate = 0,
                WarState = false,
                SanctionsLevel = 0,
                Population = 0L,
                PopulationGrowth = 1.2f,
                Happiness = 50,
                Unrest = 15,
                Refugees = 0L,
                NobilityInfluence = 50,
                MerchantsInfluence = 50,
                ClergyInfluence = 50,
                MilitaryInfluence = 50,
                MagesInfluence = 50,
                PeasantsInfluence = 50,
                ExternalInfluence = 50,
                HasParliament = false,
                ParliamentSeats = 0,
                ParliamentFractions = 0,
                ParliamentSupport = 50,
                OppositionStrength = 50,
                ActiveLawsCount = 0,
                MaxLawsAllowed = 0,
                EmergencyLawActive = false,
                ArmySize = 0,
                ArmyLoyalty = 50,
                ArmyMorale = 50,
                MilitaryUpkeep = 0,
                MartialLaw = false,
                MagicInfluence = 50,
                ManaStability = 50,
                ActiveManaZones = 0,
                ArcaneRisk = 15,
                Cities = 0,
                Villages = 0,
                InfrastructureLevel = 50,
                LogisticsEfficiency = 50,
                ResearchLevel = 0,
                ResearchSpeed = 50,
                CompletedResearches = 0,
                CoupRisk = 15,
                RebellionRisk = 15,
                AssassinationRisk = 15,
                Allies = 0,
                Enemies = 0,
                InternationalReputation = 50,
                IntelLevel = 0,
                CounterIntelLevel = 0,
                LastUpdate = null,
                TurnsSurvived = 0,
                IsInCrisis = false,
                IsInGoldenAge = false,
                IsCollapsed = false
            };
        }

        private static bool ShouldHaveParliament(string formOfGovernment)
        {
            if (string.IsNullOrWhiteSpace(formOfGovernment))
                return false;

            var f = formOfGovernment.Trim().ToLowerInvariant();
            return f.Contains("респ") || f.Contains("парламент") || f.Contains("демок");
        }

        private async Task SafeEditMessageTextAsync(
            ITelegramBotClient botClient,
            long chatId,
            int messageId,
            string text,
            InlineKeyboardMarkup replyMarkup,
            CancellationToken cancellationToken)
        {
            try
            {
                await botClient.EditMessageTextAsync(
                    chatId: chatId,
                    messageId: messageId,
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

                if (msg.Contains("message to edit not found", StringComparison.OrdinalIgnoreCase) ||
                    msg.Contains("message can't be edited", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        await botClient.SendTextMessageAsync(
                            chatId: chatId,
                            text: text,
                            replyMarkup: replyMarkup,
                            cancellationToken: cancellationToken);
                        return;
                    }
                    catch (Exception sendEx)
                    {
                        _logger.LogError(sendEx, "Fallback send failed in StateCreateOrHandleCountryCreation. ChatId={ChatId}", chatId);
                        throw;
                    }
                }

                _logger.LogError(ex,
                    "SafeEditMessageTextAsync failed in StateCreateOrHandleCountryCreation. ChatId={ChatId} MessageId={MessageId} ErrorCode={ErrorCode} Error={Error}",
                    chatId,
                    messageId,
                    ex.ErrorCode,
                    ex.Message);
                throw;
            }
        }

        private async Task SafeAnswerCallback(ITelegramBotClient botClient, CallbackQuery callbackQuery, string text, CancellationToken cancellationToken)
        {
            try
            {
                await botClient.AnswerCallbackQueryAsync(callbackQuery.Id, text, cancellationToken: cancellationToken);
            }
            catch (ApiRequestException ex) when ((ex.Message ?? string.Empty).Contains("query is too old", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogDebug(
                    "Ignoring outdated country callback answer. CallbackId={CallbackId}, UserId={UserId}, Data={CallbackData}",
                    callbackQuery.Id,
                    callbackQuery.From?.Id,
                    callbackQuery.Data);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Failed to answer country callback. CallbackId={CallbackId}, UserId={UserId}, Data={CallbackData}",
                    callbackQuery.Id,
                    callbackQuery.From?.Id,
                    callbackQuery.Data);
            }
        }
    }
}





