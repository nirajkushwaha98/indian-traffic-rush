using UnityEngine;

namespace ITR
{
    /// <summary>The only component saved in MainMenu.unity: builds the 3D showroom and all menu UI.</summary>
    public class MenuBootstrap : MonoBehaviour
    {
        void Awake()
        {
            AppRoot.Ensure();
            Time.timeScale = 1f;
            MissionManager.EnsureToday();
            var showroom = MenuShowroom.Create();
            MainMenuUI.Build(showroom);
            AudioManager.I?.PlayMusic(false);
        }
    }
}
