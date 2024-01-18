using System.Runtime.CompilerServices;
using Unity.Collections;
using System;
using System.Collections.Generic;

[assembly: InternalsVisibleTo("Dolby.Millicast.RuntimeTests")]
namespace Dolby.Millicast {
  /// <summary>
  /// Purpose of this class is to code information provided
  /// into or extract out of data structures. 
  /// </summary>
  /// 
  public class FrameTransformerCoder {
    internal static readonly UInt32 MAGIC_START_VALUE = 0xCAFEBABE;
    internal static byte[] MAGIC_BYTES {
      get {
        var magicBytes = BitConverter.GetBytes(MAGIC_START_VALUE);
        if (BitConverter.IsLittleEndian) {
          Array.Reverse(magicBytes);
        }
        return magicBytes;
      }
    }


    /// <summary>
    /// Encodes metadata along with a frame. 
    /// </summary>
    /// <param name="frame"> The original frame data</param>
    /// <param name="data"> new incoming metadata</param>
    /// <param name="destination"> final storage container</param>
    /// <returns>Total length of data</returns>
    public static int EncodeData(NativeArray<byte>.ReadOnly frame,
      byte[] metadata,
      ref NativeArray<byte> destination) {
      // metadata
      var metadataArray = metadata;
      // based on this schema: frameData | START_VALUE=0xCAFEBABE | metaData | 4 bytes = metaDataLength
      int length = 2 * sizeof(UInt32) + metadataArray.Length + frame.Length;
      if (length > destination.Length) {
        if (destination.IsCreated) {
          destination.Dispose();
        }
        destination = new NativeArray<byte>(length, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
      }
      // encoded data
      NativeArray<byte>.Copy(src: frame, srcIndex: 0, dst: destination, dstIndex: 0, length: frame.Length);

      // magic value (endian independent)
      NativeArray<byte>.Copy(src: MAGIC_BYTES, srcIndex: 0, dst: destination, dstIndex: frame.Length, length: sizeof(UInt32));

      // metadata
      NativeArray<byte>.Copy(src: metadataArray, srcIndex: 0, dst: destination, dstIndex: frame.Length + sizeof(UInt32), length: metadataArray.Length);

      // metadata length (4 bytes) in big endian
      var metadataLength = BitConverter.GetBytes(metadataArray.Length);
      if (BitConverter.IsLittleEndian)
        Array.Reverse(metadataLength);
      NativeArray<byte>.Copy(src: metadataLength, srcIndex: 0, dst: destination, dstIndex: length - sizeof(UInt32), length: sizeof(UInt32));

      return length;
    }

    /// <summary>
    /// Decodes (Extracts) data from the encoded frame buffer. 
    /// </summary>
    /// <param name="frame"> The transformed frame with metadata</param>
    /// <param name="destination">A container to hold the metadata after extraction</param>
    /// <returns>The length of the original frame or -1 in case of issue </returns>
    public static int DecodeData(NativeArray<byte>.ReadOnly frame, ref NativeArray<byte> destination) {
      if (frame.Length == 0) return -1;

      // Grab the metadata length which is the last 4 bytes
      var metadataLengthStartIdx = frame.Length - sizeof(UInt32);
      if (metadataLengthStartIdx <= 0) return -1;
      UInt32 metadataLength = ExtractIntegerLittleEndian(frame, index: metadataLengthStartIdx);
      if (metadataLength == 0 || metadataLength > frame.Length) {
        return -1;
      }

      // Now grab the magic value and validate.
      int magicValueStartIdx = (int)(frame.Length - 2 * sizeof(UInt32) - metadataLength);
      if (magicValueStartIdx <= 0 || metadataLengthStartIdx >= frame.Length) return -1;
      UInt32 magicValue = ExtractIntegerLittleEndian(frame, index: magicValueStartIdx);
      if (magicValue != MAGIC_START_VALUE) {
        return -1;
      }

      // We are good at this point, so straight up extract the data but remember to escape.
      int metadataStartIdx = magicValueStartIdx + sizeof(UInt32);
      destination = new NativeArray<byte>(Convert.ToInt32(metadataLength), Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
      byte[] metadata = new byte[metadataLength];
      NativeArray<byte>.Copy(src: frame, srcIndex: metadataStartIdx, dst: destination, dstIndex: 0, length: metadata.Length);
      return (int)(frame.Length - metadataLength - 2 * sizeof(UInt32));
    }

    private static UInt32 ExtractIntegerLittleEndian(NativeArray<byte>.ReadOnly data, int index) {
      // Grab the metadata length which is the last 4 bytes
      byte[] integerBytes = new byte[sizeof(UInt32)];
      NativeArray<byte>.Copy(src: data, srcIndex: index, dst: integerBytes, dstIndex: 0, length: sizeof(UInt32));
      if (BitConverter.IsLittleEndian) {
        Array.Reverse(integerBytes);
      }
      return BitConverter.ToUInt32(integerBytes);
    }

    /// <summary>
    /// Escape NAL emulation bytes. Based on section 7.4.1 of the H264 spec.
    /// </summary>
    /// <param name="data"></param>
    /// <param name="length"></param>
    /// <returns></returns>
    internal static int EscapeEmulationBytes(NativeArray<byte>.ReadOnly data, int start, int totalLength, ref NativeArray<byte> destination) {
      if (totalLength > data.Length) throw new Exception("Invalid length provided.");
      // Methods for parsing and writing RBSP. See section 7.4.1 of the H264 spec.
      //
      // The following sequences are illegal, and need to be escaped when encoding:
      // 00 00 00 -> 00 00 03 00
      // 00 00 01 -> 00 00 03 01
      // 00 00 02 -> 00 00 03 02
      // And things in the source that look like the emulation byte pattern (00 00 03)
      // need to have an extra emulation byte added, so it's removed when decoding:
      // 00 00 03 -> 00 00 03 03
      //
      // Decoding is simply a matter of finding any 00 00 03 sequence and removing
      // the 03 emulation byte.

      // Possible end values for emulation sequences
      byte[] possible_end_vals = new byte[] { 0x01, 0x02, 0x03 };
      // Find emulation bytes 
      List<int> emulationBytesEndIndex = new List<int>();
      for(int i = start + 2; i < totalLength; i++) {
        if (Array.BinarySearch(possible_end_vals, 0, possible_end_vals.Length, data[i]) >= 0) {
          // Check the prior two vals
          if (data[i-1] == 0 && data[i-2] == 0) {
            // Got our emulation byte sequence, store the start index.
            emulationBytesEndIndex.Add(i);
          }
        }
      }
      // Now we every 3 sequence byte is escaped as 4 bytes so basically 
      // we need to expand the data array byte by emulationBytesEndIndex.count
      if (destination.Length < totalLength + emulationBytesEndIndex.Count) {
        if (destination.IsCreated) {
          destination.Dispose();
        }
        destination = new NativeArray<byte>(totalLength + emulationBytesEndIndex.Count, Allocator.Persistent, NativeArrayOptions.ClearMemory);
      }
      // Copy everything until start
      NativeArray<byte>.Copy(src: data, srcIndex: 0, dst: destination, dstIndex: 0, length: start);

      int srcStartIndex = start;
      int dstStartIndex = start;
      foreach (var endIndex in emulationBytesEndIndex) {
        var l = endIndex - srcStartIndex;
        NativeArray<byte>.Copy(src: data, srcIndex: srcStartIndex, dst: destination, dstIndex: dstStartIndex, length: l);
        NativeArray<byte>.Copy(src: new byte[] { 0x03 }, srcIndex: 0, dst: destination, dstIndex: dstStartIndex + l , length: 1);
        NativeArray<byte>.Copy(src: new byte[] { data[endIndex] }, srcIndex: 0, dst: destination, dstIndex: dstStartIndex + l + 1, length: 1);
        srcStartIndex = endIndex + 1;
        dstStartIndex += l + 2;
      }

      // Copy the rest of the data after the last index as is.
      if (srcStartIndex < totalLength) {
        NativeArray<byte>.Copy(src: data, srcIndex: srcStartIndex, dst: destination, dstIndex: dstStartIndex, length: totalLength - srcStartIndex);
      }
      return totalLength + emulationBytesEndIndex.Count;
    }

    public static int DeEscapeEmulationBytes(NativeArray<byte>.ReadOnly data, int start, int totalLength, ref NativeArray<byte> destination) {
      // Find emulation bytes 
      List<int> emulationBytesEndIndex = new List<int>();
      for (int i = start + 2; i < totalLength; i++) {
        // Check the prior two vals
        if (data[i] == 0x03 && data[i - 1] == 0x00 && data[i - 2] == 0x00) {
          // Got our escaped emulation byte sequence, store the start index.
          emulationBytesEndIndex.Add(i);
        }
      }

      if (destination.Length < totalLength - emulationBytesEndIndex.Count) {
        if (destination.IsCreated) {
          destination.Dispose();
        }
        destination = new NativeArray<byte>(totalLength - emulationBytesEndIndex.Count, Allocator.Persistent, NativeArrayOptions.ClearMemory);
      }

      // Copy everything until start
      NativeArray<byte>.Copy(src: data, srcIndex: 0, dst: destination, dstIndex: 0, length: start);

      int srcStartIndex = start;
      int dstStartIndex = start;
      foreach (var endIndex in emulationBytesEndIndex) {
        var l = endIndex - srcStartIndex;
        NativeArray<byte>.Copy(src: data, srcIndex: srcStartIndex, dst: destination, dstIndex: dstStartIndex, length: l);
        NativeArray<byte>.Copy(src: new byte[] { data[endIndex+1] }, srcIndex: 0, dst: destination, dstIndex: dstStartIndex + l, length: 1);
        srcStartIndex = endIndex + 2;
        dstStartIndex += l + 1;
      }

      // Copy the rest of the data after the last index as is.
      if (srcStartIndex < totalLength) {
        NativeArray<byte>.Copy(src: data, srcIndex: srcStartIndex, dst: destination, dstIndex: dstStartIndex, length: totalLength - srcStartIndex);
      }
      return totalLength - emulationBytesEndIndex.Count;
    }
  }
}