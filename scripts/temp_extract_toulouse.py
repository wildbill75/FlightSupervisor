import miniaudio
import wave
import struct
import math
import os

def write_wav(filename, samples, sample_rate, num_channels):
    raw_data = bytearray()
    for s in samples:
        val = int(max(-1.0, min(1.0, s)) * 32767)
        raw_data.extend(struct.pack("<h", val))
    with wave.open(filename, 'wb') as wf:
        wf.setnchannels(num_channels)
        wf.setsampwidth(2)
        wf.setframerate(sample_rate)
        wf.writeframes(raw_data)

input_file = r"d:\FlightSupervisor\assets\sounds\ElevenLabs_2026-03-27T21_00_14_Darine - Narrator_pvc_sp95_s68_sb67_f2-5.mp3"
output_path = r"d:\FlightSupervisor\FlightSupervisor.UI\wwwroot\assets\sounds\dest_toulouse_blagnac.wav"

try:
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
    min_silence_duration_samples = int(0.4 * sample_rate * channels)
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
                    end_idx = i 
                    chunks.append(samples[current_start:end_idx])
            else:
                silence_duration = 0
        else:
            if not silence:
                is_speaking = True
                silence_duration = 0
                current_start = max(0, i - int(0.3 * sample_rate * channels))
                
    if is_speaking:
        chunks.append(samples[current_start:])
        
    print(f"Total chunks detected: {len(chunks)}")
    
    if chunks:
        # Assuming Toulouse Blagnac is the very LAST chunk
        target_chunk = chunks[-1]
        write_wav(output_path, target_chunk, sample_rate, channels)
        print(f"Extracted the last chunk as: {output_path}")
    else:
        print("No chunks found!")
        
except Exception as e:
    print("Error:", e)
