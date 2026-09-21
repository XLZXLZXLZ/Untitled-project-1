using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TiltMaze
{
	public sealed class MazeEndSequence : MonoBehaviour
	{
		[Header("UI")]
		[SerializeField]
		private CanvasGroup rootGroup;

		[SerializeField]
		private TMP_Text allClearText;

		[SerializeField]
		private TMP_Text dialogueText;

		[SerializeField]
		private Button exitButton;

		[SerializeField]
		private TMP_Text exitText;

		[SerializeField]
		private Button replayButton;

		[SerializeField]
		private TMP_Text replayText;

		[SerializeField]
		private MazeSceneReloadFade sceneFade;

		[Header("Timing")]
		[SerializeField]
		[Min(0.05f)]
		private float boardFadeDuration = 0.65f;

		[SerializeField]
		[Min(0.05f)]
		private float allClearFlyDuration = 0.42f;

		[SerializeField]
		[Min(0f)]
		private float allClearHoldDuration = 0.45f;

		[SerializeField]
		[Min(0f)]
		private float shatterWaitDuration = 0.7f;

		[SerializeField]
		[Min(0.05f)]
		private float dialogueFlyDuration = 0.45f;

		[SerializeField]
		[Min(0f)]
		private float allClearFlyOffset = 900f;

		[SerializeField]
		[Min(0f)]
		private float dialogueFlyOffset = 1100f;

		[SerializeField]
		private Ease flyEase = Ease.OutCubic;

		[SerializeField]
		[Min(0.05f)]
		private float blackFadeInDuration = 0.45f;

		[SerializeField]
		[Min(0.05f)]
		private float blackFadeOutDuration = 0.55f;

		private TMPTextShatter allClearShatter;

		private TMPTextShatter dialogueShatter;

		private TMPTextShatter exitShatter;

		private TMPTextShatter replayShatter;

		private Vector2 allClearTarget;

		private Vector2 dialogueTarget;

		private Vector2 exitTarget;

		private Vector2 replayTarget;

		private bool choosing;

		private void Awake()
		{
			if (rootGroup == null)
			{
				rootGroup = GetComponent<CanvasGroup>();
			}
			allClearShatter = EnsureShatter(allClearText);
			dialogueShatter = EnsureShatter(dialogueText);
			exitShatter = EnsureShatter(exitText);
			replayShatter = EnsureShatter(replayText);
			allClearTarget = PositionOf(allClearText);
			dialogueTarget = PositionOf(dialogueText);
			exitTarget = ((exitButton != null) ? ((RectTransform)exitButton.transform).anchoredPosition : Vector2.zero);
			replayTarget = ((replayButton != null) ? ((RectTransform)replayButton.transform).anchoredPosition : Vector2.zero);
			HideItems();
			if (exitButton != null)
			{
				exitButton.onClick.AddListener(ChooseExit);
			}
			if (replayButton != null)
			{
				replayButton.onClick.AddListener(ChooseReplay);
			}
		}

		public void Play(Transform boardRoot, TrailRenderer ballTrail, ParticleSystem environmentParticles, Color textColor)
		{
			base.gameObject.SetActive(value: true);
			choosing = false;
			if (rootGroup != null)
			{
				rootGroup.alpha = 1f;
				rootGroup.interactable = false;
				rootGroup.blocksRaycasts = false;
			}
			if (ballTrail != null)
			{
				ballTrail.Clear();
			}
			if (environmentParticles != null)
			{
				environmentParticles.Stop(withChildren: true, ParticleSystemStopBehavior.StopEmitting);
			}
			ApplyTextColor(textColor);
			Sequence sequence = DOTween.Sequence().SetUpdate(isIndependentUpdate: true);
			if (boardRoot != null)
			{
				SpriteRenderer[] componentsInChildren = boardRoot.GetComponentsInChildren<SpriteRenderer>(includeInactive: true);
				foreach (SpriteRenderer target in componentsInChildren)
				{
					target.DOKill();
					sequence.Join(target.DOFade(0f, boardFadeDuration).SetEase(Ease.InOutSine));
				}
			}
			sequence.AppendCallback(ShowAllClearAtStart);
			sequence.Append(((RectTransform)allClearText.transform).DOAnchorPos(allClearTarget, allClearFlyDuration).SetEase(flyEase));
			sequence.AppendInterval(allClearHoldDuration);
			sequence.AppendCallback(() =>
			{
				allClearShatter?.Shatter();
			});
			sequence.AppendInterval(shatterWaitDuration);
			sequence.AppendCallback(ShowDialogueAtStart);
			sequence.Append(((RectTransform)dialogueText.transform).DOAnchorPos(dialogueTarget, dialogueFlyDuration).SetEase(flyEase));
			sequence.Join(((RectTransform)exitButton.transform).DOAnchorPos(exitTarget, dialogueFlyDuration).SetEase(flyEase));
			sequence.Join(((RectTransform)replayButton.transform).DOAnchorPos(replayTarget, dialogueFlyDuration).SetEase(flyEase));
			sequence.OnComplete(() =>
			{
				choosing = true;
				SetButtonsInteractive(value: true);
				if (rootGroup != null)
				{
					rootGroup.interactable = true;
					rootGroup.blocksRaycasts = true;
				}
			});
		}

		private void ChooseExit()
		{
			if (choosing)
			{
				choosing = false;
				SetButtonsInteractive(value: false);
				dialogueShatter?.Shatter();
				exitShatter?.Shatter();
				replayShatter?.Shatter();
				DOVirtual.DelayedCall(shatterWaitDuration, Application.Quit).SetUpdate(isIndependentUpdate: true);
			}
		}

		private void ChooseReplay()
		{
			if (!choosing)
			{
				return;
			}
			choosing = false;
			SetButtonsInteractive(value: false);
			dialogueShatter?.Shatter();
			exitShatter?.Shatter();
			replayShatter?.Shatter();
			DOVirtual.DelayedCall(shatterWaitDuration, () =>
			{
				if (sceneFade != null)
				{
					sceneFade.ReloadCurrentScene(blackFadeInDuration, blackFadeOutDuration);
				}
			}).SetUpdate(isIndependentUpdate: true);
		}

		private void ShowAllClearAtStart()
		{
			allClearText.gameObject.SetActive(value: true);
			allClearText.enabled = true;
			((RectTransform)allClearText.transform).anchoredPosition = allClearTarget + Vector2.up * allClearFlyOffset;
		}

		private void ShowDialogueAtStart()
		{
			dialogueText.gameObject.SetActive(value: true);
			exitButton.gameObject.SetActive(value: true);
			replayButton.gameObject.SetActive(value: true);
			((RectTransform)dialogueText.transform).anchoredPosition = dialogueTarget + Vector2.left * dialogueFlyOffset;
			((RectTransform)exitButton.transform).anchoredPosition = exitTarget + Vector2.down * dialogueFlyOffset;
			((RectTransform)replayButton.transform).anchoredPosition = replayTarget + Vector2.down * dialogueFlyOffset;
			SetButtonsInteractive(value: false);
		}

		private void HideItems()
		{
			if (allClearText != null)
			{
				allClearText.gameObject.SetActive(value: false);
			}
			if (dialogueText != null)
			{
				dialogueText.gameObject.SetActive(value: false);
			}
			if (exitButton != null)
			{
				exitButton.gameObject.SetActive(value: false);
			}
			if (replayButton != null)
			{
				replayButton.gameObject.SetActive(value: false);
			}
		}

		private void SetButtonsInteractive(bool value)
		{
			if (exitButton != null)
			{
				exitButton.interactable = value;
			}
			if (replayButton != null)
			{
				replayButton.interactable = value;
			}
		}

		private void ApplyTextColor(Color color)
		{
			if (allClearText != null)
			{
				allClearText.color = color;
			}
			if (dialogueText != null)
			{
				dialogueText.color = color;
			}
			if (exitText != null)
			{
				exitText.color = color;
			}
			if (replayText != null)
			{
				replayText.color = color;
			}
		}

		private static Vector2 PositionOf(TMP_Text text)
		{
			if (!(text == null))
			{
				return ((RectTransform)text.transform).anchoredPosition;
			}
			return Vector2.zero;
		}

		private static TMPTextShatter EnsureShatter(TMP_Text text)
		{
			if (text == null)
			{
				return null;
			}
			TMPTextShatter obj = text.GetComponent<TMPTextShatter>() ?? text.gameObject.AddComponent<TMPTextShatter>();
			obj.Configure(text);
			return obj;
		}
	}
}
