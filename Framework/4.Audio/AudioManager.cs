﻿/****************************************************************************
* Copyright (c) 2017 snowcold
* Copyright (c) 2017 ~ 2018.12 liangxie
*
* http://qframework.io
* https://github.com/liangxiegame/QFramework
*
* Permission is hereby granted, free of charge, to any person obtaining a copy
* of this software and associated documentation files (the "Software"), to deal
* in the Software without restriction, including without limitation the rights
* to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
* copies of the Software, and to permit persons to whom the Software is
* furnished to do so, subject to the following conditions:
*
* The above copyright notice and this permission notice shall be included in
* all copies or substantial portions of the Software.
*
* THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
* IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
* FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
* AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
* LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
* OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
* THE SOFTWARE.
****************************************************************************/

using System;

namespace QFramework
{
    using System.Collections.Generic;
    using UnityEngine;

    #region 消息id定义

    public enum AudioEvent
    {
        Began = QMgrID.Audio,
        SoundSwitch,
        MusicSwitch,
        VoiceSwitch,
        SetSoundVolume,
        SetMusicVolume,
        SetVoiceVolume,
        PlayMusic,
        PlaySound,
        PlayVoice,
        PauseMusic,
        ResumeMusic,
        StopMusic,
        PauseVoice,
        StopVoice,
        StopAllSound,
        PlayNode,
        AddRetainAudio,
        RemoveRetainAudioAudio,
        Ended
    }

    #endregion

    /// <summary>
    /// TODO:目前,不支持本地化
    /// </summary>
    [MonoSingletonPath("[Audio]/AudioManager")]
    public partial class AudioManager : QMgrBehaviour, ISingleton
    {
        
        public AudioSettings Settings { get; private set; }
        
        #region 消息处理

        protected AudioUnit mMusicUnit;
        protected AudioUnit mVoiceUnit;

        public static AudioUnit MusicUnit
        {
            get { return Instance.mMusicUnit; }
        }

        public static AudioUnit VoiceUnit
        {
            get { return Instance.mVoiceUnit; }
        }

        public void OnSingletonInit()
        {
            Log.I("AudioManager OnSingletonInit");
            RegisterEvents(
                AudioEvent.SoundSwitch,
                AudioEvent.MusicSwitch,
                AudioEvent.VoiceSwitch,
                AudioEvent.SetSoundVolume,
                AudioEvent.SetMusicVolume,
                AudioEvent.SetVoiceVolume,
                AudioEvent.PlayMusic,
                AudioEvent.PlaySound,
                AudioEvent.PlayVoice,
                AudioEvent.PlayNode,
                AudioEvent.AddRetainAudio,
                AudioEvent.RemoveRetainAudioAudio
            );

            SafeObjectPool<AudioUnit>.Instance.Init(10, 1);
            mMusicUnit = AudioUnit.Allocate();
            mMusicUnit.usedCache = false;
            mVoiceUnit = AudioUnit.Allocate();
            mVoiceUnit.usedCache = false;

            CheckAudioListener();

            gameObject.transform.position = Vector3.zero;
            
            Settings = new AudioSettings();
        }

        public void Dispose()
        {
        }

        public override int ManagerId
        {
            get { return QMgrID.Audio; }
        }

        protected override void ProcessMsg(int key, QMsg msg)
        {
            switch (msg.EventID)
            {
                case (int) AudioEvent.SoundSwitch:
                    AudioMsgWithBool soundSwitchMsg = msg as AudioMsgWithBool;
                    IsSoundOn = soundSwitchMsg.on;
                    break;
                case (int) AudioEvent.MusicSwitch:
                    AudioMsgWithBool musicSwitchMsg = msg as AudioMsgWithBool;
                    IsMusicOn = musicSwitchMsg.on;
                    if (!IsMusicOn)
                    {
                        StopMusic();
                    }

                    break;
                case (int) AudioEvent.PlayMusic:
                    Debug.LogFormat("play music msg: {0}, is musicOn: ", AudioEvent.PlayMusic.ToString(), IsMusicOn);
                    PlayMusic(msg as AudioMusicMsg);
                    break;
                case (int) AudioEvent.StopMusic:
                    StopMusic();
                    break;
                case (int) AudioEvent.PlaySound:
                    AudioSoundMsg audioSoundMsg = msg as AudioSoundMsg;
                    PlaySound(audioSoundMsg);
                    break;

                case (int) AudioEvent.PlayVoice:
                    PlayVoice(msg as AudioVoiceMsg);
                    break;
                case (int) AudioEvent.StopVoice:
                    StopVoice();
                    break;
                case (int) AudioEvent.PlayNode:
                    var msgPlayNode = (msg as AudioMsgWithNode).Node;
                    StartCoroutine(msgPlayNode.Execute());
                    break;
                case (int) AudioEvent.AddRetainAudio:
                    AddRetainAudioMsg addRetainAudioMsg = msg as AddRetainAudioMsg;
                    AddRetainAudio(addRetainAudioMsg.AudioName);
                    break;
                case (int) AudioEvent.RemoveRetainAudioAudio:
                    RemoveRetainAudioMsg removeRetainAudioMsg = msg as RemoveRetainAudioMsg;
                    RemoveRetainAudio(removeRetainAudioMsg.AudioName);
                    break;
                case (int) AudioEvent.PauseMusic:
                    PauseMusic();
                    break;
                case (int) AudioEvent.ResumeMusic:
                    ResumeMusic();
                    break;
            }
        }

        #endregion


        #region 对外接口

        public override void Init()
        {
            Log.I("AudioManager.Init");
        }

        public void CheckAudioListener()
        {
// 确保有一个AudioListener
            if (FindObjectOfType<AudioListener>() == null)
            {
                gameObject.AddComponent<AudioListener>();
            }
        }

        public static bool IsOn
        {
            get { return IsSoundOn && IsMusicOn && IsVoiceOn; }
        }

        public static void On()
        {
            SetSoundOn();
            SetMusicOn();
            SetVoiceOn();
        }

        public static void Off()
        {
            SetSoundOff();
            SetMusicOff();
            SetVoiceOff();
        }

        public static void SetSoundOn()
        {
            IsSoundOn = true;
        }

        public static void SetSoundOff()
        {
            IsSoundOn = false;
        }

        public static void SetVoiceOn()
        {
            IsVoiceOn = true;
        }

        public static void SetVoiceOff()
        {
            IsVoiceOn = false;
        }

        private string mCurMusicName;

        public static void SetMusicOn()
        {
            IsMusicOn = true;

            var self = Instance;

            if (self.mCurMusicName.IsNotNullAndEmpty())
            {
                self.SendMsg(new AudioMusicMsg(self.mCurMusicName, true));
            }
        }

        public static void SetMusicOff()
        {
            IsMusicOn = false;
            StopMusic();
        }


        #endregion

        #region 内部实现

        int mCurSourceIndex;


        /// <summary>
        /// 播放音乐
        /// </summary>
        void PlayMusic(AudioMusicMsg musicMsg)
        {

            if (!IsMusicOn && musicMsg.allowMusicOff)
            {
                musicMsg.onMusicBeganCallback.InvokeGracefully();

                musicMsg.onMusicEndedCallback.InvokeGracefully();
                return;
            }

            Log.I(">>>>>> Start Play Music");

// TODO: 需要按照这个顺序去 之后查一下原因
//需要先注册事件，然后再play
            mMusicUnit.SetOnStartListener(delegate(AudioUnit musicUnit)
            {
                musicMsg.onMusicBeganCallback.InvokeGracefully();

//调用完就置为null，否则应用层每注册一个而没有注销，都会调用
                mMusicUnit.SetOnStartListener(null);
            });

            mMusicUnit.SetAudio(gameObject, musicMsg.MusicName, musicMsg.Loop);

            mMusicUnit.SetOnFinishListener(delegate(AudioUnit musicUnit)
            {
                musicMsg.onMusicEndedCallback.InvokeGracefully();

//调用完就置为null，否则应用层每注册一个而没有注销，都会调用
                mMusicUnit.SetOnFinishListener(null);
            });
        }

        public static void PlayMusic(string musicName, bool loop = true, Action onBeganCallback = null,
            Action onEndCallback = null, bool allowMusicOff = true, float volume = 1.0f)
        {
            var self = Instance;
            self.mCurMusicName = musicName;

            if (!IsMusicOn && allowMusicOff)
            {
                onBeganCallback.InvokeGracefully();
                onEndCallback.InvokeGracefully();
                return;
            }

            Log.I(">>>>>> Start Play Music");

// TODO: 需要按照这个顺序去 之后查一下原因
//需要先注册事件，然后再play
            self.mMusicUnit.SetOnStartListener(musicUnit =>
            {
                onBeganCallback.InvokeGracefully();

                self.mMusicUnit.SetVolume(volume);
//调用完就置为null，否则应用层每注册一个而没有注销，都会调用
                self.mMusicUnit.SetOnStartListener(null);
            });

            self.mMusicUnit.SetAudio(self.gameObject, musicName, loop);

            self.mMusicUnit.SetOnFinishListener(musicUnit =>
            {
                onEndCallback.InvokeGracefully();

//调用完就置为null，否则应用层每注册一个而没有注销，都会调用
                self.mMusicUnit.SetOnFinishListener(null);
            });
        }

        private void SetVolume(AudioUnit audioUnit, VolumeLevel volumeLevel)
        {
            switch (volumeLevel)
            {
                case VolumeLevel.Max:
                    audioUnit.SetVolume(1.0f);
                    break;
                case VolumeLevel.Normal:
                    audioUnit.SetVolume(0.5f);
                    break;
                case VolumeLevel.Min:
                    audioUnit.SetVolume(0.2f);
                    break;
            }
        }

        public static void PlaySound(string soundName, bool loop = false, Action<AudioUnit> callBack = null,
            int customEventId = -1)
        {
            if (!IsSoundOn) return;

            if (soundName.IsNullOrEmpty())
            {
                Log.E("soundName 为空");
                return;
            }

            var unit = SafeObjectPool<AudioUnit>.Instance.Allocate();
            unit.SetAudio(Instance.gameObject, soundName, loop);
            unit.SetOnFinishListener(callBack);
            unit.customEventID = customEventId;
        }

        /// <summary>
        /// 停止播放音乐
        /// </summary>
        public static void StopMusic()
        {
            Instance.mMusicUnit.Stop();
        }

        public static void StopVoice()
        {
            if (Instance.mVoiceUnit.IsNotNull())
            {
                Instance.mVoiceUnit.Stop();
            }
        }

        public static void PauseMusic()
        {
            if (Instance.mMusicUnit != null)
            {
                Instance.mMusicUnit.Pause();
            }
        }

        public static void ResumeMusic()
        {
            if (Instance.mMusicUnit != null)
            {
                Instance.mMusicUnit.Resume();
            }
        }

        /// <summary>
        /// 播放音效
        /// </summary>
        void PlaySound(AudioSoundMsg soundMsg)
        {
            if (IsSoundOn)
            {
                AudioUnit unit = SafeObjectPool<AudioUnit>.Instance.Allocate();


                unit.SetOnStartListener(delegate(AudioUnit soundUnit)
                {
                    soundMsg.onSoundBeganCallback.InvokeGracefully();

                    unit.SetVolume(soundMsg.Volume);

                    unit.SetOnStartListener(null);
                });

                unit.SetAudio(gameObject, soundMsg.SoundName, false);

                unit.SetOnFinishListener(delegate(AudioUnit soundUnit)
                {
                    soundMsg.onSoundEndedCallback.InvokeGracefully();

                    unit.SetOnFinishListener(null);
                });
            }
        }

        /// <summary>
        /// 播放语音
        /// </summary>
        void PlayVoice(AudioVoiceMsg msg)
        {
            mVoiceUnit.SetOnStartListener(delegate(AudioUnit musicUnit)
            {
//                SetVolume(mVoiceUnit, VolumeLevel.Min);

                msg.onVoiceBeganCallback.InvokeGracefully();

                mVoiceUnit.SetOnStartListener(null);
            });

            mVoiceUnit.SetAudio(gameObject, msg.voiceName, msg.loop);

            mVoiceUnit.SetOnFinishListener(delegate(AudioUnit musicUnit)
            {
//                SetVolume(mVoiceUnit, VolumeLevel.Max);

                msg.onVoiceEndedCallback.InvokeGracefully();

                mVoiceUnit.SetOnFinishListener(null);
            });
        }

        public static void PlayVoice(string soundName, bool loop = false)
        {
            if (soundName.IsNullOrEmpty())
            {
                return;
            }

            var unit = SafeObjectPool<AudioUnit>.Instance.Allocate();
            unit.SetAudio(Instance.gameObject, soundName, loop);
        }

        #region 单例实现


        public static AudioManager Instance
        {
            get { return MonoSingletonProperty<AudioManager>.Instance; }
        }

        void Example()
        {
            // 按钮点击音效
            SendMsg(new AudioSoundMsg("Sound.CLICK"));

            //播放背景音乐
            SendMsg(new AudioMusicMsg("music", true));

            //停止播放音乐
            SendMsg(new QMsg((ushort) AudioEvent.StopMusic));

            SendMsg(new AudioVoiceMsg("Sound.CLICK", delegate { }, delegate { }));
        }

        #endregion

        //常驻内存不卸载音频资源
        protected ResLoader mRetainResLoader;

        protected List<string> mRetainAudioNames;

        /// <summary>
        /// 添加常驻音频资源，建议尽早添加，不要在用的时候再添加
        /// </summary>
        private void AddRetainAudio(string audioName)
        {
            if (mRetainResLoader == null)
                mRetainResLoader = ResLoader.Allocate();
            if (mRetainAudioNames == null)
                mRetainAudioNames = new List<string>();

            if (!mRetainAudioNames.Contains(audioName))
            {
                mRetainAudioNames.Add(audioName);
                mRetainResLoader.Add2Load(audioName);
                mRetainResLoader.LoadAsync();
            }
        }

        /// <summary>
        /// 删除常驻音频资源
        /// </summary>
        private void RemoveRetainAudio(string audioName)
        {
            if (mRetainAudioNames != null && mRetainAudioNames.Remove(audioName))
            {
                mRetainResLoader.ReleaseRes(audioName);
            }
        }

        #endregion

        #region 留给脚本绑定的 API

        public static void PlayMusic(string musicName)
        {
            PlayMusic(musicName, true);
        }

        #endregion
    }
}