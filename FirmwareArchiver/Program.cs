using System;
using System.Threading.Tasks;

using FirmwareArchiver.DataModel;
using System.Drawing;
using System.Reflection;
using FirmwareArchiver.CustomComponents;
using System.Collections.Generic;
using iMobileDevice;
using FirmwareArchiver.Service;

namespace FirmwareArchiver
{
    class Program
    {
        static DeviceList[] deviceList;


        static async Task Main()
        {
            try
            {


                MyConsole.ASCII("FIRMWARE ARCHIVER", Color.DeepSkyBlue);
                MyConsole.Gradient($"Download all Restore IPSWs for a selected device automatically. " +
                    //$"Save SHSH2 Blobs of USB connected devices." +
                    $"Made by @nmka55. " +
                    $"Version { Assembly.GetExecutingAssembly().GetName().Version}. \n \n");

                NativeLibraries.Load();
                if (MyService.TestConnection())
                    deviceList = await MyService.GetDeviceListJSONAsync();

                ConnectedDevice connectedDevice = MyDeviceHandler.CheckConnectedDevice();
                if (connectedDevice != null) await FirmwareSelection(connectedDevice.ProductType);
                else await DeviceSelection();

            }
            catch (Exception ex)
            {
                MyConsole.Exception(ex.Message ?? "Oops... Something went wrong and program has crashed. I'm sorry. Please try again. :)");
            }
        }

        static async Task DeviceSelection()
        {
            var index = 0;

            List<string[]> tempList = new();
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

            MyConsole.UserInteraction("Please select the device. Enter index of the device from the list.");

            string input = Console.ReadLine();
            if (int.TryParse(input, out int inputValue) && inputValue >= 0 && inputValue <= index)
            {
                await FirmwareSelection(deviceList[inputValue].Identifier);
            }
            else
            {
                MyConsole.Fail("Invalid index. Please check your device index and try again.\n");
            }
        }

        static async Task FirmwareSelection(string identifier)
        {
            FirmwareList firmwareList = await MyService.GetFirmwareListJSONAsync(identifier);

            MyConsole.Info("Device Info:");
            MyConsole.Info($"Name: {firmwareList.Name}");
            MyConsole.Info($"Identifier: {firmwareList.Identifier}");
            MyConsole.Info($"BoardConfig: {firmwareList.Boardconfig}");
            MyConsole.Info($"Platform: {firmwareList.Platform}");
            MyConsole.Info($"CPID: {firmwareList.Cpid}");
            MyConsole.Info($"BDID: {firmwareList.Bdid}");
            MyConsole.Info($"Available IPSWs for this device:");

            List<string[]> tempList = new();
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


            string downloadPath = MyFileHandler.ChangePath(firmwareList.Name.Split(" ")[0] ?? "iDevice");

            bool enoughStorage = MyFileHandler.StorageChecker(firmwareList.Firmwares);
            if (enoughStorage)
            {
                await MyService.DownloadIPSW(firmwareList.Firmwares, downloadPath);
                MyConsole.Success("FINISHED DOWNLOADING IPSWs. Good luck :)");
                Environment.Exit(0);
            }
        }





    }
}
