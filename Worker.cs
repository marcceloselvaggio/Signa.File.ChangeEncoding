using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Unicode;
using System.Threading;
using System.Threading.Tasks;
using WorkerService.InfraStructure;

namespace WorkerService
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private List<FileSystemWatcher> _folderWatchers;
        private readonly IOptions<AppSettings> _appSettings;

        public Worker(ILogger<Worker> logger, IOptions<AppSettings> appSettings)
        {
            _logger = logger;
            _appSettings = appSettings;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            //while (!stoppingToken.IsCancellationRequested)
            //{
            //    _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
            //    await Task.Delay(1000, stoppingToken);
            //}
            await Task.CompletedTask;
        }

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            _logger.LogInformation("Service Starting");

            _folderWatchers = new List<FileSystemWatcher>();

            foreach (var directory in _appSettings.Value.Directories)
            {
                if (!Directory.Exists(directory.Path))
                {
                    _logger.LogWarning($"Please make sure the InputFolder [{directory.Path}] exists, then restart the service.");
                    //return Task.CompletedTask;
                    continue;
                }

                _logger.LogInformation($"Binding Events from Input Folder: {directory.Path}");

                FileSystemWatcher fw = new()
                {
                    Path = directory.Path,
                    IncludeSubdirectories = false,
                    Filter = directory.Filter,
                    NotifyFilter = NotifyFilters.FileName //| NotifyFilters.CreationTime | NotifyFilters.FileName
                };
                fw.Created += Input_OnChanged;
                fw.EnableRaisingEvents = true;
                fw.InternalBufferSize = 65536;

                _folderWatchers.Add(fw);
            }

            return base.StartAsync(cancellationToken);
        }

        protected void Input_OnChanged(object source, FileSystemEventArgs e)
        {
            if (e.ChangeType == WatcherChangeTypes.Created)
            {
                if (e.Name.StartsWith("OCO"))
                {
                    //string allLines;
                    //byte[] allBytes;

                    //using (var fs = File.Open(e.FullPath, FileMode.Open, FileAccess.Read, FileShare.Read))
                    //{
                    //    using StreamReader sr = new(fs);
                    //    allLines = sr.ReadToEnd();
                    //    //string[] lines = allLines.Split(Environment.NewLine);
                    //}

                    //File.WriteAllText(
                    //    Path.Combine(config.DestinationFolder, e.Name),
                    //    /*string.Join(Environment.NewLine, allLines)*/
                    //    //Encoding.Convert(Encoding.UTF8, Encoding.GetEncoding("us-ascii"), allLines),
                    //    Encoding.GetEncoding("us-ascii"));
                    //Thread.Sleep(2000);
                    int retries = 20;
                    const int delay = 500;

                    do
                    {
                        try
                        {
                            var config = _appSettings.Value.Directories
                                .FirstOrDefault(d => Directory.GetParent(e.FullPath).FullName == d.Path);

                            //var allLines = File.ReadAllText(e.FullPath, Encoding.Default);
                            //var allLines = File.ReadAllText(e.FullPath).Replace("\r\n", "\n").Replace("\r", "\n").Replace("\n", "\r\n");
                            var allLines = File.ReadAllText(e.FullPath).ReplaceLineEndings(Environment.NewLine);
                            
                            File.WriteAllText(Path.Combine(config.DestinationFolder, e.Name.Insert(e.Name.Length - 4, "-converted")), allLines);
                            
                            File.Delete(e.FullPath);
                            
                            continue;
                            //Encoding.Convert(Encoding.UTF8, Encoding.Latin1, Encoding.UTF8.GetBytes(allLines));

                            //using (FileStream fs = File.OpenRead(e.FullPath))
                            //{
                            //    Ude.CharsetDetector cdet = new Ude.CharsetDetector();
                            //    cdet.Feed(fs);
                            //    cdet.DataEnd();
                            //    return cdet.Charset;
                            //}


                            Encoding dstEncodingFormat = Encoding.GetEncoding("US-ASCII",
                                new EncoderExceptionFallback(),
                                new DecoderReplacementFallback());

                            byte[] output = dstEncodingFormat.GetBytes(allLines);
                            //byte[] output = Encoding.Convert(Encoding.UTF8, dstEncodingFormat, allLines);
                            //File.WriteAllBytes(Path.Combine(config.DestinationFolder, e.Name), output);
                            File.WriteAllText(Path.Combine(config.DestinationFolder, e.Name), Encoding.GetEncoding("us-ascii").GetString(output));

                            //Win 1252
                            allLines = File.ReadAllText(e.FullPath);

                            byte[] bytes = new byte[allLines.Length * sizeof(char)];
                            System.Buffer.BlockCopy(allLines.ToCharArray(), 0, bytes, 0, bytes.Length);

                            Encoding w1252 = dstEncodingFormat;
                            byte[] output2 = Encoding.Convert(Encoding.UTF8, w1252, bytes);

                            File.WriteAllText(Path.Combine(config.DestinationFolder, e.Name.Insert(e.Name.Length -4, "1252")),
                                w1252.GetString(output2));

                            byte[] asciiString = Encoding.ASCII.GetBytes(File.ReadAllText(e.FullPath));

                            File.WriteAllText(Path.Combine(config.DestinationFolder, e.Name.Insert(e.Name.Length - 4, "ASCII")),
                                Encoding.ASCII.GetString(asciiString));

                            byte[] w1252String = File.ReadAllBytes(e.FullPath);

                            //File.WriteAllText(Path.Combine(config.DestinationFolder, e.Name.Insert(e.Name.Length - 4, "w1252String")),
                            //    Encoding.GetEncoding(1252).GetString(w1252String));

                            File.WriteAllText(Path.Combine(config.DestinationFolder, e.Name.Insert(e.Name.Length - 4, "w1252String")),
                                Encoding.GetEncoding(1252).GetString(Encoding.Convert(Encoding.UTF8, Encoding.GetEncoding(1252), w1252String)));

                            File.WriteAllBytes(Path.Combine(config.DestinationFolder, e.Name.Insert(e.Name.Length - 4, "w1252Bytes")),
                                Encoding.Convert(Encoding.UTF8, Encoding.GetEncoding(1252), w1252String));

                            //File.WriteAllLines(
                            //    Path.Combine(config.DestinationFolder, e.Name),
                            //    //string.Join(Environment.NewLine, allLines),
                            //    Encoding.GetEncoding("us-ascii").GetString(Encoding.Convert(Encoding.UTF8, Encoding.GetEncoding("us-ascii"), Encoding.UTF8.GetBytes(allLines))).Split(Environment.NewLine),
                            //    Encoding.GetEncoding("us-ascii"));

                            //var content = Encoding.Convert(Encoding.Default, Encoding.GetEncoding("us-ascii"), File.ReadAllBytes(e.FullPath));

                            //File.WriteAllText(
                            //    Path.Combine(config.DestinationFolder, e.Name),
                            //    //string.Join(Environment.NewLine, allLines),
                            //    Encoding.GetEncoding("us-ascii").GetString(content),
                            //    Encoding.GetEncoding("us-ascii"));
                        }
                        catch (IOException ex)
                        {
                            if (ex.Message.StartsWith("The process cannot access the file"))
                            {
                                Thread.Sleep(delay);
                                continue;
                            }
                            else
                                break;
                        }

                        retries--;

                    } while (retries > 0);
                }
                //_logger.LogInformation($"InBound Change Event Triggered by [{e.FullPath}]");

                // do some work

                //_logger.LogInformation("Done with Inbound Change Event");
            }

            GC.Collect();
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Stopping Service");
            _folderWatchers.ForEach(f => f.EnableRaisingEvents = false);
            await base.StopAsync(cancellationToken);
        }

        public override void Dispose()
        {
            _logger.LogInformation("Disposing Service");
            _folderWatchers.ForEach(f => f.Dispose());
            base.Dispose();
            GC.Collect();
        }
    }
}
