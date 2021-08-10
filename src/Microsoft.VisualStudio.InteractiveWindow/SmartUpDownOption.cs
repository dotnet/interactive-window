// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the License.txt file in the project root for more information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.InteractiveWindow
{
    [Export(typeof(EditorOptionDefinition))]
    [Name(OptionName)]
    internal sealed class SmartUpDownOption : EditorOptionDefinition<bool>
    {
        internal const string OptionName = "InteractiveSmartUpDown";

        public override EditorOptionKey<bool> Key
        {
            get
            {
                return InteractiveWindowOptions.SmartUpDown;
            }
        }
    }
}
