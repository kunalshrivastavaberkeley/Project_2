using UnityEngine;
using UnityEngine.InputSystem;

public class HandSmack : MonoBehaviour
{
    enum State
    {
        Idle,
        Raising,
        Lowering,
        Recovering
    }
    
    [Header("Input")]
    [SerializeField] InputActionReference smackAction;
    
    [Header("Sprites")]
    [SerializeField] Sprite idleSprite;
    [SerializeField] Sprite shadowSprite;
    [SerializeField] Sprite smackSprite;

    [Tooltip("Mosquito under hand")]
    [SerializeField] Sprite bloodySprite;

    [Tooltip("Nothing under hand")]
    [SerializeField] Sprite hurtSprite;

    [SerializeField] Transform visual;

    [Header("Timing (seconds)")]
    [Tooltip("How long the hand rises before it smacks")]
    [SerializeField] float maxHoldTime = 1.5f;
    [SerializeField] float lowerDuration = 0.08f;
    [SerializeField] float bloodyDuration = 0.5f;
    [SerializeField] float hurtDuration = 1f;

    
    [Header("Raise")]
    [SerializeField]  float minShadowScale = 0.4f;
    
    [Header("Hit Detection")]
    [SerializeField] string mosquitoTag = "Mosquito";
    
    public event System.Action<Vector2, float> Smacked;
    
    SpriteRenderer sr;
    State state;
    float timer;
    float recoverDuration;
    Vector3 lockedPosition;
    

    #region SpyFly Info
    public bool IsRaising => state == State.Raising;

    public bool IsLowering => state == State.Lowering;

    public bool IsLocked => IsRaising || IsLowering;

    public float HoldTime => IsRaising ? timer : 0f;

    public float RecoveryTimeLeft =>
        state == State.Recovering ? Mathf.Max(0f, recoverDuration - timer) : 0f;

    public float MaxHoldTime => maxHoldTime;

    public float LowerDuration => lowerDuration;

    public Vector2 HitCenter => transform.position;
    #endregion

    #region Input Helper
    InputAction GetAction()
    {
        if (smackAction == null)
        {
            return null;
        }
        return smackAction.action;
    }
    #endregion

    #region Unity
    
    void Awake()
    {
        sr = visual.GetComponent<SpriteRenderer>();
        SetState(State.Idle, idleSprite);
    }

    void OnEnable()
    {
        GetAction()?.Enable();
    }

    void OnDisable()
    {
        GetAction()?.Disable();
    }

    void Update()
    {
        InputAction action = GetAction();
        if (action == null)
        {
            return;
        }

        timer += Time.deltaTime;

        if (state == State.Idle)
        {
            if (action.WasPressedThisFrame())
            {
                lockedPosition = transform.position;
                SetState(State.Raising, shadowSprite);
            }
        }
        else if (state == State.Raising)
        {
            float t = timer / maxHoldTime;
            float scale = Mathf.Lerp(1f, minShadowScale, t);
            visual.localScale = Vector3.one * scale;

            if (timer >= maxHoldTime)
            {
                SetState(State.Lowering, smackSprite);
            }
        }
        else if (state == State.Lowering)
        {
            if (timer >= lowerDuration)
            {
                Impact();
            }
        }
        else if (state == State.Recovering)
        {
            if (timer >= recoverDuration)
            {
                SetState(State.Idle, idleSprite);
            }
        }
    }

    void LateUpdate()
    {
        if (IsLocked)
        {
            transform.position = lockedPosition;
        }
    }
    
    #endregion

    #region Smacking


    void Impact()
    {
        Collider2D hit = Physics2D.OverlapPoint(HitCenter);
        bool killed = hit?.CompareTag(mosquitoTag) == true;

        Smacked?.Invoke(HitCenter, maxHoldTime);

        if (killed)
        {
            Destroy(hit.gameObject);
            Recover(bloodySprite, bloodyDuration);
        }
        else
        {
            Recover(hurtSprite, hurtDuration);
        }
    }
    
    void Recover(Sprite sprite, float duration)
    {
        recoverDuration = duration;
        SetState(State.Recovering, sprite);
    }

    void SetState(State next, Sprite sprite)
    {
        state = next;
        timer = 0f;
        visual.localScale = Vector3.one;

        if (sprite != null)
        {
            sr.sprite = sprite;
        }
    }
    
    #endregion
}