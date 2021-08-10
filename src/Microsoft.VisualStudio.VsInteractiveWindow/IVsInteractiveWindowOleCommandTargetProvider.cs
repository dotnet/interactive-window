// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the License.txt file in the project root for more information.

using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.VisualStudio.InteractiveWindow.Shell
{
    /// <summary>
    /// The implementer is given a chance to attach a command filter that routes language services
    /// commands into the Interactive Window command filter chain.
    /// </summary>
    public interface IVsInteractiveWindowOleCommandTargetProvider
    {
        IOleCommandTarget GetCommandTarget(IWpfTextView textView, IOleCommandTarget nextTarget);
    }
}
