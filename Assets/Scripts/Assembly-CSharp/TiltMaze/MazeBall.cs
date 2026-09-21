using System;
using DG.Tweening;
using UnityEngine;

namespace TiltMaze
{
	[RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D))]
	public sealed class MazeBall : MonoBehaviour
	{
		[SerializeField]
		private float impactThreshold = 5.5f;

		[SerializeField]
		[Range(0f, 1.5f)]
		private float bounceBoost = 0.55f;

		[SerializeField]
		private SpriteRenderer visual;

		[SerializeField]
		private TrailRenderer trail;

		private Rigidbody2D body;

		private Vector3 baseScale;

		private float baseGravityScale;

		private Vector2 velocityBeforePhysicsStep;

		public Rigidbody2D Body => body;

		public CircleCollider2D Circle { get; private set; }

		public SpriteRenderer Visual => visual;

		public TrailRenderer Trail => trail;

		public event Action<float> HardImpact;

		public event Action<Vector2, Vector2, float> WallImpact;

		public void SetPhysicsMaterial(PhysicsMaterial2D material)
		{
			if (Circle != null)
			{
				Circle.sharedMaterial = material;
			}
		}

		private void Awake()
		{
			body = GetComponent<Rigidbody2D>();
			Circle = GetComponent<CircleCollider2D>();
			baseScale = base.transform.localScale;
			baseGravityScale = body.gravityScale;
		}

		public void SetGravityMultiplier(float multiplier)
		{
			body.gravityScale = baseGravityScale * Mathf.Clamp(multiplier, 0.1f, 2f);
		}

		public void ResetAt(Vector2 worldPosition)
		{
			base.transform.DOKill();
			base.transform.position = worldPosition;
			base.transform.localScale = baseScale;
			body.velocity = Vector2.zero;
			body.angularVelocity = 0f;
			if (trail != null)
			{
				trail.Clear();
			}
		}

		public void SetTrailColor(Color color)
		{
			if (!(trail == null))
			{
				Gradient gradient = new Gradient();
				gradient.SetKeys(new GradientColorKey[2]
				{
					new GradientColorKey(color, 0f),
					new GradientColorKey(color, 1f)
				}, new GradientAlphaKey[2]
				{
					new GradientAlphaKey(0.42f, 0f),
					new GradientAlphaKey(0f, 1f)
				});
				trail.colorGradient = gradient;
			}
		}

		private void FixedUpdate()
		{
			velocityBeforePhysicsStep = body.velocity;
		}

		private void OnCollisionEnter2D(Collision2D collision)
		{
			float magnitude = collision.relativeVelocity.magnitude;
			if (collision.contactCount > 0)
			{
				ContactPoint2D contact = collision.GetContact(0);
				Vector2 normal = contact.normal;
				Vector2 arg = ((velocityBeforePhysicsStep.sqrMagnitude > 0.001f) ? (-velocityBeforePhysicsStep.normalized) : normal);
				WallImpact?.Invoke(contact.point, arg, magnitude);
				float num = Mathf.Max(0f, Vector2.Dot(-body.velocity, normal));
				if (num > 0.25f)
				{
					body.velocity += normal * (num * bounceBoost);
				}
			}
			if (!(magnitude < impactThreshold))
			{
				base.transform.DOKill();
				base.transform.localScale = baseScale;
				base.transform.DOScale(baseScale * 1.16f, 0.07f).SetLoops(2, LoopType.Yoyo).SetEase(Ease.OutQuad);
				HardImpact?.Invoke(magnitude);
			}
		}
	}
}
