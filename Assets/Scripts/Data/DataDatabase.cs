using UnityEngine;

[CreateAssetMenu(
    menuName = "Abondon/Data Database",
    fileName = "DataDatabase"
)]
public class DataDatabase : ScriptableObject
{
    public DataItem[] allItems;
}
