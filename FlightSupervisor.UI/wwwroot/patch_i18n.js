const fs = require('fs');

let html = fs.readFileSync('index.html', 'utf8');

const replacements = [
    // Include the locales JS
    ['<script src="app.js', '<script src="locales.js?v=2"></script>\n    <script src="app.js'],

    // Menu Tabs
    ['<span>Dashboard</span>', '<span data-i18n="nav_dashboard">Dashboard</span>'],
    ['<span>Briefing</span>', '<span data-i18n="nav_briefing">Briefing</span>'],
    ['<span>Ground Ops</span>', '<span data-i18n="nav_groundops">Ground Ops</span>'],
    ['<span>Manifest</span>', '<span data-i18n="nav_manifest">Manifest</span>'],
    ['<span>Logs</span>', '<span data-i18n="nav_logs">Logs</span>'],
    ['<span>Settings</span>', '<span data-i18n="nav_settings">Settings</span>'],
    ['id="btnSmartConnect" class="', 'id="btnSmartConnect" data-i18n="btn_not_connected" class="'],

    // Dashboard
    ['>Current Flight Phase<', ' data-i18n="dash_current_phase">Current Flight Phase<'],
    ['>Ground Operations Standing By.<', ' data-i18n="dash_meta_standby">Ground Operations Standing By.<'],
    ['>Efficiency Score<', ' data-i18n="dash_eff_score">Efficiency Score<'],
    ['uppercase mt-2">PTS<', 'uppercase mt-2" data-i18n="dash_pts">PTS<'],
    
    ['uppercase">Flight Timetable (UTC)<', 'uppercase" data-i18n="dash_timetable_utc">Flight Timetable (UTC)<'],
    ['>Milestone<', ' data-i18n="dash_th_milestone">Milestone<'],
    ['>Scheduled<', ' data-i18n="dash_th_sched">Scheduled<'],
    ['>Actual<', ' data-i18n="dash_th_actual">Actual<'],
    ['>Status<', ' data-i18n="dash_th_status">Status<'],

    ['SOBT (Off-Block)</td>', '<span data-i18n="dash_td_sobt">SOBT (Off-Block)</span></td>'],
    ['AOBT (Actual Off)</td>', '<span data-i18n="dash_td_aobt">AOBT (Actual Off)</span></td>'],
    ['SIBT (In-Block)</td>', '<span data-i18n="dash_td_sibt">SIBT (In-Block)</span></td>'],
    ['AIBT (Actual In)</td>', '<span data-i18n="dash_td_aibt">AIBT (Actual In)</span></td>'],
    
    ['>Standby<', ' data-i18n="dash_status_standby">Standby<'],
    ['>Active<', ' data-i18n="dash_status_active">Active<'],
    
    ['>Cabin Anxiety<', ' data-i18n="dash_cabin_anxiety">Cabin Anxiety<'],
    ['>Optimal Calm<', ' data-i18n="dash_anxiety_opt">Optimal Calm<'],
    ['>Announce: Delay Apology<', ' data-i18n="dash_btn_ann_delay">Announce: Delay Apology<'],
    ['>Announce: Turbulence<', ' data-i18n="dash_btn_ann_turb">Announce: Turbulence<'],
    ['\n                            PNC Comms', '\n                            <span data-i18n="dash_pnc_comms">PNC Comms</span>'],
    ['>Standing by...<', ' data-i18n="dash_pnc_standby">Standing by...<'],

    // Briefing
    ['>Flight Briefing</div>', ' data-i18n="brief_title">Flight Briefing</div>'],
    ['>Flight Details</h4>', ' data-i18n="brief_flight_details">Flight Details</h4>'],
    ['>Airline</span>', ' data-i18n="brief_airline">Airline</span>'],
    ['>Flight Num</span>', ' data-i18n="brief_fn">Flight Num</span>'],
    ['>Routing</h4>', ' data-i18n="brief_routing">Routing</h4>'],
    ['>Cruise</span>', ' data-i18n="brief_cruise">Cruise</span>'],
    ['>Route</span>', ' data-i18n="brief_route">Route</span>'],
    ['>Timetable</h4>', ' data-i18n="brief_timetable">Timetable</h4>'],
    ['>SOBT</span>', ' data-i18n="brief_sobt">SOBT</span>'],
    ['>SIBT</span>', ' data-i18n="brief_sibt">SIBT</span>'],
    ['>ETE</span>', ' data-i18n="brief_ete">ETE</span>'],
    ['>Payload</h4>', ' data-i18n="brief_payload">Payload</h4>'],
    ['>Passengers</span>', ' data-i18n="brief_pax">Passengers</span>'],
    ['>Zero Fuel</span>', ' data-i18n="brief_zfw">Zero Fuel</span>'],
    ['>Take-Off</span>', ' data-i18n="brief_tow">Take-Off</span>'],
    ['>Calculated Route</div>', ' data-i18n="brief_calc_route">Calculated Route</div>'],
    ['>PENDING ROUTE CREATION<', ' data-i18n="brief_pending_route">PENDING ROUTE CREATION<'],
    ['>Operational Limits</h3>', ' data-i18n="brief_op_limits">Operational Limits</h3>'],
    ['>Weather</span>', ' data-i18n="brief_weather">Weather</span>'],
    ['>Visibility</span>', ' data-i18n="brief_visibility">Visibility</span>'],
    ['>Cruise Alt</span>', ' data-i18n="brief_cruise_alt">Cruise Alt</span>'],
    ['>ZFW / TOW</span>', ' data-i18n="brief_zfw_tow">ZFW / TOW</span>'],

    // Cabin
    ['>Cabin & Manifest</h2>', ' data-i18n="cabin_title">Cabin & Manifest</h2>'],
    ['>Waiting for final manifest processing...</p>', ' data-i18n="cabin_pending">Waiting for final manifest processing...</p>'],
    
    // Ground
    ['>Ground Operations</h2>', ' data-i18n="ground_title">Ground Operations</h2>'],
    ['>Ground operations pending SimBrief initialization...</p>', ' data-i18n="ground_pending">Ground operations pending SimBrief initialization...</p>'],

    // Logs
    ['>Flight Logs</h2>', ' data-i18n="logs_title">Flight Logs</h2>'],

    // Settings
    ['<h2 class="text-xl font-label tracking-[0.4em] text-sky-400 uppercase opacity-80">Settings</h2>', '<h2 class="text-xl font-label tracking-[0.4em] text-sky-400 uppercase opacity-80" data-i18n="set_title">Settings</h2>'],
    ['>Save All Settings<', ' data-i18n="set_save_btn">Save All Settings<'],
    ['>SimBrief Integration</h3>', ' data-i18n="set_sb_int">SimBrief Integration</h3>'],
    ['>Link your account to fetch flight plans block.</p>', ' data-i18n="set_sb_desc">Link your account to fetch flight plans block.</p>'],
    ['>Username</label>', ' data-i18n="set_sb_user">Username</label>'],
    ['>Remember Username</label>', ' data-i18n="set_sb_rem">Remember Username</label>'],
    
    ['>Aviation Units</h3>', ' data-i18n="set_units_title">Aviation Units</h3>'],
    ['>Preferred metrics for briefing and logs.</p>', ' data-i18n="set_units_desc">Preferred metrics for briefing and logs.</p>'],
    ['>Speed</label>', ' data-i18n="set_unit_speed">Speed</label>'],
    ['>Altitude</label>', ' data-i18n="set_unit_alt">Altitude</label>'],
    ['>Weight</label>', ' data-i18n="set_unit_weight">Weight</label>'],
    ['>Temperature</label>', ' data-i18n="set_unit_temp">Temperature</label>'],
    ['>Pressure</label>', ' data-i18n="set_unit_press">Pressure</label>'],

    ['>Integrations & Wx</h3>', ' data-i18n="set_ext_title">Integrations & Wx</h3>'],
    ['>Connections to external engines.</p>', ' data-i18n="set_ext_desc">Connections to external engines.</p>'],
    ['>Weather Source</label>', ' data-i18n="set_wx_src">Weather Source</label>'],
    ['>GSX Pro Auto-Sync</label>', ' data-i18n="set_gsx_sync">GSX Pro Auto-Sync</label>'],
    ['>⚠️ Disable aircraft-specific GSX automations (e.g Fenix EFB Sync).</p>', ' data-i18n="set_gsx_warn">⚠️ Disable aircraft-specific GSX automations (e.g Fenix EFB Sync).</p>'],

    ['>Regional & Locale</h3>', ' data-i18n="set_reg_title">Regional & Locale</h3>'],
    ['>Configure application language and time formats.</p>', ' data-i18n="set_reg_desc">Configure application language and time formats.</p>'],
    ['>Language</label>', ' data-i18n="set_lang">Language</label>'],
    ['>Time Format</label>', ' data-i18n="set_time_fmt">Time Format</label>'],

    ['>Ground Operations</h3>', ' data-i18n="set_go_title">Ground Operations</h3>'],
    ['>Behavior of the turnaround simulation.</p>', ' data-i18n="set_go_desc">Behavior of the turnaround simulation.</p>'],
    ['>Duration Speed</label>', ' data-i18n="set_go_speed">Duration Speed</label>'],
    ['>Random Events Prob.</span>', ' data-i18n="set_go_prob">Random Events Prob.</span>'],

    ['>Realism & App</h3>', ' data-i18n="set_real_title">Realism & App</h3>'],
    ['>Scoring rules and window behavior.</p>', ' data-i18n="set_real_desc">Scoring rules and window behavior.</p>'],
    ['>Hardcore Mode</label>', ' data-i18n="set_hardcore">Hardcore Mode</label>'],
    ['>Fines are doubled. Safety violations immediately cancel the flight.</p>', ' data-i18n="set_hardcore_desc">Fines are doubled. Safety violations immediately cancel the flight.</p>'],
    ['>Always on Top</label>', ' data-i18n="set_ontop">Always on Top</label>'],
    ['>Flight Supervisor stays visible above MSFS.</p>', ' data-i18n="set_ontop_desc">Flight Supervisor stays visible above MSFS.</p>'],
    
    // Modals
    ['>Cancel Flight?</h3>', ' data-i18n="modal_cancel_title">Cancel Flight?</h3>'],
    ['>Are you sure you want to cancel the current flight plan?<br>All ground operations and telemetry progress will be reset.</p>', ' data-i18n="modal_cancel_desc">Are you sure you want to cancel the current flight plan?<br>All ground operations and telemetry progress will be reset.</p>'],
    ['>KEEP FLIGHT</button>', ' data-i18n="modal_cancel_no">KEEP FLIGHT</button>'],
    ['>CANCEL FLIGHT</button>', ' data-i18n="modal_cancel_yes">CANCEL FLIGHT</button>'],
    ['>Abort Service?</h3>', ' data-i18n="modal_abort_title">Abort Service?</h3>'],
    ['>Are you sure you want to skip ', '><span data-i18n="modal_abort_desc_pre">Are you sure you want to skip</span> '],
    ['>?<br><br><span class="opacity-70">Aborting ground operations early can trigger negative consequences.</span></p>', '>?<br><br><span class="opacity-70" data-i18n="modal_abort_desc_post">Aborting ground operations early can trigger negative consequences.</span></p>'],
    ['>CONTINUE</button>', ' data-i18n="modal_abort_no">CONTINUE</button>'],
    ['>ABORT</button>', ' data-i18n="modal_abort_yes">ABORT</button>']
];

for (let r of replacements) {
    if(!html.includes(r[0])) { console.log('not found: ' + r[0]); }
    html = html.replaceAll(r[0], r[1]);
}
fs.writeFileSync('index.html', html);
console.log('Done');
