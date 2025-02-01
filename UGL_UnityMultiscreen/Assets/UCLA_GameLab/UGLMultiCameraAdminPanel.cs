using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class UGLMultiScreenAdminPanel : MonoBehaviour
{
    public Canvas canvas;
    static List<UGLMultiScreenAdminPanel> all;

    static bool _AdminPanelsOpen = true;

    [SerializeField] TMPro.TMP_Dropdown _screenGameViewDropdown;
    [SerializeField] TMPro.TextMeshProUGUI _screenNumberText;
    UGLSubCamera _camera;
    UGLSubCamera camera
    {
        get
        {
            if (_camera == null) _camera = GetComponentInParent<UGLSubCamera>();
            return _camera;
        }
    }
    int screenNumer => UGLMultiScreen.I.inSimulationMode ? (camera.screenNumber) : camera.camera.targetDisplay;
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

        var desiredGameView = UGLMultiScreen.I.GetCamera(gameView);
        var desiredViewCurrentOutput = desiredGameView.screenNumber;
        var currentCamScreenNumber = _camera.screenNumber;
        _camera.SetOutputScreen(desiredViewCurrentOutput);
        desiredGameView.SetOutputScreen(currentCamScreenNumber);
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

        if (UGLMultiScreen.I.inSimulationMode)
        {
            UGLMultiScreen.I.RefreshSimulationView();
        }
    }

    [ContextMenu("Force Refresh")]
    void Refresh()
    {
        _screenNumberText.text = $"Display {screenNumer+1}";
        _screenGameViewDropdown.value = this.camera.cameraNumber;
    }
}
