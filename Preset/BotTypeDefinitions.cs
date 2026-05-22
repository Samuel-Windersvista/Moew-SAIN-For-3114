using EFT;
using SAIN.Components.BotController;
using SAIN.Helpers;
using System.Collections.Generic;

namespace SAIN.Preset
{
    public sealed class BotType
    {
        public string Name;
        public string Description;
        public string Section;
        public WildSpawnType WildSpawnType;
        public string BaseBrain;
    }

    public class BotTypeDefinitions
    {
        public static Dictionary<WildSpawnType, BotType> BotTypes = new();
        public static List<BotType> BotTypesList;
        public static readonly List<string> BotTypesNames = new();

        static BotTypeDefinitions()
        {
            BotTypesList = ImportBotTypes();
            for (int i = 0; i < BotTypesList.Count; i++)
            {
                BotType botType = BotTypesList[i];
                WildSpawnType wildSpawn = botType.WildSpawnType;

                BotTypesNames.Add(botType.Name);
                BotTypes.Add(wildSpawn, botType);
            }
        }

        private static readonly string FileName = "BotTypes";

        public static List<BotType> ImportBotTypes()
        {
            List<BotType> tempList = CreateBotTypes();
            removeExcluded(tempList, out _);

            if (JsonUtility.Load.LoadObject(out List<BotType> importedList, FileName))
            {
                // Check that the imported list contains each entry created, to account for BotTypes being added with newer versions of EFT
                CheckImportedList(importedList, tempList);
                return importedList;
            }
            else
            {
                JsonUtility.SaveObjectToJson(tempList, FileName);
                return tempList;
            }
        }

        private static void CheckImportedList(List<BotType> importedList, List<BotType> tempList)
        {
            for (int i = 0; i < tempList.Count; i++)
            {
                bool alreadyExists = false;
                for (int j = 0; j < importedList.Count; j++)
                {
                    if (tempList[i].WildSpawnType == importedList[j].WildSpawnType)
                    {
                        alreadyExists = true;
                        break;
                    }
                }
                if (!alreadyExists)
                {
                    importedList.Add(tempList[i]);
                }

            }
            removeExcluded(importedList, out bool removed);
            if (removed)
            {
                JsonUtility.SaveObjectToJson(importedList, FileName);
            }
        }

        private static void removeExcluded(List<BotType> list, out bool removed)
        {
            removed = false;
            for (int i = list.Count - 1; i >= 0; i--)
            {
                if (BotSpawnController.StrictExclusionList.Contains(list[i].WildSpawnType))
                {
                    list.RemoveAt(i);
                    removed = true;
                }
            }
        }

        private static readonly List<BotType> _typesToRemove = new();

        public static void ExportBotTypes()
        {
            JsonUtility.SaveObjectToJson(BotTypesList, FileName);
        }

        public static BotType GetBotType(WildSpawnType wildSpawnType)
        {
            if (BotTypes.ContainsKey(wildSpawnType))
            {
                return BotTypes[wildSpawnType];
            }
            else
            {
#if DEBUG
                Logger.LogError($"WildSpawnType {wildSpawnType} does not exist in BotType Dictionary");
#endif
                return BotTypes[WildSpawnType.assault];
            }
        }

        static List<BotType> CreateBotTypes()
        {
            return new List<BotType>
            {
                new BotType{ WildSpawnType = WildSpawnType.assault,                 Name = "Scav",                     Section = "Scav阵营" ,    Description = "Scavs!" },
                new BotType{ WildSpawnType = WildSpawnType.assaultGroup,            Name = "Scav Group",               Section = "Scav阵营" ,    Description = "Scavs in a Group!" },
                new BotType{ WildSpawnType = WildSpawnType.crazyAssaultEvent,       Name = "Crazy Scav Event",         Section = "Scav阵营" ,    Description = "Scavs!" },
                new BotType{ WildSpawnType = WildSpawnType.pmcUSEC,                 Name = "Usec",                     Section = "PMC阵营" ,     Description = "A PMC of the Usec Faction" },
                new BotType{ WildSpawnType = WildSpawnType.pmcBEAR,                 Name = "Bear",                     Section = "PMC阵营" ,     Description = "A PMC of the Bear Faction" },
                new BotType{ WildSpawnType = WildSpawnType.marksman,                Name = "Scav Sniper",              Section = "Scav阵营" ,    Description = "The Scav Snipers that spawn on rooftops on certain maps" },
                new BotType{ WildSpawnType = WildSpawnType.cursedAssault,           Name = "Tagged and Cursed Scav",   Section = "Scav阵营" ,    Description = "The type a scav is assigned when the player is marked as Tagged and Cursed" },
                new BotType{ WildSpawnType = WildSpawnType.bossKnight,              Name = "Knight",                   Section = "Goon小队" ,    Description = "Goons leader. Close proximity to the goons has been noted to cause smashed keyboards" },
                new BotType{ WildSpawnType = WildSpawnType.followerBigPipe,         Name = "BigPipe" ,                 Section = "Goon小队" ,    Description = "Goons follower. Close proximity to the goons has been noted to cause smashed keyboards\"" },
                new BotType{ WildSpawnType = WildSpawnType.followerBirdEye,         Name = "BirdEye",                  Section = "Goon小队" ,    Description = "Goons follower. Close proximity to the goons has been noted to cause smashed keyboards\"" },
                new BotType{ WildSpawnType = WildSpawnType.exUsec,                  Name = "Rogue",                    Section = "其他" ,        Description = "Ex Usec Personel on Lighthouse usually found around the water treatment plant" },
                new BotType{ WildSpawnType = WildSpawnType.pmcBot,                  Name = "Raider",                   Section = "其他" ,        Description = "Heavily armed scavs typically found on reserve and Labs by default" },
                new BotType{ WildSpawnType = WildSpawnType.arenaFighterEvent,       Name = "Bloodhound",               Section = "其他" ,        Description = "From the Live Event, nearly identical to raiders except with different voicelines and better gear. Found in" },
                new BotType{ WildSpawnType = WildSpawnType.sectantPriest,           Name = "Cultist Priest",           Section = "其他" ,        Description = "Found on Customs, Woods, Factory, Shoreline at night" },
                new BotType{ WildSpawnType = WildSpawnType.sectantWarrior,          Name = "Cultist",                  Section = "其他" ,        Description = "Found on Customs, Woods, Factory, Shoreline at night" },
                new BotType{ WildSpawnType = WildSpawnType.bossKilla,               Name = "Killa",                    Section = "Boss" ,        Description = "He shoot. Found on Interchange and Streets" },
				        new BotType{ WildSpawnType = WildSpawnType.bossPartisan,            Name = "Partisan",                 Section = "Boss" ,        Description = "Crazy mall santa" },

				        new BotType{ WildSpawnType = WildSpawnType.bossBully,               Name = "Rashala",                  Section = "Boss" ,        Description = "Customs Boss" },
                new BotType{ WildSpawnType = WildSpawnType.followerBully,           Name = "Rashala Guard",            Section = "随从" ,        Description = "Customs Boss Follower" },

                new BotType{ WildSpawnType = WildSpawnType.bossKojaniy,             Name = "Shturman",                 Section = "Boss" ,        Description = "Woods Boss" },
                new BotType{ WildSpawnType = WildSpawnType.followerKojaniy,         Name = "Shturman Guard",           Section = "随从" ,        Description = "Woods Boss Follower" },

                new BotType{ WildSpawnType = WildSpawnType.bossTagilla,             Name = "Tagilla",                  Section = "Boss" ,        Description = "He Smash" },
                new BotType{ WildSpawnType = WildSpawnType.followerTagilla,         Name = "Tagilla Guard",            Section = "随从" ,        Description = "They Smash Too?" },

                new BotType{ WildSpawnType = WildSpawnType.bossSanitar,             Name = "Sanitar",                  Section = "Boss" ,        Description = "Shoreline Boss" },
                new BotType{ WildSpawnType = WildSpawnType.followerSanitar,         Name = "Sanitar Guard",            Section = "随从" ,        Description = "Shoreline Boss Follower" },

                new BotType{ WildSpawnType = WildSpawnType.bossGluhar,              Name = "Gluhar",                   Section = "Boss" ,        Description = "Reserve Boss. Also can be found on Streets." },
                new BotType{ WildSpawnType = WildSpawnType.followerGluharSnipe,     Name = "Gluhar Guard Snipe",       Section = "随从" ,        Description = "Reserve Boss Follower" },
                new BotType{ WildSpawnType = WildSpawnType.followerGluharScout,     Name = "Gluhar Guard Scout",       Section = "随从" ,        Description = "Reserve Boss Follower" },
                new BotType{ WildSpawnType = WildSpawnType.followerGluharSecurity,  Name = "Gluhar Guard Security",    Section = "随从" ,        Description = "Reserve Boss Follower" },
                new BotType{ WildSpawnType = WildSpawnType.followerGluharAssault,   Name = "Gluhar Guard Assault",     Section = "随从" ,        Description = "Reserve Boss Follower" },

                new BotType{ WildSpawnType = WildSpawnType.bossZryachiy,            Name = "Zryachiy",                 Section = "Boss" ,        Description = "Lighthouse Island Sniper Boss" },
                new BotType{ WildSpawnType = WildSpawnType.followerZryachiy,        Name = "Zryachiy Guard",           Section = "随从" ,        Description = "Lighthouse Island Sniper Boss Follower" },

                new BotType{ WildSpawnType = WildSpawnType.bossBoar,                Name = "Kaban",                    Section = "Boss" ,        Description = "Streets Gangster" },
                new BotType{ WildSpawnType = WildSpawnType.followerBoar,            Name = "Kaban Guard",              Section = "随从" ,        Description = "Gangster Cannon Fodder" },
                new BotType{ WildSpawnType = WildSpawnType.followerBoarClose1,      Name = "Basmach",                  Section = "随从" ,        Description = "Gangster 1" },
                new BotType{ WildSpawnType = WildSpawnType.followerBoarClose2,      Name = "Gus",                      Section = "随从" ,        Description = "Gangster 2" },
                new BotType{ WildSpawnType = WildSpawnType.bossBoarSniper,          Name = "Kaban Sniper",             Section = "随从" ,        Description = "Gangster Sniper" },

                new BotType{ WildSpawnType = WildSpawnType.bossKolontay,            Name = "Kollontay",                Section = "Boss" ,        Description = "Crooked Cop" },
                new BotType{ WildSpawnType = WildSpawnType.followerKolontayAssault, Name = "Kollantay Assault",        Section = "随从" ,        Description = "Aggressive Guard" },
                new BotType{ WildSpawnType = WildSpawnType.followerKolontaySecurity,Name = "Kollantay Security",       Section = "随从" ,        Description = "Defensive Guard" },

                new BotType{ WildSpawnType = WildSpawnType.bossPartisan,            Name = "Partisan",                 Section = "Boss" ,        Description = "A scav legend.. who scavs hate" },
                
                new BotType{ WildSpawnType = WildSpawnType.shooterBTR,              Name = "BTR",                      Section = "其他" ,        Description = "Zoom. Zoom. Bang. Bang." },
            };
        }
    }
}