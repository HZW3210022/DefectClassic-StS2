using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Orbs;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace DefectClassic.DefectClassicCode.Powers;

/// <summary>一代「静电释放」：每次受到攻击伤害时，生成 N 个闪电球。</summary>
public sealed class StaticDischargePower : DefectClassicPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.Static(StaticHoverTip.Channeling),
        HoverTipFactory.FromOrb<LightningOrb>(),
    ];

    public override async Task AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target,
        DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (target != Owner) return;
        if (!props.HasFlag(ValueProp.Move)) return;
        // 一代要求「未被格挡的攻击伤害」，即真的掉血（result.UnblockedDamage > 0）
        if (result.UnblockedDamage <= 0) return;
        Flash();
        for (int i = 0; i < Amount; i++)
        {
            await OrbCmd.Channel<LightningOrb>(choiceContext, Owner.Player!);
        }
    }

    public override List<(string, string)> Localization => LocManager.Instance.Language switch
    {
        "zhs" => new PowerLoc("静电释放", "每当你受到攻击伤害时，生成 {Amount} 个闪电球。", "每当你受到攻击伤害时，生成 {Amount} 个闪电球。"),
        _ => new PowerLoc("Static Discharge", "Whenever you take attack damage, Channel {Amount} Lightning.", "Whenever you take attack damage, Channel {Amount} Lightning.")
    };
}
