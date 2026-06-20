using System;
using System.Collections.Generic;

namespace SIFTGuardian.Models;

public class InvestigationResult
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public InvestigationCase Case { get; set; } = new();
    public List<Finding> Findings { get; set; } = new();
    public List<AgentLog> Logs { get; set; } = new();
    public string ReportMarkdown { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending"; // e.g., Pending, InProgress, Correcting, Completed, Failed
    public double OverallConfidence { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime CompletedAt { get; set; }
}
