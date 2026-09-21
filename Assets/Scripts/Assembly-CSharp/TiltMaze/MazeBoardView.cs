using UnityEngine;

namespace TiltMaze
{
	public sealed class MazeBoardView : MonoBehaviour
	{
		[SerializeField]
		private Transform physicsRoot;

		[SerializeField]
		private Transform tiltRoot;

		[SerializeField]
		private Transform flipRoot;

		[SerializeField]
		private Transform feedbackRoot;

		[SerializeField]
		private Transform renderContent;

		[SerializeField]
		private SpriteRenderer shellRenderer;

		[SerializeField]
		private Collider2D[] shellColliders;

		public Transform PhysicsRoot => physicsRoot;

		public Transform TiltRoot => tiltRoot;

		public Transform FlipRoot => flipRoot;

		public Transform FeedbackRoot => feedbackRoot;

		public Transform RenderContent => renderContent;

		public SpriteRenderer ShellRenderer => shellRenderer;

		public Collider2D[] ShellColliders => shellColliders;
	}
}
