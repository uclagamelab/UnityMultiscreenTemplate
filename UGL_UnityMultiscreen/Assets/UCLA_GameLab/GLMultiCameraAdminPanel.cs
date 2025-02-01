using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GLMultiCameraAdminPanel : MonoBehaviour
{
    public Canvas canvas;
    static List<GLMultiCameraAdminPanel> all;

    static bool _AdminPanelsOpen = true;
    public static bool AdminPanelsOpen
    {
        get => _AdminPanelsOpen;
        set
        {
            _AdminPanelsOpen = value;

            if (all == null)
            {
                all = new List<GLMultiCameraAdminPanel>();
                foreach (var ro in SceneManager.GetActiveScene().GetRootGameObjects())
                {
                    all.AddRange(ro.GetComponentsInChildren<GLMultiCameraAdminPanel>());
                }
            }

            foreach (GLMultiCameraAdminPanel panel in all)
            {
                panel.canvas.gameObject.SetActive(_AdminPanelsOpen);
            }
        }
    }
}
