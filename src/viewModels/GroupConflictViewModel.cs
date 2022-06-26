using System.Collections.Generic;

namespace B4X_conflict_fixer {
	/// <summary>
	/// View Model for the group conflict window data
	/// </summary>
	public class GroupConflictViewModel {
		public class ListItem {
			public ListItem(string text) {
				Selected = false;
				Text = text;
			}

			public bool Selected { get; set; }
			public string Text { get; }
		}

		public List<ListItem> ListItems { get; }

		public GroupConflictViewModel() {
			ListItems = new List<ListItem>();
		}

		public string GetSelectedGroup() {
			return ListItems.Find(li => li.Selected == true).Text;
		}
	}
}
