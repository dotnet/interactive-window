// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.InteractiveWindow.Shell 
{
    /// <summary>
    /// Provides creation parameters to the interactive window factory.
    /// </summary>
    public class VsInteractiveWindowCreationParameters 
    {
        /// <summary>
        /// Unique identifier of the interactive window client.
        /// </summary>
        public Guid ProviderId { get; }

        /// <summary>
        /// Tool window instance id.
        /// </summary>
        public int InstanceId { get; }

        /// <summary>
        /// Tool window title
        /// </summary>
        public string Title { get; }

        /// <summary>
        /// Interactive window evaluator
        /// </summary>
        public IInteractiveEvaluator Evaluator { get; }

        /// <summary>
        /// Tool window creation flags that will be passed down to 
        /// <see cref="IVsToolWindowToolbarHost.AddToolbar(VSTWT_LOCATION, ref Guid, uint)"/>
        /// <seealso cref="VsInteractiveWindowCreationParameters.ToolbarCommandTarget"/>
        /// </summary>
        public __VSCREATETOOLWIN CreationFlags { get; set; }

        /// <summary>
        /// Custom toolbar command set. If provided, the default interactive window toolbar 
        /// will not be used and instead interactive window will attemtsto instantiate
        /// the provided toolbar by its command set and id. The toolbar must be specified
        /// in VSCT file.
        /// </summary>
        public Guid ToolbarCommandSet { get; set; }

        /// <summary>
        /// Custom toolbar id. <see cref="VsInteractiveWindowCreationParameters.ToolbarCommandSet"/>
        /// </summary>
        public uint ToolbarId { get; set; }

        /// <summary>
        /// Toolbar command target. If supplied, the interactive window will call 
        /// <see cref="IVsToolWindowToolbarHost3.AddToolbar3(VSTWT_LOCATION, ref Guid, uint, IDropTarget, IOleCommandTarget)"/>
        /// instead of the defailt <see cref="IVsToolWindowToolbarHost.AddToolbar(VSTWT_LOCATION, ref Guid, uint)"/>
        /// </summary>
        public IOleCommandTarget ToolbarCommandTarget { get; set; }

        public VsInteractiveWindowCreationParameters(Guid providerId, int instanceId, string title, IInteractiveEvaluator evaluator) 
        {
            if (providerId == Guid.Empty) 
            {
                throw new ArgumentException(nameof(providerId));
            }

            ProviderId = providerId;
            InstanceId = instanceId;
            Title = title;
            Evaluator = evaluator ?? throw new ArgumentNullException(nameof(evaluator));
        }
    }
}
