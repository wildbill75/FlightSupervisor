import os
from pydub import AudioSegment
from pydub.silence import split_on_silence

# DEPENDENCIES: pip install pydub
# REQUIRED: ffmpeg (doit être installé sur le système et dans le PATH)

# --- 1. CONFIGURATION DES TEMPLATES ---
# C'est ici que tu définis l'ordre exact de tes mots dans le fichier audio maitre
TEMPLATES = {
    # Exemple 1 : L'annonce du commandant
    "welcome_pa_airfrance_orly": [
        "pa_welcome_intro",       # Segment 1 : "Bienvenue à bord de ce vol"
        "airline_air_france",     # Segment 2 : "Air France"
        "pa_bound_for",           # Segment 3 : "à destination de"
        "dest_paris_orly"         # Segment 4 : "Paris Orly"
    ],
    
    # Exemple 2 : Génération de chiffres à la chaîne
    "les_chiffres": [
        f"num_{i}" for i in range(1, 10) # Générera num_1, num_2, num_3...
    ]
}

def process_smart_slicer(input_file, output_folder, template_name):
    if template_name not in TEMPLATES:
        print(f"❌ Erreur : Le template '{template_name}' n'existe pas dans la conf.")
        return

    expected_names = TEMPLATES[template_name]
    
    print(f"Chargement de '{input_file}'...")
    sound = AudioSegment.from_mp3(input_file)
    
    print("Découpage basé sur les silences...")
    # split_on_silence parameters:
    # min_silence_len=500 -> Il faut un VRAI silence de 0.5s pour séparer
    # silence_thresh -> Le niveau max (en db) pour être considéré comme un silence
    chunks = split_on_silence(
        sound,
        min_silence_len=500,
        silence_thresh=sound.dBFS - 15, 
        keep_silence=200 # Garde 200ms de queue pour que ça ne coupe pas trop sec
    )
    
    if not os.path.exists(output_folder):
        os.makedirs(output_folder)
        
    print(f"-> Détecté : {len(chunks)} segments vocaux.")
    print(f"-> Attendu : {len(expected_names)} segments selon le template '{template_name}'.")
    
    if len(chunks) != len(expected_names):
        print("⚠️ DÉCALAGE DÉTECTÉ ! Le nombre de silences ne correspond pas au template.")
        print("Je les exporte dans un dossier debug_fallbacks/ au lieu de mal les nommer :")
        debug_folder = os.path.join(output_folder, "debug_fallbacks")
        if not os.path.exists(debug_folder): os.makedirs(debug_folder)
        
        for i, chunk in enumerate(chunks):
            chunk.export(os.path.join(debug_folder, f"segment_inconnu_{i+1}.mp3"), format="mp3")
        return

    print("✅ Correspondance parfaite ! Export et renommage automatique en cours...")
    for i, chunk in enumerate(chunks):
        out_name = expected_names[i]
        out_path = os.path.join(output_folder, f"{out_name}.mp3")
        chunk.export(out_path, format="mp3")
        print(f"  -> Sauvegardé : {out_name}.mp3")

if __name__ == "__main__":
    # --- 2. EXECUTION ---
    # Fichier envoyé par l'utilisateur :
    input_file = r"d:\FlightSupervisor\assets\sounds\ElevenLabs_2026-03-27T19_07_42_Darine - Narrator_pvc_sp95_s68_sb67_f2-5.mp3"
    output_folder = r"d:\FlightSupervisor\FlightSupervisor.UI\wwwroot\assets\sounds"
    
    if os.path.exists(input_file):
        # On utilise le template "welcome_pa_airfrance_orly"
        process_smart_slicer(input_file, output_folder, "welcome_pa_airfrance_orly")
        print("\nTerminé !")
    else:
        print(f"Le fichier '{input_file}' est introuvable. Mets-le dans le même dossier que ce script.")
