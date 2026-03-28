import os
import glob
import time
import subprocess

# ----- DOSSIER SOURCE ET CIBLE -----
input_folder = r"d:\FlightSupervisor\assets\sounds"
output_folder = r"d:\FlightSupervisor\FlightSupervisor.UI\wwwroot\assets\sounds\airlines\air_france\pnc\en\female_1"

if not os.path.exists(output_folder):
    os.makedirs(output_folder)

# Récupérer tous les fichiers < 100ko (mots isolés)
all_files = glob.glob(os.path.join(input_folder, "*.mp3"))
small_files = []
for f in all_files:
    # On ignore le grand fichier de l'annonce si la taille est > 100ko
    if os.path.getsize(f) < 100000:
        small_files.append((f, os.path.getmtime(f)))

# Trier par date de création/modification
small_files.sort(key=lambda x: x[1])

print(f"=== FLIGHT SUPERVISOR : AUDIO RENAMER ===")
print(f"J'ai trouvé {len(small_files)} petits fichiers audio dans ton dossier.\n")

for i, (old_path, _) in enumerate(small_files):
    print(f"[{i+1}/{len(small_files)}] Lecture en cours...")
    
    # Joue le fichier audio avec le lecteur natif par défaut de Windows (invisiblement si possible)
    # Pour Windows, os.startfile l'ouvre dans le lecteur média. On va utiliser powershell pour jouer le son proprement sans GUI.
    play_cmd = f"powershell -c (New-Object Media.SoundPlayer '{old_path}').PlaySync();"
    # SoundPlayer ne lit pas les mp3. On peut utiliser wmplayer mais c'est intrusif.
    # L'alternative est de l'ouvrir et fermer :
    os.startfile(old_path)
    
    # Demande à l'utilisateur ce qu'il a entendu
    user_input = input("Qu'as-tu entendu ? (ex: 'oh_five', '15', 'o_clock', '8', ou 'skip' pour passer) : ").strip()
    
    if user_input.lower() == 'skip':
        continue
    if user_input.lower() == 'exit':
        break
        
    # Formatage du nom
    safe_name = user_input.replace(' ', '_').replace(':', '')
    new_name = f"time_{safe_name}.mp3"
    new_path = os.path.join(output_folder, new_name)
    
    # Déplacement
    try:
        os.rename(old_path, new_path)
        print(f"--> Renommé et déplacé : {new_name}\n")
    except Exception as e:
        print(f"Erreur lors du déplacement : {e}")

print("Opération terminée !")
