using Newtonsoft.Json;
using SAIN.Attributes;
using System.Collections.Generic;

namespace SAIN.Preset.GlobalSettings
{
    public class GeneralSettings : SAINSettingsBase<GeneralSettings>, ISAINSettings
    {
        [Name("Bot可使用手雷")]
        public bool BotsUseGrenades = true;
        
        [Name("Bot可对Bot使用手雷")]
        [Description("Bot扔手雷不如玩家谨慎，开启可防止Bot互殴时被友军手雷误杀。")]
        public bool BotVsBotGrenade = true;

        [Name("Bot物理惯性")]
        [Description("Bot按装备和战利品重量正确受惯性影响。需重开战局(该设置在Bot生成时应用)。")]
        public bool BOT_INTERTIA_TOGGLE = true;

        [Name("原版Bot行为设置")]
        [Description("如果设为开启，对应类型Bot使用原版逻辑，所有SAIN功能(个性/后坐力/难度/行为)将被禁用。")]
        public VanillaBotSettings VanillaBots = new();

        [Name("性能设置")]
        public PerformanceSettings Performance = new();

        [Name("AI限制")]
        public AILimitSettings AILimit = new();

        [Name("掩体设置")]
        public CoverSettings Cover = new();

        [Name("门设置")]
        public DoorSettings Doors = new();

        [Name("撤离设置")]
        public ExtractSettings Extract = new();

        [Name("手电筒设置")]
        public FlashlightSettings Flashlight = new();

        [Name("拾荒Bot集成")]
        [Description("修改与Looting Bots模组相关的设置。需安装Looting Bots模组。")]
        public LootingBotsSettings LootingBots = new();

        [Name("玩笑设置")]
        public JokeSettings Jokes = new();

        [Name("调试设置")]
        public DebugSettings Debug = new();

        [Hidden]
        public LayerSettings Layers = new();

        public override void Init(List<ISAINSettings> list)
        {
            list.Add(this);
            list.Add(VanillaBots);
            list.Add(Performance);
            list.Add(AILimit);
            list.Add(Cover);
            list.Add(Doors);
            list.Add(Extract);
            list.Add(Flashlight);
            list.Add(LootingBots);
            list.Add(Jokes);
            list.Add(Layers);
            Debug.Init(list);
        }
    }

}