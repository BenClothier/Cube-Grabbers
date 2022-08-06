namespace Game.Utility.Math
{
    using UnityEngine;

    public static class Ballistics
    {
        /// <summary>
        /// Calculate the lanch angle.
        /// </summary>
        /// <returns>Angle to be fired on.</returns>
        /// <param name="start">The muzzle.</param>
        /// <param name="end">Wanted hit point.</param>
        /// <param name="launchSpeed">Muzzle velocity.</param>
        /// <param name="angle">The calculated angle of trajectory.</param>
        /// <param name="isHighAngle">Whether to calculate the high angle rather than the low angle (since there are two possible trajectories.</param>
        public static bool CalculateTrajectory(Vector3 start, Vector3 end, float launchSpeed, out float angle, bool isHighAngle = true)
        {
            Vector3 dir = end - start;
            float vSqr = launchSpeed * launchSpeed;
            float y = dir.y;

            dir.y = 0.0f;
            float x = dir.sqrMagnitude;

            float g = -Physics.gravity.y;

            float uRoot = vSqr * vSqr - g * (g * (x) + (2.0f * y * vSqr));

            if (uRoot < 0.0f)
            {
                // target out of range. TODO calculate angle at furthest range.
                angle = -45.0f;
                return false;
            }

            float r = Mathf.Sqrt(uRoot);
            float bottom = g * Mathf.Sqrt(x);

            angle = isHighAngle ? -(Mathf.Atan2(vSqr + r, bottom) * Mathf.Rad2Deg) : -(Mathf.Atan2(vSqr - r, bottom) * Mathf.Rad2Deg);
            return true;

        }

        /// <summary>
        /// Gets the ballistic path.
        /// </summary>
        /// <returns>The ballistic path.</returns>
        /// <param name="startPos">Start position.</param>
        /// <param name="forward">Forward direction.</param>
        /// <param name="launchSpeed">Velocity.</param>
        /// <param name="timeResolution">Time from frame to frame.</param>
        /// <param name="maxTime">Max time to simulate, will be clamped to reach height 0 (aprox.).</param>

        public static Vector3[] GetBallisticPath(Vector3 startPos, Vector3 forward, float launchSpeed, float timeResolution, float maxTime = 5f)
        {

            //maxTime = Mathf.Min(maxTime, GetTimeOfFlight(launchSpeed, Vector3.Angle(forward, Vector3.up) * Mathf.Deg2Rad, startPos.y));
            Vector3[] positions = new Vector3[Mathf.CeilToInt(maxTime / timeResolution)];
            Vector3 velVector = forward * launchSpeed;
            int index = 0;
            Vector3 curPosition = startPos;
            for (float t = 0.0f; t < maxTime; t += timeResolution)
            {

                if (index >= positions.Length)
                    break;//rounding error using certain values for maxTime and timeResolution

                positions[index] = curPosition;
                curPosition += velVector * timeResolution;
                velVector += Physics.gravity * timeResolution;
                index++;
            }
            return positions;
        }

        /// <summary>
        /// Checks the ballistic path for collisions.
        /// </summary>
        /// <returns><c>false</c>, if ballistic path was blocked by an object on the Layermask, <c>true</c> otherwise.</returns>
        /// <param name="arc">Arc.</param>
        /// <param name="lm">Anything in this layer will block the path.</param>
        public static bool CheckBallisticPath(Vector3[] arc, LayerMask lm)
        {

            RaycastHit hit;
            for (int i = 1; i < arc.Length; i++)
            {

                //if (Physics.Raycast(arc[i - 1], arc[i] - arc[i - 1], out hit, (arc[i] - arc[i - 1]).magnitude) && (lm == (lm | (1 << hit.transform.gameObject.layer))))
                //    return false;

                if (Physics.Raycast(arc[i - 1], arc[i] - arc[i - 1], out hit, (arc[i] - arc[i - 1]).magnitude) && (lm == (lm | (1 << hit.transform.gameObject.layer))))
                {
                    Debug.DrawRay(arc[i - 1], arc[i] - arc[i - 1], Color.red, 10f);
                    return false;
                }
                else
                {
                    Debug.DrawRay(arc[i - 1], arc[i] - arc[i - 1], Color.green, 10f);
                }
            }
            return true;
        }

        public static bool GetHit(Vector3 startPos, Vector3 forward, float launchSpeed, out RaycastHit? hitInfo)
        {
            Vector3[] path = GetBallisticPath(startPos, forward, launchSpeed, 0.2f);
            for (int i = 1; i < path.Length; i++)
            {
                Debug.DrawRay (path [i - 1], path [i] - path [i - 1], Color.blue);
                if (Physics.Raycast(path[i - 1], path[i] - path[i - 1], out RaycastHit hit, (path[i] - path[i - 1]).magnitude))
                {
                    hitInfo = hit;
                    return true;
                }
            }

            hitInfo = null;
            return false;
        }


        public static float CalculateMaxRange(float launchSpeed)
        {
            return (launchSpeed * launchSpeed) / -Physics.gravity.y;
        }

        public static float GetTimeOfFlight(float vel, float angle, float height)
        {
            return (2.0f * vel * Mathf.Sin(angle)) / -Physics.gravity.y;
        }

    }
}