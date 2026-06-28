using System.Collections;
using UnityEngine;
using UnityEngine.Playables;

/// <summary>
/// Walks the selected character forward using their existing Animator parameters,
/// then hands off to the PlayableDirector for the credits UI sequence.
/// </summary>
[RequireComponent(typeof(PlayableDirector))]
public class CreditsCutsceneBinder : MonoBehaviour
{
    [Header("Character Start")]
    [Tooltip("Where the character stands at the beginning of the cutscene.")]
    public Transform characterStartPosition;

    [Header("Walk Settings")]
    public Vector2 walkDirection = Vector2.right;
    public float walkDistance = 3f;
    public float walkSpeed = 2f;

    [Header("Timing")]
    public float pauseAfterWalk = 0.8f;

    private PlayableDirector _director;
    private Rigidbody2D _playerRb;
    private Animator _playerAnimator;

    private void Awake()
    {
        _director = GetComponent<PlayableDirector>();
        _director.playOnAwake = false;
    }

    private IEnumerator Start()
    {
        while (OverworldSceneManager.PlayerInstance == null)
            yield return null;

        yield return null; // wait for ZoneSceneLoader to finish

        GameObject player = OverworldSceneManager.PlayerInstance;
        _playerRb = player.GetComponent<Rigidbody2D>();
        _playerAnimator = player.GetComponent<Animator>();

        // Disable the whole component so its Update() stops overriding the Animator
        var controller = player.GetComponent<PlayerController2D>();
        if (controller != null) controller.enabled = false;

        if (characterStartPosition != null)
            player.transform.position = characterStartPosition.position;

        _director.Play();

        yield return StartCoroutine(WalkForward());

        SetIdle();
    }

    private IEnumerator WalkForward()
    {
        if (_playerRb == null) yield break;

        Vector2 startPos = _playerRb.position;
        Vector2 targetPos = startPos + walkDirection.normalized * walkDistance;

        // This drives whichever character was spawned — no specific clip required
        _playerAnimator?.SetFloat("MoveX", walkDirection.normalized.x);
        _playerAnimator?.SetFloat("MoveY", walkDirection.normalized.y);
        _playerAnimator?.SetFloat("Speed", 1f);

        while (Vector2.Distance(_playerRb.position, targetPos) > 0.05f)
        {
            Vector2 next = Vector2.MoveTowards(_playerRb.position, targetPos, walkSpeed * Time.fixedDeltaTime);
            _playerRb.MovePosition(next);
            yield return new WaitForFixedUpdate();
        }

        _playerRb.MovePosition(targetPos);
    }

    private void SetIdle()
    {
        _playerAnimator?.SetFloat("Speed", 0f);
        _playerAnimator?.SetFloat("MoveX", 0f);
        _playerAnimator?.SetFloat("MoveY", 0f);
    }
}
