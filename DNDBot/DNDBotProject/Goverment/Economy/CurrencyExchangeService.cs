using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using DNDBot.DataBase;
using DNDBot.Goverment;

namespace DNDBot.Goverment.Economy
{
    public enum CurrencyKind
    {
        Gold = 0,
        Country = 1
    }

    public sealed class CurrencyExchangeService
    {
        public static bool IsEnabled => EconomyConfigData.GetInt("CurrencyEnabled", 0) == 1;
        private static readonly NumberFormatInfo SpaceNumberFormat = new NumberFormatInfo
        {
            NumberGroupSeparator = " ",
            NumberDecimalSeparator = "."
        };

        private static string FmtNumber(decimal value, int decimals = 2)
        {
            return value.ToString($"N{decimals}", SpaceNumberFormat);
        }

        private static string FmtNumber(long value)
        {
            return value.ToString("N0", SpaceNumberFormat);
        }

        public sealed class CurrencyRateResult
        {
            public EconomyCurrencyData.EconomyCurrencyRecord Currency { get; set; } = new();
            public decimal OfficialRateToGold { get; set; }
            public decimal BlackMarketRateToGold { get; set; }
            public float InflationFactor { get; set; }
            public float SupplyIndex { get; set; }
            public float DemandIndex { get; set; }
            public float InterventionStrength { get; set; }
            public float ExternalShock { get; set; }
            public float ScarcityIndex { get; set; }
        }

        public EconomyCurrencyData.EconomyCurrencyRecord EnsureCurrencyForCountry(CountryRecord country)
        {
            var existing = EconomyCurrencyData.GetByCountry(country.CountryId);
            if (existing != null)
                return existing;

            string code = BuildCurrencyCode(country);
            string name = $"{country.Name} Coin";
            var record = new EconomyCurrencyData.EconomyCurrencyRecord
            {
                CountryId = country.CountryId,
                CurrencyCode = code,
                CurrencyName = name,
                BaseRate = 1m,
                CurrentRate = 1m,
                InflationFactor = 0f,
                SupplyIndex = 1f,
                DemandIndex = 1f,
                CentralBankReserve = 0m,
                InterventionStrength = 0f,
                ExternalShock = 0f,
                ScarcityIndex = 0f,
                BlackMarketMultiplier = 1.1f,
                BlackMarketRate = 1m,
                LastUpdated = DateTime.Now
            };
            EconomyCurrencyData.Upsert(record);
            return record;
        }

        public CurrencyRateResult CalculateRate(int countryId)
        {
            if (!IsEnabled)
                throw new InvalidOperationException("Валютна система вимкнена.");
            var country = CountriesData.GetCountryById(countryId);
            if (country == null)
                throw new InvalidOperationException("Country not found.");

            var currency = EnsureCurrencyForCountry(country);
            var lastTurn = EconomyTurnHistoryData.GetLatestTurn(countryId);

            float inflationImpact = EconomyConfigData.GetFloat("CurrencyInflationImpact", 0.5f);
            float shortageImpact = EconomyConfigData.GetFloat("CurrencyShortageImpact", 0.03f);
            float demandSupplyImpact = EconomyConfigData.GetFloat("CurrencyDemandSupplyImpact", 0.1f);
            float warShock = EconomyConfigData.GetFloat("CurrencyWarShock", 0.2f);
            float crisisShock = EconomyConfigData.GetFloat("CurrencyCrisisShock", 0.25f);
            float interventionLimit = EconomyConfigData.GetFloat("CurrencyInterventionLimit", 0.15f);
            float blackMarketBase = EconomyConfigData.GetFloat("CurrencyBlackMarketBase", 1.1f);
            float blackMarketScarcityImpact = EconomyConfigData.GetFloat("CurrencyBlackMarketScarcityImpact", 0.6f);

            float inflationFactor = Math.Max(0f, country.Inflation / 100f) * inflationImpact;

            float shortageScore = lastTurn == null ? 0f : lastTurn.ShortageScore;
            float shortageFactor = shortageScore * shortageImpact;

            float demandIndex = currency.DemandIndex <= 0 ? 1f : currency.DemandIndex;
            float supplyIndex = currency.SupplyIndex <= 0 ? 1f : currency.SupplyIndex;
            float demandSupplyFactor = (demandIndex - supplyIndex) * demandSupplyImpact;

            float shock = 0f;
            if (country.WarState)
                shock += warShock;
            if (country.IsInCrisis)
                shock += crisisShock;

            float intervention = Math.Clamp(currency.InterventionStrength, -interventionLimit, interventionLimit);

            float scarcityIndex = Math.Clamp(shortageScore / 100f, 0f, 1f);
            float blackMarketMultiplier = Math.Max(blackMarketBase, blackMarketBase + scarcityIndex * blackMarketScarcityImpact);

            decimal baseRate = currency.BaseRate <= 0m ? 1m : currency.BaseRate;
            decimal rate = baseRate;
            rate *= (decimal)(1f - inflationFactor);
            rate *= (decimal)(1f - shortageFactor);
            rate *= (decimal)(1f - demandSupplyFactor);
            rate *= (decimal)(1f - shock);
            rate *= (decimal)(1f + intervention);
            if (rate < 0.000001m)
                rate = 0.000001m;

            decimal blackMarketRate = rate * (decimal)blackMarketMultiplier;

            EconomyCurrencyData.UpdateRates(
                countryId,
                rate,
                blackMarketRate,
                inflationFactor,
                supplyIndex,
                demandIndex,
                intervention,
                shock,
                scarcityIndex,
                blackMarketMultiplier
            );

            return new CurrencyRateResult
            {
                Currency = EconomyCurrencyData.GetByCountry(countryId) ?? currency,
                OfficialRateToGold = rate,
                BlackMarketRateToGold = blackMarketRate,
                InflationFactor = inflationFactor,
                SupplyIndex = supplyIndex,
                DemandIndex = demandIndex,
                InterventionStrength = intervention,
                ExternalShock = shock,
                ScarcityIndex = scarcityIndex
            };
        }

        public decimal ConvertCurrency(decimal amount, string fromCurrencyCode, string toCurrencyCode, bool useBlackMarket, out string details)
        {
            if (!IsEnabled)
                throw new InvalidOperationException("Валютна система вимкнена.");
            if (amount < 0)
                amount = Math.Abs(amount);

            decimal goldAmount;
            if (IsGold(fromCurrencyCode))
            {
                goldAmount = amount;
            }
            else
            {
                var from = EconomyCurrencyData.GetByCode(fromCurrencyCode);
                if (from == null)
                    throw new InvalidOperationException("Unknown source currency.");
                var refreshedFrom = CalculateRate(from.CountryId);
                var rateFrom = useBlackMarket ? refreshedFrom.BlackMarketRateToGold : refreshedFrom.OfficialRateToGold;
                goldAmount = amount * rateFrom;
            }

            decimal result;
            if (IsGold(toCurrencyCode))
            {
                result = goldAmount;
            }
            else
            {
                var to = EconomyCurrencyData.GetByCode(toCurrencyCode);
                if (to == null)
                    throw new InvalidOperationException("Unknown target currency.");
                var refreshedTo = CalculateRate(to.CountryId);
                var rateTo = useBlackMarket ? refreshedTo.BlackMarketRateToGold : refreshedTo.OfficialRateToGold;
                result = rateTo == 0 ? 0 : goldAmount / rateTo;
            }

            details = $"Gold еквівалент: {FmtNumber(goldAmount)}";
            return result;
        }

        public string Transfer(int fromCountryId, int toCountryId, decimal amount, long userId)
        {
            TryTransferCore(fromCountryId, toCountryId, amount, userId, out _, out string message);
            return message;
        }

        public string CreateAidTranche(int fromCountryId, int toCountryId, decimal dailyAmount, int days, long userId)
        {
            if (fromCountryId == toCountryId)
                return "Країна-отримувач має бути іншою.";

            if (dailyAmount <= 0m)
                return "Сума має бути більшою за 0.";

            int normalizedDays = Math.Clamp(days, 1, 365);
            if (normalizedDays <= 1)
                return Transfer(fromCountryId, toCountryId, dailyAmount, userId);

            var fromCountry = CountriesData.GetCountryById(fromCountryId);
            var toCountry = CountriesData.GetCountryById(toCountryId);
            if (fromCountry == null || toCountry == null)
                return "Країну не знайдено.";

            int trancheId = EconomyAidTranchesData.Add(fromCountryId, toCountryId, dailyAmount, normalizedDays, userId);
            if (trancheId <= 0)
                return "❌ Не вдалося створити транш.";

            string fromName = string.IsNullOrWhiteSpace(fromCountry.Name) ? $"#{fromCountry.CountryId}" : fromCountry.Name;
            string toName = string.IsNullOrWhiteSpace(toCountry.Name) ? $"#{toCountry.CountryId}" : toCountry.Name;
            string amountLabel;
            if (IsEnabled)
            {
                var rate = CalculateRate(fromCountryId);
                amountLabel = $"{FmtNumber(dailyAmount)} {rate.Currency.CurrencyCode}/день";
            }
            else
            {
                amountLabel = $"{FmtNumber(dailyAmount)} Gold/день";
            }

            return $"✅ Транш #{trancheId}: {amountLabel}, {normalizedDays} дн. ({fromName} ➜ {toName})";
        }

        public string ProcessAidTranchesForCountry(int fromCountryId)
        {
            var tranches = EconomyAidTranchesData.GetActiveByFromCountry(fromCountryId);
            if (tranches.Count == 0)
                return string.Empty;

            int paid = 0;
            int failed = 0;
            int finished = 0;
            var failedDetails = new List<string>();

            foreach (var tranche in tranches)
            {
                if (tranche.FromCountryId == tranche.ToCountryId)
                {
                    EconomyAidTranchesData.Deactivate(tranche.TrancheId);
                    continue;
                }

                bool ok = TryTransferCore(
                    tranche.FromCountryId,
                    tranche.ToCountryId,
                    tranche.DailyAmount,
                    tranche.CreatedByUserId,
                    out _,
                    out string transferMessage,
                    reasonPrefix: $"Транш #{tranche.TrancheId}");

                if (!ok)
                {
                    failed++;
                    if (failedDetails.Count < 2)
                        failedDetails.Add($"#{tranche.TrancheId}: {transferMessage}");
                    continue;
                }

                paid++;
                EconomyAidTranchesData.ApplySuccessfulDay(tranche.TrancheId);
                if (tranche.RemainingDays <= 1)
                    finished++;
            }

            if (paid == 0 && failed == 0)
                return string.Empty;

            var sb = new StringBuilder();
            sb.Append($"💸 Валютні транші: успішно {FmtNumber((long)paid)}");
            if (failed > 0)
                sb.Append($", пропущено {FmtNumber((long)failed)}");
            if (finished > 0)
                sb.Append($", завершено {FmtNumber((long)finished)}");
            if (failedDetails.Count > 0)
            {
                sb.AppendLine();
                sb.Append(string.Join("; ", failedDetails));
            }

            return sb.ToString();
        }

        private bool TryTransferCore(
            int fromCountryId,
            int toCountryId,
            decimal amount,
            long userId,
            out decimal goldAmount,
            out string message,
            string reasonPrefix = null)
        {
            goldAmount = 0m;

            if (fromCountryId == toCountryId)
            {
                message = "Країна-отримувач має бути іншою.";
                return false;
            }

            var fromCountry = CountriesData.GetCountryById(fromCountryId);
            var toCountry = CountriesData.GetCountryById(toCountryId);
            if (fromCountry == null || toCountry == null)
            {
                message = "Країну не знайдено.";
                return false;
            }

            if (amount <= 0m)
            {
                message = "Сума має бути більшою за 0.";
                return false;
            }

            bool currencyEnabled = IsEnabled;
            CurrencyRateResult fromRate = null;
            if (currencyEnabled)
            {
                fromRate = CalculateRate(fromCountryId);
                _ = CalculateRate(toCountryId);
                goldAmount = amount * fromRate.OfficialRateToGold;
            }
            else
            {
                 
                goldAmount = amount;
            }

            long roundedGoldAmount = (long)Math.Round(goldAmount);
            if (fromCountry.Treasury < roundedGoldAmount)
            {
                message = "Недостатньо коштів у казні відправника (у золоті).";
                return false;
            }

            fromCountry.Treasury -= roundedGoldAmount;
            toCountry.Treasury += roundedGoldAmount;

            CountriesData.UpdateCountry(fromCountry);
            CountriesData.UpdateCountry(toCountry);

            string toName = string.IsNullOrWhiteSpace(toCountry.Name) ? $"#{toCountry.CountryId}" : toCountry.Name;
            string fromName = string.IsNullOrWhiteSpace(fromCountry.Name) ? $"#{fromCountry.CountryId}" : fromCountry.Name;
            string expenseReason = string.IsNullOrWhiteSpace(reasonPrefix)
                ? $"Трансфер у {toName}"
                : $"{reasonPrefix}: у {toName}";
            string incomeReason = string.IsNullOrWhiteSpace(reasonPrefix)
                ? $"Трансфер від {fromName}"
                : $"{reasonPrefix}: від {fromName}";

            EconomyMoneyLogData.AddLog(fromCountryId, userId, goldAmount, "Expense", expenseReason, 0);
            EconomyMoneyLogData.AddLog(toCountryId, userId, goldAmount, "Income", incomeReason, 0);

            if (currencyEnabled && fromRate != null)
                message = $"Трансфер: {FmtNumber(amount)} {fromRate.Currency.CurrencyCode} ≈ {FmtNumber(goldAmount)} Gold.";
            else
                message = $"Трансфер: {FmtNumber(goldAmount)} Gold.";
            return true;
        }

        public string AdjustRate(int countryId, decimal newRate)
        {
            if (!IsEnabled)
                return "Валютна система вимкнена.";
            if (newRate <= 0)
                return "Новий курс має бути > 0.";
            EconomyCurrencyData.SetRate(countryId, newRate);
            return $"Курс оновлено: 1 {GetCurrencyCode(countryId)} = {FmtNumber(newRate)} Gold.";
        }

        public string Loan(int countryId, decimal amount, long userId)
        {
            return Loan(0, countryId, amount, userId);
        }

        public string Loan(int lenderCountryId, int recipientCountryId, decimal amount, long userId)
        {
            if (amount <= 0)
                return "Сума має бути більшою за 0.";
            var recipient = CountriesData.GetCountryById(recipientCountryId);
            if (recipient == null)
                return "Країну-отримувача не знайдено.";

            if (lenderCountryId > 0)
            {
                var lender = CountriesData.GetCountryById(lenderCountryId);
                if (lender == null)
                    return "Країну-кредитора не знайдено.";
                if (lender.Treasury < (long)Math.Round(amount))
                    return "Недостатньо коштів у казні кредитора.";

                lender.Treasury -= (long)Math.Round(amount);
                recipient.Treasury += (long)Math.Round(amount);
                CountriesData.UpdateCountry(lender);
                CountriesData.UpdateCountry(recipient);

                EconomyMoneyLogData.AddLog(lenderCountryId, userId, amount, "Expense", $"Позика для {recipient.Name}", 0);
                EconomyMoneyLogData.AddLog(recipientCountryId, userId, amount, "Income", $"Позика від {lender.Name}", 0);
                return $"Позика видана: {FmtNumber(amount)} Gold (від {lender.Name}).";
            }

            recipient.Treasury += (long)Math.Round(amount);
            CountriesData.UpdateCountry(recipient);
            EconomyMoneyLogData.AddLog(recipientCountryId, userId, amount, "Income", "Позика (Світовий банк)", 0);
            return $"Позика видана: {FmtNumber(amount)} Gold (Світовий банк).";
        }

        public string Repay(int countryId, decimal amount, long userId)
        {
            if (amount <= 0)
                return "Сума має бути більшою за 0.";
            var country = CountriesData.GetCountryById(countryId);
            if (country == null)
                return "Країну не знайдено.";
            if (country.Treasury < (long)Math.Round(amount))
                return "Недостатньо коштів у казні.";

            country.Treasury -= (long)Math.Round(amount);
            CountriesData.UpdateCountry(country);
            EconomyMoneyLogData.AddLog(countryId, userId, amount, "Expense", "Погашення позики", 0);
            return $"Погашено: {FmtNumber(amount)} Gold.";
        }

        public string BuildRateSummary(int countryId)
        {
            if (!IsEnabled)
                return "Валютна система вимкнена.";
            var country = CountriesData.GetCountryById(countryId);
            if (country == null)
                return "Країну не знайдено.";
            var rate = CalculateRate(countryId);
            return
$@"💱 Курс валюти: {country.Name}
Код: {rate.Currency.CurrencyCode} ({rate.Currency.CurrencyName})
Офіційний курс: 1 {rate.Currency.CurrencyCode} = {FmtNumber(rate.OfficialRateToGold, 4)} Gold
Чорний ринок: 1 {rate.Currency.CurrencyCode} = {FmtNumber(rate.BlackMarketRateToGold, 4)} Gold
Інфляція вплив: -{rate.InflationFactor:0.###}
Попит/пропозиція: {rate.DemandIndex:0.##}/{rate.SupplyIndex:0.##}
Шок: -{rate.ExternalShock:0.###}
Інтервенція: {rate.InterventionStrength:0.###}
Дефіцит: {rate.ScarcityIndex:0.##}";
        }

        public string BuildCentralBankView(int countryId)
        {
            if (!IsEnabled)
                return "Валютна система вимкнена.";
            var country = CountriesData.GetCountryById(countryId);
            if (country == null)
                return "Країну не знайдено.";
            var currency = EnsureCurrencyForCountry(country);
            return
$@"🏦 Центральний банк: {country.Name}
Валюта: {currency.CurrencyCode} ({currency.CurrencyName})
Резерви: {FmtNumber(currency.CentralBankReserve)}
Базовий курс: 1 {currency.CurrencyCode} = {FmtNumber(currency.BaseRate, 4)} Gold
Поточний курс: 1 {currency.CurrencyCode} = {FmtNumber(currency.CurrentRate, 4)} Gold
Останнє оновлення: {currency.LastUpdated:dd.MM.yyyy HH:mm}";
        }

        public string BuildInflationView(int countryId)
        {
            var country = CountriesData.GetCountryById(countryId);
            if (country == null)
                return "Країну не знайдено.";
            float inflation = country.Inflation;
            decimal priceMultiplier = EconomyService.GetInflationPriceMultiplier(inflation);
            return
$@"📈 Інфляція: {country.Name}
Поточний рівень: {inflation:0.##}%
Множник цін: x{FmtNumber(priceMultiplier, 3)}

Чим вища інфляція, тим вищі ціни на ресурси, торгівлю та будівництво.";
        }

        public string BuildBlackMarketRateView(int countryId)
        {
            if (!IsEnabled)
                return "Валютна система вимкнена.";
            var rate = CalculateRate(countryId);
            return $"🕶 Чорний курс: 1 {rate.Currency.CurrencyCode} = {FmtNumber(rate.BlackMarketRateToGold, 4)} Gold";
        }

        public string BuildExampleTurnCalculation(int countryId)
        {
            if (!IsEnabled)
                return "Валютна система вимкнена.";
            var rate = CalculateRate(countryId);
            decimal baseRate = rate.Currency.BaseRate;
            decimal afterInflation = baseRate * (decimal)(1f - rate.InflationFactor);
            decimal afterShortage = afterInflation * (decimal)(1f - (rate.ScarcityIndex * 0.03f));
            decimal afterShock = afterShortage * (decimal)(1f - rate.ExternalShock);
            decimal finalRate = rate.OfficialRateToGold;

            return
$@"🧮 Приклад розрахунку за день:
BaseRate {FmtNumber(baseRate, 4)} → інфляція {FmtNumber(afterInflation, 4)}
→ дефіцит {FmtNumber(afterShortage, 4)} → шок {FmtNumber(afterShock, 4)}
→ фінальний курс {FmtNumber(finalRate, 4)} Gold";
        }

        public string GetCurrencyCode(int countryId)
        {
            var country = CountriesData.GetCountryById(countryId);
            if (country == null)
                return "???";
            return EnsureCurrencyForCountry(country).CurrencyCode;
        }

        public int? ResolveCountryId(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
                return null;
            if (int.TryParse(token, out int id))
                return id;

            var countries = CountriesData.GetAllCountries();
            var match = countries.FirstOrDefault(c => string.Equals(c.Name, token, StringComparison.OrdinalIgnoreCase));
            if (match != null)
                return match.CountryId;

            match = countries.FirstOrDefault(c => c.Name.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0);
            return match?.CountryId;
        }

        public string ResolveCurrencyCode(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
                return "GOLD";
            if (IsGold(token))
                return "GOLD";

            var currency = EconomyCurrencyData.GetByCode(token.ToUpperInvariant());
            if (currency != null)
                return currency.CurrencyCode;

            var countryId = ResolveCountryId(token);
            if (countryId.HasValue)
            {
                var country = CountriesData.GetCountryById(countryId.Value);
                if (country != null)
                    return EnsureCurrencyForCountry(country).CurrencyCode;
            }

            return token.ToUpperInvariant();
        }

        private static bool IsGold(string code)
        {
            return string.Equals(code, "GOLD", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(code, "G", StringComparison.OrdinalIgnoreCase);
        }

        private static string BuildCurrencyCode(CountryRecord country)
        {
            string baseCode = new string((country.Name ?? string.Empty)
                .Where(char.IsLetter)
                .Take(3)
                .Select(c => char.ToUpperInvariant(c))
                .ToArray());

            if (string.IsNullOrWhiteSpace(baseCode))
                baseCode = "CUR";

            string code = baseCode;
            int i = 1;
            while (EconomyCurrencyData.GetByCode(code) != null)
            {
                code = $"{baseCode}{i}";
                i++;
            }
            return code;
        }
    }
}

