using System;
using System.IO;
using System.Collections.Concurrent;

namespace SIFTGuardian.Services;

public class ReportService
{
    private readonly string _reportsDirectory;
    private readonly ConcurrentDictionary<Guid, string> _inMemoryReports = new();

    public ReportService()
    {
        string baseDir = AppContext.BaseDirectory;
        string projectReportsPath = Path.Combine(baseDir, "..", "..", "..", "Reports");
        
        if (Directory.Exists(Path.Combine(baseDir, "..", "..", "..")) && !baseDir.Contains(".nuget"))
        {
            _reportsDirectory = Path.GetFullPath(projectReportsPath);
        }
        else
        {
            _reportsDirectory = Path.Combine(baseDir, "Reports");
        }

        try
        {
            Directory.CreateDirectory(_reportsDirectory);
        }
        catch
        {
            _reportsDirectory = Path.Combine(Path.GetTempPath(), "SIFTGuardianReports");
            Directory.CreateDirectory(_reportsDirectory);
        }
    }

    public void SaveReport(Guid id, string reportMarkdown)
    {
        _inMemoryReports[id] = reportMarkdown;

        try
        {
            string reportFile = Path.Combine(_reportsDirectory, $"report-{id}.md");
            File.WriteAllText(reportFile, reportMarkdown);
        }
        catch
        {
            // Fail silently
        }
    }

    public string GetReport(Guid id)
    {
        if (_inMemoryReports.TryGetValue(id, out string? report))
        {
            return report;
        }

        try
        {
            string reportFile = Path.Combine(_reportsDirectory, $"report-{id}.md");
            if (File.Exists(reportFile))
            {
                string content = File.ReadAllText(reportFile);
                _inMemoryReports[id] = content;
                return content;
            }
        }
        catch
        {
            // Fail silently
        }

        return string.Empty;
    }
}
