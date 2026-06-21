// Templates Data
const templates = {
    powershell: {
        name: "Suspicious PowerShell Execution",
        data: "powershell.exe -enc SQB4AGUA..."
    },
    word: {
        name: "Phishing Attachment Execution",
        data: "WINWORD.EXE executing command line certutil to download payload.exe"
    },
    w3wp: {
        name: "IIS Web Shell Backdoor",
        data: "w3wp.exe spawning cmd.exe to write ASPX backdoor shell.aspx"
    }
};

let pollingInterval = null;
let displayedLogCount = 0;
let currentReportId = null;
let currentReportMarkdown = "";

function loadTemplate(key) {
    const template = templates[key];
    if (template) {
        document.getElementById('caseName').value = template.name;
        document.getElementById('caseData').value = template.data;
        addSystemLog(`Loaded template: ${template.name}`);
    }
}

function addSystemLog(message) {
    const consoleLogs = document.getElementById('consoleLogs');
    const line = document.createElement('div');
    line.className = 'log-line system';
    line.textContent = `[SYSTEM] ${message}`;
    consoleLogs.appendChild(line);
    consoleLogs.scrollTop = consoleLogs.scrollHeight;
}

async function runInvestigation(event) {
    event.preventDefault();
    
    const caseName = document.getElementById('caseName').value;
    const caseData = document.getElementById('caseData').value;
    const deployBtn = document.getElementById('deployBtn');

    // Reset UI State
    deployBtn.disabled = true;
    document.getElementById('consoleLogs').innerHTML = "";
    displayedLogCount = 0;
    currentReportId = null;
    currentReportMarkdown = "";
    document.getElementById('downloadBtn').disabled = true;
    
    // Clear agent nodes and flow
    resetAgentNodes();
    setSystemStatus('active', 'Deploying SIFT Guardian Agents...');
    addSystemLog(`Contacting API to run: "${caseName}"`);

    try {
        const response = await fetch('/api/investigation/run', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ caseName, caseData })
        });

        if (!response.ok) {
            throw new Error(`API returned ${response.status}`);
        }

        const data = await response.json();
        addSystemLog(`Orchestration job created. Case ID: ${data.caseId}`);
        
        // Start polling logs
        startPolling();

    } catch (error) {
        addSystemLog(`Error launching investigation: ${error.message}`);
        setSystemStatus('idle', 'System Idle');
        deployBtn.disabled = false;
    }
}

function startPolling() {
    if (pollingInterval) clearInterval(pollingInterval);
    
    pollingInterval = setInterval(pollLogs, 400);
}

async function pollLogs() {
    try {
        const response = await fetch('/api/investigation/logs');
        if (!response.ok) return;

        const data = await response.json();
        const logs = data.logs || [];
        const status = data.status;
        const caseId = data.caseId;

        // Render new logs
        if (logs.length > displayedLogCount) {
            const container = document.getElementById('consoleLogs');
            for (let i = displayedLogCount; i < logs.length; i++) {
                const log = logs[i];
                const line = document.createElement('div');
                
                // Color code line based on agent
                const agent = log.agentName.toLowerCase();
                line.className = `log-line ${agent}`;
                line.textContent = `[${log.agentName.toUpperCase()}] ${log.message}`;
                container.appendChild(line);
            }
            displayedLogCount = logs.length;
            container.scrollTop = container.scrollHeight;
        }

        // Update active node highlighting based on logs/status
        updateVisualFlow(logs, status);

        // Update overall system status text
        if (status === 'InProgress') {
            setSystemStatus('active', 'Multi-Agent Investigation in Progress...');
        } else if (status === 'Self-Correcting') {
            setSystemStatus('correcting', 'Skeptic Flagged Gaps! Self-Correcting Loop Active...');
        } else if (status === 'Completed') {
            clearInterval(pollingInterval);
            setSystemStatus('success', 'Investigation Completed Successfully');
            document.getElementById('deployBtn').disabled = false;
            
            // Retrieve and display report
            currentReportId = caseId;
            fetchReport(caseId);
        } else if (status === 'Failed') {
            clearInterval(pollingInterval);
            setSystemStatus('idle', 'Investigation Workflow Failed');
            document.getElementById('deployBtn').disabled = false;
        }

    } catch (error) {
        console.error("Error polling logs:", error);
    }
}

function updateVisualFlow(logs, status) {
    resetAgentNodes();
    
    if (logs.length === 0) return;
    
    const lastLog = logs[logs.length - 1];
    const activeAgent = lastLog.agentName;

    // Highlight active agent node
    const node = document.getElementById(`node-${activeAgent}`);
    if (node) {
        node.classList.add('active');
        node.querySelector('.node-status').textContent = 'ACTIVE';
    }

    // Connectors state
    const conn1 = document.getElementById('conn-1');
    const conn2 = document.getElementById('conn-2');
    const conn3 = document.getElementById('conn-3');
    const connLoop = document.getElementById('conn-loop');

    if (status === 'Self-Correcting') {
        connLoop.classList.add('active-flow');
    }

    // Determine path highlights based on who is active
    if (activeAgent === 'Investigator') {
        // Investigator active
        conn1.classList.remove('active-flow');
        conn2.classList.remove('active-flow');
        conn3.classList.remove('active-flow');
    } else if (activeAgent === 'Skeptic') {
        conn1.classList.add('active-flow');
        conn2.classList.remove('active-flow');
    } else if (activeAgent === 'Verifier') {
        conn1.classList.add('active-flow');
        conn2.classList.add('active-flow');
        conn3.classList.remove('active-flow');
    } else if (activeAgent === 'Reporter') {
        conn1.classList.add('active-flow');
        conn2.classList.add('active-flow');
        conn3.classList.add('active-flow');
    }
}

function resetAgentNodes() {
    const agents = ['Investigator', 'Skeptic', 'Verifier', 'Reporter'];
    agents.forEach(agent => {
        const node = document.getElementById(`node-${agent}`);
        if (node) {
            node.classList.remove('active');
            node.querySelector('.node-status').textContent = 'STDBY';
        }
    });

    document.getElementById('conn-1').classList.remove('active-flow');
    document.getElementById('conn-2').classList.remove('active-flow');
    document.getElementById('conn-3').classList.remove('active-flow');
    document.getElementById('conn-loop').classList.remove('active-flow');
}

function setSystemStatus(type, text) {
    const dot = document.getElementById('statusDot');
    const label = document.getElementById('statusText');
    
    dot.className = 'status-dot';
    label.textContent = text;

    if (type === 'active') {
        dot.classList.add('pulse-active');
    } else if (type === 'correcting') {
        dot.classList.add('pulse-correcting');
    } else if (type === 'success') {
        dot.classList.add('pulse-success');
    } else {
        dot.classList.add('pulse-idle');
    }
}

async function fetchReport(caseId) {
    try {
        const response = await fetch(`/api/investigation/report/${caseId}`);
        if (!response.ok) throw new Error("Report not generated yet.");

        const data = await response.json();
        currentReportMarkdown = data.report;
        renderReport(data.report);
        
        document.getElementById('downloadBtn').disabled = false;

    } catch (error) {
        document.getElementById('reportContent').innerHTML = `
            <div class="report-placeholder">
                <div class="placeholder-icon">❌</div>
                <p>Failed to retrieve incident report: ${error.message}</p>
            </div>
        `;
    }
}

// Simple client-side Markdown rendering to keep layout visually stunning
function renderReport(markdown) {
    const lines = markdown.split('\n');
    let html = '<div class="report-markdown">';
    let inList = false;
    let inBlockquote = false;

    for (let line of lines) {
        // Handle lists
        if (line.trim().startsWith('- ')) {
            if (!inList) {
                html += '<ul>';
                inList = true;
            }
            let listContent = line.trim().substring(2);
            listContent = parseInlineMarkdown(listContent);
            html += `<li>${listContent}</li>`;
            continue;
        } else if (inList) {
            html += '</ul>';
            inList = false;
        }

        // Handle blockquotes
        if (line.trim().startsWith('>')) {
            if (!inBlockquote) {
                html += '<blockquote>';
                inBlockquote = true;
            }
            let quoteContent = line.trim().substring(1).trim();
            quoteContent = parseInlineMarkdown(quoteContent);
            html += `<p>${quoteContent}</p>`;
            continue;
        } else if (inBlockquote) {
            html += '</blockquote>';
            inBlockquote = false;
        }

        // Handle headers
        if (line.startsWith('# ')) {
            html += `<h1>${parseInlineMarkdown(line.substring(2))}</h1>`;
        } else if (line.startsWith('## ')) {
            html += `<h2>${parseInlineMarkdown(line.substring(3))}</h2>`;
        } else if (line.startsWith('### ')) {
            html += `<h3>${parseInlineMarkdown(line.substring(4))}</h3>`;
        } else if (line.trim() === '---') {
            html += '<hr>';
        } else if (line.trim() === '') {
            html += '<br>';
        } else {
            html += `<p>${parseInlineMarkdown(line)}</p>`;
        }
    }

    // Close any dangling tags
    if (inList) html += '</ul>';
    if (inBlockquote) html += '</blockquote>';

    html += '</div>';
    document.getElementById('reportContent').innerHTML = html;
}

function parseInlineMarkdown(text) {
    // Bold
    text = text.replace(/\*\*(.*?)\*\*/g, '<strong>$1</strong>');
    // Inline code
    text = text.replace(/`(.*?)`/g, '<code>$1</code>');
    return text;
}

function downloadReport() {
    if (!currentReportMarkdown) return;
    
    const blob = new Blob([currentReportMarkdown], { type: 'text/markdown' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = `sift-guardian-report-${currentReportId.substring(0, 8)}.md`;
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
    URL.revokeObjectURL(url);
}

// Start Clock Ticker
function updateClock() {
    const clock = document.getElementById('utcClock');
    if (clock) {
        const now = new Date();
        const timeStr = 'UTC ' + now.toISOString().replace('T', ' ').substring(11, 19);
        clock.textContent = timeStr;
    }
}
setInterval(updateClock, 1000);
updateClock();
