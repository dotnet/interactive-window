// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the License.txt file in the project root for more information.

namespace Microsoft.VisualStudio.InteractiveWindow.Shell
{
    internal enum CommandIds : uint
    {
        // TODO (crwilcox): should all of these be in the editoroperations?
        SmartExecute = 0x103,
        AbortExecution = 0x104,
        Reset = 0x105,
        HistoryNext = 0x0106,
        HistoryPrevious = 0x0107,
        ClearScreen = 0x0108,
        BreakLine = 0x0109,
        SearchHistoryNext = 0x010A,
        SearchHistoryPrevious = 0x010B,
        ExecuteInInteractiveWindow = 0x010C,
        CopyToInteractiveWindow = 0x010D,
        CopyCode = 0x010E,
    }
}
