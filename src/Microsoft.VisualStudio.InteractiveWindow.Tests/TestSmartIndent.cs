// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the License.txt file in the project root for more information.

using System;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.InteractiveWindow.UnitTests
{
    internal class TestSmartIndent : ISmartIndent
    {
        private readonly int[] _lineToIndentMap;

        public TestSmartIndent(params int[] lineToIndentMap)
        {
            _lineToIndentMap = lineToIndentMap;
        }

        int? ISmartIndent.GetDesiredIndentation(ITextSnapshotLine line)
        {
            return _lineToIndentMap[line.LineNumber];
        }

        void IDisposable.Dispose()
        {
        }
    }

    internal class DummySmartIndent : ISmartIndent
    {
        public static readonly ISmartIndent Instance = new DummySmartIndent();

        private DummySmartIndent()
        {
        }

        int? ISmartIndent.GetDesiredIndentation(ITextSnapshotLine line)
        {
            return null;
        }

        void IDisposable.Dispose()
        {
        }
    }

    [Export(typeof(TestSmartIndentProvider))]
    [Export(typeof(ISmartIndentProvider))]
    [ContentType(InteractiveWindowEditorsFactoryService.ContentType)]
    internal class TestSmartIndentProvider : ISmartIndentProvider
    {
        public ISmartIndent SmartIndent;

        ISmartIndent ISmartIndentProvider.CreateSmartIndent(ITextView textView) => SmartIndent ?? DummySmartIndent.Instance;
    }
}
