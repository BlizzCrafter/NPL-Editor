using Microsoft.Xna.Framework.Content.Pipeline;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;

namespace NPLTOOL.Common
{
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

        private static List<ImporterInfo> _importers;
        private static List<ProcessorInfo> _processors;

        public static string[] ImporterNames;
        public static string[] ProcessorNames;
        public static string[] ImporterTypes;
        public static string[] ProcessorTypes;

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
            ImporterNames = null;
            ImporterTypes = null;
            ProcessorNames = null;
            ProcessorTypes = null;
        }

        public static void Load(string[] references)
        {
            if (!Initialized) ResolveAssemblies(references);
        }

        private static void ResolveAssemblies(IEnumerable<string> assemblyPaths)
        {
            _importers = new List<ImporterInfo>();
            _processors = new List<ProcessorInfo>();
            ImporterNames = Array.Empty<string>();
            ImporterTypes = Array.Empty<string>();
            ProcessorNames = Array.Empty<string>();
            ProcessorTypes = Array.Empty<string>();

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
                catch
                {
                    //Logger.LogWarning(null, null, "Failed to load assembly '{0}': {1}", assemblyPath, e.Message);
                    // The assembly failed to load... nothing
                    // we can do but ignore it.
                    continue;
                }
            }
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
                    Array.Resize(ref ImporterNames, ImporterNames.Length + 1);
                    ImporterNames[ImporterNames.Length - 1] = _importers.Last().Name;
                    Array.Resize(ref ImporterTypes, ImporterTypes.Length + 1);
                    ImporterTypes[ImporterTypes.Length - 1] = _importers.Last().Type.Name;
                }
                else if (t.GetInterface(@"IContentProcessor") == typeof(IContentProcessor))
                {
                    var attributes = t.GetCustomAttributes(typeof(ContentProcessorAttribute), false);
                    if (attributes.Length != 0)
                    {
                        var processorAttribute = attributes[0] as ContentProcessorAttribute;
                        _processors.Add(new ProcessorInfo(processorAttribute, t));
                    }
                    Array.Resize(ref ProcessorNames, ProcessorNames.Length + 1);
                    ProcessorNames[ProcessorNames.Length - 1] = _processors.Last().Name;
                    Array.Resize(ref ProcessorTypes, ProcessorTypes.Length + 1);
                    ProcessorTypes[ProcessorTypes.Length - 1] = _processors.Last().Type.Name;
                }
            }
        }
    }
}
