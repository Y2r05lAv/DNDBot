using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DNDBot.Goverment
{
    public class StateDataClass
    {
         
        public string CountryName { get; set; }

         
        public string FormOfGovernment { get; set; }      
        public int GovernmentLevel { get; set; }          
        public int GovernmentXP { get; set; }          

         
        public float Stability { get; set; }              
        public float PopularLoyalty { get; set; }         
        public float EliteLoyalty { get; set; }            
        public float Corruption { get; set; }             
        public float Legitimacy { get; set; }              
        public float Control { get; set; }                 
        public float Inflation { get; set; }                 

         
        public long Treasury { get; set; }                 
        public int TaxLevel { get; set; }                  
        public float IncomePerTurn { get; set; }
        public float ExpensesPerTurn { get; set; }
        public float CorruptionLosses { get; set; }
        public float WorkRate { get; set; }                
        public long AvailableWorkers { get; set; }
        public float UnemploymentRate { get; set; }        
        public long EmployedWorkers { get; set; }
        public long UnemployedWorkers { get; set; }
        public bool WarState { get; set; }
        public int SanctionsLevel { get; set; }

         
        public long Population { get; set; }
        public float PopulationGrowth { get; set; }       
        public float PopulationGrowthBase { get; set; }   
        public float PopulationGrowthStatusModifier { get; set; }  
        public float PopulationGrowthMortalityPenalty { get; set; }  
        public float PopulationGrowthFinal { get; set; }  
        public long ChildrenPopulation { get; set; }
        public long WorkingAgePopulation { get; set; }
        public long PensionersPopulation { get; set; }
        public long SocialProtectedPopulation { get; set; }
        public float Happiness { get; set; }
        public float Unrest { get; set; }                   
        public long Refugees { get; set; }
        public float CrimeLevel { get; set; }
        public float PoliceEffectiveness { get; set; }

         
        public float NobilityInfluence { get; set; }
        public float MerchantsInfluence { get; set; }
        public float ClergyInfluence { get; set; }
        public float MilitaryInfluence { get; set; }
        public float MagesInfluence { get; set; }
        public float PeasantsInfluence { get; set; }
        public float ExternalInfluence { get; set; }

         
        public bool HasParliament { get; set; }
        public int ParliamentSeats { get; set; }
        public int ParliamentFractions { get; set; }
        public float ParliamentSupport { get; set; }      
        public float OppositionStrength { get; set; }

         
        public int ActiveLawsCount { get; set; }
        public int MaxLawsAllowed { get; set; }
        public bool EmergencyLawActive { get; set; }

         
        public int ArmySize { get; set; }
        public float ArmyLoyalty { get; set; }
        public float ArmyMorale { get; set; }
        public float MilitaryUpkeep { get; set; }
        public float MilitarySalarySpending { get; set; }
        public float MilitarySupplySpending { get; set; }
        public float SupplyCoverage { get; set; }
        public float WeaponQuality { get; set; }
        public float AmmunitionIssued { get; set; }
        public float FormationTimeMultiplier { get; set; }
        public float CombatEffectiveness { get; set; }
        public float MobilizationPressure { get; set; }
        public float DesertionRisk { get; set; }
        public float CommanderSkill { get; set; }
        public float CommanderLoyalty { get; set; }
        public float CommanderCorruption { get; set; }
        public float CommanderExperience { get; set; }
        public int VassalRegiments { get; set; }
        public int VassalTroops { get; set; }
        public float VassalFunding { get; set; }
        public string UnitStructureText { get; set; } = string.Empty;
        public bool MartialLaw { get; set; }

         
        public int CompaniesCount { get; set; }
        public long CompanyJobs { get; set; }
        public float CompanyAverageSalary { get; set; }
        public float CompanyAverageTaxRate { get; set; }
        public float StateOrderBudget { get; set; }
        public long StateOrderJobsCreated { get; set; }

         
        public float PensionSpending { get; set; }
        public float SocialProtectedSpending { get; set; }
        public float EducationSpending { get; set; }
        public float UnemploymentSupportSpending { get; set; }

         
        public float MagicInfluence { get; set; }
        public float ManaStability { get; set; }
        public int ActiveManaZones { get; set; }
        public float ArcaneRisk { get; set; }              

         
        public int Cities { get; set; }
        public int Villages { get; set; }
        public float InfrastructureLevel { get; set; }
        public float LogisticsEfficiency { get; set; }

         
        public int ResearchLevel { get; set; }
        public float ResearchSpeed { get; set; }
        public int CompletedResearches { get; set; }

         
        public float CoupRisk { get; set; }
        public float RebellionRisk { get; set; }
        public float AssassinationRisk { get; set; }

         
        public int Allies { get; set; }
        public int Enemies { get; set; }
        public float InternationalReputation { get; set; }

         
        public float IntelLevel { get; set; }
        public float CounterIntelLevel { get; set; }

         
        public DateTime LastUpdate { get; set; }
        public int TurnsSurvived { get; set; }

         
        public bool IsInCrisis { get; set; }
        public bool IsInGoldenAge { get; set; }
        public bool IsCollapsed { get; set; }

    }
}
