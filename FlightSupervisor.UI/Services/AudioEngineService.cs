using System;
using System.Collections.Concurrent;
using System.IO;
using System.Speech.Synthesis;
using System.Threading.Tasks;
using System.Windows;
using NAudio.Wave;

namespace FlightSupervisor.UI.Services
{
    public enum SpeakerId { Captain, Purser, FO }

    public class AudioRequest
    {
        public SpeakerId Speaker { get; set; }
        public string FallbackText { get; set; }
        public string FolderPath { get; set; }
        public string ExactFilePath { get; set; }
        public string PrefixFilter { get; set; }
        public bool IsAudioFile => !string.IsNullOrEmpty(FolderPath) || !string.IsNullOrEmpty(ExactFilePath);
    }

    public class AudioEngineService : IDisposable
    {
        private SpeechSynthesizer _captainSynth;
        private SpeechSynthesizer _pncSynth;
        private SpeechSynthesizer _foSynth;
        
        // NAudio player
        private WaveOutEvent _waveOut;
        private AudioFileReader _audioFile;

        private ConcurrentQueue<AudioRequest> _queue = new();
        private bool _isPlaying = false;
        private readonly object _lock = new();

        public int CaptainVolume { get; set; } = 100;
        public int PncVolume { get; set; } = 100;

        public AudioEngineService()
        {
            _waveOut = new WaveOutEvent();
            _waveOut.PlaybackStopped += (s, e) => {
                System.IO.File.AppendAllText(@"D:\FlightSupervisor\debug_audio.txt", $"[{DateTime.Now}] NAudio PlaybackStopped event triggered.\r\n");
                
                if (_audioFile != null) {
                    _audioFile.Dispose();
                    _audioFile = null;
                }
                
                FinishPlayback();
            };

            _captainSynth = new SpeechSynthesizer();
            try { _captainSynth.SelectVoiceByHints(VoiceGender.Male, VoiceAge.Adult); } catch { /* Ignore if no voice */ }
            _captainSynth.Rate = -1;
            _captainSynth.SpeakCompleted += OnSpeakCompleted;

            _pncSynth = new SpeechSynthesizer();
            try { _pncSynth.SelectVoiceByHints(VoiceGender.Female, VoiceAge.Adult); } catch { /* Ignore if no voice */ }
            _pncSynth.Rate = 0;
            _pncSynth.SpeakCompleted += OnSpeakCompleted;

            _foSynth = new SpeechSynthesizer();
            try { _foSynth.SelectVoiceByHints(VoiceGender.Female, VoiceAge.Senior); } catch { /* Ignore if no voice */ }
            _foSynth.Rate = 1;
            _foSynth.SpeakCompleted += OnSpeakCompleted;
        }

        // Backward compatibility
        public void SpeakAsCaptain(string text, bool playChime = true) => PlayAudio(SpeakerId.Captain, text, null, null, null, playChime);
        public void SpeakAsPurser(string text, bool playChime = true) => PlayAudio(SpeakerId.Purser, text, null, null, null, playChime);
        public void SpeakAsFO(string text, bool playChime = true) => PlayAudio(SpeakerId.FO, text, null, null, null, playChime);
        
        public void PlayVariantAsPurser(string folderRelativePath, string fallbackText, string prefix = null, bool playChime = true)
        {
            if (!folderRelativePath.StartsWith("EN_PNC_Beth/"))
                folderRelativePath = "EN_PNC_Beth/" + folderRelativePath;
            PlayAudio(SpeakerId.Purser, fallbackText, folderRelativePath, null, prefix, playChime);
        }

        public void PlayVariantAsFO(string folderRelativePath, string fallbackText, string prefix = null, bool playChime = true)
        {
            if (!folderRelativePath.StartsWith("EN_FO_Lucie/"))
                folderRelativePath = "EN_FO_Lucie/" + folderRelativePath;
            PlayAudio(SpeakerId.FO, fallbackText, folderRelativePath, null, prefix, playChime);
        }

        public void PlayVariantAsCaptain(string folderRelativePath, string fallbackText, string prefix = null, bool playChime = true)
        {
            if (!folderRelativePath.StartsWith("EN_FD_Rowan/"))
                folderRelativePath = "EN_FD_Rowan/" + folderRelativePath;
            PlayAudio(SpeakerId.Captain, fallbackText, folderRelativePath, null, prefix, playChime);
        }

        public void PlayVariantWithPrefixAsCaptain(string folderRelativePath, string prefixFilter, string fallbackText, bool playChime = true)
        {
            if (!folderRelativePath.StartsWith("EN_FD_Rowan/"))
                folderRelativePath = "EN_FD_Rowan/" + folderRelativePath;
            PlayAudio(SpeakerId.Captain, fallbackText, folderRelativePath, null, prefixFilter, playChime);
        }

        public void PlayExactAsCaptain(string exactRelativePath, string fallbackText, bool playChime = true)
        {
            if (!exactRelativePath.StartsWith("EN_FD_Rowan/"))
                exactRelativePath = "EN_FD_Rowan/" + exactRelativePath;
            PlayAudio(SpeakerId.Captain, fallbackText, null, exactRelativePath, null, playChime);
        }

        public void PlayExactAsFO(string exactRelativePath, string fallbackText, bool playChime = true)
        {
            if (!exactRelativePath.StartsWith("EN_FO_Lucie/"))
                exactRelativePath = "EN_FO_Lucie/" + exactRelativePath;
            PlayAudio(SpeakerId.FO, fallbackText, null, exactRelativePath, null, playChime);
        }

        public void PlayExactAsPurser(string exactRelativePath, string fallbackText, bool playChime = true)
        {
            if (!exactRelativePath.StartsWith("EN_PNC_Beth/"))
                exactRelativePath = "EN_PNC_Beth/" + exactRelativePath;
            PlayAudio(SpeakerId.Purser, fallbackText, null, exactRelativePath, null, playChime);
        }

        public void PlayAudio(SpeakerId speaker, string fallbackText, string folderRelativePath, string exactFilePath = null, string prefixFilter = null, bool playChime = true)
        {
            if (playChime)
            {
                string chimePathWav = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "wwwroot", "assets", "sounds", "pa_chime.wav");
                string chimePathMp3 = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "wwwroot", "assets", "sounds", "pa_chime.mp3");
                string selectedChime = System.IO.File.Exists(chimePathWav) ? chimePathWav : (System.IO.File.Exists(chimePathMp3) ? chimePathMp3 : null);

                if (selectedChime != null)
                {
                    _queue.Enqueue(new AudioRequest {
                        Speaker = speaker,
                        ExactFilePath = selectedChime
                    });
                }
            }

            _queue.Enqueue(new AudioRequest { 
                Speaker = speaker, 
                FallbackText = fallbackText, 
                FolderPath = folderRelativePath,
                ExactFilePath = exactFilePath,
                PrefixFilter = prefixFilter
            });
            ProcessQueue();
        }

        private void ProcessQueue()
        {
            lock (_lock)
            {
                if (_isPlaying || _queue.IsEmpty)
                    return;

                if (_queue.TryDequeue(out var item))
                {
                    _isPlaying = true;

                    string logText = $"[{DateTime.Now}] Processing queue for speaker {item.Speaker}.\r\n";

                    // First, try to resolve the Audio File
                    string selectedAudioFile = null;
                    if (item.IsAudioFile)
                    {
                        string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                        if (!string.IsNullOrEmpty(item.ExactFilePath))
                        {
                            string exactPath = Path.Combine(baseDir, "wwwroot", "assets", "sounds", item.ExactFilePath.Replace('/', '\\'));
                            logText += $"Resolving ExactFilePath: {exactPath}\r\n";
                            if (File.Exists(exactPath))
                            {
                                selectedAudioFile = exactPath;
                            }
                        }
                        else if (!string.IsNullOrEmpty(item.FolderPath))
                        {
                            string fullPathDir = Path.Combine(baseDir, "wwwroot", "assets", "sounds", item.FolderPath.Replace('/', '\\'));
                            logText += $"Resolving FolderPath: {fullPathDir}\r\n";

                            if (Directory.Exists(fullPathDir))
                            {
                                string filter = string.IsNullOrEmpty(item.PrefixFilter) ? "*.mp3" : $"{item.PrefixFilter}*.mp3";
                                var files = Directory.GetFiles(fullPathDir, filter);
                                logText += $"Found {files.Length} files matching {filter}.\r\n";
                                if (files.Length > 0)
                                {
                                    int idx = new Random().Next(files.Length);
                                    selectedAudioFile = files[idx];
                                }
                            }
                            else
                            {
                                logText += "Directory does not exist.\r\n";
                            }
                        }
                    }
                    else
                    {
                        logText += $"Not an audio file request. Fallback text: {item.FallbackText}\r\n";
                    }

                    if (selectedAudioFile != null)
                    {
                        logText += $"Selected Audio File: {selectedAudioFile}\r\n";
                        
                        // Play via NAudio
                        try
                        {
                            if (_audioFile != null) {
                                _audioFile.Dispose();
                                _audioFile = null;
                            }
                            
                            _audioFile = new AudioFileReader(selectedAudioFile);
                            
                            int vol = (item.Speaker == SpeakerId.Captain || item.Speaker == SpeakerId.FO) ? CaptainVolume : PncVolume;
                            _audioFile.Volume = (float)(vol / 100.0);
                            
                            _waveOut.Init(_audioFile);
                            
                            logText += $"Volume set to {vol / 100.0}. Playing via NAudio...\r\n";
                            System.IO.File.AppendAllText(@"D:\FlightSupervisor\debug_audio.txt", logText);
                            
                            _waveOut.Play();
                        }
                        catch (Exception ex)
                        {
                            System.IO.File.AppendAllText(@"D:\FlightSupervisor\debug_audio.txt", $"Exception in NAudio: {ex.Message}\r\n");
                            if (_audioFile != null) {
                                _audioFile.Dispose();
                                _audioFile = null;
                            }
                            FinishPlayback();
                        }
                    }
                    else
                    {
                        logText += "selectedAudioFile is null. Falling back to TTS...\r\n";
                        System.IO.File.AppendAllText(@"D:\FlightSupervisor\debug_audio.txt", logText);
                        
                        // Fallback to TTS 
                        Task.Run(() => 
                        {
                            try
                            {
                                switch (item.Speaker)
                                {
                                    case SpeakerId.Captain: 
                                        _captainSynth.Volume = CaptainVolume;
                                        _captainSynth.SpeakAsync(item.FallbackText ?? ""); 
                                        break;
                                    case SpeakerId.Purser: 
                                        _pncSynth.Volume = PncVolume;
                                        _pncSynth.SpeakAsync(item.FallbackText ?? ""); 
                                        break;
                                    case SpeakerId.FO: 
                                        _foSynth.Volume = CaptainVolume;
                                        _foSynth.SpeakAsync(item.FallbackText ?? ""); 
                                        break;
                                }
                            }
                            catch
                            {
                                FinishPlayback();
                            }
                        });
                    }
                }
            }
        }

        private void MediaPlayer_MediaEnded(object sender, EventArgs e)
        {
            FinishPlayback();
        }

        private void OnSpeakCompleted(object sender, SpeakCompletedEventArgs e)
        {
            FinishPlayback();
        }

        private void FinishPlayback()
        {
            lock (_lock)
            {
                _isPlaying = false;
            }
            ProcessQueue();
        }

        public void Dispose()
        {
            _captainSynth?.Dispose();
            _pncSynth?.Dispose();
            _foSynth?.Dispose();
            
            if (_audioFile != null) {
                _audioFile.Dispose();
                _audioFile = null;
            }
            
            _waveOut?.Dispose();
        }
    }
}
