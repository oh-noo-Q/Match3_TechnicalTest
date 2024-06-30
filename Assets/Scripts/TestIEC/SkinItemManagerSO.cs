using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(menuName = "SO/SkinItem")]
public class SkinItemManagerSO : ScriptableObject
{
    public SkinItem[] skinItems;
}

[System.Serializable]
public class SkinItem
{
    public TypeSkin type;
    public Sprite[] sprites;
}

public enum TypeSkin
{
    NORMAL,
    FISH,
}
