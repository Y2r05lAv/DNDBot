using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot.Types;
using System.Threading;
using Telegram.Bot;

namespace DNDBot.BotSystem
{
    public class InlineKeyboardsClass : BotBodyRealisation
    {
        public static InlineKeyboardMarkup CreateInlineKeyboard(params (string text, string callback)[][] rows)
        {
            var keyboard = rows.Select(row => row.Select(b => InlineKeyboardButton.WithCallbackData(b.text, b.callback)).ToArray()).ToArray();
            return new InlineKeyboardMarkup(keyboard);
        }


        public static InlineKeyboardMarkup MagicInlineKeyboardMarkup() =>
            CreateInlineKeyboard(
                     [("Переглянути ефекти", "Spell_check"), ("Застосувати ефект", "Spell_use")],
                     [("Додати ефект", "Spell_add"), ("Видалити ефект", "Spell_delete")],
                     [("Змінити ефект", "Spell_edit")]
                );


        public static InlineKeyboardMarkup TableInlineKeyboardMarkup() =>
        CreateInlineKeyboard(
            [("Індивіндуальні скарби: Показник небезпечнсті", "table_individual"), ("Рідкість магічний предметів", "table_rare")],
            [("Скарбниця: Показник небезпечності 0-4", "table_treasure04"), ("Скарбниця: Показник небезпечності 5-10", "table_treasure510")],
            [("Таблиця магічних предметів: А", "table_magicItemA"), ("Таблиця магічних предметів: Б", "table_magicItemB")],
            [("Таблиця магічних предметів: В", "table_magicItemV"), ("Таблиця магічних предметів: Е", "table_magicItemE")]

        );


        public static InlineKeyboardMarkup DiceInlineKeyboard() =>
        CreateInlineKeyboard(
            [("d4", "ThDice_4"), ("d6", "ThDice_6")],
            [("d8", "ThDice_8"), ("d10", "ThDice_10")],
            [("d12", "ThDice_12"), ("d20", "ThDice_20")]
        );

        public static InlineKeyboardMarkup TemporaryHpActionsKeyboard() =>
            CreateInlineKeyboard(
                [("➕ Додати тимчасові хіти", "thp_menu_add")],
                [("♻ Скинути тимчасові хіти", "thp_menu_clear")]
            );

        public static InlineKeyboardMarkup TemporaryHpReductionActionsKeyboard() =>
            CreateInlineKeyboard(
                [("➖ Знизити поточне і максимальне HP", "hpred_menu_add")],
                [("♻ Скинути зниження HP", "hpred_menu_reset")]
            );

        public static InlineKeyboardMarkup MonsterHasAttackKeyboard() =>
            CreateInlineKeyboard(
                [("Так", "monster_attack_yes"), ("Ні", "monster_attack_no")]
            );

        public static InlineKeyboardMarkup MonsterAttackDiceKeyboard() =>
            CreateInlineKeyboard(
                [("d4", "monster_attack_dice_4"), ("d6", "monster_attack_dice_6"), ("d8", "monster_attack_dice_8")],
                [("d10", "monster_attack_dice_10"), ("d12", "monster_attack_dice_12"), ("d20", "monster_attack_dice_20")]
            );

        public static InlineKeyboardMarkup MonsterAttackContinueKeyboard() =>
            CreateInlineKeyboard(
                [("➕ Додати ще урон", "monster_attack_add_more")],
                [("✅ Готово", "monster_attack_done")]
            );

        public static InlineKeyboardMarkup CombatControlKeyboard(bool isActive) =>
            CreateInlineKeyboard(
                [(isActive ? "⚔ Перезапустити бій" : "⚔ Розпочати бій", "combat_start"), ("➡ Наступний хід", "combat_next")],
                [("📋 Стан бою", "combat_status"), ("🛑 Завершити бій", "combat_end")],
                [("⚔ Атака", "combat_attack"), ("💊 Вилікувати", "combat_heal")],
                [("➕ Додати учасників", "combat_add_participants")]
            );

        public static InlineKeyboardMarkup CombatMonsterAttackModeKeyboard(long monsterId, bool hasAutoAttack)
        {
            var rows = new List<(string text, string callback)[]>
            {
                new[] { ("🧮 Розрахувати самостійно", $"combat_monster_manual_{monsterId}") }
            };

            if (hasAutoAttack)
            {
                rows.Add(new[] { ("🤖 Розрахувати автоматично", $"combat_monster_auto_{monsterId}") });
            }

            return CreateInlineKeyboard(rows.ToArray());
        }


        public static InlineKeyboardMarkup Transictions(Message message)
        {
            if (message?.From == null)
                return CreateInlineKeyboard();

            long userId = message.From.Id;
            var buttons = new List<(string, string)>
            {
                ("Перевірити гаманець", "trf_checkWallet"),
                ("Заплатити", "trf_pay"),
                ("Перевести грошову одиницю", "trf_convert")
            };

            if(userId != masterID){
                buttons.AddRange([
                    ("Додати гроші", "trf_add"),
                    ("Відняти гроші", "trf_min"),
                ]);
            }

            if (userId == masterID)
            {
                buttons.AddRange(
                [
                ("Додати всім", "trf_AddToAll"),
                ("Оштрафувати гравця", "trf_uitgeven"),
                ("Оштрафувати всіх", "trf_fine")
                ]);
            }

            return CreateInlineKeyboard(buttons.Select(b => new[] { b }).ToArray());
        }

        public static async Task MoneyAfNemenForConsument(ITelegramBotClient botClient, CancellationToken cancellationToken, long userId)
        {
            var users = UserData.GetUsersFromDatabase();
            var rows = users
                .Select(user =>
                {
                    string characterName = string.IsNullOrWhiteSpace(user.Name)
                        ? UserData.GetCharacterName(user.Id, user.CharacterId)
                        : user.Name;
                    return new[] { ($"{characterName} (C{user.CharacterId})", $"uitgeven_{user.Id}_{user.CharacterId}") };
                })
                .ToArray();

            var inlineKeyboard = CreateInlineKeyboard(
                rows
            );

            OldMessages[userId].Add((await botClient.SendTextMessageAsync(userId, "Кого ви хочете оштрафувати?", replyMarkup: inlineKeyboard, cancellationToken: cancellationToken)).MessageId);
        }

        public static async Task InlineButtonsCreation(ITelegramBotClient botClient, CancellationToken cancellationToken, long userId, string CallBackData, string msgText)
        {
            var inlineKeyboard = CreateInlineKeyboard(
                [("Мідна монета", $"{CallBackData}copper"),
                 ("Срібна монета", $"{CallBackData}silver"),
                 ("Золота монета", $"{CallBackData}gold"),
                 ("Платинова монета", $"{CallBackData}platinum")]
            );

            OldMessages[userId].Add((await botClient.SendTextMessageAsync(userId, msgText, replyMarkup: inlineKeyboard, cancellationToken: cancellationToken)).MessageId);
        }

        public static async Task ButtonInventoryCreation(ITelegramBotClient botClient, CancellationToken cancellationToken, long userId, string CallBackData, string msgText)
        {
            if (!OldMessages.TryGetValue(userId, out List<int> value))
                OldMessages[userId] = [];

            var inlineKeyboard = CreateInlineKeyboard(userId != masterID ?
                 [
                    [ ("Перевірити інвентар", $"{CallBackData}check") ],
                    [ ("Додати новий предмет", $"{CallBackData}add") ],
                    [ ("Призначити", $"{CallBackData}set") ]
                 ]
                  :
                [
                    [ ("Перевірити інвентар гравця", $"{CallBackData}masterCheck") ],
                    [ ("Додати новий предмет до гравця", $"{CallBackData}masterAdd") ],
                    [ ("Призначити", $"{CallBackData}set") ]
                ]
            );

            OldMessages[userId].Add((await botClient.SendTextMessageAsync(userId, msgText, replyMarkup: inlineKeyboard, cancellationToken: cancellationToken)).MessageId);
        }


        public static async Task InventoryMasterCallBack(ITelegramBotClient botClient, CancellationToken cancellationToken, long userId)
        {
            var users = UserData.GetUsersFromDatabase();
            var rows = users
                .Select(user =>
                {
                    string characterName = string.IsNullOrWhiteSpace(user.Name)
                        ? UserData.GetCharacterName(user.Id, user.CharacterId)
                        : user.Name;
                    return new[] { ($"{characterName} (C{user.CharacterId})", $"uitgeven_{user.Id}_{user.CharacterId}") };
                })
                .ToArray();

            var inlineKeyboard = CreateInlineKeyboard(
                rows
            );

            OldMessages[userId].Add((await botClient.SendTextMessageAsync(userId, "Кого ви хочете оштрафувати?", replyMarkup: inlineKeyboard, cancellationToken: cancellationToken)).MessageId);
        }

        public static InlineKeyboardMarkup InlineAttackButtons(bool StateAttack)
            => InlineAttackButtons(StateAttack, out _);

        public static InlineKeyboardMarkup InlineAttackButtons(bool StateAttack, out int targetsCount)
        {
            var users = UserData.GetUsersFromDatabase();
            var buttons = new List<(string, string)>();

            var playersInFight = GetPlayerParticipantsForCombat();
            var playerRows = users
                .Where(user => user.Id != masterID && playersInFight.Contains((user.Id, user.CharacterId)))
                .Select(user => ($"{user.Name} (C{user.CharacterId})", $"{(StateAttack ? "attack" : "heal")}_{user.Id}_{user.CharacterId}"));
            buttons.AddRange(playerRows);

            var monstersInFight = GetMonsterParticipantsForCombat();
            var monsterRows = Monsters
                .Where(m => m != null && monstersInFight.Contains(m.ID))
                .Select(m => ($"{m.Name} (ID: {m.ID})", $"{(StateAttack ? "attack" : "heal")}_{m.ID}_0"));
            buttons.AddRange(monsterRows);

            targetsCount = buttons.Count;

            if (targetsCount == 0)
                return CreateInlineKeyboard([("Немає учасників бою", "noop_no_fight_targets")]);

            return CreateInlineKeyboard(buttons.Select(b => new[] { b }).ToArray());
        }

        public static InlineKeyboardMarkup InlineAttackButtonsAllTargets(bool StateAttack)
            => InlineAttackButtonsAllTargets(StateAttack, out _);

        public static InlineKeyboardMarkup InlineAttackButtonsAllTargets(bool StateAttack, out int targetsCount)
        {
            var users = UserData.GetUsersFromDatabase();
            var buttons = new List<(string, string)>();

            var playerRows = users
                .Where(user => user.Id != masterID)
                .Select(user => ($"{user.Name} (C{user.CharacterId})", $"{(StateAttack ? "attack" : "heal")}_{user.Id}_{user.CharacterId}"));
            buttons.AddRange(playerRows);

            var monsterRows = Monsters
                .Where(m => m != null)
                .Select(m => ($"{m.Name} (ID: {m.ID})", $"{(StateAttack ? "attack" : "heal")}_{m.ID}_0"));
            buttons.AddRange(monsterRows);

            targetsCount = buttons.Count;

            if (targetsCount == 0)
                return CreateInlineKeyboard([("Немає доступних цілей", "noop_no_targets")]);

            return CreateInlineKeyboard(buttons.Select(b => new[] { b }).ToArray());
        }

        public static InlineKeyboardMarkup InlineCombatPlayerTargetButtons(string callbackPrefix, out int targetsCount)
        {
            var playersInFight = GetPlayerParticipantsForCombat();
            var rows = UserData.GetUsersFromDatabase()
                .Where(user => user.Id != masterID && playersInFight.Contains((user.Id, user.CharacterId)))
                .Select(user => new[] { ($"{user.Name} (C{user.CharacterId})", $"{callbackPrefix}_{user.Id}_{user.CharacterId}") })
                .ToArray();

            targetsCount = rows.Length;
            return targetsCount == 0
                ? CreateInlineKeyboard([("Немає доступних гравців", "noop_no_players")])
                : CreateInlineKeyboard(rows);
        }

        public static bool IsAllowedCombatTarget(long id, int characterId)
        {
            if (characterId > 0)
                return GetPlayerParticipantsForCombat().Contains((id, characterId));

            return GetMonsterParticipantsForCombat().Contains(id);
        }

        private static HashSet<(long userId, int characterId)> GetPlayerParticipantsForCombat()
        {
            var fromCurrentFight = Initiative.Keys
                .Where(k => k.characterId > 0)
                .ToHashSet();

            if (fromCurrentFight.Count > 0)
                return fromCurrentFight;

            return InitiativeUsers.Keys
                .Where(k => k.characterId > 0)
                .ToHashSet();
        }

        private static HashSet<long> GetMonsterParticipantsForCombat()
        {
            var fromCurrentFight = Initiative.Keys
                .Where(k => k.characterId == 0)
                .Select(k => k.userId)
                .ToHashSet();

            if (fromCurrentFight.Count > 0)
                return fromCurrentFight;

            if (InitiativeUsers.Count == 0)
                return new HashSet<long>();

            return Monsters
                .Where(m => m != null)
                .Select(m => m.ID)
                .ToHashSet();
        }

        public static InlineKeyboardMarkup InlineUserTargetButtons(string callbackPrefix)
        {
            var users = UserData.GetUsersFromDatabase()
                .Where(user => user.Id != masterID)
                .Select(user => new[] { ($"{user.Name} (C{user.CharacterId})", $"{callbackPrefix}_{user.Id}_{user.CharacterId}") })
                .ToArray();

            return users.Length == 0
                ? CreateInlineKeyboard([("Немає доступних цілей", "noop_target")])
                : CreateInlineKeyboard(users);
        }



    }
}
