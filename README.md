# ggj-2026a1

本仓库为 Unity 游戏项目（GGJ 2026）。

## 开发环境

- Unity：2022.3.62f3c1

## 配置表（ExcelKit）

本项目通过 Excel 生成 C# 配置类与 JSON 配置数据：

- Excel 源表：`Assets/ExcelConfigs/*.xlsx`
- 生成 C#：`Assets/Scripts/Configs/*.cs`（生成物，不要手改）
- 生成 JSON：`Assets/Resources/JsonConfigs/*.json`（生成物，不要手改）

生成方式：

- Unity 菜单：`Tools/Excel To Json`
- 命令行（Windows）：

```bat
mkdir Logs
"%UNITY_EDITOR%" -batchmode -nographics -projectPath . -executeMethod SimpleToolkits.Editor.EditorUtils.GenerateConfigs -logFile Logs/excel-gen.log -quit
```

## 物品系统（右侧物品栏）

目标：点击解密类偏收集物品栏；物品不会消耗；列表视觉上“从下到上排序”（越上越新）；点击槽位弹出物品信息。

### 物品定义表

物品静态信息来自 `Assets/ExcelConfigs/Items.xlsx`（生成类型：`ItemsConfig`）。字段约定：

- `id`：唯一 ID（必填）
- `name`：显示名称
- `desc`：描述文本
- `iconPath`：图标资源路径（`Resources.Load<Sprite>` 用；不带扩展名）

示例：若图标在 `Assets/Resources/Icons/key.png`，则 `iconPath` 填 `Icons/key`。

### 收集物品

新增需求：添加物品只需要 `id`。

```csharp
// 玩法层调用（UI 单例）
InventoryPanel.Instance.TryCollect("item_test");

// 或者不依赖 UI
Ggj.Inventory.InventoryService.TryCollect("item_test");
```

### UI 行为

- 物品槽：使用 `Resources/Prefabs/ItemSlot.prefab` 作为模板实例化。
- 排序：新收集的物品会插入到 Content 顶部，视觉上形成“从下到上”的排序。
- 点击槽位：弹出物品信息面板（遮罩点击/右上角关闭）。

## 约束与规范

详细的代理开发规范与常用命令请查看：`AGENTS.md`（所有对外说明/提交信息使用简体中文）。
