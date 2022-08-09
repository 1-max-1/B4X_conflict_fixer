using System.Linq;
using System.Windows;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace B4X_conflict_fixer {
	/// <summary>
	/// Interaction logic for BuildSelectionDialog.xaml
	/// </summary>
	public partial class BuildSelectionDialog : Window {
		public HashSet<string> FinalBuildConfigs { get; private set; }

		public BuildSelectionDialog(HashSet<string> builds) {
			InitializeComponent();

			foreach (string build in builds) {
				((BuildConflictViewModel)DataContext).ListItems.Add(new BuildConflictListItem(build));
			}
			buildConfigListView.SelectedIndex = 0;
		}

		// Called by OK button
		private void Done(object sender, RoutedEventArgs e) {
			// Make sure all build configs are valid before exiting the dialog
			var selectedBuilds = ((BuildConflictViewModel)DataContext).ListItems.Where(b => b.Selected);
			foreach (BuildConflictListItem build in selectedBuilds) {
				if (!IsBuildConfigValid(build, out string whyIsBuildInvalid)) {
					InfoMessageBox.Show(this, $"Build {build.BuildName} is invalid:\n{whyIsBuildInvalid}", "Error", "Oops");
					return;
				}
			}

			FinalBuildConfigs = new HashSet<string>(selectedBuilds.ToList().ConvertAll(b => b.ToB4XStringFormat()));
			DialogResult = true;
			Close();
		}

		// Called by "Add new build" button
		private void AddNewBuildConfig(object sender, RoutedEventArgs e) {
			// B4X always creates new build names in the format New_X so we should do the same.
			// However there could already be a build called New_<number here> so we need to find the highest number then +1.
			string newBuildName = "New_";
			List<string> existingEmptyBuilds = ((BuildConflictViewModel)DataContext).ListItems.ToList().ConvertAll(b => b.BuildName).FindAll(name => Regex.IsMatch(name, "New_\\d+"));
			if (existingEmptyBuilds.Count > 0) {
				existingEmptyBuilds.Sort();
				newBuildName += int.Parse(existingEmptyBuilds.Last()[4..]) + 1;
			}
			else {
				newBuildName += "1";
			}

			// Clone the currently selected build - this is what B4X does
			BuildConflictListItem newBuild = ((BuildConflictListItem)buildConfigListView.SelectedItem).MakeNewUsingMeAsBase(newBuildName);
			((BuildConflictViewModel)DataContext).ListItems.Add(newBuild);
			buildConfigListView.SelectedIndex = buildConfigListView.Items.Count - 1;
		}

		private bool IsBuildConfigValid(BuildConflictListItem build, out string reasonForInvalidity) {
			bool c1 = Regex.IsMatch(build.BuildName, "^[a-zA-Z0-9_]+$"); // Alphanumeric and underscore
			// No duplicate names
			bool c2 = ((BuildConflictViewModel)DataContext).ListItems.Count(b => b.BuildName == build.BuildName) == 1;
			// Alphanumeric and underscore, must be at least 2 components seperated by a dot, start of package names cannot be numeric or underscore.
			bool c3 = Regex.IsMatch(build.PackageName, "^[a-zA-Z][a-zA-Z0-9_]*(?:\\.[a-zA-Z][a-zA-Z0-9_]*)+$");
			// Must only contain alphanumeric, underscore, comma and space. Whitespace cannot occur in the middle of a build symbol.
			bool c4 = !Regex.IsMatch(build.ConditionalSymbols, "[^0-9a-zA-Z_, ]|(?:[0-9a-zA-Z_]+ +[0-9a-zA-Z_]+)");

			if (!c1) reasonForInvalidity = "Invalid build name.";
			else if (!c2) reasonForInvalidity = "Duplicate build name.";
			else if (!c3) reasonForInvalidity = "Invalid package name.";
			else reasonForInvalidity = "Invalid conditional symbols.";
			return c1 && c2 && c3 && c4;
		}
	}
}
