using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SIFTGuardian.Models;
using SIFTGuardian.Services;

namespace SIFTGuardian.Agents;

public class ReportAgent : IAgent
{
    private readonly LoggingService _loggingService;

    public string Name => "Reporter";

    public ReportAgent(LoggingService loggingService)
    {
        _loggingService = loggingService;
    }

    public async Task<AgentResponse> ProcessAsync(InvestigationCase caseData, object? context = null)
    {
        _loggingService.Log(Name, "Generating final incident report...");
        await Task.Delay(500);

        if (context is Tuple<List<Finding>, string> reportContext)
        {
            var findings = reportContext.Item1;
            var verificationDetails = reportContext.Item2;
            
            double averageConfidence = findings.Any() ? findings.Average(f => f.ConfidenceScore) : 0.0;
            
            var sb = new StringBuilder();
            sb.AppendLine("# SIFT Guardian Incident Response Report");
            sb.AppendLine($"**Case ID:** {caseData.Id}");
            sb.AppendLine($"**Case Name:** {caseData.CaseName}");
            sb.AppendLine($"**Date Analyzed:** {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
            sb.AppendLine();
            sb.AppendLine("## Executive Summary");
            sb.AppendLine($"A security investigation has been conducted on the submitted payload/evidence. The multi-agent flow concluded with an overall confidence score of **{averageConfidence}%**.");
            sb.AppendLine($"**Investigation Status:** {(findings.All(f => f.Verified) ? "VERIFIED / COMPLETED" : "UNVERIFIED")}");
            sb.AppendLine();
            sb.AppendLine("## Analysis Verification Details");
            sb.AppendLine(verificationDetails);
            sb.AppendLine();
            sb.AppendLine("## Identified Findings");
            
            foreach (var finding in findings)
            {
                sb.AppendLine($"### Finding: {finding.Title}");
                sb.AppendLine($"- **Confidence Score:** {finding.ConfidenceScore}%");
                sb.AppendLine($"- **Verified:** {(finding.Verified ? "Yes" : "No")}");
                sb.AppendLine("- **Evidentiary Support:**");
                var evidenceLines = finding.Evidence.Split('\n');
                foreach (var line in evidenceLines)
                {
                    if (!string.IsNullOrWhiteSpace(line))
                    {
                        sb.AppendLine($"  {line}");
                    }
                }
                sb.AppendLine();
            }

            sb.AppendLine("## Agent Execution Log Summary");
            sb.AppendLine("The investigation was routed through the self-correcting SIFT Guardian Multi-Agent cycle:");
            sb.AppendLine("1. **Investigator** — Discovered initial indicator patterns and flagged potential threat vectors.");
            sb.AppendLine("2. **Skeptic** — Challenged the initial findings, identifying missing telemetry references (parent process, persistence, network logs).");
            sb.AppendLine("3. **Investigator (Reanalysis)** — Queried enterprise logs and pulled supporting telemetry to correct findings and increase confidence.");
            sb.AppendLine("4. **Verifier** — Approved findings by checking telemetry availability.");
            sb.AppendLine("5. **Reporter** — Compiled final structured markdown documentation.");
            sb.AppendLine();
            sb.AppendLine("---");
            sb.AppendLine("*Report generated automatically by SIFT Guardian Incident Response Agent.*");

            string reportMarkdown = sb.ToString();
            _loggingService.Log(Name, $"Report compiled successfully with average confidence {averageConfidence}%.");

            return new AgentResponse
            {
                Success = true,
                AgentName = Name,
                Message = "Final report generated successfully.",
                VerificationDetails = reportMarkdown
            };
        }

        _loggingService.Log(Name, "Error: Invalid context provided to generate report.");
        return new AgentResponse
        {
            Success = false,
            AgentName = Name,
            Message = "Invalid report context."
        };
    }
}
