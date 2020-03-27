﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// An object that the player can interact with
public class interactable : MonoBehaviour
{
    [System.Flags]
    public enum FLAGS
    {
        NONE = 0,
        DISALLOWS_MOVEMENT = 2,
        DISALLOWS_ROTATION = 4,
    };

    public virtual string cursor() { return cursors.DEFAULT_INTERACTION; }
    public virtual FLAGS player_interact() { return FLAGS.NONE; }
    public virtual void on_start_interaction(RaycastHit point_hit) { }
    public virtual void on_end_interaction() { }
    protected void stop_interaction() { player.current.interacting_with = null; }
}

public class player : MonoBehaviour
{
    //###########//
    // CONSTANTS //
    //###########//

    public const float HEIGHT = 1.8f;
    public const float WIDTH = 0.45f;
    public const float EYE_HEIGHT = HEIGHT - WIDTH / 2;

    public const float SPEED = 10f;
    public const float ACCELERATION_TIME = 0.2f;
    public const float ACCELERATION = SPEED / ACCELERATION_TIME;
    public const float ROTATION_SPEED = 90f;
    public const float JUMP_VEL = 5f;
    public const int MAX_MOVE_PROJ_REMOVE = 4;

    public const float GROUND_TEST_DIST = 0.05f;
    public const float TERRAIN_SINK_ALLOW = GROUND_TEST_DIST / 5f;
    public const float TERRAIN_SINK_RESET_DIST = GROUND_TEST_DIST;

    public const float INTERACTION_RANGE = 3f;

    public const float MAP_CAMERA_ALT = world.MAX_ALTITUDE * 2;
    public const float MAP_CAMERA_CLIP = world.MAX_ALTITUDE * 3;
    public const float MAP_SHADOW_DISTANCE = world.MAX_ALTITUDE * 3;
    public const float MAP_OBSCURER_ALT = world.MAX_ALTITUDE * 1.5f;

    //#################//
    // UNITY CALLBACKS //
    //#################//

    void Update()
    {
        var inter_flags = interact();

        // Toggle the map view
        if (Input.GetKeyDown(KeyCode.M))
            map_open = !map_open;

        if (map_open)
        {
            // Zoom the map
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (scroll > 0) game.render_range_target /= 1.2f;
            else if (scroll < 0) game.render_range_target *= 1.2f;
            camera.orthographicSize = game.render_range;
        }

        if (inter_flags.HasFlag(interactable.FLAGS.DISALLOWS_MOVEMENT))
            rigidbody.velocity = Vector3.zero;
        else move();

        if (inter_flags.HasFlag(interactable.FLAGS.DISALLOWS_ROTATION))
            rigidbody.angularVelocity = Vector3.zero;
        else mouse_look();
    }

    //##################//
    // ITEM INTERACTION //
    //##################//

    // The object we are currently interacting with
    RaycastHit last_interaction_hit;
    interactable _interacting_with;
    public interactable interacting_with
    {
        get { return _interacting_with; }
        set
        {
            if (_interacting_with != null)
                _interacting_with.on_end_interaction();

            _interacting_with = value;

            if (value != null)
                value.on_start_interaction(last_interaction_hit);
        }
    }

    interactable.FLAGS interact()
    {
        // Interact with the current object
        if (interacting_with != null)
        {
            canvas.cursor = interacting_with.cursor();
            return interacting_with.player_interact();
        }

        // See if an interactable object is under the cursor
        var inter = utils.raycast_for_closest<interactable>(
            camera_ray(), out last_interaction_hit, INTERACTION_RANGE);

        if (inter == null)
        {
            canvas.cursor = cursors.DEFAULT;
            return interactable.FLAGS.NONE;
        }
        else canvas.cursor = cursors.DEFAULT_INTERACTION;

        // Set the interactable and cursor,
        // interact with the object
        if (Input.GetMouseButtonDown(0))
            interacting_with = inter;

        return interactable.FLAGS.NONE;
    }

    //###########//
    //  MOVEMENT //
    //###########//

    // The players current velocity
    // in player-local coordinates
    Vector3 local_velocity;

    // The rigidbody controlling player physics
    new Rigidbody rigidbody;

    // Global velocity from local velocty
    public Vector3 velocity
    {
        get
        {
            return local_velocity.x * transform.right +
                   local_velocity.z * transform.forward +
                   local_velocity.y * Vector3.up;
        }
    }

    // Returns true if the player is on the ground
    bool grounded
    {
        get
        {
            return Physics.CapsuleCast(
                transform.position + Vector3.up * (GROUND_TEST_DIST / 2f + WIDTH / 2f),
                transform.position + Vector3.up * (GROUND_TEST_DIST / 2f + HEIGHT - WIDTH / 2f),
                WIDTH / 2, Vector3.down, GROUND_TEST_DIST);
        }
    }

    void move()
    {
        Vector3 velocity = rigidbody.velocity;

        if (Input.GetKeyDown(KeyCode.Space))
            if (grounded)
                velocity.y += JUMP_VEL;

        // Accelerate in forward/backward direction on W/S
        // if neither is pressed, set forward/backward velocity to 0
        if (Input.GetKey(KeyCode.S)) velocity -= transform.forward * ACCELERATION * Time.deltaTime;
        else if (Input.GetKey(KeyCode.W)) velocity += transform.forward * ACCELERATION * Time.deltaTime;
        else velocity -= Vector3.Project(velocity, transform.forward);

        // Accelerate in left/right direction on A/D
        // if neither is pressed, set left/right velocity to 0
        if (Input.GetKey(KeyCode.A)) velocity -= camera.transform.right * ACCELERATION * Time.deltaTime;
        else if (Input.GetKey(KeyCode.D)) velocity += camera.transform.right * ACCELERATION * Time.deltaTime;
        else velocity -= Vector3.Project(velocity, camera.transform.right);

        // Ensure speed in xz plane is not > SPEED
        float xz_mag = Mathf.Sqrt(velocity.x * velocity.x + velocity.z * velocity.z);
        if (xz_mag > SPEED)
        {
            velocity.x *= SPEED / xz_mag;
            velocity.z *= SPEED / xz_mag;
        }

        // Apply the modified velocity
        rigidbody.velocity = velocity;
    }

    //#####################//
    // VIEW/CAMERA CONTROL //
    //#####################//

    // Objects used to obscure player view
    public new Camera camera { get; private set; }
    GameObject obscurer;
    GameObject map_obscurer;

    // Called when the render range changes
    public void update_render_range()
    {
        // Set the obscurer size to the render range
        obscurer.transform.localScale = Vector3.one * game.render_range;
        map_obscurer.transform.localScale = Vector3.one * game.render_range;

        if (!map_open)
        {
            // If in 3D mode, set the camera clipping plane range to
            // the same as render_range
            camera.farClipPlane = game.render_range;
            QualitySettings.shadowDistance = camera.farClipPlane;
        }
    }

    void mouse_look()
    {
        if (map_open)
        {
            // Rotate the player with A/D
            float xr = 0;
            if (Input.GetKey(KeyCode.A)) xr = -1f;
            else if (Input.GetKey(KeyCode.D)) xr = 1.0f;
            transform.Rotate(0, xr * Time.deltaTime * ROTATION_SPEED, 0);
            return;
        }

        // Rotate the view using the mouse
        // Note that horizontal moves rotate the player
        // vertical moves rotate the camera
        transform.Rotate(0, Input.GetAxis("Mouse X") * 5, 0);
        camera.transform.Rotate(-Input.GetAxis("Mouse Y") * 5, 0, 0);
    }

    // Saved rotation to restore when we return to the 3D view
    Quaternion saved_camera_rotation;

    // True if in map view
    public bool map_open
    {
        get { return camera.orthographic; }
        set
        {
            // Use the appropriate obscurer for
            // the map or 3D views
            map_obscurer.SetActive(value);
            obscurer.SetActive(!value);

            // Set the camera orthograpic if in 
            // map view, otherwise perspective
            camera.orthographic = value;

            if (value)
            {
                // Save camera rotation to restore later
                saved_camera_rotation = camera.transform.localRotation;

                // Setup the camera in map mode/position   
                camera.orthographicSize = game.render_range;
                camera.transform.localPosition = Vector3.up * (MAP_CAMERA_ALT - transform.position.y);
                camera.transform.localRotation = Quaternion.Euler(90, 0, 0);
                camera.farClipPlane = MAP_CAMERA_CLIP;

                // Render shadows further in map view
                QualitySettings.shadowDistance = MAP_SHADOW_DISTANCE;
            }
            else
            {
                // Restore 3D camera view
                camera.transform.localPosition = Vector3.up * EYE_HEIGHT;
                camera.transform.localRotation = saved_camera_rotation;
            }
        }
    }

    // Return a ray going through the centre of the screen
    public Ray camera_ray()
    {
        return new Ray(camera.transform.position,
                       camera.transform.forward);
    }

    //################//
    // STATIC METHODS //
    //################//

    // The current player
    public static player current;

    // Create and return a player
    public static player create()
    {
        var player = new GameObject("player").AddComponent<player>();

        // Create the player camera 
        player.camera = new GameObject("camera").AddComponent<Camera>();
        player.camera.clearFlags = CameraClearFlags.SolidColor;
        player.camera.transform.SetParent(player.transform);
        player.camera.transform.localPosition = new Vector3(0, EYE_HEIGHT, 0);

        // Move the player above the first map chunk so they
        // dont fall off of the map
        player.transform.position = new Vector3(0, world.SEA_LEVEL + 1, 0);

        // Enforce the render limit with a sky-color object
        player.obscurer = Resources.Load<GameObject>("misc/obscurer").inst();
        player.obscurer.transform.SetParent(player.transform);
        player.obscurer.transform.localPosition = Vector3.zero;
        var sky_color = player.obscurer.GetComponentInChildren<Renderer>().material.color;

        player.map_obscurer = Resources.Load<GameObject>("misc/map_obscurer").inst();
        player.map_obscurer.transform.SetParent(player.transform);
        Vector3 map_obsc_pos = player.transform.position;
        map_obsc_pos.y = MAP_OBSCURER_ALT;
        player.map_obscurer.transform.position = map_obsc_pos;

        // Make the sky the same color as the obscuring object
        RenderSettings.skybox = null;
        player.camera.backgroundColor = sky_color;

        // Initialize the render range
        player.update_render_range();

        // Start with the map closed
        player.map_open = false;

        // Create the player collider
        var cc = player.gameObject.AddComponent<CapsuleCollider>();
        cc.radius = WIDTH / 2f;
        cc.height = HEIGHT;
        cc.center = new Vector3(0, HEIGHT / 2, 0);

        // Create the player rigidbody
        player.rigidbody = player.gameObject.AddComponent<Rigidbody>();
        player.rigidbody.constraints = RigidbodyConstraints.FreezeRotation;

        current = player;
        return player;
    }
}