// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the License.txt file in the project root for more information.

#if TODO
using System.ComponentModel.Composition;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace Microsoft.VisualStudio.InteractiveWindow {
    [Export(typeof(IInteractiveWindowCommand))]
    internal sealed class CancelExecutionCommand : InteractiveWindowCommand {
        public override Task<ExecutionResult> Execute(IInteractiveWindow window, string arguments) {
            window.AbortCommand();
            return ExecutionResult.Succeeded;
        }

        public override string Description {
            get { return "Stops execution of the current command."; }
        }

        public override object ButtonContent {
            get {
                var image = new BitmapImage();
                image.BeginInit();
                image.StreamSource = Assembly.GetExecutingAssembly().GetManifestResourceStream("Microsoft.VisualStudio.Resources.CancelEvaluation.gif");
                image.EndInit();
                var res = new Image();
                res.Source = image;
                res.Width = res.Height = 16;
                return res;
            }
        }
    }
}
#endif
