using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;

[ExecuteInEditMode]
public class LoadNextScene : MonoBehaviour
{
    void OnEnable()
    {
        Debug.Log("Loading next scene");
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1, LoadSceneMode.Single);
    }
}
