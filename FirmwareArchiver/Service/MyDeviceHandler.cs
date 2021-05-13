using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Xml.Linq;
using FirmwareArchiver.CustomComponents;
using FirmwareArchiver.DataModel;
using iMobileDevice;
using iMobileDevice.iDevice;
using iMobileDevice.Lockdown;
using iMobileDevice.Plist;

namespace FirmwareArchiver.Service
{
    public class MyDeviceHandler
    {
        public static ConnectedDevice CheckConnectedDevice()
        {
            MyConsole.Info("Checking for USB connected iDevices...");

            int count = 0;
            var idevice = LibiMobileDevice.Instance.iDevice;
            var lockdown = LibiMobileDevice.Instance.Lockdown;

            var ret = idevice.idevice_get_device_list(out ReadOnlyCollection<string> udids, ref count);

            if (ret == iDeviceError.NoDevice || count == 0)
            {
                MyConsole.Info("No connected iDevices at the moment.");
                return null;
            }

            ret.ThrowOnError();

            MyConsole.Info("Connected Devices:");

            // Get the info name
            List<string[]> printList = new();
            List<ConnectedDevice> connectedDeviceList = new();
            List<string> keyList = new() { "InternationalMobileEquipmentIdentity", "ProductType", "SerialNumber", "UniqueChipID", "HardwareModel" };
            int index = 0;

            foreach (var udid in udids)
            {
                idevice.idevice_new(out iDeviceHandle deviceHandle, udid).ThrowOnError();
                lockdown.lockdownd_client_new_with_handshake(deviceHandle, out LockdownClientHandle lockdownHandle, "Quamotion").ThrowOnError();
                lockdown.lockdownd_get_device_name(lockdownHandle, out string deviceName).ThrowOnError();

                ConnectedDevice connectedDevice = new() { DeviceName = deviceName, UniqueDeviceID = udid };

                foreach (string key in keyList)
                {
                    var result = lockdown.lockdownd_get_value(lockdownHandle, null, key, out PlistHandle plistHandle);

                    if (result == LockdownError.Success)
                    {
                        plistHandle.Api.Plist.plist_get_string_val(plistHandle, out string temp);
                        connectedDevice.GetType().GetProperty(key).SetValue(connectedDevice, temp);
                        plistHandle.Dispose();
                    }
                }

                connectedDeviceList.Add(connectedDevice);

                printList.Add(
                    new string[] {
                        index++.ToString(),
                        connectedDevice.DeviceName,
                        connectedDevice.ProductType,
                        connectedDevice.UniqueChipID,
                        connectedDevice.HardwareModel,
                        connectedDevice.InternationalMobileEquipmentIdentity,
                        connectedDevice.SerialNumber,
                        connectedDevice.UniqueDeviceID });

                deviceHandle.Dispose();
                lockdownHandle.Dispose();

            }


            MyConsole.Table(
                new string[] {
                    "Index",
                    "DeviceName",
                    "ProductType",
                     "UniqueChipID",
                    "HardwareModel",
                    "InternationalMobileEquipmentIdentity",
                    "SerialNumber",
                    "UDID"
                   }, printList);

            MyConsole.UserInteraction("Select connected device index or hit ENTER to select different device.");
            string input = Console.ReadLine();
            if (int.TryParse(input, out int inputValue) && inputValue >= 0 && inputValue <= count)
            {
                return connectedDeviceList[inputValue];
            }
            else
            {
                return null;
            }

        }

    }
}
