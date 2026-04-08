window.manifest = null;

if (window.chrome && window.chrome.webview) {
    window.chrome.webview.addEventListener('message', event => {
        const payload = event.data;
        if (payload.action === 'manifestUpdate' || payload.type === 'manifestUpdate') {
            if (payload.manifest) {
                window.manifest = payload.manifest;
                window.renderManifest(payload.manifest);
            }
        }
    });

    // Ask for manifest upon load
    window.chrome.webview.postMessage({ action: 'requestManifest' });
}
window.renderManifest = function (manifest) {
    const container = document.getElementById('manifestContainer');
    if (!container) return;

    let flightCrew = manifest?.FlightCrew || manifest?.flightCrew;
    let passengers = manifest?.Passengers || manifest?.passengers;

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

    if (existingMap && expectedPaxCount === (manifest.Passengers || manifest.passengers).length) {
        let boardedCount = 0;
        let fastenedCount = 0;
        let injuredCount = 0;

        (manifest.Passengers || manifest.passengers).forEach(p => {
            if (p.IsBoarded === true || p.isBoarded === true) {
                boardedCount++;
                if (p.IsSeatbeltFastened) fastenedCount++;
                if (p.IsInjured) injuredCount++;
            }
            let seatEl = document.getElementById('seat-' + p.Seat);
            if (seatEl) {
                if (p.IsBoarded === true || p.isBoarded === true) {
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
                    seatEl.className = 'seat';
                    seatEl.innerHTML = '';
                    seatEl.dataset.initialized = '';
                }
            }
        });

        let headerLabel = document.getElementById('paxListHeader');
        if (headerLabel) {
            headerLabel.innerText = `LIST (${boardedCount} / ${(manifest.Passengers || manifest.passengers).length} PAX)`;
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

    container.dataset.flightPaxCount = (manifest.Passengers || manifest.passengers).length;

    let maxRow = 0;
    let hasLettersGHK = false; // check if widebody
    (manifest.Passengers || manifest.passengers).forEach(p => {
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
            let p = (manifest.Passengers || manifest.passengers).find(x => (x.Seat || x.seat) === sId);
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
                let p = (manifest.Passengers || manifest.passengers).find(x => (x.Seat || x.seat) === sId);
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
            let p = (manifest.Passengers || manifest.passengers).find(x => (x.Seat || x.seat) === sId);
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

    let boardedInitialCount = (manifest.Passengers || manifest.passengers).filter(p => p.IsBoarded === true || p.isBoarded === true).length;

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
        let cName = c.Name;
        
        // Règle Bug 14 : Le nom du Captain doit être celui du profil joueur
        if (c.Role.toLowerCase().includes("captain") || c.Role === "CDB") {
            displayRole = mDict.crew_capt || c.Role;
            const profileName = localStorage.getItem('sbProfileCallsign');
            if (profileName && profileName !== 'MAVERICK') {
                cName = profileName;
            }
        } else if (c.Role.toLowerCase().includes("officer") || c.Role === "OPL") {
            displayRole = mDict.crew_fo || c.Role;
        }

        html += `<li><strong style="color: #60A5FA;">${displayRole}:</strong> ${cName}</li>`;

    });

    const paxListLabel = mDict.manifest_list || "LIST";
    const thSeat = mDict.man_th_seat || "Seat";
    const thName = mDict.man_th_name || "Name";
    const thNat = mDict.man_th_nat || "Nat.";
    const thAge = mDict.man_th_age || "Age";

    let fastenedCount = (manifest.Passengers || manifest.passengers).filter(p => (p.IsBoarded === true || p.isBoarded === true) && (p.IsSeatbeltFastened === true || p.isSeatbeltFastened === true)).length;
    let unfastenedCount = boardedInitialCount - fastenedCount;
    let injuredCount = (manifest.Passengers || manifest.passengers).filter(p => (p.IsBoarded === true || p.isBoarded === true) && (p.IsInjured === true || p.isInjured === true)).length;
    let fallbackAircraftSeats = container.dataset.totalSeats ? parseInt(container.dataset.totalSeats) : (manifest.Passengers || manifest.passengers).length;
    let emptyCount = fallbackAircraftSeats - boardedInitialCount;

    html += `       </ul>
                    <div class="border-b border-white/5 pb-3 mb-4" style="display:flex; justify-content:space-between; align-items:flex-end;">
                        <h3 id="paxListHeader" class="text-xs font-label tracking-[0.4em] text-sky-400 uppercase opacity-80" style="margin:0;">${paxListLabel} (${boardedInitialCount} / ${(manifest.Passengers || manifest.passengers).length} PAX)</h3>
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

    (manifest.Passengers || manifest.passengers).forEach(p => {
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
                    <div class="flex gap-4 text-[9px] font-label tracking-widest text-[#b6b6b6] uppercase">
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
        grid.innerHTML = `<div class="text-center py-12 text-[#7b7b7b] font-label tracking-widest text-xs uppercase" data-i18n="logb_empty">No flight logs recorded yet.</div>`;
        return;
    }

    grid.innerHTML = history.map((f, i) => {
        const isSuper = f.Score >= 500;
        const colorPill = isSuper ? 'bg-emerald-500 shadow-[0_0_10px_rgba(16,185,129,0.5)]' : 'bg-red-500 shadow-[0_0_10px_rgba(239,68,68,0.5)]';
        const dateStr = new Date(f.FlightDate).toLocaleDateString([], { month: 'short', day: '2-digit', year: 'numeric', hour: '2-digit', minute: '2-digit' });
        const blkFormat = f.BlockTime > 0 ? `${Math.floor(f.BlockTime / 60)}h ${f.BlockTime % 60}m` : '0m';

        const payloadStr = encodeURIComponent(JSON.stringify(f)).replace(/'/g, "%27");

        return `
        <div class="bg-black/20 hover:bg-[#2a2a2b] p-4 rounded-xl border border-white/5 relative hover:border-sky-500/30 transition-colors cursor-pointer group flex items-center justify-between" onclick="replayFlightLog('${payloadStr}')">
            
            <!-- Date & Flight -->
            <div class="flex items-center gap-6 w-[30%] shrink-0">
                <div class="w-2 h-2 rounded-full ${colorPill} shrink-0"></div>
                <div>
                    <div class="text-[10px] text-[#7b7b7b] font-label tracking-widest uppercase">${dateStr}</div>
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
                    <span class="text-[#7b7b7b] uppercase tracking-widest text-[9px] font-manrope font-bold mb-1">Block Time</span>
                    <span class="text-[#b6b6b6] font-bold">${blkFormat}</span>
                </div>
                <div class="flex flex-col items-end">
                    <span class="text-[#7b7b7b] uppercase tracking-widest text-[9px] font-manrope font-bold mb-1">Touchdown</span>
                    <span class="text-[#b6b6b6] font-bold">${Math.round(f.TouchdownFpm)} fpm</span>
                </div>
                <div class="flex flex-col items-end w-24">
                    <span class="text-[#7b7b7b] uppercase tracking-widest text-[9px] font-manrope font-bold mb-1">Score</span>
                    <span class="text-${isSuper ? 'emerald' : 'red'}-400 font-bold text-sm">${f.Score} <span class="text-[9px] text-[#7b7b7b]">pts</span></span>
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


