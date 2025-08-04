using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;

public class AssetBundleManager : SingletonDontDestroyed<AssetBundleManager>
{
    //Ű������ �ε��ϸ� �ߺ��ε��� �������� �޸𸮿� �÷��״��� üũ�� �ȵȴ�. ���̺� ��� �־ �ε��Ұ��� �� ������ ���̴µ�.
    //AssetReference�� ���������� AsyncOperationHandle�� ĳ���ص��� ������ �̹� �ε��Ȱ��� �ƴ��� �𸥴�.
    //AssetReference�� CachedAsset(Protected)�� ������ �� �ִٸ� �ε����� �� �� ����.
    //�Ϸ� �ݹ麸�� ���ϰ� ����ϴ°� ���� ���غ���?
    //AsyncOperationHandle�� IsValid�� Task �������� ��ȿ��.
    //Instantiate ����� ��ü ����ī���Ͱ� �ֱ� ������, Release�ϸ� ���� ��ü�� �� �������.
    //Load ����� ���� ��ü�� ���� ������ �� Release �ؾ� �Ѵ�.
    //�ڵ鷯 ĳ���صδ°� ���� ���� ����.

    private readonly Dictionary<AssetReference, AsyncOperationHandle> dicLoadedAssetBundle = new Dictionary<AssetReference, AsyncOperationHandle>();

    #region async await.
    public async Task<T> LoadAssetBundle_Async<T>(string path) where T : class
    {
        //Addressables.LoadAssetAsync<T>(path).Completed += OnLoadAssetBundleComplete;

        AsyncOperationHandle handle = Addressables.LoadAssetAsync<T>(path);

        return await handle.Task as T;
    }
    public async Task<T> LoadAssetBundle_Async<T>(AssetReference assetReference) where T : class
    {
        //Addressables.LoadAssetAsync<T>(path).Completed += OnLoadAssetBundleComplete;

        AsyncOperationHandle handle = Addressables.LoadAssetAsync<T>(assetReference);

        return await handle.Task as T;
    }

    public async Task<GameObject> InstantiateAssetBundle_Async(AssetReference assetReference)
    {
        //Addressables.LoadAssetAsync<T>(path).Completed += OnLoadAssetBundleComplete;

        if (dicLoadedAssetBundle.ContainsKey(assetReference))
        {
            return dicLoadedAssetBundle[assetReference].Result as GameObject;
        }
        else
        {
            AsyncOperationHandle handle = Addressables.InstantiateAsync(assetReference);

            return await handle.Task as GameObject;
        }        
    }
    #endregion

    #region Load.
    public T LoadAssetBundle<T>(string path) where T : class
    {
        //Addressables.LoadAssetAsync<GameObject>(path).Completed += OnLoadAssetBundleComplete;

        AsyncOperationHandle handle = Addressables.LoadAssetAsync<T>(path);

        return handle.Result as T;
    }
    public T LoadAssetBundle<T>(AssetReference assetReference) where T : class
    {
        //Addressables.LoadAssetAsync<GameObject>(path).Completed += OnLoadAssetBundleComplete;

        //if (assetReference.Asset == null)
        //{
        //    AsyncOperationHandle handle = Addressables.LoadAssetAsync<T>(assetReference);

        //    return handle.Result as T;
        //}
        //else
        //{
        //    return assetReference.Asset as T;
        //}

        if (dicLoadedAssetBundle.ContainsKey(assetReference))
        {
            return dicLoadedAssetBundle[assetReference].Result as T;
        }
        else
        {
            AsyncOperationHandle handle = assetReference.LoadAssetAsync<T>();

            return handle.Result as T;
        }        
    }
    public IEnumerator CoLoadAssetBundle<T>(string path, Action<T> action) where T : class
    {
        AsyncOperationHandle handle = Addressables.LoadAssetAsync<T>(path);

        while (!handle.IsDone)
            yield return null;

        action?.Invoke(handle.Result as T);
    }
    public IEnumerator CoLoadAssetBundle<T>(AssetReference assetReference, Action<T> action) where T : class
    {
        //if (assetReference.Asset == null)
        //{
        //    AsyncOperationHandle handle = Addressables.LoadAssetAsync<T>(assetReference);

        //    while (handle.Result == null)
        //        yield return null;
        //}

        //action?.Invoke(assetReference.Asset as T);

        if (dicLoadedAssetBundle.ContainsKey(assetReference))
        {
            action?.Invoke(dicLoadedAssetBundle[assetReference].Result as T);
        }
        else
        {
            AsyncOperationHandle handle = assetReference.LoadAssetAsync<T>();

            while (!handle.IsDone)
                yield return null;

            dicLoadedAssetBundle.Add(assetReference, handle);

            action?.Invoke(handle.Result as T);
        }        
    }
    #endregion

    #region Instantiate.
    public T InstantiateAssetBundle<T>(string path) where T : class
    {
        AsyncOperationHandle handle = Addressables.InstantiateAsync(path);

        return handle.Result as T;
    }
    public T InstantiateAssetBundle<T>(AssetReference assetReference) where T : class
    {
        //if (assetReference.Asset == null)
        //{
        //    AsyncOperationHandle handle = assetReference.InstantiateAsync();

        //    return handle.Result as T;
        //}
        //else
        //{
        //    return assetReference.Asset as T;
        //}

        AsyncOperationHandle handle = assetReference.InstantiateAsync();

        return handle.Result as T;
    }
    public IEnumerator CoInstantiateAssetBundle<T>(string path, Action<T> action) where T : class
    {
        AsyncOperationHandle handle = Addressables.InstantiateAsync(path);

        while (!handle.IsDone)
            yield return null;

        action?.Invoke(handle.Result as T);
    }
    public IEnumerator CoInstantiateAssetBundle<T>(AssetReference assetReference, Action<T> action) where T : class
    {
        //if (assetReference.Asset == null)
        //{
        //    AsyncOperationHandle handle = assetReference.InstantiateAsync();

        //    while (!handle.IsDone)
        //        yield return null;

        //    action?.Invoke(handle.Result as T);
        //}
        //else
        //{
        //    action?.Invoke(assetReference.Asset as T);
        //}

        AsyncOperationHandle handle = assetReference.InstantiateAsync();

        while (!handle.IsDone)
            yield return null;

        action?.Invoke(handle.Result as T);
    }
    #endregion

    #region Complete Callback
    private void OnLoadAssetBundleComplete(UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle<GameObject> handle)
    {
        //null ó��.
        GameObject result = handle.Result;
    }
    #endregion

    #region UnLoad.
    public void UnLoadInstantiateAssetBundle(GameObject gameObject)
    {
        Addressables.ReleaseInstance(gameObject);
    }

    public void UnLoadAssetBundle<T>(object param) where T : class
    {
        Addressables.Release(param as T);
    }
    public void UnLoadAssetBundle(AssetReference assetReference)
    {
        Addressables.Release(assetReference);
    }
    public void UnLoadAssetBundle(GameObject gameObject)
    {
        Addressables.Release(gameObject);
    }    
    public void UnLoadAssetBundle(string path)
    {
        Addressables.Release(path);
    }
    #endregion

    #region Gameobject.
    //public void LoadAssetBundle(string path, Action<GameObject> action)
    //{
    //    Addressables.LoadAssetAsync<GameObject>(path).Completed += OnLoadAssetComplete;
    //}
    //private void OnLoadAssetComplete(UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle<GameObject> asset)
    //{
    //    //null ó��.
    //    this.asset = asset.Result;
    //}
    #endregion

    #region Test1.
    //public T LoadAssetBundle<T>(string path) where T : class
    //{
    //    Addressables.LoadAssetAsync<T>(path).Completed += OnLoadAssetComplete;
    //}
    //private void OnLoadAssetComplete<T>(UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle<T> asset)
    //{
    //    T temp = asset as T;
    //}
    #endregion

    #region Test2.
    //private GameObject asset;
    //private List<GameObject> listAsset;

    //private async void Test_LoadAsset()
    //{
    //    //����.
    //    Addressables.LoadAssetAsync<GameObject>("AssetAddress").Completed += Test_OnLoadComplete;
    //    asset = await Addressables.LoadAssetAsync<GameObject>("AssetAddress").Task;
    //    asset = Addressables.InstantiateAsync("AssetAddress").Result;

    //    //���� �� �ܾ����.
    //    Addressables.LoadAssetAsync<List<GameObject>>("AssetAddress").Completed += Test_OnLoadComplete;
    //    listAsset = await Addressables.LoadAssetAsync<List<GameObject>>("AssetAddress").Task;

    //    //���� ����.
    //    Addressables.LoadAssetAsync<GameObject>("AssetAddress[Address]").Completed += Test_OnLoadComplete;
    //    asset = await Addressables.LoadAssetAsync<GameObject>("AssetAddress[Address]").Task;
    //}
    //private void Test_ReleaseAsset()
    //{
    //    Addressables.Release("AssetAddress");
    //    Addressables.Release(asset);

    //    Addressables.ReleaseInstance(asset);
    //}

    //private void Test_OnLoadComplete(UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle<GameObject> asset)
    //{
    //    //null ó��.
    //    this.asset = asset.Result;
    //}
    //private void Test_OnLoadComplete(UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle<List<GameObject>> listAsset)
    //{
    //    //null ó��.
    //    this.listAsset = listAsset.Result;
    //}
    #endregion

    #region Test3.
    //private SceneInstance sceneInstance;
    //private void Test_LoadScene()
    //{
    //    Addressables.LoadSceneAsync("SceneAddress", UnityEngine.SceneManagement.LoadSceneMode.Single).Completed += Test_OnLoadComplete;
    //}
    //private void Test_OnLoadComplete(AsyncOperationHandle<SceneInstance> sceneInstance)
    //{
    //    this.sceneInstance = sceneInstance.Result;
    //}
    //private void Test_ReleaseScene()
    //{
    //    Addressables.UnloadSceneAsync(sceneInstance).Completed += Test_OnUnloadComplete;
    //}
    //private void Test_OnUnloadComplete(AsyncOperationHandle<SceneInstance> sceneInstance)
    //{
    //    if (sceneInstance.Status == AsyncOperationStatus.Succeeded)
    //        sceneInstance = new AsyncOperationHandle<SceneInstance>();
    //}
    #endregion
}