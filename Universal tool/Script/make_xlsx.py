# -*- coding: utf-8 -*-
"""生成 UniversalTool.xlsx（Thing 表）。
必须用 xlsxwriter：openpyxl 的 inlineStr 字符串会被游戏 NPOI 读空。
三行头：字段名 / 类型 / 默认值，数据从第 4 行起。
"""
import os
import xlsxwriter

HEADER = [
    "id", "name_JP", "unknown_JP", "unit_JP", "naming", "name", "unit", "unknown",
    "category", "sort", "_tileType", "_idRenderData", "tiles", "altTiles",
    "anime", "skins", "size", "colorMod", "colorType", "recipeKey",
    "factory", "components", "disassemble", "defMat", "tierGroup",
    "value", "LV", "chance", "quality", "HP", "weight", "electricity",
    "trait", "elements", "range", "attackType", "offense", "substats",
    "defense", "lightData", "idExtra", "idToggleExtra", "idActorEx",
    "idSound", "tag", "workTag", "filter", "roomName_JP", "roomName",
    "detail_JP", "detail",
]

TYPES = [
    "string", "string", "string", "string", "string", "string", "string", "string",
    "string", "int", "string", "string", "int[]", "int[]", "int[]", "int[]", "int[]",
    "int", "string", "string[]", "string[]", "string[]", "string[]", "string", "string",
    "int", "int", "int", "int", "int", "int", "int", "string[]", "elements", "int",
    "string", "int[]", "int[]", "int[]", "string", "string", "string", "string", "string",
    "string[]", "string", "string[]", "string[]", "string[]", "string", "string",
]

# 默认值行：所有数据行公共字段回退到这里（参考旧项目 AppExpansion 已验证配置）
DEFAULTS = [None] * 51
DEFAULTS[8] = "processor"      # category
DEFAULTS[9] = 100              # sort
DEFAULTS[10] = "ObjBig"        # _tileType
DEFAULTS[11] = "@obj"          # _idRenderData
DEFAULTS[16] = "1,1"           # size
DEFAULTS[17] = 100             # colorMod
DEFAULTS[19] = "*"             # recipeKey: 初始习得
# factory 留空 = 不可制作（官方文档约定，不设 workbench 默认值，否则未显式填 factory 的物品会意外出现在制作台）
DEFAULTS[20] = None
DEFAULTS[21] = "branch/1"      # components: 1 个树枝（本 mod 万能工作台的默认材料）
DEFAULTS[23] = "oak"           # defMat（wood 无效！）
DEFAULTS[25] = 100             # value
DEFAULTS[26] = 1               # LV
DEFAULTS[27] = 100             # chance
DEFAULTS[29] = 100             # HP
DEFAULTS[30] = 1000            # weight

# 数据行：只填差异列，其余回退默认值
ROWS = [
    {
        "id": "ut_universal_tool",
        "name_JP": "ユニバーサルワークベンチ",
        "name": "Universal Tool",
        "value": 200,
        "weight": 2000,
        "_tileType": "Obj",  # 小物件：可被行走跨过（ObjBig 会阻挡移动）
        "factory": "workbench",  # 在制作台制造
        "components": "log/10,branch/10,leaf/10,grass/10,vine/10",  # 原木×10+树枝×10+叶子×10+草×10+藤蔓×10
        "trait": "Workbench",  # 对标制作台：TraitWorkbench（游戏内置）
        "detail_JP": "作業台で作られる万能の作業台。",
        "detail": "A versatile workbench crafted at the workbench.",
    },
    {
        "id": "ut_universal_essence",
        "name_JP": "ユニバーサルエッセンス",
        "name": "Universal Essence",
        "category": "resource",  # 资源类：可堆叠
        "value": 500,
        "weight": 1,  # 0.001（很轻）
        # 无 factory（留空 = 不可制作），由凝华器 Recipe 表产出
        "detail_JP": "万能の力が凝縮されたエッセンス。",
        "detail": "A condensed essence of universal power.",
    },
    {
        "id": "ut_condenser",
        "name_JP": "コンデンサー",
        "name": "Condenser",
        "value": 200,
        "weight": 2000,
        "_tileType": "Obj",  # 小物件：可被行走跨过
        "factory": "ut_universal_tool",  # 在万能工作台制造
        "components": "log/3,plank/3,glass/10",  # 原木×3 + 木板×3 + 玻璃×10
        "trait": "EssenceExtractor",  # 自定义：像石磨的混合机，产出万能精华
        "detail_JP": "材料を万能のエッセンスに抽出する装置。",
        "detail": "A device that extracts universal essence from materials.",
    },
    {
        "id": "ut_portable_workbench",
        "name_JP": "ポータブルワークベンチ",
        "name": "Portable Workbench",
        "value": 200,
        "weight": 100,  # 0.1
        "_tileType": "Obj",  # 小物件：可被行走跨过
        "factory": "ut_universal_factory",  # 配方迁移到万能工厂
        "components": "workbench2/1,ut_universal_essence/20",  # 设计台×1 + 万能精华×20
        "trait": "PortableWorkbench",  # 自定义：随身虚拟工作台（5 个工作台 tab）
        "detail_JP": "どこでも5つの作業台を開ける携帯用の作業台。",
        "detail": "A portable workbench that opens five workbenches anywhere.",
    },
    {
        "id": "ut_tool_workbench",
        "name_JP": "ツールワークベンチ",
        "name": "Tool Workbench",
        "value": 200,
        "weight": 100,  # 0.1
        "_tileType": "Obj",  # 小物件：可被行走跨过
        "factory": "ut_universal_tool",  # 在万能工作台制造
        "components": "log/4,plank/2,ingot/2",  # 原木×4 + 木板×2 + 金属锭×2
        "trait": "Workbench",  # 游戏内置 TraitWorkbench：工具类物品的装配工作台
        "detail_JP": "道具類を作るための作業台。",
        "detail": "A workbench for assembling tools.",
    },
    {
        "id": "ut_axe_pickaxe",
        "name_JP": "マトック",
        "name": "Mattock",
        "category": "tool",  # 与原版斧/镐一致：工具分类（显示"工具"），非武器
        "value": 50,
        "weight": 100,  # 0.1
        "factory": "ut_tool_workbench",  # 配方在工具装配台
        "components": "ingot/5,stick/3,string/3,ut_universal_essence/1",
        "trait": "AxePickaxe",  # 自定义：OnCreate 注入采集元素 220/225/230；硬度随材料
        "detail_JP": "斧とツルハシを一体化した多目的ツール。材質によって性能が変わる。",
        "detail": "A multi-purpose tool combining axe and pickaxe. Performance depends on material.",
    },
    {
        "id": "ut_skill_trainer",
        "name_JP": "スキルトレーナー",
        "name": "Skill Trainer",
        "value": 200,
        "weight": 100,  # 0.1
        "_tileType": "Obj",  # 小物件：可被行走跨过
        "factory": "ut_universal_factory",  # 配方迁移到万能工厂
        "components": "book_skill/1,water/2,ut_universal_essence/10",
        "trait": "SkillTrainer",  # 自定义：消耗结晶提升已有技能等级
        "detail_JP": "結晶を消費して仲間やペットのスキルを強化できる訓練装置。",
        "detail": "A training device that consumes crystals to level up skills of allies and pets.",
    },
    {
        "id": "ut_amplifier",
        "name_JP": "アンプリファイア",
        "name": "Amplifier",
        "value": 200,
        "weight": 100,  # 0.1
        "_tileType": "Obj",  # 小物件：可被行走跨过
        "factory": "ut_universal_factory",  # 配方迁移到万能工厂
        "components": "gem/4,glass/5,ingot/2",  # 宝石×4 + 玻璃×5 + 金属锭×2
        "trait": "Amplifier",  # 自定义：消耗物品获取临时增益
        "detail_JP": "決まったアイテムを消費して一時的な強化を得る装置。",
        "detail": "A device that consumes specified items to grant temporary buffs.",
    },
    {
        "id": "ut_portable_shipping",
        "name_JP": "ポータブル出荷箱",
        "name": "Portable Shipping Chest",
        "category": "container",  # 容器分类
        "value": 200,
        "weight": 100,  # 0.1
        "_tileType": "Obj",  # 小物件：可被行走跨过
        "factory": "ut_universal_factory",  # 配方迁移到万能工厂
        "components": "container_shipping/1,ut_universal_essence/10",  # 出货箱×1 + 万能精华×10
        "trait": "PortableShipping",  # 自定义：与大出货箱联通
        "detail_JP": "大きな出荷箱と繋がっている携帯用の出荷箱。",
        "detail": "A portable shipping chest connected to the big shipping chest.",
    },
    {
        "id": "ut_duplicator",
        "name_JP": "デュプリケーター",
        "name": "Duplicator",
        "value": 200,
        "weight": 2000,
        "_tileType": "Obj",  # 小物件：可被行走跨过
        "factory": "ut_universal_tool",  # 在万能工作台制造
        "components": "log/4,plank/2,glass/10",  # 原木×4 + 木板×2 + 玻璃×10
        "trait": "Duplicator",  # 自定义：消耗万能精华复制物品
        "detail_JP": "万能のエッセンスを消費してアイテムを複製する装置。",
        "detail": "A device that duplicates items by consuming universal essence.",
    },
    {
        "id": "ut_universal_factory",
        "name_JP": "ユニバーサルファクトリー",
        "name": "Universal Factory",
        "value": 200,
        "weight": 2000,
        "_tileType": "Obj",  # 小物件：可被行走跨过
        "factory": "ut_universal_tool",  # 在万能工作台制造
        "components": "log/4,plank/2,ingot/5,nail/10",  # 原木×4 + 木板×2 + 金属锭×5 + 钉子×10
        "trait": "Workbench",  # 游戏内置工作台：万能系列制造入口
        "detail_JP": "万能シリーズの製造拠点となる工場の作業台。",
        "detail": "A factory workbench that serves as the crafting hub of the universal series.",
    },
    {
        "id": "ut_decomposer",
        "name_JP": "ユニバーサルデコンポーザー",
        "name": "Universal Decomposer",
        "value": 200,
        "weight": 2000,
        "_tileType": "Obj",  # 小物件：可被行走跨过
        "factory": "ut_universal_factory",  # 在万能工厂制造
        "components": "ut_condenser/1,ingot/5,ut_universal_essence/10",  # 凝华器×1 + 金属锭×5 + 万能精华×10
        "trait": "Decomposer",  # 自定义：分解材料为万能精华（树枝×1 → 精华×100）
        "detail_JP": "材料を万能のエッセンスに分解する装置。",
        "detail": "A device that decomposes materials into universal essence.",
    },
    {
        "id": "ut_essence_battery",
        "name_JP": "エッセンスバッテリー",
        "name": "Essence Battery",
        "value": 200,
        "weight": 2000,
        "_tileType": "Obj",  # 小物件：可被行走跨过
        "electricity": 200,  # 放置时提供 200 电力
        "factory": "ut_universal_factory",  # 在万能工厂制造
        "components": "generator_man/1,ingot/20,ut_universal_essence/300",  # 强制劳动机×1 + 金属锭×20 + 万能精华×300
        "trait": "EssenceBattery",  # 自定义：TraitGenerator 子类，放置发电
        "detail_JP": "設置すると200Wの電力を供給するバッテリー。",
        "detail": "A battery that supplies 200W of electricity when placed.",
    },
    {
        "id": "ut_attribute_infuser",
        "name_JP": "アトリビュートインフューザー",
        "name": "Attribute Infuser",
        "value": 200,
        "weight": 2000,
        "_tileType": "Obj",  # 小物件：可被行走跨过
        "factory": "ut_universal_factory",  # 在万能工厂制造
        "components": "ut_amplifier/1,ingot/20,gem/10,ut_universal_essence/1000",  # 增幅器×1 + 金属锭×20 + 宝石×10 + 万能精华×1000
        "trait": "AttributeInfuser",  # 自定义：永久提升属性（每 N 精华 +1 点，向下取整）
        "detail_JP": "万能のエッセンスを消費して属性を永久に強化する装置。",
        "detail": "A device that permanently enhances attributes by consuming universal essence.",
    },
    {
        "id": "ut_repairer",
        "name_JP": "ユニバーサルリペアラー",
        "name": "Universal Repairer",
        "value": 200,
        "weight": 2000,
        "_tileType": "Obj",  # 小物件：可被行走跨过
        "factory": "ut_universal_factory",  # 在万能工厂制造
        "components": "ut_tool_workbench/1,ingot/10,ut_universal_essence/300",  # 工具装配台×1 + 金属锭×10 + 万能精华×300
        "trait": "Repairer",  # 自定义：去诅咒 / 鉴定装备（消耗万能精华）
        "detail_JP": "呪いを解いたり装備を鑑定したりする装置。",
        "detail": "A device that removes curses and identifies equipment using universal essence.",
    },
    {
        "id": "ut_auto_factory",
        "name_JP": "オートファクトリー",
        "name": "Auto Factory",
        "value": 200,
        "weight": 2000,
        "_tileType": "Obj",  # 小物件：可被行走跨过
        "factory": "ut_universal_tool",  # 在万能工作台制造（配方迁移，原在万能工厂）
        "components": "ut_universal_factory/1,ingot/50,ut_universal_essence/500",  # 万能工厂×1 + 金属锭×50 + 万能精华×500
        "trait": "Workbench",  # 游戏内置工作台：自动化设备制造
        "detail_JP": "自動化設備を製造するための作業台。",
        "detail": "A workbench for crafting automation devices.",
    },
    {
        "id": "ut_universal_generator",
        "name_JP": "ユニバーサルジェネレーター",
        "name": "Universal Generator",
        "value": 200,
        "weight": 2000,
        "_tileType": "Obj",  # 小物件：可被行走跨过
        "factory": "ut_universal_tool",  # 在万能工作台制造
        "components": "log/10,vine/10,nail/5",  # 原木×10 + 藤蔓×10 + 钉子×5
        "trait": "Workbench",  # 游戏内置工作台：生成水/万能肥料等基础物资
        "detail_JP": "水や万能肥料などの素材を生成する作業台。",
        "detail": "A workbench that generates basic materials such as water and universal fertilizer.",
    },
    {
        "id": "ut_fertilizer",
        "name_JP": "ユニバーサル肥料",
        "name": "Universal Fertilizer",
        "category": "fertilizer",  # 与本体肥料同类别：可像本体肥料一样放置到作物格施肥（用户改定，原 resource 不可放置）
        "value": 100,
        "weight": 100,  # 0.1
        "factory": "ut_universal_generator",  # 在万能生成器制造
        "components": "fertilizer/2,ut_universal_essence/10",  # 肥料×2 + 万能精华×10
        "trait": "UniversalFertilizer",  # 自定义：放到作物格上立即催熟到可收获阶段并自毁
        "detail_JP": "作物を収穫可能な段階まで一気に熟させる万能の肥料。",
        "detail": "Instantly ripens a crop to its harvestable stage when placed on it.",
    },
    {
        "id": "ut_catalyst",
        "name_JP": "ユニバーサルカタリスト",
        "name": "Universal Catalyst",
        "category": "resource",  # 资源类：可堆叠
        "value": 300,
        "weight": 100,  # 0.1
        "factory": "ut_universal_generator",  # 在万能生成器制造
        "components": "fertilizer/5,crystal_earth/1,crystal_sun/1,crystal_mana/1,ut_universal_essence/100",  # 肥料×5 + 三种结晶各×1 + 万能精华×100
        "detail_JP": "自動機械を駆動する触媒結晶。オートコンデンサー、オートデュプリケーター、オートジェネレーターの材料。",
        "detail": "A catalyst crystal that powers the auto condenser, auto duplicator, and auto generator.",
    },
    {
        "id": "ut_amulet",
        "name_JP": "ユニバーサルアミュレット",
        "name": "Universal Amulet",
        "category": "tool",  # 纯背包道具：不占装备位
        "value": 200,
        "weight": 100,  # 0.1
        "_tileType": "Obj",  # 小物件：可被行走跨过
        "factory": "ut_universal_factory",  # 在万能工厂制造
        "components": "gem/5,ingot/5,ut_universal_essence/1000",  # 宝石×5 + 金属锭×5 + 万能精华×1000
        "trait": "UniversalAmulet,2,1",  # 自定义容器 2×1：催化剂槽 + 精华槽；持有者死亡时消耗材料免疫死亡
        "detail_JP": "触媒100とエッセンス1000を消費して死亡を一度無効化する護符。",
        "detail": "Prevents the holder's death once by consuming 100 catalysts and 1000 essences.",
    },
    {
        "id": "ut_auto_duplicator",
        "name_JP": "オートデュプリケーター",
        "name": "Auto Duplicator",
        "value": 300,
        "weight": 2000,
        "_tileType": "Obj",  # 小物件：可被行走跨过
        "electricity": -20,
        "factory": "ut_auto_factory",  # 在自动工厂制造
        "components": "ut_duplicator/1,ingot/20,ut_universal_essence/300",  # 复制机×1 + 金属锭×20 + 万能精华×300
        "trait": "AutoDuplicator,3,1",
        "detail_JP": "本体に触媒を入れると毎時1個を消費し、隣接入力機の材料を10分ごとに複製する装置。",
        "detail": "Consumes one catalyst per hour while on and duplicates materials from an adjacent input unit every 10 minutes.",
    },
    {
        "id": "ut_auto_generator",
        "name_JP": "オートジェネレーター",
        "name": "Auto Generator",
        "value": 300,
        "weight": 2000,
        "_tileType": "Obj",  # 小物件：可被行走跨过
        "electricity": -1000,
        "factory": "ut_auto_factory",  # 在自动工厂制造
        "components": "ut_universal_generator/1,ingot/20,ut_universal_essence/300",  # 万能生成器×1 + 金属锭×20 + 万能精华×300
        "trait": "AutoGenerator",
        "detail_JP": "隣接入力機の触媒や肥料を10分ごとに自動生成する装置。電力を1000消費する。",
        "detail": "Directly processes universal catalysts or fertilizer from an adjacent input unit every 10 minutes and consumes 1000 electricity.",
    },
    {
        "id": "ut_auto_condenser",
        "name_JP": "オートコンデンサー",
        "name": "Auto Condenser",
        "value": 200,
        "weight": 2000,
        "_tileType": "Obj",  # 小物件：可被行走跨过
        "electricity": -20,
        "factory": "ut_auto_factory",  # 在自动工厂制造
        "components": "ut_condenser/1,ingot/20,ut_universal_essence/300",  # 凝华器×1 + 金属锭×20 + 万能精华×300
        "trait": "AutoCondenser,3,1",
        "detail_JP": "本体に触媒を入れると毎時1個を消費し、隣接入力機の結晶からエッセンスを生成する装置。",
        "detail": "Consumes one catalyst per hour while on and produces essence from crystals in an adjacent input unit.",
    },
    {
        "id": "ut_mineral_generator",
        "name_JP": "鉱石生成機",
        "name": "Mineral Generator",
        "value": 500,
        "weight": 2000,
        "_tileType": "Obj",
        "electricity": -100,
        "factory": "ut_auto_factory",
        "components": "ingot/50,ut_catalyst/10,ut_universal_essence/1000",
        "trait": "MineralGenerator,3,1",
        "detail_JP": "本体に原版の鉱石を入れ、隣接入力機の万能エッセンスを消費して同じ鉱石を生成する装置。",
        "detail": "Uses universal essence from an adjacent input feeder to generate copies of a vanilla ore stored in the machine.",
    },
    {
        "id": "ut_auto_smelter",
        "name_JP": "自動溶鉱炉",
        "name": "Auto Smelter",
        "value": 500,
        "weight": 2000,
        "_tileType": "Obj",
        "electricity": -20,
        "factory": "ut_auto_factory",
        "components": "furance/1,ut_catalyst/10,ut_universal_essence/1000",
        "trait": "AutoSmelter,3,1",
        "detail_JP": "本体に触媒を入れると毎時1個を消費し、隣接入力機の素材を原版の溶鉱炉レシピで10個まで自動精錬する装置。",
        "detail": "Consumes one catalyst per hour while on and processes up to 10 items from an adjacent input feeder with vanilla smelter recipes.",
    },
    {
        "id": "ut_input",
        "name_JP": "インプットユニット",
        "name": "Input Feeder",
        "value": 200,
        "weight": 2000,
        "_tileType": "Obj",  # 小物件：可被行走跨过
        "factory": "ut_auto_factory",  # 在自动工厂制造
        "components": "container_shipping/1,ut_universal_essence/100",  # 出货箱×1 + 万能精华×100
        "trait": "InputFeeder,4,3",  # 自定义容器 4×3（用户改定：6×3 显示不符预期，改回原格数）
        "detail_JP": "隣接する自動設備に材料を供給する容器。",
        "detail": "A container that feeds materials to adjacent auto devices.",
    },
    {
        "id": "ut_output",
        "name_JP": "アウトプットユニット",
        "name": "Output Feeder",
        "value": 200,
        "weight": 2000,
        "_tileType": "Obj",  # 小物件：可被行走跨过
        "factory": "ut_auto_factory",  # 在自动工厂制造
        "components": "container_shipping/1,ut_universal_essence/100",  # 出货箱×1 + 万能精华×100
        "trait": "OutputFeeder,4,3",  # 自定义容器 4×3：自动设备收货口；trait 参数 1/2 = 容器宽高
        "detail_JP": "隣接する自動設備の産物を受け取る容器。",
        "detail": "A container that receives products from adjacent auto devices.",
    },
    {
        "id": "ut_collector",
        "name_JP": "アイテムコレクター",
        "name": "Item Collector",
        "value": 200,
        "weight": 2000,
        "_tileType": "Obj",  # 小物件：可被行走跨过
        "factory": "ut_universal_factory",  # 在万能工厂制造
        "components": "container_shipping/1,ingot/5,ut_universal_essence/200",  # 出货箱×1 + 金属锭×5 + 万能精华×200
        "trait": "Collector,6,6",  # 自定义容器 6×6=36 格：定时收集周围散落物品存入自身
        "detail_JP": "周囲に落ちたアイテムを自動で回収し、自身の36スロット容器に収納する装置。",
        "detail": "Automatically collects nearby dropped items into its own 36-slot container.",
    },
    {
        "id": "ut_industrial_water",
        "name_JP": "工業用水",
        "name": "Industrial Water",
        "category": "drink",
        "value": 100,
        "weight": 1000,
        "defMat": "water",
        "trait": "IndustrialWater",
        "detail_JP": "原版の水と同じ性質を持つ工業用水。",
        "detail": "Industrial water with the same properties as vanilla water.",
    },
    {
        "id": "ut_fluid_extractor",
        "name_JP": "流体抽出器",
        "name": "Fluid Extractor",
        "value": 300,
        "weight": 2000,
        "_tileType": "Obj",
        "electricity": -50,
        "factory": "ut_auto_factory",
        "components": "water/5,ut_input/1,ut_catalyst/1,ut_universal_essence/1000",
        "trait": "FluidExtractor,6,6",
        "detail_JP": "水方格に隣接すると、毎時工業用水を生産する36スロットの抽出器。",
        "detail": "A 36-slot extractor that produces industrial water every hour when placed next to a water tile.",
    },
    {
        "id": "ut_auto_farming",
        "name_JP": "オートファーマー",
        "name": "Auto Farming Machine",
        "value": 300,
        "weight": 2000,
        "_tileType": "Obj",
        "electricity": -50,
        "factory": "ut_auto_factory",
        "components": "seed/50,ut_input/1,ut_catalyst/1,ut_universal_essence/1000",
        "trait": "AutoFarming,6,6",
        "detail_JP": "種子を収納すると、指定範囲へ毎時自動で植え付け、工業用水があれば散水する装置。",
        "detail": "A 36-slot machine that plants stored seeds row by row every hour and waters them with industrial water.",
    },
    {
        "id": "ut_pipe_input",
        "name_JP": "入力ターミナル",
        "name": "Input Terminal",
        "value": 200,
        "weight": 2000,
        "_tileType": "Obj",  # 小物件：可被行走跨过
        "factory": "ut_auto_factory",  # 在自动工厂制造
        "components": "ut_input/1,ut_catalyst/1",  # 输入器×1 + 万能催化剂×1
        "trait": "PipeInput",  # 自定义：频道制取料口（同频道输出器配对，顺序/轮询模式）
        "detail_JP": "同じチャンネルの出力ユニットへ、品物を自動で送り出す装置。",
        "detail": "Sends item stacks automatically to outputs on the same channel.",
    },
    {
        "id": "ut_pipe_output",
        "name_JP": "出力ターミナル",
        "name": "Output Terminal",
        "value": 200,
        "weight": 2000,
        "_tileType": "Obj",  # 小物件：可被行走跨过
        "factory": "ut_auto_factory",  # 在自动工厂制造
        "components": "ut_output/1,ut_catalyst/1",  # 输出器×1 + 万能催化剂×1
        "trait": "PipeOutput",  # 自定义：频道制出货口（同频道输入器配对）
        "detail_JP": "同じチャンネルのインプットから品物を受け取る装置。",
        "detail": "Receives items from inputs on the same channel.",
    },
]

# Recipe 表（混合机专用）：精华提取机的配方
RECIPE_HEADER = ["id", "factory", "type", "thing", "num", "sp", "time", "ing1", "ing2", "ing3", "tag"]
RECIPE_TYPES = ["int", "string", "string", "string", "string", "int", "int", "string[]", "string[]", "string[]", "string[]"]
RECIPE_DEFAULTS = [None] * 11
RECIPE_ROWS = [
    # 凝华器（numIng=2，两个材料槽位）：结晶/锭 提炼成万能精华，全部 2 材料配方
    {
        "id": 9001, "factory": "EssenceExtractor", "type": "Resource",
        "thing": "ut_universal_essence", "num": "10", "sp": 0, "time": 2,
        "ing1": "crystal_earth", "ing2": "crystal_earth", "tag": "known",
    },
    {
        "id": 9002, "factory": "EssenceExtractor", "type": "Resource",
        "thing": "ut_universal_essence", "num": "30", "sp": 0, "time": 2,
        "ing1": "crystal_sun", "ing2": "crystal_sun", "tag": "known",
    },
    {
        "id": 9003, "factory": "EssenceExtractor", "type": "Resource",
        "thing": "ut_universal_essence", "num": "100", "sp": 0, "time": 2,
        "ing1": "crystal_mana", "ing2": "crystal_mana", "tag": "known",
    },
    {
        "id": 9004, "factory": "EssenceExtractor", "type": "Resource",
        "thing": "ut_universal_essence", "num": "5", "sp": 0, "time": 2,
        "ing1": "crystal_earth", "ing2": "ingot", "tag": "known",
    },
    {
        "id": 9005, "factory": "EssenceExtractor", "type": "Resource",
        "thing": "ut_universal_essence", "num": "15", "sp": 0, "time": 2,
        "ing1": "crystal_sun", "ing2": "ingot", "tag": "known",
    },
    {
        "id": 9006, "factory": "EssenceExtractor", "type": "Resource",
        "thing": "ut_universal_essence", "num": "50", "sp": 0, "time": 2,
        "ing1": "crystal_mana", "ing2": "ingot", "tag": "known",
    },
    {
        "id": 9007, "factory": "EssenceExtractor", "type": "Resource",
        "thing": "ut_universal_essence", "num": "1", "sp": 0, "time": 2,
        "ing1": "ingot", "ing2": "ingot", "tag": "known",
    },
    # 万能分解机（numIng=1，单材料槽位）：回收配方，GetSource 重写按 tag+类型/rarity 精确匹配
    {
        "id": 9008, "factory": "Decomposer", "type": "Resource",
        "thing": "ut_universal_essence", "num": "1", "sp": 0, "time": 1,
        "ing1": "#junk", "tag": "known,junk",
    },
    {
        "id": 9009, "factory": "Decomposer", "type": "Resource",
        "thing": "ut_universal_essence", "num": "100", "sp": 0, "time": 1,
        "ing1": "#book", "tag": "known,book",
    },
    {
        "id": 9010, "factory": "Decomposer", "type": "Resource",
        "thing": "ut_universal_essence", "num": "10", "sp": 0, "time": 1,
        "ing1": "#weapon", "tag": "known,equip_normal",
    },
    {
        "id": 9011, "factory": "Decomposer", "type": "Resource",
        "thing": "ut_universal_essence", "num": "50", "sp": 0, "time": 1,
        "ing1": "#weapon", "tag": "known,equip_rare",
    },
    {
        "id": 9012, "factory": "Decomposer", "type": "Resource",
        "thing": "ut_universal_essence", "num": "100", "sp": 0, "time": 1,
        "ing1": "#weapon", "tag": "known,equip_high",
    },
]

OUT = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "LangMod", "EN", "UniversalTool.xlsx")
os.makedirs(os.path.dirname(OUT), exist_ok=True)

wb = xlsxwriter.Workbook(OUT)
ws = wb.add_worksheet("Thing")
ws.write_row(0, 0, HEADER)
ws.write_row(1, 0, TYPES)
ws.write_row(2, 0, DEFAULTS)
for i, row in enumerate(ROWS):
    values = [row.get(h) for h in HEADER]
    ws.write_row(3 + i, 0, values)

# Recipe sheet（混合机配方）：sheet 名 "Recipe"（大写，与官方文档一致；大小写均可，日志证实大写也能加载）
wsr = wb.add_worksheet("Recipe")
wsr.write_row(0, 0, RECIPE_HEADER)
wsr.write_row(1, 0, RECIPE_TYPES)
wsr.write_row(2, 0, RECIPE_DEFAULTS)
for i, row in enumerate(RECIPE_ROWS):
    values = [row.get(h) for h in RECIPE_HEADER]
    wsr.write_row(3 + i, 0, values)

wb.close()
print("written:", OUT)
