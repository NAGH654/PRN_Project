namespace Services.Dtos
{
    public class StorageOptions
    {
        public string Root { get; set; } = "storage";
        public string SevenZipPath { get; set; } = @"C:\Program Files\7-Zip\7z.exe"; // hoặc 7za
    }
}
