// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the License.txt file in the project root for more information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.InteractiveWindow
{
    [Export(typeof(ISmartIndentProvider))]
    [ContentType(PredefinedInteractiveContentTypes.InteractiveContentTypeName)]
    internal class InteractiveSmartIndenterProvider : ISmartIndentProvider
    {
        private readonly ITextEditorFactoryService _editorFactory;
        private readonly IEnumerable<Lazy<ISmartIndentProvider, ContentTypeMetadata>> _indentProviders;

        [ImportingConstructor]
        public InteractiveSmartIndenterProvider(
            ITextEditorFactoryService editorFactory,
            [ImportMany] IEnumerable<Lazy<ISmartIndentProvider, ContentTypeMetadata>> indentProviders)
        {
            if (editorFactory == null)
            {
                throw new ArgumentNullException(nameof(editorFactory));
            }

            if (indentProviders == null)
            {
                throw new ArgumentNullException(nameof(indentProviders));
            }

            _editorFactory = editorFactory;
            _indentProviders = indentProviders;
        }

        public ISmartIndent CreateSmartIndent(ITextView view)
        {
            var window = view.TextBuffer.GetInteractiveWindow();
            if (window == null || window.CurrentLanguageBuffer == null)
            {
                return null;
            }

            return InteractiveSmartIndenter.Create(_indentProviders, window.CurrentLanguageBuffer.ContentType, view);
        }
    }
}
