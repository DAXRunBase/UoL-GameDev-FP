using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;

[ExecuteInEditMode]
public class LoadNextScene : MonoBehaviour
{
    void OnEnable()
    {
        Debug.Log("Loading MainMenu scene");
        SceneManager.LoadScene("MainMenu", LoadSceneMode.Single);
    }
}
