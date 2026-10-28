#nullable enable
using DNDBot.BotSystem;
using DNDBot.DataBase;
using DNDBot.Goverment.Economy;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;

namespace DNDBot.Goverment
{
    public partial class StateAdministration : BotBodyRealisation
    {
        private const string AcceptedLawStatus = "Accepted";

        private static bool HasPublicControlAccess(long userId, int? selectedCountryId)
        {
            if (!selectedCountryId.HasValue || selectedCountryId.Value <= 0)
                return false;
            return userId == masterID || IsSelectedCountryOwner(userId, selectedCountryId);
        }

        private static string FormatPublicStageUk(int stage)
        {
            return stage switch
            {
                0 => "нема",
                1 => "спалах",
                2 => "пік",
                3 => "спад",
                4 => "післядія",
                _ => "невідомо"
            };
        }

        private static float GetPublicStageFactor(int stage)
        {
            return stage switch
            {
                1 => 1f,
                2 => 2f,
                3 => 1f,
                4 => 0.5f,
                _ => 0f
            };
        }

        private static string NormalizePublicDiseaseTargetScope(string? scope)
        {
            if (string.IsNullOrWhiteSpace(scope))
                return "whole_state";

            string normalized = scope.Trim().ToLowerInvariant();
            return normalized switch
            {
                "whole_state" => "whole_state",
                "state" => "whole_state",
                "country" => "whole_state",
                "держава" => "whole_state",
                "region" => "region",
                "регіон" => "region",
                "city" => "city",
                "місто" => "city",
                _ => "whole_state"
            };
        }

        private static string FormatPublicDiseaseTargetUk(string? scope)
        {
            return NormalizePublicDiseaseTargetScope(scope) switch
            {
                "region" => "регіон",
                "city" => "місто",
                _ => "вся держава"
            };
        }

        private static string FormatIntlBudgetModeUk(string? mode)
        {
            return PublicSystemsEngine.NormalizeIntlBudgetMode(mode).Equals("Manual", StringComparison.OrdinalIgnoreCase)
                ? "вручну"
                : "авто";
        }

        private static string BuildLawStatusLine(bool hasLaw, string caption)
        {
            string mark = hasLaw ? "✅" : "⛔";
            string label = PublicSystemsEngine.GetLawDisplayNameUk(caption);
            return $"{mark} {label}";
        }

        private static string BuildLawRequirementMessage(string lawCode)
        {
            return $"⛔ Потрібен закон «{PublicSystemsEngine.GetLawDisplayNameUk(lawCode)}».";
        }

        private static HashSet<string> BuildAcceptedLawCodes(int countryId)
        {
            var laws = LawsData.GetByCountryAndStatus(countryId, AcceptedLawStatus);
            return PublicSystemsEngine.BuildLawCodeSet(laws);
        }

        private static string ProgramTypeToToken(string programType)
        {
            return PublicSystemsEngine.NormalizeProgramType(programType).ToLowerInvariant();
        }

        private static string ProgramTokenToType(string token)
        {
            string t = (token ?? string.Empty).Trim().ToUpperInvariant();
            return t switch
            {
                "HEALTH_EXCHANGE" => PublicSystemsEngine.ProgramHealthExchange,
                "HEALTH_DATA_SHARING" => PublicSystemsEngine.ProgramHealthDataSharing,
                "EDU_STUDENT_EXCHANGE" => PublicSystemsEngine.ProgramEduStudentExchange,
                "EDU_JOINT_RESEARCH" => PublicSystemsEngine.ProgramEduJointResearch,
                _ => string.Empty
            };
        }

        private const string PublicRouteLegacyBudget = "legacy_budget";
        private const string PublicRouteAdminHealthBudget = "admin_health_budget";
        private const string PublicRouteAdminEducationBudget = "admin_education_budget";

        private static readonly string[] AdminHealthProgramTypes =
        {
            PublicSystemsEngine.ProgramHealthExchange,
            PublicSystemsEngine.ProgramHealthDataSharing
        };

        private static readonly string[] AdminEducationProgramTypes =
        {
            PublicSystemsEngine.ProgramEduStudentExchange,
            PublicSystemsEngine.ProgramEduJointResearch
        };

        private async Task ShowPublicBudgetByRoute(
            ITelegramBotClient botClient,
            long userId,
            long chatId,
            int msgId,
            string route,
            CancellationToken cancellationToken,
            string? statusMessage = null)
        {
            switch (route)
            {
                case PublicRouteAdminHealthBudget:
                    await ShowAdminHealthBudget(botClient, userId, chatId, msgId, cancellationToken, statusMessage);
                    return;
                case PublicRouteAdminEducationBudget:
                    await ShowAdminEducationBudget(botClient, userId, chatId, msgId, cancellationToken, statusMessage);
                    return;
                default:
                    await ShowPublicSystemsBudget(botClient, userId, chatId, msgId, cancellationToken, statusMessage);
                    return;
            }
        }

        private async Task ShowPublicSystemsMenu(
            ITelegramBotClient botClient,
            long userId,
            long chatId,
            int msgId,
            CancellationToken cancellationToken,
            string? statusMessage = null)
        {
            var selectedCountryId = GetSelectedCountryIdForUser(userId);
            if (!selectedCountryId.HasValue)
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "Не знайдено країни. Оберіть країну або призначте ідентифікатор країни персонажу.",
                    StateKeyboardBuilder.PublicOverviewKeyboard(),
                    cancellationToken
                );
                return;
            }

            var country = CountriesData.GetCountryById(selectedCountryId.Value);
            if (country == null)
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "Країну не знайдено.",
                    StateKeyboardBuilder.EconomyMenuKeyboard(userId == masterID),
                    cancellationToken
                );
                return;
            }

            bool canControl = HasPublicControlAccess(userId, selectedCountryId);
            var systems = CountryPublicSystemsData.GetByCountry(country.CountryId);
            var diseases = CountryDiseasesData.GetByCountry(country.CountryId, activeOnly: true);
            var intlPrograms = CountryIntlProgramsData.GetByCountry(country.CountryId, activeOnly: true);
            var lawCodes = BuildAcceptedLawCodes(country.CountryId);

            bool hasHealthMinistry = PublicSystemsEngine.HasLaw(lawCodes, PublicSystemsEngine.LawHealthMinistry);
            bool hasEduMinistry = PublicSystemsEngine.HasLaw(lawCodes, PublicSystemsEngine.LawEduMinistry);
            bool hasIntl = PublicSystemsEngine.HasLaw(lawCodes, PublicSystemsEngine.LawIntlCooperation);

            var sb = new StringBuilder();
            sb.AppendLine($"🏥 МОЗ/МОН/Міжнародні програми: {FormatCountryName(country)}");
            sb.AppendLine($"Тік системи: 1 день (поточна модель часу гри).");
            sb.AppendLine($"Казна: {FmtNumber(country.Treasury)}");
            sb.AppendLine($"Виробничий бонус (Громзд/Грам): {(systems.HealthPublic - 50f) / 200f:+0.###;-0.###;0} / {(systems.EduLiteracy - 50f) / 250f:+0.###;-0.###;0}");
            sb.AppendLine($"Борг публічних бюджетів: {FmtNumber(systems.BudgetDebt)}");
            sb.AppendLine();
            sb.AppendLine("Закони-гейти:");
            sb.AppendLine(BuildLawStatusLine(hasHealthMinistry, "LAW_HEALTH_MINISTRY"));
            sb.AppendLine(BuildLawStatusLine(hasEduMinistry, "LAW_EDU_MINISTRY"));
            sb.AppendLine(BuildLawStatusLine(hasIntl, "LAW_INTL_COOPERATION"));
            sb.AppendLine();
            sb.AppendLine($"Активні хвороби: {FmtNumber(diseases.Count)}");
            sb.AppendLine($"Активні міжнародні програми: {FmtNumber(intlPrograms.Count)}");
            sb.AppendLine($"Режим міжнародного бюджету: {FormatIntlBudgetModeUk(systems.BudgetIntlMode)}");

            if (!string.IsNullOrWhiteSpace(statusMessage))
            {
                sb.AppendLine();
                sb.AppendLine(statusMessage);
            }

            EconomyPublicModuleRouteByUser[userId] = PublicRouteLegacyBudget;
            EconomyPublicUiContext[userId] = (chatId, msgId);
            await SafeEditMessageTextAsync(
                botClient,
                chatId,
                msgId,
                sb.ToString(),
                StateKeyboardBuilder.PublicSystemsMenuKeyboard(canControl),
                cancellationToken
            );
        }

        private async Task ShowPublicSystemsReport(
            ITelegramBotClient botClient,
            long userId,
            long chatId,
            int msgId,
            CancellationToken cancellationToken,
            string? statusMessage = null)
        {
            var selectedCountryId = GetSelectedCountryIdForUser(userId);
            if (!selectedCountryId.HasValue)
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "Не знайдено країни.",
                    StateKeyboardBuilder.PublicOverviewKeyboard(),
                    cancellationToken
                );
                return;
            }

            var country = CountriesData.GetCountryById(selectedCountryId.Value);
            if (country == null)
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "Країну не знайдено.",
                    StateKeyboardBuilder.EconomyMenuKeyboard(userId == masterID),
                    cancellationToken
                );
                return;
            }

            var systems = CountryPublicSystemsData.GetByCountry(country.CountryId);
            var diseases = CountryDiseasesData.GetByCountry(country.CountryId, activeOnly: true);
            var intlPrograms = CountryIntlProgramsData.GetByCountry(country.CountryId, activeOnly: true);
            var laws = BuildAcceptedLawCodes(country.CountryId);

            bool hasHealthMinistry = PublicSystemsEngine.HasLaw(laws, PublicSystemsEngine.LawHealthMinistry);
            bool hasSanitation = PublicSystemsEngine.HasLaw(laws, PublicSystemsEngine.LawPublicSanitation);
            bool hasQuarantine = PublicSystemsEngine.HasLaw(laws, PublicSystemsEngine.LawQuarantineProtocols);
            bool hasVaccination = PublicSystemsEngine.HasLaw(laws, PublicSystemsEngine.LawNationalImmunization);
            bool hasMedicalLicensing = PublicSystemsEngine.HasLaw(laws, PublicSystemsEngine.LawMedicalLicensing);
            bool hasPharmaAgency = PublicSystemsEngine.HasLaw(laws, PublicSystemsEngine.LawPharmaResearchAgency);
            bool hasClinicalTrials = PublicSystemsEngine.HasLaw(laws, PublicSystemsEngine.LawClinicalTrialProtocols);
            bool hasMedicineReserve = PublicSystemsEngine.HasLaw(laws, PublicSystemsEngine.LawEmergencyMedicineReserve);
            bool hasResidency = PublicSystemsEngine.HasLaw(laws, PublicSystemsEngine.LawHospitalResidencyProgram);
            bool hasEduMinistry = PublicSystemsEngine.HasLaw(laws, PublicSystemsEngine.LawEduMinistry);
            bool hasSchooling = PublicSystemsEngine.HasLaw(laws, PublicSystemsEngine.LawBasicSchooling);
            bool hasHigherEdu = PublicSystemsEngine.HasLaw(laws, PublicSystemsEngine.LawHigherEduCharter);
            bool hasScholarships = PublicSystemsEngine.HasLaw(laws, PublicSystemsEngine.LawScholarshipProgram);
            bool hasIntl = PublicSystemsEngine.HasLaw(laws, PublicSystemsEngine.LawIntlCooperation);
            bool hasIntlHealthData = PublicSystemsEngine.HasLaw(laws, PublicSystemsEngine.LawIntlDataSharingHealth);
            bool hasIntlAcademic = PublicSystemsEngine.HasLaw(laws, PublicSystemsEngine.LawIntlAcademicExchange);

            decimal healthProdBonus = (decimal)((systems.HealthPublic - 50f) / 200f);
            decimal eduProdBonus = (decimal)((systems.EduLiteracy - 50f) / 250f);

            var sb = new StringBuilder();
            sb.AppendLine($"📊 Публічні системи: {FormatCountryName(country)}");
            sb.AppendLine();
            sb.AppendLine("🏥 МОЗ");
            sb.AppendLine($"Громзд/Сан/Спром/Довіра: {systems.HealthPublic:0.#}/{systems.HealthSanitation:0.#}/{systems.HealthCapacity:0.#}/{systems.HealthTrust:0.#}");
            sb.AppendLine($"Бюджет: {FmtNumber(systems.BudgetHealth)} / тік");
            sb.AppendLine($"Виробничий модифікатор від здоров'я: {healthProdBonus:+0.###;-0.###;0}");
            sb.AppendLine($"Карантин: {(systems.QuarantineEnabled ? "увімкнено" : "вимкнено")}");
            sb.AppendLine($"Лікарі/Втома: {FmtNumber(systems.HealthDoctors)} / {systems.HealthDoctorFatigue:0.#}");
            sb.AppendLine($"Запаси ліків: {systems.HealthMedicineStock:0.#}");
            sb.AppendLine($"R&D ліків: {systems.HealthDrugResearchProgress:0.#}% | Рівень: {FmtNumber(systems.HealthDrugLevel)} | Проєкти: {FmtNumber(systems.HealthDrugProjects)}");
            sb.AppendLine($"Клінічні випробування: {FmtNumber(systems.HealthClinicalTrialTicks)} тік.");
            sb.AppendLine();
            sb.AppendLine("🎓 МОН");
            sb.AppendLine($"Грам/Якість/Фах/Згурт: {systems.EduLiteracy:0.#}/{systems.EduQuality:0.#}/{systems.EduSpecialists}/{systems.EduCohesion:0.#}");
            sb.AppendLine($"Бюджет: {FmtNumber(systems.BudgetEducation)} / тік");
            sb.AppendLine($"Виробничий модифікатор від освіти: {eduProdBonus:+0.###;-0.###;0}");
            sb.AppendLine();
            sb.AppendLine("🌍 Міжнародні програми");
            sb.AppendLine($"Престиж: {systems.IntlPrestige:0.#}");
            sb.AppendLine($"Активних програм: {FmtNumber(intlPrograms.Count(p => p.IsActive))}");
            sb.AppendLine($"Міжнародний бюджет: {FmtNumber(systems.BudgetIntl)} / тік ({FormatIntlBudgetModeUk(systems.BudgetIntlMode)})");
            sb.AppendLine($"Борг публічних бюджетів: {FmtNumber(systems.BudgetDebt)}");
            sb.AppendLine();
            sb.AppendLine("🦠 Хвороби");
            if (diseases.Count == 0)
            {
                sb.AppendLine("Активних хвороб немає.");
            }
            else
            {
                foreach (var d in diseases.OrderByDescending(d => d.Severity).ThenByDescending(d => d.Infectiousness).ThenBy(d => d.DiseaseId).Take(8))
                {
                    sb.AppendLine($"• {d.Name}: Тяж {d.Severity}/Зараз {d.Infectiousness}, {FormatPublicStageUk(d.Stage)}, {FmtNumber(d.RemainingTicks)} тік.");
                }
                if (diseases.Count > 8)
                    sb.AppendLine($"…ще {FmtNumber(diseases.Count - 8)}");
            }
            sb.AppendLine();
            sb.AppendLine("Закони:");
            sb.AppendLine(BuildLawStatusLine(hasHealthMinistry, "LAW_HEALTH_MINISTRY"));
            sb.AppendLine(BuildLawStatusLine(hasSanitation, "LAW_PUBLIC_SANITATION"));
            sb.AppendLine(BuildLawStatusLine(hasQuarantine, "LAW_QUARANTINE_PROTOCOLS"));
            sb.AppendLine(BuildLawStatusLine(hasVaccination, "LAW_NATIONAL_IMMUNIZATION"));
            sb.AppendLine(BuildLawStatusLine(hasMedicalLicensing, "LAW_MEDICAL_LICENSING"));
            sb.AppendLine(BuildLawStatusLine(hasResidency, "LAW_HOSPITAL_RESIDENCY_PROGRAM"));
            sb.AppendLine(BuildLawStatusLine(hasPharmaAgency, "LAW_PHARMA_RESEARCH_AGENCY"));
            sb.AppendLine(BuildLawStatusLine(hasClinicalTrials, "LAW_CLINICAL_TRIAL_PROTOCOLS"));
            sb.AppendLine(BuildLawStatusLine(hasMedicineReserve, "LAW_EMERGENCY_MEDICINE_RESERVE"));
            sb.AppendLine(BuildLawStatusLine(hasEduMinistry, "LAW_EDU_MINISTRY"));
            sb.AppendLine(BuildLawStatusLine(hasSchooling, "LAW_BASIC_SCHOOLING"));
            sb.AppendLine(BuildLawStatusLine(hasHigherEdu, "LAW_HIGHER_EDU_CHARTER"));
            sb.AppendLine(BuildLawStatusLine(hasScholarships, "LAW_SCHOLARSHIP_PROGRAM"));
            sb.AppendLine(BuildLawStatusLine(hasIntl, "LAW_INTL_COOPERATION"));
            sb.AppendLine(BuildLawStatusLine(hasIntlHealthData, "LAW_INTL_DATA_SHARING_HEALTH"));
            sb.AppendLine(BuildLawStatusLine(hasIntlAcademic, "LAW_INTL_ACADEMIC_EXCHANGE"));

            if (!string.IsNullOrWhiteSpace(statusMessage))
            {
                sb.AppendLine();
                sb.AppendLine(statusMessage);
            }

            EconomyPublicModuleRouteByUser[userId] = PublicRouteLegacyBudget;
            EconomyPublicUiContext[userId] = (chatId, msgId);
            await SafeEditMessageTextAsync(
                botClient,
                chatId,
                msgId,
                sb.ToString(),
                StateKeyboardBuilder.PublicSystemsMenuKeyboard(HasPublicControlAccess(userId, selectedCountryId)),
                cancellationToken
            );
        }

        private async Task ShowPublicSystemsBudget(
            ITelegramBotClient botClient,
            long userId,
            long chatId,
            int msgId,
            CancellationToken cancellationToken,
            string? statusMessage = null)
        {
            var selectedCountryId = GetSelectedCountryIdForUser(userId);
            if (!selectedCountryId.HasValue)
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "Не знайдено країни.",
                    StateKeyboardBuilder.PublicOverviewKeyboard(),
                    cancellationToken
                );
                return;
            }

            var country = CountriesData.GetCountryById(selectedCountryId.Value);
            if (country == null)
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "Країну не знайдено.",
                    StateKeyboardBuilder.EconomyMenuKeyboard(userId == masterID),
                    cancellationToken
                );
                return;
            }

            bool canControl = HasPublicControlAccess(userId, selectedCountryId);
            var systems = CountryPublicSystemsData.GetByCountry(country.CountryId);
            string intlMode = PublicSystemsEngine.NormalizeIntlBudgetMode(systems.BudgetIntlMode);

            var sb = new StringBuilder();
            sb.AppendLine($"💸 Публічні бюджети: {FormatCountryName(country)}");
            sb.AppendLine($"Казна: {FmtNumber(country.Treasury)}");
            sb.AppendLine($"МОЗ: {FmtNumber(systems.BudgetHealth)} / тік");
            sb.AppendLine($"МОН: {FmtNumber(systems.BudgetEducation)} / тік");
            sb.AppendLine($"Міжнародний бюджет: {FmtNumber(systems.BudgetIntl)} / тік");
            sb.AppendLine($"Режим міжнародного бюджету: {FormatIntlBudgetModeUk(intlMode)}");
            sb.AppendLine($"Борг публічних бюджетів: {FmtNumber(systems.BudgetDebt)}");
            sb.AppendLine();
            sb.AppendLine("Командами також можна керувати:");
            sb.AppendLine("`set_budget(health, 1000)`");
            sb.AppendLine("`set_budget(education, 1000)`");
            sb.AppendLine("`set_budget_intl(auto|manual|авто|вручну)`");

            if (!string.IsNullOrWhiteSpace(statusMessage))
            {
                sb.AppendLine();
                sb.AppendLine(statusMessage);
            }

            EconomyPublicModuleRouteByUser[userId] = PublicRouteLegacyBudget;
            EconomyPublicModuleRouteByUser[userId] = PublicRouteLegacyBudget;
            EconomyPublicUiContext[userId] = (chatId, msgId);
            await SafeEditMessageTextAsync(
                botClient,
                chatId,
                msgId,
                sb.ToString(),
                StateKeyboardBuilder.PublicSystemsBudgetKeyboard(
                    intlAuto: intlMode.Equals("Auto", StringComparison.OrdinalIgnoreCase),
                    canControl: canControl),
                cancellationToken
            );
        }

        private async Task StartPublicBudgetInput(
            ITelegramBotClient botClient,
            long userId,
            long chatId,
            int msgId,
            EconomyInputMode mode,
            string prompt,
            string route,
            CancellationToken cancellationToken)
        {
            var selectedCountryId = GetSelectedCountryIdForUser(userId);
            if (!HasPublicControlAccess(userId, selectedCountryId))
            {
                await ShowPublicBudgetByRoute(botClient, userId, chatId, msgId, route, cancellationToken, "Нема доступу до зміни бюджетів.");
                return;
            }

            EconomyPublicUiContext[userId] = (chatId, msgId);
            EconomyPublicModuleRouteByUser[userId] = string.IsNullOrWhiteSpace(route) ? PublicRouteLegacyBudget : route;
            EconomyInputState[userId] = mode;
            EconomyInputStep[userId] = 0;
            await SendHelperMessageAsync(botClient, userId, prompt, cancellationToken);
        }

        private async Task HandlePublicBudgetInput(
            ITelegramBotClient botClient,
            long userId,
            string input,
            EconomyInputMode mode,
            CancellationToken cancellationToken)
        {
            if (!TryParseDecimal(input, out var amount) || amount < 0m)
            {
                await SendHelperMessageAsync(botClient, userId, "❌ Вкажіть невід'ємне число. Приклад: 1500", cancellationToken);
                return;
            }

            var selectedCountryId = GetSelectedCountryIdForUser(userId);
            if (!HasPublicControlAccess(userId, selectedCountryId))
            {
                await SendHelperMessageAsync(botClient, userId, "Нема доступу.", cancellationToken);
                return;
            }

            if (!selectedCountryId.HasValue)
            {
                await SendHelperMessageAsync(botClient, userId, "Не знайдено країни.", cancellationToken);
                return;
            }

            var systems = CountryPublicSystemsData.GetByCountry(selectedCountryId.Value);
            decimal normalized = decimal.Round(amount, 2, MidpointRounding.AwayFromZero);

            string label = mode switch
            {
                EconomyInputMode.PublicBudgetHealth => "МОЗ",
                EconomyInputMode.PublicBudgetEducation => "МОН",
                EconomyInputMode.PublicBudgetIntl => "міжнародних програм",
                _ => "бюджет"
            };

            switch (mode)
            {
                case EconomyInputMode.PublicBudgetHealth:
                    systems.BudgetHealth = normalized;
                    break;
                case EconomyInputMode.PublicBudgetEducation:
                    systems.BudgetEducation = normalized;
                    break;
                case EconomyInputMode.PublicBudgetIntl:
                    systems.BudgetIntl = normalized;
                    break;
                default:
                    await SendHelperMessageAsync(botClient, userId, "❌ Невідомий режим вводу бюджету.", cancellationToken);
                    return;
            }

            CountryPublicSystemsData.Upsert(systems);
            EconomyPublicUiContext.TryGetValue(userId, out var ui);
            string route = EconomyPublicModuleRouteByUser.GetValueOrDefault(userId, PublicRouteLegacyBudget);

            await CleanupHelperMessagesAsync(botClient, userId, cancellationToken);
            ClearEconomyInput(userId);

            string status = $"✅ Бюджет {label} оновлено: {FmtNumber(normalized)} / тік.";
            if (ui.ChatId != 0 && ui.MsgId != 0)
            {
                await ShowPublicBudgetByRoute(botClient, userId, ui.ChatId, ui.MsgId, route, cancellationToken, status);
                return;
            }

            await botClient.SendTextMessageAsync(userId, status, cancellationToken: cancellationToken);
        }

        private async Task TogglePublicIntlBudgetMode(
            ITelegramBotClient botClient,
            long userId,
            long chatId,
            int msgId,
            CancellationToken cancellationToken)
        {
            string currentRoute = EconomyPublicModuleRouteByUser.GetValueOrDefault(userId, PublicRouteLegacyBudget);
            var selectedCountryId = GetSelectedCountryIdForUser(userId);
            if (!HasPublicControlAccess(userId, selectedCountryId))
            {
                await ShowPublicBudgetByRoute(botClient, userId, chatId, msgId, currentRoute, cancellationToken, "Нема доступу.");
                return;
            }

            if (!selectedCountryId.HasValue)
            {
                await ShowPublicBudgetByRoute(botClient, userId, chatId, msgId, currentRoute, cancellationToken, "Не знайдено країни.");
                return;
            }

            var systems = CountryPublicSystemsData.GetByCountry(selectedCountryId.Value);
            systems.BudgetIntlMode = systems.BudgetIntlMode.Equals("Auto", StringComparison.OrdinalIgnoreCase)
                ? "Manual"
                : "Auto";
            CountryPublicSystemsData.Upsert(systems);
            await ShowPublicBudgetByRoute(
                botClient,
                userId,
                chatId,
                msgId,
                currentRoute,
                cancellationToken,
                $"✅ Режим міжнародного бюджету змінено на {FormatIntlBudgetModeUk(systems.BudgetIntlMode)}.");
        }

        private async Task ShowHealthActionsByRoute(
            ITelegramBotClient botClient,
            long userId,
            long chatId,
            int msgId,
            CancellationToken cancellationToken,
            string? statusMessage = null)
        {
            string route = EconomyPublicModuleRouteByUser.GetValueOrDefault(userId, PublicRouteLegacyBudget);
            if (route == PublicRouteAdminHealthBudget)
            {
                await ShowAdminHealthActions(botClient, userId, chatId, msgId, cancellationToken, statusMessage);
                return;
            }

            await ShowPublicSystemsActions(botClient, userId, chatId, msgId, cancellationToken, statusMessage);
        }

        private async Task ShowPublicSystemsActions(
            ITelegramBotClient botClient,
            long userId,
            long chatId,
            int msgId,
            CancellationToken cancellationToken,
            string? statusMessage = null)
        {
            var selectedCountryId = GetSelectedCountryIdForUser(userId);
            if (!HasPublicControlAccess(userId, selectedCountryId))
            {
                await ShowPublicSystemsMenu(botClient, userId, chatId, msgId, cancellationToken, "Нема доступу.");
                return;
            }

            if (!selectedCountryId.HasValue)
            {
                await ShowPublicSystemsMenu(botClient, userId, chatId, msgId, cancellationToken, "Не знайдено країни.");
                return;
            }

            var systems = CountryPublicSystemsData.GetByCountry(selectedCountryId.Value);
            var lawCodes = BuildAcceptedLawCodes(selectedCountryId.Value);
            bool hasHealthMinistry = PublicSystemsEngine.HasLaw(lawCodes, PublicSystemsEngine.LawHealthMinistry);
            bool hasSanitationLaw = PublicSystemsEngine.HasLaw(lawCodes, PublicSystemsEngine.LawPublicSanitation);
            bool hasQuarantineProtocols = PublicSystemsEngine.HasLaw(lawCodes, PublicSystemsEngine.LawQuarantineProtocols);
            bool hasVaccinationLaw = PublicSystemsEngine.HasLaw(lawCodes, PublicSystemsEngine.LawNationalImmunization);
            bool hasOutreachLaw = PublicSystemsEngine.HasLaw(lawCodes, PublicSystemsEngine.LawCommunityHealthOutreach);
            bool hasMedicalLicensing = PublicSystemsEngine.HasLaw(lawCodes, PublicSystemsEngine.LawMedicalLicensing);
            bool hasPharmaAgency = PublicSystemsEngine.HasLaw(lawCodes, PublicSystemsEngine.LawPharmaResearchAgency);
            bool hasClinicalTrials = PublicSystemsEngine.HasLaw(lawCodes, PublicSystemsEngine.LawClinicalTrialProtocols);
            bool hasMedicineReserve = PublicSystemsEngine.HasLaw(lawCodes, PublicSystemsEngine.LawEmergencyMedicineReserve);
            bool hasResidency = PublicSystemsEngine.HasLaw(lawCodes, PublicSystemsEngine.LawHospitalResidencyProgram);

            var sb = new StringBuilder();
            sb.AppendLine("🛠 Дії під час хвороб");
            sb.AppendLine(BuildLawStatusLine(hasHealthMinistry, "LAW_HEALTH_MINISTRY"));
            sb.AppendLine(BuildLawStatusLine(hasSanitationLaw, "LAW_PUBLIC_SANITATION"));
            sb.AppendLine(BuildLawStatusLine(hasQuarantineProtocols, "LAW_QUARANTINE_PROTOCOLS"));
            sb.AppendLine(BuildLawStatusLine(hasVaccinationLaw, "LAW_NATIONAL_IMMUNIZATION"));
            sb.AppendLine(BuildLawStatusLine(hasOutreachLaw, "LAW_COMMUNITY_HEALTH_OUTREACH"));
            sb.AppendLine(BuildLawStatusLine(hasMedicalLicensing, "LAW_MEDICAL_LICENSING"));
            sb.AppendLine(BuildLawStatusLine(hasResidency, "LAW_HOSPITAL_RESIDENCY_PROGRAM"));
            sb.AppendLine(BuildLawStatusLine(hasPharmaAgency, "LAW_PHARMA_RESEARCH_AGENCY"));
            sb.AppendLine(BuildLawStatusLine(hasClinicalTrials, "LAW_CLINICAL_TRIAL_PROTOCOLS"));
            sb.AppendLine(BuildLawStatusLine(hasMedicineReserve, "LAW_EMERGENCY_MEDICINE_RESERVE"));
            sb.AppendLine();
            sb.AppendLine($"Карантин: {(systems.QuarantineEnabled ? "увімкнено" : "вимкнено")}");
            sb.AppendLine($"Мобілізація медиків: {FmtNumber(systems.MedicMobilizationTicks)} тік.");
            sb.AppendLine($"Захист вакцинацією: {FmtNumber(systems.VaccinationProtectionTicks)} тік.");
            sb.AppendLine($"Лікарі/Втома: {FmtNumber(systems.HealthDoctors)} / {systems.HealthDoctorFatigue:0.#}");
            sb.AppendLine($"Запаси ліків: {systems.HealthMedicineStock:0.#}");
            sb.AppendLine($"R&D ліків: {systems.HealthDrugResearchProgress:0.#}% | Рівень: {FmtNumber(systems.HealthDrugLevel)} | Проєкти: {FmtNumber(systems.HealthDrugProjects)}");
            sb.AppendLine($"Клінічні випробування: {FmtNumber(systems.HealthClinicalTrialTicks)} тік.");

            if (!string.IsNullOrWhiteSpace(statusMessage))
            {
                sb.AppendLine();
                sb.AppendLine(statusMessage);
            }

            EconomyPublicModuleRouteByUser[userId] = PublicRouteLegacyBudget;
            EconomyPublicModuleRouteByUser[userId] = PublicRouteLegacyBudget;
            EconomyPublicUiContext[userId] = (chatId, msgId);
            await SafeEditMessageTextAsync(
                botClient,
                chatId,
                msgId,
                sb.ToString(),
                StateKeyboardBuilder.PublicSystemsActionsKeyboard(),
                cancellationToken
            );
        }

        private async Task TogglePublicQuarantine(
            ITelegramBotClient botClient,
            long userId,
            long chatId,
            int msgId,
            CancellationToken cancellationToken)
        {
            var selectedCountryId = GetSelectedCountryIdForUser(userId);
            if (!HasPublicControlAccess(userId, selectedCountryId))
            {
                await ShowHealthActionsByRoute(botClient, userId, chatId, msgId, cancellationToken, "Нема доступу.");
                return;
            }

            if (!selectedCountryId.HasValue)
            {
                await ShowHealthActionsByRoute(botClient, userId, chatId, msgId, cancellationToken, "Не знайдено країни.");
                return;
            }

            var lawCodes = BuildAcceptedLawCodes(selectedCountryId.Value);
            if (!PublicSystemsEngine.HasLaw(lawCodes, PublicSystemsEngine.LawQuarantineProtocols))
            {
                await ShowHealthActionsByRoute(botClient, userId, chatId, msgId, cancellationToken, BuildLawRequirementMessage(PublicSystemsEngine.LawQuarantineProtocols));
                return;
            }

            var systems = CountryPublicSystemsData.GetByCountry(selectedCountryId.Value);
            systems.QuarantineEnabled = !systems.QuarantineEnabled;
            CountryPublicSystemsData.Upsert(systems);
            await ShowHealthActionsByRoute(
                botClient,
                userId,
                chatId,
                msgId,
                cancellationToken,
                systems.QuarantineEnabled ? "✅ Карантин увімкнено." : "✅ Карантин вимкнено.");
        }

        private async Task ApplyPublicMedicMobilization(
            ITelegramBotClient botClient,
            long userId,
            long chatId,
            int msgId,
            CancellationToken cancellationToken)
        {
            var selectedCountryId = GetSelectedCountryIdForUser(userId);
            if (!HasPublicControlAccess(userId, selectedCountryId))
            {
                await ShowHealthActionsByRoute(botClient, userId, chatId, msgId, cancellationToken, "Нема доступу.");
                return;
            }

            if (!selectedCountryId.HasValue)
            {
                await ShowHealthActionsByRoute(botClient, userId, chatId, msgId, cancellationToken, "Не знайдено країни.");
                return;
            }

            var lawCodes = BuildAcceptedLawCodes(selectedCountryId.Value);
            if (!PublicSystemsEngine.HasLaw(lawCodes, PublicSystemsEngine.LawHealthMinistry))
            {
                await ShowHealthActionsByRoute(botClient, userId, chatId, msgId, cancellationToken, BuildLawRequirementMessage(PublicSystemsEngine.LawHealthMinistry));
                return;
            }

            var systems = CountryPublicSystemsData.GetByCountry(selectedCountryId.Value);
            decimal emergencyCost = Math.Max(0m, EconomyConfigData.GetDecimal("PublicMedicMobilizationCost", 350m));
            systems.MedicMobilizationTicks = Math.Max(systems.MedicMobilizationTicks, 2);
            systems.PendingEmergencyHealthSpend += emergencyCost;
            CountryPublicSystemsData.Upsert(systems);

            await ShowHealthActionsByRoute(
                botClient,
                userId,
                chatId,
                msgId,
                cancellationToken,
                $"✅ Мобілізацію медиків активовано на 2 тіка. Додаткові витрати: {FmtNumber(emergencyCost)}.");
        }

        private async Task ApplyPublicTrustCampaign(
            ITelegramBotClient botClient,
            long userId,
            long chatId,
            int msgId,
            CancellationToken cancellationToken)
        {
            var selectedCountryId = GetSelectedCountryIdForUser(userId);
            if (!HasPublicControlAccess(userId, selectedCountryId))
            {
                await ShowHealthActionsByRoute(botClient, userId, chatId, msgId, cancellationToken, "Нема доступу.");
                return;
            }

            if (!selectedCountryId.HasValue)
            {
                await ShowHealthActionsByRoute(botClient, userId, chatId, msgId, cancellationToken, "Не знайдено країни.");
                return;
            }

            var country = CountriesData.GetCountryById(selectedCountryId.Value);
            if (country == null)
            {
                await ShowHealthActionsByRoute(botClient, userId, chatId, msgId, cancellationToken, "Країну не знайдено.");
                return;
            }

            var lawCodes = BuildAcceptedLawCodes(selectedCountryId.Value);
            if (!PublicSystemsEngine.HasLaw(lawCodes, PublicSystemsEngine.LawHealthMinistry))
            {
                await ShowHealthActionsByRoute(botClient, userId, chatId, msgId, cancellationToken, BuildLawRequirementMessage(PublicSystemsEngine.LawHealthMinistry));
                return;
            }

            var systems = CountryPublicSystemsData.GetByCountry(selectedCountryId.Value);
            float trustGain = country.Happiness < 35f || country.Treasury < 0 ? 2f : 3f;
            decimal campaignCost = Math.Max(0m, EconomyConfigData.GetDecimal("PublicTrustCampaignCost", 200m));
            systems.HealthTrust = EconomyCalculator.Clamp(systems.HealthTrust + trustGain, 0f, 100f);
            systems.PendingEmergencyHealthSpend += campaignCost;
            CountryPublicSystemsData.Upsert(systems);

            await ShowHealthActionsByRoute(
                botClient,
                userId,
                chatId,
                msgId,
                cancellationToken,
                $"✅ Кампанію довіри проведено: Довіра МОЗ +{trustGain:0.#}, витрати {FmtNumber(campaignCost)}.");
        }

        private async Task ApplyPublicVaccination(
            ITelegramBotClient botClient,
            long userId,
            long chatId,
            int msgId,
            CancellationToken cancellationToken)
        {
            var selectedCountryId = GetSelectedCountryIdForUser(userId);
            if (!HasPublicControlAccess(userId, selectedCountryId))
            {
                await ShowHealthActionsByRoute(botClient, userId, chatId, msgId, cancellationToken, "Нема доступу.");
                return;
            }

            if (!selectedCountryId.HasValue)
            {
                await ShowHealthActionsByRoute(botClient, userId, chatId, msgId, cancellationToken, "Не знайдено країни.");
                return;
            }

            var lawCodes = BuildAcceptedLawCodes(selectedCountryId.Value);
            if (!PublicSystemsEngine.HasLaw(lawCodes, PublicSystemsEngine.LawNationalImmunization))
            {
                await ShowHealthActionsByRoute(botClient, userId, chatId, msgId, cancellationToken, BuildLawRequirementMessage(PublicSystemsEngine.LawNationalImmunization));
                return;
            }

            var systems = CountryPublicSystemsData.GetByCountry(selectedCountryId.Value);
            if (systems.HealthTrust < 40f)
            {
                await ShowHealthActionsByRoute(botClient, userId, chatId, msgId, cancellationToken, "⛔ Для вакцинації потрібна довіра МОЗ >= 40.");
                return;
            }

            var diseases = CountryDiseasesData.GetByCountry(selectedCountryId.Value, activeOnly: true);
            int affected = 0;
            foreach (var disease in diseases)
            {
                if (!disease.Active)
                    continue;

                disease.RemainingTicks = Math.Max(0, disease.RemainingTicks - Math.Max(1, disease.DurationTicks / 2));
                disease.Stage = Math.Max(disease.Stage, 3);
                if (disease.RemainingTicks <= 0)
                {
                    disease.Active = false;
                    disease.Stage = 0;
                    disease.RemainingTicks = 0;
                }
                CountryDiseasesData.Upsert(disease);
                affected++;
            }

            decimal vaccinationCost = Math.Max(0m, EconomyConfigData.GetDecimal("PublicVaccinationCost", 450m));
            systems.VaccinationProtectionTicks = Math.Max(systems.VaccinationProtectionTicks, 8);
            systems.PendingEmergencyHealthSpend += vaccinationCost;
            CountryPublicSystemsData.Upsert(systems);

            string status = affected > 0
                ? $"✅ Вакцинацію проведено. Оброблено хвороб: {FmtNumber(affected)}. Витрати: {FmtNumber(vaccinationCost)}."
                : $"✅ Вакцинацію профілактично активовано на {FmtNumber(systems.VaccinationProtectionTicks)} тік. Витрати: {FmtNumber(vaccinationCost)}.";

            await ShowHealthActionsByRoute(botClient, userId, chatId, msgId, cancellationToken, status);
        }

        private async Task ApplyPublicWastewaterSurveillance(
            ITelegramBotClient botClient,
            long userId,
            long chatId,
            int msgId,
            CancellationToken cancellationToken)
        {
            var selectedCountryId = GetSelectedCountryIdForUser(userId);
            if (!HasPublicControlAccess(userId, selectedCountryId))
            {
                await ShowHealthActionsByRoute(botClient, userId, chatId, msgId, cancellationToken, "Нема доступу.");
                return;
            }

            if (!selectedCountryId.HasValue)
            {
                await ShowHealthActionsByRoute(botClient, userId, chatId, msgId, cancellationToken, "Не знайдено країни.");
                return;
            }

            var lawCodes = BuildAcceptedLawCodes(selectedCountryId.Value);
            if (!PublicSystemsEngine.HasLaw(lawCodes, PublicSystemsEngine.LawPublicSanitation))
            {
                await ShowHealthActionsByRoute(botClient, userId, chatId, msgId, cancellationToken, BuildLawRequirementMessage(PublicSystemsEngine.LawPublicSanitation));
                return;
            }

            bool hasIntlDataSharing = PublicSystemsEngine.HasLaw(lawCodes, PublicSystemsEngine.LawIntlDataSharingHealth);
            var systems = CountryPublicSystemsData.GetByCountry(selectedCountryId.Value);
            var diseases = CountryDiseasesData.GetByCountry(selectedCountryId.Value, activeOnly: true);
            decimal cost = Math.Max(0m, EconomyConfigData.GetDecimal("PublicHealthWastewaterSurveillanceCost", 280m));

            float sanitationGain = diseases.Count > 0 ? 3f : 2f;
            float capacityGain = diseases.Count > 0 ? 2f : 1f;
            float trustGain = hasIntlDataSharing ? 1.5f : 1f;

            int affected = 0;
            foreach (var disease in diseases)
            {
                if (!disease.Active)
                    continue;

                disease.RemainingTicks = Math.Max(0, disease.RemainingTicks - 1);
                if (disease.Stage > 0 && disease.Stage < 3)
                    disease.Stage++;
                if (disease.RemainingTicks <= 0)
                {
                    disease.Active = false;
                    disease.Stage = 0;
                }
                CountryDiseasesData.Upsert(disease);
                affected++;
            }

            systems.HealthSanitation = EconomyCalculator.Clamp(systems.HealthSanitation + sanitationGain, 0f, 100f);
            systems.HealthCapacity = EconomyCalculator.Clamp(systems.HealthCapacity + capacityGain, 0f, 100f);
            systems.HealthTrust = EconomyCalculator.Clamp(systems.HealthTrust + trustGain, 0f, 100f);
            systems.PendingEmergencyHealthSpend += cost;
            CountryPublicSystemsData.Upsert(systems);

            string note = hasIntlDataSharing ? " + бонус за обмін епідданими" : string.Empty;
            await ShowHealthActionsByRoute(
                botClient,
                userId,
                chatId,
                msgId,
                cancellationToken,
                $"✅ Епідмоніторинг стоків: Сан +{sanitationGain:0.#}, Потуж +{capacityGain:0.#}, Довіра +{trustGain:0.#}, оброблено хвороб {FmtNumber(affected)}, витрати {FmtNumber(cost)}.{note}");
        }

        private async Task ApplyPublicTelemedicineTriage(
            ITelegramBotClient botClient,
            long userId,
            long chatId,
            int msgId,
            CancellationToken cancellationToken)
        {
            var selectedCountryId = GetSelectedCountryIdForUser(userId);
            if (!HasPublicControlAccess(userId, selectedCountryId))
            {
                await ShowHealthActionsByRoute(botClient, userId, chatId, msgId, cancellationToken, "Нема доступу.");
                return;
            }

            if (!selectedCountryId.HasValue)
            {
                await ShowHealthActionsByRoute(botClient, userId, chatId, msgId, cancellationToken, "Не знайдено країни.");
                return;
            }

            var lawCodes = BuildAcceptedLawCodes(selectedCountryId.Value);
            if (!PublicSystemsEngine.HasLaw(lawCodes, PublicSystemsEngine.LawHealthMinistry))
            {
                await ShowHealthActionsByRoute(botClient, userId, chatId, msgId, cancellationToken, BuildLawRequirementMessage(PublicSystemsEngine.LawHealthMinistry));
                return;
            }

            var systems = CountryPublicSystemsData.GetByCountry(selectedCountryId.Value);
            var diseases = CountryDiseasesData.GetByCountry(selectedCountryId.Value, activeOnly: true)
                .OrderByDescending(d => d.Severity)
                .ThenByDescending(d => d.Infectiousness)
                .ToList();

            decimal cost = Math.Max(0m, EconomyConfigData.GetDecimal("PublicHealthTelemedCost", 240m));
            float capacityGain = systems.MedicMobilizationTicks > 0 ? 3f : 2f;
            float trustGain = systems.HealthTrust < 45f ? 2f : 1f;
            float publicGain = diseases.Count > 0 ? 1f : 0.5f;

            int triaged = 0;
            foreach (var disease in diseases.Take(2))
            {
                if (!disease.Active)
                    continue;

                disease.RemainingTicks = Math.Max(0, disease.RemainingTicks - 1);
                if (disease.Stage > 0 && disease.Stage < 3)
                    disease.Stage++;
                if (disease.RemainingTicks <= 0)
                {
                    disease.Active = false;
                    disease.Stage = 0;
                }
                CountryDiseasesData.Upsert(disease);
                triaged++;
            }

            systems.HealthCapacity = EconomyCalculator.Clamp(systems.HealthCapacity + capacityGain, 0f, 100f);
            systems.HealthTrust = EconomyCalculator.Clamp(systems.HealthTrust + trustGain, 0f, 100f);
            systems.HealthPublic = EconomyCalculator.Clamp(systems.HealthPublic + publicGain, 0f, 100f);
            systems.PendingEmergencyHealthSpend += cost;
            CountryPublicSystemsData.Upsert(systems);

            await ShowHealthActionsByRoute(
                botClient,
                userId,
                chatId,
                msgId,
                cancellationToken,
                $"✅ Телемедицина й тріаж: Потуж +{capacityGain:0.#}, Довіра +{trustGain:0.#}, Рівень здоров'я +{publicGain:0.#}, випадків у тріажі {FmtNumber(triaged)}, витрати {FmtNumber(cost)}.");
        }

        private async Task ApplyPublicDrugResearchProject(
            ITelegramBotClient botClient,
            long userId,
            long chatId,
            int msgId,
            CancellationToken cancellationToken)
        {
            var selectedCountryId = GetSelectedCountryIdForUser(userId);
            if (!HasPublicControlAccess(userId, selectedCountryId))
            {
                await ShowHealthActionsByRoute(botClient, userId, chatId, msgId, cancellationToken, "Нема доступу.");
                return;
            }

            if (!selectedCountryId.HasValue)
            {
                await ShowHealthActionsByRoute(botClient, userId, chatId, msgId, cancellationToken, "Не знайдено країни.");
                return;
            }

            var lawCodes = BuildAcceptedLawCodes(selectedCountryId.Value);
            if (!PublicSystemsEngine.HasLaw(lawCodes, PublicSystemsEngine.LawPharmaResearchAgency))
            {
                await ShowHealthActionsByRoute(
                    botClient,
                    userId,
                    chatId,
                    msgId,
                    cancellationToken,
                    BuildLawRequirementMessage(PublicSystemsEngine.LawPharmaResearchAgency));
                return;
            }

            bool hasClinical = PublicSystemsEngine.HasLaw(lawCodes, PublicSystemsEngine.LawClinicalTrialProtocols);
            bool hasIntlData = PublicSystemsEngine.HasLaw(lawCodes, PublicSystemsEngine.LawIntlDataSharingHealth);
            int maxProjects = 1 + (hasClinical ? 1 : 0) + (hasIntlData ? 1 : 0);

            var systems = CountryPublicSystemsData.GetByCountry(selectedCountryId.Value);
            if (systems.HealthDrugProjects >= maxProjects)
            {
                await ShowHealthActionsByRoute(
                    botClient,
                    userId,
                    chatId,
                    msgId,
                    cancellationToken,
                    $"⛔ Досягнуто ліміт проєктів: {FmtNumber(maxProjects)}.");
                return;
            }

            decimal cost = Math.Max(0m, EconomyConfigData.GetDecimal("PublicHealthDrugResearchCost", 420m));
            float progressBoost = hasClinical ? 6f : 4f;

            systems.HealthDrugProjects = Math.Max(0, systems.HealthDrugProjects + 1);
            systems.HealthDrugResearchProgress = EconomyCalculator.Clamp(systems.HealthDrugResearchProgress + progressBoost, 0f, 99.99f);
            systems.PendingEmergencyHealthSpend += cost;
            CountryPublicSystemsData.Upsert(systems);

            await ShowHealthActionsByRoute(
                botClient,
                userId,
                chatId,
                msgId,
                cancellationToken,
                $"✅ Запущено проєкт R&D ліків: Проєкти {FmtNumber(systems.HealthDrugProjects)}/{FmtNumber(maxProjects)}, прогрес +{progressBoost:0.#}%, витрати {FmtNumber(cost)}.");
        }

        private async Task ApplyPublicDoctorTraining(
            ITelegramBotClient botClient,
            long userId,
            long chatId,
            int msgId,
            CancellationToken cancellationToken)
        {
            var selectedCountryId = GetSelectedCountryIdForUser(userId);
            if (!HasPublicControlAccess(userId, selectedCountryId))
            {
                await ShowHealthActionsByRoute(botClient, userId, chatId, msgId, cancellationToken, "Нема доступу.");
                return;
            }

            if (!selectedCountryId.HasValue)
            {
                await ShowHealthActionsByRoute(botClient, userId, chatId, msgId, cancellationToken, "Не знайдено країни.");
                return;
            }

            var lawCodes = BuildAcceptedLawCodes(selectedCountryId.Value);
            if (!PublicSystemsEngine.HasLaw(lawCodes, PublicSystemsEngine.LawMedicalLicensing))
            {
                await ShowHealthActionsByRoute(
                    botClient,
                    userId,
                    chatId,
                    msgId,
                    cancellationToken,
                    BuildLawRequirementMessage(PublicSystemsEngine.LawMedicalLicensing));
                return;
            }

            bool hasResidency = PublicSystemsEngine.HasLaw(lawCodes, PublicSystemsEngine.LawHospitalResidencyProgram);
            var systems = CountryPublicSystemsData.GetByCountry(selectedCountryId.Value);

            decimal cost = Math.Max(0m, EconomyConfigData.GetDecimal("PublicHealthDoctorTrainingCost", 320m));
            int doctorsGain = 4;
            if (systems.EduSpecialists >= 40)
                doctorsGain += 1;
            if (systems.EduSpecialists >= 80)
                doctorsGain += 1;
            if (hasResidency)
                doctorsGain += 2;

            float fatigueDrop = hasResidency ? 6f : 4f;
            float capacityGain = hasResidency ? 2f : 1f;
            float trustGain = systems.HealthTrust < 55f ? 2f : 1f;

            systems.HealthDoctors = Math.Max(0, systems.HealthDoctors + doctorsGain);
            systems.HealthDoctorFatigue = EconomyCalculator.Clamp(systems.HealthDoctorFatigue - fatigueDrop, 0f, 100f);
            systems.HealthCapacity = EconomyCalculator.Clamp(systems.HealthCapacity + capacityGain, 0f, 100f);
            systems.HealthTrust = EconomyCalculator.Clamp(systems.HealthTrust + trustGain, 0f, 100f);
            systems.PendingEmergencyHealthSpend += cost;
            CountryPublicSystemsData.Upsert(systems);

            await ShowHealthActionsByRoute(
                botClient,
                userId,
                chatId,
                msgId,
                cancellationToken,
                $"✅ Програма підготовки лікарів: Лікарі +{FmtNumber(doctorsGain)}, Втома -{fatigueDrop:0.#}, Потуж +{capacityGain:0.#}, Довіра +{trustGain:0.#}, витрати {FmtNumber(cost)}.");
        }

        private async Task ApplyPublicClinicalTrials(
            ITelegramBotClient botClient,
            long userId,
            long chatId,
            int msgId,
            CancellationToken cancellationToken)
        {
            var selectedCountryId = GetSelectedCountryIdForUser(userId);
            if (!HasPublicControlAccess(userId, selectedCountryId))
            {
                await ShowHealthActionsByRoute(botClient, userId, chatId, msgId, cancellationToken, "Нема доступу.");
                return;
            }

            if (!selectedCountryId.HasValue)
            {
                await ShowHealthActionsByRoute(botClient, userId, chatId, msgId, cancellationToken, "Не знайдено країни.");
                return;
            }

            var lawCodes = BuildAcceptedLawCodes(selectedCountryId.Value);
            if (!PublicSystemsEngine.HasLaw(lawCodes, PublicSystemsEngine.LawClinicalTrialProtocols))
            {
                await ShowHealthActionsByRoute(
                    botClient,
                    userId,
                    chatId,
                    msgId,
                    cancellationToken,
                    BuildLawRequirementMessage(PublicSystemsEngine.LawClinicalTrialProtocols));
                return;
            }

            if (!PublicSystemsEngine.HasLaw(lawCodes, PublicSystemsEngine.LawPharmaResearchAgency))
            {
                await ShowHealthActionsByRoute(
                    botClient,
                    userId,
                    chatId,
                    msgId,
                    cancellationToken,
                    BuildLawRequirementMessage(PublicSystemsEngine.LawPharmaResearchAgency));
                return;
            }

            var systems = CountryPublicSystemsData.GetByCountry(selectedCountryId.Value);
            if (systems.HealthDrugProjects <= 0)
            {
                await ShowHealthActionsByRoute(botClient, userId, chatId, msgId, cancellationToken, "⛔ Нема активних фарма-проєктів.");
                return;
            }

            if (systems.HealthTrust < 30f)
            {
                await ShowHealthActionsByRoute(botClient, userId, chatId, msgId, cancellationToken, "⛔ Для клінічних випробувань потрібна довіра МОЗ >= 30.");
                return;
            }

            bool hasIntlData = PublicSystemsEngine.HasLaw(lawCodes, PublicSystemsEngine.LawIntlDataSharingHealth);
            decimal cost = Math.Max(0m, EconomyConfigData.GetDecimal("PublicHealthClinicalTrialsCost", 260m));
            int trialTicks = hasIntlData ? 5 : 4;
            float progressGain = hasIntlData ? 8f : 6f;

            systems.HealthClinicalTrialTicks = Math.Max(systems.HealthClinicalTrialTicks, trialTicks);
            systems.HealthDrugResearchProgress = EconomyCalculator.Clamp(systems.HealthDrugResearchProgress + progressGain, 0f, 99.99f);
            systems.HealthTrust = EconomyCalculator.Clamp(systems.HealthTrust + 1f, 0f, 100f);
            systems.PendingEmergencyHealthSpend += cost;
            CountryPublicSystemsData.Upsert(systems);

            await ShowHealthActionsByRoute(
                botClient,
                userId,
                chatId,
                msgId,
                cancellationToken,
                $"✅ Клінічні випробування активовано на {FmtNumber(trialTicks)} тік: прогрес +{progressGain:0.#}%, витрати {FmtNumber(cost)}.");
        }

        private async Task ApplyAdminHealthSanitationRaids(
            ITelegramBotClient botClient,
            long userId,
            long chatId,
            int msgId,
            CancellationToken cancellationToken)
        {
            var selectedCountryId = GetSelectedCountryIdForUser(userId);
            if (!HasPublicControlAccess(userId, selectedCountryId))
            {
                await ShowAdminHealthActions(botClient, userId, chatId, msgId, cancellationToken, "Нема доступу.");
                return;
            }

            if (!selectedCountryId.HasValue)
            {
                await ShowAdminHealthActions(botClient, userId, chatId, msgId, cancellationToken, "Не знайдено країни.");
                return;
            }

            var lawCodes = BuildAcceptedLawCodes(selectedCountryId.Value);
            if (!PublicSystemsEngine.HasLaw(lawCodes, PublicSystemsEngine.LawCommunityHealthOutreach))
            {
                await ShowAdminHealthActions(
                    botClient,
                    userId,
                    chatId,
                    msgId,
                    cancellationToken,
                    BuildLawRequirementMessage(PublicSystemsEngine.LawCommunityHealthOutreach));
                return;
            }

            var systems = CountryPublicSystemsData.GetByCountry(selectedCountryId.Value);
            var activeDiseases = CountryDiseasesData.GetByCountry(selectedCountryId.Value, activeOnly: true);
            decimal cost = Math.Max(0m, EconomyConfigData.GetDecimal("PublicHealthSanitationRaidCost", 260m));
            float sanitationGain = activeDiseases.Count > 0 ? 4f : 2f;
            float trustGain = activeDiseases.Count > 0 ? 1f : 2f;

            systems.HealthSanitation = EconomyCalculator.Clamp(systems.HealthSanitation + sanitationGain, 0f, 100f);
            systems.HealthTrust = EconomyCalculator.Clamp(systems.HealthTrust + trustGain, 0f, 100f);
            if (activeDiseases.Count == 0)
                systems.HealthPublic = EconomyCalculator.Clamp(systems.HealthPublic + 1f, 0f, 100f);

            systems.PendingEmergencyHealthSpend += cost;
            CountryPublicSystemsData.Upsert(systems);

            await ShowAdminHealthActions(
                botClient,
                userId,
                chatId,
                msgId,
                cancellationToken,
                $"✅ Санітарні рейди: Сан +{sanitationGain:0.#}, Довіра +{trustGain:0.#}, витрати {FmtNumber(cost)}.");
        }

        private async Task ShowPublicDiseases(
            ITelegramBotClient botClient,
            long userId,
            long chatId,
            int msgId,
            CancellationToken cancellationToken,
            string? statusMessage = null)
        {
            var selectedCountryId = GetSelectedCountryIdForUser(userId);
            if (!selectedCountryId.HasValue)
            {
                await ShowPublicSystemsMenu(botClient, userId, chatId, msgId, cancellationToken, "Не знайдено країни.");
                return;
            }

            var diseases = CountryDiseasesData.GetByCountry(selectedCountryId.Value, activeOnly: true)
                .OrderByDescending(d => d.Severity)
                .ThenByDescending(d => d.Infectiousness)
                .ThenBy(d => d.DiseaseId)
                .ToList();

            var items = new List<(string text, string data)>();
            foreach (var d in diseases)
            {
                string text = $"🦠 {d.Name} | Тяж {d.Severity}/Зараз {d.Infectiousness} | {FormatPublicStageUk(d.Stage)} | {FmtNumber(d.RemainingTicks)} тік.";
                items.Add((text, $"state_public_disease_{d.DiseaseId}"));
            }

            var sb = new StringBuilder();
            sb.AppendLine("🦠 Активні хвороби");
            sb.AppendLine($"Кількість: {FmtNumber(diseases.Count)}");
            sb.AppendLine($"Ціль ГМ-створення: {FormatPublicDiseaseTargetUk(PublicGmDiseaseTargetByUser.GetValueOrDefault(userId, "whole_state"))}");
            if (!string.IsNullOrWhiteSpace(statusMessage))
            {
                sb.AppendLine();
                sb.AppendLine(statusMessage);
            }

            EconomyPublicModuleRouteByUser[userId] = PublicRouteLegacyBudget;
            EconomyPublicUiContext[userId] = (chatId, msgId);
            await SafeEditMessageTextAsync(
                botClient,
                chatId,
                msgId,
                sb.ToString(),
                StateKeyboardBuilder.PublicSystemsDiseasesKeyboard(items, canControl: userId == masterID),
                cancellationToken
            );
        }

        private async Task ShowPublicDiseaseInfo(
            ITelegramBotClient botClient,
            long userId,
            long chatId,
            int msgId,
            int diseaseId,
            CancellationToken cancellationToken,
            string? statusMessage = null)
        {
            var selectedCountryId = GetSelectedCountryIdForUser(userId);
            if (!selectedCountryId.HasValue)
            {
                await ShowPublicSystemsMenu(botClient, userId, chatId, msgId, cancellationToken, "Не знайдено країни.");
                return;
            }

            var disease = CountryDiseasesData.GetByCountry(selectedCountryId.Value)
                .FirstOrDefault(d => d.DiseaseId == diseaseId);
            if (disease == null)
            {
                await ShowPublicDiseases(botClient, userId, chatId, msgId, cancellationToken, "Хворобу не знайдено.");
                return;
            }

            float stageFactor = GetPublicStageFactor(disease.Stage);
            float prodPenaltyPct = 100f * 0.02f * disease.Severity * disease.Infectiousness * stageFactor;
            float healthNeedBonus = 8f * disease.Severity * stageFactor;

            var sb = new StringBuilder();
            sb.AppendLine($"🦠 {disease.Name}");
            sb.AppendLine($"Ідентифікатор: {FmtNumber(disease.DiseaseId)}");
            sb.AppendLine($"Стан: {(disease.Active ? "активна" : "завершена")}");
            sb.AppendLine($"Тяжкість / заразність: {FmtNumber(disease.Severity)} / {FmtNumber(disease.Infectiousness)}");
            sb.AppendLine($"Стадія: {FmtNumber(disease.Stage)} ({FormatPublicStageUk(disease.Stage)})");
            sb.AppendLine($"Тривалість / залишок: {FmtNumber(disease.DurationTicks)} / {FmtNumber(disease.RemainingTicks)} тік.");
            sb.AppendLine($"Ціль: {FormatPublicDiseaseTargetUk(disease.TargetScope)}");
            sb.AppendLine($"Штраф до виробництва (оцінка): -{prodPenaltyPct:0.##}%");
            sb.AppendLine($"Бонус потреби в охороні здоров'я (оцінка): +{healthNeedBonus:0.##}");

            if (!string.IsNullOrWhiteSpace(statusMessage))
            {
                sb.AppendLine();
                sb.AppendLine(statusMessage);
            }

            var rows = new List<InlineKeyboardButton[]>();
            if (userId == masterID)
            {
                rows.Add(new[]
                {
                    InlineKeyboardButton.WithCallbackData("⬅ Стадія -1", $"state_public_disease_shift_{disease.DiseaseId}_-1"),
                    InlineKeyboardButton.WithCallbackData("➡ Стадія +1", $"state_public_disease_shift_{disease.DiseaseId}_1")
                });
                if (disease.Active)
                {
                    rows.Add(new[]
                    {
                        InlineKeyboardButton.WithCallbackData("🛑 Завершити (ГМ)", $"state_public_disease_end_{disease.DiseaseId}")
                    });
                }
            }
            rows.Add(new[]
            {
                InlineKeyboardButton.WithCallbackData("🔙 До хвороб", "state_public_diseases")
            });

            EconomyPublicUiContext[userId] = (chatId, msgId);
            await SafeEditMessageTextAsync(
                botClient,
                chatId,
                msgId,
                sb.ToString(),
                new InlineKeyboardMarkup(rows),
                cancellationToken
            );
        }

        private async Task ShiftPublicDiseaseStage(
            ITelegramBotClient botClient,
            long userId,
            long chatId,
            int msgId,
            int diseaseId,
            int delta,
            CancellationToken cancellationToken)
        {
            if (userId != masterID)
            {
                await ShowPublicDiseases(botClient, userId, chatId, msgId, cancellationToken, "Нема доступу.");
                return;
            }

            var selectedCountryId = GetSelectedCountryIdForUser(userId);
            if (!selectedCountryId.HasValue)
            {
                await ShowPublicDiseases(botClient, userId, chatId, msgId, cancellationToken, "Не знайдено країни.");
                return;
            }

            var disease = CountryDiseasesData.GetByCountry(selectedCountryId.Value)
                .FirstOrDefault(d => d.DiseaseId == diseaseId);
            if (disease == null)
            {
                await ShowPublicDiseases(botClient, userId, chatId, msgId, cancellationToken, "Хворобу не знайдено.");
                return;
            }

            if (!disease.Active)
            {
                await ShowPublicDiseaseInfo(botClient, userId, chatId, msgId, diseaseId, cancellationToken, "Хвороба вже неактивна.");
                return;
            }

            disease.Stage = Math.Clamp(disease.Stage + delta, 1, 4);
            CountryDiseasesData.Upsert(disease);
            await ShowPublicDiseaseInfo(botClient, userId, chatId, msgId, diseaseId, cancellationToken, $"✅ Стадію змінено на {disease.Stage} ({FormatPublicStageUk(disease.Stage)}).");
        }

        private async Task EndPublicDisease(
            ITelegramBotClient botClient,
            long userId,
            long chatId,
            int msgId,
            int diseaseId,
            CancellationToken cancellationToken)
        {
            if (userId != masterID)
            {
                await ShowPublicDiseases(botClient, userId, chatId, msgId, cancellationToken, "Нема доступу.");
                return;
            }

            var selectedCountryId = GetSelectedCountryIdForUser(userId);
            if (!selectedCountryId.HasValue)
            {
                await ShowPublicDiseases(botClient, userId, chatId, msgId, cancellationToken, "Не знайдено країни.");
                return;
            }

            int affected = CountryDiseasesData.EndDiseaseById(selectedCountryId.Value, diseaseId);
            await ShowPublicDiseases(
                botClient,
                userId,
                chatId,
                msgId,
                cancellationToken,
                affected > 0 ? "✅ Хворобу завершено." : "❌ Хворобу не знайдено або вже завершено.");
        }

        private async Task EndAllPublicDiseases(
            ITelegramBotClient botClient,
            long userId,
            long chatId,
            int msgId,
            CancellationToken cancellationToken)
        {
            if (userId != masterID)
            {
                await ShowPublicDiseases(botClient, userId, chatId, msgId, cancellationToken, "Нема доступу.");
                return;
            }

            var selectedCountryId = GetSelectedCountryIdForUser(userId);
            if (!selectedCountryId.HasValue)
            {
                await ShowPublicDiseases(botClient, userId, chatId, msgId, cancellationToken, "Не знайдено країни.");
                return;
            }

            int ended = 0;
            var diseases = CountryDiseasesData.GetByCountry(selectedCountryId.Value, activeOnly: true);
            foreach (var disease in diseases)
            {
                ended += CountryDiseasesData.EndDiseaseById(selectedCountryId.Value, disease.DiseaseId);
            }

            await ShowPublicDiseases(
                botClient,
                userId,
                chatId,
                msgId,
                cancellationToken,
                $"✅ Завершено хвороб: {FmtNumber(ended)}.");
        }

        private async Task ShowPublicGmSpawnHelp(
            ITelegramBotClient botClient,
            long userId,
            long chatId,
            int msgId,
            CancellationToken cancellationToken)
        {
            if (userId != masterID)
            {
                await ShowPublicDiseases(botClient, userId, chatId, msgId, cancellationToken, "Нема доступу.");
                return;
            }

            var sb = new StringBuilder();
            sb.AppendLine("🎲 ГМ: керування хворобами (через текстові команди)");
            sb.AppendLine("`gm_spawn_disease(name, severity, infectiousness, duration)`");
            sb.AppendLine("`gm_set_disease_stage(name_or_id, stage)`");
            sb.AppendLine("`gm_end_disease(name_or_id)`");
            sb.AppendLine("`gm_trigger_event(panic|scandal|breakthrough|import|паніка|скандал|прорив|завезення)`");
            sb.AppendLine("`gm_disease_target(whole_state|region|city|держава|регіон|місто)`");
            sb.AppendLine();
            sb.AppendLine("Приклад:");
            sb.AppendLine("`gm_spawn_disease(Сіра гарячка, 3, 4, 7)`");

            var keyboard = new InlineKeyboardMarkup(new[]
            {
                new [] { InlineKeyboardButton.WithCallbackData("🔙 До хвороб", "state_public_diseases") }
            });

            EconomyPublicModuleRouteByUser[userId] = PublicRouteLegacyBudget;
            EconomyPublicUiContext[userId] = (chatId, msgId);
            await SafeEditMessageTextAsync(botClient, chatId, msgId, sb.ToString(), keyboard, cancellationToken);
        }

        private async Task ShowPublicIntlPrograms(
            ITelegramBotClient botClient,
            long userId,
            long chatId,
            int msgId,
            CancellationToken cancellationToken,
            string? statusMessage = null)
        {
            var selectedCountryId = GetSelectedCountryIdForUser(userId);
            if (!selectedCountryId.HasValue)
            {
                await ShowPublicSystemsMenu(botClient, userId, chatId, msgId, cancellationToken, "Не знайдено країни.");
                return;
            }

            bool canControl = HasPublicControlAccess(userId, selectedCountryId);
            var systems = CountryPublicSystemsData.GetByCountry(selectedCountryId.Value);
            var programs = CountryIntlProgramsData.GetByCountry(selectedCountryId.Value);
            var existingByType = programs
                .GroupBy(p => PublicSystemsEngine.NormalizeProgramType(p.ProgramType), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.OrderByDescending(x => x.IntlProgramId).First(), StringComparer.OrdinalIgnoreCase);

            var laws = BuildAcceptedLawCodes(selectedCountryId.Value);
            bool hasIntlCoop = PublicSystemsEngine.HasLaw(laws, PublicSystemsEngine.LawIntlCooperation);

            var items = new List<(string text, string data)>();
            foreach (var type in PublicSystemsEngine.AllProgramTypes)
            {
                string display = PublicSystemsEngine.GetProgramDisplayNameUk(type);
                if (existingByType.TryGetValue(type, out var existing))
                {
                    string state = existing.IsActive ? "✅" : "⛔";
                    string cost = FmtNumber(existing.CostPerTick > 0m ? existing.CostPerTick : PublicSystemsEngine.GetDefaultProgramCost(type));
                    string data = canControl ? $"state_public_intl_toggle_{existing.IntlProgramId}" : "state_noop";
                    items.Add(($"{state} {display} • {cost}/тік • Фах {FmtNumber(existing.RequiredSpec)}", data));
                }
                else if (canControl)
                {
                    string data = $"state_public_intl_add_{ProgramTypeToToken(type)}";
                    items.Add(($"➕ {display}", data));
                }
            }

            var sb = new StringBuilder();
            sb.AppendLine("🌍 Міжнародні програми");
            sb.AppendLine($"Престиж: {systems.IntlPrestige:0.#}");
            sb.AppendLine($"Міжнародний бюджет: {FmtNumber(systems.BudgetIntl)} ({FormatIntlBudgetModeUk(systems.BudgetIntlMode)})");
            sb.AppendLine(BuildLawStatusLine(hasIntlCoop, "LAW_INTL_COOPERATION"));
            sb.AppendLine(BuildLawStatusLine(PublicSystemsEngine.HasLaw(laws, PublicSystemsEngine.LawIntlDataSharingHealth), "LAW_INTL_DATA_SHARING_HEALTH"));
            sb.AppendLine(BuildLawStatusLine(PublicSystemsEngine.HasLaw(laws, PublicSystemsEngine.LawIntlAcademicExchange), "LAW_INTL_ACADEMIC_EXCHANGE"));
            sb.AppendLine();
            sb.AppendLine("Натисніть елемент, щоб увімкнути/вимкнути або створити програму.");

            if (!string.IsNullOrWhiteSpace(statusMessage))
            {
                sb.AppendLine();
                sb.AppendLine(statusMessage);
            }

            EconomyPublicModuleRouteByUser[userId] = PublicRouteLegacyBudget;
            EconomyPublicUiContext[userId] = (chatId, msgId);
            await SafeEditMessageTextAsync(
                botClient,
                chatId,
                msgId,
                sb.ToString(),
                StateKeyboardBuilder.PublicSystemsIntlKeyboard(items, canControl),
                cancellationToken
            );
        }

        private async Task TogglePublicIntlProgram(
            ITelegramBotClient botClient,
            long userId,
            long chatId,
            int msgId,
            int intlProgramId,
            CancellationToken cancellationToken)
        {
            var selectedCountryId = GetSelectedCountryIdForUser(userId);
            if (!HasPublicControlAccess(userId, selectedCountryId))
            {
                await ShowPublicIntlPrograms(botClient, userId, chatId, msgId, cancellationToken, "Нема доступу.");
                return;
            }

            if (!selectedCountryId.HasValue)
            {
                await ShowPublicIntlPrograms(botClient, userId, chatId, msgId, cancellationToken, "Не знайдено країни.");
                return;
            }

            var program = CountryIntlProgramsData.GetById(intlProgramId);
            if (program == null || program.CountryId != selectedCountryId.Value)
            {
                await ShowPublicIntlPrograms(botClient, userId, chatId, msgId, cancellationToken, "Програму не знайдено.");
                return;
            }

            bool setActive = !program.IsActive;
            CountryIntlProgramsData.SetActive(selectedCountryId.Value, intlProgramId, setActive);
            await ShowPublicIntlPrograms(
                botClient,
                userId,
                chatId,
                msgId,
                cancellationToken,
                setActive ? "✅ Програму увімкнено." : "✅ Програму вимкнено.");
        }

        private async Task AddPublicIntlProgram(
            ITelegramBotClient botClient,
            long userId,
            long chatId,
            int msgId,
            string token,
            CancellationToken cancellationToken)
        {
            var selectedCountryId = GetSelectedCountryIdForUser(userId);
            if (!HasPublicControlAccess(userId, selectedCountryId))
            {
                await ShowPublicIntlPrograms(botClient, userId, chatId, msgId, cancellationToken, "Нема доступу.");
                return;
            }

            if (!selectedCountryId.HasValue)
            {
                await ShowPublicIntlPrograms(botClient, userId, chatId, msgId, cancellationToken, "Не знайдено країни.");
                return;
            }

            string programType = ProgramTokenToType(token);
            if (string.IsNullOrWhiteSpace(programType) || !PublicSystemsEngine.IsProgramTypeSupported(programType))
            {
                await ShowPublicIntlPrograms(botClient, userId, chatId, msgId, cancellationToken, "Невідомий тип програми.");
                return;
            }

            var laws = BuildAcceptedLawCodes(selectedCountryId.Value);
            if (!PublicSystemsEngine.HasLaw(laws, PublicSystemsEngine.LawIntlCooperation))
            {
                await ShowPublicIntlPrograms(botClient, userId, chatId, msgId, cancellationToken, BuildLawRequirementMessage(PublicSystemsEngine.LawIntlCooperation));
                return;
            }

            if (string.Equals(programType, PublicSystemsEngine.ProgramHealthDataSharing, StringComparison.OrdinalIgnoreCase) &&
                !PublicSystemsEngine.HasLaw(laws, PublicSystemsEngine.LawIntlDataSharingHealth))
            {
                await ShowPublicIntlPrograms(botClient, userId, chatId, msgId, cancellationToken, BuildLawRequirementMessage(PublicSystemsEngine.LawIntlDataSharingHealth));
                return;
            }

            if ((string.Equals(programType, PublicSystemsEngine.ProgramEduStudentExchange, StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(programType, PublicSystemsEngine.ProgramEduJointResearch, StringComparison.OrdinalIgnoreCase)) &&
                !PublicSystemsEngine.HasLaw(laws, PublicSystemsEngine.LawIntlAcademicExchange))
            {
                await ShowPublicIntlPrograms(botClient, userId, chatId, msgId, cancellationToken, BuildLawRequirementMessage(PublicSystemsEngine.LawIntlAcademicExchange));
                return;
            }

            var existing = CountryIntlProgramsData.GetByCountryAndType(selectedCountryId.Value, programType);
            if (existing != null)
            {
                if (!existing.IsActive)
                    CountryIntlProgramsData.SetActive(selectedCountryId.Value, existing.IntlProgramId, true);
                await ShowPublicIntlPrograms(botClient, userId, chatId, msgId, cancellationToken, "✅ Програму активовано.");
                return;
            }

            var record = new CountryIntlProgramsData.CountryIntlProgramRecord
            {
                CountryId = selectedCountryId.Value,
                ProgramType = programType,
                CostPerTick = PublicSystemsEngine.GetDefaultProgramCost(programType),
                RequiredSpec = PublicSystemsEngine.GetDefaultProgramRequiredSpec(programType),
                IsActive = true,
                TicksActive = 0,
                SuccessTicks = 0
            };
            CountryIntlProgramsData.Upsert(record);
            await ShowPublicIntlPrograms(botClient, userId, chatId, msgId, cancellationToken, "✅ Програму додано й активовано.");
        }

        private static float EstimateHealthNeed(CountryRecord country, List<CountryDiseasesData.CountryDiseaseRecord> diseases)
        {
            if (country == null)
                return 0f;

            float need = 30f;
            string season = EconomyConfigData.GetText("CurrentSeason", "Summer");
            if (season.Equals("Winter", StringComparison.OrdinalIgnoreCase))
                need += 10f;
            if (country.WarState)
                need += 10f;
            if (country.Happiness < 35f)
                need += 5f;

            if (diseases != null)
            {
                foreach (var disease in diseases)
                {
                    if (disease == null || !disease.Active)
                        continue;
                    float stageFactor = GetPublicStageFactor(disease.Stage);
                    need += 8f * Math.Clamp(disease.Severity, 1, 5) * stageFactor;
                }
            }

            return EconomyCalculator.Clamp(need, 0f, 100f);
        }

        private async Task ShowAdminHealthMenu(
            ITelegramBotClient botClient,
            long userId,
            long chatId,
            int msgId,
            CancellationToken cancellationToken,
            string? statusMessage = null)
        {
            var selectedCountryId = GetSelectedCountryIdForUser(userId);
            if (!selectedCountryId.HasValue)
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "Не знайдено країни. Оберіть країну або призначте ідентифікатор країни персонажу.",
                    StateKeyboardBuilder.PublicOverviewKeyboard(),
                    cancellationToken);
                return;
            }

            var country = CountriesData.GetCountryById(selectedCountryId.Value);
            if (country == null)
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "Країну не знайдено.",
                    StateKeyboardBuilder.AdminMenuKeyboard(userId == masterID),
                    cancellationToken);
                return;
            }

            bool canControl = HasPublicControlAccess(userId, selectedCountryId);
            var systems = CountryPublicSystemsData.GetByCountry(country.CountryId);
            var diseases = CountryDiseasesData.GetByCountry(country.CountryId, activeOnly: true);
            var lawCodes = BuildAcceptedLawCodes(country.CountryId);
            bool hasHealthMinistry = PublicSystemsEngine.HasLaw(lawCodes, PublicSystemsEngine.LawHealthMinistry);
            bool hasCommunityOutreach = PublicSystemsEngine.HasLaw(lawCodes, PublicSystemsEngine.LawCommunityHealthOutreach);
            bool hasQuarantine = PublicSystemsEngine.HasLaw(lawCodes, PublicSystemsEngine.LawQuarantineProtocols);
            bool hasVaccination = PublicSystemsEngine.HasLaw(lawCodes, PublicSystemsEngine.LawNationalImmunization);
            bool hasIntl = PublicSystemsEngine.HasLaw(lawCodes, PublicSystemsEngine.LawIntlCooperation);
            bool hasMedicalLicensing = PublicSystemsEngine.HasLaw(lawCodes, PublicSystemsEngine.LawMedicalLicensing);
            bool hasPharmaAgency = PublicSystemsEngine.HasLaw(lawCodes, PublicSystemsEngine.LawPharmaResearchAgency);
            bool hasClinicalTrials = PublicSystemsEngine.HasLaw(lawCodes, PublicSystemsEngine.LawClinicalTrialProtocols);
            bool hasMedicineReserve = PublicSystemsEngine.HasLaw(lawCodes, PublicSystemsEngine.LawEmergencyMedicineReserve);
            bool hasResidency = PublicSystemsEngine.HasLaw(lawCodes, PublicSystemsEngine.LawHospitalResidencyProgram);

            float healthNeed = EstimateHealthNeed(country, diseases);
            decimal healthProdBonus = (decimal)((systems.HealthPublic - 50f) / 200f);

            var sb = new StringBuilder();
            sb.AppendLine($"🏥 МОЗ: {FormatCountryName(country)}");
            sb.AppendLine($"Казна: {FmtNumber(country.Treasury)}");
            sb.AppendLine($"Громзд/Сан/Спром/Довіра: {systems.HealthPublic:0.#}/{systems.HealthSanitation:0.#}/{systems.HealthCapacity:0.#}/{systems.HealthTrust:0.#}");
            sb.AppendLine($"Потреба в охороні здоров'я (оцінка): {healthNeed:0.#}");
            sb.AppendLine($"Бюджет МОЗ: {FmtNumber(systems.BudgetHealth)} / тік");
            sb.AppendLine($"Виробничий модифікатор МОЗ: {healthProdBonus:+0.###;-0.###;0}");
            sb.AppendLine($"Карантин: {(systems.QuarantineEnabled ? "увімкнено" : "вимкнено")}");
            sb.AppendLine($"Лікарі/Втома: {FmtNumber(systems.HealthDoctors)} / {systems.HealthDoctorFatigue:0.#}");
            sb.AppendLine($"Запаси ліків: {systems.HealthMedicineStock:0.#}");
            sb.AppendLine($"R&D ліків: {systems.HealthDrugResearchProgress:0.#}% | Рівень: {FmtNumber(systems.HealthDrugLevel)} | Проєкти: {FmtNumber(systems.HealthDrugProjects)}");
            sb.AppendLine($"Клінічні випробування: {FmtNumber(systems.HealthClinicalTrialTicks)} тік.");
            sb.AppendLine($"Активні хвороби: {FmtNumber(diseases.Count)}");
            sb.AppendLine();
            sb.AppendLine(BuildLawStatusLine(hasHealthMinistry, "LAW_HEALTH_MINISTRY"));
            sb.AppendLine(BuildLawStatusLine(PublicSystemsEngine.HasLaw(lawCodes, PublicSystemsEngine.LawPublicSanitation), "LAW_PUBLIC_SANITATION"));
            sb.AppendLine(BuildLawStatusLine(hasQuarantine, "LAW_QUARANTINE_PROTOCOLS"));
            sb.AppendLine(BuildLawStatusLine(hasVaccination, "LAW_NATIONAL_IMMUNIZATION"));
            sb.AppendLine(BuildLawStatusLine(hasMedicalLicensing, "LAW_MEDICAL_LICENSING"));
            sb.AppendLine(BuildLawStatusLine(hasResidency, "LAW_HOSPITAL_RESIDENCY_PROGRAM"));
            sb.AppendLine(BuildLawStatusLine(hasPharmaAgency, "LAW_PHARMA_RESEARCH_AGENCY"));
            sb.AppendLine(BuildLawStatusLine(hasClinicalTrials, "LAW_CLINICAL_TRIAL_PROTOCOLS"));
            sb.AppendLine(BuildLawStatusLine(hasMedicineReserve, "LAW_EMERGENCY_MEDICINE_RESERVE"));
            sb.AppendLine(BuildLawStatusLine(hasCommunityOutreach, "LAW_COMMUNITY_HEALTH_OUTREACH"));
            sb.AppendLine(BuildLawStatusLine(hasIntl, "LAW_INTL_COOPERATION"));
            sb.AppendLine(BuildLawStatusLine(PublicSystemsEngine.HasLaw(lawCodes, PublicSystemsEngine.LawIntlDataSharingHealth), "LAW_INTL_DATA_SHARING_HEALTH"));

            if (!string.IsNullOrWhiteSpace(statusMessage))
            {
                sb.AppendLine();
                sb.AppendLine(statusMessage);
            }

            EconomyPublicModuleRouteByUser[userId] = PublicRouteAdminHealthBudget;
            EconomyPublicUiContext[userId] = (chatId, msgId);
            await SafeEditMessageTextAsync(
                botClient,
                chatId,
                msgId,
                sb.ToString(),
                StateKeyboardBuilder.AdminHealthMenuKeyboard(canControl),
                cancellationToken);
        }

        private async Task ShowAdminHealthReport(
            ITelegramBotClient botClient,
            long userId,
            long chatId,
            int msgId,
            CancellationToken cancellationToken,
            string? statusMessage = null)
        {
            var selectedCountryId = GetSelectedCountryIdForUser(userId);
            if (!selectedCountryId.HasValue)
            {
                await ShowAdminHealthMenu(botClient, userId, chatId, msgId, cancellationToken, "Не знайдено країни.");
                return;
            }

            var country = CountriesData.GetCountryById(selectedCountryId.Value);
            if (country == null)
            {
                await ShowAdminHealthMenu(botClient, userId, chatId, msgId, cancellationToken, "Країну не знайдено.");
                return;
            }

            var systems = CountryPublicSystemsData.GetByCountry(country.CountryId);
            var diseases = CountryDiseasesData.GetByCountry(country.CountryId, activeOnly: true)
                .OrderByDescending(d => d.Severity)
                .ThenByDescending(d => d.Infectiousness)
                .ThenBy(d => d.DiseaseId)
                .ToList();
            float healthNeed = EstimateHealthNeed(country, diseases);
            decimal healthProdBonus = (decimal)((systems.HealthPublic - 50f) / 200f);

            var sb = new StringBuilder();
            sb.AppendLine($"📊 Звіт МОЗ: {FormatCountryName(country)}");
            sb.AppendLine($"Громзд/Сан/Спром/Довіра: {systems.HealthPublic:0.#}/{systems.HealthSanitation:0.#}/{systems.HealthCapacity:0.#}/{systems.HealthTrust:0.#}");
            sb.AppendLine($"Потреба в охороні здоров'я (оцінка): {healthNeed:0.#}");
            sb.AppendLine($"Бюджет МОЗ: {FmtNumber(systems.BudgetHealth)} / тік");
            sb.AppendLine($"Мобілізація медиків: {FmtNumber(systems.MedicMobilizationTicks)} тік.");
            sb.AppendLine($"Захист вакцинацією: {FmtNumber(systems.VaccinationProtectionTicks)} тік.");
            sb.AppendLine($"Лікарі/Втома: {FmtNumber(systems.HealthDoctors)} / {systems.HealthDoctorFatigue:0.#}");
            sb.AppendLine($"Запаси ліків: {systems.HealthMedicineStock:0.#}");
            sb.AppendLine($"R&D ліків: {systems.HealthDrugResearchProgress:0.#}% | Рівень: {FmtNumber(systems.HealthDrugLevel)} | Проєкти: {FmtNumber(systems.HealthDrugProjects)}");
            sb.AppendLine($"Клінічні випробування: {FmtNumber(systems.HealthClinicalTrialTicks)} тік.");
            sb.AppendLine($"Виробничий модифікатор МОЗ: {healthProdBonus:+0.###;-0.###;0}");
            sb.AppendLine();
            sb.AppendLine("Активні хвороби:");
            if (diseases.Count == 0)
            {
                sb.AppendLine("• Немає");
            }
            else
            {
                foreach (var disease in diseases.Take(8))
                    sb.AppendLine($"• {disease.Name}: Тяж {disease.Severity}/Зараз {disease.Infectiousness}, {FormatPublicStageUk(disease.Stage)}, {FmtNumber(disease.RemainingTicks)} тік.");
                if (diseases.Count > 8)
                    sb.AppendLine($"…ще {FmtNumber(diseases.Count - 8)}");
            }

            if (!string.IsNullOrWhiteSpace(statusMessage))
            {
                sb.AppendLine();
                sb.AppendLine(statusMessage);
            }

            EconomyPublicModuleRouteByUser[userId] = PublicRouteAdminHealthBudget;
            EconomyPublicUiContext[userId] = (chatId, msgId);
            await SafeEditMessageTextAsync(
                botClient,
                chatId,
                msgId,
                sb.ToString(),
                StateKeyboardBuilder.AdminHealthMenuKeyboard(HasPublicControlAccess(userId, selectedCountryId)),
                cancellationToken);
        }

        private async Task ShowAdminHealthBudget(
            ITelegramBotClient botClient,
            long userId,
            long chatId,
            int msgId,
            CancellationToken cancellationToken,
            string? statusMessage = null)
        {
            var selectedCountryId = GetSelectedCountryIdForUser(userId);
            if (!selectedCountryId.HasValue)
            {
                await ShowAdminHealthMenu(botClient, userId, chatId, msgId, cancellationToken, "Не знайдено країни.");
                return;
            }

            var country = CountriesData.GetCountryById(selectedCountryId.Value);
            if (country == null)
            {
                await ShowAdminHealthMenu(botClient, userId, chatId, msgId, cancellationToken, "Країну не знайдено.");
                return;
            }

            bool canControl = HasPublicControlAccess(userId, selectedCountryId);
            var systems = CountryPublicSystemsData.GetByCountry(country.CountryId);
            string intlMode = PublicSystemsEngine.NormalizeIntlBudgetMode(systems.BudgetIntlMode);

            var sb = new StringBuilder();
            sb.AppendLine($"💸 Бюджети МОЗ: {FormatCountryName(country)}");
            sb.AppendLine($"Казна: {FmtNumber(country.Treasury)}");
            sb.AppendLine($"МОЗ: {FmtNumber(systems.BudgetHealth)} / тік");
            sb.AppendLine($"Міжнародний бюджет: {FmtNumber(systems.BudgetIntl)} / тік");
            sb.AppendLine($"Режим міжнародного бюджету: {FormatIntlBudgetModeUk(intlMode)}");
            sb.AppendLine($"Борг публічних бюджетів: {FmtNumber(systems.BudgetDebt)}");
            sb.AppendLine();
            sb.AppendLine("Команди (можна і українські аргументи):");
            sb.AppendLine("`set_budget(health, 1000)`");
            sb.AppendLine("`set_budget_intl(auto|manual|авто|вручну)`");

            if (!string.IsNullOrWhiteSpace(statusMessage))
            {
                sb.AppendLine();
                sb.AppendLine(statusMessage);
            }

            EconomyPublicModuleRouteByUser[userId] = PublicRouteAdminHealthBudget;
            EconomyPublicUiContext[userId] = (chatId, msgId);
            await SafeEditMessageTextAsync(
                botClient,
                chatId,
                msgId,
                sb.ToString(),
                StateKeyboardBuilder.AdminHealthBudgetKeyboard(intlMode.Equals("Auto", StringComparison.OrdinalIgnoreCase), canControl),
                cancellationToken);
        }

        private async Task ShowAdminHealthActions(
            ITelegramBotClient botClient,
            long userId,
            long chatId,
            int msgId,
            CancellationToken cancellationToken,
            string? statusMessage = null)
        {
            var selectedCountryId = GetSelectedCountryIdForUser(userId);
            if (!HasPublicControlAccess(userId, selectedCountryId))
            {
                await ShowAdminHealthMenu(botClient, userId, chatId, msgId, cancellationToken, "Нема доступу.");
                return;
            }

            if (!selectedCountryId.HasValue)
            {
                await ShowAdminHealthMenu(botClient, userId, chatId, msgId, cancellationToken, "Не знайдено країни.");
                return;
            }

            var systems = CountryPublicSystemsData.GetByCountry(selectedCountryId.Value);
            var lawCodes = BuildAcceptedLawCodes(selectedCountryId.Value);
            bool hasHealthMinistry = PublicSystemsEngine.HasLaw(lawCodes, PublicSystemsEngine.LawHealthMinistry);
            bool hasSanitationLaw = PublicSystemsEngine.HasLaw(lawCodes, PublicSystemsEngine.LawPublicSanitation);
            bool hasQuarantineProtocols = PublicSystemsEngine.HasLaw(lawCodes, PublicSystemsEngine.LawQuarantineProtocols);
            bool hasVaccinationLaw = PublicSystemsEngine.HasLaw(lawCodes, PublicSystemsEngine.LawNationalImmunization);
            bool hasOutreachLaw = PublicSystemsEngine.HasLaw(lawCodes, PublicSystemsEngine.LawCommunityHealthOutreach);
            bool hasMedicalLicensing = PublicSystemsEngine.HasLaw(lawCodes, PublicSystemsEngine.LawMedicalLicensing);
            bool hasPharmaAgency = PublicSystemsEngine.HasLaw(lawCodes, PublicSystemsEngine.LawPharmaResearchAgency);
            bool hasClinicalTrials = PublicSystemsEngine.HasLaw(lawCodes, PublicSystemsEngine.LawClinicalTrialProtocols);
            bool hasMedicineReserve = PublicSystemsEngine.HasLaw(lawCodes, PublicSystemsEngine.LawEmergencyMedicineReserve);
            bool hasResidency = PublicSystemsEngine.HasLaw(lawCodes, PublicSystemsEngine.LawHospitalResidencyProgram);

            var sb = new StringBuilder();
            sb.AppendLine("🛠 Дії МОЗ під час хвороб");
            sb.AppendLine(BuildLawStatusLine(hasHealthMinistry, "LAW_HEALTH_MINISTRY"));
            sb.AppendLine(BuildLawStatusLine(hasSanitationLaw, "LAW_PUBLIC_SANITATION"));
            sb.AppendLine(BuildLawStatusLine(hasQuarantineProtocols, "LAW_QUARANTINE_PROTOCOLS"));
            sb.AppendLine(BuildLawStatusLine(hasVaccinationLaw, "LAW_NATIONAL_IMMUNIZATION"));
            sb.AppendLine(BuildLawStatusLine(hasOutreachLaw, "LAW_COMMUNITY_HEALTH_OUTREACH"));
            sb.AppendLine(BuildLawStatusLine(hasMedicalLicensing, "LAW_MEDICAL_LICENSING"));
            sb.AppendLine(BuildLawStatusLine(hasResidency, "LAW_HOSPITAL_RESIDENCY_PROGRAM"));
            sb.AppendLine(BuildLawStatusLine(hasPharmaAgency, "LAW_PHARMA_RESEARCH_AGENCY"));
            sb.AppendLine(BuildLawStatusLine(hasClinicalTrials, "LAW_CLINICAL_TRIAL_PROTOCOLS"));
            sb.AppendLine(BuildLawStatusLine(hasMedicineReserve, "LAW_EMERGENCY_MEDICINE_RESERVE"));
            sb.AppendLine();
            sb.AppendLine($"Карантин: {(systems.QuarantineEnabled ? "увімкнено" : "вимкнено")}");
            sb.AppendLine($"Мобілізація медиків: {FmtNumber(systems.MedicMobilizationTicks)} тік.");
            sb.AppendLine($"Захист вакцинацією: {FmtNumber(systems.VaccinationProtectionTicks)} тік.");
            sb.AppendLine($"Лікарі/Втома: {FmtNumber(systems.HealthDoctors)} / {systems.HealthDoctorFatigue:0.#}");
            sb.AppendLine($"Запаси ліків: {systems.HealthMedicineStock:0.#}");
            sb.AppendLine($"R&D ліків: {systems.HealthDrugResearchProgress:0.#}% | Рівень: {FmtNumber(systems.HealthDrugLevel)} | Проєкти: {FmtNumber(systems.HealthDrugProjects)}");
            sb.AppendLine($"Клінічні випробування: {FmtNumber(systems.HealthClinicalTrialTicks)} тік.");

            if (!string.IsNullOrWhiteSpace(statusMessage))
            {
                sb.AppendLine();
                sb.AppendLine(statusMessage);
            }

            EconomyPublicModuleRouteByUser[userId] = PublicRouteAdminHealthBudget;
            EconomyPublicUiContext[userId] = (chatId, msgId);
            await SafeEditMessageTextAsync(
                botClient,
                chatId,
                msgId,
                sb.ToString(),
                StateKeyboardBuilder.AdminHealthActionsKeyboard(),
                cancellationToken);
        }

        private async Task ShowAdminHealthDiseases(
            ITelegramBotClient botClient,
            long userId,
            long chatId,
            int msgId,
            CancellationToken cancellationToken,
            string? statusMessage = null)
        {
            var selectedCountryId = GetSelectedCountryIdForUser(userId);
            if (!selectedCountryId.HasValue)
            {
                await ShowAdminHealthMenu(botClient, userId, chatId, msgId, cancellationToken, "Не знайдено країни.");
                return;
            }

            var diseases = CountryDiseasesData.GetByCountry(selectedCountryId.Value, activeOnly: true)
                .OrderByDescending(d => d.Severity)
                .ThenByDescending(d => d.Infectiousness)
                .ThenBy(d => d.DiseaseId)
                .ToList();

            var items = new List<(string text, string data)>();
            foreach (var d in diseases)
            {
                string text = $"🦠 {d.Name} | Тяж {d.Severity}/Зараз {d.Infectiousness} | {FormatPublicStageUk(d.Stage)} | {FmtNumber(d.RemainingTicks)} тік.";
                items.Add((text, $"state_admin_health_disease_{d.DiseaseId}"));
            }

            var sb = new StringBuilder();
            sb.AppendLine("🦠 Активні хвороби (МОЗ)");
            sb.AppendLine($"Кількість: {FmtNumber(diseases.Count)}");
            sb.AppendLine($"Ціль ГМ-створення: {FormatPublicDiseaseTargetUk(PublicGmDiseaseTargetByUser.GetValueOrDefault(userId, "whole_state"))}");
            if (!string.IsNullOrWhiteSpace(statusMessage))
            {
                sb.AppendLine();
                sb.AppendLine(statusMessage);
            }

            EconomyPublicModuleRouteByUser[userId] = PublicRouteAdminHealthBudget;
            EconomyPublicUiContext[userId] = (chatId, msgId);
            await SafeEditMessageTextAsync(
                botClient,
                chatId,
                msgId,
                sb.ToString(),
                StateKeyboardBuilder.AdminHealthDiseasesKeyboard(items, canControl: userId == masterID),
                cancellationToken);
        }

        private async Task ShowAdminHealthDiseaseInfo(
            ITelegramBotClient botClient,
            long userId,
            long chatId,
            int msgId,
            int diseaseId,
            CancellationToken cancellationToken,
            string? statusMessage = null)
        {
            var selectedCountryId = GetSelectedCountryIdForUser(userId);
            if (!selectedCountryId.HasValue)
            {
                await ShowAdminHealthMenu(botClient, userId, chatId, msgId, cancellationToken, "Не знайдено країни.");
                return;
            }

            var disease = CountryDiseasesData.GetByCountry(selectedCountryId.Value).FirstOrDefault(d => d.DiseaseId == diseaseId);
            if (disease == null)
            {
                await ShowAdminHealthDiseases(botClient, userId, chatId, msgId, cancellationToken, "Хворобу не знайдено.");
                return;
            }

            float stageFactor = GetPublicStageFactor(disease.Stage);
            float prodPenaltyPct = 100f * 0.02f * disease.Severity * disease.Infectiousness * stageFactor;
            float healthNeedBonus = 8f * disease.Severity * stageFactor;

            var sb = new StringBuilder();
            sb.AppendLine($"🦠 {disease.Name}");
            sb.AppendLine($"Ідентифікатор: {FmtNumber(disease.DiseaseId)}");
            sb.AppendLine($"Стан: {(disease.Active ? "активна" : "завершена")}");
            sb.AppendLine($"Тяжкість / заразність: {FmtNumber(disease.Severity)} / {FmtNumber(disease.Infectiousness)}");
            sb.AppendLine($"Стадія: {FmtNumber(disease.Stage)} ({FormatPublicStageUk(disease.Stage)})");
            sb.AppendLine($"Тривалість / залишок: {FmtNumber(disease.DurationTicks)} / {FmtNumber(disease.RemainingTicks)} тік.");
            sb.AppendLine($"Ціль: {FormatPublicDiseaseTargetUk(disease.TargetScope)}");
            sb.AppendLine($"Штраф до виробництва (оцінка): -{prodPenaltyPct:0.##}%");
            sb.AppendLine($"Бонус потреби в охороні здоров'я (оцінка): +{healthNeedBonus:0.##}");

            if (!string.IsNullOrWhiteSpace(statusMessage))
            {
                sb.AppendLine();
                sb.AppendLine(statusMessage);
            }

            var rows = new List<InlineKeyboardButton[]>();
            if (userId == masterID)
            {
                rows.Add(new[]
                {
                    InlineKeyboardButton.WithCallbackData("⬅ Стадія -1", $"state_admin_health_disease_shift_{disease.DiseaseId}_-1"),
                    InlineKeyboardButton.WithCallbackData("➡ Стадія +1", $"state_admin_health_disease_shift_{disease.DiseaseId}_1")
                });
                if (disease.Active)
                {
                    rows.Add(new[]
                    {
                        InlineKeyboardButton.WithCallbackData("🛑 Завершити (ГМ)", $"state_admin_health_disease_end_{disease.DiseaseId}")
                    });
                }
            }
            rows.Add(new[]
            {
                InlineKeyboardButton.WithCallbackData("🔙 До хвороб", "state_admin_health_diseases")
            });

            EconomyPublicModuleRouteByUser[userId] = PublicRouteAdminHealthBudget;
            EconomyPublicUiContext[userId] = (chatId, msgId);
            await SafeEditMessageTextAsync(botClient, chatId, msgId, sb.ToString(), new InlineKeyboardMarkup(rows), cancellationToken);
        }

        private async Task ShiftAdminHealthDiseaseStage(
            ITelegramBotClient botClient,
            long userId,
            long chatId,
            int msgId,
            int diseaseId,
            int delta,
            CancellationToken cancellationToken)
        {
            if (userId != masterID)
            {
                await ShowAdminHealthDiseases(botClient, userId, chatId, msgId, cancellationToken, "Нема доступу.");
                return;
            }

            var selectedCountryId = GetSelectedCountryIdForUser(userId);
            if (!selectedCountryId.HasValue)
            {
                await ShowAdminHealthDiseases(botClient, userId, chatId, msgId, cancellationToken, "Не знайдено країни.");
                return;
            }

            var disease = CountryDiseasesData.GetByCountry(selectedCountryId.Value).FirstOrDefault(d => d.DiseaseId == diseaseId);
            if (disease == null)
            {
                await ShowAdminHealthDiseases(botClient, userId, chatId, msgId, cancellationToken, "Хворобу не знайдено.");
                return;
            }

            if (!disease.Active)
            {
                await ShowAdminHealthDiseaseInfo(botClient, userId, chatId, msgId, diseaseId, cancellationToken, "Хвороба вже неактивна.");
                return;
            }

            disease.Stage = Math.Clamp(disease.Stage + delta, 1, 4);
            CountryDiseasesData.Upsert(disease);
            await ShowAdminHealthDiseaseInfo(botClient, userId, chatId, msgId, diseaseId, cancellationToken, $"✅ Стадію змінено на {disease.Stage} ({FormatPublicStageUk(disease.Stage)}).");
        }

        private async Task EndAdminHealthDisease(
            ITelegramBotClient botClient,
            long userId,
            long chatId,
            int msgId,
            int diseaseId,
            CancellationToken cancellationToken)
        {
            if (userId != masterID)
            {
                await ShowAdminHealthDiseases(botClient, userId, chatId, msgId, cancellationToken, "Нема доступу.");
                return;
            }

            var selectedCountryId = GetSelectedCountryIdForUser(userId);
            if (!selectedCountryId.HasValue)
            {
                await ShowAdminHealthDiseases(botClient, userId, chatId, msgId, cancellationToken, "Не знайдено країни.");
                return;
            }

            int affected = CountryDiseasesData.EndDiseaseById(selectedCountryId.Value, diseaseId);
            await ShowAdminHealthDiseases(
                botClient,
                userId,
                chatId,
                msgId,
                cancellationToken,
                affected > 0 ? "✅ Хворобу завершено." : "❌ Хворобу не знайдено або вже завершено.");
        }

        private async Task EndAllAdminHealthDiseases(
            ITelegramBotClient botClient,
            long userId,
            long chatId,
            int msgId,
            CancellationToken cancellationToken)
        {
            if (userId != masterID)
            {
                await ShowAdminHealthDiseases(botClient, userId, chatId, msgId, cancellationToken, "Нема доступу.");
                return;
            }

            var selectedCountryId = GetSelectedCountryIdForUser(userId);
            if (!selectedCountryId.HasValue)
            {
                await ShowAdminHealthDiseases(botClient, userId, chatId, msgId, cancellationToken, "Не знайдено країни.");
                return;
            }

            int ended = 0;
            foreach (var disease in CountryDiseasesData.GetByCountry(selectedCountryId.Value, activeOnly: true))
            {
                ended += CountryDiseasesData.EndDiseaseById(selectedCountryId.Value, disease.DiseaseId);
            }

            await ShowAdminHealthDiseases(botClient, userId, chatId, msgId, cancellationToken, $"✅ Завершено хвороб: {FmtNumber(ended)}.");
        }

        private async Task ShowAdminHealthGmSpawnHelp(
            ITelegramBotClient botClient,
            long userId,
            long chatId,
            int msgId,
            CancellationToken cancellationToken)
        {
            if (userId != masterID)
            {
                await ShowAdminHealthDiseases(botClient, userId, chatId, msgId, cancellationToken, "Нема доступу.");
                return;
            }

            var sb = new StringBuilder();
            sb.AppendLine("🎲 ГМ: керування хворобами (через текстові команди)");
            sb.AppendLine("`gm_spawn_disease(name, severity, infectiousness, duration)`");
            sb.AppendLine("`gm_set_disease_stage(name_or_id, stage)`");
            sb.AppendLine("`gm_end_disease(name_or_id)`");
            sb.AppendLine("`gm_trigger_event(panic|scandal|breakthrough|import|паніка|скандал|прорив|завезення)`");
            sb.AppendLine("`gm_disease_target(whole_state|region|city|держава|регіон|місто)`");
            sb.AppendLine();
            sb.AppendLine("Приклад:");
            sb.AppendLine("`gm_spawn_disease(Сіра гарячка, 3, 4, 7)`");

            var keyboard = new InlineKeyboardMarkup(new[]
            {
                new [] { InlineKeyboardButton.WithCallbackData("🔙 До хвороб", "state_admin_health_diseases") }
            });

            EconomyPublicModuleRouteByUser[userId] = PublicRouteAdminHealthBudget;
            EconomyPublicUiContext[userId] = (chatId, msgId);
            await SafeEditMessageTextAsync(botClient, chatId, msgId, sb.ToString(), keyboard, cancellationToken);
        }

        private async Task ShowAdminHealthIntlPrograms(
            ITelegramBotClient botClient,
            long userId,
            long chatId,
            int msgId,
            CancellationToken cancellationToken,
            string? statusMessage = null)
        {
            var selectedCountryId = GetSelectedCountryIdForUser(userId);
            if (!selectedCountryId.HasValue)
            {
                await ShowAdminHealthMenu(botClient, userId, chatId, msgId, cancellationToken, "Не знайдено країни.");
                return;
            }

            bool canControl = HasPublicControlAccess(userId, selectedCountryId);
            var systems = CountryPublicSystemsData.GetByCountry(selectedCountryId.Value);
            var programs = CountryIntlProgramsData.GetByCountry(selectedCountryId.Value)
                .Where(p => AdminHealthProgramTypes.Contains(PublicSystemsEngine.NormalizeProgramType(p.ProgramType), StringComparer.OrdinalIgnoreCase))
                .ToList();
            var existingByType = programs
                .GroupBy(p => PublicSystemsEngine.NormalizeProgramType(p.ProgramType), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.OrderByDescending(x => x.IntlProgramId).First(), StringComparer.OrdinalIgnoreCase);

            var laws = BuildAcceptedLawCodes(selectedCountryId.Value);
            bool hasIntlCoop = PublicSystemsEngine.HasLaw(laws, PublicSystemsEngine.LawIntlCooperation);

            var items = new List<(string text, string data)>();
            foreach (var type in AdminHealthProgramTypes)
            {
                string display = PublicSystemsEngine.GetProgramDisplayNameUk(type);
                if (existingByType.TryGetValue(type, out var existing))
                {
                    string state = existing.IsActive ? "✅" : "⛔";
                    string cost = FmtNumber(existing.CostPerTick > 0m ? existing.CostPerTick : PublicSystemsEngine.GetDefaultProgramCost(type));
                    string data = canControl ? $"state_admin_health_intl_toggle_{existing.IntlProgramId}" : "state_noop";
                    items.Add(($"{state} {display} • {cost}/тік • Фах {FmtNumber(existing.RequiredSpec)}", data));
                }
                else if (canControl)
                {
                    items.Add(($"➕ {display}", $"state_admin_health_intl_add_{ProgramTypeToToken(type)}"));
                }
            }

            var sb = new StringBuilder();
            sb.AppendLine("🌍 Міжнародні програми МОЗ");
            sb.AppendLine($"Престиж: {systems.IntlPrestige:0.#}");
            sb.AppendLine($"Міжнародний бюджет: {FmtNumber(systems.BudgetIntl)} ({FormatIntlBudgetModeUk(systems.BudgetIntlMode)})");
            sb.AppendLine(BuildLawStatusLine(hasIntlCoop, "LAW_INTL_COOPERATION"));
            sb.AppendLine(BuildLawStatusLine(PublicSystemsEngine.HasLaw(laws, PublicSystemsEngine.LawIntlDataSharingHealth), "LAW_INTL_DATA_SHARING_HEALTH"));
            sb.AppendLine();
            sb.AppendLine("Натисніть елемент, щоб увімкнути/вимкнути або створити програму.");

            if (!string.IsNullOrWhiteSpace(statusMessage))
            {
                sb.AppendLine();
                sb.AppendLine(statusMessage);
            }

            EconomyPublicModuleRouteByUser[userId] = PublicRouteAdminHealthBudget;
            EconomyPublicUiContext[userId] = (chatId, msgId);
            await SafeEditMessageTextAsync(
                botClient,
                chatId,
                msgId,
                sb.ToString(),
                StateKeyboardBuilder.AdminHealthIntlKeyboard(items, canControl),
                cancellationToken);
        }

        private async Task ToggleAdminHealthIntlProgram(
            ITelegramBotClient botClient,
            long userId,
            long chatId,
            int msgId,
            int intlProgramId,
            CancellationToken cancellationToken)
        {
            var selectedCountryId = GetSelectedCountryIdForUser(userId);
            if (!HasPublicControlAccess(userId, selectedCountryId))
            {
                await ShowAdminHealthIntlPrograms(botClient, userId, chatId, msgId, cancellationToken, "Нема доступу.");
                return;
            }

            if (!selectedCountryId.HasValue)
            {
                await ShowAdminHealthIntlPrograms(botClient, userId, chatId, msgId, cancellationToken, "Не знайдено країни.");
                return;
            }

            var program = CountryIntlProgramsData.GetById(intlProgramId);
            if (program == null ||
                program.CountryId != selectedCountryId.Value ||
                !AdminHealthProgramTypes.Contains(PublicSystemsEngine.NormalizeProgramType(program.ProgramType), StringComparer.OrdinalIgnoreCase))
            {
                await ShowAdminHealthIntlPrograms(botClient, userId, chatId, msgId, cancellationToken, "Програму не знайдено.");
                return;
            }

            bool setActive = !program.IsActive;
            CountryIntlProgramsData.SetActive(selectedCountryId.Value, intlProgramId, setActive);
            await ShowAdminHealthIntlPrograms(
                botClient,
                userId,
                chatId,
                msgId,
                cancellationToken,
                setActive ? "✅ Програму увімкнено." : "✅ Програму вимкнено.");
        }

        private async Task AddAdminHealthIntlProgram(
            ITelegramBotClient botClient,
            long userId,
            long chatId,
            int msgId,
            string token,
            CancellationToken cancellationToken)
        {
            var selectedCountryId = GetSelectedCountryIdForUser(userId);
            if (!HasPublicControlAccess(userId, selectedCountryId))
            {
                await ShowAdminHealthIntlPrograms(botClient, userId, chatId, msgId, cancellationToken, "Нема доступу.");
                return;
            }

            if (!selectedCountryId.HasValue)
            {
                await ShowAdminHealthIntlPrograms(botClient, userId, chatId, msgId, cancellationToken, "Не знайдено країни.");
                return;
            }

            string programType = ProgramTokenToType(token);
            if (string.IsNullOrWhiteSpace(programType) ||
                !AdminHealthProgramTypes.Contains(PublicSystemsEngine.NormalizeProgramType(programType), StringComparer.OrdinalIgnoreCase))
            {
                await ShowAdminHealthIntlPrograms(botClient, userId, chatId, msgId, cancellationToken, "Невідомий тип програми.");
                return;
            }

            var laws = BuildAcceptedLawCodes(selectedCountryId.Value);
            if (!PublicSystemsEngine.HasLaw(laws, PublicSystemsEngine.LawIntlCooperation))
            {
                await ShowAdminHealthIntlPrograms(botClient, userId, chatId, msgId, cancellationToken, BuildLawRequirementMessage(PublicSystemsEngine.LawIntlCooperation));
                return;
            }

            if (string.Equals(programType, PublicSystemsEngine.ProgramHealthDataSharing, StringComparison.OrdinalIgnoreCase) &&
                !PublicSystemsEngine.HasLaw(laws, PublicSystemsEngine.LawIntlDataSharingHealth))
            {
                await ShowAdminHealthIntlPrograms(botClient, userId, chatId, msgId, cancellationToken, BuildLawRequirementMessage(PublicSystemsEngine.LawIntlDataSharingHealth));
                return;
            }

            var existing = CountryIntlProgramsData.GetByCountryAndType(selectedCountryId.Value, programType);
            if (existing != null)
            {
                if (!existing.IsActive)
                    CountryIntlProgramsData.SetActive(selectedCountryId.Value, existing.IntlProgramId, true);
                await ShowAdminHealthIntlPrograms(botClient, userId, chatId, msgId, cancellationToken, "✅ Програму активовано.");
                return;
            }

            var record = new CountryIntlProgramsData.CountryIntlProgramRecord
            {
                CountryId = selectedCountryId.Value,
                ProgramType = programType,
                CostPerTick = PublicSystemsEngine.GetDefaultProgramCost(programType),
                RequiredSpec = PublicSystemsEngine.GetDefaultProgramRequiredSpec(programType),
                IsActive = true,
                TicksActive = 0,
                SuccessTicks = 0
            };
            CountryIntlProgramsData.Upsert(record);
            await ShowAdminHealthIntlPrograms(botClient, userId, chatId, msgId, cancellationToken, "✅ Програму додано й активовано.");
        }

        private async Task ShowAdminEducationMenu(
            ITelegramBotClient botClient,
            long userId,
            long chatId,
            int msgId,
            CancellationToken cancellationToken,
            string? statusMessage = null)
        {
            var selectedCountryId = GetSelectedCountryIdForUser(userId);
            if (!selectedCountryId.HasValue)
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "Не знайдено країни. Оберіть країну або призначте ідентифікатор країни персонажу.",
                    StateKeyboardBuilder.PublicOverviewKeyboard(),
                    cancellationToken);
                return;
            }

            var country = CountriesData.GetCountryById(selectedCountryId.Value);
            if (country == null)
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "Країну не знайдено.",
                    StateKeyboardBuilder.AdminMenuKeyboard(userId == masterID),
                    cancellationToken);
                return;
            }

            bool canControl = HasPublicControlAccess(userId, selectedCountryId);
            var systems = CountryPublicSystemsData.GetByCountry(country.CountryId);
            var lawCodes = BuildAcceptedLawCodes(country.CountryId);
            bool hasEduMinistry = PublicSystemsEngine.HasLaw(lawCodes, PublicSystemsEngine.LawEduMinistry);
            bool hasSchooling = PublicSystemsEngine.HasLaw(lawCodes, PublicSystemsEngine.LawBasicSchooling);
            bool hasHigherEdu = PublicSystemsEngine.HasLaw(lawCodes, PublicSystemsEngine.LawHigherEduCharter);
            bool hasScholarships = PublicSystemsEngine.HasLaw(lawCodes, PublicSystemsEngine.LawScholarshipProgram);
            bool hasTeacherCertification = PublicSystemsEngine.HasLaw(lawCodes, PublicSystemsEngine.LawTeacherCertification);

            int specialistFlow = Math.Max(0, (int)Math.Floor((systems.EduLiteracy + systems.EduQuality) / 40f));
            decimal eduProdBonus = (decimal)((systems.EduLiteracy - 50f) / 250f);

            var sb = new StringBuilder();
            sb.AppendLine($"🎓 МОН: {FormatCountryName(country)}");
            sb.AppendLine($"Казна: {FmtNumber(country.Treasury)}");
            sb.AppendLine($"Грам/Якість/Фах/Згурт: {systems.EduLiteracy:0.#}/{systems.EduQuality:0.#}/{systems.EduSpecialists}/{systems.EduCohesion:0.#}");
            sb.AppendLine($"Бюджет МОН: {FmtNumber(systems.BudgetEducation)} / тік");
            sb.AppendLine($"Приріст фахівців (база): +{FmtNumber(specialistFlow)} / тік");
            sb.AppendLine($"Виробничий модифікатор МОН: {eduProdBonus:+0.###;-0.###;0}");
            sb.AppendLine();
            sb.AppendLine(BuildLawStatusLine(hasEduMinistry, "LAW_EDU_MINISTRY"));
            sb.AppendLine(BuildLawStatusLine(hasSchooling, "LAW_BASIC_SCHOOLING"));
            sb.AppendLine(BuildLawStatusLine(hasHigherEdu, "LAW_HIGHER_EDU_CHARTER"));
            sb.AppendLine(BuildLawStatusLine(hasScholarships, "LAW_SCHOLARSHIP_PROGRAM"));
            sb.AppendLine(BuildLawStatusLine(hasTeacherCertification, "LAW_TEACHER_CERTIFICATION"));
            sb.AppendLine(BuildLawStatusLine(PublicSystemsEngine.HasLaw(lawCodes, PublicSystemsEngine.LawIntlCooperation), "LAW_INTL_COOPERATION"));
            sb.AppendLine(BuildLawStatusLine(PublicSystemsEngine.HasLaw(lawCodes, PublicSystemsEngine.LawIntlAcademicExchange), "LAW_INTL_ACADEMIC_EXCHANGE"));

            if (!string.IsNullOrWhiteSpace(statusMessage))
            {
                sb.AppendLine();
                sb.AppendLine(statusMessage);
            }

            EconomyPublicModuleRouteByUser[userId] = PublicRouteAdminEducationBudget;
            EconomyPublicUiContext[userId] = (chatId, msgId);
            await SafeEditMessageTextAsync(
                botClient,
                chatId,
                msgId,
                sb.ToString(),
                StateKeyboardBuilder.AdminEducationMenuKeyboard(canControl),
                cancellationToken);
        }

        private async Task ShowAdminEducationReport(
            ITelegramBotClient botClient,
            long userId,
            long chatId,
            int msgId,
            CancellationToken cancellationToken,
            string? statusMessage = null)
        {
            var selectedCountryId = GetSelectedCountryIdForUser(userId);
            if (!selectedCountryId.HasValue)
            {
                await ShowAdminEducationMenu(botClient, userId, chatId, msgId, cancellationToken, "Не знайдено країни.");
                return;
            }

            var country = CountriesData.GetCountryById(selectedCountryId.Value);
            if (country == null)
            {
                await ShowAdminEducationMenu(botClient, userId, chatId, msgId, cancellationToken, "Країну не знайдено.");
                return;
            }

            var systems = CountryPublicSystemsData.GetByCountry(country.CountryId);
            int specialistsBase = Math.Max(0, (int)Math.Floor((systems.EduLiteracy + systems.EduQuality) / 40f));
            int unrestPressure = 0;
            if (systems.EduQuality > 70f && systems.EduCohesion < 40f)
                unrestPressure += 1;
            if (country.Unrest > 60f)
                unrestPressure += 1;

            var sb = new StringBuilder();
            sb.AppendLine($"📊 Звіт МОН: {FormatCountryName(country)}");
            sb.AppendLine($"Грамотність: {systems.EduLiteracy:0.#}");
            sb.AppendLine($"Якість: {systems.EduQuality:0.#}");
            sb.AppendLine($"Фахівці: {systems.EduSpecialists}");
            sb.AppendLine($"Згуртованість: {systems.EduCohesion:0.#}");
            sb.AppendLine($"Бюджет МОН: {FmtNumber(systems.BudgetEducation)} / тік");
            sb.AppendLine($"Базовий приріст фахівців: +{FmtNumber(specialistsBase)} / тік");
            sb.AppendLine($"Тиск соцнапруги (оцінка): {FmtNumber(unrestPressure)}");
            sb.AppendLine($"Міжнародний бюджет: {FmtNumber(systems.BudgetIntl)} ({FormatIntlBudgetModeUk(systems.BudgetIntlMode)})");
            sb.AppendLine($"Борг публічних бюджетів: {FmtNumber(systems.BudgetDebt)}");

            if (!string.IsNullOrWhiteSpace(statusMessage))
            {
                sb.AppendLine();
                sb.AppendLine(statusMessage);
            }

            EconomyPublicModuleRouteByUser[userId] = PublicRouteAdminEducationBudget;
            EconomyPublicUiContext[userId] = (chatId, msgId);
            await SafeEditMessageTextAsync(
                botClient,
                chatId,
                msgId,
                sb.ToString(),
                StateKeyboardBuilder.AdminEducationMenuKeyboard(HasPublicControlAccess(userId, selectedCountryId)),
                cancellationToken);
        }

        private async Task ShowAdminEducationBudget(
            ITelegramBotClient botClient,
            long userId,
            long chatId,
            int msgId,
            CancellationToken cancellationToken,
            string? statusMessage = null)
        {
            var selectedCountryId = GetSelectedCountryIdForUser(userId);
            if (!selectedCountryId.HasValue)
            {
                await ShowAdminEducationMenu(botClient, userId, chatId, msgId, cancellationToken, "Не знайдено країни.");
                return;
            }

            var country = CountriesData.GetCountryById(selectedCountryId.Value);
            if (country == null)
            {
                await ShowAdminEducationMenu(botClient, userId, chatId, msgId, cancellationToken, "Країну не знайдено.");
                return;
            }

            bool canControl = HasPublicControlAccess(userId, selectedCountryId);
            var systems = CountryPublicSystemsData.GetByCountry(country.CountryId);
            string intlMode = PublicSystemsEngine.NormalizeIntlBudgetMode(systems.BudgetIntlMode);

            var sb = new StringBuilder();
            sb.AppendLine($"💸 Бюджети МОН: {FormatCountryName(country)}");
            sb.AppendLine($"Казна: {FmtNumber(country.Treasury)}");
            sb.AppendLine($"МОН: {FmtNumber(systems.BudgetEducation)} / тік");
            sb.AppendLine($"Міжнародний бюджет: {FmtNumber(systems.BudgetIntl)} / тік");
            sb.AppendLine($"Режим міжнародного бюджету: {FormatIntlBudgetModeUk(intlMode)}");
            sb.AppendLine($"Борг публічних бюджетів: {FmtNumber(systems.BudgetDebt)}");
            sb.AppendLine();
            sb.AppendLine("Команди (можна і українські аргументи):");
            sb.AppendLine("`set_budget(education, 1000)`");
            sb.AppendLine("`set_budget_intl(auto|manual|авто|вручну)`");

            if (!string.IsNullOrWhiteSpace(statusMessage))
            {
                sb.AppendLine();
                sb.AppendLine(statusMessage);
            }

            EconomyPublicModuleRouteByUser[userId] = PublicRouteAdminEducationBudget;
            EconomyPublicUiContext[userId] = (chatId, msgId);
            await SafeEditMessageTextAsync(
                botClient,
                chatId,
                msgId,
                sb.ToString(),
                StateKeyboardBuilder.AdminEducationBudgetKeyboard(intlMode.Equals("Auto", StringComparison.OrdinalIgnoreCase), canControl),
                cancellationToken);
        }

        private async Task ShowAdminEducationActions(
            ITelegramBotClient botClient,
            long userId,
            long chatId,
            int msgId,
            CancellationToken cancellationToken,
            string? statusMessage = null)
        {
            var selectedCountryId = GetSelectedCountryIdForUser(userId);
            if (!selectedCountryId.HasValue)
            {
                await ShowAdminEducationMenu(botClient, userId, chatId, msgId, cancellationToken, "Не знайдено країни.");
                return;
            }

            bool canControl = HasPublicControlAccess(userId, selectedCountryId);
            if (!canControl)
            {
                await ShowAdminEducationMenu(botClient, userId, chatId, msgId, cancellationToken, "Нема доступу.");
                return;
            }

            var systems = CountryPublicSystemsData.GetByCountry(selectedCountryId.Value);
            var lawCodes = BuildAcceptedLawCodes(selectedCountryId.Value);

            var sb = new StringBuilder();
            sb.AppendLine("🛠 Реформи МОН");
            sb.AppendLine(BuildLawStatusLine(PublicSystemsEngine.HasLaw(lawCodes, PublicSystemsEngine.LawEduMinistry), "LAW_EDU_MINISTRY"));
            sb.AppendLine(BuildLawStatusLine(PublicSystemsEngine.HasLaw(lawCodes, PublicSystemsEngine.LawBasicSchooling), "LAW_BASIC_SCHOOLING"));
            sb.AppendLine(BuildLawStatusLine(PublicSystemsEngine.HasLaw(lawCodes, PublicSystemsEngine.LawHigherEduCharter), "LAW_HIGHER_EDU_CHARTER"));
            sb.AppendLine(BuildLawStatusLine(PublicSystemsEngine.HasLaw(lawCodes, PublicSystemsEngine.LawScholarshipProgram), "LAW_SCHOLARSHIP_PROGRAM"));
            sb.AppendLine(BuildLawStatusLine(PublicSystemsEngine.HasLaw(lawCodes, PublicSystemsEngine.LawTeacherCertification), "LAW_TEACHER_CERTIFICATION"));
            sb.AppendLine();
            sb.AppendLine($"Грам/Якість/Фах/Згурт: {systems.EduLiteracy:0.#}/{systems.EduQuality:0.#}/{systems.EduSpecialists}/{systems.EduCohesion:0.#}");
            sb.AppendLine("Дії витрачають екстрені кошти МОН, що списуються на наступному тіку.");

            if (!string.IsNullOrWhiteSpace(statusMessage))
            {
                sb.AppendLine();
                sb.AppendLine(statusMessage);
            }

            EconomyPublicModuleRouteByUser[userId] = PublicRouteAdminEducationBudget;
            EconomyPublicUiContext[userId] = (chatId, msgId);
            await SafeEditMessageTextAsync(
                botClient,
                chatId,
                msgId,
                sb.ToString(),
                StateKeyboardBuilder.AdminEducationActionsKeyboard(canControl),
                cancellationToken);
        }

        private async Task ApplyAdminEducationLiteracyCampaign(
            ITelegramBotClient botClient,
            long userId,
            long chatId,
            int msgId,
            CancellationToken cancellationToken)
        {
            var selectedCountryId = GetSelectedCountryIdForUser(userId);
            if (!HasPublicControlAccess(userId, selectedCountryId))
            {
                await ShowAdminEducationActions(botClient, userId, chatId, msgId, cancellationToken, "Нема доступу.");
                return;
            }
            if (!selectedCountryId.HasValue)
            {
                await ShowAdminEducationActions(botClient, userId, chatId, msgId, cancellationToken, "Не знайдено країни.");
                return;
            }

            var lawCodes = BuildAcceptedLawCodes(selectedCountryId.Value);
            if (!PublicSystemsEngine.HasLaw(lawCodes, PublicSystemsEngine.LawBasicSchooling))
            {
                await ShowAdminEducationActions(botClient, userId, chatId, msgId, cancellationToken, BuildLawRequirementMessage(PublicSystemsEngine.LawBasicSchooling));
                return;
            }

            var country = CountriesData.GetCountryById(selectedCountryId.Value);
            if (country == null)
            {
                await ShowAdminEducationActions(botClient, userId, chatId, msgId, cancellationToken, "Країну не знайдено.");
                return;
            }

            var systems = CountryPublicSystemsData.GetByCountry(selectedCountryId.Value);
            float literacyGain = country.WarState || country.IsInCrisis ? 2f : 3f;
            decimal cost = Math.Max(0m, EconomyConfigData.GetDecimal("PublicEducationLiteracyCost", 220m));
            systems.EduLiteracy = EconomyCalculator.Clamp(systems.EduLiteracy + literacyGain, 0f, 100f);
            systems.EduCohesion = EconomyCalculator.Clamp(systems.EduCohesion + 1f, 0f, 100f);
            systems.PendingEmergencyEducationSpend += cost;
            CountryPublicSystemsData.Upsert(systems);

            await ShowAdminEducationActions(
                botClient,
                userId,
                chatId,
                msgId,
                cancellationToken,
                $"✅ Кампанія грамотності: Грамотність +{literacyGain:0.#}, Згуртованість +1, витрати {FmtNumber(cost)}.");
        }

        private async Task ApplyAdminEducationTeacherTraining(
            ITelegramBotClient botClient,
            long userId,
            long chatId,
            int msgId,
            CancellationToken cancellationToken)
        {
            var selectedCountryId = GetSelectedCountryIdForUser(userId);
            if (!HasPublicControlAccess(userId, selectedCountryId))
            {
                await ShowAdminEducationActions(botClient, userId, chatId, msgId, cancellationToken, "Нема доступу.");
                return;
            }
            if (!selectedCountryId.HasValue)
            {
                await ShowAdminEducationActions(botClient, userId, chatId, msgId, cancellationToken, "Не знайдено країни.");
                return;
            }

            var lawCodes = BuildAcceptedLawCodes(selectedCountryId.Value);
            if (!PublicSystemsEngine.HasLaw(lawCodes, PublicSystemsEngine.LawHigherEduCharter))
            {
                await ShowAdminEducationActions(botClient, userId, chatId, msgId, cancellationToken, BuildLawRequirementMessage(PublicSystemsEngine.LawHigherEduCharter));
                return;
            }

            var systems = CountryPublicSystemsData.GetByCountry(selectedCountryId.Value);
            decimal cost = Math.Max(0m, EconomyConfigData.GetDecimal("PublicEducationTeacherTrainingCost", 260m));
            systems.EduQuality = EconomyCalculator.Clamp(systems.EduQuality + 3f, 0f, 100f);
            int specGain = systems.EduSpecialists < 10 ? 3 : 2;
            systems.EduSpecialists = Math.Clamp(systems.EduSpecialists + specGain, 0, 999);
            systems.PendingEmergencyEducationSpend += cost;
            CountryPublicSystemsData.Upsert(systems);

            await ShowAdminEducationActions(
                botClient,
                userId,
                chatId,
                msgId,
                cancellationToken,
                $"✅ Підготовка викладачів: Якість +3, Фах +{FmtNumber(specGain)}, витрати {FmtNumber(cost)}.");
        }

        private async Task ApplyAdminEducationScholarshipPush(
            ITelegramBotClient botClient,
            long userId,
            long chatId,
            int msgId,
            CancellationToken cancellationToken)
        {
            var selectedCountryId = GetSelectedCountryIdForUser(userId);
            if (!HasPublicControlAccess(userId, selectedCountryId))
            {
                await ShowAdminEducationActions(botClient, userId, chatId, msgId, cancellationToken, "Нема доступу.");
                return;
            }
            if (!selectedCountryId.HasValue)
            {
                await ShowAdminEducationActions(botClient, userId, chatId, msgId, cancellationToken, "Не знайдено країни.");
                return;
            }

            var lawCodes = BuildAcceptedLawCodes(selectedCountryId.Value);
            if (!PublicSystemsEngine.HasLaw(lawCodes, PublicSystemsEngine.LawScholarshipProgram))
            {
                await ShowAdminEducationActions(botClient, userId, chatId, msgId, cancellationToken, BuildLawRequirementMessage(PublicSystemsEngine.LawScholarshipProgram));
                return;
            }

            var systems = CountryPublicSystemsData.GetByCountry(selectedCountryId.Value);
            decimal cost = Math.Max(0m, EconomyConfigData.GetDecimal("PublicEducationScholarshipCost", 320m));
            systems.EduQuality = EconomyCalculator.Clamp(systems.EduQuality + 1f, 0f, 100f);
            systems.EduSpecialists = Math.Clamp(systems.EduSpecialists + 4, 0, 999);
            float cohesionDelta = systems.EduCohesion < 40f ? -1f : 1f;
            systems.EduCohesion = EconomyCalculator.Clamp(systems.EduCohesion + cohesionDelta, 0f, 100f);
            systems.PendingEmergencyEducationSpend += cost;
            CountryPublicSystemsData.Upsert(systems);

            await ShowAdminEducationActions(
                botClient,
                userId,
                chatId,
                msgId,
                cancellationToken,
                $"✅ Стипендійний поштовх: Якість +1, Фах +4, Згурт {(cohesionDelta >= 0 ? "+1" : "-1")}, витрати {FmtNumber(cost)}.");
        }

        private async Task ApplyAdminEducationCivicsProgram(
            ITelegramBotClient botClient,
            long userId,
            long chatId,
            int msgId,
            CancellationToken cancellationToken)
        {
            var selectedCountryId = GetSelectedCountryIdForUser(userId);
            if (!HasPublicControlAccess(userId, selectedCountryId))
            {
                await ShowAdminEducationActions(botClient, userId, chatId, msgId, cancellationToken, "Нема доступу.");
                return;
            }
            if (!selectedCountryId.HasValue)
            {
                await ShowAdminEducationActions(botClient, userId, chatId, msgId, cancellationToken, "Не знайдено країни.");
                return;
            }

            var lawCodes = BuildAcceptedLawCodes(selectedCountryId.Value);
            if (!PublicSystemsEngine.HasLaw(lawCodes, PublicSystemsEngine.LawEduMinistry))
            {
                await ShowAdminEducationActions(botClient, userId, chatId, msgId, cancellationToken, BuildLawRequirementMessage(PublicSystemsEngine.LawEduMinistry));
                return;
            }

            var country = CountriesData.GetCountryById(selectedCountryId.Value);
            if (country == null)
            {
                await ShowAdminEducationActions(botClient, userId, chatId, msgId, cancellationToken, "Країну не знайдено.");
                return;
            }

            var systems = CountryPublicSystemsData.GetByCountry(selectedCountryId.Value);
            decimal cost = Math.Max(0m, EconomyConfigData.GetDecimal("PublicEducationCivicsCost", 180m));
            bool unrestReduced = country.Unrest > 60f;
            systems.EduCohesion = EconomyCalculator.Clamp(systems.EduCohesion + 3f, 0f, 100f);
            if (unrestReduced)
                country.Unrest = EconomyCalculator.Clamp(country.Unrest - 2f, 0f, 100f);
            systems.PendingEmergencyEducationSpend += cost;
            CountryPublicSystemsData.Upsert(systems);
            CountriesData.UpdateCountry(country);

            await ShowAdminEducationActions(
                botClient,
                userId,
                chatId,
                msgId,
                cancellationToken,
                $"✅ Громадянська програма: Згурт +3, Соцнапруга {(unrestReduced ? "-2" : "±0")}, витрати {FmtNumber(cost)}.");
        }

        private async Task ApplyAdminEducationTeacherCertification(
            ITelegramBotClient botClient,
            long userId,
            long chatId,
            int msgId,
            CancellationToken cancellationToken)
        {
            var selectedCountryId = GetSelectedCountryIdForUser(userId);
            if (!HasPublicControlAccess(userId, selectedCountryId))
            {
                await ShowAdminEducationActions(botClient, userId, chatId, msgId, cancellationToken, "Нема доступу.");
                return;
            }
            if (!selectedCountryId.HasValue)
            {
                await ShowAdminEducationActions(botClient, userId, chatId, msgId, cancellationToken, "Не знайдено країни.");
                return;
            }

            var lawCodes = BuildAcceptedLawCodes(selectedCountryId.Value);
            if (!PublicSystemsEngine.HasLaw(lawCodes, PublicSystemsEngine.LawTeacherCertification))
            {
                await ShowAdminEducationActions(
                    botClient,
                    userId,
                    chatId,
                    msgId,
                    cancellationToken,
                    BuildLawRequirementMessage(PublicSystemsEngine.LawTeacherCertification));
                return;
            }

            var systems = CountryPublicSystemsData.GetByCountry(selectedCountryId.Value);
            decimal cost = Math.Max(0m, EconomyConfigData.GetDecimal("PublicEducationTeacherCertificationCost", 300m));

            systems.EduQuality = EconomyCalculator.Clamp(systems.EduQuality + 2f, 0f, 100f);
            systems.EduSpecialists = Math.Clamp(systems.EduSpecialists + 2, 0, 999);
            systems.EduCohesion = EconomyCalculator.Clamp(systems.EduCohesion + 1f, 0f, 100f);
            systems.PendingEmergencyEducationSpend += cost;
            CountryPublicSystemsData.Upsert(systems);

            await ShowAdminEducationActions(
                botClient,
                userId,
                chatId,
                msgId,
                cancellationToken,
                $"✅ STEM-гранти та сертифікація: Якість +2, Фах +2, Згурт +1, витрати {FmtNumber(cost)}.");
        }

        private async Task ApplyAdminEducationHighDosageTutoring(
            ITelegramBotClient botClient,
            long userId,
            long chatId,
            int msgId,
            CancellationToken cancellationToken)
        {
            var selectedCountryId = GetSelectedCountryIdForUser(userId);
            if (!HasPublicControlAccess(userId, selectedCountryId))
            {
                await ShowAdminEducationActions(botClient, userId, chatId, msgId, cancellationToken, "Нема доступу.");
                return;
            }
            if (!selectedCountryId.HasValue)
            {
                await ShowAdminEducationActions(botClient, userId, chatId, msgId, cancellationToken, "Не знайдено країни.");
                return;
            }

            var lawCodes = BuildAcceptedLawCodes(selectedCountryId.Value);
            if (!PublicSystemsEngine.HasLaw(lawCodes, PublicSystemsEngine.LawBasicSchooling))
            {
                await ShowAdminEducationActions(botClient, userId, chatId, msgId, cancellationToken, BuildLawRequirementMessage(PublicSystemsEngine.LawBasicSchooling));
                return;
            }

            var systems = CountryPublicSystemsData.GetByCountry(selectedCountryId.Value);
            decimal cost = Math.Max(0m, EconomyConfigData.GetDecimal("PublicEducationTutoringCost", 290m));
            float literacyGain = systems.EduLiteracy < 60f ? 4f : 2.5f;
            float qualityGain = systems.EduQuality < 65f ? 2.5f : 1.5f;
            int specialistsGain = systems.EduSpecialists < 40 ? 2 : 1;

            systems.EduLiteracy = EconomyCalculator.Clamp(systems.EduLiteracy + literacyGain, 0f, 100f);
            systems.EduQuality = EconomyCalculator.Clamp(systems.EduQuality + qualityGain, 0f, 100f);
            systems.EduSpecialists = Math.Clamp(systems.EduSpecialists + specialistsGain, 0, 999);
            systems.PendingEmergencyEducationSpend += cost;
            CountryPublicSystemsData.Upsert(systems);

            await ShowAdminEducationActions(
                botClient,
                userId,
                chatId,
                msgId,
                cancellationToken,
                $"✅ Інтенсивне тьюторство: Грамотність +{literacyGain:0.#}, Якість +{qualityGain:0.#}, Фах +{FmtNumber(specialistsGain)}, витрати {FmtNumber(cost)}.");
        }

        private async Task ApplyAdminEducationSchoolMeals(
            ITelegramBotClient botClient,
            long userId,
            long chatId,
            int msgId,
            CancellationToken cancellationToken)
        {
            var selectedCountryId = GetSelectedCountryIdForUser(userId);
            if (!HasPublicControlAccess(userId, selectedCountryId))
            {
                await ShowAdminEducationActions(botClient, userId, chatId, msgId, cancellationToken, "Нема доступу.");
                return;
            }
            if (!selectedCountryId.HasValue)
            {
                await ShowAdminEducationActions(botClient, userId, chatId, msgId, cancellationToken, "Не знайдено країни.");
                return;
            }

            var lawCodes = BuildAcceptedLawCodes(selectedCountryId.Value);
            if (!PublicSystemsEngine.HasLaw(lawCodes, PublicSystemsEngine.LawBasicSchooling))
            {
                await ShowAdminEducationActions(botClient, userId, chatId, msgId, cancellationToken, BuildLawRequirementMessage(PublicSystemsEngine.LawBasicSchooling));
                return;
            }

            var country = CountriesData.GetCountryById(selectedCountryId.Value);
            if (country == null)
            {
                await ShowAdminEducationActions(botClient, userId, chatId, msgId, cancellationToken, "Країну не знайдено.");
                return;
            }

            var systems = CountryPublicSystemsData.GetByCountry(selectedCountryId.Value);
            decimal cost = Math.Max(0m, EconomyConfigData.GetDecimal("PublicEducationSchoolMealsCost", 260m));
            float cohesionGain = country.WarState || country.IsInCrisis ? 2f : 3f;
            float literacyGain = 1.5f;
            float healthPublicGain = 1f;
            float happinessGain = country.Happiness < 75f ? 2f : 1f;
            float unrestDrop = country.Unrest > 45f ? 2f : 1f;

            systems.EduCohesion = EconomyCalculator.Clamp(systems.EduCohesion + cohesionGain, 0f, 100f);
            systems.EduLiteracy = EconomyCalculator.Clamp(systems.EduLiteracy + literacyGain, 0f, 100f);
            systems.HealthPublic = EconomyCalculator.Clamp(systems.HealthPublic + healthPublicGain, 0f, 100f);
            systems.PendingEmergencyEducationSpend += cost;

            country.Happiness = EconomyCalculator.Clamp(country.Happiness + happinessGain, 0f, 100f);
            country.Unrest = EconomyCalculator.Clamp(country.Unrest - unrestDrop, 0f, 100f);

            CountryPublicSystemsData.Upsert(systems);
            CountriesData.UpdateCountry(country);

            await ShowAdminEducationActions(
                botClient,
                userId,
                chatId,
                msgId,
                cancellationToken,
                $"✅ Шкільне харчування: Згурт +{cohesionGain:0.#}, Грамотність +{literacyGain:0.#}, Рівень здоров'я +{healthPublicGain:0.#}, Щастя +{happinessGain:0.#}, Соцнапруга -{unrestDrop:0.#}, витрати {FmtNumber(cost)}.");
        }

        private async Task ApplyAdminEducationTeacherMentoring(
            ITelegramBotClient botClient,
            long userId,
            long chatId,
            int msgId,
            CancellationToken cancellationToken)
        {
            var selectedCountryId = GetSelectedCountryIdForUser(userId);
            if (!HasPublicControlAccess(userId, selectedCountryId))
            {
                await ShowAdminEducationActions(botClient, userId, chatId, msgId, cancellationToken, "Нема доступу.");
                return;
            }
            if (!selectedCountryId.HasValue)
            {
                await ShowAdminEducationActions(botClient, userId, chatId, msgId, cancellationToken, "Не знайдено країни.");
                return;
            }

            var lawCodes = BuildAcceptedLawCodes(selectedCountryId.Value);
            if (!PublicSystemsEngine.HasLaw(lawCodes, PublicSystemsEngine.LawTeacherCertification))
            {
                await ShowAdminEducationActions(botClient, userId, chatId, msgId, cancellationToken, BuildLawRequirementMessage(PublicSystemsEngine.LawTeacherCertification));
                return;
            }

            var systems = CountryPublicSystemsData.GetByCountry(selectedCountryId.Value);
            decimal cost = Math.Max(0m, EconomyConfigData.GetDecimal("PublicEducationTeacherMentoringCost", 240m));
            float qualityGain = systems.EduQuality < 70f ? 3f : 2f;
            int specialistsGain = systems.EduSpecialists < 80 ? 3 : 2;
            float cohesionGain = 1.5f;

            systems.EduQuality = EconomyCalculator.Clamp(systems.EduQuality + qualityGain, 0f, 100f);
            systems.EduSpecialists = Math.Clamp(systems.EduSpecialists + specialistsGain, 0, 999);
            systems.EduCohesion = EconomyCalculator.Clamp(systems.EduCohesion + cohesionGain, 0f, 100f);
            systems.PendingEmergencyEducationSpend += cost;
            CountryPublicSystemsData.Upsert(systems);

            await ShowAdminEducationActions(
                botClient,
                userId,
                chatId,
                msgId,
                cancellationToken,
                $"✅ Наставництво вчителів: Якість +{qualityGain:0.#}, Фах +{FmtNumber(specialistsGain)}, Згурт +{cohesionGain:0.#}, витрати {FmtNumber(cost)}.");
        }

        private async Task ShowAdminEducationIntlPrograms(
            ITelegramBotClient botClient,
            long userId,
            long chatId,
            int msgId,
            CancellationToken cancellationToken,
            string? statusMessage = null)
        {
            var selectedCountryId = GetSelectedCountryIdForUser(userId);
            if (!selectedCountryId.HasValue)
            {
                await ShowAdminEducationMenu(botClient, userId, chatId, msgId, cancellationToken, "Не знайдено країни.");
                return;
            }

            bool canControl = HasPublicControlAccess(userId, selectedCountryId);
            var systems = CountryPublicSystemsData.GetByCountry(selectedCountryId.Value);
            var programs = CountryIntlProgramsData.GetByCountry(selectedCountryId.Value)
                .Where(p => AdminEducationProgramTypes.Contains(PublicSystemsEngine.NormalizeProgramType(p.ProgramType), StringComparer.OrdinalIgnoreCase))
                .ToList();
            var existingByType = programs
                .GroupBy(p => PublicSystemsEngine.NormalizeProgramType(p.ProgramType), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.OrderByDescending(x => x.IntlProgramId).First(), StringComparer.OrdinalIgnoreCase);

            var laws = BuildAcceptedLawCodes(selectedCountryId.Value);
            bool hasIntlCoop = PublicSystemsEngine.HasLaw(laws, PublicSystemsEngine.LawIntlCooperation);

            var items = new List<(string text, string data)>();
            foreach (var type in AdminEducationProgramTypes)
            {
                string display = PublicSystemsEngine.GetProgramDisplayNameUk(type);
                if (existingByType.TryGetValue(type, out var existing))
                {
                    string state = existing.IsActive ? "✅" : "⛔";
                    string cost = FmtNumber(existing.CostPerTick > 0m ? existing.CostPerTick : PublicSystemsEngine.GetDefaultProgramCost(type));
                    string data = canControl ? $"state_admin_education_intl_toggle_{existing.IntlProgramId}" : "state_noop";
                    items.Add(($"{state} {display} • {cost}/тік • Фах {FmtNumber(existing.RequiredSpec)}", data));
                }
                else if (canControl)
                {
                    items.Add(($"➕ {display}", $"state_admin_education_intl_add_{ProgramTypeToToken(type)}"));
                }
            }

            var sb = new StringBuilder();
            sb.AppendLine("🌍 Міжнародні програми МОН");
            sb.AppendLine($"Престиж: {systems.IntlPrestige:0.#}");
            sb.AppendLine($"Міжнародний бюджет: {FmtNumber(systems.BudgetIntl)} ({FormatIntlBudgetModeUk(systems.BudgetIntlMode)})");
            sb.AppendLine(BuildLawStatusLine(hasIntlCoop, "LAW_INTL_COOPERATION"));
            sb.AppendLine(BuildLawStatusLine(PublicSystemsEngine.HasLaw(laws, PublicSystemsEngine.LawIntlAcademicExchange), "LAW_INTL_ACADEMIC_EXCHANGE"));
            sb.AppendLine();
            sb.AppendLine("Натисніть елемент, щоб увімкнути/вимкнути або створити програму.");

            if (!string.IsNullOrWhiteSpace(statusMessage))
            {
                sb.AppendLine();
                sb.AppendLine(statusMessage);
            }

            EconomyPublicModuleRouteByUser[userId] = PublicRouteAdminEducationBudget;
            EconomyPublicUiContext[userId] = (chatId, msgId);
            await SafeEditMessageTextAsync(
                botClient,
                chatId,
                msgId,
                sb.ToString(),
                StateKeyboardBuilder.AdminEducationIntlKeyboard(items, canControl),
                cancellationToken);
        }

        private async Task ToggleAdminEducationIntlProgram(
            ITelegramBotClient botClient,
            long userId,
            long chatId,
            int msgId,
            int intlProgramId,
            CancellationToken cancellationToken)
        {
            var selectedCountryId = GetSelectedCountryIdForUser(userId);
            if (!HasPublicControlAccess(userId, selectedCountryId))
            {
                await ShowAdminEducationIntlPrograms(botClient, userId, chatId, msgId, cancellationToken, "Нема доступу.");
                return;
            }

            if (!selectedCountryId.HasValue)
            {
                await ShowAdminEducationIntlPrograms(botClient, userId, chatId, msgId, cancellationToken, "Не знайдено країни.");
                return;
            }

            var program = CountryIntlProgramsData.GetById(intlProgramId);
            if (program == null ||
                program.CountryId != selectedCountryId.Value ||
                !AdminEducationProgramTypes.Contains(PublicSystemsEngine.NormalizeProgramType(program.ProgramType), StringComparer.OrdinalIgnoreCase))
            {
                await ShowAdminEducationIntlPrograms(botClient, userId, chatId, msgId, cancellationToken, "Програму не знайдено.");
                return;
            }

            bool setActive = !program.IsActive;
            CountryIntlProgramsData.SetActive(selectedCountryId.Value, intlProgramId, setActive);
            await ShowAdminEducationIntlPrograms(
                botClient,
                userId,
                chatId,
                msgId,
                cancellationToken,
                setActive ? "✅ Програму увімкнено." : "✅ Програму вимкнено.");
        }

        private async Task AddAdminEducationIntlProgram(
            ITelegramBotClient botClient,
            long userId,
            long chatId,
            int msgId,
            string token,
            CancellationToken cancellationToken)
        {
            var selectedCountryId = GetSelectedCountryIdForUser(userId);
            if (!HasPublicControlAccess(userId, selectedCountryId))
            {
                await ShowAdminEducationIntlPrograms(botClient, userId, chatId, msgId, cancellationToken, "Нема доступу.");
                return;
            }

            if (!selectedCountryId.HasValue)
            {
                await ShowAdminEducationIntlPrograms(botClient, userId, chatId, msgId, cancellationToken, "Не знайдено країни.");
                return;
            }

            string programType = ProgramTokenToType(token);
            if (string.IsNullOrWhiteSpace(programType) ||
                !AdminEducationProgramTypes.Contains(PublicSystemsEngine.NormalizeProgramType(programType), StringComparer.OrdinalIgnoreCase))
            {
                await ShowAdminEducationIntlPrograms(botClient, userId, chatId, msgId, cancellationToken, "Невідомий тип програми.");
                return;
            }

            var laws = BuildAcceptedLawCodes(selectedCountryId.Value);
            if (!PublicSystemsEngine.HasLaw(laws, PublicSystemsEngine.LawIntlCooperation))
            {
                await ShowAdminEducationIntlPrograms(botClient, userId, chatId, msgId, cancellationToken, BuildLawRequirementMessage(PublicSystemsEngine.LawIntlCooperation));
                return;
            }

            if (!PublicSystemsEngine.HasLaw(laws, PublicSystemsEngine.LawIntlAcademicExchange))
            {
                await ShowAdminEducationIntlPrograms(botClient, userId, chatId, msgId, cancellationToken, BuildLawRequirementMessage(PublicSystemsEngine.LawIntlAcademicExchange));
                return;
            }

            var existing = CountryIntlProgramsData.GetByCountryAndType(selectedCountryId.Value, programType);
            if (existing != null)
            {
                if (!existing.IsActive)
                    CountryIntlProgramsData.SetActive(selectedCountryId.Value, existing.IntlProgramId, true);
                await ShowAdminEducationIntlPrograms(botClient, userId, chatId, msgId, cancellationToken, "✅ Програму активовано.");
                return;
            }

            var record = new CountryIntlProgramsData.CountryIntlProgramRecord
            {
                CountryId = selectedCountryId.Value,
                ProgramType = programType,
                CostPerTick = PublicSystemsEngine.GetDefaultProgramCost(programType),
                RequiredSpec = PublicSystemsEngine.GetDefaultProgramRequiredSpec(programType),
                IsActive = true,
                TicksActive = 0,
                SuccessTicks = 0
            };
            CountryIntlProgramsData.Upsert(record);
            await ShowAdminEducationIntlPrograms(botClient, userId, chatId, msgId, cancellationToken, "✅ Програму додано й активовано.");
        }

        private async Task<bool> TryHandlePublicSystemsTextCommand(
            ITelegramBotClient botClient,
            long userId,
            string input,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(input))
                return false;

            string raw = input.Trim();

            if (TryParseSetBudgetCommand(raw, out string budgetType, out decimal budgetAmount))
            {
                var selectedCountryId = GetSelectedCountryIdForUser(userId);
                if (!HasPublicControlAccess(userId, selectedCountryId))
                {
                    await botClient.SendTextMessageAsync(userId, "Нема доступу.", cancellationToken: cancellationToken);
                    return true;
                }

                if (!selectedCountryId.HasValue)
                {
                    await botClient.SendTextMessageAsync(userId, "Не знайдено країни.", cancellationToken: cancellationToken);
                    return true;
                }

                var systems = CountryPublicSystemsData.GetByCountry(selectedCountryId.Value);
                string label;
                switch (budgetType)
                {
                    case "health":
                        systems.BudgetHealth = budgetAmount;
                        label = "МОЗ";
                        break;
                    case "education":
                        systems.BudgetEducation = budgetAmount;
                        label = "МОН";
                        break;
                    default:
                        await botClient.SendTextMessageAsync(userId, "❌ Невідомий бюджет. Доступно: health, education.", cancellationToken: cancellationToken);
                        return true;
                }

                CountryPublicSystemsData.Upsert(systems);

                if (EconomyPublicUiContext.TryGetValue(userId, out var ui) && ui.ChatId != 0 && ui.MsgId != 0)
                {
                    string route = EconomyPublicModuleRouteByUser.GetValueOrDefault(userId, PublicRouteLegacyBudget);
                    await ShowPublicBudgetByRoute(
                        botClient,
                        userId,
                        ui.ChatId,
                        ui.MsgId,
                        route,
                        cancellationToken,
                        $"✅ Бюджет {label} встановлено: {FmtNumber(budgetAmount)} / тік.");
                }
                else
                {
                    await botClient.SendTextMessageAsync(userId, $"✅ Бюджет {label} встановлено: {FmtNumber(budgetAmount)} / тік.", cancellationToken: cancellationToken);
                }

                return true;
            }

            if (TryParseSetBudgetIntlMode(raw, out bool intlAuto))
            {
                var selectedCountryId = GetSelectedCountryIdForUser(userId);
                if (!HasPublicControlAccess(userId, selectedCountryId))
                {
                    await botClient.SendTextMessageAsync(userId, "Нема доступу.", cancellationToken: cancellationToken);
                    return true;
                }

                if (!selectedCountryId.HasValue)
                {
                    await botClient.SendTextMessageAsync(userId, "Не знайдено країни.", cancellationToken: cancellationToken);
                    return true;
                }

                var systems = CountryPublicSystemsData.GetByCountry(selectedCountryId.Value);
                systems.BudgetIntlMode = intlAuto ? "Auto" : "Manual";
                CountryPublicSystemsData.Upsert(systems);

                if (EconomyPublicUiContext.TryGetValue(userId, out var ui) && ui.ChatId != 0 && ui.MsgId != 0)
                {
                    string route = EconomyPublicModuleRouteByUser.GetValueOrDefault(userId, PublicRouteLegacyBudget);
                    await ShowPublicBudgetByRoute(
                        botClient,
                        userId,
                        ui.ChatId,
                        ui.MsgId,
                        route,
                        cancellationToken,
                        $"✅ Режим міжнародного бюджету: {FormatIntlBudgetModeUk(systems.BudgetIntlMode)}.");
                }
                else
                {
                    await botClient.SendTextMessageAsync(
                        userId,
                        $"✅ Режим міжнародного бюджету: {FormatIntlBudgetModeUk(systems.BudgetIntlMode)}.",
                        cancellationToken: cancellationToken);
                }

                return true;
            }

            if (TryParseCommandArgs(raw, "gm_disease_target", out var targetArgs))
            {
                if (userId != masterID)
                {
                    await botClient.SendTextMessageAsync(userId, "Нема доступу.", cancellationToken: cancellationToken);
                    return true;
                }

                if (targetArgs.Length < 1)
                {
                    await botClient.SendTextMessageAsync(userId, "Формат: gm_disease_target(whole_state|region|city)", cancellationToken: cancellationToken);
                    return true;
                }

                string target = NormalizePublicDiseaseTargetScope(targetArgs[0]);
                PublicGmDiseaseTargetByUser[userId] = target;
                await botClient.SendTextMessageAsync(userId, $"✅ Ціль нових хвороб: {FormatPublicDiseaseTargetUk(target)}.", cancellationToken: cancellationToken);
                return true;
            }

            if (TryParseCommandArgs(raw, "gm_spawn_disease", out var spawnArgs))
            {
                if (userId != masterID)
                {
                    await botClient.SendTextMessageAsync(userId, "Нема доступу.", cancellationToken: cancellationToken);
                    return true;
                }

                if (spawnArgs.Length < 4 ||
                    !int.TryParse(spawnArgs[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int severity) ||
                    !int.TryParse(spawnArgs[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out int infectiousness) ||
                    !int.TryParse(spawnArgs[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out int duration))
                {
                    await botClient.SendTextMessageAsync(userId, "Формат: gm_spawn_disease(name, severity, infectiousness, duration)", cancellationToken: cancellationToken);
                    return true;
                }

                var selectedCountryId = GetSelectedCountryIdForUser(userId);
                if (!selectedCountryId.HasValue)
                {
                    await botClient.SendTextMessageAsync(userId, "Не знайдено країни.", cancellationToken: cancellationToken);
                    return true;
                }

                string name = TrimQuotes(spawnArgs[0]);
                if (string.IsNullOrWhiteSpace(name))
                {
                    await botClient.SendTextMessageAsync(userId, "❌ Вкажіть назву хвороби.", cancellationToken: cancellationToken);
                    return true;
                }

                string target = spawnArgs.Length >= 5
                    ? NormalizePublicDiseaseTargetScope(spawnArgs[4])
                    : NormalizePublicDiseaseTargetScope(PublicGmDiseaseTargetByUser.GetValueOrDefault(userId, "whole_state"));

                var record = new CountryDiseasesData.CountryDiseaseRecord
                {
                    CountryId = selectedCountryId.Value,
                    Name = name,
                    Severity = Math.Clamp(severity, 1, 5),
                    Infectiousness = Math.Clamp(infectiousness, 1, 5),
                    DurationTicks = Math.Max(1, duration),
                    RemainingTicks = Math.Max(1, duration),
                    Stage = 1,
                    TargetScope = target,
                    Active = true
                };
                CountryDiseasesData.Upsert(record);

                if (EconomyPublicUiContext.TryGetValue(userId, out var ui) && ui.ChatId != 0 && ui.MsgId != 0)
                {
                    await ShowPublicDiseases(
                        botClient,
                        userId,
                        ui.ChatId,
                        ui.MsgId,
                        cancellationToken,
                        $"✅ Додано хворобу \"{name}\" ({FormatPublicDiseaseTargetUk(target)}).");
                }
                else
                {
                    await botClient.SendTextMessageAsync(userId, $"✅ Додано хворобу \"{name}\" ({FormatPublicDiseaseTargetUk(target)}).", cancellationToken: cancellationToken);
                }

                return true;
            }

            if (TryParseCommandArgs(raw, "gm_set_disease_stage", out var stageArgs))
            {
                if (userId != masterID)
                {
                    await botClient.SendTextMessageAsync(userId, "Нема доступу.", cancellationToken: cancellationToken);
                    return true;
                }

                if (stageArgs.Length < 2 || !int.TryParse(stageArgs[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int stage))
                {
                    await botClient.SendTextMessageAsync(userId, "Формат: gm_set_disease_stage(name_or_id, stage)", cancellationToken: cancellationToken);
                    return true;
                }

                var selectedCountryId = GetSelectedCountryIdForUser(userId);
                if (!selectedCountryId.HasValue)
                {
                    await botClient.SendTextMessageAsync(userId, "Не знайдено країни.", cancellationToken: cancellationToken);
                    return true;
                }

                string key = TrimQuotes(stageArgs[0]);
                var disease = FindDiseaseByNameOrId(selectedCountryId.Value, key, activeOnly: true);
                if (disease == null)
                {
                    await botClient.SendTextMessageAsync(userId, "❌ Активну хворобу не знайдено.", cancellationToken: cancellationToken);
                    return true;
                }

                if (stage <= 0)
                {
                    disease.Active = false;
                    disease.Stage = 0;
                    disease.RemainingTicks = 0;
                }
                else
                {
                    disease.Stage = Math.Clamp(stage, 1, 4);
                }

                CountryDiseasesData.Upsert(disease);
                await botClient.SendTextMessageAsync(
                    userId,
                    disease.Active
                        ? $"✅ Стадію встановлено: {disease.Stage} ({FormatPublicStageUk(disease.Stage)})."
                        : "✅ Хворобу завершено.",
                    cancellationToken: cancellationToken);
                return true;
            }

            if (TryParseCommandArgs(raw, "gm_end_disease", out var endArgs))
            {
                if (userId != masterID)
                {
                    await botClient.SendTextMessageAsync(userId, "Нема доступу.", cancellationToken: cancellationToken);
                    return true;
                }

                if (endArgs.Length < 1)
                {
                    await botClient.SendTextMessageAsync(userId, "Формат: gm_end_disease(name_or_id)", cancellationToken: cancellationToken);
                    return true;
                }

                var selectedCountryId = GetSelectedCountryIdForUser(userId);
                if (!selectedCountryId.HasValue)
                {
                    await botClient.SendTextMessageAsync(userId, "Не знайдено країни.", cancellationToken: cancellationToken);
                    return true;
                }

                string key = TrimQuotes(endArgs[0]);
                var disease = FindDiseaseByNameOrId(selectedCountryId.Value, key, activeOnly: true);
                if (disease == null)
                {
                    await botClient.SendTextMessageAsync(userId, "❌ Активну хворобу не знайдено.", cancellationToken: cancellationToken);
                    return true;
                }

                CountryDiseasesData.EndDiseaseById(selectedCountryId.Value, disease.DiseaseId);
                await botClient.SendTextMessageAsync(userId, $"✅ Хворобу \"{disease.Name}\" завершено.", cancellationToken: cancellationToken);
                return true;
            }

            if (TryParseCommandArgs(raw, "gm_trigger_event", out var eventArgs))
            {
                if (userId != masterID)
                {
                    await botClient.SendTextMessageAsync(userId, "Нема доступу.", cancellationToken: cancellationToken);
                    return true;
                }

                if (eventArgs.Length < 1)
                {
                    await botClient.SendTextMessageAsync(userId, "Формат: gm_trigger_event(panic|scandal|breakthrough|import|паніка|скандал|прорив|завезення)", cancellationToken: cancellationToken);
                    return true;
                }

                var selectedCountryId = GetSelectedCountryIdForUser(userId);
                if (!selectedCountryId.HasValue)
                {
                    await botClient.SendTextMessageAsync(userId, "Не знайдено країни.", cancellationToken: cancellationToken);
                    return true;
                }

                var country = CountriesData.GetCountryById(selectedCountryId.Value);
                if (country == null)
                {
                    await botClient.SendTextMessageAsync(userId, "Країну не знайдено.", cancellationToken: cancellationToken);
                    return true;
                }

                var systems = CountryPublicSystemsData.GetByCountry(selectedCountryId.Value);
                string eventType = TrimQuotes(eventArgs[0]).Trim().ToLowerInvariant();
                string result;
                switch (eventType)
                {
                    case "panic":
                    case "паніка":
                        systems.HealthTrust = EconomyCalculator.Clamp(systems.HealthTrust - 4f, 0f, 100f);
                        systems.EduCohesion = EconomyCalculator.Clamp(systems.EduCohesion - 2f, 0f, 100f);
                        country.Unrest = EconomyCalculator.Clamp(country.Unrest + 2f, 0f, 100f);
                        result = "⚠ Подія паніка: Довіра МОЗ -4, Згуртованість -2, Соцнапруга +2.";
                        break;

                    case "scandal":
                    case "скандал":
                        systems.IntlPrestige = EconomyCalculator.Clamp(systems.IntlPrestige - 6f, 0f, 100f);
                        country.Stability = EconomyCalculator.Clamp(country.Stability - 2f, 0f, 100f);
                        result = "⚠ Подія скандал: Престиж -6, Стабільність -2.";
                        break;

                    case "breakthrough":
                    case "прорив":
                        systems.HealthCapacity = EconomyCalculator.Clamp(systems.HealthCapacity + 3f, 0f, 100f);
                        systems.EduQuality = EconomyCalculator.Clamp(systems.EduQuality + 2f, 0f, 100f);
                        systems.IntlPrestige = EconomyCalculator.Clamp(systems.IntlPrestige + 2f, 0f, 100f);
                        result = "✅ Подія прорив: Спроможність +3, Якість +2, Престиж +2.";
                        break;

                    case "import":
                    case "imported":
                    case "завезення":
                        {
                            string target = NormalizePublicDiseaseTargetScope(PublicGmDiseaseTargetByUser.GetValueOrDefault(userId, "whole_state"));
                            var disease = new CountryDiseasesData.CountryDiseaseRecord
                            {
                                CountryId = selectedCountryId.Value,
                                Name = "Імпортований спалах",
                                Severity = 2,
                                Infectiousness = 2,
                                DurationTicks = 4,
                                RemainingTicks = 4,
                                Stage = 1,
                                TargetScope = target,
                                Active = true
                            };
                            CountryDiseasesData.Upsert(disease);
                            systems.IntlPrestige = EconomyCalculator.Clamp(systems.IntlPrestige - 2f, 0f, 100f);
                            result = $"⚠ Подія завезення: додано хворобу ({FormatPublicDiseaseTargetUk(target)}), Престиж -2.";
                            break;
                        }

                    default:
                        await botClient.SendTextMessageAsync(userId, "❌ Невідомий тип події.", cancellationToken: cancellationToken);
                        return true;
                }

                CountryPublicSystemsData.Upsert(systems);
                CountriesData.UpdateCountry(country);
                await botClient.SendTextMessageAsync(userId, result, cancellationToken: cancellationToken);
                return true;
            }

            return false;
        }

        private static bool TryParseSetBudgetCommand(string input, out string budgetType, out decimal amount)
        {
            budgetType = string.Empty;
            amount = 0m;
            if (!TryParseCommandArgs(input, "set_budget", out var args) || args.Length < 2)
                return false;

            string rawType = (args[0] ?? string.Empty).Trim().ToLowerInvariant();
            budgetType = rawType switch
            {
                "health" => "health",
                "moz" => "health",
                "моз" => "health",
                "здоровя" => "health",
                "здоров'я" => "health",
                "education" => "education",
                "edu" => "education",
                "mon" => "education",
                "мон" => "education",
                "освіта" => "education",
                _ => string.Empty
            };

            if (string.IsNullOrWhiteSpace(budgetType))
                return false;

            return TryParseDecimal(args[1], out amount) && amount >= 0m;
        }

        private static bool TryParseSetBudgetIntlMode(string input, out bool autoMode)
        {
            autoMode = true;
            if (!TryParseCommandArgs(input, "set_budget_intl", out var args) || args.Length < 1)
                return false;

            string raw = (args[0] ?? string.Empty).Trim().ToLowerInvariant();
            if (raw is "auto" or "a" or "1" or "on" or "авто" or "автоматично" or "увімк" or "увімкнено")
            {
                autoMode = true;
                return true;
            }
            if (raw is "manual" or "m" or "0" or "off" or "вручну" or "ручний" or "вимк" or "вимкнено")
            {
                autoMode = false;
                return true;
            }
            return false;
        }

        private static bool TryParseCommandArgs(string input, string command, out string[] args)
        {
            args = Array.Empty<string>();
            if (string.IsNullOrWhiteSpace(input) || string.IsNullOrWhiteSpace(command))
                return false;

            string trimmed = input.Trim();
            string commandToken = command.Trim();

            string openPattern = commandToken + "(";
            if (trimmed.StartsWith(openPattern, StringComparison.OrdinalIgnoreCase) && trimmed.EndsWith(")", StringComparison.Ordinal))
            {
                string inner = trimmed.Substring(commandToken.Length + 1, trimmed.Length - commandToken.Length - 2).Trim();
                if (inner.Length == 0)
                {
                    args = Array.Empty<string>();
                    return true;
                }
                args = inner.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                return true;
            }

            if (trimmed.Equals(commandToken, StringComparison.OrdinalIgnoreCase))
            {
                args = Array.Empty<string>();
                return true;
            }

            string spacedPattern = commandToken + " ";
            if (trimmed.StartsWith(spacedPattern, StringComparison.OrdinalIgnoreCase))
            {
                string rest = trimmed.Substring(commandToken.Length).Trim();
                if (rest.Length == 0)
                {
                    args = Array.Empty<string>();
                    return true;
                }

                args = rest.Split(new[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                return true;
            }

            return false;
        }

        private static string TrimQuotes(string value)
        {
            string s = (value ?? string.Empty).Trim();
            if (s.Length >= 2)
            {
                if ((s.StartsWith("\"", StringComparison.Ordinal) && s.EndsWith("\"", StringComparison.Ordinal)) ||
                    (s.StartsWith("'", StringComparison.Ordinal) && s.EndsWith("'", StringComparison.Ordinal)))
                {
                    return s.Substring(1, s.Length - 2).Trim();
                }
            }
            return s;
        }

        private static CountryDiseasesData.CountryDiseaseRecord? FindDiseaseByNameOrId(int countryId, string key, bool activeOnly)
        {
            var list = CountryDiseasesData.GetByCountry(countryId, activeOnly);
            if (int.TryParse(key, NumberStyles.Integer, CultureInfo.InvariantCulture, out int id))
            {
                return list.FirstOrDefault(d => d.DiseaseId == id);
            }

            return list.FirstOrDefault(d => string.Equals(d.Name, key, StringComparison.OrdinalIgnoreCase));
        }
    }
}

