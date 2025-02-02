using UnityEngine;
using UnityEditor;
using XUUtils;
using Unity.Mathematics;
using Unity.VisualScripting;

public class UGLMultiScreenPrefs : XUGenericPeristentDataSingleton<UGLMultiScreenPrefs, UGLMultiScreenPrefsData>
{
    public override bool useSlots => false; 
    #if  UNITY_EDITOR
    [CustomEditor(typeof(UGLMultiScreenPrefs))]
    class Ed : XUGenericPersistentDataEditor<UGLMultiScreenPrefs, UGLMultiScreenPrefsData> { }
    #endif
}

[System.Serializable]
public class UGLMultiScreenPrefsData : XUBaseSaveData
{
    [SerializeField] int[] _screenRemapping = new int[6] { 0,1,2,3,4,5 };
    public int[] screenRemapping => _screenRemapping;

    public bool screenRemappingOk()
    {
        bool ok = true;
        ok &= _screenRemapping.Length == 6;
        for (int soughtI = 0; soughtI < 6 && ok; soughtI++)
        {
            int nFound = 0;
            for (int i =0; i < _screenRemapping.Length; i++)
            {
                if (_screenRemapping[i] == soughtI)
                {
                    nFound++;
                }
            }
            ok &= nFound == 1;
        }
        return ok;
    }
}
