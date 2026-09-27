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

/// <summary>一代故障机器人：撕裂。</summary>
public sealed class RipAndTear : DefectClassicCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(7m, ValueProp.Move), new DynamicVar("MagicNumber", 2m)];
    public RipAndTear() : base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AllEnemies) { }
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play) {
        for (int i = 0; i < DynamicVars["MagicNumber"].IntValue; i++)
        {
            var foes = CombatState!.HittableEnemies.ToList();
            if (foes.Count == 0) break;
            Creature t = foes[Owner.RunState!.Rng.CombatTargets.NextInt(foes.Count)];
            await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this, play).Targeting(t)
                .WithHitFx("vfx/vfx_attack_slash").Execute(ctx);
        }
    }
    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(2m);

    public override List<(string, string)> Localization => LocManager.Instance.Language switch
    {
        "zhs" => new CardLoc("狂乱撕扯", "随机对敌人造成 {Damage:diff()} 点伤害 {MagicNumber:diff()} 次。"),
        _ => new CardLoc("Rip and Tear", "Deal {Damage:diff()} damage to a random enemy {MagicNumber:diff()} times.")
    };
}
