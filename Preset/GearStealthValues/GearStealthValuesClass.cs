using SAIN.Helpers;
using System;
using System.Collections.Generic;

namespace SAIN.Preset.GearStealthValues
{
    public class GearStealthValuesClass
    {
        public Dictionary<EEquipmentType, List<ItemStealthValue>> ItemStealthValues = new();
        public readonly List<ItemStealthValue> Defaults = new();

        public GearStealthValuesClass(SAINPresetDefinition preset)
        {
            try
            {
                import(preset);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
            }
            initDefaults();
            Export(this, preset);
        }

        private void import(SAINPresetDefinition preset)
        {
            if (!preset.IsCustom)
            {
                return;
            }
            if (!JsonUtility.DoesFolderExist("Presets", preset.Name, "ItemStealthValues"))
            {
                return;
            }

            var list = new List<ItemStealthValue>();
            JsonUtility.Load.LoadStealthValues(list, "Presets", preset.Name, "ItemStealthValues");
            foreach (var type in EnumValues.GetEnum<EEquipmentType>())
            {
                var itemList = getList(type);
                foreach (var item in list)
                {
                    if (item.EquipmentType != type)
                        continue;

                    Logger.LogDebug($"Adding {item.Name}");
                    addItem(item.Name, item.EquipmentType, item.ItemID, item.StealthValue, itemList);
                }
            }
        }

        public static void Export(GearStealthValuesClass stealthValues, SAINPresetDefinition preset)
        {
            if (!preset.IsCustom)
            {
                return;
            }

            JsonUtility.CreateFolder("Presets", preset.Name, "ItemStealthValues");
            JsonUtility.SaveObjectToJson(EnumValues.GetEnum<EEquipmentType>(), "Possible Item Types For Stealth Modifiers", "Presets", preset.Name);

            foreach (var list in stealthValues.ItemStealthValues.Values)
            {
                foreach (var item in list)
                {
                    JsonUtility.SaveObjectToJson(item, item.Name, "Presets", preset.Name, "ItemStealthValues");
                }
            }
        }

        private void initDefaults()
        {
            // Phase 0: Hardcoded defaults removed.
            // All stealth values now come from JSON files in Presets/{name}/ItemStealthValues/
            // If no JSON files exist, all equipment types default to 1.0 (no effect).
        }

        private List<ItemStealthValue> getList(EEquipmentType type)
        {
            if (!ItemStealthValues.TryGetValue(type, out var list))
            {
                list = new List<ItemStealthValue>();
                ItemStealthValues.Add(type, list);
            }
            return list;
        }

        private void addItem(string name, EEquipmentType type, string id, float stealthValue, List<ItemStealthValue> list, bool addAsDefault = false)
        {
            if (!doesItemExist(name, list))
            {
                list.Add(new ItemStealthValue
                {
                    Name = name,
                    EquipmentType = type,
                    ItemID = id,
                    StealthValue = stealthValue,
                });
            }
            if (addAsDefault)
            {
                addItem(name, type, id, stealthValue, Defaults, false);
            }
        }

        private bool doesItemExist(string name, List<ItemStealthValue> list)
        {
            foreach (var item in list)
            {
                if (item.Name == name)
                {
                    return true;
                }
            }
            return false;
        }

    }
}