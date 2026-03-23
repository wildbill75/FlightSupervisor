document.addEventListener('DOMContentLoaded', () => {

    // Top Bar Dragging Interop
    const topBar = document.getElementById('top-bar');
    topBar.addEventListener('mousedown', (e) => {
        if (e.target.closest('.window-controls')) return;
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

    // Load airports data
    window.airportsDb = {};
    fetch('airports.json').then(r => r.json()).then(d => { window.airportsDb = d; }).catch(e => console.warn('No airports.json found.'));

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

    const btnStartGroundOps = document.getElementById('btnStartGroundOps');
    if (btnStartGroundOps) {
        btnStartGroundOps.addEventListener('click', () => {
            window.chrome.webview.postMessage({ action: 'startGroundOps' });
            btnStartGroundOps.style.display = 'none';
            document.querySelector('.menu li[data-target="groundops"]').click();
        });
    }

    // Connect to Simulator
    let isSimConnected = false;
    const btnSmartConnect = document.getElementById('btnSmartConnect');
    if (btnSmartConnect) {
        btnSmartConnect.addEventListener('click', () => {
            window.chrome.webview.postMessage({ action: isSimConnected ? 'disconnectSim' : 'connectSim' });
            if (!isSimConnected) {
                btnSmartConnect.querySelector('span').innerText = 'Connecting...';
                btnSmartConnect.style.color = '#FACC15';
            }
        });
    }

    const btnMin = document.getElementById('btnMin');
    const btnClose = document.getElementById('btnClose');
    if (btnMin) btnMin.addEventListener('click', () => window.chrome.webview.postMessage({ action: 'minimizeApp' }));
    if (btnClose) btnClose.addEventListener('click', () => window.chrome.webview.postMessage({ action: 'closeApp' }));

    let currentSobtUnix = 0;
    let isFlightCancelled = false;

    // WebView2 Global Message Receiver
    window.chrome.webview.addEventListener('message', event => {
        const payload = event.data;
        if (!payload || !payload.type) return;

        switch (payload.type) {
            case 'savedUsername':
                document.getElementById('sbUsername').value = payload.username;
                break;
            case 'simConnectStatus':
                isSimConnected = payload.status.includes('Connected') || payload.status.includes('Linked');
                const smartBtn = document.getElementById('btnSmartConnect');
                
                if (isSimConnected) {
                    if (smartBtn) {
                        smartBtn.querySelector('span').innerText = payload.status.includes('Linked') ? 'Linked to SunRise' : 'Connected';
                        smartBtn.className = 'smart-connect success';
                        smartBtn.style.color = '';
                    }
                } else {
                    if (smartBtn) {
                        smartBtn.querySelector('span').innerText = 'Not Connected';
                        smartBtn.className = 'smart-connect error';
                        smartBtn.style.color = '';
                    }
                }
                break;
            case 'telemetry':
                document.getElementById('flightPhase').innerText = `${payload.phase}`;
                break;
            case 'simTime':
                document.getElementById('zuluTime').innerText = `${payload.time}`;
                const cd = document.getElementById('flightCountdown');
                if (cd && payload.rawUnix && currentSobtUnix > 0) {
                    const getPunc = (planned, actual) => {
                        let d = actual - planned;
                        if (d < -300) return { t: 'Early', c: '#60A5FA', d: d };
                        if (d <= 300) return { t: 'On Time', c: '#34D399', d: d };
                        if (d <= 900) return { t: 'Moderate Late', c: '#F59E0B', d: d };
                        return { t: 'Super Late', c: '#EF4444', d: d };
                    };
                    const fmt = (diffSec) => {
                        let a = Math.abs(diffSec);
                        let h = Math.floor(a / 3600);
                        let m = Math.floor((a % 3600) / 60);
                        return (h > 0 ? `${h}h ` : '') + `${m}m`;
                    };
                    
                    if (window.finalAibtUnix && window.currentSibtUnix > 0) {
                        let st = getPunc(window.currentSibtUnix, window.finalAibtUnix);
                        cd.innerText = `Arrived ${st.t} (${st.d < 0 ? '-' : '+'}${fmt(st.d)})`;
                        cd.style.color = st.c;
                        let aibtSp = document.getElementById('bdAibt');
                        if (aibtSp) aibtSp.style.color = st.c;
                    } else if (window.finalAobtUnix) {
                        let st = getPunc(currentSobtUnix, window.finalAobtUnix);
                        cd.innerText = `Departed ${st.t} (${st.d < 0 ? '-' : '+'}${fmt(st.d)})`;
                        cd.style.color = st.c;
                        let aobtSp = document.getElementById('bdAobt');
                        if (aobtSp) aobtSp.style.color = st.c;
                    } else {
                        let diff = currentSobtUnix - payload.rawUnix;
                        let isLate = diff < 0;
                        let absDiff = Math.abs(diff);
                        let h = Math.floor(absDiff / 3600);
                        let m = Math.floor((absDiff % 3600) / 60);
                        let s = absDiff % 60;
                        let timeStr = (h > 0 ? `${h}h ` : '') + `${m}m ${s}s`;
                        cd.innerText = isLate ? `Delayed by: ${timeStr}` : `SOBT in: ${timeStr}`;
                        cd.style.color = isLate ? '#F87171' : '#34D399';
                        let aobtSp = document.getElementById('bdAobt');
                        if (aobtSp) aobtSp.style.color = '#FACC15';
                    }
                }
                break;
            case 'phaseUpdate':
                document.getElementById('flightPhase').innerText = `${payload.phase}`;
                if (payload.aobt) {
                    document.getElementById('bdAobt').innerText = payload.aobt;
                    if (payload.aobtUnix) window.finalAobtUnix = payload.aobtUnix;
                }
                if (payload.aibt) {
                    document.getElementById('bdAibt').innerText = payload.aibt;
                    if (payload.aibtUnix) window.finalAibtUnix = payload.aibtUnix;
                }
                break;
            case 'penalty':
            case 'log':
                const log = document.getElementById('penaltyLogs');
                const li = document.createElement('li');
                li.innerText = `[${new Date().toLocaleTimeString()}] ${payload.message}`;
                li.style.color = payload.type === 'penalty' ? '#F87171' : '#cbd5e1';
                li.style.marginBottom = '5px';
                log.prepend(li);
                break;
            case 'scoreUpdate':
                if (isFlightCancelled) return;
                let topScore = document.getElementById('topScoreValue');
                if (topScore) topScore.innerText = payload.score;
                const mainScore = document.getElementById('mainScoreValue');
                if (mainScore) mainScore.innerText = payload.score;
                
                if (payload.delta !== 0) {
                    mainScore.style.color = payload.delta > 0 ? '#34D399' : '#F87171';
                    mainScore.style.textShadow = payload.delta > 0 ? '0 0 20px rgba(52,211,153,0.5)' : '0 0 20px rgba(248,113,113,0.5)';
                    setTimeout(() => {
                        if (isFlightCancelled) return;
                        mainScore.style.color = '#34D399';
                        mainScore.style.textShadow = '0 0 20px rgba(52,211,153,0.3)';
                    }, 1000);

                    const feed = document.getElementById('scoreFeed');
                    if (feed.children.length === 1 && feed.children[0].innerText.includes('standing by')) {
                        feed.innerHTML = '';
                    }
                    
                    const fli = document.createElement('li');
                    let deltaStr = payload.delta > 0 ? `+${payload.delta}` : `${payload.delta}`;
                    let color = payload.delta > 0 ? '#34D399' : '#F87171';
                    fli.innerHTML = `<span style="color:${color}; font-weight:bold; width: 45px; display:inline-block;">${deltaStr}</span> <span style="color:#cbd5e1;">${payload.msg}</span>`;
                    fli.style.marginBottom = '5px';
                    fli.style.borderBottom = '1px solid rgba(255,255,255,0.05)';
                    fli.style.paddingBottom = '3px';
                    feed.prepend(fli);
                    if (feed.children.length > 6) feed.removeChild(feed.lastChild);

                    const plog = document.getElementById('penaltyLogs');
                    const logLi = document.createElement('li');
                    logLi.innerText = `[${new Date().toLocaleTimeString()}] Score ${deltaStr} : ${payload.msg} (Total: ${payload.score})`;
                    logLi.style.color = payload.delta > 0 ? '#A7F3D0' : '#FCA5A5';
                    logLi.style.marginBottom = '5px';
                    plog.prepend(logLi);
                }
                break;
            case 'fetchStatus':
                if (payload.status === 'success') {
                    document.getElementById('fetchStatus').innerText = '';
                } else {
                    document.getElementById('fetchStatus').innerText = payload.message || '';
                }
                if (payload.status === 'success') {
                    isFlightCancelled = false;
                    const phaseEl = document.getElementById('flightPhase');
                    if (phaseEl) {
                        phaseEl.style.color = '#FACC15';
                        phaseEl.style.textShadow = 'none';
                    }
                    const mScore = document.getElementById('mainScoreValue');
                    if (mScore) {
                        mScore.innerText = "1000";
                        mScore.style.fontSize = "64px";
                        mScore.style.color = "#34D399";
                        mScore.style.textShadow = "0 0 20px rgba(52,211,153,0.3)";
                    }
                    const tScore = document.getElementById('topScoreValue');
                    if (tScore) tScore.innerText = "1000";
                    const pLogs = document.getElementById('penaltyLogs');
                    if (pLogs) pLogs.innerHTML = "";
                    const sFeed = document.getElementById('scoreFeed');
                    if (sFeed) sFeed.innerHTML = "<li style=\"color:#64748b; text-align:center;\">Tracking standing by...</li>";
                    // Auto switch to Briefing tab
                    document.querySelector('.menu li[data-target="briefing"]').click();
                }
                break;
            case 'groundOpsReady':
                if (btnStartGroundOps) {
                    btnStartGroundOps.disabled = false;
                    btnStartGroundOps.style.backgroundColor = '#24A148';
                    btnStartGroundOps.style.color = '#FFFFFF';
                    btnStartGroundOps.style.cursor = 'pointer';
                }
                break;
            case 'flightCancelled':
                isFlightCancelled = true;
                const phaseEl = document.getElementById('flightPhase');
                if (phaseEl) {
                    phaseEl.innerText = "Aborted";
                    phaseEl.style.color = "#DC2626";
                    phaseEl.style.textShadow = "0 0 20px rgba(220, 38, 38, 0.8)";
                }
                const mScore = document.getElementById('mainScoreValue');
                if (mScore) {
                    mScore.innerText = "FLIGHT CANCELLED";
                    mScore.style.fontSize = "38px";
                    mScore.style.color = "#DC2626";
                    mScore.style.textShadow = "0 0 20px rgba(220, 38, 38, 0.8)";
                } 
                if (btnStartGroundOps) {
                    btnStartGroundOps.disabled = true;
                    btnStartGroundOps.style.backgroundColor = '#334155';
                    btnStartGroundOps.style.color = '#64748b';
                    btnStartGroundOps.style.cursor = 'not-allowed';
                }
                break;
            case 'flightData':
                const d = payload.data;
                
                document.getElementById('flightBreakdown').style.display = 'grid';
                document.getElementById('weatherBreakdown').style.display = 'block';

                const timeStr = (unix) => {
                    if (!unix || unix == "0") return "--:--z";
                    const dt = new Date(unix * 1000);
                    return dt.toISOString().substr(11,5) + "z";
                };

                const AIRLINES = {
                    'AFR': 'Air France', 'BAW': 'British Airways', 'EZY': 'easyJet', 'RYR': 'Ryanair', 
                    'DLH': 'Lufthansa', 'UAE': 'Emirates', 'QTR': 'Qatar Airways', 'DAL': 'Delta',
                    'AAL': 'American Airlines', 'UAL': 'United', 'SWA': 'Southwest'
                };

                if (d.general) {
                    let acode = d.general.icao_airline || '';
                    let aname = AIRLINES[acode] ? ` (${AIRLINES[acode]})` : '';
                    document.getElementById('bdAirline').innerText = acode + aname;
                    document.getElementById('bdFlightNum').innerText = d.general.flight_number || '';
                    let cruiseStr = 'FL ' + (d.general.initial_alt || '');
                    if (d.general.stepclimb_string) cruiseStr += ' / STEP ' + d.general.stepclimb_string;
                    document.getElementById('bdCruise').innerText = cruiseStr;
                    document.getElementById('bdRoute').innerText = d.general.route || '';
                    document.getElementById('bdRoute').title = d.general.route || '';

                    // Dashboard Header Update
                    const dashFlightHeader = document.getElementById('dashFlightHeader');
                    if (dashFlightHeader) dashFlightHeader.style.display = 'block';
                    const dhOrigin = document.getElementById('dhOrigin');
                    const dhDest = document.getElementById('dhDest');
                    const dhFlight = document.getElementById('dhFlight');
                    const dhAirline = document.getElementById('dhAirline');

                    const toTitleCase = (str) => str ? str.toLowerCase().replace(/(?:^|[\s-])\w/g, m => m.toUpperCase()) : '';

                    const getAirportStr = (icao, sbCity, sbName) => {
                        let city = sbCity;
                        let name = sbName;
                        if (window.airportsDb && window.airportsDb[icao]) {
                            city = window.airportsDb[icao].city || city;
                            name = window.airportsDb[icao].name || name;
                        }
                        
                        if (city && city.includes('/')) city = city.split('/')[0];
                        
                        city = toTitleCase(city);
                        name = toTitleCase(name);
                        
                        if (name && name.endsWith(' Airport')) name = name.replace(' Airport', '');
                        if (name && name.endsWith(' Intl')) name = name.replace(' Intl', '');
                        if (name && name.endsWith(' International')) name = name.replace(' International', '');
                        if (name && name.startsWith('Airport ')) name = name.substring(8);
                        
                        if (city && name && city !== name && !name.includes(city)) {
                            return `${city}-${name}`;
                        }
                        return name || city || icao;
                    };

                    let origIcao = (d.origin && d.origin.icao_code) ? d.origin.icao_code : '----';
                    let destIcao = (d.destination && d.destination.icao_code) ? d.destination.icao_code : '----';
                    
                    let origStr = getAirportStr(origIcao, d.origin?.city, d.origin?.name);
                    let destStr = getAirportStr(destIcao, d.destination?.city, d.destination?.name);

                    if (dhOrigin) dhOrigin.innerHTML = `${origIcao} <span style="font-size: 15px; color: #94A3B8; font-weight: 600;">(${origStr})</span>`;
                    if (dhDest) dhDest.innerHTML = `${destIcao} <span style="font-size: 15px; color: #94A3B8; font-weight: 600;">(${destStr})</span>`;

                    let flightNum = d.general.flight_number || '';
                    if (dhFlight) dhFlight.innerText = flightNum ? `Flight ${acode}${flightNum}` : 'Flight ---';
                    
                    let pureAirlineName = AIRLINES[acode] ? AIRLINES[acode] : (d.general.airline_name || acode || 'Unknown');
                    if (dhAirline) dhAirline.innerText = pureAirlineName;
                }
                if (d.aircraft) {
                    document.getElementById('bdAircraft').innerText = (d.aircraft.name || '') + ' (' + (d.aircraft.base_type || '') + ')';
                }
                if (d.origin) document.getElementById('bdOrigin').innerText = (d.origin.icao_code || '') + ' ' + (d.origin.name || '');
                if (d.destination) document.getElementById('bdDest').innerText = (d.destination.icao_code || '') + ' ' + (d.destination.name || '');

                if (d.times) {
                    currentSobtUnix = parseInt(d.times.sched_out || '0');
                    const sobtH = new Date(currentSobtUnix * 1000).getUTCHours().toString().padStart(2, '0');
                    const sobtM = new Date(currentSobtUnix * 1000).getUTCMinutes().toString().padStart(2, '0');
                    document.getElementById('bdSobt').innerText = `${sobtH}:${sobtM}z`;
                    window.currentSibtUnix = parseInt(d.times.sched_in || '0');
                    const sibtH = new Date(window.currentSibtUnix * 1000).getUTCHours().toString().padStart(2, '0');
                    const sibtM = new Date(window.currentSibtUnix * 1000).getUTCMinutes().toString().padStart(2, '0');
                    document.getElementById('bdSibt').innerText = `${sibtH}:${sibtM}z`;
                    let eteSec = parseInt(d.times.est_time_enroute || '0');
                    let h = Math.floor(eteSec / 3600);
                    let m = Math.floor((eteSec % 3600) / 60);
                    document.getElementById('bdEte').innerText = `${h.toString().padStart(2, '0')}:${m.toString().padStart(2, '0')}`;
                }

                if (d.weights && d.params) {
                    document.getElementById('bdPax').innerText = d.weights.pax_count || '';
                    const units = d.params.units || 'LBS';
                    document.getElementById('bdZfw').innerText = (d.weights.est_zfw || '') + ' ' + units;
                    document.getElementById('bdLdw').innerText = (d.weights.est_ldw || '') + ' ' + units;
                    let fuel = d.fuel?.plan_ramp || d.weights.est_block || d.weights.block_fuel || '';
                    document.getElementById('bdFuel').innerText = fuel + ' ' + units;
                }

                document.getElementById('briefingContent').innerText = payload.briefing;

                if (d.weather) {
                    let wTxt = `Origin METAR: ${d.weather.orig_metar || ''}\nOrigin TAF: ${d.weather.orig_taf || ''}\n\n`;
                    wTxt += `Dest METAR: ${d.weather.dest_metar || ''}\nDest TAF: ${d.weather.dest_taf || ''}\n\n`;
                    
                    const formatArr = (arr) => Array.isArray(arr) ? arr.join('\n') : (arr || '');
                    if (d.weather.altn_metar) wTxt += `Altn METAR(s):\n${formatArr(d.weather.altn_metar)}\n\n`;
                    if (d.weather.enrt_metar) wTxt += `Enroute METAR(s):\n${formatArr(d.weather.enrt_metar)}\n\n`;
                    
                    document.getElementById('weatherContent').innerText = wTxt.trim();
                }
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

        let timeDisplay = '';
        if (s.RemainingSec > 0) {
            const m = Math.floor(s.RemainingSec / 60).toString().padStart(2, '0');
            const sec = (s.RemainingSec % 60).toString().padStart(2, '0');
            timeDisplay = `(-${m}:${sec})`;
        }

        html += `
            <div class="go-service">
                <div class="go-name">${s.Name}</div>
                <div class="go-bar-bg">
                    <div class="go-bar-fill" style="width: ${s.ProgressPercent}%;"></div>
                </div>
                <div class="go-status" style="color: ${statusColor};">
                    ${s.StatusMessage} ${timeDisplay}
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
