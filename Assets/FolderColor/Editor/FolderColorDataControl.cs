using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace FolderColor
{
	public class FolderColorDataControl
	{
		public enum FolderColorMode
		{
			Global,
			Personal
		}

		[Serializable]
		public class FolderColorEntry
		{
			public string Guid;
			public string IconPath;
			public string UserId;
		}

		[InitializeOnLoad]
		public static class FolderColorData
		{
			private static FolderColorDatabase _databaseGlobal;
			private static FolderColorDatabase _databasePersonal;
			private static string _userId => Environment.UserName;

			static FolderColorData()
			{
				EditorApplication.delayCall += LoadOrCreate;
			}

			#region Public

			public static FolderColorEntry Get(string guid, FolderColorMode mode = FolderColorMode.Global)
			{
				FolderColorDatabase database = mode == FolderColorMode.Personal ? _databasePersonal : _databaseGlobal;

				if (database == null) 
					return null;

				return database.Entries.LastOrDefault(e =>e.Guid == guid &&
					(database.Mode == FolderColorMode.Global || e.UserId == _userId));
			}

			public static void Set(string guid, string iconPath, FolderColorMode mode = FolderColorMode.Global)
			{
				FolderColorDatabase database = mode == FolderColorMode.Personal ? _databasePersonal : _databaseGlobal;

				Remove(guid);

				database.Entries.Add(new FolderColorEntry
				{
					Guid = guid,
					IconPath = iconPath,
					UserId = database.Mode == FolderColorMode.Personal ? _userId : string.Empty
				});

				Save(database);
			}

			public static void Remove(string guid, FolderColorMode mode = FolderColorMode.Global)
			{
				FolderColorDatabase database = mode == FolderColorMode.Personal ? _databasePersonal : _databaseGlobal;

				if (database == null) 
					return;

				database.Entries.RemoveAll(e =>e.Guid == guid &&
					(database.Mode == FolderColorMode.Global || e.UserId == _userId));

				Save(database);
			}

			#endregion

			#region Load / Save

			private static void LoadOrCreate()
			{
				MigrateOLDJsonIfNeeded();

				TryGenerateScriptable(GetGlobalAssetPath(), ref _databaseGlobal);
				TryGenerateScriptable(GetPersonalAssetPath(), ref _databasePersonal);

				void TryGenerateScriptable(string assetPath, ref FolderColorDatabase database)
				{
					database = AssetDatabase.LoadAssetAtPath<FolderColorDatabase>(assetPath);

					if (database != null)
						return;

					database = ScriptableObject.CreateInstance<FolderColorDatabase>();
					database.Mode = FolderColorMode.Global;

					EnsureFolderExists(assetPath);
					AssetDatabase.CreateAsset(database, assetPath);
					AssetDatabase.SaveAssets();
				}
			}

			private static void Save(FolderColorDatabase database)
			{
				if (database == null) 
					return;

				EditorUtility.SetDirty(database);
				AssetDatabase.SaveAssets();
			}

			#endregion

			#region OLD JSON to Scriptable

			private static void MigrateOLDJsonIfNeeded()
			{
				string globalPath = GetGlobalAssetPath();

				if (AssetDatabase.LoadAssetAtPath<FolderColorDatabase>(globalPath) != null)
					return;

				string jsonPath = FindOLDJson();
				if (string.IsNullOrEmpty(jsonPath))
					return;

				string json = File.ReadAllText(jsonPath);
				OldJson legacy = JsonUtility.FromJson<OldJson>(json);
				if (legacy == null || legacy.assetModified == null)
					return;

				EnsureFolderExists(globalPath);

				FolderColorDatabase database = ScriptableObject.CreateInstance<FolderColorDatabase>();
				database.Mode = FolderColorMode.Global;
				database.Entries = new List<FolderColorEntry>();

				AssetDatabase.CreateAsset(database, globalPath);
				AssetDatabase.SaveAssets();

				for (int i = 0; i < legacy.assetModified.Count; i++)
				{
					string assetPath = legacy.assetModified[i];
					string guid = AssetDatabase.AssetPathToGUID(assetPath);

					if (string.IsNullOrEmpty(guid))
						continue;

					database.Entries.Add(new FolderColorEntry
					{
						Guid = guid,
						IconPath = legacy.assetModifiedTexturePath[i],
						UserId = string.Empty
					});
				}

				EditorUtility.SetDirty(database);
				AssetDatabase.SaveAssets();
				AssetDatabase.Refresh();

				AssetDatabase.DeleteAsset(jsonPath);
				Debug.Log($"[FolderColor] Migrated {database.Entries.Count} entries from OLD JSON.");
			}


			private static string FindOLDJson()
			{
				string folder = GetSaveSetupFolder();
				if (string.IsNullOrEmpty(folder)) 
					return null;

				string jsonPath = Path.Combine(folder, "FolderModificationData.json");
				return File.Exists(jsonPath) ? jsonPath : null;
			}

			[Serializable]
			private class OldJson
			{
				public List<string> assetModified;
				public List<string> assetModifiedTexturePath;
			}

			#endregion

			#region Path utilities

			private static string GetAnchorScriptPath()
			{
				string guid = AssetDatabase.FindAssets($"{nameof(FolderColorDataControl)} t:script")
					.FirstOrDefault();

				return string.IsNullOrEmpty(guid) ? null : AssetDatabase.GUIDToAssetPath(guid);
			}

			private static string GetSaveSetupFolder()
			{
				string scriptPath = GetAnchorScriptPath();
				if (string.IsNullOrEmpty(scriptPath)) 
					return null;

				return scriptPath.Replace($"/Editor/{nameof(FolderColorDataControl)}.cs", "/SaveSetUp");
			}

			private static string GetGlobalAssetPath()
			{
				return Path.Combine(GetSaveSetupFolder(), "FolderColorSettings_Global.asset");
			}

			private static string GetPersonalAssetPath()
			{
				return Path.Combine(GetSaveSetupFolder(), $"FolderColorSettings_Perso_{_userId}.asset");
			}

			private static void EnsureFolderExists(string assetPath)
			{
				string folder = Path.GetDirectoryName(assetPath);
				if (AssetDatabase.IsValidFolder(folder)) 
					return;

				string parent = Path.GetDirectoryName(folder);
				string name = Path.GetFileName(folder);
				AssetDatabase.CreateFolder(parent, name);
			}

			#endregion
		}
	}
}