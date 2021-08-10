// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the License.txt file in the project root for more information.

using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Utilities;

#pragma warning disable CS0649 // field is not assigned to

namespace Microsoft.VisualStudio.InteractiveWindow
{
    internal static class InteractiveContentTypeDefinitions
    {
        [Export, Name(PredefinedInteractiveContentTypes.InteractiveContentTypeName), BaseDefinition("text"), BaseDefinition("projection")]
        internal static readonly ContentTypeDefinition InteractiveContentTypeDefinition;

        [Export, Name(PredefinedInteractiveContentTypes.InteractiveOutputContentTypeName), BaseDefinition("text")]
        internal static readonly ContentTypeDefinition InteractiveOutputContentTypeDefinition;
    }
}
