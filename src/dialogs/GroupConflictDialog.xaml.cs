using System.Windows;
using System.Collections.Generic;

namespace B4X_conflict_fixer {
	/// <summary>
	/// A dialog to help the user solve conflicts with module and file groups.
	/// </summary>
	public partial class GroupConflictDialog : Window {
		public enum GroupTypes {
			TYPE_MODULE_GROUP,
			TYPE_FILE_GROUP
		}

		/// <summary>
		/// Once the user clicks OK, this will contain the group they selected
		/// </summary>
		public string selectedGroup;

		/// <summary>
		/// Initialize a new group conflict window with the specified group list, and the specified group type.
		/// </summary>
		/// <param name="groups">The group names to be displayed in the list</param>
		/// <param name="groupTypes">The type of group conflict - module or file</param>
		/// <param name="filename">If group type is set to file, pass the name of the conflicted file</param>
		public GroupConflictDialog(HashSet<string> groups, GroupTypes groupTypes, string filename = null) {
			InitializeComponent();

			if (groupTypes == GroupTypes.TYPE_FILE_GROUP) {
				lblTitle.Text = $"The file group that '{filename}' belongs to is in conflict!\nPlease select which file group you would like to assign '{filename}' to:";
				Title = $"File Group conflict for file '{filename}'";
			}
			else {
				lblTitle.Text = "The module group that 'Main' belongs to is in conflict!\nPlease select which group you would like to assign the 'Main' module to:";
				Title = "Module Group conflict";
			}

			foreach (string group in groups) {
				((GroupConflictViewModel)DataContext).ListItems.Add(new GroupConflictListItem(group, false));
			}
			((GroupConflictViewModel)DataContext).ListItems[0].Selected = true;
		}

		private void TxtCustomGroup_GotFocus(object sender, RoutedEventArgs e) {
			rbCustomGroup.IsChecked = true;
		}

		// Called by OK button
		private void SaveGroupSelection(object sender, RoutedEventArgs e) {
			if ((rbCustomGroup.IsChecked ?? false) && txtCustomGroup.Text.Trim() == "") {
				InfoMessageBox.Show(this, "Your custom group is not valid!", "Error");
				return;
			}
			else if(rbCustomGroup.IsChecked ?? false) {
				selectedGroup = txtCustomGroup.Text.Trim();
			}
			else {
				selectedGroup = ((GroupConflictViewModel)DataContext).GetSelectedGroup();
			}

			DialogResult = true;
			Close();
		}
	}
}
