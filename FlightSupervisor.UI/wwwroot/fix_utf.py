import sys
import codecs

file_path = r"d:\FlightSupervisor\FlightSupervisor.UI\wwwroot\app.js"
try:
    with open(file_path, "r", encoding="utf-8", errors="replace") as f:
        content = f.read()

        # The replace rules
        # For the bad strings we saw in app.js via view_file
        content = content.replace("âš ï¸ ", "⚠️")
        content = content.replace("âœ…", "✅")
        content = content.replace("ðŸ”¹", "🔹")
        content = content.replace("ðŸ ±", "🥪")
        content = content.replace("ðŸ¥ª", "🥪")
        content = content.replace("âœ✨", "✨")
        content = content.replace("ðŸ’§", "💧")
        content = content.replace("ðŸ—‘ï¸ ", "🗑️")
        content = content.replace("ðŸ—‘", "🗑️")
        content = content.replace("TERMINÃ‰", "TERMINÉ")

        # The missing fuel icon
        # In GO_ICONS or maybe telemetry badges?
        content = content.replace("Âœ¯", "⛽")

        # More possible variants
        content = content.replace("ðŸ‘¥", "👥")

    with open(file_path, "w", encoding="utf-8-sig") as f: # write with BOM to absolutely force WebView2 to read it properly
        f.write(content)

    print("String replacements applied successfully!")
except Exception as e:
    print(f"Error: {e}")
