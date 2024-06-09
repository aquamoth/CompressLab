/// <summary>
/// 
/// </summary>
/// <param name="MaxBlockSize">Should only be overridden by unit tests.</param>
public sealed class RleEncoder(ushort MaxBlockSize = ushort.MaxValue)
{
    const byte RLE_ENCODING_SAME = 0;
    const byte RLE_ENCODING_DIFFERENT = 1;
    const ushort RLE_REQUIRED_REPEAT_COUNT = 7;

    public List<byte> Decode(byte[] sourceArray)
    {
        ReadOnlySpan<byte> source = sourceArray;

        var outputLength = BitConverter.ToInt32(source[0..4]);
        var output = new List<byte>(outputLength);
        source = source[4..];

        while (source.Length > 0)
        {
            var encoding = source[0];
            if (encoding == RLE_ENCODING_SAME)
            {
                var count = BitConverter.ToUInt16(source[1..3]);
                var value = source[3];
                output.AddRange(Enumerable.Repeat(value, count));

                source = source[4..];
            }
            else if (encoding == RLE_ENCODING_DIFFERENT)
            {
                var count = BitConverter.ToUInt16(source[1..3]);
                output.AddRange(source[3..(3 + count)]);

                source = source[(3 + count)..];
            }
            else
            {
                throw new InvalidOperationException("Invalid encoding");
            }
        }

        return output;
    }

    public IEnumerable<byte> Encode(byte[] sourceArray)
    {
        ReadOnlySpan<byte> source = sourceArray;

        var output = new List<byte>(sourceArray.Length / 2); //Guess at compression ratio for initial capacity
        output.AddRange(BitConverter.GetBytes(source.Length));

        while (source.Length > 0)
        {
            ushort nextSearchStart = 0;
            bool isRleBlockFound = false;

            var blockSize = (ushort)Math.Min(source.Length, MaxBlockSize);

            var rleSearchEnd = blockSize - RLE_REQUIRED_REPEAT_COUNT;
            while (!isRleBlockFound && nextSearchStart <= rleSearchEnd)
            {
                //Identify the next RLE block
                var rleTriggerIndex = nextSearchStart + RLE_REQUIRED_REPEAT_COUNT;

                var firstRleCandidate = source[nextSearchStart];
                ushort searchIndex = (ushort)(nextSearchStart + 1); //TODO: Ensure we don't go past MaxBlockSize
                while (searchIndex < rleTriggerIndex && source[searchIndex] == firstRleCandidate)
                {
                    searchIndex++;
                }

                if (searchIndex == rleTriggerIndex)
                {
                    //This will break the loop AND write the entire RLE block below
                    isRleBlockFound = true;
                }
                else
                {
                    //No RLE in search scope. We skip past the searched range and try again
                    nextSearchStart = searchIndex;
                }
            }

            if (!isRleBlockFound)
            {
                if (blockSize == source.Length)
                {
                    //We didn't find an RLE block, and we are at the end of the entire source.
                    //We'll write the entire block uncompressed
                    //This also ensures {(blockSize - 2) > 0 -> ushort} in the cast below
                    nextSearchStart = blockSize;
                }
                else
                {
                    //We can include the rest of the bytes in the block that are obviously not part of an RLE
                    var lastByteInBlock = source[blockSize - 1];
                    for (ushort i = (ushort)(blockSize - 2); i > nextSearchStart; i--)
                    {
                        if (source[i] != lastByteInBlock)
                        {
                            nextSearchStart = (ushort)(i + 1);
                            break;
                        }
                    }
                }
            }


            //Write the next block of uncompressed bytes
            if (nextSearchStart > 0)
            {
                output.Add(RLE_ENCODING_DIFFERENT);
                output.AddRange(BitConverter.GetBytes(nextSearchStart));
                output.AddRange(source[..nextSearchStart]);

                source = source[nextSearchStart..];
                blockSize = (ushort)Math.Min(source.Length, MaxBlockSize);
            }


            //Write the entire RLE block
            if (isRleBlockFound)
            {
                var rleByte = source[0];
                ushort count = RLE_REQUIRED_REPEAT_COUNT; //We already know from above these bytes are the same
                while (count < blockSize && source[count] == rleByte)
                {
                    count++;
                }

                //Write the block of RLE bytes
                output.Add(RLE_ENCODING_SAME);//TODO: Use flags for short and long encoding blocks
                output.AddRange(BitConverter.GetBytes(count));
                output.Add(rleByte);

                source = source[count..];
            }
        }

        return output;
    }
}
