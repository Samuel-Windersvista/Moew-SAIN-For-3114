using EFT.InventoryLogic;
using SAIN.Preset.GearStealthValues;
using SAIN.SAINComponent.Classes.Info;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SAIN.Components.PlayerComponentSpace.Classes.Equipment
{
    public class AIGearModifierClass(SAINAIData sAINAIData) : AIDataBase(sAINAIData)
    {
        public float StealthModifier(float distance)
        {
            return getSightMod(distance);
        }

        private float gearStealthModifier
        {
            get
            {
                if (_calcGearTime < Time.time)
                {
                    _calcGearTime = Time.time + 1f;

                    float modifier = 1f;
                    bool success = false;
                    try
                    {
                        modifier = calcGearEffects();
                        success = true;
                    }
                    catch (Exception e)
                    {
                        Logger.LogError($"calcGearEffects failed: {e}");
                    }

                    if (success)
                    {
#if DEBUG
                        if (_nextLogTime < Time.time && modifier != 1f)
                        {
                            _nextLogTime = Time.time + 60f;
                            //Logger.LogDebug($"Stealth Mod: {modifier}");
                        }
#endif
                        _gearStealthModifier = modifier;
                    }
                    else
                    {
                        _gearStealthModifier = 1f;
                    }
                }
                return _gearStealthModifier;
            }
        }

#if DEBUG
        private static float _nextLogTime;
#endif

        private float calcGearEffects()
        {
            var stealthValues = SAINPlugin.LoadedPreset.GearStealthValuesClass.ItemStealthValues;
            float result = 1f;
            Item item;
            foreach (var value in stealthValues)
            {
                if (value.Value.Count == 0)
                    continue;

                switch (value.Key)
                {
                    case EEquipmentType.Headwear:
                        item = GearInfo.GetItem(EquipmentSlot.Headwear);
                        if (item != null)
                            result *= calcEffect(item.TemplateId, value.Value);
                        break;

                    case EEquipmentType.BackPack:
                        item = GearInfo.GetItem(EquipmentSlot.Backpack);
                        if (item != null)
                            result *= calcEffect(item.TemplateId, value.Value);
                        else
                            result *= 1.1f;
                        break;

                    case EEquipmentType.FaceCover:
                        item = GearInfo.GetItem(EquipmentSlot.FaceCover);
                        if (item != null)
                        {
                            float faceCoverValue = calcEffect(item.TemplateId, value.Value);
                            if (faceCoverValue == 1f)
                                result *= 1.05f;
                        }
                        break;

                    case EEquipmentType.Rig:
                        item = GearInfo.GetItem(EquipmentSlot.TacticalVest);
                        if (item != null)
                            result *= calcEffect(item.TemplateId, value.Value);
                        break;

                    case EEquipmentType.ArmorVest:
                        item = GearInfo.GetItem(EquipmentSlot.ArmorVest);
                        if (item != null)
                            result *= calcEffect(item.TemplateId, value.Value);
                        break;

                    case EEquipmentType.EyeWear:
                        item = GearInfo.GetItem(EquipmentSlot.Eyewear);
                        if (item != null)
                            result *= calcEffect(item.TemplateId, value.Value);
                        break;

                    default:
                        break;
                }
            }

            // Phase 2: Weapon visual exposure (long guns are more visible at range)
            var weapon = AIData?.PlayerComponent?.Equipment?.CurrentWeaponInfo;
            if (weapon != null)
            {
                float weaponMod = weapon.WeaponClass switch
                {
                    EWeaponClass.pistol => 1.00f,
                    EWeaponClass.smg => 1.00f,
                    EWeaponClass.assaultRifle => 0.95f,
                    EWeaponClass.assaultCarbine => 0.95f,
                    EWeaponClass.shotgun => 0.90f,
                    EWeaponClass.marksmanRifle => 0.82f,
                    EWeaponClass.sniperRifle => 0.70f,
                    EWeaponClass.machinegun => 0.65f,
                    EWeaponClass.grenadeLauncher => 0.55f,
                    _ => 1.00f
                };

                // Scabbard: second primary weapon on back adds exposure
                var weaponInfos = AIData?.PlayerComponent?.Equipment?.WeaponInfos;
                if (weaponInfos != null)
                {
                    foreach (var (slot, info) in weaponInfos)
                    {
                        if (slot == EquipmentSlot.SecondPrimaryWeapon && info != weapon)
                        {
                            weaponMod *= 0.90f;
                            break;
                        }
                    }
                }

                result *= weaponMod;
            }

            return result;
        }

        private float calcEffect(string id, List<ItemStealthValue> values)
        {
            foreach (var value in values)
            {
                if (value.ItemID == id)
                {
                    return value.StealthValue;
                }
            }
            return 1f;
        }

        private float _gearStealthModifier = 1f;

        private float _calcGearTime;

        private float getSightMod(float distance)
        {
            float min = 30f;
            float max = 60f;
            if (distance <= min)
            {
                return 1f;
            }

            float modifier = gearStealthModifier;

            if (distance >= max)
            {
                return modifier;
            }

            float num = max - min;
            float num2 = distance - min;
            float ratio = num2 / num;
            float result = Mathf.Lerp(1f, modifier, ratio);

            return result;
        }

    }
}