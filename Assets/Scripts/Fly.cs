using UnityEngine;

public class Fly : MonoBehaviour
{
    enum Mode
    {
        Roam,
        Evade
    }
    
    [Header("References")]
    [SerializeField] HandSmack hand;
    
    [Header("Senses")]
    [Tooltip("Seconds between the fly's checks")]
    [SerializeField] float reactionTime = 0.1f;

    [Tooltip("Extra range around death")]
    [SerializeField] float safetyMargin = 0.25f;
    
    [Header("Movement")]
    [SerializeField] float cruiseSpeed = 2.5f;
    [SerializeField] float dartSpeed = 9f;
    [SerializeField] float acceleration = 30f;
    [SerializeField] float buzzJitter = 1.5f;

    [Header("Area")]
    [SerializeField] BoxCollider2D flyArea;
    
    #region State

    Mode mode;
    float modeTimer;
    float nextThink;
    float noiseSeed;

    Vector2 velocity;
    Vector2 roamTarget;
    Vector2 escapeDir;
    
    CircleCollider2D circle;
    
    #endregion 


    #region Properties
    Vector2 Position => transform.position;

    float HitRadius => circle.radius * transform.lossyScale.x;

    float KillRange => HitRadius + safetyMargin;
    #endregion

    #region Unity

    void Awake()
    {
        if (hand == null)
        {
            hand = FindFirstObjectByType<HandSmack>();
        }

        circle = GetComponent<CircleCollider2D>();
        noiseSeed = Random.value * 100f;
        roamTarget = Position;
    }

    void Update()
    {
        float dt = Time.deltaTime;
        modeTimer = modeTimer + dt;

        if (Time.time >= nextThink)
        {
            nextThink = Time.time + reactionTime;
            Think();
        }

        if (mode == Mode.Roam)
        {
            Vector2 wanted = Roam();
            Move(wanted, 1f, dt);
        }
        else if (mode == Mode.Evade)
        {
            Move(Evade(dt), 0.3f, dt);
        }
    }
    #endregion

    #region Decisions 

    void Think()
    {

        if (mode != Mode.Evade)
        {
            if (InDanger())
            {
                StartEvade();
            }
        }
    }

    bool InDanger()
    {
        if (hand.IsRaising == false && hand.IsLowering == false)
        {
            return false;
        }

        float dist = Vector2.Distance(Position, hand.HitCenter);
        return dist <= KillRange + dartSpeed * reactionTime;
    }

    void SetMode(Mode newMode)
    {
        if (mode == newMode)
        {
            return;
        }

        mode = newMode;
        modeTimer = 0f;

        if (newMode == Mode.Roam)
        {
            roamTarget = RandomSpotInArea();
        }
    }
    
    #endregion

    #region Behaviors

    Vector2 Roam()
    {
        float distToTarget = Vector2.Distance(Position, roamTarget);

        if (distToTarget < 0.3f || modeTimer > 3f)
        {
            roamTarget = RandomSpotInArea();
            modeTimer = 0f;
        }

        Vector2 wanted = Seek(roamTarget, cruiseSpeed, 0.8f);
        return wanted;
    }

    void StartEvade()
    {
        SetMode(Mode.Evade);
        escapeDir = PickEscapeDir();

        velocity = escapeDir * dartSpeed * 0.5f;
    }

    Vector2 Evade(float dt)
    {
        if (Random.value < dt * 7f)
        {
            escapeDir = PickEscapeDir();
        }

        float distToHand = Vector2.Distance(Position, hand.HitCenter);
        bool clear = distToHand > KillRange * 2f;

        if (modeTimer > 1.5f || (modeTimer > 0.3f && clear))
        {
            SetMode(Mode.Roam);
        }

        return escapeDir * dartSpeed;
    }
    
    #endregion

    #region Movement Helpers

    void Move(Vector2 desired, float buzzScale, float dt)
    {
        velocity = Vector2.MoveTowards(velocity, desired, acceleration * dt);

        float noiseX = Mathf.PerlinNoise(noiseSeed, Time.time * 6f) - 0.5f;
        float noiseY = Mathf.PerlinNoise(noiseSeed + 50f, Time.time * 6f) - 0.5f;
        Vector2 buzz = new Vector2(noiseX, noiseY);
        buzz = buzz * 2f * buzzJitter * buzzScale;

        Vector2 next = Position + (velocity + buzz) * dt;
        Vector2 clamped = ClampToArea(next);

        Vector2 pushedBack = next - clamped;   

        if (pushedBack.sqrMagnitude > 0.000001f)
        {
            Vector2 outward = pushedBack.normalized;
            float intoEdge = Vector2.Dot(velocity, outward);

            if (intoEdge > 0f)
            {
                velocity = velocity - outward * intoEdge;
            }
        }

        transform.position = new Vector3(clamped.x, clamped.y, transform.position.z);
    }

    Vector2 Seek(Vector2 target, float speed, float slowRadius)
    {
        Vector2 toTarget = target - Position;
        float dist = toTarget.magnitude;

        if (dist < 0.001f)
        {
            return Vector2.zero;
        }

        Vector2 direction = toTarget / dist;
        float slowDown = Mathf.Clamp01(dist / slowRadius);
        return direction * speed * slowDown;
    }
    
    Vector2 PickEscapeDir()
    {
        float angle = Random.Range(0f, Mathf.PI * 2f);
        return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
    }
    
    Vector2 RandomSpotInArea()
    {
        Bounds b = flyArea.bounds;
        return new Vector2(
            Random.Range(b.min.x, b.max.x),
            Random.Range(b.min.y, b.max.y)
        );
    }
    Vector2 ClampToArea(Vector2 point)
    {
        return flyArea.ClosestPoint(point);
    }
    
    #endregion
}