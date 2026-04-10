# Sequence 播放器职责

## 触发条件

当任意 Pack 在 GraphRunner 内运行至 `.dialog` 后缀的 VFS 节点时触发。

## 执行流程

```
VFSNode (.dialog)
       │
       ▼
┌─────────────────┐
│  DialogVFSHandler │  ─── 解析 DialogSequenceSO
│  (Wait 模式)     │  ─── 挂起 Runner 进程
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│   DialogPlayer   │  ─── 仲裁队列（同一时间只播一个）
│   (前端自治)     │  ─── 逐条渲染 DialoguePanel / 角色 / 背景等
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│   播放完成回调    │  ─── HideAllGalPanels() 自动隐藏
│                 │  ─── continueAction 恢复 Runner 进程
└─────────────────┘
```

## 职责边界

| 层级 | 职责 | 不做什么 |
|------|------|----------|
| **VFS 层** | 识别 `.dialog`，发包，挂起/恢复 Runner | 不碰 UI |
| **DialogPlayer** | 仲裁队列，帧隔离，逐条播放 | 不引用 NekoGraph 类型 |
| **DialoguePanel** | 接收事件，自治渲染，回调通知 | 不感知图流程 |

## 关键约束

1. **仲裁机制**：`DialogPlayer` 维护 FIFO 队列，同一时间只执行一个 Sequence
2. **帧隔离**：Sequence 完成回调后延迟一帧 (`yield return null`) 再启动下一个，避免 Hide/Show 时序冲突
3. **自动回收**：播放完成后自动调用 `HideAllGalPanels()` 清理所有 GAL 面板
4. **进程恢复**：通过 `continueAction` 回调通知 VFS 层恢复 Runner 进程

## 数据流

```
DialogSequenceSO
    ├── DialogEntry        → 台词面板
    ├── AvatarEffectEntry  → 角色立绘
    ├── BackgroundEntry    → 背景切换
    ├── ScreenFlashEntry   → 屏幕闪白/黑
    ├── VoiceEffectEntry   → 语音播放
    └── CameraEffectEntry  → 镜头特效
```

所有 Entry 统一通过 `PostSystem` 事件分发，前端组件自治响应。
