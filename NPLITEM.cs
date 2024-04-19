using System;
using System.Linq;
using System.Text.Json.Nodes;
using NPLTOOL.Parameter;

namespace NPLTOOL
{
    public class NPLITEM
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
        public string Key;
        public bool Recursive = false;
        public string Action = "";
        public string Importer = "";
        public string Processor = "";
        public string[] Watch;
        public string[] Parameters;
        public ProcessorParameter ProcessorParameters;

        public void Add(string param, object value)
        {
            switch (param)
            {
                case "path":
                    Path = value.ToString();
                    break;
                case "recursive":
                    Recursive = (string.Compare(value.ToString(), "true", true) == 0);
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
                        ProcessorParameters ??= new ProcessorParameter();

                        var itemArray = ((JsonObject)value).ToArray();

                        for (int i = 0; i < itemArray.Length; i++)
                        {
                            var parameterKey = itemArray[i].Key; //e.g. ColorKeyColor
                            var parameterValue = itemArray[i].Value; //e.g. 255,0,255,255
                                                        
                            ProcessorParameters.SetParameter(Enum.Parse<ParameterKey>(parameterKey), parameterValue.ToString());
                        }
                    }
                    break;
                default:
                    {
                        if (Parameters == null) Parameters = new string[1];
                        else Array.Resize(ref Parameters, Parameters.Length + 1);

                        Parameters[Parameters.Length - 1] = GetParameterString(param, value);
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
