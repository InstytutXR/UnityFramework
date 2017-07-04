using UnityEngine;
using UnityEditor;
using System;

namespace Framework
{
	using Utils;
	using System.Collections.Generic;
	using System.Reflection;
	using Utils.Editor;

	namespace ValueSourceSystem
	{
		namespace Editor
		{
			public abstract class ValueSourcePropertyDrawer<T> : PropertyDrawer
			{
				public enum eEdtiorType
				{
					Static,
					Source,
				}

				private struct MemeberData
				{
					public ValueSource<T>.eSourceType _sourceType;
					public FieldInfo _fieldInfo;
					public int _index;

					public MemeberData(ValueSource<T>.eSourceType sourceType, FieldInfo fieldInfo, int index)
					{
						_sourceType = sourceType;
						_fieldInfo = fieldInfo;
						_index = index;
					}
				}

				public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
				{
					EditorGUI.BeginProperty(position, label, property);

					SerializedProperty sourceTypeProperty = property.FindPropertyRelative("_sourceType");
					SerializedProperty sourceObjectProp = property.FindPropertyRelative("_sourceObject");
					SerializedProperty sourceObjectMemberNameProp = property.FindPropertyRelative("_sourceObjectMemberName");
					SerializedProperty sourceObjectMemberIndexProp = property.FindPropertyRelative("_sourceObjectMemberIndex");
					SerializedProperty valueProperty = property.FindPropertyRelative("_value");

					SerializedProperty editorFoldoutProp = property.FindPropertyRelative("_editorFoldout");
					SerializedProperty editorHeightProp = property.FindPropertyRelative("_editorHeight");

					Rect foldoutPosition = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);

					editorFoldoutProp.boolValue = EditorGUI.Foldout(foldoutPosition, editorFoldoutProp.boolValue, label != null ? label.text : property.displayName);
					editorHeightProp.floatValue = EditorGUIUtility.singleLineHeight;

					if (editorFoldoutProp.boolValue)
					{
						int origIndent = EditorGUI.indentLevel;
						EditorGUI.indentLevel++;
						
						eEdtiorType sourceType = (ValueSource<T>.eSourceType)sourceTypeProperty.intValue == ValueSource<T>.eSourceType.Static ? eEdtiorType.Static : eEdtiorType.Source;
						bool tempOverrideType = sourceType == eEdtiorType.Static && EditorUtils.GetDraggingComponent<MonoBehaviour>() != null;
						if (tempOverrideType)
						{
							sourceType = eEdtiorType.Source;
						}

						EditorGUI.BeginChangeCheck();

						Rect typePosition = new Rect(position.x, position.y + editorHeightProp.floatValue, position.width, EditorGUIUtility.singleLineHeight);
						eEdtiorType edtiorType = (eEdtiorType)EditorGUI.EnumPopup(typePosition, "Source Type", sourceType);
						editorHeightProp.floatValue += EditorGUIUtility.singleLineHeight;

						if (EditorGUI.EndChangeCheck())
						{
							sourceObjectProp.objectReferenceValue = null;
							sourceTypeProperty.intValue = Convert.ToInt32(edtiorType);
						}

						Rect valuePosition = new Rect(position.x, position.y + editorHeightProp.floatValue, position.width, EditorGUIUtility.singleLineHeight);

						switch (sourceType)
						{
							case eEdtiorType.Source:
								{
									editorHeightProp.floatValue += DrawSourceObjectField(sourceObjectProp, sourceTypeProperty, sourceObjectMemberNameProp, sourceObjectMemberIndexProp, valuePosition);
								}
								break;
							case eEdtiorType.Static:
								{
									editorHeightProp.floatValue += DrawValueField(valuePosition, valueProperty);
								}
								break;
						}

						EditorGUI.indentLevel = origIndent;
					}

					EditorGUI.EndProperty();
				}

				public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
				{
					SerializedProperty editorHeightProp = property.FindPropertyRelative("_editorHeight");
					return editorHeightProp.floatValue;
				}

				public virtual float DrawValueField(Rect position, SerializedProperty valueProperty)
				{
					EditorGUI.PropertyField(position, valueProperty, new GUIContent("Value"));
					return EditorGUIUtility.singleLineHeight;
				}


				private float DrawSourceObjectField(SerializedProperty sourceObjectProp, SerializedProperty sourceTypeProperty, SerializedProperty sourceObjectMemberNameProp, SerializedProperty sourceObjectMemberIndexProp, Rect valuePosition)
				{
					Component currentComponent = sourceObjectProp.objectReferenceValue as Component;

					float height;
					Component selectedComponent = EditorUtils.ComponentField<MonoBehaviour>(new GUIContent("Source Object"), valuePosition, currentComponent, out height);

					if (currentComponent != selectedComponent)
					{
						sourceObjectProp.objectReferenceValue = selectedComponent;
						sourceTypeProperty.intValue = Convert.ToInt32(ValueSource<T>.eSourceType.SourceObject);
					}

					valuePosition.y += height;

					if (currentComponent != null)
					{
						height += DrawObjectDropDown(currentComponent, valuePosition, sourceTypeProperty, sourceObjectMemberNameProp, sourceObjectMemberIndexProp);
					}

					return height;
				}

				private float DrawObjectDropDown(object obj, Rect valuePosition, SerializedProperty sourceTypeProperty, SerializedProperty sourceObjectMemberNameProp, SerializedProperty sourceObjectMemberIndexProp)
				{
					List<GUIContent> memberLabels = new List<GUIContent>();
					List<MemeberData> memeberInfo = new List<MemeberData>();

					int index = 0;

					//If the object itself is an IValueSource<T> then it can be selected
					if (SystemUtils.IsTypeOf(typeof(IValueSource<T>), obj.GetType()))
					{
						memberLabels.Add(new GUIContent(".this"));
						memeberInfo.Add(new MemeberData(ValueSource<T>.eSourceType.SourceObject, null, -1));

						if (sourceObjectMemberIndexProp.intValue == -1 && string.IsNullOrEmpty(sourceObjectMemberNameProp.stringValue))
							index = 0;
					}

					//If the object is a dynamic value source container then add its value sources to the list
					if (SystemUtils.IsTypeOf(typeof(IDynamicValueSourceContainer), obj.GetType()))
					{
						IDynamicValueSourceContainer dynamicContainer = (IDynamicValueSourceContainer)obj;

						for (int i = 0; i < dynamicContainer.GetNumberOfValueSources(); i++)
						{
							if (SystemUtils.IsTypeOf(typeof(IValueSource<T>), dynamicContainer.GetValueSource(i).GetType()))
							{
								//Ideally be able to get value source name as well? just for editor?
								memberLabels.Add(new GUIContent(dynamicContainer.GetValueSourceName(i).ToString()));
								memeberInfo.Add(new MemeberData(ValueSource<T>.eSourceType.SourceDynamicMember, null, i));

								if (sourceObjectMemberIndexProp.intValue == i)
									index = memeberInfo.Count - 1;
							}
						}
					
					}

					//Finally add all public fields that are of type IValueSource<T>
					FieldInfo[] fields = ValueSource<T>.GetValueSourceFields(obj);

					foreach (FieldInfo field in fields)
					{
						memberLabels.Add(new GUIContent("." + field.Name));
						memeberInfo.Add(new MemeberData(ValueSource<T>.eSourceType.SourceMember, field, -1));

						if (sourceObjectMemberNameProp.stringValue == field.Name)
							index = memeberInfo.Count - 1;
					}
					
					//Warn if there are no valid options for the object
					if (memeberInfo.Count == 0)
					{
						EditorGUI.LabelField(valuePosition, new GUIContent("Component Property"), new GUIContent("Component has no valid IValueSource<" + typeof(T).Name + ">" + " member!"));
					}
					else
					{
						index = EditorGUI.Popup(valuePosition, new GUIContent("Component Property"), index, memberLabels.ToArray());
						sourceTypeProperty.intValue = Convert.ToInt32(memeberInfo[index]._sourceType);
						sourceObjectMemberNameProp.stringValue = memeberInfo[index]._fieldInfo != null ? memeberInfo[index]._fieldInfo.Name : null;
						sourceObjectMemberIndexProp.intValue = memeberInfo[index]._index;
					}
					

					return EditorGUIUtility.singleLineHeight;
				}
			}
		}
	}
}