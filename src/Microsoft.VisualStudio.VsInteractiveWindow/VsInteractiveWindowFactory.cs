// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
            return Create(providerId, instanceId, title, evaluator, Guid.Empty, 0, null, creationFlags);
        }

        public IVsInteractiveWindow Create(
            Guid providerId, 
            int instanceId, 
            string title, 
            IInteractiveEvaluator evaluator,
            Guid toolbarCommandSet,
            uint toolbarId, 
            IOleCommandTarget toolbarCommandTarget, 
            __VSCREATETOOLWIN creationFlags) 
        {
            return new VsInteractiveWindow(_componentModel, providerId, instanceId, title, evaluator, toolbarCommandSet, toolbarId, toolbarCommandTarget, creationFlags);
        }
    }
}
