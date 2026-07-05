using UnityEngine;

public class Rotate : MonoBehaviour
{

  private void FixedUpdate()
  {
    transform.Rotate(0f, 0.2f, 0f);
  }
    
}
