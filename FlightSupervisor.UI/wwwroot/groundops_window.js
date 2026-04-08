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

window.chrome.webview.addEventListener('message', function(e) {
    const data = e.data;
    if (data.type === 'groundOps') {
        renderGroundOps(data.services);
    }
});

// Request initial data upon load
document.addEventListener('DOMContentLoaded', () => {
    window.chrome.webview.postMessage({ action: 'requestGroundOps' });
});

function renderGroundOps(services) {
    const container = document.getElementById('groundOpsContainer');

    if (!services || services.length === 0) {
        container.innerHTML = '<p class="text-slate-500 font-mono text-center mt-10">Ground operations pending SimBrief initialization...</p>';
        return;
    }

    let html = `
    <div class="grid grid-cols-1 md:grid-cols-2 gap-4">`;

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
        const mDict = window.locales && window.locales[mLang] ? window.locales[mLang] : (window.locales ? window.locales.en : {});

        let locName = s.Name !== undefined ? s.Name : s.name;
        if (locName === "Refueling") locName = mDict.gops_refueling || locName;
        else if (locName === "Boarding") locName = mDict.gops_boarding || locName;
        else if (locName === "Deboarding") locName = mDict.gops_deboarding || locName;
        else if (locName === "Cargo" || locName === "Cargo/Luggage") {
            locName = mDict.gops_cargo || "Cargo";
            let nState = s.State !== undefined ? s.State : s.state;
            if (deboardingSrv) { // Turnaround
                if (nState === 1 && (s.ProgressPercent || 0) < 50) locName = `${locName} (UNLOADING)`;
                else if (nState === 1) locName = `${locName} (LOADING)`;
                else if (nState === 0 || nState === 5) locName = `${locName} (UNLOAD/LOAD)`;
            } else { // Pristine
                if (nState === 0 || nState === 1 || nState === 5) locName = `${locName} (LOADING)`;
            }
        }
        else if (locName === "Catering") locName = mDict.gops_catering || locName;
        else if (locName === "Cleaning") locName = mDict.gops_cleaning || locName;
        else if (locName === "Cabin Clean (PNC)") locName = "Cabin Clean (PNC)";
        else if (locName === "Water/Waste") locName = mDict.gops_water || locName;

        let stateVal = s.State !== undefined ? s.State : s.state;
        const icon = GO_ICONS[s.Name] || '🔹';

        let isBlocked = false;
        if ((s.Name === "Deboarding" || s.Name === "Boarding") && isCrewWorking) isBlocked = true;
        if ((s.Name.includes("Clean") || s.Name === "Catering") && isPaxMoving) isBlocked = true;

        let isCompleted = stateVal === 3 || stateVal === 4 || s.IsPreServiced || s.isPreServiced;

        let buttonText = `START ${locName.toUpperCase()}`;
        let buttonStyles = '';
        let buttonClass = 'transition-all duration-300';
        let isClickable = false;
        let actionName = s.Name === 'Deboarding' ? 'startDeboarding' : 'startService';

        if (stateVal === 0 || stateVal === 5) {
            if (isBlocked) {
                buttonStyles = 'color: #64748b; opacity: 0.4; cursor: default; pointer-events: none;';
                buttonText = `LOCKED (${isPaxMoving ? 'PAX' : 'SVC'})`;
            } else {
                buttonStyles = 'color: #38bdf8; cursor: pointer;';
                buttonClass += ' hover:text-white hover:bg-sky-500/10 hover:border hover:border-sky-500/50';
                isClickable = true;
            }
        } else if (stateVal === 1 || stateVal === 2) {
            buttonText = locName.toUpperCase();
            if (stateVal === 2) {
                buttonStyles = 'color: #f97316; cursor: default; pointer-events: none;';
                buttonClass += ' animate-pulse';
            } else {
                buttonStyles = 'color: #34d399; cursor: default; pointer-events: none;';
                buttonClass += ' shadow-[0_0_15px_rgba(52,211,153,0.2)] bg-emerald-500/5 animate-pulse';
            }
        } else if (isCompleted) {
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
        if (stateVal === 1) inlineStateHtml = `<span class="text-sky-400 font-bold uppercase tracking-widest text-[10px] bg-[#12141a] px-3 py-1 rounded-full border border-sky-500/20 shadow-[0_0_10px_rgba(56,189,248,0.1)]"><span class="animate-pulse">●</span> ${smMsg ? smMsg.toUpperCase() : 'IN PROGRESS'}</span>`;
        else if (stateVal === 2) inlineStateHtml = `<span class="text-orange-400 font-bold uppercase tracking-widest text-[10px] bg-[#12141a] px-3 py-1 rounded-full border border-orange-500/20 shadow-[0_0_10px_rgba(249,115,22,0.1)]">WAITING (DELAYED)</span>`;
        else if (stateVal === 5) inlineStateHtml = `<span class="text-yellow-400 font-bold uppercase tracking-widest text-[10px] bg-[#12141a] px-3 py-1 rounded-full border border-yellow-500/20 shadow-[0_0_10px_rgba(250,204,21,0.1)] animate-pulse">WAITING FOR DRIVER</span>`;
        if (isCompleted) inlineStateHtml = `<span class="text-slate-400 font-bold uppercase tracking-widest text-[10px] bg-black/40 px-3 py-1 rounded-full border border-white/5"><span class="material-symbols-outlined text-[10px]">check</span> COMPLETED</span>`;

        let extraBadgesHtml = '';
        if (s.Name === "Catering" || s.Name === "Cleanliness" || s.Name === "Cleaning" || s.Name === "Cabin Clean (PNC)" || s.Name === "Water/Waste") {
            if (!isCompleted) {
                extraBadgesHtml += `<button onclick="event.stopPropagation(); window.chrome.webview.postMessage({ action: 'skipService', service: '${(s.Name || s.name)}' });" class="px-2 py-1 ml-2 rounded bg-red-500/10 hover:bg-red-500/30 text-red-500 border border-red-500/20 text-[9px] uppercase font-bold tracking-widest leading-none shadow-[0_0_10px_rgba(239,68,68,0.2)] transition-colors relative z-10 cursor-pointer">SKIP</button>`;
            }
        }
        // Telemetry badges (might need to fetch lastTelemetry from window or C#)
        // For now, let's keep it consistent.

        let clickAction = isClickable ? `onclick="window.chrome.webview.postMessage({action: '${actionName}', service: '${(s.Name || s.name)}'})"` : '';

        let barColor = stateVal === 3 ? '#34D399' : (stateVal === 2 ? '#FB923C' : '#4A90E2');
        if (isCompleted && !(s.IsPreServiced || s.isPreServiced)) barColor = '#34D399';
        else if (isCompleted && (s.IsPreServiced || s.isPreServiced)) barColor = '#475569';

        html += `
            <div class="bg-[#12141A] rounded-xl border border-white/5 overflow-hidden flex flex-col justify-center h-full min-h-[90px] relative">
                ${(() => {
                    if (s.Name === "Water/Waste") {
                        let waterLvl = s.State === 1 ? s.ProgressPercent : 100;
                        let wasteLvl = s.State === 1 ? s.ProgressPercent : 0;
                        return `
                            <div class="absolute bottom-0 left-0 w-full flex flex-col gap-[1px] bg-black/40 h-2">
                                <div class="h-1"><div class="h-full transition-all duration-1000 bg-sky-500" style="width: ${waterLvl}%"></div></div>
                                <div class="h-1"><div class="h-full transition-all duration-1000 bg-emerald-500" style="width: ${wasteLvl}%"></div></div>
                            </div>`;
                    } else {
                        return `<div class="absolute bottom-0 left-0 w-full h-[3px] bg-black/40">
                                    <div class="h-full transition-all duration-1000 ease-out" style="width: ${s.ProgressPercent}%; background-color: ${barColor}; opacity: 0.8"></div>
                                </div>`;
                    }
                })()}
                <button ${clickAction} class="w-full h-full p-5 flex items-center outline-none border-none relative ${buttonClass}" style="${buttonStyles}">
                    <div class="flex items-center gap-4 w-[35%] overflow-hidden text-left">
                        <span class="text-2xl shrink-0">${icon}</span>
                        <strong class="font-headline tracking-widest uppercase text-[11px] md:text-[13px] whitespace-normal leading-tight break-words text-left" title="${buttonText}">${buttonText}</strong>
                    </div>
                    <div class="flex-1 flex justify-center min-w-[120px] shrink-0">
                        ${inlineStateHtml}
                    </div>
                    <div class="w-[35%] flex justify-end">
                        ${timeDisplay}
                    </div>
                </button>
                ${extraBadgesHtml ? `<div class="absolute top-2 right-2 flex items-center">${extraBadgesHtml}</div>` : ''}
            </div>
        `;
    });

    html += '</div>';
    container.innerHTML = html;
}
