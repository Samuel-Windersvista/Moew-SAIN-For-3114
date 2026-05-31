# SAIN 武器隐蔽值扩展 — 理事会审阅报告

> 审阅日期: 2026-05-31 | 议员: alpha(deepseek-v4-pro) / beta(deepseek-v4-flash) / gamma(k2p6)
> 共识: UNANIMOUS（编译错误必须修 + 基础系统必须先修）

---

## 审阅结论汇总

### 三个编译错误（3/3 一致）

| 错误 | 修正 |
|------|------|
| `EWeaponClass.DMR` 不存在 | 改用 `EWeaponClass.marksmanRifle` |
| switch 大小写全部错误（`Pistol`→`pistol`） | 全部改为小驼峰匹配枚举定义 |
| `EquipmentSlot.Scabbard` 不存在 | 检测 `WeaponInfos` 中的 `SecondPrimaryWeapon` |

### 前置基础清理（3/3 一致）

| 清理项 | 理由 |
|--------|------|
| 删除 `AIGearModifierClass` 中 `getBackpackMod/getHeadWearMod/getFaceCoverMod` 硬编码 | 与 `calcGearEffects` 重复维护 |
| 删除 `GearStealthValuesClass.initDefaults()` 改为纯 JSON 驱动 | 硬编码污染 JSON 架构 |
| 补全 FaceCover/EyeWear/ArmorVest/Rig 默认条目 | 当前 4 类完全空白 |
| 修复 `calcGearEffects` fallback 路径覆盖全部 6 类 | 当前只覆盖 3 类 |
| 修复空 `catch {}` 加错误日志 | 异常静默吞噬 |

### 数值调整（多数共识）

| 调整项 | 原值 | 新值 |
|--------|------|------|
| Pistol/SMG 听觉乘数 | 0.85 | 0.90 |
| AssaultCarbine 听觉乘数 | 1.05 | 1.00 |
| MachineGun 听觉乘数 | 1.40 | 1.30 |
| SniperRifle 视觉乘数 | 0.65 | 0.70 |
| MachineGun 视觉乘数 | 0.60 | 0.65 |
| 背挂听觉加成 | 1.10 | 1.15 |
| 背挂视觉加成 | 0.85 | 0.90 |

### 设计决策

- **不加 IsAI 限制**（2/3）: `AILimitSetting` 已有远端裁剪兜底
- **switch 改 Dictionary**（1/3）: gamma 建议，实施时评估
- **武器暴露总开关分开**（2/3）: 听觉视觉独立控制
- **乘数范围**（gamma）: 听觉 [0.90, 1.30] / 视觉 [0.70, 1.00]

---

## 议员特色贡献

| 议员 | 核心贡献 |
|------|---------|
| alpha | 发现 `GearInfo` 代码路径错误 + 3 处硬编码重复 + `grenadeLauncher/specialWeapon` 未覆盖 |
| beta | 发现 `DMR` 枚举不存在 + 消音器视觉乘数建议 + `AssaultCarbine` 值争议 |
| gamma | 发现大小写编译错误 + 乘数范围建议 + Dictionary 替代 switch 建议 |
