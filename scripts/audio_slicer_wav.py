import miniaudio
import wave
import struct
import math
import os

TEMPLATES = {
    "welcome_pa_boarding": [
        "pa_welcome_intro",
        "airline_air_france",
        "pa_bound_for",
        "dest_toulouse_blagnac",
        "pa_welcome_luggage",
        "pa_welcome_seatbelts"
    ],
    "descent_pa_vars": [
        "pa_descent_intro",
        "pa_descent_secure"
    ],
    "arrival_pa_vars": [
        "pa_arrival_welcome",
        "pa_arrival_time_is",
        "pa_arrival_remain_seated"
    ]
}

def write_wav(filename, samples, sample_rate, num_channels):
    # Convert float [-1.0, 1.0] to int16
    raw_data = bytearray()
    for s in samples:
        val = int(max(-1.0, min(1.0, s)) * 32767)
        raw_data.extend(struct.pack("<h", val))
    
    with wave.open(filename, 'wb') as wf:
        wf.setnchannels(num_channels)
        wf.setsampwidth(2)
        wf.setframerate(sample_rate)
        wf.writeframes(raw_data)

def slice_audio(input_file, output_folder, template_name):
    if template_name not in TEMPLATES:
        print(f"Template {template_name} not found.")
        return
    expected = TEMPLATES[template_name]
    
    print(f"Decoding {input_file} (fallback to WAV format)...")
    decoded = miniaudio.mp3_read_file_f32(input_file)
    sample_rate = decoded.sample_rate
    channels = decoded.nchannels
    samples = decoded.samples
    
    print(f"Total duration: {len(samples) / (sample_rate * channels):.2f}s")
    
    # Silence detection parameters
    window_ms = 30
    window_samples = int((window_ms / 1000.0) * sample_rate * channels)
    threshold_db = -38.0  # Plus bas pour ne pas couper les fins de mots "soufflées" (résonance douce)
    threshold_amp = 10 ** (threshold_db / 20.0)
    
    # State machine
    chunks = []
    is_speaking = False
    silence_duration = 0
    min_silence_duration_samples = int(0.25 * sample_rate * channels) # 250ms minimum gap
    current_start = 0
    
    for i in range(0, len(samples), window_samples):
        window = samples[i:i+window_samples]
        rms = math.sqrt(sum(s*s for s in window) / max(1, len(window)))
        silence = rms < threshold_amp
        
        if is_speaking:
            if silence:
                silence_duration += window_samples
                if silence_duration >= min_silence_duration_samples:
                    # End of speech chunk
                    is_speaking = False
                    # On garde TOUT le silence détecté (400ms) à la fin du mot pour être sûr de ne rien couper
                    end_idx = i 
                    chunks.append(samples[current_start:end_idx])
            else:
                silence_duration = 0
        else:
            if not silence:
                is_speaking = True
                silence_duration = 0
                # On remonte 300ms en arrière pour prendre l'attaque douce du mot
                current_start = max(0, i - int(0.3 * sample_rate * channels))
                
    if is_speaking:
        chunks.append(samples[current_start:])
        
    print(f"Detected {len(chunks)} chunks.")
    if not os.path.exists(output_folder):
        os.makedirs(output_folder)
        
    for i, c in enumerate(chunks):
        if i < len(expected):
            name = expected[i]
        else:
            name = f"chunk_{i+1}"
        out_path = os.path.join(output_folder, f"{name}.wav")
        write_wav(out_path, c, sample_rate, channels)
        print(f"Saved: {out_path}")

if __name__ == "__main__":
    # --- 2. CONFIGURATION DE L'EXPORT (ARBORESCENCE) ---
    AIRLINE = "air_france"
    ROLE = "pnc"            # "pnc", "captain" ou "ground"
    LANGUAGE = "en"         # "en" ou "fr"
    VOICE = "female_1"      # Identité de la voix (ex: "female_1", "male_1")
    
    # Fichier envoyé par l'utilisateur :
    input_file = r"d:\FlightSupervisor\assets\sounds\ElevenLabs_2026-03-27T22_43_57_Darine - Narrator_pvc_sp95_s68_sb67_f2-5.mp3"
    
    # Construction automatique du chemin d'export structuré
    output_dir = rf"d:\FlightSupervisor\FlightSupervisor.UI\wwwroot\assets\sounds\airlines\{AIRLINE}\{ROLE}\{LANGUAGE}\{VOICE}"
    
    print(f"Destination: {output_dir}")
    try:
        slice_audio(input_file, output_dir, "welcome_pa_boarding")
    except Exception as e:
        print("ERROR:", e)
