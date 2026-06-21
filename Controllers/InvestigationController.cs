using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SIFTGuardian.Models;
using SIFTGuardian.Services;

namespace SIFTGuardian.Controllers;

[ApiController]
[Route("api/investigation")]
public class InvestigationController : ControllerBase
{
    private readonly AgentOrchestrator _orchestrator;
    private readonly LoggingService _loggingService;
    private readonly ReportService _reportService;

    public InvestigationController(
        AgentOrchestrator orchestrator,
        LoggingService loggingService,
        ReportService reportService)
    {
        _orchestrator = orchestrator;
        _loggingService = loggingService;
        _reportService = reportService;
    }

    [HttpPost("run")]
    public IActionResult Run([FromBody] RunInvestigationRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.CaseName) || string.IsNullOrWhiteSpace(request.CaseData))
        {
            return BadRequest("CaseName and CaseData are required.");
        }

        var investigationCase = new InvestigationCase
        {
            Id = Guid.NewGuid(),
            CaseName = request.CaseName,
            CaseData = request.CaseData
        };

        // Run in background so logs can be polled in real time
        _ = Task.Run(async () =>
        {
            try
            {
                await _orchestrator.RunInvestigationAsync(investigationCase);
            }
            catch (Exception ex)
            {
                _loggingService.Log("System", $"Background job error: {ex.Message}");
            }
        });

        return Accepted(new { caseId = investigationCase.Id, message = "Investigation started successfully." });
    }

    [HttpGet("logs")]
    public IActionResult GetLogs()
    {
        var logs = _loggingService.GetLogs();
        var lastResult = _orchestrator.GetLastResult();
        
        return Ok(new
        {
            logs,
            status = lastResult?.Status ?? "None",
            caseId = lastResult?.Id ?? Guid.Empty,
            overallConfidence = lastResult?.OverallConfidence ?? 0.0
        });
    }

    [HttpGet("report/{id}")]
    public IActionResult GetReport(Guid id)
    {
        string report = _reportService.GetReport(id);
        if (string.IsNullOrEmpty(report))
        {
            return NotFound(new { message = $"Report for ID {id} not found or is still generating." });
        }

        return Ok(new { report });
    }
}

public class RunInvestigationRequest
{
    public string CaseName { get; set; } = string.Empty;
    public string CaseData { get; set; } = string.Empty;
}
