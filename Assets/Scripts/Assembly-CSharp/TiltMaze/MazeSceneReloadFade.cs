using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TiltMaze
{
	[RequireComponent(typeof(CanvasGroup))]
	public sealed class MazeSceneReloadFade : MonoBehaviour
	{
		[SerializeField]
		private CanvasGroup group;

		private bool running;

		private void Awake()
		{
			if (group == null)
			{
				group = GetComponent<CanvasGroup>();
			}
			group.alpha = 0f;
			group.interactable = false;
			group.blocksRaycasts = false;
		}

		public void ReloadCurrentScene(float fadeInDuration, float fadeOutDuration)
		{
			if (!running)
			{
				running = true;
				Object.DontDestroyOnLoad(base.gameObject);
				StartCoroutine(ReloadRoutine(fadeInDuration, fadeOutDuration));
			}
		}

		private IEnumerator ReloadRoutine(float fadeInDuration, float fadeOutDuration)
		{
			group.blocksRaycasts = true;
			yield return group.DOFade(1f, fadeInDuration).SetEase(Ease.InOutSine).SetUpdate(isIndependentUpdate: true)
				.WaitForCompletion();
			AsyncOperation loading = SceneManager.LoadSceneAsync(SceneManager.GetActiveScene().name);
			while (!loading.isDone)
			{
				yield return null;
			}
			group.alpha = 1f;
			yield return group.DOFade(0f, fadeOutDuration).SetEase(Ease.InOutSine).SetUpdate(isIndependentUpdate: true)
				.WaitForCompletion();
			Object.Destroy(base.gameObject);
		}
	}
}
