using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BackEnd;
using System.Threading.Tasks;
using System.Threading;
using System;
using Newtonsoft.Json;
using LitJson;
using System.Numerics;
using UnityEditor;

public class NetworkManager : SingletonDontDestroyed<NetworkManager>
{
    private DataAccount dataAccount_Previous = new();
    public void SetDataAccount_Previous(DataAccount dataAccount)
    {
        if (dataAccount_Previous != null)
            return;
        
        dataAccount_Previous = dataAccount;
    }
    public void SetDataAccount_Previous(BigInteger gold, BigInteger exp)
    {
        dataAccount_Previous.dataBasic.SetGold(gold);
        dataAccount_Previous.dataBasic.SetAccountEXP(exp);
    }

    public bool InitializeSuccess { get; private set; }

    private void Start()
    {
        Initialize();
    }
    //private async void Start()
    //{
    //    await Initialize_Async(null, null);
    //}
    //private IEnumerator Start()
    //{
    //    yield return CoInitialize(null, null);
    //}

    private void Update()
    {
        Backend.AsyncPoll();
    }

    #region Process Wait.
    public IEnumerator CoWaitNetworkProcess()
    {
        yield return new WaitUntil(() => InitializeSuccess == true);
    }
    public async Task WaitNetworkProcess_Async()
    {
        while (!InitializeSuccess)
            await Task.Yield();
    }
    #endregion

    #region Initialize.
    public void Initialize()
    {
        InitializeSuccess = false;

        //MainThread 에서만 실행이 가능함.
        Backend.InitializeAsync(true, callback =>
        {
            if (callback.IsSuccess())
            {
                Utils.LogColor("뒤끝 초기화 성공");

                Utils.LogColor("구글 해시키 : " + Backend.Utils.GetGoogleHash());
            }
            else
            {
                Utils.LogColor("뒤끝 초기화 실패");
                Utils.LogColor(callback.GetStatusCode());
            }

            InitializeSuccess = true;
        });
    }

    public IEnumerator CoInitialize(Action actionSuccess, Action actionFailure)
    {
        bool processComplete = false;

        Backend.InitializeAsync(true, callback =>
        {
            if (callback.IsSuccess())
            {
                Utils.LogColor("뒤끝 초기화 성공");

                Utils.LogColor("구글 해시키 : " + Backend.Utils.GetGoogleHash());

                actionSuccess?.Invoke();
            }
            else
            {
                Utils.LogColor("뒤끝 초기화 실패");
                Utils.LogColor(callback.GetStatusCode());

                actionFailure?.Invoke();
            }

            processComplete = true;            
        });

        yield return new WaitUntil(() => processComplete == true);
    }
    public async Task Initialize_Async(Action actionSuccess, Action actionFailure)
    {
        bool processComplete = false;

        Backend.InitializeAsync(true, callback =>
        {
            if (callback.IsSuccess())
            {
                Utils.LogColor("뒤끝 초기화 성공");

                Utils.LogColor("구글 해시키 : " + Backend.Utils.GetGoogleHash());

                actionSuccess?.Invoke();
            }
            else
            {
                Utils.LogColor("뒤끝 초기화 실패");
                Utils.LogColor(callback.GetStatusCode());

                actionFailure?.Invoke();
            }

            processComplete = true;
        });

        while (!processComplete)
            await Task.Yield();
    }

    public async Task Initialize_Async()
    {
        await Task.Run(() => { Initialize(); });

        //Thread thread = new(Initialize);
        //thread.Start();
    }

    private IEnumerable<Task> CoInitialize()
    {
        yield return Initialize_Async();
    }
    #endregion

    #region Public Error.
    //private IEnumerator CoManagePublicError(Action actionRetry, BackendReturnObject backendReturnObject)
    //{

    //}
    #endregion

    #region Refresh Token.
    private Coroutine coroutineRefreshToken;
    private IEnumerator CoRefreshToken()
    {
        while (true)
        {
            yield return Utils.WaitForSeconds(60.0f * 60.0f * 8.0f);

            for (int i = 0; i < 3; i++)
            {
                bool processSuccess = false;
                bool duplicatedID = false;
                Backend.BMember.RefreshTheBackendToken((callback) =>
                {
                    if (callback.IsSuccess())
                    {
                        Utils.LogColor("토큰 재발급 성공");

                        processSuccess = true;
                    }
                    else
                    {
                        Utils.LogColor("토큰 재발급 실패");

                        processSuccess = false;

                        if (callback.GetMessage().Contains("bad refreshToken"))
                        {
                            Debug.LogError("중복 로그인 발생");

                            Utils.SetMessagePopup("중복 로그인이 발생했습니다. 게임을 종료하시겠습니까?",
                                () => 
                                {
#if UNITY_EDITOR
                                    EditorApplication.isPlaying = false;
#else
                                    Application.Quit();
#endif
                                });

                            duplicatedID = true;
                        }
                        else
                        {
                            Debug.LogWarning("15초 뒤에 토큰 재발급 다시 시도");
                        }
                    }                    
                });

                if (duplicatedID)
                    yield break;

                if (processSuccess)
                    break;
                else
                    yield return Utils.WaitForSeconds(15.0f);
            }
        }
    }
    #endregion

    #region Account.
    //로컬에 저장된 토큰으로 자동 로그인.
    public IEnumerator CoLoginAuto(Action actionSuccess, Action actionFailure, IEnumerator process)
    {
        bool processComplete = false;

        bool processSuccess = false;
        Backend.BMember.LoginWithTheBackendToken((callback) =>
        {
            if (callback.IsSuccess())
            {
                processSuccess = true;
                
                Utils.LogColor("자동 로그인 성공");

                actionSuccess?.Invoke();

                if (coroutineRefreshToken != null)
                {
                    StopCoroutine(coroutineRefreshToken);
                    coroutineRefreshToken = null;
                }
                coroutineRefreshToken = StartCoroutine(CoRefreshToken());
            }
            else
            {
                Utils.LogColor("자동 로그인 실패");
                Utils.LogColor(Utils.String.MergeString("Code : ", callback.GetStatusCode(),
                    " / Message : ", callback.GetMessage()));

                if (callback.GetMessage().Contains("undefined refresh_token") ||
                    callback.GetMessage().Contains("refresh_token"))
                {
                    Utils.LogColor("로컬에 저장된 토큰이 없음");

                    UIManager.Instance.GetPopup<PopupMessage>(UIManager.PopupType.PopupMessage).SetPopup(
                        "저장된 계정정보 없음",
                        PopupMessage.PopupType.OK,
                        () =>
                        {
                            UIManager.Instance.GetPopup<PopupLogin>(UIManager.PopupType.PopupLogin).SetPopup(
                            () =>
                            {
                                UIManager.Instance.ShowPanel(UIManager.PanelType.PanelMain);
                            });
                        });
                }
                else if (callback.GetMessage().Contains("bad refreshToken") ||
                    callback.GetMessage().Contains("refreshToken"))
                {
                    Utils.LogColor("다른 기기에서 로그인");

                    UIManager.Instance.GetPopup<PopupMessage>(UIManager.PopupType.PopupMessage).SetPopup(
                        "다른 기기에서 로그인 되어 있는 계정입니다.",
                        PopupMessage.PopupType.OK,
                        () =>
                        {
                            UIManager.Instance.GetPopup<PopupLogin>(UIManager.PopupType.PopupLogin).SetPopup(
                            () =>
                            {
                                UIManager.Instance.ShowPanel(UIManager.PanelType.PanelMain);
                            });
                        });
                }
                else if (callback.GetMessage().Contains("bad refreshToken") ||
                    callback.GetMessage().Contains("refreshToken"))
                {
                    Utils.LogColor("서버 DB에 유저가 없음");

                    UIManager.Instance.GetPopup<PopupMessage>(UIManager.PopupType.PopupMessage).SetPopup(
                        "서버에 계정정보 없음",
                        PopupMessage.PopupType.OK,
                        () =>
                        {
                            UIManager.Instance.GetPopup<PopupLogin>(UIManager.PopupType.PopupLogin).SetPopup(
                            () =>
                            {
                                UIManager.Instance.ShowPanel(UIManager.PanelType.PanelMain);
                            });
                        });
                }

                actionFailure?.Invoke();
            }

            processComplete = true;
        });

        yield return new WaitUntil(() => processComplete == true);

        if (processSuccess && process != null)
            yield return process;
    }
    public async Task AsyncLoginAuto(Action actionSuccess, Action actionFailure, Func<Task> process)
    {
        bool processComplete = false;

        bool processSuccess = false;
        Backend.BMember.LoginWithTheBackendToken((callback) =>
        {
            if (callback.IsSuccess())
            {
                processSuccess = true;

                Utils.LogColor("자동 로그인 성공");

                actionSuccess?.Invoke();

                if (coroutineRefreshToken != null)
                {
                    StopCoroutine(coroutineRefreshToken);
                    coroutineRefreshToken = null;
                }
                coroutineRefreshToken = StartCoroutine(CoRefreshToken());
            }
            else
            {
                Utils.LogColor("자동 로그인 실패");
                Utils.LogColor(Utils.String.MergeString("Code : ", callback.GetStatusCode(),
                    " / Message : ", callback.GetMessage()));

                if (callback.GetMessage().Contains("undefined refresh_token") ||
                    callback.GetMessage().Contains("refresh_token"))
                {
                    Utils.LogColor("로컬에 저장된 토큰이 없음");

                    UIManager.Instance.GetPopup<PopupMessage>(UIManager.PopupType.PopupMessage).SetPopup(
                        "저장된 계정정보 없음",
                        PopupMessage.PopupType.OK,
                        () =>
                        {
                            UIManager.Instance.GetPopup<PopupLogin>(UIManager.PopupType.PopupLogin).SetPopup(
                            () =>
                            {
                                UIManager.Instance.ShowPanel(UIManager.PanelType.PanelMain);
                            });
                        });
                }
                else if (callback.GetMessage().Contains("bad refreshToken") ||
                    callback.GetMessage().Contains("refreshToken"))
                {
                    Utils.LogColor("다른 기기에서 로그인");

                    UIManager.Instance.GetPopup<PopupMessage>(UIManager.PopupType.PopupMessage).SetPopup(
                        "다른 기기에서 로그인 되어 있는 계정입니다.",
                        PopupMessage.PopupType.OK,
                        () =>
                        {
                            UIManager.Instance.GetPopup<PopupLogin>(UIManager.PopupType.PopupLogin).SetPopup(
                            () =>
                            {
                                UIManager.Instance.ShowPanel(UIManager.PanelType.PanelMain);
                            });
                        });
                }
                else if (callback.GetMessage().Contains("bad refreshToken") ||
                    callback.GetMessage().Contains("refreshToken"))
                {
                    Utils.LogColor("서버 DB에 유저가 없음");

                    UIManager.Instance.GetPopup<PopupMessage>(UIManager.PopupType.PopupMessage).SetPopup(
                        "서버에 계정정보 없음",
                        PopupMessage.PopupType.OK,
                        () =>
                        {
                            UIManager.Instance.GetPopup<PopupLogin>(UIManager.PopupType.PopupLogin).SetPopup(
                            () =>
                            {
                                UIManager.Instance.ShowPanel(UIManager.PanelType.PanelMain);
                            });
                        });
                }

                actionFailure?.Invoke();
            }

            processComplete = true;
        });

        while (!processComplete)
            await Task.Yield();

        if (processSuccess && process != null)
            await process();
    }

    //로그아웃. 로컬 저장된 토큰 다 날림.
    public IEnumerator CoLogout(Action actionSuccess, Action actionFailure)
    {
        bool processComplete = false;

        Backend.BMember.Logout((callback) =>
        {
            if (callback.IsSuccess())
            {
                Utils.LogColor("로그아웃 성공");
                UIManager.Instance.GetPopup<PopupLogin>(UIManager.PopupType.PopupLogin).SetPopup(
                    () =>
                    {
                        UIManager.Instance.HidePopup(UIManager.PopupType.PopupLogin);
                    });

                actionSuccess?.Invoke();
            }
            else
            {
                Utils.LogColor("로그아웃 실패");
                Utils.LogColor(Utils.String.MergeString("Code : ", callback.GetStatusCode(),
                " / Message : ", callback.GetMessage()));

                actionFailure?.Invoke();
            }

            processComplete = true;
        });

        yield return new WaitUntil(() => processComplete == true);
    }
    public async Task AsyncLogout(Action actionSuccess, Action actionFailure)
    {
        bool processComplete = false;

        Backend.BMember.Logout((callback) =>
        {
            if (callback.IsSuccess())
            {
                Utils.LogColor("로그아웃 성공");
                UIManager.Instance.GetPopup<PopupLogin>(UIManager.PopupType.PopupLogin).SetPopup(
                    () =>
                    {
                        UIManager.Instance.HidePopup(UIManager.PopupType.PopupLogin);
                    });

                actionSuccess?.Invoke();
            }
            else
            {
                Utils.LogColor("로그아웃 실패");
                Utils.LogColor(Utils.String.MergeString("Code : ", callback.GetStatusCode(),
                " / Message : ", callback.GetMessage()));

                actionFailure?.Invoke();
            }

            processComplete = true;
        });

        while (!processComplete)
            await Task.Yield();
    }

    //회원가입.
    public IEnumerator CoSignUp(string userID, string passWord, Action actionSuccess, Action actionFailure)
    {
        bool processComplete = false;

        Backend.BMember.CustomSignUp(userID, passWord, callback => {
            if (callback.IsSuccess())
            {
                Utils.LogColor("회원가입 성공");

                actionSuccess?.Invoke();

                if (coroutineRefreshToken != null)
                {
                    StopCoroutine(coroutineRefreshToken);
                    coroutineRefreshToken = null;
                }
                coroutineRefreshToken = StartCoroutine(CoRefreshToken());

                StartCoroutine(Utils.CoSetBlockLoading(CoLogin(
                    userID,
                    passWord,
                    () =>
                    {
                        StartCoroutine(Utils.CoSetBlockLoading(CoInsertAllData(null)));
                    },
                    null)));
            }
            else
            {
                Utils.LogColor("회원가입 실패");
                Utils.LogColor(Utils.String.MergeString("Code : ", callback.GetStatusCode(),
                    " / Message : ", callback.GetMessage()));

                if (callback.GetMessage().Contains("Duplicated customId"))
                {
                    actionSuccess?.Invoke();

                    StartCoroutine(Utils.CoSetBlockLoading(CoLogin(
                        userID,
                        passWord,
                        () =>
                        {
                            StartCoroutine(Utils.CoSetBlockLoading(CoLoadData(null)));
                        },
                        null)));
                }

                actionFailure?.Invoke();
            }

            processComplete = true;
        });

        yield return new WaitUntil(() => processComplete == true);
    }
    public async Task AsyncSignUp(string userID, string passWord, Action actionSuccess, Action actionFailure)
    {
        bool processComplete = false;

        Backend.BMember.CustomSignUp(userID, passWord, callback => {
            if (callback.IsSuccess())
            {
                Utils.LogColor("회원가입 성공");

                actionSuccess?.Invoke();

                if (coroutineRefreshToken != null)
                {
                    StopCoroutine(coroutineRefreshToken);
                    coroutineRefreshToken = null;
                }
                coroutineRefreshToken = StartCoroutine(CoRefreshToken());

                StartCoroutine(Utils.CoSetBlockLoading(CoLogin(
                    userID,
                    passWord,
                    () => 
                    { 
                        StartCoroutine(Utils.CoSetBlockLoading(CoInsertAllData(null))); 
                    }, 
                    null)));
            }
            else
            {
                Utils.LogColor("회원가입 실패");
                Utils.LogColor(Utils.String.MergeString("Code : ", callback.GetStatusCode(),
                    " / Message : ", callback.GetMessage()));

                if (callback.GetMessage().Contains("Duplicated customId"))
                {
                    StartCoroutine(Utils.CoSetBlockLoading(CoLogin(
                        userID,
                        passWord,
                        () =>
                        {
                            StartCoroutine(Utils.CoSetBlockLoading(CoLoadData(null)));
                        },
                        null)));
                }

                actionFailure?.Invoke();
            }
        });

        while (!processComplete)
            await Task.Yield();
    }

    //커스텀계정 로그인.
    public IEnumerator CoLogin(string userID, string passWord, Action actionSuccess, Action actionFailure)
    {
        bool processComplete = false;

        Backend.BMember.CustomLogin(userID, passWord, callback => {
            if (callback.IsSuccess())
            {
                Utils.LogColor("로그인 성공");

                actionSuccess?.Invoke();

                if (coroutineRefreshToken != null)
                {
                    StopCoroutine(coroutineRefreshToken);
                    coroutineRefreshToken = null;
                }
                coroutineRefreshToken = StartCoroutine(CoRefreshToken());
            }
            else
            {
                Utils.LogColor("로그인 실패.");
                Utils.LogColor(Utils.String.MergeString("Code : ", callback.GetStatusCode(),
                    " / Message : ", callback.GetMessage()));

                actionFailure?.Invoke();
            }

            processComplete = true;
        });
        
        yield return new WaitUntil(() => processComplete == true);
    }
    public async Task AsyncLogin(string userID, string passWord, Action actionSuccess, Action actionFailure)
    {
        bool processComplete = false;

        Backend.BMember.CustomLogin(userID, passWord, callback => {
            if (callback.IsSuccess())
            {
                Utils.LogColor("로그인 성공");

                actionSuccess?.Invoke();

                if (coroutineRefreshToken != null)
                {
                    StopCoroutine(coroutineRefreshToken);
                    coroutineRefreshToken = null;
                }
                coroutineRefreshToken = StartCoroutine(CoRefreshToken());
            }
            else
            {
                Utils.LogColor("로그인 실패.");
                Utils.LogColor(Utils.String.MergeString("Code : ", callback.GetStatusCode(),
                    " / Message : ", callback.GetMessage()));

                actionFailure?.Invoke();
            }

            processComplete = true;
        });

        while (!processComplete)
            await Task.Yield();
    }

    //닉네임.
    public IEnumerator CoSetNickName(string nickName, Action actionSuccess, Action actionFailure)
    {
        bool processComplete = false;

        Backend.BMember.CreateNickname(nickName, (callback) =>
        {
            if (callback.IsSuccess())
            {
                Utils.LogColor("닉네임 생성 성공");

                actionSuccess?.Invoke();
            }
            else
            {
                Utils.LogColor("닉네임 생성 실패");
                Utils.LogColor(Utils.String.MergeString("Code : ", callback.GetStatusCode(),
                    " / Message : ", callback.GetMessage()));

                if (callback.GetMessage().Contains("Duplicated nickname"))
                {
                    Utils.SetMessagePopup("선점된 닉네임이다");

                    actionFailure?.Invoke();

                    return;
                }

                //switch (callback.GetStatusCode())
                //{
                //    case "400":
                //        if (callback.GetMessage() == "UndefinedParameterException")
                //        {
                //            Utils.LogColor("빈 닉네임");
                //        }
                //        else if (callback.GetMessage() == "BadParameterException")
                //        {
                //            Utils.LogColor("20자 이상");
                //            Utils.LogColor("닉네임 앞뒤 공백");
                //        }
                //        break;
                //    case "409":
                //        Utils.LogColor("중복 닉네임");
                //        break;
                //    default:
                //        break;
                //}
            }
        });

        yield return new WaitUntil(() => processComplete == true);
    }
    public async Task AsyncSetNickName(string nickName, Action actionSuccess, Action actionFailure)
    {
        bool processComplete = false;

        Backend.BMember.CreateNickname(nickName, (callback) =>
        {
            if (callback.IsSuccess())
            {
                Utils.LogColor("닉네임 생성 성공");

                actionSuccess?.Invoke();
            }
            else
            {
                Utils.LogColor("닉네임 생성 실패");
                Utils.LogColor(Utils.String.MergeString("Code : ", callback.GetStatusCode(),
                    " / Message : ", callback.GetMessage()));

                if (callback.GetMessage().Contains("Duplicated nickname"))
                {
                    Utils.SetMessagePopup("선점된 닉네임이다");

                    actionFailure?.Invoke();

                    return;
                }

                //switch (callback.GetStatusCode())
                //{
                //    case "400":
                //        if (callback.GetMessage() == "UndefinedParameterException")
                //        {
                //            Utils.LogColor("빈 닉네임");
                //        }
                //        else if (callback.GetMessage() == "BadParameterException")
                //        {
                //            Utils.LogColor("20자 이상");
                //            Utils.LogColor("닉네임 앞뒤 공백");
                //        }
                //        break;
                //    case "409":
                //        Utils.LogColor("중복 닉네임");
                //        break;
                //    default:
                //        break;
                //}
            }
        });

        while (!processComplete)
            await Task.Yield();
    }

    //이메일 등록.
    private void SetEmail()
    {
        Backend.BMember.UpdateCustomEmail("help@thebackend.io");
    }

    //로컬에 저장된 유저정보 확인. 로그인을 해야 쓸 수 있음;;;
    public void GetLocalAccountInfo()
    {
        string inDate = Backend.UserInDate;
        string nickName = Backend.UserNickName;

        Utils.LogColor(inDate);
        Utils.LogColor(nickName);
    }

    //서버에 저장된 유저정보 확인. 로그인을 해야 쓸 수 있음;;;
    public IEnumerator CoGetServerAccountInfo(Action actionSuccess, Action actionFailure)
    {
        bool processComplete = false;

        Backend.BMember.GetUserInfo((callback) =>
        {
            if (callback.IsSuccess())
            {
                Utils.LogColor("정보 불러오기 성공");

                JsonData jsonData = callback.GetReturnValuetoJSON()["row"];

                string gamerId = jsonData["gamerId"] == null ? string.Empty : jsonData["gamerId"].ToString();
                string countryCode = jsonData["countryCode"] == null ? string.Empty : jsonData["countryCode"].ToString();
                string nickname = jsonData["nickname"] == null ? string.Empty : jsonData["nickname"].ToString();
                string inDate = jsonData["inDate"] == null ? string.Empty : jsonData["inDate"].ToString();
                string emailForFindPassword = jsonData["emailForFindPassword"] == null ? string.Empty : jsonData["emailForFindPassword"].ToString();
                string subscriptionType = jsonData["subscriptionType"] == null ? string.Empty : jsonData["subscriptionType"].ToString();
                string federationId = jsonData["federationId"] == null ? string.Empty : jsonData["federationId"].ToString();

                Utils.LogColor(gamerId);
                Utils.LogColor(countryCode);
                Utils.LogColor(nickname);
                Utils.LogColor(inDate);
                Utils.LogColor(emailForFindPassword);
                Utils.LogColor(subscriptionType);
                Utils.LogColor(federationId);

                actionSuccess?.Invoke();
            }
            else
            {
                Utils.LogColor("정보 불러오기 실패.");
                Utils.LogColor(callback.ToString());

                actionFailure?.Invoke();
            }

            processComplete = true;
        });

        yield return new WaitUntil(() => processComplete == true);
    }
    public async Task AsyncGetServerAccountInfo(Action actionSuccess, Action actionFailure)
    {
        bool processComplete = false;

        Backend.BMember.GetUserInfo((callback) =>
        {
            if (callback.IsSuccess())
            {
                Utils.LogColor("정보 불러오기 성공");

                JsonData jsonData = callback.GetReturnValuetoJSON()["row"];

                string gamerId = jsonData["gamerId"] == null ? string.Empty : jsonData["gamerId"].ToString();
                string countryCode = jsonData["countryCode"] == null ? string.Empty : jsonData["countryCode"].ToString();
                string nickname = jsonData["nickname"] == null ? string.Empty : jsonData["nickname"].ToString();
                string inDate = jsonData["inDate"] == null ? string.Empty : jsonData["inDate"].ToString();
                string emailForFindPassword = jsonData["emailForFindPassword"] == null ? string.Empty : jsonData["emailForFindPassword"].ToString();
                string subscriptionType = jsonData["subscriptionType"] == null ? string.Empty : jsonData["subscriptionType"].ToString();
                string federationId = jsonData["federationId"] == null ? string.Empty : jsonData["federationId"].ToString();

                Utils.LogColor(gamerId);
                Utils.LogColor(countryCode);
                Utils.LogColor(nickname);
                Utils.LogColor(inDate);
                Utils.LogColor(emailForFindPassword);
                Utils.LogColor(subscriptionType);
                Utils.LogColor(federationId);

                actionSuccess?.Invoke();
            }
            else
            {
                Utils.LogColor("정보 불러오기 실패.");
                Utils.LogColor(callback.ToString());

                actionFailure?.Invoke();
            }

            processComplete = true;
        });

        while (!processComplete)
            await Task.Yield();
    }
    #endregion

    #region Data.
    //public void InsertData()
    //{
    //    Param param = new();
    //    param.Add("AccountData", SaveLoadManager.Instance.DataAccount);

    //    BackendReturnObject backendReturnObject = Backend.GameData.Insert("AccountData", param);

    //    if (backendReturnObject.IsSuccess())
    //    {
    //        Utils.LogColor("저장 성공");
    //    }
    //    else
    //    {
    //        Utils.LogColor("저장 실패");
    //        Utils.LogColor(Utils.String.MergeString("Code : ", backendReturnObject.GetStatusCode(),
    //            " / Message : ", backendReturnObject.GetMessage()));
    //        //switch (backendReturnObject.GetStatusCode())
    //        //{
    //        //    case "412":
    //        //        Utils.LogColor("비활성화 된 tableName");
    //        //        break;
    //        //    case "413":
    //        //        Utils.LogColor("데이터의 크기가 400KB를 넘음");
    //        //        break;
    //        //    case "400":
    //        //        Utils.LogColor("저장 데이터의 컬럼갯수가 290개를 넘어감");
    //        //        break;
    //        //    case "404":
    //        //        Utils.LogColor("존재하지 않는 table");
    //        //        break;
    //        //    default:
    //        //        break;
    //        //}
    //    }
    //}

    //public void LoadData(string tableName)
    //{
    //    BackendReturnObject backendReturnObject = Backend.GameData.GetMyData(tableName, new Where());

    //    if (backendReturnObject.IsSuccess())
    //    {
    //        Utils.LogColor("불러오기 성공");

    //        JsonData jsonData = backendReturnObject.FlattenRows();

    //        DataAccount dataAccount = JsonConvert.DeserializeObject<DataAccount>(jsonData[0]["AccountData"].ToJson());

    //        Utils.LogColor(JsonConvert.SerializeObject(dataAccount));
    //    }
    //    else
    //    {
    //        Utils.LogColor("불러오기 실패");
    //        Utils.LogColor(Utils.String.MergeString("Code : ", backendReturnObject.GetStatusCode(),
    //            " / Message : ", backendReturnObject.GetMessage()));
    //        //switch (backendReturnObject.GetStatusCode())
    //        //{
    //        //    case "412":
    //        //        Utils.LogColor("비활성화 된 tableName");
    //        //        break;
    //        //    case "404":
    //        //        Utils.LogColor("존재하지 않는 table");
    //        //        break;
    //        //    default:
    //        //        break;
    //        //}
    //    }
    //}

    //신규유저 DB 생성.
    public IEnumerator CoInsertAllData(Action actionFinish)
    {
        BattleManager.Instance.CreateNewAccountData();

        InsertTransactionDataBasic(SaveLoadManager.Instance.DataAccount.dataBasic);
        InsertTransactionDataStats(SaveLoadManager.Instance.DataAccount.dataStats);
        InsertTransactionDataParty(SaveLoadManager.Instance.DataAccount.dataPartylist);
        InsertTransactionDataCharacter(SaveLoadManager.Instance.DataAccount.dataCharacterlist);
        InsertTransactionDataItems(SaveLoadManager.Instance.DataAccount.dataItems);

        yield return CoSendWriteTransaction(null, null);

        //yield return CoInsertDataBasic(SaveLoadManager.Instance.DataAccount.dataBasic, null, null);
        yield return CoGetDataBasic(SaveLoadManager.Instance.SetDataBasic, null);

        //yield return CoInsertDataStats(SaveLoadManager.Instance.DataAccount.dataStats, null, null);
        yield return CoGetDataStats(SaveLoadManager.Instance.SetDataStats, null);

        //yield return CoInsertDataParty(SaveLoadManager.Instance.DataAccount.listDataParty, null, null);
        yield return CoGetDataParty(SaveLoadManager.Instance.SetDataParty, null);

        //yield return CoInsertDataCharacter(SaveLoadManager.Instance.DataAccount.listDataCharacter, null, null);
        yield return CoGetDataCharacter(SaveLoadManager.Instance.SetDataCharacter, null);

        //yield return CoInsertDataItems(SaveLoadManager.Instance.DataAccount.dataItems, null, null);
        yield return CoGetDataItems(SaveLoadManager.Instance.SetDataItems, null);

#if UNITY_EDITOR
        GetLocalAccountInfo();
        yield return CoGetServerAccountInfo(null, null);
#endif

        actionFinish?.Invoke();
    }

    //기존유저 서버데이터 파싱.
    public IEnumerator CoLoadData(Action actionFinish)
    {
        yield return CoGetDataBasic(SaveLoadManager.Instance.SetDataBasic, null);

        yield return CoGetDataStats(SaveLoadManager.Instance.SetDataStats, null);

        yield return CoGetDataParty(SaveLoadManager.Instance.SetDataParty, null);

        yield return CoGetDataCharacter(SaveLoadManager.Instance.SetDataCharacter, null);

        yield return CoGetDataItems(SaveLoadManager.Instance.SetDataItems, null);

#if UNITY_EDITOR
        GetLocalAccountInfo();
        yield return CoGetServerAccountInfo(null, null);
#endif

        actionFinish?.Invoke();
    }
    public async Task AsyncLoadData()
    {
        await AsyncGetDataBasic(SaveLoadManager.Instance.SetDataBasic, null);

        await AsyncGetDataStats(SaveLoadManager.Instance.SetDataStats, null);

        await AsyncGetDataParty(SaveLoadManager.Instance.SetDataParty, null);

        await AsyncGetDataCharacter(SaveLoadManager.Instance.SetDataCharacter, null);

        await AsyncGetDataItems(SaveLoadManager.Instance.SetDataItems, null);

#if UNITY_EDITOR
        GetLocalAccountInfo();
        await AsyncGetServerAccountInfo(null, null);
#endif
    }
    #endregion

    #region DataBasic.
    //기본정보 DB 생성.
    public IEnumerator CoInsertDataBasic(DataBasic dataBasic, Action actionSuccess, Action actionFailure)
    {
        bool processComplete = false;

        Param param = new()
        {
            { "AccountName", dataBasic.accountName },
            { "AccountLevel", dataBasic.accountLevel },
            { "AccountEXP_string", dataBasic.accountEXP_string },
            { "ProfileCharacterID", dataBasic.profileCharacterID },
            { "Gold_string", dataBasic.gold_string },
            { "GrowUpPoint", dataBasic.GrowUpPoint },
            { "Gem", dataBasic.Gem },
            { "Chapter", dataBasic.chapter },
            { "Stage", dataBasic.stage }
        };

        Backend.GameData.Insert("DataBasic", param, (callback) =>
        {
            if (callback.IsSuccess())
            {
                Utils.LogColor("DataBasic DB 생성 성공");

                actionSuccess?.Invoke();
            }
            else
            {
                Utils.LogColor("DataBasic DB 생성 실패");
                Utils.LogColor(Utils.String.MergeString("Code : ", callback.GetStatusCode(),
                    " / Message : ", callback.GetMessage()));

                actionFailure?.Invoke();
            }

            processComplete = true;
        });

        yield return new WaitUntil(() => processComplete == true);
    }
    public async Task AsyncInsertDataBasic(DataBasic dataBasic, Action actionSuccess, Action actionFailure)
    {
        bool processComplete = false;

        Param param = new()
        {
            { "AccountName", dataBasic.accountName },
            { "AccountLevel", dataBasic.accountLevel },
            { "AccountEXP_string", dataBasic.accountEXP_string },
            { "ProfileCharacterID", dataBasic.profileCharacterID },
            { "Gold_string", dataBasic.gold_string },
            { "GrowUpPoint", dataBasic.GrowUpPoint },
            { "Gem", dataBasic.Gem },
            { "Chapter", dataBasic.chapter },
            { "Stage", dataBasic.stage }
        };

        Backend.GameData.Insert("DataBasic", param, (callback) =>
        {
            if (callback.IsSuccess())
            {
                Utils.LogColor("DataBasic DB 생성 성공");

                actionSuccess?.Invoke();
            }
            else
            {
                Utils.LogColor("DataBasic DB 생성 실패");
                Utils.LogColor(Utils.String.MergeString("Code : ", callback.GetStatusCode(),
                    " / Message : ", callback.GetMessage()));

                actionFailure?.Invoke();
            }

            processComplete = true;
        });

        while (!processComplete)
            await Task.Yield();
    }
    //기본정보 DB 전체 업데이트.
    public IEnumerator CoUpdateDataBasic(DataBasic dataBasic, Action actionSuccess, Action actionFailure)
    {
        bool processComplete = false;

        Param param = new()
        {
            { "AccountName", dataBasic.accountName },
            { "AccountLevel", dataBasic.accountLevel },
            { "AccountEXP_string", dataBasic.accountEXP_string },
            { "ProfileCharacterID", dataBasic.profileCharacterID },
            { "Gold_string", dataBasic.gold_string },
            { "GrowUpPoint", dataBasic.GrowUpPoint },
            { "Gem", dataBasic.Gem },
            { "Chapter", dataBasic.chapter },
            { "Stage", dataBasic.stage }
        };

        //매뉴얼 매개변수 순서 잘못됨.
        Backend.GameData.Update("DataBasic", new Where(), param, (callback) =>
        {
            if (callback.IsSuccess())
            {
                Utils.LogColor("DataBasic DB 갱신 성공");

                actionSuccess?.Invoke();
            }
            else
            {
                Utils.LogColor("DataBasic DB 갱신 실패");
                Utils.LogColor(Utils.String.MergeString("Code : ", callback.GetStatusCode(),
                    " / Message : ", callback.GetMessage()));

                actionFailure?.Invoke();
            }

            processComplete = true;
        });

        yield return new WaitUntil(() => processComplete == true);
    }
    public async Task AsyncUpdateDataBasic(DataBasic dataBasic, Action actionSuccess, Action actionFailure)
    {
        bool processComplete = false;

        Param param = new()
        {
            { "AccountName", dataBasic.accountName },
            { "AccountLevel", dataBasic.accountLevel },
            { "AccountEXP_string", dataBasic.accountEXP_string },
            { "ProfileCharacterID", dataBasic.profileCharacterID },
            { "Gold_string", dataBasic.gold_string },
            { "GrowUpPoint", dataBasic.GrowUpPoint },
            { "Gem", dataBasic.Gem },
            { "Chapter", dataBasic.chapter },
            { "Stage", dataBasic.stage }
        };

        //매뉴얼 매개변수 순서 잘못됨.
        Backend.GameData.Update("DataBasic", new Where(), param, (callback) =>
        {
            if (callback.IsSuccess())
            {
                Utils.LogColor("DataBasic DB 갱신 성공");

                actionSuccess?.Invoke();
            }
            else
            {
                Utils.LogColor("DataBasic DB 갱신 실패");
                Utils.LogColor(Utils.String.MergeString("Code : ", callback.GetStatusCode(),
                    " / Message : ", callback.GetMessage()));

                actionFailure?.Invoke();
            }

            processComplete = true;
        });

        while (!processComplete)
            await Task.Yield();
    }
    //기본정보 DB 불러오기.
    public IEnumerator CoGetDataBasic(Action<DataBasic> actionSuccess, Action actionFailure)
    {
        bool processComplete = false;

        Backend.GameData.GetMyData("DataBasic", new Where(), callback =>
        {
            if (callback.IsSuccess())
            {
                if (callback.GetReturnValuetoJSON().Count <= 0)
                {
                    Utils.LogColor("DataBasic DB 데이터 없음");
                    Utils.LogColor(Utils.String.MergeString("Code : ", callback.GetStatusCode(),
                        " / Message : ", callback.GetMessage()));
                }
                else
                {
                    JsonData jsonData = callback.FlattenRows();

                    DataBasic dataBasic = JsonConvert.DeserializeObject<DataBasic>(jsonData[0].ToJson());

                    Utils.LogColor(Utils.String.MergeString("DataBasic : ", JsonConvert.SerializeObject(dataBasic)));

                    actionSuccess?.Invoke(dataBasic);
                }
            }
            else
            {
                Utils.LogColor("DataBasic DB 불러오기 실패");
                Utils.LogColor(Utils.String.MergeString("Code : ", callback.GetStatusCode(),
                    " / Message : ", callback.GetMessage()));

                actionFailure?.Invoke();
            }

            processComplete = true;
        });

        yield return new WaitUntil(() => processComplete == true);
    }
    public async Task AsyncGetDataBasic(Action<DataBasic> actionSuccess, Action actionFailure)
    {
        bool processComplete = false;

        Backend.GameData.GetMyData("DataBasic", new Where(), callback =>
        {
            if (callback.IsSuccess())
            {
                if (callback.GetReturnValuetoJSON().Count <= 0)
                {
                    Utils.LogColor("DataBasic DB 데이터 없음");
                    Utils.LogColor(Utils.String.MergeString("Code : ", callback.GetStatusCode(),
                        " / Message : ", callback.GetMessage()));
                }
                else
                {
                    JsonData jsonData = callback.FlattenRows();

                    DataBasic dataBasic = JsonConvert.DeserializeObject<DataBasic>(jsonData[0].ToJson());

                    Utils.LogColor(Utils.String.MergeString("DataBasic : ", JsonConvert.SerializeObject(dataBasic)));

                    actionSuccess?.Invoke(dataBasic);
                }
            }
            else
            {
                Utils.LogColor("DataBasic DB 불러오기 실패");
                Utils.LogColor(Utils.String.MergeString("Code : ", callback.GetStatusCode(),
                    " / Message : ", callback.GetMessage()));

                actionFailure?.Invoke();
            }

            processComplete = true;
        });

        while (!processComplete)
            await Task.Yield();
    }
    #endregion

    #region Stats.
    //스탯 DB 생성.
    public IEnumerator CoInsertDataStats(DataStats dataStats, Action actionSuccess, Action actionFailure)
    {
        bool processComplete = false;

        Param param = new()
        {
            { "Accuracy", dataStats.accuracy },
            { "AccuracyLevel", dataStats.accuracyLevel },
            { "AccuracyLevel_Growup", dataStats.accuracyLevel_Growup },
            { "Attack_String", dataStats.attack_String },
            { "AttackLevel", dataStats.attackLevel },
            { "AttackLevel_Growup", dataStats.attackLevel_Growup },
            { "CriticalDamage", dataStats.criticalDamage },
            { "CriticalDamageLevel", dataStats.criticalDamageLevel },
            { "CriticalDamageLevel_Growup", dataStats.criticalDamageLevel_Growup },
            { "CriticalRate", dataStats.criticalRate },
            { "CriticalRateLevel", dataStats.criticalRateLevel },
            { "CriticalRateLevel_Growup", dataStats.criticalRateLevel_Growup },
            { "Evade", dataStats.evade },
            { "EvadeLevel", dataStats.evadeLevel },
            { "EvadeLevel_Growup", dataStats.evadeLevel_Growup },
            { "Hp_String", dataStats.hp_String },
            { "HpLevel", dataStats.hpLevel },
            { "HpLevel_Growup", dataStats.hpLevel_Growup },
            { "MaxDamage", dataStats.maxDamage },
            { "MaxDamageLevel", dataStats.maxDamageLevel },
            { "MaxDamageLevel_Growup", dataStats.maxDamageLevel_Growup },
            { "MinDamage", dataStats.minDamage },
            { "MinDamageLevel", dataStats.minDamageLevel },
            { "MinDamageLevel_Growup", dataStats.minDamageLevel_Growup },
        };

        Backend.GameData.Insert("DataStats", param, (callback) =>
        {
            if (callback.IsSuccess())
            {
                Utils.LogColor("DataStats DB 생성 성공");

                actionSuccess?.Invoke();
            }
            else
            {
                Utils.LogColor("DataStats DB 생성 실패");
                Utils.LogColor(Utils.String.MergeString("Code : ", callback.GetStatusCode(),
                    " / Message : ", callback.GetMessage()));

                actionFailure?.Invoke();
            }

            processComplete = true;
        });

        yield return new WaitUntil(() => processComplete == true);
    }
    public async Task AsyncInsertDataStats(DataStats dataStats, Action actionSuccess, Action actionFailure)
    {
        bool processComplete = false;

        Param param = new()
        {
            { "Accuracy", dataStats.accuracy },
            { "AccuracyLevel", dataStats.accuracyLevel },
            { "AccuracyLevel_Growup", dataStats.accuracyLevel_Growup },
            { "Attack_String", dataStats.attack_String },
            { "AttackLevel", dataStats.attackLevel },
            { "AttackLevel_Growup", dataStats.attackLevel_Growup },
            { "CriticalDamage", dataStats.criticalDamage },
            { "CriticalDamageLevel", dataStats.criticalDamageLevel },
            { "CriticalDamageLevel_Growup", dataStats.criticalDamageLevel_Growup },
            { "CriticalRate", dataStats.criticalRate },
            { "CriticalRateLevel", dataStats.criticalRateLevel },
            { "CriticalRateLevel_Growup", dataStats.criticalRateLevel_Growup },
            { "Evade", dataStats.evade },
            { "EvadeLevel", dataStats.evadeLevel },
            { "EvadeLevel_Growup", dataStats.evadeLevel_Growup },
            { "Hp_String", dataStats.hp_String },
            { "HpLevel", dataStats.hpLevel },
            { "HpLevel_Growup", dataStats.hpLevel_Growup },
            { "MaxDamage", dataStats.maxDamage },
            { "MaxDamageLevel", dataStats.maxDamageLevel },
            { "MaxDamageLevel_Growup", dataStats.maxDamageLevel_Growup },
            { "MinDamage", dataStats.minDamage },
            { "MinDamageLevel", dataStats.minDamageLevel },
            { "MinDamageLevel_Growup", dataStats.minDamageLevel_Growup },
        };

        Backend.GameData.Insert("DataStats", param, (callback) =>
        {
            if (callback.IsSuccess())
            {
                Utils.LogColor("DataStats DB 생성 성공");

                actionSuccess?.Invoke();
            }
            else
            {
                Utils.LogColor("DataStats DB 생성 실패");
                Utils.LogColor(Utils.String.MergeString("Code : ", callback.GetStatusCode(),
                    " / Message : ", callback.GetMessage()));

                actionFailure?.Invoke();
            }

            processComplete = true;
        });

        while (!processComplete)
            await Task.Yield();
    }
    //스탯 DB 전체 업데이트.
    public IEnumerator CoUpdateDataStats(DataStats dataStats, Action actionSuccess, Action actionFailure)
    {
        bool processComplete = false;

        Param param = new()
        {
            { "Accuracy", dataStats.accuracy },
            { "AccuracyLevel", dataStats.accuracyLevel },
            { "AccuracyLevel_Growup", dataStats.accuracyLevel_Growup },
            { "Attack_String", dataStats.attack_String },
            { "AttackLevel", dataStats.attackLevel },
            { "AttackLevel_Growup", dataStats.attackLevel_Growup },
            { "CriticalDamage", dataStats.criticalDamage },
            { "CriticalDamageLevel", dataStats.criticalDamageLevel },
            { "CriticalDamageLevel_Growup", dataStats.criticalDamageLevel_Growup },
            { "CriticalRate", dataStats.criticalRate },
            { "CriticalRateLevel", dataStats.criticalRateLevel },
            { "CriticalRateLevel_Growup", dataStats.criticalRateLevel_Growup },
            { "Evade", dataStats.evade },
            { "EvadeLevel", dataStats.evadeLevel },
            { "EvadeLevel_Growup", dataStats.evadeLevel_Growup },
            { "Hp_String", dataStats.hp_String },
            { "HpLevel", dataStats.hpLevel },
            { "HpLevel_Growup", dataStats.hpLevel_Growup },
            { "MaxDamage", dataStats.maxDamage },
            { "MaxDamageLevel", dataStats.maxDamageLevel },
            { "MaxDamageLevel_Growup", dataStats.maxDamageLevel_Growup },
            { "MinDamage", dataStats.minDamage },
            { "MinDamageLevel", dataStats.minDamageLevel },
            { "MinDamageLevel_Growup", dataStats.minDamageLevel_Growup },
        };

        //매뉴얼 매개변수 순서 잘못됨.
        Backend.GameData.Update("DataStats", new Where(), param, (callback) =>
        {
            if (callback.IsSuccess())
            {
                Utils.LogColor("DataStats DB 갱신 성공");

                actionSuccess?.Invoke();
            }
            else
            {
                Utils.LogColor("DataStats DB 갱신 실패");
                Utils.LogColor(Utils.String.MergeString("Code : ", callback.GetStatusCode(),
                    " / Message : ", callback.GetMessage()));

                actionFailure?.Invoke();
            }

            processComplete = true;
        });

        yield return new WaitUntil(() => processComplete == true);
    }
    public async Task AsyncUpdateDataStats(DataStats dataStats, Action actionSuccess, Action actionFailure)
    {
        bool processComplete = false;

        Param param = new()
        {
            { "Accuracy", dataStats.accuracy },
            { "AccuracyLevel", dataStats.accuracyLevel },
            { "AccuracyLevel_Growup", dataStats.accuracyLevel_Growup },
            { "Attack_String", dataStats.attack_String },
            { "AttackLevel", dataStats.attackLevel },
            { "AttackLevel_Growup", dataStats.attackLevel_Growup },
            { "CriticalDamage", dataStats.criticalDamage },
            { "CriticalDamageLevel", dataStats.criticalDamageLevel },
            { "CriticalDamageLevel_Growup", dataStats.criticalDamageLevel_Growup },
            { "CriticalRate", dataStats.criticalRate },
            { "CriticalRateLevel", dataStats.criticalRateLevel },
            { "CriticalRateLevel_Growup", dataStats.criticalRateLevel_Growup },
            { "Evade", dataStats.evade },
            { "EvadeLevel", dataStats.evadeLevel },
            { "EvadeLevel_Growup", dataStats.evadeLevel_Growup },
            { "Hp_String", dataStats.hp_String },
            { "HpLevel", dataStats.hpLevel },
            { "HpLevel_Growup", dataStats.hpLevel_Growup },
            { "MaxDamage", dataStats.maxDamage },
            { "MaxDamageLevel", dataStats.maxDamageLevel },
            { "MaxDamageLevel_Growup", dataStats.maxDamageLevel_Growup },
            { "MinDamage", dataStats.minDamage },
            { "MinDamageLevel", dataStats.minDamageLevel },
            { "MinDamageLevel_Growup", dataStats.minDamageLevel_Growup },
        };

        //매뉴얼 매개변수 순서 잘못됨.
        Backend.GameData.Update("DataStats", new Where(), param, (callback) =>
        {
            if (callback.IsSuccess())
            {
                Utils.LogColor("DataStats DB 갱신 성공");

                actionSuccess?.Invoke();
            }
            else
            {
                Utils.LogColor("DataStats DB 갱신 실패");
                Utils.LogColor(Utils.String.MergeString("Code : ", callback.GetStatusCode(),
                    " / Message : ", callback.GetMessage()));

                actionFailure?.Invoke();
            }

            processComplete = true;
        });

        while (!processComplete)
            await Task.Yield();
    }
    //스탯 DB 불러오기.
    public IEnumerator CoGetDataStats(Action<DataStats> actionSuccess, Action actionFailure)
    {
        bool processComplete = false;

        Backend.GameData.GetMyData("DataStats", new Where(), callback =>
        {
            if (callback.IsSuccess())
            {
                if (callback.GetReturnValuetoJSON().Count <= 0)
                {
                    Utils.LogColor("DataStats DB 데이터 없음");
                    Utils.LogColor(Utils.String.MergeString("Code : ", callback.GetStatusCode(),
                        " / Message : ", callback.GetMessage()));
                }
                else
                {
                    JsonData jsonData = callback.FlattenRows();

                    DataStats dataStats = JsonConvert.DeserializeObject<DataStats>(jsonData[0].ToJson());

                    Utils.LogColor(Utils.String.MergeString("DataStats : ", JsonConvert.SerializeObject(dataStats)));

                    actionSuccess?.Invoke(dataStats);
                }                
            }
            else
            {
                Utils.LogColor("DataStats DB 불러오기 실패");
                Utils.LogColor(Utils.String.MergeString("Code : ", callback.GetStatusCode(),
                    " / Message : ", callback.GetMessage()));

                actionFailure?.Invoke();
            }

            processComplete = true;
        });

        yield return new WaitUntil(() => processComplete == true);
    }
    public async Task AsyncGetDataStats(Action<DataStats> actionSuccess, Action actionFailure)
    {
        bool processComplete = false;

        Backend.GameData.GetMyData("DataStats", new Where(), callback =>
        {
            if (callback.IsSuccess())
            {
                if (callback.GetReturnValuetoJSON().Count <= 0)
                {
                    Utils.LogColor("DataStats DB 데이터 없음");
                    Utils.LogColor(Utils.String.MergeString("Code : ", callback.GetStatusCode(),
                        " / Message : ", callback.GetMessage()));
                }
                else
                {
                    JsonData jsonData = callback.FlattenRows();

                    DataStats dataStats = JsonConvert.DeserializeObject<DataStats>(jsonData[0].ToJson());

                    Utils.LogColor(Utils.String.MergeString("DataStats : ", JsonConvert.SerializeObject(dataStats)));

                    actionSuccess?.Invoke(dataStats);
                }
            }
            else
            {
                Utils.LogColor("DataStats DB 불러오기 실패");
                Utils.LogColor(Utils.String.MergeString("Code : ", callback.GetStatusCode(),
                    " / Message : ", callback.GetMessage()));

                actionFailure?.Invoke();
            }

            processComplete = true;
        });

        while (!processComplete)
            await Task.Yield();
    }
    #endregion

    #region Party List.
    //파티 DB 생성.
    public IEnumerator CoInsertDataParty(List<DataCharacter> dataPartylist, Action actionSuccess, Action actionFailure)
    {
        bool processComplete = false;

        Param param = new()
        {
            { "PartyList", dataPartylist }
        };

        Backend.GameData.Insert("DataParty", param, (callback) =>
        {
            if (callback.IsSuccess())
            {
                Utils.LogColor("DataParty DB 생성 성공");

                actionSuccess?.Invoke();
            }
            else
            {
                Utils.LogColor("DataParty DB 생성 실패");
                Utils.LogColor(Utils.String.MergeString("Code : ", callback.GetStatusCode(),
                    " / Message : ", callback.GetMessage()));

                actionFailure?.Invoke();
            }

            processComplete = true;
        });

        yield return new WaitUntil(() => processComplete == true);
    }
    public async Task AsyncInsertDataParty(List<DataCharacter> dataPartylist, Action actionSuccess, Action actionFailure)
    {
        bool processComplete = false;

        Param param = new()
        {
            { "PartyList", dataPartylist }
        };

        Backend.GameData.Insert("DataParty", param, (callback) =>
        {
            if (callback.IsSuccess())
            {
                Utils.LogColor("DataParty DB 생성 성공");

                actionSuccess?.Invoke();
            }
            else
            {
                Utils.LogColor("DataParty DB 생성 실패");
                Utils.LogColor(Utils.String.MergeString("Code : ", callback.GetStatusCode(),
                    " / Message : ", callback.GetMessage()));

                actionFailure?.Invoke();
            }

            processComplete = true;
        });

        while (!processComplete)
            await Task.Yield();
    }
    //파티 DB 전체 업데이트.
    public IEnumerator CoUpdateDataParty(List<DataCharacter> dataPartylist, Action actionSuccess, Action actionFailure)
    {
        bool processComplete = false;

        Param param = new()
        {
            { "PartyList", dataPartylist }
        };

        //매뉴얼 매개변수 순서 잘못됨.
        Backend.GameData.Update("DataParty", new Where(), param, (callback) =>
        {
            if (callback.IsSuccess())
            {
                Utils.LogColor("DataParty DB 갱신 성공");

                actionSuccess?.Invoke();
            }
            else
            {
                Utils.LogColor("DataParty DB 갱신 실패");
                Utils.LogColor(Utils.String.MergeString("Code : ", callback.GetStatusCode(),
                    " / Message : ", callback.GetMessage()));

                actionFailure?.Invoke();
            }

            processComplete = true;
        });

        yield return new WaitUntil(() => processComplete == true);
    }
    public async Task AsyncUpdateDataParty(List<DataCharacter> dataPartylist, Action actionSuccess, Action actionFailure)
    {
        bool processComplete = false;

        Param param = new()
        {
            { "PartyList", dataPartylist }
        };

        //매뉴얼 매개변수 순서 잘못됨.
        Backend.GameData.Update("DataParty", new Where(), param, (callback) =>
        {
            if (callback.IsSuccess())
            {
                Utils.LogColor("DataParty DB 갱신 성공");

                actionSuccess?.Invoke();
            }
            else
            {
                Utils.LogColor("DataParty DB 갱신 실패");
                Utils.LogColor(Utils.String.MergeString("Code : ", callback.GetStatusCode(),
                    " / Message : ", callback.GetMessage()));

                actionFailure?.Invoke();
            }

            processComplete = true;
        });

        while (!processComplete)
            await Task.Yield();
    }
    //파티 DB 불러오기.
    public IEnumerator CoGetDataParty(Action<List<DataCharacter>> actionSuccess, Action actionFailure)
    {
        bool processComplete = false;

        Backend.GameData.GetMyData("DataParty", new Where(), callback =>
        {
            if (callback.IsSuccess())
            {
                if (callback.GetReturnValuetoJSON()["rows"].Count <= 0)
                {
                    Utils.LogColor("DataParty DB 데이터 없음");
                    Utils.LogColor(Utils.String.MergeString("Code : ", callback.GetStatusCode(),
                        " / Message : ", callback.GetMessage()));
                }
                else
                {
                    JsonData jsonData = callback.FlattenRows();

                    List<DataCharacter> list = JsonConvert.DeserializeObject<List<DataCharacter>>(jsonData[0]["PartyList"].ToJson());

                    //List<DataCharacter> list = LitJson.JsonMapper.ToObject<List<DataCharacter>>(jsonData.ToJson());

                    Utils.LogColor(Utils.String.MergeString("DataParty : ", JsonConvert.SerializeObject(list)));

                    actionSuccess?.Invoke(list);
                }
            }
            else
            {
                Utils.LogColor("DataParty DB 불러오기 실패");
                Utils.LogColor(Utils.String.MergeString("Code : ", callback.GetStatusCode(),
                    " / Message : ", callback.GetMessage()));

                actionFailure?.Invoke();
            }

            processComplete = true;
        });

        yield return new WaitUntil(() => processComplete == true);
    }
    public async Task AsyncGetDataParty(Action<List<DataCharacter>> actionSuccess, Action actionFailure)
    {
        bool processComplete = false;

        Backend.GameData.GetMyData("DataParty", new Where(), callback =>
        {
            if (callback.IsSuccess())
            {
                if (callback.GetReturnValuetoJSON()["rows"].Count <= 0)
                {
                    Utils.LogColor("DataParty DB 데이터 없음");
                    Utils.LogColor(Utils.String.MergeString("Code : ", callback.GetStatusCode(),
                        " / Message : ", callback.GetMessage()));
                }
                else
                {
                    JsonData jsonData = callback.FlattenRows();

                    List<DataCharacter> list = JsonConvert.DeserializeObject<List<DataCharacter>>(jsonData[0]["PartyList"].ToJson());

                    //List<DataCharacter> list = LitJson.JsonMapper.ToObject<List<DataCharacter>>(jsonData.ToJson());

                    Utils.LogColor(Utils.String.MergeString("DataParty : ", JsonConvert.SerializeObject(list)));

                    actionSuccess?.Invoke(list);
                }
            }
            else
            {
                Utils.LogColor("DataParty DB 불러오기 실패");
                Utils.LogColor(Utils.String.MergeString("Code : ", callback.GetStatusCode(),
                    " / Message : ", callback.GetMessage()));

                actionFailure?.Invoke();
            }

            processComplete = true;
        });

        while (!processComplete)
            await Task.Yield();
    }
    #endregion

    #region Character List.
    //캐릭터리스트 DB 생성.
    public IEnumerator CoInsertDataCharacter(List<DataCharacter> dataCharacterlist, Action actionSuccess, Action actionFailure)
    {
        bool processComplete = false;

        Param param = new()
        {
            { "CharacterList", dataCharacterlist }
        };

        Backend.GameData.Insert("DataCharacter", param, (callback) =>
        {
            if (callback.IsSuccess())
            {
                Utils.LogColor("DataCharacter DB 생성 성공");

                actionSuccess?.Invoke();
            }
            else
            {
                Utils.LogColor("DataCharacter DB 생성 실패");
                Utils.LogColor(Utils.String.MergeString("Code : ", callback.GetStatusCode(),
                    " / Message : ", callback.GetMessage()));

                actionFailure?.Invoke();
            }

            processComplete = true;
        });

        yield return new WaitUntil(() => processComplete == true);
    }
    public async Task AsyncInsertDataCharater(List<DataCharacter> dataCharacterlist, Action actionSuccess, Action actionFailure)
    {
        bool processComplete = false;

        Param param = new()
        {
            { "CharacterList", dataCharacterlist }
        };

        Backend.GameData.Insert("DataCharacter", param, (callback) =>
        {
            if (callback.IsSuccess())
            {
                Utils.LogColor("DataCharacter DB 생성 성공");

                actionSuccess?.Invoke();
            }
            else
            {
                Utils.LogColor("DataCharacter DB 생성 실패");
                Utils.LogColor(Utils.String.MergeString("Code : ", callback.GetStatusCode(),
                    " / Message : ", callback.GetMessage()));

                actionFailure?.Invoke();
            }

            processComplete = true;
        });

        while (!processComplete)
            await Task.Yield();
    }
    //캐릭터리스트 DB 전체 업데이트.
    public IEnumerator CoUpdateDataCharater(List<DataCharacter> dataCharacterlist, Action actionSuccess, Action actionFailure)
    {
        bool processComplete = false;

        Param param = new()
        {
            { "CharacterList", dataCharacterlist }
        };

        //매뉴얼 매개변수 순서 잘못됨.
        Backend.GameData.Update("DataCharacter", new Where(), param, (callback) =>
        {
            if (callback.IsSuccess())
            {
                Utils.LogColor("DataCharacter DB 갱신 성공");

                actionSuccess?.Invoke();
            }
            else
            {
                Utils.LogColor("DataCharacter DB 갱신 실패");
                Utils.LogColor(Utils.String.MergeString("Code : ", callback.GetStatusCode(),
                    " / Message : ", callback.GetMessage()));

                actionFailure?.Invoke();
            }

            processComplete = true;
        });

        yield return new WaitUntil(() => processComplete == true);
    }
    public async Task AsyncUpdateDataCharater(List<DataCharacter> dataCharacterlist, Action actionSuccess, Action actionFailure)
    {
        bool processComplete = false;

        Param param = new()
        {
            { "CharacterList", dataCharacterlist }
        };

        //매뉴얼 매개변수 순서 잘못됨.
        Backend.GameData.Update("DataCharacter", new Where(), param, (callback) =>
        {
            if (callback.IsSuccess())
            {
                Utils.LogColor("DataCharacter DB 갱신 성공");

                actionSuccess?.Invoke();
            }
            else
            {
                Utils.LogColor("DataCharacter DB 갱신 실패");
                Utils.LogColor(Utils.String.MergeString("Code : ", callback.GetStatusCode(),
                    " / Message : ", callback.GetMessage()));

                actionFailure?.Invoke();
            }

            processComplete = true;
        });

        while (!processComplete)
            await Task.Yield();
    }
    //캐릭터리스트 DB 불러오기.
    public IEnumerator CoGetDataCharacter(Action<List<DataCharacter>> actionSuccess, Action actionFailure)
    {
        bool processComplete = false;

        Backend.GameData.GetMyData("DataCharacter", new Where(), callback =>
        {
            if (callback.IsSuccess())
            {
                if (callback.GetReturnValuetoJSON()["rows"].Count <= 0)
                {
                    Utils.LogColor("DataCharacter DB 데이터 없음");
                    Utils.LogColor(Utils.String.MergeString("Code : ", callback.GetStatusCode(),
                        " / Message : ", callback.GetMessage()));
                }
                else
                {
                    JsonData jsonData = callback.FlattenRows();

                    List<DataCharacter> list = JsonConvert.DeserializeObject<List<DataCharacter>>(jsonData[0]["CharacterList"].ToJson());

                    Utils.LogColor(Utils.String.MergeString("DataCharacter : ", JsonConvert.SerializeObject(list)));

                    actionSuccess?.Invoke(list);
                }
            }
            else
            {
                Utils.LogColor("DataCharacter DB 불러오기 실패");
                Utils.LogColor(Utils.String.MergeString("Code : ", callback.GetStatusCode(),
                    " / Message : ", callback.GetMessage()));

                actionFailure?.Invoke();
            }

            processComplete = true;
        });

        yield return new WaitUntil(() => processComplete == true);
    }
    public async Task AsyncGetDataCharacter(Action<List<DataCharacter>> actionSuccess, Action actionFailure)
    {
        bool processComplete = false;

        Backend.GameData.GetMyData("DataCharacter", new Where(), callback =>
        {
            if (callback.IsSuccess())
            {
                if (callback.GetReturnValuetoJSON()["rows"].Count <= 0)
                {
                    Utils.LogColor("DataCharacter DB 데이터 없음");
                    Utils.LogColor(Utils.String.MergeString("Code : ", callback.GetStatusCode(),
                        " / Message : ", callback.GetMessage()));
                }
                else
                {
                    JsonData jsonData = callback.FlattenRows();

                    List<DataCharacter> list = JsonConvert.DeserializeObject<List<DataCharacter>>(jsonData[0]["CharacterList"].ToJson());

                    Utils.LogColor(Utils.String.MergeString("DataCharacter : ", JsonConvert.SerializeObject(list)));
                    Utils.LogColor(Utils.String.MergeString("DataCharacter1 : ", LitJson.JsonMapper.ToJson(list)));

                    actionSuccess?.Invoke(list);
                }
            }
            else
            {
                Utils.LogColor("DataCharacter DB 불러오기 실패");
                Utils.LogColor(Utils.String.MergeString("Code : ", callback.GetStatusCode(),
                    " / Message : ", callback.GetMessage()));

                actionFailure?.Invoke();
            }

            processComplete = true;
        });

        while (!processComplete)
            await Task.Yield();
    }
    #endregion

    #region Item List.
    //아이템리스트 DB 생성.
    public IEnumerator CoInsertDataItems(DataItems dataItems, Action actionSuccess, Action actionFailure)
    {
        bool processComplete = false;

        Param param = new()
        {
            { "dataItems", dataItems }
        };

        Backend.GameData.Insert("DataItems", param, (callback) =>
        {
            if (callback.IsSuccess())
            {
                Utils.LogColor("DataItems DB 생성 성공");

                actionSuccess?.Invoke();
            }
            else
            {
                Utils.LogColor("DataItems DB 생성 실패");
                Utils.LogColor(Utils.String.MergeString("Code : ", callback.GetStatusCode(),
                    " / Message : ", callback.GetMessage()));

                actionFailure?.Invoke();
            }

            processComplete = true;
        });

        yield return new WaitUntil(() => processComplete == true);
    }
    public async Task AsyncInsertDataItems(DataItems dataItems, Action actionSuccess, Action actionFailure)
    {
        bool processComplete = false;

        Param param = new()
        {
            { "dataItems", dataItems }
        };

        Backend.GameData.Insert("DataItems", param, (callback) =>
        {
            if (callback.IsSuccess())
            {
                Utils.LogColor("DataItems DB 생성 성공");

                actionSuccess?.Invoke();
            }
            else
            {
                Utils.LogColor("DataItems DB 생성 실패");
                Utils.LogColor(Utils.String.MergeString("Code : ", callback.GetStatusCode(),
                    " / Message : ", callback.GetMessage()));

                actionFailure?.Invoke();
            }

            processComplete = true;
        });

        while (!processComplete)
            await Task.Yield();
    }
    //아이템리스트 DB 전체 업데이트.
    public IEnumerator CoUpdateDataItems(DataItems dataItems, Action actionSuccess, Action actionFailure)
    {
        bool processComplete = false;

        Param param = new()
        {
            { "dataItems", dataItems }
        };

        //매뉴얼 매개변수 순서 잘못됨.
        Backend.GameData.Update("DataItems", new Where(), param, (callback) =>
        {
            if (callback.IsSuccess())
            {
                Utils.LogColor("DataItems DB 갱신 성공");

                actionSuccess?.Invoke();
            }
            else
            {
                Utils.LogColor("DataItems DB 갱신 실패");
                Utils.LogColor(Utils.String.MergeString("Code : ", callback.GetStatusCode(),
                    " / Message : ", callback.GetMessage()));

                actionFailure?.Invoke();
            }

            processComplete = true;
        });

        yield return new WaitUntil(() => processComplete == true);
    }
    public async Task AsyncUpdateDataItems(DataItems dataItems, Action actionSuccess, Action actionFailure)
    {
        bool processComplete = false;

        Param param = new()
        {
            { "dataItems", dataItems }
        };

        //매뉴얼 매개변수 순서 잘못됨.
        Backend.GameData.Update("DataItems", new Where(), param, (callback) =>
        {
            if (callback.IsSuccess())
            {
                Utils.LogColor("DataItems DB 갱신 성공");

                actionSuccess?.Invoke();
            }
            else
            {
                Utils.LogColor("DataItems DB 갱신 실패");
                Utils.LogColor(Utils.String.MergeString("Code : ", callback.GetStatusCode(),
                    " / Message : ", callback.GetMessage()));

                actionFailure?.Invoke();
            }

            processComplete = true;
        });

        while (!processComplete)
            await Task.Yield();
    }
    //아이템리스트 DB 불러오기.
    public IEnumerator CoGetDataItems(Action<DataItems> actionSuccess, Action actionFailure)
    {
        bool processComplete = false;

        Backend.GameData.GetMyData("DataItems", new Where(), callback =>
        {
            if (callback.IsSuccess())
            {
                if (callback.GetReturnValuetoJSON().Count <= 0)
                {
                    Utils.LogColor("DataItems DB 데이터 없음");
                    Utils.LogColor(Utils.String.MergeString("Code : ", callback.GetStatusCode(),
                        " / Message : ", callback.GetMessage()));
                }
                else
                {
                    JsonData jsonData = callback.FlattenRows();

                    DataItems dataItems = new()
                    {
                        listCharacterPiece = JsonMapper.ToObject<List<ItemCharacterPiece>>(jsonData[0]["dataItems"]["listCharacterPiece"].ToJson())
                    };

                    Utils.LogColor(Utils.String.MergeString("DataItems : ", JsonConvert.SerializeObject(dataItems)));

                    actionSuccess?.Invoke(dataItems);
                }
            }
            else
            {
                Utils.LogColor("DataItems DB 불러오기 실패");
                Utils.LogColor(Utils.String.MergeString("Code : ", callback.GetStatusCode(),
                    " / Message : ", callback.GetMessage()));

                actionFailure?.Invoke();
            }

            processComplete = true;
        });

        yield return new WaitUntil(() => processComplete == true);
    }
    public async Task AsyncGetDataItems(Action<DataItems> actionSuccess, Action actionFailure)
    {
        bool processComplete = false;

        Backend.GameData.GetMyData("DataItems", new Where(), callback =>
        {
            if (callback.IsSuccess())
            {
                if (callback.GetReturnValuetoJSON().Count <= 0)
                {
                    Utils.LogColor("DataItems DB 데이터 없음");
                    Utils.LogColor(Utils.String.MergeString("Code : ", callback.GetStatusCode(),
                        " / Message : ", callback.GetMessage()));
                }
                else
                {
                    JsonData jsonData = callback.FlattenRows();

                    DataItems dataItems = new()
                    {
                        listCharacterPiece = JsonMapper.ToObject<List<ItemCharacterPiece>>(jsonData[0]["dataItems"]["listCharacterPiece"].ToJson())
                    };

                    Utils.LogColor(Utils.String.MergeString("DataItems : ", JsonConvert.SerializeObject(dataItems)));

                    actionSuccess?.Invoke(dataItems);
                }
            }
            else
            {
                Utils.LogColor("DataItems DB 불러오기 실패");
                Utils.LogColor(Utils.String.MergeString("Code : ", callback.GetStatusCode(),
                    " / Message : ", callback.GetMessage()));

                actionFailure?.Invoke();
            }

            processComplete = true;
        });

        while (!processComplete)
            await Task.Yield();
    }
    #endregion

    #region CharacterPiece Gacha.
    public IEnumerator CoExcuteCharacterPieceGacha(int excuteCount, Action<int> actionSuccess, Action actionFailure)
    {
        bool processComplete = false;

        //매뉴얼 매개변수 순서 잘못됨.
        Backend.Probability.GetProbabilitys("5128", excuteCount, (callback) =>
        {
            if (callback.IsSuccess())
            {
                Utils.LogColor("CharacterPiece gacha 성공");

                JsonData json = callback.GetFlattenJSON()["elements"];

                for (int i = 0; i < json.Count; i++)
                {
                    int.TryParse(json[i]["fragmentRarity"].ToString(), out int fragment);
                    if (fragment == 0)
                    {
                        //로그.
                    }
                    else
                    {
                        actionSuccess?.Invoke(fragment);
                    }
                }
            }
            else
            {
                Utils.LogColor("CharacterPiece gacha 실패");
                Utils.LogColor(Utils.String.MergeString("Code : ", callback.GetStatusCode(),
                    " / Message : ", callback.GetMessage()));

                actionFailure?.Invoke();
            }

            processComplete = true;
        });

        yield return new WaitUntil(() => processComplete == true);
    }
    public async Task AsyncExcuteCharacterPieceGacha(int excuteCount, Action<int> actionSuccess, Action actionFailure)
    {
        bool processComplete = false;

        //매뉴얼 매개변수 순서 잘못됨.
        Backend.Probability.GetProbabilitys("5128", excuteCount, (callback) =>
        {
            if (callback.IsSuccess())
            {
                Utils.LogColor("CharacterPiece gacha 성공");

                JsonData json = callback.GetFlattenJSON()["elements"];

                for (int i = 0; i < json.Count; i++)
                {
                    int.TryParse(json[i]["fragmentRarity"].ToString(), out int fragment);
                    if (fragment == 0)
                    {
                        //로그.
                    }
                    else
                    {
                        actionSuccess?.Invoke(fragment);
                    }
                }                
            }
            else
            {
                Utils.LogColor("CharacterPiece gacha 실패");
                Utils.LogColor(Utils.String.MergeString("Code : ", callback.GetStatusCode(),
                    " / Message : ", callback.GetMessage()));

                actionFailure?.Invoke();
            }

            processComplete = true;
        });

        while (!processComplete)
            await Task.Yield();
    }
    #endregion

    #region Transaction.
    public enum TransactionType
    {
        DataBasic,
        DataStats,
        DataParty,
        DataCharacter,
        DataItems
    }
    private List<TransactionType> listReservasion = new();
    public bool CheckReservasionTransaction()
    {
        return listReservasion.Count > 0;
    }
    public Coroutine CoroutineReservasion;
    public void AddReservasion(TransactionType transactionType)
    {
        if (listReservasion.Contains(transactionType))
            return;

        if (dataAccount_Previous == null)
            SetDataAccount_Previous(SaveLoadManager.Instance.DataAccount);

        Utils.LogColor("Transaction add : " + transactionType);

        listReservasion.Add(transactionType);
    }
    public IEnumerator CoExcuteReservasion()
    {
        Utils.LogColor("Transaction count : " + listReservasion.Count);

        for (int i = 0; i < listReservasion.Count; i++)
        {
            switch (listReservasion[i])
            {
                case TransactionType.DataBasic:
                    UpdateTransactionDataBasic(SaveLoadManager.Instance.DataAccount.dataBasic);
                    break;
                case TransactionType.DataStats:
                    UpdateTransactionDataStats(SaveLoadManager.Instance.DataAccount.dataStats);
                    break;
                case TransactionType.DataParty:
                    UpdateTransactionDataParty(SaveLoadManager.Instance.DataAccount.dataPartylist);
                    break;
                case TransactionType.DataCharacter:
                    UpdateTransactionDataCharacter(SaveLoadManager.Instance.DataAccount.dataCharacterlist);
                    break;
                case TransactionType.DataItems:
                    UpdateTransactionDataItems(SaveLoadManager.Instance.DataAccount.dataItems);
                    break;
                default:
                    break;
            }
        }

        yield return CoSendWriteTransaction(
            () => { dataAccount_Previous = null; },
            () => 
            { 
                SaveLoadManager.Instance.SetDataAcccount(dataAccount_Previous);
                dataAccount_Previous = null;
            });

        listReservasion.Clear();
    }
    public IEnumerator ReservasionTransaction()
    {
        yield return Utils.WaitForSeconds(5.0f);

        StartCoroutine(CoExcuteReservasion());
    }

    private List<TransactionValue> listTransaction = new();
    public bool CheckTransactionAvailable()
    {
        return listTransaction.Count != 0;
    }
    public IEnumerator CoSendWriteTransaction(Action actionSuccess, Action actionFailure)
    {
        if (!CheckTransactionAvailable())
        {
            Utils.LogColor("Transaction list empty.");
            yield break;
        }

        bool processComplete = false;

        Backend.GameData.TransactionWriteV2(listTransaction, (callback) =>
        {
            if (callback.IsSuccess())
            {
                Utils.LogColor("Trasaction 성공");

                actionSuccess?.Invoke();
            }
            else
            {
                Utils.LogColor("Trasaction 실패");
                Utils.LogColor(Utils.String.MergeString("Code : ", callback.GetStatusCode(),
                    " / Message : ", callback.GetMessage()));

                actionFailure?.Invoke();
            }

            processComplete = true;
        });

        yield return new WaitUntil(() => processComplete == true);

        listTransaction.Clear();
    }
    public async Task AsyncSendWriteTransaction(Action actionSuccess, Action actionFailure)
    {
        if (listTransaction.Count <= 0)
        {
            Utils.LogColor("Transaction list empty.");

            return;
        }

        bool processComplete = false;

        Backend.GameData.TransactionWriteV2(listTransaction, (callback) =>
        {
            if (callback.IsSuccess())
            {
                Utils.LogColor("Trasaction 성공");

                actionSuccess?.Invoke();
            }
            else
            {
                Utils.LogColor("Trasaction 실패");
                Utils.LogColor(Utils.String.MergeString("Code : ", callback.GetStatusCode(),
                    " / Message : ", callback.GetMessage()));

                actionFailure?.Invoke();
            }

            processComplete = true;
        });

        while (!processComplete)
            await Task.Yield();

        listTransaction.Clear();
    }

    //DataBasic Insert.
    public void InsertTransactionDataBasic(DataBasic dataBasic)
    {
        Param param = new()
        {
            { "AccountName", dataBasic.accountName },
            { "AccountLevel", dataBasic.accountLevel },
            { "AccountEXP_string", dataBasic.accountEXP_string },
            { "ProfileCharacterID", dataBasic.profileCharacterID },
            { "Gold_string", dataBasic.gold_string },
            { "GrowUpPoint", dataBasic.GrowUpPoint },
            { "Gem", dataBasic.Gem },
            { "Chapter", dataBasic.chapter },
            { "Stage", dataBasic.stage }
        };

        listTransaction.Add(TransactionValue.SetInsert("DataBasic", param));
    }
    //DataStats Insert.
    public void InsertTransactionDataStats(DataStats dataStats)
    {
        Param param = new()
        {
            { "Accuracy", dataStats.accuracy },
            { "AccuracyLevel", dataStats.accuracyLevel },
            { "AccuracyLevel_Growup", dataStats.accuracyLevel_Growup },
            { "Attack_String", dataStats.attack_String },
            { "AttackLevel", dataStats.attackLevel },
            { "AttackLevel_Growup", dataStats.attackLevel_Growup },
            { "CriticalDamage", dataStats.criticalDamage },
            { "CriticalDamageLevel", dataStats.criticalDamageLevel },
            { "CriticalDamageLevel_Growup", dataStats.criticalDamageLevel_Growup },
            { "CriticalRate", dataStats.criticalRate },
            { "CriticalRateLevel", dataStats.criticalRateLevel },
            { "CriticalRateLevel_Growup", dataStats.criticalRateLevel_Growup },
            { "Evade", dataStats.evade },
            { "EvadeLevel", dataStats.evadeLevel },
            { "EvadeLevel_Growup", dataStats.evadeLevel_Growup },
            { "Hp_String", dataStats.hp_String },
            { "HpLevel", dataStats.hpLevel },
            { "HpLevel_Growup", dataStats.hpLevel_Growup },
            { "MaxDamage", dataStats.maxDamage },
            { "MaxDamageLevel", dataStats.maxDamageLevel },
            { "MaxDamageLevel_Growup", dataStats.maxDamageLevel_Growup },
            { "MinDamage", dataStats.minDamage },
            { "MinDamageLevel", dataStats.minDamageLevel },
            { "MinDamageLevel_Growup", dataStats.minDamageLevel_Growup },
        };

        listTransaction.Add(TransactionValue.SetInsert("DataStats", param));
    }
    //DataParty Insert.
    public void InsertTransactionDataParty(List<DataCharacter> dataPartylist)
    {
        Param param = new()
        {
            { "PartyList", dataPartylist }
        };

        listTransaction.Add(TransactionValue.SetInsert("DataParty", param));
    }
    //DataCharacter Insert.
    public void InsertTransactionDataCharacter(List<DataCharacter> dataCharacterlist)
    {
        Param param = new()
        {
            { "CharacterList", dataCharacterlist }
        };

        listTransaction.Add(TransactionValue.SetInsert("DataCharacter", param));
    }
    //DataItems Insert.
    public void InsertTransactionDataItems(DataItems dataItems)
    {
        Param param = new()
        {
            { "dataItems", dataItems }
        };

        listTransaction.Add(TransactionValue.SetInsert("DataItems", param));
    }

    //DataBasic Update.
    public void UpdateTransactionDataBasic(DataBasic dataBasic)
    {
        Param param = new()
        {
            { "AccountName", dataBasic.accountName },
            { "AccountLevel", dataBasic.accountLevel },
            { "AccountEXP_string", dataBasic.accountEXP_string },
            { "ProfileCharacterID", dataBasic.profileCharacterID },
            { "Gold_string", dataBasic.gold_string },
            { "GrowUpPoint", dataBasic.GrowUpPoint },
            { "Gem", dataBasic.Gem },
            { "Chapter", dataBasic.chapter },
            { "Stage", dataBasic.stage }
        };

        //listTransaction.Add(TransactionValue.SetUpdateV2("DataBasic", "inDate", Backend.UserInDate, param));
        listTransaction.Add(TransactionValue.SetUpdate("DataBasic", new Where(), param));
    }
    //DataStats Update.
    public void UpdateTransactionDataStats(DataStats dataStats)
    {
        Param param = new()
        {
            { "Accuracy", dataStats.accuracy },
            { "AccuracyLevel", dataStats.accuracyLevel },
            { "AccuracyLevel_Growup", dataStats.accuracyLevel_Growup },
            { "Attack_String", dataStats.attack_String },
            { "AttackLevel", dataStats.attackLevel },
            { "AttackLevel_Growup", dataStats.attackLevel_Growup },
            { "CriticalDamage", dataStats.criticalDamage },
            { "CriticalDamageLevel", dataStats.criticalDamageLevel },
            { "CriticalDamageLevel_Growup", dataStats.criticalDamageLevel_Growup },
            { "CriticalRate", dataStats.criticalRate },
            { "CriticalRateLevel", dataStats.criticalRateLevel },
            { "CriticalRateLevel_Growup", dataStats.criticalRateLevel_Growup },
            { "Evade", dataStats.evade },
            { "EvadeLevel", dataStats.evadeLevel },
            { "EvadeLevel_Growup", dataStats.evadeLevel_Growup },
            { "Hp_String", dataStats.hp_String },
            { "HpLevel", dataStats.hpLevel },
            { "HpLevel_Growup", dataStats.hpLevel_Growup },
            { "MaxDamage", dataStats.maxDamage },
            { "MaxDamageLevel", dataStats.maxDamageLevel },
            { "MaxDamageLevel_Growup", dataStats.maxDamageLevel_Growup },
            { "MinDamage", dataStats.minDamage },
            { "MinDamageLevel", dataStats.minDamageLevel },
            { "MinDamageLevel_Growup", dataStats.minDamageLevel_Growup },
        };

        //listTransaction.Add(TransactionValue.SetUpdateV2("DataStats", "inDate", Backend.UserInDate, param));
        listTransaction.Add(TransactionValue.SetUpdate("DataStats", new Where(), param));
    }
    //DataParty Update.
    public void UpdateTransactionDataParty(List<DataCharacter> dataPartylist)
    {
        Param param = new()
        {
            { "PartyList", dataPartylist }
        };

        //listTransaction.Add(TransactionValue.SetUpdateV2("DataParty", "inDate", Backend.UserInDate, param));
        listTransaction.Add(TransactionValue.SetUpdate("DataParty", new Where(), param));
    }
    //DataCharacter Update.
    public void UpdateTransactionDataCharacter(List<DataCharacter> dataCharacterlist)
    {
        Param param = new()
        {
            { "CharacterList", dataCharacterlist }
        };

        //listTransaction.Add(TransactionValue.SetUpdateV2("DataCharacter", "inDate", Backend.UserInDate, param));
        listTransaction.Add(TransactionValue.SetUpdate("DataCharacter", new Where(), param));
    }
    //DataItems Update.
    public void UpdateTransactionDataItems(DataItems dataItems)
    {
        Param param = new()
        {
            { "dataItems", dataItems }
        };

        //listTransaction.Add(TransactionValue.SetUpdateV2("DataItems", "inDate", Backend.UserInDate, param));
        listTransaction.Add(TransactionValue.SetUpdate("DataItems", new Where(), param));
    }
    //Read.
    public void ReadTransactionDataAll()
    {
        listTransaction.Add(TransactionValue.SetGetV2("DataBasic", "row_indate", Backend.UserInDate));
        listTransaction.Add(TransactionValue.SetGetV2("DataStats", "row_indate", Backend.UserInDate));
        listTransaction.Add(TransactionValue.SetGetV2("DataParty", "row_indate", Backend.UserInDate));
        listTransaction.Add(TransactionValue.SetGetV2("DataCharacter", "row_indate", Backend.UserInDate));
        listTransaction.Add(TransactionValue.SetGetV2("DataItems", "row_indate", Backend.UserInDate));
    }
    #endregion
}