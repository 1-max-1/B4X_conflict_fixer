using System.Windows;

namespace B4X_conflict_fixer {
	/// <summary>
	/// Like a regular System.Windows.MessageBox but it looks better and the button text is customizable.
	/// </summary>
	public partial class InfoMessageBox : Window {
		public InfoMessageBox(string content, string title, string buttonText) {
			InitializeComponent();

			Title = title;
			lblContent.Text = content;
			btnOK.Content = buttonText;
		}

		public static void Show(Window owner, string content, string title, string buttonText = "OK") {
			new InfoMessageBox(content, title, buttonText) { Owner = owner }.ShowDialog();
		}

		private void CloseMessageBox(object sender, RoutedEventArgs e) {
			Close();
		}
	}
}
