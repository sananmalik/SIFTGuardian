using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SIFTGuardian.Models;
using SIFTGuardian.Services;

namespace SIFTGuardian.Agents;

public class SkepticAgent : IAgent
{
    private readonly LoggingService _loggingService;

    public string Name => "Skeptic";

    public SkepticAgent(LoggingService loggingService)
    {
        _loggingService = loggingService;
    }

    public async Task<AgentResponse> ProcessAsync(InvestigationCase caseData, object? context = null)
    {
        _loggingService.Log(Name, "Reviewing findings from Investigator...");
        await Task.Delay(500);

        if (context is List<Finding> findings && findings.Any())
        {
            var finding = findings.First();
            _loggingService.Log(Name, $"Reviewing finding '{finding.Title}' with confidence: {finding.ConfidenceScore}%");

            if (finding.ConfidenceScore < 80.0)
            {
                var missingItems = new List<string>
                {
                    "Parent process",
                    "Persistence evidence",
                    "Network activity"
                };

                _loggingService.Log(Name, $"Finding confidence too low ({finding.ConfidenceScore}%). Missing: {string.Join(", ", missingItems)}");
                
                return new AgentResponse
                {
                    Success = true,
                    AgentName = Name,
                    Message = "Evidence is insufficient. Requesting additional investigation.",
                    ReanalysisRequired = true,
                    MissingEvidence = missingItems
                };
            }
            else
            {
                _loggingService.Log(Name, $"Findings review complete. Confidence score ({finding.ConfidenceScore}%) meets the required threshold. Moving to verification.");
                return new AgentResponse
                {
                    Success = true,
                    AgentName = Name,
                    Message = "Findings accepted by Skeptic. Ready for verification.",
                    ReanalysisRequired = false
                };
            }
        }

        _loggingService.Log(Name, "Error: No findings provided to review.");
        return new AgentResponse
        {
            Success = false,
            AgentName = Name,
            Message = "No findings to review."
        };
    }
}
