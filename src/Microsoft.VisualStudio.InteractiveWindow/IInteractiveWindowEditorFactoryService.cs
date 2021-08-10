// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the License.txt file in the project root for more information.

using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Projection;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.InteractiveWindow
{
    /// <summary>
    /// Implements the service that creates text views and buffers for the interactive window.
    /// 
    /// There is a single implementation of this service for each MEF composition catalog.  The
    /// service understands how the editors and buffers need to be created and sets them up
    /// so that commands are properly routed to the editor window.
    /// 
    /// This service is imported by <see cref="IInteractiveWindowFactoryService"/> 
    /// to use in the creation of <see cref="IInteractiveWindow"/>s.
    /// </summary>
    public interface IInteractiveWindowEditorFactoryService
    {
        /// <summary>
        /// Creates a new text view for an interactive window.
        /// Must be called on UI thread.
        /// </summary>
        /// <param name="window">The interactive window the text view is being created for.</param>
        /// <param name="buffer">The projection buffer used for displaying the interactive window</param>
        /// <param name="roles">The requested text view roles.</param>
        IWpfTextView CreateTextView(IInteractiveWindow window, ITextBuffer buffer, ITextViewRoleSet roles);

        /// <summary>
        /// Creates a new input buffer for the interactive window.
        /// Must be called on UI thread.
        /// </summary>
        ITextBuffer CreateAndActivateBuffer(IInteractiveWindow window);
    }
}
