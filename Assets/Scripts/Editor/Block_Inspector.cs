namespace Editor.CustomInspector
{
    using Game.DataAssets;

    using UnityEditor;
    using UnityEditor.UIElements;
    using UnityEngine.UIElements;

    [CustomEditor(typeof(Block))]
    public class Block_Inspector : Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            return base.CreateInspectorGUI();
            //// Create a new VisualElement to be the root of our inspector UI
            //VisualElement myInspector = new VisualElement();

            //// Add a simple label
            //myInspector.Add(new Label("This is a custom inspector"));

            //// Return the finished inspector UI
            //return myInspector;
        }
    }
}
