document.addEventListener('DOMContentLoaded', () => {

    // Top Bar Dragging Interop
    const topBar = document.getElementById('top-bar');
    topBar.addEventListener('mousedown', (e) => {
        // Left click only
        if (e.button === 0) {
            window.chrome.webview.postMessage({ action: 'drag' });
        }
    });

    // Menu Tab Navigation
    const menuItems = document.querySelectorAll('.menu li');
    const sections = document.querySelectorAll('section');

    menuItems.forEach(item => {
        item.addEventListener('click', () => {
            // Update Active Menu
            menuItems.forEach(m => m.classList.remove('active'));
            item.classList.add('active');

            // Update Active Section
            const targetId = item.getAttribute('data-target');
            sections.forEach(sec => {
                if (sec.id === targetId) sec.classList.add('active');
                else sec.classList.remove('active');
            });
        });
    });

    // Fetch Flight Plan
    const btnFetchPlan = document.getElementById('btnFetchPlan');
    btnFetchPlan.addEventListener('click', () => {
        const username = document.getElementById('sbUsername').value;
        const remember = document.getElementById('sbRemember').checked;
        
        if (username.trim() === '') {
            document.getElementById('fetchStatus').innerText = 'Username required.';
            return;
        }
        
        window.chrome.webview.postMessage({
            action: 'fetch',
            username: username,
            remember: remember
        });
    });

    // WebView2 Global Message Receiver
    window.chrome.webview.addEventListener('message', event => {
        const payload = event.data;
        if (!payload || !payload.type) return;

        switch (payload.type) {
            case 'savedUsername':
                document.getElementById('sbUsername').value = payload.username;
                break;
            case 'simConnectStatus':
                const sc = document.getElementById('simConnectStatus');
                sc.innerText = payload.status;
                if (payload.status.includes('Connected') || payload.status.includes('Linked')) {
                    sc.className = 'status-pill success';
                } else {
                    sc.className = 'status-pill error';
                }
                break;
            case 'simTime':
                document.getElementById('zuluTime').innerText = `Sim Zulu: ${payload.time}`;
                break;
            case 'phaseUpdate':
                document.getElementById('flightPhase').innerText = `Phase: ${payload.phase}`;
                break;
            case 'penalty':
                const log = document.getElementById('penaltyLogs');
                const li = document.createElement('li');
                li.innerText = `[${new Date().toLocaleTimeString()}] ${payload.message}`;
                li.style.color = '#F87171';
                li.style.marginBottom = '5px';
                log.prepend(li);
                break;
            case 'fetchStatus':
                document.getElementById('fetchStatus').innerText = payload.message;
                if (payload.status === 'success') {
                    // Auto switch to Briefing tab
                    document.querySelector('.menu li[data-target="briefing"]').click();
                }
                break;
            case 'flightData':
                // Format Briefing text
                document.getElementById('briefingContent').innerText = payload.briefing;
                break;
            case 'groundOps':
                renderGroundOps(payload.services);
                break;
            case 'groundOpsComplete':
                document.getElementById('groundOpsContainer').innerHTML = '<p style="color:#34D399; font-weight:bold;">All ground operations are complete. Aircraft is secure.</p>';
                break;
        }
    });

});

function renderGroundOps(services) {
    const container = document.getElementById('groundOpsContainer');
    let html = '';
    
    services.forEach(s => {
        let btnHtml = '';
        if (s.IsOptional && s.State !== 3 /* Completed */ && s.State !== 4 /* Skipped */) {
            btnHtml = `<button class="go-btn" onclick="skipService('${s.Name}')">Skip</button>`;
        }
        
        let statusColor = '#94A3B8';
        if (s.State === 2 /* Delayed */) statusColor = '#FACC15';
        if (s.State === 3 /* Completed */) statusColor = '#34D399';
        if (s.State === 4 /* Skipped */) statusColor = '#F87171';

        html += `
            <div class="go-service">
                <div class="go-name">${s.Name}</div>
                <div class="go-bar-bg">
                    <div class="go-bar-fill" style="width: ${s.ProgressPercent}%;"></div>
                </div>
                <div class="go-status" style="color: ${statusColor};">
                    ${s.StatusMessage} (-${s.RemainingSec}s)
                </div>
                ${btnHtml}
            </div>
        `;
    });
    
    container.innerHTML = html;
}

// Global skip function for inline onclick
window.skipService = function(name) {
    window.chrome.webview.postMessage({ action: 'skipService', service: name });
};
