using DNDBot.DataBase;
using DNDBot.Goverment.Economy;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace DNDBot.Goverment
{
    public partial class StateParliamentHandle
    {
        private enum LawReviewEditField
        {
            Title,
            Description
        }

        private sealed class LawReviewEditState
        {
            public int LawId { get; set; }
            public LawReviewEditField Field { get; set; }
        }

        private static readonly Dictionary<long, LawReviewEditState> LawReviewEditStateByUser = new();

        private sealed class LawReviewEffectEditState
        {
            public int LawId { get; set; }
            public Dictionary<string, decimal?> SelectedEffects { get; } = new(StringComparer.OrdinalIgnoreCase);
        }

        private sealed class LawReviewEffectValueInputState
        {
            public int LawId { get; set; }
            public string EffectType { get; set; } = string.Empty;
            public long ChatId { get; set; }
            public int MsgId { get; set; }
        }

        private sealed class LawReviewEffectScopeInputState
        {
            public int LawId { get; set; }
            public string EffectType { get; set; } = string.Empty;
            public long ChatId { get; set; }
            public int MsgId { get; set; }
        }

        private sealed class LawReviewEffectOption
        {
            public string Token { get; set; } = string.Empty;
            public string EffectType { get; set; } = string.Empty;
            public string Label { get; set; } = string.Empty;
        }

        private static readonly Dictionary<long, LawReviewEffectEditState> LawReviewEffectEditStateByUser = new();
        private static readonly Dictionary<long, LawReviewEffectValueInputState> LawReviewEffectValueInputStateByUser = new();
        private static readonly Dictionary<long, LawReviewEffectScopeInputState> LawReviewEffectScopeInputStateByUser = new();
        private static readonly List<LawReviewEffectOption> LawReviewEffectOptions = new()
        {
            new LawReviewEffectOption { Token = "econ", EffectType = "Economy", Label = "💰 Економіка" },
            new LawReviewEffectOption { Token = "army", EffectType = "Army", Label = "🛡 Армія" },
            new LawReviewEffectOption { Token = "magic", EffectType = "Magic", Label = "✨ Магія" },
            new LawReviewEffectOption { Token = "dip", EffectType = "Diplomacy", Label = "🤝 Дипломатія" },
            new LawReviewEffectOption { Token = "law", EffectType = "Law", Label = "⚖ Право" },
            new LawReviewEffectOption { Token = "stab", EffectType = "Stability", Label = "📈 Стабільність" },
            new LawReviewEffectOption { Token = "tax", EffectType = GrossIncomeTaxEffectType, Label = "💸 Податок на брутто" },
            new LawReviewEffectOption { Token = "taxnet", EffectType = NetIncomeTaxEffectType, Label = "💳 Податок на нетто" },
            new LawReviewEffectOption { Token = "social", EffectType = SocialContributionEffectType, Label = "🏷 Соцвнесок" },
            new LawReviewEffectOption { Token = "ssal", EffectType = SetSoldierSalaryEffectType, Label = "🪖 ЗП військовим" },
            new LawReviewEffectOption { Token = "dsal", EffectType = SetDeputySalaryEffectType, Label = "🏛 ЗП депутатам" },
            new LawReviewEffectOption { Token = "csal", EffectType = SetCivilianSalaryEffectType, Label = "👥 ЗП людям" },
            new LawReviewEffectOption { Token = "pens", EffectType = SetPensionPaymentEffectType, Label = "👴 Пенсія / особа" },
            new LawReviewEffectOption { Token = "socp", EffectType = SetSocialSupportPaymentEffectType, Label = "♿ Соцвиплата / особа" },
            new LawReviewEffectOption { Token = "addt", EffectType = AddTreasuryEffectType, Label = "💰 Додати в казну" },
            new LawReviewEffectOption { Token = "remt", EffectType = RemoveTreasuryEffectType, Label = "💸 Списати з казни" },
            new LawReviewEffectOption { Token = "addtd", EffectType = AddTreasuryDailyEffectType, Label = "💰 Додати в казну / день" },
            new LawReviewEffectOption { Token = "remtd", EffectType = RemoveTreasuryDailyEffectType, Label = "💸 Списати з казни / день" },
            new LawReviewEffectOption { Token = "limimp", EffectType = LimitImportEffectType, Label = "📥 Ліміт імпорту (%)" },
            new LawReviewEffectOption { Token = "limexp", EffectType = LimitExportEffectType, Label = "📤 Ліміт експорту (%)" },
            new LawReviewEffectOption { Token = "limcmp", EffectType = LimitCompanyResourceUseEffectType, Label = "🏭 Ліміт споживання компаній (%)" },
            new LawReviewEffectOption { Token = "law_hm", EffectType = PublicSystemsEngine.LawHealthMinistry, Label = "🏥 Закон: МОЗ" },
            new LawReviewEffectOption { Token = "law_san", EffectType = PublicSystemsEngine.LawPublicSanitation, Label = "🧼 Закон: санітарія" },
            new LawReviewEffectOption { Token = "law_qr", EffectType = PublicSystemsEngine.LawQuarantineProtocols, Label = "🚧 Закон: карантин" },
            new LawReviewEffectOption { Token = "law_vac", EffectType = PublicSystemsEngine.LawNationalImmunization, Label = "💉 Закон: імунізація" },
            new LawReviewEffectOption { Token = "law_em", EffectType = PublicSystemsEngine.LawEduMinistry, Label = "🎓 Закон: МОН" },
            new LawReviewEffectOption { Token = "law_bs", EffectType = PublicSystemsEngine.LawBasicSchooling, Label = "🏫 Закон: базова освіта" },
            new LawReviewEffectOption { Token = "law_he", EffectType = PublicSystemsEngine.LawHigherEduCharter, Label = "🏛 Закон: вища освіта" },
            new LawReviewEffectOption { Token = "law_sc", EffectType = PublicSystemsEngine.LawScholarshipProgram, Label = "🎒 Закон: стипендії" },
            new LawReviewEffectOption { Token = "law_intl", EffectType = PublicSystemsEngine.LawIntlCooperation, Label = "🌍 Закон: міжнародна співпраця" },
            new LawReviewEffectOption { Token = "law_data", EffectType = PublicSystemsEngine.LawIntlDataSharingHealth, Label = "🧾 Закон: міжнародні епіддані" },
            new LawReviewEffectOption { Token = "law_acad", EffectType = PublicSystemsEngine.LawIntlAcademicExchange, Label = "🎓 Закон: міжнародний академобмін" },
            new LawReviewEffectOption { Token = "law_out", EffectType = PublicSystemsEngine.LawCommunityHealthOutreach, Label = "🫶 Закон: громадське здоров'я" },
            new LawReviewEffectOption { Token = "law_tc", EffectType = PublicSystemsEngine.LawTeacherCertification, Label = "👩‍🏫 Закон: сертифікація педагогів" },
            new LawReviewEffectOption { Token = "none", EffectType = NoEffectType, Label = "⏭ Без ефекту" }
        };

        private async Task ShowLawReviewList(ITelegramBotClient botClient, long userId, long chatId, int msgId, int page, CancellationToken cancellationToken)
        {
            if (!TryGetCountryForDeputy(userId, out int countryId, out string error))
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    error,
                    StateKeyboardBuilder.ParliamentStateKeyboard(),
                    cancellationToken
                );
                return;
            }

            var laws = LawsData.GetByCountry(countryId)
                .Where(l => IsLawOnReview(l.Status))
                .OrderByDescending(l => GetLawReviewStage(l.Status))
                .ThenBy(l => l.LawId)
                .ToList();

            if (laws.Count == 0)
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "📖 Розглядання законів\nНемає законів на читанні.",
                    StateKeyboardBuilder.ParliamentStateKeyboard(),
                    cancellationToken
                );
                return;
            }

            int stage1 = laws.Count(l => GetLawReviewStage(l.Status) == 1);
            int stage2 = laws.Count(l => GetLawReviewStage(l.Status) == 2);
            string text =
                "📖 Розглядання законів\n" +
                $"1 читання: {stage1}\n" +
                $"2 читання: {stage2}\n" +
                $"Усього: {laws.Count}";

            await SafeEditMessageTextAsync(
                botClient,
                chatId,
                msgId,
                text,
                StateKeyboardBuilder.LawReviewListKeyboard(laws, page),
                cancellationToken
            );
        }

        private async Task ShowLawReviewInfo(ITelegramBotClient botClient, long userId, long chatId, int msgId, int lawId, CancellationToken cancellationToken)
        {
            LawReviewEffectEditStateByUser.Remove(userId);
            LawReviewEffectValueInputStateByUser.Remove(userId);
            LawReviewEffectScopeInputStateByUser.Remove(userId);

            if (!TryGetCountryForDeputy(userId, out int countryId, out string error))
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    error,
                    StateKeyboardBuilder.ParliamentStateKeyboard(),
                    cancellationToken
                );
                return;
            }

            var law = LawsData.GetLawById(lawId);
            if (law == null || law.CountryId != countryId)
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "Закон не знайдено.",
                    StateKeyboardBuilder.ParliamentStateKeyboard(),
                    cancellationToken
                );
                return;
            }

            int stage = GetLawReviewStage(law.Status);
            if (stage == 0)
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "Цей закон вже не на розгляданні.",
                    StateKeyboardBuilder.ParliamentStateKeyboard(),
                    cancellationToken
                );
                return;
            }

            string text =
                $"📖 Розглядання закону #{law.LawId}\n" +
                $"Назва: {law.Title}\n" +
                $"Статус: {GetLawStatusDisplayName(law.Status)}\n" +
                $"Опис: {BuildLawDescriptionText(law)}\n" +
                $"Ефект: {BuildLawEffectText(law)}\n" +
                "\nПід час розглядання можна вносити правки в назву, опис і ефекти.";

            await SafeEditMessageTextAsync(
                botClient,
                chatId,
                msgId,
                text,
                StateKeyboardBuilder.LawReviewInfoKeyboard(law.LawId, stage),
                cancellationToken
            );
        }

        private async Task AdvanceLawReviewStage(ITelegramBotClient botClient, long userId, long chatId, int msgId, int lawId, CancellationToken cancellationToken)
        {
            LawReviewEffectEditStateByUser.Remove(userId);
            LawReviewEffectValueInputStateByUser.Remove(userId);
            LawReviewEffectScopeInputStateByUser.Remove(userId);

            if (!TryGetCountryForDeputy(userId, out int countryId, out string error))
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    error,
                    StateKeyboardBuilder.ParliamentStateKeyboard(),
                    cancellationToken
                );
                return;
            }

            var law = LawsData.GetLawById(lawId);
            if (law == null || law.CountryId != countryId)
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "Закон не знайдено.",
                    StateKeyboardBuilder.ParliamentStateKeyboard(),
                    cancellationToken
                );
                return;
            }

            int stage = GetLawReviewStage(law.Status);
            if (stage == 0)
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "Цей закон не знаходиться на читанні.",
                    StateKeyboardBuilder.ParliamentStateKeyboard(),
                    cancellationToken
                );
                return;
            }

            if (stage == 1)
            {
                law.Status = LawStatusReviewSecond;
                LawsData.UpdateLaw(law);
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    $"Закон #{law.LawId} переведено на 2 читання.\nТепер можна готувати до голосування.",
                    StateKeyboardBuilder.LawReviewInfoKeyboard(law.LawId, stage: 2),
                    cancellationToken
                );
                return;
            }

            law.Status = LawStatusProposed;
            LawsData.UpdateLaw(law);
            await SafeEditMessageTextAsync(
                botClient,
                chatId,
                msgId,
                $"Закон #{law.LawId} завершив 2 читання і готовий до голосування.",
                StateKeyboardBuilder.ReviewToVotingKeyboard(law.LawId),
                cancellationToken
            );
        }

        private async Task RejectLawDuringReview(ITelegramBotClient botClient, long userId, long chatId, int msgId, int lawId, CancellationToken cancellationToken)
        {
            LawReviewEffectEditStateByUser.Remove(userId);
            LawReviewEffectValueInputStateByUser.Remove(userId);
            LawReviewEffectScopeInputStateByUser.Remove(userId);

            if (!TryGetCountryForDeputy(userId, out int countryId, out string error))
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    error,
                    StateKeyboardBuilder.ParliamentStateKeyboard(),
                    cancellationToken
                );
                return;
            }

            var law = LawsData.GetLawById(lawId);
            if (law == null || law.CountryId != countryId)
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "Закон не знайдено.",
                    StateKeyboardBuilder.ParliamentStateKeyboard(),
                    cancellationToken
                );
                return;
            }

            if (!IsLawOnReview(law.Status))
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "Відхилити можна тільки закон на етапі розглядання.",
                    StateKeyboardBuilder.ParliamentStateKeyboard(),
                    cancellationToken
                );
                return;
            }

            law.Status = LawStatusRejected;
            LawsData.UpdateLaw(law);

            await SafeEditMessageTextAsync(
                botClient,
                chatId,
                msgId,
                $"Закон #{law.LawId} відхилено на етапі розглядання.",
                StateKeyboardBuilder.ParliamentStateKeyboard(),
                cancellationToken
            );
        }

        private async Task BeginLawReviewEdit(
            ITelegramBotClient botClient,
            long userId,
            long chatId,
            int msgId,
            int lawId,
            LawReviewEditField field,
            CancellationToken cancellationToken)
        {
            if (!TryGetCountryForDeputy(userId, out int countryId, out string error))
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    error,
                    StateKeyboardBuilder.ParliamentStateKeyboard(),
                    cancellationToken
                );
                return;
            }

            var law = LawsData.GetLawById(lawId);
            if (law == null || law.CountryId != countryId)
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "Закон не знайдено.",
                    StateKeyboardBuilder.ParliamentStateKeyboard(),
                    cancellationToken
                );
                return;
            }

            int stage = GetLawReviewStage(law.Status);
            if (stage == 0)
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "Редагувати можна лише закон, що на розгляданні.",
                    StateKeyboardBuilder.ParliamentStateKeyboard(),
                    cancellationToken
                );
                return;
            }

            LawReviewEditStateByUser[userId] = new LawReviewEditState
            {
                LawId = lawId,
                Field = field
            };
            LawReviewEffectEditStateByUser.Remove(userId);
            LawReviewEffectValueInputStateByUser.Remove(userId);
            LawReviewEffectScopeInputStateByUser.Remove(userId);

            string prompt = field == LawReviewEditField.Title
                ? $"Введіть нову назву для закону #{lawId}:"
                : $"Введіть новий опис для закону #{lawId} (або '-' щоб очистити):";
            await botClient.SendTextMessageAsync(userId, prompt, cancellationToken: cancellationToken);
            await SafeEditMessageTextAsync(
                botClient,
                chatId,
                msgId,
                $"Очікується нове значення: {(field == LawReviewEditField.Title ? "назва" : "опис")} для закону #{lawId}.",
                StateKeyboardBuilder.LawReviewInfoKeyboard(lawId, stage),
                cancellationToken
            );
        }

        private async Task BeginLawReviewEffectEdit(
            ITelegramBotClient botClient,
            long userId,
            long chatId,
            int msgId,
            int lawId,
            CancellationToken cancellationToken)
        {
            if (!TryGetCountryForDeputy(userId, out int countryId, out string error))
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    error,
                    StateKeyboardBuilder.ParliamentStateKeyboard(),
                    cancellationToken
                );
                return;
            }

            var law = LawsData.GetLawById(lawId);
            if (law == null || law.CountryId != countryId)
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "Закон не знайдено.",
                    StateKeyboardBuilder.ParliamentStateKeyboard(),
                    cancellationToken
                );
                return;
            }

            int stage = GetLawReviewStage(law.Status);
            if (stage == 0)
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "Редагувати можна лише закон, що на розгляданні.",
                    StateKeyboardBuilder.ParliamentStateKeyboard(),
                    cancellationToken
                );
                return;
            }

            LawReviewEditStateByUser.Remove(userId);
            LawReviewEffectValueInputStateByUser.Remove(userId);
            LawReviewEffectScopeInputStateByUser.Remove(userId);
            var state = new LawReviewEffectEditState { LawId = lawId };
            foreach (var entry in ParseLawEffectEntries(law.EffectType))
                state.SelectedEffects[entry.EffectType] = entry.EffectValue;
            if (state.SelectedEffects.Count == 0)
                state.SelectedEffects[NoEffectType] = null;
            if (state.SelectedEffects.Count > 1)
                state.SelectedEffects.Remove(NoEffectType);

            LawReviewEffectEditStateByUser[userId] = state;
            await ShowLawReviewEffectEdit(botClient, userId, chatId, msgId, law, cancellationToken);
        }

        private async Task ToggleLawReviewEffect(
            ITelegramBotClient botClient,
            long userId,
            long chatId,
            int msgId,
            int lawId,
            string token,
            CancellationToken cancellationToken)
        {
            if (!TryGetCountryForDeputy(userId, out int countryId, out _))
                return;

            var law = LawsData.GetLawById(lawId);
            if (law == null || law.CountryId != countryId)
                return;

            int stage = GetLawReviewStage(law.Status);
            if (stage == 0)
                return;

            if (!LawReviewEffectEditStateByUser.TryGetValue(userId, out var state) || state.LawId != lawId)
            {
                await BeginLawReviewEffectEdit(botClient, userId, chatId, msgId, lawId, cancellationToken);
                return;
            }

            if (!TryMapLawReviewEffectToken(token, out string effectType))
                return;

            if (string.Equals(effectType, NoEffectType, StringComparison.OrdinalIgnoreCase))
            {
                state.SelectedEffects.Clear();
                state.SelectedEffects[NoEffectType] = null;
            }
            else
            {
                state.SelectedEffects.Remove(NoEffectType);
                var sameBaseKeys = state.SelectedEffects.Keys
                    .Where(key => string.Equals(GetLawEffectBaseType(key), effectType, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                if (sameBaseKeys.Count > 0)
                {
                    foreach (var key in sameBaseKeys)
                        state.SelectedEffects.Remove(key);
                }
                else
                {
                    state.SelectedEffects[effectType] = CanLawEffectUseValue(effectType)
                        ? ResolveDefaultLawEffectValueForSelection(effectType, law.EffectValue)
                        : null;
                }

                if (state.SelectedEffects.Count == 0)
                    state.SelectedEffects[NoEffectType] = null;
            }

            LawReviewEffectValueInputStateByUser.Remove(userId);
            LawReviewEffectScopeInputStateByUser.Remove(userId);
            await ShowLawReviewEffectEdit(botClient, userId, chatId, msgId, law, cancellationToken);
        }

        private async Task ClearLawReviewEffectSelection(
            ITelegramBotClient botClient,
            long userId,
            long chatId,
            int msgId,
            int lawId,
            CancellationToken cancellationToken)
        {
            if (!LawReviewEffectEditStateByUser.TryGetValue(userId, out var state) || state.LawId != lawId)
            {
                await BeginLawReviewEffectEdit(botClient, userId, chatId, msgId, lawId, cancellationToken);
                return;
            }

            state.SelectedEffects.Clear();
            state.SelectedEffects[NoEffectType] = null;
            LawReviewEffectValueInputStateByUser.Remove(userId);
            LawReviewEffectScopeInputStateByUser.Remove(userId);

            if (!TryGetCountryForDeputy(userId, out int countryId, out _))
                return;

            var law = LawsData.GetLawById(lawId);
            if (law == null || law.CountryId != countryId)
                return;

            int stage = GetLawReviewStage(law.Status);
            if (stage == 0)
                return;

            await ShowLawReviewEffectEdit(botClient, userId, chatId, msgId, law, cancellationToken);
        }

        private async Task ApplyLawReviewEffectEdit(
            ITelegramBotClient botClient,
            long userId,
            long chatId,
            int msgId,
            int lawId,
            CancellationToken cancellationToken)
        {
            if (!LawReviewEffectEditStateByUser.TryGetValue(userId, out var state) || state.LawId != lawId)
            {
                await ShowLawReviewInfo(botClient, userId, chatId, msgId, lawId, cancellationToken);
                return;
            }

            if (!TryGetCountryForDeputy(userId, out int countryId, out _))
            {
                LawReviewEffectEditStateByUser.Remove(userId);
                return;
            }

            var law = LawsData.GetLawById(lawId);
            if (law == null || law.CountryId != countryId)
            {
                LawReviewEffectEditStateByUser.Remove(userId);
                return;
            }

            if (!IsLawOnReview(law.Status))
            {
                LawReviewEffectEditStateByUser.Remove(userId);
                LawReviewEffectValueInputStateByUser.Remove(userId);
            LawReviewEffectScopeInputStateByUser.Remove(userId);
                return;
            }

            var normalizedEntries = NormalizeLawEffectEntries(
                state.SelectedEffects.Select(kvp => new LawEffectEntry
                {
                    EffectType = kvp.Key,
                    EffectValue = kvp.Value
                })
            );

            law.EffectType = SerializeLawEffectEntries(normalizedEntries);
            if (normalizedEntries.Count == 1 && string.Equals(normalizedEntries[0].EffectType, NoEffectType, StringComparison.OrdinalIgnoreCase))
            {
                law.EffectValue = 0m;
            }
            else
            {
                decimal? firstSpecificValue = normalizedEntries
                    .Where(e => e.EffectValue.HasValue)
                    .Select(e => e.EffectValue)
                    .FirstOrDefault();
                if (firstSpecificValue.HasValue)
                    law.EffectValue = firstSpecificValue.Value;
            }

            LawsData.UpdateLaw(law);
            LawReviewEffectEditStateByUser.Remove(userId);
            LawReviewEffectValueInputStateByUser.Remove(userId);
            LawReviewEffectScopeInputStateByUser.Remove(userId);
            await ShowLawReviewInfo(botClient, userId, chatId, msgId, lawId, cancellationToken);
        }

        private async Task CancelLawReviewEffectEdit(
            ITelegramBotClient botClient,
            long userId,
            long chatId,
            int msgId,
            int lawId,
            CancellationToken cancellationToken)
        {
            LawReviewEffectEditStateByUser.Remove(userId);
            LawReviewEffectValueInputStateByUser.Remove(userId);
            LawReviewEffectScopeInputStateByUser.Remove(userId);
            await ShowLawReviewInfo(botClient, userId, chatId, msgId, lawId, cancellationToken);
        }

        private async Task ShowLawReviewEffectEdit(
            ITelegramBotClient botClient,
            long userId,
            long chatId,
            int msgId,
            LawRecord law,
            CancellationToken cancellationToken)
        {
            if (!LawReviewEffectEditStateByUser.TryGetValue(userId, out var state) || state.LawId != law.LawId)
                return;

            var selectedEntries = NormalizeLawEffectEntries(
                state.SelectedEffects.Select(kvp => new LawEffectEntry
                {
                    EffectType = kvp.Key,
                    EffectValue = kvp.Value
                })
            );
            var selectedPreview = new LawRecord
            {
                CountryId = law.CountryId,
                Description = law.Description,
                EffectValue = law.EffectValue,
                EffectType = SerializeLawEffectEntries(selectedEntries)
            };

            var items = new List<(string text, string data)>();
            foreach (var option in LawReviewEffectOptions)
            {
                bool isSelected = selectedEntries.Any(e =>
                    string.Equals(GetLawEffectBaseType(e.EffectType), option.EffectType, StringComparison.OrdinalIgnoreCase));
                string mark = isSelected ? "✅" : "⬜";
                items.Add(($"{mark} {option.Label}", $"stateparliament_review_effect_toggle_{law.LawId}_{option.Token}"));
            }

            var valueItems = new List<(string text, string data)>();
            var scopeItems = new List<(string text, string data)>();
            foreach (var selected in selectedEntries)
            {
                if (!CanLawEffectUseValue(selected.EffectType))
                    continue;
                if (!TryMapLawReviewEffectTypeToToken(selected.EffectType, out string token))
                    continue;
                if (!TryMapLawReviewEffectToken(token, out string mappedType))
                    continue;

                var label = LawReviewEffectOptions.FirstOrDefault(o =>
                    string.Equals(o.EffectType, mappedType, StringComparison.OrdinalIgnoreCase))?.Label ?? mappedType;
                decimal shownValue = selected.EffectValue ?? law.EffectValue;
                valueItems.Add(($"✏ {label}: {shownValue.ToString("0.####", CultureInfo.InvariantCulture)}", $"stateparliament_review_effect_value_{law.LawId}_{token}"));

                if (LawEffectSupportsResourceScope(mappedType))
                {
                    string scopeLabel = BuildLawEffectScopeLabel(selected.EffectType);
                    scopeItems.Add(($"📦 {label}: {scopeLabel}", $"stateparliament_review_effect_scope_{law.LawId}_{token}"));
                }
            }

            string text =
                $"🎯 Редагування ефектів закону #{law.LawId}\n" +
                $"Поточні ефекти: {BuildLawEffectText(law)}\n" +
                $"Обрані ефекти: {BuildLawEffectText(selectedPreview)}\n" +
                "Можна обрати декілька ефектів.\n" +
                "Для кожного ефекту натисніть кнопку ✏ і введіть окреме значення.\n" +
                "Для лімітів і податків вводьте відсоток (наприклад 70).\n" +
                "Для зарплат/пенсій/соцвиплат вводьте суму на 1 особу.\n" +
                "Для лімітів імпорту/експорту кнопкою 📦 можна задати один або кілька ресурсів.";

            await SafeEditMessageTextAsync(
                botClient,
                chatId,
                msgId,
                text,
                StateKeyboardBuilder.LawReviewEffectEditKeyboard(items, valueItems, scopeItems, law.LawId),
                cancellationToken
            );
        }

        private static bool TryMapLawReviewEffectToken(string token, out string effectType)
        {
            effectType = string.Empty;
            if (string.IsNullOrWhiteSpace(token))
                return false;

            var match = LawReviewEffectOptions.FirstOrDefault(o =>
                string.Equals(o.Token, token.Trim(), StringComparison.OrdinalIgnoreCase));
            if (match == null)
                return false;

            effectType = match.EffectType;
            return true;
        }

        private static bool TryMapLawReviewEffectTypeToToken(string effectType, out string token)
        {
            token = string.Empty;
            if (string.IsNullOrWhiteSpace(effectType))
                return false;

            string normalized = GetLawEffectBaseType(effectType);
            var match = LawReviewEffectOptions.FirstOrDefault(o =>
                string.Equals(o.EffectType, normalized, StringComparison.OrdinalIgnoreCase));
            if (match == null)
                return false;

            token = match.Token;
            return true;
        }

        private static bool TryParseLawReviewEffectToggleData(string data, out int lawId, out string token)
        {
            lawId = 0;
            token = string.Empty;

            const string prefix = "stateparliament_review_effect_toggle_";
            if (string.IsNullOrWhiteSpace(data) || !data.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                return false;

            string rest = data.Substring(prefix.Length);
            int splitIndex = rest.IndexOf('_');
            if (splitIndex <= 0)
                return false;

            if (!int.TryParse(rest.Substring(0, splitIndex), out lawId))
                return false;

            token = rest.Substring(splitIndex + 1).Trim();
            return !string.IsNullOrWhiteSpace(token);
        }

        private static bool TryParseLawReviewEffectValueData(string data, out int lawId, out string token)
        {
            lawId = 0;
            token = string.Empty;

            const string prefix = "stateparliament_review_effect_value_";
            if (string.IsNullOrWhiteSpace(data) || !data.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                return false;

            string rest = data.Substring(prefix.Length);
            int splitIndex = rest.IndexOf('_');
            if (splitIndex <= 0)
                return false;

            if (!int.TryParse(rest.Substring(0, splitIndex), out lawId))
                return false;

            token = rest.Substring(splitIndex + 1).Trim();
            return !string.IsNullOrWhiteSpace(token);
        }

        private static bool TryParseLawReviewEffectScopeData(string data, out int lawId, out string token)
        {
            lawId = 0;
            token = string.Empty;

            const string prefix = "stateparliament_review_effect_scope_";
            if (string.IsNullOrWhiteSpace(data) || !data.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                return false;

            string rest = data.Substring(prefix.Length);
            int splitIndex = rest.IndexOf('_');
            if (splitIndex <= 0)
                return false;

            if (!int.TryParse(rest.Substring(0, splitIndex), out lawId))
                return false;

            token = rest.Substring(splitIndex + 1).Trim();
            return !string.IsNullOrWhiteSpace(token);
        }

        private async Task BeginLawReviewEffectScopeInput(
            ITelegramBotClient botClient,
            long userId,
            long chatId,
            int msgId,
            int lawId,
            string token,
            CancellationToken cancellationToken)
        {
            if (!LawReviewEffectEditStateByUser.TryGetValue(userId, out var state) || state.LawId != lawId)
            {
                await BeginLawReviewEffectEdit(botClient, userId, chatId, msgId, lawId, cancellationToken);
                return;
            }

            if (!TryMapLawReviewEffectToken(token, out string effectType))
                return;
            if (!LawEffectSupportsResourceScope(effectType))
                return;

            var selectedKeys = state.SelectedEffects.Keys
                .Where(key => string.Equals(GetLawEffectBaseType(key), effectType, StringComparison.OrdinalIgnoreCase))
                .ToList();
            if (selectedKeys.Count == 0)
                return;

            LawReviewEffectScopeInputStateByUser[userId] = new LawReviewEffectScopeInputState
            {
                LawId = lawId,
                EffectType = effectType,
                ChatId = chatId,
                MsgId = msgId
            };
            LawReviewEffectValueInputStateByUser.Remove(userId);

            string label = LawReviewEffectOptions.FirstOrDefault(o =>
                string.Equals(o.EffectType, effectType, StringComparison.OrdinalIgnoreCase))?.Label ?? effectType;
            string currentScope = BuildLawEffectScopeLabel(selectedKeys[0]);
            var availableResources = EconomyResourcesData.GetAll()
                .OrderBy(r => r.ResourceType, StringComparer.OrdinalIgnoreCase)
                .Select(r => r.ResourceType)
                .Take(20)
                .ToList();
            string resourcesHint = availableResources.Count > 0
                ? string.Join(", ", availableResources)
                : "довідник ресурсів порожній";

            await botClient.SendTextMessageAsync(
                userId,
                $"Введіть ресурси для «{label}» через кому.\nПоточне охоплення: {currentScope}\nПриклад: Food,Tools\nДля всіх ресурсів введіть `*`.\nДоступно: {resourcesHint}",
                cancellationToken: cancellationToken);
        }

        private async Task BeginLawReviewEffectValueInput(
            ITelegramBotClient botClient,
            long userId,
            long chatId,
            int msgId,
            int lawId,
            string token,
            CancellationToken cancellationToken)
        {
            if (!LawReviewEffectEditStateByUser.TryGetValue(userId, out var state) || state.LawId != lawId)
            {
                await BeginLawReviewEffectEdit(botClient, userId, chatId, msgId, lawId, cancellationToken);
                return;
            }

            if (!TryMapLawReviewEffectToken(token, out string effectType))
                return;

            var selectedKeys = state.SelectedEffects.Keys
                .Where(key => string.Equals(GetLawEffectBaseType(key), effectType, StringComparison.OrdinalIgnoreCase))
                .ToList();
            if (selectedKeys.Count == 0 || !CanLawEffectUseValue(effectType))
                return;

            LawReviewEffectValueInputStateByUser[userId] = new LawReviewEffectValueInputState
            {
                LawId = lawId,
                EffectType = effectType,
                ChatId = chatId,
                MsgId = msgId
            };
            LawReviewEffectScopeInputStateByUser.Remove(userId);

            string label = LawReviewEffectOptions.FirstOrDefault(o =>
                string.Equals(o.EffectType, effectType, StringComparison.OrdinalIgnoreCase))?.Label ?? effectType;

            await botClient.SendTextMessageAsync(
                userId,
                $"Введіть значення для «{label}» (десяткове число).",
                cancellationToken: cancellationToken);
        }

        private async Task HandleLawReviewEffectValueInput(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
        {
            if (message?.From == null)
                return;

            long userId = message.From.Id;
            if (!LawReviewEffectValueInputStateByUser.TryGetValue(userId, out var inputState))
                return;

            string input = (message.Text ?? string.Empty).Trim();
            if (string.Equals(input, "/cancel", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(input, "скасувати", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(input, "cancel", StringComparison.OrdinalIgnoreCase))
            {
                LawReviewEffectValueInputStateByUser.Remove(userId);
                LawReviewEffectScopeInputStateByUser.Remove(userId);
                await botClient.SendTextMessageAsync(userId, "Ввід значення скасовано.", cancellationToken: cancellationToken);
                return;
            }

            if (!TryParseDecimal(input, out decimal value))
            {
                await botClient.SendTextMessageAsync(userId, "Невірне число. Введіть десяткове значення:", cancellationToken: cancellationToken);
                return;
            }

            if (!LawReviewEffectEditStateByUser.TryGetValue(userId, out var editState) || editState.LawId != inputState.LawId)
            {
                LawReviewEffectValueInputStateByUser.Remove(userId);
                LawReviewEffectScopeInputStateByUser.Remove(userId);
                await botClient.SendTextMessageAsync(userId, "Сесію редагування завершено.", cancellationToken: cancellationToken);
                return;
            }

            var selectedKeys = editState.SelectedEffects.Keys
                .Where(key => string.Equals(GetLawEffectBaseType(key), inputState.EffectType, StringComparison.OrdinalIgnoreCase))
                .ToList();
            if (selectedKeys.Count == 0)
            {
                editState.SelectedEffects[inputState.EffectType] = value;
            }
            else
            {
                foreach (var key in selectedKeys)
                    editState.SelectedEffects[key] = value;
            }
            LawReviewEffectValueInputStateByUser.Remove(userId);
            LawReviewEffectScopeInputStateByUser.Remove(userId);

            if (!TryGetCountryForDeputy(userId, out int countryId, out _))
                return;

            var law = LawsData.GetLawById(inputState.LawId);
            if (law == null || law.CountryId != countryId || !IsLawOnReview(law.Status))
                return;

            await ShowLawReviewEffectEdit(
                botClient,
                userId,
                inputState.ChatId,
                inputState.MsgId,
                law,
                cancellationToken);
        }

        private async Task HandleLawReviewEffectScopeInput(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
        {
            if (message?.From == null)
                return;

            long userId = message.From.Id;
            if (!LawReviewEffectScopeInputStateByUser.TryGetValue(userId, out var inputState))
                return;

            string input = (message.Text ?? string.Empty).Trim();
            if (string.Equals(input, "/cancel", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(input, "скасувати", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(input, "cancel", StringComparison.OrdinalIgnoreCase))
            {
                LawReviewEffectScopeInputStateByUser.Remove(userId);
                await botClient.SendTextMessageAsync(userId, "Ввід ресурсів скасовано.", cancellationToken: cancellationToken);
                return;
            }

            if (!TryResolveLawEffectScopeFromInput(input, out var resources, out string error))
            {
                await botClient.SendTextMessageAsync(userId, $"❌ {error}\nСпробуйте ще раз або введіть `*`.", cancellationToken: cancellationToken);
                return;
            }

            if (!LawReviewEffectEditStateByUser.TryGetValue(userId, out var editState) || editState.LawId != inputState.LawId)
            {
                LawReviewEffectScopeInputStateByUser.Remove(userId);
                await botClient.SendTextMessageAsync(userId, "Сесію редагування завершено.", cancellationToken: cancellationToken);
                return;
            }

            var selectedKeys = editState.SelectedEffects.Keys
                .Where(key => string.Equals(GetLawEffectBaseType(key), inputState.EffectType, StringComparison.OrdinalIgnoreCase))
                .ToList();
            decimal? currentValue = selectedKeys
                .Select(key => editState.SelectedEffects.TryGetValue(key, out var value) ? value : null)
                .FirstOrDefault();
            foreach (var key in selectedKeys)
                editState.SelectedEffects.Remove(key);

            editState.SelectedEffects.Remove(NoEffectType);
            string scopedEffectType = BuildScopedLawEffectType(inputState.EffectType, resources);
            editState.SelectedEffects[scopedEffectType] = currentValue;
            LawReviewEffectScopeInputStateByUser.Remove(userId);

            if (!TryGetCountryForDeputy(userId, out int countryId, out _))
                return;

            var law = LawsData.GetLawById(inputState.LawId);
            if (law == null || law.CountryId != countryId || !IsLawOnReview(law.Status))
                return;

            await ShowLawReviewEffectEdit(
                botClient,
                userId,
                inputState.ChatId,
                inputState.MsgId,
                law,
                cancellationToken);
        }

        private async Task HandleLawReviewEditInput(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
        {
            if (message?.From == null)
                return;

            long userId = message.From.Id;
            if (!LawReviewEditStateByUser.TryGetValue(userId, out var editState))
                return;

            if (!TryGetCountryForDeputy(userId, out int countryId, out string error))
            {
                LawReviewEditStateByUser.Remove(userId);
                await botClient.SendTextMessageAsync(userId, error, cancellationToken: cancellationToken);
                return;
            }

            var law = LawsData.GetLawById(editState.LawId);
            if (law == null || law.CountryId != countryId)
            {
                LawReviewEditStateByUser.Remove(userId);
                await botClient.SendTextMessageAsync(userId, "Закон не знайдено.", cancellationToken: cancellationToken);
                return;
            }

            if (!IsLawOnReview(law.Status))
            {
                LawReviewEditStateByUser.Remove(userId);
                await botClient.SendTextMessageAsync(userId, "Закон вже не на розгляданні, редагування скасовано.", cancellationToken: cancellationToken);
                return;
            }

            string input = (message.Text ?? string.Empty).Trim();
            if (string.Equals(input, "/cancel", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(input, "скасувати", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(input, "cancel", StringComparison.OrdinalIgnoreCase))
            {
                LawReviewEditStateByUser.Remove(userId);
                await botClient.SendTextMessageAsync(userId, "Редагування закону скасовано.", cancellationToken: cancellationToken);
                return;
            }

            if (editState.Field == LawReviewEditField.Title)
            {
                if (string.IsNullOrWhiteSpace(input))
                {
                    await botClient.SendTextMessageAsync(userId, "Назва не може бути порожньою. Спробуйте ще раз:", cancellationToken: cancellationToken);
                    return;
                }

                law.Title = input;
            }
            else
            {
                if (string.Equals(input, "-", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(input, "очистити", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(input, "clear", StringComparison.OrdinalIgnoreCase))
                {
                    law.Description = null;
                }
                else
                {
                    law.Description = string.IsNullOrWhiteSpace(input) ? null : input;
                }
            }

            LawsData.UpdateLaw(law);
            LawReviewEditStateByUser.Remove(userId);
            await botClient.SendTextMessageAsync(
                userId,
                $"Закон #{law.LawId} оновлено.\nСтатус: {GetLawStatusDisplayName(law.Status)}",
                cancellationToken: cancellationToken
            );
        }
    }
}


