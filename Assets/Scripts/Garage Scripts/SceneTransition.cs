using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem; // Added for New Input System

public class SceneLoader : MonoBehaviour
{
    [Header("Scene Settings")]
    [Tooltip("Type the exact name of the scene you want to load")]
    public string sceneToLoad = "Game Scene"; 
    
    [Header("Interaction Settings")]
    [Tooltip("Size of the interaction box (Width, Height, Depth)")]
    public Vector3 interactionBoxSize = new Vector3(3f, 2f, 3f);
    [Tooltip("Offset of the box relative to the object center")]
    public Vector3 interactionBoxOffset = Vector3.zero;
    
    // Changed from KeyCode to Key for the New Input System
    public Key interactKey = Key.E;

    [Header("Player Detection")]
    public string playerTag = "Player";

    private Transform playerTransform;

    void Start()
    {
        // Find the player automatically by tag
        GameObject player = GameObject.FindGameObjectWithTag(playerTag);
        if (player != null)
        {
            playerTransform = player.transform;
        }
        else
        {
            Debug.LogWarning("SceneLoader: No object with tag '" + playerTag + "' found!");
        }
    }

    void Update()
    {
        if (playerTransform == null) return;

        // Convert player position to local space of this object
        Vector3 localPlayerPos = transform.InverseTransformPoint(playerTransform.position);
        
        // Calculate box bounds in local space
        Vector3 center = interactionBoxOffset;
        Vector3 halfSize = interactionBoxSize * 0.5f;

        // Check if player is inside the box boundaries
        bool inX = localPlayerPos.x >= center.x - halfSize.x && localPlayerPos.x <= center.x + halfSize.x;
        bool inY = localPlayerPos.y >= center.y - halfSize.y && localPlayerPos.y <= center.y + halfSize.y;
        bool inZ = localPlayerPos.z >= center.z - halfSize.z && localPlayerPos.z <= center.z + halfSize.z;

        // Check input using the New Input System
        bool isKeyPressed = false;
        if (Keyboard.current != null)
        {
            isKeyPressed = Keyboard.current[interactKey].wasPressedThisFrame;
        }

        if (inX && inY && inZ && isKeyPressed)
        {
            LoadTargetScene();
        }
    }

    void LoadTargetScene()
    {
        // Check if scene name is valid before trying to load
        if (Application.CanStreamedLevelBeLoaded(sceneToLoad))
        {
            SceneManager.LoadScene(sceneToLoad);
        }
        else
        {
            Debug.LogError("Scene '" + sceneToLoad + "' cannot be loaded. Did you add it to the Build Settings?");
        }
    }

    // Helper to see the box in the Scene view
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        // Use the object's rotation and scale for the gizmo
        Matrix4x4 rotationMatrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.lossyScale);
        Gizmos.matrix = rotationMatrix;
        Gizmos.DrawWireCube(interactionBoxOffset, interactionBoxSize);
    }
}