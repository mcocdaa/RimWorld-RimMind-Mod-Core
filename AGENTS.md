# AGENTS.md — RimMind-Core

AI 客户端基础设施层，所有子模组的前置依赖。

## 项目定位

LLM 客户端(OpenAI+Player2)、异步请求队列、ContextEngine(L0-L4分层+Diff+BudgetScheduler)、Agent认知(PawnAgent+AgentBus+GoalStack)、NPC系统(NpcManager+IStorageDriver)、感知桥接(5个Patch)、数据飞轮(Flywheel)、SkipCheck互斥、审批悬浮窗、多分页设置UI。

## 构建

| 项 | 值 |
|----|-----|
| Target | 游戏运行时 net48；Domain/Application 同时提供 net10.0 测试目标；C#9.0，Nullable enable |
| RimWorld | 1.6 |
| Output | `../1.6/Assemblies/` |
| NuGet | Krafs.Rimworld.Ref 1.6.*, Lib.Harmony.Ref 2.*, Newtonsoft.Json 13.0.* |

## 源码结构

```
Source/
├── AICoreMod.cs / AICoreAPI.cs       Mod入口 + 静态公共API(RimMindAPI)
├── Client/                            OpenAI + Player2 客户端
├── Core/
│   ├── AIRequestQueue.cs             GameComponent异步队列
│   ├── Context/                       ContextEngine + KeyRegistry + BudgetScheduler + HistoryManager
│   ├── Agent/                         PawnAgent + AgentGoalStack + AgentBus + PerceptionPipeline
│   ├── Flywheel/                      FlywheelGameComponent + RuleEngine + ParameterStore
│   ├── Perception/PerceptionBridge.cs  感知桥接
│   └── Prompt/                        StructuredPromptBuilder + PromptSection + PromptBudget
├── Npc/                               NpcManager + StorageDriver(Local/Player2/Hybrid)
├── Comps/CompPawnAgent.cs            Agent ThingComp
├── Settings/                          AICoreSettings + ContextSettings
├── UI/                                SettingsUI + AgentDialogue + RequestOverlay
└── Patch/                             5个PerceptionBridge Patch + AITogglePatch + UIRoot
```

## 关键 API

```csharp
// 请求
RimMindAPI.RequestAsync(req, callback) / RequestStructuredAsync(req, schema, cb, tools)
RimMindAPI.Chat(ctxReq, ct) / CancelRequest(id) / PauseQueue() / ResumeQueue()

// Provider 注册(推荐路径)
ContextKeyRegistry.Register(key, layer, priority, pawn => content, ownerMod)

// 卸载
RimMindAPI.UnregisterModProviders(modId)

// UI扩展
RimMindAPI.RegisterSettingsTab(id, labelFn, drawFn)
RimMindAPI.RegisterToggleBehavior(id, isActive, toggle)

// SkipCheck互斥
RimMindAPI.RegisterDialogueSkipCheck / RegisterActionSkipCheck / RegisterFloatMenuSkipCheck

// 审批
RimMindAPI.RegisterPendingRequest(entry)
```

## 响应解析

统一使用 `<TagName>{JSON}</TagName>` → `JsonTagExtractor.Extract<T>(content, tag)` 解析。

## 代码约定

- Harmony ID: `mcocdaa.RimMindCore`，PostFix优先
- GameComponent 必须有 `(Game game)` 签名，RimWorld反射自动发现
- UI 文本通过 `Languages/*/Keyed/RimMind_Core.xml` Keyed翻译，禁止硬编码中文
- `ModSettings` → `ExposeData()` + `base.ExposeData()`；`ThingComp` → `PostExposeData()`
- 日志前缀 `[RimMind-Core]`

## 线程安全

- 主线程：读写游戏状态、消费ConcurrentQueue、所有RimWorld/Unity API
- 后台线程：HTTP请求、JSON解析，回调通过 `LongEventHandler.ExecuteWhenFinished` 调度回主线程
- AgentBus：Publish主线程同步，PublishFromBackground后台入队主线程消费
- **严禁**后台线程调用任何RimWorld/Unity API

## 审查状态（2026-07-31）

### 本轮关闭

- ProviderRegistry 保存 owner/priority，按优先级确定性选择，支持 `UnregisterModProviders(owner)` 与低优先级回退；同类型 provider 覆盖会记录结构化警告。
- HistoryManager 按完整轮次与 scenario 读取，自动执行 200→150 容量收敛，并由 GameComponent 持久化；pending turn 不写入存档。
- Context provider 注册、覆盖、按 owner 注销和 runtime shutdown 均释放 AgentBus 失效订阅；注册项与订阅替换使用同一串行化生命周期。
- Agent Loop 的 `MaxToolCallDepth` 已从设置注入，Core 内重复/无效深度状态已移除。
- ContextSettings 的 BudgetW1/BudgetW2 双源 UI/API 已移除；旧 Scribe key 仅在读档阶段读取并丢弃，FlywheelParameterStore 继续作为 W1/W2 唯一来源。
- Agent identity、action bridge、parameter tuner 与 typed provider 的替换均具有可发现警告；parameter tuner UI 读取复用只读快照，不再每次分配 List。
- 旧 r6 清单中的 LocalStorageDriver、StorageDriverFactory 与旧 AICoreAPI.Chat 路径已随架构硬切删除；OpenAI capability cache、EmbedCache 和现行注册表均使用线程安全实现。
- 依赖边界扫描不再发现子 Mod 直接访问 `RimMind.Core.Internal`、`RimMindCoreMod.Settings` 或 `AIRequestQueue.Instance`。

### 仍保留的优化项

- `ScenarioRegistry.Register` 的重复注册告警仍是 `ContainsKey` 后赋值，且 `_coreRegistered` 不是原子状态；运行期动态并发注册前应改为 CAS/实例化注册表。
- Advisor 有独立反馈会话深度 `AdvisorTaskDriver.MaxToolCallDepth = 3`；它不属于 Core Agent Loop，后续若需要统一配置应通过 Advisor 自身设置端口接入。
- `HybridAIClient` 的超时、部分失败和降级可观测策略仍可继续细化。
- 感知容量与重要性阈值已集中到 `RimMindDefaults`，但尚未暴露为用户设置；在确认玩法需求前保持为代码级策略常量。
- History 游戏内存档恢复需要 Autotester 资源补齐后做 E2E；当前只具备纯逻辑/Verse seam 契约，不能宣称游戏内验证通过。

### 死代码（r6 → r10 清理记录）

r6 审查列出的 27 项死代码已在 r7-r9 期间清理:
- ✅ IStreamingResponseHandler 整套机制 — 已移除
- ✅ IAgentModeProvider 整套机制 — 已移除
- ✅ MemoryEvent 类型 — 已移除(注: RimMind-Memory 中 "MemoryEvent" 字符串键仍在使用,作为缓存失效触发器,与类型无关)
- ✅ RequestOverlay.GetWindowRect/SetWindowRect — 已移除
- ✅ using System.Text 未使用 — 已清理
- ✅ 15 个 RimMindAPI 无调用者方法/属性 — 已移除

r10 审查(2026-07-08)新增清理:
- ✅ Source/backup/ 14个旧文件 — 已归档至 Refs/backup/
- ✅ RimMindDefaults.MiddlewareOrder 死常量(LayerBuild/Retry/NpcChatRetry/CacheStore) — 已移除
- ✅ code_quality_report.json — 已移出 Source
- ✅ ToolRiskLevel 枚举 — 已合并入 RiskLevel(跨模组 RimMind-Actions.Tests.csproj 同步)
- ✅ Null* 类使用方式统一(全部用 .Instance,消除 new/Instance 混用)

### 历史修复（r5-r9）

- ✅ RimMindAPI 静态字典 → ConcurrentDictionary
- ✅ AICoreAPI 8 个 List → ConcurrentDictionary
- ✅ ContextEngine/AIRequestQueue/ContextKeyRegistry/HistoryManager → 线程安全
- ✅ PawnAgent 硬编码参数 → 改用 Settings
- ✅ 双路径注册、Unicode 截断、ExposeData 快照 → 修复
- ✅ BudgetW1/W2 UI、autoApplyMode LogOnly → 修复
- ✅ ContextDiff lifetime、AIDebugLog O(1)、HistoryManager/SensorManager 线程安全 → 修复

### r10 审查修复（2026-07-08）

- ✅ EmbedCache 从 Domain/ValueObjects 迁移到 Infrastructure/Cache(分层违规修复)
- ✅ AgentBusImpl.EventTypeMap → ConcurrentDictionary(线程安全)
- ✅ ProviderRegistry → ConcurrentDictionary(线程安全)
- ✅ OutputGuardrailMiddleware 硬编码 Order/OwnerModId → 改用常量
- ✅ MiddlewareBase<TContext> 抽象基类(消除 18 个 Middleware 重复属性样板)
- ✅ ConcurrentRegistryBase<TKey,TValue> 泛型基类(消除 4 个 Registry CRUD 样板)
- ✅ Result<TValue,TError> 实现 IEquatable + == / != 操作符

## 操作边界

### ✅ 必须做
- 修改 `RimMindAPI` 后检查所有子模组调用方
- 修改 `ContextEngine`/`ContextKeyRegistry` 后验证已注册Provider兼容性
- 修改序列化字段后保持旧 Scribe key 向后兼容
- AI请求参数用 `AICoreSettings` 值，禁止硬编码

### ⚠️ 先询问
- 修改 `RimMindAPI` 静态字典线程模型
- 修改 `ContextLayer` 枚举层级(影响所有子模组上下文注入顺序)
- 修改 `ScenarioIds`(影响所有子模组上下文过滤)

### 🚫 绝对禁止
- 子模组访问 `RimMind.Core.Internal` 命名空间
- 子模组直接访问 `RimMindCoreMod.Settings`(用 `RimMindAPI.GetContextBudget()`)
- 后台线程调用任何RimWorld/Unity API
- 修改 `Newtonsoft.Json` 版本(RimWorld内置)
