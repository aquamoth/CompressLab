using Xunit;

public class RleEncoderTests
{
    [Theory]
    [InlineData("", 4, ushort.MaxValue)]
    [InlineData("CCCCCCCCCCCCC", 8, ushort.MaxValue)]
    [InlineData("AAAAACCCCCCCCCCCCC", 16, ushort.MaxValue)]
    [InlineData("ABCCCCCCCCCCDE", 18, ushort.MaxValue)]
    [InlineData("abcdeabcdfffffffffff", 20, 12)]
    public void TestEncodeDecode(string plain, int expectedCompressedLength, ushort maxBlockSize)
    {
        var encoder = new RleEncoder(maxBlockSize); 
        var expectedBytes = System.Text.Encoding.UTF8.GetBytes(plain);

        var compressedBytes = encoder.Encode(expectedBytes).ToArray();
        Assert.Equal(expectedCompressedLength, compressedBytes.Length);

        var actualBytes = encoder.Decode(compressedBytes);
        Assert.Equal(expectedBytes, actualBytes);
    }
}