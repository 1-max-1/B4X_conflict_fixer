using System.Collections.Generic;

namespace B4X_conflict_fixer {
	/// <summary>
	/// View Model for the group conflict window data
	/// </summary>
	public class GroupConflictViewModel {
		public List<GroupConflictListItem> ListItems { get; }

		public GroupConflictViewModel() {
			ListItems = new List<GroupConflictListItem>();
		}

		public string GetSelectedGroup() {
			return ListItems.Find(li => li.Selected == true).Text;
		}
	}
}
