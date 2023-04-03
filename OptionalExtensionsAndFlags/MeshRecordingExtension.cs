using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// MeshRecordingExtension is a component that records changes of a mesh structure over time (and not just the transform).
// It should be attached to the original GameObject before starting the recording.
// This extension can consume a lot of RAM quickly if you record large meshes!
// Each frame, the extension compares the mesh with the previous recorded one and skips frames which are identical.
// See IRecorderExtension for further documentation.

// Interference with VrWindowExtension:
//     If VrWindowExtension is attached, shaders will be replaced during GetsCalledAfterInstantiation() / CopyComponentToGameObject(),
//     so all Instantiated copies created in GetsCalledDuringRecording() will also have standard replacement shaders.
//     (If not overwritten by CustomMaterialExtension)

// Interference with CustomMaterialExtension:
//     CustomMaterialExtension will replace materials in GetsCalledBeforeRecording(),
//     so all Instantiated copies created in GetsCalledDuringRecording() will also have customMaterials.

public class MeshRecordingExtension : MonoBehaviour, IRecorderExtension
{
    [Tooltip("Frames per second that will be recorded. Lower than 60 FPS will save RAM, but replay may stutter (no interpolation available).")]
    public float fps = 15f;

    [Tooltip("No need to set.")]
    public GameObject originalGameObject; // Original GameObject with MeshFilter

    private List<GameObject> meshHolderChilds = new List<GameObject>(); // List of instantiated GameObjects with mesh snapshots
    private List<float> meshTimestamps = new List<float>(); // List of record times in seconds for meshHolderChilds
    private float secondsPerFrame = 0.0166f; // Time between each recorded frame
    private float length; // Total record length in seconds
    private float lengthToLatestSnapshot; // Total record time when last snapshot was stored in seconds
    private GameObject latestActiveMeshHolder; // Latest during replay shown meshHolder, to know which one to deactivate when another one gets activated

    // Unused function
    void IRecorderExtension.GetsCalledAfterInstantiation() { }

    // Called before instantiation. Saves the original GameObject to know where to copy the mesh from.
    void IRecorderExtension.GetsCalledBeforeInstantiation()
    {
        originalGameObject = gameObject;
    }

    // As Unity can't copy instances of single components, a new component has to be added and every variable has to be copied individually.
    void IRecorderExtension.CopyComponentToGameObject(GameObject _gameObject)
    {
        MeshRecordingExtension duplicatedMeshRecordingExtension = _gameObject.AddComponent<MeshRecordingExtension>();

        // Save the original GameObject to know where to copy the mesh from
        duplicatedMeshRecordingExtension.originalGameObject = gameObject;
    }

    // Setting the recording speed before starting the recording
    void IRecorderExtension.GetsCalledBeforeRecording()
    {
        if (fps > 0f)
            secondsPerFrame = 1f/ fps;
    }

    // Record a new snapshot every frame
    void IRecorderExtension.GetsCalledDuringRecording(float deltaTime)
    {
        TakeSnapshot(deltaTime);
    }

    // Prepare playback. Loads the first snapshot and hides the original mesh.
    void IRecorderExtension.GetsCalledBeforePlayback()
    {
        // If GetsCalledDuringRecording() never got called take at least one snapshot
        if (meshHolderChilds.Count == 0)
            TakeSnapshot(0);

        // Hide the mesh of the regularly duplicated GameObject
        Renderer renderer = gameObject.GetComponent<Renderer>();
        if (renderer != null)
            renderer.enabled = false;
        MeshRenderer meshRenderer = gameObject.GetComponent<MeshRenderer>();
        if (meshRenderer != null)
            meshRenderer.enabled = false;

        // Load the first snapshot
        AnimateMesh(0f);
    }

    // Change the active mesh during playback, activating the mesh recorded at the requested time and hiding the last active one
    void IRecorderExtension.GetsCalledDuringPlayback(float playTime)
    {
        AnimateMesh(playTime);
    }

    // Returns a List of all instantiated GameObjects to get their Materials later
    List<GameObject> IRecorderExtension.GetAllGameObjectsInstantiatedByThis()
    {
        return meshHolderChilds;
    }

    // Create a new snapshot by instantiating the current mesh and adding it to the mesh snapshots list.
    private void TakeSnapshot(float deltaTime)
    {
        length += deltaTime;

        // Check if it's time for a new snapshot. 
        if (lengthToLatestSnapshot + secondsPerFrame >= length && length != deltaTime)
            return;
        else
            lengthToLatestSnapshot = length;


        // Clone the current mesh
        GameObject newMeshHolder = generateNewClone();
        if (newMeshHolder == null)
            return;

        // Compare it with the latest snapshots and destroy the newest one if they are identical.
        // This can't be done before creating the snapshot, cause further changes of a shared mesh after reading it once, will prevent further changes of the mesh.
        Mesh newMesh = GetMeshForComparison(newMeshHolder);
        Mesh latestMesh = GetMeshForComparison(GetLatestMeshHolder());

        if (CheckIfIdentical(newMesh, latestMesh))
        {
            Destroy(newMeshHolder);
            return;
        }

        // If the snapshot is kept, attach the holding GameObject as child to the initially duplicated GameObject, whose mesh will be deactivated.
        newMeshHolder.transform.SetParent(gameObject.transform);
        // Changes of the transform are recorded from the initially duplicated GameObject.
        // The child holding the mesh has to exactly follow this transform.
        // Therefor the transform is set to identity.
        newMeshHolder.transform.localScale = Vector3.one;
        newMeshHolder.transform.localPosition = Vector3.zero;
        newMeshHolder.transform.localEulerAngles = Vector3.zero;
        // Deactivate the child. Only the one child will be activated during replay which represents the shape during that time of recording.
        newMeshHolder.SetActive(false);
        meshHolderChilds.Add(newMeshHolder);
        meshTimestamps.Add(length);

        // Copy the materials from the initially duplicated GameObject.
        // If CustomMaterialExtension or VrWindowExtension are used, these were modified earlier.
        // Modifications after GetsCalledBeforeRecording() will have only influence on later snapshots.
        MeshRenderer parentMeshRenderer = gameObject.GetComponent<MeshRenderer>();
        MeshRenderer childMeshRenderer = newMeshHolder.GetComponent<MeshRenderer>();
        if (parentMeshRenderer != null && childMeshRenderer != null)
            childMeshRenderer.materials = parentMeshRenderer.materials;
    }


    // Enables the mesh holding GameObject which was recorded at "time" and disables all other MeshHolder to make them invisible
    private void AnimateMesh(float time)
    {
        int meshCount = meshHolderChilds.Count;
        if (meshCount == 0)
            return;

        GameObject activateThisMesh = meshHolderChilds[meshCount - 1];

        // Find the right mesh holding GameObject by checking the recording time against the timestamp of each mesh snapshot.
        if (time >= length)
            { }  //activateThisMesh = meshHolderChilds[meshCount - 1];
        else if (time < 0.02)
            activateThisMesh = meshHolderChilds[0];
        else
        {
            // Search through the List of snapshots to find the right one.
            // If processing time becomes an issue for long records, nested Lists/Arrays could be a solution.
            // But as this is just an extension for special cases, I prefer less complexity for now.
            for (int i = 0; i < meshCount; i++)
            {
                if (meshTimestamps[i] >= time)
                {
                    activateThisMesh = meshHolderChilds[i];
                    break;
                }
            }
        }

        // Deactivate the last mesh holder
        if (latestActiveMeshHolder != null)
            latestActiveMeshHolder.SetActive(false);

        // Activate the new mesh holder
        activateThisMesh.SetActive(true);
        latestActiveMeshHolder = activateThisMesh;
    }


    // Clone the current mesh by instantiating it and assigning it to a mesh holding GameObject.
    private GameObject generateNewClone()
    {
        if (originalGameObject == null)
            return null;

        MeshFilter meshFilter = originalGameObject.GetComponent<MeshFilter>();
        if (meshFilter == null)
            return null;

        GameObject meshHolder = new GameObject();
        meshHolder.AddComponent<MeshFilter>().sharedMesh = Instantiate(meshFilter.sharedMesh);
        meshHolder.AddComponent<MeshRenderer>().sharedMaterials = meshFilter.GetComponent<MeshRenderer>().sharedMaterials;

        return meshHolder;
    }

    // Get last mesh holder in List
    private GameObject GetLatestMeshHolder()
    {
        int listCount = meshHolderChilds.Count;
        if(listCount == 0)
            return null;

        return meshHolderChilds[listCount - 1];
    }

    // Get the mesh object from a GameObject
    // Don't call this on GameObjects with meshes which can still change shape.
    // This would cause an error.
    private Mesh GetMeshForComparison(GameObject fromGameObject)
    {
        if (fromGameObject == null)
            return null;

        MeshFilter meshFilter = fromGameObject.GetComponent<MeshFilter>();
        if (meshFilter == null)
            return null;

        return meshFilter.mesh;
    }

    // Checking if two meshes are identical.
    // This requires to compare each single vertex position and colour.
    // This might cause higher high computational load during recording, but saves RAM.
    private bool CheckIfIdentical(Mesh mesh1, Mesh mesh2)
    {
        if (mesh1 == mesh2) return true;
        if (mesh1 == null || mesh2 == null) return false;

        // Compare vertices
        if (mesh1.vertexCount != mesh2.vertexCount) return false;
        List<Vector3> vertices1 = new List<Vector3>();
        mesh1.GetVertices(vertices1);
        List<Vector3> vertices2 = new List<Vector3>();
        mesh2.GetVertices(vertices2);
        for (int i = 0; i < vertices1.Count; i++)
        {
            if (vertices1[i].x != vertices2[i].x) return false;
            if (vertices1[i].y != vertices2[i].y) return false;
            if (vertices1[i].z != vertices2[i].z) return false;
        }

        // Compare colors
        List<Color> colors1 = new List<Color>();
        mesh1.GetColors(colors1);
        List<Color> colors2 = new List<Color>();
        mesh2.GetColors(colors2);
        if (colors1.Count != colors2.Count) return false;
        for (int i = 0; i < colors1.Count; i++)
        {
            if (colors1[i].r != colors2[i].r) return false;
            if (colors1[i].g != colors2[i].g) return false;
            if (colors1[i].b != colors2[i].b) return false;
            if (colors1[i].a != colors2[i].a) return false;
        }

        // No differences found, so they are assumed to be identical
        return true;
    }
}
