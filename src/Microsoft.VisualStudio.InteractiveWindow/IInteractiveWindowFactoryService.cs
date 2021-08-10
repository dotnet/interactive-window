// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the License.txt file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.InteractiveWindow
{
    /// <summary>
    /// Creates instances of the IInteractiveWindow.  
    /// </summary>
    public interface IInteractiveWindowFactoryService
    {
        /// <summary>
        /// Creates a new interactive window which runs against the provided interactive evaluator.
        /// </summary>
        IInteractiveWindow CreateWindow(IInteractiveEvaluator evaluator);
    }
}
