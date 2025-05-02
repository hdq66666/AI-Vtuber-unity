using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using TMPro;

[RequireComponent(typeof(PlayerControl), typeof(AudioSource))]
public class ApiDisplay : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TextMeshProUGUI[] texts;  // 0=动作名 1=优先级 2=ID

    [Header("Network")]
    [SerializeField] private string getUrl   = "http://127.0.0.1:8082/get_action_mapping";
    [SerializeField] private string deleteUrl = "http://127.0.0.1:8082/delete_action_mapping";
    [SerializeField] private string changeCameraUrl = "http://127.0.0.1:8082/add_camera_change";
    [SerializeField] private float firstDelay = 2f;
    [SerializeField] private float interval   = 3f;
    [SerializeField] private int   timeout    = 8;
    [SerializeField] private bool  autoDelete = true;     // 完成后自动调删除接口

    [SerializeField] private Camera[]  cameras;

    private PlayerControl _player;
    private AudioSource   _audio;
    private WaitForSeconds _wait;
    private bool _busy;

    /* ---------- Unity lifecycle ---------- */
    private void Awake()
    {
        Screen.SetResolution(1000, 1000, false);
        
        _player = GetComponent<PlayerControl>();
        _audio  = GetComponent<AudioSource>();
        _wait   = new WaitForSeconds(interval);
    }
    private void OnEnable()  => StartCoroutine(FetchLoop());

    /* ---------- Main Loop ---------- */
    private IEnumerator FetchLoop()
    {
        yield return new WaitForSeconds(firstDelay);
        while (enabled)
        {
            yield return GetMappings();
            yield return GetCameraChange();
            yield return _wait;
        }
    }

    /* ---------- GET /get_action_mapping ---------- */
    private IEnumerator GetMappings()
    {
        if (_busy) yield break;
        _busy = true;
        
        using var req = UnityWebRequest.Get(getUrl);
        req.timeout = timeout;
        yield return req.SendWebRequest();
        _busy = false;
        
        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"[ApiDisplay] GET 失败: {req.error}");
            ShowErr();
            yield break;
        }
        
        // 反序列化
        ApiRoot root = JsonUtility.FromJson<ApiRoot>(req.downloadHandler.text);

        
        // 验证数据结构
        if (root.data.data == null || root.data.data.Length == 0)
        {
            Debug.LogWarning("数据数组为空");
            ClearUI();
            yield break;
        }
        
        // 获取第一条记录（可扩展为遍历所有记录）
        ActionInfo firstItem = root.data.data[0];
    
        // 示例：访问所有字段
        Debug.Log($"动作名称：{firstItem.action_name}\n"
                  + $"分组ID：{firstItem.group_id}\n"
                  + $"音频时长：{firstItem.audio_duration}s\n"
                  + $"执行状态：{firstItem.is_executed}");
    
        // 更新UI和动画逻辑需要同步修改
        UpdateUI(firstItem);
        TriggerAnimation(firstItem);

        if (autoDelete)
            StartCoroutine(DeleteAction(firstItem.id, false));
    }

    /* ---------- GET /get_camera_change ---------- */
    private IEnumerator GetCameraChange()
    {
        if (_busy) yield break;
        _busy = true;
        
        using var req = UnityWebRequest.Get(changeCameraUrl);
        Debug.Log(req.downloadHandler.text);
        req.timeout = timeout;
        yield return req.SendWebRequest();
        _busy = false;
        
        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"[Camera] GET 失败: {req.error}" + ": " + "Camera 数据数组未修改");
            ShowErr();
            yield break;
        }

        Debug.Log(req.downloadHandler.text);
        
        // 反序列化
        ApiRoot root = JsonUtility.FromJson<ApiRoot>(req.downloadHandler.text);

        
        // 验证数据结构
        if (root.data.data == null || root.data.data.Length == 0)
        {
            Debug.LogWarning("Camera 数据数组为空");
            yield break;
        }

        foreach (ActionInfo item in root.data.data)
        {
            if(!string.IsNullOrEmpty(item.name))
            {
                Debug.Log($"Camera Funciotn:{item.name}");
                int cameraId = int.Parse(item.name);
                SwitchCamera(cameraId);
                yield break;
            }
        }
        
    }

    /* ---------- DELETE /delete_action_mapping ---------- */
    public IEnumerator DeleteAction(int id, bool deleteAll)
    {
        string url = $"{deleteUrl}?action_id={id}&delete_all={deleteAll.ToString().ToLower()}";
        // using var req = UnityWebRequest.Delete(url);
        using var req = UnityWebRequest.PostWwwForm(url, string.Empty);
        req.timeout = timeout;
        yield return req.SendWebRequest();

        if (req.result == UnityWebRequest.Result.Success)
            Debug.Log($"[ApiDisplay] 删除动作 {id} 成功");
        else
            Debug.LogError($"[ApiDisplay] 删除动作 {id} 失败: {req.error}");
    }

    /* ---------- Helpers ---------- */
    private static bool TryParse(string json, out ApiRoot root)
    {
        try { root = JsonUtility.FromJson<ApiRoot>(json); return true; }
        catch { root = default; return false; }
    }

    private void ShowErr()
    {return;
        foreach (var t in texts) t.text = "ERR";
    }

    private void ClearUI()
    {return;
        foreach (var t in texts) t.text = string.Empty;
    }

    private void UpdateUI(ActionInfo a)
    {
        Debug.Log(a.action_name);
        if (texts.Length < 3) return;
        texts[0].text = a.action_name;
        texts[1].text = $"P{a.priority}";
        texts[2].text = $"组{a.group_id}"; // 原ID显示改为分组ID
    }

    private void TriggerAnimation(ActionInfo a)
    {
        int code = a.group_id is >=1 and <=14 ? a.group_id : 0;
        // 新增分组描述使用
        Debug.Log($"动作分组：{a.group_description}");
        if (code == 0)
        {
            Debug.LogWarning($"[ApiDisplay] 未知动作组: {a.group_id}");
            return;
        }
        Debug.Log(code);
        _player.PlayerAni(code);
        _player.MarkAction(); 
        if (!string.IsNullOrEmpty(a.audio_url))
            StartCoroutine(PlayAudio(a.audio_url));
    }

    private IEnumerator PlayAudio(string url)
    {
        using var req = UnityWebRequestMultimedia.GetAudioClip(url, AudioType.WAV);
        req.timeout = timeout;
        yield return req.SendWebRequest();
        
        Debug.Log(req.result);

        if (req.result == UnityWebRequest.Result.Success)
        {
            AudioClip clip = DownloadHandlerAudioClip.GetContent(req);
            _audio.clip = clip;
            _audio.Play();

            // ---------- 关键：根据音频时长让角色张嘴 ----------
            if (clip != null)
                StartCoroutine(_player.Speak(clip.length));
        }
        else
        {
            Debug.LogError($"[ApiDisplay] 音频加载失败: {req.error}");
        }
    }

    /* ---------- JSON DTO ---------- */
    [System.Serializable]
    private struct ApiRoot
    {
        public int code;
        public string message;
        public ActionData data; // 对应最外层data字段
    }

    [System.Serializable]
    private struct ActionData
    {
        public ActionInfo[] data; // 对应内层data数组
        // 如果接口返回包含count字段可保留，否则需要删除
        public int count; 
    }

    [System.Serializable] 
    private struct ActionInfo
    {
        // 保持与接口字段完全一致
        public int id;
        public string action_name;
        public string match_word;
        public int priority;
        public int group_id;       // 原group_id字段需要改名
        public string group_description;
        public string timestamp;
        public bool is_executed;
        public string content;
        public string audio_path;
        public string audio_url;
        public float audio_duration;

        public string name;
    }

    private void SwitchCamera(int cameraID)
    {
        if(cameraID < 0 || cameraID >= cameras.Length)
        {
            Debug.LogError($"无效的摄像机ID：{cameraID}");
            return;
        }

        for (int i = 0; i < cameras.Length; i++)
        {
            cameras[i].gameObject.SetActive(i == cameraID);
        }
    }
}
