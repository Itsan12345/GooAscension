using UnityEngine;

public class LevelEntrance : MonoBehaviour
{
   [SerializeField] private string nextLevelName;
   
   private void OnTriggerEnter2D(Collider2D collision)
   {
         if (collision.GetComponentInParent<PlayerMovement>() != null)
         {
              if (SceneController.instance != null)
              {
                   SceneController.instance.ChangeLevelTo(nextLevelName);
              }
              else
              {
                   Debug.LogError("SceneController instance is null! Make sure there's a SceneController in the scene.");
              }
         }
   }
}
