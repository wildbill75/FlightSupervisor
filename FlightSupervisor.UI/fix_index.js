const fs = require('fs');

const indexHtmlPath = 'wwwroot/index.html';
const backupHtmlPath = 'briefing_backup.html';

let indexHtml = fs.readFileSync(indexHtmlPath, 'utf8');
let backupHtml = fs.readFileSync(backupHtmlPath, 'utf8');

const targetRegex = /<div id="briefing-content" class="hidden">[\s\S]*?<!-- Contenu de la Loading Sheet \(Future étape\) -->[\s\S]*?<\/div>/;

if (!indexHtml.match(targetRegex)) {
    console.error("Target string not found in index.html!");
}

const tripFuelIndex = backupHtml.indexOf('<div class="flex justify-between items-center bg-black/30 p-3 rounded-lg border border-white/5">\r\n                                    <span class="text-[10px] text-[#7b7b7b] uppercase tracking-widest font-bold">Trip Fuel</span>');

let paxHtml = `                                <div class="flex justify-between items-center bg-black/30 p-3 rounded-lg border border-white/5">
                                    <span class="text-[10px] text-[#7b7b7b] uppercase tracking-widest font-bold">PAX Count</span>
                                    <div><span id="loadPaxField" class="text-sm font-mono text-emerald-400 font-bold">---</span><span class="fuelUnitLabel text-[9px] text-emerald-400/50 ml-1 font-bold">PAX</span></div>
                                </div>
                                <div class="flex justify-between items-center bg-black/30 p-3 rounded-lg border border-white/5 mb-4">
                                    <span class="text-[10px] text-[#7b7b7b] uppercase tracking-widest font-bold">Payload</span>
                                    <div><span id="loadPayloadField" class="text-sm font-mono text-emerald-400 font-bold">---</span><span class="fuelUnitLabel text-[9px] text-emerald-400/50 ml-1 font-bold">KG</span></div>
                                </div>
                                <div class="border-t border-white/10 my-2 mt-4"></div>
`;

if(tripFuelIndex > -1){
    backupHtml = backupHtml.slice(0, tripFuelIndex) + paxHtml + backupHtml.slice(tripFuelIndex);
} else {
    console.warn("Could not find Trip fuel. Looking for fallback.");
}

const blockFuelStr = `<div class="mt-8 pt-6 border-t border-white/10 flex justify-between items-end">
                                    <span class="text-xs text-white uppercase tracking-widest font-black">Block Fuel</span>`;
const blockFuelIndex = backupHtml.indexOf(blockFuelStr);

let weightsHtml = `                                <div class="border-t border-white/10 my-4"></div>
                                <div class="flex justify-between items-center bg-black/30 p-3 rounded-lg border border-white/5 mt-4">
                                    <span class="text-[10px] text-[#7b7b7b] uppercase tracking-widest font-bold">Est. ZFW</span>
                                    <div><span id="loadZfwField" class="text-sm font-mono text-white font-bold">---</span><span class="fuelUnitLabel text-[9px] text-white/50 ml-1 font-bold">KG</span></div>
                                </div>
                                <div class="flex justify-between items-center bg-black/30 p-3 rounded-lg border border-white/5">
                                    <span class="text-[10px] text-[#7b7b7b] uppercase tracking-widest font-bold">Est. Take-Off Weight (TOW)</span>
                                    <div><span id="loadTowField" class="text-sm font-mono text-white font-bold">---</span><span class="fuelUnitLabel text-[9px] text-white/50 ml-1 font-bold">KG</span></div>
                                </div>
                                <div class="flex justify-between items-center bg-black/30 p-3 rounded-lg border border-white/5 mb-4">
                                    <span class="text-[10px] text-[#7b7b7b] uppercase tracking-widest font-bold">Est. Landing Wt. (LDW)</span>
                                    <div><span id="loadLdwField" class="text-sm font-mono text-white font-bold">---</span><span class="fuelUnitLabel text-[9px] text-white/50 ml-1 font-bold">KG</span></div>
                                </div>
`;

if(blockFuelIndex > -1){
    backupHtml = backupHtml.slice(0, blockFuelIndex) + weightsHtml + backupHtml.slice(blockFuelIndex);
} else {
    console.warn("Could not find Block fuel limit.");
}

indexHtml = indexHtml.replace(targetRegex, backupHtml);
fs.writeFileSync('wwwroot/index.html', indexHtml);
console.log("Success");
