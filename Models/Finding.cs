namespace SIFTGuardian.Models;

public class Finding
{
    public string Title { get; set; } = string.Empty;
    public string Evidence { get; set; } = string.Empty;
    public double ConfidenceScore { get; set; }
    public bool Verified { get; set; }
}
