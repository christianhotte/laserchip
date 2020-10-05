using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LEDLaserSourceController : MonoBehaviour
{
    //Description: Contains all necessary functionality for operating LED Laser source object

    //OBJECTS & COMPONENTS:
    private SpriteRenderer spr; //This object's sprite renderer
    private Transform tr; //This object's transform component

    [Header("Laser Settings:")]
    public float spinSpeed; //Determines how fast laser source spins when on

    //LASER PROPERTIES:
    private bool laserOn; //Whether or not this laser is on
    private float currentLaserRot; //What position laser is currently in

    //--|MAIN METHODS|----------------------------------------

    private void Start()
    {
        //Get Objects & Components:
        spr = GetComponent<SpriteRenderer>(); //Get laser sprite renderer component
        tr = transform; //Get laser transform component
    }
    private void Update()
    {
        //Rotate Laser Source:
        if (laserOn) //If laser is activated...
        {
            currentLaserRot += spinSpeed * Time.deltaTime; //Add spin speed to rotational position
            while (currentLaserRot > 360) currentLaserRot -= 360; //Correct for potential overflow
            tr.localRotation = Quaternion.Euler(0, 0, currentLaserRot); //Apply new rotation to transform
        }
    }

    //--|COMMANDS|------------------------------------------
    public void ToggleLaser(bool on)
    {
        //Description: Toggles laser on or off

        if (on && !laserOn) //Toggle laser ON:
        {
            spr.enabled = true; //Show laser
            laserOn = true; //Activate laser
        }
        else if (!on && laserOn) //Toggle laser OFF:
        {
            spr.enabled = false; //Hide laser
            laserOn = false; //Deactivate laser
        }
    }
}
