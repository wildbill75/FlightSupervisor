window.parseBriefing = (data, rd) => {
                    if (!data) return '';
                    if (typeof data === 'string') return "<i>Updating format...</i>";

                    let html = '';
                    if (data.HeaderText) {
                        html += `
                        <div class="mb-6 relative">
                            <button onclick="document.getElementById('rawOpText_${rd?.general?.flight_number || '0'}').classList.toggle('hidden')" class="text-[10px] uppercase font-bold tracking-widest text-slate-500 hover:text-sky-400 transition-colors flex items-center gap-1 mb-2">
                                <span class="material-symbols-outlined text-[14px]">visibility</span> Toggle Dispatch Narrative
                            </button>
                            <div id="rawOpText_${rd?.general?.flight_number || '0'}" class="hidden italic text-slate-300 font-serif text-sm p-4 bg-black/40 rounded border border-white/5 leading-relaxed">
                                ${data.HeaderText}
                            </div>
                        </div>`;
                    }
                    html += '<div class="grid grid-cols-1 lg:grid-cols-2 gap-6">';

                    if (data.Stations && Array.isArray(data.Stations)) {
                        data.Stations.forEach(station => {
                            let icon = "location_on";
                            if (station.Id.toLowerCase() === "origin") icon = "flight_takeoff";
                            else if (station.Id.toLowerCase() === "destination") icon = "flight_land";
                            else if (station.Id.toLowerCase() === "alternate") icon = "alt_route";

                            // Search for specific Tropopause/Level data to show in station text
                            let stnFlHtml = '';
                            if (station.Id.toLowerCase() === "origin") {
                                let flightLevel = rd?.general?.initial_alt || rd?.general?.initial_altitude || '';
                                if (flightLevel) {
                                    flightLevel = flightLevel.replace(/^0+/, '');
                                    if (flightLevel.length === 5 && flightLevel.endsWith('00')) flightLevel = flightLevel.substring(0, 3);
                                    stnFlHtml += `<span class="bg-black/40 px-2 py-1 rounded text-sky-400 font-bold ml-2">FL${flightLevel}</span>`;
                                }
                            }

                            let variablesHtml = '';
                            const getSeverityStyle = (severity) => {
                                if (severity === 2) return { text: 'text-red-100 font-bold', bg: 'bg-red-900/60 border-red-500 shadow-[0_0_15px_rgba(239,68,68,0.4)] animate-pulse' };
                                if (severity === 1) return { text: 'text-orange-100 font-bold', bg: 'bg-orange-900/50 border-orange-500/80 shadow-[0_0_10px_rgba(249,115,22,0.3)]' };
                                return null;
                            };

                            const addPill = (label, value, defaultColorClass, severity) => {
                                if (value && value.trim() !== '') {
                                    const sevStyle = getSeverityStyle(severity);
                                    const finalTextColor = sevStyle ? sevStyle.text : defaultColorClass;
                                    const finalBgColor = sevStyle ? sevStyle.bg : 'bg-black/40 border-white/5';
                                    variablesHtml += `
                                        <div class="flex flex-col rounded p-2 justify-center items-center text-center transition-all border ${finalBgColor}">
                                            <span class="text-[9px] uppercase tracking-wider text-slate-400/90 mb-1">${label}</span>
                                            <span class="font-bold text-[13px] ${finalTextColor}">${value}</span>
                                        </div>
                                    `;
                                }
                            };

                            addPill('QNH', station.Qnh, 'text-purple-400', 0);
                            addPill('Wind', station.Wind, 'text-emerald-400', station.WindSeverity);
                            addPill('Temp/Dew', station.TempDew, 'text-slate-200', 0);
                            addPill('Visibility', station.Visibility, 'text-sky-400', station.VisibilitySeverity);
                            addPill('Clouds', station.CloudBase, 'text-slate-300', station.CloudSeverity);

                            html += `
                            <div class="bg-[#232730] p-5 rounded-xl border border-white/5 flex flex-col gap-4 shadow-xl relative overflow-hidden">
                                <div class="absolute inset-0 bg-gradient-to-br from-white/5 to-transparent pointer-events-none"></div>
                                
                                <div class="flex items-center justify-between border-b border-white/5 pb-3 relative z-10">
                                    <div class="flex items-center gap-3">
                                        <span class="material-symbols-outlined text-sky-400 text-2xl">${icon}</span>
                                        <h4 class="text-white font-bold uppercase tracking-widest text-sm m-0">${station.Label} ${station.Icao ? `<span class="text-sky-400/80 tracking-widest">${station.Icao}</span>` : ''}</h4>
                                        ${stnFlHtml}
                                    </div>
                                </div>

                                <div class="bg-black/60 rounded p-3 font-mono text-[11px] text-slate-400 leading-relaxed border border-white/5 relative z-10">
                                    <span class="block mb-2 text-white break-words">${station.RawMetar || 'NO METAR'}</span>
                                    <span class="block text-slate-500 break-words">${station.RawTaf || 'NO TAF'}</span>
                                </div>

                                ${variablesHtml !== '' ? `<div class="grid grid-cols-2 xl:grid-cols-5 gap-2 mt-2 relative z-10">${variablesHtml}</div>` : ''}

                                ${station.RunwayAdvice ? `
                                <div class="mt-2 text-slate-300 font-medium text-[11px] flex items-center gap-2 bg-emerald-900/20 p-2 rounded border border-emerald-500/20 relative z-10">
                                    <span class="material-symbols-outlined text-emerald-400 text-[16px]">flight_takeoff</span>
                                    ${station.RunwayAdvice.replace('Warning:', '<strong class="text-orange-400 uppercase tracking-widest ml-1 text-[9px]">CAUTION</strong>')}
                                </div>` : ''}

                                ${station.Commentary ? `
                                <div class="mt-2 text-slate-400 italic font-serif text-[12px] border-l-2 border-slate-500/50 pl-4 py-2 bg-black/20 rounded-r-lg leading-relaxed relative z-10">
                                    "${station.Commentary}"
                                </div>` : ''}

                                ${station.Notams && station.Notams.trim() !== '' ? `
                                <div class="mt-4 pt-4 border-t border-red-500/20 bg-red-900/10 p-3 rounded relative z-10">
                                    <h5 class="text-red-400/90 uppercase text-[9px] font-bold tracking-widest mb-2 flex items-center gap-1"><span class="material-symbols-outlined text-red-500 text-[14px]">warning</span> NOTAMS & ALERTS</h5>
                                    <div class="text-red-200/80 text-[10px] whitespace-pre-wrap leading-relaxed">${station.Notams}</div>
                                </div>` : ''}
                            </div>`;
                        });
                    }
                    html += '</div>';

                    if (data.EnrouteText) {
                        html += `
                        <div class="mt-6 bg-[#232730] p-5 rounded-xl border border-white/5 shadow-xl relative overflow-hidden">
                            <div class="flex items-center gap-2 mb-4 border-b border-white/5 pb-3 relative z-10">
                                <span class="material-symbols-outlined text-sky-400 text-lg">public</span>
                                <h4 class="text-white font-bold uppercase tracking-widest text-xs m-0">EN ROUTE & OPERATIONS</h4>
                            </div>
                            <div class="text-slate-300 leading-relaxed font-body text-xs whitespace-pre-wrap relative z-10">
                                ${data.EnrouteText}
                            </div>
                        </div>`;
                    }

                    return html;
                };


window.renderBriefingTabs = () => {
                    const pillsContainer = document.getElementById('briefingPills');
                    const viewsContainer = document.getElementById('briefingViewsContainer');
                    if (!pillsContainer || !viewsContainer || window.allRotations.length === 0) return;

                    pillsContainer.innerHTML = '';
                    viewsContainer.innerHTML = '';
                    pillsContainer.style.display = 'flex'; // Restore Pills!

                    const timeStr = (unixTimestamp) => {
                        if (isNaN(unixTimestamp) || unixTimestamp <= 0) return '---';
                        const d = new Date(unixTimestamp * 1000);
                        return d.getUTCHours().toString().padStart(2, '0') + d.getUTCMinutes().toString().padStart(2, '0');
                    };

                    const convertWeight = (valStr) => {
                        if (!valStr || valStr === '0' || valStr === '0.0') return '---';
                        let val = parseFloat(valStr);
                        if (isNaN(val)) return valStr;
                        return Math.round(val);
                    };

                    const AIRLINES = {
                        'AFR': 'Air France', 'BAW': 'British Airways', 'EZY': 'easyJet', 'RYR': 'Ryanair',
                        'DLH': 'Lufthansa', 'UAE': 'Emirates', 'QTR': 'Qatar Airways', 'DAL': 'Delta',
                        'AAL': 'American Airlines', 'UAL': 'United', 'SWA': 'Southwest'
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
                                    <h2 class="text-4xl font-black text-white font-headline tracking-widest uppercase">${AIRLINES[window.allRotations[0].data?.general?.icao_airline] || window.allRotations[0].data?.general?.airline_name || window.allRotations[0].data?.general?.icao_airline || 'AIRLINE'}</h2>
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
                                    <div class="text-3xl font-black text-white">100 <span class="text-xs text-slate-500">/100</span></div>
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
                        <div class="space-y-4">`;

                    window.allRotations.forEach((rot, idx) => {
                        const rd = rot.data;
                        globalHtml += `
                            <div onclick="window.setBriefingTab(${idx + 1})" class="bg-gradient-to-r from-[#171A21] to-[#12141A] hover:from-sky-900/10 hover:to-[#171A21] transition-all p-6 rounded-xl border border-white/5 cursor-pointer flex items-center justify-between group shadow-lg">
                                <div class="flex items-center gap-6">
                                    <div class="text-4xl font-black text-slate-800/80 group-hover:text-sky-500/30 transition-colors">${idx + 1}</div>
                                    <div>
                                        <div class="text-xl font-black text-white tracking-widest flex items-center gap-4">
                                            <span class="text-sky-400">${rd.origin?.icao_code || '---'}</span>
                                            <span class="material-symbols-outlined text-sm text-slate-500">arrow_forward</span>
                                            <span class="text-emerald-400">${rd.destination?.icao_code || '---'}</span>
                                        </div>
                                        <div class="text-xs font-mono text-slate-400 mt-2 uppercase tracking-widest">
                                            Flight ${rd.general?.icao_airline || ''}${rd.general?.flight_number || ''}
                                        </div>
                                    </div>
                                </div>
                                <div class="text-right flex flex-col items-end gap-2">
                                    <div class="flex items-center gap-4 bg-black/40 px-4 py-2 rounded-lg border border-white/5 font-mono text-[11px]">
                                        <span class="text-slate-500">SOBT</span> <span class="text-slate-200">${timeStr(rd.times?.sched_out)}Z</span>
                                        <span class="text-slate-600">&bull;</span>
                                        <span class="text-slate-500">SIBT</span> <span class="text-slate-200">${timeStr(rd.times?.sched_in)}Z</span>
                                    </div>
                                    <div class="flex items-center gap-4">
                                        <div class="text-[11px] text-slate-500 font-bold tracking-widest uppercase mt-1 mr-2">
                                            ETE ${rd.times?.est_time_enroute ? Math.floor(rd.times.est_time_enroute/3600).toString().padStart(2,'0') + 'H' + Math.floor((rd.times.est_time_enroute%3600)/60).toString().padStart(2,'0') : '---'}
                                        </div>
                                        <button onclick="event.stopPropagation(); window.chrome.webview.postMessage({action: 'fenixExport', path: localStorage.getItem('fenixExportPath'), jsonPayload: JSON.stringify(window.allRotations[${idx}].data)})"
                                                class="flex items-center gap-2 px-3 py-1 bg-[#1C1F26] hover:bg-amber-500/20 text-slate-500 hover:text-amber-500 text-[10px] font-bold tracking-widest uppercase border border-white/5 hover:border-amber-500/30 rounded transition-colors shadow-lg group-hover:block transition-all">
                                            <span class="material-symbols-outlined text-[14px]">save</span> EXPORT TO FENIX
                                        </button>
                                    </div>
                                </div>
                            </div>
                        `;
                    });

                    globalHtml += `</div>
                                   <div class="mt-10 mb-10 flex justify-end">
                                        <button onclick="document.getElementById('btnStartGroundOps').click()" class="bg-[#232730] hover:bg-white/10 text-slate-300 hover:text-white font-bold py-4 px-10 rounded-xl uppercase tracking-widest transition-all shadow-[0_0_20px_rgba(0,0,0,0.5)] flex items-center gap-3 text-xs border border-white/5">
                                            <span class="material-symbols-outlined text-[18px]">flight_takeoff</span> START OPS
                                        </button>
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
                            flightLevel = flightLevel.replace(/^0+/, '');
                            if (flightLevel.length === 5 && flightLevel.endsWith('00')) flightLevel = flightLevel.substring(0, 3);
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

                        const parsedHtml = window.parseBriefing(rot.briefing, rd);

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
                                        <h3 class="text-amber-500 font-bold tracking-widest uppercase">Dummy Leg — Awaiting OFP</h3>
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
                                    ${parsedHtml}
                                </div>
                            </div>
                        </div>`;
                        }

                        viewsContainer.innerHTML += legHtml;
                    });


                    window.setBriefingTab = setActiveTab;
                    // Auto-focus on the Global Overview (Index 0) instead of the latest leg
                    setActiveTab(0);
                };


document.addEventListener('DOMContentLoaded', () => {
    window.isFlightActive = false;

    // --- UTILITIES ---
    window.saveLocalToggle = function(key, isChecked) {
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

            // Fallback intelligence : si le MP3 échoue, on tente la suite.
            this.audioElement.onerror = (e) => {
                console.warn(`[AudioEngine] Fichier introuvable ou erreur de lecture - ${filename}`);
                this.playNext(); // Failsafe
            };

            this.audioElement.src = `assets/sounds/${filename}`;
            this.audioElement.load();

            const playPromise = this.audioElement.play();
            if (playPromise !== undefined) {
                playPromise.catch(error => {
                    console.warn(`[AudioEngine] autoplay empêché ou erreur sur ${filename}.mp3`, error);
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
        const dt = new Date();
        const format = localStorage.getItem('selTimeFormat') || '24H';
        return dt.toLocaleTimeString('en-US', { hour: '2-digit', minute: '2-digit', second: '2-digit', hour12: format === '12H' });
    };

    window.requestAcarsUpdate = function () {
        if (!window.allRotations || window.allRotations.length === 0) return;
        
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
        if (statusStr) {
            statusStr.style.display = 'block';
            statusStr.innerText = 'SENDING...';
            statusStr.classList.add('text-sky-400');
            statusStr.classList.remove('text-emerald-400', 'text-amber-400');
        }
        
        setTimeout(() => {
            if (statusStr) {
                statusStr.innerText = 'UPLINK IN PROGRESS';
                statusStr.classList.replace('text-sky-400', 'text-amber-400');
            }
            document.getElementById('acarsScratchpad').innerText = 'AOC MSG RCV...';
            
            setTimeout(() => {
                if (window.chrome && window.chrome.webview) {
                    window.chrome.webview.postMessage({ action: 'acarsWeatherRequest' });
                }
                
                if (statusStr) {
                    statusStr.innerText = 'PRINTING NEW WX...';
                    statusStr.classList.replace('text-amber-400', 'text-emerald-400');
                    statusStr.classList.remove('animate-pulse');
                }
                document.getElementById('acarsScratchpad').innerText = 'WX BRIEF UPDATED';
                
                const btnClose = document.getElementById('btnAcarsClose');
                if (btnClose) btnClose.innerHTML = '&lt; EXIT';
                
            }, 3000);
        }, 2000);
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

    const buildSelectOptions = (options) => {
        if (options.length === 0) return `<option data-disabled="true" style="background-color: #1E2433; color: inherit;">STANDING BY...</option>`;

        // Sort options so that enabled ones appear first
        const sorted = [...options].sort((a, b) => (a.disabled === b.disabled) ? 0 : a.disabled ? 1 : -1);

        return sorted.map(o => {
            const text = o.disabled && o.reason ? `${o.text} (${o.reason})` : o.text;
            return `<option value="${o.val}" data-action="${o.action || 'announceCabin'}" data-disabled="${o.disabled ? 'true' : 'false'}" style="background-color: #1E2433; color: inherit;">${text}</option>`;
        }).join('');
    };

    const updateDropdown = (selectId, btnId, options, baseColorClass) => {
        const select = document.getElementById(selectId);
        const btn = document.getElementById(btnId);
        if (!select || !btn) return;

        const html = buildSelectOptions(options);
        // Avoid DOM reset if no changes (prevents flickering)
        if (select.dataset.lastHtml !== html) {
            const prevVal = select.value;
            select.innerHTML = html;
            select.dataset.lastHtml = html;

            if (options.length > 0 && !options.every(o => o.disabled)) {
                // Try to keep previously selected if it's still enabled, else select the first enabled
                const newMatch = Array.from(select.options).find(o => o.value === prevVal);
                if (newMatch && newMatch.getAttribute('data-disabled') !== "true") select.value = prevVal;
                else select.selectedIndex = 0;
            } else if (options.length > 0) {
                select.selectedIndex = 0; // Select the first disabled item as fallback
            }
        }

        const isCompletelyDisabled = select.options.length === 0 || select.options[select.selectedIndex]?.getAttribute('data-disabled') === "true";

        if (isCompletelyDisabled) {
            select.classList.add('opacity-50');

            btn.disabled = true;
            btn.classList.add('opacity-50'); /* REMOVED cursor-not-allowed */
            btn.classList.remove(baseColorClass);
            btn.title = 'Aucune action disponible en ce moment';
        } else {
            select.classList.remove('opacity-50');

            btn.disabled = false;
            btn.classList.remove('opacity-50'); /* REMOVED cursor-not-allowed */
            btn.classList.add(baseColorClass);
            btn.title = 'Broadcast on Intercom';
        }
    };

    function updateIntercomButtons(payload) {
        // We no longer build a dynamic layout block, we push everything into the two dropdowns!
        const phase = payload.phaseEnum;
        const used = payload.issuedCommands || [];

        // 1. FLIGHT DECK PA ACTIONS
        const paOptions = [];

        if (!used.includes('PA_Welcome')) {
            const ok = ['AtGate', 'Pushback', 'TaxiOut'].includes(phase) && payload.isBoardingComplete;
            paOptions.push({ val: 'Welcome', text: 'PA: Welcome Aboard', disabled: !ok, reason: 'Embarquement requis' });
        }
        if (!used.includes('PA_Approach') && ['Approach', 'FinalApproach'].includes(phase)) {
            const ok = phase === 'Approach';
            paOptions.push({ val: 'Approach', text: 'PA: Approach Info', disabled: !ok, reason: 'Approche requise' });
        }

        paOptions.push({ val: 'CruiseStatus', text: 'PA: Cruise Status', disabled: phase !== 'Cruise', reason: 'Croisière requise' });
        paOptions.push({ val: 'ArrivalWeather', text: 'PA: Arrival Weather', disabled: !['Descent', 'Approach'].includes(phase), reason: 'Descente/Approche requise' });

        if (flightHasExperiencedDelay) {
            paOptions.push({ val: 'DelayApology', text: 'PA: Delay Apology', disabled: false, reason: '' });
        }

        if (flightHasExperiencedTurbulence) {
            const isInAir = ['Takeoff', 'Climb', 'Cruise', 'Descent', 'Approach', 'FinalApproach'].includes(phase);
            paOptions.push({ val: 'TurbulenceApology', text: 'PA: Turbulence Apology', disabled: !isInAir, reason: 'En vol uniquement' });
        }

        if (payload.isGoAroundActive) {
            paOptions.push({ val: 'GoAround', text: '*** PA: Go-Around ***', disabled: false, reason: '' });
        }
        if (payload.isSevereTurbulenceActive && phase === 'Cruise') {
            paOptions.push({ val: 'Turbulence', text: '*** PA: Severe Turbulence ***', disabled: false, reason: '' });
        }
        if (payload.activeCrisis === 'MedicalEmergency') {
            paOptions.push({ val: 'MedicalEmergency', text: '*** PA: Doctor On Board ***', disabled: false, reason: '', action: 'resolveCrisis' });
        }

        // 2. FLIGHT DECK TO PNC ACTIONS
        const pncOptions = [];

        const diffSec = payload.cabinReportCooldownElapsed || 999;
        const isCd = diffSec < 120;
        const cdLeft = Math.ceil(120 - diffSec);
        pncOptions.push({
            val: 'intercomQuery',
            text: 'INT: Request Cabin Report',
            disabled: isCd,
            reason: isCd ? `Report Cooldown (${Math.floor(cdLeft / 60)}m ${cdLeft % 60}s)` : '',
            action: 'intercomQuery'
        });

        if (!used.includes('ARM_DOORS') && ['AtGate', 'Pushback'].includes(phase)) {
            const ok = payload.isBoardingComplete;
            pncOptions.push({ val: 'ARM_DOORS', text: 'INT: Arm Doors', disabled: !ok, reason: 'Embarquement requis', action: 'pncCommand' });
        }
        if (!used.includes('PREPARE_TAKEOFF') && phase === 'TaxiOut') {
            pncOptions.push({ val: 'PREPARE_TAKEOFF', text: 'INT: Prepare Cabin for Takeoff', disabled: false, reason: '', action: 'pncCommand' });
        }
        if (!used.includes('SEATS_TAKEOFF') && phase === 'TaxiOut') {
            const isReady = payload.securingProgress >= 100;
            pncOptions.push({ val: 'SEATS_TAKEOFF', text: isReady ? 'INT: Seats for Takeoff' : 'INT: Force Seats (Caution)', disabled: false, reason: '', action: 'pncCommand' });
        }
        if (!used.includes('TOP_DESCENT') && ['Cruise', 'Descent'].includes(phase)) {
            pncOptions.push({ val: 'TOP_DESCENT', text: 'INT: Inform Top of Descent', disabled: false, reason: '', action: 'pncCommand' });
        }
        if (!used.includes('PREPARE_LANDING') && ['Cruise', 'Descent', 'Approach', 'FinalApproach'].includes(phase)) {
            const ok = payload.altitude <= 10000 && phase !== 'Cruise';
            pncOptions.push({ val: 'PREPARE_LANDING', text: 'INT: Prepare Cabin for Landing', disabled: !ok, reason: 'Passage sous 10 000 ft requis', action: 'pncCommand' });
        }
        if (!used.includes('SEATS_LANDING') && ['Descent', 'Approach'].includes(phase)) {
            const ok = phase === 'Approach' || payload.altitude <= 5000;
            const isReady = payload.securingProgress >= 100;
            pncOptions.push({ val: 'SEATS_LANDING', text: isReady ? 'INT: Seats for Landing' : 'INT: Force Seats (Caution)', disabled: !ok, reason: "Approche requise", action: 'pncCommand' });
        }
        if (payload.cabinState === 'ServingMeals') {
            const svcText = payload.isServiceHalted ? 'PNC: Resume Service' : 'PNC: Pause Service';
            pncOptions.push({ val: 'toggleService', text: svcText, disabled: false, reason: '', action: 'toggleService' });
        }
        if (payload.activeCrisis === 'UnrulyPassenger') {
            pncOptions.push({ val: 'UnrulyPassenger', text: '*** INT: Restrain Passenger ***', disabled: false, reason: '', action: 'resolveCrisis' });
        }

        updateDropdown('dropdownPA', 'btnStaticPA', paOptions, 'hover:bg-sky-800/60');
        updateDropdown('dropdownPNC', 'btnStaticPNC', pncOptions, 'hover:bg-amber-800/60');
    }

    window.triggerPA = function () {
        const select = document.getElementById('dropdownPA');
        if (!select || !select.value) return;
        const opt = select.options[select.selectedIndex];
        if (opt.getAttribute('data-disabled') === "true") return;

        const action = opt.getAttribute('data-action') || 'announceCabin';
        const propName = action === 'resolveCrisis' ? 'crisisType' : 'annType';
        window.chrome.webview.postMessage({ action: action, [propName]: select.value });
    };

    window.triggerPNC = function () {
        const select = document.getElementById('dropdownPNC');
        if (!select || !select.value) return;
        const opt = select.options[select.selectedIndex];
        if (opt.getAttribute('data-disabled') === "true") return;

        const action = opt.getAttribute('data-action') || 'pncCommand';

        let msg = { action: action };
        if (action === 'pncCommand') msg.command = select.value;
        else if (action === 'resolveCrisis') msg.crisisType = select.value;

        window.chrome.webview.postMessage(msg);
    };


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
            const ffClean = document.getElementById('chkFirstFlightClean') ? document.getElementById('chkFirstFlightClean').checked : true;

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
            localStorage.setItem('groundProb', groundProb);
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
                if (window.flightPhase === 'Turnaround' || window.flightPhase === 'Arrived') {
                    let username = localStorage.getItem('sbUsername') || '';
                    if (!username || username.trim() === '') {
                        alert('Please configure your SimBrief ID/Username in the Settings tab first!');
                        return;
                    }
                    const dispatchModal = document.getElementById('simbriefDispatchModal');
                    if (dispatchModal) {
                        dispatchModal.style.display = 'flex';
                        const loader = document.getElementById('simbriefLoadingState');
                        if (loader) {
                            loader.style.display = 'none';
                            loader.classList.add('hidden');
                        }

                        const btnValidate = document.getElementById('btnValidateLeg');
                        if (btnValidate) {
                            btnValidate.disabled = false;
                            btnValidate.classList.remove('opacity-50', 'cursor-not-allowed');
                            window.currentLegCounter++;
                            document.getElementById('lblValidateLeg').innerText = 'VALIDATE LEG ' + window.currentLegCounter;
                            btnValidate.dataset.username = username;
                            btnValidate.dataset.pristine = "false";
                        }
                    }
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
            for(let i = 0; i < 5; i++) {
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
        const a = Math.sin(dLat/2) * Math.sin(dLat/2) + Math.cos(lat1) * Math.cos(lat2) * Math.sin(dLon/2) * Math.sin(dLon/2);
        const c = 2 * Math.atan2(Math.sqrt(a), Math.sqrt(1-a));
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

    const btnDutyNext = document.getElementById('btnDutyNext');
    if (btnDutyNext) {
        btnDutyNext.addEventListener('click', () => {
            let unInput = document.getElementById('dutySbUsername');
            let username = unInput && unInput.value.trim() !== '' ? unInput.value.trim() : localStorage.getItem('sbUsername') || '';

            if (!username || username === '') {
                alert('Please enter your SimBrief ID/Username.');
                return;
            }
            
            window.plannedDummyLegs = [];
            
            if (window.currentDutyMode === 'custom') {
                let plannedRouteInput = document.getElementById('dutyPlannedRoute');
                if (plannedRouteInput && plannedRouteInput.value.trim() !== '') {
                    let icaos = plannedRouteInput.value.toUpperCase().split(/[^A-Z0-9]/).filter(x => x.length === 4);
                    if (icaos.length >= 2) {
                        for (let i = 0; i < icaos.length - 1; i++) {
                            let dummyData = getDummyLeg(icaos[i], icaos[i+1]);
                            if (dummyData) window.plannedDummyLegs.push(dummyData);
                        }
                    }
                }
            } else if (window.currentDutyMode === 'roster') {
                const airline = document.getElementById('rosterSelAirline').value;
                const hub = document.getElementById('rosterSelHub').value;
                if (window.predefinedRosters && window.predefinedRosters[airline] && window.predefinedRosters[airline][hub]) {
                    const rot = window.predefinedRosters[airline][hub].find(r => r.id === window.selectedRosterId);
                    if (rot && rot.legs && rot.legs.length >= 2) {
                        for (let i = 0; i < rot.legs.length - 1; i++) {
                            let dummyData = getDummyLeg(rot.legs[i], rot.legs[i+1]);
                            if (dummyData) window.plannedDummyLegs.push(dummyData);
                        }
                    }
                }
            }

            localStorage.setItem('sbUsername', username);

            const ffClean = currentDutyState === 'pristine';
            localStorage.setItem('firstFlightClean', ffClean);

            const groundSpeed = localStorage.getItem('groundSpeed') || 'Realistic';
            const groundProb = localStorage.getItem('groundProb') || '25';
            const weatherSrc = 'simbrief';

            document.getElementById('dutySetupModal').style.display = 'none';

            if (window.currentDutyMode === 'roster' && window.plannedDummyLegs.length > 0) {
                // BYPASS SimBrief Dispatch Modal entirely
                window.allRotations = [];
                let currentSec = Math.floor(Date.now() / 1000) + (45 * 60); // Starts in 45 minutes
                window.plannedDummyLegs.forEach((dummy, idx) => {
                    // Inject sequential dummy times
                    dummy.times.sched_out = currentSec.toString();
                    const ete = parseInt(dummy.times.est_time_enroute || '0');
                    currentSec += ete;
                    dummy.times.sched_in = currentSec.toString();
                    currentSec += (35 * 60); // 35 min turnaround for the next leg

                    const sbLegId = `dummy_${Date.now()}_${idx}`;
                    window.allRotations.push({
                        id: sbLegId,
                        flightId: 'EST',
                        data: dummy,
                        briefing: null,
                        timestamp: new Date().toISOString()
                    });
                });

                // Enable Ground Ops Button
                const btnStartOps = document.getElementById('btnStartGroundOps');
                if (btnStartOps) {
                    btnStartOps.disabled = false;
                    btnStartOps.classList.remove('opacity-30', 'cursor-not-allowed');
                }

                // Switch to Briefing Tab via the menu logic
                const briefMenuBtn = document.querySelector('.menu li[data-target="briefing"]');
                if (briefMenuBtn) briefMenuBtn.click();

                // Initialize Dashboard State for Roster Mode Bypass
                isFlightCancelled = false;
                window.isFlightActive = true;
                const dso = document.getElementById('dashStartOverlay');
                if (dso) dso.classList.add('opacity-0', 'pointer-events-none');
                
                const btnFetchLabel = document.getElementById('btnFetchPlanLabel');
                if (btnFetchLabel) btnFetchLabel.innerText = 'CANCEL FLIGHT';
                const btnFetch = document.getElementById('btnFetchPlan');
                if (btnFetch) btnFetch.querySelector('.material-symbols-outlined').innerText = 'cancel';
                
                const mScore = document.getElementById('mainScoreValue');
                if (mScore) mScore.innerText = "1000";
                const tScore = document.getElementById('topScoreValue');
                if (tScore) tScore.innerText = "1000";
                const pLogs = document.getElementById('penaltyLogs');
                if (pLogs) pLogs.innerHTML = "";
                const sFeed = document.getElementById('scoreFeed');
                if (sFeed) sFeed.innerHTML = "<li style=\"color:#64748b; text-align:center;\">Tracking standing by...</li>";

                if (window.renderBriefingTabs) window.renderBriefingTabs();
                if (window.updateDashboard) window.updateDashboard();

                return; // Exit here
            }

            const dispatchModal = document.getElementById('simbriefDispatchModal');
            if (dispatchModal) {
                dispatchModal.style.display = 'flex';
                // Reset states for fresh dispatch session
                window.currentLegCounter = 1;
                window.simbriefSavedLegsNodes = [];

                const dispatchContainer = document.getElementById('dispatchLegsContainer');
                if (dispatchContainer) {
                    dispatchContainer.innerHTML = `
                        <button onclick="window.chrome.webview.postMessage({ action: 'openSimbriefWindow' })" class="bg-[#1C1F26] border border-sky-500/30 text-white px-8 py-6 rounded-xl hover:bg-sky-500/10 hover:border-sky-500 shadow-xl transition-all font-bold tracking-widest flex items-center justify-between group w-full">
                            <div class="flex items-center gap-4">
                                <span class="material-symbols-outlined text-3xl group-hover:scale-110 transition-transform text-sky-400">open_in_new</span>
                                <div class="text-left">
                                    <div class="text-lg">ADD LEG 1</div>
                                    <div class="text-slate-500 text-[10px] uppercase mt-1 font-manrope font-normal">Open SimBrief to configure your first flight</div>
                                </div>
                            </div>
                            <span class="material-symbols-outlined text-slate-600">chevron_right</span>
                        </button>
                        <div id="simbriefLoadingState" class="hidden flex items-center justify-center p-6 bg-sky-900/10 border border-sky-500/20 rounded-xl mt-2">
                            <div class="w-6 h-6 border-2 border-sky-500/30 border-t-sky-400 rounded-full animate-spin mr-4"></div>
                            <span id="simbriefLoadingLabel" class="text-sky-400 font-label tracking-widest text-xs uppercase animate-pulse">Contacting Dispatch...</span>
                        </div>
                    `;
                }

                // Disable global finish button initially
                const btnFinishDispatch = document.getElementById('btnFinishDispatch');
                if (btnFinishDispatch) {
                    btnFinishDispatch.classList.add('opacity-50', 'cursor-not-allowed', 'pointer-events-none');
                }
            }
        });
    }

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
            const btnStartOps = document.getElementById('btnStartGroundOps');
            if (btnStartOps) {
                btnStartOps.disabled = false;
                btnStartOps.classList.remove('opacity-30', 'cursor-not-allowed');
            }
            // Reset footer states
            const btnFinishDispatch = document.getElementById('btnFinishDispatch');
            if (btnFinishDispatch) {
                btnFinishDispatch.classList.add('opacity-50', 'cursor-not-allowed', 'pointer-events-none');
            }
            window.chrome.webview.postMessage({ action: 'finishDispatch' });
        });
    }

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
    let flightHasExperiencedDelay = false;
    let flightHasExperiencedTurbulence = false;

    // WebView2 Global Message Receiver
    window.chrome.webview.addEventListener('message', event => {
        const payload = event.data;
        if (!payload || !payload.type) return;

        switch (payload.type) {
            case 'simbriefWindowClosed':
                // Auto-fetch flight plan! No validation step needed.
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
                    groundSpeed: localStorage.getItem('groundSpeed') || 'Realistic',
                    groundProb: localStorage.getItem('groundProb') || '25',
                    firstFlightClean: ffCln,
                    syncMsfsTime: document.getElementById('chkSyncTime') ? document.getElementById('chkSyncTime').checked : false,
                    units: {
                        weight: localStorage.getItem('selUnitWeight') || 'LBS',
                        temp: localStorage.getItem('selUnitTemp') || 'C',
                        alt: localStorage.getItem('selUnitAlt') || 'FT',
                        speed: localStorage.getItem('selUnitSpeed') || 'KTS',
                        press: localStorage.getItem('selUnitPress') || 'HPA',
                        time: localStorage.getItem('selTimeFormat') || '24H'
                    }
                });
                break;
            case 'shiftResumeAvailable':
                const resumeModal = document.getElementById('shiftResumeModal');
                if (resumeModal) {
                    const srIcao = document.getElementById('srIcao');
                    const srAirline = document.getElementById('srAirline');
                    const srDate = document.getElementById('srDate');
                    if (srIcao) srIcao.innerText = payload.icao;
                    if (srAirline) srAirline.innerText = payload.airline;
                    if (srDate) srDate.innerText = payload.date;
                    
                    // ON NE L'AFFICHE PAS TOUT DE SUITE !
                    // On enregistre simplement que c'est dispo pour l'écran de démarrage.
                    window.pendingResumeShift = true;
                }
                break;
            case 'shiftResumed':
                const resMod = document.getElementById('shiftResumeModal');
                if (resMod) resMod.style.display = 'none';

                // Allow interactions
                const startOverlay = document.getElementById('dashStartOverlay');
                if (startOverlay) {
                    startOverlay.style.opacity = '0';
                    startOverlay.style.pointerEvents = 'none';
                }
                break;
            case 'shiftCleared':
                const clrMod = document.getElementById('shiftResumeModal');
                if (clrMod) clrMod.style.display = 'none';

                const sbUser = localStorage.getItem('sbUsername') || '';
                const unInput = document.getElementById('dutySbUsername');
                if (unInput) unInput.value = sbUser;
                if (!currentDutyState) selectDutyState('pristine');

                const dutyModal = document.getElementById('dutySetupModal');
                if (dutyModal) dutyModal.style.display = 'flex';
                break;
            case 'briefingUpdate':
                if (typeof window.parseBriefing === 'function') {
                    const parsedHtml = window.parseBriefing(payload.briefing);
                    const briefingElem = document.getElementById('briefingContent');
                    if (briefingElem && briefingElem.dataset.lastBriefingHtml !== parsedHtml) {
                        briefingElem.innerHTML = parsedHtml;
                        briefingElem.dataset.lastBriefingHtml = parsedHtml;
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
                window.lastTelemetry = payload;
                if (payload.isDelayed === true) flightHasExperiencedDelay = true;
                if (payload.turbulenceSeverity > 1) flightHasExperiencedTurbulence = true; // Moderate, Severe or Extreme

                updateIntercomButtons(payload);
                document.getElementById('flightPhase').innerText = `${payload.phase}`;

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
                    if (window.manifest && window.manifest.Passengers) {
                        window.manifest.Passengers.forEach(p => {
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

                const isAtGate = payload.phaseEnum === 'AtGate';
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
                if (payload.sessionFlightsCompleted !== undefined) window.activeLegIndex = payload.sessionFlightsCompleted;
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
                        document.getElementById('dashFlightCompany').innerText = GLOBAL_AIRLINES[aCode] || currentFlight.general?.airline_name || aCode || 'AIRLINE';
                        document.getElementById('dashFlightIdent').innerText = `${currentFlight.general?.icao_airline || ''}${currentFlight.general?.flight_number || ''}`;
                        
                        let baseType = currentFlight.aircraft?.base_type || '';
                        let acType = baseType;
                        if (baseType === 'A320') acType = 'Airbus A320-200';
                        else if (baseType === 'A20N') acType = 'Airbus A320neo';
                        else if (baseType === 'B738') acType = 'Boeing 737-800';
                        else if (baseType === 'B77W') acType = 'Boeing 777-300ER';
                        else if (!acType) acType = currentFlight.aircraft?.name || currentFlight.aircraft?.icaocode || 'Unknown';
                        
                        document.getElementById('dashAircraftType').innerText = acType;
                        document.getElementById('dashAircraftReg').innerText = currentFlight.aircraft?.reg || 'NO REG';
                        
                        let depCity = currentFlight.origin?.city || '';
                        let depNameStr = currentFlight.origin?.name || '---';
                        let arrCity = currentFlight.destination?.city || '';
                        let arrNameStr = currentFlight.destination?.name || '---';

                        if (depNameStr.includes('/')) {
                            const pts = depNameStr.split('/');
                            document.getElementById('dashDepCity').innerText = depCity || pts[0].trim();
                            document.getElementById('dashDepName').innerText = (pts[1] || '').trim();
                        } else {
                            document.getElementById('dashDepCity').innerText = depCity || depNameStr;
                            document.getElementById('dashDepName').innerText = depNameStr;
                        }

                        if (arrNameStr.includes('/')) {
                            const pts = arrNameStr.split('/');
                            document.getElementById('dashArrCity').innerText = arrCity || pts[0].trim();
                            document.getElementById('dashArrName').innerText = (pts[1] || '').trim();
                        } else {
                            document.getElementById('dashArrCity').innerText = arrCity || arrNameStr;
                            document.getElementById('dashArrName').innerText = arrNameStr;
                        }

                        if (currentFlight.times?.sched_out) {
                            currentSobtUnix = parseInt(currentFlight.times.sched_out);
                            const bdSobt = document.getElementById('bdSobt');
                            if (bdSobt) bdSobt.innerText = getFormattedTime(currentSobtUnix);
                        }
                        if (currentFlight.times?.sched_in) {
                            window.currentSibtUnix = parseInt(currentFlight.times.sched_in);
                            const bdSibt = document.getElementById('bdSibt');
                            if (bdSibt) bdSibt.innerText = getFormattedTime(window.currentSibtUnix);
                        }
                    }
                }
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
                    
                    // --- Global Rotation Timer Logic ---
                    const globalBanner = document.getElementById('globalRotationBanner');
                    if (globalBanner && window.allRotations && window.allRotations.length > 1 && payload.rawUnix) {
                        globalBanner.classList.remove('hidden');
                        
                        let currentIdx = window.currentLegIndex || 0;
                        document.getElementById('globalRotationStatus').innerText = `Leg ${currentIdx + 1} of ${window.allRotations.length}`;
                        
                        let lastLeg = window.allRotations[window.allRotations.length - 1];
                        let finalUnix = 0;
                        if (lastLeg.times?.sched_in) {
                            finalUnix = parseInt(lastLeg.times.sched_in);
                        }

                        if (finalUnix > 0) {
                            // Calculate current accumulated delay
                            let currentDelay = 0;
                            if (window.finalAibtUnix && window.currentSibtUnix > 0) {
                                currentDelay = window.finalAibtUnix - window.currentSibtUnix;
                            } else if (window.finalAobtUnix && typeof currentSobtUnix !== 'undefined' && currentSobtUnix > 0) {
                                currentDelay = window.finalAobtUnix - currentSobtUnix;
                            } else if (typeof currentSobtUnix !== 'undefined' && currentSobtUnix > 0 && payload.rawUnix > currentSobtUnix && !window.finalAobtUnix) {
                                currentDelay = payload.rawUnix - currentSobtUnix;
                            }
                            
                            // The true remaining time is reaching the FINAL leg's scheduled arrival, 
                            // shifted by whatever delay we've already accumulated right now.
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
                if (frBlock) frBlock.innerText = `${btHours}h ${btMins}m`;

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
                if (frFuel) frFuel.innerText = rep.BlockFuel;

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
                if (btnStartGroundOps) {
                    btnStartGroundOps.disabled = true;
                    btnStartGroundOps.innerHTML = '<span class="material-symbols-outlined text-[18px]">flight_takeoff</span> START OPS';
                }
                break;
            case 'flightData':
                const d = payload.data;
                const manifest = payload.manifest;

                const dispatchModal = document.getElementById('simbriefDispatchModal');
                if (dispatchModal && dispatchModal.style.display !== 'none') {
                    // Do not close the modal automatically anymore, display the "Next Leg" prompt
                    const loader = document.getElementById('simbriefLoadingState');
                    if (loader) loader.style.display = 'none';

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
                                errorMsg = `Leg ${window.currentLegCounter} cannot be identical to the previous flight (${origin} ➔ ${dest}). Please configure a new route in SimBrief.`;
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

                                dispatchContainer.innerHTML = (window.simbriefSavedLegsNodes ? window.simbriefSavedLegsNodes.join('') : '') + prevLegHTML + `
                                    <div id="simbriefLoadingState" class="hidden items-center justify-center p-6 bg-sky-900/10 border border-sky-500/20 rounded-xl mt-2">
                                        <div class="w-6 h-6 border-2 border-sky-500/30 border-t-sky-400 rounded-full animate-spin mr-4"></div>
                                        <span id="simbriefLoadingLabel" class="text-sky-400 font-label tracking-widest text-xs uppercase animate-pulse">Downloading OFP into Application...</span>
                                    </div>
                                `;
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
                                        <div class="text-emerald-500/70 text-[10px] uppercase mt-1 font-manrope font-bold">${origin} ➔ ${dest}</div>
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

                        dispatchContainer.innerHTML = window.simbriefSavedLegsNodes.join('') + nextButtonHtml + `
                            <div id="simbriefLoadingState" class="hidden items-center justify-center p-6 bg-sky-900/10 border border-sky-500/20 rounded-xl mt-2">
                                <div class="w-6 h-6 border-2 border-sky-500/30 border-t-sky-400 rounded-full animate-spin mr-4"></div>
                                <span id="simbriefLoadingLabel" class="text-sky-400 font-label tracking-widest text-xs uppercase animate-pulse">Downloading OFP into Application...</span>
                            </div>
                        `;

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
                            window.allRotations[i] = { data: d, briefing: payload.briefing, manifest: payload.manifest };
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
                        window.allRotations.push({ data: d, briefing: payload.briefing, manifest: payload.manifest });
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

                // Calculate estimated times for Dummy Legs based on preceding legs
                for (let i = 1; i < window.allRotations.length; i++) {
                    const prevLeg = window.allRotations[i - 1].data;
                    const currLeg = window.allRotations[i].data;
                    
                    if (currLeg.isDummy && prevLeg.times?.sched_in) {
                        // Hardcoding a 35 min average turnaround for dummy estimations
                        const tatSeconds = 35 * 60; 
                        
                        // New SOBT is previous SIBT + TAT
                        currLeg.times.sched_out = (parseInt(prevLeg.times.sched_in) + tatSeconds).toString();
                        
                        // New SIBT is new SOBT + ETE
                        const eteSeconds = parseInt(currLeg.times.est_time_enroute || '0');
                        currLeg.times.sched_in = (parseInt(currLeg.times.sched_out) + eteSeconds).toString();
                    }
                }

                
                // Briefing Tab Rendering Engine (Navigation Shift)
                
                // Trigger render
                window.renderBriefingTabs();

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
                if (dhAirline) dhAirline.innerText = d.general?.airline_name || d.general?.icao_airline || 'Unknown';

                if (payload.manifest) {
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
                break;
            case 'briefingUpdate':
                if (window.allRotations && window.allRotations.length > 0) {
                    // Assume the live ACARS update targets the active flight in the rotation (index 0)
                    window.allRotations[0].briefing = payload.briefing;
                    if (window.renderBriefingTabs) {
                        // Store the current tab index so we don't jump back to the last tab when re-rendering
                        const activeViewIndex = Array.from(document.querySelectorAll('.briefing-view')).findIndex(v => v.style.display !== 'none');
                        window.renderBriefingTabs();
                        if (activeViewIndex >= 0 && window.setBriefingTab) {
                            window.setBriefingTab(activeViewIndex);
                        }
                    }
                }
                break;
            case 'groundOps':
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
    'Refuel': '<span class="material-symbols-outlined text-[18px] text-orange-500">local_gas_station</span>',
    'Boarding': '<span class="material-symbols-outlined text-[18px] text-sky-400">group</span>',
    'Cargo': '<span class="material-symbols-outlined text-[18px] text-amber-500">luggage</span>',
    'Catering': '<span class="material-symbols-outlined text-[18px] text-pink-400">restaurant</span>',
    'Cleaning': '<span class="material-symbols-outlined text-[18px] text-fuchsia-400">cleaning_services</span>',
    'PNC Chores': '<span class="material-symbols-outlined text-[18px] text-indigo-400">dry_cleaning</span>',
    'Water/Waste': '<span class="material-symbols-outlined text-[18px] text-emerald-400">water_drop</span>'
};

const GO_NARRATIVES = {
    'Refuel': 'gops_desc_refuel',
    'Boarding': 'gops_desc_boarding',
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
    
    if (!services || services.length === 0) {
        container.innerHTML = '<p class="text-slate-500 font-mono text-center delay-fade-in" data-i18n="ground_pending">Ground operations pending SimBrief initialization...</p>';
        return;
    }
    
    let html = '<div class="grid grid-cols-1 md:grid-cols-2 gap-4">';

    services.forEach(s => {
        const mLang = (localStorage.getItem('selLanguage') || 'EN').toLowerCase();
        const mDict = window.locales && window.locales[mLang] ? window.locales[mLang] : window.locales.en;

        let locName = s.Name !== undefined ? s.Name : s.name;
        if (locName === "Refueling") locName = mDict.gops_refueling || locName;
        else if (locName === "Boarding") locName = mDict.gops_boarding || locName;
        else if (locName === "Cargo") locName = mDict.gops_cargo || locName;
        else if (locName === "Catering") locName = mDict.gops_catering || locName;
        else if (locName === "Cleaning") locName = mDict.gops_cleaning || locName;
        else if (locName === "Water/Waste") locName = mDict.gops_water || locName;

        let locStatus = s.StatusMessage !== undefined ? s.StatusMessage : s.statusMessage;
        if (s.IsPreServiced || s.isPreServiced) locStatus = mDict.gops_pre_serviced || "ALREADY SERVICED";
        else if (locStatus === "In Progress") locStatus = mDict.gops_stat_prog || locStatus;
        else if (locStatus === "Completed") locStatus = mDict.gops_stat_comp || locStatus;
        else if (locStatus === "Skipped by Capt.") locStatus = mDict.gops_stat_skip || locStatus;

        let btnSkipText = "Skip / Abort";
        btnSkipText = mDict.gops_skip_abort || btnSkipText;

        let btnHtml = '';
        let stateVal = s.State !== undefined ? s.State : s.state;
        let isOpt = s.IsOptional !== undefined ? s.IsOptional : s.isOptional;

        if (stateVal === 5 && locName === "Refueling") {
            btnHtml = `<button class="go-btn px-4 py-2 bg-amber-600/20 text-amber-500 hover:bg-amber-600/40 border border-amber-500/30 rounded text-xs tracking-widest uppercase font-bold transition-colors shadow-[0_0_10px_rgba(245,158,11,0.2)]" onclick="window.chrome.webview.postMessage({action: 'startService', service: '${s.Name}'})">Request Fuel Truck</button>`;
        }
        else if (isOpt && stateVal !== 3 /* Completed */ && stateVal !== 4 /* Skipped */ && stateVal !== 0) {
            btnHtml = `<button class="go-btn px-4 py-2 bg-slate-800 text-slate-300 hover:bg-slate-700 hover:text-white rounded text-xs tracking-widest uppercase transition-colors" onclick="skipService('${locName}')">${btnSkipText}</button>`;
        }

        let getSeverityColor = (sec) => {
            if (sec < 180) return '#FACC15';
            if (sec <= 420) return '#FB923C';
            return '#EF4444';
        };

        let statusColor = '#94A3B8';
        let barColor = '#4A90E2';
        let titleColor = stateVal === 3 ? ((s.IsPreServiced || s.isPreServiced) ? '#64748B' : '#34D399') : '#F8FAFC';
        let opacityClass = (s.IsPreServiced || s.isPreServiced) ? 'opacity-50' : '';

        if (stateVal === 2 /* Delayed */) {
            let delaySec = s.DelayAddedSec !== undefined ? s.DelayAddedSec : s.delayAddedSec;
            let c = getSeverityColor(delaySec);
            statusColor = c;
            barColor = c;
        }
        else if (stateVal === 3 /* Completed */) {
            if (s.IsPreServiced || s.isPreServiced) {
                statusColor = '#64748B';
                barColor = '#475569';
            } else {
                statusColor = '#34D399';
                barColor = '#34D399';
            }
        }
        else if (stateVal === 4 /* Skipped */) { statusColor = '#F87171'; barColor = '#F87171'; }
        else if (stateVal === 5 /* WaitingForAction */) { statusColor = '#FACC15'; barColor = '#334155'; }
        else if (stateVal === 0 /* NotStarted */) { statusColor = '#64748B'; barColor = '#334155'; }
        let timeDisplay = '';
        let remainingSec = s.RemainingSec !== undefined ? s.RemainingSec : s.remainingSec;
        if (stateVal === 0) {
            let offset = s.StartOffsetMinutes !== undefined ? s.StartOffsetMinutes : s.startOffsetMinutes;
            if (offset < 0) {
                // Negative offset means it is scheduled to start BEFORE SOBT!
                timeDisplay = `(T${offset})`;
            }
        }
        else if (remainingSec > 0 && stateVal !== 3 && stateVal !== 4) {
            const m = Math.floor(remainingSec / 60).toString().padStart(2, '0');
            const sec = (remainingSec % 60).toString().padStart(2, '0');
            timeDisplay = `(${m}:${sec})`;
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
        if (stateVal === 3) statusStateLabel = 'FINISHED';
        else if (stateVal === 4) statusStateLabel = 'ABORTED';
        else if (stateVal === 2) statusStateLabel = 'DELAYED';
        else if (stateVal === 0) statusStateLabel = 'NOT STARTED';
        else if (stateVal === 5) statusStateLabel = 'WAITING FOR ACTION';

        let extraBadgesHtml = '';
        if (window.lastTelemetry && stateVal !== 1 && (!s.IsPreServiced && !s.isPreServiced)) {
            if (s.Name === "Catering") {
                const cr = window.lastTelemetry.cateringRations !== undefined ? window.lastTelemetry.cateringRations : 0;
                const cColor = cr <= 10 ? '#EF4444' : (cr <= 25 ? '#F59E0B' : '#34D399');
                const cBg = cr <= 10 ? 'bg-red-500/10' : (cr <= 25 ? 'bg-amber-500/10' : 'bg-emerald-500/10');
                extraBadgesHtml += `<div class="px-2 py-[2px] rounded ${cBg} border text-[9px] uppercase font-bold tracking-widest leading-none flex items-center shadow-[0_0_10px_rgba(0,0,0,0.5)]" style="color: ${cColor}; border-color: ${cColor}40;">🍱 ${cr} Units</div>`;
            }
            if (s.Name === "Cleanliness" || s.Name === "Cleaning") {
                const cl = window.lastTelemetry.cabinCleanliness !== undefined ? window.lastTelemetry.cabinCleanliness : 100;
                const clColor = cl < 50 ? '#EF4444' : (cl < 75 ? '#F59E0B' : '#34D399');
                const clBg = cl < 50 ? 'bg-red-500/10' : (cl < 75 ? 'bg-amber-500/10' : 'bg-emerald-500/10');
                extraBadgesHtml += `<div class="px-2 py-[2px] rounded ${clBg} border text-[9px] uppercase font-bold tracking-widest leading-none flex items-center shadow-[0_0_10px_rgba(0,0,0,0.5)]" style="color: ${clColor}; border-color: ${clColor}40;">✨ ${Math.round(cl)}% Clean</div>`;
            }
            if (s.Name === "Water/Waste") {
                const wl = window.lastTelemetry.waterLevel !== undefined ? window.lastTelemetry.waterLevel : 100;
                const wasl = window.lastTelemetry.wasteLevel !== undefined ? window.lastTelemetry.wasteLevel : 0;
                const wColor = wl < 20 ? '#EF4444' : (wl < 50 ? '#F59E0B' : '#60A5FA');
                const wBg = wl < 20 ? 'bg-red-500/10' : (wl < 50 ? 'bg-amber-500/10' : 'bg-blue-500/10');
                const waColor = wasl > 90 ? '#EF4444' : (wasl > 70 ? '#F59E0B' : '#60A5FA');
                const waBg = wasl > 90 ? 'bg-red-500/10' : (wasl > 70 ? 'bg-amber-500/10' : 'bg-blue-500/10');
                extraBadgesHtml += `<div class="px-2 py-[2px] rounded ${wBg} border text-[9px] uppercase font-bold tracking-widest leading-none mr-2 flex items-center shadow-[0_0_10px_rgba(0,0,0,0.5)]" style="color: ${wColor}; border-color: ${wColor}40;">💧 ${Math.round(wl)}%</div>`;
                extraBadgesHtml += `<div class="px-2 py-[2px] rounded ${waBg} border text-[9px] uppercase font-bold tracking-widest leading-none flex items-center shadow-[0_0_10px_rgba(0,0,0,0.5)]" style="color: ${waColor}; border-color: ${waColor}40;">🗑️ ${Math.round(wasl)}%</div>`;
            }
        }

        html += `
            <div class="go-accordion bg-[#12141A] rounded-xl border border-white/5 overflow-hidden flex flex-col h-full ${opacityClass}">
                <div class="go-acc-header p-4 cursor-pointer hover:bg-white/[0.02] flex justify-between items-center transition-colors border-b border-transparent" onclick="toggleAccordion('${s.Name}')">
                    <div class="go-acc-title flex flex-col gap-2">
                        <div class="flex items-center gap-2">
                            <span class="go-icon text-lg flex items-center">${icon}</span>
                            <strong style="color: ${titleColor};" class="font-label tracking-[0.4em] uppercase text-xs">${locName}</strong>
                        </div>
                        ${extraBadgesHtml ? `<div class="flex items-center mt-1">${extraBadgesHtml}</div>` : ''}
                    </div>
                    <div class="go-acc-summary flex items-center gap-3">
                        <span style="color: ${statusColor}; font-weight: 600;" class="text-[10px] uppercase tracking-widest">${locStatus} ${timeDisplay}</span>
                        <svg id="acc-icon-${safeName}" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="#94A3B8" stroke-width="2" style="transform: ${chevronRot}; transition: transform 0.2s;">
                            <polyline points="6 9 12 15 18 9"></polyline>
                        </svg>
                    </div>
                </div>
                ${(() => {
                if (s.Name === "Water/Waste") {
                    let waterLvl = s.State === 1 ? s.ProgressPercent : Math.round(window.lastTelemetry?.waterLevel || 100);
                    let wasteLvl = s.State === 1 ? s.ProgressPercent : Math.round(window.lastTelemetry?.wasteLevel || 0);
                    let wColor = waterLvl < 20 ? '#EF4444' : (waterLvl < 50 ? '#F59E0B' : '#60A5FA');
                    let waColor = wasteLvl > 90 ? '#EF4444' : (wasteLvl > 70 ? '#F59E0B' : '#60A5FA');
                    if (s.IsPreServiced || s.isPreServiced) { wColor = '#475569'; waColor = '#475569'; }
                    return `
                        <div class="w-full flex flex-col gap-[1px] bg-black/40">
                            <div class="go-acc-bar w-full h-1">
                                <div class="go-bar-fill h-full transition-all duration-1000 ease-out" style="width: ${waterLvl}%; background-color: ${wColor};"></div>
                            </div>
                            <div class="go-acc-bar w-full h-1">
                                <div class="go-bar-fill h-full transition-all duration-1000 ease-out" style="width: ${wasteLvl}%; background-color: ${waColor};"></div>
                            </div>
                        </div>`;
                } else {
                    let mappedProgress = s.ProgressPercent;
                    let mappedColor = barColor;
                    if (s.State !== 1 && window.lastTelemetry && !(s.IsPreServiced || s.isPreServiced)) {
                        if (s.Name === "Catering") {
                            mappedProgress = window.lastTelemetry.cateringCompletion !== undefined ? window.lastTelemetry.cateringCompletion : 100;
                            mappedColor = mappedProgress < 20 ? '#EF4444' : (mappedProgress < 50 ? '#F59E0B' : '#34D399');
                        } else if (s.Name === "Cleanliness" || s.Name === "Cleaning") {
                            mappedProgress = window.lastTelemetry.cabinCleanliness !== undefined ? window.lastTelemetry.cabinCleanliness : 100;
                            mappedColor = mappedProgress < 50 ? '#EF4444' : (mappedProgress < 75 ? '#F59E0B' : '#34D399');
                        }
                    }
                    return `<div class="go-acc-bar w-full h-1 bg-black/40">
                                    <div class="go-bar-fill h-full transition-all duration-1000 ease-out" style="width: ${mappedProgress}%; background-color: ${mappedColor};"></div>
                                </div>`;
                }
            })()}
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
window.skipService = function (name) {
    window.currentAbortService = name;
    document.getElementById('abortServiceName').innerText = name;
    document.getElementById('abortServiceModal').style.display = 'flex';
};

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

    if (existingMap && expectedPaxCount === manifest.Passengers.length) {
        let boardedCount = 0;
        let fastenedCount = 0;
        let injuredCount = 0;

        manifest.Passengers.forEach(p => {
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
            headerLabel.innerText = `LIST (${boardedCount} / ${manifest.Passengers.length} PAX)`;
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

    container.dataset.flightPaxCount = manifest.Passengers.length;

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
                height: 26px;
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
            let p = manifest.Passengers.find(x => x.Seat === sId);
            if (p) {
                if (p.IsBoarded !== false) {
                    const seatClass = p.IsSeatbeltFastened ? 'fastened' : 'unfastened';
                    const injuryIcon = p.IsInjured ? '<span class="material-symbols-outlined text-[10px] text-red-500 absolute -top-1 -left-1 animate-pulse" style="z-index:10;">medical_services</span>' : '';
                    seatMapHtml += `<div id="seat-${sId}" class="seat ${seatClass} relative" data-initialized="true">
                        ${injuryIcon}
                        <span class="tooltip">${p.Seat} : ${p.Name} (${p.Nationality})</span>
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
                let p = manifest.Passengers.find(x => x.Seat === sId);
                if (p) {
                    if (p.IsBoarded !== false) {
                        const seatClass = p.IsSeatbeltFastened ? 'fastened' : 'unfastened';
                        const injuryIcon = p.IsInjured ? '<span class="material-symbols-outlined text-[10px] text-red-500 absolute -top-1 -left-1 animate-pulse" style="z-index:10;">medical_services</span>' : '';
                        seatMapHtml += `<div id="seat-${sId}" class="seat ${seatClass} relative" data-initialized="true">
                            ${injuryIcon}
                            <span class="tooltip">${p.Seat} : ${p.Name} (${p.Nationality})</span>
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
            let p = manifest.Passengers.find(x => x.Seat === sId);
            if (p) {
                if (p.IsBoarded !== false) {
                    const seatClass = p.IsSeatbeltFastened ? 'fastened' : 'unfastened';
                    const injuryIcon = p.IsInjured ? '<span class="material-symbols-outlined text-[10px] text-red-500 absolute -top-1 -left-1 animate-pulse" style="z-index:10;">medical_services</span>' : '';
                    seatMapHtml += `<div id="seat-${sId}" class="seat ${seatClass} relative" data-initialized="true">
                        ${injuryIcon}
                        <span class="tooltip">${p.Seat} : ${p.Name} (${p.Nationality})</span>
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

    let boardedInitialCount = manifest.Passengers.filter(p => p.IsBoarded !== false).length;

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

    let fastenedCount = manifest.Passengers.filter(p => p.IsBoarded !== false && p.IsSeatbeltFastened).length;
    let unfastenedCount = boardedInitialCount - fastenedCount;
    let injuredCount = manifest.Passengers.filter(p => p.IsBoarded !== false && p.IsInjured).length;
    let fallbackAircraftSeats = container.dataset.totalSeats ? parseInt(container.dataset.totalSeats) : manifest.Passengers.length;
    let emptyCount = fallbackAircraftSeats - boardedInitialCount;

    html += `       </ul>
                    <div class="border-b border-white/5 pb-3 mb-4" style="display:flex; justify-content:space-between; align-items:flex-end;">
                        <h3 id="paxListHeader" class="text-xs font-label tracking-[0.4em] text-sky-400 uppercase opacity-80" style="margin:0;">${paxListLabel} (${boardedInitialCount} / ${manifest.Passengers.length} PAX)</h3>
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

    manifest.Passengers.forEach(p => {
        if (p.IsBoarded !== false) {
            html += `
                <tr style="border-bottom: 1px solid rgba(51, 65, 85, 0.4);">
                    <td style="padding: 3px 4px; color: #38BDF8; font-weight: bold;">${p.Seat}</td>
                    <td style="padding: 3px 4px;">${p.Name}</td>
                    <td style="padding: 3px 4px;">${p.Nationality}</td>
                    <td style="padding: 3px 4px; text-align: center;">${p.Age}</td>
                </tr>
            `;
        }
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
        grid.innerHTML = `<div class="col-span-1 md:col-span-2 lg:col-span-3 text-center py-12 text-slate-500 font-label tracking-widest text-xs uppercase" data-i18n="logb_empty">No flight logs recorded yet.</div>`;
        return;
    }

    grid.innerHTML = history.map((f, i) => {
        const isSuper = f.Score >= 500;
        const colorPill = isSuper ? 'bg-emerald-500 shadow-[0_0_10px_rgba(16,185,129,0.5)]' : 'bg-red-500 shadow-[0_0_10px_rgba(239,68,68,0.5)]';
        const dateStr = new Date(f.FlightDate).toLocaleDateString([], { month: 'short', day: '2-digit', year: 'numeric', hour: '2-digit', minute: '2-digit' });
        const blkFormat = f.BlockTime > 0 ? `${Math.floor(f.BlockTime / 60)}h ${f.BlockTime % 60}m` : '0m';

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
