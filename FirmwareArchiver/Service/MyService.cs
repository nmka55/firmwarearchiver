using FirmwareArchiver.CustomComponents;
using System;
using System.Threading.Tasks;
using Flurl;
using Flurl.Http;
using FirmwareArchiver.DataModel;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using System.Net;
using System.ComponentModel;
using System.Linq;
using ShellProgressBar;
using System.Threading;
using System.IO;

namespace FirmwareArchiver.Service
{
    public static class MyService
    {
        public static bool TestConnection()
        {
            MyConsole.Info("Checking your internet connection...");

            List<string> listOfIPs = new() { "appldnld.apple.com", "api.ipsw.me" };
            Ping pingSender = new();
            PingReply pingReply;
            bool pingResult = true;

            foreach (string ip in listOfIPs)
            {
                pingReply = pingSender.Send(ip, 5000);
                MyConsole.Info($"Pinging: {ip} --> Result: {pingReply.Status}");

                if (pingReply == null || pingReply.Status != IPStatus.Success)
                {
                    // We failed on this attempt - no need to try any others
                    pingResult = false;
                    break;
                }
            }
            pingSender.Dispose();

            if (!pingResult)
            {
                MyConsole.Fail($"I'm sorry, looks like you're NOT CONNECTED to the internet or the servers went DOWN. Please try again. :( \nExiting...");
                Environment.Exit(0);
            }

            MyConsole.Success($"Your computer is CONNECTED to the internet :)\n");
            return pingResult;



        }

        public static async Task<DeviceList[]> GetDeviceListJSONAsync()
        {
            var url = "https://api.ipsw.me/v4/devices";

            MyConsole.Info($"Downloading device list from IPSW.me: {url}");

            DeviceList[] deviceList = await url.WithTimeout(15).GetJsonAsync<DeviceList[]>();

            if (deviceList == null || deviceList.Length <= 0)
            {
                MyConsole.Fail("Error download device list. Exiting...\n");
                Environment.Exit(0);
            }

            Array.Sort(deviceList, new DeviceList.DeviceComparer());
            MyConsole.Success("Downloading device list DONE\n");
            return deviceList;
        }

        public static async Task<FirmwareList> GetFirmwareListJSONAsync(string identifier)
        {
            var url = "https://api.ipsw.me/v4/device"
                                                .AppendPathSegment(identifier)
                                                .SetQueryParams(new
                                                {
                                                    type = "ipsw"

                                                });

            MyConsole.Info($"Downloading firmware list for {identifier}: {url}");

            FirmwareList firmwareList = await url
                                                .WithTimeout(15)
                                                .GetJsonAsync<FirmwareList>();

            if (firmwareList == null || firmwareList.Firmwares.Length <= 0)
            {
                MyConsole.Fail("Error download firmware list. Exiting...\n");
                Environment.Exit(0);
            }

            MyConsole.Success("Downloading firmware list DONE\n");
            return firmwareList;
        }

        public static async Task DownloadIPSW(Firmware[] firmwares, string downloadPath)
        {
            MyConsole.Info($"IPSW Download has started! \n");

            var options = new ProgressBarOptions
            {
                ForegroundColor = ConsoleColor.Yellow,
                BackgroundColor = ConsoleColor.DarkYellow,
                ProgressCharacter = '─'
            };
            var childOptions = new ProgressBarOptions
            {
                ForegroundColor = ConsoleColor.Yellow,
                ForegroundColorDone = ConsoleColor.DarkGreen,
                BackgroundColor = ConsoleColor.DarkGray,
                BackgroundCharacter = '\u2593'
            };

            using var pbar = new ProgressBar(firmwares.Length, $"Downloading {firmwares.Length} IPSW(s) for {firmwares[0].Identifier}", options);
            foreach (var (firmware, i) in firmwares.Select((f, i) => (f, i)))
            {
                string fileName = firmware.Url.ToString().Split('/').Last();

                //Check if file exists, if yes we check MD5 sum to make sure the file is complete.
                //If checking fails, we download it again, if it passes we move on to the next file
                if (File.Exists($"{downloadPath}/{fileName}"))
                {
                    MyConsole.Info("Duplicate file found in directory. Checking MD5Sum for file's integrity...");
                    bool isCorrectFile = MyFileHandler.CompareByMD5($"{downloadPath}/{fileName}", firmware.Md5Sum);
                    if (isCorrectFile)
                    {
                        MyConsole.Success($"MD5Sum check completed. File {downloadPath}/{fileName} is CORRECT. Moving on to the next file.");
                        continue;
                    }
                    else
                    {
                        MyConsole.Fail($"MD5Sum check completed. File {downloadPath}/{fileName} is INCORRECT. Redownloading the file.");
                    }
                }

                using var child = pbar.Spawn(100, $"Downloading file: {fileName}", childOptions);
                try
                {
                    using WebClient client = new WebClient();
                    client.DownloadProgressChanged += (o, args) =>
                                    child.Tick(args.ProgressPercentage,
                                    $"{ByteSizeLib.ByteSize.FromBytes(args.BytesReceived).ToString("#.#")}/" +
                                    $"{ByteSizeLib.ByteSize.FromBytes(args.TotalBytesToReceive).ToString("#.#")}. " +
                                    $"Downloading file no.{i + 1}: {fileName}");
                    client.DownloadFileCompleted += new AsyncCompletedEventHandler(DownloadFileCallback);


                    await client.DownloadFileTaskAsync(firmware.Url, $"{downloadPath}/{fileName}");

                    while (client.IsBusy)
                    {
                        Thread.Sleep(1);
                    }

                    pbar.Tick();
                }
                catch (WebException error)
                {
                    pbar.WriteLine(error.Message);
                }
            }
        }

        private static void DownloadFileCallback(object sender, AsyncCompletedEventArgs e)
        {
            if (e.Cancelled)
            {
                Console.WriteLine("File download cancelled.");
            }

            if (e.Error != null)
            {
                Console.WriteLine(e.Error.ToString());
            }

        }
    }
}
