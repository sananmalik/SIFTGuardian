using System.Collections.Generic;

namespace SIFTGuardian.Models;

public class AgentResponse
{
    public bool Success { get; set; }
    public string AgentName { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    
    // Investigator response contains a list of Findings
    public List<Finding>? Findings { get; set; }
    
    // Skeptic response contains feedback details
    public bool ReanalysisRequired { get; set; }
    public List<string>? MissingEvidence { get; set; }
    
    // Verifier response contains verification status for findings
    public bool IsVerified { get; set; }
    public string VerificationDetails { get; set; } = string.Empty;
}
