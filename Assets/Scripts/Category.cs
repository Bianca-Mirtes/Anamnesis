using System;
using UnityEngine;

[CreateAssetMenu(fileName = "Category", menuName = "Scriptable Objects/Category")]
public class Category : ScriptableObject
{
    public string name;
    public World[] world;
}

[Serializable]
public class World
{
    public Sprite image;
    public string id;
    public Texture2D[] faces;
}
