using System;
using System.Threading.Tasks;

using FirmwareArchiver.DataModel;
using System.Drawing;
using System.Reflection;
using FirmwareArchiver.CustomComponents;
using FirmwareArchiver.Services;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace FirmwareArchiver
{
    class Program
    {
        static DeviceList[] deviceList;
        static string downloadPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        static string pathSuffix = "_FIRMWARES";

        static async Task Main(string[] args)
        {
            try
            {
                MyConsole.ASCII("FIRMWARE ARCHIVER", Color.DeepSkyBlue);
                MyConsole.Gradient($"Download all IPSWs (no OTA updates) for a device automatically. You don't have to start the download one by one." +
                    $"Made by @nmka55. " +
                    $"Version { Assembly.GetExecutingAssembly().GetName().Version}. \n \n");

                if (ServiceHelper.TestConnection())
                    deviceList = await ServiceHelper.GetDeviceListJSONAsync();

                await DeviceSelection();

            }
            catch (Exception ex)
            {
                MyConsole.Exception(ex.Message ?? "Oops... Something went wrong and my program has crashed or something. I'm sorry. Please try again. :)");
            }
        }

        static async Task DeviceSelection()
        {
            MyConsole.UserInteraction("Please select the device. Enter index of the device from the list.");
            var index = 0;

            List<string[]> tempList = new List<string[]>();

            foreach (var device in deviceList)
            {
                tempList.Add(
                    new string[] {  index++.ToString(),
                                    device.Identifier ?? "",
                                    device.Name ?? "",
                                    device.Boardconfig ?? "",
                                    device.Platform ?? "",
                                    device.Cpid.ToString(),
                                    device.Bdid.ToString()});
            }

            MyConsole.Table(new string[] { "Index", "Identifier", "Name", "BoardConfig", "Platform", "CPID", "BDID" }, tempList);


            string input = Console.ReadLine();
            if (int.TryParse(input, out int inputValue) && inputValue >= 0 && inputValue <= index)
            {
                await FirmwareSelection(deviceList[inputValue].Identifier);
            }
            else
            {
                MyConsole.Fail("Invalid index. Please check your device index and try again.\n");
                //await DeviceSelection();
            }
        }

        static async Task FirmwareSelection(string identifier)
        {
            FirmwareList firmwareList = await ServiceHelper.GetFirmwareListJSONAsync(identifier);

            MyConsole.Info("Device Info:");
            MyConsole.Info($"Name: {firmwareList.Name}");
            MyConsole.Info($"Identifier: {firmwareList.Identifier}");
            MyConsole.Info($"BoardConfig: {firmwareList.Boardconfig}");
            MyConsole.Info($"Platform: {firmwareList.Platform}");
            MyConsole.Info($"CPID: {firmwareList.Cpid}");
            MyConsole.Info($"BDID: {firmwareList.Bdid}");
            MyConsole.Info($"Available IPSWs for this device:");

            List<string[]> tempList = new List<string[]>();

            foreach (var firmware in firmwareList.Firmwares)
            {
                tempList.Add(
                    new string[] {  firmware.Version ?? "",
                                    firmware.Buildid ?? "",
                                    ByteSizeLib.ByteSize.FromBytes(firmware.Filesize).ToString("#.#"),
                                    firmware.Signed.ToString(),
                                    firmware.Releasedate != null ? firmware.Releasedate.ToString() : "",
                                    firmware.Md5Sum ?? "",
                                    firmware.Sha1Sum ?? ""});
            }

            MyConsole.Table(new string[] { "Version", "BuildID", "Size", "Signed", "Release Date", "MD5Sum", "SHA1Sum" }, tempList);

            downloadPath = Directory.CreateDirectory($"{downloadPath}/__FIRMWARES/{identifier}").ToString();
            pathSuffix = $"/__FIRMWARES/{identifier}";

            ChangePath();

            bool enoughStorage = StorageChecker(firmwareList.Firmwares);
            if (enoughStorage)
            {
                await ServiceHelper.DownloadIPSW(firmwareList.Firmwares, downloadPath);
                MyConsole.Success("FINISHED DOWNLOADING IPSWs. Good luck :)");
                Environment.Exit(0);
            }
        }

        static bool StorageChecker(Firmware[] firmwares)
        {
            long IPSWsize = firmwares.Select(item => item.Filesize).Sum();
            bool result = false;

            MyConsole.Info($"Download location: {downloadPath}");

            MyConsole.Info($"Checking disk free space...");

            if (!string.IsNullOrEmpty(Path.GetPathRoot(downloadPath)))
            {
                DriveInfo driveInfo = new DriveInfo(Path.GetPathRoot(downloadPath));
                long freeSpace = driveInfo.AvailableFreeSpace;

                MyConsole.Info($"This download will take {ByteSizeLib.ByteSize.FromBytes(IPSWsize).ToString("#.#")} and you have " +
                    $"{ByteSizeLib.ByteSize.FromBytes(freeSpace).ToString("#.#")} of free space");

                if (IPSWsize > freeSpace)
                {
                    MyConsole.Fail($"I'm sorry, you DO NOT HAVE ENOUGH free space. Exiting...");
                    result = false;
                    Environment.Exit(0);
                }
                else
                {
                    MyConsole.Success("There is ENOUGH free space! :)");
                    result = true;
                }

            }
            else
            {
                MyConsole.UserInteraction($"Couldn't get the disk free space information. Do you still want to continue? (If disk gets full downloads will FAIL.)");
                if (Console.ReadKey().Key == ConsoleKey.Enter) result = true;
            }

            return result;
        }


        static void ChangePath()
        {
            MyConsole.Info($"Current download folder is {downloadPath}. If you want to change the directory, enter the path below(e.g D:\\Data or /Volumes/myDrive). If not, press ENTER");

            var path = Console.ReadLine();
            if (!string.IsNullOrEmpty(path))
            {
                if (Directory.CreateDirectory(path + pathSuffix).Exists)
                {
                    downloadPath = path + pathSuffix;
                }
                else
                {
                    MyConsole.UserInteraction($"Entered path '{path}' does NOT EXISTS. Try again...");
                    ChangePath();
                }
            }

            MyConsole.Info($"Download folder set to: {downloadPath}");

        }


    }
}
