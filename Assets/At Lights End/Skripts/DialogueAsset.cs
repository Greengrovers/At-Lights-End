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
        public DialogueCharacter middleCharacter;
        public bool narrator;
        [Tooltip("Main-character thoughts: blue bubble, room stays visible, no character close-ups.")]
        public bool playerThoughts;
        // Keep existing serialized Left=0 and Right=1 assignments intact.
        public enum Speaker { Left = 0, Right = 1, Middle = 2 }
        public enum LineKind { Default, PlayerThought, Narrator }
        [Serializable] public sealed class Line
        {
            public Speaker speaker;
            public LineKind kind;
            [TextArea(3, 10)] public string text;
            [Tooltip("Optional scene video event ID, e.g. cat-shadow. Plays when this line appears.")]
            public string videoEventId;
        }
        public Line[] lines = new Line[0];
        public DialogueCharacter CharacterFor(Speaker side) => side == Speaker.Left ? leftCharacter :
            side == Speaker.Right ? rightCharacter : middleCharacter;
        public bool IsNarrator(Line line) => line.kind == LineKind.Narrator || (line.kind == LineKind.Default && narrator);
        public bool IsThought(Line line) => line.kind == LineKind.PlayerThought ||
            (line.kind == LineKind.Default && !narrator && playerThoughts);
        public bool IsValid => lines != null && lines.Length > 0 &&
            Array.TrueForAll(lines, l => l != null &&
                (IsNarrator(l) || IsThought(l) || CharacterFor(l.speaker) != null));
    }
}
