
const fs = require('fs');
let content = fs.readFileSync('d:/FlightSupervisor/FlightSupervisor.UI/wwwroot/app.js', 'utf8');

content = content.replace(/window\.renderBriefingTabs\(\);\r?\n\s*window\.renderManifest\(null\);/g, 'window.renderBriefingTabs();\nif (window.populateDashboardActiveLeg) window.populateDashboardActiveLeg(window.dashboardActiveLegIndex || 0);\nwindow.renderManifest(null);');

fs.writeFileSync('d:/FlightSupervisor/FlightSupervisor.UI/wwwroot/app.js', content, 'utf8');

