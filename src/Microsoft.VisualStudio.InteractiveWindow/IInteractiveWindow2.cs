// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the License.txt file in the project root for more information.

using System;

namespace Microsoft.VisualStudio.InteractiveWindow 
{
    public interface IInteractiveWindow2 : IInteractiveWindow 
    {
        /// <summary>
        /// Adds <paramref name="input"/> to the history as if it has been executed.
        /// Method doesn't execute <paramref name="input"/> and doesn't affect current user input.
        /// </summary>
        /// <exception cref="InvalidOperationException">The interactive window has not been initialized or is resetting.</exception>
        void AddToHistory(string input);
    }
}
