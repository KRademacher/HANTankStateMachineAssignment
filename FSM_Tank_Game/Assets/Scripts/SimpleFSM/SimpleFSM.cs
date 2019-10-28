using UnityEngine;
using System.Collections;

public class SimpleFSM : FSM 
{
    public enum FSMState
    {
        None,
        Patrol,
        Chase,
        Attack,
        Dead,
        Flee,
        Evade
    }

    //Current state that the NPC is reaching
    public FSMState curState;

    //Speed of the tank
    private float curSpeed;

    //Tank Rotation Speed
    private float curRotSpeed;

    //Bullet
    public GameObject Bullet;

    //Whether the NPC is destroyed or not
    private bool bDead;
    private int health;

    private int fleeHealth;

    private RaycastHit hit;


    //Initialize the Finite state machine for the NPC tank
	protected override void Initialize () 
    {
        curState = FSMState.Patrol;
        curSpeed = 150.0f;
        curRotSpeed = 2.0f;
        bDead = false;
        elapsedTime = 0.0f;
        shootRate = 3.0f;
        health = 100;
        fleeHealth = 75;

        //Get the list of points
        pointList = GameObject.FindGameObjectsWithTag("WandarPoint");

        //Set Random destination point first
        FindNextPoint();

        //Get the target enemy(Player)
        GameObject objPlayer = GameObject.FindGameObjectWithTag("Player");
        playerTransform = objPlayer.transform;

        if(!playerTransform)
            print("Player doesn't exist.. Please add one with Tag named 'Player'");

        //Get the turret of the tank
        turret = gameObject.transform.GetChild(0).transform;
        bulletSpawnPoint = turret.GetChild(0).transform;
	}

    //Update each frame
    protected override void FSMUpdate()
    {
        switch (curState)
        {
            case FSMState.Patrol: UpdatePatrolState(); break;
            case FSMState.Chase: UpdateChaseState(); break;
            case FSMState.Attack: UpdateAttackState(); break;
            case FSMState.Dead: UpdateDeadState(); break;
            case FSMState.Flee: UpdateFleeState(); break;
            case FSMState.Evade: UpdateEvadeState(hit); break;
        }

        //Update the time
        elapsedTime += Time.deltaTime;

        //Go to dead state is no health left
        if (health <= 0)
            curState = FSMState.Dead;
    }

    /// <summary>
    /// Patrol state
    /// </summary>
    protected void UpdatePatrolState()
    {
        //Find another random patrol point if the current point is reached
        if (Vector3.Distance(transform.position, destPos) <= 100.0f)
        {
            print("Reached to the destination point\ncalculating the next point");
            FindNextPoint();
        }

        //Evade other NPC tank if ray hits.
        if (IsNearOtherNpc())
        {
            curState = FSMState.Evade;
        }

        //Check the distance with player tank
        //When the distance is near, transition to chase state
        //Unless when health is low, then flee.
        else if (Vector3.Distance(transform.position, playerTransform.position) <= 300.0f)
        {
            if (health < fleeHealth)
            {
                print("Switch to Flee State");
                curState = FSMState.Flee;
            }
            else
            {
                print("Switch to Chase Position");
                curState = FSMState.Chase;
            }
        }

        //Rotate to the target point
        Quaternion targetRotation = Quaternion.LookRotation(destPos - transform.position);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * curRotSpeed);  

        //Go Forward
        transform.Translate(Vector3.forward * Time.deltaTime * curSpeed);
    }

    /// <summary>
    /// Chase state
    /// </summary>
    protected void UpdateChaseState()
    {
        //Set the target position as the player position
        destPos = playerTransform.position;

        //Check the distance with player tank
        //When the distance is near, transition to attack state
        float dist = Vector3.Distance(transform.position, playerTransform.position);
        if (dist <= 200.0f)
        {
            curState = FSMState.Attack;
        }
        //Go back to patrol is it become too far
        else if (dist >= 300.0f)
        {
            curState = FSMState.Patrol;
        }

        //Go Forward
        transform.Translate(Vector3.forward * Time.deltaTime * curSpeed);
    }

    /// <summary>
    /// Attack state
    /// </summary>
    protected void UpdateAttackState()
    {
        //Set the target position as the player position
        destPos = playerTransform.position;

        //Check the distance with the player tank
        float dist = Vector3.Distance(transform.position, playerTransform.position);
        if (dist >= 200.0f && dist < 300.0f)
        {
            //Rotate to the target point
            Quaternion targetRotation = Quaternion.LookRotation(destPos - transform.position);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * curRotSpeed);  

            //Go Forward
            transform.Translate(Vector3.forward * Time.deltaTime * curSpeed);

            curState = FSMState.Attack;
        }
        //Transition to patrol is the tank become too far
        else if (dist >= 300.0f)
        {
            curState = FSMState.Patrol;
        }

        //Always Turn the turret towards the player
        Quaternion turretRotation = Quaternion.LookRotation(destPos - turret.position);
        turret.rotation = Quaternion.Slerp(turret.rotation, turretRotation, Time.deltaTime * curRotSpeed); 

        //Shoot the bullets
        ShootBullet();

        //Flee if health is below certain point.
        if (health < fleeHealth)
        {
            curState = FSMState.Flee;
        }
    }

    /// <summary>
    /// Flee State
    /// </summary>
    private void UpdateFleeState()
    {
        //Get the distance between player and tank. If far away, switch back to patrol.
        float distance = Vector3.Distance(transform.position, playerTransform.position);
        if (distance >= 500f)
        {
            curState = FSMState.Patrol;
        }
        //Else, rotate away from player and move away.
        Quaternion fleeRotation = Quaternion.LookRotation(transform.position - playerTransform.position);
        transform.rotation = Quaternion.Slerp(transform.rotation, fleeRotation, Time.deltaTime * curRotSpeed);
        transform.Translate(Vector3.forward * Time.deltaTime * curSpeed);
    }

    /// <summary>
    /// Evade State
    /// </summary>
    private void UpdateEvadeState(RaycastHit hit)
    {
        Transform other = hit.transform;
        float distance = Vector3.Distance(transform.position, other.position);
        if (distance >= 200f)
        {
            curState = FSMState.Patrol;
        }
        Quaternion evadeRotation = Quaternion.LookRotation(transform.position - other.position);
        transform.rotation = Quaternion.Slerp(transform.rotation, evadeRotation, Time.deltaTime * curRotSpeed);
        transform.Translate(Vector3.forward * Time.deltaTime * curSpeed);
    }

    /// <summary>
    /// Dead state
    /// </summary>
    protected void UpdateDeadState()
    {
        //Show the dead animation with some physics effects
        if (!bDead)
        {
            bDead = true;
            Explode();
        }
    }

    /// <summary>
    /// Shoot the bullet from the turret
    /// </summary>
    private void ShootBullet()
    {
        if (elapsedTime >= shootRate)
        {
            //Shoot the bullet
            Instantiate(Bullet, bulletSpawnPoint.position, bulletSpawnPoint.rotation);
            elapsedTime = 0.0f;
        }
    }

    /// <summary>
    /// Check the collision with the bullet
    /// </summary>
    /// <param name="collision"></param>
    void OnCollisionEnter(Collision collision)
    {
        //Reduce health
        if(collision.gameObject.tag == "Bullet")
            health -= collision.gameObject.GetComponent<Bullet>().damage;
    }   

    /// <summary>
    /// Find the next semi-random patrol point
    /// </summary>
    protected void FindNextPoint()
    {
        print("Finding next point");
        int rndIndex = Random.Range(0, pointList.Length);
        float rndRadius = 10.0f;
        
        Vector3 rndPosition = Vector3.zero;
        destPos = pointList[rndIndex].transform.position + rndPosition;

        //Check Range
        //Prevent to decide the random point as the same as before
        if (IsInCurrentRange(destPos))
        {
            rndPosition = new Vector3(Random.Range(-rndRadius, rndRadius), 0.0f, Random.Range(-rndRadius, rndRadius));
            destPos = pointList[rndIndex].transform.position + rndPosition;
        }
    }

    /// <summary>
    /// Check whether the next random position is the same as current tank position
    /// </summary>
    /// <param name="pos">position to check</param>
    protected bool IsInCurrentRange(Vector3 pos)
    {
        float xPos = Mathf.Abs(pos.x - transform.position.x);
        float zPos = Mathf.Abs(pos.z - transform.position.z);

        if (xPos <= 50 && zPos <= 50)
            return true;

        return false;
    }

    /// <summary>
    /// Checks if tank is near other npc tank.
    /// </summary>
    private bool IsNearOtherNpc()
    {
        //Cast Box to detect other NPC Tanks
        bool hitDetect = Physics.BoxCast(transform.position + transform.forward * 25f, transform.localScale * 25f, 
            transform.forward, out hit, transform.rotation, 75f);

        if (hitDetect)
        {
            if (hit.collider.tag != "Player")
            {
                return true;
            }
        }
        return false;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(transform.position + transform.forward * 25f, transform.localScale * 25f);
    }

    protected void Explode()
    {
        float rndX = Random.Range(10.0f, 30.0f);
        float rndZ = Random.Range(10.0f, 30.0f);
        for (int i = 0; i < 3; i++)
        {
            GetComponent<Rigidbody>().AddExplosionForce(10000.0f, transform.position - new Vector3(rndX, 10.0f, rndZ), 40.0f, 10.0f);
            GetComponent<Rigidbody>().velocity = transform.TransformDirection(new Vector3(rndX, 20.0f, rndZ));
        }

        Destroy(gameObject, 1.5f);
    }
}