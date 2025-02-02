/*
[[ Data ]]]
This is the live save data publicly accessible by the game.  You can either write 
an invidual field to disk, or write all at once. (Writing individually is 
preferable, so you won't accidentally write some other save state that was set 
temporarily, e.g. when replaying an earlier scene).

'Data' is not directly written to disk, but is copied into into the private 
'writeCache' per field, or completely as needed.  This field is what ultimately
gets written to disk.
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Text;
using System;

using System.Reflection;
using Type = System.Type;

namespace XUUtils
{
    [System.Serializable]
    public class XUBaseSaveData
    {
        public XUDateTime lastModified;
        public XUDateTime timeCreated;
    }

    public class XUGenericPersistentData<T> : MonoBehaviour where T : XUBaseSaveData, new()
    {
        #region reading writing genericization
        public interface IDataReaderWriter
        {
            bool ReadFromDisk(ref T toOverwrite, int slot = 0, string overrideFilePath = null);
            bool WriteToDisk(T toWrite, int slot = 0, string overrideFilePath = null);
            void DeleteData(int slot);
        }
        #endregion

        protected virtual IDataReaderWriter CreateReaderWriter()
        {
            return
#if (UNITY_STANDALONE || UNITY_EDITOR || UNITY_ANDROID)
            new XUBasicJsonFileReaderWriter<T>(baseSaveDataPath, encrypt);
#else
                    new XUStubReaderWriter<T>();
#endif
        }

        public virtual bool useSlots => true;
        protected int _slot = 0;
        public int activeSlot => _slot;
        public void changeSlot(int newSlot)
        {
            if (_slot != newSlot)
            {
                _slot = newSlot;
                if (Application.isPlaying)
                {
                    loadDataFromDisk();
                }
            }
        }

        protected IDataReaderWriter _dataReaderWriter = null;
        public virtual IDataReaderWriter dataReaderWriter
        {
            get
            {
                if (_dataReaderWriter == null)
                {
                    _dataReaderWriter = CreateReaderWriter();
                }
                return _dataReaderWriter;
            }
        }


        #region FIELDS AND PROPERTIES
        [Header("--- EDITOR ONLY SETTINGS ---")]

        [SerializeField]
        protected bool _useInspectorSetValuesEditorOnly = false;

        [SerializeField]
        protected bool _suppresSaves = false;

        protected virtual bool encrypt => false;

#if UNITY_EDITOR
        bool useInspectorSetValuesEditorOnly => _useInspectorSetValuesEditorOnly;
        bool suppresSaves => _suppresSaves;
#else
    bool useInspectorSetValuesEditorOnly => false;
    bool suppresSaves => false;
#endif

        [Header("--- SAVE DATA ---")]
        [SerializeField]
        protected T _currentSaveData;

        bool _flushPerField = true;
        protected T _writeCache = null;

        bool _loadedOnce = false;

        public virtual T currentSaveData
        {
            get
            {
                if ((_currentSaveData == null || !_loadedOnce) && Application.isPlaying)
                {
                    loadDataFromDisk();
                }

                return _currentSaveData;
            }

            set
            {
                _currentSaveData = value;
            }
        }

        public virtual string baseSaveDataPath =>

            Application.persistentDataPath
            + "/"
#if ST_STEAM_VR && !UNITY_ANDROID
        + (string.IsNullOrEmpty(SteamManager.UserIDPrefix) ? "" : SteamManager.UserIDPrefix + "_")
#endif
            + this.GetType().ToString();



        static Events _events = new Events();
        public static Events events => _events;

        public T data
        {
            get
            {
                if (currentSaveData == null)
                {
                    loadDataFromDisk();
                }
                return currentSaveData;
            }
        }
        #endregion


        protected virtual void Awake()
        {
            if (_currentSaveData == null)
            {
                _currentSaveData = new T();
            }

            setUpAttributedFields();

            loadDataFromDisk();

            NotifySubscriberGeneric((subscribingField) => subscribingField.OnInitialize());
        }

        //protected List<FieldInfo> interfaceFields = new List<FieldInfo>();
        protected List<FieldInfo> allFields = new List<FieldInfo>();
        void setUpAttributedFields()
        {
            //Fiend fields in the data that implement the subscriber interface.
            FieldInfo[] saveDataFields;
            Type myType = typeof(T);
            saveDataFields = myType.GetFields(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);
            foreach (var fi in saveDataFields)
            {
                //if (fi.FieldType.GetInterfaces().ContainsElement(typeof(ISTSimpleSaveSubscriber)))
                //{
                //    interfaceFields.Add(fi);
                //}
                allFields.Add(fi);
            }
        }

        void NotifySubscribersPreSave() => NotifySubscriberGeneric((subscribingField) => subscribingField.OnPreSave());
        void NotifySubscriberGeneric(System.Action<ISTSimpleSaveSubscriber> executer)
        {
            if (!Application.isPlaying) return;

            foreach (var fi in allFields)
            {
                ISTSimpleSaveSubscriber sub = fi.GetValue(this._currentSaveData) as ISTSimpleSaveSubscriber;
                if (sub != null) executer(sub);
            }
        }

        public T getSlotData(int slot)
        {
            T ret = new T(); //always reset the object if loading from disk
            bool wasOk = dataReaderWriter.ReadFromDisk(ref ret, slot);
            if (!wasOk)
            {
                ret = null;
            }
            return ret;
        }

        public void loadDataFromDisk(string overrideFilePath = null)
        {
            if (Application.isPlaying)
            {
                _loadedOnce = true;
            }

            if (useInspectorSetValuesEditorOnly)
            {
                if (_currentSaveData == null)
                {
                    _currentSaveData = new T();
                }
                _writeCache = DeepCopy(_currentSaveData);
                return;
            }

            _currentSaveData = new T(); //always reset the object if loading from disk
            bool wasOk = dataReaderWriter.ReadFromDisk(ref _currentSaveData, _slot, overrideFilePath);

            if (_flushPerField)
            {
                _writeCache = DeepCopy(_currentSaveData);
            }

            if (!wasOk)
            {
                Debug.LogWarning("Getting Fresh Save");
            }
        }

        bool _isDirty = false;
        float _dirtyTimer = 0;
        const float DIRTY_TIMEOUT_SECONDS = 1;

        void startDirtyTimeout()
        {
            _isDirty = true;
            _dirtyTimer = DIRTY_TIMEOUT_SECONDS;
        }

        private void Update()
        {
            if (_isDirty && _dirtyTimer > 0)
            {
                _dirtyTimer -= Time.deltaTime;
                if (_dirtyTimer <= 0)
                {
                    _isDirty = false;
#if UNITY_EDITOR
                    Debug.Log($"dirty timeout on '{this.GetType()}', writing to disk");
#endif
                    saveWriteCacheToDisk();
                }
            }
        }

        protected void copyAllToWriteCache()
        {
            if (_currentSaveData == null)
            {
                _currentSaveData = new();
            }
            _writeCache = DeepCopy(_currentSaveData);
        }

        public void saveAll(bool writeToDiskImmediately = false, string overridePath = null)
        {
            //write ALL proprerties to the write cache
            copyAllToWriteCache();
            if (writeToDiskImmediately || !Application.isPlaying)
            {
                saveWriteCacheToDisk(overridePath);
            }
            else
            {
                startDirtyTimeout();
            }
        }

        public void saveFieldValueType(string fieldName, bool writeToDiskImmediately = false)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying && allFields.Count == 0)
            {
                setUpAttributedFields();
            }
#endif

            var fi = allFields.BruteFindFirst((fi) => fi.Name == fieldName, out int idx);
            if (fi == null)
            {
                Debug.LogError("Failed to to dirty the save field... are you trying to dirty something that is not a member of the save data");
            }
            else if (!fi.FieldType.IsValueType)
            {
                Debug.LogError("saveFieldValueType is only for primitives like int,bool,float etc... please use saveFile() instead");
            }
            else
            {
                fi.SetValue(_writeCache, fi.GetValue(_currentSaveData));
                if (writeToDiskImmediately || !Application.isPlaying)
                {
                    saveWriteCacheToDisk();
                }
                else
                {
                    startDirtyTimeout();
                }
            }
        }

        public void saveField<F>(F fieldToDirty, bool writeToDiskImmediately = false) where F : class
        {
#if UNITY_EDITOR
            if (!Application.isPlaying && allFields.Count == 0)
            {
                setUpAttributedFields();
            }
#endif

            if (typeof(T).IsValueType)
            {
                Debug.LogError("saveField is only for reference types/classes/nullable objects etc... please use saveFileValueType() instead");
            }

            bool successful = false;
            foreach (var fi in this.allFields)
            {
#if UNITY_EDITOR
                if (typeof(F) == typeof(string) && fi.Name == fieldToDirty as string)
                {
                    Debug.LogError($"Trying to save a string that's the name of a save field!\nYou probably meant to call '{nameof(saveFieldValueType)}()'");
                    break;
                }
#endif

                if (ReferenceEquals(fieldToDirty, fi.GetValue(this._currentSaveData)))
                {
                    var copiedField = DeepCopy(fieldToDirty);
                    fi.SetValue(_writeCache, copiedField);
                    successful = true;

                }
            }

            if (!successful)
            {
                Debug.LogError("Failed to to dirty the save field... are you trying to dirty something that is not a member of the save data");
            }
            else if (writeToDiskImmediately || !Application.isPlaying)
            {
                saveWriteCacheToDisk();
            }
            else
            {
                startDirtyTimeout();
            }
        }

        public bool revertField<F>(F fieldToRevert)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying && allFields.Count == 0)
            {
                setUpAttributedFields();
            }
#endif

            foreach (var fi in this.allFields)
            {
                if (ReferenceEquals(fieldToRevert, fi.GetValue(this._currentSaveData)))
                {
                    var savedValue = DeepCopy(fi.GetValue(_writeCache));
                    fi.SetValue(_currentSaveData, savedValue);
                    return true;
                }
            }
            return false;
        }

        protected internal void saveWriteCacheToDisk(string overridePath = null, bool updateCreateAndModifiedTimes = true)
        {
            if (!suppresSaves || !Application.isPlaying) //allow saves outside play mode
            {
                var toWrite = _currentSaveData;
                if (_flushPerField)
                {
                    toWrite = _writeCache;
                }

                if (updateCreateAndModifiedTimes)
                {
                    toWrite.lastModified = System.DateTime.Now;
                    if (!toWrite.timeCreated.valid)
                    {
                        toWrite.timeCreated = System.DateTime.Now;
                    }
                }

                NotifySubscribersPreSave();

                dataReaderWriter.WriteToDisk(toWrite, _slot, overridePath);

                if (Application.isPlaying)
                {
                    events.OnSaveDataUpdated.Invoke();
                }
            }
        }


        public void deleteSaveData(int i = -1)
        {
            _isDirty = false;

            dataReaderWriter.DeleteData(i >= 0 ? i : _slot);
            currentSaveData = new T();
            events.OnSaveDataUpdated.Invoke();
        }
        public void deleteAllSaveData()
        {
            _isDirty = false;
            for (int i = 0; i < 3; i++)
            {
                dataReaderWriter.DeleteData(i);
            }
            currentSaveData = new T();
            events.OnSaveDataUpdated.Invoke();
        }

        public class Events
        {
            public System.Action OnSaveDataUpdated = () => { };
        }

        #region Object Copying
        public static void CloneObjectInto<V>(V src, ref V dest)
        {
            System.Type t = typeof(V);
            var srcCopy = DeepCopy(src);
            foreach (FieldInfo fi in t.GetFields())
            {
                fi.SetValue(dest, fi.GetValue(srcCopy));
            }
        }

        public static C DeepCopy<C>(C obj)
        {
            using (var memStream = new System.IO.MemoryStream())
            {
                var bFormatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
                bFormatter.Serialize(memStream, obj);
                memStream.Position = 0;

                return (C)bFormatter.Deserialize(memStream);
            }
        }
        #endregion
    }

    public class XUGenericPeristentDataSingleton<T, V> : XUGenericPersistentData<V> where T : XUGenericPeristentDataSingleton<T, V> where V : XUBaseSaveData, new()
    {
        static T _instance;
        protected static T instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<T>();
                }

                if (_instance == null)
                {
                    Debug.LogError("Auto creating persistent data object. Should be added to base scene.");
                    _instance = new GameObject(_instance.GetType().ToString()).AddComponent<T>();
                }

                return _instance;
            }
        }

        public static V Data
        {
            get
            {
                return instance?.currentSaveData;
            }
        }

        public static int ActiveSlot => instance.activeSlot;

        public static void ChangeSlot(int slot)
        {
            instance?.changeSlot(slot);
        }

        public static string BaseSaveDataPath
        {
            get
            {
                return instance.baseSaveDataPath;
            }
        }

        public static V GetSlotData(int slot)
        {
            return instance.getSlotData(slot);
        }

        //TODO : Don't reload if there isn't an outstanding write, or it's the first time, or we're in edit mode.
        public static void LoadDataFromDisk()
        {
            instance.loadDataFromDisk();
        }

        public static void SaveAll(bool writeToDiskImmediately = false)
        {
            instance.saveAll(writeToDiskImmediately);
        }

        public static void SaveFieldValueType(string fieldName, bool writeToDiskImmediately = false)
        {
            instance.saveFieldValueType(fieldName, writeToDiskImmediately);
        }
        public static void SaveField(object fieldToDirty, bool writeToDiskImmediately = false)
        {
            instance.saveField(fieldToDirty, writeToDiskImmediately);
        }

        public static void RevertField(object fieldToDirty)
        {
            instance.revertField(fieldToDirty);
        }

        public static void ClearSaveData(int slot = -1)
        {
            if (slot >= 0)
            {
                instance.deleteSaveData(slot);
            }
            else
            {
                Debug.Log("Deleting all data.... (holdover behavior)");
                instance.deleteAllSaveData();
            }
        }

        public static void KillPermanently()
        {
            if (instance != null)
            {
                instance._currentSaveData = default;
                Destroy(instance);
            }
            _instance = null;
        }

        private void OnDestroy()
        {
            _instance = null;
        }
    }

    public interface ISTSimpleSaveSubscriber
    {
        void OnInitialize();
        void OnPreSave();
    }

    public static class ISTSimpleSaveSubscriberExtensions
    {
        public static void SetDirty(this ISTSimpleSaveSubscriber saveData)
        {

        }
    }

#if UNITY_EDITOR
    public class XUGenericPersistentDataEditor<T, V> : UnityEditor.Editor where T : XUGenericPersistentData<V> where V : XUBaseSaveData, new()
    {
        GUIStyle _style = null;
        public override void OnInspectorGUI()
        {
            if (_style == null)
            {
                _style = new(GUI.skin.button);
                _style.normal.textColor = Color.yellow;
                _style.active.textColor = Color.yellow;
                _style.hover.textColor = Color.yellow;
                _style.fontStyle = FontStyle.Bold;
            }
            T targetScript = (T)target;
            if (targetScript.useSlots)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("Save||\nSlots||", GUILayout.ExpandWidth(false));

                //targetScript.activeSlotGUILayout.Label($"Current Slot: {targetScript.activeSlot}");
                var curSlot = targetScript.activeSlot;
                GUILayout.BeginHorizontal();
                for (int i = 0; i < 3; i++)
                {
                    bool isCurrentSlot = curSlot == i;
                    GUILayout.BeginVertical(GUILayout.Width(80));
                    GUILayout.Label(isCurrentSlot ? "__ACTIVE__" : "", GUILayout.Width(75));

                    bool btnResult;
                    if (isCurrentSlot)
                    {
                        _style.fixedWidth = 75;
                        btnResult = GUILayout.Button($"SLOT [{i}]", _style);
                    }
                    else
                    {
                        btnResult = GUILayout.Button($"SLOT {i}", GUILayout.Width(75));
                    }
                    if (btnResult)
                    {
                        targetScript.changeSlot(i);
                    }
                    GUILayout.EndVertical();
                }
                GUILayout.EndHorizontal();
                GUILayout.EndHorizontal();
            }

            DrawDefaultInspector();
            //GUILayout.Label("Singletown?? : " + (targetScript == TLGameSave._instanceHelper.instance));
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Save To Disk"))
            {
                targetScript.saveAll(true);
            }


            if (GUILayout.Button("Refresh From Disk"))
            {
                targetScript.loadDataFromDisk();
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Save Backup"))
            {
                string formatString = "MM-dd_HH-mm-ss";
                string fullPath = targetScript.baseSaveDataPath;
                string dir = System.IO.Path.GetDirectoryName(fullPath);
                string fileName = System.IO.Path.GetFileName(fullPath) + "=" + DateTime.Now.ToString(formatString);
                string chosenPath = UnityEditor.EditorUtility.SaveFilePanel("Save Backup As", dir, fileName, "");

                if (!string.IsNullOrEmpty(chosenPath))
                {
                    targetScript.saveAll(true, chosenPath);
                }
            }

            if (GUILayout.Button("Load Backup"))
            {
                string fullPath = targetScript.baseSaveDataPath;
                string dir = System.IO.Path.GetDirectoryName(fullPath);
                string fileName = System.IO.Path.GetFileName(fullPath);
                string chosenPath = UnityEditor.EditorUtility.OpenFilePanel("Save Backup As", dir, "");

                if (!string.IsNullOrEmpty(chosenPath))
                {
                    targetScript.loadDataFromDisk(chosenPath);
                }
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();

            if (GUILayout.Button("Delete Current Slot"))
            {
                targetScript.deleteAllSaveData();
            }

            if (GUILayout.Button("Delete All"))
            {
                if (UnityEditor.EditorUtility.DisplayDialog("Delte All Save Data", "Are you sure you want to delete it all?", "Yes", "Cancel"))
                {
                    targetScript.deleteAllSaveData();
                }
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Open In Explorer"))
            {
                var slotSavePath = (targetScript.dataReaderWriter as XUBasicJsonFileReaderWriter<V>)?.getSaveDataPathForSlot(targetScript.activeSlot);
                XUFileUtil.OpenFileInExplorer(slotSavePath);
            }
            GUILayout.EndHorizontal();

        }
    }
#endif
}