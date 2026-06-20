using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SIFTGuardian.Models;
using SIFTGuardian.Services;

namespace SIFTGuardian.Agents;

public class InvestigatorAgent : IAgent
{
    private readonly EvidenceService _evidenceService;
    private readonly LoggingService _loggingService;

    public string Name => "Investigator";

    public InvestigatorAgent(EvidenceService evidenceService, LoggingService loggingService)
    {
        _evidenceService = evidenceService;
        _loggingService = loggingService;
    }

    public async Task<AgentResponse> ProcessAsync(InvestigationCase caseData, object? context = null)
    {
        _loggingService.Log(Name, $"Starting analysis on case: '{caseData.CaseName}'");
        
        // Simulate thinking delay
        await Task.Delay(500);

        if (context is List<string> requestedGaps && requestedGaps.Any())
        {
            _loggingService.Log(Name, $"Reanalysis triggered. Addressing requested gaps: {string.Join(", ", requestedGaps)}");
            
            // Retrieve full telemetry from EvidenceService
            var telemetry = _evidenceService.GetTelemetry(caseData.CaseName, caseData.CaseData);
            
            var detailedEvidenceList = new List<string> { $"Initial Case Data: {caseData.CaseData}" };
            
            foreach (var gap in requestedGaps)
            {
                string category = MapGapToTelemetryCategory(gap);
                var match = telemetry.FirstOrDefault(t => t.Category.Equals(category, StringComparison.OrdinalIgnoreCase));
                if (match != null)
                {
                    detailedEvidenceList.Add($"{gap} Identified: {match.Details}");
                    _loggingService.Log(Name, $"Retrieved forensic evidence for '{gap}': {match.Details}");
                }
                else
                {
                    detailedEvidenceList.Add($"{gap}: No telemetry found");
                }
            }

            double updatedConfidence = 90.0;
            string updatedTitle = $"Confirmed: {DetermineThreatTitle(caseData.CaseData)}";
            
            var finalFinding = new Finding
            {
                Title = updatedTitle,
                Evidence = string.Join("\n- ", detailedEvidenceList),
                ConfidenceScore = updatedConfidence,
                Verified = false
            };

            _loggingService.Log(Name, $"Reanalysis complete. Updated confidence to {updatedConfidence}%. Finding: {updatedTitle}");

            return new AgentResponse
            {
                Success = true,
                AgentName = Name,
                Message = "Reanalysis successfully gathered supplementary evidence.",
                Findings = new List<Finding> { finalFinding }
            };
        }
        else
        {
            // Initial analysis
            string threatTitle = DetermineThreatTitle(caseData.CaseData);
            double initialConfidence = 70.0;
            
            var initialFinding = new Finding
            {
                Title = threatTitle,
                Evidence = $"Observed payload: {caseData.CaseData}",
                ConfidenceScore = initialConfidence,
                Verified = false
            };

            _loggingService.Log(Name, $"Initial analysis complete. Identified threat: '{threatTitle}' with confidence {initialConfidence}%.");
            
            return new AgentResponse
            {
                Success = true,
                AgentName = Name,
                Message = "Initial analysis completed based on case data.",
                Findings = new List<Finding> { initialFinding }
            };
        }
    }

    private string DetermineThreatTitle(string data)
    {
        if (data.Contains("powershell", StringComparison.OrdinalIgnoreCase) || data.Contains("-enc", StringComparison.OrdinalIgnoreCase))
        {
            return "Suspicious PowerShell Execution";
        }
        if (data.Contains("word", StringComparison.OrdinalIgnoreCase) || data.Contains("certutil", StringComparison.OrdinalIgnoreCase))
        {
            return "Malicious Word Document (Phishing)";
        }
        if (data.Contains("w3wp", StringComparison.OrdinalIgnoreCase) || data.Contains("shell.aspx", StringComparison.OrdinalIgnoreCase))
        {
            return "IIS Web Shell Injection";
        }
        return "Suspicious Command Line Artifacts";
    }

    private string MapGapToTelemetryCategory(string gap)
    {
        if (gap.Contains("parent", StringComparison.OrdinalIgnoreCase)) return "ParentProcess";
        if (gap.Contains("persistence", StringComparison.OrdinalIgnoreCase)) return "Persistence";
        if (gap.Contains("network", StringComparison.OrdinalIgnoreCase)) return "NetworkActivity";
        if (gap.Contains("tree", StringComparison.OrdinalIgnoreCase)) return "ProcessTree";
        return string.Empty;
    }
}
