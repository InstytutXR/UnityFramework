using System;
using System.Reflection;

using UnityEditor;
using UnityEngine;

namespace Framework
{
	using Serialization;

	namespace Utils
	{
		namespace Editor
		{
			[SerializedObjectEditor(typeof(EditorAssetRef<>), "PropertyField")]
			public static class EditorAssetRefEditor
			{
				#region SerializedObjectEditor
				public static object PropertyField(object obj, GUIContent label, ref bool dataChanged, GUIStyle style, params GUILayoutOption[] options)
				{
					Type assetType = SystemUtils.GetGenericImplementationType(typeof(EditorAssetRef<>), obj.GetType());

					if (assetType != null)
					{
						MethodInfo genericFieldMethod = typeof(EditorAssetRefEditor).GetMethod("EditorAssetRefField", BindingFlags.Static | BindingFlags.NonPublic);
						MethodInfo typedFieldMethod = genericFieldMethod.MakeGenericMethod(assetType);

						if (typedFieldMethod != null)
						{
							object[] args = new object[] { obj, label, dataChanged };
							obj = typedFieldMethod.Invoke(null, args);

							if ((bool)args[2])
								dataChanged = true;
						}
					}

					return obj;
				}
				#endregion

				private static EditorAssetRef<T> EditorAssetRefField<T>(EditorAssetRef<T> assetRef, GUIContent label, ref bool dataChanged) where T : UnityEngine.Object
				{
					if (label == null)
						label = new GUIContent();

					label.text += " (" + assetRef + ")";

					bool editorCollapsed = !EditorGUILayout.Foldout(!assetRef._editorCollapsed, label);

					if (editorCollapsed != assetRef._editorCollapsed)
					{
						assetRef._editorCollapsed = editorCollapsed;
						dataChanged = true;
					}

					if (!editorCollapsed)
					{
						int origIndent = EditorGUI.indentLevel;
						EditorGUI.indentLevel++;

						T asset = EditorGUILayout.ObjectField("File", assetRef._editorAsset, typeof(T), false) as T;

						//If asset changed update GUIDS
						if (assetRef._editorAsset != asset || assetRef.GetFilePath() != AssetDatabase.GetAssetPath(asset))
						{
							assetRef = new EditorAssetRef<T>(asset);
							dataChanged = true;
						}

						EditorGUI.indentLevel = origIndent;
					}

					return assetRef;
				}

			}
		}
	}
}