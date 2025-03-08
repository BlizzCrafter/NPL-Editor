using System;
using System.IO;

namespace NPLEditor.Data
{
    public static class NPLConfigReader
    {
        public static string contentRoot = "";

        public static string[] GetAllContentFiles()
        {
            Console.WriteLine("Reading Content Items.");
            Console.WriteLine();

            contentRoot = contentRoot.Replace("\\", "/");
            if (contentRoot.StartsWith("./"))
            {
                contentRoot = contentRoot.Substring(2, contentRoot.Length - 2);
            }
            Console.WriteLine("Root: " + contentRoot);
            Console.WriteLine();

            foreach (var item in Main.ContentList)
            {
                string path = item.Value.Path.ToString().Replace("\\", "/");
                if (path.Contains("../"))
                {
                    throw new Exception("'path' is not allowed to contain '../'! Use 'root' property to specify a different root instead.");
                }

                var appendRoot = false;
                if (contentRoot != "")
                {
                    if (path.StartsWith('$'))
                    {
                        // $ means that the path will not have the root appended to it.
                        path = path.TrimStart('$');
                        appendRoot = false;
                    }
                    else
                    {
                        // Appending root now so that we would work with full paths.
                        // We don't care if it's a link or not for now.
                        path = Path.Combine(contentRoot, path);
                        appendRoot = true;
                    }
                }

                Console.WriteLine("Reading content for: " + path);

                var fileName = Path.GetFileName(path);
                var filePath = Path.GetDirectoryName(path);
                var files = new string[] { };

                try
                {
                    var searchOpt = SearchOption.TopDirectoryOnly;
                    if (item.Value.Recursive)
                    {
                        searchOpt = SearchOption.AllDirectories;
                    }
                    else searchOpt = SearchOption.TopDirectoryOnly;

                    files = Directory.GetFiles(Path.Combine(contentRoot, filePath), fileName, searchOpt);
                }
                catch (Exception e)
                {
                    Console.WriteLine($"    Error reading files from {contentRoot}{filePath}: ");
                    Console.WriteLine("    " + e.Message);
                }
                return files;
            }

            Console.WriteLine();
            Console.WriteLine("Finished reading Content Items!");
            Console.WriteLine();

            return null;
        }
    }
}
