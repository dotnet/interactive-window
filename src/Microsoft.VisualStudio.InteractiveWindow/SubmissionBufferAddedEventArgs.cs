// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the License.txt file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Text;

namespace Microsoft.VisualStudio.InteractiveWindow
{
    public sealed class SubmissionBufferAddedEventArgs : EventArgs
    {
        private readonly ITextBuffer _newBuffer;

        public SubmissionBufferAddedEventArgs(ITextBuffer newBuffer)
        {
            if (newBuffer == null)
            {
                throw new ArgumentNullException(nameof(newBuffer));
            }

            _newBuffer = newBuffer;
        }

        public ITextBuffer NewBuffer
        {
            get
            {
                return _newBuffer;
            }
        }
    }
}
