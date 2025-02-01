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
    int screenNumer => UGLMultiScreen.I.inSimulationMode ? (_camera.cameraNumber) : _camera.camera.targetDisplay;
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

    private void Start()
    {
        _camera = GetComponentInParent<UGLSubCamera>();
        _screenGameViewDropdown.value = screenNumer -1;
        _screenGameViewDropdown.onValueChanged.AddListener(OnDropDownChange);
        Refresh();
    }

    private void OnDropDownChange(int gameView)
    {
        var desiredGameView = UGLMultiScreen.I.GetCamera(gameView);
        desiredGameView.SetOutputScreen(this.screenNumer);
        RefreshAll();
    }

    private void RefreshAll()
    {
        foreach (var panel in GameObject.FindObjectsByType<UGLMultiScreenAdminPanel>(FindObjectsSortMode.None))
        {
            panel.Refresh();
        }
    }

    void Refresh()
    {
        _screenNumberText.text = $"SCREEN #{screenNumer}";
    }
}
