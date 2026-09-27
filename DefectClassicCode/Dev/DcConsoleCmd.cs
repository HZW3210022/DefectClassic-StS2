using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.DevConsole;
using MegaCrit.Sts2.Core.DevConsole.ConsoleCommands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;

namespace DefectClassic.DefectClassicCode.Dev;

/// <summary>
/// 开发用控制台命令（按 ` 打开控制台后输入 <c>dc</c>）。
///
/// 加这个是因为：靠正常推图抽牌，很难把 75 张卡都试一遍。
/// 有了它就能一次把整套卡池塞进抽牌堆，几回合内全部过一遍。
///
///     dc list                     列出本 mod 全部卡 ID（可直接喂给 card 命令）
///     dc pool [牌堆]               把全部卡各 1 张塞进指定牌堆（默认 Draw）
///     dc hand &lt;卡ID&gt; [数量]        往手牌塞指定卡（默认 5 张，受 10 张上限约束）
///
/// 卡 ID 可以省略前缀，写 `zap` 等同于 `DEFECTCLASSIC-ZAP`。
/// </summary>
public sealed class DcConsoleCmd : AbstractConsoleCmd
{
    private const string Prefix = "DEFECTCLASSIC-";

    public override string CmdName => "dc";

    public override string Args => "list | pool [pile] | hand <card-id> [count]";

    public override string Description =>
        "Defect Classic 测试工具：列出卡 ID / 把整套卡池塞进牌堆 / 手牌塞指定卡。";

    // 单机自用，不需要走多人同步
    public override bool IsNetworked => false;

    // mod 模式下本来就会放开调试命令，这里显式声明即可
    public override bool DebugOnly => false;

    private static List<CardModel> MyCards =>
        ModelDb.AllCards
            .Where(c => c.Id.Entry.StartsWith(Prefix, StringComparison.Ordinal))
            .OrderBy(c => c.Id.Entry, StringComparer.Ordinal)
            .ToList();

    private static ICardScope ScopeFor(bool combat)
    {
        return combat
            ? CombatManager.Instance.DebugOnlyGetState()
            : RunManager.Instance.DebugOnlyGetState();
    }

    private static string ResolveId(string raw)
    {
        string id = raw.ToUpperInvariant();
        return id.StartsWith(Prefix, StringComparison.Ordinal) ? id : Prefix + id;
    }

    public override CmdResult Process(Player? issuingPlayer, string[] args)
    {
        if (issuingPlayer is null)
        {
            return new CmdResult(false, "需要在一个进行中的存档里使用。");
        }
        if (!RunManager.Instance.IsInProgress)
        {
            return new CmdResult(false, "当前没有进行中的游戏。");
        }

        string sub = args.Length > 0 ? args[0].ToLowerInvariant() : "list";

        switch (sub)
        {
            case "list":
            {
                List<CardModel> cards = MyCards;
                return new CmdResult(true,
                    $"Defect Classic 共 {cards.Count} 张卡：\n" +
                    string.Join("\n", cards.Select(c => c.Id.Entry)));
            }

            case "pool":
            {
                PileType pile = PileType.Draw;
                if (args.Length >= 2 && !AbstractConsoleCmd.TryParseEnum<PileType>(args[1], out pile))
                {
                    return new CmdResult(false,
                        $"未知牌堆 '{args[1]}'。可选：{string.Join(", ", Enum.GetNames<PileType>())}");
                }
                // 非战斗牌堆（抽牌堆/弃牌堆/牌组）用 RunState 的 scope，战斗牌堆用手牌/战斗牌堆的
                bool combatScope = pile != PileType.Draw && pile != PileType.Discard
                                   && pile != PileType.Deck && pile != PileType.Exhaust;
                List<CardModel> cards = MyCards;
                Task task = AddAllAsync(issuingPlayer, pile, cards, combatScope);
                return new CmdResult(task, true, $"正在把 {cards.Count} 张卡放入 {pile} …");
            }

            case "hand":
            {
                if (args.Length < 2)
                {
                    return new CmdResult(false, "用法：dc hand <card-id> [count]，例如 dc hand claw 3");
                }
                string id = ResolveId(args[1]);
                CardModel? proto = ModelDb.AllCards.FirstOrDefault(c => c.Id.Entry == id);
                if (proto is null)
                {
                    return new CmdResult(false, $"找不到卡 '{id}'。用 'dc list' 看全部 ID。");
                }
                int count = 5;
                if (args.Length >= 3)
                {
                    int.TryParse(args[2], out count);
                }
                CardPile hand = PileType.Hand.GetPile(issuingPlayer);
                int free = CardPile.MaxCardsInHand - hand.Cards.Count;
                count = Math.Clamp(count, 0, Math.Max(0, free));
                if (count == 0)
                {
                    return new CmdResult(false,
                        $"手牌已满（{hand.Cards.Count}/{CardPile.MaxCardsInHand}）。先打掉几张再试。");
                }
                Task task = AddManyAsync(issuingPlayer, PileType.Hand, proto, count,
                    CombatManager.Instance.IsInProgress);
                return new CmdResult(task, true, $"正在往手牌加 {count} 张 {id} …");
            }

            default:
                return new CmdResult(false,
                    "用法：dc list | dc pool [pile] | dc hand <card-id> [count]");
        }
    }

    private static async Task AddAllAsync(Player player, PileType pile, List<CardModel> protos, bool combatScope)
    {
        ICardScope scope = ScopeFor(combatScope);
        foreach (CardModel proto in protos)
        {
            await CardPileCmd.Add(scope.CreateCard(proto, player), pile);
        }
    }

    private static async Task AddManyAsync(Player player, PileType pile, CardModel proto, int count, bool combatScope)
    {
        ICardScope scope = ScopeFor(combatScope);
        for (int i = 0; i < count; i++)
        {
            await CardPileCmd.Add(scope.CreateCard(proto, player), pile);
        }
    }

    public override CompletionResult GetArgumentCompletions(Player? player, string[] args)
    {
        if (args.Length <= 1)
        {
            return CompleteArgument(new[] { "list", "pool", "hand" }, Array.Empty<string>(),
                args.FirstOrDefault() ?? "");
        }
        if (args.Length == 2 && string.Equals(args[0], "hand", StringComparison.OrdinalIgnoreCase))
        {
            return CompleteArgument(MyCards.Select(c => c.Id.Entry).ToList(), new[] { args[0] }, args[1]);
        }
        if (args.Length == 2 && string.Equals(args[0], "pool", StringComparison.OrdinalIgnoreCase))
        {
            return CompleteArgument(Enum.GetNames<PileType>().ToList(), new[] { args[0] }, args[1]);
        }
        return new CompletionResult
        {
            Type = CompletionType.Argument,
            ArgumentContext = CmdName
        };
    }
}
