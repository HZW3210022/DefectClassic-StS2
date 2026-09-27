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

/// <summary>一代故障机器人：暴雪：全场伤害，随本场霜冻球数递增。</summary>
public sealed class Blizzard : DefectClassicCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(0m, ValueProp.Move), new DynamicVar("MagicNumber", 2m)];
    public Blizzard() : base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AllEnemies) { }
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play) {
        int frost = CombatManager.Instance.History.Entries.OfType<OrbChanneledEntry>()
            .Count(e => e.Orb is FrostOrb && e.Orb.Owner == Owner);
        decimal dmg = frost * DynamicVars["MagicNumber"].BaseValue;
        await CreatureCmd.TriggerAnim(Owner.Creature, "Attack", Owner.Character.AttackAnimDelay);
        await DamageCmd.Attack(dmg).FromCard(this, play).TargetingAllOpponents(CombatState!)
            .WithAttackerAnim("Cast", 0.5f).Execute(ctx);
    }
    protected override void OnUpgrade() => DynamicVars["MagicNumber"].UpgradeValueBy(1m);

    public override List<(string, string)> Localization => LocManager.Instance.Language switch
    {
        "zhs" => new CardLoc("暴雪", "对所有敌人造成你在本场战斗中 生成 过的 冰霜 充能球总数 {MagicNumber:diff()} 倍的伤害。"),
        _ => new CardLoc("Blizzard", "Deal damage equal to {MagicNumber:diff()} times the number of Frost Channeled this combat to ALL enemies.")
    };
}
