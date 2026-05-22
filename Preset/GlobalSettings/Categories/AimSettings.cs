using SAIN.Attributes;
using System.Collections.Generic;

namespace SAIN.Preset.GlobalSettings
{
    public class AimSettings : SAINSettingsBase<AimSettings>, ISAINSettings
    {
        [Category("瞄准目标")]
        public HitEffectSettings HitEffects = new();

        [Name("全局始终瞄准躯干中央")]
        [Description("强制Bot瞄准躯干中央。关闭后所有Bot的此选项将被设为关，个人设置将被忽略。")]
        [Category("瞄准目标")]
        public bool AimCenterMassGlobal = true;

        [Category("散布修正")]
        [Name("敌人移动散布最大增益")]
        [Description("敌人静止时Bot散布的最大增益。随敌人速度缩放。值1=禁用。")]
        [MinMax(1f, 1.5f, 100f)]
        public float EnemyVelocityMaxBuff = 1.2f;

        [Category("散布修正")]
        [Name("敌人移动散布最大减益")]
        [Description("敌人全速移动(不冲刺)时Bot散布的最大减益。随敌人速度缩放。值1=禁用。")]
        [MinMax(0.5f, 1f, 100f)]
        public float EnemyVelocityMaxDebuff = 0.8f;

        [Category("散布修正")]
        [Name("敌人冲刺散布减益")]
        [Description("敌人冲刺时Bot散布除以此值。数值越小=瞄准越差。值1=禁用。")]
        [MinMax(0.25f, 1f, 100f)]
        public float EnemySprintingScatterMulti = 0.66f;

        [Category("散布修正")]
        [Name("姿态散布乘数")]
        [Description("数值越低=散布越小。Bot蹲下时散布最多减少到此乘数。1.2=散布减少20%。")]
        [Advanced]
        [MinMax(1f, 2f, 100f)]
        public float ScatterMulti_PoseLevel = 1.2f;

        [Category("散布修正")]
        [Name("趴下散布乘数")]
        [Description("数值越低=散布越小。Bot趴下时散布最多减少到此乘数。1.3=散布减少30%。")]
        [Advanced]
        [MinMax(1f, 2f, 100f)]
        public float ScatterMulti_Prone = 1.3f;

        [Category("散布修正")]
        [Name("身体部位可见度散布乘数")]
        [Description("数值越低=散布越小。敌人所有身体部位可见时散布最多减少到此乘数。1.25=散布减少25%。")]
        [Advanced]
        [MinMax(1f, 2f, 100f)]
        public float ScatterMulti_PartVis = 1.25f;

        [Category("散布修正")]
        [Name("倍镜-理想距离-最大增益")]
        [Description("数值越低=散布越小。目标距离>=倍镜理想距离时散布减少到此乘数。1.2=散布减少20%。")]
        [Advanced]
        [MinMax(1f, 1.5f, 100f)]
        public float OpticFarMulti = 1.2f;

        [Category("散布修正")]
        [Name("倍镜-理想距离-距离值")]
        [Description("倍镜的理想射击距离(米)。目标在此距离或以上时减小散布。")]
        [Advanced]
        [MinMax(25f, 150f, 10f)]
        public float OpticFarDistance = 100f;

        [Category("散布修正")]
        [Name("倍镜-过近距离-最大减益")]
        [Description("数值越低=散布越大。目标距离<=倍镜过近距离时散布增加到此乘数。0.8=散布增大20%。")]
        [Advanced]
        [MinMax(0.5f, 1f, 100f)]
        public float OpticCloseMulti = 0.8f;

        [Category("散布修正")]
        [Name("倍镜-过近距离-距离值")]
        [Description("定义倍镜过近的距离(米)。目标距离小于等于此值时散布增大。")]
        [Advanced]
        [MinMax(25f, 150f, 10f)]
        public float OpticCloseDistance = 75f;

        [Category("散布修正")]
        [Name("红点/全息-超距-最大减益")]
        [Description("数值越低=散布越大。目标距离>=红点/全息超距距离时散布增加到此乘数。0.85=散布增大15%。")]
        [Advanced]
        [MinMax(0.5f, 1f, 100f)]
        public float RedDotFarMulti = 0.85f;

        [Category("散布修正")]
        [Name("红点/全息-超距-距离值")]
        [Description("定义为红点/全息超距的距离(米)。目标距离大于等于此值时散布按超距最大减益增加。")]
        [Advanced]
        [MinMax(25f, 150f, 10f)]
        public float RedDotFarDistance = 100f;

        [Category("散布修正")]
        [Name("红点/全息-理想距离-最大增益")]
        [Description("数值越低=散布越小。目标距离>=理想距离时散布减少到此乘数。1.15=散布减少15%。")]
        [Advanced]
        [MinMax(1f, 1.5f, 100f)]
        public float RedDotCloseMulti = 1.15f;

        [Category("散布修正")]
        [Name("红点/全息-理想距离-距离值")]
        [Description("定义为红点/全息理想范围的距离(米)。目标距离小于等于此值时散布按理想距离最大增益减小。")]
        [Advanced]
        [MinMax(25f, 150f, 10f)]
        public float RedDotCloseDistance = 50f;

        [Category("散布修正")]
        [Name("机瞄-超距-最大减益")]
        [Description("数值越低=散布越大。目标距离>=理想距离时散布增加到此乘数。0.7=散布增大30%。")]
        [Advanced]
        [MinMax(0.5f, 1f, 100f)]
        public float IronSightFarMulti = 0.7f;

        [Category("散布修正")]
        [Name("机瞄-超距-缩放起始距离")]
        [Description("定义为机瞄超距缩放起始距离(米)。目标距离大于等于此值时开始线性增加散布至结束距离。")]
        [Advanced]
        [MinMax(25f, 200f, 10f)]
        public float IronSightScaleDistanceStart = 40f;

        [Category("散布修正")]
        [Name("机瞄-超距-缩放结束距离")]
        [Description("定义为机瞄超距缩放结束距离(米)。目标距离大于等于此值时散布增加至机瞄超距最大减益。")]
        [Advanced]
        [MinMax(25f, 200f, 10f)]
        public float IronSightScaleDistanceEnd = 75f;

        [Name("躯干中央瞄准点")]
        [Description("全局始终瞄准躯干中央开启时Bot瞄准的最大高度。0=头部正中心，1=脚下地面。若目标高于此点则调整为该点高度。")]
        [Category("瞄准目标")]
        [Advanced]
        [MinMax(0f, 1f, 10000f)]
        public float CenterMassVal = 0.3f;

        [Category("瞄准时间")]
        [Name("全局快速近战反应")]
        [Description("如果此开关关闭，所有Bot的快速近战反应将被设为关，个人设置将被忽略。")]
        public bool FasterCQBReactionsGlobal = true;

        [Category("瞄准时间")]
        [Name("开镜瞄准时间乘数")]
        [Description("Bot处于开镜状态时，瞄准时间将乘以此值。")]
        [MinMax(0.01f, 1f, 100f)]
        public float AimDownSightsAimTimeMultiplier = 0.7f;


        public override void Init(List<ISAINSettings> list)
        {
            list.Add(this);
            HitEffects.Init(list);
        }
    }
}