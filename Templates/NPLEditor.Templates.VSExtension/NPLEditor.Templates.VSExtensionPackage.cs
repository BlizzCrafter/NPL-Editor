global using Community.VisualStudio.Toolkit;
global using Microsoft.VisualStudio.Shell;
global using System;
global using Task = System.Threading.Tasks.Task;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio;
using System.Runtime.InteropServices;
using System.Threading;
using System.Diagnostics;

namespace NPLEditor.Templates.VSExtension
{
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [InstalledProductRegistration(Vsix.Name, Vsix.Description, Vsix.Version)]
    [Guid(PackageGuids.NPLEditorTemplatesVSExtensionString)]
    [ProvideEditorExtension(typeof(EditorFactory), ".npl", int.MaxValue, DefaultName = "NPL Editor")]
    public sealed class NPLEditorTemplatesVSExtensionPackage : ToolkitPackage
    {
        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            RegisterEditorFactory(new EditorFactory());

            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
        }
    }

    [Guid(PackageGuids.NPLEditorFactoryString)]
    internal class EditorFactory : IVsEditorFactory, IDisposable
    {
        public int CreateEditorInstance(uint grfCreateDoc, string pszMkDocument, string pszPhysicalView, IVsHierarchy pvHier, uint itemid, IntPtr punkDocDataExisting, out IntPtr ppunkDocView, out IntPtr ppunkDocData, out string pbstrEditorCaption, out Guid pguidCmdUI, out int pgrfCDW)
        {
            ppunkDocView = IntPtr.Zero;
            ppunkDocData = IntPtr.Zero;
            pguidCmdUI = PackageGuids.NPLEditorFactory;
            pgrfCDW = 0;
            pbstrEditorCaption = null;

            var process = new Process()
            {
                StartInfo = new ProcessStartInfo("dotnet", $"npl-editor {pszMkDocument}")
                {
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    RedirectStandardOutput = true,
                }                
            };
            process.Start();

            return VSConstants.S_OK;
        }

        public int SetSite(Microsoft.VisualStudio.OLE.Interop.IServiceProvider psp)
        {
            return VSConstants.S_OK;
        }

        public int Close()
        {
            return VSConstants.S_OK;
        }

        public int MapLogicalView(ref Guid rguidLogicalView, out string pbstrPhysicalView)
        {
            pbstrPhysicalView = null;

            return VSConstants.S_OK;
        }

        public void Dispose()
        {
        }
    }
}