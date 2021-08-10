// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the License.txt file in the project root for more information.

using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.InteractiveWindow.UnitTests
{
    public sealed class TestContentTypeDefinition
    {
        public const string ContentTypeName = "InteractiveWindowTest";

        [Export]
        [Name(ContentTypeName)]
        [BaseDefinition("code")]
        public static readonly ContentTypeDefinition Definition;
    }
}
