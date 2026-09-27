using System.Collections.Generic;
using BaseLib.Abstracts;
using DefectClassic.DefectClassicCode.Patches;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models.Orbs;

namespace DefectClassic.DefectClassicCode.Powers;

/// <summary>
/// 一代「电动力学」：闪电球命中所有敌人。
/// 实际的目标改写由 <see cref="LightningOrbElectrodynamicsPatch"/> 完成，本功率只作为标记。
/// </summary>
public sealed class ElectrodynamicsPower : DefectClassicPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromOrb<LightningOrb>()];

    public override List<(string, string)> Localization => LocManager.Instance.Language switch
    {
        "zhs" => new PowerLoc("电动力学", "闪电球命中所有敌人。", "闪电球命中所有敌人。"),
        _ => new PowerLoc("Electrodynamics", "Lightning orbs hit ALL enemies.", "Lightning orbs hit ALL enemies.")
    };
}
