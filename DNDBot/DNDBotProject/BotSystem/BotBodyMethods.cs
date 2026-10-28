using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace DNDBot
{
    public abstract class BotBodyMethods
    {
        public abstract void ClassInitialization();
        public abstract Task WeaponOperations(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken, Message message, ReplyKeyboardMarkup replyKeyboard);
        public abstract Task CollectDataOperations(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken, Message message, ReplyKeyboardMarkup replyKeyboard);
        public abstract Task SpellCreationTaskHandle(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken, Message message, ReplyKeyboardMarkup replyKeyboard);

        public abstract void DataInitialization();
        public abstract void ClearALlDataBase();
        public abstract void ClearAllMagicStatsDataBase();
        public abstract void ClearALlInventoryDataBase();
        public abstract void ClearAllSpellDataBase();
        public abstract Task HandleCallbackQuery_5(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken, Message message, CallbackQuery callbackQuery);
        public abstract Task HandleCallbackQuery_6(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken, Message message, CallbackQuery callbackQuery);

        public abstract void RemoveMonsterFromAllCollections(long monsterKey);

         

        public abstract string GetCollectMultiplayers(long userId);

        public abstract ReplyKeyboardMarkup ButtonsCreate(long userID, bool MonsterCreation, bool OneTimeKeyboard = true);
        public abstract ReplyKeyboardMarkup MiniButtonsCreate(bool OneTimeKeyboard = false);
    }
}
