using System.Linq;
using UnityEngine;

/// <summary>
/// Request to interpolate Y-Damping (e.g., for falling states).
/// </summary>
public class CameraDampingRequest
{
    public bool IsFalling { get; set; }
}