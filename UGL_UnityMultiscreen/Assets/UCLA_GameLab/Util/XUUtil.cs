/*
An assortment of random useful functions/classes/stuff

Also includes some Debug developer settings

TODO convert some of these to extension methods!

*/

//#define XU_CREATE_MENU_ITEMS

using UnityEngine;
using UnityEngine.AI;

using System.Collections;
using System.Collections.Generic;
using System;
using System.Text;
using UnityEngine.SceneManagement;
using System.Reflection;
using System.Linq;
using Object = UnityEngine.Object;
using UnityEngine.Playables;
using XUUtils;
using XUTransform = XUUtils.XUTransform;
using UnityEngine.Events;


#if UNITY_EDITOR
using UnityEditor;
using System.IO;
#endif
namespace XUUtils
{


    public static class XUExtensions
    {

        public static IEnumerable<float> AllComponents(this Matrix4x4 matrix)
        {
            yield return matrix.m00;
            yield return matrix.m01;
            yield return matrix.m02;
            yield return matrix.m03;
            yield return matrix.m10;
            yield return matrix.m11;
            yield return matrix.m12;
            yield return matrix.m13;
            yield return matrix.m20;
            yield return matrix.m21;
            yield return matrix.m22;
            yield return matrix.m30;
            yield return matrix.m31;
            yield return matrix.m32;
            yield return matrix.m33;
            yield return matrix.m33;
        }

        public static bool HasNaNComponent(this Matrix4x4 matrix)
        {
            foreach (float component in matrix.AllComponents())
            {
                if (float.IsNaN(component))
                    return true;
            }

            return false;
        }

        public static bool HasNaNComponent(this Quaternion quat)
        {
            for (int i = 0; i < 4; i++)
            {
                if (float.IsNaN(quat[i]))
                    return true;
            }

            return false;
        }

        public static Quaternion EulerScaled(this Quaternion quat, int xScale, int yScale, int zScale)
        {
            return Quaternion.Euler(quat.eulerAngles.scaled(xScale, yScale, zScale));
        }


#if UNITY_EDITOR
        // From https://github.com/lordofduct/spacepuppy-unity-framework/blob/master/SpacepuppyBaseEditor/EditorHelper.cs

        public static object GetTargetObject(this SerializedProperty prop)
        {
            if (prop == null) return null;

            var path = prop.propertyPath.Replace(".Array.data[", "[");
            object obj = prop.serializedObject.targetObject;
            var elements = path.Split('.');
            foreach (var element in elements)
            {
                if (element.Contains("["))
                {
                    var elementName = element.Substring(0, element.IndexOf("["));
                    var index = Convert.ToInt32(element.Substring(element.IndexOf("[")).Replace("[", "").Replace("]", ""));
                    obj = GetValue_Imp(obj, elementName, index);
                }
                else
                {
                    obj = GetValue_Imp(obj, element);
                }
            }
            return obj;
        }

        private static object GetValue_Imp(object source, string name)
        {
            if (source == null)
                return null;
            var type = source.GetType();

            while (type != null)
            {
                var f = type.GetField(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
                if (f != null)
                    return f.GetValue(source);

                var p = type.GetProperty(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                if (p != null)
                    return p.GetValue(source, null);

                type = type.BaseType;
            }
            return null;
        }

        private static object GetValue_Imp(object source, string name, int index)
        {
            var enumerable = GetValue_Imp(source, name) as System.Collections.IEnumerable;
            if (enumerable == null) return null;
            var enm = enumerable.GetEnumerator();
            //while (index-- >= 0)
            //    enm.MoveNext();
            //return enm.Current;

            for (int i = 0; i <= index; i++)
            {
                if (!enm.MoveNext()) return null;
            }
            return enm.Current;
        }


        public static EditorBuildSettingsScene IsInBuildSettings(this Scene scene)
        {
            foreach (EditorBuildSettingsScene es in EditorBuildSettings.scenes)
            {
                if (es.path == scene.path)
                {
                    return es;
                }
            }
            return null;
        }
        public static void AddSceneToBuildSettings(this Scene scene)
        {

            var inBuildSettings = scene.IsInBuildSettings();
            if (inBuildSettings != null)
            {
                inBuildSettings.enabled = true;
            }
            else
            {
                List<EditorBuildSettingsScene> sl = new List<EditorBuildSettingsScene>(32);
                sl.AddRange(EditorBuildSettings.scenes);
                sl.Add(new EditorBuildSettingsScene(scene.path, true));
                EditorBuildSettings.scenes = sl.ToArray();
            }

        }

#endif


        public static IEnumerable<IEnumerable<T>> GetAllCombinations<T>(this T[] values, int containingIndex = -1)
        {
            int nCombinations = Mathf.RoundToInt(Mathf.Pow(2, values.Length));

            for (int i = 1; i <= nCombinations - 1; i++)
            {
                string str = Convert.ToString(i, 2).PadLeft(values.Length, '0');

                if (containingIndex != -1 && str[containingIndex] != '1')
                    continue;

                yield return XUUtil.GetCombination(nCombinations, str, values);
            }

            yield break;
        }

        public static IEnumerable<IEnumerable<T>> GetCartesianProductsOfSetCombinations<T>(this IEnumerable<T>[] sets)
        {
            var setCombinations = sets.GetAllCombinations();

            foreach (IEnumerable<IEnumerable<T>> setCombination in setCombinations)
            {
                foreach (IEnumerable<T> subSet in setCombination.GetCartesianProduct())
                {
                    yield return subSet;
                }
            }
        }

        public static IEnumerable<IEnumerable<T>> GetCartesianProduct<T>(this IEnumerable<IEnumerable<T>> sets)
        {
            IEnumerable<IEnumerable<T>> emptyProduct = new[] { Enumerable.Empty<T>() };
            IEnumerable<IEnumerable<T>> result = emptyProduct;
            foreach (IEnumerable<T> sequence in sets)
            {
                result = from accseq in result from item in sequence select accseq.Concat(new[] { item });
            }

            return result;
        }

        static List<Component> _scratchComponentList = new List<Component>();
        static bool isVisualComponent(Component c)
        {
            return c is Renderer || c is Light || c is Transform || c is MeshFilter;
        }
        public static void RemoveNonVisualComponents(this GameObject selObj)
        {
            _scratchComponentList.Clear(); selObj.GetComponentsInChildren<Component>(true, _scratchComponentList);

            //Do 1 pass, leave rigidbodies alone to prevent errors with joints
            foreach (var c in _scratchComponentList)
            {
                if (c == null)
                {
                    continue;
                }

                if (!isVisualComponent(c) && !(c is Rigidbody))
                {
                    GameObject.DestroyImmediate(c);
                }
            }

            //do another pass getting rigidbodies
            foreach (var c in _scratchComponentList)
            {
                if (c == null)
                {
                    continue;
                }

                if (!isVisualComponent(c))
                {
                    GameObject.DestroyImmediate(c);
                }
            }
        }


        public static System.Type GetTypeSafe(this object obj) => obj?.GetType() ?? null;

        public static string GetNameSafe(this UnityEngine.Object obj)
        {
            return obj != null ? obj.name : "null";
        }

        public static string ConcatenateNames<T>(this IEnumerable<T> objs)
            where T : UnityEngine.Object
        {
            if (!objs.Any())
                return "{None}";
            return string.Join(", ", objs.Select(o => o.GetNameSafe()));
        }

        public static string DumpFieldsToString(this Component comp)
        {
            string ret = $"{comp.name} Fields:";

            foreach (var field in comp.GetType().GetFields((BindingFlags)~0))
            {
                ret += $"\t{field.Name}: {field.GetValue(comp)}\n";
            }

            return ret;
        }


        public static string GetHierarchyAndSceneAsString(this GameObject obj)
        {
            string hierarchy = GetHierarchyAsString(obj);
            string sceneName = obj.scene.name;
            return $"{sceneName}:{hierarchy}";
        }


        public static string GetRelativeHierarchyAsString(this Component comp, Transform relativeTo)
        {
            if (!comp.transform.IsChildOf(relativeTo))
            {
                Debug.LogError($"'{comp.name}' is not a child of '{relativeTo.name}'", comp);
                return null;
            }
            else
            {
                return comp.GetHierarchyAsString().Substring(relativeTo.GetHierarchyAsString().Length + 1);
            }
        }

        public static string GetHierarchyAsString(this Component comp, int maxParentLevels = -1)
        {
            if (comp == null)
                return null;
            return GetHierarchyAsString(comp.gameObject, maxParentLevels);
        }

        public static string GetHierarchyAsString(this GameObject obj, int maxParentLevels = -1)
        {
            if (obj == null)
                return null;

            var outputBuilder = new StringBuilder();
            var workingTrans = obj.transform;
            var parentCounter = 0;
            while (workingTrans != null && (parentCounter <= maxParentLevels || maxParentLevels < 0))
            {
                if (parentCounter != 0) outputBuilder.Insert(0, "/");
                outputBuilder.Insert(0, workingTrans.gameObject.name);
                workingTrans = workingTrans.parent;
                parentCounter++;
            }

            return outputBuilder.ToString();
        }

        /// <summary>
        /// Call SetActive, if active != activeSelf
        /// </summary>
        /// <param name="gameObj"></param>
        /// <param name="active"></param>
        public static void SetActiveGentle(this GameObject gameObj, bool active)
        {
            if (gameObj.activeSelf != active)
            {
                gameObj.SetActive(active);
            }
        }

        public static IEnumerable<T> GetComponentsInChildrenForBuild<T>(this Component c, bool includeInactive = false) where T : Component
        {
            Dictionary<Transform, bool> editorOnlyCache = new Dictionary<Transform, bool>();

            foreach (T found in c.GetComponentsInChildren<T>(includeInactive))
            {
                if (IsEditorOnly(found, editorOnlyCache))
                    continue;

                yield return found;
            }

            yield break;
        }

        static bool IsEditorOnly(Component c, Dictionary<Transform, bool> editorOnlyCache)
        {
            List<Transform> traversed = new List<Transform>();

            Transform t = c.transform;
            bool isEditorOnly = false;

            while (t != null)
            {
                traversed.Add(t);

                if (editorOnlyCache.TryGetValue(t, out var foundVal))
                {
                    isEditorOnly = foundVal;
                    break;
                }
                else if (t.CompareTag("EditorOnly"))
                {
                    isEditorOnly = true;
                    break;
                }
                t = t.parent;
            }

            foreach (Transform tr in traversed)
                editorOnlyCache[tr] = isEditorOnly;

            return isEditorOnly;
        }

        public static T GetComponentInParentIfNull<T>(this MonoBehaviour b, ref T thingToReturn, bool includeInactive = false) //where T : Component
        {
            if (b != null && thingToReturn == null || thingToReturn.Equals(null))
            {
                thingToReturn = b.GetComponentInParent<T>(includeInactive);
            }
            return thingToReturn;
        }
        public static T GetComponentInChildrenIfNull<T>(this Component b, ref T thingToReturn, bool includeInactive = false) //where T : Component
        {
            if (b != null && thingToReturn == null || thingToReturn.Equals(null))
            {
                thingToReturn = b.GetComponentInChildren<T>(includeInactive);
            }
            return thingToReturn;
        }
        public static T GetComponentIfNull<T>(this Component b, ref T thingToReturn) //where T : Component
        {
            if (b != null && thingToReturn == null || thingToReturn.Equals(null))
            {
                thingToReturn = b.GetComponent<T>();
            }
            return thingToReturn;
        }

        public static T GetOrAddComponentIfNull<T>(this Component b, ref T thingToReturn) where T : Component
        {
            return GetOrAddComponentIfNull<T, T>(b, ref thingToReturn);
        }
        public static T GetOrAddComponentIfNull<T, Mb>(this Component b, ref T thingToReturn) where T : class where Mb : Component, T
        {

            if ((b != null && !b.Equals(null)) && (thingToReturn == null || thingToReturn.Equals(null)))
            {
                thingToReturn = b.GetComponent<T>();
            }

            if ((b != null && !b.Equals(null)) && (thingToReturn == null || thingToReturn.Equals(null)))
            {
                thingToReturn = b.gameObject.AddComponent<Mb>();
            }

            return thingToReturn;
        }

        /// <summary>
        /// Will work even if parent is in another scene.
        /// </summary>
        /// <param name="t"></param>
        /// <param name="newParent"></param>
        /// <param name="worldPositionStays"></param>
        public static void SetParentBetter(this Transform t, Transform newParent, bool worldPositionStays = true)
        {
            XUTransform worldPos = t;
            if (newParent != null && t.gameObject.gameObject.scene != newParent.gameObject.scene)
            {
                SceneManager.MoveGameObjectToScene(t.gameObject, newParent.gameObject.scene);
            }

            t.SetParent(newParent, worldPositionStays);
        }

        public static float ManhattanMagnitude(this Vector3 v)
        {
            float ret = 0;
            for (int i = 0; i < 3; i++)
            {
                ret += Mathf.Abs(v[i]);
            }
            return ret;
        }

        public static float ManhattanMagnitude(this Vector4 v)
        {
            float ret = 0;
            for (int i = 0; i < 4; i++)
            {
                ret += Mathf.Abs(v[i]);
            }
            return ret;
        }
        public static float ManhattanMagnitude(this Vector2 v)
        {
            float ret = 0;
            for (int i = 0; i < 2; i++)
            {
                ret += Mathf.Abs(v[i]);
            }
            return ret;
        }

        public static void GetNormalAndMagnitude(this Vector3 v, out Vector3 normal, out float magnitude)
        {
            magnitude = v.magnitude;
            normal = v / magnitude;
        }

        public static string ToStringPrecise(this Vector3 v, int numDecimals = 4)
        {
            return $"({v.x.ToString("F" + numDecimals)}, {v.y.ToString("F" + numDecimals)}, {v.z.ToString("F" + numDecimals)})";
        }

        public static string ToStringWithMag(this Vector3 v, int numDecimals = 4)
        {
            return $"{v.x.ToString("N" + numDecimals)} {v.y.ToString("N" + numDecimals)}, {v.z.ToString("N" + numDecimals)} (mag {v.magnitude})";
        }

        public static string ToStringVerbose(this RaycastHit hit)
        {
            if (hit.collider != null)
                return $"Hit: object {hit.collider.ToString()}, point {hit.point}, normal {hit.normal.ToStringPrecise(2)}, distance {hit.distance:N4}";
            else
                return $"Hit: object null (cast missed)";
        }

        public static V GetOrDefault<K, V>(this Dictionary<K, V> dict, K key, V optionalDefault = default(V))
        {
            if (typeof(K).IsClass && key == null) //null keys are not allowed
            {
                return optionalDefault;
            }

            return !dict.ContainsKey(key) ? optionalDefault : dict[key];
        }

        public static V GetOrCreate<K, V>(this Dictionary<K, V> dict, K key) where V : new()
        {
            var ret = dict.GetOrDefault(key);
            if (ret == null)
            {
                ret = new V();
                dict[key] = ret;
            }

            return ret;
        }

        public static V GetOrCreate<K, V>(this Dictionary<K, V> dict, K key, System.Func<V> creator)
        {
            V ret;
            if (!dict.ContainsKey(key))
            {
                ret = creator();
                dict[key] = ret;
            }
            else
            {
                ret = dict[key];
            }
            return ret;
        }

        public static T GetOrDefault<T>(this Dictionary<string, object> dict, string key, T defaultVal = default(T))
        {
            if (dict == null || key == null || !dict.ContainsKey(key))
            {
                return defaultVal;
            }
            else
            {
                return (T)dict[key];
            }
        }

        public static char GetOrDefault(this string str, int i, char defaultChar = '\0')
        {
            if (i < 0 || i >= str.Length) return defaultChar;
            return str[i];
        }

        public static void CopyToClipboard(this string s)
        {
            TextEditor te = new TextEditor();
            te.text = s;
            te.SelectAll();
            te.Copy();
        }


        public static void AppendAll(this StringBuilder sb, params object[] manyStrings)
        {
            for (int i = 0; i < manyStrings.Length; i++)
            {
                sb.Append(manyStrings[i]);
            }
        }
        public static void AppendLine(this StringBuilder thiss, string text, int nIndents)
        {
            thiss.Indent(nIndents);
            thiss.AppendLine(text);
        }

        public static void Indent(this StringBuilder thiss, int nIndents = 1)
        {
            for (int i = 0; i < nIndents; i++)
            {
                thiss.Append("\t");
            }
        }

        public static List<int> FindAllIndicesOf(this string str, string value)
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

        public static void TakeLocalValuesFrom(this Transform thiss, Transform other, bool includeScale = false)
        {
            thiss.localPosition = other.localPosition;
            thiss.localRotation = other.localRotation;
            if (includeScale)
            {
                thiss.transform.localScale = other.localScale;
            }
        }

        public static void TakeLocalValuesFrom(this Transform thiss, XUTransform other, bool includeScale = false)
        {
            thiss.localPosition = other.position;
            thiss.localRotation = other.rotation;
        }


        public static void TakeValuesFrom(this Transform thiss, Transform other, bool includeScale = false)
        {
            thiss.SetPositionAndRotation(other.position, other.rotation);
            if (includeScale)
            {
                thiss.transform.localScale = other.localScale;
            }
        }

        public static void TakeValuesFrom(this Transform thiss, XUTransform other)
        {
            thiss.SetPositionAndRotation(other.position, other.rotation);
        }

        public static void TakeLocalRectValuesFrom(this RectTransform transform, RectTransform other)
        {
            transform.anchorMin = other.anchorMin;
            transform.anchorMax = other.anchorMax;
            transform.anchoredPosition = other.anchoredPosition;
            transform.sizeDelta = other.sizeDelta;
            //transform.localScale = other.localScale;
            //transform.rotation = other.rotation;
        }
        public static void RemoveRange<T>(this ICollection<T> thiss, IEnumerable<T> toRemove)
        {
            foreach (T removed in toRemove)
            {
                thiss.Remove(removed);
            }
        }

        public static void AddRange<T>(this ICollection<T> thiss, IEnumerable<T> toAdd)
        {
            foreach (T addd in toAdd)
            {
                thiss.Add(addd);
            }
        }

        public static IEnumerable<T> AsEnumerable<T>(this T thiss)
        {
            yield return thiss;
        }

        public delegate void TFunction(float t);
        public delegate void ConditionalAction(bool success);


        public enum YieldType
        {
            Null,
            ///<summary>WARNING: this yield waits infinitely while the game view is hidden</summary>
            WaitForEndOfFrame,
            WaitForFixedUpdate
        }

        ///<summary>Execute an arbitrary function that blends from t=0 to t=1 over a specified real-time duration.
        /// <para>⚠ WARNING: if the default YieldType.WaitForEndOfFrame is selected, your function will not execute as long as the game view is hidden ⚠</para> </summary>
        public static Coroutine xuTweenRealtime(this MonoBehaviour thiss, TFunction tFunc, float dur, float delay = 0, YieldType yieldType = YieldType.WaitForEndOfFrame)
        {
            return thiss.StartCoroutine(genericT(thiss, tFunc, dur, delay, true, yieldType));
        }

        ///<summary>Execute an arbitrary function that blends from t=0 to t=1 over a specified duration.
        /// <para>⚠ WARNING: if the default YieldType.WaitForEndOfFrame is selection, your function will not execute as long as the game view is hidden ⚠</para> </summary>
        public static Coroutine xuTween(this MonoBehaviour thiss, TFunction tfunc, float dur, float delay = 0, YieldType yieldType = YieldType.WaitForEndOfFrame)
        {
            return thiss.StartCoroutine(genericT(thiss, tfunc, dur, delay, false, yieldType));
        }

        ///<summary>Execute an arbitrary function that blends from t=0 to t=1 over a specified duration.
        /// <para>⚠ WARNING: if the default YieldType.WaitForEndOfFrame is selected, your function will not execute as long as the game view is hidden ⚠</para> </summary>
        public static Coroutine xuTween(this MonoBehaviour thiss, TFunction tfunc1, float dur1, TFunction tfunc2, float dur2, YieldType yieldType = YieldType.WaitForEndOfFrame)
        {
            return thiss.StartCoroutine(genericT(tfunc1, dur1, tfunc2, dur2, yieldType));
        }

        public static Coroutine xuDoWhenConditionMet(this MonoBehaviour thiss, System.Func<bool> condition, NoArgNoRetFunction action)
        {
            return thiss.StartCoroutine(xuDoWhenConditionMetRoutine(condition, action));
        }

        public static Coroutine xuDoWhenConditionMet(this MonoBehaviour thiss, System.Func<bool> condition, ConditionalAction action, float timeout)
        {
            return thiss.StartCoroutine(xuDoWhenConditionMetOrTimeoutRoutine(condition, action, timeout));
        }

        public static Quaternion AsQuaternion(this Vector4 thiss)
        {
            return new Quaternion(thiss.x, thiss.y, thiss.z, thiss.w);
        }


        public static Vector4 AsVector4(this Quaternion thiss)
        {
            return new Vector4(thiss.x, thiss.y, thiss.z, thiss.w);
        }

        // public static SavedTransform LocalPosRot(this Transform thiss)
        // {
        //     return new SavedTransform(thiss.localPosition, thiss.localRotation);
        // }

        // public static SavedTransform WorldPosRot(this Transform thiss)
        // {
        //     return new SavedTransform(thiss);
        // }

        public static Quaternion AxisTwist(this Quaternion thiss, Vector3 axis)
        {
            //Debug.Assert(Mathf.Approximately(axis.magnitude, 1));

            return XUUtil.GetAxisTwist(thiss, axis);
        }

        public static void UnionWithNonAlloc<T>(this HashSet<T> thiss, HashSet<T> other)
        {
            foreach (T obj in other)
            {
                thiss.Add(obj);
            }
        }

        public static void ExceptWithNonAlloc<T>(this HashSet<T> thiss, HashSet<T> other)
        {
            foreach (T obj in other)
            {
                thiss.Remove(obj);
            }
        }

        public static void UnionWithNonAlloc<T>(this HashSet<T> thiss, List<T> other)
        {
            foreach (T obj in other)
            {
                thiss.Add(obj);
            }
        }

        public static void ExceptWithNonAlloc<T>(this HashSet<T> thiss, List<T> other)
        {
            foreach (T obj in other)
            {
                thiss.Remove(obj);
            }
        }

        public static void UnionWithNonAlloc<T>(this HashSet<T> thiss, T[] other)
        {
            foreach (T obj in other)
            {
                thiss.Add(obj);
            }
        }

        public static void ExceptWithNonAlloc<T>(this HashSet<T> thiss, T[] other)
        {
            foreach (T obj in other)
            {
                thiss.Remove(obj);
            }
        }

        public static double GetSpeed(this PlayableDirector timeline)
        {
            return timeline.playableGraph.GetRootPlayable(0).GetSpeed();
        }
        public static void SetSpeed(this PlayableDirector timeline, double speed)
        {
            timeline.playableGraph.GetRootPlayable(0).SetSpeed(speed);
        }

        // public static SavedTransform PosRot(this Transform thiss, Space space)
        // {
        //     if (space == Space.Self)
        //     {
        //         return thiss.LocalPosRot();
        //     }
        //     else
        //     {
        //         return thiss.WorldPosRot();
        //     }
        // }
        public static YieldInstruction GetNewYielderByType(YieldType yieldType)
        {
            switch (yieldType)
            {
                case YieldType.Null: return null;
                case YieldType.WaitForEndOfFrame: return new WaitForEndOfFrame();
                case YieldType.WaitForFixedUpdate: return new WaitForFixedUpdate();
                default: throw new ArgumentOutOfRangeException();
            }
        }

        public static IEnumerator genericT(TFunction tfunc1, float dur1, TFunction tfunc2, float dur2, YieldType yieldType)
        {
            float startTime = Time.time;
            while (Time.time < startTime + dur1)
            {
                float t = Mathf.Clamp01((Time.time - startTime) / dur1);
                tfunc1(t);
                yield return GetNewYielderByType(yieldType);
            }
            //force call with 1
            tfunc1(1);

            startTime = Time.time;
            while (Time.time < startTime + dur2)
            {
                float t = Mathf.Clamp01((Time.time - startTime) / dur2);
                tfunc2(t);
                yield return GetNewYielderByType(yieldType);
            }
            //force call with 1
            tfunc2(1);
        }

        public static IEnumerator genericT(MonoBehaviour owner, TFunction tfunc, float dur, float delay, bool realtime, YieldType yieldType)
        {
            if (delay > 0)
            {
                if (realtime)
                    yield return new WaitForSecondsRealtime(delay);
                else
                    yield return new WaitForSeconds(delay);
            }

            //float startTime = Time.time;
            float counter = 0;
            while (counter < dur)
            {
                float t = Mathf.Clamp01(counter / dur);
                tfunc(t);
                yield return GetNewYielderByType(yieldType);

                if (realtime)
                    counter += Time.unscaledDeltaTime;
                else
                    counter += Time.deltaTime;
            }

            //force call with 1
            tfunc(1);
        }

        static IEnumerator xuDoWhenConditionMetRoutine(System.Func<bool> condition, NoArgNoRetFunction action)
        {
            yield return new WaitUntil(condition);
            action();
        }

        static IEnumerator xuDoWhenConditionMetOrTimeoutRoutine(System.Func<bool> condition, ConditionalAction action, float timeOut)
        {
            float timer = timeOut;
            while (timeOut > 0 && !condition())
            {
                yield return null;
                timeOut -= Time.unscaledDeltaTime;
            }
            action(condition());
        }

        public static void ZeroLocalPosRot(this Transform thiss)
        {
            thiss.localPosition = Vector3.zero;
            thiss.localRotation = Quaternion.identity;
        }

        public delegate void NoArgNoRetFunction();

        public static void CopyTo<T>(this List<T> thiss, List<T> dest)
        {
            dest.Clear();
            for (int i = 0; i < thiss.Count; i++)
            {
                dest.Add(thiss[i]);
            }
        }

        public static bool indexInRange<T>(this IList<T> l, int foundIdx)
        {
            return foundIdx >= 0 && foundIdx < l.Count;
        }
        public static T GetOrDefault<T>(this IList<T> l, int index, T outOfRangeValue = default)
        {
            if (l == null || !l.indexInRange(index))
            {
                return outOfRangeValue;
            }
            return l[index];
        }

        public delegate bool FilterFunction<T>(T t);
        public static int BruteFindFirst<T>(this IList<T> l, T sought) where T : class
        {
            for (int i = 0; i < l.CountNullRobust(); i++)
            {
                if (l[i] == sought)
                {
                    return i;
                }
            }
            return -1;
        }
        public static T BruteFindFirst<T>(this IList<T> l, FilterFunction<T> condition)
        {
            return BruteFindFirst(l, condition, out int dontCare);
        }

        public static T BruteFindFirst<T>(this IList<T> l, FilterFunction<T> condition, out int index)
        {
            T ret = default;
            index = -1;
            for (int i = 0; i < l.Count; i++)
            {
                if (condition(l[i]))
                {
                    index = i;
                    return l[i];
                }
            }
            return ret;
        }

        /// <summary>
        /// Find all elements matching a condition in a list, and optionally add them to an output lists
        /// of those elements, and the indices of those elements.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="l">the list to search</param>
        /// <param name="condition">condition, return true for elements sought</param>
        /// <param name="elements">a list that will be populated with elements matching condition</param>
        /// <param name="indices">a list that will be populated with the indices of elements matching condition</param>
        /// <returns>the number of elements found</returns>
        public static int BruteFindAll<T>(this IList<T> l, FilterFunction<T> condition, List<T> elements, List<int> indices = null)
        {
            int nFound = 0;
            for (int i = 0; i < l.Count; i++)
            {
                if (condition(l[i]))
                {
                    nFound++;
                    elements?.Add(l[i]);
                    indices?.Add(i);
                }
            }
            return nFound;
        }

        public static void RemoveAllNonAlloc<T>(this IList<T> l, FilterFunction<T> shouldRemoveCheck)
        {
            for (int i = l.Count - 1; i >= 0; i--)
            {
                if (shouldRemoveCheck(l[i]))
                {
                    l.RemoveAt(i);
                }
            }
        }

        public class EmptyEnumerable<T> : IEnumerable
        {
            IEnumerator IEnumerable.GetEnumerator()
            {
                return new EmptyEnumerator<T>();
            }
        }

        public class EmptyEnumerator<T> : IEnumerator<T>
        {
            public T Current
            {
                get
                {
                    return default(T);
                }
            }

            object IEnumerator.Current
            {
                get
                {
                    return null;
                }
            }

            public bool MoveNext()
            {
                return false;
            }
            public void Reset()
            {

            }

            public void Dispose()
            {
            }
        }

        public static Color32 embeddedInCol32(this uint thiss, bool includeG = true, bool includeB = true, bool includeA = false)
        {
            uint remainder = thiss;
            Color32 ret = new Color32();

            for (int i = 0; i < 4; i++)
            {
                if (i == 1 && !includeG)
                    continue;
                if (i == 2 && !includeB)
                    continue;
                if (i == 3 && !includeA)
                    continue;

                bool anyLeftAfter = remainder > 255;
                byte valToSet = (byte)(anyLeftAfter ? 255 : remainder);

                ret[i] = valToSet;
                if (!anyLeftAfter)
                    break;

                remainder -= 255;
            }

            return ret;
        }

        public static Color32 withAlpha(this Color32 thiss, byte a)
        {
            Color32 ret = thiss;
            ret.a = a;
            return ret;
        }

        public static Color withAlpha(this Color thiss, float a)
        {
            Color ret = thiss;
            ret.a = a;
            return ret;
        }

        public static Color withSaturation(this Color thiss, float sat)
        {
            Color ret = thiss;
            Color gray = new Color(ret.grayscale, ret.grayscale, ret.grayscale);
            return Color.LerpUnclamped(gray, ret, sat);
        }

        public static Color withHsvShift(this Color color, float hShift, float sShift, float vShift)
        {
            Color.RGBToHSV(color, out var h, out var s, out var v);
            h = Mathf.Repeat(h + hShift, 1f);
            s = Mathf.Clamp01(s + sShift);
            v = Mathf.Clamp01(v + vShift);
            return Color.HSVToRGB(h, s, v);
        }

        public static Color withHsvShift(this Color color, Vector3 shift) => color.withHsvShift(shift.x, shift.y, shift.z);


        public static Vector3 asVector3(this Color c) => new Vector3(c.r, c.g, c.b);
        public static Vector4 asVector4(this Color c) => new Vector4(c.r, c.g, c.b, c.a);
        public static Vector4 asVector4(this Color c, float overrideW) => new Vector4(c.r, c.g, c.b, overrideW);
        public static Color asColor(this Vector3 v) => new Color(v.x, v.y, v.z);
        public static Color asColor(this Vector4 v) => new Color(v.x, v.y, v.z, v.w);

        public static Vector3 withX(this Vector3 thiss, float x)
        {
            Vector3 ret = thiss;
            ret.x = x;
            return ret;
        }
        public static Vector3 withY(this Vector3 thiss, float y)
        {
            Vector3 ret = thiss;
            ret.y = y;
            return ret;
        }

        public static Vector3 withZ(this Vector3 thiss, float z)
        {
            Vector3 ret = thiss;
            ret.z = z;
            return ret;
        }

        public static Vector3 scaled(this Vector3 thiss, Vector3 scaleVec)
        {
            return thiss.scaled(scaleVec.x, scaleVec.y, scaleVec.z);
        }

        public static Vector3 scaled(this Vector3 thiss, float x, float y, float z)
        {
            Vector3 ret = thiss;
            ret.x *= x;
            ret.y *= y;
            ret.z *= z;
            return ret;
        }

        /// <summary>
        /// If the vector's magnitude is greater than maxLength, return 
        /// a shorter vector in the same direction but with magnitude of maxLength.
        /// </summary>
        /// <param name="v"></param>
        /// <param name="maxLength"></param>
        /// <returns></returns>
        public static Vector3 withClampedLength(this Vector3 v, float maxLength)
        {
            return v.withClampedLength(maxLength, out bool dontCare);
        }
        public static Vector3 withClampedLength(this Vector3 v, float maxLength, out bool wasClamped)
        {
            wasClamped = false;
            Vector3 ret = v;
            float mag = v.magnitude;
            if (mag != 0 && mag > maxLength)
            {
                wasClamped = true;
                ret = (v / mag) * maxLength;
            }
            return ret;
        }


        public static Vector3 withAsymptoticallyClampedLength(this Vector3 v, float clampStart, float clampEnd, float saturatedOutputValue, out float saturateAmt)
        {
            saturateAmt = 0;
            Vector3 ret = v;
            float mag = v.magnitude;
            if (mag != 0)
            {
                float clampedMag = XUUtil.AsymptoticClamp(mag, clampStart, clampEnd, saturatedOutputValue, out saturateAmt);
                ret = (v / mag) * clampedMag;
            }
            return ret;
        }


        public static Vector3 Swizzled(this Vector3 thiss, Vector3Int indices) => thiss.Swizzled(indices.x, indices.y, indices.z);
        public static Vector3 Swizzled(this Vector3 thiss, Vector3 indices) => thiss.Swizzled((int)indices.x, (int)indices.y, (int)indices.z);
        public static Vector3 Swizzled(this Vector3 thiss, int xIdx, int yIdx, int zIdx)
        {
            Vector3 ret = new Vector3(thiss[xIdx], thiss[yIdx], thiss[zIdx]);
            return ret;
        }


        public static Vector3 asXzVector3(this Vector2 thiss) => thiss.asXzVector3(0);

        public static Vector3 asXzVector3(this Vector2 thiss, float y)
        {
            return new Vector3(thiss.x, y, thiss.y);
        }

        public static Vector3 asXyVector3(this Vector2 thiss) => thiss.asXyVector3(0);
        public static Vector3 asXyVector3(this Vector2 thiss, float z)
        {
            return new Vector3(thiss.x, thiss.y, z);
        }

        public static Vector3 asVector3(this Vector4 thiss)
        {
            return new Vector4(thiss.x, thiss.y, thiss.z);
        }


        public static Vector3 asXyVector2(this Vector3 thiss)
        {
            return new Vector2(thiss.x, thiss.y);
        }

        public static Vector2 asXzVector2(this Vector3 v)
        {
            return new Vector2(v.x, v.z);
        }



        // public static float3 asFloat3(this Vector4 thiss)
        // {
        //     return new float3(thiss.x, thiss.y, thiss.z);
        // }
        // public static float2 asFloat2(this Vector4 thiss)
        // {
        //     return new float2(thiss.x, thiss.y);
        // }

        public static Vector4 asVector4(this Vector3 thiss, float w = 1)
        {
            return new Vector4(thiss.x, thiss.y, thiss.z, w);
        }


        public static Vector3 withNaNRemoved(this Vector3 thiss, float replaceValue = 0)
        {
            Vector3 ret = thiss;
            for (int i = 0; i < 3; i++)
            {
                if (ret[i] != ret[i])
                {
                    ret[i] = replaceValue;
                }
            }
            return ret;
        }

        public static bool HashNanComponent(this Vector3 v) => float.IsNaN(v.x) || float.IsNaN(v.y) || float.IsNaN(v.z);

        /// <summary>
        /// Returns a random value between the x (min) and y (max) values
        /// </summary>
        public static float GetRandomBetween(this Vector2 v, float minMod = 1, float maxMod = 1)
        {
            return UnityEngine.Random.Range(v.x * minMod, v.y * maxMod);
        }

        public static bool IsZeroLength(this Vector3 v, float tolerance = 0.00001f) =>
            v.sqrMagnitude <= tolerance * tolerance;

        public static float MaxComponent(this Vector3 v)
        {
            return Mathf.Max(v.x, v.y, v.z);
        }

        public static float MinComponent(this Vector3 v)
        {
            return Mathf.Min(v.x, v.y, v.z);
        }

        public static Vector3 AxisOfSmallestComponent(this Vector3 v)
        {
            float x = Mathf.Abs(v.x);
            float y = Mathf.Abs(v.y);
            float z = Mathf.Abs(v.z);

            if (x <= y && x <= z)
            {
                return Vector3.right;
            }
            if (y <= x && y <= z)
            {
                return Vector3.up;
            }
            else
            {
                return Vector3.forward;
            }
        }

        public static Vector3 AxisOfLargestComponent(this Vector3 v)
        {
            float x = Mathf.Abs(v.x);
            float y = Mathf.Abs(v.y);
            float z = Mathf.Abs(v.z);

            if (x >= y && x >= z)
            {
                return Vector3.right;
            }
            if (y >= x && y >= z)
            {
                return Vector3.up;
            }
            else
            {
                return Vector3.forward;
            }
        }

        public static Vector3 multComponents(this Vector3 v, Vector3 other)
        {
            return new Vector3(v.x * other.x, v.y * other.y, v.z * other.z);
        }

        public static Vector3 ClampAngleFromNormal(this Vector3 v, Vector3 normal, float maxDegrees)
        {
            var ang = Vector3.Angle(v, normal);
            if (ang >= maxDegrees)
                return Vector3.RotateTowards(normal, v, maxDegrees * Mathf.Deg2Rad, 0f);
            return v;
        }

        public static Vector3 EnforceMinAngleFromNormal(this Vector3 v, Vector3 normal, float maxDegrees)
        {
            var ang = Vector3.Angle(v, normal);
            if (ang < maxDegrees)
            {
                var angDelta = maxDegrees - ang;
                return Vector3.RotateTowards(v, -normal, angDelta * Mathf.Deg2Rad, 0f);
            }
            return v;
        }

        /// WARNING: this currently does not behave correctly when V and normal are on opposite sides of the plane
        public static Vector3 ClampAngleFromNormalOnPlane(this Vector3 v, Vector3 normal, Vector3 normalPlane, float maxDegrees)
        {
#if UNITY_EDITOR
            if (Mathf.Abs(90f - Vector3.Angle(normal, normalPlane)) > 1f)
                Debug.Log($"<color=yellow>ClampAngleFromNormalOnPlane Warning: normal {v.ToStringWithMag(3)} and plane {normalPlane.ToStringWithMag(3)} "
                    + $"are not perpendicular, but have an angle of {Vector3.Angle(normal, normalPlane)} (should be 90). Unexpected results will follow.</color>");
#endif

            var planarVDir = Vector3.ProjectOnPlane(v, normalPlane).normalized;
            var signedPlanarAngle = Vector3.SignedAngle(planarVDir, normal, normalPlane);

            if (Mathf.Abs(signedPlanarAngle) >= maxDegrees)
            {
                var correctionAngle = -Mathf.Sign(signedPlanarAngle) * Mathf.DeltaAngle(Mathf.Abs(signedPlanarAngle), maxDegrees);
                return Quaternion.AngleAxis(correctionAngle, normalPlane) * v;
            }
            return v;
        }

        public static float SignedAngleOnPlane(this Vector3 from, Vector3 to, Vector3 planeNormal)
        {
            Vector3 proj = Vector3.ProjectOnPlane(to, planeNormal).normalized;
            return Vector3.SignedAngle(from, proj, planeNormal);
        }
        public static float SignedClampedAngleOnPlane(this Vector3 from, Vector3 to, Vector3 planeNormal, float clampMin, float clampMax)
        {
            Vector3 proj = Vector3.ProjectOnPlane(to, planeNormal).normalized;
            return Mathf.Clamp(Vector3.SignedAngle(from, proj, planeNormal), clampMin, clampMax);
        }

        public static bool IsPointInCylinder(Vector3 point, float radius, Vector3 cylinderCenter, float lowerExtent, float upperExtent)
        {
            var centerToPoint = point - cylinderCenter;

            // Radius test:
            if (centerToPoint.withY(0f).sqrMagnitude > radius * radius)
                return false;
            // Vertical test:
            if (centerToPoint.y < lowerExtent || centerToPoint.y > upperExtent)
                return false;
            return true;
        }

        public static Coroutine delayedFunction(this MonoBehaviour thiss, NoArgNoRetFunction func, float delay)
        {
            return thiss.StartCoroutine(delayedFunctionRoutine(thiss, func, delay));
        }

        static IEnumerator delayedFunctionRoutine(MonoBehaviour owner, NoArgNoRetFunction func, float delay)
        {
            yield return new WaitForSeconds(delay);
            func();
        }

        public static Coroutine delayedFunctionByFrames(this MonoBehaviour thiss, NoArgNoRetFunction func, int framesDelay)
        {
            return thiss.StartCoroutine(delayedFunctionFramesByRoutine(thiss, func, framesDelay));
        }

        static IEnumerator delayedFunctionFramesByRoutine(MonoBehaviour owner, NoArgNoRetFunction func, int framesDelay)
        {
            var numFrames = 0;
            while (numFrames < framesDelay)
            {
                numFrames++;
                yield return null;
            }
            func();
        }

        //public static string toSti(this ResourceType grade)
        public static void SetLayerRecursively(this GameObject obj, int newLayer)
        {
            if (null == obj)
            {
                return;
            }

            obj.layer = newLayer;

            foreach (Transform child in obj.transform)
            {
                if (null == child)
                {
                    continue;
                }
                SetLayerRecursively(child.gameObject, newLayer);
            }
        }

        public static bool IsPrefab(this GameObject obj)
        {
            bool isPrefab = obj.gameObject.scene.rootCount == 0 || obj.scene.GetRootGameObjects()[0] == obj;
            return isPrefab;
        }

        public static bool IsPrefab(this MonoBehaviour obj)
        {
            bool isPrefab = obj.gameObject.scene.rootCount == 0 || obj.gameObject.scene.GetRootGameObjects()[0] == obj.gameObject;
            return isPrefab;
        }


        public static T GetOrAddComponent<T>(this Component c) where T : Component => c.gameObject.GetOrAddComponent<T>();
        public static T GetOrAddComponent<T>(this Component c, out bool existed) where T : Component => c.gameObject.GetOrAddComponent<T>(out existed);
        public static T GetOrAddComponent<T>(this GameObject gob) where T : Component => GetOrAddComponent<T>(gob, out bool dontCare);
        public static T GetOrAddComponent<T>(this GameObject gob, out bool existed) where T : Component
        {
            T ret = gob.GetComponent<T>();
            existed = ret != null;
            if (ret == null)
            {
                ret = gob.AddComponent<T>();
            }
            return ret;
        }

        public static void GetComponentsInChildren<TSub, TBase>(this MonoBehaviour mb, IList<TBase> list) where TSub : TBase
        {
            foreach (TSub t in mb.GetComponentsInChildren<TSub>())
            {
                list.Add(t);
            }
        }

        public static T[] GetComponentsInImmediateChildren<T>(this Transform t, bool inactiveChildren = true)
        {
            List<T> results = new List<T>();
            int childCount = t.childCount;

            for (int i = 0; i < childCount; i++)
            {
                GameObject child = t.GetChild(i).gameObject;

                if (!inactiveChildren && !child.activeSelf)
                {
                    continue;
                }

                T com = child.GetComponent<T>();

                if (com != null)
                {
                    results.Add(com);
                }
            }

            return results.ToArray();
        }

        /// <summary>
        /// Finds All children of a type above another type in hierarchy.
        /// Allows for nested controllers and controlled.
        /// </summary>
        public static T[] GetComponentsInChildrenUntilType<T, STOP>(this Transform t, bool includeInactive = true, bool includeSelf = false)
        {
            List<T> result = new List<T>();
            int childCount = t.childCount;

            if (includeSelf)
            {
                T component = t.GetComponent<T>();
                if (component as Object != null)
                {
                    result.Add(component);
                }
            }

            for (int i = 0; i < childCount; i++)
            {
                Transform child = t.GetChild(i);

                if (child == t)
                {
                    continue;
                }

                if (!includeInactive && child.gameObject.activeInHierarchy)
                {
                    continue;
                }

                STOP stop = child.GetComponent<STOP>();

                if (stop as Object != null)
                {
                    //if child has STOP component, skip
                    continue;
                }

                T component = child.GetComponent<T>();

                //need to cast as object here or null get component result will pass null check
                if (component as Object != null)
                {
                    Debug.Log("Found");
                    result.Add(component);
                }

                //recursively search all children of child (don't accept self of children cause already good)
                T[] childComponenets = child.GetComponentsInChildrenUntilType<T, STOP>(includeInactive, false);

                if (childComponenets != null && childComponenets.Length > 0)
                {
                    result.AddRange(childComponenets);
                }
            }

            return result.ToArray();
        }

        /// <summary>
        /// Finds All children of a type above another type in hierarchy.
        /// Allows for nested controllers and controlled.
        /// </summary>
        public static T[] GetTopLevelComponentsInChildren<T>(this Transform t, bool includeInactive = true, bool includeSelf = false)
        {
            List<T> result = new List<T>();
            int childCount = t.childCount;

            if (includeSelf)
            {
                T component = t.GetComponent<T>();
                if (component as Object != null)
                {
                    result.Add(component);
                }
            }

            for (int i = 0; i < childCount; i++)
            {
                Transform child = t.GetChild(i);

                if (child == t)
                {
                    continue;
                }

                if (!includeInactive && child.gameObject.activeInHierarchy)
                {
                    continue;
                }

                T component = child.GetComponent<T>();

                //need to cast as object here or null get component result will pass null check
                if (component as Object != null)
                {
                    Debug.Log("Found");
                    result.Add(component);

                    //found a component, don't check it's children
                    continue;
                }

                //recursively search all children of child (don't accept self of children cause already good)
                T[] childComponenets = child.GetTopLevelComponentsInChildren<T>(includeInactive, false);

                if (childComponenets != null && childComponenets.Length > 0)
                {
                    result.AddRange(childComponenets);
                }
            }

            return result.ToArray();
        }

        /// <summary>
        /// THIS PROBABLY DOESN'T NEED THE WHILE LOOP
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="thiss"></param>
        /// <param name="maxLevelsToTraverse"></param>
        /// <returns></returns>
        public static T GetComponentInParentOrSelf<T>(this Component thiss, int maxLevelsToTraverse = 1000)
        {
            T ret = thiss.GetComponent<T>();
            //ret = ret == null ? thiss.GetComponentInParent<T>() : ret;
            if (ret == null)
            {
                Transform nextToCheck = thiss.transform.parent;
                int levelCounter = maxLevelsToTraverse;
                while (nextToCheck != null && levelCounter > 0)
                {
                    ret = nextToCheck.GetComponent<T>();
                    if (ret != null)
                    {
                        break;
                    }
                    nextToCheck = nextToCheck.parent;
                    levelCounter--;
                }

            }
            return ret;
        }

        public static bool TryGetComponentInParent<T>(this Component c, out T component, bool includeInactive = false)
        {
            component = c.GetComponentInParent<T>(includeInactive);
            return component != null;
        }

        public static bool TryGetComponentFromRB<T>(this Collider collider, out T component)
        {
            if (collider.attachedRigidbody == null)
            {
                component = default;
                return false;
            }

            component = collider.attachedRigidbody.GetComponent<T>();
            return component != null;
        }

        public static GameObject CopiedChild(this GameObject thiss, GameObject other, string name = "", HideFlags hideFlags = HideFlags.None)
        {
            GameObject ret = GameObject.Instantiate(other);
            ret.hideFlags = hideFlags;

            if (!string.IsNullOrEmpty(name))
                ret.name = name;

            ret.transform.SetParent(thiss.transform, true);
            return ret;
        }

        public static GameObject CreateEmptyChild(this GameObject thiss, string name = "", HideFlags hideFlags = HideFlags.None)
        {
            return thiss.transform.CreateEmptyChild(name, hideFlags);
        }

        public static Transform GetOrCreateChild(this GameObject thiss, string name = "") => thiss.transform.GetOrCreateChild(name);
        public static Transform GetOrCreateChild(this Transform thiss, string name = "")
        {
            var ret = thiss.Find(name);
            if (ret == null) ret = thiss.CreateEmptyChild(name).transform;
            return ret;
        }
        public static GameObject CreateEmptyChild(this Transform thiss, string name = "", HideFlags hideFlags = HideFlags.None)
        {
            GameObject ret = new GameObject();
            ret.hideFlags = hideFlags;
            ret.name = name;

            if (ret.scene != thiss.gameObject.scene)
            {
                SceneManager.MoveGameObjectToScene(ret, thiss.gameObject.scene);
            }

            ret.transform.parent = thiss;
            ret.transform.localPosition = Vector3.zero;
            ret.transform.localRotation = Quaternion.identity;
            ret.transform.localScale = Vector3.one;

            return ret;
        }

        public static T CreateEmptyScriptObject<T>(this GameObject thiss, string name = "", HideFlags hideFlags = HideFlags.None) where T : Component
        {
            return thiss.transform.CreateEmptyScriptObject<T>(name, hideFlags);
        }

        public static T CreateEmptyScriptObject<T>(this Transform thiss, string name = "", HideFlags hideFlags = HideFlags.None) where T : Component
        {
            GameObject container = CreateEmptyChild(thiss, name, hideFlags);
            return container.AddComponent<T>();
        }

        public static GameObject CreateEmptyMeshObject(this GameObject thiss, Mesh mesh, Material[] materials, string name = "")
        {
            return thiss.transform.CreateEmptyMeshObject(mesh, materials, out _, out _, name);
        }

        public static GameObject CreateEmptyMeshObject(this Transform thiss, Mesh mesh, Material[] materials, string name = "")
        {
            return thiss.CreateEmptyMeshObject(mesh, materials, out _, out _, name);
        }

        public static GameObject CreateEmptyMeshObject(this GameObject thiss, Mesh mesh, Material[] materials, out MeshRenderer mr, out MeshFilter mf, string name = "")
        {
            return thiss.transform.CreateEmptyMeshObject(mesh, materials, out mr, out mf, name);
        }

        public static GameObject CreateEmptyMeshObject(this Transform thiss, Mesh mesh, Material[] materials, out MeshRenderer mr, out MeshFilter mf, string name = "")
        {
            mf = thiss.CreateEmptyScriptObject<MeshFilter>(name, HideFlags.None);
            mf.sharedMesh = mesh;
            mr = mf.gameObject.AddComponent<MeshRenderer>();
            mr.sharedMaterials = materials;

            return mf.gameObject;
        }

        public static GameObject CreateEmtpyMeshObject(this GameObject thiss, Mesh mesh, Material material, string name = "")
        {
            return thiss.transform.CreateEmptyMeshObject(mesh, material, out _, out _, name);
        }

        public static GameObject CreateEmptyMeshObject(this Transform thiss, Mesh mesh, Material material, string name = "")
        {
            return thiss.CreateEmptyMeshObject(mesh, material, out _, out _, name);
        }

        public static GameObject CreateEmptyMeshObject(this GameObject thiss, Mesh mesh, Material material, out MeshRenderer mr, out MeshFilter mf, string name = "")
        {
            return thiss.transform.CreateEmptyMeshObject(mesh, material, out mr, out mf, name);
        }

        public static GameObject CreateEmptyMeshObject(this Transform thiss, Mesh mesh, Material material, out MeshRenderer mr, out MeshFilter mf, string name = "")
        {
            mf = thiss.CreateEmptyScriptObject<MeshFilter>(name, HideFlags.None);
            mf.sharedMesh = mesh;
            mr = mf.gameObject.AddComponent<MeshRenderer>();
            mr.sharedMaterial = material;

            return mf.gameObject;
        }

        public static void RemoveNegativeScales(this Collider thisss)
        {
            BoxCollider bc = thisss as BoxCollider;
            SphereCollider sc = thisss as SphereCollider;
            CapsuleCollider cc = thisss as CapsuleCollider;

            Vector3 localScale = thisss.transform.localScale;

            if (bc != null || sc != null || cc != null)
            {
                bool flippedX = false;
                bool flippedY = false;
                bool flippedZ = false;

                if (localScale.x < 0)
                {
                    localScale = localScale.withX(-localScale.x);
                    flippedX = false;
                }
                else if (localScale.y < 0)
                {
                    localScale = localScale.withY(-localScale.y);
                    flippedY = false;
                }
                else if (localScale.z < 0)
                {
                    localScale = localScale.withZ(-localScale.z);
                    flippedZ = false;
                }

                if (bc != null)
                {
                    bc.center = new Vector3(bc.center.x * (flippedX ? -1 : 1), bc.center.y * (flippedY ? -1 : 1), bc.center.z * (flippedZ ? -1 : 1));
                    bc.size = new Vector3(Mathf.Abs(bc.size.x), Mathf.Abs(bc.size.y), Mathf.Abs(bc.size.z));
                }
                else if (sc != null)
                {
                    sc.center = new Vector3(sc.center.x * (flippedX ? -1 : 1), sc.center.y * (flippedY ? -1 : 1), sc.center.z * (flippedZ ? -1 : 1));
                    sc.radius = Mathf.Abs(sc.radius);
                }
                else if (cc != null)
                {
                    cc.center = new Vector3(cc.center.x * (flippedX ? -1 : 1), cc.center.y * (flippedY ? -1 : 1), cc.center.z * (flippedZ ? -1 : 1));
                    cc.radius = Mathf.Abs(cc.radius);
                    cc.height = Mathf.Abs(cc.height);
                }

                thisss.transform.localScale = localScale;
            }
        }

        public static void DestroyChildren(this Transform thiss)
        {
            while (thiss.childCount > 0)
            {
                UnityEngine.Object.DestroyImmediate(thiss.GetChild(0).gameObject);
            }
        }

        public static void DestroyChildren(this GameObject thiss)
        {
            thiss.transform.DestroyChildren();
        }

        public static bool HasDuplicates<T>(this T thiss) where T : IEnumerable
        {
            foreach (var obj in thiss)
            {
                int count = 0;
                foreach (var obj1 in thiss)
                {
                    if (obj.Equals(obj1))
                        count++;
                    if (count > 1)
                        return true;
                }
            }
            return false;
        }

        public static bool ContainsPoint(this Collider collider, Vector3 point)
        {
            return (collider.ClosestPoint(point) - point).sqrMagnitude <= Mathf.Epsilon;
        }

        /// Given a set of colliders, find the point within any of them which is closest to a specified point in worldspace
        public static Vector3 ClosestPointOnColliders(this IEnumerable<Collider> colliders, Vector3 worldPoint)
        {
            // If there are no colliders, just return the world point
            if (!colliders.Any())
                return worldPoint;

            Vector3 closestPoint = Vector3.positiveInfinity;
            float closestSqrDist = float.PositiveInfinity;
            foreach (var col in colliders)
            {
                if (col == null)
                    continue;

                var pointCandidate = col.ClosestPoint(worldPoint);
                var sqrDistCandiate = Vector3.SqrMagnitude(pointCandidate - worldPoint);
                if (sqrDistCandiate < closestSqrDist)
                {
                    closestPoint = pointCandidate;
                    closestSqrDist = sqrDistCandiate;

                    if (closestSqrDist <= Mathf.Epsilon)
                        return closestPoint;
                }
            }

            return closestPoint;
        }

        // public static bool EqualsAny<T>(this T thiss, T e1) where T : struct
        // {
        //     T firstOne = thiss;// args[0];


        //     bool ret = thiss.Equals(e1);


        //     return ret;
        // }


        // public static bool EqualsAny<T>(this T thiss, T e1, T e2) where T : struct
        // {
        //     T firstOne = thiss;// args[0];


        //     bool ret = thiss.Equals(e1) || thiss.Equals(e2);


        //     return ret;
        // }


        // public static bool EqualsAny(this int thiss, int i1, int i2)
        // {
        //     int firstOne = thiss;// args[0];


        //     bool ret = thiss == i1 || thiss == i2;
        //     return ret;
        // }

        public static float NextStateTimeLeft(this Animator thiss, int layer = 0)
        {
            return CurrentStateTimeLeft(thiss, layer) + thiss.GetNextAnimatorStateInfo(layer).length;
        }

        public static float CurrentStateTimeLeft(this Animator thiss, int layer = 0)
        {
            var stateInfo = thiss.GetCurrentAnimatorStateInfo(layer);
            return (1 - stateInfo.normalizedTime) * stateInfo.length;
        }

        public static float CurrentStateTimeSeconds(this Animator thiss, int layer = 0)
        {
            return thiss.GetCurrentAnimatorStateInfo(layer).normalizedTime * thiss.GetCurrentAnimatorStateInfo(layer).length;
        }

        public static float NextStateTimeSeconds(this Animator thiss, int layer = 0)
        {
            return thiss.GetNextAnimatorStateInfo(layer).normalizedTime * thiss.GetNextAnimatorStateInfo(layer).length;
        }

        public static float GetDuration(this AnimationCurve thiss)
        {
            if (thiss.keys.Length == 0)
            {
                return 0;
            }
            else
            {
                return thiss.keys[thiss.keys.Length - 1].time;
            }
        }

        public static string xuSubstring(this string thiss, int startIdx, int endIdx)
        {
            if (endIdx >= 0)
            {
                return thiss.Substring(startIdx, endIdx);
            }
            else
            {
                return thiss.Substring(startIdx, thiss.Length + endIdx);
            }
        }

        /// <summary>
        /// Return a clipped version of a string if it exceeds 'maxLen'
        /// optionally append ellipses if the string is clipped
        /// </summary>
        /// <param name="toClip"></param>
        /// <param name="maxLen"></param>
        /// <param name="apendEllipses"></param>
        /// <returns></returns>
        public static string clippedToMaxLength(this string toClip, int maxLen, bool apendEllipses = true)
        {
            string ret = toClip;
            //int cliplen = maxLen2;// !apendEllipses ? maxLen2 + 3 : maxLen2;
            bool needsClip = toClip.Length > maxLen;

            if (needsClip)
            {
                int clipLen = maxLen - (apendEllipses ? 3 : 0);

                ret = toClip.Substring(0, clipLen);
                if (apendEllipses)
                {
                    ret = ret + "...";
                }
            }
            return ret;
        }

        public static string Reversed(this string str)
        {
            StringBuilder s = new();
            for (int i = 0; i < str.Length; i++)
            {
                s.Append(str[str.Length - i - 1]);
            }
            return s.ToString();
        }

        public static bool ContainsElement<T>(this T[] thiss, T elem) where T : class
        {
            foreach (T t in thiss)
            {
                if (t.Equals(elem))
                {
                    return true;
                }
            }
            return false;
        }

        public static void Shuffle<T>(this IList<T> thiss)
        {
            if (thiss == null || thiss.Count < 2) { return; }

            for (int i = 0; i < thiss.Count; i++)
            {
                int j = UnityEngine.Random.Range(0, thiss.Count);
                T temp = thiss[i];
                thiss[i] = thiss[j];
                thiss[j] = temp;
            }
        }

        public delegate float SortFunction<T>(T input);
        /// <summary>
        /// Returns the element with the highest non-negative value as returned by sortFunction.
        /// (I think something similar exists in LINQ? Standard function programming thing 'Reduce' maybe?)
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <param name="sortFunction"></param>
        /// <returns></returns>
        public static T GetMaxElement<T>(this IList<T> list, SortFunction<T> sortFunction)
        {
            T ret = default;
            float bestValue = 0;

            for (int i = 0; i < list.Count; i++)
            {
                var e = list[i];
                var value = sortFunction(e);
                if ((i == 0 || value > bestValue) && value >= 0)
                {
                    ret = e;
                    bestValue = value;
                }
            }
            return ret;
        }

        public static void SetAllElements<T>(this IList<T> list, T val)
        {
            for (int i = 0; i < list.Count; i++)
            {
                list[i] = val;
            }
        }
        public static bool IsNullOrEmpty<T>(this IList<T> l)
        {
            return l == null || l.Count == 0;
        }

        public static int CountNullRobust<T>(this IList<T> l)
        {
            return l == null ? 0 : l.Count;
        }

        /// For each layer index, the LayerMask of that layer's collision map
        private static LayerMask[] masksPerLayer = new LayerMask[32];
        private static bool[] didCachePerLayerMask = new bool[32];
        /// Return the LayerMask that a given physics layer uses, as configured in ProjectSettings/Physics
        public static LayerMask GetCollisionMaskForLayer(int layerIndex)
        {
            // Generate layermask if we don't already have it:
            if (!didCachePerLayerMask[layerIndex])
            {
                for (var i = 0; i < 32; i++)
                {
                    if (!Physics.GetIgnoreLayerCollision(layerIndex, i))
                    {
                        masksPerLayer[layerIndex] |= 1 << i;
                    }
                }
                didCachePerLayerMask[layerIndex] = true;
            }

            return masksPerLayer[layerIndex];
        }
        public static LayerMask GetCollisionMask(this GameObject g) => GetCollisionMaskForLayer(g.layer);

        public static float GetScaledRadius(this CharacterController controller) => controller.radius * controller.transform.lossyScale.x;
        public static float GetScaledHeight(this CharacterController controller) => controller.height * controller.transform.lossyScale.x;
        public static Vector3 GetBotttom(this CharacterController controller) =>
            controller.transform.TransformPoint(controller.center - Vector3.up * Mathf.Max(controller.radius, controller.height / 2f));
        public static Vector3 GetTop(this CharacterController controller) =>
            controller.transform.TransformPoint(controller.center + Vector3.up * Mathf.Max(controller.radius, controller.height / 2f));
        /// Return the top of the bottom hemisphere of the capsule (which would be the collider's bottom if it were a cylinder instead of a capsule)
        public static Vector3 GetCylinderBottom(this CharacterController controller) =>
            controller.transform.TransformPoint(controller.center - Vector3.up * Mathf.Max(controller.radius, controller.height / 2f - controller.radius));
        /// Return the top of the top hemisphere of the capsule (which would be the collider's top if it were a cylinder instead of a capsule)
        public static Vector3 GetCylinderTop(this CharacterController controller) =>
            controller.transform.TransformPoint(controller.center + Vector3.up * Mathf.Max(controller.radius, controller.height / 2f - controller.radius));

        public static float GetScaledRadius(this CapsuleCollider capsule) => capsule.radius * capsule.transform.lossyScale.x;
        public static float GetScaledHeight(this CapsuleCollider capsule) => capsule.height * capsule.transform.lossyScale.x;

        /// <summary>
        /// Return the "top" or "bottom" of a capsulse collider (pass 1 for top, -1 for bottom)
        /// </summary>
        /// <param name="cc"></param>
        /// <param name="direction"></param>
        /// <returns></returns>
        public static Vector3 GetEnd(this CapsuleCollider cc, int direction)
        {
            var center = cc.transform.TransformPoint(cc.center);
            Vector3 dir = Vector3.zero;
            dir[cc.direction] = .5f * Mathf.Max(2 * cc.radius, cc.height);
            dir = cc.transform.TransformVector(dir);
            return center + direction * dir;
        }

        /// Return the top of the bottom hemisphere of the capsule (which would be the collider's bottom if it were a cylinder instead of a capsule)
        public static Vector3 GetCylinderBottom(this CapsuleCollider capsule)
        {
            return GetCylinderBottom(capsule, capsule.radius, capsule.height);
        }
        /// Return the top of the bottom hemisphere of the capsule (which would be the collider's bottom if it were a cylinder instead of a capsule)
        public static Vector3 GetCylinderBottom(this CapsuleCollider capsule, float localRadius, float localHeight)
        {
            return capsule.transform.TransformPoint(capsule.center - Vector3.up * Mathf.Max(localRadius, localHeight / 2f - localRadius));
        }

        /// Return the top of the top hemisphere of the capsule (which would be the collider's top if it were a cylinder instead of a capsule)
        public static Vector3 GetCylinderTop(this CapsuleCollider capsule)
        {
            return GetCylinderTop(capsule, capsule.radius, capsule.height);
        }
        /// Return the top of the top hemisphere of the capsule (which would be the collider's top if it were a cylinder instead of a capsule)
        public static Vector3 GetCylinderTop(this CapsuleCollider capsule, float localRadius, float localHeight)
        {
            return capsule.transform.TransformPoint(capsule.center + Vector3.up * Mathf.Max(localRadius, localHeight / 2f - localRadius));
        }

        /// Given a transform and capsule parameters, calculate where the "cylinder bottom" (flat side of lower hemisphere) of that capsule would be. Useful for building Physics.CapsuleCast parameters
        public static Vector3 GetCapsuleCylinderBottom(Vector3 transformPos, Quaternion transformRot, Vector3 transformScale, Vector3 center, float height, float radius)
        {
            var worldCenter = transformRot * (center.multComponents(transformScale)) + transformPos;
            var scaledRadius = radius * transformScale.x;
            var scaledHeight = height * transformScale.x;
            return worldCenter - ((transformRot * Vector3.up) * Mathf.Max(scaledRadius, scaledHeight / 2f - scaledRadius));
        }

        /// Given a transform and capsule parameters, calculate where the "cylinder top" (flat side of upper hemisphere) of that capsule would be. Useful for building Physics.CapsuleCast parameters
        public static Vector3 GetCapsuleCylinderTop(Vector3 transformPos, Quaternion transformRot, Vector3 transformScale, Vector3 center, float height, float radius)
        {
            var worldCenter = transformRot * (center.multComponents(transformScale)) + transformPos;
            var scaledRadius = radius * transformScale.x;
            var scaledHeight = height * transformScale.x;
            return worldCenter + ((transformRot * Vector3.up) * Mathf.Max(scaledRadius, scaledHeight / 2f - scaledRadius));
        }

        public static Vector3 GetRootPos(this NavMeshAgent agent) => agent.nextPosition + Vector3.up * agent.transform.lossyScale.x * -agent.baseOffset;


        public static Bounds GetBounds(this Rigidbody rb, bool includeInactiveChildren = false)
        {
            var colliders = rb.GetComponentsInChildren<Collider>(includeInactiveChildren);
            return GetBounds(rb.GetComponentsInChildren<Collider>(includeInactiveChildren));
        }

        public static void Stop(this Rigidbody rb)
        {
            #if UNITY_6000_0_OR_NEWER
            rb.linearVelocity *= 0;
            rb.angularVelocity *= 0;
            #else
            rb.velocity *= 0;
            rb.angularVelocity *= 0;
            #endif
        }

        public static Bounds GetBounds(this Collider[] colliders)
        {
            if (colliders == null)
                return default;

            var outBounds = new Bounds();
            bool gotFirstBounds = false;
            foreach (var c in colliders)
            {
                if (!gotFirstBounds)
                {
                    outBounds = c.bounds;
                    gotFirstBounds = true;
                }
                else
                    outBounds.Encapsulate(c.bounds);
            }

            return outBounds;
        }
    }
    public static class XUGizmos
    {
        public static void DrawSphereCast(Vector3 start, float radius, Vector3 direction, float length)
        {
            //Gizmos.color = color;

            Gizmos.DrawWireSphere(start, radius);
            Gizmos.DrawWireSphere(start + (direction * length), radius);

            Quaternion[] rots = { Quaternion.Euler(-90, 0, 0), Quaternion.Euler(90, 0, 0), Quaternion.Euler(0, -90, 0), Quaternion.Euler(0, 90, 0) };
            foreach (Quaternion rot in rots)
            {
                Vector3 edgeLineOffset = rot * direction;
                edgeLineOffset *= radius;

                Gizmos.DrawLine(start + edgeLineOffset, start + direction * length + edgeLineOffset);

            }
        }

        public static void DrawCircle(Vector3 center, Vector3 forward, float radius, int nSegs = 32)
        {
            Vector3 radiusVec = Vector3.Cross(forward, forward.Swizzled(1, 2, 0)).normalized * radius;
            for (float i = 0; i < nSegs; i++)
            {
                float ang1 = i / nSegs;
                float ang2 = (i + 1) / nSegs;

                Gizmos.DrawLine(
                    center + Quaternion.AngleAxis(360 * ang1, forward) * radiusVec,
                    center + Quaternion.AngleAxis(360 * ang2, forward) * radiusVec
                    );
            }
        }
    }

    public static class XUUtil
    {
        public static Vector3 CatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
        {
            float t2 = t * t;
            float t3 = t2 * t;

            return .5f * ((2.0f * p1) +
                (-p0 + p2) * t +
                (2.0f * p0 - 5.0f * p1 + 4 * p2 - p3) * t2 +
                (-p0 + 3.0f * p1 - 3.0f * p2 + p3) * t3);
        }

        public static Vector3 CatmullRom(IList<Vector3> controlPoints, float t, bool endPointsAreInvisible = false)
        {
            if (controlPoints.Count < 3)
                return Vector3.zero;

            int nSegs = controlPoints.Count - (endPointsAreInvisible ? 3 : 1);
            t *= nSegs;

            int segInd = Mathf.Min(nSegs - 1, (int)t);
            t = Mathf.Min(1, t - segInd);

            segInd += endPointsAreInvisible ? 1 : 0;

            Vector3 p1 = controlPoints[segInd];
            Vector3 p2 = controlPoints[segInd + 1];
            Vector3 p0 = segInd > 0 ? controlPoints[segInd - 1] : p1 + (controlPoints[segInd + 2] - p2).normalized * (p1 - p2).magnitude;
            Vector3 p3 = segInd + 2 < controlPoints.Count ? controlPoints[segInd + 2] : p2 + (controlPoints[segInd - 1] - p1).normalized * (p1 - p2).magnitude;

            return CatmullRom(p0, p1, p2, p3, t);
        }

        public static XUTransform CatmullRom(List<Transform> controlPoints, float t, bool endPointsAreInvisible = false)
        {
            if (controlPoints.Count < 3)
                return XUTransform.Identity;

            int nSegs = controlPoints.Count - (endPointsAreInvisible ? 3 : 1);
            t *= nSegs;

            int segInd = Mathf.Min(nSegs - 1, (int)t);
            t = Mathf.Min(1, t - segInd);

            segInd += endPointsAreInvisible ? 1 : 0;

            Vector3 p1 = controlPoints[segInd].position;
            Vector3 p2 = controlPoints[segInd + 1].position;
            Vector3 p0 = segInd > 0 ? controlPoints[segInd - 1].position : p1 + (controlPoints[segInd + 2].position - p2).normalized * (p1 - p2).magnitude;
            Vector3 p3 = segInd + 2 < controlPoints.Count ? controlPoints[segInd + 2].position : p2 + (controlPoints[segInd - 1].position - p1).normalized * (p1 - p2).magnitude;

            return new XUTransform(CatmullRom(p0, p1, p2, p3, t), Quaternion.SlerpUnclamped(controlPoints[segInd].rotation, controlPoints[segInd + 1].rotation, t));
        }

        public static float GetNonZeroDeltaTime(float min = .0001f)
        {
            // return Mathf.Max(min,Time.deltaTime);
            float dt = Time.deltaTime;
            return dt > min ? dt : min;
        }
        // Adapted from https://stackoverflow.com/questions/7802822/all-possible-combinations-of-a-list-of-values
        public static IEnumerable<IEnumerable<T>> GetAllCombinations<T>(params T[] values)
        {
            return values.GetAllCombinations();
        }

        public static IEnumerable<IEnumerable<T>> GetAllCombinationsContainingElement<T>(int elemIndex, params T[] values)
        {
            return values.GetAllCombinations(elemIndex);
        }

        public static IEnumerable<T> GetCombination<T>(int nCombinations, string combString, T[] values)
        {
            for (int i = 0; i < combString.Length; i++)
            {
                if (combString[i] == '1')
                {
                    yield return values[i];
                }
            }
        }

        // Example: set a, set b, set c => 
        // a, b, c, a * b, a * c, b * c, a * b * c
        public static IEnumerable<IEnumerable<T>> GetCartesianProductCombinations<T>(params IEnumerable<T>[] sets)
        {
            return sets.GetCartesianProductsOfSetCombinations();
        }

        public static IEnumerable<IEnumerable<T>> GetCartesianProduct<T>(params IEnumerable<T>[] sets)
        {
            return sets.GetCartesianProduct();
        }

        public static float GetDistanceToLineSeg(Vector3 point, Vector3 segStart, Vector3 segEnd) =>
            GetDistanceToLineSeg(point, segStart, segEnd, out var _);

        public static float GetDistanceToLineSeg(Vector3 point, Vector3 segStart, Vector3 segEnd, out Vector3 closestPt)
        {
            float segT = GetClosestLineSegT(point, segStart, segEnd);

            closestPt = Vector3.Lerp(segStart, segEnd, segT);
            return Vector3.Distance(point, closestPt);
        }

        public static float GetClosestLineSegT(Vector3 point, Vector3 segStart, Vector3 segEnd)
        {
            Vector3 diff = segEnd - segStart;
            float dx = diff.x;
            float dy = diff.y;
            float dz = diff.z;
            if ((dx == 0) && (dy == 0) && (dz == 0))
            {
                return 0;
            }

            Vector3 fromStart = point - segStart;
            float t = (fromStart.x * dx + fromStart.y * dy + fromStart.z * dz) / diff.sqrMagnitude;

            return t;
        }


        // https://stackoverflow.com/questions/2049582/how-to-determine-if-a-point-is-in-a-2d-triangle
        static float TriangleSign(Vector3 p1, Vector3 p2, Vector3 p3)
        {
            return (p1.x - p3.x) * (p2.y - p3.y) - (p2.x - p3.x) * (p1.y - p3.y);
        }

        public static bool IsPointInsideTriangle(Vector3 p, Vector3 a, Vector3 b, Vector3 c)
        {
            float d1, d2, d3;
            bool has_neg, has_pos;

            d1 = TriangleSign(p, a, b);
            d2 = TriangleSign(p, b, c);
            d3 = TriangleSign(p, c, a);

            has_neg = (d1 < 0) || (d2 < 0) || (d3 < 0);
            has_pos = (d1 > 0) || (d2 > 0) || (d3 > 0);

            return !(has_neg && has_pos);
        }

        public static bool LineSegmentToTriangleIntersection(Vector3 p0, Vector3 p1, Vector3 a, Vector3 b, Vector3 c, out float intersectionT, out float intersectionU, out float intersectionV, float epsilon = .0001f)
        {
            intersectionT = 0;

            Vector3 toSegEnd = p1 - p0;
            float segLen = Vector3.Magnitude(toSegEnd);

            Ray ray = new Ray(p0, toSegEnd.normalized);

            bool rayIntersects = RayToTriangleIntersection(ray, a, b, c, out float t, out intersectionU, out intersectionV, out _);
            if (!rayIntersects || t >= segLen - epsilon || t < epsilon)
            {
                bool oppositeRayIntersects = RayToTriangleIntersection(new Ray(p1, -ray.direction), a, b, c, out t, out intersectionU, out intersectionU, out _);
                if (!oppositeRayIntersects || t >= segLen - epsilon || t < epsilon)
                    return false;

                intersectionT = 1 - t / segLen;
                return true;
            }
            else
            {
                intersectionT = t / segLen;
                return true;
            }
        }

        public static bool RayToTriangleIntersection(Ray R, Vector3 A, Vector3 B, Vector3 C, out float intersectionT, out float intersectionU, out float intersectionV, out bool hitFrontFace)
        {
            return RayToTriangleIntersection(R, A, B, C, true, false, out intersectionT, out intersectionU, out intersectionV, out hitFrontFace);
        }

        public static bool RayToTriangleIntersection(Ray R, Vector3 A, Vector3 B, Vector3 C, bool castAgainstFrontFaces, bool castAgainstBackfaces, out float intersectionT, out float intersectionU, out float intersectionV, out bool hitFrontFace)
        {
            Vector3 E1 = B - A;
            Vector3 E2 = C - A;
            Vector3 N = Vector3.Cross(E1, E2);
            float det = -Vector3.Dot(R.direction, N);

            float invdet = 1.0f / det;
            Vector3 AO = R.origin - A;
            Vector3 DAO = Vector3.Cross(AO, R.direction);
            float u = Vector3.Dot(E2, DAO) * invdet;
            float v = -Vector3.Dot(E1, DAO) * invdet;
            float t = Vector3.Dot(AO, N) * invdet;
            intersectionT = t;
            intersectionU = u;
            intersectionV = v;
            hitFrontFace = det > 0;

            /*
            if (det == 0)
            {
                // Ray intersects with a triangle whose tangent is parallel with ray, if they are on the same plane
                Vector3 aToR = R.origin - A;
                aToR -= aToR * Vector3.Dot(aToR, N);

                if (aToR.magnitude > float.Epsilon)
                {
                    return false;
                }

                if (IsPointInsideTriangle(R.origin, A, B, C))
                {
                    intersectionT = 0;
                    return true;
                }

                bool result = false;
                intersectionT = float.MaxValue;

                float distToAB = GetDistanceToLineSeg(R.origin, A, B, out Vector3 abIntersection);
                if (Vector3.Dot(abIntersection - R.origin, R.direction) > 0)
                {
                    result = true;
                    intersectionT = Mathf.Min(intersectionT, distToAB);
                }
                float distToBC = GetDistanceToLineSeg(R.origin, B, C, out Vector3 bcIntersection);
                if (Vector3.Dot(bcIntersection - R.origin, R.direction) > 0)
                {
                    result = true;
                    intersectionT = Mathf.Min(intersectionT, distToBC);
                }
                float distToCA = GetDistanceToLineSeg(R.origin, C, A, out Vector3 caIntersection);
                if (Vector3.Dot(caIntersection - R.origin, R.direction) > 0)
                {
                    result = true;
                    intersectionT = Mathf.Min(intersectionT, distToCA);
                }

            Vector3 center = (A + B + C) / 3f;
            Vector3 toCenter = center - R.origin;
            hitFrontFace = Vector3.Dot(toCenter, R.direction) > 0;
                return result;
            }
            */

            bool faceTest = (hitFrontFace && castAgainstFrontFaces) || (!hitFrontFace && castAgainstBackfaces);

            return (faceTest && t >= 0.0 && u >= 0.0 && v >= 0.0 && (u + v) <= 1.0);
        }

        public static Ray GetRay(this Transform tran) => new Ray(tran.position, tran.forward);
        public static Ray GetRay(this XUTransform tran) => new Ray(tran.position, tran.forward);

        public static bool LineSegToLineSegIntersection(out float t0, out float t1, Vector3 start0, Vector3 end0, Vector3 start1, Vector3 end1, float epsilon = .0001f)
        {
            t0 = -1;
            t1 = -1;

            Vector3 direction0 = (end0 - start0).normalized;
            Vector3 direction1 = (end1 - start1).normalized;

            if (!LineToLineIntersection(out Vector3 lineIntersection, start0, direction0, start1, direction1, epsilon))
                return false;

            float len0 = (end0 - start0).magnitude;
            t0 = Vector3.Dot(lineIntersection - start0, direction0) / len0;
            if (t0 < 0 || t0 > 1)
                return false;

            float len1 = (end1 - start1).magnitude;
            t1 = Vector3.Dot(lineIntersection - start1, direction1) / len1;
            if (t1 < 0 || t1 > 1)
                return false;

            return true;
        }

        public static bool RayToLineSegIntersection(out float t0, out float t1, Ray ray0, Vector3 start1, Vector3 end1, float epsilon = .0001f)
        {
            t0 = -1;
            t1 = -1;

            Vector3 direction1 = (end1 - start1).normalized;

            if (!LineToLineIntersection(out Vector3 lineIntersection, ray0.origin, ray0.direction, start1, direction1, epsilon))
                return false;

            t0 = Vector3.Dot(lineIntersection - ray0.origin, ray0.direction);
            if (t0 < 0)
                return false;

            float len1 = (end1 - start1).magnitude;
            t1 = Vector3.Dot(lineIntersection - start1, direction1) / len1;
            if (t1 < 0 || t1 > 1)
                return false;

            return true;
        }


        public static bool LineToLineIntersection(out Vector3 intersection, Vector3 linePoint1, Vector3 lineDirection1, Vector3 linePoint2, Vector3 lineDirection2, float epsilon = .0001f)
        {
            Vector3 lineVec3 = linePoint2 - linePoint1;
            Vector3 crossVec1and2 = Vector3.Cross(lineDirection1, lineDirection2);
            Vector3 crossVec3and2 = Vector3.Cross(lineVec3, lineDirection2);
            float planarFactor = Vector3.Dot(lineVec3, crossVec1and2);

            //is coplanar, and not parallel
            if (Mathf.Abs(planarFactor) < epsilon
                    && crossVec1and2.sqrMagnitude > epsilon)
            {
                float s = Vector3.Dot(crossVec3and2, crossVec1and2) / crossVec1and2.sqrMagnitude;
                intersection = linePoint1 + (lineDirection1 * s);
                return true;
            }
            else
            {
                intersection = Vector3.zero;
                return false;
            }
        }

        /// <summary>
        /// remap inputVal such that it will be no greater than fullClampedOutput.
        /// </summary>
        public static float AsymptoticClamp(float inputVal, float clampStart, float clampEnd, float maxOutput, out float saturateAmt)
        {
            saturateAmt = 0;
            float ret = inputVal;
            //float clampStart = Mathf.Max(0, maxLength - clampStartOffset);
            //float clampEnd = maxLength + saturateEndOffset;
            //saturateLength = Mathf.Clamp(saturateLength, clampStart, clampEnd);
            if (inputVal > clampStart)
            {
                saturateAmt = Mathf.InverseLerp(clampStart, clampEnd, inputVal);
                //float easedSaturate = Easing.easeOutSine(0, 1, saturateAmt);//(1 - Mathf.Pow((1 - saturateAmt) * .5f, 2));
                float easedSaturate = saturateAmt;//(1 - Mathf.Pow((1 - saturateAmt) * .5f, 2));
                float clampedMag = Mathf.Lerp(clampStart, maxOutput, easedSaturate);

                ret = clampedMag;
            }
            return ret;
        }
        public static float AsymptoticClampBiDirectional(float inputVal, float clampStart, float clampEnd, float fullClampedOutput, out float saturateAmt)
        {
            float coeff = inputVal < 0 ? -1 : 1;
            var ret = coeff * AsymptoticClamp(coeff * inputVal, clampStart, clampEnd, fullClampedOutput, out saturateAmt);
            saturateAmt *= coeff;
            return ret;
        }

        public static float RemapLerp(float inRangeMin, float inRangeMax, float outRangeMin, float outRangeMax, float value)
        {
            return Mathf.Lerp(outRangeMin, outRangeMax, Mathf.InverseLerp(inRangeMin, inRangeMax, value));
        }

        public static int GetBitValue(int bitString, int bitIndex)
        {
            return (bitString & 1 << bitIndex) > 0 ? 1 : 0;
        }

        public static void Swap<T>(ref T a, ref T b)
        {
            T temp = a;
            a = b;
            b = temp;
        }

        public delegate T InterpolateFunction<T>(T a, T b, float t);

        public static Quaternion SmoothSampleQuaternionArray(IList<Quaternion> arr, float t)
        {
            float smoothIdx = t * (arr.Count - 1);
            int leftIdx = Mathf.FloorToInt(smoothIdx);
            int rightIdx = (int)Mathf.Min(leftIdx + 1, arr.Count - 1);//Mathf.CeilToInt(t * (arr.Length - 1));

            Quaternion leftElement = arr[leftIdx];
            Quaternion rightElement = arr[rightIdx];
            float mixAmt = smoothIdx - leftIdx;

            return Quaternion.Lerp(leftElement, rightElement, mixAmt);
        }

        public static T SmoothSampleArray<T>(IList<T> arr, float t, InterpolateFunction<T> interpolator)
        {
            float smoothIdx = t * (arr.Count - 1);
            int leftIdx = Mathf.FloorToInt(smoothIdx);
            int rightIdx = (int)Mathf.Min(leftIdx + 1, arr.Count - 1);//Mathf.CeilToInt(t * (arr.Length - 1));

            T leftElement = arr[leftIdx];
            T rightElement = arr[rightIdx];
            float mixAmt = smoothIdx - leftIdx;

            return interpolator(leftElement, rightElement, mixAmt);
        }

        public static List<T> DownSampleArray<T>(IList<T> arr, float resolution, Func<T, T, T, float> ptPriorityFunc)
        {
            int targetCount = Mathf.Min(Mathf.RoundToInt(arr.Count * resolution), arr.Count);

            List<int> includedPts = new List<int>();
            includedPts.Add(0);
            includedPts.Add(arr.Count - 1);

            while (includedPts.Count < targetCount)
            {
                float highestPriFound = float.MinValue;
                int segFound = -1;
                int ptFound = -1;

                for (int i = 0; i < includedPts.Count - 1; i++)
                {
                    int a = includedPts[i];
                    int b = includedPts[i + 1];

                    if (b == a + 1)
                        continue;

                    T ptA = arr[a];
                    T ptB = arr[b];

                    for (int j = a + 1; j < b; j++)
                    {
                        T ptC = arr[j];

                        float priFound = ptPriorityFunc.Invoke(ptA, ptB, ptC);

                        if (priFound > highestPriFound)
                        {
                            highestPriFound = priFound;
                            segFound = i + 1;
                            ptFound = j;
                        }
                    }
                }

                includedPts.Insert(segFound, ptFound);
            }

            List<T> ret = new List<T>();
            foreach (int i in includedPts)
                ret.Add(arr[i]);

            return ret;
        }

        public static float SmoothSampleArray(IList<float> arr, float t)
        {
            return SmoothSampleArray(arr, t, Mathf.Lerp);
        }

        public static Vector3 SmoothSampleArray(IList<Vector3> arr, float t)
        {
            return SmoothSampleArray(arr, t, Vector3.Lerp);
        }

        public static Quaternion SmoothSampleArray(IList<Quaternion> arr, float t)
        {
            return SmoothSampleArray(arr, t, RobustLerp);
        }

        static Quaternion RobustLerp(Quaternion a, Quaternion b, float t)
        {

            Quaternion ret = Quaternion.Lerp(a, b, t);

            for (int i = 0; i < 4; i++)
            {
                if (ret[i] != ret[i])
                {
                    return Quaternion.identity;
                }
            }
            return ret;
        }

        public static Vector4 SmoothSampleArray(Vector4[] arr, float t)
        {
            return SmoothSampleArray(arr, t, Vector4.Lerp);
        }

        // public static SavedTransform SmoothSampleArray(SavedTransform[] arr, float t)
        // {
        //     return SmoothSampleArray(arr, t, SavedTransform.Lerp);
        // }


        public static Vector3 RandomBetween(Vector3 min, Vector3 max)
        {
            float x = UnityEngine.Random.Range(min.x, max.x);
            float y = UnityEngine.Random.Range(min.y, max.y);
            float z = UnityEngine.Random.Range(min.z, max.z);

            return new Vector3(x, y, z);
        }

        public static void DrawCenteredText(Vector2 offset, string text, int fontSize, Color fontColor, string font)
        {
            GUIStyle style = new GUIStyle();
            style.normal.textColor = fontColor;
            style.fontSize = fontSize;
            style.font = (Font)Resources.Load("Fonts/" + font);
            Vector2 size = style.CalcSize(new GUIContent(text));
            Vector2 position = new Vector2(Screen.width, Screen.height) / 2 - size / 2;
            position += offset;
            GUI.Label(new Rect(position.x, position.y, size.x, size.y), text, style);
        }

        public static Vector3 RandomDiskVector() => DiskVectorFromRadians(UnityEngine.Random.Range(0f, Mathf.PI * 2f));
        public static Vector3 RandomDiskVector(Vector3 planeDir) => DiskVectorFromRadians(UnityEngine.Random.Range(0f, Mathf.PI * 2f), planeDir);

        public static Vector3 DiskVectorFromRadians(float theta)
        {
            return new Vector3(Mathf.Cos(theta), 0f, Mathf.Sin(theta));
        }

        public static Vector3 DiskVectorFromRadians(float theta, Vector3 planeDir)
        {
            return Quaternion.FromToRotation(Vector3.up, planeDir) * new Vector3(Mathf.Cos(theta), 0f, Mathf.Sin(theta));
        }


        public static void DrawText(Vector2 position, string text, int fontSize, Color fontColor, string font = null)
        {
            GUIStyle style = new GUIStyle();
            style.normal.textColor = fontColor;
            style.fontSize = fontSize;

            if (font != null)
            {
                style.font = (Font)Resources.Load("Fonts/" + font);
            }

            Vector2 size = style.CalcSize(new GUIContent(text));
            GUI.Label(new Rect(position.x, position.y, size.x, size.y), text, style);
        }

        public static Texture2D CreateSolidColorTexture(int w, int h) => CreateSolidColorTexture(w, h, Color.white);
        public static Texture2D CreateSolidColorTexture(int w, int h, Color color)
        {
            var ret = new Texture2D(1, 1);
            for (int i = 0; i < w; i++)
            {
                for (int j = 0; j < h; j++)
                {
                    ret.SetPixel(i, j, color);
                }
            }

            ret.Apply();
            return ret;
        }

        public static void DrawTextAtWorldPosition(Vector3 worldPt, string text, int fontSize, Color fontColor, Camera cam, Vector2 offset = new Vector2())
        {
            if (cam == null)
            {
                return;
            }
            Vector3 position = cam.WorldToScreenPoint(worldPt);//Camera.main.WorldToScreenPoint(worldPt);
            position.y = Screen.height - position.y;
            GUIStyle style = new GUIStyle();
            style.normal.textColor = fontColor;
            style.fontSize = fontSize;
            Vector2 size = style.CalcSize(new GUIContent(text));
            GUI.Label(new Rect(position.x + offset.x - size.x / 2, position.y - offset.y - size.y / 2, size.x, size.y), text, style);
            //GUI.Label(new Rect(position.x, position.y, size.x, size.y), text, style);
        }

        /// <summary>
        ///  is val >= min, and < max?
        /// </summary>
        /// <param name="val"></param>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <returns></returns>
        public static bool NumberIsBetween(float val, float min, float max)
        {
            return val >= min && val < max;
        }

        public static bool NumberIsBetween(double val, double min, double max)
        {
            return val >= min && val < max;
        }

        public static bool CloseEnough(float a, float b, float maxDifference)
        {
            return Mathf.Abs(a - b) < maxDifference;
        }

        /// <summary>
        ///  true if val greater than or equal min, and strictly less than max
        /// </summary>
        /// <param name="val"></param>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <returns></returns>
        public static bool NumberIsBetween(int val, int min, int max)
        {
            return val >= min && val < max;
        }



        public static Vector2 FloatyPerlinVector(float t)
        {
            float t1 = 1.007f * t;
            float t2 = 1f * t;
            float t3 = 0.9973f * t;
            float t4 = 0.9913f * t;
            float p1 = Mathf.PerlinNoise(t1, t3);
            float p2 = Mathf.PerlinNoise(t4, t2);
            return new Vector2(p1, p2);
        }

        public static void SwingTwistDecomposition(Quaternion rotation, Vector3 twistAxis, out Quaternion swing, out Quaternion twist)
        {
            twist = GetAxisTwist(rotation, twistAxis);
            swing = rotation * Quaternion.Inverse(twist);
        }

        public static Quaternion GetAxisTwist(Quaternion rotation, Vector3 twistAxis)
        {
            //Courtesy of, compliments to this person:
            // http://www.euclideanspace.com/maths/geometry/rotations/for/decomposition/forum.htm
            Vector3 rotation_axis = new Vector3(rotation.x,
            rotation.y, rotation.z);
            // return projection v1 on to v2 (parallel component)
            // here can be optimized if default_dir is unit
            Vector3 proj = Vector3.Project(rotation_axis, twistAxis);
            Vector4 normed = new Vector4(proj.x, proj.y, proj.z, rotation.w).normalized;
            Quaternion twist = new Quaternion(normed.x, normed.y, normed.z, normed.w);
            //twist_rotation.normalize();
            return twist;

            /*# ifdef _DEBUG
                xxquaternion composite = dir_rotation * twist_rotation;
                composite -= orientation;
            ASSERT(composite.magnitude() < 0.00001f );
#endif //_DEBUG */
        }

        public static void EnforceComponentReferenceType<T>(ref Component toAssign) where T : class
        {
            Component candidate = toAssign;

            if (toAssign != null)
            {
                toAssign = (candidate as T) as Component;
            }

            if (toAssign == null && candidate != null)// && toAssign.gameObject != candidate.gameObject)
            {
                T intermediate = candidate.GetComponent<T>();
                toAssign = intermediate != null ? intermediate as Component : null;
            }
        }


        public enum EasingFunctions
        {
            EaseIn,
            EaseOut,
            EaseInOut,
            HoldCenter,
        };

        public static float EaseExp(float t, EasingFunctions function, float expPow)
        {
            switch (function)
            {
                case EasingFunctions.EaseIn:
                    return Mathf.Pow(t, expPow);
                case EasingFunctions.EaseOut:
                    return 1 - Mathf.Pow(1 - t, expPow);
                case EasingFunctions.EaseInOut:
                    if (t < .5f)
                    {
                        t *= 2;
                        t = Mathf.Pow(t, expPow);
                        t *= .5f;
                        return t;
                    }
                    else
                    {
                        t -= .5f;
                        t *= 2;
                        t = 1 - Mathf.Pow(1 - t, expPow);
                        t *= .5f;
                        t += .5f;

                        return t;
                    }
                case EasingFunctions.HoldCenter:
                    if (t < .5f)
                    {
                        t *= 2;
                        t = 1 - Mathf.Pow(1 - t, expPow);
                        t *= .5f;
                        return t;
                    }
                    else
                    {
                        t -= .5f;
                        t *= 2;
                        t = Mathf.Pow(t, expPow);
                        t *= .5f;
                        t += .5f;

                        return t;
                    }
                default:
                    throw new System.ArgumentException();
            }
        }

        public static bool ApproxColorMatch(Color color1, Color color2, float tolerance = .05f, bool ignoreAlpha = true)
        {
            bool rMatches = Mathf.Abs(color1.r - color2.r) < tolerance;
            bool gMatches = Mathf.Abs(color1.g - color2.g) < tolerance;
            bool bMatches = Mathf.Abs(color1.b - color2.b) < tolerance;
            bool alphaMatches = ignoreAlpha || Mathf.Abs(color1.a - color2.a) < tolerance;

            return rMatches && gMatches && bMatches && alphaMatches;
        }


        public struct HSVColor
        {
            Vector4 _hsva;

            public HSVColor(Color rbgColor)
            {
                _hsva = rbgColor.AsHSVVector4();

            }

            public Color rbgValue
            {
                get => this;
                set => _hsva = ((HSVColor)value)._hsva;
            }

            public float h
            {
                get => _hsva[0];
                set => setElSafe(0, value);
            }

            public float s
            {
                get => _hsva[1];
                set => setElSafe(1, value);
            }

            public float v
            {
                get => _hsva[2];
                set => setElSafe(2, value);
            }

            public float a
            {
                get => _hsva[3];
                set => setElSafe(3, value);
            }

            void setElSafe(int hsvEl, float value)
            {
                _hsva[hsvEl] = Mathf.Clamp01(value);
            }
            void setColorRGBEl(int rbgEl, float value)
            {
                Color c = this;
                c[rbgEl] = value;
                this._hsva = c.AsHSVVector4();
            }

            public float r
            {
                get => ((Color)this).r;
                set => setColorRGBEl(0, value);
            }

            public float g
            {
                get => ((Color)this).g;
                set => setColorRGBEl(1, value);
            }

            public float b
            {
                get => ((Color)this).b;
                set => setColorRGBEl(2, value);
            }

            public static implicit operator HSVColor(Color rgbColor)
            {
                return new HSVColor(rgbColor);
            }

            public static implicit operator Color(HSVColor hsvColor)
            {
                var ret = Color.HSVToRGB(hsvColor.h, hsvColor.s, hsvColor.v);
                ret.a = hsvColor.a;
                return ret;
            }
        }

        public static Vector4 AsHSVVector4(this Color rbgColor)
        {
            Vector4 hsva = rbgColor;
            Color.RGBToHSV(rbgColor, out hsva.x, out hsva.y, out hsva.z);
            return hsva;
        }


        public static bool HasTagOnParent(this Transform thiss, string tag, int maxLevelsToTraverse = 1000)
        {
            if (thiss.CompareTag(tag))
                return true;

            Transform nextToCheck = thiss.parent;
            int levelCounter = maxLevelsToTraverse;
            while (nextToCheck != null && levelCounter > 0)
            {
                if (nextToCheck.CompareTag(tag))
                    return true;

                nextToCheck = nextToCheck.parent;
                levelCounter--;
            }

            return false;
        }

        /// <summary>
        /// Let's you search for an object by path in a specific scene, and is robust to the object being deactivated
        /// </summary>
        /// <param name="path"></param>
        /// <param name="scn"></param>
        /// <returns></returns>
        public static GameObject FindObjectByPath(string path, Scene scn)
        {
            GameObject ret = null;

            if (!scn.IsValid() || !scn.isLoaded)
            {
                return null;
            }

            foreach (var rootObj in scn.GetRootGameObjects())
            {
                string effPath = path.StartsWith("/") ? path.Substring(1) : path;
                if (effPath.StartsWith(rootObj.name))
                {
                    effPath = effPath.Substring(rootObj.name.Length + 1);
                    ret = rootObj.transform.Find(effPath)?.gameObject;
                    if (ret != null) break;
                }
            }
            return ret;
        }

        /// Equivalent to FindObjectsOfType for the active scene, and is able to find inactive components
        public static T[] FindObjectsOfTypeInScene<T>(Scene scene, bool includeInactive)
        {
            if (!scene.isLoaded)
                return new T[0];
            var sceneRoots = scene.GetRootGameObjects();
            var output = new List<T>();

            for (var i = 0; i < sceneRoots.Length; i++)
            {
                output.AddRange(sceneRoots[i].GetComponentsInChildren<T>(includeInactive));
            }

            return output.ToArray();
        }

        public static T[] FindObjectsOfTypeInAllScenes<T>(bool includeInactive = true)
        {
            var sceneCount = SceneManager.sceneCount;
            var output = new List<T>();
            for (var i = 0; i < sceneCount; i++)
                output.AddRange(FindObjectsOfTypeInScene<T>(SceneManager.GetSceneAt(i), includeInactive));
            return output.ToArray();
        }

        public static T[] GetAllObjectsOfType<T>() => FindObjectsOfTypeInAllScenes<T>(true);


        public static void DoToAllCompononentsInAllScenes<T>(bool includeInactive, System.Action<T> func) where T : Component
        {
            T[] allObsInAllScenes = FindObjectsOfTypeInAllScenes<T>(includeInactive);
            foreach (T t in allObsInAllScenes)
            {
                func(t);
            }
        }

        public static void DoToAllGameObjectsInAllScenes(System.Action<GameObject> func)
        {
            DoToAllCompononentsInAllScenes<Transform>(true, (t) => func(t.gameObject));
        }

        /// <summary>
        /// Works for Materials, Textures, and Meshes, but less useful for iterating over models (meshes are children of 'model' objects in project)
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="func"></param>

        public static void DoToAllCompononentsInScene<T>(Scene scene, bool includeInactive, System.Action<T> func)
        {
            foreach (GameObject rootOb in scene.GetRootGameObjects())
            {
                foreach (T t in rootOb.GetComponentsInChildren<T>(includeInactive))
                {
                    func(t);
                }
            }
        }

        public static void DoToAllCompononentsInScene(Scene scene, bool includeInactive, Type type, System.Action<object> func)
        {
            foreach (GameObject rootOb in scene.GetRootGameObjects())
            {
                foreach (object t in rootOb.GetComponentsInChildren(type, includeInactive))
                {
                    func(t);
                }
            }
        }

#if UNITY_EDITOR

        public static void SaveDataForPrefab<T>(T data, GameObject prefabRoot) where T : ScriptableObject
        {
            string assetRoot = UnityEditor.SceneManagement.PrefabStageUtility.GetPrefabStage(prefabRoot).assetPath;
            assetRoot = assetRoot.Substring(0, assetRoot.LastIndexOfAny(new char[] { '/', '\\' }));

            string assetPath = Path.Join(assetRoot, data.name);
            assetPath += ".asset";

            Debug.Log(assetPath);

            if (AssetDatabase.LoadAssetAtPath<T>(assetPath) != null)
                AssetDatabase.DeleteAsset(assetPath);

            AssetDatabase.CreateAsset(data, assetPath);

            AssetDatabase.SaveAssets();
        }

        public static void DoToAllObjectsInProject<T>(System.Action<T> func) where T : UnityEngine.Object
        {
            DoToAllObjectsInFolders((string[])null, true, func);
        }
        public static void DoToAllObjectsInFolder<T>(string folder, bool recursive, System.Action<T> func) where T : UnityEngine.Object
        {
            DoToAllObjectsInFolders(new string[] { folder }, recursive, func);
        }
        public static void DoToAllObjectsInFolders<T>(string[] folders, bool recursive, System.Action<T> func) where T : UnityEngine.Object
        {
            var all = GetAllObjectsInFolders<T>(folders, recursive);
            int i = 0;
            foreach (var obj in all)
            {
                float progress = (0f + i) / (all.Count);

                bool cancelled = EditorUtility.DisplayCancelableProgressBar("busy", "working...", progress);
                if (cancelled)
                {
                    return;
                }
                func(obj);
                i++;
            }
            EditorUtility.ClearProgressBar();
        }

        public static List<T> GetAllObjectsInProject<T>() where T : UnityEngine.Object
        {
            return GetAllObjectsInFolders<T>(null);
        }

        public static List<T> GetAllObjectsInFolders<T>(string[] folders, bool recursive = true) where T : UnityEngine.Object
        {
            List<T> assets = new List<T>();
            string[] guids;
            if (folders == null)
            {
                guids = AssetDatabase.FindAssets(string.Format("t:{0}", typeof(T).Name));
            }
            else
            {
                guids = AssetDatabase.FindAssets(string.Format("t:{0}", typeof(T).Name), folders);
            }

            for (int i = 0; i < guids.Length; i++)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guids[i]);

                bool okToAdd = true;

                if (!recursive)
                {
                    okToAdd = false;
                    foreach (var folder in folders)
                    {
                        var dirName = System.IO.Path.GetDirectoryName(assetPath).Replace('\\', '/');
                        if (folder == dirName)
                        {
                            okToAdd = true;
                            break;
                        }
                    }
                }

                if (okToAdd)
                {
                    T asset = AssetDatabase.LoadAssetAtPath<T>(assetPath);
                    if (asset != null)
                    {
                        assets.Add(asset);
                    }
                }

            }
            return assets;
        }

        public static IEnumerable<GameObject> GetEditorRootObjects()
        {
            UnityEditor.SceneManagement.PrefabStage currStage = UnityEditor.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage();
            if (currStage != null)
            {
                yield return currStage.prefabContentsRoot;
            }
            else
            {
                UnityEngine.SceneManagement.Scene activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
                foreach (GameObject rootObj in activeScene.GetRootGameObjects())
                    yield return rootObj;
            }

            yield break;
        }


#if XU_CREATE_MENU_ITEMS
        [MenuItem("CONTEXT/Transform/Normalize Root Scale/Detatch Root Components")]
#endif
        static void NormalizeRootScaleDetatch(MenuCommand command)
        {
            NormalizeRootScale(command, true);
        }


#if XU_CREATE_MENU_ITEMS
        [MenuItem("CONTEXT/Transform/Normalize Root Scale/Don't Detatch Root Components")]
#endif
        static void NormalizeRootScaleDontDetatch(MenuCommand command)
        {
            NormalizeRootScale(command, false);
        }

        static void NormalizeRootScale(MenuCommand command, bool detatch)
        {
            Transform root = command.context as Transform;
            if (root == null)
                return;

            Vector3 objScale = root.localScale;
            if (Mathf.Approximately(objScale.x, 1) && Mathf.Approximately(objScale.y, 1) && Mathf.Approximately(objScale.z, 1))
            {
                return;
            }

            Undo.RecordObject(root, "Normalize Root scale");
            if (detatch)
            {
                // Move all non-transform root components down one level - needs to be done if they are scale reliant
                List<Component> rootComponents = new List<Component>(root.GetComponents<Component>());
                rootComponents.RemoveAll(a => a is Transform);

                if (rootComponents.Count > 0)
                {
                    GameObject detatchedRoot = root.CreateEmptyChild("Detatched Root Components");
                    foreach (Component c in rootComponents)
                    {
                        Component cCopy = detatchedRoot.AddComponent(c.GetType());
                        EditorUtility.CopySerialized(c, cCopy);
                    }

                    while (rootComponents.Count > 0)
                    {
                        Component toDelete = rootComponents[0];
                        rootComponents.RemoveAt(0);
                        UnityEngine.Object.DestroyImmediate(toDelete);
                    }
                }
            }

            // Transfer root scale downwards
            foreach (Transform child in root)
            {
                Undo.RecordObject(child, "Normalize Root scale");
                child.transform.localScale = Vector3.Scale(objScale, child.transform.localScale);
                child.transform.localPosition = Vector3.Scale(child.transform.localPosition, objScale);
            }

            root.localScale = Vector3.one;
        }

#if XU_CREATE_MENU_ITEMS
        [MenuItem("CONTEXT/Transform/Recenter Parent")]
#endif
        static void RecenterParent(MenuCommand command)
        {

            Transform child = command.context as Transform;
            if (child == null)
                return;

            Transform parent = child.parent;
            if (parent == null)
                return;

            Dictionary<Transform, XUTransform> pTransforms = new Dictionary<Transform, XUTransform>();
            foreach (Transform sibling in parent)
            {
                pTransforms[sibling] = new XUTransform(sibling.position, sibling.rotation);
            }

            XUTransform centerT = pTransforms[child];
            centerT.applyToAbsoluteValuesOf(parent);

            foreach (Transform sibling in parent)
            {
                pTransforms[sibling].applyToAbsoluteValuesOf(sibling);
            }
        }

        public static void SetObjectArrayStickily(MonoBehaviour behavior, string paramName, UnityEngine.Object[] paramVal)
        {
            SerializedObject so = new SerializedObject(behavior);
            SerializedProperty sp = so.FindProperty(paramName);

            sp.ClearArray();
            for (int i = 0; i < paramVal.Length; i++)
            {
                sp.InsertArrayElementAtIndex(i);
                sp.GetArrayElementAtIndex(i).objectReferenceValue = paramVal[i];
            }

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void DoToAllProjectScenes(Func<Scene, bool> func)
        {
            DoToScenes(AssetDatabase.FindAssets("t:scene").Select(guid => AssetDatabase.GUIDToAssetPath(guid)), func);
        }

        //public static void DoToScenes(IEnumerable<TCSceneField> scenes, Func<Scene, bool> func)
        //{
        //    DoToScenes(scenes.Select(sceneField => sceneField.ScenePath), func);
        //}

        static void DoToScenes(IEnumerable<string> scenePaths, Func<Scene, bool> func)
        {
            int count = scenePaths.Count();
            int i = 0;
            int nModified = 0;
            foreach (string scenePath in scenePaths)
            {
                i += 1;
                EditorUtility.DisplayProgressBar("Processing Scenes", "Processing scenes - " + i + " of " + count + " (modified " + nModified + ")", (float)i / count);

                Scene s = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(scenePath);

                bool saveChanges = func.Invoke(s);

                if (saveChanges)
                {
                    UnityEditor.SceneManagement.EditorSceneManager.SaveScene(s);
                    nModified++;
                }
            }
            EditorUtility.ClearProgressBar();
        }

        /// <summary>
        /// Performs an action on all prefabs in the project.
        /// </summary>
        /// <param name="func">The function that will be performed, which returns true if the resulting changes should be saved to the prefab.</param>
        public static void DoToAllProjectPrefabs(Func<GameObject, bool> func)
        {
            List<string> goGUIDs = new List<string>(AssetDatabase.FindAssets("t:GameObject"));
            List<string> assetPaths = goGUIDs.ConvertAll(a => AssetDatabase.GUIDToAssetPath(a));
            assetPaths.RemoveAll(a => !a.EndsWith(".prefab"));

            int i = 0;
            int nModified = 0;

            foreach (string assetPath in assetPaths)
            {
                i += 1;
                EditorUtility.DisplayProgressBar("Processing Prefabs", "Processing prefabs - " + i + " of " + assetPaths.Count + " (modified " + nModified + ")", (float)i / assetPaths.Count);
                GameObject rootObj = PrefabUtility.LoadPrefabContents(assetPath);

                if (func.Invoke(rootObj))
                {
                    nModified++;
                    PrefabUtility.SaveAsPrefabAsset(rootObj, assetPath);
                }

                PrefabUtility.UnloadPrefabContents(rootObj);
            }

            EditorUtility.ClearProgressBar();
        }



        public static void SetScriptingDefineSymbolOn(string symbol, bool on, bool resyncCsharpProject = true)
        {
            var group = BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget);
            SetScriptingDefineSymbolOn(symbol, on, group, resyncCsharpProject);
        }

        public static void SetScriptingDefineSymbolOn(string symbol, bool on, BuildTargetGroup group, bool resyncCsharpProject = true)
        {
            string scriptingDefinesOrig = PlayerSettings.GetScriptingDefineSymbolsForGroup(group);

            var strs = scriptingDefinesOrig.Split(';');
            var defines = new HashSet<string>();
            defines.AddRange(strs);

            if (on)
            {
                defines.Add(symbol);
            }
            else
            {
                defines.Remove(symbol);
            }

            var sb = new System.Text.StringBuilder();
            foreach (var s in defines)
            {
                sb.Append(s);
                sb.Append(';');
            }
            if (sb.Length > 0)
            {
                // Remove the last ;
                sb.Length--;
            }

            PlayerSettings.SetScriptingDefineSymbolsForGroup(group, sb.ToString());
            //
            if (resyncCsharpProject)
            {
                UnityEditor.Compilation.CompilationPipeline.RequestScriptCompilation();
                //EditorUtility.RequestScriptReload();
            }
        }

        // Like CopySerialized, but will copy shared properties across different types - adapted from https://www.reddit.com/r/Unity3D/comments/94rpgc/i_wrote_a_piece_of_code_that_allows_for/
        public static void CopySerializedShared(UnityEngine.Object source, UnityEngine.Object dest)
        {
            SerializedObject destSO = new SerializedObject(dest);
            SerializedObject sourceSO = new SerializedObject(source);

            SerializedProperty prop_iterator = sourceSO.GetIterator();
            if (prop_iterator.NextVisible(true))
            {
                while (prop_iterator.NextVisible(true))
                {
                    SerializedProperty prop_element = destSO.FindProperty(prop_iterator.name);

                    if (prop_element != null && prop_element.propertyType == prop_iterator.propertyType)
                    {
                        destSO.CopyFromSerializedProperty(prop_iterator);
                    }
                }
            }
            destSO.ApplyModifiedProperties();

            destSO.Dispose();
            sourceSO.Dispose();
        }


        public static string GetFullFilepath(string assetPath)
        {
            string basePath = Application.dataPath;
            basePath = basePath.Substring(0, basePath.Length - "Assets".Length);

            return System.IO.Path.Combine(basePath, assetPath);
        }


        #region ANIMATION PROCESSING
        //TODO: maybe make this a context menu item again?
#if XU_CREATE_MENU_ITEMS
        [MenuItem("XUUtil/Animation/Strip AnimationClip rotation keys")]
#endif
        public static void StripAnimationClipRotations(MenuCommand command)
        {
            foreach (var ob in Selection.objects)
            {
                var ac = ob as AnimationClip;
                if (ac != null)
                {
                    Undo.RecordObject(ac, "strip non-rotation keys");
                    ac.RemoveNonRotationKeys();
                }
            }
        }
        public static void RemoveNonRotationKeys(this AnimationClip oClip)
        {
            RemovePropertiesFromAnimationClip(oClip, (cb) => !cb.propertyName.ToLower().Contains("rotation"));
        }
        public delegate bool RemovePropertiesFromAnimationClipCondition(EditorCurveBinding cb);
        public static void RemovePropertiesFromAnimationClip(AnimationClip oClip, RemovePropertiesFromAnimationClipCondition removeCondition)
        {
            string oPath = AssetDatabase.GetAssetPath(oClip);
            string newPath = oPath;//$"{System.IO.Path.GetDirectoryName(oPath)}/{DERIVED_CLIP_PREFIX}{System.IO.Path.GetFileNameWithoutExtension(oPath)}.anim";

            var newClip = new AnimationClip();
            List<EditorCurveBinding> ecbs = new();
            List<AnimationCurve> acs = new();
            foreach (var cb in AnimationUtility.GetCurveBindings(oClip))
            {
                if (!removeCondition(cb))
                {
                    ecbs.Add(cb);
                    AnimationCurve curve = AnimationUtility.GetEditorCurve(oClip, cb);
                    acs.Add(curve);
                }
            }
            oClip.ClearCurves();
            AnimationUtility.SetEditorCurves(oClip, ecbs.ToArray(), acs.ToArray());
            EditorUtility.SetDirty(newClip);
            AssetDatabase.Refresh();
        }
        #endregion
#endif

        /// <summary>
        /// Warning! creates garbage
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static System.Type GetTypeFromName(string name)
        {
            string nameFixed = name;
            if (nameFixed.Contains('.'))
            {
                nameFixed = nameFixed.Replace('.', '+');
            }
            var assemblies = System.AppDomain.CurrentDomain.GetAssemblies();

            var ret = System.Type.GetType(nameFixed);
            if (ret != null)
            {
                return ret;
            }

            for (int i = 0; i < assemblies.Length; i++)
            {
                var assembly = assemblies[assemblies.Length - i - 1];
                var tt = assembly.GetType(nameFixed);
                if (tt != null)
                {
                    return tt;
                }
            }

            return null;
        }

        #region Object Copying
        public static void CloneObjectInto<T>(T src, ref T dest)
        {
            System.Type t = typeof(T);
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

        public static IEnumerable<MethodInfo> GetAllStaticMethodsWithAttribute<T>()
        {
            //get all methods with the execute attribute
            var methods = AppDomain.CurrentDomain.GetAssemblies()
                                   .SelectMany(x => x.GetTypes())
                                   .Where(x => x.IsClass)
                                   .SelectMany(x => x.GetMethods(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static))
                                   .Where(x => x.GetCustomAttributes(typeof(T), false).FirstOrDefault() != null);
            return methods;
        }

        public enum VarNameCaseMode { asIs, forceUpper, forceLower }
        public static string MakeNameSaveForCodeVariable(string str, HashSet<string> varNamesChecker = null, VarNameCaseMode caseMode = VarNameCaseMode.asIs)
        {
            string origInput = str;
            str = str.Replace(" ", "");
            str = str.Replace("\"", "");
            str = str.Replace("\'", "");
            str = str.Replace("?", "");
            str = str.Replace("-", "_");
            str = str.Replace(".", "_");
            str = str.Replace("+", "_");
            str = str.Replace("@", "");
            str = str.Replace("$", "_");

            if (caseMode == VarNameCaseMode.forceUpper)
            {
                str = ("" + str[0]).ToUpper() + str.Substring(1);
            }
            else if (caseMode == VarNameCaseMode.forceLower)
            {
                str = ("" + str[0]).ToLower() + str.Substring(1);
            }

            if (char.IsDigit(str[0]))
            {
                str = "_" + str;
            }

            if (varNamesChecker != null)
            {
                if (str.Length == 0)
                {
                    str = null;
                    throw new System.Exception($"Can't generate variable name from input '{origInput}'");
                }
                else if (varNamesChecker.Contains(str))
                {
                    str = null;
                    throw new System.Exception($"Already generated variable name '{str}' for input string '{origInput}'");
                }
                else //no issues
                {
                    varNamesChecker.Add(str);
                }
            }


            return str;
        }

        public static void SafeInvoke(this UnityEvent action, bool logExecption = true)
            => SafeInvoke(action.Invoke, logExecption);
        public static void SafeInvoke(this System.Action action, bool logExecption = true)
        {
            try
            {
                action.Invoke();
            }
            catch (Exception e)
            {
                if (logExecption) Debug.LogException(e);
            }
        }

        #region ANIMATION PROCESSING
#if UNITY_EDITOR
        public delegate bool AnimationClipEditorFunction(ref EditorCurveBinding binding, ref AnimationCurve curve);

        public static void EditAnimation(AnimationClip oClip, string savePath, AnimationClipEditorFunction editFunction)
        {
            string oPath = AssetDatabase.GetAssetPath(oClip);
            string newPath = oPath;//$"{System.IO.Path.GetDirectoryName(oPath)}/{DERIVED_CLIP_PREFIX}{System.IO.Path.GetFileNameWithoutExtension(oPath)}.anim";

            List<EditorCurveBinding> ecbs = new();
            List<AnimationCurve> acs = new();
            foreach (var cb in AnimationUtility.GetCurveBindings(oClip))
            {
                var cbSafe = cb;
                AnimationCurve curve = AnimationUtility.GetEditorCurve(oClip, cb);
                bool includeCurve = editFunction(ref cbSafe, ref curve);
                if (includeCurve)
                {
                    ecbs.Add(cbSafe);
                    acs.Add(curve);
                }
            }
            var existingClip = string.IsNullOrEmpty(savePath) ? oClip : AssetDatabase.LoadAssetAtPath<AnimationClip>(savePath);

            var outputClip = existingClip != null ? existingClip : new AnimationClip();
            outputClip.ClearCurves();
            AnimationUtility.SetEditorCurves(outputClip, ecbs.ToArray(), acs.ToArray());
            EditorUtility.SetDirty(outputClip);

            if (existingClip == null)
            {
                AssetDatabase.CreateAsset(outputClip, savePath); //overwrites existing
            }

            AssetDatabase.Refresh();
        }
#endif
        #endregion
    }

    public class XUUInlineSingleton<T> where T : MonoBehaviour
    {
        public XUUInlineSingleton(bool createIfMissing = true)
        {
            this._createIfMissing = createIfMissing;
        }

        bool _allowFindObjectOfType = true;
        bool _createIfMissing = true;
        T _instanceCached;
        public T GetInstance()
        {
            //#1 look for an existing
            if (_instanceCached == null && _allowFindObjectOfType)
            {
                //Debug.LogError("Finding Object of type " + typeof(T));
                _instanceCached = GameObject.FindObjectOfType<T>();
                _allowFindObjectOfType = _instanceCached != null; //allow to search again, if an object was found
            }

            //#2 create from scratch (if allowed)
            if (_instanceCached == null && _createIfMissing)
            {
                _instanceCached = new GameObject($"{typeof(T)}.Instance[from {nameof(XUUtil)}.{nameof(GetInstance)}()]").AddComponent<T>();
                _allowFindObjectOfType = true; //maybe not necessary, but allow to look again
            }

            //#3 load from resources
            //TODO

            //if (instanceVariable == null && okToUseFindObjectOfType)
            //{
            //    Debug.LogError($"failed to get singleton instance of type '{typeof(T).FullName}'. Please address, as this may create performance issues");
            //}

            return _instanceCached;
        }
    }
}