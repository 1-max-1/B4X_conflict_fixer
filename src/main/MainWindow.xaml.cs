using System.IO;
using System.Text.RegularExpressions;
using System.Windows;

namespace B4X_conflict_fixer {
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window {
		public static MainWindow CurrentInstance { get; private set; }

		public MainWindow() {
			InitializeComponent();
			CurrentInstance = this;
		}

		private void BtnBrowse_Click(object sender, RoutedEventArgs e) {
			var dialog = new Microsoft.Win32.OpenFileDialog {
				Title = "Select B4X project file",
				Filter = "B4A project files (*.b4a)|*.b4a|B4J project files (*.b4j)|*.b4j"
			};

			bool? userSelectedFile = dialog.ShowDialog();
			if (userSelectedFile == true) {
				txtProjectFileLocation.Text = dialog.FileName;
			}
		}

		private async void BtnFixConflict_Click(object sender, RoutedEventArgs e) {
			string projectFile = txtProjectFileLocation.Text.Trim();
			if (projectFile == "" || !File.Exists(projectFile) || !Regex.Match(projectFile, "\\.b4a|\\.b4j").Success) {
				InfoMessageBox.Show(this, "Please select a valid B4X project file!", "Error", "Oops");
			}
			else {
				InfoMessageBox.Show(this, "To prevent further conflicts, please save and close all open B4X programs related to this project before continuing.", "Note", "Continue");

				btnFixConflict.IsEnabled = false;
				btnFixConflict.Content = "Fixing...";

				B4XProjectFile conflictFixer = new B4XProjectFile();
				bool allConflictsFixed = await conflictFixer.LoadAndFixConflicts(projectFile.Replace('/', '\\'));

				btnFixConflict.IsEnabled = true;
				btnFixConflict.Content = "Fix conflict";

				// If user cancelled don't show message
				if (allConflictsFixed)
					InfoMessageBox.Show(this, "File header conflicts have been fixed.\nYou will need to fix any code conflicts manually.", "Success!", "Done");
			}
		}
	}
}
