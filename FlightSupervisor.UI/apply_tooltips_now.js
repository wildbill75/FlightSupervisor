const fs = require('fs');

const indexHtmlPath = 'wwwroot/index.html';
let html = fs.readFileSync(indexHtmlPath, 'utf8');

// Helper to replace text exactly once
function rep(findStr, replaceStr) {
    if(html.indexOf(findStr) === -1) {
        console.log("NOT FOUND: " + findStr.substring(0, 50));
    } else {
        html = html.replace(findStr, replaceStr);
    }
}

rep(
    '<img src="assets/airlines_logos/EZY.png"',
    '<img src="easyjet-logo.png"'
);

const paxOld = '<span class="text-[9px] uppercase tracking-widest text-[#7b7b7b] font-bold mb-1">Pax Contentment <span class="text-white/30 text-[8px]">(Target)</span></span>';
const paxNew = `<div class="group relative inline-block cursor-help">${paxOld}<div class="pointer-events-none absolute bottom-full left-1/2 -translate-x-1/2 mb-2 w-48 opacity-0 transition-opacity duration-300 group-hover:opacity-100 bg-black/90 text-[#b6b6b6] font-sans font-normal text-[11px] p-2.5 rounded-lg border border-white/10 z-[110] text-center shadow-lg leading-snug tracking-normal normal-case">Target overall passenger satisfaction level determined by airline policy for this flight.</div></div>`;
rep(paxOld, paxNew);

const hardOld = '<span class="text-[9px] uppercase tracking-widest text-[#7b7b7b] font-bold mb-1">Hard Product</span>';
const hardNew = `<div class="group relative inline-block cursor-help">${hardOld}<div class="pointer-events-none absolute bottom-full left-1/2 -translate-x-1/2 mb-2 w-48 opacity-0 transition-opacity duration-300 group-hover:opacity-100 bg-black/90 text-[#b6b6b6] font-sans font-normal text-[11px] p-2.5 rounded-lg border border-white/10 z-[110] text-center shadow-lg leading-snug tracking-normal normal-case">Aircraft cabin quality: seat comfort, legroom pitch, WiFi availability, IFE screens, and power outlets.</div></div>`;
rep(hardOld, hardNew);

const softOld = '<span class="text-[9px] uppercase tracking-widest text-[#7b7b7b] font-bold mb-1">Soft Product</span>';
const softNew = `<div class="group relative inline-block cursor-help">${softOld}<div class="pointer-events-none absolute bottom-full left-1/2 -translate-x-1/2 mb-2 w-48 opacity-0 transition-opacity duration-300 group-hover:opacity-100 bg-black/90 text-[#b6b6b6] font-sans font-normal text-[11px] p-2.5 rounded-lg border border-white/10 z-[110] text-center shadow-lg leading-snug tracking-normal normal-case">In-flight service quality: meal standards, snack/beverage variety, and comfort kits.</div></div>`;
rep(softOld, softNew);

const crewOld = '<span class="text-[9px] uppercase tracking-widest text-[#7b7b7b] font-bold mb-1">Crew Friendliness</span>';
const crewNew = `<div class="group relative inline-block cursor-help">${crewOld}<div class="pointer-events-none absolute bottom-full left-1/2 -translate-x-1/2 mb-2 w-48 opacity-0 transition-opacity duration-300 group-hover:opacity-100 bg-black/90 text-[#b6b6b6] font-sans font-normal text-[11px] p-2.5 rounded-lg border border-white/10 z-[110] text-center shadow-lg leading-snug tracking-normal normal-case">Service standards, courtesy, and welcoming attitude expected from the cabin crew.</div></div>`;
rep(crewOld, crewNew);

const op1Old = '<span class="bg-indigo-500/20 text-indigo-300 text-[8px] font-bold uppercase tracking-widest px-2 py-0.5 rounded block w-fit mb-2">Framework</span>';
const op1New = `<div class="group relative inline-block cursor-help">${op1Old}<div class="pointer-events-none absolute bottom-full left-1/2 -translate-x-1/2 mb-2 w-48 opacity-0 transition-opacity duration-300 group-hover:opacity-100 bg-black/90 text-[#b6b6b6] font-sans font-normal text-[11px] p-2.5 rounded-lg border border-white/10 z-[110] text-center shadow-lg leading-snug tracking-normal normal-case">Strict regulatory framework governing operations (e.g., EASA, FAR, FAA).</div></div>`;
rep(op1Old, op1New);

const op2Old = '<span class="bg-emerald-500/20 text-emerald-300 text-[8px] font-bold uppercase tracking-widest px-2 py-0.5 rounded block w-fit mb-2">Cost Index</span>';
const op2New = `<div class="group relative inline-block cursor-help">${op2Old}<div class="pointer-events-none absolute bottom-full left-1/2 -translate-x-1/2 mb-2 w-48 opacity-0 transition-opacity duration-300 group-hover:opacity-100 bg-black/90 text-[#b6b6b6] font-sans font-normal text-[11px] p-2.5 rounded-lg border border-white/10 z-[110] text-center shadow-lg leading-snug tracking-normal normal-case">Ratio of time-related cost to fuel cost. A higher index means faster flights but higher fuel consumption.</div></div>`;
rep(op2Old, op2New);

const op3Old = '<span class="bg-sky-500/20 text-sky-300 text-[8px] font-bold uppercase tracking-widest px-2 py-0.5 rounded block w-fit mb-2">Fuel Logic</span>';
const op3New = `<div class="group relative inline-block cursor-help">${op3Old}<div class="pointer-events-none absolute bottom-full left-1/2 -translate-x-1/2 mb-2 w-48 opacity-0 transition-opacity duration-300 group-hover:opacity-100 bg-black/90 text-[#b6b6b6] font-sans font-normal text-[11px] p-2.5 rounded-lg border border-white/10 z-[110] text-center shadow-lg leading-snug tracking-normal normal-case">Dispatch fuel calculation methodology (e.g., Statistical Flight Planning).</div></div>`;
rep(op3Old, op3New);

const op4Old = '<span class="bg-rose-500/20 text-rose-300 text-[8px] font-bold uppercase tracking-widest px-2 py-0.5 rounded block w-fit mb-2">Contingency</span>';
const op4New = `<div class="group relative inline-block cursor-help">${op4Old}<div class="pointer-events-none absolute bottom-full left-1/2 -translate-x-1/2 mb-2 w-48 opacity-0 transition-opacity duration-300 group-hover:opacity-100 bg-black/90 text-[#b6b6b6] font-sans font-normal text-[11px] p-2.5 rounded-lg border border-white/10 z-[110] text-center shadow-lg leading-snug tracking-normal normal-case">Mandatory reserve fuel percentage allocated to cover unforeseen en-route weather or delays.</div></div>`;
rep(op4Old, op4New);

fs.writeFileSync(indexHtmlPath, html, 'utf8');
console.log('DONE!');
