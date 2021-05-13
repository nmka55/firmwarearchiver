using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;

namespace FirmwareArchiver.DataModel
{
    public partial class DeviceList
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("identifier")]
        public string Identifier { get; set; }

        [JsonProperty("boardconfig")]
        public string Boardconfig { get; set; }

        [JsonProperty("platform")]
        public string Platform { get; set; }

        [JsonProperty("cpid")]
        public long Cpid { get; set; }

        [JsonProperty("bdid")]
        public long Bdid { get; set; }

        public class DeviceComparer : IComparer
        {
            public int Compare(object x, object y)
            {
                return (new CaseInsensitiveComparer()).Compare(((DeviceList)x).Identifier, ((DeviceList)y).Identifier);
            }
        }

    }

    public partial class FirmwareList
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("identifier")]
        public string Identifier { get; set; }

        [JsonProperty("boardconfig")]
        public string Boardconfig { get; set; }

        [JsonProperty("platform")]
        public string Platform { get; set; }

        [JsonProperty("cpid")]
        public long Cpid { get; set; }

        [JsonProperty("bdid")]
        public long Bdid { get; set; }

        [JsonProperty("firmwares")]
        public Firmware[] Firmwares { get; set; }
    }

    public partial class Firmware
    {
        [JsonProperty("identifier")]
        public string Identifier { get; set; }

        [JsonProperty("version")]
        public string Version { get; set; }

        [JsonProperty("buildid")]
        public string Buildid { get; set; }

        [JsonProperty("sha1sum")]
        public string Sha1Sum { get; set; }

        [JsonProperty("md5sum")]
        public string Md5Sum { get; set; }

        [JsonProperty("filesize")]
        public long Filesize { get; set; }

        [JsonProperty("url")]
        public Uri Url { get; set; }

        [JsonProperty("releasedate")]
        public object Releasedate { get; set; }

        [JsonProperty("uploaddate")]
        public DateTimeOffset Uploaddate { get; set; }

        [JsonProperty("signed")]
        public bool Signed { get; set; }
    }

    public partial class ConnectedDevice
    {
        public string DeviceName { get; set; }

        public string ProductType { get; set; }

        public string UniqueChipID { get; set; }

        public string InternationalMobileEquipmentIdentity { get; set; }

        public string SerialNumber { get; set; }

        public string HardwareModel { get; set; }

        public string UniqueDeviceID { get; set; }

    }
}

