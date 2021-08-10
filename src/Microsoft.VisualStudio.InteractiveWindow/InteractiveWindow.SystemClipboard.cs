// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the License.txt file in the project root for more information.

using System.Windows;

namespace Microsoft.VisualStudio.InteractiveWindow
{
    internal partial class InteractiveWindow
    {
        private sealed class SystemClipboard : InteractiveWindowClipboard
        {
            internal override bool ContainsData(string format) => Clipboard.ContainsData(format);

            internal override object GetData(string format) => Clipboard.GetData(format);

            internal override bool ContainsText() => Clipboard.ContainsText();

            internal override string GetText() => Clipboard.GetText();

            internal override void SetDataObject(object data, bool copy) => Clipboard.SetDataObject(data, copy);

            internal override IDataObject GetDataObject() => Clipboard.GetDataObject();
        }
    }
}
