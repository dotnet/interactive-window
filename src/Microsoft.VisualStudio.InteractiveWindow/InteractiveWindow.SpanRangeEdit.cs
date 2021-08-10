// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the License.txt file in the project root for more information.


namespace Microsoft.VisualStudio.InteractiveWindow
{
    internal partial class InteractiveWindow
    {
        private struct SpanRangeEdit
        {
            public readonly int Start;
            public readonly int End;
            public readonly object[] Replacement;

            public SpanRangeEdit(int start, int count, object[] replacement)
            {
                Start = start;
                End = start + count;
                Replacement = replacement;
            }
        }
    }
}
