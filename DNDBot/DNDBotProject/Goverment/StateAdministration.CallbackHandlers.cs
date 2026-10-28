using DNDBot.BotSystem;
using Microsoft.Extensions.Logging;
using DNDBot;
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
        public async Task HandleCallbackQ(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken, Message message, CallbackQuery callbackQuery)
        {
            string data = callbackQuery.Data ?? "";
            if (callbackQuery.Message?.Chat == null)
                return;

            var parts = data.Split('_');
            if (parts.Length < 2)
                return;
            string info = parts[1];
            long userId = callbackQuery.From.Id;
            long chatId = callbackQuery.Message.Chat.Id;
            int msgId = callbackQuery.Message.MessageId;

            if (data.StartsWith("stateparliament_cancel"))
            {
                await EditStateAdministrationMessage(botClient, chatId, userId, msgId, cancellationToken);
                await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                return;
            }

            if (!data.StartsWith("state_"))
                return;

            if (data == "state_noop")
            {
                await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                return;
            }

            if (data.StartsWith("state_inlinepick_", StringComparison.OrdinalIgnoreCase))
            {
                string choice = data.Substring("state_inlinepick_".Length);
                await HandleInlineSelectionChoice(botClient, userId, choice, cancellationToken);
                await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                return;
            }

            if (data == "state_selectcountry")
            {
                await ShowCountrySelect(botClient, chatId, userId, msgId, cancellationToken);
                await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                return;
            }

            if (data.StartsWith("state_research_", StringComparison.OrdinalIgnoreCase))
            {
                if (!IsSelectedCountryOwner(userId, GetSelectedCountryIdForUser(userId)) && userId != masterID)
                {
                    await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id, "Нема доступу");
                    return;
                }

                if (data.Equals("state_research", StringComparison.OrdinalIgnoreCase))
                {
                    await ShowResearchMenu(botClient, chatId, userId, msgId, cancellationToken);
                    await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                    return;
                }

                if (data.Equals("state_research_list", StringComparison.OrdinalIgnoreCase))
                {
                    await ShowResearchList(botClient, chatId, userId, msgId, 0, cancellationToken);
                    await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                    return;
                }

                if (data.StartsWith("state_research_list_page_", StringComparison.OrdinalIgnoreCase))
                {
                    int page = 0;
                    if (TryParseTrailingId(data, out int parsed))
                        page = parsed;
                    await ShowResearchList(botClient, chatId, userId, msgId, page, cancellationToken);
                    await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                    return;
                }
                if (data.StartsWith("state_research_branchpick_", StringComparison.OrdinalIgnoreCase))
                {
                    string key = data.Substring("state_research_branchpick_".Length);
                    string branch = DecodeKey(key);
                    await ShowResearchBranchList(botClient, chatId, userId, msgId, branch, 0, cancellationToken);
                    await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                    return;
                }
                if (data.StartsWith("state_research_branchlist_page_", StringComparison.OrdinalIgnoreCase))
                {
                    int page = 0;
                    if (TryParseTrailingId(data, out int parsed))
                        page = parsed;
                    string branch = LastResearchBranchByUser.GetValueOrDefault(userId, "");
                    if (!string.IsNullOrWhiteSpace(branch))
                    {
                        await ShowResearchBranchList(botClient, chatId, userId, msgId, branch, page, cancellationToken);
                        await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                        return;
                    }
                }

                if (data.StartsWith("state_research_info_", StringComparison.OrdinalIgnoreCase))
                {
                    if (TryParseTrailingId(data, out int researchId))
                    {
                        await ShowResearchInfo(botClient, chatId, userId, msgId, researchId, cancellationToken);
                        await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                        return;
                    }
                }

                if (data.StartsWith("state_research_branch_", StringComparison.OrdinalIgnoreCase) &&
                    !data.StartsWith("state_research_branch_pick_", StringComparison.OrdinalIgnoreCase))
                {
                    await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                    return;
                }

                if (data.StartsWith("state_research_start_", StringComparison.OrdinalIgnoreCase))
                {
                    if (TryParseTrailingId(data, out int researchId))
                    {
                        await StartResearch(botClient, chatId, userId, msgId, researchId, cancellationToken);
                        await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                        return;
                    }
                }

                if (data.Equals("state_research_active", StringComparison.OrdinalIgnoreCase))
                {
                    await ShowActiveResearch(botClient, chatId, userId, msgId, cancellationToken);
                    await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                    return;
                }
                if (data.Equals("state_research_complete_now", StringComparison.OrdinalIgnoreCase))
                {
                    await CompleteActiveResearchManually(botClient, chatId, userId, msgId, cancellationToken);
                    await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                    return;
                }
                if (data.Equals("state_research_create", StringComparison.OrdinalIgnoreCase))
                {
                    if (userId != masterID)
                    {
                        await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id, "Нема доступу");
                        return;
                    }
                    await StartResearchCreation(botClient, userId, cancellationToken);
                    await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                    return;
                }
                if (data.Equals("state_research_create_yes", StringComparison.OrdinalIgnoreCase))
                {
                    await HandleResearchInput(botClient, userId, "так", cancellationToken);
                    await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                    return;
                }
                if (data.Equals("state_research_create_no", StringComparison.OrdinalIgnoreCase))
                {
                    await HandleResearchInput(botClient, userId, "ні", cancellationToken);
                    await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                    return;
                }
                if (data.StartsWith("state_research_branch_pick_", StringComparison.OrdinalIgnoreCase))
                {
                    string branchKey = data.Substring("state_research_branch_pick_".Length);
                    string branch = DecodeKey(branchKey);
                    await HandleResearchInput(botClient, userId, branch, cancellationToken);
                    await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                    return;
                }
                if (data.StartsWith("state_research_effecttype_", StringComparison.OrdinalIgnoreCase))
                {
                    string effect = data.Substring("state_research_effecttype_".Length);
                    await HandleResearchInput(botClient, userId, effect, cancellationToken);
                    await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                    return;
                }
                if (data.Equals("state_research_create_back", StringComparison.OrdinalIgnoreCase))
                {
                    await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id, "Секція в розробці");
                    return;
                }
                if (data.StartsWith("state_research_req_branch_", StringComparison.OrdinalIgnoreCase))
                {
                    string branchKey = data.Substring("state_research_req_branch_".Length);
                    await ShowResearchRequirementsList(botClient, userId, branchKey, 0, cancellationToken);
                    await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                    return;
                }
                if (data.StartsWith("state_research_req_page_", StringComparison.OrdinalIgnoreCase))
                {
                    var prefix = "state_research_req_page_";
                    string rest = data.Substring(prefix.Length);
                    int idx = rest.LastIndexOf('_');
                    if (idx > 0 && int.TryParse(rest.Substring(idx + 1), out int page))
                    {
                        string branchKey = rest.Substring(0, idx);
                        await ShowResearchRequirementsList(botClient, userId, branchKey, page, cancellationToken);
                        await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                        return;
                    }
                }
                if (data.StartsWith("state_research_req_pick_", StringComparison.OrdinalIgnoreCase))
                {
                    var prefix = "state_research_req_pick_";
                    string rest = data.Substring(prefix.Length);
                    int idx1 = rest.IndexOf('_');
                    if (idx1 > 0 && int.TryParse(rest.Substring(0, idx1), out int rid))
                    {
                        string rest2 = rest.Substring(idx1 + 1);
                        int idx2 = rest2.LastIndexOf('_');
                        if (idx2 > 0 && int.TryParse(rest2.Substring(idx2 + 1), out int page))
                        {
                            string branchKey = rest2.Substring(0, idx2);
                            ToggleResearchRequirement(userId, rid);
                            await ShowResearchRequirementsList(botClient, userId, branchKey, page, cancellationToken);
                            await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                            return;
                        }
                    }
                }
                if (data.Equals("state_research_req_done", StringComparison.OrdinalIgnoreCase))
                {
                    await FinishResearchRequirements(botClient, userId, cancellationToken);
                    await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                    return;
                }
                if (data.Equals("state_research_req_clear", StringComparison.OrdinalIgnoreCase))
                {
                    if (ResearchInputContext.TryGetValue(userId, out var ctx))
                        ctx.Requirements.Clear();
                    await ShowResearchRequirementsBranches(botClient, userId, cancellationToken);
                    await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                    return;
                }
                if (data.Equals("state_research_req_back", StringComparison.OrdinalIgnoreCase))
                {
                    await ShowResearchRequirementsBranches(botClient, userId, cancellationToken);
                    await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                    return;
                }

            }

            if (data == "state_overview")
            {
                await ShowOverviewMessage(botClient, chatId, userId, msgId, cancellationToken);
                await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                return;
            }

            if (data == "state_diplomacy")
            {
                await ShowDiplomacyMenu(botClient, userId, callbackQuery.Message.Chat.Id, msgId, 0, cancellationToken);
                await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                return;
            }

            if (data.StartsWith("state_diplomacy_page_", StringComparison.OrdinalIgnoreCase))
            {
                if (TryParseTrailingId(data, out int page))
                {
                    await ShowDiplomacyMenu(botClient, userId, callbackQuery.Message.Chat.Id, msgId, page, cancellationToken);
                    await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                    return;
                }
            }

            if (data.StartsWith("state_diplomacy_select_", StringComparison.OrdinalIgnoreCase))
            {
                if (TryParseTrailingId(data, out int targetId))
                {
                    await ShowDiplomacyRelation(botClient, userId, callbackQuery.Message.Chat.Id, msgId, targetId, cancellationToken);
                    await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                    return;
                }
            }

            if (data.StartsWith("state_diplomacy_action_", StringComparison.OrdinalIgnoreCase))
            {
                const string prefix = "state_diplomacy_action_";
                string rest = data.Substring(prefix.Length);
                int idx = rest.LastIndexOf('_');
                if (idx > 0 && int.TryParse(rest[(idx + 1)..], out int targetId))
                {
                    string token = rest.Substring(0, idx);
                    if (TryParseDiplomacyAction(token, out var action))
                    {
                        await ApplyDiplomacyAction(botClient, userId, callbackQuery.Message.Chat.Id, msgId, targetId, action, cancellationToken);
                        await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                        return;
                    }
                }
            }

            if (data.StartsWith("state_diplomacy_state_", StringComparison.OrdinalIgnoreCase))
            {
                if (TryParseTrailingId(data, out int targetId))
                {
                    await ShowDiplomacyStateSelect(botClient, userId, callbackQuery.Message.Chat.Id, msgId, targetId, cancellationToken);
                    await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                    return;
                }
            }

            if (data.StartsWith("state_diplomacy_setstate_", StringComparison.OrdinalIgnoreCase))
            {
                const string prefix = "state_diplomacy_setstate_";
                string rest = data.Substring(prefix.Length);
                int idx = rest.LastIndexOf('_');
                if (idx > 0 && int.TryParse(rest.Substring(0, idx), out int targetId))
                {
                    string stateToken = rest[(idx + 1)..];
                    await ApplyDiplomacyStateSet(botClient, userId, callbackQuery.Message.Chat.Id, msgId, targetId, stateToken, cancellationToken);
                    await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                    return;
                }
            }

            if (data == "state_economy")
            {
                await ShowEconomyMenu(botClient, userId, callbackQuery.Message.Chat.Id, msgId, cancellationToken);
                await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                return;
            }

            if (data == "state_public")
            {
                await ShowPublicSystemsMenu(botClient, userId, callbackQuery.Message.Chat.Id, msgId, cancellationToken);
                await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                return;
            }

            if (data == "state_public_report")
            {
                await ShowPublicSystemsReport(botClient, userId, callbackQuery.Message.Chat.Id, msgId, cancellationToken);
                await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                return;
            }

            if (data == "state_public_budget")
            {
                await ShowPublicSystemsBudget(botClient, userId, callbackQuery.Message.Chat.Id, msgId, cancellationToken);
                await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                return;
            }

            if (data == "state_public_budget_health")
            {
                await StartPublicBudgetInput(
                    botClient,
                    userId,
                    callbackQuery.Message.Chat.Id,
                    msgId,
                    EconomyInputMode.PublicBudgetHealth,
                    "Вкажіть бюджет МОЗ за тік (невід'ємне число).",
                    PublicRouteLegacyBudget,
                    cancellationToken);
                await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                return;
            }

            if (data == "state_public_budget_education")
            {
                await StartPublicBudgetInput(
                    botClient,
                    userId,
                    callbackQuery.Message.Chat.Id,
                    msgId,
                    EconomyInputMode.PublicBudgetEducation,
                    "Вкажіть бюджет МОН за тік (невід'ємне число).",
                    PublicRouteLegacyBudget,
                    cancellationToken);
                await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                return;
            }

            if (data == "state_public_budget_intl")
            {
                await StartPublicBudgetInput(
                    botClient,
                    userId,
                    callbackQuery.Message.Chat.Id,
                    msgId,
                    EconomyInputMode.PublicBudgetIntl,
                    "Вкажіть бюджет міжнародних програм за тік (невід'ємне число).",
                    PublicRouteLegacyBudget,
                    cancellationToken);
                await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                return;
            }

            if (data == "state_public_budget_intl_mode")
            {
                await TogglePublicIntlBudgetMode(botClient, userId, callbackQuery.Message.Chat.Id, msgId, cancellationToken);
                await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                return;
            }

            if (data == "state_public_actions")
            {
                await ShowPublicSystemsActions(botClient, userId, callbackQuery.Message.Chat.Id, msgId, cancellationToken);
                await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                return;
            }

            if (data == "state_public_action_quarantine_toggle")
            {
                await TogglePublicQuarantine(botClient, userId, callbackQuery.Message.Chat.Id, msgId, cancellationToken);
                await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                return;
            }

            if (data == "state_public_action_medic_mobilize")
            {
                await ApplyPublicMedicMobilization(botClient, userId, callbackQuery.Message.Chat.Id, msgId, cancellationToken);
                await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                return;
            }

            if (data == "state_public_action_trust_campaign")
            {
                await ApplyPublicTrustCampaign(botClient, userId, callbackQuery.Message.Chat.Id, msgId, cancellationToken);
                await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                return;
            }

            if (data == "state_public_action_vaccination")
            {
                await ApplyPublicVaccination(botClient, userId, callbackQuery.Message.Chat.Id, msgId, cancellationToken);
                await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                return;
            }

            if (data == "state_public_action_wastewater")
            {
                await ApplyPublicWastewaterSurveillance(botClient, userId, callbackQuery.Message.Chat.Id, msgId, cancellationToken);
                await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                return;
            }

            if (data == "state_public_action_telemed")
            {
                await ApplyPublicTelemedicineTriage(botClient, userId, callbackQuery.Message.Chat.Id, msgId, cancellationToken);
                await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                return;
            }

            if (data == "state_public_action_drug_research")
            {
                await ApplyPublicDrugResearchProject(botClient, userId, callbackQuery.Message.Chat.Id, msgId, cancellationToken);
                await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                return;
            }

            if (data == "state_public_action_doctor_training")
            {
                await ApplyPublicDoctorTraining(botClient, userId, callbackQuery.Message.Chat.Id, msgId, cancellationToken);
                await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                return;
            }

            if (data == "state_public_action_clinical_trials")
            {
                await ApplyPublicClinicalTrials(botClient, userId, callbackQuery.Message.Chat.Id, msgId, cancellationToken);
                await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                return;
            }

            if (data == "state_public_diseases")
            {
                await ShowPublicDiseases(botClient, userId, callbackQuery.Message.Chat.Id, msgId, cancellationToken);
                await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                return;
            }

            if (data == "state_public_gm_spawn_help")
            {
                await ShowPublicGmSpawnHelp(botClient, userId, callbackQuery.Message.Chat.Id, msgId, cancellationToken);
                await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                return;
            }

            if (data == "state_public_gm_end_all")
            {
                await EndAllPublicDiseases(botClient, userId, callbackQuery.Message.Chat.Id, msgId, cancellationToken);
                await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                return;
            }

            if (data.StartsWith("state_public_disease_shift_", StringComparison.OrdinalIgnoreCase))
            {
                const string prefix = "state_public_disease_shift_";
                string rest = data.Substring(prefix.Length);
                int split = rest.LastIndexOf('_');
                if (split > 0 &&
                    int.TryParse(rest.Substring(0, split), NumberStyles.Integer, CultureInfo.InvariantCulture, out int diseaseId) &&
                    int.TryParse(rest.Substring(split + 1), NumberStyles.Integer, CultureInfo.InvariantCulture, out int delta))
                {
                    await ShiftPublicDiseaseStage(botClient, userId, callbackQuery.Message.Chat.Id, msgId, diseaseId, delta, cancellationToken);
                }
                await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                return;
            }

            if (data.StartsWith("state_public_disease_end_", StringComparison.OrdinalIgnoreCase))
            {
                if (TryParseTrailingId(data, out int diseaseId))
                    await EndPublicDisease(botClient, userId, callbackQuery.Message.Chat.Id, msgId, diseaseId, cancellationToken);
                await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                return;
            }

            if (data.StartsWith("state_public_disease_", StringComparison.OrdinalIgnoreCase))
            {
                if (TryParseTrailingId(data, out int diseaseId))
                    await ShowPublicDiseaseInfo(botClient, userId, callbackQuery.Message.Chat.Id, msgId, diseaseId, cancellationToken);
                await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                return;
            }

            if (data == "state_public_intl")
            {
                await ShowPublicIntlPrograms(botClient, userId, callbackQuery.Message.Chat.Id, msgId, cancellationToken);
                await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                return;
            }

            if (data.StartsWith("state_public_intl_toggle_", StringComparison.OrdinalIgnoreCase))
            {
                if (TryParseTrailingId(data, out int intlProgramId))
                    await TogglePublicIntlProgram(botClient, userId, callbackQuery.Message.Chat.Id, msgId, intlProgramId, cancellationToken);
                await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                return;
            }

            if (data.StartsWith("state_public_intl_add_", StringComparison.OrdinalIgnoreCase))
            {
                string token = data.Substring("state_public_intl_add_".Length);
                await AddPublicIntlProgram(botClient, userId, callbackQuery.Message.Chat.Id, msgId, token, cancellationToken);
                await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                return;
            }

            if (data == "state_economy_master_balance")
            {
                await ShowEconomyMasterBalancingMenu(botClient, userId, callbackQuery.Message.Chat.Id, msgId, cancellationToken);
                await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                return;
            }

            if (data == "state_economy_rebalance")
            {
                await ShowEconomyProductionBalanceConfirm(botClient, userId, callbackQuery.Message.Chat.Id, msgId, cancellationToken);
                await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                return;
            }

            if (data == "state_economy_rebalance_confirm_yes")
            {
                await ApplyEconomyProductionBalance(botClient, userId, callbackQuery.Message.Chat.Id, msgId, true, cancellationToken);
                await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                return;
            }

            if (data == "state_economy_rebalance_confirm_no")
            {
                await ApplyEconomyProductionBalance(botClient, userId, callbackQuery.Message.Chat.Id, msgId, false, cancellationToken);
                await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                return;
            }

            if (data == "state_economy_rebalance_luxury")
            {
                await ShowEconomyLuxuryProductionBalanceConfirm(botClient, userId, callbackQuery.Message.Chat.Id, msgId, cancellationToken);
                await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                return;
            }

            if (data == "state_economy_rebalance_luxury_confirm_yes")
            {
                await ApplyEconomyLuxuryProductionBalance(botClient, userId, callbackQuery.Message.Chat.Id, msgId, true, cancellationToken);
                await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                return;
            }

            if (data == "state_economy_rebalance_luxury_confirm_no")
            {
                await ApplyEconomyLuxuryProductionBalance(botClient, userId, callbackQuery.Message.Chat.Id, msgId, false, cancellationToken);
                await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                return;
            }

            if (data == "state_economy_leader")
            {
                await ShowEconomyLeaderResourceMenu(botClient, userId, callbackQuery.Message.Chat.Id, msgId, cancellationToken);
                await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                return;
            }

            if (data.StartsWith("state_economy_leader_set_", StringComparison.OrdinalIgnoreCase))
            {
                string token = data.Substring("state_economy_leader_set_".Length);
                await SetEconomyLeaderResource(botClient, userId, callbackQuery.Message.Chat.Id, msgId, token, cancellationToken);
                await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                return;
            }

            if (data == "state_economy_income_limit")
            {
                await ShowEconomyIncomeLimitMenu(botClient, userId, callbackQuery.Message.Chat.Id, msgId, cancellationToken);
                await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                return;
            }

            if (data == "state_economy_income_limit_custom")
            {
                await StartEconomyIncomeLimitInput(botClient, userId, callbackQuery.Message.Chat.Id, msgId, cancellationToken);
                await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                return;
            }

            if (data == "state_economy_income_limit_disable")
            {
                await DisableEconomyIncomeLimit(botClient, userId, callbackQuery.Message.Chat.Id, msgId, cancellationToken);
                await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                return;
            }

            if (data.StartsWith("state_economy_income_limit_set_", StringComparison.OrdinalIgnoreCase))
            {
                string rawTarget = data.Substring("state_economy_income_limit_set_".Length);
                if (decimal.TryParse(rawTarget, NumberStyles.Any, CultureInfo.InvariantCulture, out var targetDaily))
                {
                    await SetEconomyIncomeLimitPreset(botClient, userId, callbackQuery.Message.Chat.Id, msgId, targetDaily, cancellationToken);
                    await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                    return;
                }
            }

            if (data == "state_country_effects")
            {
                await ShowCountryEffectsMenu(botClient, userId, callbackQuery.Message.Chat.Id, msgId, cancellationToken);
                await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                return;
            }

            if (data.StartsWith("state_country_effects_confirm_collapse_", StringComparison.OrdinalIgnoreCase))
            {
                string choice = data.Substring("state_country_effects_confirm_collapse_".Length);
                if (choice.Equals("yes", StringComparison.OrdinalIgnoreCase))
                {
                    await ApplyCountryEffectsAction(botClient, userId, callbackQuery.Message.Chat.Id, msgId, "toggle_collapse_confirmed", cancellationToken);
                }
                else
                {
                    await ShowCountryEffectsMenu(botClient, userId, callbackQuery.Message.Chat.Id, msgId, cancellationToken);
                }
                await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                return;
            }

            if (data.StartsWith("state_country_effects_", StringComparison.OrdinalIgnoreCase))
            {
                string action = data.Substring("state_country_effects_".Length);
                await ApplyCountryEffectsAction(botClient, userId, callbackQuery.Message.Chat.Id, msgId, action, cancellationToken);
                await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                return;
            }

            if (data == "state_economy_currency")
            {
                await ShowEconomyCurrencyMenu(botClient, userId, callbackQuery.Message.Chat.Id, msgId, cancellationToken);
                await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                return;
            }

            if (data == "state_economy_currency_rate")
            {
                await ShowEconomyCurrencyRate(botClient, userId, callbackQuery.Message.Chat.Id, msgId, cancellationToken);
                await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                return;
            }

            if (data == "state_economy_currency_black")
            {
                await ShowEconomyCurrencyBlackMarket(botClient, userId, callbackQuery.Message.Chat.Id, msgId, cancellationToken);
                await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                return;
            }

            if (data == "state_economy_currency_central")
            {
                await ShowEconomyCurrencyCentralBank(botClient, userId, callbackQuery.Message.Chat.Id, msgId, cancellationToken);
                await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                return;
            }

            if (data == "state_economy_currency_inflation")
            {
                await ShowEconomyCurrencyInflation(botClient, userId, callbackQuery.Message.Chat.Id, msgId, cancellationToken);
                await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                return;
            }

            if (data.StartsWith("state_economy_currency_inflation_shift_", StringComparison.OrdinalIgnoreCase))
            {
                string rawDelta = data.Substring("state_economy_currency_inflation_shift_".Length);
                if (int.TryParse(rawDelta, NumberStyles.Integer, CultureInfo.InvariantCulture, out int delta))
                {
                    await ShiftEconomyInflation(botClient, userId, callbackQuery.Message.Chat.Id, msgId, delta, cancellationToken);
                    await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                    return;
                }
            }

            if (data.StartsWith("state_economy_currency_inflation_set_", StringComparison.OrdinalIgnoreCase))
            {
                string rawValue = data.Substring("state_economy_currency_inflation_set_".Length);
                if (float.TryParse(rawValue.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out float target))
                {
                    await SetEconomyInflation(botClient, userId, callbackQuery.Message.Chat.Id, msgId, target, cancellationToken);
                    await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                    return;
                }
            }

            if (data == "state_economy_currency_convert")
            {
                await StartCurrencyConvertInput(botClient, userId, callbackQuery.Message.Chat.Id, msgId, cancellationToken);
                await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                return;
            }

            if (data.StartsWith("state_economy_currency_from_", StringComparison.OrdinalIgnoreCase) ||
                data.StartsWith("state_economy_currency_to_", StringComparison.OrdinalIgnoreCase))
            {
                await HandleCurrencyConvertSelection(botClient, userId, callbackQuery.Message.Chat.Id, msgId, data, cancellationToken);
                await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                return;
            }

            if (data.StartsWith("state_economy_currency_action_", StringComparison.OrdinalIgnoreCase))
            {
                await HandleCurrencyActionSelection(botClient, userId, callbackQuery.Message.Chat.Id, msgId, data, cancellationToken);
                await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                return;
            }

            if (data.StartsWith("state_economy_loanapprove_", StringComparison.OrdinalIgnoreCase))
            {
                await HandleLoanApprovalDecision(botClient, callbackQuery, data, cancellationToken);
                return;
            }

            if (data.StartsWith("state_economy_tradeapprove_", StringComparison.OrdinalIgnoreCase))
            {
                await HandleTradeApprovalDecision(botClient, callbackQuery, data, cancellationToken);
                return;
            }

            if (data == "state_economy_currency_transfer")
            {
                await StartCurrencyTransferInput(botClient, userId, callbackQuery.Message.Chat.Id, msgId, cancellationToken);
                await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                return;
            }

            if (data == "state_economy_currency_adjust")
            {
                await StartCurrencyAdjustInput(botClient, userId, callbackQuery.Message.Chat.Id, msgId, cancellationToken);
                await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                return;
            }

            if (data == "state_economy_currency_loan")
            {
                await StartCurrencyLoanInput(botClient, userId, callbackQuery.Message.Chat.Id, msgId, cancellationToken);
                await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                return;
            }

            if (data == "state_economy_currency_repay")
            {
                await StartCurrencyRepayInput(botClient, userId, callbackQuery.Message.Chat.Id, msgId, cancellationToken);
                await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                return;
            }

            if (data == "state_economy_report")
            {
                await ShowEconomyReport(botClient, userId, callbackQuery.Message.Chat.Id, msgId, cancellationToken);
                await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                return;
            }

            if (data == "state_economy_report_sectors")
            {
                await ShowEconomySectorReport(botClient, userId, callbackQuery.Message.Chat.Id, msgId, cancellationToken);
                await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                return;
            }

            if (data.StartsWith("state_economy_report_sector_select_", StringComparison.OrdinalIgnoreCase))
            {
                string token = data.Substring("state_economy_report_sector_select_".Length);
                await SelectEconomySectorReport(botClient, userId, callbackQuery.Message.Chat.Id, msgId, token, cancellationToken);
                await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                return;
            }

            if (data == "state_economy_report_sector_income_edit")
            {
                await StartEconomySectorRevenueInput(botClient, userId, callbackQuery.Message.Chat.Id, msgId, cancellationToken);
                await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                return;
            }

            if (data == "state_economy_report_sector_tax_edit")
            {
                await StartEconomySectorTaxInput(botClient, userId, callbackQuery.Message.Chat.Id, msgId, cancellationToken);
                await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                return;
            }

            if (data == "state_economy_report_sector_reset")
            {
                await ResetEconomySectorSettings(botClient, userId, callbackQuery.Message.Chat.Id, msgId, cancellationToken);
                await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                return;
            }

            if (data == "state_economy_warnings")
            {
                await ShowEconomyWarnings(botClient, userId, callbackQuery.Message.Chat.Id, msgId, cancellationToken);
                await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                return;
            }

            if (data == "state_economy_preview")
            {
                await ShowEconomyPreview(botClient, userId, callbackQuery.Message.Chat.Id, msgId, cancellationToken);
                await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                return;
            }

            if (data == "state_economy_preview_days")
            {
                await StartEconomyPreviewDaysInput(botClient, userId, callbackQuery.Message.Chat.Id, msgId, cancellationToken);
                await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                return;
            }

            if (data == "state_economy_breakdown_income")
            {
                await ShowEconomyBreakdown(botClient, userId, callbackQuery.Message.Chat.Id, msgId, "income", cancellationToken);
                await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                return;
            }

            if (data == "state_economy_breakdown_expenses")
            {
                await ShowEconomyBreakdown(botClient, userId, callbackQuery.Message.Chat.Id, msgId, "expenses", cancellationToken);
                await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                return;
            }

            if (data == "state_economy_breakdown_stability")
            {
                await ShowEconomyBreakdown(botClient, userId, callbackQuery.Message.Chat.Id, msgId, "stability", cancellationToken);
                await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                return;
            }

            if (data == "state_economy_breakdown_unemployment")
            {
                await ShowEconomyBreakdown(botClient, userId, callbackQuery.Message.Chat.Id, msgId, "unemployment", cancellationToken);
                await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                return;
            }

            if (data == "state_economy_labor")
            {
                await ShowEconomyLaborMarket(botClient, userId, callbackQuery.Message.Chat.Id, msgId, cancellationToken);
                await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                return;
            }

            if (data == "state_economy_companies")
            {
                await ShowEconomyCompaniesOverview(botClient, userId, callbackQuery.Message.Chat.Id, msgId, cancellationToken);
                await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                return;
            }

            if (data == "state_economy_companies_list")
            {
                await ShowEconomyCompaniesList(botClient, userId, callbackQuery.Message.Chat.Id, msgId, 0, cancellationToken);
                await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                return;
            }

            if (data.StartsWith("state_economy_companies_page_", StringComparison.OrdinalIgnoreCase))
            {
                int page = 0;
                if (TryParseTrailingId(data, out int parsed))
                    page = parsed;
                await ShowEconomyCompaniesList(botClient, userId, callbackQuery.Message.Chat.Id, msgId, page, cancellationToken);
                await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                return;
            }

            if (data.StartsWith("state_economy_companyinfo_", StringComparison.OrdinalIgnoreCase))
            {
                if (TryParseTrailingId(data, out int companyId))
                {
                    await ShowEconomyCompanyInfo(botClient, userId, callbackQuery.Message.Chat.Id, msgId, companyId, cancellationToken);
                }
                await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                return;
            }

            if (data == "state_economy_company_add")
            {
                await StartCompanyCreation(botClient, userId, cancellationToken);
                await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                return;
            }

            if (data.StartsWith("state_economy_company_edit_", StringComparison.OrdinalIgnoreCase))
            {
                if (TryParseTrailingId(data, out int companyId))
                {
                    await ShowEconomyCompanyEditMenu(botClient, userId, callbackQuery.Message.Chat.Id, msgId, companyId, cancellationToken);
                }
                await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                return;
            }

            if (data.StartsWith("state_economy_company_editfield_", StringComparison.OrdinalIgnoreCase))
            {
                await StartEditField(botClient, userId, data, "company", cancellationToken);
                await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                return;
            }

            if (data.StartsWith("state_economy_company_delete_", StringComparison.OrdinalIgnoreCase))
            {
                if (TryParseTrailingId(data, out int companyId))
                {
                    await DeleteEconomyCompany(botClient, userId, callbackQuery.Message.Chat.Id, msgId, companyId, cancellationToken);
                }
                await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                return;
            }

            if (data == "state_economy_companies_budget")
            {
                await ShowEconomyCompaniesBudget(botClient, userId, callbackQuery.Message.Chat.Id, msgId, cancellationToken);
                await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                return;
            }

            if (data.StartsWith("state_economy_company_buildings_page|", StringComparison.OrdinalIgnoreCase))
            {
                var partsCompanyPage = data.Split('|');
                if (partsCompanyPage.Length >= 3 &&
                    int.TryParse(partsCompanyPage[1], out int companyId) &&
                    int.TryParse(partsCompanyPage[2], out int page))
                {
                    await ShowEconomyCompanyBuildingsAssignment(botClient, userId, callbackQuery.Message.Chat.Id, msgId, companyId, page, cancellationToken);
                }
                await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                return;
            }

            if (data.StartsWith("state_economy_company_building_toggle|", StringComparison.OrdinalIgnoreCase))
            {
                var partsToggle = data.Split('|');
                if (partsToggle.Length >= 4 &&
                    int.TryParse(partsToggle[1], out int companyId) &&
                    int.TryParse(partsToggle[2], out int buildingId) &&
                    int.TryParse(partsToggle[3], out int page))
                {
                    await ToggleEconomyCompanyBuildingAssignment(botClient, userId, callbackQuery.Message.Chat.Id, msgId, companyId, buildingId, page, cancellationToken);
                }
                await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                return;
            }

            if (data.StartsWith("state_economy_company_buildings_", StringComparison.OrdinalIgnoreCase))
            {
                if (TryParseTrailingId(data, out int companyId))
                {
                    await ShowEconomyCompanyBuildingsAssignment(botClient, userId, callbackQuery.Message.Chat.Id, msgId, companyId, 0, cancellationToken);
                }
                await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                return;
            }

            if (data == "state_economy_company_buildings")
            {
                await ShowEconomyCompanyBuildingsOverview(botClient, userId, callbackQuery.Message.Chat.Id, msgId, cancellationToken);
                await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                return;
            }

            if (data.StartsWith("state_military", StringComparison.OrdinalIgnoreCase))
            {
                if (!HasAdminAccess(userId))
                {
                    await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id, "Нема доступу");
                    return;
                }

                if (data == "state_military")
                {
                    await ShowMilitaryMenu(botClient, userId, callbackQuery.Message.Chat.Id, msgId, cancellationToken);
                    await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                    return;
                }

                if (data == "state_military_arsenal")
                {
                    await ShowMilitaryArsenalMenu(botClient, userId, callbackQuery.Message.Chat.Id, msgId, cancellationToken);
                    await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                    return;
                }

                if (data == "state_military_arsenal_stock")
                {
                    await ShowMilitaryArsenalStock(botClient, userId, callbackQuery.Message.Chat.Id, msgId, cancellationToken);
                    await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                    return;
                }

                if (data == "state_military_arsenal_procurement")
                {
                    await ShowMilitaryArsenalProcurement(botClient, userId, callbackQuery.Message.Chat.Id, msgId, 0, cancellationToken);
                    await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                    return;
                }

                if (data.StartsWith("state_military_arsenal_procurement_page_", StringComparison.OrdinalIgnoreCase))
                {
                    int page = 0;
                    if (TryParseTrailingId(data, out int parsed))
                        page = parsed;
                    await ShowMilitaryArsenalProcurement(botClient, userId, callbackQuery.Message.Chat.Id, msgId, page, cancellationToken);
                    await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                    return;
                }

                if (data.StartsWith("state_military_arsenal_offer|", StringComparison.OrdinalIgnoreCase))
                {
                    var partsOffer = data.Split('|');
                    if (partsOffer.Length >= 4 &&
                        int.TryParse(partsOffer[1], out int companyId) &&
                        int.TryParse(partsOffer[3], out int returnPage))
                    {
                        string resourceKey = partsOffer[2];
                        await ShowMilitaryArsenalOffer(botClient, userId, callbackQuery.Message.Chat.Id, msgId, companyId, resourceKey, returnPage, cancellationToken);
                    }
                    await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                    return;
                }

                if (data.StartsWith("state_military_arsenal_buy|", StringComparison.OrdinalIgnoreCase))
                {
                    var partsBuy = data.Split('|');
                    if (partsBuy.Length >= 5 &&
                        int.TryParse(partsBuy[1], out int companyId) &&
                        int.TryParse(partsBuy[3], out int percent) &&
                        int.TryParse(partsBuy[4], out int returnPage))
                    {
                        string resourceKey = partsBuy[2];
                        await BuyMilitaryArsenalOffer(botClient, userId, callbackQuery.Message.Chat.Id, msgId, companyId, resourceKey, percent, returnPage, cancellationToken);
                    }
                    await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                    return;
                }

                if (data == "state_military_arsenal_distribute_auto")
                {
                    await ApplyMilitaryArsenalAutoDistribution(botClient, userId, callbackQuery.Message.Chat.Id, msgId, cancellationToken);
                    await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                    return;
                }

                if (data == "state_military_arsenal_autobuild")
                {
                    await StartMilitaryArsenalAutoProvisionInput(botClient, userId, callbackQuery.Message.Chat.Id, msgId, cancellationToken);
                    await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                    return;
                }

                if (data == "state_military_arsenal_distribute_manual")
                {
                    await ShowMilitaryArsenalManualDistribution(botClient, userId, callbackQuery.Message.Chat.Id, msgId, 0, cancellationToken);
                    await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                    return;
                }

                if (data.StartsWith("state_military_arsenal_distribute_manual_page_", StringComparison.OrdinalIgnoreCase))
                {
                    int page = 0;
                    if (TryParseTrailingId(data, out int parsed))
                        page = parsed;
                    await ShowMilitaryArsenalManualDistribution(botClient, userId, callbackQuery.Message.Chat.Id, msgId, page, cancellationToken);
                    await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                    return;
                }

                if (data.StartsWith("state_military_arsenal_unit_apply|", StringComparison.OrdinalIgnoreCase))
                {
                    var partsUnitApply = data.Split('|');
                    if (partsUnitApply.Length >= 4 &&
                        int.TryParse(partsUnitApply[1], out int unitId) &&
                        int.TryParse(partsUnitApply[2], out int sharePercent) &&
                        int.TryParse(partsUnitApply[3], out int returnPage))
                    {
                        await ApplyMilitaryArsenalUnitDistribution(botClient, userId, callbackQuery.Message.Chat.Id, msgId, unitId, sharePercent, returnPage, cancellationToken);
                    }
                    await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                    return;
                }

                if (data.StartsWith("state_military_arsenal_unit|", StringComparison.OrdinalIgnoreCase))
                {
                    var partsUnit = data.Split('|');
                    if (partsUnit.Length >= 3 &&
                        int.TryParse(partsUnit[1], out int unitId) &&
                        int.TryParse(partsUnit[2], out int returnPage))
                    {
                        await ShowMilitaryArsenalUnitDistribution(botClient, userId, callbackQuery.Message.Chat.Id, msgId, unitId, returnPage, cancellationToken);
                    }
                    await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                    return;
                }

                if (data == "state_military_arsenal_company_quick")
                {
                    await CreateMilitaryArsenalCompanyQuick(botClient, userId, callbackQuery.Message.Chat.Id, msgId, cancellationToken);
                    await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                    return;
                }

                if (data == "state_military_state_orders")
                {
                    await ShowMilitaryStateOrders(botClient, userId, callbackQuery.Message.Chat.Id, msgId, cancellationToken);
                    await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                    return;
                }

                if (data.StartsWith("state_military_stateorder_set_", StringComparison.OrdinalIgnoreCase))
                {
                    if (TryParseTrailingId(data, out int sharePercent))
                    {
                        await ApplyMilitaryStateOrderShare(botClient, userId, callbackQuery.Message.Chat.Id, msgId, sharePercent, cancellationToken);
                    }
                    await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                    return;
                }

                if (data == "state_military_battle")
                {
                    await ShowMilitaryBattleMenu(botClient, userId, callbackQuery.Message.Chat.Id, msgId, cancellationToken);
                    await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                    return;
                }

                if (data == "state_military_intel")
                {
                    await ShowMilitaryIntelMenu(botClient, userId, callbackQuery.Message.Chat.Id, msgId, cancellationToken);
                    await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                    return;
                }

                if (data == "state_military_intel_target")
                {
                    await ShowMilitaryIntelTargetSelect(botClient, userId, callbackQuery.Message.Chat.Id, msgId, 0, cancellationToken);
                    await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                    return;
                }

                if (data.StartsWith("state_military_intel_target_page_", StringComparison.OrdinalIgnoreCase))
                {
                    int page = 0;
                    if (TryParseTrailingId(data, out int parsed))
                        page = parsed;
                    await ShowMilitaryIntelTargetSelect(botClient, userId, callbackQuery.Message.Chat.Id, msgId, page, cancellationToken);
                    await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                    return;
                }

                if (data.StartsWith("state_military_intel_target_pick_", StringComparison.OrdinalIgnoreCase))
                {
                    if (TryParseTrailingId(data, out int targetCountryId))
                        await SetMilitaryIntelTarget(botClient, userId, callbackQuery.Message.Chat.Id, msgId, targetCountryId, cancellationToken);
                    await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                    return;
                }

                if (data == "state_military_intel_operation")
                {
                    await ShowMilitaryIntelOperationMenu(botClient, userId, callbackQuery.Message.Chat.Id, msgId, cancellationToken);
                    await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                    return;
                }

                if (data.StartsWith("state_military_intel_operation_set_", StringComparison.OrdinalIgnoreCase))
                {
                    const string prefix = "state_military_intel_operation_set_";
                    string operationType = data.Substring(prefix.Length);
                    await SetMilitaryIntelOperation(botClient, userId, callbackQuery.Message.Chat.Id, msgId, operationType, cancellationToken);
                    await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                    return;
                }

                if (data.StartsWith("state_military_intel_budget_set_", StringComparison.OrdinalIgnoreCase))
                {
                    if (TryParseTrailingId(data, out int budgetGold))
                        await SetMilitaryIntelBudget(botClient, userId, callbackQuery.Message.Chat.Id, msgId, budgetGold, cancellationToken);
                    await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                    return;
                }

                if (data == "state_military_intel_execute")
                {
                    await ExecuteMilitaryIntelOperation(botClient, userId, callbackQuery.Message.Chat.Id, msgId, cancellationToken);
                    await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                    return;
                }

                if (data == "state_military_intel_logs")
                {
                    await ShowMilitaryIntelLogs(botClient, userId, callbackQuery.Message.Chat.Id, msgId, 0, cancellationToken);
                    await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                    return;
                }

                if (data.StartsWith("state_military_intel_logs_page_", StringComparison.OrdinalIgnoreCase))
                {
                    int page = 0;
                    if (TryParseTrailingId(data, out int parsed))
                        page = parsed;
                    await ShowMilitaryIntelLogs(botClient, userId, callbackQuery.Message.Chat.Id, msgId, page, cancellationToken);
                    await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                    return;
                }

                if (data.StartsWith("state_military_intel_log_", StringComparison.OrdinalIgnoreCase))
                {
                    if (TryParseTrailingId(data, out int intelLogId))
                        await ShowMilitaryIntelLogInfo(botClient, userId, callbackQuery.Message.Chat.Id, msgId, intelLogId, cancellationToken);
                    await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                    return;
                }

                if (data == "state_military_battle_target")
                {
                    await ShowMilitaryBattleTargetSelect(botClient, userId, callbackQuery.Message.Chat.Id, msgId, 0, cancellationToken);
                    await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                    return;
                }

                if (data.StartsWith("state_military_battle_target_page_", StringComparison.OrdinalIgnoreCase))
                {
                    int page = 0;
                    if (TryParseTrailingId(data, out int parsed))
                        page = parsed;
                    await ShowMilitaryBattleTargetSelect(botClient, userId, callbackQuery.Message.Chat.Id, msgId, page, cancellationToken);
                    await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                    return;
                }

                if (data.StartsWith("state_military_battle_target_pick_", StringComparison.OrdinalIgnoreCase))
                {
                    if (TryParseTrailingId(data, out int targetCountryId))
                        await SetMilitaryBattleTarget(botClient, userId, callbackQuery.Message.Chat.Id, msgId, targetCountryId, cancellationToken);
                    await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                    return;
                }

                if (data == "state_military_battle_terrain")
                {
                    await ShowMilitaryBattleTerrainMenu(botClient, userId, callbackQuery.Message.Chat.Id, msgId, cancellationToken);
                    await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                    return;
                }

                if (data.StartsWith("state_military_battle_terrain_set_", StringComparison.OrdinalIgnoreCase))
                {
                    const string prefix = "state_military_battle_terrain_set_";
                    string terrain = data.Substring(prefix.Length);
                    await SetMilitaryBattleTerrain(botClient, userId, callbackQuery.Message.Chat.Id, msgId, terrain, cancellationToken);
                    await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                    return;
                }

                if (data == "state_military_battle_tactic")
                {
                    await ShowMilitaryBattleTacticMenu(botClient, userId, callbackQuery.Message.Chat.Id, msgId, cancellationToken);
                    await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                    return;
                }

                if (data.StartsWith("state_military_battle_tactic_set_", StringComparison.OrdinalIgnoreCase))
                {
                    const string prefix = "state_military_battle_tactic_set_";
                    string tactic = data.Substring(prefix.Length);
                    await SetMilitaryBattleTactic(botClient, userId, callbackQuery.Message.Chat.Id, msgId, tactic, cancellationToken);
                    await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                    return;
                }

                if (data == "state_military_battle_toggle_vassals")
                {
                    await ToggleMilitaryBattleIncludeVassals(botClient, userId, callbackQuery.Message.Chat.Id, msgId, cancellationToken);
                    await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                    return;
                }

                if (data == "state_military_battle_engage")
                {
                    await ExecuteMilitaryBattle(botClient, userId, callbackQuery.Message.Chat.Id, msgId, retreat: false, cancellationToken);
                    await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                    return;
                }

                if (data == "state_military_battle_retreat")
                {
                    await ExecuteMilitaryBattle(botClient, userId, callbackQuery.Message.Chat.Id, msgId, retreat: true, cancellationToken);
                    await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                    return;
                }

                if (data == "state_military_battle_logs")
                {
                    await ShowMilitaryBattleLogs(botClient, userId, callbackQuery.Message.Chat.Id, msgId, 0, cancellationToken);
                    await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                    return;
                }

                if (data.StartsWith("state_military_battle_logs_page_", StringComparison.OrdinalIgnoreCase))
                {
                    int page = 0;
                    if (TryParseTrailingId(data, out int parsed))
                        page = parsed;
                    await ShowMilitaryBattleLogs(botClient, userId, callbackQuery.Message.Chat.Id, msgId, page, cancellationToken);
                    await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                    return;
                }

                if (data.StartsWith("state_military_battle_log_", StringComparison.OrdinalIgnoreCase))
                {
                    if (TryParseTrailingId(data, out int battleLogId))
                        await ShowMilitaryBattleLogInfo(botClient, userId, callbackQuery.Message.Chat.Id, msgId, battleLogId, cancellationToken);
                    await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                    return;
                }

                if (data == "state_military_units")
                {
                    await ShowMilitaryUnitsMenu(botClient, userId, callbackQuery.Message.Chat.Id, msgId, cancellationToken);
                    await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                    return;
                }

                if (data == "state_military_units_list")
                {
                    await ShowMilitaryUnitsList(botClient, userId, callbackQuery.Message.Chat.Id, msgId, 0, cancellationToken);
                    await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                    return;
                }

                if (data.StartsWith("state_military_units_page_", StringComparison.OrdinalIgnoreCase))
                {
                    int page = 0;
                    if (TryParseTrailingId(data, out int parsed))
                        page = parsed;
                    await ShowMilitaryUnitsList(botClient, userId, callbackQuery.Message.Chat.Id, msgId, page, cancellationToken);
                    await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                    return;
                }

                if (data.StartsWith("state_military_unitinfo_", StringComparison.OrdinalIgnoreCase))
                {
                    if (TryParseTrailingId(data, out int unitId))
                        await ShowMilitaryUnitInfo(botClient, userId, callbackQuery.Message.Chat.Id, msgId, unitId, cancellationToken);
                    await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                    return;
                }

                if (data == "state_military_unit_add")
                {
                    await StartMilitaryUnitCreation(botClient, userId, cancellationToken);
                    await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                    return;
                }

                if (data == "state_military_unit_add_company")
                {
                    await CreateQuickCompanyUnit(botClient, userId, callbackQuery.Message.Chat.Id, msgId, cancellationToken);
                    await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                    return;
                }

                if (data == "state_military_unit_templates")
                {
                    await ShowMilitaryUnitTemplates(botClient, userId, callbackQuery.Message.Chat.Id, msgId, 0, cancellationToken);
                    await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                    return;
                }

                if (data.StartsWith("state_military_unit_templates_page_", StringComparison.OrdinalIgnoreCase))
                {
                    int page = 0;
                    if (TryParseTrailingId(data, out int parsed))
                        page = parsed;
                    await ShowMilitaryUnitTemplates(botClient, userId, callbackQuery.Message.Chat.Id, msgId, page, cancellationToken);
                    await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                    return;
                }

                if (data.StartsWith("state_military_unit_template_create_", StringComparison.OrdinalIgnoreCase))
                {
                    const string prefix = "state_military_unit_template_create_";
                    string key = data.Substring(prefix.Length);
                    await CreateMilitaryUnitFromTemplate(botClient, userId, callbackQuery.Message.Chat.Id, msgId, key, cancellationToken);
                    await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                    return;
                }

                if (data.StartsWith("state_military_unit_template_", StringComparison.OrdinalIgnoreCase))
                {
                    const string prefix = "state_military_unit_template_";
                    string key = data.Substring(prefix.Length);
                    await ShowMilitaryUnitTemplateInfo(botClient, userId, callbackQuery.Message.Chat.Id, msgId, key, cancellationToken);
                    await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                    return;
                }

                if (data.StartsWith("state_military_unit_editfield_", StringComparison.OrdinalIgnoreCase))
                {
                    await StartEditField(botClient, userId, data, "militaryunit", cancellationToken);
                    await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                    return;
                }

                if (data.StartsWith("state_military_unit_edit_", StringComparison.OrdinalIgnoreCase))
                {
                    if (TryParseTrailingId(data, out int unitId))
                        await ShowMilitaryUnitEditMenu(botClient, userId, callbackQuery.Message.Chat.Id, msgId, unitId, cancellationToken);
                    await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                    return;
                }

                if (data.StartsWith("state_military_unit_delete_", StringComparison.OrdinalIgnoreCase))
                {
                    if (TryParseTrailingId(data, out int unitId))
                        await DeleteMilitaryUnit(botClient, userId, callbackQuery.Message.Chat.Id, msgId, unitId, cancellationToken);
                    await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                    return;
                }

                if (data == "state_military_vassals")
                {
                    await ShowMilitaryVassalsMenu(botClient, userId, callbackQuery.Message.Chat.Id, msgId, cancellationToken);
                    await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                    return;
                }

                if (data == "state_military_vassals_list")
                {
                    await ShowMilitaryVassalsList(botClient, userId, callbackQuery.Message.Chat.Id, msgId, 0, cancellationToken);
                    await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                    return;
                }

                if (data.StartsWith("state_military_vassals_page_", StringComparison.OrdinalIgnoreCase))
                {
                    int page = 0;
                    if (TryParseTrailingId(data, out int parsed))
                        page = parsed;
                    await ShowMilitaryVassalsList(botClient, userId, callbackQuery.Message.Chat.Id, msgId, page, cancellationToken);
                    await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                    return;
                }

                if (data.StartsWith("state_military_vassalinfo_", StringComparison.OrdinalIgnoreCase))
                {
                    if (TryParseTrailingId(data, out int obligationId))
                        await ShowMilitaryVassalInfo(botClient, userId, callbackQuery.Message.Chat.Id, msgId, obligationId, cancellationToken);
                    await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                    return;
                }

                if (data == "state_military_vassal_add")
                {
                    await StartMilitaryVassalCreation(botClient, userId, cancellationToken);
                    await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                    return;
                }

                if (data == "state_military_vassal_add_country")
                {
                    await ShowMilitaryVassalCountrySelect(botClient, userId, callbackQuery.Message.Chat.Id, msgId, 0, cancellationToken);
                    await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                    return;
                }

                if (data.StartsWith("state_military_vassal_country_page_", StringComparison.OrdinalIgnoreCase))
                {
                    int page = 0;
                    if (TryParseTrailingId(data, out int parsed))
                        page = parsed;
                    await ShowMilitaryVassalCountrySelect(botClient, userId, callbackQuery.Message.Chat.Id, msgId, page, cancellationToken);
                    await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                    return;
                }

                if (data.StartsWith("state_military_vassal_pick_", StringComparison.OrdinalIgnoreCase))
                {
                    if (TryParseTrailingId(data, out int targetCountryId))
                        await AddMilitaryVassalFromCountry(botClient, userId, callbackQuery.Message.Chat.Id, msgId, targetCountryId, cancellationToken);
                    await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                    return;
                }

                if (data.StartsWith("state_military_vassal_editfield_", StringComparison.OrdinalIgnoreCase))
                {
                    await StartEditField(botClient, userId, data, "vassal", cancellationToken);
                    await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                    return;
                }

                if (data.StartsWith("state_military_vassal_edit_", StringComparison.OrdinalIgnoreCase))
                {
                    if (TryParseTrailingId(data, out int obligationId))
                        await ShowMilitaryVassalEditMenu(botClient, userId, callbackQuery.Message.Chat.Id, msgId, obligationId, cancellationToken);
                    await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                    return;
                }

                if (data.StartsWith("state_military_vassal_delete_", StringComparison.OrdinalIgnoreCase))
                {
                    if (TryParseTrailingId(data, out int obligationId))
                        await DeleteMilitaryVassal(botClient, userId, callbackQuery.Message.Chat.Id, msgId, obligationId, cancellationToken);
                    await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                    return;
                }

                if (data.StartsWith("state_military_vassal_toggle_", StringComparison.OrdinalIgnoreCase))
                {
                    if (TryParseTrailingId(data, out int obligationId))
                        await ToggleMilitaryVassalActive(botClient, userId, callbackQuery.Message.Chat.Id, msgId, obligationId, cancellationToken);
                    await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                    return;
                }
            }

            if (data == "state_economy_military_logistics")
            {
                await ShowEconomyMilitaryLogistics(botClient, userId, callbackQuery.Message.Chat.Id, msgId, cancellationToken);
                await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                return;
            }

            if (data == "state_economy_army_structure")
            {
                await ShowEconomyArmyStructure(botClient, userId, callbackQuery.Message.Chat.Id, msgId, cancellationToken);
                await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                return;
            }

            if (data == "state_economy_vassals")
            {
                await ShowEconomyVassalsOverview(botClient, userId, callbackQuery.Message.Chat.Id, msgId, cancellationToken);
                await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                return;
            }

            if (data == "state_economy_crisis_actions")
            {
                await ShowCrisisActions(botClient, userId, callbackQuery.Message.Chat.Id, msgId, cancellationToken);
                await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                return;
            }

            if (data.StartsWith("state_economy_crisis_action_", StringComparison.OrdinalIgnoreCase))
            {
                string action = data.Substring("state_economy_crisis_action_".Length);
                await ApplyCrisisAction(botClient, userId, callbackQuery.Message.Chat.Id, msgId, action, cancellationToken);
                await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                return;
            }

            if (data == "state_economy_resources_report")
            {
                await ShowEconomyResourcesFlowReport(botClient, userId, callbackQuery.Message.Chat.Id, msgId, cancellationToken);
                await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                return;
            }

            if (data == "state_economy_sanctions")
            {
                await ShowEconomySanctionsMenu(botClient, userId, callbackQuery.Message.Chat.Id, msgId, cancellationToken);
                await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                return;
            }

            if (data == "state_economy_sanctions_products")
            {
                await ShowEconomySanctionsProducts(botClient, userId, callbackQuery.Message.Chat.Id, msgId, 0, cancellationToken);
                await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                return;
            }

            if (data.StartsWith("state_economy_sanctions_products_page_", StringComparison.OrdinalIgnoreCase))
            {
                int page = 0;
                if (TryParseTrailingId(data, out int parsed))
                    page = parsed;
                await ShowEconomySanctionsProducts(botClient, userId, callbackQuery.Message.Chat.Id, msgId, page, cancellationToken);
                await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                return;
            }

            if (data.StartsWith("state_economy_sanctions_product_country_", StringComparison.OrdinalIgnoreCase))
            {
                if (TryParseTrailingId(data, out int targetCountryId))
                {
                    await ShowEconomySanctionsProductResources(botClient, userId, callbackQuery.Message.Chat.Id, msgId, targetCountryId, 0, cancellationToken);
                }
                await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                return;
            }

            if (data.StartsWith("state_economy_sanctions_product_resource_page|", StringComparison.OrdinalIgnoreCase))
            {
                var partsResPage = data.Split('|');
                if (partsResPage.Length >= 3 &&
                    int.TryParse(partsResPage[1], out int targetCountryId) &&
                    int.TryParse(partsResPage[2], out int page))
                {
                    await ShowEconomySanctionsProductResources(botClient, userId, callbackQuery.Message.Chat.Id, msgId, targetCountryId, page, cancellationToken);
                }
                await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                return;
            }

            if (data.StartsWith("state_economy_sanctions_product_all|", StringComparison.OrdinalIgnoreCase))
            {
                var partsAll = data.Split('|');
                if (partsAll.Length >= 2 &&
                    int.TryParse(partsAll[1], out int targetCountryId))
                {
                    await ShowEconomySanctionsTargetInfo(botClient, userId, callbackQuery.Message.Chat.Id, msgId, targetCountryId, cancellationToken);
                }
                await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                return;
            }

            if (data.StartsWith("state_economy_sanctions_product_resource|", StringComparison.OrdinalIgnoreCase))
            {
                var partsRes = data.Split('|');
                if (partsRes.Length >= 3 &&
                    int.TryParse(partsRes[1], out int targetCountryId))
                {
                    string resourceType = DecodeResourceKey(partsRes[2]);
                    await ShowEconomySanctionsProductInfo(botClient, userId, callbackQuery.Message.Chat.Id, msgId, targetCountryId, resourceType, cancellationToken);
                }
                await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                return;
            }

            if (data == "state_economy_sanctions_targets")
            {
                await ShowEconomySanctionsTargets(botClient, userId, callbackQuery.Message.Chat.Id, msgId, 0, cancellationToken);
                await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                return;
            }

            if (data.StartsWith("state_economy_sanctions_targets_page_", StringComparison.OrdinalIgnoreCase))
            {
                int page = 0;
                if (TryParseTrailingId(data, out int parsed))
                    page = parsed;
                await ShowEconomySanctionsTargets(botClient, userId, callbackQuery.Message.Chat.Id, msgId, page, cancellationToken);
                await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                return;
            }

            if (data.StartsWith("state_economy_sanctions_target_", StringComparison.OrdinalIgnoreCase))
            {
                if (TryParseTrailingId(data, out int targetCountryId))
                {
                    await ShowEconomySanctionsTargetInfo(botClient, userId, callbackQuery.Message.Chat.Id, msgId, targetCountryId, cancellationToken);
                    await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                    return;
                }
            }

            if (data.StartsWith("state_economy_sanctions_targetset|", StringComparison.OrdinalIgnoreCase))
            {
                var partsSet = data.Split('|');
                if (partsSet.Length >= 4 &&
                    int.TryParse(partsSet[1], out int targetCountryId) &&
                    int.TryParse(partsSet[3], out int level))
                {
                    bool isExport = partsSet[2].Equals("export", StringComparison.OrdinalIgnoreCase);
                    bool isImport = partsSet[2].Equals("import", StringComparison.OrdinalIgnoreCase);

                    if (isExport || isImport)
                    {
                        _logger.LogInformation("Sanctions target set: user={UserId} target={TargetId} side={Side} level={Level} data={Data}", userId, targetCountryId, isExport ? "export" : "import", level, data);
                        await SetEconomySanctionsTargetLevel(botClient, userId, callbackQuery.Message.Chat.Id, msgId, targetCountryId, level, isExport, cancellationToken);
                    }
                    else
                    {
                        _logger.LogInformation("Sanctions target set (legacy): user={UserId} target={TargetId} level={Level} data={Data}", userId, targetCountryId, level, data);
                        await SetEconomySanctionsTargetLevel(botClient, userId, callbackQuery.Message.Chat.Id, msgId, targetCountryId, level, true, cancellationToken);
                        await SetEconomySanctionsTargetLevel(botClient, userId, callbackQuery.Message.Chat.Id, msgId, targetCountryId, level, false, cancellationToken);
                    }
                }
                else if (partsSet.Length == 3 &&
                         int.TryParse(partsSet[1], out int targetCountryIdOld) &&
                         int.TryParse(partsSet[2], out int levelOld))
                {
                    _logger.LogInformation("Sanctions target set (old format): user={UserId} target={TargetId} level={Level} data={Data}", userId, targetCountryIdOld, levelOld, data);
                    await SetEconomySanctionsTargetLevel(botClient, userId, callbackQuery.Message.Chat.Id, msgId, targetCountryIdOld, levelOld, true, cancellationToken);
                    await SetEconomySanctionsTargetLevel(botClient, userId, callbackQuery.Message.Chat.Id, msgId, targetCountryIdOld, levelOld, false, cancellationToken);
                }
                await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                return;
            }

            if (data.StartsWith("state_economy_sanctions_targeteff|", StringComparison.OrdinalIgnoreCase))
            {
                var partsEff = data.Split('|');
                if (partsEff.Length >= 4 &&
                    int.TryParse(partsEff[1], out int targetCountryId) &&
                    int.TryParse(partsEff[3], out int effPct))
                {
                    bool isExport = partsEff[2].Equals("export", StringComparison.OrdinalIgnoreCase);
                    _logger.LogInformation("Sanctions target eff: user={UserId} target={TargetId} side={Side} eff={Eff} data={Data}", userId, targetCountryId, isExport ? "export" : "import", effPct, data);
                    await SetEconomySanctionsTargetEfficiency(botClient, userId, callbackQuery.Message.Chat.Id, msgId, targetCountryId, effPct, isExport, cancellationToken);
                }
                await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                return;
            }

            if (data.StartsWith("state_economy_sanctions_targeteff_input|", StringComparison.OrdinalIgnoreCase))
            {
                var partsInput = data.Split('|');
                if (partsInput.Length >= 3 &&
                    int.TryParse(partsInput[1], out int targetCountryId))
                {
                    bool isExport = partsInput[2].Equals("export", StringComparison.OrdinalIgnoreCase);
                    _logger.LogInformation("Sanctions target eff input: user={UserId} target={TargetId} side={Side} data={Data}", userId, targetCountryId, isExport ? "export" : "import", data);
                    await StartEconomySanctionsEfficiencyInput(botClient, userId, callbackQuery.Message.Chat.Id, msgId, targetCountryId, isExport, cancellationToken);
                }
                await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                return;
            }

            if (data.StartsWith("state_economy_sanctions_targetreset|", StringComparison.OrdinalIgnoreCase))
            {
                var partsReset = data.Split('|');
                if (partsReset.Length >= 3 &&
                    int.TryParse(partsReset[1], out int targetCountryId))
                {
                    bool isExport = partsReset[2].Equals("export", StringComparison.OrdinalIgnoreCase);
                    _logger.LogInformation("Sanctions target reset: user={UserId} target={TargetId} side={Side} data={Data}", userId, targetCountryId, isExport ? "export" : "import", data);
                    await ResetEconomySanctionsTarget(botClient, userId, callbackQuery.Message.Chat.Id, msgId, targetCountryId, isExport, cancellationToken);
                }
                await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                return;
            }

            if (data.StartsWith("state_economy_sanctions_productset|", StringComparison.OrdinalIgnoreCase))
            {
                var partsSet = data.Split('|');
                if (partsSet.Length >= 5 &&
                    int.TryParse(partsSet[1], out int targetCountryId) &&
                    int.TryParse(partsSet[4], out int level))
                {
                    string resourceType = DecodeResourceKey(partsSet[2]);
                    bool isExport = partsSet[3].Equals("export", StringComparison.OrdinalIgnoreCase);
                    bool isImport = partsSet[3].Equals("import", StringComparison.OrdinalIgnoreCase);

                    if (isExport || isImport)
                        await SetEconomySanctionsProductLevel(botClient, userId, callbackQuery.Message.Chat.Id, msgId, targetCountryId, resourceType, level, isExport, cancellationToken);
                }
                await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                return;
            }

            if (data.StartsWith("state_economy_sanctions_producteff|", StringComparison.OrdinalIgnoreCase))
            {
                var partsEff = data.Split('|');
                if (partsEff.Length >= 5 &&
                    int.TryParse(partsEff[1], out int targetCountryId) &&
                    int.TryParse(partsEff[4], out int effPct))
                {
                    string resourceType = DecodeResourceKey(partsEff[2]);
                    bool isExport = partsEff[3].Equals("export", StringComparison.OrdinalIgnoreCase);
                    await SetEconomySanctionsProductEfficiency(botClient, userId, callbackQuery.Message.Chat.Id, msgId, targetCountryId, resourceType, effPct, isExport, cancellationToken);
                }
                await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                return;
            }

            if (data.StartsWith("state_economy_sanctions_producteff_input|", StringComparison.OrdinalIgnoreCase))
            {
                var partsInput = data.Split('|');
                if (partsInput.Length >= 4 &&
                    int.TryParse(partsInput[1], out int targetCountryId))
                {
                    string resourceType = DecodeResourceKey(partsInput[2]);
                    bool isExport = partsInput[3].Equals("export", StringComparison.OrdinalIgnoreCase);
                    await StartEconomySanctionsProductEfficiencyInput(botClient, userId, callbackQuery.Message.Chat.Id, msgId, targetCountryId, resourceType, isExport, cancellationToken);
                }
                await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                return;
            }

            if (data.StartsWith("state_economy_sanctions_productreset|", StringComparison.OrdinalIgnoreCase))
            {
                var partsReset = data.Split('|');
                if (partsReset.Length >= 4 &&
                    int.TryParse(partsReset[1], out int targetCountryId))
                {
                    string resourceType = DecodeResourceKey(partsReset[2]);
                    bool isExport = partsReset[3].Equals("export", StringComparison.OrdinalIgnoreCase);
                    await ResetEconomySanctionsProduct(botClient, userId, callbackQuery.Message.Chat.Id, msgId, targetCountryId, resourceType, isExport, cancellationToken);
                }
                await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                return;
            }

            if (data.StartsWith("state_economy_sanctions_set_", StringComparison.OrdinalIgnoreCase))
            {
                if (!TryParseTrailingId(data, out int level) || level < 0 || level > 2)
                {
                    await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id, "Невідомий рівень санкцій");
                    return;
                }

                await SetEconomySanctionsLevel(botClient, userId, callbackQuery.Message.Chat.Id, msgId, level, cancellationToken);
                await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                return;
            }

            if (data == "state_economy_turn")
            {
                await RunEconomyTurn(botClient, userId, callbackQuery.Message.Chat.Id, msgId, cancellationToken);
                await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                return;
            }

            if (data == "state_time")
            {
                await ShowTimeMenu(botClient, userId, callbackQuery.Message.Chat.Id, msgId, cancellationToken);
                await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                return;
            }

            if (data == "state_time_demography")
            {
                await ShowTimeDemography(botClient, userId, callbackQuery.Message.Chat.Id, msgId, cancellationToken);
                await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                return;
            }

            if (data == "state_time_ai")
            {
                await ShowTimeAutomationMenu(botClient, userId, callbackQuery.Message.Chat.Id, msgId, 0, cancellationToken);
                await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                return;
            }

            if (data.StartsWith("state_time_ai_page_", StringComparison.OrdinalIgnoreCase))
            {
                int page = 0;
                if (TryParseTrailingId(data, out int parsedPage))
                    page = Math.Max(0, parsedPage);

                await ShowTimeAutomationMenu(botClient, userId, callbackQuery.Message.Chat.Id, msgId, page, cancellationToken);
                await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                return;
            }

            if (data.StartsWith("state_time_ai_toggle_", StringComparison.OrdinalIgnoreCase))
            {
                if (!TryParseTrailingId(data, out int toggleCountryId))
                {
                    await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id, "Невідома країна");
                    return;
                }

                await ToggleTimeAutomationCountry(botClient, userId, callbackQuery.Message.Chat.Id, msgId, toggleCountryId, cancellationToken);
                await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                return;
            }

            if (data.StartsWith("state_time_days_", StringComparison.OrdinalIgnoreCase))
            {
                if (!TryParseTrailingId(data, out int days))
                {
                    await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id, "Невідоме значення");
                    return;
                }
                await RunTimeAdvance(botClient, userId, callbackQuery.Message.Chat.Id, msgId, days, cancellationToken);
                await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                return;
            }

             

            if (data == "state_economy_buildings")
            {
                await ShowEconomyBuildingsMenu(botClient, userId, callbackQuery.Message.Chat.Id, msgId, cancellationToken);
                await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                return;
            }

            if (data == "state_economy_treasury_add")
            {
                await StartTreasuryChange(botClient, userId, true, cancellationToken);
                await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                return;
            }

            if (data == "state_economy_treasury_remove")
            {
                await StartTreasuryChange(botClient, userId, false, cancellationToken);
                await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                return;
            }

            if (data == "state_economy_moneylog")
            {
                await ShowEconomyMoneyLog(botClient, userId, callbackQuery.Message.Chat.Id, msgId, 0, cancellationToken);
                await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                return;
            }

            if (data == "state_economy_resources")
            {
                await ShowEconomyResourcesMenu(botClient, userId, callbackQuery.Message.Chat.Id, msgId, cancellationToken);
                await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                return;
            }

            if (data == "state_economy_resources_list")
            {
                await ShowEconomyResourcesList(botClient, userId, callbackQuery.Message.Chat.Id, msgId, 0, cancellationToken);
                await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                return;
            }

            if (data == "state_economy_resources_list_military")
            {
                await ShowEconomyResourcesList(botClient, userId, callbackQuery.Message.Chat.Id, msgId, 0, cancellationToken, militaryOnly: true);
                await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                return;
            }

            if (data.StartsWith("state_economy_resources_page_", StringComparison.OrdinalIgnoreCase))
            {
                int page = 0;
                if (TryParseTrailingId(data, out int parsed))
                    page = parsed;
                await ShowEconomyResourcesList(botClient, userId, callbackQuery.Message.Chat.Id, msgId, page, cancellationToken);
                await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                return;
            }

            if (data.StartsWith("state_economy_resources_military_page_", StringComparison.OrdinalIgnoreCase))
            {
                int page = 0;
                if (TryParseTrailingId(data, out int parsed))
                    page = parsed;
                await ShowEconomyResourcesList(botClient, userId, callbackQuery.Message.Chat.Id, msgId, page, cancellationToken, militaryOnly: true);
                await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                return;
            }

            if (data == "state_economy_resource_add")
            {
                await StartResourceCreation(botClient, userId, cancellationToken);
                await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                return;
            }

            if (data.StartsWith("state_economy_resourceinfo|", StringComparison.OrdinalIgnoreCase))
            {
                var partsRes = data.Split('|');
                if (partsRes.Length >= 2)
                {
                    string resourceType = DecodeResourceKey(partsRes[1]);
                    await ShowEconomyResourceInfo(botClient, userId, callbackQuery.Message.Chat.Id, msgId, resourceType, cancellationToken);
                }
                await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                return;
            }

            if (data.StartsWith("state_economy_resource_edit|", StringComparison.OrdinalIgnoreCase))
            {
                var partsRes = data.Split('|');
                if (partsRes.Length >= 2)
                {
                    string resourceType = DecodeResourceKey(partsRes[1]);
                    await ShowEconomyResourceEditMenu(botClient, userId, callbackQuery.Message.Chat.Id, msgId, resourceType, cancellationToken);
                }
                await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                return;
            }

            if (data.StartsWith("state_economy_resource_delete|", StringComparison.OrdinalIgnoreCase))
            {
                var partsRes = data.Split('|');
                if (partsRes.Length >= 2)
                {
                    string resourceType = DecodeResourceKey(partsRes[1]);
                    await ShowEconomyResourceDeleteConfirm(botClient, userId, callbackQuery.Message.Chat.Id, msgId, resourceType, cancellationToken);
                }
                await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                return;
            }

            if (data.StartsWith("state_economy_resource_deleteconfirm|", StringComparison.OrdinalIgnoreCase))
            {
                var partsRes = data.Split('|');
                if (partsRes.Length >= 2)
                {
                    string resourceType = DecodeResourceKey(partsRes[1]);
                    await DeleteEconomyResource(botClient, userId, callbackQuery.Message.Chat.Id, msgId, resourceType, cancellationToken);
                }
                await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                return;
            }

            if (data.StartsWith("state_economy_resource_tgeff|", StringComparison.OrdinalIgnoreCase))
            {
                var partsRes = data.Split('|');
                if (partsRes.Length >= 2)
                {
                    string resourceType = DecodeResourceKey(partsRes[1]);
                    await ToggleEconomyResourceProductionEfficiency(botClient, userId, callbackQuery.Message.Chat.Id, msgId, resourceType, cancellationToken);
                }
                await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                return;
            }

            if (data.StartsWith("state_economy_resource_editfield|", StringComparison.OrdinalIgnoreCase))
            {
                var partsRes = data.Split('|');
                if (partsRes.Length >= 3)
                {
                    string resourceType = DecodeResourceKey(partsRes[1]);
                    string fieldName = partsRes[2];
                    await StartEditResourceField(botClient, userId, resourceType, fieldName, cancellationToken);
                }
                await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                return;
            }

            if (data.StartsWith("state_economy_resource_parent_", StringComparison.OrdinalIgnoreCase))
            {
                await HandleResourceParentSelection(botClient, userId, callbackQuery.Message.Chat.Id, msgId, data, cancellationToken);
                await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                return;
            }

            if (data.StartsWith("state_economy_moneylog_page_", StringComparison.OrdinalIgnoreCase))
            {
                int page = 0;
                if (TryParseTrailingId(data, out int parsed))
                    page = parsed;
                await ShowEconomyMoneyLog(botClient, userId, callbackQuery.Message.Chat.Id, msgId, page, cancellationToken);
                await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                return;
            }

            if (data == "state_economy_cities")
            {
                await ShowEconomyCitiesMenu(botClient, userId, callbackQuery.Message.Chat.Id, msgId, 0, cancellationToken);
                await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                return;
            }

            if (data.StartsWith("state_economy_cities_page_", StringComparison.OrdinalIgnoreCase))
            {
                int page = 0;
                if (TryParseTrailingId(data, out int parsed))
                    page = parsed;
                await ShowEconomyCitiesMenu(botClient, userId, callbackQuery.Message.Chat.Id, msgId, page, cancellationToken);
                await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                return;
            }

            if (data == "state_economy_city_create")
            {
                await StartEconomyCityCreation(botClient, userId, cancellationToken);
                await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                return;
            }

            if (data == "state_economy_city_destroy_select")
            {
                await ShowEconomyCityDestroySelection(botClient, userId, callbackQuery.Message.Chat.Id, msgId, 0, cancellationToken);
                await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                return;
            }

            if (data.StartsWith("state_economy_city_destroy_page_", StringComparison.OrdinalIgnoreCase))
            {
                int page = 0;
                if (TryParseTrailingId(data, out int parsed))
                    page = parsed;
                await ShowEconomyCityDestroySelection(botClient, userId, callbackQuery.Message.Chat.Id, msgId, page, cancellationToken);
                await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                return;
            }

            if (data.StartsWith("state_economy_cityinfo_", StringComparison.OrdinalIgnoreCase))
            {
                if (TryParseTrailingId(data, out int cityId))
                    await ShowEconomyCityInfo(botClient, userId, callbackQuery.Message.Chat.Id, msgId, cityId, cancellationToken);
                await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                return;
            }

            if (data.StartsWith("state_economy_city_deleteconfirm_", StringComparison.OrdinalIgnoreCase))
            {
                if (TryParseTrailingId(data, out int cityId))
                    await DeleteEconomyCity(botClient, userId, callbackQuery.Message.Chat.Id, msgId, cityId, cancellationToken);
                await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                return;
            }

            if (data.StartsWith("state_economy_city_delete_", StringComparison.OrdinalIgnoreCase))
            {
                if (TryParseTrailingId(data, out int cityId))
                    await ShowEconomyCityDeleteConfirm(botClient, userId, callbackQuery.Message.Chat.Id, msgId, cityId, cancellationToken);
                await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                return;
            }

            if (data == "state_economy_buildings_list")
            {
                await ShowEconomyBuildingsList(botClient, userId, callbackQuery.Message.Chat.Id, msgId, 0, cancellationToken);
                await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                return;
            }

            if (data == "state_economy_buildings_list_military")
            {
                await ShowEconomyBuildingsList(botClient, userId, callbackQuery.Message.Chat.Id, msgId, 0, cancellationToken, militaryOnly: true);
                await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                return;
            }

            if (data == "state_economy_buildings_under_construction")
            {
                await ShowEconomyBuildingsUnderConstructionList(botClient, userId, callbackQuery.Message.Chat.Id, msgId, 0, cancellationToken);
                await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                return;
            }

            if (data.StartsWith("state_economy_buildings_page_", StringComparison.OrdinalIgnoreCase))
            {
                int page = 0;
                if (TryParseTrailingId(data, out int parsed))
                    page = parsed;
                await ShowEconomyBuildingsList(botClient, userId, callbackQuery.Message.Chat.Id, msgId, page, cancellationToken);
                await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                return;
            }

            if (data.StartsWith("state_economy_buildings_military_page_", StringComparison.OrdinalIgnoreCase))
            {
                int page = 0;
                if (TryParseTrailingId(data, out int parsed))
                    page = parsed;
                await ShowEconomyBuildingsList(botClient, userId, callbackQuery.Message.Chat.Id, msgId, page, cancellationToken, militaryOnly: true);
                await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                return;
            }

            if (data.StartsWith("state_economy_buildings_under_construction_page_", StringComparison.OrdinalIgnoreCase))
            {
                int page = 0;
                if (TryParseTrailingId(data, out int parsed))
                    page = parsed;
                await ShowEconomyBuildingsUnderConstructionList(botClient, userId, callbackQuery.Message.Chat.Id, msgId, page, cancellationToken);
                await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                return;
            }

            if (data.StartsWith("state_economy_buildinginfo_", StringComparison.OrdinalIgnoreCase))
            {
                if (TryParseTrailingId(data, out int buildingId))
                {
                    await ShowEconomyBuildingInfo(botClient, userId, callbackQuery.Message.Chat.Id, msgId, buildingId, cancellationToken);
                }
                await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                return;
            }

            if (data.StartsWith("state_economy_building_delete_", StringComparison.OrdinalIgnoreCase))
            {
                if (TryParseTrailingId(data, out int buildingId))
                {
                    await DeleteEconomyBuilding(botClient, userId, callbackQuery.Message.Chat.Id, msgId, buildingId, cancellationToken);
                }
                await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                return;
            }

            if (data.StartsWith("state_economy_building_upgrade_", StringComparison.OrdinalIgnoreCase))
            {
                if (data.StartsWith("state_economy_building_upgrade_preview_", StringComparison.OrdinalIgnoreCase))
                {
                    if (TryParseTrailingId(data, out int previewBuildingId))
                    {
                        await ShowEconomyBuildingUpgradePreview(botClient, userId, callbackQuery.Message.Chat.Id, msgId, previewBuildingId, cancellationToken);
                    }
                    await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                    return;
                }

                if (TryParseTrailingId(data, out int buildingId))
                {
                    await UpgradeEconomyBuilding(botClient, userId, callbackQuery.Message.Chat.Id, msgId, buildingId, cancellationToken);
                }
                await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                return;
            }

            if (data.StartsWith("state_economy_building_tgeff_", StringComparison.OrdinalIgnoreCase))
            {
                if (TryParseTrailingId(data, out int buildingId))
                {
                    await ToggleEconomyBuildingProductionEfficiency(botClient, userId, callbackQuery.Message.Chat.Id, msgId, buildingId, cancellationToken);
                }
                await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                return;
            }

            if (data.StartsWith("state_economy_building_city_page_", StringComparison.OrdinalIgnoreCase))
            {
                int page = 0;
                if (TryParseTrailingId(data, out int parsed))
                    page = parsed;
                await ShowBuildingCitySelection(botClient, userId, callbackQuery.Message.Chat.Id, msgId, page, cancellationToken, editExistingMessage: true);
                await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                return;
            }

            if (data.StartsWith("state_economy_building_city_pick_", StringComparison.OrdinalIgnoreCase))
            {
                if (TryParseTrailingId(data, out int index))
                    await SelectBuildingCityAndContinue(botClient, userId, index, cancellationToken);
                await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                return;
            }

            if (data.Equals("state_economy_building_city_new", StringComparison.OrdinalIgnoreCase))
            {
                await StartBuildingNewCityInput(botClient, userId, cancellationToken);
                await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                return;
            }

            if (data.Equals("state_economy_building_city_cancel", StringComparison.OrdinalIgnoreCase))
            {
                await CancelBuildingCitySelection(botClient, userId, cancellationToken);
                await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                return;
            }

            if (data == "state_economy_building_add")
            {
                await StartBuildingCreation(botClient, userId, cancellationToken);
                await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                return;
            }

            if (data.StartsWith("state_economy_building_templates_page_", StringComparison.OrdinalIgnoreCase))
            {
                int page = 0;
                if (TryParseTrailingId(data, out int parsed))
                    page = parsed;
                await ShowBuildingTemplates(botClient, userId, callbackQuery.Message.Chat.Id, msgId, page, cancellationToken);
                await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                return;
            }

            if (data == "state_economy_building_templates")
            {
                await ShowBuildingTemplates(botClient, userId, callbackQuery.Message.Chat.Id, msgId, 0, cancellationToken);
                await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                return;
            }

            if (data.StartsWith("state_economy_building_templates_military_page_", StringComparison.OrdinalIgnoreCase))
            {
                int page = 0;
                if (TryParseTrailingId(data, out int parsed))
                    page = parsed;
                await ShowBuildingTemplates(botClient, userId, callbackQuery.Message.Chat.Id, msgId, page, cancellationToken, militaryOnly: true);
                await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                return;
            }

            if (data == "state_economy_building_templates_military")
            {
                await ShowBuildingTemplates(botClient, userId, callbackQuery.Message.Chat.Id, msgId, 0, cancellationToken, militaryOnly: true);
                await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                return;
            }

            if (data.StartsWith("state_economy_building_template_use_", StringComparison.OrdinalIgnoreCase))
            {
                await StartBuildingTemplate(botClient, userId, data, cancellationToken);
                await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                return;
            }

            if (data.StartsWith("state_economy_building_template_preview|", StringComparison.OrdinalIgnoreCase))
            {
                await ShowBuildingTemplateLevelPreview(botClient, userId, callbackQuery.Message.Chat.Id, msgId, data, cancellationToken);
                await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                return;
            }

            if (data.StartsWith("state_economy_building_template_", StringComparison.OrdinalIgnoreCase))
            {
                await ShowBuildingTemplatePreview(botClient, userId, callbackQuery.Message.Chat.Id, msgId, data, cancellationToken);
                await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                return;
            }

            if (data.StartsWith("state_economy_building_edit_", StringComparison.OrdinalIgnoreCase))
            {
                if (TryParseTrailingId(data, out int buildingId))
                {
                    await ShowBuildingEditMenu(botClient, userId, callbackQuery.Message.Chat.Id, msgId, buildingId, cancellationToken);
                }
                await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                return;
            }

            if (data.StartsWith("state_economy_building_editfield_", StringComparison.OrdinalIgnoreCase))
            {
                await StartEditField(botClient, userId, data, "building", cancellationToken);
                await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                return;
            }

            if (data.StartsWith("state_economy_building_res_", StringComparison.OrdinalIgnoreCase))
            {
                await HandleBuildingResourceSelection(botClient, userId, callbackQuery.Message.Chat.Id, msgId, data, cancellationToken);
                await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                return;
            }

            if (data.StartsWith("state_economy_building_cat_", StringComparison.OrdinalIgnoreCase))
            {
                await HandleBuildingCategorySelection(botClient, userId, callbackQuery.Message.Chat.Id, msgId, data, cancellationToken);
                await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                return;
            }

            if (data == "state_economy_festivals")
            {
                await ShowEconomyFestivalsMenu(botClient, userId, callbackQuery.Message.Chat.Id, msgId, cancellationToken);
                await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                return;
            }

            if (data == "state_economy_festivals_list")
            {
                await ShowEconomyFestivalsList(botClient, userId, callbackQuery.Message.Chat.Id, msgId, 0, cancellationToken);
                await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                return;
            }

            if (data.StartsWith("state_economy_festivals_page_", StringComparison.OrdinalIgnoreCase))
            {
                int page = 0;
                if (TryParseTrailingId(data, out int parsed))
                    page = parsed;
                await ShowEconomyFestivalsList(botClient, userId, callbackQuery.Message.Chat.Id, msgId, page, cancellationToken);
                await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                return;
            }

            if (data.StartsWith("state_economy_festivalinfo_", StringComparison.OrdinalIgnoreCase))
            {
                if (TryParseTrailingId(data, out int festivalId))
                {
                    await ShowEconomyFestivalInfo(botClient, userId, callbackQuery.Message.Chat.Id, msgId, festivalId, cancellationToken);
                }
                await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                return;
            }

            if (data == "state_economy_festival_add")
            {
                await StartFestivalCreation(botClient, userId, cancellationToken);
                await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                return;
            }

            if (data.StartsWith("state_economy_festival_end_", StringComparison.OrdinalIgnoreCase))
            {
                if (TryParseTrailingId(data, out int festivalId))
                {
                    await EndEconomyFestival(botClient, userId, callbackQuery.Message.Chat.Id, msgId, festivalId, cancellationToken);
                }
                await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                return;
            }

            if (data.StartsWith("state_economy_festival_edit_", StringComparison.OrdinalIgnoreCase))
            {
                if (TryParseTrailingId(data, out int festivalId))
                {
                    await ShowFestivalEditMenu(botClient, userId, callbackQuery.Message.Chat.Id, msgId, festivalId, cancellationToken);
                }
                await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                return;
            }

            if (data.StartsWith("state_economy_festival_editfield_", StringComparison.OrdinalIgnoreCase))
            {
                await StartEditField(botClient, userId, data, "festival", cancellationToken);
                await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                return;
            }

            if (data == "state_economy_trade")
            {
                await ShowTradeMenu(botClient, userId, callbackQuery.Message.Chat.Id, msgId, cancellationToken);
                await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                return;
            }

            if (data == "state_economy_trade_list")
            {
                await ShowTradeList(botClient, userId, callbackQuery.Message.Chat.Id, msgId, 0, cancellationToken);
                await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                return;
            }

            if (data == "state_economy_trade_pending")
            {
                await ShowTradePendingList(botClient, userId, callbackQuery.Message.Chat.Id, msgId, 0, cancellationToken);
                await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                return;
            }

            if (data.StartsWith("state_economy_trade_pending_page_", StringComparison.OrdinalIgnoreCase))
            {
                int page = 0;
                if (TryParseTrailingId(data, out int parsed))
                    page = parsed;
                await ShowTradePendingList(botClient, userId, callbackQuery.Message.Chat.Id, msgId, page, cancellationToken);
                await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                return;
            }

            if (data.StartsWith("state_economy_trade_pending_info_", StringComparison.OrdinalIgnoreCase))
            {
                if (TryParseTrailingId(data, out int requestId))
                    await ShowTradePendingInfo(botClient, userId, callbackQuery.Message.Chat.Id, msgId, requestId, cancellationToken);
                await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                return;
            }

            if (data.StartsWith("state_economy_trade_page_", StringComparison.OrdinalIgnoreCase))
            {
                int page = 0;
                if (TryParseTrailingId(data, out int parsed))
                    page = parsed;
                await ShowTradeList(botClient, userId, callbackQuery.Message.Chat.Id, msgId, page, cancellationToken);
                await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                return;
            }

            if (data.StartsWith("state_economy_tradeinfo_", StringComparison.OrdinalIgnoreCase))
            {
                if (TryParseTrailingId(data, out int agreementId))
                {
                    await ShowTradeInfo(botClient, userId, callbackQuery.Message.Chat.Id, msgId, agreementId, cancellationToken);
                }
                await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                return;
            }

            if (data == "state_economy_trade_add")
            {
                await StartTradeCreation(botClient, userId, callbackQuery.Message.Chat.Id, msgId, cancellationToken);
                await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                return;
            }

            if (data == "state_economy_trade_sync_pairs")
            {
                await SyncTradeAgreementsPairs(botClient, userId, callbackQuery.Message.Chat.Id, msgId, cancellationToken);
                await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                return;
            }

            if (data.StartsWith("state_economy_trade_partner_", StringComparison.OrdinalIgnoreCase))
            {
                await HandleTradePartnerSelection(botClient, userId, callbackQuery.Message.Chat.Id, msgId, data, cancellationToken);
                await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                return;
            }

            if (data.StartsWith("state_economy_trade_mode_", StringComparison.OrdinalIgnoreCase))
            {
                await HandleTradeFlowModeSelection(botClient, userId, callbackQuery.Message.Chat.Id, msgId, data, cancellationToken);
                await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                return;
            }

            if (data.StartsWith("state_economy_trade_res_", StringComparison.OrdinalIgnoreCase))
            {
                await HandleTradeResourceSelection(botClient, userId, callbackQuery.Message.Chat.Id, msgId, data, cancellationToken);
                await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                return;
            }

            if (data.StartsWith("state_economy_trade_end_", StringComparison.OrdinalIgnoreCase))
            {
                if (TryParseTrailingId(data, out int agreementId))
                {
                    await EndTradeAgreement(botClient, userId, callbackQuery.Message.Chat.Id, msgId, agreementId, cancellationToken);
                }
                await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                return;
            }

            if (data.StartsWith("state_economy_trade_edit_", StringComparison.OrdinalIgnoreCase))
            {
                if (TryParseTrailingId(data, out int agreementId))
                {
                    await ShowTradeEditMenu(botClient, userId, callbackQuery.Message.Chat.Id, msgId, agreementId, cancellationToken);
                }
                await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                return;
            }

            if (data.StartsWith("state_economy_trade_editfield_", StringComparison.OrdinalIgnoreCase))
            {
                await StartEditField(botClient, userId, data, "trade", cancellationToken);
                await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                return;
            }

            if (data == "state_main")
            {
                await EditStateAdministrationMessage(botClient, chatId, userId, msgId, cancellationToken);
                await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                return;
            }

            if (data.StartsWith("state_selectcountry_"))
            {
                if (TryParseCountryId(data, out int countryId))
                {
                    SelectedCountryByUser[userId] = countryId;
                    if (IsOverviewView(userId))
                    {
                        await ShowOverviewMessage(botClient, chatId, userId, msgId, cancellationToken);
                    }
                    else
                    {
                        await EditStateAdministrationMessage(botClient, chatId, userId, msgId, cancellationToken);
                    }
                }

                await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                return;
            }

            if (!IsSelectedCountryOwner(userId, GetSelectedCountryIdForUser(userId)) && userId != masterID)
            {
                if (info != "overview" && info != "main" && info != "selectcountry" && info != "cancel" && info != "economy")
                {
                    await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id, "Нема доступу");
                    return;
                }
            }

            if (data.StartsWith("state_admin_", StringComparison.OrdinalIgnoreCase))
            {
                if (!HasAdminAccess(userId))
                {
                    await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id, "Нема доступу");
                    return;
                }

                if (data.Equals("state_admin_health", StringComparison.OrdinalIgnoreCase))
                {
                    await ShowAdminHealthMenu(botClient, userId, callbackQuery.Message.Chat.Id, msgId, cancellationToken);
                    await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                    return;
                }
                if (data.Equals("state_admin_health_report", StringComparison.OrdinalIgnoreCase))
                {
                    await ShowAdminHealthReport(botClient, userId, callbackQuery.Message.Chat.Id, msgId, cancellationToken);
                    await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                    return;
                }
                if (data.Equals("state_admin_health_budget", StringComparison.OrdinalIgnoreCase))
                {
                    await ShowAdminHealthBudget(botClient, userId, callbackQuery.Message.Chat.Id, msgId, cancellationToken);
                    await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                    return;
                }
                if (data.Equals("state_admin_health_budget_health", StringComparison.OrdinalIgnoreCase))
                {
                    await StartPublicBudgetInput(
                        botClient,
                        userId,
                        callbackQuery.Message.Chat.Id,
                        msgId,
                        EconomyInputMode.PublicBudgetHealth,
                        "Вкажіть бюджет МОЗ за тік (невід'ємне число).",
                        PublicRouteAdminHealthBudget,
                        cancellationToken);
                    await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                    return;
                }
                if (data.Equals("state_admin_health_budget_intl", StringComparison.OrdinalIgnoreCase))
                {
                    await StartPublicBudgetInput(
                        botClient,
                        userId,
                        callbackQuery.Message.Chat.Id,
                        msgId,
                        EconomyInputMode.PublicBudgetIntl,
                        "Вкажіть бюджет міжнародних програм за тік (невід'ємне число).",
                        PublicRouteAdminHealthBudget,
                        cancellationToken);
                    await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                    return;
                }
                if (data.Equals("state_admin_health_budget_intl_mode", StringComparison.OrdinalIgnoreCase))
                {
                    await TogglePublicIntlBudgetMode(botClient, userId, callbackQuery.Message.Chat.Id, msgId, cancellationToken);
                    await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                    return;
                }
                if (data.Equals("state_admin_health_actions", StringComparison.OrdinalIgnoreCase))
                {
                    await ShowAdminHealthActions(botClient, userId, callbackQuery.Message.Chat.Id, msgId, cancellationToken);
                    await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                    return;
                }
                if (data.Equals("state_admin_health_action_quarantine_toggle", StringComparison.OrdinalIgnoreCase))
                {
                    await TogglePublicQuarantine(botClient, userId, callbackQuery.Message.Chat.Id, msgId, cancellationToken);
                    await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                    return;
                }
                if (data.Equals("state_admin_health_action_medic_mobilize", StringComparison.OrdinalIgnoreCase))
                {
                    await ApplyPublicMedicMobilization(botClient, userId, callbackQuery.Message.Chat.Id, msgId, cancellationToken);
                    await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                    return;
                }
                if (data.Equals("state_admin_health_action_trust_campaign", StringComparison.OrdinalIgnoreCase))
                {
                    await ApplyPublicTrustCampaign(botClient, userId, callbackQuery.Message.Chat.Id, msgId, cancellationToken);
                    await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                    return;
                }
                if (data.Equals("state_admin_health_action_vaccination", StringComparison.OrdinalIgnoreCase))
                {
                    await ApplyPublicVaccination(botClient, userId, callbackQuery.Message.Chat.Id, msgId, cancellationToken);
                    await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                    return;
                }
                if (data.Equals("state_admin_health_action_wastewater", StringComparison.OrdinalIgnoreCase))
                {
                    await ApplyPublicWastewaterSurveillance(botClient, userId, callbackQuery.Message.Chat.Id, msgId, cancellationToken);
                    await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                    return;
                }
                if (data.Equals("state_admin_health_action_telemed", StringComparison.OrdinalIgnoreCase))
                {
                    await ApplyPublicTelemedicineTriage(botClient, userId, callbackQuery.Message.Chat.Id, msgId, cancellationToken);
                    await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                    return;
                }
                if (data.Equals("state_admin_health_action_drug_research", StringComparison.OrdinalIgnoreCase))
                {
                    await ApplyPublicDrugResearchProject(botClient, userId, callbackQuery.Message.Chat.Id, msgId, cancellationToken);
                    await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                    return;
                }
                if (data.Equals("state_admin_health_action_doctor_training", StringComparison.OrdinalIgnoreCase))
                {
                    await ApplyPublicDoctorTraining(botClient, userId, callbackQuery.Message.Chat.Id, msgId, cancellationToken);
                    await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                    return;
                }
                if (data.Equals("state_admin_health_action_clinical_trials", StringComparison.OrdinalIgnoreCase))
                {
                    await ApplyPublicClinicalTrials(botClient, userId, callbackQuery.Message.Chat.Id, msgId, cancellationToken);
                    await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                    return;
                }
                if (data.Equals("state_admin_health_action_sanitation_raids", StringComparison.OrdinalIgnoreCase))
                {
                    await ApplyAdminHealthSanitationRaids(botClient, userId, callbackQuery.Message.Chat.Id, msgId, cancellationToken);
                    await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                    return;
                }
                if (data.Equals("state_admin_health_diseases", StringComparison.OrdinalIgnoreCase))
                {
                    await ShowAdminHealthDiseases(botClient, userId, callbackQuery.Message.Chat.Id, msgId, cancellationToken);
                    await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                    return;
                }
                if (data.Equals("state_admin_health_gm_spawn_help", StringComparison.OrdinalIgnoreCase))
                {
                    await ShowAdminHealthGmSpawnHelp(botClient, userId, callbackQuery.Message.Chat.Id, msgId, cancellationToken);
                    await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                    return;
                }
                if (data.Equals("state_admin_health_gm_end_all", StringComparison.OrdinalIgnoreCase))
                {
                    await EndAllAdminHealthDiseases(botClient, userId, callbackQuery.Message.Chat.Id, msgId, cancellationToken);
                    await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                    return;
                }
                if (data.StartsWith("state_admin_health_disease_shift_", StringComparison.OrdinalIgnoreCase))
                {
                    const string prefix = "state_admin_health_disease_shift_";
                    string rest = data.Substring(prefix.Length);
                    int split = rest.LastIndexOf('_');
                    if (split > 0 &&
                        int.TryParse(rest.Substring(0, split), NumberStyles.Integer, CultureInfo.InvariantCulture, out int diseaseId) &&
                        int.TryParse(rest.Substring(split + 1), NumberStyles.Integer, CultureInfo.InvariantCulture, out int delta))
                    {
                        await ShiftAdminHealthDiseaseStage(botClient, userId, callbackQuery.Message.Chat.Id, msgId, diseaseId, delta, cancellationToken);
                    }
                    await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                    return;
                }
                if (data.StartsWith("state_admin_health_disease_end_", StringComparison.OrdinalIgnoreCase))
                {
                    if (TryParseTrailingId(data, out int diseaseId))
                        await EndAdminHealthDisease(botClient, userId, callbackQuery.Message.Chat.Id, msgId, diseaseId, cancellationToken);
                    await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                    return;
                }
                if (data.StartsWith("state_admin_health_disease_", StringComparison.OrdinalIgnoreCase))
                {
                    if (TryParseTrailingId(data, out int diseaseId))
                        await ShowAdminHealthDiseaseInfo(botClient, userId, callbackQuery.Message.Chat.Id, msgId, diseaseId, cancellationToken);
                    await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                    return;
                }
                if (data.Equals("state_admin_health_intl", StringComparison.OrdinalIgnoreCase))
                {
                    await ShowAdminHealthIntlPrograms(botClient, userId, callbackQuery.Message.Chat.Id, msgId, cancellationToken);
                    await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                    return;
                }
                if (data.StartsWith("state_admin_health_intl_toggle_", StringComparison.OrdinalIgnoreCase))
                {
                    if (TryParseTrailingId(data, out int intlProgramId))
                        await ToggleAdminHealthIntlProgram(botClient, userId, callbackQuery.Message.Chat.Id, msgId, intlProgramId, cancellationToken);
                    await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                    return;
                }
                if (data.StartsWith("state_admin_health_intl_add_", StringComparison.OrdinalIgnoreCase))
                {
                    string token = data.Substring("state_admin_health_intl_add_".Length);
                    await AddAdminHealthIntlProgram(botClient, userId, callbackQuery.Message.Chat.Id, msgId, token, cancellationToken);
                    await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                    return;
                }
                if (data.Equals("state_admin_education", StringComparison.OrdinalIgnoreCase))
                {
                    await ShowAdminEducationMenu(botClient, userId, callbackQuery.Message.Chat.Id, msgId, cancellationToken);
                    await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                    return;
                }
                if (data.Equals("state_admin_education_report", StringComparison.OrdinalIgnoreCase))
                {
                    await ShowAdminEducationReport(botClient, userId, callbackQuery.Message.Chat.Id, msgId, cancellationToken);
                    await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                    return;
                }
                if (data.Equals("state_admin_education_budget", StringComparison.OrdinalIgnoreCase))
                {
                    await ShowAdminEducationBudget(botClient, userId, callbackQuery.Message.Chat.Id, msgId, cancellationToken);
                    await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                    return;
                }
                if (data.Equals("state_admin_education_budget_education", StringComparison.OrdinalIgnoreCase))
                {
                    await StartPublicBudgetInput(
                        botClient,
                        userId,
                        callbackQuery.Message.Chat.Id,
                        msgId,
                        EconomyInputMode.PublicBudgetEducation,
                        "Вкажіть бюджет МОН за тік (невід'ємне число).",
                        PublicRouteAdminEducationBudget,
                        cancellationToken);
                    await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                    return;
                }
                if (data.Equals("state_admin_education_budget_intl", StringComparison.OrdinalIgnoreCase))
                {
                    await StartPublicBudgetInput(
                        botClient,
                        userId,
                        callbackQuery.Message.Chat.Id,
                        msgId,
                        EconomyInputMode.PublicBudgetIntl,
                        "Вкажіть бюджет міжнародних програм за тік (невід'ємне число).",
                        PublicRouteAdminEducationBudget,
                        cancellationToken);
                    await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                    return;
                }
                if (data.Equals("state_admin_education_budget_intl_mode", StringComparison.OrdinalIgnoreCase))
                {
                    await TogglePublicIntlBudgetMode(botClient, userId, callbackQuery.Message.Chat.Id, msgId, cancellationToken);
                    await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                    return;
                }
                if (data.Equals("state_admin_education_actions", StringComparison.OrdinalIgnoreCase))
                {
                    await ShowAdminEducationActions(botClient, userId, callbackQuery.Message.Chat.Id, msgId, cancellationToken);
                    await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                    return;
                }
                if (data.Equals("state_admin_education_action_literacy", StringComparison.OrdinalIgnoreCase))
                {
                    await ApplyAdminEducationLiteracyCampaign(botClient, userId, callbackQuery.Message.Chat.Id, msgId, cancellationToken);
                    await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                    return;
                }
                if (data.Equals("state_admin_education_action_teachers", StringComparison.OrdinalIgnoreCase))
                {
                    await ApplyAdminEducationTeacherTraining(botClient, userId, callbackQuery.Message.Chat.Id, msgId, cancellationToken);
                    await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                    return;
                }
                if (data.Equals("state_admin_education_action_scholarship", StringComparison.OrdinalIgnoreCase))
                {
                    await ApplyAdminEducationScholarshipPush(botClient, userId, callbackQuery.Message.Chat.Id, msgId, cancellationToken);
                    await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                    return;
                }
                if (data.Equals("state_admin_education_action_civics", StringComparison.OrdinalIgnoreCase))
                {
                    await ApplyAdminEducationCivicsProgram(botClient, userId, callbackQuery.Message.Chat.Id, msgId, cancellationToken);
                    await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                    return;
                }
                if (data.Equals("state_admin_education_action_teacher_cert", StringComparison.OrdinalIgnoreCase))
                {
                    await ApplyAdminEducationTeacherCertification(botClient, userId, callbackQuery.Message.Chat.Id, msgId, cancellationToken);
                    await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                    return;
                }
                if (data.Equals("state_admin_education_action_tutoring", StringComparison.OrdinalIgnoreCase))
                {
                    await ApplyAdminEducationHighDosageTutoring(botClient, userId, callbackQuery.Message.Chat.Id, msgId, cancellationToken);
                    await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                    return;
                }
                if (data.Equals("state_admin_education_action_school_meals", StringComparison.OrdinalIgnoreCase))
                {
                    await ApplyAdminEducationSchoolMeals(botClient, userId, callbackQuery.Message.Chat.Id, msgId, cancellationToken);
                    await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                    return;
                }
                if (data.Equals("state_admin_education_action_mentor", StringComparison.OrdinalIgnoreCase))
                {
                    await ApplyAdminEducationTeacherMentoring(botClient, userId, callbackQuery.Message.Chat.Id, msgId, cancellationToken);
                    await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                    return;
                }
                if (data.Equals("state_admin_education_intl", StringComparison.OrdinalIgnoreCase))
                {
                    await ShowAdminEducationIntlPrograms(botClient, userId, callbackQuery.Message.Chat.Id, msgId, cancellationToken);
                    await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                    return;
                }
                if (data.StartsWith("state_admin_education_intl_toggle_", StringComparison.OrdinalIgnoreCase))
                {
                    if (TryParseTrailingId(data, out int intlProgramId))
                        await ToggleAdminEducationIntlProgram(botClient, userId, callbackQuery.Message.Chat.Id, msgId, intlProgramId, cancellationToken);
                    await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                    return;
                }
                if (data.StartsWith("state_admin_education_intl_add_", StringComparison.OrdinalIgnoreCase))
                {
                    string token = data.Substring("state_admin_education_intl_add_".Length);
                    await AddAdminEducationIntlProgram(botClient, userId, callbackQuery.Message.Chat.Id, msgId, token, cancellationToken);
                    await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                    return;
                }
                if (data.Equals("state_admin_economy", StringComparison.OrdinalIgnoreCase))
                {
                    await ShowAdminEconomyMenu(botClient, userId, callbackQuery.Message.Chat.Id, msgId, cancellationToken);
                    await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                    return;
                }
                if (data.Equals("state_admin_social", StringComparison.OrdinalIgnoreCase))
                {
                    await ShowAdminSocialMenu(botClient, userId, callbackQuery.Message.Chat.Id, msgId, cancellationToken);
                    await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                    return;
                }
                if (data.Equals("state_admin_military", StringComparison.OrdinalIgnoreCase))
                {
                    await ShowAdminMilitaryMenu(botClient, userId, callbackQuery.Message.Chat.Id, msgId, cancellationToken);
                    await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                    return;
                }
                if (data.Equals("state_admin_resources", StringComparison.OrdinalIgnoreCase))
                {
                    await ShowAdminResourcesMenu(botClient, userId, callbackQuery.Message.Chat.Id, msgId, cancellationToken);
                    await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                    return;
                }
                if (data.Equals("state_admin_analytics", StringComparison.OrdinalIgnoreCase))
                {
                    await ShowAdminAnalytics(botClient, userId, callbackQuery.Message.Chat.Id, msgId, cancellationToken);
                    await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                    return;
                }
                if (data.Equals("state_admin_countryname", StringComparison.OrdinalIgnoreCase))
                {
                    if (userId != masterID)
                    {
                        await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id, "Нема доступу");
                        return;
                    }
                    await StartAdminCountryNameInput(botClient, userId, cancellationToken);
                    await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                    return;
                }
                if (data.Equals("state_admin_budget", StringComparison.OrdinalIgnoreCase))
                {
                    await StartAdminBudgetInput(botClient, userId, cancellationToken);
                    await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                    return;
                }
                if (data.Equals("state_admin_tax", StringComparison.OrdinalIgnoreCase))
                {
                    await StartAdminTaxInput(botClient, userId, cancellationToken);
                    await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                    return;
                }
                if (data.Equals("state_admin_inflation", StringComparison.OrdinalIgnoreCase))
                {
                    await StartAdminInflationInput(botClient, userId, cancellationToken);
                    await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                    return;
                }
                if (data.Equals("state_admin_military_upkeep", StringComparison.OrdinalIgnoreCase))
                {
                    await StartAdminMilitaryUpkeepInput(botClient, userId, cancellationToken);
                    await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                    return;
                }
                if (data.Equals("state_admin_army_morale", StringComparison.OrdinalIgnoreCase))
                {
                    await StartAdminArmyMoraleInput(botClient, userId, cancellationToken);
                    await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                    return;
                }
                if (data.Equals("state_admin_weapon_quality", StringComparison.OrdinalIgnoreCase))
                {
                    await StartAdminWeaponQualityInput(botClient, userId, cancellationToken);
                    await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                    return;
                }
                if (data.Equals("state_admin_salary_soldier", StringComparison.OrdinalIgnoreCase))
                {
                    await StartAdminSoldierSalaryInput(botClient, userId, cancellationToken);
                    await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                    return;
                }
                if (data.Equals("state_admin_salary_deputy", StringComparison.OrdinalIgnoreCase))
                {
                    await StartAdminDeputySalaryInput(botClient, userId, cancellationToken);
                    await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                    return;
                }
                if (data.Equals("state_admin_salary_civilian", StringComparison.OrdinalIgnoreCase))
                {
                    await StartAdminCivilianSalaryInput(botClient, userId, cancellationToken);
                    await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                    return;
                }
                if (data.Equals("state_admin_stability", StringComparison.OrdinalIgnoreCase))
                {
                    await StartAdminStabilityInput(botClient, userId, cancellationToken);
                    await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                    return;
                }
                if (data.Equals("state_admin_corruption", StringComparison.OrdinalIgnoreCase))
                {
                    await StartAdminCorruptionInput(botClient, userId, cancellationToken);
                    await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                    return;
                }
                if (data.Equals("state_admin_happiness", StringComparison.OrdinalIgnoreCase))
                {
                    await StartAdminHappinessInput(botClient, userId, cancellationToken);
                    await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                    return;
                }
                if (data.Equals("state_admin_unrest", StringComparison.OrdinalIgnoreCase))
                {
                    await StartAdminUnrestInput(botClient, userId, cancellationToken);
                    await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                    return;
                }
                if (data.Equals("state_admin_population_growth", StringComparison.OrdinalIgnoreCase))
                {
                    await StartAdminPopulationGrowthInput(botClient, userId, cancellationToken);
                    await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                    return;
                }
                if (data.Equals("state_admin_population_add", StringComparison.OrdinalIgnoreCase))
                {
                    await StartAdminAddPopulationInput(botClient, userId, cancellationToken);
                    await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                    return;
                }
                if (data.Equals("state_admin_population_remove", StringComparison.OrdinalIgnoreCase))
                {
                    await StartAdminRemovePopulationInput(botClient, userId, cancellationToken);
                    await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                    return;
                }
                if (data.Equals("state_admin_population_races", StringComparison.OrdinalIgnoreCase))
                {
                    await ShowAdminPopulationRacesMenu(botClient, userId, callbackQuery.Message.Chat.Id, msgId, 0, cancellationToken);
                    await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                    return;
                }
                if (data.StartsWith("state_admin_population_races_page_", StringComparison.OrdinalIgnoreCase))
                {
                    int page = 0;
                    if (TryParseTrailingId(data, out int parsed))
                        page = parsed;
                    await ShowAdminPopulationRacesMenu(botClient, userId, callbackQuery.Message.Chat.Id, msgId, page, cancellationToken);
                    await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                    return;
                }
                if (data.Equals("state_admin_population_race_new", StringComparison.OrdinalIgnoreCase))
                {
                    await StartAdminCreatePopulationRaceInput(botClient, userId, cancellationToken);
                    await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                    return;
                }
                if (data.StartsWith("state_admin_population_raceinfo|", StringComparison.OrdinalIgnoreCase))
                {
                    if (TryParseAdminPopulationRacePayload(data, "state_admin_population_raceinfo|", out string raceName, out int returnPage))
                    {
                        await ShowAdminPopulationRaceInfo(botClient, userId, callbackQuery.Message.Chat.Id, msgId, raceName, returnPage, cancellationToken);
                    }
                    await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                    return;
                }
                if (data.StartsWith("state_admin_population_race_set|", StringComparison.OrdinalIgnoreCase))
                {
                    if (TryParseAdminPopulationRacePayload(data, "state_admin_population_race_set|", out string raceName, out int returnPage))
                    {
                        await StartAdminPopulationRaceAmountInput(botClient, userId, AdminInputMode.SetPopulationRaceAmount, raceName, returnPage, cancellationToken);
                    }
                    await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                    return;
                }
                if (data.StartsWith("state_admin_population_race_add|", StringComparison.OrdinalIgnoreCase))
                {
                    if (TryParseAdminPopulationRacePayload(data, "state_admin_population_race_add|", out string raceName, out int returnPage))
                    {
                        await StartAdminPopulationRaceAmountInput(botClient, userId, AdminInputMode.AddPopulationRaceAmount, raceName, returnPage, cancellationToken);
                    }
                    await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                    return;
                }
                if (data.StartsWith("state_admin_population_race_remove|", StringComparison.OrdinalIgnoreCase))
                {
                    if (TryParseAdminPopulationRacePayload(data, "state_admin_population_race_remove|", out string raceName, out int returnPage))
                    {
                        await StartAdminPopulationRaceAmountInput(botClient, userId, AdminInputMode.RemovePopulationRaceAmount, raceName, returnPage, cancellationToken);
                    }
                    await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                    return;
                }
                if (data.StartsWith("state_admin_population_race_delete|", StringComparison.OrdinalIgnoreCase))
                {
                    if (TryParseAdminPopulationRacePayload(data, "state_admin_population_race_delete|", out string raceName, out int returnPage))
                    {
                        await DeleteAdminPopulationRace(botClient, userId, callbackQuery.Message.Chat.Id, msgId, raceName, returnPage, cancellationToken);
                    }
                    await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                    return;
                }
                if (data.Equals("state_admin_intel", StringComparison.OrdinalIgnoreCase))
                {
                    await StartAdminIntelInput(botClient, userId, cancellationToken);
                    await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                    return;
                }
                if (data.Equals("state_admin_counterintel", StringComparison.OrdinalIgnoreCase))
                {
                    await StartAdminCounterIntelInput(botClient, userId, cancellationToken);
                    await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                    return;
                }
                if (data.Equals("state_admin_mobilize", StringComparison.OrdinalIgnoreCase))
                {
                    await StartAdminMobilizeInput(botClient, userId, cancellationToken);
                    await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                    return;
                }
                if (data.Equals("state_admin_martiallaw", StringComparison.OrdinalIgnoreCase))
                {
                    var selectedCountryId = GetSelectedCountryIdForUser(userId);
                    if (!selectedCountryId.HasValue)
                    {
                        await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id, "Не знайдено країни");
                        return;
                    }
                    var country = CountriesData.GetCountryById(selectedCountryId.Value);
                    if (country == null)
                    {
                        await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id, "Країну не знайдено");
                        return;
                    }
                    country.MartialLaw = !country.MartialLaw;
                    country.LastUpdate = DateTime.UtcNow;
                    CountriesData.UpdateCountry(country);
                    AdminActionsLogData.AddLog(country.CountryId, userId, "MartialLaw",
                        (!country.MartialLaw).ToString(), country.MartialLaw.ToString(), null);
                    await ShowAdminMilitaryMenu(botClient, userId, callbackQuery.Message.Chat.Id, msgId, cancellationToken);
                    await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                    return;
                }
                if (data.Equals("state_admin_log", StringComparison.OrdinalIgnoreCase))
                {
                    await ShowAdminDecisionLog(botClient, userId, callbackQuery.Message.Chat.Id, msgId, 0, cancellationToken);
                    await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                    return;
                }
                if (data.StartsWith("state_admin_log_page_", StringComparison.OrdinalIgnoreCase))
                {
                    int page = 0;
                    if (TryParseTrailingId(data, out int parsed))
                        page = parsed;
                    await ShowAdminDecisionLog(botClient, userId, callbackQuery.Message.Chat.Id, msgId, page, cancellationToken);
                    await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                    return;
                }
                if (data.Equals("state_admin_resource_regions", StringComparison.OrdinalIgnoreCase))
                {
                    await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id, "Секція в розробці");
                    return;
                }
            }

            switch (info)
            {
                case "parliament":
                    {
                        int? selectedCountryId = GetSelectedCountryIdForUser(userId);
                        bool isCountryOwner = IsSelectedCountryOwner(userId, selectedCountryId);
                        TryEnsureParliamentProvision(userId, selectedCountryId, isCountryOwner);
                    }
                    await _stateParliamentHandle.EditMessageInfoForParlament(botClient, callbackQuery.Message.Chat.Id, msgId, userId, cancellationToken);
                    break;
                case "laws":
                    break;
                case "research":
                    if (!IsSelectedCountryOwner(userId, GetSelectedCountryIdForUser(userId)) && userId != masterID)
                    {
                        await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id, "Нема доступу");
                        return;
                    }
                    await ShowResearchMenu(botClient, chatId, userId, msgId, cancellationToken);
                    break;
                case "economy":
                    await ShowEconomyMenu(botClient, userId, callbackQuery.Message.Chat.Id, msgId, cancellationToken);
                    break;
                case "admin":
                    if (!HasAdminAccess(userId))
                    {
                        await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id, "Нема доступу");
                        return;
                    }
                    await ShowAdminMenu(botClient, userId, callbackQuery.Message.Chat.Id, msgId, cancellationToken);
                    break;
                case "masterpanel":
                    if (userId != masterID)
                    {
                        await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id, "Нема доступу");
                        return;
                    }
                    await ShowMasterPanel(botClient, chatId, userId, msgId, cancellationToken);
                    break;
                    case "cancel":
                    await EditStateAdministrationMessage(botClient, chatId, userId, msgId, cancellationToken);
                    break;
                default:
                    break;
            }


            await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
        }

    }
}
