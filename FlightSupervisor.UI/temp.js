const fs = require('fs');
const html = fs.readFileSync('wwwroot/index.html', 'utf8');
const start = html.indexOf('<section id="briefing"');
const end = html.indexOf('</section>', start) + 10;
fs.writeFileSync('temp_briefing.html', html.substring(start, end));
