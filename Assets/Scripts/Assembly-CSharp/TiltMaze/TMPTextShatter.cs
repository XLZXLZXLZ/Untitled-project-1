using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;

namespace TiltMaze
{
	public sealed class TMPTextShatter : MonoBehaviour
	{
		[SerializeField]
		private TMP_Text source;

		[SerializeField]
		[Min(0f)]
		private float horizontalScatter = 110f;

		[SerializeField]
		[Min(0f)]
		private float fallDistance = 900f;

		[SerializeField]
		private Vector2 durationRange = new Vector2(0.42f, 0.62f);

		[SerializeField]
		[Min(0f)]
		private float characterDelay = 0.018f;

		[SerializeField]
		private Vector2 rotationRange = new Vector2(-100f, 100f);

		[SerializeField]
		[Range(0f, 1f)]
		private float endScale = 0.75f;

		private readonly List<RectTransform> shardRoots = new List<RectTransform>();

		private readonly List<CanvasGroup> shardGroups = new List<CanvasGroup>();

		private readonly List<TMP_Text> shardTexts = new List<TMP_Text>();

		private readonly List<int> shardCharacterIndices = new List<int>();

		private bool prepared;

		private bool shattered;

		public TMP_Text Source => source;

		public float TotalDuration => Mathf.Max(durationRange.x, durationRange.y) + characterDelay * (float)shardRoots.Count;

		public void Configure(TMP_Text target)
		{
			if (!(source == target))
			{
				ClearShards();
				source = target;
			}
		}

		public void Prepare()
		{
			if (prepared || source == null || !(source.transform is RectTransform))
			{
				return;
			}
			source.ForceMeshUpdate();
			TMP_TextInfo textInfo = source.textInfo;
			for (int i = 0; i < textInfo.characterCount; i++)
			{
				TMP_CharacterInfo tMP_CharacterInfo = textInfo.characterInfo[i];
				if (tMP_CharacterInfo.isVisible)
				{
					GameObject obj = new GameObject(source.name + " Character " + i, typeof(RectTransform), typeof(CanvasGroup))
					{
						layer = source.gameObject.layer
					};
					RectTransform rectTransform = (RectTransform)obj.transform;
					CanvasGroup component = obj.GetComponent<CanvasGroup>();
					rectTransform.SetParent(source.transform.parent, worldPositionStays: false);
					Vector3 position = (tMP_CharacterInfo.bottomLeft + tMP_CharacterInfo.topRight) * 0.5f;
					rectTransform.position = source.transform.TransformPoint(position);
					rectTransform.rotation = source.transform.rotation;
					rectTransform.localScale = Vector3.one;
					TMP_Text tMP_Text = Object.Instantiate(source, source.transform.parent);
					tMP_Text.name = source.name + " Glyph " + i;
					tMP_Text.raycastTarget = false;
					TMPTextShatter component2 = tMP_Text.GetComponent<TMPTextShatter>();
					if (component2 != null)
					{
						component2.enabled = false;
						Object.Destroy(component2);
					}
					tMP_Text.transform.SetParent(rectTransform, worldPositionStays: true);
					tMP_Text.ForceMeshUpdate();
					MaskToCharacter(tMP_Text, i);
					shardRoots.Add(rectTransform);
					shardGroups.Add(component);
					shardTexts.Add(tMP_Text);
					shardCharacterIndices.Add(i);
					rectTransform.gameObject.SetActive(value: false);
				}
			}
			prepared = true;
		}

		public void Shatter()
		{
			if (shattered || source == null)
			{
				return;
			}
			Prepare();
			shattered = true;
			source.enabled = false;
			for (int i = 0; i < shardRoots.Count; i++)
			{
				RectTransform rectTransform = shardRoots[i];
				CanvasGroup canvasGroup = shardGroups[i];
				TMP_Text tMP_Text = shardTexts[i];
				if (!(rectTransform == null) && !(canvasGroup == null) && !(tMP_Text == null))
				{
					rectTransform.gameObject.SetActive(value: true);
					tMP_Text.ForceMeshUpdate();
					MaskToCharacter(tMP_Text, shardCharacterIndices[i]);
					float num = (float)i * characterDelay;
					float num2 = Random.Range(Mathf.Min(durationRange.x, durationRange.y), Mathf.Max(durationRange.x, durationRange.y));
					Vector3 endValue = rectTransform.position + new Vector3(Random.Range(0f - horizontalScatter, horizontalScatter), 0f - fallDistance, 0f);
					rectTransform.DOMove(endValue, num2).SetDelay(num).SetEase(Ease.InQuad)
						.SetUpdate(isIndependentUpdate: true);
					rectTransform.DORotate(new Vector3(0f, 0f, Random.Range(rotationRange.x, rotationRange.y)), num2, RotateMode.FastBeyond360).SetDelay(num).SetEase(Ease.InQuad)
						.SetUpdate(isIndependentUpdate: true);
					rectTransform.DOScale(endScale, num2).SetDelay(num).SetEase(Ease.InQuad)
						.SetUpdate(isIndependentUpdate: true);
					canvasGroup.DOFade(0f, num2 * 0.35f).SetDelay(num + num2 * 0.65f).SetUpdate(isIndependentUpdate: true);
				}
			}
		}

		private static void MaskToCharacter(TMP_Text text, int visibleIndex)
		{
			TMP_TextInfo textInfo = text.textInfo;
			for (int i = 0; i < textInfo.characterCount; i++)
			{
				TMP_CharacterInfo tMP_CharacterInfo = textInfo.characterInfo[i];
				if (tMP_CharacterInfo.isVisible && i != visibleIndex)
				{
					Color32[] colors = textInfo.meshInfo[tMP_CharacterInfo.materialReferenceIndex].colors32;
					int vertexIndex = tMP_CharacterInfo.vertexIndex;
					colors[vertexIndex].a = 0;
					colors[vertexIndex + 1].a = 0;
					colors[vertexIndex + 2].a = 0;
					colors[vertexIndex + 3].a = 0;
				}
			}
			text.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);
		}

		private void ClearShards()
		{
			foreach (RectTransform shardRoot in shardRoots)
			{
				if (shardRoot != null)
				{
					Object.Destroy(shardRoot.gameObject);
				}
			}
			shardRoots.Clear();
			shardGroups.Clear();
			shardTexts.Clear();
			shardCharacterIndices.Clear();
			prepared = false;
			shattered = false;
		}
	}
}
