namespace GameCore.Utility.Math
{
    using System;
    using UnityEngine;

    public static class GameMath
    {
        /// <summary>
        /// Determines the point of intersection between a plane defined by a point and a normal vector and a line defined by a point and a direction vector.
        /// </summary>
        /// <param name="planePoint">A point on the plane.</param>
        /// <param name="planeNormal">The normal vector of the plane.</param>
        /// <param name="linePoint">A point on the line.</param>
        /// <param name="lineDirection">The direction vector of the line.</param>
        /// <returns>The point of intersection between the line and the plane, null if the line is parallel to the plane.</returns>
        public static Vector3 CalcPointOfPlaneIntersect(Vector3 planePoint, Vector3 planeNormal, Vector3 linePoint, Vector3 lineDirection)
        {
            if (Vector3.Dot(planeNormal, lineDirection.normalized) == 0)
            {
                Debug.LogError("Cannot calculate plane intersection from line that is parallel to the plane");
                return Vector3.zero;
            }

            float t = (Vector3.Dot(planeNormal, planePoint) - Vector3.Dot(planeNormal, linePoint)) / Vector3.Dot(planeNormal, lineDirection.normalized);
            return linePoint + (lineDirection.normalized * t);
        }

        /// <summary>
        /// Calculates the distance between two points.
        /// </summary>
        /// <param name="pos1">The first position.</param>
        /// <param name="pos2">The second position.</param>
        /// <returns>The distance between the two points.</returns>
        public static float CalcPointDist(Vector3 pos1, Vector3 pos2)
        {
            return (pos2 - pos1).magnitude;
        }

        /// <summary>
        /// <para>Generates a poission time interval based on the provided rate parameter.</para>
        /// <para>If an event occurs at the end of each newly generated time interval, and a new interval is generated when the event occurs, on average the event will occur at the provided rate.</para>
        /// <br>(e.g. with rate 1/60 - meaning 1 event every 60 units of time).</br>
        /// </summary>
        /// <param name="rate">The rate at which the event will occur on average.</param>
        /// <param name="deviationMultiplier">Multiplier for limiting the extremeties of the time interval generated (e.g. with rate 1/60 and deviation of 0, a time interval of about 60 would always be generated).</param>
        /// <returns>A randomised poission time interval between the last occurence and the next occurence of the event.</returns>
        public static float GeneratePoissonInterval(float rate, float deviationMultiplier = 1f)
        {
            if (deviationMultiplier > 1 || deviationMultiplier < 0)
            {
                Debug.LogWarning("Deviation multiplier must be in the range 0..1");
                deviationMultiplier = Mathf.Clamp(deviationMultiplier, 0, 1);
            }

            float deviationLowerOffset = .5f * (1 - deviationMultiplier) * (1.3f + (deviationMultiplier / 2));
            float deviationUpperOffset = .5f * (1 - deviationMultiplier) / (1.3f + (deviationMultiplier / 2));

            return -Mathf.Log(1 - UnityEngine.Random.Range(deviationLowerOffset, Math.Min(1 - deviationUpperOffset, 0.999999f))) / rate;
        }
    }
}
