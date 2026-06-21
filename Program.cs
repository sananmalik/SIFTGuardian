using SIFTGuardian.Agents;
using SIFTGuardian.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Register Custom Incident Response Services
builder.Services.AddSingleton<EvidenceService>();
builder.Services.AddSingleton<LoggingService>();
builder.Services.AddSingleton<ReportService>();

// Register Agents
builder.Services.AddTransient<InvestigatorAgent>();
builder.Services.AddTransient<SkepticAgent>();
builder.Services.AddTransient<VerifierAgent>();
builder.Services.AddTransient<ReportAgent>();

// Register Orchestrator
builder.Services.AddSingleton<AgentOrchestrator>();

var app = builder.Build();

// Enable serving default files (index.html) and static files from wwwroot
app.UseDefaultFiles();
app.UseStaticFiles();

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
