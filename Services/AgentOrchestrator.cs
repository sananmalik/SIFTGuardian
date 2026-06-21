using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SIFTGuardian.Agents;
using SIFTGuardian.Models;

namespace SIFTGuardian.Services;

public class AgentOrchestrator
{
    private readonly InvestigatorAgent _investigator;
    private readonly SkepticAgent _skeptic;
    private readonly VerifierAgent _verifier;
    private readonly ReportAgent _reporter;
    private readonly LoggingService _loggingService;
    private readonly ReportService _reportService;

    private InvestigationResult? _lastResult;

    public AgentOrchestrator(
        InvestigatorAgent investigator,
        SkepticAgent skeptic,
        VerifierAgent verifier,
        ReportAgent reporter,
        LoggingService loggingService,
        ReportService reportService)
    {
        _investigator = investigator;
        _skeptic = skeptic;
        _verifier = verifier;
        _reporter = reporter;
        _loggingService = loggingService;
        _reportService = reportService;
    }

    public async Task<InvestigationResult> RunInvestigationAsync(InvestigationCase investigationCase)
    {
        _loggingService.ClearLogs();
        _loggingService.Log("System", $"Starting self-correcting incident response orchestration for Case ID: {investigationCase.Id}");

        var result = new InvestigationResult
        {
            Id = investigationCase.Id,
            Case = investigationCase,
            Status = "InProgress",
            CreatedAt = DateTime.UtcNow
        };

        try
        {
            // 1. Investigator (Initial Run)
            _loggingService.Log("System", "Invoking Investigator Agent (Initial Analysis)...");
            var investigatorRes = await _investigator.ProcessAsync(investigationCase);
            var currentFindings = investigatorRes.Findings ?? new List<Finding>();

            // 2. Skeptic (First Review)
            _loggingService.Log("System", "Invoking Skeptic Agent (Reviewing initial findings)...");
            var skepticRes = await _skeptic.ProcessAsync(investigationCase, currentFindings);

            // 3. Self-Correction Loop check
            if (skepticRes.ReanalysisRequired && skepticRes.MissingEvidence != null)
            {
                result.Status = "Self-Correcting";
                _loggingService.Log("System", "Skeptic requested self-correction loop. Routing back to Investigator.");

                // Investigator (Re-run with skeptic's feedback context)
                _loggingService.Log("System", "Invoking Investigator Agent for Reanalysis...");
                var reanalysisRes = await _investigator.ProcessAsync(investigationCase, skepticRes.MissingEvidence);
                currentFindings = reanalysisRes.Findings ?? currentFindings;

                // Skeptic (Second Review - should approve now)
                _loggingService.Log("System", "Invoking Skeptic Agent for Final Review...");
                await _skeptic.ProcessAsync(investigationCase, currentFindings);
            }

            // 4. Verifier
            _loggingService.Log("System", "Invoking Verifier Agent...");
            var verifierRes = await _verifier.ProcessAsync(investigationCase, currentFindings);
            string verificationDetails = verifierRes.VerificationDetails;

            // 5. Reporter
            _loggingService.Log("System", "Invoking Reporter Agent...");
            var reportContext = Tuple.Create(currentFindings, verificationDetails);
            var reporterRes = await _reporter.ProcessAsync(investigationCase, reportContext);
            string reportMarkdown = reporterRes.VerificationDetails;

            // Complete Result mapping
            result.Findings = currentFindings;
            result.ReportMarkdown = reportMarkdown;
            result.OverallConfidence = currentFindings.Any() ? currentFindings.Average(f => f.ConfidenceScore) : 0.0;
            result.Status = "Completed";
            result.CompletedAt = DateTime.UtcNow;
            result.Logs = _loggingService.GetLogs();

            // Save report via ReportService
            _reportService.SaveReport(result.Id, result.ReportMarkdown);
            _loggingService.Log("System", $"Orchestration completed successfully. Final Report ID: {result.Id}");
        }
        catch (Exception ex)
        {
            _loggingService.Log("System", $"Critical failure in orchestration workflow: {ex.Message}");
            result.Status = "Failed";
            result.CompletedAt = DateTime.UtcNow;
            result.Logs = _loggingService.GetLogs();
        }

        _lastResult = result;
        return result;
    }

    public InvestigationResult? GetLastResult()
    {
        if (_lastResult != null)
        {
            _lastResult.Logs = _loggingService.GetLogs();
        }
        return _lastResult;
    }
}
