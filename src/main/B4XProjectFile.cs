using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Text;

namespace B4X_conflict_fixer {
	class B4XProjectFile {
		private string mFilePath;

		// The project file can be split into 2 parts: the header, which contains the project information,
		// and the B4X code itself, which is what the user sees when opening the porject in B4X.
		private List<string> mFileHeader;
		private List<string> mB4XCode;

		private float mProductVersion; // B4X version

		private string mModuleGroup; // The name of the group that the 'Main' module belongs
		private string mManifest;

		// Stores the list of file groups for each file number, BEFORE any conflicts have been resolved.
		private readonly Dictionary<string, HashSet<string>> mAllFileGroups = new Dictionary<string, HashSet<string>>();
		// Stores the unique file group for each file NAME. This will be filled AFTER conflicts have been resolved.
		// Key is filename, value is filegroup
		private readonly Dictionary<string, string> mFiles = new Dictionary<string, string>();

		private HashSet<string> mBuilds = new HashSet<string>();
		private readonly HashSet<string> mLibraries = new HashSet<string>();
		private readonly HashSet<string> mModules = new HashSet<string>();

		private string StripNewline(string input) {
			return input.Replace("\n", "").Replace("\r", "");
		}

		/// <summary>
		/// Fixes any conflicts in the project file.
		/// </summary>
		/// <param name="projectFile">Path to the B4X project file</param>
		/// <returns><see langword="true"/> if conflicts were fixed, <see langword="false"/> if the user cancelled at any stage.</returns>
		public async Task<bool> LoadAndFixConflicts(string projectFile) {
			mFilePath = projectFile;

			// Everything past @EndOfDesignText@ is regular B4X code, let the user fix code conflicts themselves.
			List<string> fileLines = new List<string>(await File.ReadAllLinesAsync(projectFile));
			int lineCount = fileLines.IndexOf("@EndOfDesignText@");
			mFileHeader = fileLines.GetRange(0, lineCount);
			mB4XCode = fileLines.GetRange(lineCount, fileLines.Count - lineCount);

			if (!FixModuleGroupConflicts()) return false;
			ParseAllFileGroups();
			if (!FixFileConflicts()) return false;
			FixLibAndModuleConflicts();
			if (!FixBuildConflicts()) return false;
			if (!FixManifestConflicts()) return false;
			FixProductVersionConflicts();

			await Save();
			return true;
		}

		/// <summary>
		/// Searches through the project file and detects conflicts with the module group. User is prompted to fix any conflicts.
		/// </summary>
		/// <returns>
		/// <see langword="true" /> if there was no conflict, or the conflict was resolved, <see langword="false" /> if there was a conflict but the user cancelled the operation
		/// </returns>
		/// <remarks>
		/// "Module group" refers to what folder the Main module belongs to inside the B4X module viewer
		/// </remarks>
		private bool FixModuleGroupConflicts() {
			bool result = true;

			// Parse the module group name out of the line then convert to a set to remove duplicate module groups
			List<string> groupLines = mFileHeader.FindAll(line => line.StartsWith("Group="));
			for (int i = 0; i < groupLines.Count; i++)
				groupLines[i] = StripNewline(groupLines[i][6..]);
			HashSet<string> moduleGroups = new HashSet<string>(groupLines);
			
			// There should only be one unique 'Group' line. If there are 2, then there is a git conflict between them
			if (groupLines.Count > 1) {
				var dialog = new GroupConflictDialog(moduleGroups, GroupConflictDialog.GroupTypes.TYPE_MODULE_GROUP) {
					Owner = MainWindow.CurrentInstance
				};
				result = dialog.ShowDialog() ?? false;
				mModuleGroup = dialog.selectedGroup;
			}
			else {
				mModuleGroup = groupLines[0];
			}

			return result;
		}

		/// <summary>
		/// Parses out the file groups for each file number.
		/// </summary>
		/// <remarks>
		/// A valid B4X file should only have 1 file group per unique file number.
		/// However, if there are git conflicts then there could be 2 FileGroup elements with the same file number.
		/// </remarks>
		private void ParseAllFileGroups() {
			// Format of line is 'FileGroup' followed by a number (corresponding to the file number) then an equals caaracter then the name of the file
			// e.g. "FileGroup23=Layout Files"
			foreach (string line in mFileHeader.FindAll(line => line.StartsWith("FileGroup"))) {
				string fileNum = line[9..line.IndexOf('=')];
				if (!mAllFileGroups.ContainsKey(fileNum))
					mAllFileGroups.Add(fileNum, new HashSet<string>());
				mAllFileGroups.TryGetValue(fileNum, out HashSet<string> groupsForThisFile);

				// Add() this file group to the list for this file number.
				// If the group already exists for this file number, then Add() will just ignore it.
				groupsForThisFile.Add(StripNewline(line[(line.IndexOf('=') + 1)..]));
			}
		}

		/// <summary>
		/// Parses asset files from the project, then removes duplicates and invalid ones.
		/// If there are any conflicts between the file groups, a dialog opens allowing the user to fix the conflict. 
		/// </summary>
		/// <returns>
		/// <see langword="false" /> if there was a conflict and the user cancelled the operation, otherwise <see langword="true" />
		/// </returns>
		private bool FixFileConflicts() {
			List<string> fileLines = mFileHeader.FindAll(line => line.StartsWith("File") && !line.StartsWith("FileGroup"));
			foreach (string line in fileLines) {
				string filename = StripNewline(line[(line.IndexOf('=') + 1)..]);
				// If we have this file stored already then this is a duplicate record.
				// We have already processed all occurences and conflicts so don't do anything else with it.
				if (mFiles.ContainsKey(filename)) continue;

				// Check if this file exists in the B4X projects files folder.
				// If it doesn't then we ignore it because someone will have removed it but git conflict added it back in.
				if (!File.Exists(mFilePath[..mFilePath.LastIndexOf('\\')] + "/Files/" + filename)) continue;

				// Get all of the possible filegroups for this file. Note we search for all occurences of this filename.
				HashSet<string> groupsForThisFilename = new HashSet<string>();
				foreach (string occurence in fileLines.FindAll(line => StripNewline(line).EndsWith(filename))) {
					string fileNumber = occurence[4..occurence.IndexOf('=')];
					bool fileNumberHasGroups = mAllFileGroups.TryGetValue(fileNumber, out HashSet<string> value);

					// It is possible to get git conflicts that contain file numbers with no groups assigned to them.
					if (fileNumberHasGroups)
						groupsForThisFilename.UnionWith(value);
				}

				string finalGroup;

				// If this filename has multiple possible filegroups, then there is a git conflict
				if (groupsForThisFilename.Count > 1) {
					var dialog = new GroupConflictDialog(groupsForThisFilename, GroupConflictDialog.GroupTypes.TYPE_FILE_GROUP, filename) {
						Owner = MainWindow.CurrentInstance
					};
					if (!dialog.ShowDialog() ?? false) return false;
					finalGroup = dialog.selectedGroup;
				}
				// Otherwise we just use the first (and only) group
				else {
					finalGroup = groupsForThisFilename.Single();
				}

				mFiles.Add(filename, finalGroup);
			}

			return true;
		}

		/// <summary>
		/// Parses libraries and modules from the project file, removing any duplicates or invalid entries.
		/// </summary>
		private void FixLibAndModuleConflicts() {
			foreach (string line in mFileHeader.FindAll(s => s.StartsWith("Library"))) {
				mLibraries.Add(line[(line.IndexOf('=') + 1)..]);
			}

			foreach (string line in mFileHeader.FindAll(s => s.StartsWith("Module"))) {
				string moduleName = line[(line.IndexOf('=') + 1)..];
				string modulePath;
				
				// Module names are the path to the module. If |absolute| exists then the path is absolute path.
				// |relative| means path relative to the project file.
				// Just a module name with no path means the module is in the same folder as the project file. We can treat this case like |relative|.
				if (moduleName.StartsWith("|absolute|")) {
					modulePath = moduleName.Replace("|absolute|", "") + ".bas";
				}
				else {
					modulePath = mFilePath[..mFilePath.LastIndexOf('\\')] + "/" + moduleName.Replace("|relative|", "") + ".bas";
				}

				if (File.Exists(modulePath)) {
					mModules.Add(moduleName);
				}
			}
		}

		private bool FixBuildConflicts() {
			// Parse existing builds into list
			foreach (string line in mFileHeader.FindAll(s => s.StartsWith("Build"))) {
				mBuilds.Add(line[(line.IndexOf('=') + 1)..]);
			}

			// If there are conflict markers around at least one build then there are build conflicts.
			// Open conflict fixing dialog.
			if (Regex.IsMatch(string.Join('\n', mFileHeader), "<<<<<<<.*Build\\d+=.*=======", RegexOptions.Singleline)) {
				BuildSelectionDialog dialog = new BuildSelectionDialog(mBuilds);
				bool dialogResult = dialog.ShowDialog() ?? false;
				if (!dialogResult) return false;
				mBuilds = dialog.FinalBuildConfigs;
			}

			return true;
		}

		private void RemoveFiles(string[] files) {
			foreach (string file in files) {
				if (File.Exists(file))
					File.Delete(file);
			}
		}

		/// <summary>
		/// If there are manifest conflicts, this function will attempt to merge them. The ones that cannot be merged iwll be shown to the user for them to resolve manually.
		/// </summary>
		/// <returns><see langword="false"/> if the user cancelled, <see langword="true"/> otherwise.</returns>
		private bool FixManifestConflicts() {
			List<string> lines = mFileHeader.FindAll(line => line.StartsWith("ManifestCode="));
			// If there is more than one manifest line there is a conflict
			if (lines.Count > 1) {
				// Create temp files for git to merge
				string file1 = Path.GetTempFileName();
				string file2 = Path.GetTempFileName();
				string emptyFile = Path.GetTempFileName(); // Empty file for common ancestor
				File.WriteAllText(file1, lines[0][13..].Replace("~\\n~", "\r\n"));
				File.WriteAllText(file2, lines[1][13..].Replace("~\\n~", "\r\n"));

				try {
					// Use Git to merge the two files, using the empty file as a common ancestor
					// The merged data will be written to file1
					Process.Start(new ProcessStartInfo() { FileName = "git", Arguments = $"merge-file {file1} {emptyFile} {file2}", CreateNoWindow = true }).WaitForExit();
				}
				catch (Exception) {
					RemoveFiles(new string[] { file1, file2, emptyFile });
					InfoMessageBox.Show(MainWindow.CurrentInstance, "Git not installed. Cannot fix manifest conflicts.", "Error", "Abort");
					return false;
				}
				
				string mergedManifest = File.ReadAllText(file1);
				// Look for git merge conflict marker
				if (mergedManifest.Contains($"<<<<<<< {file1}")) {
					var dialog = new ManifestConflictDialog(lines[0][13..].Replace("~\\n~", "\r\n"), lines[1][13..].Replace("~\\n~", "\r\n"), file1);
					bool dialogResult = dialog.ShowDialog() ?? false;
					RemoveFiles(new string[] { file1, file2, emptyFile });
					if (!dialogResult) return false;

					mManifest = dialog.manifestContent.Replace("\r\n", "~\\n~").Replace("\n", "~\\n~");
				}
				// Otherwise the git merge was succesfull
				else {
					mManifest = mergedManifest.Replace("\r\n", "~\\n~").Replace("\n", "~\\n~");
				}
			}
			else {
				mManifest = lines[0][13..];
			}

			return true;
		}

		// Bump version to the highest version if there are conflicts
		private void FixProductVersionConflicts() {
			List<float> versions = mFileHeader.FindAll(s => s.StartsWith("Version=")).ConvertAll(s => Convert.ToSingle(s[8..]));
			mProductVersion = versions.Max();
			if (versions.Count > 1) {
				InfoMessageBox.Show(MainWindow.CurrentInstance, $"The version for this project has been bumped to {mProductVersion}.\nPlease check your B4X product version and update B4X if necessary.", "B4X product version conflicts");
			}
		}

		private void AddCategoryToOutputList(string categoryName, IEnumerable<string> categoryValues, List<string> outputList) {
			int i = 1;
			foreach (string value in categoryValues) {
				outputList.Add($"{categoryName}{i}={value}");
				i++;
			}
		}

		// Writes all of the fixed info to the project file
		private async Task Save() {
			List<string> lines = new List<string> { "Group=" + mModuleGroup, "ManifestCode=" + mManifest };
			AddCategoryToOutputList("Build", mBuilds, lines);
			// Files and File groups will be added in same order so the file numbers will still link up correctly
			AddCategoryToOutputList("File", mFiles.Keys, lines);
			AddCategoryToOutputList("FileGroup", mFiles.Values, lines);
			AddCategoryToOutputList("Library", mLibraries, lines);
			AddCategoryToOutputList("Module", mModules, lines);
			lines.Add($"NumberOfFiles={mFiles.Count}");
			lines.Add($"NumberOfLibraries={mLibraries.Count}");
			lines.Add($"NumberOfModules={mModules.Count}");
			lines.Add($"Version={mProductVersion}");

			string backupFile = mFilePath.Insert(mFilePath.LastIndexOf('.') + 1, "backup.");
			File.WriteAllBytes(backupFile, await File.ReadAllBytesAsync(mFilePath));
			// B4X uses UTF8-BOM encoding
			await File.WriteAllLinesAsync(mFilePath, lines.Concat(mB4XCode), new UTF8Encoding(true));
			File.Delete(backupFile);
		}
	}
}
