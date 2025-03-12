using Microsoft.Xna.Framework.Content.Pipeline;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.RuntimeBuilder;
using NPLEditor.Common;
using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using ContentItem = NPLEditor.Common.ContentItem;

namespace NPLEditor.Data
{
    public static partial class ContentBuilder
    {
        public static bool Initialized { get; set; } = false;

        public static Dictionary<string, ContentItem> ContentList = new();
        public static RuntimeBuilder RuntimeBuilder { get; set; }
        public static string OutputPath => Path.Combine(Path.GetFullPath(OutputDir), TargetPlatform.ToString());
        public static string IntermediatePath => Path.Combine(Path.GetFullPath(IntermediateDir), TargetPlatform.ToString());
        public static string OutputDir = "bin";
        public static string IntermediateDir = "obj";
        public static string ContentRoot = "";
        public static TargetPlatform TargetPlatform = TargetPlatform.DesktopGL;
        public static GraphicsProfile GraphicsProfile = GraphicsProfile.Reach;
        public static bool Compress = false;
        public static bool CancelBuildContent = false;
        public static bool BuildContentRunning = false;

        private static List<string> _tempReferences = [];
        internal static JsonNode _jsonObject;

        public static void Init()
        {
            try
            {
                string jsonString;
                using (var fs = new FileStream(AppSettings.NPLJsonFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (var reader = new StreamReader(fs))
                {
                    jsonString = reader.ReadToEnd();
                }
                _jsonObject = JsonNode.Parse(jsonString);

                ContentRoot = _jsonObject["root"]?.ToString() ?? "";
                IntermediateDir = _jsonObject["intermediateDir"]?.ToString() ?? "obj";
                OutputDir = _jsonObject["outputDir"]?.ToString() ?? "bin";

                var platformParsed = Enum.TryParse<TargetPlatform>(_jsonObject["platform"]?.ToString(), true, out var targetPlatform);
                TargetPlatform = platformParsed ? targetPlatform : TargetPlatform.DesktopGL;

                var graphicsParsed = Enum.TryParse<GraphicsProfile>(_jsonObject["graphicsProfile"]?.ToString(), true, out var graphicsProfile);
                GraphicsProfile = graphicsParsed ? graphicsProfile : GraphicsProfile.Reach;

                var compressParsed = bool.TryParse(_jsonObject["compress"]?.ToString(), out var compressed);
                Compress = compressParsed ? compressed : false;

                RuntimeBuilder = new RuntimeBuilder(
                    Directory.GetCurrentDirectory(),
                    IntermediatePath,
                    OutputPath,
                    TargetPlatform,
                    GraphicsProfile,
                    Compress)
                {
                    Logger = new NPLBuildLogger()
                };

                GetAllReferences();
                ParseAllContentFiles(_jsonObject["content"]);
            }
            catch (Exception e) { NPLLog.LogException(e, "ERROR", true); }

            Initialized = true;
        }

        public static async Task BuildContent(bool rebuildNow = false)
        {
            NPLLog.LogInfoHeadline(FontAwesome.Igloo, "BUILD CONTENT");

            RuntimeBuilder.Rebuild = rebuildNow;
            try
            {
                if (RuntimeBuilder.LaunchDebugger && !Debugger.IsAttached)
                {
                    try
                    {
                        Debugger.Launch();
                        var currentProcess = Process.GetCurrentProcess();
                        Log.Warning("Waiting for debugger to attach:");
                        Log.Warning($"({currentProcess.MainModule.FileName} PID {currentProcess.Id}).");
                        Log.Warning("Press the 'Cancel' button below to continue without debugger.");
                        while (!Debugger.IsAttached)
                        {
                            if (CancelBuildContent)
                            {
                                break;
                            }
                            Thread.Sleep(100);
                        }
                    }
                    catch (NotImplementedException e)
                    {
                        NPLLog.LogException(e, "The debugger is not implemented under Mono and thus is not supported on your platform.");
                    }
                }
                if (!CancelBuildContent)
                {
                    GetAllContentFiles(out var filesToCopy, out var filesToBuild);

                    RuntimeBuilder.RegisterCopyContent(filesToCopy.ToArray());
                    RuntimeBuilder.RegisterBuildContent(filesToBuild.ToArray());

                    await RuntimeBuilder.BuildContent();

                    NPLLog.LogInfoHeadline(FontAwesome.Igloo, "BUILD FINISHED");
                }
                else NPLLog.LogInfoHeadline(FontAwesome.Igloo, "BUILD CANCELD");
            }
            catch (Exception e) { NPLLog.LogException(e, "BUILD FAILED"); }

            CancelBuildContent = false;
            BuildContentRunning = false;
            RuntimeBuilder.Rebuild = false;
        }

        private static void ParseAllContentFiles(JsonNode jsonContent)
        {
            var content = jsonContent.AsObject().AsEnumerable();
            for (int i = 0; i < content.Count(); i++)
            {
                var data = content.ToArray()[i];
                var categoryObject = jsonContent[data.Key];

                var importerName = data.Value["importer"].ToString();
                var processorName = data.Value["processor"].ToString();

                PipelineTypes.GetTypeDescriptions(Path.GetExtension(data.Value["path"].ToString()),
                                out var outImporter, out var outProcessor);

                var nplItem = new ContentItem(data.Key, importerName, processorName);
                foreach (var categoryItem in categoryObject.AsObject())
                {
                    var itemKey = categoryItem.Key; //e.g. path
                    var itemValue = categoryItem.Value; //e.g. "C:\\"

                    nplItem.SetParameter(itemKey, itemValue);
                }
                ContentList.Add(data.Key, nplItem);
            }
        }

        public static void GetAllContentFiles(out List<string> copyFiles, out List<string> buildFiles)
        {
            copyFiles = new List<string>();
            buildFiles = new List<string>();

            ContentRoot = ContentRoot.Replace("\\", "/");
            if (ContentRoot.StartsWith("./"))
            {
                ContentRoot = ContentRoot.Substring(2, ContentRoot.Length - 2);
            }

            foreach (var item in ContentList)
            {
                string path = item.Value.Path.ToString().Replace("\\", "/");
                if (path.Contains("../"))
                {
                    throw new Exception("'path' is not allowed to contain '../'! Use 'root' property to specify a different root instead.");
                }

                if (path.StartsWith('$'))
                {
                    // $ means that the path will not have the root appended to it.
                    path = path.TrimStart('$');
                }
                else if (ContentRoot != "")
                {
                    // Appending root now so that we would work with full paths.
                    path = Path.Combine(ContentRoot, path);
                }

                var fileName = Path.GetFileName(path);
                var filePath = Path.GetDirectoryName(path);
                if (string.IsNullOrEmpty(filePath))
                {
                    filePath = Directory.GetCurrentDirectory();
                }

                try
                {
                    var searchOpt = SearchOption.TopDirectoryOnly;
                    if (item.Value.Recursive)
                    {
                        searchOpt = SearchOption.AllDirectories;
                    }
                    else searchOpt = SearchOption.TopDirectoryOnly;

                    if (item.Value.Action == Enums.BuildAction.Copy)
                    {
                        copyFiles.AddRange(Directory.GetFiles(filePath, fileName, searchOpt));
                    }
                    else if (item.Value.Action == Enums.BuildAction.Build)
                    {
                        buildFiles.AddRange(Directory.GetFiles(filePath, fileName, searchOpt));
                    }
                }
                catch (Exception e)
                {
                    NPLLog.LogException(e);
                }
            }
        }

        private static void GetAllReferences()
        {
            RuntimeBuilder.ClearAllReferences();

            var combinedReferences = new List<string>();
            foreach (var item in _jsonObject["references"].AsArray())
            {
                var reference = Environment.ExpandEnvironmentVariables(item.ToString());
                if (File.Exists(reference.ToString()))
                {
                    combinedReferences.Add(Path.GetFullPath(reference.ToString()));
                }
                else if (Directory.Exists(Path.GetDirectoryName(reference.ToString())))
                {
                    var dir = Path.GetDirectoryName(reference.ToString());
                    var matchingFiles = Directory.GetFiles(dir, "*.dll", SearchOption.AllDirectories).ToList();
                    foreach (var file in matchingFiles)
                    {
                        combinedReferences.Add(Path.GetFullPath(file));
                    }
                }
                else
                {
                    var matchingFiles = Directory.GetFiles(Directory.GetCurrentDirectory(), "*.dll", SearchOption.AllDirectories).ToList();
                    foreach (var file in matchingFiles)
                    {
                        combinedReferences.Add(Path.GetFullPath(file));
                    }
                }
            }
            var references = combinedReferences.Distinct()
                .Where(x => !string.IsNullOrEmpty(x.ToString())).ToArray();
            PipelineTypes.Load(references);
            RuntimeBuilder.AddReferences(references);
        }
    }
}
