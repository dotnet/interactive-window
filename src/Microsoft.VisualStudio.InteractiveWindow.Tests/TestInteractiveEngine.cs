// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the License.txt file in the project root for more information.

using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.InteractiveWindow.UnitTests
{
    public sealed class TestInteractiveEngine : IInteractiveEvaluator
    {
        internal event EventHandler<string> OnExecute;

        public TestInteractiveEngine()
        {
        }

        public IInteractiveWindow CurrentWindow { get; set; }

        public void Dispose()
        {
        }

        public Task<ExecutionResult> InitializeAsync()
        {
            return Task.FromResult(ExecutionResult.Success);
        }

        public Task<ExecutionResult> ResetAsync(bool initialize = true)
        {
            return Task.FromResult(ExecutionResult.Success);
        }

        public bool CanExecuteCode(string text)
        {
            return true;
        }

        public Task<ExecutionResult> ExecuteCodeAsync(string text)
        {
            OnExecute?.Invoke(this, text);
            return Task.FromResult(ExecutionResult.Success);
        }

        public string FormatClipboard()
        {
            return null;
        }

        public void AbortExecution()
        {
        }

        public string GetConfiguration()
        {
            return "config";
        }

        public string GetPrompt()
        {
            return "> ";
        }
    }
}
