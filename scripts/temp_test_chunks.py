import miniaudio
import math

input_file = r"d:\FlightSupervisor\assets\sounds\ElevenLabs_2026-03-27T21_59_46_Darine - Narrator_pvc_sp95_s68_sb67_f2-5.mp3"

decoded = miniaudio.mp3_read_file_f32(input_file)
sample_rate = decoded.sample_rate
channels = decoded.nchannels
samples = decoded.samples

window_ms = 30
window_samples = int((window_ms / 1000.0) * sample_rate * channels)
threshold_db = -38.0
threshold_amp = 10 ** (threshold_db / 20.0)

chunks = []
is_speaking = False
silence_duration = 0
min_silence_duration_samples = int(0.25 * sample_rate * channels)
current_start = 0

for i in range(0, len(samples), window_samples):
    window = samples[i:i+window_samples]
    rms = math.sqrt(sum(s*s for s in window) / max(1, len(window)))
    silence = rms < threshold_amp
    
    if is_speaking:
        if silence:
            silence_duration += window_samples
            if silence_duration >= min_silence_duration_samples:
                is_speaking = False
                chunks.append(1)
        else:
            silence_duration = 0
    else:
        if not silence:
            is_speaking = True
            silence_duration = 0
            
if is_speaking:
    chunks.append(1)
    
print(f"File duration: {len(samples) / (sample_rate * channels):.2f}s")
print(f"Total chunks detected: {len(chunks)}")
