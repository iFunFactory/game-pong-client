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
        if (facebook_.IsLoggedIn)
        {
            RequestFBLogin();
        }
        else
        {
            facebook_.LogInWithRead(new List<string>() {
                    "public_profile", "email", "user_friends"});
        }
    }

    public void logout()
    {
        if (!facebook_.IsLoggedIn)
        {
            return;
        }

        facebook_.Logout();
    }

    private void OnGUI()
    {
        GUI.enabled = facebook_.IsLoggedIn;

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
                RequestFBLogin();
                break;

            case SNResultCode.kLoginFailed:
                Debug.Log("Social Network Login Failed.");
                NetworkManager.Instance.Stop();
                break;

            case SNResultCode.kError:
                FunDebug.Assert(false);
                break;
        }
    }

    private void RequestFBLogin()
    {
        var token = Facebook.Unity.AccessToken.CurrentAccessToken;

        if (NetworkManager.Instance.GetEncoding() == FunEncoding.kJson)
        {
            Dictionary<string, object> body = new Dictionary<string, object>();
            body["id"] = token.UserId;
            body["access_token"] = token.TokenString;
            body["type"] = "fb";
            NetworkManager.Instance.Send("login", body);
        }
        else
        {
            // TODO(dkmoon): Protobuf
        }
    }

    private FacebookConnector facebook_ = null;
    private Texture2D tex_ = null;
    private string name_ = string.Empty;
}