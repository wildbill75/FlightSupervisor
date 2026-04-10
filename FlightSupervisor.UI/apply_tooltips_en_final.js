const fs = require('fs');

const indexHtmlPath = 'wwwroot/index.html';
let html = fs.readFileSync(indexHtmlPath, 'utf8');

// Helper to replace everything inside a tag including the tag itself
function replaceSubstr(html, startSubstr, endSubstr, newContent) {
    let s = html.indexOf(startSubstr);
    if (s === -1) return html;
    let e = html.indexOf(endSubstr, s);
    if (e === -1) return html;
    e += endSubstr.length;
    return html.substring(0, s) + newContent + html.substring(e);
}

// 1. Clean up Commercial Tooltips corrupted strings to clean English Tooltips
let paxHtml = `<div class="group relative inline-block cursor-help"><span class="text-[9px] uppercase tracking-widest text-[#7b7b7b] font-bold mb-1">Pax Contentment <span class="text-white/30 text-[8px]">(Target)</span></span><div class="pointer-events-none absolute bottom-full left-1/2 -translate-x-1/2 mb-2 w-48 opacity-0 transition-opacity duration-300 group-hover:opacity-100 bg-black/90 text-[#b6b6b6] font-sans font-normal text-[11px] p-2.5 rounded-lg border border-white/10 z-[110] text-center shadow-lg leading-snug tracking-normal normal-case">Target overall passenger satisfaction level determined by airline policy for this flight.</div></div>`;

let hardHtml = `<div class="group relative inline-block cursor-help"><span class="text-[9px] uppercase tracking-widest text-[#7b7b7b] font-bold mb-1">Hard Product</span><div class="pointer-events-none absolute bottom-full left-1/2 -translate-x-1/2 mb-2 w-48 opacity-0 transition-opacity duration-300 group-hover:opacity-100 bg-black/90 text-[#b6b6b6] font-sans font-normal text-[11px] p-2.5 rounded-lg border border-white/10 z-[110] text-center shadow-lg leading-snug tracking-normal normal-case">Aircraft cabin quality: seat comfort, legroom pitch, WiFi availability, IFE screens, and power outlets.</div></div>`;

let softHtml = `<div class="group relative inline-block cursor-help"><span class="text-[9px] uppercase tracking-widest text-[#7b7b7b] font-bold mb-1">Soft Product</span><div class="pointer-events-none absolute bottom-full left-1/2 -translate-x-1/2 mb-2 w-48 opacity-0 transition-opacity duration-300 group-hover:opacity-100 bg-black/90 text-[#b6b6b6] font-sans font-normal text-[11px] p-2.5 rounded-lg border border-white/10 z-[110] text-center shadow-lg leading-snug tracking-normal normal-case">In-flight service quality: meal standards, snack/beverage variety, and comfort kits.</div></div>`;

let crewHtml = `<div class="group relative inline-block cursor-help"><span class="text-[9px] uppercase tracking-widest text-[#7b7b7b] font-bold mb-1">Crew Friendliness</span><div class="pointer-events-none absolute bottom-full left-1/2 -translate-x-1/2 mb-2 w-48 opacity-0 transition-opacity duration-300 group-hover:opacity-100 bg-black/90 text-[#b6b6b6] font-sans font-normal text-[11px] p-2.5 rounded-lg border border-white/10 z-[110] text-center shadow-lg leading-snug tracking-normal normal-case">Service standards, courtesy, and welcoming attitude expected from the cabin crew.</div></div>`;

html = replaceSubstr(html, '<div class="group relative inline-block cursor-help"><span class="text-[9px] uppercase tracking-widest text-[#7b7b7b] font-bold mb-1">Pax Contentment', '</div></div>', paxHtml);
html = replaceSubstr(html, '<div class="group relative inline-block cursor-help"><span class="text-[9px] uppercase tracking-widest text-[#7b7b7b] font-bold mb-1">Hard Product', '</div></div>', hardHtml);
html = replaceSubstr(html, '<div class="group relative inline-block cursor-help"><span class="text-[9px] uppercase tracking-widest text-[#7b7b7b] font-bold mb-1">Soft Product', '</div></div>', softHtml);
html = replaceSubstr(html, '<div class="group relative inline-block cursor-help"><span class="text-[9px] uppercase tracking-widest text-[#7b7b7b] font-bold mb-1">Crew Friendliness', '</div></div>', crewHtml);

// 2. Add Operations tooltips 
function wrapTooltipStr(html, labelSpan, tooltipText) {
    if (html.indexOf(labelSpan) > -1 && html.indexOf(tooltipText) === -1) {
        let wrapped = `<div class="group relative inline-block cursor-help">${labelSpan}<div class="pointer-events-none absolute bottom-full left-1/2 -translate-x-1/2 mb-2 w-48 opacity-0 transition-opacity duration-300 group-hover:opacity-100 bg-black/90 text-[#b6b6b6] font-sans font-normal text-[11px] p-2.5 rounded-lg border border-white/10 z-[110] text-center shadow-lg leading-snug tracking-normal normal-case">${tooltipText}</div></div>`;
        return html.replace(labelSpan, wrapped);
    }
    return html;
}

html = wrapTooltipStr(html, 
    '<span class="bg-indigo-500/20 text-indigo-300 text-[8px] font-bold uppercase tracking-widest px-2 py-0.5 rounded block w-fit mb-2">Framework</span>',
    'Strict regulatory framework governing operations (e.g., EASA, FAR, FAA).'
);

html = wrapTooltipStr(html, 
    '<span class="bg-emerald-500/20 text-emerald-300 text-[8px] font-bold uppercase tracking-widest px-2 py-0.5 rounded block w-fit mb-2">Cost Index</span>',
    'Ratio of time-related cost to fuel cost. A higher index means faster flights but higher fuel consumption.'
);

html = wrapTooltipStr(html, 
    '<span class="bg-sky-500/20 text-sky-300 text-[8px] font-bold uppercase tracking-widest px-2 py-0.5 rounded block w-fit mb-2">Fuel Logic</span>',
    'Dispatch fuel calculation methodology (e.g., Statistical Flight Planning).'
);

html = wrapTooltipStr(html, 
    '<span class="bg-rose-500/20 text-rose-300 text-[8px] font-bold uppercase tracking-widest px-2 py-0.5 rounded block w-fit mb-2">Contingency</span>',
    'Mandatory reserve fuel percentage allocated to cover unforeseen en-route weather or delays.'
);

fs.writeFileSync('wwwroot/index.html', html, 'utf8');
console.log("Replaced perfectly");
