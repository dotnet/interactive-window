﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the License.txt file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.VisualStudio.InteractiveWindow.Commands;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Projection;
using Microsoft.VisualStudio.Utilities;
using Moq;
using Xunit;
using Roslyn.Test.Utilities;

namespace Microsoft.VisualStudio.InteractiveWindow.UnitTests
{
    public partial class InteractiveWindowTests : IDisposable
    {
        #region Helpers

        private readonly InteractiveWindowTestHost _testHost;
        private readonly List<InteractiveWindow.State> _states;
        private readonly TestClipboard _testClipboard;
        private readonly TaskFactory _factory = new TaskFactory(TaskScheduler.Default);

        public InteractiveWindowTests()
        {
            _states = new List<InteractiveWindow.State>();
            _testHost = new InteractiveWindowTestHost(_states.Add);
            _testClipboard = new TestClipboard();
            ((InteractiveWindow)Window).InteractiveWindowClipboard = _testClipboard;
        }

        void IDisposable.Dispose()
        {
            _testHost.Dispose();
        }

        private IInteractiveWindow Window => _testHost.Window;

        private Task TaskRun(Action action)
        {
            return _factory.StartNew(action, CancellationToken.None, TaskCreationOptions.None, TaskScheduler.Default);
        }

        private static IEnumerable<IInteractiveWindowCommand> MockCommands(params string[] commandNames)
        {
            foreach (var name in commandNames)
            {
                var mock = new Mock<IInteractiveWindowCommand>();
                mock.Setup(m => m.Names).Returns(new[] { name });
                mock.Setup(m => m.Description).Returns(string.Format("Description of {0} command.", name));
                yield return mock.Object;
            }
        }

        private static ITextSnapshot MockSnapshot(string content)
        {
            var snapshotMock = new Mock<ITextSnapshot>();
            snapshotMock.Setup(m => m[It.IsAny<int>()]).Returns<int>(index => content[index]);
            snapshotMock.Setup(m => m.Length).Returns(content.Length);
            snapshotMock.Setup(m => m.GetText()).Returns(content);
            snapshotMock.Setup(m => m.GetText(It.IsAny<int>(), It.IsAny<int>())).Returns<int, int>((start, length) => content.Substring(start, length));
            snapshotMock.Setup(m => m.GetText(It.IsAny<Span>())).Returns<Span>(span => content.Substring(span.Start, span.Length));
            return snapshotMock.Object;
        }

        #endregion

        [WpfFact]
        public void InteractiveWindow__CommandParsing()
        {
            var commandList = MockCommands("foo", "bar", "bz", "command1").ToArray();
            var commands = new Commands.Commands(null, "%", commandList);
            AssertEx.Equal(commands.GetCommands(), commandList);

            var cmdBar = commandList[1];
            Assert.Equal("bar", cmdBar.Names.First());

            Assert.Equal("%", commands.CommandPrefix);
            commands.CommandPrefix = "#";
            Assert.Equal("#", commands.CommandPrefix);

            ////                             111111
            ////                   0123456789012345
            var s1 = MockSnapshot("#bar arg1 arg2 ");

            SnapshotSpan prefixSpan, commandSpan, argsSpan;
            IInteractiveWindowCommand cmd;

            cmd = commands.TryParseCommand(new SnapshotSpan(s1, Span.FromBounds(0, 0)), out prefixSpan, out commandSpan, out argsSpan);
            Assert.Null(cmd);

            cmd = commands.TryParseCommand(new SnapshotSpan(s1, Span.FromBounds(0, 1)), out prefixSpan, out commandSpan, out argsSpan);
            Assert.Null(cmd);

            cmd = commands.TryParseCommand(new SnapshotSpan(s1, Span.FromBounds(0, 2)), out prefixSpan, out commandSpan, out argsSpan);
            Assert.Null(cmd);
            Assert.Equal(0, prefixSpan.Start);
            Assert.Equal(1, prefixSpan.End);
            Assert.Equal(1, commandSpan.Start);
            Assert.Equal(2, commandSpan.End);
            Assert.Equal(2, argsSpan.Start);
            Assert.Equal(2, argsSpan.End);

            cmd = commands.TryParseCommand(new SnapshotSpan(s1, Span.FromBounds(0, 3)), out prefixSpan, out commandSpan, out argsSpan);
            Assert.Null(cmd);
            Assert.Equal(0, prefixSpan.Start);
            Assert.Equal(1, prefixSpan.End);
            Assert.Equal(1, commandSpan.Start);
            Assert.Equal(3, commandSpan.End);
            Assert.Equal(3, argsSpan.Start);
            Assert.Equal(3, argsSpan.End);

            cmd = commands.TryParseCommand(new SnapshotSpan(s1, Span.FromBounds(0, 4)), out prefixSpan, out commandSpan, out argsSpan);
            Assert.Equal(cmdBar, cmd);
            Assert.Equal(0, prefixSpan.Start);
            Assert.Equal(1, prefixSpan.End);
            Assert.Equal(1, commandSpan.Start);
            Assert.Equal(4, commandSpan.End);
            Assert.Equal(4, argsSpan.Start);
            Assert.Equal(4, argsSpan.End);

            cmd = commands.TryParseCommand(new SnapshotSpan(s1, Span.FromBounds(0, 5)), out prefixSpan, out commandSpan, out argsSpan);
            Assert.Equal(cmdBar, cmd);
            Assert.Equal(0, prefixSpan.Start);
            Assert.Equal(1, prefixSpan.End);
            Assert.Equal(1, commandSpan.Start);
            Assert.Equal(4, commandSpan.End);
            Assert.Equal(5, argsSpan.Start);
            Assert.Equal(5, argsSpan.End);

            cmd = commands.TryParseCommand(s1.GetExtent(), out prefixSpan, out commandSpan, out argsSpan);
            Assert.Equal(cmdBar, cmd);
            Assert.Equal(0, prefixSpan.Start);
            Assert.Equal(1, prefixSpan.End);
            Assert.Equal(1, commandSpan.Start);
            Assert.Equal(4, commandSpan.End);
            Assert.Equal(5, argsSpan.Start);
            Assert.Equal(14, argsSpan.End);

            ////                             
            ////                   0123456789
            var s2 = MockSnapshot("  #bar   ");
            cmd = commands.TryParseCommand(s2.GetExtent(), out prefixSpan, out commandSpan, out argsSpan);
            Assert.Equal(cmdBar, cmd);
            Assert.Equal(2, prefixSpan.Start);
            Assert.Equal(3, prefixSpan.End);
            Assert.Equal(3, commandSpan.Start);
            Assert.Equal(6, commandSpan.End);
            Assert.Equal(9, argsSpan.Start);
            Assert.Equal(9, argsSpan.End);

            ////                             111111
            ////                   0123456789012345
            var s3 = MockSnapshot("  #   bar  args");
            cmd = commands.TryParseCommand(s3.GetExtent(), out prefixSpan, out commandSpan, out argsSpan);
            Assert.Equal(cmdBar, cmd);
            Assert.Equal(2, prefixSpan.Start);
            Assert.Equal(3, prefixSpan.End);
            Assert.Equal(6, commandSpan.Start);
            Assert.Equal(9, commandSpan.End);
            Assert.Equal(11, argsSpan.Start);
            Assert.Equal(15, argsSpan.End);
        }

        [WpfFact]
        public void InteractiveWindow_GetCommands()
        {
            var interactiveCommands = new InteractiveCommandsFactory(null, null).CreateInteractiveCommands(
                Window,
                "#",
                _testHost.ExportProvider.GetExports<IInteractiveWindowCommand>().Select(x => x.Value).ToArray());

            var commands = interactiveCommands.GetCommands();

            Assert.NotEmpty(commands);
            Assert.Equal(2, commands.Where(n => n.Names.First() == "cls").Count());
            Assert.Equal(2, commands.Where(n => n.Names.Last() == "clear").Count());
            Assert.NotNull(commands.Where(n => n.Names.First() == "help").SingleOrDefault());
            Assert.NotNull(commands.Where(n => n.Names.First() == "reset").SingleOrDefault());
        }

        [WorkItem(8121, "https://github.com/dotnet/roslyn/issues/8121")]
        [WpfFact]
        public void InteractiveWindow_GetHelpShortcutDescriptionss()
        {
            var interactiveCommands = new InteractiveCommandsFactory(null, null).CreateInteractiveCommands(
                Window,
                string.Empty,
                Enumerable.Empty<IInteractiveWindowCommand>());

            var noSmartUpDownExpected =
$@"  Enter                {InteractiveWindowResources.EnterHelp}
  Ctrl-Enter           {InteractiveWindowResources.CtrlEnterHelp1}
                       {InteractiveWindowResources.CtrlEnterHelp2}
  Shift-Enter          {InteractiveWindowResources.ShiftEnterHelp}
  Escape               {InteractiveWindowResources.EscapeHelp}
  Alt-UpArrow          {InteractiveWindowResources.AltUpArrowHelp}
  Alt-DownArrow        {InteractiveWindowResources.AltDownArrowHelp}
  Ctrl-Alt-UpArrow     {InteractiveWindowResources.CtrlAltUpArrowHelp}
  Ctrl-Alt-DownArrow   {InteractiveWindowResources.CtrlAltDownArrowHelp}
  Ctrl-K, Ctrl-Enter   {InteractiveWindowResources.CtrlKCtrlEnterHelp}
  Ctrl-E, Ctrl-Enter   {InteractiveWindowResources.CtrlECtrlEnterHelp}
  Ctrl-A               {InteractiveWindowResources.CtrlAHelp}
";

            // By default, SmartUpDown option is not set
            var descriptions = ((Commands.Commands)interactiveCommands).ShortcutDescriptions;
            Assert.Equal(noSmartUpDownExpected, descriptions);


            var withSmartUpDownExpected = noSmartUpDownExpected +
$@"  UpArrow              {InteractiveWindowResources.UpArrowHelp1}
                       {InteractiveWindowResources.UpArrowHelp2}
  DownArrow            {InteractiveWindowResources.DownArrowHelp1}
                       {InteractiveWindowResources.DownArrowHelp2}
";

            // Set SmartUpDown option to true
            Window.TextView.Options.SetOptionValue(InteractiveWindowOptions.SmartUpDown, true);

            descriptions = ((Commands.Commands)interactiveCommands).ShortcutDescriptions;
            Assert.Equal(withSmartUpDownExpected, descriptions);
        }

        [WorkItem(6625, "https://github.com/dotnet/roslyn/issues/6625")]
        [WpfFact]
        public void InteractiveWindow_DisplayCommandsHelp()
        {
            var commandList = MockCommands("foo").ToArray();
            var commands = new Commands.Commands(null, "&", commandList);

            Assert.Equal(new string[] { "&foo                 Description of foo command." }, commands.Help().ToArray());
        }

        [WorkItem(3970, "https://github.com/dotnet/roslyn/issues/3970")]
        [WpfFact]
        public async Task ResetStateTransitions()
        {
            await Window.Operations.ResetAsync().ConfigureAwait(true);
            Assert.Equal(_states, new[]
            {
                InteractiveWindow.State.Initializing,
                InteractiveWindow.State.WaitingForInput,
                InteractiveWindow.State.Resetting,
                InteractiveWindow.State.WaitingForInput,
            });
        }

        [WpfFact]
        public async Task DoubleInitialize()
        {
            try
            {
                await Window.InitializeAsync().ConfigureAwait(true);
                Assert.True(false);
            }
            catch (InvalidOperationException)
            {
            }
        }

        [WpfFact]
        public void AccessPropertiesOnUIThread()
        {
            foreach (var property in typeof(IInteractiveWindow).GetProperties())
            {
                Assert.Null(property.SetMethod);
                property.GetMethod.Invoke(Window, Array.Empty<object>());
            }

            Assert.Empty(typeof(IInteractiveWindowOperations).GetProperties());
        }

        [WpfFact]
        public async Task AccessPropertiesOnNonUIThread()
        {
            foreach (var property in typeof(IInteractiveWindow).GetProperties())
            {
                Assert.Null(property.SetMethod);
                await TaskRun(() => property.GetMethod.Invoke(Window, Array.Empty<object>())).ConfigureAwait(true);
            }

            Assert.Empty(typeof(IInteractiveWindowOperations).GetProperties());
        }

        /// <remarks>
        /// Confirm that we are, in fact, running on a non-UI thread.
        /// </remarks>
        [WpfFact]
        public async Task NonUIThread()
        {
            await TaskRun(() => Assert.False(((InteractiveWindow)Window).OnUIThread())).ConfigureAwait(true);
        }

        [WpfFact]
        public async Task CallCloseOnNonUIThread()
        {
            await TaskRun(() => Window.Close()).ConfigureAwait(true);
        }

        [WpfFact]
        public async Task CallInsertCodeOnNonUIThread()
        {
            await TaskRun(() => Window.InsertCode("1")).ConfigureAwait(true);
        }

        [WpfFact]
        public async Task CallSubmitAsyncOnNonUIThread()
        {
            await TaskRun(() => Window.SubmitAsync(Array.Empty<string>()).GetAwaiter().GetResult()).ConfigureAwait(true);
        }

        [WpfFact]
        public async Task CallWriteOnNonUIThread()
        {
            await TaskRun(() => Window.WriteLine("1")).ConfigureAwait(true);
            await TaskRun(() => Window.Write("1")).ConfigureAwait(true);
            await TaskRun(() => Window.WriteErrorLine("1")).ConfigureAwait(true);
            await TaskRun(() => Window.WriteError("1")).ConfigureAwait(true);
        }

        [WpfFact]
        public async Task CallFlushOutputOnNonUIThread()
        {
            Window.Write("1"); // Something to flush.
            await TaskRun(() => Window.FlushOutput()).ConfigureAwait(true);
        }

        [WpfFact]
        public async Task CallAddInputOnNonUIThread()
        {
            await TaskRun(() => Window.AddInput("1")).ConfigureAwait(true);
        }

        [WpfFact]
        public async Task CallAddToHistoryOnNonUIThread()
        {
            await TaskRun(() => Window.AddToHistory("1")).ConfigureAwait(true);
        }

        /// <remarks>
        /// Call is blocking, so we can't write a simple non-failing test.
        /// </remarks>
        [WpfFact]
        public void CallReadStandardInputOnUIThread()
        {
            Assert.Throws<InvalidOperationException>(() => Window.ReadStandardInput());
        }

        [WpfFact]
        public async Task CallBackspaceOnNonUIThread()
        {
            Window.InsertCode("1"); // Something to backspace.
            await TaskRun(() => Window.Operations.Backspace()).ConfigureAwait(true);
        }

        [WpfFact]
        public async Task CallBreakLineOnNonUIThread()
        {
            await TaskRun(() => Window.Operations.BreakLine()).ConfigureAwait(true);
        }

        [WpfFact]
        public async Task CallClearHistoryOnNonUIThread()
        {
            Window.AddInput("1"); // Need a history entry.
            await TaskRun(() => Window.Operations.ClearHistory()).ConfigureAwait(true);
        }

        [WpfFact]
        public async Task CallClearViewOnNonUIThread()
        {
            Window.InsertCode("1"); // Something to clear.
            await TaskRun(() => Window.Operations.ClearView()).ConfigureAwait(true);
        }

        [WpfFact]
        public async Task CallHistoryNextOnNonUIThread()
        {
            Window.AddInput("1"); // Need a history entry.
            await TaskRun(() => Window.Operations.HistoryNext()).ConfigureAwait(true);
        }

        [WpfFact]
        public async Task CallHistoryPreviousOnNonUIThread()
        {
            Window.AddInput("1"); // Need a history entry.
            await TaskRun(() => Window.Operations.HistoryPrevious()).ConfigureAwait(true);
        }

        [WpfFact]
        public async Task CallHistorySearchNextOnNonUIThread()
        {
            Window.AddInput("1"); // Need a history entry.
            await TaskRun(() => Window.Operations.HistorySearchNext()).ConfigureAwait(true);
        }

        [WpfFact]
        public async Task CallHistorySearchPreviousOnNonUIThread()
        {
            Window.AddInput("1"); // Need a history entry.
            await TaskRun(() => Window.Operations.HistorySearchPrevious()).ConfigureAwait(true);
        }

        [WpfFact]
        public async Task CallHomeOnNonUIThread()
        {
            Window.Operations.BreakLine(); // Distinguish Home from End.
            await TaskRun(() => Window.Operations.Home(true)).ConfigureAwait(true);
        }

        [WpfFact]
        public async Task CallEndOnNonUIThread()
        {
            Window.Operations.BreakLine(); // Distinguish Home from End.
            await TaskRun(() => Window.Operations.End(true)).ConfigureAwait(true);
        }

        [WpfFact]
        public void ScrollToCursorOnHomeAndEndOnNonUIThread()
        {
            Window.InsertCode(new string('1', 512));    // a long input string 

            var textView = Window.TextView;

            Window.Operations.Home(false);
            Assert.True(textView.TextViewModel.IsPointInVisualBuffer(textView.Caret.Position.BufferPosition,
                                                                     textView.Caret.Position.Affinity));
            Window.Operations.End(false);
            Assert.True(textView.TextViewModel.IsPointInVisualBuffer(textView.Caret.Position.BufferPosition,
                                                                     textView.Caret.Position.Affinity));
        }

        [WpfFact]
        public async Task CallSelectAllOnNonUIThread()
        {
            Window.InsertCode("1"); // Something to select.
            await TaskRun(() => Window.Operations.SelectAll()).ConfigureAwait(true);
        }

        [WpfFact]
        public async Task CallPasteOnNonUIThread()
        {
            await TaskRun(() => Window.Operations.Paste()).ConfigureAwait(true);
        }

        [WpfFact]
        public async Task CallCutOnNonUIThread()
        {
            await TaskRun(() => Window.Operations.Cut()).ConfigureAwait(true);
        }

        [WpfFact]
        public async Task CallDeleteOnNonUIThread()
        {
            await TaskRun(() => Window.Operations.Delete()).ConfigureAwait(true);
        }

        [WpfFact]
        public async Task CallReturnOnNonUIThread()
        {
            await TaskRun(() => Window.Operations.Return()).ConfigureAwait(true);
        }

        [WpfFact]
        public async Task CallTrySubmitStandardInputOnNonUIThread()
        {
            await TaskRun(() => Window.Operations.TrySubmitStandardInput()).ConfigureAwait(true);
        }

        [WpfFact]
        public async Task CallResetAsyncOnNonUIThread()
        {
            await TaskRun(() => Window.Operations.ResetAsync()).ConfigureAwait(true);
        }

        [WpfFact]
        public async Task CallExecuteInputOnNonUIThread()
        {
            await TaskRun(() => Window.Operations.ExecuteInput()).ConfigureAwait(true);
        }

        [WpfFact]
        public async Task CallCancelOnNonUIThread()
        {
            await TaskRun(() => Window.Operations.Cancel()).ConfigureAwait(true);
        }

        [WorkItem(4235, "https://github.com/dotnet/roslyn/issues/4235")]
        [WpfFact]
        public void TestIndentation1()
        {
            TestIndentation(indentSize: 1);
        }

        [WorkItem(4235, "https://github.com/dotnet/roslyn/issues/4235")]
        [WpfFact]
        public void TestIndentation2()
        {
            TestIndentation(indentSize: 2);
        }

        [WorkItem(4235, "https://github.com/dotnet/roslyn/issues/4235")]
        [WpfFact]
        public void TestIndentation3()
        {
            TestIndentation(indentSize: 3);
        }

        [WorkItem(4235, "https://github.com/dotnet/roslyn/issues/4235")]
        [WpfFact]
        public void TestIndentation4()
        {
            TestIndentation(indentSize: 4);
        }

        private void TestIndentation(int indentSize)
        {
            const int promptWidth = 2;

            _testHost.ExportProvider.GetExport<TestSmartIndentProvider>().Value.SmartIndent = new TestSmartIndent(
                promptWidth,
                promptWidth + indentSize,
                promptWidth
            );

            AssertCaretVirtualPosition(0, promptWidth);
            Window.InsertCode("{");
            AssertCaretVirtualPosition(0, promptWidth + 1);
            Window.Operations.BreakLine();
            AssertCaretVirtualPosition(1, promptWidth + indentSize);
            Window.InsertCode("Console.WriteLine();");
            Window.Operations.BreakLine();
            AssertCaretVirtualPosition(2, promptWidth);
            Window.InsertCode("}");
            AssertCaretVirtualPosition(2, promptWidth + 1);
        }

        private void AssertCaretVirtualPosition(int expectedLine, int expectedColumn)
        {
            ITextSnapshotLine actualLine;
            int actualColumn;
            Window.TextView.Caret.Position.VirtualBufferPosition.GetLineAndColumn(out actualLine, out actualColumn);
            Assert.Equal(expectedLine, actualLine.LineNumber);
            Assert.Equal(expectedColumn, actualColumn);
        }

        [WpfFact]
        public void ResetCommandArgumentParsing_Success()
        {
            bool initialize;
            Assert.True(ResetCommand.TryParseArguments("", out initialize));
            Assert.True(initialize);

            Assert.True(ResetCommand.TryParseArguments(" ", out initialize));
            Assert.True(initialize);

            Assert.True(ResetCommand.TryParseArguments("\r\n", out initialize));
            Assert.True(initialize);

            Assert.True(ResetCommand.TryParseArguments("noconfig", out initialize));
            Assert.False(initialize);

            Assert.True(ResetCommand.TryParseArguments(" noconfig ", out initialize));
            Assert.False(initialize);

            Assert.True(ResetCommand.TryParseArguments("\r\nnoconfig\r\n", out initialize));
            Assert.False(initialize);
        }

        [WpfFact]
        public void ResetCommandArgumentParsing_Failure()
        {
            bool initialize;
            Assert.False(ResetCommand.TryParseArguments("a", out initialize));
            Assert.False(ResetCommand.TryParseArguments("noconfi", out initialize));
            Assert.False(ResetCommand.TryParseArguments("noconfig1", out initialize));
            Assert.False(ResetCommand.TryParseArguments("noconfig 1", out initialize));
            Assert.False(ResetCommand.TryParseArguments("1 noconfig", out initialize));
            Assert.False(ResetCommand.TryParseArguments("noconfig\r\na", out initialize));
            Assert.False(ResetCommand.TryParseArguments("nOcOnfIg", out initialize));
        }

        [WpfFact]
        public void ResetCommandNoConfigClassification()
        {
            Assert.Empty(ResetCommand.GetNoConfigPositions(""));
            Assert.Empty(ResetCommand.GetNoConfigPositions("a"));
            Assert.Empty(ResetCommand.GetNoConfigPositions("noconfi"));
            Assert.Empty(ResetCommand.GetNoConfigPositions("noconfig1"));
            Assert.Empty(ResetCommand.GetNoConfigPositions("1noconfig"));
            Assert.Empty(ResetCommand.GetNoConfigPositions("1noconfig1"));
            Assert.Empty(ResetCommand.GetNoConfigPositions("nOcOnfIg"));

            Assert.Equal(new[] { 0 }, ResetCommand.GetNoConfigPositions("noconfig"));
            Assert.Equal(new[] { 0 }, ResetCommand.GetNoConfigPositions("noconfig "));
            Assert.Equal(new[] { 1 }, ResetCommand.GetNoConfigPositions(" noconfig"));
            Assert.Equal(new[] { 1 }, ResetCommand.GetNoConfigPositions(" noconfig "));
            Assert.Equal(new[] { 2 }, ResetCommand.GetNoConfigPositions("\r\nnoconfig"));
            Assert.Equal(new[] { 0 }, ResetCommand.GetNoConfigPositions("noconfig\r\n"));
            Assert.Equal(new[] { 2 }, ResetCommand.GetNoConfigPositions("\r\nnoconfig\r\n"));
            Assert.Equal(new[] { 6 }, ResetCommand.GetNoConfigPositions("error noconfig"));

            Assert.Equal(new[] { 0, 9 }, ResetCommand.GetNoConfigPositions("noconfig noconfig"));
            Assert.Equal(new[] { 0, 15 }, ResetCommand.GetNoConfigPositions("noconfig error noconfig"));
        }

        [WorkItem(4755, "https://github.com/dotnet/roslyn/issues/4755")]
        [WpfFact]
        public void ReformatBraces()
        {
            var buffer = Window.CurrentLanguageBuffer;
            var snapshot = buffer.CurrentSnapshot;
            Assert.Equal(0, snapshot.Length);

            // Text before reformatting.
            snapshot = ApplyChanges(
                buffer,
                new TextChange(0, 0, "{ {\r\n } }"));

            // Text after reformatting.
            Assert.Equal(9, snapshot.Length);
            snapshot = ApplyChanges(
                buffer,
                new TextChange(1, 1, "\r\n    "),
                new TextChange(5, 1, "    "),
                new TextChange(7, 1, "\r\n"));

            // Text from language buffer.
            var actualText = snapshot.GetText();
            Assert.Equal("{\r\n    {\r\n    }\r\n}", actualText);

            // Text including prompts.
            buffer = Window.TextView.TextBuffer;
            snapshot = buffer.CurrentSnapshot;
            actualText = snapshot.GetText();
            Assert.Equal("> {\r\n>     {\r\n>     }\r\n> }", actualText);

            // Prompts should be read-only.
            var regions = buffer.GetReadOnlyExtents(new Span(0, snapshot.Length));
            AssertEx.SetEqual(regions,
                new Span(0, 2),
                new Span(5, 2),
                new Span(14, 2),
                new Span(23, 2));
        }

        [WpfFact]
        public async Task CancelMultiLineInput()
        {
            ApplyChanges(
                Window.CurrentLanguageBuffer,
                new TextChange(0, 0, "{\r\n    {\r\n    }\r\n}"));

            // Text including prompts.
            var buffer = Window.TextView.TextBuffer;
            var snapshot = buffer.CurrentSnapshot;
            Assert.Equal("> {\r\n>     {\r\n>     }\r\n> }", snapshot.GetText());

            await TaskRun(() => Window.Operations.Cancel()).ConfigureAwait(true);

            // Text after cancel.
            snapshot = buffer.CurrentSnapshot;
            Assert.Equal("> ", snapshot.GetText());
        }

        [WpfFact]
        public void SelectAllInHeader()
        {
            Window.WriteLine("Header");
            Window.FlushOutput();
            var fullText = GetTextFromCurrentSnapshot();
            Assert.Equal("Header\r\n> ", fullText);

            Window.TextView.Caret.MoveTo(new SnapshotPoint(Window.TextView.TextBuffer.CurrentSnapshot, 1));
            Window.Operations.SelectAll(); // Used to throw.

            // Everything is selected.
            Assert.Equal(new Span(0, fullText.Length), Window.TextView.Selection.SelectedSpans.Single().Span);
        }

        [WpfFact]
        public async Task DeleteWithOutSelectionInReadOnlyArea()
        {
            await Submit(
@"1",
@"1
").ConfigureAwait(true);
            Window.InsertCode("2");

            var caret = Window.TextView.Caret;

            // with empty selection, Delete() only handles caret movement,
            // so we can only test caret location. 

            // Delete() with caret in readonly area, no-op       
            caret.MoveToPreviousCaretPosition();
            caret.MoveToPreviousCaretPosition();
            caret.MoveToPreviousCaretPosition();
            caret.MoveToPreviousCaretPosition();
            AssertCaretVirtualPosition(1, 1);

            Window.Operations.Delete();
            AssertCaretVirtualPosition(1, 1);

            // Delete() with caret in active prompt, move caret to 
            // closest editable buffer
            caret.MoveToNextCaretPosition();
            AssertCaretVirtualPosition(2, 0);
            Window.Operations.Delete();
            AssertCaretVirtualPosition(2, 2);
        }

        [WpfFact]
        public async Task DeleteWithSelectionInReadonlyArea()
        {
            await Submit(
@"1",
@"1
").ConfigureAwait(true);
            Window.InsertCode("23");

            var caret = Window.TextView.Caret;
            var selection = Window.TextView.Selection;

            // Delete() with selection in readonly area, no-op       
            caret.MoveToPreviousCaretPosition();
            caret.MoveToPreviousCaretPosition();
            caret.MoveToPreviousCaretPosition();
            caret.MoveToPreviousCaretPosition();
            caret.MoveToPreviousCaretPosition();
            AssertCaretVirtualPosition(1, 1);

            Window.Operations.SelectAll();

            Window.Operations.Delete();
            Assert.Equal("> 1\r\n1\r\n> 23", GetTextFromCurrentSnapshot());

            // Delete() with selection in active prompt, no-op
            selection.Clear();
            var start = caret.MoveToNextCaretPosition().VirtualBufferPosition;
            caret.MoveToNextCaretPosition();
            var end = caret.MoveToNextCaretPosition().VirtualBufferPosition;
            AssertCaretVirtualPosition(2, 2);

            selection.Select(start, end);

            Window.Operations.Delete();
            Assert.Equal("> 1\r\n1\r\n> 23", GetTextFromCurrentSnapshot());

            // Delete() with selection overlaps with editable buffer, 
            // delete editable content and move caret to closest editable location 
            selection.Clear();
            caret.MoveToPreviousCaretPosition();
            start = caret.MoveToPreviousCaretPosition().VirtualBufferPosition;
            caret.MoveToNextCaretPosition();
            caret.MoveToNextCaretPosition();
            end = caret.MoveToNextCaretPosition().VirtualBufferPosition;
            AssertCaretVirtualPosition(2, 3);

            selection.Select(start, end);

            Window.Operations.Delete();
            Assert.Equal("> 1\r\n1\r\n> 3", GetTextFromCurrentSnapshot());
            AssertCaretVirtualPosition(2, 2);
        }

        [WpfFact]
        public async Task BackspaceWithOutSelectionInReadOnlyArea()
        {
            await Submit(
@"1",
@"1
").ConfigureAwait(true);
            Window.InsertCode("int x");
            Window.Operations.BreakLine();
            Window.InsertCode(";");

            var caret = Window.TextView.Caret;

            // Backspace() with caret in readonly area, no-op
            Window.Operations.Home(false);
            Window.Operations.Home(false);
            caret.MoveToPreviousCaretPosition();
            caret.MoveToPreviousCaretPosition();
            caret.MoveToPreviousCaretPosition();
            Window.Operations.Home(false);
            caret.MoveToPreviousCaretPosition();
            caret.MoveToPreviousCaretPosition();
            caret.MoveToPreviousCaretPosition();
            AssertCaretVirtualPosition(1, 1);

            Window.Operations.Backspace();
            AssertCaretVirtualPosition(1, 1);
            Assert.Equal("> 1\r\n1\r\n> int x\r\n> ;", GetTextFromCurrentSnapshot());

            // Backspace() with caret in 2nd active prompt, move caret to 
            // closest editable buffer then delete previous character (breakline)        
            caret.MoveToNextCaretPosition();
            Window.Operations.End(false);
            caret.MoveToNextCaretPosition();
            caret.MoveToNextCaretPosition();
            AssertCaretVirtualPosition(3, 1);

            Window.Operations.Backspace();
            AssertCaretVirtualPosition(2, 7);
            Assert.Equal("> 1\r\n1\r\n> int x;", GetTextFromCurrentSnapshot());
        }

        [WpfFact]
        public async Task BackspaceWithSelectionInReadonlyArea()
        {
            await Submit(
@"1",
@"1
").ConfigureAwait(true);
            Window.InsertCode("int x");
            Window.Operations.BreakLine();
            Window.InsertCode(";");

            var caret = Window.TextView.Caret;
            var selection = Window.TextView.Selection;

            // Backspace() with selection in readonly area, no-op      
            Window.Operations.Home(false);
            Window.Operations.Home(false);
            caret.MoveToPreviousCaretPosition();
            caret.MoveToPreviousCaretPosition();
            caret.MoveToPreviousCaretPosition();
            Window.Operations.Home(false);
            caret.MoveToPreviousCaretPosition();
            caret.MoveToPreviousCaretPosition();
            caret.MoveToPreviousCaretPosition();
            AssertCaretVirtualPosition(1, 1);

            Window.Operations.SelectAll();

            Window.Operations.Backspace();
            Assert.Equal("> 1\r\n1\r\n> int x\r\n> ;", GetTextFromCurrentSnapshot());

            // Backspace() with selection in active prompt, no-op
            selection.Clear();
            var start = caret.MoveToNextCaretPosition().VirtualBufferPosition;
            caret.MoveToNextCaretPosition();
            var end = caret.MoveToNextCaretPosition().VirtualBufferPosition;
            AssertCaretVirtualPosition(2, 2);

            selection.Select(start, end);

            Window.Operations.Backspace();
            Assert.Equal("> 1\r\n1\r\n> int x\r\n> ;", GetTextFromCurrentSnapshot());

            // Backspace() with selection overlaps with editable buffer
            selection.Clear();
            Window.Operations.End(false);
            start = caret.Position.VirtualBufferPosition;
            caret.MoveToNextCaretPosition();
            caret.MoveToNextCaretPosition();
            end = caret.MoveToNextCaretPosition().VirtualBufferPosition;
            AssertCaretVirtualPosition(3, 2);

            selection.Select(start, end);

            Window.Operations.Backspace();
            Assert.Equal("> 1\r\n1\r\n> int x;", GetTextFromCurrentSnapshot());
            AssertCaretVirtualPosition(2, 7);
        }

        [WpfFact]
        public async Task ReturnWithOutSelectionInReadOnlyArea()
        {
            await Submit(
@"1",
@"1
").ConfigureAwait(true);
            var caret = Window.TextView.Caret;

            // Return() with caret in readonly area, no-op       
            caret.MoveToPreviousCaretPosition();
            caret.MoveToPreviousCaretPosition();
            caret.MoveToPreviousCaretPosition();
            AssertCaretVirtualPosition(1, 1);

            Window.Operations.Return();
            AssertCaretVirtualPosition(1, 1);

            // Return() with caret in active prompt, move caret to 
            // closest editable buffer first
            caret.MoveToNextCaretPosition();
            AssertCaretVirtualPosition(2, 0);

            Window.Operations.Return();
            AssertCaretVirtualPosition(3, 2);
        }

        [WpfFact]
        public async Task ReturnWithSelectionInReadonlyArea()
        {
            var caret = Window.TextView.Caret;
            var selection = Window.TextView.Selection;

            await Submit(
@"1",
@"1
").ConfigureAwait(true);
            Window.InsertCode("23");

            // Return() with selection in readonly area, no-op
            // > 1
            // |1 |
            // > 23
            MoveCaretToPreviousPosition(5);
            AssertCaretVirtualPosition(1, 1);

            Window.Operations.SelectAll();

            Window.Operations.Return();
            Assert.Equal("> 1\r\n1\r\n> 23", GetTextFromCurrentSnapshot());

            // Return() with selection in active prompt
            // > 1
            // 1
            // |> |23
            selection.Clear();
            MoveCaretToNextPosition(1);
            var start = caret.Position.VirtualBufferPosition;
            MoveCaretToNextPosition(2);
            var end = caret.Position.VirtualBufferPosition;
            AssertCaretVirtualPosition(2, 2);

            selection.Select(start, end);

            Window.Operations.Return();
            Assert.Equal("> 1\r\n1\r\n> \r\n> 23", GetTextFromCurrentSnapshot());

            // Return() with selection overlaps with editable buffer, 
            // > 1
            // 1
            // > 
            // |> 2|3
            selection.Clear();
            MoveCaretToPreviousPosition(2);
            start = caret.Position.VirtualBufferPosition;
            MoveCaretToNextPosition(3);
            end = caret.Position.VirtualBufferPosition;
            AssertCaretVirtualPosition(3, 3);

            selection.Select(start, end);

            Window.Operations.Return();
            Assert.Equal("> 1\r\n1\r\n> \r\n> \r\n> 3", GetTextFromCurrentSnapshot());
            AssertCaretVirtualPosition(4, 2);
        }

        [WpfFact]
        public async Task DeleteLineWithOutSelection()
        {
            await Submit(
@"1",
@"1
").ConfigureAwait(true);
            var caret = Window.TextView.Caret;

            // DeleteLine with caret in readonly area
            caret.MoveToPreviousCaretPosition();
            caret.MoveToPreviousCaretPosition();
            caret.MoveToPreviousCaretPosition();

            AssertCaretVirtualPosition(1, 1);
            Window.Operations.DeleteLine();
            Assert.Equal("> 1\r\n1\r\n> ", GetTextFromCurrentSnapshot());
            AssertCaretVirtualPosition(1, 1);

            // DeleteLine with caret in active prompt
            caret.MoveToNextCaretPosition();
            caret.MoveToNextCaretPosition();
            caret.MoveToNextCaretPosition();
            Window.InsertCode("int x");
            Window.Operations.BreakLine();
            Window.InsertCode(";");
            for (int i = 0; i < 11; ++i)
            {
                caret.MoveToPreviousCaretPosition();
            }

            AssertCaretVirtualPosition(2, 0);
            Window.Operations.DeleteLine();
            Assert.Equal("> 1\r\n1\r\n> ;", GetTextFromCurrentSnapshot());
            AssertCaretVirtualPosition(2, 2);

            // DeleteLine with caret in editable area   
            caret.MoveToNextCaretPosition();

            Window.Operations.DeleteLine();
            Assert.Equal("> 1\r\n1\r\n> ", GetTextFromCurrentSnapshot());
            AssertCaretVirtualPosition(2, 2);
        }

        [WpfFact]
        public async Task DeleteLineWithSelection()
        {
            var caret = Window.TextView.Caret;
            var selection = Window.TextView.Selection;

            await Submit(
@"1",
@"1
").ConfigureAwait(true);

            // DeleteLine with selection in readonly area  
            // > 1
            // |1 |
            // > 
            MoveCaretToPreviousPosition(3);
            Window.Operations.SelectAll();
            Window.Operations.DeleteLine();
            Assert.Equal("> 1\r\n1\r\n> ", GetTextFromCurrentSnapshot());

            // DeleteLine with selection in active prompt
            // > 1
            // 1
            // |>| int x
            // > ; 
            selection.Clear();
            MoveCaretToNextPosition(3);
            Window.InsertCode("int x");
            Window.Operations.BreakLine();
            Window.InsertCode(";");
            MoveCaretToPreviousPosition(11);
            var start = caret.Position.VirtualBufferPosition;
            MoveCaretToNextPosition(1);
            var end = caret.Position.VirtualBufferPosition;
            selection.Select(start, end);
            Window.Operations.DeleteLine();
            Assert.Equal("> 1\r\n1\r\n> ;", GetTextFromCurrentSnapshot());
            AssertCaretVirtualPosition(2, 2);
            Assert.True(selection.IsEmpty);

            // DeleteLine with selection in editable area  
            // > 1
            // 1
            // > int |x|; 
            Window.InsertCode("int x");
            MoveCaretToPreviousPosition(1);
            start = caret.Position.VirtualBufferPosition;
            MoveCaretToNextPosition(1);
            end = caret.Position.VirtualBufferPosition;
            selection.Select(start, end);
            Window.Operations.DeleteLine();
            Assert.Equal("> 1\r\n1\r\n> ", GetTextFromCurrentSnapshot());
            AssertCaretVirtualPosition(2, 2);
            Assert.True(selection.IsEmpty);

            // DeleteLine with selection spans all areas, no-op     
            Window.InsertCode("int x");
            Window.Operations.BreakLine();
            Window.InsertCode(";");
            Window.Operations.SelectAll();
            Window.Operations.SelectAll();
            Window.Operations.DeleteLine();
            Assert.Equal("> 1\r\n1\r\n> int x\r\n> ;", GetTextFromCurrentSnapshot());
        }

        [WpfFact]
        public async Task SubmitAsyncNone()
        {
            await SubmitAsync().ConfigureAwait(true);
        }

        [WpfFact]
        public async Task SubmitAsyncSingle()
        {
            await SubmitAsync("1").ConfigureAwait(true);
        }

        [WorkItem(5964, "https://github.com/dotnet/roslyn/issues/5964")]
        [WpfFact]
        public async Task SubmitAsyncMultiple()
        {
            await SubmitAsync("1", "2", "1 + 2").ConfigureAwait(true);
        }

        [WorkItem(8569, "https://github.com/dotnet/roslyn/issues/8569")]
        [WpfFact]
        public async Task SubmitAsyncEmptyLine()
        {
            await SubmitAsync("1", "", "1 + 2").ConfigureAwait(true);
        }

        private async Task SubmitAsync(params string[] submissions)
        {
            var actualSubmissions = new List<string>();
            var evaluator = _testHost.Evaluator;
            EventHandler<string> onExecute = (_, s) => actualSubmissions.Add(s.TrimEnd());

            evaluator.OnExecute += onExecute;
            await TaskRun(() => Window.SubmitAsync(submissions)).ConfigureAwait(true);
            evaluator.OnExecute -= onExecute;

            AssertEx.Equal(submissions, actualSubmissions);
        }

        [WpfFact]
        public void AddToHistory_EmptyLine() 
        {
            var original = new InteractiveWindowTextSnapshot(_testHost);
            Window.AddToHistory("");

            var actual = new InteractiveWindowTextSnapshot(_testHost);
            InteractiveWindowTextSnapshot.AssertEqual(original, actual);
        }

        [WpfFact]
        public void AddToHistory_NewLine() 
        {
            var original = new InteractiveWindowTextSnapshot(_testHost);
            Window.AddToHistory(Environment.NewLine);

            var actual = new InteractiveWindowTextSnapshot(_testHost);
            var expected = original
                .InsertBeforeLastPrompt(_testHost.Evaluator.GetPrompt(), ReplSpanKind.Prompt)
                .InsertBeforeLastPrompt(Environment.NewLine, ReplSpanKind.Input);

            InteractiveWindowTextSnapshot.AssertEqual(expected, actual);
        }

        [WpfFact]
        public async Task AddToHistory_BetweenWrites() 
        {
            var original = new InteractiveWindowTextSnapshot(_testHost);

            await Task.Run(() => Window.WriteLine("a"));
            await Task.Run(() => Window.WriteLine("b"));

            Window.AddToHistory("c");

            await Task.Run(() => Window.WriteLine("d"));
            await Task.Run(() => Window.WriteLine("e"));

            Window.FlushOutput();

            var actual = new InteractiveWindowTextSnapshot(_testHost);
            var expected = original
                .InsertBeforeLastPrompt($"a{Environment.NewLine}b{Environment.NewLine}", ReplSpanKind.Output)
                .InsertBeforeLastPrompt(_testHost.Evaluator.GetPrompt(), ReplSpanKind.Prompt)
                .InsertBeforeLastPrompt($"c{Environment.NewLine}", ReplSpanKind.Input)
                .InsertBeforeLastPrompt($"d{Environment.NewLine}e{Environment.NewLine}", ReplSpanKind.Output);

            InteractiveWindowTextSnapshot.AssertEqual(expected, actual);
        }

        [WpfFact]
        public async Task AddToHistory_HistoryNavigation() 
        {
            await Task.Run(() => Window.WriteLine("a"));
            Window.AddToHistory("b");
            await Task.Run(() => Window.WriteLine("c"));
            Window.AddToHistory("d");
            await Task.Run(() => Window.WriteLine("e"));
            Window.FlushOutput();
            Window.Operations.TypeChar('f');

            Assert.Equal("f", Window.CurrentLanguageBuffer.CurrentSnapshot.GetText());

            Window.Operations.HistoryPrevious();
            Assert.Equal("d", Window.CurrentLanguageBuffer.CurrentSnapshot.GetText());

            Window.Operations.HistoryPrevious();
            Assert.Equal("b", Window.CurrentLanguageBuffer.CurrentSnapshot.GetText());

            Window.Operations.HistoryNext();
            Assert.Equal("d", Window.CurrentLanguageBuffer.CurrentSnapshot.GetText());

            Window.Operations.HistoryNext();
            Assert.Equal("f", Window.CurrentLanguageBuffer.CurrentSnapshot.GetText());
        }

        [WpfFact]
        public void AddToHistory_HasInput()
        {
            var original = new InteractiveWindowTextSnapshot(_testHost);

            Window.Operations.TypeChar('a');
            Window.Operations.TypeChar('b');
            Window.AddToHistory("c");
            Window.Operations.TypeChar('d');
            Window.Operations.TypeChar('e');
            Window.AddToHistory("f");

            var actual = new InteractiveWindowTextSnapshot(_testHost);
            var expected = original
                .InsertBeforeLastPrompt(_testHost.Evaluator.GetPrompt(), ReplSpanKind.Prompt)
                .InsertBeforeLastPrompt($"c{Environment.NewLine}", ReplSpanKind.Input)
                .InsertBeforeLastPrompt(_testHost.Evaluator.GetPrompt(), ReplSpanKind.Prompt)
                .InsertBeforeLastPrompt($"f{Environment.NewLine}", ReplSpanKind.Input)
                .AddToTheEnd($"abde", ReplSpanKind.Input);

            InteractiveWindowTextSnapshot.AssertEqual(expected, actual);
        }


        [WorkItem(6397, "https://github.com/dotnet/roslyn/issues/6397")]
        [WpfFact]
        public void TypeCharWithUndoRedo()
        {
            Window.Operations.TypeChar('a');
            Window.Operations.TypeChar('b');
            Window.Operations.TypeChar('c');
            Assert.Equal("> abc", GetTextFromCurrentSnapshot());

            // undo/redo for consecutive TypeChar's shold be a single action

            ((InteractiveWindow)Window).Undo_TestOnly(1);
            Assert.Equal("> ", GetTextFromCurrentSnapshot());

            ((InteractiveWindow)Window).Redo_TestOnly(1);
            Assert.Equal("> abc", GetTextFromCurrentSnapshot());

            // make a stream selection as follows:
            // > |aaa|
            Window.Operations.SelectAll();
            Window.Operations.TypeChar('1');
            Window.Operations.TypeChar('2');
            Window.Operations.TypeChar('3');

            Assert.Equal("> 123", GetTextFromCurrentSnapshot());

            ((InteractiveWindow)Window).Undo_TestOnly(1);
            Assert.Equal("> abc", GetTextFromCurrentSnapshot());

            ((InteractiveWindow)Window).Undo_TestOnly(1);
            Assert.Equal("> ", GetTextFromCurrentSnapshot());

            // type in active prompt
            MoveCaretToPreviousPosition(2);
            Window.Operations.TypeChar('x');
            Window.Operations.TypeChar('y');
            Window.Operations.TypeChar('z');
            Assert.Equal("> xyz", GetTextFromCurrentSnapshot());
        }

        // TODO (https://github.com/dotnet/roslyn/issues/7976): delete this
        [WorkItem(7976, "https://github.com/dotnet/roslyn/issues/7976")]
        [WpfFact]
        public void Workaround7976()
        {
            Thread.Sleep(TimeSpan.FromSeconds(10));
        }

        private string GetTextFromCurrentSnapshot()
        {
            return Window.TextView.TextBuffer.CurrentSnapshot.GetText();
        }

        private async Task Submit(string submission, string output)
        {
            await TaskRun(() => Window.SubmitAsync(new[] { submission })).ConfigureAwait(true);
            // TestInteractiveEngine.ExecuteCodeAsync() simply returns
            // success rather than executing the submission, so add the
            // expected output to the output buffer.
            var buffer = Window.OutputBuffer;
            using (var edit = buffer.CreateEdit())
            {
                edit.Replace(buffer.CurrentSnapshot.Length, 0, output);
                edit.Apply();
            }
        }

        private struct TextChange
        {
            internal readonly int Start;
            internal readonly int Length;
            internal readonly string Text;

            internal TextChange(int start, int length, string text)
            {
                Start = start;
                Length = length;
                Text = text;
            }
        }

        private static ITextSnapshot ApplyChanges(ITextBuffer buffer, params TextChange[] changes)
        {
            using (var edit = buffer.CreateEdit())
            {
                foreach (var change in changes)
                {
                    edit.Replace(change.Start, change.Length, change.Text);
                }
                return edit.Apply();
            }
        }

        private class InteractiveWindowTextSnapshot 
        {
            private readonly ImmutableArray<SpanWithKind> _spans;
            private readonly string _fullText;

            public InteractiveWindowTextSnapshot(InteractiveWindowTestHost host)
            {
                var bufferFactory = host.ExportProvider.GetExportedValue<ITextBufferFactoryService>();
                var projectionSnapshot = ((IProjectionBuffer) host.Window.TextView.TextBuffer).CurrentSnapshot;

                _fullText = projectionSnapshot.GetText();
                _spans = projectionSnapshot.GetSourceSpans()
                    .SelectMany(ss => GetSpanWithKind(ss, projectionSnapshot, host.Window, bufferFactory.InertContentType))
                    .OrderBy(s => s.Start)
                    .ToImmutableArray();
            }

            private InteractiveWindowTextSnapshot(string fullText, ImmutableArray<SpanWithKind> spans) 
            {
                _fullText = fullText;
                _spans = spans;
            }

            public InteractiveWindowTextSnapshot AddToTheEnd(string text, ReplSpanKind kind) 
            {
                var startIndex = _fullText.Length;
                var fullText = _fullText + text;
                var spans = _spans.Add(new SpanWithKind(startIndex, text.Length, kind));

                return new InteractiveWindowTextSnapshot(fullText, spans);
            }

            public InteractiveWindowTextSnapshot InsertBeforeLastPrompt(string text, ReplSpanKind kind) 
            {
                var startIndex = _spans.LastOrDefault(s => s.Kind == ReplSpanKind.Prompt).Start;
                return Insert(text, startIndex, kind);
            }

            private InteractiveWindowTextSnapshot Insert(string text, int startIndex, ReplSpanKind kind) 
            {
                var length = text.Length;
                var spansBuilder = ImmutableArray.CreateBuilder<SpanWithKind>(_spans.Length + 1);

                var fullText = _fullText.Insert(startIndex, text);
                foreach (var span in _spans) 
                {
                    if (span.Start < startIndex) 
                    {
                        spansBuilder.Add(span);
                    }
                    else if (span.Start == startIndex) 
                    {
                        spansBuilder.Add(new SpanWithKind(startIndex, length, kind));
                        spansBuilder.Add(new SpanWithKind(span.Start + length, span.Length, span.Kind));
                    }
                    else 
                    {
                        spansBuilder.Add(new SpanWithKind(span.Start + length, span.Length, span.Kind));
                    }
                }

                return new InteractiveWindowTextSnapshot(fullText, spansBuilder.ToImmutable());
            }

            public static void AssertEqual(InteractiveWindowTextSnapshot expected, InteractiveWindowTextSnapshot actual) 
            {
                Assert.Equal(expected._fullText, actual._fullText);
                Assert.Equal<SpanWithKind>(expected._spans, actual._spans);
            }

            private static IEnumerable<SpanWithKind> GetSpanWithKind(SnapshotSpan snapshotSpan, IProjectionSnapshot projectionSnapshot, IInteractiveWindow window, IContentType inertContentType) 
            {
                var spans = projectionSnapshot.MapFromSourceSnapshot(snapshotSpan);
                var spanKind = GetSpanKind(snapshotSpan, window, inertContentType);
                return spans.Where(s => s.Length > 0).Select(span => new SpanWithKind(span.Start, span.Length, spanKind));
            }

            private static ReplSpanKind GetSpanKind(SnapshotSpan span, IInteractiveWindow window, IContentType inertContentType) 
            {
                var textBuffer = span.Snapshot.TextBuffer;

                if (textBuffer == window.OutputBuffer) 
                {
                    return ReplSpanKind.Output;
                }

                if (textBuffer.ContentType == inertContentType) 
                {
                    return ReplSpanKind.Prompt;
                }

                return ReplSpanKind.Input;
            }

            [DebuggerDisplay("{Start}, {Length}, {Kind}")]
            private struct SpanWithKind : IEquatable<SpanWithKind> 
            {
                public int Start { get; }
                public int Length { get; }
                public ReplSpanKind Kind { get; }

                public SpanWithKind(int start, int length, ReplSpanKind kind) 
                {
                    Start = start;
                    Length = length;
                    Kind = kind;
                }

                public bool Equals(SpanWithKind other) 
                {
                    return Start == other.Start && Length == other.Length && Kind == other.Kind;
                }

                public override bool Equals(object obj) 
                {
                    return obj is SpanWithKind && Equals((SpanWithKind)obj);
                }

                public override int GetHashCode() 
                {
                    unchecked 
                    {
                        var hashCode = Start;
                        hashCode = (hashCode*397) ^ Length;
                        hashCode = (hashCode*397) ^ (int) Kind;
                        return hashCode;
                    }
                }
            }
        }
    }

    internal static class WindowExtensions 
    {
        internal static void AddToHistory(this IInteractiveWindow window, string input)
        {
            ((IInteractiveWindow2)window).AddToHistory(input);
        }
    }

    internal static class OperationsExtensions
    {
        internal static void Copy(this IInteractiveWindowOperations operations)
        {
            ((IInteractiveWindowOperations2)operations).Copy();
        }

        internal static void CopyCode(this IInteractiveWindowOperations operations)
        {
            ((IInteractiveWindowOperations2)operations).CopyCode();
        }

        internal static void DeleteLine(this IInteractiveWindowOperations operations)
        {
            ((IInteractiveWindowOperations2)operations).DeleteLine();
        }

        internal static void CutLine(this IInteractiveWindowOperations operations)
        {
            ((IInteractiveWindowOperations2)operations).CutLine();
        }

        internal static void TypeChar(this IInteractiveWindowOperations operations, char typedChar)
        {
            ((IInteractiveWindowOperations2)operations).TypeChar(typedChar);
        }
    }
}
