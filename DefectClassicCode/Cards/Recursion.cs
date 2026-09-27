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

/// <summary>一代故障机器人：递归。</summary>
public sealed class Recursion : DefectClassicCard
{
    public Recursion() : base(1, CardType.Skill, CardRarity.Common, TargetType.Self) { }
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play) {
        var queue = Owner.PlayerCombatState!.OrbQueue;
        if (queue.Orbs.Count == 0) return;
        // 一代 RedoAction：记住被激发球的类型，再生成一个同类新球
        OrbModel orb = queue.Orbs.First();
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);
        await OrbCmd.EvokeNext(ctx, Owner);
        switch (orb)
        {
            case LightningOrb: await OrbCmd.Channel<LightningOrb>(ctx, Owner); break;
            case FrostOrb: await OrbCmd.Channel<FrostOrb>(ctx, Owner); break;
            case DarkOrb: await OrbCmd.Channel<DarkOrb>(ctx, Owner); break;
            case PlasmaOrb: await OrbCmd.Channel<PlasmaOrb>(ctx, Owner); break;
            case GlassOrb: await OrbCmd.Channel<GlassOrb>(ctx, Owner); break;
        }
    }
    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);

    public override List<(string, string)> Localization => LocManager.Instance.Language switch
    {
        "zhs" => new CardLoc("递归", "激发 你最右侧的充能球。\n然后 生成 一个刚才被 激发 的充能球。"),
        _ => new CardLoc("Recursion", "Evoke your next Orb.\nChannel the Orb that was just Evoked.")
    };
}
