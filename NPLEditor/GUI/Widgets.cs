using ImGuiNET;
using NPLEditor.Common;
using NPLEditor.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace NPLEditor.GUI
{
    public static class Widgets
    {
        public static void SimpleTooltip(
            string title,
            string text,
            float width = 450f,
            Vector4 titleColor = default,
            Vector4 textColor = default)
        {
            if (ImGui.IsItemHovered(ImGuiHoveredFlags.DelayNormal))
            {
                ImGui.BeginTooltip();
                ImGui.PushTextWrapPos(width);

                var cTitle = titleColor == default ? ImGui.GetStyle().Colors[(int)ImGuiCol.TextDisabled] : titleColor;
                ImGui.TextColored(cTitle, $"{title}: "); 
                
                ImGui.SameLine();

                var cText = textColor == default ? ImGui.GetStyle().Colors[(int)ImGuiCol.Text] : textColor;
                ImGui.TextColored(cText, text);

                ImGui.PopTextWrapPos();
                ImGui.EndTooltip();
            }
        }

        public static bool ColorEdit(ContentItem nplItem, string itemKey, string parameterKey)
        {
            var value = nplItem.Vector4Property(parameterKey);
            if (ImGui.ColorEdit4(parameterKey, ref value,
                ImGuiColorEditFlags.NoOptions | ImGuiColorEditFlags.NoPicker | ImGuiColorEditFlags.NoTooltip))
            {
                var xColor = value.ToXNA();
                var sColor = $"{xColor.R},{xColor.G},{xColor.B},{xColor.A}";

                nplItem.Property(parameterKey).Value = sColor;
                ContentBuilder.JsonObject["content"][nplItem.Category][itemKey][parameterKey] = sColor;
                return true;
            }
            return false;
        }

        public static bool Checkbox(ContentItem nplItem, string itemKey, string parameterKey)
        {
            var value = nplItem.BoolProperty(parameterKey);
            if (ImGui.Checkbox(parameterKey, ref value))
            {
                nplItem.Property(parameterKey).Value = value.ToString();
                ContentBuilder.JsonObject["content"][nplItem.Category][itemKey][parameterKey] = value.ToString();
                return true;
            }
            return false;
        }

        public static bool InputInt(ContentItem nplItem, string itemKey, string parameterKey)
        {
            var value = nplItem.IntProperty(parameterKey);
            if (ImGui.InputInt(parameterKey, ref value))
            {
                nplItem.Property(parameterKey).Value = value;
                ContentBuilder.JsonObject["content"][nplItem.Category][itemKey][parameterKey] = value.ToString();
                return true;
            }
            return false;
        }

        public static bool InputDouble(ContentItem nplItem, string itemKey, string parameterKey)
        {
            var value = nplItem.DoubleProperty(parameterKey);
            if (ImGui.InputDouble(parameterKey, ref value))
            {
                nplItem.Property(parameterKey).Value = value;
                ContentBuilder.JsonObject["content"][nplItem.Category][itemKey][parameterKey] = value.ToString();
                return true;
            }
            return false;
        }

        public static bool InputFloat(ContentItem nplItem, string itemKey, string parameterKey)
        {
            var value = nplItem.FloatProperty(parameterKey);
            if (ImGui.InputFloat(parameterKey, ref value))
            {
                nplItem.Property(parameterKey).Value = value;
                ContentBuilder.JsonObject["content"][nplItem.Category][itemKey][parameterKey] = value.ToString();
                return true;
            }
            return false;
        }

        public static bool ComboEnum(ref string property, string parameterKey, string[] names)
        {
            var selectedIndex = names.ToList().IndexOf(property.ToString());
            if (ImGui.Combo(parameterKey, ref selectedIndex, names, names.Length))
            {
                property = names[selectedIndex].ToString();
                return true;
            }
            return false;
        }

        public static bool Combo(ContentItem nplItem, string parameterKey, string[] names)
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

        public static bool ComboContentItem(ContentItem nplItem, string itemKey, string parameterKey)
        {
            var names = Enum.GetNames(nplItem.Property(parameterKey).Type);
            if (Combo(nplItem, parameterKey, names))
            {
                ContentBuilder.JsonObject["content"][nplItem.Category][itemKey][parameterKey] = nplItem.Property(parameterKey).Value.ToString();
                return true;
            }
            return false;
        }

        public static bool ComboImporterDesciptor(ContentItem nplItem, out ImporterTypeDescription importer)
        {
            var selectedIndex = nplItem.GetImporterIndex();
            var names = PipelineTypes.Importers.Select(x => x.DisplayName).ToArray();

            if (ImGui.Combo("importer", ref selectedIndex, names, names.Length))
            {
                nplItem.SelectedImporterIndex = selectedIndex;
                importer = PipelineTypes.Importers[nplItem.SelectedImporterIndex];
                nplItem.Importer = importer;
                return true;
            }
            importer = null;
            return false;
        }

        public static bool ComboProcessorDesciptor(ContentItem nplItem, out ProcessorTypeDescription processor)
        {
            var selectedIndex = nplItem.GetProcessorIndex();
            var names = PipelineTypes.Processors.Select(x => x.DisplayName).ToArray();

            if (ImGui.Combo("processor", ref selectedIndex, names, names.Length))
            {
                nplItem.SelectedProcessorIndex = selectedIndex;
                processor = PipelineTypes.Processors[nplItem.SelectedProcessorIndex];
                nplItem.Processor = processor;
                return true;
            }
            processor = null;
            return false;
        }

        public static bool TextInput(ContentItem nplItem, string itemKey, string parameterKey)
        {
            var paramValue = nplItem.Property(parameterKey).Value.ToString();
            if (ImGui.InputText(parameterKey, ref paramValue, 9999))
            {
                nplItem.Property(parameterKey).Value = paramValue;
                ContentBuilder.JsonObject["content"][nplItem.Category][itemKey][parameterKey] = paramValue;
                return true;
            }
            return false;
        }

        public static bool EditButton(EditButtonPosition position, string icon, int id, bool small, bool disabled = false)
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
        
        public static void ListEditor(
            string name,
            List<string> list,
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
                    list.Add("");
                }

                for (int i = 0; i < list.Count; i++)
                {
                    var data = list[i];
                    ImGui.PushItemWidth(ImGui.CalcItemWidth() - ImGui.GetStyle().IndentSpacing * 1.5f);
                    if (ImGui.InputText($"##{i}", ref data, 9999, ImGuiInputTextFlags.EnterReturnsTrue))
                    {
                        itemChanged = true;
                        list[i] = data;
                    }
                    ImGui.PopItemWidth();

                    if (EditButton(EditButtonPosition.After, FontAwesome.TrashAlt, i, false))
                    {
                        itemRemoved = true;
                        list.RemoveAt(i);
                    }
                }
                ImGui.TreePop();
            }
        }
    }
}
