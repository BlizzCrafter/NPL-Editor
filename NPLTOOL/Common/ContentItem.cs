using System;
using System.Linq;
using System.Numerics;
using System.Text.Json.Nodes;
using static NPLTOOL.Common.ProcessorTypeDescription;

namespace NPLTOOL.Common
{
    public enum BuildAction
    {
        Build,
        Copy,
    }

    public class ContentItem
    {
        public string Path
        {
            get => _path;
            set
            {
                _path = value.Replace("\\", "/");
                if (_path.StartsWith("/")) // MGCB will melt if the path will start with a slash.
                {
                    _path = Path.Substring(1);
                }
            }
        }
        private string _path;
        public readonly string Category;
        public bool Recursive;
        public int SelectedImporterIndex;
        public int SelectedProcessorIndex;
        public string[] Watch;
        public string[] Parameters;
        public BuildAction Action;
        public ImporterTypeDescription Importer;
        public ProcessorTypeDescription Processor;

        public ContentItem(string category, string importerKey, string processorKey)
        {
            Category = category;

            if (!string.IsNullOrEmpty(importerKey))
            {
                Importer = PipelineTypes.Importers?.ToList().Find(x => x.TypeName.Equals(importerKey));
            }

            if (!string.IsNullOrEmpty(processorKey))
            {
                Processor = PipelineTypes.Processors?.ToList().Find(x => x.TypeName.Equals(processorKey));
            }
        }

        public void SetParameter(string param, object value)
        {
            switch (param)
            {
                case "path":
                    Path = value.ToString();
                    break;
                case "recursive":
                    Recursive = string.Compare(value.ToString(), "true", true) == 0;
                    break;
                case "action":
                    Action = (BuildAction)Enum.Parse(typeof(BuildAction), value.ToString(), true);
                    break;
                case "watch":
                    {
                        var itemArray = (JsonArray)value;
                        if (Watch == null) Watch = new string[itemArray.Count];
                        for (int i = 0; i < itemArray.Count; i++)
                        {
                            Watch[i] = itemArray[i].ToString();
                        }
                    }
                    break;
                case "processorParam":
                    {
                        var itemArray = ((JsonObject)value).ToArray();

                        for (int i = 0; i < itemArray.Length; i++)
                        {
                            var parameterKey = itemArray[i].Key; //e.g. ColorKeyColor
                            var parameterValue = itemArray[i].Value; //e.g. "255,0,255,255"

                            Property(parameterKey).Value = parameterValue.ToString();
                        }
                    }
                    break;
                default:
                    {
                        if (Parameters == null) Parameters = new string[1];
                        else Array.Resize(ref Parameters, Parameters.Length + 1);

                        Parameters[^1] = GetParameterString(param, value);
                    }
                    break;
            }
        }

        public Property Property(string parameterKey)
        {
            return Processor.Properties[parameterKey];
        }

        public bool BoolProperty(string parameterKey)
        {
            return Processor.Properties[parameterKey].ToBool();        
        }

        public int IntProperty(string parameterKey)
        {
            return Processor.Properties[parameterKey].ToInt();
        }

        public double DoubleProperty(string parameterKey)
        {
            return Processor.Properties[parameterKey].ToDouble();
        }

        public float FloatProperty(string parameterKey)
        {
            return Processor.Properties[parameterKey].ToFloat();
        }

        public Vector4 Vector4Property(string parameterKey)
        {
            return Processor.Properties[parameterKey].ToVector4();
        }

        public string GetParameterString(string param, object value)
        {
            return $"{param}:{value}";
        }

        public int GetActionIndex()
        {
            var actionList = Enum.GetNames(typeof(BuildAction)).ToList();
            return actionList.IndexOf(Action.ToString());
        }

        public int GetParameterIndex(string itemKey)
        {
            var list = Parameters.ToList();

            var parameter = list.Find(x => x.StartsWith(itemKey));
            return list.IndexOf(parameter);
        }

        public int GetImporterIndex()
        {
            if (Importer != null)
            {
                var index = PipelineTypes.GetImporterIndex(Importer.TypeName);
                return SelectedImporterIndex = index;
            }
            return -1;
        }

        public int GetProcessorIndex()
        {
            if (Processor != null)
            {
                var index = PipelineTypes.GetProcessorIndex(Processor.TypeName);
                return SelectedProcessorIndex = index;
            }
            return -1;
        }
    }
}
