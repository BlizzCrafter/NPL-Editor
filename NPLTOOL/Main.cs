using ImGuiNET;
using Microsoft.VisualBasic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using NPLTOOL.GUI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
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

        private List<NPLITEM> NPLITEMS = new List<NPLITEM>();

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
                //Debug.WriteLine(root.ToString());

                var references = _jsonObject["references"].AsArray();
                foreach (var reference in references)
                {
                    //Debug.WriteLine(reference.ToString());
                }

                var content = _jsonObject["content"]["contentList"];
                //Debug.WriteLine(content.ToString());

                dynamic modifiedDataKey = null;
                dynamic modifiedItemKey = null;
                dynamic modifiedItemParamKey = null;
                dynamic modifiedDataValue = null;
                foreach (var data in _jsonObject["content"].AsObject())
                {
                    var nplItem = new NPLITEM();
                    nplItem.Key = data.Key;
                    // data.Key = contentList

                    var category = _jsonObject["content"][data.Key];
                    //Debug.WriteLine(category.ToString());

                    ImGui.PushItemWidth(ImGui.GetContentRegionAvail().X - 150 - ImGui.GetStyle().IndentSpacing);
                    if (ImGui.TreeNodeEx(data.Key, ImGuiTreeNodeFlags.DefaultOpen | ImGuiTreeNodeFlags.SpanAllColumns))
                    {
                        foreach (var inData in category.AsObject())
                        {
                            var itemKey = inData.Key; //path
                            var itemValue = inData.Value; //value
                            //Debug.WriteLine(itemKey);

                            nplItem.Add(itemKey, itemValue);

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
                                        //Debug.WriteLine(arrData.ToString());
                                    }
                                    ImGui.TreePop();
                                }
                                ImGui.PopItemWidth();
                            }
                            else if (itemKey == "processorParam")
                            {
                                ImGui.PushItemWidth(ImGui.CalcItemWidth() - ImGui.GetFrameHeight() - ImGui.GetStyle().IndentSpacing - ImGui.GetStyle().ItemSpacing.X);
                                if (ImGui.TreeNodeEx(itemKey, ImGuiTreeNodeFlags.DefaultOpen | ImGuiTreeNodeFlags.Leaf | ImGuiTreeNodeFlags.SpanAllColumns))
                                {
                                    foreach (var parameter in itemValue.AsObject())
                                    {
                                        var parameterKey = parameter.Key; //e.g. ColorKeyColor
                                        var parameterValue = parameter.Value; //e.g. 255,0,255,255

                                        var index = nplItem.GetParameterIndex(parameterKey, true);

                                        if (parameterKey == "ColorKeyColor")
                                        {
                                            string[] rgbValues = parameterValue.ToString().Split(',');
                                            int red = int.Parse(rgbValues[0]);
                                            int green = int.Parse(rgbValues[1]);
                                            int blue = int.Parse(rgbValues[2]);
                                            int alpha = int.Parse(rgbValues[3]);
                                            var color = new Color(red, green, blue, alpha).ToSystemVector4();

                                            ImGui.PushStyleColor(ImGuiCol.Button, color);
                                            ImGui.BeginDisabled(true);
                                            ImGui.Button(" ", new Num.Vector2(ImGui.GetFrameHeight())); ImGui.SameLine();
                                            ImGui.EndDisabled();
                                            ImGui.PopStyleColor();
                                            if (ImGui.InputTextWithHint(parameterKey, itemValue.ToString(), ref nplItem.ProcessorParameters[index], 9999))
                                            {
                                                var sColor = nplItem.ProcessorParameters[index].Split(':');

                                                nplItem.ProcessorParameters[index] = nplItem.GetParameterString(parameterKey, sColor[1]);

                                                ModifyDataParam(data.Key, itemKey, parameterKey, sColor[1],
                                                    out modifiedDataKey, out modifiedItemKey, out modifiedItemParamKey, out modifiedDataValue);
                                            }
                                        }
                                    }
                                    ImGui.TreePop();
                                }
                                ImGui.PopItemWidth();
                            }
                            else
                            {
                                if (itemKey == "path")
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
                                else
                                {
                                    var i = nplItem.GetParameterIndex(itemKey);

                                    ImGui.InputTextWithHint(itemKey, itemValue.ToString(), ref nplItem.Parameters[i], 9999);
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
