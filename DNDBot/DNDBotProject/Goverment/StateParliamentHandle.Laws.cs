using DNDBot.DataBase;
using DNDBot.Goverment.Economy;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace DNDBot.Goverment
{
    public partial class StateParliamentHandle
    {
        private const int LawStepTitle = 0;
        private const int LawStepDescription = 1;
        private const int LawStepParent = 2;
        private const int LawStepEffectType = 3;
        private const int LawStepEffectValue = 4;
        private const int LawStepEffectScope = 5;
        private const int LawStepExpiration = 6;
        private const int LawStepRenameName = 30;
        private const string LawStatusReviewFirst = "Review1";
        private const string LawStatusReviewSecond = "Review2";
        private const string LawStatusProposed = "Proposed";
        private const string LawStatusAccepted = "Accepted";
        private const string LawStatusRejected = "Rejected";
        private const string LawStatusRevoked = "Revoked";
        private const string RenameCountryEffectType = "RenameCountry";
        private const string RenameCountryDescriptionPrefix = "[[RENAME_TO]]";
        private const string SetSoldierSalaryEffectType = "SetSoldierSalary";
        private const string SetDeputySalaryEffectType = "SetDeputySalary";
        private const string SetCivilianSalaryEffectType = "SetCivilianSalary";
        private const string SetPensionPaymentEffectType = "SetPensionPayment";
        private const string SetSocialSupportPaymentEffectType = "SetSocialSupportPayment";
        private const string AddTreasuryEffectType = "AddTreasury";
        private const string RemoveTreasuryEffectType = "RemoveTreasury";
        private const string AddTreasuryDailyEffectType = "AddTreasuryDaily";
        private const string RemoveTreasuryDailyEffectType = "RemoveTreasuryDaily";
        private const string LimitImportEffectType = "LimitImportPercent";
        private const string LimitExportEffectType = "LimitExportPercent";
        private const string LimitCompanyResourceUseEffectType = "LimitCompanyResourceUsePercent";
        private const string SocialContributionEffectType = "SocialContributionPercent";
        private const string GrossIncomeTaxEffectType = "GrossIncomeTaxPercent";
        private const string NetIncomeTaxEffectType = "NetIncomeTaxPercent";
        private const string NoEffectType = "None";

        private static readonly Dictionary<long, int> LawCreationStepByUser = new();
        private static readonly Dictionary<long, LawRecord> LawCreationBufferByUser = new();
        private static readonly Dictionary<long, int> LawCreationCountryByUser = new();

        private sealed class ReadyLawTemplate
        {
            public int TemplateId { get; set; }
            public int Tier { get; set; }
            public string Title { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
            public string EffectType { get; set; } = string.Empty;
            public decimal? FixedEffectValue { get; set; }
            public string ConfigKey { get; set; } = string.Empty;
            public decimal ConfigDefault { get; set; }
            public decimal Multiplier { get; set; }
        }

        private static readonly List<ReadyLawTemplate> ReadyLawTemplates = new()
        {
            new ReadyLawTemplate
            {
                TemplateId = 1,
                Tier = 1,
                Title = "Ринковий статут міст і сіл",
                Description = "Уніфікувати правила торгівлі, ваги та міри у місцевих громадах.",
                EffectType = NoEffectType,
                FixedEffectValue = 0m
            },
            new ReadyLawTemplate
            {
                TemplateId = 2,
                Tier = 1,
                Title = "Закон про гільдії ремісників",
                Description = "Визнати ремісничі гільдії і затвердити правила навчання майстрів.",
                EffectType = NoEffectType,
                FixedEffectValue = 0m
            },
            new ReadyLawTemplate
            {
                TemplateId = 3,
                Tier = 1,
                Title = "Підтримка цивільної зарплати (+6%)",
                Description = "Підтримати зайнятість і купівельну спроможність населення.",
                EffectType = SetCivilianSalaryEffectType,
                ConfigKey = "CompanyBaseSalary",
                ConfigDefault = 1.0m,
                Multiplier = 1.06m
            },
            new ReadyLawTemplate
            {
                TemplateId = 4,
                Tier = 1,
                Title = "Надбавка міській варті (+7%)",
                Description = "Посилити патрулювання міст і торгових трактів.",
                EffectType = SetSoldierSalaryEffectType,
                ConfigKey = "SoldierSalaryPerTurn",
                ConfigDefault = 1.0m,
                Multiplier = 1.07m
            },
            new ReadyLawTemplate
            {
                TemplateId = 5,
                Tier = 1,
                Title = "Оптимізація зарплати депутатів (-3%)",
                Description = "Зменшити базові виплати депутатам для економії бюджетних коштів.",
                EffectType = SetDeputySalaryEffectType,
                ConfigKey = "DeputySalaryBase",
                ConfigDefault = 1.0m,
                Multiplier = 0.97m
            },
            new ReadyLawTemplate
            {
                TemplateId = 6,
                Tier = 1,
                Title = "Фонд нічної варти (витрата 18 000)",
                Description = "Закупівля ліхтарів, сирен та спорядження для нічних патрулів.",
                EffectType = RemoveTreasuryEffectType,
                FixedEffectValue = 18000m
            },
            new ReadyLawTemplate
            {
                TemplateId = 7,
                Tier = 1,
                Title = "Митний збір на мости (+25 000)",
                Description = "Ввести помірний збір з караванів на ремонт мостів і переправ.",
                EffectType = AddTreasuryEffectType,
                FixedEffectValue = 25000m
            },
            new ReadyLawTemplate
            {
                TemplateId = 8,
                Tier = 1,
                Title = "Запас зерна у коморах (витрата 22 000)",
                Description = "Створити місцеві продовольчі запаси на випадок неврожаю.",
                EffectType = RemoveTreasuryEffectType,
                FixedEffectValue = 22000m
            },
            new ReadyLawTemplate
            {
                TemplateId = 9,
                Tier = 1,
                Title = "Ліцензування цілителів і травників",
                Description = "Впровадити нагляд за аптекарями, алхіміками і цілителями.",
                EffectType = NoEffectType,
                FixedEffectValue = 0m
            },
            new ReadyLawTemplate
            {
                TemplateId = 10,
                Tier = 1,
                Title = "Караванні дозволи (+18 000)",
                Description = "Плата за державні дозволи для великих караванів.",
                EffectType = AddTreasuryEffectType,
                FixedEffectValue = 18000m
            },
            new ReadyLawTemplate
            {
                TemplateId = 11,
                Tier = 1,
                Title = "Кодекс магічної безпеки",
                Description = "Визначити правила зберігання, транспорту і продажу артефактів.",
                EffectType = NoEffectType,
                FixedEffectValue = 0m
            },
            new ReadyLawTemplate
            {
                TemplateId = 12,
                Tier = 1,
                Title = "Амністія дрібних податкових боргів",
                Description = "Разова амністія для дрібних ремісників за борги до кінця сезону.",
                EffectType = NoEffectType,
                FixedEffectValue = 0m
            },
            new ReadyLawTemplate
            {
                TemplateId = 13,
                Tier = 2,
                Title = "Створення Королівського бюро розслідувань",
                Description = "Аналог державного слідчого бюро: боротьба з контрабандою і зрадою.",
                EffectType = RemoveTreasuryEffectType,
                FixedEffectValue = 45000m
            },
            new ReadyLawTemplate
            {
                TemplateId = 14,
                Tier = 2,
                Title = "Канцелярія внутрішньої безпеки",
                Description = "Центральний орган контррозвідки та перевірки державних посадовців.",
                EffectType = RemoveTreasuryEffectType,
                FixedEffectValue = 50000m
            },
            new ReadyLawTemplate
            {
                TemplateId = 15,
                Tier = 2,
                Title = "Управління зовнішньої розвідки",
                Description = "Мережа агентури у сусідніх королівствах та вільних містах.",
                EffectType = RemoveTreasuryEffectType,
                FixedEffectValue = 52000m
            },
            new ReadyLawTemplate
            {
                TemplateId = 16,
                Tier = 2,
                Title = "Реформа королівської гвардії (+12%)",
                Description = "Підвищити виплати гвардії для професіоналізації служби.",
                EffectType = SetSoldierSalaryEffectType,
                ConfigKey = "SoldierSalaryPerTurn",
                ConfigDefault = 1.0m,
                Multiplier = 1.12m
            },
            new ReadyLawTemplate
            {
                TemplateId = 17,
                Tier = 2,
                Title = "Державний перепис і кадастр",
                Description = "Системний облік населення, землі, врожаю і військового резерву.",
                EffectType = RemoveTreasuryEffectType,
                FixedEffectValue = 26000m
            },
            new ReadyLawTemplate
            {
                TemplateId = 18,
                Tier = 2,
                Title = "Податкова палата монетного двору (+45 000)",
                Description = "Посилити контроль карбування монети та митних платежів.",
                EffectType = AddTreasuryEffectType,
                FixedEffectValue = 45000m
            },
            new ReadyLawTemplate
            {
                TemplateId = 19,
                Tier = 2,
                Title = "Державний інфраструктурний збір (+60 000)",
                Description = "Збір на дороги, переправи, порти і укріплені тракти.",
                EffectType = AddTreasuryEffectType,
                FixedEffectValue = 60000m
            },
            new ReadyLawTemplate
            {
                TemplateId = 20,
                Tier = 2,
                Title = "Антикорупційний пакет для депутатів (-10%)",
                Description = "Скоротити базові виплати депутатам і посилити звітність.",
                EffectType = SetDeputySalaryEffectType,
                ConfigKey = "DeputySalaryBase",
                ConfigDefault = 1.0m,
                Multiplier = 0.90m
            },
            new ReadyLawTemplate
            {
                TemplateId = 21,
                Tier = 2,
                Title = "Резерв цивільної оборони (витрата 28 000)",
                Description = "Створити запас спорядження для пожеж, повеней та облог.",
                EffectType = RemoveTreasuryEffectType,
                FixedEffectValue = 28000m
            },
            new ReadyLawTemplate
            {
                TemplateId = 22,
                Tier = 2,
                Title = "Фонд надзвичайного харчування (витрата 40 000)",
                Description = "Центральний запас продовольства для кризових сезонів.",
                EffectType = RemoveTreasuryEffectType,
                FixedEffectValue = 40000m
            },
            new ReadyLawTemplate
            {
                TemplateId = 23,
                Tier = 2,
                Title = "Закон про магічний ліцензійний реєстр",
                Description = "Єдиний реєстр магів, артефакторів і бойових заклиначів.",
                EffectType = NoEffectType,
                FixedEffectValue = 0m
            },
            new ReadyLawTemplate
            {
                TemplateId = 24,
                Tier = 2,
                Title = "Хартія провінційних губернаторів",
                Description = "Регламент взаємодії губернаторів з парламентом і казною.",
                EffectType = NoEffectType,
                FixedEffectValue = 0m
            },
            new ReadyLawTemplate
            {
                TemplateId = 25,
                Tier = 3,
                Title = "Лінія прикордонних фортець (витрата 90 000)",
                Description = "Масштабне будівництво фортифікацій на небезпечних рубежах.",
                EffectType = RemoveTreasuryEffectType,
                FixedEffectValue = 90000m
            },
            new ReadyLawTemplate
            {
                TemplateId = 26,
                Tier = 3,
                Title = "Управління воєнної логістики (витрата 70 000)",
                Description = "Стандарти постачання для армії та обозів.",
                EffectType = RemoveTreasuryEffectType,
                FixedEffectValue = 70000m
            },
            new ReadyLawTemplate
            {
                TemplateId = 27,
                Tier = 3,
                Title = "Воєнна надбавка військовим (+18%)",
                Description = "Підвищені виплати під час загрози вторгнення.",
                EffectType = SetSoldierSalaryEffectType,
                ConfigKey = "SoldierSalaryPerTurn",
                ConfigDefault = 1.0m,
                Multiplier = 1.18m
            },
            new ReadyLawTemplate
            {
                TemplateId = 28,
                Tier = 3,
                Title = "Реєстр мобілізаційного резерву",
                Description = "Облік усіх придатних до служби та графік зборів.",
                EffectType = NoEffectType,
                FixedEffectValue = 0m
            },
            new ReadyLawTemplate
            {
                TemplateId = 29,
                Tier = 3,
                Title = "Субсидії військовим мануфактурам (витрата 85 000)",
                Description = "Підтримка кузень, балістичних майстерень і верфей.",
                EffectType = RemoveTreasuryEffectType,
                FixedEffectValue = 85000m
            },
            new ReadyLawTemplate
            {
                TemplateId = 30,
                Tier = 3,
                Title = "Берегова сторожа і річкові патрулі (витрата 65 000)",
                Description = "Захист портів, переправ та торгівельних водних шляхів.",
                EffectType = RemoveTreasuryEffectType,
                FixedEffectValue = 65000m
            },
            new ReadyLawTemplate
            {
                TemplateId = 31,
                Tier = 3,
                Title = "Воєнний податок на імпорт (+120 000)",
                Description = "Тимчасовий податок для фінансування оборони держави.",
                EffectType = AddTreasuryEffectType,
                FixedEffectValue = 120000m
            },
            new ReadyLawTemplate
            {
                TemplateId = 32,
                Tier = 3,
                Title = "Директорат контррозвідки (витрата 50 000)",
                Description = "Викриття шпигунських мереж та диверсійних груп.",
                EffectType = RemoveTreasuryEffectType,
                FixedEffectValue = 50000m
            },
            new ReadyLawTemplate
            {
                TemplateId = 33,
                Tier = 3,
                Title = "Закон про заборонені бойові ритуали",
                Description = "Регулювання темної бойової магії і ритуальних практик.",
                EffectType = NoEffectType,
                FixedEffectValue = 0m
            },
            new ReadyLawTemplate
            {
                TemplateId = 34,
                Tier = 3,
                Title = "Стратегічна реквізиція резервів (+95 000)",
                Description = "Централізоване вилучення запасів на час загрози.",
                EffectType = AddTreasuryEffectType,
                FixedEffectValue = 95000m
            },
            new ReadyLawTemplate
            {
                TemplateId = 35,
                Tier = 4,
                Title = "Надзвичайний декрет Вищої ради",
                Description = "Рамковий закон для тимчасового розширення повноважень уряду.",
                EffectType = NoEffectType,
                FixedEffectValue = 0m
            },
            new ReadyLawTemplate
            {
                TemplateId = 36,
                Tier = 4,
                Title = "Створення спецкорпусу «Чорний Щит»",
                Description = "Елітний корпус державної безпеки. Можна перейменувати в розділі розглядання.",
                EffectType = RemoveTreasuryEffectType,
                FixedEffectValue = 110000m
            },
            new ReadyLawTemplate
            {
                TemplateId = 37,
                Tier = 4,
                Title = "Надзвичайний трибунал інквізиції (витрата 100 000)",
                Description = "Спеціальні суди для справ про зраду, єресь і демонологію.",
                EffectType = RemoveTreasuryEffectType,
                FixedEffectValue = 100000m
            },
            new ReadyLawTemplate
            {
                TemplateId = 38,
                Tier = 4,
                Title = "Реквізиція державного майна (+180 000)",
                Description = "Тимчасове вилучення стратегічного майна для порятунку казни.",
                EffectType = AddTreasuryEffectType,
                FixedEffectValue = 180000m
            },
            new ReadyLawTemplate
            {
                TemplateId = 39,
                Tier = 4,
                Title = "Мобілізація проти драконячої загрози (+25%)",
                Description = "Різке підвищення виплат війську на час надзвичайної загрози.",
                EffectType = SetSoldierSalaryEffectType,
                ConfigKey = "SoldierSalaryPerTurn",
                ConfigDefault = 1.0m,
                Multiplier = 1.25m
            },
            new ReadyLawTemplate
            {
                TemplateId = 40,
                Tier = 4,
                Title = "Воєнна ощадність парламенту (-20%)",
                Description = "Зменшення виплат депутатам під час надзвичайного стану.",
                EffectType = SetDeputySalaryEffectType,
                ConfigKey = "DeputySalaryBase",
                ConfigDefault = 1.0m,
                Multiplier = 0.80m
            },
            new ReadyLawTemplate
            {
                TemplateId = 41,
                Tier = 4,
                Title = "Раціонування провіанту та пального",
                Description = "Норми споживання для стабілізації ринку під час кризи.",
                EffectType = NoEffectType,
                FixedEffectValue = 0m
            },
            new ReadyLawTemplate
            {
                TemplateId = 42,
                Tier = 4,
                Title = "Створення Ордену «Завіса Очей»",
                Description = "Відомство зовнішнього спостереження. Назву можна змінити перед голосуванням.",
                EffectType = RemoveTreasuryEffectType,
                FixedEffectValue = 130000m
            },
            new ReadyLawTemplate
            {
                TemplateId = 43,
                Tier = 4,
                Title = "Фонд післявоєнної відбудови (витрата 140 000)",
                Description = "Інвестиції в ремонт укріплень, доріг і портів після кампанії.",
                EffectType = RemoveTreasuryEffectType,
                FixedEffectValue = 140000m
            },
            new ReadyLawTemplate
            {
                TemplateId = 44,
                Tier = 4,
                Title = "Рамковий надзвичайний закон без ефекту",
                Description = "Шаблон для термінових політичних рішень і рольового процесу.",
                EffectType = NoEffectType,
                FixedEffectValue = 0m
            },
            new ReadyLawTemplate
            {
                TemplateId = 45,
                Tier = 2,
                Title = "Закон про Міністерство охорони здоров'я",
                Description = "Створити центральний орган, що керує системою охорони здоров'я держави.",
                EffectType = PublicSystemsEngine.LawHealthMinistry,
                FixedEffectValue = 0m
            },
            new ReadyLawTemplate
            {
                TemplateId = 46,
                Tier = 2,
                Title = "Санітарний кодекс громад",
                Description = "Єдині правила санітарії, гігієни та контролю інфекцій в містах і селах.",
                EffectType = PublicSystemsEngine.LawPublicSanitation,
                FixedEffectValue = 0m
            },
            new ReadyLawTemplate
            {
                TemplateId = 47,
                Tier = 2,
                Title = "Протоколи карантину та ізоляції",
                Description = "Правила локдаунів, ізоляції осередків і санітарних кордонів.",
                EffectType = PublicSystemsEngine.LawQuarantineProtocols,
                FixedEffectValue = 0m
            },
            new ReadyLawTemplate
            {
                TemplateId = 48,
                Tier = 3,
                Title = "Національна програма імунізації",
                Description = "Законодавча база масової вакцинації та профілактики епідемій.",
                EffectType = PublicSystemsEngine.LawNationalImmunization,
                FixedEffectValue = 0m
            },
            new ReadyLawTemplate
            {
                TemplateId = 49,
                Tier = 2,
                Title = "Закон про Міністерство освіти і науки",
                Description = "Створити центральний орган, що керує шкільною, вищою освітою і наукою.",
                EffectType = PublicSystemsEngine.LawEduMinistry,
                FixedEffectValue = 0m
            },
            new ReadyLawTemplate
            {
                TemplateId = 50,
                Tier = 2,
                Title = "Закон про базову шкільну освіту",
                Description = "Гарантія доступу до базової освіти та підвищення грамотності населення.",
                EffectType = PublicSystemsEngine.LawBasicSchooling,
                FixedEffectValue = 0m
            },
            new ReadyLawTemplate
            {
                TemplateId = 51,
                Tier = 3,
                Title = "Хартія вищої освіти",
                Description = "Стандарти університетів, академій та інститутів підготовки кадрів.",
                EffectType = PublicSystemsEngine.LawHigherEduCharter,
                FixedEffectValue = 0m
            },
            new ReadyLawTemplate
            {
                TemplateId = 52,
                Tier = 3,
                Title = "Державна стипендійна програма",
                Description = "Підтримка студентів і молодих науковців для підготовки спеціалістів.",
                EffectType = PublicSystemsEngine.LawScholarshipProgram,
                FixedEffectValue = 0m
            },
            new ReadyLawTemplate
            {
                TemplateId = 53,
                Tier = 3,
                Title = "Закон про міжнародне співробітництво",
                Description = "Рамка для участі в міжнародних програмах охорони здоров'я та освіти.",
                EffectType = PublicSystemsEngine.LawIntlCooperation,
                FixedEffectValue = 0m
            },
            new ReadyLawTemplate
            {
                TemplateId = 54,
                Tier = 3,
                Title = "Обмін епідданими з партнерами",
                Description = "Підключення до міжнародного епідеміологічного обміну даними.",
                EffectType = PublicSystemsEngine.LawIntlDataSharingHealth,
                FixedEffectValue = 0m
            },
            new ReadyLawTemplate
            {
                TemplateId = 55,
                Tier = 3,
                Title = "Академічні обміни та мобільність",
                Description = "Міждержавні академічні програми, обміни викладачів і студентів.",
                EffectType = PublicSystemsEngine.LawIntlAcademicExchange,
                FixedEffectValue = 0m
            },
            new ReadyLawTemplate
            {
                TemplateId = 56,
                Tier = 3,
                Title = "Програма громадського здоров'я",
                Description = "Профілактика захворювань, санітарні рейди та робота з довірою населення.",
                EffectType = PublicSystemsEngine.LawCommunityHealthOutreach,
                FixedEffectValue = 0m
            },
            new ReadyLawTemplate
            {
                TemplateId = 57,
                Tier = 3,
                Title = "Сертифікація педагогів",
                Description = "Єдині вимоги до кваліфікації викладачів і стимул для STEM-напрямів.",
                EffectType = PublicSystemsEngine.LawTeacherCertification,
                FixedEffectValue = 0m
            }
        };

        private async Task StartLawCreation(ITelegramBotClient botClient, long userId, CancellationToken cancellationToken)
        {
            if (!TryGetCountryForDeputy(userId, out int countryId, out string error))
            {
                var errMsg = await botClient.SendTextMessageAsync(userId, error, cancellationToken: cancellationToken);
                TrackOldMessage(userId, errMsg.MessageId);
                return;
            }

            ClearLawCreationState(userId);
            LawReviewEditStateByUser.Remove(userId);
            LawCreationCountryByUser[userId] = countryId;
            LawCreationStepByUser[userId] = LawStepTitle;
            LawCreationBufferByUser[userId] = new LawRecord
            {
                CountryId = countryId,
                Title = string.Empty,
                Description = null,
                Status = LawStatusReviewFirst,
                EffectType = string.Empty,
                EffectValue = 0,
                CreatedByCharacterId = UserData.GetActiveCharacterId(userId),
                ExpiresAt = null,
                ParentLawId = null
            };

            var msg = await botClient.SendTextMessageAsync(userId, "Вкажіть назву закону:", cancellationToken: cancellationToken);
            TrackOldMessage(userId, msg.MessageId);
        }

        private async Task ShowReadyLawTemplateTierMenu(ITelegramBotClient botClient, long userId, long chatId, int msgId, CancellationToken cancellationToken)
        {
            if (!TryGetCountryForDeputy(userId, out _, out string error))
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    error,
                    StateKeyboardBuilder.ParliamentStateKeyboard(),
                    cancellationToken
                );
                return;
            }

            const string text =
                "📦 Готові закони за рівнями\n" +
                "Оберіть рівень складності/масштабу закону.\n" +
                "Після додавання закон потрапить на 1 читання, де його можна редагувати.";

            await SafeEditMessageTextAsync(
                botClient,
                chatId,
                msgId,
                text,
                StateKeyboardBuilder.ParliamentReadyLawTemplateTierKeyboard(),
                cancellationToken
            );
        }

        private async Task ShowReadyLawTemplateList(ITelegramBotClient botClient, long userId, long chatId, int msgId, int page, int? tier, CancellationToken cancellationToken)
        {
            if (!TryGetCountryForDeputy(userId, out int countryId, out string error))
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    error,
                    StateKeyboardBuilder.ParliamentStateKeyboard(),
                    cancellationToken
                );
                return;
            }

            const int perPage = 6;
            var templates = ReadyLawTemplates
                .Where(t => !tier.HasValue || t.Tier == tier.Value)
                .OrderBy(t => t.TemplateId)
                .ToList();

            if (templates.Count == 0)
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "Немає шаблонів для цього рівня.",
                    StateKeyboardBuilder.ParliamentReadyLawTemplateTierKeyboard(),
                    cancellationToken
                );
                return;
            }

            int totalPages = Math.Max(1, (int)Math.Ceiling(templates.Count / (double)perPage));
            int safePage = Math.Clamp(page, 0, totalPages - 1);

            var pageItems = templates
                .Skip(safePage * perPage)
                .Take(perPage)
                .ToList();

            var keyboardItems = pageItems
                .Select(t => (text: $"📜 {t.Title}", data: $"stateparliament_lawtemplate_pick_{t.TemplateId}"))
                .ToList();

            var lines = new List<string>
            {
                "📦 Готові закони",
                $"Рівень: {BuildLawTemplateTierTitle(tier)}",
                "Оберіть шаблон закону для швидкого внесення на 1 читання:",
                ""
            };

            for (int i = 0; i < pageItems.Count; i++)
            {
                int number = safePage * perPage + i + 1;
                var template = pageItems[i];
                decimal effectValue = ResolveReadyLawTemplateEffectValue(countryId, template);
                var previewLaw = new LawRecord
                {
                    CountryId = countryId,
                    Title = template.Title,
                    Description = template.Description,
                    EffectType = template.EffectType,
                    EffectValue = effectValue
                };

                lines.Add($"{number}. {template.Title}");
                lines.Add($"   Ефект: {BuildLawEffectText(previewLaw)}");
            }

            lines.Add("");
            lines.Add($"Сторінка {safePage + 1}/{totalPages}");
            lines.Add("Після додавання закон можна перейменувати або відредагувати в меню «Розглядання».");

            await SafeEditMessageTextAsync(
                botClient,
                chatId,
                msgId,
                string.Join("\n", lines),
                StateKeyboardBuilder.ParliamentReadyLawTemplateKeyboard(keyboardItems, tier, safePage, totalPages),
                cancellationToken
            );
        }

        private async Task ApplyReadyLawTemplate(ITelegramBotClient botClient, long userId, long chatId, int msgId, int templateId, int? tier, CancellationToken cancellationToken)
        {
            if (!TryGetCountryForDeputy(userId, out int countryId, out string error))
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    error,
                    StateKeyboardBuilder.ParliamentStateKeyboard(),
                    cancellationToken
                );
                return;
            }

            var template = ReadyLawTemplates.FirstOrDefault(t => t.TemplateId == templateId);
            if (template == null)
            {
                await ShowReadyLawTemplateList(botClient, userId, chatId, msgId, 0, tier, cancellationToken);
                return;
            }

            decimal effectValue = ResolveReadyLawTemplateEffectValue(countryId, template);
            var law = new LawRecord
            {
                CountryId = countryId,
                Title = template.Title,
                Description = template.Description,
                Status = LawStatusReviewFirst,
                EffectType = NormalizeLawEffectType(template.EffectType),
                EffectValue = effectValue,
                CreatedByCharacterId = UserData.GetActiveCharacterId(userId),
                ExpiresAt = null,
                ParentLawId = null
            };

            string addResult;
            try
            {
                addResult = LawsData.AddLaw(law);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Apply ready law template failed. UserId={UserId} CountryId={CountryId} TemplateId={TemplateId}", userId, countryId, templateId);
                addResult = $"Помилка створення закону: {ex.Message}";
            }

            string text =
                $"📦 Готовий закон додано на розглядання\n" +
                $"Назва: {law.Title}\n" +
                $"Рівень: {BuildLawTemplateTierTitle(template.Tier)}\n" +
                $"Ефект: {BuildLawEffectText(law)}\n" +
                $"Статус: 1 читання\n" +
                $"{addResult}";

            await SafeEditMessageTextAsync(
                botClient,
                chatId,
                msgId,
                text,
                StateKeyboardBuilder.ParliamentStateKeyboard(),
                cancellationToken
            );
        }

        private static decimal ResolveReadyLawTemplateEffectValue(int countryId, ReadyLawTemplate template)
        {
            if (template.FixedEffectValue.HasValue)
                return decimal.Round(template.FixedEffectValue.Value, 4, MidpointRounding.AwayFromZero);

            if (!string.IsNullOrWhiteSpace(template.ConfigKey))
            {
                string key = template.ConfigKey;
                decimal fallback = EconomyConfigData.GetDecimal(key, template.ConfigDefault);
                if (fallback <= 0m)
                    fallback = template.ConfigDefault;

                decimal current = EconomyConfigData.GetDecimalForCountry(countryId, key, fallback);
                if (current <= 0m)
                    current = fallback > 0m ? fallback : 1m;

                decimal multiplier = template.Multiplier <= 0m ? 1m : template.Multiplier;
                decimal resolved = current * multiplier;
                if (resolved < 0m)
                    resolved = 0m;
                return decimal.Round(resolved, 4, MidpointRounding.AwayFromZero);
            }

            return 0m;
        }

        private async Task HandleLawCreationInput(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
        {
            if (message?.From == null)
                return;

            long userId = message.From.Id;
            if (!LawCreationStepByUser.TryGetValue(userId, out int step) || !LawCreationBufferByUser.TryGetValue(userId, out var buffer))
                return;

            TrackOldMessage(userId, message.MessageId);
            string input = (message.Text ?? string.Empty).Trim();

            switch (step)
            {
                case LawStepTitle:
                    if (string.IsNullOrWhiteSpace(input))
                    {
                        var errMsg = await botClient.SendTextMessageAsync(userId, "Назва не може бути порожньою. Спробуйте ще раз:", cancellationToken: cancellationToken);
                        TrackOldMessage(userId, errMsg.MessageId);
                        return;
                    }
                    buffer.Title = input;
                    LawCreationStepByUser[userId] = LawStepDescription;
                    {
                        var msg = await botClient.SendTextMessageAsync(userId, "Вкажіть опис закону (можна залишити порожньо):", cancellationToken: cancellationToken);
                        TrackOldMessage(userId, msg.MessageId);
                    }
                    return;
                case LawStepDescription:
                    buffer.Description = string.IsNullOrWhiteSpace(input) ? null : input;
                    LawCreationStepByUser[userId] = LawStepParent;
                    await PromptLawParentSelection(botClient, userId, page: 0, cancellationToken);
                    return;
                case LawStepParent:
                    if (!TryApplyLawParentFromInput(buffer, input, out string parentError))
                    {
                        var errMsg = await botClient.SendTextMessageAsync(
                            userId,
                            string.IsNullOrWhiteSpace(parentError) ? "Не вдалося обрати базовий закон. Спробуйте ще раз або використайте кнопки." : parentError,
                            replyMarkup: StateKeyboardBuilder.ParliamentLawParentKeyboard(LawsData.GetByCountry(buffer.CountryId), 0),
                            cancellationToken: cancellationToken);
                        TrackOldMessage(userId, errMsg.MessageId);
                        return;
                    }

                    LawCreationStepByUser[userId] = LawStepEffectType;
                    await ContinueLawEffectTypeStep(botClient, userId, cancellationToken);
                    return;
                case LawStepEffectType:
                    if (string.IsNullOrWhiteSpace(input))
                    {
                        var errMsg = await botClient.SendTextMessageAsync(userId, "Тип ефекту не може бути порожнім. Спробуйте ще раз:", cancellationToken: cancellationToken);
                        TrackOldMessage(userId, errMsg.MessageId);
                        return;
                    }

                    string normalizedInputEffect = NormalizeLawEffectType(input);
                    if (string.Equals(normalizedInputEffect, NoEffectType, StringComparison.OrdinalIgnoreCase))
                    {
                        buffer.EffectType = NoEffectType;
                        buffer.EffectValue = 0m;
                        LawCreationStepByUser[userId] = LawStepExpiration;
                        await PromptLawExpirationDate(botClient, userId, cancellationToken);
                        return;
                    }
                    if (IsRenameCountryEffect(normalizedInputEffect))
                    {
                        buffer.EffectType = RenameCountryEffectType;
                        LawCreationStepByUser[userId] = LawStepRenameName;
                        var msg = await botClient.SendTextMessageAsync(userId, "Вкажіть нову назву країни:", cancellationToken: cancellationToken);
                        TrackOldMessage(userId, msg.MessageId);
                        return;
                    }

                    buffer.EffectType = normalizedInputEffect;
                    if (!CanLawEffectUseValue(buffer.EffectType))
                    {
                        buffer.EffectValue = 0m;
                        LawCreationStepByUser[userId] = LawStepExpiration;
                        await PromptLawExpirationDate(botClient, userId, cancellationToken);
                        return;
                    }
                    LawCreationStepByUser[userId] = LawStepEffectValue;
                    {
                        var msg = await botClient.SendTextMessageAsync(userId, "Вкажіть значення ефекту (десяткове число):", cancellationToken: cancellationToken);
                        TrackOldMessage(userId, msg.MessageId);
                    }
                    return;
                case LawStepRenameName:
                    if (!CountryNameUtils.TryNormalize(input, out var renameName, out var renameError))
                    {
                        var errMsg = await botClient.SendTextMessageAsync(userId, $"❌ {renameError} Спробуйте ще раз:", cancellationToken: cancellationToken);
                        TrackOldMessage(userId, errMsg.MessageId);
                        return;
                    }
                    buffer.Description = BuildRenameDescription(renameName, buffer.Description);
                    buffer.EffectValue = 0m;
                    LawCreationStepByUser[userId] = LawStepExpiration;
                    await PromptLawExpirationDate(botClient, userId, cancellationToken);
                    return;
                case LawStepEffectValue:
                    if (!TryParseDecimal(input, out decimal value))
                    {
                        var errMsg = await botClient.SendTextMessageAsync(userId, "Невірне значення. Вкажіть десяткове число:", cancellationToken: cancellationToken);
                        TrackOldMessage(userId, errMsg.MessageId);
                        return;
                    }
                    buffer.EffectValue = value;
                    if (LawEffectSupportsResourceScope(buffer.EffectType))
                    {
                        LawCreationStepByUser[userId] = LawStepEffectScope;
                        await PromptLawEffectScopeInput(botClient, userId, cancellationToken);
                    }
                    else
                    {
                        LawCreationStepByUser[userId] = LawStepExpiration;
                        await PromptLawExpirationDate(botClient, userId, cancellationToken);
                    }
                    return;
                case LawStepEffectScope:
                    {
                        string baseEffectType = GetLawEffectBaseType(buffer.EffectType);
                        if (!LawEffectSupportsResourceScope(baseEffectType))
                        {
                            LawCreationStepByUser[userId] = LawStepExpiration;
                            await PromptLawExpirationDate(botClient, userId, cancellationToken);
                            return;
                        }

                        if (string.IsNullOrWhiteSpace(input) &&
                            TryGetLawEffectScopeResources(buffer.EffectType, out _, out var existingResources) &&
                            existingResources.Count > 0)
                        {
                            buffer.EffectType = BuildScopedLawEffectType(baseEffectType, existingResources);
                            LawCreationStepByUser[userId] = LawStepExpiration;
                            await PromptLawExpirationDate(botClient, userId, cancellationToken);
                            return;
                        }

                        if (!TryResolveLawEffectScopeFromInput(input, out var resources, out string scopeError))
                        {
                            var errMsg = await botClient.SendTextMessageAsync(
                                userId,
                                $"❌ {scopeError} Спробуйте ще раз або введіть '*' для всіх ресурсів.",
                                cancellationToken: cancellationToken);
                            TrackOldMessage(userId, errMsg.MessageId);
                            return;
                        }

                        buffer.EffectType = BuildScopedLawEffectType(baseEffectType, resources);
                        LawCreationStepByUser[userId] = LawStepExpiration;
                        await PromptLawExpirationDate(botClient, userId, cancellationToken);
                        return;
                    }
                case LawStepExpiration:
                    if (string.IsNullOrWhiteSpace(input))
                    {
                        buffer.ExpiresAt = null;
                    }
                    else if (!DateTime.TryParse(input, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var dt))
                    {
                        var errMsg = await botClient.SendTextMessageAsync(userId, "Невірний формат дати. Спробуйте: 2026-02-07 18:30", cancellationToken: cancellationToken);
                        TrackOldMessage(userId, errMsg.MessageId);
                        return;
                    }
                    else
                    {
                        buffer.ExpiresAt = dt;
                    }
                    await FinalizeLawCreation(botClient, userId, cancellationToken);
                    return;
                default:
                    return;
            }
        }

        private async Task HandleLawTypeSelection(ITelegramBotClient botClient, CallbackQuery callbackQuery, string rawType, CancellationToken cancellationToken)
        {
            long userId = callbackQuery.From.Id;
            if (!LawCreationStepByUser.TryGetValue(userId, out int step) || step != LawStepEffectType)
                return;
            if (!LawCreationBufferByUser.TryGetValue(userId, out var buffer))
                return;

            string selected = (rawType ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(selected))
                return;

            if (string.Equals(selected, "custom", StringComparison.OrdinalIgnoreCase))
            {
                var msg = await botClient.SendTextMessageAsync(userId, "Вкажіть тип ефекту вручну:", cancellationToken: cancellationToken);
                TrackOldMessage(userId, msg.MessageId);
                return;
            }

            string normalizedSelectedEffect = NormalizeLawEffectType(selected);
            if (string.Equals(normalizedSelectedEffect, NoEffectType, StringComparison.OrdinalIgnoreCase))
            {
                buffer.EffectType = NoEffectType;
                buffer.EffectValue = 0m;
                LawCreationStepByUser[userId] = LawStepExpiration;
                await PromptLawExpirationDate(botClient, userId, cancellationToken);
                return;
            }
            if (IsRenameCountryEffect(normalizedSelectedEffect))
            {
                buffer.EffectType = RenameCountryEffectType;
                LawCreationStepByUser[userId] = LawStepRenameName;
                var msg = await botClient.SendTextMessageAsync(userId, "Вкажіть нову назву країни:", cancellationToken: cancellationToken);
                TrackOldMessage(userId, msg.MessageId);
                return;
            }

            buffer.EffectType = normalizedSelectedEffect;
            if (!CanLawEffectUseValue(buffer.EffectType))
            {
                buffer.EffectValue = 0m;
                LawCreationStepByUser[userId] = LawStepExpiration;
                await PromptLawExpirationDate(botClient, userId, cancellationToken);
                return;
            }
            LawCreationStepByUser[userId] = LawStepEffectValue;
            var valueMsg = await botClient.SendTextMessageAsync(userId, "Вкажіть значення ефекту (десяткове число):", cancellationToken: cancellationToken);
            TrackOldMessage(userId, valueMsg.MessageId);
        }

        private async Task PromptLawParentSelection(ITelegramBotClient botClient, long userId, int page, CancellationToken cancellationToken)
        {
            if (!LawCreationBufferByUser.TryGetValue(userId, out var buffer))
                return;

            var parentCandidates = GetLawParentCandidates(buffer.CountryId);
            var msg = await botClient.SendTextMessageAsync(
                userId,
                "Оберіть базовий закон для нової гілки або створіть нову гілку без батьківського закону:",
                replyMarkup: StateKeyboardBuilder.ParliamentLawParentKeyboard(parentCandidates, page),
                cancellationToken: cancellationToken);
            TrackOldMessage(userId, msg.MessageId);
        }

        private async Task HandleLawParentSelection(ITelegramBotClient botClient, CallbackQuery callbackQuery, string data, CancellationToken cancellationToken)
        {
            long userId = callbackQuery.From.Id;
            if (!LawCreationStepByUser.TryGetValue(userId, out int step) || step != LawStepParent)
                return;
            if (!LawCreationBufferByUser.TryGetValue(userId, out var buffer))
                return;
            if (callbackQuery.Message == null)
                return;

            var parentCandidates = GetLawParentCandidates(buffer.CountryId);
            if (data.StartsWith("stateparliament_lawparent_page_", StringComparison.OrdinalIgnoreCase))
            {
                int page = 0;
                var parts = data.Split('_');
                if (parts.Length > 0 && int.TryParse(parts[^1], out int parsed))
                    page = Math.Max(0, parsed);

                await SafeEditMessageTextAsync(
                    botClient,
                    callbackQuery.Message.Chat.Id,
                    callbackQuery.Message.MessageId,
                    "Оберіть базовий закон для нової гілки або створіть нову гілку без батьківського закону:",
                    StateKeyboardBuilder.ParliamentLawParentKeyboard(parentCandidates, page),
                    cancellationToken);
                return;
            }

            if (data.EndsWith("_none", StringComparison.OrdinalIgnoreCase))
            {
                buffer.ParentLawId = null;
                LawCreationStepByUser[userId] = LawStepEffectType;
                await ContinueLawEffectTypeStep(botClient, userId, cancellationToken);
                return;
            }

            if (!data.StartsWith("stateparliament_lawparent_pick_", StringComparison.OrdinalIgnoreCase))
                return;

            if (!TryParseTrailingId(data, out int parentLawId))
                return;

            var parentLaw = parentCandidates.FirstOrDefault(l => l.LawId == parentLawId);
            if (parentLaw == null)
            {
                await botClient.SendTextMessageAsync(
                    userId,
                    "Не вдалося знайти обраний закон. Спробуйте ще раз.",
                    cancellationToken: cancellationToken);
                return;
            }

            buffer.ParentLawId = parentLaw.LawId;
            LawCreationStepByUser[userId] = LawStepEffectType;
            await ContinueLawEffectTypeStep(botClient, userId, cancellationToken);
        }

        private static bool TryApplyLawParentFromInput(LawRecord buffer, string input, out string error)
        {
            error = string.Empty;
            string normalized = (input ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(normalized) ||
                normalized.Equals("0", StringComparison.OrdinalIgnoreCase) ||
                normalized.Equals("none", StringComparison.OrdinalIgnoreCase) ||
                normalized.Equals("no", StringComparison.OrdinalIgnoreCase) ||
                normalized.Equals("new", StringComparison.OrdinalIgnoreCase) ||
                normalized.Equals("нова", StringComparison.OrdinalIgnoreCase) ||
                normalized.Equals("без", StringComparison.OrdinalIgnoreCase))
            {
                buffer.ParentLawId = null;
                return true;
            }

            if (!int.TryParse(normalized, out int parentLawId) || parentLawId <= 0)
            {
                error = "Вкажіть ID закону, 0 для нової гілки, або оберіть закон кнопкою.";
                return false;
            }

            var parentLaw = LawsData.GetLawById(parentLawId);
            if (parentLaw == null || parentLaw.CountryId != buffer.CountryId)
            {
                error = "Закон з таким ID не знайдено в поточній країні.";
                return false;
            }

            buffer.ParentLawId = parentLaw.LawId;
            return true;
        }

        private async Task ContinueLawEffectTypeStep(ITelegramBotClient botClient, long userId, CancellationToken cancellationToken)
        {
            var msg = await botClient.SendTextMessageAsync(
                userId,
                "Оберіть тип ефекту закону або введіть вручну:",
                replyMarkup: StateKeyboardBuilder.ParliamentLawTypeKeyboard(),
                cancellationToken: cancellationToken);
            TrackOldMessage(userId, msg.MessageId);
        }

        private async Task PromptLawEffectScopeInput(ITelegramBotClient botClient, long userId, CancellationToken cancellationToken)
        {
            var availableResources = EconomyResourcesData.GetAll()
                .OrderBy(r => r.ResourceType, StringComparer.OrdinalIgnoreCase)
                .Select(r => r.ResourceType)
                .Take(20)
                .ToList();

            string resourcesHint = availableResources.Count > 0
                ? string.Join(", ", availableResources)
                : "довідник ресурсів порожній";
            var msg = await botClient.SendTextMessageAsync(
                userId,
                $"Оберіть ресурси для ліміту (через кому) або введіть `*` для всіх.\nПриклад: Food,Tools\nДоступно: {resourcesHint}",
                cancellationToken: cancellationToken);
            TrackOldMessage(userId, msg.MessageId);
        }

        private static List<LawRecord> GetLawParentCandidates(int countryId)
        {
            return LawsData.GetByCountry(countryId)
                .OrderByDescending(l => string.Equals(l.Status, LawStatusAccepted, StringComparison.OrdinalIgnoreCase))
                .ThenByDescending(l => string.Equals(l.Status, LawStatusProposed, StringComparison.OrdinalIgnoreCase))
                .ThenByDescending(l => string.Equals(l.Status, LawStatusReviewSecond, StringComparison.OrdinalIgnoreCase))
                .ThenByDescending(l => string.Equals(l.Status, LawStatusReviewFirst, StringComparison.OrdinalIgnoreCase))
                .ThenBy(l => string.IsNullOrWhiteSpace(l.Title) ? $"#{l.LawId}" : l.Title, StringComparer.OrdinalIgnoreCase)
                .ThenBy(l => l.LawId)
                .ToList();
        }

        private async Task HandleLawDateSkip(ITelegramBotClient botClient, CallbackQuery callbackQuery, CancellationToken cancellationToken)
        {
            long userId = callbackQuery.From.Id;
            if (!LawCreationStepByUser.TryGetValue(userId, out int step) || step != LawStepExpiration)
                return;
            if (!LawCreationBufferByUser.TryGetValue(userId, out var buffer))
                return;

            buffer.ExpiresAt = null;
            await FinalizeLawCreation(botClient, userId, cancellationToken);
        }

        private async Task PromptLawExpirationDate(ITelegramBotClient botClient, long userId, CancellationToken cancellationToken)
        {
            var msg = await botClient.SendTextMessageAsync(
                userId,
                "Вкажіть дату завершення (yyyy-MM-dd або yyyy-MM-dd HH:mm) або натисніть «Пропустити»:",
                replyMarkup: StateKeyboardBuilder.ParliamentLawDateSkipKeyboard(),
                cancellationToken: cancellationToken);
            TrackOldMessage(userId, msg.MessageId);
        }

        private async Task FinalizeLawCreation(ITelegramBotClient botClient, long userId, CancellationToken cancellationToken)
        {
            if (!LawCreationBufferByUser.TryGetValue(userId, out var buffer))
                return;

            if (buffer.ParentLawId.HasValue)
            {
                var parentLaw = LawsData.GetLawById(buffer.ParentLawId.Value);
                if (parentLaw == null || parentLaw.CountryId != buffer.CountryId)
                    buffer.ParentLawId = null;
            }

            string result;
            try
            {
                result = LawsData.AddLaw(buffer);
            }
            catch (Exception ex)
            {
                int countryId = LawCreationCountryByUser.TryGetValue(userId, out var cId) ? cId : 0;
                _logger.LogError(ex, "Create law failed. UserId={UserId} CountryId={CountryId}", userId, countryId);
                result = $"Помилка створення закону: {ex.Message}";
            }

            ClearLawCreationState(userId);
            CleanupOldMessagesForUser(botClient, cancellationToken, userId);
            string finalText = result;
            if (!result.StartsWith("Помилка", StringComparison.OrdinalIgnoreCase))
                finalText += "\nСтатус: 1 читання (розглядання).";
            await botClient.SendTextMessageAsync(userId, finalText, cancellationToken: cancellationToken);
        }

        private static void ClearLawCreationState(long userId)
        {
            LawCreationStepByUser.Remove(userId);
            LawCreationBufferByUser.Remove(userId);
            LawCreationCountryByUser.Remove(userId);
        }

        private static bool TryParseDecimal(string input, out decimal value)
        {
            return decimal.TryParse(
                (input ?? string.Empty).Replace(',', '.'),
                NumberStyles.Number,
                CultureInfo.InvariantCulture,
                out value
            );
        }

        private static bool IsLawOnReview(string status)
        {
            return GetLawReviewStage(status) > 0;
        }

        private static int GetLawReviewStage(string status)
        {
            if (string.Equals(status, LawStatusReviewFirst, StringComparison.OrdinalIgnoreCase))
                return 1;
            if (string.Equals(status, LawStatusReviewSecond, StringComparison.OrdinalIgnoreCase))
                return 2;
            return 0;
        }

        private static string GetLawStatusDisplayName(string status)
        {
            if (string.Equals(status, LawStatusReviewFirst, StringComparison.OrdinalIgnoreCase))
                return "Розглядання: 1 читання";
            if (string.Equals(status, LawStatusReviewSecond, StringComparison.OrdinalIgnoreCase))
                return "Розглядання: 2 читання";
            if (string.Equals(status, LawStatusProposed, StringComparison.OrdinalIgnoreCase))
                return "Готовий до голосування";
            if (string.Equals(status, LawStatusAccepted, StringComparison.OrdinalIgnoreCase))
                return "Прийнято";
            if (string.Equals(status, LawStatusRejected, StringComparison.OrdinalIgnoreCase))
                return "Відхилено";
            if (string.Equals(status, LawStatusRevoked, StringComparison.OrdinalIgnoreCase))
                return "Скасовано";
            return string.IsNullOrWhiteSpace(status) ? "—" : status.Trim();
        }

        private static string BuildLawTemplateTierTitle(int? tier)
        {
            if (!tier.HasValue)
                return "Усі рівні";

            return tier.Value switch
            {
                1 => "I (місцеві закони)",
                2 => "II (державні інституції)",
                3 => "III (стратегія і війна)",
                4 => "IV (надзвичайний рівень)",
                _ => "Усі рівні"
            };
        }

        private static bool TryParseLawTemplateTierPage(string data, out int? tier, out int page)
        {
            tier = null;
            page = 0;
            if (string.IsNullOrWhiteSpace(data))
                return false;

            if (data.StartsWith("stateparliament_lawtemplate_page_", StringComparison.OrdinalIgnoreCase))
            {
                var legacyParts = data.Split('_');
                if (legacyParts.Length > 0 && int.TryParse(legacyParts[^1], out int legacyPage))
                    page = Math.Max(0, legacyPage);
                return true;
            }

            if (!data.StartsWith("stateparliament_lawtemplate_tier_", StringComparison.OrdinalIgnoreCase))
                return false;

            var parts = data.Split('_');
            if (parts.Length < 6)
                return false;

            if (!int.TryParse(parts[3], out int parsedTier))
                return false;

            if (parts.Length >= 6 &&
                string.Equals(parts[4], "page", StringComparison.OrdinalIgnoreCase) &&
                int.TryParse(parts[5], out int parsedPage))
            {
                page = Math.Max(0, parsedPage);
            }

            if (parsedTier > 0)
                tier = parsedTier;

            return true;
        }

        private static bool IsRenameCountryEffect(string effectType)
        {
            if (string.IsNullOrWhiteSpace(effectType))
                return false;
            string normalized = effectType.Trim();
            return string.Equals(normalized, RenameCountryEffectType, StringComparison.OrdinalIgnoreCase)
                || string.Equals(normalized, "ПерейменуванняКраїни", StringComparison.OrdinalIgnoreCase)
                || string.Equals(normalized, "ПерейменуватиКраїну", StringComparison.OrdinalIgnoreCase)
                || string.Equals(normalized, "Перейменування", StringComparison.OrdinalIgnoreCase);
        }

        private static string BuildLawAliasKey(string effectType)
        {
            if (string.IsNullOrWhiteSpace(effectType))
                return string.Empty;

            return new string(effectType
                .Where(char.IsLetterOrDigit)
                .Select(char.ToUpperInvariant)
                .ToArray());
        }

        private static bool TryNormalizePublicLawEffectType(string effectType, out string normalizedLawCode)
        {
            normalizedLawCode = string.Empty;
            if (string.IsNullOrWhiteSpace(effectType))
                return false;

            string normalized = effectType.Trim();
            if (PublicSystemsEngine.IsLawCode(normalized))
            {
                normalizedLawCode = PublicSystemsEngine.NormalizeLawCode(normalized);
                return true;
            }

            string alias = BuildLawAliasKey(normalized);
            if (string.IsNullOrWhiteSpace(alias))
                return false;

            normalizedLawCode = alias switch
            {
                "МОЗ" or "МІНІСТЕРСТВООХОРОНИЗДОРОВЯ" or "HEALTHMINISTRY" => PublicSystemsEngine.LawHealthMinistry,
                "САНІТАРІЯ" or "САНІТАРНИЙКОДЕКС" or "PUBLICSANITATION" => PublicSystemsEngine.LawPublicSanitation,
                "КАРАНТИН" or "ПРОТОКОЛИКАРАНТИНУ" or "QUARANTINEPROTOCOLS" => PublicSystemsEngine.LawQuarantineProtocols,
                "ІМУНІЗАЦІЯ" or "ВАКЦИНАЦІЯ" or "NATIONALIMMUNIZATION" => PublicSystemsEngine.LawNationalImmunization,
                "ЛІЦЕНЗУВАННЯЛІКАРІВ" or "МЕДИЧНАЛІЦЕНЗІЯ" or "MEDICALLICENSING" => PublicSystemsEngine.LawMedicalLicensing,
                "ФАРМААГЕНЦІЯ" or "РОЗРОБКАЛІКІВ" or "PHARMARESEARCHAGENCY" => PublicSystemsEngine.LawPharmaResearchAgency,
                "КЛІНІЧНІПРОТОКОЛИ" or "КЛІНІЧНІВИПРОБУВАННЯ" or "CLINICALTRIALPROTOCOLS" => PublicSystemsEngine.LawClinicalTrialProtocols,
                "РЕЗЕРВЛІКІВ" or "СТРАТЕГІЧНИЙРЕЗЕРВЛІКІВ" or "EMERGENCYMEDICINERESERVE" => PublicSystemsEngine.LawEmergencyMedicineReserve,
                "РЕЗИДЕНЦІЇЛІКАРІВ" or "ІНТЕРНАТУРАЛІКАРІВ" or "HOSPITALRESIDENCYPROGRAM" => PublicSystemsEngine.LawHospitalResidencyProgram,
                "МОН" or "МІНІСТЕРСТВООСВІТИІНАУКИ" or "EDUMINISTRY" => PublicSystemsEngine.LawEduMinistry,
                "БАЗОВАОСВІТА" or "BASICSCHOOLING" => PublicSystemsEngine.LawBasicSchooling,
                "ВИЩАОСВІТА" or "HIGHEREDUCHARTER" => PublicSystemsEngine.LawHigherEduCharter,
                "СТИПЕНДІЇ" or "SCHOLARSHIPPROGRAM" => PublicSystemsEngine.LawScholarshipProgram,
                "INTLСПІВПРАЦЯ" or "МІЖНАРОДНЕСПІВРОБІТНИЦТВО" or "INTLCOOPERATION" => PublicSystemsEngine.LawIntlCooperation,
                "INTLЕПІДДАНІ" or "ОБМІНЕПІДДАНИМИ" or "INTLDATASHARINGHEALTH" => PublicSystemsEngine.LawIntlDataSharingHealth,
                "INTLАКАДЕМОБМІН" or "АКАДЕМІЧНІОБМІНИ" or "INTLACADEMICEXCHANGE" => PublicSystemsEngine.LawIntlAcademicExchange,
                "ГРОМАДСЬКЕЗДОРОВЯ" or "COMMUNITYHEALTHOUTREACH" => PublicSystemsEngine.LawCommunityHealthOutreach,
                "СЕРТИФІКАЦІЯПЕДАГОГІВ" or "TEACHERCERTIFICATION" => PublicSystemsEngine.LawTeacherCertification,
                _ => string.Empty
            };

            return !string.IsNullOrWhiteSpace(normalizedLawCode);
        }

        private static string NormalizeLawEffectType(string effectType)
        {
            if (string.IsNullOrWhiteSpace(effectType))
                return string.Empty;

            string input = effectType.Trim();
            if (TrySplitLawEffectScopeSpecifier(input, out var rawBaseEffectType, out var rawScope))
            {
                string normalizedBase = NormalizeLawEffectTypeCore(rawBaseEffectType);
                if (string.IsNullOrWhiteSpace(normalizedBase))
                    return string.Empty;
                if (!LawEffectSupportsResourceScope(normalizedBase))
                    return normalizedBase;

                var resources = ParseLawEffectResourceTokens(rawScope);
                return BuildScopedLawEffectType(normalizedBase, resources);
            }

            return NormalizeLawEffectTypeCore(input);
        }

        private static string NormalizeLawEffectTypeCore(string effectType)
        {
            if (string.IsNullOrWhiteSpace(effectType))
                return string.Empty;

            string normalized = effectType.Trim();
            if (IsRenameCountryEffect(normalized))
                return RenameCountryEffectType;

            if (TryNormalizePublicLawEffectType(normalized, out string publicLawCode))
                return publicLawCode;

            if (string.Equals(normalized, SetSoldierSalaryEffectType, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(normalized, "ЗпВійськовим", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(normalized, "ЗарплатаВійськовим", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(normalized, "SoldierSalary", StringComparison.OrdinalIgnoreCase))
                return SetSoldierSalaryEffectType;

            if (string.Equals(normalized, SetDeputySalaryEffectType, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(normalized, "ЗпДепутатам", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(normalized, "ЗарплатаДепутатам", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(normalized, "DeputySalary", StringComparison.OrdinalIgnoreCase))
                return SetDeputySalaryEffectType;

            if (string.Equals(normalized, SetCivilianSalaryEffectType, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(normalized, "ЗпЛюдям", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(normalized, "ЗарплатаЛюдям", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(normalized, "CivilianSalary", StringComparison.OrdinalIgnoreCase))
                return SetCivilianSalaryEffectType;

            if (string.Equals(normalized, SetPensionPaymentEffectType, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(normalized, "Пенсія", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(normalized, "Пенсії", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(normalized, "PensionPayment", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(normalized, "Pension", StringComparison.OrdinalIgnoreCase))
                return SetPensionPaymentEffectType;

            if (string.Equals(normalized, SetSocialSupportPaymentEffectType, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(normalized, "СоцВиплата", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(normalized, "СоцВиплати", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(normalized, "СоцЗахист", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(normalized, "SocialSupportPayment", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(normalized, "SocialSupport", StringComparison.OrdinalIgnoreCase))
                return SetSocialSupportPaymentEffectType;

            if (string.Equals(normalized, AddTreasuryEffectType, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(normalized, "ДодатиВКазну", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(normalized, "ДодатиДоКазни", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(normalized, "TreasuryAdd", StringComparison.OrdinalIgnoreCase))
                return AddTreasuryEffectType;

            if (string.Equals(normalized, RemoveTreasuryEffectType, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(normalized, "СписатиЗКазни", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(normalized, "ВіднятиЗКазни", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(normalized, "TreasuryRemove", StringComparison.OrdinalIgnoreCase))
                return RemoveTreasuryEffectType;

            if (string.Equals(normalized, AddTreasuryDailyEffectType, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(normalized, "ДодатиВКазнуЩодня", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(normalized, "КазнаПлюсЩодня", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(normalized, "TreasuryDailyAdd", StringComparison.OrdinalIgnoreCase))
                return AddTreasuryDailyEffectType;

            if (string.Equals(normalized, RemoveTreasuryDailyEffectType, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(normalized, "СписатиЗКазниЩодня", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(normalized, "КазнаМінусЩодня", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(normalized, "TreasuryDailyRemove", StringComparison.OrdinalIgnoreCase))
                return RemoveTreasuryDailyEffectType;

            if (string.Equals(normalized, LimitImportEffectType, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(normalized, "ОбмежитиІмпорт", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(normalized, "ЛімітІмпорту", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(normalized, "ImportLimit", StringComparison.OrdinalIgnoreCase))
                return LimitImportEffectType;

            if (string.Equals(normalized, LimitExportEffectType, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(normalized, "ОбмежитиЕкспорт", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(normalized, "ЛімітЕкспорту", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(normalized, "ExportLimit", StringComparison.OrdinalIgnoreCase))
                return LimitExportEffectType;

            if (string.Equals(normalized, LimitCompanyResourceUseEffectType, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(normalized, "ЛімітСпоживанняКомпаній", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(normalized, "ОбмежитиСпоживанняКомпаній", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(normalized, "CompanyResourceUseLimit", StringComparison.OrdinalIgnoreCase))
                return LimitCompanyResourceUseEffectType;

            if (string.Equals(normalized, SocialContributionEffectType, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(normalized, "Social", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(normalized, "Соціум", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(normalized, "СоцВнесок", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(normalized, "СоціальніЗбори", StringComparison.OrdinalIgnoreCase))
                return SocialContributionEffectType;

            if (string.Equals(normalized, GrossIncomeTaxEffectType, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(normalized, "Tax", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(normalized, "Податки", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(normalized, "ПодатокНаБрутто", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(normalized, "GrossTax", StringComparison.OrdinalIgnoreCase))
                return GrossIncomeTaxEffectType;

            if (string.Equals(normalized, NetIncomeTaxEffectType, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(normalized, "ПодатокНаНетто", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(normalized, "NetTax", StringComparison.OrdinalIgnoreCase))
                return NetIncomeTaxEffectType;

            if (string.Equals(normalized, NoEffectType, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(normalized, "Пропустити", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(normalized, "БезЕфекту", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(normalized, "Skip", StringComparison.OrdinalIgnoreCase))
                return NoEffectType;

            return normalized;
        }

        private static bool LawEffectSupportsResourceScope(string effectType)
        {
            string baseType = NormalizeLawEffectTypeCore(effectType);
            if (string.IsNullOrWhiteSpace(baseType))
                return false;

            return string.Equals(baseType, LimitImportEffectType, StringComparison.OrdinalIgnoreCase)
                || string.Equals(baseType, LimitExportEffectType, StringComparison.OrdinalIgnoreCase);
        }

        private static bool TrySplitLawEffectScopeSpecifier(string effectType, out string baseEffectType, out string scopeRaw)
        {
            baseEffectType = string.Empty;
            scopeRaw = string.Empty;

            string input = (effectType ?? string.Empty).Trim();
            if (input.Length < 3 || !input.EndsWith("]", StringComparison.Ordinal))
                return false;

            int openIndex = input.IndexOf('[');
            if (openIndex <= 0 || openIndex >= input.Length - 1)
                return false;

            baseEffectType = input.Substring(0, openIndex).Trim();
            scopeRaw = input.Substring(openIndex + 1, input.Length - openIndex - 2).Trim();
            return !string.IsNullOrWhiteSpace(baseEffectType);
        }

        private static string GetLawEffectBaseType(string effectType)
        {
            if (string.IsNullOrWhiteSpace(effectType))
                return string.Empty;

            string normalized = NormalizeLawEffectType(effectType);
            if (TrySplitLawEffectScopeSpecifier(normalized, out var rawBase, out _))
                return NormalizeLawEffectTypeCore(rawBase);

            return NormalizeLawEffectTypeCore(normalized);
        }

        private static bool TryGetLawEffectScopeResources(string effectType, out string baseEffectType, out List<string> resources)
        {
            resources = new List<string>();
            baseEffectType = GetLawEffectBaseType(effectType);
            if (string.IsNullOrWhiteSpace(baseEffectType))
                return false;

            string normalized = NormalizeLawEffectType(effectType);
            if (!TrySplitLawEffectScopeSpecifier(normalized, out _, out var scopeRaw))
                return false;

            resources = ParseLawEffectResourceTokens(scopeRaw);
            return resources.Count > 0;
        }

        private static string BuildScopedLawEffectType(string baseEffectType, IEnumerable<string> resources)
        {
            string baseType = NormalizeLawEffectTypeCore(baseEffectType);
            if (string.IsNullOrWhiteSpace(baseType))
                return string.Empty;
            if (!LawEffectSupportsResourceScope(baseType))
                return baseType;

            var normalizedResources = ParseLawEffectResourceTokens(string.Join(",", resources ?? Enumerable.Empty<string>()));
            if (normalizedResources.Count == 0)
                return baseType;

            return $"{baseType}[{string.Join(",", normalizedResources)}]";
        }

        private static List<string> ParseLawEffectResourceTokens(string scopeRaw)
        {
            var resources = new List<string>();
            if (string.IsNullOrWhiteSpace(scopeRaw))
                return resources;

            var unique = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var parts = scopeRaw.Split(new[] { ',', ';', '|', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string rawPart in parts)
            {
                string token = (rawPart ?? string.Empty).Trim();
                if (string.IsNullOrWhiteSpace(token))
                    continue;
                if (token == "*" ||
                    token.Equals("all", StringComparison.OrdinalIgnoreCase) ||
                    token.Equals("всі", StringComparison.OrdinalIgnoreCase) ||
                    token.Equals("усі", StringComparison.OrdinalIgnoreCase))
                {
                    resources.Clear();
                    return resources;
                }
                if (unique.Add(token))
                    resources.Add(token);
            }

            resources.Sort(StringComparer.OrdinalIgnoreCase);
            return resources;
        }

        private static string NormalizeLawResourceInputKey(string token)
        {
            string input = (token ?? string.Empty).Trim().ToLowerInvariant();
            return input switch
            {
                "їжа" or "харчі" or "продовольство" => "food",
                "одяг" => "clothing",
                "житло" => "housing",
                "інструменти" or "знаряддя" => "tools",
                "товари" => "goods",
                "розкіш" => "luxury",
                "зброя" => "weapons",
                "боєприпаси" => "ammunition",
                "броня" => "armor",
                "артилерія" => "artillery",
                "забезпечення" => "support",
                _ => input
            };
        }

        private static bool TryResolveLawResourceToken(string token, IReadOnlyList<EconomyResourceRecord> resources, out string resourceType)
        {
            resourceType = string.Empty;
            string raw = (token ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(raw))
                return false;

            var exact = resources.FirstOrDefault(r => string.Equals(r.ResourceType, raw, StringComparison.OrdinalIgnoreCase));
            if (exact != null)
            {
                resourceType = exact.ResourceType;
                return true;
            }

            string normalizedInput = NormalizeLawResourceInputKey(raw);
            var fallback = resources.FirstOrDefault(r =>
                string.Equals(NormalizeLawResourceInputKey(r.ResourceType), normalizedInput, StringComparison.OrdinalIgnoreCase));
            if (fallback != null)
            {
                resourceType = fallback.ResourceType;
                return true;
            }

            resourceType = raw;
            return false;
        }

        private static bool TryResolveLawEffectScopeFromInput(string input, out List<string> resources, out string error)
        {
            resources = new List<string>();
            error = string.Empty;

            string rawInput = (input ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(rawInput) ||
                rawInput == "*" ||
                rawInput.Equals("all", StringComparison.OrdinalIgnoreCase) ||
                rawInput.Equals("всі", StringComparison.OrdinalIgnoreCase) ||
                rawInput.Equals("усі", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            var allResources = EconomyResourcesData.GetAll()
                .OrderBy(r => r.ResourceType, StringComparer.OrdinalIgnoreCase)
                .ToList();
            if (allResources.Count == 0)
            {
                error = "У довіднику економіки ще немає ресурсів.";
                return false;
            }

            var unique = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var unknown = new List<string>();
            var tokens = rawInput.Split(new[] { ',', ';', '|', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string token in tokens)
            {
                string trimmed = (token ?? string.Empty).Trim();
                if (string.IsNullOrWhiteSpace(trimmed))
                    continue;

                if (TryResolveLawResourceToken(trimmed, allResources, out string resolved))
                {
                    if (unique.Add(resolved))
                        resources.Add(resolved);
                }
                else
                {
                    unknown.Add(trimmed);
                }
            }

            if (unknown.Count > 0)
            {
                error = $"Невідомі ресурси: {string.Join(", ", unknown)}.";
                return false;
            }

            resources.Sort(StringComparer.OrdinalIgnoreCase);
            return true;
        }

        private static string FormatLawResourceTypeUk(string resourceType)
        {
            string key = NormalizeLawResourceInputKey(resourceType);
            return key switch
            {
                "food" => "Їжа",
                "clothing" => "Одяг",
                "housing" => "Житло",
                "tools" => "Інструменти",
                "goods" => "Товари",
                "luxury" => "Розкіш",
                "weapons" => "Зброя",
                "ammunition" => "Боєприпаси",
                "armor" => "Броня",
                "artillery" => "Артилерія",
                "support" => "Забезпечення",
                _ => resourceType
            };
        }

        private static string BuildLawEffectScopeLabel(string effectType)
        {
            return TryGetLawEffectScopeResources(effectType, out _, out var resources) && resources.Count > 0
                ? string.Join(", ", resources.Select(FormatLawResourceTypeUk))
                : "усі товари";
        }

        private sealed class LawEffectEntry
        {
            public string EffectType { get; set; } = string.Empty;
            public decimal? EffectValue { get; set; }
        }

        private static bool CanLawEffectUseValue(string effectType)
        {
            string normalized = GetLawEffectBaseType(effectType);
            if (string.IsNullOrWhiteSpace(normalized))
                return false;

            if (string.Equals(normalized, NoEffectType, StringComparison.OrdinalIgnoreCase))
                return false;

            if (IsRenameCountryEffect(normalized))
                return false;

            if (PublicSystemsEngine.IsLawCode(normalized))
                return false;

            return true;
        }

        private static decimal ResolveDefaultLawEffectValueForSelection(string effectType, decimal fallbackValue)
        {
            string normalized = GetLawEffectBaseType(effectType);
            decimal fallback = decimal.Round(fallbackValue, 4, MidpointRounding.AwayFromZero);
            if (string.Equals(normalized, LimitImportEffectType, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(normalized, LimitExportEffectType, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(normalized, LimitCompanyResourceUseEffectType, StringComparison.OrdinalIgnoreCase))
            {
                return fallback > 0m ? fallback : 100m;
            }

            if (string.Equals(normalized, SocialContributionEffectType, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(normalized, GrossIncomeTaxEffectType, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(normalized, NetIncomeTaxEffectType, StringComparison.OrdinalIgnoreCase))
            {
                return fallback > 0m ? fallback : 5m;
            }

            if (string.Equals(normalized, SetPensionPaymentEffectType, StringComparison.OrdinalIgnoreCase))
            {
                return fallback > 0m ? fallback : EconomyConfigData.GetDecimal("PensionPerPensioner", 0.35m);
            }

            if (string.Equals(normalized, SetSocialSupportPaymentEffectType, StringComparison.OrdinalIgnoreCase))
            {
                return fallback > 0m ? fallback : EconomyConfigData.GetDecimal("SocialProtectedPayment", 0.45m);
            }

            return fallback;
        }

        private static bool TryParseLawEffectValue(string rawValue, out decimal value)
        {
            return decimal.TryParse(
                (rawValue ?? string.Empty).Trim().Replace(',', '.'),
                NumberStyles.Number,
                CultureInfo.InvariantCulture,
                out value
            );
        }

        private static List<LawEffectEntry> NormalizeLawEffectEntries(IEnumerable<LawEffectEntry> entries)
        {
            var normalized = new List<LawEffectEntry>();
            if (entries != null)
            {
                foreach (var entry in entries)
                {
                    string effectType = NormalizeLawEffectType(entry?.EffectType ?? string.Empty);
                    if (string.IsNullOrWhiteSpace(effectType))
                        continue;

                    decimal? effectValue = entry?.EffectValue;
                    if (!CanLawEffectUseValue(effectType))
                        effectValue = null;
                    else if (effectValue.HasValue)
                        effectValue = decimal.Round(effectValue.Value, 4, MidpointRounding.AwayFromZero);

                    string effectBaseType = GetLawEffectBaseType(effectType);
                    var existing = normalized.FirstOrDefault(existingEntry =>
                    {
                        string existingBaseType = GetLawEffectBaseType(existingEntry.EffectType);
                        return string.Equals(existingBaseType, effectBaseType, StringComparison.OrdinalIgnoreCase);
                    });
                    if (existing == null)
                    {
                        normalized.Add(new LawEffectEntry
                        {
                            EffectType = effectType,
                            EffectValue = effectValue
                        });
                    }
                    else
                    {
                        if (!string.Equals(existing.EffectType, effectType, StringComparison.OrdinalIgnoreCase))
                            existing.EffectType = effectType;
                        if (effectValue.HasValue)
                            existing.EffectValue = effectValue;
                    }
                }
            }

            if (normalized.Count > 1)
                normalized.RemoveAll(e => string.Equals(e.EffectType, NoEffectType, StringComparison.OrdinalIgnoreCase));

            if (normalized.Count == 0)
            {
                normalized.Add(new LawEffectEntry
                {
                    EffectType = NoEffectType,
                    EffectValue = null
                });
            }

            return normalized;
        }

        private static List<LawEffectEntry> ParseLawEffectEntries(string effectTypesRaw)
        {
            var entries = new List<LawEffectEntry>();
            if (string.IsNullOrWhiteSpace(effectTypesRaw))
                return NormalizeLawEffectEntries(entries);

            var parts = effectTypesRaw.Split(new[] { '|', ';', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string rawPart in parts)
            {
                string part = (rawPart ?? string.Empty).Trim();
                if (part.Length == 0)
                    continue;

                string typePart = part;
                string valuePart = string.Empty;
                int eqIndex = part.IndexOf('=');
                if (eqIndex > 0)
                {
                    typePart = part.Substring(0, eqIndex).Trim();
                    valuePart = part.Substring(eqIndex + 1).Trim();
                }
                else
                {
                    int colonIndex = part.IndexOf(':');
                    if (colonIndex > 0)
                    {
                        typePart = part.Substring(0, colonIndex).Trim();
                        valuePart = part.Substring(colonIndex + 1).Trim();
                    }
                    else if (!part.Contains(" ", StringComparison.Ordinal) &&
                             part.Contains(",", StringComparison.Ordinal) &&
                             !part.Contains("[", StringComparison.Ordinal))
                    {
                         
                        foreach (string commaToken in part.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                        {
                            string normalizedCommaType = NormalizeLawEffectType(commaToken);
                            if (!string.IsNullOrWhiteSpace(normalizedCommaType))
                            {
                                entries.Add(new LawEffectEntry
                                {
                                    EffectType = normalizedCommaType,
                                    EffectValue = null
                                });
                            }
                        }
                        continue;
                    }
                }

                string normalizedType = NormalizeLawEffectType(typePart);
                if (string.IsNullOrWhiteSpace(normalizedType))
                    continue;

                decimal? parsedValue = null;
                if (!string.IsNullOrWhiteSpace(valuePart) && TryParseLawEffectValue(valuePart, out decimal numericValue))
                    parsedValue = numericValue;

                entries.Add(new LawEffectEntry
                {
                    EffectType = normalizedType,
                    EffectValue = parsedValue
                });
            }

            return NormalizeLawEffectEntries(entries);
        }

        private static string SerializeLawEffectEntries(IEnumerable<LawEffectEntry> entries)
        {
            var normalized = NormalizeLawEffectEntries(entries);
            var parts = normalized.Select(entry =>
            {
                if (CanLawEffectUseValue(entry.EffectType) && entry.EffectValue.HasValue)
                    return $"{entry.EffectType}={entry.EffectValue.Value.ToString("0.####", CultureInfo.InvariantCulture)}";

                return entry.EffectType;
            });

            return string.Join("|", parts);
        }

        private static List<string> ParseLawEffectTypes(string effectTypesRaw)
        {
            return ParseLawEffectEntries(effectTypesRaw)
                .Select(e => e.EffectType)
                .ToList();
        }

        private static string SerializeLawEffectTypes(IEnumerable<string> effectTypes)
        {
            var entries = (effectTypes ?? Enumerable.Empty<string>())
                .Select(effectType => new LawEffectEntry
                {
                    EffectType = effectType,
                    EffectValue = null
                });
            return SerializeLawEffectEntries(entries);
        }

        private static string BuildRenameDescription(string newName, string existingDescription)
        {
            string safeName = newName.Replace('\r', ' ').Replace('\n', ' ').Trim();
            string result = $"{RenameCountryDescriptionPrefix}{safeName}";
            if (!string.IsNullOrWhiteSpace(existingDescription))
                result += "\n" + existingDescription;
            return result;
        }

        private static bool TryExtractRenameTarget(string description, out string name, out string userDescription)
        {
            name = string.Empty;
            userDescription = null;
            if (string.IsNullOrWhiteSpace(description))
                return false;

            string trimmed = description.Trim();
            if (trimmed.StartsWith(RenameCountryDescriptionPrefix, StringComparison.Ordinal))
            {
                string rest = trimmed.Substring(RenameCountryDescriptionPrefix.Length);
                int newline = rest.IndexOf('\n');
                if (newline >= 0)
                {
                    name = rest.Substring(0, newline).Trim();
                    userDescription = rest.Substring(newline + 1).Trim();
                }
                else
                {
                    name = rest.Trim();
                }
                return !string.IsNullOrWhiteSpace(name);
            }

            name = trimmed;
            return !string.IsNullOrWhiteSpace(name);
        }

        private static string BuildSingleLawEffectText(LawRecord law, string effectType, decimal? explicitValue)
        {
            decimal value = explicitValue ?? law.EffectValue;
            string baseType = GetLawEffectBaseType(effectType);
            string scopeSuffix = string.Empty;
            if (LawEffectSupportsResourceScope(baseType))
                scopeSuffix = $" ({BuildLawEffectScopeLabel(effectType)})";

            if (IsRenameCountryEffect(baseType))
            {
                if (TryExtractRenameTarget(law.Description, out var newName, out _))
                    return $"Перейменування країни → {newName}";
                return "Перейменування країни";
            }

            if (PublicSystemsEngine.IsLawCode(baseType))
                return $"Закон-гейт: {PublicSystemsEngine.GetLawDisplayNameUk(baseType)}";

            if (string.Equals(baseType, SetSoldierSalaryEffectType, StringComparison.OrdinalIgnoreCase))
                return $"Зарплата військовим → {value:0.####}";

            if (string.Equals(baseType, SetDeputySalaryEffectType, StringComparison.OrdinalIgnoreCase))
                return $"Зарплата депутатам → {value:0.####}";

            if (string.Equals(baseType, SetCivilianSalaryEffectType, StringComparison.OrdinalIgnoreCase))
                return $"Зарплата людям → {value:0.####}";

            if (string.Equals(baseType, SetPensionPaymentEffectType, StringComparison.OrdinalIgnoreCase))
                return $"Пенсія на 1 пенсіонера → {TreasuryCoinService.FormatGoldAmount(value)}";

            if (string.Equals(baseType, SetSocialSupportPaymentEffectType, StringComparison.OrdinalIgnoreCase))
                return $"Соцвиплата на 1 соцзахищеного → {TreasuryCoinService.FormatGoldAmount(value)}";

            if (string.Equals(baseType, AddTreasuryEffectType, StringComparison.OrdinalIgnoreCase))
                return $"Казна +{TreasuryCoinService.FormatGoldAmount(Math.Abs(value))}";

            if (string.Equals(baseType, RemoveTreasuryEffectType, StringComparison.OrdinalIgnoreCase))
                return $"Казна -{TreasuryCoinService.FormatGoldAmount(Math.Abs(value))}";

            if (string.Equals(baseType, AddTreasuryDailyEffectType, StringComparison.OrdinalIgnoreCase))
                return $"Казна +{TreasuryCoinService.FormatGoldAmount(Math.Abs(value))} / день";

            if (string.Equals(baseType, RemoveTreasuryDailyEffectType, StringComparison.OrdinalIgnoreCase))
                return $"Казна -{TreasuryCoinService.FormatGoldAmount(Math.Abs(value))} / день";

            if (string.Equals(baseType, LimitImportEffectType, StringComparison.OrdinalIgnoreCase))
                return $"Ліміт імпорту{scopeSuffix}: {FormatLawPercent(value)}";

            if (string.Equals(baseType, LimitExportEffectType, StringComparison.OrdinalIgnoreCase))
                return $"Ліміт експорту{scopeSuffix}: {FormatLawPercent(value)}";

            if (string.Equals(baseType, LimitCompanyResourceUseEffectType, StringComparison.OrdinalIgnoreCase))
                return $"Ліміт споживання компаній: {FormatLawPercent(value)}";

            if (string.Equals(baseType, SocialContributionEffectType, StringComparison.OrdinalIgnoreCase))
                return $"Соцвнесок із зарплат: {FormatLawSignedPercent(value)}";

            if (string.Equals(baseType, GrossIncomeTaxEffectType, StringComparison.OrdinalIgnoreCase))
                return $"Податок на брутто-дохід: {FormatLawSignedPercent(value)}";

            if (string.Equals(baseType, NetIncomeTaxEffectType, StringComparison.OrdinalIgnoreCase))
                return $"Податок на нетто-дохід: {FormatLawSignedPercent(value)}";

            if (string.Equals(baseType, NoEffectType, StringComparison.OrdinalIgnoreCase))
                return "Без ефекту";

            return $"{effectType} {value:0.####}";
        }

        private static string FormatLawPercent(decimal value)
        {
            decimal abs = Math.Abs(value);
            decimal percent = abs <= 1m ? abs * 100m : abs;
            return $"{percent:0.####}%";
        }

        private static string FormatLawSignedPercent(decimal value)
        {
            decimal abs = Math.Abs(value);
            decimal percent = abs <= 1m ? abs * 100m : abs;
            string sign = value < 0m ? "-" : "+";
            return $"{sign}{percent:0.####}%";
        }

        private static string BuildLawEffectText(LawRecord law)
        {
            var effectEntries = ParseLawEffectEntries(law.EffectType);
            if (effectEntries.Count == 0)
                return "Без ефекту";
            if (effectEntries.Count == 1)
                return BuildSingleLawEffectText(law, effectEntries[0].EffectType, effectEntries[0].EffectValue);

            var parts = effectEntries
                .Select(effect => BuildSingleLawEffectText(law, effect.EffectType, effect.EffectValue))
                .Where(text => !string.IsNullOrWhiteSpace(text))
                .ToList();

            return parts.Count == 0 ? "Без ефекту" : string.Join(" + ", parts);
        }

        private static string BuildLawDescriptionText(LawRecord law)
        {
            var effectEntries = ParseLawEffectEntries(law.EffectType);
            bool hasRename = effectEntries.Any(e => IsRenameCountryEffect(e.EffectType));
            if (hasRename && TryExtractRenameTarget(law.Description, out _, out var userDescription))
            {
                if (string.IsNullOrWhiteSpace(userDescription))
                    return "—";
                return userDescription;
            }

            return string.IsNullOrWhiteSpace(law.Description) ? "—" : law.Description;
        }

        private static void ApplyAcceptedLawEffects(LawRecord law, long userId)
        {
            if (law == null)
                return;

            var effectEntries = ParseLawEffectEntries(law.EffectType);
            foreach (var effectEntry in effectEntries)
            {
                string effectType = effectEntry.EffectType;
                string baseEffectType = GetLawEffectBaseType(effectType);
                decimal effectValue = effectEntry.EffectValue ?? law.EffectValue;

                if (IsRenameCountryEffect(baseEffectType))
                {
                    if (!TryExtractRenameTarget(law.Description, out var newName, out _))
                        continue;

                    if (!CountryNameUtils.TryNormalize(newName, out var normalized, out _))
                        continue;

                    var country = CountriesData.GetCountryById(law.CountryId);
                    if (country == null)
                        continue;

                    if (string.Equals(country.Name, normalized, StringComparison.Ordinal))
                        continue;

                    string oldName = country.Name;
                    country.Name = normalized;
                    country.LastUpdate = DateTime.UtcNow;
                    CountriesData.UpdateCountry(country);
                    AdminActionsLogData.AddLog(country.CountryId, userId, "CountryName", oldName, normalized, $"Law #{law.LawId}");
                    continue;
                }

                if (string.Equals(baseEffectType, SetSoldierSalaryEffectType, StringComparison.OrdinalIgnoreCase))
                {
                    if (effectValue < 0m)
                        continue;
                    ApplyConfigLawValue(law, userId, effectValue, "SoldierSalaryPerTurn", "LawSoldierSalary");
                    continue;
                }

                if (string.Equals(baseEffectType, SetDeputySalaryEffectType, StringComparison.OrdinalIgnoreCase))
                {
                    if (effectValue < 0m)
                        continue;
                    ApplyConfigLawValue(law, userId, effectValue, "DeputySalaryBase", "LawDeputySalary");
                    continue;
                }

                if (string.Equals(baseEffectType, SetCivilianSalaryEffectType, StringComparison.OrdinalIgnoreCase))
                {
                    if (effectValue < 0m)
                        continue;
                    if (ApplyConfigLawValue(law, userId, effectValue, "CompanyBaseSalary", "LawCivilianSalary"))
                    {
                        ApplyConfigLawValue(law, userId, effectValue, "BaseWage", "LawCivilianSalaryFallback");
                    }
                    continue;
                }

                if (string.Equals(baseEffectType, SetPensionPaymentEffectType, StringComparison.OrdinalIgnoreCase))
                {
                    if (effectValue < 0m)
                        continue;
                    ApplyConfigLawValue(law, userId, effectValue, "PensionPerPensioner", "LawPensionPayment");
                    continue;
                }

                if (string.Equals(baseEffectType, SetSocialSupportPaymentEffectType, StringComparison.OrdinalIgnoreCase))
                {
                    if (effectValue < 0m)
                        continue;
                    ApplyConfigLawValue(law, userId, effectValue, "SocialProtectedPayment", "LawSocialSupportPayment");
                    continue;
                }

                if (string.Equals(baseEffectType, AddTreasuryEffectType, StringComparison.OrdinalIgnoreCase))
                {
                    ApplyTreasuryLawDelta(law, userId, Math.Abs(effectValue));
                    continue;
                }

                if (string.Equals(baseEffectType, RemoveTreasuryEffectType, StringComparison.OrdinalIgnoreCase))
                {
                    ApplyTreasuryLawDelta(law, userId, -Math.Abs(effectValue));
                    continue;
                }
            }
        }

        private static bool ApplyConfigLawValue(LawRecord law, long userId, decimal effectValue, string configKey, string actionName)
        {
            decimal normalizedValue = decimal.Round(effectValue, 4, MidpointRounding.AwayFromZero);
            decimal oldValue = EconomyConfigData.GetDecimalForCountry(law.CountryId, configKey, EconomyConfigData.GetDecimal(configKey, 0m));
            if (!EconomyConfigData.UpsertCountryDecimal(law.CountryId, configKey, normalizedValue))
                return false;

            AdminActionsLogData.AddLog(
                law.CountryId,
                userId,
                actionName,
                oldValue.ToString(CultureInfo.InvariantCulture),
                normalizedValue.ToString(CultureInfo.InvariantCulture),
                $"Law #{law.LawId} -> Country:{law.CountryId}:{configKey}"
            );

            return true;
        }

        private static void ApplyTreasuryLawDelta(LawRecord law, long userId, decimal delta)
        {
            if (delta == 0m)
                return;

            var country = CountriesData.GetCountryById(law.CountryId);
            if (country == null)
                return;

            if (!TreasuryCoinService.TryApplyGoldDelta(country, delta, out long oldTotalCopper, out long newTotalCopper))
                return;

            country.LastUpdate = DateTime.UtcNow;
            CountriesData.UpdateCountry(country);

            decimal oldValue = oldTotalCopper / (decimal)TreasuryCoinService.CopperPerGold;
            decimal newValue = newTotalCopper / (decimal)TreasuryCoinService.CopperPerGold;

            AdminActionsLogData.AddLog(
                country.CountryId,
                userId,
                "LawTreasuryDelta",
                oldValue.ToString("0.##", CultureInfo.InvariantCulture),
                newValue.ToString("0.##", CultureInfo.InvariantCulture),
                $"Law #{law.LawId} delta={TreasuryCoinService.FormatSignedGoldAmount(delta)}"
            );
        }
    }
}
