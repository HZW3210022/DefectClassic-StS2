using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using DefectClassic.DefectClassicCode.Character;
using DefectClassic.DefectClassicCode.Extensions;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Localization;

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

    /// <summary>
    /// 选牌界面（例如「从弃牌堆取回一张牌」）的标题文本。
    ///
    /// ⚠️ 这层兜底是必须的：原生 <c>CardModel.SelectionScreenPrompt</c> 在本地化键缺失时
    /// <b>会直接抛 InvalidOperationException</b> —— 它先构造 LocString，再调 <c>Exists()</c>，
    /// 不存在就 throw。BaseLib 的 <c>MissingLocPatch</c> 救不了它，因为那个补丁只在
    /// 「查表取文本失败」时兜底，而异常来自 Exists() 检查本身。
    ///
    /// 后果是**静默的半途失败**：OnPlay 里前面那步（如获得格挡）已经结算，
    /// 抛异常后整张牌中断，玩家看到「加了格挡但没拿到牌」，日志里是一行
    /// <c>No selection screen prompt for CARD.XXX</c>。
    ///
    /// 这里不检查 Exists()，键缺失时只是标题为空，功能不受影响。
    /// 正式文本写在 <c>localization/{lang}/cards.json</c> 里的
    /// <c>{卡完整ID}.selectionScreenPrompt</c>（游戏会自动合并 mod 的本地化表）。
    /// </summary>
    protected new LocString SelectionScreenPrompt => new LocString("cards", Id.Entry + ".selectionScreenPrompt");
}
