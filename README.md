# Welcome to NPL Editor!
[![NuGet](https://img.shields.io/badge/NuGet-Tool-blue.svg?style=flat-square&logo=NuGet&colorA=555555&colorB=D1A300)](https://www.nuget.org/packages/NPLEditor/) [![Visual Studio](https://img.shields.io/badge/Visual%20Studio-Extension-lightgrey.svg?style=flat-square&logo=visual-studio-code&colorB=af70f2)](https://marketplace.visualstudio.com/items?itemName=BlizzCrafter.NPLEditor)

A graphical editor for '.npl' files used together with 'Nopipeline.Task' to produce '.mgcb' files for MonoGame projects.

Inspired by the [MGCB Editor](https://docs.monogame.net/articles/content_pipeline/using_mgcb_editor.html).

# Setup NPL Editor

1. Open or create a [MonoGame](https://monogame.net/) project via Visual Studio.
2. Install the [Nopipeline.Task](https://www.nuget.org/packages/Nopipeline.Task).
3. Install the [NPLEditor](https://www.nuget.org/packages/NPLEditor/) as a local dotnet tool.
4. Install the [NPLEditor.VSExtension](https://marketplace.visualstudio.com/items?itemName=BlizzCrafter.NPLEditor) via Visual Studio.
5. Profit ???

Yes! It should be possible now to double click the **Content.npl** file inside your **Content** folder to open the **NPL Editor** and start adding content to it.

# Benefits

- No JSON-Formatting Erros Anymore
  - Just manage your content and **NPL Editor** takes care about the correct formatting of your **.npl** files.
- Automatic Pipeline Imports
  - Just add your content pipeline references and **NPL Editor** extracts importers and processors from it.
- No "Name-Guessing" Anymore
  - Just with importers and processors, you don't need to guess the correct names of parameters or anything else anymore; just select what you need inside the **NPL Editor**.
- Logging
  - Realtime logging events directly inside the **NPL Editor**.
 
# Impressions

![NPLEditor](https://raw.githubusercontent.com/BlizzCrafter/NPL-Editor/master/docs/npl_tool_00.png)
![NPLEditor](https://raw.githubusercontent.com/BlizzCrafter/NPL-Editor/master/docs/npl_tool_01.png)
![NPLEditor](https://raw.githubusercontent.com/BlizzCrafter/NPL-Editor/master/docs/npl_tool_02.png)

# Now Have Fun with NPL Editor!
