const fs = require('fs');

const indexHtmlPath = 'wwwroot/index.html';
let indexHtml = fs.readFileSync(indexHtmlPath, 'utf8');

// Helper to wrap text with tooltip if it doesn't already have one
function wrapTooltip(html, labelSpanRegex, tooltipText) {
    return html.replace(labelSpanRegex, (match, prefix, content) => {
        // If it's already wrapped in a tooltip, just return the content part and re-wrap
        return `<div class="group relative inline-block cursor-help">${content || match}<div class="pointer-events-none absolute bottom-full left-1/2 -translate-x-1/2 mb-2 w-48 opacity-0 transition-opacity duration-300 group-hover:opacity-100 bg-black/90 text-[#b6b6b6] font-sans font-normal text-[11px] p-2.5 rounded-lg border border-white/10 z-[110] text-center shadow-lg leading-snug tracking-normal normal-case">${tooltipText}</div></div>`;
    });
}

// 1. Replace existing French tooltips for Commercial with English versions
indexHtml = indexHtml.replace(
    /A%quipement cabine : confort des .*? et prises Aclectriques\./g,
    "Aircraft cabin quality: seat comfort, legroom pitch, WiFi availability, IFE screens, and power outlets."
);
indexHtml = indexHtml.replace(
    /Service A.*? bord : qualitAc de la restauration, variActAc des snacks\/boissons, et trousses de confort\./g,
    "In-flight service quality: meal standards, snack/beverage variety, and comfort kits."
);
indexHtml = indexHtml.replace(
    /Niveau d'accueil, courtoisie et attitude chaleureuse exigAcs des membres d'Acquipage \(PNC\)\./g,
    "Service standards, courtesy, and welcoming attitude expected from the cabin crew."
);
// Hard replace the Pax Contentment target tooltip directly
indexHtml = indexHtml.replace(
    /Objectif de satisfaction globale attendu par la compagnie pour ce type de vol\./g,
    "Target overall passenger satisfaction level determined by airline policy for this flight."
);

// 2. Wrap Operations tags with English tooltips properly
indexHtml = wrapTooltip(indexHtml,
    /(<span class="bg-indigo-500\/20 text-indigo-300 text-\[8px\] font-bold uppercase tracking-widest px-2 py-0\.5 rounded block w-fit mb-2">Framework<\/span>)/,
    "Strict regulatory framework governing operations (e.g., EASA, FAR, FAA)."
);

indexHtml = wrapTooltip(indexHtml,
    /(<span class="bg-emerald-500\/20 text-emerald-300 text-\[8px\] font-bold uppercase tracking-widest px-2 py-0\.5 rounded block w-fit mb-2">Cost Index<\/span>)/,
    "Ratio of time-related cost to fuel cost. A higher index means faster flights but higher fuel consumption."
);

indexHtml = wrapTooltip(indexHtml,
    /(<span class="bg-sky-500\/20 text-sky-300 text-\[8px\] font-bold uppercase tracking-widest px-2 py-0\.5 rounded block w-fit mb-2">Fuel Logic<\/span>)/,
    "Dispatch fuel calculation methodology (e.g., Statistical Flight Planning)."
);

indexHtml = wrapTooltip(indexHtml,
    /(<span class="bg-rose-500\/20 text-rose-300 text-\[8px\] font-bold uppercase tracking-widest px-2 py-0\.5 rounded block w-fit mb-2">Contingency<\/span>)/,
    "Mandatory reserve fuel percentage allocated to cover unforeseen en-route weather or delays."
);

fs.writeFileSync('wwwroot/index.html', indexHtml, 'utf8');
console.log("English tooltips applied to all sections successfully!");
