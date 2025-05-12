using UnityEngine;

[CreateAssetMenu(fileName = "TutorialDatabase", menuName = "ScriptableObjects/Tutorial/TutorialDatabase")]
public class TutorialDatabase : ScriptableObject
{
    [SerializeField]
    [Header("チュートリアルデータ")]
    private TutorialData[] tutorials;

    public TutorialData[] GetTutorials => tutorials;
}