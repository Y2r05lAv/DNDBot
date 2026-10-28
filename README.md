# DNDBot

Україномовний Telegram-бот для Dungeons & Dragons і WPF-застосунок для його запуску та моніторингу.

## Що є в проєкті

- **Telegram-бот** на C# і .NET 9 з командами для гри: персонажі, інвентар, закляття, монстри та бойові механіки.
- **Системи держави й економіки** з країнами, законами, дослідженнями, дипломатією та ресурсами.
- **WPF-застосунок** на .NET Framework 4.8 для запуску й зупинки бота, перегляду його виводу та стану процесу.
- **SQL Server** для збереження ігрових даних.

## Структура

- **DNDBot/** — solution і код Telegram-бота, а також SQL-скрипти схеми.
- **DNDBot_GUI/DNDBot/** — окремий WPF solution для desktop-застосунку.

GUI запускає бота як окремий процес і шукає його виконуваний файл у папці **Public/DNDBot.exe** поруч із собою. Готові збірки навмисно не зберігаються в репозиторії.

## Технології

- C#, .NET 9
- WPF, .NET Framework 4.8
- Telegram.Bot
- SQL Server / LocalDB

## Локальний запуск

Потрібні Windows, .NET 9 SDK, SQL Server LocalDB і Visual Studio з компонентами для .NET Framework 4.8 та WPF.

1. Скопіюй **DNDBot/DNDBotProject/App.config.example** у **DNDBot/DNDBotProject/App.config**.
2. У **App.config** вкажи токен Telegram-бота, свій Telegram ID для **MasterId** і **BotOwner**, а також рядок підключення до локальної SQL Server бази. Для LocalDB використовуй приклад із **App.config.example** без `AttachDbFilename`.
3. Під час першого запуску LocalDB автоматично створюється база **DNDBot**, а бот застосовує SQL-скрипти з папки **DNDBot**. Якщо в конфігурації вказаний шлях до MDF-файлу, якого немає на цьому комп'ютері, бот також перейде на нову базу LocalDB. Скрипт **dbo.Countries.sql** створює початкову таблицю країн; інші скрипти створюють і оновлюють решту таблиць. На чистій базі ігрові дані будуть порожніми — країни та персонажів потрібно додати через бота.
4. Збери бота командою **dotnet build .\DNDBot\DNDBot.sln -c Release**.
5. Для desktop-застосунку відкрий **DNDBot_GUI/DNDBot/DNDBot.sln** у Visual Studio та збери конфігурацію **Release**.
6. Щоб запускати бота через GUI, розмісти опубліковані файли бота в папці **Public** поруч із зібраним GUI-файлом. Головний файл має називатися **DNDBot.exe**.

**App.config**, токен, локальна база й опубліковані файли не входять до GitHub-репозиторію.

## Мій внесок

Я розвивав Telegram-бота й desktop-застосунок. Частину систем державного управління та економіки створював ШІ; я не приписую собі повне авторство цих частин. Я реалізовував логіку для таких систем як інвентар, атака, створювання монстрів та ін. Для більш оптимального виконання пізніше деякі фрагменти коду були оновлено/оптимізовано за допомоги ШІ та документацій та переглянуті відповідно. 

## Скріншоти
<img width="496" height="574" alt="image" src="https://github.com/user-attachments/assets/ed2d18d6-0ac4-473c-bfbf-462139bb53f7" />
<img width="509" height="767" alt="image" src="https://github.com/user-attachments/assets/b5c325b4-0080-4b59-8d5e-4bd750fbc6c0" />
<img width="522" height="796" alt="image" src="https://github.com/user-attachments/assets/4c357d97-50c0-442f-9500-b5d6f19d8be8" />
<img width="519" height="782" alt="image" src="https://github.com/user-attachments/assets/c4556e5b-55ec-40a0-9314-feef96920fbf" />
