// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the License.txt file in the project root for more information.

using System;
using System.Windows;

namespace Microsoft.VisualStudio.InteractiveWindow.UnitTests
{
    internal sealed class TestClipboard : InteractiveWindowClipboard
    {
        private DataObject _data = null;

        internal void Clear() => _data = null;

        internal override bool ContainsData(string format) => _data?.GetData(format) != null;

        internal override object GetData(string format) => _data?.GetData(format);

        internal override bool ContainsText() => _data != null ? _data.ContainsText() : false;

        internal override string GetText() => _data?.GetText();

        internal override void SetDataObject(object data, bool copy) => _data = (DataObject)data;

        internal override IDataObject GetDataObject() => _data;
    }
}
