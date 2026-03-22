using System;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json;
using FlightSupervisor.UI.Models.SimBrief;

namespace FlightSupervisor.UI.Services
{
    public class PanelServerService
    {
        private HttpListener? _listener;
        private Thread? _serverThread;
        private bool _isRunning = false;
        private readonly SimBriefService _simBriefService;
        private readonly WeatherBriefingService _weatherService;
        private readonly FlightPhaseManager _phaseManager;

        public PanelServerService(SimBriefService simBriefService, WeatherBriefingService weatherService, FlightPhaseManager phaseManager)
        {
            _simBriefService = simBriefService;
            _weatherService = weatherService;
            _phaseManager = phaseManager;
        }

        public void StartServer()
        {
            _listener = new HttpListener();
            _listener.Prefixes.Add("http://localhost:5000/");
            _listener.Prefixes.Add("http://127.0.0.1:5000/");
            _listener.Start();
            _isRunning = true;
            _serverThread = new Thread(Listen);
            _serverThread.IsBackground = true;
            _serverThread.Start();
        }

        public void StopServer()
        {
            _isRunning = false;
            if (_listener != null && _listener.IsListening)
            {
                _listener.Stop();
                _listener.Close();
            }
        }

        private async void Listen()
        {
            while (_isRunning)
            {
                try
                {
                    var context = await _listener!.GetContextAsync();
                    _ = ProcessRequestAsync(context);
                }
                catch
                {
                    // Listener stopped or encountered error
                }
            }
        }

        private async Task ProcessRequestAsync(HttpListenerContext context)
        {
            var request = context.Request;
            var response = context.Response;

            // Allow CORS for easy external HTML testing outside MSFS
            response.AppendHeader("Access-Control-Allow-Origin", "*");
            response.AppendHeader("Access-Control-Allow-Methods", "GET");

            try
            {
                if (request.Url?.AbsolutePath == "/api/simbrief")
                {
                    var username = request.QueryString["username"];
                    if (string.IsNullOrEmpty(username))
                    {
                        response.StatusCode = 400;
                        return;
                    }

                    var sbResponse = await _simBriefService.FetchFlightPlanAsync(username);
                    if (sbResponse != null)
                    {
                        var briefingText = _weatherService.GenerateSandboxBriefing(sbResponse.Weather?.OrigMetar ?? "", sbResponse.Weather?.DestMetar ?? "");

                        var data = new
                        {
                            departure = sbResponse.Origin?.IcaoCode ?? "----",
                            destination = sbResponse.Destination?.IcaoCode ?? "----",
                            route = sbResponse.General?.Route ?? "...",
                            level = sbResponse.General?.InitialAlt ?? "---",
                            briefing = briefingText
                        };

                        string jsonString = JsonSerializer.Serialize(data);
                        byte[] buffer = Encoding.UTF8.GetBytes(jsonString);
                        response.ContentType = "application/json";
                        response.ContentLength64 = buffer.Length;
                        await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
                    }
                    else
                    {
                        response.StatusCode = 404;
                    }
                }
                else
                {
                    response.StatusCode = 404;
                }
            }
            catch (Exception)
            {
                response.StatusCode = 500;
            }
            finally
            {
                response.Close();
            }
        }
    }
}
