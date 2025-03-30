# Welcome to NPL Editor!
[![NPLEditor](https://img.shields.io/nuget/v/NPLEditor?style=for-the-badge&logo=nuget&logoSize=auto&label=NPLEditor&labelColor=0956CC&color=707070)](https://www.nuget.org/packages/NPLEditor/) [![NPLEditor.Task](https://img.shields.io/nuget/v/NPLEditor.Task?style=for-the-badge&logo=nuget&logoSize=auto&label=NPLEditor.Task&labelColor=0956CC&color=707070)](https://www.nuget.org/packages/NPLEditor.Task/) [![Visual Studio](https://img.shields.io/badge/Visual%20Studio-Extension-blue.svg?style=for-the-badge&logo=visual-studio-code&labelColor=8962CC&logoSize=auto&colorA=262626&colorB=707070)](https://marketplace.visualstudio.com/items?itemName=BlizzCrafter.NPLEditor) 

NPL Editor is a modern, powerful, and completely standalone tool that revolutionizes your MonoGame content pipeline workflow. Building upon the robust foundations of **[MGCB Editor](https://docs.monogame.net/articles/getting_started/tools/mgcb_editor.html)** and **[NoPipeline](https://github.com/Martenfur/Nopipeline)**, it delivers an enhanced, user-friendly experience designed for speed, flexibility, and reliability.

# Setup NPL Editor

1. Open or create a [MonoGame](https://monogame.net/) project via Visual Studio.
3. Install the [NPLEditor](https://www.nuget.org/packages/NPLEditor/) as a **GLOBAL** dotnet tool.
2. Install the [NPLEditor.Task](https://www.nuget.org/packages/NPLEditor.Task/).
4. Install the [NPLEditor.VSExtension](https://marketplace.visualstudio.com/items?itemName=BlizzCrafter.NPLEditor) via Visual Studio.
5. Profit ???

Yes! It should be possible now to double click the **Content.npl** file inside your **Content** folder to open the **NPL Editor**. 

Everything you change inside this editor will modify the corresponding **Content.npl** file now 🥳.

# Benefits of NPL Editor

## 🔄 Seamless Integration
- Completely free from NoPipeline and MonoGame.ContentBuilder.Task dependencies, NPL Editor runs 100% on its own!

## ⚡ Performance & Efficiency
- **Incremental Build Mode:** Automatically rebuild only the changed content within multiple `.npl` content files simultaneously!

## 🛠️ Advanced Build & Logging
- **Dynamic Pipeline Imports:** Automatically detects and imports pipeline references of MonoGame and your own custom ones!
- **Enhanced Logging:** Real-time, organized logging with adjustable verbosity keeps you informed at every step!
- **Smart Build Process:** Powered by [MonoGame.RuntimeBuilder](https://www.nuget.org/packages/MonoGame.RuntimeBuilder), it tracks file dependencies and supports dynamic runtime modifications to the build process as well as detecting multiple importers with the same file extensions!

## 📝 Error-Free Editing & Automated Resource Management
- **Seamless Content Editing:** Eliminate JSON formatting errors – NPL Editor handles your `.npl` files flawlessly!
- **Intelligent Dependency Tracking:** Modify one content item and see all dependent assets automatically rebuilt!

## 📦 Flexibility & Future-Proofing
- **Backward Compatibility:** Supports legacy NPL keywords (like "watch") from NoPipeline for a smooth transition!
- **Debug-Friendly Task Integration:** Use the new NPLEditor.Task nuget to build content on the fly during debugging without launching the full GUI!
- **Advanced Parameter Parsing:** Control log levels and other settings easily via additional launch parameters.
- **Extended Content Management:** Enjoy enhanced content handling that supports environment variables and wildcards. For example, you can add content as follows:

>     "%PROGRAMFILES%\YourLibDir\Library.dll"
>     "..\RelativePath\RelativeLibrary.dll"
>     "D:\OtherPath\**\*.png"

By combining robust performance with a flexible and intuitive design, **NPL Editor** is your go-to tool for managing MonoGame content effortlessly.
 
# Impressions

![NPLEditor](https://raw.githubusercontent.com/BlizzCrafter/NPL-Editor/master/docs/npl_tool_00.png)
![NPLEditor](https://raw.githubusercontent.com/BlizzCrafter/NPL-Editor/master/docs/npl_tool_01.png)
![NPLEditor](https://raw.githubusercontent.com/BlizzCrafter/NPL-Editor/master/docs/npl_tool_02.png)
![NPLEditor](https://raw.githubusercontent.com/BlizzCrafter/NPL-Editor/master/docs/npl_tool_03.png)

# Now Have Fun with NPL Editor!
