// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the License.txt file in the project root for more information.

using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.InteractiveWindow.UnitTests
{
    internal class JoinableTaskContextMefHost
    {
        /// <summary>
        /// Gets the singleton instance of joinable task context for directly working with async tasks on managed code
        /// </summary>
        [Export]
        internal readonly JoinableTaskContext JoinableTaskContext = new JoinableTaskContext();
    }
}
