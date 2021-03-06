using System;
using System.Collections;
using UnityEngine;
using UtyRx;

namespace Assets.Scripts.Environment.Reactive
{
#if !(UNITY_METRO || UNITY_WP8) && (UNITY_4_4 || UNITY_4_3 || UNITY_4_2 || UNITY_4_1 || UNITY_4_0_1 || UNITY_4_0 || UNITY_3_5 || UNITY_3_4 || UNITY_3_3 || UNITY_3_2 || UNITY_3_1 || UNITY_3_0_0 || UNITY_3_0 || UNITY_2_6_1 || UNITY_2_6)
    // Fallback for Unity versions below 4.5
    using Hash = System.Collections.Hashtable;
    using HashEntry = System.Collections.DictionaryEntry;    
#else
    // Unity 4.5 release notes: 
    // WWW: deprecated 'WWW(string url, byte[] postData, Hashtable headers)', 
    // use 'public WWW(string url, byte[] postData, Dictionary<string, string> headers)' instead.
    using Hash = System.Collections.Generic.Dictionary<string, string>;
    using HashEntry = System.Collections.Generic.KeyValuePair<string, string>;
#endif

    public static class ObservableWWW
    {
        public static UtyRx.IObservable<string> Get(string url, Hash headers = null, UtyRx.IProgress<float> progress = null)
        {
            return ObservableUnity
                .FromCoroutine<string>((observer, cancellation) => 
                    FetchText(new WWW(url, null, (headers ?? new Hash())), observer, progress, cancellation))
                .SubscribeOn(Scheduler.MainThread)
                .Take(1); 
        }

        public static UtyRx.IObservable<byte[]> GetAndGetBytes(string url, Hash headers = null, UtyRx.IProgress<float> progress = null)
        {
            return ObservableUnity
                .FromCoroutine<byte[]>((observer, cancellation) => 
                    FetchBytes(new WWW(url, null, (headers ?? new Hash())), observer, progress, cancellation))
                .SubscribeOn(Scheduler.MainThread)
                .Take(1);
        }
        public static UtyRx.IObservable<WWW> GetWWW(string url, Hash headers = null, UtyRx.IProgress<float> progress = null)
        {
            return ObservableUnity.FromCoroutine<WWW>((observer, cancellation) => Fetch(new WWW(url, null, (headers ?? new Hash())), observer, progress, cancellation));
        }

        public static UtyRx.IObservable<string> Post(string url, byte[] postData, UtyRx.IProgress<float> progress = null)
        {
            return ObservableUnity.FromCoroutine<string>((observer, cancellation) => FetchText(new WWW(url, postData), observer, progress, cancellation));
        }

        public static UtyRx.IObservable<string> Post(string url, byte[] postData, Hash headers, UtyRx.IProgress<float> progress = null)
        {
            return ObservableUnity.FromCoroutine<string>((observer, cancellation) => FetchText(new WWW(url, postData, headers), observer, progress, cancellation));
        }

        public static UtyRx.IObservable<string> Post(string url, WWWForm content, UtyRx.IProgress<float> progress = null)
        {
            return ObservableUnity.FromCoroutine<string>((observer, cancellation) => FetchText(new WWW(url, content), observer, progress, cancellation));
        }

        public static UtyRx.IObservable<string> Post(string url, WWWForm content, Hash headers, UtyRx.IProgress<float> progress = null)
        {
            var contentHeaders = content.headers;
            return ObservableUnity.FromCoroutine<string>((observer, cancellation) => FetchText(new WWW(url, content.data, MergeHash(contentHeaders, headers)), observer, progress, cancellation));
        }

        public static UtyRx.IObservable<byte[]> PostAndGetBytes(string url, byte[] postData, UtyRx.IProgress<float> progress = null)
        {
            return ObservableUnity.FromCoroutine<byte[]>((observer, cancellation) => FetchBytes(new WWW(url, postData), observer, progress, cancellation));
        }

        public static UtyRx.IObservable<byte[]> PostAndGetBytes(string url, byte[] postData, Hash headers, UtyRx.IProgress<float> progress = null)
        {
            return ObservableUnity.FromCoroutine<byte[]>((observer, cancellation) => FetchBytes(new WWW(url, postData, headers), observer, progress, cancellation));
        }

        public static UtyRx.IObservable<byte[]> PostAndGetBytes(string url, WWWForm content, UtyRx.IProgress<float> progress = null)
        {
            return ObservableUnity.FromCoroutine<byte[]>((observer, cancellation) => FetchBytes(new WWW(url, content), observer, progress, cancellation));
        }

        public static UtyRx.IObservable<byte[]> PostAndGetBytes(string url, WWWForm content, Hash headers, UtyRx.IProgress<float> progress = null)
        {
            var contentHeaders = content.headers;
            return ObservableUnity.FromCoroutine<byte[]>((observer, cancellation) => FetchBytes(new WWW(url, content.data, MergeHash(contentHeaders, headers)), observer, progress, cancellation));
        }

        public static UtyRx.IObservable<WWW> PostWWW(string url, byte[] postData, UtyRx.IProgress<float> progress = null)
        {
            return ObservableUnity.FromCoroutine<WWW>((observer, cancellation) => Fetch(new WWW(url, postData), observer, progress, cancellation));
        }

        public static UtyRx.IObservable<WWW> PostWWW(string url, byte[] postData, Hash headers, UtyRx.IProgress<float> progress = null)
        {
            return ObservableUnity.FromCoroutine<WWW>((observer, cancellation) => Fetch(new WWW(url, postData, headers), observer, progress, cancellation));
        }

        public static UtyRx.IObservable<WWW> PostWWW(string url, WWWForm content, UtyRx.IProgress<float> progress = null)
        {
            return ObservableUnity.FromCoroutine<WWW>((observer, cancellation) => Fetch(new WWW(url, content), observer, progress, cancellation));
        }

        public static UtyRx.IObservable<WWW> PostWWW(string url, WWWForm content, Hash headers, UtyRx.IProgress<float> progress = null)
        {
            var contentHeaders = content.headers;
            return ObservableUnity.FromCoroutine<WWW>((observer, cancellation) => Fetch(new WWW(url, content.data, MergeHash(contentHeaders, headers)), observer, progress, cancellation));
        }

        public static UtyRx.IObservable<AssetBundle> LoadFromCacheOrDownload(string url, int version, UtyRx.IProgress<float> progress = null)
        {
            return ObservableUnity.FromCoroutine<AssetBundle>((observer, cancellation) => FetchAssetBundle(WWW.LoadFromCacheOrDownload(url, version), observer, progress, cancellation));
        }

        public static UtyRx.IObservable<AssetBundle> LoadFromCacheOrDownload(string url, int version, uint crc, UtyRx.IProgress<float> progress = null)
        {
            return ObservableUnity.FromCoroutine<AssetBundle>((observer, cancellation) => FetchAssetBundle(WWW.LoadFromCacheOrDownload(url, version, crc), observer, progress, cancellation));
        }

        // over Unity5 supports Hash128
#if !(UNITY_4_6 || UNITY_4_5 || UNITY_4_4 || UNITY_4_3 || UNITY_4_2 || UNITY_4_1 || UNITY_4_0_1 || UNITY_4_0 || UNITY_3_5 || UNITY_3_4 || UNITY_3_3 || UNITY_3_2 || UNITY_3_1 || UNITY_3_0_0 || UNITY_3_0 || UNITY_2_6_1 || UNITY_2_6)
        public static UtyRx.IObservable<AssetBundle> LoadFromCacheOrDownload(string url, Hash128 hash128, UtyRx.IProgress<float> progress = null)
        {
            return ObservableUnity.FromCoroutine<AssetBundle>((observer, cancellation) => FetchAssetBundle(WWW.LoadFromCacheOrDownload(url, hash128), observer, progress, cancellation));
        }

        public static UtyRx.IObservable<AssetBundle> LoadFromCacheOrDownload(string url, Hash128 hash128, uint crc, UtyRx.IProgress<float> progress = null)
        {
            return ObservableUnity.FromCoroutine<AssetBundle>((observer, cancellation) => FetchAssetBundle(WWW.LoadFromCacheOrDownload(url, hash128, crc), observer, progress, cancellation));
        }
#endif

        // over 4.5, Hash define is Dictionary.
        // below Unity 4.5, WWW only supports Hashtable.
        // Unity 4.5, 4.6 WWW supports Dictionary and [Obsolete]Hashtable but WWWForm.content is Hashtable.
        // Unity 5.0 WWW only supports Dictionary and WWWForm.content is also Dictionary.
#if !(UNITY_METRO || UNITY_WP8) && (UNITY_4_5 || UNITY_4_6)
        static Hash MergeHash(Hashtable wwwFormHeaders, Hash externalHeaders)
        {
            var newHeaders = new Hash();
            foreach (DictionaryEntry item in wwwFormHeaders)
            {
                newHeaders.Add(item.Key.ToString(), item.Value.ToString());
            }
            foreach (HashEntry item in externalHeaders)
            {
                newHeaders.Add(item.Key, item.Value);
            }
            return newHeaders;
        }
#else
        static Hash MergeHash(Hash wwwFormHeaders, Hash externalHeaders)
        {
            foreach (HashEntry item in externalHeaders)
            {
                wwwFormHeaders[item.Key] = item.Value;
            }
            return wwwFormHeaders;
        }
#endif

        static IEnumerator Fetch(WWW www, UtyRx.IObserver<WWW> observer, UtyRx.IProgress<float> reportProgress, CancellationToken cancel)
        {
            using (www)
            {
                while (!www.isDone && !cancel.IsCancellationRequested)
                {
                    if (reportProgress != null)
                    {
                        try
                        {
                            reportProgress.Report(www.progress);
                        }
                        catch (Exception ex)
                        {
                            observer.OnError(ex);
                            yield break;
                        }
                    }
                    yield return null;
                }

                if (cancel.IsCancellationRequested) yield break;

                if (reportProgress != null)
                {
                    try
                    {
                        reportProgress.Report(www.progress);
                    }
                    catch (Exception ex)
                    {
                        observer.OnError(ex);
                        yield break;
                    }
                }

                if (!string.IsNullOrEmpty(www.error))
                {
                    observer.OnError(new WWWErrorException(www));
                }
                else
                {
                    observer.OnNext(www);
                    observer.OnCompleted();
                }
            }
        }

        static IEnumerator FetchText(WWW www, UtyRx.IObserver<string> observer, UtyRx.IProgress<float> reportProgress, CancellationToken cancel)
        {
            using (www)
            {
                while (!www.isDone && !cancel.IsCancellationRequested)
                {
                    if (reportProgress != null)
                    {
                        try
                        {
                            reportProgress.Report(www.progress);
                        }
                        catch (Exception ex)
                        {
                            observer.OnError(ex);
                            yield break;
                        }
                    }
                    yield return null;
                }

                if (cancel.IsCancellationRequested) yield break;

                if (reportProgress != null)
                {
                    try
                    {
                        reportProgress.Report(www.progress);
                    }
                    catch (Exception ex)
                    {
                        observer.OnError(ex);
                        yield break;
                    }
                }

                if (!string.IsNullOrEmpty(www.error))
                {
                    observer.OnError(new WWWErrorException(www));
                }
                else
                {
                    observer.OnNext(www.text);
                    observer.OnCompleted();
                }
            }
        }

        static IEnumerator FetchBytes(WWW www, UtyRx.IObserver<byte[]> observer, UtyRx.IProgress<float> reportProgress, CancellationToken cancel)
        {
            using (www)
            {
                while (!www.isDone && !cancel.IsCancellationRequested)
                {
                    if (reportProgress != null)
                    {
                        try
                        {
                            reportProgress.Report(www.progress);
                        }
                        catch (Exception ex)
                        {
                            observer.OnError(ex);
                            yield break;
                        }
                    }
                    yield return null;
                }

                if (cancel.IsCancellationRequested) yield break;

                if (reportProgress != null)
                {
                    try
                    {
                        reportProgress.Report(www.progress);
                    }
                    catch (Exception ex)
                    {
                        observer.OnError(ex);
                        yield break;
                    }
                }

                if (!string.IsNullOrEmpty(www.error))
                {
                    observer.OnError(new WWWErrorException(www));
                }
                else
                {
                    observer.OnNext(www.bytes);
                    observer.OnCompleted();
                }
            }
        }

        static IEnumerator FetchAssetBundle(WWW www, UtyRx.IObserver<AssetBundle> observer, UtyRx.IProgress<float> reportProgress, CancellationToken cancel)
        {
            using (www)
            {
                while (!www.isDone && !cancel.IsCancellationRequested)
                {
                    if (reportProgress != null)
                    {
                        try
                        {
                            reportProgress.Report(www.progress);
                        }
                        catch (Exception ex)
                        {
                            observer.OnError(ex);
                            yield break;
                        }
                    }
                    yield return null;
                }

                if (cancel.IsCancellationRequested) yield break;

                if (reportProgress != null)
                {
                    try
                    {
                        reportProgress.Report(www.progress);
                    }
                    catch (Exception ex)
                    {
                        observer.OnError(ex);
                        yield break;
                    }
                }

                if (!string.IsNullOrEmpty(www.error))
                {
                    observer.OnError(new WWWErrorException(www));
                }
                else
                {
                    observer.OnNext(www.assetBundle);
                    observer.OnCompleted();
                }
            }
        }
    }

    public class WWWErrorException : Exception
    {
        public string RawErrorMessage { get; private set; }
        public bool HasResponse { get; private set; }
        public System.Net.HttpStatusCode StatusCode { get; private set; }
        public System.Collections.Generic.Dictionary<string, string> ResponseHeaders { get; private set; }
        public WWW WWW { get; private set; }

        public WWWErrorException(WWW www)
        {
            this.WWW = www;
            this.RawErrorMessage = www.error;
            this.ResponseHeaders = www.responseHeaders;
            this.HasResponse = false;

            var splitted = RawErrorMessage.Split(' ');
            if (splitted.Length != 0)
            {
                int statusCode;
                if (int.TryParse(splitted[0], out statusCode))
                {
                    this.HasResponse = true;
                    this.StatusCode = (System.Net.HttpStatusCode)statusCode;
                }
            }
        }

        public override string ToString()
        {
            return RawErrorMessage;
        }
    }
}