using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
#if UNITY_EDITOR
using UnityEditor;
#endif

[System.Serializable]
public struct XUDateTime //: ISerializationCallbackReceiver
{
    public bool valid => dateTime != default;
    public TimeSpan Since => (DateTime.Now - this.dateTime);
    public TimeSpan Until => (this.dateTime - DateTime.Now);

    public static XUDateTime Now => DateTime.Now;

    public DateTime dateTime
    {
        //get => parse(_dateTime);
        //set => _dateTime = value.ToString(formatString);
        get => _year == 0 ? default : new DateTime(_year, _month, _day, _hour, _minute, (int)_second, (int)((_second % 1) * 1000));
        set
        {
            _year = value.Year;
            _month = value.Month;
            _day = value.Day;
            _hour = value.Hour;
            _minute = value.Minute;
            _second = value.Second + (value.Millisecond * .001f);
        }
    }
    const string formatString = "yyyy/M/d/H:m:s:fff";

    [SerializeField] private int _year;
    [SerializeField] private int _month;
    [SerializeField] private int _day;
    [SerializeField] private int _hour;
    [SerializeField] private int _minute;
    [SerializeField] private float _second;

    //[SerializeField] private string _dateTime;
    static DateTime _epoch => new DateTime(2000, 1, 1);


    public static implicit operator DateTime(XUDateTime udt)
    {
        return (udt.dateTime);
    }

    public static implicit operator XUDateTime(DateTime dt)
    {
        return new XUDateTime() { dateTime = dt };
    }

    static DateTime parse(string s)
    {
        return parse(s, out bool ok);
    }
    static DateTime parse(string s, out bool ok)
    {
        ok = DateTime.TryParseExact(s, formatString,
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None,
                    out DateTime dateTime);
        return dateTime;
    }


#if UNITY_EDITOR
    [CustomPropertyDrawer(typeof(XUDateTime))]
    public class Drawer : PropertyDrawer
    {
        bool _haveOverwriteDate = false;
        DateTime _overWriteDate = default;
        public override void OnGUI(Rect area, SerializedProperty property, GUIContent label)
        {
            Rect fullArea = area;
            bool haveOverwriteDate = _haveOverwriteDate;
            _haveOverwriteDate = false;
            //var milliProp = property.FindPropertyRelative("_millisSinceEpoch");
            //DateTime d = milliProp.longValue == 0 ? default : STDateTime._epoch.AddMilliseconds(milliProp.longValue);
            //string curText = d.ToString("yyyy/M/d/H:m:s:fff");
            EditorGUI.BeginProperty(area, label, property);

            //var stringProp = property.FindPropertyRelative("_dateTime");


            var _year = property.FindPropertyRelative("_year");
            var _month = property.FindPropertyRelative("_month");
            var _day = property.FindPropertyRelative("_day");
            var _hour = property.FindPropertyRelative("_hour");
            var _minute = property.FindPropertyRelative("_minute");
            var _second = property.FindPropertyRelative("_second");
            DateTime current = default;
            try
            {
                current = new DateTime(
                    _year.intValue,
                    _month.intValue,
                    _day.intValue,
                    _hour.intValue,
                    _minute.intValue,
                    (int)_second.floatValue,
                    (int)((_second.floatValue % 1) * 1000)
                    );
            }
            catch (System.Exception e) { }

            if (haveOverwriteDate)
            {
                current = _overWriteDate;
            }

            //string prevText = stringProp.stringValue;
            string prevText = current.ToString(formatString);
            area = EditorGUI.PrefixLabel(area, GUIUtility.GetControlID(FocusType.Passive), label);

            EditorGUI.BeginChangeCheck();
            string nuText = EditorGUI.TextField(area, prevText);
            var parsedInput = parse(nuText, out bool ok);

            if (EditorGUI.EndChangeCheck() || haveOverwriteDate)
            {
                //stringProp.stringValue = ok ? nuText : prevText;
                if (ok)
                {
                    _year.intValue = parsedInput.Year;
                    _month.intValue = parsedInput.Month;
                    _day.intValue = parsedInput.Day;
                    _hour.intValue = parsedInput.Hour;
                    _minute.intValue = parsedInput.Minute;
                    _second.floatValue = parsedInput.Second + (.001f * parsedInput.Millisecond);
                }
            }

            handleRightClick(fullArea, property);

            EditorGUI.EndProperty();
        }



        void handleRightClick(Rect position, SerializedProperty property)//SerializedProperty posProp, SerializedProperty rotProp)
        {
            Event e = Event.current;
            if (e.type == EventType.MouseDown && e.button == 1 && position.Contains(e.mousePosition))
            {
                GenericMenu context = new GenericMenu();

                context.AddItem(new GUIContent("Set Now"), false, () =>
                {
                    _haveOverwriteDate = true;
                    _overWriteDate = DateTime.Now;
                    //var stringProp = property.FindPropertyRelative("_dateTime");
                    //stringProp.stringValue = DateTime.Now.ToString(formatString);


                });

                context.AddItem(new GUIContent("Clear"), false, () =>
                {
                    _haveOverwriteDate = true;
                    _overWriteDate = default;
                    //var stringProp = property.FindPropertyRelative("_dateTime");
                    //DateTime dd = default; 
                    //stringProp.stringValue = dd.ToString(formatString);

                });

                context.ShowAsContext();
            }
        }
    }
#endif
}