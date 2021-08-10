// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the License.txt file in the project root for more information.

namespace Microsoft.VisualStudio.InteractiveWindow
{
    public interface IInteractiveWindowOperations2 : IInteractiveWindowOperations
    {
        /// <summary>
        /// Copies the current selection to the clipboard.
        /// </summary>
        void Copy();

        /// <summary>
        /// Copies code from user inputs to clipboard. 
        /// If selection is empty, then copy from current line, otherwise copy from selected lines.
        /// </summary>
        void CopyCode();

        /// <summary>
        /// Delete Line; Delete all selected lines, or the current line if no selection.  
        /// </summary>
        void DeleteLine();

        /// <summary>
        /// Line Cut; Cut all selected lines, or the current line if no selection, to the clipboard.   
        /// </summary>           
        void CutLine();

        /// <summary>
        /// Handles character typed in by user. 
        /// </summary>
        void TypeChar(char typedChar);
    }
}
