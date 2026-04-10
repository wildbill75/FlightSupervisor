const GO_ICONS = {
    'Refueling': '<span class="material-symbols-outlined text-[18px] text-orange-500">local_gas_station</span>',
    'Boarding': '<span class="material-symbols-outlined text-[18px] text-sky-400">group</span>',
    'Deboarding': '<span class="material-symbols-outlined text-[18px] text-sky-300">directions_run</span>',
    'Cargo': '<span class="material-symbols-outlined text-[18px] text-amber-500">luggage</span>',
    'Cargo/Luggage': '<span class="material-symbols-outlined text-[18px] text-amber-500">luggage</span>',
    'Catering': '<span class="material-symbols-outlined text-[18px] text-pink-400">restaurant</span>',
    'Cleaning': '<span class="material-symbols-outlined text-[18px] text-fuchsia-400">cleaning_services</span>',
    'Cabin Cleaning': '<span class="material-symbols-outlined text-[18px] text-fuchsia-400">cleaning_services</span>',
    'Cabin Clean (PNC)': '<span class="material-symbols-outlined text-[18px] text-indigo-400">dry_cleaning</span>',
    'PNC Chores': '<span class="material-symbols-outlined text-[18px] text-indigo-400">dry_cleaning</span>',
    'Water/Waste': '<span class="material-symbols-outlined text-[18px] text-emerald-400">water_drop</span>'
};

window.chrome.webview.addEventListener('message', function(e) {
    const data = e.data;
    if (data.type === 'groundOps') {
        renderGroundOps(data.services, data.isDispatchSignedOff);
    }
});

// Request initial data upon load
document.addEventListener('DOMContentLoaded', () => {
    window.chrome.webview.postMessage({ action: 'requestGroundOps' });
});

function renderGroundOps(services, isDispatchSignedOff = true) {
    const container = document.getElementById('groundOpsContainer');

    if (!services || services.length === 0) {
        container.innerHTML = '<p class="text-slate-500 font-mono text-center mt-10">Ground operations pending SimBrief initialization...</p>';
        return;
    }

    let html = ``;
    
    if (!isDispatchSignedOff) {
        html += `<div class="absolute top-10 left-0 w-full text-center z-50 pointer-events-none">
                    <span class="bg-black/80 text-orange-400 border border-orange-500/30 px-6 py-2 rounded-full font-bold tracking-[0.2em] shadow-[0_0_20px_rgba(249,115,22,0.3)]">
                        PENDING LOAD SHEET VALIDATION
                    </span>
                 </div>`;
    }

    html += `
    <div class="relative w-full max-w-[1200px] mx-auto min-h-[800px] h-[85vh] mt-4 z-0 ${!isDispatchSignedOff ? 'opacity-30 grayscale pointer-events-none' : ''}">
        <!-- SVG Background image constrained inside the wrapper so nose/tail are not cut off -->
        <img src="A320.svg" class="absolute top-1/2 left-1/2 -translate-x-1/2 -translate-y-1/2 h-full w-auto object-contain opacity-40 pointer-events-none select-none z-0" />
        `;

    const POSITIONS = {
        'Boarding': { left: '0.0%', top: '8.0%' },
        'Deboarding': { left: '0.0%', top: '8.0%' },
        'Water/Waste': { left: 'calc(50% - 160px)', top: '88.0%' },
        'Catering': { left: '70.3%', top: '15.0%' },
        'Cargo': { left: '74.4%', top: '35.0%' },
        'Cargo/Luggage': { left: '74.4%', top: '35.0%' },
        'Refueling': { left: '75.2%', top: '55.0%' },
        'Cleaning': { left: '0.0%', top: '75.0%' },
        'Cabin Cleaning': { left: '0.0%', top: '75.0%' },
        'Cabin Clean (PNC)': { left: '0.0%', top: '75.0%' },
        'PNC Chores': { left: '0.0%', top: '75.0%' }
    };

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
        let locName = s.Name !== undefined ? s.Name : s.name;
        if (locName === "Refueling") locName = "REFUELING";
        else if (locName === "Boarding") locName = "BOARDING";
        else if (locName === "Deboarding") locName = "DEBOARDING";
        else if (locName === "Cargo" || locName === "Cargo/Luggage") {
            let nState = s.State !== undefined ? s.State : s.state;
            if (deboardingSrv) { // Turnaround
                if (nState === 1 && (s.ProgressPercent || 0) < 50) locName = "CARGO UNLOADING";
                else if (nState === 1) locName = "CARGO LOADING";
                else locName = "CARGO LOAD/UNLOAD";
            } else { // Pristine
                locName = "CARGO LOADING";
            }
        }
        else if (locName === "Catering") locName = "CATERING";
        else if (locName === "Cleaning" || locName === "Cabin Cleaning") locName = "AIRPORT CLEANING";
        else if (locName === "Cabin Clean (PNC)" || locName === "PNC Chores") locName = "PNC CLEANING";
        else if (locName === "Water/Waste") locName = "WATER/WASTE";

        let stateVal = s.State !== undefined ? s.State : s.state;
        const icon = GO_ICONS[s.Name] || '🔹';

        let isBlocked = false;
        if ((s.Name === "Deboarding" || s.Name === "Boarding") && isCrewWorking) isBlocked = true;
        if ((s.Name.includes("Clean") || s.Name === "Catering") && isPaxMoving) isBlocked = true;

        let isCompleted = stateVal === 3 || s.IsPreServiced || s.isPreServiced;
        let isSkipped = stateVal === 4;

        let buttonText = `START ${locName.toUpperCase()}`;
        let buttonStyles = '';
        let buttonClass = '';
        let isClickable = false;
        let actionName = s.Name === 'Deboarding' ? 'startDeboarding' : 'startService';

        if (stateVal === 0 || stateVal === 5) {
            if (isBlocked) {
                buttonStyles = 'color: #64748b; opacity: 0.4; cursor: default; pointer-events: none;';
                buttonText = `LOCKED (${isPaxMoving ? 'PAX' : 'SVC'})`;
            } else {
                buttonStyles = 'color: #38bdf8; cursor: pointer;';
                isClickable = true;
            }
        } else if (stateVal === 1 || stateVal === 2) {
            buttonText = locName.toUpperCase();
            if (stateVal === 2) {
                buttonStyles = 'color: #f97316; cursor: default; pointer-events: none;';
                buttonClass += ' opacity-80';
            } else {
                buttonStyles = 'color: #34d399; cursor: default; pointer-events: none;';
                buttonClass += ' bg-emerald-500/5';
            }
        } else if (isCompleted || isSkipped) {
            buttonStyles = 'color: #64748b; opacity: 0.5; cursor: default; pointer-events: none;';
        }

        let timeDisplay = '';
        let remainingSec = s.RemainingSec !== undefined ? s.RemainingSec : s.remainingSec;
        if (remainingSec > 0 && stateVal !== 3 && stateVal !== 4) {
            const m = Math.floor(remainingSec / 60).toString().padStart(2, '0');
            const sec = (remainingSec % 60).toString().padStart(2, '0');
            timeDisplay = `<span class="text-xl font-mono font-black tracking-wider ml-auto drop-shadow-[0_0_10px_currentColor] min-w-[70px] text-right" style="color: inherit;">${m}:${sec}</span>`;
        }

        let smMsg = s.StatusMessage !== undefined ? s.StatusMessage : s.statusMessage;
        let inlineStateHtml = '';
        if (smMsg && smMsg.toLowerCase().includes("blocked")) {
            inlineStateHtml = `<span class="text-red-400 font-bold uppercase tracking-widest text-[10px] bg-black/40 px-3 py-1 rounded-full border border-red-500/20"><span class="material-symbols-outlined text-[12px] align-middle">block</span> ${smMsg.toUpperCase()}</span>`;
        }
        else if (stateVal === 1) inlineStateHtml = `<span class="text-sky-400 font-bold uppercase tracking-widest text-[10px] bg-black/40 px-3 py-1 rounded-full border border-sky-500/20"><span class="animate-pulse">●</span> ${smMsg ? smMsg.toUpperCase() : 'IN PROGRESS'}</span>`;
        else if (stateVal === 2) inlineStateHtml = `<span class="text-orange-400 font-bold uppercase tracking-widest text-[10px] bg-black/40 px-3 py-1 rounded-full border border-orange-500/20">${smMsg ? smMsg.toUpperCase() : 'WAITING (DELAYED)'}</span>`;
        else if (stateVal === 5) inlineStateHtml = `<span class="text-yellow-400 font-bold uppercase tracking-widest text-[10px] bg-black/40 px-3 py-1 rounded-full border border-yellow-500/20"><span class="animate-pulse">●</span> ${smMsg ? smMsg.toUpperCase() : 'WAITING FOR DRIVER'}</span>`;
        if (isCompleted) inlineStateHtml = `<span class="text-slate-400 font-bold uppercase tracking-widest text-[10px] bg-black/40 px-3 py-1 rounded-full border border-white/5"><span class="material-symbols-outlined text-[10px]">check</span> ${smMsg && !smMsg.toLowerCase().includes("completed") && !smMsg.toLowerCase().includes("termin") ? smMsg.toUpperCase() : 'COMPLETED'}</span>`;
        else if (isSkipped) inlineStateHtml = `<span class="text-slate-500 font-bold uppercase tracking-widest text-[10px] bg-black/40 px-3 py-1 rounded-full border border-white/5"><span class="material-symbols-outlined text-[10px]">close</span> SKIPPED</span>`;

        let extraBadgesHtml = '';
        if (s.Name === "Catering" || s.Name === "Cleanliness" || s.Name === "Cleaning" || s.Name === "Cabin Clean (PNC)" || s.Name === "Water/Waste") {
            if (!isCompleted && !isSkipped) {
                extraBadgesHtml += `<button onclick="event.stopPropagation(); window.chrome.webview.postMessage({ action: 'skipService', service: '${(s.Name || s.name)}' });" class="px-2 py-1 ml-2 rounded text-[#7b7b7b] text-[9px] uppercase font-bold tracking-widest leading-none relative z-10 cursor-pointer">SKIP</button>`;
            }
        }
        // Telemetry badges (might need to fetch lastTelemetry from window or C#)
        // For now, let's keep it consistent.

        let clickAction = isClickable ? `onclick="window.chrome.webview.postMessage({action: '${actionName}', service: '${(s.Name || s.name)}'})"` : '';

        let barColor = stateVal === 3 ? '#34D399' : (stateVal === 2 ? '#FB923C' : '#4A90E2');
        if (isCompleted && !(s.IsPreServiced || s.isPreServiced)) barColor = '#34D399';
        else if (isCompleted && (s.IsPreServiced || s.isPreServiced)) barColor = '#475569';

        let pos = POSITIONS[s.Name] || { left: '50%', top: '50%' };
        // Fallbacks if not recognized
        if (!POSITIONS[s.Name]) {
            pos = { left: '0%', top: '0%', position: 'relative' }; // Flow normally if not mapped
        }

        html += `
            <div class="${POSITIONS[s.Name] ? 'absolute w-[320px] transition-all duration-500' : 'relative w-full'} bg-[#1C1F26]/90 backdrop-blur-md rounded-xl border border-white/5 overflow-hidden flex flex-col justify-center min-h-[90px] shadow-[0_4px_20px_rgba(0,0,0,0.5)] z-10" style="${POSITIONS[s.Name] ? `left: ${pos.left}; top: ${pos.top};` : ''}">
                ${(() => {
                    if (s.Name === "Water/Waste") {
                        let waterLvl = s.State === 1 ? s.ProgressPercent : 100;
                        let wasteLvl = s.State === 1 ? s.ProgressPercent : 0;
                        if (isSkipped) { waterLvl = 0; wasteLvl = 0; }
                        return `
                            <div class="absolute bottom-0 left-0 w-full flex flex-col gap-[1px] bg-black/40 h-2">
                                <div class="h-1"><div class="h-full transition-all duration-1000 bg-sky-500" style="width: ${waterLvl}%"></div></div>
                                <div class="h-1"><div class="h-full transition-all duration-1000 bg-emerald-500" style="width: ${wasteLvl}%"></div></div>
                            </div>`;
                    } else {
                        let displayProgress = isSkipped ? 0 : s.ProgressPercent;
                        return `<div class="absolute bottom-0 left-0 w-full h-[3px] bg-black/40">
                                    <div class="h-full transition-all duration-1000 ease-out" style="width: ${displayProgress}%; background-color: ${barColor}; opacity: 0.8"></div>
                                </div>`;
                    }
                })()}
                <button ${clickAction} class="w-full h-full p-4 flex flex-col justify-center items-start gap-1.5 outline-none border-none relative ${buttonClass}" style="${buttonStyles}">
                    <div class="flex flex-row items-center justify-between w-full h-[24px]">
                        <div class="flex flex-row items-center gap-3 overflow-visible">
                            <span class="text-xl leading-none w-6 text-center shrink-0">${icon}</span>
                            <strong class="font-headline tracking-widest uppercase text-[12px] whitespace-nowrap text-left leading-none">${buttonText}</strong>
                        </div>
                        <div class="flex flex-row justify-end shrink-0 pl-2">
                            ${timeDisplay}
                        </div>
                    </div>
                    <div class="flex flex-row w-full mt-1">
                        <div class="w-[36px] shrink-0"></div> <!-- spacer offset icon -->
                        <div class="flex-1 flex justify-start">
                            ${inlineStateHtml}
                        </div>
                    </div>
                </button>
                ${extraBadgesHtml ? `<div class="absolute bottom-3 right-3 flex items-center z-20">${extraBadgesHtml}</div>` : ''}
            </div>
        `;
    });

    html += '</div>';
    container.innerHTML = html;
}
