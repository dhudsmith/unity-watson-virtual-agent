using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using CrazyMinnow.SALSA;
using UMA;
using UMA.PoseTools;

namespace CrazyMinnow.SALSA.UMA_DCS
{
    [CustomEditor(typeof(SalsaUmaSync)), CanEditMultipleObjects]
    public class SalsaUmaSync_Insp : Editor
    {
        private SalsaUmaSync instance;
        private SalsaUmaSync.EyeControl prevEyeControl;
        private bool setStyle;
        private Color title = new Color(0.3f, 0.5f, 0.7f);
        private GUIStyle sectionFoldOuts;
        private GUIStyle arrayFoldOuts;
        private GUIStyle expressionFoldOuts;
        private float deleteWidth = 30f;

        public void OnEnable()
        {
            instance = target as SalsaUmaSync;
            setStyle = true;
        }

        public override void OnInspectorGUI()
        {
            if (setStyle)
            {
                instance.FindSalsa3D();
                instance.FindRandomEyes3D();
                instance.DefaultShapes();
                instance.DefaultShapes();
                instance.SetValueGroups();
                instance.SetValueGroupsDefaultValues();
                sectionFoldOuts = new GUIStyle(EditorStyles.foldout);
                sectionFoldOuts.active.textColor = title;
                sectionFoldOuts.onActive.textColor = title;
                sectionFoldOuts.normal.textColor = title;
                sectionFoldOuts.onNormal.textColor = title;
                sectionFoldOuts.focused.textColor = title;
                sectionFoldOuts.onFocused.textColor = title;
                sectionFoldOuts.padding.left = 15;
                sectionFoldOuts.margin.left = 15;

                arrayFoldOuts = new GUIStyle(EditorStyles.foldout);
                arrayFoldOuts.active.textColor = EditorStyles.label.normal.textColor;
                arrayFoldOuts.onActive.textColor = EditorStyles.label.normal.textColor;
                arrayFoldOuts.normal.textColor = EditorStyles.label.normal.textColor;
                arrayFoldOuts.onNormal.textColor = EditorStyles.label.normal.textColor;
                arrayFoldOuts.focused.textColor = EditorStyles.label.normal.textColor;
                arrayFoldOuts.onFocused.textColor = EditorStyles.label.normal.textColor;
                arrayFoldOuts.padding.left = 15;
                arrayFoldOuts.margin.left = 15;

                expressionFoldOuts = new GUIStyle(EditorStyles.foldout);
                expressionFoldOuts.active.textColor = EditorStyles.label.normal.textColor;
                expressionFoldOuts.onActive.textColor = EditorStyles.label.normal.textColor;
                expressionFoldOuts.normal.textColor = EditorStyles.label.normal.textColor;
                expressionFoldOuts.onNormal.textColor = EditorStyles.label.normal.textColor;
                expressionFoldOuts.focused.textColor = EditorStyles.label.normal.textColor;
                expressionFoldOuts.onFocused.textColor = EditorStyles.label.normal.textColor;
                expressionFoldOuts.padding.left = 15;
                expressionFoldOuts.margin.left = 40;

                setStyle = false;
            }

            EditorGUILayout.BeginVertical();
            {
                instance.salsa3D = (Salsa3D)EditorGUILayout.ObjectField(
                    new GUIContent("Salsa3D", "Link to a Salsa3D component"), instance.salsa3D, typeof(Salsa3D), true);
                instance.eyeControl = (SalsaUmaSync.EyeControl)EditorGUILayout.EnumPopup(
                    new GUIContent("Eye Control", "Either RandomEyes3D or UMAExpressionPlayer"), instance.eyeControl);
                if (instance.eyeControl == SalsaUmaSync.EyeControl.UseRandomEyes3D)
                {
                    instance.randomEyes3D = EditorGUILayout.ObjectField(
                        new GUIContent("RandomEyes3D", "Link to a RandomEyes3D component"),
                        instance.randomEyes3D, typeof(RandomEyes3D), true) as RandomEyes3D;
                }
                if (prevEyeControl != instance.eyeControl)
                {
                    instance.SetEyeControl(instance.eyeControl);
                    prevEyeControl = instance.eyeControl;
                }
                instance.animatorController = EditorGUILayout.ObjectField(
                    new GUIContent("RuntimeAnimatorController", "Link to a RuntimeAnimatorController"),
                    instance.animatorController, typeof(RuntimeAnimatorController), true) as RuntimeAnimatorController;
                instance.expressionPlayer = EditorGUILayout.ObjectField(
                    new GUIContent("UMAExpressionPlayer", "Link to a UMAExpressionPlayer component"),
                        instance.expressionPlayer, typeof(UMAExpressionPlayer), true) as UMAExpressionPlayer;
                instance.lockShapesOverride = EditorGUILayout.Toggle(
                    new GUIContent("Lock Shapes Override", "Allows animation when SALSA is not driven by the associated AudioSource"),
                        instance.lockShapesOverride);

                GUILayout.Space(20);
                
                EditorGUILayout.BeginVertical(GUI.skin.box);
                {
                    EditorGUILayout.BeginHorizontal();
                    {
                        EditorGUILayout.LabelField("Speech Shapes");
                        if (GUILayout.Button(new GUIContent("Add Shape", "Add a new shape"), GUILayout.Width(100)))
                        {
                            instance.AddShape();
                        }
                    }
                    EditorGUILayout.EndHorizontal();
    
                    if (instance.salsa3D)
                    {
                        if (instance.expressionCount != instance.shapeValueGroups.Count)
                        {
                            instance.Initialize();
                        }
                    }
    
                    if (instance.shapes.Count > 0)
                    {
                        GUILayout.Space(10);
    
                        GUILayout.BeginHorizontal();
                        {
                            EditorGUILayout.LabelField(new GUIContent("Del", "Remove shape"), GUILayout.Width(deleteWidth));
                            EditorGUILayout.LabelField(new GUIContent("ShapeName", "BlendShape - (shapeIndex)"));
                        }
                        GUILayout.EndHorizontal();
    
                        for (int i = 0; i < instance.shapes.Count; i++)
                        {
                            EditorGUILayout.BeginVertical();
                            {
                                EditorGUILayout.BeginHorizontal();
                                {
                                    if (GUILayout.Button(
                                        new GUIContent("X", "Remove this shape (index:" + instance.shapes[i].shape.ToString() + ")"),
                                        GUILayout.Width(deleteWidth)))
                                    {
                                        instance.RemoveShape(i);
                                        break;
                                    }
                                    instance.shapes[i].shape = (SalsaUmaSync.Expression)EditorGUILayout.EnumPopup(
                                        instance.shapes[i].shape);
                                }
                                EditorGUILayout.EndHorizontal();
                            }
                            EditorGUILayout.EndVertical();
                        }
                    }
    
                    GUILayout.Space(10);
    
                    EditorGUILayout.BeginHorizontal();
                    {
                        EditorGUI.indentLevel++;
                        instance.showSpeechValueGroups = EditorGUILayout.Foldout(instance.showSpeechValueGroups, "Speech Group Values");
                        EditorGUI.indentLevel--;
                    }
                    GUILayout.EndHorizontal();
                    if (instance.showSpeechValueGroups)
                    {
                        // Disabled until SALSA 2.0
                        //EditorGUILayout.BeginHorizontal();
                        //{
                        //    EditorGUILayout.LabelField("Speech Value Groups");
                        //    if (GUILayout.Button(new GUIContent("Add Group", "Add a speech value group"), GUILayout.Width(100)))
                        //    {
                        //        instance.AddValueGroup();
                        //    }
                        //}
                        //EditorGUILayout.EndHorizontal();
    
                        if (instance.shapes.Count > 0)
                        {
                            if (instance.shapeValueGroups.Count > 0)
                            {
                                for (int grp = 0; grp < instance.shapeValueGroups.Count; grp++)
                                {
                                    EditorGUILayout.BeginHorizontal();
                                    {
                                        // Disabled until SALSA 2.0
                                        //if (GUILayout.Button(new GUIContent("X", "Delete Group"), GUILayout.Width(deleteWidth)))
                                        //{
                                        //    instance.RemoveValueGroup(grp);
                                        //    break;
                                        //}
                                        EditorGUILayout.LabelField("Group (" + grp + ")");
                                    }
                                    EditorGUILayout.EndHorizontal();
                                    for (int val = 0; val < instance.shapeValueGroups[grp].umaShapeAmounts.Count; val++)
                                    {
                                        EditorGUILayout.BeginVertical();
                                        {
                                            EditorGUILayout.BeginHorizontal();
                                            {
                                                EditorGUILayout.LabelField(new GUIContent("", ""), GUILayout.Width(20f));
                                                EditorGUILayout.LabelField(instance.shapes[val].shape.ToString());
                                                instance.shapeValueGroups[grp].umaShapeAmounts[val].shapePercent =
                                                    EditorGUILayout.Slider(instance.shapeValueGroups[grp].umaShapeAmounts[val].shapePercent, -1f, 1f);
                                            }
                                            EditorGUILayout.EndHorizontal();
                                        }
                                        EditorGUILayout.EndVertical();
                                    }
                                }
                            }
                        }
                    }
                }
                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndVertical();
        }
    }
}