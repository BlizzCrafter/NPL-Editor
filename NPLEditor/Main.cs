using System;
using System.IO;
using System.Data;
using System.Linq;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NPLEditor.Common;
using NPLEditor.Data;
using NPLEditor.Enums;
using NPLEditor.GUI;
using ImGuiNET;
using Serilog;
using Color = Microsoft.Xna.Framework.Color;
using Num = System.Numerics;

namespace NPLEditor
{
    public class Main : Game
    {
        public enum EditButtonPosition
        {
            Before,
            After
        }

        private GraphicsDeviceManager _graphics;
        private ImGuiRenderer _imGuiRenderer;
        private Num.Vector3 _clearColor = new(114f / 255f, 144f / 255f, 154f / 255f);

        private JsonNode _jsonObject;
        private string _nplJsonFilePath;
        private bool _treeNodesOpen = true;
        private bool _settingsVisible = true;
        private bool _dummyBoolIsOpen = true;
        private bool _buildContentEnabled = true;
        private LoggerVerbosity _buildContentVerbosity = LoggerVerbosity.Minimal;

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
            Window.Title = "NPL Editor";

            _graphics.GraphicsDevice.PresentationParameters.BackBufferWidth = _graphics.PreferredBackBufferWidth;
            _graphics.GraphicsDevice.PresentationParameters.BackBufferHeight = _graphics.PreferredBackBufferHeight;

            _imGuiRenderer = new ImGuiRenderer(this);

            base.Initialize();

            Log.Information("App Initialized.");
        }

        protected override void LoadContent()
        {
            Directory.SetCurrentDirectory(Path.Combine(Directory.GetCurrentDirectory(), "Content"));
            Log.Debug($"WorkingDir: {Directory.GetCurrentDirectory()}");
            Log.Debug($"LocalContentDir: {AppSettings.LocalContentPath}");
#if DEBUG
            string workingDir = Directory.GetCurrentDirectory();
            string projDir = Directory.GetParent(workingDir).Parent.Parent.FullName;
            _nplJsonFilePath = Path.Combine(projDir, "Content", "Content.npl");
#else
            string[] args = Environment.GetCommandLineArgs();
            _nplJsonFilePath = args[1];
#endif

            var jsonString = File.ReadAllText(_nplJsonFilePath);
            _jsonObject = JsonNode.Parse(jsonString);

            base.LoadContent();
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(new Color(_clearColor.X, _clearColor.Y, _clearColor.Z));

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
                | ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.AlwaysVerticalScrollbar | ImGuiWindowFlags.MenuBar;

            ImGuiViewportPtr viewport = ImGui.GetMainViewport();

            ImGui.SetNextWindowPos(viewport.Pos);
            ImGui.SetNextWindowSize(viewport.Size);
            ImGui.SetNextWindowViewport(viewport.ID);
            if (ImGui.Begin("Main", ref _dummyBoolIsOpen, mainWindowFlags))
            {
                ImGui.SetNextWindowPos(viewport.Pos);
                ImGui.SetNextWindowSize(viewport.Size);
                ImGui.SetNextWindowViewport(viewport.ID);
                if (ImGui.Begin("JsonTree", ref _dummyBoolIsOpen, windowFlags))
                {
                    MenuBar();

                    if (_settingsVisible)
                    {
                        EditButton(EditButtonPosition.Before, FontAwesome.Edit, -1, true, true);
                        
                        ImGui.SetCursorPosX(ImGui.GetStyle().IndentSpacing + ImGui.GetStyle().ItemSpacing.X);
                        ImGui.PushStyleColor(ImGuiCol.Text, ImGui.GetStyle().Colors[(int)ImGuiCol.NavHighlight]);
                        ImGui.PushItemWidth(ImGui.GetContentRegionAvail().X - 150 - ImGui.GetStyle().IndentSpacing);
                        if (ImGui.TreeNodeEx("settings", ImGuiTreeNodeFlags.DefaultOpen | ImGuiTreeNodeFlags.SpanAllColumns))
                        {
                            ImGui.PopStyleColor();

                            var root = _jsonObject["root"];
                            var rootValue = root.ToString();
                            ImGui.InputText("root", ref rootValue, 9999, ImGuiInputTextFlags.ReadOnly);

                            var references = _jsonObject["references"].AsArray();
                            ArrayEditor("Reference", references, out _, out bool itemRemoved, out bool itemChanged);
                            {
                                if (itemChanged || itemRemoved)
                                {
                                    PipelineTypes.Reset();
                                    WriteContentNPL();
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
                                PipelineTypes.Load(combinedReferences.Distinct()
                                    .Where(x => !string.IsNullOrEmpty(x.ToString()))
                                    .ToArray());
                            }
                            ImGui.TreePop();
                        }
                        ImGui.PopStyleColor();
                        ImGui.PopItemWidth();
                    }

                    JsonObject modifiedProcessorParam = null;
                    var jsonContent = _jsonObject["content"].AsObject().AsEnumerable();
                    for (int i = 0; i < jsonContent.Count(); i++)
                    {
                        var data = jsonContent.ToArray()[i];

                        if (EditButton(EditButtonPosition.Before, FontAwesome.Edit, i, true))
                        {
                            ContentDescriptor.Name = data.Key;
                            ContentDescriptor.Category = data.Key;
                            ModalDescriptor.Set(MessageType.EditContent, "Set the name for this content.");
                        }

                        var importerName = data.Value["importer"]?.ToString();
                        var processorName = data.Value["processor"]?.ToString();
                        var processorParam = data.Value["processorParam"]?.ToString();
                        if (importerName == null || processorName == null)
                        {
                            PipelineTypes.GetTypeDescriptions(Path.GetExtension(data.Value["path"].ToString()),
                                out var outImporter, out var outProcessor);

                            importerName = outImporter?.TypeName;
                            processorName = outProcessor?.TypeName;

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

                        var nplItem = new ContentItem(data.Key, importerName, processorName); //e.g. data.Key = contentList

                        var categoryObject = _jsonObject["content"][data.Key];

                        ImGui.SetCursorPosX(ImGui.GetStyle().IndentSpacing + ImGui.GetStyle().ItemSpacing.X);
                        ImGui.PushStyleColor(ImGuiCol.Text, ImGui.GetStyle().Colors[(int)ImGuiCol.NavHighlight]);
                        ImGui.PushItemWidth(ImGui.GetContentRegionAvail().X - 150 - ImGui.GetStyle().IndentSpacing);
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
                                else
                                {
                                    if (itemKey == "processorParam")
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
                                                        ComboEnum(nplItem, data.Key, itemKey, parameterKey);
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
                                        if (ImGui.InputText(" ", ref path, 9999))
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
                                        ImGui.SameLine(); ImGui.Checkbox(itemKey, ref nplItem.Recursive);
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
                            }
                            ImGui.TreePop();
                        }
                        ImGui.PopStyleColor();
                        ImGui.PopItemWidth();
                    }
                    ModifyData(modifiedProcessorParam);
                    ImGui.End();
                }
                ImGui.End();
            }
        }

        private void ModifyData(JsonObject modifiedProcessorParam)
        {
            if (ModifyDataDescriptor.HasData)
            {
                if (ModifyDataDescriptor.ParamModify)
                {
                    _jsonObject["content"][ModifyDataDescriptor.DataKey][ModifyDataDescriptor.ItemKey][ModifyDataDescriptor.ParamKey] = ModifyDataDescriptor.Value;
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

                    _jsonObject["content"][ModifyDataDescriptor.DataKey][ModifyDataDescriptor.ItemKey] = ModifyDataDescriptor.Value;
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

                Log.Debug("--- Content file successfully saved ---");
            }
            catch { Log.Error("It was not possible to save the content file."); }
        }

        protected override void OnExiting(object sender, EventArgs args)
        {
            WriteContentNPL();
            base.OnExiting(sender, args);
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

        private bool ComboEnum(ContentItem nplItem, string dataKey, string itemKey, string parameterKey)
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

        private bool EditButton(EditButtonPosition position, string text, int id, bool small, bool disabled = false)
        {
            if (disabled) ImGui.BeginDisabled();
            if (position == EditButtonPosition.After) ImGui.SameLine();
            ImGui.PushID($"##{id}");
            if (small)
            {
                ImGui.PushStyleColor(ImGuiCol.Button, ImGui.GetStyle().Colors[(int)ImGuiCol.WindowBg]);
                ImGui.PushStyleColor(ImGuiCol.ButtonHovered, ImGui.GetStyle().Colors[(int)ImGuiCol.FrameBgHovered]);
                ImGui.PushStyleColor(ImGuiCol.ButtonActive, ImGui.GetStyle().Colors[(int)ImGuiCol.FrameBgActive]);
                if (ImGui.SmallButton(text))
                {
                    if (position == EditButtonPosition.Before) ImGui.SameLine();
                    return true;
                }
                ImGui.PopStyleColor(); ImGui.PopStyleColor(); ImGui.PopStyleColor();
            }
            else
            {
                if (ImGui.Button(text))
                {
                    if (position == EditButtonPosition.Before) ImGui.SameLine();
                    return true;
                }
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
                    if (ImGui.MenuItem($"{FontAwesome.Plus} Add Content"))
                    {
                        ModalDescriptor.Set(MessageType.AddContent, "Set the name and the extension of your new content.");
                    }
                    ImGui.Separator();
                    if (ImGui.MenuItem($"{FontAwesome.Save} Save"))
                    {
                        WriteContentNPL();
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
                    ImGui.EndMenu();
                }
#if RELEASE
                if (ImGui.BeginMenu("Content"))
                {
                    if (ImGui.BeginMenu($"{FontAwesome.FileAlt} Build Verbosity"))
                    {
                        var verbosities = Enum.GetNames(typeof(LoggerVerbosity));
                        foreach (var verbosity in verbosities)
                        {
                            bool selected = verbosity == _buildContentVerbosity.ToString();
                            if (ImGui.MenuItem(verbosity, "", selected))
                            {
                                _buildContentVerbosity = Enum.Parse<LoggerVerbosity>(verbosity);
                            }
                        }
                        ImGui.EndMenu();
                    }
                    ImGui.Separator();
                    if (ImGui.MenuItem($"{FontAwesome.Igloo} Build Now", _buildContentEnabled))
                    {
                        _buildContentEnabled = false;

                        var projDir = Directory.GetParent(Directory.GetCurrentDirectory());
                        var process = new Process()
                        {
                            StartInfo = new ProcessStartInfo(
                                "dotnet", $"msbuild {projDir} /t:RunContentBuilder /fl /flp:logfile={AppSettings.BuildContentLogPath};verbosity={_buildContentVerbosity.ToString().ToLower()}"),
                            EnableRaisingEvents = true
                        };
                        process.Exited += (sender, e) => { _buildContentEnabled = true; };
                        process.Start();
                    }
                    ImGui.EndMenu();
                }
#endif
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
                            else ModalDescriptor.SetFileNotFound(AppSettings.ImportantLogPath, "Note: this log file will only be created on errors or important events.");
                        }
                        ImGui.EndMenu();
                    }
                    if (ImGui.MenuItem($"{FontAwesome.AddressBook} About"))
                    {

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
                ImGui.PushTextWrapPos(viewport.Size.X / 2f - 120f);
                ImGui.TextWrapped(message);
                ImGui.PopTextWrapPos();

                ImGui.Spacing(); ImGui.Spacing(); ImGui.Spacing(); ImGui.Spacing();

                bool error = false;
                if (ModalDescriptor.MessageType == MessageType.AddContent || ModalDescriptor.MessageType == MessageType.EditContent)
                {
                    if (ImGui.InputTextWithHint("name", "textures / sound / music / etc.", ref ContentDescriptor.Name, 9999))
                    {
                        Extensions.NumberlessRef(ref ContentDescriptor.Name);
                    }

                    if (ModalDescriptor.MessageType == MessageType.AddContent)
                    {
                        var existingItem = _jsonObject["content"].AsObject().Select(x => x.Key).ToList().Find(x => x.Equals(ContentDescriptor.Name));
                        if (existingItem != null)
                        {
                            error = true;
                            ImGui.TextColored(ImGui.GetStyle().Colors[(int)ImGuiCol.TabActive], "Already Exists");
                        }

                        ImGui.InputTextWithHint("extension", ".png / .jpg / .ogg / etc.", ref ContentDescriptor.Extension, 9999);
                    }
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
                        WriteContentNPL();
                        ClosePopupModal();
                        return true;
                    }
                    ImGui.PopStyleColor(); ImGui.PopStyleColor(); ImGui.PopStyleColor();
                }

                ImGui.SameLine(ImGui.GetContentRegionMax().X - (buttonWidth * buttonCountRightAligned) - ImGui.GetStyle().ItemSpacing.X * buttonCountRightAligned);

                if (error) ImGui.BeginDisabled();
                if (ImGui.Button("OK", new Num.Vector2(buttonWidth, 0)) || ImGui.IsKeyPressed(ImGuiKey.Enter))
                {
                    if (ModalDescriptor.MessageType == MessageType.AddContent)
                    {
                        JsonObject content = new()
                        {
                            ["path"] = ContentDescriptor.Extension,
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
                if (error) ImGui.EndDisabled();

                if (hasCancel)
                {
                    ImGui.SameLine();
                    if (ImGui.Button("Cancel", new Num.Vector2(buttonWidth, 0)) || ImGui.IsKeyPressed(ImGuiKey.Enter))
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
    }
}
