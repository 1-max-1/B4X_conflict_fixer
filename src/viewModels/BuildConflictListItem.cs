using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace B4X_conflict_fixer {
	/// <summary>
	/// Represents a B4X build configuration as an item in the build list
	/// </summary>
	public class BuildConflictListItem : INotifyPropertyChanged {
		/// <summary>
		/// True if the list item's ehckbox is checked
		/// </summary>
		public bool Selected { get; set; }

		private string mBuildName;
		public string BuildName {
			get => mBuildName;
			set {
				mBuildName = value.Trim();
				OnPropertyChanged();
			}
		}

		private string mPackageName;
		/// <summary>
		/// The java package name for this build configuration
		/// </summary>
		public string PackageName {
			get => mPackageName;
			set {
				mPackageName = value.Trim();
				OnPropertyChanged();
			}
		}

		private string mConditionalSymbols;
		/// <summary>
		/// Comma-seperated list of B4X conditional build symbols
		/// </summary>
		public string ConditionalSymbols {
			get => mConditionalSymbols;
			set {
				mConditionalSymbols = value;
				PrettifyConditionalSymbols();
				OnPropertyChanged();
			}
		}

		/// <summary>
		/// Creates a new build config list item from the passed B4X build string
		/// </summary>
		/// <param name="buildData">The string that represents the build - this should come from the .b4x project file</param>
		public BuildConflictListItem(string buildData) {
			Selected = true;

			string[] data = buildData.Split(',');
			BuildName = data[0];
			PackageName = data[1];

			// It's possible (and likely) for a build config to have no symbols, so check before assignment
			if (data.Length > 2)
				ConditionalSymbols = string.Join(',', data[2..]).Replace(" ", "");
			else
				ConditionalSymbols = "";
		}

		/// <summary>
		/// Creates a new build config list item with the specified information. Use this constructor when you don't have the B4X build string
		/// </summary>
		public BuildConflictListItem(string buildName, string packageName, string conditionalSymbols) {
			BuildName = buildName;
			PackageName = packageName;
			Selected = true;
			ConditionalSymbols = conditionalSymbols;
		}

		// Removes spaces, trailing and leading commas and empty symbols from the current instances symbol list
		private void PrettifyConditionalSymbols() {
			string trimmedSymbols = mConditionalSymbols.Trim().Replace(" ", "");
			trimmedSymbols = Regex.Replace(trimmedSymbols, ",+", ",");
			mConditionalSymbols = Regex.Replace(trimmedSymbols, "(^,)|(,$)", "");
		}

		/// <summary>
		/// Clones the current build config list item
		/// </summary>
		/// <param name="newBuildName">The only thing that cannot be cloned is the name - pass a new nique build name here</param>
		/// <returns>A new cloned item</returns>
		public BuildConflictListItem MakeNewUsingMeAsBase(string newBuildName) {
			return new BuildConflictListItem(newBuildName, PackageName, ConditionalSymbols);
		}

		/// <summary>
		/// Converts the build info to its b4x project file style string representation
		/// </summary>
		public string ToB4XStringFormat() {
			return $"{BuildName},{PackageName}{(ConditionalSymbols != "" ? "," : "")}{ConditionalSymbols}";
		}

		public event PropertyChangedEventHandler PropertyChanged;
		protected void OnPropertyChanged([CallerMemberName] string name = null) {
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
		}
	}
}
