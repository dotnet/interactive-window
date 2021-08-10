﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the License.txt file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.InteractiveWindow.Commands
{
    /// <summary>
    /// Creates an <see cref="IInteractiveWindowCommands"/> which handles updating the context type for the command type,
    /// classification of commands, and execution of commands.
    /// </summary>
    /// <remarks>
    /// Engines need to use this interface to respond to checks if code can be executed and to
    /// execute text when in a command mode.
    /// 
    /// The commands that are available for this interactive window are provided at creation time
    /// along with the prefix which commands should be prefaced with.
    /// </remarks>
    public interface IInteractiveWindowCommandsFactory
    {
        /// <summary>
        /// Creates the IInteractiveCommands instance.
        /// </summary>
        IInteractiveWindowCommands CreateInteractiveCommands(IInteractiveWindow window, string prefix, IEnumerable<IInteractiveWindowCommand> commands);
    }
}
