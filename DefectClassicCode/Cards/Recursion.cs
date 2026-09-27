using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
    /// <summary>
    /// 暗球的「层数」存在 DarkOrb 的私有字段 _evokeVal 里（初始 6，每次被动 +PassiveVal）。
    /// OrbCmd.Channel&lt;T&gt; 是用模板造全新实例，层数会被重置成 6，
    /// 所以这里手工把它搬过去 —— 对应一代 RedoAction 的语义。
    /// </summary>
    private static readonly FieldInfo? DarkStoredValue =
        typeof(DarkOrb).GetField("_evokeVal", BindingFlags.NonPublic | BindingFlags.Instance);

    public Recursion() : base(1, CardType.Skill, CardRarity.Common, TargetType.Self) { }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        var queue = Owner.PlayerCombatState!.OrbQueue;
        if (queue.Orbs.Count == 0) return;

        OrbModel original = queue.Orbs.First();

        // 一代 RedoAction 生成的是「激发之前那个球」的副本，因此暗球的层数会带过去。
        // 激发本身不会改动 _evokeVal（只有被动才累加），所以这里先读出来。
        decimal? storedDark = original is DarkOrb ? original.EvokeVal : null;

        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);
        await OrbCmd.EvokeNext(ctx, Owner);

        // 用实例版 Channel，才能对新球做手脚（泛型版拿不到实例）
        OrbModel? fresh = original switch
        {
            LightningOrb => ModelDb.Orb<LightningOrb>().ToMutable(),
            FrostOrb     => ModelDb.Orb<FrostOrb>().ToMutable(),
            DarkOrb      => ModelDb.Orb<DarkOrb>().ToMutable(),
            PlasmaOrb    => ModelDb.Orb<PlasmaOrb>().ToMutable(),
            GlassOrb     => ModelDb.Orb<GlassOrb>().ToMutable(),
            _            => null,
        };
        if (fresh is null) return;

        if (storedDark is decimal stored && DarkStoredValue is not null)
        {
            DarkStoredValue.SetValue(fresh, stored);
        }

        await OrbCmd.Channel(ctx, fresh, Owner);
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);

    public override List<(string, string)> Localization => LocManager.Instance.Language switch
    {
        "zhs" => new CardLoc("递归", "激发 你最右侧的充能球。\n然后 生成 一个刚才被 激发 的充能球。"),
        _ => new CardLoc("Recursion", "Evoke your next Orb.\nChannel the Orb that was just Evoked.")
    };
}
