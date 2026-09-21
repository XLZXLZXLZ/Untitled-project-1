using System.Collections.Generic;
using UnityEngine;

namespace TiltMaze
{
	public sealed class MazeLevelLayout : MonoBehaviour
	{
		[SerializeField]
		private Transform spawnPoint;

		[SerializeField]
		private Transform goalPoint;

		[SerializeField]
		private Transform frontZone;

		[SerializeField]
		private Transform backZone;

		[SerializeField]
		private Transform collectibleZone;

		private MazeObstacle[] frontObstacles;

		private MazeObstacle[] backObstacles;

		public Vector2 SpawnPoint => spawnPoint.localPosition;

		public Vector2 GoalPoint => GetCollectiblePoint(0);

		public int CollectibleCount
		{
			get
			{
				ResolveHierarchy();
				if (!(collectibleZone != null) || collectibleZone.childCount <= 0)
				{
					if (!(goalPoint == null))
					{
						return 1;
					}
					return 0;
				}
				return collectibleZone.childCount;
			}
		}

		public Vector2 GetCollectiblePoint(int index)
		{
			ResolveHierarchy();
			if (collectibleZone != null && collectibleZone.childCount > 0)
			{
				return base.transform.InverseTransformPoint(collectibleZone.GetChild(Mathf.Clamp(index, 0, collectibleZone.childCount - 1)).position);
			}
			return goalPoint.localPosition;
		}

		public void Attach(Transform physicsParent, Transform visualParent)
		{
			ResolveHierarchy();
			AttachGroup(frontObstacles, physicsParent, visualParent);
			AttachGroup(backObstacles, physicsParent, visualParent);
		}

		public void ApplyFace(int activeFace, Color foreground, float inactiveAlpha, List<Collider2D> activeColliders, float colorDuration = 0f)
		{
			ResolveHierarchy();
			ApplyGroup(frontObstacles, activeFace == 0, foreground, inactiveAlpha, activeColliders, colorDuration);
			ApplyGroup(backObstacles, activeFace == 1, foreground, inactiveAlpha, activeColliders, colorDuration);
		}

		public void Dispose()
		{
			ResolveHierarchy();
			DisposeGroup(frontObstacles);
			DisposeGroup(backObstacles);
			base.gameObject.SetActive(value: false);
			Object.Destroy(base.gameObject);
		}

		private void ResolveHierarchy()
		{
			if (spawnPoint == null)
			{
				spawnPoint = base.transform.Find("Spawn Point");
			}
			if (frontZone == null)
			{
				frontZone = FindZone("Front Obstacles", "Front Zone");
			}
			if (backZone == null)
			{
				backZone = FindZone("Back Obstacles", "Back Zone");
			}
			if (collectibleZone == null)
			{
				collectibleZone = FindZone("Collectible Points", "Collectibles");
			}
			frontObstacles = ((frontZone == null) ? new MazeObstacle[0] : frontZone.GetComponentsInChildren<MazeObstacle>(includeInactive: true));
			backObstacles = ((backZone == null) ? new MazeObstacle[0] : backZone.GetComponentsInChildren<MazeObstacle>(includeInactive: true));
		}

		private Transform FindZone(string primaryName, string alternateName)
		{
			Transform transform = base.transform.Find(primaryName);
			if (!(transform != null))
			{
				return base.transform.Find(alternateName);
			}
			return transform;
		}

		private void AttachGroup(MazeObstacle[] obstacles, Transform physicsParent, Transform visualParent)
		{
			foreach (MazeObstacle mazeObstacle in obstacles)
			{
				if (mazeObstacle != null)
				{
					mazeObstacle.Attach(base.transform, physicsParent, visualParent);
				}
			}
		}

		private static void ApplyGroup(MazeObstacle[] obstacles, bool active, Color foreground, float inactiveAlpha, List<Collider2D> activeColliders, float colorDuration)
		{
			foreach (MazeObstacle mazeObstacle in obstacles)
			{
				if (mazeObstacle != null)
				{
					mazeObstacle.Apply(active, foreground, inactiveAlpha, activeColliders, colorDuration);
				}
			}
		}

		private static void DisposeGroup(MazeObstacle[] obstacles)
		{
			foreach (MazeObstacle mazeObstacle in obstacles)
			{
				if (mazeObstacle != null)
				{
					mazeObstacle.DisposeParts();
				}
			}
		}
	}
}
