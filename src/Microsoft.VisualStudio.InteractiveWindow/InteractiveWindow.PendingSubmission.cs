// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the License.txt file in the project root for more information.

using System.Threading.Tasks;

namespace Microsoft.VisualStudio.InteractiveWindow
{
    internal partial class InteractiveWindow
    {
        private class PendingSubmission
        {
            public readonly string Input;

            /// <remarks>
            /// Set only on the last submission in each batch (to notify the caller).
            /// </remarks>
            public readonly TaskCompletionSource<object> Completion;

            public Task Task;

            public PendingSubmission(string input, TaskCompletionSource<object> completion)
            {
                Input = input;
                Completion = completion;
            }
        }
    }
}
