# 02 · Trait 与游戏机制

> 本页负责物品特殊行为、设备机制和 Harmony。具体 API 结论以根目录 `反编译速查.md` 为准；工程和构建查 `工具与依赖.md`。

## 1. 自定义 Trait 三条规则

1. 类放在全局命名空间。
2. 类名为 `Trait` + Excel `trait` 列值，例如 `PortableWorkbench` 对应 `TraitPortableWorkbench`。
3. 继承合适的游戏基类：`Trait`、`TraitTool`、`TraitCrafter`、`TraitContainer` 或 `TraitShippingChest`。

自定义 Trait 生效前，在插件 `Awake` 中把当前程序集加入 `ClassCache.assemblies`。完整入口由根目录规范和项目示例提供。

常用继承关系：

```text
Trait
├── TraitContainer
│   └── TraitShippingChest
├── TraitCrafter
│   └── TraitFactory
│       └── TraitWorkbench
└── TraitTool
```

## 2. 交互选择

- 背包出现“使用”：重写 `CanUse(Chara)` 返回 `true`，执行逻辑放 `OnUse`。
- 地图右键：实现 `TrySetAct(ActPlan)`。
- `TraitCrafter.HoldAsDefaultInteraction` 默认为 `true`；便携设备需要右键使用时重写为 `false`。
- 不需要自己制作窗口，优先组合 `LayerList`、`LayerCraft` 和上下文菜单等现成 UI。

## 3. 工具与采集

采集能力由物品实例元素判断，不应只填写 Thing 表的 `elements`。自定义工具在 Trait 的 `OnCreate` 中注入采集元素：

```csharp
owner.elements.SetBase(220, 1); // 挖矿
owner.elements.SetBase(225, 1); // 伐木
owner.elements.SetBase(230, 1); // 挖掘
```

工具使用非 melee 分类，`offense` 留空，避免被当成武器。工具硬度由制作材料影响，通常不需要另写硬度公式。

## 4. 容器与出货箱

- 容器继承 `TraitContainer`，格子可由 Trait 参数设置。
- 背包容器启用 `CanOpenContainer` 后可获得“打开”交互。
- 自定义容器覆写空的 `Prespawn`，避免基类随机填入杂物。
- 出货箱使用 `TraitShippingChest`；大出货箱 id 使用前先检查源表，不存在时回退普通出货箱。

## 5. 发电与自动化

- 发电量由 Thing 的 `electricity` 列决定；正数发电、负数耗电。
- 自动发电设备可继承 `TraitGenerator`；人力设备使用 `TraitGeneratorWheel`。
- 地图设备的周期处理使用 `Trait.OnSimulateHour`，离家后会按小时补跑。
- 相邻设备使用 `ForeachNeighbor`、`FindThing<T>` 和 `point.Things`。
- 搬运到容器使用 `AddCard`，它会自动从原位置摘除。
- 开关状态为 `owner.isOn`，切换使用 Trait 的 `Toggle`。
- 放置行为使用 `OnChangePlaceState`；作物催熟使用 `growth.SetStage`。

## 6. 技能、属性和状态

- 技能等级：`elements.ModBase`；经验：`ModExp`；学习：`Learn`。
- 永久属性使用 `chara.elements.ModBase`；临时增益使用 `Chara.ModTempElement`。
- 临时容器可能为 null；需要突破 `ModTempElement` 上限时，初始化后直接操作 `tempElements`。
- 鉴定、去诅咒和施法使用游戏现成效果 API，具体 id 和门槛查速查文档。
- 角色绑定的持久状态优先使用自定义 Condition；物品设置使用 uid + 独立 CSV。

## 7. 目标进入设备

需要让角色进入机器时，参考原版基因合成机模式：

1. 用 `LayerPeople.CreateSelect` 选择角色。
2. 将角色传送到机器位置并记录机器 uid。
3. 使用自定义 Condition 保存舱内状态和计时。
4. 到期后移除状态并把角色移出设备。

不要直接复用硬编码查找基因机器的 `ConSuspend`，应自建状态类。

## 8. Harmony 使用原则

适合使用 Harmony 的场景：

- 修改原版源表已有物品的少数字段；
- 接管非 virtual 方法的匹配逻辑；
- 修改配方产出、采集结果或角色状态；
- 让背包物品在非热键栏也触发周期逻辑。

补丁完成后必须有可观察验证：日志、UI 变化或游戏内行为。不要用 Harmony 替代能用数据表或现成 Trait 完成的功能。

## 9. 验证

1. 工程编译无错误。
2. 日志出现插件加载和自定义类型注册记录。
3. 物品制造或放置后交互符合设计。
4. 设备周期、电力、容器、技能或状态行为符合预期。
5. Harmony 补丁有日志或游戏内结果证明已生效。
