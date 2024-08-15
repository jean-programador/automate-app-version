namespace AutomacaoVersaoTask
{
    public class UploadOptions
    {
        public string FileName { get; set; }

        public string FilePath { get; set; }

        public VersionTaskType VersionType { get; set; }
    }

    public enum VersionTaskType
    {
        OficialVersion = 1,
        CompleteVersion = 2,
        CompleteTaskVersion = 3,
        CompactTaskVersion = 4,
        CompactTaskCompleteBrainVersion = 5,
    }
}
