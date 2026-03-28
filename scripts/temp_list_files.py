import os
import glob

folder = r"d:\FlightSupervisor\assets\sounds"
files = glob.glob(os.path.join(folder, "*.mp3"))

# Filter size < 100KB and sort by mtime
small_files = []
for f in files:
    size = os.path.getsize(f)
    if size < 100000:
        small_files.append((f, os.path.getmtime(f), size))

small_files.sort(key=lambda x: x[1])

print(f"Total small files (<100KB) found: {len(small_files)}\n")
for idx, (f, mtime, size) in enumerate(small_files):
    name = os.path.basename(f)
    # Extract just the time part for readability e.g. "22_06_51"
    parts = name.split("T")
    if len(parts) > 1:
        time_part = parts[1][:8]
    else:
        time_part = name
    print(f"{idx+1:02d}. [Time: {time_part}] Size: {size/1024:.1f} KB -> {name}")
