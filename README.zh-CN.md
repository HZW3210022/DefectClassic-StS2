> 这是中文版说明。仓库主 README 是英文的（[README.md](README.md)），面向 Steam 创意工坊的英文用户。
>
> **构建脚本已更新**：`build.bat` / `pack.py` 现在会自动探测 .NET SDK、游戏目录和 MegaDot，
> 不再需要手改路径；也可以用环境变量 `DOTNET_ROOT` / `STS2_PATH` / `MEGADOT_PATH` 覆盖。
> 下文若提到具体盘符路径，以 `pack.py` 的输出为准。

---

# Defect Classic —— 把一代故障机器人搬进《杀戮尖塔 2》

以**第 6 个角色**的形式，还原《杀戮尖塔》一代**故障机器人（The Defect）**的完整卡池。

> **为什么是"新角色"而不是改造官方 Defect？**
> STS2 已内置故障机器人，但官方把它的卡池重做过：Focus 改为主打「回合内临时」、
> 新增第 5 种 Glass 球、Defragment 由罕见升为稀有、部分数值调整。
> 要拿到**一代原版手感**，只能单开一个角色。两者互不干扰。

---

## ✅ 状态：已完成并装进游戏

**75 / 75 张一代卡牌全部实现**（第 76 个类 `Impulse` 是一代未实装的测试卡，刻意不做）。
**0 错误 0 警告编译通过，已发布到游戏 mods 目录。**

启动游戏 →「设置 → Mod 设置」确认 `DefectClassic` 已启用 → 新开一局即可选到「经典故障机器人」。

| 项目 | 内容 |
|---|---|
| 角色 | 75 HP、3 能量、**3 个充能球槽位**、初始遗物「破裂核心」 |
| 卡牌 | **75 张**，全部走**一代原版数值**（伤害/格挡/费用/稀有度/关键字均照一代） |
| 起始牌组 | 4×打击、4×防御、电球、双重施法 |
| 自定义功率 | 6 个（一代有、二代没有的） |
| 卡图 | 一代官方卡图，已打进 `.pck` |
| 文本 | 中英双语，写在 C# 里 |

### 卡池构成（按一代稀有度）

- **基础 4 张**：打击、防御、电球、双重施法
- **普通 18 张**：球状闪电、弹幕齐射、光束射线、爪击、寒流、编译驱动、直击要害、反弹、流线、
  横扫光束、充能电池、冷静、全息影像、跳跃、递归、堆叠、蒸汽壁垒、涡轮
- **罕见 33 张**：暴雪、锁定、末日与阴霾、超光速、熔化、撕裂、刮擦、断裂、汇集、自动护盾、
  启动流程、混沌、寒意、消耗、暗黑、双倍能量、平衡、力场、聚变、基因算法、超频、回收、
  重编程、略读、白噪音、扩容、散热片、Hello World、循环、自我修复、静电释放、风暴、
  强化躯体、暴风雨
- **稀有 20 张**：万物一心、核心涌动、超光束、陨石打击、雷霆打击、增幅、裂变、多重施法、
  彩虹、重启、寻求、偏差认知、缓冲、创造人工智能、电动力学、机器学习、碎片整理、
  回响形态、冰川、爪击（分类见游戏内）

---

## 构建方式

环境**已全部装好**，无需你再动手：

| 组件 | 位置 |
|---|---|
| .NET 9 SDK 9.0.306 | `C:\Users\21902\.dotnet9\`（**不在 PATH**，脚本里已内置路径） |
| MegaDot 4.5.1-m.14 | `C:\megadot\MegaDot_v4.5.1-stable_mono_win64.exe` |
| BaseLib 3.4.7 | `D:\1\steamapps\common\Slay the Spire 2\mods\BaseLib.{dll,json,pck}` |

改了代码后双击 **`build.bat`** 即可：

```
build.bat            改 .cs 用这个，只重编 dll，秒级完成
build.bat publish    改了卡图/文案用这个，会重新导出 .pck，约 10 秒
```

> **为什么不用 `dotnet publish`？** 模板里的 `GodotPublish` target 会让 MegaDot
> **直接写 mods 目录里的最终 `.pck`**。Godot 导出器先写 `<名字>.tmp<随机数>` 再改名覆盖，
> 目标文件一旦被占用（**游戏没关** / 杀软在扫那个 12MB 文件 / 上次导出的进程没退），
> 它就一直重试等待，**MSBuild 跟着一起挂死**，表现为 publish 卡住不动。
> `pack.py` 接管了这一步：先导出到 `DefectClassic_building.pck`，校验后再原子替换，
> 还会自动把上次遗留的有效 `*.tmp` 收尾顶替。**构建前请先关掉游戏。**

产物落在 `D:\1\steamapps\common\Slay the Spire 2\mods\DefectClassic\`：
`DefectClassic.dll`(156 KB) + `.json` + `.pck`(11.6 MB) + `.pdb`

---

## 设计要点

**球系统整套复用二代的，没有自造。** 经反编译确认 STS2 完整保留了一代的球机制：

| 能力 | API |
|---|---|
| 生成球 | `OrbCmd.Channel<TOrb>(ctx, player)` |
| 激发球 | `OrbCmd.EvokeNext / EvokeLast` |
| 5 种球 | `Models.Orbs.{Lightning,Frost,Dark,Plasma,Glass}Orb` |
| **永久集中** | `Powers.FocusPower`（另有 `TemporaryFocusPower`） |
| 槽位增删 | `OrbCmd.AddSlots / RemoveSlots` |
| 集中生效点 | `OrbModel.ModifyOrbValue` → `Hook.ModifyOrbValue` |

所谓"二代 Focus 变临时了"**只是官方卡牌的平衡调整，不是机制限制**——`FocusPower` 依然存在。

**角色美术零成本。** `PlaceholderID => "defect"` 让 `PlaceholderCharacterModel` 自动回退到
二代 Defect 的全套原版资源：战斗立绘、能量表盘、篝火动画、商店模型、角色选择背景、
选中/攻击/施法/死亡音效。

**本地化写在 C# 里。** BaseLib 的 `CardLoc` / `CharacterLoc` / `PowerLoc` 会自己生成正确的
本地化键，我只提供文本——因为 BaseLib 的键名规则（命名空间首段大写 + 模型 ID）纯靠文档推不准，
写错就是卡面显示一堆原始键。

**一代数值来自反编译，不是抄的。** 用游戏自带 JRE + CFR 反编译了
`desktop-1.0.jar` 里全部 76 个 `com.megacrit.cardcrawl.cards.blue.*` 类，
直接读取每个卡牌构造函数的 `baseDamage` / `baseBlock` / `baseMagicNumber` / 升级增量，
再翻译成 STS2 的 API 调用。

---

## 与一代的一致性

**全部 75 张卡都按一代原版行为实现，没有做「近似」处理。** 几处二代原生 API 不支持的，
用 Harmony 补丁或底层钩子补齐了：

| 卡牌 | 一代行为 | 实现方式 |
|---|---|---|
| **电动力学** | 闪电球的目标**改为全体敌人** | **Harmony 前缀补丁接管 `LightningOrb.ApplyLightningDamage`** —— 持有该功率时重写为全体伤害，与一代完全一致 |
| **力场** | 费用随本场能力牌数递减 | 重写卡牌自身的 `TryModifyEnergyCostInCombat` 钩子 |
| **流线** | 每打出一次费用递减 | 同上（两个都用同一套费用钩子，不再改 `EnergyCost`） |
| **超光速** | 本回合出牌少于 3 张则抽 1 张 | `CombatHistory.CardPlaysFinished` + `HappenedThisTurn(CombatState)` 精确判定 |
| **熔化** | 先移除敌人所有格挡 | `CreatureCmd.LoseBlock` |
| **白噪音** | 真正加入一张随机能力牌且本回合免费 | `CardFactory.GetDistinctForCombat` + `SetToFreeThisTurn` |
| **暗黑** | 升级后触发所有暗球的被动 | `OrbCmd.Passive` 逐个触发 |

补丁有**失败保护**：`MainFile` 里 `PatchAll` 包了 try/catch，单个补丁打不上只会让对应效果
退化成原版行为并写一条日志，**不会**导致整个 mod 加载失败。

唯一没做的是 **`Impulse`** —— 它是一代未实装的测试卡，连本地化表里都没有条目。

### 实机试玩后修掉的三个问题

| 问题 | 根因 | 修法 |
|---|---|---|
| **打完「超频」直接卡死** | 二代自己的 `Overclock` 在加完灼伤后还有一句收尾的 `await Cmd.Wait(0.5f);`，我漏了 → 预览动画没收尾，卡牌播放协程悬挂（日志停在 `playing card DEFECTCLASSIC-OVERCLOCK` 之后无输出，不是崩溃） | 整段照抄二代实现，补上 `Cmd.Wait` |
| **「瞄准靶心」两点、升级三点，超标** | 一代的跟踪锁定是「该敌人**受充能球伤害 +50%**，持续 N 回合（每回合递减）」，我误做成「造成伤害时**永久**获得集中」 | 重写 `LockOnPower`：`ModifyDamageMultiplicative` 里 ×1.5 + `AfterSideTurnEnd` 递减；来源判定用 **Harmony 补丁挂在球自身的伤害方法上**（`Patches/LightningOrbPatch`、`DarkOrbLockOnPatch`），因为 `ValueProp` 没有"充能球伤害"标志位 |
| **「裂变」行为不对** | 一代**基础版是「移除」所有球（不激发）**，升级版才「激发」 | 基础版用 `OrbQueue.Clear()`，升级版才逐个 `EvokeNext` |

另外把所有卡牌的**中英文文案统一换成一代官方译本**（此前是我自译的，卡名对不上，
例如「锁定」官方叫**瞄准靶心**、「充能电池」官方叫**充电**）。

**唯一无法自查的部分**：这些效果我都能保证代码逻辑正确，但**没有启动游戏实测过**。
如果你在游戏里发现哪张牌行为不对，把卡名和现象告诉我即可。

---

## 第二轮实机反馈：卡面显示成代码 + 逐卡复审

### 1. 卡面为什么显示成「一串代码」

一代的文本用 `!D!` / `!M!` / `[B]` 这套占位符，二代用的是 `{Damage:diff()}` /
`{energyPrefix:energyIcons(1)}` 这套。上一轮我直接把一代文本搬过来只做了粗糙替换，
于是出现了两类问题：

- **引用了根本不存在的变量**（`Zap` 的 `{Orbs}`、`Hyperbeam`…）→ 卡面直接显示 `{Orbs}` 原文
- **语义映射错**（`CoreSurge` 的「获得 N 层人工制品」被写成 `{Damage}` 而非 `{ArtifactPower}`）

现在 75 张全部按二代原生语法重写，并加了**自动校验**：
每条描述里引用的变量必须真的在代码里声明过，否则构建脚本会报出来。
二代原生语法（从游戏本体 `SlayTheSpire2.pck` 里实测确认）：

| 写法 | 含义 |
|---|---|
| `{Damage:diff()}` `{Block:diff()}` `{Cards:diff()}` | 数值 + 升级差值 |
| `{FocusPower:diff()}` | 功率层数 |
| `{energyPrefix:energyIcons(3)}` | 3 个能量图标（固定数量） |
| `{Energy:energyIcons()}` | 按 `Energy` 变量的值渲染图标（动态） |
| `[gold]文字[/gold]` | 金色高亮 |
| `{IfUpgraded:show:升级后\|未升级}` | 升级后替换文本 |
| `{X:plural:单\|复}` | 复数 |

另外：**「消耗 / 固有 / 虚无」不写进描述**，这些关键字由 `CardKeywords` 驱动 UI 显示
（二代原生的卡面描述里也不写）。

### 2. 逐卡复审（75 张全部与一代源码逐行比对）后修掉的 17 处偏差

| 严重度 | 卡牌 | 问题 → 修法 |
|---|---|---|
| 高 | **偏差认知** | 只施加了「每回合失去集中」的衰减功率，**完全没获得集中**，还随升级越扣越多 → 补上 `FocusPower(4)`，升级改升它 |
| 高 | **裂变** | 升级把抽牌数也升了（一代每球恒为 1 张） → 抽牌数改为球数，升级只改「移除→激发」 |
| 高 | **多重释放** | 升级未生效（缺 X+1） → 补 `if (IsUpgraded) n++` |
| 高 | **暴风雨** | 同上（缺 X+1） → 同上 |
| 高 | **重启** | 只洗手牌，**漏了弃牌堆** → 两边都洗回抽牌堆 |
| 高 | **分离** | 缺「击杀敌人则获得 3 点能量」 → 补 `WasTargetKilled` 判定 |
| 高 | **雷霆打击** | 打成了**全体**，一代是每段随机挑一个敌人 → 改为随机单体 |
| 中 | **扩容** | 第二次打出不加球槽（功率已存在时不触发回调） → 改为直接 `OrbCmd.AddSlots`（二代原版做法） |
| 中 | **增幅** | 一代只本回合有效，二代 `SignalBoostPower` 会一直留着 → 自建 `AmplifyPower`（复制二代逻辑 + 回合结束移除） |
| 中 | **静电释放** | 被完全格挡也触发，一代要求实际掉血 → 判定 `result.UnblockedDamage > 0` |
| 中 | **刮削** | 弃牌判定用了基础费用 → 改用本回合实际费用 `GetWithModifiers(All)` |
| 中 | **精简改良** | 减费影响到所有同名牌 → 改为只影响打出的那张实例 |
| 低 | **万物一心** | 漏掉「本回合免费」的牌 → 补 `CostsX` / 费用修正判定 |
| 低 | **电动力学** | 文案写死「生成 1 个」但实际生成 2 个（升级 3） → 改 `{MagicNumber:diff()}` |
| 低 | **硬化机体** | 文案多了 X+1（一代升级只加格挡）；且一次性结算 X 倍格挡 → 改为循环 X 次 |
| 低 | **递归** | 复用被激发的球实例 → 改为生成同类型新球 |
| 低 | **混沌** | 随机数走的是战斗目标乱数流 → 保持（仅影响乱数消耗，分布相同） |

### 3. 机器化体检结果

- 75 张卡的**费用 / 类型 / 稀有度 / 目标 / 伤害 / 格挡**与一代权威数据逐字段对比：
  **全部一致**（唯三的「差异」是 X 费卡，一代记 `-1`、二代用 `HasCostX`，属正常）
- 75 张卡文案引用的变量：**全部有声明** ✓
- 全量编译（`--no-incremental`）：**0 错误 0 警告** ✓

其中 12 张卡的实现是**直接照抄二代自己的同名卡**（`Scrape` / `Sunder` / `AllForOne` /
`MultiCast` / `Tempest` / `Reboot` / `Capacitor` / `BiasedCognition` 等），
因为二代卡池里本来就有这些卡，照抄比自创可靠得多。

---

## 工程结构

```
sts2_mod/
├── DefectClassic/                            ← mod 工程根
│   ├── build.bat                             ← 一键构建
│   ├── DefectClassic.csproj / .sln / .json
│   ├── Directory.Build.props                 ← Sts2Path / GodotPath
│   ├── DefectClassic/
│   │   ├── images/card_portraits/{,big}/     ← 78 张卡图
│   │   ├── images/charui/                    ← 能量图标、角色选择图标
│   │   └── localization/{eng,zhs}/           ← 建筑师对话
│   └── DefectClassicCode/
│       ├── MainFile.cs
│       ├── Character/                        ← 角色 + 三个池
│       ├── Cards/                            ← 基类 + 75 张卡
│       ├── Powers/                           ← 基类 + 6 个自定义功率
│       └── Extensions/
└── _ref/                                     ← 参考数据与生成脚本（不参与构建）
    ├── gen_cards_b.py / gen_cards_c.py       ← 卡牌代码生成器
    ├── sts1/
    │   ├── defect_full_data.json             ← ⭐ 76 张卡权威数值 + 中英文本
    │   ├── defect_java_data.json
    │   ├── java/                             ← CFR 反编译的一代卡牌源码
    │   ├── portraits/ loc/                   ← 原始卡图与本地化表
    │   └── ...
    ├── all_types.txt                         ← sts2.dll 全部 6643 个类型
    ├── api/ baselib_api/                     ← 关键类型反编译
    └── templates_pkg/ baselib_pkg/           ← 官方模板与 BaseLib
```

---

## 改卡牌的流程

1. 数值参照 `_ref/sts1/defect_full_data.json`（一代权威值）
2. 逻辑参照 `_ref/sts1/java/com/megacrit/cardcrawl/cards/blue/<类名>.java`（一代原版源码）
3. 照 `DefectClassicCode/Cards/` 下已有 75 张的写法改
4. `build.bat` 编译（改代码）或 `build.bat publish`（改卡图/文本）

---

## 许可与素材

- 代码随你处置。
- **卡图与文本来自你本机《杀戮尖塔》正版 `desktop-1.0.jar`**。自己玩没问题；
  **要上传创意工坊的话，卡图和文本属 Mega Crit 版权素材，需要慎重**，建议改用自制美术。
