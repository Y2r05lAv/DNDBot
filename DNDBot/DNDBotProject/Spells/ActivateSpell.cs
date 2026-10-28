using DNDBot.BotMath;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace DNDBot.Spells
{
    public class ActivateSpell : SpellCollector
    {

        public static async Task StartVoid(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken, long userId)
        {


            isProcessingAnotherProcess[userId] = true;

            StateBotMSG[userId] = 1;

            if (!OldMessages.ContainsKey(userId))
                OldMessages.Add(userId, new List<int>());

            if (!userStateSpells.ContainsKey(userId))
                userStateSpells.Add(userId, "");

            if (message != null)
                OldMessages[userId].Add(message.MessageId);

            userStateSpells[userId] = "pasteActive";

            await PasteVoid(botClient, message, cancellationToken, userId, 0);
        }

        public static async Task PasteVoid(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken, long userId, int SpellId)
        {
            if (!StateBotMSG.ContainsKey(userId))
                return;

            switch (StateBotMSG[userId])
            {
                case 1:
                     
                    await UseSpelInlineKeyboard(botClient, cancellationToken, userId);
                    break;
                case 0:

                    if (SpellData.CheckifExist(userId, SpellId))
                    {

                        if (UserData.CheckifExist(userId))
                        {

                            var spellDataClass = SpellData.GetSpellToClass(userId, SpellId);
                            if (spellDataClass == null) return;

                            var breakdown = new List<string>();
                            int total = 0;

                             
                            List<DamagePart> parts = TryParseDamageText(spellDataClass.DamageDice);

                             
                            if (parts == null || parts.Count == 0)
                            {
                                try
                                {
                                    parts = JsonSerializer.Deserialize<List<DamagePart>>(spellDataClass.DamageDice ?? "[]");
                                }
                                catch {   }
                            }

                             
                            if (parts == null || parts.Count == 0)
                            {
                                if (!string.IsNullOrWhiteSpace(spellDataClass.DamageDice) && spellDataClass.DamageDice.Contains('d'))
                                {
                                    var seg = spellDataClass.DamageDice.Split('d');
                                    if (int.TryParse(seg[0], out var c) && int.TryParse(seg[1], out var d))
                                        parts = new List<DamagePart> {
                    new DamagePart { Count = c, Die = d, Type = spellDataClass.TypeHarm, Modifier = spellDataClass.Modifier }
                };
                                }
                            }

                            if (parts == null || parts.Count == 0)
                            {
                                await botClient.SendTextMessageAsync(userId, "У цього ефекту не налаштовано урон.", cancellationToken: cancellationToken);
                                return;
                            }
                             
                            var modKeyRaw = (spellDataClass.Modifier ?? "").Trim();  
                            var hasMod = !string.IsNullOrEmpty(modKeyRaw);
                            int spellModVal = 0;
                            string spellModNameUk = "";

                            if (hasMod)
                            {
                                spellModVal = UserData.DataBaseValueReader<int>(userId, modKeyRaw);
                                spellModNameUk = ReturnCharacterModificatorNameEnglishToUkrainian(modKeyRaw);
                            }

                             
                            int manaCost = spellDataClass.ManaCost;
                            if (manaCost > 0)
                            {
                                 
                                bool manaTaken = PercentResourceManager.TryConsumeMana(userId, spellDataClass.CharacterId, manaCost.ToString());
                                if (!manaTaken)
                                {
                                    await botClient.SendTextMessageAsync(userId, $"Недостатньо мани ({manaCost}) для застосування {spellDataClass.Name}.", cancellationToken: cancellationToken);

                                     
                                    isProcessingAnotherProcess[userId] = false;
                                    userStateSpells[userId] = "";
                                    StateBotMSG[userId] = 1;
                                    return;
                                }
                            }
                             

                             
                            int rollTotal = 0;
                            foreach (var p in parts)
                            {
                                int roll = 0;
                                for (int i = 0; i < p.Count; i++)
                                    roll += MathfDND.DiceReturn(p.Die);

                                rollTotal += roll;

                                var typeText = string.IsNullOrWhiteSpace(p.Type) ? "" : $" [{p.Type}]";
                                breakdown.Add($"{p.Count}d{p.Die}{typeText} = {roll}");
                            }

                             
                            total = rollTotal + (hasMod ? spellModVal : 0);

                            if (hasMod && spellModVal != 0)
                                breakdown.Add($"+ {spellModNameUk}({spellModVal})");

                             
                            var manaAmount = ManaData.GetMagicStats(userId);
                            var manaLine = manaCost > 0 ? $"\n\nВартість мани: {manaCost}" + $"\nУ вас залишилось: {manaAmount.CurrentMana} / {manaAmount.MaxMana}" : "";
       

                            await botClient.SendTextMessageAsync(
                                userId,
                                $"Ви застосували {spellDataClass.Name}.\n" +
                                string.Join("\n", breakdown) +
                                $"\nРазом: {total}" +
                                manaLine,
                                cancellationToken: cancellationToken
                            );

                            DeleteAllOldMessages(botClient, cancellationToken, userId);

                        }
                        else
                        {
                            await botClient.SendTextMessageAsync(userId, $"Недостаньо дани: У гравця немає модифікаторів в базі даних!", cancellationToken: cancellationToken);
                        }
                    }
                    else
                    {
                        await botClient.SendTextMessageAsync(userId, "Ефект не існує! Виберіть інший ID.", cancellationToken: cancellationToken);
                         
                        return;
                    }

                    isProcessingAnotherProcess[userId] = false;
                    userStateSpells[userId] = "";
                    StateBotMSG[userId] = 1;
                    break;
            }

             
        }


         

        static List<DamagePart> TryParseDamageText(string damage)
        {
             
            if (string.IsNullOrWhiteSpace(damage))
                return new List<DamagePart>();

             
             
             
            var rx = new Regex(@"(?i)(\d+)\s*d\s*(\d+)\s*(?:\[(.*?)\])?", RegexOptions.Compiled);

            var matches = rx.Matches(damage);
            if (matches.Count == 0) return new List<DamagePart>();

            var list = new List<DamagePart>(matches.Count);
            foreach (Match m in matches)
            {
                if (!int.TryParse(m.Groups[1].Value, out var count)) continue;
                if (!int.TryParse(m.Groups[2].Value, out var die)) continue;

                var type = (m.Groups[3].Success ? m.Groups[3].Value : "").Trim();
                list.Add(new DamagePart
                {
                    Count = count,
                    Die = die,
                    Type = type,         
                    Modifier = ""        
                });
            }
            return list;
        }
    }


}
