/* License: NCSA Open Source License
 *
 * Copyright: SPT
 * AUTHORS:
 * Basuro
 */

using System.Text;

namespace SPTarkov.Core.Models;

public class PatchInfo
{
    public const string BYBA = "BYBA";

    public byte[] OriginalChecksum { get; set; }

    public int OriginalLength { get; set; }

    public byte[] PatchedChecksum { get; set; }

    public int PatchedLength { get; set; }

    public PatchItem[] Items { get; set; }

    public static PatchInfo FromBytes(byte[] bytes)
    {
        if (bytes.Length < 82)
        {
            throw new Exception("Input data too short, cannot be a valid patch");
        }

        var pi = new PatchInfo();

        using (var ms = new MemoryStream(bytes))
        using (var br = new BinaryReader(ms))
        {
            byte[] buf = null;

            buf = br.ReadBytes(4);
            if (Encoding.ASCII.GetString(buf) != BYBA)
            {
                throw new Exception("Invalid identifier");
            }

            if (br.ReadByte() != 1)
            {
                throw new Exception("Invalid major file version (1 expected)");
            }

            if (br.ReadByte() != 0)
            {
                throw new Exception("Invalid minor file version (0 expected)");
            }

            pi.OriginalLength = br.ReadInt32();
            pi.OriginalChecksum = br.ReadBytes(32);
            pi.PatchedLength = br.ReadInt32();
            pi.PatchedChecksum = br.ReadBytes(32);

            var itemCount = br.ReadInt32();

            var items = new List<PatchItem>();
            for (var i = 0; i < itemCount; i++)
            {
                items.Add(PatchItem.FromReader(br));
            }

            pi.Items = items.ToArray();
        }

        return pi;
    }

    public byte[] ToBytes()
    {
        byte[] data;

        using (var ms = new MemoryStream())
        {
            using (var bw = new BinaryWriter(ms, Encoding.ASCII, true))
            {
                // identifier "BYBA" // 4B
                var byba = Encoding.ASCII.GetBytes(BYBA);
                bw.Write(byba, 0, byba.Length);

                // version "1.0" // 2B
                bw.Write((byte) 1);
                bw.Write((byte) 0);

                // original len // 4B
                bw.Write(OriginalLength);

                // original chk // 32B
                bw.Write(OriginalChecksum, 0, OriginalChecksum.Length);

                // patched len // 4B
                bw.Write(PatchedLength);

                // patched chk // 32B
                bw.Write(PatchedChecksum, 0, PatchedChecksum.Length);

                // item count // 4B
                bw.Write(Items.Length);

                // data
                foreach (var pi in Items)
                {
                    pi.ToWriter(bw);
                }
            }

            data = new byte[ms.Length];
            ms.Seek(0, SeekOrigin.Begin);
            ms.Read(data, 0, (int) ms.Length);
        }

        return data;
    }
}
