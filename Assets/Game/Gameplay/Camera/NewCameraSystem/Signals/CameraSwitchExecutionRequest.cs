using System.Collections;
using System.Linq;
using UnityEngine;

/// <summary>
/// Internal signal for execution once cameras are resolved.
/// </summary>
public class CameraSwitchExecutionRequest
{
    public Unity.Cinemachine.CinemachineCamera Left;
    public Unity.Cinemachine.CinemachineCamera Right;
    public Vector2 ExitDirection;
}