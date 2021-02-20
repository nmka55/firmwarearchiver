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
using System.Security.Cryptography;

namespace FirmwareArchiver.Services
{
    public static class ServiceHelper
    {
        public static bool TestConnection()
        {
            MyConsole.Info("Before we get started let me check your internet connection...");

            List<string> listOfIPs = new List<string> { "appldnld.apple.com", "api.ipsw.me" };
            Ping pingSender = new Ping();
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

            if (pingResult)
            {
                MyConsole.Success($"Your computer is CONNECTED to the internet :)\n");
            }

            else
            {
                MyConsole.Fail($"I'm sorry, looks like you're NOT CONNECTED to the internet or the servers went DOWN. Please try again. :( \nExiting...");
                Environment.Exit(0);
            }

            return pingResult;

        }

        public static async Task<DeviceList[]> GetDeviceListJSONAsync()
        {
            MyConsole.Info("Downloading device list from IPSW.me...");
            MyConsole.Info("https://api.ipsw.me/v4/devices");

            DeviceList[] deviceList = await "https://api.ipsw.me/v4/devices".WithTimeout(20).GetJsonAsync<DeviceList[]>();

            if (deviceList != null && deviceList.Length > 0)
            {
                Array.Sort(deviceList, new DeviceList.DeviceComparer());
                MyConsole.Success("Downloading DONE\n");
            }

            else
            {
                MyConsole.Fail("Error download device list. Exiting...\n");
                Environment.Exit(0);
            }

            return deviceList;
        }

        public static async Task<FirmwareList> GetFirmwareListJSONAsync(string identifier)
        {
            MyConsole.Info($"Downloading firmware list for {identifier}");
            MyConsole.Info("https://api.ipsw.me/v4/device"
                                                .AppendPathSegment(identifier)
                                                .SetQueryParams(new
                                                {
                                                    type = "ipsw"

                                                }));

            FirmwareList firmwareList = await "https://api.ipsw.me/v4/device"
                                                .AppendPathSegment(identifier)
                                                .SetQueryParams(new
                                                {
                                                    type = "ipsw"

                                                })
                                                .WithTimeout(20)
                                                .GetJsonAsync<FirmwareList>();
            if (firmwareList != null && firmwareList.Firmwares.Length > 0)
            {
                MyConsole.Success("Downloading DONE\n");
            }

            else
            {
                MyConsole.Fail("Error download firmware list. Exiting...\n");
                Environment.Exit(0);
            }



            return firmwareList;
        }

        public static async Task DownloadIPSW(Firmware[] firmwares, string downloadPath)
        {
            MyConsole.Info($"Download has started! \n");

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
                    bool isCorrectFile = CompareByMD5($"{downloadPath}/{fileName}", firmware.Md5Sum);
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

        private static bool CompareByMD5(string file, string md5sum)
        {
            // Using the. NET built-in MD5 Library
            using var md5 = MD5.Create();
            byte[] one;
            using (var fs1 = File.Open(file, FileMode.Open))
            {
                // Read the file content with FileStream and calculate the HASH value
                one = md5.ComputeHash(fs1);
            }
            // Converting MD5 results (byte arrays) into strings for comparison
            var computedMD5 = BitConverter.ToString(one).Replace("-", "").ToLower();
            MyConsole.Info($"Calculated hash: {computedMD5}, hash from IPSW.me server {md5sum}");
            return computedMD5.Equals(md5sum);
        }


    }
}
