using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Users;

public class SpawnMaster : MonoBehaviour
{
    //Description: Spawns player objects when input is detected, manages player data as needed upon spawn

    //CLASSES, STRUCTS & ENUMS:
    [System.Serializable] public class PlayerTicket
    {
        //Description: Contains all necessary data to spawn a single player

        public string name; //This player's name designation
        [Header("Player Settings:")]
        public Color playerColor; //The color this player will be marked as
        public Vector2 spawnPosition; //The location where player will spawn
        public float spawnRotation; //The angle player will spawn in at
    }

    //OBJECTS & COMPONENTS:
    public List<PlayerTicket> playerTickets = new List<PlayerTicket>(); //List of player data, count is number of players who can join at once
    private List<GameObject> players = new List<GameObject>(); //List of all currently-spawned players
    [Space()]
    public List<InputActionAsset> keyboardControlSchemes = new List<InputActionAsset>(); //List of alternate control schemes on keyboard, used to handle multiple players on one keyboard
    public InputActionAsset controllerControlScheme; //Catch-all control scheme for non-keyboard player (WARNING: untested)
    public GameObject playerPrefab; //Player gameObject, one will be spawned for each player

    private InputUser keyboardUser; //Input user for keyboard
    private List<InputUser> controllerUsers = new List<InputUser>(); //List of all humans with connected controllers

    //SPAWN SETTINGS:
    [Range(0, 1)] public float playerLaserBrightness; //Modifies laser coloration to make it easier to see

    private void Update()
    {
       //Check For Unpaired Users:
        if (InputUser.GetUnpairedInputDevices().Count != 0) //If unpaired input devices are being detected...
        {
            InputControlList<InputDevice> unpairedDevices = InputUser.GetUnpairedInputDevices(); //Get unpaired input devices
            for (int x = 0; x < unpairedDevices.Count; x++) //Parse through all unpaired devices
            {
               //Pair Found Device with New User:
                InputUser newUser = InputUser.PerformPairingWithDevice(unpairedDevices[x]); //Instantiate new input user and pair to unpaired device
                if (unpairedDevices[x].displayName != "Keyboard") //If new device is not a keyboard...
                {
                    newUser.AssociateActionsWithUser(controllerControlScheme); //Associate user with controller scheme
                    SpawnPlayer(controllerControlScheme); //Spawn a controller player
                    controllerUsers.Add(newUser); //Add new user to list of controller users
                }
                else //If new user is a keyboard user...
                {
                    for (int y = 0; y < keyboardControlSchemes.Count; y++) //Parse through all valid keyboard control schemes...
                    {
                        SpawnPlayer(keyboardControlSchemes[y]); //Spawn one player for each keyboard control scheme
                    }
                    keyboardUser = newUser; //Add new user as sole keyboard user
                }
            }
        }
        
    }

    //--|COMMAND FUNCTIONS|-------------------------------------------------------------------
    private void SpawnPlayer(InputActionAsset controlScheme)
    {
        //Description: Spawns in a player and fills in all the necessary variables

       //Initialization & Validation:
        int playerNumber = players.Count; //Get designation number for new player
        if (playerNumber >= playerTickets.Count) //If there is no available ticket for new player...
        {
            RefusePlayerEvent(); //Trigger refuse player event (probably a popup)
            return; //Skip rest of spawn protocols
        }

       //Spawn & Place New Player:
        GameObject newPlayer = Instantiate(playerPrefab); //Instantiate a new player from prefab template
        PlayerTicket ticket = playerTickets[playerNumber]; //Assign player its ticket, picking next unused one in ticket list
        PlayerController playerCont = newPlayer.GetComponent<PlayerController>(); //Get shorthand for player controller
        players.Add(newPlayer); //Add new player to list of participating players
        newPlayer.transform.position = ticket.spawnPosition; //Move player to ticket-designated spawnpoint
        newPlayer.transform.rotation = Quaternion.Euler(0, 0, ticket.spawnRotation); //Rotate player by ticket-designated amount
       //Set Player Color Scheme:
        List<SpriteRenderer> playerColorTargets = new List<SpriteRenderer>(); //Initialize list to store all parts of player to color
        for (int x = 0; x < newPlayer.transform.childCount; x++) //Parse through each of player object's child objects
        {
           //Initialization & Validation:
            Transform thisChild = newPlayer.transform.GetChild(x); //Get current child object
            if (!thisChild.gameObject.CompareTag("TeamColorTarget")) { continue; } //Skip this child if it is not colorable
            SpriteRenderer thisSpr = thisChild.GetComponent<SpriteRenderer>(); //Get the spriteRenderer of this child object
           //Assign Player Color:
            thisSpr.color = ticket.playerColor; //Color part according to ticket-designated pigment
           //Optionally Correct LED Orientation:
            if (thisChild.name == "LED") { thisChild.transform.rotation = Quaternion.Euler(Vector3.zero); } //Straighten out LED
           //Optionally Set Up Laser Source Link:
            if (thisChild.name == "LEDLaserSource") //If this child is the LED Laser Source object...
            {
                playerCont.laserSourceCont = thisChild.GetComponent<LEDLaserSourceController>(); //Get LED laser source controller for player
            }
        }
        playerCont.laserColor = new Color(ticket.playerColor.r * playerLaserBrightness,  //Set color of player laser
                                          ticket.playerColor.g * playerLaserBrightness,  //Set color of player laser
                                          ticket.playerColor.b * playerLaserBrightness); //Set color of player laser
       //Assign Control Scheme to Player:
        PlayerInput newPlayerInput = newPlayer.GetComponent<PlayerInput>(); //Get player input component from player
        newPlayerInput.actions = controlScheme; //Assign designated actions to player

    }
    private void DropPlayer(GameObject player)
    {
        //Description: Drops designated player from game


    }
    private void DropPlayer(int player)
    {
        //Description: OVERLOAD for DropPlayer

        DropPlayer(players[player]); //Select designated player from players list based on given index and send to DropPlayer function
    }

//--|COMMAND UTILITY FUNCTIONS|-----------------------------------------------------------
    private void PlayerJoinEvent()
    {
        //Trigger: When a player successfully joins game


    }
    private void RefusePlayerEvent()
    {
        //Trigger: When a player tries to join but there is not enough room


    }
}
