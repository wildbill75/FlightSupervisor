window.manifest = null;

let zoom = 1.0;
let offsetX = 0;
let offsetY = 0;
let isDragging = false;
let startX = 0;
let startY = 0;

if (window.chrome && window.chrome.webview) {
    window.chrome.webview.addEventListener('message', event => {
        const payload = event.data;
        if (payload.action === 'manifestUpdate' || payload.type === 'manifestUpdate') {
            if (payload.manifest) {
                window.manifest = payload.manifest;
                window.renderManifest(payload.manifest);
            }
        }
        else if (payload.type === 'telemetry') {
            // Update passenger boarding state dynamically
            if (payload.passengers && Array.isArray(payload.passengers) && window.manifest) {
                const manifestPax = window.manifest.Passengers || window.manifest.passengers;
                if (manifestPax) {
                    manifestPax.forEach(p => {
                        const state = payload.passengers.find(s => (s.seat || s.Seat) === p.Seat);
                        if (state) {
                            p.IsBoarded = (state.IsBoarded !== undefined) ? state.IsBoarded : state.isBoarded;
                            p.IsSeatbeltFastened = (state.IsSeatbeltFastened !== undefined) ? state.IsSeatbeltFastened : state.isSeatbeltFastened;
                            p.IsInjured = (state.IsInjured !== undefined) ? state.IsInjured : state.isInjured;
                        }
                    });
                    window.renderManifest(window.manifest);
                }
            }
        }
    });
    window.chrome.webview.postMessage({ action: 'requestManifest' });
}

window.renderManifest = function (manifest) {
    const container = document.getElementById('manifestContainer');
    if (!container) return;

    let isSkeleton = false;
    if (!manifest || (!(manifest.FlightCrew || manifest.flightCrew) && !(manifest.Passengers || manifest.passengers))) {
        isSkeleton = true;
        manifest = {
            FlightCrew: [
                { Role: "Captain", Name: "Awaiting Dispatch..." },
                { Role: "First Officer", Name: "Awaiting Dispatch..." },
                { Role: "Purser", Name: "TBD" },
                { Role: "Flight Attendant", Name: "TBD" }
            ],
            Passengers: Array.from({ length: 180 }, (_, i) => ({
                Seat: (Math.floor(i / 6) + 1) + String.fromCharCode(65 + (i % 6)),
                Name: "AWAITING DATA",
                IsBoarded: false
            }))
        };
    }

    const flightCrew = manifest?.FlightCrew || manifest?.flightCrew || [];
    const passengers = manifest?.Passengers || manifest?.passengers || [];

    // Filter Crew
    const pilots = flightCrew.filter(c => c.Role.toLowerCase().includes('captain') || c.Role.toLowerCase().includes('officer'));
    const pnc = flightCrew.filter(c => !c.Role.toLowerCase().includes('captain') && !c.Role.toLowerCase().includes('officer'));

    // IN-PLACE UPDATE LOGIC
    const existingMap = document.getElementById('seatMapZoomContainer');
    const expectedPaxCount = passengers.length;
    const currentContainerCount = container.dataset.flightPaxCount ? parseInt(container.dataset.flightPaxCount) : -1;

    if (existingMap && expectedPaxCount === currentContainerCount && !isSkeleton) {
        updatePaxData(passengers, container);
        return;
    }

    // FULL RENDER LOGIC
    container.dataset.flightPaxCount = expectedPaxCount;
    let maxRow = 0;
    let hasLettersGHK = false;
    passengers.forEach(p => {
        let rowStr = p.Seat.replace(/[^0-9]/g, '');
        let row = parseInt(rowStr);
        if (row > maxRow) maxRow = row;
        if (p.Seat.includes('G') || p.Seat.includes('H') || p.Seat.includes('K')) hasLettersGHK = true;
    });
    if (maxRow === 0) maxRow = 30;

    let lettersLeft = ['A', 'B', 'C'];
    let lettersRight = ['D', 'E', 'F'];
    let lettersCenter = [];
    if (hasLettersGHK) {
        lettersLeft = ['A', 'B', 'C'];
        lettersCenter = ['D', 'E', 'F', 'G'];
        lettersRight = ['H', 'J', 'K'];
    }

    container.innerHTML = `
    <div class="grid grid-cols-12 gap-6 h-full overflow-hidden">
        
        <!-- SIDEBAR (LEFT) -->
        <div class="col-span-4 flex flex-col gap-6 h-full overflow-hidden">
            <!-- Combined Crew Box -->
            <div class="bg-white/[0.03] rounded-2xl border border-white/5 p-6 overflow-y-auto custom-scrollbar shrink-0">
                <div class="text-[9px] font-bold uppercase tracking-[0.3em] text-[#64748b] mb-6 flex items-center gap-2">
                    <span class="material-symbols-outlined text-[14px]">groups</span>
                    Flight & Cabin Crew
                </div>
                
                <div class="grid grid-cols-2 gap-x-4 gap-y-6">
                    <div class="flex flex-col gap-4">
                        <div class="text-[8px] font-black uppercase text-sky-500/50 tracking-widest border-b border-white/5 pb-1 mb-1">Flight Deck</div>
                        ${pilots.map(c => `
                            <div class="flex flex-col">
                                <span class="text-[10px] font-bold text-[#b6b6b6] uppercase tracking-widest">${c.Role || 'Pilot'}</span>
                                <span class="text-[11px] font-medium text-white/80">${c.Name || '---'}</span>
                            </div>
                        `).join('')}
                    </div>
                    <div class="flex flex-col gap-4">
                        <div class="text-[8px] font-black uppercase text-[#94a3b8] tracking-widest border-b border-white/5 pb-1 mb-1">Cabin Crew</div>
                        ${pnc.map(c => `
                            <div class="flex flex-col">
                                <span class="text-[10px] font-bold text-[#b6b6b6] uppercase tracking-widest">${c.Role || 'PNC'}</span>
                                <span class="text-[11px] font-medium text-white/80 truncate">${c.Name || '---'}</span>
                            </div>
                        `).join('')}
                    </div>
                </div>
            </div>

            <!-- List Block -->
            <div class="flex-1 flex flex-col overflow-hidden bg-white/[0.03] rounded-2xl border border-white/5 p-6">
                <div id="paxListHeader" class="text-[9px] font-bold uppercase tracking-[0.3em] text-[#64748b] mb-4">Pax Manifest (0 / ${expectedPaxCount})</div>
                <div class="flex-1 overflow-y-auto custom-scrollbar pr-2">
                    <table class="w-full text-left border-separate border-spacing-y-1">
                        <thead class="sticky top-0 bg-[#141414] z-10">
                            <tr class="text-[9px] uppercase text-[#475569] font-bold tracking-widest">
                                <th class="pb-2 px-2">Seat</th>
                                <th class="pb-2 px-4">Name</th>
                                <th class="pb-2 px-2 text-right">Status</th>
                            </tr>
                        </thead>
                        <tbody id="paxListBody">
                            ${passengers.map(p => `
                                <tr class="group hover:bg-white/[0.05] transition-colors rounded">
                                    <td class="py-1.5 px-2 font-black text-[#94a3b8] w-12 text-xs">${p.Seat}</td>
                                    <td class="py-1.5 px-4 text-white/60 truncate max-w-[160px] text-xs font-medium">${p.Name}</td>
                                    <td class="py-1.5 px-2 text-right w-12">
                                        <div id="pax-dot-${p.Seat}" class="w-1.5 h-1.5 rounded-full ${p.IsBoarded ? 'bg-[#94a3b8]' : 'bg-white/5'} ml-auto shadow-[0_0_5px_rgba(255,255,255,0.1)]"></div>
                                    </td>
                                </tr>
                            `).join('')}
                        </tbody>
                    </table>
                </div>
            </div>
        </div>

        <!-- MAIN MAP AREA (RIGHT) -->
        <div class="col-span-8 flex flex-col h-full bg-white/[0.03] rounded-2xl border border-white/5 overflow-hidden p-6 relative">
            
            <!-- Map Header -->
            <div class="flex items-center justify-between mb-4 pb-4 border-b border-white/5 shrink-0">
                <div class="text-[9px] font-bold uppercase tracking-[0.3em] text-[#64748b]">Cabin Visualization</div>
                <div class="flex gap-5">
                    <div class="flex items-center gap-2">
                         <div class="w-2 h-2 rounded-full bg-[#0ea5e9]"></div>
                         <span class="text-[9px] uppercase font-bold text-[#64748b]"><span id="legFastenedVal" class="text-white/80">0</span> FASTENED</span>
                    </div>
                    <div class="flex items-center gap-2">
                         <div class="w-2 h-2 rounded-full bg-[#f97316]"></div>
                         <span class="text-[9px] uppercase font-bold text-[#64748b]"><span id="legUnfastenedVal" class="text-white/80">0</span> UNFASTENED</span>
                    </div>
                    <div class="flex items-center gap-2">
                         <div class="w-2 h-2 rounded-full bg-[#475569]"></div>
                         <span class="text-[9px] uppercase font-bold text-[#64748b]"><span id="legEmptyVal" class="text-white/80">${expectedPaxCount}</span> EMPTY</span>
                    </div>
                    <div class="flex items-center gap-2 border-l border-white/10 pl-5">
                         <div class="w-2 h-2 rounded-full bg-red-500 animate-pulse"></div>
                         <span class="text-[9px] uppercase font-bold text-[#64748b]"><span id="legInjuredVal" class="text-white/80">0</span> INJURED</span>
                    </div>
                </div>
            </div>

            <!-- Seat Map Fixed Container -->
            <div class="flex-1 relative bg-black/10 rounded-xl overflow-hidden cursor-grab active:cursor-grabbing" id="seatMapWrapper">
                <div id="seatMapZoomContainer" class="flex items-center justify-center min-h-full transition-transform duration-75 ease-out">
                    <div class="fuselage">
                        <div class="cockpit"></div>
                        ${Array.from({ length: maxRow }, (_, i) => {
                            const row = i + 1;
                            return `
                            <div class="seat-row">
                                <div class="seat-block left">
                                    ${lettersLeft.map(l => `<div id="seat-${row}${l}" class="seat"></div>`).join('')}
                                </div>
                                <div class="row-num">${row}</div>
                                <div class="seat-block right">
                                    ${lettersRight.map(l => `<div id="seat-${row}${l}" class="seat"></div>`).join('')}
                                </div>
                            </div>`;
                        }).join('')}
                    </div>
                </div>

                <!-- Manual Controls Overlay -->
                <div class="absolute bottom-6 right-6 flex flex-col gap-2 no-drag">
                    <button onclick="changeZoom(0.1)" class="w-9 h-9 rounded-lg bg-[#1C1F26] border border-white/10 flex items-center justify-center hover:bg-white/10 text-[#64748b] shadow-xl"><span class="material-symbols-outlined text-lg">add</span></button>
                    <button onclick="changeZoom(-0.1)" class="w-9 h-9 rounded-lg bg-[#1C1F26] border border-white/10 flex items-center justify-center hover:bg-white/10 text-[#64748b] shadow-xl"><span class="material-symbols-outlined text-lg">remove</span></button>
                    <button onclick="resetZoom()" class="w-9 h-9 rounded-lg bg-[#1C1F26] border border-white/10 flex items-center justify-center hover:bg-white/10 text-[#64748b] shadow-xl"><span class="material-symbols-outlined text-lg">restart_alt</span></button>
                </div>
            </div>
        </div>
    </div>`;

    // Init zoom
    initZoomPan();
    updatePaxData(passengers, container);
};

function initZoomPan() {
    const wrapper = document.getElementById('seatMapWrapper');
    const container = document.getElementById('seatMapZoomContainer');
    if (!wrapper || !container) return;

    // Set initial Zoom for the screenshot-like fit
    zoom = 0.72;
    offsetX = 0;
    offsetY = 0;
    applyTransform();

    wrapper.addEventListener('wheel', e => {
        e.preventDefault();
        const delta = e.deltaY > 0 ? -0.05 : 0.05;
        changeZoom(delta);
    }, { passive: false });

    wrapper.addEventListener('mousedown', e => {
        if (e.target.closest('button')) return;
        isDragging = true;
        startX = e.clientX - offsetX;
        startY = e.clientY - offsetY;
    });

    window.addEventListener('mousemove', e => {
        if (!isDragging) return;
        offsetX = e.clientX - startX;
        offsetY = e.clientY - startY;
        applyTransform();
    });

    window.addEventListener('mouseup', () => {
        isDragging = false;
    });
}

window.changeZoom = function(delta) {
    zoom = Math.max(0.3, Math.min(3, zoom + delta));
    applyTransform();
}

window.resetZoom = function() {
    zoom = 0.72;
    offsetX = 0;
    offsetY = 0;
    applyTransform();
}

function applyTransform() {
    const container = document.getElementById('seatMapZoomContainer');
    if (container) {
        container.style.transform = `translate(${offsetX}px, ${offsetY}px) scale(${zoom})`;
    }
}

function updatePaxData(passengers, container) {
    let boardedCount = 0;
    let fastenedCount = 0;
    let injuredCount = 0;

    passengers.forEach(p => {
        if (p.IsBoarded) {
            boardedCount++;
            if (p.IsSeatbeltFastened) fastenedCount++;
            if (p.IsInjured) injuredCount++;
        }
        
        const seatEl = document.getElementById('seat-' + p.Seat);
        if (seatEl) {
            if (p.IsBoarded) {
                const seatClass = p.IsSeatbeltFastened ? 'fastened' : 'unfastened';
                seatEl.className = `seat ${seatClass} relative`;
                const injuryHtml = p.IsInjured ? '<span class="material-symbols-outlined text-[10px] text-red-500 absolute -top-1 -left-1 animate-pulse" style="z-index:10;">medical_services</span>' : '';
                if (!seatEl.dataset.initialized || p.IsInjured) {
                    seatEl.innerHTML = `${injuryHtml}<span class="tooltip">${p.Seat} : ${p.Name}</span>`;
                    seatEl.dataset.initialized = 'true';
                }
            } else {
                seatEl.className = 'seat';
                seatEl.innerHTML = '';
                seatEl.dataset.initialized = '';
            }
        }

        const dotEl = document.getElementById('pax-dot-' + p.Seat);
        if (dotEl) {
            dotEl.className = `w-1.5 h-1.5 rounded-full ${p.IsBoarded ? 'bg-[#94a3b8]' : 'bg-white/5'} ml-auto shadow-[0_0_5px_rgba(255,255,255,0.1)]`;
        }
    });

    const headerLabel = document.getElementById('paxListHeader');
    if (headerLabel) headerLabel.innerText = `Pax Manifest (${boardedCount} / ${passengers.length})`;
    
    const elFast = document.getElementById('legFastenedVal');
    if (elFast) elFast.innerText = fastenedCount;
    const elUnfast = document.getElementById('legUnfastenedVal');
    if (elUnfast) elUnfast.innerText = boardedCount - fastenedCount;
    const elEmpty = document.getElementById('legEmptyVal');
    if (elEmpty) elEmpty.innerText = passengers.length - boardedCount;
    const elInj = document.getElementById('legInjuredVal');
    if (elInj) elInj.innerText = injuredCount;
}
