// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the License.txt file in the project root for more information.

using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Projection;

namespace Microsoft.VisualStudio.InteractiveWindow
{
    internal static class ProjectionBufferExtensions
    {
        internal static SnapshotSpan GetSourceSpan(this IProjectionSnapshot snapshot, int index)
        {
            return snapshot.GetSourceSpans(index, 1)[0];
        }
    }
}
