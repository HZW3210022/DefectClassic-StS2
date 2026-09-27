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

/// <summary>一代故障机器人：雷霆打击：按本场闪电球数量全场伤害。</summary>
public sealed class ThunderStrike : DefectClassicCard
{
    protected override HashSet<CardTag> CanonicalTags => [CardTag.Strike];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(7m, ValueProp.Move)];
    public ThunderStrike() : base(3, CardType.Attack, CardRarity.Rare, TargetType.AllEnemies) { }
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play) {
        int n = CombatManager.Instance.History.Entries.OfType<OrbChanneledEntry>()
            .Count(e => e.Orb is LightningOrb && e.Orb.Owner == Owner);
        // 一代 NewThunderStrikeAction：每次随机挑一个存活敌人
        for (int i = 0; i < n; i++)
        {
            var foes = CombatState!.HittableEnemies.ToList();
            if (foes.Count == 0) break;
            Creature t = foes[Owner.RunState!.Rng.CombatTargets.NextInt(foes.Count)];
            await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this, play).Targeting(t)
                .WithHitFx("vfx/vfx_attack_blunt", null, "blunt_attack.mp3").Execute(ctx);
        }
    }
    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(2m);

    public override List<(string, string)> Localization => LocManager.Instance.Language switch
    {
        "zhs" => new CardLoc("雷霆打击", "你在本场战斗中每 生成 过 一个 闪电 充能球，随机造成一次 {Damage:diff()} 点伤害。"),
        _ => new CardLoc("Thunder Strike", "Deal {Damage:diff()} damage to a random enemy for each Lightning Channeled this combat.")
    };
}
