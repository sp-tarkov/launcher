/* License: NCSA Open Source License
 *
 * Copyright: SPT
 * AUTHORS:
 * Basuro
 */

namespace SPTarkov.Core.Models;

public class PatchItem
{
    public int Offset { get; set; }

    public byte[] Data { get; set; }

    public static PatchItem FromReader(BinaryReader br)
    {
        var offset = br.ReadInt32();
        var dataLength = br.ReadInt32();
        var data = br.ReadBytes(dataLength);

        return new PatchItem
        {
            Offset = offset,
            Data = data
        };
    }

    internal void ToWriter(BinaryWriter bw)
    {
        // offset // 4B
        bw.Write(Offset);

        // length // 4B
        bw.Write(Data.Length);

        // data // xB
        bw.Write(Data, 0, Data.Length);
    }
}
