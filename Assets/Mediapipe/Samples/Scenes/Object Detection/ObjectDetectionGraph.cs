// Copyright (c) 2021 homuler
//
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

using System;
using System.Collections.Generic;
using UnityEngine.Events;

namespace Mediapipe.Unity.ObjectDetection
{
  public class ObjectDetectionGraph : GraphRunner
  {
#pragma warning disable IDE1006  // UnityEvent is PascalCase
    public UnityEvent<List<Detection>> OnOutputDetectionsOutput = new UnityEvent<List<Detection>>();
#pragma warning restore IDE1006

    private const string _InputStreamName = "input_video";

    private const string _OutputDetectionsStreamName = "output_detections";
    private OutputStream<DetectionVectorPacket, List<Detection>> _outputDetectionsStream;

    public override void StartRun(ImageSource imageSource)
    {
      if (runningMode.IsSynchronous())
      {
        _outputDetectionsStream.StartPolling().AssertOk();
      }
      else
      {
        _outputDetectionsStream.AddListener(OutputDetectionsCallback).AssertOk();
      }
      StartRun(BuildSidePacket(imageSource));
    }

    public override void Stop()
    {
      base.Stop();
      OnOutputDetectionsOutput.RemoveAllListeners();
      _outputDetectionsStream = null;
    }

    public void AddTextureFrameToInputStream(TextureFrame textureFrame)
    {
      AddTextureFrameToInputStream(_InputStreamName, textureFrame);
    }

    public bool TryGetNext(out List<Detection> outputDetections, bool allowBlock = true)
    {
      if (TryGetNext(_outputDetectionsStream, out outputDetections, allowBlock, GetCurrentTimestampMicrosec()))
      {
        OnOutputDetectionsOutput.Invoke(outputDetections);
        return true;
      }
      return false;
    }

    [AOT.MonoPInvokeCallback(typeof(CalculatorGraph.NativePacketCallback))]
    private static IntPtr OutputDetectionsCallback(IntPtr graphPtr, IntPtr packetPtr)
    {
      return InvokeIfGraphRunnerFound<ObjectDetectionGraph>(graphPtr, packetPtr, (objectDetectionGraph, ptr) =>
      {
        using (var packet = new DetectionVectorPacket(ptr, false))
        {
          if (objectDetectionGraph._outputDetectionsStream.TryGetPacketValue(packet, out var value, objectDetectionGraph.timeoutMicrosec))
          {
            objectDetectionGraph.OnOutputDetectionsOutput.Invoke(value);
          }
        }
      }).mpPtr;
    }

    protected override IList<WaitForResult> RequestDependentAssets()
    {
      return new List<WaitForResult> {
        WaitForAsset("ssdlite_object_detection_labelmap.txt"),
        WaitForAsset("ssdlite_object_detection.bytes"),
      };
    }

    protected override Status ConfigureCalculatorGraph(CalculatorGraphConfig config)
    {
      if (runningMode == RunningMode.NonBlockingSync)
      {
        _outputDetectionsStream = new OutputStream<DetectionVectorPacket, List<Detection>>(calculatorGraph, _OutputDetectionsStreamName, config.AddPacketPresenceCalculator(_OutputDetectionsStreamName));
      }
      else
      {
        _outputDetectionsStream = new OutputStream<DetectionVectorPacket, List<Detection>>(calculatorGraph, _OutputDetectionsStreamName, true);
      }
      return calculatorGraph.Initialize(config);
    }

    private SidePacket BuildSidePacket(ImageSource imageSource)
    {
      var sidePacket = new SidePacket();
      SetImageTransformationOptions(sidePacket, imageSource);
      return sidePacket;
    }
  }
}
