const fs = require('fs');

let indexHtml = fs.readFileSync('wwwroot/index.html', 'utf8');
let appJs = fs.readFileSync('wwwroot/app.js', 'utf8');

// 1. ADD AIRLINE IDENTITY MODAL TO index.html
const modalHtml = `
    <!-- AIRLINE IDENTITY MODAL -->
    <div id="airlineIdentityModal" class="fixed inset-0 z-[100] flex items-center justify-center opacity-0 pointer-events-none transition-opacity duration-300 backdrop-blur-sm bg-black/40">
        <!-- Draggable Container -->
        <div class="bg-[#1C1F26]/95 border border-white/10 rounded-2xl shadow-[0_0_50px_rgba(0,0,0,0.8)] w-[450px] flex flex-col overflow-hidden transform scale-95 transition-transform duration-300" id="airlineIdentityBox">
            
            <!-- Draggable Header / Logo Banner -->
            <div class="h-32 w-full bg-slate-900 border-b border-white/10 relative flex items-center justify-center cursor-move draggable-handle relative group">
                <img src="assets/airlines_logos/EZY.png" alt="Airline Logo" class="h-24 object-contain brightness-110 drop-shadow-[0_0_15px_rgba(255,255,255,0.2)]" />
                <button onclick="document.getElementById('airlineIdentityModal').classList.add('opacity-0', 'pointer-events-none'); document.getElementById('airlineIdentityBox').classList.replace('scale-100', 'scale-95');" class="absolute top-4 right-4 w-8 h-8 rounded-full bg-black/50 hover:bg-white/10 flex items-center justify-center text-white/50 hover:text-white transition-colors">
                    <span class="material-symbols-outlined text-[18px]">close</span>
                </button>
            </div>

            <!-- Content Area -->
            <div class="p-6 flex flex-col gap-8">
                
                <!-- COMMERCIAL METRICS SECTION -->
                <div class="flex flex-col gap-4">
                    <div class="flex items-center justify-between border-b border-white/5 pb-2">
                        <span class="text-[12px] uppercase tracking-[0.3em] font-bold text-emerald-400 font-headline">Commercial</span>
                        <span class="material-symbols-outlined text-emerald-500/50 text-[18px]">corporate_fare</span>
                    </div>

                    <div class="grid grid-cols-2 gap-x-6 gap-y-4">
                        <!-- Pax Satisfaction -->
                        <div class="flex flex-col">
                            <span class="text-[9px] uppercase tracking-widest text-[#7b7b7b] font-bold mb-1">Pax Contentment <span class="text-white/30 text-[8px]">(Target)</span></span>
                            <div class="w-full bg-black/50 h-1.5 rounded-full overflow-hidden mt-1 ring-1 ring-white/5">
                                <div class="h-full bg-emerald-400 w-[60%] shadow-[0_0_10px_rgba(52,211,153,0.5)]"></div>
                            </div>
                            <span class="text-xs font-mono font-bold text-white mt-1">60%</span>
                        </div>
                        <!-- Hard Product -->
                        <div class="flex flex-col">
                            <span class="text-[9px] uppercase tracking-widest text-[#7b7b7b] font-bold mb-1">Hard Product</span>
                            <div class="w-full bg-black/50 h-1.5 rounded-full overflow-hidden mt-1 ring-1 ring-white/5">
                                <div class="h-full bg-sky-400 w-[40%] shadow-[0_0_10px_rgba(56,189,248,0.5)]"></div>
                            </div>
                            <span class="text-xs font-mono font-bold text-white mt-1">4.0 <span class="text-[9px] text-[#7b7b7b]">/10</span></span>
                        </div>
                        <!-- Soft Product -->
                        <div class="flex flex-col">
                            <span class="text-[9px] uppercase tracking-widest text-[#7b7b7b] font-bold mb-1">Soft Product</span>
                            <div class="w-full bg-black/50 h-1.5 rounded-full overflow-hidden mt-1 ring-1 ring-white/5">
                                <div class="h-full bg-sky-400 w-[60%] shadow-[0_0_10px_rgba(56,189,248,0.5)]"></div>
                            </div>
                            <span class="text-xs font-mono font-bold text-white mt-1">6.0 <span class="text-[9px] text-[#7b7b7b]">/10</span></span>
                        </div>
                        <!-- PNC Morale Base -->
                        <div class="flex flex-col">
                            <span class="text-[9px] uppercase tracking-widest text-[#7b7b7b] font-bold mb-1">Crew Friendliness</span>
                            <div class="w-full bg-black/50 h-1.5 rounded-full overflow-hidden mt-1 ring-1 ring-white/5">
                                <div class="h-full bg-amber-400 w-[70%] shadow-[0_0_10px_rgba(251,191,36,0.5)]"></div>
                            </div>
                            <span class="text-xs font-mono font-bold text-white mt-1">7.0 <span class="text-[9px] text-[#7b7b7b]">/10</span></span>
                        </div>
                    </div>
                </div>

                <!-- OPERATIONS SECTION -->
                <div class="flex flex-col gap-4">
                    <div class="flex items-center justify-between border-b border-white/5 pb-2">
                        <span class="text-[12px] uppercase tracking-[0.3em] font-bold text-orange-400 font-headline">Operations</span>
                        <span class="material-symbols-outlined text-orange-500/50 text-[18px]">rule</span>
                    </div>

                    <div class="grid grid-cols-2 gap-4">
                        <div class="bg-black/30 p-3 rounded-xl border border-white/5">
                            <span class="bg-indigo-500/20 text-indigo-300 text-[8px] font-bold uppercase tracking-widest px-2 py-0.5 rounded block w-fit mb-2">Framework</span>
                            <span class="text-sm font-bold text-white tracking-widest uppercase">EASA Ops</span>
                        </div>
                        <div class="bg-black/30 p-3 rounded-xl border border-white/5">
                            <span class="bg-emerald-500/20 text-emerald-300 text-[8px] font-bold uppercase tracking-widest px-2 py-0.5 rounded block w-fit mb-2">Cost Index</span>
                            <span class="text-xl font-black font-headline text-white tracking-widest block">12 <span class="text-[9px] text-[#7b7b7b] uppercase tracking-normal">Optimal</span></span>
                        </div>
                        <div class="bg-black/30 p-3 rounded-xl border border-white/5">
                            <span class="bg-sky-500/20 text-sky-300 text-[8px] font-bold uppercase tracking-widest px-2 py-0.5 rounded block w-fit mb-2">Fuel Logic</span>
                            <span class="text-xs font-bold text-white tracking-widest uppercase block mt-1 leading-tight">SFP Mode<br/><span class="text-[9px] text-[#7b7b7b]">Statistical Cont.</span></span>
                        </div>
                        <div class="bg-black/30 p-3 rounded-xl border border-white/5">
                            <span class="bg-rose-500/20 text-rose-300 text-[8px] font-bold uppercase tracking-widest px-2 py-0.5 rounded block w-fit mb-2">Contingency</span>
                            <span class="text-sm font-bold text-white tracking-widest uppercase">5% Default</span>
                        </div>
                    </div>
                </div>
                
            </div>
        </div>
    </div>
    <script>
        window.showAirlineIdentityModal = function(icao) {
            // For now, it's globally EasyJet just for demo, we'll populate dynamicaly soon
            const modal = document.getElementById('airlineIdentityModal');
            const box = document.getElementById('airlineIdentityBox');
            modal.classList.remove('opacity-0', 'pointer-events-none');
            box.classList.replace('scale-95', 'scale-100');
        };
        // Very basic dragging logic for modals
        document.querySelectorAll('.draggable-handle').forEach(handle => {
            let isDragging = false, startX, startY, initialX, initialY;
            const target = handle.parentElement;
            handle.addEventListener('mousedown', (e) => {
                if(e.target.tagName.toLowerCase() === 'button' || e.target.closest('button')) return;
                isDragging = true;
                startX = e.clientX; startY = e.clientY;
                const rect = target.getBoundingClientRect();
                initialX = rect.left; initialY = rect.top;
                target.style.position = 'absolute';
                target.style.left = initialX + 'px';
                target.style.top = initialY + 'px';
                target.style.margin = '0';
                target.style.transform = 'none';
            });
            document.addEventListener('mousemove', (e) => {
                if(!isDragging) return;
                target.style.left = (initialX + (e.clientX - startX)) + 'px';
                target.style.top = (initialY + (e.clientY - startY)) + 'px';
            });
            document.addEventListener('mouseup', () => isDragging = false);
        });
    </script>
`;

if (!indexHtml.includes('airlineIdentityModal')) {
    indexHtml = indexHtml.replace('</body>', modalHtml + '\n</body>');
}

// Update the Empty dashboard placeholder
indexHtml = indexHtml.replace(
    /<div class="text-\[20px\] font-black text-white tracking-widest mt-1 uppercase">---<\/div>/,
    `<div class="text-[20px] font-black text-emerald-400 tracking-widest mt-1 uppercase hover:text-white transition-colors cursor-pointer" onclick="window.showAirlineIdentityModal('---')">---</div>`
);

fs.writeFileSync('wwwroot/index.html', indexHtml);

// 2. REMOVE COMPANY DEMANDS FROM app.js AND UPDATE ROUTE HTML TO INCLUDE AIRLINE
const companyPolicyStart = appJs.indexOf('<div class="bg-[#1C1F26]/80 p-4 rounded-xl border border-white/5 shadow-md flex justify-between items-center">');
if (companyPolicyStart !== -1 && appJs.indexOf('<span class="text-[10px] text-amber-500/80 font-bold tracking-widest uppercase leading-none">Company Policy</span>', companyPolicyStart) !== -1) {
    const gridStart = appJs.lastIndexOf('<div class="grid grid-cols-2 gap-4 mt-3">', companyPolicyStart);
    if (gridStart !== -1) {
        // Find the end of this div manually
        const endStr = '</div>\n            </div>`;';
        const gridEnd = appJs.indexOf(endStr, gridStart);
        if (gridEnd !== -1) {
            appJs = appJs.substring(0, gridStart) + '`;' + appJs.substring(gridEnd + endStr.length);
        }
    }
}

// 3. EDIT BRIEFING TITLE BAR TO ADD AIRLINE (app.js)
const routeHtmlStart = appJs.indexOf('<div class="grid grid-cols-5 items-center');
if (routeHtmlStart !== -1) {
    appJs = appJs.replace(
        '<div class="grid grid-cols-5 items-center w-full bg-[#1C1F26]/80 p-5 rounded-xl border border-white/5 shadow-md divide-x divide-white/5">',
        '<div class="grid grid-cols-6 items-center w-full bg-[#1C1F26]/80 p-5 rounded-xl border border-white/5 shadow-md divide-x divide-white/5">'
    );
    appJs = appJs.replace(
        `                <div class="flex flex-col items-center justify-center">
                    <span class="text-[9px] text-[#7b7b7b] font-bold tracking-widest uppercase mb-1">Flight Number</span>
                    <span class="text-white text-xl font-black tracking-widest font-headline">\${rd.general.icao_airline || ''}\${rd.general.flight_number || ''}</span>
                </div>`,
        `                 <div class="flex flex-col items-center justify-center cursor-pointer group" onclick="if(window.showAirlineIdentityModal) window.showAirlineIdentityModal('\${rd.general.icao_airline||''}')">
                    <span class="text-[9px] text-[#7b7b7b] font-bold tracking-widest uppercase mb-1">Airline</span>
                    <span class="text-emerald-400 group-hover:text-white transition-colors text-xl font-black tracking-widest font-headline">\${rot.airlineProfile ? rot.airlineProfile.name : (rd.general.airline_name || 'EZY')}</span>
                </div>
                <div class="flex flex-col items-center justify-center">
                    <span class="text-[9px] text-[#7b7b7b] font-bold tracking-widest uppercase mb-1">Flight Number</span>
                    <span class="text-white text-xl font-black tracking-widest font-headline">\${rd.general.icao_airline || ''}\${rd.general.flight_number || ''}</span>
                </div>`
    );
}

// 4. EDIT DASHBOARD ACTIVE VIEW (app.js)
appJs = appJs.replace(
    /<div class="text-\[20px\] font-black text-white tracking-widest mt-1 uppercase text-center">\$\{ac\}<\/div>/g,
    `<div class="text-[20px] font-black text-emerald-400 hover:text-white transition-colors cursor-pointer tracking-widest mt-1 uppercase text-center" onclick="if(window.showAirlineIdentityModal) window.showAirlineIdentityModal('\${icao}')">\${ac}</div>`
);

fs.writeFileSync('wwwroot/app.js', appJs);

console.log("Done updating frontend.");
