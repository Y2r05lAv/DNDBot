using DNDBot.BotSystem;
using Microsoft.Extensions.Logging;
using DNDBot;
#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Globalization;
using System.Data;
using DNDBot.DataBase;
using DNDBot.Goverment.Research;
using DNDBot.Goverment.Economy;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace DNDBot.Goverment
{
    public partial class StateAdministration : BotBodyRealisation
    {
        public readonly ILogger<StateAdministration> _logger;
        private readonly StateParliamentHandle _stateParliamentHandle;
        internal static readonly Dictionary<long, int> SelectedCountryByUser = new();
        private static readonly Dictionary<long, string> LastStateViewByUser = new();
        private static readonly Dictionary<long, EconomyInputMode> EconomyInputState = new();
        private static readonly Dictionary<long, int> EconomyInputStep = new();
        private static readonly Dictionary<long, EconomyBuildingRecord> EconomyBuildingBuffer = new();
        private static readonly Dictionary<long, BuildingCitySelectionContext> EconomyBuildingCitySelectionContext = new();
        private static readonly Dictionary<long, List<string>> EconomyBuildingCityChoices = new();
        private static readonly Dictionary<long, EconomyCompanyRecord> EconomyCompanyBuffer = new();
        private static readonly Dictionary<long, EconomyFestivalRecord> EconomyFestivalBuffer = new();
        private static readonly Dictionary<long, EconomyTradeAgreementRecord> EconomyTradeBuffer = new();
        private static readonly Dictionary<long, MilitaryUnitRecord> EconomyMilitaryUnitBuffer = new();
        private static readonly Dictionary<long, VassalMilitaryObligationRecord> EconomyVassalBuffer = new();
        private static readonly Dictionary<long, EconomyEditContext> EconomyEditState = new();
        private static readonly Dictionary<long, int> EconomyTemplateQuantity = new();
        private static readonly Dictionary<long, List<int>> EconomyHelperMessages = new();
        private static readonly Dictionary<long, List<int>> EconomyUserMessages = new();
        private static readonly Dictionary<long, long> EconomyMoneyAmountBuffer = new();
        private static readonly Dictionary<long, string> EconomyMoneyReasonBuffer = new();
        private static readonly Dictionary<long, (long ChatId, int MsgId)> EconomyTradeUiContext = new();
        private static readonly Dictionary<long, TradeFlowMode> EconomyTradeFlowMode = new();
        private static readonly Dictionary<long, (long ChatId, int MsgId)> EconomyPreviewUiContext = new();
        private static readonly Dictionary<long, (long ChatId, int MsgId)> EconomyIncomeLimitUiContext = new();
        private static readonly Dictionary<long, (long ChatId, int MsgId)> EconomySectorUiContext = new();
        private static readonly Dictionary<long, (long ChatId, int MsgId)> EconomyCurrencyUiContext = new();
        private static readonly Dictionary<long, (long ChatId, int MsgId)> EconomyPublicUiContext = new();
        private static readonly Dictionary<long, (long ChatId, int MsgId)> EconomyMilitaryProvisionUiContext = new();
        private static readonly Dictionary<long, MilitaryBattlePlanContext> MilitaryBattlePlanByUser = new();
        private static readonly Dictionary<long, MilitaryIntelPlanContext> MilitaryIntelPlanByUser = new();
        private static readonly Dictionary<long, string> EconomyPublicModuleRouteByUser = new();
        private static readonly Dictionary<long, string> PublicGmDiseaseTargetByUser = new();
        private static readonly Dictionary<long, CurrencyConvertContext> EconomyCurrencyConvertState = new();
        private static readonly Dictionary<long, CurrencyActionContext> EconomyCurrencyActionState = new();
        private static readonly Dictionary<long, LoanApprovalRequest> LoanApprovalRequests = new();
        private static readonly Dictionary<long, TradeApprovalRequest> TradeApprovalRequests = new();
        private static long NextLoanApprovalId = 0;
        private static long NextTradeApprovalId = 0;
        private static readonly Dictionary<long, string> EconomySectorSelectionByUser = new();
        private static readonly Dictionary<long, int> EconomyPreviewDaysBuffer = new();
        private static readonly Dictionary<long, EconomyResourceRecord> EconomyResourceBuffer = new();
        private static readonly Dictionary<long, SanctionsEfficiencyContext> EconomySanctionsEdit = new();
        private static readonly Dictionary<long, (long ChatId, int MsgId)> EconomySanctionsUiContext = new();
        private static readonly Dictionary<long, ProductSanctionsEfficiencyContext> EconomyProductSanctionsEdit = new();
        private static readonly Dictionary<long, (long ChatId, int MsgId)> EconomyProductSanctionsUiContext = new();
        private static readonly Dictionary<long, AdminInputMode> AdminInputState = new();
        private static readonly Dictionary<long, string> AdminPopulationRaceBuffer = new();
        private static readonly Dictionary<long, int> AdminPopulationRacePageBuffer = new();
        private static readonly Dictionary<long, ResearchInputMode> ResearchInputState = new();
        private static readonly Dictionary<long, ResearchCreationContext> ResearchInputContext = new();
        private static readonly Dictionary<long, string> LastResearchBranchByUser = new();

        private static string DecodeKey(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                return string.Empty;
            string s = key.Replace('-', '+').Replace('_', '/');
            switch (s.Length % 4)
            {
                case 2: s += "=="; break;
                case 3: s += "="; break;
            }
            var bytes = Convert.FromBase64String(s);
            return Encoding.UTF8.GetString(bytes);
        }

        private static string EncodeKey(string value)
        {
            var bytes = Encoding.UTF8.GetBytes(value ?? string.Empty);
            return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        }

        private sealed class EconomyEditContext
        {
            public string Kind { get; set; } = string.Empty;  
            public int Id { get; set; }
            public string? Key { get; set; }
            public string FieldName { get; set; } = string.Empty;
            public FieldKind FieldKind { get; set; }
        }

        private sealed class BuildingCitySelectionContext
        {
            public int CountryId { get; set; }
            public EconomyInputMode NextMode { get; set; }
        }

        private sealed class MilitaryBattlePlanContext
        {
            public int? TargetCountryId { get; set; }
            public string Terrain { get; set; } = "Plains";
            public string Tactic { get; set; } = "Balanced";
            public bool IncludeVassals { get; set; } = true;
        }

        private sealed class MilitaryIntelPlanContext
        {
            public int? TargetCountryId { get; set; }
            public string OperationType { get; set; } = "ScoutForces";
            public long BudgetGold { get; set; } = 500;
        }

        private sealed class SanctionsEfficiencyContext
        {
            public int TargetCountryId { get; set; }
            public bool IsExport { get; set; }
        }

        private sealed class ProductSanctionsEfficiencyContext
        {
            public int TargetCountryId { get; set; }
            public string ResourceType { get; set; } = string.Empty;
            public bool IsExport { get; set; }
        }

        private sealed class CurrencyConvertContext
        {
            public string FromCode { get; set; } = string.Empty;
            public string ToCode { get; set; } = string.Empty;
        }

        private enum CurrencyActionType
        {
            Transfer,
            Loan,
            Repay,
            AdjustRate
        }

        private sealed class CurrencyActionContext
        {
            public CurrencyActionType Action { get; set; }
            public int FromCountryId { get; set; }
            public int ToCountryId { get; set; }
            public int TransferDays { get; set; } = 1;
            public bool AwaitingTransferDaysInput { get; set; }
        }

        private sealed class LoanApprovalRequest
        {
            public long RequestId { get; set; }
            public long RequesterUserId { get; set; }
            public int FromCountryId { get; set; }
            public int ToCountryId { get; set; }
            public decimal Amount { get; set; }
            public DateTime CreatedAtUtc { get; set; }
        }

        private sealed class TradeApprovalRequest
        {
            public long RequestId { get; set; }
            public long RequesterUserId { get; set; }
            public TradeFlowMode FlowMode { get; set; }
            public EconomyTradeAgreementRecord Agreement { get; set; } = new();
            public DateTime CreatedAtUtc { get; set; }
        }

        private enum EconomyInputMode
        {
            None,
            AddBuilding,
            AddCompany,
            AddFestival,
            AddTrade,
            AddMilitaryUnit,
            AddVassalObligation,
            AddBuildingTemplate,
            BuildingCityName,
            CreateCityName,
            AddTreasury,
            RemoveTreasury,
            AddResource,
            SanctionsEfficiency,
            ProductSanctionsEfficiency,
            PreviewDays,
            IncomeLimit,
            PublicBudgetHealth,
            PublicBudgetEducation,
            PublicBudgetIntl,
            SectorRevenueMultiplier,
            SectorTaxOverride,
            CurrencyConvert,
            CurrencyAdjustRate,
            CurrencyTransfer,
            CurrencyLoan,
            CurrencyRepay,
            MilitaryAutoProvisionPercent
        }

        private enum TradeFlowMode
        {
            Both,
            ExportOnly,
            ImportOnly
        }

        private enum AdminInputMode
        {
            None,
            SetCountryName,
            SetTax,
            SetInflation,
            SetMilitaryUpkeep,
            SetSoldierSalary,
            SetDeputySalary,
            SetCivilianSalary,
            SetStability,
            SetCorruption,
            SetHappiness,
            SetUnrest,
            SetPopulationGrowth,
            AddPopulation,
            RemovePopulation,
            CreatePopulationRace,
            SetPopulationRaceAmount,
            AddPopulationRaceAmount,
            RemovePopulationRaceAmount,
            MobilizePercent,
            AdjustArmyMorale,
            AdjustWeaponQualityBonus,
            SetBudget,
            SetIntel,
            SetCounterIntel
        }

        private enum ResearchInputMode
        {
            None,
            Create
        }

        private sealed class ResearchCreationContext
        {
            public int Step { get; set; }
            public Research.ResearchDefinition Def { get; set; } = new Research.ResearchDefinition();
            public List<int> Requirements { get; set; } = new();
            public List<Research.ResearchEffect> Effects { get; set; } = new();
            public bool AwaitingEffectType { get; set; }
            public bool AwaitingEffectValue { get; set; }
            public bool AwaitingEffectAmount { get; set; }
            public Research.ResearchEffect PendingEffect { get; set; } = new Research.ResearchEffect();
        }

        private enum FieldKind
        {
            String,
            Int,
            Long,
            Float,
            Decimal,
            Bool
        }

        private static readonly List<(string FieldName, string Prompt, FieldKind Kind)> BuildingFields = new()
        {
            ("Type", "Назва будівлі (тип):", FieldKind.String),
            ("Category", "Категорія (житло/виробництво/адміністрація/наука/військова/культура/інше):", FieldKind.String),
            ("Level", "Рівень (int 1-10):", FieldKind.Int),
            ("JobsProvided", "Робочі місця (int):", FieldKind.Int),
            ("UpkeepCost", "Утримання (decimal):", FieldKind.Decimal),
            ("BaseProduction", "Базове виробництво (decimal):", FieldKind.Decimal),
            ("BuildCost", "Вартість будівництва (decimal):", FieldKind.Decimal),
            ("BuildTurns", "Тривалість будівництва (дні, int):", FieldKind.Int),
            ("ResourceType", "Вихідний ресурс (що будівля виробляє, або '-' якщо немає):", FieldKind.String),
            ("InputResourceType", "Вхідний ресурс (що будівля споживає, або '-' якщо немає):", FieldKind.String),
            ("InputAmount", "Споживання вхідного ресурсу за день (decimal, 0 якщо не споживає):", FieldKind.Decimal),
            ("StateOwned", "Державна власність? (так/ні або 1/0):", FieldKind.Bool),
            ("StabilityBonus", "Бонус стабільності (float):", FieldKind.Float),
            ("CorruptionReduction", "Зниження корупції (float):", FieldKind.Float),
            ("ResearchBonus", "Бонус науки (float):", FieldKind.Float),
            ("ArmyBonus", "Бонус армії (int):", FieldKind.Int)
        };

        private static readonly List<(string FieldName, string Prompt, FieldKind Kind)> FestivalFields = new()
        {
            ("Name", "Назва фестивалю:", FieldKind.String),
            ("DurationTurns", "Тривалість (дні, int):", FieldKind.Int),
            ("Cost", "Вартість (decimal):", FieldKind.Decimal),
            ("StabilityBonus", "Бонус стабільності (float):", FieldKind.Float)
        };

        private static readonly List<(string FieldName, string Prompt, FieldKind Kind)> TradeFields = new()
        {
            ("ExportResourceType", "Ресурс експорту (їжа/товари/матеріали або '-' якщо немає):", FieldKind.String),
            ("ExportQty", "Експорт: кількість (decimal, > 0):", FieldKind.Decimal),
            ("ExportUnitPrice", "Експорт: ціна за 1 (decimal, 0 = ринкова):", FieldKind.Decimal),
            ("ImportResourceType", "Ресурс імпорту (їжа/товари/матеріали або '-' якщо немає):", FieldKind.String),
            ("ImportQty", "Імпорт: кількість (decimal, > 0):", FieldKind.Decimal),
            ("ImportUnitPrice", "Імпорт: ціна за 1 (decimal, 0 = ринкова):", FieldKind.Decimal),
            ("QuantityIsPerDay", "Кількість задана за день? (так/ні):", FieldKind.Bool),
            ("PeriodDays", "Час доставки у днях (int, напр. 1 або 7):", FieldKind.Int),
            ("DeliveryMode", "Тип доставки (караван/вода/повітря):", FieldKind.String),
            ("IsActive", "Активна угода? (так/ні або 1/0):", FieldKind.Bool)
        };

        private static readonly List<(string FieldName, string Prompt, FieldKind Kind)> BuildingTemplateFields = new()
        {
            ("Level", "Рівень (int 1-10):", FieldKind.Int),
            ("Quantity", "Кількість будівель (int):", FieldKind.Int),
            ("ResourceType", "Вихідний ресурс (що будівля виробляє, або '-' якщо немає):", FieldKind.String),
            ("InputResourceType", "Вхідний ресурс (що будівля споживає, або '-' якщо немає):", FieldKind.String),
            ("InputAmount", "Споживання вхідного ресурсу за день (decimal, 0 якщо не споживає):", FieldKind.Decimal)
        };

        private static readonly List<(string FieldName, string Prompt, FieldKind Kind)> CompanyFields = new()
        {
            ("Name", "Назва компанії:", FieldKind.String),
            ("OwnerName", "Власник (текст або '-' якщо немає):", FieldKind.String),
            ("ProductionType", "Тип виробництва (загальне/військпром/аркан-зброя/облогова/озброєння/харчі/промисловість/логістика/послуги):", FieldKind.String),
            ("InputResourceType", "Ресурс, який компанія споживає щодня (або '-' якщо не споживає):", FieldKind.String),
            ("InputAmount", "Споживання ресурсу за день (decimal, 0 якщо не споживає):", FieldKind.Decimal),
            ("JobsOffered", "Робочі місця (int):", FieldKind.Int),
            ("BaseSalary", "Базова зарплата (decimal):", FieldKind.Decimal),
            ("TaxRate", "Податок компанії (0..0.95, decimal):", FieldKind.Decimal),
            ("Budget", "Бюджет компанії (decimal):", FieldKind.Decimal),
            ("IsStateCompany", "Держкомпанія? (так/ні або 1/0):", FieldKind.Bool),
            ("MilitaryProductionShare", "Військовий профіль 0..100 (float):", FieldKind.Float),
            ("IsActive", "Активна? (так/ні або 1/0):", FieldKind.Bool)
        };

        private static readonly List<(string FieldName, string Prompt, FieldKind Kind)> ResourceFields = new()
        {
            ("ResourceType", "Тип ресурсу (унікальна назва):", FieldKind.String),
            ("ParentResourceType", "Батьківський тип ресурсу (напр. Food, або '-' якщо немає):", FieldKind.String),
            ("BasePrice", "Базова ціна (decimal):", FieldKind.Decimal),
            ("IsTradeable", "Доступний для торгівлі? (так/ні або 1/0):", FieldKind.Bool),
            ("DemandFactor", "Коефіцієнт попиту (float, 0 = без попиту, 1 = база):", FieldKind.Float)
        };

        private static readonly List<(string FieldName, string Prompt, FieldKind Kind)> MilitaryUnitFields = new()
        {
            ("UnitName", "Назва частини (напр. Північний гарнізон):", FieldKind.String),
            ("UnitLevel", "Рівень частини (гарнізон/відділення/взвод/рота/батальйон/полк/бригада/дивізія/легіон/флот):", FieldKind.String),
            ("UnitRole", "Роль (польова/гарнізон/авангард/резерв/облогова/флот):", FieldKind.String),
            ("UnitType", "Тип (піхота/копійники/лучники/арбалетники/кавалерія/важка піхота/маги/облогові/розвідники/флот/підтримка):", FieldKind.String),
            ("Race", "Раса (люди/ельфи/тіфлінги/гриби/...):", FieldKind.String),
            ("Soldiers", "Кількість солдатів (int):", FieldKind.Int),
            ("EquipmentTier", "Клас спорядження (ополчення/стандарт/важка/елітна/арканна/морська):", FieldKind.String),
            ("EquipmentQuality", "Якість спорядження 0..100 (float):", FieldKind.Float),
            ("BattleExperience", "Бойовий досвід 0..100 (float):", FieldKind.Float),
            ("Fatigue", "Втома 0..100 (float):", FieldKind.Float),
            ("TrainingLevel", "Рівень підготовки 0..100 (float):", FieldKind.Float),
            ("Morale", "Мораль 0..100 (float):", FieldKind.Float),
            ("WeaponReadiness", "Озброєння 0..100 (float):", FieldKind.Float),
            ("SupplyLevel", "Забезпечення 0..100 (float):", FieldKind.Float),
            ("CommanderName", "Ім'я командира (або '-' якщо немає):", FieldKind.String),
            ("CommanderSkill", "Навички командира 0..100 (float):", FieldKind.Float),
            ("CommanderLoyalty", "Лояльність командира 0..100 (float):", FieldKind.Float),
            ("CommanderCorruption", "Корупція командира 0..100 (float):", FieldKind.Float),
            ("CommanderExperience", "Досвід командира 0..100 (float):", FieldKind.Float),
            ("Status", "Статус (формується/активна/резерв):", FieldKind.String),
            ("IsMobilized", "Екстрена мобілізація? (так/ні або 1/0):", FieldKind.Bool),
            ("FormationProgress", "Прогрес формування 0..100 (float):", FieldKind.Float),
            ("FormationBaseDays", "Базовий час формування (дні, int):", FieldKind.Int)
        };

        private static readonly List<(string FieldName, string Prompt, FieldKind Kind)> MilitaryUnitCreationFields = new()
        {
            ("UnitName", "Назва частини (напр. Стража столиці):", FieldKind.String),
            ("UnitLevel", "Рівень частини (гарнізон/відділення/взвод/рота/батальйон/полк/бригада/дивізія/легіон/флот):", FieldKind.String),
            ("UnitRole", "Роль (польова/гарнізон/авангард/резерв/облогова/флот):", FieldKind.String),
            ("UnitType", "Тип (піхота/копійники/лучники/арбалетники/кавалерія/важка піхота/маги/облогові/розвідники/флот/підтримка):", FieldKind.String),
            ("Race", "Раса (люди/ельфи/тіфлінги/гриби/...):", FieldKind.String),
            ("Soldiers", "Кількість солдатів (int):", FieldKind.Int),
            ("EquipmentTier", "Клас спорядження (ополчення/стандарт/важка/елітна/арканна/морська):", FieldKind.String),
            ("EquipmentQuality", "Якість спорядження 0..100 (float):", FieldKind.Float),
            ("TrainingLevel", "Рівень підготовки 0..100 (float):", FieldKind.Float),
            ("Morale", "Мораль 0..100 (float):", FieldKind.Float),
            ("WeaponReadiness", "Озброєння 0..100 (float):", FieldKind.Float),
            ("SupplyLevel", "Забезпечення 0..100 (float):", FieldKind.Float),
            ("CommanderName", "Ім'я командира (або '-' якщо немає):", FieldKind.String),
            ("CommanderSkill", "Навички командира 0..100 (float):", FieldKind.Float),
            ("CommanderLoyalty", "Лояльність командира 0..100 (float):", FieldKind.Float),
            ("CommanderCorruption", "Корупція командира 0..100 (float):", FieldKind.Float),
            ("BattleExperience", "Бойовий досвід 0..100 (float):", FieldKind.Float),
            ("Fatigue", "Втома 0..100 (float):", FieldKind.Float),
            ("Status", "Статус (формується/активна/резерв):", FieldKind.String),
            ("FormationBaseDays", "Базовий час формування (дні, int):", FieldKind.Int)
        };

        private static readonly List<(string FieldName, string Prompt, FieldKind Kind)> VassalFields = new()
        {
            ("SubjectName", "Назва васала/колонії:", FieldKind.String),
            ("RegimentsProvided", "К-сть полків (int):", FieldKind.Int),
            ("FundingPerTurn", "Данина/фінансування за хід (decimal):", FieldKind.Decimal),
            ("Loyalty", "Лояльність 0..100 (float):", FieldKind.Float),
            ("RebellionRisk", "Ризик повстання 0..100 (float):", FieldKind.Float),
            ("IsActive", "Зобов'язання активне? (так/ні або 1/0):", FieldKind.Bool)
        };

        public StateAdministration(ILogger<StateAdministration> logger, StateParliamentHandle stateParliamentHandle)
        {
            _logger = logger;
            _stateParliamentHandle = stateParliamentHandle;
        }


        public async Task ShowStateAdministration(ITelegramBotClient botClient, long userId, CancellationToken cancellationToken, long sourceChatId = 0)
        {
            var provider = new StateDataProvider();
            var selectedCountryId = GetSelectedCountryIdForUser(userId);
            bool isCountryOwner = IsSelectedCountryOwner(userId, selectedCountryId);
            TryEnsureParliamentProvision(userId, selectedCountryId, isCountryOwner);
            var stateData = provider.GetStateData(userId, selectedCountryId);

            var text = StateMessageBuilder.BuildMainStateMessage(stateData);
            var keyboard = StateKeyboardBuilder.MainStateKeyboard(userId, stateData?.CountryName, stateData?.HasParliament, userId == masterID, isCountryOwner);
            long targetChatId = sourceChatId != 0 ? sourceChatId : userId;

            try
            {
                await botClient.SendTextMessageAsync(chatId: targetChatId, text: text, replyMarkup: keyboard, cancellationToken: cancellationToken);
            }
            catch (ApiRequestException ex)
            {
                _logger.LogWarning(ex, "Failed to open state administration. UserId={UserId} TargetChatId={TargetChatId}", userId, targetChatId);
                if (targetChatId != userId)
                {
                    try
                    {
                        await botClient.SendTextMessageAsync(chatId: userId, text: text, replyMarkup: keyboard, cancellationToken: cancellationToken);
                        LastStateViewByUser[userId] = "main";
                    }
                    catch (ApiRequestException fallbackEx)
                    {
                        _logger.LogWarning(fallbackEx, "Fallback open state administration in private chat failed. UserId={UserId}", userId);
                    }
                }
                return;
            }
            LastStateViewByUser[userId] = "main";
        }
        public async Task EditStateAdministrationMessage(ITelegramBotClient botClient, long chatId, long userId, int msgId, CancellationToken cancellationToken)
        {
            var provider = new StateDataProvider();
            var selectedCountryId = GetSelectedCountryIdForUser(userId);
            bool isCountryOwner = IsSelectedCountryOwner(userId, selectedCountryId);
            TryEnsureParliamentProvision(userId, selectedCountryId, isCountryOwner);
            var stateData = provider.GetStateData(userId, selectedCountryId);

            var text = StateMessageBuilder.BuildMainStateMessage(stateData);
            var keyboard = StateKeyboardBuilder.MainStateKeyboard(userId, stateData?.CountryName, stateData?.HasParliament, userId == masterID, isCountryOwner);

            await SafeEditMessageTextAsync(botClient, chatId, msgId, text, keyboard, cancellationToken);
            LastStateViewByUser[userId] = "main";
        }

    
        private async Task SafeEditMessageTextAsync(ITelegramBotClient botClient, long chatId, int messageId, string text, Telegram.Bot.Types.ReplyMarkups.InlineKeyboardMarkup replyMarkup, CancellationToken cancellationToken)
        {
            try
            {
                await botClient.EditMessageTextAsync(chatId: chatId, messageId: messageId, text: text, replyMarkup: replyMarkup, cancellationToken: cancellationToken);
                }
                catch (ApiRequestException ex)
                {
                    if (ex.Message != null && ex.Message.Contains("message is not modified", StringComparison.OrdinalIgnoreCase))
                    {
                        return;
                    }
                    if (ex.Message != null && ex.Message.Contains("message to edit not found", StringComparison.OrdinalIgnoreCase))
                    {
                        await botClient.SendTextMessageAsync(chatId: chatId, text: text, replyMarkup: replyMarkup, cancellationToken: cancellationToken);
                        return;
                    }
                    if (ex.Message != null && ex.Message.Contains("message can't be edited", StringComparison.OrdinalIgnoreCase))
                    {
                        await botClient.SendTextMessageAsync(chatId: chatId, text: text, replyMarkup: replyMarkup, cancellationToken: cancellationToken);
                        return;
                    }

                    _logger.LogError(ex, "StateAdministration SafeEditMessageTextAsync failed. ChatId={ChatId} MessageId={MessageId} TextLength={TextLength} ErrorCode={ErrorCode} Error={Error}",
                        chatId, messageId, text?.Length ?? 0, ex.ErrorCode, ex.Message);
                throw;
            }
        }


        public static int? GetSelectedCountryIdForUser(long userId)
        {
            if (SelectedCountryByUser.TryGetValue(userId, out var selectedId))
                return selectedId;

            var assigned = UserData.GetActiveCharacterCountryId(userId);
            if (assigned.HasValue && assigned.Value > 0)
            {
                SelectedCountryByUser[userId] = assigned.Value;
                return assigned.Value;
            }

            var first = CountriesData.GetAllCountries().FirstOrDefault();
            if (first != null)
            {
                SelectedCountryByUser[userId] = first.CountryId;
                return first.CountryId;
            }

            return null;
        }

        private static bool TryParseCountryId(string data, out int countryId)
        {
            countryId = 0;
            var parts = data.Split('_');
            if (parts.Length < 3)
                return false;

            return int.TryParse(parts[2], out countryId);
        }

        private static bool TryParseTrailingId(string data, out int id)
        {
            id = 0;
            if (string.IsNullOrWhiteSpace(data))
                return false;
            var parts = data.Split('_');
            if (parts.Length == 0)
                return false;
            return int.TryParse(parts[^1], out id);
        }

        private static bool IsOverviewView(long userId)
        {
            return LastStateViewByUser.TryGetValue(userId, out var view) && view == "overview";
        }

        private static bool IsSelectedCountryOwner(long userId, int? selectedCountryId)
        {
            if (!selectedCountryId.HasValue)
                return false;

            var assigned = UserData.GetActiveCharacterCountryId(userId);
            return assigned.HasValue && assigned.Value == selectedCountryId.Value;
        }

        private void TryEnsureParliamentProvision(long userId, int? selectedCountryId, bool isCountryOwner)
        {
            if (!selectedCountryId.HasValue || selectedCountryId.Value <= 0)
                return;
            if (!isCountryOwner && userId != masterID)
                return;

            try
            {
                ParliamentProvisioningService.EnsureParliamentForCountry(selectedCountryId.Value);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Auto parliament provisioning failed. UserId={UserId} CountryId={CountryId}", userId, selectedCountryId.Value);
            }
        }

        private async Task SafeAnswerCallbackQueryAsync(ITelegramBotClient botClient, string callbackQueryId, string? text = null, CancellationToken cancellationToken = default)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(text))
                {
                    await botClient.AnswerCallbackQueryAsync(callbackQueryId, cancellationToken: cancellationToken);
                }
                else
                {
                    await botClient.AnswerCallbackQueryAsync(callbackQueryId, text, cancellationToken: cancellationToken);
                }
            }
            catch (Telegram.Bot.Exceptions.ApiRequestException ex)
            {
                if (ex.Message != null &&
                    (ex.Message.Contains("query is too old", StringComparison.OrdinalIgnoreCase) ||
                     ex.Message.Contains("query ID is invalid", StringComparison.OrdinalIgnoreCase)))
                {
                    return;
                }

                _logger.LogError(ex, "StateAdministration SafeAnswerCallbackQueryAsync failed. CallbackQueryId={CallbackQueryId} ErrorCode={ErrorCode} Error={Error}",
                    callbackQueryId, ex.ErrorCode, ex.Message);
                throw;
            }
        }

    }
}
