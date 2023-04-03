using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

// Attach this script as a component to GameObjects in your scene to overwrite their Materials during recording with custom materials.
// See IRecorderExtension for further documentation on the interface.

// Interference with VrWindowExtension:
//     Even if VrWindowExtension sets standard replacement shaders,
//     this script will be called GetsCalledBeforeRecording() (during CopyComponentToGameObject() / GetsCalledAfterInstantiation() ),
//     so the customMaterials will overwrite the standard replacement shaders.
//
// Interference with MeshRecordingExtension:
//     As MeshRecordingExtension copies each Material after GetsCalledBeforeRecording() in GetsCalledDuringRecording(),
//     affected GameObjects will also have customMaterials applied.

public class CustomMaterialExtension : MonoBehaviour, IRecorderExtension
{
    [Tooltip("These materials will replace materials of GameObject in recording. (Will overwrite standard replacement shaders)")]
    public List<Material> customMaterials = new List<Material>();

    // Unused interface functions
    void IRecorderExtension.GetsCalledBeforeInstantiation() { }
    void IRecorderExtension.GetsCalledAfterInstantiation() { }
    void IRecorderExtension.GetsCalledDuringRecording(float deltaTime) { }
    void IRecorderExtension.GetsCalledBeforePlayback() { }
    void IRecorderExtension.GetsCalledDuringPlayback(float playTime) { }
    List<GameObject> IRecorderExtension.GetAllGameObjectsInstantiatedByThis() { return new List<GameObject>(); }

    // As Unity can't copy instances of single components, a new component has to be added and every variable has to be copied individually.
    void IRecorderExtension.CopyComponentToGameObject(GameObject _gameObject)
    {
        CustomMaterialExtension duplicatedCustomMaterialExtension = _gameObject.AddComponent<CustomMaterialExtension>();
        duplicatedCustomMaterialExtension.customMaterials = customMaterials;
    }

    // Replaces Materials before recording starts.
    void IRecorderExtension.GetsCalledBeforeRecording()
    {
        Renderer renderer = gameObject.GetComponent<Renderer>();
        if (renderer == null || customMaterials.Count == 0)
            return;

        renderer.materials = customMaterials.ToArray();
    }
}
