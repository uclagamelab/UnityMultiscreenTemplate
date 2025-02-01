/*
Just a struct that stores position and rotation, but also implement many of 
the same features of a UnityTransform object (e.g. getting the forward Vector, InverseTransformPoint etc...)
 */
namespace XUUtils
{

    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
#if UNITY_EDITOR
    using UnityEditor;
#endif


    [System.Serializable]
    public struct XUTransform
    {
        public Vector3 position;// = Vector3.zero;
        public Quaternion rotation;// = Quaternion.identity;
        public Vector3 eulerAngles
        {
            get { return rotation.eulerAngles; }
            set { rotation.eulerAngles = value; }
        }
        public XUTransform transform => this; //silly, but for compatibility with Unity Transform
        public bool valid => !(rotation.x == 0 && rotation.y == 0 && rotation.z == 0 && rotation.w == 0);

        public static XUTransform Identity => new XUTransform(Vector3.zero, Quaternion.identity);


        public XUTransform(Transform t)
        {
            if (t == null) //Set to 'default' / invalid values
            {
                this.position = Vector3.zero;
                this.rotation = new Quaternion(0, 0, 0, 0);
            }
            else
            {
                this.position = t.position;
                this.rotation = t.rotation;
            }
        }

        public XUTransform(Vector3 position, Vector3 eulerAngles) : this(position, Quaternion.Euler(eulerAngles))
        {
        }

        /// <summary>
        /// Transform a SavedTransfrom from world space to local space
        /// (what it's position and rotation would be if it were childed to this transform)
        /// </summary>
        public XUTransform InverseTransformTransform(XUTransform worldTransform) => InverseTransformTransform(worldTransform, Vector3.one);
        public XUTransform InverseTransformTransform(XUTransform worldTransform, Vector3 scale)
        {
            XUTransform ret = new XUTransform();
            ret.position = this.InverseTransformPoint(worldTransform.position, scale);
            ret.rotation = this.InverseTransformRotation(worldTransform.rotation);
            return ret;
        }

        public XUTransform InverseTransformTransform(Vector3 position, Quaternion rotation)
        {
            return this.InverseTransformTransform(new XUTransform(position, rotation));
        }

        /// <summary>
        /// Transform a SavedTransfrom in local space to world space
        /// (what it's position and rotation would be if it were unparented to this transform)
        /// </summary>
        public XUTransform TransformTransform(XUTransform localTransform) => TransformTransform(localTransform, Vector3.one);

        public XUTransform TransformTransform(XUTransform localTransform, Vector3 scale)
        {
            XUTransform ret = new XUTransform();
            ret.position = this.TransformPoint(Vector3.Scale(localTransform.position, scale));
            ret.rotation = this.TransformRotation(localTransform.rotation);
            return ret;
        }

        public Quaternion InverseTransformRotation(Quaternion worldRotation)
        {
            return Quaternion.Inverse(this.rotation) * worldRotation;
        }

        public Vector3 InverseTransformPoint(Vector3 worldPoint) => InverseTransformPoint(worldPoint, Vector3.one);
        public Vector3 InverseTransformPoint(Vector3 worldPoint, Vector3 scale)
        {
            Vector3 scaleRecip = scale;
            for (int i = 0; i < 3; i++) scaleRecip[i] = scaleRecip[i] == 0 ? 0 : 1f / scaleRecip[i];

            Vector3 ret = Quaternion.Inverse(rotation) * (worldPoint - this.position);

            //NOTE: Pretty sure scale should happen *after* the rotation
            ret = Vector3.Scale(ret, scaleRecip);

            return ret;
        }

        /// <summary>
        /// Convert a direction in world space to local space.
        /// </summary>
        /// <param name="worldDirection"></param>
        /// <returns></returns>
        public Vector3 InverseTransformDirection(Vector3 worldDirection)
        {
            return Quaternion.Inverse(rotation) * worldDirection;
        }

        public Vector3 TransformPoint(Vector3 localPoint)
        {

            Vector3 ret = (rotation * localPoint) + this.position;
            return ret;
        }

        public XUTransform inversed
        {
            get
            {
                var ret = this;
                var rotInv = Quaternion.Inverse(ret.rotation);
                ret.position = rotInv * -ret.position;
                ret.rotation = rotInv;
                return ret;
            }
        }

        public Quaternion TransformRotation(Quaternion localRotation)
        {
            return this.rotation * localRotation;
        }

        /// <summary>
        /// Convert a direction in local space to world.
        /// </summary>
        /// <param name="worldDirection"></param>
        /// <returns></returns>
        public Vector3 TransformDirection(Vector3 worldDirection)
        {
            return rotation * worldDirection;
        }


        public static XUTransform Lerp(XUTransform a, XUTransform b, float t)
        {
            return new XUTransform(
                Vector3.Lerp(a.position, b.position, t),
                Quaternion.Slerp(a.rotation, b.rotation, t)
            );
        }

        public XUTransform(XUTransform st) : this(st.position, st.rotation)
        {

        }

        public XUTransform(Vector3 position, Quaternion rotation)
        {
            this.position = position;
            this.rotation = rotation;
        }

        public XUTransform(Vector3 position) : this(position, Quaternion.identity)
        {
        }


        public void applyToAbsoluteValuesOf(Transform t)
        {
            t.position = this.position;
            t.rotation = this.rotation;
        }

        public void applyToLocalValuesOf(Transform t)
        {
            t.localPosition = this.position;
            t.localRotation = this.rotation;
        }

        public void applyToRigidbody(Rigidbody r)
        {
            r.position = this.position;
            r.rotation = this.rotation;
        }

        public void setValuesFrom(Transform t)
        {
            this.position = t.position;
            this.rotation = t.rotation;
        }


        public XUTransform localToGlobal(Transform parent)
        {
            return new XUTransform(parent.TransformPoint(this.position), parent.rotation * this.rotation);
        }

        public static XUTransform FromWorldValues(Transform t)
        {
            return new XUTransform(t.position, t.rotation);
        }

        public static XUTransform FromLocalValues(Transform t)
        {
            return new XUTransform(t.localPosition, t.localRotation);
        }

        public Vector3 right
        {
            get { return this.rotation * Vector3.right; }
        }

        public Vector3 up
        {
            get { return this.rotation * Vector3.up; }
            set { this.rotation = RotationFromUpVector(value); }
        }
        public static Quaternion RotationFromUpVector(Vector3 up)
        {
            Vector3 right = Vector3.Cross(up, Vector3.forward);
            Vector3 forward = Vector3.Cross(right, up);
            return Quaternion.LookRotation(forward, up);
        }
        public Vector3 forward
        {
            get { return this.rotation * Vector3.forward; }
            set { this.rotation = Quaternion.LookRotation(value); }
        }

        public Matrix4x4 trsMatrix => Matrix4x4.TRS(this.position, this.rotation, Vector3.one);



        public static implicit operator XUTransform(Transform t)  // implicit Transform to SavedTransform conversion operator
        {
            return new XUTransform(t);
        }

        //local position/rotation??

        //need scale???

        public override string ToString()
        {
            return $"({position.x.ToString(".000")}, {position.y.ToString(".000")}, {position.z.ToString(".000")}), {rotation.ToString()}";
        }

        public static bool operator ==(XUTransform t1, XUTransform t2)
        {
            return t1.position == t2.position && t1.rotation == t2.rotation;
        }

        public static bool operator !=(XUTransform t1, XUTransform t2)
        {
            return !(t1 == t2);
        }

        public static XUTransform operator -(XUTransform a, XUTransform b)
        {
            return new XUTransform(a.position - b.position, a.rotation * Quaternion.Inverse(b.rotation));
        }
    }

    public static class TCTransformExtensions
    {
        /// <summary>
        /// Transform a SavedTransfrom in local space to world space
        /// (what it's position and rotation would be if it were unparented to this transform)
        /// </summary>
        public static XUTransform TransformTransform(this Transform realTransform, XUTransform localTransform, bool applyScale = true)
        {
            XUTransform st = realTransform;
            //add in local scale
            if (applyScale) localTransform.position = Vector3.Scale(localTransform.position, realTransform.lossyScale);
            return st.TransformTransform(localTransform);
        }

        /// <summary>
        /// Transform a SavedTransfrom from world space to local space
        /// (what it's position and rotation would be if it were childed to this transform)
        /// </summary>
        public static XUTransform InverseTransformTransform(this Transform realTransform, XUTransform worldTransform)
        {
            XUTransform st = realTransform;
            //account for scale
            var ret = st.InverseTransformTransform(worldTransform);
            ret.position = new Vector3(
                ret.position.x / realTransform.lossyScale.x,
                ret.position.y / realTransform.lossyScale.y,
                ret.position.z / realTransform.lossyScale.z);
            return ret;
        }
        public static XUTransform InverseTransformTransform(this Transform realTransform, Vector3 position, Quaternion rotation)
        {
            XUTransform st = realTransform;
            //account for scale
            position = new Vector3(position.x / realTransform.lossyScale.x, position.y / realTransform.lossyScale.y, position.z / realTransform.lossyScale.z);
            return st.InverseTransformTransform(position, rotation);
        }


        public static void SetLocalTransform(this Transform thiss, XUTransform st)
        {
            thiss.localPosition = st.position;
            thiss.localRotation = st.rotation;
        }

        public static void SetLocalFromLocal(this Transform thiss, Transform st)
        {
            thiss.localPosition = st.localPosition;
            thiss.localRotation = st.localRotation;
        }

        public static void SetLocalFromWorld(this Transform thiss, Transform st)
        {
            thiss.localPosition = st.position;
            thiss.localRotation = st.rotation;
        }

        public static void SetWorldFromLocal(this Transform thiss, Transform st)
        {
            thiss.position = st.localPosition;
            thiss.localRotation = st.localRotation;
        }

        public static XUTransform LocalValues(this Transform thiss)
        {
            return new XUTransform(thiss.localPosition, thiss.localRotation);
        }
    }

#if UNITY_EDITOR
    [CustomPropertyDrawer(typeof(XUTransform))]
    public class SavedTransformDrawer : PropertyDrawer
    {
        Transform _sampleTransform;
        bool _showSampleFoldout = false;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (property.isExpanded)
            {
                if (_showSampleFoldout)
                {
                    return 6 * (EditorGUIUtility.singleLineHeight + 5);
                }
                else
                {
                    return 4 * (EditorGUIUtility.singleLineHeight + 5);
                }
            }
            else
            {
                return EditorGUIUtility.singleLineHeight;
            }
        }

        // Draw the property inside the given rect
        public override void OnGUI(Rect container, SerializedProperty property, GUIContent label)
        {
            // Using BeginProperty / EndProperty on the parent property means that
            // prefab override logic works on the entire property.
            container.height = property.isExpanded ? 95 : EditorGUIUtility.singleLineHeight;
            EditorGUI.BeginProperty(container, label, property);


            EditorGUIUtility.labelWidth = 55f;
            // Rect contentPosition = EditorGUI.PrefixLabel(position, label);
            // Draw label
            //position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);
            // int newIndex = EditorGUI.Popup(position, 0, property.displayName);

            var indent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;
            Rect contentPosition = container;

            Rect foldoutRect = contentPosition;
            foldoutRect.height = EditorGUIUtility.singleLineHeight;
            property.isExpanded = EditorGUI.Foldout(foldoutRect, property.isExpanded, property.displayName);
            if (property.isExpanded)
            {
                SerializedProperty posProp = property.FindPropertyRelative("position");
                SerializedProperty rotProp = property.FindPropertyRelative("rotation");


                // Calculate rects
                var amountRect = new Rect(contentPosition.x, container.y + EditorGUIUtility.singleLineHeight, container.width - 40, EditorGUIUtility.singleLineHeight);
                var currentRect = new Rect(contentPosition.x, container.y + 2 * EditorGUIUtility.singleLineHeight + 5, container.width - 40, EditorGUIUtility.singleLineHeight);
                //var nameRect = new Rect(position.x + 90, position.y, position.width - 90, position.height);

                EditorGUIUtility.labelWidth = 35f;
                // Draw fields - passs GUIContent.none to each so they are drawn without labels
                EditorGUI.PropertyField(amountRect, posProp, new GUIContent("Pos:"));

                EditorGUI.PropertyField(currentRect, rotProp, new GUIContent("Rot:"));
                EditorGUIUtility.labelWidth = 55f;
                ///////
                ///
                int sampleSEctionIndent = 25;
                currentRect.x = container.x;
                currentRect.x += sampleSEctionIndent;
                currentRect.y += 20;
                Rect sampleFoldoutRect = currentRect;
                sampleFoldoutRect.height = EditorGUIUtility.singleLineHeight;
                _showSampleFoldout = EditorGUI.Foldout(sampleFoldoutRect, _showSampleFoldout, "Copy Other");
                if (_showSampleFoldout)
                {
                    //////
                    currentRect.y += EditorGUIUtility.singleLineHeight;


                    currentRect.width = container.width - sampleSEctionIndent;
                    currentRect.height = EditorGUIUtility.singleLineHeight;
                    _sampleTransform = EditorGUI.ObjectField(currentRect, "target", _sampleTransform, typeof(Transform), true) as Transform;

                    int gap = 2;
                    float sampleButtonWidth = currentRect.width / 4 - gap;
                    currentRect.x += 0;// sampleButtonWidth / 2;
                    currentRect.y += EditorGUIUtility.singleLineHeight + gap;
                    currentRect.height = EditorGUIUtility.singleLineHeight;

                    currentRect.width = sampleButtonWidth;
                    if (GUI.Button(currentRect, "Copy Local"))
                    {
                        if (_sampleTransform != null)
                        {
                            posProp.vector3Value = _sampleTransform.localPosition;
                            rotProp.quaternionValue = _sampleTransform.localRotation;
                        }
                    }

                    currentRect.x += sampleButtonWidth + gap;
                    if (GUI.Button(currentRect, "Copy World"))
                    {
                        if (_sampleTransform != null)
                        {
                            posProp.vector3Value = _sampleTransform.position;
                            rotProp.quaternionValue = _sampleTransform.rotation;
                        }
                    }

                    currentRect.x += sampleButtonWidth + gap;
                    if (GUI.Button(currentRect, "Paste Local"))
                    {
                        if (_sampleTransform != null)
                        {
                            _sampleTransform.localPosition = posProp.vector3Value;
                            _sampleTransform.localRotation = rotProp.quaternionValue;
                        }
                    }

                    currentRect.x += sampleButtonWidth + gap;
                    if (GUI.Button(currentRect, "Paste World"))
                    {
                        if (_sampleTransform != null)
                        {
                            _sampleTransform.position = posProp.vector3Value;
                            _sampleTransform.rotation = rotProp.quaternionValue;
                        }
                    }
                }


                handleRightClick(container, property);// posProp, rotProp);
                                                      //EditorGUI.PropertyField(unitRect, property.FindPropertyRelative("unit"), GUIContent.none);
                                                      //EditorGUI.PropertyField(nameRect, property.FindPropertyRelative("name"), GUIContent.none);
            }
            // Don't make child fields be indented



            // Set indent back to what it was
            EditorGUI.indentLevel = indent;

            EditorGUI.EndProperty();
        }

        void handleRightClick(Rect position, SerializedProperty property)//SerializedProperty posProp, SerializedProperty rotProp)
        {
            Event e = Event.current;
            if (e.type == EventType.MouseDown && e.button == 1 && position.Contains(e.mousePosition))
            {
                SerializedProperty posProp = property.FindPropertyRelative("position");
                SerializedProperty rotProp = property.FindPropertyRelative("rotation");
                GenericMenu context = new GenericMenu();

                context.AddItem(new GUIContent("Reset To Default"), false, () =>
                {

                    posProp.vector3Value = Vector3.zero;
                    rotProp.quaternionValue = Quaternion.identity;
                    property.serializedObject.ApplyModifiedProperties();

                });
                context.ShowAsContext();
            }
        }
    }
#endif
}