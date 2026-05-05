using KaitoKid.Utilities.Interfaces;

namespace GraveyardKeepelago.Tests.TestData;

public class FakeLogger : ILogger
{
    public List<string> InfoLogs { get; } = new();
    public List<string> ErrorLogs { get; } = new();
    public List<string> DebugLogs { get; } = new();

    public void LogDebug(object message) => DebugLogs.Add(message?.ToString() ?? "");
    public void LogError(object message) => ErrorLogs.Add(message?.ToString() ?? "");
    public void LogFatal(object message) { }
    public void LogInfo(object message) => InfoLogs.Add(message?.ToString() ?? "");
    public void LogMessage(object message, object level) { }
    public void LogWarning(object message) { }
    public void LogException(Exception e, object level) { }

    // Additional ILogger interface methods from KaitoKid.Utilities.Interfaces
    public void LogError(string message) => ErrorLogs.Add(message ?? "");
    public void LogError(string message, Exception ex) => ErrorLogs.Add(message ?? "");
    public void LogWarning(string message) { }
    public void LogInfo(string message) => InfoLogs.Add(message ?? "");
    public void LogMessage(string message) { }
    public void LogDebug(string message) => DebugLogs.Add(message ?? "");
    public void LogDebugPatchIsRunning(string pluginName, string methodName, string sourceFile, string sourceLineNumber, params object[] args) { }
    public void LogDebug(string message, params object[] args) => DebugLogs.Add(message ?? "");
    public void LogErrorException(string message, Exception ex, params object[] args) => ErrorLogs.Add(message ?? "");
    public void LogWarningException(string message, Exception ex, params object[] args) { }
    public void LogErrorException(Exception ex, params object[] args) { }
    public void LogWarningException(Exception ex, params object[] args) { }
    public void LogErrorMessage(string message, params object[] args) => ErrorLogs.Add(message ?? "");
    public void LogErrorException(string message, string source, Exception ex, params object[] args) => ErrorLogs.Add(message ?? "");
}
