using UnityEngine;
using System;
using System.Text;
namespace XUUtils
{
    public class XUBasicJsonFileReaderWriter<T> : XUGenericPersistentData<T>.IDataReaderWriter where T : XUBaseSaveData, new()
    {
        string _saveDataPath;
        public string getSaveDataPathForSlot(int slot)
        {
            return _saveDataPath + "[" + slot + "]";
        }
        bool _encrypt;
        public XUBasicJsonFileReaderWriter(string saveDataPath, bool encrypt)
        {
            _saveDataPath = saveDataPath;
            _encrypt = encrypt;
        }

        public bool ReadFromDisk(ref T toOverwrite, int slot, string overrideFilePath = null)
        {
            bool foundExistingData = false;

            string readPath = string.IsNullOrEmpty(overrideFilePath) ? getSaveDataPathForSlot(slot) : overrideFilePath;

            string loadedSaveDataJson = XUFileUtil.LoadTextFromDisk(readPath);
            string decryptedSaveDataJson = null;
            if (loadedSaveDataJson != null)
            {
                try
                {
                    decryptedSaveDataJson = loadedSaveDataJson;
                    if (loadedSaveDataJson.Length > 0 && loadedSaveDataJson[0] != '{')
                    {
                        decryptedSaveDataJson = EncryptDecrypt(loadedSaveDataJson);
                    }

                    JsonUtility.FromJsonOverwrite(decryptedSaveDataJson, toOverwrite);
                    foundExistingData = true;
                }
                catch (Exception)
                {
                    //fallback for the original non decrypted json
                    try
                    {

                        JsonUtility.FromJsonOverwrite(loadedSaveDataJson, toOverwrite);
                        foundExistingData = true;
                    }
                    catch (Exception e) //catch corrupted save files
                    {
                        foundExistingData = false;
#if UNITY_EDITOR
                        Debug.LogError($"{this.GetType()} ({(this._encrypt ? "encrypted" : "not encrypted")}) issue loading save file : \n{e}\n\ncontent:\n{loadedSaveDataJson}\n\ndecryptedcontent:\n{decryptedSaveDataJson}");
#else
                    Debug.LogError($"issue loading save file : \n{e}");
#endif
                    }

                    //OnOriginalSaveLoad();
                }
            }

            return foundExistingData;
        }

        public bool WriteToDisk(T toWrite, int slot, string overrideFilePath = null)
        {
            bool successfullyWrote = true;

            string currentState = JsonUtility.ToJson(toWrite, true);
            string encryptedStateString = _encrypt ? EncryptDecrypt(currentState) : currentState;

            string writePath = string.IsNullOrEmpty(overrideFilePath) ? getSaveDataPathForSlot(slot) : overrideFilePath;
            XUFileUtil.WriteStringToFile(encryptedStateString, writePath);

            return successfullyWrote;
        }

        public static string EncryptDecrypt(string szPlainText)
        {

            StringBuilder szInputStringBuild = new StringBuilder(szPlainText);
            StringBuilder szOutStringBuild = new StringBuilder(szPlainText.Length);
            char Textch;
            for (int iCount = 0; iCount < szPlainText.Length; iCount++)
            {
                Textch = szInputStringBuild[iCount];
                Textch = (char)(Textch ^ 173);
                szOutStringBuild.Append(Textch);
            }
            return szOutStringBuild.ToString();
        }

        void XUGenericPersistentData<T>.IDataReaderWriter.DeleteData(int i)
        {
            XUFileUtil.DeleteFile(getSaveDataPathForSlot(i));
        }
    }

    public class XUStubReaderWriter<T> : XUGenericPersistentData<T>.IDataReaderWriter where T : XUBaseSaveData, new()
    {
        public bool ReadFromDisk(ref T toOverwrite, int slot, string overrideFileSuffix)
        {
            Debug.LogError($"Not Writing save data: No IDataReaderWriter set up for platform '{Application.platform}' ???");
            return false;
        }

        public bool WriteToDisk(T toWrite, int slot, string overrideFileSuffix)
        {
            Debug.LogError($"Not Writing save data: No IDataReaderWriter set up for  platform '{Application.platform}' ???");
            return false;
        }

        void XUGenericPersistentData<T>.IDataReaderWriter.DeleteData(int slot)
        {
            Debug.LogError($"Not Deleting save data: No IDataReaderWriter set up for  platform '{Application.platform}' ???");
        }
    }
}