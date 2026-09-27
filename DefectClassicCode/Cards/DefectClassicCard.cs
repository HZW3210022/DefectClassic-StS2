using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using DefectClassic.DefectClassicCode.Character;
using DefectClassic.DefectClassicCode.Extensions;
using MegaCrit.Sts2.Core.Entities.Cards;

namespace DefectClassic.DefectClassicCode.Cards;

/// <summary>
/// 本 mod 所有卡牌的基类。图标与卡图都从本 mod 的资源目录里按类名加载。
/// [Pool] 特性可被继承，所以子类会自动进入 DefectClassicCardPool。
/// </summary>
[Pool(typeof(DefectClassicCardPool))]
public abstract class DefectClassicCard(int cost, CardType type, CardRarity rarity, TargetType target)
    : CustomCardModel(cost, type, rarity, target)
{
    // 卡图规格：大图 1000x760（一代原图 500x380 亦可，会自动放大）
    public override string CustomPortraitPath => $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".BigCardImagePath();

    // 小图规格：250x190
    public override string PortraitPath => $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".CardImagePath();
}
