# RimMind 项目优化方向与任务（2026-05-20）

基于 Phase 15 深度架构检查的发现，以下是项目后续优化方向。

---

## 优先级 P0：架构完整性

### 1. 事件总线业务逻辑接入
**现状**：6 种事件类型（Perception/Action/Decision/Goal/Lifecycle/ModeChange）全部仅有日志订阅者（AgentBusCoreSubscriber），无业务逻辑消费。事件总线处于"全量广播，零业务消费"状态。
**目标**：为关键事件添加业务逻辑订阅者，使事件驱动架构真正生效。
**任务**：
- [ ] 为 PerceptionEvent 添加上下文更新订阅者（感知数据→上下文引擎）
- [ ] 为 ActionEvent 添加行为记录订阅者（行为→飞轮参数调优）
- [ ] 为 DecisionEvent 添加决策追踪订阅者（决策→策略优化器）
- [ ] 为 GoalEvent 添加目标栈管理订阅者（目标变更→目标生成器反馈）

### 2. Pipeline 中间件注册完善
**现状**：11 个中间件类已实现但未注册到任何管道（CacheMiddleware、ClientInvokeMiddleware、RetryMiddleware 等）。
**目标**：确认每个中间件的预期用途，注册到正确管道或移除死代码。
**任务**：
- [ ] 确认 Application 层中间件（Cache/ClientInvoke/RequestSanitize/Retry）是否应由 PipelineFactory 注册
- [ ] 确认 Context 层中间件（CacheLookup/CacheStore/LayerBuild/Telemetry）是否应由 ContextBuildPipelineFactory 注册
- [ ] 确认 Npc 层中间件（NpcChatRetry/SnapshotBuild）是否应由 NpcChatPipelineFactory 注册
- [ ] 为子 Mod 提供中间件扩展 API（AddMiddleware 已存在但文档不足）

---

## 优先级 P1：扩展性改进

### 3. 扩展注册机制统一
**现状**：4 种不同的注册模式（ExtensionRegistry / 直接字典 / ServiceLocator / 独立注册表），违反一致性原则。
**目标**：所有 IExtension 子接口通过 `ExtensionRegistry<T>` 统一注册。
**任务**：
- [ ] 将 IParameterTuner 从 `_parameterTuners` 字典迁移到 `ExtensionRegistry<IParameterTuner>`
- [ ] 将 IAgentActionBridge 从直接字段+SL 迁移到 `ExtensionRegistry<IAgentActionBridge>`
- [ ] 将 ISensorProvider 从 `_sensorProviders` 字典迁移到 `ExtensionRegistry<ISensorProvider>`
- [ ] 将 IToolHandler 从 IToolRegistry 迁移到 `ExtensionRegistry<IToolHandler>`
- [ ] 更新 RimMindAPI 公共接口保持向后兼容

### 4. AIProvider 扩展性改进
**现状**：`AIProviders` 使用字符串常量，添加新 Provider 需修改 3-4 处。ProviderHelper 硬编码 `"openai"` 作为 fallback。
**目标**：新 Provider 只需实现 IAIClientFactory 并注册，无需修改 Core 代码。
**任务**：
- [ ] 移除 ProviderHelper 中的硬编码 `"openai"` fallback，改为从注册表获取第一个 Provider
- [ ] 考虑将 AIProviders 从静态常量类改为从注册表动态发现
- [ ] 添加 Provider 注册/发现文档

### 5. Agent 生命周期管理 API
**现状**：缺少 Agent 生命周期管理公开 API（RimMindAPI.Agents 不存在）。
**目标**：子 Mod 可通过 RimMindAPI.Agents 查询、管理 Agent 状态。
**任务**：
- [ ] 设计 IAgentLifecycleManager 接口
- [ ] 在 RimMindAPI 中添加 Agents 属性
- [ ] 实现 Agent 查询/激活/停用/重置 API

---

## 优先级 P2：代码质量

### 6. RimMindRuntime 构造函数重构
**现状**：RimMindRuntime 构造函数中 22 次 RimMindServiceLocator.Get 调用，职责过重。
**目标**：将服务解析分散到各子系统的 Initialize 方法中。
**任务**：
- [ ] 提取 ClientManager 初始化到独立方法
- [ ] 提取 Pipeline 构建到独立方法
- [ ] 提取 ContextEngine 初始化到独立方法
- [ ] 减少 RimMindRuntime 构造函数行数至 50 行以内

### 7. 子 Mod 公共 API 规范化
**现状**：7 个子 Mod 共 56 处引用 `RimMind.Presentation` 命名空间，直接依赖 Core 的 Presentation 层实现细节。
**目标**：子 Mod 仅通过 Application 层接口交互，不直接使用 Presentation 命名空间。
**任务**：
- [ ] 将子 Mod 常用的 Presentation 类型（如 IPawnAgent、PawnAgent）的接口提取到 Application 层
- [ ] 逐步迁移子 Mod using 引用从 Presentation → Application
- [ ] 添加 ArchTest 验证子 Mod 不引用 Presentation 命名空间

### 8. CompPawnAgent 结构性例外消除
**现状**：CompPawnAgent（Infrastructure）依赖 IPawnAgentFactory/IPawnAgent（Presentation），是唯一的 Infrastructure→Presentation 依赖。
**目标**：消除此结构性例外。
**方案**：
- 方案 A：将 CompPawnAgent 移至 Presentation/Agent/ 目录（更符合其职责）
- 方案 B：将 IPawnAgentFactory 的非 Verse 依赖部分提取到 Application 层接口
**任务**：
- [ ] 评估方案 A/B 的可行性
- [ ] 实施选定方案

---

## 优先级 P3：测试与文档

### 9. 集成测试扩展
**现状**：Integration.Tests 仅 5 个测试，覆盖面不足。
**目标**：覆盖关键业务流程。
**任务**：
- [ ] 添加 Pipeline 端到端测试
- [ ] 添加 AgentBus 发布/订阅测试
- [ ] 添加 ContextEngine 构建上下文测试
- [ ] 添加 ClientManager 客户端创建测试

### 10. backup/ 目录清理
**现状**：backup/ 目录包含 32 个 .cs 文件，部分已确认完全无用。
**目标**：确认后删除确认无用的 backup 文件。
**任务**：
- [ ] 逐一审查 backup/ 中的文件
- [ ] 确认无跨项目引用后删除
- [ ] 更新文档记录已删除的文件

---

## 架构迁移完成度评估

| 检查项 | 状态 | 说明 |
|--------|------|------|
| Domain 零 Verse 依赖 | ✅ 完成 | |
| Application 零 Verse 依赖 | ✅ 完成 | |
| Infrastructure→Presentation 反向依赖 | ✅ 基本完成 | CompPawnAgent 1 处结构性例外 |
| 命名空间迁移 | ✅ 完成 | 活跃代码零旧命名空间残留 |
| ServiceLocator 消除 | ✅ 完成 | 消费者代码 0 处 SL.Get，仅 Composition Root 保留 |
| 死代码清理 | ✅ 完成 | 4 个死接口 + ISensorProvider 已移至 backup/ |
| 子 Mod 兼容性 | ✅ 完成 | 8 个子 Mod 全部可构建 |
| 事件总线业务逻辑 | ❌ 未完成 | 6 种事件仅有日志订阅者 |
| Pipeline 中间件注册 | ❌ 未完成 | 11 个中间件未注册 |
| 扩展注册机制统一 | ❌ 未完成 | 4 种不同注册模式 |
| 子 Mod API 规范化 | ❌ 未完成 | 56 处 Presentation 引用 |
