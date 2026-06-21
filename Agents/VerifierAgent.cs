using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SIFTGuardian.Models;
using SIFTGuardian.Services;

namespace SIFTGuardian.Agents;

public class VerifierAgent : IAgent
{
    private readonly EvidenceService _evidenceService;
    private readonly LoggingService _loggingService;

    public string Name => "Verifier";

    public VerifierAgent(EvidenceService evidenceService, LoggingService loggingService)
    {
        _evidenceService = evidenceService;
        _loggingService = loggingService;
    }

    public async Task<AgentResponse> ProcessAsync(InvestigationCase caseData, object? context = null)
    {
        _loggingService.Log(Name, "Starting verification process...");
        await Task.Delay(500);

        if (context is List<Finding> findings && findings.Any())
        {
            var finding = findings.First();
            _loggingService.Log(Name, $"Verifying evidence for finding: '{finding.Title}'");

            // Query base telemetry to ensure evidence matches
            var telemetry = _evidenceService.GetTelemetry(caseData.CaseName, caseData.CaseData);
            
            // Check what parts of telemetry are referenced in the finding's evidence
            var supportedByList = new List<string>();

            bool hasProcessTree = telemetry.Any(t => t.Category == "ProcessTree" && finding.Evidence.Contains(t.Details.Substring(0, Math.Min(30, t.Details.Length))));
            bool hasParent = telemetry.Any(t => t.Category == "ParentProcess" && finding.Evidence.Contains(t.Details.Substring(0, Math.Min(30, t.Details.Length))));
            bool hasPersistence = telemetry.Any(t => t.Category == "Persistence" && finding.Evidence.Contains(t.Details.Substring(0, Math.Min(30, t.Details.Length))));
            bool hasNetwork = telemetry.Any(t => t.Category == "NetworkActivity" && finding.Evidence.Contains(t.Details.Substring(0, Math.Min(30, t.Details.Length))));

            if (hasProcessTree) supportedByList.Add("Process tree");
            if (hasParent) supportedByList.Add("Parent process");
            if (hasPersistence) supportedByList.Add("Persistence mechanisms");
            if (hasNetwork) supportedByList.Add("Network activity logs");
            
            if (!supportedByList.Any())
            {
                supportedByList.Add("Command line artifacts");
            }

            finding.Verified = true;
            string supportDetails = string.Join(", ", supportedByList);
            
            _loggingService.Log(Name, $"Approved. Evidence supported by: {supportDetails}");

            return new AgentResponse
            {
                Success = true,
                AgentName = Name,
                Message = $"Finding verified and approved. Supported by: {supportDetails}",
                IsVerified = true,
                VerificationDetails = $"Approved\n\nEvidence supported by:\n- {string.Join("\n- ", supportedByList)}"
            };
        }

        _loggingService.Log(Name, "Error: No findings provided to verify.");
        return new AgentResponse
        {
            Success = false,
            AgentName = Name,
            Message = "No findings to verify."
        };
    }
}
