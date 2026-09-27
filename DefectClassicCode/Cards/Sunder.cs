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

/// <summary>一代故障机器人：断裂。</summary>
public sealed class Sunder : DefectClassicCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(24m, ValueProp.Move), new DynamicVar("MagicNumber", 3m)];
    public Sunder() : base(3, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy) { }
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play) {
        ArgumentNullException.ThrowIfNull(play.Target, "play.Target");
        // 一代 SunderAction：若这一击杀死目标，则获得 3 点能量
        if ((await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this, play).Targeting(play.Target)
                .WithHitFx("vfx/vfx_attack_blunt", null, "blunt_attack.mp3")
                .Execute(ctx)).Results.SelectMany(r => r).Any(r => r.WasTargetKilled))
        {
            await PlayerCmd.GainEnergy(DynamicVars["MagicNumber"].IntValue, Owner);
        }
    }
    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(8m);

    public override List<(string, string)> Localization => LocManager.Instance.Language switch
    {
        "zhs" => new CardLoc("分离", "造成 {Damage:diff()} 点伤害。\n如果这张牌杀死了敌人，则获得 {energyPrefix:energyIcons(3)} 。"),
        _ => new CardLoc("Sunder", "Deal {Damage:diff()} damage.\nIf this kills an enemy, gain {energyPrefix:energyIcons(3)}.")
    };
}
