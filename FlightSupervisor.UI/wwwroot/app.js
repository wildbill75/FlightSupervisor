window.formatAirportData = function(cityRaw, nameRaw) {
    let city = (cityRaw || "").split('/')[0].trim();
    let name = (nameRaw || "").replace(/airport/gi, '').replace(/aéroport/gi, '').replace(/international/gi, '').replace(/intl/gi, '').trim();
    if (city && name.toLowerCase().startsWith(city.toLowerCase())) {
        name = name.substring(city.length).trim();
        if (name.startsWith('-') || name.startsWith('/')) {
            name = name.substring(1).trim();
        }
    }
    if (!name) name = "";
    return { city: city.toUpperCase(), name: name.toUpperCase() };
};

window.formatAirportLabel = function(cityRaw, nameRaw) {
    const data = window.formatAirportData(cityRaw, nameRaw);
    if (!data.name) return data.city;
    return `${data.city} - ${data.name}`;
};

window.updateDashboardAnimation = function(telemetry) {
    if (!telemetry) return;
    const progressLine = document.getElementById('dashboardProgressLine');
    const airplaneIcon = document.getElementById('dashboardAirplaneIcon');
    if (!progressLine || !airplaneIcon) return;
    
    let rawPercent = 0;
    const phase = telemetry.phaseEnum || "Preflight";
    const preF = ["Preflight", "Boarding", "TaxiOut"];
    const postF = ["Landing", "TaxiIn", "Turnaround", "Arrived", "Finished"];
    
    if (preF.includes(phase)) {
        rawPercent = 0;
    } else if (postF.includes(phase)) {
        rawPercent = 100;
    } else {
        let totalDist = 0;
        if (window.allRotations && window.allRotations.length > 0 && window.allRotations[0].data && window.allRotations[0].data.general) {
            totalDist = parseFloat(window.allRotations[0].data.general.route_distance);
        }
        let flownDist = telemetry.originDistanceNM || 0;
        
        if (totalDist > 0 && flownDist > 0) {
            rawPercent = (flownDist / totalDist) * 100;
            if (rawPercent < 5) rawPercent = 5;
            if (rawPercent > 95) rawPercent = 95;
            if (phase === "Approach" && rawPercent < 90) rawPercent = 95;
            if (phase === "Takeoff" && rawPercent > 10) rawPercent = 5;
        } else {
            if (phase === "Takeoff") rawPercent = 5;
            else if (phase === "Climb") rawPercent = 20;
            else if (phase === "Cruise") rawPercent = 50;
            else if (phase === "Descent") rawPercent = 80;
            else if (phase === "Approach") rawPercent = 95;
        }
    }
    
    const svgX = 2 + (rawPercent / 100) * 96;
    progressLine.setAttribute('x2', svgX);
    airplaneIcon.style.left = svgX + '%';
};

window.populateBriefingView = (index = 0) => {
    const briefingContent = document.getElementById('briefing-content');
    if (!briefingContent || !window.allRotations || window.allRotations.length === 0) return;
    
    // Ensure content is visible
    briefingContent.classList.remove('hidden');
    setTimeout(() => {
        briefingContent.classList.remove('opacity-0');
        briefingContent.classList.add('opacity-100');
    }, 50);

    const rot = window.allRotations[index];
    if (!rot) return;
    
    const rd = rot.data;
    const briefingData = rot.briefing; // The WeatherBriefingData block
    
    // Route Summary update
    const elRouteSummary = document.getElementById('briefingRouteSummary');
    if (elRouteSummary) {
        let routeString = rd.general.route || '';
        let routeParts = routeString.split(' ').filter(p => p.trim() !== '');
        let formattedRoute = routeString;
        if(routeParts.length > 1) {
            let sid = routeParts[0];
            let star = routeParts[routeParts.length - 1];
            let middle = routeParts.slice(1, routeParts.length - 1).join(' ');
            formattedRoute = `<span class="text-fuchsia-400 drop-shadow-[0_0_5px_rgba(232,121,249,0.5)] font-black">${sid}</span> ${middle} <span class="text-pink-400 drop-shadow-[0_0_5px_rgba(244,114,182,0.5)] font-black">${star}</span>`;
        }

        let routeHtml = `
            <div class="grid grid-cols-6 items-center w-full bg-[#1C1F26]/80 p-5 rounded-xl border border-white/5 shadow-md divide-x divide-white/5">
                <div class="flex flex-col items-center justify-center cursor-pointer group" onclick="if(window.showAirlineIdentityModal) window.showAirlineIdentityModal('${rd.general.icao_airline||''}')">
                    <span class="text-[9px] text-[#7b7b7b] font-bold tracking-widest uppercase mb-1">Airline</span>
                    <span class="text-emerald-400 group-hover:text-white transition-colors text-xl font-black tracking-widest font-headline">${rot.airlineProfile ? rot.airlineProfile.name : (rd.general.airline_name || rd.general.icao_airline || '---')}</span>
                </div>
                <div class="flex flex-col items-center justify-center">
                    <span class="text-[9px] text-[#7b7b7b] font-bold tracking-widest uppercase mb-1">Flight Number</span>
                    <span class="text-white text-xl font-black tracking-widest font-headline">${rd.general.icao_airline || ''}${rd.general.flight_number || ''}</span>
                </div>
                <div class="flex flex-col items-center justify-center">
                    <span class="text-[9px] text-[#7b7b7b] font-bold tracking-widest uppercase mb-1">Route</span>
                    <div class="flex items-center gap-3">
                        <span class="text-xl font-black text-sky-400 tracking-widest font-headline">${rd.origin.icao_code}</span>
                        <span class="material-symbols-outlined text-white/30 text-[14px]">flight_takeoff</span>
                        <span class="text-xl font-black text-emerald-400 tracking-widest font-headline">${rd.destination.icao_code}</span>
                    </div>
                </div>
                <div class="flex flex-col items-center justify-center">
                    <span class="text-[9px] text-[#7b7b7b] font-bold tracking-widest uppercase mb-1">Aircraft</span>
                    <span class="text-slate-200 text-lg font-bold font-mono">${rd.aircraft.base_type || rd.aircraft.icaocode}</span>
                </div>
                <div class="flex flex-col items-center justify-center">
                    <span class="text-[9px] text-[#7b7b7b] font-bold tracking-widest uppercase mb-1">Cruising Alt</span>
                    <span class="text-slate-200 text-lg font-bold font-mono">FL${Math.round((rd.general.initial_altitude || 0) / 100) || 'N/A'}</span>
                </div>
                <div class="flex flex-col items-center justify-center">
                    <span class="text-[9px] text-[#7b7b7b] font-bold tracking-widest uppercase mb-1">Trip Distance</span>
                    <span class="text-slate-200 text-lg font-bold font-mono">${rd.general.route_distance} nm</span>
                </div>
            </div>
            
            <div class="bg-[#1C1F26]/80 mt-3 p-4 rounded-xl border border-white/5 shadow-md flex flex-col items-center justify-center">
                <div class="flex items-center gap-2 mb-3">
                    <span class="material-symbols-outlined text-slate-500 text-sm">route</span>
                    <span class="text-[10px] text-slate-500 font-bold tracking-widest uppercase">Filed Route</span>
                </div>
                <div class="text-slate-300 font-mono text-base font-bold leading-relaxed tracking-wider px-4 text-center">
                    ${formattedRoute}
                </div>
            </div>
            </div>`;
        elRouteSummary.innerHTML = routeHtml;
    }

    const elDep = document.getElementById('legDepartureMetarBox');
    const elArr = document.getElementById('legArrivalMetarBox');
    const elAlt = document.getElementById('legAlternateMetarBox');
    const elEnRoute = document.getElementById('legEnRouteBox');
    const elNotams = document.getElementById('legNotamsBox');

    let depHtml = '<div class="text-slate-500 font-mono text-xs">No Departure Data</div>';
    let arrHtml = '<div class="text-slate-500 font-mono text-xs">No Arrival Data</div>';
    let altHtml = '<div class="text-slate-500 font-mono text-xs">No Alternate Data</div>';
    let enRouteHtml = '<div class="text-slate-500 font-mono text-xs">No En Route Data</div>';
    let notamsHtml = '<div class="text-slate-500 font-mono text-xs">No Operational Notams</div>';

    let globalOpAlerts = [];
    if (briefingData && briefingData.Stations) {
        briefingData.Stations.forEach(st => {
            let pillsHtml = '';
            if (st.TempDew) pillsHtml += `<div class="bg-black/40 border border-white/5 rounded px-4 py-2 text-xs text-[#b6b6b6] flex items-center gap-2 font-mono shadow-sm"><div class="w-2 h-2 rounded-full bg-orange-400 opacity-80"></div> <span class="text-white font-bold">${st.TempDew}</span></div>`;
            if (st.Qnh) pillsHtml += `<div class="bg-black/40 border border-white/5 rounded px-4 py-2 text-xs text-[#b6b6b6] flex items-center gap-2 font-mono shadow-sm"><div class="w-2 h-2 rounded-full bg-sky-400 opacity-80"></div> <span class="text-white font-bold">${st.Qnh}</span></div>`;
            
            if (st.Wind) {
                let windColor = 'bg-slate-300', windText = 'text-white';
                let wMatch = st.Wind.match(/(\d+)G(\d+)/) || st.Wind.match(/(\d+)\s*kt/);
                if (wMatch) {
                    let knots = parseInt(wMatch[2] || wMatch[1], 10);
                    if (knots >= 35) { windColor = 'bg-red-500 animate-pulse'; windText = 'text-red-400'; }
                    else if (knots >= 20) { windColor = 'bg-orange-500 animate-pulse'; windText = 'text-orange-400'; }
                }
                pillsHtml += `<div class="bg-black/40 border border-white/5 rounded px-4 py-2 text-xs text-[#b6b6b6] flex items-center gap-2 font-mono shadow-sm"><div class="w-2 h-2 rounded-full ${windColor} opacity-80"></div> <span class="${windText} font-bold">${st.Wind}</span></div>`;
            }

            if (st.Visibility) {
                let visColor = 'bg-violet-400', visText = 'text-white';
                let isSM = st.Visibility.includes('SM') || st.Visibility.includes('sm');
                let isM = st.Visibility.includes('m') && !isSM;
                let numMatch = st.Visibility.match(/(\d+\.?\d*|\d+\/\d+)/);
                if (numMatch) {
                    let val = parseFloat(numMatch[1]);
                    if (st.Visibility.includes('/')) {
                        const parts = numMatch[1].split('/');
                        val = parseInt(parts[0]) / parseInt(parts[1]);
                    }
                    let isLow = false, isVeryLow = false;
                    if (isSM) {
                        if (val <= 0.5) isVeryLow = true;
                        else if (val <= 1.5) isLow = true;
                    } else if (isM) {
                        if (val <= 800) isVeryLow = true;
                        else if (val <= 2000) isLow = true;
                    }
                    if (isVeryLow) { visColor = 'bg-red-500 animate-pulse'; visText = 'text-red-400'; }
                    else if (isLow) { visColor = 'bg-orange-500 animate-pulse'; visText = 'text-orange-400'; }
                }
                pillsHtml += `<div class="bg-black/40 border border-white/5 rounded px-4 py-2 text-xs text-[#b6b6b6] flex items-center gap-2 font-mono shadow-sm"><div class="w-2 h-2 rounded-full ${visColor} opacity-80"></div> <span class="${visText} font-bold">${st.Visibility}</span></div>`;
            }

            if (st.CloudBase) pillsHtml += `<div class="bg-black/40 border border-white/5 rounded px-4 py-2 text-xs text-[#b6b6b6] flex items-center gap-2 font-mono shadow-sm"><div class="w-2 h-2 rounded-full bg-slate-500 opacity-80"></div> <span class="text-white font-bold">${st.CloudBase}</span></div>`;

            
            if (st.RunwayAdvice && st.RunwayAdvice.includes('Runway')) {
                const rwyMatch = st.RunwayAdvice.match(/Runway\s+([A-Z0-9]+)/i) || st.RunwayAdvice.match(/Piste.*?\s+([A-Z0-9]+)/i);
                if (rwyMatch) pillsHtml += `<div class="bg-black/40 border border-white/5 rounded px-4 py-2 text-xs text-[#b6b6b6] flex items-center gap-2 font-mono shadow-sm"><div class="w-2 h-2 rounded-full bg-emerald-400 opacity-80"></div> <span class="text-white font-bold">RWY ${rwyMatch[1].toUpperCase()}</span></div>`;
            }

            const rawMetarTaf = ((st.RawMetar || "") + " " + (st.RawTaf || "")).toUpperCase();

            // Wind Analysis (Gusts, Shear, Severe Wind)
            if (st.Wind) {
                const speedMatch = st.Wind.match(/(\d{2,3})(?:G(\d{2,3}))?(?:KT|MPS|KMH)/i);
                if (speedMatch) {
                    const speed = parseInt(speedMatch[1] || '0', 10);
                    const gust = parseInt(speedMatch[2] || '0', 10);
                    if (gust >= 25 || speed >= 35) {
                        globalOpAlerts.push(`[${st.Icao}] WARNING: High wind/gusts detected. Increased probability of Go-Around.`);
                    }
                }
            }
            if (rawMetarTaf.includes(" WS ") || rawMetarTaf.includes("WIND SHEAR")) {
                globalOpAlerts.push(`[${st.Icao}] DANGER: Windshear reported. Expect severe turbulence on short final; brief for immediate Go-Around.`);
            }

            // Thunderstorms
            if (rawMetarTaf.match(/\b(TS|TSRA|TSGR|\+TSRA|FC)\b/)) {
                globalOpAlerts.push(`[${st.Icao}] DANGER: Active Thunderstorms / squall cells in vicinity. Weather avoidance maneuvers mandatory.`);
            }

            // Freezing/Icing
            if (rawMetarTaf.match(/\b(FZRA|FZDZ|FZFG|\-FZRA|SN|\+SN|PL|SG|GR)\b/) || (st.TempDew && st.TempDew.includes("M"))) {
                if (st.Id && (st.Id.toLowerCase() === 'origin' || st.Id.toLowerCase() === 'departure')) {
                    globalOpAlerts.push(`[${st.Icao}] ALERT: Freezing conditions present. Mandatory Ground De-icing operations required before block out.`);
                } else {
                    globalOpAlerts.push(`[${st.Icao}] ALERT: Freezing conditions / Snow reported. Expect severe anti-ice engine load and potential holding for runway sweep.`);
                }
            }

            // Low Visibility / CAT III
            let isLowVis = false;
            if (st.Visibility) {
                const visMatch = st.Visibility.match(/\b(\d{2,4})\b/);
                if (visMatch) {
                    const vis = parseInt(visMatch[1], 10);
                    if (vis < 800) isLowVis = true;
                }
                if (st.Visibility.includes('1/2SM') || st.Visibility.includes('1/4SM') || st.Visibility.includes('1/8SM')) isLowVis = true;
            }
            if (rawMetarTaf.match(/\b(RVR|LVP|FG|VV00)\b/) || isLowVis) {
                globalOpAlerts.push(`[${st.Icao}] CRITICAL: Low Visibility Procedures (LVP) in force. Autoland CAT II/III approach likely. Verify crew and aircraft qualifications.`);
            }

            let htmlBlock = `<div class="flex justify-between items-start mb-3 relative group">
                                <div>
                                    <div class="font-bold text-white text-xl tracking-widest drop-shadow-md mb-2">${st.Icao || ''}</div>
                                    <div class="flex flex-wrap gap-2 mb-3">${pillsHtml}</div>
                                </div>
                                <button onclick="this.nextElementSibling.classList.toggle('hidden')" class="text-[10px] uppercase font-bold text-sky-400/80 hover:text-sky-300 px-3 py-1.5 rounded-md border border-sky-400/20 bg-sky-900/10 hover:bg-sky-900/30 transition-all font-mono tracking-widest mt-1">RAW DATA</button>
                                <div class="hidden absolute top-12 left-0 right-0 z-50 p-4 bg-[#0f1115] rounded-xl border border-white/10 shadow-2xl font-mono text-xs text-emerald-400/90 leading-relaxed max-h-48 overflow-y-auto custom-scrollbar">
                                    <div class="mb-3 break-words text-emerald-300 border-b border-white/5 pb-2">${st.RawMetar || 'No METAR available.'}</div>
                                    <div class="text-emerald-700/80 break-words">${st.RawTaf || 'No TAF available.'}</div>
                                </div>
                             </div>
                             <div class="text-slate-300 text-sm leading-relaxed font-sans bg-white/5 p-3 rounded-lg border-l-2 border-slate-500/50 italic">${st.Commentary || 'Pas de briefing météorologique narratif.'}</div>`;
                             
            if (st.Id.toLowerCase() === 'origin' || st.Id.toLowerCase() === 'departure') {
                depHtml = htmlBlock;
            } else if (st.Id.toLowerCase() === 'destination') {
                arrHtml = htmlBlock;
            } else if (st.Id.toLowerCase() === 'alternate') {
                altHtml = htmlBlock;
            }
        });
        
        // Combine all NOTAMs from all stations for this leg
        let allNotams = '';
        if (globalOpAlerts.length > 0) {
            allNotams += `<div class="mb-4 border-l-2 border-rose-500 pl-3 bg-rose-900/10 py-2 rounded-r-lg">
                            <ul class="list-disc list-inside text-rose-300 text-[11px] font-mono leading-relaxed space-y-1">
                                ${globalOpAlerts.map(a => `<li>${a}</li>`).join('')}
                            </ul>
                         </div>`;
        }
        briefingData.Stations.forEach(st => {
            if (st.Notams && st.Notams.trim() !== '') {
                allNotams += `<div class="mb-4 bg-black/20 p-3 rounded-lg border border-white/5"><div class="text-[10px] font-black tracking-widest text-indigo-400 mb-2 border-b border-white/5 pb-1">[${st.Icao}]</div><div class="text-slate-400 text-[10px] font-mono leading-relaxed">${st.Notams.trim().replace(/\\n/g, '<br/>')}</div></div>`;
            }
        });
        if (allNotams !== '') notamsHtml = allNotams;

        // Try extracting en-route info using WeatherBriefingService mapping if any
        if (briefingData && briefingData.EnrouteText) {
            enRouteHtml = `<div class="text-slate-300 text-sm leading-relaxed font-sans bg-white/5 p-3 rounded-lg border-l-2 border-blue-500/50">${briefingData.EnrouteText}</div>`;
        } else {
             enRouteHtml = `<div class="text-slate-300 text-sm leading-relaxed font-sans bg-white/5 p-3 rounded-lg border-l-2 border-slate-500/50 italic">No significant en-route weather detected. Operations normal.</div>`;
        }
    }

    if (elDep) elDep.innerHTML = depHtml;
    if (elArr) elArr.innerHTML = arrHtml;
    if (elAlt) elAlt.innerHTML = altHtml;
    if (elEnRoute) elEnRoute.innerHTML = enRouteHtml;
    if (elNotams) elNotams.innerHTML = notamsHtml;



    // Fuel & Weight UI has been migrated to fuelsheet_window.html (external WPF modal)
};




window.dashboardActiveLegIndex = 0;
window.isDispatchSignedOff = false;

window.navigateDashboardLeg = (dir) => {
    if (!window.allRotations || window.allRotations.length === 0) return;
    window.dashboardActiveLegIndex += dir;
    if (window.dashboardActiveLegIndex < 0) window.dashboardActiveLegIndex = 0;
    if (window.dashboardActiveLegIndex >= window.allRotations.length) window.dashboardActiveLegIndex = window.allRotations.length - 1;
    window.populateDashboardActiveLeg(window.dashboardActiveLegIndex);
    if (window.populateBriefingView) window.populateBriefingView(window.dashboardActiveLegIndex);
    if (window.renderBriefingTimeline) window.renderBriefingTimeline();
};
window.AIRLINES = {
    'AFR': 'Air France', 'BAW': 'British Airways', 'EZY': 'easyJet', 'RYR': 'Ryanair',
    'DLH': 'Lufthansa', 'UAE': 'Emirates', 'QTR': 'Qatar Airways', 'DAL': 'Delta',
    'AAL': 'American Airlines', 'UAL': 'United', 'SWA': 'Southwest'
};

window.resetDashboardWidgets = () => {
    // 1. Ground Ops Milestone Table
    const milestoneIds = ['ttSchedDep', 'ttActDep', 'ttSchedArr', 'ttActArr'];
    milestoneIds.forEach(id => {
        const el = document.getElementById(id);
        if (el) el.innerText = '--:--Z';
    });
    
    const statuses = ['ttDepStatus', 'ttArrStatus'];
    statuses.forEach(id => {
        const el = document.getElementById(id);
        if (el) {
            el.innerText = 'STANDBY';
            el.className = 'px-2 py-0.5 rounded bg-surface-container-highest text-[10px] text-[#7b7b7b] uppercase font-bold tracking-wider';
        }
    });

    // 2. Cabin Experience
    const paxValues = {
        'paxComfortValue': '--%',
        'paxAnxietyValue': '--%',
        'paxSatisfactionValue': '--%',
        'cleanlinessVal': '100%',
        'cateringRationsVal': '--',
        'waterLevelVal': '100%',
        'wasteLevelVal': '0%',
        'turbSeverityValue': 'NONE',
        'thermalValue': '22.0°C'
    };
    for (const [id, val] of Object.entries(paxValues)) {
        const el = document.getElementById(id);
        if (el) {
            if (id.includes('Value')) {
                 el.innerHTML = `${val.replace('%', '')}<span class="text-sm text-slate-600 font-light ml-1">%</span>`;
            } else {
                 el.innerText = val;
            }
            // Color coding for pristine state
            if (id === 'cleanlinessVal' || id === 'waterLevelVal' || id === 'cateringRationsVal') {
                el.style.color = '#34D399'; // Emerald-400 (Ready/Pristine)
            } else if (id === 'wasteLevelVal') {
                el.style.color = '#34D399'; // Empty waste is good
            } else {
                el.style.color = '#7b7b7b';
            }
            el.style.textShadow = 'none';
        }
    }

    const bars = ['paxComfortBar', 'paxAnxietyBar', 'paxSatisfactionBar', 'turbSeverityBar', 'cateringBar', 'pncProgressBar', 'dashMetaFill'];
    bars.forEach(id => {
        const el = document.getElementById(id);
        if (el) {
            el.style.width = '0%';
            el.style.backgroundColor = '#64748b';
            el.style.boxShadow = 'none';
        }
    });

    const needle = document.getElementById('thermalNeedle');
    if (needle) needle.style.left = '50%';

    // 3. PNC Comms
    const pncStatusLabel = document.getElementById('pncStatusLabel');
    if (pncStatusLabel) pncStatusLabel.innerText = 'Standing By';
    const pncStatusDot = document.getElementById('pncStatusDot');
    if (pncStatusDot) pncStatusDot.className = 'w-2 h-2 rounded-full bg-slate-500 animate-pulse';

    const pncStats = ['crewProactivityLabel', 'crewEfficiencyLabel', 'crewMoraleLabel'];
    pncStats.forEach(id => {
        const el = document.getElementById(id);
        if (el) el.innerText = '--';
    });

    // 4. Meta / Score / Phase
    const dashMetaText = document.getElementById('dashMetaText');
    if (dashMetaText) dashMetaText.innerText = 'Standing By.';
    
    // Hide meta bar if it was visible
    const dashMetaBar = document.getElementById('dashMetaBar');
    if (dashMetaBar) dashMetaBar.style.display = 'none';

    const flightPhase = document.getElementById('flightPhase');
    if (flightPhase) flightPhase.innerText = 'OFF';

    const mainScore = document.getElementById('mainScoreValue');
    if (mainScore) mainScore.innerText = '1000';

    const puncBar = document.getElementById('puncBarContainer');
    if (puncBar) puncBar.style.display = 'none';

    const liveLog = document.getElementById('liveScoreLog');
    if (liveLog) liveLog.innerHTML = '<li class="text-[10px] font-mono text-[#7b7b7b] text-center tracking-widest mt-8">AWAITING EVENTS...</li>';
};

window.populateDashboardActiveLeg = (index = 0) => {
    const emptyContainer = document.getElementById('dashFlightVisualEmpty');
    const activeContainer = document.getElementById('dashFlightVisualActive');
    
    if (!window.allRotations || window.allRotations.length === 0) {
        if (emptyContainer) emptyContainer.style.display = 'block';
        if (activeContainer) activeContainer.style.display = 'none';
        return;
    }
    
    if (emptyContainer) emptyContainer.style.display = 'none';
    if (activeContainer) {
        activeContainer.style.display = 'flex';
        
        const rd = window.allRotations[index].data;
        if (!rd) return;

        const icao = rd.general?.icao_airline || '---';
        const fn = (rd.general?.icao_airline || '') + (rd.general?.flight_number || '---');
        const ac = window.AIRLINES[icao] || rd.general?.airline_name || icao;
        
        let ete = '--H--';
        if (rd.times?.est_time_enroute) {
            ete = Math.floor(rd.times.est_time_enroute / 3600).toString().padStart(2, '0') + 'H' + Math.floor((rd.times.est_time_enroute % 3600) / 60).toString().padStart(2, '0');
        }

        const flRaw = rd.general?.initial_altitude || rd.general?.initial_alti || '---';
        let fl = '---';
        if (flRaw !== '---') {
            let flNum = parseInt(flRaw.toString().replace(/[^0-9]/g, ''), 10);
            if (!isNaN(flNum)) {
                if (flNum > 1000) flNum = Math.floor(flNum / 100);
                fl = 'FL' + flNum.toString().padStart(3, '0');
            } else {
                fl = 'FL' + flRaw.toString().substring(0, 3);
            }
        }

        const getStationQnh = (type) => {
            if (!window.allRotations[index].briefing) return '---';
            const stations = window.allRotations[index].briefing.Stations || window.allRotations[index].briefing.stations;
            if (!stations) return '---';
            const st = stations.find(s => {
                const sid = s.Id || s.id || '';
                return sid.toLowerCase() === type.toLowerCase();
            });
            if (!st) return '---';
            return st.Qnh || st.qnh || st.QNH || '---';
        };

        const depQnh = getStationQnh('origin') !== '---' ? getStationQnh('origin') : getStationQnh('departure');
        const arrQnh = getStationQnh('destination');

        const disableLeft = index === 0 || index <= (window.activeLegIndex || 0);
        const leftOpacity = disableLeft ? 'opacity-20 pointer-events-none' : 'opacity-100 hover:bg-white/10 cursor-pointer';
        const rightOpacity = index === window.allRotations.length - 1 ? 'opacity-20 pointer-events-none' : 'opacity-100 hover:bg-white/10 cursor-pointer';

        activeContainer.innerHTML = `
            <div class="w-full flex flex-col gap-2 animate-fade-in">
                <!-- Top Row: Banner -->
                <div class="flex items-stretch justify-between w-full bg-[#1a1d24]/60 backdrop-blur-md rounded-2xl border border-white/5 py-4 px-2 shadow-[0_8px_30px_rgba(0,0,0,0.5)] mb-2">
                    <div class="flex flex-col flex-1 items-center justify-center border-r border-white/10 px-4">
                        <div class="text-[9px] font-bold tracking-[0.2em] text-[#7b7b7b] uppercase">Airline</div>
                        <div class="text-[20px] font-black text-emerald-400 hover:text-white transition-colors cursor-pointer tracking-widest mt-1 uppercase text-center" onclick="if(window.showAirlineIdentityModal) window.showAirlineIdentityModal('${icao}')">${ac}</div>
                    </div>
                    <div class="flex flex-col flex-1 items-center justify-center border-r border-white/10 px-4">
                        <div class="text-[9px] font-bold tracking-[0.2em] text-[#7b7b7b] uppercase">Flight N&deg;</div>
                        <div class="text-[20px] font-black text-white tracking-widest mt-1 uppercase">${fn}</div>
                    </div>
                    <div class="flex flex-col flex-[1.5] items-center justify-center border-r border-white/10 px-4">
                        <div class="text-[9px] font-bold tracking-[0.2em] text-[#7b7b7b] uppercase">Airframe</div>
                        <div class="text-[20px] font-black text-white tracking-widest mt-1 whitespace-nowrap text-ellipsis">${(rd.aircraft?.name || rd.aircraft?.base_type || '---').replace(/Airbus /gi, '').replace(/FENIX /gi, '').replace(/VNAV /gi, '')}</div>
                    </div>
                    <div class="flex flex-col flex-1 items-center justify-center px-4">
                        <div class="text-[9px] font-bold tracking-[0.2em] text-[#7b7b7b] uppercase">Registration</div>
                        <div class="text-[20px] font-black text-white tracking-widest mt-1 uppercase">${rd.aircraft?.reg || '---'}</div>
                    </div>
                </div>

                <!-- Bottom Row: Leg Pill + Add Button -->
                <div class="flex items-stretch w-full gap-2">
                    
                    <!-- Main Leg Pill -->
                    <div class="flex-1 bg-[#1a1d24]/60 backdrop-blur-md rounded-2xl border border-white/5 flex shadow-[0_8px_30px_rgba(0,0,0,0.5)] overflow-hidden h-[164px]">
                        
                        <!-- Left Arrow Button -->
                        <div role="button" tabindex="0" onclick="window.navigateDashboardLeg(-1)" class="px-6 flex items-center justify-center transition-colors group ${leftOpacity}">
                           <svg viewBox="0 0 24 24" class="w-10 h-10 fill-white group-hover:scale-110 transition-transform"><path d="M15.41 16.59L10.83 12l4.58-4.59L14 6l-6 6 6 6 1.41-1.41z"/></svg> 
                        </div>

                        <!-- Clickable Leg Content Area -->
                        <div role="button" tabindex="-1" class="flex-1 border-x border-solid border-white/10 flex justify-between items-center py-4 px-10 relative cursor-default text-left">
                            
                            <!-- Left: FROM -->
                            <div class="flex flex-col z-10 w-[25%] pointer-events-none text-left">
                                <div class="text-[10px] font-bold tracking-[0.2em] text-[#7b7b7b] uppercase">From</div>
                                <div class="text-[44px] font-black text-[#58586c] tracking-widest mt-1 leading-none uppercase">${rd.origin?.icao_code || '---'}</div>
                                <div class="text-[9px] font-bold text-slate-400 uppercase tracking-wider whitespace-nowrap overflow-hidden text-ellipsis w-full max-w-[200px]" title="${window.airportsDb && window.airportsDb[rd.origin?.icao_code] ? window.formatAirportLabel(window.airportsDb[rd.origin.icao_code].city, window.airportsDb[rd.origin.icao_code].name) : ''}">
                                    ${window.airportsDb && window.airportsDb[rd.origin?.icao_code] ? window.formatAirportLabel(window.airportsDb[rd.origin.icao_code].city, window.airportsDb[rd.origin.icao_code].name) : ''}
                                </div>
                                <div class="text-[11px] font-bold text-[#7b7b7b] uppercase mt-2 tracking-wider whitespace-nowrap">RWY:${rd.origin?.plan_rwy || '---'} <span class="mx-1">/</span> QNH:${depQnh}</div>
                                <div class="text-[14px] font-bold text-white uppercase mt-1 tracking-widest">${rd.times?.sched_out ? new Date(parseInt(rd.times.sched_out) * 1000).toISOString().substr(11, 5) + 'Z' : '--:--'}</div>
                            </div>

                            <!-- Center: ROUTE & ETE -->
                            <div class="flex flex-col items-center flex-1 px-8 z-10 w-full overflow-hidden pointer-events-none">
                                <div class="text-[12px] font-bold tracking-[0.2em] text-[#7b7b7b] uppercase text-center">ETE ${ete}</div>
                                <div class="text-[16px] font-bold text-white tracking-widest uppercase mt-1 text-center">${fl}</div>
                                
                                <div class="w-full flex items-center justify-center my-4 relative pointer-events-none">
                                    <svg class="w-full h-4 text-white drop-shadow-[0_0_8px_rgba(255,255,255,0.3)] overflow-visible" preserveAspectRatio="none" viewBox="0 0 100 10">
                                        <defs>
                                            <linearGradient id="flightProgressGradient" x1="0%" y1="0%" x2="100%" y2="0%">
                                                <stop offset="0%" stop-color="#3b82f6" />
                                                <stop offset="100%" stop-color="#10b981" />
                                            </linearGradient>
                                        </defs>
                                        <line x1="2" y1="5" x2="98" y2="5" stroke="currentColor" stroke-width="2.5" stroke-opacity="0.2" />
                                        <line id="dashboardProgressLine" x1="2" y1="5" x2="2" y2="5" stroke="url(#flightProgressGradient)" stroke-width="2.5" stroke-linecap="round" class="transition-all duration-1000 ease-in-out" />
                                    </svg>
                                    <div id="dashboardAirplaneIcon" class="absolute transition-all duration-1000 ease-in-out" style="left: 2%; top: 50%; transform: translate(-50%, -50%);">
                                        <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 576 512" class="w-7 h-7 fill-white drop-shadow-[0_0_5px_rgba(255,255,255,0.8)]"><path d="M482.3 192c34 0 93.7 29 93.7 64c0 36-59.7 64-93.7 64l-116.6 0L265.2 495.9c-5.7 10-16.3 16.1-27.8 16.1l-56.2 0c-10.6 0-18.3-10.2-15.4-20.4l49-171.6L112 320 68.8 377.6c-3 4-7.8 6.4-12.8 6.4l-42 0c-7.8 0-14-6.3-14-14c0-1.3 .2-2.6 .5-3.9L32 256 .5 145.9c-.4-1.3-.5-2.6-.5-3.9c0-7.8 6.3-14 14-14l42 0c5 0 9.8 2.4 12.8 6.4L112 192l102.9 0-49-171.6C162.9 10.2 170.6 0 181.2 0l56.2 0c11.5 0 22.1 6.2 27.8 16.1L365.7 192l116.6 0z"/></svg>
                                    </div>
                                </div>
                                
                                <div class="text-[12px] font-mono tracking-[0.2em] text-white uppercase text-center w-[120%] px-2 mt-2" style="line-height:1.2;">
                                    ${rd.general?.route || 'CLEARED FOR DEPARTURE'}
                                </div>
                            </div>

                            <!-- Right: TO -->
                            <div class="flex flex-col text-right items-end z-10 w-[25%] pointer-events-none text-right">
                                <div class="text-[10px] font-bold tracking-[0.2em] text-[#7b7b7b] uppercase">To</div>
                                <div class="text-[44px] font-black text-[#58586c] tracking-widest mt-1 leading-none uppercase">${rd.destination?.icao_code || '---'}</div>
                                <div class="text-[9px] font-bold text-slate-400 uppercase tracking-wider whitespace-nowrap overflow-hidden text-ellipsis w-full max-w-[200px]" title="${window.airportsDb && window.airportsDb[rd.destination?.icao_code] ? window.formatAirportLabel(window.airportsDb[rd.destination.icao_code].city, window.airportsDb[rd.destination.icao_code].name) : ''}">
                                    ${window.airportsDb && window.airportsDb[rd.destination?.icao_code] ? window.formatAirportLabel(window.airportsDb[rd.destination.icao_code].city, window.airportsDb[rd.destination.icao_code].name) : ''}
                                </div>
                                <div class="text-[11px] font-bold text-[#7b7b7b] uppercase mt-2 tracking-wider whitespace-nowrap">RWY:${rd.destination?.plan_rwy || '---'} <span class="mx-1">/</span> QNH:${arrQnh}</div>
                                <div class="text-[14px] font-bold text-white uppercase mt-1 tracking-widest">${rd.times?.sched_in ? new Date(parseInt(rd.times.sched_in) * 1000).toISOString().substr(11, 5) + 'Z' : '--:--'}</div>
                            </div>
                        </div>

                        <!-- Right Arrow Button -->
                        <div role="button" tabindex="0" onclick="window.navigateDashboardLeg(1)" class="px-6 flex items-center justify-center transition-colors group ${rightOpacity}">
                            <svg viewBox="0 0 24 24" class="w-10 h-10 fill-white group-hover:scale-110 transition-transform"><path d="M8.59 16.59L13.17 12 8.59 7.41 10 6l6 6-6 6-1.41-1.41z"/></svg>
                        </div>
                    </div>

                    </div>
                </div>
            </div>
        `;
        if (typeof window.updateDashboardAnimation === 'function' && window.lastTelemetry) {
            window.updateDashboardAnimation(window.lastTelemetry);
        }
    }
};

window.clearCurrentLeg = function() {
    const idx = window.dashboardActiveLegIndex || 0;
    
    if (idx === 0) {
        // Block deleting index 0 (Active Leg)
        window.showSystemConfirm({
            title: "Action Restricted",
            message: "The active flight leg cannot be removed individually. Use 'CLEAR ALL LEGS' if you wish to reset the entire rotation and start over.",
            icon: "warning",
            confirmText: "Understood",
            isAlertOnly: true
        });
        return;
    }

    // Allow deleting index > 0
    window.showSystemConfirm({
        title: "Remove Upcoming Leg",
        message: "Are you sure you want to remove this leg from your rotation? This action cannot be undone.",
        icon: "delete",
        confirmText: "Remove Leg",
        onConfirm: () => {
            window.chrome.webview.postMessage({ action: "deleteLegAtIndex", index: idx });
        }
    });
};

window.clearAllLegs = function() {
    window.showSystemConfirm({
        title: "Factory Reset Rotation",
        message: "This will clear ALL loaded flight plans and reset your progress. Are you absolutely sure? All progress will be lost.",
        icon: "keyboard_return",
        confirmText: "Reset All",
        onConfirm: () => {
            window.chrome.webview.postMessage({ action: "removeAllLegs" });
        }
    });
};

// ---- SIMBRIEF NATIVE INTEGRATION ----
window.openIntegratedSimBrief = () => {
    const modal = document.getElementById('simbriefDispatchModal');
    const iframe = document.getElementById('simbrief-iframe');
    if (!modal || !iframe) return;

    modal.style.display = 'flex';
    iframe.src = "https://dispatch.simbrief.com/options/custom";
};

window.closeIntegratedSimBrief = () => {
    const modal = document.getElementById('simbriefDispatchModal');
    const iframe = document.getElementById('simbrief-iframe');
    if (modal) modal.style.display = 'none';
    if (iframe) iframe.src = "about:blank";
};

window.triggerSimBriefImport = () => {
    // Automatically close the modal when importing
    window.closeIntegratedSimBrief();

    const user = localStorage.getItem('sbUsername') || '';
    let ffCln = localStorage.getItem('firstFlightClean') === "true";
    if (typeof currentDutyState !== 'undefined' && currentDutyState) {
        ffCln = (currentDutyState === 'pristine');
        localStorage.setItem('firstFlightClean', ffCln);
    }

    const loaderSt = document.getElementById('simbriefLoadingState');
    if (loaderSt) {
        loaderSt.style.display = 'flex';
        const lbl = document.getElementById('simbriefLoadingLabel');
        if (lbl) lbl.innerText = 'Downloading OFP into Application...';
    }

    window.chrome.webview.postMessage({
        action: 'fetch',
        username: user,
        remember: true,
        syncMsfsTime: document.getElementById('chkSyncTime') ? document.getElementById('chkSyncTime').checked : false,
        options: {
            groundSpeed: localStorage.getItem('groundSpeed') || 'Realistic',
            groundProb: localStorage.getItem('groundProb') || '25',
            firstFlightClean: ffCln,
            units: {
                weight: localStorage.getItem('selUnitWeight') || 'LBS',
                temp: localStorage.getItem('selUnitTemp') || 'C',
                alt: localStorage.getItem('selUnitAlt') || 'FT',
                speed: localStorage.getItem('selUnitSpeed') || 'KTS',
                press: localStorage.getItem('selUnitPress') || 'HPA',
                time: localStorage.getItem('selTimeFormat') || '24H'
            }
        }
    });
};
// --------------------------------------

window.renderBriefingTimeline = () => {
    const container = document.getElementById('briefing-timeline');
    const valArea = document.getElementById('briefing-validation-area');
    const emptyState = document.getElementById('briefing-empty-state');
    if (!container) return;

    let html = '';
    const rotations = window.allRotations || [];
    const currentIndex = window.dashboardActiveLegIndex || 0;
    const maxSlots = 6;
    
    // Empty state logic
    if (rotations.length === 0) {
        if (emptyState) emptyState.classList.remove('hidden');
        container.classList.add('hidden');
        if (valArea) valArea.classList.add('hidden');
        return;
    } else {
        if (emptyState) emptyState.classList.add('hidden');
        container.classList.remove('hidden');
        if (valArea) valArea.classList.remove('hidden');
    }

    // 1. Render filled slots (Vols)
    rotations.forEach((rot, i) => {
        if (i >= maxSlots) return;

        const rd = rot.data;
        const from = rd?.origin?.icao_code || '---';
        const fromName = rd?.origin?.name || '';
        const to = rd?.destination?.icao_code || '---';
        const toName = rd?.destination?.name || '';
        const isActive = (i === currentIndex);

        html += `
            <div id="timelineCard_${i}" class="w-60 md:w-72 h-[130px] bg-[#1C1F26]/80 rounded-xl border ${isActive ? 'border-zinc-500 shadow-[0_0_15px_rgba(255,255,255,0.05)]' : 'border-white/5 shadow-md'} flex flex-col p-4 relative overflow-hidden group cursor-pointer hover:border-white/10 transition-all flex-shrink-0 snap-center"
                 onclick="window.dashboardActiveLegIndex = ${i}; window.renderBriefingTimeline(); window.populateDashboardActiveLeg(${i}); window.populateBriefingView(${i});">
                
                <!-- Permanent Delete Button -->
                <div class="absolute top-3 right-3 z-20">
                    <button onclick="event.stopPropagation(); window.chrome.webview.postMessage({ action: 'removeLeg', payload: { index: ${i} } });" 
                            class="text-zinc-600 hover:text-white bg-black/20 hover:bg-red-500 rounded-lg p-1 transition-colors" title="Remove Leg">
                        <span class="material-symbols-outlined text-[16px]">delete</span>
                    </button>
                </div>

                <div class="absolute top-3 w-full text-center pointer-events-none left-0">
                    <span class="text-[9px] uppercase tracking-[0.2em] text-[#7b7b7b] font-bold">Leg ${i+1}</span>
                </div>
                
                <div class="flex items-center justify-between w-full mt-auto mb-1">
                    <div class="flex flex-col items-center flex-1">
                        <div class="text-2xl md:text-3xl font-black text-white tracking-widest leading-none mb-1">${from}</div>
                        <div class="text-[9px] text-[#7b7b7b] uppercase truncate w-20 text-center" title="${fromName}">${fromName}</div>
                    </div>
                    
                    <div class="flex flex-col items-center justify-center px-1 opacity-20">
                        <span class="material-symbols-outlined text-2xl">arrow_forward</span>
                    </div>
                    
                    <div class="flex flex-col items-center flex-1">
                        <div class="text-2xl md:text-3xl font-black text-white tracking-widest leading-none mb-1">${to}</div>
                        <div class="text-[9px] text-[#7b7b7b] uppercase truncate w-20 text-center" title="${toName}">${toName}</div>
                    </div>
                </div>
                
                <!-- Silver Active Indicator (Top Left, No Pulse) -->
                ${isActive ? '<div class="absolute top-3 left-3 w-2.5 h-2.5 bg-zinc-400 rounded-full shadow-[0_0_10px_rgba(255,255,255,0.2)]"></div>' : ''}
                
                <!-- Bottom Hover Bar (Monochrome) -->
                <div class="absolute inset-x-0 bottom-0 h-1 bg-zinc-600 transform scale-x-0 group-hover:scale-x-100 transition-transform origin-center"></div>
            </div>
        `;

        // Arrow (Subtle)
        html += `
            <div class="flex items-center justify-center">
                <span class="material-symbols-outlined text-white text-2xl">arrow_forward</span>
            </div>
        `;
    });

    // 2. Render "Add Flight" button in the next available slot
    if (rotations.length < maxSlots) {
        html += `
            <div class="w-60 md:w-72 h-[130px] bg-[#1C1F26]/40 rounded-xl border-2 border-dashed border-white/10 flex items-center justify-center gap-3 p-3 group hover:bg-white/10 hover:border-white/20 transition-all cursor-pointer shadow-md flex-shrink-0 snap-center"
                 onclick="window.openIntegratedSimBrief()">
                <span class="material-symbols-outlined text-3xl md:text-4xl text-zinc-400 group-hover:scale-110 transition-transform">add_circle</span>
                <span class="text-[10px] font-bold tracking-[0.15em] uppercase text-zinc-500 group-hover:text-zinc-200 transition-colors mt-0.5">Add Flight</span>
            </div>
        `;

        // Following Placeholders
        for (let i = rotations.length + 1; i < maxSlots; i++) {
            html += `
                <div class="flex items-center justify-center flex-shrink-0">
                    <span class="material-symbols-outlined text-white text-2xl">arrow_forward</span>
                </div>
                <div class="w-60 md:w-72 h-[130px] bg-[#1C1F26]/20 rounded-xl border border-dashed border-white/10 shadow-md flex items-center justify-center flex-shrink-0 snap-center">
                    <span class="material-symbols-outlined text-3xl md:text-4xl text-white/50">flight_takeoff</span>
                </div>
            `;
        }
    } else {
        // If 6 flights reached, just close the timeline nicely
        if (html.endsWith('arrow_forward</span>\n            </div>\n        ')) {
            html = html.substring(0, html.lastIndexOf('<div class="flex items-center justify-center">'));
        }
    }

    container.innerHTML = html;

    setTimeout(() => {
        const activeCard = document.getElementById(`timelineCard_${currentIndex}`);
        if (activeCard && container) {
            const containerCenter = container.offsetWidth / 2;
            const cardCenter = activeCard.offsetLeft + (activeCard.offsetWidth / 2);
            container.scrollTo({
                left: cardCenter - containerCenter,
                behavior: 'smooth'
            });
        }
    }, 50);
};

window.unlockDashboard = (silent = false) => {
    window.isDispatchSignedOff = true;
    
    const dashBtn = document.getElementById('navDashboardBtn');
    if (dashBtn) {
        dashBtn.classList.remove('opacity-30', 'cursor-not-allowed', 'pointer-events-none');
        dashBtn.classList.add('cursor-pointer', 'hover:bg-white/5', 'hover:text-sky-300', 'text-[#b6b6b6]');
        dashBtn.title = "Dashboard"; 
    }

    // Keep validation area visible, but disable buttons
    const valArea = document.getElementById('briefing-validation-area');
    if (valArea) {
        const valBtn = valArea.querySelector('button[onclick="window.unlockDashboard();"]');
        const clearBtn = valArea.querySelector('button[onclick="window.chrome.webview.postMessage({ action: \'clearAllRotations\' });"]');
        if (valBtn) valBtn.classList.add('opacity-30', 'cursor-not-allowed', 'pointer-events-none');
        if (clearBtn) clearBtn.classList.add('opacity-30', 'cursor-not-allowed', 'pointer-events-none');
    }

    // Populate Quick Load Sheet
    const qmBlock = document.getElementById('qmBlock');
    const qmCi = document.getElementById('qmCi');
    const qmCrz = document.getElementById('qmCrz');
    const qmZfw = document.getElementById('qmZfw');
    const qmTow = document.getElementById('qmTow');
    
    if (qmBlock) qmBlock.innerText = document.getElementById('modalFuelBlockField')?.innerText || '---';
    if (qmCi) qmCi.innerText = document.getElementById('dispCiField')?.innerText || '---';
    if (qmCrz) qmCrz.innerText = document.getElementById('dispFlField')?.innerText || '---';

    const index = window.dashboardActiveLegIndex || 0;
    if (window.allRotations && window.allRotations[index] && window.allRotations[index].data) {
        const rd = window.allRotations[index].data;
        if (qmZfw && rd.weights && rd.weights.est_zfw) qmZfw.innerText = rd.weights.est_zfw;
        if (qmTow && rd.weights && rd.weights.est_tow) qmTow.innerText = rd.weights.est_tow;
    }

    // Show Quick Load Sheet button
    const toggleBtn = document.getElementById('btnToggleLoadSheet');
    if (toggleBtn) toggleBtn.classList.remove('hidden');

    if (!silent && window.Swal) {
        Swal.fire({
            title: 'DISPATCH SIGNED OFF',
            text: 'Flight rotation validated. Dashboard is now accessible.',
            icon: 'success',
            background: '#1C1F26',
            color: '#f8fafc',
            confirmButtonColor: '#0ea5e9',
            timer: 2000
        });
        
        // Auto-switch to Dashboard Tab
        document.querySelectorAll('.sidebar button').forEach(b => b.classList.remove('active', 'text-white'));
        document.querySelectorAll('.tab-content').forEach(c => c.classList.remove('active'));
        if (dashBtn) dashBtn.classList.add('active', 'text-white');
        const dashTab = document.getElementById('dashboard');
        if (dashTab) dashTab.classList.add('active');
    }

    if (window.groundOpsCache && window.renderGroundOps) {
        window.renderGroundOps(window.groundOpsCache);
    }
};

window.renderBriefingTabs = () => {
    const pillsContainer = document.getElementById('briefingPills');
    const viewsContainer = document.getElementById('briefingViewsContainer');
    if (!pillsContainer || !viewsContainer) return;

    pillsContainer.innerHTML = '';
    viewsContainer.innerHTML = '';

    // Empty State: No flights loaded yet
    if (!window.allRotations || window.allRotations.length === 0) {
        const btnStartOps = document.getElementById('btnStartGroundOps');
        if (btnStartOps) {
            btnStartOps.disabled = true;
            btnStartOps.classList.add('opacity-30', 'cursor-not-allowed');
        }
        const gPnl = document.getElementById('manualGroundOpsPnl');
        if (gPnl) gPnl.style.display = 'none';

        pillsContainer.style.display = 'none';
        viewsContainer.innerHTML = `
                            <div class="flex flex-col items-center justify-center p-16 bg-[#1C1F26] rounded-xl border border-sky-500/20 border-dashed animate-fade-in text-center mt-6 shadow-[0_0_30px_rgba(14,165,233,0.05)]">
                                <span class="material-symbols-outlined text-6xl text-sky-500/50 mb-4 drop-shadow-[0_0_15px_rgba(14,165,233,0.5)]">route</span>
                                <h2 class="text-3xl font-black text-white tracking-widest uppercase mb-3">Build Your Rotation</h2>
                                <p class="text-slate-400 text-sm mb-10 max-w-lg leading-relaxed">
                                    You have not imported any flight plans yet. Fetch your first operational flight plan from SimBrief to initialize the cabin services.
                                </p>
                                <div class="flex flex-col items-center gap-4 w-full max-w-xs">
                                    <button onclick="window.chrome.webview.postMessage({ action: 'openSimbriefWindow' })" class="w-full bg-gradient-to-r from-sky-500 to-sky-400 hover:brightness-110 text-slate-900 font-bold px-8 py-5 rounded-xl shadow-[0_0_20px_rgba(14,165,233,0.3)] transition-all uppercase tracking-widest text-[12px] flex items-center justify-center gap-3">
                                        <span class="material-symbols-outlined text-xl">download</span>
                                        FETCH SIMBRIEF
                                </div>
                            </div>
                        `;
        return;
    }

    // Enable Ground Ops Button globally when rotation is active
    const globalBtnStartOps = document.getElementById('btnStartGroundOps');
    if (globalBtnStartOps) {
        globalBtnStartOps.disabled = false;
        globalBtnStartOps.classList.remove('opacity-30', 'cursor-not-allowed');
    }
    const btnCancel = document.getElementById('btnCancelRotations');
    if (btnCancel) {
        btnCancel.style.display = 'flex';
    }

    pillsContainer.innerHTML = '';
    viewsContainer.innerHTML = '';
    pillsContainer.style.display = 'flex'; // Restore Pills!

    const timeStr = (unixTimestamp) => {
        if (isNaN(unixTimestamp) || unixTimestamp <= 0) return '---';
        const offset = window.lastTelemetry?.globalTimeOffsetSeconds || 0;
        const d = new Date((unixTimestamp + offset) * 1000);
        return d.getUTCHours().toString().padStart(2, '0') + d.getUTCMinutes().toString().padStart(2, '0');
    };

    const convertWeight = (valStr) => {
        if (!valStr || valStr === '0' || valStr === '0.0') return '---';
        let val = parseFloat(valStr);
        if (isNaN(val)) return valStr;
        return Math.round(val);
    };

    const setActiveTab = (index) => {
        document.querySelectorAll('.btn-brief-pill').forEach((btn, i) => {
            if (i === index) {
                btn.classList.add('bg-sky-500/20', 'text-sky-400', 'border-sky-500/50');
                btn.classList.remove('bg-[#1C1F26]', 'text-slate-500', 'border-white/5');
            } else {
                btn.classList.remove('bg-sky-500/20', 'text-sky-400', 'border-sky-500/50');
                btn.classList.add('bg-[#1C1F26]', 'text-slate-500', 'border-white/5');
            }
        });
        document.querySelectorAll('.briefing-view').forEach((view, i) => {
            view.style.display = i === index ? 'block' : 'none';
        });
    };

    // ---- BUILD PILLS & VIEWS ----
    pillsContainer.style.display = 'none';

    let globalHtml = `<div class="briefing-view animate-fade-in" style="display:block;">
                        <div class="flex flex-col xl:flex-row gap-6 mb-6">
                            <div class="flex-1 bg-gradient-to-r from-[#1C1F26] to-[#12141A] p-8 rounded-xl border border-white/5 shadow-xl flex items-center gap-6">
                                <span class="material-symbols-outlined text-6xl text-sky-400 opacity-80">flight</span>
                                <div>
                                    <h3 class="text-[10px] uppercase tracking-[0.3em] font-bold text-slate-500 mb-1">Flight Briefing</h3>
                                    <h2 class="text-4xl font-black text-white font-headline tracking-widest uppercase">${window.AIRLINES[window.allRotations[0].data?.general?.icao_airline] || window.allRotations[0].data?.general?.airline_name || window.allRotations[0].data?.general?.icao_airline || 'AIRLINE'}</h2>
                                </div>
                            </div>
                            <div class="flex-1 bg-[#1C1F26] p-8 rounded-xl border border-white/5 shadow-xl flex items-center justify-between">
                                <div>
                                    <h3 class="text-[10px] bg-slate-800/50 px-2 py-1 inline-block rounded uppercase tracking-[0.3em] font-bold text-slate-400 mb-3 border border-white/5">Airframe</h3>
                                    <h2 class="text-2xl font-black text-white tracking-widest uppercase">${window.allRotations[0].data?.aircraft?.name || window.allRotations[0].data?.aircraft?.base_type || 'AIRCRAFT'}</h2>
                                    <p class="text-sky-400 font-mono text-sm mt-1 uppercase tracking-widest">${window.allRotations[0].data?.aircraft?.reg || 'PENDING'}</p>
                                </div>
                                <div class="text-right">
                                    <div class="text-[10px] font-bold uppercase tracking-widest text-emerald-500 mb-1">Global Rating</div>
                                    <div class="text-3xl font-black text-white">${window.allRotations[0].airlineProfile ? window.allRotations[0].airlineProfile.global_score : 100} <span class="text-xs text-slate-500">/100</span></div>
                                </div>
                            </div>
                        </div>

                        <div class="grid grid-cols-1 md:grid-cols-2 gap-6 mb-8">
                            <div class="bg-[#1C1F26] p-6 rounded-xl border border-white/5 shadow-xl">
                                <h3 class="text-[10px] text-sky-400 font-bold uppercase tracking-[0.2em] mb-4 flex items-center gap-2 border-b border-light pb-3">
                                    <span class="material-symbols-outlined text-[16px]">verified_user</span> Company Culture
                                </h3>
                                <p class="text-xs text-slate-300 leading-relaxed font-serif">
                                    Welcome to your shift. Operations prioritize a balance of strict on-time performance and premium cabin service. Please ensure block times are respected while providing passengers with a smooth experience. Any significant weather deviations should be communicated clearly via the Purser.
                                </p>
                            </div>
                            <div class="bg-[#1C1F26] p-6 rounded-xl border border-white/5 shadow-xl">
                                <h3 class="text-[10px] text-sky-400 font-bold uppercase tracking-[0.2em] mb-4 flex items-center gap-2 border-b border-light pb-3">
                                    <span class="material-symbols-outlined text-[16px]">schedule</span> Shift Overview
                                </h3>
                                <p class="text-xs text-slate-300 leading-relaxed">
                                    You are scheduled for a rotation of <strong class="text-white">${window.allRotations.length} legs</strong> today. Ensure adequate turnaround time between flights. Weather reports across the network currently appear manageable, but refer to individual leg operational briefings for critical Notams and precise meteorological impacts.
                                </p>
                            </div>
                        </div>

                        <h3 class="text-lg font-label tracking-[0.4em] text-white/80 uppercase mb-6 flex items-center gap-3 mt-10">
                            <span class="material-symbols-outlined text-sky-400 text-2xl opacity-80">route</span> LEGS DETAILS
                        </h3>
                        <div class="space-y-4" id="briefingLegsContainer">`;

    const curLeg = window.activeLegIndex || 0;
    window.allRotations.forEach((rot, idx) => {
        const rd = rot.data;
        const isPast = idx < curLeg;
        const isActive = idx === curLeg;

        let legStatusHtml = '';
        if (isPast) {
            legStatusHtml = `<span class="bg-slate-800/80 text-slate-500 font-bold px-3 py-1 rounded text-[10px] tracking-widest border border-slate-700/50 mt-2 inline-block">LEG COMPLETED</span>`;
        }

        globalHtml += `
                            <div draggable="${isActive ? 'false' : (isPast ? 'false' : 'true')}" data-index="${idx}" onclick="${isPast ? '' : `window.setBriefingTab(${idx + 1})`}" class="${!isActive && !isPast ? 'drag-leg-item' : ''} bg-gradient-to-r ${isPast ? 'from-[#111318] to-[#0A0C0F] opacity-30 grayscale pointer-events-none cursor-default' : 'from-[#171A21] to-[#12141A] cursor-pointer hover:from-sky-900/10 hover:to-[#171A21]'} transition-all p-6 rounded-xl border ${isActive ? 'border-sky-500/30 shadow-[0_0_15px_rgba(14,165,233,0.15)]' : 'border-white/5 shadow-lg'} flex items-center justify-between group relative">
                                <div class="flex items-center gap-6 pointer-events-none">
                                    <div class="flex items-center text-4xl font-black ${isActive ? 'text-sky-500/80' : 'text-slate-800/80 group-hover:text-sky-500/30'} transition-colors">
                                        ${isPast ? `<span class="material-symbols-outlined text-4xl mr-2 text-slate-700" title="Completed">check_circle</span>` :
                isActive ? `<span class="material-symbols-outlined text-4xl mr-2 text-sky-500/60" title="Active Leg (Locked)">lock</span>` :
                    `<span class="material-symbols-outlined text-4xl mr-2 cursor-grab active:cursor-grabbing text-slate-700 pointer-events-auto hover:text-white" title="Drag to reorder">drag_indicator</span>`}
                                        ${idx + 1}
                                    </div>
                                    <div>
                                        <div class="text-xl font-black text-white tracking-widest flex items-center gap-4">
                                            <span class="${isPast ? 'text-slate-500' : 'text-sky-400'}">${rd.origin?.icao_code || '---'}</span>
                                            <span class="material-symbols-outlined text-sm text-slate-500">arrow_forward</span>
                                            <span class="${isPast ? 'text-slate-500' : 'text-emerald-400'}">${rd.destination?.icao_code || '---'}</span>
                                        </div>
                                        <div class="text-xs font-mono text-slate-400 mt-2 uppercase tracking-widest">
                                            Flight ${rd.general?.icao_airline || ''}${rd.general?.flight_number || ''}
                                        </div>
                                    </div>
                                </div>
                                <div class="text-right flex flex-col items-end gap-2">
                                    <div class="flex items-center gap-4 bg-black/40 px-4 py-2 rounded-lg border border-white/5 font-mono text-[11px] ${isPast ? 'opacity-50' : ''}">
                                        <span class="text-slate-500">SOBT</span> <span class="text-slate-200">${timeStr(rd.times?.sched_out)}Z</span>
                                        <span class="text-slate-600">&bull;</span>
                                        <span class="text-slate-500">SIBT</span> <span class="text-slate-200">${timeStr(rd.times?.sched_in)}Z</span>
                                    </div>
                                    <div class="flex items-center gap-4">
                                        <div class="text-[11px] text-slate-500 font-bold tracking-widest uppercase mt-1 mr-2 flex flex-col items-end">
                                            <span>ETE ${rd.times?.est_time_enroute ? Math.floor(rd.times.est_time_enroute / 3600).toString().padStart(2, '0') + 'H' + Math.floor((rd.times.est_time_enroute % 3600) / 60).toString().padStart(2, '0') : '---'}</span>
                                            ${legStatusHtml}
                                        </div>
                                        ${isActive && ['A319', 'A320', 'A321', 'A20N'].includes(rd.aircraft?.base_type?.toUpperCase()) ? `
                                        <button onclick="event.stopPropagation(); window.chrome.webview.postMessage({action: 'fenixExport', path: localStorage.getItem('fenixExportPath'), jsonPayload: JSON.stringify(window.allRotations[${idx}].data)})"
                                                class="flex items-center gap-2 px-3 py-1 bg-[#1C1F26] hover:bg-amber-500/20 text-slate-500 hover:text-amber-500 text-[10px] font-bold tracking-widest uppercase border border-white/5 hover:border-amber-500/30 rounded transition-colors shadow-lg group-hover:block transition-all">
                                            <span class="material-symbols-outlined text-[14px]">save</span> EXPORT TO FENIX
                                        </button>` : ''}
                                    </div>
                                </div>
                            </div>
                        `;
    });

    globalHtml += `</div>
                                   <div class="mt-10 mb-10 flex justify-between items-center border-t border-white/5 pt-8">
                                        <button onclick="window.chrome.webview.postMessage({ action: 'openSimbriefWindow' })" class="bg-[#1C1F26] hover:bg-sky-900/20 text-sky-400 font-bold py-4 px-8 rounded-xl uppercase tracking-widest transition-all shadow-lg flex items-center border border-sky-500/30 hover:border-sky-400 gap-3 text-[11px] group">
                                            <span class="material-symbols-outlined text-[16px] group-hover:rotate-90 transition-transform">add</span> ADD FLIGHT LEG
                                        </button>
                                        
                                        <div class="flex items-center gap-6">
                                        </div>
                                   </div>
                               </div>`;

    viewsContainer.innerHTML += globalHtml;

    // ---- BUILD INDIVIDUAL LEG TABS ----
    window.allRotations.forEach((rot, i) => {
        const rd = rot.data;
        const orig = rd.origin?.icao_code || '---';
        const dest = rd.destination?.icao_code || '---';
        const acode = rd.general?.icao_airline || 'ZZZ';
        const pureAirlineName = AIRLINES[acode] ? AIRLINES[acode] : (rd.general?.airline_name || acode);
        const flightIdent = rd.general?.iata_airline ? `${acode}/${rd.general.iata_airline}${rd.general.flight_number}` : `${acode}/${rd.general.flight_number}`;

        let dispName = rd.aircraft?.name || rd.aircraft?.base_type || 'Unknown';
        if (rd.aircraft?.reg && !dispName.includes(rd.aircraft.reg)) dispName = `${rd.aircraft.reg} - ${dispName}`;

        let flightLevel = rd.general?.initial_alt || rd.general?.initial_altitude || '';
        let stepclimb = rd.general?.stepclimb_string || '';
        if (!flightLevel && stepclimb) {
            const parts = stepclimb.split('/');
            if (parts.length > 1) flightLevel = parts[parts.length - 1];
        }
        if (flightLevel) {
            let flNum = parseInt(flightLevel.toString().replace(/[^0-9]/g, ''), 10);
            if (!isNaN(flNum)) {
                if (flNum > 1000) flNum = Math.floor(flNum / 100);
                flightLevel = flNum.toString().padStart(3, '0');
            }
        }

        let eteSec = parseInt(rd.times?.est_time_enroute || '0');
        let h = Math.floor(eteSec / 3600);
        let m = Math.floor((eteSec % 3600) / 60);

        const convertWeight = (valStr) => {
            if (!valStr) return '';
            let val = parseFloat(valStr);
            if (isNaN(val)) return valStr;
            return Math.round(val);
        };

        const uiWeightUnit = document.getElementById('selUnitWeight') ? document.getElementById('selUnitWeight').value : 'LBS';
        let fuel = rd.fuel?.plan_ramp || rd.weights?.est_block || rd.weights?.block_fuel || '';

        let legHtml = `<div class="briefing-view animate-fade-in" style="display:none;">
                            
                            <!-- BACK TO GLOBAL NAVIGATION -->
                            <div class="mb-6 flex">
                                <button onclick="window.setBriefingTab(0)" class="flex items-center gap-2 text-slate-400 hover:text-white transition-all bg-[#1C1F26] hover:bg-sky-500/20 px-4 py-2 rounded-lg text-xs font-bold tracking-[0.2em] uppercase border border-white/5 hover:border-sky-500/50 shadow-sm cursor-pointer group">
                                    <span class="material-symbols-outlined text-[16px] group-hover:-translate-x-1 transition-transform">arrow_back</span>
                                    Return to Global Overview
                                </button>
                            </div>

                            <!-- EFB Flight Strip Header -->
                            <div class="flex flex-col bg-[#1C1F26] rounded-xl border border-white/10 overflow-hidden mb-6 shadow-2xl relative">
                                <div class="p-6 bg-gradient-to-r from-slate-900 to-[#12141A] flex flex-col xl:flex-row items-center justify-between gap-6 relative z-10">
                                    <div class="flex items-center gap-6">
                                        <div class="bg-sky-500/20 text-sky-400 font-bold px-4 py-2 rounded flex flex-col items-center justify-center border border-sky-500/30 text-2xl tracking-widest uppercase shadow-[0_0_15px_rgba(56,189,248,0.2)]">
                                            <span>${flightIdent}</span>
                                        </div>
                                        <div>
                                            <h2 class="text-3xl font-black font-headline text-white tracking-tighter drop-shadow-md">
                                                ${dispName}
                                            </h2>
                                            <div class="text-slate-400 mt-1 font-mono text-[11px] tracking-widest uppercase">
                                                Equipment: ${rd.aircraft?.base_type || '---'} &bull; Dist: ${rd.general?.route_distance || '---'} nm
                                            </div>
                                        </div>
                                    </div>
                                    
                                    <div class="flex-1 max-w-xl mx-auto w-full flex items-center justify-center gap-4 px-8 pt-4 xl:pt-0">
                                        <div class="text-center">
                                            <div class="text-2xl font-bold text-white tracking-widest">${orig}</div>
                                            <div class="font-mono text-[10px] text-slate-500 uppercase mt-1">SOBT ${timeStr(parseInt(rd.times?.sched_out || '0'))}Z</div>
                                        </div>
                                        <div class="flex-1 flex flex-col items-center">
                                            <div class="text-[10px] font-bold text-sky-400 uppercase tracking-[0.2em] mb-1 text-center bg-sky-900/40 px-3 py-1 rounded-full border border-sky-500/20">FL ${flightLevel}</div>
                                            <div class="w-full h-px border-t-2 border-dashed border-sky-500/40 relative flex items-center justify-center">
                                                <span class="material-symbols-outlined text-sky-400 absolute bg-black/40 px-2 text-[16px] rounded-full drop-shadow-[0_0_5px_rgba(56,189,248,0.5)]">flight</span>
                                            </div>
                                            <div class="text-[10px] text-slate-500 mt-1 uppercase font-bold tracking-widest">ETE ${h.toString().padStart(2, '0')}:${m.toString().padStart(2, '0')}</div>
                                        </div>
                                        <div class="text-center">
                                            <div class="text-2xl font-bold text-white tracking-widest">${dest}</div>
                                            <div class="font-mono text-[10px] text-slate-500 uppercase mt-1">SIBT ${timeStr(parseInt(rd.times?.sched_in || '0'))}Z</div>
                                        </div>
                                    </div>

                                    <div class="mt-4 xl:mt-0">
                                       <button onclick="requestAcarsUpdate()" class="px-5 py-3 bg-slate-800/80 hover:bg-slate-700 text-sky-400 font-bold rounded-lg border border-slate-600/50 flex items-center gap-2 transition-colors text-xs tracking-widest uppercase shadow-[0_0_10px_rgba(56,189,248,0.1)]">
                                           <span class="material-symbols-outlined text-[16px]">satellite_alt</span> ACARS WX
                                       </button>
                                    </div>
                                </div>
                                
                                <div class="bg-black/60 p-3 px-6 flex flex-wrap items-center justify-between gap-6 font-mono text-[10px] border-t border-white/5 relative z-10 w-full overflow-hidden">
                                    <div class="flex items-center gap-2 w-full">
                                        <span class="text-slate-500 uppercase tracking-widest flex-shrink-0">Routing</span> 
                                        <span class="text-emerald-400 font-bold whitespace-nowrap overflow-hidden text-ellipsis ml-2 flex-1" title="${rd.general?.route || ''}">${rd.general?.route || '---'}</span>
                                    </div>
                                </div>
                            </div>

                            `;

        if (rd.isDummy) {
            legHtml += `
                            <div class="bg-black/40 p-6 rounded-xl border-2 border-amber-500/30 flex items-center justify-between gap-6 font-mono mt-6 mb-6">
                                <div class="flex items-center gap-4">
                                    <span class="material-symbols-outlined text-amber-500 text-4xl animate-pulse">pending</span>
                                    <div>
                                        <h3 class="text-amber-500 font-bold tracking-widest uppercase">Dummy Leg â€” Awaiting OFP</h3>
                                        <p class="text-slate-400 text-xs mt-1">Please generate a SimBrief operational flight plan to dispatch this leg.</p>
                                    </div>
                                </div>
                                <button onclick="window.currentLegCounter = ${i + 1}; window.chrome.webview.postMessage({ action: 'openSimbriefWindow' })" class="bg-amber-500/20 hover:bg-amber-500/40 text-amber-500 border border-amber-500/50 transition-colors px-6 py-3 rounded-lg font-bold tracking-widest text-xs flex items-center gap-2">
                                    <span class="material-symbols-outlined text-[16px]">import_export</span> GENERATE SIMBRIEF
                                </button>
                            </div>
                            </div>`;
        } else {
            legHtml += `
                            <!-- Payload & Fuel Sheet -->
                            <div class="bg-[#1C1F26] p-6 rounded-xl border border-white/5 mb-6 shadow-xl relative overflow-hidden">
                                <div class="absolute inset-0 bg-gradient-to-br from-emerald-900/5 to-transparent pointer-events-none"></div>
                                <div class="relative z-10">
                                    <h3 class="text-sm font-label tracking-[0.4em] text-white uppercase border-b border-light pb-4 mb-4 flex items-center gap-2">
                                        <span class="material-symbols-outlined text-emerald-400">inventory_2</span> DISPATCH : PAYLOAD & FUEL SHEET
                                    </h3>

                                    <div class="grid grid-cols-1 md:grid-cols-2 gap-8">
                                        <div>
                                            <h4 class="text-[10px] font-bold uppercase text-slate-500 mb-3 tracking-widest">Weights & Payload</h4>
                                            <div class="space-y-2 font-mono text-xs">
                                                <div class="flex justify-between items-center bg-black/40 p-2 rounded"><span class="text-slate-400">Passengers (PAX)</span><strong class="text-emerald-400">${rd.weights?.pax_count || '0'} / ${rd.aircraft?.max_passengers || '---'}</strong></div>
                                                <div class="flex justify-between items-center bg-black/40 p-2 rounded"><span class="text-slate-400">Cargo / Freight</span><strong class="text-white">${convertWeight(rd.weights?.cargo)} ${uiWeightUnit}</strong></div>
                                                <div class="flex justify-between items-center bg-black/40 p-2 rounded"><span class="text-slate-400">Est. Zero Fuel Wgt (ZFW)</span><strong class="text-white">${convertWeight(rd.weights?.est_zfw)} ${uiWeightUnit}</strong></div>
                                                <div class="flex justify-between items-center bg-black/40 p-2 rounded"><span class="text-slate-400">Est. Take-Off Wgt (TOW)</span><strong class="text-amber-400">${convertWeight(rd.weights?.est_tow)} ${uiWeightUnit}</strong></div>
                                            </div>
                                        </div>

                                        <div>
                                            <h4 class="text-[10px] font-bold uppercase text-slate-500 mb-3 tracking-widest">Fuel Breakdown</h4>
                                             <div class="space-y-2 font-mono text-xs">
                                                <div class="flex justify-between items-center px-2 py-1"><span class="text-slate-400">Trip Fuel</span><strong class="text-white">${convertWeight(rd.fuel?.enroute_burn)}</strong></div>
                                                <div class="flex justify-between items-center px-2 py-1"><span class="text-slate-400">Contingency</span><strong class="text-white">${convertWeight(rd.fuel?.contingency)}</strong></div>
                                                <div class="flex justify-between items-center px-2 py-1"><span class="text-slate-400">Alternate</span><strong class="text-white">${convertWeight(rd.fuel?.alternate_burn)}</strong></div>
                                                <div class="flex justify-between items-center px-2 py-1"><span class="text-slate-400">Reserve</span><strong class="text-white">${convertWeight(rd.fuel?.reserve)}</strong></div>
                                                <div class="flex justify-between items-center px-2 py-1"><span class="text-slate-400">Extra / Captain</span><strong class="text-emerald-400">${convertWeight(rd.fuel?.extra)}</strong></div>
                                                <div class="flex justify-between items-center bg-black/40 p-2 rounded border border-white/5 mt-2"><span class="text-slate-400 uppercase font-bold tracking-widest">Block Fuel</span><strong class="text-sky-400 font-bold">${convertWeight(rd.fuel?.plan_ramp)} ${uiWeightUnit}</strong></div>
                                            </div>
                                        </div>
                                    </div>

                                    <div class="mt-4 text-[11px] font-serif italic text-emerald-200/90 p-3 bg-emerald-900/20 border-l-2 border-emerald-500/50 rounded-r shadow-inner">
                                        "Dispatch computation requires ${convertWeight(rd.fuel?.plan_ramp)} ${uiWeightUnit} of block fuel for this sector. This incorporates ${convertWeight(rd.fuel?.extra)} ${uiWeightUnit} of extra padding calculated based on routing complexities and destination weather margins."
                                    </div>

                                </div>
                            </div>
                            
                            <!-- Crew Operational Briefing (Weather/Enroute) -->
                            <div class="grid grid-cols-1 gap-6 mt-6">
                                <div class="bg-[#1C1F26] p-8 rounded-xl border border-white/5 shadow-xl">
                                    <h3 class="text-sm font-label tracking-[0.4em] text-white uppercase mb-6 border-b border-light pb-4 flex items-center gap-2">
                                        <span class="material-symbols-outlined text-white">cloud</span> CREW OPERATIONAL BRIEFING
                                    </h3>
                                    <!-- Dynamic text insertion has been refactored in populateBriefingView -->
                                </div>
                            </div>
                        </div>`;
        }

        viewsContainer.innerHTML += legHtml;
    });


    window.setBriefingTab = setActiveTab;
    // Auto-focus on the Global Overview (Index 0) instead of the latest leg
    setActiveTab(0);

    if (window.allRotations && window.allRotations.length > 1) {
        window.initBriefingDragAndDrop();
    }
};

window.initBriefingDragAndDrop = () => {
    const list = document.getElementById('briefingLegsContainer');
    if (!list) return;

    let draggedItemIdx = null;

    const items = list.querySelectorAll('.drag-leg-item');
    items.forEach(item => {
        item.addEventListener('dragstart', (e) => {
            draggedItemIdx = parseInt(item.getAttribute('data-index'));
            e.dataTransfer.effectAllowed = 'move';
            item.classList.add('opacity-50', 'border-sky-500/50');
        });
        item.addEventListener('dragover', (e) => {
            e.preventDefault();
            e.dataTransfer.dropEffect = 'move';
            item.classList.add('border-t-2', 'border-t-sky-400');
        });
        item.addEventListener('dragleave', () => {
            item.classList.remove('border-t-2', 'border-t-sky-400');
        });
        item.addEventListener('drop', (e) => {
            e.preventDefault();
            item.classList.remove('border-t-2', 'border-t-sky-400');
            const targetIdx = parseInt(item.closest('.drag-leg-item').getAttribute('data-index'));

            if (draggedItemIdx !== null && draggedItemIdx !== targetIdx) {
                // Swap/Reorder Array
                const movedItem = window.allRotations.splice(draggedItemIdx, 1)[0];
                window.allRotations.splice(targetIdx, 0, movedItem);

                // Recalculate block times based on new sequence
                window.recalculateAllLegTimes();
                window.renderBriefingTabs();
            }
        });
        item.addEventListener('dragend', () => {
            item.classList.remove('opacity-50', 'border-sky-500/50');
            draggedItemIdx = null;
        });
    });
};

window.recalculateAllLegTimes = () => {
    if (!window.allRotations || window.allRotations.length === 0) return;

    let currentSec = 0;
    const tatSeconds = 35 * 60; // 35 min Turnaround

    window.allRotations.forEach((rot, idx) => {
        const rd = rot.data;
        if (!rd.times) rd.times = {};

        if (idx === 0) {
            // First leg remains untouched. Base the next departure on this leg's arrival time + Turnaround.
            currentSec = parseInt(rd.times.sched_in || '0');
            if (currentSec <= 0) {
                currentSec = parseInt(rd.times.sched_out || '0') + parseInt(rd.times.est_time_enroute || '0');
            }
            if (currentSec <= 0) currentSec = Math.floor(Date.now() / 1000);
            currentSec += tatSeconds;
            return;
        }

        rd.times.sched_out = currentSec.toString();
        const ete = parseInt(rd.times.est_time_enroute || '0');
        currentSec += ete;
        rd.times.sched_in = currentSec.toString();
        currentSec += tatSeconds;
    });
};

document.addEventListener('DOMContentLoaded', () => {
    window.isFlightActive = false;
    window.airportsDb = {};
    fetch('airports.json')
        .then(res => res.json())
        .then(data => { window.airportsDb = data; console.log('Airports DB loaded.'); })
        .catch(e => console.error('Error loading Airports DB:', e));


    // --- UTILITIES ---
    window.saveLocalToggle = function (key, isChecked) {
        localStorage.setItem(key, isChecked ? 'true' : 'false');
    };

    // --- AUDIO STRINGING ENGINE ---
    class AudioQueue {
        constructor() {
            this.queue = [];
            this.isPlaying = false;

            // Single Audio Element
            this.audioElement = new Audio();

            // Web Audio API Context
            const AudioContext = window.AudioContext || window.webkitAudioContext;
            this.audioCtx = new AudioContext();

            // Nodes
            this.sourceNode = this.audioCtx.createMediaElementSource(this.audioElement);

            // Filter 1: Bandpass for Intercom effect (Phone/PA EQ)
            this.bandpass = this.audioCtx.createBiquadFilter();
            this.bandpass.type = 'bandpass';
            this.bandpass.frequency.value = 1200; // Center frequency
            this.bandpass.Q.value = 0.8; // Width

            // Filter 2: Highshelf to cut harsh high frequencies and simulate cheap speakers
            this.highshelf = this.audioCtx.createBiquadFilter();
            this.highshelf.type = 'highshelf';
            this.highshelf.frequency.value = 3500;
            this.highshelf.gain.value = -12;

            // Distortion (WaveShaper)
            this.distorter = this.audioCtx.createWaveShaper();
            this.distorter.curve = this.makeDistortionCurve(15); // Slight saturation / Crackle
            this.distorter.oversample = '4x';

            // Connections
            this.sourceNode.connect(this.bandpass);
            this.bandpass.connect(this.highshelf);
            this.highshelf.connect(this.distorter);
            this.distorter.connect(this.audioCtx.destination);

            this.audioElement.onended = () => {
                this.playNext();
            };
        }

        makeDistortionCurve(amount) {
            let k = typeof amount === 'number' ? amount : 50;
            let n_samples = 44100;
            let curve = new Float32Array(n_samples);
            let deg = Math.PI / 180;
            for (let i = 0; i < n_samples; ++i) {
                let x = i * 2 / n_samples - 1;
                curve[i] = (3 + k) * x * 20 * deg / (Math.PI + k * Math.abs(x));
            }
            return curve;
        }

        playSequence(sequence) {
            if (this.audioCtx.state === 'suspended') {
                this.audioCtx.resume();
            }
            if (!sequence || !Array.isArray(sequence) || sequence.length === 0) return;
            this.queue.push(...sequence);
            if (!this.isPlaying) {
                this.playNext();
            }
        }

        playNext() {
            if (this.queue.length === 0) {
                this.isPlaying = false;
                return;
            }
            this.isPlaying = true;
            const filename = this.queue.shift();

            if (!filename) {
                this.playNext();
                return;
            }

            // Fallback intelligence : si le MP3 Ã©choue, on tente la suite.
            this.audioElement.onerror = (e) => {
                console.warn(`[AudioEngine] Fichier introuvable ou erreur de lecture - ${filename}`);
                this.playNext(); // Failsafe
            };

            this.audioElement.src = `assets/sounds/${filename}`;
            this.audioElement.load();

            const playPromise = this.audioElement.play();
            if (playPromise !== undefined) {
                playPromise.catch(error => {
                    console.warn(`[AudioEngine] autoplay empÃªchÃ© ou erreur sur ${filename}.mp3`, error);
                    this.playNext();
                });
            }
        }
    }
    window.audioEngine = new AudioQueue();
    // ------------------------------
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


    window.isAppBooting = true; // BOOT GUARD V3: Strict lock for 5 seconds
    const menuItems = document.querySelectorAll('.menu li, li[data-target="profile"]');
    const sections = document.querySelectorAll('section');

    // Force initial state directly in DOM to avoid race conditions
    sections.forEach(sec => {
        if (sec.id === 'briefing') sec.classList.add('active');
        else sec.classList.remove('active');
    });
    menuItems.forEach(m => {
        if (m.getAttribute('data-target') === 'briefing') m.classList.add('active');
        else m.classList.remove('active');
    });

    menuItems.forEach(item => {
        item.addEventListener('click', () => {
            const targetId = item.getAttribute('data-target');
            if (!targetId) return;

            // Dashboard Protection Guard
            if (targetId === 'dashboard' && !window.isDispatchSignedOff) {
                // Already greyed out and pointer-events-none in HTML, 
                // but adding JS guard just in case or for dynamic updates
                return;
            }

            // Update Active Menu
            menuItems.forEach(m => m.classList.remove('active'));
            item.classList.add('active');

            // Update Active Section
            // INTERCEPTION : Ground Operations ouvre maintenant la fenêtre indépendante
            if (targetId === 'groundops') {
                if (window.chrome && window.chrome.webview) {
                    window.chrome.webview.postMessage({ action: 'openGroundOpsWindow' });
                }
                return; // Ne pas changer d'onglet
            }
            if (targetId === 'logbook') {
                window.chrome.webview.postMessage({ action: 'fetchLogbook' });
            }
            sections.forEach(sec => {
                if (sec.id === targetId) {
                    sec.classList.add('active');
                    if (targetId === 'briefing') {
                        window.renderBriefingTimeline();
                    }
                }
                else sec.classList.remove('active');
            });
        });
    });

    // Initial load: force briefing tab activation to sync UI state
    const navBriefing = document.getElementById('navBriefingBtn');
    if (navBriefing) {
        navBriefing.click();
    } else if (window.renderBriefingTimeline) {
        window.renderBriefingTimeline();
    }

    // V3: Safety Reset - If no rotations arrive within 2s of boot, clear signed-off state
    setTimeout(() => {
        if ((!window.allRotations || window.allRotations.length === 0) && window.isDispatchSignedOff) {
            console.log("[SYSTEM] No active rotation detected. Resetting sign-off state.");
            window.isDispatchSignedOff = false;
            const dashBtn = document.getElementById('navDashboardBtn');
            if (dashBtn) {
                dashBtn.classList.add('opacity-30', 'cursor-not-allowed', 'pointer-events-none');
                dashBtn.classList.remove('cursor-pointer', 'hover:text-white', 'text-[#b6b6b6]');
            }
        }
    }, 2000);

    // Release Boot Guard after 5 seconds to allow normal operational auto-switching
    setTimeout(() => {
        window.isAppBooting = false;
        console.log("[SYSTEM] Boot Guard V3 released. Auto-navigation enabled.");
        
        // Final enforce of Briefing if we are still starting up without legs
        if (!window.allRotations || window.allRotations.length === 0) {
            const navBriefing = document.getElementById('navBriefingBtn');
            if (navBriefing) navBriefing.click();
        }
    }, 5000);

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

        // Sync Ground Ops configurations with C#
        window.chrome.webview.postMessage({
            action: 'saveSettings',
            options: {
                groundSpeed: localStorage.getItem('groundSpeed') || 'Realistic',
                groundProb: localStorage.getItem('groundProb') || '25',
                firstFlightClean: localStorage.getItem('firstFlightClean') === 'true'
            }
        });
    }, 500);

    // Time Formatting
    window.getFormattedTime = function (unix) {
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

    window.getLocalFormattedTime = function () {
        if (window.simZuluTime) return window.simZuluTime + 'Z';
        const dt = new Date();
        const format = localStorage.getItem('selTimeFormat') || '24H';
        return dt.toLocaleTimeString('en-US', { hour: '2-digit', minute: '2-digit', second: '2-digit', hour12: format === '12H' }) + ' L';
    };
    let acarsTimeouts = [];

    window.acknowledgeFlightReport = function() {
        const modal = document.getElementById('flightReportModal');
        if(modal) modal.style.display = 'none';
        window.chrome.webview.postMessage({ action: 'acknowledgeFlightReport' });
    };

    window.cancelRotations = function () {
        location.reload();
    };

    window.cancelRotations = function () { location.reload(); };
    window.requestAcarsUpdate = function () {
        if (!window.allRotations || window.allRotations.length === 0) return;

        acarsTimeouts.forEach(t => clearTimeout(t));
        acarsTimeouts = [];

        let idx = window.activeLegIndex || 0;
        if (idx >= window.allRotations.length) idx = window.allRotations.length - 1;

        const rotation = window.allRotations[idx]?.data;
        if (!rotation) return;

        document.getElementById('acarsOrigin').innerText = rotation.origin?.icao_code || '----';
        document.getElementById('acarsDest').innerText = rotation.destination?.icao_code || '----';
        document.getElementById('acarsAltn').innerText = rotation.alternate?.icao_code || '----';

        document.getElementById('acarsStatus').style.display = 'none';
        document.getElementById('acarsScratchpad').innerText = '';

        const btnSend = document.getElementById('btnAcarsSend');
        if (btnSend) {
            btnSend.style.display = 'flex';
            btnSend.innerHTML = 'SEND REQ *';
            btnSend.disabled = false;
        }

        const btnClose = document.getElementById('btnAcarsClose');
        if (btnClose) btnClose.innerHTML = '&lt; CLOSE';

        document.getElementById('acarsModal').style.display = 'flex';
    };

    window.sendAcarsReq = function () {
        const btn = document.getElementById('btnAcarsSend');
        if (btn) btn.style.display = 'none'; // hide send button

        const statusStr = document.getElementById('acarsStatus');
        const scratchpad = document.getElementById('acarsScratchpad');

        if (statusStr) {
            statusStr.style.display = 'block';
            statusStr.innerText = 'SENDING...';
            statusStr.className = 'text-amber-500 text-sm animate-pulse w-full text-center tracking-[0.2em] font-bold h-6';
        }

        if (scratchpad) scratchpad.innerText = 'COMM ESTABLISHED...';

        acarsTimeouts.push(setTimeout(() => {
            if (statusStr) {
                statusStr.innerText = 'UPLINK IN PROGRESS';
                statusStr.classList.replace('text-amber-500', 'text-sky-400');
            }
            if (scratchpad) scratchpad.innerText = 'AOC MSG RCV...';

            acarsTimeouts.push(setTimeout(() => {
                if (window.chrome && window.chrome.webview) {
                    window.chrome.webview.postMessage({ action: 'acarsWeatherRequest' });
                }

                if (statusStr) {
                    statusStr.innerText = 'UPLINK COMPLETE';
                    statusStr.className = 'text-emerald-400 text-sm w-full text-center tracking-[0.2em] font-bold h-6';
                }
                if (scratchpad) scratchpad.innerText = 'WX DATA RECEIVED';

                const btnClose = document.getElementById('btnAcarsClose');
                if (btnClose) btnClose.innerHTML = '< EXIT';

            }, 3000));
        }, 2000));
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

    window.closeSystemMenu = function () {
        const sysMenu = document.getElementById('systemMenu');
        sysMenu.classList.add('opacity-0', 'pointer-events-none');
        sysMenu.classList.remove('opacity-100');
    }

    const renderActionButtons = (containerId, sectionId, options, colorClasses, type) => {
        const container = document.getElementById(containerId);
        const section = document.getElementById(sectionId);
        if (!container) return;

        let enabledOptions = options.filter(o => !o.disabled);

        if (enabledOptions.length === 0) {
            container.innerHTML = '';
            container.dataset.lastHtml = '';
            return;
        }

        const html = enabledOptions.map(o => {
            const action = o.action || (type === 'PA' ? 'announceCabin' : 'pncCommand');
            const propName = action === 'resolveCrisis' ? 'crisisType' : (type === 'PA' ? 'annType' : 'command');
            
            let onclickStr = '';
            if (type === 'PA') {
                onclickStr = `window.chrome.webview.postMessage({action: '${action}', ${propName}: '${o.val}'})`;
            } else {
                if (action === 'pncCommand') {
                    onclickStr = `window.chrome.webview.postMessage({action: '${action}', command: '${o.val}'})`;
                } else if (action === 'resolveCrisis') {
                    onclickStr = `window.chrome.webview.postMessage({action: '${action}', crisisType: '${o.val}'})`;
                } else {
                    onclickStr = `window.chrome.webview.postMessage({action: '${action}'})`;
                }
            }

            return `<button onclick="${onclickStr}" class="border rounded px-2.5 py-1 text-[10px] uppercase tracking-widest font-bold transition-all ${colorClasses}">
                        ${o.text}
                    </button>`;
        }).join('');

        if (container.dataset.lastHtml !== html) {
             container.innerHTML = html;
             container.dataset.lastHtml = html;
        }
    };

    function updateIntercomButtons(payload) {
        const phase = payload.phaseEnum;
        const used = payload.issuedIntercomCommands || [];

        // 1. FLIGHT DECK PA ACTIONS
        const paOptions = [];

        if (!used.includes('PA_Welcome') && phase === 'AtGate') {
            const boardingFinished = payload.passengers && payload.passengers.length > 0 ? payload.passengers.every(p => (p.IsBoarded !== undefined ? p.IsBoarded : p.isBoarded)) : false;
            const ok = phase === 'AtGate' && boardingFinished;
            paOptions.push({ val: 'Welcome', text: 'WELCOME', disabled: !ok });
        }
        if (!used.includes('PA_Approach') && ['Approach', 'FinalApproach'].includes(phase)) {
            const ok = phase === 'Approach';
            paOptions.push({ val: 'Approach', text: 'APPROACH', disabled: !ok });
        }

        if (!used.includes('PA_CruiseStatus')) {
            paOptions.push({ val: 'CruiseStatus', text: 'CRUISE', disabled: phase !== 'Cruise' });
        }
        if (!used.includes('PA_Descent') && ['Descent', 'Approach'].includes(phase)) {
            paOptions.push({ val: 'Descent', text: 'DESCENT' });
        }

        if (flightHasExperiencedDelay) {
            paOptions.push({ val: 'DelayApology', text: 'DELAY', disabled: false });
        }

        if (flightHasExperiencedTurbulence) {
            const isInAir = ['Takeoff', 'Climb', 'Cruise', 'Descent', 'Approach', 'FinalApproach'].includes(phase);
            paOptions.push({ val: 'TurbulenceApology', text: 'TURBULENCE', disabled: !isInAir });
        }

        if (payload.isGoAroundActive) {
            paOptions.push({ val: 'GoAround', text: 'GO-AROUND', disabled: false });
        }
        if (payload.isSevereTurbulenceActive && phase === 'Cruise') {
            paOptions.push({ val: 'Turbulence', text: 'SEVERE TURB', disabled: false });
        }
        if (payload.activeCrisis === 'MedicalEmergency') {
            paOptions.push({ val: 'MedicalEmergency', text: 'DOCTOR', disabled: false, action: 'resolveCrisis' });
        }

        // 2. FLIGHT DECK TO PNC ACTIONS
        const pncOptions = [];

        const diffSec = payload.cabinReportCooldownElapsed || 999;
        const isCd = diffSec < 120;
        
        pncOptions.push({
            val: 'intercomQuery',
            text: 'CABIN REPORT',
            disabled: isCd,
            action: 'intercomQuery'
        });

        if (!used.includes('ARM_DOORS') && ['AtGate', 'Pushback'].includes(phase)) {
            const ok = payload.isBoardingComplete;
            pncOptions.push({ val: 'ARM_DOORS', text: 'ARM DOORS', disabled: !ok, action: 'pncCommand' });
        }
        if (!used.includes('PREPARE_TAKEOFF') && phase === 'TaxiOut') {
            pncOptions.push({ val: 'PREPARE_TAKEOFF', text: 'PREP TAKEOFF', disabled: false, action: 'pncCommand' });
        }
        if (!used.includes('SEATS_TAKEOFF') && phase === 'TaxiOut' && used.includes('PREPARE_TAKEOFF')) {
            const isReady = payload.securingProgress >= 100;
            pncOptions.push({ val: 'SEATS_TAKEOFF', text: isReady ? 'SEATS TAKEOFF' : 'FORCE SEATS', disabled: false, action: 'pncCommand' });
        }
        if (!used.includes('TOP_DESCENT') && ['Cruise', 'Descent'].includes(phase)) {
            pncOptions.push({ val: 'TOP_DESCENT', text: 'TOP DESCENT', disabled: false, action: 'pncCommand' });
        }
        if (!used.includes('PREPARE_LANDING') && ['Cruise', 'Descent', 'Approach', 'FinalApproach'].includes(phase)) {
            const ok = payload.altitude <= 10000 && phase !== 'Cruise';
            pncOptions.push({ val: 'PREPARE_LANDING', text: 'PREP LANDING', disabled: !ok, action: 'pncCommand' });
        }
        if (!used.includes('SEATS_LANDING') && ['Descent', 'Approach'].includes(phase)) {
            const ok = phase === 'Approach' || payload.altitude <= 5000;
            const isReady = payload.securingProgress >= 100;
            pncOptions.push({ val: 'SEATS_LANDING', text: isReady ? 'SEATS LANDING' : 'FORCE SEATS', disabled: !ok, action: 'pncCommand' });
        }
        if (payload.cabinState === 'ServingMeals') {
            const svcText = payload.isServiceHalted ? 'RESUME SVC' : 'PAUSE SVC';
            pncOptions.push({ val: 'toggleService', text: svcText, disabled: false, action: 'toggleService' });
        }
        if (payload.activeCrisis === 'UnrulyPassenger') {
            pncOptions.push({ val: 'UnrulyPassenger', text: 'RESTRAIN PAX', disabled: false, action: 'resolveCrisis' });
        }

        renderActionButtons('paButtonsContainer', 'paSection', paOptions, 'bg-sky-900/40 text-sky-400 border-sky-500/20 hover:bg-sky-800/60 shadow-[0_0_10px_rgba(14,165,233,0.1)] hover:shadow-[0_0_15px_rgba(14,165,233,0.2)]', 'PA');
        renderActionButtons('pncButtonsContainer', 'pncSection', pncOptions, 'bg-amber-900/40 text-amber-400 border-amber-500/20 hover:bg-amber-800/60 shadow-[0_0_10px_rgba(245,158,11,0.1)] hover:shadow-[0_0_15px_rgba(245,158,11,0.2)]', 'PNC');
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

    const savedFFClean = localStorage.getItem('firstFlightClean');
    if (savedFFClean !== null && document.getElementById('chkFirstFlightClean')) {
        document.getElementById('chkFirstFlightClean').checked = (savedFFClean === 'true');
    }

    const savedSyncTime = localStorage.getItem('chkSyncTime');
    if (savedSyncTime === 'true' && document.getElementById('chkSyncTime')) {
        document.getElementById('chkSyncTime').checked = true;
        if (document.getElementById('lblSyncTimeMode')) document.getElementById('lblSyncTimeMode').innerText = 'MSFS SIM';
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

    const savedFenixPath = localStorage.getItem('fenixExportPath');
    if (savedFenixPath !== null && document.getElementById('fenixExportPath')) {
        document.getElementById('fenixExportPath').value = savedFenixPath;
    }

    // Save Settings
    const btnSaveSettings = document.getElementById('btnSaveSettings');
    if (btnSaveSettings) {
        btnSaveSettings.addEventListener('click', () => {
            const username = document.getElementById('sbUsername') ? document.getElementById('sbUsername').value : '';
            const groundSpeed = document.getElementById('selGroundOpsSpeed') ? document.getElementById('selGroundOpsSpeed').value : 'Realistic';
            const groundProb = document.getElementById('rngProb') ? document.getElementById('rngProb').value : '25';
            const weatherSrc = document.getElementById('selWeatherSource') ? document.getElementById('selWeatherSource').value : 'SimBrief';
            const fenixPath = document.getElementById('fenixExportPath') ? document.getElementById('fenixExportPath').value : '';
            const gsxSync = document.getElementById('chkGsxSync') ? document.getElementById('chkGsxSync').checked : false;
            const savedFfc = localStorage.getItem('firstFlightClean');
            const ffClean = document.getElementById('chkFirstFlightClean') ? document.getElementById('chkFirstFlightClean').checked : (savedFfc === 'true');

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
            localStorage.setItem('firstFlightClean', ffClean);

            const isTop = document.getElementById('chkAlwaysOnTop') ? document.getElementById('chkAlwaysOnTop').checked : false;
            localStorage.setItem('chkAlwaysOnTop', isTop);
            if (document.getElementById('btnPin')) {
                document.getElementById('btnPin').style.opacity = isTop ? '1' : '0.4';
            }
            window.chrome.webview.postMessage({ action: 'setAlwaysOnTop', value: isTop });

            if (username) localStorage.setItem('sbUsername', username);
            localStorage.setItem('groundSpeed', groundSpeed);
            window.chrome.webview.postMessage({ action: 'updateGroundSpeed', value: groundSpeed });
            localStorage.setItem('groundProb', groundProb);
            window.chrome.webview.postMessage({
                action: 'saveSettings',
                options: {
                    groundSpeed: groundSpeed,
                    groundProb: groundProb,
                    firstFlightClean: ffClean
                }
            });
            localStorage.setItem('weatherSource', weatherSrc);
            localStorage.setItem('gsxSync', gsxSync);
            localStorage.setItem('fenixExportPath', fenixPath);

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
                    window.chrome.webview.postMessage({ action: 'updateProfileField', field: 'AvatarPosition', value: '50% 50%' });
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

    const btnSaveIdentity = document.getElementById('btnSaveIdentity');
    if (btnSaveIdentity) {
        btnSaveIdentity.addEventListener('click', () => {
            const fieldsToSave = [
                { id: 'prfCallsign', field: 'CallSign' },
                { id: 'prfFullName', field: 'FullName' },
                { id: 'prfHomeBase', field: 'HomeBaseIcao' },
                { id: 'prfCountry', field: 'CountryCode' }
            ];
            fieldsToSave.forEach(pf => {
                const el = document.getElementById(pf.id);
                if (el) {
                    let v = el.innerText.trim();
                    if (pf.field === 'HomeBaseIcao') v = v.toUpperCase();
                    if (pf.field === 'CountryCode') v = v.toUpperCase();
                    window.chrome.webview.postMessage({
                        action: 'updateProfileField',
                        field: pf.field,
                        value: v
                    });
                }
            });

            const callsign = document.getElementById('prfCallsign')?.innerText.trim() || 'MAVERICK';
            const sbCallsign = document.getElementById('sbProfileCallsign');
            if (sbCallsign) sbCallsign.innerText = callsign;

            // Visual feedback
            const originalText = btnSaveIdentity.innerText;
            btnSaveIdentity.innerText = 'SAVED!';
            btnSaveIdentity.classList.remove('text-emerald-400', 'border-emerald-500/30');
            btnSaveIdentity.classList.add('text-white', 'border-white', 'bg-emerald-600/80');
            setTimeout(() => {
                btnSaveIdentity.innerText = originalText;
                btnSaveIdentity.classList.add('text-emerald-400', 'border-emerald-500/30');
                btnSaveIdentity.classList.remove('text-white', 'border-white', 'bg-emerald-600/80');
            }, 1500);
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

    // Connected to Simulator (Abort Modal Removed)
    
    const btnDismissReport = document.getElementById('btnDismissReport');
    if (btnDismissReport) btnDismissReport.addEventListener('click', () => {
        document.getElementById('flightReportModal').style.display = 'none';
        window.chrome.webview.postMessage({ action: 'acknowledgeDebrief' });
    });

    let currentDutyState = null;

    window.selectDutyState = function (state) {
        currentDutyState = state;
        const pristine = document.getElementById('cardPristine');
        const turnaround = document.getElementById('cardTurnaround');
        if (!pristine || !turnaround) return;

        pristine.classList.remove('border-sky-500', 'bg-sky-900/10');
        pristine.classList.add('border-white/5', 'bg-[#12141A]', 'opacity-50');
        turnaround.classList.remove('border-orange-500', 'bg-orange-500/10', 'hover:border-orange-500/50');
        turnaround.classList.add('border-white/5', 'bg-[#12141A]', 'opacity-50');

        if (state === 'pristine') {
            pristine.classList.add('border-sky-500', 'bg-sky-900/10');
            pristine.classList.remove('border-white/5', 'bg-[#12141A]', 'opacity-50');
        } else {
            turnaround.classList.add('border-orange-500', 'bg-orange-500/10', 'hover:border-orange-500/50');
            turnaround.classList.remove('border-white/5', 'bg-[#12141A]', 'opacity-50');
        }
    };

    if (btnFetchPlan) {
        btnFetchPlan.addEventListener('click', () => {
            if (window.isFlightActive) {
                if (window.flightPhase === 'Turnaround' || window.flightPhase === 'AtGate') {
                    // Send to backend to advance the leg queue
                    window.chrome.webview.postMessage({ action: 'prepareNextLeg' });
                    return;
                }
                if (cancelModal) cancelModal.style.display = 'flex';
                return;
            }

            const dutyModal = document.getElementById('dutySetupModal');
            if (dutyModal) {
                const sbUser = localStorage.getItem('sbUsername') || '';
                const unInput = document.getElementById('dutySbUsername');
                if (unInput) unInput.value = sbUser;

                if (!currentDutyState) selectDutyState('pristine');

                dutyModal.style.display = 'flex';
            }
        });
    }


    window.currentLegCounter = 1;

    window.plannedDummyLegs = [];
    window.currentDutyMode = 'custom';
    window.predefinedRosters = null;
    window.selectedRosterId = null;

    window.setStepMode = (mode) => {
        window.currentDutyMode = mode;
        const btnCustom = document.getElementById('btnModeCustom');
        const btnRoster = document.getElementById('btnModeRoster');
        const viewCustom = document.getElementById('viewModeCustom');
        const viewRoster = document.getElementById('viewModeRoster');

        if (mode === 'custom') {
            btnCustom.classList.add('bg-sky-500/20', 'text-sky-400', 'border-sky-500/50', 'shadow-[0_0_15px_rgba(14,165,233,0.2)]');
            btnCustom.classList.remove('text-slate-500', 'border-transparent');
            btnRoster.classList.remove('bg-sky-500/20', 'text-sky-400', 'border-sky-500/50', 'shadow-[0_0_15px_rgba(14,165,233,0.2)]');
            btnRoster.classList.add('text-slate-500', 'border-transparent');
            viewCustom.style.display = 'grid';
            viewRoster.style.display = 'none';
        } else {
            btnRoster.classList.add('bg-sky-500/20', 'text-sky-400', 'border-sky-500/50', 'shadow-[0_0_15px_rgba(14,165,233,0.2)]');
            btnRoster.classList.remove('text-slate-500', 'border-transparent');
            btnCustom.classList.remove('bg-sky-500/20', 'text-sky-400', 'border-sky-500/50', 'shadow-[0_0_15px_rgba(14,165,233,0.2)]');
            btnCustom.classList.add('text-slate-500', 'border-transparent');
            viewCustom.style.display = 'none';
            viewRoster.style.display = 'flex';

            if (!window.predefinedRosters) {
                fetch('./data/predefined_rotations.json')
                    .then(res => res.json())
                    .then(data => {
                        window.predefinedRosters = data;
                        window.updateRosterUI();
                    })
                    .catch(err => console.error("Failed to load predefined rotations", err));
            } else {
                window.updateRosterUI();
            }
        }
    };

    window.selectRoster = (id) => {
        window.selectedRosterId = id;
        document.querySelectorAll('.roster-card').forEach(card => {
            if (card.dataset.id === id) {
                card.classList.add('border-emerald-500', 'bg-emerald-900/20', 'shadow-[0_0_15px_rgba(16,185,129,0.3)]');
                card.classList.remove('border-white/5', 'bg-[#1C1F26]', 'opacity-40');
            } else {
                card.classList.remove('border-emerald-500', 'bg-emerald-900/20', 'shadow-[0_0_15px_rgba(16,185,129,0.3)]');
                card.classList.add('border-white/5', 'bg-[#1C1F26]', 'opacity-40');
            }
        });
    };

    window.updateRosterUI = () => {
        const grid = document.getElementById('rosterGrid');
        if (!grid || !window.predefinedRosters) return;

        const airline = document.getElementById('rosterSelAirline').value;
        const hub = document.getElementById('rosterSelHub').value;

        const rotations = window.predefinedRosters[airline] && window.predefinedRosters[airline][hub] ? window.predefinedRosters[airline][hub] : [];

        grid.innerHTML = '';
        if (rotations.length === 0) {
            grid.innerHTML = '<div class="text-slate-500 text-xs italic col-span-full py-8">No rotations found for this selection.</div>';
            return;
        }

        rotations.forEach(rot => {
            let icon = rot.type === 'Classic' ? 'schedule' : 'warning';
            let color = rot.type === 'Classic' ? 'text-sky-400' : 'text-orange-400';
            let diffStars = '';
            for (let i = 0; i < 5; i++) {
                diffStars += `<span class="material-symbols-outlined text-[10px] ${i < rot.difficulty ? 'text-amber-400' : 'text-slate-700'}">star</span>`;
            }

            const routeHtml = rot.legs.join('<span class="material-symbols-outlined text-[10px] text-slate-600 mx-1 relative top-[1px]">navigate_next</span>');

            const isSelected = window.selectedRosterId === rot.id;
            const extraClasses = isSelected ? 'border-emerald-500 bg-emerald-900/20 shadow-[0_0_15px_rgba(16,185,129,0.3)]' : 'border-white/5 bg-[#1C1F26] opacity-40';

            grid.innerHTML += `
                <div class="roster-card cursor-pointer p-4 rounded-xl border transition-all hover:opacity-100 hover:border-emerald-500/50 flex flex-col items-start text-left relative overflow-hidden ${extraClasses}"
                     data-id="${rot.id}" onclick="window.selectRoster('${rot.id}')">
                    <div class="flex items-center justify-between w-full mb-2">
                        <div class="flex items-center gap-1 bg-black/40 px-2 py-0.5 rounded border border-white/5 shadow-inner">
                            <span class="material-symbols-outlined text-[12px] ${color}">${icon}</span>
                            <span class="text-[9px] uppercase tracking-widest font-bold ${color}">${rot.type}</span>
                        </div>
                        <div class="flex gap-[1px] bg-black/40 px-1 py-0.5 rounded border border-white/5 shadow-inner">${diffStars}</div>
                    </div>
                    <div class="text-white font-bold text-sm mb-1 pr-2 leading-tight">${rot.title}</div>
                    <div class="text-slate-400 text-[9px] font-mono leading-relaxed mb-3 flex flex-wrap items-center">${routeHtml}</div>
                    <div class="text-slate-500 text-[10px] leading-snug flex-grow">${rot.description}</div>
                </div>
            `;
        });

        let found = false;
        if (window.selectedRosterId) {
            found = rotations.some(r => r.id === window.selectedRosterId);
        }
        if (!found && rotations.length > 0) {
            window.selectRoster(rotations[0].id);
        }
    };


    // --- HAVERSINE DUMMY ETE ESTIMATOR ---
    function getDummyLeg(originIcao, destIcao) {
        if (!window.airportsDb || !window.airportsDb[originIcao] || !window.airportsDb[destIcao]) return null;

        const deg2rad = deg => deg * (Math.PI / 180);
        const lat1 = deg2rad(window.airportsDb[originIcao].lat);
        const lon1 = deg2rad(window.airportsDb[originIcao].lon);
        const lat2 = deg2rad(window.airportsDb[destIcao].lat);
        const lon2 = deg2rad(window.airportsDb[destIcao].lon);

        const dLat = lat2 - lat1;
        const dLon = lon2 - lon1;
        const a = Math.sin(dLat / 2) * Math.sin(dLat / 2) + Math.cos(lat1) * Math.cos(lat2) * Math.sin(dLon / 2) * Math.sin(dLon / 2);
        const c = 2 * Math.atan2(Math.sqrt(a), Math.sqrt(1 - a));
        const R = 3440.065; // Earth radius in Nautical Miles
        const distanceNM = R * c;

        // 420 KTAS average speed + 30 mins maneuvering penalty (SID/STAR)
        const dummyEteMinutes = Math.round((distanceNM / 420) * 60 + 30);

        return {
            isDummy: true,
            origin: {
                icao_code: originIcao,
                name: window.airportsDb[originIcao].name,
                plan_elevation: window.airportsDb[originIcao].elevation
            },
            destination: {
                icao_code: destIcao,
                name: window.airportsDb[destIcao].name,
                plan_elevation: window.airportsDb[destIcao].elevation
            },
            alternate: { icao_code: "NONE" },
            times: {
                est_time_enroute: dummyEteMinutes * 60
            },
            aircraft: {
                icaocode: "A320",
                name: "Airbus A320-200",
                reg: "PENDING"
            },
            general: {
                flight_number: "9999",
                icao_airline: "EZY",
                initial_alt: "FL350",
                route: "DCT"
            },
            weights: {
                est_zfw: 60000,
                est_tow: 70000,
                est_ldw: 65000,
                block_fuel: 10000,
                pax_count: 150
            }
        };
    }

    window.populateRecentIcaos = function () {
        const dl = document.getElementById('recentIcaos');
        if (!dl) return;
        dl.innerHTML = '';
        const recent = JSON.parse(localStorage.getItem('recentIcaos') || '[]');
        recent.forEach(icao => {
            const opt = document.createElement('option');
            opt.value = icao;
            dl.appendChild(opt);
        });
    };

    // Call once on initialized
    window.populateRecentIcaos();

    const btnCancelDispatch = document.getElementById('btnCancelDispatch');
    if (btnCancelDispatch) {
        btnCancelDispatch.addEventListener('click', () => {
            document.getElementById('simbriefDispatchModal').style.display = 'none';
        });
    }


    const btnFinishDispatch = document.getElementById('btnFinishDispatch');
    if (btnFinishDispatch) {
        btnFinishDispatch.addEventListener('click', () => {
            const dispatchModal = document.getElementById('simbriefDispatchModal');
            if (dispatchModal) dispatchModal.style.display = 'none';

            let sbPayloadStr = "[]";
            try {
                let sbPayload = [];
                if (window.allRotations && window.allRotations.length > 0) {
                    sbPayload = window.allRotations.map(r => r.data);
                    if (window.activeLegIndex) {
                        sbPayload = sbPayload.slice(window.activeLegIndex);
                    }
                }
                sbPayloadStr = JSON.stringify(sbPayload);
                window.chrome.webview.postMessage({ action: 'syncRotationsAndStart', payloadStr: sbPayloadStr });
                // Reset footer states
                btnFinishDispatch.classList.add('opacity-50', 'cursor-not-allowed', 'pointer-events-none');
                
                // BUG FIX: Automatically view the first leg in Briefing when closing dispatch
                window.dashboardActiveLegIndex = 0;
                if (window.populateDashboardActiveLeg) window.populateDashboardActiveLeg(0);
                if (window.populateBriefingView) window.populateBriefingView(0);
                if (window.renderBriefingTimeline) window.renderBriefingTimeline();

                window.chrome.webview.postMessage({ action: 'finishDispatch' });
            } catch (err) {
                console.error("Failed to stringify or send payload", err);
                // Fallback to finishDispatch anyway to unblock the UI
                window.chrome.webview.postMessage({ action: 'finishDispatch' });
            }
        });
    }

    // Connect to Simulator
    let isSimConnected = false;
    var lastSimTime = null;
    window.locationMismatchModalShown = false;
    const btnSmartConnect = document.getElementById('btnSmartConnect');
    if (btnSmartConnect) {
        btnSmartConnect.addEventListener('click', () => {
            window.chrome.webview.postMessage({ action: isSimConnected ? 'disconnectSim' : 'connectSim' });
            if (!isSimConnected) {
                btnSmartConnect.innerHTML = '<span class="material-symbols-outlined text-[18px]">wifi_find</span>';
                btnSmartConnect.title = 'Connecting...';
                btnSmartConnect.className = 'flex items-center justify-center w-10 h-10 rounded-xl bg-orange-900/20 text-orange-400 border border-orange-500/20 shadow-[0_0_15px_rgba(249,115,22,0.1)] transition-colors cursor-wait';
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
    let flightHasExperiencedDelay = false;
    let flightHasExperiencedTurbulence = false;

    // WebView2 Global Message Receiver
    window.chrome.webview.addEventListener('message', event => {
        const payload = event.data;
        if (!payload || !payload.type) return;

        switch (payload.type) {
            case 'simbriefWindowClosed':
                window.triggerSimBriefImport();
                break;
            case 'simbriefPlanReady':
                if (window.showSimbriefStatus) window.showSimbriefStatus('OFP Generation Detected. Waiting for SimBrief...', 'emerald');
                
                // Add a 6-second delay to allow SimBrief's backend to finish generating the OFP
                // before our application fetches it via the API.
                setTimeout(() => {
                    window.triggerSimBriefImport();
                }, 6000);
                break;
            case 'gatekeeperPassed':
                window.chrome.webview.postMessage({
                    action: 'syncRotationsAndStart',
                    payload: window.gatekeeperPendingPayload || []
                });
                const btnPass = document.getElementById('btnStartGroundOps');
                if (btnPass) {
                    btnPass.innerHTML = '<span class="material-symbols-outlined text-[18px]">flight_takeoff</span> In Progress';
                }
                const groundOpsTarget = document.querySelector('.menu li[data-target="groundops"]');
                if (groundOpsTarget) groundOpsTarget.click();
                break;
            case 'gatekeeperFailed':
                // Use SweetAlert to show the user instead of a generic browser alert
                if (window.Swal) {
                    Swal.fire({
                        title: 'ACTION DENIED',
                        text: payload.reason || 'Launch MSFS first and start a flight in Cold & Dark state (Engines OFF, On Ground).',
                        icon: 'error',
                        background: '#1C1F26',
                        color: '#f8fafc',
                        confirmButtonColor: '#0ea5e9'
                    });
                } else {
                    alert("ACTION DENIED: " + (payload.reason || "Launch MSFS first and start a flight in Cold & Dark state (Engines OFF, On Ground)."));
                }
                const btnFail = document.getElementById('btnStartGroundOps');
                if (btnFail) {
                    btnFail.disabled = false;
                    btnFail.innerHTML = '<span class="material-symbols-outlined text-[18px]">flight_takeoff</span> START OPS';
                }
                break;
            case 'manifest':
                window.manifest = payload.manifest;
                if (window.manifest && document.getElementById('cabin') && document.getElementById('cabin').classList.contains('active')) {
                    if (typeof window.renderManifest === 'function') {
                        window.renderManifest(window.manifest);
                    }
                }
                break;
            case 'briefingUpdate':
                if (payload.briefing && window.allRotations && window.allRotations[window.activeLegIndex || 0]) {
                    window.allRotations[window.activeLegIndex || 0].briefing = payload.briefing;
                }
                if (window.populateBriefingView) {
                    window.populateBriefingView(window.dashboardActiveLegIndex || 0);
                }
                // Refresh Timeline whenever we get briefing data
                if (window.renderBriefingTimeline) window.renderBriefingTimeline();

                // Unlock Dashboard if not already unlocked (User requested unlock as soon as data is entered)
                if (!window.isDispatchSignedOff && payload.briefing) {
                    // Manual sign-off is now required via button, but we ensure logic is ready
                    // window.unlockDashboard(); // Keep locked until manual signoff
                }
                break;
            case 'fuelValidationRejected':
                // STORY 38: RESET BUTTON AND SHOW MODAL
                if (window.Swal) {
                    Swal.fire({
                        title: 'ACTION BLOQUÉE',
                        text: payload.message || 'Validation impossible.',
                        icon: 'warning',
                        background: '#1C1F26',
                        color: '#f8fafc',
                        confirmButtonColor: '#0ea5e9'
                    });
                }
                const fvBtn = document.getElementById('fuelValidateBtn');
                const fvBtnText = document.getElementById('fuelValidateBtnText');
                if (fvBtn) {
                    fvBtn.classList.remove('bg-emerald-500/20', 'text-emerald-400', 'border-emerald-500/50', 'cursor-not-allowed', 'shadow-[0_0_20px_rgba(16,185,129,0.15)]');
                    fvBtn.classList.add('bg-sky-500/20', 'text-sky-400', 'hover:bg-sky-500', 'hover:text-white', 'border-sky-500/50', 'shadow-[0_0_20px_rgba(14,165,233,0.15)]', 'group', 'group-hover:scale-105');
                    if (fvBtnText) fvBtnText.innerText = "Validate & Sign";
                    const fvIcon = fvBtn.querySelector('.material-symbols-outlined');
                    if (fvIcon) fvIcon.innerText = "verified_user";
                    fvBtn.onclick = () => { if(window.requestFuelValidation) window.requestFuelValidation(window.dashboardActiveLegIndex || 0); };
                }
                break;
            case 'flightReset':
                window.currentFlight = null;
                window.manifest = null;
                window.dashboardActiveLegIndex = 0;
                if (window.populateDashboardActiveLeg) window.populateDashboardActiveLeg();
                if (window.resetDashboardWidgets) window.resetDashboardWidgets();
                if (window.renderBriefingTimeline) window.renderBriefingTimeline();
                break;
            case 'rotationCleared':
                window.allRotations = [];
                window.activeLegIndex = 0;
                window.dashboardActiveLegIndex = 0;
                window.isDispatchSignedOff = false;
                if (window.populateDashboardActiveLeg) window.populateDashboardActiveLeg();
                if (window.resetDashboardWidgets) window.resetDashboardWidgets();
                if (window.renderBriefingTimeline) window.renderBriefingTimeline();
                
                // Re-lock dashboard
                const dashBtn = document.getElementById('navDashboardBtn');
                if (dashBtn) {
                    dashBtn.classList.add('opacity-30', 'cursor-not-allowed', 'pointer-events-none');
                    dashBtn.classList.remove('cursor-pointer', 'hover:text-white', 'text-[#b6b6b6]');
                }
                break;
            case 'removeLegAtIndex':
                if (window.allRotations && payload.index !== undefined) {
                    window.allRotations.splice(payload.index, 1);
                    // Reset view to active leg if we were viewing the deleted one or if indices shifted
                    window.dashboardActiveLegIndex = 0; 
                    if (window.populateDashboardActiveLeg) window.populateDashboardActiveLeg();
                }
                break;
            case 'fuelValidationSuccess': {
                const validationIdx = window.dashboardActiveLegIndex || 0;
                if (window.allRotations && window.allRotations[validationIdx]) {
                    window.allRotations[validationIdx].data.isFuelValidated = true;
                }
                const validationDashBtn = document.getElementById('fuelValidateBtn');
                if (validationDashBtn) {
                    validationDashBtn.onclick = null;
                    validationDashBtn.classList.remove('bg-sky-500/20', 'text-sky-400', 'hover:bg-sky-500', 'hover:text-white', 'border-sky-500/50', 'hover:shadow-[0_0_30px_rgba(14,165,233,0.4)]', 'cursor-pointer', 'group', 'group-hover:scale-105');
                    validationDashBtn.classList.add('bg-emerald-500/20', 'text-emerald-400', 'border-emerald-500/50', 'cursor-not-allowed', 'shadow-[0_0_20px_rgba(16,185,129,0.15)]');
                    const validationDashBtnText = document.getElementById('fuelValidateBtnText');
                    if (validationDashBtnText) validationDashBtnText.innerText = 'FUEL VALIDATED';
                    const validationIcon = validationDashBtn.querySelector('.material-symbols-outlined');
                    if (validationIcon) validationIcon.innerText = 'check_circle';
                }
                const validationMetaText = document.getElementById('dashMetaText');
                if (validationMetaText) {
                    validationMetaText.innerText = "FUEL VALIDATED";
                    validationMetaText.style.color = "#34d399";
                }
                const validationFinalBtn = document.getElementById('btnToggleLoadSheet');
                if (validationFinalBtn) {
                    validationFinalBtn.classList.remove('hidden');
                    validationFinalBtn.classList.add('flex');
                }
                if (window.unlockDashboard) window.unlockDashboard();
                break;
            }
            case 'phaseChanged':
                console.log(`[IPC] Phase changed to ${payload.phase}`);
                if (payload.phase === 'GroundOps') {
                    // ONLY switch to dashboard automatically if dispatch is signed off AND we are NOT booting
                    if (window.isDispatchSignedOff && !window.isAppBooting) {
                        const navDashboard = document.getElementById('navDashboardBtn');
                        if (navDashboard) navDashboard.click(); // Switch to the dashboard
                    }
                    
                    if (window.populateActiveFlightDetails) {
                        window.populateActiveFlightDetails();
                    }

                    // Ensure Ground Ops UI forces a render
                    if (window.groundOpsCache && window.renderGroundOps) {
                        window.renderGroundOps(window.groundOpsCache);
                    }
                }
                break;
            case 'savedUsername':
                document.getElementById('sbUsername').value = payload.username;
                if (payload.username) {
                    localStorage.setItem('sbUsername', payload.username);
                }
                break;
            case 'appVersion':
                const el = document.getElementById('appBuildString');
                if (el) el.innerText = payload.version;
                break;
            case 'simConnectStatus':
                isSimConnected = payload.status.includes('Connected') || payload.status.includes('Linked');
                const smartBtn = document.getElementById('btnSmartConnect');
                const langSim = (localStorage.getItem('selLanguage') || 'EN').toLowerCase();
                const dictSim = window.locales ? window.locales[langSim] : null;

                if (isSimConnected) {
                    if (smartBtn) {
                        smartBtn.innerHTML = '<span class="material-symbols-outlined text-[18px]">wifi</span>';
                        smartBtn.title = payload.status.includes('Linked') ? 'Linked' : 'Connected';
                        smartBtn.className = 'flex items-center justify-center w-10 h-10 rounded-xl bg-emerald-900/20 text-emerald-400 border border-emerald-500/20 shadow-[0_0_15px_rgba(16,185,129,0.1)] hover:bg-emerald-900/40 transition-colors cursor-pointer';
                        smartBtn.style.color = '';
                    }
                } else {
                    if (smartBtn) {
                        smartBtn.innerHTML = '<span class="material-symbols-outlined text-[18px]">wifi_off</span>';
                        smartBtn.title = dictSim ? dictSim.btn_not_connected : 'Not Connected';
                        smartBtn.className = 'flex items-center justify-center w-10 h-10 rounded-xl bg-red-900/20 text-red-500 border border-red-500/20 shadow-[0_0_15px_rgba(239,68,68,0.1)] hover:bg-red-900/40 transition-colors cursor-pointer';
                        smartBtn.style.color = '';
                    }
                }
                break;
            case 'telemetry':
                window.lastTelemetry = payload;
                if (typeof window.checkTimeSkipVisibility === 'function') {
                    window.checkTimeSkipVisibility(payload.phaseEnum);
                }
                if (payload.isDelayed === true) flightHasExperiencedDelay = true;
                if (payload.turbulenceSeverity > 1) flightHasExperiencedTurbulence = true; // Moderate, Severe or Extreme

                // Debug GroundOps changes:
                if (window._prevGsxBoarding !== payload.gsxBoardingState || window._prevMainDoor !== payload.isMainDoorOpen || window._prevBeacon !== payload.isBeaconOn || window._prevEngN1 !== payload.eng1N1) {
                    console.log(`[GroundOps Telemetry] Beacon: ${payload.isBeaconOn} | MainDoor: ${payload.isMainDoorOpen} | Jetway: ${payload.isJetwayConnected} | GSX Bdg: ${payload.gsxBoardingState} | ENG1: ${payload.eng1N1}`);
                    window._prevGsxBoarding = payload.gsxBoardingState;
                    window._prevMainDoor = payload.isMainDoorOpen;
                    window._prevBeacon = payload.isBeaconOn;
                    window._prevEngN1 = payload.eng1N1;
                }

                // Airport Location Gatekeeper (Proactive Warning)
                const locWarning = document.getElementById('locationMismatchWarning');
                const locModal = document.getElementById('locationMismatchModal');
                if (locWarning) {
                    if (payload.isAtWrongAirport) {
                        locWarning.classList.remove('hidden');
                        locWarning.title = `Current position is > 10 NM from planned origin (${payload.plannedOriginIcao}). Distance: ${payload.originDistanceNM} NM`;
                        
                        // Modal is now disabled as per User Request (Story 38).
                        // We keep the modal hidden for now.
                        if (locModal) locModal.classList.add('hidden');
                    } else {
                        locWarning.classList.add('hidden');
                        if (locModal) locModal.classList.add('hidden');
                        window.locationMismatchModalShown = false; // Reset if they get back in range
                    }
                }

                updateIntercomButtons(payload);
                document.getElementById('flightPhase').innerText = `${payload.phase}`;
                
                const topFuelTracker = document.getElementById('topFuelTracker');
                if (topFuelTracker) {
                    if (payload.fob && payload.fob > 0) {
                        topFuelTracker.innerText = Math.round(payload.fob);
                    } else {
                        topFuelTracker.innerText = "---";
                    }
                }
                
                if (typeof window.updateDashboardAnimation === 'function') window.updateDashboardAnimation(payload);

                // Start Ops Button Lifecycle (Point 11)
                const startOpsBtn = document.getElementById('btnStartGroundOps');
                if (startOpsBtn) {
                    if (payload.phaseEnum !== 'AtGate' && payload.phaseEnum !== 'Turnaround') {
                        if (!startOpsBtn.disabled || !startOpsBtn.innerText.includes('FLIGHT')) {
                            startOpsBtn.disabled = true;
                            startOpsBtn.innerHTML = '<span class="material-symbols-outlined text-[18px]">lock</span> FLIGHT IN PROGRESS';
                        }
                        const gPnl = document.getElementById('manualGroundOpsPnl');
                        if (gPnl) gPnl.style.display = 'none';
                    } else if (startOpsBtn.disabled && startOpsBtn.innerText.includes('FLIGHT')) {
                        startOpsBtn.disabled = false;
                        startOpsBtn.innerHTML = '<span class="material-symbols-outlined text-[18px]">flight_takeoff</span> GROUND OPS PNL';
                    }

                    // Show manual ground ops panel
                    if (payload.phaseEnum === 'AtGate' || payload.phaseEnum === 'Turnaround') {
                        const gPnl = document.getElementById('manualGroundOpsPnl');
                        if (gPnl) gPnl.style.display = 'grid';

                        const btnDeboardToggle = document.getElementById('btnDeboardingToggle');
                        if (btnDeboardToggle) {
                            if (payload.isDeboardingAvailable && !payload.isDeboardingCompleted) {
                                btnDeboardToggle.innerText = 'START DEBOARDING';
                                // It should send action 'startDeboarding' to the backend
                                btnDeboardToggle.onclick = () => window.chrome.webview.postMessage({ action: 'startDeboarding' });
                            } else {
                                btnDeboardToggle.innerText = 'START BOARDING';
                                // It should send action 'startService' for Boarding
                                btnDeboardToggle.onclick = () => window.chrome.webview.postMessage({ action: 'startService', service: 'Boarding' });
                            }
                        }
                    }
                }

                // Turbulence Severity Update (Story 25)
                if (payload.turbulenceSeverity !== undefined) {
                    const turbValue = document.getElementById('turbSeverityValue');
                    const turbBar = document.getElementById('turbSeverityBar');
                    const severities = ['NONE', 'LIGHT', 'MODERATE', 'SEVERE', 'EXTREME'];
                    const colors = ['#64748b', '#38bdf8', '#fb923c', '#ef4444', '#a855f7'];
                    const percentages = [0, 25, 50, 75, 100];

                    const index = payload.turbulenceSeverity;
                    if (turbValue) {
                        turbValue.innerText = severities[index] || 'UNKNOWN';
                        turbValue.style.color = colors[index] || '#64748b';
                    }
                    if (turbBar) {
                        turbBar.style.width = percentages[index] + '%';
                        turbBar.style.backgroundColor = colors[index] || '#64748b';
                        turbBar.style.boxShadow = `0 0 8px ${colors[index] || '#64748b'}80`;
                    }
                }

                // Passenger Manifest Refresh (Story 25)
                if (payload.passengers && Array.isArray(payload.passengers)) {
                    let paxArray = window.manifest?.Passengers || window.manifest?.passengers || (Array.isArray(window.manifest) ? window.manifest : null);
                    if (paxArray) {
                        paxArray.forEach(p => {
                            const state = payload.passengers.find(s => s.seat === p.Seat || s.Seat === p.Seat);
                            if (state) {
                                p.IsBoarded = (state.IsBoarded !== undefined) ? state.IsBoarded : state.isBoarded;
                                p.IsSeatbeltFastened = (state.IsSeatbeltFastened !== undefined) ? state.IsSeatbeltFastened : state.isSeatbeltFastened;
                                p.IsInjured = (state.IsInjured !== undefined) ? state.IsInjured : state.isInjured;
                                p.IndividualAnxiety = (state.IndividualAnxiety !== undefined) ? state.IndividualAnxiety : state.individualAnxiety;
                            }
                        });
                    }
                    const cabinTab = document.getElementById('cabin');
                    if (cabinTab && cabinTab.classList.contains('active')) {
                        window.renderManifest(window.manifest);
                    }
                }

                const isAtGate = payload.phaseEnum === 'AtGate' || payload.phaseEnum === 'Turnaround';
                const boardingFinished = payload.passengers && payload.passengers.length > 0 ? payload.passengers.every(p => (p.IsBoarded !== undefined ? p.IsBoarded : p.isBoarded)) : false;
                const hideCabinStats = isAtGate && !boardingFinished;

                if (payload.anxiety !== undefined) {
                    const anxEl = document.getElementById('paxAnxietyValue');
                    const anxBar = document.getElementById('paxAnxietyBar');
                    if (anxEl && anxBar) {
                        if (hideCabinStats) {
                            anxEl.innerHTML = `--<span class="text-sm text-slate-500 font-light ml-1">%</span>`;
                            anxEl.style.color = '#64748b'; // slate-500
                            anxEl.style.textShadow = 'none';
                            anxBar.style.width = '0%';
                        } else {
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
                }
                if (payload.comfort !== undefined) {
                    const comfEl = document.getElementById('paxComfortValue');
                    const comfBar = document.getElementById('paxComfortBar');
                    if (comfEl && comfBar) {
                        if (hideCabinStats) {
                            comfEl.innerHTML = `--<span class="text-sm text-slate-500 font-light ml-1">%</span>`;
                            comfEl.style.color = '#64748b';
                            comfEl.style.textShadow = 'none';
                            comfBar.style.width = '0%';
                        } else {
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
                }

                if (payload.satisfaction !== undefined) {
                    const satEl = document.getElementById('paxSatisfactionValue');
                    const satBar = document.getElementById('paxSatisfactionBar');
                    if (satEl && satBar) {
                        if (hideCabinStats) {
                            satEl.innerHTML = `--<span class="text-sm text-slate-500 font-light ml-1">%</span>`;
                            satEl.style.color = '#64748b';
                            satEl.style.textShadow = 'none';
                            satBar.style.width = '0%';
                        } else {
                            satEl.innerHTML = `${Math.round(payload.satisfaction)}<span class="text-sm text-slate-500 font-light ml-1">%</span>`;
                            satBar.style.width = `${Math.round(payload.satisfaction)}%`;
                            let color = '#34D399'; // Emerald
                            if (payload.satisfaction < 50) color = '#EF4444'; // Red
                            else if (payload.satisfaction < 80) color = '#F59E0B'; // Amber

                            satEl.style.color = color;
                            satEl.style.textShadow = `0 0 20px ${color}4A`;
                            satBar.style.backgroundColor = color;
                            satBar.style.boxShadow = `0 0 8px ${color}80`;
                        }
                    }
                }

                // --- CABIN RESOURCES MULTI-LEG ---
                if (payload.cabinCleanliness !== undefined) {
                    const cleanEl = document.getElementById('cleanlinessVal');
                    if (cleanEl) {
                        cleanEl.innerText = `${Math.round(payload.cabinCleanliness)}%`;
                        cleanEl.style.color = payload.cabinCleanliness < 50 ? '#EF4444' : (payload.cabinCleanliness < 75 ? '#F59E0B' : '#34D399');
                    }
                }
                if (payload.cateringRations !== undefined) {
                    const catEl = document.getElementById('cateringRationsVal');
                    if (catEl) {
                        catEl.innerText = payload.cateringRations;
                        catEl.style.color = payload.cateringRations <= 10 ? '#EF4444' : (payload.cateringRations <= 25 ? '#F59E0B' : '#34D399');
                    }
                }
                if (payload.waterLevel !== undefined) {
                    const waterEl = document.getElementById('waterLevelVal');
                    if (waterEl) {
                        waterEl.innerText = `${Math.round(payload.waterLevel)}%`;
                        waterEl.style.color = payload.waterLevel < 20 ? '#EF4444' : (payload.waterLevel < 50 ? '#F59E0B' : '#60A5FA'); // blue-400
                    }
                }
                if (payload.wasteLevel !== undefined) {
                    const wasteEl = document.getElementById('wasteLevelVal');
                    if (wasteEl) {
                        wasteEl.innerText = `${Math.round(payload.wasteLevel)}%`;
                        wasteEl.style.color = payload.wasteLevel > 90 ? '#EF4444' : (payload.wasteLevel > 70 ? '#F59E0B' : '#60A5FA'); // blue-400
                    }
                }

                if (payload.crewProactivity !== undefined) {
                    const formatColor = (val, el) => {
                        if (el && val !== undefined) {
                            el.innerText = Math.round(val);
                            let color = '#34D399'; // Emerald
                            if (val < 40) color = '#EF4444'; // Red
                            else if (val < 75) color = '#F59E0B'; // Amber
                            el.style.color = color;
                            el.style.textShadow = `0 0 10px ${color}60`;
                        }
                    };

                    formatColor(payload.crewProactivity, document.getElementById('crewProactivityLabel'));
                    formatColor(payload.crewEfficiency, document.getElementById('crewEfficiencyLabel'));
                    formatColor(payload.crewMorale, document.getElementById('crewMoraleLabel'));
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

                            if (payload.isServiceHalted) {
                                cBar.classList.add('bg-red-500', 'animate-pulse');
                                cBar.classList.remove('bg-sky-500');
                                cVal.classList.add('text-red-500');
                            } else {
                                cBar.classList.remove('bg-red-500', 'animate-pulse');
                                cBar.classList.add('bg-sky-500');
                                cVal.classList.remove('text-red-500');
                            }
                        } else {
                            cBox.classList.remove('opacity-100', 'h-10');
                            cBox.classList.add('opacity-0', 'h-0');
                        }
                    }
                }

                // Cabin Temperature
                if (payload.cabinTemp !== undefined) {
                    const tVal = document.getElementById('thermalValue');
                    const tNeedle = document.getElementById('thermalNeedle');

                    if (tVal && tNeedle) {
                        tVal.innerHTML = `${payload.cabinTemp.toFixed(1)}<span class="text-[10px] text-slate-500 font-light ml-1">°C</span>`;

                        // Map 18-30°C to 0-100% position
                        let mappedPercent = ((payload.cabinTemp - 18.0) / 12.0) * 100.0;
                        if (mappedPercent < 0) mappedPercent = 0;
                        if (mappedPercent > 100) mappedPercent = 100;

                        tNeedle.style.left = `${mappedPercent}%`;

                        // Dynamically color the value text based on ranges
                        tVal.classList.remove('text-slate-200', 'text-blue-400', 'text-red-400', 'text-emerald-400');
                        if (payload.cabinTemp < 20.0) tVal.classList.add('text-blue-400');
                        else if (payload.cabinTemp > 25.0) tVal.classList.add('text-red-400');
                        else if (payload.cabinTemp >= 21.0 && payload.cabinTemp <= 24.0) tVal.classList.add('text-emerald-400');
                        else tVal.classList.add('text-slate-200');
                    }
                }

                if (payload.securingProgress !== undefined) {
                    const pBox = document.getElementById('pncProgressBox');
                    const pBar = document.getElementById('pncProgressBar');
                    if (pBox && pBar) {
                        if (payload.securingProgress > 0 && payload.securingProgress < 100) {
                            pBox.classList.remove('opacity-0', 'h-0', 'mb-0');
                            pBox.classList.add('opacity-100', 'h-2', 'mb-4');
                            pBar.style.width = `${payload.securingProgress}%`;

                            if (payload.isSecuringHalted) {
                                pBar.classList.add('bg-red-500', 'animate-pulse');
                                pBar.classList.remove('bg-orange-500');
                            } else {
                                pBar.classList.remove('bg-red-500', 'animate-pulse');
                                pBar.classList.add('bg-orange-500');
                            }
                        } else {
                            pBox.classList.remove('opacity-100', 'h-2', 'mb-4');
                            pBox.classList.add('opacity-0', 'h-0', 'mb-0');
                        }
                    }
                }

                // Update Flight Details Dashboard
                if (payload.sessionFlightsCompleted !== undefined) {
                    let targetIndex = payload.sessionFlightsCompleted;
                    
                    // GEL UI END OF FLIGHT: Maintient l'UI calée sur la Leg qui vient de s'achever pendant l'escale.
                    if (window.currentPhase === 'Turnaround' || window.currentPhase === 'Arrived') {
                        // Si sessionFlightsCompleted est 1, ça veut dire qu'on a fini 1 vol, et on reste calé sur l'index 0.
                        targetIndex = Math.max(0, payload.sessionFlightsCompleted - 1);
                    }

                    if (window.activeLegIndex !== targetIndex) {
                        window.activeLegIndex = targetIndex;
                        // Sync dashboard view with active leg
                        window.dashboardActiveLegIndex = window.activeLegIndex;
                        window.manifest = null; // Clear manifest to force reload for new leg
                        if (window.renderBriefingTabs) window.renderBriefingTabs();
                        if (window.renderBriefingTimeline) window.renderBriefingTimeline();
                        if (window.populateDashboardActiveLeg) window.populateDashboardActiveLeg(window.dashboardActiveLegIndex);
                    }
                }
                const dashDetails = document.getElementById('dashFlightDetails');
                if (dashDetails && window.allRotations && window.allRotations.length > 0) {
                    const currentIdx = Math.min(window.activeLegIndex || 0, window.allRotations.length - 1);
                    const currentFlight = window.allRotations[currentIdx]?.data;

                    if (currentFlight) {
                        dashDetails.style.display = 'flex';

                        document.getElementById('dashDepIcao').innerText = currentFlight.origin?.icao_code || '---';
                        document.getElementById('dashArrIcao').innerText = currentFlight.destination?.icao_code || '---';

                        const GLOBAL_AIRLINES = {
                            'AFR': 'Air France', 'BAW': 'British Airways', 'EZY': 'easyJet', 'RYR': 'Ryanair',
                            'DLH': 'Lufthansa', 'UAE': 'Emirates', 'QTR': 'Qatar Airways', 'DAL': 'Delta',
                            'AAL': 'American Airlines', 'UAL': 'United', 'SWA': 'Southwest'
                        };
                        let aCode = currentFlight.general?.icao_airline || '';
                        let dashFlightCo = document.getElementById('dashFlightCompany');
                        if (dashFlightCo) {
                            dashFlightCo.innerText = GLOBAL_AIRLINES[aCode] || currentFlight.general?.airline_name || aCode || 'AIRLINE';
                            dashFlightCo.onclick = () => { if(window.showAirlineIdentityModal) window.showAirlineIdentityModal(aCode); };
                            dashFlightCo.classList.add('cursor-pointer', 'hover:text-emerald-400', 'transition-colors');
                        }
                        
                        document.getElementById('dashFlightIdent').innerText = `${currentFlight.general?.icao_airline || ''}${currentFlight.general?.flight_number || ''}`;

                        let acType = currentFlight.aircraft?.name || currentFlight.aircraft?.base_type || currentFlight.aircraft?.icaocode || 'Unknown';
                        if (acType.toUpperCase().includes('FENIX') || acType === 'A320') {
                            acType = 'Airbus A320-200';
                        }
                        else if (acType === 'A20N') acType = 'Airbus A320neo';
                        else if (acType === 'B738') acType = 'Boeing 737-800';
                        else if (acType === 'B77W') acType = 'Boeing 777-300ER';

                        document.getElementById('dashAircraftType').innerText = acType;
                        document.getElementById('dashAircraftReg').innerText = currentFlight.aircraft?.reg || 'NO REG';

                        let depCity = currentFlight.origin?.city || '';
                        let depNameStr = currentFlight.origin?.name || '---';
                        let depIcao = currentFlight.origin?.icao_code;

                        let arrCity = currentFlight.destination?.city || '';
                        let arrNameStr = currentFlight.destination?.name || '---';
                        let arrIcao = currentFlight.destination?.icao_code;

                        if (depIcao && window.airportsDb && window.airportsDb[depIcao]) {
                            depCity = window.airportsDb[depIcao].city || depCity;
                            depNameStr = window.airportsDb[depIcao].name || depNameStr;
                        }

                        if (arrIcao && window.airportsDb && window.airportsDb[arrIcao]) {
                            arrCity = window.airportsDb[arrIcao].city || arrCity;
                            arrNameStr = window.airportsDb[arrIcao].name || arrNameStr;
                        }

                        const depFormat = window.formatAirportData(depCity, depNameStr);
                        document.getElementById('dashDepCity').innerText = depFormat.city;
                        document.getElementById('dashDepName').innerText = depFormat.name;

                        const arrFormat = window.formatAirportData(arrCity, arrNameStr);
                        document.getElementById('dashArrCity').innerText = arrFormat.city;
                        document.getElementById('dashArrName').innerText = arrFormat.name;

                        if (currentFlight.times?.sched_out) {
                            let offset = window.lastTelemetry?.globalTimeOffsetSeconds || 0;
                            currentSobtUnix = parseInt(currentFlight.times.sched_out) + offset;
                            const ttSchedDep = document.getElementById('ttSchedDep');
                            if (ttSchedDep) ttSchedDep.innerText = getFormattedTime(currentSobtUnix);
                        }
                        if (currentFlight.times?.sched_in) {
                            let offset = window.lastTelemetry?.globalTimeOffsetSeconds || 0;
                            window.currentSibtUnix = parseInt(currentFlight.times.sched_in) + offset;
                            const ttSchedArr = document.getElementById('ttSchedArr');
                            if (ttSchedArr) ttSchedArr.innerText = getFormattedTime(window.currentSibtUnix);
                        }
                    }
                }
                
                if (typeof window.updateFuelTelemetry === 'function') window.updateFuelTelemetry();
                break;
            case 'pncStatus':
                const pncDot = document.getElementById('pncStatusDot');
                const pncLbl = document.getElementById('pncStatusLabel');
                if (pncDot && pncLbl && payload.status) {
                    pncLbl.innerText = payload.status;
                    if (payload.state === 'SecuringForTakeoff' || payload.state === 'SecuringForLanding') {
                        pncDot.className = "w-2.5 h-2.5 rounded-full bg-orange-500 animate-pulse";
                    } else if (payload.state === 'TakeoffSecured' || payload.state === 'LandingSecured') {
                        pncDot.className = "w-2.5 h-2.5 rounded-full bg-emerald-500 shadow-[0_0_10px_rgba(16,185,129,0.8)]";
                    } else if (payload.state === 'ServingMeals') {
                        pncDot.className = "w-2.5 h-2.5 rounded-full bg-sky-500";
                    } else {
                        pncDot.className = "w-2.5 h-2.5 rounded-full bg-slate-500";
                    }
                }
                break;
            case 'simTime':
                let localSuffix = payload.localTime && payload.localTime !== '--:--' ? ` / ${payload.localTime} LOCAL` : '';
                let utcTime = payload.rawUnix ? getFormattedTime(payload.rawUnix).replace(/z/gi, '') : payload.time.replace(/z/gi, '');
                window.simZuluTime = utcTime;
                document.getElementById('zuluTime').innerText = `${utcTime} UTC${localSuffix}`;

                let localDateSuffix = payload.localDate && payload.localDate !== '--/--/----' ? ` / ${payload.localDate} LOCAL` : '';
                const topDateEl = document.getElementById('topDate');
                if (topDateEl && payload.date) topDateEl.innerText = `${payload.date} UTC${localDateSuffix}`;

                const zuluDateEl = document.getElementById('zuluDate');
                if (zuluDateEl && payload.date) zuluDateEl.innerText = `${payload.date} UTC${localDateSuffix}`;

                const topZuluEl = document.getElementById('topZulu');
                if (topZuluEl) topZuluEl.innerText = `${utcTime} UTC${localSuffix}`;

                const dashDateEl = document.getElementById('dashboardDate');
                if (dashDateEl && payload.date) dashDateEl.innerText = `${payload.date} UTC${localDateSuffix}`;

                const cd = document.getElementById('flightCountdown');

                // --- DYNAMIC TIMETABLE UPDATE ---
                let simFlight = window.currentFlight;
                if (window.allRotations && window.allRotations.length > 0) {
                    const cIdx = Math.min(window.activeLegIndex || 0, window.allRotations.length - 1);
                    if (window.allRotations[cIdx]?.data) simFlight = window.allRotations[cIdx].data;
                }

                if (payload.rawUnix && simFlight && simFlight.times) {
                    let offset = window.lastTelemetry?.globalTimeOffsetSeconds || 0;
                    currentSobtUnix = parseInt(simFlight.times.sched_out) + offset;
                    window.currentSibtUnix = parseInt(simFlight.times.sched_in) + offset;

                    const ttSchedDep = document.getElementById('ttSchedDep');
                    const ttSchedArr = document.getElementById('ttSchedArr');
                    if (ttSchedDep) ttSchedDep.innerText = getFormattedTime(currentSobtUnix);
                    if (ttSchedArr) ttSchedArr.innerText = getFormattedTime(window.currentSibtUnix);

                    const ttActDep = document.getElementById('ttActDep');
                    const ttDepStatus = document.getElementById('ttDepStatus');
                    const ttActArr = document.getElementById('ttActArr');
                    const ttArrStatus = document.getElementById('ttArrStatus');

                    const setBadge = (statusSpan, delaySec) => {
                        if (!statusSpan) return;
                        let m = Math.floor(Math.abs(delaySec) / 60);
                        let h = Math.floor(m / 60);
                        m = m % 60;
                        let timeStr = `${h.toString().padStart(2, '0')}:${m.toString().padStart(2, '0')}`;
                        
                        if (delaySec >= -300 && delaySec <= 300) {
                            statusSpan.innerText = `ON TIME`;
                            statusSpan.className = "px-2 py-0.5 rounded bg-emerald-500/10 text-[10px] text-emerald-400 uppercase font-bold tracking-wider";
                        } else if (delaySec < -300) {
                            statusSpan.innerText = `EARLY : ${timeStr}`;
                            statusSpan.className = "px-2 py-0.5 rounded bg-sky-500/10 text-[10px] text-sky-400 uppercase font-bold tracking-wider";
                        } else if (delaySec <= 900) {
                            statusSpan.innerText = `LATE : ${timeStr}`;
                            statusSpan.className = "px-2 py-0.5 rounded bg-amber-500/10 text-[10px] text-amber-500 uppercase font-bold tracking-wider";
                        } else {
                            statusSpan.innerText = `LATE : ${timeStr}`;
                            statusSpan.className = "px-2 py-0.5 rounded bg-rose-500/10 text-[10px] text-rose-500 uppercase font-bold tracking-wider";
                        }
                    };

                    let currDelay = 0;
                    if (window.finalAobtUnix) {
                        currDelay = window.finalAobtUnix - currentSobtUnix;
                        if (ttActDep) {
                            ttActDep.innerText = getFormattedTime(window.finalAobtUnix);
                            ttActDep.className = "py-4 font-mono text-slate-300 font-bold";
                        }
                        if (ttDepStatus) {
                            ttDepStatus.innerText = "DEPARTED";
                            ttDepStatus.className = "px-2 py-0.5 rounded bg-surface-container-highest text-[10px] text-slate-400 uppercase font-bold tracking-wider";
                        }
                    } else {
                        currDelay = payload.rawUnix > currentSobtUnix ? payload.rawUnix - currentSobtUnix : 0;
                        if (ttActDep) {
                            ttActDep.innerText = getFormattedTime(currentSobtUnix + currDelay);
                            ttActDep.className = currDelay > 180 ? "py-4 font-mono text-rose-400 font-bold animate-pulse" : "py-4 font-mono text-sky-400 font-bold animate-pulse";
                        }
                        setBadge(ttDepStatus, currDelay);
                    }

                    if (window.currentSibtUnix > 0) {
                        if (window.finalAibtUnix) {
                            let arrDelay = window.finalAibtUnix - window.currentSibtUnix;
                            if (ttActArr) {
                                ttActArr.innerText = getFormattedTime(window.finalAibtUnix);
                                ttActArr.className = "py-4 font-mono text-slate-300 font-bold";
                            }
                            if (ttArrStatus) {
                                ttArrStatus.innerText = "ARRIVED";
                                ttArrStatus.className = "px-2 py-0.5 rounded bg-surface-container-highest text-[10px] text-slate-400 uppercase font-bold tracking-wider";
                            }
                        } else {
                            let arrDelay = currDelay;
                            if (window.flightPhase === "Landing" || window.flightPhase === "Taxi In") {
                                arrDelay = payload.rawUnix - window.currentSibtUnix;
                            } else if (payload.rawUnix + currDelay > window.currentSibtUnix) {
                                arrDelay = payload.rawUnix - window.currentSibtUnix;
                            }
                            if (ttActArr) {
                                ttActArr.innerText = getFormattedTime(window.currentSibtUnix + arrDelay);
                                ttActArr.className = arrDelay > 300 ? "py-4 font-mono text-rose-400 font-bold animate-pulse" : "py-4 font-mono text-sky-400 font-bold animate-pulse";
                            }
                            setBadge(ttArrStatus, arrDelay);
                        }
                    }
                }
                 if (cd && payload.rawUnix && currentSobtUnix > 0) {
                    let d = 0; 
                    let phase = payload.phase || window.flightPhase;
                    
                    if (window.finalAibtUnix && window.currentSibtUnix > 0) {
                        d = window.finalAibtUnix - window.currentSibtUnix;
                    } else if ((phase === 'Landing' || phase === 'Taxi In') && window.currentSibtUnix > 0) {
                        d = payload.rawUnix - window.currentSibtUnix;
                    } else if (window.finalAobtUnix && window.currentSibtUnix > 0) {
                        d = window.finalAobtUnix - currentSobtUnix;
                        if (payload.rawUnix > window.currentSibtUnix) {
                            d = payload.rawUnix - window.currentSibtUnix;
                        }
                    } else {
                        d = payload.rawUnix - currentSobtUnix;
                    }

                    let absDiff = Math.floor(Math.abs(d));
                    let mTotal = Math.floor(absDiff / 60);
                    let hStr = Math.floor(mTotal / 60).toString().padStart(2, '0');
                    let mStr = (mTotal % 60).toString().padStart(2, '0');
                    let timeStr = `${hStr}:${mStr}`;

                    let cCol = '#10b981';
                    if (d < -300) {
                        cd.innerText = `EARLY : ${timeStr}`;
                        cCol = '#38bdf8';
                    } else if (d <= 300) {
                        cd.innerText = "ON TIME";
                        cCol = '#10b981';
                    } else if (d <= 900) {
                        cd.innerText = `LATE : ${timeStr}`;
                        cCol = '#f59e0b';
                    } else {
                        cd.innerText = `LATE : ${timeStr}`;
                        cCol = '#ef4444';
                    }
                    cd.style.color = cCol;

                    if (window.finalAibtUnix && window.currentSibtUnix > 0) {
                        let aibtSp = document.getElementById('bdAibt');
                        if (aibtSp) {
                            let diff = window.finalAibtUnix - window.currentSibtUnix;
                            let cCol = '#10b981';
                            if (diff < -300) cCol = '#3b82f6';
                            else if (diff <= 180) cCol = '#10b981';
                            else if (diff <= 420) cCol = '#eab308';
                            else if (diff <= 600) cCol = '#f97316';
                            else cCol = '#ef4444';
                            aibtSp.style.color = cCol;
                        }
                    }
                    if (window.finalAobtUnix) {
                        let aobtSp = document.getElementById('bdAobt');
                        if (aobtSp) {
                            let diff = window.finalAobtUnix - currentSobtUnix;
                            let cCol = '#10b981';
                            if (diff < -300) cCol = '#3b82f6';
                            else if (diff <= 180) cCol = '#10b981';
                            else if (diff <= 420) cCol = '#eab308';
                            else if (diff <= 600) cCol = '#f97316';
                            else cCol = '#ef4444';
                            aobtSp.style.color = cCol;
                        }
                    }
                }

                    // --- Global Rotation Timer Logic ---
                    const globalBanner = document.getElementById('globalRotationBanner');
                    if (globalBanner && window.allRotations && window.allRotations.length > 0 && payload.rawUnix) {
                        globalBanner.classList.remove('hidden');

                        let currentIdx = window.activeLegIndex || 0;
                        document.getElementById('globalRotationStatus').innerText = `Leg ${currentIdx + 1} of ${window.allRotations.length}`;
                        document.getElementById('currentLegStatus').innerText = `Leg ${currentIdx + 1}`;

                        let curLegData = window.allRotations[currentIdx]?.data;
                        let lastLeg = window.allRotations[window.allRotations.length - 1]?.data;
                        let offset = window.lastTelemetry?.globalTimeOffsetSeconds || 0;
                        let curLegFinalUnix = curLegData?.times?.sched_in ? parseInt(curLegData.times.sched_in) + offset : 0;
                        let finalUnix = lastLeg?.times?.sched_in ? parseInt(lastLeg.times.sched_in) + offset : 0;

                        // Calculate current accumulated delay
                        let currentDelay = 0;
                        if (window.finalAibtUnix && window.currentSibtUnix > 0) {
                            currentDelay = window.finalAibtUnix - window.currentSibtUnix;
                        } else if (window.finalAobtUnix && typeof currentSobtUnix !== 'undefined' && currentSobtUnix > 0) {
                            currentDelay = window.finalAobtUnix - currentSobtUnix;
                        } else if (typeof currentSobtUnix !== 'undefined' && currentSobtUnix > 0 && payload.rawUnix > currentSobtUnix && !window.finalAobtUnix) {
                            currentDelay = payload.rawUnix - currentSobtUnix;
                        }

                        // CURRENT LEG TIMER
                        if (curLegFinalUnix > 0) {
                            let estArrival = curLegFinalUnix;
                            if (currentDelay > 0) estArrival += currentDelay;

                            let rem = estArrival - payload.rawUnix;
                            if (rem > 0) {
                                let cM = Math.floor((rem % 3600) / 60);
                                let cS = rem % 60;
                                let cH = Math.floor(rem / 3600);
                                document.getElementById('currentLegTimer').innerText = `${cH.toString().padStart(2, '0')}:${cM.toString().padStart(2, '0')}:${cS.toString().padStart(2, '0')}`;

                                if (cH === 0 && cM < 30) document.getElementById('currentLegTimer').className = "font-mono text-xl md:text-2xl font-black text-rose-500 tracking-wider drop-shadow-[0_0_10px_rgba(244,63,94,0.4)]";
                                else if (cH === 0) document.getElementById('currentLegTimer').className = "font-mono text-xl md:text-2xl font-black text-amber-400 tracking-wider drop-shadow-[0_0_10px_rgba(251,191,36,0.3)]";
                                else document.getElementById('currentLegTimer').className = "font-mono text-xl md:text-2xl font-black text-emerald-400 tracking-wider drop-shadow-[0_0_10px_rgba(52,211,153,0.3)]";
                            } else {
                                document.getElementById('currentLegTimer').innerText = "00:00:00";
                            }
                        }

                        // GLOBAL ROTATION TIMER
                        if (finalUnix > 0) {
                            let estimatedFinalArrival = finalUnix;
                            if (currentDelay > 0) {
                                estimatedFinalArrival += currentDelay;
                            }

                            let remainingSecs = estimatedFinalArrival - payload.rawUnix;
                            if (remainingSecs > 0) {
                                let gH = Math.floor(remainingSecs / 3600);
                                let gM = Math.floor((remainingSecs % 3600) / 60);
                                let gS = remainingSecs % 60;
                                document.getElementById('globalRotationTimer').innerText = `${gH.toString().padStart(2, '0')}:${gM.toString().padStart(2, '0')}:${gS.toString().padStart(2, '0')}`;

                                // Color logic
                                if (gH === 0 && gM < 30) document.getElementById('globalRotationTimer').className = "font-mono text-xl md:text-2xl font-black text-rose-500 tracking-wider drop-shadow-[0_0_10px_rgba(244,63,94,0.4)]";
                                else if (gH === 0) document.getElementById('globalRotationTimer').className = "font-mono text-xl md:text-2xl font-black text-amber-400 tracking-wider drop-shadow-[0_0_10px_rgba(251,191,36,0.3)]";
                                else document.getElementById('globalRotationTimer').className = "font-mono text-xl md:text-2xl font-black text-sky-400 tracking-wider drop-shadow-[0_0_10px_rgba(56,189,248,0.3)]";
                            } else {
                                document.getElementById('globalRotationTimer').innerText = "00:00:00";
                            }
                        }
                    } else if (globalBanner) {
                        globalBanner.classList.add('hidden');
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
                
                if (payload.hasOwnProperty('aobtUnix')) {
                    window.finalAobtUnix = payload.aobtUnix;
                    let el = document.getElementById('bdAobt') || document.getElementById('ttActDep');
                    if (el) {
                        if (payload.aobtUnix) el.innerText = getFormattedTime(payload.aobtUnix);
                        else {
                            el.innerText = '--:--z';
                            const ts = document.getElementById('ttDepStatus');
                            if (ts) { ts.innerText = 'WAITING'; ts.className = 'px-2 py-0.5 rounded bg-surface-container-highest text-[10px] text-slate-500 uppercase font-bold tracking-wider'; }
                        }
                    }
                } else if (payload.aobt) {
                    let el = document.getElementById('bdAobt') || document.getElementById('ttActDep');
                    if (el) el.innerText = payload.aobt;
                }

                if (payload.hasOwnProperty('aibtUnix')) {
                    window.finalAibtUnix = payload.aibtUnix;
                    let el = document.getElementById('bdAibt') || document.getElementById('ttActArr');
                    if (el) {
                        if (payload.aibtUnix) el.innerText = getFormattedTime(payload.aibtUnix);
                        else {
                            el.innerText = '--:--z';
                            const ts = document.getElementById('ttArrStatus');
                            if (ts) { ts.innerText = 'WAITING'; ts.className = 'px-2 py-0.5 rounded bg-surface-container-highest text-[10px] text-slate-500 uppercase font-bold tracking-wider'; }
                        }
                    }
                } else if (payload.aibt) {
                    let el = document.getElementById('bdAibt') || document.getElementById('ttActArr');
                    if (el) el.innerText = payload.aibt;
                }
                break;
            case 'logbookData':
                renderLogbook(payload.history);
                break;
            case 'flightReport':
                {
                    const rep = payload.report;
                    const isFinal = payload.isFinal;
                    const allReps = payload.allReports || [];

                    let isLate = rep.delaySec > 300;
                    let isEarly = rep.rawDelaySec < -300;
                    let puncText = isLate ? `${Math.round(rep.delaySec / 60)}m Late` : (isEarly ? `${Math.abs(Math.round(rep.rawDelaySec / 60))}m Early` : 'On Time');
                    let puncClass = isLate ? 'red' : (isEarly ? 'blue' : 'green');
                    if (rep.delaySec <= 300 && rep.rawDelaySec > 300) puncClass = 'orange'; // Ops Delay Pardon

                    const evtTitle = document.querySelector('[data-i18n="report_title"]');
                    if (evtTitle) {
                        if (isFinal) {
                            evtTitle.innerText = "ROTATION DEBRIEFING";
                        } else {
                            evtTitle.innerText = "POST-FLIGHT DEBRIEF";
                        }
                    }

                    // Super Averages Display
                    const rotSummary = document.getElementById('frRotationSummaryContainer');
                    if (isFinal && allReps && allReps.length > 1 && rotSummary) {
                        rotSummary.style.display = 'block';

                        let totalBlock = 0;
                        let totalDelay = 0;
                        let sumSafety = 0;
                        let sumComfort = 0;
                        let sumSuper = 0;

                        allReps.forEach(r => {
                            totalBlock += parseInt(r.BlockTime) || 0;
                            totalDelay += parseInt(r.DelaySec) || 0;
                            sumSafety += parseInt(r.SafetyPoints) || 0;
                            sumComfort += parseInt(r.ComfortPoints) || 0;
                            sumSuper += parseInt(r.Score) || 0;
                        });

                        let numFlights = allReps.length;

                        document.getElementById('frRotBlockTime').innerText = `${Math.floor(totalBlock / 60)}h ${Math.floor(totalBlock % 60)}m`;
                        let delayMins = Math.round(totalDelay / 60);
                        document.getElementById('frRotDelay').innerText = delayMins > 0 ? `+${delayMins}m` : `${delayMins}m`;
                        if (delayMins > 10) document.getElementById('frRotDelay').className = "text-xl font-mono text-rose-400";
                        else if (delayMins > 0) document.getElementById('frRotDelay').className = "text-xl font-mono text-amber-400";
                        else document.getElementById('frRotDelay').className = "text-xl font-mono text-emerald-400";

                        document.getElementById('frRotSafety').innerText = Math.round(sumSafety);
                        document.getElementById('frRotComfort').innerText = Math.round(sumComfort);
                        document.getElementById('frRotSuper').innerText = Math.round(sumSuper / numFlights);
                    } else if (rotSummary) {
                        rotSummary.style.display = 'none';
                    }

                    document.getElementById('frFlightNo').innerText = `${rep.Airline || rep.airline || ''}${rep.FlightNo || rep.flightNo || ''}`;
                    document.getElementById('frRoute').innerText = `${rep.Dep || rep.dep || 'UNK'} -> ${rep.Arr || rep.arr || 'UNK'}`;

                    const mainScoreEl = document.getElementById('frScore');
                    mainScoreEl.innerText = rep.Score;
                    mainScoreEl.classList.remove('text-emerald-400', 'text-fuchsia-400', 'text-red-400', 'text-amber-400');
                    if (rep.Score >= 1100) mainScoreEl.classList.add('text-fuchsia-400');
                    else if (rep.Score >= 1000) mainScoreEl.classList.add('text-emerald-400');
                    else if (rep.Score >= 800) mainScoreEl.classList.add('text-amber-400');
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

                    setSubScore('frSafetyScore', rep.SafetyPoints ?? rep.safetyPoints ?? 0);
                    setSubScore('frComfortScore', rep.ComfortPoints ?? rep.comfortPoints ?? 0);
                    setSubScore('frMaintScore', rep.MaintenancePoints ?? rep.maintenancePoints ?? 0);
                    setSubScore('frOpsScore', rep.OperationsPoints ?? rep.operationsPoints ?? 0);

                    let btHours = Math.floor(rep.blockTime ? rep.blockTime / 60 : 0);
                    let btMins = (rep.blockTime || 0) % 60;
                    let frBlock = document.getElementById('frBlock');
                    if (frBlock) frBlock.innerText = `${btHours}h ${btMins}m`;

                    const puncBadge = document.getElementById('frPunc');
                    if (puncBadge) {
                        let repDelaySec = rep.DelaySec ?? rep.delaySec ?? 0;
                        let repRawDelaySec = rep.RawDelaySec ?? rep.rawDelaySec ?? 0;
                        puncBadge.innerText = puncText;
                        puncBadge.classList.remove('bg-emerald-500/20', 'text-emerald-400', 'bg-red-500/20', 'text-red-400', 'bg-orange-500/20', 'text-orange-400', 'bg-sky-500/20', 'text-sky-400');
                        if (isLate) puncBadge.classList.add('bg-red-500/20', 'text-red-400');
                        else if (isEarly) puncBadge.classList.add('bg-sky-500/20', 'text-sky-400');
                        else if (repDelaySec <= 300 && repRawDelaySec > 300) puncBadge.classList.add('bg-orange-500/20', 'text-orange-400');
                        else puncBadge.classList.add('bg-emerald-500/20', 'text-emerald-400');
                    }

                    let frFuel = document.getElementById('frFuel');
                    if (frFuel) frFuel.innerText = rep.blockFuel ?? rep.BlockFuel ?? 0;

                    const fpmEl = document.getElementById('frFpm');
                    if (fpmEl) {
                        let tzFpm = rep.TouchdownFpm ?? rep.touchdownFpm ?? 0;
                        fpmEl.innerText = `${tzFpm.toFixed(0)} fpm`;
                        fpmEl.classList.remove('text-emerald-400', 'text-red-500', 'text-slate-200');
                        if (tzFpm < -400) fpmEl.classList.add('text-red-500');
                        else if (tzFpm > -150) fpmEl.classList.add('text-emerald-400');
                        else fpmEl.classList.add('text-slate-200');
                    }

                    const effEl = document.getElementById('frTurnaround');
                    if (effEl) {
                        let effSec = rep.TurnaroundEfficiencySec ?? rep.turnaroundEfficiencySec ?? 0;
                        effEl.classList.remove('bg-emerald-500/20', 'text-emerald-400', 'bg-red-500/20', 'text-red-400', 'bg-slate-500/20', 'text-slate-200');
                        if (effSec > 60) {
                            effEl.innerText = `-${Math.floor(effSec / 60)}m (Early)`;
                            effEl.classList.add('bg-emerald-500/20', 'text-emerald-400', 'px-2', 'py-1', 'rounded', 'uppercase', 'tracking-wider');
                        } else if (effSec < -60) {
                            effEl.innerText = `+${Math.floor(Math.abs(effSec) / 60)}m (Late)`;
                            effEl.classList.add('bg-red-500/20', 'text-red-400', 'px-2', 'py-1', 'rounded', 'uppercase', 'tracking-wider');
                        } else {
                            effEl.innerText = "Target";
                            effEl.classList.add('bg-slate-500/20', 'text-slate-200', 'px-2', 'py-1', 'rounded', 'uppercase', 'tracking-wider');
                        }
                    }

                    const gEl = document.getElementById('frGForce');
                    if (gEl) {
                        let tzG = rep.TouchdownGForce ?? rep.touchdownGForce ?? 1.0;
                        gEl.innerText = `${tzG.toFixed(2)} G`;
                        gEl.classList.remove('text-red-500', 'text-slate-200');
                        if (tzG > 1.4) gEl.classList.add('text-red-500');
                        else gEl.classList.add('text-slate-200');
                    }

                    const ecoContainer = document.getElementById('frEcoContainer');
                    if (ecoContainer) {
                        let expectedFu = rep.ExpectedBlockBurnKg ?? rep.expectedBlockBurnKg ?? 0;
                        let actualFu = rep.ActualBlockBurnKg ?? rep.actualBlockBurnKg ?? 0;
                        
                        // only show if expectations are > 0 (e.g. simbrief was properly loaded)
                        if (expectedFu > 0 && actualFu > 0) {
                            ecoContainer.style.display = 'flex';
                            let deltaParams = actualFu - expectedFu;
                            
                            const ecoBg = document.getElementById('frEcoBg');
                            const ecoIcon = document.getElementById('frEcoIcon');
                            const ecoStatus = document.getElementById('frEcoStatus');
                            const ecoDetails = document.getElementById('frEcoDetails');
                            const ecoDelta = document.getElementById('frEcoDelta');
                            
                            ecoDetails.innerText = `Expected: ${Math.round(expectedFu)} kg | Realized: ${Math.round(actualFu)} kg`;
                            
                            let deltaPrefix = deltaParams > 0 ? "+" : "";
                            ecoDelta.innerText = `${deltaPrefix}${Math.round(deltaParams)} kg`;
                            
                            const resetStyles = () => {
                                ecoBg.className = 'w-16 h-full absolute left-0 top-0 flex items-center justify-center border-r';
                                ecoIcon.className = 'material-symbols-outlined text-3xl';
                                ecoStatus.className = 'font-black font-headline text-2xl uppercase tracking-widest leading-none';
                                ecoDelta.className = 'text-xl font-mono font-bold';
                            };
                            
                            resetStyles();
                            
                            if (deltaParams <= 0) {
                                // Efficient (Saved fuel)
                                ecoBg.classList.add('bg-emerald-500/10', 'border-emerald-500/20');
                                ecoIcon.classList.add('text-emerald-400');
                                ecoIcon.innerText = 'eco';
                                ecoStatus.classList.add('text-emerald-400');
                                ecoStatus.innerText = 'EFFICIENT BURN';
                                ecoDelta.classList.add('text-emerald-400');
                            } else if (deltaParams <= 200) {
                                // Mild overburn
                                ecoBg.classList.add('bg-amber-500/10', 'border-amber-500/20');
                                ecoIcon.classList.add('text-amber-400');
                                ecoIcon.innerText = 'local_gas_station';
                                ecoStatus.classList.add('text-amber-400');
                                ecoStatus.innerText = 'MARGINAL OVERBURN';
                                ecoDelta.classList.add('text-amber-400');
                            } else {
                                // Wasted fuel
                                ecoBg.classList.add('bg-rose-500/10', 'border-rose-500/20');
                                ecoIcon.classList.add('text-rose-500');
                                ecoIcon.innerText = 'warning';
                                ecoStatus.classList.add('text-rose-500');
                                ecoStatus.innerText = 'EXCESSIVE BURN';
                                ecoDelta.classList.add('text-rose-500');
                            }
                        } else {
                            ecoContainer.style.display = 'none';
                        }
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

                    // Instead of showing the modal automatically, output a system log message
                    const viewLabel = (localStorage.getItem('selLanguage') || 'en') === 'fr' ? 'Consulter le rapport' : 'View Report';
                    const msgContent = `[SYSTEM] Flight Report Available. <a href="#" onclick="document.getElementById('flightReportModal').style.display = 'flex'; return false;" class="text-blue-400 hover:text-blue-300 underline">${viewLabel}</a>`;
                    
                    const timeString = new Date().toLocaleTimeString('en-GB', { hour: '2-digit', minute: '2-digit' });
                    const newLog = document.createElement("div");
                    newLog.className = "mb-1";
                    newLog.innerHTML = `<span class="text-gray-400 font-mono text-xs">[${timeString}]</span> <span class="text-blue-300">${msgContent}</span>`;
                    
                    const logContainer = document.getElementById("pncChat");
                    if (logContainer) {
                        logContainer.appendChild(newLog);
                        logContainer.scrollTop = logContainer.scrollHeight;
                    }
                }
                break;
            case 'gatekeeperFailed':
                alert(payload.reason || "Cannot start ground ops! Ensure MSFS is connected, engines are off, parking brake is set, and aircraft is on the ground.");
                break;
            case 'switchTab':
                const targetTab = payload.target;
                const menuItems = document.querySelectorAll('.menu li, li[data-target="profile"]');
                const sections = document.querySelectorAll('section');

                menuItems.forEach(m => {
                    if (m.getAttribute('data-target') === targetTab) {
                        m.classList.add('active');
                    } else {
                        m.classList.remove('active');
                    }
                });
                sections.forEach(sec => {
                    if (sec.id === targetTab) {
                        sec.classList.add('active');
                    } else {
                        sec.classList.remove('active');
                    }
                });
                break;
            case 'showGroundEvent':
                if (payload.eventData) {
                    const evt = payload.eventData;

                    // Match service logic based on ServiceName or "Generic" container
                    let containerId = undefined;

                    if (evt.serviceName) {
                        // Find the safeName matching the service (e.g. "CargoLuggage" from "Cargo/Luggage")
                        const safeName = evt.serviceName.replace(/\s|[^\w]/g, '');
                        containerId = `ge-container-${safeName}`;
                    }

                    let geCardContainer = containerId ? document.getElementById(containerId) : null;

                    // Fallback to the first available if not found
                    if (!geCardContainer) {
                        console.warn(`[RAMP] Service container not found for ${evt.serviceName}, falling back to top level.`);
                        // Here we could have a generic global one, but for now just grab the first card container or skip.
                        const anyContainer = document.querySelector('[id^="ge-container-"]');
                        if (anyContainer) geCardContainer = anyContainer;
                        else return;
                    }

                    geCardContainer.innerHTML = `
                        <div class="flex items-start gap-4 p-5 bg-orange-900/20 border-t border-orange-500/30 w-full rounded-b-xl shadow-inner relative overflow-hidden">
                            <div class="absolute top-0 right-0 w-24 h-24 bg-orange-500/10 rounded-bl-[100px] pointer-events-none"></div>
                            
                            <span class="material-symbols-outlined text-orange-400 text-3xl mt-1 w-12 h-12 flex shrink-0 items-center justify-center bg-orange-500/20 rounded-full border border-orange-500/30 relative z-10">report_problem</span>
                            
                            <div class="flex flex-col flex-grow relative z-10">
                                <h3 class="text-white font-black font-headline text-lg uppercase tracking-widest leading-none mb-1">${evt.title}</h3>
                                <p class="text-slate-400 text-xs mb-4 leading-relaxed font-body">${evt.description}</p>
                                <div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-3 w-full font-manrope choices-container">
                                </div>
                            </div>
                        </div>
                    `;

                    const choicesContainer = geCardContainer.querySelector('.choices-container');

                    if (evt.choices) {
                        evt.choices.forEach(c => {
                            const btn = document.createElement('button');
                            btn.className = 'w-full py-3 px-4 rounded-xl font-bold uppercase tracking-widest text-[10px] transition-all flex items-center justify-left gap-2 text-left ';

                            if (c.colorClass === 'success') {
                                btn.className += 'bg-emerald-500/10 text-emerald-400 border border-emerald-500/30 hover:bg-emerald-500 hover:text-slate-900 shadow-[0_0_15px_rgba(16,185,129,0.1)]';
                                btn.innerHTML = `<span class="material-symbols-outlined text-[16px]">check_circle</span> <span>${c.text}</span>`;
                            } else if (c.colorClass === 'error') {
                                btn.className += 'bg-red-500/10 text-red-400 border border-red-500/30 hover:bg-red-500 hover:text-white shadow-[0_0_15px_rgba(239,68,68,0.1)]';
                                btn.innerHTML = `<span class="material-symbols-outlined text-[16px]">warning</span> <span>${c.text}</span>`;
                            } else {
                                btn.className += 'bg-black/40 text-slate-300 border border-white/10 hover:bg-white/10 hover:text-white';
                                btn.innerText = c.text;
                            }

                            btn.addEventListener('click', () => {
                                window.chrome.webview.postMessage({ action: 'resolveGroundEvent', eventId: evt.id, choiceId: c.id });
                                geCardContainer.classList.add('hidden');
                                geCardContainer.innerHTML = '';
                            });

                            choicesContainer.appendChild(btn);
                        });
                    }
                    geCardContainer.classList.remove('hidden');
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
                    let msg = payload.message || '';
                    let prefixMatch = msg.match(/^\[(.*?)\]/);
                    let prefixHtml = "";
                    let colorHash = payload.level === 'red' ? '#EF4444' : (payload.level === 'orange' ? '#F59E0B' : '#38BDF8');

                    if (prefixMatch) {
                        const tag = prefixMatch[1];
                        msg = msg.substring(prefixMatch[0].length).trim();
                        let tagColor = '#e2e8f0'; // default
                        if (tag === 'CPT PA') tagColor = '#10b981'; // emerald-500
                        else if (tag === 'PNC PA') tagColor = '#38bdf8'; // sky-400
                        else if (tag === 'CPT INT') tagColor = '#f59e0b'; // amber-500
                        else if (tag === 'PNC INT') tagColor = '#22d3ee'; // cyan-400

                        prefixHtml = `<span style="color:${tagColor}; font-weight:bold; font-size:10px; margin-right:4px;">[${tag}]</span>`;
                        if (payload.level !== 'red' && payload.level !== 'orange') colorHash = '#e2e8f0';
                    } else if (msg.startsWith("PA:")) {
                        msg = msg.substring(3).trim();
                        prefixHtml = `<span style="color:#10b981; font-weight:bold; font-size:10px; margin-right:4px;">[CPT PA]</span>`;
                        if (payload.level !== 'red' && payload.level !== 'orange') colorHash = '#e2e8f0';
                    } else if (msg.startsWith("Captain,") || msg.startsWith("Commandant,") || payload.level === 'info') {
                        prefixHtml = `<span style="color:#22d3ee; font-weight:bold; font-size:10px; margin-right:4px;">[PNC INT]</span>`;
                        if (payload.level !== 'red' && payload.level !== 'orange') colorHash = '#e2e8f0';
                    } else {
                        // Uncategorized
                        prefixHtml = `<span style="color:#slate-500; font-weight:bold; font-size:10px; margin-right:4px;">[SYS]</span>`;
                    }

                    cli.innerHTML = `<span style="color:#64748b; margin-right:4px; font-size: 9px;">${window.getLocalFormattedTime()}</span>${prefixHtml}<span style="color:${colorHash}">${msg}</span>`;
                    cli.style.marginBottom = '5px';
                    cli.style.borderBottom = '1px solid rgba(255,255,255,0.05)';
                    cli.style.paddingBottom = '3px';
                    clog.prepend(cli);
                    if (clog.children.length > 5) clog.removeChild(clog.lastChild);
                }
                if (payload.audioSequence && payload.audioSequence.length > 0) {
                    if (window.audioEngine) window.audioEngine.playSequence(payload.audioSequence);
                }
                break;
            case 'InitProfile':
                const profile = payload.payload || payload;
                if (profile) {
                    // DEBUG: Log the profile object to the terminal/console to see what it contains
                    console.log("[DEBUG] InitProfile received:", JSON.stringify(profile));

                    // Update Sidebar
                    const sbCallsign = document.getElementById('sbProfileCallsign');
                    if (sbCallsign) sbCallsign.innerText = profile.callSign || profile.CallSign || 'MAVERICK';
                    const sbRank = document.getElementById('sbProfileRank');
                    if (sbRank) sbRank.innerText = profile.calculatedRank || profile.CalculatedRank || 'Trainee';

                    // Fetch Avatar from local virtual host to bypass IPC limits
                    fetch('https://fsv.local/ProfileAvatar.b64', { cache: 'no-store' })
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

                    const prfSuperScore = document.getElementById('prfAvgScore');
                    if (prfSuperScore) prfSuperScore.innerText = Math.round(profile.averageSuperScore ?? profile.AverageSuperScore ?? 0);

                    const prfHighestScore = document.getElementById('prfHighestScore');
                    if (prfHighestScore) prfHighestScore.innerText = Math.round(profile.highestSuperScore ?? profile.HighestSuperScore ?? 0);

                    const prfPunctuality = document.getElementById('prfPunctuality');
                    if (prfPunctuality) prfPunctuality.innerText = `${Math.round(profile.punctualityRatingPercentage ?? profile.PunctualityRatingPercentage ?? 0)}%`;

                    const prfTouchdown = document.getElementById('prfBestFpm');
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

                // Update Briefing Live Score Display
                const bScore = document.getElementById('briefingScoreValue');
                if (bScore) {
                    bScore.innerText = payload.score;
                    bScore.classList.remove('text-emerald-400', 'text-fuchsia-400', 'text-amber-400', 'text-red-400');
                    if (payload.score >= 1100) bScore.classList.add('text-fuchsia-400');
                    else if (payload.score >= 1000) bScore.classList.add('text-emerald-400');
                    else if (payload.score >= 800) bScore.classList.add('text-amber-400');
                    else bScore.classList.add('text-red-400');
                }

                const updateSubBar = (idPts, idBar, pts) => {
                    const elPts = document.getElementById(idPts);
                    const elBar = document.getElementById(idBar);
                    if (!elPts || !elBar) return;

                    elPts.innerText = (pts > 0 ? '+' : '') + pts;
                    elPts.classList.remove('text-sky-400', 'text-emerald-400', 'text-amber-400', 'text-purple-400', 'text-red-400', 'text-white');
                    if (pts > 0) elPts.classList.add('text-emerald-400');
                    else if (pts < 0) elPts.classList.add('text-red-400');
                    else elPts.classList.add('text-white');

                    let pct = 50 + ((pts / 1000) * 50);
                    if (pct < 5) pct = 5;
                    if (pct > 100) pct = 100;
                    elBar.style.width = pct + '%';
                };

                const ptsSafety = payload.safety !== undefined ? payload.safety : (payload.Safety !== undefined ? payload.Safety : 0);
                const ptsComfort = payload.comfort !== undefined ? payload.comfort : (payload.Comfort !== undefined ? payload.Comfort : 0);
                const ptsMaint = payload.maint !== undefined ? payload.maint : (payload.Maint !== undefined ? payload.Maint : 0);
                const ptsOps = payload.ops !== undefined ? payload.ops : (payload.Ops !== undefined ? payload.Ops : 0);

                updateSubBar('b_safetyPts', 'b_safetyBar', ptsSafety);
                updateSubBar('b_comfortPts', 'b_comfortBar', ptsComfort);
                updateSubBar('b_maintPts', 'b_maintBar', ptsMaint);
                updateSubBar('b_opsPts', 'b_opsBar', ptsOps);

                const finalScore = payload.score !== undefined ? payload.score : (payload.Score !== undefined ? payload.Score : 1000);
                const finalDelta = payload.delta !== undefined ? payload.delta : (payload.Delta !== undefined ? payload.Delta : 0);
                const finalMsg = payload.msg || payload.message || payload.Msg || '';

                const mainScore = document.getElementById('mainScoreValue');
                if (mainScore) mainScore.innerText = finalScore;

                if (finalDelta !== 0) {
                    mainScore.classList.remove('text-emerald-400', 'text-red-400');
                    mainScore.classList.add(finalDelta > 0 ? 'text-emerald-400' : 'text-red-400');
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
                        let deltaStr = finalDelta > 0 ? `+${finalDelta}` : `${finalDelta}`;
                        let color = finalDelta > 0 ? '#34D399' : '#F87171';
                        fli.innerHTML = `<span style="color:${color}; font-weight:bold; width: 45px; display:inline-block;">${deltaStr}</span> <span style="color:#cbd5e1;">${finalMsg}</span>`;
                        fli.style.marginBottom = '5px';
                        fli.style.borderBottom = '1px solid rgba(255,255,255,0.05)';
                        fli.style.paddingBottom = '3px';
                        feed.prepend(fli);
                        if (feed.children.length > 6) feed.removeChild(feed.lastChild);
                    }

                    const plog = document.getElementById('penaltyLogs');
                    if (plog) {
                        const logLi = document.createElement('li');
                        logLi.innerText = `[${window.getLocalFormattedTime()}] ${finalMsg} (Total: ${finalScore})`;
                        if (finalDelta === 0) {
                            logLi.style.color = '#cbd5e1';
                        } else {
                            logLi.style.color = finalDelta > 0 ? '#A7F3D0' : '#FCA5A5';
                        }
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

                    const dso = document.getElementById('dashStartOverlay');
                    if (dso) dso.classList.add('opacity-0', 'pointer-events-none');

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
                    // Auto switch to GroundOps instead of Briefing
                    setTimeout(() => {
                        let sbPayloadStr = "[]";
                        try {
                            let sbPayload = [];
                            if (window.allRotations && window.allRotations.length > 0) {
                                sbPayload = window.allRotations.map(r => r.data);
                                if (window.activeLegIndex) {
                                    sbPayload = sbPayload.slice(window.activeLegIndex);
                                }
                            }
                            sbPayloadStr = JSON.stringify(sbPayload);
                            window.chrome.webview.postMessage({ action: 'syncRotationsAndStart', payloadStr: sbPayloadStr });

                            setTimeout(() => {
                                window.chrome.webview.postMessage({ action: 'finishDispatch' });

                                // Clean up UI state
                                const dispatchModal = document.getElementById('simbriefDispatchModal');
                                if (dispatchModal) dispatchModal.style.display = 'none';

                                const btnFinishDispatch = document.getElementById('btnFinishDispatch');
                                if (btnFinishDispatch) btnFinishDispatch.classList.add('opacity-50', 'cursor-not-allowed', 'pointer-events-none');
                                
                                // BUG FIX: Automatically view the first leg in Briefing when closing dispatch
                                window.dashboardActiveLegIndex = 0;
                                if (window.populateDashboardActiveLeg) window.populateDashboardActiveLeg(0);
                                if (window.populateBriefingView) window.populateBriefingView(0);
                                if (window.renderBriefingTimeline) window.renderBriefingTimeline();

                            }, 800); // Small 800ms delay to let the dashboard prepare visually
                        } catch (err) {
                            console.error(err);
                        }
                    }, 500);
                }
                break;
            case 'groundOpsReady':
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
            case 'fuelValidationRejected':
                alert(payload.message);
                break;
            case 'flightCancelled':
                isFlightCancelled = true;
                window.isFlightActive = false;

                const dso2 = document.getElementById('dashStartOverlay');
                if (dso2) dso2.classList.remove('opacity-0', 'pointer-events-none');

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

                break;
            case 'flightData':
                try {
                const d = payload.data;
                const manifest = payload.manifest;

                const loader = document.getElementById('simbriefLoadingState');
                if (loader) loader.style.display = 'none';

                const dispatchModal = document.getElementById('simbriefDispatchModal');
                if (dispatchModal && dispatchModal.style.display !== 'none') {
                    // Do not close the modal automatically anymore, display the "Next Leg" prompt

                    const dispatchContainer = document.getElementById('dispatchLegsContainer');
                    if (dispatchContainer) {
                        const origin = d.origin ? d.origin.icao_code || 'ORIG' : 'ORIG';
                        const dest = d.destination ? d.destination.icao_code || 'DEST' : 'DEST';

                        const airline = d.general ? d.general.icao_airline || '' : '';
                        const aircraft = d.aircraft ? d.aircraft.icaocode || '' : '';

                        if (window.lastFlightPlanData && (window.currentLegCounter || 1) > 1) {
                            const pOrigin = window.lastFlightPlanData.origin;
                            const pDest = window.lastFlightPlanData.dest;
                            const pAirline = window.lastFlightPlanData.airline;
                            const pAircraft = window.lastFlightPlanData.aircraft;

                            let errorMsg = null;
                            if (origin === pOrigin && dest === pDest) {
                                errorMsg = `Leg ${window.currentLegCounter} cannot be identical to the previous flight (${origin} âž” ${dest}). Please configure a new route in SimBrief.`;
                            } else if (origin !== pDest) {
                                errorMsg = `Geographic Continuity Error: Leg ${window.currentLegCounter} must depart from ${pDest} (the arrival of your previous leg), but you planned a departure from ${origin}.`;
                            } else if (airline !== pAirline || aircraft !== pAircraft) {
                                errorMsg = `Aircraft/Airline Mismatch: You cannot change aircraft type or airline during an active rotation shift.`;
                            }

                            if (errorMsg) {
                                window.chrome.webview.postMessage({ action: 'cancelLastLeg' });

                                const prevLegHTML = `
                                    <button onclick="window.chrome.webview.postMessage({ action: 'openSimbriefWindow' })" class="bg-[#1C1F26] border border-red-500/30 text-white px-8 py-6 rounded-xl hover:bg-red-500/10 hover:border-red-500 shadow-xl transition-all font-bold tracking-widest flex items-center justify-between group w-full mt-2">
                                        <div class="flex items-center gap-4">
                                            <span class="material-symbols-outlined text-3xl group-hover:scale-110 transition-transform text-red-400">warning</span>
                                            <div class="text-left">
                                                <div class="text-lg">RE-GENERATE LEG ${window.currentLegCounter}</div>
                                                <div class="text-slate-500 text-[10px] uppercase mt-1 font-manrope font-normal text-red-400/80">Previous plan rejected. Open SimBrief to fix.</div>
                                            </div>
                                        </div>
                                        <span class="material-symbols-outlined text-slate-600">chevron_right</span>
                                    </button>
                                `;

                                dispatchContainer.innerHTML = (window.simbriefSavedLegsNodes ? window.simbriefSavedLegsNodes.join('') : '') + prevLegHTML;
                                alert(errorMsg);
                                return;
                            }
                        }

                        // Store for next validation
                        window.lastFlightPlanData = {
                            origin: origin,
                            dest: dest,
                            airline: airline,
                            aircraft: aircraft
                        };

                        if (!window.simbriefSavedLegsNodes) window.simbriefSavedLegsNodes = [];

                        window.simbriefSavedLegsNodes.push(`
                            <div class="bg-emerald-500/10 border border-emerald-500/30 text-emerald-400 px-8 py-4 rounded-xl shadow-[0_0_15px_rgba(16,185,129,0.15)] flex items-center justify-between w-full mt-2">
                                <div class="flex items-center gap-4">
                                    <span class="material-symbols-outlined text-3xl">check_circle</span>
                                    <div class="text-left">
                                        <div class="text-sm font-bold tracking-widest uppercase">LEG ${window.currentLegCounter || 1} SAVED</div>
                                        <div class="text-emerald-500/70 text-[10px] uppercase mt-1 font-manrope font-bold">${origin} âž” ${dest}</div>
                                    </div>
                                </div>
                            </div>
                        `);

                        const nextLeg = (window.currentLegCounter || 1) + 1;
                        let nextButtonHtml = '';
                        if (nextLeg <= 4) {
                            nextButtonHtml = `
                                <button onclick="window.currentLegCounter = ${nextLeg}; window.chrome.webview.postMessage({ action: 'openSimbriefWindow' })" class="bg-[#1C1F26] border border-sky-500/30 text-white px-8 py-6 rounded-xl hover:bg-sky-500/10 hover:border-sky-500 shadow-xl transition-all font-bold tracking-widest flex items-center justify-between group w-full mt-2">
                                    <div class="flex items-center gap-4">
                                        <span class="material-symbols-outlined text-3xl group-hover:scale-110 transition-transform text-sky-400">open_in_new</span>
                                        <div class="text-left">
                                            <div class="text-lg">ADD LEG ${nextLeg}</div>
                                            <div class="text-slate-500 text-[10px] uppercase mt-1 font-manrope font-normal">Open SimBrief to configure your next flight</div>
                                        </div>
                                    </div>
                                    <span class="material-symbols-outlined text-slate-600">chevron_right</span>
                                </button>
                            `;
                        }

                        dispatchContainer.innerHTML = window.simbriefSavedLegsNodes.join('') + nextButtonHtml;

                        // Enforce FINISH BRIEFING
                        const btnFinishDispatch = document.getElementById('btnFinishDispatch');
                        if (btnFinishDispatch) {
                            btnFinishDispatch.classList.remove('opacity-50', 'cursor-not-allowed', 'pointer-events-none');
                        }
                    }
                }

                if (document.getElementById('dashMetaBar')) document.getElementById('dashMetaBar').style.display = 'block';
                if (document.getElementById('flightBreakdown')) document.getElementById('flightBreakdown').style.display = 'grid';

                const timeStr = window.getFormattedTime;

                if (!window.allRotations) window.allRotations = [];

                // Replace matching dummy leg with the real fetched SimBrief OFP data
                let replacedDummy = false;
                for (let i = 0; i < window.allRotations.length; i++) {
                    const rotData = window.allRotations[i].data;
                    if (rotData?.isDummy) {
                        const rotOrig = (rotData?.origin?.icao_code || '').toUpperCase();
                        const rotDest = (rotData?.destination?.icao_code || '').toUpperCase();
                        const dOrig = (d.origin?.icao_code || '').toUpperCase();
                        const dDest = (d.destination?.icao_code || '').toUpperCase();

                        // We also allow replacement if we are replacing the very first dummy leg, regardless of origin mismatch, if it's the first leg
                        if ((rotOrig === dOrig && rotDest === dDest) || (i === 0 && window.allRotations.length > 0)) {
                            window.allRotations[i] = { data: d, briefing: payload.briefing, manifest: payload.manifest, airlineProfile: payload.airlineProfile };
                            replacedDummy = true;
                            break;
                        }
                    }
                }

                // If not replaced, push it normally (unless it's a perfect duplicate of a real leg)
                if (!replacedDummy) {
                    const isDupe = window.allRotations.some(r => {
                        if (r.data?.isDummy) return false;

                        const rFlightNo = (r.data?.general?.flight_number || '').toUpperCase();
                        const dFlightNo = (d.general?.flight_number || '').toUpperCase();
                        const rOrig = (r.data?.origin?.icao_code || '').toUpperCase();
                        const rDest = (r.data?.destination?.icao_code || '').toUpperCase();
                        const dOrig = (d.origin?.icao_code || '').toUpperCase();
                        const dDest = (d.destination?.icao_code || '').toUpperCase();

                        return rFlightNo === dFlightNo && rOrig === dOrig && rDest === dDest;
                    });

                    if (!isDupe) {
                        window.allRotations.push({ data: d, briefing: payload.briefing, manifest: payload.manifest, airlineProfile: payload.airlineProfile });
                    }
                }

                // Inject remaining planned dummy legs into allRotations if they aren't there yet
                if (window.plannedDummyLegs && window.plannedDummyLegs.length > 0) {
                    const dOrig = (d.origin?.icao_code || '').toUpperCase();
                    const dDest = (d.destination?.icao_code || '').toUpperCase();

                    window.plannedDummyLegs = window.plannedDummyLegs.filter(dummy => {
                        const rotOrig = (dummy.origin?.icao_code || '').toUpperCase();
                        const rotDest = (dummy.destination?.icao_code || '').toUpperCase();
                        return !(rotOrig === dOrig && rotDest === dDest);
                    });

                    window.plannedDummyLegs.forEach(dummy => {
                        const dummyOrig = (dummy.origin?.icao_code || '').toUpperCase();
                        const dummyDest = (dummy.destination?.icao_code || '').toUpperCase();

                        const exists = window.allRotations.some(r => {
                            if (!r.data?.isDummy) return false;
                            const rOrig = (r.data?.origin?.icao_code || '').toUpperCase();
                            const rDest = (r.data?.destination?.icao_code || '').toUpperCase();
                            return rOrig === dummyOrig && rDest === dummyDest;
                        });
                        if (!exists) {
                            window.allRotations.push({ data: dummy, briefing: null, manifest: null });
                        }
                    });
                }

                // Calculate estimated times for appended legs based on preceding legs
                for (let i = 1; i < window.allRotations.length; i++) {
                    const prevLeg = window.allRotations[i - 1].data;
                    const currLeg = window.allRotations[i].data;

                    if (prevLeg.times?.sched_in) {
                        const tatSeconds = 35 * 60; // 35 min average turnaround
                        const prevSibt = parseInt(prevLeg.times.sched_in);
                        let currSobt = parseInt(currLeg.times?.sched_out || '0');
                        if (!currLeg.times) currLeg.times = {};

                        // If it's a dummy leg OR if the fetched SimBrief leg overlaps or is too tight (< 35 min turnaround)
                        if (currLeg.isDummy || isNaN(currSobt) || currSobt < prevSibt + tatSeconds) {
                            // Automatically cascade the sequence
                            currLeg.times.sched_out = (prevSibt + tatSeconds).toString();

                            const eteSeconds = parseInt(currLeg.times.est_time_enroute || '3600');
                            currLeg.times.sched_in = (parseInt(currLeg.times.sched_out) + eteSeconds).toString();
                        }
                    }
                }


                // Briefing Tab Rendering Engine (Navigation Shift)

                // Trigger render
                if (window.renderBriefingTimeline) window.renderBriefingTimeline();
                if (window.renderBriefingTabs) window.renderBriefingTabs();
                if (window.populateDashboardActiveLeg) window.populateDashboardActiveLeg(window.dashboardActiveLegIndex || 0);

                // Dashboard Header Logic (Metadata) - Only update to the newest leg payload
                const dashFlightHeader = document.getElementById('dashFlightHeader');
                if (dashFlightHeader) dashFlightHeader.style.display = 'block';
                if (document.getElementById('dashMetaBar')) document.getElementById('dashMetaBar').style.display = 'block';

                const dhOrigin = document.getElementById('dhOrigin');
                const dhDest = document.getElementById('dhDest');
                const dhFlight = document.getElementById('dhFlight');
                const dhAirline = document.getElementById('dhAirline');

                if (dhOrigin) dhOrigin.innerHTML = `${d.origin?.icao_code || '---'} <span style="font-size: 15px; color: #94A3B8; font-weight: 600;">(${d.origin?.name || d.origin?.city || ''})</span>`;
                if (dhDest) dhDest.innerHTML = `${d.destination?.icao_code || '---'} <span style="font-size: 15px; color: #94A3B8; font-weight: 600;">(${d.destination?.name || d.destination?.city || ''})</span>`;
                if (dhFlight) dhFlight.innerText = `Flight ${d.general?.icao_airline || ''}${d.general?.flight_number || ''}`;
                if (dhAirline) {
                    dhAirline.innerText = d.general?.airline_name || d.general?.icao_airline || 'Unknown';
                    const aCode = d.general?.icao_airline || '';
                    dhAirline.onclick = () => { if(window.showAirlineIdentityModal) window.showAirlineIdentityModal(aCode); };
                    dhAirline.classList.add('cursor-pointer', 'hover:text-emerald-400', 'transition-colors');
                }

                if (payload.manifest && !window.manifest) {
                    window.manifest = payload.manifest;
                    if (window.renderManifest) window.renderManifest(payload.manifest);
                }

                if (d.weather) {
                    let wTxt = `Origin METAR: ${d.weather.orig_metar || ''}\nOrigin TAF: ${d.weather.orig_taf || ''}\n\n`;
                    wTxt += `Dest METAR: ${d.weather.dest_metar || ''}\nDest TAF: ${d.weather.dest_taf || ''}\n\n`;

                    const formatArr = (arr) => Array.isArray(arr) ? arr.join('\n') : (arr || '');
                    if (d.weather.altn_metar) wTxt += `Altn METAR(s):\n${formatArr(d.weather.altn_metar)}\n\n`;
                    if (d.weather.enrt_metar) wTxt += `Enroute METAR(s):\n${formatArr(d.weather.enrt_metar)}\n\n`;

                    const weatherElem = document.getElementById('weatherContent');
                    if (weatherElem) weatherElem.innerText = wTxt.trim();
                }

                } catch (flightDataError) {
                    console.error("Crash processing flightData:", flightDataError);
                    alert("UI parsing error: " + flightDataError.message);
                }
                break;
            case 'briefingUpdate':
                if (window.allRotations && window.allRotations.length > 0) {
                    // Assume the live ACARS update targets the active flight in the rotation (index 0)
                    window.allRotations[0].briefing = payload.briefing;
                    if (window.renderBriefingTabs) {
                        // Store the current tab index so we don't jump back to the last tab when re-rendering
                        const activeViewIndex = Array.from(document.querySelectorAll('.briefing-view')).findIndex(v => v.style.display !== 'none');
                        window.renderBriefingTabs();
                        if (window.populateDashboardActiveLeg) window.populateDashboardActiveLeg(window.dashboardActiveLegIndex || 0);
                        if (activeViewIndex >= 0 && window.setBriefingTab) {
                            window.setBriefingTab(activeViewIndex);
                        }
                    }
                }
                break;
            case 'manifestUpdate':
                if (payload.manifest) {
                    window.manifest = payload.manifest;
                    if (window.renderManifest) window.renderManifest(payload.manifest);
                }
                break;
            case 'groundOps':
                window.groundOpsCache = payload.services;
                if (payload.isDispatchSignedOff !== undefined) {
                    window.isDispatchSignedOff = payload.isDispatchSignedOff;
                }
                renderGroundOps(payload.services);
                updateMetaBar(payload.services);
                if (payload.airportTier) {
                    const topTier = document.getElementById('topAirportTier');
                    const letterBox = document.getElementById('aptTierLetterBox');
                    const descBox = document.getElementById('aptTierDesc');
                    const tierPanel = document.getElementById('airportTierPanel');

                    if (topTier) topTier.innerText = payload.airportTier;
                    if (letterBox && descBox && tierPanel) {
                        tierPanel.classList.remove('hidden');
                        let letter = payload.airportTier.replace('Tier ', '');
                        letterBox.innerText = letter;
                        descBox.innerText = payload.airportTierDesc || '';

                        if (letter === 'S') letterBox.className = "w-14 h-14 rounded-lg flex items-center justify-center font-black text-2xl border drop-shadow-[0_0_15px_rgba(250,204,21,0.5)] bg-yellow-500/10 text-yellow-400 border-yellow-500/30";
                        else if (letter === 'A') letterBox.className = "w-14 h-14 rounded-lg flex items-center justify-center font-black text-2xl border drop-shadow-[0_0_15px_rgba(52,211,153,0.5)] bg-emerald-500/10 text-emerald-400 border-emerald-500/30";
                        else if (letter === 'B') letterBox.className = "w-14 h-14 rounded-lg flex items-center justify-center font-black text-2xl border drop-shadow-[0_0_15px_rgba(56,189,248,0.5)] bg-sky-500/10 text-sky-400 border-sky-500/30";
                        else if (letter === 'F') letterBox.className = "w-14 h-14 rounded-lg flex items-center justify-center font-black text-2xl border drop-shadow-[0_0_15px_rgba(239,68,68,0.5)] bg-red-500/10 text-red-500 border-red-500/30";
                        else letterBox.className = "w-14 h-14 rounded-lg flex items-center justify-center font-black text-2xl border drop-shadow-[0_0_15px_rgba(251,146,60,0.5)] bg-orange-500/10 text-orange-400 border-orange-500/30";
                    }
                }
                break;
            case 'groundOpsComplete':
                const gOC = document.getElementById('groundOpsContainer');
                let existingBanner = document.getElementById('groundOpsCompleteBanner');
                if (!existingBanner && gOC) {
                    gOC.insertAdjacentHTML('afterbegin', '<div id="groundOpsCompleteBanner" style="color:#34D399; font-weight:bold; margin-bottom:10px;">All ground operations are complete. Aircraft is secure.</div>');
                }
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
    'Refueling': '<span class="material-symbols-outlined text-[18px] text-orange-500">local_gas_station</span>',
    'Boarding': '<span class="material-symbols-outlined text-[18px] text-sky-400">group</span>',
    'Deboarding': '<span class="material-symbols-outlined text-[18px] text-sky-300">directions_run</span>',
    'Cargo': '<span class="material-symbols-outlined text-[18px] text-amber-500">luggage</span>',
    'Catering': '<span class="material-symbols-outlined text-[18px] text-pink-400">restaurant</span>',
    'Cleaning': '<span class="material-symbols-outlined text-[18px] text-fuchsia-400">cleaning_services</span>',
    'Cabin Clean (PNC)': '<span class="material-symbols-outlined text-[18px] text-indigo-400">dry_cleaning</span>',
    'Water/Waste': '<span class="material-symbols-outlined text-[18px] text-emerald-400">water_drop</span>'
};

const GO_NARRATIVES = {
    'Refuel': 'gops_desc_refuel',
    'Boarding': 'gops_desc_boarding',
    'Deboarding': 'gops_desc_deboarding',
    'Cargo': 'gops_desc_cargo',
    'Catering': 'gops_desc_catering',
    'Cleaning': 'gops_desc_cleaning',
    'PNC Chores': 'gops_desc_cleaning',
    'Water/Waste': 'gops_desc_water'
};

window.toggleAccordion = function (name) {
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

    if (metaFill) metaFill.style.width = percent + '%';
    
    const metaTitle = document.getElementById('dashMetaTitle');
    if (metaTitle) metaTitle.innerHTML = "GROUND OPERATIONS";

    if (metaText) {
        if (isFinished && totalDuration > 0) {
            metaBar.style.display = 'block';
            metaText.innerText = mDict.gops_meta_completed || "Ground Operations Completed";
            metaText.style.color = "#34D399";
            if (metaFill) metaFill.style.backgroundColor = "#34D399";
        } else if (blockingService) {
            metaBar.style.display = 'block';
            let color = getSeverityColor(blockingService.DelayAddedSec);
            let mins = Math.round(blockingService.DelayAddedSec / 60);
            metaText.innerText = `⚠️ ${blockingService.Name.toUpperCase()} : ${blockingService.ActiveDelayEvent} (+${mins}m)`;
            metaText.style.color = color;
            if (metaFill) { metaFill.style.backgroundColor = color; metaFill.style.boxShadow = `0 0 10px ${color}`; }
            setTimeout(() => { if (metaFill) metaFill.style.boxShadow = 'none'; }, 1000);
        } else if (window.recentlyCompleted && (Date.now() - window.recentlyCompletedTime < 15000)) {
            metaBar.style.display = 'block';
            const icon = GO_ICONS[window.recentlyCompleted.Name] || '✅';
            metaText.innerHTML = `${icon} <span style="font-weight:bold">${window.recentlyCompleted.Name.toUpperCase()} TERMINÉ</span>`;
            metaText.style.color = "#34D399";
            if (metaFill) metaFill.style.backgroundColor = "#34D399";
        } else if (isActive) {
            metaBar.style.display = 'block';
            metaText.innerText = "";
            if (metaTitle) metaTitle.innerHTML = `GROUND OPERATIONS IN PROGRESS (${Math.round(percent)}%)`;
            if (metaFill) metaFill.style.backgroundColor = "#4A90E2";
        } else {
            metaBar.style.display = 'none';
        }
    }
}

function renderGroundOps(services) {
    const containerDash = document.getElementById('dashboardGroundOpsPillsGrid');
    const containerBriefing = document.getElementById('groundOpsContainer');

    let html = '';

    if (!services || services.length === 0) {
        html = '<p class="text-slate-500 font-mono text-center delay-fade-in" data-i18n="ground_pending">Ground operations pending SimBrief initialization...</p>';
    } else {
        html = `
        <div class="flex flex-col gap-3">`;

    let isDeboardingActive = services.some(s => (s.Name || s.name) === "Deboarding" && (s.State !== undefined ? s.State : s.state) === 1);
    let isBoardingActive = services.some(s => (s.Name || s.name) === "Boarding" && (s.State !== undefined ? s.State : s.state) === 1);
    let isCleaningActive = services.some(s => (s.Name || s.name).includes("Clean") && (s.State !== undefined ? s.State : s.state) === 1);
    let isCateringActive = services.some(s => (s.Name || s.name).includes("Catering") && (s.State !== undefined ? s.State : s.state) === 1);
    let isPaxMoving = isDeboardingActive || isBoardingActive;
    let isCrewWorking = isCleaningActive || isCateringActive;

    let deboardingSrv = services.find(s => (s.Name || s.name) === "Deboarding");
    let boardingSrv = services.find(s => (s.Name || s.name) === "Boarding");
    let combinedServices = services.filter(s => (s.Name || s.name) !== "Deboarding" && (s.Name || s.name) !== "Boarding");

    if (deboardingSrv && boardingSrv) {
        let dState = deboardingSrv.State !== undefined ? deboardingSrv.State : deboardingSrv.state;
        let dIsCompleted = dState === 3 || dState === 4 || deboardingSrv.IsPreServiced || deboardingSrv.isPreServiced;
        if (!dIsCompleted) combinedServices.push(deboardingSrv);
        else combinedServices.push(boardingSrv);
    } else if (deboardingSrv) {
        combinedServices.push(deboardingSrv);
    } else if (boardingSrv) {
        combinedServices.push(boardingSrv);
    }

    combinedServices.forEach(s => {
        const mLang = (localStorage.getItem('selLanguage') || 'EN').toLowerCase();
        const mDict = window.locales && window.locales[mLang] ? window.locales[mLang] : window.locales.en;

        let locName = s.Name !== undefined ? s.Name : s.name;
        if (locName === "Refueling") locName = mDict.gops_refueling || locName;
        else if (locName === "Boarding") locName = mDict.gops_boarding || locName;
        else if (locName === "Deboarding") locName = mDict.gops_deboarding || locName;
        else if (locName === "Cargo" || locName === "Cargo/Luggage") {
            locName = mDict.gops_cargo || "Cargo";
            let nState = s.State !== undefined ? s.State : s.state;
            if (deboardingSrv) { // Turnaround
                if (nState === 1 && (s.ProgressPercent || 0) < 50) locName = `CARGO UNLOADING`;
                else if (nState === 1) locName = `CARGO LOADING`;
                else if (nState === 0 || nState === 5) locName = `CARGO UNLOAD/LOAD`;
            } else { // Pristine
                if (nState === 0 || nState === 1 || nState === 5) locName = `CARGO LOADING`;
            }
        }
        else if (locName === "Catering") locName = mDict.gops_catering || locName;
        else if (locName === "Cleaning") locName = mDict.gops_cleaning || locName;
        else if (locName === "Cabin Clean (PNC)" || locName === "PNC Chores") locName = "CLEANING (CREW)";
        else if (locName === "Water/Waste") locName = mDict.gops_water || locName;

        let stateVal = s.State !== undefined ? s.State : s.state;
        
        let iconKey = s.Name !== undefined ? s.Name : s.name;
        if (iconKey === "Cargo/Luggage") iconKey = "Cargo";
        if (iconKey === "PNC Chores") iconKey = "Cabin Clean (PNC)";
        const icon = GO_ICONS[iconKey] || '🔹';

        // No frontend physical blocks - let the backend/logic strictly dictate if it's startable.
        let isCompleted = stateVal === 3 || stateVal === 4 || s.IsPreServiced || s.isPreServiced;
        let isClickable = (stateVal === 0 || stateVal === 5) && window.isDispatchSignedOff;
        let actionName = s.Name === 'Deboarding' ? 'startDeboarding' : 'startService';
        let clickAction = isClickable ? `onclick="window.chrome.webview.postMessage({action: '${actionName}', service: '${(s.Name || s.name)}'})"` : '';

        // Custom Boarding Lock logic visually explicitly requested by user
        let boardBlockedText = null;
        if (s.Name === "Boarding") {
            let cleaningSrv = combinedServices.find(x => x.Name === "Cleaning" || 
                                                         x.Name === "Cabin Clean (PNC)" || 
                                                         x.Name === "PNC Chores" || 
                                                         x.Name === "CLEANING (CREW)");
            let cateringSrv = combinedServices.find(x => x.Name === "Catering");
            let block1 = cleaningSrv && (cleaningSrv.State === 1 || cleaningSrv.state === 1);
            let block2 = cateringSrv && (cateringSrv.State === 1 || cateringSrv.state === 1);
            if (block1 || block2) {
                boardBlockedText = "WAIT FOR OTHER OPERATIONS";
            }
        }

        // Center Area Logic
        let centerAreaHtml = '';
        let smMsg = s.StatusMessage !== undefined ? s.StatusMessage : s.statusMessage;

        if (stateVal === 0 || stateVal === 5) {
            let btnText = actionName === 'startDeboarding' ? 'START DEBOARDING' : `START ${(s.Name || s.name)}`;
            
            if (boardBlockedText) {
                centerAreaHtml = `<span class="text-orange-400 font-bold uppercase tracking-wide text-[9px] md:text-[10px] flex justify-center items-center gap-1 whitespace-nowrap w-[120px] md:w-[140px] flex-shrink-0 cursor-not-allowed" style="text-shadow: 0 0 10px rgba(249,115,22,0.3);"><span class="material-symbols-outlined text-[14px]">warning</span> WAIT FOR OPS</span>`;
                isClickable = false;
            } else if (smMsg && smMsg.toLowerCase().includes("blocked")) {
                centerAreaHtml = `<span class="text-orange-400 font-bold uppercase tracking-widest text-[9px] md:text-[10px] flex justify-center items-center gap-2 whitespace-nowrap w-[120px] md:w-[140px] flex-shrink-0 cursor-not-allowed">${smMsg.toUpperCase()}</span>`;
                isClickable = false;
            } else {
                centerAreaHtml = `<button ${clickAction} class="bg-sky-500/10 text-sky-400 border border-sky-500/20 w-[120px] md:w-[140px] py-1.5 md:py-2 rounded text-[9px] md:text-[10px] font-bold tracking-widest hover:bg-sky-500 hover:text-white transition-all uppercase shadow-[0_0_10px_rgba(56,189,248,0.1)] outline-none whitespace-nowrap flex-shrink-0">${btnText}</button>`;
            }
        } else if (stateVal === 1 || stateVal === 2) {
            if (stateVal === 1) centerAreaHtml = `<span class="text-sky-400 font-bold uppercase tracking-widest text-[9px] md:text-[10px] flex justify-center items-center gap-2 whitespace-nowrap w-[120px] md:w-[140px] flex-shrink-0"><span class="animate-pulse shadow-[0_0_10px_rgba(56,189,248,0.5)] bg-sky-400 rounded-full w-2 h-2"></span> ${smMsg ? smMsg.toUpperCase() : 'IN PROGRESS'}</span>`;
            if (stateVal === 2) centerAreaHtml = `<span class="text-orange-400 font-bold uppercase tracking-widest text-[9px] md:text-[10px] flex justify-center items-center gap-2 whitespace-nowrap w-[120px] md:w-[140px] flex-shrink-0"><span class="animate-pulse shadow-[0_0_10px_rgba(249,115,22,0.5)] bg-orange-400 rounded-full w-2 h-2"></span> DELAYED</span>`;
        } else if (isCompleted) {
            centerAreaHtml = `<span class="text-emerald-500 font-bold uppercase tracking-widest text-[10px] flex justify-center items-center gap-2 whitespace-nowrap w-[120px] md:w-[140px] flex-shrink-0">COMPLETED</span>`;
        } else if (stateVal === 4) { // Skipped
            centerAreaHtml = `<span class="text-slate-500 font-bold uppercase tracking-widest text-[10px] flex justify-center items-center gap-2 whitespace-nowrap w-[120px] md:w-[140px] flex-shrink-0">SKIPPED</span>`;
        }

        let timeDisplay = '';
        let remainingSec = s.RemainingSec !== undefined ? s.RemainingSec : s.remainingSec;
        if (remainingSec > 0 && stateVal !== 3 && stateVal !== 4) {
            const m = Math.floor(remainingSec / 60).toString().padStart(2, '0');
            const sec = (remainingSec % 60).toString().padStart(2, '0');
            let colorClass = stateVal === 2 ? 'text-orange-400 drop-shadow-[0_0_10px_rgba(249,115,22,0.3)]' : 'text-sky-400 drop-shadow-[0_0_10px_rgba(56,189,248,0.3)]';
            timeDisplay = `<span class="font-mono text-base md:text-lg font-black tracking-widest ${colorClass} text-right tabular-nums w-[50px] flex-shrink-0">${m}:${sec}</span>`;
        } else if (stateVal === 3 || stateVal === 4) {
            timeDisplay = `<span class="font-mono text-base md:text-lg font-black tracking-widest text-slate-600 text-right tabular-nums w-[50px] flex-shrink-0">--:--</span>`;
        }

        let extraBadgesHtml = '';
        if (s.Name === "Catering" || s.Name === "Cleanliness" || s.Name === "Cleaning" || s.Name === "Cabin Clean (PNC)" || s.Name === "Water/Waste") {
            if (!isCompleted) {
                // SKIP button
                extraBadgesHtml += `<button onclick="event.stopPropagation(); window.chrome.webview.postMessage({ action: 'skipService', service: '${(s.Name || s.name)}' });" class="px-2 py-1 rounded bg-[#1a1c23] hover:bg-red-500/10 text-red-500/50 hover:text-red-500 border border-white/5 hover:border-red-500/20 text-[9px] uppercase font-bold tracking-widest leading-none outline-none transition-colors flex-shrink-0 cursor-pointer mr-0 md:mr-3">SKIP</button>`;
            }
        }

        let barColor = stateVal === 3 ? '#34D399' : (stateVal === 2 ? '#FB923C' : '#38BDF8');
        if (isCompleted && !(s.IsPreServiced || s.isPreServiced)) barColor = '#34D399';
        else if (isCompleted && (s.IsPreServiced || s.isPreServiced)) barColor = '#475569';

        let rowClasses = `w-full grid grid-cols-[1fr_auto_80px] md:grid-cols-[1.5fr_160px_130px] items-center p-3 md:p-4 bg-[#1a1d24]/40 border border-white/5 rounded-xl transition-all relative overflow-hidden group`;
        if (isClickable) rowClasses += ` cursor-pointer hover:bg-[#1a1d24]/80 hover:border-sky-500/30`;
        if (!window.isDispatchSignedOff) rowClasses += ` opacity-25 grayscale pointer-events-none`;

        let progressHtml = '';
        if (s.Name === "Water/Waste") {
            let waterLvl = s.State === 1 ? s.ProgressPercent : Math.round(window.lastTelemetry?.waterLevel || 100);
            let wasteLvl = s.State === 1 ? s.ProgressPercent : Math.round(window.lastTelemetry?.wasteLevel || 0);
            let wColor = waterLvl < 20 ? '#EF4444' : (waterLvl < 50 ? '#F59E0B' : '#60A5FA');
            let waColor = wasteLvl > 90 ? '#EF4444' : (wasteLvl > 70 ? '#F59E0B' : '#60A5FA');
            progressHtml = `
                <div class="absolute bottom-0 left-0 w-full flex flex-col gap-[1px] bg-black/40 h-1.5">
                    <div class="h-1"><div class="h-full transition-all duration-1000 ease-out" style="width: ${waterLvl}%; background-color: ${wColor}; opacity: 0.8"></div></div>
                    <div class="h-1"><div class="h-full transition-all duration-1000 ease-out" style="width: ${wasteLvl}%; background-color: ${waColor}; opacity: 0.8"></div></div>
                </div>`;
        } else {
            let mappedProgress = s.ProgressPercent;
            let mappedColor = barColor;
            if (s.Name === "Deboarding") mappedProgress = 100 - (s.ProgressPercent || 0);

            if (s.State !== 1 && window.lastTelemetry && !isCompleted) {
                if (s.Name === "Catering") {
                    mappedProgress = window.lastTelemetry.cateringCompletion !== undefined ? window.lastTelemetry.cateringCompletion : 100;
                    mappedColor = mappedProgress < 20 ? '#EF4444' : (mappedProgress < 50 ? '#F59E0B' : '#34D399');
                } else if (s.Name === "Cleanliness" || s.Name === "Cleaning") {
                    mappedProgress = window.lastTelemetry.cabinCleanliness !== undefined ? window.lastTelemetry.cabinCleanliness : 100;
                    mappedColor = mappedProgress < 50 ? '#EF4444' : (mappedProgress < 75 ? '#F59E0B' : '#34D399');
                }
            }
            progressHtml = `<div class="absolute bottom-0 left-0 w-full h-[2px] bg-black/40"><div class="h-full transition-all duration-1000 ease-out" style="width: ${mappedProgress}%; background-color: ${mappedColor}; opacity: 0.8"></div></div>`;
        }

        let rowProps = isClickable ? clickAction : '';

        html += `
            <div class="${rowClasses}" ${rowProps}>
                ${progressHtml}
                
                <!-- Left: Title & Icon -->
                <div class="flex items-center min-w-0 pr-2 md:pr-4">
                    <span class="text-xl md:text-2xl mr-3 text-white/50 shrink-0 w-[30px] flex justify-center">${icon}</span>
                    <strong class="font-headline tracking-widest text-[#e2e8f0] uppercase text-[11px] md:text-[13px] whitespace-normal leading-tight">${locName.toUpperCase()}</strong>
                </div>

                <!-- Center: Action/Status -->
                <div class="flex justify-center items-center shrink-0 w-full">
                    ${centerAreaHtml}
                </div>

                <!-- Right: Extra Badges & Timer -->
                <div class="flex items-center justify-end pl-2 md:pl-4 min-w-0">
                    <div class="flex items-center gap-1 justify-end mr-2 md:mr-4">
                        ${extraBadgesHtml}
                    </div>
                    ${timeDisplay}
                </div>

                <!-- Optional Sub Container -->
                <div id="ge-container-${(s.Name || s.name).replace(/\s|[^\w]/g, '')}" class="hidden col-span-3"></div>
            </div>
        `;
    });

    html += '</div>';
    }

    if (!window.isDispatchSignedOff) {
        // Overlay disabled per user request
    }

    if (containerDash) containerDash.innerHTML = html;
    if (containerBriefing) containerBriefing.innerHTML = html;
}

window.renderManifest = function (manifest) {
    manifest = manifest || window.manifest;
    const container = document.getElementById('manifestContainer');
    if (!container) return;

    let flightCrew = manifest?.FlightCrew || manifest?.flightCrew;
    let passengers = manifest?.Passengers || manifest?.passengers || (Array.isArray(manifest) ? manifest : null);

    if (!manifest || (!flightCrew && !passengers)) {
        container.innerHTML = '<p style="color:#64748b;">Waiting for final manifest processing...</p>';
        return;
    }

    if (passengers && passengers.length === 0) {
        container.innerHTML = '<p style="color:#64748b;">No passengers listed on this flight plan.</p>';
        return;
    }

    // IN-PLACE DOM UPDATE TO PREVENT FLICKERING ON HOVER
    const existingMap = document.getElementById('seatMapContent');
    const expectedPaxCount = container.dataset.flightPaxCount ? parseInt(container.dataset.flightPaxCount) : -1;

    if (existingMap && expectedPaxCount === passengers.length) {
        let boardedCount = 0;
        let fastenedCount = 0;
        let injuredCount = 0;

        // PRE-CLEAR ALL DOM SEATS to avoid "Ghosts"
        const allSeats = existingMap.querySelectorAll('.seat');
        allSeats.forEach(s => {
            s.className = 'seat';
            s.innerHTML = '';
            s.dataset.initialized = '';
        });

        passengers.forEach(p => {
            const isBoarded = p.IsBoarded === true || p.isBoarded === true;
            if (isBoarded) {
                boardedCount++;
                if (p.IsSeatbeltFastened) fastenedCount++;
                if (p.IsInjured) injuredCount++;
            }
            
            let seatEl = document.getElementById('seat-' + p.Seat);
            if (seatEl) {
                if (isBoarded) {
                    const seatClass = p.IsSeatbeltFastened ? 'fastened' : 'unfastened';
                    seatEl.className = `seat ${seatClass} relative`;
                    const injuryHtml = p.IsInjured ? '<span class="material-symbols-outlined text-[10px] text-red-500 absolute -top-1 -left-1 animate-pulse" style="z-index:10;">medical_services</span>' : '';
                    if (!seatEl.dataset.initialized) {
                        seatEl.innerHTML = `${injuryHtml}<span class="tooltip">${p.Seat} : ${p.Name} (${p.Nationality})</span>`;
                        seatEl.dataset.initialized = 'true';
                    } else if (p.IsInjured && seatEl.innerHTML.indexOf('medical_services') === -1) {
                        seatEl.innerHTML = `${injuryHtml}<span class="tooltip">${p.Seat} : ${p.Name} (${p.Nationality})</span>`;
                    }
                } else {
                    // Force clear if not boarded
                    seatEl.className = 'seat';
                    seatEl.innerHTML = '';
                    seatEl.dataset.initialized = '';
                }
            }
        });

        let headerLabel = document.getElementById('paxListHeader');
        if (headerLabel) {
            headerLabel.innerText = `LIST (${boardedCount} / ${passengers.length} PAX)`;
        }

        let totalSeats = container.dataset.totalSeats ? parseInt(container.dataset.totalSeats) : expectedPaxCount;
        let unfastenedCount = boardedCount - fastenedCount;
        let emptyCount = totalSeats - boardedCount;

        let elFast = document.getElementById('legFastenedVal');
        if (elFast) elFast.innerText = fastenedCount;
        let elUnfast = document.getElementById('legUnfastenedVal');
        if (elUnfast) elUnfast.innerText = unfastenedCount;
        let elEmpty = document.getElementById('legEmptyVal');
        if (elEmpty) elEmpty.innerText = emptyCount;
        let elInj = document.getElementById('legInjuredVal');
        if (elInj) elInj.innerText = injuredCount;

        return; // Fast update complete!
    }

    container.dataset.flightPaxCount = passengers.length;

    let maxRow = 0;
    let hasLettersGHK = false; // check if widebody
    passengers.forEach(p => {
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

    let totalAircraftSeats = maxRow * (lettersLeft.length + lettersCenter.length + lettersRight.length);
    container.dataset.totalSeats = totalAircraftSeats;

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
                min-width: 22px;
                height: 26px;
                min-height: 26px;
                flex-shrink: 0;
                border-radius: 4px 4px 2px 2px;
                background: #334155;
                border: 1px solid #475569;
                position: relative;
                cursor: default;
                transition: all 0.2s;
            }
            .seat.fastened {
                background: #0ea5e9;
                border-color: #38bdf8;
                box-shadow: inset 0 -4px 0 rgba(0,0,0,0.3);
            }
            .seat.fastened:hover, .seat.unfastened:hover {
                transform: translateY(-2px);
            }
            .seat.fastened:hover {
                background: #38bdf8;
            }
            .seat.unfastened {
                background: #ef4444;
                border-color: #f87171;
                box-shadow: inset 0 -4px 0 rgba(0,0,0,0.3);
            }
            .seat.unfastened:hover {
                background: #f87171;
            }
            .seat .tooltip {
                visibility: hidden;
                background-color: #f8fafc;
                color: #0f172a;
                text-align: center;
                border-radius: 4px;
                position: absolute;
                z-index: 50;
                pointer-events: none;
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
            .seat:hover .tooltip {
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
            let p = passengers.find(x => (x.Seat || x.seat) === sId);
            if (p) {
                if (p.IsBoarded === true || p.isBoarded === true) {
                    const isFastened = p.IsSeatbeltFastened === true || p.isSeatbeltFastened === true;
                    const seatClass = isFastened ? 'fastened' : 'unfastened';
                    const isInjured = p.IsInjured === true || p.isInjured === true;
                    const injuryIcon = isInjured ? '<span class="material-symbols-outlined text-[10px] text-red-500 absolute -top-1 -left-1 animate-pulse" style="z-index:10;">medical_services</span>' : '';
                    seatMapHtml += `<div id="seat-${sId}" class="seat ${seatClass} relative" data-initialized="true">
                        ${injuryIcon}
                        <span class="tooltip">${p.Seat || p.seat} : ${p.Name || p.name} (${p.Nationality || p.nationality})</span>
                    </div>`;
                } else {
                    seatMapHtml += `<div id="seat-${sId}" class="seat"></div>`;
                }
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
                let p = passengers.find(x => (x.Seat || x.seat) === sId);
                if (p) {
                    if (p.IsBoarded === true || p.isBoarded === true) {
                        const isFastened = p.IsSeatbeltFastened === true || p.isSeatbeltFastened === true;
                        const seatClass = isFastened ? 'fastened' : 'unfastened';
                        const isInjured = p.IsInjured === true || p.isInjured === true;
                        const injuryIcon = isInjured ? '<span class="material-symbols-outlined text-[10px] text-red-500 absolute -top-1 -left-1 animate-pulse" style="z-index:10;">medical_services</span>' : '';
                        seatMapHtml += `<div id="seat-${sId}" class="seat ${seatClass} relative" data-initialized="true">
                            ${injuryIcon}
                            <span class="tooltip">${p.Seat || p.seat} : ${p.Name || p.name} (${p.Nationality || p.nationality})</span>
                        </div>`;
                    } else {
                        seatMapHtml += `<div id="seat-${sId}" class="seat"></div>`;
                    }
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
            let p = passengers.find(x => (x.Seat || x.seat) === sId);
            if (p) {
                if (p.IsBoarded === true || p.isBoarded === true) {
                    const isFastened = p.IsSeatbeltFastened === true || p.isSeatbeltFastened === true;
                    const seatClass = isFastened ? 'fastened' : 'unfastened';
                    const isInjured = p.IsInjured === true || p.isInjured === true;
                    const injuryIcon = isInjured ? '<span class="material-symbols-outlined text-[10px] text-red-500 absolute -top-1 -left-1 animate-pulse" style="z-index:10;">medical_services</span>' : '';
                    seatMapHtml += `<div id="seat-${sId}" class="seat ${seatClass} relative" data-initialized="true">
                        ${injuryIcon}
                        <span class="tooltip">${p.Seat || p.seat} : ${p.Name || p.name} (${p.Nationality || p.nationality})</span>
                    </div>`;
                } else {
                    seatMapHtml += `<div id="seat-${sId}" class="seat"></div>`;
                }
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
    const legFastened = mDict.man_leg_fastened || "Fastened";
    const legUnfastened = mDict.man_leg_unfastened || "Unfastened";
    const legEmpty = mDict.man_leg_empty || "Empty";
    const legInjured = mDict.man_leg_injured || "Injured";

    let boardedInitialCount = passengers.filter(p => p.IsBoarded === true || p.isBoarded === true).length;

    let html = `
        <div style="display:flex; gap: 40px; justify-content: space-between; height: 100%;">
            <div style="flex: 1; min-width: 250px; display: flex; flex-direction: column; height: 100%;">
                <div style="flex-shrink: 0;">
                    <h3 class="text-xs font-label tracking-[0.4em] text-sky-400 uppercase opacity-80 border-b border-white/5 pb-3 mb-4">${flightCrewLabel} (${(manifest.FlightCrew || manifest.flightCrew).length})</h3>
                    <ul style="list-style:none; padding:0; margin:0; line-height: 1.8; color:#cbd5e1; margin-bottom: 20px;">
    `;

    let cabCrewRendered = false;
    (manifest.FlightCrew || manifest.flightCrew).forEach(c => {
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

    let fastenedCount = passengers.filter(p => (p.IsBoarded === true || p.isBoarded === true) && (p.IsSeatbeltFastened === true || p.isSeatbeltFastened === true)).length;
    let unfastenedCount = boardedInitialCount - fastenedCount;
    let injuredCount = passengers.filter(p => (p.IsBoarded === true || p.isBoarded === true) && (p.IsInjured === true || p.isInjured === true)).length;
    let fallbackAircraftSeats = container.dataset.totalSeats ? parseInt(container.dataset.totalSeats) : passengers.length;
    let emptyCount = fallbackAircraftSeats - boardedInitialCount;

    html += `       </ul>
                    <div class="border-b border-white/5 pb-3 mb-4" style="display:flex; justify-content:space-between; align-items:flex-end;">
                        <h3 id="paxListHeader" class="text-xs font-label tracking-[0.4em] text-sky-400 uppercase opacity-80" style="margin:0;">${paxListLabel} (${boardedInitialCount} / ${passengers.length} PAX)</h3>
                    </div>
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

    passengers.forEach(p => {
        html += `
            <tr style="border-bottom: 1px solid rgba(51, 65, 85, 0.4);">
                <td style="padding: 3px 4px; color: #38BDF8; font-weight: bold;">${p.Seat || p.seat}</td>
                <td style="padding: 3px 4px;">${p.Name || p.name}</td>
                <td style="padding: 3px 4px;">${p.Nationality || p.nationality}</td>
                <td style="padding: 3px 4px; text-align: center;">${p.Age || p.age}</td>
            </tr>
        `;
    });

    html += `           </tbody>
                    </table>
                </div>
            </div>
            
            <div style="flex: 1.5; min-width: 380px; display: flex; flex-direction: column; text-align: center; height: 100%;">
                <div class="flex justify-between items-center border-b border-white/5 pb-3 mb-4 flex-shrink-0">
                    <h3 class="text-xs font-label tracking-[0.4em] text-sky-400 uppercase opacity-80 mb-0">${mapLabel}</h3>
                    <div class="flex gap-4 text-[9px] font-label tracking-widest text-slate-400 uppercase">
                        <div class="flex items-center gap-1"><div class="w-2.5 h-2.5 rounded bg-[#0ea5e9]"></div> <span id="legFastenedVal">${fastenedCount}</span> ${legFastened}</div>
                        <div class="flex items-center gap-1"><div class="w-2.5 h-2.5 rounded bg-[#ef4444]"></div> <span id="legUnfastenedVal">${unfastenedCount}</span> ${legUnfastened}</div>
                        <div class="flex items-center gap-1"><div class="w-2.5 h-2.5 rounded bg-[#334155]"></div> <span id="legEmptyVal">${emptyCount}</span> ${legEmpty}</div>
                        <div class="flex items-center gap-1"><span class="material-symbols-outlined text-[10px] text-red-500">medical_services</span> <span id="legInjuredVal">${injuredCount}</span> ${legInjured}</div>
                    </div>
                </div>
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
        window.manifestPanZoom = window.manifestPanZoom || { scale: 0.98, currentX: 0, currentY: 0 };
        let isDown = false;
        let startX, startY;

        // Restore global Pan/Zoom variables
        content.style.transform = `translate(${window.manifestPanZoom.currentX}px, ${window.manifestPanZoom.currentY}px) scale(${window.manifestPanZoom.scale})`;

        viewport.addEventListener('wheel', (e) => {
            e.preventDefault();
            content.style.transition = 'transform 0.1s ease-out';
            const zoomSensitivity = 0.001;
            window.manifestPanZoom.scale -= e.deltaY * zoomSensitivity;
            window.manifestPanZoom.scale = Math.max(0.3, Math.min(3, window.manifestPanZoom.scale));
            content.style.transform = `translate(${window.manifestPanZoom.currentX}px, ${window.manifestPanZoom.currentY}px) scale(${window.manifestPanZoom.scale})`;
        });

        viewport.addEventListener('mousedown', (e) => {
            if (e.button === 1 || e.button === 2 || e.button === 0) { // Middle or Right or Left
                isDown = true;
                viewport.style.cursor = 'grabbing';
                startX = e.clientX - window.manifestPanZoom.currentX;
                startY = e.clientY - window.manifestPanZoom.currentY;
            }
        });

        window.addEventListener('mouseup', () => {
            isDown = false;
            if (viewport) viewport.style.cursor = 'grab';
        });

        window.addEventListener('mousemove', (e) => {
            if (!isDown) return;
            e.preventDefault();
            content.style.transition = 'none'; // remove transition for smooth drag
            window.manifestPanZoom.currentX = e.clientX - startX;
            window.manifestPanZoom.currentY = e.clientY - startY;
            content.style.transform = `translate(${window.manifestPanZoom.currentX}px, ${window.manifestPanZoom.currentY}px) scale(${window.manifestPanZoom.scale})`;
        });

        // Prevent context menu on right click inside viewport
        viewport.addEventListener('contextmenu', e => e.preventDefault());
    }
};

function renderLogbook(history) {
    const grid = document.getElementById('logbookGrid');
    if (!grid) return;

    if (!history || history.length === 0) {
        grid.innerHTML = `<div class="text-center py-12 text-slate-500 font-label tracking-widest text-xs uppercase" data-i18n="logb_empty">No flight logs recorded yet.</div>`;
        return;
    }

    grid.innerHTML = history.map((f, i) => {
        const isSuper = f.Score >= 500;
        const colorPill = isSuper ? 'bg-emerald-500 shadow-[0_0_10px_rgba(16,185,129,0.5)]' : 'bg-red-500 shadow-[0_0_10px_rgba(239,68,68,0.5)]';
        const dateStr = new Date(f.FlightDate).toLocaleDateString([], { month: 'short', day: '2-digit', year: 'numeric', hour: '2-digit', minute: '2-digit' });
        const blkFormat = f.BlockTime > 0 ? `${Math.floor(f.BlockTime / 60)}h ${f.BlockTime % 60}m` : '0m';

        const payloadStr = encodeURIComponent(JSON.stringify(f)).replace(/'/g, "%27");

        return `
        <div class="bg-black/20 hover:bg-[#1C1F26] p-4 rounded-xl border border-white/5 relative hover:border-sky-500/30 transition-colors cursor-pointer group flex items-center justify-between" onclick="replayFlightLog('${payloadStr}')">
            
            <!-- Date & Flight -->
            <div class="flex items-center gap-6 w-[30%] shrink-0">
                <div class="w-2 h-2 rounded-full ${colorPill} shrink-0"></div>
                <div>
                    <div class="text-[10px] text-slate-500 font-label tracking-widest uppercase">${dateStr}</div>
                    <div class="text-sm font-black text-white uppercase tracking-widest mt-1">${f.Airline}${f.FlightNo}</div>
                </div>
            </div>

            <!-- Route -->
            <div class="flex items-center gap-3 w-[20%] justify-center shrink-0">
                <span class="text-lg font-headline font-black text-white tracking-widest">${f.Dep}</span>
                <span class="material-symbols-outlined text-slate-600 text-[16px] group-hover:text-sky-500/50 transition-colors">flight_takeoff</span>
                <span class="text-lg font-headline font-black text-white tracking-widest">${f.Arr}</span>
            </div>

            <!-- Stats -->
            <div class="flex items-center justify-end gap-6 w-[50%] pr-4 font-mono text-xs">
                <div class="flex flex-col items-end">
                    <span class="text-slate-500 uppercase tracking-widest text-[9px] font-manrope font-bold mb-1">Block Time</span>
                    <span class="text-slate-300 font-bold">${blkFormat}</span>
                </div>
                <div class="flex flex-col items-end">
                    <span class="text-slate-500 uppercase tracking-widest text-[9px] font-manrope font-bold mb-1">Touchdown</span>
                    <span class="text-slate-300 font-bold">${Math.round(f.TouchdownFpm)} fpm</span>
                </div>
                <div class="flex flex-col items-end w-24">
                    <span class="text-slate-500 uppercase tracking-widest text-[9px] font-manrope font-bold mb-1">Score</span>
                    <span class="text-${isSuper ? 'emerald' : 'red'}-400 font-bold text-sm">${f.Score} <span class="text-[9px] text-slate-500">pts</span></span>
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

        window.chrome.webview.dispatchEvent(spoofedEvent);
        document.getElementById('flightReportModal').style.display = 'flex';
    } catch (e) {
        console.error("Failed to parse historical log payload", e);
    }
}

setTimeout(() => {
    if (window.chrome && window.chrome.webview) {
        window.chrome.webview.postMessage({ action: 'uiReady' });
    }
}, 500);

// --- TIME SKIP WIDGET LOGIC ---
let isTimeSkipDragging = false;
let tsDragStartX = 0;
let tsDragStartY = 0;
const timeSkipModal = document.getElementById('timeSkipModal');

if (timeSkipModal) {
    timeSkipModal.addEventListener('mousedown', (e) => {
        if (e.target.closest('button')) return;
        isTimeSkipDragging = true;
        tsDragStartX = e.clientX - timeSkipModal.offsetLeft;
        tsDragStartY = e.clientY - timeSkipModal.offsetTop;
    });

    document.addEventListener('mousemove', (e) => {
        if (!isTimeSkipDragging) return;
        timeSkipModal.style.left = `${e.clientX - tsDragStartX}px`;
        timeSkipModal.style.top = `${e.clientY - tsDragStartY}px`;
        timeSkipModal.style.right = 'auto'; // allow free movement
    });

    document.addEventListener('mouseup', () => {
        isTimeSkipDragging = false;
    });
}

window.requestTimeSkip = function (minutes) {
    if (window.chrome && window.chrome.webview) {
        window.chrome.webview.postMessage({ action: 'requestTimeSkip', minutes: minutes });
        const modal = document.getElementById('timeSkipModal');
        if (modal) modal.classList.add('hidden');
    }
};

// Expose a method to handle showing hiding based on FlightPhase (from telemetry)
window.checkTimeSkipVisibility = function (phase) {
    if (!timeSkipModal) return;
    if (phase !== 'Turnaround' && phase !== 'AtGate' && phase !== 0 && phase !== 9) {
        timeSkipModal.classList.add('hidden');
        timeSkipModal.classList.remove('flex');
    }
};

// --- SYSTEM MODAL HELPER ---
window.showSystemConfirm = function(options) {
    const modal = document.getElementById('systemConfirmModal');
    if (!modal) return;

    const titleEl = document.getElementById('modalTitle');
    const messageEl = document.getElementById('modalMessage');
    const iconEl = document.getElementById('modalIcon');
    const btnConfirm = document.getElementById('btnModalConfirm');
    const btnCancel = document.getElementById('btnModalCancel');

    titleEl.innerText = options.title || 'Confirmation';
    messageEl.innerText = options.message || '';
    iconEl.innerText = options.icon || 'help_outline';
    btnConfirm.innerText = options.confirmText || 'Confirm';
    
    if (options.isAlertOnly) {
        btnCancel.classList.add('hidden');
    } else {
        btnCancel.classList.remove('hidden');
    }

    const close = () => {
        modal.classList.add('hidden');
        modal.classList.remove('flex');
    };

    btnConfirm.onclick = () => {
        close();
        if (options.onConfirm) options.onConfirm();
    };

    btnCancel.onclick = () => {
        close();
        if (options.onCancel) options.onCancel();
    };

    modal.classList.remove('hidden');
    modal.classList.add('flex');
};

document.addEventListener('DOMContentLoaded', () => {
    // --- QUICK LOAD SHEET MODAL DRAG LOGIC ---
    let isQuickLoadDragging = false;
    let qlDragStartX = 0;
    let qlDragStartY = 0;
    const quickLoadModal = document.getElementById('quickLoadSheetModal');
    const quickLoadHeader = document.getElementById('quickLoadSheetHeader');

    if (quickLoadModal && quickLoadHeader) {
        quickLoadHeader.addEventListener('mousedown', (e) => {
            if (e.target.closest('button')) return; // ignore close button
            isQuickLoadDragging = true;
            qlDragStartX = e.clientX - quickLoadModal.offsetLeft;
            qlDragStartY = e.clientY - quickLoadModal.offsetTop;
            document.body.style.userSelect = 'none'; // prevent text selection
        });

        document.addEventListener('mousemove', (e) => {
            if (!isQuickLoadDragging) return;
            quickLoadModal.style.left = `${e.clientX - qlDragStartX}px`;
            quickLoadModal.style.top = `${e.clientY - qlDragStartY}px`;
            quickLoadModal.style.right = 'auto'; // release right constraint
            quickLoadModal.style.bottom = 'auto';
            quickLoadModal.style.margin = '0'; // clear margins that might break positioning
        });

        document.addEventListener('mouseup', () => {
            if (isQuickLoadDragging) {
                isQuickLoadDragging = false;
                document.body.style.userSelect = '';
            }
        });
    }
});

// Carousel function for Ground Ops and Timing pages
window.currentDashPage = 1;
window.toggleDashPage = function (dir) {
    window.currentDashPage += dir;
    if (window.currentDashPage > 2) window.currentDashPage = 1;
    if (window.currentDashPage < 1) window.currentDashPage = 2;
    
    // Page IDs in index.html: 'dashPage1_Timing' and 'dashPage2_GroundOps'
    const page1 = document.getElementById('dashPage1_Timing');
    const page2 = document.getElementById('dashPage2_GroundOps');
    
    if (page1 && page2) {
        if (window.currentDashPage === 1) {
            page1.style.display = 'flex';
            page2.style.display = 'none';
        } else {
            page1.style.display = 'none';
            page2.style.display = 'flex';
            
            // Clean up old tailwind 'hidden' class just in case to prevent specificity conflicts
            page1.classList.remove('hidden');
            page2.classList.remove('hidden');
        }
    }
};
