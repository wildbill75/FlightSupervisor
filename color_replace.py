import os
import re

files_to_process = [
    r"d:\FlightSupervisor\FlightSupervisor.UI\wwwroot\index.html",
    r"d:\FlightSupervisor\FlightSupervisor.UI\wwwroot\app.js",
    r"d:\FlightSupervisor\FlightSupervisor.UI\wwwroot\app.css"
]

replacements = {
    # Main window background
    "bg-[#12141A]": "bg-[#141414]",
    "bg-[#12141a]": "bg-[#141414]",
    "#111318": "#141414", # generic background in CSS / Tailwind

    # Menu bar top and left background
    "bg-[#161A21]": "bg-[#1d1d1d]",
    "#161A25": "#1d1d1d",

    # Modale and pills background
    "bg-[#1C1F26]": "bg-[#2a2a2b]",
    "bg-[#2a2a35]": "bg-[#2a2a2b]",
    "bg-[#1A1C23]": "bg-[#2a2a2b]",
    "bg-[#232730]": "bg-[#2a2a2b]",
    "bg-[#1E2433]": "bg-[#2a2a2b]",
    "bg-[#1E293B]": "bg-[#2a2a2b]",

    # Button mouse over
    "hover:bg-white/5": "hover:bg-[#696969]",
    "hover:bg-white/10": "hover:bg-[#696969]",
    "hover:bg-white/20": "hover:bg-[#696969]",

    # Icons & Font color 1
    "text-slate-400": "text-[#b6b6b6]",
    "text-slate-300": "text-[#b6b6b6]",
    "text-[#78788a]": "text-[#7b7b7b]",
    "text-slate-500": "text-[#7b7b7b]",
    "text-white/50": "text-[#7b7b7b]",
    "#e2e2e9": "#b6b6b6", # global text color
    "#94a3b8": "#b6b6b6", # global text color
    "#e2e8f0": "#b6b6b6", # global text color

    # Modale borders (bonus: keep it subtle dark to fit)
    "border-[#242A35]": "border-black/20",
    "border-white/5": "border-white/5" # Let's keep these unless bad.
}

for file_path in files_to_process:
    if not os.path.exists(file_path):
        continue
    
    with open(file_path, "r", encoding="utf-8") as f:
        content = f.read()

    # Apply pure string replacements
    for k, v in replacements.items():
        content = content.replace(k, v)

    with open(file_path, "w", encoding="utf-8") as f:
        f.write(content)

print("Color replacement complete.")
