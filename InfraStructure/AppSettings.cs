namespace WorkerService.InfraStructure
{
    public class AppSettings
    {
        public DirectorySettings[] Directories { get; set; }

        public class DirectorySettings
        {
            public string Path { get; set; }
            public string Filter { get; set; }
            public string DestinationFolder { get; set; }
        }
    }
}
