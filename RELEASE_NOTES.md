## [v1.2.0](https://github.com/millicast/millicast-unity-sdk/compare/v1.1.0...v1.2.0-alpha.1) (19-Jan-2024)

> Description

### New Features
* Introduce Frame Transformer API For `McPublisher` & `McSubscriber`. See [FrameMetadataExample.cs](Samples~/Scripts/FrameMetadataExample.cs) for an example script that embeds text metadata into each encoded frame and extracts it when received in the subscriber.

* Simple example to show `WebCamTexture` publishing: [WebCamPublisherExample.cs](Samples~/Scripts/WebCamPublisherExample.cs)

### Bug Fixes
* `StreamType.Both` is selected as a default for the `McPublisher`. Previously it was set to `StreamType.VideoOnly`.