using System.Windows;
using FlightSupervisor.UI.Services;
using FlightSupervisor.UI.Models.SimBrief;

namespace FlightSupervisor.UI
{
    public partial class BriefingTesterWindow : Window
    {
        public BriefingTesterWindow()
        {
            InitializeComponent();
            
            // Default demo METAR
            MetarInput.Text = "KJFK 191451Z 31015G25KT 1/4SM R04R/1200V1500FT +SN FZFG VV002 M02/M05 A2992";
        }

        private void GenerateRandomWeather_Click(object sender, RoutedEventArgs e)
        {
            string[] metars = {
                "KJFK 191451Z 31015G25KT 1/4SM R04R/1200V1500FT +SN FZFG VV002 M02/M05 A2992", // Blizzard
                "EGLL 191220Z VRB02KT 9999 CAVOK 15/10 Q1013", // Clear
                "LFPG 190830Z 27010KT 0800 R27L/1000N FG BKN001 05/05 Q1012 NOSIG", // Fog
                "YSSY 190600Z 18035G50KT 3000 +TSRA BKN015CB 25/20 Q1000", // Storm
                "OMDB 190400Z 35012KT 5000 HZ NSC 40/25 Q1005", // Heat/Haze
                "EFHK 192220Z 04015KT 1200 -SN BLSN SCT005 OVC010 M10/M12 Q0998" // Snow
            };
            string[] tafs = {
                "TAF KJFK 191130Z 1912/2018 31015G25KT 1/2SM SN FZFG OVC005 \r\n  FM191800 32010KT 3SM -SN BR",
                "TAF EGLL 191100Z 1912/2018 VRB03KT 9999 SCT030 \r\n  PROB30 1914/1916 4000 SHRA",
                "TAF LFPG 190500Z 1906/2012 27008KT 0500 FG VV001 \r\n  BECMG 1909/1911 4000 BR",
                "TAF YSSY 190500Z 1906/2012 18030G45KT 3000 TSRA SCT010CB \r\n  FM190900 19020KT 9999 NSW",
                "TAF OMDB 190400Z 1906/2012 35015KT 5000 HZ \r\n  BECMG 1908/1910 32020G30KT 2000 DU",
                "TAF EFHK 191100Z 1912/2018 05015KT 2000 -SN OVC010 \r\n  TEMPO 1914/1917 0800 +SN VV003"
            };

            var rand = new System.Random();
            int idx = rand.Next(metars.Length);
            MetarInput.Text = metars[idx];
            TafInput.Text = tafs[idx];
        }

        private void GenerateButton_Click(object sender, RoutedEventArgs e)
        {
            var service = new WeatherBriefingService();
            OutputText.Text = service.GenerateSandboxBriefing(MetarInput.Text.Trim(), TafInput.Text.Trim());
        }
    }
}
