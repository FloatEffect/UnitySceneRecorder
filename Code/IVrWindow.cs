using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnitySceneRecorder;

// Implement this interface per replay window.
// VrWindowExtension will link to this interface by assigning the vrWindowInterface field, which will allow it to communicate with the replay window during the replay process.

namespace UnitySceneRecorder{
	public interface IVrWindow
	{
		// VrWindowExtension will register all recorded materials at the end of recording using RegisterMaterial().
		// This function can be used by IVrWindow to modify the materials' shader parameters.
		// Modifying the shader parameter may be necessary to set the stencil value or clipping plane.
		void RegisterMaterial(Material material);

		// This function should return a Shader object with a stencil test and clipping plane for opaque materials.
		// This shader will replace the original opaque shaders before replay.
		Shader GetOpaqueReplacementShader();

		// This function should return a Shader object with a stencil test and clipping plane for transparent materials.
		// This shader will replace the original opaque shaders before replay.
		Shader GetTransparentReplacementShader();
	}
}