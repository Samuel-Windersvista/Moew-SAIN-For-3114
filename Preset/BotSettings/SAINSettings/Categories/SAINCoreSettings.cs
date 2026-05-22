using EFT;
using Newtonsoft.Json;
using SAIN.Attributes;
using SAIN.Preset.GlobalSettings;

namespace SAIN.Preset.BotSettings.SAINSettings.Categories
{
    public class SAINCoreSettings : SAINSettingsBase<SAINCoreSettings>, ISAINSettings
    {
        [Category("视觉")]
        [Name("视野角度")]
        [MinMax(45f, 180f)]
        public float VisibleAngle = 160f;

        [Category("视觉")]
        [Name("基础视觉距离")]
        [MinMax(50f, 500f)]
        public float VisibleDistance = 150f;

        [Category("视觉")]
        [Name("获得视野系数")]
        [Description("默认EFT配置。影响Bot发现敌人速度。微小的改动有巨大影响。")]
        [MinMax(0.001f, 10f, 10000f)]
        [Advanced]
        public float GainSightCoef = 0.2f;

        [Category("瞄准与射击")]
        [Name("精度速度")]
        [Description("默认EFT配置。影响Bot瞄准目标的速度。")]
        [MinMax(0.01f, 10f, 100f)]
        [Advanced]
        [CopyValue]
        public float AccuratySpeed = 0.3f;

        [Category("瞄准与射击")]
        [Name("每米散布率")]
        [Description("默认EFT配置。我也不确定具体作用。")]
        [MinMax(0.001f, 1f, 1000f)]
        [Advanced]
        [CopyValue]
        public float ScatteringPerMeter = 0.08f;

        [Category("瞄准与射击")]
        [Name("近距每米散布率")]
        [Description("默认EFT配置。我也不确定具体作用。")]
        [MinMax(0.001f, 1f, 1000f)]
        [Advanced]
        [CopyValue]
        public float ScatteringClosePerMeter = 0.12f;

        [Category("听觉")]
        [Name("听觉距离乘数")]
        [Description("修改Bot听到声音的距离。")]
        [MinMax(0.1f, 3f, 1000f)]
        public float HearingDistanceMulti = 1f;

        [Name("可使用手雷")]
        public bool CanGrenade = true;

        [Hidden]
        [JsonIgnore]
        public bool CanRun = true;

        [Hidden]
        [JsonIgnore]
        public float DamageCoeff = 1f;

        public override void Apply(BotSettingsComponents settings)
        {
            settings.Core.VisibleAngle = VisibleAngle;
            settings.Core.VisibleDistance = VisibleDistance;
            settings.Core.GainSightCoef = GainSightCoef;
            settings.Core.AccuratySpeed = AccuratySpeed;
            settings.Core.ScatteringPerMeter = ScatteringPerMeter;
            settings.Core.ScatteringClosePerMeter = ScatteringClosePerMeter;
            settings.Core.HearingSense = HearingDistanceMulti;
            settings.Core.CanGrenade = CanGrenade;
            settings.Core.CanRun = CanRun;
            settings.Core.DamageCoeff = DamageCoeff;
        }


    }
}