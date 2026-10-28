IF OBJECT_ID(N'[dbo].[MonsterTemplates]', N'U') IS NOT NULL
BEGIN
    ;WITH Seed(Name, HP, Armour, XP, AttackName, AttackDamageDice) AS
    (
        SELECT N'Сторож міста', 22, 15, 50, N'Спис', N'1d8 + 2' UNION ALL
        SELECT N'Бандит', 18, 13, 50, N'Короткий меч', N'1d6 + 2' UNION ALL
        SELECT N'Лучник розбійник', 16, 12, 50, N'Лук', N'1d8 + 1' UNION ALL
        SELECT N'Вуличний громила', 30, 14, 100, N'Дворучна булава', N'2d6 + 3' UNION ALL
        SELECT N'Дикий вовк', 11, 12, 25, N'Укус', N'1d6 + 2' UNION ALL
        SELECT N'Гігантський пацюк', 7, 12, 10, N'Укус', N'1d4 + 1' UNION ALL
        SELECT N'Скелет', 13, 13, 50, N'Іржавий меч', N'1d6 + 2' UNION ALL
        SELECT N'Зомбі', 22, 8, 50, N'Гнилі кулаки', N'1d8 + 1' UNION ALL
        SELECT N'Культист', 15, 12, 50, N'Ритуальний кинджал', N'1d4 + 2' UNION ALL
        SELECT N'Навчальний манекен', 40, 10, 0, NULL, NULL UNION ALL

        SELECT N'Селянин-ополченець', 12, 10, 10, N'Вила', N'1d6 + 1' UNION ALL
        SELECT N'Міський рекрут', 16, 13, 25, N'Спис', N'1d6 + 1' UNION ALL
        SELECT N'Арбалетник варти', 20, 14, 50, N'Легкий арбалет', N'1d8 + 2' UNION ALL
        SELECT N'Сержант варти', 32, 16, 100, N'Довгий меч', N'1d8 + 3' UNION ALL
        SELECT N'Ветеран-найманець', 45, 17, 200, N'Бойова сокира', N'1d10 + 4' UNION ALL
        SELECT N'Лицар корони', 60, 18, 450, N'Лицарський меч', N'2d6 + 4' UNION ALL
        SELECT N'Паладин-інквізитор', 68, 19, 700, N'Освячений молот', N'2d8 + 5' UNION ALL
        SELECT N'Розбійник-кинджальник', 24, 14, 50, N'Пара кинджалів', N'1d4 + 1d4 + 3' UNION ALL
        SELECT N'Розбійник-дуелянт', 34, 15, 100, N'Рапіра', N'1d8 + 4' UNION ALL
        SELECT N'Отаман банди', 52, 16, 300, N'Сокира ката', N'2d6 + 4' UNION ALL
        SELECT N'Контрабандист', 22, 13, 50, N'Кастет', N'1d6 + 2' UNION ALL
        SELECT N'Мисливець', 24, 13, 50, N'Короткий лук', N'1d6 + 3' UNION ALL
        SELECT N'Слідопит фронтиру', 38, 15, 150, N'Довгий лук', N'1d8 + 4' UNION ALL
        SELECT N'Міський алхімік', 28, 12, 150, N'Кислотна колба', N'2d6 + 3' UNION ALL
        SELECT N'Асасин гільдії', 44, 16, 300, N'Отруєний клинок', N'2d6 + 4' UNION ALL
        SELECT N'Бойовий маг академії', 36, 13, 200, N'Вогняний спалах', N'2d8 + 2' UNION ALL
        SELECT N'Культист-адепт', 27, 13, 100, N'Тіньовий серп', N'1d8 + 2' UNION ALL
        SELECT N'Культист-жрець', 40, 14, 200, N'Осквернений посох', N'1d10 + 3' UNION ALL
        SELECT N'Некромант-послушник', 30, 12, 150, N'Проклятий дотик', N'2d6 + 2' UNION ALL
        SELECT N'Чемпіон арени', 55, 17, 400, N'Дворучний меч', N'2d6 + 5' UNION ALL

        SELECT N'Гоблін-розвідник', 10, 13, 25, N'Іржавий ніж', N'1d6 + 2' UNION ALL
        SELECT N'Гоблін-лучник', 9, 13, 25, N'Короткий лук', N'1d6 + 2' UNION ALL
        SELECT N'Гоблін-боєць', 15, 15, 50, N'Спис', N'1d6 + 2' UNION ALL
        SELECT N'Хобгоблін-воїн', 22, 18, 100, N'Довгий меч', N'1d8 + 2' UNION ALL
        SELECT N'Багбер', 35, 16, 200, N'Ранкова зірка', N'2d8 + 2' UNION ALL
        SELECT N'Орк-воїн', 30, 13, 100, N'Сокира', N'1d12 + 3' UNION ALL
        SELECT N'Орк-берсерк', 42, 14, 200, N'Дворучна сокира', N'2d6 + 4' UNION ALL
        SELECT N'Орк-шаман', 38, 12, 250, N'Тотемний посох', N'2d8 + 3' UNION ALL
        SELECT N'Кобольд', 8, 12, 10, N'Кинджал', N'1d4 + 2' UNION ALL
        SELECT N'Кобольд-пращник', 7, 12, 10, N'Праща', N'1d4 + 2' UNION ALL
        SELECT N'Кобольд-ватажок', 20, 14, 50, N'Короткий меч', N'1d6 + 2' UNION ALL

        SELECT N'Дикий кабан', 19, 11, 50, N'Бивні', N'2d4 + 2' UNION ALL
        SELECT N'Гієна', 12, 11, 25, N'Укус', N'1d6 + 1' UNION ALL
        SELECT N'Величезний вовк', 37, 13, 200, N'Укус', N'2d6 + 3' UNION ALL
        SELECT N'Чорний ведмідь', 34, 12, 100, N'Лапа', N'2d4 + 4' UNION ALL
        SELECT N'Бурий ведмідь', 68, 14, 450, N'Лапа', N'2d6 + 5' UNION ALL
        SELECT N'Лісовий павук', 16, 13, 50, N'Укус', N'1d8 + 2' UNION ALL
        SELECT N'Гігантський павук', 26, 14, 100, N'Отруйний укус', N'1d10 + 3' UNION ALL
        SELECT N'Печерний кажан', 9, 12, 10, N'Укус', N'1d4 + 1' UNION ALL
        SELECT N'Гірський лев', 45, 14, 200, N'Кігті', N'2d6 + 4' UNION ALL
        SELECT N'Лось скажений', 42, 13, 150, N'Роги', N'2d8 + 3' UNION ALL

        SELECT N'Скелет-лучник', 13, 13, 50, N'Короткий лук', N'1d6 + 2' UNION ALL
        SELECT N'Скелет-воїн', 22, 14, 100, N'Іржава шабля', N'1d8 + 2' UNION ALL
        SELECT N'Зомбі-воїн', 30, 8, 100, N'Гнилий меч', N'1d10 + 2' UNION ALL
        SELECT N'Вурдалак', 36, 13, 200, N'Кігті', N'2d6 + 3' UNION ALL
        SELECT N'Привид сторожі', 45, 12, 300, N'Холодний дотик', N'3d6 + 2' UNION ALL
        SELECT N'Упир', 60, 16, 700, N'Ікла', N'2d8 + 4' UNION ALL
        SELECT N'Оживлений обладунок', 33, 18, 100, N'Металевий кулак', N'1d8 + 3' UNION ALL
        SELECT N'Камяний голем-сторож', 110, 17, 1800, N'Камяний удар', N'3d10 + 5' UNION ALL

        SELECT N'Огрид-найманець', 59, 11, 450, N'Дубина', N'2d8 + 4' UNION ALL
        SELECT N'Троль мостовий', 84, 15, 700, N'Кігті', N'2d6 + 2d6 + 4' UNION ALL
        SELECT N'Гарпія', 38, 13, 200, N'Кігті', N'2d6 + 2' UNION ALL
        SELECT N'Мантикора', 68, 14, 700, N'Хвостові шипи', N'3d6 + 3' UNION ALL
        SELECT N'Мімік-скриня', 58, 12, 450, N'Укус', N'2d8 + 3' UNION ALL
        SELECT N'Болотний слиз', 45, 8, 200, N'Кислотний дотик', N'2d6 + 2' UNION ALL
        SELECT N'Елементаль вітру малий', 50, 15, 450, N'Порив', N'2d8 + 3' UNION ALL
        SELECT N'Елементаль землі малий', 65, 16, 700, N'Камяний кулак', N'2d10 + 4' UNION ALL
        SELECT N'Капітан найманців', 70, 17, 600, N'Командирський клинок', N'2d6 + 5' UNION ALL

        SELECT N'Міський пікінер', 26, 15, 75, N'Піка', N'1d10 + 2' UNION ALL
        SELECT N'Важкий арбалетник', 24, 14, 100, N'Важкий арбалет', N'1d10 + 3' UNION ALL
        SELECT N'Прапороносець гарнізону', 34, 16, 150, N'Довгий меч', N'1d8 + 3' UNION ALL
        SELECT N'Храмовий страж', 48, 17, 300, N'Священний спис', N'2d6 + 4' UNION ALL
        SELECT N'Чорний лицар', 82, 19, 900, N'Проклятий клинок', N'2d8 + 5' UNION ALL

        SELECT N'Ельф-лучник', 28, 14, 100, N'Довгий лук', N'1d8 + 4' UNION ALL
        SELECT N'Ельф-мечник', 36, 15, 150, N'Вигнутий меч', N'1d8 + 4' UNION ALL
        SELECT N'Дворф-захисник', 46, 18, 250, N'Бойовий молот', N'1d10 + 4' UNION ALL
        SELECT N'Дворф-берсерк', 52, 16, 300, N'Сокира предків', N'2d6 + 4' UNION ALL
        SELECT N'Гном-алхімік', 24, 13, 125, N'Вибухова колба', N'2d6 + 2' UNION ALL

        SELECT N'Лісова відьма', 33, 12, 200, N'Терновий батіг', N'2d8 + 2' UNION ALL
        SELECT N'Жрець світла', 42, 15, 300, N'Сонячний імпульс', N'2d10 + 2' UNION ALL
        SELECT N'Темний аколіт', 29, 13, 150, N'Тіньовий промінь', N'2d6 + 3' UNION ALL
        SELECT N'Вогняний культист', 32, 13, 175, N'Вогняний клинок', N'2d8 + 2' UNION ALL
        SELECT N'Грозовий шаман', 44, 14, 325, N'Грім-удар', N'2d10 + 3' UNION ALL

        SELECT N'Демонічний гончак', 40, 14, 250, N'Палаючий укус', N'2d6 + 4' UNION ALL
        SELECT N'Пекельний імп', 18, 13, 75, N'Жало', N'1d6 + 3' UNION ALL
        SELECT N'Крижаний мефіт', 22, 12, 100, N'Крижаний подих', N'2d6 + 2' UNION ALL
        SELECT N'Саламандра мала', 36, 14, 200, N'Палаючий спис', N'1d10 + 4' UNION ALL
        SELECT N'Залізний автоматон', 88, 18, 950, N'Механічний удар', N'3d8 + 4' UNION ALL

        SELECT N'Велетенська ящірка', 34, 13, 150, N'Укус', N'2d6 + 3' UNION ALL
        SELECT N'Тінь', 26, 14, 150, N'Тіньовий дотик', N'2d6 + 3' UNION ALL
        SELECT N'Примарний вершник', 58, 16, 500, N'Ефірний спис', N'2d8 + 4' UNION ALL
        SELECT N'Вампірський слуга', 44, 15, 300, N'Кігті', N'2d6 + 4' UNION ALL
        SELECT N'Ліч-послушник', 52, 14, 550, N'Некротичний промінь', N'3d6 + 4' UNION ALL

        SELECT N'Пірат-головоріз', 31, 14, 100, N'Абордажна шабля', N'1d8 + 3' UNION ALL
        SELECT N'Пірат-гарпунер', 29, 13, 100, N'Гарпун', N'1d10 + 2' UNION ALL
        SELECT N'Капітан піратів', 62, 17, 500, N'Пістоль і шабля', N'1d10 + 1d8 + 4' UNION ALL
        SELECT N'Морський мародер', 48, 15, 250, N'Якірний молот', N'2d6 + 4' UNION ALL
        SELECT N'Пустельний рейдер', 35, 14, 150, N'Крива шабля', N'1d8 + 4' UNION ALL

        SELECT N'Скорпіон гігантський', 52, 15, 300, N'Клешні і жало', N'2d6 + 1d8 + 3' UNION ALL
        SELECT N'Піщаний червяк малий', 66, 13, 450, N'Розривний укус', N'3d6 + 4' UNION ALL
        SELECT N'Болотний людожер', 72, 14, 500, N'Болотна палиця', N'2d10 + 4' UNION ALL
        SELECT N'Древній ент малий', 95, 16, 900, N'Удар коренем', N'3d8 + 5' UNION ALL
        SELECT N'Лісовий дріад', 30, 14, 175, N'Шиповий батіг', N'2d6 + 3' UNION ALL

        SELECT N'Крижаник', 74, 16, 700, N'Крижаний кулак', N'2d10 + 4' UNION ALL
        SELECT N'Гірський велетень молодий', 120, 15, 1800, N'Камяна брила', N'3d10 + 6' UNION ALL
        SELECT N'Рунний страж', 98, 18, 1200, N'Рунний клинок', N'3d8 + 5' UNION ALL
        SELECT N'Кобольд сапер', 11, 12, 25, N'Саморобна бомба', N'2d4 + 2' UNION ALL
        SELECT N'Гоблін підривник', 14, 13, 50, N'Порохова шашка', N'2d6 + 2' UNION ALL

        SELECT N'Грифон', 75, 15, 700, N'Кігті та дзьоб', N'2d6 + 1d8 + 4'
    )
    INSERT INTO MonsterTemplates (Name, HP, Armour, XP, AttackName, AttackDamageDice, CreatedBy)
    SELECT s.Name, s.HP, s.Armour, s.XP, s.AttackName, s.AttackDamageDice, 0
    FROM Seed s
    WHERE NOT EXISTS
    (
        SELECT 1
        FROM MonsterTemplates t
        WHERE t.Name = s.Name
    );
END;
