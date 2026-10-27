using System;
using UnityEngine;
using UnityEngine.Events;

namespace AtLightsEnd
{
    [CreateAssetMenu(menuName = "At Lights End/Dialogue/Conversation", fileName = "New Conversation")]
    public sealed class DialogueAsset : ScriptableObject
    {
        public DialogueCharacter leftCharacter;
        public DialogueCharacter rightCharacter;
        [Tooltip("Main-character thoughts: blue bubble, room stays visible, no character close-ups.")]
        public bool playerThoughts;
        public enum Speaker { Left, Right }
        [Serializable] public sealed class Line
        {
            public Speaker speaker;
            [TextArea(3, 10)] public string text;
        }
        public Line[] lines = new Line[0];
        // A monologue may leave the unused side empty. Every speaking side must exist.
        public bool IsValid => lines != null && lines.Length > 0 &&
            Array.TrueForAll(lines, l => l != null &&
                (l.speaker == Speaker.Left ? leftCharacter != null : rightCharacter != null));
    }
}
