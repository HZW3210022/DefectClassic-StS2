using BaseLib.Abstracts;
using DefectClassic.DefectClassicCode.Extensions;
using Godot;

namespace DefectClassic.DefectClassicCode.Character;

/// <summary>
/// 本 mod 专属卡池。因为卡牌基类带了 [Pool(...)]，卡牌会自动归入这里，无需手动枚举。
/// </summary>
public class DefectClassicCardPool : CustomCardPoolModel
{
    public override string Title => "defect_classic";

    public override string BigEnergyIconPath => "charui/big_energy.png".ImagePath();

    public override string TextEnergyIconPath => "charui/text_energy.png".ImagePath();

    /// <summary>卡框与卡背的主色调（二代 Defect 蓝）。</summary>
    public override Color ShaderColor => new("3EB3ED");

    public override Color DeckEntryCardColor => new("3EB3ED");

    public override bool IsColorless => false;
}
