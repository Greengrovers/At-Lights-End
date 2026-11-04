using UnityEngine;

namespace AtLightsEnd
{
    [CreateAssetMenu(menuName = "At Lights End/Dialogue/Character", fileName = "New Character")]
    public sealed class DialogueCharacter : ScriptableObject
    {
        public string displayName;
        public bool isPlayerCharacter;
        public string DisplayName => isPlayerCharacter && PlayerSession.HasPlayerName ? PlayerSession.PlayerName : displayName;
        public Sprite portrait;
        [Tooltip("Find the visible area of the assigned sprite automatically. Disable to use Portrait Region manually.")]
        public bool autoPortraitRegion = true;
        [Range(0, 0.6f), Tooltip("Fraction of the lower portrait omitted for the dialogue close-up.")]
        public float closeUpBottomCrop = 0.30f;
        [Tooltip("Visible character region, in normalized texture coordinates. Leaves source art unchanged.")]
        public Rect portraitRegion = new Rect(0, 0, 1, 1);
        public Color nameColor = new Color(0.36f, 0.25f, 0.29f);
    }
}
