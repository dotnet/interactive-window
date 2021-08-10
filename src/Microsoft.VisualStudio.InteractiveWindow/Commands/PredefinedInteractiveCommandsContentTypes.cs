// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the License.txt file in the project root for more information.

using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Utilities;

#pragma warning disable CS0649 // field is not assigned to

namespace Microsoft.VisualStudio.InteractiveWindow.Commands
{
    public static class PredefinedInteractiveCommandsContentTypes
    {
        public const string InteractiveCommandContentTypeName = "Interactive Command";

        [Export, Name(InteractiveCommandContentTypeName), BaseDefinition("code")]
        internal static readonly ContentTypeDefinition InteractiveCommandContentTypeDefinition;
    }
}
