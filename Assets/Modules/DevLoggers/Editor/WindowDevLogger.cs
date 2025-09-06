using System.Collections.Generic;
using Modules.DevLoggers;
using System.Linq;
using UnityEditor;
using UnityEngine;
using System;

namespace Modules.DevloggerEditor
{
	public class WindowDevLogger : EditorWindow
	{
		private Vector2 _scroll;
		private bool _showLog = true;
		private bool _showWarning = true;
		private bool _showError = true;
		private bool _clearLogOnPlay = true;
		private string _searchBar = "";

		private Dictionary<string, bool> _tagFilters = new Dictionary<string, bool>();

		[MenuItem("Tools/Dev Logger")]
		public static void ShowWindow()
		{
			WindowDevLogger window = GetWindow<WindowDevLogger>();
			window.titleContent = new GUIContent("Dev Logger");
			window.Show();
		}

		private void OnEnable()
		{
			DevLogger.OnNew += OnNewLog;
			EditorApplication.playModeStateChanged += OnPlayModeChange;
		}

		private void OnDisable()
		{
			DevLogger.OnNew -= OnNewLog;
			EditorApplication.playModeStateChanged -= OnPlayModeChange;
		}

		private void OnPlayModeChange(PlayModeStateChange playMode)
		{
			if (playMode == PlayModeStateChange.ExitingEditMode && _clearLogOnPlay)
				DevLogger.Stored.Clear();
		}

		private void OnNewLog(InfoDevLog info)
		{
			if (!_tagFilters.ContainsKey(info.Tag))
				_tagFilters[info.Tag] = true;

			Repaint(); 
		}

		private void OnGUI()
		{
			GUILayout.BeginHorizontal(EditorStyles.toolbar);
			_showLog = GUILayout.Toggle(_showLog, "Log", EditorStyles.toolbarButton);
			_showWarning = GUILayout.Toggle(_showWarning, "Warning", EditorStyles.toolbarButton);
			_showError = GUILayout.Toggle(_showError, "Error", EditorStyles.toolbarButton);

			GUILayout.Label("Search:", GUILayout.Width(50));
			_searchBar = GUILayout.TextField(_searchBar, EditorStyles.toolbarTextField, GUILayout.ExpandWidth(true));

			if (GUILayout.Button("Clear All", EditorStyles.toolbarButton, GUILayout.Width(80)))
			{
				DevLogger.Stored.Clear();
			}
			_clearLogOnPlay = GUILayout.Toggle(_clearLogOnPlay, "PlayMode Clear", EditorStyles.toolbarButton);

			GUILayout.EndHorizontal();


			if (_tagFilters.Count > 0)
			{
				GUILayout.BeginHorizontal(EditorStyles.toolbar);
				foreach (string tag in _tagFilters.Keys.ToList())
				{
					_tagFilters[tag] = GUILayout.Toggle(_tagFilters[tag], tag, EditorStyles.toolbarButton);
				}
				GUILayout.EndHorizontal();
			}

			GUILayout.Space(5);

			var allLogs = DevLogger.Stored.SelectMany(kvp => kvp.Value)
				.OrderBy(l => l.Frame);

			_scroll = GUILayout.BeginScrollView(_scroll);

			GUIStyle messageStyle = new(EditorStyles.label)
			{
				wordWrap = true
			};

			int rowIndex = 0;

			foreach (var log in allLogs)
			{
				if (!Filter(log)) continue;

				// --- Base striping (dark/light alternating)
				Color baseColor = (rowIndex % 2 == 0)
					? new Color(0.22f, 0.22f, 0.22f)   // darker row
					: new Color(0.26f, 0.26f, 0.26f);  // lighter row

				// --- Severity overlay (Log/Warning/Error tint)
				Color severityTint = log.Type switch
				{
					LogType.Warning => new Color(1f, 1f, 0.5f, 0.15f),
					LogType.Error => new Color(1f, 0.5f, 0.5f, 0.15f),
					_ => new Color(0.8f, 0.8f, 0.8f, 0.05f) // subtle gray for logs
				};

				// Final background color = striping base + tint
				Color finalColor = Color.Lerp(baseColor, severityTint, severityTint.a);

				// --- Pre-calc text height with margins
				string logText = $"[{log.Tag}] {log.Message}";
				float textHeight = messageStyle.CalcHeight(new GUIContent(logText), position.width - 180);
				float rowHeight = textHeight + 30;

				Rect rowRect = GUILayoutUtility.GetRect(position.width, rowHeight, GUILayout.ExpandWidth(true));

				// Draw row background
				EditorGUI.DrawRect(rowRect, finalColor);

				// Whole row is clickable
				if (GUI.Button(rowRect, GUIContent.none, GUIStyle.none))
				{
					UnityEditorInternal.InternalEditorUtility.OpenFileAtLineExternal(log.FilePath, log.LineNumber);
				}

				// --- Icon
				GUIContent icon = log.Type switch
				{
					LogType.Warning => EditorGUIUtility.IconContent("console.warnicon"),
					LogType.Error => EditorGUIUtility.IconContent("console.erroricon"),
					_ => EditorGUIUtility.IconContent("console.infoicon")
				};
				Rect iconRect = new Rect(rowRect.x + 4, rowRect.y + (rowHeight - 16) / 2, 16, 16);
				GUI.Label(iconRect, icon);

				// --- Message text
				Rect textRect = new Rect(rowRect.x + 24, rowRect.y + 10, rowRect.width - 150, textHeight);
				GUI.Label(textRect, logText, messageStyle);

				// --- Time + Frame (centered)
				Rect rightRect = new Rect(rowRect.xMax - 120, rowRect.y, 120, rowHeight - 10);
				float topSpace = (rowHeight - 32) / 2f;

				GUI.Label(
					new Rect(rightRect.x, rightRect.y + topSpace, rightRect.width, 16),
					log.Time.ToString("HH:mm:ss"),
					EditorStyles.miniLabel
				);
				GUI.Label(
					new Rect(rightRect.x, rightRect.y + topSpace + 16, rightRect.width, 16),
					$"Frame {log.Frame}",
					EditorStyles.miniLabel
				);

				rowIndex++;
			}

			GUILayout.EndScrollView();
		}

		private bool Filter(InfoDevLog log)
		{
			if (log.Type == LogType.Log && !_showLog) return false;
			if (log.Type == LogType.Warning && !_showWarning) return false;
			if (log.Type == LogType.Error && !_showError) return false;

			if (_tagFilters.ContainsKey(log.Tag) && !_tagFilters[log.Tag]) return false;

			if (!string.IsNullOrEmpty(_searchBar) &&
				!log.Message.Contains(_searchBar, StringComparison.OrdinalIgnoreCase))
				return false;


			return true;
		}
	}
}