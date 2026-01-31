# AGENTS.md（面向本仓库的智能代理开发规范）
本文件用于约束/指导在 `ggj-2026a1` 仓库内工作的“代理式编码工具”（AI agents）。

## 0. 硬性约束（必须遵守）
- **语言**：所有对外说明、文档、关键注释、Git 提交信息一律使用简体中文。
- **工程目标**：高内聚、低耦合；严格遵循 SOLID / DRY / KISS / YAGNI；优先可测试性。
- **安全**：严禁硬编码密钥/Token；任何外部输入必须校验/清洗。
- **生成物**：不要手改 Unity 自动生成的 `*.csproj`/`*.sln`；需要更新请在 Unity 中重新生成。

## 1. 项目概览（从仓库扫描得到）
- Unity：`2022.3.62f3c1`（见 `ProjectSettings/ProjectVersion.txt`）
- C#：LangVersion `9.0`；目标框架 `v4.7.1`（见 `Assembly-CSharp.csproj`）
- 主要依赖（`Packages/manifest.json`）：URP、UniTask、PrimeTween、Newtonsoft.Json、Unity Test Framework
- 编辑器工具：`Tools/Excel To Json`（`Assets/Editor/EditorUtils.cs`）

## 2. 常用命令（Build / Lint / Test）
说明：Unity 的“命令行”本质是启动 Editor 的批处理模式。以下命令均以 Windows 为例。
提示：命令行跑测试/编译前，先创建输出目录（避免 `-logFile`/`-testResults` 写入失败）：
```bat
mkdir Logs TestResults
```
### 2.1 设置 Unity 可执行文件路径
建议用环境变量统一管理 Unity 路径（不同机器安装盘符可能不同）：
```bat
set UNITY_EDITOR=D:\Program Files\Unity\Hub\Editor\2022.3.62f3c1\Editor\Unity.exe
```
也可以把 `UNITY_EDITOR` 改成你本机 Unity.exe 的真实路径。
### 2.2 “Lint”：仅做编译检查（当前仓库未单独配置格式化/静态检查工具）
```bat
"%UNITY_EDITOR%" -batchmode -nographics -projectPath . -logFile Logs/unity-compile.log -quit
```
- 目标：触发脚本编译并在 `Logs/unity-compile.log` 中检查 `error CS`、`Compilation failed` 等关键字。
- 说明：Unity 自带/IDE 自带的分析器（Unity Analyzers 等）属于“软 lint”，以不引入新噪音为准。
### 2.3 运行所有测试
EditMode：
```bat
"%UNITY_EDITOR%" -batchmode -nographics -projectPath . -runTests -testPlatform EditMode -testResults TestResults/editmode.xml -logFile Logs/editmode.log -quit
```
PlayMode：
```bat
"%UNITY_EDITOR%" -batchmode -nographics -projectPath . -runTests -testPlatform PlayMode -testResults TestResults/playmode.xml -logFile Logs/playmode.log -quit
```
### 2.4 运行单个测试（重点）
优先使用 `-testFilter`（支持“完整测试名”或“子串匹配”）。示例：
```bat
"%UNITY_EDITOR%" -batchmode -nographics -projectPath . -runTests -testPlatform EditMode -testFilter "PuzzleProgressTests.IsSolved_WhenMarkedSolved_ReturnsTrue" -testResults TestResults/single.xml -logFile Logs/single-test.log -quit
```
常用变体：
```bat
REM 1) 按类名/命名空间子串过滤（更宽松，但可能跑到多个用例）
"%UNITY_EDITOR%" -batchmode -nographics -projectPath . -runTests -testPlatform EditMode -testFilter "RuntimePrefsTests" -testResults TestResults/single.xml -logFile Logs/single-test.log -quit
REM 2) 按分类运行（需要在测试上标注 [Category("...")]）
"%UNITY_EDITOR%" -batchmode -nographics -projectPath . -runTests -testPlatform EditMode -testCategory "Smoke" -testResults TestResults/smoke.xml -logFile Logs/smoke.log -quit
```
定位测试名的推荐方式：
- Unity Editor：`Window > General > Test Runner`，复制用例名
- 或先跑一次全量测试，再从 `TestResults/*.xml` 中查找 FullyQualifiedName
### 2.5 生成 Excel 配置（Excel -> C# -> JSON）
编辑器菜单：`Tools/Excel To Json`（会删除并重建输出目录，属于“破坏性生成”）。
命令行（等价于点击菜单）：
```bat
"%UNITY_EDITOR%" -batchmode -nographics -projectPath . -executeMethod SimpleToolkits.Editor.EditorUtils.GenerateConfigs -logFile Logs/excel-gen.log -quit
```
约定：
- 源数据：`Assets/ExcelConfigs/`
- 生成 C#：`Assets/Scripts/Configs/`（例如 `LanguagesConfig.cs`）
- 生成 JSON：`Assets/Resources/JsonConfigs/`
- 生成物不要手改；如需改数据，改 Excel 并重新生成
### 2.6 构建 Player（当前仓库缺少可批处理的 BuildScript）
- 现状：仓库内未提供 `-executeMethod` 可调用的构建入口。
- 建议：新增 `Assets/Editor/Build/BuildScript.cs`（静态方法 + 明确参数），再用以下方式构建：
```bat
REM 示例（需要你实现 BuildScript.BuildWindows64）
"%UNITY_EDITOR%" -batchmode -nographics -projectPath . -executeMethod BuildScript.BuildWindows64 -logFile Logs/build.log -quit
```

### 2.7 VSCode 调试（可选）
- 推荐插件：`visualstudiotoolsforunity.vstuc`（见 `.vscode/extensions.json`）
- 启动 Unity 后，在 VSCode 运行 “Attach to Unity”（见 `.vscode/launch.json`）

### 2.8 重新生成工程文件（当 asmdef/包引用变更后）
- 由 Unity 重新生成 `*.csproj`/`*.sln`，不要手改生成文件
- 常用入口：Unity `Preferences > External Tools > Regenerate project files`
- VSCode 侧默认 solution：`ggj-2026a1.sln`（见 `.vscode/settings.json`）

## 3. 代码组织与模块化（强制）
- **命名空间**：默认不使用自定义命名空间（不写 `namespace ...`），统一使用“默认命名空间”。
- **模块拆分优先 asmdef**：每个功能一个运行时程序集 +（可选）编辑器程序集 + 测试程序集。
- **Editor 隔离**：Editor 代码只能放在 `Assets/Editor/` 或 `*.Editor.asmdef`，运行时代码禁止引用 `UnityEditor`。
- **MonoBehaviour 变薄**：MonoBehaviour 只负责生命周期/Unity 适配；业务逻辑下沉为纯 C#（可 EditMode 测试）。
推荐目录模板（新增模块时按此落地）：
- `Assets/Scripts/<Feature>/`（含 `<Feature>.Runtime.asmdef`）
- `Assets/Scripts/<Feature>/Editor/`（含 `<Feature>.Editor.asmdef`）
- `Assets/Tests/<Mode>/`（含测试程序集 asmdef）

## 3.1 玩法系统约定（当前版本）
- **交互系统**：`InteractiveItem` 负责点击/悬停/提示点；交互结果通过“条件 + 动作”组合实现：
  - 条件：`PickupCondition`（可挂多个，例如 `PuzzleSolvedCondition`）
  - 动作：`InteractionAction`（例如 `CollectToInventoryAction`、`CompleteLevelAction`、`AcquireMaskAction`）
- **关卡系统**：关卡内容尽量做成 Prefab，运行时由 `WorldMgr` 下的 `LevelPrefabSwitcher` 动态实例化挂载。
- **双重世界**：每个关卡 Prefab 内包含 `DualWorldLevel`，并约定两个子节点：`World_NoMask` 与 `World_Mask`。
- **面具系统**：面具不进入物品栏；拾取后在左侧常驻按钮（Resources 预制体）并允许按空格/点击切换世界。

## 4. 代码风格规范（C# 9 / Unity）
### 4.1 using/import 规则
- 分组顺序：`System*` -> 第三方（Cysharp/Newtonsoft/PrimeTween/…）-> `Unity*` -> 项目内部
- 组与组之间空一行；同组按字母排序
- 避免 `using static`（除非明显提升可读性）
### 4.2 格式化（统一风格）
- 4 空格缩进；大括号独占一行；禁止混用 tab
- 尽量控制每行 <= 120 字符（过长就拆行）
- 公开 API 必须写显式访问修饰符（`public/internal/private`）
### 4.3 命名约定
- 类型/方法/属性：`PascalCase`
- 接口：`I + PascalCase`
- 私有字段：`_camelCase`；序列化字段使用 `[SerializeField] private`，不要 `public` 暴露
- 布尔命名：`Is/Has/Can/Should` 前缀
- 文件名必须与主类型一致（一个文件一个主类型）
### 4.4 类型、可空与返回值语义
- 明确 null 语义：能失败的查询优先 `TryGetX(out ...)`，其次返回 `null` 并在调用方处理
- 避免在“Get”方法里频繁 `Debug.LogWarning` 造成噪音；高频路径用返回值表达失败
- 常量/配置键统一集中管理，禁止散落魔法字符串
### 4.5 错误处理与日志
- 先校验再执行：对外部输入使用 guard clauses（空、越界、非法值）
- 运行时异常：优先“可恢复”路径；不可恢复则 `Debug.LogError` + 尽早失败（避免沉默）
- 禁止在 `Update()`/高频回调里 `try/catch` 或拼接大量字符串（会产生 GC 与卡顿）
- 日志必须带上下文：对象名/关键 id/场景/模块名，必要时传 `context` 对象
### 4.6 异步（UniTask）
- Unity 异步优先 `UniTask`；避免 `async void`（UI 事件除外）
- Fire-and-forget 必须可观测：`.Forget()` 需要捕获异常并记录（或集中处理）
- 需要生命周期的异步必须支持取消：优先传 `CancellationToken`（绑定 `OnDestroy`）

### 4.7 资源/配置与依赖管理
- 允许使用 `Resources` 快速迭代，但必须封装在单一入口（如 Config/Audio/Locale 管理器）
- 禁止在业务代码里散落 `Resources.Load("Magic/Path")` 与魔法字符串；统一常量/键管理
- 生成配置（Excel->C#->JSON）属于“生成物”，不要手改，必要时可重跑生成

### 4.8 性能与 GC
- 禁止在 `Update()`/高频回调中使用 LINQ、频繁装箱、频繁字符串拼接
- 对象/组件引用要缓存（GetComponent/FindObjectOfType 属于慢路径，避免在热路径调用）
- 日志默认应可关闭或降频；不要刷屏（尤其移动端）

### 4.9 注释与可读性
- 注释优先解释“为什么/约束/边界条件”，避免复述代码
- 复杂逻辑写成小函数 + 好名字；必要时补一段中文注释说明意图

## 5. 测试规范（必须可测试）
- **新增/重构核心逻辑必须补测试**：优先 EditMode（纯 C#），必要时 PlayMode（场景/MonoBehaviour）。
- 测试命名：`<ClassName>Tests`；用例：`<Method>_<Scenario>_<Expected>`（英文命名，中文注释说明意图）。
- 禁止不稳定测试：涉及时间/异步必须设置超时、去随机化、避免依赖真实帧率。

## 6. Cursor / Copilot 规则（仓库扫描结果）
- Cursor：未发现 `.cursor/rules/` 或 `.cursorrules`
- Copilot：未发现 `.github/copilot-instructions.md`
如后续添加，请把其内容/要求同步到本文件对应章节。
