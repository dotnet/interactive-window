// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the License.txt file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.InteractiveWindow.Commands
{
    public static class InteractiveWindowCommandExtensions
    {
        /// <summary>
        /// Gets the IInteractiveWindowCommands instance for the current interactive window if one is defined.
        /// 
        /// Returns null if the interactive commands have not been created for this window.
        /// </summary>
        public static IInteractiveWindowCommands GetInteractiveCommands(this IInteractiveWindow window)
        {
            IInteractiveWindowCommands commands;
            if (window.Properties.TryGetProperty(typeof(IInteractiveWindowCommands), out commands))
            {
                return commands;
            }

            return null;
        }
    }
}
