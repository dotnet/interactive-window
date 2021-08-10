// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the License.txt file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.InteractiveWindow
{
    internal class ContentTypeMetadata
    {
        public IEnumerable<string> ContentTypes { get; }

        public ContentTypeMetadata(IDictionary<string, object> data)
        {
            this.ContentTypes = (IEnumerable<string>)data["ContentTypes"];
        }
    }

    internal static class ContentTypeMetadataHelpers
    {
        public static T OfContentType<T>(
            this IEnumerable<Lazy<T, ContentTypeMetadata>> exports,
            IContentType contentType,
            IContentTypeRegistryService contentTypeRegistry)
        {
            return (from export in exports
                    from exportedContentTypeName in export.Metadata.ContentTypes
                    let exportedContentType = contentTypeRegistry.GetContentType(exportedContentTypeName)
                    where exportedContentType.IsOfType(contentType.TypeName)
                    select export.Value).SingleOrDefault();
        }
    }
}
