using System.Linq;
using UnityEngine;

public static class DebugCommands
{
    [CommandInfo("debug_log", "Debug Log", "Debug", new[] { "message" })]
    public static CommandOutput DebugLog(IConsoleController console, int subjectLevel, string[] args, object payload)
    {
        var message = args != null && args.Length > 0
            ? string.Join(" ", args.Where(arg => !string.IsNullOrWhiteSpace(arg)))
            : "(empty)";

        if (console is GraphCommandConsoleContext graphConsole)
        {
            Debug.LogFormat(
                LogType.Log,
                LogOption.NoStacktrace,
                null,
                "[debug_log] {0} path={1} payload={2} message={3}",
                graphConsole.BuildDebugPrefix(),
                graphConsole.SignalPath,
                GraphCommandConsoleContext.SummarizeValue(payload),
                message);
        }
        else
        {
            Debug.LogFormat(
                LogType.Log,
                LogOption.NoStacktrace,
                null,
                "[debug_log] subject={0} payload={1} message={2}",
                subjectLevel,
                GraphCommandConsoleContext.SummarizeValue(payload),
                message);
        }

        return CommandOutput.Success($"debug_log: {message}", payload);
    }
}
