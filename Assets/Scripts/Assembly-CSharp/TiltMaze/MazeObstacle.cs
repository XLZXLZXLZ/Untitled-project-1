using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace TiltMaze
{
	public sealed class MazeObstacle : MonoBehaviour
	{
		[SerializeField]
		private Transform physicsPart;

		[SerializeField]
		private Transform visualPart;

		[SerializeField]
		private Collider2D solidCollider;

		[SerializeField]
		private SpriteRenderer visual;

		public void Attach(Transform levelRoot, Transform physicsParent, Transform visualParent)
		{
			Matrix4x4 matrix = levelRoot.worldToLocalMatrix * physicsPart.localToWorldMatrix;
			Matrix4x4 matrix2 = levelRoot.worldToLocalMatrix * visualPart.localToWorldMatrix;
			physicsPart.SetParent(physicsParent, worldPositionStays: false);
			ApplyMatrix(physicsPart, matrix);
			visualPart.SetParent(visualParent, worldPositionStays: false);
			ApplyMatrix(visualPart, matrix2);
		}

		private static void ApplyMatrix(Transform target, Matrix4x4 matrix)
		{
			target.localPosition = matrix.GetColumn(3);
			target.localRotation = matrix.rotation;
			target.localScale = matrix.lossyScale;
		}

		public void Apply(bool active, Color foreground, float inactiveAlpha, List<Collider2D> activeColliders, float colorDuration)
		{
			solidCollider.enabled = active;
			if (active)
			{
				activeColliders.Add(solidCollider);
			}
			Color color = foreground;
			color.a = (active ? 1f : inactiveAlpha);
			visual.DOKill();
			if (colorDuration > 0f)
			{
				visual.DOColor(color, colorDuration).SetEase(Ease.InOutSine);
			}
			else
			{
				visual.color = color;
			}
		}

		public void DisposeParts()
		{
			if (physicsPart != null)
			{
				physicsPart.gameObject.SetActive(value: false);
				Object.Destroy(physicsPart.gameObject);
			}
			if (visualPart != null)
			{
				visualPart.gameObject.SetActive(value: false);
				Object.Destroy(visualPart.gameObject);
			}
		}
	}
}
