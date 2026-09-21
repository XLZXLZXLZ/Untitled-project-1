using System;
using DG.Tweening;
using UnityEngine;

namespace TiltMaze
{
	[RequireComponent(typeof(CircleCollider2D))]
	public sealed class MazeGoalZone : MonoBehaviour
	{
		[SerializeField]
		private SpriteRenderer visual;

		[Header("Idle Beat")]
		[SerializeField]
		[Min(0.05f)]
		private float idleInterval = 0.75f;

		[SerializeField]
		[Min(0.01f)]
		private float idleTurnDuration = 0.16f;

		[SerializeField]
		private float idleTurnDegrees = 90f;

		[SerializeField]
		[Min(0.01f)]
		private float idleScaleMultiplier = 1.18f;

		[SerializeField]
		private Ease idleRotationEase = Ease.OutQuad;

		[SerializeField]
		private Ease idleScaleEase = Ease.InOutSine;

		[Header("Regular Collection")]
		[SerializeField]
		[Min(0.01f)]
		private float collectScaleMultiplier = 1.3f;

		[SerializeField]
		[Min(0.01f)]
		private float collectGrowDuration = 0.12f;

		[SerializeField]
		[Min(0.01f)]
		private float collectDisappearDuration = 0.16f;

		[SerializeField]
		private Ease collectGrowEase = Ease.OutQuad;

		[SerializeField]
		private Ease collectDisappearEase = Ease.InBack;

		[Header("Final Collection")]
		[SerializeField]
		[Min(0.01f)]
		private float finalScaleMultiplier = 1.5f;

		[SerializeField]
		[Min(0.01f)]
		private float finalGrowDuration = 0.34f;

		[SerializeField]
		[Min(0.01f)]
		private float finalDisappearDuration = 0.3f;

		[SerializeField]
		private float finalRotationDegrees = 360f;

		[SerializeField]
		[Min(0.01f)]
		private float finalRotationDuration = 0.62f;

		[SerializeField]
		private Ease finalGrowEase = Ease.OutBack;

		[SerializeField]
		private Ease finalRotationEase = Ease.InOutQuad;

		[SerializeField]
		private Ease finalDisappearEase = Ease.InBack;

		private bool armed;

		private Vector3 initialScale;

		private Tween idleRotationTween;

		private Tween idleScaleTween;

		public SpriteRenderer Visual => visual;

		public event Action<MazeGoalZone> Reached;

		private void Awake()
		{
			initialScale = visual.transform.localScale;
		}

		private void OnEnable()
		{
			BeginIdleAnimation();
		}

		private void OnDisable()
		{
			StopIdleAnimation();
		}

		public void SetArmed(bool value)
		{
			armed = value;
		}

		public void BeginIdleAnimation()
		{
			if (!(visual == null))
			{
				StopIdleAnimation();
				initialScale = visual.transform.localScale;
				float num = Mathf.Min(idleTurnDuration, idleInterval);
				float interval = Mathf.Max(0f, idleInterval - num);
				Vector3 endValue = visual.transform.localEulerAngles + Vector3.forward * idleTurnDegrees;
				idleRotationTween = DOTween.Sequence().AppendInterval(interval).Append(visual.transform.DOLocalRotate(endValue, num, RotateMode.FastBeyond360).SetEase(idleRotationEase))
					.SetLoops(-1, LoopType.Incremental);
				idleScaleTween = DOTween.Sequence().AppendInterval(interval).Append(visual.transform.DOScale(initialScale * idleScaleMultiplier, num * 0.5f).SetEase(idleScaleEase))
					.Append(visual.transform.DOScale(initialScale, num * 0.5f).SetEase(idleScaleEase))
					.SetLoops(-1, LoopType.Restart);
			}
		}

		public void PlayCollectedAnimation(bool isFinal, Action onComplete)
		{
			armed = false;
			GetComponent<CircleCollider2D>().enabled = false;
			StopIdleAnimation();
			visual.DOKill();
			Sequence sequence = DOTween.Sequence().SetUpdate(isIndependentUpdate: true);
			if (isFinal)
			{
				sequence.Append(visual.transform.DOScale(initialScale * finalScaleMultiplier, finalGrowDuration).SetEase(finalGrowEase));
				sequence.Join(visual.transform.DOLocalRotate(visual.transform.localEulerAngles + Vector3.forward * finalRotationDegrees, finalRotationDuration, RotateMode.FastBeyond360).SetEase(finalRotationEase));
				sequence.Append(visual.transform.DOScale(Vector3.zero, finalDisappearDuration).SetEase(finalDisappearEase));
				sequence.Join(visual.DOFade(0f, finalDisappearDuration));
			}
			else
			{
				sequence.Append(visual.transform.DOScale(initialScale * collectScaleMultiplier, collectGrowDuration).SetEase(collectGrowEase));
				sequence.Append(visual.transform.DOScale(Vector3.zero, collectDisappearDuration).SetEase(collectDisappearEase));
				sequence.Join(visual.DOFade(0f, collectDisappearDuration));
			}
			sequence.OnComplete(() =>
			{
				onComplete?.Invoke();
				Dispose();
			});
		}

		public void Dispose()
		{
			base.gameObject.SetActive(value: false);
			if (visual != null && visual.gameObject != base.gameObject)
			{
				visual.gameObject.SetActive(value: false);
				UnityEngine.Object.Destroy(visual.gameObject);
			}
			UnityEngine.Object.Destroy(base.gameObject);
		}

		private void StopIdleAnimation()
		{
			idleRotationTween?.Kill();
			idleScaleTween?.Kill();
			idleRotationTween = null;
			idleScaleTween = null;
			if (visual != null && initialScale != Vector3.zero)
			{
				visual.transform.localScale = initialScale;
			}
		}

		private void OnTriggerEnter2D(Collider2D other)
		{
			TryReach(other);
		}

		private void OnTriggerStay2D(Collider2D other)
		{
			TryReach(other);
		}

		private void TryReach(Collider2D other)
		{
			if (armed && !(other.GetComponentInParent<MazeBall>() == null))
			{
				armed = false;
				Reached?.Invoke(this);
			}
		}
	}
}
