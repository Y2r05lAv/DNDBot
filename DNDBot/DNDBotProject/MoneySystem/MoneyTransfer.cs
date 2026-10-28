using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace DNDBot
{
    class MoneyTransfer
    {
        private readonly ILogger<MoneyTransfer> _logger;

        Dictionary<string, int> coins = new()
        {
            { "copper", 0 },
            { "silver", 0 },
            { "gold", 0 },
            { "platinum", 0 }
        };


         
        Dictionary<string, int> coinValue = new()
        {
            { "copper", 1 },
            { "silver", 10 },
            { "gold", 100 },
            { "platinum", 1000 }
        };


        public MoneyTransfer(ILogger<MoneyTransfer> logger)
        {
            _logger = logger;
        }


        public bool TransferMoney(int amount, string toCoin, long userID)
        {
            foreach (var key in coins.Keys.ToList())
                coins[key] = UserData.DataBaseValueReader<int>(userID, key switch
                {
                    "copper" => "CopperCoin",
                    "silver" => "SilverCoin",
                    "gold" => "GoldenCoin",
                    "platinum" => "PlatinumCoin",
                    _ => throw new Exception("Invalid coin")
                });

            if (TryConvertTo(toCoin, amount))
            {
                UpdateDataBaseValue(userID);
                _logger.LogInformation("Успішна операція з {ToCoin} для користувача {UserId}", toCoin, userID);
                return true;
            }
            else
            {
                _logger.LogWarning("Недостатньо коштів для операції. Користувач: {UserId}, Ціль: {ToCoin}, Кількість: {Amount}", userID, toCoin, amount);
                return false;
            }
        }


        private bool TryConvertTo(string targetCoin, int amount)
        {
            if (!coinValue.TryGetValue(targetCoin, out int targetValue))
            {
                _logger.LogWarning("Invalid target coin: {TargetCoin}", targetCoin);
                return false;
            }

             
            long neededCopper = (long)amount * targetValue;

             
            long totalCopper = coins.Sum(kv => (long)kv.Value * coinValue[kv.Key]);
            if (totalCopper < neededCopper)
                return false;

             
            var work = coins.ToDictionary(k => k.Key, v => v.Value);

            long collected = 0;

             
             
             
            var largerThanTarget = coinValue
                .Where(kv => kv.Value > targetValue && kv.Key != targetCoin)
                .OrderBy(kv => kv.Value)
                .Select(kv => kv.Key)
                .ToList();

            var smallerThanTarget = coinValue
                .Where(kv => kv.Value < targetValue && kv.Key != targetCoin)
                .OrderByDescending(kv => kv.Value)
                .Select(kv => kv.Key)
                .ToList();

             
            foreach (var src in largerThanTarget)
            {
                if (collected >= neededCopper) break;
                int srcValue = coinValue[src];
                int availableCoins = work[src];
                if (availableCoins <= 0) continue;

                 
                long needCoins = (long)Math.Ceiling((neededCopper - collected) / (double)srcValue);
                int take = (int)Math.Min(availableCoins, needCoins);

                collected += (long)take * srcValue;
                work[src] -= take;
            }

             
            foreach (var src in smallerThanTarget)
            {
                if (collected >= neededCopper) break;
                int srcValue = coinValue[src];
                int availableCoins = work[src];
                if (availableCoins <= 0) continue;

                long needCoins = (long)Math.Ceiling((neededCopper - collected) / (double)srcValue);
                int take = (int)Math.Min(availableCoins, needCoins);

                collected += (long)take * srcValue;
                work[src] -= take;
            }

             
            if (collected < neededCopper && work[targetCoin] > 0)
            {
                int srcValue = coinValue[targetCoin];
                int availableCoins = work[targetCoin];
                long needCoins = (long)Math.Ceiling((neededCopper - collected) / (double)srcValue);
                int take = (int)Math.Min(availableCoins, needCoins);

                collected += (long)take * srcValue;
                work[targetCoin] -= take;
            }

            if (collected < neededCopper)
            {
                 
                _logger.LogWarning("Collecting funds failed despite total check. Collected {Collected}, Needed {Needed}", collected, neededCopper);
                return false;
            }

             
             
            long leftoverCopper = collected - neededCopper;

             
            work[targetCoin] += amount;

             
            work["copper"] += (int)leftoverCopper;

             
            foreach (var k in work.Keys.ToList())
                coins[k] = work[k];

            return true;
        }



        public void UpdateDataBaseValue(long userID)
        {
            _logger.LogInformation(
                "Updating coin values for user {UserId}: Copper={Copper}, Silver={Silver}, Gold={Gold}, Platinum={Platinum}",
                userID, coins["copper"], coins["silver"], coins["gold"], coins["platinum"]
            );

             
            var dbMap = new Dictionary<string, string>
            {
                { "copper", "CopperCoin" },
                { "silver", "SilverCoin" },
                { "gold", "GoldenCoin" },
                { "platinum", "PlatinumCoin" }
            };

            foreach (var coin in coins)
            {
                if (dbMap.TryGetValue(coin.Key, out var dbName))
                {
                    UserData.UpdateDataBaseValue(dbName, userID, coin.Value);
                }
                else
                {
                    _logger.LogWarning("No database mapping for coin type: {CoinKey}", coin.Key);
                }
            }
        }

    }
}
