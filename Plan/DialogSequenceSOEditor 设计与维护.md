# DialogSequenceSOEditor 设计与维护

## 1. 目标

`DialogSequenceSOEditor` 解决的是一个很具体的问题：

- `DialogSequenceSO` 既要承载对白，又要承载演出条目
- 对白主体来自 CSV，常常会重建
- 演出条目通常是人工配置，不能因为重建 CSV 就丢掉
- Unity 的 `[SerializeReference]` 对类型结构变更很敏感，编辑器辅助信息不能随便塞回运行时条目

当前实现的设计原则是：

- `DialogSequenceSO.Entries` 只保存运行时真正需要的数据
- 演出保留所需的“锚点信息”放到 editor-only 侧链资产
- CSV 重建时优先按语义锚点回挂，匹配失败再退回桶逻辑

## 2. 相关文件

- [`DialogSequenceSOEditor.cs`](G:\ProjectOfGame\GAL01\Assets\Scripts\Dialog\Editor\DialogSequenceSOEditor.cs)
- [`DialogSequenceAnchorMeta.cs`](G:\ProjectOfGame\GAL01\Assets\Scripts\Dialog\Editor\DialogSequenceAnchorMeta.cs)
- [`DialogSequenceSO.cs`](G:\ProjectOfGame\GAL01\Assets\Scripts\Dialog\Data\DialogSequenceSO.cs)
- [`ISequenceEntry.cs`](G:\ProjectOfGame\GAL01\Assets\Scripts\Dialog\Data\ISequenceEntry.cs)
- [`DialogEffectEntries.cs`](G:\ProjectOfGame\GAL01\Assets\Scripts\Dialog\Data\DialogEffectEntries.cs)

## 3. 数据分层

### 3.1 主数据

`DialogSequenceSO.Entries` 是运行时主数据，结构为：

- `DialogEntry`
- `AvatarEffectEntry`
- `VoiceEffectEntry`
- `CameraEffectEntry`
- `ScreenFlashEntry`

这里的条目会直接参与 Unity 序列化和运行时加载，所以这里不要塞编辑器辅助字段。

### 3.2 侧链数据

锚点信息保存在 `DialogSequenceAnchorMeta` 中，资产路径位于：

- `Assets/Scripts/Dialog/Editor/AnchorMeta/{DialogSequenceSO 的 guid}.asset`

每条演出会对应一条 `DialogSequenceEffectAnchorRecord`，记录：

- `EffectType`
- `EffectFingerprint`
- `FingerprintOccurrence`
- `AnchorSpeaker`
- `AnchorContentPreview`
- `AnchorSignature`
- `AnchorOccurrence`
- `FallbackBucket`
- `OriginalOrder`

这些字段只服务编辑器重建和排查，不参与运行时播放。

## 4. 为什么不用把锚点塞进 EffectEntry

之前已经踩过一次坑。

`DialogSequenceSO.Entries` 使用的是 `[SerializeReference]`。这意味着：

- 条目具体类型名是序列化身份的一部分
- 条目字段布局变化会影响老资产和已加载对象
- 给 `EffectEntry` 增加辅助字段，容易引发 Unity 的 serialization layout 报错

所以当前规则是：

- 运行时条目结构尽量稳定
- 编辑器语义信息走 sidecar

这条不要轻易破。

## 5. MetaLib 关联方式

每个 `DialogSequenceSO` 会在 editor 中注册一个稳定 id：

- `dialog-sequence:{assetGuid}`

注册逻辑在 `EnsureSequenceMetaId()`。

它的作用是：

- 给主资产一个稳定身份
- 让后续系统可以通过 MetaLib 找到该序列
- 同时给 sidecar 资产记录 `SequenceMetaId`

注意：

- sidecar 当前的直接落盘路径仍然是按 `DialogSequenceSO` 的 asset guid 定位
- `MetaLib ID` 是稳定身份，不是 sidecar 文件名

## 6. Inspector 功能组成

### 6.1 Source 区

显示并编辑：

- `CsvSource`
- `SequenceId`
- `Description`
- 当前 `MetaLib ID`

### 6.2 Build 区

按钮：

- `从 CSV 构建序列`

行为：

1. 解析 CSV 为新的 `DialogEntry` 列表
2. 先保存当前 sidecar 锚点
3. 重建对白主链
4. 尝试把旧演出条目回挂到新对白之间

### 6.3 Toolbar 区

支持：

- 合并连续对白显示
- 紧凑显示特效
- 刷新
- 新增条目
- 上移 / 下移 / 删除

### 6.4 Search 区

支持搜索：

- 对话 `ID / Speaker / Content`
- 特效自身字段
- 当前推导出的锚点文本

### 6.5 Details 区

选中条目后可编辑详细字段。

对于 `EffectEntry`，详情区还会显示当前推导出的锚点信息，便于排查“这条演出现在附着在哪句对白后面”。

## 7. CSV 重建的核心逻辑

实现入口：

- `BuildFromCsv()`

它分两层保留逻辑。

### 7.1 第一层：锚点匹配

先用 sidecar 中的锚点记录尝试回挂。

对白签名生成规则：

- `Normalize(speaker) + "|" + Normalize(content)`

规范化规则在 `NormalizeText()`：

- `Trim`
- 合并连续空白
- 转小写

如果同一句对白重复出现多次，用 `AnchorOccurrence` 区分“第几次出现”。

### 7.2 第二层：桶逻辑兜底

如果锚点匹配失败，则退回 `FallbackBucket`。

桶的定义是：

- `0` 表示第一条对白之前
- `1` 表示第 1 条对白之后
- `2` 表示第 2 条对白之后

这保证了在文本大改、锚点失效时，演出至少仍能回到一个大致正确的位置。

## 8. EffectFingerprint 的作用

sidecar 不直接保存“指向某个 EffectEntry 实例的引用”，而是用指纹识别演出。

当前规则：

- `AvatarEffectEntry`: `characterId + avatar guid + fade`
- `VoiceEffectEntry`: `targetDialogId + clip guid`
- `CameraEffectEntry`: `profile guid`
- `ScreenFlashEntry`: `flashType + duration`

如果同指纹特效出现多次，再用 `FingerprintOccurrence` 区分顺序。

这套方案不追求绝对唯一，但足够应对当前编辑器重建场景。

## 9. 维护红线

### 9.1 不要随便改 Data 目录下已序列化类型的命名空间

特别是：

- `DialogEntry`
- `EffectEntry` 及其子类
- `DialogSequenceSO`

这些类型已经进资产序列化，尤其 `Entries` 还是 `[SerializeReference]`。

直接改 namespace 或类名，会让旧资产类型识别失效。要做必须带迁移方案。

### 9.2 不要把 editor-only 锚点信息重新塞回 EffectEntry

这是之前 Unity serialization layout 问题的根源之一。除非你愿意重新承担一轮资产迁移和兼容成本，否则不要回头。

### 9.3 修改 BuildFromCsv 时，优先保护“旧演出对象复用”

当前对白重建时会尽量复用旧的 `DialogEntry`：

- 如果新 CSV 的 `Id` 和旧对白一致，就复用旧对象，只更新 `Speaker/Content`

这样做的价值是：

- 减少引用漂移
- 降低重建时的对象抖动

不要轻易改成“每次全 new”。

### 9.4 修改搜索或列表显示时，不要破坏 `_selectedIndex` 对真实 Entries 的对应关系

当前 UI 折叠和过滤只是显示层行为，真实编辑仍基于 `_target.Entries` 的真实索引。

如果把显示索引和真实索引混了，删除、移动、编辑会改错条目。

## 10. 常见问题

### 10.1 为什么会出现空对白或空特效

这通常不是运行时生成的，而是资产里已经有历史脏数据。

排查顺序：

1. 打开 `.asset` 看 `Entries`
2. 检查是否手工新增过空条目
3. 对该序列重新执行一次“从 CSV 构建序列”

### 10.2 为什么搜索结果和折叠显示不一致

搜索开启时，编辑器会优先保证定位准确，而不是保持折叠视觉效果。因为搜索结果必须映射到真实条目，否则选中和编辑会错。

### 10.3 为什么 sidecar 会跟着改

因为 `MarkDirty()` 会在这些操作后自动重建锚点并保存 sidecar：

- 编辑字段
- 移动条目
- 删除条目
- 新增条目
- CSV 重建

这是预期行为，不是额外脏写。

## 11. 后续扩展建议

如果继续做这一块，优先级建议如下：

1. 在 editor 中增加“清理空对白/空特效”按钮
2. 给搜索结果增加关键字高亮
3. 补一层“锚点命中状态”可视化
4. 真正实现 `DialogPlayer` 的异步接管，让 `.dialog` handler 正式跑通 `HandleResult.Nope`

## 12. 一句话总结

`DialogSequenceSOEditor` 的核心不是“画个 inspector”，而是：

- 让对白可以从 CSV 重建
- 让演出不因重建丢失
- 同时避免把编辑器语义数据污染进运行时序列化主链

后续维护时，只要一直守住这三个目标，方向就不会跑偏。
