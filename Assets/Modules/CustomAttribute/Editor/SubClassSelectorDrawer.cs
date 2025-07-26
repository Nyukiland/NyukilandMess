using UnityEditor.IMGUI.Controls;
using System.Collections.Generic;
using Modules.CustomAttribute;
using System.Linq;
using UnityEditor;
using UnityEngine;
using System;

using Object = UnityEngine.Object;

//based on Mackysoft's work
//code : https://github.com/mackysoft/Unity-SerializeReferenceExtensions/blob/main/Assets/MackySoft/MackySoft.SerializeReferenceExtensions/Editor/SubclassSelectorDrawer.cs

namespace Modules.CustomAttributeEditor
{
	[CustomPropertyDrawer(typeof(SubClassSelectorAttribute))]
	public class SubClassSelectorDrawer : PropertyDrawer
	{
		private readonly Dictionary<string, TypePopupCache> _typePopups = new();
		private readonly Dictionary<string, GUIContent> _typeNameCache = new();

		private SerializedProperty _targetProperty;

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			EditorGUI.BeginProperty(position, label, property);

			if (property.propertyType != SerializedPropertyType.ManagedReference)
			{
				EditorGUI.LabelField(position, label, new GUIContent("Use with [SerializeReference]"));
				EditorGUI.EndProperty();
				return;
			}

			// Draw type selection dropdown
			Rect labelRect = new Rect(position.x, position.y, position.width - ((position.width / 5) + 10), EditorGUIUtility.singleLineHeight);
			Rect popupRect = EditorGUI.PrefixLabel(labelRect, label);

			if (EditorGUI.DropdownButton(popupRect, GetTypeName(property), FocusType.Keyboard))
			{
				TypePopupCache popup = GetTypePopup(property);
				_targetProperty = property;
				popup.TypePopup.Show(popupRect);
			}

			GUI.enabled = !string.IsNullOrEmpty(property.managedReferenceFullTypename);
			if (GUI.Button(new Rect(position.x + position.width - position.width / 5, position.y, position.width / 5, EditorGUIUtility.singleLineHeight), "Edit Script"))
			{
				string[] typeInfo = property.managedReferenceFullTypename.Split(' ');
				if (typeInfo.Length == 2)
				{
					string assemblyName = typeInfo[0];
					string className = typeInfo[1];
					Type type = Type.GetType($"{className}, {assemblyName}");

					if (type != null)
					{
						// Try direct MonoScript lookup
						MonoScript[] scripts = (MonoScript[])Resources.FindObjectsOfTypeAll(typeof(MonoScript));
						foreach (MonoScript script in scripts)
						{
							if (script != null && script.GetClass() == type)
							{
								AssetDatabase.OpenAsset(script);
								break;
							}
						}
					}
				}
			}
			GUI.enabled = true;

			// Draw foldout and property content
			if (!string.IsNullOrEmpty(property.managedReferenceFullTypename))
			{
				position.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

				Rect foldoutRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
				property.isExpanded = EditorGUI.Foldout(foldoutRect, property.isExpanded, GUIContent.none, true);

				List<SerializedProperty> childPoperty = GetChildProperties(property).ToList();

				if (childPoperty.Count == 0) return;

				if (property.isExpanded)
				{
					using (new EditorGUI.IndentLevelScope())
					{
						Rect childPosition = new Rect(position.x, foldoutRect.y + EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing, position.width, 0);
						foreach (SerializedProperty childProperty in GetChildProperties(property))
						{
							float height = EditorGUI.GetPropertyHeight(childProperty, true);
							childPosition.height = height;
							EditorGUI.PropertyField(childPosition, childProperty, true);
							childPosition.y += height + EditorGUIUtility.standardVerticalSpacing;
						}
					}
				}
			}

			EditorGUI.EndProperty();
		}

		private TypePopupCache GetTypePopup(SerializedProperty property)
		{
			string fieldTypename = property.managedReferenceFieldTypename;

			if (!_typePopups.TryGetValue(fieldTypename, out TypePopupCache cache))
			{
				AdvancedDropdownState state = new();
				Type baseType = GetTypeFromTypename(fieldTypename);

				IOrderedEnumerable<Type> types = AppDomain.CurrentDomain.GetAssemblies()
					.SelectMany(a => a.GetTypes())
					.Where(t => baseType.IsAssignableFrom(t) && !t.IsAbstract && !t.IsInterface)
					.OrderBy(t => t.Name);

				TypePopup popup = new(types, 13, state);
				popup.OnItemSelected += item => {
					foreach (Object target in _targetProperty.serializedObject.targetObjects)
					{
						SerializedObject obj = new(target);
						SerializedProperty prop = obj.FindProperty(_targetProperty.propertyPath);
						object instance = Activator.CreateInstance(item.Type);
						prop.managedReferenceValue = instance;
						prop.isExpanded = instance != null;
						obj.ApplyModifiedProperties();
						obj.Update();
					}
				};

				cache = new TypePopupCache(popup, state);
				_typePopups.Add(fieldTypename, cache);
			}

			return cache;
		}

		private GUIContent GetTypeName(SerializedProperty property)
		{
			string fullTypename = property.managedReferenceFullTypename;
			if (string.IsNullOrEmpty(fullTypename))
			{
				return new GUIContent("Null");
			}

			if (_typeNameCache.TryGetValue(fullTypename, out GUIContent label))
			{
				return label;
			}

			Type type = GetTypeFromTypename(fullTypename);
			string name = type != null ? ObjectNames.NicifyVariableName(type.Name) : "(Missing)";
			label = new GUIContent(name);
			_typeNameCache.Add(fullTypename, label);
			return label;
		}

		private static Type GetTypeFromTypename(string typeName)
		{
			if (string.IsNullOrEmpty(typeName)) return null;

			string[] split = typeName.Split(' ');
			if (split.Length != 2) return null;

			return Type.GetType($"{split[1]}, {split[0]}");
		}

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			if (!property.isExpanded || property.managedReferenceValue == null)
			{
				return EditorGUIUtility.singleLineHeight * 2 + EditorGUIUtility.standardVerticalSpacing;
			}

			float height = EditorGUIUtility.singleLineHeight * 2 + EditorGUIUtility.standardVerticalSpacing;

			foreach (SerializedProperty child in GetChildProperties(property))
			{
				height += EditorGUI.GetPropertyHeight(child, true) + EditorGUIUtility.standardVerticalSpacing;
			}

			return height;
		}

		private IEnumerable<SerializedProperty> GetChildProperties(SerializedProperty property)
		{
			SerializedProperty copy = property.Copy();
			SerializedProperty end = copy.GetEndProperty();

			copy.NextVisible(true);
			while (!SerializedProperty.EqualContents(copy, end))
			{
				yield return copy;
				if (!copy.NextVisible(false)) break;
			}
		}


		//---------------------------------

		private class TypeDropDownSelector : AdvancedDropdownItem
		{
			public Type Type { get; }

			public TypeDropDownSelector(Type type, string name) : base(name)
			{
				Type = type;
			}
		}

		private class TypePopup : AdvancedDropdown
		{
			private readonly List<Type> _types;
			public event Action<TypeDropDownSelector> OnItemSelected;

			public TypePopup(IEnumerable<Type> types, int maxLineCount, AdvancedDropdownState state)
				: base(state)
			{
				_types = types.ToList();
				minimumSize = new Vector2(300, EditorGUIUtility.singleLineHeight * maxLineCount);
			}

			protected override AdvancedDropdownItem BuildRoot()
			{
				AdvancedDropdownItem root = new("Select Type");
				foreach (Type type in _types)
				{
					string displayName = ObjectNames.NicifyVariableName(type.Name);
					TypeDropDownSelector item = new(type, displayName);
					root.AddChild(item);
				}
				return root;
			}

			protected override void ItemSelected(AdvancedDropdownItem item)
			{
				if (item is TypeDropDownSelector typeItem)
				{
					OnItemSelected?.Invoke(typeItem);
				}
			}
		}

		private struct TypePopupCache
		{
			public TypePopup TypePopup { get; }
			public AdvancedDropdownState State { get; }

			public TypePopupCache(TypePopup popup, AdvancedDropdownState state)
			{
				TypePopup = popup;
				State = state;
			}
		}
	}
}