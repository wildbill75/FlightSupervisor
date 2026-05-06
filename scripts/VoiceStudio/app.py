import os
import csv
import json
import urllib.request
import urllib.error
import wave
import numpy as np
from flask import Flask, render_template, request, jsonify, Response

app = Flask(__name__)

# Try loading audio effects
try:
    import soundfile as sf
    from pedalboard import Pedalboard, HighpassFilter, LowpassFilter, Compressor, Gain, Distortion
    HAS_EFFECTS = True
except ImportError:
    HAS_EFFECTS = False

API_KEY_ENV = "ELEVENLABS_API_KEY"
CSV_PATH = os.path.join(os.path.dirname(os.path.dirname(__file__)), "dialogues.csv")
OUTPUT_DIR = os.path.join(os.path.dirname(os.path.dirname(os.path.dirname(__file__))), "FlightSupervisor.UI", "wwwroot", "assets", "sounds", "airlines")

def get_api_key():
    key = os.environ.get(API_KEY_ENV)
    if not key:
        key = "sk_f5a5665bd581c997aa028729d2fd386e455995b0bd6f392c"
    return key

# -------------- ELEVENLABS API --------------

@app.route('/api/key_status', methods=['GET'])
def check_key():
    key = get_api_key()
    return jsonify({"has_key": bool(key), "has_effects": HAS_EFFECTS})

@app.route('/api/voices', methods=['GET'])
def get_voices():
    key = get_api_key()
    if not key:
        return jsonify({"error": "No API key"}), 401
        
    url = "https://api.elevenlabs.io/v1/voices"
    headers = {"xi-api-key": key}
    req = urllib.request.Request(url, headers=headers)
    
    def map_accent(labels):
        if not labels: return ""
        accent = labels.get("accent", "").lower()
        lang = labels.get("language", "").lower()
        if accent == "american": return "US"
        if accent == "british": return "GB"
        if accent == "australian": return "AU"
        if lang == "fr": return "FR"
        if lang == "de": return "DE"
        if lang == "es": return "ES"
        if accent: return accent[:2].upper()
        if lang: return lang.upper()
        return ""

    try:
        with urllib.request.urlopen(req) as response:
            data = json.loads(response.read().decode('utf-8'))
            voices = []
            for v in data.get("voices", []):
                # Clean name (remove extra descriptions like " - Laid-Back...")
                clean_name = v["name"].split("-")[0].strip()
                accent_tag = map_accent(v.get("labels", {}))
                display_name = f"{clean_name} ({accent_tag})" if accent_tag else clean_name
                voices.append({"voice_id": v["voice_id"], "name": display_name})
            return jsonify(voices)
    except Exception as e:
        return jsonify({"error": str(e)}), 500

# -------------- CSV MANAGEMENT --------------

def ensure_csv():
    if not os.path.exists(CSV_PATH):
        with open(CSV_PATH, 'w', newline='', encoding='utf-8') as f:
            writer = csv.writer(f)
            writer.writerow(["FileName", "Role", "VoiceID", "VoiceName", "Airline", "Language", "Effect", "Text"])

@app.route('/api/dialogues', methods=['GET'])
def get_dialogues():
    ensure_csv()
    dialogues = []
    with open(CSV_PATH, 'r', encoding='utf-8') as f:
        reader = csv.DictReader(f)
        for row in reader:
            dialogues.append(row)
    return jsonify(dialogues)

@app.route('/api/dialogues', methods=['POST'])
def save_dialogues():
    ensure_csv()
    data = request.json
    if not isinstance(data, list):
        return jsonify({"error": "Expected a list of dialogues"}), 400
        
    fieldnames = ["FileName", "Role", "VoiceID", "VoiceName", "Airline", "Language", "Effect", "Text"]
    
    with open(CSV_PATH, 'w', newline='', encoding='utf-8') as f:
        writer = csv.DictWriter(f, fieldnames=fieldnames)
        writer.writeheader()
        for row in data:
            # Ensure all keys exist
            clean_row = {k: row.get(k, "") for k in fieldnames}
            writer.writerow(clean_row)
            
    return jsonify({"success": True})

# -------------- AUDIO GENERATION --------------

def apply_audio_effect(filename_mp3, filename_wav, effect_type):
    if not HAS_EFFECTS:
        os.rename(filename_mp3, filename_wav.replace('.wav', '.mp3'))
        return "Sauvegardé en MP3 (sans effets)"
        
    try:
        audio_data, sample_rate = sf.read(filename_mp3)
        
        is_direct = not effect_type or effect_type.lower() == "direct"
        
        # Add background static noise only for radio/PA effects
        if not is_direct:
            noise_level = 0.002
            noise = np.random.normal(0, noise_level, len(audio_data)).astype(np.float32)
            if len(audio_data.shape) > 1:
                noise = np.column_stack((noise, noise))
            audio_data = audio_data + noise
        
        if effect_type and effect_type.lower() == "intercom":
            board = Pedalboard([
                HighpassFilter(cutoff_frequency_hz=800.0), 
                HighpassFilter(cutoff_frequency_hz=800.0), 
                LowpassFilter(cutoff_frequency_hz=1600.0), 
                Compressor(threshold_db=-30.0, ratio=8.0), 
                Distortion(drive_db=8.0), 
                Gain(gain_db=5.0)
            ])
            audio_data = board(audio_data, sample_rate)
            eff_msg = "Intercom appliqué"
        elif effect_type and effect_type.lower() == "pa":
            board = Pedalboard([
                HighpassFilter(cutoff_frequency_hz=600.0), 
                HighpassFilter(cutoff_frequency_hz=600.0), 
                LowpassFilter(cutoff_frequency_hz=2500.0), 
                Compressor(threshold_db=-20.0, ratio=4.0), 
                Gain(gain_db=2.0)
            ])
            audio_data = board(audio_data, sample_rate)
            eff_msg = "PA appliqué"
        elif effect_type and effect_type.lower() == "vhf":
            board = Pedalboard([
                HighpassFilter(cutoff_frequency_hz=300.0), 
                LowpassFilter(cutoff_frequency_hz=3000.0), 
                Distortion(drive_db=15.0), 
                Compressor(threshold_db=-25.0, ratio=6.0), 
                Gain(gain_db=3.0)
            ])
            audio_data = board(audio_data, sample_rate)
            eff_msg = "VHF appliqué"
        else:
            eff_msg = "Aucun effet (Direct)"
            
        sf.write(filename_wav, audio_data, sample_rate, format='WAV', subtype='PCM_16')
        os.remove(filename_mp3)
        return eff_msg
    except Exception as e:
        raise Exception(f"Erreur d'effet: {e}")

@app.route('/api/generate', methods=['POST'])
def generate_audio():
    key = get_api_key()
    if not key:
        return jsonify({"error": "No API key"}), 401
        
    req_data = request.json
    voice_id = req_data.get("VoiceID")
    text = req_data.get("Text")
    file_name = req_data.get("FileName")
    airline = req_data.get("Airline", "Generic").lower().replace(" ", "_")
    role = req_data.get("Role", "pnc").lower().replace(" ", "_")
    language = req_data.get("Language", "en").lower().replace(" ", "_")
    effect = req_data.get("Effect", "")
    
    if not voice_id or not text or not file_name:
        return jsonify({"error": "Missing parameters"}), 400
        
    if not file_name.lower().endswith(".wav"):
        file_name += ".wav"
        
    # For the new flattened structure we want: wwwroot/assets/sounds/{role}/{language}/{voice}/
    # The airline and destination are embedded in the file_name
    clean_voice_name = req_data.get("VoiceName", voice_id).lower().replace(" ", "_")
    target_folder = os.path.join(OUTPUT_DIR, role, language, clean_voice_name)
    os.makedirs(target_folder, exist_ok=True)
    output_filename_wav = os.path.join(target_folder, file_name)
    
    # Check if exists
    if os.path.exists(output_filename_wav):
        return jsonify({"status": "skipped", "message": "Fichier existant"})
        
    url = f"https://api.elevenlabs.io/v1/text-to-speech/{voice_id}?output_format=mp3_44100_128"
    headers = {
        "Accept": "audio/mpeg",
        "Content-Type": "application/json",
        "xi-api-key": key
    }
    data = {
        "text": text,
        "model_id": "eleven_multilingual_v2",
        "voice_settings": {
            "stability": 0.5,
            "similarity_boost": 0.75
        }
    }
    
    req_obj = urllib.request.Request(url, data=json.dumps(data).encode('utf-8'), headers=headers, method='POST')
    
    try:
        with urllib.request.urlopen(req_obj) as response:
            mp3_data = response.read()
            temp_mp3 = output_filename_wav.replace(".wav", "_temp.mp3")
            with open(temp_mp3, "wb") as f:
                f.write(mp3_data)
                
            eff_msg = apply_audio_effect(temp_mp3, output_filename_wav, effect)
            return jsonify({"status": "success", "message": f"Généré ({eff_msg})"})
            
    except urllib.error.HTTPError as e:
        return jsonify({"error": f"API Error: {e.code} - {e.read().decode('utf-8')}"}), 500
    except Exception as e:
        return jsonify({"error": str(e)}), 500

@app.route('/api/preview', methods=['POST'])
def preview_audio():
    key = get_api_key()
    if not key:
        return jsonify({"error": "No API key"}), 401
        
    req_data = request.json
    voice_id = req_data.get("VoiceID")
    text = req_data.get("Text")
    effect = req_data.get("Effect", "")
    
    if not voice_id or not text:
        return jsonify({"error": "Missing parameters"}), 400
        
    url = f"https://api.elevenlabs.io/v1/text-to-speech/{voice_id}?output_format=mp3_44100_128"
    headers = {
        "Accept": "audio/mpeg",
        "Content-Type": "application/json",
        "xi-api-key": key
    }
    data = {
        "text": text,
        "model_id": "eleven_multilingual_v2",
        "voice_settings": {
            "stability": 0.5,
            "similarity_boost": 0.75
        }
    }
    
    req_obj = urllib.request.Request(url, data=json.dumps(data).encode('utf-8'), headers=headers, method='POST')
    
    try:
        with urllib.request.urlopen(req_obj) as response:
            mp3_data = response.read()
            
            import tempfile
            with tempfile.NamedTemporaryFile(suffix=".mp3", delete=False) as f_mp3:
                f_mp3.write(mp3_data)
                temp_mp3 = f_mp3.name
                
            temp_wav = temp_mp3.replace(".mp3", ".wav")
            
            apply_audio_effect(temp_mp3, temp_wav, effect)
            
            actual_file = temp_wav
            if not os.path.exists(actual_file):
                actual_file = temp_wav.replace('.wav', '.mp3')
                
            with open(actual_file, "rb") as f:
                audio_bytes = f.read()
                
            os.remove(actual_file)
            
            mimetype = "audio/wav" if actual_file.endswith(".wav") else "audio/mpeg"
            return Response(audio_bytes, mimetype=mimetype)
            
    except urllib.error.HTTPError as e:
        return jsonify({"error": f"API Error: {e.code} - {e.read().decode('utf-8')}"}), 500
    except Exception as e:
        return jsonify({"error": str(e)}), 500

# -------------- FRONTEND --------------

@app.route('/')
def index():
    return render_template('index.html')

if __name__ == '__main__':
    app.run(port=5050, debug=True)
