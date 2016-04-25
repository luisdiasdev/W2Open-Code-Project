using System.Runtime.InteropServices;

namespace W2Open.Common.GameStructure
{
    /// <summary>
    /// All the packet structures must implement this interface.
    /// </summary>
    public interface IGamePacket
    {
        MPacketHeader Header { get; set; }
    }

    /// <summary>
    /// Header present in all the valid game packets.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = ProjectBasics.DEFAULT_PACK)]
    public struct MPacketHeader
    {
        public ushort Size;

        public byte Key;
        public byte CheckSum;

        public ushort Opcode;
        public ushort ClientId;

        public uint TimeStamp;
    }
}