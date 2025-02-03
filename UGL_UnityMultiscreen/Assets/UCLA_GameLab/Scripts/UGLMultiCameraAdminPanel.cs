using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class UGLMultiScreenAdminPanel : MonoBehaviour
{
    public Canvas canvas;
    static List<UGLMultiScreenAdminPanel> all;

    static bool _AdminPanelsOpen = false;

    [SerializeField] TMPro.TMP_Dropdown _screenGameViewDropdown;
    [SerializeField] TMPro.TextMeshProUGUI _screenNumberText;
    [SerializeField] TMPro.TextMeshProUGUI _arrangementLocText;
    UGLSubCamera _camera;
    UGLSubCamera camera
    {
        get
        {
            if (_camera == null) _camera = GetComponentInParent<UGLSubCamera>();
            return _camera;
        }
    }
    int displayNumber => UGLMultiScreen.Current.inSimulationMode ? (camera.outputDisplayNumber) : camera.camera.targetDisplay;
    public static bool AdminPanelsOpen
    {
        get => _AdminPanelsOpen;
        set
        {
            _AdminPanelsOpen = value;

            if (all == null)
            {
                all = new List<UGLMultiScreenAdminPanel>();
                foreach (var ro in SceneManager.GetActiveScene().GetRootGameObjects())
                {
                    all.AddRange(ro.GetComponentsInChildren<UGLMultiScreenAdminPanel>());
                }
            }

            foreach (UGLMultiScreenAdminPanel panel in all)
            {
                panel.canvas.gameObject.SetActive(_AdminPanelsOpen);
            }
        }
    }
    static bool ignoreDropdownChanges = false;

    private void Start()
    {
        
        _screenGameViewDropdown.onValueChanged.AddListener(OnDropDownChange);
        ignoreDropdownChanges = true;
        Refresh();
        ignoreDropdownChanges = false;
    }

    private void OnDropDownChange(int gameView)
    {
        if (ignoreDropdownChanges) return;

        var desiredGameView = UGLMultiScreen.Current.GetCameraByNumber(gameView);
        var desiredViewCurrentOutput = desiredGameView.outputDisplayNumber;
        var currentCamScreenNumber = _camera.outputDisplayNumber;
        _camera.SetOutputDisplay(desiredViewCurrentOutput);
        desiredGameView.SetOutputDisplay(currentCamScreenNumber);
        RefreshAll();
    }

    private void RefreshAll()
    {
        ignoreDropdownChanges = true;
        foreach (var panel in GameObject.FindObjectsByType<UGLMultiScreenAdminPanel>(FindObjectsSortMode.None))
        {
            panel.Refresh();
        }
        ignoreDropdownChanges = false;

        if (UGLMultiScreen.Current.inSimulationMode)
        {
            UGLMultiScreen.Current.RefreshCameraSettings();
        }
    }

    [ContextMenu("Force Refresh")]
    void Refresh()
    {
        var arrangeLoc = camera.arrangementLocation;
        _screenNumberText.text = $"Display {displayNumber+1}";
        _arrangementLocText.text = $"<size=16>arrangement Location</size>\n({arrangeLoc.x},{arrangeLoc.y})";
        _screenGameViewDropdown.value = this.camera.cameraNumber;
    }
}
