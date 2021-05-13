using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using FirmwareArchiver.CustomComponents;
using FirmwareArchiver.DataModel;

namespace FirmwareArchiver.Service
{
    public class MyFileHandler
    {
        public static string downloadPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        public static string pathSuffix = "__FIRMWARES";

        public static bool CompareByMD5(string filepath, string md5sum)
        {
            // Using the. NET built-in MD5 Library
            using var md5 = MD5.Create();
            byte[] one;
            using (var fs1 = File.Open(filepath, FileMode.Open))
            {
                // Read the file content with FileStream and calculate the HASH value
                one = md5.ComputeHash(fs1);
            }
            // Converting MD5 results (byte arrays) into strings for comparison
            var computedMD5 = BitConverter.ToString(one).Replace("-", "").ToLower();
            MyConsole.Info($"Calculated hash: {computedMD5}, hash from IPSW.me server {md5sum}");
            return computedMD5.Equals(md5sum);
        }

        public static string ChangePath(string producttype)
        {
            downloadPath = Directory.CreateDirectory($"{downloadPath}/__FIRMWARES/{producttype}").ToString();
            pathSuffix = $"/__FIRMWARES/{producttype}";

            MyConsole.Info($"Current download folder is {downloadPath}. " +
                $"If you want to change the directory, enter the path below(e.g D:\\Data or /Volumes/myDrive). " +
                $"If not, hit ENTER");

            var path = Console.ReadLine();
            if (!string.IsNullOrEmpty(path))
            {
                string temp = path + pathSuffix;
                if (Directory.CreateDirectory(temp).Exists)
                {
                    downloadPath = temp;
                }
                else
                {
                    MyConsole.UserInteraction($"Entered path '{path}' does NOT EXISTS. Try again...");
                    ChangePath(producttype);
                }
            }

            MyConsole.Info($"Download folder set to: {downloadPath}");
            return downloadPath;
        }

        public static bool StorageChecker(Firmware[] firmwares)
        {
            long IPSWsize = firmwares.Select(item => item.Filesize).Sum();
            bool result = false;
            string pathRoot = Path.GetPathRoot(downloadPath);

            MyConsole.Info($"Checking disk free space of {pathRoot}");

            if (!string.IsNullOrEmpty(pathRoot))
            {
                DriveInfo driveInfo = new DriveInfo(pathRoot);
                long freeSpace = driveInfo.AvailableFreeSpace;

                MyConsole.Info($"This download will take {ByteSizeLib.ByteSize.FromBytes(IPSWsize).ToString("#.#")} " +
                    $"and you have {ByteSizeLib.ByteSize.FromBytes(freeSpace).ToString("#.#")} of free space");

                if (IPSWsize > freeSpace)
                {
                    MyConsole.Fail($"I'm sorry, you DO NOT HAVE ENOUGH free space. Exiting...");
                    result = false;
                    Environment.Exit(0);
                }

                MyConsole.Success("There is ENOUGH free space! :)");
                result = true;
            }
            else
            {
                MyConsole.UserInteraction($"Couldn't get the disk free space information. Do you still want to continue? (If disk gets full, downloads will FAIL.)");
                if (Console.ReadKey().Key == ConsoleKey.Enter) result = true;
            }

            return result;
        }

    }
}
