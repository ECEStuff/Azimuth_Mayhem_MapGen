using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Azimuth
{
    internal sealed class GenerationProgressDialog : Form
    {
        private readonly CancellationTokenSource cancellation = new CancellationTokenSource();
        private readonly Action<CancellationToken, Action<string>> work;
        private readonly Label status;
        private readonly Button cancel;
        private bool finished;
        internal Exception Failure { get; private set; }
        internal GenerationProgressDialog(Action<CancellationToken, Action<string>> work)
        {
            this.work = work;
            Text = X3Utils.Ui("Generator.ProgressTitle");
            Width = 520; Height = 180; FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MinimizeBox = MaximizeBox = false;
            status = new Label { Left = 12, Top = 15, Width = 480, Height = 45, Text = X3Utils.Ui("Generator.Searching") };
            var progress = new ProgressBar { Left = 12, Top = 65, Width = 480, Style = ProgressBarStyle.Marquee };
            cancel = new Button { Left = 410, Top = 98, Text = X3Utils.Ui("Edit.Cancel") };
            cancel.Click += (s, e) => CancelSearch();
            Controls.AddRange(new Control[] { status, progress, cancel });
        }
        protected override async void OnShown(EventArgs e)
        {
            base.OnShown(e);
            var progress = new Progress<string>(message => { if (!finished && !IsDisposed) status.Text = message; });
            DialogResult result;
            try
            {
                await Task.Run(() => work(cancellation.Token, message => ((IProgress<string>)progress).Report(message)));
                cancellation.Token.ThrowIfCancellationRequested();
                result = DialogResult.OK;
            }
            catch (OperationCanceledException) { result = DialogResult.Cancel; }
            catch (Exception ex) { Failure = ex; result = DialogResult.Abort; }
            finished = true; DialogResult = result; Close();
        }
        private void CancelSearch()
        {
            cancellation.Cancel(); cancel.Enabled = false; status.Text = X3Utils.Ui("Generator.Cancelling");
        }
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (!finished) { CancelSearch(); e.Cancel = true; }
            base.OnFormClosing(e);
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing) cancellation.Dispose();
            base.Dispose(disposing);
        }
    }
}
