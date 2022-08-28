namespace Game.Utility
{
    using System.Linq;
    using UnityEngine;

    public static class Raycasting
    {
        public static bool CalculateMouseWorldIntersect(Vector2 mousePos, out RaycastHit hitInfo, int maxRayDistance = 200)
        {
            Ray pointerRay = Camera.main.ScreenPointToRay(mousePos);
            if (Physics.Raycast(pointerRay, out hitInfo, maxRayDistance))
            {
                return true;
            }

            return false;
        }

        public static bool CalculateMouseWorldIntersect(Vector2 mousePos, out RaycastHit hitInfo, int layermask = ~0, int maxRayDistance = 200)
        {
            Ray pointerRay = Camera.main.ScreenPointToRay(mousePos);
            if (Physics.Raycast(pointerRay, out hitInfo, maxRayDistance, layermask))
            {
                return true;
            }

            return false;
        }

        public static bool CalculateMouseWorldIntersect(Vector2 mousePos, out RaycastHit hitInfo, string[] tagFilter, int layermask = ~0, int maxRayDistance = 200)
        {
            Ray pointerRay = Camera.main.ScreenPointToRay(mousePos);

            if (Physics.Raycast(pointerRay, out hitInfo, maxRayDistance, layermask))
            {
                RaycastHit tempInfo = hitInfo;
                return tagFilter.Any(tag => tempInfo.collider.gameObject.CompareTag(tag));
            }

            return false;
        }

        /// <summary>
        /// Calculate where the mouse pointer intersects with a virtual plane.
        /// </summary>
        /// <param name="mousePos">the screen position of the mouse.</param>
        /// <returns>The position on the plane that is underneath the mouse pointer..</returns>
        public static Vector3 CalculateMousePlaneInstersect(Vector2 mousePos, Vector3 planePos, Vector3 planeNormal)
        {
            Ray pointerRay = Camera.main.ScreenPointToRay(mousePos);
            return GameMath.CalcPointOfPlaneIntersect(planePos, planeNormal, pointerRay.origin, pointerRay.direction);
        }
    }
}
