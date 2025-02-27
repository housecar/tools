using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ScriptBinder))]
public class ComponentBinderEditor : UnityEditor.Editor
{
    public ScriptBinder binder;
    private bool showComponents = true;

    private void OnEnable()
    {
        binder = (ScriptBinder)target;
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Add Component Binding"))
        {
            binder.bindings.Add(new ScriptBinder.ComponentBinding());
            EditorUtility.SetDirty(binder);
        }

        EditorGUILayout.EndHorizontal();

        showComponents = EditorGUILayout.Foldout(showComponents, "Component Bindings");

        if (showComponents)
        {
            EditorGUI.indentLevel++;
            for (int i = 0; i < binder.bindings.Count; i++)
            {
                EditorGUILayout.BeginHorizontal(GUILayout.Height(20));

                // 使用更小的宽度绘制名称输入框
                EditorGUIUtility.labelWidth = 60;
                binder.bindings[i].componentName = EditorGUILayout.TextField(
                    "Name",
                    binder.bindings[i].componentName,
                    GUILayout.Width(300)
                );

                // 组件拖拽区域
                EditorGUIUtility.labelWidth = 70;
                var newComponent = EditorGUILayout.ObjectField(
                    binder.bindings[i].component,
                    typeof(Component),
                    true,
                    GUILayout.MinWidth(100)
                ) as Component;

                if (newComponent != binder.bindings[i].component)
                {
                    binder.bindings[i].component = newComponent;
                    EditorUtility.SetDirty(binder);
                }

                // 使用更小的按钮
                if (GUILayout.Button("▼", GUILayout.Width(16), GUILayout.Height(16)))
                {
                    var gameObject = binder.bindings[i].component != null
                        ? binder.bindings[i].component.gameObject
                        : binder.gameObject;

                    var allComponents = gameObject.GetComponents<Component>();
                    var menu = new GenericMenu();

                    for (int j = 0; j < allComponents.Length; j++)
                    {
                        var component = allComponents[j];
                        if (component != null)
                        {
                            var componentName = component.GetType().Name;
                            var index = i; // 捕获循环变量
                            menu.AddItem(
                                new GUIContent(componentName),
                                binder.bindings[i].component == component,
                                () =>
                                {
                                    binder.bindings[index].component = component;
                                    EditorUtility.SetDirty(binder);
                                }
                            );
                        }
                    }

                    menu.ShowAsContext();
                }

                if (GUILayout.Button("×", GUILayout.Width(16), GUILayout.Height(16)))
                {
                    binder.bindings.RemoveAt(i);
                    EditorUtility.SetDirty(binder);
                    i--;
                }

                EditorGUILayout.EndHorizontal();

                // 可选：添加小间距
                // GUILayout.Space(2);
            }

            EditorGUI.indentLevel--;
        }

        EditorGUILayout.Space(10);

        // 添加模块名和脚本名输入框
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.Space(8);

        float originalLabelWidth = EditorGUIUtility.labelWidth;
        EditorGUIUtility.labelWidth = 120;
        binder.moduleName = EditorGUILayout.TextField("Module Name", binder.moduleName, GUILayout.Width(400));
        binder.scriptName = EditorGUILayout.TextField("Script Name", binder.scriptName, GUILayout.Width(400));
        EditorGUIUtility.labelWidth = originalLabelWidth;

        // Script Type 下拉框
        EditorGUI.BeginChangeCheck();
        var newType =
            (ScriptBinder.ScriptType)EditorGUILayout.EnumPopup("Script Type", binder.scriptType, GUILayout.Width(400));
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(binder, "Change Script Type");
            binder.scriptType = newType;
            EditorUtility.SetDirty(binder);
        }


        // Generate 按钮
        EditorGUILayout.BeginHorizontal();
        GUILayout.Space(15); // 保持一致的左边距
        if (GUILayout.Button("Generate Script", GUILayout.Height(24)))
        {
            GenerateScript();
        }

        GUILayout.Space(15); // 右边距
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(2);
        EditorGUILayout.EndVertical();

        serializedObject.ApplyModifiedProperties();
    }

    private void GenerateComponentPaths()
    {
        Debug.Log("=== Component References ===");
        foreach (var binding in binder.bindings)
        {
            if (binding.component != null)
            {
                string path = GetRelativePath(binder.gameObject, binding.component);
                string componentType = binding.component.GetType().Name;

                // 格式化输出
                string output =
                    $"{componentType} {binding.componentName} = transform.Find<{componentType}>(\"{path}\");";
                Debug.Log(output);

                EditorUtility.SetDirty(binder);
            }
            else
            {
                Debug.LogWarning("Found binding with null component");
            }
        }

        Debug.Log("=== End of References ===");
    }

    private string GetRelativePath(GameObject root, Component target)
    {
        var targetGo = target.gameObject;
        var path = new System.Text.StringBuilder();

        // 只构建游戏对象的路径，不包含组件类型
        if (targetGo != root)
        {
            var pathToRoot = new List<string>();
            Transform current = targetGo.transform;

            while (current != null && current.gameObject != root)
            {
                pathToRoot.Add(current.name);
                current = current.parent;
            }

            if (current != null)
            {
                // 从父到子添加路径
                for (int i = pathToRoot.Count - 1; i >= 0; i--)
                {
                    path.Append(pathToRoot[i]);
                    if (i > 0) path.Append("/");
                }
            }
        }

        return path.ToString();
    }

    private void GenerateScript()
    {
        if (string.IsNullOrEmpty(binder.moduleName) || string.IsNullOrEmpty(binder.scriptName))
        {
            Debug.LogError("Module name and script name cannot be empty!");
            return;
        }

        // 构建文件路径
        string directoryPath = $"Assets/Scripts/GamePlay/UI/GUI/{binder.moduleName}";
        string filePath = $"{directoryPath}/{binder.scriptName}.cs";

        // 确保目录存在
        if (!Directory.Exists(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }

        // 生成脚本内容
        var content = new System.Text.StringBuilder();
        content.AppendLine("using FpUI;");
        content.AppendLine("using ProjectM.Core.UI;");
        content.AppendLine("using UnityEngine;");
        content.AppendLine("using UnityEngine.UI;");
        content.AppendLine();
        content.AppendLine($"namespace ProjectM.GamePlay");
        content.AppendLine("{");
        content.AppendLine($"    public class {binder.scriptName} : BaseGUI");
        content.AppendLine("    {");
        content.AppendLine("        #region UI Components");

        // 添加组件引用
        foreach (var binding in binder.bindings)
        {
            if (binding.component != null)
            {
                string path = GetRelativePath(binder.gameObject, binding.component);
                string componentType = binding.component.GetType().Name;
                content.AppendLine($"        public {componentType} {binding.componentName};");
            }
        }

        content.AppendLine("        #endregion");
        content.AppendLine();
        content.AppendLine("        protected override void FindComponents()");
        content.AppendLine("        {");

        // 添加组件初始化代码
        foreach (var binding in binder.bindings)
        {
            if (binding.component != null)
            {
                string path = GetRelativePath(binder.gameObject, binding.component);
                string componentType = binding.component.GetType().Name;
                content.AppendLine(
                    $"            {binding.componentName} = transform.Find<{componentType}>(\"{path}\");");
            }
        }

        content.AppendLine("        }");
        content.AppendLine("    }");
        content.AppendLine("}");

        // 写入文件
        File.WriteAllText(filePath, content.ToString());
        Debug.Log($"Script generated at: {filePath}");

        // 刷新 AssetDatabase 以显示新文件
        AssetDatabase.Refresh();
    }
}