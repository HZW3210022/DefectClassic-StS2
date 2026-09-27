using BaseLib.Abstracts;
using DefectClassic.DefectClassicCode.Extensions;
using Godot;

namespace DefectClassic.DefectClassicCode.Character;

public class DefectClassicRelicPool : CustomRelicPoolModel
{
    public override Color LabOutlineColor => new("3EB3ED");

    public override string BigEnergyIconPath => "charui/big_energy.png".ImagePath();

    public override string TextEnergyIconPath => "charui/text_energy.png".ImagePath();
}
