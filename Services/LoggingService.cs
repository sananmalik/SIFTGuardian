using System;
using System.Collections.Generic;
using System.IO;
using SIFTGuardian.Models;

namespace SIFTGuardian.Services;

public class LoggingService
{
    private readonly List<AgentLog> _inMemoryLogs = new();
    private readonly string _logsDirectory;

    public LoggingService()
    {
        // Place Logs folder in the project root if running from bin, or current dir
        string baseDir = AppContext.BaseDirectory;
        string projectLogsPath = Path.Combine(baseDir, "..", "..", "..", "Logs");
        
        if (Directory.Exists(Path.Combine(baseDir, "..", "..", "..")) && !baseDir.Contains(".nuget"))
        {
            _logsDirectory = Path.GetFullPath(projectLogsPath);
        }
        else
        {
            _logsDirectory = Path.Combine(baseDir, "Logs");
        }

        try
        {
            Directory.CreateDirectory(_logsDirectory);
        }
        catch
        {
            _logsDirectory = Path.Combine(Path.GetTempPath(), "SIFTGuardianLogs");
            Directory.CreateDirectory(_logsDirectory);
        }
    }

    public void Log(string agentName, string message)
    {
        var log = new AgentLog
        {
            AgentName = agentName,
            Message = message,
            Timestamp = DateTime.UtcNow
        };

        lock (_inMemoryLogs)
        {
            _inMemoryLogs.Add(log);
        }

        WriteToFile(log);
    }

    public List<AgentLog> GetLogs()
    {
        lock (_inMemoryLogs)
        {
            return new List<AgentLog>(_inMemoryLogs);
        }
    }

    public void ClearLogs()
    {
        lock (_inMemoryLogs)
        {
            _inMemoryLogs.Clear();
        }
    }

    private void WriteToFile(AgentLog log)
    {
        try
        {
            string dateStr = DateTime.UtcNow.ToString("yyyy-MM-dd");
            string logFile = Path.Combine(_logsDirectory, $"sift-guardian-{dateStr}.log");
            string logLine = $"[{log.Timestamp:yyyy-MM-dd HH:mm:ss}] [{log.AgentName}] {log.Message}{Environment.NewLine}";
            File.AppendAllText(logFile, logLine);
        }
        catch
        {
            // Fail silently to avoid breaking execution if directory is locked
        }
    }
}
