using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Orbs;
using DefectClassic.DefectClassicCode.Powers;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace DefectClassic.DefectClassicCode.Cards;

/// <summary>一代故障机器人：多重施法：激发充能球 X 次。</summary>
public sealed class MultiCast : DefectClassicCard
{
    protected override bool HasEnergyCostX => true;
    public override OrbEvokeType OrbEvokeType => OrbEvokeType.All;
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.Static(StaticHoverTip.Evoke)];
    public MultiCast() : base(0, CardType.Skill, CardRarity.Rare, TargetType.Self) { }
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play) {
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);
        int n = ResolveEnergyXValue();
        if (IsUpgraded) n++;
        for (int i = 0; i < n; i++)
        {
            await OrbCmd.EvokeNext(ctx, Owner, i == n - 1);
            await Cmd.Wait(0.25f);
        }
    }
    /// <summary>一代升级：激发次数 X+1（不改变费用）。</summary>
    protected override void OnUpgrade() { }

    public override List<(string, string)> Localization => LocManager.Instance.Language switch
    {
        "zhs" => new CardLoc("多重释放", "激发 你最右侧的充能球 X{IfUpgraded:show:+1} 次。"),
        _ => new CardLoc("Multi-Cast", "Evoke your next Orb X{IfUpgraded:show:+1} times.")
    };
}
