// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition.Hosting;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.Utilities;
using Xunit;

namespace Microsoft.VisualStudio.InteractiveWindow.UnitTests
{
    public sealed class InteractiveWindowTestHost : IDisposable
    {
        internal readonly IInteractiveWindow Window;
        internal readonly CompositionContainer ExportProvider;
        internal readonly TestInteractiveEngine Evaluator;

        private static readonly Lazy<AggregateCatalog> s_lazyCatalog = new Lazy<AggregateCatalog>(() =>
        {
            var assemblies = new[] { typeof(TestInteractiveEngine).Assembly, typeof(InteractiveWindow).Assembly }.Concat(GetVisualStudioAssemblies());
            return new AggregateCatalog(assemblies.Select(a => new AssemblyCatalog(a)));
        });

        internal InteractiveWindowTestHost(Action<InteractiveWindow.State> stateChangedHandler = null)
        {
            try
            {
                ExportProvider = new CompositionContainer(
                    s_lazyCatalog.Value,
                    CompositionOptions.DisableSilentRejection | CompositionOptions.IsThreadSafe);

                var contentTypeRegistryService = ExportProvider.GetExport<IContentTypeRegistryService>().Value;
                Evaluator = new TestInteractiveEngine(contentTypeRegistryService);
                Window = ExportProvider.GetExport<IInteractiveWindowFactoryService>().Value.CreateWindow(Evaluator);
                ((InteractiveWindow)Window).StateChanged += stateChangedHandler;
                Window.InitializeAsync().Wait();
            }
            catch (ReflectionTypeLoadException e)
            {
                Assert.False(true, 
                    e.Message + 
                    "LoaderExceptions: " + 
                    Environment.NewLine + 
                    string.Join(Environment.NewLine, e.LoaderExceptions.Select(l => l.Message)));
            }
        }

        public static Assembly[] GetVisualStudioAssemblies()
            => new[]
            {
                Assembly.Load("Microsoft.VisualStudio.Platform.VSEditor"),

                // Must include this because several editor options are actually stored as exported information 
                // on this DLL.  Including most importantly, the tab size information.
                Assembly.Load("Microsoft.VisualStudio.Text.Logic"),

                // Include this DLL to get several more EditorOptions including WordWrapStyle.
                Assembly.Load("Microsoft.VisualStudio.Text.UI"),

                // Include this DLL to get more EditorOptions values.
                Assembly.Load("Microsoft.VisualStudio.Text.UI.Wpf"),

                // Include this DLL to satisfy ITextUndoHistoryRegistry
                Assembly.Load("BasicUndo"),

                Assembly.Load("Microsoft.VisualStudio.Language.StandardClassification"),
            };

        public void Dispose()
        {
            if (Window != null)
            {
                // close interactive host process:
                var engine = Window.Evaluator;
                if (engine != null)
                {
                    engine.Dispose();
                }

                // dispose buffer:
                Window.Dispose();
            }
        }
    }
}
