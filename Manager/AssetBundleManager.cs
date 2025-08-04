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
    //키값으로 로딩하면 중복로딩은 안하지만 메모리에 올려뒀는지 체크가 안된다. 테이블에 경로 넣어서 로딩할경우는 좀 안좋아 보이는데.
    //AssetReference도 마찬가지로 AsyncOperationHandle를 캐싱해두지 않으면 이미 로딩된건지 아닌지 모른다.
    //AssetReference의 CachedAsset(Protected)을 접근할 수 있다면 로딩여부 알 수 있음.
    //완료 콜백보단 리턴값 사용하는게 조금 편해보임?
    //AsyncOperationHandle의 IsValid는 Task 돌릴때만 유효함.
    //Instantiate 기능은 자체 참조카운터가 있기 때문에, Release하면 복제 객체도 다 사라진다.
    //Load 기능은 참조 개체를 먼저 해제한 뒤 Release 해야 한다.
    //핸들러 캐싱해두는게 가장 좋아 보임.

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
        //null 처리.
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
    //    //null 처리.
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
    //    //단일.
    //    Addressables.LoadAssetAsync<GameObject>("AssetAddress").Completed += Test_OnLoadComplete;
    //    asset = await Addressables.LoadAssetAsync<GameObject>("AssetAddress").Task;
    //    asset = Addressables.InstantiateAsync("AssetAddress").Result;

    //    //하위 싹 긁어오기.
    //    Addressables.LoadAssetAsync<List<GameObject>>("AssetAddress").Completed += Test_OnLoadComplete;
    //    listAsset = await Addressables.LoadAssetAsync<List<GameObject>>("AssetAddress").Task;

    //    //하위 단일.
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
    //    //null 처리.
    //    this.asset = asset.Result;
    //}
    //private void Test_OnLoadComplete(UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle<List<GameObject>> listAsset)
    //{
    //    //null 처리.
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