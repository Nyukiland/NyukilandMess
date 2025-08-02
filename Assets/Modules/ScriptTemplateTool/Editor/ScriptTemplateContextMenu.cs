using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System.IO;
using System.Linq;

namespace Modules.ScriptTemplate
{
	public class ScriptTemplateContextMenu : Editor
	{
		[MenuItem("Assets/Create/Scripting/StateMachine/Ability Script", false, 1)]
		public static void CreateAbilityScript()
		{
			CreateScriptFromTemplate("AbilityTemplate.txt");
		}

		[MenuItem("Assets/Create/Scripting/StateMachine/Resource Script", false, 1)]
		public static void CreateResourceScript()
		{
			CreateScriptFromTemplate("ResourceTemplate.txt");
		}

		[MenuItem("Assets/Create/Scripting/StateMachine/State Script", false, 1)]
		public static void CreateStateScript()
		{
			CreateScriptFromTemplate("StateTemplate.txt");
		}

		[MenuItem("Assets/Create/Scripting/StateMachine/Combined State Script", false, 1)]
		public static void CreateCompositeStateScript()
		{
			CreateScriptFromTemplate("CombinedStateTemplate.txt");
		}

		[MenuItem("Assets/Create/Scripting/Editor/Editor Window Script", false, 1)]
		public static void CreateEditorWindowScript()
		{
			CreateScriptFromTemplate("EditorWindowTemplate.txt");
		}

		[MenuItem("Assets/Create/Scripting/Editor/Editor Script", false, 1)]
		public static void CreateEditorScript()
		{
			CreateScriptFromTemplate("EditorTemplate.txt");
		}

		[MenuItem("Assets/Create/Scripting/Editor/Attribute Script", false, 1)]
		public static void CreateAttributeScript()
		{
			CreateScriptFromTemplate("AttributeTemplate.txt");
		}

		[MenuItem("Assets/Create/Scripting/Editor/Attribute Drawer Script", false, 1)]
		public static void CreateAttributeDrawerScript()
		{
			CreateScriptFromTemplate("AttributeDrawerTemplate.txt");
		}

		private static void CreateScriptFromTemplate(string templateName)
		{
			string[] g = AssetDatabase.FindAssets($"t:Script {nameof(ScriptTemplateContextMenu)}");
			string filePath = AssetDatabase.GUIDToAssetPath(g[0]);
			int lastSlash = filePath.LastIndexOf('/');
			int secondLastSlash = filePath.LastIndexOf('/', lastSlash - 1);
			string trimmedPath = filePath.Substring(0, secondLastSlash);
			string templatePath = trimmedPath + "/" + templateName;

			if (!File.Exists(templatePath))
			{
				Debug.LogError($"Template file not found: {templatePath}");
				return;
			}

			string selectedPath = GetSelectedPath();
			string defaultFileName = "NewScript.cs";

			if (string.IsNullOrEmpty(selectedPath))
			{
				Debug.LogError("SelectedPath not Found");
				return;
			}

			ProjectWindowUtil.StartNameEditingIfProjectWindowExists(
				0,
				ScriptableObject.CreateInstance<DoCreateScriptAsset>(),
				Path.Combine(selectedPath, defaultFileName),
				null,
				File.ReadAllText(templatePath)
			);
		}

		private static string GetSelectedPath()
		{
			string selectedPath = AssetDatabase.GetAssetPath(Selection.activeObject);
			if (string.IsNullOrEmpty(selectedPath))
			{
				return "";
			}

			if (AssetDatabase.IsValidFolder(selectedPath))
			{
				return selectedPath;
			}

			return Path.GetDirectoryName(selectedPath);
		}

		private class DoCreateScriptAsset : UnityEditor.ProjectWindowCallback.EndNameEditAction
		{
			public override void Action(int instanceId, string pathName, string resourceFile)
			{
				string scriptName = Path.GetFileNameWithoutExtension(pathName);
				string scriptContent = resourceFile.Replace("#SCRIPTNAME#", scriptName);

				string namespaceText = FindNamespace(pathName.Replace($"/{scriptName}.cs", ""));

				if (string.IsNullOrEmpty(namespaceText))
				{
					List<string> myList = new(scriptContent.Split("\n"));
					var properText = myList.Where(x => !x.Contains("#NAMESPACE_START#") && !x.Contains("#NAMESPACE_END#"));
					scriptContent = string.Join("\n", properText.ToArray());
				}
				else
				{
					List<string> myList = new(scriptContent.Split("\n"));
					bool start = false;
					for (int i = 0; i < myList.Count; i++)
					{
						if (myList[i].Contains("#NAMESPACE_END#"))
						{
							myList[i] = myList[i].Replace("#NAMESPACE_END#", "}");
							break;
						}
						else if (myList[i].Contains("#NAMESPACE_START#"))
						{
							myList[i] = myList[i].Replace("#NAMESPACE_START#", $"namespace {namespaceText} \n" + "{");
							start = true;
							continue;
						}

						if (start)
						{
							myList[i] = "\t" + myList[i];
						}
					}

					scriptContent = string.Join("\n", myList.ToArray());
				}

				File.WriteAllText(pathName, scriptContent);
				AssetDatabase.Refresh();

				Object asset = AssetDatabase.LoadAssetAtPath<Object>(pathName);
				ProjectWindowUtil.ShowCreatedAsset(asset);
			}

			private string FindNamespace(string path)
			{
				while (!string.IsNullOrEmpty(path) && Directory.Exists(path))
				{
					string[] asmdefFiles = Directory.GetFiles(path, "*.asmdef", SearchOption.TopDirectoryOnly);
					if (asmdefFiles.Length > 0)
					{
						return Path.GetFileNameWithoutExtension(asmdefFiles[0]);
					}

					path = Path.GetDirectoryName(path);
				}

				return "";
			}
		}
	}
}