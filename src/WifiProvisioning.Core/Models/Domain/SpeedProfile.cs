namespace WifiProvisioning.Core.Models.Domain;
    public class SpeedProfile
    {
        public string Code { get; set; } = string.Empty;

        public int DownloadSpeedMbps { get; set; }

        public int UploadSpeedMbps { get; set; }
    }