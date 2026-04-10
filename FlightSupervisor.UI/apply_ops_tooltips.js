const fs = require('fs');

const indexHtmlPath = 'wwwroot/index.html';
let html = fs.readFileSync(indexHtmlPath, 'utf8');

function addOpsTooltip(html, labelSpan, tooltipText) {
    let s = html.indexOf(labelSpan);
    if (s > -1) {
        let wrapped = `<div class="group relative inline-block cursor-help w-fit">${labelSpan}<div class="pointer-events-none absolute bottom-full left-1/2 -translate-x-1/2 mb-2 w-48 opacity-0 transition-opacity duration-300 group-hover:opacity-100 bg-black/90 text-[#b6b6b6] font-sans font-normal text-[11px] p-2.5 rounded-lg border border-white/10 z-[110] text-center shadow-lg leading-snug tracking-normal normal-case">${tooltipText}</div></div>`;
        return html.replace(labelSpan, wrapped);
    }
    return html;
}

html = addOpsTooltip(html, 
    '<span class="bg-indigo-500/20 text-indigo-300 text-[8px] font-bold uppercase tracking-widest px-2 py-0.5 rounded block w-fit mb-2">Framework</span>',
    'Strict regulatory framework governing operations (e.g., EASA, FAR, FAA).'
);

html = addOpsTooltip(html, 
    '<span class="bg-emerald-500/20 text-emerald-300 text-[8px] font-bold uppercase tracking-widest px-2 py-0.5 rounded block w-fit mb-2">Cost Index</span>',
    'Ratio of time-related cost to fuel cost. A higher index means faster flights but higher fuel consumption.'
);

html = addOpsTooltip(html, 
    '<span class="bg-sky-500/20 text-sky-300 text-[8px] font-bold uppercase tracking-widest px-2 py-0.5 rounded block w-fit mb-2">Fuel Logic</span>',
    'Dispatch fuel calculation methodology (e.g., Statistical Flight Planning).'
);

html = addOpsTooltip(html, 
    '<span class="bg-rose-500/20 text-rose-300 text-[8px] font-bold uppercase tracking-widest px-2 py-0.5 rounded block w-fit mb-2">Contingency</span>',
    'Mandatory reserve fuel percentage allocated to cover unforeseen en-route weather or delays.'
);

fs.writeFileSync('wwwroot/index.html', html, 'utf8');
console.log("Ops replaced");
