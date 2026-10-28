using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DNDBot.Goverment
{
    public class StateMessageBuilder
    {
        public static string BuildMainStateMessage(StateDataClass state)
        {
            return
$@"📊 Стан держави

Форма правління: {state.FormOfGovernment}
Стабільність: {GetStatusEmoji(state.Stability)} {state.Stability}%
Лояльність населення: {GetStatusEmoji(state.PopularLoyalty)} {state.PopularLoyalty}%
Корупція: {GetStatusEmoji(100 - state.Corruption)} {state.Corruption}%

💰 Казна: {state.Treasury:N0} золота
👥 Населення: {state.Population:N0}
👶/👨‍💼/👴/♿: {state.ChildrenPopulation:N0} / {state.WorkingAgePopulation:N0} / {state.PensionersPopulation:N0} / {state.SocialProtectedPopulation:N0}
📈 Мод. населення: база {FormatSigned(state.PopulationGrowthBase)}% | стан {FormatSigned(state.PopulationGrowthStatusModifier)}% | штраф смертності {FormatPenalty(state.PopulationGrowthMortalityPenalty)}% | фінал {FormatSigned(state.PopulationGrowthFinal)}% / день
🧑‍🏭 Робоча сила: {state.AvailableWorkers:N0} ({state.WorkRate:0.##}%)
✅ Працюють / безробітні: {state.EmployedWorkers:N0} / {state.UnemployedWorkers:N0}
📉 Безробіття: {state.UnemploymentRate:0.##}%
🏭 Компанії: {state.CompaniesCount} (місця: {state.CompanyJobs:N0})
📦 Держзамовлення: {state.StateOrderBudget:N0} (нових місць: {state.StateOrderJobsCreated:N0})
🛡 Боєздатність: {state.CombatEffectiveness:0.##}% | Якість зброї: {state.WeaponQuality:0.##}%
🚔 Злочинність/поліція: {state.CrimeLevel:0.##}% / {state.PoliceEffectiveness:0.##}%";
        }

        public static string BuildOverviewStateMessage(StateDataClass state)
        {
            return
$@"🧭 Огляд держави
Країна: {state.CountryName}

🏛 Управління
Форма правління: {state.FormOfGovernment}
Стабільність: {GetStatusEmoji(state.Stability)} {state.Stability}%
Лояльність населення: {GetStatusEmoji(state.PopularLoyalty)} {state.PopularLoyalty}%
Корупція: {GetStatusEmoji(100 - state.Corruption)} {state.Corruption}%
Легітимність: {GetStatusEmoji(state.Legitimacy)} {state.Legitimacy}%
Контроль: {GetStatusEmoji(state.Control)} {state.Control}%

💰 Економіка
Казна: {state.Treasury:N0} золота
Податки: {state.TaxLevel}%
Доходи / витрати: {state.IncomePerTurn:N0} / {state.ExpensesPerTurn:N0} за день
Соцвитрати (пенсії/соцзахист/освіта/безробіття):
{state.PensionSpending:N0} / {state.SocialProtectedSpending:N0} / {state.EducationSpending:N0} / {state.UnemploymentSupportSpending:N0}
Військові витрати (утримання/зарплати/забезпечення):
{state.MilitaryUpkeep:N0} / {state.MilitarySalarySpending:N0} / {state.MilitarySupplySpending:N0}
Держзамовлення: {state.StateOrderBudget:N0}
Інфляція: {state.Inflation}%
Безробіття: {state.UnemploymentRate:0.##}%
Війна: {(state.WarState ? "так" : "ні")}
Санкції: {FormatSanctionsLevel(state.SanctionsLevel)}

👥 Населення
Населення: {state.Population:N0}
👶 Діти: {state.ChildrenPopulation:N0}
👨‍💼 Працездатні: {state.WorkingAgePopulation:N0}
👴 Пенсіонери: {state.PensionersPopulation:N0}
♿ Соцзахищені: {state.SocialProtectedPopulation:N0}
Працюють / безробітні: {state.EmployedWorkers:N0} / {state.UnemployedWorkers:N0}
Приріст населення (база): {FormatSigned(state.PopulationGrowthBase)}% / день
Модифікатор стану країни: {FormatSigned(state.PopulationGrowthStatusModifier)}% / день
Штраф смертності: {FormatPenalty(state.PopulationGrowthMortalityPenalty)}% / день
Фінальний модифікатор населення: {FormatSigned(state.PopulationGrowthFinal)}% / день
Щастя: {GetStatusEmoji(state.Happiness)} {state.Happiness}%
Соц. напруга: {GetStatusEmoji(100 - state.Unrest)} {state.Unrest}%
Злочинність / поліція: {state.CrimeLevel:0.##}% / {state.PoliceEffectiveness:0.##}%
Біженці: {state.Refugees:N0}

🏭 Компанії
Кількість: {state.CompaniesCount}
Робочі місця: {state.CompanyJobs:N0}
Середня зарплата: {state.CompanyAverageSalary:0.##}
Середній податок компаній: {state.CompanyAverageTaxRate:0.##}%

🛡 Військо
Армія: {state.ArmySize:N0}
Забезпечення: {state.SupplyCoverage:0.##}% | Амуніція: {state.AmmunitionIssued:0.##}%
Якість зброї: {state.WeaponQuality:0.##}% | Боєздатність: {state.CombatEffectiveness:0.##}%
Тиск мобілізації: {state.MobilizationPressure:0.##}% | Ризик дезертирства: {state.DesertionRisk:0.##}%
Час формування частин (множник): x{state.FormationTimeMultiplier:0.##}
Командування (навички/лояльність/корупція/досвід):
{state.CommanderSkill:0.##}% / {state.CommanderLoyalty:0.##}% / {state.CommanderCorruption:0.##}% / {state.CommanderExperience:0.##}%
Структура: {state.UnitStructureText}
Васальні полки / фінансування: {state.VassalRegiments} / {state.VassalFunding:N0}

🏗 Інфраструктура
Міста / села: {state.Cities:N0} / {state.Villages:N0}
Рівень інфраструктури: {state.InfrastructureLevel}%
Логістика: {state.LogisticsEfficiency}%

🏛 Парламент
Наявність: {(state.HasParliament ? "так" : "ні")}
Ліміт мандатів / ціль фракцій: {state.ParliamentSeats} / {state.ParliamentFractions}
Підтримка парламенту: {state.ParliamentSupport}%
Опозиція: {state.OppositionStrength}%
Закони: {state.ActiveLawsCount}/{state.MaxLawsAllowed}
Надзвичайний стан: {(state.EmergencyLawActive ? "так" : "ні")}

🤝 Дипломатія
Союзники / вороги: {state.Allies} / {state.Enemies}
Репутація: {state.InternationalReputation}%

";
        }



        private static string FormatSanctionsLevel(int level)
        {
            return level switch
            {
                1 => "Легкі",
                2 => "Середні",
                3 => "Жорсткі",
                _ => "Немає"
            };
        }

        private static string GetStatusEmoji(float value)
        {
            if (value >= 70) return "🟢";
            if (value >= 40) return "🟡";
            if (value >= 20) return "🟠";
            return "🔴";
        }

        private static string FormatSigned(float value)
        {
            if (value > 0f) return $"+{value:0.##}";
            if (value < 0f) return $"{value:0.##}";
            return "0";
        }

        private static string FormatPenalty(float value)
        {
            if (value <= 0f)
                return "0";
            return $"-{value:0.##}";
        }
    }
}
