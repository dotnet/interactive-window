// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.InteractiveWindow.Shell
{
    public interface IVsInteractiveWindowFactory2: IVsInteractiveWindowFactory 
    {
        IVsInteractiveWindow Create(Guid providerId, int instanceId, string title, IInteractiveEvaluator evaluator, IVsInteractiveWindowDecorator decorator, __VSCREATETOOLWIN creationFlags = 0);
    }
}
