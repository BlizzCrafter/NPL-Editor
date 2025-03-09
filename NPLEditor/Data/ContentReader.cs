using System;
using System.Collections.Generic;
using System.IO;

namespace NPLEditor.Data
{
    public static class ContentReader
    {
        public static string contentRoot = "";

        public static void GetAllContentFiles(out List<string> copyFiles, out List<string> buildFiles)
        {
            copyFiles = new List<string>();
            buildFiles = new List<string>();

            contentRoot = contentRoot.Replace("\\", "/");
            if (contentRoot.StartsWith("./"))
            {
                contentRoot = contentRoot.Substring(2, contentRoot.Length - 2);
            }

            foreach (var item in Main.ContentList)
            {
                string path = item.Value.Path.ToString().Replace("\\", "/");
                if (path.Contains("../"))
                {
                    throw new Exception("'path' is not allowed to contain '../'! Use 'root' property to specify a different root instead.");
                }

                if (path.StartsWith('$'))
                {
                    // $ means that the path will not have the root appended to it.
                    path = path.TrimStart('$');
                }
                else if (contentRoot != "")
                {
                    // Appending root now so that we would work with full paths.
                    path = Path.Combine(contentRoot, path);
                }

                var fileName = Path.GetFileName(path);
                var filePath = Path.GetDirectoryName(path);
                if (string.IsNullOrEmpty(filePath))
                {
                    filePath = Directory.GetCurrentDirectory();
                }

                try
                {
                    var searchOpt = SearchOption.TopDirectoryOnly;
                    if (item.Value.Recursive)
                    {
                        searchOpt = SearchOption.AllDirectories;
                    }
                    else searchOpt = SearchOption.TopDirectoryOnly;

                    if (item.Value.Action == Enums.BuildAction.Copy)
                    {
                        copyFiles.AddRange(Directory.GetFiles(filePath, fileName, searchOpt));
                    }
                    else if(item.Value.Action == Enums.BuildAction.Build)
                    {
                        buildFiles.AddRange(Directory.GetFiles(filePath, fileName, searchOpt));
                    }
                }
                catch (Exception e)
                {
                    NPLLog.LogException(e);
                }
            }
        }
    }
}
