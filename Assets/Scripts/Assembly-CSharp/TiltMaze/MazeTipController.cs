using DG.Tweening;
using TMPro;
using UnityEngine;

namespace TiltMaze
{
	public sealed class MazeTipController : MonoBehaviour
	{
		[Header("Tip Objects")]
		[SerializeField]
		private RectTransform rotateTip;

		[SerializeField]
		private TMP_Text rotateText;

		[SerializeField]
		private TMPTextShatter rotateShatter;

		[SerializeField]
		private RectTransform flipTip;

		[SerializeField]
		private TMP_Text flipText;

		[SerializeField]
		private TMPTextShatter flipShatter;

		[Header("Fly In")]
		[SerializeField]
		[Min(0f)]
		private float horizontalOffset = 1000f;

		[SerializeField]
		[Min(0.05f)]
		private float flyDuration = 0.38f;

		[SerializeField]
		private Ease flyEase = Ease.OutBack;

		private Vector2 rotateTarget;

		private Vector2 flipTarget;

		private bool pressedA;

		private bool pressedD;

		private bool rotateFinished;

		private bool flipFinished;

		private int shownLevel = -1;

		private void Awake()
		{
			rotateTarget = ((rotateTip != null) ? rotateTip.anchoredPosition : Vector2.zero);
			flipTarget = ((flipTip != null) ? flipTip.anchoredPosition : Vector2.zero);
			HideImmediately(rotateTip);
			HideImmediately(flipTip);
		}

		public void ShowForLevel(int levelIndex, Color color)
		{
			if (shownLevel == levelIndex)
			{
				return;
			}
			shownLevel = levelIndex;
			if (levelIndex == 0 && !rotateFinished)
			{
				if (rotateText != null)
				{
					rotateText.color = color;
				}
				FlyIn(rotateTip, rotateTarget, 0f - horizontalOffset);
			}
			else if (levelIndex == 1 && !flipFinished)
			{
				if (flipText != null)
				{
					flipText.color = color;
				}
				FlyIn(flipTip, flipTarget, horizontalOffset);
			}
		}

		public void NotifyRotationInput(int direction)
		{
			if (shownLevel == 0 && !rotateFinished)
			{
				if (direction > 0)
				{
					pressedA = true;
				}
				if (direction < 0)
				{
					pressedD = true;
				}
				if (pressedA && pressedD)
				{
					rotateFinished = true;
					rotateShatter?.Shatter();
				}
			}
		}

		public void NotifyFlipInput()
		{
			if (shownLevel == 1 && !flipFinished)
			{
				flipFinished = true;
				flipShatter?.Shatter();
			}
		}

		private void FlyIn(RectTransform tip, Vector2 target, float offset)
		{
			if (!(tip == null))
			{
				tip.gameObject.SetActive(value: true);
				tip.DOKill();
				CanvasGroup component = tip.GetComponent<CanvasGroup>();
				tip.anchoredPosition = target + new Vector2(offset, 0f);
				if (component != null)
				{
					component.DOKill();
					component.alpha = 0f;
					component.DOFade(1f, flyDuration).SetEase(Ease.OutQuad).SetUpdate(isIndependentUpdate: true);
				}
				tip.DOAnchorPos(target, flyDuration).SetEase(flyEase).SetUpdate(isIndependentUpdate: true);
			}
		}

		private static void HideImmediately(RectTransform tip)
		{
			if (!(tip == null))
			{
				tip.gameObject.SetActive(value: false);
			}
		}
	}
}
