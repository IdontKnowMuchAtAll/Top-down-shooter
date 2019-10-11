using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Panda;
using UnityEngine.Events;
public class BossBehaviour : MonoBehaviour {
    private ObjectPooler _objectPooler;
    private LineRenderer _lineRenderer;
    public float dashSpeed;
    public float moveSpeed;
    float laserLength = 100.0f;
    float laserWidth = 3.0f;
    private Rigidbody _rigidBody;
    private bool isWalking = false;
    private bool isDashing = false;
    private Vector3 direction;
    private Vector3 velocity;
    public Vector3 middle;
    private float fireSpeed = 20.0f;
    [SerializeField] private float damage;
    private float lastCollisionTime; //when the boss collides with the player at high speeds this time is used to stop it from triggering oncollision twice
    [SerializeField] private GameObject player;
    [SerializeField] private GameObject laserParticles;
    [SerializeField] private GameObject dashParticles;
    private bool[] inCoroutine;
    private bool laserDashCoroutine;
    private Health _health;
    void Start () {
        inCoroutine = new bool[8] { true, true, true, true, true, true, true, true };
        isDashing = false;
        _objectPooler = ObjectPooler.instance;
        _rigidBody = GetComponent<Rigidbody>();
        _health = GetComponent<Health>();
        _lineRenderer = GetComponent<LineRenderer>();
        _lineRenderer.enabled = false;
	}
	
    [Task]
    bool SetMoveDirection()
    {
        float xDist = Mathf.Abs(transform.position.x - player.transform.position.x);
        float zDist = Mathf.Abs(transform.position.z - player.transform.position.z);
        float threshHold = 0.6f;
        if((xDist) < (zDist))
        {
            if (xDist > threshHold)
            {
                if (transform.position.x < player.transform.position.x)
                {
                    direction = Vector3.right;
                }
                else
                {
                    direction = Vector3.left;
                }
                return true;

            }
        }
        else
        {
            if (zDist > threshHold)
            {
                if (transform.position.z < player.transform.position.z)
                {
                    direction = Vector3.forward;
                }
                else
                {
                    direction = Vector3.back;
                }
                return true;

            }
        }
        return false;
    }

    [Task]
    void MoveTo_Point(float x, float z)
    {
        if (Task.current.isStarting)
        {
            isWalking = true;
            isDashing = false;
        }
        //this code still needs to be repeated if tthe boss were to be moved by anything else the direction needs to be adjusted
        direction = new Vector3(x, transform.position.y, z) - transform.position;
        direction.Normalize();

        if (Vector3.Distance(transform.position, new Vector3(x, transform.position.y, z)) < 0.1f)
        {
            isWalking = false;
            Task.current.Succeed();
        }

    }


    [Task]
    bool StopDashing()
    {
        StopAllCoroutines();
        dashParticles.SetActive(false);
        isDashing = false;

        return true;
    }

    [Task]
    void Dash()
    {
        if(Task.current.isStarting)
            isDashing = true;

        if (!isDashing)
            Task.current.Succeed();
    }

    [Task]
    void StartDash()
    {
        if (Task.current.isStarting) //only do this once
        {
            isWalking = false;
            laserDashCoroutine = false;
            float delay = 0.2f;
            StartCoroutine(StartDashParticles(delay));
        }
        if(laserDashCoroutine)
        {
            Task.current.Succeed();
        }
    }

    IEnumerator StartDashParticles(float delay)
    {
        dashParticles.SetActive(true);
        yield return new WaitForSeconds(delay);
        laserDashCoroutine = true;
        float waitTime = 2.0f - delay;
        if (waitTime > 0.0f)
            yield return new WaitForSeconds(waitTime);
        dashParticles.SetActive(false);
    }

    [Task]
    bool Walk()
    {
        isWalking = true;
        isDashing = false;
        return true;
    }

    [Task]
    void MoveTo_Middle()
    {
        isWalking = true;
        isDashing = false;
        direction = middle - transform.position;
        direction.Normalize();

        if(Vector3.Distance(transform.position, new Vector3(middle.x, transform.position.y, middle.z)) < 0.1f)
            Task.current.Succeed();
    }

    [Task]
    bool HasLocked_Player()
    {
        if (isDashing) //if dashing this means you were already locked onto the player
            return true;

        //cast raycasts to check if the player is in a good position
        RaycastHit hit;


        var pos = new Vector3(transform.position.x, player.transform.position.y, transform.position.z);


        float range = 500.0f;
        int layerMask = LayerMask.GetMask("Player");
        Debug.DrawRay(transform.position, Vector3.left);
        if (Physics.Raycast(pos, Vector3.left, out hit, range, layerMask))
        {
            direction = Vector3.left;
            return true;
        }
        else if (Physics.Raycast(pos, Vector3.right, out hit, range, layerMask))
        {
            direction = Vector3.right;
            return true;
        }
        else if (Physics.Raycast(pos, Vector3.forward, out hit, range, layerMask))
        {
            direction = Vector3.forward;
            return true;
        }
        else if (Physics.Raycast(pos, Vector3.back, out hit, range, layerMask))
        {
            direction = Vector3.back;
            return true;
        }
        return false;
    }

    [Task]
    bool IsHealthPercentHigherThan(float percent)
    {
        float healthPercent = _health.currentHealth / _health.startHealth * 100.0f;
        if(healthPercent >= percent)
        {
            return true;
        }
        return false;
    }

    [Task]
    bool IsHealthPercentLowerThan(float percent)
    {
        float healthPercent = _health.currentHealth / _health.startHealth * 100.0f;
        if (healthPercent <= percent)
        {
            return true;
        }
        return false;
    }


    private void Update()
    {
        if (isWalking)
        {
            velocity = new Vector3(moveSpeed * direction.x, 0.0f, moveSpeed * direction.z) * Time.fixedDeltaTime;
        }
        else if (isDashing)
        {
            velocity = new Vector3(dashSpeed * direction.x, 0.0f, dashSpeed * direction.z) * Time.fixedDeltaTime;
        }
        else
            velocity = Vector3.zero;

        _rigidBody.velocity = velocity;

    }

    [Task]
    bool ShootPattern1()
    {
        for (int i = 1; i <= 8; ++i)
        {
            Shoot(DirFromAngle(45.0f * i));
        }

        return true;
    }

    [Task]
    bool StopMovement()
    {
        isWalking = false;
        isDashing = false;
        return true;
    }

    [Task]
    bool ChangeFireSpeed(float speed)
    {
        fireSpeed = speed;
        return true;
    }

    [Task]
    void StartLaser()
    {
        //do something so its obvious to the player that the boss is going to shoot
        if (Task.current.isStarting)
        {
            laserDashCoroutine = false;
            isDashing = false;
            isWalking = false;
            float delay = 0.5f;
            StartCoroutine(StartLaserParticles(delay));
        }

        if (laserDashCoroutine)
            Task.current.Succeed();
    }

    IEnumerator StartLaserParticles(float delay)
    {
        laserParticles.SetActive(true);
        yield return new WaitForSeconds(delay);
        laserDashCoroutine = true;
        float waitTime = 1.5f - delay;
        if(waitTime > 0.0f)
            yield return new WaitForSeconds(waitTime);

        laserParticles.SetActive(false);
    }

    [Task]
    bool ShootLaser()
    {
        int layerMask = LayerMask.GetMask("Player");
        RaycastHit hit;
        //no need to do spherecastall, just a normal spherecast only checking the layer on which the player is is better
        if(Physics.SphereCast(transform.position, laserWidth, direction, out hit, laserLength, layerMask))
        {
            Debug.Log("Boss hit player with laser");
            player.GetComponent<Health>().Damage(50.0f);
        }


        _lineRenderer.enabled = true;
        _lineRenderer.SetPosition(0, transform.position);
        _lineRenderer.SetPosition(1, transform.position + direction * laserLength);
        StartCoroutine(DisableLaser(0.1f));
        return true;
    }

    IEnumerator DisableLaser(float delay)
    {
        yield return new WaitForSeconds(delay);
        _lineRenderer.enabled = false;
    }

    [Task]
    bool ShootPattern2()
    {
        for(int i = 1; i <= 8; ++i)
        {
            Shoot(DirFromAngle(45.0f * i -22.5f ));
        }
        return true;
    }

    IEnumerator Shooting(float startAngle, float endAngle, float interval, float wait, int index)
    {
        if(interval > 0)
        for (float i = startAngle; i < endAngle; i += interval)
        {
            Debug.Log(index);
            Shoot(DirFromAngle(i));
            yield return new WaitForSeconds(wait);
        }
        else
        {
            for (float i = startAngle; i > endAngle; i += interval)
            {
                Debug.Log(index);

                Shoot(DirFromAngle(i));
                yield return new WaitForSeconds(wait);
            }

        }
        inCoroutine[index] = false;
    }

    [Task]
    void ShootPatternFromTo_Angle(float startAngle, float endAngle, float interval, float timeBetweenBullets, int index)
    {
        if (Task.current.isStarting)
        {
            inCoroutine[index] = true;
            StartCoroutine(Shooting(startAngle, endAngle, interval, timeBetweenBullets, index));
        }
        if (!inCoroutine[index])
        {
            Task.current.Succeed();
        }
    }

    Vector3 DirFromAngle(float angle)
    {
        return new Vector3(Mathf.Sin(angle * Mathf.Deg2Rad), 0, Mathf.Cos(angle * Mathf.Deg2Rad));
    }

    [Task]
    bool Shoot(Vector3 direction)
    {
        GameObject projectile = _objectPooler.SpawnFromPool("projectiles");
        ProjectileBehaviour projectileBehaviour = projectile.GetComponent<ProjectileBehaviour>();
        projectileBehaviour.SetProjectile(transform.position - new Vector3(0,2,0), direction, fireSpeed, 800.0f, damage, gameObject);
        return true;
    }

    private void FixedUpdate()
    {
        //_rigidBody.velocity = velocity;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Wall"))
        {
            StopDashing();
        }
        else if(collision.gameObject.CompareTag("Player"))
        {
            if (Time.time - lastCollisionTime > 0.1f)
            {
                lastCollisionTime = Time.time;
                //get the direction of the collision
                Vector3 dir = collision.contacts[0].point - new Vector3(transform.position.x, collision.contacts[0].point.y, transform.position.z);
                dir.Normalize();

                //push player away from the boss with force
                float force = 200.0f;
                player.GetComponent<ImpactBehaviour>().AddImpact(dir, force);
                player.GetComponent<Health>().Damage(50.0f);
                StopDashing();
            }
        }
    }
}
