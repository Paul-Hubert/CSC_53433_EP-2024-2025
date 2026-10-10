using System.Collections.Generic;
using UnityEngine;

namespace EvoSim
{
    /// <summary>Reads one animal and the world and returns one token from a fixed list (SENSE-01).</summary>
    public abstract class Sense : SpeciesModule
    {
        [SerializeField, Tooltip("The sense's label in the situation text (SENSE-10, SPEC-20). Empty = the sense's default.")]
        protected string label = "";

        /// <summary>The label the brain reads: the inspector's text, or the sense's default.</summary>
        public string Label => string.IsNullOrEmpty(label) ? DefaultLabel : label;

        /// <summary>The reference label of this sense ("Food", "Predator"…).</summary>
        protected virtual string DefaultLabel => GetType().Name.Replace("Sense", "");

        /// <summary>This sense's index in the species' sense order.</summary>
        public int Index { get; internal set; } = -1;

        /// <summary>Every token this sense can return, "none" included. Fixed before the run (SENSE-01).</summary>
        public abstract IReadOnlyList<string> Tokens { get; }

        /// <summary>Read one animal at a decision (SENSE-03).</summary>
        public abstract int Read(Animal a, SenseContext s);

        /// <summary>The text the brain reads for a token (SENSE-10).</summary>
        public virtual string Write(int token, TextStyle style) => $"{Label}: {Tokens[token]}.";

        /// <summary>An attachment (an image…) for this animal, or null (SENSE-40).</summary>
        public virtual object Attachment(Animal a, SenseContext s) => null;

        /// <summary>True if this sense can return attachments (SENSE-41, V-51).</summary>
        public virtual bool HasAttachments => false;

        /// <summary>A cache key for an attachment, or null: then the query is never memoised (SENSE-41).</summary>
        public virtual string AttachmentKey(object attachment) => null;

        /// <summary>Sets the label from code (WorldBuilder, tests).</summary>
        public void SetLabel(string text) => label = text;
    }
}
