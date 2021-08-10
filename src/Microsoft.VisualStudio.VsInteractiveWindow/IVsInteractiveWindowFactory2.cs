// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the License.txt file in the project root for more information.

using System;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.InteractiveWindow.Shell
{
    public interface IVsInteractiveWindowFactory2: IVsInteractiveWindowFactory 
    {
        IVsInteractiveWindow Create(
            Guid providerId, 
            int instanceId, 
            string title, 
            IInteractiveEvaluator evaluator,
            __VSCREATETOOLWIN creationFlags,
            Guid toolbarCommandSet,
            uint toolbarId,
            IOleCommandTarget toolbarCommandTarget);
    }
}
