using Microsoft.Xna.Framework.Content.Pipeline;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.RuntimeBuilder;
using NPLEditor.Data;
using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;

namespace NPLEditor.Common
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
        public static JsonNode JsonObject;

        public static void Init()
        {
            NPLLog.LogDebugHeadline(FontAwesome.Igloo, "INITIALIZE CONTENT BUILDER");

            try
            {
                string jsonString;
                using (var fs = new FileStream(AppSettings.NPLJsonFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (var reader = new StreamReader(fs))
                {
                    jsonString = reader.ReadToEnd();
                }
                JsonObject = JsonNode.Parse(jsonString);
                
                ParseSettings();

                ContentRoot = JsonObject["root"].ToString();
                IntermediateDir = JsonObject["intermediateDir"].ToString();
                OutputDir = JsonObject["outputDir"].ToString();

                var platformParsed = Enum.TryParse<TargetPlatform>(JsonObject["platform"].ToString(), true, out var targetPlatform);
                TargetPlatform = platformParsed ? targetPlatform : TargetPlatform.DesktopGL;

                var graphicsParsed = Enum.TryParse<GraphicsProfile>(JsonObject["graphicsProfile"].ToString(), true, out var graphicsProfile);
                GraphicsProfile = graphicsParsed ? graphicsProfile : GraphicsProfile.Reach;

                var compressParsed = bool.TryParse(JsonObject["compress"].ToString(), out var compressed);
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
                ParseAllContentFiles();
            }
            catch (Exception e) { NPLLog.LogException(e, "ERROR", true); }

            Initialized = true;
            NPLLog.LogDebugHeadline(FontAwesome.Igloo, "CONTENT BUILDER INITIALIZED");
        }

        /// <summary>
        /// Parsing the meta-info of a .npl file.
        /// This also ensures that this data is written to a .npl file without this meta-info.
        /// </summary>
        private static void ParseSettings()
        {
            // Reorder process so that "settings" or meta-data will be at the top of the json file.
            // Since these are dictionary-entries we can not reorder the index like with lists unfortunately.
            //
            // 1. Save refs and remove content and references entries.
            var contentRef = JsonObject["content"];
            var referencesRef = JsonObject["references"];
            JsonObject.AsObject().Remove("content");
            JsonObject.AsObject().Remove("references");

            // 2. Create "settings" or meta-data entries if they are not exist.
            if (JsonObject["root"] == null) JsonObject["root"] = "";
            if (JsonObject["intermediateDir"] == null) JsonObject["intermediateDir"] = "obj";
            if (JsonObject["outputDir"] == null) JsonObject["outputDir"] = "bin";
            if (JsonObject["platform"] == null) JsonObject["platform"] = nameof(TargetPlatform.DesktopGL);
            if (JsonObject["graphicsProfile"] == null) JsonObject["graphicsProfile"] = nameof(GraphicsProfile.Reach);
            if (JsonObject["compress"] == null) JsonObject["compress"] = "true";

            // 3. Re-Add references and content entries.
            JsonObject.AsObject().Add("references", referencesRef);
            JsonObject.AsObject().Add("content", contentRef);
            
            // 4. Save the new json-file with the following order:
            // settings
            // references
            // content
            SaveContentNPL();
        }

        public static async Task BuildContent(bool rebuildNow = false)
        {
            NPLLog.LogDebugHeadline(FontAwesome.Igloo, "BUILD CONTENT");

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

                    NPLLog.LogDebugHeadline(FontAwesome.Igloo, "BUILD FINISHED");
                }
                else NPLLog.LogDebugHeadline(FontAwesome.Igloo, "BUILD CANCELD");
            }
            catch (Exception e) { NPLLog.LogException(e, "BUILD FAILED"); }

            CancelBuildContent = false;
            BuildContentRunning = false;
            RuntimeBuilder.Rebuild = false;
        }

        private static void ParseAllContentFiles()
        {
            var content = JsonObject["content"].AsObject().AsEnumerable();
            for (int i = 0; i < content.Count(); i++)
            {
                var data = content.ToArray()[i];
                var categoryObject = JsonObject["content"][data.Key];

                var importerName = data.Value["importer"]?.ToString();
                var processorName = data.Value["processor"]?.ToString();

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

        public static void GetAllContentFiles(out List<string> allCopyFiles, out List<string> allBuildFiles)
        {
            allCopyFiles = new List<string>();
            allBuildFiles = new List<string>();

            RuntimeBuilder.ClearAllDependencies();
            RuntimeBuilder.ClearAllImportersAndProcessors();

            ContentRoot = ContentRoot.Replace("\\", "/");
            if (ContentRoot.StartsWith("./"))
            {
                ContentRoot = ContentRoot.Substring(2, ContentRoot.Length - 2);
            }

            try
            {
                foreach (var nplItem in ContentList)
                {
                    string path = nplItem.Value.Path.ToString().Replace("\\", "/");
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

                    GetFilePath(path, out var fileName, out var filePath);

                    var searchOpt = SearchOption.TopDirectoryOnly;
                    if (nplItem.Value.Recursive)
                    {
                        searchOpt = SearchOption.AllDirectories;
                    }
                    else searchOpt = SearchOption.TopDirectoryOnly;

                    if (nplItem.Value.Action == Enums.BuildAction.Copy)
                    {
                        allCopyFiles.AddRange(Directory.GetFiles(filePath, fileName, searchOpt));
                    }
                    else if (nplItem.Value.Action == Enums.BuildAction.Build)
                    {
                        var nplItemBuildFiles = Directory.GetFiles(filePath, fileName, searchOpt);
                        allBuildFiles.AddRange(nplItemBuildFiles);

                        foreach (var nplItemBuildFile in nplItemBuildFiles)
                        {
                            RuntimeBuilder.SetImporterAndProcessor(
                                Path.GetFileName(nplItemBuildFile), 
                                nplItem.Value.Importer.TypeName, 
                                nplItem.Value.Processor.TypeName);
                        }

                        if (nplItem.Value.Dependencies != null)
                        {
                            foreach (var dependency in nplItem.Value.Dependencies)
                            {
                                GetFilePath(dependency, out var dependencyFileName, out var dependencyFilePath);
                                foreach (var nplItemBuildFile in nplItemBuildFiles)
                                {
                                    RuntimeBuilder.AddDependencies(Path.GetFileName(nplItemBuildFile), Directory.GetFiles(Path.Combine(filePath, dependencyFilePath), dependencyFileName, searchOpt).ToList());
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                NPLLog.LogException(e);
            }
        }

        private static void GetFilePath(string path, out string fileName, out string filePath)
        {
            fileName = Path.GetFileName(path);
            filePath = Path.GetDirectoryName(path);
            if (string.IsNullOrEmpty(filePath))
            {
                filePath = Directory.GetCurrentDirectory();
            }
        }

        private static void GetAllReferences()
        {
            RuntimeBuilder.ClearAllReferences();
            _tempReferences.Clear();

            var combinedReferences = new List<string>();
            foreach (var item in JsonObject["references"].AsArray())
            {
                _tempReferences.Add(item.ToString());

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
