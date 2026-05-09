using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace NDMFVRoidArmPatch.Editor
{
    [CustomEditor(typeof(NDMFVRoidArmPatchComponent))]
    public sealed class NDMFVRoidArmPatchComponentEditor : UnityEditor.Editor
    {
        private enum Language
        {
            Japanese,
            English
        }

        private const string PrefKeyLanguage = "NDMFVRoidArmPatchComponentEditor.Language";
        private const string PrefKeyAdvancedFoldout = "NDMFVRoidArmPatchComponentEditor.AdvancedFoldout";

        private static readonly string[] ConstraintModeJa =
        {
            "VRChat Constraints",
            "Unity Constraints"
        };

        private static readonly string[] ConstraintModeEn =
        {
            "VRChat Constraints",
            "Unity Constraints"
        };

        private static readonly string[] BuildOrderJa =
        {
            "After Modular Avatar",
            "Before Modular Avatar"
        };

        private static readonly string[] BuildOrderEn =
        {
            "After Modular Avatar",
            "Before Modular Avatar"
        };

        private SerializedProperty enableShoulderFixProp;
        private SerializedProperty shoulderPositionOffsetProp;
        private SerializedProperty shoulderEulerOffsetProp;
        private SerializedProperty upperArmRollAxisProp;
        private SerializedProperty upperArmRollWeightProp;

        private SerializedProperty enableWristFixProp;
        private SerializedProperty wristThicknessScaleProp;
        private SerializedProperty wristWidthScaleProp;
        private SerializedProperty wristRollAxisProp;
        private SerializedProperty wristRollWeightProp;
        private SerializedProperty wristTwistBoneTypeProp;
        private SerializedProperty wristTwistBoneCountProp;
        private SerializedProperty wristSkinMaterialNameProp;

        private SerializedProperty enableThumbFixProp;
        private SerializedProperty thumbEulerOffsetProp;

        private SerializedProperty constraintModeProp;
        private SerializedProperty buildOrderProp;
        private SerializedProperty verboseLogProp;

        private Language language;
        private bool advancedFoldout;

        private const float MainLabelWidth = 84f;
        private const float SubLabelWidth = 110f;
        private const float Gap = 8f;
        private const float ToggleWidth = 16f;

        private void OnEnable()
        {
            enableShoulderFixProp = serializedObject.FindProperty("enableShoulderFix");
            shoulderPositionOffsetProp = serializedObject.FindProperty("shoulderPositionOffset");
            shoulderEulerOffsetProp = serializedObject.FindProperty("shoulderEulerOffset");
            upperArmRollAxisProp = serializedObject.FindProperty("upperArmRollAxis");
            upperArmRollWeightProp = serializedObject.FindProperty("upperArmRollWeight");

            enableWristFixProp = serializedObject.FindProperty("enableWristFix");
            wristThicknessScaleProp = serializedObject.FindProperty("wristThicknessScale");
            wristWidthScaleProp = serializedObject.FindProperty("wristWidthScale");
            wristRollAxisProp = serializedObject.FindProperty("wristRollAxis");
            wristRollWeightProp = serializedObject.FindProperty("wristRollWeight");
            wristTwistBoneTypeProp = serializedObject.FindProperty("wristTwistBoneType");
            wristTwistBoneCountProp = serializedObject.FindProperty("wristTwistBoneCount");
            wristSkinMaterialNameProp = serializedObject.FindProperty("wristSkinMaterialName");

            enableThumbFixProp = serializedObject.FindProperty("enableThumbFix");
            thumbEulerOffsetProp = serializedObject.FindProperty("thumbEulerOffset");

            constraintModeProp = serializedObject.FindProperty("constraintMode");
            buildOrderProp = serializedObject.FindProperty("buildOrder");
            verboseLogProp = serializedObject.FindProperty("verboseLog");

            language = (Language)EditorPrefs.GetInt(PrefKeyLanguage, 0);
            advancedFoldout = EditorPrefs.GetBool(PrefKeyAdvancedFoldout, false);
        }

        public override void OnInspectorGUI()
        {
            var component = (NDMFVRoidArmPatchComponent)target;
            var avatarRoot = FindAvatarRootForComponent(component);
            bool isPreviewing = NDMFVRoidArmPatchPreviewUtility.IsPreviewing(avatarRoot);

            serializedObject.Update();
            EditorGUI.BeginChangeCheck();

            DrawTopRow(component, isPreviewing);
            EditorGUILayout.Space(4);

            DrawInfoBox();
            EditorGUILayout.Space(6);

            DrawShoulderRows();
            EditorGUILayout.Space(2);
            DrawWristRows(component);
            EditorGUILayout.Space(2);
            DrawThumbRow();
            EditorGUILayout.Space(8);

            DrawAdvancedSection();

            bool changed = EditorGUI.EndChangeCheck();
            serializedObject.ApplyModifiedProperties();

            if (changed && isPreviewing)
            {
                NDMFVRoidArmPatchPreviewUtility.RestartPreviewIfActive(component);
            }
        }

        private void DrawTopRow(NDMFVRoidArmPatchComponent component, bool isPreviewing)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                var oldColor = GUI.backgroundColor;
                GUI.backgroundColor = isPreviewing ? new Color(0.4f, 0.85f, 0.4f) : oldColor;

                if (GUILayout.Button(T("Preview", "Preview"), GUILayout.Width(90f), GUILayout.Height(20f)))
                {
                    serializedObject.ApplyModifiedProperties();
                    NDMFVRoidArmPatchPreviewUtility.TogglePreview(component);
                    GUIUtility.ExitGUI();
                }

                GUI.backgroundColor = oldColor;

                GUILayout.FlexibleSpace();

                EditorGUI.BeginChangeCheck();
                var newLanguage = (Language)EditorGUILayout.EnumPopup(language, GUILayout.Width(90f));
                if (EditorGUI.EndChangeCheck())
                {
                    language = newLanguage;
                    EditorPrefs.SetInt(PrefKeyLanguage, (int)language);
                }
            }
        }

        private void DrawInfoBox()
        {
            int constraintCount = 0;

            if (enableShoulderFixProp.boolValue) constraintCount += 4;
            if (enableWristFixProp.boolValue) constraintCount += 2;
            if (enableThumbFixProp.boolValue) constraintCount += 6;

            string message = T(
                $"・肘周辺がわずかに歪む場合があります。\n・現在の設定で増える Constraint 数: {constraintCount} 個",
                $"• Elbow area may deform slightly.\n• Additional constraints with current settings: {constraintCount}"
            );

            EditorGUILayout.HelpBox(message, MessageType.Info);
        }

        private void DrawShoulderRows()
        {
            DrawShoulderMainRow();

            using (new EditorGUI.DisabledScope(!enableShoulderFixProp.boolValue))
            {
                DrawShoulderSubRow(
                    T("Roll Weight", "Roll Weight"),
                    upperArmRollWeightProp,
                    T("ねじれ軸だけ元 UpperArm に寄せる強さ。", "How strongly the twist axis follows the original UpperArm.")
                );

                DrawShoulderSubRow(
                    T("Position Offset", "Position Offset"),
                    shoulderPositionOffsetProp,
                    T(
                        "肩ボーンの位置オフセット。 Yに0.01ほど入れるとVRM Converter for VRChatと近い結果になります。",
                        "Local position offset relative to the shoulder bone."
                    )
                );

                DrawShoulderSubRow(
                    T("Euler Offset", "Euler Offset"),
                    shoulderEulerOffsetProp,
                    T(
                        "肩に加える回転オフセット。右肩は内部で自動反転して適用されます。",
                        "Rotation offset applied to shoulders. The right shoulder is mirrored internally."
                    )
                );
            }
        }

        private void DrawShoulderMainRow()
        {
            Rect rect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight);

            Rect toggleRect = new Rect(rect.x, rect.y, ToggleWidth, rect.height);
            Rect mainLabelRect = new Rect(toggleRect.xMax + 2f, rect.y, MainLabelWidth, rect.height);
            Rect subLabelRect = new Rect(mainLabelRect.xMax + Gap, rect.y, SubLabelWidth, rect.height);
            Rect valueRect = new Rect(subLabelRect.xMax + 4f, rect.y, rect.xMax - (subLabelRect.xMax + 4f), rect.height);

            enableShoulderFixProp.boolValue = EditorGUI.Toggle(toggleRect, enableShoulderFixProp.boolValue);
            EditorGUI.LabelField(mainLabelRect, T("Shoulder Fix", "Shoulder Fix"));

            using (new EditorGUI.DisabledScope(!enableShoulderFixProp.boolValue))
            {
                EditorGUI.LabelField(
                    subLabelRect,
                    new GUIContent(
                        T("Roll Axis", "Roll Axis"),
                        T("ねじれ補正で使う軸。初期値は X。", "Axis used for twist correction. Default is X.")
                    )
                );
                DrawAxisToolbar(valueRect, upperArmRollAxisProp);
            }
        }

        private void DrawWristRows(NDMFVRoidArmPatchComponent component)
        {
            var wristScaleAxes = GetWristScaleAxisLabels();

            Rect rect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight);

            Rect toggleRect = new Rect(rect.x, rect.y, ToggleWidth, rect.height);
            Rect mainLabelRect = new Rect(toggleRect.xMax + 2f, rect.y, MainLabelWidth, rect.height);
            Rect subLabelRect = new Rect(mainLabelRect.xMax + Gap, rect.y, SubLabelWidth, rect.height);
            Rect valueRect = new Rect(subLabelRect.xMax + 4f, rect.y, rect.xMax - (subLabelRect.xMax + 4f), rect.height);

            enableWristFixProp.boolValue = EditorGUI.Toggle(toggleRect, enableWristFixProp.boolValue);
            EditorGUI.LabelField(
                mainLabelRect,
                new GUIContent(
                    T("Wrist Fix", "Wrist Fix"),
                    T(
                        "前腕の見た目骨にスケール補正と手首 twist 補正を同時に適用します。",
                        "Applies both scale correction and wrist twist correction to the forearm display bone."
                    )
                )
            );

            using (new EditorGUI.DisabledScope(!enableWristFixProp.boolValue))
            {
                EditorGUI.LabelField(
                    subLabelRect,
                    new GUIContent(
                        T("Roll Axis", "Roll Axis"),
                        T("手首回転の twist 軸です。", "Twist axis for wrist rotation.")
                    )
                );
                DrawAxisToolbar(valueRect, wristRollAxisProp);

                using (new EditorGUI.DisabledScope(IsWristTwistBoneModeActive()))
                {
                    DrawShoulderSubRow(
                        T("Roll Weight", "Roll Weight"),
                        wristRollWeightProp,
                        T("手首の roll を前腕へ伝える強さ。", "How strongly hand roll is transferred to the forearm.")
                    );
                }

                DrawShoulderSubRow(
                    T($"Thickness ({wristScaleAxes.thicknessAxis})", $"Thickness ({wristScaleAxes.thicknessAxis})"),
                    wristThicknessScaleProp,
                    T(
                        $"前腕の厚み補正。現在は local {wristScaleAxes.thicknessAxis} に適用されます。",
                        $"Forearm thickness adjustment. Currently applied on local {wristScaleAxes.thicknessAxis}."
                    )
                );

                DrawShoulderSubRow(
                    T($"Width ({wristScaleAxes.widthAxis})", $"Width ({wristScaleAxes.widthAxis})"),
                    wristWidthScaleProp,
                    T(
                        $"前腕の幅補正。現在は local {wristScaleAxes.widthAxis} に適用されます。",
                        $"Forearm width adjustment. Currently applied on local {wristScaleAxes.widthAxis}."
                    )
                );

                DrawTwistBoneTypeRow();
                DrawTwistBoneCountRow();
                DrawWristSkinMaterialRow(component);
            }
        }

        private void DrawThumbRow()
        {
            DrawInlineRow(
                enableThumbFixProp,
                T("Thumb Fix", "Thumb Fix"),
                thumbEulerOffsetProp,
                T("Euler Offset", "Euler Offset"),
                T(
                    "親指全体に加える回転オフセット。右手は内部で自動反転して適用されます。VRM0.0経由のVRoidは(0,-15,-15)がおすすめです。",
                    "Shared thumb rotation offset. Right side is mirrored internally."
                )
            );
        }

        private bool IsWristTwistBoneModeActive()
        {
            return wristTwistBoneTypeProp.enumValueIndex != (int)WristTwistBoneType.None;
        }

        private void DrawWristSkinMaterialRow(NDMFVRoidArmPatchComponent component)
        {
            if ((WristTwistBoneType)wristTwistBoneTypeProp.enumValueIndex != WristTwistBoneType.SkinOnly) return;

            var candidates = CollectMaterialCandidates(component);
            if (candidates.Count == 0)
            {
                EditorGUILayout.HelpBox(T("Skin Material候補が見つかりません。", "No Skin Material candidates were found."), MessageType.Warning);
                return;
            }

            int selected = candidates.IndexOf(wristSkinMaterialNameProp.stringValue);
            if (string.IsNullOrEmpty(wristSkinMaterialNameProp.stringValue) || selected < 0)
            {
                string inferred = InferSkinMaterialName(candidates);
                wristSkinMaterialNameProp.stringValue = inferred;
                selected = candidates.IndexOf(inferred);
                if (!ContainsBodyAndSkin(inferred))
                {
                    EditorGUILayout.HelpBox(T("肌マテリアルを推定できなかったため先頭候補を使用します。", "Could not infer a skin material; using the first candidate."), MessageType.Warning);
                }
            }

            int next = DrawSubRowPopup(T("Skin Material", "Skin Material"), candidates.ToArray(), selected, T("SkinOnlyモードで使う実マテリアル名。", "Material name used in SkinOnly mode."));
            wristSkinMaterialNameProp.stringValue = candidates[next];
        }

        private void DrawTwistBoneTypeRow()
        {
            Rect rect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight);
            Rect spacerRect = new Rect(rect.x, rect.y, ToggleWidth, rect.height);
            Rect mainLabelRect = new Rect(spacerRect.xMax + 2f, rect.y, MainLabelWidth, rect.height);
            Rect subLabelRect = new Rect(mainLabelRect.xMax + Gap, rect.y, SubLabelWidth, rect.height);
            Rect valueRect = new Rect(subLabelRect.xMax + 4f, rect.y, rect.xMax - (subLabelRect.xMax + 4f), rect.height);

            EditorGUI.LabelField(mainLabelRect, GUIContent.none);
            EditorGUI.LabelField(subLabelRect, new GUIContent(T("Twist Bone Type", "Twist Bone Type"), GetTwistBoneTypeTooltip()));

            string[] labels = { "None", "AllTwist", "SkinOnly" };
            wristTwistBoneTypeProp.enumValueIndex = EditorGUI.Popup(valueRect, wristTwistBoneTypeProp.enumValueIndex, labels);
        }

        private void DrawTwistBoneCountRow()
        {
            string[] countLabels = { "4", "6", "8", "12" };
            int[] enumIndices =
            {
                (int)WristTwistBoneCount.Count4,
                (int)WristTwistBoneCount.Count6,
                (int)WristTwistBoneCount.Count8,
                (int)WristTwistBoneCount.Count12
            };

            int selectedIdx = Array.IndexOf(enumIndices, wristTwistBoneCountProp.intValue);
            if (selectedIdx < 0) selectedIdx = 2;

            Rect rect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight);
            Rect spacerRect = new Rect(rect.x, rect.y, ToggleWidth, rect.height);
            Rect mainLabelRect = new Rect(spacerRect.xMax + 2f, rect.y, MainLabelWidth, rect.height);
            Rect subLabelRect = new Rect(mainLabelRect.xMax + Gap, rect.y, SubLabelWidth, rect.height);
            Rect valueRect = new Rect(subLabelRect.xMax + 4f, rect.y, rect.xMax - (subLabelRect.xMax + 4f), rect.height);

            EditorGUI.LabelField(mainLabelRect, GUIContent.none);
            EditorGUI.LabelField(subLabelRect, new GUIContent(T("Twist Bone Count", "Twist Bone Count"), T("追加する前腕ツイストボーン数。", "Number of forearm twist bones to use.")));
            int next = GUI.Toolbar(valueRect, selectedIdx, countLabels);
            wristTwistBoneCountProp.intValue = enumIndices[next];
        }

        private int DrawSubRowPopup(string label, string[] options, int selectedIndex, string tooltip)
        {
            Rect rect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight);
            Rect spacerRect = new Rect(rect.x, rect.y, ToggleWidth, rect.height);
            Rect mainLabelRect = new Rect(spacerRect.xMax + 2f, rect.y, MainLabelWidth, rect.height);
            Rect subLabelRect = new Rect(mainLabelRect.xMax + Gap, rect.y, SubLabelWidth, rect.height);
            Rect valueRect = new Rect(subLabelRect.xMax + 4f, rect.y, rect.xMax - (subLabelRect.xMax + 4f), rect.height);

            EditorGUI.LabelField(mainLabelRect, GUIContent.none);
            EditorGUI.LabelField(subLabelRect, new GUIContent(label, tooltip));
            return EditorGUI.Popup(valueRect, selectedIndex, options);
        }

        private static void DrawAxisToolbar(Rect rect, SerializedProperty axisProperty)
        {
            string[] axisLabels = { "X", "Y", "Z" };
            axisProperty.enumValueIndex = GUI.Toolbar(rect, axisProperty.enumValueIndex, axisLabels);
        }

        private static List<string> CollectMaterialCandidates(NDMFVRoidArmPatchComponent component)
        {
            var results = new List<string>();
            if (component == null) return results;

            var avatarRoot = FindAvatarRootForComponent(component);
            Transform searchRoot = avatarRoot != null ? avatarRoot.transform : component.transform;

            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var renderer in searchRoot.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            foreach (var mat in renderer.sharedMaterials)
            {
                if (mat == null || string.IsNullOrEmpty(mat.name)) continue;
                if (seen.Add(mat.name)) results.Add(mat.name);
            }
            return results;
        }

        private static string InferSkinMaterialName(List<string> candidates)
        {
            foreach (var c in candidates) if (ContainsBodyAndSkin(c)) return c;
            return candidates[0];
        }

        private static bool ContainsBodyAndSkin(string name)
        {
            string lower = name.ToLowerInvariant();
            return lower.Contains("body") && lower.Contains("skin");
        }

        private string GetTwistBoneTypeTooltip()
        {
            switch ((WristTwistBoneType)wristTwistBoneTypeProp.enumValueIndex)
            {
                case WristTwistBoneType.None:
                    return T("ツイストボーンを追加しないため、コンストレイント数を節約できます。肘のズレが気になるケースがあります。", "No twist bones are added. This saves constraints, but elbow offset may remain.");
                case WristTwistBoneType.AllTwist:
                    return T("前腕・手首ウェイトを持つ頂点をすべてツイストボーンへ再配分します。通常はこちらを使用してください。", "Redistributes all forearm/wrist weighted vertices to twist bones. Recommended.");
                case WristTwistBoneType.SkinOnly:
                    return T("選択した肌マテリアルの頂点のみツイストします。肌以外で前腕・手首ウェイトを持つ頂点は、袖のねじれ破綻を避けるため根本ツイストボーンへ固定します。手袋やリストバンドでは不自然になる場合があります。", "Only selected skin-material vertices are twisted; non-skin forearm/wrist vertices are fixed to root twist and may look odd on gloves/wristbands.");
                default:
                    return string.Empty;
            }
        }

        private void DrawShoulderSubRow(string label, SerializedProperty property, string tooltip)
        {
            Rect rect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight);

            Rect spacerRect = new Rect(rect.x, rect.y, ToggleWidth, rect.height);
            Rect mainLabelRect = new Rect(spacerRect.xMax + 2f, rect.y, MainLabelWidth, rect.height);
            Rect subLabelRect = new Rect(mainLabelRect.xMax + Gap, rect.y, SubLabelWidth, rect.height);
            Rect valueRect = new Rect(subLabelRect.xMax + 4f, rect.y, rect.xMax - (subLabelRect.xMax + 4f), rect.height);

            EditorGUI.LabelField(mainLabelRect, GUIContent.none);
            EditorGUI.LabelField(subLabelRect, new GUIContent(label, tooltip));
            EditorGUI.PropertyField(valueRect, property, GUIContent.none);
        }

        private void DrawInlineRow(
            SerializedProperty enableProp,
            string label,
            SerializedProperty valueProp,
            string valueLabel,
            string valueTooltip)
        {
            Rect rect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight);

            Rect toggleRect = new Rect(rect.x, rect.y, ToggleWidth, rect.height);
            Rect labelRect = new Rect(toggleRect.xMax + 2f, rect.y, MainLabelWidth, rect.height);
            Rect valueLabelRect = new Rect(labelRect.xMax + Gap, rect.y, SubLabelWidth, rect.height);
            Rect valueRect = new Rect(valueLabelRect.xMax + 4f, rect.y, rect.xMax - (valueLabelRect.xMax + 4f), rect.height);

            enableProp.boolValue = EditorGUI.Toggle(toggleRect, enableProp.boolValue);
            EditorGUI.LabelField(labelRect, label);

            using (new EditorGUI.DisabledScope(!enableProp.boolValue))
            {
                EditorGUI.LabelField(valueLabelRect, new GUIContent(valueLabel, valueTooltip));
                EditorGUI.PropertyField(valueRect, valueProp, GUIContent.none);
            }
        }

        private void DrawAdvancedSection()
        {
            EditorGUI.BeginChangeCheck();
            advancedFoldout = EditorGUILayout.Foldout(advancedFoldout, T("Advanced", "Advanced"), true);
            if (EditorGUI.EndChangeCheck())
            {
                EditorPrefs.SetBool(PrefKeyAdvancedFoldout, advancedFoldout);
            }

            if (!advancedFoldout) return;

            EditorGUI.indentLevel++;

            DrawConstraintModePopup();
            DrawBuildOrderPopup();

            EditorGUILayout.PropertyField(verboseLogProp, new GUIContent(T("Verbose Log", "Verbose Log")));
            EditorGUILayout.Space(4);

            Rect rawButtonRect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight);
            Rect buttonRect = EditorGUI.IndentedRect(rawButtonRect);
            if (GUI.Button(buttonRect, T("Reset Preview", "Reset Preview")))
            {
                NDMFVRoidArmPatchPreviewUtility.ResetAllPreviewArtifacts();
                GUIUtility.ExitGUI();
            }

            EditorGUILayout.HelpBox(
                T(
                    "モデルが重複したり、見えない場合に押してください。\nPreview オブジェクトを削除し、Renderer を再表示します。",
                    "Use this if the avatar stays hidden, frozen, or stuck after Preview.\nThis removes temporary Preview objects and re-enables renderers."
                ),
                MessageType.Warning
            );

            EditorGUI.indentLevel--;
        }

        private void DrawConstraintModePopup()
        {
            var options = language == Language.Japanese ? ConstraintModeJa : ConstraintModeEn;
            int current = constraintModeProp.enumValueIndex;

            EditorGUI.BeginChangeCheck();
            int next = EditorGUILayout.Popup(
                new GUIContent(
                    T("Constraint Mode", "Constraint Mode"),
                    T(
                        "Unity Constraintsは、 VRChat以外用のオプションです。",
                        "Choose between VRChat Constraints and Unity Constraints. Unity Constraints is intended for compatibility-oriented workflows where later conversion may be handled by other tools."
                    )
                ),
                current,
                options
            );
            if (EditorGUI.EndChangeCheck())
            {
                constraintModeProp.enumValueIndex = next;
            }
        }

        private void DrawBuildOrderPopup()
        {
            var options = language == Language.Japanese ? BuildOrderJa : BuildOrderEn;
            int current = buildOrderProp.enumValueIndex;

            EditorGUI.BeginChangeCheck();
            int next = EditorGUILayout.Popup(
                new GUIContent(
                    T("Build Order", "Build Order"),
                    T(
                        "Afterは、MAで後から追加される衣装やパーツも処理対象にできます。 Beforeは、このツールで追加したコンストレイントをMAで処理したい場合に使用します。",
                        "After Modular Avatar is recommended when clothing or parts are added later by Modular Avatar. Before Modular Avatar is for workflows where generated content needs to exist before later conversion steps."
                    )
                ),
                current,
                options
            );
            if (EditorGUI.EndChangeCheck())
            {
                buildOrderProp.enumValueIndex = next;
            }
        }

        private (string thicknessAxis, string widthAxis) GetWristScaleAxisLabels()
        {
            var axis = (TwistAxis)wristRollAxisProp.enumValueIndex;

            switch (axis)
            {
                case TwistAxis.X:
                    return ("Y", "Z");
                case TwistAxis.Y:
                    return ("Z", "X");
                case TwistAxis.Z:
                    return ("Y", "X");
                default:
                    return ("Y", "Z");
            }
        }

        private static GameObject FindAvatarRootForComponent(NDMFVRoidArmPatchComponent component)
        {
            if (component == null) return null;

            Transform current = component.transform;
            while (current != null)
            {
                var animator = current.GetComponent<Animator>();
                if (animator != null && animator.avatar != null && animator.avatar.isHuman)
                {
                    return animator.gameObject;
                }

                current = current.parent;
            }

            return null;
        }

        private string T(string ja, string en)
        {
            return language == Language.Japanese ? ja : en;
        }
    }
}
