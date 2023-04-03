using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Attach this as component to every original GameObject with Renderer, and link a IVrWindow for it to be able to control stencil value and clipping plane after recording.
// IVrWindow has to be set immediately before starting a recording. Otherwise it might be overwritten by another Recorder.
// Replacement Shaders implementing a stencil test and clipping for the duplicated GameObjects can be set here or in IVrWindow.
// For more information, see the documentation for the IRecorderExtension interface.

// Interference with CustomMaterialExtension:
//     If CustomMaterialExtension is attached, standard replacement Shaders will be overwritten during GetsCalledBeforeRecording().
//     Therefore, they will overwrite the replacement Shaders set here.

public class VrWindowExtension : MonoBehaviour, IRecorderExtension
{
    [Tooltip("This Shader will replace the Shader of opaque Materials in duplicated GameObjects. Can alternatively be defined in vrWindowInterface.")]
    public Shader opaqueStandardReplacementShader;
    [Tooltip("This Shader will replace the Shader of transparent Materials in duplicated GameObjects. Can alternatively be defined in vrWindowInterface.")]
    public Shader transparentStandardReplacementShader;

    [Tooltip("Link IVrWindow directly before starting a recording for it to be able to register it's Materials.")]
    public IVrWindow vrWindowInterface;
    [Tooltip("If vrWindowInterface isn't linked by code, you can link it's GameObject instead to find it.")]
    public GameObject iVrWindowHolder;

    // Unused functions
    void IRecorderExtension.GetsCalledBeforeInstantiation() { }
    void IRecorderExtension.GetsCalledBeforeRecording() { }
    void IRecorderExtension.GetsCalledDuringRecording(float deltaTime) { }
    void IRecorderExtension.GetsCalledDuringPlayback(float playTime) { }
    List<GameObject> IRecorderExtension.GetAllGameObjectsInstantiatedByThis() { return new List<GameObject>(); }

    // As Unity can't copy instances of single components, a new component has to be added and every variable has to be copied individually.
    void IRecorderExtension.CopyComponentToGameObject(GameObject _gameObject)
    {
        VrWindowExtension duplicatedVrWindowExtension = _gameObject.AddComponent<VrWindowExtension>();

        // Copy all variables
        duplicatedVrWindowExtension.opaqueStandardReplacementShader = opaqueStandardReplacementShader;
        duplicatedVrWindowExtension.transparentStandardReplacementShader = transparentStandardReplacementShader;
        duplicatedVrWindowExtension.vrWindowInterface = vrWindowInterface;
        duplicatedVrWindowExtension.iVrWindowHolder = iVrWindowHolder;

        // Replace Materials of the duplicated GameObject
        duplicatedVrWindowExtension.LockCurrentVrWindow();
    }

    void IRecorderExtension.GetsCalledAfterInstantiation()
    {
        // Replace Materials of the duplicated GameObject
        LockCurrentVrWindow();
    }

    // Register all Materials at IVrWindow
    void IRecorderExtension.GetsCalledBeforePlayback()
    {
        if (vrWindowInterface == null)
            return;

        // Find all renderers of this duplicated GameObject and all GameObjects created during recording as child of this GameObject
        List<Renderer> renderers = new List<Renderer>();
        renderers.Add(gameObject.GetComponent<Renderer>());
        foreach (IRecorderExtension recorderExtension in gameObject.GetComponents<IRecorderExtension>())
            foreach(GameObject _gameObject in recorderExtension.GetAllGameObjectsInstantiatedByThis())
                renderers.Add(_gameObject.GetComponent<Renderer>());

        // Register all Materials at the linked IVrWindow
        foreach (Renderer renderer in renderers)
            foreach (Material material in renderer.materials)
                vrWindowInterface.RegisterMaterial(material);
    }


    // Replace Materials of the duplicated GameObject
    public void LockCurrentVrWindow()
    {
        // If vrWindowInterface is not set, get IVrWindow from iVrWindowHolder instead
        if (vrWindowInterface == null && iVrWindowHolder != null)
            vrWindowInterface = iVrWindowHolder.GetComponent<IVrWindow>();

        if (vrWindowInterface == null)
            return;

        // Get the local Renderer, and load replacement Materials
        List<Renderer> renderers = new List<Renderer>();
        renderers.Add(gameObject.GetComponent<Renderer>());

        if (transparentStandardReplacementShader == null)
            transparentStandardReplacementShader = vrWindowInterface.GetTransparentReplacementShader();

        if (opaqueStandardReplacementShader == null)
            opaqueStandardReplacementShader = vrWindowInterface.GetOpaqueReplacementShader();

        // Replace every material of the duplicated GameObject
        foreach (Renderer renderer in renderers)
        {
            foreach (Material material in renderer.materials)
            {
                // >2500 usually means this Material is part of the transparent render queue
                if (transparentStandardReplacementShader != null && material.renderQueue > 2500)
                    material.shader = transparentStandardReplacementShader;
                else if (opaqueStandardReplacementShader != null) // opaque render queue
                    material.shader = opaqueStandardReplacementShader;
            }
        }
    }
}
