// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the License.txt file in the project root for more information.

using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;
using Roslyn.Test.Utilities;

namespace Microsoft.VisualStudio.InteractiveWindow.UnitTests
{
    [Export(typeof(IInteractiveWindowEditorFactoryService))]
    internal class InteractiveWindowEditorsFactoryService : IInteractiveWindowEditorFactoryService
    {
        public const string ContentType = "text";

        private readonly ITextBufferFactoryService _textBufferFactoryService;
        private readonly ITextEditorFactoryService _textEditorFactoryService;
        private readonly IContentTypeRegistryService _contentTypeRegistry;

        [ImportingConstructor]
        public InteractiveWindowEditorsFactoryService(ITextBufferFactoryService textBufferFactoryService, ITextEditorFactoryService textEditorFactoryService, IContentTypeRegistryService contentTypeRegistry)
        {
            _textBufferFactoryService = textBufferFactoryService;
            _textEditorFactoryService = textEditorFactoryService;
            _contentTypeRegistry = contentTypeRegistry;
        }

        IWpfTextView IInteractiveWindowEditorFactoryService.CreateTextView(IInteractiveWindow window, ITextBuffer buffer, ITextViewRoleSet roles)
        {
            WpfTestCase.RequireWpfFact($"Creates an IWpfTextView in {nameof(InteractiveWindowEditorsFactoryService)}");

            var textView = _textEditorFactoryService.CreateTextView(buffer, roles);
            return _textEditorFactoryService.CreateTextViewHost(textView, false).TextView;
        }

        ITextBuffer IInteractiveWindowEditorFactoryService.CreateAndActivateBuffer(IInteractiveWindow window)
        {
            IContentType contentType;
            if (!window.Properties.TryGetProperty(typeof(IContentType), out contentType))
            {
                contentType = _contentTypeRegistry.GetContentType(ContentType);
            }

            return _textBufferFactoryService.CreateTextBuffer(contentType);
        }
    }
}
