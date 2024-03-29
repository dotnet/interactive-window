﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the License.txt file in the project root for more information.

using System;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Formatting;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Text.Projection;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.InteractiveWindow
{
    [Export(typeof(IInteractiveWindowFactoryService))]
    internal class InteractiveWindowProvider : IInteractiveWindowFactoryService
    {
        private readonly IContentTypeRegistryService _contentTypeRegistry;
        private readonly ITextBufferFactoryService _bufferFactory;
        private readonly IProjectionBufferFactoryService _projectionBufferFactory;
        private readonly IEditorOperationsFactoryService _editorOperationsFactory;
        private readonly ITextBufferUndoManagerProvider _textBufferUndoManagerProvider;
        private readonly ITextEditorFactoryService _editorFactory;
        private readonly IRtfBuilderService _rtfBuilderService;
        private readonly IIntellisenseSessionStackMapService _intellisenseSessionStackMap;
        private readonly ISmartIndentationService _smartIndenterService;
        private readonly IInteractiveWindowEditorFactoryService _windowFactoryService;
        private readonly IUIThreadOperationExecutor _uiThreadOperationExecutor;

        [ImportingConstructor]
        public InteractiveWindowProvider(
            IContentTypeRegistryService contentTypeRegistry,
            ITextBufferFactoryService bufferFactory,
            IProjectionBufferFactoryService projectionBufferFactory,
            IEditorOperationsFactoryService editorOperationsFactory,
            ITextBufferUndoManagerProvider textBufferUndoManagerProvider,
            ITextEditorFactoryService editorFactory,
            IRtfBuilderService rtfBuilderService,
            IIntellisenseSessionStackMapService intellisenseSessionStackMap,
            ISmartIndentationService smartIndenterService,
            IInteractiveWindowEditorFactoryService windowFactoryService,
            IUIThreadOperationExecutor uiThreadOperationExecutor)
        {
            _contentTypeRegistry = contentTypeRegistry;
            _bufferFactory = bufferFactory;
            _projectionBufferFactory = projectionBufferFactory;
            _editorOperationsFactory = editorOperationsFactory;
            _textBufferUndoManagerProvider = textBufferUndoManagerProvider;
            _editorFactory = editorFactory;
            _rtfBuilderService = rtfBuilderService;
            _intellisenseSessionStackMap = intellisenseSessionStackMap;
            _smartIndenterService = smartIndenterService;
            _windowFactoryService = windowFactoryService;
            _uiThreadOperationExecutor = uiThreadOperationExecutor;
        }

        public IInteractiveWindow CreateWindow(IInteractiveEvaluator evaluator)
        {
            if (evaluator == null)
            {
                throw new ArgumentNullException(nameof(evaluator));
            }

            return new InteractiveWindow(
                _windowFactoryService,
                _contentTypeRegistry,
                _bufferFactory,
                _projectionBufferFactory,
                _editorOperationsFactory,
                _textBufferUndoManagerProvider,
                _editorFactory,
                _rtfBuilderService,
                _intellisenseSessionStackMap,
                _smartIndenterService,
                evaluator,
                _uiThreadOperationExecutor);
        }
    }
}
