using SAIN.Attributes;
using System.Collections.Generic;

namespace SAIN.Preset.GlobalSettings
{
    public class LookSettings : SAINSettingsBase<LookSettings>, ISAINSettings
    {
        [Category("核心设置")]
        [Name("视觉速度设置")]
        public VisionSpeedSettings VisionSpeed = new();

        [Category("核心设置")]
        [Name("视觉距离设置")]
        public VisionDistanceSettings VisionDistance = new();

        [Category("核心设置")]
        [Name("时间设置")]
        public TimeSettings Time = new();

        [Category("核心设置")]
        [Name("手电筒与夜视仪设置")]
        public LightNVGSettings Light = new();

        [Category("额外设置")]
        [Name("未注视Bot设置")]
        public NotLookingSettings NotLooking = new();

        [Category("额外设置")]
        [Name("防草丛透视")]
        public NoBushESPSettings NoBushESP = new();

        public override void Init(List<ISAINSettings> list)
        {
            VisionSpeed.Init(list);
            list.Add(VisionDistance);
            list.Add(NotLooking);
            list.Add(NoBushESP);
            list.Add(Time);
            list.Add(Light);
        }
    }
}