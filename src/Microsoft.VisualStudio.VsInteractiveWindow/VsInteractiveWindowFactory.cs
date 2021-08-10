// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the License.txt file in the project root for more information.

using System;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.OLE.Interop;

namespace Microsoft.VisualStudio.InteractiveWindow.Shell
{
    [Export(typeof(IVsInteractiveWindowFactory))]
    [Export(typeof(IVsInteractiveWindowFactory2))]
    internal sealed class VsInteractiveWindowFactory : IVsInteractiveWindowFactory2
    {
        private readonly IComponentModel _componentModel;

        [ImportingConstructor]
        internal VsInteractiveWindowFactory(SVsServiceProvider serviceProvider)
        {
            _componentModel = (IComponentModel)serviceProvider.GetService(typeof(SComponentModel));
        }

        public IVsInteractiveWindow Create(Guid providerId, int instanceId, string title, IInteractiveEvaluator evaluator, __VSCREATETOOLWIN creationFlags)
        {
            return Create(providerId, instanceId, title, evaluator, creationFlags, Guid.Empty, 0, null);
        }

        public IVsInteractiveWindow Create(
            Guid providerId, 
            int instanceId, 
            string title, 
            IInteractiveEvaluator evaluator,
            __VSCREATETOOLWIN creationFlags,
            Guid toolbarCommandSet,
            uint toolbarId, 
            IOleCommandTarget toolbarCommandTarget) 
        {
            return new VsInteractiveWindow(_componentModel, providerId, instanceId, title, evaluator, creationFlags, toolbarCommandSet, toolbarId, toolbarCommandTarget);
        }
    }
}
