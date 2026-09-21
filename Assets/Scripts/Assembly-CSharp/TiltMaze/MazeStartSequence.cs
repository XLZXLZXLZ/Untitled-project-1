using DG.Tweening;
using TMPro;
using UnityEngine;

namespace TiltMaze
{
	public sealed class MazeStartSequence : MonoBehaviour
	{
		[Header("Countdown TMP")]
		[SerializeField]
		private TMP_Text threeText;

		[SerializeField]
		private TMP_Text twoText;

		[SerializeField]
		private TMP_Text oneText;

		[SerializeField]
		private TMP_Text startText;

		[Header("Timing")]
		[SerializeField]
		[Min(0.05f)]
		private float flyDuration = 0.32f;

		[SerializeField]
		[Min(0f)]
		private float centerHoldDuration = 0.06f;

		[SerializeField]
		[Min(0f)]
		private float shatterGap = 0.22f;

		[SerializeField]
		[Min(100f)]
		private float horizontalStartOffset = 1200f;

		[SerializeField]
		[Min(100f)]
		private float verticalStartOffset = 850f;

		[Header("START Pulse")]
		[SerializeField]
		[Min(1f)]
		private float startGrowScale = 1.32f;

		[SerializeField]
		[Range(0.1f, 1f)]
		private float startShrinkScale = 0.88f;

		[SerializeField]
		[Min(0.02f)]
		private float startPulseStepDuration = 0.08f;

		private MazeGameController controller;

		private TMPTextShatter threeShatter;

		private TMPTextShatter twoShatter;

		private TMPTextShatter oneShatter;

		private TMPTextShatter startShatter;

		private bool played;

		public void Configure(MazeGameController owner, TMP_Text ignoredLegacyStartText = null)
		{
			controller = owner;
			Animator component = GetComponent<Animator>();
			if (component != null)
			{
				component.enabled = false;
			}
			threeText = ResolveText(threeText, "3");
			twoText = ResolveText(twoText, "2");
			oneText = ResolveText(oneText, "1");
			startText = ResolveText(startText, "Start!") ?? ignoredLegacyStartText;
			threeShatter = EnsureShatter(threeText);
			twoShatter = EnsureShatter(twoText);
			oneShatter = EnsureShatter(oneText);
			startShatter = EnsureShatter(startText);
			SetVisible(threeText, visible: false);
			SetVisible(twoText, visible: false);
			SetVisible(oneText, visible: false);
			SetVisible(startText, visible: false);
		}

		public void Play()
		{
			if (played)
			{
				controller?.BeginGameplayFromAnimationEvent();
				return;
			}
			played = true;
			base.transform.DOKill();
			Sequence sequence = DOTween.Sequence().SetUpdate(isIndependentUpdate: true);
			AppendFlyAndShatter(sequence, threeText, threeShatter, new Vector2(0f - horizontalStartOffset, 0f));
			AppendFlyAndShatter(sequence, twoText, twoShatter, new Vector2(horizontalStartOffset, 0f));
			AppendFlyAndShatter(sequence, oneText, oneShatter, new Vector2(0f, verticalStartOffset));
			AppendStart(sequence);
			sequence.AppendInterval(shatterGap);
			sequence.OnComplete(() =>
			{
				controller?.BeginGameplayFromAnimationEvent();
			});
		}

		public void StartGameplayEvent()
		{
			controller?.BeginGameplayFromAnimationEvent();
		}

		private void AppendFlyAndShatter(Sequence sequence, TMP_Text text, TMPTextShatter shatter, Vector2 offset)
		{
			if (!(text == null))
			{
				RectTransform rect = (RectTransform)text.transform;
				sequence.AppendCallback(() =>
				{
					rect.DOKill();
					rect.anchoredPosition = offset;
					SetVisible(text, visible: true);
				});
				sequence.Append(rect.DOAnchorPos(Vector2.zero, flyDuration).SetEase(Ease.InOutCubic));
				sequence.AppendInterval(centerHoldDuration);
				sequence.AppendCallback(() =>
				{
					shatter?.Shatter();
				});
				sequence.AppendInterval(shatterGap);
			}
		}

		private void AppendStart(Sequence sequence)
		{
			if (!(startText == null))
			{
				RectTransform rect = (RectTransform)startText.transform;
				Vector3 baseScale = rect.localScale;
				sequence.AppendCallback(() =>
				{
					rect.DOKill();
					rect.anchoredPosition = new Vector2(0f, 0f - verticalStartOffset);
					rect.localScale = baseScale;
					SetVisible(startText, visible: true);
				});
				sequence.Append(rect.DOAnchorPos(Vector2.zero, flyDuration).SetEase(Ease.InOutCubic));
				sequence.Append(rect.DOScale(baseScale * startGrowScale, startPulseStepDuration).SetEase(Ease.OutQuad));
				sequence.Append(rect.DOScale(baseScale * startShrinkScale, startPulseStepDuration).SetEase(Ease.InOutQuad));
				sequence.Append(rect.DOScale(baseScale, startPulseStepDuration).SetEase(Ease.OutBack));
				sequence.AppendCallback(() =>
				{
					startShatter?.Shatter();
				});
			}
		}

		private TMP_Text ResolveText(TMP_Text current, string childName)
		{
			if (current != null)
			{
				return current;
			}
			Transform transform = base.transform.Find(childName);
			if (!(transform == null))
			{
				return transform.GetComponent<TMP_Text>();
			}
			return null;
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

		private static void SetVisible(TMP_Text text, bool visible)
		{
			if (!(text == null))
			{
				Color color = text.color;
				color.a = 1f;
				text.color = color;
				text.enabled = visible;
			}
		}
	}
}
