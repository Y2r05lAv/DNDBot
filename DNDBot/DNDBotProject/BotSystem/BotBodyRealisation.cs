using DNDBot.AttackSystem;
using DNDBot.BotMath;
using DNDBot.BotSystem;
using DNDBot.DataBase;
using DNDBot.Spells;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace DNDBot
{
    public class BotBodyRealisation : BotBodyMethods
    {
        public static ServiceProvider serviceProvider;

        public static readonly long ownerID = long.Parse(ConfigurationManager.ConnectionStrings["BotOwner"].ConnectionString);
        public static readonly long DndMaster = long.Parse(ConfigurationManager.ConnectionStrings["MasterId"].ConnectionString);
        public static long masterID = long.Parse(ConfigurationManager.ConnectionStrings["MasterId"].ConnectionString);
        public static long ParliamentGroupChatId = 0;

        private static string FightKey(long userId, int characterId) => $"{userId}:{characterId}";
        private static bool TryParseFightKey(string key, out long userId, out int characterId)
        {
            userId = 0; characterId = 0;
            var p = key.Split(':');
            return p.Length == 2 && long.TryParse(p[0], out userId) && int.TryParse(p[1], out characterId);
        }


        public static readonly Dictionary<long, ManaDataClass> MagicDataBuffer = new();
        public static Dictionary<long, bool> isProcessingAnotherProcess = new();

        #region Statistic
        public static readonly string[] GeneralCheck = { "Сила", "Спритність", "Витривалість", "Інтелект", "Мудрість", "Харизма" };

        public static readonly string[] PossibleChecks = { "Сила", "Спритність","Витривалість","Інтелект","Мудрість","Харизма", "Акробатика", "Аналіз","Атлетика","Виступ", "Виживання", "Догляд за тваринами",
        "Залякування","Інтуїція","Історія","Медицина","Магія","Непомітність","Обман","Переконання","Пильність","Природа","Релігія","Спритність рук"};

        public static readonly List<string> Strenght = new List<string> { "Сила", "Атлетика" };

        public static readonly List<string> Speed = new List<string> { "Спритність", "Акробатика", "Непомітність", "Спритність рук" };

        public static readonly List<string> Durablity = new List<string> { "Витривалість" };

        public static readonly List<string> Intellect = new List<string> { "Інтелект", "Аналіз", "Історія", "Магія", "Природа", "Релігія" };

        public static readonly List<string> Wisdom = new List<string> { "Мудрість", "Виживання", "Догляд за тваринами", "Інтуїція", "Медицина", "Пильність" };

        public static readonly List<string> Harism = new List<string> { "Харизма", "Виступ", "Залякування", "Обман", "Переконання" };
        #endregion

        public static readonly List<string> Weapon = new List<string> {"Посох", "Булава", "Дубина", "Кийок", "Кинджал", "Легкий молот", "Метальний спис", "Ручна сокира",
        "Серп", "Спис", "Легкий арбалет","Дротик","Короткий лук","Праща","Алебарда","Батіг","Бойова кирка","Бойова сокира","Бойовий молот","Бойовий Ціп","Глефа",
        "Дворучна сокира","Дворучний меч","Довгий меч","Довгий спис","Короткий меч","Кувалда","Моргенштерн","Піка","Рапіра","Тризуб","Шабля","Важкий арбалет",
        "Ручний арбалет","Довгий лук","Духова трубка"
        };

        #region UserState
        public static Dictionary<long, string> userState = new Dictionary<long, string> { { 0, "" } };
        public static Dictionary<long, string> userStateDataCollector = new Dictionary<long, string> { { 0, "" } };
        public static Dictionary<long, string> userStateDataChanger = new Dictionary<long, string> { { 0, "" } };
        public static Dictionary<long, string> userStateXP = new Dictionary<long, string> { { 0, "" } };
        public static Dictionary<long, string> userStateMagicXP = new Dictionary<long, string> { { 0, "" } };
        public static Dictionary<long, string> userStateAtack = new Dictionary<long, string> { { 0, "" } };
        #endregion

        public static MonsterCreator newMonster;

        public static string HealState = "";
        public static string TemporaryHpState = "";
        public static string TemporaryHpReductionState = "";
        public static long choosedUser;
        public static int damage;
        public static int PlayerHit = -1;

        public static long damagedCreature;
        public static int damagedCreatureCharacterId;
        public static int choosedUserCharacterId;
        public static Dictionary<long, int> FightCharacter = new Dictionary<long, int>();


        public static Dictionary<long, List<int>> OldMessages = new Dictionary<long, List<int>> { { 0, new List<int>() } };


        public static List<MonsterCreator> Monsters = new List<MonsterCreator>();
        public static Dictionary<long, UserDataClass> UserDataClassArray = new Dictionary<long, UserDataClass>();
        public static Dictionary<long, InventoryDataClass> InventoryDataClass = new Dictionary<long, InventoryDataClass>();
        public static Dictionary<long, SpellDataClass> SpellDataClass = new Dictionary<long, SpellDataClass>();

        public static Dictionary<(long userId, int characterId), Dictionary<string, int>> InitiativeUsers
            = new(5);

        public static Dictionary<(long userId, int characterId), Dictionary<string, int>> Initiative
            = new();



        public static Dictionary<long, string> userStateInventory = new Dictionary<long, string> { { 0, "" } };
        public static Dictionary<long, int> userStateValueID = new Dictionary<long, int> { { 0, 0 } };
        public static  Dictionary<long, long> StateBotMSG = new Dictionary<long, long> { { 0, 0 } };
        public static Dictionary<long, long> userStatePay = new();
        public static Dictionary<long, int> userStatePayCharacter = new();
        public static Dictionary<long, string> userStateWallet = new();
        public static Dictionary<long, string> userStateValueName = new();
        public static Dictionary<long, string> userStateMethod = new();
        public static Dictionary<long, string> MonsterTemplateState = new();
        public static Dictionary<long, int> SelectedMonsterTemplate = new();


        public static int State = 0;
        public static int MonsterCount = 1;
        public static int MonsterID = 1;

        #region Class
        public static TelegramBotClient Client;
        public static MathfDND MathDND;
        public static WeaponInfo WI;


        public static readonly Dictionary<int, int> XpConsuption = new()
        {
            {1, 0},
            {2, 300},
            {3, 900},
            {4, 2700},
            {5, 6500},
            {6, 14000},
            {7, 23000},
            {8, 34000},
            {9, 48000},
            {10, 64000},
            {11, 85000},
            {12, 100000},
            {13, 120000},
            {14, 140000},
            {15, 165000},
            {16, 195000},
            {17, 225000},
            {18, 265000},
            {19, 305000},
            {20, 355000}
        };
        #endregion



        public override void ClassInitialization()
        {
            Client = new TelegramBotClient(ConfigurationManager.ConnectionStrings["BotKey"].ConnectionString);
            MathDND = new MathfDND();
            WI = new WeaponInfo();
        }


        public override void RemoveMonsterFromAllCollections(long monsterKey)  
        {
            if (Monsters.Any(monster => monster != null && monster.ID == monsterKey))
            {
                Monsters.RemoveAll(monster => monster != null && monster.ID == monsterKey);
                Console.WriteLine($"Монстра {monsterKey} було видалено з Monsters");
            }

            var initiativeKey = (monsterKey, 0);  

            if (Initiative.ContainsKey(initiativeKey))
            {
                Initiative.Remove(initiativeKey);
                Console.WriteLine($"Монстра {monsterKey} було видалено з Initiative");
            }
        }



        public static void DeleteAllOldMessages(ITelegramBotClient botClient, CancellationToken cancellationToken, long userId)
        {
            foreach (var oldMsg in OldMessages[userId])
            {
                DeleteMSG(botClient, oldMsg, userId, cancellationToken);
            }
        }


        public static void DeleteMSG(ITelegramBotClient botClient, int msgId, long userId, CancellationToken cancellationToken)
        {
            if (userId == 0)
                return;

            botClient.DeleteMessageAsync(userId, msgId, cancellationToken: cancellationToken);
        }

        public static void UpdateParliamentGroupChatId(long chatId)
        {
            if (chatId == 0)
                return;

            ParliamentGroupChatId = chatId;
        }

        public override string GetCollectMultiplayers(long userId)
        {
            return UserData.DataBaseReader(userId);
        }

         
        public override ReplyKeyboardMarkup ButtonsCreate(long userID, bool MonsterCreation, bool OneTimeKeyboard = false)
        {

            var keyboardButtons = new List<KeyboardButton[]>();

             
            if (userID != masterID)
            {
                var defaultButtons = new[]
                {
                    CreateRow("Інформація", "Допомога", "Транзакції"),
                    CreateRow("Перевірки", "Попадання", "Ініціатива"),
                    CreateRow("Атака", "Зброя", "Вилікувати"),
                    CreateRow("Бойова панель", "Тимчасові хіти", "Тимчасове зниження ХП"),
                    CreateRow("Сутності", "Бій", "Ефекти", "Мана"),
                    CreateRow("Кубіки", "Інвентар", "Додати досвід"),
                    CreateRow("Додати магічний досвід", "Змінити дані", "Керувати інформацією бота"),
                    CreateRow("Змінити процентні значення", "Зілля мани", "Зілля зцілення"),
                    CreateRow("Відновити ману за хід", "Відновити ману після бою"),
                    CreateRow("Державне управління")
                };

                keyboardButtons.AddRange(defaultButtons);
            }

             
            if (userID == masterID)
            {
                var masterButtons = new[]
                {
                    CreateRow("Інформація", "Допомога", "Транзакції"),
                    CreateRow("Перевірки", "Попадання", "Ініціатива"),
                    CreateRow("Атака", "Зброя", "Вилікувати"),
                    CreateRow("Бойова панель", "Тимчасові хіти", "Тимчасове зниження ХП"),
                    CreateRow("Сутності", "Бій", "Ефекти", "Мана"),
                    CreateRow("Створити монстра", "Шаблони монстрів", "Заскочені монстри", "Очистити"),
                    CreateRow("Кубіки", "Інвентар", "Додати досвід"),
                    CreateRow("Відновити ману за хід", "Відновити ману після бою"),
                    CreateRow("Державне управління", "Додати досвід до всіх")
                };

                keyboardButtons.AddRange(masterButtons);
            }

            var markup = new ReplyKeyboardMarkup(keyboardButtons.ToArray())
            {
                ResizeKeyboard = true,
                OneTimeKeyboard = OneTimeKeyboard
            };

            return markup;
        }
        public override ReplyKeyboardMarkup MiniButtonsCreate(bool OneTimeKeyboard = false)
        {
            var minimalRows = new[]
            {
                    CreateRow("Меню", "Державне управління")
                };

            var minimalMarkup = new ReplyKeyboardMarkup(minimalRows)
            {
                ResizeKeyboard = true,
                OneTimeKeyboard = OneTimeKeyboard
            };

            return minimalMarkup;
        }



        private KeyboardButton[] CreateRow(params string[] labels)
        {
            return labels.Select(label => new KeyboardButton(label)).ToArray();
        }

        public async override Task WeaponOperations(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken, Message message, ReplyKeyboardMarkup replyKeyboard)
        {
            await WeaponRealisation.Weapons(botClient, update, cancellationToken, message, replyKeyboard);
        }

        public async override Task SpellCreationTaskHandle(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken, Message message, ReplyKeyboardMarkup replyKeyboard)
        {
            await SpellCollector.SpellCreationTaskHandle(botClient, update, cancellationToken, message);
        }


        public async override Task HandleCallbackQuery_5(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken, Message message, CallbackQuery callbackQuery)
        {
            await DataChanger.HandleCallbackQ(botClient, update, cancellationToken, message, callbackQuery);
        }
        public async override Task HandleCallbackQuery_6(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken, Message message, CallbackQuery callbackQuery)
        {
            await SpellCollector.HandleCallbackQ(botClient, update, cancellationToken, message, callbackQuery);
        }

        public async override Task CollectDataOperations(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken, Message message, ReplyKeyboardMarkup replyKeyboard)
        {
            await DataCollector.CollectData(botClient, update, cancellationToken, message, replyKeyboard);
        }


        public static async Task PayMoney(ITelegramBotClient botClient, CancellationToken cancellationToken, long userId)
        {
            var inlineKeyboard = new List<List<InlineKeyboardButton>>();

            var users = UserData.GetUsersFromDatabase();
            var callbacks = new HashSet<string>();

             
            foreach (var user in users)
            {
                string callbackData = $"pay_{user.Id}_{user.CharacterId}";
                if (!callbacks.Add(callbackData))
                    continue;

                string characterName = string.IsNullOrWhiteSpace(user.Name)
                    ? UserData.GetCharacterName(user.Id, user.CharacterId)
                    : user.Name;

                inlineKeyboard.Add(new List<InlineKeyboardButton>
                {
                    InlineKeyboardButton.WithCallbackData($"{characterName} (C{user.CharacterId})", callbackData)
                });
            }

            if (inlineKeyboard.Count == 0)
            {
                await botClient.SendTextMessageAsync(userId, "Немає доступних гравців для переказу.", cancellationToken: cancellationToken);
                return;
            }

            await botClient.SendTextMessageAsync(userId, "Кому ви хочете заплатити?", replyMarkup: new InlineKeyboardMarkup(inlineKeyboard), cancellationToken: cancellationToken);
        }

        public static async Task GetUserInCallBackDataMasterCommand(ITelegramBotClient botClient, CancellationToken cancellationToken, string callBackData, string textMessage, bool InventoryCheck = true)
        {
            var inlineKeyboard = new List<List<InlineKeyboardButton>>();

            if (InventoryCheck)
            {
                var users = UserInventory.GetUsersFromDatabase();

                if (users.Count > 0)
                {
                    foreach (var user in users)
                    {
                        if (UserData.CheckifExist(user.UserId))
                        {
                            inlineKeyboard.Add(new List<InlineKeyboardButton>
                        {
                            InlineKeyboardButton.WithCallbackData($"{UserData.DataBaseValueReader<string>(user.UserId, "Name")}", $"{callBackData}{user.UserId}")
                        });
                        }
                        else
                        {
                            await botClient.SendTextMessageAsync(masterID, $"Одного користувача було не знайдено: {user.UserId}", replyMarkup: new InlineKeyboardMarkup(inlineKeyboard), cancellationToken: cancellationToken);

                        }
                    }
                    await botClient.SendTextMessageAsync(masterID, $"{textMessage}", replyMarkup: new InlineKeyboardMarkup(inlineKeyboard), cancellationToken: cancellationToken);

                }
                else
                {
                    await botClient.SendTextMessageAsync(masterID, "Помилка: Недостатньо гравців", cancellationToken: cancellationToken);
                }
            }
            else
            {
                var users = UserData.GetUsersFromDatabase();

                if (users.Count > 0)
                {
                    bool isCharacterSelectionMode = callBackData == "xpAdd_" || callBackData == "magicXpAdd_";
                    var addedCallbacks = new HashSet<string>();

                    foreach (var user in users)
                    {
                        string callbackData;
                        string buttonText;

                        if (isCharacterSelectionMode)
                        {
                            callbackData = $"{callBackData}{user.Id}_{user.CharacterId}";
                            string characterName = string.IsNullOrWhiteSpace(user.Name)
                                ? UserData.GetCharacterName(user.Id, user.CharacterId)
                                : user.Name;
                            buttonText = $"{characterName} (ID {user.Id}, C{user.CharacterId})";
                        }
                        else
                        {
                            callbackData = $"{callBackData}{user.Id}";
                            string userName = UserData.DataBaseValueReader<string>(user.Id, "Name");
                            buttonText = string.IsNullOrWhiteSpace(userName) ? $"ID {user.Id}" : userName;
                        }

                        if (!addedCallbacks.Add(callbackData))
                            continue;

                        inlineKeyboard.Add(
                        [
                            InlineKeyboardButton.WithCallbackData(buttonText, callbackData)
                        ]);
                    }
                    await botClient.SendTextMessageAsync(masterID, $"{textMessage}", replyMarkup: new InlineKeyboardMarkup(inlineKeyboard), cancellationToken: cancellationToken);

                }
                else
                {
                    await botClient.SendTextMessageAsync(masterID, "Помилка: Недостатньо гравців", cancellationToken: cancellationToken);
                }
            }


        }

        public override void DataInitialization()
        {
            BaseRepository.DBInitialization();
        }
        public override void ClearALlInventoryDataBase()
        {
            UserInventory.DeletingAllInventoryData();
        }

        public override void ClearAllSpellDataBase()
        {
            SpellData.DeletingAllSpellData();
        }

        public override void ClearALlDataBase()
        {
            UserData.DeletingAllData();
        }

        public override void ClearAllMagicStatsDataBase()
        {
            ManaData.DeletingAllData();
        }

        public static void UpdateUserStatePay(ref Dictionary<long, long> userState, long userId, bool fromCallBack = false, long chatId = 0)
        {
            if (fromCallBack)
            {
                if (userState.ContainsKey(chatId))
                    userState[chatId] = userId;
                else
                    userState.Add(chatId, userId);
            }
            else
            {
                if (userState.ContainsKey(userId))
                    userState[userId] = 0;
                else
                    userState.Add(userId, 0);
            }
        }

        public static string ReturnCharacterModificatorNameEnglishToUkrainian(string characterModificatorName)
        {
            characterModificatorName = characterModificatorName.Trim();
            return characterModificatorName switch
            {
                "Strenght" => "Сила",
                "Agility" => "Спритність",
                "Stamina" => "Витривалість",
                "Intelligence" => "Інтелект",
                "Wisdom" => "Мудрість",
                "Charisma" => "Харизма",
                _ => characterModificatorName  
            };
        }

        public static string ReturnCharacterModificatorNameUkrainianToEnglish(string characterModificatorName)
        {
            characterModificatorName = characterModificatorName.Trim();
            return characterModificatorName switch
            {
                "Сила" => "Strenght",
                "Спритність" => "Agility",
                "Витривалість" => "Stamina",
                "Інтелект" => "Intelligence",
                "Мудрість" => "Wisdom",
                "Харизма" => "Charisma",
                _ => characterModificatorName  
            };
        }


        public class Wallet
        {
            public int Copper { get; set; }
            public int Silver { get; set; }
            public int Gold { get; set; }
            public int Platinum { get; set; }
            public int CopperOwner { get; set; }
            public int SilverOwner { get; set; }
            public int GoldOwner { get; set; }
            public int PlatinumOwner { get; set; }


            public Wallet(int copper, int silver, int gold, int platinum, int copperOwner, int silverOwner, int goldOwner, int platinumOwner)
            {
                Copper = copper;
                Silver = silver;
                Gold = gold;
                Platinum = platinum;
                CopperOwner = copperOwner;
                SilverOwner = silverOwner;
                GoldOwner = goldOwner;
                PlatinumOwner = platinumOwner;
            }
        }

    }




}
