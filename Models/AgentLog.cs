using System;

namespace SIFTGuardian.Models;

public class AgentLog
{
    public string AgentName { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
