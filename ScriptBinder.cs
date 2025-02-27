using UnityEngine;
using System;
using System.Collections.Generic;


public class ScriptBinder : MonoBehaviour
{

    public enum ScriptType
    {
        GUI,
        Item
    }
    [Serializable]
    public class ComponentBinding
    {
        public string componentName;
        public Component component;
    }

    [SerializeField]
    public List<ComponentBinding> bindings = new List<ComponentBinding>();

    
    [SerializeField]
    public string moduleName = "";
 
    [SerializeField]
    public string scriptName = "";
    
    [SerializeField] 
    public ScriptType scriptType = ScriptType.GUI; // 默认为GUI类型

    public T GetComponent<T>(string name) where T : Component
    {
        var binding = bindings.Find(b => b.componentName == name);
        if (binding != null && binding.component is T component)
        {
            return component;
        }
        Debug.LogWarning($"Component {typeof(T)} with name {name} not found");
        return null;
    }
}