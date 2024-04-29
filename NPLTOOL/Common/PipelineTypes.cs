using Microsoft.Xna.Framework.Content.Pipeline;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection;

namespace NPLTOOL.Common
{
    public class ImporterTypeDescription
    {
        public string TypeName;
        public string DisplayName;
        public string DefaultProcessor;
        public IEnumerable<string> FileExtensions;
        public Type OutputType;

        public ImporterTypeDescription()
        {
            TypeName = "Invalid / Missing Importer";
        }

        public override string ToString()
        {
            return TypeName;
        }

        public override int GetHashCode()
        {
            return TypeName == null ? 0 : TypeName.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            var other = obj as ImporterTypeDescription;
            if (other == null)
                return false;

            if (string.IsNullOrEmpty(other.TypeName) != string.IsNullOrEmpty(TypeName))
                return false;

            return TypeName.Equals(other.TypeName);
        }
    }
    public class ProcessorTypeDescription
    {
        public class Property
        {
            public string Name;
            public string DisplayName;
            public Type Type;
            public object Value;
            public object DefaultValue;
            public bool Browsable;

            public bool ToBool()
            {
                return Value.ToString().ToBool();
            }

            public int ToInt()
            {
                return Value.ToString().ToInt();
            }

            public double ToDouble()
            {
                return Value.ToString().ToInt();
            }

            public float ToFloat()
            {
                return Value.ToString().ToFloat();
            }

            public Vector4 ToVector4()
            {
                return Value.ToString().ToVector4();
            }

            public override string ToString()
            {
                return Name;
            }
        }

        public class ProcessorPropertyCollection : IEnumerable<Property>
        {
            private readonly Property[] _properties;

            public ProcessorPropertyCollection(IEnumerable<Property> properties)
            {
                _properties = properties.ToArray();
            }

            public Property this[int index]
            {
                get
                {
                    return _properties[index];
                }
                set
                {
                    _properties[index] = value;
                }
            }

            public Property this[string name]
            {
                get
                {
                    foreach (var p in _properties)
                    {
                        if (p.Name.Equals(name))
                            return p;
                    }

                    throw new IndexOutOfRangeException();
                }

                set
                {
                    for (var i = 0; i < _properties.Length; i++)
                    {
                        var p = _properties[i];
                        if (p.Name.Equals(name))
                        {
                            _properties[i] = value;
                            return;
                        }

                    }

                    throw new IndexOutOfRangeException();
                }
            }

            public bool Contains(string name)
            {
                return _properties.Any(e => e.Name == name);
            }

            public IEnumerator<Property> GetEnumerator()
            {
                return _properties.AsEnumerable().GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return _properties.GetEnumerator();
            }
        }

        public string TypeName;
        public string DisplayName;
        public ProcessorPropertyCollection Properties;
        public Type InputType;

        public override string ToString()
        {
            return TypeName;
        }
    }

    public class PipelineTypes
    {
        [DebuggerDisplay("ImporterInfo: {Type.Name}")]
        private struct ImporterInfo
        {
            public ContentImporterAttribute Attribute;
            public Type Type;
            public string Name;

            public ImporterInfo(ContentImporterAttribute attribute, Type t)
            {
                Name = attribute.DisplayName ?? t.Name;
                Attribute = attribute;
                Type = t;
            }
        }

        [DebuggerDisplay("ProcessorInfo: {Type.Name}")]
        private struct ProcessorInfo
        {
            public ContentProcessorAttribute Attribute;
            public Type Type;
            public string Name;

            public ProcessorInfo(ContentProcessorAttribute attribute, Type t)
            {
                Name = attribute.DisplayName ?? t.Name;
                Attribute = attribute;
                Type = t;
            }
        }

        public static ImporterTypeDescription[] Importers { get; private set; }
        public static ProcessorTypeDescription[] Processors { get; private set; }

        private static List<ImporterInfo> _importers;
        private static List<ProcessorInfo> _processors;

        public static void GetTypeDescriptions(
            string fileExtension,
            out ImporterTypeDescription outImporter,
            out ProcessorTypeDescription outProcessor)
        {
            outImporter = null;
            outProcessor = null;

            foreach (var importer in Importers)
            {
                if (importer.FileExtensions.Any(extension => extension.Equals(fileExtension)))
                {
                    outImporter = importer;
                    outProcessor = Processors.ToList().Find(x => x.TypeName.Equals(importer.DefaultProcessor));
                    break;
                }
            }
        }

        public static int GetImporterIndex(string value)
        {
            var info = _importers.Find(x => x.Type.Name.Equals(value));
            var index = _importers.ToList().IndexOf(info);
            return index;
        }

        public static int GetProcessorIndex(string value)
        {
            var info = _processors.Find(x => x.Name.Equals(value) || x.Type.Name.Equals(value));
            var index = _processors.ToList().IndexOf(info);
            return index;
        }

        public static bool Initialized
        {
            get
            {
                if (_importers == null || _processors == null)
                {
                    return false;
                }
                else return true;
            }
        }

        public static void Reset()
        {
            _importers = null;
            _processors = null;
        }

        public static void Load(string[] references)
        {
            if (!Initialized) ResolveAssemblies(references);
        }

        private static void ResolveAssemblies(IEnumerable<string> assemblyPaths)
        {
            _importers = new List<ImporterInfo>();
            _processors = new List<ProcessorInfo>();

            var appPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            foreach (var file in Directory.GetFiles(appPath, "*.dll"))
            {
                var assemblyName = Path.GetFileNameWithoutExtension(file);
                var assembly = Assembly.Load(assemblyName);
                try
                {
                    if (!assembly.ToString().Contains("MonoGame"))
                        continue;

                    var types = assembly.GetTypes();
                    ProcessTypes(types);
                }
                catch { }
            }

            foreach (var path in assemblyPaths)
            {
                try
                {
                    var a = Assembly.LoadFrom(path);
                    var types = a.GetTypes();
                    ProcessTypes(types);
                }
                catch (Exception e)
                {
                    //TODO: Use Serilog
                    Debug.WriteLine("Failed to load assembly '{0}': {1}", Path.GetFileName(path), e.Message);
                    continue;
                }
            }

            var importerDescriptions = new ImporterTypeDescription[_importers.Count];
            var cur = 0;
            foreach (var item in _importers)
            {
                // Find the abstract base class ContentImporter<T>.
                var baseType = item.Type.BaseType;
                while (!baseType.IsAbstract)
                    baseType = baseType.BaseType;

                var outputType = baseType.GetGenericArguments()[0];
                var name = item.Attribute.DisplayName;
                if (string.IsNullOrEmpty(name))
                    name = item.GetType().Name;
                var desc = new ImporterTypeDescription()
                {
                    TypeName = item.Type.Name,
                    DisplayName = name,
                    DefaultProcessor = item.Attribute.DefaultProcessor,
                    FileExtensions = item.Attribute.FileExtensions,
                    OutputType = outputType,
                };
                importerDescriptions[cur] = desc;
                cur++;
            }

            Importers = importerDescriptions;


            var processorDescriptions = new ProcessorTypeDescription[_processors.Count];

            const BindingFlags bindings = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
            
            cur = 0;
            foreach (var item in _processors)
            {
                var obj = Activator.CreateInstance(item.Type);
                var typeProperties = item.Type.GetProperties(bindings);
                var properties = new List<ProcessorTypeDescription.Property>();
                foreach (var i in typeProperties)
                {
                    var attrs = i.GetCustomAttributes(true);
                    var name = i.Name;
                    var browsable = true;
                    var defvalue = i.GetValue(obj, null);
                    if (defvalue is Microsoft.Xna.Framework.Color color)
                    {
                        defvalue = color.Parsable();
                    }

                    foreach (var a in attrs)
                    {
                        if (a is BrowsableAttribute)
                            browsable = (a as BrowsableAttribute).Browsable;
                        else if (a is DisplayNameAttribute)
                            name = (a as DisplayNameAttribute).DisplayName;
                    }

                    var p = new ProcessorTypeDescription.Property()
                    {
                        Name = i.Name,
                        DisplayName = name,
                        Type = i.PropertyType,
                        Value = defvalue,
                        DefaultValue = defvalue,
                        Browsable = browsable
                    };
                    properties.Add(p);
                }

                var inputType = (obj as IContentProcessor).InputType;
                var desc = new ProcessorTypeDescription()
                {
                    TypeName = item.Type.Name,
                    DisplayName = item.Attribute.DisplayName,
                    Properties = new ProcessorTypeDescription.ProcessorPropertyCollection(properties),
                    InputType = inputType,
                };
                if (string.IsNullOrEmpty(desc.DisplayName))
                    desc.DisplayName = desc.TypeName;

                processorDescriptions[cur] = desc;
                cur++;
            }

            Processors = processorDescriptions;
        }

        private static void ProcessTypes(IEnumerable<Type> types)
        {
            foreach (var t in types)
            {
                if (t.IsAbstract)
                    continue;

                if (t.GetInterface(@"IContentImporter") == typeof(IContentImporter))
                {
                    var attributes = t.GetCustomAttributes(typeof(ContentImporterAttribute), false);
                    if (attributes.Length != 0)
                    {
                        var importerAttribute = attributes[0] as ContentImporterAttribute;
                        _importers.Add(new ImporterInfo(importerAttribute, t));
                    }
                    else
                    {
                        // If no attribute specify default one
                        var importerAttribute = new ContentImporterAttribute(".*");
                        importerAttribute.DefaultProcessor = "";
                        importerAttribute.DisplayName = t.Name;
                        _importers.Add(new ImporterInfo(importerAttribute, t));
                    }
                }
                else if (t.GetInterface(@"IContentProcessor") == typeof(IContentProcessor))
                {
                    var attributes = t.GetCustomAttributes(typeof(ContentProcessorAttribute), false);
                    if (attributes.Length != 0)
                    {
                        var processorAttribute = attributes[0] as ContentProcessorAttribute;
                        _processors.Add(new ProcessorInfo(processorAttribute, t));
                    }
                }
            }
        }
    }
}
