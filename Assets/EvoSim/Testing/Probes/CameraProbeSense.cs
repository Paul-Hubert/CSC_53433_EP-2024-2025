using System.Collections.Generic;

namespace EvoSim.Testing
{
    /// <summary>A test sense that attaches an "image" (a byte array) with no cache key (T-SENSE-10, S24).</summary>
    public class CameraProbeSense : Sense
    {
        readonly List<string> tokens = new List<string> { "seen" };
        public bool GiveKey;
        public override IReadOnlyList<string> Tokens => tokens;
        protected override string DefaultLabel => "Eyes";
        public override bool HasAttachments => true;
        public override int Read(Animal a, SenseContext s) => 0;
        public override object Attachment(Animal a, SenseContext s) => new byte[64 * 64];
        public override string AttachmentKey(object attachment) => GiveKey ? "image" : null;
    }
}
