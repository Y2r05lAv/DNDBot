using DNDBot.DataBase;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace DNDBot.Goverment
{
    public partial class StateParliamentHandle : BotBodyRealisation
    {
        public readonly ILogger<StateParliamentHandle> _logger;
        private static readonly Dictionary<long, bool> AwaitingQuorumPercent = new();
        private static readonly Dictionary<long, int> QuorumPercentByUser = new();
        private static readonly Dictionary<long, int> PendingSessionCountryByUser = new();
        private static readonly Dictionary<long, bool> AwaitingSpeechResult = new();
        private static readonly Dictionary<long, int> PendingSpeechCountryByUser = new();
        private static readonly Dictionary<long, long> PendingSpeechRequesterByUser = new();
        private static readonly Dictionary<int, int> SpeechInfluenceByCountry = new();
        private static readonly Dictionary<int, int> ActiveParliamentSessionByCountry = new();
        private static readonly Dictionary<long, bool> AwaitingVotingPercentByUser = new();
        private static readonly Dictionary<long, VotingPercentContext> PendingVotingByUser = new();
        private static int msgIdForParliamentSession = 0;
        private static readonly Random Rng = new();

        public StateParliamentHandle(ILogger<StateParliamentHandle> logger)
        {
            _logger = logger;
        }

        public async Task HandleCallbackQ(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken, Message message, CallbackQuery callbackQuery)
        {
            string data = callbackQuery.Data ?? "";
            long userId = callbackQuery.From.Id;

            if (callbackQuery.Message?.Chat != null &&
                callbackQuery.Message.Chat.Type != Telegram.Bot.Types.Enums.ChatType.Private &&
                data.StartsWith("stateparliament_", StringComparison.OrdinalIgnoreCase))
            {
                await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id, "Держуправління працює в особистому чаті.", cancellationToken);
                return;
            }

            var dataParts = data.Split('_');
            string info = dataParts.Length > 1 ? dataParts[1] : string.Empty;

            if (TryParseDeputyInfoCallback(data, out int deputyInfoId))
            {
                await HandleDeputySelection(botClient, callbackQuery, deputyInfoId, cancellationToken);
                await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                return;
            }

            if (!data.StartsWith("stateparliament_"))
                return;
            if (callbackQuery.Message == null)
                return;
            if (string.IsNullOrWhiteSpace(info))
                return;

            switch (info)
            {
                case "activelaw":
                    await ShowActiveLawCategories(botClient, userId, callbackQuery.Message.Chat.Id, callbackQuery.Message.MessageId, cancellationToken);
                    break;
                case "review":
                    if (string.Equals(data, "stateparliament_review", StringComparison.OrdinalIgnoreCase))
                    {
                        await ShowLawReviewList(botClient, userId, callbackQuery.Message.Chat.Id, callbackQuery.Message.MessageId, 0, cancellationToken);
                    }
                    else if (data.StartsWith("stateparliament_review_page_", StringComparison.OrdinalIgnoreCase))
                    {
                        int reviewPage = 0;
                        var parts = data.Split('_');
                        if (parts.Length > 0 && int.TryParse(parts[^1], out int parsed))
                            reviewPage = Math.Max(0, parsed);
                        await ShowLawReviewList(botClient, userId, callbackQuery.Message.Chat.Id, callbackQuery.Message.MessageId, reviewPage, cancellationToken);
                    }
                    else if (data.StartsWith("stateparliament_review_show_", StringComparison.OrdinalIgnoreCase) && TryParseTrailingId(data, out int reviewLawId))
                    {
                        await ShowLawReviewInfo(botClient, userId, callbackQuery.Message.Chat.Id, callbackQuery.Message.MessageId, reviewLawId, cancellationToken);
                    }
                    else if (data.StartsWith("stateparliament_review_advance_", StringComparison.OrdinalIgnoreCase) && TryParseTrailingId(data, out int advanceLawId))
                    {
                        await AdvanceLawReviewStage(botClient, userId, callbackQuery.Message.Chat.Id, callbackQuery.Message.MessageId, advanceLawId, cancellationToken);
                    }
                    else if (data.StartsWith("stateparliament_review_reject_", StringComparison.OrdinalIgnoreCase) && TryParseTrailingId(data, out int rejectLawId))
                    {
                        await RejectLawDuringReview(botClient, userId, callbackQuery.Message.Chat.Id, callbackQuery.Message.MessageId, rejectLawId, cancellationToken);
                    }
                    else if (data.StartsWith("stateparliament_review_edit_title_", StringComparison.OrdinalIgnoreCase) && TryParseTrailingId(data, out int editTitleLawId))
                    {
                        await BeginLawReviewEdit(botClient, userId, chatId: callbackQuery.Message.Chat.Id, msgId: callbackQuery.Message.MessageId, lawId: editTitleLawId, field: LawReviewEditField.Title, cancellationToken: cancellationToken);
                    }
                    else if (data.StartsWith("stateparliament_review_edit_desc_", StringComparison.OrdinalIgnoreCase) && TryParseTrailingId(data, out int editDescLawId))
                    {
                        await BeginLawReviewEdit(botClient, userId, chatId: callbackQuery.Message.Chat.Id, msgId: callbackQuery.Message.MessageId, lawId: editDescLawId, field: LawReviewEditField.Description, cancellationToken: cancellationToken);
                    }
                    else if (data.StartsWith("stateparliament_review_edit_effect_", StringComparison.OrdinalIgnoreCase) && TryParseTrailingId(data, out int editEffectLawId))
                    {
                        await BeginLawReviewEffectEdit(botClient, userId, callbackQuery.Message.Chat.Id, callbackQuery.Message.MessageId, editEffectLawId, cancellationToken);
                    }
                    else if (TryParseLawReviewEffectToggleData(data, out int toggleLawId, out string effectToken))
                    {
                        await ToggleLawReviewEffect(botClient, userId, callbackQuery.Message.Chat.Id, callbackQuery.Message.MessageId, toggleLawId, effectToken, cancellationToken);
                    }
                    else if (TryParseLawReviewEffectValueData(data, out int valueLawId, out string valueToken))
                    {
                        await BeginLawReviewEffectValueInput(botClient, userId, callbackQuery.Message.Chat.Id, callbackQuery.Message.MessageId, valueLawId, valueToken, cancellationToken);
                    }
                    else if (TryParseLawReviewEffectScopeData(data, out int scopeLawId, out string scopeToken))
                    {
                        await BeginLawReviewEffectScopeInput(botClient, userId, callbackQuery.Message.Chat.Id, callbackQuery.Message.MessageId, scopeLawId, scopeToken, cancellationToken);
                    }
                    else if (data.StartsWith("stateparliament_review_effect_apply_", StringComparison.OrdinalIgnoreCase) && TryParseTrailingId(data, out int applyEffectLawId))
                    {
                        await ApplyLawReviewEffectEdit(botClient, userId, callbackQuery.Message.Chat.Id, callbackQuery.Message.MessageId, applyEffectLawId, cancellationToken);
                    }
                    else if (data.StartsWith("stateparliament_review_effect_clear_", StringComparison.OrdinalIgnoreCase) && TryParseTrailingId(data, out int clearEffectLawId))
                    {
                        await ClearLawReviewEffectSelection(botClient, userId, callbackQuery.Message.Chat.Id, callbackQuery.Message.MessageId, clearEffectLawId, cancellationToken);
                    }
                    else if (data.StartsWith("stateparliament_review_effect_cancel_", StringComparison.OrdinalIgnoreCase) && TryParseTrailingId(data, out int cancelEffectLawId))
                    {
                        await CancelLawReviewEffectEdit(botClient, userId, callbackQuery.Message.Chat.Id, callbackQuery.Message.MessageId, cancelEffectLawId, cancellationToken);
                    }
                    break;
                case "voting":
                    await ShowVotingLawList(botClient, userId, callbackQuery.Message.Chat.Id, callbackQuery.Message.MessageId, 0, cancellationToken);
                    break;
                case "voting_page":
                    int votingPage = 0;
                    if (data.StartsWith("stateparliament_voting_page_", StringComparison.OrdinalIgnoreCase))
                    {
                        var parts = data.Split('_');
                        if (parts.Length > 0 && int.TryParse(parts[^1], out int parsed))
                            votingPage = parsed;
                    }
                    await ShowVotingLawList(botClient, userId, callbackQuery.Message.Chat.Id, callbackQuery.Message.MessageId, votingPage, cancellationToken);
                    break;
                case "votinglaw":
                    if (TryParseTrailingId(data, out int votingLawId))
                        await ShowVotingLawInfo(botClient, userId, callbackQuery.Message.Chat.Id, callbackQuery.Message.MessageId, votingLawId, cancellationToken);
                    break;
                case "votingstart":
                    if (data.StartsWith("stateparliament_votingstart_manual_", StringComparison.OrdinalIgnoreCase))
                    {
                        if (TryParseTrailingId(data, out int voteStartLawId))
                            await HandleVotingStart(botClient, userId, callbackQuery.Message.Chat.Id, callbackQuery.Message.MessageId, voteStartLawId, manual: true, cancellationToken);
                    }
                    else if (data.StartsWith("stateparliament_votingstart_auto_", StringComparison.OrdinalIgnoreCase))
                    {
                        if (TryParseTrailingId(data, out int voteStartLawId))
                            await HandleVotingStart(botClient, userId, callbackQuery.Message.Chat.Id, callbackQuery.Message.MessageId, voteStartLawId, manual: false, cancellationToken);
                    }
                    break;
                case "votingconfirm":
                    if (data.StartsWith("stateparliament_votingconfirm_yes_", StringComparison.OrdinalIgnoreCase) && TryParseTrailingId(data, out int yesLawId))
                        await HandleVotingConfirm(botClient, callbackQuery, yesLawId, accept: true, VotingAction.AcceptLaw, cancellationToken);
                    else if (data.StartsWith("stateparliament_votingconfirm_no_", StringComparison.OrdinalIgnoreCase) && TryParseTrailingId(data, out int noLawId))
                        await HandleVotingConfirm(botClient, callbackQuery, noLawId, accept: false, VotingAction.AcceptLaw, cancellationToken);
                    break;
                case "votingconfirmrevoke":
                    if (data.StartsWith("stateparliament_votingconfirmrevoke_yes_", StringComparison.OrdinalIgnoreCase) && TryParseTrailingId(data, out int yesRevokeLawId))
                        await HandleVotingConfirm(botClient, callbackQuery, yesRevokeLawId, accept: true, VotingAction.RevokeLaw, cancellationToken);
                    else if (data.StartsWith("stateparliament_votingconfirmrevoke_no_", StringComparison.OrdinalIgnoreCase) && TryParseTrailingId(data, out int noRevokeLawId))
                        await HandleVotingConfirm(botClient, callbackQuery, noRevokeLawId, accept: false, VotingAction.RevokeLaw, cancellationToken);
                    break;
                case "activelawcategory":
                    if (data.EndsWith("_accepted", StringComparison.OrdinalIgnoreCase))
                        await ShowActiveLawList(botClient, userId, callbackQuery.Message.Chat.Id, callbackQuery.Message.MessageId, LawStatusAccepted, 0, cancellationToken);
                    else if (data.EndsWith("_rejected", StringComparison.OrdinalIgnoreCase))
                        await ShowActiveLawList(botClient, userId, callbackQuery.Message.Chat.Id, callbackQuery.Message.MessageId, LawStatusRejected, 0, cancellationToken);
                    else if (data.EndsWith("_revoked", StringComparison.OrdinalIgnoreCase))
                        await ShowActiveLawList(botClient, userId, callbackQuery.Message.Chat.Id, callbackQuery.Message.MessageId, LawStatusRevoked, 0, cancellationToken);
                    break;
                case "activelawlist":
                    if (data.Contains("_accepted_page_", StringComparison.OrdinalIgnoreCase))
                        await ShowActiveLawList(botClient, userId, callbackQuery.Message.Chat.Id, callbackQuery.Message.MessageId, LawStatusAccepted, ParsePage(data), cancellationToken);
                    else if (data.Contains("_rejected_page_", StringComparison.OrdinalIgnoreCase))
                        await ShowActiveLawList(botClient, userId, callbackQuery.Message.Chat.Id, callbackQuery.Message.MessageId, LawStatusRejected, ParsePage(data), cancellationToken);
                    else if (data.Contains("_revoked_page_", StringComparison.OrdinalIgnoreCase))
                        await ShowActiveLawList(botClient, userId, callbackQuery.Message.Chat.Id, callbackQuery.Message.MessageId, LawStatusRevoked, ParsePage(data), cancellationToken);
                    break;
                case "activelawbranches":
                    if (data.StartsWith("stateparliament_activelawbranches_page_", StringComparison.OrdinalIgnoreCase))
                    {
                        int acceptedPage = 0;
                        var parts = data.Split('_');
                        if (parts.Length > 0 && int.TryParse(parts[^1], out int parsed))
                            acceptedPage = Math.Max(0, parsed);
                        await ShowAcceptedLawBranches(botClient, userId, callbackQuery.Message.Chat.Id, callbackQuery.Message.MessageId, acceptedPage, cancellationToken);
                    }
                    break;
                case "activelawinfo":
                    if (TryParseTrailingId(data, out int lawInfoId))
                        await ShowActiveLawInfo(botClient, userId, callbackQuery.Message.Chat.Id, callbackQuery.Message.MessageId, lawInfoId, cancellationToken);
                    break;
                case "lawbranch":
                    if (TryParseLawBranchData(data, out int branchRootLawId, out int branchPage))
                        await ShowLawBranch(botClient, userId, callbackQuery.Message.Chat.Id, callbackQuery.Message.MessageId, branchRootLawId, branchPage, cancellationToken);
                    break;
                case "activelawrepeal":
                    _logger.LogInformation("Activating law repeal flow. Data={Data} UserId={UserId} ChatId={ChatId} MsgId={MsgId}",
                        data, userId, callbackQuery.Message.Chat.Id, callbackQuery.Message.MessageId);
                    if (data.EndsWith("_confirm", StringComparison.OrdinalIgnoreCase) && TryParseLastInt(data, out int repealConfirmId))
                        await ShowActiveLawRevokeConfirm(botClient, userId, callbackQuery.Message.Chat.Id, callbackQuery.Message.MessageId, repealConfirmId, cancellationToken);
                    else if (data.EndsWith("_manual", StringComparison.OrdinalIgnoreCase) && TryParseLastInt(data, out int repealManualId))
                        await HandleActiveLawRevokeStart(botClient, userId, callbackQuery.Message.Chat.Id, callbackQuery.Message.MessageId, repealManualId, manual: true, cancellationToken);
                    else if (data.EndsWith("_auto", StringComparison.OrdinalIgnoreCase) && TryParseLastInt(data, out int repealAutoId))
                        await HandleActiveLawRevokeStart(botClient, userId, callbackQuery.Message.Chat.Id, callbackQuery.Message.MessageId, repealAutoId, manual: false, cancellationToken);
                    else if (data.EndsWith("_yes", StringComparison.OrdinalIgnoreCase) && TryParseLastInt(data, out int repealYesId))
                        await HandleActiveLawRevokeStart(botClient, userId, callbackQuery.Message.Chat.Id, callbackQuery.Message.MessageId, repealYesId, manual: false, cancellationToken);
                    else if (data.EndsWith("_no", StringComparison.OrdinalIgnoreCase) && TryParseLastInt(data, out int repealNoId))
                        await ShowActiveLawInfo(botClient, userId, callbackQuery.Message.Chat.Id, callbackQuery.Message.MessageId, repealNoId, cancellationToken);
                    break;
                case "proposelaw":
                    await StartLawCreation(botClient, userId, cancellationToken);
                    break;
                case "lawtemplate":
                    if (string.Equals(data, "stateparliament_lawtemplate_tiers", StringComparison.OrdinalIgnoreCase))
                    {
                        await ShowReadyLawTemplateTierMenu(botClient, userId, callbackQuery.Message.Chat.Id, callbackQuery.Message.MessageId, cancellationToken);
                    }
                    else if (TryParseLawTemplateTierPage(data, out int? templateTier, out int templatePage))
                    {
                        await ShowReadyLawTemplateList(botClient, userId, callbackQuery.Message.Chat.Id, callbackQuery.Message.MessageId, templatePage, templateTier, cancellationToken);
                    }
                    else if (data.StartsWith("stateparliament_lawtemplate_pick_", StringComparison.OrdinalIgnoreCase) &&
                             TryParseTrailingId(data, out int templateId))
                    {
                        await ApplyReadyLawTemplate(botClient, userId, callbackQuery.Message.Chat.Id, callbackQuery.Message.MessageId, templateId, tier: null, cancellationToken);
                    }
                    break;
                case "lawtype":
                    if (data.StartsWith("stateparliament_lawtype_", StringComparison.OrdinalIgnoreCase))
                    {
                        string rawType = data.Substring("stateparliament_lawtype_".Length);
                        await HandleLawTypeSelection(botClient, callbackQuery, rawType, cancellationToken);
                    }
                    break;
                case "lawparent":
                    await HandleLawParentSelection(botClient, callbackQuery, data, cancellationToken);
                    break;
                case "lawdate":
                    if (data.EndsWith("_skip", StringComparison.OrdinalIgnoreCase))
                    {
                        await HandleLawDateSkip(botClient, callbackQuery, cancellationToken);
                    }
                    break;
                case "speech":
                    await StartSpeechEvaluation(botClient, callbackQuery, cancellationToken);
                    break;
                case "faction":
                    await EditMessageInfoForFaction(botClient, callbackQuery.Message.Chat.Id, callbackQuery.Message.MessageId, userId, cancellationToken);
                    break;
                case "factioncreate":
                    await StartPartyCreation(botClient, userId, cancellationToken);
                    break;
                case "factionedit":
                    if (data.Equals("stateparliament_factionedit", StringComparison.OrdinalIgnoreCase))
                    {
                        await StartPartyEditSelection(botClient, userId, callbackQuery.Message.Chat.Id, callbackQuery.Message.MessageId, cancellationToken);
                    }
                    else if (data.StartsWith("stateparliament_factionedit_pick_", StringComparison.OrdinalIgnoreCase) && TryParseTrailingId(data, out int editPartyId))
                    {
                        await HandlePartyEditSelection(botClient, callbackQuery, editPartyId, cancellationToken);
                    }
                    break;
                case "factiondelete":
                    if (data.Equals("stateparliament_factiondelete", StringComparison.OrdinalIgnoreCase))
                    {
                        await StartPartyDeleteSelection(botClient, userId, callbackQuery.Message.Chat.Id, callbackQuery.Message.MessageId, cancellationToken);
                    }
                    else if (data.StartsWith("stateparliament_factiondelete_pick_", StringComparison.OrdinalIgnoreCase) && TryParseTrailingId(data, out int deletePartyId))
                    {
                        await HandlePartyDeleteSelection(botClient, callbackQuery, deletePartyId, cancellationToken);
                    }
                    else if (data.StartsWith("stateparliament_factiondelete_yes_", StringComparison.OrdinalIgnoreCase) && TryParseTrailingId(data, out int confirmDeletePartyId))
                    {
                        await HandlePartyDeleteConfirm(botClient, callbackQuery, confirmDeletePartyId, cancellationToken);
                    }
                    break;
                case "factionlist":
                    await ShowPartiesList(botClient, userId, callbackQuery.Message.Chat.Id, callbackQuery.Message.MessageId, cancellationToken);
                    break;
                case "factionmembers":
                    await ShowPartyMembers(botClient, userId, callbackQuery.Message.Chat.Id, callbackQuery.Message.MessageId, cancellationToken);
                    break;
                case "deputies":
                    await EditMessageInfoForDeputies(botClient, callbackQuery.Message.Chat.Id, callbackQuery.Message.MessageId, userId, cancellationToken);
                    break;
                case "deputycreate":
                    if (data.StartsWith("stateparliament_deputycreate_party_", StringComparison.OrdinalIgnoreCase))
                    {
                        await HandleDeputyPartySelection(botClient, callbackQuery, DeputyCreationMode.Manual, cancellationToken);
                    }
                    else if (data.Equals("stateparliament_deputycreate", StringComparison.OrdinalIgnoreCase))
                    {
                        await StartDeputyCreation(botClient, userId, cancellationToken);
                    }
                    break;
                case "deputyfastcreate":
                    if (data.StartsWith("stateparliament_deputyfastcreate_party_", StringComparison.OrdinalIgnoreCase) ||
                        data.StartsWith("stateparliament_deputy_fastcreate_party_", StringComparison.OrdinalIgnoreCase))
                    {
                        await HandleDeputyPartySelection(botClient, callbackQuery, DeputyCreationMode.Fast, cancellationToken);
                    }
                    else if (data.Equals("stateparliament_deputyfastcreate", StringComparison.OrdinalIgnoreCase) ||
                             data.Equals("stateparliament_deputy_fastcreate", StringComparison.OrdinalIgnoreCase))
                    {
                        await StartDeputyFastCreation(botClient, userId, cancellationToken);
                    }
                    break;
                case "deputydismisslist":
                    int dismissPage = 0;
                    if (data.StartsWith("stateparliament_deputydismisslist_page_", StringComparison.OrdinalIgnoreCase))
                    {
                        var parts = data.Split('_');
                        if (parts.Length > 0 && int.TryParse(parts[^1], out int parsed))
                            dismissPage = parsed;
                    }
                    await ShowDeputyDismissList(botClient, userId, callbackQuery.Message.Chat.Id, callbackQuery.Message.MessageId, dismissPage, cancellationToken);
                    break;
                case "deputydismiss":
                    if (data.StartsWith("stateparliament_deputydismiss_", StringComparison.OrdinalIgnoreCase))
                    {
                        if (TryParseTrailingId(data, out int deputyId))
                            await HandleDeputyDismissSelection(botClient, callbackQuery, deputyId, cancellationToken);
                    }
                    break;
                case "deputydismissdo":
                    if (TryParseTrailingId(data, out int deputyDismissId))
                        await HandleDeputyDismissAction(botClient, callbackQuery, deputyDismissId, cancellationToken);
                    break;
                case "deputykill":
                    if (TryParseTrailingId(data, out int deputyKillId))
                        await HandleDeputyKillAction(botClient, callbackQuery, deputyKillId, cancellationToken);
                    break;
                case "deputydismissall":
                    if (data.EndsWith("_confirm", StringComparison.OrdinalIgnoreCase))
                    {
                        await ShowDismissAllConfirm(botClient, userId, callbackQuery.Message.Chat.Id, callbackQuery.Message.MessageId, cancellationToken);
                    }
                    else if (data.EndsWith("_yes", StringComparison.OrdinalIgnoreCase))
                    {
                        await HandleDismissAllDeputies(botClient, userId, callbackQuery.Message.Chat.Id, callbackQuery.Message.MessageId, cancellationToken);
                    }
                    else if (data.EndsWith("_no", StringComparison.OrdinalIgnoreCase))
                    {
                        await EditMessageInfoForDeputies(botClient, callbackQuery.Message.Chat.Id, callbackQuery.Message.MessageId, userId, cancellationToken);
                    }
                    break;
                case "deputy":
                    if (data.StartsWith("stateparliament_deputy_create_party_", StringComparison.OrdinalIgnoreCase))
                    {
                        await HandleDeputyPartySelection(botClient, callbackQuery, DeputyCreationMode.Manual, cancellationToken);
                    }
                    else if (data.Equals("stateparliament_deputy_create", StringComparison.OrdinalIgnoreCase))
                    {
                        await StartDeputyCreation(botClient, userId, cancellationToken);
                    }
                    else if (data.StartsWith("stateparliament_deputy_fastcreate_party_", StringComparison.OrdinalIgnoreCase))
                    {
                        await HandleDeputyPartySelection(botClient, callbackQuery, DeputyCreationMode.Fast, cancellationToken);
                    }
                    else if (data.Equals("stateparliament_deputy_fastcreate", StringComparison.OrdinalIgnoreCase))
                    {
                        await StartDeputyFastCreation(botClient, userId, cancellationToken);
                    }
                    else if (data.StartsWith("stateparliament_deputy_dismissall_", StringComparison.OrdinalIgnoreCase))
                    {
                        if (data.EndsWith("_confirm", StringComparison.OrdinalIgnoreCase))
                            await ShowDismissAllConfirm(botClient, userId, callbackQuery.Message.Chat.Id, callbackQuery.Message.MessageId, cancellationToken);
                        else if (data.EndsWith("_yes", StringComparison.OrdinalIgnoreCase))
                            await HandleDismissAllDeputies(botClient, userId, callbackQuery.Message.Chat.Id, callbackQuery.Message.MessageId, cancellationToken);
                        else if (data.EndsWith("_no", StringComparison.OrdinalIgnoreCase))
                            await EditMessageInfoForDeputies(botClient, callbackQuery.Message.Chat.Id, callbackQuery.Message.MessageId, userId, cancellationToken);
                    }
                    break;
                case "conveneparl":
                    await EditMessageInfoForConveneParl(botClient, callbackQuery.Message.Chat.Id, callbackQuery.Message.MessageId, userId, cancellationToken);
                    break;
                case "closesession":
                    await CloseActiveParliamentSession(botClient, userId, callbackQuery.Message.Chat.Id, callbackQuery.Message.MessageId, cancellationToken);
                    break;
                case "joinquorum":
                    await StartQuorumPercentInput(botClient, userId, cancellationToken);
                    break;
                case "leavequorum":
                    await NotifyLeaveQuorum(botClient, cancellationToken);
                    break;
                case "startsession":
                    if (data.EndsWith("_all", StringComparison.OrdinalIgnoreCase))
                    {
                        await ResolveSessionStart(botClient, userId, allPresent: true, cancellationToken: cancellationToken);
                    }
                    else if (data.EndsWith("_calc", StringComparison.OrdinalIgnoreCase))
                    {
                        await ResolveSessionStart(botClient, userId, allPresent: false, cancellationToken: cancellationToken);
                    }
                    else if (data.EndsWith("_cancel", StringComparison.OrdinalIgnoreCase))
                    {
                        await botClient.SendTextMessageAsync(masterID, "🏛 Функції парламенту\nСкликання сесії скасовано. Депутати розійшлися.", cancellationToken: cancellationToken);
                        await TrySendToParliamentGroupAsync(
                            botClient,
                            "Скликання сесії скасовано. Засідання не відбулося.",
                            cancellationToken,
                            notifyMasterOnFailure: false
                        );
                    }
                    break;
                case "sessionattendance":
                    await HandleSessionAttendanceCallback(botClient, callbackQuery, data, cancellationToken);
                    break;
                case "back":
                    await EditMessageInfoForParlament(botClient, callbackQuery.Message.Chat.Id, callbackQuery.Message.MessageId, userId, cancellationToken);
                    break;
                case "cancel":
                    await EditMessageInfoForStateMain(botClient, callbackQuery.Message.Chat.Id, callbackQuery.Message.MessageId, userId, cancellationToken);
                    break;
                case "alldeputies":
                    int page = 0;
                    if (data.StartsWith("stateparliament_alldeputies_page_", StringComparison.OrdinalIgnoreCase))
                    {
                        var parts = data.Split('_');
                        if (parts.Length > 0 && int.TryParse(parts[^1], out int parsed))
                            page = parsed;
                    }

                    if (!TryGetCountryForDeputy(userId, out int countryId, out string error))
                    {
                        await botClient.SendTextMessageAsync(userId, error, cancellationToken: cancellationToken);
                        break;
                    }

                    var deputies = DeputiesData.GetAllDeputies()
                        .FindAll(d => d.CountryId == countryId);
                    await SafeEditMessageTextAsync(
                        botClient,
                        callbackQuery.Message.Chat.Id,
                        callbackQuery.Message.MessageId,
                        "🤵Список усіх депутатів",
                        StateKeyboardBuilder.DeputiesListKeyboard(deputies, page),
                        cancellationToken
                    );
                    break;
                default:
                    break;
            }

            await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
        }


        public async Task HandleTextInput(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
        {
            if (message?.From == null)
                return;
            if (message.Chat == null || message.Chat.Type != Telegram.Bot.Types.Enums.ChatType.Private)
                return;

            long userId = message.From.Id;
            if (!AwaitingQuorumPercent.TryGetValue(userId, out bool awaiting) || !awaiting)
            {
                if (AwaitingSpeechResult.TryGetValue(userId, out bool awaitingSpeech) && awaitingSpeech)
                {
                    await HandleSpeechResultInput(botClient, message, cancellationToken);
                    return;
                }

                if (AwaitingVotingPercentByUser.TryGetValue(userId, out bool awaitingVoting) && awaitingVoting)
                {
                    await HandleVotingPercentInput(botClient, message, cancellationToken);
                    return;
                }

                if (LawReviewEffectValueInputStateByUser.ContainsKey(userId))
                {
                    await HandleLawReviewEffectValueInput(botClient, message, cancellationToken);
                    return;
                }

                if (LawReviewEffectScopeInputStateByUser.ContainsKey(userId))
                {
                    await HandleLawReviewEffectScopeInput(botClient, message, cancellationToken);
                    return;
                }

                if (LawReviewEditStateByUser.ContainsKey(userId))
                {
                    await HandleLawReviewEditInput(botClient, message, cancellationToken);
                    return;
                }

                if (LawCreationStepByUser.ContainsKey(userId))
                {
                    await HandleLawCreationInput(botClient, message, cancellationToken);
                    return;
                }

                if (AwaitingDeputyFastCount.Contains(userId))
                {
                    await HandleDeputyFastCountInput(botClient, message, cancellationToken);
                    return;
                }

                if (DeputyCreationModeByUser.TryGetValue(userId, out var mode) && mode == DeputyCreationMode.Manual)
                {
                    await HandleDeputyManualInput(botClient, message, cancellationToken);
                    return;
                }

                if (PartyCreationStepByUser.ContainsKey(userId))
                {
                    await HandlePartyCreationInput(botClient, message, cancellationToken);
                    return;
                }

                if (PartyEditStepByUser.ContainsKey(userId))
                {
                    await HandlePartyEditInput(botClient, message, cancellationToken);
                    return;
                }

                if (TryParseCreateLoyalDeputiesCommand(message.Text, out int? count))
                {
                    await HandleCreateLoyalDeputiesTextCommand(botClient, userId, count, cancellationToken);
                    return;
                }

                return;
            }

            string input = (message.Text ?? string.Empty).Trim();
            if (!int.TryParse(input, out int percent) || percent < 0 || percent > 100)
            {
                await botClient.SendTextMessageAsync(
                    userId,
                    "Невірне значення. Вкажіть число від 0 до 100:",
                    cancellationToken: cancellationToken
                );
                return;
            }

            AwaitingQuorumPercent[userId] = false;
            QuorumPercentByUser[userId] = percent;

            await botClient.SendTextMessageAsync(
                userId,
                $"Кворум встановлено: {percent}%.",
                cancellationToken: cancellationToken
            );

            await CallParlaimentSession(botClient, userId, cancellationToken);
        }

        public async Task<bool> TryCancelUserInputAsync(ITelegramBotClient botClient, long userId, CancellationToken cancellationToken)
        {
            bool hasActiveInput =
                (AwaitingQuorumPercent.TryGetValue(userId, out bool awaitingQuorum) && awaitingQuorum) ||
                (AwaitingSpeechResult.TryGetValue(userId, out bool awaitingSpeech) && awaitingSpeech) ||
                (AwaitingVotingPercentByUser.TryGetValue(userId, out bool awaitingVoting) && awaitingVoting) ||
                PendingSessionCountryByUser.ContainsKey(userId) ||
                PendingSpeechCountryByUser.ContainsKey(userId) ||
                PendingSpeechRequesterByUser.ContainsKey(userId) ||
                PendingVotingByUser.ContainsKey(userId) ||
                LawCreationStepByUser.ContainsKey(userId) ||
                LawCreationBufferByUser.ContainsKey(userId) ||
                LawCreationCountryByUser.ContainsKey(userId) ||
                LawReviewEditStateByUser.ContainsKey(userId) ||
                LawReviewEffectEditStateByUser.ContainsKey(userId) ||
                LawReviewEffectValueInputStateByUser.ContainsKey(userId) ||
                LawReviewEffectScopeInputStateByUser.ContainsKey(userId) ||
                PartyCreationStepByUser.ContainsKey(userId) ||
                PartyCreationBufferByUser.ContainsKey(userId) ||
                PartyCreationCountryByUser.ContainsKey(userId) ||
                PartyEditStepByUser.ContainsKey(userId) ||
                PartyEditBufferByUser.ContainsKey(userId) ||
                PartyEditCountryByUser.ContainsKey(userId) ||
                DeputyCreationModeByUser.ContainsKey(userId) ||
                DeputyCreationStepByUser.ContainsKey(userId) ||
                DeputyCreationBufferByUser.ContainsKey(userId) ||
                DeputyCreationCountryByUser.ContainsKey(userId) ||
                DeputyFastCountByUser.ContainsKey(userId) ||
                AwaitingDeputyFastCount.Contains(userId);

            if (!hasActiveInput)
                return false;

            AwaitingQuorumPercent.Remove(userId);
            PendingSessionCountryByUser.Remove(userId);
            AwaitingSpeechResult.Remove(userId);
            PendingSpeechCountryByUser.Remove(userId);
            PendingSpeechRequesterByUser.Remove(userId);
            AwaitingVotingPercentByUser.Remove(userId);
            PendingVotingByUser.Remove(userId);

            ClearLawCreationState(userId);
            LawReviewEditStateByUser.Remove(userId);
            LawReviewEffectEditStateByUser.Remove(userId);
            LawReviewEffectValueInputStateByUser.Remove(userId);
            LawReviewEffectScopeInputStateByUser.Remove(userId);

            ClearPartyCreationState(userId);
            ClearPartyEditState(userId);
            ClearDeputyCreationState(userId);
            LastDeputiesMenuByUser.Remove(userId);

            CleanupOldMessagesForUser(botClient, cancellationToken, userId);

            await Task.CompletedTask;
            return true;
        }
    }
}
