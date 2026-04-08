using UnityEngine;

/// <summary>
/// 【支线剧情事件定义】
/// 使用 [TriggerEventInfo] 特性在 NekoGraph 外部定义事件喵~
/// 事件名支持中文，策划可以直接使用喵~
/// </summary>
[TriggerEventInfo(
    "支线推进 A", 
    EventProtocol.None, 
    "📜 支线推进 A", 
    "支线剧情",
    Tooltip = "支线剧情推进到阶段 A 喵~"
)]
public static class SideStoryEvents_A
{
    public static void Handle(object payload)
    {
        Debug.LogFormat(LogType.Log, LogOption.NoStacktrace, null, "[支线剧情] 支线推进到阶段 A 喵~！");
    }
}

[TriggerEventInfo(
    "支线推进 B", 
    EventProtocol.None, 
    "📜 支线推进 B", 
    "支线剧情",
    Tooltip = "支线剧情推进到阶段 B 喵~"
)]
public static class SideStoryEvents_B
{
    public static void Handle(object payload)
    {
        Debug.LogFormat(LogType.Log, LogOption.NoStacktrace, null, "[支线剧情] 支线推进到阶段 B 喵~！");
    }
}

[TriggerEventInfo(
    "支线推进 C", 
    EventProtocol.None, 
    "📜 支线推进 C", 
    "支线剧情",
    Tooltip = "支线剧情推进到阶段 C 喵~"
)]
public static class SideStoryEvents_C
{
    public static void Handle(object payload)
    {
        Debug.LogFormat(LogType.Log, LogOption.NoStacktrace, null, "[支线剧情] 支线推进到阶段 C 喵~！");
    }
}

[TriggerEventInfo(
    "支线推进 D", 
    EventProtocol.None, 
    "📜 支线推进 D", 
    "支线剧情",
    Tooltip = "支线剧情推进到阶段 D 喵~"
)]
public static class SideStoryEvents_D
{
    public static void Handle(object payload)
    {
        Debug.LogFormat(LogType.Log, LogOption.NoStacktrace, null, "[支线剧情] 支线推进到阶段 D 喵~！");
    }
}

[TriggerEventInfo(
    "支线推进 E", 
    EventProtocol.None, 
    "📜 支线推进 E", 
    "支线剧情",
    Tooltip = "支线剧情推进到阶段 E 喵~"
)]
public static class SideStoryEvents_E
{
    public static void Handle(object payload)
    {
        Debug.LogFormat(LogType.Log, LogOption.NoStacktrace, null, "[支线剧情] 支线推进到阶段 E 喵~！");
    }
}

[TriggerEventInfo(
    "支线推进 F", 
    EventProtocol.None, 
    "📜 支线推进 F", 
    "支线剧情",
    Tooltip = "支线剧情推进到阶段 F 喵~"
)]
public static class SideStoryEvents_F
{
    public static void Handle(object payload)
    {
        Debug.LogFormat(LogType.Log, LogOption.NoStacktrace, null, "[支线剧情] 支线推进到阶段 F 喵~！");
    }
}
