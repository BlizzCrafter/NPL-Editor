using ImGuiNET;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content.Pipeline;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.RuntimeBuilder;
using NPLEditor.Common;
using NPLEditor.Data;
using NPLEditor.Enums;
using NPLEditor.GUI;
using Serilog;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Color = Microsoft.Xna.Framework.Color;
using ContentItem = NPLEditor.Common.ContentItem;
using Num = System.Numerics;

namespace NPLEditor
{
    public class Main : Game
    {
        public static Dictionary<string, ContentItem> ContentList = new();
        public static bool ScrollLogToBottom { get; set; }

        private GraphicsDeviceManager _graphics;
        private ImGuiRenderer _imGuiRenderer;
        private JsonNode _jsonObject;
        private RuntimeBuilder _runtimeBuilder { get; set; }
        private TargetPlatform _targetPlatform;
        private GraphicsProfile _graphicsProfile;
        private string _outputDir = "bin";
        private string _intermediateDir = "obj";
        private bool _compress = false;
        private bool _buildContentRunning = false;
        private bool _clearLogViewOnBuild = true;
        private bool _launchDebuggerContent = false;
        private bool _incrementalContent = false;
        private bool _cancelBuildContent = false;
        private bool _treeNodesOpen = true;
        private bool _logOpen = false;
        private bool _settingsVisible = true;
        private bool _orderingOptionsVisible = false;
        private bool _dummyBoolIsOpen = true;
        private string _nplJsonFilePath;

        public Main()
        {
            _graphics = new GraphicsDeviceManager(this);
            _graphics.PreferredBackBufferWidth = 800;
            _graphics.PreferredBackBufferHeight = 500;

            // Currently not usable in DesktopGL because of this bug:
            // https://github.com/MonoGame/MonoGame/issues/7914
            //_graphics.PreferMultiSampling = true;

            Window.AllowUserResizing = true;
            IsMouseVisible = true;

            Content.RootDirectory = "Content";
        }

        protected override void Initialize()
        {
            Window.Title = AppSettings.Title;

            _graphics.GraphicsDevice.PresentationParameters.BackBufferWidth = _graphics.PreferredBackBufferWidth;
            _graphics.GraphicsDevice.PresentationParameters.BackBufferHeight = _graphics.PreferredBackBufferHeight;

            _imGuiRenderer = new ImGuiRenderer(this);

            base.Initialize();

            NPLLog.LogInfoHeadline(FontAwesome.FlagCheckered, "APP-INITIALIZED");
        }

        protected override void LoadContent()
        {
#if DEBUG
            string projectDir = Directory.GetParent(AppSettings.LocalContentPath).Parent.Parent.FullName;
            string contentDir = Path.Combine(projectDir, "Content");
            _nplJsonFilePath = Path.Combine(contentDir, "Content.npl");

            Directory.SetCurrentDirectory(contentDir);
#else
            Directory.SetCurrentDirectory(Path.Combine(Directory.GetCurrentDirectory(), "Content"));

            string[] args = Environment.GetCommandLineArgs();
            _nplJsonFilePath = args[1];
            Log.Verbose($"Launch Arguments: {args}");
#endif

            var intermediateBuildDir = Path.GetFullPath(_intermediateDir);
            var outputBuildDir = Path.GetFullPath(_outputDir);

            Log.Debug($"WorkingDir: {Directory.GetCurrentDirectory()}");
            Log.Debug($"IntermediateDir: {intermediateBuildDir}");
            Log.Debug($"OutputDir: {outputBuildDir}");
            Log.Debug($"LocalContentDir: {AppSettings.LocalContentPath}");

            _runtimeBuilder = new RuntimeBuilder(
                Directory.GetCurrentDirectory(),
                intermediateBuildDir,
                outputBuildDir,
                TargetPlatform.DesktopGL,
                GraphicsProfile.Reach,
                true)
            {
                Logger = new NPLBuildLogger()
            };

            try
            {
                var jsonString = File.ReadAllText(_nplJsonFilePath);
                _jsonObject = JsonNode.Parse(jsonString);
            }
            catch (Exception e) { NPLLog.LogException(e, "ERROR", true); }
            base.LoadContent();
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Black);

            _imGuiRenderer.BeforeLayout(gameTime);
            ImGuiLayout();
            _imGuiRenderer.AfterLayout();

            base.Draw(gameTime);
        }

        protected virtual void ImGuiLayout()
        {
            var mainWindowFlags =
                ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoResize
                | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoBringToFrontOnFocus | ImGuiWindowFlags.NoNavFocus
                | ImGuiWindowFlags.NoBackground;

            var windowFlags = ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoDecoration
                | ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.MenuBar | ImGuiWindowFlags.AlwaysVerticalScrollbar;

            ImGuiViewportPtr viewport = ImGui.GetMainViewport();

            ImGui.SetNextWindowPos(viewport.Pos);
            ImGui.SetNextWindowSize(viewport.Size);
            ImGui.SetNextWindowViewport(viewport.ID);
            if (ImGui.Begin("Main", ref _dummyBoolIsOpen, mainWindowFlags))
            {
                ImGui.SetNextWindowPos(viewport.Pos);
                ImGui.SetNextWindowSize(viewport.Size);
                ImGui.SetNextWindowViewport(viewport.ID);
                if (ImGui.Begin("Content", ref _dummyBoolIsOpen, windowFlags))
                {
                    MenuBar();

                    if (!_logOpen)
                    {
                        if (_settingsVisible)
                        {
                            EditButton(EditButtonPosition.Before, FontAwesome.Edit, -1, true, true);

                            ImGui.SetCursorPosX(ImGui.GetStyle().IndentSpacing + ImGui.GetStyle().ItemSpacing.X);
                            ImGui.PushStyleColor(ImGuiCol.Text, ImGui.GetStyle().Colors[(int)ImGuiCol.NavHighlight]);
                            ImGui.PushItemWidth(ImGui.GetContentRegionAvail().X - 150 + ImGui.GetStyle().IndentSpacing);
                            if (ImGui.TreeNodeEx("settings", ImGuiTreeNodeFlags.DefaultOpen | ImGuiTreeNodeFlags.SpanAllColumns))
                            {
                                ImGui.PopStyleColor();

                                var root = _jsonObject["root"];
                                var rootValue = ContentReader.contentRoot = root.ToString();
                                if (ImGui.InputText("root", ref rootValue, 9999, ImGuiInputTextFlags.EnterReturnsTrue))
                                {
                                    _jsonObject["root"] = rootValue;
                                    ContentReader.contentRoot = rootValue;
                                    WriteContentNPL();
                                }

                                var intermediateDir = _jsonObject["intermediateDir"]?.ToString();
                                if (intermediateDir == null)
                                {
                                    _jsonObject["intermediateDir"] = _intermediateDir;
                                    intermediateDir = _jsonObject["intermediateDir"].ToString();
                                    _runtimeBuilder.SetIntermediateDir(_intermediateDir);
                                }
                                if (ImGui.InputText("intermediateDir", ref intermediateDir, 9999, ImGuiInputTextFlags.EnterReturnsTrue))
                                {
                                    _jsonObject["intermediateDir"] = _intermediateDir = intermediateDir;
                                    _runtimeBuilder.SetIntermediateDir(_intermediateDir);
                                    WriteContentNPL();
                                }

                                var outputDir = _jsonObject["outputDir"]?.ToString();
                                if (outputDir == null)
                                {
                                    _jsonObject["outputDir"] = _outputDir;
                                    outputDir = _jsonObject["outputDir"].ToString();
                                    _runtimeBuilder.SetOutputDir(_outputDir);
                                }
                                if (ImGui.InputText("outputDir", ref outputDir, 9999, ImGuiInputTextFlags.EnterReturnsTrue))
                                {
                                    _jsonObject["outputDir"] = _outputDir = outputDir;
                                    _runtimeBuilder.SetOutputDir(_outputDir);
                                    WriteContentNPL();
                                }

                                var platform = _jsonObject["platform"]?.ToString();
                                if (platform == null)
                                {
                                    _jsonObject["platform"] = _targetPlatform.ToString();
                                    platform = _jsonObject["platform"].ToString();
                                    _runtimeBuilder.SetPlatform(_targetPlatform);
                                }
                                if (ComboEnum(ref platform, "platform", Enum.GetNames(typeof(TargetPlatform))))
                                {
                                    _jsonObject["platform"] = platform;
                                    _targetPlatform = Enum.Parse<TargetPlatform>(platform);
                                    _runtimeBuilder.SetPlatform(_targetPlatform);
                                    WriteContentNPL();
                                }

                                var graphicsProfile = _jsonObject["graphicsProfile"]?.ToString();
                                if (graphicsProfile == null)
                                {
                                    _jsonObject["graphicsProfile"] = _graphicsProfile.ToString();
                                    graphicsProfile = _jsonObject["graphicsProfile"].ToString();
                                    _runtimeBuilder.SetGraphicsProfile(_graphicsProfile);
                                }
                                if (ComboEnum(ref graphicsProfile, "graphicsProfile", Enum.GetNames(typeof(GraphicsProfile))))
                                {
                                    _jsonObject["graphicsProfile"] = graphicsProfile;
                                    _graphicsProfile = Enum.Parse<GraphicsProfile>(graphicsProfile);
                                    _runtimeBuilder.SetGraphicsProfile(_graphicsProfile);
                                    WriteContentNPL();
                                }

                                var compress = _jsonObject["compress"]?.ToString();
                                if (compress == null)
                                {
                                    _jsonObject["compress"] = _compress.ToString();
                                    compress = _jsonObject["compress"].ToString();
                                    _runtimeBuilder.SetCompressContent(_compress);
                                }
                                else _compress = bool.Parse(compress);

                                if (ImGui.Checkbox("compress", ref _compress))
                                {
                                    _jsonObject["compress"] = compress = _compress.ToString();
                                    _runtimeBuilder.SetCompressContent(_compress);
                                }

                                var references = _jsonObject["references"].AsArray();
                                ArrayEditor("Reference", references, out _, out bool itemRemoved, out bool itemChanged);
                                {
                                    if (itemChanged || itemRemoved)
                                    {
                                        PipelineTypes.Reset();
                                        _runtimeBuilder.ClearAllReferences();
                                    }
                                }

                                if (PipelineTypes.IsDirty)
                                {
                                    var combinedReferences = new List<string>();
                                    foreach (var item in references)
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
                                    var combinedReferencesArray = combinedReferences.Distinct()
                                        .Where(x => !string.IsNullOrEmpty(x.ToString()))
                                        .ToArray();
                                    PipelineTypes.Load(combinedReferencesArray);
                                    _runtimeBuilder.AddReferences(combinedReferencesArray);
                                }
                                ImGui.TreePop();
                            }
                            ImGui.PopStyleColor();
                            ImGui.PopItemWidth();
                        }

                        JsonObject modifiedProcessorParam = null;
                        var jsonContent = _jsonObject["content"].AsObject().AsEnumerable();
                        for (int i = 0, id = 0; i < jsonContent.Count(); i++, id--)
                        {
                            var data = jsonContent.ToArray()[i];
                            var editButtonCount = _orderingOptionsVisible ? 1 : 0;

                            if (_orderingOptionsVisible)
                            {
                                editButtonCount++;
                                if (EditButton(EditButtonPosition.Before, FontAwesome.ArrowDown, id, true, i == jsonContent.Count() - 1))
                                {
                                    MoveTreeItem(i, true);
                                }
                                editButtonCount++;
                                if (EditButton(EditButtonPosition.Before, FontAwesome.ArrowUp, id - 1, true, i == 0))
                                {
                                    MoveTreeItem(i, false);
                                }
                            }
                            editButtonCount++;
                            if (EditButton(EditButtonPosition.Before, FontAwesome.Edit, i, true))
                            {
                                ContentDescriptor.Name = data.Key;
                                ContentDescriptor.Category = data.Key;
                                var path = _jsonObject["content"][data.Key]["path"];
                                if (path != null) ContentDescriptor.Path = path.ToString();

                                ModalDescriptor.Set(MessageType.EditContent, "Set the name and the path for this content.");
                            }

                            var importerName = data.Value["importer"]?.ToString();
                            var processorName = data.Value["processor"]?.ToString();
                            var processorParam = data.Value["processorParam"]?.ToString();
                            if (importerName == null || processorName == null)
                            {
                                PipelineTypes.GetTypeDescriptions(Path.GetExtension(data.Value["path"].ToString()),
                                    out var outImporter, out var outProcessor);

                                importerName = outImporter.TypeName;
                                processorName = outProcessor.TypeName;

                                data.Value["importer"] = importerName;
                                data.Value["processor"] = processorName;

                                if (importerName != null && processorName != null)
                                {
                                    ModifyDataDescriptor.ForceWrite = true;
                                }
                            }
                            if (processorParam == null)
                            {
                                if (WriteJsonProcessorParameters(processorName, data))
                                {
                                    ModifyDataDescriptor.ForceWrite = true;
                                }
                            }
                            var hasWatcher = _jsonObject["content"][data.Key]["watch"] != null;
                            if (!hasWatcher)
                            {
                                _jsonObject["content"][data.Key].AsObject().Add("watch", new JsonArray());
                            }

                            var nplItem = new ContentItem(data.Key, importerName, processorName); //e.g. data.Key = contentList
                            if (!ContentList.ContainsKey(data.Key)) ContentList.Add(data.Key, nplItem);

                            var categoryObject = _jsonObject["content"][data.Key];

                            ImGui.SetCursorPosX(ImGui.GetStyle().IndentSpacing * editButtonCount + ImGui.GetStyle().ItemSpacing.X);
                            ImGui.PushStyleColor(ImGuiCol.Text, ImGui.GetStyle().Colors[(int)ImGuiCol.NavHighlight]);
                            ImGui.PushItemWidth(ImGui.GetContentRegionAvail().X - 150 + ImGui.GetStyle().IndentSpacing * editButtonCount);
                            if (ImGui.TreeNodeEx(data.Key, ImGuiTreeNodeFlags.DefaultOpen | ImGuiTreeNodeFlags.SpanAllColumns))
                            {
                                ImGui.PopStyleColor();

                                foreach (var categoryItem in categoryObject.AsObject())
                                {
                                    var itemKey = categoryItem.Key; //e.g. path
                                    var itemValue = categoryItem.Value; //e.g. "C:\\"

                                    nplItem.SetParameter(itemKey, itemValue);
                                                                        
                                    if (itemKey == "watch")
                                    {
                                        ArrayEditor("Watcher", categoryItem.Value.AsArray(), out _, out _, out _);
                                    }
                                    else if (itemKey == "processorParam")
                                    {
                                        ImGui.PushItemWidth(ImGui.CalcItemWidth() - ImGui.GetStyle().IndentSpacing);
                                        if (ImGui.TreeNodeEx(itemKey, ImGuiTreeNodeFlags.DefaultOpen | ImGuiTreeNodeFlags.Leaf | ImGuiTreeNodeFlags.SpanAllColumns))
                                        {
                                            if (ImGui.BeginTable("Parameter", 2, ImGuiTableFlags.NoClip))
                                            {
                                                var itemCount = 0;
                                                foreach (var parameter in itemValue.AsObject())
                                                {
                                                    var parameterKey = parameter.Key; //e.g. ColorKeyColor
                                                    var parameterValue = parameter.Value; //e.g. 255,0,255,255

                                                    itemCount++;
                                                    var columnPos = itemCount % 2;

                                                    if (columnPos == 1)
                                                    {
                                                        ImGui.TableNextRow();
                                                        ImGui.TableSetupColumn("Column 1");
                                                        ImGui.TableSetupColumn("Column 2");
                                                        ImGui.TableSetColumnIndex(0);
                                                    }
                                                    else if (columnPos == 0) ImGui.TableSetColumnIndex(1);

                                                    if (nplItem.Property(parameterKey).Type == typeof(bool))
                                                    {
                                                        Checkbox(nplItem, data.Key, itemKey, parameterKey);
                                                    }
                                                    else if (nplItem.Property(parameterKey).Type == typeof(int))
                                                    {
                                                        InputInt(nplItem, data.Key, itemKey, parameterKey);
                                                    }
                                                    else if (nplItem.Property(parameterKey).Type == typeof(double))
                                                    {
                                                        InputDouble(nplItem, data.Key, itemKey, parameterKey);
                                                    }
                                                    else if (nplItem.Property(parameterKey).Type == typeof(float))
                                                    {
                                                        InputFloat(nplItem, data.Key, itemKey, parameterKey);
                                                    }
                                                    else if (nplItem.Property(parameterKey).Type == typeof(Color))
                                                    {
                                                        ColorEdit(nplItem, data.Key, itemKey, parameterKey);
                                                    }
                                                    else if (nplItem.Property(parameterKey).Type.IsEnum)
                                                    {
                                                        ComboContentItem(nplItem, data.Key, itemKey, parameterKey);
                                                    }
                                                    else TextInput(nplItem, data.Key, itemKey, parameterKey);
                                                }
                                            }
                                            ImGui.EndTable();
                                            ImGui.TreePop();
                                        }
                                        ImGui.PopItemWidth();
                                    }
                                    else if (itemKey == "path")
                                    {
                                        var path = nplItem.Path;
                                        if (ImGui.InputText(" ", ref path, 9999, ImGuiInputTextFlags.EnterReturnsTrue))
                                        {
                                            nplItem.Path = path;
                                            itemValue = path;
                                            ModifyDataDescriptor.Set(data.Key, itemKey, itemValue);
                                        }
                                    }
                                    else if (itemKey == "action")
                                    {
                                        var actionIndex = nplItem.GetActionIndex();
                                        var actionNames = Enum.GetNames(typeof(BuildAction));
                                        if (ImGui.Combo(itemKey, ref actionIndex, actionNames, actionNames.Length))
                                        {
                                            itemValue = actionNames[actionIndex].ToLowerInvariant();
                                            nplItem.Action = (BuildAction)Enum.Parse(typeof(BuildAction), itemValue.ToString(), true);
                                            ModifyDataDescriptor.Set(data.Key, itemKey, itemValue);
                                        }
                                    }
                                    else if (itemKey == "recursive")
                                    {
                                        ImGui.SameLine();
                                        if (ImGui.Checkbox(itemKey, ref nplItem.Recursive))
                                        {
                                            itemValue = nplItem.Recursive;
                                            ModifyDataDescriptor.Set(data.Key, itemKey, itemValue);
                                        }
                                    }
                                    else if (itemKey == "importer" || itemKey == "processor")
                                    {
                                        if (ComboTypeDesciptors(nplItem, itemKey, out string value))
                                        {
                                            if (itemKey == "processor")
                                            {
                                                GetJsonProcessorParameters(value, out modifiedProcessorParam);
                                            }
                                            ModifyDataDescriptor.Set(data.Key, itemKey, value);
                                        }
                                    }
                                }
                                ImGui.TreePop();
                            }
                            ImGui.PopStyleColor();
                            ImGui.PopItemWidth();
                        }
                        ModifyData(modifiedProcessorParam);
                        ImGui.End();
                    }
                    else if (_logOpen)
                    {
                        ImGui.Spacing(); ImGui.Spacing(); ImGui.Spacing(); ImGui.Spacing();

                        ImGui.PushStyleColor(ImGuiCol.FrameBg, ImGui.GetStyle().Colors[(int)ImGuiCol.WindowBg]);
                        ImGui.TextUnformatted(NPLEditorSink.Output.ToString());
                        ImGui.PopStyleColor();

                        ImGui.Spacing(); ImGui.Spacing(); ImGui.Spacing(); ImGui.Spacing();

                        if (!_buildContentRunning)
                        {
                            if (ImGui.Button($"{FontAwesome.StepBackward} Back", new Num.Vector2(ImGui.GetContentRegionAvail().X, 0)))
                            {
                                _logOpen = false;
                            }
                        }
                        else
                        {
                            if (ImGui.Button($"{FontAwesome.Stop} CANCEL", new Num.Vector2(ImGui.GetContentRegionAvail().X, 0)))
                            {
                                //ToDo: Implement already running build content cancelation.
                                _cancelBuildContent = true;
                            }
                        }

                        //BUG: SetScrollHere currently doesn't work on InputTextMultiline.
                        //ImGui.InputTextMultiline("##LogText", ref logText, 999999, ImGui.GetContentRegionAvail(), ImGuiInputTextFlags.ReadOnly);

                        if (ScrollLogToBottom && ImGui.GetScrollMaxY() > ImGui.GetScrollY())
                        {
                            ScrollLogToBottom = false;
                            ImGui.SetScrollHereY(1.0f);
                        }
                    }
                    ImGui.End();
                }
            }
        }

        private void ModifyData(JsonObject modifiedProcessorParam)
        {
            if (ModifyDataDescriptor.HasData)
            {
                if (ModifyDataDescriptor.ParamModify)
                {
                    ModifyParameter();
                    WriteContentNPL();
                    ModifyDataDescriptor.Reset();
                }
                else
                {
                    if (ModifyDataDescriptor.ItemKey == "processor")
                    {
                        if (modifiedProcessorParam == null)
                        {
                            _jsonObject["content"][ModifyDataDescriptor.DataKey].AsObject().Remove("processorParam");
                        }
                        else _jsonObject["content"][ModifyDataDescriptor.DataKey]["processorParam"] = modifiedProcessorParam;
                    }
                    else if (ModifyDataDescriptor.ItemKey == "path" && string.IsNullOrEmpty(ModifyDataDescriptor.Value.ToString()))
                    {
                        _jsonObject["content"][ModifyDataDescriptor.DataKey].AsObject().Remove("importer");
                        _jsonObject["content"][ModifyDataDescriptor.DataKey].AsObject().Remove("processor");
                        _jsonObject["content"][ModifyDataDescriptor.DataKey].AsObject().Remove("processorParam");
                    }

                    Modify();
                    WriteContentNPL();
                    ModifyDataDescriptor.Reset();
                }
            }
            else if (ModifyDataDescriptor.ForceWrite)
            {
                WriteContentNPL();
                ModifyDataDescriptor.ForceWrite = false;
            }
        }
        private void Modify()
        {
            _jsonObject["content"][ModifyDataDescriptor.DataKey][ModifyDataDescriptor.ItemKey] = ModifyDataDescriptor.Value;
        }
        private void ModifyParameter()
        {
            _jsonObject["content"][ModifyDataDescriptor.DataKey][ModifyDataDescriptor.ItemKey][ModifyDataDescriptor.ParamKey] = ModifyDataDescriptor.Value;
        }

        private bool GetJsonProcessorParameters(string processor, out JsonObject props)
        {
            var properties = PipelineTypes.Processors?.ToList().Find(x => x.TypeName.Equals(processor))?.Properties;
            if (properties != null && properties.Any())
            {
                props = new JsonObject();
                foreach (var property in properties)
                {
                    props.Add(property.Name, property.DefaultValue?.ToString() ?? "");
                }
                return true;
            }
            props = null;
            return false;
        }

        private bool WriteJsonProcessorParameters(
            string processor, KeyValuePair<string, JsonNode> data)
        {
            if (GetJsonProcessorParameters(processor, out JsonObject props))
            {
                data.Value["processorParam"] = props;
                return true;
            }
            return false;
        }

        private void WriteContentNPL()
        {
            try
            {
                string jsonString = JsonSerializer.Serialize(_jsonObject, new JsonSerializerOptions()
                {
                    WriteIndented = true
                });
                File.WriteAllText(_nplJsonFilePath, jsonString);

                Log.Debug("Content file successfully saved.");
            }
            catch (Exception e) { NPLLog.LogException(e, "SAVE ERROR"); }
        }

        protected override void OnExiting(object sender, ExitingEventArgs args)
        {
            WriteContentNPL();
            base.OnExiting(sender, args);
        }

        private void MoveTreeItem(int i, bool down)
        {
            var content = _jsonObject["content"].AsObject().ToList();

            foreach (var item in content)
            {
                _jsonObject["content"].AsObject().Remove(item.Key);
            }
                        
            for (int x = 0; x < content.Count; x++)
            {
                if (down)
                {
                    if (x == i + 1) _jsonObject["content"].AsObject().Add(content[i]);
                    else if (x == i) _jsonObject["content"].AsObject().Add(content[i + 1]);
                    else _jsonObject["content"].AsObject().Add(content[x]);
                }
                else
                {
                    if (x == i - 1) _jsonObject["content"].AsObject().Add(content[i]);
                    else if (x == i) _jsonObject["content"].AsObject().Add(content[i - 1]);
                    else _jsonObject["content"].AsObject().Add(content[x]);
                }
            }

            WriteContentNPL();
        }

        #region ImGui Widgets

        private bool ColorEdit(ContentItem nplItem, string dataKey, string itemKey, string parameterKey)
        {
            var value = nplItem.Vector4Property(parameterKey);
            if (ImGui.ColorEdit4(parameterKey, ref value,
                ImGuiColorEditFlags.NoOptions | ImGuiColorEditFlags.NoPicker | ImGuiColorEditFlags.NoTooltip))
            {
                var xColor = value.ToXNA();
                var sColor = $"{xColor.R},{xColor.G},{xColor.B},{xColor.A}";

                nplItem.Property(parameterKey).Value = sColor;
                ModifyDataDescriptor.Set(dataKey, itemKey, nplItem.Property(parameterKey).Value, parameterKey);
                return true;
            }
            return false;
        }

        private bool Checkbox(ContentItem nplItem, string dataKey, string itemKey, string parameterKey)
        {
            var value = nplItem.BoolProperty(parameterKey);
            if (ImGui.Checkbox(parameterKey, ref value))
            {
                nplItem.Property(parameterKey).Value = value.ToString();
                ModifyDataDescriptor.Set(dataKey, itemKey, nplItem.Property(parameterKey).Value, parameterKey);
                return true;
            }
            return false;
        }

        private bool InputInt(ContentItem nplItem, string dataKey, string itemKey, string parameterKey)
        {
            var value = nplItem.IntProperty(parameterKey);
            if (ImGui.InputInt(parameterKey, ref value))
            {
                nplItem.Property(parameterKey).Value = value;
                ModifyDataDescriptor.Set(dataKey, itemKey, nplItem.Property(parameterKey).Value, parameterKey);
                return true;
            }
            return false;
        }

        private bool InputDouble(ContentItem nplItem, string dataKey, string itemKey, string parameterKey)
        {
            var value = nplItem.DoubleProperty(parameterKey);
            if (ImGui.InputDouble(parameterKey, ref value))
            {
                nplItem.Property(parameterKey).Value = value;
                ModifyDataDescriptor.Set(dataKey, itemKey, nplItem.Property(parameterKey).Value, parameterKey);
                return true;
            }
            return false;
        }

        private bool InputFloat(ContentItem nplItem, string dataKey, string itemKey, string parameterKey)
        {
            var value = nplItem.FloatProperty(parameterKey);
            if (ImGui.InputFloat(parameterKey, ref value))
            {
                nplItem.Property(parameterKey).Value = value;
                ModifyDataDescriptor.Set(dataKey, itemKey, nplItem.Property(parameterKey).Value, parameterKey);
                return true;
            }
            return false;
        }

        private bool ComboEnum(ref string property, string parameterKey, string[] names)
        {
            var selectedIndex = names.ToList().IndexOf(property.ToString());
            if (ImGui.Combo(parameterKey, ref selectedIndex, names, names.Length))
            {
                property = names[selectedIndex].ToString();
                return true;
            }
            return false;
        }

        private bool Combo(ContentItem nplItem, string parameterKey, string[] names)
        {
            var property = nplItem.Property(parameterKey);
            var selectedIndex = names.ToList().IndexOf(property.Value.ToString());
            if (ImGui.Combo(parameterKey, ref selectedIndex, names, names.Length))
            {
                nplItem.Property(parameterKey).Value = names[selectedIndex].ToString();
                return true;
            }
            return false;
        }

        private bool ComboContentItem(ContentItem nplItem, string dataKey, string itemKey, string parameterKey)
        {
            var names = Enum.GetNames(nplItem.Property(parameterKey).Type);
            if (Combo(nplItem, parameterKey, names))
            {
                ModifyDataDescriptor.Set(dataKey, itemKey, nplItem.Property(parameterKey).Value, parameterKey);
                return true;
            }
            return false;
        }

        private bool ComboTypeDesciptors(ContentItem nplItem, string itemKey, out string value)
        {
            if (itemKey == "importer")
            {
                var selectedIndex = nplItem.GetImporterIndex();
                var names = PipelineTypes.Importers.Select(x => x.DisplayName).ToArray();

                if (ImGui.Combo(itemKey, ref selectedIndex, names, names.Length))
                {
                    nplItem.SelectedImporterIndex = selectedIndex;
                    value = PipelineTypes.Importers[nplItem.SelectedImporterIndex].TypeName;
                    return true;
                }
            }
            else if (itemKey == "processor")
            {
                var selectedIndex = nplItem.GetProcessorIndex();
                var names = PipelineTypes.Processors.Select(x => x.DisplayName).ToArray();

                if (ImGui.Combo(itemKey, ref selectedIndex, names, names.Length))
                {
                    nplItem.SelectedProcessorIndex = selectedIndex;
                    value = PipelineTypes.Processors[nplItem.SelectedProcessorIndex].TypeName;
                    return true;
                }
            }
            value = string.Empty;
            return false;
        }

        private bool TextInput(ContentItem nplItem, string dataKey, string itemKey, string parameterKey)
        {
            var paramValue = nplItem.Property(parameterKey).Value.ToString();
            if (ImGui.InputText(parameterKey, ref paramValue, 9999))
            {
                nplItem.Property(parameterKey).Value = paramValue;
                ModifyDataDescriptor.Set(dataKey, itemKey, nplItem.Property(parameterKey).Value, parameterKey);
                return true;
            }
            return false;
        }

        private bool EditButton(EditButtonPosition position, string icon, int id, bool small, bool disabled = false)
        {
            if (disabled) ImGui.BeginDisabled();
            if (position == EditButtonPosition.After) ImGui.SameLine();
            ImGui.PushID($"##{id}");
            if (small)
            {
                ImGui.PushStyleColor(ImGuiCol.Button, ImGui.GetStyle().Colors[(int)ImGuiCol.WindowBg]);
                ImGui.PushStyleColor(ImGuiCol.ButtonHovered, ImGui.GetStyle().Colors[(int)ImGuiCol.FrameBgHovered]);
                ImGui.PushStyleColor(ImGuiCol.ButtonActive, ImGui.GetStyle().Colors[(int)ImGuiCol.FrameBgActive]);
                if (ImGui.SmallButton(icon))
                {
                    if (position == EditButtonPosition.Before) ImGui.SameLine();
                    ImGui.PopStyleColor(); ImGui.PopStyleColor(); ImGui.PopStyleColor();
                    return true;
                }
                ImGui.PopStyleColor(); ImGui.PopStyleColor(); ImGui.PopStyleColor();
            }
            else
            {
                ImGui.PushStyleColor(ImGuiCol.Button, ImGui.GetStyle().Colors[(int)ImGuiCol.TabActive]);
                ImGui.PushStyleColor(ImGuiCol.ButtonHovered, ImGui.GetStyle().Colors[(int)ImGuiCol.TabHovered]);
                ImGui.PushStyleColor(ImGuiCol.ButtonActive, ImGui.GetStyle().Colors[(int)ImGuiCol.Tab]);
                if (ImGui.Button(icon))
                {
                    if (position == EditButtonPosition.Before) ImGui.SameLine();
                    ImGui.PopStyleColor(); ImGui.PopStyleColor(); ImGui.PopStyleColor();
                    return true;
                }
                ImGui.PopStyleColor(); ImGui.PopStyleColor(); ImGui.PopStyleColor();
            }
            ImGui.PopID();
            if (position == EditButtonPosition.Before) ImGui.SameLine();
            if (disabled) ImGui.EndDisabled();
            return false;
        }

        private void ArrayEditor(
            string name,
            JsonArray jsonArray,
            out bool itemAdded,
            out bool itemRemoved,
            out bool itemChanged)
        {
            itemAdded = false;
            itemRemoved = false;
            itemChanged = false;

            ImGui.PushItemWidth(ImGui.CalcItemWidth() - ImGui.GetStyle().IndentSpacing);
            if (ImGui.TreeNodeEx($"{FontAwesome.Plus} Add {name}", ImGuiTreeNodeFlags.DefaultOpen | ImGuiTreeNodeFlags.Leaf | ImGuiTreeNodeFlags.SpanAllColumns))
            {
                if (ImGui.IsItemClicked())
                {
                    itemAdded = true;
                    jsonArray.Add("");
                    WriteContentNPL();
                }

                for (int i = 0; i < jsonArray.Count; i++)
                {
                    var data = jsonArray[i].ToString();
                    ImGui.PushItemWidth(ImGui.CalcItemWidth() - ImGui.GetStyle().IndentSpacing * 2f);
                    if (ImGui.InputText($"##{i}", ref data, 9999, ImGuiInputTextFlags.EnterReturnsTrue))
                    {
                        itemChanged = true;
                        jsonArray[i] = data;
                        WriteContentNPL();
                    }
                    ImGui.PopItemWidth();

                    if (EditButton(EditButtonPosition.After, FontAwesome.TrashAlt, i, false))
                    {
                        itemRemoved = true;
                        jsonArray.RemoveAt(i);
                        WriteContentNPL();
                    }
                }
                ImGui.TreePop();
            }
        }

        private void MenuBar()
        {
            bool changeTreeVisibility = false;
            if (ImGui.BeginMenuBar())
            {
                if (ImGui.BeginMenu("File"))
                {
                    ImGui.SeparatorText("New");
                    if (ImGui.MenuItem($"{FontAwesome.Plus} Add Content"))
                    {
                        ModalDescriptor.Set(MessageType.AddContent, "Set the name and the path of your new content.");
                    }
                    ImGui.SeparatorText("App");
                    if (ImGui.MenuItem($"{FontAwesome.Save} Save"))
                    {
                        WriteContentNPL();
                    }
                    if (ImGui.MenuItem($"{FontAwesome.WindowClose} Exit"))
                    {
                        WriteContentNPL();
                        Exit();
                    }
                    ImGui.EndMenu();
                }
                if (ImGui.BeginMenu("Options"))
                {
                    ImGui.SeparatorText("View");
                    if (ImGui.MenuItem($"{(_treeNodesOpen ? $"{FontAwesome.Eye} Trees Open" : $"{FontAwesome.EyeSlash} Trees Closed")}"))
                    {
                        _treeNodesOpen = !_treeNodesOpen;
                        changeTreeVisibility = true;
                    }
                    if (ImGui.MenuItem($"{(_settingsVisible ? $"{FontAwesome.Eye} Settings Visible" : $"{FontAwesome.EyeSlash} Settings Hidden")}"))
                    {
                        _settingsVisible = !_settingsVisible;
                    }
                    if (ImGui.MenuItem($"{(_logOpen ? $"{FontAwesome.EyeSlash} Close Log" : $"{FontAwesome.Eye} Show Log")}"))
                    {
                        ScrollLogToBottom = true;
                        _logOpen = !_logOpen;
                    }
                    if (ImGui.MenuItem($"{(_orderingOptionsVisible ? $"{FontAwesome.Eye} Hide Order Arrows" : $"{FontAwesome.EyeSlash} Show Order Arrows")}"))
                    {
                        _orderingOptionsVisible = !_orderingOptionsVisible!;
                    }
                    ImGui.EndMenu();
                }

                if (ImGui.BeginMenu("Content"))
                {
                    ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 10);
                    if (ImGui.MenuItem($"{FontAwesome.Igloo} Build Now", !_buildContentRunning))
                    {
                        _buildContentRunning = true;
                        _logOpen = true;

                        if (_clearLogViewOnBuild) NPLEditorSink.Output.Clear();

                        Task.Factory.StartNew(() => BuildContent());
                    }
                    ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 10);
                    if (ImGui.MenuItem($"{FontAwesome.Igloo} Rebuild Now", !_buildContentRunning))
                    {
                        _buildContentRunning = true;
                        _logOpen = true;

                        if (_clearLogViewOnBuild) NPLEditorSink.Output.Clear();

                        Task.Factory.StartNew(() => BuildContent(rebuildNow: true));
                    }
                    ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 10);
                    if (ImGui.MenuItem($"{FontAwesome.Broom} Clean Now"))
                    {
                        _logOpen = true;

                        if (_clearLogViewOnBuild) NPLEditorSink.Output.Clear();

                        _runtimeBuilder.CleanContent();
                        NPLLog.LogInfoHeadline(FontAwesome.Broom, "CONTENT CLEANED");
                    }
                    ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 5);
                    ImGui.SeparatorText("Options");
                    if (ImGui.MenuItem("Incremental", "", _incrementalContent))
                    {
                        _incrementalContent = !_incrementalContent;
                        _runtimeBuilder.Incremental = _incrementalContent;
                    }
                    if (ImGui.MenuItem("Launch Debugger", "", _launchDebuggerContent))
                    {
                        _launchDebuggerContent = !_launchDebuggerContent;
                        _runtimeBuilder.LaunchDebugger = _launchDebuggerContent;
                    }
                    if (ImGui.MenuItem("Clear Log View on Build", "", _clearLogViewOnBuild))
                    {
                        _clearLogViewOnBuild = !_clearLogViewOnBuild;
                    }
                    ImGui.EndMenu();
                }

                if (ImGui.BeginMenu("Help"))
                {
                    if (ImGui.BeginMenu($"{FontAwesome.FileArchive} Logs"))
                    {
                        if (ImGui.MenuItem("All"))
                        {
                            if (File.Exists(AppSettings.AllLogPath))
                            {
                                ProcessStartInfo process = new(AppSettings.AllLogPath)
                                {
                                    UseShellExecute = true
                                };
                                Process.Start(process);
                            }
                            else ModalDescriptor.SetFileNotFound(AppSettings.AllLogPath, "Note: this log file will only be created on certain events.");
                        }
                        if (ImGui.MenuItem("Important"))
                        {
                            if (File.Exists(AppSettings.ImportantLogPath))
                            {
                                ProcessStartInfo process = new(AppSettings.ImportantLogPath)
                                {
                                    UseShellExecute = true
                                };
                                Process.Start(process);
                            }
                            else ModalDescriptor.SetFileNotFound(AppSettings.ImportantLogPath, "Note: this log file gets created on errors or important events.");
                        }
                        ImGui.EndMenu();
                    }
                    if (ImGui.MenuItem($"{FontAwesome.AddressBook} About"))
                    {
                        ModalDescriptor.SetAbout();
                    }
                    ImGui.EndMenu();
                }
                ImGui.EndMenuBar();
            }

            if (ModalDescriptor.IsOpen)
            {
                ImGui.OpenPopup(ModalDescriptor.Title);
                PopupModal(ModalDescriptor.Title, ModalDescriptor.Message);
            }

            if (changeTreeVisibility)
            {
                var ids = new List<string>
                {
                    "settings"
                };
                ids.AddRange(_jsonObject["content"].AsObject().Select(x => x.Key).ToArray());

                foreach (var stringID in ids)
                {
                    var id = ImGui.GetID(stringID);
                    ImGui.GetStateStorage().SetInt(id, _treeNodesOpen ? 1 : 0);
                }
            }
        }

        private bool PopupModal(string title, string message)
        {
            ImGuiWindowFlags modalWindowFlags = ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoDecoration
            | ImGuiWindowFlags.Modal | ImGuiWindowFlags.Popup | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.AlwaysAutoResize;

            ImGuiViewportPtr viewport = ImGui.GetMainViewport();

            ImGui.SetNextWindowPos(viewport.GetCenter(), ImGuiCond.Always, new Num.Vector2(0.5f));
            if (ImGui.BeginPopupModal(ModalDescriptor.Title, ref _dummyBoolIsOpen, modalWindowFlags))
            {
                var buttonCountRightAligned = 1;
                var buttonWidth = 60f;

                ImGui.SeparatorText(title);

                ImGui.SetCursorPos(new Num.Vector2(ImGui.GetStyle().ItemSpacing.X / 2f, ImGui.GetFrameHeight()));
                ImGui.PushTextWrapPos(viewport.Size.X / 2f);
                ImGui.TextWrapped(message);
                ImGui.PopTextWrapPos();

                ImGui.Spacing(); ImGui.Spacing(); ImGui.Spacing(); ImGui.Spacing();

                if (ModalDescriptor.MessageType == MessageType.AddContent || ModalDescriptor.MessageType == MessageType.EditContent)
                {
                    if (ImGui.InputTextWithHint("name", "textures / sound / music / etc.", ref ContentDescriptor.Name, 9999))
                    {
                        ContentDescriptor.Error("");

                        Extensions.NumberlessRef(ref ContentDescriptor.Name);

                        var existingItem = _jsonObject["content"].AsObject().Select(x => x.Key).ToList().Find(x => x.Equals(ContentDescriptor.Name));
                        if (existingItem != null) ContentDescriptor.Error("Already exists!");
                    }
                    if (string.IsNullOrEmpty(ContentDescriptor.Name)) ContentDescriptor.Error("Name must be set!");

                    if (ContentDescriptor.HasError)
                    {
                        ImGui.TextColored(ImGui.GetStyle().Colors[(int)ImGuiCol.TabActive], ContentDescriptor.ErrorMessage);
                    }

                    ImGui.InputTextWithHint("path", "Graphics/*.png / Music/*.ogg etc.", ref ContentDescriptor.Path, 9999);
                }
                else if (ModalDescriptor.MessageType == MessageType.About)
                {
                    ImGui.Spacing(); ImGui.Spacing(); ImGui.Spacing(); ImGui.Spacing();

                    ImGui.PushStyleColor(ImGuiCol.Button, ImGui.GetStyle().Colors[(int)ImGuiCol.PlotHistogram]);
                    ImGui.PushStyleColor(ImGuiCol.ButtonHovered, ImGui.GetStyle().Colors[(int)ImGuiCol.PlotHistogramHovered]);
                    ImGui.PushStyleColor(ImGuiCol.ButtonActive, ImGui.GetStyle().Colors[(int)ImGuiCol.PlotHistogram]);
                    if (ImGui.Button($"{FontAwesomeBrands.Github} NPL Editor", new Num.Vector2(ImGui.GetContentRegionAvail().X, 0)))
                    {
                        ProcessStartInfo process = new(AppSettings.GitHubRepoURL)
                        {
                            UseShellExecute = true
                        };
                        Process.Start(process);
                    }
                    ImGui.PopStyleColor(); ImGui.PopStyleColor(); ImGui.PopStyleColor();
                }

                ImGui.Spacing(); ImGui.Spacing(); ImGui.Spacing(); ImGui.Spacing();

                bool hasDelete = (ModalDescriptor.MessageType & MessageType.Delete) != 0;
                bool hasCancel = (ModalDescriptor.MessageType & MessageType.Cancel) != 0;
                if (hasCancel) buttonCountRightAligned++;

                if (hasDelete)
                {
                    ImGui.PushStyleColor(ImGuiCol.Button, ImGui.GetStyle().Colors[(int)ImGuiCol.TabActive]);
                    ImGui.PushStyleColor(ImGuiCol.ButtonHovered, ImGui.GetStyle().Colors[(int)ImGuiCol.TabHovered]);
                    ImGui.PushStyleColor(ImGuiCol.ButtonActive, ImGui.GetStyle().Colors[(int)ImGuiCol.Tab]);
                    ImGui.SetCursorPosX(ImGui.GetStyle().ItemSpacing.X);
                    if (ImGui.Button($"{FontAwesome.TrashAlt}", new Num.Vector2(buttonWidth, 0)))
                    {
                        _jsonObject["content"].AsObject().Remove(ContentDescriptor.Category);
                        ContentList.Remove(ContentDescriptor.Category);
                        WriteContentNPL();
                        ClosePopupModal();
                        return true;
                    }
                    ImGui.PopStyleColor(); ImGui.PopStyleColor(); ImGui.PopStyleColor();
                }

                ImGui.SameLine(ImGui.GetContentRegionMax().X - (buttonWidth * buttonCountRightAligned) - ImGui.GetStyle().ItemSpacing.X * buttonCountRightAligned);

                if (ContentDescriptor.HasError) ImGui.BeginDisabled();
                if (ImGui.Button("OK", new Num.Vector2(buttonWidth, 0)) || (ImGui.IsKeyPressed(ImGuiKey.Enter) && !ContentDescriptor.HasError))
                {
                    if (ModalDescriptor.MessageType == MessageType.AddContent)
                    {
                        JsonObject content = new()
                        {
                            ["path"] = ContentDescriptor.Path,
                            ["recursive"] = "false",
                            ["action"] = "build"
                        };

                        _jsonObject["content"].AsObject().Add(ContentDescriptor.Name, content);
                        WriteContentNPL();
                    }
                    else if (ModalDescriptor.MessageType == MessageType.EditContent)
                    {
                        var node = _jsonObject["content"][ContentDescriptor.Category];
                        var nodeIndex = _jsonObject["content"].AsObject().Select(x => x.Key).ToList().IndexOf(ContentDescriptor.Category);
                        var content = _jsonObject["content"].AsObject().ToList();

                        foreach (var item in content)
                        {
                            _jsonObject["content"].AsObject().Remove(item.Key);
                        }

                        for (int i = 0; i < content.Count; i++)
                        {
                            if (i != nodeIndex) _jsonObject["content"].AsObject().Add(content[i]);
                            else _jsonObject["content"].AsObject().Add(ContentDescriptor.Name, node);
                        }

                        WriteContentNPL();
                    }
                    ClosePopupModal();
                    return true;
                }
                if (ContentDescriptor.HasError) ImGui.EndDisabled();

                if (hasCancel)
                {
                    ImGui.SameLine();
                    if (ImGui.Button("Cancel", new Num.Vector2(buttonWidth, 0)) || ImGui.IsKeyPressed(ImGuiKey.Escape))
                    {
                        ClosePopupModal();
                        return false;
                    }
                }
                ImGui.EndPopup();
            }
            return false;
        }

        private void ClosePopupModal()
        {
            ContentDescriptor.Reset();
            ModalDescriptor.Reset();
            ImGui.CloseCurrentPopup();
        }

        #endregion ImGui Widgets

        public async Task BuildContent(bool rebuildNow = false)
        {
            NPLLog.LogInfoHeadline(FontAwesome.Igloo, "BUILD CONTENT");
            
            _runtimeBuilder.Rebuild = rebuildNow;
            try
            {
                if (_runtimeBuilder.LaunchDebugger && !Debugger.IsAttached)
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
                            if (_cancelBuildContent)
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
                if (!_cancelBuildContent)
                {
                    ContentReader.GetAllContentFiles(out var filesToCopy, out var filesToBuild);

                    _runtimeBuilder.RegisterCopyContent(filesToCopy.ToArray());
                    _runtimeBuilder.RegisterBuildContent(filesToBuild.ToArray());
                    
                    await _runtimeBuilder.BuildContent();

                    NPLLog.LogInfoHeadline(FontAwesome.Igloo, "BUILD FINISHED");
                }
                else NPLLog.LogInfoHeadline(FontAwesome.Igloo, "BUILD CANCELD");
            }
            catch (Exception e) { NPLLog.LogException(e, "BUILD FAILED"); }

            _cancelBuildContent = false;
            _buildContentRunning = false;

            if (rebuildNow) _runtimeBuilder.Rebuild = false;
        }
    }
}
