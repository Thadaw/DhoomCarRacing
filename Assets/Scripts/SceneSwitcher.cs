using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneSwitcher : MonoBehaviour
{
  [SerializeField] CanvasGroup canvasGroup;
  [SerializeField] float speed = 1f;
  private void Awake()
  {
      if (canvasGroup != null)
          canvasGroup.alpha = 1f;
  }
  private void Start()
  {
      if (canvasGroup != null)
          StartCoroutine(FadeIn());
  }
  IEnumerator FadeIn()
  {
    while (canvasGroup.alpha > 0f)
    {
        canvasGroup.alpha -= Time.deltaTime * speed;
        yield return null;
    }
  }
  IEnumerator FadeOut(string sceneName)
  {
    while (canvasGroup.alpha < 1f)
    {
        canvasGroup.alpha += Time.deltaTime * speed;
        yield return null;
    }
    SceneManager.LoadScene(sceneName);
  }

  public void SceneLoder(string sceneName)
  {
      if (canvasGroup == null)
      {
          SceneManager.LoadScene(sceneName);
          return;
      }
      StartCoroutine(FadeOut(sceneName));
      
  }
  public void QuitGame()
  {
      Application.Quit();
  }
    
}
