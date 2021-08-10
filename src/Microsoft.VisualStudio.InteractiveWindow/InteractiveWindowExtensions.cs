// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the License.txt file in the project root for more information.

using Microsoft.VisualStudio.Text;

namespace Microsoft.VisualStudio.InteractiveWindow
{
    public static class InteractiveWindowExtensions
    {
        /// <summary>
        /// Gets the interactive window associated with the text buffer if the text
        /// buffer is being hosted in the interactive window.
        /// 
        /// Returns null if the text buffer is not hosted in the interactive window.
        /// </summary>
        public static IInteractiveWindow GetInteractiveWindow(this ITextBuffer buffer)
        {
            return InteractiveWindow.FromBuffer(buffer);
        }
    }
}
