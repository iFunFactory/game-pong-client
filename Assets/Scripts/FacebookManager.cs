// Copyright (C) 2013 iFunFactory Inc. All Rights Reserved.
//
// This work is confidential and proprietary to iFunFactory Inc. and
// must not be used, disclosed, copied, or distributed without the prior
// consent of iFunFactory Inc.

using Fun;
using System.Collections.Generic;
using UnityEngine;
using Facebook.Unity;
public class FacebookManager : Singleton<FacebookManager>
{
    private void Awake()
    {
        facebook_ = GameObject.Find("SocialNetwork").GetComponent<FacebookConnector>();

        facebook_.OnEventCallback += new SocialNetwork.EventHandler(OnEventHandler);
        facebook_.OnPictureDownloaded += delegate (SocialNetwork.UserInfo user)
        {
            FunDebug.Log("{0}'s profile picture.", user.name);
            if (tex_ == null)
                tex_ = user.picture;
            if (name_ == string.Empty)
                name_ = user.name;
        };

        facebook_.Init();
    }

    public void login()
    {
        if (logged_in_) return;

        facebook_.LogInWithPublish(new List<string>() {
                "public_profile", "email", "user_friends", "publish_actions"});
    }

    private void OnGUI()
    {
        GUI.enabled = logged_in_;

        if (tex_ != null)
        {
            GUI.Label(new Rect(30, 10, 150, 20), "안녕하세요 " + name_ + " 님!");
            GUI.DrawTexture(new Rect(30, 30, 128, 128), tex_);
        }
    }

    private void OnEventHandler(SNResultCode result)
    {
        switch (result)
        {
            case SNResultCode.kLoggedIn:
                logged_in_ = true;
                break;

            case SNResultCode.kError:
                FunDebug.Assert(false);
                break;
        }
    }

    private FacebookConnector facebook_ = null;

    private bool logged_in_ = false;
    private Texture2D tex_ = null;
    private string name_ = string.Empty;
}