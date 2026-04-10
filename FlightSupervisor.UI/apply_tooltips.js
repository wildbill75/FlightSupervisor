const fs = require('fs');

const indexHtmlPath = 'wwwroot/index.html';
let indexHtml = fs.readFileSync(indexHtmlPath, 'utf8');

// 1. Fix Logo
indexHtml = indexHtml.replace(
    '<img src="assets/airlines_logos/EZY.png"',
    '<img src="easyjet-logo.png"'
);

// Helper for tooltip
function addTooltip(html, labelSpan, tooltipText) {
    const wrappedLabel = `<div class="group relative inline-block cursor-help">${labelSpan}<div class="pointer-events-none absolute bottom-full left-1/2 -translate-x-1/2 mb-2 w-48 opacity-0 transition-opacity duration-300 group-hover:opacity-100 bg-black/90 text-[#b6b6b6] font-sans font-normal text-[11px] p-2.5 rounded-lg border border-white/10 z-[110] text-center shadow-lg leading-snug tracking-normal normal-case">${tooltipText}</div></div>`;
    return html.replace(labelSpan, wrappedLabel);
}

// 2. Add Tooltips to Commercial
indexHtml = addTooltip(indexHtml,
    '<span class="text-[9px] uppercase tracking-widest text-[#7b7b7b] font-bold mb-1">Pax Contentment <span class="text-white/30 text-[8px]">(Target)</span></span>',
    'Objectif de satisfaction globale attendu par la compagnie pour ce type de vol.'
);

indexHtml = addTooltip(indexHtml,
    '<span class="text-[9px] uppercase tracking-widest text-[#7b7b7b] font-bold mb-1">Hard Product</span>',
    'Équipement cabine : confort des sièges, espace pour les jambes, présence de WiFi, écrans (IFE), et prises électriques.'
);

indexHtml = addTooltip(indexHtml,
    '<span class="text-[9px] uppercase tracking-widest text-[#7b7b7b] font-bold mb-1">Soft Product</span>',
    'Service à bord : qualité de la restauration, variété des snacks/boissons, et trousses de confort.'
);

indexHtml = addTooltip(indexHtml,
    '<span class="text-[9px] uppercase tracking-widest text-[#7b7b7b] font-bold mb-1">Crew Friendliness</span>',
    'Niveau d\'accueil, courtoisie et attitude chaleureuse exigés des membres d\'équipage (PNC).'
);

// 3. Add Tooltips to Operations
indexHtml = addTooltip(indexHtml,
    '<span class="text-[8px] text-white font-bold tracking-widest px-1.5 py-0.5 rounded bg-indigo-500/20 text-indigo-300">FRAMEWORK</span>',
    'Cadre réglementaire strict applicable aux opérations aériennes (ex: EASA FAR, FAA).'
);

indexHtml = addTooltip(indexHtml,
    '<span class="text-[8px] text-white font-bold tracking-widest px-1.5 py-0.5 rounded bg-emerald-500/20 text-emerald-300">COST INDEX</span>',
    'Indice affectant la vitesse de croisière et la consommation : plus il est élevé, plus le vol est rapide mais consommateur.'
);

indexHtml = addTooltip(indexHtml,
    '<span class="text-[8px] text-white font-bold tracking-widest px-1.5 py-0.5 rounded bg-sky-500/20 text-sky-300">FUEL LOGIC</span>',
    'Mode de calcul du carburant supplémentaire utilisé par les répartiteurs (ici : Statistical Flight Planning).'
);

indexHtml = addTooltip(indexHtml,
    '<span class="text-[8px] text-white font-bold tracking-widest px-1.5 py-0.5 rounded bg-rose-500/20 text-rose-300">CONTINGENCY</span>',
    'Pourcentage obligatoire de carburant de réserve alloué pour palier aux aléas météo ou de route.'
);

fs.writeFileSync('wwwroot/index.html', indexHtml, 'utf8');
console.log("Tooltips applied successfully!");
