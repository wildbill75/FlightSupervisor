const fs = require('fs');
let indexHtml = fs.readFileSync('wwwroot/index.html', 'utf8');
let backupHtml = fs.readFileSync('briefing_backup.html', 'utf8');
const targetRegex = /<div id="briefing-content" class="hidden">[\s\S]*?<!-- Contenu de la Loading Sheet \(Future étape\) -->[\s\S]*?<\/div>/;

let paxHtml = `                                <div class="flex justify-between items-center bg-black/30 p-3 rounded-lg border border-white/5">
                                    <span class="text-[10px] text-[#7b7b7b] uppercase tracking-widest font-bold">PAX Count</span>
                                    <div><span id="loadPaxField" class="text-sm font-mono text-white font-bold">---</span><span class="text-[9px] text-[#7b7b7b] ml-1 font-bold">PAX</span></div>
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
                                    <span class="text-[10px] text-[#7b7b7b] uppercase tracking-widest font-bold">Est. Take-Off Weight</span>
                                    <div><span id="loadTowField" class="text-sm font-mono text-emerald-400 font-bold">---</span><span class="fuelUnitLabel text-[9px] text-emerald-400/50 ml-1 font-bold">KG</span></div>
                                </div>
                                <div class="flex justify-between items-center bg-black/30 p-3 rounded-lg border border-white/5 mb-4">
                                    <span class="text-[10px] text-[#7b7b7b] uppercase tracking-widest font-bold">Est. Landing Wt.</span>
                                    <div><span id="loadLdwField" class="text-sm font-mono text-sky-400 font-bold">---</span><span class="fuelUnitLabel text-[9px] text-sky-400/50 ml-1 font-bold">KG</span></div>
                                </div>
`;

// Insert pax before Trip Fuel
const tripRegex = /<div class="flex justify-between items-center bg-black\/30 p-3 rounded-lg border border-white\/5">\s*<span class="text-\[10px\] text-\[#7b7b7b\] uppercase tracking-widest font-bold">Trip Fuel<\/span>/;
backupHtml = backupHtml.replace(tripRegex, paxHtml + '$&');

// Insert weights before Block Fuel
const blockRegex = /<div class="mt-8 pt-6 border-t border-white\/10 flex justify-between items-end">\s*<span class="text-xs text-white uppercase tracking-widest font-black">Block Fuel<\/span>/;
backupHtml = backupHtml.replace(blockRegex, weightsHtml + '$&');

if (!indexHtml.match(targetRegex)) {
    console.error("FAILED to find target regex in index!");
} else {
    indexHtml = indexHtml.replace(targetRegex, backupHtml);
    fs.writeFileSync('wwwroot/index.html', indexHtml);
    console.log("Briefing successfully restored and updated!");
}
