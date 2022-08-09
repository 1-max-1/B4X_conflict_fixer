using System.Collections.ObjectModel;

namespace B4X_conflict_fixer {
	/// <summary>
	/// View model for the BuildSelectionDialog page
	/// </summary>
	public class BuildConflictViewModel {

		// Need observable collection for list view to update
		public ObservableCollection<BuildConflictListItem> ListItems { get; }

		public BuildConflictViewModel() {
			ListItems = new ObservableCollection<BuildConflictListItem>();
		}
	}
}
