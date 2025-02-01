/*
 

 
 */
namespace XUUtils
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using System.Reflection;
    using SavedTransform = XUTransform;
    using System;

    public class SpicyCheato : MonoBehaviour
    {

        static class Folders
        {
            public const string Save = "General/SAVE DATA";
            public const string General = "General";
            public const string Console = "General/CONSOLE";
            public const string Player = "General/Player";
            public const string MR = "General/MR";
            public const string Profiling = "Profiling";
            public const string Cheats = "Cheats";
            public const string CostumeAdjustments = "Costume Adjust";
        }

        public static bool CheatoMenuIsOpen => instance._showCheats;
        Transform headTransform => Camera.main.transform;//TCGlobals.I.Camera.DataBodyHeadTransform;

        public static event System.Action<bool> OnOpenChanged = (open) => { };

        //List<SpicyCheatoVRButton> _hoveredButtons;
        static SpicyCheato _instance;
        static SpicyCheato instance
        {
            get
            {
                if (isApplicationQuitting) return null;

                if (_instance == null)
                {
                    _instance = GameObject.FindObjectOfType<SpicyCheato>();

                }

                if (_instance == null)
                {
                    GameObject cheathelper = new GameObject("SpicyCheato");
                    cheathelper.transform.SetAsLastSibling();
                    _instance = cheathelper.AddComponent<SpicyCheato>();
                }
                return _instance;
            }
        }

        public interface ICheatBoolVal
        {
            bool cheatBoolVal
            {
                get;
            }
        }


        //[System.Serializable]
        public class Cheatlet : ICheatBoolVal
        {
            public string shortName;
            public bool _repeatingButton = false;
            public bool hideIfSrcDisabled = false;

            public System.Action<Cheatlet> action;

            public MonoBehaviour src;

            public bool markedForDeath = false;

            public string folder;

            public void PerformAction()
            {
                action.Invoke(this);
            }

            public bool cheatBoolVal
            {
                get;
                internal set;
            }
        }


        //[SerializeField]
        List<Cheatlet> _allCheats = new List<Cheatlet>();
        List<string> _folders = new List<string>(512);

        readonly KeyCode cheatoKey = KeyCode.Alpha7;


        SavedTransform _savedHeadPoseLocal;
        float _savedHeadScaleLocal;


        private void Awake()
        {
            _instance = this;

            //_hoveredButtons = new List<SpicyCheatoVRButton>();
            //_hoveredButtons.Add(null);
            //_hoveredButtons.Add(null);

            SpicyCheato.RegisterNewCheat(Folders.General + "/Clear Cheatos", ClearAllCheatos);
        }

        void OnDestroy()
        {
            _instance = null;
        }

        void ClearAllCheatos()
        {
            foreach (Cheatlet ch in _allCheats)
            {
                ch.markedForDeath = true;
            }
            _allCheats.Clear();
        }

        void sortList()
        {
            _allCheats.Sort((a, b) =>
            {
                bool aIsFolder = a.shortName.StartsWith("[");
                bool bIsFolder = b.shortName.StartsWith("[");
                int ret = 0;
                if (aIsFolder && !bIsFolder)
                {
                    ret = -1;
                }
                else if (!aIsFolder && bIsFolder)
                {
                    ret = 1;
                }
                else //either both folder, or both not
                {
                    ret = string.CompareOrdinal(a.shortName, b.shortName);
                }
                return ret;

            });
        }
        private IEnumerator Start()
        {

            while (headTransform == null)
            {
                yield return null;
            }
            this.transform.SetParent(headTransform.parent);
            this.transform.localPosition = Vector3.zero;
            this.transform.localRotation = Quaternion.identity;
        }

        public bool _showCheats = false;

        string __currentFolder = "/";

        string _currentFolder
        {
            get
            {
                return __currentFolder;
            }
            set
            {
                __currentFolder = value;
                if (string.IsNullOrEmpty(__currentFolder))
                {
                    __currentFolder = "/";
                }
            }
        }

        void EnterParentFolder()
        {
            int splitPt = _currentFolder.LastIndexOf("/");
            if (splitPt > 0)
            {
                _currentFolder = _currentFolder.Substring(0, splitPt);
            }
            RefreshCheatos();
        }

        public static void RefreshCheatos()
        {

        }

        public static void RefreshCheatoText()
        {
        }


        class CheatoFolder
        {
            public string name;
            CheatoFolder parentFolder;
            List<CheatoFolder> subFolders;
            public CheatoFolder(string name)
            {
                this.name = name;
                this.subFolders = new List<CheatoFolder>(16);
            }
        }

        public int nCheatsInFolder()
        {
            return -1;
        }


        static void GetCheatNameAndDirection(string inputPath, out string folderPath, out string buttonName)
        {
            folderPath = null;
            buttonName = inputPath;
            int lastSlashIdx = inputPath.LastIndexOf('/');
            if (lastSlashIdx > 0)
            {
                folderPath = inputPath.Substring(0, lastSlashIdx);//System.IO.Path.GetDirectoryName(inputPath);
                buttonName = inputPath.Substring(lastSlashIdx + 1);
            }

            if (string.IsNullOrEmpty(folderPath))
            {
                folderPath = null;
            }

        }

        //Register a cheat not tied to any monobehaviour, will never die
        public static Cheatlet RegisterNewCheat(string cheatName, System.Action cheatAction)
        {
            string cheatPath, cheatButtonName;
            GetCheatNameAndDirection(cheatName, out cheatPath, out cheatButtonName);
            return RegisterNewCheat(instance, cheatPath, cheatButtonName, cheatAction);
        }

        public static Cheatlet RegisterNewCheat(MonoBehaviour src, string cheatName, System.Action cheatAction)
        {
            string cheatPath, cheatButtonName;
            GetCheatNameAndDirection(cheatName, out cheatPath, out cheatButtonName);
            return RegisterNewCheat(src, cheatPath, cheatButtonName, cheatAction);
        }

        public static Cheatlet RegisterNewCheat(MonoBehaviour src, string cheatFolderFullPathRaw, string cheatName, System.Action cheatAction)
        {
            return RegisterNewCheat(src, cheatFolderFullPathRaw, cheatName, false, cheatAction);
        }

        public static Cheatlet RegisterNewCheat(MonoBehaviour src, string cheatFolderFullPathRaw, string cheatName, System.Action<Cheatlet> cheatAction)
        {
            return RegisterNewCheat(src, cheatFolderFullPathRaw, cheatName, false, cheatAction);
        }

        public static List<Cheatlet> RegisterCheatoTweakableFloat(MonoBehaviour src, string cheatFolderFullPathRaw, FieldInfo field, float increment, float minValue = float.MinValue, float maxValue = float.MaxValue)
        {
            List<Cheatlet> ret = new List<Cheatlet>();

            string fieldName = field.Name;
            float startVal = (float)field.GetValue(src);

            void OnCheato(float increment)
            {
                float currVal = (float)field.GetValue(src);
                currVal += increment;
                currVal = Mathf.Clamp(currVal, minValue, maxValue);

                field.SetValue(src, currVal);

                ret[0].shortName = $"++{fieldName} {currVal.ToString("#0.##")}";
                ret[1].shortName = $"--{fieldName} {currVal.ToString("#0.##")}";
                RefreshCheatoText();
            }

            ret.Add(RegisterNewCheat(src, cheatFolderFullPathRaw, $"++{fieldName} {startVal.ToString("#0.##")}", () => OnCheato(increment)));
            ret.Add(RegisterNewCheat(src, cheatFolderFullPathRaw, $"--{fieldName} {startVal.ToString("#0.##")}", () => OnCheato(-increment)));

            return ret;
        }

        public static List<Cheatlet> RegisterCheatoTweakableFloatProperty(MonoBehaviour src, string cheatFolderFullPathRaw, string propertyName, float increment, float minValue = float.MinValue, float maxValue = float.MaxValue)
        {
            System.Type srcType = src.GetType();
            PropertyInfo property = srcType.GetProperty(propertyName, (BindingFlags)(~0));
            return RegisterCheatoTweakableFloat(src, cheatFolderFullPathRaw, property, increment, minValue, maxValue);
        }

        public static List<Cheatlet> RegisterCheatoTweakableFloatField(MonoBehaviour src, string cheatFolderFullPathRaw, string fieldName, float increment, float minValue = float.MinValue, float maxValue = float.MaxValue)
        {
            System.Type srcType = src.GetType();
            FieldInfo field = srcType.GetField(fieldName, (BindingFlags)(~0));
            return RegisterCheatoTweakableFloat(src, cheatFolderFullPathRaw, field, increment, minValue, maxValue);
        }

        public static List<Cheatlet> RegisterCheatoTweakableFloat(MonoBehaviour src, string cheatFolderFullPathRaw, PropertyInfo property, float increment, float minValue = float.MinValue, float maxValue = float.MaxValue)
        {
            List<Cheatlet> ret = new List<Cheatlet>();

            string propName = property.Name;
            float startVal = (float)property.GetValue(src);

            void OnCheato(float increment)
            {
                float currVal = (float)property.GetValue(src);
                currVal += increment;
                currVal = Mathf.Clamp(currVal, minValue, maxValue);

                property.SetValue(src, currVal);

                ret[0].shortName = $"--{propName} {currVal}";
                ret[1].shortName = $"++{propName} {currVal}";
                RefreshCheatoText();
            }

            ret.Add(RegisterNewCheat(src, cheatFolderFullPathRaw, $"--{propName} {startVal}", () => OnCheato(-increment)));
            ret.Add(RegisterNewCheat(src, cheatFolderFullPathRaw, $"++{propName} {startVal}", () => OnCheato(increment)));

            return ret;
        }

        public static ICheatBoolVal RegisterNewCheatBool(string cheatFolderFullPathRaw, string cheatName)
        {
            return RegisterNewCheat(null, cheatFolderFullPathRaw, cheatName, true, null as System.Action);
        }

        static Cheatlet RegisterNewCheat(MonoBehaviour src, string cheatFolderFullPathRaw, string cheatName, bool repeatingButton, System.Action cheatAction)
        {
            System.Action<Cheatlet> action = (ch) => { cheatAction(); };
            return RegisterNewCheat(src, cheatFolderFullPathRaw, cheatName, repeatingButton, action);
        }

        static Cheatlet RegisterNewCheat(MonoBehaviour src, string cheatFolderFullPathRaw, string cheatName, bool repeatingButton, System.Action<Cheatlet> cheatAction)
        {
#if FM_DEV_BUILD
        if (_instance == null)
        {
            return null;
        }

        string cheatFolderFullPath = cheatFolderFullPathRaw;

        if (cheatFolderFullPathRaw != null)
        {
            cheatFolderFullPath = removeStartAndEndSlash(cheatFolderFullPath);


            List<int> slashIndices = FindAllIndicesOf(cheatFolderFullPath, "/");


            slashIndices.RemoveAll((i) => i == 0 || i == cheatFolderFullPath.Length - 1);
            slashIndices.Add(cheatFolderFullPath.Length);

            List<string> subFolderFullPaths = new List<string>();

            for (int i = 0; i < slashIndices.Count; i++)
            {
                subFolderFullPaths.Add(cheatFolderFullPath.Substring(0, slashIndices[i]));
            }

            for (int i = 0; i < subFolderFullPaths.Count; i++)
            {
                string parentFolder = i == 0 ? null : subFolderFullPaths[i - 1];
                string intermediateFolder = subFolderFullPaths[i];
                if (!_instance._folders.Contains(intermediateFolder))
                {
                    _instance._folders.Add(intermediateFolder);
                    int lastSlash = intermediateFolder.LastIndexOf('/');

                    string folderDisplayName = intermediateFolder.Substring(lastSlash + 1, intermediateFolder.Length - (lastSlash + 1));//System.IO.Path.GetDirectoryName(intermediateFolder);

                    if (!string.IsNullOrEmpty(folderDisplayName))
                    {
                        //add a cheat to show the folder
                        RegisterNewCheat(_instance, parentFolder, "[" + folderDisplayName + "]", () => { _instance.setCheatFolder(intermediateFolder); });

                        //add a "back" cheat to the folder 
                        RegisterNewCheat(_instance, intermediateFolder, "[-BACK-]", () => { _instance.setCheatFolder(parentFolder); });
                    }
                }

            }


            //--
            //string[] cheatFolders = cheatFolderPath.Split('/');
            //for (int i = 0; i < cheatFolders.Length; i++)
            //{
            //    string cheatFolder = cheatFolders[i];
            //    bool alreadyHasFolder = !_instance._folders.Contains(cheatFolder);

            //    _instance._folders.Add(cheatFolder);

            //    //add a cheat to show the folder
            //    RegisterNewCheat(_instance, null, "[" + cheatFolder + "]", () => { _instance.setCheatFolder(cheatFolderPath); });

            //    //add a "back" cheat to the folder 
            //    RegisterNewCheat(_instance, cheatFolder, "[BACK]", () => { _instance.setCheatFolder(null); });
            //}
            //--

        }

        Cheatlet nuCheat = new Cheatlet();
        nuCheat.src = src == null ? _instance : src; //this src business don't make a ton of sense....
        nuCheat.action = cheatAction;
        nuCheat.shortName = cheatName;
        nuCheat.folder = cheatFolderFullPath;
        nuCheat._repeatingButton = repeatingButton;
        instance._allCheats.Add(nuCheat);

        _instance.sortList();
        return nuCheat;
#else
            return null;
#endif
        }

        public static List<int> FindAllIndicesOf(string str, string value)
        {
            if (String.IsNullOrEmpty(value))
                throw new ArgumentException("the string to find may not be empty", "value");
            List<int> indexes = new List<int>();
            for (int index = 0; ; index += value.Length)
            {
                index = str.IndexOf(value, index);
                if (index == -1)
                    return indexes;
                indexes.Add(index);
            }
        }

        public static string removeStartAndEndSlash(string inp)
        {

            string sanitizd = inp;
            if (sanitizd.StartsWith("/"))
            {
                sanitizd = sanitizd.Substring(1, sanitizd.Length - 1);
            }

            if (sanitizd.EndsWith("/"))
            {
                sanitizd = sanitizd.Substring(0, sanitizd.Length - 1);
            }
            return sanitizd;
        }

        //Register a cheat tied to a particular monobehaviour, dies(?) if the object goes null
        public static SpicyCheato.Cheatlet RegisterNewCheat(string cheatFolderFullPathRaw, string cheatName, System.Action cheatAction)
        {
            return RegisterNewCheat(null, cheatFolderFullPathRaw, cheatName, cheatAction);
        }


        void setCheatFolder(string cheatFolder)
        {
            this._currentFolder = cheatFolder;
        }



        bool okToToggle = true;
        float holdAccumulator = 0;


        private void Update()
        {
#if FM_DEV_BUILD
            purgeDeadCheatoUpdate();

            bool toggleCheatMenu = Input.GetKeyDown(cheatoKey);

            float holdDuration = 2;
            bool bothTriggersHeld = false;
            bool neitherTriggerHeld = true;

            if (bothTriggersHeld && okToToggle)
            {
                float increaseAmount = Time.deltaTime;

                bool toggleImmediately = _showCheats; //close instantly


#if UNITY_EDITOR
                holdDuration = .25f;
#endif


                if (toggleImmediately)
                {
                    increaseAmount = 1000;
                }
                holdAccumulator += increaseAmount;
            }
            else
            {
                holdAccumulator = 0;
            }

            bool holdAccumulatorSaturated = holdAccumulator >= holdDuration;


            toggleCheatMenu |= okToToggle && holdAccumulatorSaturated;// bothTriggersHeld;

            if (neitherTriggerHeld)
            {
                okToToggle = true;
            }


            if (toggleCheatMenu)
            {
                PurgeDeadCheatos();
                okToToggle = false;
                _showCheats = !_showCheats;

                Cursor.visible = _showCheats;
                Cursor.lockState = _showCheats ? CursorLockMode.None : CursorLockMode.Locked;
   
            }

#endif
        }

        //int _lastDeadCheatoIdx = 0;
        float cheatoClearTimer = 0;
        void purgeDeadCheatoUpdate()
        {
            cheatoClearTimer += Time.unscaledDeltaTime;
            if (cheatoClearTimer > 1)
            {
                cheatoClearTimer = 0;
                PurgeDeadCheatos();
            }

        }

        public static void RemoveCheatlet(Cheatlet cheatlet, bool squelchRefresh)
        {
            if (instance == null) return;

            instance._allCheats.Remove(cheatlet);

            if (!squelchRefresh)
                RefreshCheatos();
        }

        public static void RemoveAllRegisteredToSource(MonoBehaviour src)
        {
#if !FM_DEV_BUILD
            return;
#endif
            if (instance == null || src == null) return;

            for (int i = instance._allCheats.Count - 1; i >= 0; i--)
            {
                var cheat = instance._allCheats[i];
                if (cheat?.src == src)
                {
                    instance._allCheats.RemoveAt(i);
                }
            }

        }

        void PurgeDeadCheatos(bool cleanUpFolders = false)
        {
            bool needToCleanUpButtons = false;
            for (int i = 0; i < _allCheats.Count; i++)
            {
                //_lastDeadCheatoIdx = _lastDeadCheatoIdx % _allCheats.Count;
                Cheatlet onDeathRow = _allCheats[i];
                if (cheatletIsDead(onDeathRow))
                {
                    needToCleanUpButtons = true;
                    if (onDeathRow != null)
                    {
                        onDeathRow.markedForDeath = true;
                    }
                    //Debug.Log("Cheato Offed");
                    _allCheats.RemoveAt(i);
                    i--;
                }
                //_lastDeadCheatoIdx++;
            }
        }


        bool cheatletIsDead(Cheatlet ch)
        {
            return ch == null || ch.src == null;
        }


        bool isCheatFiltered(Cheatlet ch)
        {
            string effFolder = string.IsNullOrEmpty(ch.folder) ? "/" : ch.folder;
            bool cheatIsFiltered = effFolder != _currentFolder;
            cheatIsFiltered |= ch.hideIfSrcDisabled && ch.src != null && !ch.src.enabled;
            return cheatIsFiltered;
        }

        static bool isApplicationQuitting = false;
        void OnApplicationQuit()
        {
            isApplicationQuitting = true;
        }


#if UNITY_EDITOR || !UNITY_ANDROID
        private void OnGUI()
        {
            if (_showCheats)
            {
                int bWidth = 140;
                int bHeight = 35;
                float spacing = 15;

                float screenMargin = 10;

                int i = 0;
                int nCols = Mathf.FloorToInt((Screen.width - 2 * screenMargin) / (bWidth + spacing));
                nCols = nCols < 1 ? 1 : nCols;
                for (int rawI = 0; rawI < _allCheats.Count; rawI++)
                {
                    Cheatlet ch = _allCheats[rawI];

                    bool cheatIsFiltered = isCheatFiltered(ch);
                    if (cheatIsFiltered)
                    {
                        continue;
                    }

                    int xi = i % nCols;
                    int yi = i / nCols;



                    GUI.BeginGroup(new Rect(screenMargin, screenMargin, Screen.width - screenMargin, Screen.height - screenMargin));
                    Rect r = new Rect(xi * (bWidth + spacing), yi * (bHeight + spacing) + 2 * spacing, bWidth, bHeight);
                    bool fromBottomOfScreen = true;
                    if (fromBottomOfScreen)
                    {
                        r.y = Screen.height - r.y - bHeight;
                    }
                    if (ch._repeatingButton)
                    {
                        ch.cheatBoolVal = GUI.RepeatButton(r, ch.shortName);
                    }
                    else
                    {
                        if (GUI.Button(r, ch.shortName) && ch.action != null)
                        {
                            ch.action.Invoke(ch);
                        }
                    }


                    GUI.EndGroup();

                    i++;
                }
            }
        }
#endif
    }

}