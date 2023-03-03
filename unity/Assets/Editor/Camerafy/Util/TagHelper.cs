using UnityEngine;
using UnityEditor;

namespace Camerafy.Editor.Util
{
    public static class TagHelper
    {
        ///-------------------------------------------------------------------------------------------------
        /// Summary:    Adds a tag to the Unity tag system if its not already in there.
        ///
        /// Author: https://answers.unity.com/questions/33597/is-it-possible-to-create-a-tag-programmatically.html
        ///
        /// Date:   3/02/2020
        ///
        ///-------------------------------------------------------------------------------------------------

        public static string AddTag(string InTag)
        {
            Object[] asset = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
            if ((asset != null) && (asset.Length > 0))
            {
                SerializedObject so = new SerializedObject(asset[0]);
                SerializedProperty tags = so.FindProperty("tags");

                for (int i = 0; i < tags.arraySize; ++i)
                {
                    if (tags.GetArrayElementAtIndex(i).stringValue == InTag)
                    {
                        return InTag;
                    }
                }

                tags.InsertArrayElementAtIndex(0);
                tags.GetArrayElementAtIndex(0).stringValue = InTag;
                so.ApplyModifiedProperties();
                so.Update();
            }

            return InTag;
        }
    }
}
