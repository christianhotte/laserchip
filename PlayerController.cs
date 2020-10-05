using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    //Description: Manages player input, movement, laser, and animation

    //CLASSES, STRUCTS & ENUMS:
    internal class LaserSeg
    {
        //Description: Contains data about a single segment of active laser

        //Laser Positional Data:
        internal Vector2 laserPosition; //The position of this segment
        internal float laserAngle; //The angle of this segment (for sprite, signage is irrelevant)

        //Laser Properties:
        internal Color laserColor; //The color of this segment
        internal int laserSize; //The size of this segment
        internal bool laserPoint; //Whether or not this laser is a joint in the laser path
    }

    //OBJECTS & COMPONENTS:
    private Transform tr;  //This player's transform component
    private Transform sprTr; //This player's sprite's transform component
    private Rigidbody2D rb; //This player's rigidbody component
    private Animator anim; //This player's animator controller component

    public LEDLaserSourceController laserSourceCont; //This player's laser source controller component
    private List<GameObject> activeLaser = new List<GameObject>(); //This player's entire active laser path, if it has one

    //INPUT VARIABLES:
    private Vector2 movementInput; //This player's movement axes
    private Vector2 laserInput; //This player's laser input axes
    public bool triggeringLaser; //Whether or not this player is currently triggering their laser

    //MOVEMENT VARIABLES:
    [Header("Movement Vars:")]
    [SerializeField] private Vector2 velocity; //This player's current velocity (in units per second)
    [SerializeField] private float rotVelocity; //This player's current angular velocity (in units per second)
    [Space()]
    public float maxMoveSpeed; //Player's maximum move speed (in units per second)
    [Range(0, 1)] public float accelPower; //How quickly player can accelerate to target speed (0 being never, 1 being instantaneous)
    [Range(0, 1)] public float brakePower; //How quickly player can go from moving to standstill (0 being no brake, 1 being instantaneous)
    [Range(0, 1)] public float speedSnapThreshold; //Velocity magnitude at which player will snap to full speed when moving (lower values mean finer accuracy)
    [Range(0, 1)] public float stopSnapThreshold;  //Velocity magnitude at which player will snap to a complete halt when braking (lower values mean finer accuracy)
    [Space()]

    //LASER VARIABLES:
    [Header("Laser Vars:")]
    public float laserRange; //How far laser travels before stopping (failsafe)
    public AnimationCurve laserIntensityCurve; //Curve representing transparency of laser based on intensity
    [SerializeField] private float laserIntensity; //How powerful laser currently is (affects coloration and refraction properties)
    [SerializeField] private int laserSize; //How wide laser currently is (0-4)
    internal Color laserColor; //What color this player's laser is
    public int maxLaserSegments; //Limiter on laser segment number to prevent infinite while loops
    private bool reverseLaser; //Used to animate laser strobing

    //RESOURCE VARIABLES:
    [Header("Resource Vars:")]
    [SerializeField] internal int health; //How much health this player has left
    [SerializeField] internal int charge; //How much charge this player currently has

    //VISUALIZATION VARIABLES:
    [Header("Visualization Vars:")]
    public GameObject laserPrefab; //Laser segment sprite prefab gameobject
    public float laserDensity; //How many laser segments are spawned (per unit of distance)
    public int laserPulseLength; //How long laser waits in between pulses
    private int pulsesSinceLaserReverse; //How long it has been since laser reversed
    public float spriteRotMaxSpeed; //Maximum speed sprite will rotate at (in degrees per second) (factored into animation curve value)
    [Range(0, 1)] public float spriteRotAccel; //How quickly player sprite will rotate to desired direction
    [Range(0, 1)] public float spriteRotBrake; //How quickly player sprite will stop rotating

    private void Start()
    {
       //Get Objects & Components:
        tr = transform; //Get player transform
        anim = tr.GetComponentInChildren<Animator>(); //Get player animator
        sprTr = anim.transform; //Get transform of sprite child object
        rb = GetComponent<Rigidbody2D>(); //Get player rigidbody
    }

    //INPUT DETECTION:
    public void OnMove(InputValue value) //Called when change in move input is detected
    {
       //Record Input:
        movementInput = value.Get<Vector2>(); //Get movement vector from player controller
       //Parse Results:
        if (movementInput != Vector2.zero) anim.SetBool("Walking", true); //If player is moving, start up walking animation
        else anim.SetBool("Walking", false); //If player is not moving, end walking animation
    }

    public void OnLaser(InputValue value) //Called when change in laser input is detected
    {
       //Record Input:
        laserInput = value.Get<Vector2>(); //Get laser vector from player controller
       //Parse Results:
        if (laserInput != Vector2.zero) triggeringLaser = true; //If input is detected, turn laser on
        else triggeringLaser = false; //Otherwise, don't
    }

    private void Update()
    {
        UpdateLaser(); //Check and update laser status
    }
    private void FixedUpdate()
    {
        ProcessMovement(); //Process player movement
    }

    //--|CORE FUNCTIONS|----------------------------------------------------------------------
    private void ProcessMovement()
    {
        //Description: Checks player movement variables and generates movement properties in real-time

       //Get Velocity:
        if (movementInput != Vector2.zero) //If movement input is being detected...
        {
            Vector2 targetVel = movementInput * maxMoveSpeed; //Get target velocity variable to use in lerping and speed check operations
            velocity = Vector2.Lerp(velocity, targetVel, accelPower); //Change player velocity based on input direction and acceleration power
            if (velocity.sqrMagnitude > targetVel.sqrMagnitude * (1 - speedSnapThreshold)) //If player has exceeded full speed snap threshold
            {
                velocity = targetVel; //Snap velocity to designated target (once close enough)
            }
        }
        else if (velocity != Vector2.zero) //If no movement input is being detected but player is moving...
        {
            velocity = Vector2.Lerp(velocity, Vector2.zero, brakePower); //Slow player velocity based on braking power
            if (velocity.magnitude < stopSnapThreshold) { velocity = Vector2.zero; } //Snap player to a halt if stop snapping thresh has been reached
        }

        //Rotate Player Sprite:
        if (velocity != Vector2.zero && movementInput != Vector2.zero) //If player is moving and providing input...
        {
            rotVelocity = Mathf.Lerp(rotVelocity, spriteRotMaxSpeed, spriteRotAccel); //Get rotational velocity from sprite rotation curve
        }
        else //Otherwise...
        {
            rotVelocity = Mathf.Lerp(rotVelocity, 0, spriteRotBrake); //Zero out rotational velocity
        }

       //Move Player:
        if (velocity != Vector2.zero) //If player has any velocity...
        {
           //Player Position:
            rb.velocity = velocity * Time.deltaTime; //Feed computed velocity to rigidbody
           //Player Rotation:
            //Find Rotation Angle:
            float targetLookAngle = Vector2.SignedAngle(Vector2.up, velocity); //Get angle of movement input
            float actualLookAngle = Mathf.LerpAngle(sprTr.localRotation.z, targetLookAngle, (rotVelocity * spriteRotMaxSpeed)); //Get lerped rotation of sprite
            //Set Rotation Angle:
            sprTr.rotation = Quaternion.Euler(0, 0, actualLookAngle); //Commit new rotation
        }

    }
    private void UpdateLaser()
    {
        //Description: Handles activation, physics, and resolution of laser system

       //LASER PRE-CLEANUP:
        pulsesSinceLaserReverse++; //Increment laser pulse tracker
        if (pulsesSinceLaserReverse >= laserPulseLength) //If laser has reached or exceeded pulse length...
        {
            pulsesSinceLaserReverse = 0; //Scrub pulse tracker
            reverseLaser = !reverseLaser; //Invert laser reversal state
        }
        CleanupActiveLaser(); //Perform laser cleanup every update

       //LASER TRIGGER CHECKS:
        if (triggeringLaser) //If player is attempting to trigger laser...
        {
            laserSourceCont.ToggleLaser(true); //Enable laser
        }
        else //If laser is not being triggered or is not able to be triggered...
        {
            laserSourceCont.ToggleLaser(false); //Disable laser
            return; //Skip rest of function
        }
            
       //SEQUENCE LASER PATH:
        //Initializations:
        float rangeRemaining = laserRange; //Create variable to track how much range laser has left
        List<Vector2> pathWaypoints = new List<Vector2>(); //Initialize list to store laser path waypoints in
        List<LaserSeg> segmentList = new List<LaserSeg>(); //Initialize list of laserSeg classes to track laser properties
        
        float entryAngle = Vector2.SignedAngle(Vector2.up, laserInput); //Get initial entry angle for laser travel path
        Color entryColor = laserColor; //Get laser starting intensity
        int entrySize = laserSize; //Get laser starting size
        float entryIntensity = laserIntensity; //Get laser intensity abstract number for modification
        
        Rigidbody2D lastHitObject = null; //The last rigidbody hit, prevents laser from hitting same object multiple times in a row
        int lastHitObjectOrigLayer = 0; //Initialize variable for storing original layer of last hit object
        
        //Additional Prep Work:
        pathWaypoints.Add(transform.position); //Add position as first waypoint
        GetComponent<CircleCollider2D>().enabled = false; //Temporarily disable collision on player
        sprTr.GetComponent<BoxCollider2D>().enabled = false; //Temporarily disable collsion on sprite
        entryColor.a = laserIntensityCurve.Evaluate(entryIntensity); //Correct starting color based on starting intensity

        //Main Laser Sequencer Loop:
        while (rangeRemaining > 0) //While laser has range remaining (loop is generally meant to be broken internally)
        {
           //Validation:
            if (pathWaypoints.Count > maxLaserSegments) break; //Emergency loop break
           //Assemble Linecast Data:
            Vector2 startVector = pathWaypoints[pathWaypoints.Count - 1]; //Get start vector from most recent waypoint
            Vector2 distanceVector = rangeRemaining * Vector2.up; //Get 0 degree vector representative of maximum possible length of segment
            Vector2 endVector = distanceVector.Rotate(entryAngle); //Rotate distance vector based on entry angle
            endVector += startVector; //Offset end vector by start vector
           //Perform Linecast:
            RaycastHit2D castHit = Physics2D.Linecast(startVector, endVector); //Execute linecast
           //Check Results:
            if (castHit && castHit.transform.GetComponent<LaserSurfaceController>() != null) //If linecast hit something...
            {
               //Record Hit Data:
                pathWaypoints.Add(castHit.point); //Add the point of impact to path
                rangeRemaining -= Mathf.Abs(castHit.distance); //Subtract cast distance from total remaining range
               //Populate Segment List:
                int newSegmentAmount = Mathf.RoundToInt(castHit.distance * laserDensity); //Get number of lasers in new portion
                for (int x = 0; x < newSegmentAmount; x++) //Create number of new laser segments equal to found amount
                {
                    LaserSeg newSegment = new LaserSeg(); //Create new laser segment object
                    newSegment.laserPosition = Vector2.Lerp(startVector, castHit.point, (x * 1.0f) / (newSegmentAmount * 1.0f)); //Find segment position, segments will be evenly-spaced along cast line (pseudo-cast ints to floats)
                    newSegment.laserAngle = entryAngle; //Assign entry angle data
                    newSegment.laserColor = entryColor; //Assign entry color data
                    newSegment.laserSize = entrySize;   //Assign entry size data
                    segmentList.Add(newSegment); //Add new segment to list
                }
                segmentList[segmentList.Count - 1].laserPoint = true; //Set final segment in list to be a point
               //Modify Laser Properties Based on Surface:
                LaserSurface surface = castHit.transform.GetComponent<LaserSurfaceController>().surfaceType; //Get laser surface data from hit object
                if (surface.intensifiesLaser != 0) //If surface intensifies laser...
                {
                    entryIntensity = Mathf.Clamp(entryIntensity * surface.intensifiesLaser, 0, 1); //Get new intensity value
                    entryColor.a = laserIntensityCurve.Evaluate(entryIntensity); //Get new laser color based on intensity curve
                }
                if (surface.magnifiesLaser != 0) //If surface magnifies laser...
                {
                    entrySize = Mathf.Clamp(entrySize + surface.magnifiesLaser, 0, 4); //Get new entry size and clamp within size range
                }
                switch (surface.reflectionProperties) //Reflection Property Definitions:
                {
                    case LaserSurface.ReflectionType.termination: //Surface terminates laser path
                        rangeRemaining = 0; //Cancel any remaining range
                        break;
                    case LaserSurface.ReflectionType.simpleReflection: //Surface fully reflects laser
                        Vector2 entryVector = Vector2.up.Rotate(entryAngle); //Get incoming angle vector
                        Vector2 exitVector = Vector2.Reflect(entryVector, castHit.normal); //Reflect entry vector across normal
                        entryAngle = Vector2.SignedAngle(Vector2.up, exitVector); //Compute new entry angle
                        break;
                    case LaserSurface.ReflectionType.angleRefraction: //Surface refracts laser at angle
                        entryAngle += surface.refractionAngle; //Add refraction angle to entry
                        break;
                }
            }
            else //If cast hit nothing...
            {
               //Record Line Data:
                pathWaypoints.Add(endVector); //Add final vector to waypoint list
                rangeRemaining = 0; //Confirm that laser must have run out of range
               //Populate Segment List:
                int newSegmentAmount = Mathf.RoundToInt(Vector2.Distance(startVector, endVector) * laserDensity);
                for (int x = 0; x < newSegmentAmount; x++) //Create number of new laser segments equal to found amount
                {
                    LaserSeg newSegment = new LaserSeg(); //Create new laser segment object
                    newSegment.laserPosition = Vector2.Lerp(startVector, endVector, (x*1.0f) / (newSegmentAmount*1.0f)); //Find segment position, segments will be evenly-spaced along cast line (pseudo-cast ints to floats)
                    newSegment.laserAngle = entryAngle; //Assign entry angle data
                    newSegment.laserColor = entryColor; //Assign entry color data
                    newSegment.laserSize = entrySize;   //Assign entry size data
                    segmentList.Add(newSegment); //Add new segment to list
                }
                segmentList[segmentList.Count - 1].laserPoint = true; //Set final segment in list to be a point
            }
            //Recursive Cast Hit Avoidance Protocol:
            if (lastHitObject != null) lastHitObject.gameObject.layer = lastHitObjectOrigLayer; //Move previous last-hit object back to original layer
            if (castHit && castHit.transform.GetComponent<LaserSurfaceController>() != null) //Log additional lastHit object if laser is continuing
            {
                lastHitObject = castHit.rigidbody; //Get new last-hit body
                lastHitObjectOrigLayer = lastHitObject.gameObject.layer; //Get last-hit body's original layer
                lastHitObject.gameObject.layer = 2; //Temporarily move object to ignoreraycast layer
            }
        }

        //RE-ENABLE COLLISION:
        if (lastHitObject != null) lastHitObject.gameObject.layer = lastHitObjectOrigLayer; //Move previous last-hit object back to original layer
        GetComponent<CircleCollider2D>().enabled = true; //Re-enable collision on player
        sprTr.GetComponent<BoxCollider2D>().enabled = true; //Re-enable collsion on sprite

        //DRAW LASER PATH:
        for (int x = 0; x < segmentList.Count; x++) //Parse through each segment created by lasercast
        {
           //Initializations:
            LaserSeg thisSeg = segmentList[x]; //Get segment data from list
            GameObject newLaser = Instantiate(laserPrefab); //Instantiate a new laser segment prefab
            SpriteRenderer laserSprite = newLaser.GetComponent<SpriteRenderer>(); //Get sprite renderer from segment
            Animator laserAnimator = newLaser.GetComponent<Animator>(); //Get animator from segment
           //Positioning:
            newLaser.transform.position = thisSeg.laserPosition; //Set laser position
            newLaser.transform.rotation = Quaternion.Euler(0, 0, thisSeg.laserAngle); //Set laser rotation
            if (reverseLaser) newLaser.transform.localScale = new Vector2(-newLaser.transform.localScale.x, newLaser.transform.localScale.y); //Flip laser sprite if reversing
           //Laser Details:
            laserSprite.color = thisSeg.laserColor; //Set laser color
            laserAnimator.SetInteger("Power", thisSeg.laserSize); //Set laser size
           //Cleanup:
            activeLaser.Add(newLaser); //Add segment to active laser list
        }
    }

//--|UTILITY FUNCTIONS|----------------------------------------------------------------------
    public void CleanupActiveLaser()
    {
        //Description: Wipes active laser objects when called

        if (activeLaser.Count != 0) //If player currently has an active laser...
        {
            for (int x = 0; x < activeLaser.Count; x++) //Parse through all active laser objects...
            {
                Destroy(activeLaser[x]);
            }
        }
    }
}
