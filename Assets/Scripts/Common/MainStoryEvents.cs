using UnityEngine;

/// <summary>
/// 【主线剧情事件定义】
/// 使用 [TriggerEventInfo] 特性在 NekoGraph 外部定义事件喵~
/// 事件名支持中文，策划可以直接使用喵~
/// </summary>
[TriggerEventInfo(
    "主线推进 A", 
    EventProtocol.None, 
    "📖 主线推进 A", 
    "主线剧情",
    Tooltip = "主线剧情推进到阶段 A 喵~"
)]
public static class MainStoryEvents_A
{
    public static void Handle(object payload)
    {
        Debug.LogFormat(LogType.Log, LogOption.NoStacktrace, null, "[主线剧情] 主线推进到阶段 A 喵~！");
    }
}

[TriggerEventInfo(
    "主线推进 B", 
    EventProtocol.None, 
    "📖 主线推进 B", 
    "主线剧情",
    Tooltip = "主线剧情推进到阶段 B 喵~"
)]
public static class MainStoryEvents_B
{
    public static void Handle(object payload)
    {
        Debug.LogFormat(LogType.Log, LogOption.NoStacktrace, null, "[主线剧情] 主线推进到阶段 B 喵~！");
    }
}

[TriggerEventInfo(
    "主线推进 C", 
    EventProtocol.None, 
    "📖 主线推进 C", 
    "主线剧情",
    Tooltip = "主线剧情推进到阶段 C 喵~"
)]
public static class MainStoryEvents_C
{
    public static void Handle(object payload)
    {
        Debug.LogFormat(LogType.Log, LogOption.NoStacktrace, null, "[主线剧情] 主线推进到阶段 C 喵~！");
    }
}

[TriggerEventInfo(
    "主线推进 D", 
    EventProtocol.None, 
    "📖 主线推进 D", 
    "主线剧情",
    Tooltip = "主线剧情推进到阶段 D 喵~"
)]
public static class MainStoryEvents_D
{
    public static void Handle(object payload)
    {
        Debug.LogFormat(LogType.Log, LogOption.NoStacktrace, null, "[主线剧情] 主线推进到阶段 D 喵~！");
    }
}

[TriggerEventInfo(
    "主线推进 E", 
    EventProtocol.None, 
    "📖 主线推进 E", 
    "主线剧情",
    Tooltip = "主线剧情推进到阶段 E 喵~"
)]
public static class MainStoryEvents_E
{
    public static void Handle(object payload)
    {
        Debug.LogFormat(LogType.Log, LogOption.NoStacktrace, null, "[主线剧情] 主线推进到阶段 E 喵~！");
    }
}

[TriggerEventInfo(
    "主线推进 F", 
    EventProtocol.None, 
    "📖 主线推进 F", 
    "主线剧情",
    Tooltip = "主线剧情推进到阶段 F 喵~"
)]
public static class MainStoryEvents_F
{
    public static void Handle(object payload)
    {
        Debug.LogFormat(LogType.Log, LogOption.NoStacktrace, null, "[主线剧情] 主线推进到阶段 F 喵~！");
    }
}
