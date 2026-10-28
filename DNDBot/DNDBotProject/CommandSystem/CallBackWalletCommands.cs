using DNDBot.BotSystem;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace DNDBot.CommandSystem
{
    class CallBackWalletCommands : BotBodyRealisation
    {
        public readonly ILogger<CallBackWalletCommands> _logger;
        public readonly SubUserComandsRealisation _subUserComandsRealisation;

        public CallBackWalletCommands(ILogger<CallBackWalletCommands> logger, SubCommandsPlayerBattle subCommandsPlayerBattle, SubUserComandsRealisation subUserComandsRealisation)
        {
            _logger = logger;
            _subUserComandsRealisation = subUserComandsRealisation;
        }

        public async Task WalletCallBackCommands(ITelegramBotClient botClient, CancellationToken cancellationToken, long chatId, string infoButton)
        {
            _logger.LogInformation("Starting wallet callback commands for user {UserId}", chatId);

            try
            {
                switch (infoButton)
                {
                    case "checkWallet":
                        _logger.LogInformation("User {UserId} requested wallet check", chatId);
                        await _subUserComandsRealisation.Wallet(botClient, cancellationToken, chatId);
                        break;
                    case "add":
                        _logger.LogInformation("User {UserId} requested to add coins", chatId);
                        await InlineKeyboardsClass.InlineButtonsCreation(botClient, cancellationToken, chatId, "add_", "Яку монету ви хочете додати?");
                        break;
                    case "min":
                        _logger.LogInformation("User {UserId} requested to subtract coins", chatId);
                        await InlineKeyboardsClass.InlineButtonsCreation(botClient, cancellationToken, chatId, "min_", "Яку монету ви хочете відняти?");
                        break;
                    case "pay":
                        _logger.LogInformation("User {UserId} initiated payment", chatId);
                        await PayMoney(botClient, cancellationToken, chatId);
                        break;
                    case "convert":
                        _logger.LogInformation("User {UserId} requested currency conversion", chatId);
                        await InlineKeyboardsClass.InlineButtonsCreation(botClient, cancellationToken, chatId, "transfer_", "Виберіть валюту, котру хочете отримати з інших ваших монет:");
                        break;
                    case "uitgeven":
                        _logger.LogInformation("User {UserId} requested money spending", chatId);
                        await InlineKeyboardsClass.MoneyAfNemenForConsument(botClient, cancellationToken, chatId);
                        break;
                    case "AddToAll":
                        _logger.LogInformation("Master {MasterId} is adding coins to all players", chatId);
                        await InlineKeyboardsClass.InlineButtonsCreation(botClient, cancellationToken, masterID, "payToAll_", "Виберіть монету, котру хочете додати до всіх гравців:");
                        break;
                    case "fine":
                        _logger.LogInformation("Master {MasterId} is fining all players", chatId);
                        await InlineKeyboardsClass.InlineButtonsCreation(botClient, cancellationToken, masterID, "fine_", "Виберіть монету, на яку ви хочете оштрафувати гравців?");
                        break;
                    default:
                        _logger.LogWarning("Unknown wallet command {Command} from user {UserId}", infoButton, chatId);
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing wallet command {Command} for user {UserId}", infoButton, chatId);
                await botClient.SendTextMessageAsync(chatId, "Сталася помилка під час обробки команди. Спробуйте ще раз.", cancellationToken: cancellationToken);
            }
        }
    }
}
