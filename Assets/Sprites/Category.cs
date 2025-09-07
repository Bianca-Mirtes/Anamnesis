using UnityEngine;

[CreateAssetMenu(fileName = "Category", menuName = "Scriptable Objects/Category")]
public class Category : ScriptableObject
{
    public string name;
    public Sprite[] images;
}
