﻿using ImGuiNET;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NPLEditor.Common;
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

        public static class ModifyDataDescriptor
        {
            public static string DataKey { get; private set; }
            public static string ItemKey { get; private set; }
            public static string ParamKey { get; private set; }
            public static dynamic Value { get; private set; }

            public static bool HasData { get; private set; } = false;
            public static bool ParamModify { get; private set; } = false;

            public static void Set(string dataKey, string itemKey, dynamic dataValue, string paramKey = "")
            {
                HasData = true;

                DataKey = dataKey;
                ItemKey = itemKey;
                ParamKey = paramKey;
                Value = dataValue;

                if (!string.IsNullOrEmpty(paramKey))
                {
                    ParamModify = true;
                }
            }

            public static void Reset()
            {
                HasData = false;
                ParamModify = false;

                DataKey = string.Empty;
                ItemKey = string.Empty;
                ParamKey = string.Empty;
                Value = null;
            }
        }
        public static class ModalDescriptor
        {
            public static string Title { get; private set; } = "";
            public static string Message { get; private set; } = "";
            public static bool IsOpen { get; private set; } = false;

            public static void SetFileNotFound(string filePath, string message)
            {
                string shortMessage = $"'{Path.GetFileName(filePath)}' not found.";
                string longMessage = message;
                Set(MessageType.FileNotFound, $"{shortMessage}\n\n{longMessage}");
            }

            public static void Set(MessageType messageType, string message)
            {
                switch (messageType)
                {
                    case MessageType.FileNotFound:
                        {
                            Title = $"{FontAwesome.ExclamationTriangle} error";
                            break;
                        }
                    default: 
                        {
                            Title = $"{FontAwesome.ExclamationCircle} info";
                            break;
                        }
                }
                Set(Title, message);
            }

            public static void Set(string title, string message)
            {
                Title = title;
                Message = message;
                IsOpen = true;
            }

            public static void Reset()
            {
                Title = "";
                Message = "";
                IsOpen = false;
            }
        }

        private GraphicsDeviceManager _graphics;
        private ImGuiRenderer _imGuiRenderer;
        private Num.Vector3 _clearColor = new(114f / 255f, 144f / 255f, 154f / 255f);

        private JsonNode _jsonObject;
        private string _nplJsonFilePath;
        private bool _treeNodesCollapsed = false;
        private bool _settingsVisible = true;
        private bool _contentListVisible = true;
        private bool _dummyBoolIsOpen = true;

        public Main()
        {
            _graphics = new GraphicsDeviceManager(this);
            _graphics.PreferredBackBufferWidth = 800;
            _graphics.PreferredBackBufferHeight = 500;

            // Currently not usable in DesktopGL because of this bug:
            // https://github.com/MonoGame/MonoGame/issues/7914
            _graphics.PreferMultiSampling = false;

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

        private void MenuBar()
        {
            bool changeTreeVisibility = false;
            if (ImGui.BeginMenuBar())
            {
                if (ImGui.BeginMenu("Options"))
                {
                    ImGui.SeparatorText("View");
                    if (ImGui.MenuItem(_treeNodesCollapsed ? "Unfold Trees" : "Fold Trees"))
                    {
                        _treeNodesCollapsed = !_treeNodesCollapsed;
                        changeTreeVisibility = true;
                    }
                    if (ImGui.MenuItem(_settingsVisible ? "Hide Settings" : "Show Settings"))
                    {
                        _settingsVisible = !_settingsVisible;
                    }
                    if (ImGui.MenuItem(_contentListVisible ? "Hide Content List" : "Show Content List"))
                    {
                        _contentListVisible = !_contentListVisible;
                    }
                    ImGui.EndMenu();
                }
                if (ImGui.BeginMenu("Help"))
                {
                    if (ImGui.BeginMenu("Logs"))
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
                    if (ImGui.MenuItem("About"))
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
                    ImGui.GetStateStorage().SetInt(id, _treeNodesCollapsed ? 0 : 1);
                }
            }
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
                        EditButton(EditButtonPosition.Before, FontAwesome.Edit, 0, true, true);
                        
                        ImGui.SetCursorPosX(ImGui.GetStyle().IndentSpacing + ImGui.GetStyle().ItemSpacing.X);
                        ImGui.PushStyleColor(ImGuiCol.Text, ImGui.GetStyle().Colors[(int)ImGuiCol.NavHighlight]);
                        ImGui.PushItemWidth(ImGui.GetContentRegionAvail().X - 150 - ImGui.GetStyle().IndentSpacing);
                        if (ImGui.TreeNodeEx("settings", ImGuiTreeNodeFlags.DefaultOpen | ImGuiTreeNodeFlags.SpanAllColumns))
                        {
                            ImGui.PopStyleColor();

                            ImGui.BeginDisabled();
                            var root = _jsonObject["root"];
                            var rootValue = root.ToString();
                            ImGui.InputText("root", ref rootValue, 9999, ImGuiInputTextFlags.ReadOnly);
                            ImGui.EndDisabled();

                            var references = _jsonObject["references"].AsArray();
                            ArrayEditor("Reference", references, out _, out _, out bool itemChanged);
                            {
                                if (itemChanged) PipelineTypes.Reset();
                            }
                            PipelineTypes.Load(references
                                .Where(x => !string.IsNullOrEmpty(x.ToString()))
                                .Select(x => x.ToString()).ToArray());

                            ImGui.TreePop();
                        }
                        ImGui.PopStyleColor();
                        ImGui.PopItemWidth();
                    }

                    var content = _jsonObject["content"]["contentList"];

                    JsonObject modifiedProcessorParam = null;
                    foreach (var data in _jsonObject["content"].AsObject())
                    {
                        var isContentList = data.Key.Equals("contentList");
                        if (isContentList && !_contentListVisible) continue;

                        EditButton(EditButtonPosition.Before, FontAwesome.Edit, 0, true, isContentList);

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
                        }
                        if (processorParam == null)
                        {
                            WriteJsonProcessorParameters(processorName, data);
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
                                    if (isContentList) ImGui.BeginDisabled();
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
                                    if (isContentList) ImGui.EndDisabled();
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

        private void WriteJsonProcessorParameters(
            string processor, KeyValuePair<string, JsonNode> data)
        {
            if (GetJsonProcessorParameters(processor, out JsonObject props))
            {
                data.Value["processorParam"] = props;
            }
        }

        private void WriteContentNPL()
        {
            string jsonString = JsonSerializer.Serialize(_jsonObject, new JsonSerializerOptions()
            {
                WriteIndented = true
            });
            File.WriteAllText(_nplJsonFilePath, jsonString);
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
                ImGui.PopStyleColor();
                ImGui.PopStyleColor();
                ImGui.PopStyleColor();
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

        private bool PopupModal(string title, string message)
        {
            ImGuiWindowFlags modalWindowFlags = ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoDecoration
                | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.Modal | ImGuiWindowFlags.Popup | ImGuiWindowFlags.NoCollapse;

            ImGuiViewportPtr viewport = ImGui.GetMainViewport();

            ImGui.SetNextWindowPos(viewport.GetCenter(), ImGuiCond.Always, new Num.Vector2(0.5f));
            ImGui.SetNextWindowSize(new Num.Vector2(viewport.Size.X / 2f, 150f));
            if (ImGui.BeginPopupModal(ModalDescriptor.Title, ref _dummyBoolIsOpen, modalWindowFlags))
            {
                ImGui.SeparatorText(title);

                ImGui.SetCursorPos(new Num.Vector2(ImGui.GetStyle().ItemSpacing.X / 2f, ImGui.GetFrameHeight()));
                ImGui.PushTextWrapPos(ImGui.GetContentRegionMax().X - 50 - (ImGui.GetStyle().ItemSpacing.X / 2f) * 2f);
                ImGui.TextWrapped(message);
                ImGui.PopTextWrapPos();

                ImGui.SetCursorPos(new Num.Vector2(
                    ImGui.GetContentRegionMax().X - 50 - ImGui.GetStyle().ItemSpacing.X / 2f,
                    ImGui.GetContentRegionMax().Y - ImGui.GetFrameHeight() - ImGui.GetStyle().ItemSpacing.X / 2f));
                if (ImGui.Button("OK", new Num.Vector2(50, 0)) || ImGui.IsKeyPressed(ImGuiKey.Enter))
                {
                    ModalDescriptor.Reset();
                    ImGui.CloseCurrentPopup();
                    return true;
                }
                ImGui.EndPopup();
            }
            return false;
        }

        #endregion ImGui Widgets
    }
}
