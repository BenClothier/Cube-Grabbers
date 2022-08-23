namespace Game.Utility.Math
{
    using System.Linq;
    using UnityEngine;

    public static class Ballistics
    {
        /// <summary>
        /// Calculate the information of a collidable ballistics path.
        /// </summary>
        /// <param name="launchOrigin"> The start position of the launched projectile. </param>
        /// <param name="launchTarget"> The target position for the projectile to hit. </param>
        /// <param name="launchSpeed"> The speed to launch the projectile at. </param>
        /// <param name="timeResolution">The time interval between each path point.</param>
        /// <param name="maxTime">Max time to simulate.</param>
        /// <returns></returns>
        public static LaunchPathInfo GenerateComplexTrajectoryPath(Vector3 launchOrigin, Vector3 launchTarget, float launchSpeed, float timeResolution = 0.002f, float maxTime = 8)
        {
            CalculateTrajectoryAngle(launchOrigin, launchTarget, launchSpeed, out float angle);
            TrajectoryAngleToLookDir(launchOrigin, launchTarget, angle, out Quaternion launchDir);

            LaunchPathInfo path = GenerateBasicBallisticPath(launchOrigin, launchDir, launchSpeed, timeResolution, maxTime);
            return GenerateCollisionBalisticPath(path);
        }

        /// <summary>
        /// Calculate the lanch angle.
        /// </summary>
        /// <param name="launchOrigin">The start position of the launched projectile.</param>
        /// <param name="launchTarget">The target position for the projectile to hit.</param>
        /// <param name="launchSpeed">The speed at which to launch the projectile.</param>
        /// <param name="angle">The calculated angle of trajectory.</param>
        /// <param name="isHighAngle">Whether to calculate the high angle rather than the low angle (since there are two possible trajectories).</param>
        /// <returns>The angle for the projectile to be launched at.</returns>
        private static bool CalculateTrajectoryAngle(Vector3 launchOrigin, Vector3 launchTarget, float launchSpeed, out float angle, bool isHighAngle = true)
        {
            Vector3 dir = launchTarget - launchOrigin;
            float vSqr = launchSpeed * launchSpeed;
            float y = dir.y;

            dir.y = 0.0f;
            float x = dir.sqrMagnitude;

            float g = -Physics.gravity.y;

            float uRoot = vSqr * vSqr - g * (g * (x) + (2.0f * y * vSqr));

            if (uRoot < 0.0f)
            {
                angle = -45.0f;
                return false;
            }

            float r = Mathf.Sqrt(uRoot);
            float bottom = g * Mathf.Sqrt(x);

            angle = isHighAngle ? -(Mathf.Atan2(vSqr + r, bottom) * Mathf.Rad2Deg) : -(Mathf.Atan2(vSqr - r, bottom) * Mathf.Rad2Deg);
            return true;

        }

        /// <summary>
        /// Gets the basic ballistic path of a projectile launched with the given parameters.
        /// </summary>
        /// <param name="launchOrigin">The start position of the launched projectile.</param>
        /// <param name="launchDir">The trajectory to launch the projectile at.</param>
        /// <param name="launchSpeed">The speed at which to launch the projectile.</param>
        /// <param name="timeResolution">The time interval between each path point.</param>
        /// <param name="maxTime">Max time to simulate, will be clamped to reach height 0 (aprox.).</param>
        /// <returns>The basic ballistic path information.</returns>

        private static LaunchPathInfo GenerateBasicBallisticPath(Vector3 launchOrigin, Quaternion launchDir, float launchSpeed, float timeResolution, float maxTime)
        {
            Vector3[] positions = new Vector3[Mathf.CeilToInt(maxTime / timeResolution)];
            Vector3 velVector = launchDir * Vector3.forward * launchSpeed;
            int index = 0;
            Vector3 curPosition = launchOrigin;

            for (float t = 0.0f; t < maxTime; t += timeResolution)
            {

                if (index >= positions.Length)
                {
                    break; // rounding error using certain values for maxTime and timeResolution
                }

                positions[index] = curPosition;
                curPosition += velVector * timeResolution;
                velVector += Physics.gravity * timeResolution;
                index++;
            }

            LaunchPathInfo launchPathInfo = default;
            launchPathInfo.launchDir = launchDir;
            launchPathInfo.launchPath = positions;
            return launchPathInfo;
        }

        /// <summary>
        /// Checks the ballistics path for collisions.
        /// </summary>
        /// <param name="basicBallisticPathInfo">The basic path to base the complex path off.</param>
        /// <returns>The complex ballistic path information.</returns>
        private static LaunchPathInfo GenerateCollisionBalisticPath(LaunchPathInfo basicBallisticPathInfo)
        {
            Vector3[] basicBallisticPath = basicBallisticPathInfo.launchPath;
            Vector3 maxY = new Vector3(0, float.MinValue, 0);
            int i;

            for (i = 1; i < basicBallisticPath.Length; i++)
            {
                Vector3 rayOrigin = basicBallisticPath[i - 1];
                Vector3 rayDir = basicBallisticPath[i] - basicBallisticPath[i - 1];
                maxY = (rayOrigin.y > maxY.y) ? rayOrigin : maxY;

                if (Physics.Raycast(rayOrigin, rayDir, out RaycastHit hit, (basicBallisticPath[i] - basicBallisticPath[i - 1]).magnitude, LayerMask.GetMask("Default")))
                {
                    basicBallisticPathInfo.hit = hit;
                    break;
                }
            }

            basicBallisticPathInfo.highestPoint = maxY;
            basicBallisticPathInfo.launchPath = ThinOutPath(basicBallisticPath.Take(i).ToArray());
            return basicBallisticPathInfo;
        }

        private static void TrajectoryAngleToLookDir(Vector3 start, Vector3 end, float angle, out Quaternion launchDir)
        {
            Vector3 wantedRotationVector = Quaternion.LookRotation(end - start).eulerAngles;
            wantedRotationVector.x = angle;
            launchDir = Quaternion.Euler(wantedRotationVector);
        }

        private static Vector3[] ThinOutPath(Vector3[] path, int thinFactor = 15)
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