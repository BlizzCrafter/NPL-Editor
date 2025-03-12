using ImGuiNET;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content.Pipeline;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.RuntimeBuilder;
using NPLEditor.Data;
using NPLEditor.Enums;
using NPLEditor.GUI.Widgets;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;

namespace NPLEditor.Common
{
    public static partial class ContentBuilder
    {
        private static List<string> _tempReferences = [];

        /// <summary>
        /// Main settings of a Content.npl file (References, OutputDir, IntermediateDir, etc).
        /// Also loads pipeline types (TextureImporter, TextureProcessor, etc).
        /// </summary>
        public static void SettingsEditor()
        {
            if (ImGui.InputText("root", ref ContentRoot, 9999, ImGuiInputTextFlags.EnterReturnsTrue))
            {
                JsonObject["root"] = ContentRoot;
                Main.WriteContentNPL();
            }

            if (ImGui.InputText("intermediateDir", ref IntermediateDir, 9999, ImGuiInputTextFlags.EnterReturnsTrue))
            {
                JsonObject["intermediateDir"] = IntermediateDir;
                RuntimeBuilder.SetIntermediateDir(IntermediatePath);
                Log.Debug($"IntermediateDir: {IntermediatePath}");
                Main.WriteContentNPL();
            }
            Widgets.SimpleTooltip("Path", IntermediatePath, 800f);

            if (ImGui.InputText("outputDir", ref OutputDir, 9999, ImGuiInputTextFlags.EnterReturnsTrue))
            {
                JsonObject["outputDir"] = OutputDir;
                RuntimeBuilder.SetOutputDir(OutputPath);
                Log.Debug($"OutputDir: {OutputPath}");
                Main.WriteContentNPL();
            }
            Widgets.SimpleTooltip("Path", OutputPath, 800f);

            var platform = TargetPlatform.ToString();
            if (Widgets.ComboEnum(ref platform, "platform", Enum.GetNames(typeof(TargetPlatform))))
            {
                JsonObject["platform"] = platform;
                TargetPlatform = Enum.Parse<TargetPlatform>(platform);
                RuntimeBuilder.SetPlatform(TargetPlatform);
                Log.Debug($"IntermediateDir: {IntermediatePath}");
                Log.Debug($"OutputDir: {OutputPath}");
                Main.WriteContentNPL();
            }

            var graphicsProfile = GraphicsProfile.ToString();
            if (Widgets.ComboEnum(ref graphicsProfile, "graphicsProfile", Enum.GetNames(typeof(GraphicsProfile))))
            {
                JsonObject["graphicsProfile"] = graphicsProfile;
                GraphicsProfile = Enum.Parse<GraphicsProfile>(graphicsProfile);
                RuntimeBuilder.SetGraphicsProfile(GraphicsProfile);
                Main.WriteContentNPL();
            }

            if (ImGui.Checkbox("compress", ref Compress))
            {
                JsonObject["compress"] = Compress.ToString();
                RuntimeBuilder.SetCompressContent(Compress);
            }

            Widgets.ListEditor("Reference", _tempReferences, out bool itemAdded, out bool itemRemoved, out bool itemChanged);
            {
                if (itemAdded || itemChanged || itemRemoved)
                {
                    var jsonNode = new JsonNode[_tempReferences.Count];
                    for (int i = 0; i < _tempReferences.Count; i++)
                    {
                        jsonNode[i] = _tempReferences[i];
                    }
                    JsonObject["references"] = new JsonArray(jsonNode);
                    Main.WriteContentNPL();
                    GetAllReferences();
                }
            }
        }

        /// <summary>
        /// Build-Content of a Content.npl file (e.g. Textures, SoundEffects, Music, Videos, etc).
        /// </summary>
        public static void ContentEditor()
        {
            for (int i = 0, id = 0; i < ContentList.Count; i++, id--)
            {
                var nplItem = ContentList.Values.ToArray()[i];
                var editButtonCount = Main.OrderingOptionsVisible ? 1 : 0;

                if (Main.OrderingOptionsVisible)
                {
                    editButtonCount++;
                    if (Widgets.EditButton(EditButtonPosition.Before, FontAwesome.ArrowDown, id, true, i == ContentList.Count - 1))
                    {
                        Main.MoveTreeItem(i, true);
                    }
                    editButtonCount++;
                    if (Widgets.EditButton(EditButtonPosition.Before, FontAwesome.ArrowUp, id - 1, true, i == 0))
                    {
                        Main.MoveTreeItem(i, false);
                    }
                }
                editButtonCount++;
                if (Widgets.EditButton(EditButtonPosition.Before, FontAwesome.Edit, i, true))
                {
                    ContentDescriptor.Name = nplItem.Category;
                    ContentDescriptor.Category = nplItem.Category;
                    ContentDescriptor.Path = nplItem.Path;

                    ModalDescriptor.Set(MessageType.EditContent, "Set the name and the path for this content.");
                }

                ImGui.SetCursorPosX(ImGui.GetStyle().IndentSpacing * editButtonCount + ImGui.GetStyle().ItemSpacing.X);
                ImGui.PushStyleColor(ImGuiCol.Text, ImGui.GetStyle().Colors[(int)ImGuiCol.NavHighlight]);
                ImGui.PushItemWidth(ImGui.GetContentRegionAvail().X - 150 + ImGui.GetStyle().IndentSpacing * editButtonCount);
                if (ImGui.TreeNodeEx(nplItem.Category, ImGuiTreeNodeFlags.DefaultOpen | ImGuiTreeNodeFlags.SpanAllColumns))
                {
                    ImGui.PopStyleColor();

                    var path = nplItem.Path;
                    if (ImGui.InputText(" ", ref path, 9999, ImGuiInputTextFlags.EnterReturnsTrue))
                    {
                        nplItem.Path = path;

                        JsonObject["content"][nplItem.Category]["path"] = nplItem.Path;
                        Main.WriteContentNPL();
                    }
                    ImGui.SameLine();
                    if (ImGui.Checkbox("recursive", ref nplItem.Recursive))
                    {
                        JsonObject["content"][nplItem.Category]["recursive"] = nplItem.Recursive;
                        Main.WriteContentNPL();
                    }

                    var actionIndex = nplItem.GetActionIndex();
                    var actionNames = Enum.GetNames(typeof(BuildAction));
                    if (ImGui.Combo("action", ref actionIndex, actionNames, actionNames.Length))
                    {
                        var itemValue = actionNames[actionIndex].ToLowerInvariant();
                        nplItem.Action = (BuildAction)Enum.Parse(typeof(BuildAction), itemValue.ToString(), true);
                        JsonObject["content"][nplItem.Category]["action"] = itemValue;
                        Main.WriteContentNPL();
                    }

                    if (Widgets.ComboImporterDesciptor(nplItem, out var importer))
                    {
                        JsonObject["content"][nplItem.Category]["importer"] = importer.TypeName;
                        Main.WriteContentNPL();
                    }
                    if (Widgets.ComboProcessorDesciptor(nplItem, out var processor))
                    {
                        JsonObject["content"][nplItem.Category]["processor"] = processor.TypeName;
                        Main.WriteJsonProcessorParameters(nplItem);
                        Main.WriteContentNPL();
                    }

                    ImGui.PushItemWidth(ImGui.CalcItemWidth() - ImGui.GetStyle().IndentSpacing);
                    if (ImGui.TreeNodeEx("processorParam", ImGuiTreeNodeFlags.DefaultOpen | ImGuiTreeNodeFlags.Leaf | ImGuiTreeNodeFlags.SpanAllColumns))
                    {
                        if (ImGui.BeginTable("Parameter", 2, ImGuiTableFlags.NoClip))
                        {
                            var itemCount = 0;
                            foreach (var parameter in nplItem.Processor.Properties)
                            {
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

                                if (nplItem.Property(parameter.Name).Type == typeof(bool))
                                {
                                    Widgets.Checkbox(nplItem, "processorParam", parameter.Name);
                                }
                                else if (nplItem.Property(parameter.Name).Type == typeof(int))
                                {
                                    Widgets.InputInt(nplItem, "processorParam", parameter.Name);
                                }
                                else if (nplItem.Property(parameter.Name).Type == typeof(double))
                                {
                                    Widgets.InputDouble(nplItem, "processorParam", parameter.Name);
                                }
                                else if (nplItem.Property(parameter.Name).Type == typeof(float))
                                {
                                    Widgets.InputFloat(nplItem, "processorParam", parameter.Name);
                                }
                                else if (nplItem.Property(parameter.Name).Type == typeof(Color))
                                {
                                    Widgets.ColorEdit(nplItem, "processorParam", parameter.Name);
                                }
                                else if (nplItem.Property(parameter.Name).Type.IsEnum)
                                {
                                    Widgets.ComboContentItem(nplItem, "processorParam", parameter.Name);
                                }
                                else Widgets.TextInput(nplItem, "processorParam", parameter.Name);
                            }
                        }
                        ImGui.EndTable();
                        ImGui.TreePop();
                    }
                    ImGui.PopItemWidth();

                    Widgets.ListEditor("Dependencies", nplItem.Dependencies, out var itemAdded, out var itemRemoved, out var itemChanged);
                    {
                        if (itemAdded || itemChanged || itemRemoved)
                        {
                            var jsonNode = new JsonNode[nplItem.Dependencies.Count];
                            for (int x = 0; x < nplItem.Dependencies.Count; x++)
                            {
                                jsonNode[x] = nplItem.Dependencies[x];
                            }
                            JsonObject["content"][nplItem.Category]["dependencies"] = new JsonArray(jsonNode);
                            Main.WriteContentNPL();
                        }
                    }
                    ImGui.TreePop();
                }
                ImGui.PopStyleColor();
                ImGui.PopItemWidth();
            }
        }
    }
}
