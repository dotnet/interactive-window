// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the License.txt file in the project root for more information.

using System.Windows;

namespace Microsoft.VisualStudio.InteractiveWindow
{
    internal abstract class InteractiveWindowClipboard
    {
        internal abstract bool ContainsData(string format);

        internal abstract object GetData(string format);

        internal abstract bool ContainsText();

        internal abstract string GetText();

        internal abstract void SetDataObject(object data, bool copy);

        internal abstract IDataObject GetDataObject();
    }
}
