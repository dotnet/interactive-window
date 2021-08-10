// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the License.txt file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.Editor.Shared.Utilities
{
    // Based on CoreCLR's implementation of the TaskScheduler they return from TaskScheduler.FromCurrentSynchronizationContext
    internal class SynchronizationContextTaskScheduler : TaskScheduler
    {
        private readonly SendOrPostCallback _postCallback;
        private readonly SynchronizationContext _synchronizationContext;

        internal SynchronizationContextTaskScheduler(SynchronizationContext synchronizationContext)
        {
            if (synchronizationContext == null)
                throw new ArgumentNullException(nameof(synchronizationContext));

            _postCallback = new SendOrPostCallback(PostCallback);
            _synchronizationContext = synchronizationContext;
        }

        public override Int32 MaximumConcurrencyLevel
        {
            get { return 1; }
        }

        protected override void QueueTask(Task task)
        {
            _synchronizationContext.Post(_postCallback, task);
        }
        protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
        {
            if (SynchronizationContext.Current == _synchronizationContext)
            {
                return TryExecuteTask(task);
            }

            return false;
        }

        protected override IEnumerable<Task> GetScheduledTasks()
        {
            return null;
        }

        private void PostCallback(object obj)
        {
            TryExecuteTask((Task)obj);
        }
    }
}
