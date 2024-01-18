
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.TestTools;
using Unity.Collections;
using Dolby.Millicast;
using System;
using System.Collections.Generic;
using UnityEngine.XR;
using System.Text;
using static UnityEngine.GraphicsBuffer;
using System.Linq;
using NUnit.Framework.Internal;

namespace Dolby.Millicast.RuntimeTests {
  class FrameTransformerTest {
    [TestCase(1)]
    [TestCase(8)]
    [TestCase(1000)]
    [TestCase(10000)]
    public void FrameTransformCoderWorksBidirectional(int length) {
      var data = new NativeArray<byte>(length, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
      var metadata = 0xDEADBEEF;
      NativeArray<byte> destination = new NativeArray<byte>(length, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
      var totalLength = FrameTransformerCoder.EncodeData(data.AsReadOnly(), BitConverter.GetBytes(metadata), ref destination);
      Assert.That(totalLength, Is.EqualTo(3 * sizeof(UInt32) + length));
      Assert.That(totalLength, Is.GreaterThan(length));
      Assert.That(totalLength, Is.EqualTo(destination.Length));
      var extractedMetadata = new NativeArray<byte>(0, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
      var originalLength = FrameTransformerCoder.DecodeData(destination.AsReadOnly(), destination: ref extractedMetadata);
      Assert.That(originalLength, Is.EqualTo(length));
      Assert.That(BitConverter.ToUInt32(extractedMetadata.ToArray()), Is.EqualTo(metadata));
    }

    [Test]
    public void FrameTransformCoderWorksHandlesIncorrectMetadataLength() {
      var data = new NativeArray<byte>(1000, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
      UInt32 metadataLength = 10000;
      var metadataLengthBytes = BitConverter.GetBytes(metadataLength);
      if (BitConverter.IsLittleEndian) Array.Reverse(metadataLengthBytes);
      NativeArray<byte>.Copy(src: metadataLengthBytes, srcIndex: 0, dst: data, dstIndex: data.Length - sizeof(UInt32), length: sizeof(UInt32));
      var extractedMetadata = new NativeArray<byte>(0, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
      var originalLength = FrameTransformerCoder.DecodeData(data.AsReadOnly(), destination: ref extractedMetadata);
      Assert.That(originalLength, Is.EqualTo(-1));
    }

    [Test]
    public void FrameTransformCoderWorksHandlesIncorrectMagicValue() {
      var data = new NativeArray<byte>(1000, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
      UInt32 metadataLength = 500;
      var metadataLengthBytes = BitConverter.GetBytes(metadataLength);
      if (BitConverter.IsLittleEndian) Array.Reverse(metadataLengthBytes);

      NativeArray<byte>.Copy(src: metadataLengthBytes, srcIndex: 0, dst: data, dstIndex: data.Length - sizeof(UInt32), length: sizeof(UInt32));
      var extractedMetadata = new NativeArray<byte>(0, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
      var originalLength = FrameTransformerCoder.DecodeData(data.AsReadOnly(), destination: ref extractedMetadata);
      Assert.That(originalLength, Is.EqualTo(-1));
    }

    [Test]
    [TestCase(new byte[] { 0x00, 0x00, 0x01 },
              new byte[] { 0x00, 0x00, 0x03, 0x01 })]
    [TestCase(new byte[] { 0x00, 0x00, 0x02 },
              new byte[] { 0x00, 0x00, 0x03, 0x02 })]
    [TestCase(new byte[] { 0x00, 0x00, 0x03 },
              new byte[] { 0x00, 0x00, 0x03, 0x03 })]
    [TestCase(new byte[] { 0x00, 0x00, 0x01, 0x00, 0x02, 0x00, 0x00, 0x03 },
              new byte[] { 0x00, 0x00, 0x03, 0x01, 0x00, 0x02, 0x00, 0x00, 0x03, 0x03 })]
    [TestCase(new byte[] { 0x00, 0x00, 0x02, 0x00, 0x00, 0x02 },
              new byte[] { 0x00, 0x00, 0x02, 0x00, 0x00, 0x03, 0x02 }, 3  )]
    [TestCase(new byte[] { 0x00, 0x02 },
              new byte[] { 0x00, 0x02 })]
    [TestCase(new byte[] { 0x00 },
              new byte[] { 0x00 })]
    [TestCase(new byte[] { 0x00, 0x01, 0x02, 0x03, 0x04 },
        new byte[] { 0x00, 0x01, 0x02, 0x03, 0x04 })]
    public void FrameTransformCoderTestEscapingWorks(byte[] originalArray, byte[] encodedArray, int start = 0) {
      NativeArray<byte> originalNativeArray = new NativeArray<byte>(originalArray.Length, Allocator.Temp, NativeArrayOptions.ClearMemory);
      NativeArray<byte>.Copy(src: originalArray, srcIndex: 0, dst: originalNativeArray, dstIndex: 0, length: originalArray.Length);

      NativeArray<byte> destination = new NativeArray<byte>(1, Allocator.Temp, NativeArrayOptions.ClearMemory);
      var totalLength = FrameTransformerCoder.EscapeEmulationBytes(originalNativeArray.AsReadOnly(), start: start, totalLength: originalNativeArray.Length, ref destination);
      Assert.That(Enumerable.SequenceEqual(encodedArray, destination.ToArray()), Is.True);
    }

    [Test]
    [TestCase(new byte[] { 0x00, 0x00, 0x01 },
        new byte[] { 0x00, 0x00, 0x03, 0x01 })]
    [TestCase(new byte[] { 0x00, 0x00, 0x02 },
        new byte[] { 0x00, 0x00, 0x03, 0x02 })]
    [TestCase(new byte[] { 0x00, 0x00, 0x03 },
        new byte[] { 0x00, 0x00, 0x03, 0x03 })]
    [TestCase(new byte[] { 0x00, 0x00, 0x01, 0x00, 0x02, 0x00, 0x00, 0x03 },
        new byte[] { 0x00, 0x00, 0x03, 0x01, 0x00, 0x02, 0x00, 0x00, 0x03, 0x03 })]
    [TestCase(new byte[] { 0x00, 0x00, 0x02, 0x00, 0x00, 0x02 },
        new byte[] { 0x00, 0x00, 0x02, 0x00, 0x00, 0x03, 0x02 }, 3)]
    [TestCase(new byte[] { 0x00, 0x02 },
        new byte[] { 0x00, 0x02 })]
    [TestCase(new byte[] { 0x00 },
        new byte[] { 0x00 })]
    [TestCase(new byte[] { 0x00, 0x01, 0x02, 0x03, 0x04 },
        new byte[] { 0x00, 0x01, 0x02, 0x03, 0x04 })]
    public void FrameTransformCoderTestDeEscapingWorks(byte[] originalArray, byte[] encodedArray, int start = 0) {
      NativeArray<byte> encodedNativeArray = new NativeArray<byte>(encodedArray.Length, Allocator.Temp, NativeArrayOptions.ClearMemory);
      NativeArray<byte>.Copy(src: encodedArray, srcIndex: 0, dst: encodedNativeArray, dstIndex: 0, length: encodedArray.Length);

      NativeArray<byte> destination = new NativeArray<byte>(1, Allocator.Temp, NativeArrayOptions.ClearMemory);
      var totalLength = FrameTransformerCoder.DeEscapeEmulationBytes(encodedNativeArray.AsReadOnly(), start: start, totalLength: encodedNativeArray.Length, ref destination);
      Assert.That(Enumerable.SequenceEqual(originalArray, destination.ToArray()), Is.True);
    }

    [Test]
    [TestCase(1)]
    [TestCase(10)]
    [TestCase(100)]
    [TestCase(10000)]
    [TestCase(100000)]
    [TestCase(10000000)]
    public void FrameTransformCoderTestEscapeEmulationBidirectional(int length) {
      System.Random rnd = new System.Random();
      byte[] src = new byte[length];
      rnd.NextBytes(src);
      int startIndex = rnd.Next(0, length);
      NativeArray<byte> srcNative = new NativeArray<byte>(src, Allocator.Temp);
      // Randomly choose random locations to insert emulation bytes
      int numEmulationInstances = rnd.Next(0, length / 3);
      for(int i = 0; i < numEmulationInstances; i++) {
        int randomStartIndex = rnd.Next(0, length - 3);
        int randomEmulationByteSequence = rnd.Next(0, 3);
        List<byte[]> emulationBytes = new List<byte[]> { 
          new byte[] { 0x00, 0x00, 0x01 },
          new byte[] { 0x00, 0x00, 0x02 },
          new byte[] { 0x00, 0x00, 0x03 }
        };
        NativeArray<byte>.Copy(src: emulationBytes[randomEmulationByteSequence], srcIndex: 0, dst: srcNative, dstIndex: randomStartIndex, length: 3);
      }
      NativeArray<byte> encodeDestination = new NativeArray<byte>(1, Allocator.Temp, NativeArrayOptions.ClearMemory);
      NativeArray<byte> decodeDestination = new NativeArray<byte>(1, Allocator.Temp, NativeArrayOptions.ClearMemory);

      var encodedTotalLength = FrameTransformerCoder.EscapeEmulationBytes(srcNative.AsReadOnly(), start: startIndex, totalLength: srcNative.Length, ref encodeDestination);
      var decodedTotalLength = FrameTransformerCoder.DeEscapeEmulationBytes(encodeDestination.AsReadOnly(), start: startIndex, totalLength: encodedTotalLength, ref decodeDestination);

      Assert.That(decodedTotalLength, Is.EqualTo(length));
      Assert.That(Enumerable.SequenceEqual(srcNative.ToArray(), decodeDestination.ToArray()), Is.True);
    }
  }
}