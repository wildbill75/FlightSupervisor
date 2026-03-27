document.addEventListener('DOMContentLoaded', () => {
    window.isFlightActive = false;

    // Top Bar Dragging Interop
    const topBar = document.getElementById('top-bar');
    if (topBar) {
        topBar.addEventListener('mousedown', (e) => {
            if (e.target.closest('.window-controls') || e.target.closest('button')) return;
            if (e.button === 0) {
                window.chrome.webview.postMessage({ action: 'drag' });
            }
        });
    }

    // Menu Tab Navigation
    const btnAcceptContract = document.getElementById('btnAcceptContract');
    if (btnAcceptContract) {
        btnAcceptContract.addEventListener('click', () => {
            btnAcceptContract.innerHTML = `
                <div class="flex items-center gap-2">
                    <span class="material-symbols-outlined text-[16px]">check_circle</span>
                    <span>CONTRACT ACTIVE</span>
                </div>
            `;
            btnAcceptContract.style.pointerEvents = 'none';
            btnAcceptContract.classList.remove('from-emerald-500', 'to-emerald-400', 'text-[#0B0C10]', 'hover:brightness-110');
            btnAcceptContract.classList.add('bg-emerald-900/40', 'text-emerald-400', 'border', 'border-emerald-500/30');
            btnAcceptContract.style.background = 'transparent';
            btnAcceptContract.style.boxShadow = 'none';
            
            window.chrome.webview.postMessage({ action: 'acceptContract' });
        });
    }
    
    const menuItems = document.querySelectorAll('.menu li, li[data-target="profile"]');
    const sections = document.querySelectorAll('section');

    menuItems.forEach(item => {
        item.addEventListener('click', () => {
            // Update Active Menu
            menuItems.forEach(m => m.classList.remove('active'));
            item.classList.add('active');

            // Update Active Section
            const targetId = item.getAttribute('data-target');
            if (targetId === 'logs') {
                window.chrome.webview.postMessage({ action: 'fetchLogbook' });
            }
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
    const selItems = ['selLanguage', 'selTimeFormat', 'selUnitSpeed', 'selUnitAlt', 'selUnitWeight', 'selUnitTemp', 'selUnitPress', 'selCrisisFreq'];
    selItems.forEach(id => {
        const val = localStorage.getItem(id);
        const el = document.getElementById(id);
        if (val && el) el.value = val;
    });
    
    setTimeout(() => {
        const initialFreq = localStorage.getItem('selCrisisFreq') || 'Realistic';
        window.chrome.webview.postMessage({ action: 'setCrisisFrequency', value: initialFreq });
    }, 500);

    // Time Formatting
    window.getFormattedTime = function(unix) {
        if (!unix || unix == "0") return "--:--z";
        const dt = new Date(unix * 1000);
        const format = localStorage.getItem('selTimeFormat') || '24H';
        let h = dt.getUTCHours();
        let m = dt.getUTCMinutes().toString().padStart(2, '0');
        if (format === '12H') {
            let ampm = h >= 12 ? 'PM' : 'AM';
            h = h % 12;
            h = h ? h : 12;
            return `${h.toString().padStart(2, '0')}:${m} ${ampm} Z`;
        }
        return `${h.toString().padStart(2, '0')}:${m}z`;
    };

    window.getLocalFormattedTime = function() {
        const dt = new Date();
        const format = localStorage.getItem('selTimeFormat') || '24H';
        return dt.toLocaleTimeString('en-US', { hour: '2-digit', minute: '2-digit', second: '2-digit', hour12: format === '12H' });
    };

    // Language processing
    function setLanguage(lang) {
        if (!window.locales || !window.locales[lang]) return;
        const dict = window.locales[lang];
        document.querySelectorAll('[data-i18n]').forEach(el => {
            const key = el.getAttribute('data-i18n');
            if (dict[key]) {
                if (el.tagName === 'INPUT' && el.type === 'text') {
                    el.placeholder = dict[key];
                } else {
                    el.innerHTML = dict[key];
                }
            }
        });

        // Update btnStartGroundOps innerHTML preserving icon if it's not in progress
        const startBtn = document.getElementById('btnStartGroundOps');
        if (startBtn && !startBtn.disabled) {
            startBtn.innerHTML = `<span class="material-symbols-outlined text-[18px]">flight_takeoff</span> ${dict.btn_start_ops}`;
        }
        
        // Update Fetch button
        const cancelLbl = document.getElementById('btnFetchPlanLabel');
        if (cancelLbl) {
            if (window.isFlightActive) {
                cancelLbl.innerText = dict.modal_cancel_yes;
            } else {
                cancelLbl.innerText = dict.btn_fetch_plan;
            }
        }
    }

    // Initialize Language
    const savedLang = localStorage.getItem('selLanguage') || 'EN';
    const initialLang = savedLang.toLowerCase();
    setLanguage(initialLang);
    
    setTimeout(() => {
        if (window.chrome && window.chrome.webview) {
            window.chrome.webview.postMessage({ action: 'changeLanguage', language: initialLang });
        }
    }, 500);

    window.closeSystemMenu = function() {
        const sysMenu = document.getElementById('systemMenu');
        sysMenu.classList.add('opacity-0', 'pointer-events-none');
        sysMenu.classList.remove('opacity-100');
    }

    function updateIntercomButtons(payload) {
        const container = document.getElementById('dynamicIntercomContainer');
        if (!container) return;

        let buttonsHtml = '';
        const phase = payload.phaseEnum; // Provided via backend: "AtGate", "TaxiOut", "Cruise", etc.
        const used = payload.issuedCommands || [];

        // Button Generator Helper
        const makeBtn = (action, val, text, colorClass, iconHtml) => {
            const propName = action === 'pncCommand' ? 'command' : (action === 'resolveCrisis' ? 'crisisType' : 'annType');
            return `<button onclick="window.chrome.webview.postMessage({action:'${action}', ${propName}:'${val}'})" class="w-full py-2 ${colorClass} rounded-lg text-white font-bold text-[10px] uppercase tracking-widest transition-colors flex items-center justify-center gap-2 shadow hover:brightness-110">
                        <span class="material-symbols-outlined text-[14px]">${iconHtml}</span> ${text}
                    </button>`;
        };

        const colorsPnc = 'bg-sky-900/40 border border-sky-500/30 text-sky-400';
        const colorsPa = 'bg-emerald-900/40 border border-emerald-500/30 text-emerald-400';
        const colorsAlert = 'bg-orange-900/40 border border-orange-500/30 text-orange-400';

        // Cabin Passenger Announcements (PA)
        if (phase === 'AtGate' || phase === 'Boarding') {
            if (!used.includes('PA_Welcome')) {
                buttonsHtml += makeBtn('announceCabin', 'Welcome', 'PA: Welcome Aboard', colorsPa, 'campaign');
            }
            if (payload.isDelayed && !used.includes('PA_Delay')) {
                buttonsHtml += makeBtn('announceCabin', 'Delay', 'PA: Apology for Delay', colorsAlert, 'warning');
            }
        } else if (phase === 'Descent') {
            if (!used.includes('PA_Descent')) {
                buttonsHtml += makeBtn('announceCabin', 'Descent', 'PA: Initial Descent', colorsPa, 'campaign');
            }
        }

        // PNC Crew Commands (Intercom)
        if (phase === 'TaxiOut') {
            if (!used.includes('PREPARE_TAKEOFF')) {
                buttonsHtml += makeBtn('pncCommand', 'PREPARE_TAKEOFF', 'PNC: Prepare for Takeoff', colorsPnc, 'airline_seat_recline_normal');
            }
        }
        if (phase === 'TaxiOut' || phase === 'Takeoff' || phase === 'InitialClimb') {
            if (!used.includes('SEATS_TAKEOFF')) {
                buttonsHtml += makeBtn('pncCommand', 'SEATS_TAKEOFF', 'PNC: Seats for Takeoff', colorsPnc, 'airline_seat_recline_extra');
            }
        }
        if (phase === 'Climb' || phase === 'Cruise') {
            if (!used.includes('START_SERVICE') && payload.cabinState !== 'ServingMeals') {
                buttonsHtml += makeBtn('pncCommand', 'START_SERVICE', 'PNC: Start Service', colorsPnc, 'room_service');
            }
        }
        if (phase === 'Descent') {
            if (!used.includes('PREPARE_LANDING')) {
                buttonsHtml += makeBtn('pncCommand', 'PREPARE_LANDING', 'PNC: Prepare for Landing', colorsPnc, 'flight_land');
            }
        }
        if (phase === 'Approach' || phase === 'FinalApproach') {
            if (!used.includes('SEATS_LANDING')) {
                buttonsHtml += makeBtn('pncCommand', 'SEATS_LANDING', 'PNC: Seats for Landing', colorsPnc, 'airline_seat_recline_extra');
            }
        }

        // Turbulence / Seatbelt Warnings (Available Airborne)
        if (phase !== 'AtGate' && phase !== 'TaxiOut' && phase !== 'TaxiIn') {
            if (payload.seatbeltsOn) {
                buttonsHtml += makeBtn('announceCabin', 'Turbulence', 'PA: Turbulence Warning', colorsAlert, 'campaign');
            }
        }

        // Active Crisis Resolutions
        if (payload.activeCrisis === 'MedicalEmergency') {
            buttonsHtml += makeBtn('resolveCrisis', 'MedicalEmergency', 'PA: Page Doctor On Board', 'bg-red-900/60 border border-red-500 text-red-400 animate-pulse', 'medical_services');
        } else if (payload.activeCrisis === 'UnrulyPassenger') {
            buttonsHtml += makeBtn('resolveCrisis', 'UnrulyPassenger', 'PNC: Restrain Passenger', 'bg-red-900/60 border border-red-500 text-red-400 animate-pulse', 'security');
        }

        // Always Available
        buttonsHtml += makeBtn('pncCommand', 'REQUEST_STATUS', 'PNC: Request Cabin Report', 'bg-slate-800/80 border border-slate-600/50 text-slate-300', 'assignment');

        // Prevent redundant DOM updates
        if (container.innerHTML !== buttonsHtml) {
            container.innerHTML = buttonsHtml;
        }
    }
    const selLanguage = document.getElementById('selLanguage');
    if (selLanguage) {
        selLanguage.addEventListener('change', (e) => {
            const lang = e.target.value.toLowerCase();
            setLanguage(lang);
            // Notify C# Backend about language change
            window.chrome.webview.postMessage({ action: 'changeLanguage', language: lang });
        });
    }

    const savedHardcore = localStorage.getItem('chkHardcore');
    if (savedHardcore !== null && document.getElementById('chkHardcore')) {
        document.getElementById('chkHardcore').checked = (savedHardcore === 'true');
    }

    const savedTop = localStorage.getItem('chkAlwaysOnTop');
    if (savedTop !== null && document.getElementById('chkAlwaysOnTop')) {
        const isTop = (savedTop === 'true');
        document.getElementById('chkAlwaysOnTop').checked = isTop;
        if (document.getElementById('btnPin')) {
            document.getElementById('btnPin').style.opacity = isTop ? '1' : '0.4';
        }
        window.chrome.webview.postMessage({ action: 'setAlwaysOnTop', value: isTop });
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
            
            const selItems = ['selLanguage', 'selTimeFormat', 'selUnitSpeed', 'selUnitAlt', 'selUnitWeight', 'selUnitTemp', 'selUnitPress', 'selCrisisFreq'];
            selItems.forEach(id => {
                const el = document.getElementById(id);
                if (el) {
                    localStorage.setItem(id, el.value);
                    if (id === 'selCrisisFreq') {
                        window.chrome.webview.postMessage({ action: 'setCrisisFrequency', value: el.value });
                    }
                }
            });
            const hardcore = document.getElementById('chkHardcore') ? document.getElementById('chkHardcore').checked : false;
            localStorage.setItem('chkHardcore', hardcore);
            
            const isTop = document.getElementById('chkAlwaysOnTop') ? document.getElementById('chkAlwaysOnTop').checked : false;
            localStorage.setItem('chkAlwaysOnTop', isTop);
            if (document.getElementById('btnPin')) {
                document.getElementById('btnPin').style.opacity = isTop ? '1' : '0.4';
            }
            window.chrome.webview.postMessage({ action: 'setAlwaysOnTop', value: isTop });
            
            if (username) localStorage.setItem('sbUsername', username);
            localStorage.setItem('groundSpeed', groundSpeed);
            localStorage.setItem('groundProb', groundProb);
            localStorage.setItem('weatherSource', weatherSrc);
            localStorage.setItem('gsxSync', gsxSync);
            
            const lang = (localStorage.getItem('selLanguage') || 'EN').toLowerCase();
            const dict = window.locales && window.locales[lang] ? window.locales[lang] : window.locales.en;
            
            btnSaveSettings.innerText = dict.btn_settings_saved || 'Settings Saved';
            btnSaveSettings.style.backgroundColor = '#34D399';
            
            setTimeout(() => { 
                btnSaveSettings.innerText = dict.btn_save_settings || 'Save Settings';
                btnSaveSettings.style.backgroundColor = '#4A90E2';
            }, 1500);
        });
    }

    // Fetch Flight Plan / Cancel Flight
    const btnFetchPlan = document.getElementById('btnFetchPlan');
    const cancelModal = document.getElementById('cancelModal');
    const btnCancelYes = document.getElementById('btnCancelYes');
    const btnCancelNo = document.getElementById('btnCancelNo');

    if (btnCancelNo) btnCancelNo.addEventListener('click', () => { cancelModal.style.display = 'none'; });
    if (btnCancelYes) btnCancelYes.addEventListener('click', () => {
        cancelModal.style.display = 'none';
        window.chrome.webview.postMessage({ action: 'cancelFlight' });
    });

    // Profile Avatar Upload
    const avatarInput = document.getElementById('avatarUploadInput');
    if (avatarInput) {
        avatarInput.addEventListener('change', (e) => {
            const file = e.target.files[0];
            if (file) {
                const reader = new FileReader();
                reader.onload = (event) => {
                    const base64Str = event.target.result;
                    // Update UI immediately
                    const sbImg = document.getElementById('sbProfileImg');
                    if (sbImg) {
                        sbImg.src = base64Str;
                        sbImg.classList.remove('hidden');
                        document.getElementById('sbProfileIcon').classList.add('hidden');
                    }
                    
                    const bigImg = document.getElementById('prfBigAvatar');
                    if (bigImg) {
                        bigImg.src = base64Str;
                        bigImg.style.objectPosition = "50% 50%";
                        bigImg.classList.remove('hidden');
                        document.getElementById('prfBigIcon').classList.add('hidden');
                    }
                    
                    // Send to backend C# ProfileManager
                    window.chrome.webview.postMessage({ action: 'updateAvatar', payload: base64Str });
                };
                reader.readAsDataURL(file);
            }
        });
    }

    const btnEditAvatar = document.getElementById('btnEditAvatar');
    if (btnEditAvatar) {
        btnEditAvatar.addEventListener('click', () => {
            if (avatarInput) avatarInput.click();
        });
    }

    const prfBigAvatar = document.getElementById('prfBigAvatar');
    if (prfBigAvatar) {
        let isDraggingAvatar = false;
        let startX, startY;
        let startPosX = 50, startPosY = 50;

        prfBigAvatar.addEventListener('mousedown', (e) => {
            isDraggingAvatar = true;
            startX = e.clientX;
            startY = e.clientY;
            let currentPos = prfBigAvatar.style.objectPosition || '50% 50%';
            let parts = currentPos.trim().split(/\s+/);
            if (parts.length >= 2) {
                startPosX = parseFloat(parts[0]) || 50;
                startPosY = parseFloat(parts[1]) || 50;
            }
            prfBigAvatar.style.cursor = 'grabbing';
            e.preventDefault();
        });

        window.addEventListener('mousemove', (e) => {
            if (!isDraggingAvatar) return;
            const dx = e.clientX - startX;
            const dy = e.clientY - startY;
            // Negative mapping so pulling mouse left brings the object position right (natural grabbing)
            const newX = Math.max(0, Math.min(100, startPosX - (dx * 0.5)));
            const newY = Math.max(0, Math.min(100, startPosY - (dy * 0.5)));
            const pos = `${newX}% ${newY}%`;
            prfBigAvatar.style.objectPosition = pos;
            
            const sbImg = document.getElementById('sbProfileImg');
            if (sbImg) sbImg.style.objectPosition = pos;
        });

        window.addEventListener('mouseup', () => {
            if (isDraggingAvatar) {
                isDraggingAvatar = false;
                prfBigAvatar.style.cursor = 'grab';
                window.chrome.webview.postMessage({ 
                    action: 'updateProfileField', 
                    field: 'AvatarPosition', 
                    value: prfBigAvatar.style.objectPosition
                });
            }
        });

        prfBigAvatar.style.cursor = 'grab';
    }

    // Profile Text Fields Edit
    const profileFields = [
        { id: 'prfCallsign', field: 'CallSign' },
        { id: 'prfFullName', field: 'FullName' },
        { id: 'prfHomeBase', field: 'HomeBaseIcao' },
        { id: 'prfCountry', field: 'CountryCode' }
    ];

    profileFields.forEach(pf => {
        const el = document.getElementById(pf.id);
        if (el) {
            el.addEventListener('blur', () => {
                let v = el.innerText.trim();
                // We keep HomeBase and Country in uppercase naturally, but Callsign can be mixed case
                if (pf.field === 'HomeBaseIcao') v = v.toUpperCase();
                if (pf.field === 'CountryCode') v = v.toUpperCase();
                if (el.innerText !== v) el.innerText = v;
                window.chrome.webview.postMessage({ 
                    action: 'updateProfileField', 
                    field: pf.field, 
                    value: v
                });
            });
            el.addEventListener('input', () => {
                // Save immediately without reformatting text to prevent cursor jump
                window.chrome.webview.postMessage({ 
                    action: 'updateProfileField', 
                    field: pf.field, 
                    value: el.innerText.trim()
                });
            });
            el.addEventListener('keydown', (e) => {
                if (e.key === 'Enter') {
                    e.preventDefault();
                    el.blur();
                }
            });
        }
    });

    // Connect to Simulator

    const abortModal = document.getElementById('abortServiceModal');
    const btnAbortYes = document.getElementById('btnAbortYes');
    const btnAbortNo = document.getElementById('btnAbortNo');

    if (btnAbortNo) btnAbortNo.addEventListener('click', () => { abortModal.style.display = 'none'; });
    if (btnAbortYes) btnAbortYes.addEventListener('click', () => {
        abortModal.style.display = 'none';
        if (window.currentAbortService) {
            window.chrome.webview.postMessage({ action: 'skipService', service: window.currentAbortService });
            window.currentAbortService = null;
        }
    });

    const btnDismissReport = document.getElementById('btnDismissReport');
    if (btnDismissReport) btnDismissReport.addEventListener('click', () => {
        document.getElementById('flightReportModal').style.display = 'none';
    });

    btnFetchPlan.addEventListener('click', () => {
        if (window.isFlightActive) {
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
            weatherSource: localStorage.getItem('weatherSource') || 'simbrief',
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
            btnStartGroundOps.innerHTML = '<span class="material-symbols-outlined text-[18px]">flight_takeoff</span> In Progress';
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
                btnSmartConnect.innerText = 'Connecting...';
                btnSmartConnect.className = 'text-[10px] font-bold py-4 px-8 tracking-widest uppercase rounded-xl bg-orange-900/20 text-orange-400 border border-orange-500/20 shadow-[0_0_15px_rgba(249,115,22,0.1)] transition-colors w-full';
                btnSmartConnect.style.color = '';
            }
        });
    }

    const btnMin = document.getElementById('btnMin');
    const btnMax = document.getElementById('btnMax');
    const btnClose = document.getElementById('btnClose');
    const btnPin = document.getElementById('btnPin');
    
    if (btnMin) btnMin.addEventListener('click', () => window.chrome.webview.postMessage({ action: 'minimizeApp' }));
    if (btnMax) btnMax.addEventListener('click', () => {
        window.chrome.webview.postMessage({ action: 'maximizeApp' });
        const icon = btnMax.querySelector('.material-symbols-outlined');
        if (icon) icon.innerText = icon.innerText === 'crop_square' ? 'content_copy' : 'crop_square';
    });
    if (btnClose) btnClose.addEventListener('click', () => window.chrome.webview.postMessage({ action: 'closeApp' }));
    if (btnPin) btnPin.addEventListener('click', () => {
        let isTop = localStorage.getItem('chkAlwaysOnTop') === 'true';
        isTop = !isTop;
        localStorage.setItem('chkAlwaysOnTop', isTop);
        if (document.getElementById('chkAlwaysOnTop')) document.getElementById('chkAlwaysOnTop').checked = isTop;
        btnPin.style.opacity = isTop ? '1' : '0.4';
        window.chrome.webview.postMessage({ action: 'setAlwaysOnTop', value: isTop });
    });

    let currentSobtUnix = 0;
    let isFlightCancelled = false;

    // WebView2 Global Message Receiver
    window.chrome.webview.addEventListener('message', event => {
        const payload = event.data;
        if (!payload || !payload.type) return;

        switch (payload.type) {
            case 'briefingUpdate':
                if (typeof window.parseBriefing === 'function') {
                    const html = window.parseBriefing(payload.briefing);
                    const briefingElem = document.getElementById('briefingContent');
                    if (briefingElem) briefingElem.innerHTML = html;
                }
                break;
            case 'savedUsername':
                document.getElementById('sbUsername').value = payload.username;
                break;
            case 'simConnectStatus':
                isSimConnected = payload.status.includes('Connected') || payload.status.includes('Linked');
                const smartBtn = document.getElementById('btnSmartConnect');
                const langSim = (localStorage.getItem('selLanguage') || 'EN').toLowerCase();
                const dictSim = window.locales ? window.locales[langSim] : null;
                
                if (isSimConnected) {
                    if (smartBtn) {
                        smartBtn.innerText = payload.status.includes('Linked') ? 'Linked' : 'Connected';
                        smartBtn.className = 'text-[10px] font-bold py-4 px-8 tracking-widest uppercase rounded-xl bg-emerald-900/20 text-emerald-400 border border-emerald-500/20 shadow-[0_0_15px_rgba(16,185,129,0.1)] hover:bg-emerald-900/40 transition-colors w-full';
                        smartBtn.style.color = '';
                    }
                } else {
                    if (smartBtn) {
                        smartBtn.innerText = dictSim ? dictSim.btn_not_connected : 'Not Connected';
                        smartBtn.className = 'text-[10px] font-bold py-4 px-8 tracking-widest uppercase rounded-xl bg-red-900/20 text-red-500 border border-red-500/20 shadow-[0_0_15px_rgba(239,68,68,0.1)] hover:bg-red-900/40 transition-colors w-full';
                        smartBtn.style.color = '';
                    }
                }
                break;
            case 'telemetry':
                updateIntercomButtons(payload);
                document.getElementById('flightPhase').innerText = `${payload.phase}`;
                if (payload.anxiety !== undefined) {
                    const anxEl = document.getElementById('paxAnxietyValue');
                    const anxBar = document.getElementById('paxAnxietyBar');
                    if (anxEl && anxBar) {
                        anxEl.innerHTML = `${Math.round(payload.anxiety)}<span class="text-sm text-slate-500 font-light ml-1">%</span>`;
                        anxBar.style.width = `${Math.round(payload.anxiety)}%`;
                        let color = '#34D399';
                        if (payload.anxiety >= 60) color = '#EF4444';
                        else if (payload.anxiety >= 30) color = '#F59E0B';
                        
                        anxEl.style.color = color;
                        anxEl.style.textShadow = `0 0 20px ${color}4A`;
                        anxBar.style.backgroundColor = color;
                        anxBar.style.boxShadow = `0 0 8px ${color}80`;
                    }
                }
                if (payload.comfort !== undefined) {
                    const comfEl = document.getElementById('paxComfortValue');
                    const comfBar = document.getElementById('paxComfortBar');
                    if (comfEl && comfBar) {
                        comfEl.innerHTML = `${Math.round(payload.comfort)}<span class="text-sm text-slate-500 font-light ml-1">%</span>`;
                        comfBar.style.width = `${Math.round(payload.comfort)}%`;
                        let color = '#38bdf8';
                        if (payload.comfort <= 30) color = '#EF4444';
                        else if (payload.comfort <= 60) color = '#F59E0B';
                        else if (payload.comfort <= 80) color = '#34D399';
                        
                        comfEl.style.color = color;
                        comfEl.style.textShadow = `0 0 20px ${color}4A`;
                        comfBar.style.backgroundColor = color;
                        comfBar.style.boxShadow = `0 0 8px ${color}80`;
                    }
                }

                // Satiety and Catering Progression
                const sIcon = document.getElementById('satietyIcon');
                if (sIcon) {
                    if (payload.satietyActive) sIcon.classList.remove('hidden');
                    else sIcon.classList.add('hidden');
                }
                
                if (payload.serviceProgress !== undefined && payload.cabinState) {
                    const cBox = document.getElementById('cateringProgressBox');
                    const cBar = document.getElementById('cateringBar');
                    const cVal = document.getElementById('cateringValue');
                    
                    if (cBox && cBar && cVal) {
                        if (payload.cabinState === 'ServingMeals' && payload.serviceProgress > 0 && payload.serviceProgress < 100) {
                            cBox.classList.remove('opacity-0', 'h-0');
                            cBox.classList.add('opacity-100', 'h-10');
                            cBar.style.width = `${payload.serviceProgress}%`;
                            cVal.innerHTML = `${Math.round(payload.serviceProgress)}<span class="text-[10px] text-slate-500 font-light ml-1">%</span>`;
                        } else {
                            cBox.classList.remove('opacity-100', 'h-10');
                            cBox.classList.add('opacity-0', 'h-0');
                        }
                    }
                }

                if (payload.airline) {
                    const card = document.getElementById('airlineIdentityCard');
                    if (card) card.style.display = 'flex';
                    const aName = document.getElementById('aiAirlinename');
                    if (aName && aName.innerText !== payload.airline.name) {
                        aName.innerText = payload.airline.name;
                        aName.title = payload.airline.name;
                        document.getElementById('aiGlobalScore').innerText = payload.airline.globalScore;
                        document.getElementById('aiHard').innerText = payload.airline.hardProductScore + "/10";
                        document.getElementById('aiSoft').innerText = payload.airline.softProductScore + "/10";
                        document.getElementById('aiSafety').innerText = payload.airline.safetyRecord + "/10";
                        
                        const dList = document.getElementById('aiDirectives');
                        if (dList && payload.airline.directives) {
                            dList.innerHTML = '';
                            payload.airline.directives.forEach(dir => {
                                const li = document.createElement('li');
                                li.className = 'text-sky-400/80 mb-1';
                                li.innerText = `• ${dir}`;
                                dList.appendChild(li);
                            });
                        }

                        const oList = document.getElementById('aiObjectivesList');
                        const cSec = document.getElementById('aiContractSection');
                        const btnAcc = document.getElementById('btnAcceptContract');
                        if (oList && payload.airline.objectives) {
                            const obs = payload.airline.objectives;
                            if (obs.maxDelaySec > 0 || obs.mustPerformCatering || obs.minComfort > 0) {
                                cSec.style.display = 'flex';
                                btnAcc.style.display = 'block';
                                oList.innerHTML = `
                                    <span>⏳ Max Delay: <b class="text-white">${Math.floor(obs.maxDelaySec / 60)}m</b></span>
                                    <span class="text-white/20">|</span>
                                    <span>💺 Min Comfort: <b class="text-white">${obs.minComfort}%</b></span>
                                    <span class="text-white/20">|</span>
                                    <span>🛬 Max FPM: <b class="text-white">${obs.maxTouchdownFpm}</b></span>
                                `;
                                if (obs.mustPerformCatering) {
                                    oList.innerHTML += `<span class="text-white/20">|</span><span>🍱 <b class="text-emerald-400">Catering</b></span>`;
                                }
                            } else {
                                cSec.style.display = 'none';
                            }
                        } else if (cSec) {
                            cSec.style.display = 'none';
                        }
                    }
                }
                break;
            case 'pncStatus':
                const pncDot = document.getElementById('pncStatusDot');
                const pncLbl = document.getElementById('pncStatusLabel');
                if(pncDot && pncLbl && payload.status) {
                    pncLbl.innerText = payload.status;
                    if(payload.state === 'SecuringForTakeoff' || payload.state === 'SecuringForLanding') {
                        pncDot.className = "w-2.5 h-2.5 rounded-full bg-orange-500 animate-pulse";
                    } else if(payload.state === 'TakeoffSecured' || payload.state === 'LandingSecured') {
                        pncDot.className = "w-2.5 h-2.5 rounded-full bg-emerald-500 shadow-[0_0_10px_rgba(16,185,129,0.8)]";
                    } else if(payload.state === 'ServingMeals') {
                        pncDot.className = "w-2.5 h-2.5 rounded-full bg-sky-500";
                    } else {
                        pncDot.className = "w-2.5 h-2.5 rounded-full bg-slate-500";
                    }
                }
                break;
            case 'simTime':
                if (payload.rawUnix) document.getElementById('zuluTime').innerText = getFormattedTime(payload.rawUnix);
                else document.getElementById('zuluTime').innerText = payload.time;
                const cd = document.getElementById('flightCountdown');
                if (cd && payload.rawUnix && currentSobtUnix > 0) {
                    let d = 0; // Delay in seconds
                    let isArrTimer = false;
                    
                    const getPunc = (planned, actual) => {
                        let diff = actual - planned;
                        if (diff < -300) return { t: 'Early', c: '#3b82f6', d: diff };
                        if (diff <= 180) return { t: 'On Time', c: '#10b981', d: diff };
                        if (diff <= 420) return { t: 'Light Delay', c: '#eab308', d: diff };
                        if (diff <= 600) return { t: 'Mod Late', c: '#f97316', d: diff };
                        return { t: 'Sig Late', c: '#ef4444', d: diff };
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
                        const mLoc = window.locales ? window.locales[(localStorage.getItem('selLanguage') || 'EN').toLowerCase()] : null;
                        let lblDelay = mLoc && mLoc.dash_delayed_by ? mLoc.dash_delayed_by : 'Delayed by';
                        let lblSobt = mLoc && mLoc.dash_sobt_in ? mLoc.dash_sobt_in : 'SOBT in';
                        cd.innerText = isLate ? `${lblDelay}: ${timeStr}` : `${lblSobt}: ${timeStr}`;
                        let cCol = '#10b981';
                        if (d < -300) cCol = '#3b82f6';
                        else if (d <= 180) cCol = '#10b981';
                        else if (d <= 420) cCol = '#eab308';
                        else if (d <= 600) cCol = '#f97316';
                        else cCol = '#ef4444';
                        cd.style.color = cCol;
                        let aobtSp = document.getElementById('bdAobt');
                        if (aobtSp) aobtSp.style.color = '#FACC15';
                    }

                    // Update Gradient Pointer
                    let pb = document.getElementById('puncBarContainer');
                    if (pb) {
                        pb.style.display = 'block';
                        let pct = 0;
                        if (d <= -900) pct = 0;
                        else if (d < 0) pct = 30 - (Math.abs(d) / 900) * 30; // Blue: 0-30%
                        else if (d <= 180) pct = 30 + (d / 180) * 20; // Green: 30-50%
                        else if (d <= 600) pct = 50 + ((d - 180) / 420) * 30; // Yellow-Orange: 50-80%
                        else pct = Math.min(100, 80 + ((d - 600) / 600) * 20); // Red: 80-100%
                        
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
            case 'crisisTriggered':
                const crisisBanner = document.getElementById('crisisBanner');
                const crisisTitle = document.getElementById('crisisTitle');
                const crisisDesc = document.getElementById('crisisDesc');
                const crisisAudio = document.getElementById('crisisAudio');
                
                if (crisisBanner) {
                    if (crisisTitle) crisisTitle.innerText = payload.title || "CRITICAL ALERT";
                    if (crisisDesc) crisisDesc.innerText = payload.desc || "Immediate crew action required in the cabin.";
                    crisisBanner.style.transform = 'translateY(0)';
                }
                if (crisisAudio) {
                    crisisAudio.loop = true;
                    crisisAudio.play().catch(e => console.warn("Audio autoplay blocked:", e));
                }
                break;
            case 'crisisTick':
                const cTimer = document.getElementById('crisisTimer');
                if (cTimer && payload.elapsedSeconds !== undefined) {
                    const elapsed = payload.elapsedSeconds;
                    const m = Math.floor(elapsed / 60).toString().padStart(2, '0');
                    const s = (elapsed % 60).toString().padStart(2, '0');
                    cTimer.innerText = `${m}:${s}`;
                    cTimer.classList.remove('text-white', 'text-red-400');
                    if (elapsed > 60) cTimer.classList.add('text-red-400');
                    else cTimer.classList.add('text-white');
                }
                break;
            case 'crisisResolved':
                const cBannerRes = document.getElementById('crisisBanner');
                const cAudioRes = document.getElementById('crisisAudio');
                
                if (cBannerRes) {
                    cBannerRes.style.transform = 'translateY(-100%)';
                }
                if (cAudioRes) {
                    cAudioRes.pause();
                    cAudioRes.currentTime = 0;
                }
                break;
            case 'phaseUpdate':
                document.getElementById('flightPhase').innerText = `${payload.phase}`;
                if (payload.aobtUnix) {
                    window.finalAobtUnix = payload.aobtUnix;
                    document.getElementById('bdAobt').innerText = getFormattedTime(payload.aobtUnix);
                } else if (payload.aobt) document.getElementById('bdAobt').innerText = payload.aobt;

                if (payload.aibtUnix) {
                    window.finalAibtUnix = payload.aibtUnix;
                    document.getElementById('bdAibt').innerText = getFormattedTime(payload.aibtUnix);
                } else if (payload.aibt) document.getElementById('bdAibt').innerText = payload.aibt;
                break;
            case 'logbookData':
                renderLogbook(payload.history);
                break;
            case 'flightReport':
                let rep = payload.report;
                let isLate = rep.DelaySec > 300;
                let isEarly = rep.RawDelaySec < -300;
                let puncText = isLate ? `${Math.round(rep.DelaySec / 60)}m Late` : (isEarly ? `${Math.abs(Math.round(rep.RawDelaySec / 60))}m Early` : 'On Time');
                let puncClass = isLate ? 'red' : (isEarly ? 'blue' : 'green');
                if (rep.DelaySec <= 300 && rep.RawDelaySec > 300) puncClass = 'orange'; // Ops Delay Pardon
                
                document.getElementById('frFlightNo').innerText = `${rep.Airline}${rep.FlightNo}`;
                document.getElementById('frRoute').innerText = `${rep.Dep} ➔ ${rep.Arr}`;
                
                const mainScoreEl = document.getElementById('frScore');
                mainScoreEl.innerText = rep.Score;
                mainScoreEl.classList.remove('text-emerald-400', 'text-fuchsia-400', 'text-red-400');
                if (rep.Score >= 1100) mainScoreEl.classList.add('text-fuchsia-400');
                else if (rep.Score >= 1000) mainScoreEl.classList.add('text-emerald-400');
                else mainScoreEl.classList.add('text-red-400');

                const setSubScore = (id, pts) => {
                    const el = document.getElementById(id);
                    if (!el) return;
                    el.innerText = (pts > 0 ? '+' : '') + pts;
                    el.classList.remove('text-emerald-400', 'text-red-400', 'text-white');
                    if (pts > 0) el.classList.add('text-emerald-400');
                    else if (pts < 0) el.classList.add('text-red-400');
                    else el.classList.add('text-white');
                };
                
                setSubScore('frSafetyScore', rep.SafetyPoints || 0);
                setSubScore('frComfortScore', rep.ComfortPoints || 0);
                setSubScore('frMaintScore', rep.MaintenancePoints || 0);
                setSubScore('frOpsScore', rep.OperationsPoints || 0);

                let btHours = Math.floor(rep.BlockTime / 60);
                let btMins = rep.BlockTime % 60;
                let frBlock = document.getElementById('frBlock');
                if(frBlock) frBlock.innerText = `${btHours}h ${btMins}m`;

                const puncBadge = document.getElementById('frPunc');
                if (puncBadge) {
                    puncBadge.innerText = puncText;
                    puncBadge.classList.remove('bg-emerald-500/20', 'text-emerald-400', 'bg-red-500/20', 'text-red-400', 'bg-orange-500/20', 'text-orange-400', 'bg-sky-500/20', 'text-sky-400');
                    if (isLate) puncBadge.classList.add('bg-red-500/20', 'text-red-400');
                    else if (isEarly) puncBadge.classList.add('bg-sky-500/20', 'text-sky-400');
                    else if (rep.DelaySec <= 300 && rep.RawDelaySec > 300) puncBadge.classList.add('bg-orange-500/20', 'text-orange-400');
                    else puncBadge.classList.add('bg-emerald-500/20', 'text-emerald-400');
                }

                let frFuel = document.getElementById('frFuel');
                if(frFuel) frFuel.innerText = rep.BlockFuel;
                
                const fpmEl = document.getElementById('frFpm');
                if (fpmEl) {
                    fpmEl.innerText = `${rep.TouchdownFpm.toFixed(0)} fpm`;
                    fpmEl.classList.remove('text-emerald-400', 'text-red-500', 'text-slate-200');
                    if (rep.TouchdownFpm < -400) fpmEl.classList.add('text-red-500');
                    else if (rep.TouchdownFpm > -150) fpmEl.classList.add('text-emerald-400');
                    else fpmEl.classList.add('text-slate-200');
                }
                
                const gEl = document.getElementById('frGForce');
                if (gEl) {
                    gEl.innerText = `${rep.TouchdownGForce.toFixed(2)} G`;
                    gEl.classList.remove('text-red-500', 'text-slate-200');
                    if (rep.TouchdownGForce > 1.4) gEl.classList.add('text-red-500');
                    else gEl.classList.add('text-slate-200');
                }

                const objContainer = document.getElementById('frObjectivesContainer');
                const objList = document.getElementById('frObjectivesList');
                if (objContainer && objList) {
                    objList.innerHTML = '';
                    objContainer.style.display = 'block';
                    
                    if (rep.Objectives && rep.Objectives.length > 0) {
                        rep.Objectives.forEach(obj => {
                            const isPass = obj.Passed;
                            const icon = isPass ? 'check_circle' : 'cancel';
                            const iconColor = isPass ? 'text-emerald-400' : 'text-red-400';
                            const bgColor = isPass ? 'bg-emerald-500/10' : 'bg-red-500/10';
                            const ptsStr = obj.Points > 0 ? `+${obj.Points} pts` : `${obj.Points} pts`;

                            const row = document.createElement('div');
                            row.className = `flex items-center justify-between p-3 rounded-xl border border-white/5 ${bgColor}`;
                            row.innerHTML = `
                                <div class="flex items-center gap-3">
                                    <span class="material-symbols-outlined ${iconColor}">${icon}</span>
                                    <span class="text-sm text-slate-200 font-medium">${obj.Description}</span>
                                </div>
                                <div class="font-mono font-bold text-sm ${iconColor}">${ptsStr}</div>
                            `;
                            objList.appendChild(row);
                        });
                    } else {
                        objList.innerHTML = `
                            <div class="flex items-center justify-center p-3 rounded-xl border border-white/5 bg-slate-800/30">
                                <span class="text-sm text-slate-400 font-medium italic">No Company Challenge taken</span>
                            </div>
                        `;
                    }
                }

                const achContainer = document.getElementById('frAchievementsContainer');
                const achList = document.getElementById('frAchievementsList');
                if (achContainer && achList) {
                    achList.innerHTML = '';
                    if (rep.NewAchievements && rep.NewAchievements.length > 0) {
                        achContainer.style.display = 'block';
                        rep.NewAchievements.forEach(ach => {
                            let borderClass = 'border-amber-500/30';
                            let bgClass = 'bg-amber-500/10';
                            let textClass = ach.ColorClass || 'text-amber-400';
                            
                            // Hacky parsing to derive borders from text colors
                            if (textClass.includes('sky')) { borderClass = 'border-sky-500/30'; bgClass = 'bg-sky-500/10'; }
                            if (textClass.includes('emerald')) { borderClass = 'border-emerald-500/30'; bgClass = 'bg-emerald-500/10'; }
                            if (textClass.includes('purple')) { borderClass = 'border-purple-500/30'; bgClass = 'bg-purple-500/10'; }
                            if (textClass.includes('red')) { borderClass = 'border-red-500/30'; bgClass = 'bg-red-500/10'; }

                            const row = document.createElement('div');
                            row.className = `flex flex-col items-center p-3 rounded-xl border ${borderClass} ${bgClass} text-center shadow-lg`;
                            row.innerHTML = `
                                <span class="material-symbols-outlined text-3xl mb-2 ${textClass} drop-shadow-[0_0_10px_currentColor]">${ach.Icon || 'workspace_premium'}</span>
                                <span class="text-[10px] font-bold tracking-widest uppercase text-white mb-2 leading-tight">${ach.Title}</span>
                                <span class="text-[9px] text-slate-300 leading-tight">${ach.Description}</span>
                            `;
                            achList.appendChild(row);
                        });
                    } else {
                        achContainer.style.display = 'none';
                    }
                }

                const eventLogContainer = document.getElementById('frEventLog');
                if (eventLogContainer) {
                    eventLogContainer.innerHTML = '';
                    if (rep.FlightEvents && rep.FlightEvents.length > 0) {
                        rep.FlightEvents.forEach(evt => {
                            const isPenalty = evt.Amount < 0;
                            const colorClass = isPenalty ? 'text-red-400' : 'text-emerald-400';
                            const sign = isPenalty ? '' : '+';
                            
                            let icon = 'military_tech';
                            if (evt.Category === 0) icon = 'security';
                            else if (evt.Category === 1) icon = 'mood';
                            else if (evt.Category === 2) icon = 'build';
                            else if (evt.Category === 3) icon = 'schedule';
                            
                            const row = document.createElement('div');
                            row.className = 'flex items-center justify-between p-3 rounded bg-black/20 border border-white/5 hover:bg-white/5 transition-colors';
                            row.innerHTML = `
                                <div class="flex items-center gap-3">
                                    <span class="material-symbols-outlined text-[16px] text-slate-500">${icon}</span>
                                    <span class="text-xs text-slate-300 font-medium">${evt.Reason}</span>
                                </div>
                                <div class="font-mono font-bold text-sm ${colorClass}">${sign}${evt.Amount}</div>
                            `;
                            eventLogContainer.appendChild(row);
                        });
                    } else {
                        eventLogContainer.innerHTML = '<div class="text-xs text-slate-500 italic p-3 text-center">No recorded events for this flight.</div>';
                    }
                }

                if (window.generateChiefPilotDebrief) {
                    const lang = localStorage.getItem('selLanguage') || 'en';
                    document.getElementById('frChiefPilotSpeech').innerHTML = window.generateChiefPilotDebrief(rep, lang);
                }

                document.getElementById('flightReportModal').style.display = 'flex';
                break;
            case 'showGroundEvent':
                const geModal = document.getElementById('groundEventModal');
                const geTitle = document.getElementById('geTitle');
                const geDesc = document.getElementById('geDesc');
                const geChoices = document.getElementById('geChoices');
                
                if (geModal && payload.eventData) {
                    const evt = payload.eventData;
                    geTitle.innerText = evt.title;
                    geDesc.innerText = evt.description;
                    geChoices.innerHTML = '';
                    
                    if (evt.choices) {
                        evt.choices.forEach(c => {
                            const btn = document.createElement('button');
                            btn.className = 'w-full py-3 px-4 rounded-xl font-bold uppercase tracking-widest text-[11px] transition-all shadow-lg flex items-center justify-left gap-3 ';
                            
                            if (c.colorClass === 'success') {
                                btn.className += 'bg-emerald-500/10 text-emerald-400 border border-emerald-500/30 hover:bg-emerald-500 hover:text-slate-900 shadow-[0_0_15px_rgba(16,185,129,0.1)]';
                                btn.innerHTML = `<span class="material-symbols-outlined text-[18px]">check_circle</span> ${c.text}`;
                            } else if (c.colorClass === 'error') {
                                btn.className += 'bg-red-500/10 text-red-400 border border-red-500/30 hover:bg-red-500 hover:text-white shadow-[0_0_15px_rgba(239,68,68,0.1)]';
                                btn.innerHTML = `<span class="material-symbols-outlined text-[18px]">warning</span> ${c.text}`;
                            } else {
                                btn.className += 'bg-[#12141A] text-slate-300 border border-white/10 hover:bg-white/10 hover:text-white';
                                btn.innerText = c.text;
                            }
                            
                            btn.addEventListener('click', () => {
                                window.chrome.webview.postMessage({ action: 'resolveGroundEvent', eventId: evt.id, choiceId: c.id });
                                geModal.style.display = 'none';
                            });
                            
                            geChoices.appendChild(btn);
                        });
                    }
                    geModal.style.display = 'flex';
                }
                break;
            case 'penalty':
            case 'log':
                const log1 = document.getElementById('penaltyLogs');
                const log2 = document.getElementById('liveScoreLog');
                
                let logColor = '#cbd5e1';
                if (payload.type === 'penalty') {
                    logColor = (payload.message.includes('(+') || payload.message.includes('Bonus') || payload.message.includes('Parfait')) ? '#34D399' : '#F87171';
                }

                if (log1) {
                    const li = document.createElement('li');
                    li.innerText = `[${window.getLocalFormattedTime()}] ${payload.message}`;
                    li.style.color = logColor;
                    li.style.marginBottom = '5px';
                    log1.prepend(li);
                }
                
                if (log2) {
                    const placeholder = log2.querySelector('li.text-center');
                    if (placeholder) placeholder.remove();
                    const li2 = document.createElement('li');
                    li2.innerText = `[${window.getLocalFormattedTime()}] ${payload.message}`;
                    li2.style.color = logColor;
                    li2.style.fontSize = '11px';
                    li2.style.borderLeft = `2px solid ${logColor}`;
                    li2.style.paddingLeft = '6px';
                    li2.style.marginBottom = '4px';
                    log2.prepend(li2);
                }
                break;
            case 'cabinLog':
                const clog = document.getElementById('cabinLogsList');
                if (clog) {
                    if (clog.children.length === 1 && clog.children[0].innerText.includes('Standing by')) clog.innerHTML = '';
                    const cli = document.createElement('li');
                    cli.innerText = `[${window.getLocalFormattedTime()}] ${payload.message}`;
                    if (payload.level === 'red') cli.style.color = '#EF4444';
                    else if (payload.level === 'orange') cli.style.color = '#F59E0B';
                    else cli.style.color = '#38BDF8';
                    cli.style.marginBottom = '5px';
                    cli.style.borderBottom = '1px solid rgba(255,255,255,0.05)';
                    cli.style.paddingBottom = '3px';
                    clog.prepend(cli);
                    if (clog.children.length > 3) clog.removeChild(clog.lastChild);
                }
                break;
            case 'InitProfile':
                const profile = payload;
                if (profile) {
                    // DEBUG: Log the profile object to the terminal/console to see what it contains
                    console.log("[DEBUG] InitProfile received:", JSON.stringify(profile));

                    // Update Sidebar
                    const sbCallsign = document.getElementById('sbProfileCallsign');
                    if (sbCallsign) sbCallsign.innerText = profile.callSign || profile.CallSign || 'MAVERICK';
                    const sbRank = document.getElementById('sbProfileRank');
                    if (sbRank) sbRank.innerText = profile.calculatedRank || profile.CalculatedRank || 'Trainee';

                    // Fetch Avatar from local virtual host to bypass IPC limits
                    fetch('http://fsv.local/ProfileAvatar.b64', { cache: 'no-store' })
                        .then(r => r.ok ? r.text() : null)
                        .then(b64 => {
                            if (b64 && b64.startsWith('data:image')) {
                                const pos = profile.avatarPosition || profile.AvatarPosition || "50% 50%";
                                const sbImg = document.getElementById('sbProfileImg');
                                if (sbImg) {
                                    sbImg.src = b64;
                                    sbImg.style.objectPosition = pos;
                                    sbImg.classList.remove('hidden');
                                    document.getElementById('sbProfileIcon').classList.add('hidden');
                                }
                                const bigImg = document.getElementById('prfBigAvatar');
                                if (bigImg) {
                                    bigImg.src = b64;
                                    bigImg.style.objectPosition = pos;
                                    bigImg.classList.remove('hidden');
                                    document.getElementById('prfBigIcon').classList.add('hidden');
                                }
                            }
                        })
                        .catch(err => console.log('Avatar not found or not set', err));

                    // Update Main Profile Page
                    const prfCallsign = document.getElementById('prfCallsign');
                    if (prfCallsign) prfCallsign.innerText = profile.callSign || profile.CallSign || 'MAVERICK';
                    
                    const fName = profile.firstName || profile.FirstName || 'John';
                    const lName = profile.lastName || profile.LastName || 'Doe';
                    const prfFullName = document.getElementById('prfFullName');
                    if (prfFullName) prfFullName.innerText = `${fName} ${lName}`;
                    
                    const prfHomeBase = document.getElementById('prfHomeBase');
                    if (prfHomeBase) prfHomeBase.innerText = profile.homeBaseIcao || profile.HomeBaseIcao || 'LFPG';
                    
                    const prfCountry = document.getElementById('prfCountry');
                    if (prfCountry) prfCountry.innerText = profile.countryCode || profile.CountryCode || 'FR';
                    
                    const prfRankBadge = document.getElementById('prfRankBadge');
                    if (prfRankBadge) prfRankBadge.innerText = profile.calculatedRank || profile.CalculatedRank || 'Trainee';

                    // Formatting Helper
                    const formatTime = (totalMins) => {
                        const h = Math.floor(Math.abs(totalMins) / 60);
                        const m = Math.floor(Math.abs(totalMins) % 60);
                        return `${h}h ${m}m`;
                    };

                    const prfTotalTime = document.getElementById('prfTotalTime');
                    const totalMins = profile.totalBlockTimeMinutes ?? profile.TotalBlockTimeMinutes ?? 0;
                    if (prfTotalTime) prfTotalTime.innerText = formatTime(totalMins);

                    const prfTotalFlights = document.getElementById('prfTotalFlights');
                    if (prfTotalFlights) prfTotalFlights.innerText = profile.totalFlights ?? profile.TotalFlights ?? 0;
                    
                    const prfSuperScore = document.getElementById('prfSuperScore');
                    if (prfSuperScore) prfSuperScore.innerText = Math.round(profile.averageSuperScore ?? profile.AverageSuperScore ?? 0);
                    
                    const prfHighestScore = document.getElementById('prfHighestScore');
                    if (prfHighestScore) prfHighestScore.innerText = Math.round(profile.highestSuperScore ?? profile.HighestSuperScore ?? 0);

                    const prfPunctuality = document.getElementById('prfPunctuality');
                    if (prfPunctuality) prfPunctuality.innerText = `${Math.round(profile.punctualityRatingPercentage ?? profile.PunctualityRatingPercentage ?? 0)}%`;
                    
                    const prfTouchdown = document.getElementById('prfTouchdown');
                    const tdFpm = profile.smoothestTouchdownFpm ?? profile.SmoothestTouchdownFpm ?? 0;
                    if (prfTouchdown) prfTouchdown.innerText = tdFpm === 0 ? "--- fpm" : `${Math.round(tdFpm)} fpm`;
                    
                    const prfManualTime = document.getElementById('prfManualTime');
                    if (prfManualTime) prfManualTime.innerText = formatTime(profile.ManualFlyingTimeMinutes || 0);

                    const prfGoArounds = document.getElementById('prfGoArounds');
                    if (prfGoArounds) prfGoArounds.innerText = profile.TotalGoArounds || 0;

                    const prfDiversions = document.getElementById('prfDiversions');
                    if (prfDiversions) prfDiversions.innerText = profile.TotalDiversions || 0;

                    // WALL OF FAME RENDERING
                    const badgeDefs = [
                        { id: "first_entry", title: "First Entry", icon: "menu_book", color: "text-sky-400", bg: "bg-sky-500/10", border: "border-sky-500/30" },
                        { id: "butter_bread", title: "Butter the Bread", icon: "flight_land", color: "text-sky-400", bg: "bg-sky-500/10", border: "border-sky-500/30" },
                        { id: "swiss_watch", title: "Swiss Watch", icon: "schedule", color: "text-sky-400", bg: "bg-sky-500/10", border: "border-sky-500/30" },
                        { id: "by_the_book", title: "By the Book", icon: "checklist_rtl", color: "text-sky-400", bg: "bg-sky-500/10", border: "border-sky-500/30" },
                        { id: "frequent_flyer", title: "Frequent Flyer", icon: "military_tech", color: "text-amber-400", bg: "bg-amber-500/10", border: "border-amber-500/30" },
                        { id: "hand_of_god", title: "The Hand of God", icon: "front_hand", color: "text-amber-400", bg: "bg-amber-500/10", border: "border-amber-500/30" },
                        { id: "company_man", title: "Company Man", icon: "work", color: "text-amber-400", bg: "bg-amber-500/10", border: "border-amber-500/30" },
                        { id: "safe_and_sound", title: "Safe and Sound", icon: "verified_user", color: "text-amber-400", bg: "bg-amber-500/10", border: "border-amber-500/30" },
                        { id: "go_around_flaps3", title: "Go-Around, Flaps 3", icon: "autorenew", color: "text-amber-400", bg: "bg-amber-500/10", border: "border-amber-500/30" },
                        { id: "flawless_execution", title: "Flawless Execution", icon: "workspace_premium", color: "text-purple-400", bg: "bg-purple-500/10", border: "border-purple-500/30" },
                        { id: "through_storm", title: "Through the Storm", icon: "storm", color: "text-purple-400", bg: "bg-purple-500/10", border: "border-purple-500/30" },
                        { id: "feather_touch", title: "Feather Touch", icon: "airline_seat_flat", color: "text-purple-400", bg: "bg-purple-500/10", border: "border-purple-500/30" },
                        { id: "iron_bladder", title: "Iron Bladder", icon: "local_cafe", color: "text-purple-400", bg: "bg-purple-500/10", border: "border-purple-500/30" },
                        { id: "airmanship_master", title: "Airmanship Master", icon: "rocket_launch", color: "text-purple-400", bg: "bg-purple-500/10", border: "border-purple-500/30" },
                        { id: "spine_crusher", title: "Spine Crusher", icon: "personal_injury", color: "text-red-500", bg: "bg-red-500/10", border: "border-red-500/30" },
                        { id: "no_coffee", title: "Coffee Machine is Broken", icon: "no_drinks", color: "text-red-500", bg: "bg-red-500/10", border: "border-red-500/30" },
                        { id: "pitch_black", title: "Pitch Black", icon: "dark_mode", color: "text-red-500", bg: "bg-red-500/10", border: "border-red-500/30" }
                    ];

                    const badgesGrid = document.getElementById('prfBadgesGrid');
                    if (badgesGrid && profile.UnlockedAchievements) {
                        badgesGrid.innerHTML = '';
                        badgeDefs.forEach(b => {
                            const isUnlocked = profile.UnlockedAchievements.includes(b.id);
                            const html = `
                                <div class="flex flex-col items-center p-3 rounded-xl border ${isUnlocked ? b.border : 'border-white/5'} ${isUnlocked ? b.bg : 'bg-black/20'} transition-all ${isUnlocked ? '' : 'opacity-40 grayscale'} hover:grayscale-0 hover:opacity-100" title="${b.title}">
                                    <span class="material-symbols-outlined text-[32px] mb-2 ${isUnlocked ? b.color : 'text-slate-500'} drop-shadow-lg">${b.icon}</span>
                                    <span class="text-[10px] font-bold tracking-widest uppercase text-center ${isUnlocked ? 'text-white' : 'text-slate-500'}">${b.title}</span>
                                </div>
                            `;
                            badgesGrid.innerHTML += html;
                        });
                    }
                }
                break;
            case 'scoreUpdate':
                if (isFlightCancelled) return;
                let topScore = document.getElementById('topScoreValue');
                if (topScore) topScore.innerText = payload.score;
                const mainScore = document.getElementById('mainScoreValue');
                if (mainScore) mainScore.innerText = payload.score;
                
                if (payload.delta !== 0) {
                    mainScore.classList.remove('text-emerald-400', 'text-red-400');
                    mainScore.classList.add(payload.delta > 0 ? 'text-emerald-400' : 'text-red-400');
                    setTimeout(() => {
                        if (isFlightCancelled) return;
                        mainScore.classList.remove('text-red-400');
                        mainScore.classList.add('text-emerald-400');
                    }, 1000);

                    const feed = document.getElementById('scoreFeed');
                    if (feed) {
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
                    }

                    const plog = document.getElementById('penaltyLogs');
                    if (plog) {
                        const logLi = document.createElement('li');
                        logLi.innerText = `[${window.getLocalFormattedTime()}] ${payload.msg} (Total: ${payload.score})`;
                        logLi.style.color = payload.delta > 0 ? '#A7F3D0' : '#FCA5A5';
                        logLi.style.marginBottom = '5px';
                        plog.prepend(logLi);
                    }
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
                    window.isFlightActive = true;
                    const langFetch = (localStorage.getItem('selLanguage') || 'EN').toLowerCase();
                    const dictFetch = window.locales ? window.locales[langFetch] : null;

                    const btnFetchLabel = document.getElementById('btnFetchPlanLabel');
                    if (btnFetchLabel) btnFetchLabel.innerText = dictFetch ? dictFetch.modal_cancel_yes : 'CANCEL FLIGHT';
                    const btnFetch = document.getElementById('btnFetchPlan');
                    if (btnFetch) {
                        btnFetch.querySelector('.material-symbols-outlined').innerText = 'cancel';
                    }
                    const phaseEl = document.getElementById('flightPhase');
                    const mScore = document.getElementById('mainScoreValue');
                    if (mScore) {
                        mScore.innerText = "1000";
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
                    startBtn.disabled = false;
                    startBtn.innerHTML = '<span class="material-symbols-outlined text-[18px]">flight_takeoff</span> START OPS';
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
                window.isFlightActive = false;
                const langCancel = (localStorage.getItem('selLanguage') || 'EN').toLowerCase();
                const dictCancel = window.locales ? window.locales[langCancel] : null;

                const btnFetchLabel2 = document.getElementById('btnFetchPlanLabel');
                if (btnFetchLabel2) btnFetchLabel2.innerText = dictCancel ? dictCancel.btn_fetch_plan : 'FETCH PLAN';
                const btnFetch2 = document.getElementById('btnFetchPlan');
                if (btnFetch2) {
                    btnFetch2.querySelector('.material-symbols-outlined').innerText = 'cloud_download';
                }
                const phaseEl = document.getElementById('flightPhase');
                if (phaseEl) {
                    phaseEl.innerText = "Aborted";
                    phaseEl.style.color = "#DC2626";
                    phaseEl.style.textShadow = "0 0 20px rgba(220, 38, 38, 0.8)";
                }
                const mScore = document.getElementById('mainScoreValue');
                if (mScore) {
                    mScore.innerText = "CANCELED";
                    mScore.classList.add('text-red-500');
                } 
                const mBar = document.getElementById('dashMetaBar');
                if (mBar) {
                    const mText = document.getElementById('dashMetaText');
                    const mFill = document.getElementById('dashMetaFill');
                    if (mText) { mText.innerText = dictCancel ? (dictCancel.gops_meta_aborted || "OPS ABORTED") : "OPS ABORTED"; mText.style.color = "#DC2626"; }
                    if (mFill) mFill.style.backgroundColor = "#DC2626";
                }
                if (btnStartGroundOps) {
                    btnStartGroundOps.disabled = true;
                    btnStartGroundOps.innerHTML = '<span class="material-symbols-outlined text-[18px]">flight_takeoff</span> START OPS';
                }
                break;
            case 'flightData':
                const d = payload.data;
                
                if (document.getElementById('dashMetaBar')) document.getElementById('dashMetaBar').style.display = 'block';
                if (document.getElementById('flightBreakdown')) document.getElementById('flightBreakdown').style.display = 'grid';

                const timeStr = window.getFormattedTime;

                const AIRLINES = {
                    'AFR': 'Air France', 'BAW': 'British Airways', 'EZY': 'easyJet', 'RYR': 'Ryanair', 
                    'DLH': 'Lufthansa', 'UAE': 'Emirates', 'QTR': 'Qatar Airways', 'DAL': 'Delta',
                    'AAL': 'American Airlines', 'UAL': 'United', 'SWA': 'Southwest'
                };

                if (d.general) {
                    let acode = d.general.icao_airline || 'ZZZ';
                    let iata = d.general.iata_airline || '';
                    let fnum = d.general.flight_number || '';
                    
                    let pureAirlineName = AIRLINES[acode] ? AIRLINES[acode] : (d.general.airline_name || acode);
                    
                    let flightIdent = iata ? `${acode}/${iata}${fnum}` : `${acode}/${fnum}`;
                    
                    if (document.getElementById('bdAirline')) document.getElementById('bdAirline').innerText = flightIdent;
                    if (document.getElementById('bdAirline2')) document.getElementById('bdAirline2').innerText = pureAirlineName;
                    
                    if (document.getElementById('bdFlightNum')) document.getElementById('bdFlightNum').innerText = pureAirlineName;
                    if (document.getElementById('bdFlightNum2')) document.getElementById('bdFlightNum2').innerText = flightIdent;
                    
                    let flightLevel = d.general.initial_alt || d.general.initial_altitude || '';
                    let stepclimb = d.general.stepclimb_string || '';
                    
                    if (!flightLevel && stepclimb) {
                        const parts = stepclimb.split('/');
                        if (parts.length > 1) flightLevel = parts[parts.length - 1]; // e.g., "0360"
                    }

                    if (flightLevel) {
                        flightLevel = flightLevel.replace(/^0+/, ''); // "0360" -> "360"
                        if (flightLevel.length === 5 && flightLevel.endsWith('00')) flightLevel = flightLevel.substring(0, 3); // "37000" -> "370"
                    }

                    if (stepclimb) {
                        stepclimb = stepclimb.split('/').map(s => {
                            if (s.length === 4 && s.startsWith('0') && !isNaN(s)) return s.substring(1);
                            return s;
                        }).join('/');
                    }
                    if (document.getElementById('bdCruise')) document.getElementById('bdCruise').innerText = `FL${flightLevel}`;

                    let cruiseStr = flightLevel ? 'FL' + flightLevel : 'N/A';
                    if (stepclimb && stepclimb !== `${d.origin?.icao_code || ''}/${flightLevel}`) {
                        cruiseStr += ` (Steps: ${stepclimb})`;
                    }
                    if (document.getElementById('bdCruise')) document.getElementById('bdCruise').innerText = cruiseStr;
                    if (document.getElementById('bdCruise2')) document.getElementById('bdCruise2').innerText = cruiseStr;

                    let routeStr = d.general.route || '';
                    if (flightLevel) routeStr += ` (FL${flightLevel})`;
                    if (document.getElementById('bdRoute')) {
                        document.getElementById('bdRoute').innerText = routeStr;
                        document.getElementById('bdRoute').title = routeStr;
                    }
                    if (document.getElementById('bdRoute2')) {
                        document.getElementById('bdRoute2').innerText = routeStr;
                        document.getElementById('bdRoute2').title = routeStr;
                    }

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
                    
                    pureAirlineName = AIRLINES[acode] ? AIRLINES[acode] : (d.general.airline_name || acode || 'Unknown');
                    if (dhAirline) dhAirline.innerText = pureAirlineName;
                }
                if (d.aircraft) {
                    let dispName = d.aircraft.name || d.aircraft.base_type || 'Unknown';
                    if (d.aircraft.reg && !dispName.includes(d.aircraft.reg)) {
                        dispName = `${d.aircraft.reg} - ${dispName}`;
                    }
                    if (d.aircraft.name && d.aircraft.name.includes('|')) {
                        // User's custom format typically uses dashes instead of pipes in SimBrief JSON, but we will accept whatever name is provided!
                        dispName = d.aircraft.name;
                    }
                    if (document.getElementById('bdAircraft')) document.getElementById('bdAircraft').innerText = dispName;
                }
                if (d.general && document.getElementById('bdDistance')) {
                    document.getElementById('bdDistance').innerText = d.general.route_distance || '---';
                }
                if (d.origin) document.getElementById('bdOrigin').innerText = d.origin.iata_code || d.origin.icao_code || '---';
                if (d.destination) document.getElementById('bdDest').innerText = d.destination.iata_code || d.destination.icao_code || '---';

                if (d.times) {
                    currentSobtUnix = parseInt(d.times.sched_out || '0');
                    if (document.getElementById('bdSobt')) document.getElementById('bdSobt').innerText = timeStr(currentSobtUnix);
                    if (document.getElementById('bdSobt2')) document.getElementById('bdSobt2').innerText = timeStr(currentSobtUnix);
                    window.currentSibtUnix = parseInt(d.times.sched_in || '0');
                    if (document.getElementById('bdSibt')) document.getElementById('bdSibt').innerText = timeStr(window.currentSibtUnix);
                    if (document.getElementById('bdSibt2')) document.getElementById('bdSibt2').innerText = timeStr(window.currentSibtUnix);
                    let eteSec = parseInt(d.times.est_time_enroute || '0');
                    let h = Math.floor(eteSec / 3600);
                    let m = Math.floor((eteSec % 3600) / 60);
                    if (document.getElementById('bdEte')) document.getElementById('bdEte').innerText = `${h.toString().padStart(2, '0')}:${m.toString().padStart(2, '0')}`;
                    if (document.getElementById('bdEte2')) document.getElementById('bdEte2').innerText = `${h.toString().padStart(2, '0')}:${m.toString().padStart(2, '0')}`;
                }

                if (d.weights && d.params) {
                    if (document.getElementById('bdPax')) document.getElementById('bdPax').innerText = d.weights.pax_count || '';
                    if (document.getElementById('bdPax2')) document.getElementById('bdPax2').innerText = d.weights.pax_count || '';
                    
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

                    if (document.getElementById('bdZfw')) document.getElementById('bdZfw').innerText = convertWeight(d.weights.est_zfw) + ' ' + uiWeightUnit;
                    if (document.getElementById('bdZfw2')) document.getElementById('bdZfw2').innerText = convertWeight(d.weights.est_zfw) + ' ' + uiWeightUnit;
                    if (document.getElementById('bdTow')) document.getElementById('bdTow').innerText = convertWeight(d.weights.est_tow || d.weights.est_ldw) + ' ' + uiWeightUnit;
                    if (document.getElementById('bdTow2')) document.getElementById('bdTow2').innerText = convertWeight(d.weights.est_tow || d.weights.est_ldw) + ' ' + uiWeightUnit;
                    let fuel = d.fuel?.plan_ramp || d.weights.est_block || d.weights.block_fuel || '';
                    if (document.getElementById('bdFuel')) document.getElementById('bdFuel').innerText = convertWeight(fuel) + ' ' + uiWeightUnit;
                }

                window.parseBriefing = (data) => {
                    if (!data) return '';
                    if (typeof data === 'string') return "<i>Updating format...</i>";
                    
                    let html = '';
                    
                    if (data.HeaderText) {
                        html += `<div class="mb-6 italic text-amber-400 font-semibold text-sm px-2 text-center border-b border-amber-900/30 pb-4">${data.HeaderText}</div>`;
                    }

                    html += '<div class="grid grid-cols-1 lg:grid-cols-2 gap-4">';
                    
                    if (data.Stations && Array.isArray(data.Stations)) {
                        data.Stations.forEach(station => {
                            let icon = "📋";
                            if (station.Id.toLowerCase() === "origin") icon = "🛫";
                            else if (station.Id.toLowerCase() === "destination") icon = "🛬";
                            else if (station.Id.toLowerCase() === "alternate") icon = "🔄";
                            
                            let variablesHtml = '';
                            const addPill = (label, value, colorClass) => {
                                if(value && value.trim() !== '') {
                                    variablesHtml += `
                                        <div class="flex flex-col bg-black/40 rounded border border-white/5 p-2 justify-center items-center text-center">
                                            <span class="text-[9px] uppercase tracking-wider text-slate-500 mb-1">${label}</span>
                                            <span class="font-bold text-xs ${colorClass}">${value}</span>
                                        </div>
                                    `;
                                }
                            };

                            addPill('QNH', station.Qnh, 'text-purple-400');
                            addPill('Wind', station.Wind, 'text-emerald-400');
                            addPill('Temp/Dew', station.TempDew, 'text-amber-400');
                            addPill('Visibility', station.Visibility, 'text-sky-400');

                            html += `
                            <div class="bg-[#12141A] p-5 rounded-xl border border-white/5 flex flex-col gap-4 shadow-xl">
                                <div class="flex items-center justify-between border-b border-white/5 pb-2">
                                    <div class="flex items-center gap-2">
                                        <span class="text-slate-400 text-lg">${icon}</span>
                                        <h4 class="text-slate-200 font-bold uppercase tracking-widest text-xs m-0">${station.Label}</h4>
                                    </div>
                                </div>
                                
                                <div class="bg-black/60 rounded p-3 font-mono text-[11px] text-slate-400 leading-relaxed border border-white/5">
                                    <span class="block mb-2 text-white/90 break-words">${station.RawMetar || 'NO METAR'}</span>
                                    <span class="block break-words">${station.RawTaf || 'NO TAF'}</span>
                                </div>
                                
                                ${variablesHtml !== '' ? `<div class="grid grid-cols-2 xl:grid-cols-4 gap-2 mt-2">${variablesHtml}</div>` : ''}
                                
                                ${station.Commentary ? `
                                <div class="mt-4 text-sky-200/90 italic font-serif text-[13px] border-l-2 border-sky-500/50 pl-4 py-2 bg-sky-900/10 rounded-r-lg leading-relaxed">
                                    "${station.Commentary}"
                                </div>` : ''}
                                
                                ${station.RunwayAdvice ? `
                                <div class="mt-2 text-emerald-400/90 font-medium text-[11px] flex items-center gap-2 bg-emerald-900/10 p-2 rounded border border-emerald-500/10">
                                    <span>🛣️</span>
                                    ${station.RunwayAdvice}
                                </div>` : ''}

                                ${station.Notams && station.Notams.trim() !== '' ? `
                                <div class="mt-4 pt-4 border-t border-red-500/20 bg-red-900/5 p-3 rounded">
                                    <h5 class="text-red-400/90 uppercase text-[9px] font-bold tracking-widest mb-2 flex items-center gap-1"><span class="text-red-500">⚠</span> NOTAMS & ALERTS</h5>
                                    <div class="text-red-200/80 text-[10px] whitespace-pre-wrap leading-relaxed">${station.Notams}</div>
                                </div>` : ''}
                            </div>`;
                        });
                    }
                    html += '</div>';

                    if (data.EnrouteText) {
                        html += `
                        <div class="mt-6 bg-[#12141A] p-5 rounded-xl border border-white/5 shadow-xl">
                            <div class="flex items-center gap-2 mb-4 border-b border-white/5 pb-2">
                                <span class="text-slate-400 text-lg">✈️</span>
                                <h4 class="text-slate-200 font-bold uppercase tracking-widest text-xs m-0">EN ROUTE & OPERATIONS</h4>
                            </div>
                            <div class="text-slate-300 leading-relaxed font-body text-xs whitespace-pre-wrap">
                                ${data.EnrouteText}
                            </div>
                        </div>`;
                    }

                    return html;
                };

                const briefingElem = document.getElementById('briefingContent');
                if (briefingElem) {
                    briefingElem.style.whiteSpace = 'normal'; // Reset from pre-wrap
                    briefingElem.innerHTML = window.parseBriefing(payload.briefing);
                }
                
                if (window.renderManifest && payload.manifest) window.renderManifest(payload.manifest);

                if (d.weather) {
                    let wTxt = `Origin METAR: ${d.weather.orig_metar || ''}\nOrigin TAF: ${d.weather.orig_taf || ''}\n\n`;
                    wTxt += `Dest METAR: ${d.weather.dest_metar || ''}\nDest TAF: ${d.weather.dest_taf || ''}\n\n`;
                    
                    const formatArr = (arr) => Array.isArray(arr) ? arr.join('\n') : (arr || '');
                    if (d.weather.altn_metar) wTxt += `Altn METAR(s):\n${formatArr(d.weather.altn_metar)}\n\n`;
                    if (d.weather.enrt_metar) wTxt += `Enroute METAR(s):\n${formatArr(d.weather.enrt_metar)}\n\n`;
                    
                    const weatherElem = document.getElementById('weatherContent');
                    if (weatherElem) weatherElem.innerText = wTxt.trim();
                }
                break;
            case 'groundOps':
                renderGroundOps(payload.services);
                updateMetaBar(payload.services);
                break;
            case 'groundOpsComplete':
                document.getElementById('groundOpsContainer').innerHTML = '<p style="color:#34D399; font-weight:bold;">All ground operations are complete. Aircraft is secure.</p>';
                break;
            case 'groundOpsProgress':
                const globalC = document.getElementById('globalProgressContainer');
                const globalBar = document.getElementById('globalProgressBar');
                const botC = document.getElementById('groundOpsBottomProgress');
                const botBar = document.getElementById('groundOpsProgressBar');
                const statusTxt = document.getElementById('groundOpsStatusText');
                const timeTxt = document.getElementById('groundOpsTimeText');

                if (payload.isActive && payload.pct >= 0 && payload.pct <= 100) {
                    if (globalC) globalC.style.display = 'block';
                    if (botC) botC.style.display = 'flex';
                    
                    if (globalBar) globalBar.style.width = payload.pct + '%';
                    if (botBar) botBar.style.width = payload.pct + '%';
                    
                    if (statusTxt) statusTxt.innerText = payload.status || 'IN PROGRESS';
                    if (timeTxt) timeTxt.innerText = payload.timeString || '';
                } else {
                    if (globalC) globalC.style.display = 'none';
                    if (botC) botC.style.display = 'none';
                }
                break;
        }
    });

});

window.closedAccordions = window.closedAccordions || new Set();

const GO_ICONS = {
    'Refuel': '<span class="material-symbols-outlined text-[18px] text-orange-500">local_gas_station</span>',
    'Boarding': '<span class="material-symbols-outlined text-[18px] text-sky-400">group</span>',
    'Cargo': '<span class="material-symbols-outlined text-[18px] text-amber-500">luggage</span>',
    'Catering': '<span class="material-symbols-outlined text-[18px] text-pink-400">restaurant</span>',
    'Cleaning': '<span class="material-symbols-outlined text-[18px] text-fuchsia-400">cleaning_services</span>',
    'Water/Waste': '<span class="material-symbols-outlined text-[18px] text-emerald-400">water_drop</span>'
};

const GO_NARRATIVES = {
    'Refuel': 'gops_desc_refuel',
    'Boarding': 'gops_desc_boarding',
    'Cargo': 'gops_desc_cargo',
    'Catering': 'gops_desc_catering',
    'Cleaning': 'gops_desc_cleaning',
    'Water/Waste': 'gops_desc_water'
};

window.toggleAccordion = function(name) {
    if (window.closedAccordions.has(name)) window.closedAccordions.delete(name);
    else window.closedAccordions.add(name);
    
    const safeName = name.replace(/\s|[^\w]/g, '');
    const content = document.getElementById('acc-content-' + safeName);
    if (content) content.style.display = window.closedAccordions.has(name) ? 'none' : 'block';
    const chevron = document.getElementById('acc-icon-' + safeName);
    if (chevron) chevron.style.transform = window.closedAccordions.has(name) ? 'rotate(0deg)' : 'rotate(180deg)';
};

window.groundServiceStates = window.groundServiceStates || {};
window.recentlyCompleted = window.recentlyCompleted || null;
window.recentlyCompletedTime = window.recentlyCompletedTime || 0;

function updateMetaBar(services) {
    if (!window.isFlightActive) return;
    if (!services || services.length === 0) return;

    let totalDuration = 0;
    let elapsedDuration = 0;
    let isActive = false;
    let isFinished = true;
    let blockingService = null;
    let maxDelaySec = -1;

    const mLoc = (localStorage.getItem('selLanguage') || 'EN').toLowerCase();
    const mDict = window.locales && window.locales[mLoc] ? window.locales[mLoc] : window.locales.en;

    const metaFill = document.getElementById('dashMetaFill');
    const metaText = document.getElementById('dashMetaText');

    function getSeverityColor(sec) {
        if (sec < 180) return '#eab308'; // Jaune (< 3 min)
        if (sec <= 600) return '#f97316'; // Orange (3 - 10 min)
        return '#ef4444'; // Rouge (> 10 min)
    }

    services.forEach(s => {
        let duration = s.TotalDurationSec + s.DelayAddedSec;
        totalDuration += duration;
        elapsedDuration += Math.min(s.ElapsedSec, duration);
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

    let percent = totalDuration > 0 ? (elapsedDuration / totalDuration) * 100 : 0;
    
    const metaBar = document.getElementById('dashMetaBar');

    if (!metaBar) return;
    if (metaBar.style.display === 'none') metaBar.style.display = 'block';

    if (metaFill) metaFill.style.width = percent + '%';
    if (metaText) {
        if (isFinished && totalDuration > 0) {
            metaText.innerText = mDict.gops_meta_completed || "Ground Operations Completed";
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
            metaText.innerHTML = `${icon} <span style="font-weight:bold">${window.recentlyCompleted.Name.toUpperCase()} TERMINÉ</span>`;
            metaText.style.color = "#34D399";
            if (metaFill) metaFill.style.backgroundColor = "#34D399";
        } else if (isActive) {
            metaText.innerText = `${mDict.gops_meta_progress || "Ground Operations in Progress"} (${Math.round(percent)}%)`;
            metaText.style.color = "#FACC15";
            if (metaFill) metaFill.style.backgroundColor = "#4A90E2";
        } else {
            metaText.innerText = mDict.gops_meta_standby || "Ground Operations Standing By";
            metaText.style.color = "#94A3B8";
            if (metaFill) metaFill.style.backgroundColor = "#334155";
        }
    }
}

function renderGroundOps(services) {
    const container = document.getElementById('groundOpsContainer');
    let html = '<div class="grid grid-cols-1 md:grid-cols-2 gap-4">';
    
    services.forEach(s => {
        const mLang = (localStorage.getItem('selLanguage') || 'EN').toLowerCase();
        const mDict = window.locales && window.locales[mLang] ? window.locales[mLang] : window.locales.en;

        let locName = s.Name;
        if (locName === "Refueling") locName = mDict.gops_refueling || locName;
        else if (locName === "Boarding") locName = mDict.gops_boarding || locName;
        else if (locName === "Cargo") locName = mDict.gops_cargo || locName;
        else if (locName === "Catering") locName = mDict.gops_catering || locName;
        else if (locName === "Cleaning") locName = mDict.gops_cleaning || locName;
        else if (locName === "Water/Waste") locName = mDict.gops_water || locName;

        let locStatus = s.StatusMessage;
        if (locStatus === "In Progress") locStatus = mDict.gops_stat_prog || locStatus;
        else if (locStatus === "Completed") locStatus = mDict.gops_stat_comp || locStatus;
        else if (locStatus === "Skipped by Capt.") locStatus = mDict.gops_stat_skip || locStatus;

        let btnSkipText = "Skip / Abort";
        btnSkipText = mDict.gops_skip_abort || btnSkipText;

        let btnHtml = '';
        if (s.State === 5 && s.Name === "Refueling") {
            btnHtml = `<button class="go-btn px-4 py-2 bg-amber-600/20 text-amber-500 hover:bg-amber-600/40 border border-amber-500/30 rounded text-xs tracking-widest uppercase font-bold transition-colors shadow-[0_0_10px_rgba(245,158,11,0.2)]" onclick="window.chrome.webview.postMessage({action: 'startService', service: '${s.Name}'})">Request Fuel Truck</button>`;
        }
        else if (s.IsOptional && s.State !== 3 /* Completed */ && s.State !== 4 /* Skipped */ && s.State !== 0) {
            btnHtml = `<button class="go-btn px-4 py-2 bg-slate-800 text-slate-300 hover:bg-slate-700 hover:text-white rounded text-xs tracking-widest uppercase transition-colors" onclick="skipService('${s.Name}')">${btnSkipText}</button>`;
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
        else if (s.State === 3 /* Completed */) { statusColor = '#34D399'; barColor = '#34D399'; }
        else if (s.State === 4 /* Skipped */) { statusColor = '#F87171'; barColor = '#F87171'; }
        else if (s.State === 0 /* WaitingForAction */) { statusColor = '#FACC15'; barColor = '#334155'; }

        let timeDisplay = '';
        if (s.RemainingSec > 0) {
            const m = Math.floor(s.RemainingSec / 60).toString().padStart(2, '0');
            const sec = (s.RemainingSec % 60).toString().padStart(2, '0');
            timeDisplay = `(-${m}:${sec})`;
        }

        const safeName = s.Name.replace(/\s|[^\w]/g, '');
        const isOpen = !window.closedAccordions.has(s.Name);
        const displayStyle = isOpen ? 'block' : 'none';
        const chevronRot = isOpen ? 'rotate(180deg)' : 'rotate(0deg)';
        const icon = GO_ICONS[s.Name] || '🔹';
        let narrative = 'Ground operation in progress.';
        if (mDict) {
            const key = GO_NARRATIVES[s.Name];
            narrative = key ? mDict[key] : (mDict.gops_desc_generic || narrative);
        } else {
            narrative = GO_NARRATIVES[s.Name] || 'Opération au sol en cours.';
        }

        let statusStateLabel = 'ACTIVE';
        if (s.State === 3) statusStateLabel = 'FINISHED';
        else if (s.State === 4) statusStateLabel = 'ABORTED';
        else if (s.State === 2) statusStateLabel = 'DELAYED';
        else if (s.State === 0) statusStateLabel = 'WAITING FOR ACTION';

        html += `
            <div class="go-accordion bg-[#12141A] rounded-xl border border-white/5 overflow-hidden flex flex-col h-full">
                <div class="go-acc-header p-4 cursor-pointer hover:bg-white/[0.02] flex justify-between items-center transition-colors border-b border-transparent" onclick="toggleAccordion('${s.Name}')">
                    <div class="go-acc-title flex flex-col gap-1">
                        <div class="flex items-center gap-2">
                            <span class="go-icon text-lg flex items-center">${icon}</span>
                            <strong style="color: ${s.State === 3 ? '#34D399' : '#F8FAFC'};" class="font-label tracking-[0.4em] uppercase text-xs">${locName}</strong>
                        </div>
                    </div>
                    <div class="go-acc-summary flex items-center gap-3">
                        <span style="color: ${statusColor}; font-weight: 600;" class="text-[10px] uppercase tracking-widest">${locStatus} ${timeDisplay}</span>
                        <svg id="acc-icon-${safeName}" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="#94A3B8" stroke-width="2" style="transform: ${chevronRot}; transition: transform 0.2s;">
                            <polyline points="6 9 12 15 18 9"></polyline>
                        </svg>
                    </div>
                </div>
                <div class="go-acc-bar w-full h-1 bg-black/40">
                    <div class="go-bar-fill h-full transition-all duration-1000 ease-out" style="width: ${s.ProgressPercent}%; background-color: ${barColor};"></div>
                </div>
                <div id="acc-content-${safeName}" class="go-acc-content p-4 bg-[#0F1116]" style="display: ${displayStyle}; flex-grow: 1;">
                    <p style="color: #cbd5e1; font-size: 13px; margin: 0 0 15px 0; line-height: 1.5; font-style: italic;">"${narrative}"</p>
                    <div style="display: flex; justify-content: space-between; align-items: center; margin-top: auto;">
                        <span style="font-size: 10px; color: #64748b; font-family: monospace;">STATUS: ${statusStateLabel}</span>
                        ${btnHtml}
                    </div>
                </div>
            </div>
        `;
    });
    
    html += '</div>';

    // STORY 29: Global Time Warp Button
    const hasActiveOps = services.some(s => s.State !== 3 && s.State !== 4);
    if (hasActiveOps) {
        let btnLbl = (window.locales && window.locales[(localStorage.getItem('selLanguage') || 'EN').toLowerCase()]?.gops_timewarp) || "Time Warp (Skip to SOBT)";
        let penLbl = (window.locales && window.locales[(localStorage.getItem('selLanguage') || 'EN').toLowerCase()]?.gops_timewarp_penalty) || "-50 SuperScore Penalty";

        html += `
        <div class="mt-8 flex flex-col items-center justify-center">
            <button class="px-8 py-3 bg-rose-600/20 text-rose-400 hover:bg-rose-600/40 hover:text-white border border-rose-500/30 rounded-lg text-sm tracking-widest uppercase font-black transition-all shadow-[0_0_15px_rgba(244,63,94,0.3)] hover:shadow-[0_0_25px_rgba(244,63,94,0.5)] flex items-center gap-3" onclick="window.chrome.webview.postMessage({action: 'requestTimeWarp'})">
                <span class="text-xl">⏩</span>
                ${btnLbl}
            </button>
            <p class="text-rose-500/60 text-[10px] uppercase font-bold tracking-widest mt-3 flex items-center gap-1">
                <span class="text-xs">⚠️</span> ${penLbl}
            </p>
        </div>
        `;
    }

    container.innerHTML = html;
}

// Global skip function for inline onclick
window.currentAbortService = null;
window.skipService = function(name) {
    window.currentAbortService = name;
    document.getElementById('abortServiceName').innerText = name;
    document.getElementById('abortServiceModal').style.display = 'flex';
};

window.renderManifest = function(manifest) {
    const container = document.getElementById('manifestContainer');
    if (!container) return;

    if (!manifest || (!manifest.FlightCrew && !manifest.Passengers)) {
        container.innerHTML = '<p style="color:#64748b;">Waiting for final manifest processing...</p>';
        return;
    }

    if (manifest.Passengers.length === 0) {
        container.innerHTML = '<p style="color:#64748b;">No passengers listed on this flight plan.</p>';
        return;
    }

    let maxRow = 0;
    let hasLettersGHK = false; // check if widebody
    manifest.Passengers.forEach(p => {
        let rowStr = p.Seat.replace(/[^0-9]/g, '');
        let row = parseInt(rowStr);
        if (row > maxRow) maxRow = row;
        if (p.Seat.includes('G') || p.Seat.includes('H') || p.Seat.includes('K')) hasLettersGHK = true;
    });

    // Determine layout mapping
    let lettersLeft = ['A', 'B', 'C'];
    let lettersRight = ['D', 'E', 'F'];
    let lettersCenter = [];

    if (hasLettersGHK) {
        lettersLeft = ['A', 'B', 'C'];
        lettersCenter = ['D', 'E', 'F', 'G'];
        lettersRight = ['H', 'J', 'K']; 
    }

    let seatMapHtml = `
        <style>
            .fuselage {
                width: 100%;
                width: 380px;
                border: 4px solid #334155;
                border-radius: 120px 120px 30px 30px;
                padding: 120px 20px 40px 20px;
                background: linear-gradient(to bottom, #0f172a, #1e293b);
                position: relative;
                margin: 0 auto;
                box-shadow: inset 0 0 25px rgba(0,0,0,0.8);
            }
            .cockpit {
                position: absolute;
                top: 10px;
                left: 50%;
                transform: translateX(-50%);
                width: 80px;
                height: 40px;
                background: linear-gradient(145deg, rgba(56, 189, 248, 0.3), rgba(12, 74, 110, 0.4));
                border-radius: 60px 60px 15px 15px;
                border: 2px solid #475569;
            }
            .seat-row {
                display: flex;
                justify-content: space-between;
                margin-bottom: 6px;
                align-items: center;
                position: relative;
            }
            .row-num {
                position: absolute;
                left: 50%;
                transform: translateX(-50%);
                color: #64748b;
                font-size: 15px;
                font-weight: bold;
            }
            .seat-block {
                display: flex;
                gap: 4px;
            }
            .seat {
                width: 22px;
                height: 26px;
                border-radius: 4px 4px 2px 2px;
                background: #334155;
                border: 1px solid #475569;
                position: relative;
                cursor: default;
                transition: all 0.2s;
            }
            .seat.occupied {
                background: #059669;
                border-color: #34d399;
                box-shadow: inset 0 -4px 0 rgba(0,0,0,0.3);
            }
            .seat.occupied:hover {
                background: #10b981;
                transform: translateY(-2px);
            }
            .seat.occupied .tooltip {
                visibility: hidden;
                background-color: #f8fafc;
                color: #0f172a;
                text-align: center;
                border-radius: 4px;
                padding: 4px 6px;
                position: absolute;
                z-index: 10;
                bottom: 130%;
                left: 50%;
                transform: translateX(-50%);
                font-size: 14px;
                padding: 6px 10px;
                white-space: nowrap;
                opacity: 0;
                transition: opacity 0.2s;
                font-weight: bold;
                box-shadow: 0 4px 6px rgba(0,0,0,0.3);
            }
            .seat.occupied:hover .tooltip {
                visibility: visible;
                opacity: 1;
            }
        </style>
        <div class="fuselage">
            <div class="cockpit"></div>
    `;

    for (let r = 1; r <= maxRow; r++) {
        if (r === 13) continue;
        
        seatMapHtml += `<div class="seat-row">`;
        seatMapHtml += `<div class="row-num">${r}</div>`;
        
        // Left block
        seatMapHtml += `<div class="seat-block">`;
        lettersLeft.forEach(l => {
            let sId = r + l;
            let p = manifest.Passengers.find(x => x.Seat === sId);
            if (p) {
                seatMapHtml += `<div class="seat occupied"><span class="tooltip">${p.Seat} : ${p.Name} (${p.Nationality})</span></div>`;
            } else {
                seatMapHtml += `<div class="seat"></div>`;
            }
        });
        seatMapHtml += `</div>`;
        
        // Center block (widebody only)
        if (lettersCenter.length > 0) {
            seatMapHtml += `<div class="seat-block" style="margin: 0 10px;">`;
            lettersCenter.forEach(l => {
                let sId = r + l;
                let p = manifest.Passengers.find(x => x.Seat === sId);
                if (p) {
                    seatMapHtml += `<div class="seat occupied"><span class="tooltip">${p.Seat} : ${p.Name} (${p.Nationality})</span></div>`;
                } else {
                    seatMapHtml += `<div class="seat"></div>`;
                }
            });
            seatMapHtml += `</div>`;
        }
        
        // Right block
        seatMapHtml += `<div class="seat-block">`;
        lettersRight.forEach(l => {
            let sId = r + l;
            let p = manifest.Passengers.find(x => x.Seat === sId);
            if (p) {
                seatMapHtml += `<div class="seat occupied"><span class="tooltip">${p.Seat} : ${p.Name} (${p.Nationality})</span></div>`;
            } else {
                seatMapHtml += `<div class="seat"></div>`;
            }
        });
        seatMapHtml += `</div>`;
        
        seatMapHtml += `</div>`; // end row
    }
    seatMapHtml += `</div>`; // end fuselage

    const mLang = (localStorage.getItem('selLanguage') || 'EN').toLowerCase();
    const mDict = window.locales && window.locales[mLang] ? window.locales[mLang] : window.locales.en;
    const flightCrewLabel = mDict.crew_flight || "FLIGHT CREW";
    const cabinCrewLabel = mDict.crew_cabin || "CABIN CREW";
    const mapLabel = mDict.seat_map_title || "SEAT MAP";

    let html = `
        <div style="display:flex; gap: 40px; justify-content: space-between; height: 100%;">
            <div style="flex: 1; min-width: 250px; display: flex; flex-direction: column; height: 100%;">
                <div style="flex-shrink: 0;">
                    <h3 class="text-xs font-label tracking-[0.4em] text-sky-400 uppercase opacity-80 border-b border-white/5 pb-3 mb-4">${flightCrewLabel} (${manifest.FlightCrew.length})</h3>
                    <ul style="list-style:none; padding:0; margin:0; line-height: 1.8; color:#cbd5e1; margin-bottom: 20px;">
    `;

    let cabCrewRendered = false;
    manifest.FlightCrew.forEach(c => {
        if (!cabCrewRendered && (c.Role === "Purser" || c.Role === "Flight Attendant")) {
            html += `<li class="text-[10px] font-label tracking-[0.4em] text-sky-400 uppercase opacity-80 border-b border-white/5 pb-2 mb-2 mt-4 border-dotted mt-4">${cabinCrewLabel}</li>`;
            cabCrewRendered = true;
        }

        let displayRole = c.Role;
        if (c.Role.toLowerCase().includes("captain") || c.Role === "CDB") displayRole = mDict.crew_capt || c.Role;
        else if (c.Role.toLowerCase().includes("officer") || c.Role === "OPL") displayRole = mDict.crew_fo || c.Role;

        html += `<li><strong style="color: #60A5FA;">${displayRole}:</strong> ${c.Name}</li>`;
    });

    const paxListLabel = mDict.manifest_list || "LIST";
    const thSeat = mDict.man_th_seat || "Seat";
    const thName = mDict.man_th_name || "Name";
    const thNat = mDict.man_th_nat || "Nat.";
    const thAge = mDict.man_th_age || "Age";

    html += `       </ul>
                    <h3 class="text-xs font-label tracking-[0.4em] text-sky-400 uppercase opacity-80 border-b border-white/5 pb-3 mb-4">${paxListLabel} (${manifest.Passengers.length} PAX)</h3>
                </div>
                <div style="flex: 1; overflow-y: auto; padding-right: 15px; border-right: 1px solid #1e293b; color:#94A3B8; font-size:13px;">
                    <table style="width:100%; text-align:left; border-collapse: collapse;">
                        <thead style="position: sticky; top: 0; background: #0f172a; z-index: 5;">
                            <tr style="border-bottom: 1px solid #334155; color: #cbd5e1;">
                                <th style="padding: 4px;">${thSeat}</th>
                                <th style="padding: 4px;">${thName}</th>
                                <th style="padding: 4px;">${thNat}</th>
                                <th style="padding: 4px; text-align: center;">${thAge}</th>
                            </tr>
                        </thead>
                        <tbody>
    `;

    manifest.Passengers.forEach(p => {
        html += `
            <tr style="border-bottom: 1px solid rgba(51, 65, 85, 0.4);">
                <td style="padding: 3px 4px; color: #38BDF8; font-weight: bold;">${p.Seat}</td>
                <td style="padding: 3px 4px;">${p.Name}</td>
                <td style="padding: 3px 4px;">${p.Nationality}</td>
                <td style="padding: 3px 4px; text-align: center;">${p.Age}</td>
            </tr>
        `;
    });

    html += `           </tbody>
                    </table>
                </div>
            </div>
            
            <div style="flex: 1.5; min-width: 380px; display: flex; flex-direction: column; text-align: center; height: 100%;">
                <h3 class="text-xs font-label tracking-[0.4em] text-sky-400 uppercase opacity-80 border-b border-white/5 pb-3 mb-4 flex-shrink-0">${mapLabel}</h3>
                <div id="seatMapViewport" style="flex: 1; display: flex; justify-content: center; align-items: center; overflow: hidden; padding-top: 10px; cursor: grab; position: relative;">
                    <div id="seatMapContent" style="transform: scale(0.98); transform-origin: center; transition: transform 0.1s ease-out;">
                        ${seatMapHtml}
                    </div>
                </div>
            </div>
        </div>`;

    container.style.height = 'calc(100vh - 120px)';
    container.style.display = 'flex';
    container.style.flexDirection = 'column';
    container.innerHTML = html;

    const btnSendPnc = document.getElementById('btnSendPncCommand');
    if (btnSendPnc) {
        btnSendPnc.addEventListener('click', () => {
            const sel = document.getElementById('pncCommandSelect');
            if (sel && sel.value) {
                window.chrome.webview.postMessage({ action: 'pncCommand', command: sel.value });
                sel.value = "";
            }
        });
    }

    // Pan and Zoom logic
    const viewport = document.getElementById('seatMapViewport');
    const content = document.getElementById('seatMapContent');
    if (viewport && content) {
        let scale = 0.98;
        let isDown = false;
        let startX, startY;
        let currentX = 0, currentY = 0;

        viewport.addEventListener('wheel', (e) => {
            e.preventDefault();
            content.style.transition = 'transform 0.1s ease-out';
            const zoomSensitivity = 0.001;
            scale -= e.deltaY * zoomSensitivity;
            scale = Math.max(0.3, Math.min(3, scale)); // Limits zoom
            content.style.transform = `translate(${currentX}px, ${currentY}px) scale(${scale})`;
        });

        viewport.addEventListener('mousedown', (e) => {
            if (e.button === 1 || e.button === 2 || e.button === 0) { // Middle or Right or Left
                isDown = true;
                viewport.style.cursor = 'grabbing';
                startX = e.clientX - currentX;
                startY = e.clientY - currentY;
            }
        });

        window.addEventListener('mouseup', () => {
            isDown = false;
            if(viewport) viewport.style.cursor = 'grab';
        });

        window.addEventListener('mousemove', (e) => {
            if (!isDown) return;
            e.preventDefault();
            content.style.transition = 'none'; // remove transition for smooth drag
            currentX = e.clientX - startX;
            currentY = e.clientY - startY;
            content.style.transform = `translate(${currentX}px, ${currentY}px) scale(${scale})`;
        });
        
        // Prevent context menu on right click inside viewport
        viewport.addEventListener('contextmenu', e => e.preventDefault());
    }
};

function renderLogbook(history) {
    const grid = document.getElementById('logbookGrid');
    if (!grid) return;
    
    if (!history || history.length === 0) {
        grid.innerHTML = `<div class="col-span-1 md:col-span-2 lg:col-span-3 text-center py-12 text-slate-500 font-label tracking-widest text-xs uppercase" data-i18n="logb_empty">No flight logs recorded yet.</div>`;
        return;
    }

    grid.innerHTML = history.map((f, i) => {
        const isSuper = f.Score >= 500;
        const colorPill = isSuper ? 'bg-emerald-500 shadow-[0_0_10px_rgba(16,185,129,0.5)]' : 'bg-red-500 shadow-[0_0_10px_rgba(239,68,68,0.5)]';
        const dateStr = new Date(f.FlightDate).toLocaleDateString([], { month: 'short', day: '2-digit', year: 'numeric', hour: '2-digit', minute: '2-digit' });
        const blkFormat = f.BlockTime > 0 ? `${Math.floor(f.BlockTime/60)}h ${f.BlockTime%60}m` : '0m';
        
        const payloadStr = encodeURIComponent(JSON.stringify(f)).replace(/'/g, "%27");

        return `
        <div class="bg-[#1C1F26] p-6 rounded-xl border border-white/5 relative hover:border-sky-500/30 hover:bg-white/[0.02] transition-colors cursor-pointer group" onclick="replayFlightLog('${payloadStr}')">
            <div class="absolute top-6 right-6 w-3 h-3 rounded-full ${colorPill}"></div>
            <div class="text-[10px] text-slate-500 font-label tracking-widest uppercase mb-2">${dateStr}</div>
            <div class="text-xl font-headline font-black text-white uppercase tracking-wider mb-4 flex items-center gap-2">
                ${f.Dep} <span class="material-symbols-outlined text-slate-600 text-sm">flight</span> ${f.Arr}
            </div>
            <div class="space-y-2 mt-4 pt-4 border-t border-white/5 font-mono text-xs">
                <div class="flex justify-between">
                    <span class="text-slate-400 uppercase tracking-widest text-[9px] font-manrope font-bold">Flight No</span>
                    <span class="text-white">${f.Airline}${f.FlightNo}</span>
                </div>
                <div class="flex justify-between">
                    <span class="text-slate-400 uppercase tracking-widest text-[9px] font-manrope font-bold">Score</span>
                    <span class="text-${isSuper ? 'emerald' : 'red'}-400 font-bold">${f.Score} pts</span>
                </div>
                <div class="flex justify-between">
                    <span class="text-slate-400 uppercase tracking-widest text-[9px] font-manrope font-bold">Block Time</span>
                    <span class="text-white">${blkFormat}</span>
                </div>
                <div class="flex justify-between">
                    <span class="text-slate-400 uppercase tracking-widest text-[9px] font-manrope font-bold">Touchdown</span>
                    <span class="text-white">${Math.round(f.TouchdownFpm)} fpm</span>
                </div>
            </div>
        </div>`;
    }).join('');
}

function replayFlightLog(encodedPayload) {
    try {
        const report = JSON.parse(decodeURIComponent(encodedPayload));
        
        // Dispatch synthetic message to the webview listeners
        const spoofedEvent = new MessageEvent('message', { 
            data: { type: 'flightReport', report: report } 
        });
        
        // As we haven't detached the anonymous listener, we can dispatch it on the window.chrome.webview directly if it inherits EventTarget
        // Otherwise, manually call the logic by dispatching on `window` and let the listener catch it if we rebind it, 
        // OR better yet, let's just mutate the event listeners.
        // Easiest is to dispatch a regular window event if we rename the listener, but for now we'll try dispatching on the webview:
        window.chrome.webview.dispatchEvent(spoofedEvent);
        document.getElementById('flightReportModal').style.display = 'flex';
    } catch(e) {
        console.error("Failed to parse historical log payload", e);
    }
}
