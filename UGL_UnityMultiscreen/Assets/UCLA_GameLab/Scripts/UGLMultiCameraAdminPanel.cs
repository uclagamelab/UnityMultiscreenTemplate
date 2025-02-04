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
    [SerializeField] UnityEngine.UI.Button _resetCameraAssignments;
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
        _resetCameraAssignments.onClick.AddListener(BTN_ResetCameraAssignments);
    }

    private void OnDropDownChange(int gameView)
    {
        if (ignoreDropdownChanges) return;

        var desiredGameView = UGLMultiScreen.Current.GetCameraByNumber(gameView);
        var desiredViewCurrentOutput = desiredGameView.outputDisplayNumber;
        var currentCamScreenNumber = _camera.outputDisplayNumber;
        _camera.SetOutputDisplay(desiredViewCurrentOutput, true);
        desiredGameView.SetOutputDisplay(currentCamScreenNumber, true);
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

    void BTN_ResetCameraAssignments()
    {
        foreach (var cam in UGLMultiScreen.Current.Cameras)
        {
            //TODO: this should be a dedicated method.
            cam.SetOutputDisplay(cam.cameraNumber, true);
        }
        UGLMultiScreen.Current.RefreshCameraSettings(true);
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
