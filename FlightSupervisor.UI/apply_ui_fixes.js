const fs = require('fs');

const indexHtmlPath = 'wwwroot/index.html';
const backupHtmlPath = 'briefing_backup.html';
const modalHtmlPath = 'modal.html';

let indexHtml = fs.readFileSync(indexHtmlPath, 'utf8');

// Read briefing backup as UTF16-LE
let backupBuffer = fs.readFileSync(backupHtmlPath);
let backupHtml = backupBuffer.toString('utf16le');
if (backupHtml.charCodeAt(0) === 0xFEFF) {
    backupHtml = backupHtml.slice(1);
}

let modalHtml = fs.readFileSync(modalHtmlPath, 'utf8');

let paxHtml = `                                <div class="flex justify-between items-center bg-black/30 p-3 rounded-lg border border-white/5">
                                    <span class="text-[10px] text-[#7b7b7b] uppercase tracking-widest font-bold">PAX Count</span>
                                    <div><span id="loadPaxField" class="text-sm font-mono text-white font-bold">---</span><span class="fuelUnitLabel text-[9px] text-[#7b7b7b] ml-1 font-bold">PAX</span></div>
                                </div>
                                <div class="flex justify-between items-center bg-black/30 p-3 rounded-lg border border-white/5 mb-4">
                                    <span class="text-[10px] text-[#7b7b7b] uppercase tracking-widest font-bold">Payload</span>
                                    <div><span id="loadPayloadField" class="text-sm font-mono text-white font-bold">---</span><span class="fuelUnitLabel text-[9px] text-[#7b7b7b] ml-1 font-bold">KG</span></div>
                                </div>
                                <div class="border-t border-white/10 my-2 mt-4"></div>
`;

let weightsHtml = `                                <div class="border-t border-white/10 my-4"></div>
                                <div class="flex justify-between items-center bg-black/30 p-3 rounded-lg border border-white/5 mt-4">
                                    <span class="text-[10px] text-[#7b7b7b] uppercase tracking-widest font-bold">Est. ZFW</span>
                                    <div><span id="loadZfwField" class="text-sm font-mono text-white font-bold">---</span><span class="fuelUnitLabel text-[9px] text-[#7b7b7b] ml-1 font-bold">KG</span></div>
                                </div>
                                <div class="flex justify-between items-center bg-black/30 p-3 rounded-lg border border-white/5">
                                    <span class="text-[10px] text-[#7b7b7b] uppercase tracking-widest font-bold">Est. Take-Off Weight (TOW)</span>
                                    <div><span id="loadTowField" class="text-sm font-mono text-emerald-400 font-bold">---</span><span class="fuelUnitLabel text-[9px] text-emerald-400/50 ml-1 font-bold">KG</span></div>
                                </div>
                                <div class="flex justify-between items-center bg-black/30 p-3 rounded-lg border border-white/5 mb-4">
                                    <span class="text-[10px] text-[#7b7b7b] uppercase tracking-widest font-bold">Est. Landing Wt. (LDW)</span>
                                    <div><span id="loadLdwField" class="text-sm font-mono text-sky-400 font-bold">---</span><span class="fuelUnitLabel text-[9px] text-sky-400/50 ml-1 font-bold">KG</span></div>
                                </div>
`;

// Insert pax before Trip Fuel
const tripRegex = /<div class="flex justify-between items-center bg-black\/30 p-3 rounded-lg border border-white\/5">(\r\n|\n|\s)*<span class="text-\[10px\] text-\[#7b7b7b\] uppercase tracking-widest font-bold">\s*Trip Fuel<\/span>/;
backupHtml = backupHtml.replace(tripRegex, paxHtml + '$&');

// Insert weights before Block Fuel
const blockRegex = /<div class="mt-8 pt-6 border-t border-white\/10 flex justify-between items-end">(\r\n|\n|\s)*<span class="text-xs text-white uppercase tracking-widest font-black">\s*Block Fuel<\/span>/;
backupHtml = backupHtml.replace(blockRegex, weightsHtml + '$&');

// Find where briefing-content is directly with string indexOf and replace the block
const startIndex = indexHtml.indexOf('<div id="briefing-content" class="hidden">');
const nextSection = indexHtml.indexOf('</section>', startIndex);

if (startIndex > -1 && nextSection > -1) {
    indexHtml = indexHtml.substring(0, startIndex) + backupHtml + "\n" + indexHtml.substring(nextSection);
    
    // Check if modal exists, if not append it before </body>
    if (!indexHtml.includes('id="airlineIdentityModal"')) {
        indexHtml = indexHtml.replace('</body>', '\n' + modalHtml + '\n</body>');
    }

    fs.writeFileSync('wwwroot/index.html', indexHtml, 'utf8');
    console.log("Briefing successfully restored and updated with modal and load sheet modifications!");
} else {
    console.error("FAILED to find target section in index!");
}
