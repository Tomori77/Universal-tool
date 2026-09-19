# Universal tool · 万能工具

> 一个面向 [Elin](https://store.steampowered.com/app/2135150/Elin/)（EA 0.23.x）的大型综合 Mod，提供万能工作台、资源加工、便携工具、属性成长、自动化设备与跨位置物流。
> 本仓库同时作为 Mod 制作参考：包含完整源码、数据表生成脚本、三语言本地化、贴图，以及一套可复用的开发文档与工具链。

![预览](Universal%20tool/preview.jpg)

## 特性

- **核心制造链**：万能工作台 → 万能工厂 → 自动工厂，逐级解锁。
- **资源与加工**：万能精华、凝华器、万能分解机、复制器、万能肥料、万能催化剂。
- **工具与便携设备**：镐尖斧（伐木/挖矿/挖掘三合一）、便携工作台、便携出货箱。
- **训练与属性**：技能训练器、增幅器、属性灌注器、万能修复器、精华电池、万能护身符。
- **自动化设备**：自动凝华器、自动复制机、自动生成器、矿物生成机、自动熔炉、自动农业机、流体抽取器、物品收集器。
- **物流系统**：输入器/输出器、输入终端/输出终端（频道配对、速率与类别筛选）。
- **游戏内设置**：通过 Mod Options 提供总开关、倍率、上限与消耗等配置；未安装 Mod Options 时回退到 BepInEx cfg。

完整物品说明见 [`Universal tool/文档/物品清单.md`](Universal%20tool/文档/物品清单.md)。

## 安装（玩家）

1. 确认游戏已安装 BepInEx 6.0.0-pre.1（Elin 通常自带）。
2. 将 `Universal tool` 文件夹整体复制到游戏目录：

   ```text
   <Elin>\Package\Universal tool\
   ```

3. 可选：从创意工坊订阅 **Mod Options**（ID `3381182341`）以获得游戏内设置页面；未安装时配置仍写入 `BepInEx\config\`。
4. 启动游戏，确认 `BepInEx\LogOutput.log` 中有插件加载记录。

发布包结构：

```text
Universal tool\
├── UniversalTool.dll
├── package.xml
├── preview.jpg
├── LangMod\    # 中/英/日 本地化
└── Texture\    # 物品贴图
```

## 从源码编译

需要 .NET SDK（目标框架 `net472`）。

1. 打开 `Universal tool/Universal tool.csproj`，把游戏程序集路径 `D:\Steam\steamapps\common\Elin\...` 改为本机实际路径。
2. 若需要设置菜单，将第三方 `ModOptions.dll` 放到 `docs/refs/ModOptions.dll`（该目录不随仓库上传，需自行准备）。
3. 编译：

   ```powershell
   dotnet build "Universal tool\Universal tool.csproj" -c Release
   ```

编译产物为 `UniversalTool.dll`，连同 `package.xml`、`LangMod\`、`Texture\`、`preview.jpg` 一起构成发布包。

> 数据表由 [`Universal tool/Script/make_xlsx.py`](Universal%20tool/Script/make_xlsx.py) 生成，输出 `LangMod/EN/UniversalTool.xlsx`；修改物品后需重新生成。

## 仓库结构

```text
Universal-tool\
├── Universal tool\      # Mod 工程
│   ├── Script\          # C# 源码（Trait、Harmony、配置、调度器）
│   ├── LangMod\         # xlsx 数据表与三语言本地化
│   ├── Texture\         # 物品贴图
│   ├── 文档\            # 设计规范、物品清单、更新公告
│   ├── package.xml
│   └── preview.jpg
├── AI-Mod制作教程\      # 入门与专题教程（00~04）
├── tools\               # 辅助工具（贴图脚本、ID 查询、生图提示词）
├── Mod制作规范.md        # 流程与发布规则总纲
├── 反编译速查.md         # 已验证 API 与机制
├── 工具与依赖.md         # 环境、路径、命令与依赖
└── docs\                # 本地资料，不上传（见 .gitignore）
```

## 文档导航

| 需求 | 文档 |
|---|---|
| 制作流程、目录、发布标准 | [`Mod制作规范.md`](Mod制作规范.md) |
| 已验证 API、机制与坑点 | [`反编译速查.md`](反编译速查.md) |
| 工具、路径、命令与依赖 | [`工具与依赖.md`](工具与依赖.md) |
| 入门与专题教程 | [`AI-Mod制作教程/`](AI-Mod制作教程/) |
| 物品说明 | [`Universal tool/文档/物品清单.md`](Universal%20tool/文档/物品清单.md) |
| 版本变化 | [`Universal tool/文档/更新公告.md`](Universal%20tool/文档/更新公告.md) |

## 已知问题

以下功能已实现并通过编译，但尚未完成游戏内验证，使用前请留意：

- 万能肥料的实际放置与消耗流程；
- 万能护身符的死亡拦截；
- 矿物生成机的原版矿石识别与材质复制；
- 自动熔炉的原版配方匹配与批量混合处理；
- 流体抽取器的水方格检测；
- 自动农业机的播种与浇水；
- 频道终端的持久化与类别筛选；
- 自动设备的离家补算、输入输出与耗电。

## 开发约定

- 自定义 Trait 使用全局命名空间，类名为 `Trait` + 数据表 trait 值。
- 每 10 分钟运行的自动设备统一接入 `Script/TraitAutomationScheduler.cs`，设备只实现单次 Tick。
- 数据表使用 xlsxwriter 生成，Thing/Recipe 均为三行头。
- 修改已有官方物品时使用运行时补丁，不在 xlsx 中整行覆盖。

## 许可与致谢

- 本仓库目前未附加开源许可证；如需复用代码或资源，请先联系作者。
- 参考模组：ElinEnchantingTable、InstantFishing。
- 官方 Modding 文档：[Elin Modding Wiki](https://elin-modding-resources.github.io/Elin.Docs/)。
