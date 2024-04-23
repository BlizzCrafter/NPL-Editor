using ImGuiNET;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NPLTOOL.Common;
using NPLTOOL.GUI;
using NPLTOOL.Parameter;
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

        private Texture2D _xnaTexture;
        private Num.Vector3 clear_color = new Num.Vector3(114f / 255f, 144f / 255f, 154f / 255f);

        private JsonNode _jsonObject;
        private string _nplJsonFilePath;

        private readonly List<ContentItem> NPLITEMS = new();

        public Main()
        {
            _graphics = new GraphicsDeviceManager(this);
            _graphics.PreferredBackBufferWidth = 800;
            _graphics.PreferredBackBufferHeight = 500;
            
            //Window.AllowUserResizing = true;

            // Currently not usable in DesktopGL because of this bug:
            // https://github.com/MonoGame/MonoGame/issues/7914
            _graphics.PreferMultiSampling = false;

            Content.RootDirectory = "Content";
            IsMouseVisible = true;
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
            // Texture loading example

            // First, load the texture as a Texture2D (can also be done using the XNA/FNA content pipeline)
            _xnaTexture = CreateTexture(GraphicsDevice, 300, 150, pixel =>
            {
                var red = (pixel % 300) / 2;
                return new Color(red, 1, 1);
            });

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

            // Call BeforeLayout first to set things up
            _imGuiRenderer.BeforeLayout(gameTime);

            // Draw our UI
            ImGuiLayout();

            // Call AfterLayout now to finish up and draw all the things
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
                    var processor = data.Value["processor"]?.ToString();
                    var nplItem = new ContentItem(data.Key, processor); //e.g. data.Key = contentList

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
                                                if (ImGui.ColorEdit4(parameterKey, ref nplItem.ParameterProcessor<TextureProcessorParameter>().ColorKeyColor,
                                                    ImGuiColorEditFlags.NoOptions | ImGuiColorEditFlags.NoPicker | ImGuiColorEditFlags.NoTooltip))
                                                {
                                                    var xColor = nplItem.ParameterProcessor<TextureProcessorParameter>().ColorKeyColor.ToXNA();
                                                    var sColor = $"{xColor.R},{xColor.G},{xColor.B},{xColor.A}";

                                                    ModifyDataParam(data.Key, itemKey, parameterKey, sColor,
                                                        out modifiedDataKey, out modifiedItemKey, out modifiedItemParamKey, out modifiedDataValue);
                                                }
                                            }
                                            else if (Enum.Parse<ParameterKey>(parameterKey) == ParameterKey.ColorKeyEnabled)
                                            {
                                                if (ImGui.Checkbox(parameterKey, ref nplItem.ParameterProcessor<TextureProcessorParameter>().ColorKeyEnabled))
                                                {
                                                    ModifyDataParam(data.Key, itemKey, parameterKey, nplItem.ParameterProcessor<TextureProcessorParameter>().ColorKeyEnabled.ToString(),
                                                        out modifiedDataKey, out modifiedItemKey, out modifiedItemParamKey, out modifiedDataValue);
                                                }
                                            }
                                            else if (Enum.Parse<ParameterKey>(parameterKey) == ParameterKey.GenerateMipmaps)
                                            {
                                                if (ImGui.Checkbox(parameterKey, ref nplItem.ParameterProcessor<TextureProcessorParameter>().GenerateMipmaps))
                                                {
                                                    ModifyDataParam(data.Key, itemKey, parameterKey, nplItem.ParameterProcessor<TextureProcessorParameter>().GenerateMipmaps.ToString(),
                                                        out modifiedDataKey, out modifiedItemKey, out modifiedItemParamKey, out modifiedDataValue);
                                                }
                                            }
                                            else if (Enum.Parse<ParameterKey>(parameterKey) == ParameterKey.PremultiplyAlpha)
                                            {
                                                if (ImGui.Checkbox(parameterKey, ref nplItem.ParameterProcessor<TextureProcessorParameter>().PremultiplyAlpha))
                                                {
                                                    ModifyDataParam(data.Key, itemKey, parameterKey, nplItem.ParameterProcessor<TextureProcessorParameter>().PremultiplyAlpha.ToString(),
                                                        out modifiedDataKey, out modifiedItemKey, out modifiedItemParamKey, out modifiedDataValue);
                                                }
                                            }
                                            else if (Enum.Parse<ParameterKey>(parameterKey) == ParameterKey.ResizeToPowerOfTwo)
                                            {
                                                if (ImGui.Checkbox(parameterKey, ref nplItem.ParameterProcessor<TextureProcessorParameter>().ResizeToPowerOfTwo))
                                                {
                                                    ModifyDataParam(data.Key, itemKey, parameterKey, nplItem.ParameterProcessor<TextureProcessorParameter>().ResizeToPowerOfTwo.ToString(),
                                                        out modifiedDataKey, out modifiedItemKey, out modifiedItemParamKey, out modifiedDataValue);
                                                }
                                            }
                                            else if (Enum.Parse<ParameterKey>(parameterKey) == ParameterKey.MakeSquare)
                                            {
                                                if (ImGui.Checkbox(parameterKey, ref nplItem.ParameterProcessor<TextureProcessorParameter>().MakeSquare))
                                                {
                                                    ModifyDataParam(data.Key, itemKey, parameterKey, nplItem.ParameterProcessor<TextureProcessorParameter>().MakeSquare.ToString(),
                                                        out modifiedDataKey, out modifiedItemKey, out modifiedItemParamKey, out modifiedDataValue);
                                                }
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
                            else if (itemKey == "importer")
                            {
                                var index = PipelineTypes.GetImporterIndex(itemValue.ToString());
                                nplItem.SelectedImporterIndex = index;

                                var importerNames = PipelineTypes.Importers.Select(x => x.DisplayName).ToArray();

                                if (ImGui.Combo(itemKey, ref nplItem.SelectedImporterIndex, importerNames, importerNames.Length))
                                {
                                    itemValue = PipelineTypes.Importers[nplItem.SelectedImporterIndex].TypeName;

                                    ModifyData(data.Key, itemKey, itemValue,
                                        out modifiedDataKey, out modifiedItemKey, out modifiedDataValue);
                                }
                            }
                            else if (itemKey == "processor")
                            {
                                var index = PipelineTypes.GetProcessorIndex(itemValue.ToString());
                                nplItem.SelectedProcessorIndex = index;

                                var processorNames = PipelineTypes.Processors.Select(x => x.DisplayName).ToArray();

                                if (ImGui.Combo(itemKey, ref nplItem.SelectedProcessorIndex, processorNames, processorNames.Length))
                                {
                                    itemValue = PipelineTypes.Processors[nplItem.SelectedProcessorIndex].TypeName;

                                    ModifyData(data.Key, itemKey, itemValue,
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

        public static Texture2D CreateTexture(GraphicsDevice device, int width, int height, Func<int, Color> paint)
        {
            //initialize a texture
            var texture = new Texture2D(device, width, height);

            //the array holds the color for each pixel in the texture
            Color[] data = new Color[width * height];
            for (var pixel = 0; pixel < data.Length; pixel++)
            {
                //the function applies the color according to the specified pixel
                data[pixel] = paint(pixel);
            }

            //set the color
            texture.SetData(data);

            return texture;
        }
    }
}
