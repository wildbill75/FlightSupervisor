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

    // Restore Settings
    const savedSpeed = localStorage.getItem('groundSpeed');
    if (savedSpeed && document.getElementById('selGroundOpsSpeed')) {
        document.getElementById('selGroundOpsSpeed').value = savedSpeed;
    }
    
    const savedWeather = localStorage.getItem('weatherSource');
    if (savedWeather && document.getElementById('selWeatherSource')) {
        document.getElementById('selWeatherSource').value = savedWeather;
    }
    
    const savedGsx = localStorage.getItem('gsxSync');
    if (savedGsx !== null && document.getElementById('chkGsxSync')) {
        document.getElementById('chkGsxSync').checked = (savedGsx === 'true');
    }
    
    // UI Options Load
    const selItems = ['selLanguage', 'selTimeFormat', 'selUnitSpeed', 'selUnitAlt', 'selUnitWeight', 'selUnitTemp', 'selUnitPress'];
    selItems.forEach(id => {
        const val = localStorage.getItem(id);
        const el = document.getElementById(id);
        if (val && el) el.value = val;
    });

    const savedHardcore = localStorage.getItem('chkHardcore');
    if (savedHardcore !== null && document.getElementById('chkHardcore')) {
        document.getElementById('chkHardcore').checked = (savedHardcore === 'true');
    }
    
    const rngProb = document.getElementById('rngProb');
    const lblProb = document.getElementById('lblProb');
    if (rngProb && lblProb) {
        rngProb.addEventListener('input', () => { lblProb.innerText = rngProb.value + '%'; });
        const savedProb = localStorage.getItem('groundProb');
        if (savedProb) {
            rngProb.value = savedProb;
            lblProb.innerText = savedProb + '%';
        }
    }

    // Reset Settings
    const btnResetGroundOps = document.getElementById('btnResetGroundOps');
    if (btnResetGroundOps) {
        btnResetGroundOps.addEventListener('click', () => {
            const speedSel = document.getElementById('selGroundOpsSpeed');
            const probRng = document.getElementById('rngProb');
            const probLbl = document.getElementById('lblProb');
            
            if (speedSel) speedSel.value = 'Realistic';
            if (probRng) probRng.value = 25;
            if (probLbl) probLbl.innerText = '25%';
            
            localStorage.setItem('groundSpeed', 'Realistic');
            localStorage.setItem('groundProb', '25');
        });
    }

    // Save Settings
    const btnSaveSettings = document.getElementById('btnSaveSettings');
    if (btnSaveSettings) {
        btnSaveSettings.addEventListener('click', () => {
            const username = document.getElementById('sbUsername') ? document.getElementById('sbUsername').value : '';
            const groundSpeed = document.getElementById('selGroundOpsSpeed') ? document.getElementById('selGroundOpsSpeed').value : 'Realistic';
            const groundProb = document.getElementById('rngProb') ? document.getElementById('rngProb').value : '25';
            const weatherSrc = document.getElementById('selWeatherSource') ? document.getElementById('selWeatherSource').value : 'SimBrief';
            const gsxSync = document.getElementById('chkGsxSync') ? document.getElementById('chkGsxSync').checked : false;
            
            const selItems = ['selLanguage', 'selTimeFormat', 'selUnitSpeed', 'selUnitAlt', 'selUnitWeight', 'selUnitTemp', 'selUnitPress'];
            selItems.forEach(id => {
                const el = document.getElementById(id);
                if (el) localStorage.setItem(id, el.value);
            });
            const hardcore = document.getElementById('chkHardcore') ? document.getElementById('chkHardcore').checked : false;
            localStorage.setItem('chkHardcore', hardcore);
            
            if (username) localStorage.setItem('sbUsername', username);
            localStorage.setItem('groundSpeed', groundSpeed);
            localStorage.setItem('groundProb', groundProb);
            localStorage.setItem('weatherSource', weatherSrc);
            localStorage.setItem('gsxSync', gsxSync);
            
            btnSaveSettings.innerText = 'Settings Saved';
            btnSaveSettings.style.backgroundColor = '#34D399';
            
            setTimeout(() => { 
                btnSaveSettings.innerText = 'Save Settings';
                btnSaveSettings.style.backgroundColor = '#4A90E2';
            }, 1500);
        });
    }

    // Fetch Flight Plan / Cancel Flight
    const btnFetchPlan = document.getElementById('btnFetchPlan');
    let isFlightActive = false;
    const cancelModal = document.getElementById('cancelModal');
    const btnCancelYes = document.getElementById('btnCancelYes');
    const btnCancelNo = document.getElementById('btnCancelNo');

    if (btnCancelNo) btnCancelNo.addEventListener('click', () => { cancelModal.style.display = 'none'; });
    if (btnCancelYes) btnCancelYes.addEventListener('click', () => {
        cancelModal.style.display = 'none';
        window.chrome.webview.postMessage({ action: 'cancelFlight' });
    });

    btnFetchPlan.addEventListener('click', () => {
        if (isFlightActive) {
            if (cancelModal) cancelModal.style.display = 'flex';
            return;
        }

        const username = document.getElementById('sbUsername').value;
        const remember = document.getElementById('sbRemember').checked;
        const groundSpeed = document.getElementById('selGroundOpsSpeed') ? document.getElementById('selGroundOpsSpeed').value : 'Realistic';
        const groundProb = rngProb ? rngProb.value : '25';
        
        if (username.trim() === '') {
            document.getElementById('fetchStatus').innerText = 'Username required.';
            return;
        }
        
        localStorage.setItem('groundSpeed', groundSpeed);
        localStorage.setItem('groundProb', groundProb);
        
        window.chrome.webview.postMessage({
            action: 'fetch',
            username: username,
            remember: remember,
            groundSpeed: groundSpeed,
            groundProb: groundProb,
            units: {
                weight: localStorage.getItem('selUnitWeight') || 'LBS',
                temp: localStorage.getItem('selUnitTemp') || 'C',
                alt: localStorage.getItem('selUnitAlt') || 'FT',
                speed: localStorage.getItem('selUnitSpeed') || 'KTS',
                press: localStorage.getItem('selUnitPress') || 'HPA',
                time: localStorage.getItem('selTimeFormat') || '24H'
            }
        });
    });

    const btnStartGroundOps = document.getElementById('btnStartGroundOps');
    if (btnStartGroundOps) {
        btnStartGroundOps.addEventListener('click', () => {
            window.chrome.webview.postMessage({ action: 'startGroundOps' });
            btnStartGroundOps.disabled = true;
            btnStartGroundOps.style.backgroundColor = '#334155';
            btnStartGroundOps.style.color = '#64748b';
            btnStartGroundOps.style.cursor = 'not-allowed';
            btnStartGroundOps.innerText = 'Ops in Progress';
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
                    let d = 0; // Delay in seconds
                    let isArrTimer = false;
                    
                    const getPunc = (planned, actual) => {
                        let diff = actual - planned;
                        if (diff < -300) return { t: 'Early', c: '#60A5FA', d: diff };
                        if (diff <= 300) return { t: 'On Time', c: '#34D399', d: diff };
                        if (diff <= 900) return { t: 'Moderate Late', c: '#F59E0B', d: diff };
                        return { t: 'Super Late', c: '#EF4444', d: diff };
                    };
                    const fmt = (diffSec) => {
                        let a = Math.abs(diffSec);
                        let h = Math.floor(a / 3600);
                        let m = Math.floor((a % 3600) / 60);
                        return (h > 0 ? `${h}h ` : '') + `${m}m`;
                    };
                    
                    if (window.finalAibtUnix && window.currentSibtUnix > 0) {
                        d = window.finalAibtUnix - window.currentSibtUnix;
                        let st = getPunc(window.currentSibtUnix, window.finalAibtUnix);
                        cd.innerText = `Arrived ${st.t} (${st.d < 0 ? '-' : '+'}${fmt(st.d)})`;
                        cd.style.color = st.c;
                        let aibtSp = document.getElementById('bdAibt');
                        if (aibtSp) aibtSp.style.color = st.c;
                        isArrTimer = true;
                    } else if (window.finalAobtUnix) {
                        d = window.finalAobtUnix - currentSobtUnix;
                        let st = getPunc(currentSobtUnix, window.finalAobtUnix);
                        cd.innerText = `Departed ${st.t} (${st.d < 0 ? '-' : '+'}${fmt(st.d)})`;
                        cd.style.color = st.c;
                        let aobtSp = document.getElementById('bdAobt');
                        if (aobtSp) aobtSp.style.color = st.c;
                    } else {
                        d = payload.rawUnix - currentSobtUnix;
                        let isLate = d > 0;
                        let absDiff = Math.abs(d);
                        let h = Math.floor(absDiff / 3600);
                        let m = Math.floor((absDiff % 3600) / 60);
                        let s = absDiff % 60;
                        let timeStr = (h > 0 ? `${h}h ` : '') + `${m}m ${s}s`;
                        cd.innerText = isLate ? `Delayed by: ${timeStr}` : `SOBT in: ${timeStr}`;
                        cd.style.color = isLate ? '#F87171' : '#34D399';
                        let aobtSp = document.getElementById('bdAobt');
                        if (aobtSp) aobtSp.style.color = '#FACC15';
                    }

                    // Update Gradient Pointer
                    let pb = document.getElementById('puncBarContainer');
                    if (pb) {
                        pb.style.display = 'block';
                        let pct = 0;
                        if (d < -300) pct = Math.max(0, 20 - (Math.abs(d) - 300) / 100); // Blue: 0-20%
                        else if (d <= 300) pct = 20 + ((d + 300) / 600) * 30; // Green: 20-50%
                        else if (d <= 900) pct = 50 + ((d - 300) / 600) * 30; // Orange: 50-80%
                        else pct = Math.min(100, 80 + ((d - 900) / 600) * 20); // Red: 80-100%
                        
                        document.getElementById('puncPointer').style.left = pct + '%';
                        let lbl = document.getElementById('puncLabel');
                        lbl.style.left = pct + '%';
                        let dMin = Math.round(d / 60);
                        lbl.innerText = (dMin > 0 ? `+${dMin}m` : `${dMin}m`);
                        if (pct < 10) lbl.style.transform = 'translateX(0)';
                        else if (pct > 90) lbl.style.transform = 'translateX(-100%)';
                        else lbl.style.transform = 'translateX(-50%)';
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
            case 'flightReport':
                let rep = payload.report;
                let isLate = rep.DelaySec > 300;
                let isEarly = rep.RawDelaySec < -300;
                let puncText = isLate ? `${Math.round(rep.DelaySec / 60)}m Late` : (isEarly ? `${Math.abs(Math.round(rep.RawDelaySec / 60))}m Early` : 'On Time');
                let puncColor = isLate ? '#EF4444' : (isEarly ? '#60A5FA' : '#34D399');
                
                let reportHtml = `
                    <div style="background: #0f172a; border: 1px solid #334155; border-radius: 6px; padding: 12px; margin-top: 15px; margin-bottom: 5px; box-shadow: 0 4px 6px -1px rgba(0, 0, 0, 0.5);">
                        <h3 style="color: #38BDF8; font-size: 14px; text-transform: uppercase; border-bottom: 1px solid #334155; padding-bottom: 6px; margin: 0 0 10px 0;">
                            <span style="font-size:16px;">📋</span> FINAL FLIGHT REPORT : ${rep.Airline}${rep.FlightNo}
                        </h3>
                        <div style="display: grid; grid-template-columns: 1fr 1fr; gap: 8px; font-size: 12px; color: #cbd5e1;">
                            <div><strong style="color:#94a3b8;">Route:</strong> ${rep.Dep} ➔ ${rep.Arr}</div>
                            <div><strong style="color:#94a3b8;">SuperScore:</strong> <span style="color:#FACC15; font-size:14px; font-weight:bold;">${rep.Score} pts</span></div>
                            
                            <div><strong style="color:#94a3b8;">Block Time:</strong> ${rep.BlockTime}m <em style="color:#64748b;">(Plan: ${rep.SchedBlockTime}m)</em></div>
                            <div><strong style="color:#94a3b8;">Punctuality:</strong> <span style="color:${puncColor}; font-weight:bold;">${puncText}</span></div>
                            
                            <div><strong style="color:#94a3b8;">Touchdown:</strong> ${rep.TouchdownFpm.toFixed(0)} fpm</div>
                            <div><strong style="color:#94a3b8;">Impact Force:</strong> ${rep.TouchdownGForce.toFixed(2)} G</div>
                            
                            <div><strong style="color:#94a3b8;">Block Fuel:</strong> ${rep.BlockFuel}</div>
                            <div><strong style="color:#94a3b8;">TOW / ZFW:</strong> ${rep.Tow} / ${rep.Zfw}</div>
                        </div>
                    </div>
                `;
                let termRep = document.getElementById('logTerminal');
                if (termRep) {
                    let logDiv = document.createElement('div');
                    logDiv.innerHTML = reportHtml;
                    termRep.appendChild(logDiv);
                    termRep.scrollTop = termRep.scrollHeight;
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
                    isFlightActive = true;
                    const btnFetch = document.getElementById('btnFetchPlan');
                    if (btnFetch) {
                        btnFetch.innerText = 'Cancel Current Flight';
                        btnFetch.style.backgroundColor = 'rgba(239, 68, 68, 0.1)';
                        btnFetch.style.color = '#EF4444';
                        btnFetch.style.border = '1px solid #EF4444';
                    }
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
                const startBtn = document.getElementById('btnStartGroundOps');
                if (startBtn) {
                    startBtn.style.display = 'block';
                    startBtn.disabled = false;
                    startBtn.style.backgroundColor = '#10B981';
                    startBtn.style.color = '#FFFFFF';
                    startBtn.style.cursor = 'pointer';
                    startBtn.innerText = 'Start Ground Ops';
                }
                // Reset Meta Bar display to default
                const metaText = document.getElementById('dashMetaText');
                const metaFill = document.getElementById('dashMetaFill');
                const metaBar = document.getElementById('dashMetaBar');
                if (metaText) { metaText.innerText = 'GROUND OPS : STANDBY'; metaText.style.color = '#cbd5e1'; }
                if (metaFill) { metaFill.style.width = '0%'; metaFill.style.backgroundColor = '#38BDF8'; }
                if (metaBar) metaBar.style.display = 'none';
                break;
            case 'flightReset':
                location.reload();
                break;
            case 'flightCancelled':
                isFlightCancelled = true;
                isFlightActive = false;
                const btnFetch = document.getElementById('btnFetchPlan');
                if (btnFetch) {
                    btnFetch.innerText = 'Fetch Latest Plan';
                    btnFetch.style.backgroundColor = '#4A90E2';
                    btnFetch.style.color = 'white';
                    btnFetch.style.border = 'none';
                }
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
                const mBar = document.getElementById('dashMetaBar');
                if (mBar) {
                    const mText = document.getElementById('dashMetaText');
                    const mFill = document.getElementById('dashMetaFill');
                    if (mText) { mText.innerText = "OPS ABORTED"; mText.style.color = "#DC2626"; }
                    if (mFill) mFill.style.backgroundColor = "#DC2626";
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
                
                if (document.getElementById('dashMetaBar')) document.getElementById('dashMetaBar').style.display = 'block';
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
                    let flightLevel = d.general.initial_alt || '';
                    if (!flightLevel && d.general.stepclimb_string) {
                        const parts = d.general.stepclimb_string.split('/');
                        if (parts.length > 1) flightLevel = parts[parts.length - 1]; // e.g., "0360"
                    }

                    let cruiseStr = flightLevel ? 'FL' + flightLevel : 'N/A';
                    if (d.general.stepclimb_string && d.general.stepclimb_string !== `${d.origin?.icao_code || ''}/${flightLevel}`) {
                        cruiseStr += ` (Steps: ${d.general.stepclimb_string})`;
                    }
                    document.getElementById('bdCruise').innerText = cruiseStr;

                    let routeStr = d.general.route || '';
                    if (flightLevel) routeStr += ` (FL${flightLevel})`;
                    document.getElementById('bdRoute').innerText = routeStr;
                    document.getElementById('bdRoute').title = routeStr;

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
                    const uiTimeFmt = document.getElementById('selTimeFormat') ? document.getElementById('selTimeFormat').value : '24H';
                    const formatTime = (unix) => {
                        const dt = new Date(unix * 1000);
                        let h = dt.getUTCHours();
                        let m = dt.getUTCMinutes().toString().padStart(2, '0');
                        if (uiTimeFmt === '12H') {
                            const ampm = h >= 12 ? 'pm' : 'am';
                            h = h % 12;
                            if (h === 0) h = 12;
                            return `${h}:${m}${ampm}z`;
                        } else {
                            return `${h.toString().padStart(2, '0')}:${m}z`;
                        }
                    };

                    currentSobtUnix = parseInt(d.times.sched_out || '0');
                    document.getElementById('bdSobt').innerText = formatTime(currentSobtUnix);
                    window.currentSibtUnix = parseInt(d.times.sched_in || '0');
                    document.getElementById('bdSibt').innerText = formatTime(window.currentSibtUnix);
                    let eteSec = parseInt(d.times.est_time_enroute || '0');
                    let h = Math.floor(eteSec / 3600);
                    let m = Math.floor((eteSec % 3600) / 60);
                    document.getElementById('bdEte').innerText = `${h.toString().padStart(2, '0')}:${m.toString().padStart(2, '0')}`;
                }

                if (d.weights && d.params) {
                    document.getElementById('bdPax').innerText = d.weights.pax_count || '';
                    
                    const uiWeightUnit = document.getElementById('selUnitWeight') ? document.getElementById('selUnitWeight').value : 'LBS';
                    const sbUnits = d.params.units || 'LBS';
                    const isLbsToKg = (sbUnits === 'LBS' && uiWeightUnit === 'KG');
                    const isKgToLbs = (sbUnits === 'KGS' && uiWeightUnit === 'LBS');

                    const convertWeight = (valStr) => {
                        if (!valStr) return '';
                        let val = parseFloat(valStr);
                        if (isNaN(val)) return valStr;
                        if (isLbsToKg) return Math.round(val * 0.453592);
                        if (isKgToLbs) return Math.round(val * 2.20462);
                        return Math.round(val);
                    };

                    document.getElementById('bdZfw').innerText = convertWeight(d.weights.est_zfw) + ' ' + uiWeightUnit;
                    document.getElementById('bdLdw').innerText = convertWeight(d.weights.est_ldw) + ' ' + uiWeightUnit;
                    let fuel = d.fuel?.plan_ramp || d.weights.est_block || d.weights.block_fuel || '';
                    document.getElementById('bdFuel').innerText = convertWeight(fuel) + ' ' + uiWeightUnit;
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
                updateMetaBar(payload.services);
                break;
            case 'groundOpsComplete':
                document.getElementById('groundOpsContainer').innerHTML = '<p style="color:#34D399; font-weight:bold;">All ground operations are complete. Aircraft is secure.</p>';
                break;
        }
    });

});

window.openAccordions = window.openAccordions || new Set();

const GO_ICONS = {
    'Refuel': '⛽',
    'Boarding': '🛂',
    'Cargo': '🧳',
    'Catering': '🍽️',
    'Cleaning': '🧹',
    'Water/Waste': '💧'
};

const GO_NARRATIVES = {
    'Refuel': 'Le camion-citerne est connecté et transfère le carburant vers les réservoirs principaux.',
    'Boarding': 'L\'embarquement des passagers est en cours via la porte principale.',
    'Cargo': 'Les bagagistes chargent les conteneurs ULD et les bagages en vrac dans soutes.',
    'Catering': 'Le service traiteur réapprovisionne les galleys et chariots repas.',
    'Cleaning': 'L\'équipe de nettoyage prépare la cabine pour le prochain vol.',
    'Water/Waste': 'Vidange des toilettes et remplissage de l\'eau potable en cours.'
};

window.toggleAccordion = function(name) {
    if (window.openAccordions.has(name)) window.openAccordions.delete(name);
    else window.openAccordions.add(name);
    
    const safeName = name.replace(/\s|[^\w]/g, '');
    const content = document.getElementById('acc-content-' + safeName);
    if (content) content.style.display = window.openAccordions.has(name) ? 'block' : 'none';
    const chevron = document.getElementById('acc-icon-' + safeName);
    if (chevron) chevron.style.transform = window.openAccordions.has(name) ? 'rotate(180deg)' : 'rotate(0deg)';
};

window.groundServiceStates = window.groundServiceStates || {};
window.recentlyCompleted = window.recentlyCompleted || null;
window.recentlyCompletedTime = window.recentlyCompletedTime || 0;

function updateMetaBar(services) {
    if (!services || services.length === 0) return;

    let totalDuration = 0;
    let totalElapsed = 0;
    let isActive = false;
    let isFinished = true;
    let blockingService = null;
    let maxDelaySec = -1;

    services.forEach(s => {
        let duration = s.TotalDurationSec + s.DelayAddedSec;
        totalDuration += duration;
        totalElapsed += Math.min(s.ElapsedSec, duration);
        if (s.State === 1 /* InProgress */ || s.State === 2 /* Delayed */) isActive = true;
        if (s.State !== 3 /* Completed */ && s.State !== 4 /* Skipped */) isFinished = false;
        
        if (s.State === 2 /* Delayed */ && s.DelayAddedSec > maxDelaySec) {
            maxDelaySec = s.DelayAddedSec;
            blockingService = s;
        }

        if (s.State === 3 && window.groundServiceStates[s.Name] !== 3) {
            window.recentlyCompleted = s;
            window.recentlyCompletedTime = Date.now();
        }
        window.groundServiceStates[s.Name] = s.State;
    });

    let percent = totalDuration > 0 ? (totalElapsed / totalDuration) * 100 : 0;
    
    const metaBar = document.getElementById('dashMetaBar');
    if (!metaBar) return;
    if (metaBar.style.display === 'none') metaBar.style.display = 'block';

    const metaFill = document.getElementById('dashMetaFill');
    const metaText = document.getElementById('dashMetaText');

    function getSeverityColor(sec) {
        if (sec < 180) return '#FACC15'; // Jaune (< 3 min)
        if (sec <= 420) return '#FB923C'; // Orange (3 - 7 min)
        return '#EF4444'; // Rouge (> 7 min)
    }

    if (metaFill) metaFill.style.width = percent + '%';
    if (metaText) {
        if (isFinished && totalDuration > 0) {
            metaText.innerText = "Ground Operations Completed";
            metaText.style.color = "#34D399";
            if (metaFill) metaFill.style.backgroundColor = "#34D399";
        } else if (blockingService) {
            let color = getSeverityColor(blockingService.DelayAddedSec);
            let mins = Math.round(blockingService.DelayAddedSec / 60);
            metaText.innerText = `⚠️ ${blockingService.Name.toUpperCase()} : ${blockingService.ActiveDelayEvent} (+${mins}m)`;
            metaText.style.color = color;
            if (metaFill) { metaFill.style.backgroundColor = color; metaFill.style.boxShadow = `0 0 10px ${color}`; }
            setTimeout(() => { if (metaFill) metaFill.style.boxShadow = 'none'; }, 1000);
        } else if (window.recentlyCompleted && (Date.now() - window.recentlyCompletedTime < 15000)) {
            const icon = GO_ICONS[window.recentlyCompleted.Name] || '✅';
            metaText.innerText = `${icon} ${window.recentlyCompleted.Name.toUpperCase()} TERMINÉ`;
            metaText.style.color = "#34D399";
            if (metaFill) metaFill.style.backgroundColor = "#34D399";
        } else if (isActive) {
            metaText.innerText = `Ground Operations in Progress (${Math.round(percent)}%)`;
            metaText.style.color = "#FACC15";
            if (metaFill) metaFill.style.backgroundColor = "#4A90E2";
        } else {
            metaText.innerText = "Ground Operations Standing By";
            metaText.style.color = "#94A3B8";
            if (metaFill) metaFill.style.backgroundColor = "#334155";
        }
    }
}

function renderGroundOps(services) {
    const container = document.getElementById('groundOpsContainer');
    let html = '';
    
    services.forEach(s => {
        let btnHtml = '';
        if (s.IsOptional && s.State !== 3 /* Completed */ && s.State !== 4 /* Skipped */) {
            btnHtml = `<button class="go-btn" onclick="skipService('${s.Name}')">Skip / Abort</button>`;
        }
        
        let getSeverityColor = (sec) => {
            if (sec < 180) return '#FACC15'; 
            if (sec <= 420) return '#FB923C'; 
            return '#EF4444'; 
        };

        let statusColor = '#94A3B8';
        let barColor = '#4A90E2';
        if (s.State === 2 /* Delayed */) { 
            let c = getSeverityColor(s.DelayAddedSec);
            statusColor = c; 
            barColor = c; 
        }
        if (s.State === 3 /* Completed */) { statusColor = '#34D399'; barColor = '#34D399'; }
        if (s.State === 4 /* Skipped */) { statusColor = '#F87171'; barColor = '#F87171'; }

        let timeDisplay = '';
        if (s.RemainingSec > 0) {
            const m = Math.floor(s.RemainingSec / 60).toString().padStart(2, '0');
            const sec = (s.RemainingSec % 60).toString().padStart(2, '0');
            timeDisplay = `(-${m}:${sec})`;
        }

        const safeName = s.Name.replace(/\s|[^\w]/g, '');
        const isOpen = window.openAccordions.has(s.Name);
        const displayStyle = isOpen ? 'block' : 'none';
        const chevronRot = isOpen ? 'rotate(180deg)' : 'rotate(0deg)';
        const icon = GO_ICONS[s.Name] || '🔹';
        const narrative = GO_NARRATIVES[s.Name] || 'Opération au sol en cours.';

        html += `
            <div class="go-accordion">
                <div class="go-acc-header" onclick="toggleAccordion('${s.Name}')">
                    <div class="go-acc-title">
                        <span class="go-icon">${icon}</span>
                        <strong style="color: ${s.State === 3 ? '#34D399' : '#F8FAFC'};">${s.Name}</strong>
                    </div>
                    <div class="go-acc-summary">
                        <span style="color: ${statusColor}; font-weight: 600;">${s.StatusMessage} ${timeDisplay}</span>
                        <svg id="acc-icon-${safeName}" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="#94A3B8" stroke-width="2" style="transform: ${chevronRot}; transition: transform 0.2s;">
                            <polyline points="6 9 12 15 18 9"></polyline>
                        </svg>
                    </div>
                </div>
                <div class="go-acc-bar">
                    <div class="go-bar-fill" style="width: ${s.ProgressPercent}%; background-color: ${barColor};"></div>
                </div>
                <div id="acc-content-${safeName}" class="go-acc-content" style="display: ${displayStyle};">
                    <p style="color: #cbd5e1; font-size: 13px; margin: 0 0 15px 0; line-height: 1.5; font-style: italic;">"${narrative}"</p>
                    <div style="display: flex; justify-content: space-between; align-items: center;">
                        <span style="font-size: 12px; color: #64748b; font-family: monospace;">STATUS: ${s.State === 3 ? 'FINISHED' : s.State === 4 ? 'ABORTED' : s.State === 2 ? 'DELAYED' : 'ACTIVE'}</span>
                        ${btnHtml}
                    </div>
                </div>
            </div>
        `;
    });
    
    container.innerHTML = html;
}

// Global skip function for inline onclick
window.skipService = function(name) {
    window.chrome.webview.postMessage({ action: 'skipService', service: name });
};
