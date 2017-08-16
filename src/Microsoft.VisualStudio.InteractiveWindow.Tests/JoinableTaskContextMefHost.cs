// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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