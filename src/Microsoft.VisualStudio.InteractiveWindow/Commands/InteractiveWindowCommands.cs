﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the License.txt file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Language.StandardClassification;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.InteractiveWindow.Commands
{
    internal sealed class Commands : IInteractiveWindowCommands
    {
        private const string _commandSeparator = ", ";

        private readonly Dictionary<string, IInteractiveWindowCommand> _commands;
        private readonly int _maxCommandNameLength;
        private readonly IInteractiveWindow _window;
        private readonly IContentType _commandContentType;
        private readonly IStandardClassificationService _classificationRegistry;
        private IContentType _languageContentType;
        private ITextBuffer _previousBuffer;

        public string CommandPrefix { get; set; }

        public bool InCommand
        {
            get
            {
                return _window.CurrentLanguageBuffer.ContentType == _commandContentType;
            }
        }

        internal Commands(IInteractiveWindow window, string prefix, IEnumerable<IInteractiveWindowCommand> commands, IContentTypeRegistryService contentTypeRegistry = null, IStandardClassificationService classificationRegistry = null)
        {
            CommandPrefix = prefix;
            _window = window;

            Dictionary<string, IInteractiveWindowCommand> commandsDict = new Dictionary<string, IInteractiveWindowCommand>();
            foreach (var command in commands)
            {
                int length = 0;
                foreach (var name in command.Names)
                {
                    if (commandsDict.ContainsKey(name))
                    {
                        throw new InvalidOperationException(string.Format(InteractiveWindowResources.DuplicateCommand, string.Join(_commandSeparator, command.Names)));
                    }
                    if (length != 0)
                    {
                        length += _commandSeparator.Length;
                    }
                    // plus the length of `#` for display purpose
                    length += name.Length + 1;

                    commandsDict[name] = command;
                }
                if (length == 0)
                {
                    throw new InvalidOperationException(string.Format(InteractiveWindowResources.MissingCommandName, command.GetType().Name));
                }
                _maxCommandNameLength = Math.Max(_maxCommandNameLength, length);
            }

            _commands = commandsDict;

            _classificationRegistry = classificationRegistry;

            if (contentTypeRegistry != null)
            {
                _commandContentType = contentTypeRegistry.GetContentType(PredefinedInteractiveCommandsContentTypes.InteractiveCommandContentTypeName);
            }

            if (window != null)
            {
                window.SubmissionBufferAdded += Window_SubmissionBufferAdded;
                window.Properties[typeof(IInteractiveWindowCommands)] = this;
            }
        }

        private void Window_SubmissionBufferAdded(object sender, SubmissionBufferAddedEventArgs e)
        {
            if (_previousBuffer != null)
            {
                _previousBuffer.Changed -= NewBufferChanged;
            }

            _languageContentType = e.NewBuffer.ContentType;
            e.NewBuffer.Changed += NewBufferChanged;
            _previousBuffer = e.NewBuffer;
        }

        private void NewBufferChanged(object sender, TextContentChangedEventArgs e)
        {
            bool isCommand = IsCommand(e.After.GetExtent());

            ITextBuffer buffer = e.After.TextBuffer;
            IContentType contentType = buffer.ContentType;
            IContentType newContentType = null;

            if (contentType == _languageContentType)
            {
                if (isCommand)
                {
                    newContentType = _commandContentType;
                }
            }
            else
            {
                if (!isCommand)
                {
                    newContentType = _languageContentType;
                }
            }

            if (newContentType != null)
            {
                buffer.ChangeContentType(newContentType, editTag: null);
            }
        }

        internal bool IsCommand(SnapshotSpan span)
        {
            SnapshotSpan prefixSpan, commandSpan, argumentsSpan;
            return TryParseCommand(span, out prefixSpan, out commandSpan, out argumentsSpan) != null;
        }

        internal IInteractiveWindowCommand TryParseCommand(SnapshotSpan span, out SnapshotSpan prefixSpan, out SnapshotSpan commandSpan, out SnapshotSpan argumentsSpan)
        {
            string prefix = CommandPrefix;

            SnapshotSpan trimmed = span.TrimStart();
            if (!trimmed.StartsWith(prefix))
            {
                prefixSpan = commandSpan = argumentsSpan = default(SnapshotSpan);
                return null;
            }

            prefixSpan = trimmed.SubSpan(0, prefix.Length);
            var nameAndArgs = trimmed.SubSpan(prefix.Length).TrimStart();
            SnapshotPoint nameEnd = nameAndArgs.IndexOfAnyWhiteSpace() ?? span.End;
            commandSpan = new SnapshotSpan(span.Snapshot, Span.FromBounds(nameAndArgs.Start.Position, nameEnd.Position));

            argumentsSpan = new SnapshotSpan(span.Snapshot, Span.FromBounds(nameEnd.Position, span.End.Position)).Trim();

            return this[commandSpan.GetText()];
        }

        public IInteractiveWindowCommand this[string name]
        {
            get
            {
                IInteractiveWindowCommand command;
                _commands.TryGetValue(name, out command);
                return command;
            }
        }

        public IEnumerable<IInteractiveWindowCommand> GetCommands()
        {
            return _commands.Values;
        }

        internal IEnumerable<string> Help()
        {
            // The magic number `19` here is calculated based on how the description is dispayed for other help entries to keep the texts aligned
            // (As of now other help entries include REPL commands and script derectives, both have manually formatted help description strings.
            string format = "{0,-" + Math.Max(19, _maxCommandNameLength) + "}  {1}";
            return _commands.GroupBy(entry => entry.Value).
                Select(group => string.Format(format, string.Join(_commandSeparator, group.Key.Names.Select(s => CommandPrefix + s)), group.Key.Description)).
                OrderBy(line => line);
        }

        public IEnumerable<ClassificationSpan> Classify(SnapshotSpan span)
        {
            SnapshotSpan prefixSpan, commandSpan, argumentsSpan;
            var command = TryParseCommand(span.Snapshot.GetExtent(), out prefixSpan, out commandSpan, out argumentsSpan);
            if (command == null)
            {
                yield break;
            }

            if (span.OverlapsWith(prefixSpan))
            {
                yield return Classification(span.Snapshot, prefixSpan, _classificationRegistry.Keyword);
            }

            if (span.OverlapsWith(commandSpan))
            {
                yield return Classification(span.Snapshot, commandSpan, _classificationRegistry.Keyword);
            }

            if (argumentsSpan.Length > 0)
            {
                foreach (var classifiedSpan in command.ClassifyArguments(span.Snapshot, argumentsSpan.Span, span.Span))
                {
                    yield return classifiedSpan;
                }
            }
        }

        private ClassificationSpan Classification(ITextSnapshot snapshot, Span span, IClassificationType classificationType)
        {
            return new ClassificationSpan(new SnapshotSpan(snapshot, span), classificationType);
        }

        /// <returns>
        /// Null if parsing fails, the result of execution otherwise.
        /// </returns>
        public Task<ExecutionResult> TryExecuteCommand()
        {
            var span = _window.CurrentLanguageBuffer.CurrentSnapshot.GetExtent();

            SnapshotSpan prefixSpan, commandSpan, argumentsSpan;
            var command = TryParseCommand(span, out prefixSpan, out commandSpan, out argumentsSpan);
            if (command == null)
            {
                return null;
            }

            return ExecuteCommandAsync(command, argumentsSpan.GetText());
        }

        private async Task<ExecutionResult> ExecuteCommandAsync(IInteractiveWindowCommand command, string arguments)
        {
            try
            {
                return await command.Execute(_window, arguments).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                _window.ErrorOutputWriter.WriteLine(InteractiveWindowResources.CommandFailed, command.Names.First(), e.Message);
                return ExecutionResult.Failure;
            }
        }

        private const string HelpIndent = "  ";

        private static readonly string[] s_CSVBScriptDirectives = new[]
        {
            "#r                   " + InteractiveWindowResources.RefHelp,
            "#load                " + InteractiveWindowResources.LoadHelp
        };

        private static readonly string[] s_shortcutDescriptions = new[]
        {
            "Enter                " + InteractiveWindowResources.EnterHelp,
            "Ctrl-Enter           " + InteractiveWindowResources.CtrlEnterHelp1,
            "                     " + InteractiveWindowResources.CtrlEnterHelp2,
            "Shift-Enter          " + InteractiveWindowResources.ShiftEnterHelp,
            "Escape               " + InteractiveWindowResources.EscapeHelp,
            "Alt-UpArrow          " + InteractiveWindowResources.AltUpArrowHelp,
            "Alt-DownArrow        " + InteractiveWindowResources.AltDownArrowHelp,
            "Ctrl-Alt-UpArrow     " + InteractiveWindowResources.CtrlAltUpArrowHelp,
            "Ctrl-Alt-DownArrow   " + InteractiveWindowResources.CtrlAltDownArrowHelp,
            "Ctrl-K, Ctrl-Enter   " + InteractiveWindowResources.CtrlKCtrlEnterHelp,
            "Ctrl-E, Ctrl-Enter   " + InteractiveWindowResources.CtrlECtrlEnterHelp,
            "Ctrl-A               " + InteractiveWindowResources.CtrlAHelp
        };

        private static readonly string[] s_shortcutDescriptionsSmartUpDown = new[]
        {
            "UpArrow              " + InteractiveWindowResources.UpArrowHelp1,
            "                     " + InteractiveWindowResources.UpArrowHelp2,
            "DownArrow            " + InteractiveWindowResources.DownArrowHelp1,
            "                     " + InteractiveWindowResources.DownArrowHelp2
        };


        internal string ShortcutDescriptions
        {
            get
            {
                var sb = new StringBuilder();
                foreach (var line in s_shortcutDescriptions)
                {
                    sb.Append(HelpIndent + line + "\r\n");
                }
                if (UseSmartUpDown)
                {
                    foreach (var line in s_shortcutDescriptionsSmartUpDown)
                    {
                        sb.Append(HelpIndent + line + "\r\n");
                    }
                }
                return sb.ToString();
            }
        }

        public void DisplayHelp()
        {
            _window.WriteLine(InteractiveWindowResources.KeyboardShortcuts);
            _window.Write(ShortcutDescriptions);

            _window.WriteLine(InteractiveWindowResources.ReplCommands);
            foreach (var line in Help())
            {
                _window.Write(HelpIndent);
                _window.WriteLine(line);
            }

            // Hack: Display script directives only in CS/VB interactive window
            // TODO: https://github.com/dotnet/roslyn/issues/6441
            var evaluatorTypeName = _window.Evaluator.GetType().Name;
            if (evaluatorTypeName == "CSharpInteractiveEvaluator" ||
                evaluatorTypeName == "VisualBasicInteractiveEvaluator")
            {
                _window.WriteLine(InteractiveWindowResources.CSVBScriptDirectives);
                foreach (var line in s_CSVBScriptDirectives)
                {
                    _window.Write(HelpIndent);
                    _window.WriteLine(line);
                }
            }
        }

        public void DisplayCommandUsage(IInteractiveWindowCommand command, TextWriter writer, bool displayDetails)
        {
            if (displayDetails)
            {
                writer.WriteLine(command.Description);
                writer.WriteLine(string.Empty);
            }

            writer.WriteLine(InteractiveWindowResources.Usage);
            writer.Write(HelpIndent);
            writer.Write(CommandPrefix);
            writer.Write(string.Join(_commandSeparator + CommandPrefix, command.Names));

            string commandLine = command.CommandLine;
            if (commandLine != null)
            {
                writer.Write(" ");
                writer.Write(commandLine);
            }

            if (displayDetails)
            {
                writer.WriteLine(string.Empty);

                var paramsDesc = command.ParametersDescription;
                if (paramsDesc != null && paramsDesc.Any())
                {
                    writer.WriteLine(string.Empty);
                    writer.WriteLine(InteractiveWindowResources.Parameters);

                    int maxParamNameLength = paramsDesc.Max(entry => entry.Key.Length);
                    string paramHelpLineFormat = HelpIndent + "{0,-" + maxParamNameLength + "}  {1}";

                    foreach (var paramDesc in paramsDesc)
                    {
                        writer.WriteLine(string.Format(paramHelpLineFormat, paramDesc.Key, paramDesc.Value));
                    }
                }

                IEnumerable<string> details = command.DetailedDescription;
                if (details != null && details.Any())
                {
                    writer.WriteLine(string.Empty);
                    foreach (var line in details)
                    {
                        writer.WriteLine(line);
                    }
                }
            }
        }

        public void DisplayCommandHelp(IInteractiveWindowCommand command)
        {
            DisplayCommandUsage(command, _window.OutputWriter, displayDetails: true);
        }

        private bool UseSmartUpDown => _window.TextView.Options.GetOptionValue(InteractiveWindowOptions.SmartUpDown);
    }
}
