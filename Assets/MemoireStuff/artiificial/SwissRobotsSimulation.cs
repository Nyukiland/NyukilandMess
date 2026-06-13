using UnityEngine;
using System.Collections;

public class SwissRobotsSimulation : MonoBehaviour
{
    [Header("Environment Settings")]
    public float arenaSize = 25f;
    public int puckCount = 60;
    public int robotCount = 6;

    private void Start()
    {
        SetupCamera();
        SetupArena();
        SetupPucks();
        SetupRobots();
    }

    private void SetupCamera()
    {
        Camera cam = Camera.main;
        if (cam == null) cam = new GameObject("Main Camera").AddComponent<Camera>();

        cam.transform.position = new Vector3(0, arenaSize * 0.8f, 0);
        cam.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        cam.orthographic = true;
        cam.orthographicSize = arenaSize * 0.6f;
    }

    private void SetupArena()
    {
        GameObject arena = new GameObject("Arena");

        // Floor
        Transform floor = GameObject.CreatePrimitive(PrimitiveType.Plane).transform;
        floor.SetParent(arena.transform);
        floor.localScale = new Vector3(arenaSize * 0.1f, 1, arenaSize * 0.1f);
        floor.position = Vector3.zero;

        // Walls
        float wallThickness = 1f;
        float halfSize = arenaSize / 2f;

        Vector3[] wallPositions = {
            new Vector3(0, 0.5f, halfSize),   // Top
            new Vector3(0, 0.5f, -halfSize),  // Bottom
            new Vector3(halfSize, 0.5f, 0),   // Right
            new Vector3(-halfSize, 0.5f, 0)   // Left
        };

        Vector3[] wallScales = {
            new Vector3(arenaSize + wallThickness, 1, wallThickness),
            new Vector3(arenaSize + wallThickness, 1, wallThickness),
            new Vector3(wallThickness, 1, arenaSize + wallThickness),
            new Vector3(wallThickness, 1, arenaSize + wallThickness)
        };

        for (int i = 0; i < 4; i++)
        {
            Transform wall = GameObject.CreatePrimitive(PrimitiveType.Cube).transform;
            wall.SetParent(arena.transform);
            wall.position = wallPositions[i];
            wall.localScale = wallScales[i];
            wall.GetComponent<MeshRenderer>().material.color = Color.black;
        }
    }

    private void SetupPucks()
    {
        GameObject puckContainer = new GameObject("Pucks");
        Material puckMat = new Material(Shader.Find("Standard")) { color = Color.blue };

        for (int i = 0; i < puckCount; i++)
        {
            Vector3 spawnPos = new Vector3(
                Random.Range(-arenaSize * 0.4f, arenaSize * 0.4f),
                0.25f,
                Random.Range(-arenaSize * 0.4f, arenaSize * 0.4f)
            );

            GameObject puck = GameObject.CreatePrimitive(PrimitiveType.Cube);
            puck.name = "Puck";
            puck.transform.position = spawnPos;
            puck.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
            puck.transform.SetParent(puckContainer.transform);
            puck.GetComponent<MeshRenderer>().material = puckMat;

            Rigidbody rb = puck.AddComponent<Rigidbody>();
            rb.mass = 1f;
            rb.linearDamping = 5f; // High drag simulates floor friction so they don't slide forever
            rb.angularDamping = 5f;
            rb.constraints = RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        }
    }

    private void SetupRobots()
    {
        GameObject robotContainer = new GameObject("Robots");
        Material robotMat = new Material(Shader.Find("Standard")) { color = Color.red };

        for (int i = 0; i < robotCount; i++)
        {
            Vector3 spawnPos = new Vector3(
                Random.Range(-arenaSize * 0.4f, arenaSize * 0.4f),
                0.5f,
                Random.Range(-arenaSize * 0.4f, arenaSize * 0.4f)
            );

            // Main Body
            GameObject robot = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            robot.name = "SwissRobot";
            robot.transform.position = spawnPos;
            robot.transform.localScale = new Vector3(1f, 0.5f, 1f);
            robot.transform.SetParent(robotContainer.transform);
            robot.GetComponent<MeshRenderer>().material = robotMat;

            // Create U-Shaped Scoop Prongs
            Transform bumperCenter = GameObject.CreatePrimitive(PrimitiveType.Cube).transform;
            bumperCenter.SetParent(robot.transform);
            bumperCenter.localPosition = new Vector3(0, 0, 0.5f);
            bumperCenter.localScale = new Vector3(0.8f, 0.8f, 0.2f);
            bumperCenter.GetComponent<MeshRenderer>().material = robotMat;

            Vector3[] armOffsets = { new Vector3(-0.4f, 0, 0.6f), new Vector3(0.4f, 0, 0.6f) };
            foreach (Vector3 offset in armOffsets)
            {
                Transform arm = GameObject.CreatePrimitive(PrimitiveType.Cube).transform;
                arm.SetParent(robot.transform);
                arm.localPosition = offset;
                arm.localScale = new Vector3(0.2f, 0.8f, 0.4f);
                arm.GetComponent<MeshRenderer>().material = robotMat;
            }

            robot.transform.rotation = Quaternion.Euler(0, Random.Range(0f, 360f), 0);
            
            // Physics Setup
            Rigidbody rb = robot.AddComponent<Rigidbody>();
            rb.mass = 15f; // Heavy enough to push 1-2 pucks, but easily stalled by a cluster
            rb.linearDamping = 2f;
            rb.constraints = RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

            robot.AddComponent<SwissRobotBehavior>();
        }
    }
}

public class SwissRobotBehavior : MonoBehaviour
{
    private Rigidbody rb;
    private bool isAvoiding = false;
    private float stallTimer = 0f;

    [Header("Motor Settings")]
    public float motorForce = 250f;
    public float stallSpeedThreshold = 1.0f; // Speed under which the robot is considered "stuck"

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void FixedUpdate()
    {
        if (isAvoiding) return;

        // Apply continuous forward motor force
        rb.AddForce(transform.forward * motorForce, ForceMode.Force);

        // Embodied Sensor: Check if the motor is stalled
        if (rb.linearVelocity.sqrMagnitude < stallSpeedThreshold)
        {
            stallTimer += Time.fixedDeltaTime;
            
            // If stalled against an obstacle for 0.4 seconds, trigger avoidance
            if (stallTimer > 0.4f)
            {
                StartCoroutine(AvoidanceRoutine());
            }
        }
        else
        {
            stallTimer = 0f; // Reset timer if moving freely
        }
    }

    private IEnumerator AvoidanceRoutine()
    {
        isAvoiding = true;
        stallTimer = 0f;

        // 1. Back up rapidly
        rb.linearVelocity = Vector3.zero;
        rb.AddForce(-transform.forward * motorForce * 0.8f, ForceMode.Impulse);
        yield return new WaitForSeconds(0.5f);

        // 2. Stop and turn a random angle
        rb.linearVelocity = Vector3.zero;
        float turnAngle = Random.Range(100f, 160f); // Turn wide enough to face away from the cluster
        if (Random.value > 0.5f) turnAngle = -turnAngle; // Randomize Left or Right
        
        transform.Rotate(0, turnAngle, 0);

        // 3. Brief pause before engaging the forward motor again
        yield return new WaitForSeconds(0.1f);
        isAvoiding = false;
    }
}