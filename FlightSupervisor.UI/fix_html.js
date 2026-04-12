const fs = require('fs');

const indexHtmlPath = 'wwwroot/index.html';
let html = fs.readFileSync(indexHtmlPath, 'utf8');

// Fix 1: max-w-7xl mx-auto
const containerSearch = '<div class="flex flex-col items-center gap-6 mb-6 flex-none w-full">\r\n                    <div id="briefing-timeline" class="flex items-center gap-4 py-4 w-full overflow-x-auto min-h-[160px] custom-thin-scrollbar">';
const containerReplace = '<div class="flex flex-col items-center gap-6 mb-6 flex-none w-full max-w-7xl mx-auto">\r\n                    <div id="briefing-timeline" class="flex items-center gap-4 py-4 w-full overflow-x-auto min-h-[160px] custom-thin-scrollbar">';

const containerSearchN = containerSearch.replace(/\r\n/g, '\n');
const containerReplaceN = containerReplace.replace(/\r\n/g, '\n');

if(html.indexOf(containerSearch) !== -1) {
    html = html.replace(containerSearch, containerReplace);
} else if(html.indexOf(containerSearchN) !== -1) {
    html = html.replace(containerSearchN, containerReplaceN);
}

// Fix 2: navBriefingBtn
const btnSearch = '<li id="navBriefingBtn" class="active no-drag flex justify-center items-center w-12 h-12 rounded-xl cursor-pointer hover:bg-sky-500/10 hover:text-sky-300 transition-all duration-300 text-slate-400 relative group" data-target="briefing" title="Briefing">\r\n                <div class="absolute inset-0 rounded-xl bg-sky-400/0 group-[.active]:bg-sky-500/20 group-[.active]:shadow-[0_0_15px_rgba(56,189,248,0.2)] transition-all duration-500"></div>\r\n                <div class="absolute left-[-8px] h-8 w-1 bg-sky-400 rounded-r-full opacity-0 group-[.active]:opacity-100 shadow-[0_0_10px_rgba(56,189,248,0.8)] transition-all duration-500"></div>\r\n                <span class="material-symbols-outlined shrink-0 text-[20px] font-light relative z-10 group-[.active]:text-sky-400">assignment</span>\r\n            </li>';
const btnReplace = '<li id="navBriefingBtn" class="active no-drag flex justify-center items-center w-12 h-12 rounded-xl cursor-pointer text-slate-400 hover:bg-white/5 transition-all duration-300 group" data-target="briefing" title="Briefing">\r\n                <span class="material-symbols-outlined shrink-0 text-[20px] font-light relative z-10 group-hover:drop-shadow-[0_0_8px_rgba(255,255,255,0.5)] group-[.active]:text-white group-[.active]:drop-shadow-[0_0_8px_rgba(255,255,255,0.5)]">assignment</span>\r\n            </li>';

const btnSearchN = btnSearch.replace(/\r\n/g, '\n');
const btnReplaceN = btnReplace.replace(/\r\n/g, '\n');

if(html.indexOf(btnSearch) !== -1) {
    html = html.replace(btnSearch, btnReplace);
} else if(html.indexOf(btnSearchN) !== -1) {
    html = html.replace(btnSearchN, btnReplaceN);
}

fs.writeFileSync(indexHtmlPath, html, 'utf8');
console.log('Done!');
