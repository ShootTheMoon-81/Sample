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

        //MainThread ������ ������ ������.
        Backend.InitializeAsync(true, callback =>
        {
            if (callback.IsSuccess())
            {
                Utils.LogColor("�ڳ� �ʱ�ȭ ����");

                Utils.LogColor("���� �ؽ�Ű : " + Backend.Utils.GetGoogleHash());
            }
            else
            {
                Utils.LogColor("�ڳ� �ʱ�ȭ ����");
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
                Utils.LogColor("�ڳ� �ʱ�ȭ ����");

                Utils.LogColor("���� �ؽ�Ű : " + Backend.Utils.GetGoogleHash());

                actionSuccess?.Invoke();
            }
            else
            {
                Utils.LogColor("�ڳ� �ʱ�ȭ ����");
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
                Utils.LogColor("�ڳ� �ʱ�ȭ ����");

                Utils.LogColor("���� �ؽ�Ű : " + Backend.Utils.GetGoogleHash());

                actionSuccess?.Invoke();
            }
            else
            {
                Utils.LogColor("�ڳ� �ʱ�ȭ ����");
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
                        Utils.LogColor("��ū ��߱� ����");

                        processSuccess = true;
                    }
                    else
                    {
                        Utils.LogColor("��ū ��߱� ����");

                        processSuccess = false;

                        if (callback.GetMessage().Contains("bad refreshToken"))
                        {
                            Debug.LogError("�ߺ� �α��� �߻�");

                            Utils.SetMessagePopup("�ߺ� �α����� �߻��߽��ϴ�. ������ �����Ͻðڽ��ϱ�?",
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
                            Debug.LogWarning("15�� �ڿ� ��ū ��߱� �ٽ� �õ�");
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
    //���ÿ� ����� ��ū���� �ڵ� �α���.
    public IEnumerator CoLoginAuto(Action actionSuccess, Action actionFailure, IEnumerator process)
    {
        bool processComplete = false;

        bool processSuccess = false;
        Backend.BMember.LoginWithTheBackendToken((callback) =>
        {
            if (callback.IsSuccess())
            {
                processSuccess = true;
                
                Utils.LogColor("�ڵ� �α��� ����");

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
                Utils.LogColor("�ڵ� �α��� ����");
                Utils.LogColor(Utils.String.MergeString("Code : ", callback.GetStatusCode(),
                    " / Message : ", callback.GetMessage()));

                if (callback.GetMessage().Contains("undefined refresh_token") ||
                    callback.GetMessage().Contains("refresh_token"))
                {
                    Utils.LogColor("���ÿ� ����� ��ū�� ����");

                    UIManager.Instance.GetPopup<PopupMessage>(UIManager.PopupType.PopupMessage).SetPopup(
                        "����� �������� ����",
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
                    Utils.LogColor("�ٸ� ��⿡�� �α���");

                    UIManager.Instance.GetPopup<PopupMessage>(UIManager.PopupType.PopupMessage).SetPopup(
                        "�ٸ� ��⿡�� �α��� �Ǿ� �ִ� �����Դϴ�.",
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
                    Utils.LogColor("���� DB�� ������ ����");

                    UIManager.Instance.GetPopup<PopupMessage>(UIManager.PopupType.PopupMessage).SetPopup(
                        "������ �������� ����",
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

                Utils.LogColor("�ڵ� �α��� ����");

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
                Utils.LogColor("�ڵ� �α��� ����");
                Utils.LogColor(Utils.String.MergeString("Code : ", callback.GetStatusCode(),
                    " / Message : ", callback.GetMessage()));

                if (callback.GetMessage().Contains("undefined refresh_token") ||
                    callback.GetMessage().Contains("refresh_token"))
                {
                    Utils.LogColor("���ÿ� ����� ��ū�� ����");

                    UIManager.Instance.GetPopup<PopupMessage>(UIManager.PopupType.PopupMessage).SetPopup(
                        "����� �������� ����",
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
                    Utils.LogColor("�ٸ� ��⿡�� �α���");

                    UIManager.Instance.GetPopup<PopupMessage>(UIManager.PopupType.PopupMessage).SetPopup(
                        "�ٸ� ��⿡�� �α��� �Ǿ� �ִ� �����Դϴ�.",
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
                    Utils.LogColor("���� DB�� ������ ����");

                    UIManager.Instance.GetPopup<PopupMessage>(UIManager.PopupType.PopupMessage).SetPopup(
                        "������ �������� ����",
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

    //�α׾ƿ�. ���� ����� ��ū �� ����.
    public IEnumerator CoLogout(Action actionSuccess, Action actionFailure)
    {
        bool processComplete = false;

        Backend.BMember.Logout((callback) =>
        {
            if (callback.IsSuccess())
            {
                Utils.LogColor("�α׾ƿ� ����");
                UIManager.Instance.GetPopup<PopupLogin>(UIManager.PopupType.PopupLogin).SetPopup(
                    () =>
                    {
                        UIManager.Instance.HidePopup(UIManager.PopupType.PopupLogin);
                    });

                actionSuccess?.Invoke();
            }
            else
            {
                Utils.LogColor("�α׾ƿ� ����");
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
                Utils.LogColor("�α׾ƿ� ����");
                UIManager.Instance.GetPopup<PopupLogin>(UIManager.PopupType.PopupLogin).SetPopup(
                    () =>
                    {
                        UIManager.Instance.HidePopup(UIManager.PopupType.PopupLogin);
                    });

                actionSuccess?.Invoke();
            }
            else
            {
                Utils.LogColor("�α׾ƿ� ����");
                Utils.LogColor(Utils.String.MergeString("Code : ", callback.GetStatusCode(),
                " / Message : ", callback.GetMessage()));

                actionFailure?.Invoke();
            }

            processComplete = true;
        });

        while (!processComplete)
            await Task.Yield();
    }

    //ȸ������.
    public IEnumerator CoSignUp(string userID, string passWord, Action actionSuccess, Action actionFailure)
    {
        bool processComplete = false;

        Backend.BMember.CustomSignUp(userID, passWord, callback => {
            if (callback.IsSuccess())
            {
                Utils.LogColor("ȸ������ ����");

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
                Utils.LogColor("ȸ������ ����");
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
                Utils.LogColor("ȸ������ ����");

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
                Utils.LogColor("ȸ������ ����");
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

    //Ŀ���Ұ��� �α���.
    public IEnumerator CoLogin(string userID, string passWord, Action actionSuccess, Action actionFailure)
    {
        bool processComplete = false;

        Backend.BMember.CustomLogin(userID, passWord, callback => {
            if (callback.IsSuccess())
            {
                Utils.LogColor("�α��� ����");

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
                Utils.LogColor("�α��� ����.");
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
                Utils.LogColor("�α��� ����");

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
                Utils.LogColor("�α��� ����.");
                Utils.LogColor(Utils.String.MergeString("Code : ", callback.GetStatusCode(),
                    " / Message : ", callback.GetMessage()));

                actionFailure?.Invoke();
            }

            processComplete = true;
        });

        while (!processComplete)
            await Task.Yield();
    }

    //�г���.
    public IEnumerator CoSetNickName(string nickName, Action actionSuccess, Action actionFailure)
    {
        bool processComplete = false;

        Backend.BMember.CreateNickname(nickName, (callback) =>
        {
            if (callback.IsSuccess())
            {
                Utils.LogColor("�г��� ���� ����");

                actionSuccess?.Invoke();
            }
            else
            {
                Utils.LogColor("�г��� ���� ����");
                Utils.LogColor(Utils.String.MergeString("Code : ", callback.GetStatusCode(),
                    " / Message : ", callback.GetMessage()));

                if (callback.GetMessage().Contains("Duplicated nickname"))
                {
                    Utils.SetMessagePopup("������ �г����̴�");

                    actionFailure?.Invoke();

                    return;
                }

                //switch (callback.GetStatusCode())
                //{
                //    case "400":
                //        if (callback.GetMessage() == "UndefinedParameterException")
                //        {
                //            Utils.LogColor("�� �г���");
                //        }
                //        else if (callback.GetMessage() == "BadParameterException")
                //        {
                //            Utils.LogColor("20�� �̻�");
                //            Utils.LogColor("�г��� �յ� ����");
                //        }
                //        break;
                //    case "409":
                //        Utils.LogColor("�ߺ� �г���");
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
                Utils.LogColor("�г��� ���� ����");

                actionSuccess?.Invoke();
            }
            else
            {
                Utils.LogColor("�г��� ���� ����");
                Utils.LogColor(Utils.String.MergeString("Code : ", callback.GetStatusCode(),
                    " / Message : ", callback.GetMessage()));

                if (callback.GetMessage().Contains("Duplicated nickname"))
                {
                    Utils.SetMessagePopup("������ �г����̴�");

                    actionFailure?.Invoke();

                    return;
                }

                //switch (callback.GetStatusCode())
                //{
                //    case "400":
                //        if (callback.GetMessage() == "UndefinedParameterException")
                //        {
                //            Utils.LogColor("�� �г���");
                //        }
                //        else if (callback.GetMessage() == "BadParameterException")
                //        {
                //            Utils.LogColor("20�� �̻�");
                //            Utils.LogColor("�г��� �յ� ����");
                //        }
                //        break;
                //    case "409":
                //        Utils.LogColor("�ߺ� �г���");
                //        break;
                //    default:
                //        break;
                //}
            }
        });

        while (!processComplete)
            await Task.Yield();
    }

    //�̸��� ���.
    private void SetEmail()
    {
        Backend.BMember.UpdateCustomEmail("help@thebackend.io");
    }

    //���ÿ� ����� �������� Ȯ��. �α����� �ؾ� �� �� ����;;;
    public void GetLocalAccountInfo()
    {
        string inDate = Backend.UserInDate;
        string nickName = Backend.UserNickName;

        Utils.LogColor(inDate);
        Utils.LogColor(nickName);
    }

    //������ ����� �������� Ȯ��. �α����� �ؾ� �� �� ����;;;
    public IEnumerator CoGetServerAccountInfo(Action actionSuccess, Action actionFailure)
    {
        bool processComplete = false;

        Backend.BMember.GetUserInfo((callback) =>
        {
            if (callback.IsSuccess())
            {
                Utils.LogColor("���� �ҷ����� ����");

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
                Utils.LogColor("���� �ҷ����� ����.");
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
                Utils.LogColor("���� �ҷ����� ����");

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
                Utils.LogColor("���� �ҷ����� ����.");
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
    //        Utils.LogColor("���� ����");
    //    }
    //    else
    //    {
    //        Utils.LogColor("���� ����");
    //        Utils.LogColor(Utils.String.MergeString("Code : ", backendReturnObject.GetStatusCode(),
    //            " / Message : ", backendReturnObject.GetMessage()));
    //        //switch (backendReturnObject.GetStatusCode())
    //        //{
    //        //    case "412":
    //        //        Utils.LogColor("��Ȱ��ȭ �� tableName");
    //        //        break;
    //        //    case "413":
    //        //        Utils.LogColor("�������� ũ�Ⱑ 400KB�� ����");
    //        //        break;
    //        //    case "400":
    //        //        Utils.LogColor("���� �������� �÷������� 290���� �Ѿ");
    //        //        break;
    //        //    case "404":
    //        //        Utils.LogColor("�������� �ʴ� table");
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
    //        Utils.LogColor("�ҷ����� ����");

    //        JsonData jsonData = backendReturnObject.FlattenRows();

    //        DataAccount dataAccount = JsonConvert.DeserializeObject<DataAccount>(jsonData[0]["AccountData"].ToJson());

    //        Utils.LogColor(JsonConvert.SerializeObject(dataAccount));
    //    }
    //    else
    //    {
    //        Utils.LogColor("�ҷ����� ����");
    //        Utils.LogColor(Utils.String.MergeString("Code : ", backendReturnObject.GetStatusCode(),
    //            " / Message : ", backendReturnObject.GetMessage()));
    //        //switch (backendReturnObject.GetStatusCode())
    //        //{
    //        //    case "412":
    //        //        Utils.LogColor("��Ȱ��ȭ �� tableName");
    //        //        break;
    //        //    case "404":
    //        //        Utils.LogColor("�������� �ʴ� table");
    //        //        break;
    //        //    default:
    //        //        break;
    //        //}
    //    }
    //}

    //�ű����� DB ����.
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

    //�������� ���������� �Ľ�.
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
    //�⺻���� DB ����.
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
                Utils.LogColor("DataBasic DB ���� ����");

                actionSuccess?.Invoke();
            }
            else
            {
                Utils.LogColor("DataBasic DB ���� ����");
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
                Utils.LogColor("DataBasic DB ���� ����");

                actionSuccess?.Invoke();
            }
            else
            {
                Utils.LogColor("DataBasic DB ���� ����");
                Utils.LogColor(Utils.String.MergeString("Code : ", callback.GetStatusCode(),
                    " / Message : ", callback.GetMessage()));

                actionFailure?.Invoke();
            }

            processComplete = true;
        });

        while (!processComplete)
            await Task.Yield();
    }
    //�⺻���� DB ��ü ������Ʈ.
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

        //�Ŵ��� �Ű����� ���� �߸���.
        Backend.GameData.Update("DataBasic", new Where(), param, (callback) =>
        {
            if (callback.IsSuccess())
            {
                Utils.LogColor("DataBasic DB ���� ����");

                actionSuccess?.Invoke();
            }
            else
            {
                Utils.LogColor("DataBasic DB ���� ����");
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

        //�Ŵ��� �Ű����� ���� �߸���.
        Backend.GameData.Update("DataBasic", new Where(), param, (callback) =>
        {
            if (callback.IsSuccess())
            {
                Utils.LogColor("DataBasic DB ���� ����");

                actionSuccess?.Invoke();
            }
            else
            {
                Utils.LogColor("DataBasic DB ���� ����");
                Utils.LogColor(Utils.String.MergeString("Code : ", callback.GetStatusCode(),
                    " / Message : ", callback.GetMessage()));

                actionFailure?.Invoke();
            }

            processComplete = true;
        });

        while (!processComplete)
            await Task.Yield();
    }
    //�⺻���� DB �ҷ�����.
    public IEnumerator CoGetDataBasic(Action<DataBasic> actionSuccess, Action actionFailure)
    {
        bool processComplete = false;

        Backend.GameData.GetMyData("DataBasic", new Where(), callback =>
        {
            if (callback.IsSuccess())
            {
                if (callback.GetReturnValuetoJSON().Count <= 0)
                {
                    Utils.LogColor("DataBasic DB ������ ����");
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
                Utils.LogColor("DataBasic DB �ҷ����� ����");
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
                    Utils.LogColor("DataBasic DB ������ ����");
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
                Utils.LogColor("DataBasic DB �ҷ����� ����");
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
    //���� DB ����.
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
                Utils.LogColor("DataStats DB ���� ����");

                actionSuccess?.Invoke();
            }
            else
            {
                Utils.LogColor("DataStats DB ���� ����");
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
                Utils.LogColor("DataStats DB ���� ����");

                actionSuccess?.Invoke();
            }
            else
            {
                Utils.LogColor("DataStats DB ���� ����");
                Utils.LogColor(Utils.String.MergeString("Code : ", callback.GetStatusCode(),
                    " / Message : ", callback.GetMessage()));

                actionFailure?.Invoke();
            }

            processComplete = true;
        });

        while (!processComplete)
            await Task.Yield();
    }
    //���� DB ��ü ������Ʈ.
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

        //�Ŵ��� �Ű����� ���� �߸���.
        Backend.GameData.Update("DataStats", new Where(), param, (callback) =>
        {
            if (callback.IsSuccess())
            {
                Utils.LogColor("DataStats DB ���� ����");

                actionSuccess?.Invoke();
            }
            else
            {
                Utils.LogColor("DataStats DB ���� ����");
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

        //�Ŵ��� �Ű����� ���� �߸���.
        Backend.GameData.Update("DataStats", new Where(), param, (callback) =>
        {
            if (callback.IsSuccess())
            {
                Utils.LogColor("DataStats DB ���� ����");

                actionSuccess?.Invoke();
            }
            else
            {
                Utils.LogColor("DataStats DB ���� ����");
                Utils.LogColor(Utils.String.MergeString("Code : ", callback.GetStatusCode(),
                    " / Message : ", callback.GetMessage()));

                actionFailure?.Invoke();
            }

            processComplete = true;
        });

        while (!processComplete)
            await Task.Yield();
    }
    //���� DB �ҷ�����.
    public IEnumerator CoGetDataStats(Action<DataStats> actionSuccess, Action actionFailure)
    {
        bool processComplete = false;

        Backend.GameData.GetMyData("DataStats", new Where(), callback =>
        {
            if (callback.IsSuccess())
            {
                if (callback.GetReturnValuetoJSON().Count <= 0)
                {
                    Utils.LogColor("DataStats DB ������ ����");
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
                Utils.LogColor("DataStats DB �ҷ����� ����");
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
                    Utils.LogColor("DataStats DB ������ ����");
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
                Utils.LogColor("DataStats DB �ҷ����� ����");
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
    //��Ƽ DB ����.
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
                Utils.LogColor("DataParty DB ���� ����");

                actionSuccess?.Invoke();
            }
            else
            {
                Utils.LogColor("DataParty DB ���� ����");
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
                Utils.LogColor("DataParty DB ���� ����");

                actionSuccess?.Invoke();
            }
            else
            {
                Utils.LogColor("DataParty DB ���� ����");
                Utils.LogColor(Utils.String.MergeString("Code : ", callback.GetStatusCode(),
                    " / Message : ", callback.GetMessage()));

                actionFailure?.Invoke();
            }

            processComplete = true;
        });

        while (!processComplete)
            await Task.Yield();
    }
    //��Ƽ DB ��ü ������Ʈ.
    public IEnumerator CoUpdateDataParty(List<DataCharacter> dataPartylist, Action actionSuccess, Action actionFailure)
    {
        bool processComplete = false;

        Param param = new()
        {
            { "PartyList", dataPartylist }
        };

        //�Ŵ��� �Ű����� ���� �߸���.
        Backend.GameData.Update("DataParty", new Where(), param, (callback) =>
        {
            if (callback.IsSuccess())
            {
                Utils.LogColor("DataParty DB ���� ����");

                actionSuccess?.Invoke();
            }
            else
            {
                Utils.LogColor("DataParty DB ���� ����");
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

        //�Ŵ��� �Ű����� ���� �߸���.
        Backend.GameData.Update("DataParty", new Where(), param, (callback) =>
        {
            if (callback.IsSuccess())
            {
                Utils.LogColor("DataParty DB ���� ����");

                actionSuccess?.Invoke();
            }
            else
            {
                Utils.LogColor("DataParty DB ���� ����");
                Utils.LogColor(Utils.String.MergeString("Code : ", callback.GetStatusCode(),
                    " / Message : ", callback.GetMessage()));

                actionFailure?.Invoke();
            }

            processComplete = true;
        });

        while (!processComplete)
            await Task.Yield();
    }
    //��Ƽ DB �ҷ�����.
    public IEnumerator CoGetDataParty(Action<List<DataCharacter>> actionSuccess, Action actionFailure)
    {
        bool processComplete = false;

        Backend.GameData.GetMyData("DataParty", new Where(), callback =>
        {
            if (callback.IsSuccess())
            {
                if (callback.GetReturnValuetoJSON()["rows"].Count <= 0)
                {
                    Utils.LogColor("DataParty DB ������ ����");
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
                Utils.LogColor("DataParty DB �ҷ����� ����");
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
                    Utils.LogColor("DataParty DB ������ ����");
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
                Utils.LogColor("DataParty DB �ҷ����� ����");
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
    //ĳ���͸���Ʈ DB ����.
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
                Utils.LogColor("DataCharacter DB ���� ����");

                actionSuccess?.Invoke();
            }
            else
            {
                Utils.LogColor("DataCharacter DB ���� ����");
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
                Utils.LogColor("DataCharacter DB ���� ����");

                actionSuccess?.Invoke();
            }
            else
            {
                Utils.LogColor("DataCharacter DB ���� ����");
                Utils.LogColor(Utils.String.MergeString("Code : ", callback.GetStatusCode(),
                    " / Message : ", callback.GetMessage()));

                actionFailure?.Invoke();
            }

            processComplete = true;
        });

        while (!processComplete)
            await Task.Yield();
    }
    //ĳ���͸���Ʈ DB ��ü ������Ʈ.
    public IEnumerator CoUpdateDataCharater(List<DataCharacter> dataCharacterlist, Action actionSuccess, Action actionFailure)
    {
        bool processComplete = false;

        Param param = new()
        {
            { "CharacterList", dataCharacterlist }
        };

        //�Ŵ��� �Ű����� ���� �߸���.
        Backend.GameData.Update("DataCharacter", new Where(), param, (callback) =>
        {
            if (callback.IsSuccess())
            {
                Utils.LogColor("DataCharacter DB ���� ����");

                actionSuccess?.Invoke();
            }
            else
            {
                Utils.LogColor("DataCharacter DB ���� ����");
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

        //�Ŵ��� �Ű����� ���� �߸���.
        Backend.GameData.Update("DataCharacter", new Where(), param, (callback) =>
        {
            if (callback.IsSuccess())
            {
                Utils.LogColor("DataCharacter DB ���� ����");

                actionSuccess?.Invoke();
            }
            else
            {
                Utils.LogColor("DataCharacter DB ���� ����");
                Utils.LogColor(Utils.String.MergeString("Code : ", callback.GetStatusCode(),
                    " / Message : ", callback.GetMessage()));

                actionFailure?.Invoke();
            }

            processComplete = true;
        });

        while (!processComplete)
            await Task.Yield();
    }
    //ĳ���͸���Ʈ DB �ҷ�����.
    public IEnumerator CoGetDataCharacter(Action<List<DataCharacter>> actionSuccess, Action actionFailure)
    {
        bool processComplete = false;

        Backend.GameData.GetMyData("DataCharacter", new Where(), callback =>
        {
            if (callback.IsSuccess())
            {
                if (callback.GetReturnValuetoJSON()["rows"].Count <= 0)
                {
                    Utils.LogColor("DataCharacter DB ������ ����");
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
                Utils.LogColor("DataCharacter DB �ҷ����� ����");
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
                    Utils.LogColor("DataCharacter DB ������ ����");
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
                Utils.LogColor("DataCharacter DB �ҷ����� ����");
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
    //�����۸���Ʈ DB ����.
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
                Utils.LogColor("DataItems DB ���� ����");

                actionSuccess?.Invoke();
            }
            else
            {
                Utils.LogColor("DataItems DB ���� ����");
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
                Utils.LogColor("DataItems DB ���� ����");

                actionSuccess?.Invoke();
            }
            else
            {
                Utils.LogColor("DataItems DB ���� ����");
                Utils.LogColor(Utils.String.MergeString("Code : ", callback.GetStatusCode(),
                    " / Message : ", callback.GetMessage()));

                actionFailure?.Invoke();
            }

            processComplete = true;
        });

        while (!processComplete)
            await Task.Yield();
    }
    //�����۸���Ʈ DB ��ü ������Ʈ.
    public IEnumerator CoUpdateDataItems(DataItems dataItems, Action actionSuccess, Action actionFailure)
    {
        bool processComplete = false;

        Param param = new()
        {
            { "dataItems", dataItems }
        };

        //�Ŵ��� �Ű����� ���� �߸���.
        Backend.GameData.Update("DataItems", new Where(), param, (callback) =>
        {
            if (callback.IsSuccess())
            {
                Utils.LogColor("DataItems DB ���� ����");

                actionSuccess?.Invoke();
            }
            else
            {
                Utils.LogColor("DataItems DB ���� ����");
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

        //�Ŵ��� �Ű����� ���� �߸���.
        Backend.GameData.Update("DataItems", new Where(), param, (callback) =>
        {
            if (callback.IsSuccess())
            {
                Utils.LogColor("DataItems DB ���� ����");

                actionSuccess?.Invoke();
            }
            else
            {
                Utils.LogColor("DataItems DB ���� ����");
                Utils.LogColor(Utils.String.MergeString("Code : ", callback.GetStatusCode(),
                    " / Message : ", callback.GetMessage()));

                actionFailure?.Invoke();
            }

            processComplete = true;
        });

        while (!processComplete)
            await Task.Yield();
    }
    //�����۸���Ʈ DB �ҷ�����.
    public IEnumerator CoGetDataItems(Action<DataItems> actionSuccess, Action actionFailure)
    {
        bool processComplete = false;

        Backend.GameData.GetMyData("DataItems", new Where(), callback =>
        {
            if (callback.IsSuccess())
            {
                if (callback.GetReturnValuetoJSON().Count <= 0)
                {
                    Utils.LogColor("DataItems DB ������ ����");
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
                Utils.LogColor("DataItems DB �ҷ����� ����");
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
                    Utils.LogColor("DataItems DB ������ ����");
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
                Utils.LogColor("DataItems DB �ҷ����� ����");
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

        //�Ŵ��� �Ű����� ���� �߸���.
        Backend.Probability.GetProbabilitys("5128", excuteCount, (callback) =>
        {
            if (callback.IsSuccess())
            {
                Utils.LogColor("CharacterPiece gacha ����");

                JsonData json = callback.GetFlattenJSON()["elements"];

                for (int i = 0; i < json.Count; i++)
                {
                    int.TryParse(json[i]["fragmentRarity"].ToString(), out int fragment);
                    if (fragment == 0)
                    {
                        //�α�.
                    }
                    else
                    {
                        actionSuccess?.Invoke(fragment);
                    }
                }
            }
            else
            {
                Utils.LogColor("CharacterPiece gacha ����");
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

        //�Ŵ��� �Ű����� ���� �߸���.
        Backend.Probability.GetProbabilitys("5128", excuteCount, (callback) =>
        {
            if (callback.IsSuccess())
            {
                Utils.LogColor("CharacterPiece gacha ����");

                JsonData json = callback.GetFlattenJSON()["elements"];

                for (int i = 0; i < json.Count; i++)
                {
                    int.TryParse(json[i]["fragmentRarity"].ToString(), out int fragment);
                    if (fragment == 0)
                    {
                        //�α�.
                    }
                    else
                    {
                        actionSuccess?.Invoke(fragment);
                    }
                }                
            }
            else
            {
                Utils.LogColor("CharacterPiece gacha ����");
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
                Utils.LogColor("Trasaction ����");

                actionSuccess?.Invoke();
            }
            else
            {
                Utils.LogColor("Trasaction ����");
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
                Utils.LogColor("Trasaction ����");

                actionSuccess?.Invoke();
            }
            else
            {
                Utils.LogColor("Trasaction ����");
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