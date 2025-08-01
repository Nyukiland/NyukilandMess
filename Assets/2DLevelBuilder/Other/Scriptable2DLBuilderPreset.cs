using UnityEngine;

namespace SLBuilder
{
    public class Scriptable2DLBuilderPreset : ScriptableObject
    {
        public enum ColliderType
        {
            none,
            BoxCollider,
            CircleCollider,
            PolygonCollider
        }

        public string presetName = "name";

        public Sprite imageToWorkWith;
        public ColliderType colType;

        public Vector2 startAnchor;
        public Vector2 endAnchor;

        public bool scaleAllAxis;
        public string folderName = "Default";

        public float offsetX, offsetY, scaleFactX, scaleFactY, globalScaleFact;

        public void SetImageFactor()
        {
            Vector2 imageCenter = imageToWorkWith.pivot / imageToWorkWith.rect.size;

            float height = imageToWorkWith.rect.height;
            float width = imageToWorkWith.rect.width;

            offsetX = (width / imageToWorkWith.pixelsPerUnit) * (imageCenter.x - startAnchor.x);
            offsetY = (height / imageToWorkWith.pixelsPerUnit) * (imageCenter.y - startAnchor.y);

            scaleFactX = ((imageToWorkWith.pixelsPerUnit / width) / Mathf.Abs(endAnchor.x - startAnchor.x));
            scaleFactY = ((imageToWorkWith.pixelsPerUnit / height) / Mathf.Abs(endAnchor.y - startAnchor.y));


            float temp = height > width ? height : width;
            globalScaleFact = (imageToWorkWith.pixelsPerUnit / temp) / Mathf.Abs(Vector2.Distance(endAnchor, startAnchor));
        }
    }
}
