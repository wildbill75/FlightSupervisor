const fs = require('fs');
const path = 'wwwroot/index.html';
let html = fs.readFileSync(path, 'utf8');

// The corrupted tags are basically `<div class="group relative inline-block cursor-help">\d+<div ...>Text</div></div>`
// and followed by `<span class="...">EASA Ops</span>` etc.. 
// Wait, the corrupted block replaced the original `<span class="bg-...">...</span>` entirely!
// I need to put the original spans back!

const originalSpans = {
    'Strict regulatory framework': '<span class="bg-indigo-500/20 text-indigo-300 text-[8px] font-bold uppercase tracking-widest px-2 py-0.5 rounded block w-fit mb-2">Framework</span>',
    'Ratio of time-related cost': '<span class="bg-emerald-500/20 text-emerald-300 text-[8px] font-bold uppercase tracking-widest px-2 py-0.5 rounded block w-fit mb-2">Cost Index</span>',
    'Dispatch fuel calculation': '<span class="bg-sky-500/20 text-sky-300 text-[8px] font-bold uppercase tracking-widest px-2 py-0.5 rounded block w-fit mb-2">Fuel Logic</span>',
    'Mandatory reserve fuel': '<span class="bg-rose-500/20 text-rose-300 text-[8px] font-bold uppercase tracking-widest px-2 py-0.5 rounded block w-fit mb-2">Contingency</span>'
};

html = html.replace(/<div class="group relative inline-block cursor-help(?: w-fit)?">\d+<div class="pointer-events-none absolute bottom-full left-1\/2 -translate-x-1\/2 mb-2 w-48 opacity-0 transition-opacity duration-300 group-hover:opacity-100 bg-black\/90 text-\[#b6b6b6\] font-sans font-normal text-\[11px\] p-2\.5 rounded-lg border border-white\/10 z-\[110\] text-center shadow-lg leading-snug tracking-normal normal-case">([^<]+)<\/div><\/div>/g, (match, tooltipText) => {
    let originalSpan = '';
    if (tooltipText.includes('Strict regulatory')) originalSpan = originalSpans['Strict regulatory framework'];
    else if (tooltipText.includes('Ratio of time-related cost')) originalSpan = originalSpans['Ratio of time-related cost'];
    else if (tooltipText.includes('Dispatch fuel calculation')) originalSpan = originalSpans['Dispatch fuel calculation'];
    else if (tooltipText.includes('Mandatory reserve fuel')) originalSpan = originalSpans['Mandatory reserve fuel'];
    
    return `<div class="group relative inline-block cursor-help w-fit">${originalSpan}<div class="pointer-events-none absolute bottom-full left-1/2 -translate-x-1/2 mb-2 w-48 opacity-0 transition-opacity duration-300 group-hover:opacity-100 bg-black/90 text-[#b6b6b6] font-sans font-normal text-[11px] p-2.5 rounded-lg border border-white/10 z-[110] text-center shadow-lg leading-snug tracking-normal normal-case">${tooltipText}</div></div>`;
});

fs.writeFileSync(path, html, 'utf8');
console.log("Fixed numerical indices!");
