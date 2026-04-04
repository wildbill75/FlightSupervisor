using System;
using System.Collections.Concurrent;
using System.Speech.Synthesis;
using System.Threading.Tasks;

namespace FlightSupervisor.UI.Services
{
    public enum SpeakerId { Captain, Purser, FO }

    public class AudioEngineService : IDisposable
    {
        private SpeechSynthesizer _captainSynth;
        private SpeechSynthesizer _pncSynth;
        private SpeechSynthesizer _foSynth;
        
        private ConcurrentQueue<(SpeakerId Speaker, string Text)> _queue = new();
        private bool _isPlaying = false;
        private readonly object _lock = new();

        public AudioEngineService()
        {
            _captainSynth = new SpeechSynthesizer();
            try { _captainSynth.SelectVoiceByHints(VoiceGender.Male, VoiceAge.Adult); } catch { /* Ignore if no voice */ }
            // Optional: Reduce speed slightly for clearer radio sound
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

        public void SpeakAsCaptain(string text)
        {
            _queue.Enqueue((SpeakerId.Captain, text));
            ProcessQueue();
        }

        public void SpeakAsPurser(string text)
        {
            _queue.Enqueue((SpeakerId.Purser, text));
            ProcessQueue();
        }

        public void SpeakAsFO(string text)
        {
            _queue.Enqueue((SpeakerId.FO, text));
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
                    // SpeakAsync doit être lancé pour ne pas bloquer le thread principal UI
                    Task.Run(() => 
                    {
                        if (item.Speaker == SpeakerId.Captain)
                        {
                            _captainSynth.SpeakAsync(item.Text);
                        }
                        else if (item.Speaker == SpeakerId.Purser)
                        {
                            _pncSynth.SpeakAsync(item.Text);
                        }
                        else if (item.Speaker == SpeakerId.FO)
                        {
                            _foSynth.SpeakAsync(item.Text);
                        }
                    });
                }
            }
        }

        private void OnSpeakCompleted(object sender, SpeakCompletedEventArgs e)
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
        }
    }
}
