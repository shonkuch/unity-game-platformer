using UnityEngine;
using UnityEngine.SceneManagement;

public class Portal : MonoBehaviour
{
    [SerializeField] private string sceneName;
    
    private void OnTriggerStay2D(Collider2D other)
    {
        var target = other.GetComponentInParent<PlayerController>();
        if (target == null) 
            return;

        SceneManager.LoadScene(sceneName); 
    }
}
