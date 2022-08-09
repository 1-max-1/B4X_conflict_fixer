namespace B4X_conflict_fixer {
	/// <summary>
	/// Represents a module group, file group or build config that is in conflict
	/// </summary>
	public class GroupConflictListItem {
		public GroupConflictListItem(string text, bool selected) {
			Selected = selected;
			Text = text;
		}

		public bool Selected { get; set; }
		public string Text { get; }
	}
}
