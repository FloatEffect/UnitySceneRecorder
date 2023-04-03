using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Attach this component to GameObjects which should be duplicated by the Recorder by recreation instead of instantiation.
// This is the standard case anyway.
// Recreation is suggested over instantiation if the original GameObject owns Components which can not be destroyed or deactivated without problems.

public class ForceDuplicationByRecreation : MonoBehaviour { }