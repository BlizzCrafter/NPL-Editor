using System;
using System.Linq;
using System.Text.Json.Nodes;
using NPLTOOL.Parameter;

namespace NPLTOOL.Common
{
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
        public string _path;
        public readonly string Category;
        public bool Recursive;
        public string Action;
        public int SelectedImporterIndex;
        public int SelectedProcessorIndex;
        public string[] Watch;
        public string[] Parameters;
        public ProcessorTypeDescription Processor;

        private IParameterProcessor _parameterProcessor;

        public ContentItem(string category, string processorKey)
        {
            Category = category;

            if (!string.IsNullOrEmpty(processorKey))
            {
                Processor = PipelineTypes.Processors.ToList().Find(x => x.TypeName.Equals(processorKey));
                if (Processor != null && Processor.Properties != null && Processor.Properties.Any())
                {
                    if (processorKey == TextureProcessorParameter.ProcessorType) _parameterProcessor = new TextureProcessorParameter();
                }
            }
        }

        public T ParameterProcessor<T>() where T : class
        {
            return _parameterProcessor as T;
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
                    Action = value.ToString();
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

                            _parameterProcessor.SetValue(parameterKey, parameterValue.ToString());
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

        public string GetParameterString(string param, object value)
        {
            return $"{param}:{value}";
        }

        public int GetParameterIndex(string itemKey)
        {
            var list = Parameters.ToList();

            var parameter = list.Find(x => x.StartsWith(itemKey));
            return list.IndexOf(parameter);
        }
    }
}
