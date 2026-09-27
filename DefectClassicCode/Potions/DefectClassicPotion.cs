using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using DefectClassic.DefectClassicCode.Character;
using DefectClassic.DefectClassicCode.Extensions;

namespace DefectClassic.DefectClassicCode.Potions;

[Pool(typeof(DefectClassicPotionPool))]
public abstract class DefectClassicPotion : CustomPotionModel
{
	public override string? CustomPackedImagePath =>
		$"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".PotionImagePath();
	public override string? CustomPackedOutlinePath =>
		$"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".PotionOutlineImagePath();
}