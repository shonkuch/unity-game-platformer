using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerUI : MonoBehaviour
{
    [SerializeField] private GameObject deadScreen;
    [SerializeField] private GameObject aliveScreen;
    [SerializeField] private List<GameObject> hearts;
    public void OnRestart()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name); 
    }

    public void OnDeath()
    {
        deadScreen.SetActive(true);
        aliveScreen.SetActive(false);
    }

    public void OnHit(int currentHP)
    {
        for (int i = 0; i < 3 - currentHP; i++)
        {
            hearts[i].gameObject.SetActive(false);
        }
    }
}
