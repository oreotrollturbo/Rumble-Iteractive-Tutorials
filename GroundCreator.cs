using Il2CppRUMBLE.Managers;
using Il2CppRUMBLE.MoveSystem;
using UnityEngine;
using Object = System.Object;

namespace InteractiveTutorials;

public static class GroundCreator
{
    // Constant thickness matching the FlatLand mod
    private const float GROUND_THICKNESS = 0.01f;

    public static GameObject CreateGroundCollider(Vector3 position, Vector2 size, 
                                                 bool isMainGround = true, 
                                                 string name = "GroundCollider")
    {
        // Create the ground GameObject
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
        
        // Set transform properties
        ground.transform.position = position;
        ground.transform.localScale = new Vector3(size.x, GROUND_THICKNESS, size.y);
        
        // Make the ground completely transparent
        Renderer renderer = ground.GetComponent<Renderer>();
        renderer.material.shader = Shader.Find("Universal Render Pipeline/Lit");
        renderer.material.color = new Color(0, 0, 0, 0);
        //renderer.material.color = new Color(0.5f, 0.5f, 0.5f, 0.3f); //Debug colour
        
        
        // Remove the default BoxCollider and add our custom colliders
        GameObject.Destroy(ground.GetComponent<BoxCollider>());
        
        // Add MeshCollider
        MeshCollider meshCollider = ground.AddComponent<MeshCollider>();
        
        // Add GroundCollider component (RUMBLE-specific)
        GroundCollider groundCollider = ground.AddComponent<GroundCollider>();
        groundCollider.isMainGroundCollider = isMainGround;
        groundCollider.collider = meshCollider;
        
        // Set layer to Ground (layer 9) for proper physics interactions
        ground.layer = 9;
        
        return ground;
    }

    // Overload with single size value for square ground
    public static GameObject CreateGroundCollider(Vector3 position, float size,
                                                 bool isMainGround = true,
                                                 string name = "GroundCollider")
    {
        return CreateGroundCollider(position, new Vector2(size, size), isMainGround, name);
    }
}