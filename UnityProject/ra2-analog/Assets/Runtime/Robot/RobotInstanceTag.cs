using UnityEngine;

namespace Ra2.Robot
{
    /// <summary>
    /// Runtime identity on assembled robot root so contact probes can resolve victims
    /// without Transform teleport or client-trusted damage.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RobotInstanceTag : MonoBehaviour
    {
        public int RobotId { get; private set; }
        public RobotSpawnedInstance Instance { get; private set; }

        public void Bind(RobotSpawnedInstance instance)
        {
            Instance = instance;
            RobotId = instance != null ? instance.RobotId : -1;
        }

        /// <summary>S7-09 thin contact probe identity (no full spawn instance).</summary>
        public void BindProbe(int robotId)
        {
            Instance = null;
            RobotId = robotId;
        }
    }
}
