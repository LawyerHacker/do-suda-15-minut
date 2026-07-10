using UnityEngine;

namespace CourtRunner15
{
    /// <summary>
    /// Точка входа: гарантирует создание игры в сцене. Дополнительно игра
    /// стартует в любой сцене через RuntimeInitializeOnLoadMethod — даже
    /// пустая сцена по кнопке Play превращается в раннер.
    /// </summary>
    public class CourtRunnerBootstrap : MonoBehaviour
    {
        private void Awake()
        {
            GameManager.EnsureExists();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoBoot()
        {
            GameManager.EnsureExists();
        }
    }
}
