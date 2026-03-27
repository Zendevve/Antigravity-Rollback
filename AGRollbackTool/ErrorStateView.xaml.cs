using System;
using System.Windows;
using System.Windows.Controls;

namespace AGRollbackTool
{
    /// <summary>
    /// Interaction logic for ErrorStateView.xaml
    /// </summary>
    public partial class ErrorStateView : UserControl
    {
        public ErrorStateView()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Sets the error information to display
        /// </summary>
        /// <param name="errorMessage">User-friendly error message</param>
        /// <param name="exceptionMessage">Technical exception message</param>
        /// <param name="stackTrace">Stack trace string (can be null or empty)</param>
        public void SetError(string errorMessage, string exceptionMessage, string stackTrace = null)
        {
            ErrorMessageTextBlock.Text = errorMessage ?? "An error occurred";
            ErrorDetailTextBlock.Text = exceptionMessage ?? "No exception details available";
            ExceptionMessageTextBlock.Text = exceptionMessage ?? "No exception message";
            StackTraceTextBox.Text = string.IsNullOrWhiteSpace(stackTrace) ? "No stack trace available" : stackTrace;
        }

        /// <summary>
        /// Event for when the retry button is clicked
        /// </summary>
        public event EventHandler RetryClicked;

        /// <summary>
        /// Event for when the cancel button is clicked
        /// </summary>
        public event EventHandler CancelClicked;

        private void RetryButton_Click(object sender, RoutedEventArgs e)
        {
            RetryClicked?.Invoke(this, EventArgs.Empty);
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            CancelClicked?.Invoke(this, EventArgs.Empty);
        }
    }
}
