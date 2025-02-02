using UnityEngine;
using UnityEditor;
using XUUtils;

public class UGLMultiScreenPrefs : XUGenericPeristentDataSingleton<UGLMultiScreenPrefs, UGLMultiScreenPrefsData>
{

#if  UNITY_EDITOR
    [CustomEditor(typeof(UGLMultiScreenPrefs))]
    class Ed : XUGenericPersistentDataEditor<UGLMultiScreenPrefs, UGLMultiScreenPrefsData> { }
#endif
}

[System.Serializable]
public class UGLMultiScreenPrefsData : XUBaseSaveData
{
    public int i;
}
