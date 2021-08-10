// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the License.txt file in the project root for more information.

using System.Threading.Tasks;

namespace Microsoft.VisualStudio.InteractiveWindow
{
    /// <summary>
    /// The result of command execution.  
    /// </summary>
    public struct ExecutionResult
    {
        public static readonly ExecutionResult Success = new ExecutionResult(true);
        public static readonly ExecutionResult Failure = new ExecutionResult(false);
        public static readonly Task<ExecutionResult> Succeeded = Task.FromResult(Success);
        public static readonly Task<ExecutionResult> Failed = Task.FromResult(Failure);

        private readonly bool _isSuccessful;

        public ExecutionResult(bool isSuccessful)
        {
            _isSuccessful = isSuccessful;
        }

        public bool IsSuccessful
        {
            get
            {
                return _isSuccessful;
            }
        }
    }
}
