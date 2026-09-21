using System;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TiltMaze
{
	public sealed class MazeGameController : MonoBehaviour
	{
		private enum GameState
		{
			Menu = 0,
			Intro = 1,
			Playing = 2,
			Rotating = 3,
			Flipping = 4,
			LevelTransition = 5,
			Complete = 6
		}

		[Header("Prefab Instances")]
		[SerializeField]
		private MazeBoardView board;

		[SerializeField]
		private MazeBall ball;

		[SerializeField]
		private MazeGoalZone goal;

		[SerializeField]
		private MazeLevelLayout[] levelPrefabs;

		[Header("Presentation")]
		[SerializeField]
		private Camera gameCamera;

		[SerializeField]
		private Text statusText;

		[SerializeField]
		private CanvasGroup startMenu;

		[SerializeField]
		private Graphic introBackgroundPanel;

		[SerializeField]
		private CanvasGroup endMenu;

		[SerializeField]
		private Button startButton;

		[SerializeField]
		private Button quitButton;

		[SerializeField]
		private Button replayButton;

		[SerializeField]
		private RectTransform[] fallingTitleLetters;

		[SerializeField]
		private TMPTextShatter titleShatter;

		[SerializeField]
		private TMPTextShatter startOptionShatter;

		[SerializeField]
		private TMPTextShatter quitOptionShatter;

		[SerializeField]
		private MazeStartSequence startSequence;

		[SerializeField]
		private MazeTipController tipController;

		[SerializeField]
		private MazeEndSequence endSequence;

		[SerializeField]
		private SpriteRenderer ripplePrefab;

		[SerializeField]
		private ParticleSystem goalBurstPrefab;

		[SerializeField]
		private ParticleSystem wallImpactParticlePrefab;

		[SerializeField]
		private ParticleSystem environmentParticles;

		[SerializeField]
		private PhysicsMaterial2D menuBallMaterial;

		[SerializeField]
		private PhysicsMaterial2D gameplayBallMaterial;

		[SerializeField]
		private Color darkColor = new Color(0.035f, 0.035f, 0.045f, 1f);

		[SerializeField]
		private Color lightColor = new Color(0.96f, 0.96f, 0.94f, 1f);

		[SerializeField]
		[Range(0f, 1f)]
		private float ghostAlphaOnDarkBackground = 0.08f;

		[SerializeField]
		[Range(0f, 1f)]
		private float ghostAlphaOnLightBackground = 0.16f;

		[Header("Motion")]
		[SerializeField]
		[Min(0.05f)]
		private float rotateDuration = 0.14f;

		[SerializeField]
		[Min(0.1f)]
		private float flipDuration = 0.34f;

		[SerializeField]
		private float cameraShakeStrength = 0.16f;

		[SerializeField]
		[Min(0f)]
		private float wallImpactParticleMinimumSpeed = 0.5f;

		[SerializeField]
		[Min(0.05f)]
		private float wallImpactParticleLifetime = 1.2f;

		[SerializeField]
		private bool tintWallImpactParticles = true;

		[SerializeField]
		[Min(0f)]
		private float menuShatterDuration = 0.7f;

		[SerializeField]
		[Min(0.05f)]
		private float menuPanelFadeDuration = 0.8f;

		[SerializeField]
		[Min(0.05f)]
		private float cameraAlignDuration = 1.35f;

		[SerializeField]
		private Vector3 gameplayCameraEulerAngles = Vector3.zero;

		[SerializeField]
		private Ease cameraAlignEase = Ease.InOutSine;

		[SerializeField]
		[Min(0.1f)]
		private float openingFlipDuration = 0.5f;

		[SerializeField]
		[Range(0f, 1f)]
		private float fallbackMenuBounciness = 1f;

		[SerializeField]
		[Range(0f, 1f)]
		private float fallbackGameplayBounciness = 0.8f;

		private readonly List<Collider2D> activeSolidColliders = new List<Collider2D>();

		private readonly List<MazeGoalZone> activeCollectibles = new List<MazeGoalZone>();

		private MazeLevelLayout activeLevel;

		private GameState state;

		private int currentLevel;

		private int activeFace;

		private int queuedRotation;

		private float rotationTargetAngle;

		private Vector2 lastSafeBallPosition;

		private Vector3 initialCameraPosition;

		private float initialCameraSize;

		private int totalCollectibles;

		private int collectedCount;

		private PhysicsMaterial2D runtimeMenuBallMaterial;

		private PhysicsMaterial2D runtimeGameplayBallMaterial;

		private const float BallRadius = 0.48f;

		private void Start()
		{
			if (!ValidateConfiguration())
			{
				base.enabled = false;
				return;
			}
			ResolveUiReferences();
			ResolvePresentationReferences();
			ResolveStartSequence();
			if (tipController == null)
			{
				tipController = FindSceneComponent<MazeTipController>("Tip");
			}
			if (endSequence == null)
			{
				endSequence = FindSceneComponent<MazeEndSequence>("End Menu");
			}
			if (statusText != null)
			{
				statusText.gameObject.SetActive(value: false);
			}
			AudioManager.Instance.PlayMusic("bgm");
			ball.transform.SetParent(board.FeedbackRoot, worldPositionStays: true);
			ball.HardImpact += HandleHardImpact;
			ball.WallImpact += HandleWallImpact;
			if (startButton != null)
			{
				startButton.onClick.AddListener(StartGame);
			}
			if (quitButton != null)
			{
				quitButton.onClick.AddListener(QuitGame);
			}
			if (replayButton != null)
			{
				replayButton.onClick.AddListener(RestartFromEnd);
			}
			goal.gameObject.SetActive(value: false);
			initialCameraPosition = gameCamera.transform.position;
			initialCameraSize = gameCamera.orthographicSize;
			board.FeedbackRoot.localScale = Vector3.one;
			board.FlipRoot.localScale = Vector3.one;
			if (menuBallMaterial == null)
			{
				menuBallMaterial = ball.Circle.sharedMaterial;
			}
			if (menuBallMaterial != null)
			{
				ball.SetPhysicsMaterial(menuBallMaterial);
			}
			else
			{
				runtimeMenuBallMaterial = new PhysicsMaterial2D("Menu Ball Material (Runtime)")
				{
					bounciness = fallbackMenuBounciness
				};
				menuBallMaterial = runtimeMenuBallMaterial;
				ball.SetPhysicsMaterial(menuBallMaterial);
			}
			ball.Body.simulated = true;
			SetCollectiblesArmed(armed: false);
			if (environmentParticles != null)
			{
				environmentParticles.Stop(withChildren: true, ParticleSystemStopBehavior.StopEmittingAndClear);
				environmentParticles.gameObject.SetActive(value: false);
			}
			state = GameState.Menu;
			SetCanvasVisible(startMenu, visible: true, interactive: true);
			SetCanvasVisible(endMenu, visible: false, interactive: false);
		}

		private void Update()
		{
			ReadInput();
			UpdateBoardRotation();
			if (state == GameState.Playing && IsBallPositionSafe(ball.Body.position))
			{
				lastSafeBallPosition = ball.Body.position;
			}
		}

		private bool ValidateConfiguration()
		{
			int num;
			if (board != null && ball != null && goal != null && gameCamera != null && levelPrefabs != null)
			{
				num = ((levelPrefabs.Length != 0) ? 1 : 0);
				if (num != 0)
				{
					goto IL_005b;
				}
			}
			else
			{
				num = 0;
			}
			Debug.LogError("MazeGameController is missing prefab, level, or camera references.", this);
			goto IL_005b;
			IL_005b:
			return (byte)num != 0;
		}

		private void ReadInput()
		{
			if (state == GameState.Menu)
			{
				if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Space))
				{
					StartGame();
				}
				else if (Input.GetKeyDown(KeyCode.Escape))
				{
					QuitGame();
				}
				return;
			}
			if (Input.GetKeyDown(KeyCode.R))
			{
				if (state == GameState.Playing || state == GameState.Complete)
				{
					RestartLevel();
				}
				return;
			}
			int num = 0;
			if (Input.GetKeyDown(KeyCode.A))
			{
				num = 1;
			}
			if (Input.GetKeyDown(KeyCode.D))
			{
				num = -1;
			}
			if ((state == GameState.Playing || state == GameState.Rotating) && num != 0)
			{
				tipController?.NotifyRotationInput(num);
				RotateBoard(num);
			}
			if (state == GameState.Playing && Input.GetKeyDown(KeyCode.Space))
			{
				tipController?.NotifyFlipInput();
				BeginFlip(advanceLevel: false);
			}
		}

		private void RotateBoard(int direction)
		{
			if (state == GameState.Rotating)
			{
				if (queuedRotation != 0)
				{
					return;
				}
				queuedRotation = direction;
			}
			else
			{
				state = GameState.Rotating;
				queuedRotation = 0;
				rotationTargetAngle = Mathf.Round(board.transform.eulerAngles.z / 45f) * 45f;
			}
			rotationTargetAngle += (float)direction * 45f;
			board.FeedbackRoot.DOKill();
			board.FeedbackRoot.localScale = Vector3.one;
			board.FeedbackRoot.DOScale(0.965f, rotateDuration * 0.5f).SetLoops(2, LoopType.Yoyo).SetEase(Ease.OutQuad);
		}

		private void UpdateBoardRotation()
		{
			if (state == GameState.Rotating)
			{
				float num = Mathf.MoveTowardsAngle(board.transform.eulerAngles.z, rotationTargetAngle, 45f / rotateDuration * Time.deltaTime);
				board.transform.rotation = Quaternion.Euler(0f, 0f, num);
				Physics2D.SyncTransforms();
				if (!(Mathf.Abs(Mathf.DeltaAngle(num, rotationTargetAngle)) > 0.01f))
				{
					board.transform.rotation = Quaternion.Euler(0f, 0f, rotationTargetAngle);
					queuedRotation = 0;
					state = GameState.Playing;
					PlayRipple();
				}
			}
		}

		private void BeginFlip(bool advanceLevel)
		{
			state = (advanceLevel ? GameState.LevelTransition : GameState.Flipping);
			AudioManager.Instance.PlaySfx("flip");
			queuedRotation = 0;
			Vector2 preservedLocalPosition = board.transform.InverseTransformPoint(ball.Body.position);
			Vector2 preservedVelocity = ball.Body.velocity;
			ball.Body.simulated = false;
			SetCollectiblesArmed(armed: false);
			board.FlipRoot.DOKill();
			Sequence sequence = DOTween.Sequence().SetUpdate(isIndependentUpdate: true);
			sequence.Append(board.FlipRoot.DOScaleX(0f, flipDuration * 0.5f).SetEase(Ease.InQuad));
			sequence.AppendCallback(() =>
			{
				if (advanceLevel)
				{
					LoadLevel(currentLevel + 1, placeBall: false);
					AlignNextSpawnToBallSector();
					PlaceBallAtSpawn();
					preservedVelocity = Vector2.zero;
				}
				else
				{
					ball.Body.position = board.transform.TransformPoint(preservedLocalPosition);
				}
				activeFace = 1 - activeFace;
				preservedVelocity.x = 0f - preservedVelocity.x;
				ApplyFaceState(flipDuration * 0.5f);
				Physics2D.SyncTransforms();
				ResolveBallOverlap();
			});
			sequence.Append(board.FlipRoot.DOScaleX(1f, flipDuration * 0.5f).SetEase(Ease.OutQuad));
			sequence.OnComplete(() =>
			{
				Time.timeScale = 1f;
				ball.Body.simulated = true;
				ball.Body.velocity = preservedVelocity;
				state = GameState.Playing;
				SetCollectiblesArmed(armed: true);
				lastSafeBallPosition = ball.Body.position;
				PlayRipple();
				SetStatus();
				tipController?.ShowForLevel(currentLevel, (activeFace == 0) ? lightColor : darkColor);
			});
		}

		private void HandleGoalReached(MazeGoalZone collected)
		{
			if (state != GameState.Playing && state != GameState.Rotating)
			{
				return;
			}
			AudioManager.Instance.PlaySfx("pickup");
			activeCollectibles.Remove(collected);
			collectedCount++;
			bool num = collectedCount >= totalCollectibles;
			PlayGoalBurst(collected.transform.position);
			if (!num)
			{
				collected.PlayCollectedAnimation(isFinal: false, null);
				SetStatus();
				return;
			}
			state = GameState.LevelTransition;
			SetCollectiblesArmed(armed: false);
			Time.timeScale = 0.35f;
			gameCamera.DOOrthoSize(gameCamera.orthographicSize * 0.9f, 0.24f).SetLoops(2, LoopType.Yoyo).SetUpdate(isIndependentUpdate: true);
			collected.PlayCollectedAnimation(isFinal: true, () =>
			{
				if (currentLevel < levelPrefabs.Length - 1)
				{
					BeginFlip(advanceLevel: true);
				}
				else
				{
					Time.timeScale = 1f;
					state = GameState.Complete;
					ball.Body.simulated = false;
					SetStatus(string.Empty);
					AudioManager.Instance.PlaySfx("flip");
					if (endSequence != null)
					{
						Color textColor = ((activeFace == 0) ? lightColor : darkColor);
						endSequence.Play(board.FeedbackRoot, ball.Trail, environmentParticles, textColor);
					}
					else
					{
						SetCanvasVisible(endMenu, visible: true, interactive: true);
					}
				}
			});
		}

		private void LoadLevel(int index, bool placeBall)
		{
			DisposeCollectibles();
			if (activeLevel != null)
			{
				activeLevel.Dispose();
			}
			currentLevel = Mathf.Clamp(index, 0, levelPrefabs.Length - 1);
			activeLevel = UnityEngine.Object.Instantiate(levelPrefabs[currentLevel], board.transform);
			activeLevel.name = levelPrefabs[currentLevel].name;
			activeLevel.transform.localPosition = Vector3.zero;
			activeLevel.transform.localRotation = Quaternion.identity;
			activeLevel.transform.localScale = Vector3.one;
			activeLevel.Attach(board.PhysicsRoot, board.RenderContent);
			CreateCollectibles();
			ApplyFaceState();
			if (placeBall)
			{
				PlaceBallAtSpawn();
			}
			SetStatus();
		}

		private void ApplyFaceState(float colorDuration = 0f)
		{
			bool flag = activeFace == 0;
			Color color = (flag ? lightColor : darkColor);
			Color color2 = (flag ? darkColor : lightColor);
			Color color3 = color;
			ball.SetTrailColor(color);
			if (colorDuration > 0f)
			{
				DOTween.To(() => gameCamera.backgroundColor, (Color value) =>
				{
					gameCamera.backgroundColor = value;
				}, color2, colorDuration).SetEase(Ease.InOutSine);
				board.ShellRenderer.DOKill();
				ball.Visual.DOKill();
				board.ShellRenderer.DOColor(color, colorDuration).SetEase(Ease.InOutSine);
				ball.Visual.DOColor(color, colorDuration).SetEase(Ease.InOutSine);
				foreach (MazeGoalZone activeCollectible in activeCollectibles)
				{
					if (activeCollectible != null)
					{
						activeCollectible.Visual.DOColor(color3, colorDuration).SetEase(Ease.InOutSine);
					}
				}
				if (statusText != null)
				{
					statusText.DOColor(color, colorDuration).SetEase(Ease.InOutSine);
				}
			}
			else
			{
				gameCamera.backgroundColor = color2;
				board.ShellRenderer.color = color;
				ball.Visual.color = color;
				foreach (MazeGoalZone activeCollectible2 in activeCollectibles)
				{
					if (activeCollectible2 != null)
					{
						activeCollectible2.Visual.color = color3;
					}
				}
			}
			activeSolidColliders.Clear();
			Collider2D[] shellColliders = board.ShellColliders;
			foreach (Collider2D collider2D in shellColliders)
			{
				if (!(collider2D == null))
				{
					collider2D.enabled = true;
					activeSolidColliders.Add(collider2D);
				}
			}
			float inactiveAlpha = (flag ? ghostAlphaOnDarkBackground : ghostAlphaOnLightBackground);
			activeLevel.ApplyFace(activeFace, color, inactiveAlpha, activeSolidColliders, colorDuration);
		}

		private void CreateCollectibles()
		{
			collectedCount = 0;
			totalCollectibles = Mathf.Max(1, activeLevel.CollectibleCount);
			for (int i = 0; i < totalCollectibles; i++)
			{
				Vector2 collectiblePoint = activeLevel.GetCollectiblePoint(i);
				MazeGoalZone mazeGoalZone = UnityEngine.Object.Instantiate(goal, board.PhysicsRoot);
				mazeGoalZone.name = $"Collectible {i + 1:00}";
				mazeGoalZone.gameObject.SetActive(value: true);
				mazeGoalZone.transform.localPosition = collectiblePoint;
				mazeGoalZone.Visual.transform.SetParent(board.RenderContent, worldPositionStays: false);
				mazeGoalZone.Visual.transform.localPosition = collectiblePoint;
				mazeGoalZone.Visual.color = ((activeFace == 0) ? lightColor : darkColor);
				mazeGoalZone.Reached += HandleGoalReached;
				mazeGoalZone.SetArmed(value: false);
				activeCollectibles.Add(mazeGoalZone);
			}
		}

		private void DisposeCollectibles()
		{
			foreach (MazeGoalZone activeCollectible in activeCollectibles)
			{
				if (activeCollectible != null)
				{
					activeCollectible.Dispose();
				}
			}
			activeCollectibles.Clear();
		}

		private void SetCollectiblesArmed(bool armed)
		{
			foreach (MazeGoalZone activeCollectible in activeCollectibles)
			{
				if (activeCollectible != null)
				{
					activeCollectible.SetArmed(armed);
				}
			}
		}

		private void PlaceBallAtSpawn()
		{
			Vector2 worldPosition = board.transform.TransformPoint(activeLevel.SpawnPoint);
			ball.ResetAt(worldPosition);
			lastSafeBallPosition = worldPosition;
		}

		private void AlignNextSpawnToBallSector()
		{
			int num = Sector(ball.Body.position - (Vector2)board.transform.position);
			int num2 = Sector(activeLevel.SpawnPoint);
			float z = Mathf.DeltaAngle(0f, (float)(num - num2) * 45f);
			board.transform.rotation = Quaternion.Euler(0f, 0f, z);
			Physics2D.SyncTransforms();
		}

		private static int Sector(Vector2 vector)
		{
			return Mathf.RoundToInt(Mathf.Atan2(vector.x, vector.y) * 57.29578f / 45f + 8f) % 8;
		}

		private void ResolveBallOverlap()
		{
			Vector2 position = ball.Body.position;
			if (IsBallPositionSafe(position))
			{
				return;
			}
			for (float num = 0.1f; num <= 3f; num += 0.1f)
			{
				for (int i = 0; i < 32; i++)
				{
					float f = (float)i * MathF.PI * 2f / 32f;
					Vector2 position2 = position + new Vector2(Mathf.Cos(f), Mathf.Sin(f)) * num;
					if (IsBallPositionSafe(position2))
					{
						ball.Body.position = position2;
						return;
					}
				}
			}
			ball.Body.position = lastSafeBallPosition;
		}

		private bool IsBallPositionSafe(Vector2 position)
		{
			foreach (Collider2D activeSolidCollider in activeSolidColliders)
			{
				if (!(activeSolidCollider == null) && activeSolidCollider.enabled && (activeSolidCollider.ClosestPoint(position) - position).sqrMagnitude < 0.2304f)
				{
					return false;
				}
			}
			return true;
		}

		private void RestartLevel()
		{
			if (state == GameState.Complete)
			{
				RestartFromEnd();
				return;
			}
			DOTween.Kill(board.transform);
			Time.timeScale = 1f;
			state = GameState.Playing;
			queuedRotation = 0;
			ball.Body.simulated = true;
			LoadLevel(currentLevel, placeBall: true);
			SetCollectiblesArmed(armed: true);
		}

		private void StartGame()
		{
			if (state == GameState.Menu)
			{
				state = GameState.Intro;
				if (startMenu != null)
				{
					startMenu.blocksRaycasts = false;
					startMenu.interactable = false;
				}
				titleShatter?.Shatter();
				startOptionShatter?.Shatter();
				quitOptionShatter?.Shatter();
				if (introBackgroundPanel != null)
				{
					introBackgroundPanel.DOKill();
					introBackgroundPanel.DOFade(0f, menuPanelFadeDuration).SetEase(Ease.InOutSine).SetUpdate(isIndependentUpdate: true);
				}
				else if (startMenu != null)
				{
					startMenu.DOKill();
					startMenu.DOFade(0f, menuPanelFadeDuration).SetEase(Ease.InOutSine).SetUpdate(isIndependentUpdate: true);
				}
				gameCamera.transform.DOKill();
				gameCamera.transform.DORotate(gameplayCameraEulerAngles, cameraAlignDuration).SetEase(cameraAlignEase).SetUpdate(isIndependentUpdate: true)
					.OnComplete(() =>
					{
						PlayIntro();
					});
			}
		}

		private void RestartFromEnd()
		{
			Time.timeScale = 1f;
			SetCanvasVisible(endMenu, visible: false, interactive: false);
			gameCamera.transform.DOKill();
			gameCamera.DOKill();
			gameCamera.transform.position = initialCameraPosition;
			gameCamera.orthographicSize = initialCameraSize;
			activeFace = 0;
			queuedRotation = 0;
			board.transform.rotation = Quaternion.identity;
			board.TiltRoot.localScale = Vector3.one;
			board.FlipRoot.localScale = Vector3.one;
			board.FeedbackRoot.localScale = Vector3.one;
			ball.Body.simulated = true;
			LoadLevel(0, placeBall: true);
			PlayIntro();
		}

		private void QuitGame()
		{
			if (state == GameState.Menu)
			{
				state = GameState.Intro;
				if (startMenu != null)
				{
					startMenu.blocksRaycasts = false;
					startMenu.interactable = false;
				}
				titleShatter?.Shatter();
				startOptionShatter?.Shatter();
				quitOptionShatter?.Shatter();
				DOVirtual.DelayedCall(menuShatterDuration, Application.Quit).SetUpdate(isIndependentUpdate: true);
			}
		}

		private void PlayIntro()
		{
			state = GameState.Intro;
			SetCollectiblesArmed(armed: false);
			if (startSequence != null)
			{
				startSequence.Play();
			}
			else
			{
				BeginGameplayFromAnimationEvent();
			}
		}

		public void BeginGameplayFromAnimationEvent()
		{
			if (state != GameState.Intro)
			{
				return;
			}
			state = GameState.LevelTransition;
			ball.Body.simulated = false;
			SetCollectiblesArmed(armed: false);
			board.FlipRoot.DOKill();
			Sequence sequence = DOTween.Sequence().SetUpdate(isIndependentUpdate: true);
			sequence.Append(board.FlipRoot.DOScaleX(0f, openingFlipDuration * 0.5f).SetEase(Ease.InQuad));
			sequence.AppendCallback(() =>
			{
				activeFace = 0;
				ApplyGameplayBallMaterial();
				LoadLevel(0, placeBall: true);
				Physics2D.SyncTransforms();
			});
			sequence.Append(board.FlipRoot.DOScaleX(1f, openingFlipDuration * 0.5f).SetEase(Ease.OutQuad));
			sequence.OnComplete(() =>
			{
				if (state == GameState.LevelTransition)
				{
					ball.Body.simulated = true;
					SetCollectiblesArmed(armed: true);
					state = GameState.Playing;
					lastSafeBallPosition = ball.Body.position;
					SetStatus();
					tipController?.ShowForLevel(currentLevel, (activeFace == 0) ? lightColor : darkColor);
					if (environmentParticles != null)
					{
						environmentParticles.gameObject.SetActive(value: true);
						environmentParticles.Play(withChildren: true);
					}
				}
			});
		}

		private void ApplyGameplayBallMaterial()
		{
			if (gameplayBallMaterial != null)
			{
				ball.SetPhysicsMaterial(gameplayBallMaterial);
				return;
			}
			if (runtimeGameplayBallMaterial == null)
			{
				runtimeGameplayBallMaterial = new PhysicsMaterial2D("Gameplay Ball Material");
				runtimeGameplayBallMaterial.name = "Gameplay Ball Material (Runtime)";
				if (menuBallMaterial != null)
				{
					runtimeGameplayBallMaterial.friction = menuBallMaterial.friction;
				}
				runtimeGameplayBallMaterial.bounciness = fallbackGameplayBounciness;
			}
			ball.SetPhysicsMaterial(runtimeGameplayBallMaterial);
		}

		private void HandleHardImpact(float speed)
		{
			if (!(gameCamera == null))
			{
				float strength = cameraShakeStrength * Mathf.InverseLerp(5.5f, 12f, speed);
				gameCamera.transform.DOShakePosition(0.14f, strength, 12, 80f);
			}
		}

		private void HandleWallImpact(Vector2 point, Vector2 reverseVelocity, float speed)
		{
			if (state != GameState.Playing || speed < wallImpactParticleMinimumSpeed)
			{
				return;
			}
			float volumeScale = Mathf.Lerp(0.58f, 1f, Mathf.InverseLerp(wallImpactParticleMinimumSpeed, 8f, speed));
			AudioManager.Instance.PlaySfx("wall", volumeScale);
			if (!(wallImpactParticlePrefab == null))
			{
				ParticleSystem particleSystem = UnityEngine.Object.Instantiate(wallImpactParticlePrefab, board.transform);
				particleSystem.transform.position = point;
				if (reverseVelocity.sqrMagnitude > 0.001f)
				{
					particleSystem.transform.up = reverseVelocity;
				}
				ParticleSystem.MainModule main = particleSystem.main;
				main.simulationSpace = ParticleSystemSimulationSpace.Local;
				if (tintWallImpactParticles)
				{
					main.startColor = ((activeFace == 0) ? lightColor : darkColor);
				}
				particleSystem.Play(withChildren: true);
				UnityEngine.Object.Destroy(particleSystem.gameObject, wallImpactParticleLifetime);
			}
		}

		private void PlayRipple()
		{
			if (!(ripplePrefab == null))
			{
				SpriteRenderer ripple = UnityEngine.Object.Instantiate(ripplePrefab, board.RenderContent);
				ripple.transform.localPosition = Vector3.zero;
				ripple.transform.localRotation = Quaternion.identity;
				ripple.transform.localScale = Vector3.one;
				Color color = ((activeFace == 0) ? lightColor : darkColor);
				color.a = 0.55f;
				ripple.color = color;
				Sequence sequence = DOTween.Sequence();
				sequence.Join(ripple.transform.DOScale(1.16f, 0.32f).SetEase(Ease.OutQuad));
				sequence.Join(ripple.DOFade(0f, 0.32f).SetEase(Ease.OutQuad));
				sequence.OnComplete(() =>
				{
					UnityEngine.Object.Destroy(ripple.gameObject);
				});
			}
		}

		private void PlayGoalBurst(Vector3 position)
		{
			if (!(goalBurstPrefab == null))
			{
				ParticleSystem particleSystem = UnityEngine.Object.Instantiate(goalBurstPrefab, board.transform);
				particleSystem.transform.localPosition = board.transform.InverseTransformPoint(position);
				particleSystem.transform.localRotation = Quaternion.identity;
				ParticleSystem.MainModule main = particleSystem.main;
				main.simulationSpace = ParticleSystemSimulationSpace.Local;
				main.startColor = ((activeFace == 0) ? lightColor : darkColor);
				particleSystem.Play();
				UnityEngine.Object.Destroy(particleSystem.gameObject, 1.5f);
			}
		}

		private static void SetCanvasVisible(CanvasGroup group, bool visible, bool interactive)
		{
			if (!(group == null))
			{
				group.alpha = (visible ? 1f : 0f);
				group.interactable = visible & interactive;
				group.blocksRaycasts = visible & interactive;
				group.gameObject.SetActive(visible);
			}
		}

		private void ResolveUiReferences()
		{
			if (startMenu == null)
			{
				startMenu = FindSceneComponent<CanvasGroup>("Start Menu");
			}
			if (introBackgroundPanel == null)
			{
				GameObject gameObject = GameObject.Find("Background Panel") ?? GameObject.Find("Start Background Panel") ?? GameObject.Find("Panel");
				if (gameObject != null)
				{
					introBackgroundPanel = gameObject.GetComponent<Graphic>();
				}
			}
			if (endMenu == null)
			{
				endMenu = FindSceneComponent<CanvasGroup>("End Menu");
			}
			if (startButton == null)
			{
				startButton = FindSceneComponent<Button>("Start Button");
			}
			if (quitButton == null)
			{
				quitButton = FindSceneComponent<Button>("Quit Button");
			}
			if (replayButton == null)
			{
				replayButton = FindSceneComponent<Button>("Replay Button");
			}
			GameObject legacyTitle = GameObject.Find("Falling Title");
			TMP_Text text = FindOrCreateTitleText(legacyTitle);
			TMP_Text text2 = EnsureButtonTmp(startButton, "START GAME");
			TMP_Text text3 = EnsureButtonTmp(quitButton, "EXIT GAME");
			titleShatter = EnsureShatter(text, titleShatter);
			startOptionShatter = EnsureShatter(text2, startOptionShatter);
			quitOptionShatter = EnsureShatter(text3, quitOptionShatter);
			titleShatter?.Prepare();
			startOptionShatter?.Prepare();
			quitOptionShatter?.Prepare();
		}

		private TMP_Text FindOrCreateTitleText(GameObject legacyTitle)
		{
			TMP_Text tMP_Text = FindTmpByObjectName("Main Title TMP");
			if (tMP_Text != null)
			{
				tMP_Text.text = "UNTITLED PROJECT 1";
				return tMP_Text;
			}
			if (startMenu == null)
			{
				return null;
			}
			if (legacyTitle != null)
			{
				legacyTitle.SetActive(value: false);
			}
			GameObject obj = new GameObject("Main Title TMP", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
			RectTransform obj2 = (RectTransform)obj.transform;
			obj2.SetParent(startMenu.transform, worldPositionStays: false);
			Vector2 anchorMin = (obj2.anchorMax = new Vector2(0.5f, 0.5f));
			obj2.anchorMin = anchorMin;
			obj2.anchoredPosition = new Vector2(0f, 170f);
			obj2.sizeDelta = new Vector2(1150f, 130f);
			TextMeshProUGUI component = obj.GetComponent<TextMeshProUGUI>();
			component.text = "UNTITLED PROJECT 1";
			component.alignment = TextAlignmentOptions.Center;
			component.fontSize = 72f;
			component.fontStyle = FontStyles.Bold;
			component.color = Color.white;
			component.raycastTarget = false;
			return component;
		}

		private static TMP_Text EnsureButtonTmp(Button button, string fallbackText)
		{
			if (button == null)
			{
				return null;
			}
			TMP_Text tMP_Text = button.GetComponentInChildren<TMP_Text>(includeInactive: true);
			Text componentInChildren = button.GetComponentInChildren<Text>(includeInactive: true);
			if (tMP_Text == null)
			{
				GameObject obj = new GameObject("Label TMP", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
				RectTransform obj2 = (RectTransform)obj.transform;
				obj2.SetParent(button.transform, worldPositionStays: false);
				obj2.anchorMin = Vector2.zero;
				obj2.anchorMax = Vector2.one;
				Vector2 offsetMin = (obj2.offsetMax = Vector2.zero);
				obj2.offsetMin = offsetMin;
				TextMeshProUGUI component = obj.GetComponent<TextMeshProUGUI>();
				component.text = ((componentInChildren != null && !string.IsNullOrWhiteSpace(componentInChildren.text)) ? componentInChildren.text : fallbackText);
				component.alignment = TextAlignmentOptions.Center;
				component.fontSize = ((componentInChildren != null) ? ((float)componentInChildren.fontSize) : 38f);
				component.color = ((componentInChildren != null) ? componentInChildren.color : Color.white);
				component.raycastTarget = false;
				tMP_Text = component;
			}
			if (componentInChildren != null)
			{
				componentInChildren.enabled = false;
			}
			Image component2 = button.GetComponent<Image>();
			if (component2 != null)
			{
				Color color = component2.color;
				color.a = 0f;
				component2.color = color;
			}
			return tMP_Text;
		}

		private static TMPTextShatter EnsureShatter(TMP_Text text, TMPTextShatter current)
		{
			if (text == null)
			{
				return current;
			}
			TMPTextShatter obj = ((current != null) ? current : (text.GetComponent<TMPTextShatter>() ?? text.gameObject.AddComponent<TMPTextShatter>()));
			obj.Configure(text);
			return obj;
		}

		private static TMP_Text FindTmpByObjectName(string objectName)
		{
			GameObject gameObject = GameObject.Find(objectName);
			if (!(gameObject == null))
			{
				return gameObject.GetComponent<TMP_Text>();
			}
			return null;
		}

		private void ResolveStartSequence()
		{
			if (startSequence == null)
			{
				GameObject gameObject = GameObject.Find("StartAnim");
				if (gameObject != null)
				{
					startSequence = gameObject.GetComponent<MazeStartSequence>() ?? gameObject.AddComponent<MazeStartSequence>();
				}
			}
			if (startSequence == null)
			{
				return;
			}
			TMP_Text ignoredLegacyStartText = null;
			TMP_Text[] componentsInChildren = startSequence.GetComponentsInChildren<TMP_Text>(includeInactive: true);
			foreach (TMP_Text tMP_Text in componentsInChildren)
			{
				if (tMP_Text.text.IndexOf("START", StringComparison.OrdinalIgnoreCase) >= 0)
				{
					ignoredLegacyStartText = tMP_Text;
					break;
				}
			}
			startSequence.Configure(this, ignoredLegacyStartText);
		}

		private static T FindSceneComponent<T>(string objectName) where T : Component
		{
			GameObject gameObject = GameObject.Find(objectName);
			if (!(gameObject == null))
			{
				return gameObject.GetComponent<T>();
			}
			return null;
		}

		private void ResolvePresentationReferences()
		{
			if (ripplePrefab == null)
			{
				GameObject gameObject = Resources.Load<GameObject>("Maze/MazeRipple");
				if (gameObject != null)
				{
					ripplePrefab = gameObject.GetComponent<SpriteRenderer>();
				}
			}
			if (goalBurstPrefab == null)
			{
				GameObject gameObject2 = Resources.Load<GameObject>("Maze/MazeCollectibleBurst");
				if (gameObject2 != null)
				{
					goalBurstPrefab = gameObject2.GetComponent<ParticleSystem>();
				}
			}
		}

		private void SetStatus(string overrideMessage = null)
		{
			if (!(statusText == null))
			{
				statusText.text = overrideMessage ?? string.Format("关卡 {0}/{1}    收集 {2}/{3}    面 {4}", currentLevel + 1, levelPrefabs.Length, collectedCount, totalCollectibles, (activeFace == 0) ? "白" : "黑");
				statusText.color = ((activeFace == 0) ? lightColor : darkColor);
			}
		}
	}
}
