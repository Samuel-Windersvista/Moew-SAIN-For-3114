import json
import os
import re

# --- Configuration ---
OUTPUT_DIR = r"E:\云文件\GitHub\Moew-SAIN-For-3114\Preset\DefaultStealthValues"
DATA_DIR = r"E:\Game\EFT_Offline\Life_in_Norvinsk\mods\《生活在诺文斯克》-单独-现实主义-Realism数值重制+小火山汉化+兼容补丁\user\mods\SPT-Realism\db\templates\gear"

os.makedirs(OUTPUT_DIR, exist_ok=True)

# --- Armor Class Mapping to Stealth Value Tiers ---
def classify_helmet_armor(armor_class):
    """Classify helmet armor class into stealth value."""
    if not armor_class or armor_class in ["Unclassified", "Gopnik", "Spooki", ""]:
        return "none"
    
    ac = armor_class.upper().strip()
    
    # High protection (≥4)
    if ac in ["GOST 5", "GOST 5A", "GOST 6", "GOST 6A", "PM 5", "PM 8", "PM 10"]:
        return "high"
    if "V50" in ac or "17GR" in ac:
        return "high"
    
    # Medium protection (3-4)
    if ac in ["NIJ IIIA", "NIJ III", "NIJ III+", "NIJ IV", "GOST 3", "GOST 3A", "GOST 4", "PM 3", "PM 4"]:
        return "medium"
    if "AR/PD" in ac:
        return "medium"
    if "RF3" in ac:
        return "medium"
    
    # Low protection (≤2)
    if ac in ["GOST 1", "GOST 2", "GOST 2A", "NIJ II", "NIJ IIA", "PM 2"]:
        return "low"
    
    # Default fallback
    return "low"


def classify_armor_vest(armor_class):
    """Classify armor vest into stealth value category."""
    if not armor_class or armor_class in ["Unclassified", ""]:
        return "light"
    
    ac = armor_class.upper().strip()
    
    # Heavy (≥5)
    if ac in ["GOST 5", "GOST 5A", "GOST 6", "GOST 6A", "PM 5", "PM 8", "PM 10", "NIJ IV"]:
        return "heavy"
    if ac in ["XSAPI", "ESAPI"]:
        return "heavy"
    
    # Medium (3-4)
    if ac in ["GOST 3", "GOST 3A", "GOST 4", "NIJ III", "NIJ III+", "NIJ IIIA"]:
        return "medium"
    if "RF3" in ac:
        return "medium"
    
    # Light (≤2)
    if ac in ["GOST 1", "GOST 2", "GOST 2A", "NIJ II", "NIJ IIA"]:
        return "light"
    
    # Default
    return "light"


def get_stealth_for_helmet(armor_class, name):
    """Calculate stealth value for a helmet."""
    category = classify_helmet_armor(armor_class)
    
    # Check for special bright/UN helmets
    name_lower = name.lower()
    is_un = "un" in name_lower and "helmet" in name_lower
    is_bomber = "bomber" in name_lower
    is_crew = "crew" in name_lower
    is_jack = "jack_o" in name_lower
    
    if category == "none":
        if is_bomber or is_crew or is_jack:
            return 1.10  # light non-armor headwear
        return 1.05  # unarmored
    
    elif category == "low":
        if is_un:
            return 0.82  # bright blue UN helmet
        return 1.05  # light helmet
    
    elif category == "medium":
        if armor_class == "NIJ II":
            return 0.95  # lighter medium
        if "IIIA" in armor_class.upper():
            return 0.92
        if "GOST 2" in armor_class.upper():
            return 0.93
        return 0.92
    
    elif category == "high":
        if "GOST 5" in armor_class.upper() or "GOST 6" in armor_class.upper():
            return 0.85
        if "PM" in armor_class.upper():
            return 0.85
        return 0.88


def get_stealth_for_armor(armor_class):
    """Calculate stealth value for armor vest."""
    category = classify_armor_vest(armor_class)
    
    if category == "heavy":
        ac = armor_class.upper().strip()
        if ac in ["GOST 6A", "PM 10", "XSAPI", "ESAPI"]:
            return 0.78  # heaviest
        if ac in ["GOST 5", "GOST 5A", "PM 5", "PM 8"]:
            return 0.80
        if "NIJ IV" in ac:
            return 0.82
        return 0.82
    
    elif category == "medium":
        ac = armor_class.upper().strip()
        if ac in ["GOST 4", "NIJ III+"]:
            return 0.88
        if ac in ["GOST 3", "GOST 3A"]:
            return 0.90
        if "NIJ III" in ac:
            return 0.90
        if "RF3" in ac or "IIIA" in ac:
            return 0.92
        return 0.90
    
    elif category == "light":
        ac = armor_class.upper().strip()
        if ac in ["GOST 2", "GOST 2A", "NIJ II"]:
            return 0.95
        if "NIJ IIA" in ac:
            return 0.98
        return 0.95


def get_stealth_for_rig(comfort, speed_penalty, name):
    """Calculate stealth value for chest rig based on size indicators."""
    name_lower = name.lower()
    
    # Small/compact rigs
    if any(kw in name_lower for kw in ["micro", "bankrobber", "zulu", "thunderbolt", "d3crx", "sprofi_recon"]):
        return 1.02
    if any(kw in name_lower for kw in ["cs_assault", "sling"]):
        return 1.05
    
    # Large/military rigs
    if any(kw in name_lower for kw in ["commando", "trooper", "boss", "triton", "mk3", "mk2", "tarzan", "bssmk1", "bearing"]):
        return 0.90
    if any(kw in name_lower for kw in ["multipurpose", "azimut", "beltab", "6h112", "wartech_vest"]):
        return 0.95
    
    # Use comfort and speed penalty as size proxies
    if speed_penalty < -0.6:
        return 0.92  # larger rig
    elif speed_penalty < -0.2:
        return 0.96  # medium rig
    else:
        return 1.02  # small rig


def get_stealth_for_backpack(comfort, speed_penalty, name):
    """Calculate stealth value for backpack based on size indicators."""
    name_lower = name.lower()
    
    # Tactical sling / very small
    if any(kw in name_lower for kw in ["sling", "takedown", "daypack", "drawbridge", "medpack", "gunslinger"]):
        return 1.05
    if any(kw in name_lower for kw in ["gr99t20", "f5switchblade", "wild", "redfox"]):
        return 1.02
    
    # Medium (20-30 slots roughly)
    if any(kw in name_lower for kw in ["blackjack", "trooper", "pillbox", "lbt1476", "wartech", "dragon"]):
        return 0.90
    if any(kw in name_lower for kw in ["gr99_t30", "paratus", "satl", "camelback", "standart"]):
        return 0.88
    
    # Large (>30 slots)
    if any(kw in name_lower for kw in ["bigbackpack", "pilgrim", "mech", "f4terminator", "betav2", "tehinkom"]):
        return 0.82
    if any(kw in name_lower for kw in ["molle", "vkboarmy", "6sh118", "sso_attack", "boss", "terraframe"]):
        return 0.85
    if any(kw in name_lower for kw in ["forward_backpack", "tactical_backpack", "standart"]):
        return 0.88
    
    # Use speed penalty as general proxy
    if speed_penalty < -3.0:
        return 0.85  # large
    elif speed_penalty < -2.0:
        return 0.92  # medium
    else:
        return 1.02  # small


def get_stealth_for_facecover(armor_class, blocks_mouth, name):
    """Calculate stealth value for face cover."""
    name_lower = name.lower()
    
    # Heavy armored face masks
    if any(kw in name_lower for kw in ["atomic", "glorious", "death_mask"]):
        if "IIIA" in (armor_class or "").upper():
            return 0.90  # heavy armored mask
        return 0.95
    
    # Shattered mask / partial
    if "shattered" in name_lower:
        return 0.95
    
    # Glass/glasses - minimal
    if "glass" in name_lower or "glasses" in name_lower or "oakley" in name_lower or "m_frame" in name_lower:
        return 1.02
    
    # Regular face covers
    if blocks_mouth:
        return 0.95
    else:
        return 1.00


# --- Equipment type mapping ---
EQUIPMENT_TYPES = {
    "Headwear": "helmetTemplates.json",
    "ArmorVest": "armorVestsTemplates.json",
    "Rig": "chestrigTemplates.json",
    "BackPack": "bagTemplates.json",
    "FaceCover": "armorMasksTemplates.json"
}

# --- Process each file ---
stats = {}

for eq_type, filename in EQUIPMENT_TYPES.items():
    filepath = os.path.join(DATA_DIR, filename)
    with open(filepath, 'r', encoding='utf-8') as f:
        data = json.load(f)
    
    items_processed = []
    
    for item_id, item_data in data.items():
        name = item_data.get("Name", "unknown")
        armor_class = item_data.get("ArmorClass", "")
        comfort = item_data.get("Comfort", 1.0)
        speed_penalty = item_data.get("speedPenaltyPercent", 0)
        blocks_mouth = item_data.get("BlocksMouth", False)
        
        # Calculate stealth value based on type
        if eq_type == "Headwear":
            stealth = get_stealth_for_helmet(armor_class, name)
        elif eq_type == "ArmorVest":
            stealth = get_stealth_for_armor(armor_class)
        elif eq_type == "Rig":
            stealth = get_stealth_for_rig(comfort, speed_penalty, name)
        elif eq_type == "BackPack":
            stealth = get_stealth_for_backpack(comfort, speed_penalty, name)
        elif eq_type == "FaceCover":
            stealth = get_stealth_for_facecover(armor_class, blocks_mouth, name)
        else:
            stealth = 1.0
        
        stealth = round(stealth, 2)
        
        output = {
            "Name": name,
            "EquipmentType": eq_type,
            "ItemID": item_id,
            "StealthValue": stealth
        }
        
        # Write individual file
        filename_out = f"{name}.json"
        # Sanitize filename
        filename_out = re.sub(r'[<>:"/\\|?*]', '_', filename_out)
        filepath_out = os.path.join(OUTPUT_DIR, filename_out)
        
        with open(filepath_out, 'w', encoding='utf-8') as f:
            json.dump(output, f, indent=2, ensure_ascii=False)
        
        items_processed.append({
            "name": name,
            "armor_class": armor_class,
            "stealth": stealth
        })
    
    # Calculate stats
    values = [i["stealth"] for i in items_processed]
    min_v = min(values)
    max_v = max(values)
    
    # Representative samples
    sorted_items = sorted(items_processed, key=lambda x: x["stealth"])
    low_samples = [i for i in sorted_items if i["stealth"] <= 0.90][:3]
    mid_samples = [i for i in sorted_items if 0.91 <= i["stealth"] <= 1.00][:3]
    high_samples = [i for i in sorted_items if i["stealth"] >= 1.01][:3]
    
    representatives = low_samples + mid_samples + high_samples
    
    stats[eq_type] = {
        "count": len(items_processed),
        "min_stealth": min_v,
        "max_stealth": max_v,
        "representatives": representatives
    }

# --- Print summary report ---
print("=" * 80)
print("ITEM STEALTH VALUES GENERATION REPORT")
print("=" * 80)

for eq_type, s in stats.items():
    print(f"\n--- {eq_type} ---")
    print(f"  Total items: {s['count']}")
    print(f"  Stealth range: {s['min_stealth']} - {s['max_stealth']}")
    print(f"  Representative samples:")
    for r in s['representatives'][:5]:
        print(f"    - {r['name']:45s} (AC: {str(r['armor_class']):20s}) -> {r['stealth']}")

print("\n" + "=" * 80)
print("Output directory:", OUTPUT_DIR)
print(f"Total files written: {sum(s['count'] for s in stats.values())}")

# --- Write a summary report file ---
with open(os.path.join(OUTPUT_DIR, "_summary_report.txt"), 'w', encoding='utf-8') as f:
    f.write("ITEM STEALTH VALUES GENERATION REPORT\n")
    f.write("=" * 80 + "\n")
    for eq_type, s in stats.items():
        f.write(f"\n--- {eq_type} ---\n")
        f.write(f"  Total items: {s['count']}\n")
        f.write(f"  Stealth range: {s['min_stealth']} - {s['max_stealth']}\n")
        f.write(f"  Representative samples:\n")
        for r in s['representatives'][:5]:
            f.write(f"    - {r['name']:45s} (AC: {str(r['armor_class']):20s}) -> {r['stealth']}\n")

print("Done!")
