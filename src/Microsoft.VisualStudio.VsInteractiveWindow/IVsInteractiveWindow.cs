// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the License.txt file in the project root for more information.

using System;
using Microsoft.VisualStudio.InteractiveWindow;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.InteractiveWindow.Shell
{
    /// <summary>
    /// Provides access to an interactive window being hosted inside of Visual Studio's process using the
    /// default tool window.
    /// 
    /// These tool windows are created using ProvideInteractiveWindowAttribute which provides the normal
    /// tool window registration options.  Instances of the tool window are then created using 
    /// IVsInteractiveWindowFactory when VS calls on your packages IVsToolWindowFactory.CreateToolWindow
    /// method.
    /// </summary>
    public interface IVsInteractiveWindow
    {
        /// <summary>
        /// Gets the interactive window instance.
        /// </summary>
        IInteractiveWindow InteractiveWindow { get; }

        /// <summary>
        /// Shows the window.
        /// </summary>
        void Show(bool focus);

        /// <summary>
        /// Configures the window for the specified VS language service guid language preferences.
        /// 
        /// Also installs a language appropriate command filter if one is exported via IVsInteractiveWindowOleCommandTargetProvider.
        /// </summary>
        void SetLanguage(Guid languageServiceGuid, IContentType contentType);
    }
}
