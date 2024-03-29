﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the License.txt file in the project root for more information.

// Dumps commands in QueryStatus and Exec.
// #define DUMP_COMMANDS

using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Utilities;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.VisualStudio.InteractiveWindow.Shell
{
    internal sealed class VsInteractiveWindowCommandFilter : IOleCommandTarget
    {
        //
        // Command filter chain: 
        // *window* -> VsTextView -> ... -> *pre-language + the current language service's filter* -> editor services -> *preEditor* -> editor
        //
        private readonly IOleCommandTarget _preLanguageCommandFilter;
        private readonly IOleCommandTarget _editorServicesCommandFilter;
        private readonly IOleCommandTarget _preEditorCommandFilter;
        private readonly IOleCommandTarget _editorCommandFilter;
        // we route undo/redo commands to this target:
        internal IOleCommandTarget currentBufferCommandHandler;
        internal IOleCommandTarget firstLanguageServiceCommandFilter;
        private readonly IInteractiveWindow _window;
        internal readonly IVsTextView textViewAdapter;
        private readonly IWpfTextViewHost _textViewHost;
        internal readonly IEnumerable<Lazy<IVsInteractiveWindowOleCommandTargetProvider, ContentTypeMetadata>> _oleCommandTargetProviders;
        internal readonly IContentTypeRegistryService _contentTypeRegistry;

        public VsInteractiveWindowCommandFilter(IVsEditorAdaptersFactoryService adapterFactory, IInteractiveWindow window, IVsTextView textViewAdapter, IVsTextBuffer bufferAdapter, IEnumerable<Lazy<IVsInteractiveWindowOleCommandTargetProvider, ContentTypeMetadata>> oleCommandTargetProviders, IContentTypeRegistryService contentTypeRegistry)
        {
            _window = window;
            _oleCommandTargetProviders = oleCommandTargetProviders;
            _contentTypeRegistry = contentTypeRegistry;

            this.textViewAdapter = textViewAdapter;

            // make us a code window so we'll have the same colors as a normal code window.
            ErrorHandler.ThrowOnFailure(((IVsTextEditorPropertyCategoryContainer)textViewAdapter).GetPropertyCategory(DefGuidList.guidEditPropCategoryViewMasterSettings, out var propContainer));
            propContainer.SetProperty(VSEDITPROPID.VSEDITPROPID_ViewComposite_AllCodeWindowDefaults, true);
            propContainer.SetProperty(VSEDITPROPID.VSEDITPROPID_ViewGlobalOpt_AutoScrollCaretOnTextEntry, true);

            // editor services are initialized in textViewAdapter.Initialize - hook underneath them:
            _preEditorCommandFilter = new CommandFilter(this, CommandFilterLayer.PreEditor);
            ErrorHandler.ThrowOnFailure(textViewAdapter.AddCommandFilter(_preEditorCommandFilter, out _editorCommandFilter));

            textViewAdapter.Initialize(
                (IVsTextLines)bufferAdapter,
                IntPtr.Zero,
                (uint)TextViewInitFlags.VIF_HSCROLL | (uint)TextViewInitFlags.VIF_VSCROLL | (uint)TextViewInitFlags3.VIF_NO_HWND_SUPPORT,
                new[] { new INITVIEW { fSelectionMargin = 0, fWidgetMargin = 0, fVirtualSpace = 0, fDragDropMove = 1 } });

            // disable change tracking because everything will be changed
            var textViewHost = adapterFactory.GetWpfTextViewHost(textViewAdapter);

            _preLanguageCommandFilter = new CommandFilter(this, CommandFilterLayer.PreLanguage);
            ErrorHandler.ThrowOnFailure(textViewAdapter.AddCommandFilter(_preLanguageCommandFilter, out _editorServicesCommandFilter));

            _textViewHost = textViewHost;
        }

        private IOleCommandTarget TextViewCommandFilterChain
        {
            get
            {
                ThreadHelper.ThrowIfNotOnUIThread();

                // Non-character command processing starts with WindowFrame which calls ReplWindow.Exec.
                // We need to invoke the view's Exec method in order to invoke its full command chain 
                // (features add their filters to the view).
                return (IOleCommandTarget)textViewAdapter;
            }
        }

        #region IOleCommandTarget

        public int QueryStatus(ref Guid pguidCmdGroup, uint cCmds, OLECMD[] prgCmds, IntPtr pCmdText)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var nextTarget = TextViewCommandFilterChain;

            return nextTarget.QueryStatus(ref pguidCmdGroup, cCmds, prgCmds, pCmdText);
        }

        public int Exec(ref Guid pguidCmdGroup, uint nCmdID, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var nextTarget = TextViewCommandFilterChain;

            return nextTarget.Exec(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);
        }

        #endregion

        public IWpfTextViewHost TextViewHost
        {
            get
            {
                return _textViewHost;
            }
        }

        private enum CommandFilterLayer
        {
            PreLanguage,
            PreEditor
        }

        private sealed class CommandFilter : IOleCommandTarget
        {
            private readonly VsInteractiveWindowCommandFilter _window;
            private readonly CommandFilterLayer _layer;

            public CommandFilter(VsInteractiveWindowCommandFilter window, CommandFilterLayer layer)
            {
                _window = window;
                _layer = layer;
            }

            public int QueryStatus(ref Guid pguidCmdGroup, uint cCmds, OLECMD[] prgCmds, IntPtr pCmdText)
            {
                ThreadHelper.ThrowIfNotOnUIThread();

                try
                {
                    return _layer switch
                    {
                        CommandFilterLayer.PreLanguage => _window.PreLanguageCommandFilterQueryStatus(ref pguidCmdGroup, cCmds, prgCmds, pCmdText),
                        CommandFilterLayer.PreEditor => _window.PreEditorCommandFilterQueryStatus(ref pguidCmdGroup, cCmds, prgCmds, pCmdText),
                        _ => throw ExceptionUtilities.UnexpectedValue(_layer),
                    };
                }
                catch (Exception e) when (FatalError.ReportWithoutCrashUnlessCanceled(e))
                {
                    // Exceptions should not escape from command filters.
                    return _window._editorCommandFilter.QueryStatus(ref pguidCmdGroup, cCmds, prgCmds, pCmdText);
                }
            }

            public int Exec(ref Guid pguidCmdGroup, uint nCmdID, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
            {
                ThreadHelper.ThrowIfNotOnUIThread();

                try
                {
                    return _layer switch
                    {
                        CommandFilterLayer.PreLanguage => _window.PreLanguageCommandFilterExec(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut),
                        CommandFilterLayer.PreEditor => _window.PreEditorCommandFilterExec(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut),
                        _ => throw ExceptionUtilities.UnexpectedValue(_layer),
                    };
                }
                catch (Exception e) when (FatalError.ReportWithoutCrashUnlessCanceled(e))
                {
                    // Exceptions should not escape from command filters.
                    return _window._editorCommandFilter.Exec(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);
                }
            }
        }

        private int PreEditorCommandFilterQueryStatus(ref Guid pguidCmdGroup, uint cCmds, OLECMD[] prgCmds, IntPtr pCmdText)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (pguidCmdGroup == Guids.InteractiveCommandSetId)
            {
                switch ((CommandIds)prgCmds[0].cmdID)
                {
                    case CommandIds.BreakLine:
                        prgCmds[0].cmdf = _window.CurrentLanguageBuffer != null ? CommandEnabled : CommandDisabled;
                        prgCmds[0].cmdf |= (uint)OLECMDF.OLECMDF_DEFHIDEONCTXTMENU;
                        return VSConstants.S_OK;
                }
            }
            else if (pguidCmdGroup == VSConstants.GUID_VSStandardCommandSet97)
            {
                switch ((VSConstants.VSStd97CmdID)prgCmds[0].cmdID)
                {
                    // TODO: Add support of rotating clipboard ring 
                    // https://github.com/dotnet/roslyn/issues/5651
                    case VSConstants.VSStd97CmdID.PasteNextTBXCBItem:
                        prgCmds[0].cmdf = CommandDisabled;
                        return VSConstants.S_OK;
                }
            }

            return _editorCommandFilter.QueryStatus(ref pguidCmdGroup, cCmds, prgCmds, pCmdText);
        }

        private int PreEditorCommandFilterExec(ref Guid pguidCmdGroup, uint nCmdID, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var nextTarget = _editorCommandFilter;

            if (pguidCmdGroup == Guids.InteractiveCommandSetId)
            {
                switch ((CommandIds)nCmdID)
                {
                    case CommandIds.BreakLine:
                        if (_window.Operations.BreakLine())
                        {
                            return VSConstants.S_OK;
                        }
                        break;
                }
            }
            else if (pguidCmdGroup == VSConstants.VSStd2K)
            {
                switch ((VSConstants.VSStd2KCmdID)nCmdID)
                {
                    case VSConstants.VSStd2KCmdID.TYPECHAR:
                        {
                            if (_window.Operations is IInteractiveWindowOperations2 operations)
                            {
                                char typedChar = (char)(ushort)System.Runtime.InteropServices.Marshal.GetObjectForNativeVariant(pvaIn);
                                operations.TypeChar(typedChar);
                                return VSConstants.S_OK;
                            }
                            else
                            {
                                _window.Operations.Delete();
                            }
                            break;
                        }

                    case VSConstants.VSStd2KCmdID.RETURN:
                        if (_window.Operations.Return())
                        {
                            return VSConstants.S_OK;
                        }
                        break;

                    // TODO: 
                    //case VSConstants.VSStd2KCmdID.DELETEWORDLEFT:
                    //case VSConstants.VSStd2KCmdID.DELETEWORDRIGHT:
                    //    break;

                    case VSConstants.VSStd2KCmdID.BACKSPACE:
                        _window.Operations.Backspace();
                        return VSConstants.S_OK;

                    case VSConstants.VSStd2KCmdID.UP:

                        if (_window.CurrentLanguageBuffer != null && !_window.IsRunning && CaretAtEnd && UseSmartUpDown)
                        {
                            _window.Operations.HistoryPrevious();
                            return VSConstants.S_OK;
                        }
                        break;

                    case VSConstants.VSStd2KCmdID.DOWN:
                        if (_window.CurrentLanguageBuffer != null && !_window.IsRunning && CaretAtEnd && UseSmartUpDown)
                        {
                            _window.Operations.HistoryNext();
                            return VSConstants.S_OK;
                        }
                        break;

                    case VSConstants.VSStd2KCmdID.CANCEL:
                        if (_window.TextView.Selection.IsEmpty)
                        {
                            _window.Operations.Cancel();
                        }
                        break;

                    case VSConstants.VSStd2KCmdID.BOL:
                        _window.Operations.Home(false);
                        return VSConstants.S_OK;

                    case VSConstants.VSStd2KCmdID.BOL_EXT:
                        _window.Operations.Home(true);
                        return VSConstants.S_OK;

                    case VSConstants.VSStd2KCmdID.EOL:
                        _window.Operations.End(false);
                        return VSConstants.S_OK;

                    case VSConstants.VSStd2KCmdID.EOL_EXT:
                        _window.Operations.End(true);
                        return VSConstants.S_OK;
                    case VSConstants.VSStd2KCmdID.CUTLINE:
                        {
                            if (_window.Operations is IInteractiveWindowOperations2 operations)
                            {
                                operations.CutLine();
                                return VSConstants.S_OK;
                            }
                        }
                        break;
                    case VSConstants.VSStd2KCmdID.DELETELINE:
                        {
                            if (_window.Operations is IInteractiveWindowOperations2 operations)
                            {
                                operations.DeleteLine();
                                return VSConstants.S_OK;
                            }
                        }
                        break;
                }
            }
            else if (pguidCmdGroup == VSConstants.GUID_VSStandardCommandSet97)
            {
                switch ((VSConstants.VSStd97CmdID)nCmdID)
                {
                    // TODO: Add support of rotating clipboard ring 
                    // https://github.com/dotnet/roslyn/issues/5651
                    case VSConstants.VSStd97CmdID.PasteNextTBXCBItem:
                        return (int)OLE.Interop.Constants.OLECMDERR_E_NOTSUPPORTED;

                    case VSConstants.VSStd97CmdID.Paste:
                        _window.Operations.Paste();
                        return VSConstants.S_OK;

                    case VSConstants.VSStd97CmdID.Cut:
                        _window.Operations.Cut();
                        return VSConstants.S_OK;

                    case VSConstants.VSStd97CmdID.Copy:
                        {
                            if (_window.Operations is IInteractiveWindowOperations2 operations)
                            {
                                operations.Copy();
                                return VSConstants.S_OK;
                            }
                        }
                        break;

                    case VSConstants.VSStd97CmdID.Delete:
                        _window.Operations.Delete();
                        return VSConstants.S_OK;

                    case VSConstants.VSStd97CmdID.SelectAll:
                        _window.Operations.SelectAll();
                        return VSConstants.S_OK;
                }
            }

            return nextTarget.Exec(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);
        }

        private int PreLanguageCommandFilterQueryStatus(ref Guid pguidCmdGroup, uint cCmds, OLECMD[] prgCmds, IntPtr pCmdText)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var nextTarget = firstLanguageServiceCommandFilter ?? _editorServicesCommandFilter;

            if (pguidCmdGroup == Guids.InteractiveCommandSetId)
            {
                switch ((CommandIds)prgCmds[0].cmdID)
                {
                    case CommandIds.HistoryNext:
                    case CommandIds.HistoryPrevious:
                    case CommandIds.SearchHistoryNext:
                    case CommandIds.SearchHistoryPrevious:
                    case CommandIds.SmartExecute:
                        // TODO: Submit?
                        prgCmds[0].cmdf = _window.CurrentLanguageBuffer != null ? CommandEnabled : CommandDisabled;
                        prgCmds[0].cmdf |= (uint)OLECMDF.OLECMDF_DEFHIDEONCTXTMENU;
                        return VSConstants.S_OK;
                    case CommandIds.AbortExecution:
                        prgCmds[0].cmdf = _window.IsRunning ? CommandEnabled : CommandDisabled;
                        prgCmds[0].cmdf |= (uint)OLECMDF.OLECMDF_DEFHIDEONCTXTMENU;
                        return VSConstants.S_OK;
                    case CommandIds.Reset:
                        prgCmds[0].cmdf = !_window.IsResetting ? CommandEnabled : CommandDisabled;
                        prgCmds[0].cmdf |= (uint)OLECMDF.OLECMDF_DEFHIDEONCTXTMENU;
                        return VSConstants.S_OK;
                    case CommandIds.CopyCode:
                        prgCmds[0].cmdf = _window.Operations is IInteractiveWindowOperations2 ? CommandEnabled : CommandDisabled;
                        return VSConstants.S_OK;
                    default:
                        prgCmds[0].cmdf = CommandEnabled;
                        break;
                }
                prgCmds[0].cmdf |= (uint)OLECMDF.OLECMDF_DEFHIDEONCTXTMENU;
            }
            else if (currentBufferCommandHandler != null && pguidCmdGroup == VSConstants.GUID_VSStandardCommandSet97)
            {
                // undo/redo support:
                switch ((VSConstants.VSStd97CmdID)prgCmds[0].cmdID)
                {
                    case VSConstants.VSStd97CmdID.Undo:
                    case VSConstants.VSStd97CmdID.MultiLevelUndo:
                    case VSConstants.VSStd97CmdID.MultiLevelUndoList:
                    case VSConstants.VSStd97CmdID.Redo:
                    case VSConstants.VSStd97CmdID.MultiLevelRedo:
                    case VSConstants.VSStd97CmdID.MultiLevelRedoList:
                        return currentBufferCommandHandler.QueryStatus(ref pguidCmdGroup, cCmds, prgCmds, pCmdText);
                }
            }

            if (nextTarget != null)
            {
                var result = nextTarget.QueryStatus(ref pguidCmdGroup, cCmds, prgCmds, pCmdText);
#if DUMP_COMMANDS
            //DumpCmd("QS", result, ref pguidCmdGroup, prgCmds[0].cmdID, prgCmds[0].cmdf);
#endif
                return result;
            }
            return VSConstants.E_FAIL;
        }

        private int PreLanguageCommandFilterExec(ref Guid pguidCmdGroup, uint nCmdID, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var nextTarget = firstLanguageServiceCommandFilter ?? _editorServicesCommandFilter;

            if (pguidCmdGroup == Guids.InteractiveCommandSetId)
            {
                switch ((CommandIds)nCmdID)
                {
                    case CommandIds.AbortExecution: _window.Evaluator.AbortExecution(); return VSConstants.S_OK;
                    case CommandIds.Reset: _ = Task.Run(() => _window.Operations.ResetAsync()); return VSConstants.S_OK;
                    case CommandIds.SmartExecute: _window.Operations.ExecuteInput(); return VSConstants.S_OK;
                    case CommandIds.HistoryNext: _window.Operations.HistoryNext(); return VSConstants.S_OK;
                    case CommandIds.HistoryPrevious: _window.Operations.HistoryPrevious(); return VSConstants.S_OK;
                    case CommandIds.ClearScreen: _window.Operations.ClearView(); return VSConstants.S_OK;
                    case CommandIds.CopyCode:
                        {
                            if (_window.Operations is IInteractiveWindowOperations2 operation)
                            {
                                operation.CopyCode();
                            }
                            return VSConstants.S_OK;
                        }
                    case CommandIds.SearchHistoryNext:
                        _window.Operations.HistorySearchNext();
                        return VSConstants.S_OK;
                    case CommandIds.SearchHistoryPrevious:
                        _window.Operations.HistorySearchPrevious();
                        return VSConstants.S_OK;
                }
            }
            else if (pguidCmdGroup == VSConstants.VSStd2K)
            {
                switch ((VSConstants.VSStd2KCmdID)nCmdID)
                {
                    case VSConstants.VSStd2KCmdID.RETURN:
                        if (_window.Operations.TrySubmitStandardInput())
                        {
                            return VSConstants.S_OK;
                        }
                        break;

                    case VSConstants.VSStd2KCmdID.SHOWCONTEXTMENU:
                        ShowContextMenu();
                        return VSConstants.S_OK;
                }
            }
            else if (currentBufferCommandHandler != null && pguidCmdGroup == VSConstants.GUID_VSStandardCommandSet97)
            {
                // undo/redo support:
                switch ((VSConstants.VSStd97CmdID)nCmdID)
                {
                    // TODO: remove (https://github.com/dotnet/roslyn/issues/5642)
                    case VSConstants.VSStd97CmdID.FindReferences:
                        return VSConstants.S_OK;
                    case VSConstants.VSStd97CmdID.Undo:
                    case VSConstants.VSStd97CmdID.MultiLevelUndo:
                    case VSConstants.VSStd97CmdID.MultiLevelUndoList:
                    case VSConstants.VSStd97CmdID.Redo:
                    case VSConstants.VSStd97CmdID.MultiLevelRedo:
                    case VSConstants.VSStd97CmdID.MultiLevelRedoList:
                        return currentBufferCommandHandler.Exec(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);
                }
            }

            int res = nextTarget.Exec(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);
#if DUMP_COMMANDS
            DumpCmd("Exec", result, ref pguidCmdGroup, nCmdID, 0);
#endif
            return res;
        }

        private void ShowContextMenu()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var uishell = (IVsUIShell)Package.GetGlobalService(typeof(SVsUIShell));
            if (uishell != null)
            {
                var pt = System.Windows.Forms.Cursor.Position;
                var position = new[] { new POINTS { x = (short)pt.X, y = (short)pt.Y } };
                var guid = Guids.InteractiveCommandSetId;
                ErrorHandler.ThrowOnFailure(uishell.ShowContextMenu(0, ref guid, (int)MenuIds.InteractiveWindowContextMenu, position, this));
            }
        }

        private const uint CommandEnabled = (uint)(OLECMDF.OLECMDF_ENABLED | OLECMDF.OLECMDF_SUPPORTED);
        private const uint CommandDisabled = (uint)OLECMDF.OLECMDF_SUPPORTED;

        private bool CaretAtEnd
        {
            get
            {
                var caret = _window.TextView.Caret;
                return caret.Position.BufferPosition.Position == caret.Position.BufferPosition.Snapshot.Length;
            }
        }

        private bool UseSmartUpDown
        {
            get
            {
                return _window.TextView.Options.GetOptionValue(InteractiveWindowOptions.SmartUpDown);
            }
        }

        public IOleCommandTarget EditorServicesCommandFilter
        {
            get
            {
                return _editorServicesCommandFilter;
            }
        }
    }
}
