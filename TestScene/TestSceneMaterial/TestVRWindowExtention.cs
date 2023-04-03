using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestVRWindowExtention : MonoBehaviour, IVrWindow
{
    public Shader opaqueStandardReplacementShader;
    public Shader transparentStandardReplacementShader;

    void IVrWindow.RegisterMaterial(Material material)
    {

    }

    Shader IVrWindow.GetOpaqueReplacementShader()
    {
        return opaqueStandardReplacementShader;
    }

    Shader IVrWindow.GetTransparentReplacementShader()
    {
        return transparentStandardReplacementShader;
    }
}
