document.addEventListener('DOMContentLoaded', () => {
    let elevenLabsVoices = [];
    let dialoguesData = [];

    const tableBody = document.getElementById('tableBody');
    const emptyState = document.getElementById('emptyState');
    const consoleOutput = document.getElementById('consoleOutput');
    const apiText = document.getElementById('apiText');
    const apiDot = document.getElementById('apiDot');

    // 1. Initial Load
    async function init() {
        log("Initialisation de Voice Studio...");
        await checkApiStatus();
        await fetchVoices();
        initMacroUI();
        await fetchDialogues();
    }

    // 2. Fetch API Status
    async function checkApiStatus() {
        try {
            const res = await fetch('/api/key_status');
            const data = await res.json();
            if (data.has_key) {
                apiText.textContent = "API Connectée";
                apiDot.classList.replace('bg-yellow-500', 'bg-green-500');
                if(!data.has_effects) log("[Avertissement] Pedalboard/Soundfile absents. Pas d'effets audio.", "text-yellow-400");
            } else {
                apiText.textContent = "Clé API Manquante";
                apiDot.classList.replace('bg-yellow-500', 'bg-red-500');
                log("ERREUR: Clé API ELEVENLABS_API_KEY introuvable dans l'environnement.", "text-red-400");
            }
        } catch(e) {
            log("Impossible de contacter le serveur local.", "text-red-400");
        }
    }

    // 3. Fetch Voices
    async function fetchVoices() {
        try {
            log("Récupération de vos voix ElevenLabs...");
            const res = await fetch('/api/voices');
            if(res.ok) {
                elevenLabsVoices = await res.json();
                log(`Trouvé ${elevenLabsVoices.length} voix.`);
            } else {
                log("Erreur lors de la récupération des voix.", "text-red-400");
            }
        } catch(e) {
            log("Erreur lors de la récupération des voix.", "text-red-400");
        }
    }

    // 4. Fetch Dialogues
    async function fetchDialogues() {
        try {
            const res = await fetch('/api/dialogues');
            dialoguesData = await res.json();
            renderTable();
            log("Fichier CSV chargé.");
        } catch(e) {
            log("Erreur de lecture du CSV.", "text-red-400");
        }
    }

    const fileNames = [
        "pa_welcome_intro",
        "airline_air_france",
        "pa_bound_for",
        "dest_toulouse_blagnac",
        "pa_welcome_luggage",
        "pa_welcome_seatbelts",
        "pa_safety_demo",
        "pa_descent_intro",
        "pa_descent_secure",
        "pa_arrival_welcome",
        "pa_arrival_time_is",
        "pa_arrival_remain_seated",
        "pa_turbulence_warning",
        "pa_service_start"
    ];

    const roles = ["PNC", "CAPTAIN"];
    const airlines = ["AIR_FRANCE", "GENERIC", "EASYJET", "RYANAIR", "LUFTHANSA"];
    const languages = ["EN", "FR", "ES", "DE", "GB", "US"];

    // 5. Render Table
    function renderTable() {
        tableBody.innerHTML = '';
        if (dialoguesData.length === 0) {
            emptyState.classList.remove('hidden');
        } else {
            emptyState.classList.add('hidden');
            dialoguesData.forEach((row, index) => {
                const tr = document.createElement('tr');
                tr.className = "hover:bg-slate-800/30 transition-colors animate-fade-in group whitespace-nowrap";
                tr.style.animationDelay = `${index * 0.05}s`;
                
                const safeRole = (row.Role || '').toUpperCase();
                const safeAirline = (row.Airline || '').toUpperCase();
                const safeLang = (row.Language || '').toUpperCase();

                tr.innerHTML = `
                    <td class="px-4 py-2 border-slate-800/50">
                        <select class="dynamic-input font-mono text-cyan-400 min-w-[150px]" data-idx="${index}" data-field="FileName">
                            ${fileNames.map(f => `<option value="${f}" ${f === row.FileName ? 'selected' : ''}>${f}</option>`).join('')}
                            ${row.FileName && !fileNames.includes(row.FileName) ? `<option value="${row.FileName}" selected>${row.FileName}</option>` : ''}
                        </select>
                    </td>
                    <td class="px-4 py-2 border-slate-800/50">
                        <select class="dynamic-input min-w-[100px]" data-idx="${index}" data-field="Role">
                            ${roles.map(r => `<option value="${r}" ${r === safeRole ? 'selected' : ''}>${r}</option>`).join('')}
                            ${safeRole && !roles.includes(safeRole) ? `<option value="${safeRole}" selected>${safeRole}</option>` : ''}
                        </select>
                    </td>
                    <td class="px-4 py-2 border-slate-800/50">
                        <select class="dynamic-input font-medium min-w-[150px]" data-idx="${index}" data-field="VoiceID">
                            ${elevenLabsVoices.map(v => `<option value="${v.voice_id}" ${v.voice_id === row.VoiceID ? 'selected' : ''}>${v.name}</option>`).join('')}
                            ${row.VoiceID && !elevenLabsVoices.some(v => v.voice_id === row.VoiceID) ? `<option value="${row.VoiceID}" selected>${row.VoiceName || 'ID Inconnu'}</option>` : ''}
                        </select>
                    </td>
                    <td class="px-4 py-2 border-slate-800/50">
                        <select class="dynamic-input min-w-[120px]" data-idx="${index}" data-field="Airline">
                            ${airlines.map(a => `<option value="${a}" ${a === safeAirline ? 'selected' : ''}>${a}</option>`).join('')}
                            ${safeAirline && !airlines.includes(safeAirline) ? `<option value="${safeAirline}" selected>${safeAirline}</option>` : ''}
                        </select>
                    </td>
                    <td class="px-4 py-2 border-slate-800/50">
                        <select class="dynamic-input font-bold min-w-[80px]" data-idx="${index}" data-field="Language">
                            ${languages.map(l => `<option value="${l}" ${l === safeLang ? 'selected' : ''}>${l}</option>`).join('')}
                            ${safeLang && !languages.includes(safeLang) ? `<option value="${safeLang}" selected>${safeLang}</option>` : ''}
                        </select>
                    </td>
                    <td class="px-4 py-2 border-slate-800/50">
                        <select class="dynamic-input min-w-[130px]" data-idx="${index}" data-field="Effect">
                            <option value="PA" ${row.Effect === 'PA' ? 'selected' : ''}>PA (Cabine)</option>
                            <option value="Intercom" ${row.Effect === 'Intercom' ? 'selected' : ''}>Intercom (Pilote)</option>
                            <option value="" ${!row.Effect ? 'selected' : ''}>None</option>
                        </select>
                    </td>
                    <td class="px-4 py-2 border-slate-800/50 min-w-[300px] whitespace-normal">
                        <textarea class="dynamic-input w-full h-auto bg-slate-900/50 border border-slate-700 rounded p-2 text-slate-200 resize-y min-h-[60px]" data-idx="${index}" data-field="Text" placeholder="Texte de l'annonce...">${row.Text.replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;').replace(/"/g, '&quot;')}</textarea>
                    </td>
                    <td class="px-4 py-2 border-slate-800/50 text-right opacity-0 group-hover:opacity-100 transition-opacity whitespace-nowrap">
                        <button class="text-cyan-400 hover:text-cyan-300 p-2 btn-generate-row" data-idx="${index}" title="Générer l'audio">
                            <i class="fa-solid fa-play"></i>
                        </button>
                        <button class="text-red-400 hover:text-red-300 p-2 btn-delete" data-idx="${index}" title="Supprimer">
                            <i class="fa-solid fa-trash"></i>
                        </button>
                    </td>
                `;
                tableBody.appendChild(tr);
            });

            // Bind events
            document.querySelectorAll('.dynamic-input').forEach(input => {
                input.addEventListener('change', (e) => {
                    const idx = e.target.getAttribute('data-idx');
                    const field = e.target.getAttribute('data-field');
                    dialoguesData[idx][field] = e.target.value;
                    
                    if(field === "VoiceID" && e.target.tagName === "SELECT") {
                        dialoguesData[idx]["VoiceName"] = e.target.options[e.target.selectedIndex].text;
                    }
                });
            });

            document.querySelectorAll('.btn-delete').forEach(btn => {
                btn.addEventListener('click', (e) => {
                    const idx = e.currentTarget.getAttribute('data-idx');
                    dialoguesData.splice(idx, 1);
                    renderTable();
                });
            });

            document.querySelectorAll('.btn-generate-row').forEach(btn => {
                btn.addEventListener('click', async (e) => {
                    const idx = e.currentTarget.getAttribute('data-idx');
                    const row = dialoguesData[idx];
                    
                    if (!row.FileName || !row.Text || !row.VoiceID) {
                        alert("Données incomplètes pour générer cette ligne.");
                        return;
                    }

                    const originalHtml = btn.innerHTML;
                    btn.innerHTML = `<i class="fa-solid fa-spinner fa-spin"></i>`;
                    btn.disabled = true;

                    log(`[Manuel] Génération: ${row.FileName} (Voice: ${row.VoiceName || row.VoiceID}) ...`);
                    
                    try {
                        const res = await fetch('/api/generate', {
                            method: 'POST',
                            headers: {'Content-Type': 'application/json'},
                            body: JSON.stringify(row)
                        });
                        
                        const data = await res.json();
                        if(res.ok && data.status === "success") {
                            log(`  -> Succès: ${data.message}`, "text-green-400");
                        } else if(res.ok && data.status === "skipped") {
                            log(`  -> Ignoré: ${data.message}`, "text-slate-400");
                        } else {
                            log(`  -> ERREUR: ${data.error}`, "text-red-400");
                            alert(`Erreur: ${data.error}`);
                        }
                    } catch(err) {
                        log(`  -> ERREUR RESEAU: ${err.message}`, "text-red-400");
                    } finally {
                        btn.innerHTML = originalHtml;
                        btn.disabled = false;
                    }
                });
            });
        }
    }

    // 6. Add Row
    document.getElementById('btnAddRow').addEventListener('click', () => {
        dialoguesData.push({
            FileName: `new_audio_${Date.now().toString().slice(-4)}`,
            Role: "",
            VoiceID: "",
            VoiceName: "",
            Airline: "Generic",
            Language: "EN",
            Effect: "",
            Text: ""
        });
        renderTable();
    });

    // 6b. Macros Variables (Modal Logic)
    const macroPack = document.getElementById('macroPack');
    const packsContainer = document.getElementById('packsContainer');
    const smartContainer = document.getElementById('smartContainer');
    const builderContainer = document.getElementById('builderContainer');
    const macroModeRadios = document.getElementsByName('macroMode');

    function updateMacroModeVisibility() {
        const selectedMode = document.querySelector('input[name="macroMode"]:checked').value;
        packsContainer.classList.add('hidden');
        smartContainer.classList.add('hidden');
        builderContainer.classList.add('hidden');

        if (selectedMode === 'packs') {
            packsContainer.classList.remove('hidden');
        } else if (selectedMode === 'smart') {
            smartContainer.classList.remove('hidden');
        } else if (selectedMode === 'builder') {
            builderContainer.classList.remove('hidden');
            
            // Populate dropdowns if empty
            const selAirline = document.getElementById('builderAirline');
            if(selAirline.options.length === 0) {
                Object.entries(defaultAirlines).forEach(([code, name]) => {
                    selAirline.add(new Option(name, code));
                });
            }
            const selDest = document.getElementById('builderDest');
            if(selDest.options.length === 0) {
                Object.entries(defaultAirports).forEach(([code, name]) => {
                    selDest.add(new Option(`${code} - ${name}`, code));
                });
            }

            // Load saved prefix
            const savedPrefix = localStorage.getItem('voiceStudio_builderPrefix') || 'pa_welcome';
            document.getElementById('builderPrefix').value = savedPrefix;
            
            updateBuilderFilename();
        }
    }

    function updateBuilderFilename() {
        const prefix = document.getElementById('builderPrefix').value.trim();
        const airlineCode = document.getElementById('builderAirline').value;
        const airlineName = defaultAirlines[airlineCode] || '';
        const destCode = document.getElementById('builderDest').value;
        const greeting = document.getElementById('builderGreeting').value;
        const aircraft = document.getElementById('builderAircraft').value;
        const text = document.getElementById('builderText').value;
        
        // Remove spaces for the filename (e.g. Air France -> AirFrance)
        const cleanAirline = airlineName.replace(/\s+/g, '');
        const cleanAircraft = aircraft.replace(/\s+/g, '');
        
        const usesAirline = text.includes('{airline}') || text.includes('{airlineName}');
        const usesDest = text.includes('{dest}') || text.includes('{destName}');
        const usesGreeting = text.includes('{greeting}');
        const usesAircraft = text.includes('{aircraft}') || text.includes('{aircraftType}');
        
        let filenameParts = [];
        if (prefix) filenameParts.push(prefix);
        if (greeting && usesGreeting) filenameParts.push(greeting.toUpperCase());
        if (cleanAircraft && usesAircraft && cleanAircraft.toLowerCase() !== "aircraft") filenameParts.push(cleanAircraft.toUpperCase());
        if (cleanAirline && usesAirline) filenameParts.push(cleanAirline.toUpperCase());
        if (destCode && usesDest) filenameParts.push(destCode.toUpperCase());
        
        document.getElementById('builderFilename').value = filenameParts.join('_');
        
        // Save prefix
        localStorage.setItem('voiceStudio_builderPrefix', prefix);
    }

    document.getElementById('builderPrefix').addEventListener('input', updateBuilderFilename);
    document.getElementById('builderAirline').addEventListener('change', updateBuilderFilename);
    document.getElementById('builderDest').addEventListener('change', updateBuilderFilename);
    document.getElementById('builderGreeting').addEventListener('change', updateBuilderFilename);
    document.getElementById('builderAircraft').addEventListener('change', updateBuilderFilename);
    document.getElementById('builderText').addEventListener('input', updateBuilderFilename);

    macroModeRadios.forEach(radio => radio.addEventListener('change', updateMacroModeVisibility));

    macroPack.addEventListener('change', () => {
        // Kept for future logic if needed
    });

    function initMacroUI() {
        const selectVoice = document.getElementById('macroVoice');
        if(selectVoice && elevenLabsVoices.length > 0) {
            selectVoice.innerHTML = elevenLabsVoices.map(v => `<option value="${v.voice_id}">${v.name}</option>`).join('');
        }
        updateMacroModeVisibility();
        updateBuilderLanguage();
    }
    
    // Call initMacroUI when voices are loaded (which is done in startUp logic around line 70, wait I need to make sure it's called after voices are fetched)

    document.getElementById('btnPreviewMacro').addEventListener('click', async () => {
        const btn = document.getElementById('btnPreviewMacro');
        const voiceId = document.getElementById('macroVoice').value;
        const effect = document.getElementById('macroEffect').value;
        const lang = document.getElementById('macroLang').value;
        
        const testText = lang === "FR" 
            ? "Ceci est un test audio pour vérifier la voix et l'effet." 
            : "This is an audio test to check the voice and the effect.";
            
        btn.disabled = true;
        btn.innerHTML = `<i class="fa-solid fa-spinner fa-spin"></i> Génération...`;
        
        try {
            const res = await fetch('/api/preview', {
                method: 'POST',
                headers: {'Content-Type': 'application/json'},
                body: JSON.stringify({
                    VoiceID: voiceId,
                    Text: testText,
                    Effect: effect
                })
            });
            
            if (res.ok) {
                const blob = await res.blob();
                const audioPlayer = document.getElementById('audioPlayer');
                audioPlayer.src = URL.createObjectURL(blob);
                audioPlayer.play();
            } else {
                const data = await res.json();
                alert("Erreur: " + data.error);
            }
        } catch(e) {
            alert("Erreur réseau: " + e.message);
        } finally {
            btn.disabled = false;
            btn.innerHTML = `<i class="fa-solid fa-headphones"></i> Tester l'audio`;
        }
    });

    // Default Dictionaries
    const smartTemplates = {
        "pa_welcome_intro_01": {
            "US": "Ladies and gentlemen, good {greeting} from the flightdeck. This is your Captain speaking. On behalf of {airlineName} I would like to welcome you all on board this {aircraftType} on our flight to {destName}.",
            "GB": "Ladies and gentlemen, good {greeting} from the flightdeck. This is your Captain speaking. On behalf of {airlineName} I would like to welcome you all on board this {aircraftType} on our flight to {destName}.",
            "AU": "Ladies and gentlemen, good {greeting} from the flightdeck. This is your Captain speaking. On behalf of {airlineName} I would like to welcome you all on board this {aircraftType} on our flight to {destName}.",
            "FR": "Mesdames et messieurs, {greeting}. Ici votre Commandant. Au nom de tout l'équipage, nous vous souhaitons la bienvenue à bord de ce {aircraftType} de {airlineName} à destination de {destName}."
        },
        "pa_welcome_flight_time": {
            "US": "Today's flight time will be approximately...",
            "GB": "Today's flight time will be approximately...",
            "AU": "Today's flight time will be approximately...",
            "FR": "Notre temps de vol aujourd'hui sera d'environ..."
        },
        "pa_welcome_arrival_weather_01": {
            "US": "The weather at our destination is excellent, with clear skies and very good visibility.",
            "GB": "The weather at our destination is excellent, with clear skies and very good visibility.",
            "AU": "The weather at our destination is excellent, with clear skies and very good visibility.",
            "FR": "La météo à notre destination est excellente, avec un ciel dégagé et une très bonne visibilité."
        },
        "pa_welcome_arrival_weather_02": {
            "US": "The weather at our destination is relatively good. Despite some clouds, visibility remains excellent.",
            "GB": "The weather at our destination is relatively good. Despite some clouds, visibility remains excellent.",
            "AU": "The weather at our destination is relatively good. Despite some clouds, visibility remains excellent.",
            "FR": "La météo à l'arrivée est plutôt bonne. Malgré un ciel nuageux, la visibilité reste excellente."
        },
        "pa_welcome_arrival_weather_03": {
            "US": "The conditions at our destination are quite poor today, with overcast skies, rain, and reduced visibility.",
            "GB": "The conditions at our destination are quite poor today, with overcast skies, rain, and reduced visibility.",
            "AU": "The conditions at our destination are quite poor today, with overcast skies, rain, and reduced visibility.",
            "FR": "Les conditions à l'arrivée sont assez moyennes, avec un ciel très couvert, de la pluie et une visibilité réduite."
        },
        "pa_welcome_arrival_weather_04": {
            "US": "The weather at our destination is quite stormy, we are expecting thunderstorms and heavy precipitation in the area.",
            "GB": "The weather at our destination is quite stormy, we are expecting thunderstorms and heavy precipitation in the area.",
            "AU": "The weather at our destination is quite stormy, we are expecting thunderstorms and heavy precipitation in the area.",
            "FR": "La météo à l'arrivée est très agitée. Nous prévoyons des orages et de fortes précipitations dans la zone de l'aéroport."
        },
        "pa_welcome_arrival_weather_05": {
            "US": "The weather at our destination is currently foggy, which is heavily restricting visibility.",
            "GB": "The weather at our destination is currently foggy, which is heavily restricting visibility.",
            "AU": "The weather at our destination is currently foggy, which is heavily restricting visibility.",
            "FR": "Les conditions à l'arrivée sont très brumeuses, avec un épais brouillard limitant fortement la visibilité."
        },
        "pa_welcome_arrival_weather_06": {
            "US": "Winter weather conditions await us at our destination, with snow and reduced visibility.",
            "GB": "Winter weather conditions await us at our destination, with snow and reduced visibility.",
            "AU": "Winter weather conditions await us at our destination, with snow and reduced visibility.",
            "FR": "Des conditions hivernales nous attendent à notre destination, avec des chutes de neige et une visibilité réduite."
        },
        "pa_welcome_enroute_weather_01": {
            "US": "Regarding our flight conditions, we are expecting a very smooth ride today.",
            "GB": "Regarding our flight conditions, we are expecting a very smooth ride today.",
            "AU": "Regarding our flight conditions, we are expecting a very smooth ride today.",
            "FR": "Concernant les conditions de vol, nous prévoyons un trajet très calme aujourd'hui."
        },
        "pa_welcome_enroute_weather_02": {
            "US": "We're expecting a generally smooth flight, with perhaps a few light bumps along the way.",
            "GB": "We're expecting a generally smooth flight, with perhaps a few light bumps along the way.",
            "AU": "We're expecting a generally smooth flight, with perhaps a few light bumps along the way.",
            "FR": "Nous prévoyons un vol globalement calme, malgré quelques légères turbulences possibles en route."
        },
        "pa_welcome_enroute_weather_03": {
            "US": "We're expecting a somewhat bumpy ride at times. Please keep your seatbelts fastened.",
            "GB": "We're expecting a somewhat bumpy ride at times. Please keep your seatbelts fastened.",
            "AU": "We're expecting a somewhat bumpy ride at times. Please keep your seatbelts fastened.",
            "FR": "Nous prévoyons un vol assez mouvementé par moments. Gardez bien votre ceinture attachée."
        },
        "pa_welcome_enroute_weather_04": {
            "US": "Some challenging weather conditions may force us to deviate around storms, please expect some turbulence.",
            "GB": "Some challenging weather conditions may force us to deviate around storms, please expect some turbulence.",
            "AU": "Some challenging weather conditions may force us to deviate around storms, please expect some turbulence.",
            "FR": "Des conditions météo complexes nous obligeront à contourner quelques orages, attendez-vous à quelques secousses."
        }
    };

    const greetingsTransl = {
        "US": { "morning": "Morning", "afternoon": "Afternoon", "evening": "Evening" },
        "GB": { "morning": "Morning", "afternoon": "Afternoon", "evening": "Evening" },
        "AU": { "morning": "Morning", "afternoon": "Afternoon", "evening": "Evening" },
        "FR": { "morning": "Bonjour", "evening": "Bonsoir" },
        "ES": { "morning": "mañana", "afternoon": "tarde", "evening": "noche" },
        "DE": { "morning": "Morgen", "afternoon": "Nachmittag", "evening": "Abend" }
    };

    function applyFrenchGrammar(text) {
        // Fix "de " + vowel -> "d'"
        text = text.replace(/\bde\s+([aeiouyAEIOUYéèêëàâäîïôöùûühH])/g, "d'$1");
        text = text.replace(/\bDe\s+([aeiouyAEIOUYéèêëàâäîïôöùûühH])/g, "D'$1");
        
        // Fix "ce " + vowel -> "cet "
        text = text.replace(/\bce\s+([aeiouyAEIOUYéèêëàâäîïôöùûühH])/g, "cet $1");
        text = text.replace(/\bCe\s+([aeiouyAEIOUYéèêëàâäîïôöùûühH])/g, "Cet $1");
        
        return text;
    }

    function updateBuilderLanguage() {
        const lang = document.getElementById('macroLang').value;
        const templateSelect = document.getElementById('builderTemplateSelect').value;
        const builderText = document.getElementById('builderText');
        const greetingSelect = document.getElementById('builderGreeting');
        
        // Update greeting options while keeping the current selection if possible
        const currentGreeting = greetingSelect.value;
        const transl = greetingsTransl[lang] || greetingsTransl["US"];
        
        greetingSelect.innerHTML = Object.entries(transl).map(([val, text]) => 
            `<option value="${val}" ${val === currentGreeting ? 'selected' : ''}>${text}</option>`
        ).join('');

        // Translate the template if one is selected
        if (templateSelect !== "custom" && smartTemplates[templateSelect]) {
            const translatedText = smartTemplates[templateSelect][lang] || smartTemplates[templateSelect]["US"];
            if (translatedText) {
                builderText.value = translatedText;
            }
        }
        
        updateBuilderFilename();
    }

    document.getElementById('builderTemplateSelect').addEventListener('change', (e) => {
        const templateId = e.target.value;
        if (templateId !== "custom") {
            document.getElementById('builderPrefix').value = templateId;
            updateBuilderLanguage();
            updateBuilderFilename();
        }
    });

    document.getElementById('macroLang').addEventListener('change', updateBuilderLanguage);


    const defaultAirports = {
        "LFPG": "Paris Charles de Gaulle",
        "LFPO": "Paris Orly",
        "LFMN": "Nice Côte d'Azur",
        "LFML": "Marseille Provence",
        "LFLL": "Lyon Saint-Exupéry",
        "LFBO": "Toulouse Blagnac",
        "LFBD": "Bordeaux Mérignac",
        "EGLL": "Londres Heathrow",
        "EGKK": "Londres Gatwick",
        "EHAM": "Amsterdam Schiphol",
        "EDDF": "Francfort",
        "EDDM": "Munich",
        "LEMD": "Madrid Barajas",
        "LEBL": "Barcelone El Prat",
        "LIRF": "Rome Fiumicino",
        "LIMC": "Milan Malpensa",
        "KJFK": "New York John F. Kennedy",
        "KLAX": "Los Angeles",
        "OMDB": "Dubaï"
    };

    const defaultAirlines = {
        "air_france": "Air France",
        "easyjet": "EasyJet",
        "ryanair": "Ryanair",
        "lufthansa": "Lufthansa",
        "british_airways": "British Airways",
        "klm": "KLM",
        "iberia": "Iberia",
        "emirates": "Emirates",
        "delta": "Delta Airlines",
        "american": "American Airlines"
    };

    function parseCustomData(text, fallback) {
        if(!text || !text.trim()) return fallback;
        const result = {};
        text.split('\n').forEach(line => {
            if(line.includes(':')) {
                const parts = line.split(':');
                const key = parts[0].trim().toLowerCase();
                const val = parts.slice(1).join(':').trim();
                if(key && val) result[key] = val;
            }
        });
        return Object.keys(result).length > 0 ? result : fallback;
    }

    document.getElementById('btnStartMacro').addEventListener('click', async () => {
        const pack = document.getElementById('macroPack').value;
        const voiceId = document.getElementById('macroVoice').value;
        const voiceName = elevenLabsVoices.find(v => v.voice_id === voiceId)?.name || "Unknown";
        const role = document.getElementById('macroRole').value;
        const airline = document.getElementById('macroAirline').value;
        const lang = document.getElementById('macroLang').value;
        const customText = ""; // Removed custom text feature to simplify UX
        const effect = document.getElementById('macroEffect').value;
        
        log(`====== GÉNÉRATION MACRO : ${pack.toUpperCase()} ======`, "text-indigo-400 font-bold");

        let itemsToGenerate = [];
        const mode = document.querySelector('input[name="macroMode"]:checked').value;

        if (mode === "packs") {
            if (pack === "hours") {
            for(let i = 0; i <= 23; i++) {
                let plural = i > 1 ? "s" : "";
                itemsToGenerate.push({ FileName: `hour_${i}`, Text: `${i} heure${plural}` });
            }
        } else if (pack === "minutes") {
            for(let i = 0; i <= 59; i++) {
                itemsToGenerate.push({ FileName: `num_${i}`, Text: `${i}` });
            }
        } else if (pack === "flight_times") {
            for(let h = 0; h <= 5; h++) {
                for(let m = 0; m < 60; m += 5) {
                    if (h === 0 && m < 30) continue; 
                    if (h === 5 && m > 0) continue; 
                    
                    let mStr = m === 0 ? "00" : (m < 10 ? "0" + m : m);
                    let filename = `pa_welcome_flight_time_${h}H${mStr}`;
                    
                    let text = "";
                    if (lang === "FR") {
                        if (h === 0) {
                            text = `Notre temps de vol aujourd'hui sera d'environ ${m} minutes.`;
                        } else {
                            let plural = h > 1 ? "s" : "";
                            text = `Notre temps de vol aujourd'hui sera d'environ ${h} heure${plural}`;
                            if (m > 0) text += ` et ${m} minutes.`;
                            else text += `.`;
                        }
                    } else { 
                        if (h === 0) {
                            text = `Today's flight time will be approximately ${m} minutes.`;
                        } else {
                            let plural = h > 1 ? "s" : "";
                            text = `Today's flight time will be approximately ${h} hour${plural}`;
                            if (m > 0) text += ` and ${m} minutes.`;
                            else text += `.`;
                        }
                    }
                    itemsToGenerate.push({ FileName: filename, Text: text });
                }
            }
        } else if (pack === "temp_c") {
            for(let i = -40; i <= 50; i++) {
                let prefix = i < 0 ? "m" : "";
                let val = Math.abs(i);
                itemsToGenerate.push({ FileName: `temp_c_${prefix}${val}`, Text: `${i} degrés Celsius` });
            }
        } else if (pack === "temp_f") {
            for(let i = 0; i <= 120; i++) {
                itemsToGenerate.push({ FileName: `temp_f_${i}`, Text: `${i} degrés Fahrenheit` });
            }
        } else if (pack === "altitudes") {
            for(let i = 1000; i <= 45000; i += 1000) {
                itemsToGenerate.push({ FileName: `alt_${i}`, Text: `${i} pieds` });
            }
        } else if (pack === "destinations") {
            const data = parseCustomData(customText, defaultAirports);
            for(const [code, name] of Object.entries(data)) {
                itemsToGenerate.push({ FileName: `dest_${code.toLowerCase()}`, Text: name });
            }
        } else if (pack === "departures") {
            const data = parseCustomData(customText, defaultAirports);
            for(const [code, name] of Object.entries(data)) {
                itemsToGenerate.push({ FileName: `dep_${code.toLowerCase()}`, Text: name });
            }
        } else if (pack === "airlines_names") {
            const data = parseCustomData(customText, defaultAirlines);
            for(const [code, name] of Object.entries(data)) {
                itemsToGenerate.push({ FileName: `airline_${code.toLowerCase()}`, Text: name });
            }
        }
        } else if (mode === "builder") {
            const airlineCode = document.getElementById('builderAirline').value;
            const airlineNameRaw = document.getElementById('builderAirline').options[document.getElementById('builderAirline').selectedIndex].text;
            const destCode = document.getElementById('builderDest').value;
            const destName = document.getElementById('builderDest').options[document.getElementById('builderDest').selectedIndex].text.split(' - ')[1];
            const greeting = document.getElementById('builderGreeting').options[document.getElementById('builderGreeting').selectedIndex].text;
            const aircraft = document.getElementById('builderAircraft').value;
            
            let text = document.getElementById('builderText').value;
            let filename = document.getElementById('builderFilename').value;
            
            if (!text) {
                alert("Veuillez entrer le texte de la phrase.");
                return;
            }

            // Replace simple tags if the user left them, or just rely on the user having typed the text
            // Support both standard {airline} and C# {airlineName} tags
            text = text.replace(/{airline}/g, airlineNameRaw)
                       .replace(/{airlineName}/g, airlineNameRaw)
                       .replace(/{dest}/g, destName)
                       .replace(/{destName}/g, destName)
                       .replace(/{greeting}/g, greeting)
                       .replace(/{aircraft}/g, aircraft)
                       .replace(/{aircraftType}/g, aircraft)
                       .replace(/{CaptainName}/g, "Captain"); // Default fallback for now

            if (lang === "FR") {
                text = applyFrenchGrammar(text);
            }

            itemsToGenerate.push({ FileName: filename, Text: text });
            
            log(`Génération Sur-Mesure : 1 fichier préparé.`, "text-cyan-400 font-bold");

        } else if (mode === "smart") {
            const template = document.getElementById('smartText').value;
            const prefix = document.getElementById('smartPrefix').value || "smart_macro";
            
            if (!template) {
                alert("Veuillez entrer un texte avec des variables (ex: {airline}).");
                return;
            }

            const airportsData = parseCustomData(customText, defaultAirports);
            const airlinesData = parseCustomData(customText, defaultAirlines);
            
            // Build permutations
            itemsToGenerate = [];
            
            const hasAirline = template.includes('{airline}');
            const hasDest = template.includes('{dest}');
            const hasDep = template.includes('{dep}');
            const hasGreeting = template.includes('{greeting}');
            const hasAircraft = template.includes('{aircraft}');

            const airlinesToIterate = hasAirline ? Object.entries(airlinesData) : [["", ""]];
            const destsToIterate = hasDest ? Object.entries(airportsData) : [["", ""]];
            const depsToIterate = hasDep ? Object.entries(airportsData) : [["", ""]];
            const greetingsToIterate = hasGreeting ? ["morning", "afternoon", "evening"] : [""];
            const aircraftsToIterate = hasAircraft ? ["A320", "Boeing 737", "aircraft"] : [""];

            for (const [alCode, alName] of airlinesToIterate) {
                for (const [destCode, destName] of destsToIterate) {
                    for (const [depCode, depName] of depsToIterate) {
                        for (const greeting of greetingsToIterate) {
                            for (const aircraft of aircraftsToIterate) {
                                let text = template;
                                let filename = prefix;
                                
                                if (hasAirline) {
                                    text = text.replace(/{airline}/g, alName);
                                    filename += `_${alCode.toUpperCase()}`;
                                }
                                if (hasDep) {
                                    text = text.replace(/{dep}/g, depName);
                                    filename += `_DEP_${depCode.toUpperCase()}`;
                                }
                                if (hasDest) {
                                    text = text.replace(/{dest}/g, destName);
                                    filename += `_DEST_${destCode.toUpperCase()}`;
                                }
                                if (hasGreeting) {
                                    const transl = greetingsTransl[lang] || greetingsTransl["EN"];
                                    const translatedGreeting = transl[greeting] || greeting;
                                    text = text.replace(/{greeting}/g, translatedGreeting);
                                    filename += `_${greeting.toUpperCase()}`;
                                }
                                if (hasAircraft) {
                                    text = text.replace(/{aircraft}/g, aircraft.replace(" ", ""));
                                    filename += `_${aircraft.replace(" ", "").toUpperCase()}`;
                                }

                                if (lang === "FR") {
                                    text = applyFrenchGrammar(text);
                                }

                                itemsToGenerate.push({ FileName: filename, Text: text });
                            }
                        }
                    }
                }
            }
            
            log(`Génération Smart Phrase : ${itemsToGenerate.length} combinaisons calculées.`, "text-cyan-400 font-bold");
        }

        log(`Création de ${itemsToGenerate.length} fichiers audio en arrière-plan...`);
        
        let success = 0;
        let errors = 0;

        for (let i = 0; i < itemsToGenerate.length; i++) {
            const item = itemsToGenerate[i];
            const payload = {
                FileName: item.FileName,
                Role: role,
                VoiceID: voiceId,
                VoiceName: voiceName,
                Airline: airline,
                Language: lang,
                Effect: effect,
                Text: item.Text
            };

            log(`[${i+1}/${itemsToGenerate.length}] Macro: ${item.FileName}...`);
            
            try {
                const res = await fetch('/api/generate', {
                    method: 'POST',
                    headers: {'Content-Type': 'application/json'},
                    body: JSON.stringify(payload)
                });
                
                const data = await res.json();
                if(res.ok && data.status === "success") {
                    log(`  -> Succès: ${data.message}`, "text-green-400");
                    success++;
                } else {
                    log(`  -> ERREUR: ${data.error || data.message}`, "text-red-400");
                    errors++;
                }
            } catch(e) {
                log(`  -> ERREUR RESEAU: ${e.message}`, "text-red-400");
                errors++;
            }
        }

        log(`====== FIN MACRO : ${success} créés, ${errors} erreurs ======`, "text-indigo-400 font-bold");
    });

    // 7. Save
    document.getElementById('btnSave').addEventListener('click', async () => {
        const btn = document.getElementById('btnSave');
        const icon = btn.querySelector('i');
        icon.className = "fa-solid fa-circle-notch fa-spin";
        
        try {
            const res = await fetch('/api/dialogues', {
                method: 'POST',
                headers: {'Content-Type': 'application/json'},
                body: JSON.stringify(dialoguesData)
            });
            if(res.ok) {
                log("Fichier CSV sauvegardé avec succès.", "text-green-400");
            } else {
                log("Erreur lors de la sauvegarde.", "text-red-400");
            }
        } catch(e) {
            log("Erreur de connexion.", "text-red-400");
        }
        
        icon.className = "fa-solid fa-floppy-disk";
    });



    // Logger
    function log(message, colorClass = "text-slate-300") {
        const line = document.createElement('div');
        line.className = `mb-1 ${colorClass}`;
        const time = new Date().toLocaleTimeString('fr-FR', {hour12: false});
        line.innerHTML = `<span class="text-slate-600 mr-2">[${time}]</span> ${message}`;
        consoleOutput.appendChild(line);
        consoleOutput.scrollTop = consoleOutput.scrollHeight;
    }

    document.getElementById('btnClearConsole').addEventListener('click', () => {
        consoleOutput.innerHTML = '';
    });

    // Start
    init();
});
