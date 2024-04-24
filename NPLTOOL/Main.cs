﻿using ImGuiNET;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NPLTOOL.Common;
using NPLTOOL.GUI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using Color = Microsoft.Xna.Framework.Color;
using Num = System.Numerics;

namespace NPLTOOL
{
    public class Main : Game
    {
        private GraphicsDeviceManager _graphics;
        private ImGuiRenderer _imGuiRenderer;

        private Num.Vector3 clear_color = new Num.Vector3(114f / 255f, 144f / 255f, 154f / 255f);

        private JsonNode _jsonObject;
        private string _nplJsonFilePath;

        private readonly List<ContentItem> NPLITEMS = new();

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
        }

        protected override void LoadContent()
        {
            //TODO: Use filepath of a startup argument instead.
            //string[] args = Environment.GetCommandLineArgs();

            string workingDir = Directory.GetCurrentDirectory();
            string projDir = Directory.GetParent(workingDir).Parent.Parent.FullName;
            _nplJsonFilePath = Path.Combine(projDir, "Content.npl");
            
            var jsonString = File.ReadAllText(_nplJsonFilePath);
            _jsonObject = JsonNode.Parse(jsonString);

            base.LoadContent();
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(new Color(clear_color.X, clear_color.Y, clear_color.Z));

            _imGuiRenderer.BeforeLayout(gameTime);
            ImGuiLayout();
            _imGuiRenderer.AfterLayout();

            base.Draw(gameTime);
        }

        protected virtual void ImGuiLayout()
        {
            bool dummyBool = true;

            var mainWindowFlags = 
                ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoResize 
                | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoBringToFrontOnFocus | ImGuiWindowFlags.NoNavFocus
                | ImGuiWindowFlags.NoBackground;

            var windowFlags = ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoDecoration
                | ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.AlwaysVerticalScrollbar;

            ImGuiViewportPtr viewport = ImGui.GetMainViewport();

            ImGui.SetNextWindowPos(viewport.Pos);
            ImGui.SetNextWindowSize(viewport.Size);
            ImGui.SetNextWindowViewport(viewport.ID);
            ImGui.Begin("Main", ref dummyBool, mainWindowFlags);

            ImGui.SetNextWindowPos(viewport.Pos);
            ImGui.SetNextWindowSize(viewport.Size);
            ImGui.SetNextWindowViewport(viewport.ID);
            if (ImGui.Begin("JsonTree", ref dummyBool, windowFlags))
            {
                var root = _jsonObject["root"];

                var references = _jsonObject["references"].AsArray();
                PipelineTypes.Load(references.Select(x => x.ToString()).ToArray());

                var content = _jsonObject["content"]["contentList"];

                dynamic modifiedDataKey = null;
                dynamic modifiedItemKey = null;
                dynamic modifiedItemParamKey = null;
                dynamic modifiedDataValue = null;
                foreach (var data in _jsonObject["content"].AsObject())
                {
                    var importer = data.Value["importer"]?.ToString();
                    var processor = data.Value["processor"]?.ToString();
                    var nplItem = new ContentItem(data.Key, importer, processor); //e.g. data.Key = contentList

                    var categoryObject = _jsonObject["content"][data.Key];

                    ImGui.PushItemWidth(ImGui.GetContentRegionAvail().X - 150 - ImGui.GetStyle().IndentSpacing);
                    if (ImGui.TreeNodeEx(data.Key, ImGuiTreeNodeFlags.DefaultOpen | ImGuiTreeNodeFlags.SpanAllColumns))
                    {
                        foreach (var categoryItem in categoryObject.AsObject())
                        {
                            var itemKey = categoryItem.Key; //e.g. path
                            var itemValue = categoryItem.Value; //e.g. "C:\\"

                            nplItem.SetParameter(itemKey, itemValue);

                            if (itemKey == "watch")
                            {
                                ImGui.PushItemWidth(ImGui.CalcItemWidth() - ImGui.GetStyle().IndentSpacing);
                                if (ImGui.TreeNodeEx(itemKey, ImGuiTreeNodeFlags.DefaultOpen | ImGuiTreeNodeFlags.Leaf | ImGuiTreeNodeFlags.SpanAllColumns))
                                {
                                    for (int i = 0; i < itemValue.AsArray().Count; i++)
                                    {
                                        if (ImGui.InputTextWithHint($"##{i}", itemValue[i].ToString(), ref nplItem.Watch[i], 9999))
                                        {
                                            itemValue[i] = nplItem.Watch[i];

                                            ModifyData(data.Key, itemKey, itemValue, 
                                                out modifiedDataKey, out modifiedItemKey, out modifiedDataValue);
                                        }
                                    }
                                    ImGui.TreePop();
                                }
                                ImGui.PopItemWidth();
                            }
                            else if (itemKey == "processorParam")
                            {
                                ImGui.PushItemWidth(ImGui.CalcItemWidth() - ImGui.GetStyle().IndentSpacing);
                                if (ImGui.TreeNodeEx(itemKey, ImGuiTreeNodeFlags.DefaultOpen | ImGuiTreeNodeFlags.Leaf | ImGuiTreeNodeFlags.SpanAllColumns))
                                {
                                    if (ImGui.BeginTable("Checkmarks", 2, ImGuiTableFlags.NoClip))
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

                                            if (Enum.Parse<ParameterKey>(parameterKey) == ParameterKey.ColorKeyColor)
                                            {
                                                if (ColorEdit(nplItem, parameterKey))
                                                {
                                                    ModifyDataParam(data.Key, itemKey, parameterKey, nplItem.Property(parameterKey).Value,
                                                        out modifiedDataKey, out modifiedItemKey, out modifiedItemParamKey, out modifiedDataValue);
                                                }
                                            }
                                            else if (Enum.Parse<ParameterKey>(parameterKey) == ParameterKey.ColorKeyEnabled)
                                            {
                                                if (Checkbox(nplItem, parameterKey))
                                                {
                                                    ModifyDataParam(data.Key, itemKey, parameterKey, nplItem.Property(parameterKey).Value,
                                                        out modifiedDataKey, out modifiedItemKey, out modifiedItemParamKey, out modifiedDataValue);
                                                }
                                            }
                                            else if (Enum.Parse<ParameterKey>(parameterKey) == ParameterKey.GenerateMipmaps)
                                            {
                                                if (Checkbox(nplItem, parameterKey))
                                                {
                                                    ModifyDataParam(data.Key, itemKey, parameterKey, nplItem.Property(parameterKey).Value,
                                                        out modifiedDataKey, out modifiedItemKey, out modifiedItemParamKey, out modifiedDataValue);
                                                }
                                            }
                                            else if (Enum.Parse<ParameterKey>(parameterKey) == ParameterKey.PremultiplyAlpha)
                                            {
                                                if (Checkbox(nplItem, parameterKey))
                                                {
                                                    ModifyDataParam(data.Key, itemKey, parameterKey, nplItem.Property(parameterKey).Value,
                                                        out modifiedDataKey, out modifiedItemKey, out modifiedItemParamKey, out modifiedDataValue);
                                                }
                                            }
                                            else if (Enum.Parse<ParameterKey>(parameterKey) == ParameterKey.ResizeToPowerOfTwo)
                                            {
                                                if (Checkbox(nplItem, parameterKey))
                                                {
                                                    ModifyDataParam(data.Key, itemKey, parameterKey, nplItem.Property(parameterKey).Value,
                                                        out modifiedDataKey, out modifiedItemKey, out modifiedItemParamKey, out modifiedDataValue);
                                                }
                                            }
                                            else if (Enum.Parse<ParameterKey>(parameterKey) == ParameterKey.MakeSquare)
                                            {
                                                if (Checkbox(nplItem, parameterKey))
                                                {
                                                    ModifyDataParam(data.Key, itemKey, parameterKey, nplItem.Property(parameterKey).Value,
                                                        out modifiedDataKey, out modifiedItemKey, out modifiedItemParamKey, out modifiedDataValue);
                                                }
                                            }
                                            else if (Enum.Parse<ParameterKey>(parameterKey) == ParameterKey.TextureFormat)
                                            {
                                            }
                                        }
                                    }
                                    ImGui.EndTable();
                                    ImGui.TreePop();
                                }
                                ImGui.PopItemWidth();
                            }
                            else if (itemKey == "path")
                            {
                                if (ImGui.InputTextWithHint(" ", itemValue.ToString(), ref nplItem._path, 9999))
                                {
                                    itemValue = nplItem._path;

                                    ModifyData(data.Key, itemKey, itemValue,
                                            out modifiedDataKey, out modifiedItemKey, out modifiedDataValue);
                                }
                            }
                            else if (itemKey == "action")
                            {
                                ImGui.InputTextWithHint(itemKey, itemValue.ToString(), ref nplItem.Action, 9999);
                            }
                            else if (itemKey == "recursive")
                            {
                                ImGui.SameLine(); ImGui.Checkbox(itemKey, ref nplItem.Recursive);
                            }
                            else if (itemKey == "importer" || itemKey == "processor")
                            {
                                if (Combo(nplItem, itemKey, out string value))
                                {
                                    ModifyData(data.Key, itemKey, value,
                                        out modifiedDataKey, out modifiedItemKey, out modifiedDataValue);
                                }
                            }
                        }
                        ImGui.TreePop();
                    }
                    ImGui.PopItemWidth();
                }
                if (modifiedItemParamKey != null)
                {
                    _jsonObject["content"][modifiedDataKey][modifiedItemKey][modifiedItemParamKey] = modifiedDataValue;
                    WriteContentNPL();
                }
                else if (modifiedDataKey != null && modifiedItemKey != null && modifiedDataValue != null)
                {
                    _jsonObject["content"][modifiedDataKey][modifiedItemKey] = modifiedDataValue;
                    WriteContentNPL();
                }
            }
            ImGui.End();

            ImGui.End();
        }

        private void WriteContentNPL()
        {
            string jsonString = JsonSerializer.Serialize(_jsonObject, new JsonSerializerOptions()
            {
                WriteIndented = true
            });
            File.WriteAllText(_nplJsonFilePath, jsonString);
        }

        private void ModifyData(
            dynamic dataKey, dynamic itemKey, dynamic itemValue,
            out dynamic modifiedDataKey, out dynamic modifiedItemKey, out dynamic modifiedDataValue)
        {
            modifiedDataKey = dataKey;
            modifiedItemKey = itemKey;
            modifiedDataValue = itemValue;
        }
        private void ModifyDataParam(
            dynamic dataKey, dynamic itemKey, dynamic itemParameter, dynamic itemValue, 
            out dynamic modifiedDataKey, out dynamic modifiedItemKey, out dynamic modifiedItemParameter, out dynamic modifiedDataValue)
        {
            modifiedDataKey = dataKey;
            modifiedItemKey = itemKey;
            modifiedItemParameter = itemParameter;
            modifiedDataValue = itemValue;
        }

        private bool ColorEdit(ContentItem nplItem, string parameterKey)
        {
            var value = nplItem.Vector4Property(parameterKey);

            if (ImGui.ColorEdit4(parameterKey, ref value,
                ImGuiColorEditFlags.NoOptions | ImGuiColorEditFlags.NoPicker | ImGuiColorEditFlags.NoTooltip))
            {
                var xColor = value.ToXNA();
                var sColor = $"{xColor.R},{xColor.G},{xColor.B},{xColor.A}";

                nplItem.Property(parameterKey).Value = sColor;

                return true;
            }
            return false;
        }

        private bool Checkbox(ContentItem nplItem, string parameterKey)
        {
            var value = nplItem.BoolProperty(parameterKey);

            if (ImGui.Checkbox(parameterKey, ref value))
            {
                nplItem.Property(parameterKey).Value = value.ToString();

                return true;
            }
            return false;
        }

        private bool Combo(ContentItem nplItem, string itemKey, out string value)
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
    }
}
