using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace SpotifyCurrentlyListeningTo
{
    public class SongInformation
    {
        public string Name;
        public string Artist;
        public string Album;
        public string Duration;
        public string PlayedCount;
    }

    public class Application
    {
        #region apple scripts

        private const string TellSpotifyScript = "tell application \"Spotify\"";
        private const string EndTellScript = "end tell";
        private const string SongNameScript = "return name of current track";
        private const string SongArtistScript = "return artist of current track";
        private const string SongAlbumScript = "return album of current track";
        private const string SongDurationScript = "return duration of current track";
        private const string SongPlayedCountScript = "return played count of current track";

        #endregion

        private string PreviousSong;

        public void Run()
        {
            EnsureOutputDirectory();

            Console.WriteLine("Welcome to SpotifyCurrentlyListeningTo!");
            Console.WriteLine("This little handy tool will output spotify song informations into text file to be used by OBS or other streaming tools.");
            Console.WriteLine("To exit this program, press any key. Otherwise enjoy the songs flowing in. :-)");
            Console.WriteLine();
            SongLoop();
            Console.ReadKey();
        }

        private string GetOutputDirectory()
        {
            var applicationDirectory = Directory.GetCurrentDirectory();
            return Path.Combine(applicationDirectory, "Output");
        }

        private void EnsureOutputDirectory()
        {
            var outputDirectory = GetOutputDirectory();
            if (!Directory.Exists(outputDirectory))
            {
                Directory.CreateDirectory(outputDirectory);
            }
        }

        private void SongLoop()
        {
            Task.Run(async () =>
            {
                var songInformation = GetSongInformation();
                var consoleString = $"{songInformation.Name} - {songInformation.Artist} ({songInformation.Duration}, {songInformation.Album}) - {songInformation.PlayedCount}";

                if (consoleString != PreviousSong)
                {
                    Console.WriteLine(consoleString);
                    OutputSongInformationToTextFile(songInformation);
                    PreviousSong = consoleString;
                }

                await Task.Delay(500);
                SongLoop();
            });
        }

        private void OutputSongInformationToTextFile(SongInformation songInformation)
        {
            WriteTextToFile("name.txt", songInformation.Name);
            WriteTextToFile("album.txt", songInformation.Album);
            WriteTextToFile("artist.txt", songInformation.Artist);
            WriteTextToFile("duration.txt", songInformation.Duration);
            WriteTextToFile("playedCount.txt", songInformation.PlayedCount);
        }

        private void WriteTextToFile(string filename, string text)
        {
            using (var textWriter = File.CreateText(Path.Combine(GetOutputDirectory(), filename)))
            {
                textWriter.WriteLine(text);
            }
        }

        private SongInformation GetSongInformation() => new SongInformation()
        {
            Album = RunAppleScript(SongAlbumScript),
            Artist = RunAppleScript(SongArtistScript),
            Duration = GetDuration(),
            Name = RunAppleScript(SongNameScript),
            PlayedCount = RunAppleScript(SongPlayedCountScript)
        };

        private string GetDuration()
        {
            var durationStringInMs = RunAppleScript(SongDurationScript);

            if (int.TryParse(durationStringInMs, out int durationInMs))
            {
                return TimeSpan.FromMilliseconds(durationInMs).ToString(@"mm\:ss");
            }

            return string.Empty;
        }

        private string RunAppleScript(string script)
        {
            var process = new Process()
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "/usr/bin/osascript",
                    Arguments = "-l AppleScript",
                    RedirectStandardOutput = true,
                    RedirectStandardInput = true,
                    UseShellExecute = false
                }
            };
            process.Start();
            process.StandardInput.Write($"{TellSpotifyScript}\n{script}\n{EndTellScript}");
            process.StandardInput.Close();
            var result = process.StandardOutput.ReadToEnd();
            process.WaitForExit();
            return result.Replace("\n", "").Replace("\r", "");
        }
    }
}
