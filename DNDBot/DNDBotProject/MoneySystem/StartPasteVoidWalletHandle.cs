using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot.Types;
using Telegram.Bot;
using Microsoft.Extensions.Logging;

namespace DNDBot.MoneySystem
{
    class StartPasteVoidWalletHandle : BotBodyRealisation
    {
        private readonly ILogger<StartPasteVoidWalletHandle> _logger;
        private readonly MoneyTransfer _moneyTransfer;
        private readonly WalletSubCommandsHandle _walletSubCommandsHandle;
        private readonly WalletSwitchHandle _walletSwitchHandle;

        public StartPasteVoidWalletHandle(ILogger<StartPasteVoidWalletHandle> logger, MoneyTransfer moneyTransfer,
            WalletSubCommandsHandle walletSubCommandsHandle, WalletSwitchHandle walletSwitchHandle)
        {
            _logger = logger;
            _moneyTransfer = moneyTransfer;
            _walletSubCommandsHandle = walletSubCommandsHandle;
            _walletSwitchHandle = walletSwitchHandle;
        }

       
    }
}
