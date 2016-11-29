// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.VisualStudio.InteractiveWindow.Shell
{
    public interface IVsInteractiveWindowFactory2: IVsInteractiveWindowFactory 
    {
        IVsInteractiveWindow Create(VsInteractiveWindowCreationParameters creationInfo);
    }
}
