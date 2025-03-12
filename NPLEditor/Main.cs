using ImGuiNET;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NPLEditor.Common;
using NPLEditor.Data;
using NPLEditor.Enums;
using NPLEditor.GUI;
using NPLEditor.GUI.Widgets;
using Serilog;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Color = Microsoft.Xna.Framework.Color;
using ContentItem = NPLEditor.Common.ContentItem;
using Num = System.Numerics;

namespace NPLEditor
{
    public class Main : Game
    {
        public static bool ScrollLogToBottom { get; set; }
        public static bool ScrollContentToBottom { get; set; }

        public static bool OrderingOptionsVisible = false;

        private GraphicsDeviceManager _graphics;
        private ImGuiRenderer _imGuiRenderer;
        private bool _clearLogViewOnBuild = true;
        private bool _launchDebuggerContent = false;
        private bool _incrementalContent = false;
        private bool _treeNodesOpen = true;
        private bool _logOpen = false;
        private bool _settingsVisible = true;
        private bool _dummyBoolIsOpen = true;

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
                            Widgets.EditButton(EditButtonPosition.Before, FontAwesome.Edit, -1, true, true);

                            ImGui.SetCursorPosX(ImGui.GetStyle().IndentSpacing + ImGui.GetStyle().ItemSpacing.X);
                            ImGui.PushStyleColor(ImGuiCol.Text, ImGui.GetStyle().Colors[(int)ImGuiCol.NavHighlight]);
                            ImGui.PushItemWidth(ImGui.GetContentRegionAvail().X - 150 + ImGui.GetStyle().IndentSpacing);
                            if (ImGui.TreeNodeEx("settings", ImGuiTreeNodeFlags.DefaultOpen | ImGuiTreeNodeFlags.SpanAllColumns))
                            {
                                ImGui.PopStyleColor();
                                ContentBuilder.SettingsEditor();
                                ImGui.TreePop();
                            }
                            ImGui.PopStyleColor();
                            ImGui.PopItemWidth();
                        }

                        ContentBuilder.ContentEditor();
                        if (ScrollContentToBottom && ImGui.GetScrollMaxY() > ImGui.GetScrollY())
                        {
                            ScrollContentToBottom = false;
                            ImGui.SetScrollHereY(1.0f);
                        }
                        ImGui.End();
                    }
                    else if (_logOpen)
                    {
                        ImGui.Spacing(); ImGui.Spacing(); ImGui.Spacing(); ImGui.Spacing();

                        ImGui.PushStyleColor(ImGuiCol.FrameBg, ImGui.GetStyle().Colors[(int)ImGuiCol.WindowBg]);
                        ImGui.TextUnformatted(NPLEditorSink.Output.ToString());
                        ImGui.PopStyleColor();

                        ImGui.Spacing(); ImGui.Spacing(); ImGui.Spacing(); ImGui.Spacing();

                        if (!ContentBuilder.BuildContentRunning)
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
                                ContentBuilder.CancelBuildContent = true;
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

        public static void WriteJsonProcessorParameters(ContentItem nplItem)
        {
            var props = new JsonObject();
            var properties = nplItem.Processor.Properties;
            if (properties != null && properties.Any())
            {
                foreach (var property in properties)
                {
                    props.Add(property.Name, property.DefaultValue?.ToString() ?? "");
                }
            }
            ContentBuilder.JsonObject["content"][nplItem.Category]["processorParam"] = props;
        }

        public static void WriteContentNPL()
        {
            try
            {
                string jsonString = JsonSerializer.Serialize(ContentBuilder.JsonObject, new JsonSerializerOptions()
                {
                    WriteIndented = true
                });
                using (var fs = new FileStream(AppSettings.NPLJsonFilePath, FileMode.Create, FileAccess.Write, FileShare.Read))
                using (var writer = new StreamWriter(fs))
                {
                    writer.Write(jsonString);
                }

                Log.Debug("Content file successfully saved.");
            }
            catch (Exception e) { NPLLog.LogException(e, "SAVE ERROR"); }
        }

        protected override void OnExiting(object sender, ExitingEventArgs args)
        {
            WriteContentNPL();
            base.OnExiting(sender, args);
        }

        public static void MoveTreeItem(int i, bool down)
        {
            var content = ContentBuilder.JsonObject["content"].AsObject().ToList();

            foreach (var item in content)
            {
                ContentBuilder.JsonObject["content"].AsObject().Remove(item.Key);
            }

            for (int x = 0; x < content.Count; x++)
            {
                if (down)
                {
                    if (x == i + 1) ContentBuilder.JsonObject["content"].AsObject().Add(content[i]);
                    else if (x == i) ContentBuilder.JsonObject["content"].AsObject().Add(content[i + 1]);
                    else ContentBuilder.JsonObject["content"].AsObject().Add(content[x]);
                }
                else
                {
                    if (x == i - 1) ContentBuilder.JsonObject["content"].AsObject().Add(content[i]);
                    else if (x == i) ContentBuilder.JsonObject["content"].AsObject().Add(content[i - 1]);
                    else ContentBuilder.JsonObject["content"].AsObject().Add(content[x]);
                }
            }

            // Sort ContentList based on the new order in _jsonObject
            var sortedContentList = new Dictionary<string, ContentItem>();
            foreach (var item in ContentBuilder.JsonObject["content"].AsObject())
            {
                sortedContentList.Add(item.Key, ContentBuilder.ContentList[item.Key]);
            }
            ContentBuilder.ContentList = sortedContentList;

            WriteContentNPL();
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
                    if (ImGui.MenuItem($"{(OrderingOptionsVisible ? $"{FontAwesome.Eye} Hide Order Arrows" : $"{FontAwesome.EyeSlash} Show Order Arrows")}"))
                    {
                        OrderingOptionsVisible = !OrderingOptionsVisible!;
                    }
                    ImGui.EndMenu();
                }

                if (ImGui.BeginMenu("Build"))
                {
                    ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 10);
                    if (ImGui.MenuItem($"{FontAwesome.Igloo} Build Now", !ContentBuilder.BuildContentRunning))
                    {
                        ContentBuilder.BuildContentRunning = true;
                        _logOpen = true;

                        if (_clearLogViewOnBuild) NPLEditorSink.Output.Clear();

                        Task.Factory.StartNew(() => ContentBuilder.BuildContent());
                    }
                    ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 10);
                    if (ImGui.MenuItem($"{FontAwesome.Igloo} Rebuild Now", !ContentBuilder.BuildContentRunning))
                    {
                        ContentBuilder.BuildContentRunning = true;
                        _logOpen = true;

                        if (_clearLogViewOnBuild) NPLEditorSink.Output.Clear();

                        Task.Factory.StartNew(() => ContentBuilder.BuildContent(rebuildNow: true));
                    }
                    ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 10);
                    if (ImGui.MenuItem($"{FontAwesome.Broom} Clean Now"))
                    {
                        _logOpen = true;

                        if (_clearLogViewOnBuild) NPLEditorSink.Output.Clear();

                        ContentBuilder.RuntimeBuilder.CleanContent();
                        NPLLog.LogInfoHeadline(FontAwesome.Broom, "CONTENT CLEANED");
                    }
                    ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 5);
                    ImGui.SeparatorText("Options");
                    if (ImGui.MenuItem("Incremental", "", _incrementalContent))
                    {
                        _incrementalContent = !_incrementalContent;
                        ContentBuilder.RuntimeBuilder.Incremental = _incrementalContent;
                    }
                    if (ImGui.MenuItem("Debug Mode", "", _launchDebuggerContent))
                    {
                        _launchDebuggerContent = !_launchDebuggerContent;
                        ContentBuilder.RuntimeBuilder.LaunchDebugger = _launchDebuggerContent;
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
                ids.AddRange(ContentBuilder.JsonObject["content"].AsObject().Select(x => x.Key).ToArray());

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

                        if (ContentBuilder.ContentList.ContainsKey(ContentDescriptor.Name))
                        {
                            ContentDescriptor.Error("Already exists!");
                        }
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
                        ContentBuilder.JsonObject["content"].AsObject().Remove(ContentDescriptor.Category);
                        ContentBuilder.ContentList.Remove(ContentDescriptor.Category);
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

                        ContentBuilder.JsonObject["content"].AsObject().Add(ContentDescriptor.Name, content);
                        ContentBuilder.ContentList.Add(ContentDescriptor.Name, new ContentItem(ContentDescriptor.Name, ContentDescriptor.Path));
                        WriteContentNPL();

                        ScrollContentToBottom = true;
                    }
                    else if (ModalDescriptor.MessageType == MessageType.EditContent)
                    {
                        var node = ContentBuilder.JsonObject["content"][ContentDescriptor.Category];
                        var nodeIndex = ContentBuilder.JsonObject["content"].AsObject().Select(x => x.Key).ToList().IndexOf(ContentDescriptor.Category);
                        var content = ContentBuilder.JsonObject["content"].AsObject().ToList();

                        foreach (var item in content)
                        {
                            ContentBuilder.JsonObject["content"].AsObject().Remove(item.Key);
                        }

                        for (int i = 0; i < content.Count; i++)
                        {
                            if (i != nodeIndex) ContentBuilder.JsonObject["content"].AsObject().Add(content[i]);
                            else ContentBuilder.JsonObject["content"].AsObject().Add(ContentDescriptor.Name, node);
                        }

                        ContentBuilder.ContentList[ContentDescriptor.Category].Category = ContentDescriptor.Name;
                        ContentBuilder.ContentList[ContentDescriptor.Category].Path = ContentDescriptor.Path;
                        ContentBuilder.ContentList.ChangeKey(ContentDescriptor.Category, ContentDescriptor.Name);
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
    }
}