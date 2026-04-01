const fs = require('fs');
let content = fs.readFileSync('d:/FlightSupervisor/FlightSupervisor.UI/wwwroot/app.js', 'utf8');

// 1. Replace btnDispatchDuty with btnDutyNext logic
const target1 = `    const btnDispatchDuty = document.getElementById('btnDispatchDuty');
    if (btnDispatchDuty) {
        btnDispatchDuty.addEventListener('click', () => {
            const username = document.getElementById('dutySbUsername') ? document.getElementById('dutySbUsername').value : '';
            if (!username || username.trim() === '') {
                const ipt = document.getElementById('dutySbUsername');
                if (ipt) { ipt.classList.add('border-red-500'); setTimeout(() => ipt.classList.remove('border-red-500'), 1000); }
                return;
            }
            
            const ffClean = currentDutyState === 'pristine';
            localStorage.setItem('firstFlightClean', ffClean);
            localStorage.setItem('sbUsername', username);
            
            const groundSpeed = localStorage.getItem('groundSpeed') || 'Realistic';
            const groundProb = localStorage.getItem('groundProb') || '25';
            const weatherSrc = document.getElementById('dutyWeatherSource') ? document.getElementById('dutyWeatherSource').value : 'simbrief';
            
            document.getElementById('dutySetupModal').style.display = 'none';
            
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
            });
        });
    }`;

const replace1 = `    window.currentLegCounter = 1;

    const btnDutyNext = document.getElementById('btnDutyNext');
    if (btnDutyNext) {
        btnDutyNext.addEventListener('click', () => {
            let username = localStorage.getItem('sbUsername') || '';
            if (!username || username.trim() === '') {
                alert('Please configure your SimBrief ID/Username in the Settings tab first!');
                return;
            }
            
            const ffClean = currentDutyState === 'pristine';
            localStorage.setItem('firstFlightClean', ffClean);
            
            const groundSpeed = localStorage.getItem('groundSpeed') || 'Realistic';
            const groundProb = localStorage.getItem('groundProb') || '25';
            const weatherSrc = 'simbrief';
            
            document.getElementById('dutySetupModal').style.display = 'none';
            
            const dispatchModal = document.getElementById('simbriefDispatchModal');
            if(dispatchModal) {
                dispatchModal.style.display = 'flex';
                document.getElementById('simbriefHtmlContainer').style.display = 'none';
                document.getElementById('simbriefLoadingState').style.display = 'flex';
                const btnValidate = document.getElementById('btnValidateLeg');
                btnValidate.disabled = true;
                btnValidate.classList.add('opacity-50', 'cursor-not-allowed');
                document.getElementById('lblValidateLeg').innerText = 'VALIDATE LEG ' + window.currentLegCounter;
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
            });
        });
    }

    const btnCancelDispatch = document.getElementById('btnCancelDispatch');
    if(btnCancelDispatch) {
        btnCancelDispatch.addEventListener('click', () => {
            document.getElementById('simbriefDispatchModal').style.display = 'none';
        });
    }

    const btnValidateLeg = document.getElementById('btnValidateLeg');
    if(btnValidateLeg) {
        btnValidateLeg.addEventListener('click', () => {
            document.getElementById('simbriefDispatchModal').style.display = 'none';
            // Validation successful! The background UI is already built anyway.
        });
    }`;

// 2. We inject the OFP into the dispatch modal inside window.chrome.webview.addEventListener('message', event => ..., payload.type === 'manifestUpdate')
const target2 = `        if (payload.type === 'manifestUpdate') {
            console.log('Received manifestUpdate:', payload);
            window.manifestData = payload;`;

const replace2 = `        if (payload.type === 'manifestUpdate') {
            console.log('Received manifestUpdate:', payload);
            window.manifestData = payload;
            
            const dispatchModal = document.getElementById('simbriefDispatchModal');
            if (dispatchModal && dispatchModal.style.display !== 'none') {
                const container = document.getElementById('simbriefHtmlContainer');
                const loader = document.getElementById('simbriefLoadingState');
                if (container && loader) {
                    loader.style.display = 'none';
                    container.style.display = 'block';
                    container.classList.remove('hidden');
                    // Inject OFP HTML
                    if (payload.text && payload.text.plan_html) {
                        container.innerHTML = payload.text.plan_html;
                    } else {
                        container.innerHTML = '<br><strong>No HTML OFP Available / Parse Error</strong>';
                    }
                    
                    const btnValidate = document.getElementById('btnValidateLeg');
                    if (btnValidate) {
                        btnValidate.disabled = false;
                        btnValidate.classList.remove('opacity-50', 'cursor-not-allowed');
                    }
                }
            }`;

content = content.split('\r\n').join('\n');
let normTarget1 = target1.split('\r\n').join('\n');
let normTarget2 = target2.split('\r\n').join('\n');

if (content.includes(normTarget1) && content.includes(normTarget2)) {
    content = content.replace(normTarget1, replace1).replace(normTarget2, replace2);
    fs.writeFileSync('d:/FlightSupervisor/FlightSupervisor.UI/wwwroot/app.js', content, 'utf8');
    console.log("Success");
} else {
    console.log("Targets not found!");
}
