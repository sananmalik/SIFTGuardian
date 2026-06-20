using System;

namespace SIFTGuardian.Models;

public class InvestigationCase
{
    public Guid Id { get; set; }
    public string CaseName { get; set; } = string.Empty;
    public string CaseData { get; set; } = string.Empty;
}
