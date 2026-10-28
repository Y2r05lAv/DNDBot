using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace DNDBot.CommandSystem
{
    class SubTableCallBackCommands : BotBodyRealisation
    {

        private static readonly string IndividualPath = @"D:\DNDBot\DNDBot\DNDBot\Photos/individual.jpg";
        private static readonly string RarePhoto = @"D:\DNDBot\DNDBot\DNDBot\Photos/rare.jpg";
        private static readonly string Treasure04Photo = @"D:\DNDBot\DNDBot\DNDBot\Photos/treasure04.jpg";
        private static readonly string Treasure510Photo = @"D:\DNDBot\DNDBot\DNDBot\Photos/treasure510.jpg";
        private static readonly string MagicItemAPhoto = @"D:\DNDBot\DNDBot\DNDBot\Photos/magicItemA.jpg";
        private static readonly string MagicItemBPhoto = @"D:\DNDBot\DNDBot\DNDBot\Photos/magicItemB.jpg";
        private static readonly string MagicItemVPhoto = @"D:\DNDBot\DNDBot\DNDBot\Photos/magicItemV.jpg";
        private static readonly string MagicItemEPhoto = @"D:\DNDBot\DNDBot\DNDBot\Photos/magicItemE.jpg";
        private static readonly string MagicItemE2Photo = @"D:\DNDBot\DNDBot\DNDBot\Photos/magicItemE2.jpg";

        public static async Task TableCallBackCommands(ITelegramBotClient botClient, CancellationToken cancellationToken, long chatId, string infoButton)
        {
            switch (infoButton)
            {
                case "individual":
                     
                    await botClient.SendTextMessageAsync(chatId, "Індівідуальні скарби: Показники небезпечності:", cancellationToken: cancellationToken);
                    await ReturnPhotos(botClient, cancellationToken, chatId, $"individual.jpg", IndividualPath);
                    break;
                case "rare":
                    await botClient.SendTextMessageAsync(chatId, "Рідкість магічних предметів:", cancellationToken: cancellationToken);
                    await ReturnPhotos(botClient, cancellationToken, chatId, $"rare.jpg", RarePhoto);
                    break;
                case "treasure04":
                    await botClient.SendTextMessageAsync(chatId, "Скарбниця: Показник небезпечності 0-4", cancellationToken: cancellationToken);
                    await ReturnPhotos(botClient, cancellationToken, chatId, $"treasure04.jpg", Treasure04Photo);
                    break;
                case "treasure510":
                    await botClient.SendTextMessageAsync(chatId, "Скарбниця: Показник небезпечності 5-10", cancellationToken: cancellationToken);

                    await ReturnPhotos(botClient, cancellationToken, chatId, $"treasure510.jpg", Treasure510Photo);
                    break;
                case "magicItemA":
                    await botClient.SendTextMessageAsync(chatId, "Таблиця магічних предметів А", cancellationToken: cancellationToken);

                    await ReturnPhotos(botClient, cancellationToken, chatId, $"magicItemA.jpg", MagicItemAPhoto);

                    break;
                case "magicItemB":
                    await botClient.SendTextMessageAsync(chatId, "Таблиця магічних предметів Б", cancellationToken: cancellationToken);

                    await ReturnPhotos(botClient, cancellationToken, chatId, $"magicItemB.jpg", MagicItemBPhoto);
                    break;
                case "magicItemV":
                    await botClient.SendTextMessageAsync(chatId, "Таблиця магічних предметів В", cancellationToken: cancellationToken);

                    await ReturnPhotos(botClient, cancellationToken, chatId, $"magicItemV.jpg", MagicItemVPhoto);
                    break;
                case "magicItemE":
                    await botClient.SendTextMessageAsync(chatId, "Таблиця магічних предметів E", cancellationToken: cancellationToken);

                    await ReturnPhotos(botClient, cancellationToken, chatId, $"magicItemE.jpg", MagicItemEPhoto);
                    await ReturnPhotos(botClient, cancellationToken, chatId, $"magicItemE2.jpg", MagicItemE2Photo);

                    break;
            }
        }

        private static async Task ReturnPhotos(ITelegramBotClient botClient, CancellationToken cancellationToken, long chatId, string photoPath, string path)
        {
            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read))
            {
                InputFileStream inputFile = new InputFileStream(stream, $"{photoPath}");

                await botClient.SendPhotoAsync(chatId, photo: inputFile);
            }
        }
    }
}
