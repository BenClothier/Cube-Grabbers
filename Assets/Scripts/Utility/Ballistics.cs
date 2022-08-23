namespace Game.Utility.Math
{
    using System.Linq;
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

        public static Vector3[] GetBallisticPath(Vector3 startPos, Vector3 forward, float launchSpeed, float timeResolution, float maxTime = 8)
        {
            Vector3[] positions = new Vector3[Mathf.CeilToInt(maxTime / timeResolution)];
            Vector3 velVector = forward * launchSpeed;
            int index = 0;
            Vector3 curPosition = startPos;

            for (float t = 0.0f; t < maxTime; t += timeResolution)
            {

                if (index >= positions.Length)
                    break; //rounding error using certain values for maxTime and timeResolution

                positions[index] = curPosition;
                curPosition += velVector * timeResolution;
                velVector += Physics.gravity * timeResolution;
                index++;
            }
            return positions;
        }

        /// <summary>
        /// Checks the ballistics path for collisions.
        /// </summary>
        /// <param name="arc">The path.</param>
        /// <returns>The hit information, if there was a hit.</returns>
        public static LaunchPathInfo CheckBallisticPath(Vector3[] arc, LaunchPathInfo pathInfo)
        {
            Vector3 maxY = new Vector3(0, float.MinValue, 0);
            int i;

            for (i = 1; i < arc.Length; i++)
            {
                Vector3 rayOrigin = arc[i - 1];
                Vector3 rayDir = arc[i] - arc[i - 1];
                maxY = (rayOrigin.y > maxY.y) ? rayOrigin : maxY;

                if (Physics.Raycast(rayOrigin, rayDir, out RaycastHit hit, (arc[i] - arc[i - 1]).magnitude, LayerMask.GetMask("Default")))
                {
                    pathInfo.hit = hit;
                    break;
                }
            }

            pathInfo.highestPoint = maxY;
            pathInfo.launchPath = ThinOutPath(arc.Take(i).ToArray());
            return pathInfo;
        }

        public static LaunchPathInfo GenerateLaunchPathInfo(Vector3 startPos, Quaternion launchDir, Vector3 forward, float launchSpeed)
        {
            Vector3[] path = GetBallisticPath(startPos, forward, launchSpeed, 0.002f, 8);

            LaunchPathInfo launchPathInfo = default;
            launchPathInfo = CheckBallisticPath(path, launchPathInfo);
            launchPathInfo.launchDir = launchDir;

            return launchPathInfo;
        }

        public static Vector3 CalculateHighestPoint(Vector3 launchOrigin, Vector3 launchTrajectory, float launchSpeed)
        {
            Vector3 hrz = new (launchTrajectory.x * launchSpeed, 0, launchTrajectory.z * launchSpeed);
            float v_y = launchTrajectory.y * launchSpeed;

            return launchOrigin + (hrz * (v_y / -Physics.gravity.y)) + (Vector3.up * (v_y * v_y) / (-Physics.gravity.y * 2));
        }

        public static Vector3[] ThinOutPath(Vector3[] path, int thinFactor = 15)
        {
            var last = path.Length - 1;
            return path.Where((item, i) => i % thinFactor == 0 || i == last)
                           .ToArray();
        }

        public struct LaunchPathInfo
        {
            public RaycastHit? hit;
            public Vector3 highestPoint;
            public Vector3[] launchPath;
            public Quaternion launchDir;
        }
    }
}