const fs = require('fs');
let content = fs.readFileSync('wwwroot/app.js', 'utf8');

const targetNextLeg = `                if(dispatchModal) {
                    dispatchModal.style.display = 'flex';
                    const container = document.getElementById('simbriefHtmlContainer');
                    if(container) container.style.display = 'none';
                    const loader = document.getElementById('simbriefLoadingState');
                    if(loader) loader.style.display = 'flex';
                    
                    const btnValidate = document.getElementById('btnValidateLeg');
                    if(btnValidate) {
                        btnValidate.disabled = true;
                        btnValidate.classList.add('opacity-50', 'cursor-not-allowed');
                    }
                    window.currentLegCounter++;
                    const lblVal = document.getElementById('lblValidateLeg');
                    if(lblVal) lblVal.innerText = 'VALIDATE LEG ' + window.currentLegCounter;
                }
                
                window.chrome.webview.postMessage({
                    action: 'fetch',
                    username: username,
                    remember: true,
                    weatherSource: 'simbrief',
                    groundSpeed: localStorage.getItem('groundSpeed') || 'Realistic',
                    groundProb: localStorage.getItem('groundProb') || '25',
                    firstFlightClean: false,
                    units: {
                        weight: localStorage.getItem('selUnitWeight') || 'LBS',
                        temp: localStorage.getItem('selUnitTemp') || 'C',
                        alt: localStorage.getItem('selUnitAlt') || 'FT',
                        speed: localStorage.getItem('selUnitSpeed') || 'KTS',
                        press: localStorage.getItem('selUnitPress') || 'HPA',
                        time: localStorage.getItem('selTimeFormat') || '24H'
                    }
                });`;

const replaceNextLeg = `                if(dispatchModal) {
                    dispatchModal.style.display = 'flex';
                    const iframe = document.getElementById('simbriefIframe');
                    const loader = document.getElementById('simbriefLoadingState');
                    if (iframe && loader) {
                        loader.style.display = 'flex';
                        document.getElementById('simbriefLoadingLabel').innerText = "Loading Simbrief Interface...";
                        
                        iframe.onload = () => {
                            loader.style.display = 'none';
                            iframe.style.display = 'block';
                            iframe.classList.remove('hidden');
                        };
                        iframe.src = 'https://dispatch.simbrief.com/options/custom';
                    }
                    
                    const btnValidate = document.getElementById('btnValidateLeg');
                    if(btnValidate) {
                        btnValidate.disabled = false;
                        btnValidate.classList.remove('opacity-50', 'cursor-not-allowed');
                        window.currentLegCounter++;
                        document.getElementById('lblValidateLeg').innerText = 'VALIDATE LEG ' + window.currentLegCounter;
                        btnValidate.dataset.username = username;
                        btnValidate.dataset.pristine = "false";
                    }
                }`;

const targetBtnNext = `            const dispatchModal = document.getElementById('simbriefDispatchModal');
            if(dispatchModal) {
                dispatchModal.style.display = 'flex';
                const container = document.getElementById('simbriefHtmlContainer');
                if(container) container.style.display = 'none';
                const loader = document.getElementById('simbriefLoadingState');
                if(loader) loader.style.display = 'flex';
                
                const btnValidate = document.getElementById('btnValidateLeg');
                if(btnValidate) {
                    btnValidate.disabled = true;
                    btnValidate.classList.add('opacity-50', 'cursor-not-allowed');
                }
                const lblVal = document.getElementById('lblValidateLeg');
                if(lblVal) lblVal.innerText = 'VALIDATE LEG ' + window.currentLegCounter;
            }
            
            window.chrome.webview.postMessage({
                action: 'fetch',
                username: username,
                remember: true,
                weatherSource: weatherSrc,
                groundSpeed: groundSpeed,
                groundProb: groundProb,
                firstFlightClean: ffClean,
                units: {
                    weight: localStorage.getItem('selUnitWeight') || 'LBS',
                    temp: localStorage.getItem('selUnitTemp') || 'C',
                    alt: localStorage.getItem('selUnitAlt') || 'FT',
                    speed: localStorage.getItem('selUnitSpeed') || 'KTS',
                    press: localStorage.getItem('selUnitPress') || 'HPA',
                    time: localStorage.getItem('selTimeFormat') || '24H'
                }
            });`;

const replaceBtnNext = `            const dispatchModal = document.getElementById('simbriefDispatchModal');
            if(dispatchModal) {
                dispatchModal.style.display = 'flex';
                const iframe = document.getElementById('simbriefIframe');
                const loader = document.getElementById('simbriefLoadingState');
                if (iframe && loader) {
                    loader.style.display = 'flex';
                    document.getElementById('simbriefLoadingLabel').innerText = "Loading Simbrief Interface...";
                    
                    iframe.onload = () => {
                        loader.style.display = 'none';
                        iframe.style.display = 'block';
                        iframe.classList.remove('hidden');
                    };
                    iframe.src = 'https://dispatch.simbrief.com/options/custom';
                }
                
                const btnValidate = document.getElementById('btnValidateLeg');
                if(btnValidate) {
                    btnValidate.disabled = false;
                    btnValidate.classList.remove('opacity-50', 'cursor-not-allowed');
                    document.getElementById('lblValidateLeg').innerText = 'VALIDATE LEG ' + window.currentLegCounter;
                    btnValidate.dataset.username = username;
                    btnValidate.dataset.pristine = ffClean;
                }
            }`;

const targetBtnValidate = `    const btnValidateLeg = document.getElementById('btnValidateLeg');
    if(btnValidateLeg) {
        btnValidateLeg.addEventListener('click', () => {
            document.getElementById('simbriefDispatchModal').style.display = 'none';
            // Start ops logic can be invoked manually by the user later via "Start Ops" button on the UI.
        });
    }`;

const replaceBtnValidate = `    const btnValidateLeg = document.getElementById('btnValidateLeg');
    if(btnValidateLeg) {
        btnValidateLeg.addEventListener('click', () => {
            const username = btnValidateLeg.dataset.username;
            const ffClean = btnValidateLeg.dataset.pristine === "true";
            
            // Re-disable everything, show loader
            btnValidateLeg.disabled = true;
            btnValidateLeg.classList.add('opacity-50', 'cursor-not-allowed');
            document.getElementById('lblValidateLeg').innerText = 'FETCHING OFP...';
            
            const iframe = document.getElementById('simbriefIframe');
            if (iframe) {
                iframe.style.display = 'none';
                iframe.classList.add('hidden');
            }
            const loader = document.getElementById('simbriefLoadingState');
            if (loader) {
                loader.style.display = 'flex';
                document.getElementById('simbriefLoadingLabel').innerText = 'Downloading OFP into Application...';
            }
            
            window.chrome.webview.postMessage({
                action: 'fetch',
                username: username,
                remember: true,
                weatherSource: 'simbrief',
                groundSpeed: localStorage.getItem('groundSpeed') || 'Realistic',
                groundProb: localStorage.getItem('groundProb') || '25',
                firstFlightClean: ffClean,
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
    }`;

const targetFlightData = `                const dispatchModal = document.getElementById('simbriefDispatchModal');
                if (dispatchModal && dispatchModal.style.display !== 'none') {
                    const container = document.getElementById('simbriefHtmlContainer');
                    const loader = document.getElementById('simbriefLoadingState');
                    if (container && loader) {
                        loader.style.display = 'none';
                        container.style.display = 'block';
                        container.classList.remove('hidden');
                        
                        // Inject OFP HTML
                        if (d && d.text && d.text.plan_html) {
                            container.innerHTML = d.text.plan_html;
                        } else {
                            container.innerHTML = '<br><strong class="text-red-400">No HTML OFP Available / Parse Error</strong>';
                        }
                        
                        const btnValidate = document.getElementById('btnValidateLeg');
                        if (btnValidate) {
                            btnValidate.disabled = false;
                            btnValidate.classList.remove('opacity-50', 'cursor-not-allowed');
                        }
                    }
                }`;

const replaceFlightData = `                const dispatchModal = document.getElementById('simbriefDispatchModal');
                if (dispatchModal && dispatchModal.style.display !== 'none') {
                    // Flight successfully fetched, we can close the dispatch modal automatically!
                    dispatchModal.style.display = 'none';
                    const iframe = document.getElementById('simbriefIframe');
                    if (iframe) iframe.src = 'about:blank'; // Clear iframe memory
                    
                    const loader = document.getElementById('simbriefLoadingState');
                    if (loader) {
                        loader.style.display = 'none';
                    }
                    const btnValidate = document.getElementById('btnValidateLeg');
                    if (btnValidate) {
                        btnValidate.disabled = false;
                        btnValidate.classList.remove('opacity-50', 'cursor-not-allowed');
                        document.getElementById('lblValidateLeg').innerText = 'VALIDATE LEG X';
                    }
                }`;

content = content.split('\r\n').join('\n');
const items = [
    { name: 'targetNextLeg', t: targetNextLeg, r: replaceNextLeg },
    { name: 'targetBtnNext', t: targetBtnNext, r: replaceBtnNext },
    { name: 'targetBtnValidate', t: targetBtnValidate, r: replaceBtnValidate },
    { name: 'targetFlightData', t: targetFlightData, r: replaceFlightData }
];

let failed = false;
for (const item of items) {
    let nt = item.t.split('\r\n').join('\n');
    if (content.indexOf(nt) !== -1) {
        content = content.replace(nt, item.r.split('\r\n').join('\n'));
        console.log("Replaced:", item.name);
    } else {
        console.log("Failed to find:", item.name);
        failed = true;
    }
}

if (!failed) {
    fs.writeFileSync('wwwroot/app.js', content, 'utf8');
} else {
    fs.writeFileSync('wwwroot/app_test.js', content, 'utf8');
}
