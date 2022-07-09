using System;
using System.IO;
using System.Windows;
using System.Diagnostics;

namespace B4X_conflict_fixer {
	/// <summary>
	/// A dialog that assists the user with fixing manifest conflicts
	/// </summary>
	public partial class ManifestConflictDialog : Window {
		private readonly string[] externalEditorStartCommands = new string[] { @"C:\Program Files (x86)\Notepad++\notepad++.exe", "notepad", "cmd" };

		// The file that contains the temporary git-merged manifest file
		private readonly string mTempManifestFile;

		/// <summary>
		/// Once the user fixes all conflicts and presses OK, this will be set to the content of the resulting manifest
		/// </summary>
		public string manifestContent;

		public ManifestConflictDialog(string oldContent, string newContent, string mergedManifestFile) {
			InitializeComponent();

			DiffView.OldText = oldContent;
			DiffView.NewText = newContent;
			mTempManifestFile = mergedManifestFile;
		}

		private void Window_Loaded(object sender, RoutedEventArgs e) {
			InfoMessageBox.Show(this, "Your manifest has un-mergeable conflicts. You will need to fix these manually. Use the controls below.", "Conflict");
		}

		// Opens the temporary manifest file with the users selected external editor
		private void OpenManifest(object sender, RoutedEventArgs e) {
			string startCommand = externalEditorStartCommands[externalEditor.SelectedIndex];
			// With Visual Studio code we need to use 'cmd /C code' instead of 'code' - otherwise Process.Start doesn't like it
			string args = (startCommand == "cmd" ? "/C code " : "") + mTempManifestFile;
			ProcessStartInfo pi = new ProcessStartInfo { FileName = startCommand, Arguments = args, CreateNoWindow = true};

			try {
				Process.Start(pi);
			}
			catch (Exception) {
				InfoMessageBox.Show(this, "Failed to start " + externalEditor.SelectedValue, "Error");
			}
		}

		private void Done(object sender, RoutedEventArgs e) {
			string tempManifestContent = File.ReadAllText(mTempManifestFile);
			if (tempManifestContent.Contains($"<<<<<<< {mTempManifestFile}")) {
				InfoMessageBox.Show(this, "Not all conflicts have been fixed!", "Error", "Oops");
			}
			else {
				DialogResult = true;
				manifestContent = tempManifestContent;
				Close();
			}
		}
	}
}
