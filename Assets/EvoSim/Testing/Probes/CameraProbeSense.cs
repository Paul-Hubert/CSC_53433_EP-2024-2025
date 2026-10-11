using System.Collections.Generic;

namespace EvoSim.Testing
{
    /// <summary>
    /// A test sense that attaches an "image" (a byte array) to every query, with no cache key unless asked (T-SENSE-10,
    /// S24): such queries are never memoised or cached (SENSE-41), and a brain that takes no attachments fails validation
    /// (V-51).
    /// </summary>
    public class CameraProbeSense : Sense
    {
        readonly List<string> tokens = new List<string> { "seen" };   // backs Tokens: one token, always read
        /// <summary>True: <see cref="AttachmentKey"/> gives the key "image", so equal queries may be memoised again.</summary>
        public bool GiveKey;

        /// <summary>One token, "seen".</summary>
        public override IReadOnlyList<string> Tokens => tokens;

        /// <summary>"Eyes".</summary>
        protected override string DefaultLabel => "Eyes";

        /// <summary>True: this sense attaches an image to each query.</summary>
        public override bool HasAttachments => true;

        /// <summary>Always "seen".</summary>
        /// <inheritdoc/>
        public override int Read(Animal a, SenseContext s) => 0;

        /// <summary>A new 64 × 64 byte array, standing for a camera image.</summary>
        /// <inheritdoc/>
        public override object Attachment(Animal a, SenseContext s) => new byte[64 * 64];

        /// <summary>"image" when <see cref="GiveKey"/> is set, else null (no key: the query is never memoised).</summary>
        /// <inheritdoc/>
        public override string AttachmentKey(object attachment) => GiveKey ? "image" : null;
    }
}
