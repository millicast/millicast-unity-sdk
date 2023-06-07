# Unity Millicast Realtime Streaming 

## Overview
The Millicast Unity package allows game developers to publish and view streams from the Millicast service from within their Unity games. For example, users can publish scenes captured from their in-game cameras, as well as audio sources, for other viewers to subscribe to, as well as rendering video and audio streams incoming from the service onto textures and audio sources. 


## Documentation

The API (class) Documentation can be found [here](https://millicast.github.io/millicast-unity-sdk/Documentation/html/index.html).
The Dolby.io tutorials and further documentation can be found [here](https://docs.dolby.io/streaming-apis/docs/unity-getting-started).

## Requirements 
This package uses Unity WebRTC as a dependency, and therefore requires the following: 

### Unity Version
It is recommended to use the latest Long-Term Support (LTS) version of Unity, see the post in Unity blog for LTS versions.

This version of the package is compatible with the following versions of the Unity Editor:

* **Unity 2020.3**
* **Unity 2021.3**
* **Unity 2022.1**

### Development Platforms
* **Windows**
* **Linux**
* **MacOS (Intel and Apple Silicon)**

### Target Platforms
* **Windows**
* **Linux (Ubuntu 16.04, 18.04, 20.04)**
* **macOS (Intel and Apple Silicon)**
* **iOS**
* **Android (ARM64 only. ARMv7 is not supported)**


### Unsupported Platforms 
* **Windows UWP**
* **iOS Simulator**
* **WebGL**

### Note on building for Android
To build the apk file for Android platform, you need to configure player settings below.

* Choose IL2CPP for Scripting backend in Player Settings Window.
* Set enadle ARM64 and Set disable ARMv7 for Target Architectures setting in Player Settings Window.

### Note on using URP (Universal Render Pipeline)
If using URP, users needs to convert the materials to URP by selecting the `Materials` folder under `Samples` and go to `Edit > Render Pipeline > Universal Render Pipeline`. According to your needs, select Upgrade Selected Materials to URP Materials. 

### Supported Codecs
* **VP8**
* **VP9**
* **H264**
* **AV1**

### Sample Scenes
* **360Subscribe**
    This scene demonstrates the usage of McSubscriber component which is used to subscribe to a video stream with the help of stream name and using a material as render target. The same material is used for all the meshes in the scene.
    McSubscriber component is attached to the Main Camera
* **StreamingExample**
    This scene demonstrates the usage of publishing a video to the server and then connecting to the published stream as subscriber.
    To stream the dynamic content, this scene's main camera looking on to a cube which is rotating. Once the user starts publishing, the application will capture the camera feed and publish it to the server.
* **VideoConfigExample**
    This scene is similar to te StreamingExample, but the user has additional options to set the video codec type, video quality settings, screen resolution etc
    User can predefine the video configurations using the scriptable object and assign it to the McPublisher component. If its not assigned, it will take the default video configuration values. 
    How ever, user can still update the video configuration from UI drop down options at run time.
*  **UISubscriber**
    This scene demonstrates the usage of MCSubscriber using the RawImage as streaming Target renderer. User can start/stop the video stream using the Subscribe/UnSubscribe buttons.
*  **Virtual Stereo Subscriber** This scene demonstrates a simple subscriber usage of virtualized stereo speakers, where the left channel is played on the left speaker, and the right channel is played on the right one. 
### Additional Android Player Settings:
    We currently support the OpenGLES3 Graphics API on Android Devices. Vulkan Graphics API is not supported.

    If you encounter any crashes or difficulty deploying, check your project's player settings and update as below.
    
    Player Settings -> Other Settings 

    * Un-tick Auto Graphics API

    * Under Graphics APIs:

        *  remove Vulkan

        *  Add or select OpenGLES3

        *  Optionally Leave below options unticked:
            *   require ES3.1
            *   require ES3.1 + AEP
            *   require ES3.2

### Notes on simulcast 
Currently, publishing with simulcast only works with VP8 and on Windows machines. Attempting to publish with simulcast on other platforms will cause a crash due to a bug in Unity WebRTC.

### Notes on H264
Using H264 on Windows requires an Nvidia GPU as the Unity WebRTC implementation uses a hardware encoder only. Make sure to have up-to-date drivers. Head over to the [Unity WebRTC documentation](https://docs.unity3d.com/Packages/com.unity.webrtc@3.0/manual/videostreaming.html) to learn more about the video codecs supported by Unity WebRTC and thus by us. If you have issues on Windows where the stream is not being viewed on the web browser when publishing using H264, it is likely an issue with an out-dated driver.


### Simulcast Feature
* Supported Codec : VP8
* Supported Platform : Windows


### Known Limitations
* Currently, attempting to publish with an `AudioSource` selected in a `VideoPlayer` does not work as intended.
