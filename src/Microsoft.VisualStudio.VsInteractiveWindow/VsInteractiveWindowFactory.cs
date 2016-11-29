// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

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
            return Create(new VsInteractiveWindowCreationParameters(providerId, instanceId, title, evaluator) 
            {
                CreationFlags = creationFlags
            });
        }

        public IVsInteractiveWindow Create(VsInteractiveWindowCreationParameters creationParameters) 
        {
            return new VsInteractiveWindow(_componentModel, creationParameters);
        }
    }
}
