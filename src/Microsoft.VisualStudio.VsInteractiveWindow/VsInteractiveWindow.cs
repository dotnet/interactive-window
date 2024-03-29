﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the License.txt file in the project root for more information.

// Dumps commands in QueryStatus and Exec.
// #define DUMP_COMMANDS

using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Input;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.InteractiveWindow.Shell
{
    /// <summary>
    /// Default tool window for hosting interactive windows inside of Visual Studio.  This hooks up support for
    /// find in windows, forwarding commands down to the text view adapter, and providing access for setting
    /// VS specific concepts (such as language service GUIDs) for the interactive window.
    /// 
    /// Interactive windows can also be hosted outside of this tool window if the user creates an IInteractiveWindow
    /// directly.  In that case the user is responsible for doing what this class does themselves.  But the
    /// interactive window will be properly initialized for running inside of Visual Studio's process by our 
    /// VsInteractiveWindowEditorsFactoryService which handles all of the mapping of VS commands to API calls
    /// on the interactive window.
    /// </summary>
    [Guid(Guids.InteractiveToolWindowIdString)]
    internal sealed class VsInteractiveWindow : ToolWindowPane, IOleCommandTarget, IVsInteractiveWindow
    {
        // Keep in sync with Microsoft.VisualStudio.Editor.Implementation.EnableFindOptionDefinition.OptionName.
        private const string EnableFindOptionName = "Enable Autonomous Find";

        private readonly IComponentModel _componentModel;
        private readonly IVsEditorAdaptersFactoryService _editorAdapters;
        private readonly Guid _toolbarCommandSet;
        private readonly uint _toolbarId;
        private readonly IOleCommandTarget _toolbarCommandTarget;
        private readonly IInteractiveEvaluator _evaluator;

        private IInteractiveWindow _window;
        private IOleCommandTarget _commandTarget;
        private IWpfTextViewHost _textViewHost;

        internal VsInteractiveWindow(
            IComponentModel model,
            Guid providerId,
            int instanceId,
            string title,
            IInteractiveEvaluator evaluator,
            __VSCREATETOOLWIN creationFlags,
            Guid toolbarCommandSet,
            uint toolbarId,
            IOleCommandTarget toolbarCommandTarget)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            _componentModel = model;
            Caption = title;
            _editorAdapters = _componentModel.GetService<IVsEditorAdaptersFactoryService>();
            _evaluator = evaluator;

            _toolbarCommandSet = toolbarCommandSet;
            _toolbarCommandTarget = toolbarCommandTarget;
            _toolbarId = toolbarId;

            // The following calls this.OnCreate:
            Guid clsId = ToolClsid;
            Guid empty = Guid.Empty;
            Guid typeId = providerId;

            var vsShell = (IVsUIShell)ServiceProvider.GlobalProvider.GetService(typeof(SVsUIShell));
            if (vsShell == null)
            {
                throw new InvalidOperationException("Service 'SVsUIShell' is not available");
            }

            // we don't pass __VSCREATETOOLWIN.CTW_fMultiInstance because multi instance panes are
            // destroyed when closed.  We are really multi instance but we don't want to be closed.

            ErrorHandler.ThrowOnFailure(
                vsShell.CreateToolWindow(
                    (uint)(__VSCREATETOOLWIN.CTW_fInitNew | __VSCREATETOOLWIN.CTW_fToolbarHost | creationFlags),
                    (uint)instanceId,
                    GetIVsWindowPane(),
                    ref clsId,
                    ref typeId,
                    ref empty,
                    null,
                    title,
                    null,
                    out var frame
                )
            );
            var guid = GetType().GUID;
            ErrorHandler.ThrowOnFailure(frame.SetGuidProperty((int)__VSFPROPID.VSFPROPID_CmdUIGuid, ref guid));
            Frame = frame;
        }

        public void SetLanguage(Guid languageServiceGuid, IContentType contentType)
        {
            _window.SetLanguage(languageServiceGuid, contentType);
        }

        public IInteractiveWindow InteractiveWindow { get { return _window; } }

        #region ToolWindowPane overrides

        protected override void OnCreate()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            _window = _componentModel.GetService<IInteractiveWindowFactoryService>().CreateWindow(_evaluator);
            _window.SubmissionBufferAdded += SubmissionBufferAdded;
            _textViewHost = _window.GetTextViewHost();
            var textView = _textViewHost.TextView;
            textView.Options.SetOptionValue(EnableFindOptionName, true);
            AutomationProperties.SetName(textView.VisualElement, Caption);

            var viewAdapter = _editorAdapters.GetViewAdapter(textView);
            _commandTarget = viewAdapter as IOleCommandTarget;
        }

        private void SubmissionBufferAdded(object sender, SubmissionBufferAddedEventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            GetToolbarHost().ForceUpdateUI();
        }

        private void OnFramePropertyChanged(object sender, PropertyChangedEventArgs eventArgs)
        {
            if (eventArgs.PropertyName.Equals("Title", StringComparison.OrdinalIgnoreCase) && _textViewHost != null) {
                AutomationProperties.SetName(_textViewHost.TextView.VisualElement, Caption);
            }
        }

        protected override void OnClose()
        {
            _window.Close();
            base.OnClose();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_window != null)
                {
                    _window.Dispose();
                }
            }
        }

        /// <summary>
        /// This property returns the control that should be hosted in the Tool Window. It can be
        /// either a FrameworkElement (for easy creation of tool windows hosting WPF content), or it
        /// can be an object implementing one of the IVsUIWPFElement or IVsUIWin32Element
        /// interfaces.
        /// </summary>
        public override object Content
        {
            get { return _textViewHost; }
            set { }
        }

        public override void OnToolWindowCreated()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            Guid commandUiGuid = VSConstants.GUID_TextEditorFactory;
            ((IVsWindowFrame)Frame).SetGuidProperty((int)__VSFPROPID.VSFPROPID_InheritKeyBindings, ref commandUiGuid);
            ((INotifyPropertyChanged)Frame).PropertyChanged += OnFramePropertyChanged;

            base.OnToolWindowCreated();

            // add our toolbar which  is defined in our VSCT file
            var toolbarHost = GetToolbarHost();
            Guid guidInteractiveCmdSet = Guids.InteractiveCommandSetId;
            uint id = (uint)MenuIds.InteractiveWindowToolbar;

            if(_toolbarCommandSet != Guid.Empty)
            {
                guidInteractiveCmdSet = _toolbarCommandSet;
                id = _toolbarId;
            }

            if (_toolbarCommandTarget != null)
            {
                IVsToolWindowToolbarHost3 tbh3 = (IVsToolWindowToolbarHost3)toolbarHost;
                ErrorHandler.ThrowOnFailure(tbh3.AddToolbar3(VSTWT_LOCATION.VSTWT_TOP, ref guidInteractiveCmdSet, id, null, _toolbarCommandTarget));
            }
            else
            {
                ErrorHandler.ThrowOnFailure(toolbarHost.AddToolbar(VSTWT_LOCATION.VSTWT_TOP, ref guidInteractiveCmdSet, id));
            }
        }

        #endregion

        #region Window IOleCommandTarget

        public int QueryStatus(ref Guid pguidCmdGroup, uint cCmds, OLECMD[] prgCmds, IntPtr pCmdText)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            return _commandTarget.QueryStatus(ref pguidCmdGroup, cCmds, prgCmds, pCmdText);
        }

        public int Exec(ref Guid pguidCmdGroup, uint nCmdID, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            return _commandTarget.Exec(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);
        }

        #endregion

        #region IVsInteractiveWindow

        public void Show(bool focus)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var windowFrame = (IVsWindowFrame)Frame;
            ErrorHandler.ThrowOnFailure(focus ? windowFrame.Show() : windowFrame.ShowNoActivate());

            if (focus && _window.TextView is IInputElement input)
            {
                Keyboard.Focus(input);
            }
        }

        private IVsToolWindowToolbarHost GetToolbarHost()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var frame = (IVsWindowFrame)Frame;
            ErrorHandler.ThrowOnFailure(frame.GetProperty((int)__VSFPROPID.VSFPROPID_ToolbarHost, out var result));
            return (IVsToolWindowToolbarHost)result;
        }

        #endregion
    }
}
