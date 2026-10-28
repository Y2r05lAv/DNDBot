using DNDBot.BotMath;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot.Types;
using Telegram.Bot;
using Microsoft.Extensions.Logging;

namespace DNDBot.CommandSystem
{
    class SubUserComandsRealisation : BotBodyRealisation
    {
        private readonly ILogger<SubUserComandsRealisation> _subLogger;

        public SubUserComandsRealisation(ILogger<SubUserComandsRealisation> logger)
        {
            _subLogger = logger;
        }

        public async Task XpAdd(ITelegramBotClient botClient, CancellationToken cancellationToken, long userId)  
        {

            LevelSystem.AddToAllUsers = false;  



            if (userId != masterID)
            {
                if (!await CheckUserExists(botClient, userId, cancellationToken)) return;  

                OldMessages[userId].Add((await botClient.SendTextMessageAsync(userId, $"Скільки досвіду ви хочете додати?", cancellationToken: cancellationToken)).MessageId);
                _subLogger.LogInformation($"User with {userId} id started XpAdd method.");

                if (!userStateXP.TryAdd(userId, "start"))
                    userStateXP[userId] = "start";

                userStateMagicXP[userId] = "";
            }
            else
            {
                await GetUserInCallBackDataMasterCommand(botClient, cancellationToken, "xpAdd_", "Кому ви хочете додати досвід?", false);
                _subLogger.LogInformation($"Master with {userId} started XpAdd to users");
            }
        }


        public async Task MagicXpAdd(ITelegramBotClient botClient, CancellationToken cancellationToken, long userId)  
        {

            LevelSystem.AddToAllUsers = false;  



            if (userId != masterID)
            {
                if (!await CheckUserExists(botClient, userId, cancellationToken)) return;  

                OldMessages[userId].Add((await botClient.SendTextMessageAsync(userId, $"Скільки магічного досвіду ви хочете додати?", cancellationToken: cancellationToken)).MessageId);
                _subLogger.LogInformation($"User with {userId} id started Magic XpAdd method.");

                if (!userStateMagicXP.TryAdd(userId, "start"))
                    userStateMagicXP[userId] = "start";

                userStateXP[userId] = "";
            }
            else
            {
                await GetUserInCallBackDataMasterCommand(botClient, cancellationToken, "magicXpAdd_", "Кому ви хочете додати магічний досвід?", false);
                _subLogger.LogInformation($"Master with {userId} started Magic XpAdd to users");
            }
        }

        public async Task GetUserInfo(ITelegramBotClient botClient, Message message, long userId, BotBodyRealisation botBodyRealisation, CancellationToken cancellationToken)
        {
             
            long targetChatId = userId;


            if (userId != masterID)
            {
                if (!await CheckUserExists(botClient, userId, cancellationToken, targetChatId)) return;  

                await botClient.SendTextMessageAsync(targetChatId, $"Ваші модифікатори та статистика: \n\n" + botBodyRealisation.GetCollectMultiplayers(userId), cancellationToken: cancellationToken); _subLogger.LogInformation($"Trying to get info about {userId} user.");
                _subLogger.LogInformation($"User {userId} got information: Task Successful.");
            }
            else
            {
                _subLogger.LogInformation($"User {userId} got all info about all users.");
                await botClient.SendTextMessageAsync(targetChatId, $"Всі модифікатори та статистика гравців: \n\n" + UserData.GetAllUsers(), cancellationToken: cancellationToken);
            }
        }





        #pragma warning disable CS0108
        public new async Task Wallet(ITelegramBotClient botClient, CancellationToken cancellationToken, long userId)
        #pragma warning restore CS0108
        {
            _subLogger.LogInformation($"Trying to get info about {userId} wallet.");

            DeleteAllOldMessages(botClient, cancellationToken, userId);
            if (userId != masterID)
            {
                if (!await CheckUserExists(botClient, userId, cancellationToken)) return;  

                await botClient.SendTextMessageAsync(userId, $"Гаманець:\n\n" + UserData.DataBaseWalletReader(userId), cancellationToken: cancellationToken);
                _subLogger.LogInformation($"User {userId} got wallet information: Task Successful.");
            }
            else
            {
                var users = UserData.GetUsersFromDatabase();
                foreach (var user in users)
                {
                    if (user.Id == masterID)
                        continue;

                    await botClient.SendTextMessageAsync(masterID, $"Гаманець гравця {user.Name}:\n\n" + UserData.DataBaseWalletReader(user.Id), cancellationToken: cancellationToken);

                }
                _subLogger.LogInformation($"Master {masterID} got all users wallets.");
            }
        }

        public string ReturnPossibleChecks()  
        {
            string possibleStringCheckList = string.Join(", ", PossibleChecks);

            _subLogger.LogInformation($"All possible checks was returned to users.");
            return possibleStringCheckList;
        }

        public async Task CharacterizationCheck(ITelegramBotClient botClient, CancellationToken cancellationToken, Message msg, List<string> CheckList, string CheckingVariableString)
        {
            if (msg?.From == null)
                return;

            long userId = msg.From.Id;

            try
            {
                if (!await CheckUserExists(botClient, userId, cancellationToken)) return;  

                int Multiplier = UserData.DataBaseValueReader<int>(userId, ReturnCharacterModificatorNameUkrainianToEnglish(CheckingVariableString.Trim()));
                int CheckDice = MathfDND.DiceReturn(20);

                await botClient.SendTextMessageAsync(msg.Chat.Id, $"Модифікатор - {Multiplier} \nНа кубіку - {CheckDice} \nПеревірка '{CheckList[0]}' - {CheckDice + Multiplier}", cancellationToken: cancellationToken);
                _subLogger.LogInformation($"User with {userId} got CharacterizationCheck: Task Successful");
            }
            catch (Exception ex)
            {
                _subLogger.LogError(ex, $"Error in CharacterizationCheck for user {userId}");
                await botClient.SendTextMessageAsync(msg.Chat.Id, $"Сталася помилка під час перевірки. Спробуйте ще раз.", cancellationToken: cancellationToken);
            }
        }


        private async Task<bool> CheckUserExists(ITelegramBotClient botClient, long userId, CancellationToken cancellationToken, long? chatId = null)
        {
            if (!UserData.CheckifExist(userId))
            {
                await botClient.SendTextMessageAsync(chatId ?? userId, $"Ваших даних немає.", cancellationToken: cancellationToken);
                _subLogger.LogWarning($"User {userId} does not exist in the database.");
                return false;
            }
            return true;
        }
    }
}
